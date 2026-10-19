using UnityEngine;

// 번역표 (1.2~): 발견형 도감 · 수집 보상 · NEW (CodexSeen · CodexUI)
// 열쇠는 codex. 접두어 (한국어 원문은 0번 칸) · 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddCodex()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 탭 · 안내
        C("각인", "각인", "Brands", "刻印", "烙印");
        C("비밀", "비밀", "Secrets", "秘密", "秘密");
        C("codex.tab.evo", "보스를 쓰러뜨려 고른 무기 진화 · 각성과 스킬 진화입니다. 한 번 얻은 것만 자세히 보입니다.",
          "Weapon evolutions and awakenings chosen after bosses, plus skill evolutions. Only what you've obtained is revealed.",
          "ボス撃破後に選んだ武器進化・覚醒とスキル進化です。一度手に入れたものだけ詳しく表示されます。",
          "击败首领后选择的武器进化 · 觉醒与技能进化。只有获得过的才会显示详情。");
        C("codex.tab.relic", "2장부터 나오는 봉인된 석관에서 고르는 잠든 스킬입니다. [{RELIC}]로 씁니다. 얻은 유물만 자세히 보입니다.",
          "Dormant skills chosen from Sealed Sarcophagi (Chapter 2+). Use them with [{RELIC}]. Only relics you've obtained are revealed.",
          "2章から現れる封印された石棺で選ぶ眠ったスキルです。[{RELIC}] で使います。手に入れた遺物だけ詳しく表示されます。",
          "从第2章起出现的封印石棺中选择的沉睡技能。按 [{RELIC}] 使用。只有获得过的遗物才会显示详情。");
        C("codex.tab.brand", "이야기 모드 판의 규칙 카드입니다. 판을 시작할 때와 2 · 3장 보스 뒤에 고릅니다. 골라 본 각인만 자세히 보입니다.",
          "Rule cards for Story runs, chosen at the start and after the Chapter 2 and 3 bosses. Only brands you've picked are revealed.",
          "ストーリーモードのプレイのルールカードです。開始時と2・3章のボスの後に選びます。選んだ刻印だけ詳しく表示されます。",
          "故事模式对局的规则卡。在开局时与第2 · 3章首领后选择。只有选过的烙印才会显示详情。");
        C("codex.tab.secret", "숨겨진 영웅 · 보스 · 행운입니다. 조건을 이루거나 만나면 열립니다.",
          "Hidden heroes, bosses and lucky surprises, revealed when you meet them or fulfill their conditions.",
          "隠された英雄・ボス・幸運です。条件を満たすか出会うと開きます。",
          "隐藏的英雄 · 首领 · 幸运。达成条件或遇见后解锁。");
        C("codex.showall.on", "모두 보기 켜짐", "Show All is on", "すべて表示 オン", "全部显示 已开启");

        // ================================================================= 수집률 · 보상
        C("codex.rate", "수집률 {0}%  ·  {1} / {2}", "Collected {0}%  ·  {1} / {2}", "収集率 {0}%  ・  {1} / {2}", "收集率 {0}%  ·  {1} / {2}");
        C("codex.rewards", "수집 보상", "Rewards", "収集報酬", "收集奖励");
        C("codex.rewards.title", "도감 수집 보상", "Codex Rewards", "図鑑の収集報酬", "图鉴收集奖励");
        C("codex.rewards.how", "진화 · 유물 · 각인 · 비밀 탭에서 실제로 얻은 것만 셉니다. 「도감 모두 보기」로 본 것은 들어가지 않습니다.",
          "Only what you've actually obtained in the Evolutions, Relics, Brands and Secrets tabs counts. Entries revealed with \"Show Entire Codex\" don't.",
          "進化・遺物・刻印・秘密タブで実際に手に入れたものだけを数えます。「図鑑をすべて表示」で見たものは含まれません。",
          "只计算在进化 · 遗物 · 烙印 · 秘密页中实际获得的内容。通过「显示全部图鉴」看到的不计入。");
        C("codex.reward.skin", "전설 이펙트 스킨 「{0}」", "Legendary effect skin \"{0}\"", "伝説エフェクトスキン「{0}」", "传说特效皮肤「{0}」");
        C("codex.reward.owned", "이미 가지고 있으면 대신 {0} P를 받습니다.", "If you already own it, you get {0} P instead.", "すでに持っている場合は、代わりに {0} P を受け取ります。", "若已拥有，则改为获得 {0} P。");
        C("codex.claim", "받기", "Claim", "受け取る", "领取");
        C("codex.claimed", "받음", "Claimed", "受け取り済み", "已领取");
        C("codex.got", "보상을 받았습니다! {0}", "Reward claimed! {0}", "報酬を受け取りました！{0}", "已领取奖励！{0}");

        // ================================================================= 판 안 알림
        C("codex.toast", "도감 새 기록 · {0}", "New Codex entry · {0}", "図鑑に新しく記録・{0}", "图鉴新记录 · {0}");
        C("codex.toast.rate", "수집률 {0}%", "Collected {0}%", "収集率 {0}%", "收集率 {0}%");
        C("codex.milestone", "도감 수집률 {0}% 달성!", "Codex {0}% complete!", "図鑑収集率 {0}% 達成！", "图鉴收集率达到 {0}%！");
        C("codex.milestone.sub", "메인 메뉴 도감에서 보상을 받으세요.", "Claim your reward in the main menu Codex.", "メインメニューの図鑑で報酬を受け取りましょう。", "请在主菜单的图鉴中领取奖励。");

        // ================================================================= 진화 탭
        C("codex.skillevo.body", "레벨업 카드를 모두 최대로 올려 만든 스킬 진화는 메인 메뉴 「스킬 진화」 도감에 하나씩 기록되고, 이곳 수집률에도 들어갑니다.",
          "Skill evolutions you create by maxing their level-up cards are recorded one by one in the main menu \"Skill Evolutions\" codex, and count toward the collection here too.",
          "レベルアップカードをすべて最大にして作ったスキル進化は、メインメニューの「スキル進化」図鑑に1つずつ記録され、ここの収集率にも含まれます。",
          "将升级卡全部升到最高而形成的技能进化，会逐一记录在主菜单的「技能进化」图鉴中，并计入此处的收集率。");
        C("codex.hint.evo1", "보스를 쓰러뜨린 뒤 무기 진화에서 이 갈래를 고르면 기록됩니다.",
          "Recorded when you choose this path in a weapon evolution after defeating a boss.",
          "ボスを倒した後の武器進化でこの系統を選ぶと記録されます。",
          "击败首领后在武器进化中选择此分支即可记录。");
        C("codex.hint.evo2", "「{0}」 다음 2차 진화에서 고를 수 있습니다.", "Can be chosen as the second evolution after \"{0}\".", "「{0}」の次の第二進化で選べます。", "可在「{0}」之后的第二次进化中选择。");
        C("codex.hint.evo2k", "1차 진화 뒤 다음 보스를 쓰러뜨리면 고를 수 있는 2차 진화입니다.",
          "A second evolution you can choose after your first one, upon defeating the next boss.",
          "第一進化の後、次のボスを倒すと選べる第二進化です。",
          "完成第一次进化后，击败下一个首领即可选择的第二次进化。");
        C("codex.hint.lockedhero", "잠긴 영웅의 진화입니다. 그 영웅을 해금해 진화하면 기록됩니다.",
          "An evolution of a locked hero. Unlock that hero and obtain this evolution to record it.",
          "ロック中の英雄の進化です。その英雄を解放して進化すると記録されます。",
          "未解锁英雄的进化。解锁该英雄并完成进化后即可记录。");
        C("codex.hint.awaken", "2차까지 진화한 뒤 이야기 모드 3장 보스 · 무한 모드 세 번째 보스를 쓰러뜨리면 고르는 각성입니다.",
          "An awakening chosen after your second evolution, upon defeating the Chapter 3 boss in Story or the third boss in Endless.",
          "第二進化の後、ストーリーの3章ボス・エンドレスの3体目のボスを倒すと選べる覚醒です。",
          "完成第二次进化后，击败故事模式第3章首领或无尽模式第三个首领时选择的觉醒。");

        // ================================================================= 유물 · 각인 탭
        C("codex.hint.relic", "2장부터 나타나는 봉인된 석관을 부수고 이 유물을 고르면 기록됩니다.",
          "Recorded when you break a Sealed Sarcophagus (Chapter 2+) and choose this relic.",
          "2章から現れる封印された石棺を壊してこの遺物を選ぶと記録されます。",
          "打破第2章起出现的封印石棺并选择此遗物即可记录。");
        C("codex.hint.brand", "이야기 모드 판에서 이 각인을 고르면 기록됩니다.", "Recorded when you pick this brand in a Story run.", "ストーリーモードのプレイでこの刻印を選ぶと記録されます。", "在故事模式中选择此烙印即可记录。");

        // ================================================================= 비밀 탭
        C("codex.sec.hero", "비밀 영웅", "Secret Hero", "秘密の英雄", "秘密英雄");
        C("codex.sec.boss", "비밀 보스", "Secret Boss", "秘密のボス", "秘密首领");
        C("codex.sec.luck", "숨은 행운", "Hidden Luck", "隠れた幸運", "隐藏的幸运");
        C("codex.sec.hero.buy", "조건을 이루었습니다! 캐릭터 창에서 {0} P로 해금할 수 있습니다.",
          "Condition met! Unlock them for {0} P in the Characters window.",
          "条件を達成しました！キャラクター画面で {0} P で解放できます。",
          "已达成条件！可在角色界面花费 {0} P 解锁。");
        C("codex.hint.dev", "무한 모드에서 보스를 하나라도 쓰러뜨린 채 15분을 버티면 모든 보스가 깨어납니다. 모두 쓰러뜨리면 누군가 내려옵니다.",
          "Survive 15 minutes in Endless with at least one boss defeated, and every boss awakens. Defeat them all, and someone descends.",
          "エンドレスでボスを1体でも倒したまま15分耐えると、すべてのボスが目覚めます。すべて倒すと、誰かが降りてきます。",
          "在无尽模式中至少击败一名首领并坚持到15分钟，所有首领都会苏醒。将其全部击败，就会有人降临。");
        C("codex.hint.mom", "개발자를 쓰러뜨린 뒤 영혼 500개를 거두면, 개발자보다 더 무서운 누군가가 강림합니다.",
          "Reap 500 souls after defeating the Developer, and someone scarier descends.",
          "開発者を倒した後に魂を500個集めると、開発者よりも恐ろしい誰かが降臨します。",
          "击败开发者后收割500个灵魂，比开发者更可怕的人便会降临。");
        C("codex.hint.jackpot", "보물 상자를 열다 보면 아주 드물게 특별한 일이 일어납니다.",
          "Open enough treasure chests and, very rarely, something special happens.",
          "宝箱を開けていると、ごくまれに特別なことが起こります。",
          "开启宝箱时，极少数情况下会发生特别的事。");
        C("codex.sec.jackpot.desc", "보물 상자를 열 때 아주 드물게 행운의 룰렛이 돌아 보상 3~5개가 한꺼번에 쏟아집니다. 재료를 모두 모은 스킬 진화가 있으면 진화 촉매도 함께 나옵니다.",
          "Very rarely, opening a chest spins the wheel of fortune and 3–5 rewards pour out at once. If you have every ingredient for a skill evolution, an Evolution Catalyst drops as well.",
          "宝箱を開けるとごくまれに幸運のルーレットが回り、報酬3〜5個が一度にあふれ出ます。素材がすべてそろったスキル進化があれば、進化の触媒も一緒に出てきます。",
          "开启宝箱时极少数情况下幸运轮盘会转动，一次涌出3~5份奖励。若有材料齐全的技能进化，还会附带进化催化剂。");

        // ================================================================= 설정
        C("도감 모두 보기", "도감 모두 보기", "Show Entire Codex", "図鑑をすべて表示", "显示全部图鉴");
        C("켜면 도감의 진화 · 유물 · 각인 · 비밀 탭에서 아직 못 얻은 것도 모두 보입니다 (스포일러). 수집률과 보상에는 들어가지 않습니다.",
          "켜면 도감의 진화 · 유물 · 각인 · 비밀 탭에서 아직 못 얻은 것(스포일러)까지 모두 보입니다. 수집률과 보상에는 들어가지 않습니다.",
          "When on, the Codex's Evolutions, Relics, Brands and Secrets tabs reveal everything, even what you haven't obtained (spoilers). Revealed items don't count toward collection or rewards.",
          "オンにすると、図鑑の進化・遺物・刻印・秘密タブでまだ手に入れていないものもすべて表示されます（ネタバレ）。収集率と報酬には含まれません。",
          "开启后，图鉴的进化 · 遗物 · 烙印 · 秘密页中尚未获得的内容也会全部显示（剧透）。不计入收集率与奖励。");
    }
}
