// 번역표: 거너 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름) — 거너 담당이 채움
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   (Paths/Paths.Gunner.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
public static partial class LocTable
{
    static void AddPathsGunner()
    {
        // 1.4.8~ 입문 카드 · 공명 문구 줄이기 (docs/review/2026-10-21-UI-정리-기획.md 3절): 키는 그대로 · PK 로 화면 문구만
        // ================================================================= 관통 저격
        PA("관통 저격", "Piercing Snipe", "貫通狙撃", "贯穿狙击");
        PK("멈춰서 모아 쏘는 관통 저격탄", "멈춰서 저격 · 관통", "Stand still · Pierce", "立ち止まって狙撃・貫通", "站定狙击・贯穿");
        PA("저격 자세", "Sniper Stance", "狙撃の構え", "狙击架势");
        PK("멈춰 있으면 0.6초마다 조준이 모여 다음 탄이 저격탄이 됩니다. (모든 적 관통 · 피해 ×2.2 · 탄속 ×1.6)",
           "멈춰 있으면 0.6초마다 다음 탄이 저격탄이 됩니다. (모든 적 관통 · 피해 ×2.2 · 탄속 ×1.6)",
           "Standing still turns your next bullet into a sniper round every 0.6s. (Pierces all · ×2.2 damage · ×1.6 speed)",
           "立ち止まっていると0.6秒ごとに次の弾が狙撃弾になります。(すべての敵を貫通・ダメージ×2.2・弾速×1.6)",
           "站着不动时每0.6秒下一发子弹变为狙击弹。(贯穿所有敌人・伤害×2.2・弹速×1.6)");
        PK("움직이며 쏜 탄의 피해가 25% 줄고, 탄창이 8발에서 6발로 줄어듭니다.",
           "움직이며 쏜 탄의 피해가 25% 줄고, 탄창이 8발 → 6발이 됩니다.",
           "Bullets fired on the move deal 25% less. Magazine 8 → 6.",
           "移動中に撃った弾のダメージが25%減り、弾倉が8発 → 6発になります。",
           "移动中射出的子弹伤害降低25%，弹匣8发 → 6发。");
        PA("보스 · 일렬", "Bosses · Lines", "ボス・一列の敵", "首领・成排敌人");
        PA("기동 · 포위", "Mobility · Being surrounded", "機動・包囲", "机动・被包围");
        // 1.5.9~ 실제로는 보스 · 중간 보스의 빈틈(WeakSpot)에 맞을 때만 걸림 — 「약점」(약점 간파 · 입문 카드 약점 줄)과 헷갈리지 않게 빈틈 용어로 (키는 그대로)
        PK("저격탄이 약점에 맞으면 피해 ×1.5와 함께 작은 폭발이 일어납니다.",
           "저격탄이 빈틈에 맞으면 피해 ×1.5와 함께 작은 폭발이 일어납니다.",
           "Sniper rounds that hit a weak point deal ×1.5 damage and set off a small explosion.",
           "狙撃弾が隙に当たると、ダメージ×1.5とともに小さな爆発が起きます。",
           "狙击弹命中破绽时伤害×1.5，并引发小型爆炸。");
        PA("저격탄이 꿰뚫은 적 수만큼 다음 조준이 빨라집니다. (5마리면 즉시)",
           "Each enemy a sniper round pierces speeds up your next aim. (5 enemies = instant)",
           "狙撃弾が貫いた敵の数だけ、次の照準が速くなります。(5体で即座に)",
           "狙击弹每贯穿一个敌人，下一次瞄准就更快。(5个即刻完成)");
        PA("일제 사격(우클릭)이 모두 저격탄이 됩니다.",
           "Every shot of your Volley (right-click) becomes a sniper round.",
           "一斉射撃(右クリック)の弾がすべて狙撃弾になります。",
           "齐射(右键)的子弹全部变为狙击弹。");

        // ================================================================= 도탄 난사
        PA("도탄 난사", "Ricochet Spray", "跳弾乱射", "跳弹扫射");
        PA("큰 탄창 · 튕기는 난사", "Big magazine · Bouncing spray", "大きな弾倉・跳ねる乱射", "大弹匣・弹跳扫射");
        PA("난사", "Spray", "乱射", "扫射");
        PA("탄창이 14발로 늘고 연사가 1.35배 빨라지며, 모든 탄이 한 번 튕깁니다. (퍼짐 ±8°)",
           "Your magazine grows to 14 rounds, you fire 1.35× faster, and every bullet ricochets once. (Spread ±8°)",
           "弾倉が14発に増え、連射が1.35倍速くなり、すべての弾が一度跳ねます。(拡散±8°)",
           "弹匣增至14发，射速提高到1.35倍，所有子弹弹跳一次。(散布±8°)");
        PA("한 발의 피해가 30% 줄어듭니다.", "Each bullet deals 30% less damage.", "1発のダメージが30%減ります。", "每发子弹伤害降低30%。");
        PA("무리", "Crowds", "群れ", "成群敌人");
        PA("보스 단일 · 탄 관리", "Single bosses · Ammo upkeep", "ボス単体・弾の管理", "单体首领・弹药管理");
        PA("튕길 때마다 다음 튕김의 피해가 15% 늘어납니다. (최대 3회)",
           "Each ricochet makes the next one deal 15% more damage. (Up to 3 times)",
           "跳ねるたびに次の跳弾のダメージが15%増えます。(最大3回)",
           "每次弹跳，下一次弹跳伤害提高15%。(最多3次)");
        PA("투기장 경계에 닿은 탄도 되튕기고, 사라질 때 파편 두 개가 튑니다.",
           "Bullets also bounce off walls and arena edges, and burst into two fragments when they're spent.",
           "壁や闘技場の端に当たった弾も跳ね返り、消えるときに破片が2つ飛び散ります。",
           "子弹碰到墙壁和场地边缘也会反弹，消失时迸出两块碎片。");
        PA("튕김 횟수가 무제한이 됩니다. (튕길 때마다 피해 −10%)",
           "Ricochets become unlimited. (−10% damage per bounce)",
           "跳弾の回数が無制限になります。(跳ねるたびにダメージ−10%)",
           "弹跳次数变为无限。(每次弹跳伤害−10%)");

        // ================================================================= 화염 과열
        PA("화염 과열", "Flame Overheat", "火炎過熱", "烈焰过热");
        PK("장전 대신 열 게이지 · 과열 폭발", "열 게이지 · 과열 폭발", "Heat gauge · Overheat blast", "熱ゲージ・過熱爆発", "热量槽・过热爆炸");
        PA("과열 사격", "Overheat Fire", "過熱射撃", "过热射击");
        PK("장전 대신 열 게이지가 찹니다. 열 50% 위에서는 모든 탄이 화상을 입히고, 100%가 되면 2초 동안 과열되며 몸 둘레에 화염 폭발이 일어납니다. (공격력 250%)",
           "장전 대신 열이 찹니다. 50% 위에서는 탄이 화상을 입히고, 100%면 2초 동안 과열되며 둘레가 폭발합니다. (공격력 250%)",
           "You build heat instead of reloading. Above 50% heat, bullets burn; at 100% you overheat for 2s and blast your surroundings. (250% Attack)",
           "装填の代わりに熱が溜まります。熱50%以上ではすべての弾が火傷を与え、100%で2秒間過熱して周りが爆発します。(攻撃力250%)",
           "不再装填，改为积累热量。热量超过50%时所有子弹附带灼烧，达到100%时过热2秒，周围爆炸。(攻击力250%)");
        PK("과열된 2초 동안 쏠 수 없고, 직접 피해가 15% 줄어듭니다.",
           "과열 중엔 쏠 수 없고, 직접 피해가 15% 줄어듭니다.",
           "You can't fire while overheated. Direct damage -15%.",
           "過熱中は撃てず、直接ダメージが15%減ります。",
           "过热期间无法射击，直接伤害降低15%。");
        PK("지속 광역 · 보스 화상", "지속 광역 · 보스 화상", "Area over time · Boss burns", "持続範囲攻撃・ボスへの火傷", "持续范围伤害・灼烧首领");
        PA("과열 순간", "The overheat window", "過熱の瞬間", "过热的瞬间");
        PA("화상이 3중첩까지 쌓입니다.", "Burns stack up to 3 times.", "火傷が3重まで重なります。", "灼烧最多叠加3层。");
        PA("과열 폭발 자리에 4초 동안 불 장판이 남고, 열 90~100%에서 쏜 탄은 피해 ×1.5입니다.",
           "Overheat blasts leave a 4s fire field, and bullets fired at 90–100% heat deal ×1.5 damage.",
           "過熱爆発の跡に4秒間炎の床が残り、熱90~100%で撃った弾はダメージ×1.5になります。",
           "过热爆炸处会留下持续4秒的火焰地带，热量90~100%时射出的子弹伤害×1.5。");
        PA("과열 시간이 0.8초로 줄고 폭발이 두 번 일어납니다.",
           "Overheat lasts only 0.8s, and the blast goes off twice.",
           "過熱時間が0.8秒に縮み、爆発が2回起きます。",
           "过热时间缩短为0.8秒，爆炸发生两次。");

        // ================================================================= 산탄 돌격
        PA("산탄 돌격", "Shotgun Rush", "散弾突撃", "霰弹突击");
        PA("탄창 4 산탄 · 장전 돌진", "4-shell shotgun · Reload dash", "4発の散弾・装填ダッシュ", "4发霰弹・装填冲刺");
        PA("산탄 장전", "Buckshot Load", "散弾装填", "霰弹装填");
        PK("평타가 6발 산탄이 됩니다. (사거리 6칸 · 40° · 가까울수록 셈) 탄창은 4발, 장전은 40% 빠르고, 장전을 시작하면 이동 방향으로 2.5칸 돌진합니다. (0.15초 무적)",
           "평타가 6발 산탄이 됩니다. (40° · 가까울수록 셈) 탄창은 4발, 장전은 40% 빠르고, 장전을 시작하면 2.5칸 돌진합니다. (0.15초 무적)",
           "Basic attacks fire 6 pellets. (40° · stronger up close) 4-shell magazine, 40% faster reload, and starting a reload dashes you 2.5 tiles. (0.15s invincible)",
           "通常攻撃が6発の散弾になります。(40°・近いほど強力) 弾倉は4発、装填は40%速く、装填を始めると2.5マス突進します。(0.15秒無敵)",
           "普通攻击变为6发霰弹。(40°・越近越强) 弹匣4发，装填快40%，开始装填时冲刺2.5格。(0.15秒无敌)");
        PA("6칸 밖의 적은 거의 맞히지 못합니다.", "You can barely hit enemies beyond 6 tiles.", "6マスより遠い敵にはほとんど当たりません。", "几乎打不中6格以外的敌人。");
        PK("근접 무리 · 생존", "근접 무리 · 생존", "Close crowds · Survival", "近接の群れ・生存", "近身群敌・生存");
        PK("원거리 · 탄막 보스", "원거리 · 탄막 보스", "Range · Bullet-hell bosses", "遠距離・弾幕ボス", "远程・弹幕首领");
        PA("돌진에 부딪힌 적을 밀치고 다음 산탄의 피해가 40% 늘어납니다.",
           "Your dash shoves enemies aside, and your next shotgun blast deals 40% more damage.",
           "突進でぶつかった敵を押しのけ、次の散弾のダメージが40%増えます。",
           "冲刺会撞开敌人，下一发霰弹伤害提高40%。");
        PA("탄창의 마지막 발이 일직선으로 꿰뚫는 슬러그가 됩니다. (공격력 300%)",
           "The last shell in your magazine becomes a slug that pierces in a straight line. (300% of Attack)",
           "弾倉の最後の1発が一直線に貫くスラッグ弾になります。(攻撃力300%)",
           "弹匣的最后一发变为直线贯穿的独头弹。(攻击力300%)");
        PA("돌진을 두 번 연달아 하고, 끝에 충격파가 터집니다.",
           "You dash twice in a row, ending with a shockwave.",
           "突進を2回連続で行い、最後に衝撃波が炸裂します。",
           "连续冲刺两次，结束时爆发冲击波。");

        // ================================================================= 포탑 설치
        PA("포탑 설치", "Turret Deploy", "砲台設置", "架设炮塔");
        PK("장전할 때 포탑 · 진지 방어", "장전 포탑 · 진지 방어", "Reload turrets · Hold the line", "装填で砲台・陣地防衛", "装填架炮塔・阵地防守");
        PA("거치 포탑", "Mounted Turret", "据え置き砲台", "固定炮塔");
        PK("장전을 마칠 때마다 발밑에 포탑이 서서 0.4초마다 가까운 적을 쏩니다. 사거리 안에 보스가 있으면 보스를 먼저 쏩니다. (공격력 20% · 최대 2개 · 12초)", "장전을 마칠 때마다 포탑이 서서 0.4초마다 가까운 적을 쏩니다. (보스 우선 · 공격력 20% · 최대 2개 · 12초)", "Each finished reload deploys a turret that shoots a nearby enemy every 0.4s. (Bosses first · 20% Attack · up to 2 · 12s)", "装填を終えるたびに砲台が立ち、0.4秒ごとに近くの敵を撃ちます。(ボス優先・攻撃力20%・最大2基・12秒)", "每次装填完毕架起炮塔，每0.4秒射击附近的敌人。(优先首领・攻击力20%・最多2座・12秒)");
        PA("내 공격 속도가 20% 느려집니다.", "Your attack speed is 20% slower.", "自分の攻撃速度が20%遅くなります。", "自身攻击速度降低20%。");
        PK("진지 방어 · 서 있는 보스", "진지 방어 · 서 있는 보스", "Holding ground · Static bosses", "陣地防衛・動かないボス", "阵地防守・不移动的首领");
        PK("이동 강제 (침식 · 결계)", "침식 · 결계", "Erosion · Barriers", "侵食・結界", "侵蚀・结界");
        PA("포탑이 3개까지 서고, 포탑 4칸 안에 서 있으면 장전이 2배 빨라집니다.",
           "Up to 3 turrets can stand, and you reload twice as fast within 4 tiles of a turret.",
           "砲台が3基まで立ち、砲台から4マス以内にいると装填が2倍速くなります。",
           "炮塔最多可架设3座，站在炮塔4格内时装填速度翻倍。");
        PA("포탑이 0.8초마다 적 투사체 하나를 쏴서 지우고, 사라질 때 폭발합니다.",
           "Turrets shoot down one enemy projectile every 0.8s and explode when they expire.",
           "砲台が0.8秒ごとに敵の弾を1つ撃ち落とし、消えるときに爆発します。",
           "炮塔每0.8秒击落一枚敌方弹幕，消失时会爆炸。");
        PA("포탑이 사라지지 않고 4개까지 서며, 탄피 지뢰를 깝니다.",
           "Turrets never expire, up to 4 can stand, and they lay Shell Mines.",
           "砲台が消えなくなり、4基まで立ち、薬莢地雷をまきます。",
           "炮塔不再消失，最多可架设4座，并会布下弹壳地雷。");

        // ================================================================= 새 교차
        PA("철갑 소이탄", "Armor-Piercing Incendiary", "徹甲焼夷弾", "穿甲燃烧弹");
        PA("저격탄이 지나간 줄 전체에 화상이 3중첩으로 붙습니다.",
           "Sniper rounds apply 3 stacks of burn to every enemy along their path.",
           "狙撃弾が通った列の敵すべてに、火傷が3重で付きます。",
           "狙击弹穿过的整条路线上的敌人都会附加3层灼烧。");
        PA("화염 돌진", "Flame Rush", "火炎突進", "烈焰冲刺");
        PA("과열 폭발이 돌진 끝에서 터지고, 과열 중에도 돌진할 수 있습니다.",
           "Overheat blasts erupt at the end of your dash, and you can dash even while overheated.",
           "過熱爆発が突進の終点で炸裂し、過熱中でも突進できます。",
           "过热爆炸会在冲刺终点爆发，过热期间也能冲刺。");

        // ================================================================= 효과가 띄우는 글 · 판 기록 출처 이름 (dealt_by · 결과 화면)
        PA("저격", "Ready", "狙撃", "狙击");
        PA("저격탄", "Sniper round", "狙撃弾", "狙击弹");
        PA("도탄 파편", "Ricochet fragments", "跳弾の破片", "跳弹碎片");
        PA("과열 폭발", "Overheat blast", "過熱爆発", "过热爆炸");
        PA("포탑", "Turret", "砲台", "炮塔");
    }
}
