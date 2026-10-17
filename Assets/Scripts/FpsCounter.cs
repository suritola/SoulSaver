using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 1.1.9~ 설정 「프레임 표시」: 화면 오른쪽 위 구석에 작은 FPS 숫자 (0.5초마다 갱신, 매 프레임 할당 없음)
// 씬이 바뀌어도 남는 전용 캔버스 하나 · 끄면 통째로 비활성 (비용 0)
public class FpsCounter : MonoBehaviour
{
    static FpsCounter instance;

    TextMeshProUGUI label;
    int frames;
    float elapsed;

    public static void Refresh()
    {
        bool want = GameSettings.ShowFps && !Application.isBatchMode;
        if (!want)
        {
            if (instance != null) instance.gameObject.SetActive(false);
            return;
        }
        if (instance == null) Create();
        instance.gameObject.SetActive(true);
    }

    static void Create()
    {
        GameObject go = new GameObject("FpsCounter");
        DontDestroyOnLoad(go);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        UiScaler.Fit(cs);               // UiScaler 가 1초마다 찾지 않으므로 만들 때 맞춤

        GameObject t = new GameObject("Fps", typeof(RectTransform));
        t.transform.SetParent(go.transform, false);
        RectTransform r = (RectTransform)t.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-6f, -2f);
        r.sizeDelta = new Vector2(120f, 30f);
        TextMeshProUGUI label = t.AddComponent<TextMeshProUGUI>();
        if (UIKit.Font != null) label.font = UIKit.Font;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.TopRight;
        label.color = new Color(0.75f, 1f, 0.6f, 0.85f);
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.text = "";

        instance = go.AddComponent<FpsCounter>();
        instance.label = label;
    }

    void OnEnable()
    {
        frames = 0;
        elapsed = 0f;
    }

    void Update()
    {
        frames++;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed < 0.5f) return;
        label.SetText("{0} FPS", Mathf.RoundToInt(frames / elapsed));
        frames = 0;
        elapsed = 0f;
    }
}
