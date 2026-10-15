// 번역표 (1.1~): 판 후반 보상 — 3장 각성 · 초월 카드 (LevelShop.Transcend) · 도감 · 튜토리얼 문구
// LocTable.Skills 의 정적 생성자가 부름 (이미 있는 키는 건드리지 않음)
public static partial class LocTable
{
    static void AddTranscend()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }

        // 초월 카드
        A("초월 카드", "Transcendence Cards", "超越カード", "超越卡");
        A("초월 단계", "Transcendence", "超越段階", "超越阶段");
        A("최대 레벨 뒤 경험치 막대가 다시 가득 찼습니다 · 하나를 고르세요",
          "Your XP bar filled again past max level · Choose one",
          "最大レベルの後、経験値バーが再び満タンになりました・1つ選んでください",
          "达到等级上限后经验条再次填满 · 请选择一张");
        A("초월: 파괴", "Transcend: Ruin", "超越：破壊", "超越：毁灭");
        A("초월: 충전", "Transcend: Charge", "超越：充填", "超越：充能");
        A("초월: 급소", "Transcend: Vitals", "超越：急所", "超越：要害");
        A("초월: 생명", "Transcend: Life", "超越：生命", "超越：生命");
        A("초월: 질풍", "Transcend: Gale", "超越：疾風", "超越：疾风");
        A("모든 무기 피해 +10% (기본 공격력 기준)", "All weapon damage +10% (of base attack)", "すべての武器ダメージ +10%（基本攻撃力基準）", "所有武器伤害 +10%（以基础攻击力计）");
        A("필살기 게이지 차는 속도 +25%", "Ultimate gauge charge speed +25%", "必殺技ゲージの溜まる速さ +25%", "必杀技槽充能速度 +25%");
        A("치명타 확률 +5%, 치명타 피해 +20%", "Crit chance +5%, crit damage +20%", "会心率 +5%、会心ダメージ +20%", "暴击率 +5%，暴击伤害 +20%");
        A("최대 체력 +25, 체력 모두 회복", "Max health +25, fully heal", "最大体力 +25、体力全回復", "最大生命 +25，生命全部恢复");
        A("공격 속도 +6%", "Fire rate +6%", "攻撃速度 +6%", "攻击速度 +6%");
        A("최대 레벨에 닿은 뒤에는 경험치 막대가 가득 찰 때마다 초월 카드 세 장 중 하나를 고릅니다. (레벨은 오르지 않고, 카드마다 중첩 상한이 있습니다)",
          "After reaching max level, each time your XP bar fills you pick one of three Transcendence cards. (Your level doesn't rise, and each card has a stack limit)",
          "最大レベルに達した後は、経験値バーが満タンになるたびに超越カード3枚から1枚を選びます。（レベルは上がらず、カードごとに重ねがけの上限があります）",
          "达到等级上限后，每当经验条填满，就从三张超越卡中选择一张。（等级不会提升，每张卡都有叠加上限）");

        // 도감 · 튜토리얼
        A("레벨이 오를 때 세 장 중 하나를 고르거나 건너뜁니다 · 최대 레벨 50 · 무한 모드 80 (그 뒤로는 경험치 막대가 찰 때마다 초월 카드) · 재료를 모두 올리면 스킬 진화",
          "Pick one of three cards or skip on each level-up · Max level 50 · Endless 80 (after that, a Transcendence card each time the XP bar fills) · Max all ingredients to evolve a skill",
          "レベルアップごとに3枚から1枚を選ぶかスキップ・最大レベル50・エンドレス80（その後は経験値バーが満タンになるたびに超越カード）・素材をすべて上げるとスキル進化",
          "每次升级从三张中选一张或跳过 · 等级上限50 · 无尽80（之后每当经验条填满获得超越卡）· 素材全部升满即可技能进化");
        A("최대 레벨(50 · 무한 모드 80)에 닿은 뒤 경험치 막대가 가득 찰 때마다 (레벨은 오르지 않음) 금테 초월 카드 세 장 중 하나를 고릅니다. 파괴: 모든 무기 피해 +10% (최대 5번) · 충전: 필살기 게이지 차는 속도 +25% (최대 3번) · 급소: 치명타 확률 +5%, 치명타 피해 +20% (최대 4번) · 생명: 최대 체력 +25, 체력 모두 회복 (최대 4번) · 질풍: 공격 속도 +6% (최대 5번)",
          "After reaching max level (50 · Endless 80), each time your XP bar fills (your level doesn't rise) you pick one of three gold-framed Transcendence cards. Ruin: all weapon damage +10% (up to 5) · Charge: ultimate gauge charge speed +25% (up to 3) · Vitals: crit chance +5%, crit damage +20% (up to 4) · Life: max health +25, fully heal (up to 4) · Gale: fire rate +6% (up to 5)",
          "最大レベル（50・エンドレス80）に達した後、経験値バーが満タンになるたびに（レベルは上がりません）金枠の超越カード3枚から1枚を選びます。破壊：すべての武器ダメージ +10%（最大5回）・充填：必殺技ゲージの溜まる速さ +25%（最大3回）・急所：会心率 +5%、会心ダメージ +20%（最大4回）・生命：最大体力 +25、体力全回復（最大4回）・疾風：攻撃速度 +6%（最大5回）",
          "达到等级上限（50 · 无尽80）后，每当经验条填满（等级不会提升），就从三张金框超越卡中选择一张。毁灭：所有武器伤害 +10%（最多5次）· 充能：必杀技槽充能速度 +25%（最多3次）· 要害：暴击率 +5%，暴击伤害 +20%（最多4次）· 生命：最大生命 +25，生命全部恢复（最多4次）· 疾风：攻击速度 +6%（最多5次）");
        A("모든 캐릭터. 이야기 모드에서 3장 보스를, 무한 모드에서 세 번째 보스를 쓰러뜨리면 2차까지 진화한 무기가 각성합니다. 파괴(모든 공격 피해 +50%) · 질풍(공격 · 발사 간격 -30%) · 영혼(치명타 확률 +20%, 치명타 피해 +80%) 가운데 하나를 고릅니다.",
          "All characters. Defeat the Chapter 3 boss in Story mode, or the third boss in Endless mode, and your twice-evolved weapon awakens. Choose one: Ruin (all attack damage +50%) · Gale (attack / fire interval -30%) · Soul (crit chance +20%, crit damage +80%).",
          "全キャラクター。ストーリーモードで3章のボスを、エンドレスモードで3体目のボスを倒すと、2次まで進化した武器が覚醒します。破壊（すべての攻撃ダメージ +50%）・疾風（攻撃・発射間隔 -30%）・魂（会心率 +20%、会心ダメージ +80%）から1つを選びます。",
          "所有角色。在故事模式击败第3章首领，或在无尽模式击败第三个首领后，完成二次进化的武器将会觉醒。从毁灭（所有攻击伤害 +50%）· 疾风（攻击·射击间隔 -30%）· 灵魂（暴击率 +20%，暴击伤害 +80%）中选择一个。");
        A("보스를 쓰러뜨리면 무기가 진화합니다. 세 갈래 중 하나를 고르세요. (보통 판에서는 두 번 · 3장 보스는 각성, 되돌릴 수 없음)",
          "Defeating a boss evolves your weapon. Choose one of three paths. (Twice in a normal run · the Chapter 3 boss awakens it, cannot be undone)",
          "ボスを倒すと武器が進化します。3つの道から1つを選びましょう。(通常のプレイでは2回・3章のボスで覚醒、元に戻せません)",
          "击败首领后武器会进化。从三条路线中选择一条。(普通对局中两次 · 第3章首领后觉醒，无法撤销)");
    }
}
