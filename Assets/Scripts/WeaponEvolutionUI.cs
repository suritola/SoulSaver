using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 무기 진화 화면 (거너): 스테이지 보스를 쓰러뜨리면 화면이 어두워지고 빛이 모인 뒤
// 지금 무기가 부서지며 세 갈래 진화 카드가 펼쳐짐. 하나를 고르고 확정하면 되돌릴 수 없음
// 게임은 멈춰 있으므로 모든 연출은 실제 시간(unscaled)으로 움직임
public class WeaponEvolutionUI : MonoBehaviour
{
    static Color Gold => UiRules.Legacy ? LegacyGold : UiTokens.Text1;      // UI 개편 (Legacy 끔): 금색 글자 대신 뼈색 (기획서 C6 「금색 25벌」)
    static readonly Color LegacyGold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Soul = new Color(0.72f, 0.58f, 1f);

    public static bool Open { get; private set; }
    // 1.0.4~ 설정 「번쩍임」을 끄면 화면 섬광을 1/4 로 (알파 1 → 0.25 · 0.6 → 0.15), 「화면 흔들림」을 끄면 무기 떨림 없음
    static float FlashMul => GameSettings.Flashes ? 1f : 0.25f;
    static float ShakeMul => GameSettings.ScreenShake ? 1f : 0f;

    SpecialAbilities sp;
    int[] options;
    int tier;
    int selected = -1;
    bool confirmed;

    RectTransform root;
    Image dim, flash, burst, burst2;
    RectTransform[] cards;
    Image[] cardFrames;
    Button confirmButton;
    TMP_Text confirmText, title, subtitle;
    Image titleLine;        // 1.4.8~ 제목 아래 금 장식선 (그림이 없으면 null)

    // 진화를 고를 때까지 기다림 (고른 무기 번호를 넘겨줌)
    public static IEnumerator Run(SpecialAbilities sp, int[] options, System.Action<int> picked)
    {
        if (sp == null || options == null || options.Length == 0) yield break;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) yield break;
        UIKit.EnsureStyle();

        GameObject go = new GameObject("WeaponEvolution", typeof(RectTransform));
        WeaponEvolutionUI ui = go.AddComponent<WeaponEvolutionUI>();
        ui.sp = sp;
        ui.options = options;
        ui.tier = sp.EvolutionTier + 1;
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.rootCanvas.transform, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.SetAsLastSibling();
        SoulTreeUI.OnTop(go, 600);
        ui.root = r;
        Open = true;
        TooltipUI.Hide();

        yield return ui.Play();

        int id = ui.options[ui.selected];
        Open = false;
        Destroy(go);
        picked?.Invoke(id);
    }

    // 1.0.4~: 연출 건너뛰기 — 클릭 · [상호작용 키]로 카드가 펼쳐진 상태 · 결과 화면으로 바로
    // 보스를 쏘던 연타가 바로 넘기지 않게 시작 연출은 0.4초, 결과 화면은 확정한 입력이 넘어오지 않게 0.15초 뒤부터 받음
    float startedAt;
    bool skipIntro;
    static bool SkipPressed(float since, float guard = 0.15f) => Time.unscaledTime - since > guard && (Input.GetMouseButtonDown(0) || KeyBindings.Down(GameAction.Interact) || GamePad.ADown);

    IEnumerator Play()
    {
        Build();
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        startedAt = Time.unscaledTime;

        // 1) 어두워지며 가운데로 영혼 빛이 모임
        fx?.Play("pulse", 0.7f, 0.7f);
        Image core = Img("Core", root, Vector2.zero, new Vector2(150f, 150f), CurrentSprite(), Color.white);
        core.preserveAspect = true;
        float fxT = GameSettings.FxTime;        // 1.1.9~ 「빠른 연출」이면 인트로 · 결과 머무름 절반
        for (float t = 0f; t < 1.1f * fxT && !skipIntro; t += Time.unscaledDeltaTime)
        {
            if (SkipPressed(startedAt, 0.4f)) { skipIntro = true; break; }
            float k = t / (1.1f * fxT);
            dim.color = new Color(0.02f, 0.01f, 0.04f, 0.9f * Mathf.Clamp01(k * 2f));
            burst.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(900f, 260f, k);
            burst.color = new Color(Soul.r, Soul.g, Soul.b, 0.8f * k);
            burst.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
            core.rectTransform.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t * 30f) * k);
            core.rectTransform.anchoredPosition = Random.insideUnitCircle * 6f * k * ShakeMul;
            if (Random.value < 0.5f) Spark(Random.insideUnitCircle.normalized * Random.Range(350f, 600f), Vector2.zero, 0.5f);
            yield return null;
        }

        // 2) 번쩍! 무기가 부서지고 제목이 내려꽂힘
        fx?.Play("shatter", 0.9f, 0.8f);
        fx?.Play("bigboom", 0.7f, 1.1f);
        fx?.Shake(0.35f, 0.25f);
        for (int i = 0; i < 36; i++) Spark(Vector2.zero, Random.insideUnitCircle.normalized * Random.Range(400f, 1100f), Random.Range(0.4f, 0.8f));
        Destroy(core.gameObject);
        title.gameObject.SetActive(true);
        subtitle.gameObject.SetActive(true);
        for (float t = 0f; t < 0.45f * fxT && !skipIntro; t += Time.unscaledDeltaTime)
        {
            if (SkipPressed(startedAt, 0.4f)) { skipIntro = true; break; }
            float k = t / (0.45f * fxT);
            flash.color = new Color(1f, 0.95f, 1f, (1f - k) * FlashMul);
            burst.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(260f, 1500f, k);
            burst.color = new Color(Soul.r, Soul.g, Soul.b, 0.8f * (1f - k) + 0.12f);
            title.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, EaseOut(k));
            title.alpha = k;
            subtitle.alpha = k;
            if (titleLine != null) titleLine.color = new Color(1f, 1f, 1f, k);
            yield return null;
        }
        flash.color = Color.clear;
        dim.color = new Color(0.02f, 0.01f, 0.04f, 0.9f);          // 1.0.4~ 건너뛰어도 끝 모습으로
        burst.rectTransform.sizeDelta = Vector2.one * 1500f;
        burst.color = new Color(Soul.r, Soul.g, Soul.b, 0.12f);
        title.rectTransform.localScale = Vector3.one;
        title.alpha = subtitle.alpha = 1f;
        if (titleLine != null) titleLine.color = Color.white;

        // 3) 세 카드가 가운데에서 펼쳐짐
        fx?.Play("whoosh", 0.7f, 0.9f);
        Vector2[] goal = new Vector2[cards.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            goal[i] = new Vector2((i - (cards.Length - 1) / 2f) * 480f, -50f);          // 1.1~ -40 → -50: 고른 카드(1.08배) 윗변이 부제 글자 밑(295)에 오게
            cards[i].gameObject.SetActive(true);
        }
        for (float t = 0f; t < 0.55f * fxT && !skipIntro; t += Time.unscaledDeltaTime)
        {
            if (SkipPressed(startedAt, 0.4f)) { skipIntro = true; break; }
            float k = EaseOut(t / (0.55f * fxT));
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].anchoredPosition = Vector2.Lerp(Vector2.zero, goal[i], k);
                cards[i].localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, k);
                cards[i].localRotation = Quaternion.Euler(0f, 0f, (1f - k) * (i - 1) * 25f);
            }
            yield return null;
        }
        for (int i = 0; i < cards.Length; i++) { cards[i].anchoredPosition = goal[i]; cards[i].localScale = Vector3.one; cards[i].localRotation = Quaternion.identity; }
        confirmButton.gameObject.SetActive(true);
        yield return null;          // 1.0.4~ 건너뛴 키 입력이 같은 프레임에 확정으로 이어지지 않게

        // 4) 고르기 (클릭 또는 1 · 2 · 3, 확정은 버튼 · Space · 같은 숫자 한 번 더 — 1.0.4~ 레벨업 · 보스 보상 창과 같은 방식, 키패드 숫자도)
        while (!confirmed)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    if (selected == i) Confirm();
                    else Select(i);
                }
                if (!UiRules.Legacy)
                {
                    // UI 개편: 고른 카드 1.08배 · 테 맥박 대신 청록 테 카드 그림
                    cards[i].localScale = Vector3.one;
                    Sprite cs = UiRules.Sprite(i == selected ? "ui2_card_s" : "ui2_card_n");
                    if (cs != null && cardFrames[i].sprite != cs) { cardFrames[i].sprite = cs; cardFrames[i].type = Image.Type.Tiled; cardFrames[i].pixelsPerUnitMultiplier = 1f; }
                    cardFrames[i].color = Color.white;
                    continue;
                }
                float target = i == selected ? 1.08f : 1f;
                cards[i].localScale = Vector3.one * Mathf.MoveTowards(cards[i].localScale.x, target, Time.unscaledDeltaTime * 1.5f);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
                cardFrames[i].color = i == selected ? Color.Lerp(Gold, Color.white, pulse) : new Color(0.55f, 0.48f, 0.7f);
            }
            burst.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 12f);
            confirmButton.interactable = selected >= 0;
            confirmText.color = !UiRules.Legacy ? (selected >= 0 ? UiTokens.Text1 : UiTokens.Text3)
                              : selected >= 0 ? Color.Lerp(Gold, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : new Color(0.6f, 0.56f, 0.62f);
            // 1.0.5~ 패드 십자키 좌우: 카드 고르기 (커서도 그 카드로 옮겨, A 를 누르면 확정)
            int pad = GamePad.TakeDpadX();
            if (pad != 0)
            {
                Select(selected < 0 ? (pad > 0 ? 0 : cards.Length - 1) : Mathf.Clamp(selected + pad, 0, cards.Length - 1));
                if (selected >= 0) GamePad.WarpTo(cards[selected]);
            }
            if (selected >= 0 && KeyBindings.Down(GameAction.Interact)) Confirm();
            yield return null;
        }

        // 5) 고른 카드가 가운데로 모이고 빛이 터짐
        fx?.Play("railgun", 0.6f, 0.8f);
        fx?.Play("shimmer", 0.8f, 1f);
        fx?.Shake(0.4f, 0.3f);
        confirmButton.gameObject.SetActive(false);
        RectTransform chosen = cards[selected];
        Vector2 from = chosen.anchoredPosition;
        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        {
            float k = EaseOut(t / 0.6f);
            chosen.anchoredPosition = Vector2.Lerp(from, new Vector2(0f, -70f), k);     // 1.1~ 결과 글자(제목 · 부제)를 가리지 않게 아래로 · 덜 키움 (윗변 298)
            chosen.localScale = Vector3.one * Mathf.Lerp(1.08f, 1.15f, k);
            for (int i = 0; i < cards.Length; i++)
            {
                if (i == selected) continue;
                CanvasGroup g = cards[i].GetComponent<CanvasGroup>();
                g.alpha = 1f - k;
                cards[i].localScale = Vector3.one * (1f - 0.4f * k);
            }
            flash.color = new Color(1f, 0.92f, 0.7f, 0.6f * Mathf.Sin(k * Mathf.PI) * FlashMul);
            if (Random.value < 0.6f) Spark(chosen.anchoredPosition, Random.insideUnitCircle.normalized * Random.Range(300f, 900f), 0.6f);
            yield return null;
        }
        title.text = sp.EvolutionName(options[selected], tier);
        subtitle.text = Loc.T("무기가 진화했습니다! 이제 이 무기와 함께 싸웁니다");
        for (int i = 0; i < 40; i++) Spark(new Vector2(0f, -70f), Random.insideUnitCircle.normalized * Random.Range(500f, 1300f), Random.Range(0.5f, 0.9f));
        // 1.0.4~: 결과 화면 1.1초도 클릭 · [상호작용 키]로 넘김 (확정한 그 입력은 받지 않게 0.15초 뒤부터)
        float resultAt = Time.unscaledTime;
        while (Time.unscaledTime - resultAt < 1.1f * GameSettings.FxTime && !SkipPressed(resultAt)) yield return null;

        // 6) 사라짐
        CanvasGroup all = root.gameObject.AddComponent<CanvasGroup>();
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            all.alpha = 1f - t / 0.4f;
            yield return null;
        }
    }

    void Select(int i)
    {
        if (confirmed || i < 0 || i >= cards.Length) return;
        if (selected != i) SpecialAbilities.SharedFx?.Play("ding", 0.5f, 1f + 0.1f * i);
        selected = i;
    }

    void Confirm()
    {
        if (selected < 0 || confirmed) return;
        confirmed = true;
    }

    // ================================================================= 화면 만들기
    void Build()
    {
        dim = Img("Dim", root, Vector2.zero, Vector2.zero, null, Color.clear);
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;           // 뒤의 화면을 누르지 못하게

        burst = Img("Burst", root, Vector2.zero, new Vector2(900f, 900f), sp.glowSprite, Color.clear);
        burst2 = Img("Burst2", root, new Vector2(0f, -40f), new Vector2(1700f, 700f), sp.glowSprite, new Color(Soul.r, Soul.g, Soul.b, 0.08f));

        title = UIKit.Text(root, "", 72f, Gold, new Vector2(0f, 395f), new Vector2(1500f, 100f));      // 1.1~ 380 → 395 (부제 · 카드와 띄움)
        title.text = tier >= 3 ? Loc.T("무기 각성") : tier >= 2 ? Loc.T("무기 최종 진화") : Loc.T("무기 진화");
        title.fontStyle = FontStyles.Bold;
        // 1.4.8~ 제목 아래 금 장식선 (ui_divider · 3배 도트) — 제목의 자식이라 켜짐 · 커짐을 따라감 · 투명도는 제목과 함께 바꿈 · 부제(320)와 안 겹치게 352
        if (UIKit.DividerSprite != null)
            titleLine = Img("TitleLine", title.rectTransform, new Vector2(0f, -43f), new Vector2(384f, 24f), UIKit.DividerSprite, Color.white);
        title.gameObject.SetActive(false);
        subtitle = UIKit.Text(root, "", 30f, Parch, new Vector2(0f, 320f), new Vector2(1500f, 50f));       // 1.1~ 305 → 320 (커진 카드에 안 가리게)
        subtitle.text = Loc.T("보스의 영혼이 무기에 스며든다. 한 갈래를 고르세요 (되돌릴 수 없음)");
        subtitle.gameObject.SetActive(false);

        cards = new RectTransform[options.Length];
        cardFrames = new Image[options.Length];
        for (int i = 0; i < options.Length; i++) BuildCard(i);

        confirmButton = UIKit.MakeButton(root, "", new Vector2(0f, -440f), new Vector2(560f, 76f), Confirm, 28f);
        confirmText = confirmButton.GetComponentInChildren<TMP_Text>();
        confirmText.text = Loc.T("진화한다 [") + KeyBindings.Name(GameAction.Interact) + "]";
        confirmButton.gameObject.SetActive(false);

        flash = Img("Flash", root, Vector2.zero, Vector2.zero, null, Color.clear);
        Stretch(flash.rectTransform);
    }

    void BuildCard(int i)
    {
        int id = options[i];
        RectTransform c = UIKit.Rect("Card" + i, root, Vector2.zero, new Vector2(440f, 640f));
        c.gameObject.AddComponent<CanvasGroup>();
        Image frame = c.gameObject.AddComponent<Image>();
        frame.sprite = UIKit.ButtonSprite;
        frame.type = Image.Type.Sliced;
        cardFrames[i] = frame;
        Button b = c.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        int idx = i;
        b.onClick.AddListener(() =>
        {
            if (GamePad.PadClickNow && selected == idx) Confirm();      // 1.0.5~ 패드: 고른 카드를 A 로 한 번 더 누르면 확정
            else Select(idx);
        });

        Image inner = Img("Inner", c, Vector2.zero, new Vector2(412f, 612f), null, new Color(0.08f, 0.06f, 0.12f, 0.97f));
        inner.raycastTarget = false;

        // 같은 계열을 한 번 더 고르면 '강화판'
        bool same = tier >= 2 && CharacterData.IsGunner && id == sp.GunEvo1;
        string tag = tier >= 3 ? Loc.T("각성") : !CharacterData.IsGunner ? (tier >= 2 ? Loc.T("추가 강화") : Loc.T("1차 진화"))
                   : tier >= 2 ? (same ? Loc.T("같은 계열 · 극대화") : Loc.T("새 계열로 분기")) : Loc.T("1차 진화");
        TMP_Text tg = UIKit.Text(c, "", 22f, same ? Gold : new Color(0.7f, 0.85f, 1f), new Vector2(0f, 285f), new Vector2(400f, 34f));
        tg.text = tag;

        Img("Glow", c, new Vector2(0f, 170f), new Vector2(260f, 260f), sp.glowSprite, new Color(Soul.r, Soul.g, Soul.b, 0.35f)).raycastTarget = false;
        Sprite weapon = sp.EvolutionIcon(id);
        Image icon = Img("Icon", c, new Vector2(0f, 170f), new Vector2(170f, 170f), weapon, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text name = UIKit.Text(c, "", 36f, Gold, new Vector2(0f, 55f), new Vector2(400f, 54f));
        name.text = sp.EvolutionName(id, tier);
        TMP_Text desc = UIKit.Text(c, "", 22f, Parch, new Vector2(0f, -55f), new Vector2(380f, 150f), TextAlignmentOptions.Top);
        // 미리보기: 이 무기가 어떻게 나가는지 작은 그림으로 되풀이
        EvoPreview.Attach(UIKit.Rect("Preview", c, new Vector2(0f, -205f), new Vector2(380f, 120f)), id, sp.glowSprite);
        desc.text = sp.EvolutionDesc(id, tier);
        TMP_Text key = UIKit.Text(c, "", 20f, new Color(0.6f, 0.56f, 0.62f), new Vector2(0f, -294f), new Vector2(200f, 28f));
        key.text = "[" + (i + 1) + "]";

        c.gameObject.SetActive(false);
        cards[i] = c;
    }

    Sprite CurrentSprite() => sp.MainWeaponIcon;

    // 튀는 빛 조각 (UI 위에서 날아가며 사라짐)
    void Spark(Vector2 from, Vector2 velocity, float life)
    {
        Image s = Img("Spark", root, from, Vector2.one * Random.Range(14f, 34f), sp.glowSprite, Color.Lerp(Soul, Gold, Random.value));
        s.raycastTarget = false;
        StartCoroutine(Fly(s, from, velocity, life));
    }

    IEnumerator Fly(Image s, Vector2 from, Vector2 velocity, float life)
    {
        Color c0 = s.color;
        bool gather = velocity == Vector2.zero;         // 속도가 없으면 가운데로 빨려 들어감
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

    static Image Img(string name, Transform parent, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        RectTransform r = UIKit.Rect(name, parent, pos, size);
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
