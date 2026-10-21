using UnityEngine;

// 1.2~ 난이도 사다리 · 무한 이정표 · 영웅 해금 대체 루트 · 받기 창 · 밸런스 결정 묶음(보스 보상 가운데 칸 · 초월 간격 · 무게 배율) 번역
// 열쇠는 「ladder.」 「milestone.」 「claim.」 「route.」 접두어 · 설명은 완결 문장 (문구 다듬기 지침 2026-10-16) · 수치는 코드 상수와 같게 (바꾸면 같이)
//   Ladder.cs (EndlessMilestones · UnlockRoutes · ClaimRewards) · MenuExtras 난이도 툴팁 · DailyChallenge · CharacterData · ResultScreen · ChestRewards.BossSlot · LevelShop.Transcend · CodexUI · SoulWeight
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddLadder()
    {
        void A(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ---------------- 잠금 안내 (예전 「어려움을 클리어하면 …」)
        A("ladder.lock.daily", "쉬움을 클리어하면 일일 도전이 열립니다.",
          "Clear Easy to unlock the Daily Challenge.",
          "イージーをクリアするとデイリーチャレンジが解放されます。",
          "通关简单难度后解锁每日挑战。");
        // 1.3.3~ 쉬움 클리어로 보통 · 어려움 · 무한이 한꺼번에 열림 (GameMode.OnCleared)
        A("ladder.lock.endless", "쉬움을 클리어하면 무한 모드가 열립니다.",
          "Clear Easy to unlock Endless Mode.",
          "イージーをクリアするとエンドレスモードが解放されます。",
          "通关简单难度后解锁无尽模式。");
        A("ladder.lock.endless.hard", "쉬움을 클리어하면 무한 모드가 열립니다. 어려움을 클리어하면 영혼의 무게도 열립니다.",
          "Clear Easy to unlock Endless Mode. Clearing Hard also unlocks Soul Weight.",
          "イージーをクリアするとエンドレスモードが解放されます。ハードをクリアすると魂の重みも解放されます。",
          "通关简单难度后解锁无尽模式。通关困难难度还会解锁灵魂之重。");

        // ---------------- 난이도 버튼 툴팁 (클리어하면 열리는 것)
        A("ladder.tip.easy", "클리어하면 보통 · 어려움 난이도와 무한 모드, 영혼 각인, 일일 도전이 열립니다.",
          "Clearing it unlocks Normal and Hard, Endless Mode, Soul Brands and the Daily Challenge.",
          "クリアするとノーマル・ハード、エンドレスモード、魂の刻印、デイリーチャレンジが解放されます。",
          "通关后解锁普通 · 困难难度、无尽模式、灵魂烙印与每日挑战。");
        A("ladder.tip.normal", "클리어하면 영혼 각인 「광란」이 열립니다.",
          "Clearing it unlocks the Soul Brand \"Frenzy\".",
          "クリアすると魂の刻印「狂乱」が解放されます。",
          "通关后解锁灵魂烙印「狂乱」。");
        A("ladder.tip.hard", "클리어하면 영혼의 무게가 열립니다.",
          "Clearing it unlocks Soul Weight.",
          "クリアすると魂の重みが解放されます。",
          "通关后解锁灵魂之重。");
        A("ladder.tip.endless", "끝없이 버티는 모드입니다. 5 · 10 · 15 · 20분을 버티거나, 한 판에서 보스 5마리를 쓰러뜨리거나, 개발자를 쓰러뜨리면 처음 한 번 이정표 포인트를 얻습니다.",
          "Survive for as long as you can. Survive 5, 10, 15 or 20 minutes, defeat 5 bosses in one run, or defeat the Developer to earn milestone points the first time.",
          "どこまでも生き延びるモードです。5・10・15・20分生き延びる、1回のプレイでボスを5体倒す、開発者を倒すと、初回だけマイルストーンのポイントを獲得します。",
          "坚持到底的模式。坚持5 · 10 · 15 · 20分钟、在一局中击败5个首领或击败开发者时，首次可获得里程碑点数。");
        A("ladder.tip.endless.points", "처치 포인트는 이번 판 처치 2,000 뒤 절반, 8,000 뒤 5분의 1로 줄어듭니다. 보스 · 이정표 · 클리어 보너스는 그대로입니다.",
          "Kill points drop to half after 2,000 kills in a run and to one fifth after 8,000. Boss, milestone and clear bonuses are unaffected.",
          "撃破ポイントは1回のプレイで撃破2,000体以降は半分、8,000体以降は5分の1になります。ボス・マイルストーン・クリアボーナスはそのままです。",
          "击杀点数在本局击杀2,000后减半，8,000后降为五分之一。首领、里程碑与通关奖励不受影响。");
        A("ladder.tip.cleared", "이미 클리어했습니다.", "Already cleared.", "クリア済みです。", "已通关。");

        // ---------------- 소급 해금 알림 (받기 창 맨 위, 한 번)
        A("ladder.retro.endless", "무한 모드가 열렸습니다. 이제 쉬움을 클리어하면 무한 모드까지 열리므로, 이미 클리어한 기록으로 열었습니다.",
          "Endless Mode is now unlocked. Clearing Easy now unlocks Endless Mode, so your earlier clear counts.",
          "エンドレスモードが解放されました。イージーをクリアするとエンドレスまで解放されるようになったため、これまでのクリア記録で解放しました。",
          "无尽模式已解锁。现在通关简单难度即可解锁无尽模式，因此已根据你之前的通关记录解锁。");

        // ---------------- 무한 이정표
        A("milestone.min", "무한 모드 {0}분 버티기", "Survive {0} minutes in Endless Mode", "エンドレスモードで{0}分生き延びる", "在无尽模式中坚持{0}分钟");
        A("milestone.boss5", "무한 모드 한 판에서 보스 5마리 쓰러뜨리기", "Defeat 5 bosses in a single Endless run", "エンドレスモードの1回のプレイでボスを5体倒す", "在一局无尽模式中击败5个首领");
        A("milestone.dev", "무한 모드에서 최종 보스 「개발자」 쓰러뜨리기", "Defeat the final boss, the Developer, in Endless Mode", "エンドレスモードで最終ボス「開発者」を倒す", "在无尽模式中击败最终首领「开发者」");
        A("milestone.reached", "무한 모드 이정표 달성!", "Endless milestone reached!", "エンドレスのマイルストーン達成！", "达成无尽模式里程碑！");
        A("milestone.claim", "무한 모드 이정표", "Endless Milestone", "エンドレスのマイルストーン", "无尽模式里程碑");
        A("result.goal.milestone", "다음 이정표 · 무한 모드 {0}분", "Next Milestone · Endless {0} min", "次のマイルストーン・エンドレス{0}分", "下一个里程碑 · 无尽{0}分钟");
        A("result.goal.milestone.left", "다음 이정표까지 {0}", "{0} to go", "あと{0}", "还差{0}");

        // ---------------- 받기 창
        A("claim.title", "받을 보상", "Rewards to Claim", "受け取れる報酬", "可领取的奖励");
        A("claim.title.notice", "새로 열린 콘텐츠", "Newly Unlocked", "新たに解放された要素", "新解锁内容");
        A("claim.sub", "이미 이룬 기록으로 열린 보상입니다. 눌러서 받으세요.",
          "These rewards were unlocked by what you have already achieved. Click to claim them.",
          "すでに達成した記録で解放された報酬です。押して受け取ってください。",
          "这些是根据你已达成的记录解锁的奖励。点击领取。");
        A("claim.take", "받기", "Claim", "受け取る", "领取");
        A("claim.taken", "받음", "Claimed", "受け取り済み", "已领取");
        A("claim.all", "모두 받기", "Claim All", "すべて受け取る", "全部领取");
        A("claim.more", "나머지 {0}개는 「모두 받기」로 함께 받습니다.", "The other {0} are included in Claim All.", "残りの{0}件は「すべて受け取る」で一緒に受け取れます。", "其余{0}项可通过「全部领取」一并领取。");
        A("claim.got", "포인트 {0}P를 받았습니다.", "You received {0} P.", "{0}Pを受け取りました。", "已领取{0}P。");

        // ---------------- 영웅 해금 대체 루트
        A("route.or", "또는", "or", "または", "或");
        A("route.prereq", "먼저 「쉬움」을 클리어해 무한 모드 열기", "First clear Easy to unlock Endless", "まず「イージー」をクリアしてエンドレスを解放", "先通关「简单」以解锁无尽模式");
        A("route.chests", "이야기 모드에서 보물 상자 15개 열기", "Open 15 treasure chests in Story Mode", "ストーリーモードで宝箱を15個開ける", "在故事模式中打开15个宝箱");
        A("route.quick", "이야기 모드에서 장 보스 4종을 각각 결계가 펼쳐지기 전에 쓰러뜨리기", "Defeat each of the 4 chapter bosses in Story Mode before it raises its Barrier",
          "ストーリーモードで4種の章ボスをそれぞれ結界を張られる前に倒す", "在故事模式中分别赶在4种章节首领展开结界前将其击败");
        A("route.weight", "어려움에서 영혼의 무게 2 클리어", "Clear Soul Weight 2 on Hard", "ハードで魂の重み2をクリア", "在困难难度通关灵魂之重2");
        A("route.chests.short", "보물 상자 {0}/{1}", "Chests {0}/{1}", "宝箱 {0}/{1}", "宝箱 {0}/{1}");
        A("route.quick.short", "결계 전 보스 {0}/{1}", "Pre-Barrier bosses {0}/{1}", "結界前ボス {0}/{1}", "结界前首领 {0}/{1}");
        A("route.weight.short", "영혼의 무게 {0}/{1}", "Soul Weight {0}/{1}", "魂の重み {0}/{1}", "灵魂之重 {0}/{1}");
        A("route.quick.toast", "결계 전에 쓰러뜨렸다!  {1} 해금 길 {0}", "Defeated before its Barrier!  {1} route {0}", "結界の前に倒した！  {1}の解放ルート {0}", "在结界前击败！  {1}解锁路线 {0}");
        A("route.retro", "{0}의 해금 조건을 이미 이루었습니다. 새로 더해진 길 「{1}」의 기록이 인정되어 이제 {2}P로 살 수 있습니다.",
          "You have already met {0}'s unlock condition. Your record on the new route \"{1}\" counts, so {0} can now be bought for {2} P.",
          "{0}の解放条件をすでに達成しています。新しく加わったルート「{1}」の記録が認められ、{2}Pで購入できるようになりました。",
          "你已达成{0}的解锁条件。新增路线「{1}」的记录已被认可，现在可以用{2}P购买。");

        // ---------------- 밸런스 결정 묶음
        A("chest.boss.legend", "고르면 룰렛이 돌아 보상 3~5개를 한꺼번에 얻습니다.", "Pick this to spin the roulette and get 3–5 rewards at once.",
          "選ぶとルーレットが回り、報酬を3〜5個まとめて獲得します。", "选择后转动轮盘，一次获得3~5个奖励。");
        A("weight.subtitle2", "고른 무게까지의 항목이 모두 쌓입니다. 무게 1마다 포인트가 {0}% 늘어납니다.",
          "All effects up to the chosen weight stack. Each weight adds {0}% points.",
          "選んだ重みまでの効果がすべて重なります。重み1ごとにポイントが{0}%増えます。",
          "所选重量及以下的效果全部叠加。每1重量点数增加{0}%。");
        A("transcend.hint", "최대 레벨 뒤에는 경험치 막대를 이야기 모드 두 번(첫 장은 한 번) · 무한 모드 세 번 채울 때마다 초월 카드를 고릅니다. (레벨은 그대로) 같은 카드를 다시 고르면 단계가 오릅니다.",
          "After max level, fill the XP bar twice in Story (once for the first card) or three times in Endless to pick a Transcendence card. (Your level stays) Pick the same card again to raise its stage.",
          "最大レベル後は、経験値バーをストーリー2回（最初は1回）・エンドレス3回満たすたびに超越カードを選びます。（レベルはそのまま）同じカードを再び選ぶと段階が上がります。",
          "达到等级上限后，故事模式每填满经验条两次（第一张一次）、无尽模式每三次可选一张超越卡。（等级不变）再次选择同一张卡会提升阶段。");
        A("transcend.codex.levelhint", "레벨이 오르면 세 장 중 하나를 고르거나 건너뜁니다. 최대 레벨 50 뒤에는 막대를 이야기 모드 두 번 · 무한 모드 세 번 채울 때마다 초월 카드를 고릅니다. (한계 돌파: 레벨 100까지 · 레벨업 세 번마다) 재료를 모두 올리면 스킬이 진화합니다.",
          "Each level-up, pick one of three cards or skip. After max level 50, pick a Transcendence card every 2 XP bars in Story or 3 in Endless. (Beyond the Limit: up to level 100, every 3 level-ups) Max all ingredients to evolve a skill.",
          "レベルアップごとに3枚から1枚を選ぶかスキップします。最大レベル50の後は、バーをストーリー2回・エンドレス3回満たすたびに超越カードを選びます。（限界突破：レベル100まで・3回レベルアップごと）素材をすべて上げるとスキルが進化します。",
          "每次升级从三张中选一张或跳过。等级上限50之后，故事模式每填满经验条两次、无尽模式每三次可选一张超越卡。（突破极限：可升至100级 · 每升3级）素材全部升满即可进化技能。");
        A("transcend.codex.rule", "최대 레벨(50) 뒤 경험치 막대를 이야기 모드 두 번(첫 장은 한 번) · 무한 모드 세 번 채울 때마다 금테 초월 카드 세 장 중 하나를 골라 판의 규칙을 바꿉니다. 같은 카드를 다시 고르면 단계가 오릅니다. (최대 3단계 · 맵 지배 2단계 · 한계 돌파는 레벨업 세 번마다)",
          "After max level (50), fill the XP bar twice in Story (once for the first card) or three times in Endless to pick one of three gold-framed Transcendence cards and change a rule of the run. Pick the same card again to raise its stage. (Up to stage 3 · Dominion 2 · Beyond the Limit: every 3 level-ups)",
          "最大レベル（50）の後、経験値バーをストーリー2回（最初は1回）・エンドレス3回満たすたびに、金枠の超越カード3枚から1枚を選んでルールを変えます。同じカードを再び選ぶと段階が上がります。（最大3段階・地の支配は2段階・限界突破は3回レベルアップごと）",
          "达到等级上限（50）后，故事模式每填满经验条两次（第一张一次）、无尽模式每三次，从三张金框超越卡中选一张改变规则。再次选择同一张卡会提升阶段。（最多3阶段 · 地图支配2阶段 · 突破极限每升3级）");
        A("transcend.codex.list", "메아리: 필살기 뒤 메아리 · 영혼 수확: 처치 수마다 파동 · 시간의 틈: 위기에 시간 정지 · 연금 손: 코인 → 경험치 · 그림자 분신: 따라와 쏘는 분신 · 중력장: 서 있으면 끌어당김 · 역전: 최후의 보루에 대폭발 · 맵 지배: 맵 물체 부활 (이야기 모드) · 연쇄 붕괴: 쓰러진 적이 터짐 · 영혼 보호막: 한 번 막음 · 영혼불 발자국: 불타는 발자국",
          "Echo: an echo after your Ultimate · Reaping: a wave every so many kills · Time Rift: time stops in danger · Alchemist's Touch: coins → XP · Shadow Clones: clones that shoot · Gravity Well: stand still to pull · Reversal: blast on Last Stand · Dominion: map objects respawn (Story) · Cascade: slain enemies burst · Soul Ward: blocks one hit · Soulfire Steps: a burning trail",
          "残響：必殺技後の残響・魂の収穫：撃破数ごとに波動・時の裂け目：危機に時間停止・錬金の手：コイン→経験値・影分身：ついて来て撃つ分身・重力場：立ち止まると引き寄せ・逆転：最後の砦で大爆発・地の支配：物体が再出現（ストーリー）・連鎖崩壊：倒れた敵が破裂・魂の障壁：1回防ぐ・魂火の足跡：燃える足跡",
          "回响：必杀技后回响 · 灵魂收割：按击杀数触发波动 · 时间裂隙：危急时时间静止 · 炼金之手：金币→经验 · 影分身：跟随射击的分身 · 重力场：静止时吸引 · 逆转：最后的堡垒触发大爆炸 · 地图支配：物体重生（故事模式）· 连锁崩坏：被击败的敌人爆裂 · 灵魂护盾：抵挡一次 · 魂火足迹：燃烧的足迹");
    }
}
