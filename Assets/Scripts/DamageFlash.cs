using UnityEngine;
using UnityEngine.UI;

// 피격 화면 효과: 맞으면 화면 가장자리가 붉게 번쩍이고, 체력이 낮으면 은은하게 맥박침
// 처음 쓸 때 캔버스에 스스로 만들어짐 (씬에 따로 둘 필요 없음)
public class DamageFlash : MonoBehaviour
{
    const float LowHealthRatio = 0.3f;
    float nextBeat, beatEcho;

    static DamageFlash instance;

    Image image;
    float flash;
    float shownAlpha;
    PlayerController player;

    public static void Show(float strength)
    {
        if (instance == null) Create();
        // 번쩍임 효과를 끄면 피격 번쩍임 없음 (저체력 테두리는 깜빡이지 않고 은은하게 유지)
        if (!GameSettings.Flashes) return;
        if (instance != null) instance.flash = Mathf.Max(instance.flash, Mathf.Clamp01(strength));
    }

    static void Create()
    {
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;

        GameObject go = new GameObject("DamageFlash", typeof(RectTransform), typeof(Image), typeof(DamageFlash));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false);
        r.SetSiblingIndex(0);                   // HUD 뒤에 깔려서 글자를 가리지 않음
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        // 최적화: 자기만 든 하위 캔버스로 — 저체력 맥박 · 피격 번쩍임 동안 HUD 전체를 다시 묶지 않게 (형제 0번 그대로 = HUD 뒤, 클릭 없음)
        UiScaler.Isolate(go, false);

        instance = go.GetComponent<DamageFlash>();
        instance.image = go.GetComponent<Image>();
        instance.image.sprite = MakeVignette();
        instance.image.raycastTarget = false;
        instance.image.color = new Color(1f, 1f, 1f, 0f);
    }

    // 가운데는 투명하고 가장자리로 갈수록 붉은 테두리
    // 최적화: 매번 같은 그림이라 한 번만 만들어 판끼리 같이 씀 (판마다 128² 텍스처를 새로 만들지 않음)
    static Sprite vignette;
    static Sprite MakeVignette()
    {
        if (vignette != null) return vignette;
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.2f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d));
                px[y * size + x] = new Color(0.85f, 0.04f, 0.04f, a * 0.9f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
        vignette = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        vignette.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return vignette;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        // 느려진 시간(타겟팅 스킬)이나 멈춘 화면에서도 자연스럽게 사라지도록 실제 시간 사용
        flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 2.2f);

        if (player == null) player = FindFirstObjectByType<PlayerController>();
        float low = 0f;
        if (player != null && player.PlayerMaxHealth > 0f && player.PlayerHealth / player.PlayerMaxHealth < LowHealthRatio)
        {
            low = GameSettings.Flashes ? 0.22f + 0.14f * Mathf.Sin(Time.unscaledTime * 5f) : 0.22f;
            // 심장 박동 "쿵-쿵" (멈춘 동안에는 안 남)
            if (Time.timeScale > 0f && player.PlayerHealth > 0f && Time.unscaledTime >= nextBeat && SpecialAbilities.SharedFx != null)
            {
                nextBeat = Time.unscaledTime + 0.95f;
                SpecialAbilities.SharedFx.Play("thump", 0.45f, 0.55f);
                beatEcho = Time.unscaledTime + 0.18f;
            }
            if (beatEcho > 0f && Time.unscaledTime >= beatEcho)
            {
                beatEcho = 0f;
                if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.Play("thump", 0.3f, 0.5f);
            }
        }

        float alpha = Mathf.Max(flash, low);
        // 최적화: 눈에 보이는 만큼(1/255) 바뀔 때만 색을 씀 · 켜짐도 바뀔 때만
        if (Mathf.Abs(alpha - shownAlpha) >= 1f / 255f || (alpha == 0f && shownAlpha != 0f))
        {
            shownAlpha = alpha;
            image.color = new Color(1f, 1f, 1f, alpha);
        }
        bool on = alpha > 0.01f;
        if (image.enabled != on) image.enabled = on;
    }
}
