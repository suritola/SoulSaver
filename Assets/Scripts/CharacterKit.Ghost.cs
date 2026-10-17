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
    int FreezeNeed => Mathf.Max(2, 5 - card[1]);
    float FreezeTime => augment == AugDeepChill ? 1.5f : 1f;
    float HandReach => 4.5f + card[0];
    public bool IsFrozen(GameObject g) => g != null && frozenUntil.TryGetValue(g, out float t) && t > Time.time;
    public int ChillOf(GameObject g) => g != null && chill.TryGetValue(g, out int n) ? n : 0;

    // 영혼 (도깨비불)
    public int Wisps { get; private set; }
    public int WispMax => 4 + card[5];         // 1.0.2 조정: 6 + 2 → 4 + 1 (카드 5 · 6 · 7)
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
        FxAnim cloud = Fx.Play("fx_cloud", (a + b) * 0.5f, DecorSize(TrailHalfWidth * 2f), new Color(0.8f, 0.95f, 1f, 0.55f), 10f,
                               Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 10, true, 1.5f);
        if (cloud != null && cloud.sr != null && cloud.sr.sprite != null)
        {
            float unit = cloud.sr.sprite.bounds.size.x;
            Vector3 s = cloud.transform.localScale;
            if (unit > 0f) cloud.transform.localScale = new Vector3((d.magnitude + TrailHalfWidth * 2f) / unit, s.y, 1f);
        }
        for (float t = 0f; t < 1.5f; t += 0.5f)
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
            Play("freeze", 0.5f, 1f);
            SignatureSkills.Frozen(g);
        }
        chill[g] = have;
    }

    // 처치: 영혼이 생김 · 원혼 · 얼음 파편
    void GhostKill(Vector3 pos)
    {
        if (Id != CharacterId.Ghost) return;
        float chance = 0.2f + SignatureSkills.WispChanceBonus();       // 1.0.2 조정: 35% → 20%
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
            wispFx[i].transform.position = transform.position + Quaternion.Euler(0f, 0f, ang) * Vector3.right * r + Vector3.up * 0.3f;
        }
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
    bool possessAiming, possessing;
    Transform possessTarget;
    LineRenderer possessRange;          // 빙의 미리 보기: 터질 범위 (반지름 PossessRadius)
    const float PossessRadius = 5f;
    // 1.0.4~: 빙의 지속 1초 → 3초. 들어간 동안 내가 움직이면 그 적의 몸이 따라오고, 이동 속도는 그 적의 속도(난이도 · 장 배율이 붙은 값) 그대로
    //         너무 느리거나 빠른 적에서 조작이 망가지지 않게 내 평소 이동 속도의 30% ~ 130% 사이로만 (평소 21쯤이면 6.3 ~ 27.3 · 잡몹 6 ~ 16은 거의 그대로, 보스 5 ~ 7은 6.3 ~ 7)
    //         보스 · 적이 스킬을 쓰는 동안(casting) · 움직이지 않는 것(보스가 만든 등불 · 묘비)은 예전처럼 그 몸에 붙어 따라감 (스킬 동작이 깨지지 않게)
    const float PossessTime = 3f, PossessEnter = 0.15f, PossessGrace = 0.65f;
    const float PossessSpeedMin = 0.3f, PossessSpeedMax = 1.3f;
    float possessSpeed = -1f;           // 0 이상이면 빙의한 적의 이동 속도로 움직이는 중 (MoveMul 이 씀)

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
            Hostile.SetArc(possessRange, possessTarget.position, PossessRadius, 0f, 360f);
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
        StartCoroutine(PossessRoutine(possessTarget, Damage * 6f * UltMul));
        // 1.0.4~: 무기 필살기 추가 발동은 터지는 순간에 터진 자리로 (PossessRoutine 끝 · 예전엔 1.2초 뒤 들어간 자리)
        Spend(gauge);
    }

    IEnumerator PossessRoutine(Transform target, float dmg)
    {
        possessing = true;
        // 1.0.4~: 무적을 한 번에 1.8초 주던 것 → 들어가는 동안 + 들어가 있는 동안 계속 이어 주고, 터진 뒤 0.65초 (예전과 같은 여유)
        player.GrantInvincibility(PossessEnter + PossessGrace);
        Color before = body.color;
        Vector3 from = transform.position;
        Play("possess", 0.9f, 1f);
        Play("whoosh", 0.7f, 0.5f);
        for (float t = 0f; t < PossessEnter && target != null; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, target.position, t / PossessEnter);
            yield return null;
        }
        body.color = new Color(before.r, before.g, before.b, 0.12f);
        FxAnim glow = target != null ? Fx.Play("fx_soulburst", target.position, 2.6f, Wraith, 12f, 0f, 22, true, PossessTime) : null;
        if (glow != null) glow.follow = target;
        // 1.0.4~: 콜라이더가 자식에 있는 적 · 보스도 몸통(루트)을 찾아 끌고 다님
        EnermyController host = target != null ? target.GetComponentInParent<EnermyController>() : null;
        bosss bossHost = target != null && host == null ? target.GetComponentInParent<bosss>() : null;
        Transform carry = host != null ? host.transform : bossHost != null ? bossHost.transform : null;
        bool hostDiedEarly = false;
        for (float t = 0f; t < PossessTime; t += Time.deltaTime)
        {
            // 1.0.2: 들어간 적이 먼저 쓰러지면(다른 공격 · 사라짐) 기다리지 않고 바로 그 자리에서 터짐
            if (target == null || (host != null && host.IsDead) || (bossHost != null && bossHost.IsDead)) { hostDiedEarly = true; break; }
            player.GrantInvincibility(PossessGrace);
            // 1.0.4~: 내가 움직이고 그 적의 몸이 따라옴 (이동 속도는 그 적의 속도) · 스킬을 쓰는 중이거나 움직이지 않는 것은 그 몸에 붙어 따라감
            bool steer = carry != null && !(host != null ? host.casting : bossHost.casting);
            possessSpeed = steer ? Mathf.Max(0f, host != null ? host.speed : bossHost.speed) : -1f;
            if (steer) { Vector3 d = transform.position - target.position; d.z = 0f; carry.position += d; }
            else transform.position = target.position;
            yield return null;
        }
        possessSpeed = -1f;
        if (glow != null) Destroy(glow.gameObject);
        Vector3 at = transform.position;
        body.color = before;
        bool boss = target != null && target.CompareTag("boss");
        bool killed = false;
        if (target != null && !hostDiedEarly)
        {
            Specials.Damage(target.gameObject, dmg * (boss ? 1.5f : 1f), Vector3.zero, 0f);
            killed = host != null && host.IsDead;
        }
        ghostOnce.Clear();
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(at, PossessRadius, ghostHits)))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss")) || (target != null && c.transform == target)) continue;
            if (!ghostOnce.Add(c.gameObject)) continue;
            Specials.Damage(c.gameObject, dmg * 0.5f, (c.transform.position - at).normalized, 3f);
            AddChill(c.gameObject, 3);
        }
        // 1.0.2: 영혼 폭발 그림이 장식 배율(1.45 × 스킬 1.4)로 지름 20칸쯤 보여 실제 범위(반지름 5칸)의 두 배였음 → 범위에 맞춤
        Fx.Spawn("fx_soulburst", at, DecorSize(PossessRadius * 2f), Wraith, 16f);
        Fx.Spawn("fx_shock", at, PossessRadius * 2f, Chill, 18f);
        ShockRing.Spawn(at, 0.5f, PossessRadius, 0.4f, Chill, 0.25f);
        Hostile.Shake(0.25f);
        Play("boom", 0.8f, 0.8f);
        Play("wail", 0.8f, 0.8f);
        Play("freeze", 0.7f, 0.8f);
        if (card[3] > 0) StartCoroutine(PossessMist(at, 1f + card[3]));
        KitUltAdd(0.05f, at);
        SignatureSkills.Possessed(at, killed);
        possessing = false;
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
    }
}
