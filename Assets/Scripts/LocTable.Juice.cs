// 번역표 (1.1~): 손맛 묶음 + 레벨업 템포 (Juice · LevelShop.Chain)
// Juice 의 정적 생성자가 부름 (이미 있는 키는 건드리지 않음 · 공용 정적 생성자를 고치지 않으려고 따로)
public static partial class LocTable
{
    static bool juiceAdded;

    public static void EnsureJuice()
    {
        if (juiceAdded) return;
        juiceAdded = true;
        void A(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // 처치 수 이정표 배너 ({0} 처치 수 · {1} 코인)
        A("juice.milestone", "처치 {0}! 코인 +{1}", "{0} kills! +{1} coins", "{0}体撃破！コイン +{1}", "击败 {0}！金币 +{1}");
        // 쌓인 레벨업 이어 고르기 ({0} 지금 몇 번째 · {1} 모두)
        A("juice.chain", "레벨업 카드 {0}/{1}", "Level-up card {0}/{1}", "レベルアップカード {0}/{1}", "升级卡牌 {0}/{1}");
    }
}
