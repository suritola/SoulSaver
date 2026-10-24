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
        PruneOrbHits();
    }

    // 명중 기록 가지치기 — 0.5초 넘은 기록은 판정에 안 쓰이므로 2초마다 지움 (한 판 내내 쌓이던 누수)
    float orbPruneAt;
    static readonly List<Collider2D> orbStale = new List<Collider2D>(32);
    void PruneOrbHits()
    {
        if (Time.time < orbPruneAt) return;
        orbPruneAt = Time.time + 2f;
        orbStale.Clear();
        foreach (var kv in orbHitTimes)
            if (kv.Key == null || Time.time - kv.Value > 1f) orbStale.Add(kv.Key);
        for (int i = 0; i < orbStale.Count; i++) orbHitTimes.Remove(orbStale[i]);
        orbStale.Clear();
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

    // 영혼 트리 생존 끝 칸 「되살아난 영혼」: 판마다 한 번 체력 40%로 (1.0.1)
    public int TreeRevives;
    bool revivedByTree;
    int treeRevivesUsed;        // 1.5.9~ 이번 판에 쓴 「되살아난 영혼」 횟수 (판 중간 저장 — 이어하기에서 기록을 다시 적용해도 다시 채워지지 않게)

    // 1.5.9~ 판 중간 저장 (RunSave): 쓴 부활 횟수 — 복원은 기록을 다시 적용한 뒤 (ReplayNode 가 채운 TreeRevives 에서 뺌)
    public void GetRevivesUsed(out int tree, out int undying) { tree = treeRevivesUsed; undying = undyingUses; }
    public void RestoreRevivesUsed(int tree, int undying)
    {
        treeRevivesUsed = Mathf.Max(0, tree);
        TreeRevives = Mathf.Max(0, TreeRevives - treeRevivesUsed);
        undyingUses = Mathf.Max(0, undying);
    }

    // 불사의 맹세: 판마다 한 번
    public bool TryUndying()
    {
        revivedByTree = false;
        if (!Has(UndyingId) || undyingUses >= UndyingMaxUses)
        {
            if (TreeRevives <= 0) return false;
            TreeRevives--;
            treeRevivesUsed++;
            revivedByTree = true;
            fx.Play("pulse", 1f, 0.6f);
            fx.Play("chime", 1f, 0.9f);
            fx.Shake(0.4f, 0.3f);
            Flash(player.transform.position, 6f, new Color(0.6f, 0.95f, 1f, 0.8f), 0.6f);
            Notice.Big(Loc.T("영혼이 되살아났다!"), NoticeIcon.Revive, 2.2f, NoticeLevel.Urgent);
            return true;
        }
        undyingUses++;
        fx.Play("pulse", 1f, 0.6f);
        fx.Play("chime", 1f, 0.7f);
        fx.Shake(0.4f, 0.3f);
        Flash(player.transform.position, 6f, new Color(1f, 0.9f, 0.5f, 0.8f), 0.6f);
        Notice.Big(Loc.T("불사의 맹세가 발동했다!"), NoticeIcon.Shield, 2.2f, NoticeLevel.Urgent);
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
    // weakMul > 1: 직격점(pos) 빈틈 — 폭발 피해가 그 보스 · 중간 보스에게 들어갈 때 늘림 (따로 한 번 더 때리지 않음 · 거너 유탄)
    public void Explode(Vector3 pos, float radius, float damage, float knock, Color color, float weakMul = 0f)
    {
        using (var ovl = PhysHits.Circle(pos, radius)) foreach (Collider2D c in ovl)
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Vector3 dir = (c.transform.position - pos).normalized;
            if (weakMul > 1f) WeakHit.Hit(c.gameObject, WeakShape.Point(pos, 0.4f, weakMul), damage, dir, knock);
            else Specials.Damage(c.gameObject, damage, dir, knock);
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
        // 1.5.1~ 흐린 빛(가장자리가 번져 판정 원이 안 보이던 것) → 도트 바닥 원판 (체크 디더 + 테두리, 가장자리 = 판정 원 · 지름 그대로)
        //   원판이 빛보다 고르게 진해 알파를 조금 낮춤. 그림이 없으면 예전 빛 그대로
        Sprite floor = PixelLine.Floor;
        GameObject z = floor != null
            ? MakeSprite("DamageZone", floor, pos, PixelLine.DiscScale(floor, radius * 2f), new Color(color.r, color.g, color.b, color.a * 0.8f), "Background", 7)
            : MakeSprite("DamageZone", glowSprite, pos, radius * 2f / 8f, color, "Background", 7);
        DamageZone dz = z.AddComponent<DamageZone>();
        dz.radius = radius;
        dz.duration = duration;
        dz.tickDamage = tickDamage;
        return dz;
    }

    // 맞은 적 집합을 빌려 씀 (피해 도중 다시 연쇄 번개가 터져도 서로 다른 집합)
    static readonly Stack<HashSet<Collider2D>> chainSets = new Stack<HashSet<Collider2D>>();

    void ChainLightning(Vector3 from, Collider2D first, float damage, int jumps, float falloff = 0.8f)
    {
        HashSet<Collider2D> hit = chainSets.Count > 0 ? chainSets.Pop() : new HashSet<Collider2D>();
        hit.Clear();
        try { ChainLightning(hit, from, first, damage, jumps, falloff); }
        finally { hit.Clear(); if (chainSets.Count < 8) chainSets.Push(hit); }
    }

    void ChainLightning(HashSet<Collider2D> hit, Vector3 from, Collider2D first, float damage, int jumps, float falloff)
    {
        fx.Play("zap", 0.5f, Random.Range(0.9f, 1.1f));
        hit.Add(first);
        Vector3 current = from;
        for (int j = 0; j < jumps; j++)
        {
            Collider2D next = null;
            float best = 6f;
            using (var ovl = PhysHits.Circle(current, 6f)) foreach (Collider2D c in ovl)
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
        PixelLine.Apply(lr);        // 1.5.1~ 도트 획 (색 부분 두께는 그대로)
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
