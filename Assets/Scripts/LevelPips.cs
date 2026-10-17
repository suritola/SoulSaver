using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.1.2~ 공용 「단계 칸」: 숫자 「0→1/5」 · 「Lv 2 / 3」 대신 최대 단계 수만큼 작은 칸을 가로로 그림
//   찬 칸    = 밝은 크림색
//   차오를 칸 = 금빛으로 깜빡이며 커졌다 작아짐 (이번에 고르면 찰 칸, Set 의 adding)
//   남은 칸  = 어두운 속 + 흐린 테두리만
//   최대     = 칸 전체 금빛 + 가운데 「MAX」 작은 글자
// 칸이 많으면 폭을 줄이고, 그래도 너무 좁으면(칸 폭 < 높이 0.6배) 두 줄로
// 그림은 코드로 만든 둥근 네모 하나(점 필터, 9-슬라이스)를 색만 바꿔 씀 — 폰트 기호(●, ✔ 등)를 쓰지 않아 어떤 언어에서도 깨지지 않음
// 게임이 멈춘 창(레벨업 · 상자)에서도 움직이게 실제 시간(unscaledTime)으로 맥동
//
// 쓰는 법:
//   LevelPips pips = LevelPips.Create(parent, new Vector2(0f, -30f), new Vector2(200f, 14f));
//   pips.Set(2, 5, 1);          // 2칸 참 · 다음 1칸 맥동 · 남은 2칸 빈칸
//   pips.Set(3, 3);             // 최대 → 전부 금빛 + MAX
//   pips.Set(0, 0);             // 최대 단계가 없으면(끝없음) 숨김
public class LevelPips : MonoBehaviour
{
    public static readonly Color Filled = new Color(0.93f, 0.87f, 0.7f);
    public static readonly Color Gold = new Color(1f, 0.8f, 0.27f);
    public static readonly Color GoldBright = new Color(1f, 0.97f, 0.72f);
    public static readonly Color EmptyFill = new Color(0.07f, 0.06f, 0.09f, 0.85f);
    public static readonly Color EmptyRim = new Color(0.42f, 0.38f, 0.33f, 0.9f);
    public static readonly Color FilledRim = new Color(0.12f, 0.09f, 0.06f, 0.95f);

    const int MaxCells = 12;
    const float Gap = 3f;

    RectTransform root;
    Vector2 area;
    readonly Image[] rims = new Image[MaxCells];
    readonly Image[] fills = new Image[MaxCells];
    TextMeshProUGUI maxLabel;
    Image plateRim, plateFill;          // 최대일 때 「MAX」 밑에 까는 금빛 판 (글자가 칸 사이 틈을 가로질러 지저분하던 것)
    int now, max, adding;
    int builtFor = -1;
    // 칸을 정수 픽셀로 맞춘 기준 (루트 캔버스 배율 = 해상도 · UI 크기). 배율이 바뀌면 다시 배치
    Canvas rootCanvas;
    float laidScale = -1f;

    // parent 안 pos(가운데 기준)에 size 크기 칸 줄. 높이(size.y)가 한 칸의 높이 (두 줄이면 위아래로 조금 넘침)
    public static LevelPips Create(RectTransform parent, Vector2 pos, Vector2 size, string name = "LevelPips")
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        LevelPips p = go.AddComponent<LevelPips>();
        p.root = r;
        p.area = size;
        return p;
    }

    // now: 지금 찬 칸 · max: 최대 단계(0 이하면 숨김) · adding: 이번에 차오를 칸 수(맥동)
    public void Set(int now, int max, int adding = 0)
    {
        max = Mathf.Min(max, MaxCells);
        this.max = max;
        this.now = Mathf.Clamp(now, 0, Mathf.Max(0, max));
        this.adding = Mathf.Clamp(adding, 0, Mathf.Max(0, max - this.now));
        gameObject.SetActive(max > 0);
        if (max <= 0) return;
        if (builtFor != max || !Mathf.Approximately(PixelScale(), laidScale)) Layout(max);
        Paint(0f);
    }

    // 캔버스 단위 1 = 화면 몇 픽셀인지 (루트 캔버스 배율: 해상도 · UI 크기 설정)
    float PixelScale()
    {
        if (rootCanvas == null)
        {
            Canvas c = GetComponentInParent<Canvas>(true);
            rootCanvas = c != null ? c.rootCanvas : null;
        }
        return rootCanvas != null ? Mathf.Max(0.05f, rootCanvas.scaleFactor) : 1f;
    }

    public bool IsMax => max > 0 && now >= max;

    // 흐리게 (지금은 고를 수 없는 재료 등)
    public void Dim(bool on)
    {
        CanvasGroup g = GetComponent<CanvasGroup>();
        if (g == null) { if (!on) return; g = gameObject.AddComponent<CanvasGroup>(); g.interactable = false; g.blocksRaycasts = false; }
        g.alpha = on ? 0.4f : 1f;
    }

    // 1.2.3~ 칸 폭 · 간격 · 테두리를 화면 정수 픽셀로 맞춤
    //   예전엔 폭이 소수(HUD 3칸 = 18.67)라 칸마다 픽셀 경계에 걸리는 자리가 달라, 좌우 대칭인 바깥 두 칸은 19px 인데
    //   한가운데(0 기준 ±9.33) 칸만 18px · 테두리 두께도 달라 가운데 칸만 깨져 보였음
    //   → 모든 칸을 같은 정수 픽셀 폭 · 간격으로 두면 칸끼리 픽셀 위상이 같아 모두 똑같이 그려짐
    void Layout(int n)
    {
        builtFor = n;
        float s = PixelScale();
        laidScale = s;
        int H = Mathf.Max(3, Mathf.RoundToInt(area.y * s));
        int areaW = Mathf.Max(3, Mathf.RoundToInt(area.x * s));
        int gap = Mathf.Max(1, Mathf.RoundToInt(Gap * s));
        int rows = 1;
        if ((areaW - (n - 1) * gap) / (float)n < H * 0.6f && n >= 6) rows = 2;          // 칸이 너무 좁으면 두 줄
        int perRow = Mathf.CeilToInt(n / (float)rows);
        int cellH = rows == 1 ? H : Mathf.Max(3, Mathf.RoundToInt(H * 0.8f));
        int w = Mathf.Min(Mathf.RoundToInt(H * 2.6f), (areaW - (perRow - 1) * gap) / perRow);      // 칸이 적어도 너무 길쭉하지 않게
        w = Mathf.Max(3, w);
        // 속은 테두리 안쪽 (칸이 아주 작으면 1px)
        int inset = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(w, cellH) * 0.18f), 1, Mathf.Max(1, Mathf.RoundToInt(2.5f * s)));
        for (int i = 0; i < MaxCells; i++)
        {
            bool has = i < n;
            if (!has) { if (rims[i] != null) rims[i].gameObject.SetActive(false); continue; }
            if (rims[i] == null) MakeCell(i);
            rims[i].gameObject.SetActive(true);
            int row = i / perRow, col = i % perRow;
            int inRow = row < rows - 1 ? perRow : n - perRow * (rows - 1);
            float x = -(inRow * w + (inRow - 1) * gap) * 0.5f + col * (w + gap) + w * 0.5f;        // 픽셀
            float y = rows == 1 ? 0f : (row == 0 ? 1f : -1f) * (cellH + gap) * 0.5f;
            PlaceBox(rims[i], fills[i], w, cellH, inset, x, y, s);
        }
        int rowW = perRow * w + (perRow - 1) * gap;
        if (maxLabel == null)
        {
            GameObject go = new GameObject("Max", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(root, false);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(0f, -6f);
            r.offsetMax = new Vector2(0f, 6f);
            maxLabel = go.GetComponent<TextMeshProUGUI>();
            if (UIKit.Font != null) maxLabel.font = UIKit.Font;
            if (UIKit.FontMaterial != null) maxLabel.fontSharedMaterial = UIKit.FontMaterial;
            maxLabel.text = "MAX";
            maxLabel.fontStyle = FontStyles.Bold;
            maxLabel.alignment = TextAlignmentOptions.Center;
            maxLabel.enableWordWrapping = false;
            maxLabel.color = new Color(0.25f, 0.13f, 0.02f);
            maxLabel.outlineWidth = 0.2f;
            maxLabel.outlineColor = new Color32(255, 240, 190, 255);
            maxLabel.raycastTarget = false;
        }
        maxLabel.fontSize = Mathf.Clamp(area.y * 1.05f, 9f, 22f);

        // 최대일 때 「MAX」 밑 금빛 판: 글자 폭 + 여백 (칸 줄 폭 안) · 칸과 같은 픽셀 위상(폭의 홀짝을 칸 줄과 맞춤)
        if (plateRim == null) MakeBox("MaxPlate", out plateRim, out plateFill);
        int pad = Mathf.Max(2, inset + 1);
        int plateW = Mathf.CeilToInt(maxLabel.GetPreferredValues("MAX").x * s) + pad * 2;
        if (((plateW - rowW) & 1) != 0) plateW++;
        plateW = Mathf.Min(plateW, rowW);
        PlaceBox(plateRim, plateFill, plateW, cellH, inset, 0f, 0f, s);
        plateRim.transform.SetAsLastSibling();
        maxLabel.transform.SetAsLastSibling();
    }

    // 칸 하나 배치 (크기 · 위치는 픽셀) — 테두리 1텍셀 = 정확히 1px 이 되게 9-슬라이스 배율도 맞춤
    static void PlaceBox(Image rim, Image fill, int wPx, int hPx, int insetPx, float xPx, float yPx, float s)
    {
        float u = 1f / s;
        RectTransform cr = rim.rectTransform;
        cr.sizeDelta = new Vector2(wPx * u, hPx * u);
        cr.anchoredPosition = new Vector2(xPx * u, yPx * u);
        rim.pixelsPerUnitMultiplier = s;
        fill.pixelsPerUnitMultiplier = s;
        RectTransform fr = fill.rectTransform;
        fr.offsetMin = new Vector2(insetPx * u, insetPx * u);
        fr.offsetMax = new Vector2(-insetPx * u, -insetPx * u);
    }

    void MakeCell(int i) => MakeBox("Pip" + i, out rims[i], out fills[i]);

    void MakeBox(string name, out Image rim, out Image fill)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(root, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        rim = go.GetComponent<Image>();
        rim.sprite = CellSprite;
        rim.type = Image.Type.Sliced;
        rim.raycastTarget = false;

        GameObject fg = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        RectTransform fr = fg.GetComponent<RectTransform>();
        fr.SetParent(r, false);
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fill = fg.GetComponent<Image>();
        fill.sprite = CellSprite;
        fill.type = Image.Type.Sliced;
        fill.raycastTarget = false;
    }

    void Update()
    {
        if (max <= 0) return;
        // 해상도 · UI 크기가 바뀌면 정수 픽셀로 다시 배치 (값 비교만 · 할당 없음)
        if (!Mathf.Approximately(PixelScale(), laidScale)) Layout(max);
        // 차오를 칸이 있을 때만 맥동 (HUD 처럼 늘 떠 있는 칸은 가만히 — 매 프레임 색을 바꾸면 캔버스를 계속 다시 그림)
        if (adding > 0) Paint(0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
    }

    // k: 맥동 0~1
    void Paint(float k)
    {
        bool full = IsMax;
        for (int i = 0; i < max && i < MaxCells; i++)
        {
            if (rims[i] == null) continue;
            bool on = i < now;
            bool next = !on && i < now + adding;
            Image rim = rims[i], fill = fills[i];
            if (full)
            {
                rim.color = FilledRim;
                fill.color = Color.Lerp(Gold, GoldBright, k * 0.35f);
                rim.rectTransform.localScale = Vector3.one;
            }
            else if (next)
            {
                rim.color = Color.Lerp(Gold, Color.white, k);
                fill.color = Color.Lerp(Gold, GoldBright, k);
                rim.rectTransform.localScale = Vector3.one * (1f + 0.28f * k);
            }
            else if (on)
            {
                rim.color = FilledRim;
                fill.color = Filled;
                rim.rectTransform.localScale = Vector3.one;
            }
            else
            {
                rim.color = EmptyRim;
                fill.color = EmptyFill;
                rim.rectTransform.localScale = Vector3.one;
            }
        }
        if (plateRim != null)
        {
            if (plateRim.gameObject.activeSelf != full) plateRim.gameObject.SetActive(full);
            if (full)
            {
                plateRim.color = FilledRim;
                plateFill.color = Color.Lerp(Gold, GoldBright, 0.35f + k * 0.35f);
            }
        }
        if (maxLabel != null) maxLabel.gameObject.SetActive(full);
    }

    // ================================================================= 칸 그림 (둥근 네모 12x12, 모서리 1칸 비움 · 9-슬라이스 테두리 1)
    // 1.2.3~ 테두리 3 → 1: PlaceBox 가 1텍셀 = 1px 로 맞춰 모서리 홈이 어느 칸이든 정확히 1px (예전엔 3텍셀을 소수 픽셀로 늘려 칸마다 홈 크기가 달랐음)
    static Sprite cellSprite;
    public static Sprite CellSprite
    {
        get
        {
            if (cellSprite != null) return cellSprite;
            const int S = 12;
            Texture2D t = new Texture2D(S, S, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    bool corner = (x == 0 || x == S - 1) && (y == 0 || y == S - 1);
                    px[y * S + x] = corner ? new Color32(255, 255, 255, 0) : new Color32(255, 255, 255, 255);
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            cellSprite = Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
            cellSprite.name = "LevelPipCell";
            return cellSprite;
        }
    }
}
