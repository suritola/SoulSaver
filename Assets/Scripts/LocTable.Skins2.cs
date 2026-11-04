using UnityEngine;

// 번역표 (2.0.0~): 새 영웅 넷(지도 제작자 · 시계공 · 우체부 · 유령)의 스킨 이름 24 · 설명 24 (SkinData)
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddSkins2()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        // ---------------- 지도 제작자
        S("제도사", "Draftsman", "製図士", "制图员");
        S("카키 조끼와 올리브 모자, 등에 멘 도면통. 걸음마다 연필 낙서와 사각 소리, 레벨업하면 도면이 촤악 펼쳐지고 이기면 측량 깃발이 꽂힙니다.",
          "A khaki vest, an olive cap and a blueprint tube on the back. Pencil doodles and scratching with every step, a blueprint unrolls on level-up, and a survey flag is planted on victory.",
          "カーキのベストとオリーブ色の帽子、背中には図面筒。一歩ごとに鉛筆の落書きとサラサラ音、レベルアップで図面がバサッと広がり、勝利すると測量旗が立ちます。",
          "卡其背心配橄榄色帽子，背着图纸筒。每走一步都留下铅笔涂鸦与沙沙声，升级时图纸哗地展开，胜利时插上测量旗。");
        S("해도사", "Chart Navigator", "海図士", "海图师");
        S("남색 외투와 선장 모자, 손에 육분의. 걸음마다 물방울이 튀고, 쉬면 갈매기가 지나가며, 레벨업하면 나침반이 돌고 쓰러지면 모자만 물에 뜹니다.",
          "A navy coat, a captain's hat and a sextant in hand. Water splashes with each step, gulls fly past when idle, a compass spins on level-up, and only your hat floats when you fall.",
          "紺色のコートと船長帽、手には六分儀。歩くたびに水しぶきが跳ね、休むとカモメが通り過ぎ、レベルアップで羅針盤が回り、倒れると帽子だけが水に浮かびます。",
          "藏青外套与船长帽，手持六分仪。每一步溅起水花，静止时海鸥掠过，升级时罗盘旋转，倒下时只剩帽子浮在水上。");
        S("별지도꾼", "Star Mapper", "星図師", "星图师");
        S("별을 꽂은 밤하늘 망토. 걸을 때마다 별이 반짝이며 음계를 한 음씩 연주하고, 쉬면 유성이 떨어지며, 레벨업하면 별이 솟고 이기면 유성우가 쏟아집니다.",
          "A night-sky cloak pinned with stars. Every step twinkles and plays the next note of a scale, shooting stars fall when idle, stars rise on level-up, and a meteor shower pours down when you win.",
          "星を留めた夜空のマント。歩くたびに星がきらめき音階を一音ずつ奏で、休むと流れ星が落ち、レベルアップで星が昇り、勝つと流星群が降り注ぎます。",
          "缀满星星的夜空披风。每走一步星光闪烁、奏出音阶中的一个音，静止时流星划落，升级时群星升起，胜利时流星雨倾泻而下。");
        S("백지도의 저자", "Author of the Blank Map", "白地図の著者", "白地图的作者");
        S("종이로 된 몸에 빨간 교정 펜. 종잇조각이 흩날리고, 평타마다 교정 체크가 「슥」, 필살기에 종이 찢는 소리, 쓰러지면 구겨진 종이 공이 굴러갑니다.",
          "A body of paper and a red proofreading pen. Paper scraps drift around you, each basic attack scratches a check mark, your ultimate tears paper, and a crumpled paper ball rolls away when you fall.",
          "紙でできた体に赤い校正ペン。紙片が舞い、通常攻撃のたびに校正のチェックが「シュッ」、必殺技で紙を裂く音、倒れると丸めた紙が転がっていきます。",
          "纸做的身体配红色校对笔。纸片纷飞，每次普攻「唰」地画下校对勾，必杀技伴着撕纸声，倒下时揉皱的纸团滚落一旁。");

        // ---------------- 시계공
        S("견습 시계공", "Apprentice Clockmaker", "見習い時計職人", "学徒钟表匠");
        S("가죽 앞치마와 쇠 톱니. 걸음이 째깍째깍 울리고, 던질 때 나사가 튀며, 레벨업하면 톱니 셋이 맞물려 돌고 쓰러지면 째깍 소리가 느려지다 멈춥니다.",
          "A leather apron and iron gears. Your footsteps tick and tock, screws fly with every throw, three gears mesh and spin on level-up, and when you fall the ticking slows to a stop.",
          "革のエプロンと鉄の歯車。足音がチクタクと刻まれ、投げるとネジが飛び、レベルアップで三つの歯車がかみ合って回り、倒れるとチクタク音がゆっくり止まります。",
          "皮围裙与铁齿轮。脚步声滴答作响，投掷时螺丝飞溅，升级时三个齿轮咬合转动，倒下时滴答声渐渐变慢直至停止。");
        S("모래시계 수도사", "Hourglass Monk", "砂時計の修道士", "沙漏修士");
        S("모래빛 후드와 모래시계 목걸이. 걸음마다 모래가 흘러내리고, 평타마다 모래시계가 뒤집히며, 레벨업하면 종과 성가가 울리고 쓰러지면 유리가 깨져 모래가 쏟아집니다.",
          "A sand-colored hood and an hourglass pendant. Sand trickles with each step, the hourglass flips on every basic attack, bells and chant ring on level-up, and the glass shatters into pouring sand when you fall.",
          "砂色のフードと砂時計の首飾り。歩くたびに砂がこぼれ、通常攻撃のたびに砂時計が返り、レベルアップで鐘と聖歌が響き、倒れるとガラスが割れて砂があふれます。",
          "沙色兜帽配沙漏项链。每一步都有细沙流下，每次普攻沙漏翻转，升级时钟声与圣歌响起，倒下时玻璃碎裂、细沙倾泻。");
        S("뻐꾸기 시계", "Cuckoo Clock", "鳩時計", "布谷鸟钟");
        S("등에 멘 뻐꾸기 시계 집과 초록 모자. 걸음이 똑 · 딱 번갈아 울리고, 출발하면 뻐꾸기가 「뻐꾹」 튀어나오며, 맞아도 「뻐꾹」, 이기면 뻐꾸기 넷이 정시를 알립니다.",
          "A cuckoo-clock house on the back and a green cap. Steps alternate tick and tock, a cuckoo pops out as you start moving, hits go cuckoo too, and four cuckoos strike the hour when you win.",
          "背負った鳩時計の家と緑の帽子。足音がチク・タクと交互に鳴り、動き出すとカッコウが「カッコー」と飛び出し、被弾しても「カッコー」、勝つとカッコウ4羽が正時を告げます。",
          "背着布谷鸟钟屋、戴绿帽子。脚步嘀、嗒交替作响，起步时布谷鸟「布谷」探出头，受击也「布谷」，胜利时四只布谷鸟齐声报时。");
        S("멈춘 시계의 주인", "Master of the Stopped Clock", "止まった時計の主", "停摆之钟的主人");
        S("11시 59분에 멈춘 깨진 시계판. 머리 위에 회중시계가 떠 있고, 출발할 때 시간이 잠깐 멈추며, 필살기에 큰 종소리, 쓰러지면 유리와 나사로 흩어집니다.",
          "A cracked clock face stopped at 11:59. A pocket watch hovers overhead, time freezes for a moment as you start moving, a great bell tolls on your ultimate, and you scatter into glass and screws when you fall.",
          "11時59分で止まった割れた文字盤。頭上に懐中時計が浮かび、動き出すとき時間が一瞬止まり、必殺技で大きな鐘の音、倒れるとガラスとネジになって散ります。",
          "停在11点59分的破碎表盘。头顶悬着怀表，起步时时间短暂静止，必杀技时大钟轰鸣，倒下时散作玻璃与螺丝。");

        // ---------------- 우체부
        S("신문 배달부", "Paperboy", "新聞配達員", "送报员");
        S("파란 야구 모자와 둘둘 만 신문. 운동화 발소리 사이로 자전거 벨이 따릉, 레벨업하면 신문 1면이 빙글 돌며 나타나고 맞으면 신문지가 흩날립니다.",
          "A blue baseball cap and a rolled-up newspaper. A bicycle bell rings between your sneaker steps, a front page spins into view on level-up, and newspapers scatter when you're hit.",
          "青い野球帽と丸めた新聞。スニーカーの足音の合間に自転車のベルがチリン、レベルアップで新聞の一面がくるくる回って現れ、被弾すると新聞紙が舞い散ります。",
          "蓝色棒球帽与卷起的报纸。运动鞋脚步声间响起自行车铃「叮铃」，升级时报纸头版旋转登场，受击时报纸四散飞舞。");
        S("비둘기 조련사", "Pigeon Trainer", "ハト使い", "鸽子训练师");
        S("모자 위에 비둘기가 앉은 잿빛 외투. 여덟 걸음마다 「구구」, 맞으면 깃털이 흩날리고, 레벨업하면 비둘기 떼가 솟으며 이기면 흰 비둘기가 하늘로 날아갑니다.",
          "A gray coat with a pigeon perched on the hat. It coos every eight steps, feathers fly when you're hit, a flock bursts up on level-up, and white doves take to the sky when you win.",
          "帽子に鳩がとまった灰色のコート。8歩ごとに「クルックー」、被弾すると羽が舞い、レベルアップで鳩の群れが舞い上がり、勝つと白い鳩が空へ飛び立ちます。",
          "帽子上停着鸽子的灰色外套。每走八步「咕咕」一声，受击时羽毛纷飞，升级时鸽群腾起，胜利时白鸽飞向天空。");
        S("택배 기사", "Courier", "宅配ドライバー", "快递员");
        S("형광 조끼에 택배 상자와 단말기. 열두 걸음마다 바코드 「삑」, 레벨업하면 상자가 솟으며 「띵동」, 처치를 이으면 상자가 날아들고, 이기면 상자가 쌓입니다.",
          "A hi-vis vest, a parcel and a handheld scanner. A barcode beep every twelve steps, a box rises with a ding-dong on level-up, chained kills pull parcels in, and boxes stack up when you win.",
          "蛍光ベストに宅配の箱と端末。12歩ごとにバーコードの「ピッ」、レベルアップで箱がせり上がり「ピンポーン」、連続撃破で箱が飛んできて、勝つと箱が積み上がります。",
          "荧光背心配快递箱与终端机。每走十二步条码「哔」一声，升级时箱子升起「叮咚」，连续击杀时包裹飞来，胜利时箱子堆成一摞。");
        S("반송 우체부", "Return-to-Sender Postman", "返送郵便配達員", "退件邮差");
        S("검은 제복에 빨간 RETURN 도장과 밀랍 봉인. 봉투가 감돌고, 걸음마다 봉투 자국이 찍히며, 필살기에 쿵쿵쿵 도장 소리, 레벨업하면 머리 위에 도장이 「쾅」 찍힙니다.",
          "A black uniform with a red RETURN stamp and a wax seal. Envelopes drift around you, each step leaves an envelope print, your ultimate thumps three stamps, and a stamp slams down overhead on level-up.",
          "黒い制服に赤いRETURNの判子と封蝋。封筒が漂い、歩くたびに封筒の跡が押され、必殺技でドンドンドンと判子の音、レベルアップで頭上に判子が「ドン」と押されます。",
          "黑色制服配红色RETURN印章与火漆封印。信封环绕，每一步留下信封印迹，必杀技时咚咚咚盖章声，升级时头顶「砰」地盖下印章。");

        // ---------------- 유령
        S("이불 유령", "Blanket Ghost", "布団ゴースト", "被单幽灵");
        S("체크무늬 이불에 잠옷 모자를 쓴 유령. 걸으면 이불 자락이 사락 끌리고, 쉬면 코를 골며, 레벨업하면 별 · 달 모빌이 자장가와 함께 내려옵니다.",
          "A ghost in a checkered blanket and a nightcap. The blanket hem swishes along as you move, you snore when idle, and a star-and-moon mobile descends with a lullaby on level-up.",
          "チェック柄の毛布にナイトキャップの幽霊。動くと毛布の裾がさらさらと引きずられ、休むといびきをかき、レベルアップで星と月のモビールが子守唄とともに降りてきます。",
          "裹着格子被子、戴着睡帽的幽灵。移动时被角沙沙拖地，静止时打起呼噜，升级时星星月亮挂饰伴着摇篮曲缓缓降下。");
        S("도깨비불", "Will-o'-the-Wisp", "鬼火", "鬼火");
        S("청록 불꽃으로 타오르는 유령. 걸으면 꺼진 불씨 자국이 남고, 쉬면 저만치 떨어진 곳에 불이 깜빡 떴다 사라지며, 쓰러지면 연기 한 줄기만 남습니다.",
          "A ghost burning with teal flame. Steps leave burnt-out embers, a flame flickers up some distance away when idle, and only a wisp of smoke remains when you fall.",
          "青緑の炎で燃える幽霊。歩くと消えた火の粉の跡が残り、休むと少し離れた所に火がふっと浮かんで消え、倒れると一筋の煙だけが残ります。",
          "燃着青绿火焰的幽灵。走过之处留下熄灭的火星，静止时远处会忽地亮起一团火又消失，倒下时只剩一缕青烟。");
        S("심해 유령", "Deep-Sea Ghost", "深海ゴースト", "深海幽灵");
        S("발광 반점과 낚싯대 불빛. 걸음마다 거품이 오르고, 쉬면 물고기가 곁을 맴돌며, 레벨업하면 해파리가 떠오르고, 쓰러지면 깊은 물속으로 가라앉습니다.",
          "Glowing spots and an angler's lure light. Bubbles rise with each step, fish linger by your side when idle, a jellyfish floats up on level-up, and you sink into the deep when you fall.",
          "発光する斑点と釣り竿の灯り。歩くたびに泡が昇り、休むと魚がそばを漂い、レベルアップでクラゲが浮かび、倒れると深い水の底へ沈みます。",
          "发光斑点配钓竿灯。每一步都冒出气泡，静止时小鱼在身旁游弋，升级时水母浮起，倒下时沉入深水之中。");
        S("삭제된 장면", "Deleted Scene", "削除されたシーン", "被删除的场景");
        S("반은 그려지고 반은 마젠타 와이어프레임. 걸음마다 픽셀 자국이 남고, 쉬면 깜빡이는 입력 커서가 뜨며, 필살기에 모뎀 소리, 쓰러지면 위에서부터 지워집니다.",
          "Half drawn, half magenta wireframe. Steps leave pixel prints, a blinking text cursor appears when idle, your ultimate screeches like a modem, and you get erased from the top down when you fall.",
          "半分は描かれ、半分はマゼンタのワイヤーフレーム。歩くたびにピクセルの跡が残り、休むと点滅する入力カーソルが現れ、必殺技でモデムの音、倒れると上から消えていきます。",
          "一半已绘制，一半是洋红线框。每一步留下像素印迹，静止时浮现闪烁的输入光标，必杀技伴着调制解调器声，倒下时从上往下被抹除。");

        // ---------------- 무기
        S("붓", "Ink Brush", "筆", "毛笔");
        S("대나무 자루 붓. 획마다 먹물이 번지며 방울이 튀고, 맞히면 「똑」 먹물 소리가 납니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "A bamboo-handled brush. Every stroke blooms with ink and flicks droplets, and hits land with an inky drip. Changes the look of the basic attack before evolution.",
          "竹の柄の筆。線ごとに墨がにじんで滴が飛び、命中すると「ポタッ」と墨の音がします。進化前の通常攻撃の見た目が変わります。",
          "竹柄毛笔。每一笔墨色晕开、墨点飞溅，命中时「嗒」地滴墨声。会改变进化前基础普攻的外观。");
        S("분필", "Chalk", "チョーク", "粉笔");
        S("하얀 분필. 획이 가루처럼 흩날리며 사각거리고, 쉴 땐 공중에 별 낙서가 뜨며, 진화하면 무지갯빛 분필 가루가 솟습니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "White chalk. Strokes scatter like dust with a scratchy sound, a doodled star floats in the air when idle, and rainbow chalk dust rises when it evolves. Changes the look of the basic attack before evolution.",
          "白いチョーク。線が粉のように舞いカリカリ鳴り、休むと空中に星の落書きが浮かび、進化すると虹色のチョークの粉が昇ります。進化前の通常攻撃の見た目が変わります。",
          "白色粉笔。笔画如粉尘飘散、沙沙作响，静止时空中浮现星星涂鸦，进化时彩虹色粉笔灰升起。会改变进化前基础普攻的外观。");
        S("금화", "Gold Coin", "金貨", "金币");
        S("금화를 던집니다. 금화가 빙글 돌며 「팅~」, 맞히면 「짤랑」 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "Throws gold coins. They spin with a ting, and hits jingle. Changes the look of the basic attack before evolution.",
          "金貨を投げます。金貨がくるりと回り「チーン」、命中すると「チャリン」と鳴ります。進化前の通常攻撃の見た目が変わります。",
          "投掷金币。金币旋转「叮~」，命中时「叮当」作响。会改变进化前基础普攻的外观。");
        S("주판알", "Abacus Bead", "そろばん玉", "算盘珠");
        S("윤나는 주판알. 던지면 「딱」, 맞히면 「딱딱」, 쉴 땐 손 위에서 알이 오가고, 진화하면 금빛 알이 차르륵 솟습니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "Polished abacus beads. Throws clack, hits clack-clack, beads slide back and forth above your hand when idle, and golden beads rattle upward when it evolves. Changes the look of the basic attack before evolution.",
          "つやのあるそろばん玉。投げると「パチッ」、命中で「パチパチ」、休むと手の上で玉が行き来し、進化すると金色の玉がジャラッと昇ります。進化前の通常攻撃の見た目が変わります。",
          "油亮的算盘珠。投出「啪」，命中「啪啪」，静止时珠子在手上来回拨动，进化时金珠哗啦升起。会改变进化前基础普攻的外观。");
        S("종이비행기", "Paper Plane", "紙飛行機", "纸飞机");
        S("접은 종이비행기. 날릴 때 「슈웅」 바람을 가르고, 맞히면 종이 소리가 납니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "A folded paper plane. Throws whoosh through the air, and hits rustle like paper. Changes the look of the basic attack before evolution.",
          "折った紙飛行機。飛ばすと「シューン」と風を切り、命中すると紙の音がします。進化前の通常攻撃の見た目が変わります。",
          "折好的纸飞机。掷出时「嗖」地划破空气，命中时发出纸张声。会改变进化前基础普攻的外观。");
        S("청구서", "Bill", "請求書", "账单");
        S("붉은 머리띠 청구서. 던지면 도장이 「쿵」, 맞히면 계산대 종이 「땡」, 쉴 땐 영수증이 길게 나풀거리고, 큰 한 방엔 금화가 튑니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "A bill with a red header. Throws thump like a stamp, hits ding like a register bell, a long receipt flutters when idle, and big hits pop coins. Changes the look of the basic attack before evolution.",
          "赤い帯の請求書。投げるとハンコが「ドン」、命中でレジのベルが「チン」、休むとレシートが長く垂れてひらひらし、大きな一撃では金貨が弾けます。進化前の通常攻撃の見た目が変わります。",
          "红色抬头的账单。投出时印章「咚」，命中时收银铃「叮」，静止时长长的收据飘动，重击时金币弹出。会改变进化前基础普攻的外观。");
        S("촛대", "Candlestick", "燭台", "烛台");
        S("일렁이는 촛대. 쏠 때 촛불이 번쩍 「화륵」 불씨가 오르고, 맞히면 촛농 떨어지는 「똑」 소리가 납니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "A flickering candlestick. Shots flare up with a whoosh of rising embers, and hits drip like falling wax. Changes the look of the basic attack before evolution.",
          "揺らめく燭台。撃つとろうそくがパッと「ボウッ」と灯り火の粉が昇り、命中すると蝋が垂れる「ポタッ」という音がします。進化前の通常攻撃の見た目が変わります。",
          "摇曳的烛台。发射时烛火一闪「呼」地升起火星，命中时「嗒」地滴蜡声。会改变进化前基础普攻的外观。");
        S("쇠사슬 등불", "Chained Lantern", "鎖のランタン", "锁链灯笼");
        S("쇠사슬을 두른 등불. 쏠 때 「철커덩」, 쉴 땐 등불이 사슬에 흔들리고, 큰 한 방엔 주황빛이 번지며, 진화하면 쇠사슬이 금빛으로 솟습니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "A lantern wrapped in chains. Shots clank, the lantern sways on its chain when idle, big hits flood orange light, and the chains rise gold when it evolves. Changes the look of the basic attack before evolution.",
          "鎖を巻いたランタン。撃つと「ガチャン」、休むとランタンが鎖に揺れ、大きな一撃では橙色の光が広がり、進化すると鎖が金色に昇ります。進化前の通常攻撃の見た目が変わります。",
          "缠着铁链的提灯。发射时「哐当」，静止时提灯在链上摇晃，重击时橙光晕开，进化时铁链化作金色升起。会改变进化前基础普攻的外观。");
    }
}
