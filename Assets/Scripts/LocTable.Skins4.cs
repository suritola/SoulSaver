using UnityEngine;

// 번역표 (2.2.0~): 새 영웅 셋(인형사 · 장의사 · 사진사)의 캐릭터 스킨 이름 12 · 설명 12 · 무기 스킨 이름 6 · 설명 6 (SkinData)
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddSkins4()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        // ---------------- 인형사
        S("그림자극", "Shadow Play", "影絵芝居", "皮影戏");
        S("검은 종이 실루엣 연출가, 더미도 그림자 인형이 되어 벽에 비친 듯 납작합니다. 피격 소리가 가벼워집니다.",
          "A director cut from black paper; the dummy becomes a shadow puppet, flat as if cast on a wall. Hit sounds become lighter.",
          "黒い紙の影絵の演出家。ダミーも影絵人形になり、壁に映ったように平たくなります。被弾音が軽くなります。",
          "黑纸剪影般的演出者，假人也变成皮影，像映在墙上一样扁平。受击声变得轻快。");
        S("종이 인형", "Paper Doll", "紙人形", "纸娃娃");
        S("접은 종이 옷의 연출가, 더미는 접이식 종이 인형이고 줄은 실 대신 종이끈입니다. 피격하면 종이 구겨지는 소리가 납니다.",
          "A director in folded-paper clothes; the dummy is a folding paper doll and the strings are paper cords. Getting hit crinkles like paper.",
          "折り紙の服の演出家。ダミーは折りたたみ式の紙人形で、糸の代わりに紙ひもを使います。被弾すると紙がくしゃっとなる音がします。",
          "穿折纸衣服的演出者，假人是折叠纸偶，丝线换成了纸绳。受击时会发出纸张揉皱的声音。");
        S("묘역 인형사", "Graveyard Puppeteer", "墓地の人形師", "墓地人偶师");
        S("묘역 무대의 연출가, 더미가 해골 인형이 되어 휘두를 때 뼈가 달그락거립니다. 움직이면 영혼불 잔상이 남습니다.",
          "A director of the graveyard stage; the dummy becomes a skeleton puppet whose bones rattle when swung. Moving leaves soul-fire afterimages.",
          "墓地の舞台の演出家。ダミーが骸骨人形になり、振ると骨がカタカタ鳴ります。動くと鬼火の残像が残ります。",
          "墓地舞台的演出者，假人变成骷髅偶，挥动时骨头咯咯作响。移动时会留下鬼火残影。");
        S("개발자의 손", "The Developer's Hand", "開発者の手", "开发者之手");
        S("더미가 마우스 커서, 줄이 케이블, 커튼콜은 커서 여럿이 따닥따닥 클릭합니다. 처치하면 작은 창이 틱 닫히고 발밑에 작업 표시줄 띠가 깔립니다. (업적형)",
          "The dummy is a mouse cursor and the strings are cables; the curtain call is a flurry of cursors clicking. Defeated foes vanish like closing windows, and a taskbar strip lies underfoot. (Achievement)",
          "ダミーはマウスカーソル、糸はケーブル。カーテンコールは複数のカーソルがカチカチとクリックします。倒すと小さなウィンドウがパチッと閉じ、足元にタスクバーが敷かれます。(実績型)",
          "假人是鼠标光标，丝线是电缆，谢幕时多个光标噼啪点击。击杀后小窗口啪地关闭，脚下铺着任务栏条。(成就型)");

        // ---------------- 장의사
        S("묘지기", "Gravekeeper", "墓守", "守墓人");
        S("랜턴을 든 밤 묘지기. 모자 대신 두건, 구멍 가장자리에 랜턴 불빛이 비칩니다. 피격 소리가 가벼워집니다.",
          "A night gravekeeper with a lantern. A hood instead of a hat, and lantern light glints at the hole's edge. Hit sounds become lighter.",
          "ランタンを持つ夜の墓守。帽子の代わりにフード、穴のふちにランタンの灯りが差します。被弾音が軽くなります。",
          "提着提灯的夜间守墓人。以兜帽代替帽子，坑边映着提灯的光。受击声变得轻快。");
        S("석공", "Stonemason", "石工", "石匠");
        S("돌가루 앉은 모자에 앞치마의 석공. 묘비 대신 작은 조각상이 솟습니다. 피격하면 쇳소리가 납니다.",
          "A stonemason in an apron and a stone-dusted hat. Small statues rise instead of gravestones. Getting hit rings like metal.",
          "石粉をかぶった帽子とエプロンの石工。墓石の代わりに小さな彫像がせり上がります。被弾すると金属音がします。",
          "戴着落满石粉的帽子、系着围裙的石匠。升起的不是墓碑而是小雕像。受击时发出金属声。");
        S("지옥 장의사", "Hellish Undertaker", "地獄の葬儀屋", "地狱殡葬师");
        S("지옥의 묘지기. 묘비가 불꽃 표식을 달고 솟습니다. 움직이면 불씨 잔상이 남고, 피격하면 포효 소리가 납니다.",
          "A gravekeeper of hell. Gravestones rise bearing a flame mark. Moving leaves ember afterimages; getting hit lets out a roar.",
          "地獄の墓守。墓石が炎の印をつけてせり上がります。動くと火の粉の残像が残り、被弾すると咆哮が響きます。",
          "地狱的守墓人。墓碑带着火焰标记升起。移动时会留下火星残影，受击时发出咆哮。");
        S("미완성 묘비", "Unfinished Gravestone", "未完成の墓石", "未完成的墓碑");
        S("비문이 「TODO: 비문 쓰기」인 회색 상자 묘비. 매장 때 「저장 중…」 모래시계가 돌고 띵 저장음이 울리며, 처치하면 작은 저장 아이콘으로 줄어듭니다. (업적형)",
          "A gray-box gravestone whose epitaph reads \"TODO: write epitaph\". Burials show a \"Saving...\" hourglass and a ding; defeated foes shrink into a small save icon. (Achievement)",
          "墓碑銘が「TODO: 碑文を書く」の灰色の箱の墓石。埋葬時に「保存中…」の砂時計が回り、ポーンと保存音が鳴ります。倒すと小さなセーブアイコンに縮みます。(実績型)",
          "碑文写着「TODO: 写碑文」的灰色方块墓碑。安葬时出现「保存中…」沙漏并响起叮的存档音，击杀后缩成小存档图标。(成就型)");

        // ---------------- 사진사
        S("신문 기자", "Press Reporter", "新聞記者", "报社记者");
        S("PRESS 띠를 두른 모자와 낡은 코트, 플래시가 조금 노랗습니다. 피격 소리가 가벼워집니다.",
          "A hat with a PRESS card and a worn coat; the flash is a little yellow. Hit sounds become lighter.",
          "PRESSの札をつけた帽子と古びたコート。フラッシュがやや黄色いです。被弾音が軽くなります。",
          "帽子上插着PRESS卡片，穿着旧外套，闪光灯略带黄色。受击声变得轻快。");
        S("즉석 카메라", "Instant Camera", "インスタントカメラ", "拍立得");
        S("무지개 줄무늬 재킷에 즉석 카메라, 인화 사본이 흰 테 사진으로 나옵니다. 피격하면 삑 소리가 납니다.",
          "A rainbow-striped jacket and an instant camera; print copies come out as white-bordered photos. Getting hit beeps.",
          "虹色ストライプのジャケットとインスタントカメラ。焼き増しは白枠の写真で出てきます。被弾するとピッと鳴ります。",
          "彩虹条纹夹克配拍立得，复印件会变成白边照片。受击时发出滴声。");
        S("유령 사진가", "Ghost Photographer", "幽霊カメラマン", "幽灵摄影师");
        S("반투명한 몸에 보라 플래시, 찍힌 적 뒤로 희미한 영혼이 함께 찍힙니다. 움직이면 잔상이 남고, 피격하면 메아리가 울립니다.",
          "A translucent body with a purple flash; a faint soul is photographed behind each captured foe. Moving leaves afterimages; getting hit echoes.",
          "半透明の体に紫のフラッシュ。撮られた敵の後ろにかすかな魂も一緒に写ります。動くと残像が残り、被弾するとこだまが響きます。",
          "半透明的身体配紫色闪光，被拍到的敌人身后会拍进一缕淡淡的灵魂。移动时留下残影，受击时回声回荡。");
        S("스크린샷 F12", "Screenshot F12", "スクリーンショットF12", "截图 F12");
        S("카메라가 F12 키, 셔터는 촤착, 적은 픽셀 캡처 조각이 되고 처치하면 썸네일로 줄어듭니다. 파노라마 때 모서리에 「스크린샷 저장됨」이 뜹니다. (업적형)",
          "The camera is the F12 key and the shutter goes click-clack; foes become pixel-capture fragments and shrink into thumbnails when defeated. During the panorama, \"Screenshot saved\" pops up in the corner. (Achievement)",
          "カメラがF12キー、シャッターはシャキッ。敵はピクセルのキャプチャ片になり、倒すとサムネイルに縮みます。パノラマ時は隅に「スクリーンショットを保存しました」が出ます。(実績型)",
          "相机变成F12键，快门声咔嚓。敌人化作像素截图碎片，击杀后缩成缩略图。全景时角落会弹出「截图已保存」。(成就型)");

        // ---------------- 무기 스킨
        S("곰 인형", "Teddy Bear", "くまのぬいぐるみ", "小熊玩偶");
        S("더미가 낡은 곰 인형으로 바뀌고, 휘두를 때 솜이 날립니다. 묵직한 소리가 더해집니다.",
          "The dummy becomes a worn teddy bear and stuffing flies when it swings. A heavy thud is added.",
          "ダミーが古いくまのぬいぐるみになり、振ると綿が舞います。重い音が加わります。",
          "假人变成旧小熊玩偶，挥动时棉絮飞舞。附加沉重的声音。");
        S("목각 인형", "Wooden Doll", "木彫り人形", "木偶");
        S("더미가 관절 목각 인형으로 바뀌고, 휘두를 때 딱 나무 소리가 납니다.",
          "The dummy becomes a jointed wooden doll and swings with a sharp wooden clack.",
          "ダミーが関節つきの木彫り人形になり、振るとカツンと木の音がします。",
          "假人变成关节木偶，挥动时发出清脆的木头声。");
        S("곡괭이", "Pickaxe", "つるはし", "鹤嘴锄");
        S("곡괭이로 팝니다. 흙 튀김이 돌 조각으로 바뀌고 묵직한 쇳소리가 더해집니다.",
          "Dig with a pickaxe. Flying dirt turns to rock chips and a heavy iron clang is added.",
          "つるはしで掘ります。飛び散る土が石のかけらになり、重い金属音が加わります。",
          "用鹤嘴锄挖掘。飞溅的泥土变成碎石，并附加沉重的金属声。");
        S("은수저", "Silver Spoon", "銀のスプーン", "银勺");
        S("은수저로 팝니다. 사각 작은 소리가 나고 구멍이 반짝입니다.",
          "Dig with a silver spoon. It makes a small scraping sound and the hole glitters.",
          "銀のスプーンで掘ります。サクッと小さな音がして、穴がきらめきます。",
          "用银勺挖掘。发出细小的沙沙声，坑洞闪闪发亮。");
        S("일회용 카메라", "Disposable Camera", "使い捨てカメラ", "一次性相机");
        S("종이 상자 카메라, 셔터 소리가 지익 딸깍 납니다.",
          "A cardboard camera; the shutter goes zzzt-click.",
          "紙箱のカメラ。シャッター音はジジッ、カチッと鳴ります。",
          "纸盒相机，快门声是「吱——咔嗒」。");
        S("필름 카메라", "Film Camera", "フィルムカメラ", "胶片相机");
        S("주름 상자가 달린 옛 대형 카메라, 플래시 가루가 펑 터집니다.",
          "An old large camera with a bellows; the flash powder bursts with a bang.",
          "蛇腹のついた古い大型カメラ。フラッシュ粉がボンと弾けます。",
          "带有风箱的老式大相机，闪光粉砰地炸开。");
    }
}
