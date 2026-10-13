// 번역표 (1.1.2): 새 캐릭터 (지도 제작자 · 시계공 · 우체부 · 유령) — 이름 · 설명 · 카드 · 고유 스킬 · 진화 · 무기 · 패시브 · 이야기
// LocTable.Skills 의 정적 생성자가 부름 (이미 있는 키는 건드리지 않음)
public static partial class LocTable
{
    static void AddHeroes()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }

        // ================================================================= 지도 제작자
        A("지도 제작자", "Cartographer", "地図職人", "制图师");
        A("길 잃은 측량사", "The Lost Surveyor", "迷子の測量士", "迷途的测量师");
        A("깃펜으로 세상을 그리는 측량사. 그은 선으로 적을 베고, 선을 이어 도형을 닫으면 안쪽을 통째로 짓이깁니다.",
          "A surveyor who draws the world with a quill. Strokes cut foes, and closing a shape crushes everything inside.",
          "羽ペンで世界を描く測量士。引いた線で敵を斬り、線をつないで図形を閉じると内側をまとめて押し潰します。",
          "用羽毛笔描绘世界的测量师。画出的线斩击敌人，连线闭合图形则将内部一并碾碎。");
        A("깃펜", "Quill", "羽ペン", "羽毛笔");
        A("마우스 쪽으로 잉크 획을 그어 닿은 적을 벱니다. 획은 앞 획의 끝에서 이어지고, 이어진 획으로 도형을 닫으면 안쪽의 적이 크게 다칩니다",
          "Draw an ink stroke toward the cursor, cutting enemies it touches. Each stroke continues from the last; close a shape and enemies inside take heavy damage",
          "マウスの方へインクの線を引き、触れた敵を斬ります。線は前の線の端から続き、つないだ線で図形を閉じると内側の敵に大ダメージ",
          "向鼠标方向画出墨线斩击触碰的敌人。每一笔从上一笔末端延续，连线闭合图形时内部敌人受到重创");
        A("지도 접기", "Fold the Map", "地図折り", "折叠地图");
        A("누르고 있으면 접히는 선이 보이고, 떼면 선 양쪽의 적을 선 위로 끌어당겨 짓이깁니다. 오래 누를수록 넓게 접힙니다",
          "Hold to show the fold line; release to drag enemies on both sides onto the line and crush them. Hold longer for a wider fold",
          "押している間は折り目が見え、離すと線の両側の敵を線の上へ引き寄せて押し潰します。長く押すほど広く折れます",
          "按住显示折线，松开后将折线两侧的敌人拉到线上碾碎。按得越久折得越宽");
        A("측량 완료!", "Surveyed!", "測量完了！", "测量完成！");
        A("깃펜 공격력", "Quill Damage", "羽ペン攻撃力", "羽毛笔攻击力");
        A("깃펜 공격력 · 이동 속도", "Quill Damage · Move Speed", "羽ペン攻撃力 · 移動速度", "羽毛笔攻击力 · 移动速度");
        A("무한 모드 클리어 (개발자 처치)", "Clear Endless (defeat the Developer)", "エンドレスをクリア（開発者を撃破）", "通关无尽模式（击败开发者）");

        // ---------------- 전용 레벨업 카드
        A("번지는 잉크", "Spreading Ink", "にじむインク", "晕染墨迹");
        A("획을 따라 잉크가 번져 2초 동안 밟은 적을 태웁니다.", "Ink spreads along each stroke, burning enemies that step in it for 2s.", "線に沿ってインクがにじみ、2秒間踏んだ敵を焼きます。", "墨迹沿笔画晕开，2秒内灼烧踩入的敌人。");
        A("잉크 피해 (초당)", "Ink damage (per sec)", "インクダメージ（毎秒）", "墨迹伤害（每秒）");
        A("굵은 펜촉", "Broad Nib", "太いペン先", "粗笔尖");
        A("획이 굵어져 더 넓게 벱니다.", "Strokes grow thicker and cut a wider path.", "線が太くなり、より広く斬ります。", "笔画变粗，斩击范围更宽。");
        A("획 굵기", "Stroke width", "線の太さ", "笔画粗细");
        A("나침반 바늘", "Compass Needle", "羅針盤の針", "罗盘指针");
        A("네 번째 획마다 나침반 바늘이 가까운 적에게 날아갑니다. (공격력 120%씩)", "Every fourth stroke launches compass needles at nearby enemies. (120% attack each)", "4本目の線ごとに羅針盤の針が近くの敵へ飛びます。（攻撃力120%ずつ）", "每画第四笔，罗盘指针飞向附近敌人。（每根攻击力120%）");
        A("바늘 수", "Needles", "針の数", "指针数");
        A("접힌 자국", "Crease", "折り目", "折痕");
        A("지도 접기 뒤 접힌 선에 자국이 남아 닿는 적을 0.3초마다 벱니다. (공격력 40%)", "After Fold the Map, the crease remains and cuts enemies on it every 0.3s. (40% attack)", "地図折りの後、折り目が残り触れた敵を0.3秒ごとに斬ります。（攻撃力40%）", "折叠地图后留下折痕，每0.3秒斩击其上的敌人。（攻击力40%）");
        A("자국 지속", "Crease duration", "折り目の持続", "折痕持续");
        A("측량 표식", "Survey Mark", "測量標識", "测量标记");
        A("획에 맞은 적에게 5초 동안 표식을 남깁니다. 표식이 붙은 적은 닫힌 도형 안에서 피해를 더 받습니다.", "Strokes mark enemies for 5s. Marked enemies take more damage inside closed shapes.", "線に当たった敵に5秒間標識を残します。標識の付いた敵は閉じた図形の中で多くダメージを受けます。", "被笔画击中的敌人留下5秒标记。被标记的敌人在闭合图形内受到更多伤害。");
        A("도형 피해", "Shape damage", "図形ダメージ", "图形伤害");
        A("끊기지 않는 선", "Unbroken Line", "途切れない線", "不断之线");
        A("획이 이어지는 시간이 늘어나고, 도형을 닫을 때마다 필살기 게이지가 찹니다.", "Strokes stay connected longer, and closing a shape fills your Ultimate gauge.", "線がつながる時間が延び、図形を閉じるたびに必殺技ゲージがたまります。", "笔画保持连接的时间更长，每次闭合图形都会充能必杀技。");
        A("게이지", "Gauge", "ゲージ", "充能");

        // ---------------- 고유 스킬
        A("범례", "Legend", "凡例", "图例");
        A("도형을 닫을 때마다 4초 동안 주는 모든 피해가 늘어납니다. (최대 3중첩)", "Each closed shape raises all damage you deal for 4s. (Up to 3 stacks)", "図形を閉じるたびに4秒間与えるすべてのダメージが増えます。（最大3重）", "每闭合一个图形，4秒内造成的所有伤害提升。（最多3层）");
        A("등고선", "Contour Lines", "等高線", "等高线");
        A("도형을 닫으면 둘레에서 고리 파동이 한 번 더 퍼집니다.", "Closing a shape sends a ring wave out from its edge.", "図形を閉じると周りから輪の波動がもう一度広がります。", "闭合图形时，从边缘再扩散一圈环形波动。");
        A("파동 피해", "Wave damage", "波動ダメージ", "波动伤害");
        A("축척", "Scale", "縮尺", "比例尺");
        A("획이 길수록 피해가 늘어납니다. (사거리 끝까지 그으면 최대)", "Longer strokes deal more damage. (Max at full range)", "線が長いほどダメージが増えます。（射程いっぱいで最大）", "笔画越长伤害越高。（画满射程时最大）");
        A("북극성", "North Star", "北極星", "北极星");
        A("지도 접기를 쓰면 접힌 선 가운데에 별이 떠 4초 동안 가까운 적에게 별빛을 쏩니다. (공격력 50%)", "Fold the Map raises a star over the crease that shoots starlight at nearby enemies for 4s. (50% attack)", "地図折りを使うと折り目の中央に星が浮かび、4秒間近くの敵に星の光を撃ちます。（攻撃力50%）", "使用折叠地图时，折线中央升起一颗星，4秒内向附近敌人射出星光。（攻击力50%）");
        A("쏘는 간격", "Fire interval", "発射間隔", "发射间隔");
        A("여백", "Margin", "余白", "留白");
        A("3초 동안 획을 긋지 않으면 다음 획이 두 배 굵고 더 아픕니다.", "If you don't draw for 3s, your next stroke is twice as thick and hits harder.", "3秒間線を引かないと、次の線が2倍太く強くなります。", "3秒内不画线，下一笔会变为两倍粗且更痛。");
        A("여백 획 피해", "Margin stroke damage", "余白の線のダメージ", "留白笔画伤害");
        A("지름길", "Shortcut", "近道", "捷径");
        A("획을 이어 그리는 동안 이동 속도가 오릅니다.", "Move faster while your strokes are connected.", "線をつないでいる間、移動速度が上がります。", "连续画线期间移动速度提升。");
        A("지우개", "Eraser", "消しゴム", "橡皮擦");
        A("적을 쓰러뜨리면 그 자리 둘레의 적 투사체를 지웁니다.", "Defeating an enemy erases enemy projectiles around it.", "敵を倒すとその周りの敵の投射物を消します。", "击败敌人时擦除其周围的敌方投射物。");
        A("지우는 범위", "Erase radius", "消す範囲", "擦除范围");
        A("보물 지도", "Treasure Map", "宝の地図", "藏宝图");
        A("도형을 닫은 자리에 3초 동안 보물 표시가 남아, 그 안에서 쓰러진 적이 확률로 코인 3개를 줍니다.", "Closed shapes leave a treasure mark for 3s; enemies defeated inside may drop 3 coins.", "図形を閉じた場所に3秒間宝の印が残り、その中で倒れた敵が確率でコイン3枚をくれます。", "闭合图形处留下3秒宝藏标记，在其中被击败的敌人有几率掉落3枚金币。");
        A("코인 확률", "Coin chance", "コイン確率", "金币几率");
        A("나침도", "Compass Rose", "羅針図", "罗盘玫瑰");
        A("도형을 닫은 자리에 나침도가 그려져 3초 동안 안의 적을 느리게 합니다.", "Closed shapes draw a compass rose that slows enemies inside for 3s.", "図形を閉じた場所に羅針図が描かれ、3秒間中の敵を遅くします。", "闭合图形处绘出罗盘玫瑰，3秒内减速其中的敌人。");
        A("느려짐", "Slow", "鈍化", "减速");
        A("잉크 폭풍", "Ink Storm", "インクの嵐", "墨之风暴");
        A("지도 접기로 쓰러진 적에게서 잉크가 튀어 주변 적에게 피해를 줍니다.", "Enemies killed by Fold the Map splash ink that damages nearby foes.", "地図折りで倒れた敵からインクが飛び散り、周りの敵にダメージを与えます。", "被折叠地图击败的敌人溅出墨汁，伤害周围敌人。");
        A("잉크 피해", "Ink damage", "インクダメージ", "墨汁伤害");

        // ---------------- 스킬 진화
        A("세계 지도", "World Map", "世界地図", "世界地图");
        A("도형 피해가 1.5배가 되고, 닫힌 뒤 안쪽 적이 두 번 더 다치며 둘레의 적에게 표식(받는 피해 +20%)이 번집니다.", "Shape damage x1.5; enemies inside are struck twice more and nearby foes are marked (+20% damage taken).", "図形ダメージが1.5倍になり、閉じた後に内側の敵がさらに2回傷つき、周りの敵に標識（被ダメージ+20%）が広がります。", "图形伤害变为1.5倍，闭合后内部敌人再受两次伤害，周围敌人被标记（受到伤害+20%）。");
        A("입체 지도", "Relief Map", "立体地図", "立体地图");
        A("등고선 파동이 세 겹으로 퍼집니다.", "Contour waves ripple out three times.", "等高線の波動が三重に広がります。", "等高线波动扩散三层。");
        A("직행로", "Express Route", "直行路", "直达路线");
        A("획을 이어 그리는 동안 받는 피해가 30% 줄어듭니다.", "Take 30% less damage while your strokes are connected.", "線をつないでいる間、受けるダメージが30%減ります。", "连续画线期间受到的伤害减少30%。");
        A("별자리", "Constellation", "星座", "星座");
        A("북극성이 셋으로 늘고, 별끼리 이어진 선이 닿는 적을 벱니다.", "North Star becomes three stars, and the lines between them cut enemies.", "北極星が3つに増え、星同士をつなぐ線が触れた敵を斬ります。", "北极星增至三颗，星与星之间的连线斩击触碰的敌人。");
        A("먹물 홍수", "Ink Flood", "墨の洪水", "墨水洪流");
        A("번지는 잉크가 두 배로 번지고, 잉크 폭풍이 지나간 자리에 잉크 웅덩이가 남습니다.", "Spreading Ink spreads twice as wide, and Ink Storm leaves ink pools behind.", "にじむインクが2倍に広がり、インクの嵐の跡にインクの水たまりが残ります。", "晕染墨迹扩大两倍，墨之风暴经过处留下墨池。");
        A("대담한 필치", "Bold Strokes", "大胆な筆致", "大胆笔触");
        A("모든 획이 여백 획(두 배 굵기 · 여백 피해)이 됩니다.", "Every stroke becomes a Margin stroke (double width · Margin damage).", "すべての線が余白の線（2倍の太さ・余白ダメージ）になります。", "所有笔画都变为留白笔画（两倍粗细 · 留白伤害）。");
        A("황금 항로", "Golden Route", "黄金航路", "黄金航线");
        A("보물 표시가 두 배 넓어지고, 그 안에서 쓰러진 적은 반드시 코인 3개를 줍니다.", "Treasure marks are twice as wide, and enemies defeated inside always drop 3 coins.", "宝の印が2倍広くなり、その中で倒れた敵は必ずコイン3枚をくれます。", "宝藏标记扩大两倍，在其中被击败的敌人必定掉落3枚金币。");

        // ---------------- 무기 진화 (형태 · 궁극기 · 강화) · 패시브
        A("측량 사슬", "Survey Chain", "測量の鎖", "测量锁链");
        A("획 끝에서 가장 가까운 적에게 측량 사슬이 튑니다. (피해 50%)", "A survey chain jumps from each stroke's end to the nearest enemy. (50% damage)", "線の端から最も近い敵へ測量の鎖が飛びます。（ダメージ50%）", "测量锁链从笔画末端弹向最近的敌人。（伤害50%）");
        A("획 끝에서 가장 가까운 적에게 측량 사슬이 튑니다. (피해 50%)\n궁극기: 삼각 측량", "A survey chain jumps from each stroke's end to the nearest enemy. (50% damage)\nUltimate: Triangulation", "線の端から最も近い敵へ測量の鎖が飛びます。（ダメージ50%）\n究極技：三角測量", "测量锁链从笔画末端弹向最近的敌人。（伤害50%）\n究极技：三角测量");
        A("잉크병", "Inkpot", "インク瓶", "墨水瓶");
        A("획 끝에서 잉크병이 깨져 작게 터집니다. (피해 60%)", "An inkpot shatters at each stroke's end in a small burst. (60% damage)", "線の端でインク瓶が割れて小さく爆発します。（ダメージ60%）", "墨水瓶在笔画末端碎裂小范围爆炸。（伤害60%）");
        A("획 끝에서 잉크병이 깨져 작게 터집니다. (피해 60%)\n궁극기: 먹물 해일", "An inkpot shatters at each stroke's end in a small burst. (60% damage)\nUltimate: Ink Tide", "線の端でインク瓶が割れて小さく爆発します。（ダメージ60%）\n究極技：墨の津波", "墨水瓶在笔画末端碎裂小范围爆炸。（伤害60%）\n究极技：墨潮");
        A("지구본", "Globe", "地球儀", "地球仪");
        A("도형을 닫기 쉬워지고(닫는 거리 1.5배), 닫으면 안쪽 적이 가운데로 모이며 도형 피해 +30%.", "Shapes are easier to close (1.5x closing distance); closing pulls enemies to the center and shape damage +30%.", "図形を閉じやすくなり（閉じる距離1.5倍）、閉じると内側の敵が中央に集まり図形ダメージ+30%。", "更容易闭合图形（闭合距离1.5倍），闭合时内部敌人聚向中央，图形伤害+30%。");
        A("도형을 닫기 쉬워지고, 닫으면 안쪽 적이 가운데로 모이며 도형 피해 +30%.\n궁극기: 자전", "Shapes are easier to close; closing pulls enemies to the center and shape damage +30%.\nUltimate: Rotation", "図形を閉じやすくなり、閉じると内側の敵が中央に集まり図形ダメージ+30%。\n究極技：自転", "更容易闭合图形，闭合时内部敌人聚向中央，图形伤害+30%。\n究极技：自转");
        A("삼각 측량", "Triangulation", "三角測量", "三角测量");
        A("내 둘레에 커다란 삼각형 세 개를 차례로 그려 안쪽의 적을 벱니다. (공격력 220%씩)", "Draw three great triangles around you in turn, cutting enemies inside. (220% attack each)", "自分の周りに大きな三角形を3つ順に描き、内側の敵を斬ります。（攻撃力220%ずつ）", "在身边依次画出三个巨大三角形，斩击其中的敌人。（每个攻击力220%）");
        A("먹물 해일", "Ink Tide", "墨の津波", "墨潮");
        A("마우스 쪽으로 거대한 먹물 파도를 일으켜 휩쓸린 적을 밀어내고 느리게 합니다. (공격력 300%)", "Raise a giant ink wave toward the cursor that pushes back and slows enemies it sweeps. (300% attack)", "マウスの方へ巨大な墨の波を起こし、巻き込んだ敵を押し出して遅くします。（攻撃力300%）", "向鼠标方向掀起巨大墨浪，击退并减速被卷入的敌人。（攻击力300%）");
        A("자전", "Rotation", "自転", "自转");
        A("지구본 네 개가 3초 동안 내 둘레를 돌며 닿는 적을 벱니다. (0.25초마다 공격력 60%)", "Four globes orbit you for 3s, cutting enemies they touch. (60% attack every 0.25s)", "地球儀4つが3秒間自分の周りを回り、触れた敵を斬ります。（0.25秒ごとに攻撃力60%）", "四个地球仪在身边环绕3秒，斩击触碰的敌人。（每0.25秒攻击力60%）");
        A("정밀 측량", "Precise Survey", "精密測量", "精密测量");
        A("획이 적 하나를 지날 때마다 그다음 적에게 주는 피해가 10%씩 오릅니다. (최대 +50%)", "Each enemy a stroke passes raises its damage to the next by 10%. (Up to +50%)", "線が敵を1体通るごとに次の敵へのダメージが10%ずつ上がります。（最大+50%）", "笔画每穿过一名敌人，对下一名的伤害提升10%。（最多+50%）");
        A("이중 인쇄", "Double Print", "二重刷り", "双重印刷");
        A("획을 그을 때마다 0.3초 뒤 같은 자리에 한 번 더 그어집니다. (피해 50%)", "Every stroke is drawn again in the same place 0.3s later. (50% damage)", "線を引くたびに0.3秒後同じ場所にもう一度引かれます。（ダメージ50%）", "每画一笔，0.3秒后在同一位置再画一次。（伤害50%）");
        A("판화", "Engraving", "版画", "版画");
        A("도형을 닫으면 안쪽에 잉크 폭발이 일어납니다. (공격력 150%)", "Closing a shape sets off an ink explosion inside. (150% attack)", "図形を閉じると内側でインクの爆発が起こります。（攻撃力150%）", "闭合图形时内部发生墨水爆炸。（攻击力150%）");
        A("탐험가의 체력", "Explorer's Stamina", "探検家の体力", "探险家的体力");
        A("도형을 닫을 때마다 체력을 2 회복합니다.\n진화: 4", "Heal 2 each time you close a shape.\nEvolved: 4", "図形を閉じるたびに体力を2回復します。\n進化：4", "每闭合一个图形恢复2点生命。\n进化：4");
        A("현지 안내", "Local Guide", "現地案内", "当地向导");
        A("움직이는 동안 받는 피해가 10% 줄어듭니다.\n진화: 18%", "Take 10% less damage while moving.\nEvolved: 18%", "動いている間、受けるダメージが10%減ります。\n進化：18%", "移动中受到的伤害减少10%。\n进化：18%");
        A("미지의 영역", "Terra Incognita", "未知の領域", "未知领域");
        A("6칸보다 멀리 있는 적에게 주는 피해가 20% 늘어납니다.\n진화: +35%", "Deal 20% more damage to enemies over 6 tiles away.\nEvolved: +35%", "6マスより遠くの敵へのダメージが20%増えます。\n進化：+35%", "对6格以外的敌人造成的伤害提升20%。\n进化：+35%");
        A("정밀 제도", "Fine Drafting", "精密製図", "精密制图");
        A("획 끝 가까이의 적에게 주는 피해가 30% 늘어납니다.\n진화: +50%", "Deal 30% more damage to enemies near a stroke's end.\nEvolved: +50%", "線の端近くの敵へのダメージが30%増えます。\n進化：+50%", "对笔画末端附近的敌人造成的伤害提升30%。\n进化：+50%");

        // ---------------- 이야기
        A("길 잃은 측량사는 지도에서 초원 한 칸이 통째로 지워진 것을 발견했다.", "The lost surveyor found an entire square of meadow erased from the map.", "迷子の測量士は、地図から草原がひとマス丸ごと消えているのを見つけた。", "迷途的测量师发现地图上整整一格草原被抹去了。");
        A("지워진 땅은 다시 그리면 된다. 깃펜 끝에 잉크를 듬뿍 찍었다.", "Erased land can be drawn again. The quill drank deep from the inkwell.", "消された土地は描き直せばいい。羽ペンの先にたっぷりとインクをつけた。", "被抹去的土地，重新画上就好。羽毛笔尖蘸满了墨水。");
        A("“첫 번째 선은 저 슬라임의 둘레로 하지.”", "\"My first line will trace that slime.\"", "「最初の線は、あのスライムの輪郭にしよう。」", "“第一笔，就沿着那只史莱姆的轮廓画吧。”");
        A("길 잃은 측량사는 어떤 지도에도 없는 구멍이 신전 바닥에 뚫린 것을 보았다.", "The lost surveyor saw a hole in the temple floor that no map had ever shown.", "迷子の測量士は、どの地図にも載っていない穴が神殿の床に開いているのを見た。", "迷途的测量师看见神殿地面上开了一个任何地图上都没有的洞。");
        A("지도에 없는 곳이라면, 지금 그리면 된다.", "If it isn't on the map, it can be drawn now.", "地図にない場所なら、今描けばいい。", "地图上没有的地方，现在画上就好。");
        A("측량사는 깃펜을 쥐고 어둠 속으로 첫 선을 그었다.", "The surveyor gripped the quill and drew the first line into the dark.", "測量士は羽ペンを握り、闇の中へ最初の線を引いた。", "测量师握紧羽毛笔，向黑暗中画下第一笔。");
        A("측량사는 마지막 빈칸에 「심연」이라고 적고 지도를 접었다.", "The surveyor wrote \"Abyss\" in the last blank square and folded the map.", "測量士は最後の空欄に「深淵」と書き込み、地図を畳んだ。", "测量师在最后一格空白处写下「深渊」，折起了地图。");
        A("세상에서 가장 정확한 지도가 완성된 날이었다.", "It was the day the most accurate map in the world was finished.", "世界で最も正確な地図が完成した日だった。", "那是世界上最精确的地图完成的日子。");
        A("“이제 길을 잃는 사람은 없겠지.”", "\"No one will lose their way now.\"", "「これでもう、道に迷う人はいないだろう。」", "“这下，再也不会有人迷路了吧。”");
        A("하지만 지도 끝 붉게 칠해진 사막에, 아직 그리지 못한 무언가가 꿈틀거리고 있었다...", "But in the red desert at the map's edge, something not yet drawn was stirring...", "しかし地図の端、赤く塗られた砂漠で、まだ描かれていない何かがうごめいていた…", "然而在地图边缘那片涂红的沙漠里，某个尚未画下的东西正在蠢动……");

        // ================================================================= 시계공
        A("시계공", "Clockmaker", "時計職人", "钟表匠");
        A("멈춘 시간의 장인", "Master of Stopped Time", "止まった時の職人", "停滞时间的工匠");
        A("톱니바퀴와 태엽으로 시간을 다루는 장인. 맞힐수록 시계 눈금이 차 정각의 큰 톱니를 던지고, 위기에는 3초 전으로 되돌아갑니다.",
          "A craftsman who bends time with gears and springs. Hits fill the clock until a great o'clock gear flies, and in a pinch he rewinds 3 seconds.",
          "歯車とゼンマイで時を操る職人。当てるほど時計の目盛りがたまり正時の大歯車を投げ、ピンチには3秒前へ巻き戻ります。",
          "以齿轮与发条操纵时间的工匠。命中越多钟表刻度越满，便会掷出整点大齿轮；危急时倒回3秒前。");
        A("톱니바퀴", "Gears", "歯車", "齿轮");
        A("회전하는 톱니를 던집니다. 맞힐 때마다 발밑 시계 눈금이 차고, 다 차면 다음 톱니가 모든 적을 꿰뚫고 터지는 「정각」 톱니가 됩니다",
          "Throw spinning gears. Each hit fills the clock ring at your feet; when full, your next gear becomes an O'Clock gear that pierces everything and explodes",
          "回転する歯車を投げます。当てるたびに足元の時計の目盛りがたまり、満タンになると次の歯車がすべての敵を貫いて爆発する「正時」の歯車になります",
          "掷出旋转齿轮。每次命中都会填满脚下的钟表刻度，填满后下一枚齿轮变为贯穿所有敌人并爆炸的「整点」齿轮");
        A("되감기", "Rewind", "巻き戻し", "倒带");
        A("누르고 있으면 3초 전의 내 모습이 보이고, 떼면 그 자리로 돌아갑니다. 지나온 길의 적을 베고, 그동안 잃은 체력의 절반을 되찾으며, 잃은 체력의 세 배를 둘레 적에게 되갚습니다",
          "Hold to see yourself from 3 seconds ago; release to return there. Cut enemies along the way, recover half the health lost, and repay three times that loss to nearby foes",
          "押している間は3秒前の自分が見え、離すとその場所へ戻ります。通った道の敵を斬り、その間に失った体力の半分を取り戻し、失った体力の3倍を周りの敵に返します",
          "按住可看到3秒前的自己，松开回到那里。斩击途经的敌人，夺回期间失去生命的一半，并将失去生命的三倍回敬给周围敌人");
        A("정각!", "O'Clock!", "正時！", "整点！");
        A("시간 정지!", "Time Stop!", "時間停止！", "时间停止！");
        A("톱니 공격력", "Gear Damage", "歯車攻撃力", "齿轮攻击力");
        A("톱니 공격력 · 이동 속도", "Gear Damage · Move Speed", "歯車攻撃力 · 移動速度", "齿轮攻击力 · 移动速度");

        // ---------------- 전용 레벨업 카드
        A("튕기는 톱니", "Ricochet Gear", "跳ねる歯車", "弹跳齿轮");
        A("톱니가 적을 맞힌 뒤 가까운 다른 적에게 튕깁니다.", "Gears bounce to another nearby enemy after a hit.", "歯車が敵に当たった後、近くの別の敵へ跳ねます。", "齿轮命中后弹向附近的另一名敌人。");
        A("튕기는 횟수", "Bounces", "跳ねる回数", "弹跳次数");
        A("빠른 태엽", "Quick Spring", "速いゼンマイ", "快发条");
        A("정각까지 필요한 눈금이 줄어듭니다.", "Fewer ticks are needed to reach O'Clock.", "正時までに必要な目盛りが減ります。", "达到整点所需的刻度减少。");
        A("필요한 눈금", "Ticks needed", "必要な目盛り", "所需刻度");
        A("시계추", "Pendulum", "振り子", "钟摆");
        A("톱니를 다섯 번 던질 때마다 시계추가 앞쪽을 크게 휩씁니다. (공격력 150%)", "Every fifth gear, a pendulum sweeps wide in front of you. (150% attack)", "歯車を5回投げるごとに振り子が前方を大きく薙ぎ払います。（攻撃力150%）", "每掷出五枚齿轮，钟摆便大幅横扫前方。（攻击力150%）");
        A("휩쓰는 범위", "Sweep range", "薙ぎ払う範囲", "横扫范围");
        A("남은 시각", "Lingering Hour", "残された時刻", "残留时刻");
        A("되감기로 떠난 자리에 시계가 남아 0.5초마다 둘레를 벱니다. (공격력 60%)", "A clock remains where you rewound from, cutting around it every 0.5s. (60% attack)", "巻き戻しで去った場所に時計が残り、0.5秒ごとに周りを斬ります。（攻撃力60%）", "倒带离开处留下时钟，每0.5秒斩击周围。（攻击力60%）");
        A("시계 지속", "Clock duration", "時計の持続", "时钟持续");
        A("정각의 종", "O'Clock Bell", "正時の鐘", "整点之钟");
        A("정각 톱니가 터지면 종소리가 퍼져 주변 적을 잠깐 멈춥니다.", "When an O'Clock gear bursts, a bell toll briefly freezes nearby enemies.", "正時の歯車が爆発すると鐘の音が広がり、周りの敵を少し止めます。", "整点齿轮爆炸时钟声扩散，使周围敌人短暂停住。");
        A("멈추는 시간", "Freeze time", "止める時間", "停止时间");
        A("녹슨 톱니", "Rusty Gear", "錆びた歯車", "锈蚀齿轮");
        A("톱니에 맞은 적이 2초 동안 느려집니다.", "Enemies hit by gears are slowed for 2s.", "歯車に当たった敵が2秒間遅くなります。", "被齿轮击中的敌人减速2秒。");

        // ---------------- 고유 스킬
        A("과회전", "Overclock", "過回転", "超频");
        A("정각 톱니가 터지면 3초 동안 공격 속도가 오릅니다.", "When an O'Clock gear bursts, attack speed rises for 3s.", "正時の歯車が爆発すると3秒間攻撃速度が上がります。", "整点齿轮爆炸后3秒内攻击速度提升。");
        A("자명종", "Alarm Clock", "目覚まし時計", "闹钟");
        A("체력이 30% 아래로 떨어지면 시간이 멈춰 모든 적이 잠깐 멈추고 적 투사체가 사라집니다. (30초에 한 번)", "When health drops below 30%, time stops: all enemies freeze briefly and enemy projectiles vanish. (Once per 30s)", "体力が30%を下回ると時間が止まり、すべての敵が少し止まって敵の投射物が消えます。（30秒に1回）", "生命低于30%时时间停止：所有敌人短暂停住，敌方投射物消失。（每30秒一次）");
        A("맞물린 톱니", "Meshed Gears", "噛み合う歯車", "啮合齿轮");
        A("톱니가 튕길 때마다 피해가 늘어납니다.", "Gears deal more damage with every bounce.", "歯車が跳ねるたびにダメージが増えます。", "齿轮每次弹跳伤害提升。");
        A("튕길 때마다", "Per bounce", "跳ねるたびに", "每次弹跳");
        A("회중시계", "Pocket Watch", "懐中時計", "怀表");
        A("되감기로 되찾는 체력이 늘어납니다.", "Rewind recovers more health.", "巻き戻しで取り戻す体力が増えます。", "倒带夺回的生命增加。");
        A("되찾는 체력", "Health recovered", "取り戻す体力", "夺回生命");
        A("분침", "Minute Hand", "分針", "分针");
        A("정각 톱니의 피해가 늘어납니다.", "O'Clock gears deal more damage.", "正時の歯車のダメージが増えます。", "整点齿轮的伤害提升。");
        A("정각 톱니 피해", "O'Clock gear damage", "正時の歯車ダメージ", "整点齿轮伤害");
        A("시간의 메아리", "Echo of Time", "時のこだま", "时间回响");
        A("톱니를 던지고 0.6초 뒤 같은 방향으로 잔상 톱니가 한 번 더 날아갑니다.", "0.6s after each throw, an afterimage gear flies the same way.", "歯車を投げて0.6秒後、同じ方向へ残像の歯車がもう一度飛びます。", "掷出齿轮0.6秒后，残影齿轮朝同一方向再飞一次。");
        A("잔상 톱니 피해", "Afterimage damage", "残像の歯車ダメージ", "残影齿轮伤害");
        A("태엽 감기", "Winding Up", "ゼンマイ巻き", "上发条");
        A("가만히 서 있으면 시계 눈금이 저절로 찹니다.", "Standing still fills the clock on its own.", "じっと立っていると時計の目盛りがひとりでにたまります。", "站立不动时钟表刻度自动填充。");
        A("초당 눈금", "Ticks per sec", "毎秒の目盛り", "每秒刻度");
        A("시간 고리", "Time Loop", "時の輪", "时间之环");
        A("되감기로 돌아간 자리에 4초 동안 고리가 생겨 안의 적이 느려집니다.", "Rewinding leaves a 4s loop at your destination that slows enemies inside.", "巻き戻しで戻った場所に4秒間輪ができ、中の敵が遅くなります。", "倒带返回处生成持续4秒的环，减速其中的敌人。");
        A("빌린 시간", "Borrowed Time", "借りた時間", "借来的时间");
        A("적을 처치할 때마다 필살기 게이지가 찹니다.", "Each kill fills your Ultimate gauge.", "敵を倒すたびに必殺技ゲージがたまります。", "每击杀一名敌人都会充能必杀技。");
        A("처치당 게이지", "Gauge per kill", "撃破ごとのゲージ", "每次击杀充能");
        A("스톱워치", "Stopwatch", "ストップウォッチ", "秒表");
        A("되감을 때 지난 3초 동안 준 피해의 일부가 가까운 적들에게 한 번 더 들어갑니다.", "When you rewind, part of the damage you dealt in the last 3s strikes nearby enemies again.", "巻き戻す時、直前3秒間に与えたダメージの一部が近くの敵にもう一度入ります。", "倒带时，过去3秒造成伤害的一部分再次作用于附近敌人。");
        A("다시 주는 피해", "Damage replayed", "再び与えるダメージ", "再次造成伤害");

        // ---------------- 스킬 진화
        A("톱니 폭풍", "Gear Storm", "歯車の嵐", "齿轮风暴");
        A("톱니가 튕길 때마다 작은 톱니가 하나 더 갈라져 가까운 적에게 날아갑니다.", "Every bounce splits off a small gear that flies at a nearby enemy.", "歯車が跳ねるたびに小さな歯車がもう1つ分かれて近くの敵へ飛びます。", "齿轮每次弹跳都会分出一枚小齿轮飞向附近敌人。");
        A("대종", "Great Bell", "大鐘", "大钟");
        A("정각 톱니가 터지면 둘레 14칸의 모든 적이 0.5초 동안 멈춥니다.", "When an O'Clock gear bursts, every enemy within 14 tiles freezes for 0.5s.", "正時の歯車が爆発すると周囲14マスのすべての敵が0.5秒止まります。", "整点齿轮爆炸时，周围14格内所有敌人停住0.5秒。");
        A("역설", "Paradox", "逆説", "悖论");
        A("남은 시각의 시계가 두 배로 벱니다.", "Lingering Hour clocks cut twice as hard.", "残された時刻の時計が2倍の強さで斬ります。", "残留时刻的时钟斩击伤害翻倍。");
        A("영원", "Eternity", "永遠", "永恒");
        A("자명종이 15초마다 울리고, 시간이 멈춘 동안 체력이 빠르게 회복됩니다.", "Alarm Clock can ring every 15s, and you heal quickly while time is stopped.", "目覚まし時計が15秒ごとに鳴り、時間が止まっている間体力が素早く回復します。", "闹钟每15秒即可响起，时间停止期间快速恢复生命。");
        A("태엽 장치", "Clockwork", "からくり仕掛け", "发条装置");
        A("정각 톱니가 터진 자리에서 톱니 세 개가 사방으로 날아갑니다.", "Three gears fly out in all directions where an O'Clock gear bursts.", "正時の歯車が爆発した場所から歯車3つが四方へ飛びます。", "整点齿轮爆炸处，三枚齿轮飞向四方。");
        A("무한 반향", "Endless Echo", "無限の反響", "无尽回响");
        A("잔상 톱니가 두 번 날아갑니다.", "Afterimage gears fly twice.", "残像の歯車が2回飛びます。", "残影齿轮飞出两次。");
        A("시간 빚", "Time Debt", "時の借り", "时间之债");
        A("스톱워치가 두 배로 들어가고, 되감을 때마다 필살기 게이지를 30% 돌려받습니다.", "Stopwatch replays twice as much, and each rewind refunds 30% of your Ultimate gauge.", "ストップウォッチが2倍入り、巻き戻すたびに必殺技ゲージを30%取り戻します。", "秒表效果翻倍，每次倒带返还30%必杀技充能。");

        // ---------------- 무기 진화 · 궁극기 · 강화 · 패시브
        A("뻐꾸기 시계", "Cuckoo Clock", "鳩時計", "布谷鸟钟");
        A("정각 톱니가 터질 때마다 뻐꾸기 셋이 튀어나와 가까운 적을 쪼습니다. (공격력 100%씩)", "Each O'Clock gear burst releases three cuckoos that peck nearby enemies. (100% attack each)", "正時の歯車が爆発するたびに鳩が3羽飛び出して近くの敵をつつきます。（攻撃力100%ずつ）", "每次整点齿轮爆炸都会弹出三只布谷鸟啄击附近敌人。（每只攻击力100%）");
        A("정각 톱니가 터질 때마다 뻐꾸기 셋이 튀어나와 가까운 적을 쪼습니다.\n궁극기: 뻐꾸기 떼", "Each O'Clock gear burst releases three cuckoos that peck nearby enemies.\nUltimate: Cuckoo Flock", "正時の歯車が爆発するたびに鳩が3羽飛び出して近くの敵をつつきます。\n究極技：鳩の群れ", "每次整点齿轮爆炸都会弹出三只布谷鸟啄击附近敌人。\n究极技：布谷鸟群");
        A("모래시계", "Hourglass", "砂時計", "沙漏");
        A("톱니에 맞은 적이 모래에 묻혀 1초 동안 절반 속도로 느려집니다.", "Enemies hit by gears are buried in sand, moving at half speed for 1s.", "歯車に当たった敵が砂に埋もれ、1秒間半分の速さに遅くなります。", "被齿轮击中的敌人被沙子掩埋，1秒内速度减半。");
        A("톱니에 맞은 적이 모래에 묻혀 1초 동안 절반 속도로 느려집니다.\n궁극기: 모래 폭풍", "Enemies hit by gears are buried in sand, moving at half speed for 1s.\nUltimate: Sandstorm", "歯車に当たった敵が砂に埋もれ、1秒間半分の速さに遅くなります。\n究極技：砂嵐", "被齿轮击中的敌人被沙子掩埋，1秒内速度减半。\n究极技：沙暴");
        A("괘종시계", "Grandfather Clock", "柱時計", "落地钟");
        A("톱니가 크고 느려지지만 피해가 40% 늘어납니다.", "Gears grow larger and slower but deal 40% more damage.", "歯車が大きく遅くなりますが、ダメージが40%増えます。", "齿轮变大变慢，但伤害提升40%。");
        A("톱니가 크고 느려지지만 피해가 40% 늘어납니다.\n궁극기: 열두 번의 종", "Gears grow larger and slower but deal 40% more damage.\nUltimate: Twelve Tolls", "歯車が大きく遅くなりますが、ダメージが40%増えます。\n究極技：十二の鐘", "齿轮变大变慢，但伤害提升40%。\n究极技：十二响钟");
        A("뻐꾸기 떼", "Cuckoo Flock", "鳩の群れ", "布谷鸟群");
        A("뻐꾸기 열두 마리가 가까운 적들을 차례로 덮칩니다. (공격력 150%씩)", "Twelve cuckoos swoop on nearby enemies in turn. (150% attack each)", "鳩12羽が近くの敵へ次々と襲いかかります。（攻撃力150%ずつ）", "十二只布谷鸟依次扑向附近敌人。（每只攻击力150%）");
        A("모래 폭풍", "Sandstorm", "砂嵐", "沙暴");
        A("마우스 자리에 4초 동안 모래 회오리가 생겨 적을 끌어당기고 0.3초마다 벱니다. (공격력 60%)", "A sand whirlwind forms at the cursor for 4s, pulling enemies in and cutting every 0.3s. (60% attack)", "マウスの位置に4秒間砂の竜巻が生まれ、敵を引き寄せて0.3秒ごとに斬ります。（攻撃力60%）", "在鼠标处生成持续4秒的沙之旋风，吸引敌人并每0.3秒斩击。（攻击力60%）");
        A("열두 번의 종", "Twelve Tolls", "十二の鐘", "十二响钟");
        A("내 둘레로 종소리 고리가 열두 번 퍼집니다. (공격력 80%씩)", "Twelve rings of bell tolls spread around you. (80% attack each)", "自分の周りに鐘の音の輪が12回広がります。（攻撃力80%ずつ）", "钟声之环在身边扩散十二次。（每次攻击力80%）");
        A("정밀 톱니", "Precision Gear", "精密な歯車", "精密齿轮");
        A("정각까지 필요한 눈금이 2 줄어듭니다.", "O'Clock needs 2 fewer ticks.", "正時までに必要な目盛りが2減ります。", "达到整点所需刻度减少2。");
        A("이중 톱니", "Twin Gears", "二重の歯車", "双重齿轮");
        A("톱니를 하나 더 던집니다.", "Throw one more gear.", "歯車をもう1つ投げます。", "多掷出一枚齿轮。");
        A("시간 균열", "Time Rift", "時の裂け目", "时间裂隙");
        A("정각 톱니가 터진 자리에 2초 동안 시간 균열이 생겨 안의 적을 느리게 하고 벱니다.", "O'Clock gear bursts leave a 2s time rift that slows and cuts enemies inside.", "正時の歯車が爆発した場所に2秒間時の裂け目が生まれ、中の敵を遅くして斬ります。", "整点齿轮爆炸处出现持续2秒的时间裂隙，减速并斩击其中敌人。");
        A("기계 심장", "Clockwork Heart", "機械の心臓", "机械之心");
        A("체력이 초당 0.5씩 회복됩니다.\n진화: 초당 1", "Regenerate 0.5 health per second.\nEvolved: 1 per second", "体力が毎秒0.5ずつ回復します。\n進化：毎秒1", "每秒恢复0.5生命。\n进化：每秒1");
        A("예측", "Foresight", "予測", "预判");
        A("체력이 절반 이상일 때 받는 피해가 12% 줄어듭니다.\n진화: 20%", "Take 12% less damage while above half health.\nEvolved: 20%", "体力が半分以上のとき受けるダメージが12%減ります。\n進化：20%", "生命高于一半时受到的伤害减少12%。\n进化：20%");
        A("부품 수집", "Spare Parts", "部品集め", "零件收集");
        A("적을 처치하면 8% 확률로 다음 톱니가 정각 톱니가 됩니다.\n진화: 15%", "Kills have an 8% chance to make your next gear an O'Clock gear.\nEvolved: 15%", "敵を倒すと8%の確率で次の歯車が正時の歯車になります。\n進化：15%", "击杀敌人时有8%几率使下一枚齿轮成为整点齿轮。\n进化：15%");
        A("시간 여유", "Spare Time", "時間の余裕", "时间余裕");
        A("필살기 게이지가 10% 더 빨리 찹니다.\n진화: 18%", "Your Ultimate gauge fills 10% faster.\nEvolved: 18%", "必殺技ゲージが10%速くたまります。\n進化：18%", "必杀技充能速度提升10%。\n进化：18%");

        // ---------------- 이야기
        A("마을 시계탑의 바늘이 멈췄다. 시계공은 그것이 고장이 아니라 경고라는 것을 알았다.", "The village clock tower's hands stopped. The clockmaker knew it was no fault, but a warning.", "村の時計塔の針が止まった。時計職人はそれが故障ではなく警告だと分かっていた。", "村里钟楼的指针停了。钟表匠知道那不是故障，而是警告。");
        A("멈춘 시간을 다시 돌리려면, 원인을 부숴야 한다.", "To set stopped time running again, the cause must be broken.", "止まった時を再び動かすには、原因を壊さなければならない。", "想让停滞的时间重新转动，就得摧毁根源。");
        A("시계공은 톱니바퀴 한 주머니를 챙겨 초원으로 향했다.", "The clockmaker packed a pouch of gears and headed for the meadow.", "時計職人は歯車をひと袋詰めて草原へ向かった。", "钟表匠装了一袋齿轮，向草原出发。");
        A("죽은 자가 걸어 다니는 것은, 누군가 시간을 거꾸로 감았기 때문이다.", "The dead walk because someone has wound time backward.", "死者が歩き回るのは、誰かが時を逆に巻いたからだ。", "死者行走，是因为有人把时间倒着上了发条。");
        A("시계공은 톱니바퀴 한 주머니를 챙겨 신전의 구멍으로 내려갔다.", "The clockmaker packed a pouch of gears and climbed down into the temple's hole.", "時計職人は歯車をひと袋詰めて神殿の穴へ降りていった。", "钟表匠装了一袋齿轮，下到了神殿的洞里。");
        A("멈췄던 시계탑의 바늘이 다시 째깍거리기 시작했다.", "The clock tower's frozen hands began to tick again.", "止まっていた時計塔の針が再びカチカチと動き始めた。", "停住的钟楼指针又开始嘀嗒作响。");
        A("시계공은 바늘을 1분 앞으로 돌려 두었다. 늘 조금 일찍 준비하는 편이 좋으니까.", "The clockmaker set the hands one minute fast. It's always better to be a little early.", "時計職人は針を1分進めておいた。いつも少し早めに備えるほうがいいから。", "钟表匠把指针拨快了一分钟。总是早一点准备比较好。");
        A("“다음은 언제 멈출지, 이제 나는 알지.”", "\"Now I know when it will stop next.\"", "「次にいつ止まるか、今の私には分かる。」", "“下次什么时候会停，现在我知道了。”");

        // ================================================================= 우체부
        A("우체부", "Postman", "郵便配達人", "邮差");
        A("저승 배달부", "Courier of the Dead", "冥府の配達人", "冥府信使");
        A("살아 있는 자에게도 죽은 자에게도 편지를 전하는 배달부. 편지는 가장 먼 적부터 찾아가고, 속달 한 번이면 화면의 적 모두에게 편지가 터집니다.",
          "A courier who delivers to the living and the dead alike. Letters seek out the farthest foe first, and one Express Delivery bursts letters on every enemy around.",
          "生者にも死者にも手紙を届ける配達人。手紙は最も遠い敵から探しに行き、速達ひとつで周りの敵すべてに手紙が弾けます。",
          "无论生者死者都会送达信件的邮差。信件先寻找最远的敌人，一次特快便能让周围所有敌人身上的信件同时爆开。");
        A("편지", "Letters", "手紙", "信件");
        A("마우스 쪽에서 가장 먼 적을 받는 이로 정해 편지를 날립니다. 편지는 다른 적을 지나쳐 받는 이를 쫓아가고, 열 번 배달할 때마다 코인과 필살기 게이지를 받습니다",
          "Address a letter to the farthest enemy toward the cursor. It slips past other foes to chase its addressee, and every 10 deliveries earns coins and Ultimate gauge",
          "マウスの方で最も遠い敵を宛先にして手紙を飛ばします。手紙は他の敵をすり抜けて宛先を追い、10回配達するたびにコインと必殺技ゲージがもらえます",
          "以鼠标方向最远的敌人为收件人掷出信件。信件越过其他敌人追踪收件人，每送达10次获得金币与必杀技充能");
        A("속달", "Express Delivery", "速達", "特快专递");
        A("누르고 있는 동안 둘레의 적에게 편지 표식이 차례로 붙고 (오래 누를수록 많이), 떼면 1초 뒤 표식이 모두 한꺼번에 터집니다",
          "While held, letter marks attach to nearby enemies one by one (more the longer you hold); release and all marks burst together 1s later",
          "押している間、周りの敵に手紙の印が順に付き（長く押すほど多く）、離すと1秒後に印がすべて一斉に弾けます",
          "按住时依次在周围敌人身上贴上信件标记（按得越久越多），松开1秒后所有标记同时爆炸");
        A("배달 완료!", "Delivered!", "配達完了！", "送达！");
        A("편지 공격력", "Letter Damage", "手紙攻撃力", "信件攻击力");
        A("편지 공격력 · 이동 속도", "Letter Damage · Move Speed", "手紙攻撃力 · 移動速度", "信件攻击力 · 移动速度");
        A("통", "", "通", "封");

        // ---------------- 전용 레벨업 카드
        A("등기 우편", "Registered Mail", "書留", "挂号信");
        A("편지가 받는 이에게 닿으면 가까운 다른 적에게 다시 배달됩니다. (피해 80%)", "After reaching its addressee, a letter is redelivered to another nearby enemy. (80% damage)", "手紙が宛先に届くと、近くの別の敵へもう一度配達されます。（ダメージ80%）", "信件送达收件人后，再投递给附近另一名敌人。（伤害80%）");
        A("다시 배달", "Redeliveries", "再配達", "再次投递");
        A("소포", "Parcel", "小包", "包裹");
        A("편지가 닿은 자리에서 작게 터집니다. (피해 50%)", "Letters burst on arrival. (50% damage)", "手紙が届いた場所で小さく爆発します。（ダメージ50%）", "信件送达处小范围爆炸。（伤害50%）");
        A("폭발 범위", "Blast radius", "爆発範囲", "爆炸范围");
        A("우표 수집", "Stamp Collecting", "切手集め", "集邮");
        A("배달할 때마다 다음 편지의 피해가 늘어납니다. (최대 10중첩)", "Each delivery strengthens your next letters. (Up to 10 stacks)", "配達するたびに次の手紙のダメージが増えます。（最大10重）", "每次送达都会强化之后的信件。（最多10层）");
        A("답장", "Reply", "返信", "回信");
        A("속달 표식이 터진 자리에서 작은 편지가 가까운 적에게 날아갑니다. (공격력 60%)", "Where Express marks burst, small letters fly to nearby enemies. (60% attack)", "速達の印が弾けた場所から小さな手紙が近くの敵へ飛びます。（攻撃力60%）", "特快标记爆炸处飞出小信件射向附近敌人。（攻击力60%）");
        A("답장 수", "Replies", "返信の数", "回信数");
        A("빠른 우편", "Quick Post", "速い郵便", "快邮");
        A("편지가 더 빠르게 날아가고 더 날카롭게 꺾입니다.", "Letters fly faster and turn more sharply.", "手紙がより速く飛び、より鋭く曲がります。", "信件飞得更快，转向更灵敏。");
        A("속도 · 꺾임", "Speed · Turning", "速度・旋回", "速度 · 转向");
        A("반송", "Return to Sender", "返送", "退回");
        A("받는 이가 쓰러지면 편지가 다른 적에게 한 번 더 날아갑니다.", "If the addressee falls, the letter flies on to another enemy.", "宛先が倒れると、手紙がもう一度別の敵へ飛びます。", "收件人倒下时，信件再飞向另一名敌人。");
        A("반송 피해", "Return damage", "返送ダメージ", "退回伤害");

        // ---------------- 고유 스킬
        A("특급", "Priority Mail", "特急", "特急");
        A("배달 열 번마다 필살기 게이지가 더 찹니다.", "Every 10 deliveries fill extra Ultimate gauge.", "配達10回ごとに必殺技ゲージがさらにたまります。", "每送达10次额外充能必杀技。");
        A("소인", "Postmark", "消印", "邮戳");
        A("편지를 받은 적에게 소인이 찍혀 4초 동안 받는 피해가 늘어납니다.", "Enemies who receive a letter are postmarked, taking more damage for 4s.", "手紙を受け取った敵に消印が押され、4秒間受けるダメージが増えます。", "收到信件的敌人被盖上邮戳，4秒内受到的伤害提升。");
        A("우편 가방", "Mailbag", "郵便かばん", "邮包");
        A("움직이는 동안 편지의 피해가 늘어납니다.", "Letters deal more damage while you move.", "動いている間、手紙のダメージが増えます。", "移动中信件伤害提升。");
        A("편지 피해", "Letter damage", "手紙ダメージ", "信件伤害");
        A("행운의 편지", "Chain Letter", "幸運の手紙", "幸运信");
        A("편지로 적을 쓰러뜨리면 30% 확률로 새 편지가 가까운 적들에게 날아갑니다. (공격력 70%)", "Letter kills have a 30% chance to send new letters to nearby enemies. (70% attack)", "手紙で敵を倒すと30%の確率で新しい手紙が近くの敵たちへ飛びます。（攻撃力70%）", "信件击杀敌人时有30%几率向附近敌人寄出新信件。（攻击力70%）");
        A("새 편지", "New letters", "新しい手紙", "新信件");
        A("배달 경로", "Delivery Route", "配達ルート", "投递路线");
        A("바로 앞 받는 이와 다른 적에게 배달하면 피해가 늘어납니다.", "Delivering to a different enemy than the last addressee deals more damage.", "直前の宛先と違う敵に配達するとダメージが増えます。", "投递给与上一位收件人不同的敌人时伤害提升。");
        A("영수증", "Receipt", "領収書", "收据");
        A("배달을 마칠 때마다 체력을 회복합니다.", "Heal each time you complete a delivery.", "配達を終えるたびに体力を回復します。", "每完成一次投递恢复生命。");
        A("궂은 날씨", "Foul Weather", "悪天候", "恶劣天气");
        A("체력이 절반 아래일 때 편지의 피해가 늘어납니다.", "Letters deal more damage while you're below half health.", "体力が半分を下回ると手紙のダメージが増えます。", "生命低于一半时信件伤害提升。");
        A("익일 배송", "Next-Day Delivery", "翌日配送", "次日达");
        A("속달 표식이 터지고 1초 뒤 같은 자리가 한 번 더 터집니다.", "1s after Express marks burst, the same spots burst again.", "速達の印が弾けた1秒後、同じ場所がもう一度弾けます。", "特快标记爆炸1秒后，同一位置再爆炸一次。");
        A("두 번째 피해", "Second blast", "2回目のダメージ", "第二次伤害");
        A("정확한 주소", "Exact Address", "正確な住所", "精确地址");
        A("8칸보다 멀리 있는 적에게 배달하면 피해가 늘어납니다.", "Deliveries to enemies over 8 tiles away deal more damage.", "8マスより遠くの敵に配達するとダメージが増えます。", "投递给8格以外的敌人时伤害提升。");
        A("광고 전단", "Junk Mail", "広告チラシ", "广告传单");
        A("편지를 보낼 때 20% 확률로 전단 세 장이 부채꼴로 흩어집니다.", "Sending letters has a 20% chance to scatter three flyers in a fan.", "手紙を送るとき20%の確率でチラシ3枚が扇状に散らばります。", "寄信时有20%几率呈扇形撒出三张传单。");
        A("전단 피해", "Flyer damage", "チラシダメージ", "传单伤害");

        // ---------------- 스킬 진화
        A("내용증명", "Certified Mail", "内容証明", "内容证明");
        A("소인의 효과가 두 배가 됩니다.", "Postmark's effect is doubled.", "消印の効果が2倍になります。", "邮戳效果翻倍。");
        A("대형 소포", "Bulk Parcel", "大型小包", "大型包裹");
        A("소포 폭발이 두 배로 넓어지고, 행운의 편지 확률이 50%가 됩니다.", "Parcel blasts are twice as wide, and Chain Letter's chance rises to 50%.", "小包の爆発が2倍広くなり、幸運の手紙の確率が50%になります。", "包裹爆炸范围翻倍，幸运信几率提升至50%。");
        A("우표 수집가", "Philatelist", "切手収集家", "集邮家");
        A("우표가 20중첩까지 쌓이고, 특급 게이지가 두 배가 됩니다.", "Stamps stack up to 20, and Priority Mail's gauge is doubled.", "切手が20重まで重なり、特急のゲージが2倍になります。", "邮票可叠加至20层，特急充能翻倍。");
        A("답장 폭주", "Reply Flood", "返信の嵐", "回信潮");
        A("익일 배송이 터질 때도 답장 편지가 날아갑니다.", "Next-Day Delivery blasts also send reply letters.", "翌日配送が弾けるときも返信の手紙が飛びます。", "次日达爆炸时也会飞出回信。");
        A("총알 배송", "Bullet Delivery", "弾丸配送", "子弹快递");
        A("움직이는 동안 편지를 한 통 더 보냅니다.", "Send one more letter while moving.", "動いている間、手紙をもう1通送ります。", "移动中多寄出一封信。");
        A("반송 불가", "No Returns", "返送不可", "拒绝退回");
        A("반송된 편지가 최대 세 번까지 계속 이어집니다.", "Returned letters keep going up to three times.", "返送された手紙が最大3回まで続きます。", "退回的信件最多可连续三次。");
        A("전단 폭탄", "Flyer Bomb", "チラシ爆弾", "传单炸弹");
        A("광고 전단이 맞은 자리에서 터집니다. (공격력 60%)", "Junk Mail flyers explode on hit. (60% attack)", "広告チラシが当たった場所で爆発します。（攻撃力60%）", "广告传单命中处爆炸。（攻击力60%）");

        // ---------------- 무기 진화 · 궁극기 · 강화 · 패시브
        A("비둘기 우편", "Pigeon Post", "鳩便", "信鸽");
        A("첫 편지는 비둘기가 물고 가 받는 이를 두 번 더 쪼습니다. (피해 50%씩)", "A pigeon carries your first letter and pecks the addressee twice more. (50% damage each)", "最初の手紙は鳩がくわえて運び、宛先をさらに2回つつきます。（ダメージ50%ずつ）", "第一封信由信鸽衔去，再啄收件人两次。（每次伤害50%）");
        A("첫 편지는 비둘기가 물고 가 받는 이를 두 번 더 쪼습니다.\n궁극기: 비둘기 떼", "A pigeon carries your first letter and pecks the addressee twice more.\nUltimate: Pigeon Flock", "最初の手紙は鳩がくわえて運び、宛先をさらに2回つつきます。\n究極技：鳩の群れ", "第一封信由信鸽衔去，再啄收件人两次。\n究极技：鸽群");
        A("소포 상자", "Parcel Box", "小包箱", "包裹箱");
        A("편지가 소포 상자가 되어 맞은 자리에서 터집니다. (피해 60%)", "Letters become parcel boxes that burst on arrival. (60% damage)", "手紙が小包箱になり、届いた場所で爆発します。（ダメージ60%）", "信件变为包裹箱，送达处爆炸。（伤害60%）");
        A("편지가 소포 상자가 되어 맞은 자리에서 터집니다.\n궁극기: 택배 폭격", "Letters become parcel boxes that burst on arrival.\nUltimate: Parcel Barrage", "手紙が小包箱になり、届いた場所で爆発します。\n究極技：宅配爆撃", "信件变为包裹箱，送达处爆炸。\n究极技：快递轰炸");
        A("우체통", "Postbox", "郵便ポスト", "邮筒");
        A("3초마다 발밑에 우체통이 서서 4초 동안 가까운 적에게 편지를 던집니다.", "Every 3s a postbox appears at your feet and throws letters at nearby enemies for 4s.", "3秒ごとに足元に郵便ポストが立ち、4秒間近くの敵へ手紙を投げます。", "每3秒脚下立起一个邮筒，4秒内向附近敌人投掷信件。");
        A("3초마다 발밑에 우체통이 서서 4초 동안 가까운 적에게 편지를 던집니다.\n궁극기: 우체통 행렬", "Every 3s a postbox appears at your feet and throws letters at nearby enemies for 4s.\nUltimate: Postbox Parade", "3秒ごとに足元に郵便ポストが立ち、4秒間近くの敵へ手紙を投げます。\n究極技：ポストの行列", "每3秒脚下立起一个邮筒，4秒内向附近敌人投掷信件。\n究极技：邮筒行列");
        A("비둘기 떼", "Pigeon Flock", "鳩の群れ", "鸽群");
        A("비둘기 스무 마리가 마우스 쪽으로 날아가며 닿는 적을 쪼습니다. (공격력 90%씩)", "Twenty pigeons fly toward the cursor, pecking enemies they touch. (90% attack each)", "鳩20羽がマウスの方へ飛び、触れた敵をつつきます。（攻撃力90%ずつ）", "二十只鸽子飞向鼠标方向，啄击触碰的敌人。（每只攻击力90%）");
        A("택배 폭격", "Parcel Barrage", "宅配爆撃", "快递轰炸");
        A("마우스 둘레에 상자 열 개가 차례로 떨어져 터집니다. (공격력 180%씩)", "Ten boxes drop around the cursor in turn and explode. (180% attack each)", "マウスの周りに箱10個が次々と落ちて爆発します。（攻撃力180%ずつ）", "十个箱子依次落在鼠标周围并爆炸。（每个攻击力180%）");
        A("우체통 행렬", "Postbox Parade", "ポストの行列", "邮筒行列");
        A("내 둘레에 우체통 다섯 개가 서서 4초 동안 가까운 적에게 편지를 던집니다.", "Five postboxes rise around you and throw letters at nearby enemies for 4s.", "自分の周りに郵便ポスト5つが立ち、4秒間近くの敵へ手紙を投げます。", "身边立起五个邮筒，4秒内向附近敌人投掷信件。");
        A("두꺼운 봉투", "Thick Envelope", "分厚い封筒", "厚信封");
        A("편지 피해가 25% 늘어납니다.", "Letter damage +25%.", "手紙のダメージが25%増えます。", "信件伤害提升25%。");
        A("다발 배송", "Bundle Delivery", "まとめ配送", "批量投递");
        A("편지를 두 통 더 보냅니다. (피해 50%)", "Send two more letters. (50% damage)", "手紙をさらに2通送ります。（ダメージ50%）", "多寄出两封信。（伤害50%）");
        A("등기 표식", "Registered Marks", "書留の印", "挂号标记");
        A("속달 표식이 50% 더 많이 붙습니다.", "Express Delivery places 50% more marks.", "速達の印が50%多く付きます。", "特快标记数量增加50%。");
        A("튼튼한 다리", "Sturdy Legs", "丈夫な脚", "强健双腿");
        A("이동 속도가 8% 빨라집니다.\n진화: 14%", "Move speed +8%.\nEvolved: 14%", "移動速度が8%速くなります。\n進化：14%", "移动速度提升8%。\n进化：14%");
        A("단골 손님", "Regular Customer", "常連さん", "老主顾");
        A("보스 · 중간 보스에게 주는 피해가 15% 늘어납니다.\n진화: +25%", "Deal 15% more damage to bosses and mini-bosses.\nEvolved: +25%", "ボス・中ボスへのダメージが15%増えます。\n進化：+25%", "对首领与精英造成的伤害提升15%。\n进化：+25%");
        A("우편 요금", "Postage", "郵便料金", "邮资");
        A("배달을 마칠 때마다 5% 확률로 코인 1개를 받습니다.\n진화: 10%", "Each delivery has a 5% chance to earn 1 coin.\nEvolved: 10%", "配達を終えるたびに5%の確率でコイン1枚をもらいます。\n進化：10%", "每次投递有5%几率获得1枚金币。\n进化：10%");
        A("개 조심", "Beware of Dog", "猛犬注意", "当心恶犬");
        A("둘레 3칸에 적이 넷 이상이면 받는 피해가 10% 줄어듭니다.\n진화: 18%", "Take 10% less damage while four or more enemies are within 3 tiles.\nEvolved: 18%", "周囲3マスに敵が4体以上いると受けるダメージが10%減ります。\n進化：18%", "周围3格内有四名以上敌人时受到的伤害减少10%。\n进化：18%");

        // ---------------- 이야기
        A("저승 배달부의 가방에 수취인이 「킹 슬라임」인 편지가 한 통 들어 있었다.", "In the courier's bag was a single letter addressed to \"King Slime.\"", "冥府の配達人のかばんに、宛先が「キングスライム」の手紙が1通入っていた。", "冥府信使的包里，有一封收件人写着「史莱姆王」的信。");
        A("보낸 이는 초원에서 녹아 사라진 사람들. 내용은 짧았다. 「돌려줘.」", "It came from everyone dissolved in the meadow. It was short: \"Give us back.\"", "差出人は草原で溶けて消えた人々。中身は短かった。「返して。」", "寄信人是在草原上被溶化消失的人们。内容很短：「还给我们。」");
        A("배달부는 모자를 고쳐 쓰고 초원으로 걸어 나갔다. 편지는 반드시 전해진다.", "The courier straightened his cap and walked into the meadow. Every letter gets delivered.", "配達人は帽子をかぶり直して草原へ歩き出した。手紙は必ず届けられる。", "信使正了正帽子，走向草原。信件一定会送达。");
        A("저승 배달부의 가방에 수취인이 「리치 왕」인 편지가 한 통 들어 있었다.", "In the courier's bag was a single letter addressed to \"the Lich King.\"", "冥府の配達人のかばんに、宛先が「リッチキング」の手紙が1通入っていた。", "冥府信使的包里，有一封收件人写着「巫妖王」的信。");
        A("보낸 이는 묘역에 잠들어 있던 모든 영혼. 내용은 짧았다. 「이제 그만 재워 줘.」", "It came from every soul that slept in the catacombs. It was short: \"Let us rest now.\"", "差出人は墓所で眠っていたすべての魂。中身は短かった。「もう眠らせて。」", "寄信人是长眠在墓地的所有灵魂。内容很短：「让我们安息吧。」");
        A("배달부는 모자를 고쳐 쓰고 신전의 구멍으로 내려갔다. 편지는 반드시 전해진다.", "The courier straightened his cap and climbed down into the temple's hole. Every letter gets delivered.", "配達人は帽子をかぶり直して神殿の穴へ降りていった。手紙は必ず届けられる。", "信使正了正帽子，下到神殿的洞里。信件一定会送达。");
        A("배달부의 가방이 텅 비었다. 모든 편지가 제 주인에게 닿았다.", "The courier's bag was empty. Every letter had reached its owner.", "配達人のかばんが空になった。すべての手紙が持ち主に届いた。", "信使的包空了。所有信件都已送到主人手中。");
        A("떠오르는 영혼들이 하나씩 배달부에게 손을 흔들었다. 답장 대신이었다.", "One by one, the rising souls waved to the courier. That was their reply.", "昇っていく魂たちが一人ずつ配達人に手を振った。返事の代わりだった。", "升起的灵魂一个个向信使挥手。那就是它们的回信。");
        A("“배달 완료.”", "\"Delivered.\"", "「配達完了。」", "“送达。”");
        A("하지만 가방 바닥에서, 붉은 사막의 소인이 찍힌 편지 한 통이 바스락거렸다...", "But at the bottom of the bag, a letter postmarked from the red desert rustled...", "しかしかばんの底で、赤い砂漠の消印が押された手紙が1通かさかさと音を立てた…", "然而包底，一封盖着红色沙漠邮戳的信沙沙作响……");
    }
}
