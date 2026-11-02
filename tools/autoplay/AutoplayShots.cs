using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 자동 플레이 중 특정 순간의 화면 캡처 (복사본 전용 · AP_SHOTS=폴더 일 때만 · run.ps1 -Shots <폴더> -Render)
// 촬영 방식은 tools/qa/QaCapture.cs 와 같음: 본 카메라는 UI 층을 빼고 RenderTexture 에, UI 전용 카메라가 그 위에 화면 캔버스를 그림
// 시각마다 한 번씩만 (이름이 겹치지 않게 언어 접두사). 판정 · 진행에는 영향 없음 (찍는 동안 한두 프레임만 더 걸림)
public static class AutoplayShots
{
    public static string Dir;
    static string lang = "ko";
    static readonly HashSet<string> done = new HashSet<string>();
    static RenderTexture rt;
    static Texture2D frame;
    static Camera uiCam;
    const int UiLayer = 5;
    static readonly Dictionary<int, int> origOrder = new Dictionary<int, int>();

    public static void Init(string dir, string language)
    {
        Dir = dir; lang = language;
        if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);
    }
    public static bool On => !string.IsNullOrEmpty(Dir);
    public static bool Taken(string name) => done.Contains(name);

    // 이름이 같은 것은 한 번만 · 이름 앞에 언어
    public static bool Hold;      // 찍는 동안 봇이 창을 닫지 않게 (AutoplayBot.Update 가 봄)
    public static IEnumerator Shot(string name, float realWait = 0f)
    {
        if (!On || !done.Add(name)) yield break;
        if (realWait > 0f) Hold = true;
        for (float until = Time.realtimeSinceStartup + realWait; Time.realtimeSinceStartup < until;) yield return null;
        Camera cam = Camera.main;
        if (cam == null) yield break;
        int w = 1920, h = 1080;
        if (rt == null) { rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); frame = new Texture2D(w, h, TextureFormat.RGBA32, false); }
        cam.targetTexture = rt;
        Camera ui = UiCam(cam);
        PrepCanvases(ui);
        yield return null; yield return null;
        cam = Camera.main;
        if (cam == null) yield break;
        cam.targetTexture = rt;
        ui = UiCam(cam);
        PrepCanvases(ui);
        Canvas.ForceUpdateCanvases();
        int mask0 = cam.cullingMask;
        cam.cullingMask = mask0 & ~(1 << UiLayer);
        ui.orthographicSize = cam.orthographicSize;
        cam.Render();
        ui.Render();
        cam.cullingMask = mask0;
        RenderTexture.active = rt;
        frame.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        frame.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(Dir, lang + "_" + name + ".png"), frame.EncodeToPNG());
        Hold = false;
        AutoplayRunner.Log("찰칵 " + lang + "_" + name);
    }

    static void PrepCanvases(Camera cam)
    {
        SortingLayer[] layers = SortingLayer.layers;
        string top = layers[layers.Length - 1].name;
        var list = new List<(Canvas c, int order)>();
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            int id = c.GetInstanceID();
            if (c.isRootCanvas)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) { origOrder[id] = c.sortingOrder; c.renderMode = RenderMode.ScreenSpaceCamera; }
                if (!origOrder.ContainsKey(id)) continue;
                c.worldCamera = cam;
                c.planeDistance = 5f;
                foreach (Transform t in c.GetComponentsInChildren<Transform>(true)) if (t.gameObject.layer != UiLayer) t.gameObject.layer = UiLayer;
                list.Add((c, origOrder[id]));
            }
            else if (c.overrideSorting)
            {
                Canvas root = c.rootCanvas;
                if (root == null || !(origOrder.ContainsKey(root.GetInstanceID()) || root.renderMode == RenderMode.ScreenSpaceOverlay)) continue;
                if (!origOrder.ContainsKey(id)) origOrder[id] = c.sortingOrder;
                list.Add((c, origOrder[id]));
            }
        }
        list.Sort((a, b) => a.order.CompareTo(b.order));
        for (int i = 0; i < list.Count; i++)
        {
            list[i].c.overrideSorting = list[i].c.isRootCanvas ? true : list[i].c.overrideSorting;
            list[i].c.sortingLayerName = top;
            list[i].c.sortingOrder = 30000 + i * 2;
        }
    }

    static Camera UiCam(Camera main)
    {
        if (uiCam == null)
        {
            GameObject go = new GameObject("ApUiCamera");
            Object.DontDestroyOnLoad(go);
            uiCam = go.AddComponent<Camera>();
        }
        uiCam.CopyFrom(main);
        uiCam.clearFlags = CameraClearFlags.Depth;
        uiCam.cullingMask = 1 << UiLayer;
        uiCam.depth = main.depth + 50f;
        uiCam.targetTexture = main.targetTexture;
        uiCam.enabled = true;
        return uiCam;
    }
}
