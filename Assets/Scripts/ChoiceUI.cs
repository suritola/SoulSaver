using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 여러 장 중 하나를 고르는 창 (1.0.1): 보물 상자 · 보스 보상 3택 · 저주 제단
// 카드마다 그림(Icons/choice_<이름>, tools/pixelart/make_choice_icons.py) · 이름 · 설명
// 누르거나 숫자 키로 고르고(금빛 테두리), 더블클릭 · 같은 숫자 한 번 더 · [Space] · [Enter]로 확정 (레벨업 카드와 같은 방식, 1.0.4~ 통일)
// 1.0.4~: 열린 직후 0.6초 · 마우스를 뗄 때까지는 고를 수 없음 (레벨업 창 LockSelection 과 같음 — 쏘던 클릭이 저주 제단 카드를 확정하던 문제)
// 열린 동안 게임이 멈추고 ESC 메뉴 · 보스 결계는 열리지 않음
public class ChoiceUI : MonoBehaviour
{
    public struct Option
    {
        public string name, desc;       // 한국어 원문 (번역은 UIKit.Text)
        public Color color;
        public string icon;             // Icons/choice_<icon> (1.1~ tree_ 로 시작하면 영혼 트리 그림 Icons/<icon>, IconPath)
        public Sprite sprite;           // 1.1~ 그림을 직접 줄 때 (있으면 icon 대신 · 능력치 · 레벨업 카드 아이콘 등)
        public int pipLevel, pipMax, pipAdd;    // 1.1.2~ 이름 아래 단계 칸 (LevelPips, pipMax 0 이면 없음)
        public Option(string name, string desc, Color color, string icon = null) { this.name = name; this.desc = desc; this.color = color; this.icon = icon; sprite = null; pipLevel = pipMax = pipAdd = 0; }
        public Option(string name, string desc, Color color, Sprite sprite) { this.name = name; this.desc = desc; this.color = color; icon = null; this.sprite = sprite; pipLevel = pipMax = pipAdd = 0; }
        // 단계 칸을 붙인 선택지 (now: 지금 단계 · max: 최대 · add: 고르면 차오를 칸)
        public Option WithPips(int now, int max, int add = 1) { pipLevel = now; pipMax = max; pipAdd = add; return this; }
    }

    // 1.1~ 보상마다 다른 그림을 쓰려고 영혼 트리 그림(tree_*)도 받음 (영혼 트리의 축복이 영혼 조각 그림을 같이 쓰던 문제)
    public static string IconPath(string icon) => icon.StartsWith("tree_") ? "Icons/" + icon : "Icons/choice_" + icon;

    public static bool Open { get; internal set; }      // 1.1~ internal: 전설 상자 룰렛(ChestRewards)도 켬

    // 1.1~ 안전장치: 창이 열린 채 씬이 바뀌면 코루틴이 사라져 Open 이 true 로 남음 → 다음 판의 고르기 창 · ESC 메뉴가 모두 막힘
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => Open = false;
    }

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.93f, 0.9f, 0.84f);
    const float DoubleClick = 0.35f;

    int selected = -1, confirmed = -1, lastClick = -1;
    float lastClickAt;
    const float SelectDelay = 0.6f;
    bool ready;                     // 1.0.4~ 열린 직후 입력 보호
    RectTransform[] cards;
    Image[] frames;

    // 1.1.9~ 설정 「카드 한 번 클릭으로 확정」 (튜토리얼은 두 단계 그대로)
    static bool OneClick => GameSettings.OneClickPick && !TutorialRun.Active;

    void Click(int i)
    {
        if (!ready) return;
        if (OneClick) { Select(i); confirmed = i; return; }
        float now = Time.unscaledTime;
        if (lastClick == i && now - lastClickAt <= DoubleClick) { confirmed = i; return; }
        if (GamePad.PadClickNow && selected == i) { confirmed = i; return; }        // 1.0.5~ 패드: 고른 카드를 A 로 한 번 더 누르면 확정
        lastClick = i;
        lastClickAt = now;
        Select(i);
    }

    void Select(int i)
    {
        if (i == selected) return;
        selected = i;
        Hostile.Play("pop", 0.4f, 1.3f);
        for (int k = 0; k < cards.Length; k++)
        {
            bool on = k == selected;
            frames[k].enabled = on;
            cards[k].localScale = Vector3.one * (on ? 1.06f : 1f);
        }
    }

    // 1.2~ 카드 아래 작은 버튼 하나 (영혼 각인 「지난 판 각인 그대로」): 고르면 onPick(options.Length) · 다음 Run 에서 한 번만 쓰고 지움
    //   숫자 키 [카드 수 + 1] · 패드 X 로도
    public static string ExtraLabel, ExtraTip;

    public static IEnumerator Run(string title, string sub, Option[] options, System.Action<int> onPick)
    {
        string extraLabel = ExtraLabel, extraTip = ExtraTip;
        ExtraLabel = ExtraTip = null;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null || options == null || options.Length == 0) { onPick?.Invoke(0); yield break; }
        while (Open) yield return null;                 // 다른 고르기 창이 떠 있으면 닫힐 때까지
        UIKit.EnsureStyle();
        float before = Time.timeScale;
        Time.timeScale = 0f;
        Open = true;
        TooltipUI.Hide();

        GameObject go = new GameObject("Choice", typeof(RectTransform));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.rootCanvas.transform, false);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.SetAsLastSibling();
        SoulTreeUI.OnTop(go, 620);
        ChoiceUI ui = go.AddComponent<ChoiceUI>();

        RectTransform back = UIKit.Rect("Back", r, Vector2.zero, Vector2.zero);
        back.anchorMin = Vector2.zero; back.anchorMax = Vector2.one;
        back.offsetMin = back.offsetMax = Vector2.zero;
        back.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.01f, 0.04f, 0.85f);

        UIKit.Text(r, title, 54f, Gold, new Vector2(0f, 355f), new Vector2(1400f, 80f));
        // 1.4.8~ 제목 아래 금 장식선 (ui_divider · 3배 도트) — 부제(292)와 안 겹치게 제목 아래끝(315) 바로 밑 · 그림이 없으면 없음
        if (UIKit.DividerSprite != null)
        {
            RectTransform dl = UIKit.Rect("TitleLine", r, new Vector2(0f, 320f), new Vector2(384f, 24f));
            Image di = dl.gameObject.AddComponent<Image>();
            di.sprite = UIKit.DividerSprite;
            di.raycastTarget = false;
        }
        if (!string.IsNullOrEmpty(sub)) UIKit.Text(r, sub, 28f, Parch, new Vector2(0f, 292f), new Vector2(1400f, 50f));
        UIKit.Text(r, GamePad.Active ? "십자키 ← → 로 고르고 [A]로 확정" : OneClick ? "카드를 누르면 바로 확정됩니다" : "더블클릭 · 같은 숫자 두 번 또는 [Space]로 확정", 24f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, -330f), new Vector2(1400f, 40f));

        const float w = 400f, h = 500f, gap = 44f;
        int n = options.Length;
        ui.cards = new RectTransform[n];
        ui.frames = new Image[n];
        float x0 = -(n - 1) * (w + gap) * 0.5f;
        for (int i = 0; i < n; i++)
        {
            int idx = i;
            Option o = options[i];
            RectTransform card = UIKit.Rect("Card", r, new Vector2(x0 + i * (w + gap), -20f), new Vector2(w, h));
            ui.cards[i] = card;

            // 고른 카드의 금빛 테두리 (카드보다 조금 크게, 카드 뒤)
            RectTransform fr = UIKit.Rect("Frame", card, Vector2.zero, new Vector2(w + 16f, h + 16f));
            Image fi = fr.gameObject.AddComponent<Image>();
            fi.sprite = UIKit.ButtonSprite;
            fi.type = Image.Type.Sliced;
            fi.color = Gold;
            fi.raycastTarget = false;
            fi.enabled = false;
            ui.frames[i] = fi;

            RectTransform face = UIKit.Rect("Face", card, Vector2.zero, new Vector2(w, h));
            Image ci = face.gameObject.AddComponent<Image>();
            ci.sprite = UIKit.ButtonSprite;
            ci.type = Image.Type.Sliced;
            ci.color = Color.Lerp(new Color(0.16f, 0.13f, 0.2f), o.color, 0.25f);
            Button b = face.gameObject.AddComponent<Button>();
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1.15f, 1.1f, 0.95f);
            cb.pressedColor = new Color(0.85f, 0.8f, 0.75f);
            b.colors = cb;
            b.onClick.AddListener(() => ui.Click(idx));

            // 그림 (뒤에 옅은 빛)
            Sprite icon = o.sprite != null ? o.sprite : string.IsNullOrEmpty(o.icon) ? null : Resources.Load<Sprite>(IconPath(o.icon));
            if (icon != null)
            {
                RectTransform glow = UIKit.Rect("Glow", face, new Vector2(0f, 120f), new Vector2(210f, 210f));
                Image gi = glow.gameObject.AddComponent<Image>();
                gi.sprite = SpecialAbilities.GlowSprite;
                gi.color = new Color(o.color.r, o.color.g, o.color.b, 0.35f);
                gi.raycastTarget = false;
                RectTransform ir = UIKit.Rect("Icon", face, new Vector2(0f, 120f), new Vector2(150f, 150f));
                Image ii = ir.gameObject.AddComponent<Image>();
                ii.sprite = icon;
                ii.preserveAspect = true;
                ii.raycastTarget = false;
            }
            // 1.4.8~ 이름은 한 줄 (길면 두 줄로 꺾여 그림을 덮지 않고 줄어듦) · 설명 글씨 26 → 하한 20 (예전 UIKit.Text 하한 12) · 줄마다 문단 간격 (레벨업 카드 TidyCards 와 같은 규칙)
            TMP_Text nameText = UIKit.Text(face, o.name, 38f, o.color, new Vector2(0f, 10f), new Vector2(w - 40f, 60f));
            nameText.enableWordWrapping = false;
            nameText.fontSizeMin = 24f;
            // 1.1.2~ 단계 칸이 있으면 이름 바로 아래에 두고 설명을 그만큼 내림 (설명은 UIKit.Text 의 자동 크기라 칸 안에서 줄어듦)
            bool pips = o.pipMax > 0;
            if (pips) LevelPips.Create(face, new Vector2(0f, -32f), new Vector2(w - 120f, 16f)).Set(o.pipLevel, o.pipMax, o.pipAdd);
            TMP_Text descText = UIKit.Text(face, o.desc, LevelShop.CardBodyMax, Parch, new Vector2(0f, pips ? -120f : -108f), new Vector2(w - 50f, pips ? 138f : 166f));
            descText.fontSizeMin = LevelShop.CardBodyMin;
            descText.paragraphSpacing = 10f;
            TermLinks.Apply(descText);      // 1.4.8~ 글 속 용어 툴팁 (Glossary)
            TMP_Text key = UIKit.Text(face, "", 22f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, -215f), new Vector2(w, 34f));
            key.text = "[" + (i + 1) + "]";
        }
        Hostile.Play("chime", 0.7f, 1.1f);

        if (!string.IsNullOrEmpty(extraLabel))
        {
            Button extra = UIKit.MakeButton(r, extraLabel, new Vector2(0f, -394f), new Vector2(420f, 56f), () => { if (ui.ready) ui.confirmed = n; }, 24f);
            extra.name = "ChoiceExtra";
            TMP_Text et = extra.GetComponentInChildren<TMP_Text>();
            if (et != null) { UIKit.Forget(et); et.text = Loc.T(extraLabel) + "  <size=70%><color=#A89C86>[" + (GamePad.Active ? "X" : (n + 1).ToString()) + "]</color></size>"; }
            if (!string.IsNullOrEmpty(extraTip))
            {
                TooltipTrigger tip = extra.gameObject.AddComponent<TooltipTrigger>();
                tip.titleProvider = () => Loc.T(extraLabel);
                tip.bodyProvider = () => Loc.T(extraTip);
            }
        }

        // 1.0.4~ 입력 보호: 흐리게 시작해 0.6초 동안 밝아지고, 마우스 버튼을 떼야 고를 수 있음
        CanvasGroup group = go.GetComponent<CanvasGroup>();
        if (group == null) group = go.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.alpha = LevelShop.LockAlpha;     // 1.4.8~ 0.6 → 0.85 (뒤 화면이 카드 글 위로 비치지 않게)
        float openedAt = Time.unscaledTime;
        float introEnd = CardIntro.Play(ui.cards);        // 1.4.1~ 카드 등장 애니메이션 — 다 나오기 전엔 고를 수 없음 (연달아 뜨는 창에서 잘못 누르기 방지)

        while (ui.confirmed < 0)
        {
            if (!ui.ready)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - openedAt) / SelectDelay);
                group.alpha = Mathf.Lerp(LevelShop.LockAlpha, 1f, t);
                if (t >= 1f && Time.unscaledTime >= introEnd && !Input.GetMouseButton(0) && !Input.GetMouseButton(1))
                {
                    ui.ready = true;
                    group.interactable = true;
                    group.alpha = 1f;
                }
                yield return null;
                continue;
            }
            if (!string.IsNullOrEmpty(extraLabel) && n < 9 && (Input.GetKeyDown(KeyCode.Alpha1 + n) || Input.GetKeyDown(KeyCode.Keypad1 + n) || GamePad.XDown)) ui.confirmed = n;
            for (int i = 0; i < n && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    if (ui.selected == i) ui.confirmed = i;         // 1.0.4~ 같은 숫자 한 번 더 → 확정
                    else if (OneClick) { ui.Select(i); ui.confirmed = i; }
                    else ui.Select(i);
                }
            // 1.0.5~ 패드 십자키 좌우: 카드 고르기 (커서도 그 카드로 옮겨, A 를 누르면 확정)
            int pad = GamePad.TakeDpadX();
            if (pad != 0)
            {
                int k = ui.selected < 0 ? (pad > 0 ? 0 : n - 1) : Mathf.Clamp(ui.selected + pad, 0, n - 1);
                ui.Select(k);
                GamePad.WarpTo(ui.cards[k]);
            }
            if (ui.confirmed < 0 && ui.selected >= 0 && (Input.GetKeyDown(KeyCode.Space) || KeyBindings.Down(GameAction.Interact) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                ui.confirmed = ui.selected;
            // 고른 카드는 숨 쉬듯 반짝
            if (ui.selected >= 0) ui.frames[ui.selected].color = Color.Lerp(Gold, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
            yield return null;
        }
        int pick = ui.confirmed;
        Destroy(go);
        Open = false;
        Time.timeScale = before > 0f ? before : 1f;
        Hostile.Play("pulse", 0.6f, 1.2f);
        onPick?.Invoke(pick);
    }
}
