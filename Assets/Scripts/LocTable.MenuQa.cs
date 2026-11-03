using UnityEngine;

// 2.0.1~ 메뉴 화면 검수(4개 언어 캡처) 반영 — 값용 「무한」 · 도감 탭 짧은 이름 · 검색칸 안내
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddMenuQa()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        // 사거리 · 탄창 값 — 「무한」 키는 「무한 모드」(Endless · エンドレス · 无尽)라 값에는 따로
        C("무한.값", "무한", "Unlimited", "無限", "无限");
        // 도감 검색칸 자리 글 (좁은 칸에서 두 줄로 접히던 「이름이나 설명으로 찾기…」)
        S("검색…", "Search…", "検索…", "搜索…");
        // 도감 탭 짧은 이름
        C("codex.tab.hero", "영웅", "Heroes", "ヒーロー", "英雄");
        C("codex.tab.levelup", "레벨업", "Level Up", "Lvアップ", "升级");
    }
}
