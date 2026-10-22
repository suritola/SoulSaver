using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 메뉴 버튼을 반짝반짝 강조 (2.1.9~, 아직 해 보지 않은 새 플레이 튜토리얼)
// 금빛 테두리가 숨 쉬듯 굵어졌다 가늘어지고, 버튼이 살짝 커졌다 작아지며, 오른쪽 위에 「NEW」 배지
public class NewGlow : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.84f, 0.35f);
    Outline outline;
    Image image;
    Color baseColor;
    TMP_Text badge;
    Image pillImage;
    Vector3 baseScale;
    bool started;

    // 1.2~ 반짝임 · NEW 배지를 걷고 버튼을 원래 색 · 크기로 (도감처럼 창을 닫았을 때 더 볼 것이 없으면)
    public void Remove()
    {
        if (started)
        {
            if (image != null) image.color = baseColor;
            transform.localScale = baseScale;
            if (outline != null) Destroy(outline);
            if (badge != null) Destroy(badge.gameObject);
            if (pillImage != null) Destroy(pillImage.gameObject);
        }
        Destroy(this);
    }

    void Start()
    {
        image = GetComponent<Image>();
        if (image != null) baseColor = image.color;
        baseScale = transform.localScale;
        if (!UiRules.Legacy && UiRules.Sprite("ui2_dot") != null)
        {
            // UI 개편 P0-4b: 숨 쉬는 금 테 · 커졌다 작아짐 · NEW 알약 대신 오른쪽 위 청록 점 하나 (맥동 없음)
            image = null;
            Image dot = new GameObject("NewDot", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            dot.rectTransform.SetParent(transform, false);
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(1f, 1f);
            dot.rectTransform.anchoredPosition = new Vector2(-18f, -18f);
            dot.rectTransform.sizeDelta = new Vector2(12f, 12f);
            dot.sprite = UiRules.Sprite("ui2_dot");
            dot.raycastTarget = false;
            pillImage = dot;
            started = true;
            return;
        }
        // 1.4.8~ 글자 메뉴 버튼(MenuItemFx)은 상자가 없으므로 테두리 · 색 · 크기 맥박 없이 배지만
        bool textItem = GetComponent<MenuItemFx>() != null;
        if (textItem) image = null;
        else
        {
            outline = gameObject.AddComponent<Outline>();
            outline.useGraphicAlpha = false;
        }

        UIKit.EnsureStyle();
        badge = UIKit.Text(transform, "NEW", 22f, new Color(0.15f, 0.08f, 0f), Vector2.zero, new Vector2(64f, 28f));
        UIKit.Forget(badge);
        badge.text = "NEW";
        RectTransform br = badge.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(1f, 1f);
        br.anchoredPosition = new Vector2(-30f, -14f);
        Image pill = new GameObject("NewPill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        pill.rectTransform.SetParent(transform, false);
        pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(1f, 1f);
        pill.rectTransform.anchoredPosition = br.anchoredPosition;
        pill.rectTransform.sizeDelta = new Vector2(64f, 28f);
        // 1.1~ 버튼 스프라이트는 가운데가 어두운 보라라 어두운 글자가 안 보였음 → 밝은 금색 단색 판
        // 1.4.8~ 새 도트 스타일: 금장 테두리 돌판(ui_button · 작아서 테두리 반 두께) + 금빛 글자 · 그림이 없으면 예전 금색 단색 판 + 어두운 글자
        if (RecommendGlow.PillArt)
        {
            pill.sprite = UIKit.ButtonSprite;
            pill.type = Image.Type.Sliced;
            pill.pixelsPerUnitMultiplier = 2f;
            pill.color = Color.white;
            badge.color = Gold;
        }
        else
        {
            pill.sprite = null;
            pill.color = Gold;
        }
        pill.raycastTarget = false;
        pillImage = pill;
        badge.transform.SetAsLastSibling();
        started = true;
    }

    void Update()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.5f);
        if (outline != null)
        {
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.45f + 0.55f * pulse);
            outline.effectDistance = Vector2.one * (2f + 4f * pulse);
        }
        if (image != null) image.color = Color.Lerp(baseColor, new Color(1f, 0.9f, 0.6f), 0.35f * pulse);
        if (outline != null) transform.localScale = baseScale * (1f + 0.04f * pulse);
    }
}
