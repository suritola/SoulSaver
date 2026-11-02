// 번역표: 공사 반장 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   + 31번 새 카드 「작업 지시」 이름 · 설명 · 수치 이름 + 갈래 효과의 피해 출처 이름(결과 화면 · 판 기록) + 머리 위 글 「한 번 더!」
//   (Paths/Paths.Foreman.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
// 용어: 더미 pile · 山 · 堆 / 덤프 dump · ダンプ · 倾倒 / 빠각 crunch · ガツン · 嘎嘣 / 바리케이드 barricade · バリケード · 路障 / 시멘트 cement · セメント · 水泥 / 쇠공 wrecking ball · 鉄球 · 铁球
public static partial class LocTable
{
    static void AddPathsForeman()
    {
        // ---------------- 불도저
        PA("불도저", "Bulldozer", "ブルドーザー", "推土机");
        PA("앞으로 밀어붙임 · 큰 더미로 짓누름", "Push straight ahead · Crush with a huge pile", "前へ押し進む · 大きな山で押し潰す", "向前猛推 · 用大堆碾压");
        PA("밀 때 앞으로는 느려지지 않고(옆 · 뒤로 느려지는 것도 절반), 더미 상한이 4 늘어납니다.",
           "You don't slow down when pushing forward (slowdown to the sides and back is halved), and the pile cap rises by 4.",
           "押すとき前方では遅くならず（横・後ろで遅くなるのも半分）、山の上限が4増えます。",
           "向前推时不会减速（侧向和后退的减速也减半），堆的上限增加4。");
        PA("덤프 거리가 40% 줄어듭니다.", "The dump distance is 40% shorter.", "ダンプの距離が40%短くなります。", "倾倒距离缩短40%。");
        PA("무리 · 생존", "Crowds · Survival", "群れ · 生存", "群敌 · 生存");
        PA("넓게 퍼진 적 · 원거리", "Spread-out foes · Ranged foes", "広く散った敵 · 遠距離", "分散的敌人 · 远程");
        PA("더미 무게가 10 이상이면 앞의 적을 흡수하지 않고 3칸 밀쳐 길을 냅니다. (공격력 40%)",
           "At pile weight 10 or more, foes ahead are shoved 3 tiles to clear a path instead of being absorbed. (40% Attack)",
           "山の重さが10以上だと、前の敵を取り込まず3マス押しのけて道を作ります。（攻撃力40%）",
           "堆的重量达到10以上时，前方的敌人不再被吸入，而是被推开3格开出一条路。（攻击力40%）");
        PA("벽 · 바리케이드 · 보스에 대고 1초 동안 누르면 완전 압착이 일어납니다. 더미 안 모두 공격력 250%를 한 번 받고, 상대가 보스면 무게 × 공격력 25%를 더 받습니다. (무게 상한 16 · 2초에 한 번)",
           "Pressing against a wall, barricade or boss for 1s triggers a full crush: everything in the pile takes 250% Attack once, and a boss also takes weight × 25% Attack more. (Weight capped at 16 · once per 2s)",
           "壁・バリケード・ボスに1秒押し当てると完全圧着が起きます。山の中の全員が攻撃力250%を1回受け、相手がボスなら重さ × 攻撃力25%を追加で受けます。（重さ上限16・2秒に1回）",
           "顶着墙壁、路障或首领按住1秒会触发完全挤压：堆内所有敌人受到一次攻击力250%的伤害，对象是首领时再受到重量 × 攻击力25%的伤害。（重量上限16 · 每2秒一次）");
        PA("더미 상한이 30이 되고, 완전 압착이 0.5초 만에 일어납니다.", "The pile cap becomes 30, and the full crush triggers after just 0.5s.", "山の上限が30になり、完全圧着が0.5秒で起きます。", "堆的上限变为30，完全挤压只需0.5秒触发。");

        // ---------------- 산사태
        PA("산사태", "Landslide", "地すべり", "山体滑坡");
        PA("길고 좁은 흙 파도 · 무리 폭발", "A long, narrow wave of earth · Crowd burst", "細長い土の波 · 群れの爆発", "又长又窄的土浪 · 群体爆发");
        PA("덤프가 길고 좁은 흙 파도(길이 1.6배 · 폭 0.7배)가 되고, 지나간 적이 0.5초 묻히며, 덤프 피해가 40% 늘어납니다.",
           "The dump becomes a long, narrow wave of earth (1.6x length · 0.7x width), foes it passes are buried for 0.5s, and dump damage rises by 40%.",
           "ダンプが細長い土の波（長さ1.6倍・幅0.7倍）になり、通り過ぎた敵が0.5秒埋まり、ダンプのダメージが40%増えます。",
           "倾倒变成又长又窄的土浪（长度1.6倍 · 宽度0.7倍），经过的敌人被掩埋0.5秒，倾倒伤害提高40%。");
        PA("압착 피해가 35% 줄어듭니다.", "Crush damage is 35% lower.", "圧着ダメージが35%減ります。", "挤压伤害降低35%。");
        PA("무리 폭발 · 통로 정리", "Crowd burst · Clearing lanes", "群れの爆発 · 通路の掃除", "群体爆发 · 清理通道");
        PA("덤프 사이 공백", "Gaps between dumps", "ダンプの合間の空白", "两次倾倒之间的空档");
        PA("덤프가 닿은 자리에 흙더미가 2초 동안 남아 밟은 적이 40% 느려집니다.",
           "A mound of earth stays where the dump lands for 2s, slowing foes that step on it by 40%.",
           "ダンプが届いた場所に土の山が2秒残り、踏んだ敵が40%遅くなります。",
           "倾倒落点会留下土堆2秒，踩上去的敌人减速40%。");
        PA("무게 12 이상으로 덤프하면 1칸 옆으로 두 번째 파도가 따라옵니다. (60%) 보스가 파도에 맞으면 무게 × 공격력 15%를 더 받습니다. (무게 상한 16)",
           "Dumping at weight 12 or more sends a second wave 1 tile to the side. (60%) A boss hit by the wave takes weight × 15% Attack more. (Weight capped at 16)",
           "重さ12以上でダンプすると、1マス横に2つ目の波が付いてきます。（60%）ボスが波に当たると重さ × 攻撃力15%を追加で受けます。（重さ上限16）",
           "以12以上的重量倾倒时，旁边1格会跟着出现第二道浪。（60%）首领被浪击中时再受到重量 × 攻击力15%的伤害。（重量上限16）");
        PA("덤프 0.5초 뒤 남은 흙이 한 번 더 굴러갑니다. (40%)", "0.5s after a dump, the leftover earth rolls through once more. (40%)", "ダンプの0.5秒後、残った土がもう一度転がります。（40%）", "倾倒0.5秒后，残余的土会再滚过一次。（40%）");

        // ---------------- 콘크리트
        PA("콘크리트", "Concrete", "コンクリート", "混凝土");
        PA("적을 굳혀 오래 붙잡음 · 지속 피해", "Set foes in place for long · Lasting damage", "敵を固めて長く捕らえる · 継続ダメージ", "把敌人浇固 · 持续伤害");
        PA("레미콘", "Ready-Mix", "生コン", "预拌混凝土");
        PA("짓누를 때마다 더미 안 적에게 시멘트가 쌓여(최대 3), 다 굳은 적은 압착 피해를 50% 더 받고 쓰러지면 잔해(5초)로 남습니다.",
           "Each crush stacks cement on foes in the pile (up to 3). Fully set foes take 50% more crush damage and leave rubble (5s) when they fall.",
           "押し潰すたびに山の中の敵にセメントが積もり（最大3）、固まった敵は圧着ダメージが50%増え、倒れると瓦礫（5秒）になります。",
           "每次挤压都会给堆内敌人叠加水泥（最多3层），完全凝固的敌人受到的挤压伤害提高50%，倒下后会留下碎石（5秒）。");
        PA("이동 속도가 10% 줄고, 덤프 거리가 50% 줄어듭니다.", "Movement speed is 10% lower, and the dump distance is 50% shorter.", "移動速度が10%下がり、ダンプの距離が50%短くなります。", "移动速度降低10%，倾倒距离缩短50%。");
        PA("오래 붙잡는 지속 피해 · 경제", "Lasting damage · Economy", "長く捕らえる継続ダメージ · 経済", "持续伤害 · 经济");
        PA("기동 · 빠른 적", "Mobility · Fast foes", "機動 · 素早い敵", "机动 · 快速敌人");
        PA("굳은 적에 닿아 있는 더미 속 적도 짓누를 때마다 시멘트가 하나 더 쌓입니다.",
           "Foes in the pile touching a set foe also gain one extra cement per crush.",
           "固まった敵に接している山の中の敵も、押し潰すたびにセメントがもう1つ積もります。",
           "与已凝固敌人相贴的堆内敌人，每次挤压也会多叠加1层水泥。");
        PA("더미 안에서 쓰러진 적이 터지며 둘레 1.5칸에 공격력 60%와 시멘트 1을 줍니다. (한 번 연쇄) 보스는 들이받기 · 빠각마다 시멘트 1이 쌓여, 3이 되면 4초 동안 압착 · 빠각 · 들이받기 피해가 25% 늘어납니다.",
           "A foe that falls inside the pile bursts, dealing 60% Attack and 1 cement within 1.5 tiles. (Chains once) A boss gains 1 cement per bash or crunch; at 3, crush, crunch and bash damage rises by 25% for 4s.",
           "山の中で倒れた敵が弾け、周囲1.5マスに攻撃力60%とセメント1を与えます。（1回だけ連鎖）ボスは体当たり・ガツンのたびにセメント1が積もり、3になると4秒間、圧着・ガツン・体当たりのダメージが25%増えます。",
           "堆内倒下的敌人会爆开，对周围1.5格造成攻击力60%的伤害并附加1层水泥。（连锁一次）首领每次被顶撞或嘎嘣都会叠加1层水泥，达到3层时，4秒内挤压、嘎嘣、顶撞伤害提高25%。");
        PA("시멘트가 2만 쌓여도 굳습니다.", "Foes set with just 2 cement.", "セメントが2つ積もるだけで固まります。", "只需2层水泥就会凝固。");

        // ---------------- 철거
        PA("철거", "Demolition", "解体", "拆除");
        PA("쇠공 길 만들기 · 필살기 폭딜", "Lay down a wrecking-ball road · Ultimate burst", "鉄球の道づくり · 必殺技の爆発力", "铺铁球之路 · 必杀爆发");
        PA("철거 반장", "Demolition Foreman", "解体班長", "拆除工头");
        PA("쇠공이 튕기는 횟수가 3에서 4로 늘고, 바리케이드가 2초 더 서 있으며, 필살기 게이지가 15% 덜 듭니다.",
           "The wrecking ball bounces 4 times instead of 3, barricades stand 2s longer, and the Ultimate gauge costs 15% less.",
           "鉄球が跳ねる回数が3回から4回に増え、バリケードが2秒長く立ち、必殺技ゲージの消費が15%減ります。",
           "铁球弹跳次数从3次增加到4次，路障多存在2秒，必杀槽消耗减少15%。");
        PA("압착 피해가 20% 줄어듭니다.", "Crush damage is 20% lower.", "圧着ダメージが20%減ります。", "挤压伤害降低20%。");
        PA("지형 만들기 · 필살기 폭딜", "Shaping terrain · Ultimate burst", "地形づくり · 必殺技の爆発力", "塑造地形 · 必杀爆发");
        PA("평타 공백", "Basic-attack gaps", "通常攻撃の空白", "普通攻击的空档");
        PA("덤프한 더미가 바리케이드에 부딪히면 빠각 피해가 두 배가 됩니다.",
           "When a dumped pile slams into a barricade, the crunch damage doubles.",
           "ダンプした山がバリケードにぶつかると、ガツンのダメージが2倍になります。",
           "倾倒的堆撞上路障时，嘎嘣伤害翻倍。");
        PA("바리케이드가 사라질 때 앞쪽으로 작은 쇠공이 4칸 굴러갑니다. (공격력 120%) 쇠공이 보스에 맞으면 그 자리에서 한 번 더 튕깁니다. (필살기마다 최대 2번)",
           "When a barricade disappears, a small wrecking ball rolls 4 tiles forward. (120% Attack) A ball that hits a boss bounces once more on the spot. (Up to 2 per Ultimate)",
           "バリケードが消えるとき、前方へ小さな鉄球が4マス転がります。（攻撃力120%）鉄球がボスに当たると、その場でもう一度跳ねます。（必殺技ごとに最大2回）",
           "路障消失时，前方会滚出一颗小铁球，滚动4格。（攻击力120%）铁球击中首领时会在原地再弹一次。（每次必杀最多2次）");
        PA("쇠공이 두 개 떨어집니다. (두 번째는 15° 옆 · 피해 70%)", "Two wrecking balls fall. (The second lands 15° to the side · 70% damage)", "鉄球が2つ落ちます。（2つ目は15°横・ダメージ70%）", "会落下两颗铁球。（第二颗偏15° · 伤害70%）");

        // ---------------- 안전 제일
        PA("안전 제일", "Safety First", "安全第一", "安全第一");
        PA("탄을 막는 더미 · 탄막 보스", "A pile that blocks shots · Bullet-hell bosses", "弾を防ぐ山 · 弾幕ボス", "挡弹的堆 · 弹幕首领");
        PA("더미가 무게와 상관없이 앞쪽 적 투사체를 막고, 밀고 있는 동안 받는 피해가 15% 줄어듭니다.",
           "The pile blocks enemy projectiles from the front regardless of weight, and you take 15% less damage while pushing.",
           "山が重さに関係なく前方の敵の飛び道具を防ぎ、押している間に受けるダメージが15%減ります。",
           "无论重量多少，堆都会挡住前方的敌方投射物，推动期间受到的伤害降低15%。");
        PA("압착에 쓰는 무게 상한이 12에서 8로 줄어듭니다.", "The weight cap used for crushing drops from 12 to 8.", "圧着に使う重さの上限が12から8に下がります。", "用于挤压的重量上限从12降到8。");
        PA("생존 · 탄막 보스", "Survival · Bullet-hell bosses", "生存 · 弾幕ボス", "生存 · 弹幕首领");
        PA("무리 화력", "Crowd firepower", "群れへの火力", "群体火力");
        PA("더미가 막은 투사체마다 3초 동안 압착 피해가 5% 늘어납니다. (최대 50%)",
           "Each projectile the pile blocks raises crush damage by 5% for 3s. (Up to 50%)",
           "山が防いだ飛び道具ごとに、3秒間圧着ダメージが5%増えます。（最大50%）",
           "堆每挡下一枚投射物，3秒内挤压伤害提高5%。（最多50%）");
        PA("맞으면(1초에 한 번) 더미가 0.6초 동안 반장 둘레를 감싸 사방을 막습니다. 보스 탄을 막을 때마다 다음 들이받기 · 빠각 피해가 10% 늘어납니다. (최대 60%)",
           "When you're hit (once per 1s), the pile wraps around you for 0.6s and blocks every direction. Each boss projectile blocked raises the next bash or crunch damage by 10%. (Up to 60%)",
           "攻撃を受けると（1秒に1回）、山が0.6秒間班長の周りを囲んで四方を防ぎます。ボスの弾を防ぐたびに、次の体当たり・ガツンのダメージが10%増えます。（最大60%）",
           "受到攻击时（每1秒一次），堆会环绕工头0.6秒挡住四面八方。每挡下一枚首领的弹幕，下一次顶撞或嘎嘣的伤害提高10%。（最多60%）");
        PA("막은 투사체를 되튕깁니다. (공격력 60%)", "Blocked projectiles are sent back. (60% Attack)", "防いだ飛び道具を撥ね返します。（攻撃力60%）", "挡下的投射物会被弹回。（攻击力60%）");

        // ---------------- 새 교차 둘
        PA("콘크리트 벽", "Concrete Wall", "コンクリートの壁", "混凝土墙");
        PA("상한까지 찬 더미를 덤프하면 쏟는 대신 그 자리에 3초 동안 벽으로 굳습니다. 적을 막고, 벽 압착 · 빠각의 상대가 됩니다. (동시 2개)",
           "Dumping a pile that has reached the cap sets it into a wall for 3s on the spot instead of pouring it out. It blocks foes and serves as a target for wall crushes and crunches. (Up to 2 at once)",
           "上限まで溜まった山をダンプすると、流す代わりにその場で3秒間壁として固まります。敵を防ぎ、壁圧着・ガツンの相手になります。（同時に2つまで）",
           "对已满上限的堆进行倾倒时，不再倾泻，而是在原地凝固成墙3秒。它能挡住敌人，也能成为靠墙挤压、嘎嘣的对象。（同时最多2个）");
        PA("낙석 주의", "Falling Rocks", "落石注意", "小心落石");
        PA("산사태 파도가 바리케이드에 닿으면 바리케이드가 무너지며 그 뒤로 산사태가 한 번 더 일어납니다. (60% · 바리케이드마다 한 번)",
           "When a landslide wave reaches a barricade, the barricade collapses and another landslide follows behind it. (60% · once per barricade)",
           "地すべりの波がバリケードに届くと、バリケードが崩れてその後ろでもう一度地すべりが起きます。（60%・バリケードごとに1回）",
           "山体滑坡的浪头碰到路障时，路障会倒塌，其后方再发生一次山体滑坡。（60% · 每个路障一次）");

        // ---------------- 31번 새 카드
        PA("작업 지시", "Work Order", "作業指示", "作业指令");
        PA("덤프한 직후 0.8초 안에 다시 누르면 「한 번 더!」 — 흩어진 더미를 삽날 앞으로 다시 끌어모아 두 번째 덤프를 합니다.",
           "Press again within 0.8s of a dump for \"One more!\" — the scattered pile is gathered back in front of the blade for a second dump.",
           "ダンプの直後0.8秒以内にもう一度押すと「もう一回！」 — 散らばった山をブレードの前へ集め直して、2回目のダンプをします。",
           "倾倒后0.8秒内再按一次即可「再来一次！」——把散开的堆重新聚到铲刃前方，进行第二次倾倒。");
        PA("두 번째 덤프 피해", "Second dump damage", "2回目のダンプのダメージ", "第二次倾倒伤害");
        PA("한 번 더!", "One more!", "もう一回！", "再来一次！");

        // ---------------- 피해 출처 이름 (결과 화면 · 판 기록)
        PA("완전 압착", "Full Crush", "完全圧着", "完全挤压");
        PA("산사태", "Landslide", "地すべり", "山体滑坡");
        PA("시멘트 폭발", "Cement Burst", "セメント爆発", "水泥爆裂");
        PA("굴러가는 쇠공", "Rolling Ball", "転がる鉄球", "滚动铁球");
        PA("되튕긴 탄", "Sent-back Shot", "撥ね返した弾", "弹回的弹幕");
        PA("콘크리트 벽", "Concrete Wall", "コンクリートの壁", "混凝土墙");
        PA("낙석", "Falling Rocks", "落石", "落石");
    }
}
