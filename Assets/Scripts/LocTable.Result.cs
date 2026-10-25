using UnityEngine;

// 1.1~ 결과 화면 · 포인트 배율 · 클리어 보너스 (ResultScreen, Point, StoryDirector 엔딩) 번역
// 열쇠는 「result.」 접두어 (한국어 문장도 E 의 첫 칸에) · 다른 표와 겹치지 않게 ContainsKey 로 확인한 뒤 넣음
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddResultBoot() { if (!SceneFlow.DeferStartup(5, AddResult)) AddResult(); }      // 1.6.1~ 켤 때는 로딩 화면이 뜬 뒤로 (큰 함수는 불릴 때 통째로 JIT 되므로 작은 함수에서 미룸 · SceneFlow)

    static void AddResult()
    {
        void A(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        A("result.finalbonus", "최종 클리어 보너스 +{0} P", "Final clear bonus +{0} P", "最終クリアボーナス +{0} P", "最终通关奖励 +{0} P");
        A("result.bonus", "클리어 보너스 +{0}", "Clear bonus +{0}", "クリアボーナス +{0}", "通关奖励 +{0}");
        A("result.diffpoints", "포인트 ×{0}", "Points ×{0}", "ポイント ×{0}", "点数 ×{0}");
        A("result.goal.title", "다음 목표", "Next Goals", "次の目標", "下一个目标");
        A("result.goal.hero", "영웅 · {0}", "Hero · {0}", "英雄・{0}", "英雄 · {0}");
        A("result.goal.mastery", "{0} 숙련", "{0} Mastery", "{0} 熟練度", "{0} 熟练度");
        A("result.goal.masteryhint", "이야기 모드를 클리어하면 +1", "+1 per Story mode clear", "ストーリーモードクリアで+1", "通关故事模式+1");
        A("result.goal.kills", "업적 · 적 3,000마리 처치", "Achievement · Defeat 3,000 enemies", "実績・敵を3,000体撃破", "成就 · 击败3,000名敌人");
        A("result.goal.endless", "업적 · 무한 모드 20분 버티기", "Achievement · Survive 20 min in Endless", "実績・エンドレスで20分耐える", "成就 · 无尽模式坚持20分钟");
        A("result.goal.ready", "지금 살 수 있습니다!", "Ready to buy!", "今すぐ買えます！", "现在就能购买！");
        A("result.goal.almost", "거의 다!", "Almost!", "あと少し！", "就差一点！");
        A("result.retry.same", "다시 하기", "Retry", "もう一度", "再来一次");          // 2026-10-22~ 짧게 (예전 「같은 설정으로 다시」)
        A("result.retry.hero", "영웅 변경", "Change Hero", "英雄変更", "更换英雄");     // 예전 「영웅 바꿔 다시」
        A("result.menu", "메인 메뉴", "Main Menu", "メインメニュー", "主菜单");
    }
}
