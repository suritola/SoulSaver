using UnityEngine;

// 번역표 (2.0.6~): 영혼 제단 (MetaUpgrades) · 영웅 각성 · 보스 러시 — 키 앞글자 meta. · awaken. · rush.
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddMetaLines()
    {
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        K("meta.title", "영혼 제단", "Soul Altar", "魂の祭壇", "灵魂祭坛");
        K("meta.sub", "포인트를 바쳐 모든 영웅을 영구히 강화합니다", "Offer points to permanently empower every hero", "ポイントを捧げて、すべての英雄を永久に強化します", "献上点数，永久强化所有英雄");
        K("meta.points", "보유 포인트: {0}", "Points: {0}", "所持ポイント：{0}", "持有点数：{0}");
        K("meta.level", "{0} / {1} 단계", "Rank {0} / {1}", "{0} / {1} 段階", "{0} / {1} 阶");
        K("meta.max", "최대", "MAX", "最大", "已满");
        K("meta.atk", "공격력", "Attack", "攻撃力", "攻击力");
        K("meta.hp", "최대 체력", "Max HP", "最大体力", "最大生命");
        K("meta.spd", "이동 속도", "Move Speed", "移動速度", "移动速度");
        K("meta.exp", "경험치 획득", "EXP Gain", "経験値獲得", "经验获取");
        K("meta.pts", "포인트 획득", "Point Gain", "ポイント獲得", "点数获取");
        K("meta.shard", "영혼 조각 획득", "Soul Shard Gain", "魂のかけら獲得", "灵魂碎片获取");

        K("awaken.btn", "각성 {0} / {1}", "Awaken {0} / {1}", "覚醒 {0} / {1}", "觉醒 {0} / {1}");
        K("awaken.title", "{0} 각성", "Awaken: {0}", "{0}の覚醒", "{0} 觉醒");
        K("awaken.sub", "포인트로 이 영웅만 영구히 진화시킵니다", "Spend points to permanently evolve this hero", "ポイントでこの英雄だけを永久に進化させます", "花费点数永久进化这位英雄");
        K("awaken.tier", "{0}단계", "Tier {0}", "{0}段階", "第{0}阶");
        K("awaken.e1", "공격력 +5%", "Attack +5%", "攻撃力 +5%", "攻击力 +5%");
        K("awaken.e2", "판 시작 레벨업 카드 +1 (이야기 모드)", "+1 level-up card at run start (Story)", "開始時のレベルアップカード +1（ストーリー）", "开局升级卡 +1（故事模式）");
        K("awaken.e3", "최대 체력 +10%", "Max HP +10%", "最大体力 +10%", "最大生命 +10%");
        K("awaken.e4", "필살기 게이지 충전 +15%", "Ultimate charge +15%", "必殺技ゲージ充填 +15%", "必杀技充能 +15%");
        K("awaken.e5", "판 시작부터 1차 무기 진화", "Start each run with a 1st weapon evolution", "開始時から1次武器進化", "开局即进行第一次武器进化");
        K("awaken.buy", "각성 ({0}P)", "Awaken ({0}P)", "覚醒（{0}P）", "觉醒（{0}P）");
    }
}
