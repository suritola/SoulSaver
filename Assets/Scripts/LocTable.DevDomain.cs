using UnityEngine;

// 번역표: 개발자 결계 「디버그 모드」 · 엄마 결계 「엄마의 대청소」 (BossUltimate.Developer.cs · 도감)
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddDevDomainBoot() { if (!SceneFlow.DeferStartup(5, AddDevDomain)) AddDevDomain(); }      // 1.6.1~ 켤 때는 로딩 화면이 뜬 뒤로 (큰 함수는 불릴 때 통째로 JIT 되므로 작은 함수에서 미룸 · SceneFlow)

    static void AddDevDomain()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void K(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        // ================================================================= 개발자 「디버그 모드」
        K("디버그 모드", "Debug Mode", "デバッグモード", "调试模式");
        K("이 세계의 규칙은, 내가 쓴다.", "In this world, I write the rules.", "この世界のルールは、僕が書く。", "这个世界的规则，由我来写。");
        K("// 주석", "// comment", "// コメント", "// 注释");
        K("강제 종료", "Force Quit", "強制終了", "强制退出");

        // ================================================================= 엄마 「엄마의 대청소」
        K("엄마의 대청소", "Mom's Big Cleanup", "ママの大掃除", "妈妈的大扫除");
        K("다 치울 때까지, 아무 데도 못 나가.", "Nobody leaves until this place is spotless.", "片付くまで、どこにも行かせないよ。", "收拾干净之前，哪儿也别想去。");
        K("방 좀 치워!", "Clean your room!", "部屋を片付けなさい！", "把房间收拾一下！");
        K("밥 먹고 해!", "Eat first, then play!", "ご飯を食べてからにしなさい！", "先吃饭再玩！");
        K("게임 좀 그만해!", "Enough games already!", "ゲームはもうおしまい！", "别再玩游戏了！");
        K("몇 시인데 아직도 안 자니!", "Do you know what time it is? Go to bed!", "何時だと思ってるの、早く寝なさい！", "都几点了还不睡！");
        K("엄마가 몇 번을 말했니!", "How many times have I told you!", "何回言ったらわかるの！", "妈妈说了多少遍了！");
        K("통금 시간이다! 들어와!", "Curfew! Get inside!", "門限よ！入りなさい！", "门禁时间到了！快进来！");

        // ================================================================= 도감 (개발자 · 엄마 카드 뒤에 붙는 결계 문장)
        C("필살기 「디버그 모드」 — 12초 동안 결계 안이 디버그 화면으로 바뀝니다. 예상 이동 경로에 찍히는 브레이크포인트, 위에서부터 한 줄씩 실행되는 코드 줄(주석 처리된 한 줄만 안전합니다), 결계 벽에 튕기는 버그, 회전하는 무한 루프 탄막이 이어지고, 결계 끝 무렵에는 플레이어를 가로지르는 거대한 X자 「강제 종료」를 씁니다. 체력이 절반 아래면 「강제 종료」를 한 번 더 씁니다.", "필살기 「디버그 모드」 — 12초 동안 결계 안이 디버그 화면이 됩니다. 브레이크포인트 · 한 줄씩 실행되는 코드(주석 줄만 안전) · 튕기는 버그 · 무한 루프 탄막이 이어지고, 끝 무렵 X자 「강제 종료」를 씁니다. 체력이 절반 아래면 한 번 더 씁니다.",
          "Ultimate \"Debug Mode\" — for 12s the barrier becomes a debug screen: breakpoints, code lines running top to bottom (only the commented-out line is safe), bouncing bugs and infinite-loop bullets. Near the end it casts the giant X \"Force Quit\", once more below half health.",
          "必殺技「デバッグモード」— 12秒間、結界の中がデバッグ画面になります。ブレークポイント・1行ずつ実行されるコード（コメント行だけ安全）・跳ね返るバグ・無限ループの弾幕が続き、終わり際にX字の「強制終了」を使います。体力が半分を切るともう一度使います。",
          "必杀技「调试模式」— 12秒内结界变为调试画面：断点、逐行执行的代码（只有注释行安全）、反弹的 Bug、无限循环弹幕接连而来，临近结束时使出巨大 X 形「强制退出」。生命低于一半时会再用一次。");
        C("필살기 「엄마의 대청소」 — 14초 동안 결계 안을 대청소합니다. 내 자리를 내려치는 등짝 스매싱, 몸을 끌어당기며 먼지를 빨아들이는 청소기, 줄지어 떨어지는 빨래, 잔소리와 함께 쏟아지는 탄막이 이어지고, 통금 시간이 되면 결계가 절반 가까이 좁아집니다.", "필살기 「엄마의 대청소」 — 14초 동안 결계 안을 대청소합니다. 등짝 스매싱 · 끌어당기는 청소기 · 떨어지는 빨래 · 잔소리 탄막이 이어지고, 통금 시간이 되면 결계가 절반 가까이 좁아집니다.",
          "Ultimate \"Mom's Big Cleanup\" — for 14s she cleans the barrier: back smacks, a vacuum that drags you in, falling laundry and nagging bullets. At curfew the barrier shrinks to nearly half.",
          "必殺技「ママの大掃除」— 14秒間、結界の中を大掃除します。背中スマッシュ・引き寄せる掃除機・落ちてくる洗濯物・お小言の弾幕が続き、門限になると結界が半分近くまで狭まります。",
          "必杀技「妈妈的大扫除」— 14秒内对结界大扫除：后背一巴掌、把你吸过去的吸尘器、落下的衣物、唠叨弹幕接连而来；门禁时间一到，结界缩小到将近一半。");
    }
}
