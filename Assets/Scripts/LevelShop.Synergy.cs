using TMPro;
using UnityEngine;

// 빌드 시너지 표시 (1.0.1): 레벨업 카드 중, 이미 가진 카드와 잘 어울리는 카드 아래에 「시너지 · 그 카드」
// 어떤 빌드를 노릴지 감이 오게 (예: 멀티 샷을 가졌으면 소각탄 · 전기탄 · 폭발 탄두 카드에 표시)
public partial class LevelShop
{
    // 서로 어울리는 카드 (한국어 카드 이름, 양쪽 모두에 표시)
    static readonly string[][] Synergies =
    {
        // 공용
        new[] { "코인충", "코인 자석" }, new[] { "피의 굶주림", "생명의 샘" },
        // 거너
        new[] { "멀티 샷", "소각탄" }, new[] { "멀티 샷", "전기탄" }, new[] { "멀티 샷", "폭발 탄두" },
        new[] { "관통하는 총알", "도탄 사격" }, new[] { "전기탄", "도탄 사격" }, new[] { "장전 충격파", "밀어내기" },
        // 검사
        new[] { "흡혈 베기", "연속 베기" }, new[] { "굳건한 자세", "칼바람" },
        // 도적
        new[] { "갈고리 표창", "급소 노리기" }, new[] { "갈고리 표창", "도탄 표창" }, new[] { "그림자 분신", "표창 폭풍" },
        // 궁수
        new[] { "사냥감 표식", "분열 화살" }, new[] { "사냥감 표식", "메아리 화살" }, new[] { "정조준", "강풍 화살" },
        // 연금술사
        new[] { "원소 융합", "급속 냉동" }, new[] { "원소 융합", "끈적한 산성" }, new[] { "연쇄 반응", "파편 플라스크" },
        // 1.0.4~ 새 영웅 4명 (예전엔 짝이 없어 「시너지!」가 거의 뜨지 않았음)
        // 지도 제작자
        new[] { "번지는 잉크", "굵은 펜촉" }, new[] { "측량 표식", "끊기지 않는 선" },
        // 시계공
        new[] { "빠른 태엽", "정각의 종" }, new[] { "튕기는 톱니", "녹슨 톱니" },
        // 우체부
        new[] { "등기 우편", "반송" }, new[] { "소포", "우표 수집" },
        // 유령
        new[] { "얼어붙는 손길", "얼음 파편" }, new[] { "꿰뚫는 냉기", "얼어붙는 손길" },
    };

    readonly TMP_Text[] synergyTags = new TMP_Text[3];
    readonly string[] synergyPartners = new string[3];       // 1.0.4~ 배지 툴팁용
    readonly float[] abilityBottom0 = new float[3];          // 1.1~ 카드 설명 칸 원래 아래 여백 (추천 + 시너지일 때만 올림)
    readonly bool[] abilityBottomKnown = new bool[3];

    // 지금 영웅의 카드 번호 (번역된 이름으로 찾음, 없으면 -1)
    int CardIdByName(string ko)
    {
        string t = Loc.T(ko);
        for (int i = 0; i < ability_name.Length; i++)
            if (ability_name[i] == t) return i;
        return -1;
    }

    // 이 카드와 어울리면서 이미 가진 카드 이름 (없으면 null)
    string SynergyPartner(int id)
    {
        if (id < 0 || id >= ability_name.Length || id == SupplyId) return null;
        foreach (string[] pair in Synergies)
            for (int k = 0; k < 2; k++)
            {
                if (CardIdByName(pair[k]) != id) continue;
                int other = CardIdByName(pair[1 - k]);
                if (other >= 0 && other != id && LevelOf(other) > 0) return ability_name[other];
            }
        return null;
    }

    void ShowSynergyTags(int[] ids)
    {
        for (int slot = 0; slot < 3; slot++)
        {
            Transform card = CardOf(slot);
            if (card == null) continue;
            if (synergyTags[slot] == null)
            {
                // 1.0.1: 연두 글씨만 있어서 잘 안 보였음 → 초록 배지 (HintBadge)
                RectTransform r = HintBadge.Make(card, "SynergyTag", Vector2.zero, new Vector2(400f, 52f),
                    new Color(0.04f, 0.26f, 0.1f, 0.96f), new Color(0.45f, 1f, 0.5f), 28f).transform.parent as RectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
                synergyTags[slot] = r.GetComponentInChildren<TMP_Text>();
                // 1.0.4~: 배지에 마우스를 올리면 뜻 설명 (배지는 카드 안이라 클릭은 그대로 카드로 감)
                UnityEngine.UI.Image rim = r.GetComponent<UnityEngine.UI.Image>();
                if (rim != null) rim.raycastTarget = true;
                int idx = slot;
                TooltipTrigger tip = r.gameObject.AddComponent<TooltipTrigger>();
                // 2026-10-19~ 갈래 규칙 판: 「공명 I까지 N」 · 갈림길 「교차 ○○」 배지 (LevelShop.Paths) — 자리 · 툴팁 코드는 그대로
                tip.titleProvider = () => Paths.Active ? Loc.T("갈래") : Loc.T("시너지");
                tip.bodyProvider = () => Paths.Active
                    ? Loc.T("갈래 단계(입문 1 + 갈래 카드 단계)가 4에 닿으면 공명 I, 8에 닿으면 공명 II가 열립니다. 두 갈래가 모두 공명 I에 닿으면 교차가 열립니다.")
                    : Loc.T("이미 가진 카드와 잘 어울리는 카드입니다. 함께 키우면 서로의 효과를 살려 줍니다.")
                                         + (synergyPartners[idx] != null ? "\n<color=#B6FF9E>" + Loc.T("어울리는 카드") + "  " + synergyPartners[idx] + "</color>" : "");
            }
            // 카드 아래 테두리에 걸침 (그 아래는 확정 안내 줄) · 추천 카드면 「★ 추천」 알약이 그 자리라 바로 위 카드 안쪽으로
            RectTransform tr = synergyTags[slot].transform.parent as RectTransform;
            tr.pivot = new Vector2(0.5f, slot == recommendedSlot ? 0f : 0.5f);
            tr.anchoredPosition = new Vector2(0f, slot == recommendedSlot ? 34f : 10f);
            string partner = !ShowSynergyIntro ? null : Paths.Active ? PathBadge(ids[slot]) : SynergyPartner(ids[slot]);
            synergyPartners[slot] = partner;
            synergyTags[slot].transform.parent.gameObject.SetActive(partner != null);
            if (partner != null) synergyTags[slot].text = Paths.Active ? "<color=#B6FF9E>" + partner + "</color>" : "<color=#B6FF9E>" + Loc.T("시너지") + "!</color> " + partner;
            LiftAbilityAbove(slot, partner != null && slot == recommendedSlot);
        }
    }

    // 1.1~ 추천 카드의 시너지 배지는 카드 안쪽(아래에서 34~89)에 놓여 긴 설명의 마지막 줄(수치 · Lv)을 덮었음
    //      → 그때만 설명 칸 아래끝을 58 올림 (설명은 자동 크기라 칸 안에서 줄어듦)
    void LiftAbilityAbove(int slot, bool lift)
    {
        TextMeshProUGUI ab = slot == 0 ? FirstAbility : slot == 1 ? SecondAbility : ThirdAbility;
        if (ab == null) return;
        RectTransform r = ab.rectTransform;
        if (!abilityBottomKnown[slot]) { abilityBottom0[slot] = r.offsetMin.y; abilityBottomKnown[slot] = true; }
        r.offsetMin = new Vector2(r.offsetMin.x, abilityBottom0[slot] + (lift ? 58f : 0f));
    }
}
