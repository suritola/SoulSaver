using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 마우스를 올린 UI 옆에 뜨는 설명창 (씬에 하나)
public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [Header("모양")]
    public Sprite panelSprite;
    public TMP_FontAsset font;
    public Material fontMaterial;
    public float width = 440f;
    public float padding = 36f;
    public Color tint = Color.white;     // 코드로 만든 설명창(메인 메뉴)은 흰 버튼 그림을 어둡게 칠함

    RectTransform panel;
    RectTransform canvasRect;
    TextMeshProUGUI titleText;
    TextMeshProUGUI bodyText;

    void Awake()
    {
        Instance = this;
        canvasRect = GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();
        Build();
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (panel != null) Destroy(panel.gameObject);
    }

    void OnDisable() => Hide();

    void Build()
    {
        GameObject go = new GameObject("TooltipPanel", typeof(RectTransform), typeof(Image));
        panel = go.GetComponent<RectTransform>();
        // 최상위 캔버스 바로 아래 + 자기 그리기 순서: 전체 화면 창(영혼 트리 등)보다 항상 위에 그려지게
        panel.SetParent(canvasRect, false);
        Canvas top = go.AddComponent<Canvas>();
        top.overrideSorting = true;
        top.sortingOrder = 1000;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);

        Image bg = go.GetComponent<Image>();
        bg.sprite = panelSprite;
        bg.color = tint;
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;

        titleText = MakeText("Title", 32f, new Color(0.96f, 0.83f, 0.47f));
        bodyText = MakeText("Body", 26f, new Color(0.92f, 0.88f, 0.80f));
    }

    TextMeshProUGUI MakeText(string name, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(panel, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        if (fontMaterial != null) text.fontSharedMaterial = fontMaterial;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    // 1.0.1: 메인 메뉴 씬에는 설명창이 없어 도감(스킬 진화 등)에 마우스를 올려도 아무것도 안 떴음 → 없으면 그 캔버스에 만듦
    public static void Ensure(Transform from)
    {
        if (Instance != null || from == null) return;
        Canvas c = from.GetComponentInParent<Canvas>();
        if (c == null) return;
        UIKit.EnsureStyle();
        GameObject go = new GameObject("TooltipUI", typeof(RectTransform));
        go.SetActive(false);
        go.transform.SetParent(c.rootCanvas.transform, false);
        TooltipUI t = go.AddComponent<TooltipUI>();
        t.panelSprite = UIKit.ButtonSprite;
        t.font = UIKit.Font;
        t.fontMaterial = UIKit.FontMaterial;
        t.tint = new Color(0.12f, 0.1f, 0.17f, 0.97f);
        go.SetActive(true);
    }

    public static void Show(string title, string body)
    {
        // 씬에 적힌 툴팁 문장도 현재 언어로
        if (Instance != null) Instance.ShowInternal(Loc.T(title), Loc.T(body));
    }

    public static void Hide()
    {
        if (Instance != null && Instance.panel != null) Instance.panel.gameObject.SetActive(false);
    }

    void ShowInternal(string title, string body)
    {
        float inner = width - padding * 2f;

        titleText.text = title;
        bodyText.text = body;
        float titleH = titleText.GetPreferredValues(title, inner, 0f).y;
        float bodyH = string.IsNullOrEmpty(body) ? 0f : bodyText.GetPreferredValues(body, inner, 0f).y;
        float gap = bodyH > 0f ? 10f : 0f;

        titleText.rectTransform.sizeDelta = new Vector2(inner, titleH);
        titleText.rectTransform.anchoredPosition = new Vector2(padding, -padding);
        bodyText.rectTransform.sizeDelta = new Vector2(inner, bodyH);
        bodyText.rectTransform.anchoredPosition = new Vector2(padding, -padding - titleH - gap);

        panel.sizeDelta = new Vector2(width, padding * 2f + titleH + gap + bodyH);
        panel.gameObject.SetActive(true);
        panel.SetAsLastSibling();
        Canvas top = panel.GetComponent<Canvas>();
        if (top != null) { top.overrideSorting = true; top.sortingOrder = 1000; }
        Follow();
    }

    void LateUpdate()
    {
        if (panel != null && panel.gameObject.activeSelf) Follow();
    }

    // 마우스 오른쪽 아래에 띄우고, 화면 밖으로 나가면 반대쪽으로 뒤집음
    void Follow()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out Vector2 local);
        Vector2 half = canvasRect.rect.size * 0.5f;
        Vector2 size = panel.sizeDelta;

        bool left = local.x + 24f + size.x > half.x;
        bool up = local.y - 24f - size.y < -half.y;
        panel.pivot = new Vector2(left ? 1f : 0f, up ? 0f : 1f);

        Vector2 pos = local + new Vector2(left ? -24f : 24f, up ? 24f : -24f);
        panel.anchoredPosition = pos;
    }
}
