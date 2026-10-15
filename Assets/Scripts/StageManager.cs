using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스테이지 진행: 보스 처치 → 신전 문 열림 → 특수 능력 선택 → 지옥 맵
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("참조")]
    public EnemySpawner spawner;
    public Transform player;
    public bossbar bossBar;

    [Header("맵")]
    public Renderer[] caveRenderers;        // 동굴 바닥/벽/소품 (끄기만 함, 벽 콜라이더는 그대로)
    public GameObject hellMap;
    public GameObject portal;               // 보스를 잡으면 켜지는 신전 문
    public Color hellBackground = new Color(0.12f, 0.03f, 0.03f);
    public Vector2 stage2PlayerStart = new Vector2(0.5f, 0f);
    public GameObject meadowMap;            // 초원 (스테이지 2, 2.1.2~ 1장)
    public Color meadowBackground = new Color(0.18f, 0.32f, 0.16f);
    // 무한 모드 불타는 사막 (지옥 맵을 복제해 만듦)
    public Color desertBackground = new Color(0.24f, 0.13f, 0.06f);
    GameObject desertMap;
    // 4장 영혼의 심연 (2.1.1~): 지옥 맵을 복제해 처음 들어갈 때 만듦 (AbyssStage)
    GameObject abyssMap;
    // 지하 묘역의 시작 자리 · 배경색 (2.1.1~ 묘역이 2장이 되어 나중에 다시 켤 때)
    Vector3 caveStart;
    Color caveBackground;
    GameObject[] endlessBosses;

    [Header("UI")]
    public Image fade;                      // 전체 화면 검은 막
    public GameObject specialPanel;         // 특수 능력 선택 화면
    public SpecialTreeUI specialTree;
    public SpecialAbilities specials;
    public GameObject banner;               // 화면 중앙 알림
    public TextMeshProUGUI bannerText;

    public int[] chosenSpecials = new int[0];

    [Header("신전 문 강제 입장")]
    public float portalTimeLimit = 30f;     // 보스를 잡은 뒤 이 시간이 지나면 자동으로 들어감
    public float portalPullTime = 8f;       // 마지막 이 시간 동안 문 쪽으로 점점 세게 끌려감
    TextMeshProUGUI countdownText;
    LineRenderer portalGuide;

    [Header("특수 능력 포인트 (2장 중간 보스 보상)")]
    public int specialPoints = 0;
    // 특수 강화 키는 KeyBindings (기본 T)

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    GameObject upgradeButton;
    public GameObject UpgradeButton => upgradeButton;      // 1.0.4~: 튜토리얼이 영혼 트리 버튼을 짚어 줌
    RectTransform upgradeGlow;
    Image upgradeGlowImage;
    TextMeshProUGUI upgradeText;
    bool upgradeOpen;

    // 특수 능력 화면(지옥 입장 / 강화) · 무기 진화 · 영혼 트리가 열려 있는지
    public bool IsMenuOpen => upgradeOpen || transitioning || evolving;

    // 거너: 무기 진화 + 영혼 트리 (특수 능력 포인트 대신)
    static bool Evo => SpecialAbilities.UsesEvolution;
    // 보스를 잡으면 무기 진화를 고르는지 (진화 캐릭터 · 진화할 단계가 남음)
    public bool EvolutionAvailable => Evo && specials != null && specials.NextEvolutionOptions() != null;
    bool evolving;
    public bool IsEvolving => evolving;
    bool started;                       // 인트로가 끝나 버튼을 보여도 되는지
    // 이어하기 (RunSave.Restore 가 정함): 화면 위 시계를 이 시간부터
    [System.NonSerialized] public float RestoredClock;
    // 이어하기 배너에 덧붙일 말 (ResumeGate: 특수 능력 포인트 +2)
    string resumeNote;
    public bool Started => started;     // 판 중간 저장 (RunSave): 시작 이야기 · 첫 카드 전에는 저장하지 않음
    int treeClosedFrame = -1;
    float affordCheckAt;
    bool affordable;
    int shownShards = -1;                   // 버튼 글은 조각 수가 바뀔 때만 새로 씀 (매 프레임 글자 생성 방지)
    public bool TreeAffordable => affordable;   // 레벨업 알림과 한 줄로 묶을 때 (LevelShop)
    // 경험치 바 위 가운데에 특수 강화 버튼이 떠 있음 (레벨업 대기 배지가 그 위로 비켜 감)
    public bool UpgradeButtonAtBottom => !Evo && upgradeButton != null && upgradeButton.activeSelf;
    TMP_Text gainText;               // 버튼 옆에 잠깐 뜨는 "+N" (조각이 들어오는 느낌)
    int gainAmount, gainFrom = -1;
    float gainUntil;
    KeyCode shownKey;
    Loc.Lang shownLang;

    public int CurrentStage { get; private set; }

    bool transitioning;
    int[] pendingPicks;

    void Awake()
    {
        Instance = this;
    }

    // 3장 초원: 배경이 너무 밝아 이펙트(밝은 색 불꽃 · 번개 · 피해 숫자)가 묻혀서 바닥 · 장식을 어둡고 탁하게
    const float MeadowDim = 0.58f;

    void DimMeadow()
    {
        meadowBackground = Dim(meadowBackground);
        if (meadowMap == null) return;
        foreach (SpriteRenderer sr in meadowMap.GetComponentsInChildren<SpriteRenderer>(true)) sr.color = Dim(sr.color);
        foreach (UnityEngine.Tilemaps.Tilemap tm in meadowMap.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true)) tm.color = Dim(tm.color);
    }

    // 밝기를 낮추고 채도도 조금 빼서 (형광 초록이 이펙트를 덮지 않게), 알파는 그대로
    static Color Dim(Color c)
    {
        float grey = (c.r + c.g + c.b) / 3f;
        Color d = Color.Lerp(c, new Color(grey, grey, grey), 0.25f) * MeadowDim;
        d.a = c.a;
        return d;
    }

    void Start()
    {
        LastStand.ResetRun();
        Curse.Active = false;
        gameObject.AddComponent<ChapterEvents>();        // 장 중반 습격 · 엘리트 · 보물 상자 (1.0.1)
        gameObject.AddComponent<GrowthPrompts>();        // 머리 위 「레벨업 [Space]」 · 「영혼 트리 [T]」 알림 (1.0.1)
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.stages = AbyssStage.Append(spawner.stages);       // 4장 (무한 모드는 그 뒤에 붙음)
            spawner.onBossDefeated += OnBossDefeated;
            spawner.onMidBossSpawned += OnMidBossSpawned;
            spawner.onMidBossDefeated += OnMidBossDefeated;
        }

        if (player != null) caveStart = player.position;
        if (Camera.main != null) caveBackground = Camera.main.backgroundColor;
        if (hellMap != null) hellMap.SetActive(false);
        if (meadowMap != null) meadowMap.SetActive(false);
        DimMeadow();
        if (portal != null) portal.SetActive(false);
        if (specialPanel != null) specialPanel.SetActive(false);
        if (banner != null) banner.SetActive(false);
        CompactBanner();
        SetFade(0f);

        BuildUpgradeButton();
        // 떠돌이 상점의 무기 · 스킬 강화 창은 없어짐: 무기는 진화, 스킬은 트리에서만 강화

        StartCoroutine(Opening());
    }

    // ================================================================= 시작 (인트로 · 무한 모드)
    IEnumerator Opening()
    {
        bool spawnWas = spawner != null && spawner.spawningEnabled;
        if (spawner != null) spawner.spawningEnabled = false;
        yield return null;                                  // 트레일러 촬영이 켜질 때까지 한 프레임
        if (GameInput.TrailerRunning)
        {
            if (spawner != null) spawner.spawningEnabled = spawnWas;
            yield break;
        }

        bool endless = GameMode.IsEndless;
        // 2.1.9: 플레이 튜토리얼 — 시작 이야기 없이 1장 맵에서 단계별 안내 (TutorialRun, 적은 따로 나오지 않음)
        if (TutorialRun.Active)
        {
            if (Chapters.First != 0) PrepareFirstChapter();
            if (spawner != null) spawner.spawningEnabled = false;
            started = true;
            yield return TutorialRun.Run(this);
            yield break;
        }
        // 2.1.9: 메인 메뉴 「이어하기」면 시작 이야기 · 첫 카드 대신 저장한 판을 되살림 (RunSave)
        if (!endless && RunSave.Pending != null)
        {
            yield return RunSave.Restore(this);
            if (spawner != null) spawner.spawningEnabled = true;
            started = true;
            RunClock.Create(RestoredClock);
            ShowBanner(Loc.T("이어하기") + "\n" + Chapters.Title(CurrentStage) + (resumeNote != null ? "\n" + Loc.T(resumeNote) : ""), resumeNote != null ? 3f : 2.5f);
            resumeNote = null;
            yield break;
        }
        if (endless) PrepareEndless();
        else if (Chapters.First != 0) PrepareFirstChapter();
        yield return StoryDirector.Intro(endless);
        // 무한 모드 (1.8.7~): 시작할 때는 진화하지 않고, 보스를 잡을 때마다 1차 → 2차 → 각성 (최대 3번)
        if (endless && !Evo) yield return PickEndlessSpecials();
        if (spawner != null) spawner.spawningEnabled = true;
        started = true;
        string openBanner;
        float openTime;
        if (endless)
        {
            GameMode.StartEndlessClock();
            gameObject.AddComponent<EndlessMode>().Init(this, endlessBosses);
            openBanner = Loc.T("무한 모드 · 불타는 사막") + "\n" + Loc.T("얼마나 버틸 수 있을까?");
            openTime = 3f;
        }
        else
        {
            RunClock.Create();
            openBanner = Chapters.Title(CurrentStage);
            openTime = 2.5f;
            // 영웅 숙련도: 단계마다 레벨업 카드 한 장 더 (1.0.1)
            int mastery = Mastery.Level(CharacterData.Selected);
            LevelShop shop = FindFirstObjectByType<LevelShop>();
            if (mastery > 0 && shop != null && !Demo.On && !DailyChallenge.Active)
            {
                shop.GrantFreePicks(mastery);
                openBanner = Chapters.Title(CurrentStage) + "\n" + Loc.T("숙련 보너스: 레벨업 카드 +{0}").Replace("{0}", mastery.ToString());
                openTime = 3f;
            }
        }
        // 1.0.4~: 장 제목 · 이동 도움말은 시작 카드 창을 닫고 시간이 흐를 때 띄움 (멈춘 카드 창 뒤에서 흘러가 버렸음)
        LevelShop cards = FindFirstObjectByType<LevelShop>();
        while ((cards != null && cards.IsOpen) || Time.timeScale == 0f) yield return null;
        ShowBanner(openBanner, openTime);
        Hints.Show("move", GamePad.Active ? "왼쪽 스틱 이동 · 오른쪽 스틱 조준(기울이면 자동 공격) · [RT] 공격"      // 1.0.5~ 패드 안내
                                          : "{MOVE} 이동 · 마우스 조준 · [{ATTACK}] 공격");      // 1.0.4~ 바꾼 공격 키를 따름
    }

    void PrepareEndless()
    {
        foreach (Renderer r in caveRenderers) if (r != null) r.enabled = false;
        if (spawner == null) return;
        desertMap = EndlessMode.BuildDesertMap(hellMap, spawner.spawnAreaMin, spawner.spawnAreaMax, stage2PlayerStart);
        if (Camera.main != null) Camera.main.backgroundColor = desertBackground;
        spawner.stages = EndlessMode.WithEndlessStage(spawner.stages, out endlessBosses);
        CurrentStage = spawner.stages.Length - 1;           // 무한 모드 스테이지는 맨 뒤 (4장 다음)
        spawner.StartStage(CurrentStage);
        spawner.spawningEnabled = false;
        if (player != null) player.position = stage2PlayerStart;
    }

    // 2.1.1~: 1장이 지하 묘역이 아니면 (초원) 그 맵에서 시작
    void PrepareFirstChapter()
    {
        if (spawner == null) return;
        int s = Chapters.First;
        ShowStageMap(s);
        CurrentStage = s;
        spawner.StartStage(s);
        spawner.spawningEnabled = false;
        if (player != null) player.position = s == 0 ? caveStart : (Vector3)stage2PlayerStart;
    }

    // 무한 모드는 처음부터 특수 능력 3개를 고르고 시작
    IEnumerator PickEndlessSpecials()
    {
        if (specialPanel == null || specialTree == null) yield break;
        transitioning = true;
        float before = Time.timeScale;
        Time.timeScale = 0f;
        pendingPicks = null;
        specialPanel.SetActive(true);
        specialTree.Open(specials, OnPickSpecial);
        while (pendingPicks == null) yield return null;
        ApplyPicks(pendingPicks);
        TooltipUI.Hide();
        specialPanel.SetActive(false);
        Time.timeScale = before;           // 시작 능력 카드 창이 떠 있으면 그대로 멈춰 있음
        transitioning = false;
    }

    void Update()
    {
        if (Evo) { UpdateSoulTreeButton(); return; }
        bool show = specialPoints > 0 && Chapters.SlotOf(CurrentStage) >= 1 && !transitioning && !upgradeOpen;      // 2장부터 (2.1.1~ 장 순서)
        if (upgradeButton != null)
        {
            if (upgradeButton.activeSelf != show) upgradeButton.SetActive(show);
            if (show)
            {
                // 멈춘 화면에서도 반짝이도록 실제 시간 사용
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
                upgradeGlow.localScale = Vector3.one * (1f + 0.15f * pulse);
                upgradeGlowImage.color = new Color(1f, 0.72f, 0.3f, 0.3f + 0.45f * pulse);
                upgradeText.color = Color.Lerp(Gold, Color.white, pulse);
                upgradeText.text = Loc.T("특수 강화 [") + KeyBindings.Name(GameAction.Upgrade) + Loc.T("]   포인트 ") + specialPoints;
            }
        }
        if (show && !PlayerController.Cinematic && KeyBindings.Down(GameAction.Upgrade)) OpenUpgrade();
    }

    // ================================================================= 거너: 영혼 트리 버튼 · 무기 진화
    void UpdateSoulTreeButton()
    {
        bool show = started && !transitioning && !upgradeOpen && !evolving && !GameInput.TrailerRunning;
        if (upgradeButton == null) return;
        if (upgradeButton.activeSelf != show) upgradeButton.SetActive(show);
        if (!show) return;
        // 살 수 있는 칸이 있을 때만 반짝임. 트리를 새로 만들어 확인하므로 자주 하지 않음:
        // 조각 수가 바뀌었으면 0.5초 뒤에 한 번, 아니면 2초마다
        if (shownShards != SoulShards.Amount && affordCheckAt > Time.unscaledTime + 0.5f) affordCheckAt = Time.unscaledTime + 0.5f;
        if (Time.unscaledTime >= affordCheckAt)
        {
            affordCheckAt = Time.unscaledTime + 2f;
            affordable = specials != null && specials.AnyAffordable();
            if (affordable) Hints.Show("soultree_buy", "영혼 조각으로 배울 수 있는 칸이 생겼습니다. [{UPGRADE}]로 영혼 트리를 여세요.");
        }
        UpdateShardGain();
        float pulse = affordable ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
        upgradeGlow.localScale = Vector3.one * (1f + 0.15f * pulse);
        upgradeGlowImage.color = affordable ? new Color(0.72f, 0.55f, 1f, 0.3f + 0.45f * pulse) : Color.clear;
        upgradeText.color = affordable ? Color.Lerp(new Color(0.8f, 0.7f, 1f), Color.white, pulse) : new Color(0.75f, 0.7f, 0.82f);
        // 키 코드 · 언어로 비교 (키 이름 글자를 매 프레임 만들지 않게)
        KeyCode key = GamePad.Active ? KeyCode.JoystickButton6 : KeyBindings.Get(GameAction.Upgrade);      // 1.0.5~ 패드면 View 버튼
        if (shownShards != SoulShards.Amount || shownKey != key || shownLang != Loc.Current)
        {
            shownShards = SoulShards.Amount;
            shownKey = key;
            shownLang = Loc.Current;
            upgradeText.text = Loc.T("영혼 트리") + " [" + KeyBindings.Name(GameAction.Upgrade) + "]  <color=#C9B8FF>" + shownShards + "</color>";
        }
        if (!PlayerController.Cinematic && KeyBindings.Down(GameAction.Upgrade) && Time.frameCount != treeClosedFrame)
        {
            // [Shift]+[T]: 트리를 열지 않고 추천 칸(없으면 가장 싼 칸)을 바로 배움
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) QuickBuy();
            else OpenSoulTree();
        }
    }

    void QuickBuy()
    {
        if (specials == null || Time.timeScale == 0f) return;
        SpecialAbilities.SoulNode n = specials.QuickBuy();
        affordCheckAt = 0f;
        if (n == null)
        {
            SpecialAbilities.SharedFx?.Play("buzz", 0.5f);
            ShowBanner(Loc.T("지금 배울 수 있는 칸이 없습니다"), 1.2f);
            return;
        }
        ShowBanner(Loc.T("영혼 트리") + "  ·  " + n.name, 1.6f);
    }

    // 조각이 들어오면 버튼 왼쪽에 "+N" 이 잠깐 떴다가 사라짐 (0.4초 안에 들어온 양은 합쳐서)
    void UpdateShardGain()
    {
        int now = SoulShards.Amount;
        if (gainFrom < 0) gainFrom = now;
        if (now > gainFrom)
        {
            gainAmount = (Time.unscaledTime < gainUntil - 0.6f ? gainAmount : 0) + (now - gainFrom);
            gainUntil = Time.unscaledTime + 1f;
            if (gainText == null)
            {
                gainText = UIKit.Text(upgradeButton.transform, "", 22f, new Color(0.8f, 0.7f, 1f), new Vector2(-185f, 0f), new Vector2(120f, 36f), TextAlignmentOptions.Right);
            }
            gainText.text = "+" + gainAmount;
        }
        gainFrom = now;
        if (gainText == null) return;
        float left = gainUntil - Time.unscaledTime;
        gainText.gameObject.SetActive(left > 0f);
        if (left > 0f)
        {
            float k = 1f - left;                                    // 0 → 1
            gainText.alpha = Mathf.Clamp01(left * 2f);
            gainText.rectTransform.anchoredPosition = new Vector2(-185f, 6f * k);
        }
    }

    public void OpenSoulTree()
    {
        // 상점, 레벨업, 일시정지 중에는 열지 않음
        if (upgradeOpen || evolving || Time.timeScale == 0f || specials == null || SoulTreeUI.IsOpen) return;
        upgradeOpen = true;
        Time.timeScale = 0f;
        SoulTreeUI.Open(specials, () =>
        {
            upgradeOpen = false;
            treeClosedFrame = Time.frameCount;
            affordCheckAt = 0f;
            Time.timeScale = 1f;
        });
    }

    // 보스를 쓰러뜨리면 무기 진화 (진화할 단계가 남아 있을 때만). waitMenus: 다른 창이 닫힐 때까지 기다림
    public IEnumerator Evolution(bool waitMenus)
    {
        int[] options = specials != null ? specials.NextEvolutionOptions() : null;
        if (options == null) yield break;
        evolving = true;
        if (waitMenus)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            while (Time.timeScale == 0f || upgradeOpen || transitioning) yield return null;
        }
        float before = Time.timeScale;
        Time.timeScale = 0f;
        int picked = options[0];
        yield return WeaponEvolutionUI.Run(specials, options, id => picked = id);
        bool first = specials.EvolutionTier == 0;
        specials.EvolveWeapon(picked);
        RunSave.Record("e" + picked + "@" + Mathf.RoundToInt(RunStats.Seconds));      // 판 중간 저장 (이어할 때 같은 진화를 같은 시각으로 다시 적용)
        if (first) specials.OnEvolvedTier1();
        Time.timeScale = before > 0f ? before : 1f;
        evolving = false;
        affordCheckAt = 0f;
        Hints.Show("soultree_btn", "영혼 조각이 모이면 [{UPGRADE}]로 영혼 트리를 열어 무기 · 필살기 · 생존 · 영혼 칸을 배우세요.");
    }

    IEnumerator EvolutionThenPortal(string banner, float time, bool evolve)
    {
        int fromStage = CurrentStage;
        if (evolve) yield return Evolution(true);
        // 진화 창이 뜨기 전에 이미 문으로 들어갔으면 안내 · 카운트다운은 생략
        if (CurrentStage != fromStage) yield break;
        yield return BossReward();
        if (CurrentStage != fromStage) yield break;
        ShowBanner(banner, time);
        StartCoroutine(PortalCountdown(fromStage));
    }

    // 보스 보상 3택 (1.0.1): 무기 진화 뒤 하나를 고름 — 공격력 · 영혼 조각 · 생명력 (판 중간 저장 'a' · 'h')
    // 이어하기 (RunSave): 보스를 잡고 보상을 고르기 전에 나갔으면 지금
    public IEnumerator ResumeBossReward() => BossReward();

    IEnumerator BossReward()
    {
        if (TutorialRun.Active || Demo.On || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) { RunSave.NeedBossReward = false; yield break; }
        yield return new WaitForSecondsRealtime(0.5f);
        while (Time.timeScale == 0f || upgradeOpen || transitioning) yield return null;
        int shards = 40 + 10 * Chapters.SlotOf(CurrentStage);
        ChoiceUI.Option[] options =
        {
            new ChoiceUI.Option("전설의 힘", "공격력 +10% (이번 판 끝까지)", new Color(1f, 0.6f, 0.3f), "might"),      // 1.0.4~ 「영구 지속」은 다음 판에도 남는 것처럼 읽혔음
            new ChoiceUI.Option("영혼 조각 다발", Loc.T("영혼 조각 +{0}").Replace("{0}", shards.ToString()), new Color(0.55f, 0.9f, 1f), "shards"),
            new ChoiceUI.Option("생명의 정수", "최대 체력 +20, 체력 모두 회복", new Color(1f, 0.45f, 0.45f), "essence"),
        };
        int pick = 0;
        yield return ChoiceUI.Run("보스 보상", "하나를 고르세요", options, i => pick = i);
        RunSave.NeedBossReward = false;                     // 고른 뒤에야 (고르기 전에 나가면 이어할 때 다시)
        PlayerController p = Hostile.Player;
        if (p == null) yield break;
        if (pick == 0)
        {
            p.damage *= 1.1f;
            RunSave.Record("a10");
        }
        else if (pick == 1) SoulShards.Add(shards, p.transform.position, false);
        else
        {
            p.PlayerMaxHealth += 20f;
            p.PlayerHealth = p.PlayerMaxHealth;
            RunSave.Record("h20");
        }
    }

    // ================================================================= 특수 능력 포인트
    void OnMidBossSpawned()
    {
        if (Evo) { ShowBanner(Loc.T("중간 보스 등장!\n쓰러뜨리면 영혼 조각을 많이 줍니다"), 3f); return; }
        ShowBanner(Loc.T("중간 보스 등장!\n쓰러뜨리면 특수 능력 포인트 +1"), 3f);
    }

    void OnMidBossDefeated()
    {
        if (Evo)
        {
            ShowBanner(Loc.T("영혼 조각을 모았다!\n[") + KeyBindings.Name(GameAction.Upgrade) + Loc.T("] 영혼 트리"), 3f);
            return;
        }
        specialPoints++;
        Hints.Show("upgrade", "특수 능력 포인트는 [{UPGRADE}]를 눌러 새 능력을 배우거나 가진 능력을 진화하는 데 씁니다.");
        ShowBanner(Loc.T("특수 능력 포인트 +1!\n[") + KeyBindings.Name(GameAction.Upgrade) + Loc.T("] 또는 아래 버튼으로 강화"), 3f);
    }

    public void OpenUpgrade()
    {
        if (Evo) { OpenSoulTree(); return; }
        // 상점, 레벨업, 일시정지 중에는 열지 않음
        if (upgradeOpen || specialPoints <= 0 || Time.timeScale == 0f || specialTree == null) return;
        upgradeOpen = true;
        Time.timeScale = 0f;
        specialPanel.SetActive(true);
        specialTree.OpenUpgrade(specials, specialPoints, OnUpgradeConfirm, CloseUpgrade);
    }

    // 가진 능력은 진화, 새 능력은 장착
    // 고른 능력: 가진 것은 진화, 새것은 장착
    void ApplyPicks(int[] ids)
    {
        RunSave.Record("s" + string.Join(",", ids));
        if (specials == null) { chosenSpecials = ids; return; }
        List<int> fresh = new List<int>();
        int evolvedCount = 0;
        foreach (int id in ids)
        {
            if (specials.Has(id)) specials.Evolve(id, evolvedCount++);
            else fresh.Add(id);
        }
        if (fresh.Count > 0) specials.Equip(fresh);
        chosenSpecials = new List<int>(specials.EquippedIds).ToArray();
    }

    void OnUpgradeConfirm(int[] ids)
    {
        ApplyPicks(ids);
        specialPoints = Mathf.Max(0, specialPoints - ids.Length);
        CloseUpgrade();
    }

    // 판 중간 저장 (RunSave): 기록한 특수 능력 선택을 다시 적용
    public void ReplayPicks(int[] ids) => ApplyPicks(ids);

    // 이어하기 (RunSave): 연출 없이 그 장의 맵에서 시작
    public void JumpToStage(int stage) => JumpToStage(stage, caveStart);

    // 트레일러 촬영용: 연출 없이 바로 해당 스테이지 맵으로 (0 동굴, 1 지옥, 2 초원, 3 심연)
    public void JumpToStage(int stage, Vector3 caveStart)
    {
        ShowStageMap(stage);
        if (portal != null) portal.SetActive(false);
        if (bossBar != null) bossBar.bossSpawn = false;
        CurrentStage = stage;
        spawner.StartStage(stage);
        if (player != null) player.position = stage == 0 ? caveStart : (Vector3)stage2PlayerStart;
        LastStand.ResetChapter();
        Curse.Active = false;       // 이어하기: 저장 기록의 'k' 가 다시 켬
    }

    // 트레일러 촬영용: 스킬 트리 닫기
    public void CloseUpgradeNow() { if (upgradeOpen) CloseUpgrade(); }

    void CloseUpgrade()
    {
        TooltipUI.Hide();
        specialPanel.SetActive(false);
        upgradeOpen = false;
        Time.timeScale = 1f;
    }

    // 경험치 바 위 가운데에 반짝이는 버튼
    void BuildUpgradeButton()
    {
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null || specialTree == null) return;

        upgradeButton = new GameObject("SpecialUpgradeButton", typeof(RectTransform));
        RectTransform root = upgradeButton.GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        if (banner != null) root.SetSiblingIndex(banner.transform.GetSiblingIndex());
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
        root.sizeDelta = new Vector2(400f, 64f);
        root.anchoredPosition = new Vector2(70f, 128f);

        GameObject glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        upgradeGlow = glow.GetComponent<RectTransform>();
        upgradeGlow.SetParent(root, false);
        upgradeGlow.sizeDelta = new Vector2(560f, 170f);
        upgradeGlowImage = glow.GetComponent<Image>();
        upgradeGlowImage.sprite = specials != null ? specials.glowSprite : null;
        upgradeGlowImage.raycastTarget = false;

        GameObject face = new GameObject("Face", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform fr = face.GetComponent<RectTransform>();
        fr.SetParent(root, false);
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        Image img = face.GetComponent<Image>();
        img.sprite = specialTree.headerSprite;
        img.type = Image.Type.Sliced;
        Button b = face.GetComponent<Button>();
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        cb.selectedColor = Color.white;
        b.colors = cb;
        b.onClick.AddListener(OpenUpgrade);

        GameObject label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform lr = label.GetComponent<RectTransform>();
        lr.SetParent(fr, false);
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(12f, 4f);
        lr.offsetMax = new Vector2(-12f, -4f);
        upgradeText = label.GetComponent<TextMeshProUGUI>();
        if (specialTree.font != null) upgradeText.font = specialTree.font;
        if (specialTree.fontMaterial != null) upgradeText.fontSharedMaterial = specialTree.fontMaterial;
        upgradeText.fontSize = 26f;
        upgradeText.enableAutoSizing = true;
        upgradeText.fontSizeMin = 14f;
        upgradeText.fontSizeMax = 26f;
        upgradeText.alignment = TextAlignmentOptions.Center;
        upgradeText.raycastTarget = false;

        // 거너: 늘 떠 있는 버튼이라 오른쪽 위 구석(코인 · 포인트 아래)에 작게
        if (Evo)
        {
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 1f);
            root.sizeDelta = new Vector2(230f, 44f);
            root.anchoredPosition = new Vector2(-30f, -222f);
            upgradeGlow.sizeDelta = new Vector2(310f, 100f);
            upgradeText.fontSize = upgradeText.fontSizeMax = 20f;
            upgradeText.fontSizeMin = 12f;
        }

        upgradeButton.SetActive(false);
    }

    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.onBossDefeated -= OnBossDefeated;
            spawner.onMidBossSpawned -= OnMidBossSpawned;
            spawner.onMidBossDefeated -= OnMidBossDefeated;
        }
        if (Instance == this) Instance = null;
    }

    void OnBossDefeated(int stage)
    {
        if (GameMode.IsEndless)
        {
            if (EndlessMode.Instance != null) EndlessMode.Instance.OnBossDefeated();
            return;
        }
        Point.Bonus(Point.ChapterBonusPer * Chapters.Number(stage));     // 1.1~ 장 클리어 보너스 200 × 장 번호 (체험판 1장 포함 · 무한 모드는 위에서 빠짐)
        if (stage == 0 && Demo.On)
        {
            StartCoroutine(DemoFinish());
            return;
        }
        int next = Chapters.Next(stage);
        if (next >= 0)
        {
            // 판 중간 저장: 무기 진화를 고르기 전에 나가면 이어할 때 고르게
            RunSave.NeedEvolution = EvolutionAvailable;
            RunSave.NeedBossReward = true;
            // 다음 장으로 가는 문 (2.1.1~ 장 순서는 Chapters: 초원 → 지하 묘역 → 불타는 지옥 → 영혼의 심연)
            string text = Loc.T(Defeated[Mathf.Clamp(stage, 0, 3)]) + "\n" + Loc.T(Gate[Mathf.Clamp(next, 0, 3)]);
            if (portal != null) portal.SetActive(true);
            StartCoroutine(EvolutionThenPortal(text, 3.5f, Evo));     // 무기 진화(진화 캐릭터) → 보스 보상 3택 → 문
        }
        else
        {
            ShowBanner(Loc.T("거울의 군주를 쓰러뜨렸다!\n모든 스테이지 클리어!"), 6f, true);     // 1.0.4~ 뒤 알림(숙련도 · 해금)에 덮이지 않게
            // 클리어 기록 · 난이도 잠금 해제는 바로 저장하고, 잠시 뒤 엔딩
            Difficulty cleared = GameMode.Current;
            if (cleared == Difficulty.Easy && RunStats.Seconds <= 1200f) CharacterData.Achieve(CharUnlock.EasyFast);     // 새 캐릭터 해제 조건 (1.0.2, 1.0.4~ 15분 → 20분)
            RunSave.Delete();           // 판이 끝났으므로 이어할 판은 지움 (2.1.9)
            string opened = GameMode.OnCleared(cleared);
            Cleared?.Invoke(cleared);
            if (Mastery.OnCleared(CharacterData.Selected))
                ShowBanner(Loc.T("영웅 숙련도가 올랐다!") + "  " + Mastery.Label(CharacterData.Selected), 4f);
            StartCoroutine(EndingAfter(cleared, opened));
        }
    }

    // 보스를 쓰러뜨렸을 때 · 그 장으로 가는 문이 열릴 때 (스테이지 번호 순서: 묘역 · 지옥 · 초원 · 심연)
    static readonly string[] Defeated = { "리치 왕을 쓰러뜨렸다!", "지옥의 군주를 쓰러뜨렸다!", "킹 슬라임을 쓰러뜨렸다!", "거울의 군주를 쓰러뜨렸다!" };
    static readonly string[] Gate =
    {
        "갈라진 땅 아래로 무너진 신전이 드러났다",
        "묘역 깊은 곳에서 지옥의 문이 열렸다",
        "성문 너머로 초원이 보인다",
        "꺼져 가는 불길 아래로 심연의 문이 열렸다",
    };

    // 체험판: 리치 왕을 쓰러뜨리면 (거너는 무기 진화까지 맛본 뒤) 끝 화면 (Demo.cs)
    IEnumerator DemoFinish()
    {
        if (spawner != null) spawner.spawningEnabled = false;
        ShowBanner(Loc.T("리치 왕을 쓰러뜨렸다!"), 3f);
        if (Evo) yield return Evolution(true);
        yield return new WaitForSecondsRealtime(3f);
        while (Time.timeScale == 0f) yield return null;     // 레벨업 · 상점 창이 열려 있으면 닫을 때까지
        Demo.ShowEnd();
    }

    // 마지막 장(2.1.1~ 4장) 보스를 쓰러뜨려 한 판을 클리어했을 때 (업적 등)
    public static event System.Action<Difficulty> Cleared;

    IEnumerator EndingAfter(Difficulty cleared, string opened)
    {
        yield return new WaitForSecondsRealtime(4f);
        if (StoryDirector.ShouldSkipAll) yield break;
        yield return StoryDirector.Ending(cleared, opened);
    }

    // PortalGate가 플레이어를 감지하면 호출
    public void EnterPortal()
    {
        if (transitioning) return;
        int next = Chapters.Next(CurrentStage);
        if (next >= 0) StartCoroutine(EnterStage(next));
    }

    void EnsureAbyssMap()
    {
        if (abyssMap == null && spawner != null)
            abyssMap = AbyssStage.BuildMap(hellMap, spawner.spawnAreaMin, spawner.spawnAreaMax, stage2PlayerStart);
    }

    // 그 스테이지의 맵만 켜고 배경색을 맞춤 (0 묘역 · 1 지옥 · 2 초원 · 3 심연)
    void ShowStageMap(int stage)
    {
        if (stage == AbyssStage.Index) EnsureAbyssMap();
        foreach (Renderer r in caveRenderers) if (r != null) r.enabled = stage == 0;
        if (hellMap != null) hellMap.SetActive(stage == 1);
        if (meadowMap != null) meadowMap.SetActive(stage == 2);
        if (abyssMap != null) abyssMap.SetActive(stage == AbyssStage.Index);
        if (Camera.main != null)
            Camera.main.backgroundColor = stage == 0 ? caveBackground : stage == 1 ? hellBackground : stage == 2 ? meadowBackground : AbyssStage.Background;
    }

    // 다음 장으로 맵 교체: 첫 장을 끝내고 처음 넘어갈 때는 특수 능력 선택 (거너는 보스 처치 때 무기 진화로 대신),
    // 그 뒤로는 특수 능력 포인트 +2
    IEnumerator EnterStage(int stage)
    {
        transitioning = true;
        spawner.spawningEnabled = false;
        Time.timeScale = 0f;
        yield return Fade(0f, 1f, 0.8f);

        bool firstGate = Chapters.SlotOf(stage) == 1;
        if (firstGate && !Evo)
        {
            pendingPicks = null;
            specialPanel.SetActive(true);
            if (specialTree != null) specialTree.Open(specials, OnPickSpecial);
            SetFade(0f);
            while (pendingPicks == null) yield return null;
            ApplyPicks(pendingPicks);
            TooltipUI.Hide();
            SetFade(1f);
            specialPanel.SetActive(false);
        }

        ShowStageMap(stage);
        if (portal != null) portal.SetActive(false);
        if (bossBar != null) bossBar.bossSpawn = false;

        CurrentStage = stage;
        spawner.StartStage(stage);
        Curse.MarkPending(stage);       // 페이드 중에 나가도 이어할 때 제단이 나오게 (1.0.4~)
        if (player != null) player.position = stage == 0 ? caveStart : (Vector3)stage2PlayerStart;
        LastStand.ResetChapter();
        bool points = !Evo && !firstGate;
        if (points) specialPoints += 2;

        Time.timeScale = 1f;
        yield return Fade(1f, 0f, 0.8f);
        ShowBanner(Chapters.Title(stage) + (points ? "\n" + Loc.T("특수 능력 포인트 +2") : ""), 3f);
        transitioning = false;
        StartCoroutine(Curse.Altar(stage));     // 2장부터 저주 제단 (1.0.1)
        RunSave.Save();             // 장에 들어갈 때마다 자동 저장 (2.1.9) — 제단을 띄운 뒤 (고르기 전에 나가도 이어할 때 다시, 1.0.4~)
    }

    // 이어하기 (RunSave): 보스를 잡고 문에 들어가기 전에 나갔으면, 문에 들어갈 때 받는 것을 지금 (맵 · 위치는 이미 그 장)
    public IEnumerator ResumeGate(int stage)
    {
        bool firstGate = Chapters.SlotOf(stage) == 1;
        if (firstGate && !Evo && specialPanel != null)
        {
            transitioning = true;
            float before = Time.timeScale;
            Time.timeScale = 0f;
            pendingPicks = null;
            specialPanel.SetActive(true);
            if (specialTree != null) specialTree.Open(specials, OnPickSpecial);
            while (pendingPicks == null) yield return null;
            ApplyPicks(pendingPicks);
            TooltipUI.Hide();
            specialPanel.SetActive(false);
            Time.timeScale = before > 0f ? before : 1f;
            transitioning = false;
        }
        bool points = !Evo && !firstGate;
        if (points)
        {
            specialPoints += 2;
            resumeNote = "특수 능력 포인트 +2";
        }
        StartCoroutine(Curse.Altar(stage));
    }

    // 보스를 잡은 뒤 제한 시간: 막바지엔 문 쪽으로 끌려가고, 끝나면 자동 입장
    IEnumerator PortalCountdown(int fromStage)
    {
        if (portal == null || player == null || CurrentStage != fromStage) yield break;
        EnsureCountdownUI();
        // 포탈 판정 상자(문 아래쪽) 위치
        Vector3 target = portal.transform.position + new Vector3(0f, -3.8f, 0f);
        float left = portalTimeLimit;
        float wisp = 0f;

        while (left > 0f && CurrentStage == fromStage && !transitioning)
        {
            left -= Time.deltaTime;            // 멈춘 동안에는 줄지 않음
            bool urgent = left <= portalPullTime;

            countdownText.gameObject.SetActive(true);
            countdownText.text = urgent
                ? Loc.T("지옥의 문이 당신을 끌어당긴다!  ") + Mathf.CeilToInt(left)
                : Loc.T("신전 문으로 들어가세요  ") + Mathf.CeilToInt(left) + Loc.T("초");
            countdownText.color = urgent ? Color.Lerp(new Color(1f, 0.35f, 0.3f), Color.white, Mathf.PingPong(Time.unscaledTime * 4f, 1f)) : Gold;

            // 플레이어 → 문 안내선
            portalGuide.enabled = true;
            portalGuide.SetPosition(0, player.position);
            portalGuide.SetPosition(1, target);
            float a = urgent ? 0.5f + 0.3f * Mathf.Sin(Time.time * 10f) : 0.25f + 0.1f * Mathf.Sin(Time.time * 3f);
            portalGuide.startColor = new Color(1f, 0.8f, 0.4f, a);
            portalGuide.endColor = new Color(1f, 0.55f, 0.2f, a * 0.3f);

            if (urgent && Time.timeScale > 0f)
            {
                // 점점 강해지는 끌어당김 (마지막엔 이동 속도보다 강함)
                float k = 1f - left / portalPullTime;
                float pull = Mathf.Lerp(3f, 26f, k * k);
                player.position = Vector3.MoveTowards(player.position, target, pull * Time.deltaTime);

                wisp += Time.deltaTime;
                if (wisp >= 0.05f)
                {
                    wisp = 0f;
                    SoulWisp.Spawn(player.position + (Vector3)(Random.insideUnitCircle * 3f), target, new Color(1f, 0.6f, 0.25f, 0.9f));
                }
            }
            yield return null;
        }

        countdownText.gameObject.SetActive(false);
        portalGuide.enabled = false;
        if (CurrentStage == fromStage && !transitioning) EnterPortal();
    }

    void EnsureCountdownUI()
    {
        if (countdownText == null)
        {
            Canvas canvas = UIKit.HudCanvas();
            GameObject go = new GameObject("PortalCountdown", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(canvas.transform, false);
            if (banner != null) r.SetSiblingIndex(banner.transform.GetSiblingIndex());
            // 알림판(-64 ~ -136) 바로 아래, 레벨업 표시(-222 ~)보다 위
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(1100f, 64f);
            r.anchoredPosition = new Vector2(0f, -148f);
            countdownText = go.GetComponent<TextMeshProUGUI>();
            if (specialTree != null && specialTree.font != null) countdownText.font = specialTree.font;
            if (specialTree != null && specialTree.fontMaterial != null) countdownText.fontSharedMaterial = specialTree.fontMaterial;
            countdownText.fontSize = 36f;
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.raycastTarget = false;
        }
        if (portalGuide == null)
        {
            portalGuide = Hostile.NewLine("PortalGuide", Gold, 0.18f, 3);
            portalGuide.positionCount = 2;
            portalGuide.endWidth = 0.05f;
            portalGuide.enabled = false;
        }
    }

    // 스킬 트리에서 능력을 확정했을 때 (능력 번호)
    public void OnPickSpecial(int[] ids)
    {
        pendingPicks = ids;
    }

    // 1.0.4~: 알림판 줄 세우기 — 예전엔 새 알림이 앞 알림을 바로 지워서 해금 달성 · 모든 스테이지 클리어 알림이 보스 처치 · 숙련도 · 재촉 알림에 덮였음
    //   떠 있는 동안 새 알림이 오면 뒤에 줄을 서고, 줄이 있으면 지금 알림은 최소 MinBannerShow 초만 보인 뒤 다음으로 넘어감
    //   keep = 해금 · 클리어처럼 꼭 봐야 하는 알림 (기다리는 알림이 있어도 끝까지 보임)
    const float MinBannerShow = 1.2f;
    const int MaxBannerQueue = 4;
    readonly List<(string text, float seconds, bool keep)> bannerQueue = new List<(string text, float seconds, bool keep)>();
    string bannerShowing;
    bool bannerKeep;
    float bannerTick = -1f;

    // 알림판이 떠 있으면 true (재촉 알림처럼 급하지 않은 것은 이때 건너뜀)
    public bool BannerBusy => bannerRoutine != null && Time.unscaledTime - bannerTick < 3f;

    public void ShowBanner(string text, float seconds) => ShowBanner(text, seconds, false);

    public void ShowBanner(string text, float seconds, bool keep)
    {
        if (banner == null) return;
        // 코루틴이 (오브젝트가 꺼지는 등으로) 멈췄으면 줄을 비우고 새로 시작
        if (bannerRoutine != null && Time.unscaledTime - bannerTick >= 3f) { StopCoroutine(bannerRoutine); bannerRoutine = null; bannerQueue.Clear(); }
        if (bannerRoutine == null)
        {
            bannerTick = Time.unscaledTime;
            bannerRoutine = StartCoroutine(BannerRoutine(text, seconds, keep));
            return;
        }
        // 같은 글이 떠 있거나 이미 줄에 있으면 또 넣지 않음
        if (text == bannerShowing) return;
        foreach (var q in bannerQueue) if (q.text == text) return;
        if (bannerQueue.Count >= MaxBannerQueue)
        {
            int drop = bannerQueue.FindIndex(q => !q.keep);
            if (drop < 0) { if (!keep) return; drop = 0; }
            bannerQueue.RemoveAt(drop);
        }
        bannerQueue.Add((text, seconds, keep));
    }

    Coroutine bannerRoutine;

    // 알림판을 화면 위쪽의 작은 띠로 (시야를 가리지 않게)
    CanvasGroup bannerGroup;

    void CompactBanner()
    {
        if (banner == null) return;
        RectTransform r = banner.GetComponent<RectTransform>();
        if (r != null)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -64f);       // 플레이 시간 바로 아래
            r.sizeDelta = new Vector2(640f, 72f);             // 왼쪽 위 체력 패널(x 620까지)에 닿지 않는 폭
        }
        Image bg = banner.GetComponent<Image>();
        if (bg != null) bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 0.72f);
        if (bannerText != null)
        {
            RectTransform tr = bannerText.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(20f, 6f);
            tr.offsetMax = new Vector2(-20f, -6f);
            bannerText.enableAutoSizing = true;
            bannerText.fontSizeMin = 14f;
            bannerText.fontSizeMax = 26f;
            bannerText.raycastTarget = false;
        }
        // ?? 는 유니티의 "없음"(가짜 null)을 못 알아보므로 직접 확인
        bannerGroup = banner.GetComponent<CanvasGroup>();
        if (bannerGroup == null) bannerGroup = banner.AddComponent<CanvasGroup>();
        bannerGroup.blocksRaycasts = false;
        bannerGroup.interactable = false;
    }

    // 알림판 높이: 평소엔 플레이 시간 바로 아래, 보스 체력바가 떠 있으면 그 아래 (체력바를 가리지 않게)
    readonly Vector3[] bossBarCorners = new Vector3[4];
    float BannerY()
    {
        const float Normal = -64f;
        if (bossBar == null || !bossBar.bossSpawn || bossBar.backBar == null || !bossBar.backBar.activeInHierarchy) return Normal;
        RectTransform bb = bossBar.LowestBar;      // 분열한 슬라임 체력바가 여럿이면 맨 아래 것 기준
        RectTransform parent = banner.transform.parent as RectTransform;
        if (bb == null || parent == null) return Normal;
        bb.GetWorldCorners(bossBarCorners);
        float bottom = parent.InverseTransformPoint(bossBarCorners[0]).y - parent.rect.yMax;     // 부모 위쪽 기준
        return Mathf.Min(Normal, bottom - 12f);
    }

    IEnumerator BannerRoutine(string text, float seconds, bool keep)
    {
        // 1.0.4~: 한 알림이 끝나면 줄 선 다음 알림을 이어서 보임
        RectTransform r = banner.GetComponent<RectTransform>();
        while (true)
        {
            bannerShowing = text;
            bannerKeep = keep;
            bannerText.text = text;
            banner.SetActive(true);
            // 살짝 내려오며 나타났다가 서서히 사라짐
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                bannerTick = Time.unscaledTime;
                float k = t / 0.2f;
                float y = BannerY();
                if (bannerGroup != null) bannerGroup.alpha = k;
                if (r != null) r.anchoredPosition = new Vector2(0f, Mathf.Lerp(y + 20f, y, k));
                yield return null;
            }
            if (bannerGroup != null) bannerGroup.alpha = 1f;
            // 떠 있는 동안 보스 체력바가 나타나거나 사라지면 따라 움직임 (기다리는 알림이 있으면, 꼭 봐야 하는 알림이 아닌 한 짧게)
            for (float t = 0f; t < Mathf.Max(0.3f, seconds - 0.5f); t += Time.unscaledDeltaTime)
            {
                bannerTick = Time.unscaledTime;
                if (!bannerKeep && bannerQueue.Count > 0 && t >= MinBannerShow) break;
                if (r != null) r.anchoredPosition = new Vector2(0f, BannerY());
                yield return null;
            }
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                bannerTick = Time.unscaledTime;
                if (bannerGroup != null) bannerGroup.alpha = 1f - t / 0.3f;
                if (r != null) r.anchoredPosition = new Vector2(0f, BannerY());
                yield return null;
            }
            if (bannerQueue.Count == 0) break;
            (text, seconds, keep) = bannerQueue[0];
            bannerQueue.RemoveAt(0);
        }
        banner.SetActive(false);
        bannerShowing = null;
        bannerRoutine = null;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetFade(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetFade(to);
    }

    void SetFade(float alpha)
    {
        if (fade == null) return;
        Color c = fade.color;
        c.a = alpha;
        fade.color = c;
        fade.raycastTarget = alpha > 0.01f;
        fade.gameObject.SetActive(alpha > 0.001f);
    }
}
