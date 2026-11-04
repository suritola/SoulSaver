using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// 스킨 상점 미리보기 무대 녹화 (배치 모드): 메인 메뉴 → 스킨 상점 → 스킨마다 「미리보기」 무대를 한 바퀴씩 MP4 로
//   소리: 배치 모드는 소리가 나지 않으므로 SpecialFeedback.Tapped 로 재생 요청을 엿들어 직접 섞음
//   실행: pwsh tools/trailer/skin-record.ps1 -Hero Gunner   (결과 %LOCALAPPDATA%\SoulSaverBuild\Capture\skins\skins_<영웅>.mp4 + 구간표)
public class SkinCapture
{
    const int W = 1280, H = 720, Fps = 30, Rate = 48000;
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static Type T(string n) => Type.GetType(n + ", Assembly-CSharp");
    static object Get(object o, string f) => o.GetType().GetField(f, Any).GetValue(o);
    static void Set(object o, string f, object v) => o.GetType().GetField(f, Any).SetValue(o, v);

    // 섞을 소리: (시작 표본, 단음 표본, 음량, 음높이)
    static readonly List<(long at, float[] mono, float vol, float pitch)> voices = new List<(long, float[], float, float)>();
    static readonly Dictionary<AudioClip, float[]> cache = new Dictionary<AudioClip, float[]>();
    static long nowSample;

    static float[] Mono(AudioClip c)
    {
        if (cache.TryGetValue(c, out float[] m)) return m;
        if (c.loadState == AudioDataLoadState.Unloaded) c.LoadAudioData();
        float[] raw = new float[c.samples * c.channels];
        c.GetData(raw, 0);
        // 클립 표본율 → 48kHz 단음
        int n = (int)((long)c.samples * Rate / c.frequency);
        m = new float[n];
        for (int i = 0; i < n; i++)
        {
            double p = i * (double)c.frequency / Rate;
            int i0 = Math.Min((int)p, c.samples - 1);
            float s = 0f;
            for (int ch = 0; ch < c.channels; ch++) s += raw[i0 * c.channels + ch];
            m[i] = s / c.channels;
        }
        return cache[c] = m;
    }

    static void OnTap(AudioClip c, float vol, float pitch)
    {
        if (c == null) return;
        voices.Add((nowSample, Mono(c), vol, Mathf.Max(0.1f, pitch)));
    }

    [UnityTest]
    public IEnumerator Reel()
    {
        LogAssert.ignoreFailingMessages = true;
        string outDir = Environment.GetEnvironmentVariable("SKINCAP_OUT") ?? "C:/Temp/skins";
        string hero = Environment.GetEnvironmentVariable("SKINCAP_HERO") ?? "Gunner";
        float secs = float.Parse(Environment.GetEnvironmentVariable("SKINCAP_SECS") ?? "13.5");
        Directory.CreateDirectory(outDir);
        Time.captureFramerate = Fps;
        T("SkinFx").GetField("InCapture").SetValue(null, true);     // 배치 모드에서도 새 스킨 연출 · 소리
        SceneManager.LoadScene("MainMenu");
        for (int i = 0; i < 30; i++) yield return null;

        Type sf = T("SpecialFeedback");
        FieldInfo tap = sf.GetField("Tapped", Any);
        tap.SetValue(null, (Action<AudioClip, float, float>)OnTap);

        // 영웅 번호 · 스킨 목록 (그 영웅의 캐릭터 · 무기 스킨, 맨 앞에 기본)
        Type charId = T("CharacterId");
        int who = (int)Enum.Parse(charId, hero);
        Type skinData = T("SkinData"), skinKind = T("SkinKind");
        Array all = (Array)skinData.GetField("All", Any).GetValue(null);
        var list = new List<(object kind, object def, string label)>();
        list.Add((Enum.Parse(skinKind, "Character"), null, "기본"));
        foreach (string k in new[] { "Character", "Weapon" })
            foreach (object s in all)
                if (Get(s, "kind").ToString() == k && (int)Get(s, "who") == who)
                    list.Add((Get(s, "kind"), s, (k == "Weapon" ? "무기 · " : "") + TierName((int)Get(s, "tier")) + " · " + (string)Get(s, "name")));

        Camera cam = Camera.main;
        if (cam == null) { cam = new GameObject("CapCam").AddComponent<Camera>(); cam.orthographic = true; }
        cam.cullingMask |= 1 << 5;
        RenderTexture rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        Texture2D frame = new Texture2D(W, H, TextureFormat.RGBA32, false);

        // 스킨 상점 열기 (메뉴 캔버스 뿌리)
        Canvas root = null;
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (c.isRootCanvas) { root = c; break; }
        Type shop = T("SkinShopUI");
        shop.GetMethod("Open", Any).Invoke(null, new object[] { root.transform });
        yield return null;
        object ui = UnityEngine.Object.FindFirstObjectByType(shop);

        int perFrame = Rate / Fps;
        var videoAttr = new VideoTrackAttributes { frameRate = new MediaRational(Fps), width = W, height = H, includeAlpha = false, bitRateMode = UnityEditor.VideoBitrateMode.High };
        var audioAttr = new AudioTrackAttributes { sampleRate = new MediaRational(Rate), channelCount = 2, language = "ko" };
        string path = Path.Combine(outDir, "skins_" + hero + ".mp4");
        var marks = new List<string>();
        float[] mix = new float[perFrame];
        int frames = 0;
        using (var enc = new MediaEncoder(path, videoAttr, audioAttr))
        using (var buf = new NativeArray<float>(perFrame * 2, Allocator.Persistent))
        {
            foreach (var item in list)
            {
                Set(ui, "kind", item.kind);
                Set(ui, "who", who);
                Set(ui, "selected", item.def);
                shop.GetMethod("Rebuild", Any).Invoke(ui, null);
                shop.GetMethod("OpenStage", Any, null, Type.EmptyTypes, null).Invoke(ui, null);
                marks.Add((frames / (float)Fps).ToString("0.00") + "\t" + item.label);
                yield return null;
                object stage = Get(ui, "stage");
                Camera stageCam = Get(stage, "cam") as Camera;
                int n = Mathf.RoundToInt(secs * Fps);
                for (int f = 0; f < n; f++)
                {
                    yield return null;
                    CanvasesToCamera(cam);
                    if (stageCam != null) stageCam.Render();
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    frame.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                    frame.Apply();
                    RenderTexture.active = null;
                    enc.AddFrame(frame);

                    // 이번 프레임 소리 섞기
                    Array.Clear(mix, 0, perFrame);
                    for (int v = voices.Count - 1; v >= 0; v--)
                    {
                        var vc = voices[v];
                        bool alive = false;
                        for (int i = 0; i < perFrame; i++)
                        {
                            double p = (nowSample + i - vc.at) * (double)vc.pitch;
                            if (p < 0) continue;
                            int i0 = (int)p;
                            if (i0 >= vc.mono.Length - 1) break;
                            alive = true;
                            mix[i] += Mathf.Lerp(vc.mono[i0], vc.mono[i0 + 1], (float)(p - i0)) * vc.vol;
                        }
                        if (!alive && (nowSample - vc.at) * vc.pitch > vc.mono.Length) voices.RemoveAt(v);
                    }
                    var b = buf;
                    for (int i = 0; i < perFrame; i++) { float s = (float)Math.Tanh(mix[i] * 1.6f) * 0.9f; b[i * 2] = s; b[i * 2 + 1] = s; }
                    enc.AddSamples(buf);
                    nowSample += perFrame;
                    frames++;
                }
                // 무대 닫기 (같은 경로: Destroy → OnDestroy 가 빌린 전역을 되돌림)
                UnityEngine.Object.Destroy((stage as Component).gameObject);
                Set(ui, "stage", null);
                voices.Clear();
                yield return null;
            }
        }
        tap.SetValue(null, null);
        T("SkinFx").GetField("InCapture").SetValue(null, false);
        File.WriteAllLines(Path.Combine(outDir, "skins_" + hero + ".txt"), marks);
        Time.captureFramerate = 0;
        Debug.Log("[SKINCAP] done " + path + " frames=" + frames);
    }

    static string TierName(int t) => t == 0 ? "일반" : t == 1 ? "희귀" : t == 2 ? "영웅" : "전설";

    static void CanvasesToCamera(Camera cam)
    {
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 5f;
            }
    }
}
