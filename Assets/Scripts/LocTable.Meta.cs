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
        K("rush.name", "보스 러시", "Boss Rush", "ボスラッシュ", "首领连战");
        K("rush.card", "보스 다섯을 차례로 쓰러뜨리세요.\n리치 왕 → 지옥의 군주 → 킹 슬라임 → 거울의 군주 → 개발자\n보스를 잡을 때마다 무기 진화 · 체력 30% 회복\n기록: 모두 쓰러뜨린 시간 (짧을수록 좋음)",
          "Defeat five bosses in a row.\nLich King → Hell Lord → King Slime → Mirror Lord → Developer\nEach kill: weapon evolution · heal 30%\nRecord: total clear time (faster is better)",
          "5体のボスを順番に倒しましょう。\nリッチキング → 地獄の君主 → キングスライム → 鏡の君主 → 開発者\nボスを倒すたびに武器進化・体力30%回復\n記録：全員を倒した時間（短いほど良い）",
          "依次击败五名首领。\n巫妖王 → 地狱君主 → 史莱姆王 → 镜之君主 → 开发者\n每击败一名首领：武器进化 · 恢复30%生命\n记录：全部击败的时间（越短越好）");
        K("rush.hub.hero", "메인 메뉴에서 고른 영웅으로 도전합니다", "You challenge with the hero selected in the main menu", "メインメニューで選んだ英雄で挑戦します", "使用主菜单中选择的英雄挑战");
        K("rush.hub.locked", "보통 난이도를 클리어하면 열립니다", "Unlocks after clearing Normal", "ノーマルをクリアすると解放", "通关普通难度后解锁");
        K("rush.hud.boss", "보스 {0}/{1}", "Boss {0}/{1}", "ボス {0}/{1}", "首领 {0}/{1}");
        K("rush.banner", "보스 러시", "Boss Rush", "ボスラッシュ", "首领连战");
        K("rush.banner.sub", "다섯 보스를 모두 쓰러뜨려라!", "Defeat all five bosses!", "5体のボスをすべて倒せ！", "击败全部五名首领！");
        K("rush.clear.title", "보스 러시 클리어!", "Boss Rush Cleared!", "ボスラッシュ クリア！", "首领连战 通关！");
        K("rush.result.fail", "보스 {0}/{1} · {2}에 쓰러짐", "Bosses {0}/{1} · fell at {2}", "ボス {0}/{1} · {2}で力尽きた", "首领 {0}/{1} · 于 {2} 倒下");
        K("rush.lb.rank", "보스 러시 순위", "Boss Rush Rank", "ボスラッシュ ランク", "首领连战排名");
        K("awaken.buy", "각성 ({0}P)", "Awaken ({0}P)", "覚醒（{0}P）", "觉醒（{0}P）");
    }
}
