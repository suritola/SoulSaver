using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 일일 도전이 바꾸는 배율 (GameMode 의 같은 이름 배율에 곱함)
public enum DailyStat { EnemyHp, EnemyDamage, EnemySpeed, Spawn, Reward, Gauge, BossHp, EnemySkill }

// 일일 도전 (2.1.1~): 하루(UTC)마다 정해진 캐릭터 · 규칙 하나로 무한 모드를 버팀
// 모두가 같은 조건이라 그날의 스팀 순위표로 겨룸 (SteamLeaderboards). 무한 모드가 열려야 할 수 있음
// 시작할 때 GameMode · CharacterData 의 Override 로 무한 모드 · 그날 캐릭터를 쓰고, 메인 메뉴로 돌아오면 원래대로
public static class DailyChallenge
{
    public struct Rule
    {
        public string name, desc;
        public (DailyStat stat, float mul)[] muls;
        public int attack, maxHp;           // 1.2~ 영혼 각인 규칙의 공격력 · 최대 체력 % (판을 시작할 때 한 번, DailyRewards)
    }

    static readonly Rule[] Rules =
    {
        new Rule { name = "몰려드는 무리", desc = "적이 더 자주 몰려옵니다 (생성 간격 -30%). 대신 경험치 · 코인 +30%",
                   muls = new[] { (DailyStat.Spawn, 0.7f), (DailyStat.Reward, 1.3f) } },
        new Rule { name = "강철 피부", desc = "적 체력 +40%. 대신 적이 주는 피해 -20%",
                   muls = new[] { (DailyStat.EnemyHp, 1.4f), (DailyStat.EnemyDamage, 0.8f) } },
        new Rule { name = "질풍", desc = "적 이동 속도 +20%. 대신 필살기 충전 +40%",
                   muls = new[] { (DailyStat.EnemySpeed, 1.2f), (DailyStat.Gauge, 1.4f) } },
        new Rule { name = "거인의 시간", desc = "보스 체력 +50%. 대신 경험치 · 코인 +25%",
                   muls = new[] { (DailyStat.BossHp, 1.5f), (DailyStat.Reward, 1.25f) } },
        new Rule { name = "유리 몸", desc = "받는 피해 +50%. 대신 필살기 충전 +60%",
                   muls = new[] { (DailyStat.EnemyDamage, 1.5f), (DailyStat.Gauge, 1.6f) } },
        new Rule { name = "분노한 주문", desc = "적이 스킬을 훨씬 자주 씁니다 (+40%). 대신 적 체력 -15%",
                   muls = new[] { (DailyStat.EnemySkill, 0.71f), (DailyStat.EnemyHp, 0.85f) } },
        new Rule { name = "풍요", desc = "경험치 · 코인 +50%. 대신 적 체력 +25%",
                   muls = new[] { (DailyStat.Reward, 1.5f), (DailyStat.EnemyHp, 1.25f) } },
    };

    // 1.2~ 규칙 7 → 14종: 영혼 각인의 규칙을 하루 빌려 씀 (각인을 아직 못 연 플레이어의 맛보기 · 수치는 ContentRegistry.SoulBrands 와 같게)
    //   RulesFrom 이후 날짜만 14종에서 뽑음 — 그 전 날의 규칙(이미 겨룬 순위표)은 그대로
    static readonly DateTime RulesFrom = new DateTime(2026, 10, 20);
    static readonly Rule[] BrandRules =
    {
        new Rule { name = "daily.rule.glass.name", desc = "daily.rule.glass.desc", attack = 40, maxHp = -30, muls = new (DailyStat, float)[0] },
        new Rule { name = "daily.rule.greed.name", desc = "daily.rule.greed.desc", muls = new[] { (DailyStat.Reward, 1.6f), (DailyStat.EnemyHp, 1.3f) } },
        new Rule { name = "daily.rule.iron.name", desc = "daily.rule.iron.desc", attack = -15, muls = new[] { (DailyStat.EnemyDamage, 0.75f) } },
        new Rule { name = "daily.rule.frenzy.name", desc = "daily.rule.frenzy.desc", attack = 25, muls = new[] { (DailyStat.EnemySpeed, 1.2f), (DailyStat.EnemySkill, 0.77f) } },
        new Rule { name = "daily.rule.calm.name", desc = "daily.rule.calm.desc", muls = new[] { (DailyStat.EnemySpeed, 0.85f), (DailyStat.Gauge, 0.65f) } },
        new Rule { name = "daily.rule.bulwark.name", desc = "daily.rule.bulwark.desc", attack = -20, maxHp = 40, muls = new (DailyStat, float)[0] },
        new Rule { name = "daily.rule.dance.name", desc = "daily.rule.dance.desc", attack = 30, muls = new[] { (DailyStat.EnemyDamage, 1.5f), (DailyStat.Reward, 1.3f) } },
    };
    static bool IsBrandRule(Rule r) => r.name != null && r.name.StartsWith("daily.rule.");

    public static bool Active { get; private set; }
    static DateTime runDay;
    static Rule runRule;

    public static DateTime Today => DateTime.UtcNow.Date;
    public static string Key(DateTime day) => day.ToString("yyyyMMdd");
    static int Seed(DateTime day) => day.Year * 10000 + day.Month * 100 + day.Day;
    public static int RunSeed => Seed(runDay);      // 1.2.3~ 무한 지형 변화 시드 (같은 날 같은 얼룩 모양 · 난수 순서 · 얼룩 중심과 조각 자리는 보스가 쓰러진 자리 · 플레이에 따라 다름)

    // 그날의 캐릭터 (만들어진 캐릭터 중에서, 아직 사지 않은 캐릭터도 이날은 빌려 씀)
    public static CharacterId CharacterOf(DateTime day)
    {
        List<CharacterId> ids = new List<CharacterId>();
        for (int i = 0; i < CharacterData.All.Length; i++)
            if (CharacterData.IsDeveloped((CharacterId)i) && !CharacterData.IsNewHero((CharacterId)i)) ids.Add((CharacterId)i);     // 1.0.2 새 캐릭터는 해제 조건이 있어 빌려 주지 않음 (예전 날짜의 캐릭터도 그대로)
        return ids[new System.Random(Seed(day)).Next(ids.Count)];
    }

    public static Rule RuleOf(DateTime day)
    {
        System.Random rng = new System.Random(Seed(day) * 31 + 7);
        rng.Next();
        if (day < RulesFrom) return Rules[rng.Next(Rules.Length)];
        int i = rng.Next(Rules.Length + BrandRules.Length);
        return i < Rules.Length ? Rules[i] : BrandRules[i - Rules.Length];
    }

    // GameMode 배율에 곱하는 값 (일일 도전 중이 아니면 1)
    public static float Mul(DailyStat stat)
    {
        if (!Active || runRule.muls == null) return 1f;
        float m = 1f;
        foreach ((DailyStat s, float mul) in runRule.muls) if (s == stat) m *= mul;
        return m;
    }

    // 1.2~ 난이도 사다리: 쉬움 클리어로 열림 (예전 어려움 클리어 = 무한 모드) — 저장값이 아니라 클리어 기록으로 계산하므로 기존 저장은 저절로 소급
    //   무한 모드가 아직 잠겨 있어도 일일 도전은 GameMode.Override 로 무한 규칙을 씀 (빌려 주는 영웅 · 시드 · 순위표 규칙은 그대로)
    public static bool Unlocked => !Demo.On && GameMode.HasCleared(Difficulty.Easy);
    public static string RunBoard => BoardOf(runDay);
    // 그날의 순위표 이름: 14종 규칙으로 뽑는 날(RulesFrom~)은 「_r2」를 붙여 1.1.9 이하(7종으로 뽑아 규칙이 다름)의 기록과 섞이지 않게
    public static string BoardOf(DateTime day) => SteamLeaderboards.Daily(Key(day) + (day >= RulesFrom ? "_r2" : ""));
    public static string RunDayKey => Key(runDay);      // 1.2~ 일일 보상 (DailyRewards: 자정을 넘겨도 시작한 날)
    public static string RunRuleName => runRule.name ?? "";

    // 1.2~ 각인 규칙의 공격력 · 최대 체력 (판 시작 뒤 플레이어가 생기면 한 번 · 일일 도전은 저장하지 않으므로 기록 없이) — 적용했으면 true
    public static bool ApplyPlayerRule()
    {
        if (!Active) return true;
        PlayerController p = Hostile.Player;
        if (p == null || StageManager.Instance == null || !StageManager.Instance.Started) return false;
        if (runRule.attack != 0) p.damage *= 1f + runRule.attack / 100f;
        if (runRule.maxHp != 0)
        {
            float delta = Mathf.Round(p.PlayerMaxHealth * runRule.maxHp / 100f);
            if (p.PlayerMaxHealth + delta < 1f) delta = 1f - p.PlayerMaxHealth;
            p.PlayerMaxHealth += delta;
            p.PlayerHealth = Mathf.Clamp(p.PlayerHealth + Mathf.Max(0f, delta), 1f, p.PlayerMaxHealth);
        }
        return true;
    }

    public static void Begin()
    {
        if (!Unlocked) return;
        runDay = Today;
        runRule = RuleOf(runDay);
        Active = true;
        GameMode.Override = Difficulty.Endless;
        CharacterData.Override = CharacterOf(runDay);
        SceneFlow.Go("GameScene");
    }

    // 메인 메뉴로 돌아오면 원래 난이도 · 캐릭터로 (메뉴가 난이도 버튼을 그리기 전에도 부름)
    public static void End()
    {
        if (!Active) return;
        Active = false;
        GameMode.Override = null;
        CharacterData.Override = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name == "MainMenu") End();
            // 같은 날은 처음 나오는 것들이 비슷하게 (시간이 흐르면 갈라짐)
            else if (s.name == "GameScene" && Active) UnityEngine.Random.InitState(Seed(runDay));
        };
    }

    // ================================================================= 기록 · 순위
    const string BestKey = "daily.best.";
    public static float Best(DateTime day) => Prefs.GetFloat(BestKey + Key(day), 0f);
    public static float RunBest => Best(runDay);       // 지금 하는 판의 날 (자정을 넘겨도 시작한 날)

    // 결과 화면에 보여 줄 마지막 순위 (올린 순위표 · 순위 · 전체, 받기 전에는 0)
    public static string LastBoard = "";
    public static int LastRank, LastTotal;

    // 무한 모드 판이 끝날 때 (EndlessMode): 일일 도전이면 그날 기록 · 순위표, 아니면 무한 모드 순위표
    public static void Submit(bool daily, float seconds, int bosses)
    {
        if (GameInput.Auto || Application.isBatchMode || seconds < 1f) return;
        if (daily)
        {
            string key = BestKey + Key(runDay);
            if (seconds > Prefs.GetFloat(key, 0f)) { Prefs.SetFloat(key, seconds); Prefs.Save(); }
        }
        string board = daily ? RunBoard : SteamLeaderboards.Endless;
        LastBoard = board;
        LastRank = LastTotal = 0;
        // 자세한 값: 캐릭터 · 보스 처치 · 레벨 · 처치 수 · 1.1.5~ 판 버전 (SteamLeaderboards.DetailCount · 순위 창 영웅 아이콘 · 보스 칸)
        int[] details = { (int)CharacterData.Selected, bosses, RunStats.Level, RunStats.Kills, SteamLeaderboards.VersionCode() };
        SteamLeaderboards.Upload(board, Mathf.FloorToInt(seconds), details, (rank, total) =>
        {
            if (LastBoard != board) return;
            LastRank = rank;
            LastTotal = total;
        });
    }

    // 결과 화면: 올린 순위가 오면 기록 글 끝에 붙임 (스팀이 없으면 그대로)
    public class RankLine : MonoBehaviour
    {
        public TMP_Text text;
        public string baseText;
        public bool daily;

        void Update()
        {
            if (text == null || LastRank <= 0) return;
            // 1.1.5~ 「내 순위 n위 / 전체」 (순위 창 LeaderboardUI 와 같은 말)
            text.text = baseText + "   " + Loc.T(daily ? "오늘의 순위" : "무한 모드 순위") + " · " + Loc.T("lb.myrank").Replace("{0}", LastRank.ToString("N0"))
                      + " <color=#A89C86>/ " + LastTotal.ToString("N0") + "</color>";
            enabled = false;
        }
    }

    // ================================================================= 메인 메뉴 창
    static GameObject open;
    static Color Gold => UiRules.Legacy ? LegacyGold : UiTokens.Text1;      // UI 개편 (Legacy 끔): 금색 글자 대신 뼈색 (기획서 C6 「금색 25벌」)
    static readonly Color LegacyGold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);

    public static void Open(Transform root)
    {
        if (open != null) return;
        DateTime day = Today;
        CharacterId who = CharacterOf(day);
        Rule rule = RuleOf(day);

        RectTransform win = UIKit.Modal(root, "DailyChallenge", new Vector2(1240f, 820f), out open);
        TMP_Text title = UIKit.Text(win, "", 46f, Gold, new Vector2(0f, 355f), new Vector2(900f, 64f));
        title.text = Loc.T("일일 도전") + "  <size=60%><color=#A89C86>" + day.ToString("yyyy-MM-dd") + " (UTC)</color></size>";
        UIKit.MakeButton(win, "닫기", new Vector2(510f, 355f), new Vector2(150f, 58f), Close, 22f);

        // 왼쪽: 오늘의 조건 · 내 기록 · 시작
        TMP_Text info = UIKit.Text(win, "", 28f, Parch, new Vector2(-300f, 60f), new Vector2(560f, 480f), TextAlignmentOptions.TopLeft);
        info.enableAutoSizing = true;
        info.fontSizeMin = 16f;
        info.fontSizeMax = 28f;
        float best = Best(day);
        info.text = "<color=#F5D478>" + Loc.T("오늘의 영웅") + "</color>\n<size=130%>" + Loc.T(CharacterData.Def(who).name) + "</size>\n\n"
                  + "<color=#F5D478>" + Loc.T("오늘의 규칙") + "</color>\n<size=115%>" + Loc.T(rule.name) + "</size>\n" + Loc.T(rule.desc)
                  + (IsBrandRule(rule) ? "\n<size=85%><color=#C9A2FF>" + Loc.T("daily.rule.taste") + "</color></size>" : "") + "\n\n"
                  + "<color=#A89C86>" + Loc.T("무한 모드 규칙으로 오래 버틸수록 높은 순위입니다. 몇 번이든 다시 도전할 수 있고 가장 좋은 기록만 남습니다.") + "</color>\n\n"
                  + "<color=#F5D478>" + Loc.T("오늘 내 최고 기록") + "</color>  " + (best > 0f ? EndlessMode.Clock(best) : "-")
                  + "\n\n" + DailyRewards.WindowLines();        // 1.2~ 오늘의 보상 (5 · 10 · 15분) · 연속 출석

        Button start = UIKit.MakeButton(win, "도전 시작", new Vector2(-300f, -265f), new Vector2(360f, 84f), () => { Close(); Begin(); }, 32f);
        start.interactable = Unlocked;
        if (!Unlocked)
        {
            start.GetComponent<Image>().color = new Color(0.25f, 0.24f, 0.28f, 0.8f);
            // 1.1~ 잠긴 버튼: 판이 기본 disabledColor 와 곱해져 사라지지 않게 하고, 글자도 흐리게 (누를 수 있어 보이지 않게)
            ColorBlock cb = start.colors;
            cb.disabledColor = Color.white;
            start.colors = cb;
            TMP_Text startLabel = start.GetComponentInChildren<TMP_Text>();
            if (startLabel != null) startLabel.color = Dim;
            UIKit.Text(win, Demo.On ? "정식판에서 만날 수 있습니다" : "ladder.lock.daily", 20f, Dim, new Vector2(-300f, -330f), new Vector2(560f, 30f));
        }

        // 오른쪽: 오늘의 순위표
        RectTransform board = UIKit.Rect("Board", win, new Vector2(300f, -20f), new Vector2(560f, 620f));
        Image bg = board.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.18f);
        bg.raycastTarget = false;
        UIKit.Text(board, "오늘의 순위", 28f, Gold, new Vector2(0f, 280f), new Vector2(520f, 40f));
        TMP_Text list = UIKit.Text(board, "", 22f, Parch, new Vector2(0f, -10f), new Vector2(520f, 500f), TextAlignmentOptions.TopLeft);      // 1.1.5~ 아래에 「전체 순위」 버튼 자리
        list.enableAutoSizing = false;
        list.fontSize = 22f;
        list.lineSpacing = 6f;
        BoardView view = open.AddComponent<BoardView>();
        view.list = list;
        view.countdown = UIKit.Text(win, "", 20f, Dim, new Vector2(0f, 300f), new Vector2(900f, 30f));
        view.Load(BoardOf(day));
        // 1.1.5~ 전체 순위 창 (무한 모드 순위와 같은 창 · 일일 도전 순위표로), 닫으면 이 창으로 돌아옴
        if (!Demo.On)
            UIKit.MakeButton(board, "lb.full", new Vector2(0f, -280f), new Vector2(300f, 50f), () =>
            {
                Close();
                LeaderboardUI.Open(root, LeaderboardUI.Board.Daily, SteamLeaderboards.Scope.Global, () => Open(root));
            }, 22f);
    }

    public static void Close()
    {
        if (open != null) UnityEngine.Object.Destroy(open);
        open = null;
    }

    // 순위표 글자 (이름은 스팀이 늦게 알려 줄 수 있어 1초마다 다시 씀) · 다음 도전까지 남은 시간 · ESC 로 닫기
    class BoardView : MonoBehaviour
    {
        public TMP_Text list, countdown;
        List<SteamLeaderboards.Entry> entries;
        int total;
        float next;
        float giveUp;           // 1.1~ 이때까지 답이 없으면 실패 안내
        bool failed;

        public void Load(string boardName)
        {
            if (!SteamLeaderboards.Available)
            {
                list.text = "<color=#A89C86>" + Loc.T("스팀에 연결되어 있지 않아 순위를 볼 수 없습니다.") + "</color>";
                return;
            }
            list.text = "<color=#A89C86>" + Loc.T("불러오는 중…") + "</color>";
            giveUp = Time.unscaledTime + 15f;
            SteamLeaderboards.Top(boardName, 10, (e, t) => { if (this == null) return; entries = e; total = t; failed = e == null; next = 0f; });
        }

        void Update()
        {
            if (GameInput.EscDown && SettingsUI.EscHandledFrame != Time.frameCount) { SettingsUI.EscHandledFrame = Time.frameCount; Close(); return; }     // 1.1.5~ 같은 프레임에 다른 창이 쓴 ESC 는 넘김
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 1f;
            TimeSpan left = Today.AddDays(1) - DateTime.UtcNow;
            countdown.text = Loc.T("다음 도전까지") + " " + ((int)left.TotalHours).ToString("00") + ":" + left.Minutes.ToString("00") + "  " + DailyRewards.ResetLocal();     // 1.2~ 현지 시각도
            if (entries == null)
            {
                // 1.1~ 순위표를 못 받았거나 너무 오래 걸리면 「불러오는 중…」에 멈추지 않고 안내
                if (failed || (SteamLeaderboards.Available && Time.unscaledTime > giveUp))
                    list.text = "<color=#A89C86>" + Loc.T("순위를 불러오지 못했습니다. 창을 다시 열어 보세요.") + "</color>";
                return;
            }
            Loc.EnsureCjkFallback(list);       // 1.1~ 한국어 · 영어에서도 한자 이름이 □ 로 안 보이게
            if (entries.Count == 0) { list.text = "<color=#A89C86>" + Loc.T("아직 기록이 없습니다. 첫 번째 도전자가 되어 보세요!") + "</color>"; return; }
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            foreach (SteamLeaderboards.Entry e in entries)
            {
                // 1.1~ 글자 단위로 자르고 noparse 로 감쌈 (1.1.5~ 순위 창과 같은 SteamLeaderboards.DisplayName)
                string name = SteamLeaderboards.DisplayName(e.user, 14);
                if (string.IsNullOrEmpty(name)) name = "…";
                string line = "#" + e.rank + "  " + name + "  <color=#A89C86>" + EndlessMode.Clock(e.score) + "</color>";
                b.Append(e.me ? "<color=#F5D478>" + line + "</color>" : line).Append('\n');
            }
            b.Append("\n<color=#A89C86>").Append(Loc.T("참가자 ")).Append(total).Append("</color>");
            list.text = b.ToString();
        }
    }
}
