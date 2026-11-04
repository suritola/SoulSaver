using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 메뉴 · 상점 · 설정 등 모든 버튼에 마우스를 올리거나 누를 때 짧은 소리
// 게임이 켜지면 스스로 만들어지고, 화면에 있는 버튼(코드로 만든 버튼 포함)을 주기적으로 찾아 소리를 붙임
// 소리는 사운드 파일 없이 코드에서 파형을 만들어 씀 (SpecialFeedback 과 같은 방식)
public class UISound : MonoBehaviour
{
    const int Rate = 44100;
    const float Volume = 0.35f;

    static UISound instance;
    AudioSource source;
    AudioClip hover, click;
    float lastHover;
    float scanAt;
    Selectable[] found = new Selectable[256];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null || Application.isBatchMode) return;
        GameObject go = new GameObject("UISound");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<UISound>();
    }

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        // 메뉴가 멈춘 화면에서도 울리도록
        source.ignoreListenerPause = true;

        // 올림: 아주 짧고 높은 똑 · 누름: 두 겹의 또각
        hover = Make("ui_hover", 0.035f, t => Mathf.Sin(2f * Mathf.PI * 2400f * t) * Mathf.Pow(1f - t / 0.035f, 3f) * 0.35f);
        click = Make("ui_click", 0.09f, t =>
        {
            float body = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 520f, t / 0.09f) * t);
            float tick = t < 0.012f ? Mathf.Sin(2f * Mathf.PI * 3200f * t) * 0.6f : 0f;
            return (body * 0.5f + tick) * Mathf.Pow(1f - t / 0.09f, 2.5f) * 0.6f;
        });
    }

    static AudioClip Make(string name, float dur, System.Func<float, float> wave)
    {
        int n = Mathf.CeilToInt(dur * Rate);
        float[] data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
        AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // 새로 생긴 버튼에 소리를 붙임 (0.25초마다, 켜져 있는 것만 · 멈춘 화면에서도)
    void Update()
    {
        if (Time.unscaledTime < scanAt) return;
        scanAt = Time.unscaledTime + 0.25f;
        if (found.Length < Selectable.allSelectableCount) found = new Selectable[Selectable.allSelectableCount * 2];
        int count = Selectable.AllSelectablesNoAlloc(found);
        for (int i = 0; i < count; i++)
        {
            Selectable s = found[i];
            if (s != null && !s.TryGetComponent(out UISoundHook _)) s.gameObject.AddComponent<UISoundHook>();
            found[i] = null;
        }
    }

    // 2026-11-04~ 커서 스킨 호버 · 클릭 소리 (모든 메뉴 버튼의 공통 지점) — 없으면 공통 똑 · 또각
    public static void Hover()
    {
        if (instance == null || Time.unscaledTime - instance.lastHover < 0.04f) return;
        instance.lastHover = Time.unscaledTime;
        AudioClip skin = CursorSkin.SkinSound("hover");
        instance.source.PlayOneShot(skin ?? instance.hover, Volume * GameSettings.SfxVolume * (skin != null ? 0.5f : 1f));
    }

    public static void Click()
    {
        if (instance == null) return;
        AudioClip skin = CursorSkin.SkinSound("click");
        instance.source.PlayOneShot(skin ?? instance.click, Volume * GameSettings.SfxVolume * (skin != null ? 0.6f : 1f));
        CursorSkin.MenuClick();
    }
}

// 버튼 하나에 붙는 소리 연결 (누를 수 있을 때만)
public class UISoundHook : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    Selectable selectable;

    void Awake() => selectable = GetComponent<Selectable>();

    bool Usable => selectable != null && selectable.IsInteractable() && !GameInput.Auto;

    public void OnPointerEnter(PointerEventData e)
    {
        if (Usable) UISound.Hover();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (Usable && e.button == PointerEventData.InputButton.Left) UISound.Click();
    }
}
