using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

// UI 전면 개편 새 부품 (docs/art/UI-개편-기획서.md 8절 · UiRules.Legacy = false 일 때만 씀) — 2026-10-22~ P0-0d 확인 창 시제품부터
//   그림은 9칸 나눔 + Tiled (돌결이 늘어나지 않게) · 상태는 색 곱하기 대신 그림 바꾸기(SpriteSwap) · 크기 변화 없음
//   글자는 UIKit.Text 를 그대로 써서 번역 · 언어 바꾸기(Remember)를 따름 · 굵게 없음
public static class UiV2
{
    public enum Kind { Primary, Secondary, Danger }

    static string KindName(Kind k) => k == Kind.Primary ? "primary" : k == Kind.Danger ? "danger" : "secondary";

    // 그림 판 하나 (그림이 없으면 토큰 색 단색 — 화면이 비지 않게)
    public static Image Slab(RectTransform r, string sprite, Color fallback)
    {
        Image img = r.gameObject.AddComponent<Image>();
        Sprite s = UiRules.Sprite(sprite);
        if (s != null) { img.sprite = s; img.type = Image.Type.Tiled; img.pixelsPerUnitMultiplier = 1f; img.color = Color.white; }
        else img.color = fallback;
        return img;
    }

    // 막 + 가운데 창 (UIKit.Modal 과 같은 자리 · 같은 out blocker) — 창 그림자 · 0.94 → 1 확대 대신 0.14초 페이드 + 2도트 위로
    public static RectTransform Modal(Transform root, string name, Vector2 size, out GameObject blocker)
    {
        UIKit.EnsureStyle();
        Canvas canvas = root.GetComponentInChildren<Canvas>();
        Transform parent = canvas != null ? canvas.transform : root;
        blocker = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform br = blocker.GetComponent<RectTransform>();
        br.SetParent(parent, false);
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
        br.offsetMin = br.offsetMax = Vector2.zero;
        br.SetAsLastSibling();
        blocker.GetComponent<Image>().color = UiTokens.Scrim(false);
        RectTransform win = UIKit.Rect("Window", br, Vector2.zero, size);
        Slab(win, "ui2_panel", UiTokens.Panel);
        Sprite rune = UiRules.Sprite("ui2_rune");
        if (rune != null)
        {
            RectTransform rr = UIKit.Rect("Rune", win, Vector2.zero, new Vector2(27f, 27f));
            rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(0f, 1f);
            rr.anchoredPosition = new Vector2(12f, -12f);
            Image ri = rr.gameObject.AddComponent<Image>();
            ri.sprite = rune;
            ri.raycastTarget = false;
        }
        blocker.AddComponent<UiAppear>().target = win;
        return win;
    }

    // 버튼 (주 · 보조 · 위험) — 기본/올림(=선택)/누름/비활성 그림 · 누르면 글자 1도트 아래 · 올림/선택이면 포커스 괄호
    public static Button Btn(Transform parent, string ko, Vector2 pos, Vector2 size, UnityAction onClick, Kind kind = Kind.Secondary, float fontSize = UiTokens.Body)
    {
        string k = KindName(kind);
        RectTransform r = UIKit.Rect("Button", parent, pos, size);
        Image img = Slab(r, "ui2_btn_" + k + "_n", kind == Kind.Danger ? UiTokens.DangerDeep : kind == Kind.Primary ? UiTokens.AccentDeep : UiTokens.Raised);
        Button b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        if (img.sprite != null)
        {
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState
            {
                highlightedSprite = UiRules.Sprite("ui2_btn_" + k + "_h"),
                selectedSprite = UiRules.Sprite("ui2_btn_" + k + "_h"),
                pressedSprite = UiRules.Sprite("ui2_btn_" + k + "_p"),
                disabledSprite = UiRules.Sprite("ui2_btn_" + k + "_d"),
            };
        }
        if (onClick != null) b.onClick.AddListener(onClick);
        TMP_Text t = UIKit.Text(r, ko, fontSize, kind == Kind.Danger ? UiTokens.Danger : UiTokens.Text1, Vector2.zero, size - new Vector2(Mathf.Min(UiTokens.S4 * 2f, size.x * 0.2f), UiTokens.S2));
        t.fontSizeMin = Mathf.Min(fontSize, UiTokens.MinSize(fontSize));
        UiFocusMark m = r.gameObject.AddComponent<UiFocusMark>();
        m.label = t.rectTransform;
        return b;
    }

    // 장면 · 복제 버튼을 새 모습으로 (P0-4a ESC 부터): 그림 바꾸기 · 굵게 · 그라데이션 · 그림자 · 자간 걷기 · 포커스 괄호 — MenuItemFx 는 붙이지 않음
    public static void Restyle(Button b, Kind kind, float size)
    {
        if (b == null) return;
        string k = KindName(kind);
        Image img = b.GetComponent<Image>();
        if (img == null) img = b.gameObject.AddComponent<Image>();
        Sprite n = UiRules.Sprite("ui2_btn_" + k + "_n");
        if (n != null)
        {
            img.sprite = n; img.type = Image.Type.Tiled; img.pixelsPerUnitMultiplier = 1f; img.color = Color.white;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState
            {
                highlightedSprite = UiRules.Sprite("ui2_btn_" + k + "_h"),
                selectedSprite = UiRules.Sprite("ui2_btn_" + k + "_h"),
                pressedSprite = UiRules.Sprite("ui2_btn_" + k + "_p"),
                disabledSprite = UiRules.Sprite("ui2_btn_" + k + "_d"),
            };
        }
        b.targetGraphic = img;
        foreach (Shadow s in b.GetComponentsInChildren<Shadow>(true)) Object.Destroy(s);       // Outline 도 Shadow 를 이음
        TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
        if (t != null)
        {
            t.fontStyle &= ~FontStyles.Bold;
            t.enableVertexGradient = false;
            t.characterSpacing = 0f;
            if (UIKit.FontMaterial != null && t.font == UIKit.Font) t.fontSharedMaterial = UIKit.FontMaterial;     // 외곽선 재질 대신 원본 (다른 글꼴이면 재질이 맞지 않아 그대로)
            t.color = kind == Kind.Danger ? UiTokens.Danger : UiTokens.Text1;
            t.enableAutoSizing = true;
            t.fontSize = t.fontSizeMax = size;
            t.fontSizeMin = UiTokens.MinSize(size);
        }
        if (b.GetComponent<UiFocusMark>() == null) b.gameObject.AddComponent<UiFocusMark>().label = t != null ? t.rectTransform : null;
    }

    // 버튼 글자를 번역 키로 바꿔 닮 (언어를 바꾸면 따라감 · UIKit.Remember) — 짧은 버튼 이름 (LocTable.UiButtons)
    public static void Label(Button b, string key)
    {
        if (b == null) return;
        TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
        if (t == null) return;
        t.text = Loc.T(key);
        UIKit.Remember(t, key);
    }

    // 1도트 구분선 (높이 3 · 그림이 없으면 단색)
    public static Image Line(Transform parent, string name, Vector2 pos, float width, bool accent = false)
    {
        RectTransform r = UIKit.Rect(name, parent, pos, new Vector2(width, UiRules.Dot));
        Image img = Slab(r, accent ? "ui2_line_accent" : "ui2_line", accent ? UiTokens.Accent : UiTokens.LineDim);
        img.raycastTarget = false;
        return img;
    }

    // 확인 창 아래 키 안내 (패드면 숨김)
    public static void KeyHint(RectTransform win, string ko, float y)
    {
        if (GamePad.Active) return;
        UIKit.Text(win, ko, UiTokens.Small, UiTokens.Text3, new Vector2(0f, y), new Vector2(win.sizeDelta.x - UiTokens.WindowPad * 2f, 28f), TextAlignmentOptions.Left);
    }
}

// 창 등장: 막 · 창이 0.14초에 걸쳐 나타나며 창은 2도트(6) 아래에서 제자리로 — 실제 시간 · 사라지거나 꺼지면 바로 제자리 (남는 상태 없음)
public class UiAppear : MonoBehaviour
{
    public RectTransform target;
    CanvasGroup group;
    Vector2 home;
    float t;
    bool started;

    void Awake()
    {
        group = gameObject.GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        if (!GameSettings.ReduceMotion) group.alpha = 0f;      // 첫 프레임에 한 번 다 보였다가 사라지지 않게
    }

    void Start()
    {
        started = true;
        if (target != null) home = target.anchoredPosition;
        if (GameSettings.ReduceMotion) { t = UiTokens.Base; Apply(1f); enabled = false; return; }
        Apply(0f);
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(t / UiTokens.Base);
        Apply(1f - (1f - k) * (1f - k));            // ease-out
        if (k >= 1f) enabled = false;
    }

    void OnDisable() { if (started) Apply(1f); else if (group != null) group.alpha = 1f; }      // 시작 전이면 자리(home 미정)는 건드리지 않음

    void Apply(float k)
    {
        if (group != null) group.alpha = k;
        if (target != null) target.anchoredPosition = home + new Vector2(0f, -UiTokens.S1 * (1f - k));
    }
}

// 포커스 괄호 + 누름 1도트 — 마우스를 올리면 그 버튼을 선택해(패드가 아닐 때) 「올림 + 선택」 두 개가 함께 강조되지 않게
public class UiFocusMark : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform label;
    public bool selectOnHover = true;       // Space · Enter 를 직접 처리하는 창(고르기 창 등)의 버튼은 끔 — 지나간 버튼이 Submit 을 가로채지 않게
    GameObject mark;
    bool hover, selected;
    Vector2 labelHome;

    void Awake()
    {
        if (label != null) labelHome = label.anchoredPosition;
    }

    void Start()
    {
        if (label != null) labelHome = label.anchoredPosition;
        Sprite s = UiRules.Sprite("ui2_focus");
        if (s == null) return;
        RectTransform rt = GetComponent<RectTransform>();
        RectTransform m = UIKit.Rect("Focus", rt, Vector2.zero, Vector2.zero);
        m.anchorMin = Vector2.zero; m.anchorMax = Vector2.one;          // 버튼 크기를 따라감 (늘어나는 앵커 버튼도 · 바깥 2도트)
        m.offsetMin = new Vector2(-UiTokens.S1, -UiTokens.S1); m.offsetMax = new Vector2(UiTokens.S1, UiTokens.S1);
        Image mi = m.gameObject.AddComponent<Image>();
        mi.sprite = s;
        mi.type = Image.Type.Sliced;
        mi.pixelsPerUnitMultiplier = 1f;
        mi.raycastTarget = false;                   // 패드 가상 커서 · 클릭이 괄호에 막히지 않게
        mark = m.gameObject;
        Refresh();
    }

    void Refresh()
    {
        if (mark != null) mark.SetActive(hover || selected);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hover = true;
        // 창이 가만히 있는 커서 밑에 떴을 때(움직임 0)는 선택하지 않음 — 위험 확인 창의 기본 선택(취소)이 무너져 Enter 한 번에 판이 날아갈 수 있었음 (검토)
        EventSystem es = EventSystem.current;
        GameObject cur = es != null ? es.currentSelectedGameObject : null;
        bool typing = cur != null && cur.GetComponent<TMP_InputField>() != null;
        if (selectOnHover && !GamePad.Active && es != null && cur != gameObject && !typing && e != null && e.delta.sqrMagnitude > 0f)
        {
            Selectable sel = GetComponent<Selectable>();
            if (sel != null && sel.IsInteractable()) es.SetSelectedGameObject(gameObject);
        }
        Refresh();
    }

    public void OnPointerExit(PointerEventData e) { hover = false; Nudge(false); Refresh(); }
    public void OnSelect(BaseEventData e) { selected = true; Refresh(); }
    public void OnDeselect(BaseEventData e) { selected = false; Refresh(); }
    public void OnPointerDown(PointerEventData e) => Nudge(true);
    public void OnPointerUp(PointerEventData e) => Nudge(false);

    void Nudge(bool down)
    {
        if (label != null) label.anchoredPosition = labelHome + (down ? new Vector2(0f, -UiTokens.S1 * 0.5f) : Vector2.zero);
    }

    void OnEnable()
    {
        selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;     // 창을 다시 열면 남아 있는 선택에도 괄호
        Refresh();
    }

    void OnDisable() { hover = false; selected = false; Nudge(false); Refresh(); }
}
