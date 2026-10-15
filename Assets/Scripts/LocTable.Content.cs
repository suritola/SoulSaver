using UnityEngine;

// 번역표 (1.1~): 장 중반 이벤트 풀 · 보물 상자 보상 · 저주 (ContentRegistry · ChapterEvents · ChestRewards)
// 열쇠는 event. · chest. · curse. 접두어 (한국어 원문은 0번 칸) · 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
// 다른 번역표 파일을 고치지 않게 게임이 켜질 때 스스로 더함
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddContent()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 장 중반 이벤트
        C("event.raid.name", "습격", "Ambush", "襲撃", "伏击");
        C("event.raid.desc", "둘레에 적 무리가 한꺼번에 나타납니다. 모두 쓰러뜨리면 보물 상자",
          "A horde appears all around you. Defeat them all for a treasure chest",
          "周囲に敵の群れが一斉に現れます。全滅させると宝箱",
          "周围同时出现大批敌人。全部击败可获得宝箱");
        C("event.elite.name", "엘리트", "Elite", "エリート", "精英");
        C("event.elite.desc", "크고 단단한 엘리트가 나타납니다. 쓰러뜨리면 정예 보물 상자",
          "A big, tough elite appears. Defeat it for an elite treasure chest",
          "大きくて硬いエリートが現れます。倒すと精鋭の宝箱",
          "出现一名高大坚韧的精英。击败可获得精英宝箱");
        C("event.goblin.name", "황금 고블린", "Golden Goblin", "黄金ゴブリン", "黄金哥布林");
        C("event.goblin.desc", "코인을 잔뜩 든 고블린이 달아납니다. 20초 안에 잡으면 코인이 쏟아집니다",
          "A goblin loaded with coins runs away. Catch it within 20s for a shower of coins",
          "コインを抱えたゴブリンが逃げ回ります。20秒以内に倒すとコインがあふれ出ます",
          "满载金币的哥布林四处逃窜。20秒内抓住它，金币倾泻而出");
        C("event.cursed.name", "저주받은 상자", "Cursed Chest", "呪われた箱", "被诅咒的宝箱");
        C("event.cursed.desc", "열면 저주받은 적들이 깨어납니다. 모두 쓰러뜨리면 정예 보물 상자",
          "Opening it awakens cursed foes. Defeat them all for an elite treasure chest",
          "開けると呪われた敵が目覚めます。全滅させると精鋭の宝箱",
          "打开后被诅咒的敌人将苏醒。全部击败可获得精英宝箱");
        C("event.altar.name", "영혼 제단 지키기", "Guard the Soul Altar", "魂の祭壇を守れ", "守护灵魂祭坛");
        C("event.altar.desc", "제단 둘레에 머물러 영혼을 모으세요. 다 모으면 보물 상자와 영혼 조각",
          "Stay within the altar's circle to gather souls. Fill it for a treasure chest and soul shards",
          "祭壇の輪の中にとどまって魂を集めましょう。集めきると宝箱と魂のかけら",
          "停留在祭坛圈内收集灵魂。收集完成可获得宝箱与灵魂碎片");
        C("event.pack.name", "정예 무리", "Elite Pack", "精鋭の群れ", "精英群");
        C("event.pack.desc", "강한 정예 몇 마리가 함께 몰려옵니다. 모두 쓰러뜨리면 정예 보물 상자",
          "A few powerful elites charge in together. Defeat them all for an elite treasure chest",
          "強力な精鋭が数体まとめて押し寄せます。全滅させると精鋭の宝箱",
          "几名强大的精英一同袭来。全部击败可获得精英宝箱");
        C("event.pack.tag", "정예", "Elite", "精鋭", "精英");

        // 예고 (3초 전) · 진행 표시 · 결과
        C("event.soon", "3초 뒤: {0}!\n{1}", "In 3s: {0}!\n{1}", "3秒後：{0}！\n{1}", "3秒后：{0}！\n{1}");
        C("event.soon.label", "{0} · {1}초 뒤 시작", "{0} · starts in {1}s", "{0}・{1}秒後に開始", "{0} · {1}秒后开始");
        C("event.goblin.start", "황금 고블린이 달아난다!\n20초 안에 잡으세요", "The Golden Goblin is running!\nCatch it within 20s", "黄金ゴブリンが逃げ出した！\n20秒以内に捕まえよう", "黄金哥布林逃跑了！\n在20秒内抓住它");
        C("event.goblin.label", "황금 고블린 · {0}초", "Golden Goblin · {0}s", "黄金ゴブリン・{0}秒", "黄金哥布林 · {0}秒");
        C("event.goblin.caught", "황금 고블린을 잡았다!\n코인 +{0}", "Golden Goblin caught!\nCoins +{0}", "黄金ゴブリンを捕まえた！\nコイン +{0}", "抓住了黄金哥布林！\n金币 +{0}");
        C("event.goblin.escaped", "황금 고블린이 달아났다...", "The Golden Goblin got away...", "黄金ゴブリンに逃げられた……", "黄金哥布林逃走了……");
        C("event.cursed.appear", "저주받은 상자가 나타났다!\n열면 저주받은 적이 깨어납니다", "A Cursed Chest appeared!\nOpening it awakens cursed foes", "呪われた箱が現れた！\n開けると呪われた敵が目覚めます", "被诅咒的宝箱出现了！\n打开后被诅咒的敌人将苏醒");
        C("event.cursed.wait", "저주받은 상자 · {0}초 뒤 사라짐", "Cursed Chest · vanishes in {0}s", "呪われた箱・{0}秒後に消える", "被诅咒的宝箱 · {0}秒后消失");
        C("event.cursed.gone", "저주받은 상자가 사라졌다", "The Cursed Chest vanished", "呪われた箱が消えた", "被诅咒的宝箱消失了");
        C("event.cursed.open", "저주가 깨어났다!\n모두 쓰러뜨리면 정예 보물 상자", "The curse awakens!\nDefeat them all for an elite treasure chest", "呪いが目覚めた！\n全滅させると精鋭の宝箱", "诅咒苏醒了！\n全部击败可获得精英宝箱");
        C("event.cursed.label", "저주받은 적 · 남은 적 {0} · {1}초", "Cursed foes · {0} left · {1}s", "呪われた敵・残り {0}・{1}秒", "被诅咒的敌人 · 剩余 {0} · {1}秒");
        C("event.cursed.fail", "저주를 이겨 내지 못했다\n시간 안에 모두 쓰러뜨리지 못했습니다", "The curse prevails\nYou didn't defeat them all in time", "呪いに打ち勝てなかった\n時間内に全滅できませんでした", "未能战胜诅咒\n未能在时限内全部击败");
        C("event.altar.start", "영혼 제단!\n둘레에 머물러 영혼을 모으세요", "Soul Altar!\nStay in the circle to gather souls", "魂の祭壇！\n輪の中にとどまって魂を集めよう", "灵魂祭坛！\n停留在圈内收集灵魂");
        C("event.altar.label", "영혼 제단 · {0}% · {1}초", "Soul Altar · {0}% · {1}s", "魂の祭壇・{0}%・{1}秒", "灵魂祭坛 · {0}% · {1}秒");
        C("event.altar.done", "영혼 제단을 지켰다!\n영혼 조각 +{0}", "Soul Altar guarded!\nSoul shards +{0}", "魂の祭壇を守った！\n魂のかけら +{0}", "守住了灵魂祭坛！\n灵魂碎片 +{0}");
        C("event.altar.fail", "영혼 제단이 꺼졌다\n시간 안에 영혼을 다 모으지 못했습니다", "The Soul Altar went dark\nYou didn't gather enough souls in time", "魂の祭壇が消えた\n時間内に魂を集めきれませんでした", "灵魂祭坛熄灭了\n未能在时限内收集足够的灵魂");
        C("event.pack.start", "정예 무리!\n모두 쓰러뜨리면 정예 보물 상자", "Elite Pack!\nDefeat them all for an elite treasure chest", "精鋭の群れ！\n全滅させると精鋭の宝箱", "精英群！\n全部击败可获得精英宝箱");
        C("event.pack.label", "정예 무리 · 남은 적 {0} · {1}초", "Elite Pack · {0} left · {1}s", "精鋭の群れ・残り {0}・{1}秒", "精英群 · 剩余 {0} · {1}秒");
        C("event.pack.fail", "정예 무리를 놓쳤다\n시간 안에 모두 쓰러뜨리지 못했습니다", "The Elite Pack slipped away\nYou didn't defeat them all in time", "精鋭の群れを取り逃がした\n時間内に全滅できませんでした", "精英群逃走了\n未能在时限内全部击败");

        // ================================================================= 보물 상자
        C("chest.title", "보물 상자", "Treasure Chest", "宝箱", "宝箱");
        C("chest.pick", "하나를 고르세요", "Choose one", "1つ選んでください", "请选择一项");
        C("chest.elite.title", "정예 보물 상자", "Elite Treasure Chest", "精鋭の宝箱", "精英宝箱");
        C("chest.elite.appear", "정예 보물 상자가 나타났다!", "An elite treasure chest appeared!", "精鋭の宝箱が現れた！", "精英宝箱出现了！");
        C("chest.rare", "희귀", "Rare", "レア", "稀有");
        // 일반
        C("chest.coins.name", "황금", "Gold", "黄金", "黄金");
        C("chest.coins.desc", "코인 +{0}", "Coins +{0}", "コイン +{0}", "金币 +{0}");
        C("chest.shards.name", "영혼", "Soul", "魂", "灵魂");
        C("chest.shards.desc", "영혼 조각 +{0}", "Soul shards +{0}", "魂のかけら +{0}", "灵魂碎片 +{0}");
        C("chest.life.name", "생명", "Life", "生命", "生命");
        C("chest.life.desc", "체력을 모두 회복하고 최대 체력 +10", "Fully restore HP and +10 max HP", "体力を全回復し、最大体力+10", "完全恢复生命并提升最大生命+10");
        C("chest.heal.name", "치유의 샘", "Healing Spring", "癒しの泉", "治愈之泉");
        C("chest.heal.desc", "최대 체력의 50%만큼 회복합니다", "Restore 50% of max HP", "最大体力の50%を回復します", "恢复最大生命的50%");
        C("chest.level.name", "깨달음", "Insight", "悟り", "顿悟");
        C("chest.level.desc", "레벨업 카드를 한 번 더 고릅니다", "Pick one more level-up card", "レベルアップカードをもう1回選べます", "多选择一次升级卡牌");
        C("chest.might.name", "날 선 힘", "Honed Might", "研ぎ澄まされた力", "锋锐之力");
        C("chest.might.desc", "공격력 +5% (이번 판 끝까지)", "Attack +5% (for the rest of the run)", "攻撃力 +5%（このランの間）", "攻击力 +5%（本局内持续）");
        // 희귀
        C("chest.card.name", "숙련", "Mastery", "熟練", "熟练");
        C("chest.card.desc", "{0} +1단계", "{0} +1 level", "{0} +1段階", "{0} +1级");
        C("chest.node.name", "영혼 트리의 축복", "Soul Tree's Blessing", "魂のツリーの祝福", "灵魂树的祝福");
        C("chest.node.desc", "영혼 트리 칸 하나를 조각 없이 배웁니다: {0}", "Learn one Soul Tree node for free: {0}", "魂のツリーのマスを1つ無料で習得：{0}", "免费学习一个灵魂树节点：{0}");
        C("chest.reroll.name", "운명의 주사위", "Dice of Fate", "運命のサイコロ", "命运骰子");
        C("chest.reroll.desc", "레벨업 카드 다시 뽑기 +1회", "Level-up card reroll +1", "レベルアップカードの引き直し +1回", "升级卡牌重抽 +1次");
        C("chest.dormant.name", "잠든 힘", "Dormant Power", "眠れる力", "沉睡之力");
        C("chest.dormant.desc", "잠든 특수 능력을 깨웁니다: {0}", "Awaken a dormant special ability: {0}", "眠っていた特殊能力を目覚めさせます：{0}", "唤醒沉睡的特殊能力：{0}");
        C("chest.essence.name", "생명의 정수", "Essence of Life", "生命の精髄", "生命精华");
        C("chest.essence.desc", "최대 체력 +20, 체력 모두 회복", "+20 max HP, fully restore HP", "最大体力+20、体力を全回復", "最大生命+20，完全恢复生命");
        // 전설 (룰렛)
        C("chest.legend.title", "전설 상자!", "Legendary Chest!", "伝説の宝箱！", "传说宝箱！");
        C("chest.legend.sub", "행운의 룰렛이 돌아갑니다...", "The wheel of fortune spins...", "幸運のルーレットが回る……", "幸运轮盘转动中……");
        C("chest.jackpot.name", "대박!", "Jackpot!", "大当たり！", "大奖！");
        C("chest.jackpot.desc", "보상 {0}개가 한꺼번에 쏟아집니다", "{0} rewards pour out at once", "報酬{0}個が一度にあふれ出ます", "{0}份奖励一齐涌出");
        C("chest.jackpot.take", "모두 받기", "Take Them All", "すべて受け取る", "全部收下");
        C("chest.catalyst.line", "진화 촉매: {0} 진화!", "Evolution Catalyst: {0} evolves!", "進化の触媒：{0} 進化！", "进化催化剂：{0} 进化！");

        // ================================================================= 저주
        C("curse.altar.name", "저주 제단", "Cursed Altar", "呪いの祭壇", "诅咒祭坛");
        C("curse.altar.desc", "이번 장 적 · 보스 체력 +30%\n경험치 · 코인 보상 +50%", "This chapter: enemy · boss HP +30%\nEXP · coin rewards +50%", "この章の敵・ボスの体力 +30%\n経験値・コイン報酬 +50%", "本章敌人 · 首领生命 +30%\n经验 · 金币奖励 +50%");
    }
}
