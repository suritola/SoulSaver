using UnityEngine;

// 번역표 (1.1.5~): 스팀 순위 창 (LeaderboardUI · 결과 화면 「순위 보기」 · 일일 도전 「전체 순위」)
// 열쇠는 lb. 접두어 (한국어 원문은 0번 칸) · 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
// 다른 번역표 파일을 고치지 않게 게임이 켜질 때 스스로 더함 (LocTable.SoulBrand 와 같은 방식)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddLeaderboard()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        C("lb.button", "순위표", "Leaderboard", "ランキング", "排行榜");
        C("lb.full", "전체 순위 보기", "Full leaderboard", "全ランキングを見る", "查看完整排名");
        C("lb.view", "순위 보기", "View ranking", "ランキングを見る", "查看排名");
        C("lb.tab.global", "전체 상위", "Top 100", "上位100", "前100名");
        C("lb.tab.around", "내 주변", "Around me", "自分の周辺", "我的附近");
        C("lb.tab.friends", "친구", "Friends", "フレンド", "好友");
        C("lb.col.rank", "순위", "Rank", "順位", "排名");
        C("lb.col.name", "플레이어", "Player", "プレイヤー", "玩家");
        C("lb.col.bosses", "보스 처치", "Bosses", "ボス撃破", "击败首领");
        C("lb.col.time", "버틴 시간", "Survived", "生存時間", "存活时间");
        C("lb.jump", "내 순위로", "Go to my rank", "自分の順位へ", "跳到我的排名");
        C("lb.mybest", "내 최고 기록", "My best", "自己ベスト", "个人最佳");
        C("lb.myrank", "내 순위 {0}위", "My rank #{0}", "自分の順位 {0}位", "我的排名 第{0}名");
        C("lb.total", "전체 {0}명", "{0} players", "全{0}人", "共{0}人");
        C("lb.uploading", "이번 판 기록을 올리는 중…", "Uploading this run's record…", "今回の記録を送信中…", "正在上传本局记录…");
        C("lb.empty.me", "아직 내 기록이 없습니다. 무한 모드를 플레이하면 순위에 오릅니다!",
          "You have no record yet. Play Endless mode to get ranked!",
          "まだ自分の記録がありません。エンドレスをプレイするとランキングに載ります！",
          "你还没有记录。玩无尽模式即可上榜！");
        C("lb.empty.friends", "친구 중에 기록이 있는 사람이 없습니다.", "None of your friends have a record yet.", "記録のあるフレンドはまだいません。", "还没有好友留下记录。");
        C("lb.hint.keys", "← → 탭 바꾸기 · Tab 순위표 바꾸기 · ↑ ↓ PageUp PageDown 목록 · End 내 줄 · ESC 닫기",
          "← → switch tab · Tab switch board · ↑ ↓ PageUp PageDown scroll · End my row · ESC close",
          "← → タブ切替 · Tab ランキング切替 · ↑ ↓ PageUp PageDown スクロール · End 自分の行 · ESC 閉じる",
          "← → 切换标签 · Tab 切换排行榜 · ↑ ↓ PageUp PageDown 滚动 · End 我的位置 · ESC 关闭");
        C("lb.hint.pad", "십자키 ← → 탭 바꾸기 · 오른쪽 스틱 목록 · [B] 닫기",
          "D-pad ← → switch tab · Right stick scroll · [B] close",
          "十字キー ← → タブ切替 · 右スティック スクロール · [B] 閉じる",
          "十字键 ← → 切换标签 · 右摇杆 滚动 · [B] 关闭");
    }
}
