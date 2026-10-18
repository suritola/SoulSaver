// 번역표 (1.0.2): 새 캐릭터 (지도 제작자 · 시계공 · 우체부 · 유령) — 이름 · 설명 · 카드 · 고유 스킬 · 진화 · 무기 · 패시브 · 이야기
// LocTable.Skills 의 정적 생성자가 부름 (이미 있는 키는 건드리지 않음)
public static partial class LocTable
{
    static void AddHeroes()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }
        // 1.1.9 문구 다듬기: 키(코드 속 한국어)는 그대로 두고 보이는 한국어만 바꿀 때
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 지도 제작자
        A("지도 제작자", "Cartographer", "地図職人", "制图师");
        A("길 잃은 측량사", "The Lost Surveyor", "迷子の測量士", "迷途的测量师");
        K("깃펜으로 세상을 그리는 측량사. 그은 선으로 적을 베고, 세 획마다 저절로 그려지는 도형으로 안쪽을 통째로 짓이깁니다.",
          "깃펜으로 세상을 그리는 측량사입니다. 그은 선으로 적을 베고, 세 획마다 저절로 그려지는 도형으로 안쪽을 통째로 짓이깁니다.",
          "A surveyor who draws the world with a quill. Strokes cut foes, and every third stroke draws a shape that crushes everything inside.",
          "羽ペンで世界を描く測量士。引いた線で敵を斬り、3本ごとに自動で描かれる図形で内側をまとめて押し潰します。",
          "用羽毛笔描绘世界的测量师。画出的线斩击敌人，每三笔自动画出的图形将内部一并碾碎。");
        A("깃펜", "Quill", "羽ペン", "羽毛笔");
        K("마우스 쪽으로 잉크 획을 그어 닿은 적을 벱니다. 세 번째 획마다 획 끝을 감싸는 삼각형이 저절로 닫혀 안쪽의 적이 크게 다칩니다",
          "마우스 쪽으로 잉크 획을 그어 닿은 적을 벱니다. 세 번째 획마다 획 끝을 감싸는 삼각형이 저절로 닫혀 안쪽의 적이 크게 다칩니다.",
          "Draw an ink stroke toward the cursor, cutting enemies it touches. Every third stroke closes a triangle around its end, dealing heavy damage to enemies inside.",
          "マウスの方へインクの線を引き、触れた敵を斬ります。3本目の線ごとに線の端を囲む三角形が自動で閉じ、内側の敵に大ダメージを与えます。",
          "向鼠标方向画出墨线斩击触碰的敌人。每第三笔会自动在笔画末端闭合一个三角形，内部敌人受到重创。");
        A("지도 접기", "Fold the Map", "地図折り", "折叠地图");
        K("누르고 있으면 접히는 선이 보이고, 떼면 선 양쪽의 적을 선 위로 끌어당겨 짓이깁니다. 오래 누를수록 넓게 접힙니다",
          "누르고 있으면 접히는 선이 보이고, 떼면 선 양쪽의 적을 선 위로 끌어당겨 짓이깁니다. 오래 누를수록 넓게 접힙니다.",
          "Hold to show the fold line; release to drag enemies on both sides onto the line and crush them. Hold longer for a wider fold.",
          "押している間は折り目が見え、離すと線の両側の敵を線の上へ引き寄せて押し潰します。長く押すほど広く折れます。",
          "按住显示折线，松开后将折线两侧的敌人拉到线上碾碎。按得越久折得越宽。");
        A("측량 완료!", "Surveyed!", "測量完了！", "测量完成！");
        A("깃펜 공격력", "Quill Damage", "羽ペン攻撃力", "羽毛笔攻击力");
        A("깃펜 공격력 · 이동 속도", "Quill Damage · Move Speed", "羽ペン攻撃力・移動速度", "羽毛笔攻击力 · 移动速度");
        K("무한 모드 클리어 (개발자 엄마 처치)", "무한 모드 클리어하기 (개발자 엄마 처치)", "Clear Endless (defeat the Developer's Mom)", "エンドレスをクリア（開発者のママを撃破）", "通关无尽模式（击败开发者的妈妈）");     // 1.0.4~ 클리어 = 엄마 처치

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
        A("접힌 자국 지속", "Crease duration", "折り目の持続", "折痕持续");
        A("측량 표식", "Survey Mark", "測量標識", "测量标记");
        A("획에 맞은 적에게 5초 동안 표식을 남깁니다. 표식이 붙은 적은 닫힌 도형 안에서 피해를 더 받습니다.", "Strokes mark enemies for 5s. Marked enemies take more damage inside closed shapes.", "線に当たった敵に5秒間標識を残します。標識の付いた敵は閉じた図形の中で多くダメージを受けます。", "被笔画击中的敌人留下5秒标记。被标记的敌人在闭合图形内受到更多伤害。");
        A("도형 피해", "Shape damage", "図形ダメージ", "图形伤害");
        A("끊기지 않는 선", "Unbroken Line", "途切れない線", "不断之线");
        A("도형이 커지고(단계마다 +15%), 도형을 닫을 때마다 필살기 게이지가 찹니다.", "Shapes grow larger (+15% per rank), and closing a shape fills your Ultimate gauge.", "図形が大きくなり（段階ごとに+15%）、図形を閉じるたびに必殺技ゲージがたまります。", "图形变大（每级+15%），每次闭合图形都会充能必杀技。");
        A("게이지", "Gauge", "ゲージ", "充能");

        // ---------------- 고유 스킬
        A("범례", "Legend", "凡例", "图例");
        A("도형을 닫을 때마다 4초 동안 주는 모든 피해가 늘어납니다. (최대 3중첩)", "Each closed shape raises all damage you deal for 4s. (Up to 3 stacks)", "図形を閉じるたびに4秒間与えるすべてのダメージが増えます。（最大3重）", "每闭合一个图形，4秒内造成的所有伤害提升。（最多3层）");
        A("등고선", "Contour Lines", "等高線", "等高线");
        A("도형을 닫으면 둘레에서 고리 파동이 한 번 더 퍼집니다.", "Closing a shape sends one more ring wave out from its edge.", "図形を閉じると周りから輪の波動がもう一度広がります。", "闭合图形时，从边缘再扩散一圈环形波动。");
        A("파동 피해", "Wave damage", "波動ダメージ", "波动伤害");
        A("축척", "Scale", "縮尺", "比例尺");
        A("획이 길수록 피해가 늘어나고, 단계마다 획 사거리가 15% 늘어납니다. (사거리 끝까지 그으면 최대)", "Longer strokes deal more damage, and each level extends stroke range by 15%. (Max at full range)", "線が長いほどダメージが増え、段階ごとに線の射程が15%伸びます。（射程いっぱいで最大）", "笔画越长伤害越高，且每级笔画射程增加15%。（画满射程时最大）");
        A("북극성", "North Star", "北極星", "北极星");
        A("지도 접기를 쓰면 접힌 선 가운데에 별이 떠 4초 동안 가까운 적에게 별빛을 쏩니다. (공격력 50%)", "Fold the Map raises a star over the crease that shoots starlight at nearby enemies for 4s. (50% attack)", "地図折りを使うと折り目の中央に星が浮かび、4秒間近くの敵に星の光を撃ちます。（攻撃力50%）", "使用折叠地图时，折线中央升起一颗星，4秒内向附近敌人射出星光。（攻击力50%）");
        A("쏘는 간격", "Fire interval", "発射間隔", "发射间隔");
        A("여백", "Margin", "余白", "留白");
        A("3초 동안 획을 긋지 않으면 다음 획이 두 배 굵고 더 아픕니다.", "If you don't draw for 3s, your next stroke is twice as thick and hits harder.", "3秒間線を引かないと、次の線が2倍太く強くなります。", "3秒内不画线，下一笔会变为两倍粗且更痛。");
        A("여백 획 피해", "Margin stroke damage", "余白の線のダメージ", "留白笔画伤害");
        A("지름길", "Shortcut", "近道", "捷径");
        A("획을 그은 뒤 잠시(1.4초) 이동 속도가 오릅니다.", "Move faster for a moment (1.4s) after each stroke.", "線を引いた後しばらく（1.4秒）移動速度が上がります。", "每次画线后短时间（1.4秒）移动速度提升。");
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
        K("지도 접기로 쓰러진 적에게서 잉크가 튀어 주변 적에게 피해를 줍니다.", "지도 접기로 쓰러진 적에게서 잉크가 튀어 둘레의 적에게 피해를 줍니다.", "Enemies killed by Fold the Map splash ink that damages nearby foes.", "地図折りで倒れた敵からインクが飛び散り、周りの敵にダメージを与えます。", "被折叠地图击败的敌人溅出墨汁，伤害周围敌人。");
        A("잉크 피해", "Ink damage", "インクダメージ", "墨汁伤害");

        // ---------------- 스킬 진화
        A("세계 지도", "World Map", "世界地図", "世界地图");
        A("도형 피해가 1.5배가 되고, 닫힌 뒤 안쪽 적이 두 번 더 다치며 둘레의 적에게 표식(받는 피해 +20%)이 번집니다.", "Shape damage becomes 1.5×; once closed, enemies inside are struck twice more and a mark (+20% damage taken) spreads to nearby foes.", "図形ダメージが1.5倍になり、閉じた後に内側の敵がさらに2回傷つき、周りの敵に標識（被ダメージ+20%）が広がります。", "图形伤害变为1.5倍，闭合后内部敌人再受两次伤害，周围敌人被标记（受到伤害+20%）。");
        A("입체 지도", "Relief Map", "立体地図", "立体地图");
        A("등고선 파동이 세 겹으로 퍼집니다.", "Contour waves ripple out three times.", "等高線の波動が三重に広がります。", "等高线波动扩散三层。");
        A("직행로", "Express Route", "直行路", "直达路线");
        A("획을 그은 뒤 잠시(1.4초) 받는 피해가 30% 줄어듭니다.", "Take 30% less damage for a moment (1.4s) after each stroke.", "線を引いた後しばらく（1.4秒）受けるダメージが30%減ります。", "每次画线后短时间（1.4秒）受到的伤害减少30%。");
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
        K("획 끝에서 가장 가까운 적에게 측량 사슬이 튑니다. (피해 50%)\n궁극기: 삼각 측량", "획 끝에서 가장 가까운 적에게 측량 사슬이 튑니다. (피해 50%)\n필살기에 덧붙음: 삼각 측량", "A survey chain jumps from each stroke's end to the nearest enemy. (50% damage)\nAdded to your Ultimate: Triangulation", "線の端から最も近い敵へ測量の鎖が飛びます。（ダメージ50%）\n必殺技に上乗せ：三角測量", "测量锁链从笔画末端弹向最近的敌人。（伤害50%）\n附加于必杀技：三角测量");
        A("잉크병", "Inkpot", "インク瓶", "墨水瓶");
        A("획 끝에서 잉크병이 깨져 작게 터집니다. (피해 60%)", "An inkpot shatters at each stroke's end in a small burst. (60% damage)", "線の端でインク瓶が割れて小さく爆発します。（ダメージ60%）", "墨水瓶在笔画末端碎裂小范围爆炸。（伤害60%）");
        K("획 끝에서 잉크병이 깨져 작게 터집니다. (피해 60%)\n궁극기: 먹물 해일", "획 끝에서 잉크병이 깨져 작게 터집니다. (피해 60%)\n필살기에 덧붙음: 먹물 해일", "An inkpot shatters at each stroke's end in a small burst. (60% damage)\nAdded to your Ultimate: Ink Tide", "線の端でインク瓶が割れて小さく爆発します。（ダメージ60%）\n必殺技に上乗せ：墨の津波", "墨水瓶在笔画末端碎裂小范围爆炸。（伤害60%）\n附加于必杀技：墨潮");
        A("지구본", "Globe", "地球儀", "地球仪");
        K("도형이 1.5배 커지고, 닫으면 안쪽 적이 가운데로 모이며 도형 피해 +30%.", "도형이 1.5배 커지고, 닫으면 안쪽 적이 가운데로 모이며 도형 피해가 30% 늘어납니다.", "Shapes grow 1.5× larger; closing one pulls enemies to the center and raises shape damage by 30%.", "図形が1.5倍大きくなり、閉じると内側の敵が中央に集まり、図形ダメージが30%増えます。", "图形变为1.5倍大，闭合时内部敌人聚向中央，图形伤害提升30%。");
        K("도형이 1.5배 커지고, 닫으면 안쪽 적이 가운데로 모이며 도형 피해 +30%.\n궁극기: 자전", "도형이 1.5배 커지고, 닫으면 안쪽 적이 가운데로 모이며 도형 피해가 30% 늘어납니다.\n필살기에 덧붙음: 자전", "Shapes grow 1.5× larger; closing one pulls enemies to the center and raises shape damage by 30%.\nAdded to your Ultimate: Rotation", "図形が1.5倍大きくなり、閉じると内側の敵が中央に集まり、図形ダメージが30%増えます。\n必殺技に上乗せ：自転", "图形变为1.5倍大，闭合时内部敌人聚向中央，图形伤害提升30%。\n附加于必杀技：自转");
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
        K("하지만 지도 끝 붉게 칠해진 사막에, 아직 그리지 못한 무언가가 꿈틀거리고 있었다...", "하지만 지도 끝 붉게 칠해진 사막에, 아직 그리지 못한 무언가가 꿈틀거리고 있었다…", "But in the red desert at the map's edge, something not yet drawn was stirring...", "しかし地図の端、赤く塗られた砂漠で、まだ描かれていない何かがうごめいていた……", "然而在地图边缘那片涂红的沙漠里，某个尚未画下的东西正在蠢动……");

        // ================================================================= 시계공
        A("시계공", "Clockmaker", "時計職人", "钟表匠");
        A("멈춘 시간의 장인", "Master of Stopped Time", "止まった時の職人", "停滞时间的工匠");
        K("톱니바퀴와 태엽으로 시간을 다루는 장인. 맞힐수록 시계 눈금이 차 정각의 큰 톱니를 던지고, 위기에는 3초 전으로 되돌아갑니다.",
          "톱니바퀴와 태엽으로 시간을 다루는 장인입니다. 맞힐수록 시계 눈금이 차 정각의 큰 톱니를 던지고, 위기에는 3초 전으로 되돌아갑니다.",
          "A craftsman who bends time with gears and springs. Hits fill the clock until a great o'clock gear flies, and in a pinch he rewinds 3 seconds.",
          "歯車とゼンマイで時を操る職人。当てるほど時計の目盛りがたまり正時の大歯車を投げ、ピンチには3秒前へ巻き戻ります。",
          "以齿轮与发条操纵时间的工匠。命中越多钟表刻度越满，便会掷出整点大齿轮；危急时倒回3秒前。");
        A("톱니바퀴", "Gears", "歯車", "齿轮");
        K("회전하는 톱니를 던집니다. 맞힐 때마다 발밑 시계 눈금이 차고, 다 차면 다음 톱니가 모든 적을 꿰뚫고 터지는 「정각」 톱니가 됩니다",
          "회전하는 톱니를 던집니다. 맞힐 때마다 발밑 시계 눈금이 차고, 다 차면 다음 톱니가 모든 적을 꿰뚫고 터지는 「정각」 톱니가 됩니다.",
          "Throw spinning gears. Each hit fills the clock ring at your feet; when full, your next gear becomes an O'Clock gear that pierces everything and explodes.",
          "回転する歯車を投げます。当てるたびに足元の時計の目盛りがたまり、満タンになると次の歯車がすべての敵を貫いて爆発する「正時」の歯車になります。",
          "掷出旋转齿轮。每次命中都会填满脚下的钟表刻度，填满后下一枚齿轮变为贯穿所有敌人并爆炸的「整点」齿轮。");
        A("되감기", "Rewind", "巻き戻し", "倒带");
        K("누르고 있으면 3초 전의 내 모습이 보이고, 떼면 그 자리로 돌아갑니다. 지나온 길의 적을 베고, 그동안 잃은 체력의 절반을 되찾으며, 잃은 체력의 세 배를 둘레 적에게 되갚습니다",
          "누르고 있으면 3초 전의 내 모습이 보이고, 떼면 그 자리로 돌아갑니다. 지나온 길의 적을 베고, 그동안 잃은 체력의 절반을 되찾으며, 잃은 체력의 세 배를 둘레의 적에게 되갚습니다.",
          "Hold to see yourself from 3 seconds ago; release to return there. Cut enemies along the way, recover half the health lost, and repay three times that loss to nearby foes.",
          "押している間は3秒前の自分が見え、離すとその場所へ戻ります。通った道の敵を斬り、その間に失った体力の半分を取り戻し、失った体力の3倍を周りの敵に返します。",
          "按住可看到3秒前的自己，松开回到那里。斩击途经的敌人，夺回期间失去生命的一半，并将失去生命的三倍回敬给周围敌人。");
        A("정각!", "O'Clock!", "正時！", "整点！");
        A("시간 정지!", "Time Stop!", "時間停止！", "时间停止！");
        A("톱니 공격력", "Gear Damage", "歯車攻撃力", "齿轮攻击力");
        A("톱니 공격력 · 이동 속도", "Gear Damage · Move Speed", "歯車攻撃力・移動速度", "齿轮攻击力 · 移动速度");

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
        K("정각 톱니가 터지면 종소리가 퍼져 주변 적을 잠깐 멈춥니다.", "정각 톱니가 터지면 종소리가 퍼져 둘레의 적을 잠깐 멈춥니다.", "When an O'Clock gear bursts, a bell toll briefly freezes nearby enemies.", "正時の歯車が爆発すると鐘の音が広がり、周りの敵を少し止めます。", "整点齿轮爆炸时钟声扩散，使周围敌人短暂停住。");
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
        K("적을 처치할 때마다 필살기 게이지가 찹니다.", "적을 쓰러뜨릴 때마다 필살기 게이지가 찹니다.", "Each kill fills your Ultimate gauge.", "敵を倒すたびに必殺技ゲージがたまります。", "每击杀一名敌人都会充能必杀技。");
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
        A("스톱워치가 두 배로 들어가고, 되감을 때마다 필살기 게이지를 30% 돌려받습니다.", "Stopwatch replays twice as much damage, and each rewind refunds 30% of your Ultimate gauge.", "ストップウォッチのダメージが2倍になり、巻き戻すたびに必殺技ゲージを30%取り戻します。", "秒表效果翻倍，每次倒带返还30%必杀技充能。");

        // ---------------- 무기 진화 · 궁극기 · 강화 · 패시브
        A("뻐꾸기 시계", "Cuckoo Clock", "鳩時計", "布谷鸟钟");
        A("정각 톱니가 터질 때마다 뻐꾸기 셋이 튀어나와 가까운 적을 쪼습니다. (공격력 100%씩)", "Each O'Clock gear burst releases three cuckoos that peck nearby enemies. (100% attack each)", "正時の歯車が爆発するたびにカッコウが3羽飛び出して近くの敵をつつきます。（攻撃力100%ずつ）", "每次整点齿轮爆炸都会弹出三只布谷鸟啄击附近敌人。（每只攻击力100%）");
        K("정각 톱니가 터질 때마다 뻐꾸기 셋이 튀어나와 가까운 적을 쪼습니다.\n궁극기: 뻐꾸기 떼", "정각 톱니가 터질 때마다 뻐꾸기 셋이 튀어나와 가까운 적을 쪼습니다.\n필살기에 덧붙음: 뻐꾸기 떼", "Each O'Clock gear burst releases three cuckoos that peck nearby enemies.\nAdded to your Ultimate: Cuckoo Flock", "正時の歯車が爆発するたびにカッコウが3羽飛び出して近くの敵をつつきます。\n必殺技に上乗せ：カッコウの群れ", "每次整点齿轮爆炸都会弹出三只布谷鸟啄击附近敌人。\n附加于必杀技：布谷鸟群");
        A("모래시계", "Hourglass", "砂時計", "沙漏");
        A("톱니에 맞은 적이 모래에 묻혀 1초 동안 절반 속도로 느려집니다.", "Enemies hit by gears are buried in sand, moving at half speed for 1s.", "歯車に当たった敵が砂に埋もれ、1秒間半分の速さに遅くなります。", "被齿轮击中的敌人被沙子掩埋，1秒内速度减半。");
        K("톱니에 맞은 적이 모래에 묻혀 1초 동안 절반 속도로 느려집니다.\n궁극기: 모래 폭풍", "톱니에 맞은 적이 모래에 묻혀 1초 동안 절반 속도로 느려집니다.\n필살기에 덧붙음: 모래 폭풍", "Enemies hit by gears are buried in sand, moving at half speed for 1s.\nAdded to your Ultimate: Sandstorm", "歯車に当たった敵が砂に埋もれ、1秒間半分の速さに遅くなります。\n必殺技に上乗せ：砂嵐", "被齿轮击中的敌人被沙子掩埋，1秒内速度减半。\n附加于必杀技：沙暴");
        A("괘종시계", "Grandfather Clock", "柱時計", "落地钟");
        A("톱니가 크고 느려지지만 피해가 40% 늘어납니다.", "Gears grow larger and slower but deal 40% more damage.", "歯車が大きく遅くなりますが、ダメージが40%増えます。", "齿轮变大变慢，但伤害提升40%。");
        K("톱니가 크고 느려지지만 피해가 40% 늘어납니다.\n궁극기: 열두 번의 종", "톱니가 크고 느려지지만 피해가 40% 늘어납니다.\n필살기에 덧붙음: 열두 번의 종", "Gears grow larger and slower but deal 40% more damage.\nAdded to your Ultimate: Twelve Tolls", "歯車が大きく遅くなりますが、ダメージが40%増えます。\n必殺技に上乗せ：十二の鐘", "齿轮变大变慢，但伤害提升40%。\n附加于必杀技：十二响钟");
        A("뻐꾸기 떼", "Cuckoo Flock", "カッコウの群れ", "布谷鸟群");
        A("뻐꾸기 열두 마리가 가까운 적들을 차례로 덮칩니다. (공격력 150%씩)", "Twelve cuckoos swoop on nearby enemies in turn. (150% attack each)", "カッコウ12羽が近くの敵へ次々と襲いかかります。（攻撃力150%ずつ）", "十二只布谷鸟依次扑向附近敌人。（每只攻击力150%）");
        A("모래 폭풍", "Sandstorm", "砂嵐", "沙暴");
        K("마우스 자리에 4초 동안 모래 회오리가 생겨 적을 끌어당기고 0.3초마다 벱니다. (공격력 60%)", "마우스 위치에 4초 동안 모래 회오리가 생겨 적을 끌어당기고 0.3초마다 벱니다. (공격력 60%)", "A sand whirlwind forms at the cursor for 4s, pulling enemies in and cutting every 0.3s. (60% attack)", "マウスの位置に4秒間砂の竜巻が生まれ、敵を引き寄せて0.3秒ごとに斬ります。（攻撃力60%）", "在鼠标处生成持续4秒的沙之旋风，吸引敌人并每0.3秒斩击。（攻击力60%）");
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
        K("적을 처치하면 8% 확률로 다음 톱니가 정각 톱니가 됩니다.\n진화: 15%", "적을 쓰러뜨리면 8% 확률로 다음 톱니가 정각 톱니가 됩니다.\n진화: 15%", "Kills have an 8% chance to make your next gear an O'Clock gear.\nEvolved: 15%", "敵を倒すと8%の確率で次の歯車が正時の歯車になります。\n進化：15%", "击杀敌人时有8%几率使下一枚齿轮成为整点齿轮。\n进化：15%");
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
        A("편지", "Letters", "手紙", "信件");
        A("속달", "Express Delivery", "速達", "特快专递");
        K("누르고 있는 동안 둘레의 적에게 편지 표식이 차례로 붙고 (오래 누를수록 많이), 떼면 1초 뒤 표식이 모두 한꺼번에 터집니다",
          "누르고 있는 동안 둘레의 적에게 편지 표식이 차례로 붙고(오래 누를수록 많이), 떼면 1초 뒤 표식이 모두 한꺼번에 터집니다.",
          "While held, letter marks attach to nearby enemies one by one (more the longer you hold); release and all marks burst together 1s later.",
          "押している間、周りの敵に手紙の印が順に付き（長く押すほど多く）、離すと1秒後に印がすべて一斉に弾けます。",
          "按住时依次在周围敌人身上贴上信件标记（按得越久越多），松开1秒后所有标记同时爆炸。");
        A("배달 완료!", "Delivered!", "配達完了！", "送达！");
        A("편지 공격력", "Letter Damage", "手紙攻撃力", "信件攻击力");
        A("편지 공격력 · 이동 속도", "Letter Damage · Move Speed", "手紙攻撃力・移動速度", "信件攻击力 · 移动速度");
        A("통", "", "通", "封");

        // ---------------- 전용 레벨업 카드
        A("등기 우편", "Registered Mail", "書留", "挂号信");
        A("다시 배달", "Redeliveries", "再配達", "再次投递");
        A("소포", "Parcel", "小包", "包裹");
        A("편지가 닿은 자리에서 작게 터집니다. (피해 50%)", "Letters burst on arrival. (50% damage)", "手紙が届いた場所で小さく爆発します。（ダメージ50%）", "信件送达处小范围爆炸。（伤害50%）");
        A("폭발 범위", "Blast radius", "爆発範囲", "爆炸范围");
        A("우표 수집", "Stamp Collecting", "切手集め", "集邮");
        A("답장", "Reply", "返信", "回信");
        A("답장 수", "Replies", "返信の数", "回信数");
        A("빠른 우편", "Quick Post", "速い郵便", "快邮");
        A("편지가 더 빠르게 날아가고 더 날카롭게 꺾입니다.", "Letters fly faster and turn more sharply.", "手紙がより速く飛び、より鋭く曲がります。", "信件飞得更快，转向更灵敏。");
        A("속도 · 꺾임", "Speed · Turning", "速度・旋回", "速度 · 转向");
        A("반송", "Return to Sender", "返送", "退回");
        A("받는 이가 쓰러지면 편지가 다른 적에게 한 번 더 날아갑니다.", "If the addressee falls, the letter flies on to another enemy.", "宛先が倒れると、手紙がもう一度別の敵へ飛びます。", "收件人倒下时，信件再飞向另一名敌人。");
        A("반송 피해", "Return damage", "返送ダメージ", "退回伤害");

        // ---------------- 고유 스킬
        A("특급", "Priority Mail", "特急", "特急");
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
        A("답장 폭주", "Reply Flood", "返信の嵐", "回信潮");
        A("익일 배송이 터질 때도 답장 편지가 날아갑니다.", "Next-Day Delivery blasts also send reply letters.", "翌日配送が弾けるときも返信の手紙が飛びます。", "次日达爆炸时也会飞出回信。");
        A("총알 배송", "Bullet Delivery", "弾丸配送", "子弹快递");
        A("움직이는 동안 편지를 한 통 더 보냅니다.", "Send one more letter while moving.", "動いている間、手紙をもう1通送ります。", "移动中多寄出一封信。");
        A("반송 불가", "No Returns", "返送不可", "拒绝退回");
        A("전단 폭탄", "Flyer Bomb", "チラシ爆弾", "传单炸弹");
        A("광고 전단이 맞은 자리에서 터집니다. (공격력 60%)", "Junk Mail flyers explode on hit. (60% attack)", "広告チラシが当たった場所で爆発します。（攻撃力60%）", "广告传单命中处爆炸。（攻击力60%）");

        // ---------------- 무기 진화 · 궁극기 · 강화 · 패시브
        A("비둘기 우편", "Pigeon Post", "鳩便", "信鸽");
        A("첫 편지는 비둘기가 물고 가 받는 이를 두 번 더 쪼습니다. (피해 50%씩)", "A pigeon carries your first letter and pecks the addressee twice more. (50% damage each)", "最初の手紙は鳩がくわえて運び、宛先をさらに2回つつきます。（ダメージ50%ずつ）", "第一封信由信鸽衔去，再啄收件人两次。（每次伤害50%）");
        K("첫 편지는 비둘기가 물고 가 받는 이를 두 번 더 쪼습니다.\n궁극기: 비둘기 떼", "첫 편지는 비둘기가 물고 가 받는 이를 두 번 더 쪼습니다.\n필살기에 덧붙음: 비둘기 떼", "A pigeon carries your first letter and pecks the addressee twice more.\nAdded to your Ultimate: Pigeon Flock", "最初の手紙は鳩がくわえて運び、宛先をさらに2回つつきます。\n必殺技に上乗せ：鳩の群れ", "第一封信由信鸽衔去，再啄收件人两次。\n附加于必杀技：鸽群");
        A("소포 상자", "Parcel Box", "小包箱", "包裹箱");
        A("편지가 소포 상자가 되어 맞은 자리에서 터집니다. (피해 60%)", "Letters become parcel boxes that burst on arrival. (60% damage)", "手紙が小包箱になり、届いた場所で爆発します。（ダメージ60%）", "信件变为包裹箱，送达处爆炸。（伤害60%）");
        K("편지가 소포 상자가 되어 맞은 자리에서 터집니다.\n궁극기: 택배 폭격", "편지가 소포 상자가 되어 맞은 자리에서 터집니다.\n필살기에 덧붙음: 택배 폭격", "Letters become parcel boxes that burst on arrival.\nAdded to your Ultimate: Parcel Barrage", "手紙が小包箱になり、届いた場所で爆発します。\n必殺技に上乗せ：宅配爆撃", "信件变为包裹箱，送达处爆炸。\n附加于必杀技：快递轰炸");
        A("우체통", "Postbox", "郵便ポスト", "邮筒");
        A("3초마다 발밑에 우체통이 서서 4초 동안 가까운 적에게 편지를 던집니다.", "Every 3s a postbox appears at your feet and throws letters at nearby enemies for 4s.", "3秒ごとに足元に郵便ポストが立ち、4秒間近くの敵へ手紙を投げます。", "每3秒脚下立起一个邮筒，4秒内向附近敌人投掷信件。");
        K("3초마다 발밑에 우체통이 서서 4초 동안 가까운 적에게 편지를 던집니다.\n궁극기: 우체통 행렬", "3초마다 발밑에 우체통이 서서 4초 동안 가까운 적에게 편지를 던집니다.\n필살기에 덧붙음: 우체통 행렬", "Every 3s a postbox appears at your feet and throws letters at nearby enemies for 4s.\nAdded to your Ultimate: Postbox Parade", "3秒ごとに足元に郵便ポストが立ち、4秒間近くの敵へ手紙を投げます。\n必殺技に上乗せ：ポストの行列", "每3秒脚下立起一个邮筒，4秒内向附近敌人投掷信件。\n附加于必杀技：邮筒行列");
        A("비둘기 떼", "Pigeon Flock", "鳩の群れ", "鸽群");
        A("비둘기 스무 마리가 마우스 쪽으로 날아가며 닿는 적을 쪼습니다. (공격력 90%씩)", "Twenty pigeons fly toward the cursor, pecking enemies they touch. (90% attack each)", "鳩20羽がマウスの方へ飛び、触れた敵をつつきます。（攻撃力90%ずつ）", "二十只鸽子飞向鼠标方向，啄击触碰的敌人。（每只攻击力90%）");
        A("택배 폭격", "Parcel Barrage", "宅配爆撃", "快递轰炸");
        A("마우스 둘레에 상자 열 개가 차례로 떨어져 터집니다. (공격력 180%씩)", "Ten boxes drop around the cursor in turn and explode. (180% attack each)", "マウスの周りに箱10個が次々と落ちて爆発します。（攻撃力180%ずつ）", "十个箱子依次落在鼠标周围并爆炸。（每个攻击力180%）");
        A("우체통 행렬", "Postbox Parade", "ポストの行列", "邮筒行列");
        A("내 둘레에 우체통 다섯 개가 서서 4초 동안 가까운 적에게 편지를 던집니다.", "Five postboxes rise around you and throw letters at nearby enemies for 4s.", "自分の周りに郵便ポスト5つが立ち、4秒間近くの敵へ手紙を投げます。", "身边立起五个邮筒，4秒内向附近敌人投掷信件。");
        A("두꺼운 봉투", "Thick Envelope", "分厚い封筒", "厚信封");
        A("편지 피해가 25% 늘어납니다.", "Letter damage increases by 25%.", "手紙のダメージが25%増えます。", "信件伤害提升25%。");
        A("다발 배송", "Bundle Delivery", "まとめ配送", "批量投递");
        A("등기 표식", "Registered Marks", "書留の印", "挂号标记");
        A("속달 표식이 50% 더 많이 붙습니다.", "Express Delivery places 50% more marks.", "速達の印が50%多く付きます。", "特快标记数量增加50%。");
        A("튼튼한 다리", "Sturdy Legs", "丈夫な脚", "强健双腿");
        A("이동 속도가 8% 빨라집니다.\n진화: 14%", "Move speed increases by 8%.\nEvolved: 14%", "移動速度が8%速くなります。\n進化：14%", "移动速度提升8%。\n进化：14%");
        A("단골 손님", "Regular Customer", "常連さん", "老主顾");
        A("보스 · 중간 보스에게 주는 피해가 15% 늘어납니다.\n진화: +25%", "Deal 15% more damage to bosses and mid-bosses.\nEvolved: +25%", "ボス・中ボスへのダメージが15%増えます。\n進化：+25%", "对首领与中头目造成的伤害提升15%。\n进化：+25%");
        A("우편 요금", "Postage", "郵便料金", "邮资");
        A("배달을 마칠 때마다 5% 확률로 코인 1개를 받습니다.\n진화: 10%", "Each delivery has a 5% chance to earn 1 coin.\nEvolved: 10%", "配達を終えるたびに5%の確率でコイン1枚をもらいます。\n進化：10%", "每次投递有5%几率获得1枚金币。\n进化：10%");
        A("개 조심", "Beware of Dog", "猛犬注意", "当心恶犬");
        A("둘레 3칸에 적이 넷 이상이면 받는 피해가 10% 줄어듭니다.\n진화: 18%", "Take 10% less damage while four or more enemies are within 3 tiles.\nEvolved: 18%", "周囲3マスに敵が4体以上いると受けるダメージが10%減ります。\n進化：18%", "周围3格内有四名以上敌人时受到的伤害减少10%。\n进化：18%");

        // 1.0.6 우편배달부 하향 (바뀐 수치 · 동작에 맞춘 설명 — 옛 문구 줄은 지움)
        K("살아 있는 자에게도 죽은 자에게도 편지를 전하는 배달부. 편지는 가장 먼 적부터 찾아가고, 속달 한 번이면 둘레의 적 여럿에게 편지가 한꺼번에 터집니다.",
          "살아 있는 자에게도 죽은 자에게도 편지를 전하는 배달부입니다. 편지는 가장 먼 적부터 찾아가고, 속달 한 번이면 둘레의 적 여럿에게 편지가 한꺼번에 터집니다.",
          "A courier who delivers to the living and the dead alike. Letters seek out the farthest foe first, and one Express Delivery bursts letters on many nearby enemies at once.",
          "生者にも死者にも手紙を届ける配達人。手紙は最も遠い敵から探しに行き、速達ひとつで周りの多くの敵に手紙が一斉に弾けます。",
          "无论生者死者都会送达信件的邮差。信件先寻找最远的敌人，一次特快便能让周围众多敌人身上的信件同时爆开。");
        K("마우스 쪽에서 가장 먼 적을 받는 이로 정해 편지를 날립니다. 편지는 다른 적을 지나쳐 받는 이를 쫓아가고, 직접 보낸 편지를 열 번 배달할 때마다 코인과 필살기 게이지를 받습니다",
          "마우스 쪽에서 가장 먼 적을 받는 이로 정해 편지를 날립니다. 편지는 다른 적을 지나쳐 받는 이를 쫓아가고, 직접 보낸 편지를 열 번 배달할 때마다 코인과 필살기 게이지를 받습니다.",
          "Address a letter to the farthest enemy toward the cursor. It slips past other foes to chase its addressee, and every 10 deliveries of your own letters earn coins and Ultimate gauge.",
          "マウスの方で最も遠い敵を宛先にして手紙を飛ばします。手紙は他の敵をすり抜けて宛先を追い、自分で送った手紙を10回配達するたびにコインと必殺技ゲージがもらえます。",
          "以鼠标方向最远的敌人为收件人掷出信件。信件越过其他敌人追踪收件人，亲手寄出的信件每送达10次获得金币与必杀技充能。");
        A("편지가 받는 이에게 닿으면 가까운 다른 적에게 다시 배달됩니다. (피해 65%)", "After reaching its addressee, a letter is redelivered to another nearby enemy. (65% damage)", "手紙が宛先に届くと、近くの別の敵へもう一度配達されます。（ダメージ65%）", "信件送达收件人后，再投递给附近另一名敌人。（伤害65%）");
        A("배달할 때마다 다음 편지의 피해가 늘어납니다. (최대 10중첩 · 맞으면 절반이 떨어짐)", "Each delivery strengthens your next letters. (Up to 10 stacks · getting hit loses half)", "配達するたびに次の手紙のダメージが増えます。（最大10重・被弾すると半分なくなる）", "每次送达都会强化之后的信件。（最多10层 · 受击时失去一半）");
        A("속달 표식이 터진 자리에서 작은 편지가 가까운 적에게 날아갑니다. (공격력 60% · 속달 한 번에 최대 10통)", "Where Express marks burst, small letters fly to nearby enemies. (60% attack · up to 10 per Express Delivery)", "速達の印が弾けた場所から小さな手紙が近くの敵へ飛びます。（攻撃力60%・速達1回につき最大10通）", "特快标记爆炸处飞出小信件射向附近敌人。（攻击力60% · 每次特快最多10封）");
        A("직접 보낸 편지를 열 번 배달할 때마다 필살기 게이지가 더 찹니다.", "Every 10 deliveries of your own letters fill extra Ultimate gauge.", "自分で送った手紙を10回配達するたびに必殺技ゲージがさらにたまります。", "亲手寄出的信件每送达10次额外充能必杀技。");
        A("배달을 마칠 때마다 체력을 회복합니다. (0.3초에 한 번)", "Heal each time you complete a delivery. (Once per 0.3s)", "配達を終えるたびに体力を回復します。（0.3秒に1回）", "每完成一次投递恢复生命。（每0.3秒一次）");
        A("우표가 15중첩까지 쌓이고, 특급 게이지가 1.5배가 됩니다.", "Stamps stack up to 15, and Priority Mail grants 1.5× gauge.", "切手が15重まで重なり、特急のゲージが1.5倍になります。", "邮票可叠加至15层，特急充能变为1.5倍。");
        A("반송된 편지가 최대 두 번까지 계속 이어집니다.", "Returned letters keep going up to two times.", "返送された手紙が最大2回まで続きます。", "退回的信件最多可连续两次。");
        A("편지를 두 통 더 보냅니다. (피해 55%)", "Send two more letters. (55% damage)", "手紙をさらに2通送ります。（ダメージ55%）", "多寄出两封信。（伤害55%）");

        // ---------------- 이야기
        A("저승 배달부의 가방에 수취인이 「킹 슬라임」인 편지가 한 통 들어 있었다.", "In the courier's bag was a single letter addressed to \"King Slime\".", "冥府の配達人のかばんに、宛先が「キングスライム」の手紙が1通入っていた。", "冥府信使的包里，有一封收件人写着「史莱姆王」的信。");
        A("보낸 이는 초원에서 녹아 사라진 사람들. 내용은 짧았다. 「돌려줘.」", "It came from everyone dissolved in the meadow. It was short: \"Give us back.\"", "差出人は草原で溶けて消えた人々。中身は短かった。「返して。」", "寄信人是在草原上被溶化消失的人们。内容很短：「还给我们。」");
        A("배달부는 모자를 고쳐 쓰고 초원으로 걸어 나갔다. 편지는 반드시 전해진다.", "The courier straightened his cap and walked into the meadow. Every letter gets delivered.", "配達人は帽子をかぶり直して草原へ歩き出した。手紙は必ず届けられる。", "信使正了正帽子，走向草原。信件一定会送达。");
        A("저승 배달부의 가방에 수취인이 「리치 왕」인 편지가 한 통 들어 있었다.", "In the courier's bag was a single letter addressed to \"the Lich King\".", "冥府の配達人のかばんに、宛先が「リッチ王」の手紙が1通入っていた。", "冥府信使的包里，有一封收件人写着「巫妖王」的信。");
        A("보낸 이는 묘역에 잠들어 있던 모든 영혼. 내용은 짧았다. 「이제 그만 재워 줘.」", "It came from every soul that slept in the catacombs. It was short: \"Let us rest now.\"", "差出人は墓所で眠っていたすべての魂。中身は短かった。「もう眠らせて。」", "寄信人是长眠在墓地的所有灵魂。内容很短：「让我们安息吧。」");
        A("배달부는 모자를 고쳐 쓰고 신전의 구멍으로 내려갔다. 편지는 반드시 전해진다.", "The courier straightened his cap and climbed down into the temple's hole. Every letter gets delivered.", "配達人は帽子をかぶり直して神殿の穴へ降りていった。手紙は必ず届けられる。", "信使正了正帽子，下到神殿的洞里。信件一定会送达。");
        A("배달부의 가방이 텅 비었다. 모든 편지가 제 주인에게 닿았다.", "The courier's bag was empty. Every letter had reached its owner.", "配達人のかばんが空になった。すべての手紙が持ち主に届いた。", "信使的包空了。所有信件都已送到主人手中。");
        A("떠오르는 영혼들이 하나씩 배달부에게 손을 흔들었다. 답장 대신이었다.", "One by one, the rising souls waved to the courier. That was their reply.", "昇っていく魂たちが一人ずつ配達人に手を振った。返事の代わりだった。", "升起的灵魂一个个向信使挥手。那就是它们的回信。");
        A("“배달 완료.”", "\"Delivered.\"", "「配達完了。」", "“送达。”");
        K("하지만 가방 바닥에서, 붉은 사막의 소인이 찍힌 편지 한 통이 바스락거렸다...", "하지만 가방 바닥에서, 붉은 사막의 소인이 찍힌 편지 한 통이 바스락거렸다…", "But at the bottom of the bag, a letter postmarked from the red desert rustled...", "しかしかばんの底で、赤い砂漠の消印が押された手紙が1通かさかさと音を立てた……", "然而包底，一封盖着红色沙漠邮戳的信沙沙作响……");

        // ================================================================= 유령
        A("유령", "Ghost", "幽霊", "幽灵");
        A("죽지 못한 영혼", "The Soul That Lingers", "死にきれない魂", "无法安息之魂");
        K("세상을 떠나지 못한 영혼. 차가운 손길로 적을 얼리고, 쓰러뜨린 적의 영혼을 몸 둘레에 거느려 피해를 대신 받게 하며, 적의 몸에 빙의해 안에서 터뜨립니다.",
          "세상을 떠나지 못한 영혼입니다. 차가운 손길로 적을 얼리고, 쓰러뜨린 적의 영혼을 몸 둘레에 거느려 피해를 대신 받게 하며, 적의 몸에 빙의해 안에서 터뜨립니다.",
          "A soul that could not leave this world. Its cold touch freezes foes, the souls of the fallen orbit it and take hits in its place, and it possesses enemies to burst them from within.",
          "この世を去れない魂。冷たい手で敵を凍らせ、倒した敵の魂を周りにまとってダメージを肩代わりさせ、敵の体に憑依して内側から破裂させます。",
          "无法离开人世的灵魂。以冰冷之手冻结敌人，让被击败敌人的灵魂环绕身边代为承受伤害，并附身于敌人体内将其从内部引爆。");
        A("차가운 손길", "Cold Touch", "冷たい手", "冰冷之手");
        K("앞으로 짧게 뻗는 손이 한 줄의 적을 모두 꿰뚫고 냉기를 쌓습니다 (다섯 번 쌓이면 얼어붙음). 적을 쓰러뜨리면 확률로 영혼이 생겨 몸 둘레를 돌다가, 맞을 때 체력 대신 영혼이 사라지며 피해를 막습니다",
          "앞으로 짧게 뻗는 손이 한 줄의 적을 모두 꿰뚫고 냉기를 쌓습니다. 냉기가 다섯 번 쌓이면 얼어붙습니다. 적을 쓰러뜨리면 확률로 영혼이 생겨 몸 둘레를 돌다가, 맞을 때 체력 대신 영혼이 사라지며 피해를 막습니다.",
          "A hand lunges a short way forward, piercing a line of enemies and building chill. Five stacks freeze them. Kills may spawn souls that orbit you; when you're hit, a soul vanishes instead of your health.",
          "前へ短く伸びる手が一列の敵をすべて貫き、冷気をためます。5回たまると凍結します。敵を倒すと確率で魂が生まれて周りを回り、被弾すると体力の代わりに魂が消えてダメージを防ぎます。",
          "向前短伸的手贯穿一整列敌人并累积寒气。叠满五层即会冻结。击败敌人时有几率生成环绕身边的灵魂，受击时由灵魂代替生命消散以抵挡伤害。");
        A("빙의", "Possession", "憑依", "附身");
        // 1.0.4 요청 ghost (빙의 1초 → 3초 · 들어간 동안 그 적의 이동 속도로 조종)
        K("누르고 있으면 마우스 가까운 적에 표시가 붙고, 떼면 그 적의 몸에 들어가 3초 동안 그 적의 이동 속도로 몸을 끌고 다니다가 안에서 터뜨립니다. 들어간 동안은 무적이고, 둘레 적에게도 피해와 냉기를 줍니다",
          "누르고 있으면 마우스에 가까운 적에게 표시가 붙고, 떼면 그 적의 몸에 들어가 3초 동안 그 적의 이동 속도로 몸을 끌고 다니다가 안에서 터뜨립니다. 들어간 동안은 무적이고, 둘레의 적에게도 피해와 냉기를 줍니다.",
          "Hold to mark the enemy nearest the cursor; release to slip into its body, steer it around at its own move speed for 3s, then burst it from within. You're invulnerable inside, and nearby enemies take damage and chill too.",
          "押している間はマウス近くの敵に印が付き、離すとその敵の体に入り、3秒間その敵の移動速度で体を操って動き回った後、内側から破裂させます。入っている間は無敵で、周りの敵にもダメージと冷気を与えます。",
          "按住标记鼠标附近的敌人，松开后潜入其体内，以该敌人的移动速度操控其身体行动3秒，随后从内部引爆。潜入期间无敌，并对周围敌人造成伤害与寒气。");
        A("명계의 문!", "Gate of the Dead!", "冥界の門！", "冥界之门！");
        A("손길 공격력", "Touch Damage", "手の攻撃力", "冰手攻击力");
        A("손길 공격력 · 이동 속도", "Touch Damage · Move Speed", "手の攻撃力・移動速度", "冰手攻击力 · 移动速度");

        // ---------------- 전용 레벨업 카드
        A("꿰뚫는 냉기", "Piercing Chill", "貫く冷気", "穿透寒气");
        A("손길이 더 멀리 뻗습니다.", "Your touch reaches farther.", "手がより遠くまで伸びます。", "冰手伸得更远。");
        A("손길 길이", "Reach", "手の長さ", "伸展距离");
        A("얼어붙는 손길", "Freezing Touch", "凍てつく手", "冻结之手");
        A("얼어붙기까지 필요한 냉기가 줄어듭니다.", "Fewer chill stacks are needed to freeze.", "凍結までに必要な冷気が減ります。", "冻结所需的寒气层数减少。");
        A("필요한 냉기", "Chill needed", "必要な冷気", "所需寒气");
        A("원혼", "Vengeful Spirit", "怨霊", "冤魂");
        A("적이 쓰러지면 그 영혼이 가까운 적에게 날아가 부딪힙니다.", "When an enemy falls, its soul flies into a nearby enemy.", "敵が倒れるとその魂が近くの敵へ飛んでぶつかります。", "敌人倒下时，其灵魂飞向附近敌人撞击。");
        A("원혼 피해", "Spirit damage", "怨霊ダメージ", "冤魂伤害");
        A("빙의 여운", "Lingering Possession", "憑依の余韻", "附身余韵");
        A("빙의로 터진 자리에 냉기 안개가 남아 안의 적에게 냉기를 쌓습니다.", "Possession leaves a chill mist that builds chill on enemies inside.", "憑依で破裂した場所に冷気の霧が残り、中の敵に冷気をためます。", "附身爆炸处留下寒雾，为其中的敌人累积寒气。");
        A("안개 지속", "Mist duration", "霧の持続", "寒雾持续");
        A("영혼 수호", "Soul Guard", "魂の守り", "灵魂守护");
        A("몸 둘레에 거느릴 수 있는 영혼이 늘어납니다.", "You can keep more souls around you.", "周りにまとえる魂が増えます。", "可环绕身边的灵魂数量增加。");
        A("최대 영혼", "Max souls", "最大の魂", "最大灵魂");
        A("얼음 파편", "Ice Shards", "氷の破片", "冰之碎片");
        A("얼어붙은 적이 쓰러지면 얼음 파편이 사방으로 튑니다. (공격력 50%)", "Frozen enemies shatter into shards in all directions when they fall. (50% attack)", "凍った敵が倒れると氷の破片が四方へ飛び散ります。（攻撃力50%）", "被冻结的敌人倒下时，冰之碎片向四方飞溅。（攻击力50%）");

        // ---------------- 고유 스킬
        A("원한", "Grudge", "怨み", "怨恨");
        A("냉기가 쌓인 적에게 주는 피해가 쌓인 냉기만큼 늘어납니다. (얼어붙은 적은 3중첩으로)", "Deal more damage to enemies the more chill they carry. (Frozen counts as 3 stacks)", "冷気がたまった敵へのダメージが、たまった冷気の分だけ増えます。（凍った敵は3重扱い）", "对累积寒气的敌人造成的伤害随寒气层数提升。（冻结视为3层）");
        A("냉기 하나당", "Per chill", "冷気1つごと", "每层寒气");
        A("곡소리", "Wail", "嘆きの声", "哀嚎");
        K("영혼이 피해를 막을 때 곡소리가 퍼져 둘레 적을 밀치고 다치게 합니다.", "영혼이 피해를 막을 때 곡소리가 퍼져 둘레의 적을 밀치고 다치게 합니다.","When a soul blocks a hit, a wail knocks back and damages nearby enemies.", "魂がダメージを防ぐとき、嘆きの声が広がり周りの敵を押しのけて傷つけます。", "灵魂抵挡伤害时，哀嚎扩散，击退并伤害周围敌人。");
        A("곡소리 피해", "Wail damage", "嘆きの声ダメージ", "哀嚎伤害");
        A("한기", "Creeping Cold", "寒気", "寒意");
        A("손길에 맞은 적 둘레의 적에게도 냉기가 번집니다.", "Chill spreads to enemies around those you touch.", "手に当たった敵の周りの敵にも冷気が広がります。", "寒气蔓延至被冰手击中敌人周围的敌人。");
        A("번지는 적", "Spreads to", "広がる敵", "蔓延敌人");
        A("생기 흡수", "Life Drain", "生気吸収", "生机吸取");
        A("얼어붙은 적을 손길로 맞히면 체력을 회복합니다.", "Touching a frozen enemy heals you.", "凍った敵を手で当てると体力を回復します。", "用冰手击中被冻结的敌人时恢复生命。");
        A("실체 없음", "Incorporeal", "実体なし", "无形");
        A("움직이는 동안 확률로 공격이 몸을 그대로 통과합니다.", "While moving, attacks may pass right through you.", "動いている間、確率で攻撃が体をそのまま通り抜けます。", "移动中有几率让攻击直接穿过身体。");
        A("통과 확률", "Pass-through chance", "通過確率", "穿过几率");
        A("도깨비불", "Will-o'-Wisp", "鬼火", "鬼火");
        A("거느린 영혼 하나마다 주는 피해가 늘어납니다.", "Each soul you keep increases your damage.", "まとった魂1つごとに与えるダメージが増えます。", "每个环绕的灵魂都会提升造成的伤害。");
        A("영혼 하나당", "Per soul", "魂1つごと", "每个灵魂");
        A("저주", "Curse", "呪い", "诅咒");
        A("빙의한 적이 쓰러지면 둘레의 적 셋에게 저주가 걸려 5초 동안 받는 피해가 늘어납니다.", "When a possessed enemy dies, three nearby enemies are cursed to take more damage for 5s.", "憑依した敵が倒れると周りの敵3体に呪いがかかり、5秒間受けるダメージが増えます。", "被附身的敌人倒下时，周围三名敌人受到诅咒，5秒内受到的伤害提升。");
        A("폴터가이스트", "Poltergeist", "ポルターガイスト", "骚灵");
        A("3초마다 가까운 적을 들어 올렸다 내리꽂습니다.", "Every 3s, lift a nearby enemy and slam it down.", "3秒ごとに近くの敵を持ち上げて叩きつけます。", "每3秒将附近敌人举起再砸下。");
        A("내리꽂는 피해", "Slam damage", "叩きつけダメージ", "砸落伤害");
        A("동상", "Frostbite", "凍傷", "冻伤");
        A("얼어붙은 적이 받는 피해가 늘어납니다.", "Frozen enemies take more damage.", "凍った敵が受けるダメージが増えます。", "被冻结的敌人受到的伤害提升。");
        A("망령의 행렬", "Procession of Shades", "亡霊の行列", "亡灵行列");
        A("빙의하면 영혼이 바로 생겨납니다.", "Possession immediately spawns souls.", "憑依すると魂がすぐに生まれます。", "附身时立即生成灵魂。");
        A("생기는 영혼", "Souls spawned", "生まれる魂", "生成灵魂");

        // ---------------- 스킬 진화
        A("눈보라", "Blizzard", "吹雪", "暴风雪");
        A("손길이 지나간 자리에 1.5초 동안 냉기 안개가 남아 냉기를 쌓습니다.", "Your touch leaves a 1.5s chill mist that builds chill.", "手が通った場所に1.5秒間冷気の霧が残り、冷気をためます。", "冰手经过处留下持续1.5秒的寒雾，累积寒气。");
        A("밴시", "Banshee", "バンシー", "女妖");
        A("곡소리에 휘말린 적이 곧바로 얼어붙습니다.", "Enemies caught in your Wail freeze instantly.", "嘆きの声に巻き込まれた敵がすぐに凍ります。", "被哀嚎波及的敌人立即冻结。");
        A("흡혈귀", "Vampire", "吸血鬼", "吸血鬼");
        A("생기 흡수가 두 배가 되고, 적이 얼어붙을 때마다 체력을 회복합니다.", "Life Drain is doubled, and you heal whenever an enemy freezes.", "生気吸収が2倍になり、敵が凍るたびに体力を回復します。", "生机吸取翻倍，每当敌人被冻结时恢复生命。");
        A("망령", "Specter", "亡霊", "幽魂");
        A("실체 없음 확률이 두 배가 되고, 거느린 영혼 하나마다 이동 속도가 2% 오릅니다.", "Incorporeal's chance is doubled, and each soul you keep adds 2% move speed.", "実体なしの確率が2倍になり、まとった魂1つごとに移動速度が2%上がります。", "无形几率翻倍，每个环绕的灵魂提升2%移动速度。");
        A("파멸", "Doom", "破滅", "毁灭");
        A("저주가 둘레 20칸의 모든 적에게 걸립니다.", "Curse spreads to every enemy within 20 tiles.", "呪いが周囲20マスのすべての敵にかかります。", "诅咒作用于周围20格内所有敌人。");
        A("소란한 집", "Haunted House", "騒がしい家", "闹鬼之屋");
        A("폴터가이스트가 적 셋을 한꺼번에 들어 올립니다.", "Poltergeist lifts three enemies at once.", "ポルターガイストが敵3体を一度に持ち上げます。", "骚灵一次举起三名敌人。");
        A("빙하", "Glacier", "氷河", "冰川");
        A("얼음 파편에 맞은 적이 곧바로 얼어붙습니다.", "Enemies hit by Ice Shards freeze instantly.", "氷の破片に当たった敵がすぐに凍ります。", "被冰之碎片击中的敌人立即冻结。");

        // ---------------- 무기 진화 · 궁극기 · 강화 · 패시브
        A("사슬 망령", "Chained Wraith", "鎖の亡霊", "锁链亡灵");
        A("손길에 처음 맞은 적에게서 영혼 사슬이 가장 가까운 적에게 튑니다. (피해 40%)", "A soul chain leaps from the first enemy you touch to the next nearest enemy. (40% damage)", "手に最初に当たった敵から魂の鎖が最も近い敵へ飛びます。（ダメージ40%）", "灵魂锁链从冰手首个击中的敌人弹向最近的敌人。（伤害40%）");
        K("손길에 처음 맞은 적에게서 영혼 사슬이 가장 가까운 적에게 튑니다.\n궁극기: 영혼 사슬", "손길에 처음 맞은 적에게서 영혼 사슬이 가장 가까운 적에게 튑니다.\n필살기에 덧붙음: 영혼 사슬", "A soul chain leaps from the first enemy you touch to the next nearest enemy.\nAdded to your Ultimate: Soul Chains", "手に最初に当たった敵から魂の鎖が最も近い敵へ飛びます。\n必殺技に上乗せ：魂の鎖", "灵魂锁链从冰手首个击中的敌人弹向最近的敌人。\n附加于必杀技：灵魂锁链");
        A("얼음 유령", "Frost Wraith", "氷の幽霊", "冰霜幽灵");
        A("냉기가 두 배로 쌓입니다.", "Chill builds twice as fast.", "冷気が2倍たまります。", "寒气累积速度翻倍。");
        K("냉기가 두 배로 쌓입니다.\n궁극기: 절대 영도", "냉기가 두 배로 쌓입니다.\n필살기에 덧붙음: 절대 영도", "Chill builds twice as fast.\nAdded to your Ultimate: Absolute Zero", "冷気が2倍たまります。\n必殺技に上乗せ：絶対零度", "寒气累积速度翻倍。\n附加于必杀技：绝对零度");
        A("그림자 유령", "Shadow Wraith", "影の幽霊", "暗影幽灵");
        A("손길이 0.15초 뒤 한 번 더 뻗습니다. (피해 50%)", "Your touch reaches again 0.15s later. (50% damage)", "手が0.15秒後にもう一度伸びます。（ダメージ50%）", "冰手在0.15秒后再伸出一次。（伤害50%）");
        K("손길이 0.15초 뒤 한 번 더 뻗습니다.\n궁극기: 그림자 군단", "손길이 0.15초 뒤 한 번 더 뻗습니다.\n필살기에 덧붙음: 그림자 군단", "Your touch reaches again 0.15s later.\nAdded to your Ultimate: Shadow Legion", "手が0.15秒後にもう一度伸びます。\n必殺技に上乗せ：影の軍団", "冰手在0.15秒后再伸出一次。\n附加于必杀技：暗影军团");
        A("영혼 사슬", "Soul Chains", "魂の鎖", "灵魂锁链");
        A("둘레의 적을 모두 영혼 사슬로 묶어 3초 동안 0.5초마다 벱니다. (공격력 70%)", "Bind all nearby enemies with soul chains, cutting them every 0.5s for 3s. (70% attack)", "周りの敵をすべて魂の鎖で縛り、3秒間0.5秒ごとに斬ります。（攻撃力70%）", "以灵魂锁链束缚周围所有敌人，3秒内每0.5秒斩击。（攻击力70%）");
        A("절대 영도", "Absolute Zero", "絶対零度", "绝对零度");
        A("둘레의 모든 적을 2초 동안 얼리고 피해를 줍니다. (공격력 300%)", "Freeze every nearby enemy for 2s and damage them. (300% attack)", "周りのすべての敵を2秒間凍らせてダメージを与えます。（攻撃力300%）", "冻结周围所有敌人2秒并造成伤害。（攻击力300%）");
        A("그림자 군단", "Shadow Legion", "影の軍団", "暗影军团");
        A("그림자 넷이 3초 동안 둘레의 적에게 손길을 뻗습니다. (공격력 80%씩)", "Four shadows reach for nearby enemies for 3s. (80% attack each)", "影4体が3秒間周りの敵へ手を伸ばします。（攻撃力80%ずつ）", "四道暗影在3秒内向周围敌人伸出冰手。（每次攻击力80%）");
        A("깊은 냉기", "Deep Chill", "深い冷気", "深寒");
        A("얼어붙는 시간이 50% 늘어납니다.", "Freeze duration increases by 50%.", "凍結時間が50%延びます。", "冻结时间延长50%。");
        A("두 손", "Two Hands", "両手", "双手");
        A("손길이 양옆으로 두 갈래 더 뻗습니다. (피해 70%)", "Your touch splits into two more reaches to either side. (70% damage)", "手が左右にさらに2本伸びます。（ダメージ70%）", "冰手向两侧再伸出两道。（伤害70%）");
        A("영혼 폭발", "Soul Burst", "魂の爆発", "灵魂爆发");
        K("영혼이 피해를 막을 때 터져 둘레 적에게 피해를 줍니다. (공격력 150%)", "영혼이 피해를 막을 때 터져 둘레의 적에게 피해를 줍니다. (공격력 150%)","When a soul blocks a hit, it bursts and damages nearby enemies. (150% attack)", "魂がダメージを防ぐとき爆発し、周りの敵にダメージを与えます。（攻撃力150%）", "灵魂抵挡伤害时爆炸，伤害周围敌人。（攻击力150%）");
        A("서늘한 기운", "Chill Aura", "冷ややかな気配", "寒气萦绕");
        A("둘레 2.5칸의 적이 15% 느려집니다.\n진화: 25%", "Enemies within 2.5 tiles are slowed 15%.\nEvolved: 25%", "周囲2.5マスの敵が15%遅くなります。\n進化：25%", "周围2.5格内的敌人减速15%。\n进化：25%");
        A("떠도는 혼", "Wandering Soul", "さまよう魂", "游荡之魂");
        A("이동 속도가 6% 빨라집니다.\n진화: 12%", "Move speed increases by 6%.\nEvolved: 12%", "移動速度が6%速くなります。\n進化：12%", "移动速度提升6%。\n进化：12%");
        A("영혼 수확", "Soul Harvest", "魂の収穫", "灵魂收割");
        A("적을 쓰러뜨릴 때 영혼이 생길 확률이 20% 오릅니다.\n진화: 35%", "Kills are 20% more likely to spawn a soul.\nEvolved: 35%", "敵を倒したときに魂が生まれる確率が20%上がります。\n進化：35%", "击败敌人时生成灵魂的几率提升20%。\n进化：35%");
        A("체력이 20% 아래로 떨어질 피해를 받으면 3초 동안 무적이 됩니다. (60초에 한 번)\n진화: 40초", "A hit that would drop you below 20% health grants 3s of invulnerability instead. (Once per 60s)\nEvolved: 40s", "体力が20%を下回るダメージを受けると3秒間無敵になります。（60秒に1回）\n進化：40秒", "受到会使生命低于20%的伤害时改为无敌3秒。（每60秒一次）\n进化：40秒");
        A("명계의 문", "Gate of the Dead", "冥界の門", "冥界之门");

        // ---------------- 이야기
        A("오래전 이 초원에서 죽은 유령은 아직 떠나지 못하고 있었다.", "A ghost who died in this meadow long ago had still not departed.", "ずっと昔この草原で死んだ幽霊は、まだ旅立てずにいた。", "很久以前死在这片草原上的幽灵，至今仍未离去。");
        A("떠나지 못한 이유를 이제야 알았다. 지켜야 할 것이 남아 있었다.", "Now it finally knew why. There was still something to protect.", "旅立てなかった理由が今ようやく分かった。守るべきものが残っていたのだ。", "如今终于明白无法离去的理由。还有要守护的东西。");
        A("유령은 차가운 손을 쥐었다 폈다. 이번에는 끝까지 지켜 낼 것이다.", "The ghost clenched and opened its cold hand. This time, it would protect to the end.", "幽霊は冷たい手を握っては開いた。今度こそ最後まで守り抜く。", "幽灵握紧又张开冰冷的手。这一次，要守护到最后。");
        A("그중 하나는 리치 왕의 부름을 거역했다. 묘역에서 가장 오래된 유령이었다.", "One of them defied the Lich King's call. It was the oldest ghost in the catacombs.", "その中の一体はリッチ王の呼び声に逆らった。墓所で最も古い幽霊だった。", "其中一个违抗了巫妖王的召唤。那是墓地中最古老的幽灵。");
        A("죽은 자를 깨우는 왕이라면, 죽은 자가 재워야 한다.", "If a king wakes the dead, the dead must put him to sleep.", "死者を起こす王ならば、死者が眠らせねばならない。", "唤醒死者的王，就该由死者让他安眠。");
        A("유령은 소리 없이 신전의 구멍으로 스며들었다.", "The ghost seeped silently into the temple's hole.", "幽霊は音もなく神殿の穴へ染み込んでいった。", "幽灵悄无声息地渗入神殿的洞里。");
        A("떠오르는 영혼들 사이에서, 유령의 몸도 조금씩 빛으로 풀려 갔다.", "Among the rising souls, the ghost's body too began to unravel into light.", "昇っていく魂たちの間で、幽霊の体も少しずつ光へとほどけていった。", "在升起的灵魂之间，幽灵的身体也一点点化作光芒。");
        A("지켜야 할 것을 끝까지 지켰다. 이제 떠나도 된다.", "It had protected what it had to, to the very end. Now it could leave.", "守るべきものを最後まで守った。もう旅立ってもいい。", "要守护的东西已守护到最后。现在可以离开了。");
        A("“…고마웠어.”", "\"...Thank you.\"", "「……ありがとう。」", "“……谢谢你。”");
        K("하지만 마지막 빛이 사라지기 전, 붉은 사막 쪽에서 낯익은 냉기가 불어왔다...", "하지만 마지막 빛이 사라지기 전, 붉은 사막 쪽에서 낯익은 냉기가 불어왔다…", "But before the last light faded, a familiar chill blew in from the red desert...", "しかし最後の光が消える前、赤い砂漠の方から見覚えのある冷気が吹いてきた……", "然而在最后的光芒消失之前，一股熟悉的寒气从红色沙漠吹来……");

        // ================================================================= 고유 규칙 (궁수 숨 고르기 · 연금술사 불안정도)
        K("누르고 있으면 시위를 당기고, 떼면 발사. 가득 찬 순간(딩!) 놓으면 「완벽!」 화살, 너무 오래 쥐면 약해집니다. 당기는 동안은 느려집니다. 숨 고르기: 당기는 동안 숨이 닳고, 바닥나면 활이 떨려 절반 위력 · 「완벽!」은 숨을 되돌려 줍니다",
          "누르고 있으면 시위를 당기고, 떼면 쏩니다. 가득 찬 순간(딩!) 놓으면 「완벽!」 화살이 나가고, 너무 오래 쥐면 약해집니다. 당기는 동안은 느려집니다. 숨 고르기: 당기는 동안 숨이 닳고, 바닥나면 활이 떨려 위력이 절반이 됩니다. 「완벽!」은 숨을 되돌려 줍니다.",
          "Hold to draw the bowstring, release to fire. Release right as it fills (ding!) for a Perfect arrow; hold too long and it weakens. You move slower while drawing. Breath: drawing uses up breath; run out and your bow shakes, halving its power. Perfect shots restore breath.",
          "押し続けて弦を引き、離すと発射します。満タンの瞬間（チン！）に離すと「パーフェクト！」の矢になり、握りすぎると弱まります。引いている間は遅くなります。息継ぎ：引いている間は息が減り、尽きると弓が震えて威力が半分になります。「パーフェクト！」で息が戻ります。",
          "按住拉弓，松开发射。拉满的瞬间（叮！）松开即为「完美！」之箭，握太久会变弱。拉弓时移动变慢。调息：拉弓时消耗气息，耗尽后弓会颤抖，威力减半。「完美！」会恢复气息。");
        K("화염 · 빙결 · 산성 시약을 번갈아 채운 플라스크. 가끔 불안정한 플라스크가 크게 폭발. 불안정도: 던질수록 올라 폭발이 커지지만, 가득 차면 발밑에서 자폭합니다 (대폭발 플라스크가 모두 쏟아 냄)",
          "화염 · 빙결 · 산성 시약을 번갈아 채운 플라스크를 던집니다. 가끔 불안정한 플라스크가 크게 폭발합니다. 불안정도: 던질수록 올라 폭발이 커지지만, 가득 차면 발밑에서 자폭합니다. (대폭발 플라스크는 불안정도를 모두 쏟아 냄)",
          "Throw flasks filled with fire, frost and acid reagents in turn. Now and then an unstable flask explodes violently. Instability: each throw raises it and makes blasts bigger, but when it's full, the flask detonates at your feet. (The Great Blast Flask spends all your Instability)",
          "火炎・氷結・酸の試薬を交互に詰めたフラスコを投げます。時々、不安定なフラスコが大爆発します。不安定度：投げるほど上がり爆発が大きくなりますが、満タンになると足元で自爆します。（大爆発フラスコは不安定度をすべて吐き出す）",
          "掷出轮流装入火焰、冰冻、酸性试剂的烧瓶。偶尔会有不稳定的烧瓶大爆炸。不稳定度：投掷越多越高、爆炸越大，但满了会在脚下自爆。（大爆炸烧瓶会将不稳定度全部倾泻）");
        A("숨이 차다!", "Out of breath!", "息切れ！", "喘不过气！");
        A("폭주!", "Overload!", "暴走！", "失控！");
    }
}
