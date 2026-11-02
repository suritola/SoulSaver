using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 장의사 갈래 효과 — 키트 쪽 (2.0.0 · 기획서 hero-undertaker.md 9절 · Paths/Paths.Undertaker.cs 가 수치 · 문장)
//   생매장: 갇힘 2.2초 · 갇힌 적 매장 ×1.35 · 파기 속도 ×0.7 · 갇힌 둘 이상 구멍 +0.5칸 · 보스 피해 가산 · 극의 갇힘 3초 / 구멍 +1  → UtPath* 값 + UtBuryOne 이 물음
//   묘역 설계: 빈 묘비 · 상한 14 · 수명 35 · 매장 ×0.7 · 비문 +10%(SignatureSkills.UtOutgoing) · 보스가 묘비를 부수면 흙 · 극의 내구 ×2 / 상한 20
//   땅굴: 내 구멍에 걸어 들어가면 최근 다른 구멍에서 튀어나옴 · 구멍 −1 · 받는 피해 +10%(UndertakerPaths.Taken) · 곧바로 가장 크게 · 직선 위 적 · 극의 쉼 0.8초
//   장례식: 구멍 수명 3초 · 장례 범위 +4칸(SignatureSkills.UtFuneralPad) · 장례 뒤 6초 가속 · 임시 묘비 무너짐 피해 · 극의 장례 한 번 더
//   도굴꾼: 묘비 위에 파면 무너짐 150% + 가장 큰 구멍 · 수명 8초 · 파편 셋 · 보스 가까이 ×1.5 · 극의 이어 무너짐
//   교차: 합동 장례(UtFuneralWait) · 저승길(UtPathTick)
// 상태는 모두 「끝나는 시각」 값 (켜고 끄는 코루틴 깃발 없음) · 만드는 것에는 개수 상한 (파편 3 · 이어 무너짐 4)
public partial class CharacterKit
{
    static bool UtpOn(int path) => UndertakerPaths.On(path);

    float utTunnelAt, utFuneralBoostUntil, utUnderworldUntil, utBossBreakAt;
    bool utTunnelInside;

    // ================================================================= 갈래 자리 값 (CharacterKit.Undertaker 가 읽음)
    int UtPathHoles => (UtpOn(UndertakerPaths.Tunnel) ? -UndertakerPaths.TunnelHoleCost : 0)
                     + (UtpOn(UndertakerPaths.Live) && Paths.Mastered(UndertakerPaths.Live) ? UndertakerPaths.LiveMasteryHoles : 0);

    int UtPathGraveCap => UtpOn(UndertakerPaths.Yard)
        ? (Paths.Mastered(UndertakerPaths.Yard) ? UndertakerPaths.YardMasteryCap : UndertakerPaths.YardCap) - UndertakerTuning.GraveCap : 0;

    float UtPathTrap => UtpOn(UndertakerPaths.Live)
        ? (Paths.Mastered(UndertakerPaths.Live) ? UndertakerPaths.LiveMasteryTrap : UndertakerPaths.LiveTrap) - UndertakerTuning.TrapSec : 0f;

    float UtPathGrow => (UtpOn(UndertakerPaths.Live) ? UndertakerPaths.LiveDigSpeed : 1f) * (UtFuneralBoost ? UndertakerPaths.FuneralRes1Dig : 1f);

    float UtPathLife => UtpOn(UndertakerPaths.Funeral) ? UndertakerPaths.FuneralHoleLife - UndertakerTuning.HoleLife : 0f;

    // 매장 피해 배율: 묘역 설계 대가 −30%
    float UtPathBuryMul => UtpOn(UndertakerPaths.Yard) ? UndertakerPaths.YardBuryCost : 1f;

    // 갇힌 적에게 주는 매장 피해 (생매장 입문 · 장례의 일제 매장에는 붙이지 않음)
    float UtPathTrappedMul(bool fromUlt) => !fromUlt && UtpOn(UndertakerPaths.Live) ? UndertakerPaths.LiveBuryMul : 1f;

    // 갇힌 적이 2마리 이상인 구멍은 반지름 +0.5칸 (생매장 공명 I · 파는 중에는 아님)
    float UtPathHoleR(UtHole h) =>
        h != null && !h.growing && !h.pitfall && h.traps.Count >= UndertakerPaths.LiveRes1Need
        && UtpOn(UndertakerPaths.Live) && Paths.Res(UndertakerPaths.Live) >= 1 ? UndertakerPaths.LiveRes1Grow : 0f;

    // 보스 매장 피해 가산: 구멍에 갇힌 적 하나당 +15% (최대 +60%) — 생매장 공명 II
    float UtPathBossMul(int trapped)
    {
        if (!UtpOn(UndertakerPaths.Live) || Paths.Res(UndertakerPaths.Live) < 2 || trapped <= 0) return 1f;
        return 1f + Mathf.Min(UndertakerPaths.LiveRes2Cap, UndertakerPaths.LiveRes2PerTrapped * trapped) * Paths.Power(UndertakerPaths.Live);
    }

    // 묘비 수명 (더해지는 값) · 내구 배율 — SignatureSkills.UtGraveLifeExtra 가 부름
    public float UtPathGraveLife
    {
        get
        {
            float v = 0f;
            if (UtpOn(UndertakerPaths.Yard)) v += UndertakerPaths.YardLife - UndertakerTuning.GraveLife;
            if (UtpOn(UndertakerPaths.Robber)) v += UndertakerPaths.RobberGraveLife - UndertakerTuning.GraveLife;
            return v;
        }
    }

    int UtPathGraveHp(int hp) => UtpOn(UndertakerPaths.Yard) && Paths.Mastered(UndertakerPaths.Yard) ? hp * UndertakerPaths.YardMasteryHpMul : hp;

    bool UtFuneralBoost => Time.time < utFuneralBoostUntil;
    public bool UtNoRestNow => UtFuneralBoost;               // 장례식 공명 I: 매장 뒤 쉼 없음
    public float UtTunnelLeft => Mathf.Max(0f, utTunnelAt - Time.time);

    // ================================================================= 묘역 설계
    // 빈 구멍을 매장하면 빈 묘비 (UtBuryOne 이 부름)
    void UtPathEmptyGrave(Vector2 pos)
    {
        if (UtpOn(UndertakerPaths.Yard)) UtSpawnGrave(pos, false, false, -1f, UndertakerPaths.YardEmptyGraveHp);
    }

    // 보스가 묘비를 부숨: 무너진 흙이 보스에게 공격력 250% (1초에 한 번 · 묘역 설계 공명 II)
    void UtPathBossBroke(bosss boss, Vector2 pos)
    {
        if (boss == null || !UtpOn(UndertakerPaths.Yard) || Paths.Res(UndertakerPaths.Yard) < 2 || Time.time < utBossBreakAt) return;
        utBossBreakAt = Time.time + UndertakerPaths.YardRes2Gap;
        Vector3 p = new Vector3(pos.x, pos.y, 0f);
        Paths.Deal(boss.gameObject, Paths.Atk * UndertakerPaths.YardRes2BossHit * Paths.Power(UndertakerPaths.Yard), (boss.transform.position - p).normalized, 0f, UndertakerPaths.CollapseSource);
        Fx.Spawn("fx_dirt", boss.transform.position, 2.2f, Color.white, 16f, 0f, 14);
    }

    // ================================================================= 도굴꾼
    // 되파기(카드)나 도굴(입문)로 묘비가 무너짐 (UtRedig 가 부름): 150% 무너짐 · 파편 · 이어 무너짐
    void UtPathRedug(Vector2 p)
    {
        if (!UtpOn(UndertakerPaths.Robber)) return;
        UtPathCollapse(p, true);
        if (!Paths.Mastered(UndertakerPaths.Robber)) return;
        // 극의: 2칸 안 다른 묘비로 이어짐 (최대 4개 · 가까운 것부터)
        int left = UndertakerPaths.RobberChainMax;
        List<Vector2> queue = new List<Vector2> { p };
        for (int qi = 0; qi < queue.Count && left > 0; qi++)
        {
            Vector2 q = queue[qi];
            List<UndertakerGrave> near = new List<UndertakerGrave>(4);
            foreach (UndertakerGrave g in utGraves)
                if (g != null && !g.isWall && Vector2.Distance(g.Pos, q) <= UndertakerPaths.RobberChainR) near.Add(g);
            near.Sort((a, b) => Vector2.Distance(a.Pos, q).CompareTo(Vector2.Distance(b.Pos, q)));
            foreach (UndertakerGrave g in near)
            {
                if (left <= 0) break;
                if (g == null) continue;
                Vector2 gp = g.Pos;
                UtBreakGrave(g);
                UtPathCollapse(gp, false);
                queue.Add(gp);
                left--;
            }
        }
    }

    // 묘비 하나가 무너지는 피해: 둘레 2칸 공격력 150% (보스 4칸 안이면 ×1.5) + 가까운 적 셋에게 파편
    void UtPathCollapse(Vector2 p, bool shards)
    {
        Vector3 p3 = new Vector3(p.x, p.y, 0f);
        float pw = Paths.Power(UndertakerPaths.Robber);
        float mul = UndertakerPaths.RobberBreak;
        if (Paths.Res(UndertakerPaths.Robber) >= 2)
        {
            foreach (bosss b in bosss.Live)
                if (b != null && !b.IsDead && Vector2.Distance(b.transform.position, p) <= UndertakerPaths.RobberRes2Range) { mul *= UndertakerPaths.RobberRes2Mul; break; }
        }
        Paths.Circle(p3, UndertakerPaths.RobberBreakR, Paths.Atk * mul * pw * formPower, 2f, UndertakerPaths.CollapseSource);
        Fx.Spawn("fx_grave_break", p3, 2.4f, Color.white, 14f);
        if (!shards || Paths.Res(UndertakerPaths.Robber) < 1) return;
        // 파편 셋: 가까운 적 셋에게 공격력 40%씩
        List<(float d, Collider2D c)> found = new List<(float, Collider2D)>(16);
        using (var list = PhysHits.Circle(p3, 8f, true))
        {
            foreach (Collider2D c in list)
            {
                if (c == null || !(c.CompareTag("enermy") || c.CompareTag("boss"))) continue;
                found.Add((((Vector2)c.transform.position - p).sqrMagnitude, c));
            }
        }
        found.Sort((a, b) => a.d.CompareTo(b.d));
        for (int i = 0; i < found.Count && i < UndertakerPaths.RobberShards; i++)
        {
            Collider2D c = found[i].c;
            if (c == null) continue;
            Paths.Deal(c.gameObject, Paths.Atk * UndertakerPaths.RobberShardHit * pw * formPower, (c.transform.position - p3).normalized, 0f, UndertakerPaths.CollapseSource);
            if (c != null) Fx.Spawn("fx_dirt", c.transform.position, 1.2f, Color.white, 16f, 0f, 14);
        }
    }

    // ================================================================= 장례식
    // 임시 묘비가 무너질 때 (UtBreakGrave 가 부름 · 공명 II): 둘레 2칸 공격력 80% (보스 포함)
    void UtPathTempBroke(Vector2 pos)
    {
        if (!UtpOn(UndertakerPaths.Funeral) || Paths.Res(UndertakerPaths.Funeral) < 2) return;
        Vector3 p3 = new Vector3(pos.x, pos.y, 0f);
        Paths.Circle(p3, 2f, Paths.Atk * UndertakerPaths.FuneralRes2Hit * Paths.Power(UndertakerPaths.Funeral) * formPower, 0f, UndertakerPaths.CollapseSource);
    }

    // 장례가 끝남 (UtFuneralRoutine 이 부름): 공명 I 가속 6초 · 저승길 4초
    void UtPathFuneralDone()
    {
        if (UtpOn(UndertakerPaths.Funeral) && Paths.Res(UndertakerPaths.Funeral) >= 1) utFuneralBoostUntil = Time.time + UndertakerPaths.FuneralRes1Sec;
        if (UndertakerPaths.CrossIs(UndertakerPaths.UnderworldCross)) utUnderworldUntil = Time.time + UndertakerPaths.UnderworldSec;
    }

    // 장례를 한 번 더 (극의): 1.5초 뒤 피해 50% · 묘비 없음
    IEnumerator UtFuneralEcho()
    {
        yield return new WaitForSeconds(UndertakerPaths.FuneralMasteryDelay);
        while (utFuneralBusy) yield return null;           // 첫 장례가 끝난 뒤에 (적 목록을 같이 쓰므로)
        if (player == null || player.IsDying) yield break;
        yield return StartCoroutine(UtFuneralRoutine(true));
    }

    // 일제 매장을 기다리는 동안 (합동 장례: 지나가던 다른 적도 같은 구멍에 빠짐)
    IEnumerator UtFuneralWait(float delay, bool mass, List<Vector3> spots, List<FxAnim> pits, float fallDmg)
    {
        if (!mass) { yield return new WaitForSeconds(delay); yield break; }
        for (float t = 0f; t < delay; t += 0.2f)
        {
            yield return new WaitForSeconds(0.2f);
            if (player == null) yield break;
            int n = spots.Count;
            for (int i = 0; i < n && utFall.Count < UndertakerTuning.FuneralCap; i++)
            {
                using (var list = PhysHits.Circle(spots[i], 1.1f, true))
                {
                    foreach (Collider2D c in list)
                    {
                        if (utFall.Count >= UndertakerTuning.FuneralCap) break;
                        if (c == null || !c.CompareTag("enermy") || !c.TryGetComponent(out EnermyController e)) continue;
                        if (e.IsDead || e.IsMidBoss || e.holder != null || e.swarm != null || !EnemyHold.TryTake(e, this)) continue;
                        utFall.Add(e);
                        spots.Add(spots[i]);         // utFall 과 같은 순서로
                        using (DamageSource.As(DamageSource.Ult)) Specials.Damage(e.gameObject, fallDmg, Vector3.zero, 0f);
                        if (e != null && !e.IsDead) EnemyHold.Raise(e, body, 0);
                    }
                }
            }
        }
    }

    // ================================================================= 땅굴 (LateUpdate 마다)
    void UtPathTick(float now)
    {
        if (!UtpOn(UndertakerPaths.Tunnel) || player == null || player.IsDying) { utTunnelInside = false; return; }
        Vector2 me = transform.position;
        UtHole inside = null;
        foreach (UtHole h in utHoles)
        {
            if (h == null || h.pitfall || h.burying || h.growing || h.go == null) continue;
            if (Vector2.Distance(me, h.pos) <= h.r) { inside = h; break; }
        }
        if (inside == null) { utTunnelInside = false; return; }
        if (utTunnelInside || now < utTunnelAt) return;
        if (!UtTunnelTarget(inside, out Vector2 to, out UtHole toHole)) return;
        utTunnelInside = true;           // 도착한 구멍에서 곧바로 다시 들어가지 않게 (구멍 밖으로 나가야 풀림)
        UtTunnelGo(inside, to, toHole);
    }

    // 나올 곳: 가장 최근에 판 다른 구멍 · 저승길(장례 뒤 4초)이면 화면 안 구멍 · 묘비 가운데 커서에 가장 가까운 곳
    bool UtTunnelTarget(UtHole from, out Vector2 pos, out UtHole hole)
    {
        pos = default; hole = null;
        bool under = Time.time < utUnderworldUntil && UndertakerPaths.CrossIs(UndertakerPaths.UnderworldCross);
        if (!under)
        {
            foreach (UtHole h in utHoles)
            {
                if (h == null || h == from || h.pitfall || h.burying || h.growing || h.go == null) continue;
                if (hole == null || h.bornAt > hole.bornAt) hole = h;
            }
            if (hole == null) return false;
            pos = hole.pos;
            return true;
        }
        bool hasView = UtView(0f, out Rect view);
        float best = float.MaxValue;
        foreach (UtHole h in utHoles)
        {
            if (h == null || h == from || h.pitfall || h.burying || h.growing || h.go == null) continue;
            if (hasView && !view.Contains(h.pos)) continue;
            float d = (h.pos - utCursor).sqrMagnitude;
            if (d < best) { best = d; pos = h.pos; hole = h; }
        }
        foreach (UndertakerGrave g in utGraves)
        {
            if (g == null || g.isWall || g.hostile) continue;
            if (hasView && !view.Contains(g.Pos)) continue;
            float d = (g.Pos - utCursor).sqrMagnitude;
            if (d < best) { best = d; pos = g.Pos; hole = null; }
        }
        return best < float.MaxValue;
    }

    void UtTunnelGo(UtHole from, Vector2 to, UtHole toHole)
    {
        Vector3 a = transform.position;
        Vector3 dest = Hostile.ClampArena(new Vector3(to.x, to.y, a.z));
        Paths.Invuln(UndertakerPaths.TunnelInvuln);
        Fx.Spawn("fx_tunnel", a, 3f, Color.white, 14f, 0f, 16);
        transform.position = dest;
        Fx.Spawn("fx_tunnel", dest, 3.4f, Color.white, 14f, 0f, 16);
        Fx.Spawn("fx_dirt", dest, 2.4f, new Color(1f, 1f, 1f, 0.8f), 16f, 0f, 14);
        Play("thump", 0.5f, 0.8f);
        Play("whoosh", 0.3f, 0.7f);
        bool under = Time.time < utUnderworldUntil && UndertakerPaths.CrossIs(UndertakerPaths.UnderworldCross);
        utTunnelAt = Time.time + (under ? 0f : Paths.Mastered(UndertakerPaths.Tunnel) ? UndertakerPaths.TunnelMasteryGap : UndertakerPaths.TunnelGap);
        float pw = Paths.Power(UndertakerPaths.Tunnel);
        Paths.Circle(dest, UndertakerPaths.TunnelPopR, Paths.Atk * UndertakerPaths.TunnelPop * pw * formPower, UndertakerPaths.TunnelPopKnock, UndertakerPaths.TunnelSource);
        // 공명 I: 튀어나온 구멍이 곧바로 가장 크게
        if (toHole != null && Paths.Res(UndertakerPaths.Tunnel) >= 1) toHole.r = toHole.rMax;
        // 공명 II: 들어간 구멍 → 나온 구멍 직선 위 적이 흙에 걸림 (1초 50% 느려짐 · 공격력 60% · 보스는 피해만)
        if (Paths.Res(UndertakerPaths.Tunnel) >= 2) UtTunnelLine(a, dest, pw);
        RunCounters.Both("tunnel");
    }

    void UtTunnelLine(Vector3 a, Vector3 b, float pw)
    {
        Vector3 mid = (a + b) * 0.5f;
        float half = Vector3.Distance(a, b) * 0.5f + UndertakerPaths.TunnelRes2Width + 1f;
        using var list = PhysHits.Circle(mid, half, true);
        foreach (Collider2D c in list)
        {
            if (c == null) continue;
            bool boss = c.CompareTag("boss");
            if (!boss && !c.CompareTag("enermy")) continue;
            if (Hostile.DistanceToSegment(c.transform.position, a, b) > UndertakerPaths.TunnelRes2Width) continue;
            if (!boss && c.TryGetComponent(out EnermyController e) && !e.IsDead && !e.IsMidBoss && e.holder == null)
                e.Slow(UndertakerPaths.TunnelRes2Slow, UndertakerPaths.TunnelRes2Sec);
            Paths.Deal(c.gameObject, Paths.Atk * UndertakerPaths.TunnelRes2Hit * pw * formPower, Vector3.zero, 0f, UndertakerPaths.TunnelSource);
            if (c != null) Fx.Spawn("fx_dirt", c.transform.position, 1.1f, Color.white, 16f, 0f, 14);
        }
    }
}
