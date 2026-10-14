using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 여러 장 중 하나를 고르는 창 (1.0.1): 보물 상자 · 보스 보상 3택 · 저주 제단
// 카드마다 그림(Icons/choice_<이름>, tools/pixelart/make_choice_icons.py) · 이름 · 설명
// 누르거나 숫자 키로 고르고(금빛 테두리), 더블클릭 · [Space] · [Enter]로 확정 (레벨업 카드와 같은 방식)
// 열린 동안 게임이 멈추고 ESC 메뉴 · 보스 결계는 열리지 않음
public class ChoiceUI : MonoBehaviour
{
    public struct Option
    {
        public string name, desc;       // 한국어 원문 (번역은 UIKit.Text)
        public Color color;
        public string icon;             // Icons/choice_<icon>
        public Option(string name, string desc, Color color, string icon = null) { this.name = name; this.desc = desc; this.color = color; this.icon = icon; }
    }

    public static bool Open { get; private set; }

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.93f, 0.9f, 0.84f);
    const float DoubleClick = 0.35f;

    int selected = -1, confirmed = -1, lastClick = -1;
    float lastClickAt;
    RectTransform[] cards;
    Image[] frames;

    void Click(int i)
    {
        float now = Time.unscaledTime;
        if (lastClick == i && now - lastClickAt <= DoubleClick) { confirmed = i; return; }
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

    public static IEnumerator Run(string title, string sub, Option[] options, System.Action<int> onPick)
    {
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
        if (!string.IsNullOrEmpty(sub)) UIKit.Text(r, sub, 28f, Parch, new Vector2(0f, 292f), new Vector2(1400f, 50f));
        UIKit.Text(r, "더블클릭 또는 [{INTERACT}]로 확정", 24f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, -330f), new Vector2(1400f, 40f));

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
            Sprite icon = string.IsNullOrEmpty(o.icon) ? null : Resources.Load<Sprite>("Icons/choice_" + o.icon);
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
            UIKit.Text(face, o.name, 38f, o.color, new Vector2(0f, 10f), new Vector2(w - 40f, 60f));
            UIKit.Text(face, o.desc, 26f, Parch, new Vector2(0f, -105f), new Vector2(w - 50f, 160f));
            TMP_Text key = UIKit.Text(face, "", 22f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, -215f), new Vector2(w, 34f));
            key.text = "[" + (i + 1) + "]";
        }
        Hostile.Play("chime", 0.7f, 1.1f);

        while (ui.confirmed < 0)
        {
            for (int i = 0; i < n && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) ui.Select(i);
            if (ui.selected >= 0 && (Input.GetKeyDown(KeyCode.Space) || KeyBindings.Down(GameAction.Interact) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))     // 1.0.4~ 바꾼 상호작용 키도
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
