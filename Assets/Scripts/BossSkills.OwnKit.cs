using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 무한 모드 최종 보스 「개발자」 · 「개발자 엄마」의 고유 기본 공격 (영웅 필살기 흉내와 따로 도는 두 번째 차례)
//   기획: docs/review/2026-10-18-개발자-엄마-기본-공격.md · 공격은 BossSkills.DevOwn.cs (개발자 6종) · BossSkills.MomOwn.cs (엄마 6종)
//   이 파일: 차례(OwnTurn) · 고르기 · 문턱 · 자리 잡기 · 시작 · 끝 · 끊기 · 감시 · 피해 · 풀 · 공정성 검사 · 합성음
//
// 끊김 안전 (1.2.7 사고 교훈 · 기획서 E8) — 개발자 · 엄마에게는 ClearSkillState · BusyWatchdog · CancelForErosion 이 돌지 않으므로 스스로 지킨다
//   · 정상 끝(FinishOwn) · 끊기(CancelOwn) · 감시는 모두 같은 ResetOwnState() 로 정리 (돌지 않아도 부름)
//   · 영웅 멈춤 · 메달 멈춤은 끝 시각(heroHoldUntil · medalFreezeUntil)으로만 — 끄는 코드가 안 돌아도 시간이 지나면 저절로 풀림
//   · 모든 기다림은 시각 끝이 있음 (「될 때까지」 루프 없음) · 감시 12초 · Update 에서 부르는 것은 모두 try/catch
//   · 무적 · 공중 · groundAt · 받는 피해 배율 · domainHold · 몸 색 · 회전 · timeScale · 플레이어 감속 · 입력은 건드리지 않음 (되돌릴 코드가 없음)
//   · 스크립트 붙은 오브젝트를 Instantiate 로 복제하지 않음 — 표시는 모두 new GameObject + 렌더러, 보스마다 ownRoot 하나 아래 풀
public partial class BossSkills
{
    enum OwnAtk : byte
    {
        None, LiveCoding, SelectDelete, BuildFail, ErrorPopup, Loading, StackOverflow,
        Slipper, Scratcher, TimeSale, Broom, Medal, LightsOut, Count
    }

    // ================================================================= 상수 (기획서 L2)
    const float OwnFirstDev = 1.6f, OwnFirstMom = 1.2f;
    const float HeroFirstFinal = 4.5f;            // 개발자 · 엄마 첫 영웅 필살기 (BossSkills.Start)
    const float OwnWatchdog = 12f;                // 가장 긴 공격(라이브 코딩 구간 2 · 불 끄고 자 첫 사용) 약 6초의 2배
    const float OwnQuietAfterHero = 1.2f;         // 불 끄고 자: 영웅 필살기가 모두 끝나고 이만큼 지나야
    const float OwnSlowGate = 0.7f;               // 플레이어 이동 배율이 이보다 낮으면 이동 회피 공격을 시작하지 않음
    const int OwnBulletCap = 320;
    const float OwnTeleFloorDev = 0.5f, OwnTeleFloorMom = 0.42f;
    const float OwnPlayerSpeed = 20f;             // 회피 거리 검사의 기준 이동 속도 (GameScene speed 20)
    const int MaxOwnText = 10, MaxOwnLine = 12, MaxOwnDash = 8, MaxOwnDot = 48, MaxOwnGhost = 30, MaxOwnBlock = 24, MaxOwnIcon = 20, MaxOwnRect = 6;
    const int MaxOwnTele = 16;
    // 월드 글자 크기 배율: 화면 확인(QA 캡처)에서 3.4 ~ 4.5 크기 글자(오류 창 「확인」 · 빌드 % · 터미널 줄 · 「쉿… 자는 척!」)가 10px 남짓으로 읽히지 않았음
    //   (머리 위 이름표 SkillTag 가 9 를 쓰는 것과 같은 까닭 — 「4.5 는 화면에서 너무 작아 안 보였음」) → 부르는 쪽 수치는 그대로 두고 여기서 2배
    const float OwnTextScale = 2f;
    const string GlyphChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,;:=+-*/()[]{}<>!?\"'_#&|%";     // fx_codefont 프레임 순서와 같게 (make_fx5.py)

    static readonly Color ErrRed = new Color(1f, 0.35f, 0.3f, 0.95f);
    static readonly Color Terminal = new Color(0.2f, 1f, 0.45f, 0.95f);
    static readonly Color MomPink = new Color(1f, 0.6f, 0.78f, 0.95f);        // EndlessMode.MomTint
    static readonly Color MomWhite = new Color(1f, 0.92f, 0.96f, 0.95f);
    static readonly Color HomeLight = new Color(1f, 0.88f, 0.55f, 0.95f);

    // ================================================================= 상태
    float ownNext = -1f, ownClaimSince = -1f, heroHoldUntil, ownStartedAt, medalFreezeUntil, lastHeroEndAt = -99f;
    bool ownRunning, ownDirty, ownLightOnly, ownPopupSeen, ownLightsSeen, ownCodeSeen;
    int ownEpoch, ownPhase = -1, ownHits, ownMaxHits = 2, heroMovers, ownStackUses;
    OwnAtk ownPick, ownCur, ownLast1, ownLast2, ownSkipOnce;
    Coroutine ownCo;
    static BossSkills ownGlobalOwner;           // 개발자와 엄마가 함께일 때(옛 저장 이어하기에서만) 둘 합쳐 고유 공격 하나
    readonly List<OwnAtk> ownIntro = new List<OwnAtk>(3);
    readonly float[] ownCooldown = new float[(int)OwnAtk.Count];
    readonly Stack<IEnumerator> ownStack = new Stack<IEnumerator>(4);
    readonly List<OwnAtk> ownCand = new List<OwnAtk>(8);
    // 최근 자리 잡기 (최근 10초 합이 6초를 넘으면 문턱 3 이상 공격만 · 굶주림 방지)
    readonly float[] claimFrom = new float[8], claimTo = new float[8];
    int claimIdx;
    // 플레이어 최근 이동 방향 (범위 선택 두 번째 사각형이 붙는 쪽)
    Vector3 ownPlayerLast;
    Vector2 ownPlayerVel;

    bool Mom => boss != null && boss.developerMom;
    bool OwnHoldsHero => ownClaimSince >= 0f || Time.time < heroHoldUntil;
    bool OtherOwnBusy => ownGlobalOwner != null && ownGlobalOwner != this && ownGlobalOwner.ownRunning;
    // 결계 · 침식 대기 중: 도는 고유 공격도 새 줄 · 휘두름 · 슬리퍼를 더 시작하지 않음 (E9)
    static bool OwnWinding => BossUltimate.DevHold || BossUltimate.ErosionPending;
    bool OwnOk(int e) => e == ownEpoch && Alive;

    // 장면 시작 (BossUltimate.ResetStatics)
    public static void ResetOwnStatics() => ownGlobalOwner = null;

    // ================================================================= 차례
    void OwnTurn(PlayerController p)
    {
        float now = Time.time;
        // 플레이어 이동 방향 (부드럽게)
        Vector3 pp = p.transform.position;
        if (Time.deltaTime > 0f)
        {
            Vector2 v = (pp - ownPlayerLast) / Time.deltaTime;
            if (v.sqrMagnitude < 2500f) ownPlayerVel = Vector2.Lerp(ownPlayerVel, v, 0.15f);
        }
        ownPlayerLast = pp;

        if (ownNext < 0f) ownNext = now + (Mom ? OwnFirstMom : OwnFirstDev);
        UpdateOwnPhase();
        if (ownRunning || now < ownNext || OtherOwnBusy) { EndClaim(); return; }
        if (ownPick == OwnAtk.None) ownPick = PickOwn();
        if (ownPick == OwnAtk.None) { ownNext = now + 0.5f; return; }

        bool ready = devRunning <= GateOf(ownPick)
                     && (!NeedsMove(ownPick) || p.MoveSlowNow >= OwnSlowGate)
                     && (ownPick != OwnAtk.TimeSale || heroMovers == 0)
                     && (ownPick != OwnAtk.LightsOut || LightsReady(p));
        if (ready)
        {
            EndClaim();
            OwnAtk a = ownPick;
            ownPick = OwnAtk.None;
            if (!StartOwn(a, p)) { ownSkipOnce = a; ownNext = now + 0.3f; }
            return;
        }
        if (ownClaimSince < 0f) ownClaimSince = now;
        else if (now - ownClaimSince >= ClaimMaxOf(ownPick))
        {
            // 포기: 다시 쓰기 시간 = 자리 잡기 최대 × 2 (같은 공격이 번갈아 뽑혀 영웅 줄이 굶지 않게)
            ownCooldown[(int)ownPick] = Mathf.Max(ownCooldown[(int)ownPick], now + ClaimMaxOf(ownPick) * 2f);
            EndClaim();
            ownNext = now + 1.5f;
            ownPick = OwnAtk.None;
        }
    }

    void EndClaim()
    {
        if (ownClaimSince < 0f) return;
        claimFrom[claimIdx] = ownClaimSince;
        claimTo[claimIdx] = Time.time;
        claimIdx = (claimIdx + 1) % claimFrom.Length;
        ownClaimSince = -1f;
    }

    float RecentClaim()
    {
        float now = Time.time, from = now - 10f, sum = 0f;
        for (int i = 0; i < claimFrom.Length; i++)
        {
            float a = Mathf.Max(claimFrom[i], from), b = Mathf.Min(claimTo[i], now);
            if (b > a) sum += b - a;
        }
        if (ownClaimSince >= 0f) sum += now - Mathf.Max(ownClaimSince, from);
        return sum;
    }

    bool LightsReady(PlayerController p) =>
        devRunning == 0 && Time.time - lastHeroEndAt >= OwnQuietAfterHero && medals.Count == 0 && !p.IsInvincible;

    static int GateOf(OwnAtk a)
    {
        switch (a)
        {
            case OwnAtk.LiveCoding: case OwnAtk.BuildFail: return 3;
            case OwnAtk.SelectDelete: case OwnAtk.Loading: case OwnAtk.StackOverflow: return 2;
            case OwnAtk.ErrorPopup: return 1;
            case OwnAtk.Slipper: case OwnAtk.Scratcher: return 4;
            case OwnAtk.Broom: case OwnAtk.Medal: return 3;
            case OwnAtk.TimeSale: return 1;
            default: return 0;          // 불 끄고 자!
        }
    }

    float ClaimMaxOf(OwnAtk a) => a == OwnAtk.LightsOut ? 5f : Mom ? 3f : 4f;

    // 이동으로 피하는 공격 (감속 중엔 시작하지 않음) — 라이브 코딩(빈틈 사이로) · 불 끄고 자(가만히)만 아님
    static bool NeedsMove(OwnAtk a) => a != OwnAtk.LiveCoding && a != OwnAtk.LightsOut;

    static bool IsLight(OwnAtk a) => a == OwnAtk.LiveCoding || a == OwnAtk.SelectDelete || a == OwnAtk.Slipper || a == OwnAtk.Broom;

    static float CooldownOf(OwnAtk a)
    {
        switch (a)
        {
            case OwnAtk.ErrorPopup: return 8f;
            case OwnAtk.TimeSale: return 7f;
            case OwnAtk.Medal: return 10f;
            case OwnAtk.LightsOut: return 14f;
            default: return 0f;
        }
    }

    int MaxHitsOf(OwnAtk a) => a == OwnAtk.Scratcher || a == OwnAtk.LightsOut || a == OwnAtk.TimeSale ? 1 : 2;

    // 구간: 개발자 66 · 33% (3구간) · 엄마 75 · 50 · 25% (4구간) — 내려가지 않음
    // 1.4.2~ 지금 구간 (0부터 · 등장 전 0) — 무한 모드 HUD 「최종 보스 · 개발자 2/3」 (EndlessMode)
    public int OwnPhase => Mathf.Max(0, ownPhase);
    int PhaseNow()
    {
        float f = HpFrac;
        if (Mom) return f <= 0.25f ? 3 : f <= 0.5f ? 2 : f <= 0.75f ? 1 : 0;
        return f <= 0.33f ? 2 : f <= 0.66f ? 1 : 0;
    }

    void UpdateOwnPhase()
    {
        int ph = PhaseNow();
        if (ownPhase < 0) { ownPhase = ph; return; }      // 등장 (체력 가득 · 이어하기도 다시 가득) — 알림 없음
        if (ph <= ownPhase) return;
        for (int k = ownPhase + 1; k <= ph; k++)
        {
            // 1.4.2~ 무한 모드 단계 보상: 체력 구간을 처음 넘을 때 영혼 조각 (개발자 +30 · 엄마 +40 · 판마다 한 번 · 상태는 바꾸지 않음 · 무한 30분 기획 A3)
            if (EndlessMode.Instance != null && EndlessMode.Instance.ClaimPhaseReward(Mom, k))
            {
                SoulShards.Add(Mom ? EndlessMode.MomPhaseShards : EndlessMode.DevPhaseShards, transform.position, true);
                Hints.Show("endless.phaseshard", "최종 보스의 체력 구간을 넘길 때마다 영혼 조각을 얻습니다.");
            }
            if (Mom)
            {
                if (k == 1) ownIntro.Add(OwnAtk.Broom);
                else if (k == 2) ownIntro.Add(OwnAtk.Medal);
                else if (k == 3) ownIntro.Add(OwnAtk.LightsOut);
            }
            else
            {
                if (k == 1) { ownIntro.Add(OwnAtk.ErrorPopup); ownIntro.Add(OwnAtk.Loading); }
                else if (k == 2) ownIntro.Add(OwnAtk.StackOverflow);
            }
        }
        ownPhase = ph;
        // 큰 알림은 마지막 구간 것만 (큰 피해로 둘을 한 번에 넘어도 한 번)
        string key = "devown.phase." + (Mom ? "mom" : "dev") + ph;
        if (StageManager.Instance != null)
            Notice.Big(Loc.T(key), Loc.T(key + ".sub"), NoticeIcon.Boss, 2.4f, NoticeLevel.Urgent);
        // 규칙형 공격의 도움말은 구간에 들어설 때 (Hints 는 줄을 서서 7초씩이라 첫 사용 때 띄우면 늦음)
        if (!Mom && ph >= 1) Hints.Show("dev.popup", "「오류 창」이 떨어지면 「확인」 버튼 위에 서거나 창 밖으로 나가세요. 창 안의 다른 자리는 모두 지워집니다.");
        if (Mom && ph >= 3) Hints.Show("mom.lights", "엄마가 「불 끄고 자!」를 외치면 문틈 불빛 안에서는 움직이지 마세요. 가만히 있으면 자는 줄 압니다. 불빛 밖으로 나가도 됩니다. 제자리에서 쏘는 것은 괜찮지만, 몸이 앞으로 나가는 공격 · 기술은 들킵니다.");
    }

    bool Unlocked(OwnAtk a)
    {
        int ph = Mathf.Max(0, ownPhase);
        switch (a)
        {
            case OwnAtk.LiveCoding: case OwnAtk.SelectDelete: case OwnAtk.BuildFail: return !Mom;
            case OwnAtk.ErrorPopup: case OwnAtk.Loading: return !Mom && ph >= 1;
            case OwnAtk.StackOverflow: return !Mom && ph >= 2;
            case OwnAtk.Slipper: case OwnAtk.Scratcher: case OwnAtk.TimeSale: return Mom;
            case OwnAtk.Broom: return Mom && ph >= 1;
            case OwnAtk.Medal: return Mom && ph >= 2;
            case OwnAtk.LightsOut: return Mom && ph >= 3;
            default: return false;
        }
    }

    bool Allowed(OwnAtk a, bool starving)
    {
        if (!Unlocked(a) || a == ownSkipOnce || Time.time < ownCooldown[(int)a]) return false;
        if (ownLightOnly && !IsLight(a)) return false;
        if (starving && GateOf(a) < 3) return false;
        if (a == OwnAtk.Medal && medals.Count >= 2) return false;
        if (a == OwnAtk.LightsOut && medals.Count > 0) return false;
        return true;
    }

    // 고르기 (E3): 구간 소개 먼저 → 그 구간에 열린 것 중 다시 쓰기 시간 · 상한 · 바로 앞의 것(4개 이상이면 그 앞도)을 빼고 무작위
    OwnAtk PickOwn()
    {
        bool starving = RecentClaim() > 6f;
        OwnAtk skip = ownSkipOnce;
        ownSkipOnce = OwnAtk.None;
        // 첫 수는 시그니처
        if (ownLast1 == OwnAtk.None) return Mom ? OwnAtk.Slipper : OwnAtk.LiveCoding;
        for (int i = 0; i < ownIntro.Count; i++)
        {
            OwnAtk a = ownIntro[i];
            if (a == skip || ownLightOnly) continue;
            if (Allowed(a, false)) { ownIntro.RemoveAt(i); return a; }
        }
        ownCand.Clear();
        int open = 0;
        for (int i = 1; i < (int)OwnAtk.Count; i++) if (Unlocked((OwnAtk)i)) open++;
        for (int pass = 0; pass < 2 && ownCand.Count == 0; pass++)
            for (int i = 1; i < (int)OwnAtk.Count; i++)
            {
                OwnAtk a = (OwnAtk)i;
                if (a == skip || !Allowed(a, starving && pass == 0)) continue;
                if (pass == 0 && (a == ownLast1 || (open >= 4 && a == ownLast2))) continue;
                ownCand.Add(a);
            }
        if (ownCand.Count == 0) return OwnAtk.None;
        return ownCand[Random.Range(0, ownCand.Count)];
    }

    // ================================================================= 시작 · 끝 · 끊기
    bool StartOwn(OwnAtk a, PlayerController p)
    {
        if (!OwnPrecheck(a, p)) return false;
        OwnSfx.Ensure();
        EnsureOwnRoot();
        ownRunning = true;
        ownDirty = true;
        ownHits = 0;
        ownMaxHits = MaxHitsOf(a);
        ownLast2 = ownLast1;
        ownLast1 = a;
        ownCur = a;
        ownLightOnly = false;
        ownStartedAt = Time.time;
        float cd = CooldownOf(a);
        if (cd > 0f) ownCooldown[(int)a] = Time.time + cd;
        ownGlobalOwner = this;
        boss.casting = true;
        if (a != OwnAtk.BuildFail) ShowOwnTag(a);       // 빌드 실패는 진행 막대가 머리 위를 씀
        ShowOwnAura(Mom ? MomPink : a == OwnAtk.LiveCoding ? Terminal : Dev);
        int e = ++ownEpoch;
        ownCo = StartCoroutine(OwnRoutine(a, e));
        return true;
    }

    // 고유 공격 하나 = Unity 코루틴 하나 (StopCoroutine 한 번으로 모두 멈춤)
    //   공격 안에서 yield return 한 IEnumerator 는 같은 코루틴 안에서 차례로 돌림 · 예외가 나면 그 자리에서 끝내고 정리
    IEnumerator OwnRoutine(OwnAtk a, int e)
    {
        ownStack.Clear();
        ownStack.Push(OwnBody(a, e));
        while (ownStack.Count > 0)
        {
            if (e != ownEpoch) yield break;
            IEnumerator top = ownStack.Peek();
            bool more;
            try { more = top.MoveNext(); }
            catch (System.Exception ex) { Debug.LogException(ex); ownStack.Clear(); break; }
            if (!more) { ownStack.Pop(); continue; }
            if (top.Current is IEnumerator sub) { ownStack.Push(sub); continue; }
            yield return null;
        }
        ownStack.Clear();
        if (e == ownEpoch) FinishOwn();
    }

    IEnumerator OwnBody(OwnAtk a, int e)
    {
        switch (a)
        {
            case OwnAtk.LiveCoding: return LiveCoding(e);
            case OwnAtk.SelectDelete: return SelectDelete(e);
            case OwnAtk.BuildFail: return BuildFail(e);
            case OwnAtk.ErrorPopup: return ErrorPopup(e);
            case OwnAtk.Loading: return LoadingRing(e);
            case OwnAtk.StackOverflow: return StackOverflow(e);
            case OwnAtk.Slipper: return SlipperToss(e);
            case OwnAtk.Scratcher: return BackScratcher(e);
            case OwnAtk.TimeSale: return TimeSale(e);
            case OwnAtk.Broom: return BroomSweep(e);
            case OwnAtk.Medal: return FriendsSon(e);
            default: return LightsOut(e);
        }
    }

    void FinishOwn()
    {
        ownCo = null;
        ResetOwnState();
        ownNext = Time.time + (Mom ? Mathf.Lerp(1.3f, 2.5f, HpFrac) : Mathf.Lerp(1.8f, 3.4f, HpFrac));
    }

    // 결계 · 침식 시작 · 보스 쓰러짐 · 결계 안전장치 · 감시 · 예외: 피해 없이 끊고 이 공격의 표시 · 경고 · 메달을 지움 (이미 쏜 탄은 그대로)
    public void CancelOwn()
    {
        if (ownCo != null) { StopCoroutine(ownCo); ownCo = null; }
        ownStack.Clear();
        ownEpoch++;
        ownPick = OwnAtk.None;
        ClearMedals();
        KillSlippers();
        ResetOwnState();
    }

    // 정리는 한 곳 (정상 끝 · 끊김 · 감시가 같이 씀 · 돌지 않아도 불러도 됨)
    void ResetOwnState()
    {
        // 판 진행을 막을 수 있는 상태(돌고 있음 · 주인 · 영웅 멈춤 · 시전)를 먼저 되돌림 — 아래 표시 정리에서 예외가 나도 남지 않게
        heroHoldUntil = 0f;
        medalFreezeUntil = 0f;
        ownRunning = false;
        ownCur = OwnAtk.None;
        if (ownGlobalOwner == this) ownGlobalOwner = null;
        if (boss != null && devRunning == 0 && !BossUltimate.Active) boss.casting = false;
        EndClaim();
        RestoreRewinds();           // Ctrl+Z 로 멈춘 탄: 속도 되살림 + 곧 사라짐
        EndOwnTelegraphs();         // 이 공격이 띄운 경고 원 · 띠
        HideOwnPools();             // 글자 · 선 · 점선 · 점 · 블록 · 아이콘 · 사각 · 부채꼴
        HideOwnTag();
        ownDirty = false;
    }

    // 결계 · 침식이 끝남 (AfterDomain): 고유는 영웅보다 0.5초 뒤 · 자리 잡기 · 영웅 멈춤 초기화 · 첫 공격은 가벼운 것만 (E9)
    void OwnAfterDomain(float seconds)
    {
        if (boss == null || !boss.developer) return;
        ownNext = Mathf.Max(ownNext, Time.time + seconds + 0.5f);
        EndClaim();
        heroHoldUntil = 0f;
        ownPick = OwnAtk.None;
        ownLightOnly = true;
    }

    // 보스 오브젝트가 꺼짐 (OnDisable — ownRoot · 메달은 owned 와 함께 이미 지워짐 · 코루틴은 Unity 가 멈춤)
    //   다른 오브젝트는 건드리지 않고 필드만 되돌림 (장면 정리 중일 수 있음)
    void OwnOnDisable()
    {
        ownCo = null;
        ownStack.Clear();
        ownEpoch++;
        ownRunning = false;
        ownDirty = false;
        heroHoldUntil = medalFreezeUntil = 0f;
        ownClaimSince = -1f;
        ownPick = OwnAtk.None;
        medals.Clear();
        rwCount = 0;
        teleCount = 0;
        ownTag = null;
        if (ownGlobalOwner == this) ownGlobalOwner = null;
    }

    // BossSkills.Update 맨 앞 (개발자 · 엄마만 · return 줄 앞): 끊기 · 감시 · 메달
    void OwnGuard()
    {
        try
        {
            bool anything = ownRunning || medals.Count > 0 || ownDirty || ownClaimSince >= 0f || heroHoldUntil > 0f || rwCount > 0 || teleCount > 0;
            if (anything && (!Alive || BossUltimate.Active)) { CancelOwn(); return; }
            if (ownRunning && Time.time - ownStartedAt > OwnWatchdog)
            {
                Debug.LogWarning("[BossSkills] 고유 공격 감시: " + ownCur + " 가 " + OwnWatchdog + "초를 넘겨 끊음");
                CancelOwn();
                return;
            }
            if (medals.Count > 0) TickMedals();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            try { CancelOwn(); } catch (System.Exception ex2) { Debug.LogException(ex2); }
            ownNext = Time.time + 5f;
        }
    }

    // ================================================================= 앞검사 (공정성 · E7)
    bool OwnPrecheck(OwnAtk a, PlayerController p)
    {
        switch (a)
        {
            case OwnAtk.ErrorPopup: return PlacePopup(p.transform.position, PopupSize(0), out popupC, out popupBtn);
            case OwnAtk.SelectDelete: return CanOutrun(5.4f, 0.95f);
            case OwnAtk.Loading: return CanOutrun(ownPhase >= 2 ? 3.6f : 6f, 1.4f);
            case OwnAtk.Broom: return CanOutrun(9f, 0.9f + 0.6f);
            case OwnAtk.Scratcher: return CanOutrun(5f, 0.65f + 0.45f);
            case OwnAtk.BuildFail: return CanOutrun(7.5f, 1.2f);
            default: return true;
        }
    }

    // 필요 거리 ÷ (기준 속도 × 지금 감속) + 반응 0.15초 ≤ 경고 시간
    bool CanOutrun(float dist, float warn)
    {
        PlayerController pl = Hostile.Player;
        float mul = pl != null ? Mathf.Max(0.05f, pl.MoveSlowNow) : 1f;
        return dist / (OwnPlayerSpeed * mul) + 0.15f <= warn;
    }

    // 안전 자리: 맵 안 · 벽 아님 · 용암 · 늪 아님 (무한 지형의 웅덩이는 벽이 아니라 IsWall 이 못 잡음)
    static bool SafeSpot(Vector3 p) => Hostile.InArena(p) && !Hostile.IsWall(p) && !EndlessTerrain.NearZone(p, 0.5f);

    static bool PathClear(Vector3 from, Vector3 to)
    {
        for (int i = 1; i <= 3; i++)
            if (!SafeSpot(Vector3.Lerp(from, to, i / 3f))) return false;
        return true;
    }

    float OwnTele(float s) => Mathf.Max(s, Mom ? OwnTeleFloorMom : OwnTeleFloorDev);

    IEnumerator OwnWait(float until, int e)
    {
        while (Time.time < until && OwnOk(e)) yield return null;
    }

    // ================================================================= 피해
    static readonly string[] OwnNames =
    {
        "", "라이브 코딩", "범위 선택", "빌드 실패", "오류 창", "로딩 중…", "스택 오버플로",
        "슬리퍼 투척", "효자손", "타임 세일!", "빗자루질", "엄친아", "불 끄고 자!",
    };
    static string[] ownLabelsDev, ownLabelsMom;
    static string OwnLabelOf(OwnAtk a, bool mom)
    {
        if (ownLabelsDev == null)
        {
            ownLabelsDev = new string[OwnNames.Length];
            ownLabelsMom = new string[OwnNames.Length];
            for (int i = 0; i < OwnNames.Length; i++)
            {
                ownLabelsDev[i] = "개발자 · " + OwnNames[i];
                ownLabelsMom[i] = "개발자 엄마 · " + OwnNames[i];
            }
        }
        int k = Mathf.Clamp((int)a, 0, OwnNames.Length - 1);
        return mom ? ownLabelsMom[k] : ownLabelsDev[k];
    }
    string OwnLabel(OwnAtk a) => OwnLabelOf(a, Mom);

    // raw × Power, 한 대 상한 = 최대 체력 × share / (TryHit 이 곱하는 배율), 끝에 × DevPow (엄마 2배)
    float OwnDmg(float raw, float share)
    {
        PlayerController pl = Hostile.Player;
        float v = raw * Power;
        if (pl != null) v = Mathf.Min(v, pl.PlayerMaxHealth * share / Mathf.Max(0.01f, GameMode.DamageMul * Chapters.HurtMul));
        return v * DevPow;
    }

    bool OwnHit(bool inside, float raw, float share, OwnAtk a)
    {
        PlayerController pl = Hostile.Player;
        if (!inside || pl == null || pl.IsDying || ownHits >= ownMaxHits) return false;
        AttackLabel.Pending = OwnLabel(a);
        bool hit = pl.TryHit(OwnDmg(raw, share));
        AttackLabel.Pending = null;
        if (hit) ownHits++;
        return hit;
    }

    bool PlayerIn(Vector3 at, float r)
    {
        PlayerController pl = Hostile.Player;
        return pl != null && Hostile.HurtDistance(pl, at) < r + 0.2f;      // 1.4.4~ 몸 아래쪽 판정
    }

    HostileProjectile OwnShot(Vector3 pos, Vector2 dir, float speed, float raw, float share, float radius, Color c, OwnAtk a, char glyph)
    {
        HostileProjectile h = Hostile.Shoot(pos, dir, speed, OwnDmg(raw, share), radius, c, 1.2f, 6f);
        if (h == null) return null;
        h.label = OwnLabel(a);
        h.noTrail = true;
        Sprite g = GlyphOf(glyph);
        if (g != null) h.SetGlyph(g, new Color(0.08f, 0.06f, 0.12f, 1f));
        return h;
    }

    // ================================================================= 머리 위 이름표 · 발밑 후광
    GameObject ownTag;
    SpriteRenderer ownAura;

    // 1.6.0~ 보스 몸 틀은 EnemyHitbox 고정 표 (그림을 바꿔도 글자 · 후광 자리 그대로)
    float OwnHeadY => sr != null ? EnemyHitbox.FrameOf(sr).extents.y + 1f : 3f;

    void ShowOwnTag(OwnAtk a)
    {
        HideOwnTag();
        string n = Loc.T(OwnNames[(int)a]);
        if (!n.EndsWith("!") && !n.EndsWith("！") && !n.EndsWith("…")) n += "!";
        ownTag = SkillTag.Show(transform, n, OwnHeadY, Mom ? MomPink : Dev);
    }

    void HideOwnTag()
    {
        if (ownTag != null) Destroy(ownTag);
        ownTag = null;
    }

    void ShowOwnAura(Color c)
    {
        ownAura = OwnIcon(Hostile.Glow, transform.position, sr != null ? EnemyHitbox.FrameOf(sr).size.x * 1.2f : 6f, new Color(c.r, c.g, c.b, 0.35f), 2);
    }

    // 공격 루프가 매 프레임 부름: 후광이 보스 발밑을 따라가며 맥동
    void TickAura()
    {
        if (ownAura == null || !ownAura.gameObject.activeSelf) return;
        Vector3 at = transform.position + Vector3.down * (sr != null ? EnemyHitbox.FrameOf(sr).extents.y * 0.6f : 1f);
        ownAura.transform.position = at;
        Color c = ownAura.color;
        c.a = 0.25f + 0.15f * Mathf.Sin(Time.time * 8f);
        ownAura.color = c;
    }

    // ================================================================= 경고 (끊길 때 EndEarly 로 거둠)
    readonly Telegraph[] teles = new Telegraph[MaxOwnTele];
    readonly int[] teleIds = new int[MaxOwnTele];
    int teleCount;

    Telegraph OwnCircle(Vector3 pos, float r, float dur, Color c) => TrackTele(Hostile.Circle(pos, r, dur, c));
    Telegraph OwnLineWarn(Vector3 a, Vector3 b, float w, float dur, Color c) => TrackTele(Hostile.Line(a, b, w, dur, c));

    Telegraph TrackTele(Telegraph t)
    {
        if (t == null) return null;
        // 가득 차면 이미 끝난 것부터 덮어씀 (번호가 달라졌거나 꺼진 것)
        if (teleCount >= MaxOwnTele)
        {
            int w = 0;
            for (int i = 0; i < teleCount; i++)
                if (teles[i] != null && teles[i].gameObject.activeSelf && teles[i].UseId == teleIds[i]) { teles[w] = teles[i]; teleIds[w] = teleIds[i]; w++; }
            teleCount = w;
            if (teleCount >= MaxOwnTele) return t;
        }
        teles[teleCount] = t;
        teleIds[teleCount] = t.UseId;
        teleCount++;
        return t;
    }

    void EndOwnTelegraphs()
    {
        for (int i = 0; i < teleCount; i++)
        {
            if (teles[i] != null) teles[i].EndEarly(teleIds[i]);
            teles[i] = null;
        }
        teleCount = 0;
    }

    // ================================================================= Ctrl+Z 로 멈춘 탄 (끊기면 속도 되살림 + 곧 사라짐)
    const int MaxRewind = 96;
    readonly HostileProjectile[] rwShot = new HostileProjectile[MaxRewind];
    readonly int[] rwSerial = new int[MaxRewind];
    readonly float[] rwSpeed = new float[MaxRewind];
    readonly bool[] rwFrozen = new bool[MaxRewind];
    readonly Vector3[] rwFrom = new Vector3[MaxRewind];
    int rwCount;

    bool RwValid(int i) => rwShot[i] != null && rwShot[i].isActiveAndEnabled && rwShot[i].Serial == rwSerial[i];

    void RestoreRewinds()
    {
        for (int i = 0; i < rwCount; i++)
        {
            if (rwFrozen[i] && RwValid(i))
            {
                HostileProjectile h = rwShot[i];
                h.speed = rwSpeed[i];
                h.life = Mathf.Min(h.life, 0.6f);       // HostileProjectile.life 는 「남은 수명」 (Update 가 깎음)
                h.Flash(null);
            }
            rwShot[i] = null;
            rwFrozen[i] = false;
        }
        rwCount = 0;
    }

    // ================================================================= 풀 (보스마다 ownRoot 하나 아래 · 보스가 지워지면 owned 와 함께 한 번에)
    GameObject ownRoot;
    readonly List<TextMeshPro> oText = new List<TextMeshPro>();
    readonly List<LineRenderer> oLine = new List<LineRenderer>();
    readonly List<LineRenderer> oDash = new List<LineRenderer>();
    readonly List<SpriteRenderer> oDot = new List<SpriteRenderer>();
    readonly List<SpriteRenderer> oDotOrb = new List<SpriteRenderer>();
    readonly List<SpriteRenderer> oGhost = new List<SpriteRenderer>();
    readonly List<SpriteRenderer> oBlock = new List<SpriteRenderer>();
    readonly List<SpriteRenderer> oIcon = new List<SpriteRenderer>();
    readonly List<SpriteRenderer> oRect = new List<SpriteRenderer>();
    MeshRenderer oCone;
    MaterialPropertyBlock conePb;

    void EnsureOwnRoot()
    {
        if (ownRoot != null) return;
        // OnDisable 뒤 다시 켜졌거나 처음: 지워진 풀 참조를 비우고 새로 (null 검사 후 다시 만듦)
        ownRoot = new GameObject("DevOwnRoot");
        Own(ownRoot);
        oText.Clear(); oLine.Clear(); oDash.Clear(); oDot.Clear(); oDotOrb.Clear(); oGhost.Clear(); oBlock.Clear(); oIcon.Clear(); oRect.Clear();
        oCone = null;
        ownAura = null;
    }

    void HideOwnPools()
    {
        if (ownRoot == null) return;
        foreach (TextMeshPro t in oText) if (t != null) t.gameObject.SetActive(false);
        foreach (LineRenderer l in oLine) if (l != null) l.gameObject.SetActive(false);
        foreach (LineRenderer l in oDash) if (l != null) { SpecialFeedback.Hide(l); l.gameObject.SetActive(false); }
        foreach (SpriteRenderer s in oDot) if (s != null) s.gameObject.SetActive(false);
        foreach (SpriteRenderer s in oGhost) if (s != null) s.gameObject.SetActive(false);
        foreach (SpriteRenderer s in oBlock) if (s != null) s.gameObject.SetActive(false);
        foreach (SpriteRenderer s in oIcon) if (s != null) s.gameObject.SetActive(false);
        foreach (SpriteRenderer s in oRect) if (s != null) s.gameObject.SetActive(false);
        if (oCone != null) oCone.gameObject.SetActive(false);
    }

    TextMeshPro OwnText(Vector3 pos, string s, Color c, float size, int order = 46)
    {
        EnsureOwnRoot();
        TextMeshPro t = null;
        for (int i = 0; i < oText.Count; i++) if (oText[i] != null && !oText[i].gameObject.activeSelf) { t = oText[i]; break; }
        if (t == null)
        {
            if (oText.Count >= MaxOwnText) return null;
            GameObject go = new GameObject("OwnText");
            go.transform.SetParent(ownRoot.transform, false);
            t = go.AddComponent<TextMeshPro>();
            UIKit.EnsureStyle();
            if (UIKit.Font != null) t.font = UIKit.Font;
            Material m = Notice.WorldMaterial(t.font);
            if (m != null) t.fontSharedMaterial = m;
            else if (UIKit.FontMaterial != null) t.fontSharedMaterial = UIKit.FontMaterial;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = false;
            t.fontStyle = FontStyles.Bold;
            t.sortingLayerID = SortingLayer.NameToID("Effect");
            t.rectTransform.sizeDelta = new Vector2(40f, 6f);
            oText.Add(t);
        }
        t.gameObject.SetActive(true);
        t.transform.position = pos;
        t.transform.localScale = Vector3.one;
        t.transform.rotation = Quaternion.identity;
        t.text = s;
        t.color = Hostile.Accessible(c);
        t.fontSize = size * OwnTextScale;
        t.sortingOrder = order;
        t.maxVisibleCharacters = 99999;
        t.alignment = TextAlignmentOptions.Center;
        ownDirty = true;
        return t;
    }

    LineRenderer OwnSolid(Color c, float width, int order = 22)
    {
        EnsureOwnRoot();
        LineRenderer l = null;
        for (int i = 0; i < oLine.Count; i++) if (oLine[i] != null && !oLine[i].gameObject.activeSelf) { l = oLine[i]; break; }
        if (l == null)
        {
            if (oLine.Count >= MaxOwnLine) return null;
            l = Hostile.NewLine("OwnLine", c, width, order);
            l.transform.SetParent(ownRoot.transform, false);
            oLine.Add(l);
        }
        l.gameObject.SetActive(true);
        l.loop = false;
        l.positionCount = 0;
        l.startColor = l.endColor = Hostile.Accessible(c);
        l.startWidth = l.endWidth = width;
        l.sortingOrder = order;
        ownDirty = true;
        return l;
    }

    static void SetSeg(LineRenderer l, Vector3 a, Vector3 b)
    {
        if (l == null) return;
        if (l.positionCount != 2) l.positionCount = 2;
        l.SetPosition(0, a);
        l.SetPosition(1, b);
    }

    LineRenderer OwnDash(int order = 24)
    {
        EnsureOwnRoot();
        LineRenderer l = null;
        for (int i = 0; i < oDash.Count; i++) if (oDash[i] != null && !oDash[i].gameObject.activeSelf) { l = oDash[i]; break; }
        if (l == null)
        {
            if (oDash.Count >= MaxOwnDash) return null;
            // 점선 재질이 하나씩 생기므로 보스마다 MaxOwnDash 개만 한 번 · 만든 뒤 ownRoot 아래로 (안 옮기면 보스가 지워져도 남음)
            if (SpecialAbilities.SharedFx != null) l = SpecialAbilities.SharedFx.NewLine("OwnDash", true, order);
            else l = Hostile.NewLine("OwnDash", Color.white, 0.2f, order);
            l.transform.SetParent(ownRoot.transform, false);
            oDash.Add(l);
        }
        l.gameObject.SetActive(true);
        l.sortingOrder = order;
        ownDirty = true;
        return l;
    }

    static void DashSeg(LineRenderer l, Vector3 a, Vector3 b, Color c, float w)
    {
        if (l == null) return;
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.SetLine(l, a, b, c, w);
        else
        {
            l.enabled = true;
            SetSeg(l, a, b);
            l.startColor = l.endColor = c;
            l.startWidth = l.endWidth = w;
        }
    }

    SpriteRenderer PoolSprite(List<SpriteRenderer> pool, int max, string name, Sprite s, Vector3 pos, float worldH, Color c, int order, string layer = "Effect")
    {
        if (s == null) return null;
        EnsureOwnRoot();
        SpriteRenderer r = null;
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null && !pool[i].gameObject.activeSelf) { r = pool[i]; break; }
        if (r == null)
        {
            if (pool.Count >= max) return null;
            GameObject go = SpecialAbilities.MakeSprite(name, s, pos, 1f, c, layer, order);
            go.transform.SetParent(ownRoot.transform, true);
            r = go.GetComponent<SpriteRenderer>();
            pool.Add(r);
        }
        r.gameObject.SetActive(true);
        r.sprite = s;
        r.color = c;
        r.sortingOrder = order;
        r.flipX = r.flipY = false;
        Transform t = r.transform;
        t.position = pos;
        t.rotation = Quaternion.identity;
        float h = s.bounds.size.y;
        t.localScale = Vector3.one * (h > 0f ? worldH / h : 1f);
        ownDirty = true;
        return r;
    }

    SpriteRenderer OwnIcon(Sprite s, Vector3 pos, float worldH, Color c, int order = 26) => PoolSprite(oIcon, MaxOwnIcon, "OwnIcon", s, pos, worldH, c, order);
    SpriteRenderer OwnBlock(Vector3 pos, float worldH) => PoolSprite(oBlock, MaxOwnBlock, "OwnBlock", FxFrame("fx_codeblock", 0), pos, worldH, Color.white, 24);
    SpriteRenderer OwnGhost(Sprite s, Vector3 pos, float worldH, Color c) => PoolSprite(oGhost, MaxOwnGhost, "OwnGhost", s, pos, worldH, c, 24);

    // 흰 사각 (1칸) — 크기 · 색을 정해 사각형으로 (삭제 번쩍임 · 창 그림자 · 제목 막대)
    SpriteRenderer OwnRect(Vector3 center, float w, float h, Color c, int order)
    {
        SpriteRenderer r = PoolSprite(oRect, MaxOwnRect, "OwnRect", OwnWhite, center, 1f, c, order);
        if (r != null) r.transform.localScale = new Vector3(w, h, 1f);
        return r;
    }

    // 구슬 모양 점 (로딩 고리) — 적 탄과 같은 꾸밈(Dress), 탄은 아님
    SpriteRenderer OwnDot(Vector3 pos, Color c, float diameter, out SpriteRenderer orbSr)
    {
        orbSr = null;
        if (Hostile.Glow == null) return null;
        EnsureOwnRoot();
        int idx = -1;
        for (int i = 0; i < oDot.Count; i++) if (oDot[i] != null && !oDot[i].gameObject.activeSelf) { idx = i; break; }
        if (idx < 0)
        {
            if (oDot.Count >= MaxOwnDot) return null;
            GameObject go = SpecialAbilities.MakeSprite("OwnDot", Hostile.Glow, pos, 1f, c, "HitScan", 9);
            go.transform.SetParent(ownRoot.transform, true);
            oDot.Add(go.GetComponent<SpriteRenderer>());
            oDotOrb.Add(null);
            idx = oDot.Count - 1;
        }
        SpriteRenderer r = oDot[idx];
        r.gameObject.SetActive(true);
        r.transform.position = pos;
        Transform orb = HostileProjectile.Dress(r.gameObject, c, diameter);
        if (orb != null && oDotOrb[idx] == null) oDotOrb[idx] = orb.GetComponent<SpriteRenderer>();
        orbSr = oDotOrb[idx];
        ownDirty = true;
        return r;
    }

    // 부채꼴 채움 (정적 메시 하나 · 재질 하나 · 색은 MaterialPropertyBlock)
    MeshRenderer OwnCone(Vector3 apex, Vector2 dir, float range, Color c)
    {
        EnsureOwnRoot();
        if (oCone == null)
        {
            GameObject go = new GameObject("OwnCone");
            go.transform.SetParent(ownRoot.transform, false);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ConeMesh;
            oCone = go.AddComponent<MeshRenderer>();
            oCone.sharedMaterial = ConeMat;
            oCone.sortingLayerName = "Effect";
            oCone.sortingOrder = 3;
        }
        if (conePb == null) conePb = new MaterialPropertyBlock();
        oCone.gameObject.SetActive(true);
        oCone.transform.position = apex;
        oCone.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        oCone.transform.localScale = new Vector3(range, range, 1f);
        conePb.SetColor("_Color", Hostile.Accessible(c));
        oCone.SetPropertyBlock(conePb);
        ownDirty = true;
        return oCone;
    }

    void ConeColor(Color c)
    {
        if (oCone == null || conePb == null) return;
        conePb.SetColor("_Color", Hostile.Accessible(c));
        oCone.SetPropertyBlock(conePb);
    }

    const float ConeHalf = 65f;
    static Mesh coneMesh;
    static Mesh ConeMesh
    {
        get
        {
            if (coneMesh != null) return coneMesh;
            const int seg = 16;
            Vector3[] v = new Vector3[seg + 2];
            Color[] col = new Color[seg + 2];
            Vector2[] uv = new Vector2[seg + 2];        // 1.5.1~ 가로 = 중심(0) → 끝(1) 반지름 방향 (fx_ind_cone 동심 띠 · 끝 테두리)
            int[] tri = new int[seg * 3];
            v[0] = Vector3.zero;
            uv[0] = new Vector2(0f, 0.5f);
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(-ConeHalf, ConeHalf, i / (float)seg) * Mathf.Deg2Rad;
                v[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                uv[i + 1] = new Vector2(1f, i / (float)seg);
            }
            for (int i = 0; i < v.Length; i++) col[i] = Color.white;
            for (int i = 0; i < seg; i++) { tri[i * 3] = 0; tri[i * 3 + 1] = i + 2; tri[i * 3 + 2] = i + 1; }
            coneMesh = new Mesh { name = "OwnCone", hideFlags = HideFlags.DontUnloadUnusedAsset };
            coneMesh.vertices = v;
            coneMesh.colors = col;
            coneMesh.uv = uv;
            coneMesh.triangles = tri;
            coneMesh.RecalculateBounds();
            return coneMesh;
        }
    }

    static Material coneMat;
    static Material ConeMat
    {
        get
        {
            // 1.5.1~ 단색 → 도트 부채꼴 (반지름 방향 동심 띠 + 끝 테두리 · 모양 · 크기는 같은 메시 그대로)
            if (coneMat == null) coneMat = PixelLine.Cone;
            return coneMat;
        }
    }

    static Sprite ownWhite;
    static Sprite OwnWhite
    {
        get
        {
            if (ownWhite != null) return ownWhite;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Color32[] px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            ownWhite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            ownWhite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return ownWhite;
        }
    }

    static Sprite[] glyphs;
    static Sprite GlyphOf(char c)
    {
        if (glyphs == null || glyphs.Length == 0) glyphs = Fx.Frames("fx_codefont");
        int i = GlyphChars.IndexOf(char.ToUpperInvariant(c));
        return i >= 0 && i < glyphs.Length ? glyphs[i] : null;
    }

    static Sprite FxFrame(string name, int i)
    {
        Sprite[] f = Fx.Frames(name);
        return f != null && f.Length > 0 ? f[Mathf.Clamp(i, 0, f.Length - 1)] : null;
    }

    void OwnFloat(Vector3 at, string text, Color c, float size = 5f)
    {
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.FloatText(at, text, Hostile.Accessible(c), size, 0.6f, 1.0f);
    }
}

// 개발자 · 엄마 고유 공격 합성음 (처음 쓸 때 한 번 · 이미 있으면 그대로)
static class OwnSfx
{
    static float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
    static float N(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
    static float Dec(float t, float d, float p) => Mathf.Pow(Mathf.Clamp01(1f - t / d), p);

    public static void Ensure()
    {
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        if (fx == null) return;
        // 기계식 키보드 「탁」
        fx.MakeClip("dev_key", 0.05f, (t, d, r) =>
            N(r) * Dec(t, 0.008f, 1.5f) * 0.55f + Sin(Mathf.Lerp(2600f, 1800f, t / d), t) * Dec(t, 0.02f, 2f) * 0.25f + Sin(180f, t) * Dec(t, 0.03f, 2f) * 0.3f);
        // 「띠-딩」 오류음
        fx.MakeClip("dev_error", 0.42f, (t, d, r) =>
        {
            bool first = t < 0.14f;
            float f = first ? 880f : 622f;
            float lt = first ? t : t - 0.14f;
            float s = Sin(f, t) * 0.5f + Mathf.Sign(Sin(f, t)) * 0.08f;
            return s * Dec(lt, first ? 0.14f : 0.28f, 1.5f) * 0.6f;
        });
        // 찰싹
        fx.MakeClip("mom_slap", 0.14f, (t, d, r) =>
            N(r) * Dec(t, 0.025f, 1.2f) * 0.75f + Sin(Mathf.Lerp(700f, 220f, t / d), t) * Dec(t, d, 2f) * 0.35f);
        // 문 삐걱
        fx.MakeClip("mom_door", 0.7f, (t, d, r) =>
        {
            float k = t / d;
            float f = 240f + 160f * k + 25f * Mathf.Sin(70f * t);
            return (Mathf.Sign(Sin(f, t)) * 0.12f + Sin(2f * f, t) * 0.08f) * Mathf.Sin(Mathf.PI * k) * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(23f * t)));
        });
        // 쓱싹
        fx.MakeClip("mom_sweep", 0.5f, (t, d, r) => N(r) * 0.32f * Mathf.Abs(Mathf.Sin(14f * t)) * Mathf.Sin(Mathf.PI * t / d));
    }
}
