using System.Collections.Generic;
using UnityEngine;

// 끝없는 성장 (1.0.4~): 고를 레벨업 카드를 모두 배웠거나 최대 레벨(50 · 무한 모드 100)에 이르면
// 경험치가 멈추지 않고 계속 쌓이고, 레벨업마다 상한 없이 쌓이는 「성장」 카드 넷 중 하나를 고름
//   힘: 모든 무기 피해 +2% (기본 공격력 기준, 더하기) · 생명: 최대 체력 +5 · 손놀림: 공격 속도 +1% · 발걸음: 이동 속도 +1% (이동 속도 상한은 그대로)
// 한 장의 수치는 작게 (이야기 모드 밸런스는 그대로), 무한 모드처럼 오래 하는 판에서 쌓여 체감되게
// 카드 번호는 100 ~ 103 (능력 배열 밖이라 왼쪽 위 스킬 칸 · 진화 · 시너지에는 끼지 않음), 이어하기 기록은 다른 카드처럼 c<번호>
public partial class LevelShop
{
    public const int GrowthFirstId = 100, GrowthCount = 4;
    public static bool IsGrowth(int id) => id >= GrowthFirstId && id < GrowthFirstId + GrowthCount;

    const float GrowthAttack = 0.02f;       // 힘: 모든 무기 피해 +2% (1.1.2~ 평타 고점 하향: +3% → +2%)
    const float GrowthHp = 5f;              // 생명: 최대 체력 +5
    const float GrowthRate = 0.01f;         // 손놀림: 공격 속도 +1% (곱, 1.1.2~ +2% → +1%)
    const float GrowthMove = 0.01f;         // 발걸음: 이동 속도 +1% (곱)
    static readonly string[] GrowthNames = { "성장: 힘", "성장: 생명", "성장: 손놀림", "성장: 발걸음" };

    readonly int[] growthLevel = new int[GrowthCount];
    public int GrowthLevel(int k) => k >= 0 && k < GrowthCount ? growthLevel[k] : 0;
    public int GrowthPicks
    {
        get
        {
            int s = 0;
            foreach (int n in growthLevel) s += n;
            return s;
        }
    }
    public static string GrowthName(int k) => Loc.T(GrowthNames[Mathf.Clamp(k, 0, GrowthCount - 1)]);

    // 최대 레벨에 이름 (그 뒤 레벨업은 성장 카드만)
    public bool AtLevelCap
    {
        get
        {
            if (bul == null) bul = FindFirstObjectByType<PlayerController>();
            return bul != null && bul.level >= LevelCap;
        }
    }
    // 이번 레벨업은 성장 카드만 나옴 (만렙 표시도 이것으로)
    public bool GrowthOnly => NothingToLearn || AtLevelCap;

    // 한 창의 세 장이 겹치지 않게: 아직 안 나온 성장 카드에서 뽑음
    readonly List<int> growthDeck = new List<int>();

    void ResetGrowthDeck()
    {
        growthDeck.Clear();
        for (int k = 0; k < GrowthCount; k++) growthDeck.Add(GrowthFirstId + k);
    }

    int DrawGrowth()
    {
        if (growthDeck.Count == 0) ResetGrowthDeck();
        int i = Random.Range(0, growthDeck.Count);
        int id = growthDeck[i];
        growthDeck.RemoveAt(i);
        return id;
    }

    // 카드 칸에 쓸 이름 · 설명 (성장 카드는 능력 배열 밖이라 따로)
    string CardTitleOf(int id) => IsGrowth(id) ? GrowthName(id - GrowthFirstId) : ability_name[id];
    string CardBodyOf(int id) => IsGrowth(id) ? GrowthCardText(id - GrowthFirstId) : ability_content[id];

    // 아이콘: 능력치 창과 같은 그림 (공격력 · 공격 속도 · 이동 속도), 생명은 강철같은 심장
    Sprite GrowthIcon(int k) => k switch
    {
        0 => SpecialAbilities.StatIcon(0),
        1 => abilityHUD != null ? abilityHUD.GetIcon(8) : null,
        2 => SpecialAbilities.StatIcon(2),
        _ => SpecialAbilities.StatIcon(4),
    };

    // 다른 카드와 같은 모양 (한 문장 + 금색 "수치  지금 → 다음") + 「상한 없이 계속 쌓입니다」 + Lv
    string GrowthCardText(int k)
    {
        if (bul == null) bul = FindFirstObjectByType<PlayerController>();
        int n = GrowthLevel(k);
        string S(string ko) => Loc.T(ko);
        string Pct(float v) => "+" + (v * 100f).ToString("0.#") + "%";
        string text;
        switch (k)
        {
            case 0:
                text = CardText(S("모든 무기 피해가 2% 늘어납니다. (기본 공격력 기준)"), S("공격력"), Pct(GrowthAttack * n), Pct(GrowthAttack * (n + 1)), n, 0);
                break;
            case 1:
                {
                    float hp = bul != null ? bul.PlayerMaxHealth : 100f;
                    text = CardText(S("최대 체력이 5 늘어나고 그만큼 회복합니다."), S("최대 체력"),
                                    Mathf.RoundToInt(hp).ToString(), Mathf.RoundToInt(hp + GrowthHp).ToString(), n, 0);
                    break;
                }
            case 2:
                text = CardText(S("공격 속도가 1% 빨라집니다."), S("공격 속도"),
                                Pct(Mathf.Pow(1f + GrowthRate, n) - 1f), Pct(Mathf.Pow(1f + GrowthRate, n + 1) - 1f), n, 0);
                break;
            default:
                text = CardText(S("이동 속도가 1% 빨라집니다. (이동 속도 상한은 그대로)"), S("이동 속도"),
                                Pct(Mathf.Pow(1f + GrowthMove, n) - 1f), Pct(Mathf.Pow(1f + GrowthMove, n + 1) - 1f), n, 0);
                break;
        }
        return text + "\n<color=#9FE89F>" + S("상한 없이 계속 쌓입니다") + "</color>" + (n > 0 ? "   <color=#A89C86>Lv " + n + "</color>" : "");
    }

    // 성장 카드 효과 (고를 때 · 이어하기로 다시 적용할 때)
    void ApplyGrowth(int k)
    {
        if (k < 0 || k >= GrowthCount) return;
        if (bul == null) bul = FindFirstObjectByType<PlayerController>();
        if (bul == null) return;
        growthLevel[k]++;
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        switch (k)
        {
            case 0:
                if (sp != null) sp.GrowAttack(GrowthAttack);            // 영혼 트리 「무기 화력」과 같은 기준 (처음 공격력의 3%를 더함)
                else bul.damage *= 1f + GrowthAttack;
                break;
            case 1:
                bul.PlayerMaxHealth += GrowthHp;
                bul.PlayerHealth = Mathf.Min(bul.PlayerMaxHealth, bul.PlayerHealth + GrowthHp);
                break;
            case 2:
                if (sp != null) sp.GrowRate(1f / (1f + GrowthRate));     // 권총 · 모든 무기 발사 간격
                else bul.ShootSpeed /= 1f + GrowthRate;
                break;
            default:
                bul.speed *= 1f + GrowthMove;
                break;
        }
    }

    // 성장 카드를 처음 볼 때 한 번 안내
    void GrowthHint() => Hints.Show("growth", "최대 레벨(50 · 무한 모드 80) 전에 배울 레벨업 카드를 모두 배우면, 레벨업마다 끝없이 쌓이는 「성장」 카드를 고릅니다.");
}
