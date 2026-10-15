using UnityEngine;

// 1.1~ 결과 화면 · 포인트 배율 · 클리어 보너스 (ResultScreen, Point, StoryDirector 엔딩) 번역
// 열쇠는 「result.」 접두어 (한국어 문장도 E 의 첫 칸에) · 다른 표와 겹치지 않게 ContainsKey 로 확인한 뒤 넣음
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddResult()
    {
        void A(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        A("result.finalbonus", "최종 클리어 보너스 +{0} P", "Final clear bonus +{0} P", "最終クリアボーナス +{0} P", "最终通关奖励 +{0} P");
        A("result.bonus", "클리어 보너스 +{0}", "Clear bonus +{0}", "クリアボーナス +{0}", "通关奖励 +{0}");
        A("result.diffpoints", "포인트 ×{0}", "Points ×{0}", "ポイント ×{0}", "点数 ×{0}");
        A("result.goal.title", "다음 목표", "Next goals", "次の目標", "下一个目标");
        A("result.goal.hero", "영웅 · {0}", "Hero · {0}", "英雄・{0}", "英雄 · {0}");
        A("result.goal.mastery", "{0} 숙련", "{0} Mastery", "{0} 熟練度", "{0} 熟练度");
        A("result.goal.masteryhint", "클리어하면 +1", "+1 per clear", "クリアで+1", "通关+1");
        A("result.goal.kills", "업적 · 적 3,000마리 처치", "Achievement · Defeat 3,000 enemies", "実績・敵を3,000体撃破", "成就 · 击败3,000名敌人");
        A("result.goal.endless", "업적 · 무한 모드 20분 버티기", "Achievement · Survive 20 min in Endless", "実績・無限モードで20分耐える", "成就 · 无尽模式坚持20分钟");
        A("result.goal.ready", "지금 살 수 있습니다!", "Ready to buy!", "今すぐ買えます！", "现在就能购买！");
        A("result.goal.almost", "거의 다!", "Almost!", "あと少し！", "就差一点！");
        A("result.retry.same", "같은 설정으로 다시", "Retry Same Setup", "同じ設定でもう一度", "以相同设置再来");
        A("result.retry.hero", "캐릭터 바꿔 다시", "Change Hero", "キャラを変えて再挑戦", "换角色再来");
    }
}
