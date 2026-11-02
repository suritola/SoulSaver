using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 씨름꾼 갈래 효과 — 키트 쪽 (2.0.0 · 기획서 hero-wrestler.md 9절 · Paths/Paths.Wrestler.cs 가 수치 · 문장)
//   볼링: 핀 상한 ×2 · 거리 +25% · 옆으로 3.5칸 · 되튕김 · 볼 터짐 · 왕복      → CharacterKit.Wrestler (WrFlight · WrLand · WrStrike) + 여기 도우미
//   모래판: 반지름 7 · 14초 · 35% · 게이지 · 경계 미끄러짐 · 8번 가득 · 둘째 모래판 · 모래판 끌기(31번 카드)  → 여기 (WrPathTick · WrPathRingOut)
//   인간 방패: 10초 · −30% · 되튕김 · 방패 폭발 · 보스 탄 게이지               → CharacterKit.Wrestler (WrBlock) + 여기
//   엎어치기: 던지는 대신 앞 바닥에 내리꽂음 · 웅덩이 · 뒤집기 · 되메치기        → 여기 (WrSlamDown)
//   장사: 중간 보스 들기 · 무게 +0.5 · 배지기 +40% · 끌려옴 · 들썩임 · 두 배    → CharacterKit.Wrestler (WrWeightOf · WrBelly · WrFlight · WrLand)
// 상태는 모두 「끝나는 시각」 값 (켜고 끄는 코루틴 깃발 없음) · 만드는 것에는 개수 상한 (웅덩이 8 · 모래판 2)
public partial class CharacterKit
{
    // ---------------- 상태 (끝나는 시각 · 센 수)
    float wrRing2End, wrRing2R, wrRing2Life;           // 모래판 극의: 둘째(옛) 모래판
    Vector3 wrRing2Pos;
    GameObject wrRing2Obj;
    LineRenderer wrRing2Rope;
    float wrRingBaseLife;                              // 이번 모래판의 원래 지속 (8번 가득 채울 때)
    int wrRingOutAll;                                  // 이번 판 링아웃 수 (8번마다 가득)
    float wrRingGaugeAt;                               // 링아웃 게이지 0.5초에 한 번
    int wrFollowUsed;                                  // 모래판 끌기: 이번 모래판이 따라온 횟수
    float wrSlideStart = -1f;
    Vector3 wrSlideFrom, wrSlideTo;
    float wrEdgeAt, wrMudAt, wrReflectAt, wrSlamChainAt, wrBossGauge;
    int wrSlamChain, wrGiantBellies;
    bool wrInstantLift;                                // 방패가 터진 뒤 다음 들기만 즉시
    readonly List<WrMud> wrMud = new List<WrMud>();
    readonly List<EnermyController> wrTmp = new List<EnermyController>();

    struct WrMud { public Vector3 pos; public float r, until; }

    public int WrRingOutAll => wrRingOutAll;

    // ---------------- 갈래가 바꾸는 수치
    float WrCarryLimit => WrestlerPaths.On(WrestlerPaths.Shield) ? WrestlerPaths.ShieldHoldMax : WrCarryMax;

    // 던짐 거리 (조준선 · 비행 모두): 볼링 +25% · 엎어치기는 앞 1.5칸
    float WrThrowDist(WrWeight w) => WrestlerPaths.On(WrestlerPaths.Slam) ? WrestlerPaths.SlamAhead : w.dist * (WrestlerPaths.On(WrestlerPaths.Bowl) ? WrestlerPaths.BowlRange : 1f);

    // 대가: 인간 방패의 던짐 · 핀 피해 −25% · 장사의 가벼운 적 던짐 −40%
    float WrPathThrowMul(WrWeight w)
    {
        float m = 1f;
        if (WrestlerPaths.On(WrestlerPaths.Shield)) m *= WrestlerPaths.ShieldThrowCost;
        if (w.tier == 0 && WrestlerPaths.On(WrestlerPaths.Giant)) m *= WrestlerPaths.GiantLightCost;
        return m;
    }

    // 볼링 공명 II: 보스를 맞힌 볼은 그때까지 쓰러뜨린 핀 하나마다 보스 피해 +10% (최대 +80%)
    float WrBowlBossMul(WrBall b)
    {
        if (Paths.Res(WrestlerPaths.Bowl) < 2 || !WrestlerPaths.On(WrestlerPaths.Bowl)) return 1f;
        return 1f + Mathf.Min(WrestlerPaths.BowlBossCap, WrestlerPaths.BowlBossPerPin * b.pinCount) * Paths.Power(WrestlerPaths.Bowl);
    }

    float WrSlamRadiusOf(WrWeight w, bool flip) => (WrestlerPaths.SlamRadius + WrestlerPaths.SlamRadiusPerTier * w.tier) * (flip ? WrestlerPaths.SlamFlipRadius : 1f);

    // ---------------- 볼링 공명 I: 되튕김
    // 이 점이 들어 있는 모래판의 가운데 (첫째 → 둘째)
    bool WrRingAt(Vector3 p, out Vector3 center)
    {
        if (Time.time < wrRingEnd && ((Vector2)(p - wrRingPos)).sqrMagnitude <= wrRingR * wrRingR) { center = wrRingPos; return true; }
        if (Time.time < wrRing2End && ((Vector2)(p - wrRing2Pos)).sqrMagnitude <= wrRing2R * wrRing2R) { center = wrRing2Pos; return true; }
        center = Vector3.zero;
        return false;
    }

    // 경계에서 안쪽으로 반사
    static Vector2 WrReflectRing(Vector2 dir, Vector3 p, Vector3 center)
    {
        Vector2 inward = ((Vector2)(center - p)).normalized;
        return Vector2.Reflect(dir, inward);
    }

    // 벽 · 경기장 끝에서 튕김: 가로 → 세로 → 되돌아가기 순으로 비어 있는 방향 (없으면 zero = 되튕기지 않음)
    static Vector2 WrBounceWall(Vector3 p, Vector2 dir, float step)
    {
        Vector2 a = new Vector2(-dir.x, dir.y);
        if (WrFree(p + (Vector3)(a.normalized * step * 2f))) return a;
        Vector2 b = new Vector2(dir.x, -dir.y);
        if (WrFree(p + (Vector3)(b.normalized * step * 2f))) return b;
        Vector2 c = -dir;
        return WrFree(p + (Vector3)(c.normalized * step * 2f)) ? c : Vector2.zero;
    }

    static bool WrFree(Vector3 q) => !Hostile.IsWall(q) && (Hostile.ClampArena(q) - q).sqrMagnitude < 0.0001f;

    // ---------------- 인간 방패: 막을 때마다 (공명 I 되튕김 · 공명 II 보스 탄 게이지)
    void WrBlockedPath(Vector3 hitAt)
    {
        if (!WrestlerPaths.On(WrestlerPaths.Shield)) return;
        if (Paths.Res(WrestlerPaths.Shield) >= 1 && Time.time >= wrReflectAt && SignatureSkills.Instance != null)
        {
            wrReflectAt = Time.time + 0.1f;      // 탄막이 빽빽해도 되튕김 탄은 0.1초에 하나
            Vector2 d = ((Vector2)(Mouse - hitAt)).normalized;
            if (d.sqrMagnitude < 0.01f) d = Vector2.up;
            SignatureSkills.Instance.Shot(hitAt, d, Paths.Atk * WrestlerPaths.ShieldReflectRate * Paths.Power(WrestlerPaths.Shield), 1, 16f, 14f, "fx_spark", 0.7f, WrSand);
        }
        // 보스가 있는 동안 막은 탄은 보스 탄으로 셈 (탄에 쏜 주인 표시가 없음) — 한 번 들 때 최대 10%
        if (Paths.Res(WrestlerPaths.Shield) >= 2 && wrBossGauge < WrestlerPaths.ShieldBossGaugeCap && player != null && player.skillGauge != null
            && GameObject.FindGameObjectWithTag("boss") != null)
        {
            wrBossGauge += WrestlerPaths.ShieldBossGauge;
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * WrestlerPaths.ShieldBossGauge);
        }
    }

    // 공명 II: 방패가 닳아 쓰러지면 터지고(2.5칸 · 공격력 150%) 다음 들기가 즉시
    void WrShieldBurst(Vector3 at)
    {
        if (Paths.Res(WrestlerPaths.Shield) < 2 || !WrestlerPaths.On(WrestlerPaths.Shield)) return;
        Paths.Circle(at, WrestlerPaths.ShieldBurstRadius, Paths.Atk * WrestlerPaths.ShieldBurstRate * Paths.Power(WrestlerPaths.Shield), 3f, WrestlerPaths.BurstSource);
        Fx.Spawn(WFx("fx_sandwave", "fx_shock"), at, WrestlerPaths.ShieldBurstRadius * 2.2f, new Color(0.6f, 0.75f, 1f, 0.9f), 18f);
        Play("boom", 0.4f, 1.2f);
        Impact.Hit(ImpactTier.Attack, at, Vector2.up);
        wrInstantLift = true;
    }

    // ---------------- 엎어치기: 던지는 대신 앞 1.5칸 바닥에 내리꽂음
    void WrSlamDown(EnermyController e, List<EnermyController> mates, Vector2 dir, float dmg, bool inRing, WrWeight w)
    {
        Vector3 me = transform.position;
        Vector3 spot = Hostile.ClampArena(me + (Vector3)(dir * WrestlerPaths.SlamAhead));
        if (Hostile.IsWall(spot)) spot = me;
        // 공명 II: 2초 안에 세 번 메치면 세 번째가 뒤집기 (반경 ×1.6 · 피해 ×2)
        bool flip = false;
        if (Paths.Res(WrestlerPaths.Slam) >= 2)
        {
            if (Time.time - wrSlamChainAt > WrestlerPaths.SlamChainWindow) wrSlamChain = 0;
            if (wrSlamChain == 0) wrSlamChainAt = Time.time;
            wrSlamChain++;
            if (wrSlamChain >= 3) { flip = true; wrSlamChain = 0; }
        }
        float radius = WrSlamRadiusOf(w, flip);
        float hit = dmg * WrestlerPaths.SlamRate * w.W * Paths.Power(WrestlerPaths.Slam) * (flip ? WrestlerPaths.SlamFlipRate : 1f);

        // 든 적(들) 내려놓기
        if (e != null) { e.transform.position = EnemyHold.FreeSpot(spot); EnemyHold.Release(e, this); }
        for (int i = 0; i < mates.Count; i++)
        {
            EnermyController x = mates[i];
            if (x == null || x.IsDead) continue;
            Vector2 off = new Vector2(i % 2 == 0 ? 0.8f : -0.8f, 0.2f * (i / 2));
            x.transform.position = EnemyHold.FreeSpot(spot + (Vector3)off);
            EnemyHold.Release(x, this);
        }

        // 충격: 반지름 안의 적 · 보스에게 피해 (보스 2칸 안이면 ×1.5) → 든 적 말고 잡몹 · 큰 적은 바깥으로 밀침
        wrTmp.Clear();
        using (var ovl = PhysHits.Circle(spot, radius, true)) foreach (Collider2D c in ovl)
        {
            if (c == null || !Paths.IsFoe(c)) continue;
            bool boss = c.CompareTag("boss");
            float d = hit;
            if (Paths.Res(WrestlerPaths.Slam) >= 2 && Paths.IsBoss(c.gameObject) && Vector2.Distance(spot, c.ClosestPoint(spot)) <= WrestlerPaths.SlamBossNear) d *= WrestlerPaths.SlamBossMul;
            Vector2 away = ((Vector2)(c.transform.position - spot)).normalized;
            using (DamageSource.As(DamageSource.Basic)) Specials.Damage(c.gameObject, d, away, 0f);
            if (!boss && c != null && c.TryGetComponent(out EnermyController ec) && ec != e && !ec.IsDead && ec.holder == null && ec.swarm == null) wrTmp.Add(ec);
        }
        foreach (EnermyController ec in wrTmp)
        {
            if (ec == null || ec.IsDead) continue;
            Vector3 from = ec.transform.position;
            Vector2 away = ((Vector2)(from - spot)).normalized;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitCircle.normalized;
            Vector3 np = Hostile.ClampArena(from + (Vector3)(away * WrestlerPaths.SlamPush));
            if (Hostile.IsWall(np)) continue;
            ec.transform.position = np;
            if (WrInRing(from) && !WrInRing(np)) WrRingOut(ec, np);         // 경계를 넘으면 링아웃
        }
        wrTmp.Clear();
        // 모래판 안에서 출발해 밖에 메쳐졌으면 링아웃
        if (inRing && e != null && !e.IsDead && WrRingOn && !WrInRing(spot)) WrRingOut(e, spot);

        Fx.Spawn(WFx("fx_sandpuff", "fx_shock"), spot, radius * 2f, WrSand, 20f);
        Fx.Spawn(WFx("fx_sandwave", "fx_shock"), spot, radius * (flip ? 2.6f : 2f), new Color(WrSand.r, WrSand.g, WrSand.b, 0.7f), 20f);
        Play("thump", flip ? 0.9f : 0.7f, flip ? 0.65f : 0.8f);
        Hostile.Shake(flip ? 0.2f : 0.08f);
        Impact.Hit(flip ? ImpactTier.Skill : ImpactTier.Attack, spot, Vector2.up);
        PlayerMotion.Of(this).Pound(dir);
        RunCounters.Both("throw");
        if (w.tier >= 2) RunCounters.Add("heavy.throw");
        wrGap = -1f;

        if (card[6] > 0) WrTrip(spot, 1.5f, 0.4f + 0.2f * (card[6] - 1), e);                  // 발 걸기
        if (SignatureSkills.IsEvo("wre.slam")) WrThud(spot, 2f, 0.5f);                          // 천지 메치기
        if (augment == AugFlip && e != null && !e.IsDead) StartCoroutine(WrFlipBack(e, spot, dmg * 0.5f * w.W));      // 뒤집기(강화)
        // 공명 I: 메친 자리에 2초 모래 웅덩이 (개수 상한 8 — 넘치면 가장 오래된 것부터 버림)
        if (Paths.Res(WrestlerPaths.Slam) >= 1)
        {
            while (wrMud.Count >= 8) wrMud.RemoveAt(0);
            wrMud.Add(new WrMud { pos = spot, r = radius, until = Time.time + WrestlerPaths.SlamMudLife });
        }
        // 교차 도효 다지기: 모래판 안에서 메친 충격이 경계까지 번짐
        if (Paths.CrossOn(WrestlerPaths.TampCross) && Time.time < wrRingEnd && ((Vector2)(spot - wrRingPos)).sqrMagnitude <= wrRingR * wrRingR) WrTamp();
        // 극의: 메친 적이 튀어 올라 한 번 더 메쳐짐
        if (Paths.Mastered(WrestlerPaths.Slam) && e != null && !e.IsDead) StartCoroutine(WrSlamAgain(spot, radius, hit));
        SignatureSkills.WrLanded(spot, 0);
        PlayerMotion.Of(this).Throw(0.3f, dir);
        Play("whoosh", 0.6f, 0.7f);
    }

    IEnumerator WrSlamAgain(Vector3 spot, float radius, float hit)
    {
        yield return new WaitForSeconds(0.35f);
        if (player == null || player.IsDying) yield break;
        using (DamageSource.As(DamageSource.Basic)) DamageCircle(spot, radius, hit, 0.5f);
        Fx.Spawn(WFx("fx_sandpuff", "fx_shock"), spot, radius * 2f, WrSand, 20f);
        Play("thump", 0.5f, 1f);
    }

    // 도효 다지기: 경계 1칸 안의 적(보스 제외)을 바깥으로 1.5칸 밀어냄 (잡몹이 넘으면 링아웃)
    void WrTamp()
    {
        wrTmp.Clear();
        foreach (Collider2D c in Specials.Overlap(wrRingPos, wrRingR + 0.5f, wrHits))
        {
            if (c == null || !c.CompareTag("enermy") || !c.TryGetComponent(out EnermyController e) || e.IsDead || e.holder != null || e.swarm != null) continue;
            float d = ((Vector2)(c.transform.position - wrRingPos)).magnitude;
            if (d > wrRingR - WrestlerPaths.TampEdge && d <= wrRingR) wrTmp.Add(e);
        }
        foreach (EnermyController e in wrTmp)
        {
            if (e == null || e.IsDead) continue;
            Vector3 from = e.transform.position;
            Vector2 away = ((Vector2)(from - wrRingPos)).normalized;
            Vector3 np = Hostile.ClampArena(from + (Vector3)(away * WrestlerPaths.TampPush));
            if (Hostile.IsWall(np)) continue;
            e.transform.position = np;
            if (!WrInRing(np)) WrRingOut(e, np);
        }
        wrTmp.Clear();
    }

    // ---------------- 모래판: 링아웃마다 (게이지 · 8번마다 지속 가득)
    void WrPathRingOut()
    {
        wrRingOutAll++;
        if (!WrestlerPaths.On(WrestlerPaths.Ring)) return;
        // 입문: 링아웃마다 필살기 게이지 +0.5% (0.5초에 한 번 — 집단 링아웃이 게이지를 끝없이 되돌리지 않게)
        if (Time.time >= wrRingGaugeAt && player != null && player.skillGauge != null)
        {
            wrRingGaugeAt = Time.time + WrestlerPaths.RingGaugeGap;
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * WrestlerPaths.RingGaugeSeed);
        }
        // 공명 II: 링아웃 8번마다 모래판 지속이 가득 참
        if (Paths.Res(WrestlerPaths.Ring) >= 2 && wrRingOutAll % WrestlerPaths.RingRefillEvery == 0 && Time.time < wrRingEnd)
        {
            wrRingBorn = Time.time;
            wrRingLifeAll = wrRingBaseLife;
            wrRingEnd = Time.time + wrRingBaseLife;
            Fx.Spawn("fx_sparkle", wrRingPos, wrRingR * 0.8f, new Color(WrGold.r, WrGold.g, WrGold.b, 0.6f), 20f);
        }
    }

    // ---------------- 모래판 극의: 둘째(옛) 모래판
    void WrDemoteRing()
    {
        if (wrRing2Obj != null) Destroy(wrRing2Obj);
        if (wrRing2Rope != null) Destroy(wrRing2Rope.gameObject);
        wrRing2Obj = wrRingObj;
        wrRing2Rope = wrRingRope;
        wrRingObj = null;
        wrRingRope = null;
        wrRing2Pos = wrRingPos;
        wrRing2R = wrRingR;
        wrRing2End = wrRingEnd;
        wrRing2Life = wrRingLifeAll;
    }

    // 둘째 모래판 그리기 · 끝나면 치움
    void WrRing2Tick()
    {
        if (wrRing2Obj == null && wrRing2Rope == null) return;
        if (Time.time >= wrRing2End)
        {
            if (wrRing2Obj != null) Destroy(wrRing2Obj);
            if (wrRing2Rope != null) Destroy(wrRing2Rope.gameObject);
            wrRing2Obj = null;
            wrRing2Rope = null;
            return;
        }
        float left = wrRing2End - Time.time;
        float frac = Mathf.Clamp01(left / Mathf.Max(0.1f, wrRing2Life));
        bool blink = left < 1f && ((int)(Time.time * 10f) % 2 == 0);
        if (wrRing2Obj != null)
        {
            SpriteRenderer sr = wrRing2Obj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 1f, 1f, blink ? 0.25f : 0.5f);
        }
        if (wrRing2Rope != null)
        {
            Hostile.SetArc(wrRing2Rope, wrRing2Pos, wrRing2R, 90f, 90f - 360f * frac);
            wrRing2Rope.startColor = wrRing2Rope.endColor = blink ? new Color(1f, 1f, 1f, 0.5f) : new Color(WrSand.r, WrSand.g, WrSand.b, 0.95f);
            wrRing2Rope.startWidth = wrRing2Rope.endWidth = 0.2f;
        }
    }

    // ---------------- 매 프레임 (WrLate): 모래 웅덩이 · 경계 미끄러짐 · 모래판 끌기
    void WrPathTick()
    {
        if (Time.timeScale == 0f) return;
        float now = Time.time;
        // 엎어치기 공명 I: 웅덩이 안의 적이 40% 느려짐 (0.2초마다 0.4초씩 — 나가면 곧 풀림)
        if (wrMud.Count > 0 && now >= wrMudAt)
        {
            wrMudAt = now + 0.2f;
            for (int i = wrMud.Count - 1; i >= 0; i--)
            {
                WrMud m = wrMud[i];
                if (now >= m.until) { wrMud.RemoveAt(i); continue; }
                foreach (Collider2D c in Specials.Overlap(m.pos, m.r, wrHits))
                    if (c != null && c.CompareTag("enermy") && c.TryGetComponent(out EnermyController e) && e.holder == null) e.Slow(WrestlerPaths.SlamMudSlow, 0.4f);
            }
        }
        if (now >= wrRingEnd) { wrSlideStart = -1f; return; }

        // 모래판 공명 I: 경계에 걸친 잡몹이 0.5초마다 바깥으로 1칸 미끄러짐 (밖으로 나가면 링아웃)
        if (Paths.Res(WrestlerPaths.Ring) >= 1 && WrestlerPaths.On(WrestlerPaths.Ring) && now >= wrEdgeAt)
        {
            wrEdgeAt = now + WrestlerPaths.RingSlideGap;
            wrTmp.Clear();
            foreach (Collider2D c in Specials.Overlap(wrRingPos, wrRingR + 0.5f, wrHits))
            {
                if (c == null || !c.CompareTag("enermy") || !c.TryGetComponent(out EnermyController e) || e.IsDead || e.holder != null || e.swarm != null || WrTier(e) != 0) continue;
                float d = ((Vector2)(c.transform.position - wrRingPos)).magnitude;
                if (d > wrRingR - 1f && d <= wrRingR) wrTmp.Add(e);
            }
            foreach (EnermyController e in wrTmp)
            {
                if (e == null || e.IsDead) continue;
                Vector2 away = ((Vector2)(e.transform.position - wrRingPos)).normalized;
                Vector3 np = Hostile.ClampArena(e.transform.position + (Vector3)(away * WrestlerPaths.RingSlideStep));
                if (Hostile.IsWall(np)) continue;
                e.transform.position = np;
                if (!WrInRing(np)) WrRingOut(e, np);
            }
            wrTmp.Clear();
        }

        // 31번 카드 「모래판 끌기」: 내가 모래판 밖으로 나가면 모래판이 0.6초 동안 내 쪽으로 미끄러져 옴 (남은 지속 그대로 · 모래판 하나마다 1/2/3회, 외길 4회)
        if (wrSlideStart >= 0f)
        {
            float k = Mathf.Clamp01((now - wrSlideStart) / WrestlerPaths.RingFollowDelay);
            wrRingPos = Vector3.Lerp(wrSlideFrom, wrSlideTo, k);
            if (wrRingObj != null) wrRingObj.transform.position = wrRingPos;
            if (k >= 1f) wrSlideStart = -1f;
        }
        else if (player != null && !player.IsDying && wrFollowUsed < Paths.Level(Paths.ExtraId))
        {
            Vector3 me = transform.position;
            Vector2 off = wrRingPos - me;
            float dist = off.magnitude;
            if (dist > wrRingR + 0.2f)
            {
                wrFollowUsed++;
                wrSlideStart = now;
                wrSlideFrom = wrRingPos;
                wrSlideTo = me + (Vector3)(off / dist * (wrRingR - 0.5f));        // 경계가 나에게 닿을 때까지
                Play("hum", 0.3f, 1.2f);
            }
        }
    }

    // OnDestroyWrestler 에서: 둘째 모래판 · 웅덩이 치움
    void WrPathCleanup()
    {
        if (wrRing2Obj != null) Destroy(wrRing2Obj);
        if (wrRing2Rope != null) Destroy(wrRing2Rope.gameObject);
        wrRing2Obj = null;
        wrRing2Rope = null;
        wrMud.Clear();
        wrTmp.Clear();
    }
}
