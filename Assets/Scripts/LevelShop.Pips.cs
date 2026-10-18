using UnityEngine;

// 1.1.2~ 레벨업 카드 맨 위 단계 칸 (LevelPips): 카드 글 끝의 「Lv 2 / 3」 대신
// 찬 칸 = 지금 단계 · 금빛으로 맥동하는 칸 = 이 카드를 고르면 찰 칸 · 빈칸 = 남은 단계
// 최대 단계가 없는 카드(관통 등 끝없는 카드 · 성장 카드 · 비상 보급)는 칸을 숨김
public partial class LevelShop
{
    // 카드(420x600) 위끝에서 30 아래 · 제목(위끝 56 아래부터)과 겹치지 않음
    const float CardPipsY = -30f;
    static readonly Vector2 CardPipsSize = new Vector2(300f, 16f);
    readonly LevelPips[] cardPips = new LevelPips[3];

    void ShowCardPips(int[] ids)
    {
        for (int slot = 0; slot < 3 && slot < ids.Length; slot++)
        {
            RectTransform card = CardOf(slot) as RectTransform;
            if (card == null) continue;
            if (cardPips[slot] == null)
            {
                cardPips[slot] = LevelPips.Create(card, Vector2.zero, CardPipsSize, "CardPips");
                RectTransform r = (RectTransform)cardPips[slot].transform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, CardPipsY);
            }
            int id = ids[slot];
            int max = id == SupplyId || IsGrowth(id) ? 0 : CapOf(id);      // 외길이면 늘어난 단계까지 (LevelShop.Paths)
            cardPips[slot].Set(LevelOf(id), max, 1);
        }
    }
}
