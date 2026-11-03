using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 사진사 갈래 효과 — 키트 쪽 (2.0.0 · 기획서 hero-photographer.md 9절 · Paths/Paths.Photographer.cs 가 수치 · 문장)
//   연사: 필름 12 · 틀 ×0.8 · 종이 1.5초(셔터 간격 ×0.6 은 PhotographerPaths.RateMul) · 필름 끊김 · 장노출 겹침 2배 · 극의 감기 0.6초 + 감는 동안 셔터
//   망원: 사거리 18 · 틀 ×0.65 · 초점 원 1.2 · 초점 ×2.2 · 6칸 안 반감 · 초점 종이 2배 + 표식 3 · 클로즈업 · 극의 초점 둘
//   암실: 인화 두 장 · 미끼 4.5초 · 찢김 ×1.6 · 필름 4 · 감기 +0.3초 · 암실 커튼 · 보스 +15%(PhotographerPaths.Outgoing) · 극의 사본 10 · 인화 세 장
//   종이 공작: 종이 4초 · 종잇조각 2장 기본 · 조각 종이 1.5초 · 종이 더미 · 극의 조각 ×2 · 한 번 더 튐
//   플래시: 스트로보 테두리 멈춤 · 극의 1초 / 3칸  (투사체 지우기 · 무적 · 게이지는 SignatureSkills.OnPhShutter)
//   교차: 종이 인형극(사본이 다가감) · 스트로보 연사(세 번째 셔터 테두리 ×2)
// 상태는 모두 「끝나는 시각」 값 · 만드는 것에는 개수 상한 (커튼 6 · 사본 18/30 · 장노출 5)
public partial class CharacterKit
{
    sealed class PhExpose { public Rect rect; }

    readonly List<PhExpose> phExposes = new List<PhExpose>(6);
    readonly Dictionary<EnermyController, Vector2> phSnap = new Dictionary<EnermyController, Vector2>(32);       // x = 연속 횟수 · y = 창 끝 시각
    readonly Dictionary<EnermyController, float> phStopped = new Dictionary<EnermyController, float>(48);        // 다시 멈출 수 있는 시각
    readonly List<PhPaper> phPileTmp = new List<PhPaper>(16);
    EnermyController phLocked;
    int phShutterN, phCurtains;
    float phWindAt, phPileAt, phPathPruneAt;

    // 추적 초점이 잠근 적 (자동 초점 카드가 체력이 가장 많은 적을 고른 경우만)
    public EnermyController PhLocked => phLocked;

    // ================================================================= 갈래 자리 값 (CharacterKit.Photographer 가 읽음)
    int PhFilmBase => PhotographerTuning.FilmDefault
        + (PhotographerPaths.On(PhotographerPaths.Burst) ? PhotographerPaths.BurstMag - PhotographerTuning.FilmDefault : 0)
        - (PhotographerPaths.On(PhotographerPaths.Dark) ? PhotographerPaths.DarkFilmLess : 0);

    float PhRange => PhotographerPaths.On(PhotographerPaths.Tele) ? PhotographerPaths.TeleRange : def.range;

    float PhPathFrame => (PhotographerPaths.On(PhotographerPaths.Burst) ? PhotographerPaths.BurstFrame : 1f)
                       * (PhotographerPaths.On(PhotographerPaths.Tele) ? PhotographerPaths.TeleFrame : 1f);

    // 종이가 되는 시간: 종이 공작 4초 · 연사는 그 60% (둘 다면 2.4초)
    float PhPaperSec
    {
        get
        {
            float s = PhotographerPaths.On(PhotographerPaths.Craft) ? PhotographerPaths.CraftPaper : PhotographerTuning.PaperSec;
            return PhotographerPaths.On(PhotographerPaths.Burst) ? s * (PhotographerPaths.BurstPaper / PhotographerTuning.PaperSec) : s;
        }
    }

    // 필름 감기 시간 바탕: 암실 +0.3초 · 연사 극의 0.6초
    float PhReloadBaseNow => PhotographerPaths.Master(PhotographerPaths.Burst) ? PhotographerPaths.BurstMasteryWind
        : phReloadBase + (PhotographerPaths.On(PhotographerPaths.Dark) ? PhotographerPaths.DarkWind : 0f);

    // 종잇조각: 종이 공작 2장 기본(현상액이 더 늘리면 그쪽) · 극의 2배
    int PhShardCount
    {
        get
        {
            int n = PhotographerPaths.On(PhotographerPaths.Craft) ? PhotographerPaths.CraftShards : 0;
            if (card[2] > 0) n = Mathf.Max(n, 1 + card[2]);
            return PhotographerPaths.Master(PhotographerPaths.Craft) ? n * 2 : n;
        }
    }
    float PhShardPaper => PhotographerPaths.ResAt(PhotographerPaths.Craft, 1) ? PhotographerPaths.CraftRes1Paper : 0.8f;
    int PhShardGens => PhotographerPaths.Master(PhotographerPaths.Craft) ? 3 : 2;

    // 인화: 암실 두 장 · 극의 세 장 / 한 장당 사본 상한 9(극의 13) / 동시 사본 18(극의 30)
    int PhPrintCount => !PhotographerPaths.On(PhotographerPaths.Dark) ? 1
        : PhotographerPaths.Master(PhotographerPaths.Dark) ? PhotographerPaths.DarkMasteryPrints : PhotographerPaths.DarkPrints;
    int PhPerPrintCap => 9 + (PhotographerPaths.Master(PhotographerPaths.Dark) ? PhotographerPaths.DarkMasteryPerPrint : 0);
    int PhCopyCap => PhotographerPaths.Master(PhotographerPaths.Dark) ? PhotographerPaths.DarkMasteryTotal : PhotographerTuning.CopyCap + 6;

    // 사본이 이 자리 r칸 안에 있나 (암실 공명 II)
    public bool PhCopyNear(Vector2 pos, float r)
    {
        float r2 = r * r;
        foreach (PhCopy c in phCopies)
            if (c.tf != null && (c.pos - pos).sqrMagnitude <= r2) return true;
        return false;
    }

    // ================================================================= 초점 (망원 극의는 둘)
    void PhPickFocus(Vector2 c, float fr, EnermyController exE, GameObject exB, out EnermyController fe, out GameObject fb)
    {
        fe = null; fb = null;
        float bestD = fr;
        foreach (EnermyController e in phNormals)
        {
            if (e == null || e == exE) continue;
            float d = Vector2.Distance(c, e.transform.position);
            if (d < bestD) { bestD = d; fe = e; fb = null; }
        }
        foreach (GameObject b in phBosses)
        {
            if (b == null || b == exB) continue;
            float d = Vector2.Distance(c, b.transform.position);
            if (d < bestD) { bestD = d; fe = null; fb = b; }
        }
    }

    // 보스에게 찍힘: 초점 표식 +2(가운데) · +1 · 망원 공명 I 은 초점 +3 / 표식 5 인 보스를 초점으로 찍으면 클로즈업(공명 II)
    void PhBossFocus(GameObject b, bool focus)
    {
        if (b == null) return;
        if (focus && PhotographerPaths.ResAt(PhotographerPaths.Tele, 2) && PhMarkCount >= PhotographerTuning.MarkMax)
        {
            Vector3 bp = b.transform.position;
            DamagePopup.NextCrit = true;
            Paths.Deal(b, Paths.Atk * PhotographerPaths.TeleCloseUp * Paths.Power(PhotographerPaths.Tele), (bp - transform.position).normalized, 0f, PhotographerPaths.CloseUpSource);
            phMarks = 0; phMarkUntil = 0f;          // 표식을 모두 씀
            Fx.Spawn("fx_flashframe", bp, 3.5f, PhFlashSafe(new Color(1f, 1f, 1f, 0.8f)), 20f, 0f, 22);
            Impact.Hit(ImpactTier.Skill, bp, Vector2.zero);
            return;
        }
        PhAddMarks(focus ? (PhotographerPaths.ResAt(PhotographerPaths.Tele, 1) ? PhotographerPaths.TeleRes1Marks : 2) : 1);
    }

    // ================================================================= 연사
    // 필름 끊김 (공명 I): 1초 안에 같은 적을 세 번 찍으면 공격력 100% 한 번 더 + 종이 새로
    void PhPathSnap(EnermyController e, Vector3 dir)
    {
        if (e == null || !PhotographerPaths.ResAt(PhotographerPaths.Burst, 1)) return;
        float now = Time.time;
        phSnap.TryGetValue(e, out Vector2 v);
        if (v.y > now) v.x += 1f; else { v.x = 1f; v.y = now + PhotographerPaths.BurstSnapWin; }
        if (v.x >= PhotographerPaths.BurstSnapN)
        {
            v = Vector2.zero;
            DamagePopup.NextCrit = true;
            Paths.Deal(e.gameObject, Paths.Atk * PhotographerPaths.BurstSnapMul * Paths.Power(PhotographerPaths.Burst), dir.normalized, 0f, PhotographerPaths.SnapSource);
            Fx.Spawn("fx_paperize", e.transform.position, 1.8f, Color.white, 18f, 0f, 22);
            if (e != null && !e.IsDead) PhPaperTake(e, PhPaperSec, false, true);       // 2.0.1~ 필름 끊김(공명 I) 「종이 새로」 — 명시 갱신
        }
        phSnap[e] = v;
    }

    // 장노출 틀이 겹친 자리는 피해 2배 (공명 II · 보스도)
    float PhExposeOverlap(PhExpose me, Vector3 pos)
    {
        if (!PhotographerPaths.ResAt(PhotographerPaths.Burst, 2)) return 1f;
        foreach (PhExpose o in phExposes)
            if (o != me && o.rect.Contains(pos)) return PhotographerPaths.BurstOverlapMul;
        return 1f;
    }

    // ================================================================= 플래시 (스트로보)
    // 셔터 순간 틀 바깥 테두리까지 번쩍여 일반 적이 멈춤 (같은 적은 3초에 한 번) · 스트로보 연사 교차: 세 번째 셔터마다 테두리 2배
    void PhPathStrobe(Vector2 c, Vector2 size)
    {
        if (!PhotographerPaths.On(PhotographerPaths.Flash)) return;
        phShutterN++;
        bool master = PhotographerPaths.Master(PhotographerPaths.Flash);
        float rim = master ? PhotographerPaths.FlashMasteryRim : PhotographerPaths.FlashRim;
        if (PhotographerPaths.CrossIs(PhotographerPaths.StrobeCross) && phShutterN % PhotographerPaths.StrobeEvery == 0) rim *= PhotographerPaths.StrobeRim;
        float stop = master ? PhotographerPaths.FlashMasteryStop : PhotographerPaths.FlashStop;
        Vector2 big = size + Vector2.one * (2f * rim);
        Fx.Spawn("fx_flashframe", new Vector3(c.x, c.y, 0f), big.y, PhFlashSafe(new Color(1f, 1f, 1f, 0.4f)), 12f, 0f, 20);
        float now = Time.time;
        using (var ovl = PhysHits.Box(c, big, 0f)) foreach (Collider2D col in ovl)
        {
            if (col == null || !col.CompareTag("enermy")) continue;
            EnermyController e = EnermyController.FromCollider(col);
            if (e == null || e.IsDead || e.IsMidBoss || e.swarm != null || e.holder != null) continue;
            if (phStopped.TryGetValue(e, out float next) && now < next) continue;
            phStopped[e] = now + PhotographerPaths.FlashStopGap;
            e.Slow(0f, stop);
        }
    }

    // ================================================================= 암실 커튼 (공명 I) · 종이 인형극 (교차)
    void PhPathCurtain(Vector3 at)
    {
        if (!PhotographerPaths.ResAt(PhotographerPaths.Dark, 1) || phCurtains >= 6) return;
        StartCoroutine(PhCurtainRun(at));
    }

    IEnumerator PhCurtainRun(Vector3 at)
    {
        phCurtains++;
        Fx.Spawn("fx_flashframe", at, PhotographerPaths.DarkCurtainR * 2f, PhFlashSafe(new Color(0.55f, 0.35f, 0.8f, 0.5f)), 8f, 0f, 19);
        for (float t = 0f; t < PhotographerPaths.DarkCurtainSec; t += 0.1f)
        {
            for (int i = HostileProjectile.Live.Count - 1; i >= 0; i--)
            {
                HostileProjectile hp = HostileProjectile.Live[i];
                if (hp == null || Vector2.Distance(hp.transform.position, at) > PhotographerPaths.DarkCurtainR) continue;
                Fx.Spawn("fx_papertear", hp.transform.position, 1.0f, Color.white, 18f, 0f, 22);
                Destroy(hp.gameObject);
            }
            yield return new WaitForSeconds(0.1f);
        }
        phCurtains--;
    }

    // 종이 인형극: 사본이 펄럭이며 가까운 적에게 초당 1칸씩 다가감 (0.1초 간격 호출)
    void PhPuppetStep(PhCopy c)
    {
        if (c.tf == null) return;
        Vector2 best = Vector2.zero;
        float bd = 64f;
        bool found = false;
        using (var ovl = PhysHits.Circle(c.pos, 8f)) foreach (Collider2D col in ovl)
        {
            if (col == null || !col.CompareTag("enermy")) continue;
            EnermyController e = EnermyController.FromCollider(col);
            if (e == null || e.IsDead || e.swarm != null) continue;
            float d = ((Vector2)e.transform.position - c.pos).sqrMagnitude;
            if (d < bd) { bd = d; best = e.transform.position; found = true; }
        }
        float flap = 0.75f * (0.8f + 0.2f * Mathf.Sin((Time.time - c.born) * 12f));
        c.tf.localScale = new Vector3(flap, 0.6f, 1f);
        if (!found) return;
        Vector3 np = Hostile.ClampArena(c.pos + (best - c.pos).normalized * (PhotographerPaths.PuppetSpeed * 0.1f));
        if (Hostile.IsWall(np)) return;
        c.pos = np;
        c.tf.position = np;
    }

    // ================================================================= 매 프레임 (PhLate 가 부름)
    void PhPathTick()
    {
        // 연사 극의: 필름을 감는 동안에도 셔터 (피해 50%)
        if (PhotographerPaths.Master(PhotographerPaths.Burst) && player.IsReloading && GameInput.FireHeld && !Busy
            && !player.IsSkillUsing && !phPano && Time.time >= phWindAt)
        {
            int shots = Mathf.Max(1, player.multiShot);
            phWindAt = Time.time + player.ShootSpeed / (player.fireRateMultiplier * AttackSpeedMul);
            PhotoAttack(Damage * player.MultiShotDamageRate(shots) * PhotographerPaths.BurstWindMul, shots);
        }
        // 종이 공작 공명 II: 종이 더미 (0.5초마다 확인)
        if (phPaperList.Count >= PhotographerPaths.CraftPileN && Time.time >= phPileAt && PhotographerPaths.ResAt(PhotographerPaths.Craft, 2))
        {
            phPileAt = Time.time + 0.5f;
            PhPileCheck();
        }
        // 기록 정리 (2초마다)
        if (Time.time >= phPathPruneAt)
        {
            phPathPruneAt = Time.time + 2f;
            PhPrune(phSnap, (v, now) => v.y < now);
            PhPrune(phStopped, (v, now) => v < now);
        }
    }

    static void PhPrune<T>(Dictionary<EnermyController, T> dict, System.Func<T, float, bool> expired)
    {
        if (dict.Count == 0) return;
        List<EnermyController> gone = null;
        foreach (var kv in dict) if (kv.Key == null || expired(kv.Value, Time.time)) (gone ??= new List<EnermyController>()).Add(kv.Key);
        if (gone != null) foreach (EnermyController g in gone) dict.Remove(g);
    }

    // 종이 적 다섯 이상이 3칸 안에 모이면 접혀 터짐 (공격력 200% · 둘레 4칸 안 보스에게는 50% · 모인 종이는 풀림)
    void PhPileCheck()
    {
        float r2 = PhotographerPaths.CraftPileR * PhotographerPaths.CraftPileR;
        foreach (PhPaper p in phPaperList)
        {
            if (p.e == null || p.e.IsDead) continue;
            Vector3 pos = p.e.transform.position;
            int n = 0;
            foreach (PhPaper q in phPaperList)
                if (q.e != null && !q.e.IsDead && (q.e.transform.position - pos).sqrMagnitude <= r2) n++;
            if (n < PhotographerPaths.CraftPileN) continue;
            PhPileBurst(pos);
            phPileAt = Time.time + 1.5f;
            return;
        }
    }

    void PhPileBurst(Vector3 pos)
    {
        float dmg = Paths.Atk * PhotographerPaths.CraftPileMul * Paths.Power(PhotographerPaths.Craft);
        float r = PhotographerPaths.CraftPileR;
        Fx.Spawn("fx_papertear", pos, r * 2.2f, Color.white, 16f, 0f, 22);
        Play("whoosh", 0.3f, 1.6f);
        RunCounters.Both("paper.pile");
        phPileTmp.Clear();
        foreach (PhPaper p in phPaperList) if (p.e != null && (p.e.transform.position - pos).sqrMagnitude <= r * r) phPileTmp.Add(p);
        using (var ovl = PhysHits.Circle(pos, PhotographerPaths.CraftPileBossR, true)) foreach (Collider2D col in ovl)
        {
            if (col == null || !Paths.IsFoe(col)) continue;
            float d = Vector2.Distance(col.transform.position, pos);
            Vector3 dir = (col.transform.position - pos).normalized;
            if (Paths.IsBoss(col.gameObject)) Paths.Deal(col.gameObject, dmg * PhotographerPaths.CraftPileBossMul, dir, 0f, PhotographerPaths.PileSource);
            else if (d <= r) Paths.Deal(col.gameObject, dmg, dir, 0f, PhotographerPaths.PileSource);
        }
        foreach (PhPaper p in phPileTmp) if (p.e != null && phPapers.ContainsKey(p.e)) PhPaperEnd(p, false);
        Impact.Hit(ImpactTier.Skill, pos, Vector2.zero);
    }
}
