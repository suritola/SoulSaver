using UnityEngine;

// UI 개편 P0-6 (2026-10-22~): 짧은 버튼 이름 (docs/art/UI-개편-기획서.md 3-10 · 8절 9번) — UiRules.Legacy 를 끈 새 모습에서만 이 키로 바꿔 닮
//   행동은 동사형 · 이동은 목적지 명사 · 영어 14자 · 일본어 6자 · 중국어 4자 안 · 「메인 메뉴」는 결과 화면 result.menu 와 같은 키
//   예전 한국어 원문 키(「계속하기」 등)는 옛 화면이 그대로 씀 (덮어쓰지 않음 — find-text 로 찾을 수 있게)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddUiButtons()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        C("ui.btn.resume", "계속", "Resume", "再開", "继续");
        C("ui.btn.build", "빌드", "Build", "ビルド", "构筑");
        C("ui.btn.giveup", "포기", "Give Up", "ギブアップ", "放弃");
        C("ui.btn.start", "시작", "Start", "スタート", "开始");
        C("ui.btn.quit", "종료", "Quit", "終了", "退出");
        C("ui.btn.ranks", "순위", "Ranks", "ランキング", "排行");
        C("ui.card.new", "새로", "New", "新規", "新");          // 레벨업 카드 머리 줄 (처음 배우는 카드)
    }
}
