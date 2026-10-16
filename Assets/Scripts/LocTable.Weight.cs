using UnityEngine;

// 1.2~ 영혼의 무게 (SoulWeight · 메인 메뉴 무게 버튼 · 고르는 창 · 캐릭터 화면 · 결과 화면) 번역
// 열쇠는 「weight.」 접두어 · 수치는 SoulWeight 상수와 같게 (바꾸면 같이)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddWeight()
    {
        void A(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        A("weight.title", "영혼의 무게", "Soul Weight", "魂の重み", "灵魂之重");
        A("weight.short", "무게 {0}", "Weight {0}", "重み {0}", "重量 {0}");
        A("weight.best", "최고 무게 {0}", "Best {0}", "最高 {0}", "最高 {0}");
        A("weight.newrecord", "새 기록!", "New record!", "新記録！", "新纪录！");
        A("weight.change", "바꾸기", "Change", "変更", "更改");
        A("weight.subtitle", "고른 무게까지의 항목이 모두 쌓입니다. 무게 1마다 포인트가 5% 늘어납니다.",
          "All effects up to the chosen weight stack. Each weight adds 5% more points.",
          "選んだ重みまでの効果がすべて重なります。重み1ごとにポイントが5%増えます。",
          "所选重量及以下的效果全部叠加。每1重量点数增加5%。");
        A("weight.points", "포인트 +{0}%", "Points +{0}%", "ポイント +{0}%", "点数 +{0}%");
        A("weight.locked", "이 영웅으로 무게 {0}까지 클리어하면 열립니다.", "Clear Weight {0} with this hero to unlock.", "この英雄で重み{0}をクリアすると解放されます。", "用此英雄通关重量{0}后解锁。");
        A("weight.none", "무게 없음 · 평소 어려움", "No weight · Regular Hard", "重みなし・通常のハード", "无重量 · 普通困难");
        A("weight.step.zero", "평소 어려움", "Regular Hard", "通常のハード", "普通困难");
        A("weight.step.hp", "적 체력 +15%", "Enemy HP +15%", "敵の体力+15%", "敌人生命+15%");
        A("weight.step.alive", "한 화면 적 수 +3 (이후 무게마다 +3)", "Max enemies on screen +3 (+3 more per weight after)", "画面内の敵の数+3（以降、重みごとに+3）", "同屏敌人数量+3（之后每级重量再+3）");
        A("weight.step.barrier", "보스 결계 게이지 속도 +35%", "Boss Barrier gauge speed +35%", "ボスの結界ゲージ速度+35%", "首领结界槽充能速度+35%");
        A("weight.step.heal", "회복 물약 · 고기 회복량 -40%", "Potion & meat healing -40%", "回復ポーション・肉の回復量-40%", "治疗药水 · 肉的回复量-40%");
        A("weight.step.reroll", "레벨업 카드 다시 뽑기 없음", "No level-up card rerolls", "レベルアップカードの引き直しなし", "无法重抽升级卡牌");
        A("weight.step.laststand", "최후의 보루 없음", "No Last Stand", "最後の砦なし", "无最后的堡垒");
        A("weight.fx.hp", "적 체력 +{0}%", "Enemy HP +{0}%", "敵の体力+{0}%", "敌人生命+{0}%");
        A("weight.fx.alive", "적 수 +{0}", "Enemies +{0}", "敵の数+{0}", "敌人数量+{0}");
        A("weight.fx.barrier", "결계 게이지 +{0}%", "Barrier gauge +{0}%", "結界ゲージ+{0}%", "结界槽+{0}%");
        A("weight.fx.heal", "회복 -{0}%", "Healing -{0}%", "回復-{0}%", "回复-{0}%");
        A("weight.unlocked", "새 도전 해금: 영혼의 무게!\n메인 메뉴에서 어려움을 고르면 무게를 질 수 있습니다.",
          "New challenge unlocked: Soul Weight!\nPick Hard in the main menu to take on weight.",
          "新たな挑戦解放：魂の重み！\nメインメニューでハードを選ぶと重みを背負えます。",
          "新挑战解锁：灵魂之重！\n在主菜单选择困难即可背负重量。");
        A("weight.cleared", "영혼의 무게 {0} 클리어!", "Soul Weight {0} cleared!", "魂の重み{0}クリア！", "灵魂之重{0}通关！");
        A("weight.next", "무게 {0} 해금", "Weight {0} unlocked", "重み{0}解放", "重量{0}已解锁");
        A("weight.detail", "영혼의 무게: 가장 무거운 클리어 {0} · 무게 {1}까지 고를 수 있습니다.",
          "Soul Weight: heaviest clear {0} · you can pick up to Weight {1}.",
          "魂の重み：最重クリア{0}・重み{1}まで選べます。",
          "灵魂之重：最高通关{0} · 最高可选择重量{1}。");
        A("weight.detail.none", "영혼의 무게: 아직 클리어 기록이 없습니다. (어려움에서 무게 1부터)",
          "Soul Weight: No clears yet. (Start from Weight 1 on Hard)",
          "魂の重み：まだクリア記録がありません。（ハードで重み1から）",
          "灵魂之重：尚无通关记录。（在困难中从重量1开始）");
        A("weight.menu.hardonly", "어려움을 고르면 「영혼의 무게」를 질 수 있습니다.",
          "Pick Hard to take on Soul Weight.",
          "ハードを選ぶと「魂の重み」を背負えます。",
          "选择困难即可背负「灵魂之重」。");
        A("weight.run", "영혼의 무게 {0} · 포인트 +{1}%", "Soul Weight {0} · Points +{1}%", "魂の重み{0}・ポイント+{1}%", "灵魂之重{0} · 点数+{1}%");     // 판 시작 알림 (StageManager.Opening)
        A("weight.potiondesc", "체력 {0}% 회복", "Restore {0}% HP", "体力{0}%回復", "恢复{0}%生命");
    }
}
