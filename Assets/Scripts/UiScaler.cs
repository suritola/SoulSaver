using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 설정 (2.1.9~): UI 크기 · 창이 비활성일 때 소리 끄기
// UI 크기: 화면 크기에 맞춰 늘어나는 모든 캔버스(CanvasScaler · ScaleWithScreenSize)의 기준 해상도를 크기로 나눔 (작게 나누면 글자 · 버튼이 커짐)
//   씬 캔버스 · 실행 중에 만든 캔버스(HUD · 창) 모두: 1초마다 새 캔버스를 찾아 맞춤 (처음 본 기준 해상도를 기억)
// 소리: 창이 포커스를 잃으면 AudioListener.pause (음악 · 효과음 모두), 돌아오면 다시
public class UiScaler : MonoBehaviour
{
    static UiScaler instance;
    static readonly Dictionary<CanvasScaler, Vector2> baseRes = new Dictionary<CanvasScaler, Vector2>();
    float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        GameObject go = new GameObject("UiScaler");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<UiScaler>();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => ApplyAll();
        ApplyAll();
    }

    // 1.1.9: 자주 바뀌는 HUD 조각을 하위 캔버스로 떼어 냄 → 그 조각이 바뀌어도 HUD 전체(루트 캔버스)를 다시 묶지 않음
    // 순서 · 크기 · 겉모습은 그대로 (overrideSorting 끔 = 부모 순서대로 그림). 클릭 · 툴팁이 계속 되도록 GraphicRaycaster 도 붙임
    public static void Isolate(GameObject g)
    {
        if (g == null || g.GetComponent<Canvas>() != null) return;
        if (g.GetComponentInParent<Canvas>(true) == null) return;          // 캔버스 밖이면 건드리지 않음
        g.AddComponent<Canvas>();
        g.AddComponent<GraphicRaycaster>();
    }

    // 실행 중에 만든 캔버스를 바로 맞춤 (다음 검사까지 크기가 튀지 않게)
    public static void Fit(CanvasScaler cs)
    {
        if (cs == null) return;
        baseRes[cs] = cs.referenceResolution;
        cs.referenceResolution = cs.referenceResolution / Mathf.Max(0.5f, GameSettings.UiScale);
    }

    public static void ApplyAll()
    {
        float scale = Mathf.Max(0.5f, GameSettings.UiScale);
        List<CanvasScaler> dead = null;
        foreach (KeyValuePair<CanvasScaler, Vector2> kv in baseRes)
            if (kv.Key == null) (dead ??= new List<CanvasScaler>()).Add(kv.Key);
        if (dead != null) foreach (CanvasScaler d in dead) baseRes.Remove(d);

        foreach (CanvasScaler cs in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cs.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
            if (!baseRes.TryGetValue(cs, out Vector2 b)) { b = cs.referenceResolution; baseRes[cs] = b; }
            Vector2 want = b / scale;
            if (cs.referenceResolution != want) cs.referenceResolution = want;      // 1.1.9: 같으면 쓰지 않음
        }
    }

    void Update()
    {
        // 창 · HUD 를 실행 중에 새로 만들므로 가끔 다시 찾음 (자주 하지 않음)
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 1f;
        // 1.1.9: UI 크기가 기본(1배)이면 새 캔버스도 고칠 게 없으므로 씬 전체 검색을 건너뜀 (적 수백 마리일 때 1초마다 끊김)
        // 크기를 바꾸면 GameSettings 가 ApplyAll 을 바로 부르고, 그 뒤로는 다시 1초마다 찾음
        if (Mathf.Approximately(Mathf.Max(0.5f, GameSettings.UiScale), 1f)) return;
        ApplyAll();
    }

    void OnApplicationFocus(bool focus)
    {
        AudioListener.pause = !focus && GameSettings.MuteUnfocused;
    }
}
