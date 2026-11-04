using UnityEngine;

// 번역표 (2.2.0~): 새 영웅 셋(인형사 · 장의사 · 사진사)의 캐릭터 스킨 이름 12 · 설명 12 · 무기 스킨 이름 6 · 설명 6 (SkinData), 상점 미리보기 무대 문구
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
        S("검은 종이 실루엣 연출가, 더미도 그림자 인형이 되어 벽에 비친 듯 납작합니다. 레벨업하면 새 실루엣이 날아오르며 징이 울리고, 쓰러지면 촛불이 꺼집니다.",
          "A director cut from black paper; the dummy becomes a shadow puppet, flat as if cast on a wall. A bird silhouette takes flight with a gong on level-up, and when you fall, the candle goes out.",
          "黒い紙の影絵の演出家。ダミーも影絵人形になり、壁に映ったように平たくなります。レベルアップで鳥の影が舞い上がって銅鑼が鳴り、倒れるとろうそくが消えます。",
          "黑纸剪影般的演出者，假人也变成皮影，像映在墙上一样扁平。升级时飞鸟剪影腾空而起、锣声响起，倒下时烛火熄灭。");
        S("종이 인형", "Paper Doll", "紙人形", "纸娃娃");
        S("접은 종이 옷의 연출가, 더미는 접이식 종이 인형이고 줄은 실 대신 종이끈입니다. 출발하면 종이학이 날아오르고, 레벨업하면 종이꽃이 피며 오르골이 울립니다.",
          "A director in folded paper clothes; the dummy is a folding paper doll and the strings are paper cords. Paper cranes take off as you start moving, and a paper flower blooms to a music box on level-up.",
          "折り紙の服の演出家。ダミーは折りたたみ式の紙人形で、糸の代わりに紙ひもです。動き出すと折り鶴が飛び立ち、レベルアップで紙の花が咲きオルゴールが鳴ります。",
          "身穿折纸衣装的导演，人偶是可折叠的纸人偶，提线换成了纸绳。起步时纸鹤飞起，升级时纸花绽放、八音盒响起。");
        S("묘역 인형사", "Graveyard Puppeteer", "墓地の人形師", "墓地人偶师");
        S("묘역 무대의 연출가, 더미가 해골 인형이 되어 휘두를 때 뼈가 달그락거립니다. 움직이면 영혼불 잔상이 남고, 레벨업하면 해골이 춤추며 솟고, 필살기엔 막이 내립니다.",
          "A director of the graveyard stage; the dummy becomes a skeleton puppet whose bones rattle as it swings. Moving leaves soulfire trails, skulls dance upward on level-up, and a curtain falls for your ultimate.",
          "墓地の舞台の演出家。ダミーがどくろ人形になり、振るたびに骨がカタカタ鳴ります。動くと魂火の残像が残り、レベルアップでどくろが踊りながら昇り、必殺技では幕が下ります。",
          "墓园舞台的导演，人偶化作骷髅人偶，挥舞时骨头咔嗒作响。移动留下魂火残影，升级时骷髅舞动升起，必杀技时帷幕落下。");
        S("개발자의 손", "The Developer's Hand", "開発者の手", "开发者之手");
        S("더미가 마우스 커서, 줄이 케이블, 커튼콜은 커서 여럿이 따닥따닥 클릭합니다. 처치하면 작은 창이 틱 닫히고 발밑에 작업 표시줄 띠가 깔리며, 레벨업하면 코드 줄이 써집니다. (업적형)",
          "The dummy is a mouse cursor and the strings are cables; the curtain call is a flurry of cursors clicking. Defeated foes vanish like closing windows, a taskbar strip lies underfoot, and lines of code write out on level-up. (Achievement)",
          "ダミーはマウスカーソル、糸はケーブル。カーテンコールは複数のカーソルがカチカチとクリックします。倒すと小さなウィンドウがパチッと閉じ、足元にタスクバーが敷かれ、レベルアップでコードの行が書かれます。(実績型)",
          "假人是鼠标光标，丝线是电缆，谢幕时多个光标噼啪点击。击杀后小窗口啪地关闭，脚下铺着任务栏条，升级时写出一行行代码。(成就型)");

        // ---------------- 장의사
        S("묘지기", "Gravekeeper", "墓守", "守墓人");
        S("랜턴을 든 밤 묘지기, 구멍 가장자리에 랜턴 불빛이 비칩니다. 젖은 흙 발소리, 레벨업하면 반딧불 기둥과 부엉이 소리, 쓰러지면 랜턴이 떨어져 꺼집니다.",
          "A night gravekeeper with a lantern, its light glowing at the edge of each grave. Footsteps in wet soil, a pillar of fireflies and an owl's hoot on level-up, and when you fall, the lantern drops and goes out.",
          "ランタンを持った夜の墓守。穴の縁にランタンの灯りが映ります。湿った土の足音、レベルアップで蛍の柱とフクロウの声、倒れるとランタンが落ちて消えます。",
          "提着灯笼的守夜墓人，坑洞边缘映着灯笼的光。脚踩湿土的声响，升级时萤火虫光柱与猫头鹰鸣叫，倒下时灯笼坠地熄灭。");
        S("석공", "Stonemason", "石工", "石匠");
        S("돌가루 앉은 모자에 앞치마의 석공, 묘비 대신 작은 조각상이 솟습니다. 평타마다 정 소리 「탕」, 레벨업하면 조각상이 솟고 쓰러지면 돌로 굳습니다.",
          "A stonemason in a dusty cap and apron; small statues rise instead of gravestones. Each basic attack rings a chisel tap, a statue rises on level-up, and you turn to stone when you fall.",
          "石粉をかぶった帽子とエプロンの石工。墓石の代わりに小さな彫像が立ちます。通常攻撃ごとにノミの「カン」、レベルアップで彫像がそびえ、倒れると石に固まります。",
          "帽上落满石粉、系着围裙的石匠，竖起的不是墓碑而是小雕像。每次普攻凿子「当」一声，升级时雕像升起，倒下时化为石头。");
        S("지옥 장의사", "Hellish Undertaker", "地獄の葬儀屋", "地狱殡葬师");
        S("지옥의 묘지기, 묘비가 불꽃 표식을 달고 솟습니다. 구덩이 안이 주황빛으로 타오르고, 쉬면 검은 백합이 피며, 레벨업하면 화환이 뜨며 조종이 울립니다.",
          "A gravekeeper from hell; gravestones rise bearing a flame mark. Pits glow orange inside, black lilies bloom when idle, and a wreath appears to a tolling bell on level-up.",
          "地獄の墓守。墓石が炎の印をつけてそびえます。穴の中が橙色に燃え、休むと黒い百合が咲き、レベルアップで花輪が浮かび弔いの鐘が鳴ります。",
          "地狱的守墓人，墓碑带着火焰印记升起。坑内燃着橙光，静止时黑百合绽放，升级时花环浮现、丧钟鸣响。");
        S("미완성 묘비", "Unfinished Gravestone", "未完成の墓石", "未完成的墓碑");
        S("비문이 「TODO: 비문 쓰기」인 회색 상자 묘비. 매장 때 「저장 중…」 모래시계가 돌고 띵 저장음이 울리며, 처치하면 디스켓이 날아들고 이기면 디스켓이 쏟아집니다. (업적형)",
          "A gray-box gravestone whose epitaph reads 'TODO: write epitaph'. Burials show a 'Saving...' hourglass and a ding; kills pull floppy disks in, and disks pour out when you win. (Achievement)",
          "墓碑銘が「TODO: 碑文を書く」の灰色の箱の墓石。埋葬時に「保存中…」の砂時計が回り、ポーンと保存音が鳴ります。倒すとフロッピーディスクが飛んできて、勝つとディスクがあふれ出します。(実績型)",
          "碑文写着「TODO: 写碑文」的灰色方块墓碑。安葬时出现「保存中…」沙漏并响起叮的存档音，击杀时软盘飞来，胜利时软盘倾泻而出。(成就型)");

        // ---------------- 사진사
        S("신문 기자", "Press Reporter", "新聞記者", "报社记者");
        S("기자 모자와 낡은 코트, 플래시가 조금 노랗습니다. 구두가 또각거리고, 레벨업하면 사진이 현상되듯 떠오르며, 필살기에 타자기 줄 바꿈 「띵」이 울립니다.",
          "A reporter's hat and a worn coat, with a slightly yellow flash. Your shoes click as you walk, a photo develops into view on level-up, and your ultimate rings with a typewriter's carriage-return ding.",
          "記者の帽子と古びたコート、フラッシュは少し黄色め。靴音がコツコツ響き、レベルアップで写真が現像されるように浮かび上がり、必殺技でタイプライターの改行「チン」が鳴ります。",
          "记者帽与旧外套，闪光灯略微发黄。皮鞋嗒嗒作响，升级时照片如显影般浮现，必杀技响起打字机换行的「叮」。");
        S("즉석 카메라", "Instant Camera", "インスタントカメラ", "拍立得");
        S("무지개 줄무늬 재킷에 즉석 카메라, 인화 사본이 흰 테 사진으로 나옵니다. 셔터가 하얗게 번쩍이고, 평타마다 사진이 「지잉」 나오며, 이기면 사진이 분수처럼 쏟아집니다.",
          "A rainbow-striped jacket and an instant camera; prints come out as white-framed photos. The flash goes white, every basic attack whirs out a photo, and photos fountain out when you win.",
          "虹色ストライプのジャケットにインスタントカメラ。焼き増しは白い縁の写真で出てきます。フラッシュが白く光り、通常攻撃のたびに写真が「ジー」と出て、勝つと写真が噴水のようにあふれます。",
          "彩虹条纹夹克配拍立得，冲印的副本是白边照片。闪光变为纯白，每次普攻「滋」地吐出一张照片，胜利时照片如喷泉般涌出。");
        S("유령 사진가", "Ghost Photographer", "幽霊カメラマン", "幽灵摄影师");
        S("반투명한 몸에 보라 플래시, 찍힌 적 뒤로 희미한 영혼이 함께 찍힙니다. 셔터가 두 번 메아리치고, 레벨업하면 심령사진이 떨어지며, 쓰러지면 세피아빛으로 바랩니다.",
          "A translucent body and a purple flash; faint spirits show up behind the enemies you shoot. The shutter echoes twice, a spirit photo drops on level-up, and you fade to sepia when you fall.",
          "半透明の体に紫のフラッシュ。写った敵の後ろにかすかな霊が一緒に写ります。シャッターが二度こだまし、レベルアップで心霊写真が落ち、倒れるとセピア色に褪せます。",
          "半透明的身体配紫色闪光，被拍到的敌人身后会一同拍进模糊的灵魂。快门声两度回响，升级时落下灵异照片，倒下时褪成棕褐色。");
        S("스크린샷 F12", "Screenshot F12", "スクリーンショットF12", "截图 F12");
        S("카메라가 F12 키, 셔터는 촤착, 적은 픽셀 캡처 조각이 되고 처치하면 썸네일로 줄어듭니다. 파노라마 때 모서리에 저장 완료 썸네일이 뜨고, 이기면 플래시가 연달아 터집니다. (업적형)",
          "The camera is the F12 key and the shutter goes click-clack; foes become pixel-capture fragments and shrink into thumbnails when defeated. During the panorama, a 'saved' thumbnail pops up in the corner, and victory sets off a string of flashes. (Achievement)",
          "カメラがF12キー、シャッターはシャキッ。敵はピクセルのキャプチャ片になり、倒すとサムネイルに縮みます。パノラマ時は隅に保存完了のサムネイルが出て、勝つとフラッシュが連続で光ります。(実績型)",
          "相机变成F12键，快门声咔嚓。敌人化作像素截图碎片，击杀后缩成缩略图。全景时角落会弹出保存完成的缩略图，胜利时闪光接连亮起。(成就型)");

        // ---------------- 무기 스킨
        S("곰 인형", "Teddy Bear", "くまのぬいぐるみ", "小熊玩偶");
        S("더미가 낡은 곰 인형으로 바뀌고, 휘두를 때 솜이 날립니다. 폭신하게 「퍽」, 맞히면 둔탁하게 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "The dummy becomes a worn teddy bear that sheds stuffing as it swings. Soft whumps, and hits land dull and heavy. Changes the look of the basic attack before evolution.",
          "ダミーが古いクマのぬいぐるみに変わり、振るうと綿が舞います。ふわっと「ボフッ」、命中すると鈍く響きます。進化前の通常攻撃の見た目が変わります。",
          "人偶变成旧泰迪熊，挥舞时棉絮飞扬。软软「噗」一声，命中时发出闷响。会改变进化前基础普攻的外观。");
        S("목각 인형", "Wooden Doll", "木彫り人形", "木偶");
        S("더미가 관절 목각 인형으로 바뀌고, 휘두를 때 「딱딱」 나무 소리가 납니다. 쉴 땐 나무 조각이 달그락, 큰 한 방엔 인형이 고개 숙여 인사하며 박수가 터집니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "The dummy becomes a jointed wooden doll that clacks as it swings. Wood chips rattle when idle, and big hits make the doll bow to applause. Changes the look of the basic attack before evolution.",
          "ダミーが関節つきの木彫り人形に変わり、振るうと「カタカタ」と木の音がします。休むと木片がカタカタ、大きな一撃では人形がおじぎをして拍手が起こります。進化前の通常攻撃の見た目が変わります。",
          "人偶变成关节木偶，挥舞时「哒哒」木头声。静止时木屑咔嗒作响，重击时木偶鞠躬、掌声响起。会改变进化前基础普攻的外观。");
        S("곡괭이", "Pickaxe", "つるはし", "鹤嘴锄");
        S("곡괭이로 팝니다. 흙 튀김이 돌 조각으로 바뀌고, 「깡」 쇳소리와 함께 맞히면 돌이 부서집니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "Digs with a pickaxe. Dirt sprays become stone chips with a metallic clang, and hits crunch like breaking rock. Changes the look of the basic attack before evolution.",
          "つるはしで掘ります。土しぶきが石のかけらに変わり、「カン」という金属音とともに、命中すると石が砕けます。進化前の通常攻撃の見た目が変わります。",
          "用鹤嘴锄挖掘。溅起的泥土变成碎石，伴着「铛」的金属声，命中时石块碎裂。会改变进化前基础普攻的外观。");
        S("은수저", "Silver Spoon", "銀のスプーン", "银勺");
        S("은수저로 팝니다. 「사각」 작은 소리에 구멍이 반짝이고, 큰 한 방엔 빛줄기가 솟으며, 진화하면 금빛으로 번쩍입니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "Digs with a silver spoon. Tiny scrapes leave sparkling holes, big hits send up a beam of light, and it flashes gold when it evolves. Changes the look of the basic attack before evolution.",
          "銀のスプーンで掘ります。「サクッ」と小さな音で穴がきらめき、大きな一撃では光の筋が昇り、進化すると金色に輝きます。進化前の通常攻撃の見た目が変わります。",
          "用银汤匙挖掘。「沙」的轻响中坑洞闪闪发亮，重击时光柱升起，进化时金光一闪。会改变进化前基础普攻的外观。");
        S("일회용 카메라", "Disposable Camera", "使い捨てカメラ", "一次性相机");
        S("종이 상자 카메라, 셔터 소리가 「지익 딸깍」 납니다. 찍을 때 필름 톱니가 돌고, 맞히면 짧은 셔터음이 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "A cardboard box camera whose shutter goes zzip-click. The film wheel spins with each shot, and hits sound a short shutter click. Changes the look of the basic attack before evolution.",
          "紙箱のカメラで、シャッター音は「ジーッ、カシャ」。撮るとフィルムの歯車が回り、命中すると短いシャッター音が鳴ります。進化前の通常攻撃の見た目が変わります。",
          "纸盒相机，快门声「滋—咔嚓」。拍摄时胶卷齿轮转动，命中时响起短促快门声。会改变进化前基础普攻的外观。");
        S("필름 카메라", "Film Camera", "フィルムカメラ", "胶片相机");
        S("주름 상자가 달린 옛 대형 카메라, 플래시 가루가 「펑」 터집니다. 장전하면 필름을 감고, 큰 한 방엔 커다란 사진과 박수가 터집니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "An old large-format camera with bellows; the flash powder goes poof. Reloads wind the film, and big hits pop a huge photo to applause. Changes the look of the basic attack before evolution.",
          "蛇腹のついた昔の大判カメラで、フラッシュの粉が「ボン」と弾けます。リロードでフィルムを巻き、大きな一撃では大きな写真と拍手が飛び出します。進化前の通常攻撃の見た目が変わります。",
          "带折叠皮腔的老式大画幅相机，闪光粉「砰」地炸开。装填时卷动胶片，重击时巨幅照片弹出、掌声响起。会改变进化前基础普攻的外观。");

        // ---------------- 상점 미리보기 무대 (SkinPreview)
        S("미리보기", "Preview", "プレビュー", "预览");
        C("skin.preview.note", "걸음 · 공격 · 명중 · 처치 · 피격 · 레벨업 · 대기 · 필살기 · 승리 · 사망을 게임과 같은 연출 · 소리로 되풀이합니다 (판정 · 크기는 그대로, 다른 칸은 지금 낀 스킨)",
          "Loops steps, attack, hit, kill, damage, level-up, idle, ultimate, victory and death with the same effects and sounds as in-game (hitboxes and size unchanged; other slots show what you have equipped)",
          "足音・攻撃・命中・撃破・被弾・レベルアップ・待機・必殺技・勝利・死亡をゲームと同じ演出・音で繰り返します（判定・大きさはそのまま、他の枠は今の装備）",
          "以与游戏相同的特效和音效循环展示脚步、攻击、命中、击杀、受击、升级、待机、必杀技、胜利和阵亡（判定与大小不变，其他栏位为当前装备）");
    }
}
