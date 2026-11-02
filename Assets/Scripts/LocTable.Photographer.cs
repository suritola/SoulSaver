using UnityEngine;

// 번역표 (2.0.0): 새 영웅 사진사 — 이름 · 설명 · 카드 · 고유 스킬 · 진화 · 무기 · 강화 · 패시브 · 이야기 · 전령 · 기억 · 계약 · 보드 · 사막 비밀
// 키 앞글자 x. · xe. (hero-photographer.md) — 이미 있는 키는 건드리지 않음 (영웅 키 photo)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddPhotographerLines()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 정체 · 글
        A("사진사", "Photographer", "写真家", "摄影师");
        A("마감 직전의 사진사", "The Photographer at the Deadline", "締め切り直前の写真家", "临近截稿的摄影师");
        A("상점 페이지 스크린샷을 찍던 사진사. 「적이 많이 보이는 장면이 없다」는 지적을 듣고 사막까지 왔습니다. 뷰파인더에 담은 적을 납작한 종이로 만들고, 필름 한 통을 다 찍으면 가장 잘 나온 사진을 인화해 미끼로 세웁니다.",
          "A photographer who used to take store-page screenshots. Told that \"there is no scene with lots of enemies,\" he came all the way to the desert. Enemies caught in the viewfinder turn into flat sheets of paper, and when a roll of film runs out, the best shot is printed and set up as bait.",
          "ストアページのスクリーンショットを撮っていた写真家。「敵がたくさん映る場面がない」と言われ、砂漠までやって来ました。ファインダーに収めた敵を平らな紙に変え、フィルム1本を撮り終えると、一番よく撮れた写真を現像して囮として立てます。",
          "曾经拍摄商店页面截图的摄影师。被人指出“没有敌人很多的画面”，于是一路来到沙漠。取景框里的敌人会变成扁平的纸片，一卷胶卷拍完后，会把拍得最好的照片洗出来立成诱饵。");
        A("카메라", "Camera", "カメラ", "相机");
        A("마우스 자리에 네모난 뷰파인더가 따라다닙니다. 누르면 「찰칵」 — 틀 안의 적이 피해를 입고 잠깐 흰 테두리의 종이가 됩니다 (종이는 느리게 펄럭이고, 부딪혀도 아프지 않으며, 맞으면 크게 다칩니다). 틀 가운데에 담은 적은 초점이 맞아 더 아프고, 여섯 마리 넘게 담으면 「걸작」입니다. 필름 6장을 다 쓰면 저절로 감기면서 그 통에서 가장 잘 나온 사진을 마우스 자리에 인화해, 찍힌 적들의 종이 사본이 둘레의 적을 끌어모았다가 찢어지며 벱니다 (재장전 키로 일찍 감을 수도 있음)",
          "A square viewfinder follows the mouse. Press to go \"click\": enemies in the frame take damage and briefly become white-edged paper (paper drifts slowly, doesn't hurt on contact, and takes much more damage). Enemies in the center of the frame are in focus and take more; catching six or more makes it a \"masterpiece.\" When all 6 frames are used, the film rewinds on its own and prints the best shot of the roll at the mouse; the paper copies of the shot enemies draw in nearby enemies, then tear apart and slash (you can also rewind early with the reload key)",
          "マウスの位置に四角いファインダーがついてきます。押すと「カシャッ」— 枠の中の敵がダメージを受け、少しの間白い縁の紙になります（紙はゆっくりひらひら動き、触れても痛くなく、攻撃されると大きなダメージを受けます）。枠の中心に収めた敵はピントが合ってより痛く、6体以上収めると「傑作」です。フィルム6枚を使い切ると自動で巻き取られ、そのロールで一番よく撮れた写真をマウスの位置に現像し、写った敵の紙のコピーが周りの敵を引き寄せて、破れて斬りつけます（リロードキーで早く巻くこともできます）",
          "取景框会跟随鼠标。按下“咔嚓”——框内的敌人受到伤害，并短暂变成白边纸片（纸片缓慢飘动，接触不会受伤，受到的伤害大增）。框中心的敌人对焦清晰，受伤更重，拍到六个以上就是“杰作”。6张胶卷用完后会自动卷片，并把这一卷中拍得最好的照片洗在鼠标处，被拍到的敌人的纸片复制品会吸引周围敌人，随后撕裂并斩击（也可以用换弹键提前卷片）");
        A("파노라마", "Panorama", "パノラマ", "全景");
        A("누르고 있으면 화면을 가로지르는 촬영 띠가 보이고 (마우스로 위아래 · 오래 누를수록 두껍게), 떼면 셔터 커튼이 띠를 따라 한 번 지나가 그 안의 적을 모두 찍습니다. 찍힌 적은 종이가 되어 커튼이 지나간 쪽으로 바람에 날아갑니다",
          "Hold to show a shooting band across the screen (move it up and down with the mouse; the longer you hold, the thicker it gets). Release and a shutter curtain sweeps along the band once, shooting every enemy inside. Shot enemies turn to paper and blow away in the direction the curtain passed",
          "押し続けると画面を横切る撮影帯が表示され（マウスで上下、長く押すほど太くなる）、離すとシャッターカーテンが帯に沿って一度通り過ぎ、中の敵をすべて撮影します。撮られた敵は紙になり、カーテンが通った方向へ風に飛ばされます",
          "按住会显示横贯屏幕的拍摄带（用鼠标上下移动，按得越久越粗），松开后快门帘沿拍摄带扫过一次，拍下带内所有敌人。被拍到的敌人会变成纸片，被风吹向快门帘经过的方向");
        A("셔터 공격력", "Shutter Attack", "シャッター攻撃力", "快门攻击力");
        A("필름 — 남은 장 수. 다 쓰면 감으며 그 통에서 가장 잘 나온 사진을 마우스 자리에 인화합니다. ★ — 틀에 든 적 수 (여섯이면 걸작).",
          "Film: frames left. When it runs out, it rewinds and prints the best shot of the roll at the mouse. ★: number of enemies in the frame (six is a masterpiece).",
          "フィルム — 残り枚数。使い切ると巻き取りながら、そのロールで一番よく撮れた写真をマウスの位置に現像します。★ — 枠に入った敵の数（6体で傑作）。",
          "胶卷——剩余张数。用完后卷片，并把这一卷中拍得最好的照片洗在鼠标处。★——取景框内的敌人数（六个即为杰作）。");
        A("스크린샷 담당", "Screenshot Guy", "スクリーンショット担当", "截图负责人");
        A("스크린샷 저장 중...", "Saving screenshot...", "スクリーンショット保存中...", "正在保存截图...");
        A("이번 판 최고의 사진", "Best Photo of This Run", "今回のベストショット", "本局最佳照片");
        K("bossmem.noun.photographer", "사진", "photos", "写真", "照片");

        // ================================================================= 전용 레벨업 카드 6
        A("장노출", "Long Exposure", "長時間露光", "长曝光");
        A("셔터를 누른 뒤 틀이 잠시 남아, 그사이 틀에 들어온 적도 찍힙니다. (피해 30%)",
          "After the shutter, the frame lingers for a moment, and enemies that enter it meanwhile are shot too. (30% damage)",
          "シャッターを押した後も枠がしばらく残り、その間に枠に入った敵も撮影されます。（ダメージ30%）",
          "按下快门后取景框会短暂保留，期间进入框内的敌人也会被拍到。（伤害30%）");
        A("틀 유지", "Frame hold", "枠の維持", "取景框保留");
        A("광각 렌즈", "Wide-Angle Lens", "広角レンズ", "广角镜头");
        A("뷰파인더가 넓어집니다.", "The viewfinder gets wider.", "ファインダーが広がります。", "取景框变大。");
        A("가로세로", "Width and height", "縦横", "宽高");
        A("현상액", "Developer Fluid", "現像液", "显影液");
        A("종이가 된 적이 쓰러지면 종잇조각이 가까운 적에게 날아가 벱니다. (공격력 50% · 맞은 적은 0.8초 종이)",
          "When a paper enemy falls, paper scraps fly to nearby enemies and slash them. (50% attack · hit enemies become paper for 0.8s)",
          "紙になった敵が倒れると、紙片が近くの敵へ飛んで斬りつけます。（攻撃力50%・当たった敵は0.8秒紙になる）",
          "变成纸片的敌人倒下时，碎纸片会飞向附近的敌人并斩击。（攻击力50%·被击中的敌人变成纸片0.8秒）");
        A("조각 수", "Scraps", "紙片の数", "碎片数");
        A("바람에 날린 사진", "Photo Blown by the Wind", "風に舞う写真", "随风飘舞的照片");
        A("파노라마로 날아간 종이 적이 부딪힌 적도 종이가 되고, 부딪힘 피해가 늘어납니다.",
          "Enemies hit by paper enemies blown away by Panorama also turn to paper, and collision damage increases.",
          "パノラマで飛ばされた紙の敵がぶつかった敵も紙になり、衝突ダメージが増えます。",
          "被全景吹飞的纸片敌人撞到的敌人也会变成纸片，撞击伤害增加。");
        A("부딪힘 피해", "Collision damage", "衝突ダメージ", "撞击伤害");
        A("자동 초점", "Autofocus", "オートフォーカス", "自动对焦");
        A("커서 가까이에 있는 가장 큰 적에게 뷰파인더 가운데가 저절로 끌려갑니다.",
          "The center of the viewfinder is automatically drawn to the largest enemy near the cursor.",
          "ファインダーの中心が、カーソル近くにいる最も大きな敵へ自動で引き寄せられます。",
          "取景框中心会自动被吸向光标附近最大的敌人。");
        A("끌리는 거리", "Pull distance", "引き寄せ距離", "吸附距离");
        A("인화지 추가", "Extra Photo Paper", "印画紙追加", "增加相纸");
        A("인화할 때 사본이 늘고 미끼가 오래 갑니다. (단계마다 미끼 +0.5초)",
          "More copies are printed and the bait lasts longer. (+0.5s bait per level)",
          "現像時にコピーが増え、囮が長持ちします。（段階ごとに囮+0.5秒）",
          "洗印时复制品增多，诱饵持续更久。（每级诱饵+0.5秒）");
        A("사본 수", "Copies", "コピー数", "复制品数");

        // ================================================================= 고유 스킬 10
        A("플래시 반사", "Flash Reflection", "フラッシュ反射", "闪光反射");
        A("셔터를 누르면 틀 안의 적 투사체가 종이가 되어 떨어집니다.", "When you press the shutter, enemy projectiles in the frame turn to paper and fall.", "シャッターを押すと、枠内の敵の投射物が紙になって落ちます。", "按下快门时，框内的敌方投射物会变成纸片落下。");
        A("지우는 틀 크기", "Erase frame size", "消去する枠の大きさ", "清除框大小");
        A("셀카", "Selfie", "自撮り", "自拍");
        A("틀 안에 내 몸을 넣고 찍으면 체력을 회복하고 잠깐 무적이 됩니다. (2초에 한 번)", "Shoot with yourself inside the frame to recover health and gain brief invincibility. (once per 2s)", "枠の中に自分の体を入れて撮ると、体力が回復し少しの間無敵になります。（2秒に1回）", "把自己放进取景框里拍摄，可恢复生命并短暂无敌。（每2秒一次）");
        A("셀프 타이머", "Self-Timer", "セルフタイマー", "自拍定时");
        A("필름을 감을 때 커서 자리에 타이머 카메라가 남아 2초 뒤 그 자리를 한 번 더 찍습니다.", "When the film rewinds, a timer camera is left at the cursor and shoots that spot once more 2 seconds later.", "フィルムを巻く時、カーソルの位置にタイマーカメラが残り、2秒後にその場所をもう一度撮影します。", "卷片时会在光标处留下一台定时相机，2秒后再次拍摄该处。");
        A("타이머 피해", "Timer damage", "タイマーのダメージ", "定时伤害");
        A("오려 붙이기", "Cut and Paste", "切り貼り", "剪贴");
        A("종이가 된 적을 다시 찍으면 접혀서 종이비행기가 되어 가까운 다른 적에게 날아갑니다. (맞은 적은 종이)", "Shooting a paper enemy again folds it into a paper plane that flies to another nearby enemy. (the hit enemy becomes paper)", "紙になった敵をもう一度撮ると、折りたたまれて紙飛行機になり、近くの別の敵へ飛んでいきます。（当たった敵は紙になる）", "再次拍摄已变成纸片的敌人，它会折成纸飞机飞向附近的其他敌人。（被击中的敌人变成纸片）");
        A("비행기 피해", "Plane damage", "飛行機のダメージ", "纸飞机伤害");
        A("노출 과다", "Overexposure", "露出オーバー", "过度曝光");
        A("걸작 사진은 틀이 하얗게 타 2초 동안 0.5초마다 안의 적을 벱니다.", "A masterpiece burns the frame white, slashing enemies inside every 0.5s for 2 seconds.", "傑作の写真は枠が真っ白に焼け、2秒間0.5秒ごとに中の敵を斬ります。", "杰作照片会让取景框烧成一片白，2秒内每0.5秒斩击框内敌人。");
        A("장판 피해", "Zone damage", "領域ダメージ", "区域伤害");
        A("흔들린 사진", "Blurred Photo", "ブレた写真", "抖动的照片");
        A("움직이며 찍으면 틀이 이동 방향으로 길게 늘어납니다.", "When you shoot while moving, the frame stretches long in your direction of movement.", "動きながら撮ると、枠が移動方向に長く伸びます。", "移动中拍摄时，取景框会沿移动方向拉长。");
        A("늘어남", "Stretch", "伸び", "拉长");
        A("앨범", "Album", "アルバム", "相册");
        A("종이가 된 적을 열 마리 쓰러뜨릴 때마다 필름 1장을 채우고 필살기 게이지를 받습니다.", "Every ten paper enemies you defeat refills 1 frame of film and grants ultimate gauge.", "紙になった敵を10体倒すごとに、フィルムが1枚補充され、必殺技ゲージを獲得します。", "每击败十个纸片敌人，就补充1张胶卷并获得必杀技能量。");
        A("셔터 찬스", "Shutter Chance", "シャッターチャンス", "快门良机");
        A("스킬을 쓰려고 예고하는 적을 찍으면 그 스킬이 끊기고 더 아픕니다. (보스는 끊기지 않고 피해만)", "Shooting an enemy that is winding up a skill cancels it and hurts more. (bosses are not interrupted, damage only)", "スキルを使おうと予告している敵を撮ると、そのスキルが中断されより痛くなります。（ボスは中断されずダメージのみ）", "拍摄正在预告技能的敌人会打断该技能并造成更高伤害。（首领不会被打断，只有伤害）");
        A("현상소", "Darkroom", "現像所", "冲印室");
        A("인화 사본이 찢어질 때 둘레의 적을 끌어당깁니다. (보스 제외)", "When a printed copy tears, it pulls in nearby enemies. (bosses excluded)", "印刷したコピーが破れる時、周りの敵を引き寄せます。（ボス除く）", "洗印的复制品撕裂时会吸引周围敌人。（首领除外）");
        A("끌어당김", "Pull", "引き寄せ", "吸引");
        A("기념 사진", "Commemorative Photo", "記念写真", "纪念照");
        A("보스와 일반 적을 한 틀에 함께 찍으면, 함께 찍힌 일반 적 수만큼 보스가 더 다칩니다. (최대 6마리)", "Shooting a boss and regular enemies in one frame hurts the boss more for each regular enemy caught with it. (up to 6)", "ボスと一般の敵を一つの枠で撮ると、一緒に撮れた一般の敵の数だけボスがより大きなダメージを受けます。（最大6体）", "把首领和普通敌人拍进同一取景框时，同框的普通敌人越多，首领受到的伤害越高。（最多6个）");
        A("마리당", "Per enemy", "1体あたり", "每个");

        // ================================================================= 스킬 진화 7
        A("야경", "Night Scene", "夜景", "夜景");
        A("남은 틀이 커서를 따라 끌려가며 1초 동안 빛 궤적을 남깁니다. 궤적에 닿은 적은 0.3초마다 공격력 25%를 받습니다.", "The lingering frame follows the cursor for 1 second, leaving a light trail. Enemies touching the trail take 25% attack every 0.3s.", "残った枠がカーソルに引かれて動き、1秒間光の軌跡を残します。軌跡に触れた敵は0.3秒ごとに攻撃力25%のダメージを受けます。", "残留的取景框会跟随光标移动1秒并留下光轨。接触光轨的敌人每0.3秒受到攻击力25%的伤害。");
        A("추적 초점", "Tracking Focus", "追尾フォーカス", "追踪对焦");
        A("자동 초점이 체력이 가장 많은 적을 잠그고, 잠긴 적이 예고 중이면 언제 찍어도 셔터 찬스 + 1초 기절입니다. (보스는 피해만)", "Autofocus locks onto the enemy with the most health, and if the locked enemy is winding up, any shot counts as Shutter Chance + 1s stun. (bosses: damage only)", "オートフォーカスが体力の最も多い敵をロックし、ロックした敵が予告中なら、いつ撮ってもシャッターチャンス＋1秒気絶になります。（ボスはダメージのみ）", "自动对焦会锁定生命最多的敌人，被锁定的敌人处于预告状态时，任何时候拍摄都视为快门良机＋眩晕1秒。（首领只有伤害）");
        A("스튜디오", "Studio", "スタジオ", "摄影棚");
        A("인화 자리에 조명이 3초 서서 사본에 끌린 적을 0.5초마다 1초 종이로 만들고, 사본이 찢어질 때 끌어당긴 적을 모두 종이로 만듭니다.", "A light stands at the print spot for 3 seconds, turning enemies drawn to the copies into paper for 1s every 0.5s, and when a copy tears, all pulled enemies become paper.", "現像場所に照明が3秒間立ち、コピーに引かれた敵を0.5秒ごとに1秒間紙にし、コピーが破れる時に引き寄せた敵をすべて紙にします。", "洗印处会立起一盏灯3秒，每0.5秒把被复制品吸引的敌人变成纸片1秒，复制品撕裂时把所有被吸引的敌人变成纸片。");
        A("색종이 폭풍", "Confetti Storm", "紙吹雪の嵐", "彩纸风暴");
        A("종잇조각이 맞힌 적에게서 조각 한 장이 더 튀고 (최대 2세대), 종이비행기가 적 셋을 꿰뚫습니다.", "Paper scraps spawn one more scrap from the enemy they hit (up to 2 generations), and paper planes pierce three enemies.", "紙片が当たった敵からさらに紙片が1枚飛び（最大2世代）、紙飛行機が敵3体を貫きます。", "碎纸片击中敌人后会再弹出一片（最多2代），纸飞机会贯穿三个敌人。");
        A("태풍 파노라마", "Typhoon Panorama", "台風パノラマ", "台风全景");
        A("파노라마 커튼이 되돌아오며 한 번 더 지나가고, 날아간 종이 적이 화면 끝에서 되튕겨 한 번 더 부딪힙니다.", "The Panorama curtain returns and sweeps once more, and blown-away paper enemies bounce back and collide once more.", "パノラマのカーテンが戻ってもう一度通り過ぎ、飛ばされた紙の敵が跳ね返ってもう一度ぶつかります。", "全景快门帘折返再扫过一次，被吹飞的纸片敌人会弹回并再撞击一次。");
        A("플립북", "Flipbook", "パラパラ漫画", "翻页书");
        A("걸작을 찍으면 앨범이 넘어가며 같은 틀이 0.4초 간격으로 세 번 더 찍힙니다. (피해 40%)", "Shooting a masterpiece flips the album, shooting the same frame three more times at 0.4s intervals. (40% damage)", "傑作を撮るとアルバムがめくれ、同じ枠が0.4秒間隔でさらに3回撮影されます。（ダメージ40%）", "拍到杰作时相册翻页，同一取景框每隔0.4秒再拍三次。（伤害40%）");
        A("기념 앨범", "Memorial Album", "記念アルバム", "纪念相册");
        A("셀프 타이머도 기념 사진이 되고, 타이머 카메라가 두 대 섭니다.", "Self-Timer shots also count as commemorative photos, and two timer cameras are set up.", "セルフタイマーも記念写真になり、タイマーカメラが2台立ちます。", "定时自拍也算纪念照，并会架起两台定时相机。");

        // ================================================================= 무기 · 패시브 · 궁극기
        A("플래시 전구", "Flash Bulb", "フラッシュ電球", "闪光灯泡");
        A("삼각대 카메라", "Tripod Camera", "三脚カメラ", "三脚相机");
        A("영사기", "Projector", "映写機", "放映机");
        A("셔터를 누를 때마다 틀 둘레 1칸 테두리에도 전구 불꽃이 튀어 피해를 줍니다. (피해 40%)\n궁극기: 불꽃 플래시",
          "Each time you press the shutter, bulb sparks also fly in a 1-tile border around the frame, dealing damage. (40% damage)\nUltimate: Flare Flash",
          "シャッターを押すたびに、枠の周り1マスの縁にも電球の火花が散ってダメージを与えます。（ダメージ40%）\n究極技: 火花フラッシュ",
          "每次按下快门，取景框外1格的边缘也会迸出灯泡火花造成伤害。（伤害40%）\n终极技: 火花闪光");
        A("3초마다 마지막으로 찍은 자리에 삼각대 카메라가 서서 5초 동안 1.2초마다 그 자리를 찍습니다. (피해 50% · 종이 포함 · 최대 2대)\n궁극기: 감시 카메라",
          "Every 3 seconds a tripod camera is set up where you last shot, and shoots that spot every 1.2s for 5 seconds. (50% damage, includes paper, max 2)\nUltimate: Surveillance Cameras",
          "3秒ごとに最後に撮った場所に三脚カメラが立ち、5秒間1.2秒ごとにその場所を撮影します。（ダメージ50%・紙化を含む・最大2台）\n究極技: 監視カメラ",
          "每3秒在最后拍摄的位置架起一台三脚相机，5秒内每1.2秒拍摄该处。（伤害50%·含纸化·最多2台）\n终极技: 监视摄像头");
        A("한 장에 찍힌 종이 적들이 필름 줄로 이어져, 줄을 가로지르는 적이 0.4초마다 베입니다. (피해 30%)\n궁극기: 상영회",
          "Paper enemies shot in one frame are linked by a film strip, and enemies crossing the strip are slashed every 0.4s. (30% damage)\nUltimate: Screening",
          "1枚に撮られた紙の敵がフィルムの帯でつながり、帯を横切る敵が0.4秒ごとに斬られます。（ダメージ30%）\n究極技: 上映会",
          "同一张照片里的纸片敌人被胶片带相连，穿过胶片带的敌人每0.4秒被斩击。（伤害30%）\n终极技: 放映会");
        A("필름이 1장 늘어납니다.\n진화: 2장", "Adds 1 frame of film.\nEvolved: 2 frames", "フィルムが1枚増えます。\n進化: 2枚", "胶卷增加1张。\n进化: 2张");
        A("필름 보관함", "Film Case", "フィルムケース", "胶卷盒");
        A("걸작 사진을 찍으면 체력을 2 회복합니다.\n진화: 4", "Taking a masterpiece heals 2 health.\nEvolved: 4", "傑作を撮ると体力を2回復します。\n進化: 4", "拍到杰作时恢复2点生命。\n进化: 4");
        A("모델 섭외", "Model Casting", "モデル手配", "模特邀约");
        A("셔터를 누를 때 내 둘레 2칸의 적을 밀쳐 냅니다.\n진화: 3칸", "Pressing the shutter pushes away enemies within 2 tiles of you.\nEvolved: 3 tiles", "シャッターを押すと、自分の周り2マスの敵を押しのけます。\n進化: 3マス", "按下快门时会推开自己周围2格内的敌人。\n进化: 3格");
        A("셔터 소리", "Shutter Sound", "シャッター音", "快门声");
        A("필름 감기가 20% 빨라집니다.\n진화: 35%", "Film rewinds 20% faster.\nEvolved: 35%", "フィルムの巻き取りが20%速くなります。\n進化: 35%", "卷片速度加快20%。\n进化: 35%");
        A("빠른 현상", "Quick Development", "早い現像", "快速冲洗");
        A("불꽃 플래시", "Flare Flash", "火花フラッシュ", "火花闪光");
        A("내 둘레에 전구 여덟 개가 차례로 터집니다. (공격력 128%씩)", "Eight bulbs burst one after another around you. (128% attack each)", "自分の周りで電球8個が順番に弾けます。（攻撃力128%ずつ）", "八个灯泡依次在你周围爆开。（每个攻击力128%）");
        A("감시 카메라", "Surveillance Cameras", "監視カメラ", "监视摄像头");
        A("카메라 네 대가 마우스 둘레에 서서 4초 동안 0.6초마다 찍습니다. (공격력 48%씩)", "Four cameras stand around the mouse and shoot every 0.6s for 4 seconds. (48% attack each)", "カメラ4台がマウスの周りに立ち、4秒間0.6秒ごとに撮影します。（攻撃力48%ずつ）", "四台相机立在鼠标周围，4秒内每0.6秒拍摄一次。（每次攻击力48%）");
        A("상영회", "Screening", "上映会", "放映会");
        A("마우스 자리에 4초 동안 빛 기둥 화면이 서서 안의 적을 0.3초마다 벱니다. (공격력 40%)", "A screen of light stands at the mouse for 4 seconds, slashing enemies inside every 0.3s. (40% attack)", "マウスの位置に4秒間光の柱のスクリーンが立ち、中の敵を0.3秒ごとに斬ります。（攻撃力40%）", "鼠标处会立起一块光柱屏幕4秒，每0.3秒斩击内部敌人。（攻击力40%）");
        A("셔터를 누를 때마다 틀 둘레 1칸 테두리에도 전구 불꽃이 튀어 피해를 줍니다. (피해 40%)", "Each time you press the shutter, bulb sparks also fly in a 1-tile border around the frame, dealing damage. (40% damage)", "シャッターを押すたびに、枠の周り1マスの縁にも電球の火花が散ってダメージを与えます。（ダメージ40%）", "每次按下快门，取景框外1格的边缘也会迸出灯泡火花造成伤害。（伤害40%）");
        A("3초마다 마지막으로 찍은 자리에 삼각대 카메라가 서서 5초 동안 1.2초마다 그 자리를 찍습니다. (피해 50% · 종이 포함 · 최대 2대)", "Every 3 seconds a tripod camera is set up where you last shot, and shoots that spot every 1.2s for 5 seconds. (50% damage, includes paper, max 2)", "3秒ごとに最後に撮った場所に三脚カメラが立ち、5秒間1.2秒ごとにその場所を撮影します。（ダメージ50%・紙化を含む・最大2台）", "每3秒在最后拍摄的位置架起一台三脚相机，5秒内每1.2秒拍摄该处。（伤害50%·含纸化·最多2台）");
        A("한 장에 찍힌 종이 적들이 필름 줄로 이어져, 줄을 가로지르는 적이 0.4초마다 베입니다. (피해 30%)", "Paper enemies shot in one frame are linked by a film strip, and enemies crossing the strip are slashed every 0.4s. (30% damage)", "1枚に撮られた紙の敵がフィルムの帯でつながり、帯を横切る敵が0.4秒ごとに斬られます。（ダメージ30%）", "同一张照片里的纸片敌人被胶片带相连，穿过胶片带的敌人每0.4秒被斩击。（伤害30%）");

        // ================================================================= 무기 강화 3
        A("이중 노출", "Double Exposure", "二重露光", "双重曝光");
        A("셔터를 누르고 0.3초 뒤 같은 틀이 한 번 더 찍힙니다. (피해 50% · 종이 판정 없음)", "0.3 seconds after pressing the shutter, the same frame is shot once more. (50% damage, no paper effect)", "シャッターを押して0.3秒後、同じ枠がもう一度撮影されます。（ダメージ50%・紙化なし）", "按下快门0.3秒后，同一取景框会再拍一次。（伤害50%·无纸化）");
        A("흑백 사진", "Black-and-White Photo", "白黒写真", "黑白照片");
        A("종이가 된 적이 받는 피해가 ×1.4에서 ×1.6이 됩니다.", "Damage taken by paper enemies goes from x1.4 to x1.6.", "紙になった敵が受けるダメージが×1.4から×1.6になります。", "纸片敌人受到的伤害从×1.4提高到×1.6。");
        A("보정", "Retouch", "補正", "修图");
        A("초점 원이 50% 커지고 초점 피해가 ×1.6에서 ×1.9가 됩니다.", "The focus circle grows 50% and focus damage goes from x1.6 to x1.9.", "フォーカス円が50%大きくなり、フォーカスダメージが×1.6から×1.9になります。", "对焦圈扩大50%，对焦伤害从×1.6提高到×1.9。");

        // ================================================================= 이야기
        A("사진사는 상점 페이지에 쓸 장면을 찾고 있었다. 적이 많이 보이는, 진짜 같은 장면을.", "The photographer was looking for a scene for the store page. One with lots of enemies, one that looked real.", "写真家はストアページに使う場面を探していた。敵がたくさん映る、本物らしい場面を。", "摄影师正在为商店页面寻找画面。一个敌人很多、看起来真实的画面。");
        A("“지금 사진은 너무 한가해 보여요.” 개발자의 말이 아직 귀에 남아 있었다.", "\"The current screenshot looks way too empty.\" The developer's words still rang in his ears.", "「今の写真は暇そうに見えます」。開発者の言葉がまだ耳に残っていた。", "“现在的截图看起来太冷清了。”开发者的话还在耳边回响。");
        A("사진사는 필름 한 통을 끼우고 초원으로 걸어 나갔다. 이번엔 꽉 찬 화면이다.", "The photographer loaded a roll of film and walked out onto the meadow. This time, a packed frame.", "写真家はフィルムを一本装填して草原へ歩き出した。今度は画面いっぱいだ。", "摄影师装上一卷胶卷，走向草原。这次要拍满满一屏。");
        A("사진사는 어둠 속에서 카메라를 들어 올렸다. 관이 줄지어 선 이만한 단체 사진은 흔치 않다.", "The photographer raised his camera in the darkness. A group photo with this many coffins in a row is rare.", "写真家は暗闇の中でカメラを持ち上げた。棺がずらりと並ぶ集合写真なんて、そうそうない。", "摄影师在黑暗中举起相机。棺材排成一列的合影可不常见。");
        A("“죽은 자도 사진에는 나오나 볼까.”", "\"Let's see if the dead show up in photos.\"", "「死者も写真には写るのか、見てみよう」", "“看看亡者能不能入镜。”");
        A("사진사는 플래시를 터뜨리며 신전의 구멍으로 내려갔다.", "The photographer popped his flash and went down into the hole in the temple.", "写真家はフラッシュを焚きながら神殿の穴へ降りていった。", "摄影师闪着闪光灯，走下神殿的洞口。");
        A("사진사는 마지막 한 장을 현상했다. 화면이 적으로 가득 찬, 더없이 북적이는 장면이었다.", "The photographer developed one last shot. A scene packed with enemies, as crowded as it gets.", "写真家は最後の一枚を現像した。画面が敵で埋め尽くされた、これ以上ないほど賑やかな場面だった。", "摄影师冲洗了最后一张照片。画面里挤满了敌人，再热闹不过。");
        A("“이걸로 상점 페이지 바꾸자.”", "\"Let's swap the store page with this.\"", "「これでストアページを変えよう」", "“就用这张换商店页面吧。”");
        A("하지만 현상된 사진 구석, 붉은 사막에 아무도 찍지 않은 삼각대가 하나 더 서 있었다...", "But in the corner of the developed photo, in the red desert, stood one more tripod that nobody had shot...", "しかし現像された写真の隅、赤い砂漠に、誰も撮っていない三脚がもう一つ立っていた...", "可是冲洗好的照片角落里，红色沙漠中，还立着一架没人拍过的三脚架...");

        // ================================================================= 전령 대사 6 · 비밀 힌트
        K("herald.hero.photo.1", "사진사가 쓰러졌다.\n“…노출이 너무 길었나. 다음 장은 빨리 찍는다.”",
          "The photographer has fallen.\n\"...Was the exposure too long? I'll shoot faster next frame.\"",
          "写真家が倒れた。\n「…露出が長すぎたか。次の一枚は早く撮る。」",
          "摄影师倒下了。\n“……曝光太久了吗。下一张我会拍快点。”");
        K("herald.hero.photo.2", "필름이 남은 채로 끝났다.\n“남은 장은 다음 판의 첫 장이야.”",
          "It ended with film left over.\n\"The leftover frames are the first frames of the next run.\"",
          "フィルムが残ったまま終わった。\n「残りの枚数は次の回の最初の一枚だ。」",
          "胶卷还剩着就结束了。\n“剩下的几张，就是下一局的第一张。”");
        K("herald.hero.photo.3", "종이로 만들지 못한 적이 너를 쓰러뜨렸다.\n“틀 밖이었어. 화면은 틀보다 넓으니까.”",
          "An enemy you couldn't turn to paper took you down.\n\"It was outside the frame. The screen is wider than the frame.\"",
          "紙にできなかった敵がお前を倒した。\n「枠の外だった。画面は枠より広いからな。」",
          "没能变成纸片的敌人击倒了你。\n“它在取景框外。屏幕可比取景框宽。”");
        K("herald.hero.photo.4", "사진 속 적은 웃지 않는다. 너도 웃지 않는다.\n“모델은 웃지 않아도 돼. 많기만 하면.”",
          "Enemies in photos don't smile. Neither do you.\n\"Models don't have to smile. They just have to be many.\"",
          "写真の中の敵は笑わない。お前も笑わない。\n「モデルは笑わなくていい。数さえ多ければ。」",
          "照片里的敌人不会笑。你也不笑。\n“模特不用笑，只要人多就行。”");
        K("herald.hero.photo.5", "리치 왕은 사진에 찍히지 않았다.\n“초점이 안 맞았을 뿐이야. 다시 간다.”",
          "The Lich King didn't show up in the photo.\n\"It was just out of focus. Going again.\"",
          "リッチ王は写真に写らなかった。\n「ピントが合わなかっただけだ。もう一度行く。」",
          "巫妖王没有被拍进照片。\n“只是没对上焦而已。再来一次。”");
        K("herald.hero.photo.6", "사막에 빈 삼각대가 있다던데. 네 것인가.\n“아니. 내 건 등에 있어. 그건… 누가 나를 찍으려는 거지.”",
          "They say there is an empty tripod in the desert. Is it yours?\n\"No. Mine is on my back. That one... someone is trying to shoot me.\"",
          "砂漠に空っぽの三脚があるそうだ。お前のものか。\n「いや。俺のは背中にある。あれは…誰かが俺を撮ろうとしているんだ。」",
          "听说沙漠里有一架空三脚架。是你的吗？\n“不是。我的在背上。那个……是有人想拍我。”");
        K("herald.secret.photographer", "사막에 빈 삼각대가 하나 서 있다. 무엇을 찍으려는지는 아무도 모른다.",
          "An empty tripod stands in the desert. No one knows what it means to shoot.",
          "砂漠に空っぽの三脚が一つ立っている。何を撮ろうとしているのかは誰も知らない。",
          "沙漠里立着一架空三脚架。没人知道它想拍什么。");
        K("herald.secret.photo.met", "사진사를 만났다. 현상하러 간다더니, 삼각대는 두고 갔다.",
          "You met the photographer. He said he was off to develop, but left the tripod behind.",
          "写真家に会った。現像しに行くと言いながら、三脚は置いていった。",
          "你遇见了摄影师。他说要去冲洗照片，却把三脚架留下了。");
        K("sec.hint.13", "누군가 북적이는 장면을 기다리고 있다. 빈 삼각대가 그쪽을 본다.",
          "Someone is waiting for a crowded scene. An empty tripod looks that way.",
          "誰かが賑やかな場面を待っている。空っぽの三脚がそちらを向いている。",
          "有人在等待一个热闹的场面。空三脚架望着那个方向。");
        K("secret.riddle.photographer", "누군가 북적이는 장면을 기다리고 있다. 빈 삼각대가 그쪽을 본다.",
          "Someone is waiting for a crowded scene. An empty tripod looks that way.",
          "誰かが賑やかな場面を待っている。空っぽの三脚がそちらを向いている。",
          "有人在等待一个热闹的场面。空三脚架望着那个方向。");
        K("secret.say.photographer", "좋아, 이걸로 상점 페이지 바꾸자!", "Great, let's swap the store page with this!", "いいね、これでストアページを変えよう！", "太好了，就用这个换商店页面吧！");
        K("secret.say.photographer.again", "또 북적이네. 이번 건 저장해 둘게.", "Crowded again. I'll save this one.", "また賑やかだな。これは保存しておくよ。", "又这么热闹。这张我存起来。");
        K("secret.say.photographer.self", "내 장면을 내가 찍힌 건가?", "Did I just get shot in my own scene?", "俺の場面で俺が撮られたのか？", "我的画面，我自己被拍进去了？");
        K("secret.say.photographer.bye", "현상하러 갈게요.", "I'll go develop these.", "現像しに行ってきます。", "我去冲洗了。");

        // ================================================================= 기억 조각 6
        K("memory.photo.1.title", "첫 사진", "First Photo", "最初の写真", "第一张照片");
        K("memory.photo.1.text", "비어 있는 상점 화면 스크린샷 한 장.\n뒷면에 “적 더 많이”라고 빨간 펜.\n“그 한 줄이 이 길의 시작이었죠.”",
          "One screenshot of an empty store page.\nOn the back, in red pen: \"more enemies.\"\n\"That one line was the start of this road.\"",
          "空っぽのストア画面のスクリーンショット一枚。\n裏には赤ペンで「敵をもっと」。\n「その一行がこの道の始まりでした。」",
          "一张空荡荡的商店页面截图。\n背面用红笔写着“敌人再多点”。\n“就是那一行字，成了这条路的起点。”");
        K("memory.photo.2.title", "다섯 번째 현상", "The Fifth Development", "5回目の現像", "第五次冲洗");
        K("memory.photo.2.text", "다섯 번 쓰러지고 나서야 알았다.\n좋은 사진은 서두르면 흔들린다는 것을.\n“흔들린 사진도 사진이긴 해요.”",
          "Only after falling five times did I learn\nthat a good photo blurs if you rush it.\n\"A blurry photo is still a photo.\"",
          "5回倒れてようやく分かった。\n良い写真は急ぐとブレるのだと。\n「ブレた写真も写真ではありますけどね。」",
          "倒下五次之后才明白，\n好照片一着急就会抖。\n“抖了的照片也算照片。”");
        K("memory.photo.3.title", "리치 왕의 단체 사진", "The Lich King's Group Photo", "リッチ王の集合写真", "巫妖王的合影");
        K("memory.photo.3.text", "리치 왕은 관 속 하객들과 함께 찍혔다.\n모두가 같은 표정이어서 편집은 쉬웠다.\n“웃어 달라고는 안 했어요.”",
          "The Lich King was photographed with his coffin guests.\nEveryone wore the same expression, so editing was easy.\n\"I never asked them to smile.\"",
          "リッチ王は棺の中の来客たちと一緒に撮られた。\n皆が同じ表情だったので編集は楽だった。\n「笑ってくれとは頼んでませんよ。」",
          "巫妖王和棺中的宾客一起入了镜。\n大家表情都一样，修图很容易。\n“我可没让他们笑。”");
        K("memory.photo.4.title", "어려운 구도", "A Hard Composition", "難しい構図", "困难的构图");
        K("memory.photo.4.text", "어려운 날일수록 구도는 단순해진다.\n한가운데, 가장 큰 놈 하나.\n“초점은 거기서부터 시작이에요.”",
          "The harder the day, the simpler the composition.\nDead center, the biggest one.\n\"Focus starts from there.\"",
          "厳しい日ほど構図は単純になる。\n真ん中に、一番大きいやつ一つ。\n「ピントはそこから始まります。」",
          "越是艰难的日子，构图越简单。\n正中央，最大的那一个。\n“对焦就从那里开始。”");
        K("memory.photo.5.title", "사막의 삼각대", "The Desert Tripod", "砂漠の三脚", "沙漠里的三脚架");
        K("memory.photo.5.text", "사막에 빈 삼각대가 하나 서 있다.\n누가 세웠는지 기억나지 않는다.\n“내 건 등에 있으니까요.”",
          "An empty tripod stands in the desert.\nI can't remember who set it up.\n\"Because mine is on my back.\"",
          "砂漠に空っぽの三脚が一つ立っている。\n誰が立てたのか思い出せない。\n「俺のは背中にありますからね。」",
          "沙漠里立着一架空三脚架。\n不记得是谁架的。\n“因为我的在背上嘛。”");
        K("memory.photo.6.title", "마감", "The Deadline", "締め切り", "截稿");
        K("memory.photo.6.text", "마감은 늘 오늘이다.\n그래서 필름은 늘 한 장이 남는다.\n“그 한 장은 내일을 위한 거예요.”",
          "The deadline is always today.\nSo there is always one frame of film left.\n\"That one frame is for tomorrow.\"",
          "締め切りはいつも今日だ。\nだからフィルムはいつも一枚残る。\n「その一枚は明日のためです。」",
          "截稿永远是今天。\n所以胶卷总会剩一张。\n“那一张是留给明天的。”");

        // ================================================================= 영혼 계약 · 보드
        K("contract.h.photo.1.name", "걸작 사진", "Masterpiece Shots", "傑作写真", "杰作照片");
        K("contract.h.photo.1.cond", "걸작 사진(한 틀에 적 6마리 이상) 20장 찍기", "Take 20 masterpiece shots (6+ enemies in one frame)", "傑作写真（1枠に敵6体以上）を20枚撮る", "拍摄20张杰作照片（一个取景框内6个以上敌人）");
        K("contract.h.photo.2.name", "종이 공장", "Paper Factory", "紙工場", "纸片工厂");
        K("contract.h.photo.2.cond", "적 300마리를 종이로 만들기", "Turn 300 enemies into paper", "敵300体を紙にする", "把300个敌人变成纸片");
        K("contract.h.photo.3.name", "파노라마", "Panorama", "パノラマ", "全景");
        K("contract.h.photo.3.cond", "필살기 10번 쓰기", "Use the ultimate 10 times", "必殺技を10回使う", "使用必杀技10次");
        K("contract.h.photo.4.name", "인화 찢기", "Tearing Prints", "プリント破り", "撕裂洗印");
        K("contract.h.photo.4.cond", "인화 사본 40장 찢기", "Tear 40 printed copies", "プリントのコピーを40枚破る", "撕裂40张洗印复制品");
        K("contract.h.photo.6.name", "기념 사진", "Commemorative Photos", "記念写真", "纪念照");
        K("contract.h.photo.6.cond", "보스·중간 보스와 일반 적을 한 틀에 15번 찍기", "Shoot a boss or mid-boss with regular enemies in one frame 15 times", "ボス・中ボスと一般の敵を1枠で15回撮る", "把首领或中型首领与普通敌人拍进同一取景框15次");
        K("board.photo.1.name", "삼천 번의 셔터", "Three Thousand Shutters", "三千回のシャッター", "三千次快门");
        K("board.photo.1.desc", "찰칵, 찰칵, 또 찰칵. 손가락 지문이 셔터 버튼에 새겨졌습니다.", "Click, click, click again. Your fingerprint is now engraved on the shutter button.", "カシャ、カシャ、またカシャ。指紋がシャッターボタンに刻まれました。", "咔嚓，咔嚓，又是咔嚓。指纹已经刻在了快门按钮上。");
        K("board.photo.1.cond", "셔터 3000번 누르기", "Press the shutter 3000 times", "シャッターを3000回押す", "按快门3000次");
        K("board.photo.2.name", "네 왕을 담은 필름", "Film That Held Four Kings", "四王を収めたフィルム", "装下四王的胶卷");
        K("board.photo.2.desc", "왕들도 렌즈 앞에서는 한 장의 사진일 뿐이었습니다.", "Even kings were just a single photograph in front of the lens.", "王たちもレンズの前では一枚の写真にすぎませんでした。", "即使是君王，在镜头前也不过是一张照片。");
        K("board.photo.2.cond", "사진사로 장 보스 4종 모두 쓰러뜨리기", "Defeat all four chapter bosses as the Photographer", "写真家で章のボス4種をすべて倒す", "以摄影师击败全部四种章节首领");
        K("board.photo.3.name", "백오십 장의 사본", "A Hundred and Fifty Copies", "百五十枚のコピー", "一百五十张复制品");
        K("board.photo.3.desc", "인화한 종이는 늘 한 번 쓰이고 찢어집니다. 사진사는 그게 가장 좋은 쓰임이라 믿습니다.", "A printed sheet is used once and torn. The photographer believes that is its best use.", "現像した紙は一度使われて破れます。写真家はそれが一番いい使い道だと信じています。", "洗印的纸片只用一次就会被撕碎。摄影师相信这就是它最好的用途。");
        K("board.photo.3.cond", "인화 사본 150장 찢기", "Tear 150 printed copies", "プリントのコピーを150枚破る", "撕裂150张洗印复制品");
    }
}
