using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 설정 (2.1.9~): UI 크기 · 창이 비활성일 때 소리 끄기
// UI 크기: 화면 크기에 맞춰 늘어나는 모든 캔버스(CanvasScaler · ScaleWithScreenSize)의 기준 해상도를 크기로 나눔 (작게 나누면 글자 · 버튼이 커짐)
//   씬 캔버스: 씬을 불러올 때 맞춤 · 실행 중에 만든 캔버스(CanvasScaler 를 단 것): 만드는 곳에서 Fit 을 부름 (처음 본 기준 해상도를 기억)
// 소리: 창이 포커스를 잃으면 AudioListener.pause (음악 · 효과음 모두), 돌아오면 다시
public class UiScaler : MonoBehaviour
{
    static UiScaler instance;
    static readonly Dictionary<CanvasScaler, Vector2> baseRes = new Dictionary<CanvasScaler, Vector2>();

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
    // clickable=false: 클릭 · 툴팁이 없는 조각(게이지 · 체력바 · 알림 · 번쩍임)은 GraphicRaycaster 를 붙이지 않음 (클릭마다 도는 검사 줄임)
    public static void Isolate(GameObject g, bool clickable = true)
    {
        if (g == null || g.GetComponent<Canvas>() != null) return;
        if (g.GetComponentInParent<Canvas>(true) == null) return;          // 캔버스 밖이면 건드리지 않음
        g.AddComponent<Canvas>();
        if (clickable || AnyRaycastTarget(g)) g.AddComponent<GraphicRaycaster>();
    }

    // raycastTarget 이 켜진 그림이 하나라도 있으면 레이캐스터를 둠 (마우스가 그 위에 있을 때 사격이 막히는 지금 동작을 그대로)
    static readonly List<Graphic> graphicBuf = new List<Graphic>();
    static bool AnyRaycastTarget(GameObject g)
    {
        g.GetComponentsInChildren(true, graphicBuf);
        bool any = false;
        for (int i = 0; i < graphicBuf.Count; i++) if (graphicBuf[i].raycastTarget) { any = true; break; }
        graphicBuf.Clear();
        return any;
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

    // 최적화: 1초마다 씬 전체를 뒤지던 검색을 없앰 — 씬 로드 때 ApplyAll, 실행 중에 만든 캔버스는 만드는 곳이 Fit 을 부름
    //   (BossUltimate · StoryDirector · FpsCounter · TrailerDirector), UI 크기를 바꾸면 GameSettings 가 ApplyAll

    void OnApplicationFocus(bool focus)
    {
        AudioListener.pause = !focus && GameSettings.MuteUnfocused;
    }
}
