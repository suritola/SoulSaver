using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

// 무한 모드 (1.2~ 보통 또는 어려움을 클리어하면 열림): 불타는 사막 맵에서 1 · 2 · 3장의 적이 모두 나오고
// 리치 왕 → 지옥의 군주 → 킹 슬라임이 번갈아 주기적으로 등장. 죽을 때까지 버팀
public class EndlessMode : MonoBehaviour
{
    public static EndlessMode Instance { get; private set; }

    // 마지막 판 기록 (게임 오버 화면에서 보여 줌)
    public static float LastSeconds = -1f;
    public static int LastBosses;
    const string BestKey = "endless.bestSeconds";
    public static float BestSeconds => Prefs.GetFloat(BestKey, 0f);

    const float FirstBossAt = 240f;         // 시작 후 첫 보스 (1.1.7~ 1:30 → 4:00)
    const float BossEvery = 150f;           // 보스를 쓰러뜨린 뒤 다음 보스까지 (보스를 잡을 때마다 15초씩 짧아짐, 최소 75초)
    const float BossEveryMin = 75f;
    const float MidBossFrom = 360f;         // 6분부터 중간 보스가 주기적으로 (후반이 지루하지 않게)
    const float MidBossEvery = 70f;         // 강도가 오를수록 짧아짐 (최소 30초)
    public const float MidBossPhaseFrom = 90f;  // 1.1.7~ 페이즈가 오를 때 나오는 중간 보스는 1:30 뒤에만 (EnemySpawn.UpdatePhase)
    float nextMidBoss;

    // 1.2.3~ 「보스는 기다리지 않는다」: 보스가 BossOverlapAfter 넘게 살아 있으면 다음 차례 보스가 함께 나옴 (15분 전 · 한계 돌파 · 동시에 MaxLiveBosses 마리 · 종류는 다름)
    //   모래 폭풍 풍화: ErodeAfter 넘게 살아 있는 보스는 받는 피해가 1분마다 ErodePerMin 씩 늘어남 (최대 ErodeMax 배 · bosss.erosionMul · 개발자 · 엄마 제외)
    //   최후의 시간(15:00)은 보스를 FinalMinBosses 마리 이상 쓰러뜨렸을 때만 — 아니면 모래 폭풍(GameMode.Surge 는 시간대로)만 거세지고 보스 순환은 계속 (storm)
    const int MaxLiveBosses = 2;
    const float BossOverlapAfter = 180f;
    const float ErodeAfter = 180f, ErodePerMin = 0.25f, ErodeMax = 2.5f;
    public const int FinalMinBosses = 1;
    bool storm;             // 15:00 을 보스 처치 0 으로 넘김 — 이 판에서는 최후의 시간이 열리지 않음 (저장 · 이어하기)
    int erodeSec = -1;
    // 1.2.3~ 규칙 E: 개발자 · 엄마와 싸우는 동안(등장 연출 제외) 약한 잡몹을 — EnemySpawner.SpawnEnemy 가 봄
    //   1초마다 4마리 · 동시 최대 30 · 기본 체력 22 이하 5종 · 처치 포인트 ×0.25 (경험치 · 처치 수는 그대로)
    public const float FinalMobEvery = 1f;
    public const int FinalMobBatch = 4;
    public const int FinalMobMax = 30;
    public const float FinalMobPointMul = 0.25f;
    const int FinalMobMaxBaseHp = 22;

    // 최종 국면 (1.0.2 · 1.0.4~ 30분 → 15분): 15분을 버티면 적이 기하급수로 몰려오고(GameMode.Surge) 모든 보스가 한꺼번에 깨어남
    // → 그 보스들을 모두 쓰러뜨리면 최종 보스 「개발자」(1.0.4~ 체력 20만 → 30만 · 공격 주기 ÷10)
    //   1.0.4~ 개발자가 나오기 시작한 뒤로는 잡몹 · 습격 · 중간 보스 · 보스 부하가 나오지 않음 (1.2.3~ 약한 잡몹만 1초마다 4마리 · 규칙 E · EnemySpawner.SpawnEnemy)
    // → 1.0.4~ 30분이 되면 「개발자 엄마」(개발자의 모든 능력치 2배: 체력 60만 · 공격 주기 ÷20 · 피해 · 이동 속도 2배)
    //   개발자를 30분 전에 못 잡았으면 둘이 함께 → 개발자와 엄마를 모두 쓰러뜨리면 무한 모드 클리어
    public const float FinalAt = 900f;
    // 1.1.2~ 엄마는 30분이 아니라 「개발자를 쓰러뜨린 뒤 잡몹 n마리」를 잡으면 강림 (HUD 「그녀의 강림까지 남은 영혼 수 n/1000」)
    //   1.2.5~ 3000 → 1000 (3000마리를 모으는 동안 잡몹이 너무 많아 렉) — 3000 기준 저장은 이미 잡은 수를 그대로 빼서 남은 수가 0 ~ 1000 (다 찼으면 이어하자마자 강림)
    public const int MomSouls = 1000;
    public const int DeveloperHp = 100000;      // 1.2.5~ 20만 → 10만 (1.1.2~ 10만 → 20만 · 1.0.7~ 30만 → 10만 · 엄마 60만 → 30만)
    public const int MomHp = 200000;            // 1.2.5~ 40만 → 20만 (1.1.2~ 30만 → 40만)
    const string ClearKey = "endless.cleared";
    public static bool EverCleared => Prefs.GetInt(ClearKey, 0) == 1;
    int finalStage;         // 0 평소 · 1 모든 보스 · 2 개발자 (· 엄마) · 3 클리어 · 4 클리어 뒤 한계 돌파 (1.1.4~ 엔딩 없이 죽을 때까지)
    bosss developer, mom;
    bool devSummoned, momSummoned, devDown, momDown;     // 1.0.4~
    int momKillBase = -1;       // 1.1.2~ 개발자를 쓰러뜨린 순간의 처치 수 (RunStats.Kills) — 여기서 MomSouls 만큼 더 잡으면 엄마
    public int MomSoulsLeft => momKillBase < 0 ? MomSouls : Mathf.Clamp(MomSouls - (RunStats.Kills - momKillBase), 0, MomSouls);
    public bool FinalPhase => finalStage > 0;
    // 최종 보스(개발자 · 엄마)를 부르기 시작했지만 아직 나오지 않음 — 등장 연출 동안에도 보스 곡을 유지 (MusicManager)
    public bool FinalBossIncoming => finalStage < 3 && ((devSummoned && developer == null && !devDown) || (momSummoned && mom == null && !momDown));
    // 1.1.2~ 최종 보스 전용 곡 (MusicManager): 0 없음 · 1 개발자 곡 · 2 엄마 곡
    // 등장 배너 1초 전에 켜져 「흐르던 곡이 끊김 → 무음 → 첫 타격」이 배너와 맞물림. 살아 있는 쪽을 따르고 둘이 함께면 엄마 곡.
    // 둘 다 쓰러지면(클리어 연출) 마지막 곡을 엔딩이 시작될 때까지 그대로 (엔딩은 MusicManager 가 메뉴 곡으로)
    int finalMusic;
    public int FinalBossMusic
    {
        get
        {
            if (finalMusic == 0) return 0;
            if (finalStage >= 3) return finalMusic;
            // 처치 처리(devDown · momDown)로만 판단 — IsDead 를 보면 쓰러지는 연출 1초(boss.Death) 동안 0 이 되어
            // 보스 곡으로 튀었다가 클리어 순간 엄마 곡이 처음부터 다시 나왔음
            bool momLive = momSummoned && !momDown;
            bool devLive = devSummoned && !devDown;
            if (finalMusic == 2 && momLive) return 2;
            if (devLive) return 1;
            return momLive ? 1 : 0;      // 엄마가 불렸지만 아직 신호 전(함께 등장) — 개발자 곡 유지
        }
    }
    public bool Cleared => finalStage == 3;          // 1.1~ 클리어 → 엔딩 대기 중 (초월 카드 창이 엔딩 위에 뜨지 않게)
    // 1.0.4~ 개발자를 부르기 시작한 뒤로는 습격 · 중간 보스 · 보스 부하가 나오지 않음 (EnemySpawner 가 물어봄 · 1.0.7~ 일반 잡몹은 다시 나옴 · 1.2.3~ 개발자 · 엄마전 잡몹은 규칙 E 뿐)
    public bool NoMobs => devSummoned && finalStage != 4;
    // 1.1.4~ 클리어 뒤 「한계 돌파」: 잡몹 · 습격 · 중간 보스 · 보스가 다시 나오고 시간이 갈수록 계속 강해짐 (기록은 죽을 때까지 늘어남)
    public bool Beyond => finalStage == 4;
    // 1.1.2~ 개발자 · 엄마가 살아 있거나 내려오는 중이면 보스 말고는 어떤 적도 나오지 않음 (잡몹 포함 · 개발자를 잡은 뒤 엄마가 오기 전에만 잡몹 · 1.2.5~ 3배 부스트 없이 상한 AfterDevMaxAlive)
    public bool FinalBossAlive => finalStage < 3 && ((developer != null && !developer.IsDead) || (mom != null && !mom.IsDead) || FinalBossIncoming);
    // 1.2.3~ 규칙 E: 개발자 · 엄마가 실제로 서서 싸우는 중 (등장 연출 FinalBossIncoming 은 빼고 — 그 사이 ClearMobs 가 흩는 중) → 약한 잡몹만 조금씩
    public bool FinalBossFighting => finalStage < 3 && ((developer != null && !developer.IsDead) || (mom != null && !mom.IsDead));
    // 1.1.2~ 개발자를 쓰러뜨린 뒤 엄마가 오기 전까지 잡몹이 개발자 등장 전보다 3배 빨리 나옴 (영혼을 모으는 국면 · GameMode.SpawnIntervalMul 이 나눔)
    //   1.2.5~ 3배 → 1배 (Surge ×16 · 한 화면 +90 에 3배까지 겹쳐 120마리 넘게 떠 렉) — 이 국면은 아래 고정 간격 · 상한으로
    public const float AfterDevSpawnMul = 1f;
    // 1.2.5~ 영혼을 모으는 국면 (개발자 처치 뒤 ~ 엄마 강림 전): 한 화면 최대 AfterDevMaxAlive · 생성은 Surge · 강도와 상관없이 AfterDevSpawnEvery 초마다 한 마리
    //   최대 초당 약 6.7마리 → 1000마리를 쉬지 않고 잡으면 약 2분 30초 (실제로는 2분 30초 ~ 3분)
    public const int AfterDevMaxAlive = 55;
    public const float AfterDevSpawnEvery = 0.15f;
    public static bool GatheringSouls => Instance != null && Instance.devDown && !Instance.momSummoned && Instance.finalStage < 3;
    public static float MobSpawnBoost => GatheringSouls ? AfterDevSpawnMul : 1f;
    public static readonly Color MomTint = new Color(1f, 0.6f, 0.78f);      // 1.0.4~ 개발자 엄마: 개발자 그림을 분홍빛으로 · 더 크게

    public int BossesDefeated { get; private set; }

    StageManager stages;
    EnemySpawner spawner;
    GameObject[] bossPrefabs;
    float nextBoss;
    int bossTurn;
    TextMeshProUGUI hud;
    int shownSec = -1, shownBosses = -1, shownSouls = -1;

    bool daily;      // 일일 도전 판 (메인 메뉴로 나갈 때 DailyChallenge 가 먼저 풀려도 기록이 섞이지 않게 시작할 때 기억)

    public void Init(StageManager sm, GameObject[] bosses, bool resumed = false)
    {
        Instance = this;
        daily = DailyChallenge.Active;
        stages = sm;
        spawner = sm.spawner;
        bossPrefabs = bosses;
        nextBoss = Time.time + FirstBossAt;
        nextMidBoss = Time.time + MidBossFrom;
        BuildHud();
        // 1.2.3~ 지형 변화 「왕들의 영토」 시드 (일일 도전은 그날 시드 · 이어하기는 RestoreRun 이 저장한 시드로 Restore)
        if (!resumed && EndlessTerrain.Instance != null) EndlessTerrain.Instance.Begin(daily ? DailyChallenge.RunSeed : EndlessTerrain.NewSeed());
        if (!resumed) StartCoroutine(StartAtLevel(10));     // 1.2.3~ 이어하기는 LevelShop.RestoreLevels 가 레벨 · 대기 레벨업을 이미 되살렸음 (다시 주면 두 번 쌓임)
    }

    // 무한 모드는 레벨 10으로 시작: 올라간 레벨만큼 레벨업 포인트가 쌓임
    System.Collections.IEnumerator StartAtLevel(int level)
    {
        PlayerController p = FindFirstObjectByType<PlayerController>();
        LevelShop shop = FindFirstObjectByType<LevelShop>();
        if (p == null || shop == null) yield break;
        while (shop.IsOpen || Time.timeScale == 0f) yield return null;
        int add = Mathf.Max(0, level - p.level);
        Juice.LevelUp(p.transform.position);
        shop.GrantLevels(p, add);           // 올라갈 레벨만큼 레벨업 포인트로 쌓아 두고 [Space]로 하나씩 고름 (고를 때 레벨이 오름)
        startGiven = true;
    }

    bool startGiven;    // 1.2.3~ 레벨 10 시작 보너스를 줬음 (카드 · ESC 창이 열린 채 기다리는 동안 저장하고 나가면 이어할 때 줌)

    void Update()
    {
        if (spawner == null) return;
        float s = GameMode.EndlessSeconds;
        // 초나 보스 수가 바뀔 때만 글자를 새로
        int sec = Mathf.FloorToInt(s);
        int souls = MomSoulsLeft;
        if (hud != null && (sec != shownSec || BossesDefeated != shownBosses || souls != shownSouls))
        {
            shownSec = sec;
            shownBosses = BossesDefeated;
            shownSouls = souls;
            float best = daily ? DailyChallenge.RunBest : BestSeconds;
            // 1.1.2~ 개발자를 쓰러뜨렸으면 엄마가 강림하기까지 남은 영혼(잡몹 처치) 수
            string final = finalStage == 4 ? Loc.T("클리어 뒤 · 한계 돌파") : devDown && !momSummoned ? Loc.T("그녀의 강림까지 남은 영혼 수") + " " + MomSoulsLeft + "/" + MomSouls : Loc.T(finalStage >= 2 ? "최종 보스" : "최후의 시간");
            hud.text = (daily ? Loc.T("일일 도전") + " · " + Loc.T(DailyChallenge.RunRuleName) : Loc.T("무한 모드")) + "  " + Clock(s) + "   " + Loc.T("보스 처치 ") + BossesDefeated
                     + (finalStage > 0 ? "   <color=#FF5A4A>" + final + "</color>"
                        : storm ? "   <color=#FF5A4A>" + Loc.T("모래 폭풍") + "</color>"         // 1.2.3~ 15:00 을 보스 처치 0 으로 넘긴 판
                        : best > 0f ? "   " + Loc.T("최고 ") + Clock(best) : "");
        }
        if (sec != erodeSec) { erodeSec = sec; UpdateErosion(); }      // 1.2.3~ 풍화는 1초에 한 번 (HUD 글이 안 바뀌어도)

        if (s >= 600f) CharacterData.Achieve(CharUnlock.Endless10);        // 새 캐릭터 해제 조건 (1.0.2)
        if (!daily) EndlessMilestones.CheckTime(s);                         // 1.2~ 무한 이정표 5 · 10 · 15 · 20분 (첫 달성 한 번 · 일일 도전 제외)
        bool free = Time.timeScale > 0f && !stages.IsMenuOpen && !PlayerController.Cinematic;
        if (finalStage == 0 && !storm && s >= FinalAt && free)
        {
            // 1.2.3~ 15:00 판정 한 번: 보스를 쓰러뜨려 왔으면 지금처럼 모든 보스 → 개발자 → 엄마, 처치 0 이면 모래 폭풍만 (보스 순환은 계속)
            if (BossesDefeated >= FinalMinBosses) StartFinal();
            else StartStorm();
            return;
        }
        // 1.1.2~ 개발자를 쓰러뜨린 뒤 잡몹 MomSouls 마리: 개발자 엄마
        if (devDown && !momSummoned && momKillBase < 0 && finalStage < 3) momKillBase = RunStats.Kills;      // 1.2.5~ 처치 기준이 빠진 저장 · 이어하기에서도 엄마가 끝내 안 오지 않게 지금부터 셈
        if (finalStage >= 2 && finalStage < 3 && devDown && !momSummoned && momKillBase >= 0 && souls <= 0 && free) { StartCoroutine(SummonMom()); return; }
        // 1.2.3~ 보스가 살아 있어도 nextBoss(등장 3분 뒤)가 되면 다음 보스가 함께 (동시 MaxLiveBosses · 쓰러지는 연출 중인 보스가 있으면 처치 처리가 nextBoss 를 다시 정할 때까지 기다림)
        if ((finalStage == 0 || finalStage == 4) && Time.time >= nextBoss && Time.timeScale > 0f && !stages.IsMenuOpen && bosss.Dying == 0 && bosss.LiveCount < MaxLiveBosses) SpawnBoss();
        if (!NoMobs && Time.time >= nextMidBoss && Time.timeScale > 0f && !stages.IsMenuOpen)
        {
            spawner.SpawnMidBoss();
            nextMidBoss = Time.time + Mathf.Max(30f, MidBossEvery / Mathf.Max(1f, GameMode.EndlessIntensity * 0.6f));
        }
    }

    const float SecondBossHpMul = 0.5f;     // 1.1.7~ 첫 바퀴 두 번째 보스(지옥의 군주 · 기본 체력이 리치 왕의 5배)만: 이야기 2장 보스 수준으로

    void SpawnBoss()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0) return;
        // 1.2.3~ 살아 있는 종류는 건너뜀 (결계 · 체력바 · 킹 슬라임 목록이 종류마다 하나) — 상한 2 · 종류 4 라 보통은 걸리지 않음
        int skipped = 0;
        while (skipped < bossPrefabs.Length && bossPrefabs[bossTurn % bossPrefabs.Length] != null
               && bossPrefabs[bossTurn % bossPrefabs.Length].TryGetComponent(out bosss k) && bosss.KindAlive(k.bossKind)) { bossTurn++; skipped++; }
        if (skipped >= bossPrefabs.Length) { nextBoss = Time.time + 30f; return; }
        GameObject prefab = bossPrefabs[bossTurn % bossPrefabs.Length];
        if (prefab == null) { bossTurn++; nextBoss = Time.time + 30f; return; }
        bossTurn++;
        nextBoss = Time.time + BossOverlapAfter;          // 1.2.3~ 쓰러뜨리면 OnBossDefeated 가 다시 정함 · 못 쓰러뜨리면 이 시각에 다음 보스가 함께 나옴 (예전 float.MaxValue)
        bool overlap = bosss.LiveCount > 0;               // 이미 살아 있는 보스가 있음 → 겹쳐 등장
        spawner.bossSpawned = true;
        spawner.bossCleared = false;
        if (stages.bossBar != null) stages.bossBar.bossSpawn = true;
        Vector3 at = (Vector3)spawner.bossSpawnPoint + (overlap ? Vector3.right * 4.5f : Vector3.zero);     // StartFinal 의 두 번째 자리와 같은 간격 (둘이 한 자리에 겹쳐 서지 않게)
        GameObject go = Instantiate(prefab, at, Quaternion.identity);
        if (go.TryGetComponent(out bosss b))
        {
            b.spawnTimed = true;
            b.spawnedAt = Time.time;                      // 1.2.3~ 겹침 · 풍화 기준 (Start 전)
            if (finalStage == 0 && bossTurn == 2) b.setEnemyHP = Mathf.RoundToInt(b.setEnemyHP * SecondBossHpMul);   // bossTurn 은 위에서 이미 ++ 됨 · Start 가 BossHpMul 을 곱하기 전 · finalStage 0: 15분 전에 보스를 하나만 잡고 한계 돌파로 간 판에서 뒤늦게 걸리지 않게
        }
        Hostile.Play("roar", 1f, overlap ? 0.75f : 0.9f);
        if (overlap)
        {
            Notice.Big(Loc.T("다음 왕이 기다리지 않고 나타났다!"), Loc.T("보스는 기다려 주지 않습니다. 한 번에 둘까지 나옵니다."), NoticeIcon.Boss, 3.5f, NoticeLevel.Urgent);
            Hints.Show("endless.bossqueue", "보스는 기다려 주지 않습니다. 보스가 3분 넘게 살아 있으면 다음 보스가 함께 나오고(한 번에 둘까지), 오래 버틴 보스는 모래 폭풍에 풍화되어 받는 피해가 늘어납니다. 15분까지 보스를 하나라도 쓰러뜨려야 「최후의 시간」이 열립니다.");
        }
        else Notice.Big(Loc.T("사막의 모래 폭풍 속에서 보스가 나타났다!"), NoticeIcon.Boss, 2.5f, NoticeLevel.Urgent);
    }

    // 1.2.3~ 1초마다: 살아 있은 시간으로 풍화 배율 (bosss.Live ≤ 4 · 할당 없음) · 시작하는 순간 보스마다 토스트 한 번
    void UpdateErosion()
    {
        foreach (bosss b in bosss.Live)
        {
            if (b == null || b.IsDead) continue;
            if (b.developer || !b.spawnTimed) { b.erosionMul = 1f; continue; }
            float over = Time.time - b.spawnedAt - ErodeAfter;
            float mul = over <= 0f ? 1f : 1f + Mathf.Min(over / 60f * ErodePerMin, ErodeMax - 1f);
            if (mul > 1f && b.erosionMul <= 1f)
                Notice.Toast(NoticeIcon.Warn, Loc.T("{0} — 모래 폭풍에 풍화되어 받는 피해가 1분마다 {1}% 늘어납니다.").Replace("{0}", bossbar.BossName(b.bossKind)).Replace("{1}", Mathf.RoundToInt(ErodePerMin * 100f).ToString()), 4f);
            b.erosionMul = mul;
        }
    }

    // 1.2.3~ 15:00 에 보스 처치 0: 최후의 시간(모든 보스 → 개발자)은 이 판에서 열리지 않고 모래 폭풍(GameMode.Surge · 시간대로)만 거세짐 · 보스 순환(상한 2 · 풍화)은 계속
    // 지형 조각은 그대로 (보스 넷이 오지 않으니 아레나를 비울 이유가 없음 — EndlessTerrain.OnFinalBegin 을 부르지 않음)
    void StartStorm()
    {
        storm = true;
        Hostile.Play("roar", 1f, 0.6f);
        Hostile.Shake(0.4f);
        Notice.Big(Loc.T("15분을 버텼다… 모래 폭풍이 거세진다!"), Loc.T("최후의 시간은 보스를 한 번이라도 쓰러뜨린 자에게만 열립니다. 적은 끝없이 몰려옵니다 — 끝까지 버티세요."), NoticeIcon.Warn, 4f, NoticeLevel.Urgent);
    }

    // 15분 (1.0.4~ 30분 → 15분): 살아 있지 않은 종류의 보스를 모두 한꺼번에 (같은 종류는 하나만 · 결계 · 체력바 · 킹 슬라임 목록이 종류마다 하나라서)
    void StartFinal()
    {
        finalStage = 1;
        if (EndlessTerrain.Instance != null) EndlessTerrain.Instance.OnFinalBegin();      // 1.2.3~ 지형 조각이 모두 꺼짐 (시각이 아니라 최종 국면 시작에 묶음)
        nextBoss = float.MaxValue;
        int made = 0;
        if (bossPrefabs != null)
            foreach (GameObject prefab in bossPrefabs)
            {
                if (prefab == null || (prefab.TryGetComponent(out bosss b) && bosss.KindAlive(b.bossKind))) continue;
                Vector3 at = (Vector3)spawner.bossSpawnPoint + (made == 0 ? Vector3.zero : Quaternion.Euler(0f, 0f, 90f * made) * Vector3.right * 4.5f);
                Instantiate(prefab, Hostile.ClampArena(at), Quaternion.identity);
                made++;
            }
        spawner.bossSpawned = true;
        spawner.bossCleared = false;
        if (stages.bossBar != null) stages.bossBar.bossSpawn = true;
        Hostile.Play("roar", 1f, 0.6f);
        Hostile.Shake(0.4f);
        Notice.Big(Loc.T("15분을 버텼다… 모든 보스가 깨어났다!") + "\n" + Loc.T("적이 끝없이 몰려온다!"), NoticeIcon.Boss, 4f, NoticeLevel.Urgent);
    }

    // 모든 보스를 쓰러뜨리면 최종 보스 「개발자」 (거울의 군주 몸을 빌려 모든 캐릭터의 최종 진화 필살기를 돌려 씀 · BossSkills.Developer)
    // 1.0.4~ 30분에 엄마와 함께 불릴 수도 있음 (SummonMom) · 부르기 시작한 순간부터 잡몹이 나오지 않고, 화면에 남은 잡몹은 영혼으로 흩어짐
    System.Collections.IEnumerator SummonDeveloper()
    {
        if (devSummoned) yield break;
        devSummoned = true;
        bool allDown = bosss.LiveCount == 0;
        if (finalStage < 2) finalStage = 2;
        while (PlayerController.Cinematic || Time.timeScale == 0f) yield return null;
        spawner.ClearMobs();
        yield return new WaitForSeconds(0.5f);
        finalMusic = Mathf.Max(finalMusic, 1);      // 1.1.2~ 배너 1초 전: 보스 곡이 끊기고 무음 → 배너와 함께 개발자 곡 첫 타격 (MusicManager)
        yield return new WaitForSeconds(1.0f);
        Notice.Big((allDown ? Loc.T("모든 보스가 쓰러졌다…") + "\n" : "") + Loc.T("최종 보스 「개발자」가 강림한다!"), NoticeIcon.Boss, 3.5f, NoticeLevel.Urgent);
        Hostile.Play("roar", 1f, 0.45f);
        Hostile.Shake(0.5f);
        yield return new WaitForSeconds(2.5f);
        developer = SpawnFinalBoss(false);
        if (developer == null) devDown = true;      // 견본이 없으면 엄마만 쓰러뜨려도 클리어
    }

    // 1.0.4~ 30분: 「개발자 엄마」 (개발자의 모든 능력치 2배) · 개발자가 아직 나오지 않았으면 둘이 함께
    System.Collections.IEnumerator SummonMom()
    {
        momSummoned = true;
        if (finalStage < 2) finalStage = 2;
        bool together = !devSummoned;
        if (together) StartCoroutine(SummonDeveloper());
        while (PlayerController.Cinematic || Time.timeScale == 0f) yield return null;
        spawner.ClearMobs();
        yield return new WaitForSeconds(together ? 3.5f : 0f);      // 함께면 개발자가 먼저 내려온 뒤
        finalMusic = 2;                                              // 1.1.2~ 배너 1초 전: 엄마 곡 (MusicManager)
        yield return new WaitForSeconds(1f);
        Notice.Big(Loc.T("영혼 1000개가 모였다…") + "\n" + Loc.T("최종 보스 「개발자 엄마」가 나타났다!") + "\n" + Loc.T("「우리 아들 괴롭힌 게 너니?」"), NoticeIcon.Boss, 4f, NoticeLevel.Urgent);
        Hostile.Play("roar", 1f, 0.35f);
        Hostile.Shake(0.7f);
        yield return new WaitForSeconds(2.5f);
        mom = SpawnFinalBoss(true);
        if (mom == null) momDown = true;
    }

    // 개발자 · 개발자 엄마 만들기 (Start 전에 정함: 체력은 난이도 배율 없이 정확히 10만 · 30만 · 1.0.7~)
    bosss SpawnFinalBoss(bool isMom)
    {
        GameObject prefab = null;
        if (bossPrefabs != null)
            foreach (GameObject g in bossPrefabs)
                if (g != null && (prefab == null || (g.TryGetComponent(out bosss b) && b.bossKind == AbyssStage.MirrorKind))) prefab = g;
        if (prefab == null) return null;
        Vector3 at = (Vector3)spawner.bossSpawnPoint + (isMom && developer != null && !developer.IsDead ? Vector3.right * 5f : Vector3.zero);
        GameObject go = Instantiate(prefab, Hostile.ClampArena(at), Quaternion.identity);
        bosss boss = go.GetComponent<bosss>();
        if (boss != null)
        {
            boss.developer = true;
            boss.developerMom = isMom;
            CodexSeen.Mark(isMom ? "sec.mom" : "sec.dev", bossbar.BossName(isMom ? bossbar.DeveloperMomKind : bossbar.DeveloperKind));     // 1.2~ 발견형 도감 비밀
            boss.bossKind = AbyssStage.MirrorKind;
            boss.difficultyApplied = true;
            boss.setEnemyHP = isMom ? MomHp : DeveloperHp;
            boss.expReward *= isMom ? 10 : 5;
            boss.coinDrop *= isMom ? 10 : 5;
            boss.contactDamage *= 0.8f;        // 1.0.6~ 피해 -20% (접촉 48 → 38.4, 엄마 96 → 76.8)
            if (isMom)
            {
                // 엄마: 접촉 피해 · 이동 속도 2배 (공격 주기 · 필살기 피해 2배는 BossSkills.Developer)
                boss.speed *= 2f;
                boss.contactDamage *= 2f;
                boss.baseTint = MomTint;
            }
        }
        // 개발자 전용 도트 (Resources/Abyss/developer_0~3: 금빛 후광 · 후드 · 빛나는 안경 · 코드가 흐르는 노트북) · 엄마는 같은 그림을 분홍빛으로 더 크게
        Sprite[] art = AbyssStage.Frames("developer");
        if (art[0] != null)
        {
            Animator anim = go.GetComponent<Animator>();
            if (anim != null) Destroy(anim);
            if (go.TryGetComponent(out SpriteRenderer sr)) sr.sprite = art[0];
            AbyssBody body = go.GetComponent<AbyssBody>();
            if (body == null) body = go.AddComponent<AbyssBody>();
            body.frames = art;
            body.fps = isMom ? 8f : 6f;
        }
        if (isMom && go.TryGetComponent(out SpriteRenderer tint)) tint.color = MomTint;
        go.transform.localScale *= isMom ? 1.75f : 1.35f;
        Fx.Spawn("fx_soulburst", go.transform.position, isMom ? 18f : 14f, isMom ? MomTint : new Color(1f, 0.85f, 0.35f), 16f);
        spawner.bossSpawned = true;
        spawner.bossCleared = false;
        if (stages.bossBar != null) stages.bossBar.bossSpawn = true;
        return boss;
    }

    // 1.0.4~ 개발자를 30분 전에 쓰러뜨림: 처치 연출 · 보상이 끝난 뒤 다음을 예고
    System.Collections.IEnumerator ForeshadowMom()
    {
        yield return new WaitForSeconds(3.5f);
        while (PlayerController.Cinematic || Time.timeScale == 0f || stages.IsMenuOpen) yield return null;
        if (!momSummoned && finalStage < 3) Notice.Big(Loc.T("개발자를 쓰러뜨렸다!") + "\n" + Loc.T("영혼 1000개를 거두면 그녀가 강림한다…"), NoticeIcon.Star, 4f, NoticeLevel.Normal);
    }

    // 개발자와 개발자 엄마를 모두 쓰러뜨림 (1.0.4~ 개발자만으로는 끝나지 않음): 무한 모드 클리어
    // 1.1.4~ 엔딩으로 넘어가지 않고 5초 뒤 「한계 돌파」(finalStage 4) — 죽을 때까지 계속 (기록 · 해금 · 업적은 여기서 그대로)
    System.Collections.IEnumerator ClearEndless(bool momLast)
    {
        finalStage = 3;
        spawner.spawningEnabled = false;
        Prefs.SetInt(ClearKey, 1);
        CharacterData.Achieve(CharUnlock.EndlessClear);      // 새 캐릭터 해제 조건 (1.0.2)
        Prefs.Save();
        // 최종 클리어 보너스: 1.1.4~ 엔딩(StoryDirector.RunEnding)을 거치지 않으므로 여기서 같은 조건으로 (일일 도전은 없음 · 판마다 한 번)
        // 결과 화면의 획득 포인트에 들어감 (Point.RunBonus 는 한계 돌파 뒤 죽을 때까지 남음)
        int bonusBefore = Point.RunBonus;
        if (!DailyChallenge.Active) Point.Bonus(Point.EndlessFinalBonus);       // 1.2~ 1,500 → 5,000 (처치 포인트 체감과 함께)
        string bonus = Point.RunBonus > bonusBefore ? "\n" + Loc.T("result.finalbonus").Replace("{0}", Point.EndlessFinalBonus.ToString("N0")) : "";
        PlayerController p = FindFirstObjectByType<PlayerController>();
        if (p != null) p.GrantInvincibility(30f);
        Notice.Big(Loc.T(momLast ? "개발자 엄마를 쓰러뜨렸다!" : "개발자를 쓰러뜨렸다!") + "\n" + Loc.T("무한 모드 클리어!") + "  " + Clock(GameMode.EndlessSeconds) + bonus, NoticeIcon.Star, 6f, NoticeLevel.Important);     // 1.0.4~ 해금 알림과 줄 서서 둘 다 보임
        Hostile.Play("chime", 1f, 1f);
        yield return new WaitForSecondsRealtime(5f);
        // 1.1.4~ 한계 돌파: 최종 보스 곡을 끄고(평소 · 보스 곡으로) 적 · 보스를 다시, 초월 카드도 다시 열림 (Cleared 는 finalStage 3 동안만)
        finalStage = 4;
        if (EndlessTerrain.Instance != null) EndlessTerrain.Instance.OnBeyond();          // 1.2.3~ 처치마다 다시 영토가 번짐
        finalMusic = 0;
        spawner.spawningEnabled = true;
        spawner.bossCleared = false;
        nextBoss = Time.time + 20f;
        nextMidBoss = Time.time + 30f;
        Notice.Big(Loc.T("한계 돌파!") + "\n" + Loc.T("이제 쓰러질 때까지 버텨라 — 적은 끝없이 강해진다"), NoticeIcon.Boss, 4f, NoticeLevel.Urgent);
    }

    // 함께 나온 보스 중 마지막이 아닌 보스를 쓰러뜨렸을 때: 처치 연출만 빼고 보상(진화 · 전리품 · 포인트)은 똑같이
    public void OnExtraBossDown()
    {
        SteamManager.OnExtraBossDown();         // 1.2.3~ 보스전 무피격 · 아슬아슬 업적을 보스마다 판정
        OnBossDefeated();
    }

    // StageManager가 보스 처치를 알려 줌
    int trophies;

    public void OnBossDefeated()
    {
        if (finalStage == 3) return;
        BossesDefeated++;
        if (BossesDefeated >= 5) CharacterData.Achieve(CharUnlock.EndlessBosses5);
        if (!daily && BossesDefeated >= 5) EndlessMilestones.Reach(EndlessMilestones.Bosses5);      // 1.2~ 이정표 (첫 달성 한 번)
        // 1.0.4~ 개발자를 쓰러뜨려도 끝나지 않음: 개발자와 엄마를 모두 쓰러뜨려야 클리어 (보스가 쓰러진 그 순간 불리므로 그때 표시)
        bool devWas = devDown, momWas = momDown;
        if (developer != null && developer.IsDead) devDown = true;
        if (mom != null && mom.IsDead) momDown = true;
        if (!daily && devDown && !devWas && developer != null) EndlessMilestones.Reach(EndlessMilestones.Developer);     // 1.2~ 이정표: 개발자 처치 (엄마와 함께 쓰러져 바로 클리어로 가도)
        if (finalStage != 4 && devSummoned && momSummoned && devDown && momDown) { StartCoroutine(ClearEndless(momDown && !momWas)); return; }
        if (finalStage == 1 && bosss.LiveCount == 0) StartCoroutine(SummonDeveloper());
        if (devDown && !devWas && !momSummoned) { momKillBase = RunStats.Kills; StartCoroutine(ForeshadowMom()); }
        spawner.bossCleared = false;            // 다음 보스를 다시 부를 수 있게
        nextBoss = Time.time + Mathf.Max(BossEveryMin, BossEvery - 15f * BossesDefeated);
        // 1.2.3~ 겹쳐 나온 보스가 아직 살아 있으면 그 보스의 「등장 3분 뒤 다음 보스」 시각을 지움 없이 (둘 중 이른 쪽 · 할당 없음)
        foreach (bosss b in bosss.Live)
            if (b != null && !b.IsDead && !b.developer && b.spawnTimed) nextBoss = Mathf.Min(nextBoss, b.spawnedAt + BossOverlapAfter);
        if (EndlessTerrain.Instance != null) EndlessTerrain.Instance.OnBossDefeated(BossesDefeated, finalStage);     // 1.2.3~ 쓰러진 왕의 영토가 번짐 (처치 연출 · 진화 창 뒤)
        if (SpecialAbilities.UsesEvolution)
        {
            // 보스마다 무기 진화: 1차 → 2차 → 각성 (최대 3번)
            // 진화가 다 끝난 뒤에도 보스를 잡을 이유가 있게: 잡을 때마다 계속 쌓이는 전리품
            SpecialAbilities sp = SpecialAbilities.SharedInstance;
            // 진화 창이 이미 열려 있거나 기다리는 중이면 (함께 나온 보스가 거의 같이 쓰러짐) 이번 보스는 전리품으로
            if (sp != null && (sp.NextEvolutionOptions() == null || stages.IsEvolving))
            {
                string loot = sp.BossTrophy(++trophies);
                RunSave.Record("bt" + trophies);        // 1.2.3~ 이어할 때 같은 전리품 효과를 다시 (RunSave.Restore 의 기록 재생)
                Notice.Big(Loc.T("보스 처치!") + "\n" + Loc.T("전리품") + " : " + loot, NoticeIcon.Star, 3f, NoticeLevel.Normal);
                return;
            }
            Notice.Big(Loc.T("보스 처치!"), NoticeIcon.Star, 2.5f, NoticeLevel.Normal);
            RunSave.NeedEvolution = true;               // 1.2.3~ 진화 창이 뜨기 전에 나가면 이어할 때 고르게 (이야기 모드와 같음)
            stages.StartCoroutine(stages.Evolution(true));
            return;
        }
        stages.specialPoints++;
        Notice.Big(Loc.T("보스 처치!  특수 능력 포인트 +1"), NoticeIcon.Star, 3f, NoticeLevel.Normal);
    }

    // 1.2.3~ 판 중간 저장 (RunSave.Save): 무한 전용 상태. 평소 보스가 살아 있으면 같은 보스가 바로 다시 나오게 차례를 1 되돌림 (이야기 모드와 같은 규칙)
    public void SaveRun(RunSave.Data d)
    {
        d.endlessSeconds = GameMode.EndlessSeconds;
        d.endlessBosses = BossesDefeated;
        d.endlessTrophies = trophies;
        d.endlessStartGiven = startGiven;
        d.endlessFinal = finalStage == 3 ? 4 : finalStage;              // 클리어 연출 중이면 이어할 때 바로 한계 돌파 (보너스 · 기록은 ClearEndless 가 이미 적었음)
        d.endlessDevSummoned = devSummoned; d.endlessMomSummoned = momSummoned;
        d.endlessDevDown = devDown;         d.endlessMomDown = momDown;
        d.endlessMomKillBase = momKillBase;
        bool bossLive = (finalStage == 0 || finalStage == 4) && spawner != null && spawner.bossSpawned && !spawner.bossCleared;
        d.endlessBossAlive = bossLive;
        d.endlessStorm = storm;
        // 1.2.3~ 살아 있는 보스는 체력 · 살아 있은 시간까지 그대로 (겹쳐 나온 둘 · 최종 국면 넷) — 다 쓰러지는 연출 중이면 예전처럼 같은 보스를 체력 가득으로 다시
        d.endlessLiveBosses = finalStage == 0 || finalStage == 1 || finalStage == 4 ? LiveBossesText() : "";
        bool exact = d.endlessLiveBosses.Length > 0;
        d.endlessTurn = bossLive && !exact ? Mathf.Max(0, bossTurn - 1) : bossTurn;
        d.endlessNextBoss = (bossLive && !exact) || nextBoss == float.MaxValue ? -1f : Mathf.Max(0f, nextBoss - Time.time);
        d.endlessNextMid = Mathf.Max(0f, nextMidBoss - Time.time);
        if (finalStage == 3) { d.endlessNextBoss = 20f; d.endlessNextMid = 30f; }     // ClearEndless 가 한계 돌파를 시작할 때 주는 값
        int mask = 0;
        if (finalStage == 1 && bossPrefabs != null)
            for (int i = 0; i < bossPrefabs.Length && i < 31; i++)
                if (bossPrefabs[i] != null && bossPrefabs[i].TryGetComponent(out bosss b) && bosss.KindAlive(b.bossKind)) mask |= 1 << i;
        d.endlessFinalAlive = mask;
        if (EndlessTerrain.Instance != null) EndlessTerrain.Instance.Save(out d.terrainSeed, out d.terrain);     // 1.2.3~ 지형 변화
    }

    // 1.2.3~ 살아 있는 보스 (개발자 · 엄마 제외 · 최대 4): "종류,최대 체력,체력,살아 있은 초[,1];…" — 저장할 때만 (StringBuilder 하나 재사용)
    //   끝의 ",1" = 쓰러지는 연출 중(처치 처리 전)이던 보스 → 체력 1 로 다시 (처치 수 · 진화 · 지형 · 15분 판정이 사라지지 않게 · 보상은 Die 에서 이미 받아 다시 주지 않음)
    static readonly System.Text.StringBuilder liveText = new System.Text.StringBuilder(64);
    static string LiveBossesText()
    {
        liveText.Length = 0;
        foreach (bosss b in bosss.Live)
        {
            if (b == null || b.developer) continue;
            bool dying = b.DefeatPending;
            if (b.IsDead && !dying) continue;
            if (liveText.Length > 0) liveText.Append(';');
            liveText.Append(b.bossKind).Append(',').Append(b.setEnemyHP).Append(',').Append(dying ? 1 : Mathf.Max(1, Mathf.CeilToInt(b.EnemyHealth)))
                    .Append(',').Append(Mathf.Max(0, Mathf.RoundToInt(Time.time - b.spawnedAt)));
            if (dying) liveText.Append(",1");
        }
        return liveText.ToString();
    }

    // 1.2.3~ 이어하기: 저장한 보스를 그때의 체력 · 살아 있은 시간으로 (인트로 · 알림 · 울음 없이) — 하나라도 되살렸으면 true
    bool RestoreLive(string text)
    {
        if (string.IsNullOrEmpty(text) || bossPrefabs == null) return false;
        int made = 0;
        foreach (string part in text.Split(';'))
        {
            string[] f = part.Split(',');
            if (f.Length < 4 || !int.TryParse(f[0], out int kind) || !int.TryParse(f[1], out int maxHp) || !int.TryParse(f[2], out int hp) || !int.TryParse(f[3], out int alive)) continue;
            GameObject prefab = null;
            foreach (GameObject g in bossPrefabs)
                if (g != null && g.TryGetComponent(out bosss pb) && pb.bossKind == kind) { prefab = g; break; }
            if (prefab == null || bosss.KindAlive(kind)) continue;
            Vector3 at = (Vector3)spawner.bossSpawnPoint + (made == 0 ? Vector3.zero : Quaternion.Euler(0f, 0f, 90f * (made - 1)) * Vector3.right * 4.5f);
            GameObject go = Instantiate(prefab, Hostile.ClampArena(at), Quaternion.identity);
            if (go.TryGetComponent(out bosss b))
            {
                b.restoreMaxHp = Mathf.Max(1, maxHp);         // Start 가 난이도 배율을 곱한 뒤 덮어씀 (보상 배율은 그대로 들어감)
                b.restoreHp = Mathf.Max(1, hp);
                b.spawnTimed = true;
                b.spawnedAt = Time.time - Mathf.Max(0, alive);  // 겹침 · 풍화 시각이 이어짐 (풍화 배율은 다음 초에 UpdateErosion 이 다시 계산)
                if (f.Length >= 5 && f[4] == "1") { b.restoreDying = true; b.restoreHp = 1f; }     // 쓰러지는 중에 저장됨 → 한 대에 처치 (보상 중복 없음)
            }
            StoryDirector.MarkIntroduced(kind);              // 이어할 때 인트로를 다시 틀지 않음
            made++;
        }
        if (made == 0) return false;
        spawner.bossSpawned = true;
        spawner.bossCleared = false;
        if (stages.bossBar != null) stages.bossBar.bossSpawn = true;
        return true;
    }

    // 1.2.3~ 이어하기 (StageManager.Opening): Init 바로 뒤 · 같은 프레임에 (Update 가 먼저 돌면 복원 전 상태로 StartFinal 이 잘못 걸림)
    public void RestoreRun(RunSave.Data d)
    {
        BossesDefeated = d.endlessBosses;
        bossTurn = Mathf.Max(0, d.endlessTurn);
        trophies = d.endlessTrophies;
        finalStage = d.endlessFinal == 3 ? 4 : Mathf.Clamp(d.endlessFinal, 0, 4);
        devDown = d.endlessDevDown; momDown = d.endlessMomDown;
        momKillBase = devDown ? d.endlessMomKillBase : -1;
        nextBoss = d.endlessNextBoss >= 0f ? Time.time + d.endlessNextBoss : float.MaxValue;
        nextMidBoss = Time.time + Mathf.Max(0f, d.endlessNextMid);
        startGiven = d.endlessStartGiven;
        storm = d.endlessStorm;
        if (!startGiven) StartCoroutine(StartAtLevel(10));     // 시작 보너스를 받기 전에 저장한 판 (p.level 은 Restore 가 이미 되살림 → 모자란 만큼만)
        if (finalStage == 1)
        {
            if (RestoreLive(d.endlessLiveBosses)) nextBoss = float.MaxValue;     // 1.2.3~ 체력 · 살아 있은 시간 그대로
            else RestoreFinal(d.endlessFinalAlive);
        }
        else if (finalStage == 2)
        {
            // 살아 있던(또는 내려오던) 최종 보스는 등장 연출부터 다시 — 깃발을 내려 두면 SummonDeveloper · SummonMom 이 다시 켬 (체력 가득 · 전용 곡도 그 안에서)
            bool devLive = d.endlessDevSummoned && !devDown, momLive = d.endlessMomSummoned && !momDown;
            devSummoned = d.endlessDevSummoned && !devLive;
            momSummoned = d.endlessMomSummoned && !momLive;
            if (momLive) StartCoroutine(SummonMom());            // 개발자도 살아 있었으면 둘이 함께
            else if (devLive || !devSummoned) StartCoroutine(SummonDeveloper());
        }
        else
        {
            devSummoned = d.endlessDevSummoned; momSummoned = d.endlessMomSummoned;
            if (RestoreLive(d.endlessLiveBosses)) { }                // 1.2.3~ 살아 있던 보스를 그때의 체력으로 · 다음 보스 시각은 저장값 그대로 (겹침)
            else if (d.endlessBossAlive) nextBoss = Time.time;       // 다음 Update 에서 SpawnBoss → 같은 보스가 바로 (체력 가득 · 쓰러지는 연출 중에 저장했을 때)
        }
        // 1.2.3~ 지형 변화: 국면 · 플레이어 자리가 되살아난 뒤, 적이 나오기 전 — 기록대로 즉시 (파동 · 알림 없이)
        if (EndlessTerrain.Instance != null) EndlessTerrain.Instance.Restore(d.terrainSeed, d.terrain);
    }

    // 15분 최종 국면에서 살아 있던 종류만 다시 (StartFinal 과 같은 자리 · 같은 깃발 · 비트가 비어 있으면 모두)
    void RestoreFinal(int aliveMask)
    {
        if (aliveMask == 0 || bossPrefabs == null) { finalStage = 0; StartFinal(); return; }
        finalStage = 1;
        nextBoss = float.MaxValue;
        int made = 0;
        for (int i = 0; i < bossPrefabs.Length && i < 31; i++)
        {
            if ((aliveMask & (1 << i)) == 0 || bossPrefabs[i] == null) continue;
            Vector3 at = (Vector3)spawner.bossSpawnPoint + (made == 0 ? Vector3.zero : Quaternion.Euler(0f, 0f, 90f * made) * Vector3.right * 4.5f);
            Instantiate(bossPrefabs[i], Hostile.ClampArena(at), Quaternion.identity);
            made++;
        }
        spawner.bossSpawned = true;
        spawner.bossCleared = false;
        if (stages.bossBar != null) stages.bossBar.bossSpawn = true;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        // 이번 판 기록 (죽어서 게임 오버로 넘어갈 때)
        LastSeconds = GameMode.EndlessSeconds;
        LastBosses = BossesDefeated;
        Instance = null;
        // 일일 도전은 그날 기록 · 순위표에만 (규칙이 달라 무한 모드 최고 기록과 섞지 않음)
        if (!daily && LastSeconds > BestSeconds && !GameInput.Auto)
        {
            Prefs.SetFloat(BestKey, LastSeconds);
            Prefs.Save();
        }
        DailyChallenge.Submit(daily, LastSeconds, LastBosses);
    }

    public static string Clock(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    void BuildHud()
    {
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        GameObject go = new GameObject("EndlessHud", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false);
        // 1.1~: 맨 뒤에 붙어 보스 체력바 · 일시정지 · 레벨업 창 위에 그려졌음 → HUD 순서 안(탄약 패널 바로 뒤)
        Transform ammoPanel = canvas.transform.Find("AmmoPanel");
        if (ammoPanel != null) r.SetSiblingIndex(ammoPanel.GetSiblingIndex() + 1);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(1200f, 50f);
        r.anchoredPosition = new Vector2(0f, -40f);
        hud = go.GetComponent<TextMeshProUGUI>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) hud.font = UIKit.Font;
        if (UIKit.FontMaterial != null) hud.fontSharedMaterial = UIKit.FontMaterial;
        hud.fontSize = 30f;
        hud.color = new Color(1f, 0.78f, 0.45f);
        hud.alignment = TextAlignmentOptions.Center;
        // 1.1~: 긴 일일 도전 규칙 이름이면 두 줄로 꺾여 아래 보스 이름과 겹쳤음 → 한 줄 고정, 넘치면 줄임
        hud.enableWordWrapping = false;
        hud.enableAutoSizing = true;
        hud.fontSizeMax = 30f;
        hud.fontSizeMin = 18f;
        hud.raycastTarget = false;
        CombatStatsHUD.Ensure();            // 1.1.9~ 전투 통계 (무한 모드 글자 왼쪽)
    }

    // ================================================================= 스테이지 구성
    // 1 · 2 · 3 · 4장의 적을 모두 모은 마지막 스테이지 (보스는 EndlessMode가 따로 부름)
    public static StageConfig[] WithEndlessStage(StageConfig[] stages, out GameObject[] bosses)
    {
        List<GameObject> enemies = new List<GameObject>();
        List<float> tiers = new List<float>();
        List<GameObject> bossList = new List<GameObject>();
        for (int s = 0; s < stages.Length && s < 4; s++)
        {
            StageConfig st = stages[s];
            if (st.bossPrefab != null) bossList.Add(st.bossPrefab);
            if (st.enemies == null) continue;
            for (int i = 0; i < st.enemies.Length; i++)
            {
                if (st.enemies[i] == null || enemies.Contains(st.enemies[i])) continue;
                enemies.Add(st.enemies[i]);
                tiers.Add(s + i / (float)Mathf.Max(1, st.enemies.Length));    // 0 ~ 3: 약한 적 → 강한 적
            }
        }
        bosses = bossList.ToArray();

        // 처치 수에 따라 강한 적이 차례로 풀리는 페이즈 9개: 처음엔 1장 첫 적들만, 점점 강한 적 위주로
        // 아직 차례가 안 된 강한 적은 나오지 않고, 이미 풀린 약한 적은 계속 조금씩 섞여 나옴
        int[] kills = { 0, 25, 55, 90, 130, 180, 240, 310, 390 };
        float[] minSec = { 0f, 45f, 90f, 150f, 210f, 285f, 360f, 420f, 480f };   // 1.1.7~ 등급은 이 시각 뒤에만 (처치 수와 둘 다)
        float[] interval = { 1.2f, 1.05f, 0.95f, 0.85f, 0.78f, 0.72f, 0.66f, 0.62f, 0.58f };
        int[] alive = { 8, 10, 12, 14, 16, 18, 19, 20, 22 };
        float topTier = 0f;
        foreach (float t in tiers) topTier = Mathf.Max(topTier, t);
        SpawnPhase[] phases = new SpawnPhase[kills.Length];
        for (int p = 0; p < phases.Length; p++)
        {
            float focus = topTier * p / (phases.Length - 1);    // 이 페이즈에 가장 많이 나오는 등급
            float[] w = new float[enemies.Count];
            for (int i = 0; i < w.Length; i++)
                w[i] = tiers[i] > focus + 0.35f ? 0f : Mathf.Max(0.25f, 1.2f - Mathf.Abs(tiers[i] - focus) * 0.5f);
            phases[p] = new SpawnPhase { killsToEnter = kills[p], minSeconds = minSec[p], spawnInterval = interval[p], maxAlive = alive[p], weights = w };
        }
        float[] minions = new float[enemies.Count];
        for (int i = 0; i < minions.Length; i++) minions[i] = 1f;
        // 1.2.3~ 규칙 E: 개발자 · 엄마전에 나오는 약한 적 (기본 체력 22 이하 — 해골 · 구울 · 임프 · 망령 · 초원 슬라임 · 모두 근접 기본형)
        float[] finals = new float[enemies.Count];
        for (int i = 0; i < finals.Length; i++) finals[i] = enemies[i].TryGetComponent(out EnermyController fe) && fe.setEnemyHP <= FinalMobMaxBaseHp ? 1f : 0f;

        StageConfig endless = new StageConfig
        {
            stageName = "Endless",
            enemies = enemies.ToArray(),
            phases = phases,
            bossKills = int.MaxValue,
            bossPrefab = null,
            minionWeights = minions,
            finalWeights = finals,
        };
        List<StageConfig> list = new List<StageConfig>(stages);
        while (list.Count > 4) list.RemoveAt(list.Count - 1);
        list.Add(endless);
        return list.ToArray();
    }

    // ================================================================= 불타는 사막 맵
    // 지옥 맵 배치를 복제하고 타일 그림만 사막 도트(Resources/Desert)로 바꾼 뒤 소품 · 불티를 뿌림
    public static GameObject BuildDesertMap(GameObject hellMap, Vector2 areaMin, Vector2 areaMax, Vector3 keepClear)
    {
        GameObject map = ReskinHellMap(hellMap, "DesertMap", "Desert", "_desert", out Dictionary<string, Sprite> desert, out int sorting, out int order);
        if (map == null) return null;
        ScatterProps(map.transform, desert, areaMin, areaMax, keepClear, sorting, order + 1);
        map.AddComponent<EndlessTerrain>().Init(map, areaMin, areaMax, desert, sorting, order);     // 1.2.3~ 보스 처치마다 지형 변화
        map.AddComponent<DesertAmbience>();
        return map;
    }

    // 지옥 맵 배치를 복제하고 타일 그림만 Resources/<folder> 의 같은 이름(_hell → suffix) 그림으로 (무한 모드 사막 · 4장 심연)
    // sprites: 그 폴더의 그림 (소품을 뿌릴 때), sorting · order: 가장 위 타일 층 (소품은 그 위에)
    public static GameObject ReskinHellMap(GameObject hellMap, string name, string folder, string suffix, out Dictionary<string, Sprite> sprites, out int sorting, out int order)
    {
        sprites = new Dictionary<string, Sprite>();
        sorting = order = 0;
        if (hellMap == null) return null;
        GameObject map = Instantiate(hellMap, hellMap.transform.parent);
        map.name = name;
        map.SetActive(true);

        Dictionary<string, Sprite> desert = sprites;
        foreach (Sprite s in Resources.LoadAll<Sprite>(folder)) desert[s.name] = s;
        // 최적화: Tile 은 EndlessTerrain 의 정적 캐시를 같이 씀 (판마다 새로 만들지 않음 · 칸 플래그는 아래에서 None 으로 지정 · 이 맵엔 TilemapCollider2D 없음)

        foreach (Tilemap tm in map.GetComponentsInChildren<Tilemap>(true))
        {
            TilemapRenderer tr = tm.GetComponent<TilemapRenderer>();
            if (tr != null && tr.sortingOrder >= order) { order = tr.sortingOrder; sorting = tr.sortingLayerID; }
            BoundsInt b = tm.cellBounds;
            foreach (Vector3Int pos in b.allPositionsWithin)
            {
                Sprite s = tm.GetSprite(pos);
                if (s == null || !desert.TryGetValue(s.name.Replace("_hell", suffix), out Sprite d)) continue;
                Tile tile = EndlessTerrain.TileFor(d);
                Matrix4x4 m = tm.GetTransformMatrix(pos);
                Color c = tm.GetColor(pos);
                tm.SetTile(pos, tile);
                tm.SetTileFlags(pos, TileFlags.None);
                tm.SetTransformMatrix(pos, m);
                tm.SetColor(pos, c);
            }
        }

        return map;
    }

    static void ScatterProps(Transform parent, Dictionary<string, Sprite> desert, Vector2 min, Vector2 max, Vector3 keepClear, int sorting, int order)
    {
        (string name, float weight, float scale)[] kinds =
        {
            ("prop_cactus", 3f, 1.6f), ("prop_cactus_small", 3f, 1.5f), ("prop_rocks", 3f, 1.6f), ("prop_bush", 3f, 1.5f),
            ("prop_skull", 1.5f, 1.4f), ("prop_ribs", 1.2f, 1.6f), ("prop_brazier", 1f, 1.5f), ("prop_flame_pillar", 1f, 1.6f),
        };
        float total = 0f;
        foreach (var k in kinds) total += k.weight;

        Random.State saved = Random.state;
        Random.InitState(7031);                  // 매 판 같은 배치
        GameObject root = new GameObject("DesertProps");
        root.transform.SetParent(parent, false);
        int placed = 0;
        for (int tries = 0; tries < 900 && placed < 95; tries++)
        {
            Vector3 pos = new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), 0f);
            if (Vector2.Distance(pos, keepClear) < 6f || Hostile.WallNear(pos, 1.2f)) continue;
            float roll = Random.Range(0f, total);
            var kind = kinds[kinds.Length - 1];
            foreach (var k in kinds) { roll -= k.weight; if (roll < 0f) { kind = k; break; } }
            if (!desert.TryGetValue(kind.name, out Sprite sprite)) continue;

            GameObject go = new GameObject(kind.name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * kind.scale * Random.Range(0.85f, 1.15f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerID = sorting;
            sr.sortingOrder = order;
            sr.flipX = Random.value < 0.5f;
            if (kind.name == "prop_brazier" || kind.name == "prop_flame_pillar") go.AddComponent<FireFlicker>();
            placed++;
        }
        Random.state = saved;
    }
}

// 화로 · 불기둥: 살랑거리며 불티가 튐
public class FireFlicker : MonoBehaviour
{
    Vector3 baseScale;
    float seed, nextSpark;
    SpriteRenderer sr;

    void Start()
    {
        baseScale = transform.localScale;
        seed = Random.Range(0f, 10f);
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 1.1.9: 화면 밖 화로 · 불기둥은 흔들림 · 불티를 건너뜀 (안 보이므로 겉모습 같음)
        if (sr != null && !sr.isVisible) return;
        float t = Time.time * 7f + seed;
        transform.localScale = new Vector3(baseScale.x * (1f + 0.03f * Mathf.Sin(t * 1.3f)), baseScale.y * (1f + 0.07f * Mathf.Sin(t)), 1f);
        if (Time.time >= nextSpark)
        {
            nextSpark = Time.time + Random.Range(0.25f, 0.6f);
            Vector3 top = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.7f * baseScale.y, 0f);
            FlameParticle.Spawn(Hostile.Glow, top, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(1f, 2f)), Random.Range(0.6f, 1f), 0.06f, 0.02f, false);
        }
    }
}

// 사막 전체: 화면 주변에 불티가 떠오르고 가끔 모래 바람이 스침 (1.2.3~ 영토 위에서는 그 영토의 바람 색 · 불티는 사막 · 지옥만)
public class DesertAmbience : MonoBehaviour
{
    float nextEmber, nextGust;

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null || Time.timeScale == 0f) return;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector3 c = cam.transform.position;
        int realm = EndlessTerrain.Instance != null ? EndlessTerrain.Instance.RealmAt(c) : EndlessTerrain.Desert;

        if (Time.time >= nextEmber && EndlessTerrain.Embers(realm))
        {
            nextEmber = Time.time + 0.06f;
            Vector3 p = new Vector3(c.x + Random.Range(-w, w), c.y - h - 0.5f + Random.Range(0f, h * 0.6f), 0f);
            FlameParticle.Spawn(Hostile.Glow, p, new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(1.2f, 2.6f)), Random.Range(1.4f, 2.4f), 0.035f, 0.012f, false);
        }
        if (Time.time >= nextGust)
        {
            nextGust = Time.time + Random.Range(2.5f, 5f);
            Vector3 from = new Vector3(c.x - w - 2f, c.y + Random.Range(-h, h), 0f);
            Fx.Spawn("fx_smoke", from + new Vector3(Random.Range(0f, w * 2f), 0f, 0f), Random.Range(3f, 5f), EndlessTerrain.Gust(realm), 10f);
        }
    }
}
