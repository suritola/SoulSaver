using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 2.0.1~ 사용자 「떠돌이 상점을 닫으면 1초 멈추던 것을 게임 안 모든 창에 · 3초로 · [Space] 즉시 시작」
//   창마다 고치지 않고 한곳에서: 게임 장면에서 시간이 0.2초 넘게 멈췄다가(창) 다시 흐르면 3프레임 지켜보고(다음 창이 이어 열리면 세지 않음) 다시 멈추고 3초를 셈
//   보스 컷신 · 이야기 연출 · 스킬 진화 연출 · 장면 전환 · 쓰러짐 뒤에는 세지 않음 (창이 아님)
//   [Space] (패드 A) 를 누르면 다음 프레임에 바로 흐름 (같은 프레임에 Space 상호작용이 겹치지 않게)
// 1.2.7 사고(끊긴 상태가 남아 판이 멈춤) 재발 방지 — 독립 검토 반영:
//   · 세다가 창 · 연출이 끼어들어 손을 떼면 owed: 그 창이 우리 0 을 기억했다가 0 으로 되돌려도, 창이 다 닫히면 반드시 다시 세고 1 로 풂
//   · 안전장치: 열린 창 · 연출이 없는데 20초 넘게 0 이면 무조건 셈 (어떤 길로든 영영 멈추지 않게)
public class ResumeCountdown : MonoBehaviour
{
    public const float Seconds = 3f;
    const float MinPause = 0.2f;        // 이보다 짧은 멈춤은 창이 아님 (손맛 멈춤은 0.02배라 0 이 아님)
    const int WatchFrames = 3;          // 다시 흐른 뒤 이만큼 지켜봄 (레벨업 연쇄 · 보스 보상 → 진화처럼 다음 창이 곧바로 열리면 세지 않음)
    const float StuckLimit = 20f;       // 창 · 연출 없이 이만큼 0 이면 안전장치

    public static bool Active => inst != null && inst.counting;
    static ResumeCountdown inst;

    float zeroSince = -1f;              // 시간이 0 이 된 실제 시각 (-1 = 흐르는 중)
    bool cinematicDuringPause;          // 멈춘 동안 컷신 · 연출이 있었나
    int watching = -1;                  // 다시 흐른 뒤 지켜본 프레임 (-1 = 지켜보지 않음)
    bool owed;                          // 세다가 끼어든 창에 손을 뗐음 → 창이 닫히면 반드시 다시 셈
    bool counting, skipNext;
    float endAt;
    GameObject ui;
    TMP_Text num, hint;
    LevelShop levelShop;
    Shop stall;
    StageManager stage;

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

    public static bool InCapture;       // 녹화 도구(tools/trailer · 배치 모드)가 카운트다운 화면을 찍을 때만 켬
    static bool Skip => GameInput.Auto || GameInput.TrailerRunning || (Application.isBatchMode && !InCapture);

    static bool Cinematic => BossUltimate.CutscenePlaying || StoryDirector.Playing || Herald.Playing || PlayerController.Cinematic
                             || SceneFlow.Loading || TitleIntro.Showing || SkillEvolutionUI.Open;

    // 시간을 멈춰 둘 창 · 연출이 열려 있나 (ESCmenu.SomethingHoldsPause 와 같은 목록 + 키로 여는 창 + 모달 수)
    //   holding = 세는 중 · 멈춤 판단용: 「진화 창이 뜨기 전 시간이 흐르길 기다리는 evolving」은 창이 아님
    //     (재검토 H1: 세기 시작한 프레임에 보스가 죽어 evolving 이 켜지면, 우리는 창이라 손을 떼고 진화는 시간이 흐르길 기다려 서로 기다리며 멈췄음)
    bool WindowOpen(bool holding)
    {
        if (levelShop == null) levelShop = FindFirstObjectByType<LevelShop>();
        if (stall == null) stall = FindFirstObjectByType<Shop>();
        if (stage == null) stage = FindFirstObjectByType<StageManager>();
        bool stageMenu = stage != null && stage.IsMenuOpen && !(holding && stage.IsEvolving);
        return ModalMark.Count > 0 || ESCmenu.IsOpen || SoulTreeUI.IsOpen || HeroGuide.IsOpen || ChoiceUI.Open || WeaponEvolutionUI.Open || Demo.EndOpen
               || (levelShop != null && levelShop.IsOpen) || (stall != null && stall.isShopOpen) || stageMenu
               || Cinematic;
    }

    void Update()
    {
        if (Skip) { if (counting) Stop(true); watching = -1; owed = false; return; }
        PlayerController p = Hostile.Player;
        bool alive = p != null && !p.IsDying;
        if (!alive)
        {
            // 쓰러짐 연출은 실제 시간으로 흐름 — 우리가 건 0 이 남지 않게
            if (counting) Stop(Time.timeScale == 0f);
            watching = -1; owed = false; zeroSince = -1f;
            return;
        }

        if (counting)
        {
            if (WindowOpen(true)) { Stop(false); owed = true; zeroSince = Time.unscaledTime; cinematicDuringPause = Cinematic; return; }       // 그 창에 맡기고 닫히면 반드시 다시 셈
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
            watching = -1;
            if (zeroSince < 0f) { zeroSince = Time.unscaledTime; cinematicDuringPause = false; }
            bool open = WindowOpen(true);
            if (Cinematic) cinematicDuringPause = true;
            float stopped = Time.unscaledTime - zeroSince;
            // 끼어든 창이 우리 0 을 그대로 되돌렸거나(owed) · 아무것도 잡고 있지 않은데 오래 멈춤(안전장치) → 우리가 세고 1 로 풂
            if (!open && ((owed && stopped >= MinPause) || stopped >= StuckLimit))
            {
                if (!owed) Debug.LogWarning("[ResumeCountdown] 창 · 연출 없이 " + stopped.ToString("0") + "초 멈춰 있어 다시 흐르게 함");
                Begin();
            }
            return;
        }

        // 막 흐르기 시작함 → 몇 프레임 지켜봄
        if (zeroSince >= 0f)
        {
            bool wasWindow = Time.unscaledTime - zeroSince >= MinPause && !cinematicDuringPause;
            zeroSince = -1f;
            watching = wasWindow ? 0 : -1;
            owed = false;
        }
        if (watching >= 0)
        {
            // 보스 보상 연쇄(진화 → 보상 → 각인) 사이: 다음 창을 기다리며 셈을 미룸 (재검토 M1 · 남아도 멈추지 않음)
            if (stage != null && stage.RewardChain) { watching = 0; return; }
            // 첫 프레임은 건너뜀: 창을 닫은 프레임의 Destroy 는 프레임 끝에 처리돼 모달 수가 아직 남아 있음 (재검토 M2)
            if (watching > 0 && WindowOpen(false)) { watching = -1; return; }        // 다음 창이 열림 → 그 창이 닫힐 때 다시
            if (++watching >= WatchFrames) { watching = -1; Begin(); }
        }
    }

    void Begin()
    {
        counting = true;
        owed = false;
        skipNext = false;
        zeroSince = -1f;
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
        if (!ui.activeSelf) { ui.SetActive(true); ui.transform.SetAsLastSibling(); }      // 나중에 붙은 알림 · 배너에 가리지 않게
        num.text = Mathf.CeilToInt(left).ToString();
        float f = left - Mathf.Floor(left);         // 초가 바뀔 때 1 → 0: 숫자가 커졌다 줄어듦 (바뀐 걸 눈이 따라가게)
        num.transform.localScale = Vector3.one * (1f + 0.3f * f * f);
        hint.text = Loc.T("resume.skip").Replace("{0}", GamePad.Active ? "A" : "Space");
    }

    void Build()
    {
        Canvas cv = UIKit.HudCanvas();
        if (cv == null) return;
        // 2.0.3~ 사용자 「숫자랑 안내가 한 개도 안 보여」: 화면 가운데(영웅 · 피해 숫자 · 적 위)에 작게 겹쳐 묻혔음
        //   → 화면 전체를 옅게 어둡게 · 숫자를 크게 영웅 머리 위로 · 안내는 그 아래 (영웅을 가리지 않게)
        RectTransform root = UIKit.Rect("ResumeCountdownUI", cv.transform, Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        UnityEngine.UI.Image dim = root.gameObject.AddComponent<UnityEngine.UI.Image>();
        dim.color = new Color(0.03f, 0.02f, 0.05f, 0.5f);
        dim.raycastTarget = false;
        ui = root.gameObject;
        // 2.0.3~ 숫자 · 안내를 한 판 위에 (사용자 「둘 다 배경 넣어서 더 잘 보이게」) — 창 틀 그림(금 테 · 어두운 돌), 없으면 어두운 판 · 영웅 머리 위
        RectTransform panel = UIKit.Rect("Panel", root, new Vector2(0f, 235f), new Vector2(600f, 340f));
        UnityEngine.UI.Image pi = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
        if (UIKit.PanelSprite != null) { pi.sprite = UIKit.PanelSprite; pi.type = UnityEngine.UI.Image.Type.Sliced; pi.color = Color.white; }
        else pi.color = new Color(0.05f, 0.03f, 0.08f, 0.92f);
        pi.raycastTarget = false;
        num = UIKit.Text(panel, "", 200f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 40f), new Vector2(400f, 230f));
        hint = UIKit.Text(panel, "", 38f, Color.white, new Vector2(0f, -115f), new Vector2(560f, 60f));
        foreach (TMP_Text t in new[] { num, hint })
        {
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(10, 6, 16, 255);
        }
        ui.SetActive(false);
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
