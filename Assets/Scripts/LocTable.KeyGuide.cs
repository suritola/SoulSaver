using UnityEngine;

// 번역표 (1.1.2~): 특수 스킬 · 유물을 얻었을 때 어느 키로 쓰는지 안내 (SpecialAbilities.KeyGuide)
// 열쇠는 keyguide. 접두어 (한국어 원문은 0번 칸) · 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddKeyGuide()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 얻는 순간 (화면 가운데)
        C("keyguide.new.skill", "새 스킬", "New Skill", "新スキル", "新技能");
        C("keyguide.new.relic", "새 유물", "New Relic", "新しい遺物", "新遗物");
        C("keyguide.press.key", "{0} 키를 눌러 사용", "Press {0} to use", "{0} キーで使用", "按 {0} 键使用");
        C("keyguide.press.pad", "{0} 버튼을 눌러 사용", "Press {0} to use", "{0} ボタンで使用", "按 {0} 按钮使用");
        C("keyguide.press.none", "키가 비어 있습니다 · 설정 → 조작에서 정해 주세요", "No key assigned · Set one in Settings → Controls",
          "キーが未設定です・設定 → 操作で割り当ててください", "未指定按键 · 请在 设置 → 操作 中指定");
        C("keyguide.relic.shared", "두 유물은 같은 키 · 준비된 것부터 나갑니다", "Both relics share this key · whichever is ready fires first",
          "2つの遺物は同じキー・準備できた方から発動", "两个遗物共用此键 · 先就绪的先发动");

        // ================================================================= HUD 칸 설명
        C("keyguide.use", "[{0}]로 사용", "Use with [{0}]", "[{0}]で使用", "按 [{0}] 使用");

        // ================================================================= 보물 상자 「잠든 힘」 · 석관 유물 고르기 (들어갈 칸 · 키)
        C("keyguide.slot", "{0} 칸에 들어갑니다 · 스킬 {1}/{2}", "Goes into the {0} slot · Skill {1}/{2}",
          "{0} の枠に入ります・スキル {1}/{2}", "放入 {0} 栏位 · 技能 {1}/{2}");
        C("keyguide.slot.last", "마지막 스킬 칸 · 다 차면 「잠든 힘」은 더 나오지 않습니다", "Last skill slot · once full, Dormant Power stops appearing",
          "最後のスキル枠・埋まると「眠れる力」は出なくなります", "最后一个技能栏 · 栏满后不再出现「沉睡之力」");
        C("keyguide.relic.slot", "{0} 유물 칸에 들어갑니다 · 유물 {1}/{2}", "Goes into the {0} relic slot · Relic {1}/{2}",
          "{0} の遺物枠に入ります・遺物 {1}/{2}", "放入 {0} 遗物栏 · 遗物 {1}/{2}");
        C("keyguide.relic.share", "가진 유물 「{0}」과 같은 키 · 준비된 것부터 나갑니다", "Shares the key with 「{0}」 · whichever is ready fires first",
          "持っている「{0}」と同じキー・準備できた方から発動", "与已有的「{0}」共用按键 · 先就绪的先发动");
        C("keyguide.relic.last", "마지막 유물 칸 · 다 차면 석관은 코인을 줍니다", "Last relic slot · once full, sarcophagi give coins",
          "最後の遺物枠・埋まると石棺はコインをくれます", "最后一个遗物栏 · 栏满后石棺改为给金币");
    }
}
