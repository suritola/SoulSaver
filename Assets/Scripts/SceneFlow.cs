using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 1.1.9~ 씬 전환 공용 로딩 화면 (편의성 2차 1)
// 예전엔 모든 씬 전환이 동기 LoadScene 이라 「게임 시작」을 누르면 메뉴가 굳은 채 1~3초 아무 반응이 없었음
// → 검은 막을 내리고, 비동기로 불러오며 0.4초 넘게 걸리면 「불러오는 중」 + 팁 한 줄 + (게임 씬이면) 영웅 초상
// 떠나는 씬은 timeScale 0 으로 얼림 (막이 내리는 동안 적이 움직이거나 플레이어가 맞지 않게) · 새 씬은 늘 timeScale 1 로 시작
public class SceneFlow : MonoBehaviour
{
    static SceneFlow instance;
    static bool loading;
    public static bool Loading => loading;

    Canvas canvas;
    Image curtain;
    RectTransform loadingBox;
    TMP_Text title, tip;
    Image portrait;
    float dotsAt;

    // 모든 LoadScene 을 이것으로
    public static void Go(string scene)
    {
        GameInput.ResetAttackLock();        // 공격 고정 · 필살기 토글은 씬이 바뀌면 풀림 (새 판이 저절로 쏘며 시작하지 않게)
        GameInput.ResetUltLatch();
        if (GameInput.Auto || Application.isBatchMode || GameInput.TrailerRunning) { Time.timeScale = 1f; SceneManager.LoadScene(scene); return; }   // 테스트 · 트레일러는 즉시
        if (loading)
        {
            // 새 씬이 켜진 뒤 그 씬이 곧바로 다른 씬으로 보내는 경우(튜토리얼 「바로 첫 판 시작」 → 메인 메뉴 → 게임 시작)만 이어서,
            // 그 전(막이 내리는 중 · 불러오는 중)의 두 번 누름 · 겹친 호출은 무시
            if (activated) queued = scene;
            return;
        }
        Ensure().StartCoroutine(instance.Run(scene));
    }

    static bool activated;
    static string queued;

    static SceneFlow Ensure()
    {
        if (instance != null) return instance;
        GameObject go = new GameObject("SceneFlow", typeof(RectTransform));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneFlow>();
        instance.Build();
        return instance;
    }

    void Build()
    {
        UIKit.EnsureStyle();
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;                         // 이야기 연출 캔버스(31000) · 툴팁(1000) 보다 위
        CanvasScaler cs = gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        UiScaler.Fit(cs);
        gameObject.AddComponent<GraphicRaycaster>();         // 막이 내린 동안 아래 버튼을 다시 누르지 못하게

        RectTransform cr = UIKit.Rect("Curtain", transform, Vector2.zero, Vector2.zero);
        cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one;
        cr.offsetMin = cr.offsetMax = Vector2.zero;
        curtain = cr.gameObject.AddComponent<Image>();
        curtain.color = new Color(0f, 0f, 0f, 0f);
        curtain.raycastTarget = true;

        loadingBox = UIKit.Rect("Loading", transform, Vector2.zero, new Vector2(1920f, 1080f));
        loadingBox.anchorMin = Vector2.zero; loadingBox.anchorMax = Vector2.one;
        loadingBox.offsetMin = loadingBox.offsetMax = Vector2.zero;
        title = UIKit.Text(loadingBox, "", 36f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 30f), new Vector2(600f, 54f));
        UIKit.Forget(title);
        tip = UIKit.Text(loadingBox, "", 24f, new Color(0.92f, 0.88f, 0.8f), new Vector2(0f, -30f), new Vector2(1200f, 70f));
        tip.fontSizeMin = 16f;
        UIKit.Forget(tip);
        RectTransform pr = UIKit.Rect("Portrait", loadingBox, Vector2.zero, new Vector2(120f, 120f));
        pr.anchorMin = pr.anchorMax = pr.pivot = Vector2.zero;
        pr.anchoredPosition = new Vector2(40f, 40f);
        portrait = pr.gameObject.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        loadingBox.gameObject.SetActive(false);
        canvas.enabled = false;
    }

    IEnumerator Run(string scene)
    {
        loading = true;
        activated = false;
        queued = null;
        canvas.enabled = true;
        Time.timeScale = 0f;                                 // 떠나는 씬을 얼림 (일시정지 · 엔딩에서 왔어도 막이 내리는 동안 게임이 풀리지 않게)
        Application.backgroundLoadingPriority = ThreadPriority.High;
        yield return Fade(0f, 1f, 0.25f);
        while (scene != null)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(scene);
            if (op == null)
            {
                Debug.LogWarning("[SceneFlow] 씬 없음: " + scene);
                Time.timeScale = 1f;
                break;
            }
            op.allowSceneActivation = false;
            float t0 = Time.unscaledTime;
            bool shown = false;
            while (op.progress < 0.9f)
            {
                if (!shown && Time.unscaledTime - t0 > 0.4f) { ShowLoading(scene); shown = true; }     // 0.4초 넘게 걸릴 때만 글자 (짧으면 검은 막만)
                if (shown) Dots();
                yield return null;
            }
            Time.timeScale = 1f;                             // 새 씬은 늘 1 로 시작 (호출처들이 각자 1 로 두던 것을 한곳에)
            activated = true;                                // 새 씬의 sceneLoaded · Awake · Start 가 부르는 Go 는 이어서 받음
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            yield return null;                               // 새 씬 Awake · sceneLoaded 핸들러(MenuExtras.Install 등) 끝나기
            activated = false;
            scene = queued;                                  // 새 씬이 바로 다른 씬으로 보냈으면 막을 걷지 않고 이어서
            queued = null;
            if (scene != null) Time.timeScale = 0f;
        }
        loadingBox.gameObject.SetActive(false);
        Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;
        yield return Fade(1f, 0f, 0.3f);
        canvas.enabled = false;
        loading = false;
    }

    IEnumerator Fade(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            curtain.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        curtain.color = new Color(0f, 0f, 0f, to);
        curtain.raycastTarget = to > 0.01f;
    }

    void ShowLoading(string scene)
    {
        loadingBox.gameObject.SetActive(true);
        dotsAt = Time.unscaledTime;
        Dots();
        string[] pool = TipPool();
        tip.text = pool.Length > 0 ? Loc.T(pool[Random.Range(0, pool.Length)]) : "";
        bool game = scene == "GameScene";
        Sprite s = game ? CharacterUI.Portrait(CharacterData.Selected) : null;
        portrait.sprite = s;
        portrait.enabled = s != null;
    }

    // 「불러오는 중」 + 점 세 개가 0.4초마다 하나씩
    void Dots()
    {
        int n = Mathf.FloorToInt((Time.unscaledTime - dotsAt) / 0.4f) % 4;
        title.text = Loc.T("불러오는 중") + new string('.', n) + "<alpha=#00>" + new string('.', 3 - n);     // 글자 폭이 흔들리지 않게 남은 점은 투명
    }

    static readonly string[] ExtraTips =
    {
        "레벨업 카드는 모아 두고 전투가 잠잠할 때 한꺼번에 골라도 됩니다.",
        "일시정지 메뉴에서 이번 판의 빌드와 능력치를 볼 수 있습니다.",
        "떠돌이 상점을 놓치면 다음 상점이 더 빨리 옵니다.",
        "보스 체력이 절반 아래로 내려가면 특수 스킬을 씁니다. 경고 표시를 잘 보세요.",
    };

    static string[] TipPool()
    {
        System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>(CodexUI.LoadingTips());
        list.AddRange(ExtraTips);
        return list.ToArray();
    }
}
