using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 씨름꾼 (2.0.0): 천하장사 — docs/plans/2026-11-02-big-update/hero-wrestler.md
//   평타 · 샅바 잡기: 커서 가까운 적을 붙잡아 머리 위로 들어 올림(들린 적이 날아오는 공격을 막음) → 다시 누르면 커서 쪽으로 던짐(볼링공) →
//                    지나가는 적(핀)은 옆으로 튕겨 쓰러지고, 던진 적은 착지에서 메침 피해. 누르고 있으면 잡기 · 던지기를 저절로 이어 감
//                    잡을 적이 없으면 손바닥 밀치기 · 보스는 옮기지 않고 배지기(제자리 들썩 연출 + 피해)
//   우클릭 · 천하장사: 누르고 있으면 가장 큰 적에 황소 뿔 표식 → 떼면 뛰어들어 들어 메침 + 모래 충격파 + 메친 자리에 모래판 10초
//   모래판: 땅에 고정된 원(반지름 5.5) · 안에서 출발해 밖에 떨어진 적(또는 밀려 경계를 넘은 적)은 장외(링아웃)
//   상태는 「끝나는 시각」 값만 (carryEnd · liftDone · ringEnd) — 켜고 끄는 코루틴 깃발 없음 (공통 장치: EnemyHold)
// 레벨업 카드 (card): 0 도미노 · 1 한 아름 · 2 되치기 · 3 모래 둑 · 5 번쩍 들기 · 6 발 걸기 / 형태: 들돌 · 워낭 · 밧줄 샅바 / 강화: 안다리 · 뒤집기 · 모래 바람
public partial class CharacterKit
{
    static readonly Color WrSand = new Color(0.91f, 0.76f, 0.48f);
    static readonly Color WrGold = new Color(1f, 0.82f, 0.3f);

    public const float WrReach = 2.4f, WrDive = 1.6f, WrDiveTime = 0.12f, WrCarryMax = 4f;
    public const float WrRingRadius = 5.5f, WrRingLife = 10f, WrSlamRadius = 7f;
    const int WrFlightCap = 6;

    // ---------------- 무게 (몸 반지름 네 단계 · 3-1절) — 경계값은 EnemyHitbox.BodyWorldRadius 기준
    public struct WrWeight { public int tier; public float lift, walk, speed, dist, W; public int pins; public float ballR; }

    static WrWeight WrTable(int tier) => tier switch
    {
        0 => new WrWeight { tier = 0, lift = 0.10f, walk = 0.9f, speed = 18f, dist = 9f, W = 1.0f, pins = 3, ballR = 0.8f },
        1 => new WrWeight { tier = 1, lift = 0.18f, walk = 0.8f, speed = 16f, dist = 8f, W = 1.3f, pins = 5, ballR = 1.0f },
        2 => new WrWeight { tier = 2, lift = 0.30f, walk = 0.65f, speed = 13f, dist = 6.5f, W = 1.8f, pins = 8, ballR = 1.4f },
        _ => new WrWeight { tier = 3, lift = 0.45f, walk = 0.5f, speed = 11f, dist = 5f, W = 2.4f, pins = 10, ballR = 1.8f },
    };

    static float WrBodyR(Component c)
    {
        if (c == null) return 0.6f;
        EnemyHitbox hb = c.GetComponent<EnemyHitbox>();
        if (hb != null && hb.BodyWorldRadius > 0.05f) return hb.BodyWorldRadius;
        return c.TryGetComponent(out Collider2D col) ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.y) : 0.6f;
    }

    public static int WrTier(EnermyController e)
    {
        if (e == null) return 1;
        if (e.IsMidBoss) return 3;
        float r = WrBodyR(e);
        return r < 0.55f ? 0 : r < 0.9f ? 1 : 2;
    }

    // 갈래 「장사」: 중간 보스를 들 수 있는가 (기본은 못 듦)
    bool WrMidOk => WrestlerPaths.On(WrestlerPaths.Giant);

    // 번쩍 들기(card 5): 무거운 적 · 중간 보스를 들 때 걸리는 시간과 들고 걸을 때 느려짐이 줄어듦
    WrWeight WrWeightOf(EnermyController e, bool forThrow = false)
    {
        int tier = WrTier(e);
        if (forThrow && form == SpecialAbilities.KitLiftStone && tier < 2) tier++;      // 들돌: 던진 적이 한 단계 무겁게 날아감 (피해 · 핀 상한 ↑ · 거리 ↓)
        WrWeight w = WrTable(tier);
        if (card[5] > 0 && w.tier >= 2)
        {
            float cut = card[5] == 1 ? 0.25f : card[5] == 2 ? 0.45f : card[5] == 3 ? 0.65f : 0.85f;      // 외길 4단계 −85%
            w.lift *= 1f - cut;
            w.walk = 1f - (1f - w.walk) * (1f - cut);
        }
        // 갈래 장사 「거구 사냥」: 무거운 적 · 중간 보스 무게 배율 +0.5 · 대가로 들기 시간 ×1.5
        if (WrestlerPaths.On(WrestlerPaths.Giant))
        {
            w.lift *= WrestlerPaths.GiantLiftCost;
            if (w.tier >= 2) w.W += WrestlerPaths.GiantWeightAdd;
        }
        return w;
    }

    // ---------------- 상태 (끝나는 시각 값)
    EnermyController wrCarried;
    WrWeight wrW;
    Vector3 wrGrabPos;
    float wrGrabAt, wrLiftDone, wrCarryEnd, wrBellyAt;
    bool wrLiftFired;
    float wrGap = -1f;
    int wrComboCount;
    float wrCounterReady, wrLassoReady, wrCowbellAt, wrBlockAt, wrLevee;
    int wrBellyCount;
    float wrBellyWindow;
    readonly List<EnermyController> wrExtra = new List<EnermyController>();         // 한 아름: 함께 든 가벼운 적
    readonly List<WrBall> wrBalls = new List<WrBall>();
    static readonly List<Collider2D> wrHits = new List<Collider2D>(64);
    LineRenderer wrAimLine, wrAimRing, wrMarkRing;
    Vector3 wrMoveLast;
    float wrMoveDirTime;
    Vector2 wrMoveDir;

    public bool WrCarrying => wrCarried != null && !wrCarried.IsDead && wrCarried.holder == (object)this;
    public bool HideHeldWeapon => Id == CharacterId.Wrestler && WrCarrying;
    public float WrCarryFill => WrCarrying ? Mathf.Clamp01((wrCarryEnd - Time.time) / WrCarryLimit) : 0f;
    public Vector3 WrCarriedPos => wrCarried != null ? wrCarried.transform.position : transform.position;
    public Vector2 WrMoveDir => wrMoveDir;
    public float WrMoveTime => wrMoveDirTime;

    // 평타 간격 덮어쓰기 (PlayerController 가 부름) — 잡은 뒤에는 짧게 바로 던질 수 있게
    public float AttackGap(float normal)
    {
        float mul = wrGapMul;
        wrGapMul = 1f;
        if (wrGap < 0f) return normal * mul;
        float g = wrGap;
        wrGap = -1f;
        return g;
    }

    // 걷는 속도: 적을 들면 무게만큼 느려짐 (모래판 안에서는 느려짐 절반)
    float WrWalkMul
    {
        get
        {
            if (Id != CharacterId.Wrestler || !WrCarrying) return 1f;
            float walk = wrW.walk;
            if (WrInRing(transform.position)) walk = 1f - (1f - walk) * 0.5f;
            if (SignatureSkills.IsEvo("wre.wall")) walk = Mathf.Min(1f, walk + 0.05f);
            return walk;
        }
    }

    // 맞는 피해 배율: 모래판 안 −20% · 되치기(맞는 순간 빈손이면 때린 적을 붙잡고 피해 절반)
    float WrTakenMul()
    {
        float mul = 1f;
        if (WrInRing(transform.position)) mul *= 0.8f;
        if (WrCarrying && WrestlerPaths.On(WrestlerPaths.Shield)) mul *= WrestlerPaths.ShieldTaken;      // 갈래 인간 방패: 들고 있는 동안 받는 피해 −30%
        if (card[2] > 0 && !WrCarrying && Time.time >= wrCounterReady && player != null && !player.IsDying)
        {
            EnermyController foe = WrNearestTakeable(3f);
            if (foe != null && WrTakeNow(foe, 0.1f, true))
            {
                wrCounterReady = Time.time + Mathf.Max(2f, 7f - card[2]);      // 6 / 5 / 4초 (외길 4단계 3초)
                Fx.Spawn("fx_sparkle", foe.transform.position, 1.4f, WrSand, 24f);
                mul *= 0.5f;
            }
        }
        return mul;
    }

    EnermyController WrNearestTakeable(float range)
    {
        EnermyController best = null;
        float bd = range * range;
        foreach (Collider2D c in Specials.Overlap(transform.position, range, wrHits))
        {
            if (c == null || !c.CompareTag("enermy") || !c.TryGetComponent(out EnermyController e) || !EnemyHold.CanTake(e, WrMidOk)) continue;
            float d = (e.transform.position - transform.position).sqrMagnitude;
            if (d < bd) { bd = d; best = e; }
        }
        return best;
    }

    // ================================================================= 평타
    void WrestlerAttack(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        Vector3 cursor = Mouse;
        if (WrCarrying)
        {
            // 다시 누름: 다 들어 올렸으면 던짐 (아직이면 간격이 차는 대로 다시 부름)
            if (Time.time >= wrLiftDone - 0.02f) WrThrow(dir, dmg);
            else wrGap = Mathf.Max(0.03f, wrLiftDone - Time.time + 0.02f);
            return;
        }
        wrCarried = null;
        Collider2D target = WrCandidate(dir, cursor);
        if (target != null)
        {
            EnermyController e = target.GetComponent<EnermyController>();
            bool belly = e == null || (e.IsMidBoss && !WrMidOk);
            if (belly) { WrBelly(target, dir, dmg); return; }
            float dist = Mathf.Max(0f, Vector2.Distance(transform.position, target.ClosestPoint(transform.position)));
            bool dive = dist > WrReach + (form == SpecialAbilities.KitRope ? 1.5f : 0f);
            WrWeight w = WrWeightOf(e);
            if (WrTakeNow(e, dive ? WrDiveTime : 0f, false))
            {
                if (dive) StartCoroutine(WrDiveTo(e.transform.position, dist));
                wrGap = Mathf.Max(0.05f, (dive ? WrDiveTime : 0f) + w.lift + 0.12f);
                PlayerMotion.Of(this).Throw(0.25f, dir);
                return;
            }
        }
        // 잡을 적이 없으면: 샅바 낚기(고유 스킬) → 들돌 던지기(형태) → 손바닥 밀치기
        if (WrLasso(dir)) return;
        if (form == SpecialAbilities.KitLiftStone && WrStoneThrow(dir, dmg)) return;
        WrPalm(start, dir, dmg);
    }

    // 잡을 후보: 몸 가장자리에서 2.4(+파고들기 1.6)칸 안 · 커서 쪽 ±80° 안에서 커서와 가장 가까운 적 (커서가 멀면 각이 가장 작은 적)
    Collider2D WrCandidate(Vector2 dir, Vector3 cursor)
    {
        Vector3 me = transform.position;
        float reach = WrReach + WrDive + (form == SpecialAbilities.KitRope ? 1.5f : 0f);
        Collider2D best = null;
        float bestScore = float.MaxValue;
        bool cursorNear = false;
        foreach (Collider2D c in Specials.Overlap(me, reach + 3f, wrHits))
        {
            if (c == null) continue;
            bool boss = c.CompareTag("boss");
            if (!boss && !c.CompareTag("enermy")) continue;
            EnermyController e = boss ? null : c.GetComponent<EnermyController>();
            if (!boss && e == null) continue;
            if (e != null && (e.IsDead || e.holder != null || e.swarm != null)) continue;
            Vector2 to = c.transform.position - me;
            float edge = Vector2.Distance(me, c.ClosestPoint(me));
            if (edge > reach) continue;
            float ang = Vector2.Angle(dir, to);
            if (to.sqrMagnitude > 1f && ang > 80f) continue;
            float cd = ((Vector2)(c.transform.position - cursor)).magnitude;
            // 커서가 적 가까이(3칸)면 커서 거리, 아니면 각이 작은 적
            float score = cd < 3f ? cd : 100f + ang;
            if (cd < 3f) cursorNear = true;
            if (score < bestScore) { bestScore = score; best = c; }
        }
        _ = cursorNear;
        return best;
    }

    // 붙잡음: 보스가 아닌 적을 바로 잡고(EnemyHold) 들어 올리기 시작 (파고드는 시간만큼 뒤에 올림)
    bool WrTakeNow(EnermyController e, float diveTime, bool counter)
    {
        if (!EnemyHold.TryTake(e, this, WrMidOk)) return false;
        wrCarried = e;
        wrW = WrWeightOf(e);
        wrGrabPos = e.transform.position;
        wrGrabAt = Time.time + diveTime;
        // 기술 연결(고유 스킬): 던진 적이 땅에 닿기 전에 다음 적을 잡으면 바로 들림 · 꽃가마: 천하장사 뒤 5초 즉시 들기
        bool chain = SignatureSkills.Lv("wr.combo") > 0 && wrBalls.Count > 0;
        wrComboCount = chain ? Mathf.Min(3, wrComboCount + 1) : 0;
        float lift = chain || SignatureSkills.WrParade || counter || wrInstantLift ? 0.03f : wrW.lift;
        wrInstantLift = false;          // 인간 방패 공명 II: 방패가 터진 뒤 다음 들기만 즉시
        wrBossGauge = 0f;
        wrLiftDone = wrGrabAt + lift;
        wrCarryEnd = Time.time + WrCarryLimit;
        wrLiftFired = false;
        EnemyHold.Raise(e, body, 2);
        RunCounters.Add("grab");
        Play("thump", 0.35f, 1.3f);
        Fx.Spawn(WFx("fx_sandpuff", "fx_sparkle"), e.transform.position, 1.6f, WrSand, 18f);
        // 안다리(강화): 잡는 순간 둘레 1.5칸 적이 넘어져 0.5초 못 움직임
        if (augment == AugInsideTrip) WrTrip(e.transform.position, 1.5f, 0.5f, e);
        // 한 아름(card 1): 곁(1.2칸)의 가벼운 적을 함께 들어 올려 나란히 던짐
        if (card[1] > 0) WrGatherExtra(e);
        return true;
    }

    void WrGatherExtra(EnermyController main)
    {
        wrExtra.Clear();
        foreach (Collider2D c in Specials.Overlap(main.transform.position, 1.2f, wrHits))
        {
            if (wrExtra.Count >= card[1]) break;
            if (c == null || !c.CompareTag("enermy") || !c.TryGetComponent(out EnermyController o) || o == main) continue;
            if (WrTier(o) != 0 || !EnemyHold.TryTake(o, this, false)) continue;
            wrExtra.Add(o);
            EnemyHold.Raise(o, body, 2);
        }
    }

    // 파고들기: 후보 쪽으로 0.12초 동안 최대 1.6칸
    IEnumerator WrDiveTo(Vector3 toward, float dist)
    {
        Vector3 from = transform.position;
        Vector3 dirv = ((Vector2)(toward - from)).normalized;
        float len = Mathf.Min(WrDive + (form == SpecialAbilities.KitRope ? 1.5f : 0f), Mathf.Max(0f, dist - WrReach * 0.5f));
        for (float t = 0f; t < WrDiveTime; t += Time.deltaTime)
        {
            if (player == null || player.IsDying) yield break;
            Vector3 p = from + dirv * (len * Mathf.Clamp01((t + Time.deltaTime) / WrDiveTime));
            if (!Hostile.IsWall(p) && (Hostile.ClampArena(p) - p).sqrMagnitude < 0.0001f) transform.position = p;
            yield return null;
        }
    }

    // 손바닥 밀치기: 앞 90° · 2.6칸 · 공격력 70% · 밀침 3 (단추가 헛돌지 않게)
    void WrPalm(Vector3 start, Vector2 dir, float dmg)
    {
        Vector3 o = transform.position;
        int n = 0;
        foreach (Collider2D c in Specials.Overlap(o, 2.6f + 1f, wrHits))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            Vector2 to = c.transform.position - o;
            if (to.sqrMagnitude > 0.25f && Vector2.Angle(dir, to) > 45f) continue;
            if (c.TryGetComponent(out EnermyController e) && e.holder != null) continue;
            WeakHit.Damage(c.gameObject, WeakShape.Arc(o, dir, 10f, 3.6f, 1.4f), dmg * 0.7f, to.normalized, 3f);
            n++;
        }
        PlayerMotion.Of(this).Throw(0.2f, dir);
        Fx.Spawn(WFx("fx_sandpuff", "fx_sparkle"), o + (Vector3)(dir * 1.6f), 1.8f, WrSand, 20f);
        Play("whoosh", 0.4f, 0.8f);
        if (n > 0) Play("thump", 0.4f, 1.2f);
    }

    // 보스 배지기: 보스는 옮기지 않고 제자리 들썩 연출 + 공격력 220% (다음까지 간격 ×1.4)
    void WrBelly(Collider2D boss, Vector2 dir, float dmg)
    {
        Vector3 bp = boss.transform.position;
        Vector3 me = transform.position;
        float edge = Vector2.Distance(me, boss.ClosestPoint(me));
        if (edge > 1.6f)
        {
            Vector3 to = Vector3.MoveTowards(me, boss.ClosestPoint(me), edge - 1.2f);
            if (!Hostile.IsWall(to)) transform.position = to;      // 보스 가장자리에 붙음
        }
        float mul = 2.2f * (WrestlerPaths.On(WrestlerPaths.Giant) ? 1f + (WrestlerPaths.GiantBossGrapple - 1f) * Paths.Power(WrestlerPaths.Giant) : 1f);      // 갈래 장사: 보스 배지기 +40%
        WeakHit.Damage(boss.gameObject, WeakShape.Arc(transform.position, dir, 25f, 4.5f, 1.4f), dmg * mul, dir, 0.4f);
        wrGap = -1f;
        wrBellyAt = Time.time;
        // 들썩 연출: 보스 발밑 그림자 0.2초 + 모래 튐 (보스 그림 자체는 건드리지 않음)
        Fx.Spawn(WFx("fx_liftshadow", "fx_sparkle"), bp + Vector3.down * 0.4f, 3f, new Color(0f, 0f, 0f, 0.5f), 12f, 0f, 3);
        Fx.Spawn(WFx("fx_sandpuff", "fx_shock"), bp, 3f, WrSand, 18f);
        PlayerMotion.Of(this).Pound(dir);
        Play("thump", 0.7f, 0.8f);
        Hostile.Shake(0.12f);
        RunCounters.Add("boss.grapple");
        WrGapSet(1.4f);
        SignatureSkills.WrBelly(bp, boss.gameObject);
        wrBellyCount++;
        // 갈래 장사 공명 II: 보스 배지기 세 번마다 보스가 들썩여 3초 동안 받는 피해 +15%
        if (Paths.Res(WrestlerPaths.Giant) >= 2 && WrestlerPaths.On(WrestlerPaths.Giant) && ++wrGiantBellies % WrestlerPaths.GiantRattleEvery == 0)
        {
            WrestlerPaths.Rattle(boss.gameObject);
            Fx.Spawn(WFx("fx_liftshadow", "fx_sparkle"), bp + Vector3.down * 0.4f, 4f, new Color(1f, 0.8f, 0.3f, 0.6f), 14f, 0f, 3);
        }
        if (SignatureSkills.IsEvo("wre.bull") && wrBellyCount % 2 == 0) StartCoroutine(WrGoldBull(boss.gameObject, dir));
    }

    float wrGapMul = 1f;
    void WrGapSet(float mul) { wrGapMul = mul; }

    // 황금 황소(진화): 보스 배지기 두 번째마다 황금 황소가 달려와 보스를 들이받음 (공격력 300%)
    IEnumerator WrGoldBull(GameObject boss, Vector2 dir)
    {
        if (boss == null) yield break;
        Vector3 end = boss.transform.position;
        Vector3 begin = end - (Vector3)(dir * 8f);
        FxAnim bull = Fx.Play(WFx("fx_bullrun", "fx_sparkle"), begin, 3f, WrGold, 12f, 0f, 20, true, 0.6f);
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            if (bull != null) bull.transform.position = Vector3.Lerp(begin, end, t / 0.45f);
            yield return null;
        }
        if (bull != null) Destroy(bull.gameObject);
        if (boss == null) yield break;
        using (DamageSource.As(DamageSource.Signature)) Specials.Damage(boss, SignatureSkills.WrAtk * 3f, dir, 0.5f);
        Fx.Spawn("fx_shock", end, 6f, WrGold, 20f);
        Impact.Hit(ImpactTier.Evolve, end, Vector2.up);
        Play("boom", 0.6f, 0.9f);
    }

    // 샅바 낚기(고유 스킬): 손 닿는 거리에 잡을 적이 없으면 샅바를 던져 6칸 안의 적을 끌어와 잡음 (보스 제외)
    bool WrLasso(Vector2 dir)
    {
        int lv = SignatureSkills.Lv("wr.lasso");
        if (lv <= 0 || Time.time < wrLassoReady) return false;
        EnermyController best = null;
        float bd = float.MaxValue;
        foreach (Collider2D c in Specials.Overlap(transform.position, 6.5f, wrHits))
        {
            if (c == null || !c.CompareTag("enermy") || !c.TryGetComponent(out EnermyController e) || !EnemyHold.CanTake(e, WrMidOk)) continue;
            Vector2 to = c.transform.position - transform.position;
            float d = to.magnitude + Vector2.Angle(dir, to) * 0.05f;
            if (d < bd) { bd = d; best = e; }
        }
        if (best == null) return false;
        Vector3 at = best.transform.position;
        if (!WrTakeNow(best, 0f, false)) return false;
        wrLassoReady = Time.time + SignatureSkills.WrLassoCooldown;
        wrLiftDone = Mathf.Max(wrLiftDone, Time.time + 0.3f);       // 끌려오는 동안
        wrGap = 0.4f;
        Fx.Beam(transform.position, at, 0.5f, new Color(0.9f, 0.7f, 0.4f, 0.9f), 0.25f, 18);
        Play("whoosh", 0.5f, 1.4f);
        PlayerMotion.Of(this).Throw(0.25f, dir);
        return true;
    }

    // ================================================================= 들고 있는 동안 (LateUpdate)
    void WrLate()
    {
        if (player == null) return;
        if (player.IsDying) { WrReleaseAll(); return; }
        // 걷는 방향 (황소 돌진)
        Vector3 pos = transform.position;
        Vector2 step = pos - wrMoveLast;
        wrMoveLast = pos;
        if (step.sqrMagnitude > 0.0004f && Time.timeScale > 0f)
        {
            Vector2 nd = step.normalized;
            if (wrMoveDirTime <= 0f || Vector2.Angle(nd, wrMoveDir) < 40f) wrMoveDirTime += Time.deltaTime; else wrMoveDirTime = 0f;
            wrMoveDir = nd;
        }
        else wrMoveDirTime = 0f;

        WrRingTick();
        WrPathTick();                   // 갈래: 모래 웅덩이 · 경계 미끄러짐 · 모래판 끌기
        if (!WrCarrying)
        {
            if (wrCarried != null) { wrCarried = null; wrExtra.RemoveAll(x => x == null || x.IsDead || x.holder != (object)this); }
            WrDrawMarks(false);
            return;
        }

        EnermyController e = wrCarried;
        // 위치: 머리 위(둘째 층) — 파고드는 동안은 잡은 자리 · 올리는 동안 부드럽게 · 좌우는 플레이어 방향 · 0.15칸 버둥
        float lift = Mathf.Max(0.01f, wrLiftDone - wrGrabAt);
        float k = Mathf.Clamp01((Time.time - wrGrabAt) / lift);
        k = 1f - (1f - k) * (1f - k);
        Vector3 over = WrOverhead(WrBodyR(e));
        float wob = Mathf.Sin(Time.time * 26f) * 0.15f * Mathf.Clamp01(k);
        Vector3 spot = Vector3.Lerp(wrGrabPos, over, k) + new Vector3(wob, 0f, 0f);
        e.transform.position = spot;
        for (int i = wrExtra.Count - 1; i >= 0; i--)
        {
            EnermyController x = wrExtra[i];
            if (x == null || x.IsDead || x.holder != (object)this) { wrExtra.RemoveAt(i); continue; }
            x.transform.position = Vector3.Lerp(x.transform.position, over + new Vector3((i % 2 == 0 ? 1f : -1f) * (0.9f + 0.5f * (i / 2)), -0.1f, 0f), 0.4f);
        }
        if (!wrLiftFired && Time.time >= wrLiftDone)
        {
            wrLiftFired = true;
            SignatureSkills.WrLifted(transform.position);
            if (SignatureSkills.IsEvo("wre.slam")) WrThud(over, 2f, 0.5f);
        }
        // 투사체 막기: 들린 몸에 닿는 적 투사체를 지움 (막을 때마다 들린 적이 공격력 30% 피해 = 방패가 닳음)
        if (Time.time >= wrBlockAt) WrBlock(e, over);
        // 워낭(형태): 들고 있는 동안 0.5초마다 둘레 3칸에 소리 파동 (공격력 30%)
        if (form == SpecialAbilities.KitCowbell && Time.time >= wrCowbellAt)
        {
            wrCowbellAt = Time.time + 0.5f;
            DamageCircle(transform.position, 3f, Damage * 0.3f * formPower, 0.6f);
            Fx.Spawn(WFx("fx_cowbellwave", "fx_shock"), transform.position, 6f, new Color(1f, 0.85f, 0.45f, 0.6f), 18f);
            Play("chime", 0.25f, 0.7f);
        }
        // 4초가 지나면 버둥치며 빠져나가기 직전 발밑에 메침 (그 적에게 공격력 100% · 둘레 1.2칸 50%)
        if (Time.time >= wrCarryEnd) WrDropSlam(e);
        WrDrawMarks(Time.time >= wrLiftDone);
    }

    Vector3 WrOverhead(float bodyR)
    {
        Vector3 p = transform.position + Vector3.up * (1.2f + bodyR);
        return p;
    }

    void WrBlock(EnermyController shield, Vector3 at)
    {
        bool any = false;
        float r = 0.9f + WrBodyR(shield) * 0.6f;
        List<HostileProjectile> list = HostileProjectile.Live;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            HostileProjectile hp = list[i];
            if (hp == null) continue;
            if (((Vector2)(hp.transform.position - at)).sqrMagnitude > (r + hp.radius * 0.5f) * (r + hp.radius * 0.5f)) continue;
            Vector3 hitAt = hp.transform.position;
            Destroy(hp.gameObject);
            any = true;
            WrBlockedPath(hitAt);           // 갈래 인간 방패: 되튕김 · 보스 탄 게이지
            // 극의: 막아도 방패가 닳지 않음 · 입문: 닳는 피해 30 → 40%
            if (!Paths.Mastered(WrestlerPaths.Shield))
            {
                float wear = WrestlerPaths.On(WrestlerPaths.Shield) ? WrestlerPaths.ShieldWear : 0.3f;
                using (DamageSource.As(DamageSource.Signature)) Specials.Damage(shield.gameObject, Damage * wear, Vector3.zero, 0f);
            }
            SignatureSkills.WrBlocked(hitAt);
            if (shield == null || shield.IsDead) { WrShieldBurst(at); break; }
        }
        if (any) { Fx.Spawn("fx_sparkle", at, 1.4f, WrSand, 24f); Play("clank", 0.3f, 0.7f); }
        wrBlockAt = Time.time + 0.03f;
    }

    // 4초 버둥: 발밑에 메침
    void WrDropSlam(EnermyController e)
    {
        Vector3 at = transform.position;
        WrReleaseCarried(true);
        if (e == null || e.IsDead) return;
        e.transform.position = EnemyHold.FreeSpot(at + (Vector3)(Random.insideUnitCircle.normalized * 1.2f));
        using (DamageSource.As(DamageSource.Basic))
        {
            Specials.Damage(e.gameObject, Damage, Vector3.down, 0.3f);
            DamageCircle(e.transform.position, 1.2f, Damage * 0.5f, 1f);
        }
        Fx.Spawn(WFx("fx_sandpuff", "fx_shock"), e.transform.position, 2.4f, WrSand, 18f);
        Play("thump", 0.5f, 0.9f);
    }

    // 들고 있는 것(주된 적 · 함께 든 적)을 놓음 — 놓은 적은 제자리 근처에 서고 빈손이 됨
    void WrReleaseCarried(bool andExtra)
    {
        EnermyController e = wrCarried;
        wrCarried = null;
        if (e != null) EnemyHold.Release(e, this);
        if (andExtra)
        {
            foreach (EnermyController x in wrExtra) if (x != null) { EnemyHold.Release(x, this); x.transform.position = EnemyHold.FreeSpot(x.transform.position + Vector3.down * 1.2f); }
            wrExtra.Clear();
        }
    }

    void WrReleaseAll()
    {
        wrCarried = null;
        wrExtra.Clear();
        EnemyHold.ReleaseAll(this);
        WrDrawMarks(false);
    }

    // 쿵: 둘레 반지름 안 적을 밀치고 다치게 함 (배지기 · 천지 메치기)
    public void WrThud(Vector3 at, float r, float rate)
    {
        using (DamageSource.As(DamageSource.Signature)) DamageCircle(at, r, Damage * rate * formPower, 2f);
        Fx.Spawn(WFx("fx_sandpuff", "fx_shock"), at, r * 2f, WrSand, 20f);
        Play("thump", 0.45f, 0.9f);
    }

    // 발 걸기: 둘레의 적(보스 제외)이 넘어져 잠깐 움직이지 못함 (Slow(0, t) = 묶임과 같은 처리)
    void WrTrip(Vector3 at, float r, float sec, EnermyController except)
    {
        foreach (Collider2D c in Specials.Overlap(at, r, wrHits))
            if (c != null && c.CompareTag("enermy") && c.TryGetComponent(out EnermyController o) && o != except && !o.IsMidBoss && o.holder == null) o.Slow(0f, sec);
    }

    // ================================================================= 던지기 (볼링공)
    sealed class WrBall
    {
        public EnermyController e;          // 던져진 적 (들돌이면 null)
        public Transform mover;             // 실제로 움직이는 그림
        public Vector3 from;
        public Vector2 dir;
        public WrWeight w;
        public float dmg;                   // 평타 기준 공격력 × 배율(연결 · 꽃가마)
        public int gen;                     // 0 = 던진 공, 1·2 = 튕겨 나온 핀(세대 상한 2)
        public float scale = 1f;            // 눈덩이로 커진 정도
        public bool fromRing, stop, returning, stone, noSelf, shock;
        public float distMul = 1f;
        public int pinsCapExtra;
        public readonly List<EnermyController> pins = new List<EnermyController>();
        public int pinCount;
    }

    void WrThrow(Vector2 dir, float dmg)
    {
        EnermyController e = wrCarried;
        List<EnermyController> mates = new List<EnermyController>(wrExtra);
        wrCarried = null;
        wrExtra.Clear();
        if (e == null || e.IsDead) return;
        if (dir.sqrMagnitude < 0.01f) dir = body != null && body.flipX ? Vector2.left : Vector2.right;
        Vector3 from = e.transform.position;
        bool inRing = WrInRing(transform.position);
        WrWeight w = WrWeightOf(e, true);
        float mul = SignatureSkills.WrThrowMul(wrComboCount);
        bool ironClad = SignatureSkills.IsEvo("wre.giantbull") && wrGiantBullHeld == e;
        if (ironClad) { wrGiantBullHeld = null; }
        mul *= WrPathThrowMul(w);       // 갈래 대가: 인간 방패 던짐 −25% · 장사의 가벼운 적 −40%
        // 갈래 엎어치기: 던지는 대신 앞 바닥에 내리꽂음
        if (WrestlerPaths.On(WrestlerPaths.Slam)) { WrSlamDown(e, mates, dir, dmg * mul, inRing, w); return; }
        // 한 아름: 함께 든 적도 나란히 (천지 메치기: X자로 엇갈려)
        float spread = SignatureSkills.IsEvo("wre.slam") ? 16f : 7f;
        WrLaunch(e, from, dir, w, dmg * mul, inRing, mates.Count > 0 ? -spread : 0f, ironClad);
        for (int i = 0; i < mates.Count; i++)
        {
            EnermyController x = mates[i];
            if (x == null || x.IsDead) continue;
            float a = (i % 2 == 0 ? 1f : -1f) * spread * (1 + i / 2) * (SignatureSkills.IsEvo("wre.slam") ? 1f : 1f);
            WrLaunch(x, x.transform.position, dir, WrWeightOf(x, true), dmg * mul, inRing, a, false);
        }
        RunCounters.Both("throw");
        if (w.tier >= 2) RunCounters.Add("heavy.throw");
        PlayerMotion.Of(this).Throw(0.3f, dir);
        Play("whoosh", 0.7f, 0.7f);
        wrGap = -1f;
        // 모래 바람(강화): 0.15초 뒤 같은 길로 한 번 더 쓸고 지나감 (핀 피해 50%)
        if (augment == AugSandWake) StartCoroutine(WrWind(from, dir, w.dist, dmg * 0.9f * w.W * 0.5f));
    }

    EnermyController wrGiantBullHeld;

    void WrLaunch(EnermyController e, Vector3 from, Vector2 dir, WrWeight w, float dmg, bool inRing, float angle, bool bigBang)
    {
        if (angle != 0f) dir = Quaternion.Euler(0f, 0f, angle) * dir;
        // 동시 비행 상한 6: 넘으면 가장 오래된 것을 그 자리에 착지 처리
        wrBalls.RemoveAll(b => b == null || b.e == null && !b.stone);
        while (wrBalls.Count >= WrFlightCap) { wrBalls[0].stop = true; wrBalls.RemoveAt(0); }
        WrBall ball = new WrBall { e = e, mover = e != null ? e.transform : null, from = from, dir = dir.normalized, w = w, dmg = dmg, fromRing = inRing };
        if (bigBang) ball.shock = true;
        wrBalls.Add(ball);
        StartCoroutine(WrFlight(ball));
    }

    IEnumerator WrFlight(WrBall b)
    {
        EnermyController e = b.e;
        WrWeight w = b.w;
        Vector3 p = b.from;
        bool bowl = WrestlerPaths.On(WrestlerPaths.Bowl) && b.gen == 0 && !b.stone && !b.returning;       // 갈래 볼링
        float maxDist = w.dist * b.distMul * (b.gen > 0 ? 0.6f : 1f) * (WrestlerPaths.On(WrestlerPaths.Bowl) && b.gen == 0 ? WrestlerPaths.BowlRange : 1f);
        if (SignatureSkills.IsEvo("wre.giantbull") && b.gen == 0) maxDist *= 1f;
        float speed = w.speed, travelled = 0f, life = maxDist / Mathf.Max(1f, speed) + 0.3f, t = 0f;
        float ballR = w.ballR * b.scale;
        int cap = w.pins + b.pinsCapExtra;
        if (b.gen == 0 && WrestlerPaths.On(WrestlerPaths.Bowl)) cap = w.pins * (int)WrestlerPaths.BowlPinCapMul + b.pinsCapExtra;
        // 교차 거구 볼링: 무거운 적 · 중간 보스를 던지면 핀 상한 없음
        if (b.gen == 0 && !b.stone && w.tier >= 2 && Paths.CrossOn(WrestlerPaths.GiantBowlCross)) cap = int.MaxValue / 2;
        bool pull = b.gen == 0 && !b.stone && w.tier >= 2 && Paths.Res(WrestlerPaths.Giant) >= 1 && WrestlerPaths.On(WrestlerPaths.Giant);       // 장사 공명 I: 길 둘레 적이 끌려와 핀이 됨
        bool bounceOk = bowl && Paths.Res(WrestlerPaths.Bowl) >= 1, bounced = false;                    // 볼링 공명 I: 한 번 되튕김
        bool tripOk = bowl && Paths.Mastered(WrestlerPaths.Bowl), tripped = false;                      // 볼링 극의: 끝에서 되돌아옴
        int legStart = 0;
        float pinPush = WrestlerPaths.On(WrestlerPaths.Bowl) ? WrestlerPaths.BowlPinPush : SignatureSkills.WrBowlPush;
        HashSet<int> seen = new HashSet<int>();
        if (e != null) seen.Add(e.gameObject.GetInstanceID());
        bool bump = false;
        Vector2 dir = b.dir;
        while (true)
        {
            if (b.stone ? b.mover == null : (e == null || e.IsDead || e.holder != (object)this)) { wrBalls.Remove(b); if (b.stone && b.mover != null) Destroy(b.mover.gameObject); yield break; }
            if (b.stop || (player != null && player.IsDying)) break;
            float dt = Time.deltaTime;
            Vector3 next = p + (Vector3)(dir * speed * dt);
            bool blocked = Hostile.IsWall(next) || (Hostile.ClampArena(next) - next).sqrMagnitude > 0.0001f;
            // 볼링 공명 I: 모래판 경계에 닿으면 안쪽으로 한 번 되튕김
            Vector3 edgeCenter = Vector3.zero;
            bool edge = bounceOk && !bounced && !blocked && b.fromRing && WrRingAt(p, out edgeCenter) && !WrInRing(next);
            if (blocked || edge)
            {
                if (bounceOk && !bounced)
                {
                    Vector2 nd = edge ? WrReflectRing(dir, p, edgeCenter) : WrBounceWall(p, dir, speed * dt);
                    if (nd.sqrMagnitude > 0.01f)
                    {
                        dir = nd.normalized;
                        bounced = true;
                        Fx.Spawn(WFx("fx_sandpuff", "fx_sparkle"), p, 1.6f, WrSand, 20f);
                        Play("thump", 0.3f, 1.4f);
                        yield return null;
                        continue;
                    }
                }
                if (blocked) { bump = true; break; }
            }
            p = next;
            travelled += speed * dt;
            t += dt;
            float h = Mathf.Sin(Mathf.Clamp01(travelled / Mathf.Max(0.1f, maxDist)) * Mathf.PI) * 0.6f;
            if (b.mover != null) b.mover.position = p + Vector3.up * h;
            if (Random.value < 0.35f) Fx.Spawn(WFx("fx_throwtrail", "fx_smoke"), p, 1.2f, new Color(WrSand.r, WrSand.g, WrSand.b, 0.55f), 20f);
            bool done = false;
            using (var ovl = PhysHits.Circle(p, pull ? ballR + WrestlerPaths.GiantPullRadius : ballR)) foreach (Collider2D c in ovl)
            {
                if (c == null) continue;
                bool isBoss = c.CompareTag("boss");
                if (!isBoss && !c.CompareTag("enermy")) continue;
                GameObject go = c.gameObject;
                if (e != null && go == e.gameObject) continue;
                // 장사 공명 I: 볼 둘레 1.5칸의 적은 볼 쪽으로 끌려옴 (몸이 볼에 닿으면 핀으로 맞음)
                if (pull && !isBoss && ((Vector2)(go.transform.position - p)).sqrMagnitude > ballR * ballR && c.TryGetComponent(out EnermyController pulled) && !pulled.IsDead && pulled.holder == null && pulled.swarm == null)
                {
                    Vector3 pt = Vector3.MoveTowards(go.transform.position, p, 14f * dt);
                    if (!Hostile.IsWall(pt)) go.transform.position = pt;
                    continue;
                }
                if (!seen.Add(go.GetInstanceID())) continue;
                if (isBoss)
                {
                    // 보스: 공격력 200% × W 한 번 (빈틈 위면 WeakHit 규칙) · 볼은 그 자리에서 벽꿍
                    float bd = b.dmg * 2f * w.W * (b.stone ? 0.75f : 1f) * WrBowlBossMul(b);
                    WeakHit.Hit(go, WeakShape.Marked(1.75f), bd, dir, 0.5f);
                    bump = true; done = true;
                    SignatureSkills.WrBossHit(go.transform.position);
                    break;
                }
                EnermyController pe = go.GetComponent<EnermyController>();
                if (pe == null || pe.IsDead || pe.holder != null) continue;
                // 핀: 공격력 90% × W + 길에서 옆으로 2칸 튕김
                Vector2 to = go.transform.position - p;
                Vector2 side = new Vector2(-dir.y, dir.x) * Mathf.Sign(dir.x * to.y - dir.y * to.x == 0f ? 1f : dir.x * to.y - dir.y * to.x);
                bool wasIn = WrInRing(go.transform.position);
                float pd = b.dmg * 0.9f * w.W * (b.returning ? 0.5f : 1f);
                using (DamageSource.As(DamageSource.Basic)) Specials.Damage(go, pd, side, 0f);
                if (pe != null && !pe.IsDead)
                {
                    Vector3 pushed = Hostile.ClampArena(go.transform.position + (Vector3)(side * pinPush));
                    if (!Hostile.IsWall(pushed)) go.transform.position = pushed;
                    b.pins.Add(pe);
                    // 링아웃: 볼에 튕긴 핀이 경계를 넘음 (잡몹만 — 장외 스트라이크 진화는 보통 적까지)
                    int pt = WrTier(pe);
                    bool allow = pt == 0 || (pt == 1 && SignatureSkills.IsEvo("wre.ringstrike"));
                    if (b.fromRing && wasIn && allow && WrRingOn && !WrInRing(go.transform.position)) WrRingOut(pe, go.transform.position);
                    // 도미노(card 0): 처음 맞은 핀이 함께 날아가 다른 적을 쓰러뜨림 (핀 피해 60% · 2세대 핀은 다시 볼이 되지 않음)
                    if (b.gen == 0 && pe != null && !pe.IsDead && b.pinCount < card[0]) WrSpawnPin(pe, go.transform.position, dir, b.dmg * 0.6f, b.gen + 1);
                }
                b.pinCount++;
                SignatureSkills.WrPin(b.pinCount);
                if (SignatureSkills.Lv("wr.snowball") > 0 && b.pinCount <= 8 && b.gen == 0) { b.scale = 1f + 0.06f * b.pinCount; ballR = w.ballR * b.scale; b.dmg *= 1f + SignatureSkills.WrSnowStep; }
                if (b.pinCount - legStart >= cap) { done = true; break; }
                if (WrTier(pe) > w.tier) { bump = true; done = true; break; }       // 무거운 적에 부딪혀 멈춤 → 벽꿍
            }
            // 볼링 극의: 끝에서 되돌아옴 (왕복 · 핀 상한 새로)
            if (!done && tripOk && !tripped && travelled >= maxDist)
            {
                tripped = true;
                dir = -dir;
                travelled = 0f;
                t = 0f;
                legStart = b.pinCount;
                seen.Clear();
                if (e != null) seen.Add(e.gameObject.GetInstanceID());
                Fx.Spawn(WFx("fx_sandpuff", "fx_sparkle"), p, 2f, WrSand, 20f);
                yield return null;
                continue;
            }
            if (done || travelled >= maxDist || t > life) break;
            yield return null;
        }
        wrBalls.Remove(b);
        WrLand(b, p, bump, dir);
    }

    // 튕긴 핀이 함께 날아감 (세대 2 상한)
    void WrSpawnPin(EnermyController pe, Vector3 at, Vector2 dir, float dmg, int gen)
    {
        if (gen > 2 || pe == null || !EnemyHold.TryTake(pe, this, false)) return;
        WrWeight pw = WrTable(Mathf.Min(1, WrTier(pe)));
        pw.dist = 4f; pw.pins = 3;
        Vector2 d = Quaternion.Euler(0f, 0f, Random.Range(-35f, 35f)) * dir;
        WrBall nb = new WrBall { e = pe, mover = pe.transform, from = at, dir = d.normalized, w = pw, dmg = dmg, gen = gen, fromRing = false, noSelf = false };
        wrBalls.Add(nb);
        StartCoroutine(WrFlight(nb));
    }

    // 착지: 던진 적 자신이 메침 피해 공격력 160% × W (벽 · 경기장 끝 · 보스 · 무거운 적에 부딪혀 멈췄으면 벽꿍 ×1.5) + 둘레 1.2칸 50%
    void WrLand(WrBall b, Vector3 p, bool bump, Vector2 dir)
    {
        EnermyController e = b.e;
        if (b.stone)
        {
            if (b.mover != null) Destroy(b.mover.gameObject);
            using (DamageSource.As(DamageSource.Basic)) DamageCircle(p, 1.6f, b.dmg * 1.5f, 1.5f);
            Fx.Spawn(WFx("fx_sandpuff", "fx_shock"), p, 3.2f, WrSand, 18f);
            Play("thump", 0.6f, 0.8f);
            return;
        }
        if (e == null) return;
        Vector3 spot = EnemyHold.FreeSpot(p);
        EnemyHold.Release(e, this);
        if (e.IsDead) return;
        e.transform.position = spot;
        WrWeight w = b.w;
        if (!b.returning && !b.noSelf)
        {
            // 갈래: 볼링 대가(메침 −50%) · 장사 극의(중간 보스를 던져 벽꿍 · 링아웃시키면 두 배)
            bool willOut = b.fromRing && WrRingOn && !WrInRing(spot);
            bool giantDouble = Paths.Mastered(WrestlerPaths.Giant) && WrestlerPaths.On(WrestlerPaths.Giant) && w.tier >= 3 && (bump || willOut);
            float self = b.dmg * 1.6f * w.W * (bump ? 1.5f : 1f) * (WrestlerPaths.On(WrestlerPaths.Bowl) ? WrestlerPaths.BowlSelfCost : 1f) * (giantDouble ? WrestlerPaths.GiantMasteryMul : 1f);
            using (DamageSource.As(DamageSource.Basic))
            {
                Specials.Damage(e.gameObject, self, dir, 0.3f);
                DamageCircle(spot, 1.2f, b.dmg * 0.5f, 0.8f);
            }
        }
        Fx.Spawn(WFx("fx_sandpuff", "fx_shock"), spot, 2.4f * Mathf.Max(1f, b.scale), WrSand, 18f);
        Play("thump", 0.55f, 0.9f);
        // 링아웃: 모래판 안에서 출발한 볼이 밖에 떨어짐
        bool ringOut = false;
        if (b.fromRing && !b.returning && WrRingOn && !WrInRing(spot) && !e.IsDead) { WrRingOut(e, spot, Paths.Mastered(WrestlerPaths.Giant) && WrestlerPaths.On(WrestlerPaths.Giant) && w.tier >= 3 ? WrestlerPaths.GiantMasteryMul : 1f); ringOut = true; }
        // 벽꿍 (무거움 이상): 손맛
        if (bump && w.tier >= 2 && !ringOut) Impact.Hit(ImpactTier.Attack, spot, Vector2.up);
        // 스트라이크: 한 번에 핀 6마리 (볼링 갈래가 없으면 핀 상한 5라 무거운 적으로만)
        bool forced = b.gen == 0 && w.tier >= 2 && Paths.CrossOn(WrestlerPaths.GiantBowlCross);        // 교차 거구 볼링: 멈출 때 반드시 스트라이크
        if ((b.pinCount >= WrestlerPaths.BowlStrikePins || forced) && b.gen == 0) WrStrike(b, spot, forced);
        // 발 걸기(card 6): 떨어진 자리 둘레 1.5칸의 적이 넘어짐
        if (card[6] > 0 && !b.returning) WrTrip(spot, 1.5f, 0.4f + 0.2f * (card[6] - 1), e);
        // 천지 메치기: 떨어질 때도 둘레 2칸에 쿵
        if (SignatureSkills.IsEvo("wre.slam") && !b.returning) WrThud(spot, 2f, 0.5f);
        // 뒤집기(강화): 떨어진 뒤 한 번 더 튀어 올라 메쳐짐 (피해 50%)
        if (augment == AugFlip && !b.returning && !e.IsDead && b.gen == 0) StartCoroutine(WrFlipBack(e, spot, b.dmg * 0.5f * w.W));
        // 밧줄 샅바(형태): 던진 적이 밧줄에 매여 끝에서 되돌아오며 한 번 더 쓰러뜨림 (피해 50%)
        if (form == SpecialAbilities.KitRope && !b.returning && b.gen == 0 && !e.IsDead && player != null)
        {
            Vector2 back = ((Vector2)(transform.position - spot));
            if (back.sqrMagnitude > 1f && EnemyHold.TryTake(e, this, false))
            {
                WrBall rb = new WrBall { e = e, mover = e.transform, from = spot, dir = back.normalized, w = w, dmg = b.dmg * 0.5f, gen = 1, returning = true, distMul = 1f };
                wrBalls.Add(rb);
                Fx.Beam(transform.position, spot, 0.35f, new Color(0.85f, 0.7f, 0.4f, 0.8f), 0.3f, 17);
                StartCoroutine(WrFlight(rb));
            }
        }
        // 황소 장사(진화): 천하장사로 든 적을 던지면 충격파가 한 번 더 퍼짐 (공격력 150%)
        if (b.shock)
        {
            using (DamageSource.As(DamageSource.Signature)) DamageCircle(spot, WrSlamRadius * 0.7f, Damage * 1.5f, 2.5f);
            Fx.Spawn(WFx("fx_sandwave", "fx_shock"), spot, WrSlamRadius * 1.6f, new Color(WrSand.r, WrSand.g, WrSand.b, 0.9f), 18f);
            Impact.Hit(ImpactTier.Evolve, spot, Vector2.up);
        }
        SignatureSkills.WrLanded(spot, b.pinCount);
    }

    IEnumerator WrFlipBack(EnermyController e, Vector3 spot, float dmg)
    {
        yield return new WaitForSeconds(0.25f);
        if (e == null || e.IsDead) yield break;
        using (DamageSource.As(DamageSource.Basic))
        {
            Specials.Damage(e.gameObject, dmg, Vector3.down, 0.2f);
            DamageCircle(e.transform.position, 1.2f, dmg * 0.4f, 0.5f);
        }
        Fx.Spawn(WFx("fx_sandpuff", "fx_shock"), e.transform.position, 2.2f, WrSand, 18f);
        Play("thump", 0.4f, 1.1f);
    }

    // 스트라이크: 핀을 다섯 넘게 쓰러뜨림 — 손맛 · 계약 · 스트라이크 장사(진화): 볼이 터지며 핀들이 사방으로 흩어져 한 번씩 더 굴러감
    void WrStrike(WrBall b, Vector3 at, bool forced = false)
    {
        if (b.pinCount >= WrestlerPaths.BowlStrikePins) RunCounters.Both("strike");
        // 볼링 공명 II (또는 교차 거구 볼링): 볼이 터짐 (반경 2.5칸 · 공격력 200%)
        if (forced || (Paths.Res(WrestlerPaths.Bowl) >= 2 && WrestlerPaths.On(WrestlerPaths.Bowl)))
        {
            Paths.Circle(at, WrestlerPaths.BowlStrikeRadius, Paths.Atk * WrestlerPaths.BowlStrikeRate * Paths.Power(WrestlerPaths.Bowl), 2.5f, WrestlerPaths.StrikeSource);
            Fx.Spawn(WFx("fx_sandwave", "fx_shock"), at, WrestlerPaths.BowlStrikeRadius * 2.2f, new Color(WrGold.r, WrGold.g, WrGold.b, 0.9f), 18f);
        }
        Fx.Spawn(WFx("fx_strike", "fx_explosion"), at, 5f, WrGold, 18f);
        Impact.Hit(ImpactTier.Attack, at, Vector2.up);
        WrCheerLater(0.08f, 0.35f);
        if (!SignatureSkills.IsEvo("wre.strike") || b.gen != 0 || b.pinCount < WrestlerPaths.BowlStrikePins) return;
        int n = 0;
        foreach (EnermyController pe in b.pins)
        {
            if (pe == null || pe.IsDead || n >= 5) continue;
            WrSpawnPin(pe, pe.transform.position, Quaternion.Euler(0f, 0f, 72f * n) * Vector2.right, b.dmg * 0.6f, 2);
            n++;
        }
    }

    // 모래 바람: 던진 길을 따라 쓸고 지나감
    IEnumerator WrWind(Vector3 from, Vector2 dir, float dist, float dmg)
    {
        yield return new WaitForSeconds(0.15f);
        HashSet<int> seen = new HashSet<int>();
        for (float d = 0.5f; d <= dist; d += 0.8f)
        {
            Vector3 p = from + (Vector3)(dir * d);
            if (Hostile.IsWall(p)) break;
            using (var ovl = PhysHits.Circle(p, 1.1f)) foreach (Collider2D c in ovl)
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss")) || !seen.Add(c.gameObject.GetInstanceID())) continue;
                if (c.TryGetComponent(out EnermyController ec) && ec.holder != null) continue;
                using (DamageSource.As(DamageSource.Basic)) Specials.Damage(c.gameObject, dmg, dir, 0.5f);
            }
            if (((int)(d * 1.25f)) % 2 == 0) Fx.Spawn(WFx("fx_sandpuff", "fx_smoke"), p, 1.6f, new Color(WrSand.r, WrSand.g, WrSand.b, 0.6f), 22f);
            yield return null;
        }
    }

    // 들돌(형태): 잡을 적이 없으면 발밑의 들돌을 들어 던짐 (공격력 150% · 핀 4)
    bool WrStoneThrow(Vector2 dir, float dmg)
    {
        GameObject g = new GameObject("LiftStone");
        g.transform.position = transform.position;
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        Sprite s = Resources.Load<Sprite>("Weapons/weapon_liftstone");
        if (s != null) { sr.sprite = s; sr.sortingLayerID = body.sortingLayerID; sr.sortingOrder = body.sortingOrder + 2; g.transform.localScale = Vector3.one * 1.4f; }
        WrWeight w = WrTable(1); w.pins = 4; w.dist = 8f; w.ballR = 1f;
        WrBall st = new WrBall { stone = true, mover = g.transform, from = transform.position + (Vector3)(dir * 0.8f), dir = dir.normalized, w = w, dmg = dmg * 1.5f / 1.3f, fromRing = false };
        wrBalls.Add(st);
        StartCoroutine(WrFlight(st));
        PlayerMotion.Of(this).Throw(0.3f, dir);
        Play("whoosh", 0.6f, 0.6f);
        return true;
    }

    // ================================================================= 모래판
    float wrRingBorn, wrRingLifeAll, wrRingEnd, wrRingR = WrRingRadius;
    Vector3 wrRingPos;
    GameObject wrRingObj;
    LineRenderer wrRingRope;
    int wrRingOuts;
    static Sprite wrSandSprite;

    public bool WrRingOn => Time.time < wrRingEnd || Time.time < wrRing2End;
    public Vector3 WrRingPos => wrRingPos;
    public float WrRingR => wrRingR;
    public float WrRingFill => WrRingOn ? Mathf.Clamp01((wrRingEnd - Time.time) / Mathf.Max(0.1f, wrRingLifeAll)) : 0f;
    public bool WrInRing(Vector3 p)
    {
        if (Time.time < wrRingEnd && ((Vector2)(p - wrRingPos)).sqrMagnitude <= wrRingR * wrRingR) return true;
        return Time.time < wrRing2End && ((Vector2)(p - wrRing2Pos)).sqrMagnitude <= wrRing2R * wrRing2R;      // 모래판 극의: 둘째(옛) 모래판
    }

    static Sprite WrSandTile()
    {
        if (wrSandSprite != null) return wrSandSprite;
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - 31.5f, dy = y - 31.5f;
                if (dx * dx + dy * dy > 31.5f * 31.5f) { px[y * n + x] = new Color32(0, 0, 0, 0); continue; }
                int h = ((x * 73856093) ^ (y * 19349663)) & 15;
                float k = h < 3 ? 0.86f : h > 13 ? 1.08f : 1f;
                px[y * n + x] = new Color32((byte)Mathf.Min(255, 232 * k), (byte)Mathf.Min(255, 194 * k), (byte)Mathf.Min(255, 122 * k), 255);
            }
        tex.SetPixels32(px);
        tex.Apply();
        wrSandSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return wrSandSprite;
    }

    // 천하장사가 메친 자리에 모래판 (이미 있으면 옛것이 사라지고 새것) — 도효 장인 · 천하 도효(진화)가 크기 · 지속을 바꿈
    public void WrStartRing(Vector3 at)
    {
        bool ringPath = WrestlerPaths.On(WrestlerPaths.Ring);       // 갈래 모래판 「도효 장인」: 반지름 7 · 지속 14초
        float r = (ringPath ? WrestlerPaths.RingRadiusSeed : WrRingRadius) * (SignatureSkills.IsEvo("wre.dohyo") ? 1.4f : 1f);
        float life = ringPath ? WrestlerPaths.RingLifeSeed : WrRingLife;
        if (Paths.Mastered(WrestlerPaths.Ring) && Time.time < wrRingEnd) WrDemoteRing();       // 극의: 옛 모래판을 지우지 않고 둘째 자리로
        wrRingBaseLife = life;
        wrFollowUsed = 0;
        wrSlideStart = -1f;
        wrRingPos = at;
        wrRingR = r;
        wrRingBorn = Time.time;
        wrRingLifeAll = life;
        wrRingEnd = Time.time + life;
        wrRingOuts = 0;
        if (wrRingObj != null) Destroy(wrRingObj);
        wrRingObj = new GameObject("SandRing");
        wrRingObj.transform.position = at;
        SpriteRenderer sr = wrRingObj.AddComponent<SpriteRenderer>();
        sr.sprite = WrSandTile();
        sr.color = new Color(1f, 1f, 1f, 0.5f);
        sr.sortingLayerName = "Background";
        sr.sortingOrder = 9;
        wrRingObj.transform.localScale = Vector3.one * (r * 2f);
        if (wrRingRope == null) { wrRingRope = Hostile.NewLine("SandRope", WrSand, 0.2f, 19); wrRingRope.loop = false; }
        wrRingRope.enabled = true;
    }

    // 모래판 시간을 늘림 (모래판 다지기 · 링아웃 연장) — 원래 지속의 두 배까지
    public void WrExtendRing(float sec)
    {
        if (!WrRingOn || sec <= 0f) return;
        float cap = wrRingBorn + wrRingLifeAll * 2f;
        float before = wrRingEnd;
        wrRingEnd = Mathf.Min(cap, wrRingEnd + sec);
        wrRingLifeAll = Mathf.Max(wrRingLifeAll, wrRingEnd - wrRingBorn);
        _ = before;
    }

    void WrRingTick()
    {
        WrRing2Tick();
        if (wrRingObj == null && wrRingRope == null) return;
        if (Time.time >= wrRingEnd)
        {
            if (wrRingObj != null) Destroy(wrRingObj);
            wrRingObj = null;
            if (wrRingRope != null) wrRingRope.enabled = false;
            return;
        }
        float left = wrRingEnd - Time.time;
        float frac = Mathf.Clamp01(left / Mathf.Max(0.1f, wrRingLifeAll));
        // 남은 시간 = 테가 시계 방향으로 사라짐 · 마지막 1초 깜빡
        bool blink = left < 1f && ((int)(Time.time * 10f) % 2 == 0);
        if (wrRingObj != null)
        {
            SpriteRenderer sr = wrRingObj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 1f, 1f, blink ? 0.25f : 0.5f);
        }
        if (wrRingRope != null)
        {
            Hostile.SetArc(wrRingRope, wrRingPos, wrRingR, 90f, 90f - 360f * frac);
            Color rc = HeroAim.ColorOf(HeroAimKind.Wrestler);
            wrRingRope.startColor = wrRingRope.endColor = blink ? new Color(1f, 1f, 1f, 0.5f) : new Color(WrSand.r, WrSand.g, WrSand.b, 0.95f);
            wrRingRope.startWidth = wrRingRope.endWidth = 0.2f;
            _ = rc;
        }
        // 모래 둑(card 3): 모래판 경계를 넘어 들어오는 적이 모래에 발이 빠져 잠깐 크게 느려짐 (70%)
        if (card[3] > 0 && Time.time >= wrLevee)
        {
            wrLevee = Time.time + 0.15f;
            float sink = 0.6f + 0.3f * (card[3] - 1);
            foreach (Collider2D c in Specials.Overlap(wrRingPos, wrRingR + 0.3f, wrHits))
            {
                if (c == null || !c.CompareTag("enermy") || !c.TryGetComponent(out EnermyController e) || e.holder != null) continue;
                float d = Vector2.Distance(c.transform.position, wrRingPos);
                if (d > wrRingR - 1f && d <= wrRingR + 0.3f) e.Slow(0.3f, sink);
            }
        }
    }

    // 링아웃: 잡몹은 쓰러짐(남은 체력보다 큰 피해 — KillBySkill 을 쓰지 않아 보호막 · 불사 기믹을 존중) · 무거운 적은 최대 체력 20% + 1초 넘어짐 · 중간 보스 6% · 보스는 해당 없음
    public void WrRingOut(EnermyController e, Vector3 at, float mul = 1f)
    {
        if (e == null || e.IsDead) return;
        int tier = WrTier(e);
        float amount;
        if (e.IsMidBoss) amount = e.setEnemyHP * 0.06f * mul;
        else if (tier >= 2) { amount = e.setEnemyHP * (WrestlerPaths.On(WrestlerPaths.Ring) ? WrestlerPaths.RingHeavySeed : 0.2f) * mul; e.Slow(0f, 1f); }
        else amount = e.EnemyHealth * 4f + 20f;
        Paths.Deal(e.gameObject, amount, Vector3.down, 0f, "링아웃");
        wrRingOuts++;
        WrPathRingOut();                // 갈래 모래판: 게이지 · 8번마다 지속 가득
        RunCounters.Both("ringout");
        SpecialAbilities.SharedFx?.FloatText(at + Vector3.up * 1.6f, Loc.T("장외!"), WrGold, 5.5f, 0.15f);
        Fx.Spawn(WFx("fx_ringout", "fx_sparkle"), at, 3.4f, WrGold, 18f);
        Impact.Hit(ImpactTier.Attack, at, Vector2.up);
        WrCheerLater(0.08f, 0.45f);
        SignatureSkills.WrRingOut(at, tier, wrRingOuts);
    }

    void WrCheerLater(float delay, float vol) => StartCoroutine(WrCheerRoutine(delay, vol));

    IEnumerator WrCheerRoutine(float delay, float vol)
    {
        yield return new WaitForSecondsRealtime(delay);
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        if (fx == null || Impact.Off || Time.timeScale == 0f) yield break;
        Impact.Sounds.Register(fx);
        fx.PlayRaw(Impact.Sounds.Cheer, vol, Random.Range(0.95f, 1.1f));
    }

    // ================================================================= 표시 (잡을 후보 고리 · 던질 길)
    void WrDrawMarks(bool throwing)
    {
        if (Time.timeScale == 0f) { WrHideMarks(); return; }
        if (!throwing && WrCarrying) { WrHideMarks(); return; }
        Color hero = HeroAim.ColorOf(HeroAimKind.Wrestler);
        if (!WrCarrying)
        {
            // 빈손: 잡을 후보 발밑에 샅바 고리
            Collider2D cand = Time.timeScale > 0f ? WrCandidate(((Vector2)(Mouse - transform.position)).normalized, Mouse) : null;
            if (wrMarkRing == null) { wrMarkRing = Hostile.NewLine("GrabMark", hero, 0.1f, 22); wrMarkRing.loop = true; }
            if (cand == null) { wrMarkRing.enabled = false; if (wrAimLine != null) wrAimLine.enabled = false; if (wrAimRing != null) wrAimRing.enabled = false; return; }
            wrMarkRing.enabled = true;
            Hostile.SetArc(wrMarkRing, cand.transform.position, WrBodyR(cand) + 0.4f, 0f, 360f);
            wrMarkRing.startColor = wrMarkRing.endColor = new Color(hero.r, hero.g, hero.b, 0.55f + 0.3f * Mathf.Sin(Time.time * 10f));
            wrMarkRing.startWidth = wrMarkRing.endWidth = 0.1f;
            HeroAim.Mark(wrMarkRing, HeroAimKind.Wrestler, HeroAimFx.None);
            if (wrAimLine != null) wrAimLine.enabled = false;
            if (wrAimRing != null) wrAimRing.enabled = false;
            return;
        }
        // 들고 있음: 커서 쪽 던질 길(길이 = 그 무게의 던짐 거리) · 끝 원 = 떨어질 자리 (모래판 밖이면 금색)
        if (wrMarkRing != null) wrMarkRing.enabled = false;
        if (wrAimLine == null) { wrAimLine = Hostile.NewLine("ThrowLine", hero, 0.12f, 22); }
        if (wrAimRing == null) { wrAimRing = Hostile.NewLine("ThrowEnd", hero, 0.1f, 22); wrAimRing.loop = true; }
        Vector3 from = transform.position;
        Vector2 dir = ((Vector2)(Mouse - from)).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = body != null && body.flipX ? Vector2.left : Vector2.right;
        WrWeight w = WrWeightOf(wrCarried, true);
        Vector3 end = from;
        float reach = WrThrowDist(w);
        float len = reach;
        for (float d = 0.5f; d <= reach; d += 0.5f)
        {
            Vector3 q = from + (Vector3)(dir * d);
            if (Hostile.IsWall(q) || (Hostile.ClampArena(q) - q).sqrMagnitude > 0.0001f) { len = d - 0.5f; break; }
        }
        end = from + (Vector3)(dir * len);
        bool out2 = WrRingOn && WrInRing(from) && !WrInRing(end);
        Color c = out2 ? WrGold : hero;
        c.a = 0.75f;
        wrAimLine.enabled = true;
        wrAimLine.positionCount = 2;
        wrAimLine.SetPosition(0, from);
        wrAimLine.SetPosition(1, end);
        wrAimLine.startColor = wrAimLine.endColor = c;
        wrAimLine.startWidth = wrAimLine.endWidth = 0.14f;
        HeroAim.Mark(wrAimLine, HeroAimKind.Wrestler, HeroAimFx.None);
        wrAimRing.enabled = true;
        Hostile.SetArc(wrAimRing, end, WrestlerPaths.On(WrestlerPaths.Slam) ? WrSlamRadiusOf(w, false) : w.ballR, 0f, 360f);
        wrAimRing.startColor = wrAimRing.endColor = c;
        wrAimRing.startWidth = wrAimRing.endWidth = 0.1f;
        HeroAim.Mark(wrAimRing, HeroAimKind.Wrestler, HeroAimFx.None);
    }

    void WrHideMarks()
    {
        if (wrMarkRing != null) wrMarkRing.enabled = false;
        if (wrAimLine != null) wrAimLine.enabled = false;
        if (wrAimRing != null) wrAimRing.enabled = false;
    }

    // 머리 위 막대 (CharacterKit.Rules): 들고 있으면 들기 남은 시간(마지막 1초 붉게 두근) · 빈손이면 모래판 남은 시간(모래색)
    void WrRuleBar()
    {
        if (WrCarrying)
        {
            float left = wrCarryEnd - Time.time;
            Color hero = CharacterData.Ally(CharacterId.Wrestler);
            Color c = left < 1f ? Color.Lerp(new Color(1f, 0.3f, 0.25f), Color.white, 0.4f + 0.4f * Mathf.Sin(Time.time * 18f)) : hero;
            DrawRuleBar(WrCarryFill, c, true, "들기");
        }
        else if (WrRingOn) DrawRuleBar(WrRingFill, WrSand, true, "모래판");
        else DrawRuleBar(0f, WrSand, false, "모래판");
    }

    static string WFx(string wanted, string fallback) => Fx.Frames(wanted).Length > 0 ? wanted : fallback;

    void OnDestroyWrestler()
    {
        EnemyHold.ReleaseAll(this);
        wrCarried = null;
        wrExtra.Clear();
        wrBalls.Clear();
        WrPathCleanup();
        if (wrRingObj != null) Destroy(wrRingObj);
        if (wrRingRope != null) Destroy(wrRingRope.gameObject);
        if (wrAimLine != null) Destroy(wrAimLine.gameObject);
        if (wrAimRing != null) Destroy(wrAimRing.gameObject);
        if (wrMarkRing != null) Destroy(wrMarkRing.gameObject);
        if (wrUltLine != null) Destroy(wrUltLine.gameObject);
        if (wrUltRing != null) Destroy(wrUltRing.gameObject);
        if (wrUltRing2 != null) Destroy(wrUltRing2.gameObject);
    }
}
