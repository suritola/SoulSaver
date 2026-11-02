using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공사 반장 갈래 효과 — 키트 쪽 (2.0.0 · 기획서 hero-foreman.md 9절 · Paths/Paths.Foreman.cs 가 수치 · 문장)
//   불도저: 앞으로 안 느려짐 · 상한 +4 · 3칸 밀쳐 길 내기 · 완전 압착 · 상한 30            → FilterMove · FmAbsorb · FmUpdateWall (여기 FmPath* 를 부름)
//   산사태: 길고 좁은 파도 · 묻힘 · 흙더미 · 두 번째 파도 · 보스 · 되굴러감 · 작업 지시(31번)  → FmDump · FmDumpRoutine · FmLanding + 여기 (FmPathLanded · FmTryRedump)
//   콘크리트: 시멘트 · 굳은 적 +50% · 잔해 · 닿은 적 시멘트 · 터짐 · 보스 시멘트             → FmCrushTick + 여기 (FmCementTick · FmPathBurst · FmPathBossHit)
//   철거: 튕김 4 · 바리케이드 +2초 · 게이지 −15% · 빠각 ×2 · 굴러가는 쇠공 · 쌍 쇠공         → Foreman.Ult + 여기 (FmPathFenceGone · FmPathBossBounce)
//   안전 제일: 무게 무관 막기 · −15% · 압착 보너스 · 감싸기 · 보스 보너스 · 되튕김          → FmBlockShots + 여기 (FmPathBlocked · FmPathHurt)
//   교차: 콘크리트 벽(FmMakeWall) · 낙석 주의(FmPathLanded)
// 상태는 모두 「끝나는 시각」 값 · 개수 (켜고 끄는 코루틴 깃발 없음) · 만드는 것에는 개수 상한 (흙더미 8 · 벽 2 · 바리케이드는 기존 9)
public partial class CharacterKit
{
    // ---------------- 상태
    float fmWallSince = -1f, fmFullAt, fmWrapUntil, fmWrapAt, fmCemBuffUntil, fmMoundAt, fmBossCheckAt;
    bool fmBossAlive;
    int fmBossCem, fmSafeBoss;
    float fmRedumpUntil, fmRedumpNext = 1f;
    readonly List<EnermyController> fmRedump = new List<EnermyController>(32);
    readonly float[] fmSafeUntil = new float[10];
    readonly List<(Vector3 pos, float until)> fmMounds = new List<(Vector3, float)>(8);
    readonly List<Vector3> fmHardPos = new List<Vector3>(16);

    // 덤프 한 번의 정보 (착지 훅이 쓰는 것)
    struct FmDumpInfo { public Vector3 origin; public float pathDmg; public bool fence, redump; }

    static bool FmOn(int path) => ForemanPaths.On(path);

    public int FmSafetyStacks
    {
        get
        {
            int n = 0;
            float now = Time.time;
            for (int i = 0; i < fmSafeUntil.Length; i++) if (fmSafeUntil[i] > now) n++;
            return n;
        }
    }

    // 굳는 데 필요한 시멘트 (콘크리트 극의 2)
    int FmHardAt => FmOn(ForemanPaths.Concrete) && Paths.Mastered(ForemanPaths.Concrete) ? ForemanPaths.ConcreteMasteryNeed : ForemanPaths.CementMax;

    // ================================================================= 값 (원래 식에 곱하거나 더하는 곳)
    // 더미 상한: 불도저 +4 (극의 30)
    int FmPathPileMax(int baseMax)
    {
        if (!FmOn(ForemanPaths.Bulldoze)) return baseMax;
        return Paths.Mastered(ForemanPaths.Bulldoze) ? Mathf.Max(baseMax + ForemanPaths.BulldozeExtra, ForemanPaths.BulldozeMasteryMax) : baseMax + ForemanPaths.BulldozeExtra;
    }

    // 압착에 쓰는 무게 상한: 안전 제일 8
    int FmPathMassCap(int cap) => FmOn(ForemanPaths.Safety) ? Mathf.Min(cap, ForemanPaths.SafetyMassCap) : cap;

    float FmPathFenceExtra => FmOn(ForemanPaths.Demolish) ? ForemanPaths.DemolishBarricadeExtra : 0f;

    // 이동 필터: 불도저는 앞으로 느려지지 않고 옆 · 뒤 감소가 절반 (c = 이동 방향과 삽날 방향의 cos)
    float FmPathMoveFilter(float f, float c)
    {
        if (!FmOn(ForemanPaths.Bulldoze)) return f;
        return c >= 0.5f ? 1f : 1f - (1f - f) * 0.5f;
    }

    // 덤프 거리 배율: 불도저 −40% · 콘크리트 −50% · 산사태 ×1.6
    float FmPathDumpDist()
    {
        float m = 1f;
        if (FmOn(ForemanPaths.Bulldoze)) m *= ForemanPaths.BulldozeDumpCost;
        if (FmOn(ForemanPaths.Concrete)) m *= ForemanPaths.ConcreteDumpCost;
        if (FmOn(ForemanPaths.Slide)) m *= ForemanPaths.SlideLen;
        return m;
    }

    float FmPathSweepWidth => FmOn(ForemanPaths.Slide) ? ForemanPaths.SlideWidth : 1f;
    float FmPathDumpDmg => FmOn(ForemanPaths.Slide) ? 1f + (ForemanPaths.SlideDamage - 1f) * Paths.Power(ForemanPaths.Slide) : 1f;

    float FmCementBuff() => Time.time < fmCemBuffUntil ? ForemanPaths.ConcreteBossMul : 1f;

    // 압착 배율: 산사태 −35% · 철거 −20% · 안전 제일 막은 탄 쌓임 · 보스 시멘트 켜짐
    float FmPathCrushMul()
    {
        float m = FmCementBuff();
        if (FmOn(ForemanPaths.Slide)) m *= ForemanPaths.SlideCrushCost;
        if (FmOn(ForemanPaths.Demolish)) m *= ForemanPaths.DemolishCrushCost;
        if (FmOn(ForemanPaths.Safety) && Paths.Res(ForemanPaths.Safety) >= 1)
            m *= 1f + Mathf.Min(ForemanPaths.SafetyBlockMax, ForemanPaths.SafetyBlockBonus * FmSafetyStacks) * Paths.Power(ForemanPaths.Safety);
        return m;
    }

    // 굳은 적은 압착 +50% (콘크리트)
    float FmHardMul(EnermyController e) => FmOn(ForemanPaths.Concrete) && FmCementOf(e) >= FmHardAt ? ForemanPaths.CementCrush : 1f;

    // 보스 들이받기 · 빠각 배율: 보스 시멘트 켜짐 · 안전 제일 막은 보스 탄
    float FmPathBossMul() => FmCementBuff() * (1f + fmSafeBoss * ForemanPaths.SafetyBossBonus * Paths.Power(ForemanPaths.Safety));

    // 보스에게 들이받기 · 빠각이 들어간 뒤: 안전 제일 보너스를 씀 · 콘크리트 보스 시멘트 쌓임
    void FmPathBossHit()
    {
        fmSafeBoss = 0;
        if (!FmOn(ForemanPaths.Concrete) || Paths.Res(ForemanPaths.Concrete) < 2) return;
        if (++fmBossCem < ForemanPaths.CementMax) return;
        fmBossCem = 0;
        fmCemBuffUntil = Time.time + ForemanPaths.ConcreteBossSec;
        Fx.Spawn(FFx("fx_cement", "fx_sparkle"), fmBossTarget != null ? fmBossTarget.transform.position : fmCenter, 2.2f, Color.white, 14f);
    }

    // ================================================================= 불도저
    // 공명 I: 무게 10 이상이면 앞의 적을 흡수하지 않고 3칸 밀쳐 길을 냄 (true 면 이 적은 처리됨)
    bool FmPathShove(EnermyController e, Vector2 rel)
    {
        if (!FmOn(ForemanPaths.Bulldoze) || Paths.Res(ForemanPaths.Bulldoze) < 1 || fmMass < ForemanPaths.BulldozeShoveAt) return false;
        Vector2 d = rel.sqrMagnitude > 0.0001f ? rel.normalized : fmBlade;
        Paths.Deal(e.gameObject, Paths.Atk * ForemanPaths.BulldozeShove * Paths.Power(ForemanPaths.Bulldoze), d, 0f);
        if (e == null || e.IsDead) return true;
        EnemyHold.Place(e, EnemyHold.FreeSpot(e.transform.position + (Vector3)(d * ForemanPaths.BulldozeShoveDist)));
        return true;
    }

    // 공명 II: 벽 · 바리케이드 · 보스에 대고 1초(극의 0.5초) 누르면 완전 압착 (FmUpdateWall 이 0.1초마다 부름)
    void FmPathWall()
    {
        if (!FmOn(ForemanPaths.Bulldoze) || Paths.Res(ForemanPaths.Bulldoze) < 2 || !fmHolding || !fmWall) { fmWallSince = -1f; return; }
        float now = Time.time;
        if (fmWallSince < 0f) { fmWallSince = now; return; }
        float hold = Paths.Mastered(ForemanPaths.Bulldoze) ? ForemanPaths.FullCrushMasteryHold : ForemanPaths.FullCrushHold;
        if (now - fmWallSince < hold || now < fmFullAt) return;
        fmWallSince = now;
        fmFullAt = now + ForemanPaths.FullCrushGap;
        FmFullCrush();
    }

    void FmFullCrush()
    {
        FmClean();
        float pw = Paths.Power(ForemanPaths.Bulldoze);
        float each = Damage * ForemanPaths.FullCrushRate * formPower * pw;
        fmTmp.Clear();
        foreach (FmItem it in fmItems) if (it.e != null) fmTmp.Add(it.e);
        float total = 0f;
        int hit = 0;
        using (DamagePopup.Quiet())
        {
            foreach (EnermyController e in fmTmp)
            {
                if (e == null || e.IsDead) continue;
                Vector3 p = e.transform.position;
                bool hard = FmCementOf(e) >= FmHardAt;
                Paths.Deal(e.gameObject, each, fmBlade, 0f, ForemanPaths.FullCrushSource);
                total += each;
                hit++;
                if (e.IsDead) { RunCounters.Both("b.crushkill"); SignatureSkills.FmPileKilled(p, hard); }
            }
            if (fmBossTarget != null)
            {
                float bd = Damage * ForemanPaths.FullCrushBoss * Mathf.Min(fmMass, ForemanPaths.FullCrushMassCap) * FmBossMul() * FmPathBossMul() * formPower * pw;
                Paths.Deal(fmBossTarget, bd, fmBlade, 0.5f, ForemanPaths.FullCrushSource);
                total += bd;
                hit++;
                FmPathBossHit();
            }
        }
        if (hit == 0) return;
        if (GameSettings.DamageNumbers != 2 && SpecialAbilities.SharedFx != null)
            SpecialAbilities.SharedFx.FloatText(fmCenter + Vector3.up * (fmR + 1.2f), "-" + Mathf.RoundToInt(total) + (hit > 1 ? " ×" + hit : ""), FmOrange, 5.5f, 0.1f);
        RunCounters.Both("b.fullcrush");
        Fx.Spawn(FFx("fx_crunch", "fx_shock"), fmCenter, 3.6f + fmR * 0.4f, Color.white, 20f);
        Play("crack", 0.7f, 0.65f);
        Play("thump", 0.7f, 0.7f);
        Hostile.Shake(0.14f);
        if (fmMass >= 6) Impact.Hit(ImpactTier.Attack, fmCenter, fmBlade);
    }

    // ================================================================= 콘크리트
    // 짓누를 때마다 더미 안 적에게 시멘트 (고유 스킬 시멘트 · 콘크리트 갈래 입문) — 공명 I: 굳은 적에 닿은 적은 하나 더
    void FmCementTick()
    {
        bool skill = SignatureSkills.FmLv("b.cement") > 0;
        bool path = FmOn(ForemanPaths.Concrete);
        if (!skill && !path) return;
        int hardAt = FmHardAt;
        bool touch = path && Paths.Res(ForemanPaths.Concrete) >= 1;
        fmHardPos.Clear();
        if (touch)
            foreach (FmItem it in fmItems)
                if (it.e != null && !it.e.IsDead && it.cement >= hardAt) fmHardPos.Add(it.e.transform.position);
        foreach (FmItem it in fmItems)
        {
            if (it.e == null || it.e.IsDead || it.cement >= ForemanPaths.CementMax) continue;
            int add = 1;
            if (fmHardPos.Count > 0 && it.cement < hardAt)
            {
                Vector3 p = it.e.transform.position;
                for (int i = 0; i < fmHardPos.Count; i++)
                    if (((Vector2)(fmHardPos[i] - p)).sqrMagnitude <= 1f) { add = 2; break; }
            }
            int before = it.cement;
            it.cement = Mathf.Min(ForemanPaths.CementMax, before + add);
            if (before < hardAt && it.cement >= hardAt)
                Fx.Spawn(FFx("fx_cement", "fx_sparkle"), it.e.transform.position, 1.4f, Color.white, 14f);
        }
    }

    // 공명 II: 더미 안에서 쓰러진 적이 터짐 — 둘레 1.5칸에 공격력 60% + 시멘트 1 (한 번 연쇄: 터짐이 낸 처치는 다시 터지지 않음)
    void FmPathBurst(Vector3 p)
    {
        if (!FmOn(ForemanPaths.Concrete) || Paths.Res(ForemanPaths.Concrete) < 2) return;
        float r = ForemanPaths.ConcreteBurstR;
        foreach (FmItem it in fmItems)
            if (it.e != null && !it.e.IsDead && it.cement < ForemanPaths.CementMax && ((Vector2)(it.e.transform.position - p)).sqrMagnitude <= r * r) it.cement++;
        Paths.Circle(p, r, Paths.Atk * ForemanPaths.ConcreteBurst * Paths.Power(ForemanPaths.Concrete), 1.5f, ForemanPaths.CementSource);
        Fx.Spawn(FFx("fx_cement", "fx_dust"), p, r * 2f, new Color(0.8f, 0.82f, 0.85f, 0.9f), 16f);
    }

    // ================================================================= 산사태
    // 흙더미 (공명 I): 덤프가 닿은 자리에 2초 — 밟은 적 40% 느려짐
    void FmAddMound(Vector3 at)
    {
        if (fmMounds.Count >= 8) fmMounds.RemoveAt(0);
        fmMounds.Add((at, Time.time + ForemanPaths.SlideMoundLife));
        Fx.Spawn(FFx("fx_roller", "fx_dust"), at, 3.2f, new Color(0.85f, 0.7f, 0.5f, 0.8f), 12f);
    }

    // 파도가 지나가는 동안 맞은 적마다 (FmDumpRoutine 이 부름): 묻힘 0.5초 · 공명 II 보스는 무게 비례 추가
    void FmPathSwept(GameObject g, int M)
    {
        if (!FmOn(ForemanPaths.Slide)) return;
        if (g.TryGetComponent(out EnermyController e) && !Paths.IsBoss(g) && !e.IsDead) e.Slow(0f, ForemanPaths.SlideStun);
        if (Paths.Res(ForemanPaths.Slide) >= 2 && Paths.IsBoss(g))
            Paths.Deal(g, Paths.Atk * ForemanPaths.SlideBoss * Mathf.Min(M, ForemanPaths.FullCrushMassCap) * Paths.Power(ForemanPaths.Slide), fmBlade, 0.5f, ForemanPaths.SlideSource);
    }

    // 착지 뒤 갈래 효과 (FmLanding 이 부름)
    void FmPathLanded(FmDumpInfo info, Vector3 land, Vector2 dir, int M, bool crashed, Vector3 crashAt, FmItem[] items)
    {
        float sp = Paths.Power(ForemanPaths.Slide);
        if (FmOn(ForemanPaths.Slide))
        {
            if (Paths.Res(ForemanPaths.Slide) >= 1) FmAddMound(land);
            if (Paths.Res(ForemanPaths.Slide) >= 2 && M >= ForemanPaths.SlideSecondAt)
            {
                // 1칸 옆으로 두 번째 파도 (60%)
                Vector3 side = new Vector3(-dir.y, dir.x);
                FmLineDamage(info.origin + side, land + side, 0.9f, info.pathDmg * ForemanPaths.SlideSecond * sp, dir);
                Fx.Spawn(FFx("fx_landslide", "fx_dust"), (info.origin + land) * 0.5f + side, Mathf.Max(3f, Vector3.Distance(info.origin, land)), new Color(1f, 1f, 1f, 0.6f), 14f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 6);
            }
            if (Paths.Mastered(ForemanPaths.Slide)) StartCoroutine(FmEchoLater(info.origin, land, dir, info.pathDmg * ForemanPaths.SlideEcho * sp));
            // 작업 지시(31번 카드): 이 덤프에 쏟아진 적을 기억 (두 번째 덤프로는 다시 기억하지 않음)
            if (!info.redump && Paths.Level(Paths.ExtraId) > 0)
            {
                fmRedump.Clear();
                foreach (FmItem it in items) if (it.e != null && !it.e.IsDead) fmRedump.Add(it.e);
                fmRedumpUntil = Time.time + ForemanPaths.RedumpWindow;
            }
        }
        if (crashed && info.fence)
        {
            // 철거 공명 I: 덤프한 더미가 바리케이드에 부딪히면 빠각 ×2
            if (FmOn(ForemanPaths.Demolish) && Paths.Res(ForemanPaths.Demolish) >= 1)
            {
                DamageCircle(crashAt, 1.8f, Damage * 0.25f * Mathf.Min(M, 12) * ForemanPaths.DemolishCrashMul * formPower * Paths.Power(ForemanPaths.Demolish), 2f);
                Fx.Spawn(FFx("fx_crunch", "fx_shock"), crashAt, 3.6f, new Color(1f, 0.85f, 0.5f), 20f);
            }
            // 낙석 주의 (교차): 바리케이드가 무너지며 그 뒤로 산사태가 한 번 더
            if (Paths.CrossOn(ForemanPaths.RockfallCross))
            {
                FmFence f = FmFenceAt(crashAt, 0.6f);
                if (f != null)
                {
                    Vector3 fp = f.pos;
                    FmCollapse(f, true);
                    Vector3 to = Hostile.ClampArena(fp + (Vector3)(dir * 5f));
                    FmLineDamage(fp, to, 1.2f, info.pathDmg * ForemanPaths.RockfallRate * sp, dir);
                    Fx.Spawn(FFx("fx_landslide", "fx_dust"), (fp + to) * 0.5f, 5f, new Color(1f, 1f, 1f, 0.8f), 14f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 6);
                    Play("thump", 0.6f, 0.6f);
                }
            }
        }
    }

    IEnumerator FmEchoLater(Vector3 a, Vector3 b, Vector2 dir, float dmg)
    {
        yield return new WaitForSeconds(ForemanPaths.SlideEchoDelay);
        if (player == null || player.IsDying) yield break;
        Fx.Spawn(FFx("fx_landslide", "fx_dust"), (a + b) * 0.5f, Mathf.Max(3f, Vector3.Distance(a, b)), new Color(1f, 1f, 1f, 0.55f), 14f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 6);
        FmLineDamage(a, b, 1.0f, dmg, dir);
    }

    // 작업 지시: 덤프 직후 0.8초 안에 다시 누르면 흩어진 더미를 삽날 앞으로 다시 모아 두 번째 덤프 (UpdatePlow 가 누른 순간에 부름)
    bool FmTryRedump()
    {
        if (fmRedump.Count == 0) return false;
        int lv = Mathf.Clamp(Paths.Level(Paths.ExtraId), 0, 4);
        if (lv <= 0 || !FmOn(ForemanPaths.Slide) || Time.time > fmRedumpUntil) { fmRedump.Clear(); return false; }
        FmClean();
        int max = FmPileMax;
        foreach (EnermyController e in fmRedump)
        {
            if (e == null || e.IsDead || e.holder != null) continue;
            int m = FmMassOf(e);
            if (fmMass + m > max || !EnemyHold.TryTake(e, this)) continue;
            fmItems.Add(new FmItem { e = e, mass = m });
            EnemyHold.Raise(e, body, 2);
            fmMass += m;
        }
        fmRedump.Clear();
        if (fmItems.Count == 0) { fmMass = 0; return false; }
        // 삽날 앞에 바로 모음
        float R = 0.45f * Mathf.Sqrt(Mathf.Max(1, fmMass));
        fmR = R;
        Vector3 c = transform.position + (Vector3)(fmBlade * (FmBladeAt + 0.5f + R));
        fmCenter = c;
        Vector2 rotY = new Vector2(-fmBlade.y, fmBlade.x);
        int n = fmItems.Count;
        for (int i = 0; i < n; i++)
        {
            float rr = R * Mathf.Sqrt((i + 0.5f) / n), a = 2.4f * i;
            Vector3 p = c + (Vector3)(fmBlade * (rr * Mathf.Cos(a)) + rotY * (rr * Mathf.Sin(a) * 0.88f));
            if (fmItems[i].e != null) EnemyHold.Place(fmItems[i].e, p);
        }
        fmRedumpNext = 0.35f + 0.15f * lv;       // 50% · 65% · 80% · 95%
        SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 2.2f, Loc.T("한 번 더!"), FmYellow, 5.5f, 0f, 1f);
        Play("whoosh", 0.4f, 0.8f);
        FmEndHold(true);
        return true;
    }

    // ================================================================= 철거
    // 바리케이드가 사라질 때 (공명 II): 앞쪽으로 작은 쇠공이 굴러감 (공격력 120% · 4칸)
    void FmPathFenceGone(FmFence f)
    {
        if (!FmOn(ForemanPaths.Demolish) || Paths.Res(ForemanPaths.Demolish) < 2 || player == null || player.IsDying) return;
        Vector2 n = f.normal;
        if (Vector2.Dot(n, (Vector2)(f.pos - player.transform.position)) < 0f) n = -n;
        Vector3 from = f.pos;
        Vector3 to = Hostile.ClampArena(from + (Vector3)(n * ForemanPaths.DemolishRollLen));
        FmLineDamage(from, to, 0.6f, Paths.Atk * ForemanPaths.DemolishRollRate * Paths.Power(ForemanPaths.Demolish), n);
        Fx.Stretch(FFx("fx_wreckball", "fx_sparkle"), from, to, 0.9f, Color.white, 0.4f, 14, 22f);
    }

    // 쇠공이 튕긴 자리에서 (공명 II): 근처 보스에게 한 번 더 튕김 (필살기마다 최대 2번)
    void FmPathBossBounce(Vector3 at, float baseDmg, ref int used)
    {
        if (!FmOn(ForemanPaths.Demolish) || Paths.Res(ForemanPaths.Demolish) < 2 || used >= ForemanPaths.DemolishBossBounce) return;
        Specials.Overlap(at, 3.5f, fmHits);
        for (int i = 0; i < fmHits.Count; i++)
        {
            Collider2D c = fmHits[i];
            if (c == null || !Paths.IsBoss(c.gameObject)) continue;
            used++;
            Vector3 bp = c.transform.position;
            DamageCircle(bp, 2.2f, baseDmg * 2.5f * Paths.Power(ForemanPaths.Demolish), 3f);
            Fx.Spawn("fx_explosion", bp, 4.4f, new Color(1f, 0.85f, 0.4f), 18f);
            Play("thump", 0.6f, 0.7f);
            Hostile.Shake(0.15f);
            return;
        }
    }

    // ================================================================= 안전 제일
    // 투사체를 막을 때마다: 공명 I 압착 보너스 쌓임 · 공명 II 보스 탄이면 다음 들이받기 · 빠각 보너스 · 극의 되튕김
    void FmPathBlocked(Vector3 at, Vector2 dir, bool allowReflect)
    {
        if (!FmOn(ForemanPaths.Safety)) return;
        float now = Time.time;
        if (Paths.Res(ForemanPaths.Safety) >= 1)
        {
            int idx = 0;
            for (int i = 1; i < fmSafeUntil.Length; i++) if (fmSafeUntil[i] < fmSafeUntil[idx]) idx = i;       // 가장 먼저 끝나는 자리에 덮어씀
            fmSafeUntil[idx] = now + ForemanPaths.SafetyBlockSec;
        }
        if (Paths.Res(ForemanPaths.Safety) >= 2)
        {
            if (now >= fmBossCheckAt) { fmBossCheckAt = now + 1f; fmBossAlive = GameObject.FindWithTag("boss") != null; }
            if (fmBossAlive && fmSafeBoss < Mathf.RoundToInt(ForemanPaths.SafetyBossMax / ForemanPaths.SafetyBossBonus)) fmSafeBoss++;
        }
        if (allowReflect && Paths.Mastered(ForemanPaths.Safety))
        {
            List<Transform> near = Paths.Nearest(at, 16f, 1);
            if (near.Count > 0 && near[0] != null)
            {
                Fx.Stretch(FFx("fx_rubble", "fx_sparkle"), at, near[0].position, 0.5f, new Color(1f, 0.9f, 0.5f), 0.2f, 14, 20f);
                Paths.Deal(near[0].gameObject, Paths.Atk * ForemanPaths.SafetyReflect * Paths.Power(ForemanPaths.Safety), (near[0].position - at).normalized, 1f, ForemanPaths.ReflectSource);
            }
        }
    }

    // 맞았을 때 (공명 II · ForemanPaths.Hurt): 더미가 0.6초 동안 반장 둘레를 감쌈 (1초에 한 번)
    public void FmPathHurt()
    {
        if (Time.time < fmWrapAt || fmItems.Count == 0) return;
        fmWrapAt = Time.time + ForemanPaths.SafetyWrapGap;
        fmWrapUntil = Time.time + ForemanPaths.SafetyWrapSec;
        Fx.Spawn(FFx("fx_dust", "fx_sparkle"), transform.position, 3f, FmDust, 18f);
        Play("clank", 0.4f, 0.7f);
    }

    // ================================================================= 교차: 콘크리트 벽
    // 상한까지 찬 더미를 덤프하면 쏟는 대신 그 자리에 3초 동안 벽으로 굳음 (동시 2개) — 적을 막고 벽 압착 · 빠각의 상대가 됨
    void FmMakeWall(FmItem[] items, Vector3 origin, Vector2 dir)
    {
        Vector2 side = new Vector2(-dir.y, dir.x);
        float width = Mathf.Clamp(2f * fmR + 1.5f, 3f, 8f);
        int walls = 0;
        foreach (FmFence f in fmFences) if (f.wall) walls++;
        for (int i = 0; i < fmFences.Count && walls >= ForemanPaths.WallCap; i++)
            if (fmFences[i].wall) { FmCollapse(fmFences[i], false); walls--; i--; }
        Vector3 at = Hostile.ClampArena(origin);
        FmFence w = FmAddFence(at, side, width, ForemanPaths.WallLife, 0.7f);
        w.wall = true;
        if (w.sr != null) w.sr.color = new Color(0.78f, 0.8f, 0.84f);
        int k = 0;
        foreach (FmItem it in items)
        {
            if (it.rubble != null) { Destroy(it.rubble.gameObject); continue; }
            if (it.e == null) continue;
            Vector3 spot = at + (Vector3)(dir * (1.6f + (k % 3) * 0.6f) + side * (((k % 5) - 2) * 0.6f));
            k++;
            EnemyHold.Place(it.e, EnemyHold.FreeSpot(spot));
            EnemyHold.Release(it.e, this);
            if (!it.e.IsDead) it.e.Slow(0f, 0.6f);
        }
        Fx.Spawn(FFx("fx_cement", "fx_dust"), at, width, new Color(0.8f, 0.82f, 0.85f, 0.9f), 14f);
        Play("thump", 0.7f, 0.6f);
        Hostile.Shake(0.12f);
    }

    // ================================================================= 매 프레임 (FmLate 가 부름)
    void FmPathTick()
    {
        if (fmMounds.Count == 0 || Time.time < fmMoundAt) return;
        fmMoundAt = Time.time + 0.25f;
        float slow = 1f - ForemanPaths.SlideMoundSlow;
        for (int i = fmMounds.Count - 1; i >= 0; i--)
        {
            if (Time.time >= fmMounds[i].until) { fmMounds.RemoveAt(i); continue; }
            Specials.Overlap(fmMounds[i].pos, 1.6f, fmHits);
            for (int j = 0; j < fmHits.Count; j++)
            {
                Collider2D c = fmHits[j];
                if (c != null && c.CompareTag("enermy") && c.TryGetComponent(out EnermyController e) && e.holder == null && !e.IsDead) e.Slow(slow, 0.4f);
            }
        }
    }
}
