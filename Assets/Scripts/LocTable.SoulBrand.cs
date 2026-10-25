using UnityEngine;

// 번역표 (1.2~): 영혼 각인 (SoulBrand · ContentRegistry.SoulBrands)
// 열쇠는 brand. 접두어 (한국어 원문은 0번 칸) · 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
// 다른 번역표 파일을 고치지 않게 게임이 켜질 때 스스로 더함 (LocTable.Content 와 같은 방식)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddSoulBrandBoot() { if (!SceneFlow.DeferStartup(5, AddSoulBrand)) AddSoulBrand(); }      // 1.6.1~ 켤 때는 로딩 화면이 뜬 뒤로 (큰 함수는 불릴 때 통째로 JIT 되므로 작은 함수에서 미룸 · SceneFlow)

    static void AddSoulBrand()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 창 · 안내
        C("brand.title", "영혼 각인", "Soul Brand", "魂の刻印", "灵魂烙印");
        C("brand.pick.start", "이번 판에 새길 각인을 하나 고르세요.", "Choose a brand to carve into this run.", "このプレイに刻む刻印を1つ選んでください。", "请选择一个要刻入本局的烙印。");
        C("brand.pick.boss", "보스를 쓰러뜨렸다! 각인을 하나 더 새깁니다. ({0})", "Boss defeated! Carve one more brand. ({0})", "ボスを倒した！刻印をもう1つ刻みます。（{0}）", "击败了首领！再刻下一个烙印。（{0}）");
        C("brand.points", "포인트 ×{0}", "Points ×{0}", "ポイント ×{0}", "点数 ×{0}");
        C("brand.got", "영혼 각인: {0}", "Soul Brand: {0}", "魂の刻印：{0}", "灵魂烙印：{0}");
        C("brand.tag", "각인 {0}", "Brands {0}", "刻印 {0}", "烙印 {0}");      // 결과 화면 · ESC 요약 줄 (RunStats.Summary)
        C("brand.esc", "이번 판 각인", "This Run's Brands", "このプレイの刻印", "本局烙印");
        C("brand.esc.total", "포인트 배율 ×{0}", "Point multiplier ×{0}", "ポイント倍率 ×{0}", "点数倍率 ×{0}");
        C("brand.menu.how",
          "이야기 모드 판을 시작할 때 각인 하나를 고르고, 2장 · 3장 보스를 쓰러뜨릴 때마다 하나씩 더 새겨 한 판에 최대 3개까지 지닙니다. 각인마다 장점과 단점이 함께 있고, 어려운 각인일수록 포인트를 더 법니다.",
          "Choose a brand when you start a Story run, and carve one more each time you defeat the Chapter 2 and 3 bosses (up to 3 per run). Every brand has an upside and a downside, and harder brands earn more points.",
          "ストーリーモードのプレイ開始時に刻印を1つ選び、2章・3章のボスを倒すたびにもう1つずつ刻みます（1回のプレイで最大3つ）。刻印には長所と短所があり、厳しい刻印ほどポイントを多く稼げます。",
          "开始故事模式时选择一个烙印，每击败第2章 · 第3章首领时再刻下一个（每局最多3个）。每个烙印都有利有弊，越难的烙印获得的点数越多。");
        C("brand.menu.locked", "이야기 모드 「쉬움」을 클리어하면 영혼 각인이 열립니다.", "Clear Easy in Story mode to unlock Soul Brands.", "ストーリーモードの「イージー」をクリアすると魂の刻印が解放されます。", "通关故事模式「简单」难度即可解锁灵魂烙印。");
        C("brand.menu.count", "열린 각인 {0} / {1}", "Brands unlocked {0} / {1}", "解放した刻印 {0} / {1}", "已解锁烙印 {0} / {1}");
        C("brand.menu.unlock", "해금 조건", "To Unlock", "解放条件", "解锁条件");

        // ================================================================= 각인 12장 (이름 · 효과 · 해금 조건)
        C("brand.glass.name", "유리 대포", "Glass Cannon", "ガラスの大砲", "玻璃大炮");
        C("brand.glass.desc", "공격력 +40%\n최대 체력 -30%", "Attack +40%\nMax HP -30%", "攻撃力 +40%\n最大体力 -30%", "攻击力 +40%\n最大生命 -30%");
        C("brand.tide.name", "영혼의 밀물", "Soul Tide", "魂の満ち潮", "灵魂涨潮");
        C("brand.tide.desc", "경험치 · 코인 +25%\n적 등장 간격 -25%", "EXP · coins +25%\nEnemy spawn interval -25%", "経験値・コイン +25%\n敵の出現間隔 -25%", "经验 · 金币 +25%\n敌人生成间隔 -25%");
        C("brand.giant.name", "거인 사냥꾼", "Giant Hunter", "巨人狩り", "巨人猎手");
        C("brand.giant.desc", "필살기 충전 +50%\n보스 체력 +40%", "Ultimate charge +50%\nBoss HP +40%", "必殺技チャージ +50%\nボスの体力 +40%", "必杀技充能 +50%\n首领生命 +40%");
        C("brand.iron.name", "무쇠 맹세", "Iron Oath", "鉄の誓い", "钢铁誓约");
        C("brand.iron.desc", "받는 피해 -25%\n공격력 -15%", "Damage taken -25%\nAttack -15%", "受けるダメージ -25%\n攻撃力 -15%", "受到伤害 -25%\n攻击力 -15%");
        C("brand.greed.name", "탐욕", "Greed", "強欲", "贪婪");
        C("brand.greed.desc", "경험치 · 코인 +60%\n적 체력 +30%", "EXP · coins +60%\nEnemy HP +30%", "経験値・コイン +60%\n敵の体力 +30%", "经验 · 金币 +60%\n敌人生命 +30%");
        C("brand.greed.hint", "적을 누적 5,000마리 쓰러뜨리기", "Defeat 5,000 enemies in total", "敵を累計5,000体倒す", "累计击败5,000名敌人");
        C("brand.frenzy.name", "광란", "Frenzy", "狂乱", "狂乱");
        C("brand.frenzy.desc", "공격력 +25%\n적 이동 속도 +20% · 적 스킬 빈도 +30%", "Attack +25%\nEnemy speed +20% · enemy skill frequency +30%", "攻撃力 +25%\n敵の移動速度 +20%・敵のスキル頻度 +30%", "攻击力 +25%\n敌人移动速度 +20% · 敌人技能频率 +30%");
        C("brand.frenzy.hint", "「보통」 난이도 클리어하기", "Clear Normal difficulty", "「ノーマル」をクリアする", "通关「普通」难度");
        C("brand.blood.name", "피의 계약", "Blood Pact", "血の契約", "鲜血契约");
        C("brand.blood.desc", "경험치 · 코인 +40% · 필살기 충전 +40%\n최대 체력 -40%", "EXP · coins +40% · Ultimate charge +40%\nMax HP -40%", "経験値・コイン +40%・必殺技チャージ +40%\n最大体力 -40%", "经验 · 金币 +40% · 必杀技充能 +40%\n最大生命 -40%");
        C("brand.blood.hint", "서로 다른 스킬 진화 5종 만들기", "Create 5 different skill evolutions", "異なるスキル進化を5種作る", "完成5种不同的技能进化");
        C("brand.calm.name", "고요", "Stillness", "静寂", "寂静");
        C("brand.calm.desc", "적 이동 속도 -15%\n필살기 충전 -35%", "Enemy speed -15%\nUltimate charge -35%", "敵の移動速度 -15%\n必殺技チャージ -35%", "敌人移动速度 -15%\n必杀技充能 -35%");
        C("brand.calm.hint", "「쉬움」 난이도를 20분 안에 클리어하기", "Clear Easy within 20 minutes", "「イージー」を20分以内にクリアする", "在20分钟内通关「简单」难度");
        C("brand.tyrant.name", "폭군의 왕관", "Tyrant's Crown", "暴君の王冠", "暴君之冠");
        C("brand.tyrant.desc", "공격력 +20%\n보스 체력 +60% · 받는 피해 +20%", "Attack +20%\nBoss HP +60% · damage taken +20%", "攻撃力 +20%\nボスの体力 +60%・受けるダメージ +20%", "攻击力 +20%\n首领生命 +60% · 受到伤害 +20%");
        C("brand.tyrant.hint", "「어려움」 난이도 클리어하기", "Clear Hard difficulty", "「ハード」をクリアする", "通关「困难」难度");
        C("brand.surge.name", "마력 폭주", "Mana Surge", "魔力暴走", "魔力暴走");
        C("brand.surge.desc", "필살기 충전 +80%\n적 스킬 빈도 +40%", "Ultimate charge +80%\nEnemy skill frequency +40%", "必殺技チャージ +80%\n敵のスキル頻度 +40%", "必杀技充能 +80%\n敌人技能频率 +40%");
        C("brand.surge.hint", "서로 다른 스킬 진화 15종 만들기", "Create 15 different skill evolutions", "異なるスキル進化を15種作る", "完成15种不同的技能进化");
        C("brand.bulwark.name", "철벽", "Bulwark", "鉄壁", "铁壁");
        C("brand.bulwark.desc", "최대 체력 +40%\n공격력 -20%", "Max HP +40%\nAttack -20%", "最大体力 +40%\n攻撃力 -20%", "最大生命 +40%\n攻击力 -20%");
        C("brand.bulwark.hint", "검사로 한 판 클리어하기", "Clear a run as the Swordsman", "剣士で1回クリアする", "以剑士通关一次");
        C("brand.dance.name", "죽음의 춤", "Dance of Death", "死の舞踏", "死亡之舞");
        C("brand.dance.desc", "공격력 +30% · 경험치 · 코인 +30%\n받는 피해 +50%", "Attack +30% · EXP · coins +30%\nDamage taken +50%", "攻撃力 +30%・経験値・コイン +30%\n受けるダメージ +50%", "攻击力 +30% · 经验 · 金币 +30%\n受到伤害 +50%");
        C("brand.dance.hint", "무한 모드에서 10분 버티기", "Survive 10 minutes in Endless", "エンドレスで10分生き残る", "在无尽模式中坚持10分钟");
    }
}
