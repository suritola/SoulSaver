using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 떠돌이 상점 물약 (1.9.4~): 코인으로 사는 소모품. 상점 창 오른쪽에 한 줄로
//   회복 물약 (즉시) · 축소 물약 · 신속 물약 · 질풍 물약 (상점을 닫은 뒤 일정 시간)
// 효과가 남아 있는 동안은 같은 물약을 다시 살 수 없음 (회복 물약은 체력이 가득이면), 살 때마다 조금씩 비싸짐
public partial class Shop
{
    static readonly (string name, string desc, int price)[] Potions =
    {
        ("회복 물약", "체력 40% 회복", 12),
        ("축소 물약", "45초 동안 몸이 작아져 덜 맞음", 15),
        ("신속 물약", "45초 동안 공격 속도 +30%", 18),
        ("질풍 물약", "45초 동안 이동 속도 +25%", 12),
    };
    const float PotionPriceGrowth = 1.25f;
    // 회복 물약이 채우는 비율 (1.2~ 영혼의 무게 4: 40% → 24%)
    static float PotionHeal => 0.4f * SoulWeight.HealMul;

    readonly int[] potionPrice = new int[Potions.Length];

    // 1.0.1: 상점 창은 Shop.Window 가 새로 그림 (물약은 그림 카드)
    void BuildPotions() => BuildWindow();

    void BuyPotion(int id)
    {
        if (playerControllerd == null || PotionBlocked(id) || !TryPay(potionPrice[id])) return;
        potionPrice[id] = Mathf.CeilToInt(potionPrice[id] * PotionPriceGrowth);
        PotionBuffs buffs = PotionBuffs.Of(playerControllerd);
        switch (id)
        {
            case 0:
                playerControllerd.PlayerHealth = Mathf.Min(playerControllerd.PlayerMaxHealth, playerControllerd.PlayerHealth + playerControllerd.PlayerMaxHealth * PotionHeal);
                break;
            case 1: buffs.Begin(PotionBuffs.Kind.Shrink, 45f); break;
            case 2: buffs.Begin(PotionBuffs.Kind.Haste, 45f); break;
            case 3: buffs.Begin(PotionBuffs.Kind.Swift, 45f); break;
        }
        Hostile.Play("glassclink", 0.6f, 1.1f);
        UpdateShopText();
    }

    // 지금 살 수 없는 물약 (회복: 체력 가득 · 나머지: 효과가 남아 있음)
    bool PotionBlocked(int id)
    {
        if (playerControllerd == null) return true;
        if (id == 0) return playerControllerd.PlayerHealth >= playerControllerd.PlayerMaxHealth;
        PotionBuffs b = playerControllerd.GetComponent<PotionBuffs>();
        return b != null && b.Remaining((PotionBuffs.Kind)(id - 1)) > 0f;
    }

    void PotionText()
    {
        if (potionName == null) return;
        for (int i = 0; i < Potions.Length; i++)
        {
            if (potionName[i] == null) continue;
            string price = CoinPrice(Discounted(potionPrice[i]));      // 1.0.4~ 모자라면 빨갛게
            bool blocked = PotionBlocked(i);
            if (blocked)
            {
                PotionBuffs b = i > 0 && playerControllerd != null ? playerControllerd.GetComponent<PotionBuffs>() : null;
                price = b != null ? Loc.T("효과 중") + " " + Mathf.CeilToInt(b.Remaining((PotionBuffs.Kind)(i - 1))) + Loc.T("초") : Loc.T("체력 가득");
            }
            potionButtons[i].interactable = !blocked;
            potionName[i].text = Loc.T(Potions[i].name);
            potionDesc[i].text = i == 0 && SoulWeight.HealMul < 1f ? Loc.T("weight.potiondesc").Replace("{0}", Mathf.RoundToInt(PotionHeal * 100f).ToString()) : Loc.T(Potions[i].desc);
            potionCost[i].text = price;
        }
    }
}

// 물약 효과 (플레이어에 붙음). 게임 시간으로 세서 상점 · 일시정지 중에는 줄지 않음
public class PotionBuffs : MonoBehaviour
{
    public enum Kind { Shrink, Haste, Swift }
    const float ShrinkMul = 0.7f, HasteMul = 1.3f, SwiftMul = 1.25f;

    PlayerController player;
    readonly float[] left = new float[3];
    TextMeshProUGUI hud;

    public static PotionBuffs Of(PlayerController p)
    {
        PotionBuffs b = p.GetComponent<PotionBuffs>();
        if (b == null) { b = p.gameObject.AddComponent<PotionBuffs>(); b.player = p; }
        return b;
    }

    public float Remaining(Kind k) => left[(int)k];

    public void Begin(Kind k, float seconds)
    {
        if (player == null) player = GetComponent<PlayerController>();
        int i = (int)k;
        if (left[i] <= 0f) Apply(k, true);
        left[i] = seconds;
    }

    // 크기 · 속도는 곱했다가 나눔 (거대화 물약 같은 다른 효과와 겹쳐도 원래대로 돌아오게)
    void Apply(Kind k, bool on)
    {
        if (player == null) return;
        switch (k)
        {
            case Kind.Shrink: player.transform.localScale *= on ? ShrinkMul : 1f / ShrinkMul; break;
            case Kind.Haste: player.fireRateMultiplier *= on ? HasteMul : 1f / HasteMul; break;
            case Kind.Swift: player.speed *= on ? SwiftMul : 1f / SwiftMul; break;
        }
    }

    void Update()
    {
        string text = "";
        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] <= 0f) continue;
            left[i] -= Time.deltaTime;
            if (left[i] <= 0f) { left[i] = 0f; Apply((Kind)i, false); continue; }
            string name = i == 0 ? "축소 물약" : i == 1 ? "신속 물약" : "질풍 물약";
            text += (text.Length > 0 ? "\n" : "") + Loc.T(name) + "  " + Mathf.CeilToInt(left[i]) + Loc.T("초");
        }
        ShowHud(text);
    }

    // 왼쪽 가운데에 남은 시간
    void ShowHud(string text)
    {
        if (hud == null)
        {
            if (text.Length == 0) return;
            Canvas canvas = UIKit.HudCanvas();
            if (canvas == null) return;
            GameObject go = new GameObject("PotionBuffs", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(canvas.transform, false);
            // 1.1~ 첫 구매는 상점이 열린 채라 맨 뒤 자식으로 붙어 상점 창 위에 그려졌음 (UI 120% 에서 창 왼쪽과 겹침) → 상점 판보다 앞 순서로
            Shop shop = Object.FindFirstObjectByType<Shop>();
            if (shop != null && shop.shopPanel != null)
            {
                Transform s = shop.shopPanel.transform;
                while (s.parent != null && s.parent != canvas.transform) s = s.parent;
                if (s.parent == canvas.transform) r.SetSiblingIndex(s.GetSiblingIndex());
            }
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(30f, -40f);      // 왼쪽 위 능력 칸이 세 줄로 늘어도 겹치지 않게 조금 아래
            r.sizeDelta = new Vector2(360f, 120f);
            hud = go.GetComponent<TextMeshProUGUI>();
            UIKit.EnsureStyle();
            if (UIKit.Font != null) hud.font = UIKit.Font;
            if (UIKit.FontMaterial != null) hud.fontSharedMaterial = UIKit.FontMaterial;
            hud.fontSize = 24f;
            hud.alignment = TextAlignmentOptions.Left;
            hud.color = new Color(0.75f, 0.95f, 1f);
            hud.raycastTarget = false;
        }
        if (hud.text != text) hud.text = text;
        if (hud.gameObject.activeSelf != text.Length > 0) hud.gameObject.SetActive(text.Length > 0);
    }

    void OnDestroy()
    {
        if (hud != null) Destroy(hud.gameObject);
    }
}
