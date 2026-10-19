// 번역표: 도적 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   (Paths/Paths.Rogue.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
// 용어: 표창 Throwing Star · 手裏剣 · 飞镖 / 돌진(출혈 돌진) Dash · ダッシュ · 冲刺 / 게이지 gauge · ゲージ · 能量槽 / 중첩 stack · 重ね · 层
public static partial class LocTable
{
    static void AddPathsRogue()
    {
        // ================================================================= 출혈
        PA("출혈", "Bleed", "出血", "出血");
        PA("5중첩 출혈 · 전염", "5-stack bleed · Contagion", "5重ね出血・感染", "5层出血 · 传染");
        PA("독 바른 칼날", "Envenomed Edge", "毒塗りの刃", "淬毒之刃");
        PA("모든 표창이 출혈을 입히고, 출혈이 5중첩까지 쌓입니다.",
           "Every Throwing Star inflicts Bleed, and Bleed stacks up to 5 times.",
           "すべての手裏剣が出血を与え、出血が5重ねまで重なります。",
           "所有飞镖都会造成出血，出血最多叠加5层。");
        PA("직접 피해가 35% 줄어듭니다.", "Direct damage is reduced by 35%.", "直接ダメージが35%減ります。", "直接伤害降低35%。");
        PA("보스 지속 · 무리 전염", "Sustained boss damage · Spreads through crowds", "ボスへの持続・群れへの感染", "对首领持续输出 · 在敌群中传染");
        PA("즉시 처치", "Instant kills", "即時撃破", "瞬间击杀");
        PA("5중첩 적을 돌진으로 지나가면 출혈 폭발이 바로 터집니다.",
           "Dashing through an enemy with 5 stacks makes its Bleed burst at once.",
           "5重ねの敵をダッシュで通り抜けると、出血がすぐに破裂します。",
           "冲刺穿过5层出血的敌人时，出血会立刻爆发。");
        PA("출혈로 쓰러진 적이 둘레 둘에게 출혈 2중첩을 옮깁니다.",
           "Enemies killed by Bleed pass 2 Bleed stacks to 2 nearby enemies.",
           "出血で倒れた敵が、周りの2体に出血を2重ねうつします。",
           "因出血倒下的敌人会把2层出血传给周围2个敌人。");
        PA("출혈 피해가 1.3배가 되고, 보스에게는 10중첩까지 쌓입니다.",
           "Bleed damage becomes ×1.3, and Bleed stacks up to 10 times on bosses.",
           "出血ダメージが1.3倍になり、ボスには10重ねまで重なります。",
           "出血伤害变为1.3倍，对首领最多可叠加10层。");

        // ================================================================= 그림자
        PA("그림자", "Shadow", "影", "暗影");
        PA("돌진 2회 · 미끼 분신", "2 Dash charges · Decoy clone", "ダッシュ2回・おとり分身", "2次冲刺 · 诱饵分身");
        PA("그림자 걸음", "Shadow Step", "影歩き", "影步");
        PA("돌진을 2번 모아 둘 수 있고, 돌진을 시작한 자리에 미끼 분신이 1.5초 동안 남아 적을 끌어들입니다.",
           "Store up to 2 Dashes. A decoy clone stays where each Dash began for 1.5 seconds and draws enemies to it.",
           "ダッシュを2回ためておけます。ダッシュを始めた場所におとり分身が1.5秒残り、敵を引きつけます。",
           "可以储存2次冲刺。冲刺起点会留下诱饵分身1.5秒，吸引敌人。");
        PA("돌진 출혈이 30% 줄어듭니다.", "Dash Bleed is reduced by 30%.", "ダッシュの出血が30%減ります。", "冲刺造成的出血降低30%。");
        PA("생존 · 회피 · 보스 시선 돌리기", "Survival · Evasion · Drawing the boss away", "生存・回避・ボスの目をそらす", "生存 · 闪避 · 引开首领");
        PA("화력", "Firepower", "火力", "火力");
        PA("분신이 사라질 때 연막이 퍼집니다.", "Clones release a Smoke Screen when they vanish.", "分身が消えるとき煙幕が広がります。", "分身消失时会散开烟幕。");
        PA("분신 연막 안에서 던진 표창은 치명타(×2)가 됩니다.",          // 1.4.2~ 분신 연막만
           "Throwing Stars thrown inside a clone's Smoke Screen become critical hits (×2).",
           "分身の煙幕の中で投げた手裏剣は会心(×2)になります。",
           "在分身烟幕中投出的飞镖必定暴击（×2）。");
        PA("돌진을 3번 모아 두고, 분신도 표창을 함께 던집니다.",
           "Store up to 3 Dashes, and clones throw Throwing Stars with you.",
           "ダッシュを3回ためられ、分身も一緒に手裏剣を投げます。",
           "可以储存3次冲刺，分身也会一起投掷飞镖。");

        // ================================================================= 회귀 칼날
        PA("회귀 칼날", "Returning Blade", "回帰の刃", "回旋刃");
        PA("되돌아오는 표창 · 탄 경제", "Returning stars · Ammo economy", "戻ってくる手裏剣・弾の節約", "回旋飞镖 · 弹药经济");
        PA("곡선 투척", "Curved Throw", "曲線投げ", "弧线投掷");
        PA("표창이 8칸 끝에서 되돌아오며 다시 벱니다. (피해 25%) 돌아온 표창을 몸으로 받으면 탄창이 한 발 찹니다. (두 번 던질 때마다 한 번)",      // 1.4.2~ 두 묶음에 한 번
           "Throwing Stars turn back after 8 tiles and cut again on the way. (25% damage) Catching a returning star with your body refills 1 shot. (Once every two throws)",
           "手裏剣が8マス先で戻ってきて、もう一度斬ります。(ダメージ25%) 戻った手裏剣を体で受けると弾倉が1発回復します。(2回投げるごとに1回)",
           "飞镖飞出8格后折返，再次斩击。（伤害25%）用身体接住飞回的飞镖可补充1发。（每投掷两次1次）");
        PA("사거리가 무한에서 8칸으로 줄어듭니다.", "Range drops from unlimited to 8 tiles.", "射程が無限から8マスに縮みます。", "射程从无限缩短为8格。");
        PA("근중거리 무리 · 탄 경제", "Close-to-mid-range crowds · Ammo economy", "近〜中距離の群れ・弾の節約", "近中距离敌群 · 弹药经济");
        PA("원거리", "Long range", "遠距離", "远距离");
        PA("왕복으로 같은 적을 맞히면 던지기 연습이 2중첩 쌓입니다.",
           "Hitting the same enemy on both legs of the trip adds 2 Practiced Throw stacks.",
           "行きと帰りで同じ敵に当てると、投てき練習が2重ねたまります。",
           "往返都命中同一敌人时，投掷练习叠加2层。");
        PA("돌아올 때 몸 둘레를 한 바퀴 돈 뒤 돌아옵니다.",
           "Returning stars circle around you once before coming back.",
           "戻るとき体の周りを1周してから戻ります。",
           "飞回时会绕身体转一圈再回来。");
        PA("표창이 한 번 더 왕복합니다. (두 번째 왕복은 피해 50%)",      // 1.4.2~ 두 번째 왕복 50%
           "Throwing Stars make one more round trip. (50% damage on the second trip)",
           "手裏剣がもう1回往復します。(2回目の往復はダメージ50%)",
           "飞镖再往返一次。（第二次往返伤害50%）");

        // ================================================================= 질주 암살
        PA("질주 암살", "Dash Assassin", "疾走暗殺", "疾走暗杀");
        PA("긴 돌진 직접 피해 · 순간 화력", "Long Dash direct damage · Burst damage", "長いダッシュの直接ダメージ・瞬間火力", "长距离冲刺直接伤害 · 瞬间爆发");
        PA("돌진 거리 ×1.5, 돌진 게이지 −30%, 돌진 경로가 출혈 대신 직접 피해를 줍니다. (공격력 500%)",
           "Dash distance ×1.5, Dash gauge cost −30%, and the Dash path deals direct damage instead of Bleed. (500% Attack)",
           "ダッシュ距離×1.5、ダッシュのゲージ−30%、ダッシュの経路が出血のかわりに直接ダメージを与えます。(攻撃力500%)",
           "冲刺距离×1.5，冲刺能量槽消耗−30%，冲刺路径改为造成直接伤害而非出血。（攻击力500%）");
        PA("평타 공격 속도가 20% 느려집니다.", "Basic attack speed is 20% slower.", "通常攻撃の攻撃速度が20%遅くなります。", "普通攻击速度降低20%。");
        PA("기동 · 관통 · 보스 순간 화력", "Mobility · Piercing · Burst on bosses", "機動・貫通・ボスへの瞬間火力", "机动 · 贯穿 · 对首领瞬间爆发");
        PA("게이지 의존", "Gauge-dependent", "ゲージ頼み", "依赖能量槽");
        PA("돌진으로 처치하면 게이지 25%를 돌려받습니다. (한 번의 돌진으로 돌려받는 게이지는 모두 합쳐 40%까지)",      // 1.4.2~ 반환 상한
           "Killing with a Dash refunds 25% of the gauge. (Gauge refunded from one Dash is capped at 40% in total)",
           "ダッシュで倒すとゲージを25%取り戻します。(1回のダッシュで戻るゲージは合計40%まで)",
           "冲刺击杀敌人时返还25%能量槽。（单次冲刺返还的能量槽合计最多40%）");
        PA("돌진한 길이 X자로 엇갈리면 교차점이 터집니다. (공격력 300%)",
           "When Dash paths cross in an X, the crossing point explodes. (300% Attack)",
           "ダッシュした道がX字に交わると、交点が爆発します。(攻撃力300%)",
           "冲刺路径呈X形交叉时，交叉点会爆炸。（攻击力300%）");
        PA("돌진 중 처치하면 바로 다시 돌진합니다. (3연속 · 이어지는 돌진은 보스에게 피해 60%)",      // 1.4.2~ 이어지는 돌진 보스 피해 60%
           "Killing during a Dash lets you Dash again at once. (Up to 3 in a row · follow-up Dashes deal 60% damage to bosses)",
           "ダッシュ中に倒すと、すぐにもう一度ダッシュします。(3連続・続くダッシュはボスへのダメージ60%)",
           "冲刺中击杀敌人会立刻再次冲刺。（最多3连 · 后续冲刺对首领伤害为60%）");

        // ================================================================= 광란
        PA("광란", "Frenzy", "狂乱", "狂乱");
        PA("처치마다 빨라짐 · 무리", "Faster with every kill · Crowds", "倒すたび速くなる・群れ", "每次击杀都更快 · 敌群");
        PA("탄창 6 → 10, 공격 속도 +25%, 처치할 때마다 1.5초 동안 공격 속도 +3%. (10중첩)",
           "Magazine 6 → 10, attack speed +25%, and each kill grants +3% attack speed for 1.5 seconds. (Stacks 10 times)",
           "弾倉6 → 10、攻撃速度+25%、倒すたびに1.5秒間攻撃速度+3%。(10重ね)",
           "弹匣6 → 10，攻击速度+25%，每次击杀在1.5秒内攻击速度+3%。（10层）");
        PA("받는 피해가 15% 늘고, 표창 피해가 20% 줄어듭니다.",
           "Damage taken is increased by 15%, and Throwing Star damage is reduced by 20%.",
           "受けるダメージが15%増え、手裏剣のダメージが20%減ります。",
           "受到的伤害增加15%，飞镖伤害降低20%。");
        PA("무리", "Crowds", "群れ", "敌群");
        PA("피격 위험", "Risk of getting hit", "被弾の危険", "受击风险");
        PA("10중첩이면 표창을 두 개씩 던집니다.", "At 10 stacks, you throw two Throwing Stars at a time.", "10重ねなら手裏剣を2つずつ投げます。", "叠满10层时，每次投出两倍飞镖。");
        PA("빈 주머니의 비가 10처치마다도 내립니다.", "Empty-Pouch Rain also falls every 10 kills.", "10体倒すごとにも空の袋の雨が降ります。", "每击杀10个敌人也会降下空囊之雨。");
        PA("맞아도 중첩이 떨어지지 않습니다.", "Getting hit no longer removes your stacks.", "被弾しても重ねが消えません。", "受击时层数不再消失。");

        // ================================================================= 새 교차
        PA("그림자 출혈", "Shadow Bleed", "影の出血", "暗影出血");
        PA("분신이 표창을 던지고, 분신의 표창이 출혈 2중첩을 입힙니다.",
           "Clones throw Throwing Stars, and their stars inflict 2 Bleed stacks.",
           "分身が手裏剣を投げ、分身の手裏剣が出血を2重ね与えます。",
           "分身会投掷飞镖，分身的飞镖造成2层出血。");
        PA("폭풍 질주", "Storm Dash", "嵐の疾走", "风暴疾驰");
        PA("돌진이 광란 5중첩을 바로 채웁니다.", "Dashing instantly fills 5 Frenzy stacks.", "ダッシュで狂乱が5重ねまで一気にたまります。", "冲刺立即补满5层狂乱。");

        // ================================================================= 피해 출처 이름 (판 결과 「준 피해」)
        PA("출혈 터짐", "Bleed Burst", "出血破裂", "出血爆裂");
        PA("돌진 교차", "Dash Cross", "ダッシュ交差", "冲刺交叉");
        PA("출혈 중첩", "Bleed Stacks", "出血の重ね", "出血叠层");
    }
}
