using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 인형사 갈래 효과 — 키트 쪽 (2.0.0 · 기획서 hero-puppeteer.md 9절 · Paths/Paths.Puppeteer.cs 가 수치 · 문장)
//   팽팽한 줄: 줄 베기 ×1.6 · 굵기 ×1.5 · 하한 0.5 · 줄 끝 ×1.3 · 보스 톱질 · 둘째 가닥   → PupCutSegment · PupTautFor + 여기 (PupPathCutMul · PupPathSaw · PupPathCutSegments)
//   호위 인형: 곁에 둔 더미(휘두름 +40% · 투사체 막기) · 줄 6칸 · 연타 · 뒤이은 휘두름       → PupPathSwingMul · PupPathTick · PupPathComboMul · PupBackSwing
//   바꿔치기: 게이지 12% · 간격 0.8 · 등장 ×1.6 · 꼬인 실 · 처치 게이지 돌려받음            → PupTrySwap 이 부르는 PupPathSwapCutHit · PupPathSwapDone
//   거울 인형: 좌우 대칭 거울 더미 · 겹침 표식 · 합주 · 세 번째 더미                          → PupPathLate · PupPathExtraSwings
//   꼭두각시: 실 4 → 4초 꼭두각시(끌림 · 둘레 벰 · 풀릴 때 터짐) · 볼링 · 얽힘               → PupApplyCut 이 부르는 PupPathCutThread + 여기 (PupPathTick)
//   교차: 이중주(PupPathCutHitMul) · 대역(PupPathStandIn)
//   진화: 천 가닥의 실(풀릴 때 가까운 적 둘에게 실 2) · 인형 호위대(복화술로 끌려온 적을 1초 붙잡음)
// 상태는 모두 「끝나는 시각」 값 · 개수 (켜고 끄는 코루틴 깃발 없음) · 만드는 것에는 개수 상한 (꼭두각시 6 · 호위대 24 · 거울 더미 2)
// 꼭두각시는 EnemyHold 로만 잡음 (보스 · 중간 보스는 잡지 않음) — 위치는 EnemyHold.Place + 경기장 경계, 끝 · 쓰러짐 · 정리에서 ReleaseAll
public partial class CharacterKit
{
    static bool PupOn(int path) => PuppeteerPaths.On(path);

    // ================================================================= 상태
    int pupSwingSerial;                                    // 휘두름 번호 (연타가 같은 휘두름의 두 번째 피해를 구분)
    float pupEntranceUntil, pupStandInUntil;
    Transform pupStandInTf;

    struct PupCombo { public int serial, n; public float at; }
    readonly Dictionary<int, PupCombo> pupCombos = new Dictionary<int, PupCombo>(64);
    readonly Dictionary<int, int> pupKnots = new Dictionary<int, int>(16);
    readonly Dictionary<int, float> pupKnotNext = new Dictionary<int, float>(16);
    readonly Dictionary<int, (int grp, float at)> pupDuet = new Dictionary<int, (int, float)>(64);

    // 꼭두각시 · 호위대 (kind 0 = 꼭두각시 · 1 = 인형 호위대의 1초 붙잡힘)
    sealed class PupPuppet { public EnermyController e; public float until, standUntil, nextBowl; public int kind; public Vector3 standPos; }
    readonly List<PupPuppet> pupPuppets = new List<PupPuppet>(12);
    readonly Dictionary<int, int> pupThreads = new Dictionary<int, int>(64);
    readonly Dictionary<int, float> pupTangleNext = new Dictionary<int, float>(16);
    readonly Dictionary<int, float> pupBowlAt = new Dictionary<int, float>(32);

    // 거울 더미 (0) · 세 번째 더미 (1)
    sealed class PupGhost
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Transform bodyTf;
        public Vector2 pos, vel;
        public float swingAt = -9f;
        public bool active;
    }
    readonly PupGhost[] pupGhosts = new PupGhost[2];
    LineRenderer pupMirLine, pupFanLine;

    // ---------------- 밖에서 읽는 값
    public bool PupGuardNear => Id == CharacterId.Puppeteer && PupOn(PuppeteerPaths.Guard) && PupDistance <= PuppeteerPaths.GuardNear;

    public int PupPuppetCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < pupPuppets.Count; i++) if (pupPuppets[i].kind == 0) n++;
            return n;
        }
    }

    float PupPathBaseLen => PupOn(PuppeteerPaths.Guard) ? PuppeteerPaths.GuardMaxLen : PupBaseLen;

    int PupPuppetCap => Paths.Mastered(PuppeteerPaths.Marionette) ? PuppeteerPaths.PuppetMasteryCap : PuppeteerPaths.PuppetCap;
    float PupPuppetLife => Paths.Mastered(PuppeteerPaths.Marionette) ? PuppeteerPaths.PuppetMasteryLife : PuppeteerPaths.PuppetLife;

    bool PupPathSawOn => PupOn(PuppeteerPaths.Taut) && Paths.Res(PuppeteerPaths.Taut) >= 2;

    // ================================================================= 값 (원래 식에 곱하는 곳)
    // 줄 베기 피해: 팽팽한 줄 ×1.6 (tautPath: 이 줄에 팽팽한 줄이 걸리나 — 거울 줄은 이중주 교차일 때만) · 호위 −30% · 꼭두각시 −40%
    float PupPathCutMul(bool tautPath)
    {
        float m = 1f;
        if (tautPath && PupOn(PuppeteerPaths.Taut)) m *= PuppeteerPaths.TautCut;
        if (PupOn(PuppeteerPaths.Guard)) m *= PuppeteerPaths.GuardCutCost;
        if (PupOn(PuppeteerPaths.Marionette)) m *= PuppeteerPaths.MarionetteCutCost;
        return m;
    }

    // 맞힌 줄 베기 한 번의 배율: 줄 끝 3분의 1 ×1.3 (팽팽한 줄 공명 I) · 이중주 (두 줄이 0.4초 안에 같은 적을 베면 두 배)
    float PupPathCutHitMul(int seg, int id, float proj, bool tautPath)
    {
        float m = 1f;
        if (tautPath && PupOn(PuppeteerPaths.Taut) && Paths.Res(PuppeteerPaths.Taut) >= 1 && proj >= PuppeteerPaths.TautTipFrom) m *= PuppeteerPaths.TautTipMul;
        if ((seg == 0 || seg == 3) && Paths.CrossOn(PuppeteerPaths.CrossDuet))
        {
            int grp = seg == 3 ? 1 : 0;
            if (pupDuet.TryGetValue(id, out var d) && d.grp != grp && Time.time - d.at <= PuppeteerPaths.DuetGap)
            {
                m *= 2f;
                pupDuet.Remove(id);
            }
            else
            {
                if (pupDuet.Count > 96) pupDuet.Clear();
                pupDuet[id] = (grp, Time.time);
            }
        }
        return m;
    }

    // 휘두름 피해 배율 (갈래 몫): 팽팽한 줄 −30% · 곁에 둔 더미 +40% · 등장 ×1.6 / mirRate: 거울 더미가 있을 때 두 더미 각각의 몫 (겹치면 합주)
    float PupPathSwingMul(bool echo, out float mirRate)
    {
        float m = 1f;
        if (PupOn(PuppeteerPaths.Taut)) m *= PuppeteerPaths.TautSwingCost;
        if (PupGuardNear) m *= PuppeteerPaths.GuardSwing;
        if (!echo && Time.time < pupEntranceUntil)
        {
            pupEntranceUntil = 0f;
            m *= PuppeteerPaths.SwapEntranceMul;
            Fx.Spawn("fx_sparkle", pupPos, 1.6f, new Color(1f, 0.9f, 0.6f), 24f);
        }
        mirRate = 1f;
        if (PupOn(PuppeteerPaths.Mirror))
            mirRate = PupMirrorOverlapping && Paths.Res(PuppeteerPaths.Mirror) >= 2 ? PuppeteerPaths.MirrorDuetRate : PuppeteerPaths.MirrorRate;
        return m;
    }

    PupGhost PupMirror => pupGhosts[0] != null && pupGhosts[0].active ? pupGhosts[0] : null;
    bool PupMirrorOverlapping => PupMirror != null && Vector2.Distance(pupPos, PupMirror.pos) <= PuppeteerPaths.MirrorOverlap;

    // ================================================================= 호위 인형
    // 연타 (공명 II): 같은 적을 1.2초 안에 이어 휘두르면 회당 +8% (휘두름으로 줄 때만 · PuppeteerPaths.Outgoing 이 부름)
    public float PupPathComboMul(GameObject target)
    {
        if (!pupSwinging || target == null || !PupOn(PuppeteerPaths.Guard) || Paths.Res(PuppeteerPaths.Guard) < 2) return 1f;
        int id = target.GetInstanceID();
        pupCombos.TryGetValue(id, out PupCombo c);
        int n = c.n;
        if (c.serial != pupSwingSerial)
        {
            n = c.serial != 0 && Time.time - c.at <= PuppeteerPaths.GuardComboGap ? Mathf.Min(PuppeteerPaths.GuardComboMax, c.n + 1) : 0;
            if (pupCombos.Count > 96) PupPruneCombos();
            pupCombos[id] = new PupCombo { serial = pupSwingSerial, n = n, at = Time.time };
        }
        return 1f + PuppeteerPaths.GuardComboStep * n * Paths.Power(PuppeteerPaths.Guard);
    }

    void PupPruneCombos()
    {
        List<int> old = null;
        foreach (var kv in pupCombos) if (Time.time - kv.Value.at > 3f) (old ??= new List<int>()).Add(kv.Key);
        if (old != null) foreach (int k in old) pupCombos.Remove(k);
        if (pupCombos.Count > 160) pupCombos.Clear();
    }

    // 극의: 0.2초 뒤 한 번 더 (피해 60%)
    IEnumerator PupBackSwing(float dmg)
    {
        yield return new WaitForSeconds(PuppeteerPaths.GuardBackDelay);
        if (player != null && !player.IsDying && Time.timeScale > 0f) PupSwing(dmg * PuppeteerPaths.GuardBackRate, false, true);
    }

    // ================================================================= 팽팽한 줄
    // 공명 II: 보스 몸에 걸린 줄의 톱질 (줄 베기와 따로 0.25초마다)
    void PupPathSaw(GameObject go, Vector2 p, Vector2 along)
    {
        Paths.Deal(go, Paths.Atk * PuppeteerPaths.TautSawRate * Paths.Power(PuppeteerPaths.Taut), along, 0f, PuppeteerPaths.SawSource);
        Fx.Spawn(FFxPup("fx_string_cut", "fx_spark"), p, 1.2f, PupSky, 24f);
        if (Time.frameCount % 3 == 0) Play("whoosh", 0.15f, 1.7f);
    }

    // 극의: 줄이 10° 옆으로 한 가닥 더 · 거울 더미 줄 (PupLate 가 줄 판정에서 부름)
    Vector2 PupFanEnd(Vector2 a)
    {
        Vector2 v = pupPos - a;
        float r = PuppeteerPaths.TautSecondAngle * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return a + new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    void PupPathCutSegments(Vector2 a)
    {
        if (PupOn(PuppeteerPaths.Taut) && Paths.Mastered(PuppeteerPaths.Taut) && (pupPos - a).sqrMagnitude > 0.01f)
            PupCutSegment(2, a, PupFanEnd(a), PuppeteerPaths.TautSecondRate, PupTaut, true);
        PupGhost g = PupMirror;
        if (g != null)
        {
            bool duet = Paths.CrossOn(PuppeteerPaths.CrossDuet);
            float t = PupTautFor(Vector2.Distance(a, g.pos), duet);
            PupCutSegment(3, a, g.pos, PuppeteerPaths.MirrorRate, t, duet);
        }
    }

    // ================================================================= 거울 인형
    // 매 프레임 (PupLate): 거울 더미 · 세 번째 더미 따라가기 + 줄 그리기. target = 본 더미가 가려는 점 (영웅 기준 좌우 · 위아래를 뒤집음)
    void PupPathLate(Vector2 me, Vector2 target, float dt, bool paused)
    {
        bool mir = PupOn(PuppeteerPaths.Mirror);
        PupGhostUpdate(0, mir, Hostile.ClampArena(new Vector2(2f * me.x - target.x, target.y)), dt);
        PupGhostUpdate(1, mir && Paths.Mastered(PuppeteerPaths.Mirror), Hostile.ClampArena(new Vector2(target.x, 2f * me.y - target.y)), dt);
        Vector2 a = me + Vector2.up * 0.9f;
        PupGhost g = PupMirror;
        if (g != null) PupDrawLine(ref pupMirLine, "PupMirString", a, g.pos);
        else if (pupMirLine != null) pupMirLine.enabled = false;
        if (PupOn(PuppeteerPaths.Taut) && Paths.Mastered(PuppeteerPaths.Taut)) PupDrawLine(ref pupFanLine, "PupFanString", a, PupFanEnd(a));
        else if (pupFanLine != null) pupFanLine.enabled = false;
    }

    void PupGhostUpdate(int i, bool on, Vector2 target, float dt)
    {
        PupGhost g = pupGhosts[i];
        if (!on)
        {
            if (g != null) { g.active = false; if (g.go != null && g.go.activeSelf) g.go.SetActive(false); }
            return;
        }
        if (g == null || g.go == null)
        {
            g = pupGhosts[i] = new PupGhost();
            g.go = new GameObject(i == 0 ? "PuppetMirror" : "PuppetThird");
            GameObject b = new GameObject("Body");
            b.transform.SetParent(g.go.transform, false);
            g.sr = b.AddComponent<SpriteRenderer>();
            g.bodyTf = b.transform;
            g.sr.sortingLayerID = PlayerLook.TopLayer;
            g.sr.sortingOrder = 29;
            g.sr.color = new Color(0.82f, 0.92f, 1f, 0.85f);
        }
        if (!g.go.activeSelf || !g.active)
        {
            g.go.SetActive(true);
            g.active = true;
            g.pos = target;
            g.vel = Vector2.zero;
        }
        if (dt > 0f) g.pos = Vector2.SmoothDamp(g.pos, target, ref g.vel, PupFollowTime, PupFollowSpeed, dt);
        g.go.transform.position = new Vector3(g.pos.x, g.pos.y, 0f);
        float bob = 0.6f + 0.08f * Mathf.Sin(Time.time * (2f * Mathf.PI / 1.4f) + 1.3f * (i + 1));
        g.bodyTf.localPosition = new Vector3(0f, bob + 0.5f, 0f);
        if (pupFrames != null && pupFrames.Length > 0)
        {
            float sw = Time.time - g.swingAt;
            Sprite s = pupFrames[Mathf.Clamp(sw < 0.25f ? 1 + Mathf.Min(2, (int)(sw / 0.25f * 3f)) : 0, 0, pupFrames.Length - 1)];
            if (g.sr.sprite != s) g.sr.sprite = s;
            float h = s.bounds.size.y;
            float k = h > 0.01f ? 2.4f / h : 1f;
            g.bodyTf.localScale = new Vector3(i == 0 ? -k : k, k, 1f);          // 거울 더미는 좌우를 뒤집어 그림
        }
    }

    // 본 더미가 휘두를 때 같이 (PupSwing 이 부름): 거울 · 세 번째 더미 + 꼭두각시 휘두름. baseDmg = 본 더미의 갈래 배율까지 곱한 피해 (거울 비율 전)
    int PupPathExtraSwings(Vector3 at, float r, float baseDmg, float mirRate)
    {
        int hits = 0;
        if (PupOn(PuppeteerPaths.Mirror))
        {
            float pw = Paths.Power(PuppeteerPaths.Mirror);
            PupGhost g = PupMirror;
            if (g != null)
            {
                bool overlap = Vector2.Distance(pupPos, g.pos) <= PuppeteerPaths.MirrorOverlap;
                int n = PupGhostSwing(g, r, baseDmg * mirRate * pw);
                hits += n;
                // 공명 I: 두 더미가 겹쳐 같은 적을 치면 그 적이 3초 동안 받는 피해 +15%
                if (overlap && n > 0 && Paths.Res(PuppeteerPaths.Mirror) >= 1)
                    Paths.Circle(at, r, 0f, 0f, null, c => SignatureSkills.PupMark(c.gameObject, PuppeteerPaths.MirrorMarkSec, PuppeteerPaths.MirrorMarkMul));
            }
            PupGhost third = pupGhosts[1];
            if (third != null && third.active && Paths.Mastered(PuppeteerPaths.Mirror))
                hits += PupGhostSwing(third, r, baseDmg * PuppeteerPaths.MirrorThirdRate * pw);
        }
        PupPuppetsSwing();
        return hits;
    }

    int PupGhostSwing(PupGhost g, float r, float dmg)
    {
        g.swingAt = Time.time;
        Vector3 at = new Vector3(g.pos.x, g.pos.y, 0f);
        int n = DamageCircle(at, r, dmg, 1.2f, 1.75f);
        Fx.Spawn(FFxPup("fx_puppet_swing", "fx_shock"), at, r * 2f, PupSky, 20f);
        return n;
    }

    // ================================================================= 바꿔치기
    // 가르기에 맞은 적마다 (PupTrySwap 이 부름): 공명 II — 보스에 꼬인 실 (3이면 조임)
    void PupPathSwapCutHit(GameObject go)
    {
        if (go == null || !PupOn(PuppeteerPaths.Swap) || Paths.Res(PuppeteerPaths.Swap) < 2 || !Paths.IsBoss(go)) return;
        int id = go.GetInstanceID();
        if (pupKnotNext.TryGetValue(id, out float nx) && Time.time < nx) return;
        pupKnots.TryGetValue(id, out int k);
        k++;
        if (k < PuppeteerPaths.SwapKnotNeed) { pupKnots[id] = k; return; }
        pupKnots[id] = 0;
        pupKnotNext[id] = Time.time + PuppeteerPaths.SwapKnotGap;
        if (pupKnotNext.Count > 32) pupKnotNext.Clear();
        Paths.Deal(go, Paths.Atk * PuppeteerPaths.SwapKnotRate * Paths.Power(PuppeteerPaths.Swap), Vector3.zero, 0f, PuppeteerPaths.KnotSource);
        Fx.Spawn(FFxPup("fx_string_cut", "fx_spark"), go.transform.position, 2.2f, Color.white, 24f);
        Hostile.Shake(0.1f);
    }

    // 자리 바꾸기가 끝난 뒤 (cut: 가른 적 수): 등장 시작 · 극의 게이지 돌려받음
    void PupPathSwapDone(int cut)
    {
        if (!PupOn(PuppeteerPaths.Swap)) return;
        if (Paths.Res(PuppeteerPaths.Swap) >= 1) pupEntranceUntil = Time.time + PuppeteerPaths.SwapEntrance;
        if (Paths.Mastered(PuppeteerPaths.Swap) && pupSwapKills > 0 && player != null && player.skillGauge != null)
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * Mathf.Min(PuppeteerPaths.SwapMasteryRefundMax, PuppeteerPaths.SwapMasteryRefund * pupSwapKills));
        pupSwapKills = 0;
    }

    // 교차 대역 (바꿔치기 × 꼭두각시): 영웅이 떠난 자리에 가장 가까운 꼭두각시가 끌려와 2초 동안 미끼 (참이면 붙박이 더미를 만들지 않음)
    bool PupPathStandIn(Vector2 hero)
    {
        if (!Paths.CrossOn(PuppeteerPaths.CrossStandIn)) return false;
        PupPuppet best = null;
        float bd = float.MaxValue;
        foreach (PupPuppet p in pupPuppets)
        {
            if (p.kind != 0 || p.e == null) continue;
            float d = ((Vector2)p.e.transform.position - hero).sqrMagnitude;
            if (d < bd) { bd = d; best = p; }
        }
        if (best == null) return false;
        Vector3 spot = new Vector3(hero.x, hero.y, 0f);
        EnemyHold.Place(best.e, spot);
        best.standPos = spot;
        best.standUntil = Time.time + PuppeteerPaths.StandInSec;
        best.until = Mathf.Max(best.until, best.standUntil);
        if (EnermyController.Decoy == null) { EnermyController.Decoy = best.e.transform; EnermyController.DecoyBossToo = false; }
        pupStandInTf = best.e.transform;
        pupStandInUntil = best.standUntil;
        Fx.Spawn(FFxPup("fx_swap_poof", "fx_sparkle"), spot, 3f, Color.white, 20f);
        return true;
    }

    // ================================================================= 꼭두각시
    // 줄에 벤 적에게 실 1 (PupApplyCut 이 부름 · 실 자국은 세지 않음)
    void PupPathCutThread(GameObject go, EnermyController e) => PupAddThreads(go, e, 1);

    void PupAddThreads(GameObject go, EnermyController e, int add)
    {
        if (go == null || !PupOn(PuppeteerPaths.Marionette)) return;
        int id = go.GetInstanceID();
        if (e != null && (e.IsDead || e.holder != null)) return;
        pupThreads.TryGetValue(id, out int n);
        n = Mathf.Min(PuppeteerPaths.ThreadMax, n + add);
        if (n < PuppeteerPaths.ThreadMax)
        {
            if (pupThreads.Count > 128) pupThreads.Clear();
            pupThreads[id] = n;
            return;
        }
        if (e != null && !e.IsMidBoss)
        {
            // 잡몹 · 정예: 실 4 → 꼭두각시 (자리가 없으면 실 4 로 기다림)
            if (PupMakePuppet(e)) pupThreads[id] = 0; else pupThreads[id] = n;
        }
        else if (Paths.IsBoss(go)) PupTangle(go, e, id);
        else pupThreads[id] = 0;
    }

    bool PupMakePuppet(EnermyController e)
    {
        if (PupPuppetCount >= PupPuppetCap || !EnemyHold.TryTake(e, this)) return false;
        pupPuppets.Add(new PupPuppet { e = e, until = Time.time + PupPuppetLife, kind = 0 });
        Fx.Spawn("fx_sparkle", e.transform.position, 1.6f, new Color(0.95f, 0.7f, 0.9f), 24f);
        Play("pop", 0.25f, 1.5f);
        return true;
    }

    // 공명 II 얽힘: 보스 · 중간 보스에 실 4 → 2초 이동 −35% · 받는 피해 +15% (12초에 한 번 · 옮기지 않음)
    void PupTangle(GameObject go, EnermyController e, int id)
    {
        if (Paths.Res(PuppeteerPaths.Marionette) < 2) { pupThreads[id] = 0; return; }
        if (pupTangleNext.TryGetValue(id, out float nx) && Time.time < nx) return;       // 기다리는 동안 실은 4 로 둠
        pupThreads[id] = 0;
        pupTangleNext[id] = Time.time + PuppeteerPaths.TangleGap;
        if (pupTangleNext.Count > 32) pupTangleNext.Clear();
        if (e != null) e.Slow(1f - PuppeteerPaths.TangleSlow, PuppeteerPaths.TangleSec);
        SignatureSkills.PupMark(go, PuppeteerPaths.TangleSec, PuppeteerPaths.TangleMul);
        Fx.Spawn(FFxPup("fx_string_cut", "fx_spark"), go.transform.position, 3f, PupSky, 24f);
        Play("whoosh", 0.35f, 0.8f);
    }

    // 더미가 휘두를 때 (PupPathExtraSwings): 꼭두각시가 제자리에서 둘레를 벰
    void PupPuppetsSwing()
    {
        if (!PupOn(PuppeteerPaths.Marionette) || pupPuppets.Count == 0) return;
        float dmg = Paths.Atk * PuppeteerPaths.PuppetSwingRate * Paths.Power(PuppeteerPaths.Marionette);
        for (int i = 0; i < pupPuppets.Count; i++)
        {
            PupPuppet p = pupPuppets[i];
            if (p.kind != 0 || p.e == null || p.e.IsDead) continue;
            Vector3 at = p.e.transform.position;
            PupPuppetBlast(at, PuppeteerPaths.PuppetSwingR, dmg, 0.6f, p.e.gameObject);
            Fx.Spawn(FFxPup("fx_puppet_swing", "fx_shock"), at, PuppeteerPaths.PuppetSwingR * 2f, new Color(0.95f, 0.7f, 0.9f), 20f);
        }
    }

    // 꼭두각시 둘레 피해 (자기 몸은 빼고)
    void PupPuppetBlast(Vector3 pos, float r, float dmg, float knock, GameObject skip)
    {
        using var list = PhysHits.Circle(pos, r, true);
        foreach (Collider2D c in list)
        {
            if (c == null || !Paths.IsFoe(c) || c.gameObject == skip) continue;
            if (c.TryGetComponent(out EnermyController o) && (o.IsDead || o.swarm != null)) continue;
            Paths.Deal(c.gameObject, dmg, (c.transform.position - pos).normalized, knock, PuppeteerPaths.PuppetSource);
        }
    }

    // 인형 호위대(진화): 복화술로 끌려온 적을 꼭두각시처럼 1초 붙잡음 (SignatureSkills.PupTick 이 부름)
    public void PupHoldVoiced(EnermyController e)
    {
        if (e == null || e.IsMidBoss || pupPuppets.Count >= 24 || !EnemyHold.TryTake(e, this)) return;
        pupPuppets.Add(new PupPuppet { e = e, until = Time.time + 1f, kind = 1 });
    }

    // 휘두름으로 쓰러뜨린 곳이 붙잡힌 적 자리면 참 (인형 호위대: 솜 채우기 회복 두 배)
    public bool PupTakeVoicedKill(Vector3 pos)
    {
        for (int i = 0; i < pupPuppets.Count; i++)
        {
            PupPuppet p = pupPuppets[i];
            if (p.kind == 1 && p.e != null && ((Vector2)(p.e.transform.position - pos)).sqrMagnitude < 2.25f) return true;
        }
        return false;
    }

    // ================================================================= 매 프레임 (PupLate 가 멈추지 않았을 때)
    void PupPathTick()
    {
        // 호위 인형: 곁에 둔 더미 몸(0.8칸)에 닿은 적 투사체가 사라짐
        if (PupGuardNear && HostileProjectile.Live.Count > 0)
        {
            float r2 = PuppeteerPaths.GuardBodyR * PuppeteerPaths.GuardBodyR;
            for (int i = HostileProjectile.Live.Count - 1; i >= 0; i--)
            {
                HostileProjectile hp = HostileProjectile.Live[i];
                if (hp == null || ((Vector2)hp.transform.position - pupPos).sqrMagnitude > r2) continue;
                Fx.Spawn("fx_spark", hp.transform.position, 1.1f, new Color(0.7f, 0.9f, 1f), 24f);
                Destroy(hp.gameObject);
            }
        }
        // 대역 미끼가 끝남 / 사라짐
        if (pupStandInTf != null || pupStandInUntil > 0f)
        {
            if (pupStandInTf == null || Time.time >= pupStandInUntil)
            {
                if (EnermyController.Decoy == pupStandInTf) { EnermyController.Decoy = null; EnermyController.DecoyBossToo = true; }
                pupStandInTf = null;
                pupStandInUntil = 0f;
            }
        }
        if (pupPuppets.Count == 0) return;
        float dt = Time.deltaTime;
        for (int i = pupPuppets.Count - 1; i >= 0; i--)
        {
            PupPuppet p = pupPuppets[i];
            EnermyController e = p.e;
            if (e == null || e.IsDead || !e.isActiveAndEnabled || !ReferenceEquals(e.holder, this))
            {
                if (e != null) EnemyHold.Release(e, this);
                pupPuppets.RemoveAt(i);
                continue;
            }
            if (p.kind == 1)
            {
                if (Time.time >= p.until) { EnemyHold.Release(e, this); pupPuppets.RemoveAt(i); }
                continue;
            }
            if (Time.time >= p.until) { PupPuppetEnd(p); pupPuppets.RemoveAt(i); continue; }
            Vector3 pos = e.transform.position;
            if (Time.time < p.standUntil)
            {
                EnemyHold.Place(e, p.standPos);        // 대역: 그 자리에 서서 적을 끔
                continue;
            }
            // 더미 쪽으로 초당 6칸 끌려감
            Vector3 np = Vector3.MoveTowards(pos, new Vector3(pupPos.x, pupPos.y, 0f), PuppeteerPaths.PuppetPull * dt);
            if (np != pos && !Hostile.IsWall(np)) EnemyHold.Place(e, Hostile.ClampArena(np));
            // 공명 I 볼링: 끌려가는 꼭두각시에 부딪힌 적 (0.1초마다 살핌 · 같은 적은 0.6초에 한 번)
            if (Paths.Res(PuppeteerPaths.Marionette) >= 1 && Time.time >= p.nextBowl)
            {
                p.nextBowl = Time.time + 0.1f;
                PupBowl(e);
            }
        }
        if (pupBowlAt.Count > 96) pupBowlAt.Clear();
    }

    void PupBowl(EnermyController puppet)
    {
        Vector3 pos = puppet.transform.position;
        float dmg = Paths.Atk * PuppeteerPaths.PuppetBowl * Paths.Power(PuppeteerPaths.Marionette);
        using var list = PhysHits.Circle(pos, 0.8f, true);
        foreach (Collider2D c in list)
        {
            if (c == null || !Paths.IsFoe(c) || c.gameObject == puppet.gameObject) continue;
            if (c.TryGetComponent(out EnermyController o) && (o.IsDead || o.swarm != null || o.holder != null)) continue;
            int id = c.gameObject.GetInstanceID();
            if (pupBowlAt.TryGetValue(id, out float next) && Time.time < next) continue;
            pupBowlAt[id] = Time.time + 0.6f;
            Paths.Deal(c.gameObject, dmg, (c.transform.position - pos).normalized, 1.5f, PuppeteerPaths.PuppetSource);
            Fx.Spawn("fx_spark", c.transform.position, 1.2f, new Color(0.95f, 0.7f, 0.9f), 24f);
        }
    }

    // 꼭두각시가 풀림: 그 자리 공격력 80% · 천 가닥의 실(진화)은 실이 가까운 적 둘에게 옮겨 가 실 2
    void PupPuppetEnd(PupPuppet p)
    {
        EnermyController e = p.e;
        Vector3 pos = e.transform.position;
        EnemyHold.Release(e, this);
        pupThreads.Remove(e.gameObject.GetInstanceID());
        PupPuppetBlast(pos, PuppeteerPaths.PuppetSwingR, Paths.Atk * PuppeteerPaths.PuppetEndRate * Paths.Power(PuppeteerPaths.Marionette), 1.2f, e.gameObject);
        Fx.Spawn("fx_shock", pos, PuppeteerPaths.PuppetSwingR * 2f, new Color(0.95f, 0.7f, 0.9f), 20f);
        Play("pop", 0.3f, 1.1f);
        if (PupEvo("me.strings"))
        {
            List<Transform> near = Paths.Nearest(pos, 6f, 2, e.gameObject);
            for (int i = 0; i < near.Count; i++)
            {
                Transform t = near[i];
                if (t == null) continue;
                Fx.Stretch(FFxPup("fx_string_twang", "fx_spark"), pos, t.position, 0.5f, PupSky, 0.2f, 14, 24f);
                PupAddThreads(t.gameObject, t.GetComponent<EnermyController>(), 2);
            }
        }
    }

    // ================================================================= 정리
    // 꼭두각시 · 호위대 놓기, 거울 더미 숨기기 (쓰러짐 · 판 끝 · 씬 전환). destroy: 오브젝트까지 지움
    public void PupPathCleanup(bool destroy = false)
    {
        EnemyHold.ReleaseAll(this);
        pupPuppets.Clear();
        pupThreads.Clear();
        if (EnermyController.Decoy == pupStandInTf && pupStandInTf != null) { EnermyController.Decoy = null; EnermyController.DecoyBossToo = true; }
        pupStandInTf = null;
        pupStandInUntil = 0f;
        for (int i = 0; i < pupGhosts.Length; i++)
        {
            PupGhost g = pupGhosts[i];
            if (g == null) continue;
            g.active = false;
            if (g.go != null) { if (destroy) Destroy(g.go); else g.go.SetActive(false); }
            if (destroy) pupGhosts[i] = null;
        }
        foreach (LineRenderer lr in new[] { pupMirLine, pupFanLine })
        {
            if (lr == null) continue;
            if (destroy) Destroy(lr.gameObject); else lr.enabled = false;
        }
    }
}
