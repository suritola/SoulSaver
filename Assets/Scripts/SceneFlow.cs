using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 1.1.9~ 씬 전환 공용 로딩 화면 (편의성 2차 1)
// 예전엔 모든 씬 전환이 동기 LoadScene 이라 「게임 시작」을 누르면 메뉴가 굳은 채 1~3초 아무 반응이 없었음
// → 검은 막을 내리고, 비동기로 불러오며 0.4초 넘게 걸리면 「불러오는 중」 + 팁 한 줄 + (게임 씬이면) 영웅 초상
// 떠나는 씬은 timeScale 0 으로 얼림 (막이 내리는 동안 적이 움직이거나 플레이어가 맞지 않게) · 새 씬은 늘 timeScale 1 로 시작
public class SceneFlow : MonoBehaviour
{
    static SceneFlow instance;
    static bool loading;
    public static bool Loading => loading;

    Canvas canvas;
    Image curtain;
    RectTransform loadingBox;
    TMP_Text title, tip;
    Image portrait;
    float dotsAt;
    // 2026-10-22~ 팁을 여러 개 (사용자 「렉 걸리는 동안 게이지가 서서히 차고 팁도 여러 개」) — TipEvery 초마다 다음 팁 (같은 팁 연속 없음)
    // 1.6.1~ 팁이 천천히 나타났다 사라짐 (TipFade 초씩) — 읽을 시간을 위해 간격 2.5 → 4초
    const float TipEvery = 4f, TipFade = 0.5f;
    float tipAt;
    int tipIndex = -1;
    // 장면 불러오기는 LoadShare 까지만, 새 장면이 켜진 뒤(맵 다시 깔기 등 Awake · Start 무거운 일) 나머지를 PostLoadSecs 동안 천천히 채움
    const float LoadShare = 0.7f, PostLoadSecs = 0.9f;

    // 모든 LoadScene 을 이것으로
    public static void Go(string scene)
    {
        GameInput.ResetAttackLock();        // 공격 고정 · 필살기 토글은 씬이 바뀌면 풀림 (새 판이 저절로 쏘며 시작하지 않게)
        GameInput.ResetUltLatch();
        if (GameInput.Auto || Application.isBatchMode || GameInput.TrailerRunning) { Time.timeScale = 1f; SceneManager.LoadScene(scene); return; }   // 테스트 · 트레일러는 즉시
        if (loading)
        {
            // 새 씬이 켜진 뒤 그 씬이 곧바로 다른 씬으로 보내는 경우(튜토리얼 「바로 첫 판 시작」 → 메인 메뉴 → 게임 시작)만 이어서,
            // 그 전(막이 내리는 중 · 불러오는 중)의 두 번 누름 · 겹친 호출은 무시
            if (activated) queued = scene;
            return;
        }
        Ensure().StartCoroutine(instance.Guard(instance.Run(scene), null));
    }

    static bool activated;
    static string queued;

    // 1.6.1~ 게임을 켤 때 (사용자 「게임을 켜면 잠시 아무 반응이 없다가 켜짐 → 먼저 게임을 켜고 로딩창을 띄워 기다리게」)
    //   예전엔 빌드 첫 장면이 메인 메뉴라 메뉴 장면 불러오기 · 메뉴의 Awake · Start · 시작 초기화가 첫 프레임 전에 몰려 창이 약 2.5초 검게 멈춰 있었음
    //   → 빌드 첫 장면을 거의 빈 Boot 장면(카메라 · 글꼴 참조 하나)으로 두고, 그 첫 프레임부터 이 로딩 화면을 (막이 내려오는 연출 없이) 바로 띄운 채
    //     메인 메뉴를 비동기로 불러옴 · 자동 봇 · 배치 · 트레일러는 그냥 즉시 · 로딩 화면이 실패하면 동기로라도 메인 메뉴 (Boot 에 남는 길 없음)
    public const string BootScene = "Boot", MenuScene = "MainMenu";
    static bool bootRequested;          // Boot 에서 메인 메뉴 비동기 불러오기를 이미 시작했는지 (실패 처리에서 두 번 부르지 않게)
    const float BootPostSecs = 0.5f;    // 켤 때는 메뉴가 켜진 뒤 게이지를 채우는 시간을 짧게 (그 뒤 시작 연출 TitleIntro 가 이어짐)
    const float LoadCap = 60f;          // 장면 불러오기 기다림 상한 (실제 시간) — 넘으면 그냥 켜고 넘어감 (멈춰 남지 않게)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootToMenu()
    {
        if (SceneManager.GetActiveScene().name != BootScene) return;
        if (SteamManager.Restarting) return;                  // 스팀으로 다시 켜는 중 (이 프로세스는 곧 끝남)
        try { AudioListener.volume = GameSettings.Volume; } catch (System.Exception e) { Debug.LogException(e); }     // 설정 시작(아래 미룬 일)보다 음악이 먼저 나와도 큰 소리로 튀지 않게
        if (GameInput.Auto || Application.isBatchMode || GameInput.TrailerRunning) { BootFallback(); return; }
        bootBuild = instance == null;       // 로딩 UI 를 지금 처음 만들면 글자는 첫 프레임 뒤에 (Run 이 BuildTexts)
        try { Ensure().StartCoroutine(instance.Guard(instance.Run(MenuScene, true), BootFallback)); }
        catch (System.Exception e) { Debug.LogException(e); BootFallback(); }
    }

    // 로딩 화면을 못 띄웠거나 도중에 예외: 미룬 시작 초기화를 마저 하고, 비동기를 시작했으면 Recover 가 활성화를 풀어 그대로 켜지고, 아니면 동기로 메인 메뉴
    static void BootFallback()
    {
        RunStartupNow();
        if (bootRequested) return;
        if (SceneManager.GetActiveScene().name == BootScene) SceneManager.LoadScene(MenuScene);
    }

    // ---------------------------------------------------------------- 첫 프레임 뒤로 미루는 시작 초기화
    // 시험 빌드 계측 (1.6.0): 첫 프레임 전에 도는 시작 초기화가 약 1.7초 — 스팀 초기화 0.47초 · 설정 시작(GameSettings.Boot) 0.34초 · 커서 0.1초 등
    //   1.6.1 계측 빌드 (시작 함수마다 시각): 번역표 LocTable 첫 사용(1,900줄 기본 표 정적 초기화) 약 0.5초 · LocTable.Content 0.34초 · 나머지 번역표 0.1초
    //   Boot 에서 켤 때만 이것들을 로딩 화면이 한 번 그려진 뒤로 미룸 (order 작은 것부터: 스팀 0 → 번역표 5 → 설정 10 — 첫 실행 언어 감지가 스팀 언어를 봄 → 커서 20)
    //   같은 order 는 등록 순서 그대로 (번역표는 먼저 넣은 쪽이 이김 — 순서가 바뀌면 글이 달라질 수 있어 안정 정렬)
    //   메인 메뉴를 부르기 전에 반드시 모두 실행 (로딩 화면 실패 · 동기 길 포함) · 하나가 예외여도 나머지는 그대로
    struct StartupWork { public int order, seq; public System.Action work; }
    static readonly System.Collections.Generic.List<StartupWork> startup = new System.Collections.Generic.List<StartupWork>();
    static bool startupBegun;
    static int startupNext;

    // 미뤘으면 true (호출처는 그냥 돌아감) · false 면 호출처가 지금 바로 실행 (에디터에서 다른 장면으로 시작 · 배치 · 이미 시작함)
    public static bool DeferStartup(int order, System.Action work)
    {
        if (startupBegun || Application.isBatchMode || work == null || !StartingFromBoot()) return false;
        startup.Add(new StartupWork { order = order, seq = startup.Count, work = work });
        return true;
    }

    // 첫 장면이 Boot 인지 (장면 전 초기화 때는 활성 장면 이름이 아직 비어 있을 수 있어 빌드 첫 장면 경로로도 봄 — 에디터에서는 지금 연 장면이 첫 장면)
    static bool StartingFromBoot()
    {
        string n = SceneManager.GetActiveScene().name;
        if (n == BootScene) return true;
        return string.IsNullOrEmpty(n) && !Application.isEditor && SceneUtility.GetScenePathByBuildIndex(0).EndsWith("/" + BootScene + ".unity");
    }

    void SetTexts(bool on)
    {
        foreach (TMP_Text t in new[] { title, tip, pct }) if (t != null) t.gameObject.SetActive(on);
    }

    // 하나 실행 (남은 게 없으면 false)
    static bool StartupStep()
    {
        if (!startupBegun)
        {
            startupBegun = true;
            startup.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : a.seq.CompareTo(b.seq));
        }
        if (startupNext >= startup.Count) return false;
        System.Action w = startup[startupNext++].work;
        try { w(); } catch (System.Exception e) { Debug.LogException(e); }
        return true;
    }

    // 한 프레임에 budget 초까지 몰아서 (번역표 수십 개를 한 프레임씩 하면 그것만 0.5초) · 남은 게 있으면 true
    static bool StartupSteps(float budget)
    {
        float until = Time.realtimeSinceStartup + budget;
        while (StartupStep()) if (Time.realtimeSinceStartup >= until) return startupNext < startup.Count;
        return false;
    }

    static float StartupDone => startup.Count == 0 ? 1f : (float)startupNext / startup.Count;

    static void RunStartupNow() { while (StartupStep()) { } }

    static SceneFlow Ensure()
    {
        if (instance != null) return instance;
        GameObject go = new GameObject("SceneFlow", typeof(RectTransform));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneFlow>();
        instance.Build();
        // 1.2.9~ 장면마다 막 · 로딩 말고 다른 자식은 지움 (다른 UI 가 이 캔버스에 붙으면 장면이 바뀌어도 남았음 · UIKit.HudCanvas)
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (instance == null) return;
            Transform root = instance.transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                string n = root.GetChild(i).name;
                if (n != "Curtain" && n != "Loading") Destroy(root.GetChild(i).gameObject);
            }
        };
        return instance;
    }

    void Build()
    {
        UIKit.EnsureStyle();
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;                         // 이야기 연출 캔버스(31000) · 툴팁(1000) 보다 위
        CanvasScaler cs = gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        UiScaler.Fit(cs);
        gameObject.AddComponent<GraphicRaycaster>();         // 막이 내린 동안 아래 버튼을 다시 누르지 못하게

        RectTransform cr = UIKit.Rect("Curtain", transform, Vector2.zero, Vector2.zero);
        cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one;
        cr.offsetMin = cr.offsetMax = Vector2.zero;
        curtain = cr.gameObject.AddComponent<Image>();
        curtain.color = new Color(0f, 0f, 0f, 0f);
        curtain.raycastTarget = true;

        loadingBox = UIKit.Rect("Loading", transform, Vector2.zero, new Vector2(1920f, 1080f));
        loadingBox.anchorMin = Vector2.zero; loadingBox.anchorMax = Vector2.one;
        loadingBox.offsetMin = loadingBox.offsetMax = Vector2.zero;
        // 1.3.3~ 검은 화면 대신: 짙은 보라 배경 · 떠오르는 영혼 불빛 · 로딩바 · 바 끝을 달리는 영웅
        RectTransform bgr = UIKit.Rect("Backdrop", loadingBox, Vector2.zero, Vector2.zero);
        bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one; bgr.offsetMin = bgr.offsetMax = Vector2.zero;
        Image bg = bgr.gameObject.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.045f, 0.1f, 1f);
        bg.raycastTarget = false;
        Sprite glow = SoftDot();        // 메인 메뉴에서 처음 만들어질 때는 SpecialAbilities.GlowSprite 가 아직 없어서 직접 만듦
        {
            RectTransform hr = UIKit.Rect("Halo", loadingBox, new Vector2(0f, -40f), new Vector2(1400f, 700f));
            Image halo = hr.gameObject.AddComponent<Image>();
            halo.sprite = glow;
            halo.color = new Color(0.45f, 0.3f, 0.75f, 0.22f);
            halo.raycastTarget = false;
            for (int i = 0; i < motes.Length; i++)
            {
                RectTransform mr = UIKit.Rect("Mote", loadingBox, Vector2.zero, new Vector2(18f, 18f));
                Image mi = mr.gameObject.AddComponent<Image>();
                mi.sprite = glow;
                mi.color = new Color(0.55f, 0.9f, 1f, 0.5f);
                mi.raycastTarget = false;
                motes[i] = mr;
            }
        }
        // 로딩바: 금빛 테두리 · 어두운 칸 · 금 → 주황 채움 (왼쪽부터)
        RectTransform frame = UIKit.Rect("BarFrame", loadingBox, new Vector2(0f, 0f), new Vector2(BarW + 8f, 30f));
        barFrame = frame;
        Image fi = frame.gameObject.AddComponent<Image>();
        // 1.4.8~ 새 도트 스타일: 금 단색 테두리 대신 막대 틀 그림(ui_bar_frame · 테두리 반 두께 3 → 안쪽 홈 BarW+2 × 24 에 칸 · 채움이 그대로 들어감)
        //   칸은 틀의 홈이 대신하므로 투명 · 채움은 막대 채움 그림(ui_bar_fill)에 주황을 곱함 · 크기 · 길이 계산은 그대로 · 그림이 없으면 예전 모습
        bool art = UIKit.BarFrameSprite != null && UIKit.BarFillSprite != null;
        if (art)
        {
            fi.sprite = UIKit.BarFrameSprite;
            fi.type = Image.Type.Sliced;
            fi.pixelsPerUnitMultiplier = 2f;
            fi.color = Color.white;
        }
        else fi.color = new Color(0.96f, 0.83f, 0.47f, 1f);
        fi.raycastTarget = false;
        RectTransform well = UIKit.Rect("BarWell", frame, Vector2.zero, new Vector2(BarW, 22f));
        Image wi = well.gameObject.AddComponent<Image>();
        wi.color = art ? new Color(0f, 0f, 0f, 0f) : new Color(0.1f, 0.06f, 0.13f, 1f);
        wi.raycastTarget = false;
        barFill = UIKit.Rect("BarFill", well, Vector2.zero, new Vector2(0f, 22f));
        barFill.anchorMin = barFill.anchorMax = barFill.pivot = new Vector2(0f, 0.5f);
        barFill.anchoredPosition = Vector2.zero;
        Image bi = barFill.gameObject.AddComponent<Image>();
        if (art) bi.sprite = UIKit.BarFillSprite;
        bi.color = new Color(1f, 0.72f, 0.3f, 1f);
        bi.raycastTarget = false;
        RectTransform shine = UIKit.Rect("BarShine", barFill, Vector2.zero, Vector2.zero);
        shine.anchorMin = new Vector2(0f, 0.55f); shine.anchorMax = Vector2.one; shine.offsetMin = shine.offsetMax = Vector2.zero;
        Image si = shine.gameObject.AddComponent<Image>();
        si.color = new Color(1f, 0.9f, 0.6f, 0.7f);
        si.raycastTarget = false;
        // 바 위를 달리는 영웅 (발이 바 위끝에 닿게 · 바 채움 끝을 따라감)
        runner = UIKit.Rect("Runner", loadingBox, Vector2.zero, new Vector2(RunnerSize, RunnerSize));
        runner.pivot = new Vector2(0.5f, 0f);
        portrait = runner.gameObject.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        logoLine = UIKit.Rect("LogoLine", loadingBox, new Vector2(0f, -84f), new Vector2(420f, UiRules.Dot));
        Image ll = logoLine.gameObject.AddComponent<Image>();
        ll.color = UiTokens.Accent;
        ll.raycastTarget = false;
        BuildSpinner();
        // 1.6.1~ 로딩 화면 전체를 한 번에 흐리게 (메인 메뉴 · 새 장면으로 넘어갈 때 검은 컷 대신 짧은 교차 페이드) · 장 전환 RunStageLoad 도 이것을 씀
        loadingGroup = loadingBox.gameObject.AddComponent<CanvasGroup>();
        loadingGroup.interactable = false;
        loadingGroup.blocksRaycasts = false;     // 클릭은 막(curtain)이 막음
        if (!UiRules.Legacy) StyleV2(bg, fi, wi, bi, shine);
        if (!bootBuild) BuildTexts();       // 켤 때는 첫 프레임 뒤에 (Run)
        Layout(false);
        loadingBox.gameObject.SetActive(false);
        canvas.enabled = false;
    }

    // 1.6.1~ 글자 4개 (제목 · 퍼센트 · 팁 · 로고) — 켤 때는 첫 프레임을 그린 뒤에 만듦 (처음 만드는 TMP 글자가 무거워 첫 화면을 늦췄음 · 1.6.1 계측: 로딩 UI 만들기 0.5~0.6초)
    //   보통 전환 · QA 는 Build 에서 바로 · 몇 번 불러도 한 번만
    static bool bootBuild;
    void BuildTexts()
    {
        if (title != null) return;
        title = UIKit.Text(loadingBox, "", 36f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 290f), new Vector2(600f, 54f));      // 바 위를 달리는 영웅(높이 240)보다 위
        UIKit.Forget(title);
        pct = UIKit.Text(loadingBox, "", 22f, new Color(0.92f, 0.88f, 0.8f), new Vector2(BarW * 0.5f + 50f, 0f), new Vector2(90f, 30f));
        UIKit.Forget(pct);
        tip = UIKit.Text(loadingBox, "", 24f, new Color(0.92f, 0.88f, 0.8f), new Vector2(0f, -70f), new Vector2(1200f, 70f));
        tip.fontSizeMin = 16f;
        UIKit.Forget(tip);
        // 켤 때 가운데 로고 (뒤이어 나오는 시작 연출 TitleIntro 와 같은 자리 · 크기 · 청록 밑줄 — 로딩이 걷히면 그대로 이어지게) · 게임 이름이라 번역 안 함
        logo = UIKit.Text(loadingBox, "", 120f, UiRules.Legacy ? title.color : UiTokens.Text1, Vector2.zero, new Vector2(1600f, 160f));
        logo.text = "SOUL SAVER";
        logo.enableAutoSizing = false;
        logo.fontSize = 120f;
        UIKit.Forget(logo);
        if (!UiRules.Legacy)
        {
            title.color = UiTokens.Text1; title.fontSize = title.fontSizeMax = UiTokens.Heading;
            tip.color = UiTokens.Text2; tip.fontSize = tip.fontSizeMax = UiTokens.Body; tip.fontSizeMin = UiTokens.Small;
            pct.color = UiTokens.Text3; pct.fontSize = pct.fontSizeMax = UiTokens.Small;
        }
        Layout(layoutBoot);
    }

    // ---------------------------------------------------------------- 1.6.1~ 켤 때 모양 · 영혼불 스피너
    RectTransform barFrame, logoLine, spinner;
    TMP_Text logo;
    CanvasGroup loadingGroup;
    Image spinnerImage;
    bool spinnerGpu;             // 셰이더로 도는지 (아니면 Tick 이 크기 · 밝기를 맥동)

    // boot: 가운데 로고 · 그 아래 「불러오는 중」 · 막대 (달리는 영웅 없음) / 보통: 예전 자리 (제목 위 · 막대 가운데 · 영웅이 막대 위를 달림)
    //   팁은 둘 다 화면 아래쪽 · 스피너는 둘 다 오른쪽 아래 — 앵커 고정 (Layout Group 없음)
    bool layoutBoot;
    void Layout(bool boot)
    {
        layoutBoot = boot;
        logoLine.gameObject.SetActive(boot);
        runner.gameObject.SetActive(!boot);
        float barY = boot ? -230f : 0f;
        barFrame.anchoredPosition = new Vector2(0f, barY);
        if (title == null) return;          // 글자는 아직 (켤 때 첫 프레임)
        logo.gameObject.SetActive(boot);
        pct.rectTransform.anchoredPosition = new Vector2(BarW * 0.5f + 50f, barY);
        title.rectTransform.anchoredPosition = new Vector2(0f, boot ? barY + 52f : 290f);
        tip.rectTransform.anchoredPosition = new Vector2(0f, -420f);
    }

    // 셰이더 UI/SoulSpinner (Resources/Shaders/UISpinner) 가 GPU 에서 _Time 으로 돌림 — 스크립트는 값을 안 바꿔 캔버스도 다시 짜지 않음
    //   (주의: 메인 스레드가 한 덩어리 일로 막힌 프레임은 GPU 도 새로 그리지 않음 — 막힌 사이를 채우는 게 아니라 Update 없이 매끄럽게 도는 것)
    //   셰이더를 못 읽거나 지원 안 되면 코드로 만든 둥근 빛이 맥동 (예전 방식)
    void BuildSpinner()
    {
        spinner = UIKit.Rect("Spinner", loadingBox, Vector2.zero, new Vector2(SpinnerSize, SpinnerSize));
        spinner.anchorMin = spinner.anchorMax = spinner.pivot = new Vector2(1f, 0f);
        spinner.anchoredPosition = new Vector2(-72f, 64f);
        spinnerImage = spinner.gameObject.AddComponent<Image>();
        spinnerImage.raycastTarget = false;
        spinnerImage.color = UiTokens.Accent;
        Shader sh = null;
        try { sh = Resources.Load<Shader>("Shaders/UISpinner"); } catch (System.Exception e) { Debug.LogException(e); }
        spinnerGpu = sh != null && sh.isSupported;
        if (spinnerGpu) spinnerImage.material = new Material(sh) { hideFlags = HideFlags.DontUnloadUnusedAsset };
        else
        {
            Debug.LogWarning("[SceneFlow] 스피너 셰이더를 못 써서 예전 방식으로");
            spinnerImage.sprite = SoftDot();
        }
    }

    const float SpinnerSize = 88f;

    // UI 개편 (Legacy 끔): 흐린 후광 · 떠다니는 불빛 · 금 막대 틀 · 반사 줄 · 통통 튀는 영웅 대신 — 거의 검은 바탕 · 홈 막대 + 청록 두 톤 채움 · 걷기만 하는 영웅 192 (32 × 6)
    //   글자: 「불러오는 중」 뼈색 34 · 팁 26 Text2 · % 22 Text3 (씬 전환 · 장 전환 로딩이 같은 화면)
    void StyleV2(Image bg, Image frame, Image well, Image fill, RectTransform shine)
    {
        bg.color = UiTokens.Void;
        Transform halo = loadingBox.Find("Halo");
        if (halo != null) halo.gameObject.SetActive(false);
        foreach (RectTransform m in motes) if (m != null) m.gameObject.SetActive(false);
        Sprite inset = UiRules.Sprite("ui2_inset"), f2 = UiRules.Sprite("ui2_fill");
        if (inset != null) { frame.sprite = inset; frame.type = Image.Type.Tiled; frame.pixelsPerUnitMultiplier = 1f; frame.color = Color.white; }
        well.color = new Color(0f, 0f, 0f, 0f);
        if (f2 != null) { fill.sprite = f2; fill.type = Image.Type.Sliced; fill.pixelsPerUnitMultiplier = 1f; }
        fill.color = UiTokens.Accent;
        if (shine != null) shine.gameObject.SetActive(false);
        runner.sizeDelta = new Vector2(192f, 192f);
    }

    // 가운데가 밝고 가장자리로 사라지는 둥근 빛 (64×64, 한 번만)
    static Sprite SoftDot()
    {
        const int N = 64;
        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        Color32[] px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0f, 0f, N, N), Vector2.one * 0.5f, N);
    }

    const float BarW = 900f, RunnerSize = 240f;
    RectTransform barFill, runner;
    TMP_Text pct;
    readonly RectTransform[] motes = new RectTransform[14];
    Sprite[] runFrames;
    float shownProgress, targetProgress;

    // 진행률 (0 ~ 1): 바는 목표를 부드럽게 따라가고, 영웅은 바 끝에서 6프레임으로 걸음 · 영혼 불빛은 아래에서 위로 떠오름 (멈춘 시간과 상관없이 실제 시간)
    // 2026-10-22~ (검토): 로딩 화면 꾸미기(Tick · Dots · ShowLoading)는 안에서 예외를 삼킴 — 그림 · 팁 읽기가 실패해도 씬 불러오기 자체는 끝까지 감
    //   (예외가 Run 까지 올라오면 allowSceneActivation = false 인 op 가 남아 뒤의 씬 전환이 모두 멈췄음) · QA 가 이름으로 부르므로 이름은 그대로
    void Tick() { try { TickCore(); } catch (System.Exception e) { Debug.LogException(e); } }
    void Dots() { try { DotsCore(); } catch (System.Exception e) { Debug.LogException(e); } }
    void ShowLoading(string scene) { try { ShowLoadingCore(scene); } catch (System.Exception e) { Debug.LogException(e); } }

    void TickCore()
    {
        float dt = Time.unscaledDeltaTime, now = Time.unscaledTime;
        shownProgress = Mathf.MoveTowards(shownProgress, targetProgress, dt * 1.6f);
        shownProgress = Mathf.Lerp(shownProgress, targetProgress, 1f - Mathf.Exp(-dt * 6f));
        barFill.sizeDelta = new Vector2(BarW * shownProgress, barFill.sizeDelta.y);
        if (pct != null) pct.text = Mathf.RoundToInt(shownProgress * 100f) + "%";
        float bob = UiRules.Legacy ? Mathf.Abs(Mathf.Sin(now * 9f)) * 6f : 0f;        // UI 개편: 통통 튀지 않고 걷기 그림만
        runner.anchoredPosition = new Vector2(-BarW * 0.5f + BarW * shownProgress, 15f + bob);
        if (runFrames != null && runFrames.Length > 0) portrait.sprite = runFrames[Mathf.FloorToInt(now * 10f) % runFrames.Length];
        if (!spinnerGpu && spinner != null)
        {
            float k = 0.5f + 0.5f * Mathf.Sin(now * 4f);
            spinner.sizeDelta = Vector2.one * SpinnerSize * (0.7f + 0.3f * k);
            spinnerImage.color = new Color(UiTokens.Accent.r, UiTokens.Accent.g, UiTokens.Accent.b, 0.5f + 0.5f * k);
        }
        for (int i = 0; i < motes.Length; i++)
        {
            if (motes[i] == null) continue;
            float speed = 40f + (i * 37 % 50), x = -880f + (i * 131 % 1760);
            float y = Mathf.Repeat(now * speed + i * 97f, 1200f) - 600f;
            motes[i].anchoredPosition = new Vector2(x + Mathf.Sin(now * 0.8f + i) * 30f, y);
            float s = 10f + (i % 4) * 6f;
            motes[i].sizeDelta = new Vector2(s, s);
        }
    }

    // UI 개편 P0-2 (2026-10-22~): 로딩 코루틴을 감쌈 — 로딩 화면을 꾸미는 코드(ShowLoading · Dots · Tick 등)에서 예외가 나면
    //   예전엔 loading 이 true 로 남아 Go() 가 모든 씬 전환을 조용히 무시하고 막이 클릭까지 막았음 (1.2.7 같은 「남는 상태」) → 막 · 로딩 화면 · 시간을 되돌리고 끝냄
    IEnumerator Guard(IEnumerator body, System.Action onFail)
    {
        while (true)
        {
            object cur;
            try
            {
                if (!body.MoveNext()) yield break;
                cur = body.Current;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Recover();
                onFail?.Invoke();
                yield break;
            }
            yield return cur;
        }
    }

    AsyncOperation pendingOp;           // 불러오는 중인 씬 (Recover 가 활성화 막힘을 풀어 뒤 전환이 멈추지 않게)
    bool stageWorked;                   // 장 전환 work 를 이미 실행했는지 (실패해도 맵 교체는 꼭 함)

    void Recover()
    {
        if (pendingOp != null) { pendingOp.allowSceneActivation = true; pendingOp = null; }
        if (loadingBox != null)
        {
            CanvasGroup g = loadingBox.GetComponent<CanvasGroup>();
            if (g != null) g.alpha = 1f;
            loadingBox.gameObject.SetActive(false);
        }
        if (curtain != null) { curtain.color = new Color(0f, 0f, 0f, 0f); curtain.raycastTarget = false; }
        if (canvas != null) canvas.enabled = false;
        Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;
        if (Time.timeScale == 0f && !stageLoading) Time.timeScale = 1f;      // 장 전환이면 호출처(StageManager)가 검은 막 아래서 시간을 직접 풂
        stageLoading = false;
        loading = false;
        activated = false;
        queued = null;
    }

    bool stageLoading;

    // boot: 게임을 켤 때 Boot 장면에서 (막을 바로 까맣게 · 첫 프레임 뒤 팁을 지금 언어로 다시 고름 · 메뉴가 켜진 뒤 채우는 시간 짧게)
    IEnumerator Run(string scene, bool boot = false)
    {
        loading = true;
        activated = false;
        queued = null;
        canvas.enabled = true;
        Time.timeScale = 0f;                                 // 떠나는 씬을 얼림 (일시정지 · 엔딩에서 왔어도 막이 내리는 동안 게임이 풀리지 않게)
        Application.backgroundLoadingPriority = ThreadPriority.High;
        if (boot)
        {
            // 켤 때: 막을 바로 까맣게 · 로딩 화면을 먼저 몇 프레임 그린 뒤에 불러오기 시작
            //   (첫 프레임 전에 LoadSceneAsync 를 부르면 Unity 가 시작 단계에서 그 불러오기를 끝까지 · 켜기까지 한꺼번에 해 버려 로딩 화면이 한 번도 안 그려졌음 — 시험 빌드에서 확인)
            curtain.color = new Color(0f, 0f, 0f, 1f);
            curtain.raycastTarget = true;
            // 첫 프레임은 바탕 · 빈 막대 · 스피너만 — 글자(TMP 첫 그리기 약 0.5초) · 번역표(Loc 첫 사용 = 기본 표 초기화 약 0.5초) · 영웅 그림 읽기를 하지 않음 (1.6.1 계측)
            loadingBox.gameObject.SetActive(true);
            loadingGroup.alpha = 1f;
            Layout(true);
            SetTexts(false);
            if (logo != null) logo.gameObject.SetActive(false);
            runFrames = null;
            shownProgress = targetProgress = 0f;
            Tick();
            yield return null;
            Debug.Log("[SceneFlow] 켤 때 로딩 화면 첫 프레임: " + Time.realtimeSinceStartup.ToString("0.00") + "초");      // 1.6.1~ 시험 빌드에서 Player.log 로 확인
            // 둘째 프레임: 글자를 만들고 로고 (영문 · 번역표를 안 봄) · 퍼센트만 켬
            BuildTexts();
            SetTexts(false);
            logo.gameObject.SetActive(true);
            pct.gameObject.SetActive(true);
            Tick();
            yield return null;
            // 미뤄 둔 시작 초기화 (스팀 → 번역표 → 설정 → 커서) — 한 프레임에 약 50ms 씩 몰아서 · 게이지 0 → 15% · 메인 메뉴 불러오기 전에 모두
            float t0 = Time.realtimeSinceStartup;
            while (StartupSteps(0.05f)) { targetProgress = 0.15f * StartupDone; Tick(); yield return null; }
            targetProgress = 0.15f;
            Debug.Log("[SceneFlow] 켤 때 미룬 시작 초기화 " + startup.Count + "개: " + (Time.realtimeSinceStartup - t0).ToString("0.00") + "초");
            // 이제 번역표 · 언어 · 글꼴이 준비됨: 「불러오는 중」 · 팁을 지금 언어로 (팁은 천천히 나타남)
            title.gameObject.SetActive(true);
            tip.gameObject.SetActive(true);
            dotsAt = Time.unscaledTime;
            tipIndex = -1;
            try { NextTip(); } catch (System.Exception e) { Debug.LogException(e); }
            Dots(); Tick();
            yield return null;
            // 첫 전투 진입 끊김 줄이기: 게임 장면에서 쓰는 셰이더를 지금 한 번씩 준비 (상한 WarmupCap 초 · 예외면 건너뜀)
            IEnumerator warm = WarmupShaders();
            while (true)
            {
                bool more;
                try { more = warm.MoveNext(); } catch (System.Exception e) { Debug.LogException(e); more = false; }
                if (!more) break;
                Dots(); Tick();
                yield return null;
            }
        }
        else yield return Fade(0f, 1f, 0.25f);
        while (scene != null)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(scene);
            if (boot) bootRequested = op != null;
            pendingOp = op;
            if (op == null)
            {
                Debug.LogWarning("[SceneFlow] 씬 없음: " + scene);
                Time.timeScale = 1f;
                if (boot) BootFallback();
                break;
            }
            op.allowSceneActivation = false;
            // 1.3.3~ 막이 내려오면 바로 로딩 화면 (예전엔 0.4초 넘게 걸릴 때만 글자 · 짧으면 검은 막뿐이었음) · 켤 때는 이미 떠 있음 (게이지 · 팁 그대로)
            if (!boot) ShowLoading(scene);
            float capAt = Time.realtimeSinceStartup + LoadCap;
            while (op.progress < 0.9f && Time.realtimeSinceStartup < capAt)
            {
                targetProgress = Mathf.Max(targetProgress, Mathf.Clamp01(op.progress / 0.9f) * LoadShare);     // 켤 때 이미 조금 찬 게이지가 뒤로 가지 않게
                Dots();
                Tick();
                yield return null;
            }
            // 다 불렀으면 LoadShare 까지 채우고 장면을 켬 (켜는 프레임에 새 장면의 무거운 Awake · Start 가 몰림 — 예전엔 100% 에서 멈춘 채 기다렸음)
            targetProgress = LoadShare;
            for (float w = 0f; w < 0.3f && shownProgress < LoadShare - 0.005f; w += Time.unscaledDeltaTime) { Dots(); Tick(); yield return null; }
            Time.timeScale = 1f;                             // 새 씬은 늘 1 로 시작 (호출처들이 각자 1 로 두던 것을 한곳에)
            activated = true;                                // 새 씬의 sceneLoaded · Awake · Start 가 부르는 Go 는 이어서 받음
            op.allowSceneActivation = true;
            capAt = Time.realtimeSinceStartup + LoadCap;
            while (!op.isDone && Time.realtimeSinceStartup < capAt) { Dots(); Tick(); yield return null; }
            if (!op.isDone) Debug.LogWarning("[SceneFlow] 씬 불러오기가 " + LoadCap + "초 넘게 끝나지 않아 로딩 화면을 걷음: " + scene);
            pendingOp = null;
            if (boot) Debug.Log("[SceneFlow] 켤 때 메인 메뉴 켜짐: " + Time.realtimeSinceStartup.ToString("0.00") + "초");
            yield return null;                               // 새 씬 Awake · sceneLoaded 핸들러(MenuExtras.Install 등) 끝나기
            // 나머지를 천천히 채움 (무거운 첫 프레임들이 지나가는 동안 · 팁이 바뀜) — 실제 시간 · 이어서 부르는 씬이 있으면 건너뜀
            float post = boot ? BootPostSecs : PostLoadSecs;
            boot = false;                                    // 이어서 부르는 씬이 있으면 그것은 보통 전환처럼
            if (queued == null)
                for (float w = 0f; w < post; w += Mathf.Min(Time.unscaledDeltaTime, 0.1f))
                {
                    targetProgress = Mathf.Lerp(LoadShare, 1f, w / post);
                    Dots(); Tick();
                    yield return null;
                }
            activated = false;
            scene = queued;                                  // 새 씬이 바로 다른 씬으로 보냈으면 막을 걷지 않고 이어서
            queued = null;
            if (scene != null) Time.timeScale = 0f;
        }
        // 1.6.1~ 막대가 100% 까지 다 찬 것을 보여 준 뒤 잠깐 머물고 (최대 0.6 + 0.3초) · 검은 컷 대신 로딩 화면과 막을 함께 0.3초 페이드
        targetProgress = 1f;
        for (float w = 0f; w < 0.6f && shownProgress < 0.995f; w += Mathf.Min(Time.unscaledDeltaTime, 0.1f)) { Dots(); Tick(); yield return null; }
        shownProgress = 1f; Tick();
        for (float w = 0f; w < HoldSecs; w += Mathf.Min(Time.unscaledDeltaTime, 0.1f)) { Dots(); yield return null; }
        Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;
        float from = curtain.color.a;
        for (float t = 0f; t < 0.3f; t += Mathf.Min(Time.unscaledDeltaTime, 0.1f))
        {
            float a = 1f - t / 0.3f;
            loadingGroup.alpha = a;
            curtain.color = new Color(0f, 0f, 0f, from * a);
            yield return null;
        }
        loadingBox.gameObject.SetActive(false);
        loadingGroup.alpha = 1f;
        curtain.color = new Color(0f, 0f, 0f, 0f);
        curtain.raycastTarget = false;
        canvas.enabled = false;
        loading = false;
    }

    const float HoldSecs = 0.3f;

    // ---------------------------------------------------------------- 1.6.1~ 셰이더 예열 (켤 때 로딩 화면에서 한 번)
    //   게임 장면에서 처음 쓰는 순간 GPU 프로그램을 만드느라 끊기던 것을 미리: 바닥(FloorFlat) · 외곽선(SpriteOutline) · 더하기 빛(SpriteAdditive) · 기본 스프라이트
    //   키워드 없는 기본 변형만 (게임이 쓰는 것) · 하나에 한 프레임 · 상한 WarmupCap 초를 넘기면 나머지는 건너뜀 · 걸린 시간을 Player.log 에
    const float WarmupCap = 3f;
    static readonly string[] WarmupResources = { "Shaders/FloorFlat", "Shaders/SpriteOutline", "Shaders/SpriteAdditive" };

    IEnumerator WarmupShaders()
    {
        float t0 = Time.realtimeSinceStartup;
        int done = 0, total = WarmupResources.Length + 1;
        for (int i = 0; i < total; i++)
        {
            if (Time.realtimeSinceStartup - t0 > WarmupCap) { Debug.LogWarning("[SceneFlow] 셰이더 예열 상한 " + WarmupCap + "초를 넘어 나머지 건너뜀"); break; }
            Shader sh = null;
            try { sh = i < WarmupResources.Length ? Resources.Load<Shader>(WarmupResources[i]) : Shader.Find("Sprites/Default"); }
            catch (System.Exception e) { Debug.LogException(e); }
            if (sh != null && sh.isSupported)
            {
                try
                {
                    ShaderVariantCollection svc = new ShaderVariantCollection();
                    svc.Add(new ShaderVariantCollection.ShaderVariant(sh, UnityEngine.Rendering.PassType.Normal));
                    svc.WarmUp();
                    Destroy(svc);
                    done++;
                }
                catch (System.Exception e) { Debug.LogWarning("[SceneFlow] 셰이더 예열 실패 " + sh.name + ": " + e.Message); }
            }
            targetProgress = Mathf.Max(targetProgress, 0.15f + 0.05f * (i + 1) / total);
            yield return null;
        }
        Debug.Log("[SceneFlow] 셰이더 예열 " + done + "개: " + (Time.realtimeSinceStartup - t0).ToString("0.00") + "초");
    }

    // 2026-10-22~ 장 전환(같은 씬 안에서 맵 교체)도 로딩 화면 — 예전엔 검은 막 아래서 맵을 까는 동안 그냥 검은 화면이라 멈춘 것처럼 보였음 (사용자)
    //   호출처가 검은 막을 다 내린 뒤 부름: 로딩 화면을 띄워 한 프레임 그린 다음 work(무거운 동기 작업) → 게이지를 채우고 걷음 (다시 호출처의 검은 막 위)
    //   씬 불러오는 중 · 테스트 · 트레일러면 그냥 work 만
    //   화면 정리는 SceneFlow(씬이 바뀌어도 남음)에서 돌림 — 호출처가 도중에 사라지거나 work 가 예외를 내도 막 · 로딩 화면 · loading 이 남지 않게
    public static IEnumerator StageLoad(System.Action work)
    {
        if (loading || GameInput.Auto || Application.isBatchMode || GameInput.TrailerRunning) { work(); yield break; }
        bool done = false;
        Ensure().StartCoroutine(instance.Guard(instance.RunStageLoad(work, () => done = true), () =>
        {
            // 로딩 화면에서 예외가 나도 맵 교체 · 적 생성 시작은 꼭 (안 하면 그 장에서 적이 나오지 않아 진행이 막힘)
            if (!instance.stageWorked) { instance.stageWorked = true; try { work(); } catch (System.Exception e) { Debug.LogException(e); } }
            done = true;
        }));
        for (float w = 0f; !done && w < 15f; w += Time.unscaledDeltaTime) yield return null;     // 상한: 실제 시간 15초 (멈춰 남지 않게)
    }

    IEnumerator RunStageLoad(System.Action work, System.Action finished)
    {
        SceneFlow f = this;
        loading = true;
        stageLoading = true;
        stageWorked = false;
        f.canvas.enabled = true;
        f.curtain.color = new Color(0f, 0f, 0f, 1f);
        f.curtain.raycastTarget = true;
        CanvasGroup g = f.loadingBox.GetComponent<CanvasGroup>();
        if (g == null) g = f.loadingBox.gameObject.AddComponent<CanvasGroup>();
        f.ShowLoading(null);
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            g.alpha = t / 0.2f;
            f.targetProgress = 0.3f;
            f.Dots(); f.Tick();
            yield return null;
        }
        g.alpha = 1f;
        yield return null;                                   // 로딩 화면이 그려진 뒤에 굳게
        stageWorked = true;
        try { work(); }
        catch (System.Exception e) { Debug.LogException(e); }
        f.targetProgress = 1f;
        for (float w = 0f; w < 0.6f && f.shownProgress < 0.995f; w += Mathf.Min(Time.unscaledDeltaTime, 0.1f)) { f.Dots(); f.Tick(); yield return null; }
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime) { g.alpha = 1f - t / 0.2f; yield return null; }
        g.alpha = 1f;
        f.loadingBox.gameObject.SetActive(false);
        f.curtain.color = new Color(0f, 0f, 0f, 0f);
        f.curtain.raycastTarget = false;
        f.canvas.enabled = false;
        loading = false;
        stageLoading = false;
        finished();                                          // 다 걷은 뒤에 호출처가 이어 감 (로딩 화면에 가려진 채 게임이 먼저 풀리지 않게)
    }

    IEnumerator Fade(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            curtain.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        curtain.color = new Color(0f, 0f, 0f, to);
        curtain.raycastTarget = to > 0.01f;
    }

    void ShowLoadingCore(string scene)
    {
        loadingBox.gameObject.SetActive(true);
        if (loadingGroup != null) loadingGroup.alpha = 1f;
        BuildTexts();
        Layout(false);
        SetTexts(true);                 // 켤 때 첫 프레임에 꺼 둔 글자가 혹시 남아 있어도 다음 로딩 화면에선 보이게
        dotsAt = Time.unscaledTime;
        Dots();
        tipIndex = -1;
        NextTip();
        // 1.3.3~ 어느 씬이든 지금 영웅이 바 위를 달림 (스킨 시트 6프레임 · 없으면 초상 한 장)
        Sprite s = CharacterUI.Portrait(CharacterData.Selected);
        runFrames = RunFrames(s);
        portrait.sprite = s;
        portrait.enabled = s != null;
        shownProgress = targetProgress = 0f;
        Tick();
    }

    static Sprite[] RunFrames(Sprite first)
    {
        if (first == null) return null;
        string n = first.name;
        int cut = n.LastIndexOf('_');
        if (cut <= 0) return new[] { first };
        string sheet = n.Substring(0, cut);
        Sprite[] all = Resources.LoadAll<Sprite>("Characters/" + sheet);
        if (all == null || all.Length == 0) return new[] { first };
        System.Array.Sort(all, (a, b) => string.CompareOrdinal(a.name, b.name));
        return all;
    }

    void NextTip()
    {
        if (tip == null) return;
        string[] pool = TipPool();
        tipAt = Time.unscaledTime;
        if (pool.Length == 0) { tip.text = ""; return; }
        int next = Random.Range(0, pool.Length);
        if (pool.Length > 1 && next == tipIndex) next = (next + 1) % pool.Length;
        tipIndex = next;
        tip.text = Loc.T(pool[next]);
    }

    // 「불러오는 중」 + 점 세 개가 0.4초마다 하나씩 · 팁은 TipEvery 초마다 다음 것
    void DotsCore()
    {
        if (title == null) return;
        float ph = Time.unscaledTime - tipAt;
        if (ph >= TipEvery) { NextTip(); ph = 0f; }
        tip.alpha = Mathf.Clamp01(Mathf.Min(ph / TipFade, (TipEvery - ph) / TipFade));
        int n = Mathf.FloorToInt((Time.unscaledTime - dotsAt) / 0.4f) % 4;
        title.text = Loc.T("불러오는 중") + new string('.', n) + "<alpha=#00>" + new string('.', 3 - n);     // 글자 폭이 흔들리지 않게 남은 점은 투명
    }

    static readonly string[] ExtraTips =
    {
        "레벨업 카드는 모아 두고 전투가 잠잠할 때 한꺼번에 골라도 됩니다.",
        "일시정지 메뉴에서 이번 판의 빌드와 능력치를 볼 수 있습니다.",
        "떠돌이 상점을 놓치면 다음 상점이 더 빨리 옵니다.",
        "보스 체력이 절반 아래로 내려가면 특수 스킬을 씁니다. 경고 표시를 잘 보세요.",
        // 1.6.1~ 세계관 한 줄 (번역 LocTable.Ux2)
        "영혼의 심연에 갇힌 영혼들은 청록빛 불꽃이 되어 구해 줄 이를 기다린다.",
        "킹 슬라임의 산성은 초원의 풀 한 포기까지 녹여 버렸다.",
        "리치 왕은 지하 묘역에서 죽은 자들을 끝없이 깨운다.",
    };

    static string[] TipPool()
    {
        System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>(CodexUI.LoadingTips());
        list.AddRange(ExtraTips);
        return list.ToArray();
    }
}
