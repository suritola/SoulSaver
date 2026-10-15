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
    Vector3 baseScale;

    void Start()
    {
        image = GetComponent<Image>();
        if (image != null) baseColor = image.color;
        baseScale = transform.localScale;
        outline = gameObject.AddComponent<Outline>();
        outline.useGraphicAlpha = false;

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
        pill.sprite = null;
        pill.color = Gold;
        pill.raycastTarget = false;
        badge.transform.SetAsLastSibling();
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
        transform.localScale = baseScale * (1f + 0.04f * pulse);
    }
}
