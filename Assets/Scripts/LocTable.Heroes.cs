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
    }
}
