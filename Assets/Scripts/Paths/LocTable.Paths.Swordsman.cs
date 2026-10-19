// 번역표: 검사 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명 · 효과가 띄우는 글
//   (Paths/Paths.Swordsman.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
// 용어: 근접 특성 Melee traits · 날아가는 검기 Flying Slash · 회전 베기 Spin Slash · 혈갑 Blood Guard · 강철 의지 Iron Will · 균열 Fracture (기존 번역과 같게)
public static partial class LocTable
{
    static void AddPathsSwordsman()
    {
        // ================================================================= 검기
        PA("검기", "Sword Wave", "剣気", "剑气");
        PA("휘두를 때마다 검기 · 안전 거리", "A wave on every swing · Safe distance", "振るたびに剣気 · 安全な間合い", "每次挥砍放出剑气 · 安全距离");
        PA("검기 방출", "Wave Release", "剣気放出", "剑气释放");
        PA("휘두를 때마다 검기가 반드시 날아갑니다. (9칸 · 베기 피해 60% · 관통 · 「날아가는 검기」 단계마다 +5%) 휘두르기 범위는 20% 줄어듭니다.",
           "Every swing always launches a sword wave. (9 tiles · 60% of slash damage · pierces · +5% per Flying Slash rank) Your swing range shrinks by 20%.",
           "振るたびに必ず剣気が飛びます。（9マス · 斬撃ダメージの60% · 貫通 · 「飛ぶ剣気」1段階ごとに+5%）振りの範囲は20%狭くなります。",
           "每次挥砍必定放出剑气。（9格 · 斩击伤害的60% · 穿透 · 「飞行剑气」每级+5%）挥砍范围缩小20%。");
        PA("근접 특성(휘두르는 동안 받는 피해 절반)이 사라집니다.",
           "You lose your Melee trait (half damage taken while swinging).",
           "近接特性（振っている間は受けるダメージ半分）がなくなります。",
           "失去近战特性（挥砍时受到的伤害减半）。");
        PA("안전 거리 · 일렬 · 보스", "Safe distance · Lines · Bosses", "安全な間合い · 一列 · ボス", "安全距离 · 一列敌人 · 首领");
        PA("근접 생존력", "Close-range survival", "近接での生存力", "近身生存能力");
        PA("검기로 같은 적을 세 번 맞히면 균열이 바로 터집니다.",
           "Hitting the same enemy three times with sword waves sets off Fracture at once.",
           "剣気で同じ敵に3回当てると、亀裂がすぐに弾けます。",
           "剑气命中同一敌人三次时，裂痕立即爆发。");
        PA("검기가 두 줄기 X자로 날아가고(한 줄기 70% 세기), 교차점에서 잔상이 터집니다. (베기 피해 30%)",
           "Sword waves fly as two streams in an X (70% strength each), and an afterimage bursts where they cross. (30% of slash damage)",
           "剣気が2本のX字に飛び（1本70%の威力）、交差点で残像が弾けます。（斬撃ダメージの30%）",
           "剑气化为两道呈X形飞出（每道70%威力），并在交汇点爆发残影。（斩击伤害的30%）");
        PA("검기가 끝에서 되돌아옵니다. (80% 세기)",
           "Sword waves return from the end of their flight. (80% strength)",
           "剣気が端から戻ってきます。（80%の威力）",
           "剑气飞到尽头后折返。（80%威力）");

        // ================================================================= 받아치기
        PA("받아치기", "Parry", "受け返し", "招架反击");
        PA("막기 0.25초 · 반격", "0.25s block · Counter", "0.25秒の受け · 反撃", "0.25秒格挡 · 反击");
        PK("우클릭을 누른 첫 0.25초가 막기가 됩니다. 그사이 맞으면 피해 0. 투사체 · 보스 공격을 막으면 0.6초 무적 · 다음 베기 ×3도 얻습니다. (3초에 한 번 · 잡몹 몸통은 피해만 막음) 실패해도 손해는 없지만, 막기는 1.2초에 한 번(성공하면 2초에 한 번)입니다.", "우클릭 뒤 0.25초 동안 막기가 되어 그사이 받는 피해가 0입니다. 투사체 · 보스 공격을 막으면 0.6초 무적과 다음 베기 ×3도 얻습니다. (3초에 한 번 · 잡몹 몸통은 피해만 막음) 막기는 1.2초에 한 번(성공하면 2초에 한 번)입니다.", "For 0.25s after right-click you block, taking 0 damage. Blocking a projectile or boss attack also grants 0.6s invincibility and a ×3 next slash. (Once per 3s · enemy body contact is only blocked) You can block once per 1.2s (once per 2s after a success).", "右クリック後の0.25秒間が受けになり、その間のダメージは0です。弾やボスの攻撃を受けると0.6秒無敵と次の斬撃×3も得ます。（3秒に1回・雑魚の体当たりはダメージを防ぐだけ）受けは1.2秒に1回（成功すると2秒に1回）です。", "按下右键后0.25秒内为格挡，期间受到的伤害为0。格挡弹幕或首领攻击时还获得0.6秒无敌和下一次斩击×3。（每3秒一次 · 小怪身体碰撞只挡伤害）格挡每1.2秒一次（成功后每2秒一次）。");
        PA("회전 베기를 끝까지 모으는 시간이 30% 늘어납니다.",
           "Charging Spin Slash to full takes 30% longer.",
           "回転斬りを最大までためる時間が30%長くなります。",
           "回旋斩蓄满所需时间增加30%。");
        PA("보스 탄막 · 생존", "Boss barrages · Survival", "ボスの弾幕 · 生存", "首领弹幕 · 生存");
        PA("무리 · 손 기술", "Hordes · Execution skill", "群れ · 操作技術", "敌群 · 操作技巧");
        PA("투사체 · 보스 공격을 막으면 둘레의 적 투사체를 모두 되돌려 보냅니다.",
           "Blocking a projectile or boss attack sends every nearby enemy projectile back.",
           "弾やボスの攻撃を受けると、周りの敵の弾をすべて打ち返します。",
           "格挡弹幕或首领攻击时，将周围所有敌方弹幕反弹回去。");
        PA("3초 안에 두 번 막으면 4초 동안 역습 자세가 됩니다. (사거리 ×1.4 · 피해 ×1.5)",
           "Block twice within 3s to enter Counter Stance for 4s. (Range ×1.4 · Damage ×1.5)",
           "3秒以内に2回受けると、4秒間逆襲の構えになります。（射程×1.4 · ダメージ×1.5）",
           "3秒内格挡两次，进入4秒反击架势。（范围×1.4 · 伤害×1.5）");
        PA("막기가 0.4초로 늘고, 투사체 · 보스 공격을 막을 때마다 필살기 게이지가 20% 찹니다. (3초에 한 번)",
           "The block lasts 0.4s, and each projectile or boss attack you block fills 20% of the Ultimate Gauge. (Once per 3s)",
           "受けが0.4秒に伸び、弾やボスの攻撃を受けるたびに必殺技ゲージが20%たまります。（3秒に1回）",
           "格挡延长至0.4秒，每次格挡弹幕或首领攻击时充能20%必杀技槽。（每3秒一次）");
        PA("막기!", "Parry!", "受け！", "招架！");
        PA("역습 자세!", "Counter Stance!", "逆襲の構え！", "反击架势！");
        PA("반격!", "Counter!", "反撃！", "反击！");

        // ================================================================= 피의 맹세
        PA("피의 맹세", "Blood Oath", "血の誓い", "血之誓约");
        PA("체력을 바쳐 흡혈 · 근접 지속", "Pay health for lifesteal · Sustained melee", "体力を捧げて吸血 · 近接で粘る", "献出生命换取吸血 · 近战持久");
        PA("휘두를 때마다 지금 체력의 2%를 바치고, 베기 피해가 35% 늘며 흡혈이 2배가 됩니다.",
           "Each swing offers 2% of your current health. Slash damage +35% and lifesteal is doubled.",
           "振るたびに現在の体力の2%を捧げ、斬撃ダメージが35%増え、吸血が2倍になります。",
           "每次挥砍献出当前生命的2%，斩击伤害提高35%，吸血效果翻倍。");
        PA("물약 · 재생 회복이 50% 줄어듭니다.",
           "Healing from potions and regeneration is reduced by 50%.",
           "ポーションと再生の回復量が50%減ります。",
           "药水与生命回复的治疗量降低50%。");
        PA("근접 무리 지속", "Sustained melee vs hordes", "近接で群れと粘る", "近战持续对抗敌群");
        PA("원거리 탄막", "Ranged barrages", "遠距離の弾幕", "远程弹幕");
        PA("체력이 50% 아래면 피해 +25%, 25% 아래면 +50%입니다.",
           "Below 50% health, damage +25%. Below 25%, +50%.",
           "体力が50%未満ならダメージ+25%、25%未満なら+50%です。",
           "生命低于50%时伤害+25%，低于25%时+50%。");
        PA("혈갑 보호막이 가득 차면 베기가 피의 파동이 되어 길게 뻗습니다. (9칸 · 베기 피해 50%)",
           "While your Blood Guard shield is full, slashes become a far-reaching blood wave. (9 tiles · 50% of slash damage)",
           "血甲のシールドが満タンの間、斬撃が血の波動となって長く伸びます。（9マス · 斬撃ダメージの50%）",
           "血甲护盾充满时，斩击化为血之波动远远延伸。（9格 · 斩击伤害的50%）");
        PA("장마다 한 번(무한 모드는 보스를 쓰러뜨릴 때마다) 체력 1로 버티고, 3초 동안 흡혈이 3배가 됩니다.",
           "Once per chapter (in Endless, once per boss defeated), survive with 1 health and triple your lifesteal for 3s.",
           "章ごとに1回（エンドレスモードではボスを倒すたびに）、体力1で耐え、3秒間吸血が3倍になります。",
           "每章一次（无尽模式中每击败一个首领一次），以1点生命撑住，并在3秒内吸血变为3倍。");

        // ================================================================= 검무
        PA("검무", "Sword Dance", "剣舞", "剑舞");
        PA("미끄러지며 베기 · 기동", "Gliding slashes · Mobility", "滑りながら斬る · 機動", "滑行斩击 · 机动");
        PA("검무 보법", "Dancing Steps", "剣舞の歩法", "剑舞步法");
        PA("움직이며 휘두르면 진행 방향으로 1.5칸 미끄러지며 벱니다. 움직이는 동안 공격 속도 +15%.",
           "Swinging while moving makes you glide 1.5 tiles in your direction as you slash. Attack speed +15% while moving.",
           "動きながら振ると、進行方向へ1.5マス滑りながら斬ります。移動中は攻撃速度+15%。",
           "移动中挥砍时，会朝前进方向滑行1.5格并斩击。移动时攻击速度+15%。");
        PA("멈춰서 휘두르면 공격 속도가 20% 느려집니다.",
           "Swinging while standing still slows your attack speed by 20%.",
           "止まって振ると攻撃速度が20%遅くなります。",
           "原地挥砍时攻击速度降低20%。");
        PA("기동 · 무리", "Mobility · Hordes", "機動 · 群れ", "机动 · 敌群");
        PA("서서 싸우는 보스 · 결계", "Stand-and-fight bosses · Barriers", "立ち止まるボス戦 · 結界", "站桩首领战 · 结界");
        PA("미끄러진 길에 칼날 잔상이 1초 동안 남습니다. (0.25초마다 베기 피해 20%)",
           "Your glide path leaves blade afterimages for 1s. (20% of slash damage every 0.25s)",
           "滑った道に刃の残像が1秒間残ります。（0.25秒ごとに斬撃ダメージの20%）",
           "滑行路径上留下持续1秒的刀刃残影。（每0.25秒造成斩击伤害的20%）");
        PA("검무가 최대 중첩이면 다섯 번째 휘두르기마다 회전 베기가 저절로 나갑니다. (30% 세기)",
           "At max Sword Dance stacks, every fifth swing unleashes a Spin Slash on its own. (30% strength)",
           "剣舞が最大スタックの間、5回目の振りごとに回転斬りが自動で出ます。（30%の威力）",
           "剑舞叠满时，每第五次挥砍自动释放回旋斩。（30%威力）");
        PA("미끄러질 때 0.1초 무적이 되고(0.6초에 한 번), 2.5칸을 미끄러집니다.",
           "Gliding grants 0.1s of invincibility (once per 0.6s), and you glide 2.5 tiles.",
           "滑る時に0.1秒無敵になり（0.6秒に1回）、2.5マス滑ります。",
           "滑行时获得0.1秒无敌（每0.6秒一次），并滑行2.5格。");

        // ================================================================= 회오리
        PA("회오리", "Whirlwind", "旋風", "旋风");
        PA("원형 베기 · 모으며 베기", "Circular slashes · Slash while charging", "円形斬り · ためながら斬る", "环形斩击 · 蓄力时斩击");
        PA("회전 검사", "Spinning Blade", "回転剣士", "回旋剑士");
        PA("평타가 몸 둘레 원형 베기가 됩니다. (반경 85% · 피해 80%) 회전 베기를 모으는 동안 0.5초마다 둘레를 벱니다. (베기 피해 50%)",
           "Your basic attack becomes a circular slash around you. (85% radius · 80% damage) While charging Spin Slash, you cut around you every 0.5s. (50% of slash damage)",
           "通常攻撃が体の周りを斬る円形斬りになります。（半径85% · ダメージ80%）回転斬りをためている間、0.5秒ごとに周りを斬ります。（斬撃ダメージの50%）",
           "普通攻击变为环绕自身的环形斩击。（半径85% · 伤害80%）蓄力回旋斩期间，每0.5秒斩击周围。（斩击伤害的50%）");
        PA("한 적에게 주는 피해가 낮습니다.", "Low damage against a single enemy.", "単体へのダメージが低めです。", "对单个敌人的伤害较低。");
        PA("포위 무리", "Surrounding hordes", "包囲する群れ", "包围的敌群");
        PA("보스 단일", "Single bosses", "ボス単体", "单个首领");
        PA("모으는 동안 강철 의지 끌어당김이 기본 4칸으로 생깁니다.",
           "While charging, you get Iron Will's pull with a base radius of 4 tiles.",
           "ためている間、鋼の意志の引き寄せが基本4マスで発生します。",
           "蓄力期间自带半径4格的钢铁意志牵引。");
        PA("회전 베기가 끝난 자리에 2초 동안 소용돌이가 남아 적을 끌어당기고 벱니다.",
           "Spin Slash leaves a vortex for 2s that pulls in and cuts enemies.",
           "回転斬りの跡に2秒間渦が残り、敵を引き寄せて斬ります。",
           "回旋斩结束处留下持续2秒的漩涡，牵引并斩击敌人。");
        PA("회전 베기가 끝나도 3초 동안 계속 돌며 둘레를 벱니다. (0.3초마다 베기 피해 25%)",
           "After Spin Slash ends, you keep spinning for 3s, cutting around you. (25% of slash damage every 0.3s)",
           "回転斬りが終わっても3秒間回り続け、周りを斬ります。（0.3秒ごとに斬撃ダメージの25%）",
           "回旋斩结束后继续旋转3秒，斩击周围。（每0.3秒造成斩击伤害的25%）");

        // ================================================================= 새 교차
        PA("피의 검기", "Blood Wave", "血の剣気", "血之剑气");
        PA("검기가 맞힌 적마다 체력을 0.3 흡혈하고(1초에 최대 4), 피의 맹세의 피해 증가가 검기에도 붙습니다.",
           "Sword waves drain 0.3 health per enemy hit (up to 4 per second), and Blood Oath's damage bonus applies to them too.",
           "剣気が当たった敵ごとに体力を0.3吸血し（1秒に最大4）、血の誓いのダメージ増加が剣気にも乗ります。",
           "剑气每命中一个敌人吸取0.3点生命（每秒最多4点），血之誓约的伤害提升也作用于剑气。");
        PA("칼날 폭풍", "Blade Tempest", "刃の暴風", "利刃狂岚");
        PA("미끄러지기가 끝날 때 원형 베기가 일어납니다. (베기 피해 50%)",
           "A circular slash erupts when a glide ends. (50% of slash damage)",
           "滑り終わりに円形斬りが起こります。（斬撃ダメージの50%）",
           "滑行结束时触发一次环形斩击。（斩击伤害的50%）");
    }
}
