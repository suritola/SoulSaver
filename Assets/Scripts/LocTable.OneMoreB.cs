using UnityEngine;

// 1.2~ 전령 설정 · 받기 창 · 일일 도전 보상 · 출석 · 규칙 · 판 사이 손질 (지난 판 각인 · 추천 · 초월 충전) 번역
// tools: docs/review/2026-10-16-한판더-글.md 에서 뽑아 만든 표 (글을 고치면 문서와 함께)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddOneMoreB()
    {
        void A(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        A("herald.setting", "망자의 전령", "Herald of the Dead", "死者の使者", "亡者使者");
        A("herald.setting.desc", "쓰러진 뒤 전령이 한 줄을 건넵니다. 끄면 바로 결과 화면으로 갑니다.", "After you fall, the Herald speaks one line. Turn this off to go straight to the results.", "倒れたあと、使者がひと言告げます。オフにするとすぐ結果画面に進みます。", "倒下后，使者会说一句话。关闭后直接进入结果画面。");
        A("herald.name", "망자의 전령", "Herald of the Dead", "死者の使者", "亡者使者");
        A("herald.skip", "[{INTERACT}] · 클릭: 넘기기", "[{INTERACT}] · Click: Skip", "[{INTERACT}]・クリック：スキップ", "[{INTERACT}] · 点击：跳过");
        A("herald.skip.pad", "[A] 넘기기", "[A] Skip", "[A] スキップ", "[A] 跳过");
        A("claim.title", "받을 보상", "Rewards to Claim", "受け取れる報酬", "可领取的奖励");
        A("claim.desc", "이미 이룬 기록으로 열린 보상입니다. 눌러서 받으세요.", "These rewards were unlocked by what you have already achieved. Click to claim them.", "すでに達成した記録で解放された報酬です。押して受け取ってください。", "这些是根据你已达成的记录解锁的奖励。点击领取。");
        A("claim.first", "지금까지의 기록을 돌려봤습니다. 이미 넘은 이정표와 채운 칸이 여기 있습니다.", "We looked back over your record so far. The milestones you have already passed and the cells you have already filled are here.", "これまでの記録を振り返りました。すでに越えたマイルストーンと埋めたマスがここにあります。", "回顾了你至今的记录。已越过的里程碑与已填满的格子都在这里。");
        A("claim.sum", "합계 +{0} P", "Total +{0} P", "合計 +{0} P", "合计 +{0} P");
        A("claim.all", "모두 받기", "Claim All", "すべて受け取る", "全部领取");
        A("claim.one", "받기", "Claim", "受け取る", "领取");
        A("claim.done", "받았습니다.", "Claimed.", "受け取りました。", "已领取。");
        A("claim.badge", "받을 보상 {0}", "{0} to claim", "受け取れる報酬 {0}", "待领取 {0}");
        A("daily.reward.5", "오늘의 보상 · 5분  +500 P", "Today's reward · 5 min  +500 P", "今日の報酬・5分  +500 P", "今日奖励 · 5分钟  +500 P");
        A("daily.reward.10", "오늘의 보상 · 10분  +1,000 P", "Today's reward · 10 min  +1,000 P", "今日の報酬・10分  +1,000 P", "今日奖励 · 10分钟  +1,000 P");
        A("daily.reward.15", "완주!  +2,000 P", "Full run!  +2,000 P", "完走！  +2,000 P", "完赛！  +2,000 P");
        A("daily.finish.body", "15분을 버텼습니다. 오늘의 보상을 모두 받았습니다. 계속 버틸 수도, 여기서 결과를 볼 수도 있습니다.", "You survived 15 minutes and earned all of today's rewards. You can keep going or see your results now.", "15分間生き延び、今日の報酬をすべて受け取りました。このまま続けることも、ここで結果を見ることもできます。", "你坚持了15分钟，已获得今日全部奖励。可以继续坚持，也可以现在查看结果。");
        A("daily.finish.keep", "계속 버티기", "Keep Going", "続ける", "继续坚持");
        A("daily.finish.result", "결과 보기", "See Results", "結果を見る", "查看结果");
        A("daily.finish.again", "오늘의 보상은 이미 모두 받았습니다. 순위는 더 좋은 기록으로 바뀝니다.", "You have already collected all of today's rewards. Your rank updates if this run is better.", "今日の報酬はすでにすべて受け取っています。順位はより良い記録に更新されます。", "今日奖励已全部领取。成绩更好时排名会更新。");
        A("daily.done.1", "첫 완주!  각인 「고요」가 열렸습니다.", "First full run!  Soul Brand \"Stillness\" unlocked.", "初完走！  刻印「静寂」が解放されました。", "首次完赛！  烙印「寂静」已解锁。");
        A("daily.done.5", "완주 5회!  새 유물이 석관에 잠들었습니다.", "Five full runs!  A new relic sleeps in the sarcophagi.", "完走5回！  新しい遺物が石棺に眠りました。", "完赛5次！  一件新遗物沉睡于石棺之中。");
        A("daily.done.10", "완주 10회!  커서 스킨 「달력」을 받았습니다.", "Ten full runs!  Cursor skin \"Calendar\" received.", "完走10回！  カーソルスキン「カレンダー」を受け取りました。", "完赛10次！  获得光标皮肤「日历」。");
        A("daily.done.30", "완주 30회!  칭호 「매일 오는 자」", "Thirty full runs!  Title \"The Daily Visitor\"", "完走30回！  称号「毎日来る者」", "完赛30次！  称号「每日来访者」");
        A("daily.streak.3", "3일 연속 출석!  +1,000 P", "3 days in a row!  +1,000 P", "3日連続出席！  +1,000 P", "连续出席3天！  +1,000 P");
        A("daily.streak.7", "7일 연속 출석!  +3,000 P", "7 days in a row!  +3,000 P", "7日連続出席！  +3,000 P", "连续出席7天！  +3,000 P");
        A("daily.streak.30", "30일 연속 출석!  +10,000 P", "30 days in a row!  +10,000 P", "30日連続出席！  +10,000 P", "连续出席30天！  +10,000 P");
        A("daily.streak.label", "연속 {0}일", "{0} days in a row", "連続 {0}日", "连续 {0} 天");
        A("daily.streak.broken", "연속 출석이 끊겼습니다. 오늘부터 다시 셉니다.", "Your attendance streak was broken. It starts again today.", "連続出席が途切れました。今日から数え直します。", "连续出席中断了。从今天重新计算。");
        A("daily.attend.how", "출석은 그날 일일 도전을 1분 이상 한 것으로 셉니다.", "Attendance counts when you play that day's challenge for at least one minute.", "出席はその日のデイリーチャレンジを1分以上プレイすると数えられます。", "当天进行每日挑战满1分钟即算出席。");
        A("daily.reset.local", "({0}에 바뀜)", "(changes at {0})", "（{0}に切り替わり）", "（{0} 更换）");
        A("daily.herocell", "오늘의 영웅으로 완주!  이 영웅의 계약 보드 1칸을 미리 채웠습니다.", "Full run as today's hero!  One cell on this hero's contract board has been filled in advance.", "今日の英雄で完走！  この英雄の契約ボードを1マス先に埋めました。", "以今日英雄完赛！  已提前填满这位英雄契约板的1格。");
        A("daily.rule.glass.name", "유리 대포의 날", "Glass Cannon Day", "ガラスの大砲の日", "玻璃大炮之日");
        A("daily.rule.glass.desc", "공격력 +40%. 대신 최대 체력 -30%", "Attack +40%. But max HP -30%", "攻撃力 +40%。代わりに最大体力 -30%", "攻击力 +40%。但最大生命 -30%");
        A("daily.rule.greed.name", "탐욕의 날", "Greed Day", "強欲の日", "贪婪之日");
        A("daily.rule.greed.desc", "경험치 · 코인 +60%. 대신 적 체력 +30%", "EXP · coins +60%. But enemy HP +30%", "経験値・コイン +60%。代わりに敵の体力 +30%", "经验 · 金币 +60%。但敌人生命 +30%");
        A("daily.rule.iron.name", "무쇠 맹세의 날", "Iron Oath Day", "鉄の誓いの日", "钢铁誓约之日");
        A("daily.rule.iron.desc", "받는 피해 -25%. 대신 공격력 -15%", "Damage taken -25%. But attack -15%", "受けるダメージ -25%。代わりに攻撃力 -15%", "受到伤害 -25%。但攻击力 -15%");
        A("daily.rule.frenzy.name", "광란의 날", "Frenzy Day", "狂乱の日", "狂乱之日");
        A("daily.rule.frenzy.desc", "공격력 +25%. 대신 적 이동 속도 +20% · 적 스킬 빈도 +30%", "Attack +25%. But enemy speed +20% · enemy skill frequency +30%", "攻撃力 +25%。代わりに敵の移動速度 +20%・敵のスキル頻度 +30%", "攻击力 +25%。但敌人移动速度 +20% · 敌人技能频率 +30%");
        A("daily.rule.calm.name", "고요의 날", "Stillness Day", "静寂の日", "寂静之日");
        A("daily.rule.calm.desc", "적 이동 속도 -15%. 대신 필살기 충전 -35%", "Enemy speed -15%. But Ultimate charge -35%", "敵の移動速度 -15%。代わりに必殺技チャージ -35%", "敌人移动速度 -15%。但必杀技充能 -35%");
        A("daily.rule.bulwark.name", "철벽의 날", "Bulwark Day", "鉄壁の日", "铁壁之日");
        A("daily.rule.bulwark.desc", "최대 체력 +40%. 대신 공격력 -20%", "Max HP +40%. But attack -20%", "最大体力 +40%。代わりに攻撃力 -20%", "最大生命 +40%。但攻击力 -20%");
        A("daily.rule.dance.name", "죽음의 춤의 날", "Dance of Death Day", "死の舞踏の日", "死亡之舞之日");
        A("daily.rule.dance.desc", "공격력 +30% · 경험치 · 코인 +30%. 대신 받는 피해 +50%", "Attack +30% · EXP · coins +30%. But damage taken +50%", "攻撃力 +30%・経験値・コイン +30%。代わりに受けるダメージ +50%", "攻击力 +30% · 经验 · 金币 +30%。但受到伤害 +50%");
        A("daily.rule.taste", "영혼 각인의 규칙을 하루 빌려 씁니다. 각인을 아직 열지 않았어도 됩니다.", "A Soul Brand's rule, borrowed for a day. You need not have unlocked the brand.", "魂の刻印のルールを一日借りて使います。刻印をまだ解放していなくても構いません。", "借用灵魂烙印的规则一天。即使尚未解锁该烙印也可以。");
        A("brand.same", "지난 판 각인 그대로", "Same Brand as Last Run", "前回の刻印をそのまま", "沿用上局烙印");
        A("brand.same.tip", "지난 판에 처음 고른 각인을 이번 판에도 씁니다.", "Uses the brand you picked at the start of your last run.", "前回のラン開始時に選んだ刻印を今回も使います。", "使用上一局开始时选择的烙印。");
        A("recommend.hero", "숙련 0인 {0}로 해 보는 건 어떨까요? 난이도를 내려도 계약 · 숙련 · 기억은 그대로 오릅니다.", "How about trying {0}, who has no mastery yet? Lowering the difficulty still advances contracts, mastery and memories.", "熟練0の{0}で試してみませんか？難易度を下げても契約・熟練・記憶はそのまま進みます。", "要不要试试熟练度为0的{0}？降低难度后契约、熟练与记忆照样会提升。");
        A("recommend.hero.noname", "아직 숙련이 없는 영웅으로 해 보는 건 어떨까요? 난이도를 내려도 계약 · 숙련 · 기억은 그대로 오릅니다.", "How about a hero with no mastery yet? Lowering the difficulty still advances contracts, mastery and memories.", "まだ熟練のない英雄で試してみませんか？難易度を下げても契約・熟練・記憶はそのまま進みます。", "要不要试试还没有熟练度的英雄？降低难度后契约、熟练与记忆照样会提升。");
        A("recommend.diff", "한 단계 낮은 난이도로 해 보는 건 어떨까요? 계약 · 숙련 · 기억은 어느 난이도에서도 오릅니다.", "How about one difficulty lower? Contracts, mastery and memories advance at any difficulty.", "一段低い難易度で試してみませんか？契約・熟練・記憶はどの難易度でも進みます。", "要不要试试低一档的难度？契约、熟练与记忆在任何难度都会提升。");
        A("recommend.hint", "세 번 모두 같은 곳에서 쓰러졌습니다. 결과 화면의 「쓰러진 곳」을 한 번 읽어 보세요.", "You fell to the same thing all three times. Take a look at where you fell on the result screen.", "3回とも同じ原因で倒れました。結果画面の「倒れた原因」を一度読んでみてください。", "三次都倒在同一处。看一眼结果画面上的「倒下于」吧。");
        A("transcend.pips", "초월 카드까지 막대 {0}/3", "{0}/3 bars to the next Transcendence card", "超越のカードまでバー {0}/3", "距下一张超越卡 {0}/3 条");
        A("transcend.pips.endless", "초월 카드까지 막대 {0}/3 (무한 모드)", "{0}/3 bars to the next Transcendence card (Endless)", "超越のカードまでバー {0}/3（エンドレス）", "距下一张超越卡 {0}/3 条（无尽模式）");
    }
}
