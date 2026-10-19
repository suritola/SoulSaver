using UnityEngine;

// 번역표: 개발자 · 개발자 엄마 고유 기본 공격 (BossSkills.OwnKit · DevOwn · MomOwn · 도감 · 일시정지 · 보스바 · 망자의 전령)
// 기술 이름은 받은 피해 이름(「개발자 · 라이브 코딩」)의 부분 번역(AttackLabel.Translate)에도 쓰이므로 한국어 문장 자체를 키로
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지) · 「확인」은 이미 있음 (LocTable.cs)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddDevOwn()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void K(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        // ================================================================= 기술 이름
        K("라이브 코딩", "Live Coding", "ライブコーディング", "现场编程");
        K("범위 선택", "Select & Delete", "範囲選択", "框选删除");
        K("빌드 실패", "Build Failed", "ビルド失敗", "构建失败");
        K("오류 창", "Error Popup", "エラーダイアログ", "错误弹窗");
        K("로딩 중…", "Loading…", "読み込み中…", "加载中…");
        K("스택 오버플로", "Stack Overflow", "スタックオーバーフロー", "栈溢出");
        K("슬리퍼 투척", "Slipper Toss", "スリッパ投げ", "拖鞋飞袭");
        K("효자손", "Back Scratcher", "孫の手", "痒痒挠");
        K("타임 세일!", "Time Sale!", "タイムセール！", "限时抢购！");
        K("빗자루질", "Broom Sweep", "ほうき掃き", "扫帚横扫");
        K("엄친아", "Mom's Friend's Son", "ママ友の息子", "别人家的孩子");
        K("불 끄고 자!", "Lights Out!", "電気消して寝なさい！", "关灯睡觉！");

        // ================================================================= 머리 위 · 월드 글자
        K("빌드 중…", "Building…", "ビルド中…", "构建中…");
        K("빌드 실패!", "Build failed!", "ビルド失敗！", "构建失败！");
        K("다시 빌드…", "Rebuilding…", "再ビルド中…", "重新构建…");
        C("devown.popup.title", "오류", "Error", "エラー", "错误");
        C("devown.popup.ok", "확인!", "OK!", "OK！", "确定！");
        C("devown.popup.msg0", "영웅이 너무 강합니다. 너프가 필요합니다.", "Hero is too strong. Nerf required.", "英雄が強すぎます。ナーフが必要です。", "英雄太强了，需要削弱。");
        C("devown.popup.msg1", "프로그램이 응답하지 않습니다.", "The program is not responding.", "プログラムは応答していません。", "程序未响应。");
        C("devown.popup.msg2", "알 수 없는 오류가 발생했습니다.", "An unknown error has occurred.", "不明なエラーが発生しました。", "发生了未知错误。");
        C("devown.stack", "스택", "Stack", "スタック", "栈");
        K("스택 오버플로!", "Stack overflow!", "スタックオーバーフロー！", "栈溢出！");
        K("한 짝 더!", "And the other one!", "もう片方も！", "还有一只！");
        K("반값!", "Half price!", "半額！", "半价！");
        K("득템!", "Got it!", "ゲット！", "抢到了！");
        C("devown.medal.0", "엄마 친구 아들은 1등 했대", "Mom's friend's son came first", "ママ友の息子さんは一番だって", "别人家的孩子考了第一");
        C("devown.medal.1", "엄친아는 벌써 만렙이래", "He's already max level", "あの子はもうレベルMAXだって", "人家早就满级了");
        C("devown.medal.2", "엄친아는 결계에서 한 대도 안 맞았대", "He never got hit in a barrier", "あの子は結界で一発も食らわなかったって", "人家在结界里一下都没挨");
        C("devown.medal.3", "엄친아는 방도 매일 치운대", "He cleans his room every day", "あの子は毎日部屋を片付けるって", "人家天天收拾房间");
        K("비교 거부!", "No comparisons!", "比べないで！", "拒绝比较！");
        K("쉿… 자는 척!", "Shh… pretend to sleep!", "しーっ…寝たふり！", "嘘……装睡！");
        K("안 자고 뭐 해!", "Why are you still up?!", "まだ起きてるの！", "还不睡在干嘛！");
        K("…자는구나.", "…Asleep. Good.", "…寝てるわね。", "……睡着了啊。");
        K("…진짜 자?", "…Really asleep?", "…本当に寝てる？", "……真睡着了？");

        // ================================================================= 구간 알림
        C("devown.phase.dev1", "개발자가 새 기능을 배포했다!", "The Developer shipped new features!", "開発者が新機能をリリースした！", "开发者上线了新功能！");
        C("devown.phase.dev1.sub", "「버그 아니고 기능입니다.」", "\"It's not a bug, it's a feature.\"", "「バグじゃなくて仕様です。」", "「这不是 bug，是特性。」");
        C("devown.phase.dev2", "개발자가 금요일 밤 배포를 강행한다!", "The Developer is deploying on a Friday night!", "開発者が金曜の夜にデプロイを強行する！", "开发者要在周五晚上强行上线！");
        C("devown.phase.dev2.sub", "「테스트는 실서버에서 하면 되죠.」", "\"We'll just test it in production.\"", "「テストは本番でやればいいでしょ。」", "「测试嘛，上线了再测。」");
        C("devown.phase.mom1", "엄마가 고무장갑을 꼈다!", "Mom put on her rubber gloves!", "ママがゴム手袋をはめた！", "妈妈戴上了橡胶手套！");
        C("devown.phase.mom1.sub", "「오늘 이 방 다 치운다.」", "\"This room is getting cleaned today.\"", "「今日こそこの部屋を片付けるわよ。」", "「今天必须把这屋子收拾干净。」");
        C("devown.phase.mom2", "엄마가 성적표를 봤다!", "Mom saw your report card!", "ママが通知表を見た！", "妈妈看到成绩单了！");
        C("devown.phase.mom2.sub", "「엄마 친구 아들은 1등 했다더라.」", "\"Mom's friend's son came first, you know.\"", "「ママ友の息子さんは一番だったそうよ。」", "「别人家的孩子可是考了第一。」");
        C("devown.phase.mom3", "밤 11시, 엄마가 순찰을 돈다!", "11 p.m. — Mom is making her rounds!", "夜11時、ママの見回りが始まる！", "晚上11点，妈妈开始查房！");
        C("devown.phase.mom3.sub", "「불 끄고 자!」", "\"Lights out!\"", "「電気消して寝なさい！」", "「关灯睡觉！」");

        // ================================================================= 도움말 (Hints.Show 가 한국어 문장으로 번역)
        K("개발자의 「라이브 코딩」은 글자 하나하나가 탄입니다. 띄어쓰기 자리로 빠져나가세요. 초록 「//」 주석 줄은 실행되지 않아 맞아도 아프지 않습니다.",
          "In the Developer's Live Coding, every character is a bullet. Slip through where the spaces are. Green // comment lines never run, so they can't hurt you.",
          "開発者の「ライブコーディング」は一文字ずつが弾です。空白の位置をすり抜けましょう。緑の「//」コメント行は実行されないので、当たっても痛くありません。",
          "开发者的「现场编程」中，每个字符都是一发子弹。从空格的位置钻过去。绿色的「//」注释行不会执行，碰到也不会受伤。");
        K("「오류 창」이 떨어지면 「확인」 버튼 위에 서거나 창 밖으로 나가세요. 창 안의 다른 자리는 모두 지워집니다.",
          "When an Error Popup drops, stand on its OK button or get out of the window. Everywhere else inside it gets deleted.",
          "「エラーダイアログ」が落ちてきたら、「OK」ボタンの上に立つかウィンドウの外へ出ましょう。中のほかの場所はすべて消されます。",
          "「错误弹窗」落下时，站到「确定」按钮上或离开窗口。窗口内的其他地方都会被删除。");
        K("「엄친아」 메달은 부술 수 있습니다. 쫓아오기 전에 쏴서 깨뜨리세요. 오래 두면 그 자리에서 터집니다.",
          "The Mom's Friend's Son medals can be destroyed. Shoot them before they catch you. Left alone, they burst where they stand.",
          "「ママ友の息子」のメダルは壊せます。追いつかれる前に撃って壊しましょう。放っておくとその場で破裂します。",
          "「别人家的孩子」的奖牌可以打碎。在它追上你之前开枪打碎它。放着不管会在原地炸开。");
        K("엄마가 「불 끄고 자!」를 외치면 문틈 불빛 안에서는 움직이지 마세요. 가만히 있으면 자는 줄 압니다. 불빛 밖으로 나가도 됩니다. 제자리에서 쏘는 것은 괜찮지만, 몸이 앞으로 나가는 공격 · 기술은 들킵니다.",
          "When Mom shouts \"Lights out!\", don't move inside the light from the door. Stay still and she'll think you're asleep. You can also leave the light. Shooting in place is fine, but attacks or skills that move your body will get you caught.",
          "ママが「電気消して寝なさい！」と叫んだら、ドアの隙間の光の中では動かないでください。じっとしていれば寝ていると思われます。光の外へ出てもかまいません。その場で撃つのは大丈夫ですが、体が前に出る攻撃・技は見つかります。",
          "妈妈喊出「关灯睡觉！」时，在门缝透出的光里不要移动。一动不动，她就会以为你睡着了。走出光外也可以。原地射击没关系，但会让身体向前移动的攻击或技能会被发现。");

        // ================================================================= 도감 · 일시정지 · 보스바
        C("codex.dev.desc2",
          "15분까지 보스를 하나라도 쓰러뜨렸으면 15분에 모든 보스가 깨어나고, 그 보스들을 모두 쓰러뜨리면 강림합니다. 영웅들의 최종 진화 필살기를 처음엔 10초에 한 번, 체력이 닳을수록 빨라져 1초에 한 번까지 씁니다. 그 사이사이 개발자만의 기술도 씁니다: 띄어쓰기 자리가 빈틈인 「라이브 코딩」, 점선 사각형을 지우는 「범위 선택」, 막대가 차면 둘레가 터지는 「빌드 실패」. 체력이 66% 아래면 「오류 창」(「확인」 버튼 위는 안전)과 「로딩 중…」, 33% 아래면 「스택 오버플로」가 더해집니다. 싸우는 동안에는 약한 적만 1초에 4마리씩, 최대 30마리까지 나옵니다.",
          "If you have defeated at least one boss by the 15-minute mark, every boss awakens at 15 minutes; defeat them all and the Developer descends. It uses the heroes' final-evolution ultimates, once every 10 seconds at first and faster as its health drops, down to once every second. In between, it uses moves of its own: Live Coding (the spaces are the gaps), Select & Delete (it erases a dotted rectangle), and Build Failed (the area around it blows up when the bar fills). Below 66% health it adds Error Popup (its OK button is safe) and Loading…, and below 33% Stack Overflow. During the fight only weak enemies appear, 4 every second, up to 30 at a time.",
          "15分までにボスを1体でも倒していれば、15分に全てのボスが目覚め、それらを全て倒すと降臨します。英雄たちの最終進化必殺技を、最初は10秒に1回、体力が減るほど速く、最後は1秒に1回使います。その合間に開発者だけの技も使います：空白が隙間になる「ライブコーディング」、点線の四角を消す「範囲選択」、バーが満ちると周りが爆発する「ビルド失敗」。体力が66%を切ると「エラーダイアログ」（「OK」ボタンの上は安全）と「読み込み中…」、33%を切ると「スタックオーバーフロー」が加わります。戦っている間は弱い敵だけが1秒に4体ずつ、最大30体まで現れます。",
          "若在15分钟前至少击败过一名首领，15分钟时所有首领将苏醒；全部击败后开发者降临。它会使用英雄们的最终进化必杀技，起初每10秒一次，生命越低越快，最终每1秒一次。其间还会使出开发者专属的招式：以空格为缝隙的「现场编程」、删除虚线方框的「框选删除」、进度条填满就炸开周围的「构建失败」。生命低于66%时加入「错误弹窗」（站在「确定」按钮上是安全的）和「加载中…」，低于33%时再加入「栈溢出」。战斗期间只会出现弱小的敌人，每秒4名，最多同时30名。");
        C("codex.mom.desc2",
          "개발자를 쓰러뜨린 뒤 영혼 500개를 거두면 강림합니다. 개발자보다 체력이 2배이고, 피해와 이동 속도도 2배입니다. 영웅들의 필살기를 처음엔 5초에 한 번, 체력이 닳을수록 1초에 한 번까지 쓰고, 그 사이사이 엄마만의 기술도 씁니다: 그어진 선을 따라 날아오는 「슬리퍼 투척」, 다가서며 앞을 휩쓰는 「효자손」, 미리 그어 둔 길로 내달리는 「타임 세일!」. 체력이 75% 아래면 「빗자루질」, 50% 아래면 쏴서 깨뜨릴 수 있는 「엄친아」 메달, 25% 아래면 「불 끄고 자!」(불빛 안에서 움직이면 맞음)가 더해집니다. 싸우는 동안에는 약한 적만 1초에 4마리씩, 최대 30마리까지 나옵니다. 개발자와 엄마를 모두 쓰러뜨리면 무한 모드를 클리어합니다.",
          "Descends once you gather 500 souls after defeating the Developer. It has twice the Developer's health, and its damage and movement speed are doubled as well. It uses the heroes' ultimates once every 5 seconds at first, down to once every second as its health drops, and in between it uses moves of its own: Slipper Toss (it flies along the line she draws), Back Scratcher (she steps in and swats everything in front), and Time Sale! (she charges down a route she marks out first). Below 75% health she adds Broom Sweep, below 50% the Mom's Friend's Son medals (you can shoot them down), and below 25% Lights Out! (moving inside the light gets you hit). During the fight only weak enemies appear, 4 every second, up to 30 at a time. Defeat both the Developer and the Developer's Mom to clear Endless Mode.",
          "開発者を倒した後、魂を500個集めると降臨します。開発者の2倍の体力を持ち、ダメージと移動速度も2倍です。英雄たちの必殺技を最初は5秒に1回、体力が減るほど速く、最後は1秒に1回使い、その合間にママだけの技も使います：引いた線に沿って飛んでくる「スリッパ投げ」、踏み込んで前方をなぎ払う「孫の手」、先に描いた道を駆け抜ける「タイムセール！」。体力が75%を切ると「ほうき掃き」、50%を切ると撃って壊せる「ママ友の息子」のメダル、25%を切ると「電気消して寝なさい！」（光の中で動くと当たる）が加わります。戦っている間は弱い敵だけが1秒に4体ずつ、最大30体まで現れます。開発者とママを両方倒すと、エンドレスをクリアします。",
          "击败开发者后收集500个灵魂即会降临。它的生命是开发者的2倍，伤害和移动速度也是2倍。它会使用英雄们的必杀技，起初每5秒一次，生命越低越快，最终每1秒一次；其间还会使出妈妈专属的招式：沿着画出的线飞来的「拖鞋飞袭」、上前横扫正前方的「痒痒挠」、沿事先画好的路线狂奔的「限时抢购！」。生命低于75%时加入「扫帚横扫」，低于50%时加入可以打碎的「别人家的孩子」奖牌，低于25%时加入「关灯睡觉！」（在光里移动就会挨打）。战斗期间只会出现弱小的敌人，每秒4名，最多同时30名。击败开发者和妈妈即可通关无尽模式。");
        C("codex.dev.secret",
          "15분까지 보스를 하나라도 쓰러뜨렸으면, 15분에 깨어난 모든 보스를 쓰러뜨린 뒤 강림합니다. 영웅들의 최종 진화 필살기에 「라이브 코딩」 · 「범위 선택」 · 「빌드 실패」 같은 개발자만의 기술을 섞어 씁니다.",
          "If you have defeated at least one boss by the 15-minute mark, it descends once you defeat every boss that awakens at 15 minutes. It mixes the heroes' final-evolution ultimates with moves of its own, such as Live Coding, Select & Delete, and Build Failed.",
          "15分までにボスを1体でも倒していれば、15分に目覚めた全てのボスを倒した後に降臨します。英雄たちの最終進化必殺技に、「ライブコーディング」・「範囲選択」・「ビルド失敗」など開発者だけの技を織り交ぜて使います。",
          "若在15分钟前至少击败过一名首领，击败15分钟时苏醒的所有首领后便会降临。它会把英雄们的最终进化必杀技与「现场编程」、「框选删除」、「构建失败」等开发者专属招式混在一起使用。");
        C("codex.mom.secret",
          "개발자를 쓰러뜨린 뒤 영혼 500개를 거두면 강림합니다. 개발자보다 체력 · 피해 · 이동 속도가 2배이고, 영웅들의 필살기에 「슬리퍼 투척」 · 「효자손」 · 「불 끄고 자!」 같은 엄마만의 기술을 섞어 씁니다. 개발자와 엄마를 모두 쓰러뜨리면 무한 모드를 클리어합니다.",
          "Descends once you gather 500 souls after defeating the Developer. It has twice the Developer's health, damage, and movement speed, and mixes the heroes' ultimates with moves of its own, such as Slipper Toss, Back Scratcher, and Lights Out!. Defeat both the Developer and the Developer's Mom to clear Endless Mode.",
          "開発者を倒した後、魂を500個集めると降臨します。体力・ダメージ・移動速度は開発者の2倍で、英雄たちの必殺技に「スリッパ投げ」・「孫の手」・「電気消して寝なさい！」などママだけの技を織り交ぜて使います。開発者とママを両方倒すと、エンドレスをクリアします。",
          "击败开发者后收集500个灵魂即会降临。生命、伤害和移动速度都是开发者的2倍，会把英雄们的必杀技与「拖鞋飞袭」、「痒痒挠」、「关灯睡觉！」等妈妈专属招式混在一起使用。击败开发者和妈妈即可通关无尽模式。");
        C("codex.dev.tip",
          "영웅들의 최종 진화 필살기를 흉내 내고, 사이사이 자기 기술을 씁니다.\n· 라이브 코딩 — 띄어쓰기 자리가 빈틈 · 초록 「//」 줄은 안전 · 멈춰 깜빡인 줄은 되돌아옴\n· 범위 선택 — 점선 사각형 밖으로\n· 빌드 실패 — 막대가 차기 전에 멀리\n· 오류 창 — 「확인」 버튼 위나 창 밖 (체력 66% 아래)\n· 로딩 중… — 도는 빈자리로 빠져나가기 (66% 아래)\n· 스택 오버플로 — 쓰러지는 쪽에서 비켜서기 (33% 아래)",
          "Mimics the heroes' final-evolution ultimates, using moves of its own in between.\n· Live Coding — the spaces are the gaps · green // lines are safe · a line that stops and flickers comes back\n· Select & Delete — get out of the dotted rectangle\n· Build Failed — get away before the bar fills\n· Error Popup — stand on OK or leave the window (below 66% health)\n· Loading… — escape through the spinning gap (below 66%)\n· Stack Overflow — step aside from where it topples (below 33%)",
          "英雄たちの最終進化必殺技を真似し、合間に自分の技を使います。\n・ライブコーディング — 空白の位置が隙間 ・ 緑の「//」行は安全 ・ 止まって点滅した行は戻ってくる\n・範囲選択 — 点線の四角の外へ\n・ビルド失敗 — バーが満ちる前に離れる\n・エラーダイアログ — 「OK」ボタンの上かウィンドウの外へ（体力66%未満）\n・読み込み中… — 回る隙間から抜け出す（66%未満）\n・スタックオーバーフロー — 倒れる方向から退く（33%未満）",
          "模仿英雄们的最终进化必杀技，其间穿插自己的招式。\n· 现场编程 — 空格处就是缝隙 · 绿色「//」行是安全的 · 停下闪烁的那一行会折返\n· 框选删除 — 离开虚线方框\n· 构建失败 — 进度条填满前远离\n· 错误弹窗 — 站在「确定」按钮上或离开窗口（生命低于66%）\n· 加载中… — 从旋转的缺口钻出去（低于66%）\n· 栈溢出 — 避开它倒下的方向（低于33%）");
        C("codex.mom.tip",
          "영웅들의 필살기를 흉내 내고, 사이사이 엄마만의 기술을 씁니다.\n· 슬리퍼 투척 — 그어진 선에서 비켜서기 · 땅에 맞으면 한 번 더 튈 수 있음\n· 효자손 — 부채꼴 밖이나 엄마 등 뒤로\n· 타임 세일! — 그어 둔 길 위에 서 있지 않기\n· 빗자루질 — 빗자루 사이 빈틈으로 (체력 75% 아래)\n· 엄친아 — 메달은 쏴서 깨뜨릴 수 있음 (50% 아래)\n· 불 끄고 자! — 불빛 안에서는 가만히, 아니면 불빛 밖으로 (25% 아래)",
          "Mimics the heroes' ultimates, using moves of her own in between.\n· Slipper Toss — step off the drawn line · it can bounce once off the ground\n· Back Scratcher — get outside the fan or behind her\n· Time Sale! — don't stand on the route she marks\n· Broom Sweep — slip through the gap in the broom (below 75% health)\n· Mom's Friend's Son — the medals can be shot down (below 50%)\n· Lights Out! — hold still inside the light, or leave it (below 25%)",
          "英雄たちの必殺技を真似し、合間にママだけの技を使います。\n・スリッパ投げ — 引かれた線から退く ・ 地面に当たるともう一度跳ねることがある\n・孫の手 — 扇形の外かママの背後へ\n・タイムセール！ — 描かれた道の上に立たない\n・ほうき掃き — ほうきの隙間を抜ける（体力75%未満）\n・ママ友の息子 — メダルは撃って壊せる（50%未満）\n・電気消して寝なさい！ — 光の中ではじっと、さもなくば光の外へ（25%未満）",
          "模仿英雄们的必杀技，其间穿插妈妈专属的招式。\n· 拖鞋飞袭 — 离开画出的线 · 落地后可能再弹一次\n· 痒痒挠 — 躲到扇形外或妈妈身后\n· 限时抢购！ — 别站在她画好的路线上\n· 扫帚横扫 — 从扫帚的缝隙钻过去（生命低于75%）\n· 别人家的孩子 — 奖牌可以打碎（低于50%）\n· 关灯睡觉！ — 在光里一动不动，或者走出光外（低于25%）");
        C("codex.dev.rule", "체력이 줄수록 필살기를 더 자주 쓰고 새 기술이 더해지며, 결계 게이지가 차면 결계를 펼칩니다.",
          "As its health drops it uses ultimates more often and adds new moves, and it opens its barrier when the barrier gauge fills.",
          "体力が減るほど必殺技を頻繁に使い、新しい技が加わります。結界ゲージが満ちると結界を展開します。",
          "生命越低，必杀技用得越频繁，还会加入新招式；结界槽满时会展开结界。");
        C("devown.medal.name", "엄친아 메달", "Mom's Friend's Son Medal", "ママ友の息子のメダル", "别人家孩子的奖牌");
        C("devown.medal.peek", "엄마가 꺼내 든 비교의 상징. 6초 동안 천천히 쫓아오다 멈춰 터집니다. 쏴서 깨뜨릴 수 있습니다.",
          "Mom's symbol of comparison. It slowly chases you for 6 seconds, then stops and bursts. You can shoot it to break it.",
          "ママが持ち出した比較の象徴。6秒間ゆっくり追ってきた後、止まって破裂します。撃って壊せます。",
          "妈妈拿出的比较的象征。会慢慢追你6秒，然后停下炸开。可以开枪打碎。");

        // ================================================================= 망자의 전령 (Herald.Lines — cause=개발자 · skill=…)
        C("herald.boss.dev.code.h", "개발자의 코드는 글자마다 탄이다. 띄어쓰기 자리가 네 길이고, 초록 「//」 줄은 실행되지 않는다. 지나간 줄이 멈춰 깜빡이면 되돌아온다 — 그 줄이 지나간 자리에서 비켜라.",
          "The Developer's code is a bullet for every letter. The spaces are your path, and the green // lines never run. When a line that passed you stops and flickers, it is coming back; get off the path it took.",
          "開発者のコードは一文字ごとが弾だ。空白がお前の道で、緑の「//」の行は実行されぬ。通り過ぎた行が止まって点滅したら戻ってくる。その行が通った跡から退け。",
          "开发者的代码，每个字符都是一发子弹。空格就是你的路，绿色的「//」行不会执行。经过的那行一旦停下闪烁就会折返——离开它走过的路线。");
        C("herald.boss.dev.popup.h", "오류 창이 떨어지면 「확인」 버튼 위에 서라. 창 밖으로 나가도 된다. 창 안의 나머지는 모두 지워진다.",
          "When the error window drops, stand on the OK button. Leaving the window works too. Everything else inside it gets deleted.",
          "エラーウィンドウが落ちてきたら「OK」ボタンの上に立て。ウィンドウの外へ出てもいい。中の残りはすべて消される。",
          "错误窗口落下时，站到「确定」按钮上。离开窗口也行。窗口里其余的地方都会被删除。");
        C("herald.boss.dev.build.h", "막대가 차오르는 동안 개발자에게서 멀어져라. 빌드는 언제나 실패하고, 실패는 둘레를 날린다. 날아오는 오류 조각은 틈으로.",
          "While the bar fills, get away from the Developer. The build always fails, and the failure blows out everything around it. Take the gaps between the flying error shards.",
          "バーが満ちる間に開発者から離れよ。ビルドは必ず失敗し、失敗は周りを吹き飛ばす。飛んでくるエラーの破片は隙間で避けよ。",
          "进度条填满之前，远离开发者。构建总会失败，失败会炸开周围一圈。飞来的错误碎片，从缝隙里穿过去。");
        C("herald.boss.mom.slipper.h", "엄마의 슬리퍼는 그어진 선을 그대로 날아온다. 선에서 한 걸음 비켜라 — 두 번째 짝은 네가 비켜선 자리를 노리고, 땅에 맞으면 한 번 더 튄다.",
          "Mom's slipper flies exactly along the line she draws. Step off the line. The second one aims where you stepped, and a slipper that hits the ground can bounce once more.",
          "ママのスリッパは引かれた線のとおりに飛んでくる。線から一歩退け。もう片方はお前が退いた先を狙い、地面に当たればもう一度跳ねる。",
          "妈妈的拖鞋会沿着画出的线笔直飞来。离开那条线一步——另一只会瞄准你躲开的位置，落地后还会再弹一次。");
        C("herald.boss.mom.medal.h", "엄마 친구 아들의 메달은 부술 수 있다. 쫓아오기 전에 깨뜨려라. 비교를 끝내는 것은 언제나 너다.",
          "The medals of Mom's friend's son can be broken. Shatter them before they reach you. It is always you who ends the comparison.",
          "ママ友の息子のメダルは壊せる。追いつかれる前に砕け。比べられるのを終わらせるのは、いつだってお前だ。",
          "别人家孩子的奖牌是可以打碎的。在它追上你之前把它打碎。结束比较的，永远是你自己。");
        C("herald.boss.mom.lights.h", "「불 끄고 자!」 — 문틈 불빛 안에서는 숨도 쉬지 마라. 가만히 있으면 엄마는 네가 자는 줄 안다. 불빛 밖으로 나가도 된다. 그리고 엄마는 꼭 한 번 더 문을 연다.",
          "\"Lights out!\" Inside the light from the door, don't even breathe. Stay still and Mom thinks you're asleep. Leaving the light works too. And she always opens the door one more time.",
          "「電気消して寝なさい！」——ドアの隙間の光の中では息もするな。じっとしていれば、ママはお前が寝ていると思う。光の外へ出てもいい。そしてママは必ずもう一度ドアを開ける。",
          "「关灯睡觉！」——门缝透出的光里，连气都别喘。一动不动，妈妈就以为你睡着了。走出光外也行。而且妈妈一定会再开一次门。");
    }
}
