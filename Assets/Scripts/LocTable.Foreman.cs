using UnityEngine;

// 번역표 (2.0.0): 새 영웅 공사 반장 — 이름 · 설명 · 카드 · 고유 스킬 · 진화 · 무기 · 강화 · 패시브 · 이야기 · 전령 · 기억 · 계약 · 보드
// 키 앞글자 b. · be. (hero-foreman.md) — 이미 있는 키는 건드리지 않음
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddForemanLines()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 정체 · 글
        A("공사 반장", "Foreman", "現場監督", "工头");
        A("영원한 공사 중", "Forever Under Construction", "永遠に工事中", "永远在施工");
        A("어느 게임에나 걸린 「공사 중 · 추후 업데이트 예정」 표지판 뒤에서 일하던 반장. 적을 베는 대신 삽날로 밀어 한 더미로 쌓아 짓누르고, 다 모이면 통째로 쏟아 버립니다. 입버릇은 「아 지금 하잖아요!」",
          "A foreman who worked behind the \"Under Construction · Coming Soon\" sign every game has. Instead of slashing enemies, he shoves them into one pile with his plow blade, crushes them, and dumps the whole heap at once. His catchphrase: \"I'm doing it right now!\"",
          "どのゲームにも掲げられている「工事中・近日アップデート予定」の看板の裏で働いていた現場監督。敵を斬る代わりに排土板で押して一つの山に積み上げ、押し潰し、溜まったら丸ごとぶちまけます。口癖は「今やってるじゃないですか！」",
          "在每款游戏里都挂着的“施工中 · 后续更新”牌子背后干活的工头。他不砍敌人，而是用推铲把敌人推成一堆再碾压，堆满后整堆倾倒。口头禅是“我这不正在做嘛！”");
        A("삽날", "Plow Blade", "排土板", "推铲");
        A("누르고 있는 동안 마우스 쪽으로 삽날을 세우고, 닿는 적을 앞에 더미로 쌓습니다. 더미가 무거울수록 짓누르는 피해가 커지고, 벽 · 바리케이드 · 큰 적에 부딪히면 「빠각」 크게 다칩니다. 떼면 더미를 마우스 쪽으로 쏟아 산사태를 일으킵니다 (덤프). 밀고 있는 동안은 느려지고, 옆 · 뒤로는 더 느립니다",
          "While held, the plow blade points toward the cursor and piles the enemies it touches in front of you. The heavier the pile, the harder it crushes, and hitting a wall, barricade or big enemy makes a big \"crack\". Release to dump the pile toward the cursor in a landslide. You move slower while pushing, and slower still sideways or backwards.",
          "押している間、排土板をカーソルの方へ向け、触れた敵を前に山として積み上げます。山が重いほど押し潰すダメージが増し、壁・バリケード・大きな敵にぶつかると「バキッ」と大ダメージ。離すと山をカーソルの方へぶちまけて土砂崩れを起こします（ダンプ）。押している間は遅くなり、横や後ろへはさらに遅くなります。",
          "按住时推铲指向光标方向，把碰到的敌人堆在身前。堆越重，碾压伤害越高；撞上墙、路障或大型敌人会发出“咔嚓”一声造成重创。松开后把整堆敌人朝光标方向倾倒，引发山崩（倾倒）。推动时移动变慢，向侧面和后方更慢。");
        A("철거 공고", "Demolition Notice", "撤去通知", "拆除公告");
        A("누르고 있으면 쇠공이 떨어질 자리와 굴러갈 길이 보이고, 떼면 그 자리에 쇠공이 떨어져 세 번 튕기며 굴러갑니다. 튕긴 자리마다 「공사 중」 바리케이드가 4초 동안 적의 길을 막습니다 (보스는 부수고 지나갑니다)",
          "Hold to see where the wrecking ball will land and the path it will roll; release and it drops there and bounces three times while rolling on. Each bounce leaves an \"Under Construction\" barricade that blocks enemies for 4s (bosses smash through).",
          "押し続けると鉄球が落ちる場所と転がる道が見え、離すとそこに鉄球が落ちて3回跳ねながら転がります。跳ねた場所ごとに「工事中」バリケードが4秒間敵の行く手を塞ぎます（ボスは壊して通ります）。",
          "按住会显示铁球的落点和滚动路线，松开后铁球砸在该处，并弹跳三次向前滚动。每次弹跳的位置都会立起“施工中”路障，挡住敌人4秒（首领会撞毁路障通过）。");
        A("압착 공격력", "Crush Damage", "圧着攻撃力", "挤压攻击力");
        A("더미 — 누르는 동안 삽날 앞에 쌓인 적입니다. 무거울수록 짓누르는 피해가 커지고, 떼면 마우스 쪽으로 쏟아집니다. 무게는 머리 위 칸으로 보입니다.",
          "Pile — the enemies stacked in front of the plow blade while you hold. The heavier it is, the harder it crushes, and releasing dumps it toward the cursor. Its weight shows in the bar above your head.",
          "山 — 押している間、排土板の前に積まれた敵です。重いほど押し潰すダメージが増し、離すとカーソルの方へぶちまけられます。重さは頭上のゲージで分かります。",
          "堆——按住时堆在推铲前的敌人。越重碾压伤害越高，松开后朝光标方向倾倒。重量显示在头顶的条上。");
        A("무게", "Weight", "重さ", "重量");
        A("아 지금 하잖아요!", "I'm doing it right now!", "今やってるじゃないですか！", "我这不正在做嘛！");
        A("완공!", "Completed!", "完成！", "竣工！");
        A("얼리 액세스 종료!", "Early Access Over!", "アーリーアクセス終了！", "抢先体验结束！");
        K("sec.hint.10", "끝나지 않는 공사 끝에는, 대답하는 표지판이 있다.", "At the end of the never-ending construction, there is a sign that answers back.", "終わらない工事の果てに、答えを返す看板がある。", "在没有尽头的施工尽头，有一块会回答的牌子。");

        // ================================================================= 전용 레벨업 카드 6
        A("긴 삽날", "Long Blade", "長い排土板", "加长推铲");
        A("삽날이 넓어지고 더미에 더 많은 적을 쌓을 수 있습니다. (단계마다 폭 +0.4칸)", "The blade gets wider and the pile can hold more enemies. (+0.4 tiles of width per level)", "排土板が広くなり、山にもっと多くの敵を積めます。（段階ごとに幅+0.4マス）", "推铲变宽，堆里能堆更多敌人。（每级宽度+0.4格）");
        A("더미 상한", "Pile limit", "山の上限", "堆上限");
        A("잔해", "Rubble", "瓦礫", "残骸");
        A("더미 안에서 쓰러진 적이 잔해로 남아 5초 동안 더미 무게를 채웁니다. 덤프하면 함께 쏟아져 부딪힌 적을 다치게 합니다.", "Enemies defeated inside the pile leave rubble that fills the pile's weight for 5s. When you dump, it spills out too and hurts what it hits.", "山の中で倒れた敵が瓦礫として残り、5秒間山の重さを埋めます。ダンプすると一緒にぶちまけられ、ぶつかった敵にダメージを与えます。", "在堆里被击倒的敌人会留下残骸，5秒内填补堆的重量。倾倒时一并倒出，砸伤撞到的敌人。");
        A("잔해 최대", "Max rubble", "瓦礫の最大数", "残骸上限");
        A("빠각!", "Crack!", "バキッ！", "咔嚓！");
        A("더미가 무언가에 부딪혀 「빠각」 할 때마다 충격파가 퍼집니다. (공격력 80%)", "Every time the pile hits something with a \"crack\", a shockwave spreads. (80% attack)", "山が何かにぶつかって「バキッ」となるたびに衝撃波が広がります。（攻撃力80%）", "每当堆撞到东西发出“咔嚓”声时，就会扩散冲击波。（攻击力80%）");
        A("충격파 범위", "Shockwave range", "衝撃波の範囲", "冲击波范围");
        A("공사 구간", "Work Zone", "工事区間", "施工区段");
        A("바리케이드가 더 오래 서 있고, 닿은 적을 30% 느리게 합니다.", "Barricades stand longer and slow enemies that touch them by 30%.", "バリケードが長く立ち続け、触れた敵を30%遅くします。", "路障立得更久，并使碰到的敌人减速30%。");
        A("바리케이드 지속", "Barricade duration", "バリケードの持続", "路障持续时间");
        A("안전 수칙", "Safety Rules", "安全規則", "安全守则");
        A("더미가 앞쪽에서 날아오는 적 투사체를 막습니다.", "The pile blocks enemy projectiles coming from the front.", "山が前方から飛んでくる敵の飛び道具を防ぎます。", "堆会挡住从前方飞来的敌方投射物。");
        A("막는 데 필요한 무게", "Weight needed to block", "防ぐのに必要な重さ", "挡弹所需重量");
        A("적재함 확장", "Bigger Bed", "荷台拡張", "扩大货斗");
        A("덤프가 더 멀리 쏟아지고, 쏟아진 적이 더 오래 묻혀 있습니다. (단계마다 묻힘 +0.2초)", "Dumps spill farther, and dumped enemies stay buried longer. (+0.2s of burial per level)", "ダンプがより遠くまでぶちまけられ、ぶちまけられた敵がより長く埋まります。（段階ごとに埋没+0.2秒）", "倾倒得更远，被倒出的敌人被埋得更久。（每级埋没时间+0.2秒）");
        A("덤프 거리", "Dump distance", "ダンプ距離", "倾倒距离");

        // ================================================================= 고유 스킬 10
        A("야근 수당", "Overtime Pay", "残業手当", "加班费");
        A("무게 4 이상인 더미를 밀고 있는 동안 필살기 게이지가 찹니다.", "While pushing a pile of weight 4 or more, the ultimate gauge fills.", "重さ4以上の山を押している間、必殺技ゲージが溜まります。", "推着重量4以上的堆时，必杀技槽会充能。");
        A("초당 게이지", "Gauge per second", "毎秒ゲージ", "每秒充能");
        A("철근", "Rebar", "鉄筋", "钢筋");
        A("덤프할 때 더미 속 철근이 앞으로 튀어나가 한 줄의 적을 꿰뚫습니다. (공격력 70%씩)", "When you dump, rebar from the pile shoots forward and pierces a line of enemies. (70% attack each)", "ダンプするとき、山の中の鉄筋が前へ飛び出し、一直線の敵を貫きます。（攻撃力70%ずつ）", "倾倒时，堆里的钢筋向前飞出，贯穿一排敌人。（每根攻击力70%）");
        A("철근 수", "Rebar count", "鉄筋の数", "钢筋数量");
        A("롤러 자국", "Roller Tracks", "ローラー跡", "压路痕");
        A("무게 6 이상인 더미를 밀면 지나간 자리에 1.5초 동안 다져진 자국이 남아 밟은 적을 느리게 합니다.", "Pushing a pile of weight 6 or more leaves packed tracks for 1.5s that slow enemies who step on them.", "重さ6以上の山を押すと、通った場所に1.5秒間踏み固めた跡が残り、踏んだ敵を遅くします。", "推着重量6以上的堆时，经过之处会留下持续1.5秒的压实痕迹，踩到的敌人会减速。");
        A("크레인", "Crane", "クレーン", "起重机");
        A("쇠공이 튕길 때마다 둘레의 적을 쇠공 쪽으로 끌어당깁니다. (보스 제외)", "Each time the wrecking ball bounces, it pulls surrounding enemies toward it. (Bosses excluded)", "鉄球が跳ねるたびに、周囲の敵を鉄球の方へ引き寄せます。（ボス除く）", "每次铁球弹跳，都会把周围的敌人拉向铁球。（首领除外）");
        A("끌어당기는 범위", "Pull range", "引き寄せ範囲", "拉拽范围");
        A("안전 제일", "Safety First", "安全第一", "安全第一");
        A("더미를 밀고 있는 동안 받는 피해가 줄어듭니다.", "You take less damage while pushing a pile.", "山を押している間、受けるダメージが減ります。", "推着堆时受到的伤害降低。");
        A("확률", "Chance", "確率", "几率");      // 2.0.0 검토: 자재 회수 카드의 수치 이름
        A("받는 피해 감소", "Damage reduction", "被ダメージ軽減", "受伤减免");
        A("시멘트", "Cement", "セメント", "水泥");
        A("짓누를 때마다 더미 안 적이 조금씩 굳어, 세 번 굳은 적은 덤프 때 바위처럼 더 멀리 날아가 더 아프게 부딪힙니다.", "Each crush hardens the enemies in the pile a little; those hardened three times fly farther like boulders when dumped and hit harder.", "押し潰すたびに山の中の敵が少しずつ固まり、3回固まった敵はダンプの際に岩のようにより遠くへ飛び、より強くぶつかります。", "每次碾压都会让堆里的敌人逐渐变硬，硬化三次的敌人在倾倒时会像岩石一样飞得更远、撞得更痛。");
        A("굳은 적 덤프 피해", "Hardened dump damage", "固まった敵のダンプダメージ", "硬化敌人倾倒伤害");
        A("과적", "Overload", "過積載", "超载");
        A("더미가 가득 찬 뒤에도 적을 밀면, 넘친 적이 더미 위에서 굴러떨어지며 부딪힌 적과 함께 다칩니다.", "Pushing enemies after the pile is full makes the overflow roll off the top, hurting itself and whatever it hits.", "山がいっぱいになった後も敵を押すと、あふれた敵が山の上から転げ落ち、ぶつかった敵と一緒にダメージを受けます。", "堆满之后继续推敌人，溢出的敌人会从堆上滚落，与被撞到的敌人一起受伤。");
        A("굴러떨어지는 피해", "Tumble damage", "転げ落ちるダメージ", "滚落伤害");
        A("우회로", "Detour", "迂回路", "绕行路");
        A("바리케이드에 부딪힌 적이 빠각 피해를 받고 반장 쪽으로 1.5칸 미끄러집니다. (적마다 1초에 한 번)", "Enemies that hit a barricade take crack damage and slide 1.5 tiles toward the foreman. (Once per second per enemy)", "バリケードにぶつかった敵が「バキッ」ダメージを受け、現場監督の方へ1.5マス滑ります。（敵ごとに1秒に1回）", "撞上路障的敌人会受到“咔嚓”伤害，并向工头滑行1.5格。（每个敌人每秒一次）");
        A("빠각 피해", "Crack damage", "バキッのダメージ", "咔嚓伤害");
        A("자재 회수", "Salvage", "資材回収", "回收建材");
        A("더미 안에서 쓰러진 적이 확률로 코인을 하나 더 떨어뜨립니다.", "Enemies defeated inside the pile may drop one extra coin.", "山の中で倒れた敵が、確率でコインをもう1枚落とします。", "在堆里被击倒的敌人有几率多掉落一枚金币。");
        A("추후 업데이트 예정", "Coming Soon", "近日アップデート予定", "后续更新");
        A("아직 공사 중인 스킬입니다. 덤프할 때마다 공정률이 올라, 다 차면 덤프한 자리에 「완공」 쇠공이 떨어집니다. (공정률만큼 미리 조금씩 · 공격력 150%)", "This skill is still under construction. Each dump raises the progress, and when it fills, a \"Completed\" wrecking ball drops where you dumped. (Smaller balls drop in advance according to progress · 150% attack)", "まだ工事中のスキルです。ダンプするたびに進捗率が上がり、満タンになるとダンプした場所に「完工」鉄球が落ちます。（進捗率に応じて少しずつ先行して落下・攻撃力150%）", "这是仍在施工的技能。每次倾倒都会提高工程进度，满了之后会在倾倒处落下“竣工”铁球。（按进度提前落下较小的铁球 · 攻击力150%）");
        A("완공까지 덤프", "Dumps until completion", "完工までのダンプ", "距竣工所需倾倒次数");

        // ================================================================= 스킬 진화 7
        A("불도저", "Bulldozer", "ブルドーザー", "推土机");
        A("더미 상한이 1.5배가 되고, 삽날이 1칸 더 멀리서 적을 빨아들입니다. 과적으로 굴러떨어진 적은 다시 더미 끝에 붙습니다.", "The pile limit becomes 1.5x and the blade pulls in enemies from 1 tile farther. Enemies that tumbled off from Overload stick back onto the end of the pile.", "山の上限が1.5倍になり、排土板が1マス遠くから敵を吸い込みます。過積載で転げ落ちた敵は再び山の端にくっつきます。", "堆上限变为1.5倍，推铲可从远1格处吸入敌人。因超载滚落的敌人会重新粘在堆的末端。");
        A("덤프 트럭", "Dump Truck", "ダンプカー", "自卸卡车");
        A("덤프한 흙이 착지한 뒤 한 번 더 앞으로 쏟아지고(피해 60%), 철근이 두 배로 튀어나갑니다.", "After landing, the dumped earth spills forward once more (60% damage), and twice as much rebar shoots out.", "ダンプした土が着地した後にもう一度前へ流れ出し（ダメージ60%）、鉄筋が2倍飛び出します。", "倾倒的土石落地后会再向前倾泻一次（伤害60%），钢筋数量翻倍。");
        A("콘크리트", "Concrete", "コンクリート", "混凝土");
        A("굳은 적이 쓰러지면 콘크리트 덩이(무게 2)가 남고, 덤프하면 바위처럼 꿰뚫으며 굴러갑니다. 잔해 최대가 두 배가 됩니다.", "A hardened enemy that falls leaves a concrete chunk (weight 2) that rolls through like a boulder when dumped. Max rubble doubles.", "固まった敵が倒れるとコンクリートの塊（重さ2）が残り、ダンプすると岩のように貫きながら転がります。瓦礫の最大数が2倍になります。", "硬化的敌人倒下后会留下混凝土块（重量2），倾倒时像岩石一样贯穿滚动。残骸上限翻倍。");
        A("통행 금지", "No Entry", "通行止め", "禁止通行");
        A("바리케이드가 다섯 개로 늘고 서로 테이프로 이어져, 테이프를 넘는 적이 빠각 피해를 받고 튕겨 나갑니다.", "Barricades increase to five, linked by tape; enemies crossing the tape take crack damage and are knocked back.", "バリケードが5つに増え、テープでつながり、テープを越える敵が「バキッ」ダメージを受けて弾き飛ばされます。", "路障增加到五个并由警戒带相连，越过警戒带的敌人会受到“咔嚓”伤害并被弹开。");
        A("무재해 100일", "100 Days Accident-Free", "無災害100日", "无事故100天");
        A("더미가 막은 적 투사체를 앞으로 되튕기고(공격력 80%), 안전 제일의 피해 감소가 두 배가 됩니다.", "Projectiles blocked by the pile bounce back forward (80% attack), and the damage reduction of Safety First doubles.", "山が防いだ敵の飛び道具を前へ跳ね返し（攻撃力80%）、安全第一の被ダメージ軽減が2倍になります。", "堆挡下的敌方投射物会向前弹回（攻击力80%），“安全第一”的减伤翻倍。");
        A("항타기", "Pile Driver", "杭打ち機", "打桩机");
        A("빠각 충격파가 세 번 연달아 내리찍고, 롤러 자국이 두 배로 넓고 오래 남습니다.", "The crack shockwave slams down three times in a row, and roller tracks become twice as wide and last twice as long.", "「バキッ」の衝撃波が3回連続で叩きつけ、ローラー跡が2倍の広さで長く残ります。", "“咔嚓”冲击波会连续砸下三次，压路痕的宽度和持续时间翻倍。");
        A("정식 출시", "Official Release", "正式リリース", "正式发布");
        A("공정률이 곧바로 100%가 되고 완공 쇠공이 두 개씩 떨어지며, 자재 회수 확률이 두 배가 됩니다. 「얼리 액세스 종료!」", "Progress jumps straight to 100%, two completed wrecking balls drop each time, and Salvage chance doubles. \"Early Access Over!\"", "進捗率がすぐに100%になり、完工鉄球が2つずつ落ち、資材回収の確率が2倍になります。「アーリーアクセス終了！」", "工程进度直接变为100%，每次落下两个竣工铁球，回收建材几率翻倍。“抢先体验结束！”");

        // ================================================================= 특수 능력 (형태 · 패시브) · 궁극기 · 강화
        K("더미가 버킷에 담겨 높이 들립니다. 덤프가 포물선으로 30% 더 멀리 날아가 착지할 때 터집니다. (공격력 80% · 반지름 1.6칸)\n궁극기: 버킷 내려찍기",
          "더미가 버킷에 담겨 높이 들립니다. 덤프가 포물선으로 30% 더 멀리 날아가 착지할 때 터집니다. (공격력 80% · 반지름 1.6칸)\n궁극기: 버킷 내려찍기",
          "The pile is scooped into a bucket and lifted high. The dump arcs 30% farther and explodes on landing. (80% attack · 1.6 tile radius)\nUltimate: Bucket Slam",
          "山がバケットに入って高く持ち上げられます。ダンプが放物線を描いて30%遠くまで飛び、着地時に爆発します。（攻撃力80%・半径1.6マス）\n究極技：バケット叩きつけ",
          "堆被装进铲斗高高举起。倾倒会沿抛物线飞出，距离增加30%，落地时爆炸。（攻击力80% · 半径1.6格）\n终极技：铲斗砸地");
        K("삽날이 떨리며 짓누를 때마다 삽날 앞 적을 0.3초 기절시키고, 압착 피해가 20% 늘어납니다.\n궁극기: 진동 다짐",
          "삽날이 떨리며 짓누를 때마다 삽날 앞 적을 0.3초 기절시키고, 압착 피해가 20% 늘어납니다.\n궁극기: 진동 다짐",
          "The blade vibrates, stunning enemies in front of it for 0.3s with every crush, and crush damage rises by 20%.\nUltimate: Vibration Pack",
          "排土板が振動し、押し潰すたびに前の敵を0.3秒気絶させ、圧着ダメージが20%増えます。\n究極技：振動締め固め",
          "推铲震动，每次碾压都会使身前的敌人眩晕0.3秒，碾压伤害增加20%。\n终极技：震动夯实");
        K("더미 둘레를 사슬 쇠공이 돌며(반지름 R + 1) 닿는 적을 칩니다. (공격력 60% · 적마다 0.5초에 한 번)\n궁극기: 쌍 쇠공",
          "더미 둘레를 사슬 쇠공이 돌며(반지름 R + 1) 닿는 적을 칩니다. (공격력 60% · 적마다 0.5초에 한 번)\n궁극기: 쌍 쇠공",
          "A chained wrecking ball circles the pile (radius R + 1) and strikes enemies it touches. (60% attack · once per 0.5s per enemy)\nUltimate: Twin Wrecking Balls",
          "鎖付きの鉄球が山の周りを回り（半径R+1）、触れた敵を打ちます。（攻撃力60%・敵ごとに0.5秒に1回）\n究極技：ツイン鉄球",
          "链条铁球绕着堆旋转（半径R + 1），击打碰到的敌人。（攻击力60% · 每个敌人0.5秒一次）\n终极技：双铁球");
        K("더미를 밀 때 느려지는 정도가 절반이 됩니다.\n진화: 느려지지 않음", "더미를 밀 때 느려지는 정도가 절반이 됩니다.\n진화: 느려지지 않음",
          "Slowdown while pushing the pile is halved.\nEvolved: no slowdown", "山を押すときの鈍化が半分になります。\n進化：遅くならない", "推堆时的减速效果减半。\n进化：不再减速");
        K("보스 · 중간 보스에게 주는 들이받기 · 빠각 피해가 20% 늘어납니다.\n진화: +35%", "보스 · 중간 보스에게 주는 들이받기 · 빠각 피해가 20% 늘어납니다.\n진화: +35%",
          "Ram and crack damage dealt to bosses and mid-bosses rises by 20%.\nEvolved: +35%", "ボス・中ボスに与える体当たり・「バキッ」ダメージが20%増えます。\n進化：+35%", "对首领和中型首领造成的顶撞与“咔嚓”伤害增加20%。\n进化：+35%");
        K("바리케이드가 사라질 때 무너지며 둘레 2칸 적에게 피해를 줍니다. (공격력 100%)\n진화: 180%", "바리케이드가 사라질 때 무너지며 둘레 2칸 적에게 피해를 줍니다. (공격력 100%)\n진화: 180%",
          "When a barricade disappears it collapses, hurting enemies within 2 tiles. (100% attack)\nEvolved: 180%", "バリケードが消えるときに崩れ、周囲2マスの敵にダメージを与えます。（攻撃力100%）\n進化：180%", "路障消失时会坍塌，对周围2格内的敌人造成伤害。（攻击力100%）\n进化：180%");
        K("덤프할 때마다 체력을 1 회복합니다. (1초에 한 번)\n진화: 2", "덤프할 때마다 체력을 1 회복합니다. (1초에 한 번)\n진화: 2",
          "Each dump restores 1 health. (Once per second)\nEvolved: 2", "ダンプするたびに体力を1回復します。（1秒に1回）\n進化：2", "每次倾倒恢复1点生命。（每秒一次）\n进化：2");
        A("굴착기 버킷", "Excavator Bucket", "掘削バケット", "挖掘机铲斗");
        A("착암기", "Jackhammer", "さく岩機", "凿岩机");
        A("철거 쇠공", "Wrecking Ball", "解体用鉄球", "拆除铁球");
        A("안전화", "Safety Boots", "安全靴", "安全鞋");
        A("현장 소장", "Site Manager", "現場所長", "现场主管");
        A("무너진 담", "Fallen Wall", "崩れた塀", "倒塌的围墙");
        A("새참", "Snack Break", "おやつ休憩", "工间加餐");
        A("더미가 버킷에 담겨 높이 들립니다. 덤프가 포물선으로 30% 더 멀리 날아가 착지할 때 터집니다. (공격력 80% · 반지름 1.6칸)", "The pile is scooped into a bucket and lifted high. The dump arcs 30% farther and explodes on landing. (80% attack · 1.6 tile radius)", "山がバケットに入って高く持ち上げられます。ダンプが放物線を描いて30%遠くまで飛び、着地時に爆発します。（攻撃力80%・半径1.6マス）", "堆被装进铲斗高高举起。倾倒会沿抛物线飞出，距离增加30%，落地时爆炸。（攻击力80% · 半径1.6格）");
        A("삽날이 떨리며 짓누를 때마다 삽날 앞 적을 0.3초 기절시키고, 압착 피해가 20% 늘어납니다.", "The blade vibrates, stunning enemies in front of it for 0.3s with every crush, and crush damage rises by 20%.", "排土板が振動し、押し潰すたびに前の敵を0.3秒気絶させ、圧着ダメージが20%増えます。", "推铲震动，每次碾压都会使身前的敌人眩晕0.3秒，碾压伤害增加20%。");
        A("더미 둘레를 사슬 쇠공이 돌며(반지름 R + 1) 닿는 적을 칩니다. (공격력 60% · 적마다 0.5초에 한 번)", "A chained wrecking ball circles the pile (radius R + 1) and strikes enemies it touches. (60% attack · once per 0.5s per enemy)", "鎖付きの鉄球が山の周りを回り（半径R+1）、触れた敵を打ちます。（攻撃力60%・敵ごとに0.5秒に1回）", "链条铁球绕着堆旋转（半径R + 1），击打碰到的敌人。（攻击力60% · 每个敌人0.5秒一次）");
        A("버킷 내려찍기", "Bucket Slam", "バケット叩きつけ", "铲斗砸地");
        A("마우스 둘레를 큰 버킷이 세 번 내려찍습니다. (공격력 128%씩)", "A giant bucket slams down around the cursor three times. (128% attack each)", "巨大なバケットがカーソルの周りを3回叩きつけます。（攻撃力128%ずつ）", "巨大的铲斗在光标周围砸地三次。（每次攻击力128%）");
        A("진동 다짐", "Vibration Pack", "振動締め固め", "震动夯实");
        A("내 둘레 6칸에 땅울림이 네 번 퍼져 적을 기절시킵니다. (공격력 72%씩 · 0.4초 기절)", "Ground tremors ripple four times within 6 tiles around you, stunning enemies. (72% attack each · 0.4s stun)", "自分の周囲6マスに地鳴りが4回広がり、敵を気絶させます。（攻撃力72%ずつ・0.4秒気絶）", "震动在你周围6格内扩散四次，使敌人眩晕。（每次攻击力72% · 眩晕0.4秒）");
        A("쌍 쇠공", "Twin Wrecking Balls", "ツイン鉄球", "双铁球");
        A("철거 공고 쇠공이 하나 더 떨어져 25° 옆으로 튕깁니다. (공격력 56%)", "One more Demolition Notice wrecking ball drops and bounces off at a 25° angle. (56% attack)", "撤去通知の鉄球がもう1つ落ち、25°横へ跳ねます。（攻撃力56%）", "再落下一颗拆除公告铁球，向侧面25°弹跳。（攻击力56%）");
        A("뒷날", "Rear Blade", "後ろ刃", "后刃");
        A("삽날 뒤에도 작은 날이 달려, 뒤에서 닿는 적을 밀쳐내고 다치게 합니다. (공격력 40% · 0.5초에 한 번)", "A small blade on the back of the plow pushes back and hurts enemies touching from behind. (40% attack · once per 0.5s)", "排土板の後ろにも小さな刃が付き、後ろから触れた敵を押しのけてダメージを与えます。（攻撃力40%・0.5秒に1回）", "推铲背面也装有小刃，会推开并击伤从背后碰到的敌人。（攻击力40% · 每0.5秒一次）");
        A("유압 실린더", "Hydraulic Cylinder", "油圧シリンダー", "液压缸");
        A("압착 피해가 무게 12가 아니라 18까지 계속 커집니다. (무게 18 → 한 마리당 81%)", "Crush damage keeps rising up to weight 18 instead of 12. (Weight 18 → 81% per enemy)", "圧着ダメージが重さ12ではなく18まで増え続けます。（重さ18→1体あたり81%）", "碾压伤害会持续增长到重量18，而不是12。（重量18 → 每只81%）");
        A("안전 펜스", "Safety Fence", "安全フェンス", "安全围栏");
        A("덤프한 자리에 2초 동안 짧은 바리케이드(폭 2칸)가 섭니다.", "A short barricade (2 tiles wide) stands for 2s where you dumped.", "ダンプした場所に2秒間、短いバリケード（幅2マス）が立ちます。", "倾倒处会立起一个持续2秒的短路障（宽2格）。");

        // ================================================================= 이야기
        A("초원 한가운데 「공사 중」 표지판이 서 있었다. 그 뒤에서 안전모 하나가 쑥 올라왔다.", "A \"Under Construction\" sign stood in the middle of the meadow. A hard hat popped up from behind it.", "草原の真ん中に「工事中」の看板が立っていた。その裏からヘルメットがひょいと現れた。", "草原中央立着一块“施工中”的牌子。牌子后面冒出一顶安全帽。");
        A("“여기 원래 다리 놓을 자리였거든요. 슬라임이 다 녹였네.”", "\"This was supposed to be where the bridge goes. The slime melted it all.\"", "「ここは元々橋を架ける場所だったんですよ。スライムが全部溶かしちゃいましたね。」", "“这里本来是要修桥的地方。史莱姆全给融了。”");
        A("반장은 삽날을 세웠다. 치우는 것부터가 공사다.", "The foreman raised his plow blade. Clearing it out is part of the job.", "現場監督は排土板を立てた。片付けるところから工事だ。", "工头竖起了推铲。清理也是施工的一部分。");
        A("무너진 신전 앞에 「추후 업데이트 예정」 표지판이 박혀 있었다. 박은 사람은 반장 자신이었다.", "A \"Coming Soon\" sign was planted in front of the ruined temple. The foreman himself had put it there.", "崩れた神殿の前に「近日アップデート予定」の看板が刺さっていた。刺したのは現場監督自身だった。", "倒塌的神殿前插着一块“后续更新”的牌子。插牌子的正是工头自己。");
        A("“언제 하냐고요? 아 지금 하잖아요!”", "\"When will it be done? I'm doing it right now!\"", "「いつやるのかって？今やってるじゃないですか！」", "“问什么时候做？我这不正在做嘛！”");
        A("반장은 삽날을 앞세우고 구멍 아래로 밀고 들어갔다.", "The foreman pushed down into the hole, plow blade first.", "現場監督は排土板を先頭に、穴の下へ押し入っていった。", "工头把推铲顶在前面，推进了洞底。");
        A("반장은 마지막 표지판을 뽑아 들었다. 뒷면에는 「공사 완료」라고 적혀 있었다.", "The foreman pulled up the last sign. \"Construction Complete\" was written on the back.", "現場監督は最後の看板を引き抜いた。裏には「工事完了」と書かれていた。", "工头拔起了最后一块牌子。背面写着“施工完毕”。");
        A("처음으로 그 면을 앞으로 돌려 세웠다.", "For the first time, he turned that side to face front.", "初めてその面を前に向けて立てた。", "他第一次把那一面转到了前面。");
        A("“…다음 패치까지는요.”", "\"...Until the next patch, anyway.\"", "「…次のパッチまではですけど。」", "“……直到下一次补丁为止。”");
        A("하지만 지평선 너머, 붉은 사막에 아직 치우지 못한 고깔이 줄지어 서 있었다...", "But beyond the horizon, in the red desert, a line of traffic cones still stood uncleared...", "しかし地平線の向こう、赤い砂漠にはまだ片付けていないコーンが並んでいた...", "但在地平线的那一边，红色沙漠里仍排着一行没来得及收走的锥桶……");

        // ================================================================= 전령 대사 6줄
        K("herald.hero.foreman.1", "반장이 공사 도중에 쓰러졌다.\n“공사 중단 아닙니다. 잠깐 쉬는 거예요.”", "The foreman collapsed mid-construction.\n\"This isn't a work stoppage. I'm just taking a short break.\"", "現場監督は工事の途中で倒れた。\n「工事中断じゃありません。ちょっと休んでるだけです。」", "工头在施工途中倒下了。\n“不是停工。只是稍微歇一会儿。”");
        K("herald.hero.foreman.2", "삽날은 앞에만 있다. 등 뒤는 누가 막는가.\n“…안전 요원을 뽑았어야 했는데.”", "The blade only faces front. Who guards the back?\n\"...I should've hired a safety officer.\"", "排土板は前にしかない。背中は誰が守るのか。\n「…安全要員を雇うべきだったなあ。」", "推铲只在前面。身后由谁来挡？\n“……早该雇个安全员的。”");
        K("herald.hero.foreman.3", "언제 끝나느냐고 물었다.\n“아 지금 하잖아요! …지금 하고 있었잖아요.”", "He was asked when it would be finished.\n\"I'm doing it right now! ...I WAS doing it right now.\"", "いつ終わるのかと聞かれた。\n「今やってるじゃないですか！…今やってたじゃないですか。」", "有人问他什么时候能完工。\n“我这不正在做嘛！……刚才我就在做啊。”");
        K("herald.hero.foreman.4", "쌓기만 하고 쏟지 못한 더미는 무덤이 된다.\n“덤프는 타이밍이죠. 다음엔 일찍 쏟습니다.”", "A pile that was stacked but never dumped becomes a grave.\n\"Dumping is all about timing. Next time I'll dump earlier.\"", "積むだけで空けられなかった山は墓になる。\n「ダンプはタイミングですよ。次は早めに空けます。」", "只堆不倒的堆会变成坟墓。\n“倾倒讲究时机。下次我早点倒。”");
        K("herald.hero.foreman.5", "리치 왕은 무너진 신전을 그대로 두라 했다.\n“철거 공고 붙였습니다. 이의 신청 기간 지났어요.”", "The Lich King ordered the ruined temple left as it was.\n\"I posted the demolition notice. The objection period has passed.\"", "リッチキングは崩れた神殿をそのままにせよと言った。\n「撤去通知は貼りました。異議申し立て期間は過ぎてますよ。」", "巫妖王下令保留倒塌的神殿原样。\n“拆除公告我已经贴了。异议期早过了。”");
        K("herald.hero.foreman.6", "개발자가 너를 만들다 만 것을 안다.\n“압니다. 그래서 제가 마무리하러 온 거예요.”", "I know the developer left you half-made.\n\"I know. That's why I came to finish the job.\"", "開発者が君を作りかけにしたことを知っている。\n「知ってます。だから僕が仕上げに来たんです。」", "我知道开发者没把你做完。\n“我知道。所以我是来收尾的。”");
        K("bossmem.noun.foreman", "삽날", "blade", "ブレード", "推铲");

        // ================================================================= 기억 조각 6
        K("memory.foreman.1.title", "표지판 뒤", "Behind the Sign", "看板の裏", "牌子背后");
        K("memory.foreman.1.text", "모든 게임에 같은 표지판이 걸려 있었다. 그 뒤를 들여다본 사람은 없었다.\n반장은 거기서 일했다. 아무도 보지 않는 곳에서, 언젠가 열릴 길을 닦았다.\n처음 쓰러진 날 전령이 물었다. 무엇을 짓고 있었느냐고.\n“아직 안 정했어요. 그게 공사죠.”",
          "The same sign hung in every game. No one ever looked behind it.\nThe foreman worked there. Where no one could see, he paved a road that would open someday.\nThe day he first fell, the herald asked what he had been building.\n\"I haven't decided yet. That's construction.\"",
          "どのゲームにも同じ看板が掛かっていた。その裏を覗いた人はいなかった。\n現場監督はそこで働いた。誰も見ていない場所で、いつか開く道をならしていた。\n初めて倒れた日、伝令が尋ねた。何を造っていたのか、と。\n「まだ決めてません。それが工事ってもんです。」",
          "每款游戏里都挂着同样的牌子，却从没有人看过它的背后。\n工头就在那里干活。在无人看见的地方，铺着一条总有一天会开通的路。\n他第一次倒下的那天，传令官问他在建什么。\n“还没定。这就是施工。”");
        K("memory.foreman.2.title", "다섯 번의 사고", "Five Accidents", "五度の事故", "五次事故");
        K("memory.foreman.2.text", "무재해 기록판은 다섯 번 0으로 돌아갔다. 반장은 매번 새 숫자를 적었다.\n“0일. 오늘부터 다시 셉니다.”",
          "The accident-free board reset to zero five times. Each time, the foreman wrote a new number.\n\"Zero days. We start counting again today.\"",
          "無災害記録板は5回ゼロに戻った。現場監督はそのたびに新しい数字を書いた。\n「0日。今日からまた数えます。」",
          "无事故记录牌被归零了五次。工头每次都写下新的数字。\n“0天。从今天重新开始数。”");
        K("memory.foreman.3.title", "준공식", "Groundbreaking's End", "竣工式", "竣工典礼");
        K("memory.foreman.3.text", "처음으로 끝까지 간 날, 테이프를 자를 사람이 없었다.\n반장은 자기 삽날로 잘랐다.",
          "The day he first made it all the way, there was no one to cut the ribbon.\nThe foreman cut it with his own plow blade.",
          "初めて最後までたどり着いた日、テープを切る人がいなかった。\n現場監督は自分の排土板で切った。",
          "他第一次走到终点的那天，没有人来剪彩。\n工头用自己的推铲剪断了彩带。");
        K("memory.foreman.4.title", "설계 변경", "Design Change", "設計変更", "设计变更");
        K("memory.foreman.4.text", "어려운 길을 넘긴 밤, 그는 도면을 다시 그렸다.\n길이 무거워질수록 삽날을 넓혔다.",
          "The night he got through a hard road, he redrew the blueprints.\nThe heavier the road became, the wider he made the blade.",
          "難しい道を越えた夜、彼は図面を描き直した。\n道が重くなるほど、排土板を広げた。",
          "闯过艰难道路的那一夜，他重新画了图纸。\n路越沉重，他就把推铲做得越宽。");
        K("memory.foreman.5.title", "야간 작업", "Night Shift", "夜間作業", "夜间作业");
        K("memory.foreman.5.text", "사막에서 오래 버틴 밤, 고깔 불빛만 깜빡였다. 그는 일하는 동안엔 무섭지 않다고 했다. 할 일이 있으니까.",
          "On the night he held out long in the desert, only the cone lights blinked. He said he isn't scared while working. Because there's work to do.",
          "砂漠で長く耐えた夜、コーンの明かりだけが瞬いていた。彼は働いている間は怖くないと言った。やることがあるから。",
          "在沙漠里撑了很久的那一夜，只有锥桶的灯光在闪烁。他说干活的时候就不害怕，因为有事可做。");
        K("memory.foreman.6.title", "추후 업데이트 예정", "Coming Soon", "近日アップデート予定", "后续更新");
        K("memory.foreman.6.text", "숙련의 끝에서 그는 알았다. 표지판의 글은 거짓말이 아니었다.\n업데이트는 정말 온다 — 누군가 삽을 들기만 하면.",
          "At the end of mastery he realized the sign wasn't lying.\nThe update really does come, as long as someone picks up a shovel.",
          "熟練の果てで彼は悟った。看板の文字は嘘ではなかった。\nアップデートは本当に来る――誰かがシャベルを持ちさえすれば。",
          "在精通的尽头，他明白了：牌子上的字并没有撒谎。\n更新真的会来——只要有人拿起铁锹。");

        // ================================================================= 영혼 계약 · 보드
        K("contract.h.foreman.1.name", "백 번의 덤프", "A Hundred Dumps", "百回のダンプ", "一百次倾倒");
        K("contract.h.foreman.1.cond", "덤프 100번", "Dump 100 times", "ダンプを100回", "倾倒100次");
        K("contract.h.foreman.2.name", "열 번의 철거 공고", "Ten Demolition Notices", "十度の撤去通知", "十次拆除公告");
        K("contract.h.foreman.2.cond", "필살기 10번 쓰기", "Use the ultimate 10 times", "必殺技を10回使う", "使用必杀技10次");
        K("contract.h.foreman.3.name", "무게 15", "Weight 15", "重さ15", "重量15");
        K("contract.h.foreman.3.cond", "무게 15 더미 한 번 만들기", "Build a pile of weight 15 once", "重さ15の山を一度作る", "堆出一次重量15的堆");
        K("contract.h.foreman.4.name", "빠각!", "Crack!", "バキッ！", "咔嚓！");
        K("contract.h.foreman.4.cond", "빠각 50번", "Crack 50 times", "バキッを50回", "咔嚓50次");
        K("contract.h.foreman.6.name", "압착 삼백", "Three Hundred Crushed", "圧着三百", "碾压三百");
        K("contract.h.foreman.6.cond", "압착으로 적 300마리 쓰러뜨리기", "Defeat 300 enemies by crushing", "圧着で敵を300体倒す", "用碾压击倒300只敌人");
        K("board.foreman.1.name", "오백 번의 덤프", "Five Hundred Dumps", "五百回のダンプ", "五百次倾倒");
        K("board.foreman.1.desc", "쌓고 또 쌓고, 쏟아냈습니다. 현장은 늘 먼지투성이였습니다.", "Piled up and piled up, then dumped. The site was always full of dust.", "積んで、また積んで、ぶちまけました。現場はいつも埃まみれでした。", "堆了又堆，然后倾倒。工地上总是尘土飞扬。");
        K("board.foreman.1.cond", "덤프 500번", "Dump 500 times", "ダンプを500回", "倾倒500次");
        K("board.foreman.2.name", "네 왕을 밀어붙인 삽날", "The Blade That Pushed Four Kings", "四王を押し切った排土板", "推倒四王的推铲");
        K("board.foreman.2.desc", "왕들은 벽이었습니다. 그래도 밀렸습니다.", "The kings were walls. They still got pushed.", "王たちは壁でした。それでも押されました。", "诸王就是一堵堵墙。可还是被推倒了。");
        K("board.foreman.2.cond", "공사 반장으로 장 보스 4종 모두 쓰러뜨리기", "Defeat all four chapter bosses as the Foreman", "現場監督で章のボス4種をすべて倒す", "以工头击败全部四种章节首领");
        K("board.foreman.3.name", "빠각 삼백", "Three Hundred Cracks", "バキッ三百", "咔嚓三百");
        K("board.foreman.3.desc", "부딪히고 또 부딪혔습니다. 안전모는 멀쩡했습니다.", "Crashed and crashed again. The hard hat was fine.", "ぶつかって、またぶつかりました。ヘルメットは無事でした。", "撞了又撞。安全帽安然无恙。");
        K("board.foreman.3.cond", "빠각 300번", "Crack 300 times", "バキッを300回", "咔嚓300次");
    }
}
