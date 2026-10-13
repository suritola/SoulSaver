using UnityEngine;

// 캐릭터마다 다른 상점 업그레이드
// 거너는 그대로 (공격 속도 · 재장전 속도 · 최대 탄창), 다른 캐릭터는 2~4번 버튼이 무기에 맞는 업그레이드로 바뀜
//   검사     베기 사거리 · 베기 각도 · 회전 베기 위력
//   도적     투척 속도 · 표창 회수 속도 · 표창 주머니 (효과는 거너와 같고 이름만)
//   궁수     시위 당기는 속도(공격 속도) · 관통력 · 화살비 위력
//   연금술사 투척 속도(그대로) · 폭발 범위 · 대폭발 플라스크 위력
public partial class Shop
{
    const int KitMaxBuys = 5;
    const int PierceMaxBuys = 3;
    const float KitPriceGrowth = 1.4f;
    readonly int[] kitBuys = new int[5];
    readonly int[] kitPrice = { 0, 0, 8, 8, 8 };

    static CharacterId Char => CharacterData.Selected;
    CharacterKit Kit => CharacterKit.Instance;

    // 공격력 버튼: 캐릭터 기본 배율만큼 오름 (거너 +0.25, 검사 +0.375 …)
    float KitDamageStep => damageStep * CharacterData.Current.damage;

    string KitDamageLabel => Char switch
    {
        CharacterId.Swordsman => "장검 공격력",
        CharacterId.Rogue => "표창 공격력",
        CharacterId.Archer => "화살 공격력",
        CharacterId.Alchemist => "플라스크 공격력",
        CharacterId.Cartographer => "깃펜 공격력",
        CharacterId.Clockmaker => "톱니 공격력",
        _ => "총알 공격력",
    };

    // 이 버튼이 캐릭터 전용 업그레이드인지 (아니면 원래 업그레이드를 씀)
    bool IsKitSlot(int slot) => Char switch
    {
        CharacterId.Swordsman => slot >= 2 && slot <= 4,
        CharacterId.Archer or CharacterId.Alchemist => slot == 3 || slot == 4,
        _ => false,
    };

    int KitMax(int slot) => Char == CharacterId.Archer && slot == 3 ? PierceMaxBuys : KitMaxBuys;
    bool KitMaxed(int slot) => kitBuys[slot] >= KitMax(slot);

    // 영혼 트리를 쓰는 캐릭터: 상점은 무기와 상관없는 기본 능력치(공격력 · 이동 속도)만 팜
    // 공격 속도 · 재장전 · 탄창은 무기마다 달라서 영혼 트리의 무기 가지로 옮김
    // 줄 셋을 숨기고, 남은 두 줄이 가운데 오도록 창을 줄임 (줄 간격 108)
    void BasicStatsOnly()
    {
        if (!SpecialAbilities.UsesEvolution || shopPanel == null) return;
        const float Row = 108f;
        foreach (Transform c in shopPanel.transform)
        {
            string n = c.name;
            if (n.StartsWith("ShootSpeed") || n.StartsWith("ReloadSpeed") || n.StartsWith("MaxBullet")) { c.gameObject.SetActive(false); continue; }
            RectTransform r = c as RectTransform;
            if (r == null) continue;
            if (n == "Window") { r.sizeDelta -= new Vector2(0f, Row * 3f); continue; }
            Vector2 p = r.anchoredPosition;
            // 위쪽(제목 · 코인 · 공격력)은 1.5줄 내리고, 아래쪽(이동 속도 · 닫기 안내)은 1.5줄 올림
            p.y += n.StartsWith("MoveSpeed") || n == "CloseHint" ? Row * 1.5f : -Row * 1.5f;
            r.anchoredPosition = p;
        }
    }

    // 캐릭터 전용 업그레이드면 사고 true (원래 업그레이드는 건너뜀)
    bool KitPress(int slot)
    {
        if (!IsKitSlot(slot) || Kit == null) return false;
        if (KitMaxed(slot) || !TryPay(kitPrice[slot])) return true;

        CharacterKit k = Kit;
        switch (Char)
        {
            case CharacterId.Swordsman:
                if (slot == 2) k.reachMul += 0.08f;
                if (slot == 3) k.arcBonus += 10f;
                if (slot == 4) k.ultMul += 0.1f;
                break;
            case CharacterId.Archer:
                if (slot == 3) playerControllerd.pene++;
                if (slot == 4) k.ultMul += 0.1f;
                break;
            case CharacterId.Alchemist:
                if (slot == 3) k.blastMul += 0.08f;
                if (slot == 4) k.ultMul += 0.1f;
                break;
        }
        kitBuys[slot]++;
        kitPrice[slot] = Mathf.CeilToInt(kitPrice[slot] * KitPriceGrowth);
        UpdateShopText();
        return true;
    }

    void KitShopText()
    {
        if (Char == CharacterId.Gunner) return;

        // 도적: 효과는 그대로, 이름만 표창에 맞게
        if (Char == CharacterId.Rogue)
        {
            Rename(statTextInput2, "투척 속도");
            Rename(statTextInput3, "표창 회수 속도");
            Rename(statTextInput4, "표창 주머니");
            return;
        }
        // 궁수 · 연금술사: 공격 속도 버튼은 그대로, 이름만
        if (Char == CharacterId.Archer)
        {
            Rename(statTextInput2, "시위 당기는 속도");
        }
        if (Char == CharacterId.Alchemist)
        {
            Rename(statTextInput2, "투척 속도");
        }

        CharacterKit k = Kit;
        if (k == null) return;
        for (int slot = 2; slot <= 4; slot++)
        {
            if (!IsKitSlot(slot)) continue;
            TextMeshProUGUIPair(slot, out TMPro.TextMeshProUGUI price, out TMPro.TextMeshProUGUI stat);
            bool maxed = KitMaxed(slot);
            if (price != null) price.text = PriceText(kitPrice[slot], maxed);
            if (stat == null) continue;
            string label, now, next;
            switch (Char)
            {
                case CharacterId.Swordsman when slot == 2:
                    label = "베기 사거리";
                    now = Cells(CharacterData.Current.range * k.reachMul);
                    next = Cells(CharacterData.Current.range * (k.reachMul + 0.08f));
                    break;
                case CharacterId.Swordsman when slot == 3:
                    label = "베기 각도";
                    now = Mathf.RoundToInt(100f + 2f * k.arcBonus) + "°";
                    next = Mathf.RoundToInt(100f + 2f * (k.arcBonus + 10f)) + "°";
                    break;
                case CharacterId.Archer when slot == 3:
                    label = "관통력";
                    now = Loc.T("적 ") + playerControllerd.pene + Loc.T("마리");
                    next = Loc.T("적 ") + (playerControllerd.pene + 1) + Loc.T("마리");
                    break;
                case CharacterId.Alchemist when slot == 3:
                    label = "폭발 범위";
                    now = Pct(k.blastMul);
                    next = Pct(k.blastMul + 0.08f);
                    break;
                default:
                    label = "필살기 위력";
                    now = Pct(k.ultMul);
                    next = Pct(k.ultMul + 0.1f);
                    break;
            }
            stat.text = maxed ? Loc.T(label) + "\n" + now + Loc.T(" (최대)") : Loc.T(label) + "\n" + now + " -> " + next;
        }
    }

    void TextMeshProUGUIPair(int slot, out TMPro.TextMeshProUGUI price, out TMPro.TextMeshProUGUI stat)
    {
        price = slot == 2 ? priceTextInput2 : slot == 3 ? priceTextInput3 : priceTextInput4;
        stat = slot == 2 ? statTextInput2 : slot == 3 ? statTextInput3 : statTextInput4;
    }

    static string Cells(float v) => v.ToString("0.#") + Loc.T("칸");
    static string Pct(float mul) => Mathf.RoundToInt(mul * 100f) + "%";

    // 버튼 글의 첫 줄(이름)만 바꿈
    static void Rename(TMPro.TextMeshProUGUI t, string label)
    {
        if (t == null) return;
        int i = t.text.IndexOf('\n');
        t.text = Loc.T(label) + (i >= 0 ? t.text.Substring(i) : "");
    }
}
