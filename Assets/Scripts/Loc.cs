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
    // 1.5.6~ 언어 · 키 이름이 바뀔 때마다 +1 — 「바뀔 때만 글을 다시 쓰는」 곳은 이 값도 비교 (예전엔 언어를 바꿔도 이전 언어 글이 남았음)
    public static int Version { get; private set; }
    internal static void RaiseChanged() { Version++; reverse = null; Changed?.Invoke(); }

    // 1.5.6~ 화면에 보이는 번역 글 → 한국어 키 (언어 lang 기준 · 키 이름까지 바꾼 그대로). 한 번 번역해 넣고 잊은 글을 새 언어로 되돌릴 때 (SceneLocalizer)
    //   같은 번역이 여러 키에 있으면 먼저 찾은 키 (그 언어에서는 어느 쪽이든 같은 글)
    static Dictionary<string, string>[] reverse;
    public static string KeyOf(string shown, Lang lang)
    {
        if (string.IsNullOrEmpty(shown)) return null;
        if (LocTable.Entries.ContainsKey(shown)) return shown;
        reverse ??= new Dictionary<string, string>[4];
        int li = (int)lang;
        if (reverse[li] == null)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string[]> kv in LocTable.Entries)
            {
                string tr = li == 0 ? (!string.IsNullOrEmpty(kv.Value[0]) ? kv.Value[0] : kv.Key)
                          : li < kv.Value.Length && !string.IsNullOrEmpty(kv.Value[li]) ? kv.Value[li] : null;
                if (tr == null) continue;
                string applied = KeyBindings.Apply(tr);
                if (!map.ContainsKey(applied)) map[applied] = kv.Key;
            }
            reverse[li] = map;
        }
        return reverse[li].TryGetValue(shown, out string key) ? key : null;
    }

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
        // 1.2.4: 에디터에서 글리프를 미리 구운 글꼴 에셋(Resources/Fonts/NotoSansJP-Bold SDF 등)이 있으면 그것을 씀
        //   (실행 중 동적 생성은 새 글자마다 메인 스레드에서 그리고 2048² 텍스처를 올려 끊김) · 없으면 예전처럼 만듦
        TMP_FontAsset baked = Resources.Load<TMP_FontAsset>(path + " SDF");
        if (baked != null) return baked;
        Font f = Resources.Load<Font>(path);
        if (f == null) return null;
        TMP_FontAsset a = TMP_FontAsset.CreateFontAsset(f, 64, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048,
                                                        AtlasPopulationMode.Dynamic, true);
        if (a != null) a.name = f.name + " (runtime)";
        return a;
    }
}
