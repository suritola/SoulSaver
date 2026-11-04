using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 스킨 소리 WAV 뽑기 (스킨 시청각 개편 5-4절 「귀로 확인하는 길」 · 2026-11-04 마무리)
//   판을 돌리지 않고 SkinLooks 표의 소리 레시피를 게임과 같은 합성(SkinAudio.RenderRecipe)으로 WAV 파일로 — 변주(_0 · _1 · _2)까지
//   저장: <프로젝트>/Recordings/SkinWav/<스킨 id>/<칸>_<변주>.wav (Recordings 는 git 에 안 올라감) · 끝나면 폴더를 엶
public static class SkinWavExport
{
    const string Root = "Soul Saver/스킨 소리 WAV/";

    [MenuItem(Root + "끼운 스킨 (지금 영웅)")]
    static void Equipped()
    {
        List<SkinDef> l = new List<SkinDef>();
        foreach (SkinKind k in new[] { SkinKind.Character, SkinKind.Weapon, SkinKind.Effect, SkinKind.Cursor })
        {
            SkinDef s = SkinData.Equipped(k);
            if (s != null) l.Add(s);
        }
        Export(l, "끼운 스킨");
    }

    [MenuItem(Root + "일반 등급 전부")] static void Tier0() => ExportTier(0);
    [MenuItem(Root + "희귀 등급 전부")] static void Tier1() => ExportTier(1);
    [MenuItem(Root + "영웅 등급 전부")] static void Tier2() => ExportTier(2);
    [MenuItem(Root + "전설 등급 전부")] static void Tier3() => ExportTier(3);

    static void ExportTier(int tier)
    {
        List<SkinDef> l = new List<SkinDef>();
        foreach (SkinDef s in SkinData.All) if (s.tier == tier) l.Add(s);
        Export(l, SkinData.TierNames[tier]);
    }

    static void Export(List<SkinDef> skins, string what)
    {
        string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Recordings", "SkinWav");
        int files = 0;
        try
        {
            for (int i = 0; i < skins.Count; i++)
            {
                SkinDef s = skins[i];
                SkinLook look = SkinLooks.Get(s);
                if (look == null) continue;
                if (EditorUtility.DisplayCancelableProgressBar("스킨 소리 WAV 뽑기", s.id, i / (float)Mathf.Max(1, skins.Count))) break;
                string sd = Path.Combine(dir, s.id);
                Directory.CreateDirectory(sd);
                foreach (var kv in look.sfx)
                {
                    // 게임과 같은 변주 규칙: | 로 나눈 변주는 그대로, 3변주 칸(걸음 · 몸짓 · 처치 · 명중 · 발사 · 클릭)은 음높이만 다른 셋
                    string[] parts = kv.Value.Split('|');
                    int n = parts.Length > 1 ? parts.Length : SkinAudio.ThreeWay(kv.Key) && !(kv.Key == "kill" && look.opt.Contains("scale")) ? 3 : 1;
                    for (int v = 0; v < n; v++)
                    {
                        string name = s.id + ":" + kv.Key + ":" + v;
                        float[] buf = SkinAudio.RenderRecipe(name, parts.Length > 1 ? parts[v] : parts[0], parts.Length > 1 || n == 1 ? 1 : v);
                        WriteWav(Path.Combine(sd, kv.Key + "_" + v + ".wav"), buf, SkinAudio.SampleRate);
                        files++;
                    }
                }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        Debug.Log("[SkinWav] " + what + " — WAV " + files + "개: " + dir);
        if (files > 0) EditorUtility.RevealInFinder(dir);
    }

    // 16비트 PCM 모노
    static void WriteWav(string path, float[] data, int rate)
    {
        using (BinaryWriter w = new BinaryWriter(File.Create(path)))
        {
            int bytes = data.Length * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + bytes);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16);
            w.Write((short)1);
            w.Write((short)1);
            w.Write(rate);
            w.Write(rate * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            w.Write(bytes);
            foreach (float f in data) w.Write((short)(Mathf.Clamp(f, -1f, 1f) * 32767f));
        }
    }
}
