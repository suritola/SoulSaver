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
    public Color CaveBackground => caveBackground;      // 1.2.3~ 무한 지형 변화 배경색 섞기
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
    LineRenderer portalGuideShade;      // 1.4.4~ 안내선 뒤 진한 그림자 (초원 · 묘역의 밝은 바닥에서도 보이게)

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
    public bool OpeningDone => started;     // 1.4.1~ 판 시작 순서(이야기 · 안내 · 각인 · 특수 능력)가 끝났는지 — 갈래 입문 창이 그 위에 겹쳐 보이지 않게 LevelShop 이 기다림
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
        if (banner != null) banner.SetActive(false);         // 1.1.2~ 옛 알림판은 쓰지 않음 (Notice)
        SetFade(0f);

        BuildUpgradeButton();
        ArrangeCorner();                // 1.4.8~ 오른쪽 위 포인트 · 영혼 트리 버튼 한 줄 · [ESC] 안내는 첫 60초만
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
        // 2.1.9: 메인 메뉴 「이어하기」면 시작 이야기 · 첫 카드 대신 저장한 판을 되살림 (RunSave) · 1.2.3~ 무한 모드도 같은 길
        if (RunSave.Pending != null)
        {
            RunSave.Data saved = RunSave.Pending;                   // Restore 가 Pending 을 비우므로 먼저
            if (endless) PrepareEndless();                          // 사막 맵 · 무한 스테이지 · 보스 목록 · 시작 자리 (이야기 판은 Restore 안의 JumpToStage 가 맡음)
            yield return RunSave.Restore(this);
            if (endless)
            {
                GameMode.StartEndlessClock(saved.endlessSeconds);
                EndlessMode em = gameObject.AddComponent<EndlessMode>();
                em.Init(this, endlessBosses, true);
                em.RestoreRun(saved);                               // Init 바로 뒤 · 같은 프레임 (Update 가 끼지 않게)
            }
            if (spawner != null) spawner.spawningEnabled = true;
            started = true;
            if (endless)
                Notice.Big(Loc.T("이어하기"), Loc.T("무한 모드 · 불타는 사막") + "\n" + Loc.T("생존 시간 ") + EndlessMode.Clock(saved.endlessSeconds) + " · " + Loc.T("보스 처치 ") + saved.endlessBosses, NoticeIcon.Boss, 2.8f);
            else
            {
                RunClock.Create(RestoredClock);
                CombatStatsHUD.Ensure();        // 1.1.9~ 전투 통계 (설정 → 표시) · 무한 모드는 EndlessMode.Init 이 붙임
                Notice.Big(Loc.T("이어하기"), Chapters.Title(CurrentStage) + (resumeNote != null ? "\n" + Loc.T(resumeNote) : "") + SoulWeight.BannerLine(), NoticeIcon.Soul, resumeNote != null ? 3.2f : 2.8f);     // 1.2~ 무게를 진 판이면 한 줄 더
            }
            resumeNote = null;
            yield break;
        }
        if (endless) PrepareEndless();
        else if (Chapters.First != 0) PrepareFirstChapter();
        yield return StoryDirector.Intro(endless);
        yield return HeroGuide.Show(this);                  // 1.1.9~ 이 영웅으로 처음이면 평타 · 필살기 · 머리 위 막대 안내 (한 번)
        // 무한 모드 (1.8.7~): 시작할 때는 진화하지 않고, 보스를 잡을 때마다 1차 → 2차 → 각성 (최대 3번)
        if (endless && !Evo) yield return PickEndlessSpecials();
        Contracts.BeginRun();                                   // 1.2~ 영혼 계약 세 장 (화면 아래 · 멈추지 않음)
        if (!endless) yield return SoulBrand.RunStart();        // 1.2~ 영혼 각인 3택 1 (쉬움을 클리어한 이야기 판만)
        if (spawner != null) spawner.spawningEnabled = true;
        started = true;
        FindFirstObjectByType<LevelShop>()?.OpenFreeIfWaiting();      // 1.4.1~ 판 시작 입문 창은 시작 순서가 끝난 지금 (겹쳐 보이지 않게)
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
            CombatStatsHUD.Ensure();
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
        // 1.2~ 영혼의 무게를 진 판이면 장 제목 아래 「영혼의 무게 N · 포인트 +x%」 (무한 모드 · 무게 없는 판은 빈 글)
        string weightLine = SoulWeight.BannerLine();
        if (weightLine.Length > 0) { openBanner += weightLine; openTime = Mathf.Max(openTime, 3f); }
        // 1.0.4~: 장 제목 · 이동 도움말은 시작 카드 창을 닫고 시간이 흐를 때 띄움 (멈춘 카드 창 뒤에서 흘러가 버렸음)
        LevelShop cards = FindFirstObjectByType<LevelShop>();
        while ((cards != null && cards.IsOpen) || Time.timeScale == 0f) yield return null;
        Notice.Big(openBanner, endless ? NoticeIcon.Boss : NoticeIcon.Star, openTime + 0.3f);
        Hints.Show("move", GamePad.Active ? "왼쪽 스틱 이동 · 오른쪽 스틱 조준(기울이면 자동 공격) · [RT] 공격"      // 1.0.5~ 패드 안내
                                          : "{MOVE} 이동 · 마우스 조준 · [{ATTACK}] 공격");      // 1.0.4~ 바꾼 공격 키를 따름
    }

    void PrepareEndless()
    {
        foreach (Renderer r in caveRenderers) if (r != null) r.enabled = false;
        if (spawner == null) return;
        desertMap = EndlessMode.BuildDesertMap(hellMap, spawner.spawnAreaMin, spawner.spawnAreaMax, stage2PlayerStart);
        ToneMap(desertMap, VisualRules.FloorTone(0, true));     // 1단계-7~ 무한 모드 바닥 (영토가 바뀌어도 같은 타일맵이라 한 번)
        if (Camera.main != null) Camera.main.backgroundColor = Toned(desertBackground, VisualRules.FloorTone(0, true));
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

    int shownUpPoints = -1;
    KeyCode shownUpKey;
    bool shownUpPad;
    Loc.Lang shownUpLang;

    void Update()
    {
        UpdateEscHint();
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
                // 최적화: 포인트 · 키 · 패드 · 언어가 바뀔 때만 글자를 새로 (예전엔 떠 있는 동안 매 프레임 문자열 4개 + TMP 재생성)
                KeyCode upKey = KeyBindings.Get(GameAction.Upgrade);
                bool upPad = GamePad.Active;
                if (shownUpPoints != specialPoints || shownUpKey != upKey || shownUpPad != upPad || shownUpLang != Loc.Current)
                {
                    shownUpPoints = specialPoints; shownUpKey = upKey; shownUpPad = upPad; shownUpLang = Loc.Current;
                    upgradeText.text = Loc.T("특수 강화 [") + KeyBindings.Name(GameAction.Upgrade) + Loc.T("]   포인트 ") + specialPoints;
                }
            }
        }
        if (show && !PlayerController.Cinematic && KeyBindings.Down(GameAction.Upgrade)) OpenUpgrade();
    }

    // ================================================================= 거너: 영혼 트리 버튼 · 무기 진화
    bool glowWritten = true;     // 1.1.9: 꺼진 모습을 아직 안 썼으면 true
    void UpdateSoulTreeButton()
    {
        bool show = started && !transitioning && !upgradeOpen && !evolving && !GameInput.TrailerRunning;
        if (upgradeButton == null) return;
        if (upgradeButton.activeSelf != show) upgradeButton.SetActive(show);
        if (!show) return;
        // 살 수 있는 칸이 있을 때만 반짝임. 트리를 새로 만들어 확인하므로 자주 하지 않음:
        // 조각 수가 바뀌었으면 0.5초 뒤에 한 번, 아니면 2초마다
        // 1.1.9: 이미 살 수 있으면 조각이 늘어도 결과가 같으므로 앞당기지 않음 (처치마다 트리를 새로 만들던 비용 · 무한 모드에서 큼)
        if (!affordable && shownShards < SoulShards.Amount && affordCheckAt > Time.unscaledTime + 0.5f) affordCheckAt = Time.unscaledTime + 0.5f;
        else if (shownShards > SoulShards.Amount && affordCheckAt > Time.unscaledTime + 0.5f) affordCheckAt = Time.unscaledTime + 0.5f;
        if (Time.unscaledTime >= affordCheckAt)
        {
            affordCheckAt = Time.unscaledTime + 2f;
            affordable = specials != null && specials.AnyAffordable();
            if (affordable) Hints.Show("soultree_buy", "영혼 조각으로 배울 수 있는 칸이 생겼습니다. [{UPGRADE}]로 영혼 트리를 여세요.");
        }
        UpdateShardGain();
        // 1.1.9: 반짝이지 않을 때는 한 번만 씀 (매 프레임 TMP 색 · 크기 쓰기가 캔버스를 다시 묶었음)
        if (affordable || glowWritten)
        {
            float pulse = affordable ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
            upgradeGlow.localScale = Vector3.one * (1f + 0.15f * pulse);
            upgradeGlowImage.color = affordable ? new Color(0.72f, 0.55f, 1f, 0.3f + 0.45f * pulse) : Color.clear;
            upgradeText.color = affordable ? Color.Lerp(new Color(0.8f, 0.7f, 1f), Color.white, pulse) : new Color(0.75f, 0.7f, 0.82f);
            glowWritten = affordable;
        }
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
            Notice.Toast(NoticeIcon.Tree, Loc.T("지금 배울 수 있는 칸이 없습니다"), 2.5f);
            return;
        }
        Notice.Toast(NoticeIcon.Tree, Loc.T("영혼 트리") + "  ·  " + n.name, 3f);
    }

    // 조각이 들어오면 버튼 왼쪽에 "+N" 이 잠깐 떴다가 사라짐 (0.4초 안에 들어온 양은 합쳐서)
    int gainShown = -1;
    float gainShownAt = -1f;
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
                // 1.4.8~ 버튼 바로 아래 (버튼 왼쪽은 이제 포인트 자리 — ArrangeCorner)
                gainText = UIKit.Text(upgradeButton.transform, "", 22f, new Color(0.8f, 0.7f, 1f), new Vector2(0f, GainY), new Vector2(CornerTreeW, 32f), TextAlignmentOptions.Right);
            }
        }
        // 1.1.9: 글자는 0.25초에 한 번만 다시 만듦 (처치마다 TMP 메시 재생성)
        if (gainText != null && gainShown != gainAmount && Time.unscaledTime - gainShownAt >= 0.25f)
        {
            gainShown = gainAmount;
            gainShownAt = Time.unscaledTime;
            gainText.text = "+" + gainAmount;
        }
        gainFrom = now;
        if (gainText == null) return;
        float left = gainUntil - Time.unscaledTime;
        if (gainText.gameObject.activeSelf != left > 0f) gainText.gameObject.SetActive(left > 0f);
        if (left > 0f)
        {
            float k = 1f - left;                                    // 0 → 1
            gainText.alpha = Mathf.Clamp01(left * 2f);
            gainText.rectTransform.anchoredPosition = new Vector2(0f, GainY + 6f * k);
        }
    }

    const float GainY = -(CornerRowH * 0.5f + 18f);       // 버튼 가운데 기준 아래 (토스트 줄 위끝 -214 보다 위)

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
            yield return new WaitForSecondsRealtime(0.8f * GameSettings.FxTime);        // 1.1.9~ 「빠른 연출」이면 절반
            while (Time.timeScale == 0f || upgradeOpen || transitioning) yield return null;
        }
        float before = Time.timeScale;
        Time.timeScale = 0f;
        int picked = options[0];
        yield return WeaponEvolutionUI.Run(specials, options, id => picked = id);
        bool first = specials.EvolutionTier == 0;
        specials.EvolveWeapon(picked);
        CodexSeen.OnWeaponEvolved(specials, picked);        // 1.2~ 발견형 도감 (처음 고른 진화면 알림)
        // 1.1~ 각성은 aw<번호>@<초> (이야기 모드 3장 각성)
        RunSave.Record((SpecialAbilities.IsAwaken(picked) ? "aw" : "e") + picked + "@" + Mathf.RoundToInt(RunStats.Seconds));      // 판 중간 저장 (이어할 때 같은 진화를 같은 시각으로 다시 적용)
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
        yield return SoulBrand.CatchUp(Chapters.Next(fromStage));      // 1.2~ 2장 · 3장 보스: 영혼 각인 한 장 더
        if (CurrentStage != fromStage) yield break;
        Notice.Big(banner, NoticeIcon.Star, time, NoticeLevel.Important);
        StartCoroutine(PortalCountdown(fromStage));
    }

    // 보스 보상 3택 (1.0.1): 무기 진화 뒤 하나를 고름 (판 중간 저장 'ap' · 'h')
    // 1.2~ 「전설의 힘」 · 「생명의 정수」 두 칸은 그대로(지금 선택지의 상한 유지), 가운데 칸만 보물 상자 보상 풀에서 (ChestRewards.BossSlot)
    //   희귀 60 · 일반 25 · 전설 15% — 전설이면 그 칸을 고를 때 룰렛 (한 판 전설 상한 2 에 포함) · 예전 가운데 칸 「영혼 조각 다발」은 상자 풀의 영혼 조각으로
    // 이어하기 (RunSave): 보스를 잡고 보상을 고르기 전에 나갔으면 지금 (1.2.6~ 가운데 칸은 저장된 그대로 · 고른 보상은 그 보상의 기록으로 다시 적용)
    public IEnumerator ResumeBossReward() => BossReward();

    IEnumerator BossReward()
    {
        if (TutorialRun.Active || Demo.On || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) { RunSave.NeedBossReward = false; yield break; }
        yield return new WaitForSecondsRealtime(0.5f * GameSettings.FxTime);
        while (Time.timeScale == 0f || upgradeOpen || transitioning) yield return null;
        PlayerController player = Hostile.Player;
        ChestRewards.BossSlot slot = player != null ? ChestRewards.MakeBossSlot(player, player.transform.position) : null;
        List<ChoiceUI.Option> options = new List<ChoiceUI.Option>
        {
            new ChoiceUI.Option("전설의 힘", "공격력 +10% (기본 공격력 기준, 이번 판 끝까지)", new Color(1f, 0.6f, 0.3f), "might"),      // 1.0.4~ 「영구 지속」은 다음 판에도 남는 것처럼 읽혔음
        };
        if (slot != null) options.Add(slot.option);
        options.Add(new ChoiceUI.Option("생명의 정수", "최대 체력 +20, 체력 모두 회복", new Color(1f, 0.45f, 0.45f), "essence"));
        int pick = 0;
        yield return ChoiceUI.Run("보스 보상", "하나를 고르세요", options.ToArray(), i => pick = i);
        RunSave.NeedBossReward = false;
        RunSave.BossSlotPick = null;                        // 1.2.6~ 다음 보스는 새로 뽑음
        // ↑ 고른 뒤에야 (고르기 전에 나가면 이어할 때 다시 · 룰렛 도중에 나가도 두 번 받지 않게 받기 전에 지움, 상자와 같음)
        PlayerController p = Hostile.Player;
        if (p == null) yield break;
        if (pick == 0)
        {
            RunSave.AddBaseAttack(p, 10f);         // 1.1.2~ 평타 고점 하향: 지금 공격력 ×1.1 → 기본 공격력의 +10% (영혼 트리 「무기 화력」과 같은 기준, 곱으로 쌓이지 않게)
        }
        else if (slot != null && pick == 1) yield return slot.Take();
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
        if (Evo) { Notice.Big(Loc.T("중간 보스 등장!\n쓰러뜨리면 영혼 조각을 많이 줍니다"), NoticeIcon.Boss, 3f, NoticeLevel.Urgent); return; }
        Notice.Big(Loc.T("중간 보스 등장!\n쓰러뜨리면 특수 능력 포인트 +1"), NoticeIcon.Boss, 3f, NoticeLevel.Urgent);
    }

    void OnMidBossDefeated()
    {
        if (Evo)
        {
            Notice.Toast(NoticeIcon.Shards, Loc.T("영혼 조각을 모았다!\n[") + KeyBindings.Name(GameAction.Upgrade) + Loc.T("] 영혼 트리"), 4f);
            return;
        }
        specialPoints++;
        Hints.Show("upgrade", "특수 능력 포인트는 [{UPGRADE}]를 눌러 새 능력을 배우거나 가진 능력을 진화하는 데 씁니다.");
        Notice.Toast(NoticeIcon.Up, Loc.T("특수 능력 포인트 +1!\n[") + KeyBindings.Name(GameAction.Upgrade) + Loc.T("] 또는 아래 버튼으로 강화"), 4.5f);
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
        UiScaler.Isolate(upgradeButton);                // 1.1.9: 매 프레임 반짝임이 HUD 전체를 다시 묶지 않게

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
        // 1.4.8~ 새 도트 스타일: 옛 Theme/ui_button 대신 금장 테두리 돌판(ui_button) · 못 찾으면 특수 강화 창의 이름판 그림
        UIKit.EnsureStyle();
        img.sprite = RecommendGlow.PillArt ? UIKit.ButtonSprite : specialTree.headerSprite;
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
        upgradeText.fontSizeMin = 20f;          // 1.4.8~ 최소 글자 20 (UI 정리 기획 · 예전 14)
        upgradeText.fontSizeMax = 26f;
        upgradeText.alignment = TextAlignmentOptions.Center;
        upgradeText.raycastTarget = false;

        // 거너: 늘 떠 있는 버튼이라 오른쪽 위 구석에 작게
        // 1.4.8~ 코인 칸 바로 아래 한 줄(포인트 오른쪽)에 — 예전엔 포인트 · [ESC] 안내 아래 세 번째 줄(-222)에 따로 떠 있었음 (ArrangeCorner)
        if (Evo)
        {
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 1f);
            root.sizeDelta = new Vector2(CornerTreeW, CornerRowH);
            root.anchoredPosition = new Vector2(-CornerRight, CornerRowTop);
            upgradeGlow.sizeDelta = new Vector2(CornerTreeW + 80f, 100f);
            upgradeText.fontSize = upgradeText.fontSizeMax = 22f;
            upgradeText.fontSizeMin = 20f;
            Notice.ToastsBelow(root);       // 1.2.5~ 오른쪽 위 토스트가 이 버튼 아래에서 시작하고 버튼보다 위에 그려지게 (예전엔 겹쳐 가렸음)
        }

        PauseInspect.TipTreeButton(upgradeButton);      // 1.1.9~ 마우스를 올리면 설명 (일시정지 중에는 바로)
        upgradeButton.SetActive(false);
    }

    // ================================================================= 1.4.8~ 오른쪽 위 구석 정돈 (UI 정리 기획 2026-10-21)
    // 예전: 코인 칸 아래에 「포인트 : n」(-132) · 「[ESC] 메뉴 · 설정」(-176) · 영혼 트리 버튼(-222)이 저마다 한 줄씩 떠 있었음
    // → 코인 칸(아래끝 -120) 바로 아래 한 줄: 왼쪽 포인트 · 오른쪽 영혼 트리 버튼(거너 · 진화 영웅만)
    //   [ESC] 안내는 그 아래 포인트 밑에, 판이 흐른 첫 60초만 (그 뒤엔 숨김 — 메뉴 키는 설정 · 튜토리얼에서도 알려 줌)
    //   토스트 줄(위끝 -214)은 그대로 이 아래에서 시작
    const float CornerRowTop = -130f, CornerRowH = 44f, CornerRight = 30f, CornerTreeW = 260f, CornerGap = 16f;
    const float EscHintFor = 60f;
    GameObject escHint;
    float escHintTime;

    void ArrangeCorner()
    {
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        bool tree = Evo && upgradeButton != null;
        // 버튼이 잠깐 숨어도(연출 · 창) 포인트가 옆으로 튀지 않게, 이 판에 버튼이 있는지로만 자리를 정함
        float left = tree ? CornerRight + CornerTreeW + CornerGap : CornerRight;
        Point pt = FindFirstObjectByType<Point>();
        if (pt != null && pt.TextInput != null)
        {
            TMP_Text t = pt.TextInput;
            RectTransform r = t.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
            r.sizeDelta = new Vector2(tree ? 300f : 420f, CornerRowH);
            r.anchoredPosition = new Vector2(-left, CornerRowTop);
            t.alignment = TextAlignmentOptions.MidlineRight;
            t.enableWordWrapping = false;
            t.enableAutoSizing = false;
            t.fontSize = 26f;                       // 본문 단계 (예전 30 · 버튼 22 와 한 줄에)
        }
        Transform hint = canvas.transform.Find("KeyHint");
        if (hint != null && hint is RectTransform hr)
        {
            escHint = hint.gameObject;
            hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(1f, 1f);
            hr.sizeDelta = new Vector2(420f, 32f);
            hr.anchoredPosition = new Vector2(-left, CornerRowTop - CornerRowH - 4f);      // 포인트 바로 밑 (영혼 트리 버튼 밑은 「+N」 자리)
            if (hint.TryGetComponent(out TMP_Text ht))
            {
                ht.alignment = TextAlignmentOptions.MidlineRight;
                ht.enableWordWrapping = false;
                ht.enableAutoSizing = false;
                ht.fontSize = 22f;                  // 보조 단계
            }
        }
    }

    // 판 시작 뒤 시간이 흐른 첫 60초만 [ESC] 안내 (멈춘 시간 · 시작 이야기는 세지 않음)
    void UpdateEscHint()
    {
        if (escHint == null) return;
        if (started && Time.timeScale > 0f) escHintTime += Time.deltaTime;
        bool show = escHintTime < EscHintFor && !GameInput.TrailerRunning;
        if (escHint.activeSelf != show) escHint.SetActive(show);
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
        if (Demo.On && Chapters.SlotOf(stage) == 0)       // 체험판은 1장(초원 · 킹 슬라임)에서 끝
        {
            StartCoroutine(DemoFinish(stage));
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
            Notice.Big(Loc.T("거울의 군주를 쓰러뜨렸다!\n모든 스테이지 클리어!"), NoticeIcon.Star, 6f, NoticeLevel.Important);     // 1.0.4~ 뒤 알림(숙련도 · 해금)에 덮이지 않게
            // 클리어 기록 · 난이도 잠금 해제는 바로 저장하고, 잠시 뒤 엔딩
            Difficulty cleared = GameMode.Current;
            if (cleared == Difficulty.Easy && RunStats.Seconds <= 1200f) CharacterData.Achieve(CharUnlock.EasyFast);     // 새 캐릭터 해제 조건 (1.0.2, 1.0.4~ 15분 → 20분)
            RunSave.Delete();           // 판이 끝났으므로 이어할 판은 지움 (2.1.9)
            bool weightWasOpen = SoulWeight.Unlocked;
            string opened = GameMode.OnCleared(cleared);
            Cleared?.Invoke(cleared);
            if (Mastery.OnCleared(CharacterData.Selected))
                Notice.Toast(NoticeIcon.Up, Loc.T("영웅 숙련도가 올랐다!") + "  " + Mastery.Label(CharacterData.Selected), 5f);
            // 1.2~ 영혼의 무게: 처음 어려움을 깨면 열림 · 무게를 진 판이면 이 영웅의 가장 무거운 클리어를 새로 적고 다음 무게를 엶
            string weight = SoulWeight.OnCleared(CharacterData.Selected, cleared, weightWasOpen);
            if (weight != null) Notice.Big(weight, NoticeIcon.Weight, 4f, NoticeLevel.Important);
            if (cleared == Difficulty.Hard) UnlockRoutes.OnWeightCleared();     // 1.2~ 유령 대체 루트: 어려움에서 영혼의 무게 2 클리어
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

    // 체험판: 1장 보스(킹 슬라임)를 쓰러뜨리면 (거너는 무기 진화까지 맛본 뒤) 끝 화면 (Demo.cs)
    IEnumerator DemoFinish(int stage)
    {
        if (spawner != null) spawner.spawningEnabled = false;
        Notice.Big(Loc.T(Defeated[Mathf.Clamp(stage, 0, 3)]), NoticeIcon.Star, 3f, NoticeLevel.Important);
        if (Evo) yield return Evolution(true);
        yield return new WaitForSecondsRealtime(3f * GameSettings.FxTime);
        while (Time.timeScale == 0f) yield return null;     // 레벨업 · 상점 창이 열려 있으면 닫을 때까지
        Demo.ShowEnd();
    }

    // 마지막 장(2.1.1~ 4장) 보스를 쓰러뜨려 한 판을 클리어했을 때 (업적 등)
    public static event System.Action<Difficulty> Cleared;

    IEnumerator EndingAfter(Difficulty cleared, string opened)
    {
        yield return new WaitForSecondsRealtime(4f * GameSettings.FxTime);
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
    // 1단계-7~ 바닥 톤다운 (VisualRules.FloorTone): 타일맵 · 지도 그림 색에 곱함 — 처음 본 색을 기억해 두고 늘 「기억한 색 × 배율」로 (여러 번 불려도 겹쳐 곱하지 않음)
    //   지옥 지도를 복제해 심연 · 사막을 만들므로, 복제 전에 지옥을 원래 색으로 되돌림 · 신전 문(portal)은 건드리지 않음
    readonly Dictionary<Object, Color> toneBase = new Dictionary<Object, Color>();
    void ToneMap(GameObject root, Color mul)
    {
        if (root == null) return;
        foreach (UnityEngine.Tilemaps.Tilemap tm in root.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true)) ToneOne(tm, mul);
        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sortingLayerName == "Background" && (portal == null || !sr.transform.IsChildOf(portal.transform))) ToneOne(sr, mul);
    }
    void ToneOne(Object o, Color mul)
    {
        if (o is UnityEngine.Tilemaps.Tilemap tm)
        {
            if (!toneBase.TryGetValue(tm, out Color b)) toneBase[tm] = b = tm.color;
            tm.color = new Color(b.r * mul.r, b.g * mul.g, b.b * mul.b, b.a);
        }
        else if (o is SpriteRenderer sr)
        {
            if (!toneBase.TryGetValue(sr, out Color b)) toneBase[sr] = b = sr.color;
            sr.color = new Color(b.r * mul.r, b.g * mul.g, b.b * mul.b, b.a);
        }
    }
    static Color Toned(Color c, Color mul) => new Color(c.r * mul.r, c.g * mul.g, c.b * mul.b, c.a);

    void ShowStageMap(int stage)
    {
        ToneMap(hellMap, Color.white);          // 복제 전 원래 색으로 (심연 지도가 지옥을 복제)
        if (stage == AbyssStage.Index) EnsureAbyssMap();
        Color tone = VisualRules.FloorTone(stage, false);
        foreach (Renderer r in caveRenderers)
            if (r != null && r.TryGetComponent(out UnityEngine.Tilemaps.Tilemap ctm)) ToneOne(ctm, stage == 0 ? tone : Color.white);
            else if (r is SpriteRenderer csr && csr.sortingLayerName == "Background") ToneOne(csr, stage == 0 ? tone : Color.white);
        ToneMap(hellMap, stage == 1 ? tone : Color.white);
        ToneMap(meadowMap, stage == 2 ? tone : Color.white);
        ToneMap(abyssMap, stage == AbyssStage.Index ? tone : Color.white);
        foreach (Renderer r in caveRenderers) if (r != null) r.enabled = stage == 0;
        if (hellMap != null) hellMap.SetActive(stage == 1);
        if (meadowMap != null) meadowMap.SetActive(stage == 2);
        if (abyssMap != null) abyssMap.SetActive(stage == AbyssStage.Index);
        if (Camera.main != null)
            Camera.main.backgroundColor = Toned(stage == 0 ? caveBackground : stage == 1 ? hellBackground : stage == 2 ? meadowBackground : AbyssStage.Background, tone);
    }

    // 다음 장으로 맵 교체: 첫 장을 끝내고 처음 넘어갈 때는 특수 능력 선택 (거너는 보스 처치 때 무기 진화로 대신),
    // 그 뒤로는 특수 능력 포인트 +2
    IEnumerator EnterStage(int stage)
    {
        transitioning = true;
        spawner.spawningEnabled = false;
        Time.timeScale = 0f;
        yield return Fade(0f, 1f, 0.8f * GameSettings.FxTime);

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
        yield return Fade(1f, 0f, 0.8f * GameSettings.FxTime);
        Notice.Big(Chapters.Title(stage) + (points ? "\n" + Loc.T("특수 능력 포인트 +2") : ""), NoticeIcon.Star, 3f);
        transitioning = false;
        StartCoroutine(Curse.Altar(stage));     // 2장부터 저주 제단 (1.0.1)
        StartCoroutine(SoulBrand.CatchUp(stage));       // 1.2~ 보스 각인을 고르기 전에 문에 들어왔으면 지금 (이미 골랐으면 그냥 끝남)
        RunSave.Save();            // 장에 들어갈 때마다 자동 저장 (2.1.9) — 제단을 띄운 뒤 (고르기 전에 나가도 이어할 때 다시, 1.0.4~)
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
        shownCountSec = -1;
        // 포탈 판정 상자(문 아래쪽) 위치
        Vector3 target = portal.transform.position + new Vector3(0f, -3.8f, 0f);
        float left = portalTimeLimit;
        float wisp = 0f;

        while (left > 0f && CurrentStage == fromStage && !transitioning)
        {
            left -= Time.deltaTime;            // 멈춘 동안에는 줄지 않음
            bool urgent = left <= portalPullTime;

            // 1.4.8~ 창(영혼 트리 · 특수 트리 · 카드 · 상점 · 일시정지)이 열린 동안은 숨김 — 창 제목 띠 뒤로 비쳐 겹쳐 보였음 (닫히면 다음 프레임에 다시)
            bool showCount = !Notice.Held;          // 시간이 멈춘 동안 · 창이 열린 동안 모두 (Notice 와 같은 기준)
            if (countdownText.gameObject.activeSelf != showCount) countdownText.gameObject.SetActive(showCount);
            // 최적화: 표시 초 · 단계 · 언어가 바뀔 때만 글자를 새로 (예전엔 30초 동안 매 프레임)
            int sec = Mathf.CeilToInt(left);
            if (sec != shownCountSec || urgent != shownCountUrgent || Loc.Current != shownCountLang)
            {
                shownCountSec = sec; shownCountUrgent = urgent; shownCountLang = Loc.Current;
                countdownText.text = urgent
                    ? Loc.T("지옥의 문이 당신을 끌어당긴다!  ") + sec
                    : Loc.T("신전 문으로 들어가세요  ") + sec + Loc.T("초");
            }
            countdownText.color = urgent ? Color.Lerp(new Color(1f, 0.35f, 0.3f), Color.white, Mathf.PingPong(Time.unscaledTime * 4f, 1f)) : Gold;

            // 플레이어 → 문 안내선
            // 1.4.4~ 모든 장에서 보이게: 예전엔 알파 0.25 · 굵기 0.18 금빛 한 줄이라 어두운 지옥 바닥에서만 눈에 띄고
            //   초원 · 묘역의 밝은 바닥에선 거의 안 보였음 (「지옥에서만 문 경로가 나온다」 제보) → 진하게 + 뒤에 어두운 그림자 줄
            portalGuide.enabled = true;
            portalGuide.SetPosition(0, player.position);
            portalGuide.SetPosition(1, target);
            float a = urgent ? 0.75f + 0.25f * Mathf.Sin(Time.time * 10f) : 0.6f + 0.15f * Mathf.Sin(Time.time * 3f);
            portalGuide.startColor = new Color(1f, 0.8f, 0.4f, a);
            portalGuide.endColor = new Color(1f, 0.55f, 0.2f, a * 0.45f);
            portalGuideShade.enabled = true;
            portalGuideShade.SetPosition(0, player.position);
            portalGuideShade.SetPosition(1, target);
            portalGuideShade.startColor = new Color(0.05f, 0.03f, 0.06f, 0.6f * a);
            portalGuideShade.endColor = new Color(0.05f, 0.03f, 0.06f, 0.6f * a * 0.45f);

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
        portalGuideShade.enabled = false;
        if (CurrentStage == fromStage && !transitioning) EnterPortal();
    }

    int shownCountSec = -1;
    bool shownCountUrgent;
    Loc.Lang shownCountLang;

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
            // 최적화: 막바지에 매 프레임 색이 깜빡여도 HUD 전체를 다시 묶지 않게 하위 캔버스로 (클릭 없음)
            UiScaler.Isolate(go, false);
        }
        if (portalGuide == null)
        {
            portalGuide = Hostile.NewLine("PortalGuide", Gold, 0.24f, 3);     // 1.4.4~ 0.18 → 0.24 (모든 장에서 보이게)
            portalGuide.positionCount = 2;
            portalGuide.endWidth = 0.08f;
            portalGuide.enabled = false;
        }
        if (portalGuideShade == null)
        {
            portalGuideShade = Hostile.NewLine("PortalGuideShade", new Color(0.05f, 0.03f, 0.06f, 0.45f), 0.42f, 2);
            portalGuideShade.positionCount = 2;
            portalGuideShade.endWidth = 0.16f;
            portalGuideShade.enabled = false;
        }
    }

    // 스킬 트리에서 능력을 확정했을 때 (능력 번호)
    public void OnPickSpecial(int[] ids)
    {
        pendingPicks = ids;
    }

    // 1.1.2~ 안내 메시지 개편: 옛 알림판(화면 위 640×72 반투명 띠 · 14~26pt)은 Notice 로 — 짧은 한 줄은 오른쪽 위 토스트, 나머지는 화면 가운데 위 큰 알림
    //   대기열 · 우선순위 · 창이 열린 동안 붙잡기(옛 BannerHeld)는 Notice 가 맡음. 새로 쓰는 곳은 Notice.Big · Notice.Toast 를 바로 부를 것
    public bool BannerBusy => Notice.Busy;

    public void ShowBanner(string text, float seconds) => ShowBanner(text, seconds, false);

    public void ShowBanner(string text, float seconds, bool keep) => Notice.Legacy(text, seconds, keep);

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
