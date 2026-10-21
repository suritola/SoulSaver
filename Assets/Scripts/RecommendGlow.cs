using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 레벨업 창의 추천 카드 (2.1.6~, LevelShop.ShowRecommend): 진화에 가장 가까워지는 카드 하나를 반짝반짝 빛나게
//   카드 뒤: 숨 쉬듯 커졌다 작아지는 금빛 후광 + 색이 금 → 흰 → 보라로 도는 굵은 테두리
//   카드 위: 테두리를 따라 튀는 별빛 · 「★ 추천」 (고르면 바로 진화하면 「추천 · 바로 진화!」)
// 레벨업 창은 시간이 멈춘 채라 unscaledTime 으로 움직임 · 판정이나 클릭은 막지 않음 (raycastTarget 끔)
public class RecommendGlow : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.84f, 0.35f);
    static readonly Color Violet = new Color(0.82f, 0.55f, 1f);
    const int SparkCount = 10;
    const float Pad = 14f;          // 테두리가 카드 밖으로 나오는 두께

    RectTransform target;
    RectTransform halo, rim, sparkRoot;
    Image haloImg, rimImg;
    TextMeshProUGUI badge;
    Image pill;
    readonly Image[] sparks = new Image[SparkCount];
    readonly float[] sparkStart = new float[SparkCount];
    readonly float[] sparkLife = new float[SparkCount];

    public void Show(RectTransform card, bool evolvesNow)
    {
        target = card;
        bool on = card != null && card.parent != null;
        if (!on) { SetActive(false); return; }
        Build(card);
        badge.text = "★ " + Loc.T(evolvesNow ? "추천 · 바로 진화!" : "추천");
        // 카드 뒤에 그리려고 버튼(카드)들 가운데 가장 앞 형제 자리로 (옆 카드를 덮지 않게 · 배치는 ignoreLayout 으로 그대로)
        halo.SetAsLastSibling();
        rim.SetAsLastSibling();
        int at = card.GetSiblingIndex();
        foreach (Transform c in card.parent)
            if (c != halo && c != rim && c.GetComponent<Button>() != null) at = Mathf.Min(at, c.GetSiblingIndex());
        halo.SetSiblingIndex(at);
        rim.SetSiblingIndex(at + 1);
        sparkRoot.SetAsLastSibling();
        pill.transform.SetAsLastSibling();
        for (int i = 0; i < SparkCount; i++) sparkStart[i] = Time.unscaledTime - Random.value * 0.8f;
        SetActive(true);
        Follow();
    }

    void OnDisable() => SetActive(false);

    void SetActive(bool on)
    {
        if (halo != null) halo.gameObject.SetActive(on);
        if (rim != null) rim.gameObject.SetActive(on);
        if (sparkRoot != null) sparkRoot.gameObject.SetActive(on);
    }

    void Build(RectTransform card)
    {
        Transform parent = card.parent;
        if (halo == null || halo.parent != parent)
        {
            if (halo != null) Destroy(halo.gameObject);
            if (rim != null) Destroy(rim.gameObject);
            haloImg = MakeImage("RecommendHalo", parent, SpecialAbilities.GlowSprite != null ? SpecialAbilities.GlowSprite : UIKit.ButtonSprite, false);
            halo = haloImg.rectTransform;
            UIKit.EnsureStyle();
            rimImg = MakeImage("RecommendRim", parent, UIKit.ButtonSprite, true);
            rim = rimImg.rectTransform;
        }
        if (sparkRoot == null || sparkRoot.parent != card)
        {
            if (sparkRoot != null) Destroy(sparkRoot.gameObject);
            GameObject go = new GameObject("RecommendSparks", typeof(RectTransform));
            sparkRoot = go.GetComponent<RectTransform>();
            sparkRoot.SetParent(card, false);
            sparkRoot.anchorMin = Vector2.zero;
            sparkRoot.anchorMax = Vector2.one;
            sparkRoot.offsetMin = sparkRoot.offsetMax = Vector2.zero;
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            Sprite star = SpecialAbilities.GlowSprite != null ? SpecialAbilities.GlowSprite : UIKit.ButtonSprite;
            for (int i = 0; i < SparkCount; i++)
            {
                sparks[i] = MakeImage("Spark", sparkRoot, star, false);
                sparks[i].rectTransform.anchorMin = sparks[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            }
            // 「★ 추천」: 카드 아래 테두리에 걸친 알약 (카드 아래의 안내 문구 · 건너뛰기 버튼과 겹치지 않게)
            pill = MakeImage("RecommendPill", sparkRoot, UIKit.ButtonSprite, true);
            RectTransform pr = pill.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.anchoredPosition = new Vector2(0f, 10f);     // 1.1~ 카드 아래 테두리보다 10 위 (아래 확정 안내 줄과 띄움)
            pr.sizeDelta = new Vector2(360f, 58f);       // 1.0.1: 300×44 → 360×58 (더 잘 보이게)
            badge = new GameObject("RecommendBadge", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            RectTransform br = badge.rectTransform;
            br.SetParent(pr, false);
            br.anchorMin = Vector2.zero;
            br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(8f, 2f);
            br.offsetMax = new Vector2(-8f, -2f);
            badge.enableAutoSizing = true;
            badge.fontSizeMin = 18f;
            badge.fontSizeMax = 34f;
            badge.fontStyle = FontStyles.Bold;
            if (UIKit.Font != null) badge.font = UIKit.Font;
            if (UIKit.FontMaterial != null) badge.fontSharedMaterial = UIKit.FontMaterial;
            badge.alignment = TextAlignmentOptions.Center;
            badge.enableWordWrapping = false;
            // 1.4.8~ 새 도트 스타일(금장 돌판 + 금 글자)이면 외곽선 없이 (외곽선은 글자 재질 복사본을 만듦) · 옛 금색 단색 알약이면 예전처럼
            if (!PillArt)
            {
                badge.outlineWidth = 0.12f;
                badge.outlineColor = new Color32(255, 240, 200, 255);
            }
            badge.raycastTarget = false;
        }
    }

    // 1.4.8~ 버튼 그림이 새 도트 판(ui_button)인지 — 아니면(코드로 그린 평평한 칸 · 장면 버튼 그림) 예전 색
    //   매 프레임 불려서(숨 쉬기) 그림이 바뀔 때만 이름을 봄 (name 은 부를 때마다 문자열을 만듦)
    static Sprite pillArtFor;
    static bool pillArt;
    internal static bool PillArt
    {
        get
        {
            Sprite s = UIKit.ButtonSprite;
            if (!ReferenceEquals(s, pillArtFor)) { pillArtFor = s; pillArt = s != null && s.name == "ui_button"; }
            return pillArt;
        }
    }

    static Image MakeImage(string name, Transform parent, Sprite sprite, bool sliced)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.raycastTarget = false;
        return img;
    }

    void LateUpdate()
    {
        if (target == null || halo == null || !halo.gameObject.activeSelf) return;
        if (!target.gameObject.activeInHierarchy) { SetActive(false); return; }
        Follow();
    }

    // 카드 자리 · 크기 · 확대(마우스 올림)를 따라가며 빛남
    void Follow()
    {
        float t = Time.unscaledTime;
        Vector2 size = Vector2.Scale(target.rect.size, (Vector2)target.localScale);
        Vector3 center = target.TransformPoint(target.rect.center);
        float breathe = (0.5f + 0.5f * Mathf.Sin(t * 4.2f)) * GameSettings.Motion;      // 1.1.9~ 「움직임 줄이기」

        halo.anchorMin = halo.anchorMax = target.anchorMin == target.anchorMax ? target.anchorMin : new Vector2(0.5f, 0.5f);
        halo.position = center;
        halo.sizeDelta = size * (1.35f + 0.1f * breathe);
        haloImg.color = new Color(Gold.r, Gold.g, Gold.b, 0.45f + 0.35f * breathe);

        rim.anchorMin = rim.anchorMax = halo.anchorMin;
        rim.position = center;
        rim.sizeDelta = size + Vector2.one * (Pad * 2f + 4f * breathe);
        float k = Mathf.Repeat(t * 0.6f, 1f) * 3f;      // 금 → 흰 → 보라 → 금
        Color c = k < 1f ? Color.Lerp(Gold, Color.white, k) : k < 2f ? Color.Lerp(Color.white, Violet, k - 1f) : Color.Lerp(Violet, Gold, k - 2f);
        rimImg.color = new Color(c.r, c.g, c.b, 0.9f);

        // 1.0.1: 어두운 보라 알약 + 금빛 글씨 → 밝은 금빛 알약 + 진한 갈색 글씨 (대비가 훨씬 큼)
        // 1.4.8~ 새 도트 스타일: 금장 테두리 돌판(ui_button) + 금빛 글씨 · 판은 숨 쉴 때 살짝 금빛으로 (그림이 없으면 예전 금색 알약)
        if (PillArt)
        {
            badge.color = Gold;
            pill.color = Color.Lerp(Color.white, new Color(1f, 0.88f, 0.55f), breathe * 0.6f);
        }
        else
        {
            badge.color = new Color(0.22f, 0.08f, 0f);
            pill.color = Color.Lerp(Gold, new Color(1f, 0.97f, 0.75f), breathe * 0.5f);
        }
        pill.rectTransform.localScale = Vector3.one * (1f + 0.1f * breathe);

        // 별빛: 테두리 위 아무 데서나 솟았다 사라짐
        Vector2 half = target.rect.size * 0.5f;
        for (int i = 0; i < SparkCount; i++)
        {
            float age = t - sparkStart[i];
            if (sparkLife[i] <= 0f || age > sparkLife[i])
            {
                sparkStart[i] = t;
                sparkLife[i] = Random.Range(0.45f, 0.9f);
                age = 0f;
                sparks[i].rectTransform.anchoredPosition = EdgePoint(half) + target.rect.center;
                sparks[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
            }
            float u = age / sparkLife[i];
            float pop = Mathf.Sin(u * Mathf.PI);
            float s = Mathf.Lerp(10f, 46f, pop);
            sparks[i].rectTransform.sizeDelta = new Vector2(s, s);
            Color sc = i % 3 == 0 ? Violet : i % 3 == 1 ? Gold : Color.white;
            sparks[i].color = new Color(sc.r, sc.g, sc.b, pop);
        }
    }

    static Vector2 EdgePoint(Vector2 half)
    {
        float perim = 4f * (half.x + half.y);
        float d = Random.value * perim;
        if (d < 2f * half.x) return new Vector2(-half.x + d, half.y);
        d -= 2f * half.x;
        if (d < 2f * half.y) return new Vector2(half.x, half.y - d);
        d -= 2f * half.y;
        if (d < 2f * half.x) return new Vector2(half.x - d, -half.y);
        d -= 2f * half.x;
        return new Vector2(-half.x, -half.y + d);
    }
}

// 플레이에 도움 되는 작은 안내 배지 (1.0.1): 다시 뽑기 · 시너지처럼 글씨만 있던 안내가 배경에 묻혀 잘 안 보였음
//   진한 배경 알약 + 밝은 테두리 + 굵고 검은 외곽선 글씨, 숨 쉬듯 두근거리며 테두리가 반짝임 (멈춘 화면이라 unscaledTime)
public class HintBadge : MonoBehaviour
{
    Image rim;
    Color rimColor;
    float phase;

    // clickable (1.1.9~): 배지를 누를 수 있게 Button 을 붙임 — 꺼낼 때는 t.transform.parent.GetComponent<Button>()
    public static TMP_Text Make(Transform parent, string name, Vector2 pos, Vector2 size, Color fill, Color rimColor, float fontSize, bool clickable = false)
    {
        UIKit.EnsureStyle();
        RectTransform r = UIKit.Rect(name, parent, pos, size);
        r.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        HintBadge hb = r.gameObject.AddComponent<HintBadge>();
        // 1.4.8~ 새 도트 스타일: 금장 테두리 돌판 한 장 · 상태 색(rimColor)은 판에 살짝만 곱함 (두근거릴 때 흰빛으로)
        //   그림이 없으면 예전처럼 밝은 테두리 판 위에 진한 판
        bool art = RecommendGlow.PillArt;
        hb.rimColor = art ? Color.Lerp(Color.white, rimColor, 0.3f) : rimColor;
        hb.phase = Random.value * 6f;
        hb.rim = r.gameObject.AddComponent<Image>();
        hb.rim.sprite = UIKit.ButtonSprite;
        hb.rim.type = Image.Type.Sliced;
        hb.rim.color = hb.rimColor;
        hb.rim.raycastTarget = false;
        if (!art)
        {
            // 테두리: 조금 큰 밝은 판 위에 진한 판
            RectTransform inner = UIKit.Rect("Fill", r, Vector2.zero, Vector2.zero);
            inner.anchorMin = Vector2.zero;
            inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(4f, 4f);
            inner.offsetMax = new Vector2(-4f, -4f);
            Image fi = inner.gameObject.AddComponent<Image>();
            fi.sprite = UIKit.ButtonSprite;
            fi.type = Image.Type.Sliced;
            fi.color = fill;
            fi.raycastTarget = false;
        }
        TMP_Text t = UIKit.Text(r, "", fontSize, Color.white, Vector2.zero, size - new Vector2(20f, 8f));
        t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(0, 0, 0, 255);
        t.enableWordWrapping = false;
        if (clickable)
        {
            hb.rim.raycastTarget = true;
            Button b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = hb.rim;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1f, 0.95f, 0.8f);
            cb.pressedColor = new Color(0.85f, 0.75f, 0.55f);
            cb.selectedColor = Color.white;           // 누른 뒤 선택된 채 어둡게 남지 않게
            b.colors = cb;
            Navigation nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
        }
        return t;
    }

    // 기본 크기는 1 로 고정 (예전엔 켜질 때의 크기를 기억해서, 두근거리던 중에 창이 닫혔다 열릴 때마다 계속 커졌음)
    void Update()
    {
        float b = (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f + phase)) * GameSettings.Motion;
        transform.localScale = Vector3.one * (1f + 0.06f * b);
        if (rim != null) rim.color = Color.Lerp(rimColor, Color.white, 0.55f * b);
    }
}
