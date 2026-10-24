using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 검사 고유 스킬 · 진화
//   s.riposte 역습 · s.trail 검의 궤적 · s.bloodguard 혈갑 · s.whirl 회전 가속 · s.will 강철 의지
//   s.dance 검무 · s.crack 균열 · s.giant 거인 사냥꾼 · s.echo 잔향 베기 · s.soul 영혼 흡수
public partial class SignatureSkills
{
    static readonly Color Steel = new Color(0.75f, 0.88f, 1f);
    static readonly Color Blood = new Color(1f, 0.3f, 0.35f);

    float lastHurt = -99f;
    float shield;
    float rateUntil;
    float rateMul = 1f;
    int danceStacks;
    float danceUntil;
    int souls;
    bool counterReady;
    readonly Dictionary<GameObject, int> crackHits = new Dictionary<GameObject, int>();

    // ---------------- 게임 코드가 부르는 검사 훅
    // 베기 직전: 이번 베기 피해 배율 (영혼 흡수) · 반격 검기
    public static float BeforeSwing(Vector3 origin, Vector2 dir) => Instance != null && Instance.who == CharacterId.Swordsman ? Instance.SwordBefore(origin, dir) : 1f;
    public static void SwingHit(Collider2D c) { if (Instance != null && Instance.who == CharacterId.Swordsman && c != null) Instance.OnSwingHit(c); }
    public static void AfterSwing(Vector3 origin, Vector2 dir, float reach, int hits) { if (Instance != null && Instance.who == CharacterId.Swordsman) Instance.OnAfterSwing(origin, dir, reach, hits); }
    public static void Spin(Vector3 origin, float radius, float dmg, float charge) { if (Instance != null && Instance.who == CharacterId.Swordsman) Instance.OnSpin(origin, radius, dmg, charge); }
    public static void Parried() { if (Instance != null && Instance.E("se.counter")) Instance.counterReady = true; }
    // 광검무: 검무가 최대일 때 모든 베기가 강한 일격
    public static bool ForceCombo => Instance != null && Instance.E("se.dance") && Instance.danceStacks >= 5 && Time.time < Instance.danceUntil;

    // ---------------- 2026-10-19~ 검사 갈래 (Paths/Paths.Swordsman.cs) 가 묻고 부르는 것
    // 검무 최대 중첩 (검무 공명 II)
    public static bool DanceFull => Instance != null && Instance.who == CharacterId.Swordsman && Instance.L("s.dance") > 0 && Instance.danceStacks >= 5 && Time.time < Instance.danceUntil;
    // 혈갑 보호막이 가득 (피의 맹세 공명 II · 보호막 카드가 없으면 늘 false)
    public static bool BloodShieldFull => Instance != null && Instance.who == CharacterId.Swordsman && Instance.L("s.bloodguard") > 0 && Instance.player != null && Instance.shield >= Instance.ShieldCap * 0.98f;
    // 받아치기 막기 성공: 역습(맞은 뒤 1.5초 피해 증가)을 켜고, 완벽한 반격 진화면 반격 검기도 (맞은 것처럼 · 피해는 없음)
    public static void PathParried()
    {
        if (Instance == null || Instance.who != CharacterId.Swordsman) return;
        Instance.lastHurt = Time.time;
        if (Instance.E("se.counter")) Instance.counterReady = true;
    }
    // 검기 공명 I: 검기로 같은 적을 세 번 맞히면 균열이 바로 터짐 (균열 카드가 없으면 1단계 세기) · power = 갈래 효과 배율
    public static void CrackNow(GameObject g, float power)
    {
        if (Instance == null || Instance.who != CharacterId.Swordsman || g == null || !Instance.Alive) return;
        Instance.CrackBurst(g, Mathf.Max(1, Instance.L("s.crack")), power);
    }

    void CrackBurst(GameObject g, int cr, float power)
    {
        float dmg = Atk * V(cr, 0.8f, 0.4f) * P("s.crack") * power;
        Vector3 p = g.transform.position;
        using var tier = EvoFx("s.crack");
        Deal(g, dmg, Vector3.zero, 0f);
        if (E("se.giant")) Circle(p, 2.6f, dmg * 0.6f, 0.5f);
        Fx.Spawn("fx_fissure", p, 2.4f * Fs("s.crack"), Color.white, 14f, Random.Range(0f, 360f), 14);
        Fx.Spawn("fx_shock", p, 3f * Fs("s.crack"), new Color(0.7f, 0.85f, 1f, 0.9f), 24f);
        Flair("s.crack", p, 2.6f, new Color(0.7f, 0.85f, 1f));
        Hostile.Play("crack", 0.45f, 1.3f);
    }

    float SwordBefore(Vector3 origin, Vector2 dir)
    {
        float mul = 1f;
        int s = L("s.soul");
        if (s > 0 && souls > 0)
        {
            mul += souls * V(s, 0.03f, 0.01f);     // 1.0.1 하향: 영혼 하나당 4 ~ 10% → 3 ~ 6%, 최대 10 → 8 (한 번에 +100% → +48%)
            souls = 0;
        }
        if (counterReady)
        {
            counterReady = false;
            using var tier = Fx.Tier(3);
            Bullet b = Shot(origin + (Vector3)(dir * 0.8f), dir, Atk * 3f * mul, 999, 30f, 18f, "fx_swordwave", 7f, new Color(1f, 0.9f, 0.6f));      // 1.1.2~ 250 → 300%
            Flair(null, origin, 4f, new Color(1f, 0.9f, 0.6f));
            if (b != null) b.hitOnce = new HashSet<int>();
            Fx.Spawn("fx_shock", origin, 5f, new Color(1f, 0.85f, 0.45f, 0.9f), 22f);
            Hostile.Play("crack", 0.8f, 0.8f);
            Hostile.Shake(0.15f);
        }
        return mul;
    }

    void OnSwingHit(Collider2D c)
    {
        GameObject g = c.gameObject;
        // 혈갑: 벤 적마다 보호막
        // 1.0.1 너프: 벤 적마다 0.6 ~ 1.5% (예전 1.2 ~ 3%), 한 번 휘두를 때 최대 3마리, 보호막이 맞은 뒤 1.5초는 쌓이지 않음
        // (몰린 적을 베는 동안 맞는 족족 다시 가득 차서 검사가 절대 죽지 않았음)
        int bg = L("s.bloodguard");
        if (bg > 0 && bloodHits < 3 && Time.time >= shieldPauseUntil)
        {
            bloodHits++;
            shield = Mathf.Min(ShieldCap, shield + player.PlayerMaxHealth * 0.003f * (1 + bg));
        }
        // 검무: 벨 때마다 이동 속도 (한 번 휘두를 때 한 중첩)
        if (L("s.dance") > 0 && !danceCounted)
        {
            danceCounted = true;
            danceStacks = Time.time < danceUntil ? Mathf.Min(5, danceStacks + 1) : 1;
            danceUntil = Time.time + 1.5f;
        }
        // 균열: 같은 적을 세 번 (거인 처단: 보스는 두 번)
        int cr = L("s.crack");
        if (cr > 0)
        {
            crackHits.TryGetValue(g, out int n);
            n++;
            int need = E("se.giant") && IsBoss(g) ? 2 : 3;
            if (n >= need)
            {
                n = 0;
                float dmg = Atk * V(cr, 0.8f, 0.4f) * P("s.crack");     // 1.1.2~ 거인 처단: ×1.5
                Vector3 p = c.transform.position;
                using var tier = EvoFx("s.crack");
                Deal(g, dmg, Vector3.zero, 0f);
                if (E("se.giant")) Circle(p, 2.6f, dmg * 0.6f, 0.5f);
                Fx.Spawn("fx_fissure", p, 2.4f * Fs("s.crack"), Color.white, 14f, Random.Range(0f, 360f), 14);
                Fx.Spawn("fx_shock", p, 3f * Fs("s.crack"), new Color(1f, 0.8f, 0.5f, 0.9f), 24f);
                Flair("s.crack", p, 2.6f, new Color(1f, 0.8f, 0.5f));
                Hostile.Play("crack", 0.45f, 1.3f);
            }
            if (g != null) crackHits[g] = n;
            if (crackHits.Count > 200) crackHits.Clear();
        }
    }

    bool danceCounted;
    int bloodHits;              // 이번 휘두르기에 혈갑을 쌓은 적 수
    float shieldPauseUntil;     // 보호막이 맞은 뒤 다시 쌓이기 시작하는 때
    // 1.0.1: 최대 6 ~ 15% (예전 10 ~ 25%), 피의 성채 1.5배 (예전 2배)
    float ShieldCap => player.PlayerMaxHealth * V(L("s.bloodguard"), 0.06f, 0.03f) * (E("se.blood") ? 1.5f : 1f);

    void OnAfterSwing(Vector3 origin, Vector2 dir, float reach, int hits)
    {
        danceCounted = false;
        bloodHits = 0;
        int tr = L("s.trail");
        if (tr <= 0) return;
        Vector3 at = origin + (Vector3)(dir * reach * 0.55f);
        float r = reach * 0.55f;
        float dmg = Atk * V(tr, 0.08f, 0.03f) * P("s.trail");         // 휘두를 때마다 겹쳐서 예전 15 ~ 36% 는 너무 셌음 · 1.1.2~ 천검 궤적: ×1.6
        using var tier = EvoFx("s.trail");
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        SigZone z = Zone(at, r, 1f, 0.25f, zone => { foreach (Collider2D c in Inside(zone)) Deal(c.gameObject, dmg, Vector3.zero, 0f); });
        FxAnim a = Fx.Play("fx_swordswing", origin, reach * 2.03f, Big("s.trail") ? Vivid(new Color(0.7f, 0.85f, 1f), 0.75f) : new Color(0.7f, 0.85f, 1f, 0.45f), 6f, rot, 13, true, 1f);
        if (a != null) a.transform.SetParent(z.transform, true);
        if (E("se.trail"))
            z.onEnd = zone =>
            {
                using var evoTier = Fx.Tier(3);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 d = Quaternion.Euler(0f, 0f, i * 60f + rot) * Vector2.right;
                    Shot(zone.transform.position, d, Atk * 0.6f, 3, 22f, 10f, "fx_swordwave", 2.8f, Vivid(new Color(0.7f, 0.85f, 1f)));
                }
                Hostile.Play("whoosh", 0.4f, 1.4f);
            };
    }

    void OnSpin(Vector3 origin, float radius, float dmg, float charge)
    {
        int w = L("s.whirl");
        if (w > 0)
        {
            rateMul = 1f + V(w, 0.2f, 0.1f);
            rateUntil = Time.time + 1.5f + 2.5f * charge;
        }
        int ec = L("s.echo");
        if (ec > 0) StartCoroutine(EchoSpin(origin, radius, dmg * V(ec, 0.4f, 0.15f) * P("s.echo")));       // 1.1.2~ 영혼 회오리: ×1.4
        if (E("se.eye") && Time.time >= eyeOfStormUntil) { eyeOfStormUntil = Time.time + 3f; StartCoroutine(EyeOfStorm(3f)); }     // 1.3.8~ 겹쳐 실행되지 않게 (동시에 하나)
    }

    IEnumerator EchoSpin(Vector3 origin, float radius, float dmg)
    {
        yield return new WaitForSeconds(0.45f);
        if (!Alive) yield break;
        float r = radius * 0.85f;
        // 영혼 회오리: 흡수한 영혼만큼 커지고 강해짐, 쓴 영혼의 절반을 되돌려 줌
        if (E("se.vortex") && souls > 0)
        {
            r *= 1f + 0.06f * souls;
            dmg *= 1f + 0.12f * souls;
            souls = souls / 2;
        }
        Vector3 p = transform.position;
        using var tier = EvoFx("s.echo");
        Circle(p, r, dmg, 2f);
        Fx.Spawn("fx_spinslash", p, r * 2.4f * Fs("s.echo"), Big("s.echo") ? new Color(1f, 0.85f, 0.5f, 0.9f) : new Color(0.8f, 0.9f, 1f, 0.7f), 22f);
        Flair("s.echo", p, r, new Color(0.8f, 0.9f, 1f));
        Hostile.Play("slash", 0.6f, 1.2f);
    }

    float eyeOfStormUntil;
    IEnumerator EyeOfStorm(float seconds)
    {
        FxAnim t;
        using (Fx.Tier(3)) t = Fx.Play("fx_tornado", transform.position, 14f, new Color(1f, 0.9f, 0.6f, 0.6f), 14f, 0f, 3, true, seconds);
        Flair(null, transform.position, 7f, Steel);
        if (t != null) t.follow = transform;
        for (float time = 0f; time < seconds && Alive; time += 0.25f)
        {
            Vector3 p = transform.position;
            foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(p, 7f, hits)))
            {
                if (c == null || !c.CompareTag("enermy")) continue;
                c.transform.position = Vector3.MoveTowards(c.transform.position, p, 1.2f);
            }
            Circle(p, 3f, Atk * 0.5f, 0f);          // 1.1.2~ 40 → 50%
            yield return new WaitForSeconds(0.25f);
        }
    }

    float SwordOutgoing(GameObject target)
    {
        float mul = 1f;
        int r = L("s.riposte");
        if (r > 0 && Time.time - lastHurt < 1.5f) mul *= 1f + V(r, 0.2f, 0.1f);       // 1.0.1 하향: 30 ~ 75% → 20 ~ 50% (근접이라 거의 늘 켜져 있었음)
        int g = L("s.giant");
        if (g > 0 && IsBoss(target)) mul *= 1f + V(g, 0.08f, 0.05f);     // 1.0.1 하향: 12 ~ 36% → 8 ~ 23%
        return mul;
    }

    void SwordHit(GameObject target, float dmg, bool killed, bool proc) { }

    void SwordKilled(Vector3 pos)
    {
        if (L("s.soul") > 0) souls = Mathf.Min(8, souls + 1);
    }

    float SwordTaken(float dmg)
    {
        // 1.4.1~ 갈래 받아치기 막기를 혈갑 보호막보다 먼저 (보호막이 피해를 먹어 막기 · 보상이 빠지거나 보호막을 헛되이 쓰던 것)
        if (dmg > 0f && SwordsmanPaths.ParryFirst(dmg)) return 0f;
        if (shield <= 0f || dmg <= 0f) return dmg;
        float absorbed = Mathf.Min(shield, dmg);
        shield -= absorbed;
        shieldPauseUntil = Time.time + 1.5f;
        if (shield <= 0.01f)
        {
            shield = 0f;
            if (E("se.blood"))
            {
                Vector3 p = transform.position;
                using var tier = Fx.Tier(3);
                Circle(p, 4.5f, Atk * 2.5f, 2.5f);      // 1.1.2~ 200 → 250%
                Fx.Spawn("fx_shock", p, 15f, VisualRules.Kit(Blood, new Color(0.3f, 0.56f, 1f)), 20f);
                Fx.Spawn("fx_bleed", p, 7f, Color.white, 16f);
                Flair(null, p, 6f, VisualRules.Kit(Blood, new Color(0.3f, 0.56f, 1f)));
                Hostile.Play("boom", 0.5f, 1.3f);
            }
        }
        return dmg - absorbed;
    }

    void SwordHurt(float taken)
    {
        lastHurt = Time.time;
        if (E("se.counter")) counterReady = true;
    }

    float SwordMove() => L("s.dance") > 0 && Time.time < danceUntil ? 1f + danceStacks * V(L("s.dance"), 0.04f, 0.02f) : 1f;
    float SwordRate() => Time.time < rateUntil ? rateMul : 1f;

    GameObject shieldGlow;

    void SwordTick()
    {
        // 강철 의지: 회전 베기를 모으는 동안 주변 적을 끌어당김
        int w = L("s.will");
        CharacterKit k = Kit;
        if (w > 0 && k != null && k.Charging)
        {
            Vector3 p = transform.position;
            float r = V(w, 5f, 1f);
            foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(p, r, hits)))
            {
                if (c == null || !c.CompareTag("enermy")) continue;
                if ((c.transform.position - p).sqrMagnitude < 1.4f) continue;
                c.transform.position = Vector3.MoveTowards(c.transform.position, p, 2.5f * Time.deltaTime);
            }
        }
        // 혈갑 보호막이 있으면 몸에 붉은 빛
        bool show = shield > 0.5f;
        if (show && shieldGlow == null && SpecialAbilities.GlowSprite != null)
            shieldGlow = SpecialAbilities.MakeSprite("BloodGuard", SpecialAbilities.GlowSprite, transform.position, 0.3f, VisualRules.Kit(new Color(1f, 0.25f, 0.3f, 0.35f), new Color(0.25f, 0.53f, 1f, 0.35f)), "Effect", 1);
        if (!show && shieldGlow != null) Destroy(shieldGlow);
        if (shieldGlow != null)
        {
            shieldGlow.transform.position = transform.position;
            float k2 = ShieldCap > 0f ? shield / ShieldCap : 0f;
            shieldGlow.transform.localScale = Vector3.one * (0.2f + 0.15f * k2 + 0.02f * Mathf.Sin(Time.time * 6f));
        }
    }
}
