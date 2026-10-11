using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 특수 패시브 (구슬 · 오라 · 가시 …) 와 여러 능력이 같이 쓰는 효과 (폭발 · 연쇄 번개 · 장판 …)
// SpecialAbilities.cs 에서 나눔 (2.1.1)
public partial class SpecialAbilities
{
    // ================================================================= passives
    void SpawnOrbs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject o = MakeSprite("GuardianSoul", swirlSprite, player.transform.position, 0.55f, new Color(0.55f, 0.95f, 1f), "Effect", 2);
            orbs.Add(o.transform);
        }
    }

    const float OrbRadius = 4.6f;
    static readonly List<Collider2D> orbHits = new List<Collider2D>(16), orbHitsCopy = new List<Collider2D>(16);

    void UpdateOrbs()
    {
        float baseAngle = Time.time * 180f;
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null) continue;
            float a = (baseAngle + 360f / orbs.Count * i) * Mathf.Deg2Rad;
            Vector3 pos = player.transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a) - 0.4f, 0f) * OrbRadius;
            orbs[i].position = pos;
            orbs[i].Rotate(0f, 0f, -540f * Time.deltaTime);

            // 목록을 복사해 두고 돎 (피해 도중 다른 검색이 같은 목록을 써도 안전하게)
            orbHitsCopy.Clear();
            orbHitsCopy.AddRange(Specials.Overlap(pos, 1.25f, orbHits));
            foreach (Collider2D c in orbHitsCopy)
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                if (orbHitTimes.TryGetValue(c, out float last) && Time.time - last < 0.5f) continue;
                orbHitTimes[c] = Time.time;
                if (Time.time - lastOrbSound > 0.15f)
                {
                    lastOrbSound = Time.time;
                    fx.Play("pew", 0.18f, 2f);
                }
                Specials.Damage(c.gameObject, Damage * (IsEvolved(OrbsId) ? 1.2f : 0.8f), (c.transform.position - player.transform.position).normalized, 0.8f);
            }
        }
    }

    // 플레이어가 맞았을 때 (복수의 가시)
    public void OnPlayerHurt()
    {
        using var source = DamageSource.As(DamageSource.Special);     // 복수의 가시 · 캐릭터 능력 (적이 때린 순간이라 출처가 비어 있음)
        if (player != null) KitOnHurt();
        if (!Has(ThornsId) || player == null) return;
        fx.Play("boom", 0.6f, 1.4f);
        fx.FloatText(player.transform.position, Loc.T("가시 반격!"), new Color(1f, 0.35f, 0.4f), 4.5f, 0.3f);
        bool evo = IsEvolved(ThornsId);
        Explode(player.transform.position, evo ? 6f : 4f, Damage * (evo ? 5f : 3f), 2f, new Color(0.9f, 0.2f, 0.3f, 0.85f));
    }

    // 영혼 트리 생존 끝 칸 「되살아난 영혼」: 판마다 한 번 체력 40%로 (1.0.5)
    public int TreeRevives;
    bool revivedByTree;

    // 불사의 맹세: 판마다 한 번
    public bool TryUndying()
    {
        revivedByTree = false;
        if (!Has(UndyingId) || undyingUses >= UndyingMaxUses)
        {
            if (TreeRevives <= 0) return false;
            TreeRevives--;
            revivedByTree = true;
            fx.Play("pulse", 1f, 0.6f);
            fx.Play("chime", 1f, 0.9f);
            fx.Shake(0.4f, 0.3f);
            Flash(player.transform.position, 6f, new Color(0.6f, 0.95f, 1f, 0.8f), 0.6f);
            if (StageManager.Instance != null) StageManager.Instance.ShowBanner(Loc.T("영혼이 되살아났다!"), 2f);
            return true;
        }
        undyingUses++;
        fx.Play("pulse", 1f, 0.6f);
        fx.Play("chime", 1f, 0.7f);
        fx.Shake(0.4f, 0.3f);
        Flash(player.transform.position, 6f, new Color(1f, 0.9f, 0.5f, 0.8f), 0.6f);
        if (StageManager.Instance != null) StageManager.Instance.ShowBanner(Loc.T("불사의 맹세가 발동했다!"), 2f);
        return true;
    }

    int UndyingMaxUses => IsEvolved(UndyingId) ? 2 : 1;

    // 부활할 때 체력 (진화하면 절반)
    public float UndyingReviveHealth(float maxHealth) => revivedByTree ? maxHealth * 0.4f : IsEvolved(UndyingId) ? maxHealth * 0.5f : 1f;

    // 탄창 저주: 탄창의 마지막 한 발 (진화하면 두 발)
    public bool IsLastBulletCursed(int bulletsBeforeShot) => Has(CurseId) && bulletsBeforeShot >= 1 && bulletsBeforeShot <= (IsEvolved(CurseId) ? 2 : 1);

    public void CurseBullet(Bullet b, bool cursed)
    {
        if (b == null || !cursed) return;
        if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(1f, 0.3f, 0.3f);
        b.transform.localScale *= 1.5f;
        float dmg = Damage * 1.5f;
        b.onHitEnemy += (bullet, col) => Explode(col.transform.position, 2.5f, dmg, 1.5f, new Color(1f, 0.3f, 0.2f, 0.85f));
    }

    void OnEnemyKilled(Vector3 pos)
    {
        KitOnKill(pos);
        TreeOnKill(pos);
        if (!Has(SoulBurstId)) return;
        souls = Mathf.Min(40, souls + 1);
        if (souls == SoulsNeeded)
        {
            fx.Play("chime", 0.5f, 1.3f);
            fx.FloatText(player.transform.position, Loc.T("영혼 폭발 준비"), new Color(0.55f, 0.95f, 1f), 4.5f, 0f);
        }
    }

    // ================================================================= shared effects
    public void Explode(Vector3 pos, float radius, float damage, float knock, Color color)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, radius))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Vector3 dir = (c.transform.position - pos).normalized;
            Specials.Damage(c.gameObject, damage, dir, knock);
        }
        Flash(pos, radius * 2f, color, 0.3f);
        Flash(pos, radius * 0.9f, new Color(1f, 1f, 1f, 0.9f), 0.12f);
        // 도트 폭발: 따뜻한 색은 화염, 차가운 색은 영혼 폭발
        Fx.Spawn(color.r >= color.b ? "fx_explosion" : "fx_soulburst", pos, radius * 2.4f, Color.white, 16f);
        Fx.Spawn("fx_shock", pos, radius * 2.6f, color, 20f);
        ShockRing.Spawn(pos, radius * 0.3f, radius * 1.15f, 0.3f, color, 0.3f);
        for (int i = 0; i < Mathf.Clamp(Mathf.RoundToInt(radius * 3f), 4, 16); i++)
            SoulWisp.Spawn(pos, pos + (Vector3)(Random.insideUnitCircle.normalized * radius * 1.5f), color, true);
        if (fx != null)
        {
            fx.Play("boom", Mathf.Clamp(radius / 5f, 0.3f, 0.9f), Mathf.Clamp(1.6f - radius * 0.12f, 0.8f, 1.5f));
            if (radius >= 3f) fx.Shake(0.12f + radius * 0.03f, 0.12f);
        }
    }

    public DamageZone SpawnZone(Vector3 pos, float radius, float duration, float tickDamage, Color color)
    {
        GameObject z = MakeSprite("DamageZone", glowSprite, pos, radius * 2f / 8f, color, "Background", 7);
        DamageZone dz = z.AddComponent<DamageZone>();
        dz.radius = radius;
        dz.duration = duration;
        dz.tickDamage = tickDamage;
        return dz;
    }

    void ChainLightning(Vector3 from, Collider2D first, float damage, int jumps, float falloff = 0.8f)
    {
        fx.Play("zap", 0.5f, Random.Range(0.9f, 1.1f));
        HashSet<Collider2D> hit = new HashSet<Collider2D> { first };
        Vector3 current = from;
        for (int j = 0; j < jumps; j++)
        {
            Collider2D next = null;
            float best = 6f;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(current, 6f))
            {
                if (hit.Contains(c) || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                float d = Vector2.Distance(current, c.transform.position);
                if (d < best) { best = d; next = c; }
            }
            if (next == null) break;
            hit.Add(next);
            DrawBolt(current, next.transform.position, new Color(0.6f, 0.9f, 1f), 0.18f);
            Flash(next.transform.position, 1.6f, new Color(0.6f, 0.9f, 1f, 0.8f), 0.12f);
            Specials.Damage(next.gameObject, damage, Vector3.zero, 0f);
            current = next.transform.position;
            damage *= falloff;
        }
    }

    public void Flash(Vector3 pos, float size, Color color, float duration)
    {
        // 번쩍임 효과를 끄면 섬광을 옅게
        if (!GameSettings.Flashes) color.a *= 0.35f;
        FadeSprite.Spawn("Flash", glowSprite, pos, size / 8f, color, "Effect", 3, duration);
    }

    void DrawLine(Vector3 a, Vector3 b, Color color, float duration)
    {
        GameObject go = new GameObject("Line");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = 0.25f;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.sortingLayerName = "Effect";
        Destroy(go, duration);
    }

    public static GameObject MakeSprite(string name, Sprite sprite, Vector3 pos, float scale, Color color, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return go;
    }

    Vector3 ClampToArena(Vector3 p)
    {
        EnemySpawner sp = FindFirstObjectByType<EnemySpawner>();
        if (sp == null) return p;
        return new Vector3(Mathf.Clamp(p.x, sp.spawnAreaMin.x, sp.spawnAreaMax.x), Mathf.Clamp(p.y, sp.spawnAreaMin.y, sp.spawnAreaMax.y + 1.5f), 0f);
    }

    public void ScytheReturned()
    {
        activeScythe = null;
        if (fx != null && CurrentWeapon == ScytheId) fx.Play("clank", 0.5f);
    }
}
