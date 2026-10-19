// 번역표 (2026-10-19~): 빌드 갈래 공통 문구 — 입문 · 갈림길 · 외길 · 공명 · 극의 · 교차 · HUD · 도움말 (LevelShop.Paths · Paths)
// 영웅별 문구(갈래 이름 · 입문 카드 · 공명 · 극의 · 교차)는 Paths/LocTable.Paths.<영웅>.cs — 이 파일의 AddPaths 가 차례로 부름
// LocTable.Skills 의 정적 생성자가 부름 (이미 있는 키는 건드리지 않음)
public static partial class LocTable
{
    // 영웅별 파일이 같이 쓰는 등록 함수 (한국어 원문 = 키)
    static void PA(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }
    // 키(코드 속 한국어)는 두고 화면에 보일 한국어만 바꿀 때
    static void PK(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

    static void AddPaths()
    {
        // ================================================================= 이름 · 단계
        PA("갈래", "Path", "流派", "流派");
        PA("입문", "Path Seed", "入門", "入门");
        PA("외길", "Single Path", "一筋", "专精");
        PA("공명 I", "Resonance I", "共鳴 I", "共鸣 I");
        PA("공명 II", "Resonance II", "共鳴 II", "共鸣 II");
        PA("극의", "Mastery", "極意", "极意");
        PA("교차", "Crossover", "交差", "交汇");
        PA("완성", "Completion", "完成", "完成");
        PA("갈림길", "Crossroads", "分かれ道", "岔路");
        PA("대가", "Cost", "代償", "代价");
        PA("강점", "Strong", "強み", "优势");
        PA("약점", "Weak", "弱み", "劣势");
        PA("갈래 단계", "Path rank", "流派段階", "流派阶段");
        PA("갈래 카드", "Path cards", "流派カード", "流派卡");
        PA("지금 갈래", "Current path", "現在の流派", "当前流派");
        PA("공명 I까지 {0}", "{0} to Resonance I", "共鳴Iまであと{0}", "距共鸣I还差{0}");
        PA("공명 II까지 {0}", "{0} to Resonance II", "共鳴IIまであと{0}", "距共鸣II还差{0}");

        // ================================================================= 입문 · 갈림길 창
        PA("이번 판의 갈래를 고르세요", "Choose this run's path", "このプレイの流派を選んでください", "选择本局的流派");
        PA("두 번째 갈래 또는 외길", "A second path, or Single Path", "二つ目の流派か、一筋", "第二个流派，或专精");

        // ================================================================= 외길 카드
        PA("「{0}」 하나만 더 깊게 팝니다. 두 번째 갈래는 고르지 않습니다.",
           "Go deeper into {0} alone. You won't take a second path.",
           "「{0}」だけをさらに深く極めます。二つ目の流派は選びません。",
           "只深入钻研「{0}」，不再选择第二个流派。");
        PA("첫 갈래 하나만 더 깊게 팝니다. 두 번째 갈래는 고르지 않습니다.",
           "Go deeper into your first path alone. You won't take a second path.",
           "最初の流派だけをさらに深く極めます。二つ目の流派は選びません。",
           "只深入钻研第一个流派，不再选择第二个流派。");
        PA("갈래 카드 최대 단계 +1", "Path cards: max rank +1", "流派カードの最大段階+1", "流派卡最高等级+1");
        PA("늘어난 단계까지 모두 채우면 극의", "Fill the extra ranks to reach Mastery", "増えた段階まですべて満たすと極意", "填满增加的等级即可达成极意");

        // ================================================================= 배지 툴팁 · 도움말
        PA("갈래 단계(입문 1 + 갈래 카드 단계)가 4에 닿으면 공명 I, 8에 닿으면 공명 II가 열립니다. 두 갈래가 모두 공명 I에 닿으면 교차가 열립니다.",
           "Path rank (1 for the Path Seed + your path cards' ranks) unlocks Resonance I at 4 and Resonance II at 8. When both paths reach Resonance I, their Crossover opens.",
           "流派段階（入門1＋流派カードの段階）が4で共鳴I、8で共鳴IIが開きます。二つの流派がどちらも共鳴Iに達すると交差が開きます。",
           "流派阶段（入门1＋流派卡等级）达到4时开启共鸣I，达到8时开启共鸣II。两个流派都达到共鸣I时开启交汇。");
        PA("이번 판의 갈래를 고르세요. 입문 카드는 평타 · 움직임을 바꾸고 대가가 붙습니다. 레벨업에는 모든 카드가 나오고, 고른 갈래에 맞는 카드를 모으면 공명 · 극의가 열립니다.",
           "Choose this run's path. A Path Seed changes your basic attack or movement, and comes with a cost. Level-ups offer every card, and collecting cards that match your path unlocks its Resonance and Mastery.",
           "このプレイの流派を選んでください。入門カードは通常攻撃や動きを変え、代償が付きます。レベルアップにはすべてのカードが出て、選んだ流派に合うカードを集めると共鳴・極意が開きます。",
           "选择本局的流派。入门卡会改变普通攻击或移动方式，并附带代价。升级时会出现所有卡，收集与所选流派相符的卡即可开启共鸣与极意。");
        PA("갈림길: 두 번째 갈래를 고르거나 「외길」로 첫 갈래만 더 깊게 팝니다.",
           "Crossroads: take a second path, or choose Single Path to go deeper into your first.",
           "分かれ道：二つ目の流派を選ぶか、「一筋」で最初の流派だけをさらに深く極めます。",
           "岔路：选择第二个流派，或选「专精」只深入第一个流派。");

        // ================================================================= 영웅 아홉 (Paths/LocTable.Paths.<영웅>.cs)
        AddPathsGunner();
        AddPathsSwordsman();
        AddPathsRogue();
        AddPathsArcher();
        AddPathsAlchemist();
        AddPathsCartographer();
        AddPathsClockmaker();
        AddPathsPostman();
        AddPathsGhost();
    }
}
