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
    const float TipEvery = 2.5f;
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
        title = UIKit.Text(loadingBox, "", 36f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 290f), new Vector2(600f, 54f));      // 바 위를 달리는 영웅(높이 240)보다 위
        UIKit.Forget(title);
        // 로딩바: 금빛 테두리 · 어두운 칸 · 금 → 주황 채움 (왼쪽부터)
        RectTransform frame = UIKit.Rect("BarFrame", loadingBox, new Vector2(0f, 0f), new Vector2(BarW + 8f, 30f));
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
        pct = UIKit.Text(loadingBox, "", 22f, new Color(0.92f, 0.88f, 0.8f), new Vector2(BarW * 0.5f + 50f, 0f), new Vector2(90f, 30f));
        UIKit.Forget(pct);
        tip = UIKit.Text(loadingBox, "", 24f, new Color(0.92f, 0.88f, 0.8f), new Vector2(0f, -70f), new Vector2(1200f, 70f));
        tip.fontSizeMin = 16f;
        UIKit.Forget(tip);
        // 바 위를 달리는 영웅 (발이 바 위끝에 닿게 · 바 채움 끝을 따라감)
        runner = UIKit.Rect("Runner", loadingBox, Vector2.zero, new Vector2(RunnerSize, RunnerSize));
        runner.pivot = new Vector2(0.5f, 0f);
        portrait = runner.gameObject.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        if (!UiRules.Legacy) StyleV2(bg, fi, wi, bi, shine);
        loadingBox.gameObject.SetActive(false);
        canvas.enabled = false;
    }

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
        title.color = UiTokens.Text1; title.fontSize = title.fontSizeMax = UiTokens.Heading;
        tip.color = UiTokens.Text2; tip.fontSize = tip.fontSizeMax = UiTokens.Body; tip.fontSizeMin = UiTokens.Small;
        pct.color = UiTokens.Text3; pct.fontSize = pct.fontSizeMax = UiTokens.Small;
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
        pct.text = Mathf.RoundToInt(shownProgress * 100f) + "%";
        float bob = UiRules.Legacy ? Mathf.Abs(Mathf.Sin(now * 9f)) * 6f : 0f;        // UI 개편: 통통 튀지 않고 걷기 그림만
        runner.anchoredPosition = new Vector2(-BarW * 0.5f + BarW * shownProgress, 15f + bob);
        if (runFrames != null && runFrames.Length > 0) portrait.sprite = runFrames[Mathf.FloorToInt(now * 10f) % runFrames.Length];
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

    IEnumerator Run(string scene)
    {
        loading = true;
        activated = false;
        queued = null;
        canvas.enabled = true;
        Time.timeScale = 0f;                                 // 떠나는 씬을 얼림 (일시정지 · 엔딩에서 왔어도 막이 내리는 동안 게임이 풀리지 않게)
        Application.backgroundLoadingPriority = ThreadPriority.High;
        yield return Fade(0f, 1f, 0.25f);
        while (scene != null)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(scene);
            pendingOp = op;
            if (op == null)
            {
                Debug.LogWarning("[SceneFlow] 씬 없음: " + scene);
                Time.timeScale = 1f;
                break;
            }
            op.allowSceneActivation = false;
            // 1.3.3~ 막이 내려오면 바로 로딩 화면 (예전엔 0.4초 넘게 걸릴 때만 글자 · 짧으면 검은 막뿐이었음)
            ShowLoading(scene);
            while (op.progress < 0.9f)
            {
                targetProgress = Mathf.Clamp01(op.progress / 0.9f) * LoadShare;
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
            while (!op.isDone) { Dots(); Tick(); yield return null; }
            pendingOp = null;
            yield return null;                               // 새 씬 Awake · sceneLoaded 핸들러(MenuExtras.Install 등) 끝나기
            // 나머지를 천천히 채움 (무거운 첫 프레임들이 지나가는 동안 · 팁이 바뀜) — 실제 시간 · 이어서 부르는 씬이 있으면 건너뜀
            if (queued == null)
                for (float w = 0f; w < PostLoadSecs; w += Mathf.Min(Time.unscaledDeltaTime, 0.1f))
                {
                    targetProgress = Mathf.Lerp(LoadShare, 1f, w / PostLoadSecs);
                    Dots(); Tick();
                    yield return null;
                }
            activated = false;
            scene = queued;                                  // 새 씬이 바로 다른 씬으로 보냈으면 막을 걷지 않고 이어서
            queued = null;
            if (scene != null) Time.timeScale = 0f;
        }
        loadingBox.gameObject.SetActive(false);
        Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;
        yield return Fade(1f, 0f, 0.3f);
        canvas.enabled = false;
        loading = false;
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
        if (Time.unscaledTime - tipAt >= TipEvery) NextTip();
        int n = Mathf.FloorToInt((Time.unscaledTime - dotsAt) / 0.4f) % 4;
        title.text = Loc.T("불러오는 중") + new string('.', n) + "<alpha=#00>" + new string('.', 3 - n);     // 글자 폭이 흔들리지 않게 남은 점은 투명
    }

    static readonly string[] ExtraTips =
    {
        "레벨업 카드는 모아 두고 전투가 잠잠할 때 한꺼번에 골라도 됩니다.",
        "일시정지 메뉴에서 이번 판의 빌드와 능력치를 볼 수 있습니다.",
        "떠돌이 상점을 놓치면 다음 상점이 더 빨리 옵니다.",
        "보스 체력이 절반 아래로 내려가면 특수 스킬을 씁니다. 경고 표시를 잘 보세요.",
    };

    static string[] TipPool()
    {
        System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>(CodexUI.LoadingTips());
        list.AddRange(ExtraTips);
        return list.ToArray();
    }
}
