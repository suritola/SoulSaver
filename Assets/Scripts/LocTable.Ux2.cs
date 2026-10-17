using UnityEngine;

// 번역표 (1.1.9~): 편의성 · 사용자 경험 개선 2차 — 로딩 화면 · 메뉴 영웅 패널 · 영웅 안내 · 빌드 자세히 보기 · 보스 등장 대사 · 도감 이야기 · 저장 표시
//   · 모니터 · 최대 프레임 · 스팀 덱 · 접근성 탭 · 최고 기록 · 기록 창 · 보스 분노 · 확인 창 키 · 도감 검색 · 일시정지 중 HUD 설명
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddUx2()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        // ================================================================= 1. 로딩 화면
        S("불러오는 중", "Loading", "読み込み中", "加载中");
        S("레벨업 카드는 모아 두고 전투가 잠잠할 때 한꺼번에 골라도 됩니다.", "You can save up level-up cards and pick them all when the fight calms down.", "レベルアップカードはためておいて、戦闘が落ち着いたときにまとめて選んでも構いません。", "可以先攒着升级卡牌，等战斗平静时一起选择。");
        S("일시정지 메뉴에서 이번 판의 빌드와 능력치를 볼 수 있습니다.", "The pause menu shows your current build and stats.", "ポーズメニューで今回のビルドと能力値を確認できます。", "暂停菜单可查看本局构筑与属性。");
        S("떠돌이 상점을 놓치면 다음 상점이 더 빨리 옵니다.", "Miss a wandering shop and the next one arrives sooner.", "さすらいの商店を逃すと、次の商店が早く来ます。", "错过流浪商店后，下一家会更快出现。");
        S("보스 체력이 절반 아래로 내려가면 특수 스킬을 씁니다. 경고 표시를 잘 보세요.", "Bosses use special skills below half HP. Watch the warning marks.", "ボスは体力が半分以下になると特殊スキルを使います。警告表示に注意してください。", "首领生命低于一半时会使用特殊技能。请留意警告标记。");

        // ================================================================= 7. 저장 안전성
        S("저장됨", "Saved", "保存しました", "已保存");
        S("마지막 저장: {0}초 전", "Last saved {0}s ago", "最後の保存：{0}秒前", "上次保存：{0}秒前");
        S("저장하지 못했습니다. 저장 폴더를 확인해 주세요.", "Could not save. Please check the save folder.", "保存できませんでした。保存フォルダを確認してください。", "保存失败，请检查存档文件夹。");

        // ================================================================= 13. 확인 창 키보드
        S("[Enter] 확인   [ESC] 취소", "[Enter] Confirm   [ESC] Cancel", "[Enter] 決定   [ESC] キャンセル", "[Enter] 确认   [ESC] 取消");

        // ================================================================= 15. 일시정지 중 HUD 설명
        S("멈춘 동안 화면의 아이콘이나 적 위에 마우스를 올리면 설명이 보입니다.", "While paused, hover over a HUD icon or an enemy to read its description.", "停止中に画面のアイコンや敵へカーソルを合わせると説明が表示されます。", "暂停时将鼠标移到界面图标或敌人上即可查看说明。");
        S("멈춘 동안 왼쪽 스틱으로 커서를 아이콘이나 적 위에 두면 설명이 보입니다.", "While paused, move the cursor with the left stick onto a HUD icon or an enemy to read its description.", "停止中に左スティックでカーソルをアイコンや敵の上に置くと説明が表示されます。", "暂停时用左摇杆将光标移到界面图标或敌人上即可查看说明。");
        S("평타 한 발의 피해입니다. 멀티 샷이면 발당 피해 × 발사 수로 보입니다.", "Damage of one basic attack. With Multi Shot it shows damage per shot × number of shots.", "通常攻撃1発のダメージです。マルチショットなら1発のダメージ×発射数で表示します。", "一次普攻的伤害。多重射击时显示为每发伤害 × 发射数。");
        S("받는 피해를 이 비율만큼 줄입니다. 상한이 있습니다.", "Reduces damage taken by this percentage. It has a cap.", "受けるダメージをこの割合だけ減らします。上限があります。", "按此比例减少所受伤害。有上限。");
        S("초당 평타 횟수입니다.", "Basic attacks per second.", "1秒あたりの通常攻撃回数です。", "每秒普攻次数。");
        S("탄창을 다 쓴 뒤 다시 채우는 데 걸리는 시간입니다. 탄창이 없는 영웅은 「-」로 보입니다.", "Time to refill the magazine after it runs out. Heroes without a magazine show \"-\".", "弾倉を使い切った後、補充にかかる時間です。弾倉のないヒーローは「-」と表示されます。", "弹匣用尽后重新装填所需的时间。没有弹匣的英雄显示为「-」。");
        S("시작 속도에 비한 지금 이동 속도입니다.", "Current move speed compared with your starting speed.", "開始時の速度に対する現在の移動速度です。", "与初始速度相比的当前移动速度。");
        S("지금 체력과 최대 체력입니다. 0이 되면 쓰러집니다.", "Current and maximum HP. You fall at 0.", "現在の体力と最大体力です。0になると倒れます。", "当前生命值与最大生命值。降到0时倒下。");
        S("공격이 치명타가 될 확률입니다. 영혼 트리에서 올립니다.", "Chance for an attack to be a critical hit. Raised in the Soul Tree.", "攻撃がクリティカルになる確率です。魂のツリーで上げます。", "攻击成为暴击的概率。在灵魂树中提升。");
        S("치명타가 났을 때의 피해 배율입니다.", "Damage multiplier on a critical hit.", "クリティカル時のダメージ倍率です。", "暴击时的伤害倍率。");
        S("버프 · 특수 능력으로 모든 피해에 곱해지는 값입니다.", "Multiplier applied to all your damage by buffs and special abilities.", "バフ・特殊能力によって全てのダメージに掛かる倍率です。", "增益与特殊能力对所有伤害的乘数。");
        S("총알 하나가 뚫고 지나가는 적의 수입니다.", "Number of enemies one bullet passes through.", "弾1発が貫通する敵の数です。", "一发子弹可穿透的敌人数量。");
        S("초당 회복하는 체력입니다. 적을 쓰러뜨릴 때 회복하는 양은 작게 붙습니다.", "HP recovered per second. HP recovered per kill is shown smaller.", "1秒あたりに回復する体力です。敵を倒したときの回復量は小さく表示されます。", "每秒恢复的生命值。击倒敌人时恢复的量以小字显示。");
        S("지금 키우는 카드가 레벨업 카드에 나올 확률을 올립니다. 1마다 40%씩 더 자주 나옵니다.", "Makes the cards you are already leveling appear more often. Each point is +40%.", "育てているカードがレベルアップカードに出る確率を上げます。1につき40%多く出ます。", "提高正在培养的卡牌出现在升级卡牌中的概率。每点 +40%。");
        S("최대 레벨 뒤에 고른 성장 카드 수와 영혼 정련 횟수입니다.", "Growth cards picked after max level and Soul Refinements done.", "最大レベル後に選んだ成長カードの数と魂の精錬の回数です。", "达到最高等级后选取的成长卡牌数与灵魂精炼次数。");
        S("경험치", "EXP", "経験値", "经验值");
        S("다음 레벨까지의 경험치입니다. 가득 차면 레벨업 카드를 고를 수 있습니다.", "Experience toward the next level. When full, you can pick a level-up card.", "次のレベルまでの経験値です。満タンになるとレベルアップカードを選べます。", "距离下一级的经验值。满后可选择升级卡牌。");
        S("필살기 게이지", "Ultimate Gauge", "必殺技ゲージ", "必杀技能量");
        S("적을 쓰러뜨리면 차고, 가득 차면 [{ULT}]로 필살기를 씁니다.", "Fills as you defeat enemies. When full, press [{ULT}] to use your Ultimate.", "敵を倒すと溜まり、満タンになると [{ULT}] で必殺技を使えます。", "击倒敌人时积累，满后按 [{ULT}] 使用必杀技。");
        S("남은 탄과 탄창 크기입니다. [{RELOAD}]로 재장전하고, 다 쓰면 저절로 재장전합니다.", "Rounds left and magazine size. Press [{RELOAD}] to reload; it reloads automatically when empty.", "残弾と弾倉の大きさです。[{RELOAD}] でリロードし、撃ち切ると自動でリロードします。", "剩余子弹与弹匣容量。按 [{RELOAD}] 装填，打空时会自动装填。");
        S("이번 판에 모은 코인입니다. 떠돌이 상점에서 쓰고, 판이 끝나면 사라집니다.", "Coins collected this run. Spend them at the wandering shop; they are gone when the run ends.", "今回のランで集めたコインです。さすらいの商店で使い、ランが終わると消えます。", "本局收集的金币。在流浪商店使用，对局结束后消失。");
        S("영혼 트리에서 칸을 배우는 데 씁니다. 적을 쓰러뜨리면 떨어집니다.", "Spent on Soul Tree nodes. Dropped by defeated enemies.", "魂のツリーの枝を習得するのに使います。敵を倒すと落ちます。", "用于学习灵魂树的节点。击倒敌人时掉落。");
        S("이번 판의 플레이 시간입니다. 멈춘 시간은 빼고 셉니다.", "Play time this run. Paused time is not counted.", "今回のランのプレイ時間です。停止中の時間は数えません。", "本局的游戏时间。不计算暂停时间。");
        S("[{UPGRADE}]로 영혼 트리를 엽니다. 배울 수 있는 칸이 있으면 버튼이 빛납니다.", "Press [{UPGRADE}] to open the Soul Tree. The button glows when a node can be learned.", "[{UPGRADE}] で魂のツリーを開きます。習得できる枝があるとボタンが光ります。", "按 [{UPGRADE}] 打开灵魂树。有可学习的节点时按钮会发光。");
        // ================================================================= 2. 메뉴 영웅 패널 · 선택하고 시작
        S("영웅 바꾸기", "Change Hero", "ヒーローを変える", "更换英雄");
        S("선택하고 시작", "Select & Start", "選んで開始", "选择并开始");
        S("지금 영웅", "Current Hero", "現在のヒーロー", "当前英雄");

        // ================================================================= 3. 영웅 첫 판 안내
        S("영웅 안내", "Hero Guide", "ヒーローガイド", "英雄指南");
        S("머리 위 막대", "Bar Above Head", "頭上のバー", "头顶条");
        S("시작!", "Start!", "開始！", "开始！");
        S("일시정지 메뉴의 「영웅 안내」에서 언제든 다시 볼 수 있습니다.", "You can read this again any time from Hero Guide in the pause menu.", "ポーズメニューの「ヒーローガイド」でいつでも読み返せます。", "可随时在暂停菜单的「英雄指南」中再次查看。");
        S("숨 — 활을 당기는 동안 줄고, 바닥나면 활이 떨려 위력이 절반이 됩니다. 「완벽!」 발사가 숨을 되돌립니다.", "Breath — drains while drawing the bow; when empty the bow shakes and deals half damage. A Perfect shot restores it.", "息 — 弓を引いている間に減り、尽きると弓が震えて威力が半分になります。「パーフェクト！」の一撃で回復します。", "呼吸 — 拉弓时消耗，耗尽后弓会颤抖、威力减半。「完美！」射击可将其恢复。");
        S("불안정 — 던질수록 올라 폭발이 커지고, 가득 차면 발밑에서 자폭합니다.", "Instability — rises with every throw and makes explosions bigger; when full, it blows up at your feet.", "不安定 — 投げるほど上がり爆発が大きくなり、満タンになると足元で自爆します。", "不稳定 — 每次投掷都会上升、爆炸更大；满了会在脚下自爆。");
        S("다음 획 — 세 번째 획마다 삼각형이 닫혀 안쪽 적이 크게 다칩니다.", "Next Stroke — every third stroke closes a triangle that heavily damages enemies inside.", "次の一画 — 3画目ごとに三角形が閉じ、内側の敵に大ダメージを与えます。", "下一笔 — 每第三笔会闭合三角形，重创其中的敌人。");
        S("눈금 — 맞힐 때마다 차고, 다 차면 다음 톱니가 모든 적을 꿰뚫는 「정각」 톱니가 됩니다.", "Ticks — fill with every hit; when full, the next gear becomes an O'Clock gear that pierces everything.", "目盛り — 命中ごとに溜まり、満タンになると次の歯車が全てを貫く「正時」の歯車になります。", "刻度 — 每次命中积累，满后下一枚齿轮成为贯穿一切的「整点」齿轮。");
        S("배달 — 직접 보낸 편지를 열 번 배달하면 코인과 필살기 게이지를 받습니다. 표식 — 속달을 누르고 있는 동안 붙는 편지 수입니다.", "Deliveries — every 10 letters delivered grant coins and Ultimate gauge. Marks — the number of letters attached while holding Express.", "配達 — 自分で送った手紙を10回届けるとコインと必殺ゲージを得ます。印 — 速達を押している間に付く手紙の数です。", "投递 — 亲自送达10封信可获得金币与必杀能量。标记 — 按住速递时附着的信件数量。");
        S("영혼 — 쓰러뜨린 적에서 생겨 몸 둘레를 돌며, 맞을 때 체력 대신 하나씩 사라집니다.", "Wisps — rise from slain enemies and circle you; each one absorbs a hit instead of your HP.", "魂 — 倒した敵から生まれて周囲を回り、被弾時にHPの代わりに1つ消えます。", "魂灵 — 从倒下的敌人中产生并环绕你，受击时代替生命值逐个消失。");

        // ================================================================= 4. 빌드 자세히 보기
        S("빌드 자세히 보기", "Build Details", "ビルド詳細", "构筑详情");
        S("이번 판 빌드", "This Run's Build", "今回のビルド", "本局构筑");
        S("아직 없음", "None yet", "まだなし", "暂无");

        // ================================================================= 5. 보스 등장 대사
        S("보스 등장 대사", "Boss Entrance Lines", "ボス登場セリフ", "首领登场对白");
        S("처음만", "First Time Only", "初回のみ", "仅首次");
        S("항상", "Always", "常に", "总是");
        S("보스가 처음 나올 때는 늘 대사를 봅니다. 「처음만」이면 다음 판부터 이름만 짧게 보여 주고 바로 싸웁니다.", "You always see the lines the first time a boss appears. With First Time Only, later runs show just the name card briefly and start the fight.", "ボスの初登場時は必ずセリフを見ます。「初回のみ」なら次の周から名前だけを短く見せてすぐ戦闘に入ります。", "首领首次出现时总会显示对白。选择「仅首次」后，之后的局只短暂显示名字即开战。");

        // ================================================================= 6. 도감 이야기 탭
        S("이야기", "Story", "物語", "故事");
        S("시작 이야기", "Opening", "開幕の物語", "开场故事");
        S("보스 등장", "Boss Entrance", "ボス登場", "首领登场");
        S("엔딩", "Ending", "エンディング", "结局");
        C("codex.tab.story", "본 이야기만 글로 다시 읽을 수 있습니다. 영웅마다 시작 이야기와 엔딩이 다릅니다.", "Re-read the stories you have seen. Each hero has their own opening and ending.", "見た物語だけを文字で読み返せます。ヒーローごとに開幕とエンディングが異なります。", "可重读已看过的故事。每位英雄的开场与结局都不同。");
        S("아직 보지 못한 이야기", "Not Seen Yet", "まだ見ていない物語", "尚未看过的故事");
        S("이 영웅으로 판을 시작하면 볼 수 있습니다.", "Start a run as this hero to see it.", "このヒーローでランを始めると見られます。", "以这位英雄开始一局即可看到。");
        S("이 영웅으로 끝까지 클리어하면 볼 수 있습니다.", "Clear a run as this hero to see it.", "このヒーローでクリアすると見られます。", "以这位英雄通关即可看到。");
        S("{0}에서 만나면 볼 수 있습니다.", "Meet them in {0} to see it.", "{0}で出会うと見られます。", "在{0}遇到后即可看到。");

        // ================================================================= 8. 모니터 (최대 프레임 문구는 LocTable.Perf — 1.1.9 핫픽스)
        S("모니터", "Monitor", "モニター", "显示器");
        S("모니터 {0}", "Monitor {0}", "モニター {0}", "显示器 {0}");
        S("게임 창을 띄울 모니터입니다. 전체 화면이면 그 모니터의 해상도로 맞춥니다. 「이 탭 기본값으로」는 이 값을 바꾸지 않습니다.", "The monitor the game window appears on. In full screen, the game matches that monitor's resolution. Reset This Tab leaves this unchanged.", "ゲームウィンドウを表示するモニターです。全画面ならそのモニターの解像度に合わせます。「このタブを初期値に」では変わりません。", "游戏窗口所在的显示器。全屏时将匹配该显示器的分辨率。「重置此页」不会更改此项。");
        S("(수직 동기화 켜짐)", "(V-Sync On)", "(垂直同期オン)", "(垂直同步开启)");

        // ================================================================= 9. 스팀 덱
        S("스팀 덱에 맞춰 설정했습니다 (UI 크기 110% · 게임패드). 설정에서 바꿀 수 있습니다.", "Settings were adjusted for Steam Deck (UI size 110%, gamepad). You can change them in Settings.", "Steam Deck向けに設定しました（UIサイズ110%・ゲームパッド）。設定で変更できます。", "已针对 Steam Deck 调整设置（界面大小110% · 手柄）。可在设置中更改。");

        // ================================================================= 10. 접근성 탭
        S("접근성", "Accessibility", "アクセシビリティ", "辅助功能");
        S("표시", "HUD", "表示", "显示");
        S("공격 고정", "Attack Lock", "攻撃固定", "攻击锁定");
        S("켜면 공격 키를 한 번 누르면 손을 떼도 계속 쏘고, 다시 누르면 멈춥니다. 조준은 마우스 그대로입니다. 궁수는 가득 당겨지면 자동으로 놓되 「완벽!」 보너스는 받지 못하고, 숨이 차면 숨을 고릅니다.", "Press the attack key once to keep firing after you let go, and press it again to stop. Aiming still follows the mouse. The Archer releases automatically at full draw, gets no Perfect bonus, and rests when out of breath.", "攻撃キーを1回押すと離しても撃ち続け、もう1回押すと止まります。照準はマウスのままです。アーチャーは引き切ると自動で放ちますが「パーフェクト！」ボーナスは得られず、息が切れると休みます。", "按一次攻击键后松手也会持续射击，再按一次停止。瞄准仍跟随鼠标。弓手拉满后自动松开，但不会获得「完美！」加成，喘不过气时会停下休息。");
        S("[{ATTACK}] 다시 누르면 멈춤", "Press [{ATTACK}] again to stop", "[{ATTACK}] をもう一度押すと停止", "再按 [{ATTACK}] 停止");
        S("필살기 누르면 토글", "Ultimate: Press to Toggle", "必殺技：押してトグル", "必杀技：按下切换");
        S("켜면 필살기 키를 누르고 있는 대신 한 번 누르면 조준이 시작되고, 다시 누르면 발동합니다. 오래 누르기가 힘들 때 유용합니다.", "Press the Ultimate key once to start aiming and again to fire, instead of holding it. Helpful if holding keys is difficult.", "必殺キーを押し続ける代わりに、1回押すと照準を開始し、もう1回押すと発動します。長押しが難しいときに便利です。", "按一次必杀键开始瞄准，再按一次发动，无需长按。适合难以长按的玩家。");
        S("[{ULT}] 다시 누르면 발동", "Press [{ULT}] again to fire", "[{ULT}] をもう一度押すと発動", "再按 [{ULT}] 发动");
        S("대사 속도", "Dialogue Speed", "セリフ速度", "对白速度");
        S("빠름", "Fast", "速い", "快");
        S("즉시", "Instant", "即時", "立即");
        S("이야기 · 보스 대사 글자가 나타나는 속도입니다. 「즉시」는 한 줄이 한 번에 다 나옵니다.", "How fast story and boss dialogue text appears. Instant shows each line at once.", "物語・ボスのセリフ文字が表示される速さです。「即時」は1行が一度に表示されます。", "故事与首领对白文字的显示速度。「立即」会一次显示整行。");

        // ================================================================= 11. 최고 기록 · 기록 창
        S("기록", "Records", "記録", "记录");
        S("최고 {0}", "Best {0}", "最高 {0}", "最佳 {0}");
        S("최단 클리어 {0}", "Fastest clear {0}", "最速クリア {0}", "最快通关 {0}");
        S("지난 판", "Recent Runs", "最近のラン", "最近对局");
        S("영웅별 통계", "Stats by Hero", "ヒーロー別統計", "英雄统计");
        S("판 수", "Runs", "回数", "局数");
        S("클리어 수", "Clears", "クリア数", "通关数");
        S("최고 레벨", "Best Level", "最高レベル", "最高等级");
        S("최장 시간", "Longest Run", "最長時間", "最长时间");
        S("최단 클리어", "Fastest Clear", "最速クリア", "最快通关");
        S("가장 많이 쓰러진 원인", "Most Common Cause of Death", "最も多い敗因", "最常见的死因");
        S("합계", "Total", "合計", "合计");
        S("클리어", "Clear", "クリア", "通关");
        S("쓰러짐", "Fell", "敗北", "倒下");
        S("포기", "Gave Up", "ギブアップ", "放弃");
        S("아직 기록이 없습니다. 한 판을 끝내면 여기에 남습니다.", "No records yet. Finish a run and it will appear here.", "まだ記録がありません。ランを終えるとここに残ります。", "暂无记录。完成一局后将显示在此。");

        // ================================================================= 12. 보스전 정보
        S("분노!", "Enraged!", "激怒！", "狂暴！");
        S("{0}의 체력이 절반 아래입니다. 이제 특수 스킬을 씁니다.", "{0} is below half HP. It now uses special skills.", "{0}の体力が半分以下になりました。これから特殊スキルを使います。", "{0}的生命值已低于一半。现在开始使用特殊技能。");
        S("체력이 절반 아래로 내려가면 특수 스킬을 쓰고, 결계 게이지가 차면 결계를 펼칩니다.", "Below half HP it uses special skills, and when the gauge fills it unleashes its Barrier.", "体力が半分以下になると特殊スキルを使い、ゲージが満ちると結界を展開します。", "生命低于一半时会使用特殊技能，结界能量满时会展开结界。");
        S("체력 66% · 33%에서 단계가 오르고, 결계 게이지가 차면 결계를 펼칩니다.", "It enters a new phase at 66% and 33% HP, and when the gauge fills it unleashes its Barrier.", "体力66%・33%で段階が上がり、ゲージが満ちると結界を展開します。", "生命值降至66%、33%时进入新阶段，结界能量满时会展开结界。");

        // ================================================================= 14. 도감 검색
        S("검색", "Search", "検索", "搜索");
        S("이름이나 설명으로 찾기…", "Search by name or description…", "名前や説明で検索…", "按名称或说明搜索…");
        S("키보드로 입력", "Type with a Keyboard", "キーボードで入力", "请用键盘输入");
        S("검색 결과 {0}개", "{0} results", "検索結果 {0}件", "搜索结果 {0} 条");
        S("일치하는 항목이 없습니다.", "No matches found.", "一致する項目がありません。", "没有匹配项。");
    }
}
