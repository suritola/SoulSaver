using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 다국어: 한국어 문장 자체를 열쇠로 영어 · 일본어 · 중국어 번역을 찾음 (번역이 없으면 한국어 그대로)
// 언어와 볼륨은 GameSettings에 저장
public static class Loc
{
    public enum Lang { Korean = 0, English = 1, Japanese = 2, Chinese = 3 }

    public static readonly string[] LangNames = { "한국어", "English", "日本語", "中文" };

    public static Lang Current => GameSettings.Language;

    public static event System.Action Changed;
    internal static void RaiseChanged() => Changed?.Invoke();

    // 번역한 뒤 {SKILL1} 같은 키 표시를 지금 설정된 키 이름으로 바꿈 (KeyBindings)
    public static string T(string ko) => KeyBindings.Apply(Translate(ko));

    static string Translate(string ko)
    {
        if (string.IsNullOrEmpty(ko)) return ko;
        if (!LocTable.Entries.TryGetValue(ko, out string[] tr)) return ko;
        int i = (int)Current;
        // 0번(한국어)은 한국어 문장을 새로 고쳐 쓸 때만 채움
        if (i == 0) return !string.IsNullOrEmpty(tr[0]) ? tr[0] : ko;
        // 1.0.4~ 다른 언어의 ""는 일부러 비운 번역 (영어에는 붙지 않는 단위 「발」 · 「개」 · 「종」 등) — 한국어로 돌아가지 않음
        if (i < tr.Length && tr[i] != null) return tr[i];
        return ko;
    }

    public static bool Has(string ko) => ko != null && LocTable.Entries.ContainsKey(ko);

    // ================================================================= fonts
    // 일본어 · 중국어 글자는 기본 폰트에 없으므로 Noto Sans (OFL)를 보조 폰트로 붙임
    static TMP_FontAsset jp, sc;

    public static void ApplyFonts()
    {
        List<TMP_FontAsset> fallback = TMP_Settings.fallbackFontAssets;
        if (fallback == null) return;
        TMP_FontAsset want = null;
        if (Current == Lang.Japanese) want = jp ??= MakeFont("Fonts/NotoSansJP-Bold");
        if (Current == Lang.Chinese) want = sc ??= MakeFont("Fonts/NotoSansSC-Bold");
        fallback.Remove(jp);
        fallback.Remove(sc);
        if (want != null) fallback.Insert(0, want);

        // 이미 떠 있는 글자들도 다시 그리게
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.font != null && t.font != jp && t.font != sc && t.font.fallbackFontAssetTable != null)
            {
                t.font.fallbackFontAssetTable.Remove(jp);
                t.font.fallbackFontAssetTable.Remove(sc);
                if (want != null) t.font.fallbackFontAssetTable.Insert(0, want);
            }
            t.havePropertiesChanged = true;
        }
    }

    // 1.1~ 스팀 이름처럼 언어와 상관없는 글: 한자가 □ 로 보이지 않게 Noto 를 전역 보조 폰트 맨 뒤에 붙임 (붙였으면 다시 그림)
    public static void EnsureCjkFallback(TMP_Text t)
    {
        List<TMP_FontAsset> fallback = TMP_Settings.fallbackFontAssets;
        if (fallback == null) return;
        bool added = false;
        foreach (TMP_FontAsset f in new[] { NativeFont(Lang.Chinese), NativeFont(Lang.Japanese) })
            if (f != null && !fallback.Contains(f)) { fallback.Add(f); added = true; }
        if (added && t != null) t.havePropertiesChanged = true;
    }

    // 설정 창의 언어 이름처럼 현재 언어와 상관없이 그 언어 글자로 보여야 할 때 (한국어 · 영어는 null = 기본 폰트)
    public static TMP_FontAsset NativeFont(Lang lang)
    {
        if (lang == Lang.Japanese) return jp ??= MakeFont("Fonts/NotoSansJP-Bold");
        if (lang == Lang.Chinese) return sc ??= MakeFont("Fonts/NotoSansSC-Bold");
        return null;
    }

    static TMP_FontAsset MakeFont(string path)
    {
        Font f = Resources.Load<Font>(path);
        if (f == null) return null;
        TMP_FontAsset a = TMP_FontAsset.CreateFontAsset(f, 64, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048,
                                                        AtlasPopulationMode.Dynamic, true);
        if (a != null) a.name = f.name + " (runtime)";
        return a;
    }
}
