// 번역표: 지도 제작자 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   (Paths/Paths.Cartographer.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
// 「정밀 측량」(갈래 이름 = 입문 카드 이름) · 「북극성」 · 「잉크 폭풍」은 이미 LocTable.Heroes 에 있음
public static partial class LocTable
{
    static void AddPathsCartographer()
    {
        // ================================================================= 0 정밀 측량
        PA("정밀 측량", "Precise Survey", "精密測量", "精密测量");
        PA("네 획 사각형 · 무리 일괄", "Four-stroke squares · Crowd sweeps", "四画の四角形・群れを一掃", "四笔方形 · 成群清扫");
        PA("도형이 네 획마다 사각형으로 닫힙니다. (넓이 ×1.8 · 피해 ×1.4)",
           "Every fourth stroke closes a square instead of a triangle. (Area ×1.8 · Damage ×1.4)",
           "図形が四画ごとに四角形で閉じます。(広さ×1.8・ダメージ×1.4)",
           "每四笔闭合成方形。(面积 ×1.8 · 伤害 ×1.4)");
        PA("도형이 닫히는 주기가 느려집니다.", "Shapes close less often.", "図形が閉じる間隔が長くなります。", "图形闭合的间隔变长。");
        PA("무리 일괄 · 보스 가두기", "Crowd sweeps · Boxing in bosses", "群れを一掃・ボスを囲い込む", "成群清扫 · 围困首领");
        PA("주기", "Cycle time", "周期", "周期");
        PA("도형 안의 적에게 측량 표식이 저절로 붙습니다.",
           "Enemies inside a shape are automatically given Survey Marks.",
           "図形の中の敵に測量標識が自動で付きます。",
           "图形内的敌人会自动被打上测量标记。");
        PA("직전 도형과 겹친 곳의 적은 도형 피해가 2배입니다. (보스 제외)",
           "Enemies where a shape overlaps the previous one take double shape damage. (Not bosses)",
           "直前の図形と重なった場所の敵は図形ダメージが2倍になります。(ボスを除く)",
           "与上一个图形重叠处的敌人受到双倍图形伤害。(首领除外)");
        PA("도형이 오각형이 됩니다. (넓이 ×2.5 · 피해 ×1.6)",
           "Shapes become pentagons. (Area ×2.5 · Damage ×1.6)",
           "図形が五角形になります。(広さ×2.5・ダメージ×1.6)",
           "图形变为五边形。(面积 ×2.5 · 伤害 ×1.6)");

        // ================================================================= 1 일필
        PA("일필", "Single Stroke", "一筆", "一笔");
        PA("도형 대신 굵은 한 획", "One bold stroke instead of shapes", "図形の代わりに太い一画", "以粗重一笔代替图形");
        PA("일필휘지", "Bold Flourish", "一筆揮毫", "一笔挥就");
        PA("도형이 닫히지 않는 대신 모든 획의 길이 ×1.4 · 굵기 ×1.5 · 피해 ×1.7.",
           "Shapes no longer close, but every stroke gets Length ×1.4 · Width ×1.5 · Damage ×1.7.",
           "図形が閉じない代わりに、すべての画の長さ×1.4・太さ×1.5・ダメージ×1.7。",
           "图形不再闭合，但所有笔画长度 ×1.4 · 粗细 ×1.5 · 伤害 ×1.7。");
        PA("도형 효과가 없습니다.", "No shape effects.", "図形の効果がありません。", "没有图形效果。");
        PA("보스 · 일렬", "Bosses · Lines of enemies", "ボス・一列の敵", "首领 · 成列敌人");
        PA("포위", "Being surrounded", "包囲", "被包围");
        PA("여백 대기 시간이 3초에서 1.5초로 줄어듭니다.",
           "Margin's wait time drops from 3 seconds to 1.5 seconds.",
           "余白の待ち時間が3秒から1.5秒に短くなります。",
           "留白的等待时间从3秒缩短为1.5秒。");
        PA("획 끝에 먹이 튀며 작게 터집니다. (공격력 30%)",
           "Ink splatters at the end of each stroke in a small burst. (30% of Attack)",
           "画の終わりに墨が飛び散り、小さく弾けます。(攻撃力30%)",
           "笔画末端溅起墨汁，造成小范围爆炸。(攻击力30%)");
        PA("획이 되돌아오며 한 번 더 벱니다. (피해 50%)",
           "Each stroke sweeps back and cuts once more. (50% damage)",
           "画が引き返してもう一度斬ります。(ダメージ50%)",
           "笔画会折返再斩一次。(伤害50%)");

        // ================================================================= 2 먹물
        PA("먹물", "Ink Pools", "墨だまり", "墨池");
        PA("획이 잉크 웅덩이 · 지역", "Strokes leave ink pools · Area control", "画がインクだまりに・範囲制圧", "笔画化作墨池 · 区域控制");
        PA("먹물병 엎기", "Spilled Inkwell", "インク瓶をこぼす", "打翻墨水瓶");
        PA("모든 획이 4초 동안 잉크 웅덩이를 남깁니다. (초당 공격력 40% · 겹쳐도 한 번만)",
           "Every stroke leaves an ink pool for 4 seconds. (40% of Attack per second · Overlapping pools hit only once)",
           "すべての画が4秒間インクだまりを残します。(毎秒攻撃力40%・重なっても1回だけ)",
           "所有笔画都会留下持续4秒的墨池。(每秒攻击力40% · 重叠也只计算一次)");
        PA("획 피해가 15% 줄어듭니다.", "Stroke damage is reduced by 15%.", "画のダメージが15%減ります。", "笔画伤害降低15%。");
        PA("지역 무리", "Crowds in an area", "範囲の群れ", "区域内的成群敌人");
        PA("이동 보스", "Mobile bosses", "動き回るボス", "移动的首领");
        PA("웅덩이 위의 적이 40% 느려집니다.", "Enemies standing in pools are slowed by 40%.", "インクだまりの上の敵が40%遅くなります。", "墨池上的敌人减速40%。");
        PA("웅덩이 위에서 쓰러진 적에게서 잉크 폭풍이 일어납니다. (공격력 50%)",
           "Enemies that die in a pool burst into an Ink Storm. (50% of Attack)",
           "インクだまりの上で倒れた敵からインクの嵐が起こります。(攻撃力50%)",
           "在墨池上倒下的敌人会掀起墨之风暴。(攻击力50%)");
        PA("웅덩이가 8초 남습니다.", "Pools last 8 seconds.", "インクだまりが8秒残ります。", "墨池持续8秒。");

        // ================================================================= 3 접지술
        PA("접지술", "Art of Folding", "折図術", "折图术");
        PA("지도 접기 중심 · 필살기 폭딜", "Built around Fold the Map · Ultimate burst", "地図折りが主役・必殺技で大ダメージ", "以折叠地图为核心 · 必杀技爆发");
        PA("지도 접기 게이지가 60% 빨리 차고, 도형을 닫을 때마다 게이지가 5% 찹니다.",
           "The gauge for Fold the Map fills 60% faster, and closing a shape fills it by 5%.",
           "地図折りゲージが60%速くたまり、図形を閉じるたびにゲージが5%たまります。",
           "折叠地图的计量条充能速度加快60%，每闭合一个图形再充能5%。");
        PA("획 · 도형 피해가 25% 줄어듭니다.", "Stroke and shape damage is reduced by 25%.", "画・図形のダメージが25%減ります。", "笔画 · 图形伤害降低25%。");
        PA("필살기 폭딜 · 보스", "Ultimate burst · Bosses", "必殺技の大ダメージ・ボス", "必杀技爆发 · 首领");
        PA("주기 사이", "Between ultimates", "必殺技の合間", "必杀技间隙");
        PA("접힌 선이 3초 동안 남아 0.5초마다 벱니다. (공격력 20%)",
           "The fold line lingers for 3 seconds, cutting every 0.5 seconds. (20% of Attack)",
           "折り目が3秒間残り、0.5秒ごとに斬ります。(攻撃力20%)",
           "折线残留3秒，每0.5秒斩击一次。(攻击力20%)");
        PA("8초 안에 지도 접기를 두 번 쓰면 두 선의 교차점에 북극성이 뜹니다.",
           "Using Fold the Map twice within 8 seconds raises a North Star where the two lines cross.",
           "8秒以内に地図折りを2回使うと、2本の線の交点に北極星が昇ります。",
           "8秒内使用两次折叠地图，两条折线的交点会升起北极星。");
        PA("지도 접기를 2번 모아 둡니다.", "Fold the Map can store 2 charges.", "地図折りを2回分ためておけます。", "折叠地图可储存2次。");

        // ================================================================= 4 행군 측량
        PA("행군 측량", "Marching Survey", "行軍測量", "行军测量");
        PA("걸어간 길이 선 · 기동 생존", "Your path becomes a line · Mobility and survival", "歩いた道が線に・機動と生存", "走过的路化作线 · 机动生存");
        PA("걸어간 길이 잉크 선이 되어 닿는 적을 0.3초마다 벱니다. (공격력 25%) 지나온 길이 고리로 닫히면 안쪽을 도형처럼 짓이깁니다. (공격력 200% · 2초에 한 번)",
           "The path you walk becomes an ink line that cuts enemies touching it every 0.3 seconds. (25% of Attack) When your path closes into a loop, everything inside is crushed like a shape. (200% of Attack · once every 2 seconds)",
           "歩いた道がインクの線になり、触れた敵を0.3秒ごとに斬ります。(攻撃力25%) 通った道が輪になって閉じると、内側を図形のように押し潰します。(攻撃力200%・2秒に1回)",
           "走过的路会化作墨线，每0.3秒斩击接触的敌人。(攻击力25%) 走过的路连成闭环时，会像图形一样碾压环内敌人。(攻击力200% · 每2秒一次)");
        PA("평타 획 피해가 30% 줄어듭니다.", "Basic stroke damage is reduced by 30%.", "通常攻撃の画のダメージが30%減ります。", "普攻笔画伤害降低30%。");
        PA("기동 · 생존 · 무리", "Mobility · Survival · Crowds", "機動・生存・群れ", "机动 · 生存 · 成群敌人");
        PA("보스 (계속 걸어야 함)", "Bosses (you must keep walking)", "ボス (歩き続ける必要あり)", "首领 (必须一直走动)");
        PA("닫힌 고리 안의 적 투사체를 지웁니다.", "Closing a loop erases enemy projectiles inside it.", "閉じた輪の中の敵の弾を消します。", "清除闭环内的敌方弹幕。");
        PA("고리를 닫으면 1.5초 동안 이동 속도 +30% · 받는 피해 −30%.",
           "Closing a loop grants Move Speed +30% · Damage Taken −30% for 1.5 seconds.",
           "輪を閉じると1.5秒間、移動速度+30%・被ダメージ−30%。",
           "闭合闭环后1.5秒内，移动速度 +30% · 受到伤害 −30%。");
        PA("고리 피해가 2배가 됩니다.", "Loop damage is doubled.", "輪のダメージが2倍になります。", "闭环伤害翻倍。");

        // ================================================================= 새 교차
        PA("잉크 행군", "Ink March", "インク行軍", "墨迹行军");
        PA("걸어간 길이 잉크 웅덩이가 됩니다.", "The path you walk leaves ink pools.", "歩いた道がインクだまりになります。", "走过的路会留下墨池。");
        PA("대형 도면", "Grand Blueprint", "大型図面", "大型图纸");
        PA("여백 획 하나가 도형을 바로 닫습니다.", "A single Margin stroke closes a shape instantly.", "余白の一画が図形をすぐに閉じます。", "一笔留白即可立刻闭合图形。");

        // ================================================================= 피해 출처 (판 기록 dealt_by)
        PA("먹물 웅덩이", "Ink Pool", "インクだまり", "墨池");
        PA("먹 튀김", "Ink Splatter", "墨はね", "溅墨");
        PA("접힌 선", "Fold Line", "折り目", "折线");
        PA("행군 잉크 선", "Marching Ink Line", "行軍のインク線", "行军墨线");
        PA("행군 고리", "Marching Loop", "行軍の輪", "行军闭环");
    }
}
