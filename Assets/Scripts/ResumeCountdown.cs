using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 2.0.1~ 사용자 「떠돌이 상점을 닫으면 1초 멈추던 것을 게임 안 모든 창에 · 3초로 · [Space] 즉시 시작」
//   창마다 고치지 않고 한곳에서: 게임 장면에서 시간이 0.2초 넘게 멈췄다가(창) 다시 흐르면 곧바로 다시 멈추고 3초를 셈
//   보스 컷신 · 이야기 연출 · 장면 전환 · 쓰러짐 뒤에는 세지 않음 (창이 아님) · 세는 중 ESC · 영혼 트리 등 창이 열리면 그만두고, 그 창이 닫히면 새로 3초
//   [Space] (패드 A) 를 누르면 다음 프레임에 바로 흐름 (같은 프레임에 Space 상호작용이 겹치지 않게)
public class ResumeCountdown : MonoBehaviour
{
    public const float Seconds = 3f;
    const float MinPause = 0.2f;        // 이보다 짧은 멈춤은 창이 아님 (손맛 멈춤은 0.02배라 0 이 아님)

    public static bool Active => inst != null && inst.counting;
    static ResumeCountdown inst;

    float zeroSince = -1f;              // 시간이 0 이 된 실제 시각 (-1 = 흐르는 중)
    bool cinematicDuringPause;          // 멈춘 동안 컷신 · 연출이 있었나
    bool counting, skipNext;
    float endAt;
    GameObject ui;
    TMP_Text num, hint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) => Attach(s);
        Attach(SceneManager.GetActiveScene());
    }

    static void Attach(Scene s)
    {
        if (s.name != "GameScene" || inst != null) return;
        GameObject go = new GameObject("ResumeCountdown");
        SceneManager.MoveGameObjectToScene(go, s);      // 장면과 함께 사라짐 (다음 판은 새로)
        inst = go.AddComponent<ResumeCountdown>();
    }

    void OnDestroy() { if (inst == this) inst = null; }

    static bool Skip => GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode;

    static bool Cinematic => BossUltimate.CutscenePlaying || StoryDirector.Playing || Herald.Playing || PlayerController.Cinematic
                             || SceneFlow.Loading || TitleIntro.Showing;

    // 세는 동안 열리면 그 창에 맡기는 것들 (시간이 멈춘 동안에도 키로 열 수 있는 창 + 연출)
    static bool WindowOpen => ESCmenu.IsOpen || SoulTreeUI.IsOpen || HeroGuide.IsOpen || ChoiceUI.Open || SkillEvolutionUI.Open
                              || WeaponEvolutionUI.Open || Cinematic;

    void Update()
    {
        if (Skip) { if (counting) Stop(true); return; }
        PlayerController p = Hostile.Player;
        bool alive = p != null && !p.IsDying;

        if (counting)
        {
            if (!alive) { Stop(false); return; }
            if (WindowOpen) { Stop(false); zeroSince = Time.unscaledTime; cinematicDuringPause = Cinematic; return; }       // 그 창이 닫히면 새로 셈
            if (Time.timeScale != 0f) Time.timeScale = 0f;      // 누가 먼저 풀어도 다 셀 때까지 멈춤
            if (skipNext) { Stop(true); return; }
            if (Input.GetKeyDown(KeyCode.Space) || GamePad.ADown) { skipNext = true; return; }
            float left = endAt - Time.unscaledTime;
            if (left <= 0f) { Stop(true); return; }
            Show(left);
            return;
        }

        if (Time.timeScale == 0f)
        {
            if (zeroSince < 0f) { zeroSince = Time.unscaledTime; cinematicDuringPause = false; }
            if (Cinematic) cinematicDuringPause = true;
            return;
        }

        // 막 흐르기 시작함
        if (zeroSince >= 0f)
        {
            bool wasWindow = Time.unscaledTime - zeroSince >= MinPause && !cinematicDuringPause;
            zeroSince = -1f;
            if (wasWindow && alive && !WindowOpen) Begin();
        }
    }

    void Begin()
    {
        counting = true;
        skipNext = false;
        endAt = Time.unscaledTime + Seconds;
        Time.timeScale = 0f;
        Show(Seconds);
    }

    void Stop(bool resume)
    {
        counting = false;
        skipNext = false;
        if (ui != null) ui.SetActive(false);
        if (resume && Time.timeScale == 0f) Time.timeScale = 1f;
    }

    void Show(float left)
    {
        if (ui == null) Build();
        if (ui == null) return;
        ui.SetActive(true);
        num.text = Mathf.CeilToInt(left).ToString();
        hint.text = Loc.T("resume.skip").Replace("{0}", GamePad.Active ? "A" : "Space");
    }

    void Build()
    {
        Canvas cv = UIKit.HudCanvas();
        if (cv == null) return;
        RectTransform root = UIKit.Rect("ResumeCountdownUI", cv.transform, new Vector2(0f, 60f), new Vector2(900f, 260f));
        ui = root.gameObject;
        ui.transform.SetAsLastSibling();
        num = UIKit.Text(root, "", 120f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 40f), new Vector2(300f, 150f));
        hint = UIKit.Text(root, "", 30f, Color.white, new Vector2(0f, -70f), new Vector2(880f, 50f));
        foreach (TMP_Text t in new[] { num, hint })
        {
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(10, 6, 16, 255);
        }
    }
}

public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddResumeCountdown()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        C("resume.skip", "[{0}] 즉시 시작", "[{0}] Start now", "[{0}] すぐに開始", "[{0}] 立即开始");
    }
}
