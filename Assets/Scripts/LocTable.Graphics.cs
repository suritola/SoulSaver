using UnityEngine;

// 번역표: 설정 「화면」 탭 「그래픽 품질」 (GraphicsQuality · MenuExtras.BuildDisplay)
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddGraphicsBoot() { if (!SceneFlow.DeferStartup(5, AddGraphics)) AddGraphics(); }      // 1.6.1~ 켤 때는 로딩 화면이 뜬 뒤로 (큰 함수는 불릴 때 통째로 JIT 되므로 작은 함수에서 미룸 · SceneFlow)

    static void AddGraphics()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        S("그래픽 품질", "Graphics Quality", "グラフィック品質", "画质");
        S("높음", "High", "高", "高");
        S("낮음", "Low", "低", "低");
        S("낮음으로 두면 잔상 · 불꽃 · 빛 번짐 같은 꾸밈 효과만 줄여 내장 그래픽에서도 부드럽게 움직입니다. 적 · 공격 · 경고 표시 · 피해 숫자는 그대로입니다.",
          "Low reduces only decorative effects such as trails, embers and glow so the game runs smoothly on integrated graphics. Enemies, attacks, warning marks and damage numbers are unchanged.",
          "「低」にすると残像・火の粉・光のにじみなどの装飾効果だけを減らし、内蔵グラフィックでも滑らかに動きます。敵・攻撃・警告表示・ダメージ数字はそのままです。",
          "设为「低」时只会减少残影、火花、光晕等装饰效果，让游戏在集成显卡上也能流畅运行。敌人、攻击、警告标记和伤害数字保持不变。");
    }
}
