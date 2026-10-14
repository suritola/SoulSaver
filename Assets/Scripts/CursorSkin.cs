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

    void Update()
    {
        if (!GameSettings.GameCursor)
        {
            if (shown != null) { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); shown = null; }
            return;
        }
        // 전투 중(게임 씬에서 시간이 흐를 때)만 조준점, 레벨업 · 영혼 트리 · 일시정지 · 메뉴는 화살표
        bool aim = Time.timeScale > 0f && SceneManager.GetActiveScene().name == "GameScene";
        string name = skinId + (aim ? "_aim" : "_arrow");
        if (animated) name += "_" + (int)(Time.unscaledTime * AnimFps) % AnimFrames;
        if (name == shown) return;

        Texture2D tex = Load(name) ?? Load(Default + (aim ? "_aim" : "_arrow"));
        if (tex == null) return;
        // 1.0.4~ 32×32 이하 그림은 하드웨어 커서(마우스를 늦게 따라오지 않음), 그보다 크면 크기 제한이 없는 소프트웨어 커서
        Cursor.SetCursor(tex, aim ? (tex.width == 48 ? AimHot : new Vector2(tex.width * 0.5f, tex.height * 0.5f)) : ArrowHot, tex.width <= 32 && tex.height <= 32 ? CursorMode.Auto : CursorMode.ForceSoftware);
        shown = name;
    }
}
