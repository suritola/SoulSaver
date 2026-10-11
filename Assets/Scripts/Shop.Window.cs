using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 떠돌이 상점 창 (1.0.8 개편): 씬에 놓인 옛 판은 숨기고 코드로 새로 그림 — 사고파는 로직 · 가격은 그대로
//   위: 제목 · 보유 코인 · 닫기 / 가운데: 강화 카드 2장 (공격력 · 이동 속도) / 아래: 물약 카드 4장 (그림 · 이름 · 효과 · 가격)
// 강화 글자는 원래 쓰던 priceTextInput · statTextInput(1 · 5)에 새 글자를 연결해 UpdateShopText 가 그대로 채움
public partial class Shop
{
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Dim = new Color(0.72f, 0.68f, 0.76f);
    static readonly string[] PotionIcons = { "potion_heal", "potion_shrink", "potion_haste", "potion_swift" };
    static readonly Color[] PotionTints =
    {
        new Color(0.86f, 0.28f, 0.32f), new Color(0.6f, 0.36f, 0.88f), new Color(0.94f, 0.75f, 0.2f), new Color(0.3f, 0.78f, 0.9f),
    };

    TextMeshProUGUI coinLabel;
    TextMeshProUGUI[] potionName, potionDesc, potionCost;
    Button[] potionButtons;
    Button buyAttack, buyMove;

    void BuildWindow()
    {
        if (shopPanel == null) return;
        for (int i = 0; i < Potions.Length; i++) potionPrice[i] = Potions[i].price;
        UIKit.EnsureStyle();

        // 옛 판(창 · 줄 · 제목 · 코인 · 안내)은 숨김. 화면 전체를 어둡게 덮는 판은 그대로
        foreach (Transform c in shopPanel.transform)
        {
            RectTransform r = c as RectTransform;
            if (r != null && r.anchorMin == r.anchorMax) c.gameObject.SetActive(false);
        }
        mycoins = null;

        const float W = 1320f, H = 800f;
        RectTransform win = UIKit.Rect("ShopWindow", shopPanel.transform, Vector2.zero, new Vector2(W, H));
        Img(win, UIKit.ButtonSprite, new Color(0.66f, 0.52f, 0.3f), true);
        RectTransform inner = UIKit.Rect("Inner", win, Vector2.zero, new Vector2(W - 16f, H - 16f));
        Img(inner, null, new Color(0.075f, 0.06f, 0.1f, 0.98f), false);

        // 머리띠: 제목 · 코인 · 닫기
        RectTransform head = UIKit.Rect("Head", inner, new Vector2(0f, H * 0.5f - 62f), new Vector2(W - 16f, 108f));
        Img(head, null, new Color(0.17f, 0.12f, 0.2f, 1f), false);
        TMP_Text title = UIKit.Text(head, "떠돌이 상점", 50f, Gold, new Vector2(-W * 0.5f + 230f, 8f), new Vector2(420f, 64f), TextAlignmentOptions.Left);
        title.text = Loc.T("떠돌이 상점");
        TMP_Text sub = UIKit.Text(head, "", 20f, Dim, new Vector2(-W * 0.5f + 330f, -32f), new Vector2(620f, 30f), TextAlignmentOptions.Left);
        sub.text = Loc.T("코인으로 능력을 영구히 올리고, 물약으로 잠시 힘을 빌립니다");

        RectTransform coinBox = UIKit.Rect("Coins", head, new Vector2(250f, 0f), new Vector2(260f, 70f));
        Img(coinBox, UIKit.ButtonSprite, new Color(0.28f, 0.22f, 0.12f), true);
        Icon(coinBox, "Icons/menu_coin", new Vector2(-88f, 0f), 50f);
        coinLabel = (TextMeshProUGUI)UIKit.Text(coinBox, "", 34f, Gold, new Vector2(30f, 0f), new Vector2(170f, 56f), TextAlignmentOptions.Left);
        UIKit.MakeButton(head, "닫기 [P]", new Vector2(W * 0.5f - 120f, 0f), new Vector2(170f, 64f), ToggleShop, 24f);

        // 강화
        Section(inner, "강화", new Vector2(-W * 0.5f + 90f, 238f));
        UpgradeCard(inner, new Vector2(-322f, 140f), "Icons/shop_attack", new Color(0.95f, 0.5f, 0.35f), OnPressB1, out statTextInput, out priceTextInput, out buyAttack);
        UpgradeCard(inner, new Vector2(322f, 140f), "Icons/shop_move", new Color(0.45f, 0.85f, 1f), OnPressB5, out statTextInput5, out priceTextInput5, out buyMove);

        // 물약
        Section(inner, "물약", new Vector2(-W * 0.5f + 90f, 22f));
        potionName = new TextMeshProUGUI[Potions.Length];
        potionDesc = new TextMeshProUGUI[Potions.Length];
        potionCost = new TextMeshProUGUI[Potions.Length];
        potionButtons = new Button[Potions.Length];
        for (int i = 0; i < Potions.Length; i++) PotionCard(inner, i, new Vector2(-480f + i * 320f, -160f));
        UpdateShopText();
    }

    void UpgradeCard(RectTransform parent, Vector2 pos, string icon, Color tint, UnityEngine.Events.UnityAction buy,
                     out TextMeshProUGUI stat, out TextMeshProUGUI price, out Button button)
    {
        RectTransform card = UIKit.Rect("Upgrade", parent, pos, new Vector2(620f, 156f));
        Img(card, UIKit.ButtonSprite, new Color(tint.r * 0.45f, tint.g * 0.45f, tint.b * 0.45f), true);
        RectTransform bg = UIKit.Rect("Bg", card, Vector2.zero, new Vector2(608f, 144f));
        Img(bg, null, new Color(0.12f, 0.1f, 0.16f, 1f), false);
        RectTransform frame = UIKit.Rect("IconFrame", card, new Vector2(-236f, 0f), new Vector2(112f, 112f));
        Img(frame, null, new Color(tint.r * 0.3f, tint.g * 0.3f, tint.b * 0.3f, 1f), false);
        Icon(frame, icon, Vector2.zero, 96f);
        stat = (TextMeshProUGUI)UIKit.Text(card, "", 26f, Color.white, new Vector2(-10f, 0f), new Vector2(320f, 130f), TextAlignmentOptions.Left);
        stat.richText = true;
        stat.enableAutoSizing = true;
        stat.fontSizeMin = 16f;
        stat.fontSizeMax = 26f;
        button = UIKit.MakeButton(card, "", new Vector2(222f, 0f), new Vector2(150f, 108f), buy, 22f);
        price = button.GetComponentInChildren<TextMeshProUGUI>();
        price.richText = true;
    }

    void PotionCard(RectTransform parent, int i, Vector2 pos)
    {
        Color tint = PotionTints[i];
        RectTransform card = UIKit.Rect("Potion", parent, pos, new Vector2(300f, 330f));
        Img(card, UIKit.ButtonSprite, new Color(tint.r * 0.45f, tint.g * 0.45f, tint.b * 0.45f), true);
        RectTransform bg = UIKit.Rect("Bg", card, Vector2.zero, new Vector2(288f, 318f));
        Img(bg, null, new Color(0.12f, 0.1f, 0.16f, 1f), false);
        RectTransform glow = UIKit.Rect("Glow", card, new Vector2(0f, 92f), new Vector2(150f, 150f));
        Image g = Img(glow, SpecialAbilities.GlowSprite, new Color(tint.r, tint.g, tint.b, 0.35f), false);
        g.raycastTarget = false;
        Icon(card, "Icons/" + PotionIcons[i], new Vector2(0f, 92f), 112f);
        potionName[i] = (TextMeshProUGUI)UIKit.Text(card, "", 28f, Gold, new Vector2(0f, 8f), new Vector2(270f, 40f));
        potionDesc[i] = (TextMeshProUGUI)UIKit.Text(card, "", 20f, Dim, new Vector2(0f, -46f), new Vector2(264f, 64f));
        potionDesc[i].enableWordWrapping = true;
        potionDesc[i].enableAutoSizing = true;
        potionDesc[i].fontSizeMin = 14f;
        potionDesc[i].fontSizeMax = 20f;
        int id = i;
        potionButtons[i] = UIKit.MakeButton(card, "", new Vector2(0f, -122f), new Vector2(240f, 62f), () => BuyPotion(id), 22f);
        potionCost[i] = potionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
    }

    void Section(RectTransform parent, string ko, Vector2 pos)
    {
        TMP_Text t = UIKit.Text(parent, "", 30f, Gold, pos + new Vector2(60f, 0f), new Vector2(240f, 44f), TextAlignmentOptions.Left);
        t.text = "◆ " + Loc.T(ko);
        RectTransform line = UIKit.Rect("Line", parent, new Vector2(70f, pos.y - 26f), new Vector2(1180f, 3f));
        Img(line, null, new Color(Gold.r, Gold.g, Gold.b, 0.35f), false);
    }

    static Image Img(RectTransform r, Sprite sprite, Color color, bool sliced)
    {
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        if (sliced && sprite != null) img.type = Image.Type.Sliced;
        img.color = color;
        return img;
    }

    static void Icon(RectTransform parent, string path, Vector2 pos, float size)
    {
        Sprite s = Resources.Load<Sprite>(path);
        if (s == null) return;
        RectTransform r = UIKit.Rect("Icon", parent, pos, new Vector2(size, size));
        Image img = Img(r, s, Color.white, false);
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    // UpdateShopText 끝에서: 코인 · 강화 글자 다듬기 · 물약 칸
    void WindowText()
    {
        if (coinLabel != null && coind != null) coinLabel.text = coind.coins.ToString();
        Polish(statTextInput, priceTextInput, buyAttack);
        Polish(statTextInput5, priceTextInput5, buyMove);
        PotionText();
    }

    // "이름\n지금 -> 다음" → 금빛 이름 + 크게 "지금 → 다음", 살 수 없으면 단추를 흐리게
    void Polish(TextMeshProUGUI stat, TextMeshProUGUI price, Button b)
    {
        if (stat == null) return;
        string t = stat.text.Replace(" -> ", "  →  ").Replace("-> ", "→ ");
        int nl = t.IndexOf('\n');
        if (nl > 0 && !t.StartsWith("<color")) t = "<color=#F5D478>" + t.Substring(0, nl) + "</color>\n<size=115%>" + t.Substring(nl + 1) + "</size>";
        stat.text = t;
        if (price != null && b != null)
        {
            bool maxed = price.text == Loc.T("최대");
            b.interactable = !maxed;
        }
    }
}
