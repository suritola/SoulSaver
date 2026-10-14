using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

// 무한 모드 (어려움을 클리어하면 열림): 불타는 사막 맵에서 1 · 2 · 3장의 적이 모두 나오고
// 리치 왕 → 지옥의 군주 → 킹 슬라임이 번갈아 주기적으로 등장. 죽을 때까지 버팀
public class EndlessMode : MonoBehaviour
{
    public static EndlessMode Instance { get; private set; }

    // 마지막 판 기록 (게임 오버 화면에서 보여 줌)
    public static float LastSeconds = -1f;
    public static int LastBosses;
    const string BestKey = "endless.bestSeconds";
    public static float BestSeconds => Prefs.GetFloat(BestKey, 0f);

    const float FirstBossAt = 90f;          // 시작 후 첫 보스
    const float BossEvery = 150f;           // 보스를 쓰러뜨린 뒤 다음 보스까지 (보스를 잡을 때마다 15초씩 짧아짐, 최소 75초)
    const float BossEveryMin = 75f;
    const float MidBossFrom = 360f;         // 6분부터 중간 보스가 주기적으로 (후반이 지루하지 않게)
    const float MidBossEvery = 70f;         // 강도가 오를수록 짧아짐 (최소 30초)
    float nextMidBoss;

    // 최종 국면 (1.0.2 · 1.0.4~ 30분 → 15분): 15분을 버티면 적이 기하급수로 몰려오고(GameMode.Surge) 모든 보스가 한꺼번에 깨어남
    // → 그 보스들을 모두 쓰러뜨리면 최종 보스 「개발자」(1.0.4~ 체력 20만 → 30만 · 공격 주기 ÷10)
    //   1.0.4~ 개발자가 나오기 시작한 뒤로는 잡몹 · 습격 · 중간 보스 · 보스 부하가 나오지 않음
    // → 1.0.4~ 30분이 되면 「개발자 엄마」(개발자의 모든 능력치 2배: 체력 60만 · 공격 주기 ÷20 · 피해 · 이동 속도 2배)
    //   개발자를 30분 전에 못 잡았으면 둘이 함께 → 개발자와 엄마를 모두 쓰러뜨리면 무한 모드 클리어
    public const float FinalAt = 900f;
    public const float MomAt = 1800f;
    public const int DeveloperHp = 150000;      // 1.0.7~ 너무 안 죽어서 절반 (30만 → 15만 · 엄마 60만 → 30만)
    public const int MomHp = 300000;
    const string ClearKey = "endless.cleared";
    public static bool EverCleared => Prefs.GetInt(ClearKey, 0) == 1;
    int finalStage;         // 0 평소 · 1 모든 보스 · 2 개발자 (· 엄마) · 3 클리어
    bosss developer, mom;
    bool devSummoned, momSummoned, devDown, momDown;     // 1.0.4~
    public bool FinalPhase => finalStage > 0;
    // 1.0.4~ 개발자를 부르기 시작한 뒤로는 습격 · 중간 보스 · 보스 부하가 나오지 않음 (EnemySpawner 가 물어봄 · 1.0.7~ 일반 잡몹은 다시 나옴)
    public bool NoMobs => devSummoned;
    public static readonly Color MomTint = new Color(1f, 0.6f, 0.78f);      // 1.0.4~ 개발자 엄마: 개발자 그림을 분홍빛으로 · 더 크게

    public int BossesDefeated { get; private set; }

    StageManager stages;
    EnemySpawner spawner;
    GameObject[] bossPrefabs;
    float nextBoss;
    int bossTurn;
    TextMeshProUGUI hud;
    int shownSec = -1, shownBosses = -1;

    bool daily;      // 일일 도전 판 (메인 메뉴로 나갈 때 DailyChallenge 가 먼저 풀려도 기록이 섞이지 않게 시작할 때 기억)

    public void Init(StageManager sm, GameObject[] bosses)
    {
        Instance = this;
        daily = DailyChallenge.Active;
        stages = sm;
        spawner = sm.spawner;
        bossPrefabs = bosses;
        nextBoss = Time.time + FirstBossAt;
        nextMidBoss = Time.time + MidBossFrom;
        BuildHud();
        StartCoroutine(StartAtLevel(10));
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
    }

    void Update()
    {
        if (spawner == null) return;
        float s = GameMode.EndlessSeconds;
        // 초나 보스 수가 바뀔 때만 글자를 새로
        int sec = Mathf.FloorToInt(s);
        if (hud != null && (sec != shownSec || BossesDefeated != shownBosses))
        {
            shownSec = sec;
            shownBosses = BossesDefeated;
            float best = daily ? DailyChallenge.RunBest : BestSeconds;
            // 1.0.4~ 개발자를 먼저 쓰러뜨렸으면 30분까지 남은 시간
            string final = devDown && !momSummoned ? Loc.T("누군가 오기까지") + " " + Clock(Mathf.Max(0f, MomAt - s)) : Loc.T(finalStage >= 2 ? "최종 보스" : "최후의 시간");
            hud.text = (daily ? Loc.T("일일 도전") + " · " + Loc.T(DailyChallenge.RunRuleName) : Loc.T("무한 모드")) + "  " + Clock(s) + "   " + Loc.T("보스 처치 ") + BossesDefeated
                     + (finalStage > 0 ? "   <color=#FF5A4A>" + final + "</color>" : best > 0f ? "   " + Loc.T("최고 ") + Clock(best) : "");
        }

        if (s >= 600f) CharacterData.Achieve(CharUnlock.Endless10);        // 새 캐릭터 해제 조건 (1.0.2)
        bool free = Time.timeScale > 0f && !stages.IsMenuOpen && !PlayerController.Cinematic;
        if (finalStage == 0 && s >= FinalAt && free) { StartFinal(); return; }
        // 1.0.4~ 30분: 개발자 엄마 (개발자가 아직 나오지 않았으면 함께)
        if (finalStage >= 1 && finalStage < 3 && !momSummoned && s >= MomAt && free) { StartCoroutine(SummonMom()); return; }
        if (finalStage == 0 && !spawner.bossSpawned && Time.time >= nextBoss && Time.timeScale > 0f && !stages.IsMenuOpen) SpawnBoss();
        if (!NoMobs && Time.time >= nextMidBoss && Time.timeScale > 0f && !stages.IsMenuOpen)
        {
            spawner.SpawnMidBoss();
            nextMidBoss = Time.time + Mathf.Max(30f, MidBossEvery / Mathf.Max(1f, GameMode.EndlessIntensity * 0.6f));
        }
    }

    void SpawnBoss()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0) return;
        GameObject prefab = bossPrefabs[bossTurn % bossPrefabs.Length];
        bossTurn++;
        nextBoss = float.MaxValue;              // 쓰러뜨리면 다시 정함
        spawner.bossSpawned = true;
        spawner.bossCleared = false;
        if (stages.bossBar != null) stages.bossBar.bossSpawn = true;
        Instantiate(prefab, spawner.bossSpawnPoint, Quaternion.identity);
        Hostile.Play("roar", 1f, 0.9f);
        stages.ShowBanner(Loc.T("사막의 모래 폭풍 속에서 보스가 나타났다!"), 2.5f);
    }

    // 15분 (1.0.4~ 30분 → 15분): 살아 있지 않은 종류의 보스를 모두 한꺼번에 (같은 종류는 하나만 · 결계 · 체력바 · 킹 슬라임 목록이 종류마다 하나라서)
    void StartFinal()
    {
        finalStage = 1;
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
        stages.ShowBanner(Loc.T("15분을 버텼다… 모든 보스가 깨어났다!") + "\n" + Loc.T("적이 끝없이 몰려온다!"), 4f);
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
        yield return new WaitForSeconds(1.5f);
        stages.ShowBanner((allDown ? Loc.T("모든 보스가 쓰러졌다…") + "\n" : "") + Loc.T("최종 보스 「개발자」가 강림한다!"), 3.5f);
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
        yield return new WaitForSeconds(together ? 4.5f : 1f);      // 함께면 개발자가 먼저 내려온 뒤
        stages.ShowBanner(Loc.T("30분이 지났다…") + "\n" + Loc.T("최종 보스 「개발자 엄마」가 나타났다!") + "\n" + Loc.T("「우리 아들 괴롭힌 게 너니?」"), 4f);
        Hostile.Play("roar", 1f, 0.35f);
        Hostile.Shake(0.7f);
        yield return new WaitForSeconds(2.5f);
        mom = SpawnFinalBoss(true);
        if (mom == null) momDown = true;
    }

    // 개발자 · 개발자 엄마 만들기 (Start 전에 정함: 체력은 난이도 배율 없이 정확히 15만 · 30만 · 1.0.7~)
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
        if (!momSummoned && finalStage < 3) stages.ShowBanner(Loc.T("개발자를 쓰러뜨렸다!") + "\n" + Loc.T("30분이 되면 누군가 찾아온다…"), 4f);
    }

    // 개발자와 개발자 엄마를 모두 쓰러뜨림 (1.0.4~ 개발자만으로는 끝나지 않음): 무한 모드 클리어 (기록 저장 → 엔딩 → 메인 메뉴)
    System.Collections.IEnumerator ClearEndless(bool momLast)
    {
        finalStage = 3;
        spawner.spawningEnabled = false;
        Prefs.SetInt(ClearKey, 1);
        CharacterData.Achieve(CharUnlock.EndlessClear);      // 새 캐릭터 해제 조건 (1.0.2)
        Prefs.Save();
        PlayerController p = FindFirstObjectByType<PlayerController>();
        if (p != null) p.GrantInvincibility(30f);
        stages.ShowBanner(Loc.T(momLast ? "개발자 엄마를 쓰러뜨렸다!" : "개발자를 쓰러뜨렸다!") + "\n" + Loc.T("무한 모드 클리어!") + "  " + Clock(GameMode.EndlessSeconds), 6f, true);     // 1.0.4~ 해금 알림과 줄 서서 둘 다 보임
        Hostile.Play("chime", 1f, 1f);
        yield return new WaitForSecondsRealtime(5f);
        if (StoryDirector.ShouldSkipAll) yield break;
        yield return StoryDirector.Ending(Difficulty.Endless, null, false);
    }

    // 함께 나온 보스 중 마지막이 아닌 보스를 쓰러뜨렸을 때: 처치 연출만 빼고 보상(진화 · 전리품 · 포인트)은 똑같이
    public void OnExtraBossDown() => OnBossDefeated();

    // StageManager가 보스 처치를 알려 줌
    int trophies;

    public void OnBossDefeated()
    {
        if (finalStage == 3) return;
        BossesDefeated++;
        if (BossesDefeated >= 5) CharacterData.Achieve(CharUnlock.EndlessBosses5);
        // 1.0.4~ 개발자를 쓰러뜨려도 끝나지 않음: 개발자와 엄마를 모두 쓰러뜨려야 클리어 (보스가 쓰러진 그 순간 불리므로 그때 표시)
        bool devWas = devDown, momWas = momDown;
        if (developer != null && developer.IsDead) devDown = true;
        if (mom != null && mom.IsDead) momDown = true;
        if (devSummoned && momSummoned && devDown && momDown) { StartCoroutine(ClearEndless(momDown && !momWas)); return; }
        if (finalStage == 1 && bosss.LiveCount == 0) StartCoroutine(SummonDeveloper());
        if (devDown && !devWas && !momSummoned) StartCoroutine(ForeshadowMom());
        spawner.bossCleared = false;            // 다음 보스를 다시 부를 수 있게
        nextBoss = Time.time + Mathf.Max(BossEveryMin, BossEvery - 15f * BossesDefeated);
        if (SpecialAbilities.UsesEvolution)
        {
            // 보스마다 무기 진화: 1차 → 2차 → 각성 (최대 3번)
            // 진화가 다 끝난 뒤에도 보스를 잡을 이유가 있게: 잡을 때마다 계속 쌓이는 전리품
            SpecialAbilities sp = SpecialAbilities.SharedInstance;
            // 진화 창이 이미 열려 있거나 기다리는 중이면 (함께 나온 보스가 거의 같이 쓰러짐) 이번 보스는 전리품으로
            if (sp != null && (sp.NextEvolutionOptions() == null || stages.IsEvolving))
            {
                string loot = sp.BossTrophy(++trophies);
                stages.ShowBanner(Loc.T("보스 처치!") + "\n" + Loc.T("전리품") + " : " + loot, 3f);
                return;
            }
            stages.ShowBanner(Loc.T("보스 처치!"), 2.5f);
            stages.StartCoroutine(stages.Evolution(true));
            return;
        }
        stages.specialPoints++;
        stages.ShowBanner(Loc.T("보스 처치!  특수 능력 포인트 +1"), 3f);
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
        hud.raycastTarget = false;
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
            phases[p] = new SpawnPhase { killsToEnter = kills[p], spawnInterval = interval[p], maxAlive = alive[p], weights = w };
        }
        float[] minions = new float[enemies.Count];
        for (int i = 0; i < minions.Length; i++) minions[i] = 1f;

        StageConfig endless = new StageConfig
        {
            stageName = "Endless",
            enemies = enemies.ToArray(),
            phases = phases,
            bossKills = int.MaxValue,
            bossPrefab = null,
            minionWeights = minions,
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
        Dictionary<Sprite, Tile> tiles = new Dictionary<Sprite, Tile>();

        foreach (Tilemap tm in map.GetComponentsInChildren<Tilemap>(true))
        {
            TilemapRenderer tr = tm.GetComponent<TilemapRenderer>();
            if (tr != null && tr.sortingOrder >= order) { order = tr.sortingOrder; sorting = tr.sortingLayerID; }
            BoundsInt b = tm.cellBounds;
            foreach (Vector3Int pos in b.allPositionsWithin)
            {
                Sprite s = tm.GetSprite(pos);
                if (s == null || !desert.TryGetValue(s.name.Replace("_hell", suffix), out Sprite d)) continue;
                if (!tiles.TryGetValue(d, out Tile tile))
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = d;
                    tiles[d] = tile;
                }
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

    void Start()
    {
        baseScale = transform.localScale;
        seed = Random.Range(0f, 10f);
    }

    void Update()
    {
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

// 사막 전체: 화면 주변에 불티가 떠오르고 가끔 모래 바람이 스침
public class DesertAmbience : MonoBehaviour
{
    float nextEmber, nextGust;

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null || Time.timeScale == 0f) return;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector3 c = cam.transform.position;

        if (Time.time >= nextEmber)
        {
            nextEmber = Time.time + 0.06f;
            Vector3 p = new Vector3(c.x + Random.Range(-w, w), c.y - h - 0.5f + Random.Range(0f, h * 0.6f), 0f);
            FlameParticle.Spawn(Hostile.Glow, p, new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(1.2f, 2.6f)), Random.Range(1.4f, 2.4f), 0.035f, 0.012f, false);
        }
        if (Time.time >= nextGust)
        {
            nextGust = Time.time + Random.Range(2.5f, 5f);
            Vector3 from = new Vector3(c.x - w - 2f, c.y + Random.Range(-h, h), 0f);
            Fx.Spawn("fx_smoke", from + new Vector3(Random.Range(0f, w * 2f), 0f, 0f), Random.Range(3f, 5f), new Color(0.95f, 0.78f, 0.5f, 0.35f), 10f);
        }
    }
}
