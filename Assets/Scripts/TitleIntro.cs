using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 게임을 켤 때 한 번: 게임 타이틀로 만든 시작 연출 (2026-10-23~ 사용자 「Made with Unity 를 없애고 내 게임 타이틀로 시작 효과를」)
//   거의 검은 바탕 → 청록 영혼불이 떠오름 → SOUL SAVER 가 넓은 자간에서 모이며 나타남 → 아래 청록 1도트 선이 펼쳐짐 → 잠깐 뒤 어두워지며 메인 메뉴
//   아무 키 · 클릭으로 바로 넘김 · 메인 메뉴로 돌아올 때는 다시 안 나옴 · 자동 봇 · 배치 · 트레일러는 건너뜀
//   막(정렬 32000 아래 · 31500)이 메뉴 누름을 막으므로 끝나거나 넘기면 반드시 지움 (남는 상태 없음 — 1.2.7 교훈: 상한 6초)
public class TitleIntro : MonoBehaviour
{
    static bool played;
    const float Total = 2.8f, FadeOut = 0.5f, MaxLife = 6f;

    CanvasGroup group;
    RectTransform flame, line;
    TMP_Text title;
    float t, outAt = -1f;

    // 1.5.9~ 연출을 넘긴 ESC · Enter · Space · 패드 B 가 뒤에 이미 떠 있는 언어 고르기 · 저장 알림 · 패치노트 창까지 누르던 것:
    //   떠 있는 동안 Showing → ModalEsc · 패치노트 EscCloser 가 ESC 를 넘김, EventSystem 의 Submit · 방향키도 꺼 둠
    //   (MaxLife 6초로 반드시 지워지고 OnDestroy 에서 되돌리므로 남는 상태 없음)
    static TitleIntro live;
    public static bool Showing => live != null;
    UnityEngine.EventSystems.EventSystem navEs;
    bool navWas = true;

    public static void PlayOnce()
    {
        if (played) return;
        played = true;
        if (GameInput.Auto || Application.isBatchMode || GameInput.TrailerRunning) return;
        UIKit.EnsureStyle();
        GameObject go = new GameObject("TitleIntro", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 31500;                 // 메뉴 · 언어 고르기 창 위 · 씬 전환 막(32000) 아래
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        TitleIntro a = go.AddComponent<TitleIntro>();
        a.Build((RectTransform)go.transform);
    }

    void Build(RectTransform root)
    {
        group = gameObject.AddComponent<CanvasGroup>();
        RectTransform bg = UIKit.Rect("Back", root, Vector2.zero, Vector2.zero);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = bg.offsetMax = Vector2.zero;
        Image bi = bg.gameObject.AddComponent<Image>();
        bi.color = UiTokens.Void;               // 누름도 막음 (연출 중 메뉴가 눌리지 않게)

        flame = UIKit.Rect("Flame", root, new Vector2(0f, 120f), new Vector2(30f, 42f));     // 5x7 도트 × 2 (정수 배)
        Image fi = flame.gameObject.AddComponent<Image>();
        fi.sprite = UiRules.Sprite("ui2_marker");
        fi.raycastTarget = false;
        if (fi.sprite == null) fi.color = UiTokens.Accent;

        title = UIKit.Text(root, "", 120f, UiTokens.Text1, new Vector2(0f, 0f), new Vector2(1600f, 160f));
        title.text = "SOUL SAVER";
        title.enableAutoSizing = false;
        title.fontSize = 120f;
        UIKit.Forget(title);

        line = UIKit.Rect("Line", root, new Vector2(0f, -84f), new Vector2(0f, UiRules.Dot));
        Image li = line.gameObject.AddComponent<Image>();
        li.color = UiTokens.Accent;
        li.raycastTarget = false;
        live = this;
        navEs = UnityEngine.EventSystems.EventSystem.current;
        if (navEs != null) { navWas = navEs.sendNavigationEvents; navEs.sendNavigationEvents = false; }
        Apply();
    }

    void OnDestroy()
    {
        if (live == this) live = null;
        if (navEs != null) navEs.sendNavigationEvents = navWas;
    }

    // 1.6.1~ 게임을 켜면 이제 Boot 로딩 화면(SceneFlow) 아래서 메인 메뉴가 켜지므로, 로딩 화면이 덮고 있는 동안은 연출 시간을 멈춤
    //   (안 멈추면 연출 앞 1초가 가려진 채 지나감) · 그래도 남지 않게 실제로 떠 있던 시간 상한 MaxWait
    const float MaxWait = 30f;
    float life;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        life += dt;
        if (life >= MaxWait) { Destroy(gameObject); return; }
        if (SceneFlow.Loading) return;
        t += Mathf.Min(dt, 0.1f);
        // 넘기기: 아무 키 · 클릭 · 패드 (0.25 초 뒤부터 — 켜자마자 남은 입력으로 넘어가지 않게)
        if (outAt < 0f && t > 0.25f && (Input.anyKeyDown || Input.GetMouseButtonDown(0))) outAt = t;
        if (outAt < 0f && t >= Total - FadeOut) outAt = t;
        if ((outAt >= 0f && t - outAt >= FadeOut) || t >= MaxLife) { Destroy(gameObject); return; }
        Apply();
    }

    static float Ease(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }

    void Apply()
    {
        bool still = GameSettings.ReduceMotion;
        // 영혼불: 0 ~ 0.5 초에 아래에서 떠오르며 나타남
        float f = Ease(t / 0.5f);
        flame.anchoredPosition = new Vector2(0f, 120f - (still ? 0f : 18f * (1f - f)));
        flame.GetComponent<Image>().color = new Color(1f, 1f, 1f, f);
        // 제목: 0.35 ~ 1.1 초에 자간 40 → 8 로 모이며 나타남
        float k = Ease((t - 0.35f) / 0.75f);
        title.characterSpacing = still ? 8f : Mathf.Lerp(40f, 8f, k);
        title.alpha = k;
        // 선: 0.9 ~ 1.4 초에 양옆으로 펼쳐짐 (폭 0 → 560)
        float w = Ease((t - 0.9f) / 0.5f);
        line.sizeDelta = new Vector2(Mathf.Round(560f * w / UiRules.Dot) * UiRules.Dot, UiRules.Dot);
        // 끝: 막 전체가 0.5 초에 걸쳐 사라짐
        group.alpha = outAt < 0f ? 1f : 1f - Mathf.Clamp01((t - outAt) / FadeOut);
        group.blocksRaycasts = group.alpha > 0.05f;
    }
}
