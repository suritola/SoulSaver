using UnityEngine;

// 번역표 (1.1.2~): 게임 중 안내 메시지 개편 (Notice) — 설정 「안내 메시지 크기」 · 도움말 닫기 · 최후의 보루 큰 알림
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddNotice()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 설정
        C("안내 메시지 크기", "안내 메시지 크기", "Message Size", "通知の大きさ", "提示信息大小");
        C("게임 중 뜨는 큰 알림 · 오른쪽 위 소식 · 도움말 글자의 크기입니다.", "게임 중 뜨는 큰 알림 · 오른쪽 위 소식 · 도움말 글자의 크기입니다.",
          "Sets the text size of in-game announcements, the news feed at the top right, and tips.",
          "ゲーム中に出る大きな通知・右上のお知らせ・ヒントの文字の大きさです。",
          "设置游戏中大型提示、右上角消息与提示文字的大小。");
        C("notice.size.0", "보통", "Normal", "標準", "标准");
        C("notice.size.1", "크게", "Large", "大", "大");
        C("notice.size.2", "아주 크게", "Extra Large", "特大", "特大");

        // ================================================================= 도움말 · 큰 알림
        C("notice.tip.close", "[{0}] 닫기", "[{0}] Close", "[{0}] 閉じる", "[{0}] 关闭");
        C("notice.laststand.sub", "2초 동안 무적이 되고 둘레의 적을 밀어냅니다.", "You are invincible for 2s and knock back nearby enemies.", "2秒間無敵になり、周囲の敵を押しのけます。", "2秒内无敌，并击退周围的敌人。");
    }
}
