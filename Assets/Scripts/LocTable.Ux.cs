using UnityEngine;

// 번역표 (1.1.9~): 편의성 · 사용자 경험 개선 — 패드 분리 안내 · 커서 가두기 · 탭 기본값 · 포기하고 결과 보기 · 배지 클릭 · 한 번 클릭 확정
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddUx()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 스팀 오버레이 · 게임패드 분리
        C("게임패드 연결이 끊어졌습니다", "게임패드 연결이 끊어졌습니다.", "The gamepad was disconnected.", "ゲームパッドの接続が切れました。", "手柄已断开连接。");
        C("게임패드가 연결되었습니다", "게임패드가 연결되었습니다.", "A gamepad was connected.", "ゲームパッドが接続されました。", "手柄已连接。");

        // ================================================================= 설정 「게임」: 커서 가두기 · 한 번 클릭 확정
        C("커서를 창 안에 가두기", "커서를 창 안에 가두기", "Keep Cursor in Window", "カーソルをウィンドウ内に固定", "将光标限制在窗口内");
        C("전투 중 마우스가 창 밖으로 나가지 않게 합니다. 멈춘 화면 · 메뉴 · 패드를 쓸 때는 풀립니다. 창 모드나 모니터가 두 대일 때 유용합니다.",
          "전투 중 마우스가 창 밖으로 나가지 않게 합니다. 멈춘 화면 · 메뉴 · 패드를 쓸 때는 풀립니다. 창 모드나 모니터가 두 대일 때 유용합니다.",
          "Keeps the mouse inside the window during combat. It is released on paused screens and menus, and while using a gamepad. This is useful in windowed mode or with two monitors.",
          "戦闘中にマウスがウィンドウの外へ出ないようにします。停止画面・メニュー・パッド使用中は解除されます。ウィンドウモードやモニター2台のときに便利です。",
          "战斗中防止鼠标移出窗口。暂停画面、菜单和使用手柄时会解除。窗口模式或双显示器时很有用。");
        C("카드 한 번 클릭으로 확정", "카드 한 번 클릭으로 확정", "One-Click Card Confirm", "カードをワンクリックで確定", "单击即确认卡牌");
        C("켜면 레벨업 · 보물 상자 · 보스 보상 카드와 영혼 트리 칸을 한 번만 눌러도 바로 확정됩니다. 끄면 카드는 누른 뒤 [{INTERACT}] · 더블클릭으로, 영혼 트리 칸은 두 번 눌러 확정합니다. 무기 진화는 늘 두 번 확인합니다.",
          "켜면 레벨업 · 보물 상자 · 보스 보상 카드와 영혼 트리 칸을 한 번만 눌러도 바로 확정됩니다. 끄면 카드는 누른 뒤 [{INTERACT}] · 더블클릭으로, 영혼 트리 칸은 두 번 눌러 확정합니다. 무기 진화는 늘 두 번 확인합니다.",
          "On: level-up, chest and boss-reward cards and Soul Tree nodes are confirmed with a single click. Off: click a card, then confirm with [{INTERACT}] or a double-click, and click a Soul Tree node twice. Weapon evolution always asks twice.",
          "オン：レベルアップ・宝箱・ボス報酬のカードと魂のツリーのマスを1回押すだけで確定します。オフ：カードは押してから [{INTERACT}]・ダブルクリックで、魂のツリーのマスは2回押して確定します。武器進化は常に2回確認します。",
          "开启：升级、宝箱、首领奖励卡牌与灵魂树节点单击即确认。关闭：卡牌点击后用 [{INTERACT}] 或双击确认，灵魂树节点需点击两次确认。武器进化始终二次确认。");
        C("카드를 클릭하면 바로 확정됩니다", "카드를 클릭하면 바로 확정됩니다.", "Click a card to confirm it right away.", "カードをクリックするとすぐ確定します。", "点击卡牌即可立即确认。");
        C("카드를 누르면 바로 확정됩니다", "카드를 누르면 바로 확정됩니다.", "Click a card to confirm it right away.", "カードを押すとすぐ確定します。", "点击卡牌即可立即确认。");

        // ================================================================= 설정: 이 탭 기본값으로
        C("이 탭 기본값으로", "이 탭 기본값으로", "Reset This Tab", "このタブを初期値に", "重置此页");
        C("한 번 더 누르면 되돌립니다", "한 번 더 누르면 되돌립니다", "Press Again to Reset", "もう一度押すと戻します", "再按一次即可重置");
        C("되돌렸습니다", "되돌렸습니다", "Defaults Restored", "初期値に戻しました", "已恢复默认");
        C("이 탭의 설정만 처음 값으로 되돌립니다(조작 탭은 모든 키 배치). 다른 탭과 언어 · 화면 모드 · 해상도는 그대로 둡니다.",
          "이 탭의 설정만 처음 값으로 되돌립니다. 조작 탭에서는 모든 키 배치를 되돌립니다. 다른 탭과 언어 · 화면 모드 · 해상도는 그대로 둡니다.",
          "Resets only this tab's settings to their defaults. On the Controls tab, all key bindings are reset. Other tabs, language, display mode and resolution are kept.",
          "このタブの設定だけを初期値に戻します。操作タブではすべてのキー配置を戻します。他のタブと言語・画面モード・解像度はそのままです。",
          "仅将此页的设置恢复为默认值。在操作页会恢复全部按键设置。其他页面以及语言、显示模式、分辨率保持不变。");

        // ================================================================= 무한 모드 · 일일 도전: 포기하고 결과 보기
        C("포기하고 결과 보기", "포기하고 결과 보기", "Give Up & See Results", "ギブアップして結果を見る", "放弃并查看结果");
        C("여기서 포기하고 결과를 볼까요?", "여기서 포기하고 결과를 볼까요?", "Give up here and see your results?", "ここでギブアップして結果を見ますか？", "要在此放弃并查看结果吗？");
        C("지금까지의 생존 시간 · 보스 처치 수가 기록되고 순위표에 올라갑니다.", "지금까지의 생존 시간 · 보스 처치 수가 기록되고 순위표에 올라갑니다.",
          "Your survival time and boss kills so far will be recorded and submitted to the leaderboard.",
          "ここまでの生存時間・ボス撃破数が記録され、ランキングに登録されます。",
          "至今的生存时间与击杀首领数将被记录并上传排行榜。");
        C("여기까지!", "여기까지!", "Run Ended", "ここまで！", "到此为止！");
        // 1.2.7~ 무한 모드 · 일일 도전 중 게임 종료 (EndlessQuit)
        C("지금 판을 끝내고 게임을 종료합니다. 지금까지의 생존 시간 · 보스 처치 수가 순위표에 올라갑니다.",
          "지금 판을 끝내고 게임을 종료합니다. 지금까지의 생존 시간 · 보스 처치 수가 순위표에 올라갑니다.",
          "This run will end and the game will close. Your survival time and boss kills so far will be submitted to the leaderboard.",
          "この回を終えてゲームを終了します。ここまでの生存時間・ボス撃破数がランキングに登録されます。",
          "本局将结束并关闭游戏。至今的生存时间与击杀首领数将上传排行榜。");
        C("순위표에 기록을 올리는 중…", "순위표에 기록을 올리는 중…", "Submitting to the leaderboard…", "ランキングに記録を登録中…", "正在上传排行榜记录…");

        // ================================================================= 「레벨업!」 배지 클릭
        C("눌러서 카드를 고릅니다. 모아 두고 나중에 골라도 됩니다.", "눌러서 카드를 고릅니다. 모아 두고 나중에 골라도 됩니다.",
          "Click to pick a card. You can also save them up and choose later.",
          "押してカードを選びます。ためておいて後で選んでも構いません。",
          "点击选择卡牌。也可以先攒着之后再选。");
    }
}
