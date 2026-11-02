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
        S("카키 조끼와 올리브 모자, 등에 멘 도면통. 피격 소리가 가벼워집니다.",
          "A khaki vest, an olive cap and a blueprint tube on the back. Hit sounds become lighter.",
          "カーキのベストとオリーブ色の帽子、背中には図面筒。被弾音が軽くなります。",
          "卡其背心配橄榄色帽子，背着图纸筒。受击声变得轻快。");
        S("해도사", "Chart Navigator", "海図士", "海图师");
        S("남색 외투와 선장 모자, 손에 육분의. 피격하면 맑은 방울 소리가 납니다.",
          "A navy coat, a captain's hat and a sextant in hand. Getting hit rings a clear chime.",
          "紺色のコートと船長帽、手には六分儀。被弾すると澄んだ鈴の音が鳴ります。",
          "藏青外套与船长帽，手持六分仪。受击时会响起清脆的铃声。");
        S("별지도꾼", "Star Mapper", "星図師", "星图师");
        S("별을 꽂은 밤하늘 망토. 움직이면 별빛 잔상이 남고, 피격하면 바람 소리가 납니다.",
          "A night-sky cloak with a star on the hat. Moving leaves starlight afterimages; getting hit sounds like wind.",
          "星を飾った夜空のマント。動くと星明かりの残像が残り、被弾すると風の音が鳴ります。",
          "缀着星星的夜空斗篷。移动时会留下星光残影，受击时发出风声。");
        S("백지도의 저자", "Author of the Blank Map", "白地図の著者", "白地图的作者");
        S("종이로 된 몸에 빨간 교정 펜. 종이 조각 오라가 감돌고, 처치하면 종이가 흩날리며 필살기에 종이 찢는 소리가 울립니다.",
          "A body of paper with a red proofreading pen. A paper-scrap aura swirls around; kills scatter paper, and the ultimate rings with the sound of tearing paper.",
          "紙でできた体と赤い校正ペン。紙片のオーラが漂い、倒すと紙が舞い散り、必殺技には紙を裂く音が響きます。",
          "纸做的身体，手握红色校对笔。纸片光环环绕，击杀时纸张纷飞，必杀技会响起撕纸声。");

        // ---------------- 시계공
        S("견습 시계공", "Apprentice Clockmaker", "見習い時計職人", "学徒钟表匠");
        S("가죽 앞치마와 쇠 톱니. 피격 소리가 철컹거립니다.",
          "A leather apron and an iron cog. Hit sounds clank.",
          "革のエプロンと鉄の歯車。被弾音ががちゃがちゃと鳴ります。",
          "皮围裙与铁齿轮。受击声变得叮当作响。");
        S("모래시계 수도사", "Hourglass Monk", "砂時計の修道士", "沙漏修士");
        S("모래빛 후드와 모래시계 목걸이. 피격하면 맑은 방울 소리가 납니다.",
          "A sand-colored hood and an hourglass pendant. Getting hit rings a clear chime.",
          "砂色のフードと砂時計のペンダント。被弾すると澄んだ鈴の音が鳴ります。",
          "沙色兜帽与沙漏项链。受击时会响起清脆的铃声。");
        S("뻐꾸기 시계", "Cuckoo Clock", "鳩時計", "布谷鸟钟");
        S("등에 멘 뻐꾸기 시계 집과 초록 모자. 움직이면 톱니 잔상이 남고, 피격하면 뻐꾹 소리가 납니다.",
          "A cuckoo-clock house on the back and a green hat. Moving leaves cog afterimages; getting hit goes \"cuckoo\".",
          "背中に鳩時計の家、緑の帽子。動くと歯車の残像が残り、被弾すると「ポッポー」と鳴ります。",
          "背着布谷鸟钟小屋，戴绿色帽子。移动时留下齿轮残影，受击时发出布谷声。");
        S("멈춘 시계의 주인", "Master of the Stopped Clock", "止まった時計の主", "停摆之钟的主人");
        S("11시 59분에 멈춘 깨진 시계판. 시계 오라가 감돌고, 처치하면 톱니가 분해되며 필살기에 큰 종소리가 울립니다.",
          "A cracked clock face stopped at 11:59. A clock aura swirls around; kills break gears apart, and the ultimate rings a great bell.",
          "11時59分で止まったひび割れの文字盤。時計のオーラが漂い、倒すと歯車が分解し、必殺技には大きな鐘の音が響きます。",
          "停在11点59分的碎裂表盘。钟表光环环绕，击杀时齿轮分解，必杀技会响起巨大的钟声。");

        // ---------------- 우체부
        S("신문 배달부", "Paperboy", "新聞配達員", "送报员");
        S("파란 야구 모자와 둘둘 만 신문. 피격 소리가 가벼워집니다.",
          "A blue baseball cap and a rolled-up newspaper. Hit sounds become lighter.",
          "青い野球帽と丸めた新聞。被弾音が軽くなります。",
          "蓝色棒球帽与卷起的报纸。受击声变得轻快。");
        S("비둘기 조련사", "Pigeon Trainer", "ハト使い", "鸽子训练师");
        S("모자 위에 비둘기가 앉은 잿빛 외투. 피격하면 날갯짓 소리가 납니다.",
          "A grey coat with a pigeon perched on the hat. Getting hit sounds like flapping wings.",
          "帽子の上にハトが止まった灰色のコート。被弾すると羽ばたきの音が鳴ります。",
          "帽子上停着鸽子的灰色外套。受击时发出振翅声。");
        S("택배 기사", "Courier", "宅配ドライバー", "快递员");
        S("형광 조끼에 택배 상자와 단말기. 움직이면 잔상이 남고, 피격하면 단말기 삑 소리가 납니다.",
          "A hi-vis vest, a parcel box and a handheld scanner. Moving leaves afterimages; getting hit beeps.",
          "蛍光ベストに宅配ボックスと端末。動くと残像が残り、被弾すると端末の「ピッ」という音が鳴ります。",
          "荧光背心配快递箱与扫描终端。移动时留下残影，受击时发出滴的一声。");
        S("반송 우체부", "Return-to-Sender Postman", "返送郵便配達員", "退件邮差");
        S("검은 제복에 빨간 RETURN 도장과 밀랍 봉인. 도장 오라가 감돌고, 처치하면 소인 찍힌 봉투가 흩어지며 필살기에 쿵쿵쿵 도장 소리가 울립니다.",
          "A black uniform with a red RETURN stamp and a wax seal. A stamp aura swirls around; kills scatter postmarked envelopes, and the ultimate thumps three times like a stamp.",
          "黒い制服に赤いRETURNの判子と封蝋。判子のオーラが漂い、倒すと消印入りの封筒が散り、必殺技にはドンドンドンと判子の音が響きます。",
          "黑色制服配红色RETURN印章与火漆封印。印章光环环绕，击杀时盖着邮戳的信封四散，必杀技会响起咚咚咚的盖章声。");

        // ---------------- 유령
        S("이불 유령", "Blanket Ghost", "布団ゴースト", "被单幽灵");
        S("체크무늬 이불에 잠옷 모자를 쓴 유령. 피격 소리가 포근해집니다.",
          "A ghost in a checkered blanket and a nightcap. Hit sounds become soft and cozy.",
          "チェック柄の布団にナイトキャップをかぶったゴースト。被弾音がふんわりします。",
          "盖着格子被单、戴睡帽的幽灵。受击声变得柔和。");
        S("도깨비불", "Will-o'-the-Wisp", "鬼火", "鬼火");
        S("청록 불꽃으로 타오르는 유령. 피격하면 불씨 튀는 소리가 납니다.",
          "A ghost burning with teal flames. Getting hit crackles like sparks.",
          "青緑の炎で燃えるゴースト。被弾すると火の粉が弾ける音が鳴ります。",
          "燃烧着青绿色火焰的幽灵。受击时发出火星迸溅声。");
        S("심해 유령", "Deep-Sea Ghost", "深海ゴースト", "深海幽灵");
        S("발광 반점과 낚싯대 불빛. 움직이면 물빛 잔상이 남고, 피격하면 물방울 소리가 납니다.",
          "Glowing spots and an angler lure. Moving leaves watery afterimages; getting hit bubbles.",
          "発光する斑点と提灯のような光。動くと水色の残像が残り、被弾すると泡の音が鳴ります。",
          "发光斑点与灯笼鱼般的诱光。移动时留下水色残影，受击时发出气泡声。");
        S("삭제된 장면", "Deleted Scene", "削除されたシーン", "被删除的场景");
        S("반은 그려지고 반은 마젠타 와이어프레임. 깨진 격자 오라가 감돌고, 처치하면 픽셀로 흩어지며 필살기에 모뎀 소리가 울립니다.",
          "Half rendered, half magenta wireframe. A broken-grid aura swirls around; kills scatter into pixels, and the ultimate rings with a dial-up modem.",
          "半分は描かれ、半分はマゼンタのワイヤーフレーム。壊れた格子のオーラが漂い、倒すとピクセルに散り、必殺技にはモデムの音が響きます。",
          "一半已绘制，一半是品红线框。破碎网格光环环绕，击杀时化作像素四散，必杀技会响起调制解调器的声音。");

        // ---------------- 무기
        S("붓", "Ink Brush", "筆", "毛笔");
        S("대나무 자루 붓. 획이 먹빛으로 번지고 맑은 울림이 더해집니다.",
          "A bamboo-handled brush. Strokes bleed ink-dark and a clear ring is added.",
          "竹の柄の筆。線が墨色ににじみ、澄んだ響きが加わります。",
          "竹柄毛笔。笔画晕出墨色，并带有清脆的回响。");
        S("분필", "Chalk", "チョーク", "粉笔");
        S("하얀 분필. 획이 가루처럼 흩날리고 사각거리는 소리가 섞입니다.",
          "A white piece of chalk. Strokes scatter like dust with a scratchy sound mixed in.",
          "白いチョーク。線が粉のように舞い、さらさらという音が混ざります。",
          "白色粉笔。笔画如粉末飞散，并混入沙沙声。");
        S("금화", "Gold Coin", "金貨", "金币");
        S("금화를 던집니다. 금빛으로 날고 짤랑이는 소리가 더해집니다.",
          "Throw gold coins. They fly gold and add a jingling sound.",
          "金貨を投げます。金色に飛び、チャリンという音が加わります。",
          "投掷金币。金光飞舞，并带有叮当声。");
        S("주판알", "Abacus Bead", "そろばん玉", "算盘珠");
        S("윤나는 주판알. 갈색으로 날고 딱딱 부딪는 소리가 섞입니다.",
          "A glossy abacus bead. It flies brown with a clacking sound mixed in.",
          "つやのあるそろばん玉。茶色に飛び、カチカチとぶつかる音が混ざります。",
          "光亮的算盘珠。褐色飞出，并混入啪嗒碰撞声。");
        S("종이비행기", "Paper Plane", "紙飛行機", "纸飞机");
        S("접은 종이비행기. 하얗게 날고 바람 가르는 소리가 더해집니다.",
          "A folded paper plane. It flies white with a whooshing sound added.",
          "折った紙飛行機。白く飛び、風を切る音が加わります。",
          "折好的纸飞机。白色飞出，并带有破风声。");
        S("청구서", "Bill", "請求書", "账单");
        S("붉은 머리띠 청구서. 붉게 날고 도장 찍는 소리가 섞입니다.",
          "A bill with a red header. It flies red with a stamping sound mixed in.",
          "赤い帯の請求書。赤く飛び、判子を押す音が混ざります。",
          "红色抬头的账单。红光飞出，并混入盖章声。");
        S("촛대", "Candlestick", "燭台", "烛台");
        S("일렁이는 촛불. 따뜻한 빛으로 날고 불붙는 소리가 더해집니다.",
          "A flickering candle. It flies with a warm glow and adds a fire-lighting sound.",
          "揺らめくろうそく。暖かい光で飛び、火のつく音が加わります。",
          "摇曳的烛火。暖光飞出，并带有点火声。");
        S("쇠사슬 등불", "Chained Lantern", "鎖のランタン", "锁链灯笼");
        S("쇠사슬을 두른 등불. 주황빛으로 날고 철컹이는 소리가 섞입니다.",
          "A lantern wrapped in chains. It flies orange with a clanking sound mixed in.",
          "鎖を巻いたランタン。オレンジ色に飛び、ガシャンという音が混ざります。",
          "缠着锁链的灯笼。橙光飞出，并混入铿锵声。");
    }
}
