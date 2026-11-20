using UnityEngine;

// 번역표 (2.0.6~): 영혼 제단 · 유산 (SoulLegacy) · 영웅 각성 (HeroAwaken) · 보스 러시 (BossRushRun) — 키 앞글자 meta. · legacy. · awaken. · rush.
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddMetaLines()
    {
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ---------------- 영혼 제단 · 유산
        K("meta.title", "영혼 제단", "Soul Altar", "魂の祭壇", "灵魂祭坛");
        K("meta.sub", "업적을 이루면 유산이 깨어납니다. 깨어난 유산 중 셋을 지니고 싸우세요 (저절로 발동)", "Feats awaken legacies. Carry up to three into battle (they trigger on their own)", "偉業を成すと遺産が目覚めます。目覚めた遺産を3つまで携えて戦いましょう（自動で発動）", "达成伟业即可唤醒遗产。最多携带三个参战（自动触发）");
        K("legacy.slots", "지닌 유산 {0} / {1}", "Legacies carried {0} / {1}", "携えた遺産 {0} / {1}", "携带遗产 {0} / {1}");
        K("legacy.open", "깨어남", "Awakened", "目覚めた", "已唤醒");
        K("legacy.cond", "조건", "Condition", "条件", "条件");
        K("legacy.equip", "지니기", "Carry", "携える", "携带");
        K("legacy.unequip", "내려놓기", "Put down", "外す", "卸下");
        K("legacy.endure.proc", "불굴! 쓰러지지 않았다", "Indomitable! You refuse to fall", "不屈！倒れなかった", "不屈！你没有倒下");

        K("legacy.blood", "피의 맹세", "Blood Oath", "血の誓い", "血之誓约");
        K("legacy.blood.d", "적을 쓰러뜨리면 3% 확률로 최대 체력 2% 회복", "Kills have a 3% chance to heal 2% max HP", "敵を倒すと3%の確率で最大体力の2%回復", "击杀敌人时有3%几率恢复2%最大生命");
        K("legacy.blood.c", "적 20,000마리 처치 (누적)", "Defeat 20,000 enemies (total)", "敵を20,000体撃破（累計）", "累计击败20,000名敌人");
        K("legacy.endure", "불굴", "Indomitable", "不屈", "不屈");
        K("legacy.endure.d", "판마다 한 번, 쓰러질 피해를 받으면 체력 30%로 버팀", "Once per run, survive a fatal blow with 30% HP", "1回のプレイにつき1度、致命傷を体力30%で耐える", "每局一次，受到致命伤害时以30%生命存活");
        K("legacy.endure.c", "어려움 클리어", "Clear Hard", "ハードをクリア", "通关困难");
        K("legacy.hunter", "왕 사냥꾼", "Kingslayer", "王狩り", "弑王者");
        K("legacy.hunter.d", "보스에게 주는 피해 +15%", "+15% damage to bosses", "ボスへのダメージ +15%", "对首领伤害 +15%");
        K("legacy.hunter.c", "영웅 5명으로 이야기 모드 클리어", "Clear Story with 5 heroes", "5人の英雄でストーリーをクリア", "用5名英雄通关故事模式");
        K("legacy.harvest", "영혼 수확", "Soul Harvest", "魂の収穫", "灵魂收割");
        K("legacy.harvest.d", "보스를 쓰러뜨리면 필살기 게이지가 가득 참", "Defeating a boss fills your Ultimate gauge", "ボスを倒すと必殺技ゲージが満タンに", "击败首领时必杀技充满");
        K("legacy.harvest.c", "무한 모드 클리어", "Clear Endless", "エンドレスをクリア", "通关无尽模式");
        K("legacy.breath", "숨 고르기", "Second Wind", "息を整える", "调息");
        K("legacy.breath.d", "레벨업할 때마다 최대 체력 3% 회복", "Heal 3% max HP on every level-up", "レベルアップごとに最大体力の3%回復", "每次升级恢复3%最大生命");
        K("legacy.breath.c", "쉬움 클리어", "Clear Easy", "イージーをクリア", "通关简单");
        K("legacy.pioneer", "선구자", "Pathfinder", "先駆者", "先驱者");
        K("legacy.pioneer.d", "판 시작 레벨업 카드 +1 (이야기 모드)", "+1 level-up card at run start (Story)", "開始時のレベルアップカード +1（ストーリー）", "开局升级卡 +1（故事模式）");
        K("legacy.pioneer.c", "영웅 한 명의 숙련을 최대로", "Max out one hero's Mastery", "英雄1人の熟練を最大に", "将一名英雄的熟练度练满");
        K("legacy.echo", "역류의 잔향", "Echo of the Reflux", "逆流の残響", "逆流余响");
        K("legacy.echo.d", "체력 30% 이하일 때 받는 피해 -25%", "-25% damage taken while at or below 30% HP", "体力30%以下のとき被ダメージ -25%", "生命不高于30%时受到伤害 -25%");
        K("legacy.echo.c", "역류 클리어", "Clear Reflux", "逆流をクリア", "通关逆流");
        K("legacy.rush", "연전의 기억", "Memory of the Gauntlet", "連戦の記憶", "连战之忆");
        K("legacy.rush.d", "보스를 쓰러뜨리면 최대 체력 20% 회복", "Defeating a boss heals 20% max HP", "ボスを倒すと最大体力の20%回復", "击败首领时恢复20%最大生命");
        K("legacy.rush.c", "보스 러시 클리어", "Clear Boss Rush", "ボスラッシュをクリア", "通关首领连战");

        // ---------------- 영웅 각성
        K("awaken.btn", "각성 {0} / {1}", "Awaken {0} / {1}", "覚醒 {0} / {1}", "觉醒 {0} / {1}");
        K("awaken.title", "{0} 각성", "Awaken: {0}", "{0}の覚醒", "{0} 觉醒");
        K("awaken.sub", "이 영웅으로 조건을 이루면 영구히 진화합니다 (저절로 발동)", "Fulfill feats with this hero to evolve it permanently (triggers on its own)", "この英雄で条件を満たすと永久に進化します（自動で発動）", "用这位英雄达成条件即可永久进化（自动触发）");
        K("awaken.tier", "{0}단계", "Tier {0}", "{0}段階", "第{0}阶");
        K("awaken.e1", "판 시작 레벨업 카드 +1 (이야기 모드)", "+1 level-up card at run start (Story)", "開始時のレベルアップカード +1（ストーリー）", "开局升级卡 +1（故事模式）");
        K("awaken.c1", "이 영웅으로 이야기 모드 클리어", "Clear Story with this hero", "この英雄でストーリーをクリア", "用这位英雄通关故事模式");
        K("awaken.e2", "필살기를 쓰면 최대 체력 8% 회복", "Using your Ultimate heals 8% max HP", "必殺技を使うと最大体力の8%回復", "使用必杀技时恢复8%最大生命");
        K("awaken.c2", "이 영웅으로 이야기 모드 보스 네 종 처치", "Defeat all four Story bosses with this hero", "この英雄でストーリーのボス4種を撃破", "用这位英雄击败故事模式全部四种首领");
        K("awaken.e3", "판 시작 때 필살기 게이지 절반", "Start each run with half an Ultimate gauge", "開始時に必殺技ゲージが半分", "开局必杀技充能一半");
        K("awaken.c3", "이 영웅으로 필살기 300번", "Use this hero's Ultimate 300 times", "この英雄で必殺技を300回", "用这位英雄使用必杀技300次");
        K("awaken.e4", "체력 40% 이하일 때 받는 피해 -20%", "-20% damage taken while at or below 40% HP", "体力40%以下のとき被ダメージ -20%", "生命不高于40%时受到伤害 -20%");
        K("awaken.c4", "이 영웅으로 무한 모드 15분 버티기", "Survive 15 minutes in Endless with this hero", "この英雄でエンドレスを15分生き残る", "用这位英雄在无尽模式坚持15分钟");
        K("awaken.e5", "판 시작부터 1차 무기 진화", "Start each run with a 1st weapon evolution", "開始時から1次武器進化", "开局即进行第一次武器进化");
        K("awaken.c5", "이 영웅의 숙련을 최대로", "Max out this hero's Mastery", "この英雄の熟練を最大に", "将这位英雄的熟练度练满");

        // ---------------- 보스 러시
        K("rush.name", "보스 러시", "Boss Rush", "ボスラッシュ", "首领连战");
        K("rush.card", "보스 다섯을 차례로 쓰러뜨리세요.\n킹 슬라임 → 리치 왕 → 지옥의 군주 → 거울의 군주 → 개발자\n보스를 잡을 때마다 무기 진화 · 체력 30% 회복\n기록: 모두 쓰러뜨린 시간 (짧을수록 좋음)",
          "Defeat five bosses in a row.\nKing Slime → Lich King → Hell Lord → Mirror Lord → Developer\nEach kill: weapon evolution · heal 30%\nRecord: total clear time (faster is better)",
          "5体のボスを順番に倒しましょう。\nキングスライム → リッチキング → 地獄の君主 → 鏡の君主 → 開発者\nボスを倒すたびに武器進化・体力30%回復\n記録：全員を倒した時間（短いほど良い）",
          "依次击败五名首领。\n史莱姆王 → 巫妖王 → 地狱君主 → 镜之君主 → 开发者\n每击败一名首领：武器进化 · 恢复30%生命\n记录：全部击败的时间（越短越好）");
        K("rush.hub.hero", "메인 메뉴에서 고른 영웅으로 도전합니다", "You challenge with the hero selected in the main menu", "メインメニューで選んだ英雄で挑戦します", "使用主菜单中选择的英雄挑战");
        K("rush.hub.locked", "보통 난이도를 클리어하면 열립니다", "Unlocks after clearing Normal", "ノーマルをクリアすると解放", "通关普通难度后解锁");
        K("rush.hud.boss", "보스 {0}/{1}", "Boss {0}/{1}", "ボス {0}/{1}", "首领 {0}/{1}");
        K("rush.banner", "보스 러시", "Boss Rush", "ボスラッシュ", "首领连战");
        K("rush.banner.sub", "다섯 보스를 모두 쓰러뜨려라!", "Defeat all five bosses!", "5体のボスをすべて倒せ！", "击败全部五名首领！");
        K("rush.clear.title", "보스 러시 클리어!", "Boss Rush Cleared!", "ボスラッシュ クリア！", "首领连战 通关！");
        K("rush.result.fail", "보스 {0}/{1} · {2}에 쓰러짐", "Bosses {0}/{1} · fell at {2}", "ボス {0}/{1} · {2}で力尽きた", "首领 {0}/{1} · 于 {2} 倒下");
        K("rush.lb.rank", "보스 러시 순위", "Boss Rush Rank", "ボスラッシュ ランク", "首领连战排名");
    }
}
