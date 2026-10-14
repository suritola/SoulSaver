using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스킬 진화 연출 (무기 진화 화면처럼): 화면이 어두워지고 재료 스킬 아이콘들이 둘레에서 빛나며 돌다가
// 가운데로 빨려 들어가 부딪치고, 번쩍! 진화한 스킬 아이콘 · 이름 · 새 능력이 나타남. 클릭 · Space로 닫음
// 게임은 멈춰 있으므로 실제 시간(unscaled)으로 움직임. 여러 개가 한꺼번에 진화하면 차례로
public class SkillEvolutionUI : MonoBehaviour
{
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Soul = new Color(0.79f, 0.63f, 1f);

    public static bool Open { get; private set; }
    // 1.0.4~ 설정 「번쩍임」을 끄면 화면 섬광을 1/4 로 (알파 1 → 0.25)
    static float FlashMul => GameSettings.Flashes ? 1f : 0.25f;
    static readonly Queue<(LevelShop.SkillEvo evo, CharacterId who)> queue = new Queue<(LevelShop.SkillEvo, CharacterId)>();
    static SkillEvolutionUI runner;

    RectTransform root;
    Image dim, flash, burst, ring;
    Sprite glow;
    bool closeRequested;

    public static void Queue(LevelShop.SkillEvo evo, CharacterId who)
    {
        if (evo == null || RunSave.Replaying) return;      // 이어하기로 다시 적용할 때는 연출 없이
        queue.Enqueue((evo, who));
        if (runner == null)
        {
            Canvas canvas = UIKit.HudCanvas();
            if (canvas == null) { queue.Clear(); return; }
            GameObject go = new GameObject("SkillEvolution", typeof(RectTransform));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(canvas.rootCanvas.transform, false);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            r.SetAsLastSibling();
            SoulTreeUI.OnTop(go, 610);
            runner = go.AddComponent<SkillEvolutionUI>();
            runner.root = r;
            runner.StartCoroutine(runner.RunAll());
        }
    }

    void OnDestroy()
    {
        if (runner == this) runner = null;
        Open = false;
    }

    IEnumerator RunAll()
    {
        Open = true;
        TooltipUI.Hide();
        float before = Time.timeScale;
        Time.timeScale = 0f;
        glow = SpecialAbilities.SharedInstance != null ? SpecialAbilities.SharedInstance.glowSprite : null;
        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            yield return Play(next.evo, next.who);
        }
        // 다른 창(레벨업 · 일시정지)이 시간을 멈춰 두지 않았다면 되돌림
        Time.timeScale = ESCmenu.IsOpen ? 0f : (before > 0f ? before : 1f);
        Open = false;
        Destroy(gameObject);
    }

    // 1.0.4~: 연출 건너뛰기 — 모여드는 동안 클릭 · [상호작용 키]를 누르면 바로 결과로 (레벨업 카드를 확정한 더블클릭이 넘어오지 않게 열린 직후 0.3초는 받지 않음)
    //         같은 진화를 이 PC에서 두 번째 볼 때부터는 모여드는 연출이 두 배 빠름
    static bool SkipPressed(float since) => Time.unscaledTime - since > 0.3f && (Input.GetMouseButtonDown(0) || KeyBindings.Down(GameAction.Interact) || GamePad.ADown);

    IEnumerator Play(LevelShop.SkillEvo e, CharacterId who)
    {
        for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
        closeRequested = false;
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        string seenKey = "seen.evofx." + who + ":" + e.key;
        float speed = Prefs.GetInt(seenKey, 0) == 1 ? 2f : 1f;
        if (speed == 1f && !GameInput.Auto) Prefs.SetInt(seenKey, 1);
        float startedAt = Time.unscaledTime;
        bool skip = false;

        dim = Img("Dim", Vector2.zero, Vector2.zero, null, Color.clear);
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        burst = Img("Burst", Vector2.zero, new Vector2(900f, 900f), glow, Color.clear);
        ring = Img("Ring", Vector2.zero, new Vector2(520f, 520f), glow, Color.clear);

        // 1) 어두워지며 재료 아이콘이 둘레에 나타나 돔
        fx?.Play("pulse", 0.7f, 0.9f);
        fx?.Play("shimmer", 0.6f, 0.8f);
        int n = e.parts.Length;
        Image[] parts = new Image[n];
        Image[] glows = new Image[n];
        TMP_Text[] names = new TMP_Text[n];
        for (int i = 0; i < n; i++)
        {
            glows[i] = Img("PartGlow", Vector2.zero, new Vector2(200f, 200f), glow, new Color(Soul.r, Soul.g, Soul.b, 0.35f));
            parts[i] = Img("Part", Vector2.zero, new Vector2(150f, 150f), LevelShop.CardIcon(who, e.parts[i]), Color.white);
            parts[i].preserveAspect = true;
            names[i] = UIKit.Text(root, "", 26f, Parch, Vector2.zero, new Vector2(320f, 40f));
            names[i].text = LevelShop.CardName(who, e.parts[i]);
        }
        TMP_Text title = UIKit.Text(root, "", 64f, Soul, new Vector2(0f, 390f), new Vector2(1500f, 90f));
        title.text = Loc.T("스킬 진화");
        title.fontStyle = FontStyles.Bold;
        title.alpha = 0f;

        float spin = 0f;
        for (float t = 0f; t < 1.4f; t += Time.unscaledDeltaTime * speed)
        {
            if (SkipPressed(startedAt)) { skip = true; break; }
            float k = t / 1.4f;
            dim.color = new Color(0.03f, 0.01f, 0.06f, 0.9f * Mathf.Clamp01(k * 2.5f));
            title.alpha = Mathf.Clamp01(k * 2f);
            spin += Time.unscaledDeltaTime * Mathf.Lerp(40f, 260f, k * k);
            float radius = Mathf.Lerp(330f, 250f, k);
            for (int i = 0; i < n; i++)
            {
                float a = (spin + i * 360f / n + 90f) * Mathf.Deg2Rad;
                Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                parts[i].rectTransform.anchoredPosition = p;
                glows[i].rectTransform.anchoredPosition = p;
                names[i].rectTransform.anchoredPosition = p + new Vector2(0f, -100f);
                names[i].alpha = Mathf.Clamp01(k * 3f) * (1f - Mathf.Clamp01((k - 0.7f) * 4f));
                parts[i].rectTransform.localScale = Vector3.one * Mathf.Min(1f, k * 4f);
            }
            burst.color = new Color(Soul.r, Soul.g, Soul.b, 0.5f * k);
            burst.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(900f, 300f, k);
            if (Random.value < 0.6f) Spark(Random.insideUnitCircle.normalized * Random.Range(400f, 650f), Vector2.zero, 0.5f);
            yield return null;
        }

        // 2) 가운데로 빨려 들어가 부딪침
        fx?.Play("whoosh", 0.8f, 0.7f);
        Vector2[] from = new Vector2[n];
        for (int i = 0; i < n; i++) from[i] = parts[i].rectTransform.anchoredPosition;
        for (float t = 0f; t < 0.35f && !skip; t += Time.unscaledDeltaTime * speed)
        {
            if (SkipPressed(startedAt)) { skip = true; break; }
            float k = EaseIn(t / 0.35f);
            for (int i = 0; i < n; i++)
            {
                Vector2 p = Vector2.Lerp(from[i], Vector2.zero, k);
                parts[i].rectTransform.anchoredPosition = p;
                glows[i].rectTransform.anchoredPosition = p;
                parts[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, k);
                names[i].alpha = 0f;
            }
            yield return null;
        }

        for (int i = 0; i < n; i++) names[i].alpha = 0f;       // 1.0.4~ 건너뛰었을 때 재료 이름이 남지 않게

        // 3) 번쩍! 진화한 스킬
        fx?.Play("shatter", 0.9f, 1.1f);
        fx?.Play("bigboom", 0.6f, 1.3f);
        fx?.Play("levelup", 0.8f, 1f);
        fx?.Shake(0.35f, 0.25f);
        for (int i = 0; i < n; i++)
        {
            Destroy(glows[i].gameObject);
            Destroy(parts[i].gameObject);
        }
        for (int i = 0; i < 44; i++) Spark(Vector2.zero, Random.insideUnitCircle.normalized * Random.Range(400f, 1200f), Random.Range(0.5f, 0.9f));
        flash = Img("Flash", Vector2.zero, Vector2.zero, null, Color.clear);
        Stretch(flash.rectTransform);

        Img("EvoGlow", new Vector2(0f, 90f), new Vector2(380f, 380f), glow, new Color(Gold.r, Gold.g, Gold.b, 0.45f));
        Image icon = Img("Evo", new Vector2(0f, 90f), new Vector2(240f, 240f), Resources.Load<Sprite>("Icons/ability_" + e.icon), Color.white);
        icon.preserveAspect = true;
        TMP_Text name = UIKit.Text(root, "", 58f, Gold, new Vector2(0f, -100f), new Vector2(1400f, 80f));
        name.text = Loc.T(e.name);
        name.fontStyle = FontStyles.Bold;
        TMP_Text recipe = UIKit.Text(root, "", 24f, Soul, new Vector2(0f, -158f), new Vector2(1400f, 36f));
        List<string> rn = new List<string>();
        foreach (int p in e.parts) rn.Add(LevelShop.CardName(who, p));
        recipe.text = string.Join("  +  ", rn);
        TMP_Text desc = UIKit.Text(root, "", 30f, Parch, new Vector2(0f, -240f), new Vector2(1100f, 120f), TextAlignmentOptions.Top);
        desc.text = Loc.T(e.desc);
        TMP_Text hint = UIKit.Text(root, "", 24f, new Color(0.65f, 0.6f, 0.7f), new Vector2(0f, -380f), new Vector2(900f, 36f));
        hint.text = Loc.T("클릭하거나 [") + KeyBindings.Name(GameAction.Interact) + Loc.T("] 를 눌러 계속");

        dim.color = new Color(0.03f, 0.01f, 0.06f, 0.9f);           // 1.0.4~ 건너뛰어도 끝 모습으로
        title.alpha = 1f;
        for (float t = 0f; t < 0.5f && !skip; t += Time.unscaledDeltaTime * speed)
        {
            if (SkipPressed(startedAt)) { skip = true; break; }
            float k = t / 0.5f;
            flash.color = new Color(1f, 0.95f, 1f, (1f - k) * FlashMul);
            icon.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, EaseOut(k));
            name.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, EaseOut(k));
            name.alpha = desc.alpha = recipe.alpha = k;
            ring.color = new Color(Gold.r, Gold.g, Gold.b, 0.7f * (1f - k));
            ring.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(200f, 1400f, k);
            burst.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(300f, 1100f, k);
            yield return null;
        }
        flash.color = Color.clear;
        icon.rectTransform.localScale = Vector3.one;                  // 1.0.4~ 건너뛰어도 끝 모습으로
        name.rectTransform.localScale = Vector3.one;
        name.alpha = desc.alpha = recipe.alpha = 1f;
        ring.color = Color.clear;
        burst.rectTransform.sizeDelta = Vector2.one * 1100f;
        Button closer = dim.gameObject.AddComponent<Button>();
        closer.transition = Selectable.Transition.None;
        closer.onClick.AddListener(() => closeRequested = true);

        // 4) 기다림 (아이콘이 숨 쉬듯, 빛이 돎). 너무 빨리 넘어가지 않게 0.4초 뒤부터
        float shown = 0f;
        while (!(shown > 0.4f && (closeRequested || KeyBindings.Down(GameAction.Interact))))
        {
            shown += Time.unscaledDeltaTime;
            if (shown <= 0.4f) closeRequested = false;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
            icon.rectTransform.localScale = Vector3.one * (1f + 0.05f * pulse);
            burst.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 15f);
            burst.color = new Color(Soul.r, Soul.g, Soul.b, 0.25f + 0.1f * pulse);
            hint.alpha = 0.5f + 0.5f * pulse;
            if (Random.value < 0.15f) Spark(new Vector2(0f, 90f), Random.insideUnitCircle.normalized * Random.Range(150f, 400f), 0.7f);
            yield return null;
        }
        fx?.Play("chime", 0.6f, 1.3f);

        // 5) 사라짐
        CanvasGroup all = root.gameObject.GetComponent<CanvasGroup>();
        if (all == null) all = root.gameObject.AddComponent<CanvasGroup>();
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            all.alpha = 1f - t / 0.3f;
            yield return null;
        }
        all.alpha = 1f;
    }

    // 튀는 빛 조각 (속도가 0이면 가운데로 빨려 들어감)
    void Spark(Vector2 from, Vector2 velocity, float life)
    {
        Image s = Img("Spark", from, Vector2.one * Random.Range(14f, 34f), glow, Color.Lerp(Soul, Gold, Random.value));
        StartCoroutine(Fly(s, from, velocity, life));
    }

    IEnumerator Fly(Image s, Vector2 from, Vector2 velocity, float life)
    {
        Color c0 = s.color;
        bool gather = velocity == Vector2.zero;
        for (float t = 0f; t < life && s != null; t += Time.unscaledDeltaTime)
        {
            float k = t / life;
            s.rectTransform.anchoredPosition = gather ? Vector2.Lerp(from, Vector2.zero, EaseIn(k)) : from + velocity * t * (1f - 0.5f * k);
            s.color = new Color(c0.r, c0.g, c0.b, c0.a * (gather ? k : 1f - k));
            yield return null;
        }
        if (s != null) Destroy(s.gameObject);
    }

    static float EaseOut(float k) => 1f - (1f - Mathf.Clamp01(k)) * (1f - Mathf.Clamp01(k));
    static float EaseIn(float k) => Mathf.Clamp01(k) * Mathf.Clamp01(k);

    Image Img(string name, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        RectTransform r = UIKit.Rect(name, root, pos, size);
        Image i = r.gameObject.AddComponent<Image>();
        i.sprite = sprite;
        i.color = color;
        i.raycastTarget = false;
        return i;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
