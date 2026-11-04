using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 2.0.0~ 영혼 릴레이 (docs/plans/2026-11-02-big-update/modes.md 1절): 영웅 셋을 차례로 골라 무한 사막에 들어가고,
//   쓰러지면 그 자리 그 시각에 다음 영웅이 일어남 (RelaySwap) · 앞 영웅의 빌드는 사라지되 「유품」 하나가 남음 (RelayHeirlooms)
// 일일 도전 패턴: GameMode.Override = Endless · CharacterData.Override = <지금 주자> → 사막 · 무한 스테이지 · 순위 제출 길(EndlessMode.Finish)이 따라옴
// 메인 메뉴로 돌아오면 ModeRun.EndAll 이 풀어 줌
public static class RelayRun
{
    public static bool Active { get; private set; }
    public static int[] Order = { 0, 1, 2 };                  // 영웅 번호 (CharacterId) 순서
    public static int Index { get; private set; }             // 지금 주자 0 ~ 2
    public static bool Swapping { get; internal set; }        // 교대 논리가 도는 같은 프레임 (피해 · 저장을 거절)
    public static bool HandoffPending { get; private set; }   // 2.0.0~ 검토: 교대가 다음 프레임으로 미뤄진 사이 (물리 콜백 안에서 키트를 지우지 않으려고) — 두 번째 교대 · 쓰러짐 처리를 막음
    public static int FreeLeft, EvoOwed;                      // 아직 못 고른 환급 무료 카드 · 진화 빚
    public static readonly float[] SegmentStart = new float[3];     // 각 주자가 시작한 때 (GameMode.EndlessSeconds)
    public static int BaseMag = 6;                            // 영웅 배율이 곱해지기 전 인스펙터 값 (PlayerController.Start 가 기록)
    public static float BaseReload = 3f;

    public const int Runners = 3;
    public static CharacterId Current => (CharacterId)Order[Mathf.Clamp(Index, 0, Runners - 1)];
    public static CharacterId HeroAt(int k) => (CharacterId)Order[Mathf.Clamp(k, 0, Runners - 1)];

    // ================================================================= 열리는 조건
    public static int OwnedHeroes
    {
        get
        {
            int n = 0;
            for (int i = 0; i < CharacterData.All.Length; i++) if (CharacterData.IsUnlocked((CharacterId)i)) n++;
            return n;
        }
    }
    public static bool Unlocked => !Demo.On && GameMode.IsUnlocked(Difficulty.Endless) && OwnedHeroes >= Runners;

    // ================================================================= 기록 (Prefs)
    const string LastKey = "relay.last", BestAllKey = "relay.best", BestPrefix = "relay.best.", NoticedKey = "relay.unlockNoticed";
    public static int[] LastOrder
    {
        get
        {
            List<int> r = new List<int>();
            foreach (string s in Prefs.GetString(LastKey, "").Split(','))
                if (int.TryParse(s, out int v) && v >= 0 && v < CharacterData.All.Length && CharacterData.IsUnlocked((CharacterId)v) && !r.Contains(v)) r.Add(v);
            return r.Count == Runners ? r.ToArray() : null;
        }
    }
    // 같은 셋이면 같은 보드 (순서는 다른 판으로 보지 않음 · 결정 R3): 작은 순으로 정렬한 두 자리 수
    public static string ComboKey(int[] order)
    {
        int[] s = (int[])order.Clone();
        System.Array.Sort(s);
        return s[0].ToString("00") + "_" + s[1].ToString("00") + "_" + s[2].ToString("00");
    }
    public static float BestOf(int[] order) => Prefs.GetFloat(BestPrefix + ComboKey(order), 0f);
    public static float BestAll => Prefs.GetFloat(BestAllKey, 0f);
    public static float BestOfCombo(string combo) => Prefs.GetFloat(BestPrefix + combo, 0f);
    public static bool UnlockNoticed { get => Prefs.GetInt(NoticedKey, 0) == 1; set { Prefs.SetInt(NoticedKey, value ? 1 : 0); Prefs.Save(); } }

    // ================================================================= 시작 · 끝
    public static void Begin(int[] order)
    {
        if (!Unlocked || !Setup(order)) return;
        Prefs.SetString(LastKey, string.Join(",", Order));
        Prefs.Save();
        SceneFlow.Go("GameScene");
    }

    // 배치 테스트 · 봇 균형 측정용: 장면은 부르는 쪽이 로드 (업적 · 순위 · 포인트는 기존 Auto 조건이 막음)
    public static void BeginForTest(int[] order) { if (Setup(order)) fresh = false; }

    // 2.0.0~ 검토: 「다시 시작」(ESC · 결과 화면 [R] · 같은 영웅으로 다시)은 Begin 을 거치지 않고 GameScene 을 다시 불러서 마지막 주자 · 유품 · 구간 시간이 남았음
    //   SceneFlow.Go("GameScene") → ModeRun.BeforeGameScene 이 여기를 부름 — Begin · Resume 이 막 준비한 첫 판은 그대로, 그 뒤 다시 불러오는 판은 첫 주자부터 새로
    static bool fresh;
    internal static void BeforeGameScene()
    {
        if (!Active) return;
        if (fresh) { fresh = false; return; }
        ResetRunProgress();
    }

    static void ResetRunProgress()
    {
        Index = 0;
        Swapping = false;
        HandoffPending = false;
        FreeLeft = EvoOwed = 0;
        System.Array.Clear(SegmentStart, 0, SegmentStart.Length);
        CharacterData.Override = (CharacterId)Order[0];
        ResetRunState();
    }

    static bool Setup(int[] order)
    {
        if (order == null || order.Length != Runners) return false;
        for (int i = 0; i < Runners; i++)
        {
            if (order[i] < 0 || order[i] >= CharacterData.All.Length) return false;
            for (int j = 0; j < i; j++) if (order[j] == order[i]) return false;
        }
        ModeRun.EndAll();
        Order = (int[])order.Clone();
        ResetRunProgress();
        fresh = true;
        Active = true;
        GameMode.Override = Difficulty.Endless;
        return true;
    }

    // 이어하기 (RunSave.Continue): 첫 주자부터 다시 시작 — 기록 재생의 hz 가 교대를 한 번씩 되풀이해 Index 를 올림
    public static void Resume(int[] order, int[] segments)
    {
        if (!Setup(order)) return;
        if (segments != null) for (int i = 0; i < segments.Length && i < Runners; i++) SegmentStart[i] = segments[i];
    }

    public static void End()
    {
        if (!Active) return;
        Active = false;
        Swapping = false;
        HandoffPending = false;
        fresh = false;
        GameMode.Override = null;
        CharacterData.Override = null;
        ResetRunState();
    }

    static void ResetRunState()
    {
        bossMask = 0;
        handoffAt = -99f;
        handoffKills = 0;
        RelayHeirlooms.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name == "MainMenu") End();
            else if (s.name == "GameScene" && Active) new GameObject("RelayRunner").AddComponent<RelayRunner>();
        };
    }

    // PlayerController.Start (영웅 배율이 곱해지기 전): 교대 때 거너 기본값으로 되돌릴 탄창 · 장전 시간
    public static void CaptureBase(PlayerController p)
    {
        if (p == null) return;
        BaseMag = p.MaxBullet;
        BaseReload = p.reloadTime;
    }

    // ================================================================= 넘겨주기
    // PlayerController.TryHit: 체력이 0 이하가 되어 DeathSequence 를 부를 차례 (최후의 보루 · 불사의 맹세 같은 기존 부활이 모두 실패한 뒤)
    public static bool TryHandoff(PlayerController p)
    {
        if (!Active || Swapping || p == null || p.IsDying || Index >= Runners - 1 || RunSave.Replaying) return false;
        if (HandoffPending) return true;        // 이미 다음 프레임에 교대가 예약됨 — 또 맞아도 두 번 교대하지 않고 쓰러짐도 시작하지 않음
        RelayRunner r = RelayRunner.Instance;
        if (r == null) return false;
        // 2.0.0~ 검토: 이 함수는 물리 콜백(OnTriggerStay2D → TryHit) 안에서 불림 — 거기서 DestroyImmediate(키트)는 막혀 키트가 둘 붙었음
        //   체력 1 + 무적만 주고 실제 교대는 다음 프레임 (코루틴 yield return null 뒤 = 콜백 밖)
        HandoffPending = true;
        p.PlayerHealth = 1f;
        p.GrantInvincibility(3f);
        r.StartCoroutine(DoHandoff(p));
        return true;
    }

    static IEnumerator DoHandoff(PlayerController p)
    {
        yield return null;
        HandoffPending = false;
        if (!Active || p == null || p.IsDying || Index >= Runners - 1) yield break;
        CharacterId prev = Current;
        Vector3 at = p.transform.position;
        float now = GameMode.EndlessSeconds;
        Index++;
        SegmentStart[Index] = now;
        handoffAt = Time.time;
        handoffKills = 0;
        Herald.RecordDeath();       // 그 영웅 죽음 수 · 장의사 묘비 (전령 대사는 마지막 죽음에만)
        RelaySwap.Run(p, Current, false);
        RelayRunner r = RelayRunner.Instance;
        if (r != null) r.StartCoroutine(HandoffFx(p, prev, at));
    }

    static float handoffAt = -99f;
    static int handoffKills;
    static int bossMask;

    // 이어받은 뒤 10초 안에 몇 마리 잡았는지 (계약 「이어받고 10초 안에 30마리」)
    internal static void OnKill()
    {
        if (!Active || Time.time - handoffAt > 10f) return;
        handoffKills++;
        RunCounters.Max("relay.handoffBurst", handoffKills);
    }

    // EndlessMode.OnBossDefeated: 이 주자가 보스를 쓰러뜨렸음 (계약 · 업적 「세 주자 모두」)
    public static void OnBossDefeated()
    {
        if (!Active) return;
        // 2.0.1~ 비트마스크라 Max 가 아니라 OR 로 합침 — Max 면 이어하기 뒤 저장값(예: 1번 주자 0b001)과 새 값(0b100) 중 하나만 남아 「세 주자 모두」가 영영 안 됐음
        bossMask |= RunCounters.Get("relay.bossBy") | (1 << Index);
        RunCounters.Max("relay.bossBy", bossMask);          // 저장값의 비트를 모두 품으므로 늘 저장값 이상
        if (bossMask == 7) SteamAchievements.Unlock(SteamAchievements.RelayAllBoss);
    }

    public static void OnDeveloperDefeated()
    {
        if (Active) SteamAchievements.Unlock(SteamAchievements.RelayDev);
    }

    // EndlessMode.Update: 초마다
    public static void Tick(float seconds)
    {
        if (!Active) return;
        if (Index == 0) RunCounters.Max("relay.segment0", Mathf.FloorToInt(seconds - SegmentStart[0]));
        if (seconds >= 600f) SteamAchievements.Unlock(SteamAchievements.RelayFirst);
        if (Index == 0 && seconds >= EndlessMode.FinalAt) SteamAchievements.Unlock(SteamAchievements.RelayFirstHero);
    }

    // ================================================================= 교대 연출 (0.35초 파동 · 유해 · 말풍선 · 창)
    static IEnumerator HandoffFx(PlayerController p, CharacterId prev, Vector3 at)
    {
        RelayRunner host = RelayRunner.Instance;
        // 유해 소품: 앞 영웅 몸을 회색 · 눕힘, 8초 뒤 흐려져 사라짐
        Sprite body = CharacterUI.Portrait(prev);
        if (body != null)
        {
            GameObject corpse = new GameObject("RelayCorpse");
            corpse.transform.position = at + Vector3.down * 0.3f;
            corpse.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            SpriteRenderer sr = corpse.AddComponent<SpriteRenderer>();
            sr.sprite = body;
            sr.color = new Color(0.55f, 0.55f, 0.6f, 1f);
            if (p.TryGetComponent(out SpriteRenderer pr)) { sr.sortingLayerID = pr.sortingLayerID; sr.sortingOrder = pr.sortingOrder - 1; }
            float fit = 2.6f / Mathf.Max(0.5f, body.bounds.size.y);
            corpse.transform.localScale = Vector3.one * fit;
            corpse.AddComponent<RelayFade>().Begin(8f);
        }
        // 시간 0.35배로 시작해 실제 시간 0.6초 동안 1 로 (조작은 막지 않음)
        host.StartCoroutine(TimeDip());
        Hostile.Play("bigboom", 0.6f, 1.3f);
        SkinFx.OnRevive();      // 2026-11-04~ 새 주자의 캐릭터 스킨 교대 등장 연출 (희귀부터 · 기록 재생 중엔 건너뜀)

        yield return new WaitForSecondsRealtime(0.35f);
        // 2.0.1~ 그 0.35초 사이 ESC · 창으로 멈췄으면 풀릴 때까지 기다림 (멈춘 화면에서 파동 피해 · 처치가 터졌음) — 상한은 없어도 됨: 남기는 상태가 없고 장면이 바뀌면 함께 끝남
        while (Time.timeScale == 0f) yield return null;
        if (p == null || p.IsDying) yield break;
        Vector3 c = p.transform.position;
        Wave(p, c);
        Impact.Hit(ImpactTier.Ult, c);
        ShockRing.Spawn(c, 0.5f, WaveRadius * 1.1f, 0.45f, new Color(0.6f, 0.9f, 1f), 0.6f);
        Fx.Spawn("fx_shock", c, WaveRadius * 2f, new Color(0.6f, 0.9f, 1f), 16f);
        if (SpecialAbilities.SharedFx != null)
            SpecialAbilities.SharedFx.FloatText(c + Vector3.up * 2.2f, Loc.T(CharacterData.Current.name), CharacterData.Current.color, 6f, 0f, 1.5f);
        // 2.0.1~ 유품 설명 한 줄 (전엔 어디에도 안 보였음) · 읽을 시간 6초
        Notice.Toast(NoticeIcon.Star, Loc.T("relay.toast.handoff").Replace("{0}", Loc.T(CharacterData.Def(prev).name)).Replace("{1}", RelayHeirlooms.NameOf(prev))
                                      + "\n" + RelayHeirlooms.DescOf(prev), 6f);        // (알림은 size 태그를 지우고 높이를 글에 맞춤 · 110 넘으면 글자가 줄어듦)

        yield return new WaitForSecondsRealtime(0.25f);
        yield return Pending();
    }

    // 쓰러진 자리에서 둘레 7칸: 일반 적은 공격력 300% + 밀쳐냄, 보스 · 중간 보스는 50% 피해만 (밀리지 않음)
    public const float WaveRadius = 7f;
    static void Wave(PlayerController p, Vector3 c)
    {
        float dmg = p.damage * p.damageMultiplier;
        using (DamageSource.As("영혼 이어받기"))
        {
            List<EnermyController> near = new List<EnermyController>();
            foreach (EnermyController e in EnermyController.Live)
                if (e != null && !e.IsDead && (e.transform.position - c).sqrMagnitude <= WaveRadius * WaveRadius) near.Add(e);
            foreach (EnermyController e in near)
            {
                Vector3 d = e.transform.position - c;
                d.z = 0f;
                if (d.sqrMagnitude < 0.01f) d = Vector3.right;
                if (e.IsMidBoss) { e.TakeDamage(dmg * 0.5f, 0f, d.normalized); continue; }
                if (e.holder == null) e.transform.position = Hostile.ClampArena(c + d.normalized * (WaveRadius + 0.5f));
                e.TakeDamage(dmg * 3f, 1f, d.normalized);
            }
            foreach (bosss b in new List<bosss>(bosss.Live))
            {
                if (b == null || b.IsDead || (b.transform.position - c).sqrMagnitude > (WaveRadius + 1.5f) * (WaveRadius + 1.5f)) continue;
                b.TakeDamage(dmg * 0.5f, 0f, (b.transform.position - c).normalized);
                RunCounters.Add("relay.burstBoss");
            }
        }
    }

    static IEnumerator TimeDip()
    {
        if (!Mathf.Approximately(Time.timeScale, 1f)) yield break;
        float expect = 0.35f, t = 0f;
        Time.timeScale = expect;
        while (t < 0.6f)
        {
            yield return null;
            if (Time.timeScale == 0f) continue;      // 2.0.0~ 검토: 창이 멈춘 동안은 기다림 (창이 닫으며 Juice.Settle 로 1 을 돌려줌 → 아래에서 끝남)
            if (Mathf.Abs(Time.timeScale - expect) > 0.02f) yield break;      // 일시정지 · 창이 시간을 잡았으면 맡김
            t += Time.unscaledDeltaTime;
            expect = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(t / 0.6f));
            Time.timeScale = expect;
        }
        if (Mathf.Abs(Time.timeScale - expect) <= 0.02f) Time.timeScale = 1f;
    }

    // 진화 빚 → 환급 무료 카드 (결계 · 등장 연출이 끝난 뒤) · 이어하기 재생이 끝난 뒤에도 같은 길
    public static IEnumerator Pending()
    {
        StageManager sm = StageManager.Instance;
        LevelShop shop = Object.FindFirstObjectByType<LevelShop>();
        while (PlayerController.Cinematic || BossUltimate.Active || Time.timeScale == 0f) yield return null;
        while (EvoOwed > 0)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null || pl.IsDying) yield break;
            // 2.0.1~ 다른 진화(보스 처치 · 앞선 교대의 Pending)가 기다리거나 창을 연 동안 함께 열면 창이 두 번 떠 단계가 꼬였음 — 끝날 때까지 기다림
            //   상한: 시간이 흐르는 동안만 20초 (창이 열려 멈춘 시간은 세지 않음 · 진화 루틴이 끊겨 evolving 이 남아도 판이 막히지 않게)
            float waited = 0f;
            while (sm != null && sm.IsEvolving && waited < 20f)
            {
                if (Time.timeScale > 0f) waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (EvoOwed <= 0) break;            // 기다리는 사이 다른 Pending 이 빚을 다 갚았음
            pl = Hostile.Player;
            if (pl == null || pl.IsDying) yield break;
            if (sm != null) yield return sm.Evolution(true);
            EvoOwed = Mathf.Max(0, EvoOwed - 1);
            RunSave.MarkDirty();
        }
        if (shop != null && FreeLeft > 0) shop.RelayGrantFree(FreeLeft);
    }

    // ================================================================= 판이 끝날 때 (EndlessMode.Finish)
    public static float LastPrevBest;                 // 이 조합의 이전 최고 (결과 화면 「이전 → 이번」)
    public static bool LastNewRecord;
    public static string LastCombo = "";

    public static void Submit(float seconds, int bosses)
    {
        LastCombo = ComboKey(Order);
        LastPrevBest = BestOf(Order);
        LastNewRecord = false;
        DailyChallenge.LastBoard = "";
        DailyChallenge.LastRank = DailyChallenge.LastTotal = 0;
        if (GameInput.Auto || Application.isBatchMode || seconds < 1f) return;
        if (seconds > LastPrevBest)
        {
            Prefs.SetFloat(BestPrefix + LastCombo, seconds);
            LastNewRecord = true;
        }
        if (seconds > BestAll) Prefs.SetFloat(BestAllKey, seconds);
        Prefs.Save();
        int code = Order[0] * 10000 + Order[1] * 100 + Order[2];
        int[] details = { code, bosses, RunStats.Level, RunStats.Kills, SteamLeaderboards.VersionCode() };
        int score = Mathf.FloorToInt(seconds);
        string combo = SteamLeaderboards.Relay(LastCombo);
        DailyChallenge.LastBoard = combo;
        SteamLeaderboards.Upload(SteamLeaderboards.RelayAll, score, details, null);
        SteamLeaderboards.Upload(combo, score, details, (rank, total) =>
        {
            if (DailyChallenge.LastBoard != combo) return;
            DailyChallenge.LastRank = rank;
            DailyChallenge.LastTotal = total;
        });
    }

    // 결과 화면 세 줄 (MenuExtras.InstallGameOver)
    public static string ResultText(float seconds, int bosses)
    {
        System.Text.StringBuilder names = new System.Text.StringBuilder();
        System.Text.StringBuilder seg = new System.Text.StringBuilder();
        for (int k = 0; k < Runners; k++)
        {
            string hero = Loc.T(CharacterData.Def(HeroAt(k)).name);
            if (k > 0) { names.Append(" → "); seg.Append(" · "); }
            names.Append(hero);
            if (k > Index) { seg.Append(hero).Append(" -"); continue; }
            float from = SegmentStart[k];
            float to = k < Index ? SegmentStart[k + 1] : seconds;
            seg.Append(hero).Append(' ').Append(EndlessMode.Clock(Mathf.Max(0f, to - from)));
        }
        string best = LastNewRecord
            ? Loc.T("relay.result.best") + " " + (LastPrevBest > 0f ? EndlessMode.Clock(LastPrevBest) + " → " : "") + EndlessMode.Clock(seconds) + " <color=#F5D478>(" + Loc.T("relay.result.newrecord") + ")</color>"
            : Loc.T("relay.result.best") + " " + EndlessMode.Clock(Mathf.Max(LastPrevBest, BestOf(Order)));
        return Loc.T("영혼 릴레이") + " · " + names + "\n"
             + Loc.T("생존 시간 ") + EndlessMode.Clock(seconds) + "   " + Loc.T("보스 처치 ") + bosses + "   <size=80%><color=#A89C86>(" + seg + ")</color></size>\n"
             + best;
    }

    // ================================================================= 저장 (RunSave.Stage / Continue)
    public static void SaveTo(RunSave.Data d)
    {
        d.relay = string.Join(",", Order);
        d.relayIndex = Index;
        d.relayEvoOwed = EvoOwed;
        d.relayFreeLeft = FreeLeft;
        d.relaySegments = Mathf.RoundToInt(SegmentStart[0]) + "," + Mathf.RoundToInt(SegmentStart[1]) + "," + Mathf.RoundToInt(SegmentStart[2]);
    }

    public static int[] ParseInts(string s, int count)
    {
        if (string.IsNullOrEmpty(s)) return null;
        string[] p = s.Split(',');
        if (p.Length != count) return null;
        int[] r = new int[count];
        for (int i = 0; i < count; i++) if (!int.TryParse(p[i], out r[i])) return null;
        return r;
    }

    // 저장된 릴레이 판이 이어하기 가능한지 (셋 모두 해금)
    public static bool SaveValid(RunSave.Data d)
    {
        int[] o = ParseInts(d.relay, Runners);
        if (o == null) return false;
        foreach (int v in o) if (v < 0 || v >= CharacterData.All.Length || !CharacterData.IsUnlocked((CharacterId)v)) return false;
        return true;
    }

    // hz 재생이 부름: 기록 한 줄 = 교대 한 번
    internal static void ReplayAdvance()
    {
        if (Index < Runners - 1) Index++;
        CharacterData.Override = Current;
    }

    // 재생이 끝난 뒤: 저장해 둔 진화 빚 · 환급 카드를 되살림 (RunSave.Restore)
    public static void RestoreAfterReplay(RunSave.Data d)
    {
        Index = Mathf.Clamp(d.relayIndex, 0, Runners - 1);
        EvoOwed = Mathf.Max(0, d.relayEvoOwed);
        FreeLeft = Mathf.Max(0, d.relayFreeLeft);
        CharacterData.Override = Current;
        RelayHeirlooms.Rebuild();
    }

    // ================================================================= 주자 띠 (HUD)
    public class RelayRunner : MonoBehaviour
    {
        public static RelayRunner Instance { get; private set; }
        readonly Image[] portraits = new Image[Runners];
        readonly Image[] frames = new Image[Runners];
        readonly TMP_Text[] marks = new TMP_Text[Runners];
        TMP_Text heirloomText;
        int shownIndex = -1, shownLang = -1;
        float nextTick;

        void Awake()
        {
            Instance = this;
            EnermyController.Killed += Killed;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            EnermyController.Killed -= Killed;
        }

        static void Killed(Vector3 pos) => OnKill();

        void Start() => Build();

        void Build()
        {
            Canvas canvas = UIKit.HudCanvas();
            if (canvas == null) return;
            UIKit.EnsureStyle();
            RectTransform strip = UIKit.Rect("RelayStrip", canvas.transform, Vector2.zero, new Vector2(520f, 76f));
            strip.anchorMin = strip.anchorMax = strip.pivot = new Vector2(0.5f, 1f);
            strip.anchoredPosition = new Vector2(0f, -78f);
            for (int k = 0; k < Runners; k++)
            {
                RectTransform f = UIKit.Rect("Frame" + k, strip, new Vector2(-70f + 70f * k, -22f), new Vector2(40f, 40f));
                frames[k] = f.gameObject.AddComponent<Image>();
                frames[k].raycastTarget = false;
                RectTransform pr = UIKit.Rect("Portrait", f, Vector2.zero, new Vector2(34f, 34f));
                portraits[k] = pr.gameObject.AddComponent<Image>();
                portraits[k].sprite = CharacterUI.Portrait(HeroAt(k));
                portraits[k].preserveAspect = true;
                portraits[k].raycastTarget = false;
                marks[k] = UIKit.Text(f, "", 22f, new Color(0.9f, 0.9f, 0.95f), Vector2.zero, new Vector2(40f, 40f));
                marks[k].raycastTarget = false;
            }
            heirloomText = UIKit.Text(strip, "", 18f, new Color(0.78f, 0.74f, 0.9f), new Vector2(0f, -56f), new Vector2(520f, 24f));
            heirloomText.raycastTarget = false;
            UiScaler.Isolate(strip.gameObject);
        }

        void Update()
        {
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 0.25f;
            if (portraits[0] == null) return;
            if (shownIndex != Index || shownLang != Loc.Version)
            {
                shownIndex = Index;
                shownLang = Loc.Version;
                for (int k = 0; k < Runners; k++)
                {
                    bool dead = k < Index, now = k == Index;
                    portraits[k].color = dead ? new Color(0.45f, 0.45f, 0.5f, 0.8f) : Color.white;       // 쓰러진 주자는 회색 + 묘비
                    frames[k].color = now ? new Color(0.96f, 0.83f, 0.47f, 0.95f) : new Color(0f, 0f, 0f, 0.55f);
                    marks[k].text = dead ? "†" : "";
                }
            }
            heirloomText.text = RelayHeirlooms.HudLine();
            heirloomText.enabled = !Notice.BigShowing;      // 2.0.0~ 화면 검사: 큰 알림 띠가 떠 있는 동안은 띠 글자와 겹쳐 읽히지 않음
        }
    }

    // 유해 소품: 몇 초 뒤 흐려져 사라짐
    class RelayFade : MonoBehaviour
    {
        float life, t;
        SpriteRenderer sr;
        public void Begin(float seconds) { life = seconds; sr = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            t += Time.deltaTime;
            if (sr != null && t > life - 2f) { Color c = sr.color; c.a = Mathf.Clamp01((life - t) / 2f); sr.color = c; }
            if (t >= life) Destroy(gameObject);
        }
    }
}
