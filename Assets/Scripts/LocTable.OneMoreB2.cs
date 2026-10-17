using UnityEngine;

// 1.2~ 「한 판 더」 구현에서 새로 쓴 짧은 글 (글 문서에 없던 것: 보드 토스트 · 칭호 · 기억 탭 안내 · 일일 도전 창 줄 · 1.2 범위 보드에 맞춘 기억 조건 ⑤)
// 문체는 문구 다듬기 지침(2026-10-16): 이름 · 버튼은 명사형, 설명은 「~합니다.」 완결 문장
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddOneMoreB2()
    {
        void A(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        A("board.open", "계약 보드", "Contract Board", "契約ボード", "契约板");
        A("board.cell.toast", "계약 보드 칸을 채웠습니다 · {0}", "Contract board cell filled · {0}", "契約ボードのマスを埋めました・{0}", "已填满契约板格子 · {0}");
        A("title.label", "칭호", "Titles", "称号", "称号");
        A("title.got", "칭호를 얻었습니다 · 「{0}」", "Title earned · \"{0}\"", "称号を獲得しました・「{0}」", "获得称号 · 「{0}」");
        A("title.none", "아직 얻은 칭호가 없습니다. 보드의 칸 · 기억 조각 · 일일 도전 완주로 얻습니다.",
          "No titles yet. Earn them from board cells, memories and Daily Challenge full runs.",
          "まだ称号がありません。ボードのマス・記憶・デイリーチャレンジの完走で手に入ります。",
          "还没有称号。可通过契约板格子、记忆与每日挑战完赛获得。");
        A("title.daily10", "달력을 넘기는 자", "The Page-Turner", "暦をめくる者", "翻日历之人");
        A("title.daily30", "매일 오는 자", "The Daily Visitor", "毎日来る者", "每日来访者");
        A("memory.cond.5b", "이 영웅으로 무한 모드에서 10분을 버티거나, 계약 보드에서 이 영웅의 칸을 모두 채우면 돌아옵니다. (칸 {0}/{1})",
          "Returns when you survive 10 minutes in Endless as this hero, or fill all of this hero's cells on the contract board. (Cells {0}/{1})",
          "この英雄でエンドレスを10分生き残るか、契約ボードでこの英雄のマスをすべて埋めると戻ります。（マス {0}/{1}）",
          "以这位英雄在无尽模式中坚持10分钟，或在契约板上填满这位英雄的全部格子后归来。（格 {0}/{1}）");
        A("memory.read", "눌러서 읽기", "Click to read", "クリックで読む", "点击阅读");
        A("memory.hint", "영웅마다 여섯 장의 기억이 있습니다. 돌아온 기억을 누르면 읽을 수 있습니다. 도감 수집률과 보상에는 들어가지 않습니다.",
          "Each hero has six memories. Click one that has returned to read it. Memories do not count toward Codex completion or its rewards.",
          "英雄ごとに6つの記憶があります。戻った記憶をクリックすると読めます。図鑑の収集率と報酬には含まれません。",
          "每位英雄有六段记忆。点击已归来的记忆即可阅读。记忆不计入图鉴收集率与奖励。");
        A("daily.today.title", "오늘의 보상", "Today's Rewards", "今日の報酬", "今日奖励");
        A("daily.today.row", "{0}분 버티기 · +{1} P", "Survive {0} min · +{1} P", "{0}分耐える・+{1} P", "坚持{0}分钟 · +{1} P");
        A("transcend.pips2", "다음 초월 카드까지 경험치 막대 {0} / {1}", "EXP bars to the next Transcendence card: {0} / {1}", "次の超越カードまで経験値バー {0} / {1}", "距离下一张超越卡的经验条 {0} / {1}");
        A("contract.result.newmemory", "새 기억 · {0}", "New memory · {0}", "新しい記憶・{0}", "新记忆 · {0}");
    }
}
