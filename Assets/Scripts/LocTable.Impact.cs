using UnityEngine;

// 번역표 (2.0.0~): 설정 「접근성」 탭 「특수효과」 (영웅 손맛 연출 Impact) — 이미 있는 키는 건드리지 않음
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddImpact()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        S("특수효과", "Special Effects", "特殊演出", "特殊效果");
        C("specialfx.desc",
          "영웅의 결정적인 순간(평타 특수 조건 · 고유 스킬 · 스킬 진화 · 필살기)에 화면이 살짝 당겨지고 아주 짧게 느려졌다가 크게 흔들리며 효과음이 납니다. 「약하게」는 느려짐 없이 흔들림만 절반, 「꺼짐」은 모두 끕니다. 화면 흔들림 · 번쩍임 효과 설정도 따릅니다.",
          "At a hero's big moments (special basic-attack hits, signature skills, skill evolutions, ultimates) the screen zooms in slightly, slows for a split second, then shakes hard with a sound effect. \"Weak\" removes the slowdown and halves the shake; \"Off\" disables it all. Also follows the Screen Shake and Flash Effects settings.",
          "ヒーローの決定的な瞬間（通常攻撃の特殊条件・固有スキル・スキル進化・必殺技）に、画面が少し寄ってほんの一瞬スローになり、大きく揺れて効果音が鳴ります。「弱め」はスローなしで揺れ半分、「オフ」はすべて無効です。画面の揺れ・フラッシュ効果の設定にも従います。",
          "在英雄的关键时刻（普通攻击特殊条件、专属技能、技能进化、必杀技），画面会轻微拉近、极短暂地放慢，然后剧烈震动并播放音效。「弱」去掉放慢并将震动减半，「关」则全部关闭。同时遵循画面震动与闪光效果设置。");
    }
}
