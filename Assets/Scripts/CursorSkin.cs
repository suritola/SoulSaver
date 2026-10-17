using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 전용 마우스 커서 (1.8.7~): 메뉴 · 멈춘 화면은 화살표, 전투 중에는 조준점
// 설정 "게임 커서"로 끄면 윈도우 기본 포인터. 모양 · 색은 스킨 상점의 커서 스킨 (Resources/Cursors, tools/pixelart/make_cursors.py)
// 48x48 로 크게 그리므로 하드웨어 커서 크기 제한이 없는 소프트웨어 커서로 표시
public class CursorSkin : MonoBehaviour
{
    const string Default = "cursor_default";
    static readonly Vector2 ArrowHot = new Vector2(3f, 3f), AimHot = new Vector2(24f, 24f);
    const int AnimFrames = 4;          // 전설 스킨 반짝임
    const float AnimFps = 6f;

    static CursorSkin instance;
    static string skinId = Default;
    static bool animated;
    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
    string shown;                      // 지금 쓰는 텍스처 이름 (바뀔 때만 SetCursor)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (Application.isBatchMode || instance != null) return;
        GameObject go = new GameObject("CursorSkin");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<CursorSkin>();
        Refresh();
    }

    // 설정 · 스킨이 바뀌었을 때
    public static void Refresh()
    {
        SkinDef s = SkinData.Equipped(SkinKind.Cursor);
        skinId = s != null ? s.id : Default;
        animated = Load(skinId + "_aim_0") != null;
        BuildNames();
        if (instance != null) instance.shown = null;
        if (!GameSettings.GameCursor) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    static Texture2D Load(string name)
    {
        if (!cache.TryGetValue(name, out Texture2D t))
        {
            t = Resources.Load<Texture2D>("Cursors/" + name);
            cache[name] = t;
        }
        return t;
    }

    // 최적화: 상태(조준 · 반짝임 프레임)별 텍스처 이름을 스킨이 바뀔 때 미리 만들어 둠 (예전엔 매 프레임 문자열 2~4개)
    //   [0] 화살표 · [1] 조준점, 각각 [0..AnimFrames-1] 반짝임 프레임 · [AnimFrames] 반짝임 없음
    static string[][] names;
    static string namesFor;
    static readonly string[] fallback = { Default + "_arrow", Default + "_aim" };

    static void BuildNames()
    {
        names = new string[2][];
        for (int a = 0; a < 2; a++)
        {
            string head = skinId + (a == 1 ? "_aim" : "_arrow");
            names[a] = new string[AnimFrames + 1];
            for (int f = 0; f < AnimFrames; f++) names[a][f] = head + "_" + f;
            names[a][AnimFrames] = head;
        }
        namesFor = skinId;
    }

    static string NameFor(bool aim)
    {
        if (names == null || namesFor != skinId) BuildNames();
        int f = animated ? (int)(Time.unscaledTime * AnimFps) % AnimFrames : AnimFrames;
        return names[aim ? 1 : 0][f];
    }

    // 1.0.5~ 패드 가상 커서 · 조준점 그림 (GamePad.OnGUI 가 직접 그림 — 게임 커서를 꺼 둬도 패드 커서는 이 모양)
    public static Texture2D PadTexture(bool aim, out Vector2 hot)
    {
        string name = NameFor(aim);
        Texture2D tex = Load(name) ?? Load(fallback[aim ? 1 : 0]);
        hot = tex == null ? Vector2.zero : aim ? (tex.width == 48 ? AimHot : new Vector2(tex.width * 0.5f, tex.height * 0.5f)) : ArrowHot;
        return tex;
    }

    // 1.1.9~ 설정 「커서를 창 안에 가두기」: 전투 중(시간이 흐르는 게임 씬)에만 Confined — 창 밖 클릭으로 포커스를 잃어 갑자기 멈추거나 옆 모니터로 넘어가지 않게
    // 멈춘 화면 · 메뉴 · 연출 · 패드 사용 중 · 창이 비활성이면 풀어 둠 (macOS 는 Confined 를 지원하지 않아 None 과 같음)
    static CursorLockMode applied = CursorLockMode.None;

    static void UpdateConfine()
    {
        bool want = GameSettings.ConfineCursor && Application.isFocused && Time.timeScale > 0f
                    && SceneManager.GetActiveScene().name == "GameScene"
                    && !ESCmenu.IsOpen && !SettingsUI.IsOpen && !GamePad.Active && !StoryDirector.Playing && !BossUltimate.CutscenePlaying;
        CursorLockMode m = want ? CursorLockMode.Confined : CursorLockMode.None;
        if (m == applied) return;
        applied = m;
        Cursor.lockState = m;
    }

    void OnApplicationFocus(bool focus)
    {
        if (focus || applied == CursorLockMode.None) return;
        applied = CursorLockMode.None;          // 알트탭 뒤 커서가 자유롭게
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        UpdateConfine();
        if (!GameSettings.GameCursor)
        {
            if (shown != null) { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); shown = null; }
            return;
        }
        // 전투 중(게임 씬에서 시간이 흐를 때)만 조준점, 레벨업 · 영혼 트리 · 일시정지 · 메뉴는 화살표
        bool aim = Time.timeScale > 0f && InGameScene();
        string name = NameFor(aim);
        if (ReferenceEquals(name, shown)) return;

        Texture2D tex = Load(name) ?? Load(fallback[aim ? 1 : 0]);
        if (tex == null) return;
        // 1.0.4~ 32×32 이하 그림은 하드웨어 커서(마우스를 늦게 따라오지 않음), 그보다 크면 크기 제한이 없는 소프트웨어 커서
        Cursor.SetCursor(tex, aim ? (tex.width == 48 ? AimHot : new Vector2(tex.width * 0.5f, tex.height * 0.5f)) : ArrowHot, tex.width <= 32 && tex.height <= 32 ? CursorMode.Auto : CursorMode.ForceSoftware);
        shown = name;
    }

    // 최적화: 씬 이름 글자(매 호출 새 문자열)를 씬이 바뀔 때만 읽음
    int sceneHandle = -1;
    bool sceneIsGame;
    bool InGameScene()
    {
        Scene s = SceneManager.GetActiveScene();
        if (s.handle != sceneHandle) { sceneHandle = s.handle; sceneIsGame = s.name == "GameScene"; }
        return sceneIsGame;
    }
}
