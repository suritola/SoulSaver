using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 유령 (1.1.2): 죽지 못한 영혼
//   평타 · 차가운 손길: 앞으로 짧게 뻗는 손이 한 줄의 적을 모두 꿰뚫고 냉기를 쌓음 (다섯 번 쌓이면 1초 얼어붙음)
//   영혼(도깨비불): 적을 쓰러뜨리면 확률로 영혼이 생겨 몸 둘레를 돎 (최대 6). 맞을 때 영혼이 대신 사라지며 피해를 막음
//   우클릭 · 빙의: 누르고 있으면 마우스 가까운 적에 표시, 떼면 그 적의 몸에 들어갔다가 1초 뒤 안에서 터뜨림
//                 (들어간 동안 무적 · 둘레 적에게도 피해와 냉기)
// 레벨업 카드 (card): 0 꿰뚫는 냉기 · 1 얼어붙는 손길 · 2 원혼 · 3 빙의 여운 · 5 영혼 수호 · 6 얼음 파편
// 형태: 사슬 망령 · 얼음 유령 · 그림자 유령 / 강화: 깊은 냉기 · 두 손 · 영혼 폭발
public partial class CharacterKit
{
    static readonly Color Chill = new Color(0.6f, 0.9f, 1f, 0.9f);
    static readonly Color Wraith = new Color(0.75f, 0.95f, 1f, 0.85f);

    // 냉기: 적마다 쌓인 수 · 얼어붙은 때까지
    readonly Dictionary<GameObject, int> chill = new Dictionary<GameObject, int>();
    readonly Dictionary<GameObject, float> frozenUntil = new Dictionary<GameObject, float>();
    int FreezeNeed => Mathf.Max(2, 5 - card[1]);
    float FreezeTime => augment == AugDeepChill ? 1.5f : 1f;
    float HandReach => 4.5f + card[0];
    public bool IsFrozen(GameObject g) => g != null && frozenUntil.TryGetValue(g, out float t) && t > Time.time;
    public int ChillOf(GameObject g) => g != null && chill.TryGetValue(g, out int n) ? n : 0;

    // 영혼 (도깨비불)
    public int Wisps { get; private set; }
    public int WispMax => 6 + 2 * card[5];
    readonly List<FxAnim> wispFx = new List<FxAnim>();
    static readonly List<Collider2D> ghostHits = new List<Collider2D>(64);

    void GhostAttack(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        foreach (Vector2 d in Spread(dir, shots, 18f)) Hand(start, d, dmg, true);
        if (augment == AugTwoHands)
        {
            Hand(start, Quaternion.Euler(0f, 0f, 25f) * dir, dmg * 0.7f, false);
            Hand(start, Quaternion.Euler(0f, 0f, -25f) * dir, dmg * 0.7f, false);
        }
        if (form == SpecialAbilities.KitShadowGhost) StartCoroutine(ShadowHand(dir, dmg * 0.5f));
        PlayerMotion.Of(this).Lunge(0.12f, dir);
        Play("whoosh", 0.4f, 0.75f);
        Play("shimmer", 0.25f, 1.6f);
    }

    // 손 하나: 한 줄 (굵기 0.9) 의 적을 모두
    void Hand(Vector3 start, Vector2 dir, float dmg, bool main)
    {
        Vector3 end = start + (Vector3)(dir * HandReach * SignatureSkills.HandReachMul());
        Vector3 mid = (start + end) * 0.5f;
        bool first = true;
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(mid, HandReach * 0.5f + 1f, ghostHits)))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            if (Hostile.DistanceToSegment(c.transform.position, start, end) > 0.9f) continue;
            GameObject g = c.gameObject;
            Specials.Damage(g, dmg * formPower, dir, 0.6f);
            AddChill(g, form == SpecialAbilities.KitIceGhost ? 2 : 1);
            SignatureSkills.Touched(c);
            if (first && form == SpecialAbilities.KitSoulChain)
            {
                Transform other = NearestOther(c.transform.position, 6f, c.transform, dir, 360f);
                if (other != null)
                {
                    Fx.Bolt(c.transform.position, other.position, 0.5f, Wraith);
                    Specials.Damage(other.gameObject, dmg * 0.4f * formPower, Vector3.zero, 0f);
                }
            }
            first = false;
        }
        Fx.Beam(start, end, main ? 1f : 0.7f, SkinFx.Tint(new Color(0.55f, 0.85f, 1f, 0.75f)), 0.18f);
        Fx.Spawn("fx_bonehand", end, 1.6f, Wraith, 22f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        if (SignatureSkills.HandTrail()) StartCoroutine(FrostTrail(start, end));
    }

    IEnumerator ShadowHand(Vector2 dir, float dmg)
    {
        yield return new WaitForSeconds(0.15f);
        Hand(player.MuzzlePosition, dir, dmg, false);
    }

    // 눈보라 (진화): 손이 지난 자리에 1.5초 냉기 안개
    IEnumerator FrostTrail(Vector3 a, Vector3 b)
    {
        Fx.Spawn("fx_cloud", (a + b) * 0.5f, (b - a).magnitude, new Color(0.8f, 0.95f, 1f, 0.5f), 10f);
        for (float t = 0f; t < 1.5f; t += 0.5f)
        {
            yield return new WaitForSeconds(0.5f);
            foreach (Collider2D c in new List<Collider2D>(Specials.Overlap((a + b) * 0.5f, (b - a).magnitude * 0.5f + 1f, ghostHits)))
                if (c != null && (c.CompareTag("enermy") || c.CompareTag("boss")) && Hostile.DistanceToSegment(c.transform.position, a, b) <= 1f)
                    AddChill(c.gameObject, 1);
        }
    }

    // 냉기 쌓기 → 다 차면 얼어붙음
    public void AddChill(GameObject g, int n)
    {
        if (g == null || IsFrozen(g)) return;
        chill.TryGetValue(g, out int have);
        have += n;
        if (g.TryGetComponent(out EnermyController e)) e.Slow(1f - 0.1f * Mathf.Min(have, 5), 1.5f);
        if (have >= FreezeNeed)
        {
            have = 0;
            frozenUntil[g] = Time.time + FreezeTime;
            if (e != null) e.Slow(0f, FreezeTime);
            Fx.Spawn("fx_sparkle", g.transform.position, 1.6f, Chill, 20f);
            Play("shatter", 0.25f, 1.8f);
            SignatureSkills.Frozen(g);
        }
        chill[g] = have;
    }

    // 처치: 영혼이 생김 · 원혼 · 얼음 파편
    void GhostKill(Vector3 pos)
    {
        if (Id != CharacterId.Ghost) return;
        float chance = 0.35f + SignatureSkills.WispChanceBonus();
        if (Random.value < chance) AddWisps(1);
        if (card[2] > 0)
        {
            Transform t = Specials.NearestEnemy(pos, 8f);
            if (t != null)
            {
                Fx.Bolt(pos, t.position, 0.6f, Wraith, 0.2f);
                Specials.Damage(t.gameObject, Damage * (0.3f + 0.3f * card[2]), Vector3.zero, 0.5f);
            }
        }
        // 얼음 파편: 얼어붙은 적이 쓰러지면 (얼어붙은 표시는 쓰러진 자리 가까이로 찾음)
        if (card[6] > 0 && RecentlyFrozenNear(pos))
        {
            int n = 2 + 2 * card[6];
            for (int i = 0; i < n; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, 360f / n * i) * Vector2.right;
                Bullet b = Projectile(pos, d, Damage * 0.5f, 1, 22f, 6f, "fx_spike", 0.6f, Chill, false);
                if (b != null && SignatureSkills.ShardsFreeze()) b.onHitEnemy += (s, c) => { if (c != null) AddChill(c.gameObject, FreezeNeed); };
            }
        }
        PruneChill();
    }

    bool RecentlyFrozenNear(Vector3 pos)
    {
        foreach (var kv in frozenUntil)
            if (kv.Key != null && kv.Value > Time.time - 0.2f && (kv.Key.transform.position - pos).sqrMagnitude < 1f) return true;
        return false;
    }

    float pruneChillAt;
    void PruneChill()
    {
        if (Time.time < pruneChillAt) return;
        pruneChillAt = Time.time + 3f;
        List<GameObject> dead = new List<GameObject>();
        foreach (var kv in chill) if (kv.Key == null) dead.Add(kv.Key);
        foreach (GameObject g in dead) chill.Remove(g);
        dead.Clear();
        foreach (var kv in frozenUntil) if (kv.Key == null || kv.Value < Time.time - 5f) dead.Add(kv.Key);
        foreach (GameObject g in dead) frozenUntil.Remove(g);
    }

    public void AddWisps(int n)
    {
        Wisps = Mathf.Min(WispMax, Wisps + n);
    }

    // 맞을 때: 영혼이 대신 사라지며 막음 (SignatureSkills.Taken 에서)
    public bool TryWispBlock()
    {
        if (Id != CharacterId.Ghost || Wisps <= 0) return false;
        Wisps--;
        Vector3 p = transform.position;
        Fx.Spawn("fx_soulburst", p, 3f, Wraith, 18f);
        Play("shimmer", 0.6f, 0.7f);
        if (augment == AugSoulBurst)
        {
            DamageCircle(p, 3f, Damage * 1.5f, 2f);
            Fx.Spawn("fx_shock", p, 6f, Chill, 18f);
        }
        SignatureSkills.WispBlocked(p);
        return true;
    }

    // 영혼 그림: 몸 둘레를 돎
    void DrawWisps()
    {
        while (wispFx.Count < Wisps)
        {
            FxAnim a = Fx.Play("fx_orb", transform.position, 0.6f, Wraith, 10f, 0f, 21, true, -1f);
            if (a == null) break;
            wispFx.Add(a);
        }
        while (wispFx.Count > Wisps)
        {
            FxAnim a = wispFx[wispFx.Count - 1];
            wispFx.RemoveAt(wispFx.Count - 1);
            if (a != null) Destroy(a.gameObject);
        }
        for (int i = 0; i < wispFx.Count; i++)
        {
            if (wispFx[i] == null) continue;
            float ang = Time.time * 120f + 360f / Mathf.Max(1, wispFx.Count) * i;
            float r = 1.3f + 0.1f * Mathf.Sin(Time.time * 3f + i);
            wispFx[i].transform.position = transform.position + Quaternion.Euler(0f, 0f, ang) * Vector3.right * r + Vector3.up * 0.3f;
        }
    }

    // ================================================================= 우클릭: 빙의
    bool possessAiming, possessing;
    Transform possessTarget;

    Transform PossessCandidate()
    {
        Vector3 m = ClampRange(transform.position, Mouse, 14f);
        Transform best = null;
        float bd = 9f;
        foreach (Collider2D c in Specials.Overlap(m, 3f, ghostHits))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            if (c.TryGetComponent(out EnermyController e) && e.IsDead) continue;
            float d = (c.transform.position - m).sqrMagnitude;
            if (d < bd) { bd = d; best = c.transform; }
        }
        return best;
    }

    void UpdatePossess(SkillGauge gauge, bool full)
    {
        DrawWisps();
        if (possessing) return;
        if (!possessAiming && GameInput.UltDown && full) { possessAiming = true; Play("hum", 0.4f, 0.6f); }
        if (!possessAiming) return;
        possessTarget = PossessCandidate();
        if (ultAimRing == null) { ultAimRing = Hostile.NewLine("UltAimRing", Color.white, 0.12f, 25); ultAimRing.loop = true; }
        ultAimRing.enabled = true;
        float pulse = 0.55f + 0.35f * Mathf.Sin(Time.time * 14f);
        if (possessTarget != null)
        {
            Hostile.SetArc(ultAimRing, possessTarget.position, 1.4f, 0f, 360f);
            ultAimRing.startColor = ultAimRing.endColor = new Color(Chill.r, Chill.g, Chill.b, pulse);
        }
        else
        {
            Hostile.SetArc(ultAimRing, ClampRange(transform.position, Mouse, 14f), 3f, 0f, 360f);
            ultAimRing.startColor = ultAimRing.endColor = new Color(0.6f, 0.6f, 0.7f, 0.35f);       // 빙의할 적이 없음
        }
        ultAimRing.startWidth = ultAimRing.endWidth = 0.14f;

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        possessAiming = false;
        ultAimRing.enabled = false;
        if (possessTarget == null) { Play("buzz", 0.4f); return; }          // 적이 없으면 게이지를 쓰지 않음
        StartCoroutine(PossessRoutine(possessTarget, Damage * 6f * UltMul));
        KitUltAdd(1.2f, possessTarget.position);
        Spend(gauge);
    }

    IEnumerator PossessRoutine(Transform target, float dmg)
    {
        possessing = true;
        player.GrantInvincibility(1.8f);
        Color before = body.color;
        Vector3 from = transform.position;
        Play("whoosh", 0.9f, 0.5f);
        for (float t = 0f; t < 0.15f && target != null; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, target.position, t / 0.15f);
            yield return null;
        }
        body.color = new Color(before.r, before.g, before.b, 0.12f);
        FxAnim glow = target != null ? Fx.Play("fx_soulburst", target.position, 2.6f, Wraith, 12f, 0f, 22, true, 1f) : null;
        if (glow != null) glow.follow = target;
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            if (target != null) transform.position = target.position;
            yield return null;
        }
        Vector3 at = transform.position;
        body.color = before;
        bool boss = target != null && target.CompareTag("boss");
        bool killed = false;
        if (target != null)
        {
            Specials.Damage(target.gameObject, dmg * (boss ? 1.5f : 1f), Vector3.zero, 0f);
            killed = target.TryGetComponent(out EnermyController e) && e.IsDead;
        }
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(at, 5f, ghostHits)))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss")) || (target != null && c.transform == target)) continue;
            Specials.Damage(c.gameObject, dmg * 0.5f, (c.transform.position - at).normalized, 3f);
            AddChill(c.gameObject, 3);
        }
        Fx.Spawn("fx_soulburst", at, 10f, Wraith, 16f);
        Fx.Spawn("fx_shock", at, 10f, Chill, 18f);
        ShockRing.Spawn(at, 0.5f, 5f, 0.4f, Chill, 0.25f);
        Hostile.Shake(0.25f);
        Play("boom", 0.8f, 0.8f);
        Play("shatter", 0.7f, 0.7f);
        if (card[3] > 0) StartCoroutine(PossessMist(at, 1f + card[3]));
        SignatureSkills.Possessed(at, killed);
        possessing = false;
    }

    // 빙의 여운 (카드): 냉기 안개
    IEnumerator PossessMist(Vector3 at, float seconds)
    {
        FxAnim a = Fx.Play("fx_cloud", at, 8f, new Color(0.8f, 0.95f, 1f, 0.5f), 8f, 0f, 4, true, seconds);
        for (float t = 0f; t < seconds; t += 0.5f)
        {
            yield return new WaitForSeconds(0.5f);
            foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(at, 3.5f, ghostHits)))
                if (c != null && (c.CompareTag("enermy") || c.CompareTag("boss"))) AddChill(c.gameObject, 1);
        }
        if (a != null) Destroy(a.gameObject);
    }

    void OnDestroyGhost()
    {
        foreach (FxAnim a in wispFx) if (a != null) Destroy(a.gameObject);
    }
}
