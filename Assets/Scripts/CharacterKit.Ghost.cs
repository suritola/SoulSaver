using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 유령 (1.0.2): 죽지 못한 영혼
//   평타 · 차가운 손길: 앞으로 짧게 뻗는 손이 한 줄의 적을 모두 꿰뚫고 냉기를 쌓음 (다섯 번 쌓이면 1초 얼어붙음)
//   영혼(도깨비불): 적을 쓰러뜨리면 확률로(20%) 영혼이 생겨 몸 둘레를 돎 (최대 4). 맞을 때 영혼이 대신 사라지며 피해를 막음
//   우클릭 · 빙의: 누르고 있으면 마우스 가까운 적에 표시, 떼면 그 적의 몸에 들어가 3초 동안 그 적의 이동 속도로 몸을 끌고 다니다가 안에서 터뜨림
//                 (들어간 동안 무적 · 둘레 적에게도 피해와 냉기) — 1.0.4~: 1초 → 3초, 들어간 동안 조종
// 레벨업 카드 (card): 0 꿰뚫는 냉기 · 1 얼어붙는 손길 · 2 원혼 · 3 빙의 여운 · 5 영혼 수호 · 6 얼음 파편
// 형태: 사슬 망령 · 얼음 유령 · 그림자 유령 / 강화: 깊은 냉기 · 두 손 · 영혼 폭발
public partial class CharacterKit
{
    static readonly Color Chill = new Color(0.6f, 0.9f, 1f, 0.9f);
    static readonly Color Wraith = new Color(0.75f, 0.95f, 1f, 0.85f);

    // 냉기: 적마다 쌓인 수 · 얼어붙은 때까지
    readonly Dictionary<GameObject, int> chill = new Dictionary<GameObject, int>();
    readonly Dictionary<GameObject, float> frozenUntil = new Dictionary<GameObject, float>();
    public int FreezeNeed => Mathf.Max(2, 5 - card[1]);
    // 갈래 산산조각(대가): 얼어붙는 시간 절반
    float FreezeTime => (augment == AugDeepChill ? 1.5f : 1f) * (Paths.Has(GhostPaths.Shatter) ? GhostPaths.ShatterFreezeMul : 1f);
    // 갈래 혹한: 손길 +1칸
    float HandReach => 4.5f + card[0] + (Paths.Has(GhostPaths.Frost) ? GhostPaths.FrostReach : 0f);
    public bool IsFrozen(GameObject g) => g != null && frozenUntil.TryGetValue(g, out float t) && t > Time.time;
    public int ChillOf(GameObject g) => g != null && chill.TryGetValue(g, out int n) ? n : 0;

    // 영혼 (도깨비불)
    public int Wisps { get; private set; }
    public int WispMax => GhostPaths.WispCap(3 + card[5]);     // 갈래 영혼 군단: 공명 I +2 · 극의 10 | 1.3.5: 4 → 3 (카드 4 · 5 · 6) · 1.0.2 조정: 6 + 2 → 4 + 1
    readonly List<FxAnim> wispFx = new List<FxAnim>();
    static readonly List<Collider2D> ghostHits = new List<Collider2D>(64);

    void GhostAttack(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        dmg *= GhostPaths.HandMul();            // 갈래 대가: 혹한 손길 −25% · 사령술 평타 −20%
        foreach (Vector2 d in Spread(dir, shots, 18f)) Hand(start, d, dmg, true);
        if (augment == AugTwoHands)
        {
            Hand(start, Quaternion.Euler(0f, 0f, 25f) * dir, dmg * 0.7f, false);
            Hand(start, Quaternion.Euler(0f, 0f, -25f) * dir, dmg * 0.7f, false);
        }
        if (form == SpecialAbilities.KitShadowGhost) StartCoroutine(ShadowHand(dir, dmg * 0.5f));
        PlayerMotion.Of(this).Lunge(0.12f, dir);
        Play("whoosh", 0.4f, 0.75f);
        Play("freeze", 0.2f, 1.4f);
    }

    // 1.0.2: 판정을 보이는 그림에 맞춤 — 적의 중심이 아니라 몸(콜라이더) 크기까지, 손끝의 뼈 손 그림 둘레까지,
    // 손이 길어지면(HandReachMul) 찾는 범위도 같이 (예전엔 기본 길이로만 찾아 먼 쪽 적이 빠졌음)
    // 1.1.2 뒤: 판정 = 보이는 손길 — 띠 반폭 = 보이는 띠 굵기의 절반(본 손 1.8 · 그림자 손 1.3칸 × 단계 굵기 BandMul),
    //   손끝 = 보이는 뼈 손 몸통(장식 1.45배로 그린 2 · 1.6칸 그림의 가로 · 세로 반지름 평균 ≈ 0.65 × 그린 크기) × 단계 굵기
    //   (예전엔 단계 · 손과 상관없이 반폭 0.9 · 손끝 1.3 — 2 · 3단계에 굵어진 띠, 그림자 손의 얇은 띠와 어긋났음)
    const float HandHalfWidth = 0.9f, HandTipRadius = 1.3f, ShadowHalfWidth = 0.65f, ShadowTipRadius = 1.05f;
    static float BodyRadius(Collider2D c) => Mathf.Max(c.bounds.extents.x, c.bounds.extents.y) * 0.8f;
    static bool HandTouches(Collider2D c, Vector3 start, Vector3 end, float halfWidth, float tipRadius)
    {
        float r = BodyRadius(c);
        Vector3 p = c.bounds.center;
        return Hostile.DistanceToSegment(p, start, end) <= halfWidth + r || (p - end).magnitude <= tipRadius + r;
    }

    // 손 하나: 한 줄의 적을 모두
    void Hand(Vector3 start, Vector2 dir, float dmg, bool main)
    {
        Vector3 end = start + (Vector3)(dir * HandReach * SignatureSkills.HandReachMul());
        Vector3 mid = (start + end) * 0.5f;
        bool first = true;
        ghostOnce.Clear();
        float band = AttackLook.BandMul(AttackLook.Tier);
        float halfW = (main ? HandHalfWidth : ShadowHalfWidth) * band, tipR = (main ? HandTipRadius : ShadowTipRadius) * band;
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(mid, (end - start).magnitude * 0.5f + tipR + 1.5f, ghostHits)))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            if (!HandTouches(c, start, end, halfW, tipR)) continue;
            GameObject g = c.gameObject;
            if (!ghostOnce.Add(g)) continue;        // 콜라이더가 여럿인 적도 손 하나에 한 번만
            // 갈래 산산조각: 얼어붙은 적을 손길로 치면 부서짐 (잡몹 즉사 · 큰 적 체력 비례 · 보스는 균열로 따로)
            if (IsFrozen(g)) GhostPaths.TryShatter(this, g, dir);
            // 빈틈: 커서 쪽으로 뻗은 주 손길만 (그림자 손 · 영혼 사슬 번개는 제외)
            if (main) WeakHit.Damage(g, WeakShape.Segment(start, end, halfW, 1.75f), dmg * formPower, dir, 0.6f);
            else Specials.Damage(g, dmg * formPower, dir, 0.6f);
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
        // 1.1.2: 손길을 한눈에 — 판정 폭(반폭 0.9 = 폭 1.8 + 적 몸)까지 굵게 (예전 1칸), 흰 심 줄 · 뼈 손 2배 가까이, 적 · 맵 물체 위 순서
        Fx.Beam(start, end, (main ? 1.8f : 1.3f) * band, SkinFx.Tint(new Color(0.4f, 0.8f, 1f, 0.85f)), 0.2f, AttackLook.AreaOrder - 1);
        Fx.Beam(start, end, (main ? 0.55f : 0.4f) * band, new Color(1f, 1f, 1f, 0.95f), 0.14f, AttackLook.AreaOrder);
        Fx.Spawn("fx_bonehand", end, (main ? 2f : 1.6f) * band, Wraith, 22f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f, AttackLook.AreaOrder + 1);
        if (SignatureSkills.HandTrail()) StartCoroutine(FrostTrail(start, end));
    }

    IEnumerator ShadowHand(Vector2 dir, float dmg)
    {
        yield return new WaitForSeconds(0.15f);
        Hand(player.MuzzlePosition, dir, dmg, false);
    }

    // 장식 이펙트(fx_cloud · fx_soulburst 등)를 화면에서 visible 크기로 보이게 하는 Fx 크기
    // (Fx.cs: 장식은 DecorScale 1.45배, 스킬 출처면 SkillDecor 1.4배 더 — 판정 범위와 보이는 크기를 맞추려고)
    static float DecorSize(float visible)
    {
        string s = DamageSource.Current;
        bool skill = s != null && s != DamageSource.Basic && s != DamageSource.Other;
        return visible / (1.45f * (skill ? 1.4f : 1f));
    }

    // 눈보라 (진화): 손이 지난 자리에 1.5초 냉기 안개
    // 1.0.2: 예전엔 손 길이만 한 동그란 구름(지름 ≈ 길이 × 1.45)이 한 번 재생되고 사라져, 보이는 범위 · 시간과 판정(폭 1칸 · 1.5초)이 달랐음
    //        → 손이 지난 띠 모양으로 늘인 안개가 1.5초 동안 그대로 남고, 그 띠(반폭 1.2칸 + 적 몸 크기)에 닿는 적에게 냉기
    const float TrailHalfWidth = 1.2f;
    IEnumerator FrostTrail(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        float life = 1.5f * GhostPaths.MistMul;         // 갈래 혹한 극의: 냉기 안개 2배
        FxAnim cloud = Fx.Play("fx_cloud", (a + b) * 0.5f, DecorSize(TrailHalfWidth * 2f), new Color(0.8f, 0.95f, 1f, 0.55f), 10f,
                               Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 10, true, life);
        if (cloud != null && cloud.sr != null && cloud.sr.sprite != null)
        {
            float unit = cloud.sr.sprite.bounds.size.x;
            Vector3 s = cloud.transform.localScale;
            if (unit > 0f) cloud.transform.localScale = new Vector3((d.magnitude + TrailHalfWidth * 2f) / unit, s.y, 1f);
        }
        for (float t = 0f; t < life; t += 0.5f)
        {
            yield return new WaitForSeconds(0.5f);
            ghostOnce.Clear();
            foreach (Collider2D c in new List<Collider2D>(Specials.Overlap((a + b) * 0.5f, d.magnitude * 0.5f + TrailHalfWidth + 1f, ghostHits)))
                if (c != null && (c.CompareTag("enermy") || c.CompareTag("boss")) && Hostile.DistanceToSegment(c.bounds.center, a, b) <= TrailHalfWidth + BodyRadius(c)
                    && ghostOnce.Add(c.gameObject))
                    AddChill(c.gameObject, 1);
        }
    }
    static readonly HashSet<GameObject> ghostOnce = new HashSet<GameObject>();

    // 냉기 쌓기 → 다 차면 얼어붙음
    //   frostDouble: false 면 혹한 2배를 받지 않음 (혹한 공명 I 오라 — 언 무리가 서로를 계속 다시 얼리지 않게)
    public void AddChill(GameObject g, int n, bool frostDouble = true)
    {
        if (g == null || n <= 0) return;
        if (frostDouble && Paths.Has(GhostPaths.Frost)) n *= 2;            // 갈래 혹한: 냉기가 두 배로 쌓임
        GhostPaths.CrackChill(this, g, n);                  // 갈래 산산조각: 보스 냉기 5중첩마다 균열 (얼어 있는 동안도 셈)
        if (IsFrozen(g)) return;
        chill.TryGetValue(g, out int have);
        have += n;
        if (g.TryGetComponent(out EnermyController e)) e.Slow(1f - 0.1f * Mathf.Min(have, 5), 1.5f);
        if (have >= FreezeNeed)
        {
            have = 0;
            frozenUntil[g] = Time.time + FreezeTime;
            if (e != null) e.Slow(0f, FreezeTime);
            Fx.Spawn("fx_sparkle", g.transform.position, 1.6f, Chill, 20f);
            Play("freeze", 0.5f, 1f);
            RunCounters.Both("freeze");             // 1.2~ 영혼 계약 · 보드
            SignatureSkills.Frozen(g);
            GhostPaths.Froze(g, Time.time + FreezeTime);    // 갈래 혹한 공명 II: 풀릴 때 냉기 파동
        }
        chill[g] = have;
    }

    // ---------------- 갈래 효과가 쓰는 창구 (Paths/Paths.Ghost.cs)
    // 얼음을 품 (산산조각으로 부순 뒤 같은 얼음으로 두 번 부서지지 않게)
    public void Thaw(GameObject g) { if (g != null) frozenUntil.Remove(g); }

    // 지금 얼어붙은 적 (최대 max 개 · into 를 비우고 채움)
    public int FrozenNow(List<GameObject> into, int max)
    {
        into.Clear();
        float now = Time.time;
        foreach (var kv in frozenUntil)
        {
            if (into.Count >= max) break;
            if (kv.Key != null && kv.Value > now && kv.Key.activeInHierarchy) into.Add(kv.Key);
        }
        return into.Count;
    }

    // 얼음 파편 n 개를 사방으로 (피해 = 평타 50% × mul)
    public void IceShards(Vector3 pos, int n, float mul)
    {
        n = Mathf.Clamp(n, 0, 12);
        for (int i = 0; i < n; i++)
        {
            Vector2 d = Quaternion.Euler(0f, 0f, 360f / n * i + 22.5f) * Vector2.right;
            Projectile(pos, d, Damage * 0.5f * mul, 1, 22f, 6f, "fx_spike", 0.6f, Chill, false);
        }
    }

    public static float BodyRadiusOf(Collider2D c) => c != null ? BodyRadius(c) : 0f;

    // 처치: 영혼이 생김 · 원혼 · 얼음 파편
    void GhostKill(Vector3 pos)
    {
        if (Id != CharacterId.Ghost) return;
        float chance = (0.1f + SignatureSkills.WispChanceBonus()) * (Paths.Has(GhostPaths.Legion) ? 2f : 1f);       // 갈래 영혼 군단: 2배 | 1.3.5: 20% → 10% (유령이 너무 안 죽음) · 1.0.2 조정: 35% → 20%
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

    // 1.0.2: 쓰러진 그 적만 (예전엔 쓰러진 자리 1칸 안의 살아 있는 얼어붙은 적으로도 터졌고, 같은 적이 여러 번 터뜨릴 수 있었음)
    bool RecentlyFrozenNear(Vector3 pos)
    {
        GameObject hit = null;
        foreach (var kv in frozenUntil)
            if (kv.Key != null && kv.Value > Time.time - 0.2f && (kv.Key.transform.position - pos).sqrMagnitude < 1f
                && kv.Key.TryGetComponent(out EnermyController e) && e.IsDead) { hit = kv.Key; break; }
        if (hit == null) return false;
        frozenUntil.Remove(hit);
        return true;
    }

    float pruneChillAt;
    void PruneChill()
    {
        if (Time.time < pruneChillAt) return;
        pruneChillAt = Time.time + 3f;
        List<GameObject> dead = new List<GameObject>();
        foreach (var kv in chill) if (kv.Key == null || (kv.Key.TryGetComponent(out EnermyController e) && e.IsDead)) dead.Add(kv.Key);
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
        if (GhostPaths.WispBlockFails()) return false;      // 갈래 영혼 군단(대가): 막을 확률 50% (못 막으면 영혼은 그대로)
        Wisps--;
        Vector3 p = transform.position;
        Fx.Spawn("fx_soulburst", p, 3f, Wraith, 18f);
        Play("wail", 0.5f, 1.3f);
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
        DrawChillMarks();
        PruneChill();
        while (wispFx.Count < Wisps)
        {
            FxAnim a = Fx.Play("fx_orb", transform.position, 0.8f, Wraith, 10f, 0f, 21, true, -1f);     // 1.0.2: 0.6 → 0.8 (잘 보이게)
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
            Vector3 orbit = transform.position + Quaternion.Euler(0f, 0f, ang) * Vector3.right * r + Vector3.up * 0.3f;
            // 갈래 영혼 군단: 돌진 중인 영혼은 그 적까지 갔다가 돌아옴 (그림만 · 피해는 GhostPaths 가 닿는 순간에)
            if (i < WispDashSlots && wispDashTo[i] != null && Time.time - wispDashAt[i] < WispDashTime)
            {
                float k = (Time.time - wispDashAt[i]) / WispDashTime;
                orbit = Vector3.Lerp(orbit, wispDashTo[i].position, Mathf.Sin(k * Mathf.PI));
            }
            else if (i < WispDashSlots) wispDashTo[i] = null;
            wispFx[i].transform.position = orbit;
        }
    }

    // 영혼 돌진 그림 (갈래 영혼 군단): 몇 번째 영혼이 어느 적에게 · 언제
    public const float WispDashTime = 0.36f;
    const int WispDashSlots = 16;
    readonly Transform[] wispDashTo = new Transform[WispDashSlots];
    readonly float[] wispDashAt = new float[WispDashSlots];

    public void WispDash(int i, Transform to)
    {
        if (i < 0 || i >= WispDashSlots || to == null) return;
        wispDashTo[i] = to;
        wispDashAt[i] = Time.time;
    }

    // 냉기 표시 (1.0.2): 냉기가 쌓인 적 몸 둘레에 고리 — 쌓인 만큼(얼어붙기까지 필요한 수 중) 차오르고, 얼어붙으면 꽉 찬 고리가 깜빡
    readonly Dictionary<GameObject, LineRenderer> chillMarks = new Dictionary<GameObject, LineRenderer>();
    static readonly HashSet<GameObject> chillShown = new HashSet<GameObject>();
    static readonly List<GameObject> chillStale = new List<GameObject>();
    readonly Dictionary<LineRenderer, Vector3> chillShape = new Dictionary<LineRenderer, Vector3>();     // 1.2.4: 마지막으로 그린 고리 (반지름 · 차오름 · 얼어붙음)

    void DrawChillMarks()
    {
        chillShown.Clear();
        foreach (var kv in frozenUntil) if (kv.Value > Time.time) ShowChill(kv.Key, 1f, true);
        foreach (var kv in chill) if (kv.Value > 0) ShowChill(kv.Key, Mathf.Clamp01(kv.Value / (float)FreezeNeed), false);
        chillStale.Clear();
        foreach (var kv in chillMarks) if (kv.Key == null || !chillShown.Contains(kv.Key)) chillStale.Add(kv.Key);
        foreach (GameObject g in chillStale)
        {
            if (chillMarks.TryGetValue(g, out LineRenderer lr) && lr != null) { chillShape.Remove(lr); Destroy(lr.gameObject); }
            chillMarks.Remove(g);
        }
    }

    void ShowChill(GameObject g, float k, bool frozen)
    {
        if (g == null || !g.activeInHierarchy || chillShown.Contains(g)) return;
        if (g.TryGetComponent(out EnermyController e) && e.IsDead) return;
        Vector3 center = g.transform.position;
        float r = 0.8f;
        if (g.TryGetComponent(out Collider2D col) && col.enabled) { center = col.bounds.center; r = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y) + 0.25f; }
        if (!chillMarks.TryGetValue(g, out LineRenderer lr) || lr == null)
        {
            lr = Hostile.NewLine("ChillMark", Chill, 0.12f, 30);
            lr.useWorldSpace = false;      // 1.2.4: 고리는 선 자기 자리 기준으로 그려 두고 매 프레임 선 위치만 옮김 (보이는 좌표는 같음)
            chillMarks[g] = lr;
        }
        chillShown.Add(g);
        lr.transform.position = center;
        // 1.2.4: 적마다 매 프레임 61점을 다시 넣던 것 → 반지름 · 차오름 · 얼어붙음이 바뀔 때만 다시 그림
        Vector3 shape = new Vector3(r, frozen ? 1f : k, frozen ? 1f : 0f);
        bool redraw = !chillShape.TryGetValue(lr, out Vector3 old) || old.x != shape.x || old.y != shape.y || old.z != shape.z;
        if (redraw) chillShape[lr] = shape;
        if (frozen)
        {
            float pulse = 0.6f + 0.35f * Mathf.Sin(Time.time * 12f);
            lr.loop = true;
            if (redraw) Hostile.SetArc(lr, Vector3.zero, r, 0f, 360f);
            lr.startColor = lr.endColor = new Color(0.85f, 0.97f, 1f, pulse);
            lr.startWidth = lr.endWidth = 0.18f;
        }
        else
        {
            lr.loop = false;
            if (redraw) Hostile.SetArc(lr, Vector3.zero, r, 90f, 90f - 360f * k);
            lr.startColor = lr.endColor = new Color(Chill.r, Chill.g, Chill.b, 0.9f);
            lr.startWidth = lr.endWidth = 0.12f;
        }
    }

    // ================================================================= 우클릭: 빙의
    bool possessAiming;
    // 2026-10-19~ 빙의 상태는 「끝나는 시각」 하나로 (사령술이 빙의를 늘림 · 기획서 6-3 (1) 5)
    //   루틴이 끊겨도(예외 · 코루틴 중단 · 비활성) 그 시각이 지나면 저절로 풀림 → 켜고 끄는 bool 이 남지 않음
    float possessUntil;
    bool possessing => Time.time < possessUntil;
    Transform possessTarget;
    LineRenderer possessRange;          // 빙의 미리 보기: 터질 범위 (반지름 PossessR)
    const float PossessRadius = 5f;
    float PossessR => PossessRadius * GhostPaths.BlastMul;     // 갈래 사령술 공명 I: 1.5배
    // 1.0.4~: 빙의 지속 1초 → 3초. 들어간 동안 내가 움직이면 그 적의 몸이 따라오고, 이동 속도는 그 적의 속도(난이도 · 장 배율이 붙은 값) 그대로
    //         너무 느리거나 빠른 적에서 조작이 망가지지 않게 내 평소 이동 속도의 30% ~ 130% 사이로만 (평소 21쯤이면 6.3 ~ 27.3 · 잡몹 6 ~ 16은 거의 그대로, 보스 5 ~ 7은 6.3 ~ 7)
    //         보스 · 적이 스킬을 쓰는 동안(casting) · 움직이지 않는 것(보스가 만든 등불 · 묘비)은 예전처럼 그 몸에 붙어 따라감 (스킬 동작이 깨지지 않게)
    const float PossessTime = 3f, PossessEnter = 0.15f, PossessGrace = 0.3f;      // 1.3.5: 빙의 뒤 무적 0.65 → 0.3초
    const float PossessSpeedMin = 0.3f, PossessSpeedMax = 1.3f;
    Color possessBodyColor = Color.white;
    bool bodyFaded;                     // 빙의로 몸을 흐리게 해 둠 (빙의가 끝나면 UpdatePossess · 루틴 끝에서 되돌림)
    void ResetPossess()
    {
        possessUntil = 0f;
        possessSpeed = -1f;
        RestoreBody();
    }
    void RestoreBody()
    {
        if (!bodyFaded) return;
        bodyFaded = false;
        if (body != null) body.color = new Color(possessBodyColor.r, possessBodyColor.g, possessBodyColor.b, possessBodyColor.a < 0.5f ? 1f : possessBodyColor.a);
    }
    // 0 이상이면 빙의한 적의 이동 속도로 움직이는 중 (CharacterKit.MoveMul 이 씀)
    // 2026-10-19~ 「끝나는 시각」 방식: 루틴이 매 프레임 다시 넣고, 끊기면 0.25초 뒤 저절로 -1 (예전엔 루틴 끝에서야 되돌림)
    float possessSpeedValue = -1f, possessSpeedUntil;
    float possessSpeed
    {
        get => Time.time < possessSpeedUntil ? possessSpeedValue : -1f;
        set { possessSpeedValue = value; possessSpeedUntil = value >= 0f ? Time.time + 0.25f : 0f; }
    }

    // 플레이어 이동 배율 (MoveMul): 내 속도(player.speed × 고유 스킬 배율)에 곱해 빙의한 적의 속도가 되게
    float PossessMoveMul()
    {
        float own = player != null ? player.speed * SignatureSkills.MoveMul : 0f;
        if (own <= 0.01f) return 1f;
        return Mathf.Clamp(possessSpeed, own * PossessSpeedMin, own * PossessSpeedMax) / own;
    }

    // 1.0.2: 적의 중심이 아니라 몸 가장자리까지의 거리로 고름 (큰 적 · 보스는 몸 위에 마우스를 올려도 안 잡히던 것)
    Transform PossessCandidate()
    {
        Vector3 m = ClampRange(transform.position, Mouse, 14f);
        Transform best = null;
        float bd = 3f;
        foreach (Collider2D c in Specials.Overlap(m, 3f, ghostHits))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            if (c.TryGetComponent(out EnermyController e) && e.IsDead) continue;
            float d = Mathf.Max(0f, (c.bounds.center - m).magnitude - BodyRadius(c));
            if (d < bd) { bd = d; best = c.transform; }
        }
        return best;
    }

    static float TargetRing(Transform t) => t != null && t.TryGetComponent(out Collider2D c) ? Mathf.Max(c.bounds.extents.x, c.bounds.extents.y) + 0.45f : 1.4f;

    void HidePossessAim()
    {
        possessAiming = false;
        if (ultAimRing != null) ultAimRing.enabled = false;
        if (possessRange != null) possessRange.enabled = false;
    }

    void UpdatePossess(SkillGauge gauge, bool full)
    {
        DrawWisps();
        // 1.3.8~ 빙의 루틴이 끊기면(예외 · 코루틴 중단) 이동 속도 · 반투명 · 빙의 상태가 남던 것 → 정해진 시간 + 여유가 지나면 되돌림
        // 2026-10-19~ 빙의 상태 · 이동 속도가 「끝나는 시각」 방식이 되어 저절로 풀림 (끝 시각 = 이번 빙의 길이 + 1.5초, 사령술 4.5초도 같이 늘어남)
        //   여기서는 흐려 둔 몸만 되돌림
        if (!possessing && bodyFaded) RestoreBody();
        if (possessing) { if (possessAiming) HidePossessAim(); return; }
        if (possessAiming && Time.timeScale == 0f) { HidePossessAim(); return; }       // 1.0.2: 멈춤 중엔 조준을 거둠 (멈춘 동안 떼면 풀린 뒤에 빙의하던 것)
        if (!possessAiming && GameInput.UltDown && full && Time.timeScale > 0f) { possessAiming = true; Play("hum", 0.4f, 0.6f); }
        if (!possessAiming) return;
        possessTarget = PossessCandidate();
        if (ultAimRing == null) { ultAimRing = Hostile.NewLine("UltAimRing", Color.white, 0.12f, 25); ultAimRing.loop = true; }
        if (possessRange == null) { possessRange = Hostile.NewLine("PossessRange", Color.white, 0.08f, 24); possessRange.loop = true; }
        ultAimRing.enabled = true;
        ultAimRing.loop = true;
        float pulse = 0.55f + 0.35f * Mathf.Sin(Time.time * 14f);
        if (possessTarget != null)
        {
            // 들어갈 적 (몸 크기에 맞춘 고리) + 터질 범위 (반지름 5칸 · 이 안에 몸이 닿는 적이 피해와 냉기)
            Hostile.SetArc(ultAimRing, possessTarget.position, TargetRing(possessTarget), 0f, 360f);
            ultAimRing.startColor = ultAimRing.endColor = new Color(Chill.r, Chill.g, Chill.b, pulse);
            possessRange.enabled = true;
            Hostile.SetArc(possessRange, possessTarget.position, PossessR, 0f, 360f);
            possessRange.startColor = possessRange.endColor = new Color(Chill.r, Chill.g, Chill.b, 0.35f);
            possessRange.startWidth = possessRange.endWidth = 0.08f;
        }
        else
        {
            Hostile.SetArc(ultAimRing, ClampRange(transform.position, Mouse, 14f), 3f, 0f, 360f);
            ultAimRing.startColor = ultAimRing.endColor = new Color(0.6f, 0.6f, 0.7f, 0.35f);       // 빙의할 적이 없음
            possessRange.enabled = false;
        }
        ultAimRing.startWidth = ultAimRing.endWidth = 0.14f;

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        HidePossessAim();
        if (possessTarget == null) { Play("buzz", 0.4f); return; }          // 적이 없으면 게이지를 쓰지 않음
        StartCoroutine(PossessRoutine(possessTarget, Damage * 6f * UltMul, false));
        // 1.0.4~: 무기 필살기 추가 발동은 터지는 순간에 터진 자리로 (PossessRoutine 끝 · 예전엔 1.2초 뒤 들어간 자리)
        Spend(gauge);
        // 갈래 사령술: 빙의 게이지가 25% 덜 듦 (쓴 뒤 최대치의 25% 를 돌려줌)
        if (gauge != null && Paths.Has(GhostPaths.Necro)) gauge.AddSkillPoint(gauge.MaxSkillPoint * GhostPaths.NecroRefund);
    }

    // chained: 사령술 극의 「터진 뒤 바로 다시 빙의」 (게이지를 쓰지 않음 · 한 번만 · 더 짧고 무적도 짧음)
    IEnumerator PossessRoutine(Transform target, float dmg, bool chained)
    {
        // 이번 빙의 길이 · 무적 길이 (사령술 4.5초 · 무적은 처음 2초만 — 대가)
        float len = GhostPaths.PossessLength(PossessTime, chained);
        float safe = GhostPaths.PossessSafeLength(len, chained);
        // 상태 감시: 이번 빙의가 정상으로 끝날 시각 + 1.5초 여유 (빙의를 늘리면 감시 시각도 같이 늘어남)
        possessUntil = Time.time + PossessEnter + len + 1.5f;
        if (!bodyFaded) possessBodyColor = body.color;
        // 1.0.4~: 무적을 한 번에 1.8초 주던 것 → 들어가는 동안 + 들어가 있는 동안 계속 이어 주고, 터진 뒤 0.3초
        // 2026-10-19~ 무적은 지금 끝 시각에 Max 로만 합침 · 한 번에 최대 1.5초(Paths.InvulnCap) · 쓰러지는 중엔 주지 않음
        //   Paths.Invuln 은 Hostile.Player 를 거치므로, 갈래 아닌 판 빙의 무적까지 그 값에 묶이지 않게 내 player 로 직접 (PossessInvuln)
        PossessInvuln(PossessEnter + PossessGrace);
        Color before = possessBodyColor;
        Vector3 from = transform.position;
        FxAnim glow = null;
        Play("possess", 0.9f, chained ? 1.2f : 1f);
        Play("whoosh", 0.7f, 0.5f);
        try
        {
            for (float t = 0f; t < PossessEnter && target != null; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(from, target.position, t / PossessEnter);
                yield return null;
            }
            body.color = new Color(before.r, before.g, before.b, 0.12f);
            bodyFaded = true;
            glow = target != null ? Fx.Play("fx_soulburst", target.position, 2.6f, Wraith, 12f, 0f, 22, true, len) : null;
            if (glow != null) glow.follow = target;
            // 1.0.4~: 콜라이더가 자식에 있는 적 · 보스도 몸통(루트)을 찾아 끌고 다님
            EnermyController host = target != null ? target.GetComponentInParent<EnermyController>() : null;
            bosss bossHost = target != null && host == null ? target.GetComponentInParent<bosss>() : null;
            Transform carry = host != null ? host.transform : bossHost != null ? bossHost.transform : null;
            bool hostDiedEarly = false, exposed = false;
            hostIgnoreAt = 0f;
            for (float t = 0f; t < len; t += Time.deltaTime)
            {
                // 1.0.2: 들어간 적이 먼저 쓰러지면(다른 공격 · 사라짐) 기다리지 않고 바로 그 자리에서 터짐
                if (target == null || (host != null && host.IsDead) || (bossHost != null && bossHost.IsDead)) { hostDiedEarly = true; break; }
                if (player == null || player.IsDying) yield break;          // 쓰러지면 그만 (터지지 않음 · finally 가 되돌림)
                // 무적: 정한 길이만큼만 (사령술은 처음 2초에서 딱 끊김 · 보통 빙의는 예전처럼 끝난 뒤 0.3초 여유까지)
                if (t < safe) PossessInvuln(safe < len ? Mathf.Min(PossessGrace, safe - t) : PossessGrace);
                else if (!exposed)
                {
                    // 사령술: 무적이 끝남 → 몸이 다시 보임 (맞을 수 있다는 표시)
                    exposed = true;
                    body.color = new Color(before.r, before.g, before.b, 0.55f);
                    Play("hum", 0.35f, 1.6f);
                }
                // 2026-10-19 검토: 무적이 끝나는 빙의(사령술 · 이어진 빙의)는 빙의한 그 몸(보스면 보스 몸통)과 겹쳐 접촉 피해를 계속 받던 것
                //   → 그 몸과의 접촉만 무시 (다른 적의 접촉 · 투사체 · 패턴은 그대로 맞음 = 대가 유지) · 끝나거나 끊기면 finally 에서 되돌림
                //   0.2초마다 다시 걸어 둠 (유체 이탈이 그 사이 같은 쌍을 되돌려도 다시 끔 · 무적이 끝나는 순간 이미 꺼져 있게 처음부터)
                if (safe < len && Time.time >= hostIgnoreAt)
                {
                    hostIgnoreAt = Time.time + 0.2f;
                    IgnoreHostBody(carry != null ? carry : target);
                }
                // 1.0.4~: 내가 움직이고 그 적의 몸이 따라옴 (이동 속도는 그 적의 속도) · 스킬을 쓰는 중이거나 움직이지 않는 것은 그 몸에 붙어 따라감
                bool steer = carry != null && !(host != null ? host.casting : bossHost.casting);
                possessSpeed = steer ? Mathf.Max(0f, host != null ? host.speed : bossHost.speed) : -1f;
                if (steer) { Vector3 d = transform.position - target.position; d.z = 0f; carry.position += d; }
                else transform.position = target.position;
                // 갈래 사령술: 빙의한 몸이 부딪친 적에게 냉기 · 피해
                GhostPaths.PossessBump(this, carry != null ? carry : target);
                yield return null;
            }
            possessSpeed = -1f;
            if (glow != null) { Destroy(glow.gameObject); glow = null; }
            Vector3 at = transform.position;
            RestoreBody();
            if (player == null || player.IsDying) yield break;
            bool boss = target != null && target.CompareTag("boss");
            bool killed = false;
            if (target != null && !hostDiedEarly)
            {
                Specials.Damage(target.gameObject, dmg * (boss ? 1.5f : 1f), Vector3.zero, 0f);
                killed = host != null && host.IsDead;
            }
            float radius = PossessR;
            bool innerFreeze = GhostPaths.InnerFreeze;          // 교차 「안에서 얼리기」: 둘레 적을 얼림
            ghostOnce.Clear();
            foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(at, radius, ghostHits)))
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss")) || (target != null && c.transform == target)) continue;
                if (!ghostOnce.Add(c.gameObject)) continue;
                Specials.Damage(c.gameObject, dmg * 0.5f, (c.transform.position - at).normalized, 3f);
                if (c == null) continue;
                AddChill(c.gameObject, innerFreeze ? FreezeNeed : 3);
                GhostPaths.BlastCurse(c.gameObject);              // 사령술 공명 I: 저주 기본 (보스 제외)
            }
            // 1.0.2: 영혼 폭발 그림이 장식 배율(1.45 × 스킬 1.4)로 지름 20칸쯤 보여 실제 범위(반지름 5칸)의 두 배였음 → 범위에 맞춤
            Fx.Spawn("fx_soulburst", at, DecorSize(radius * 2f), Wraith, 16f);
            Fx.Spawn("fx_shock", at, radius * 2f, Chill, 18f);
            ShockRing.Spawn(at, 0.5f, radius, 0.4f, Chill, 0.25f);
            Hostile.Shake(0.25f);
            Play("boom", 0.8f, 0.8f);
            Play("wail", 0.8f, 0.8f);
            Play("freeze", 0.7f, 0.8f);
            if (card[3] > 0) StartCoroutine(PossessMist(at, 1f + card[3]));
            if (!chained) KitUltAdd(0.05f, at);
            GhostPaths.PossessBlast(this, at, target);          // 사령술 공명 II: 원혼 셋
            // 고유 스킬의 빙의 효과(망령의 행렬 · 저주)는 직접 한 빙의에서만 — 사령술 극의 이어진 빙의는 빼서 두 번 걸리지 않게 (2026-10-19 검토)
            if (!chained) SignatureSkills.Possessed(at, killed);
            possessUntil = 0f;
            // 사령술 극의: 가까운 적에게 한 번 바로 다시 빙의 (이어진 빙의에서는 다시 잇지 않음)
            if (!chained && GhostPaths.ChainPossess)
            {
                Transform next = GhostPaths.ChainTarget(at, target);
                if (next != null) StartCoroutine(PossessRoutine(next, dmg * GhostPaths.ChainDamage, true));
            }
        }
        finally
        {
            // 끝나거나 끊기면(예외 · 쓰러짐) 이동 속도 · 흐린 몸 · 빛을 되돌림 — 코루틴이 멈춰 여기까지 못 와도 「끝나는 시각」이 지나면 저절로 풀림
            //   (빙의 상태 possessUntil 은 건드리지 않음: 정상 끝은 위에서 0 · 이어진 빙의가 방금 새로 넣었을 수 있음 · 끊기면 끝 시각에 저절로)
            possessSpeed = -1f;
            if (glow != null) Destroy(glow.gameObject);
            RestoreBody();
            RestoreHostBody();
        }
    }

    // 빙의 무적: player 를 직접 (Paths.Invuln 과 같은 규칙 — 끝 시각에 Max 로 합침 · 최대 1.5초 · 쓰러지는 중엔 주지 않음)
    void PossessInvuln(float seconds)
    {
        if (player == null || player.IsDying || seconds <= 0f) return;
        player.GrantInvincibility(Mathf.Min(seconds, Paths.InvulnCap));
    }

    // 무적이 끝나는 빙의 동안: 빙의한 몸(자식 콜라이더 포함 · 보스 부품 제외)과 내 몸의 접촉을 끔 — 이미 꺼져 있던 쌍(유체 이탈)은 건드리지 않고, 내가 끈 쌍만 기억해 되돌림
    readonly List<(Collider2D mine, Collider2D theirs)> possessIgnored = new List<(Collider2D, Collider2D)>(8);
    float hostIgnoreAt;
    void IgnoreHostBody(Transform hostBody)
    {
        if (hostBody == null || player == null) return;
        try
        {
            Collider2D[] mine = player.GetComponents<Collider2D>();
            foreach (Collider2D c in hostBody.GetComponentsInChildren<Collider2D>())
            {
                if (c == null || c.GetComponent<BossPart>() != null) continue;
                foreach (Collider2D m in mine)
                {
                    if (m == null || Physics2D.GetIgnoreCollision(m, c)) continue;
                    Physics2D.IgnoreCollision(m, c, true);
                    possessIgnored.Add((m, c));
                }
            }
        }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    void RestoreHostBody()
    {
        for (int i = 0; i < possessIgnored.Count; i++)
        {
            (Collider2D m, Collider2D c) = possessIgnored[i];
            if (m == null || c == null) continue;
            try { Physics2D.IgnoreCollision(m, c, false); }
            catch (System.Exception e) { Debug.LogException(e); }
        }
        possessIgnored.Clear();
    }

    // 빙의 여운 (카드): 냉기 안개
    // 1.0.2: 안개 그림(크기 8 → 장식 배율로 지름 16칸쯤)이 판정(반지름 3.5칸)의 두 배 넘게 보였음 → 판정 크기로 + 테두리 고리
    const float MistRadius = 3.5f;
    IEnumerator PossessMist(Vector3 at, float seconds)
    {
        FxAnim a = Fx.Play("fx_cloud", at, DecorSize(MistRadius * 2f), new Color(0.8f, 0.95f, 1f, 0.55f), 8f, 0f, 4, true, seconds);
        LineRenderer edge = Hostile.NewLine("PossessMistEdge", new Color(Chill.r, Chill.g, Chill.b, 0.4f), 0.08f, 6);
        edge.loop = true;
        Hostile.SetArc(edge, at, MistRadius, 0f, 360f);
        try
        {
            for (float t = 0f; t < seconds; t += 0.5f)
            {
                yield return new WaitForSeconds(0.5f);
                ghostOnce.Clear();
                foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(at, MistRadius, ghostHits)))
                    if (c != null && (c.CompareTag("enermy") || c.CompareTag("boss")) && ghostOnce.Add(c.gameObject)) AddChill(c.gameObject, 1);
            }
        }
        finally
        {
            if (a != null) Destroy(a.gameObject);
            if (edge != null) Destroy(edge.gameObject);
        }
    }

    void OnDestroyGhost()
    {
        foreach (FxAnim a in wispFx) if (a != null) Destroy(a.gameObject);
        wispFx.Clear();
        foreach (var kv in chillMarks) if (kv.Value != null) Destroy(kv.Value.gameObject);
        chillMarks.Clear();
        chillShape.Clear();
        if (possessRange != null) Destroy(possessRange.gameObject);
        RestoreHostBody();
        ResetPossess();
    }
}
