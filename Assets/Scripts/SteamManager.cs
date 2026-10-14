#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX) && !DISABLESTEAMWORKS
#define STEAM
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if STEAM
using Steamworks;
#endif

// 스팀 연동 (Steamworks.NET): 초기화 · 콜백 · 업적
// 스팀용 빌드에서만 켜짐. GitHub 빌드는 BuildScript가 DISABLESTEAMWORKS를 넣어서 스팀 없이 실행됨
// 업적은 게임 이벤트를 듣고 있다가 조건을 채우면 SteamAchievements.Unlock으로 풂
public class SteamManager : MonoBehaviour
{
    // Steamworks 파트너 사이트의 App ID (tools/steam/steam-config.json · steam-demo-config.json 의 appId 와 같아야 함)
    public const uint FullAppId = 5328770;
    public const uint DemoAppId = 5369030;
#if SOULSAVER_DEMO
    public const uint AppId = DemoAppId;        // 체험판 (Demo.cs)
#else
    public const uint AppId = FullAppId;
#endif

    public static SteamManager Instance { get; private set; }
    public static bool Initialized { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("SteamManager");
        DontDestroyOnLoad(go);
        go.AddComponent<SteamManager>();
    }

    void Awake()
    {
        Instance = this;
#if STEAM
        if (!Packsize.Test() || !DllCheck.Test())
        {
            Debug.LogWarning("[Steam] Steamworks.NET 설정이 잘못되었습니다 (Packsize / DllCheck)");
            return;
        }
        // 스팀 밖에서 실행하면 스팀을 통해 다시 실행 (에디터 · App ID 미설정 때는 건너뜀)
        if (!Application.isEditor && AppId != 0 && SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
        {
            Application.Quit();
            return;
        }
        try { Initialized = SteamAPI.Init(); }
        catch (System.DllNotFoundException e) { Debug.LogWarning("[Steam] steam_api 라이브러리를 찾지 못했습니다: " + e.Message); }
        if (!Initialized) Debug.LogWarning("[Steam] 초기화 실패 - 스팀이 켜져 있는지, steam_appid.txt가 있는지 확인하세요");
#endif
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnermyController.Killed += OnEnemyKilled;
        SpecialAbilities.Evolved += _ =>
        {
            SteamAchievements.Unlock(SteamAchievements.Evolve);
            if (++runEvolves >= 3) SteamAchievements.Unlock(SteamAchievements.Evolve3);
        };
        PlayerController.UltUsed += () =>
        {
            SteamAchievements.Unlock(SteamAchievements.FirstUlt);
            if (++runUlts >= 30) SteamAchievements.Unlock(SteamAchievements.Ult30);
        };
        Shop.StallOpened += () => SteamAchievements.Unlock(SteamAchievements.Shopper);
        StageManager.Cleared += OnCleared;
        SpecialAbilities.Rerolled += () => SteamAchievements.Unlock(SteamAchievements.Reroll);
        SkinData.Bought += OnSkinBought;
    }

    // 스킨 구매 (예전에 산 스킨은 씬을 불러올 때 CheckOwnedSkins로 확인)
    void OnSkinBought(SkinDef s)
    {
        SteamAchievements.Unlock(SteamAchievements.Skin);
        if (s.tier >= 3) SteamAchievements.Unlock(SteamAchievements.SkinLegend);
    }

    void CheckOwnedSkins()
    {
        foreach (SkinDef s in SkinData.All)
            if (SkinData.Owns(s.id)) OnSkinBought(s);
    }

    // 한 판 클리어 (3장 보스): 난이도별 업적
    void OnCleared(Difficulty d)
    {
        if (d == Difficulty.Easy) SteamAchievements.Unlock(SteamAchievements.UnlockDifficulty);
        if (d == Difficulty.Normal) SteamAchievements.Unlock(SteamAchievements.ClearNormal);
        if (d == Difficulty.Hard) SteamAchievements.Unlock(SteamAchievements.ClearHard);
    }

    // ================================================================= 업적 조건
    // 여러 판에 걸친 누적 처치 수 (이 PC의 PlayerPrefs)
    const string TotalKillsKey = "stats.totalKills";
    const int TotalKillsGoal = 3000;

    int runKills, runEvolves, runUlts;
    EnemySpawner spawner;
    PlayerController player;

    // 보스전: 피해를 받았는지 (체력이 한 번이라도 줄었는지)
    bool bossFight, bossHit;
    float lastHealth;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameOver" && !RunStats.Cleared) SteamAchievements.Unlock(SteamAchievements.FirstDeath);     // 1.0.4~: 클리어 뒤 결과 화면은 죽음이 아님
        CheckOwnedSkins();
        if (scene.name != "GameScene") return;
        runKills = runEvolves = runUlts = 0;
        bossFight = false;
        player = FindFirstObjectByType<PlayerController>();
        spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null) return;
        spawner.onMidBossDefeated += () => SteamAchievements.Unlock(SteamAchievements.MidBoss);
        spawner.onBossDefeated += OnBossDefeated;
    }

    void OnBossDefeated(int stage)
    {
        // 무한 모드의 보스는 스테이지 업적과 상관없음
        if (!GameMode.IsEndless)
        {
            if (stage == 0) SteamAchievements.Unlock(SteamAchievements.BossLich);
            else if (stage == 1) SteamAchievements.Unlock(SteamAchievements.BossDemon);
            else if (stage == AbyssStage.Index)
            {
                SteamAchievements.Unlock(SteamAchievements.Clear);      // 2.1.1~: 한 판 클리어는 4장 거울의 군주
                if (!BossUltimate.UsedThisFight(AbyssStage.MirrorKind)) SteamAchievements.Unlock(SteamAchievements.MirrorQuick);
            }
        }
        if (bossFight && !bossHit) SteamAchievements.Unlock(SteamAchievements.NoHitBoss);
        if (player != null && player.PlayerHealth > 0f && player.PlayerHealth <= player.PlayerMaxHealth * 0.1f)
            SteamAchievements.Unlock(SteamAchievements.CloseCall);
        bossFight = false;
    }

    void OnEnemyKilled(Vector3 at)
    {
        runKills++;
        if (runKills >= 500) SteamAchievements.Unlock(SteamAchievements.Kills500);
        if (GameInput.Auto) return;
        int total = Prefs.GetInt(TotalKillsKey, 0) + 1;
        Prefs.SetInt(TotalKillsKey, total);
        if (total >= TotalKillsGoal) SteamAchievements.Unlock(SteamAchievements.Kills3000Total);
    }

    // 보스가 나온 순간부터 쓰러질 때까지 체력이 줄었는지 매 프레임 확인
    void TrackBossFight()
    {
        if (player == null || spawner == null) return;
        bool active = spawner.bossSpawned && !spawner.bossCleared;
        if (active && !bossFight) { bossFight = true; bossHit = false; lastHealth = player.PlayerHealth; }
        if (!bossFight) return;
        if (player.PlayerHealth < lastHealth) bossHit = true;
        lastHealth = player.PlayerHealth;
    }

    float nextPoll;

    void Update()
    {
#if STEAM
        if (Initialized) SteamAPI.RunCallbacks();
#endif
        TrackBossFight();
        if (Time.unscaledTime < nextPoll) return;
        nextPoll = Time.unscaledTime + 0.5f;
        if (player != null && player.level >= 10) SteamAchievements.Unlock(SteamAchievements.Level10);
        if (player != null && player.level >= 15) SteamAchievements.Unlock(SteamAchievements.Level15);
        SpecialAbilities special = player != null ? player.special : null;
        if (special != null)
        {
            // 1.8.2에 액티브 스킬이 빠져서 FullSkills는 운명 가지 3칸, Arsenal은 무기 2차 진화로 바꿈 (API 이름은 그대로)
            int[] branch = special.OwnedPerBranch();
            int learned = 0;
            foreach (int c in branch) learned += c;
            if (branch[2] >= 3) SteamAchievements.Unlock(SteamAchievements.FullSkills);
            if (special.EvolutionTier >= 1) SteamAchievements.Unlock(SteamAchievements.WeaponEvolve);
            if (special.EvolutionTier >= 2) SteamAchievements.Unlock(SteamAchievements.Arsenal);
            if (learned >= 20) SteamAchievements.Unlock(SteamAchievements.Tree20);
            if (learned >= 40) SteamAchievements.Unlock(SteamAchievements.Tree40);
        }
        if (GameMode.IsEndless)
        {
            float s = GameMode.EndlessSeconds;
            if (s >= 600f) SteamAchievements.Unlock(SteamAchievements.Endless10);
            if (s >= 1200f) SteamAchievements.Unlock(SteamAchievements.Endless20);
            if (EndlessMode.Instance != null && EndlessMode.Instance.BossesDefeated >= 5) SteamAchievements.Unlock(SteamAchievements.EndlessBosses);
            return;
        }
        // 그 맵에 들어섰을 때 (2.1.1~ 장 순서가 바뀌어 번호 크기로 비교하지 않음)
        if (spawner != null && spawner.stageIndex == 1) SteamAchievements.Unlock(SteamAchievements.EnterHell);
        if (spawner != null && spawner.stageIndex == 2) SteamAchievements.Unlock(SteamAchievements.EnterMeadow);
        if (spawner != null && spawner.stageIndex == AbyssStage.Index) SteamAchievements.Unlock(SteamAchievements.EnterAbyss);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EnermyController.Killed -= OnEnemyKilled;
#if STEAM
        if (Instance == this && Initialized) SteamAPI.Shutdown();
#endif
        if (Instance == this) { Instance = null; Initialized = false; }
    }
}

// 업적 API 이름 (Steamworks 파트너 사이트에 같은 이름으로 등록해야 함 · docs/steam/achievements.md)
public static class SteamAchievements
{
    public const string FirstUlt = "ACH_FIRST_ULT";
    public const string EnterHell = "ACH_ENTER_HELL";
    public const string EnterMeadow = "ACH_ENTER_MEADOW";
    public const string MidBoss = "ACH_MIDBOSS";
    public const string Evolve = "ACH_EVOLVE";
    public const string Level10 = "ACH_LEVEL_10";
    public const string Kills500 = "ACH_KILLS_500";
    public const string BossLich = "ACH_BOSS_LICH";
    public const string BossDemon = "ACH_BOSS_DEMON";
    public const string Clear = "ACH_CLEAR";
    public const string FirstDeath = "ACH_FIRST_DEATH";
    public const string Shopper = "ACH_SHOPPER";
    public const string FullSkills = "ACH_FULL_SKILLS";
    public const string Arsenal = "ACH_ARSENAL";
    public const string Evolve3 = "ACH_EVOLVE_3";
    public const string Ult30 = "ACH_ULT_30";
    public const string Level15 = "ACH_LEVEL_15";
    public const string Kills3000Total = "ACH_KILLS_3000_TOTAL";
    public const string NoHitBoss = "ACH_NO_HIT_BOSS";
    public const string CloseCall = "ACH_CLOSE_CALL";
    public const string UnlockDifficulty = "ACH_UNLOCK_DIFFICULTY";
    public const string ClearNormal = "ACH_CLEAR_NORMAL";
    public const string ClearHard = "ACH_CLEAR_HARD";
    public const string Endless10 = "ACH_ENDLESS_10";
    public const string Endless20 = "ACH_ENDLESS_20";
    public const string EndlessBosses = "ACH_ENDLESS_BOSSES";
    public const string WeaponEvolve = "ACH_WEAPON_EVOLVE";
    public const string Tree20 = "ACH_TREE_20";
    public const string Tree40 = "ACH_TREE_40";
    public const string Reroll = "ACH_REROLL";
    public const string Skin = "ACH_SKIN";
    public const string SkinLegend = "ACH_SKIN_LEGEND";
    // 2.1~: 스킬 진화 · 보스 필살기 결계
    public const string SkillEvolve = "ACH_SKILL_EVOLVE";
    public const string SkillEvolve3 = "ACH_SKILL_EVOLVE_3";
    public const string SkillEvolve15 = "ACH_SKILL_EVOLVE_15";
    public const string BarrierSurvive = "ACH_BARRIER_SURVIVE";
    public const string BarrierNoHit = "ACH_BARRIER_NOHIT";
    public const string BarrierBreak = "ACH_BARRIER_BREAK";
    // 2.1.1~: 4장 영혼의 심연
    public const string EnterAbyss = "ACH_ENTER_ABYSS";
    public const string MirrorQuick = "ACH_MIRROR_QUICK";

    static readonly System.Collections.Generic.HashSet<string> done = new System.Collections.Generic.HashSet<string>();

    public static void Unlock(string id)
    {
        if (GameInput.Auto || TutorialRun.Active) return;      // 트레일러 자동 조종 · 튜토리얼 중에는 풀지 않음
        if (!done.Add(id)) return;       // 한 번 실행 중에 같은 업적은 한 번만
        Debug.Log("[Steam] 업적: " + id);
#if STEAM
        if (!SteamManager.Initialized) return;
        if (SteamUserStats.GetAchievement(id, out bool already) && already) return;
        SteamUserStats.SetAchievement(id);
        SteamUserStats.StoreStats();
#endif
    }
}

// 스팀 순위표 (2.1.1~): 무한 모드 최고 생존 시간 · 일일 도전(그날마다 따로)
// 순위표는 처음 쓸 때 FindOrCreateLeaderboard 로 만들어짐 (파트너 사이트에 미리 만들지 않아도 됨)
// 스팀이 없으면(GitHub 빌드 · 체험판 · 스팀 꺼짐) 아무것도 안 하고 콜백도 부르지 않음
public static class SteamLeaderboards
{
    public const string Endless = "ENDLESS_BEST";
    public static string Daily(string day) => "DAILY_" + day;

    public struct Entry
    {
        public int rank, score, character;
        public ulong user;
        public bool me;
    }

    public static bool Available => SteamManager.Initialized && !Demo.On && !GameInput.Auto && !Application.isBatchMode;

    // 이름 (스팀이 아직 모르는 사람은 정보를 요청해 두고 빈 문자열 · 화면이 다시 물어봄)
    public static string NameOf(ulong user)
    {
#if STEAM
        if (!SteamManager.Initialized) return "";
        CSteamID id = new CSteamID(user);
        if (SteamFriends.RequestUserInformation(id, true)) return "";
        return SteamFriends.GetFriendPersonaName(id);
#else
        return "";
#endif
    }

    // 점수 올리기 (더 좋을 때만 바뀜) → 지금 순위 · 전체 인원
    public static void Upload(string board, int score, int[] details, System.Action<int, int> done)
    {
#if STEAM
        if (!Available) return;
        Find(board, h =>
        {
            CallResult<LeaderboardScoreUploaded_t> cr = null;
            cr = CallResult<LeaderboardScoreUploaded_t>.Create((r, io) =>
            {
                alive.Remove(cr);
                if (io || r.m_bSuccess == 0) return;
                done?.Invoke(r.m_nGlobalRankNew, SteamUserStats.GetLeaderboardEntryCount(h));
            });
            alive.Add(cr);
            cr.Set(SteamUserStats.UploadLeaderboardScore(h, ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, details, details != null ? details.Length : 0));
        });
#endif
    }

    // 위에서 count 명 + 내 기록(순위 밖이면 맨 아래에 붙임) → 목록 · 전체 인원
    public static void Top(string board, int count, System.Action<List<Entry>, int> done)
    {
#if STEAM
        if (!Available) return;
        Find(board, h => Download(h, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 1, count, top =>
            Download(h, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, 0, 0, mine =>
            {
                if (mine.Count > 0 && !top.Exists(e => e.me)) top.Add(mine[0]);
                done?.Invoke(top, SteamUserStats.GetLeaderboardEntryCount(h));
            })));
#endif
    }

#if STEAM
    static readonly Dictionary<string, SteamLeaderboard_t> boards = new Dictionary<string, SteamLeaderboard_t>();
    static readonly List<object> alive = new List<object>();     // 콜백이 올 때까지 CallResult 를 붙잡아 둠

    static void Find(string board, System.Action<SteamLeaderboard_t> then)
    {
        if (boards.TryGetValue(board, out SteamLeaderboard_t known)) { then(known); return; }
        CallResult<LeaderboardFindResult_t> cr = null;
        cr = CallResult<LeaderboardFindResult_t>.Create((r, io) =>
        {
            alive.Remove(cr);
            if (io || r.m_bLeaderboardFound == 0) return;
            boards[board] = r.m_hSteamLeaderboard;
            then(r.m_hSteamLeaderboard);
        });
        alive.Add(cr);
        cr.Set(SteamUserStats.FindOrCreateLeaderboard(board, ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeSeconds));
    }

    static void Download(SteamLeaderboard_t h, ELeaderboardDataRequest req, int from, int to, System.Action<List<Entry>> then)
    {
        CallResult<LeaderboardScoresDownloaded_t> cr = null;
        cr = CallResult<LeaderboardScoresDownloaded_t>.Create((r, io) =>
        {
            alive.Remove(cr);
            List<Entry> list = new List<Entry>();
            if (!io)
            {
                ulong me = SteamUser.GetSteamID().m_SteamID;
                int[] details = new int[4];
                for (int i = 0; i < r.m_cEntryCount; i++)
                {
                    if (!SteamUserStats.GetDownloadedLeaderboardEntry(r.m_hSteamLeaderboardEntries, i, out LeaderboardEntry_t e, details, details.Length)) continue;
                    list.Add(new Entry
                    {
                        rank = e.m_nGlobalRank,
                        score = e.m_nScore,
                        character = e.m_cDetails > 0 ? details[0] : -1,
                        user = e.m_steamIDUser.m_SteamID,
                        me = e.m_steamIDUser.m_SteamID == me,
                    });
                }
            }
            then(list);
        });
        alive.Add(cr);
        cr.Set(SteamUserStats.DownloadLeaderboardEntries(h, req, from, to));
    }
#endif
}
