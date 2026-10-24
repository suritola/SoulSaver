using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// 마우스 감도 (설정 · 일반): 전투 중에만 커서가 움직인 양에 감도를 곱해 커서를 그 자리로 옮김
// 커서를 잠그지 않으므로 HUD 클릭 · 툴팁 · 커서 스킨은 그대로. 메뉴 · 멈춘 화면은 원래 속도
// 윈도우 빌드에서만 동작 (에디터 · 다른 OS 에서는 효과 없음)
// 읽기 · 옮기기를 모두 윈도우 좌표로 해서 Unity 좌표와의 1픽셀 차이로 커서가 저절로 미끄러지지 않게
public class MouseSensitivity : MonoBehaviour
{
    static MouseSensitivity instance;
    Vector2 pos;            // 감도를 곱해 쌓은 커서 위치 (창 안 좌표, 소수점까지)
    Vector2Int last;        // 지난 프레임에 둔 커서 위치
    bool tracking;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    public static bool Supported => true;

    struct POINT { public int X, Y; }
    struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] static extern System.IntPtr GetActiveWindow();
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern bool ScreenToClient(System.IntPtr hWnd, ref POINT p);
    [DllImport("user32.dll")] static extern bool ClientToScreen(System.IntPtr hWnd, ref POINT p);
    [DllImport("user32.dll")] static extern bool GetClientRect(System.IntPtr hWnd, out RECT r);

    // 창 안 커서 위치와 창 크기 (창을 못 찾으면 false)
    static bool Read(out Vector2Int cursor, out Vector2Int size)
    {
        cursor = size = Vector2Int.zero;
        System.IntPtr hwnd = GetActiveWindow();
        if (hwnd == System.IntPtr.Zero || !GetCursorPos(out POINT p) || !ScreenToClient(hwnd, ref p) || !GetClientRect(hwnd, out RECT r)) return false;
        cursor = new Vector2Int(p.X, p.Y);
        size = new Vector2Int(r.Right - r.Left, r.Bottom - r.Top);
        return size.x > 0 && size.y > 0;
    }

    static void Move(Vector2Int to)
    {
        System.IntPtr hwnd = GetActiveWindow();
        POINT p = new POINT { X = to.x, Y = to.y };
        if (hwnd != System.IntPtr.Zero && ClientToScreen(hwnd, ref p)) SetCursorPos(p.X, p.Y);
    }
#else
    public static bool Supported => false;
    static bool Read(out Vector2Int cursor, out Vector2Int size) { cursor = size = Vector2Int.zero; return false; }
    static void Move(Vector2Int to) { }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Supported || Application.isBatchMode || instance != null) return;
        GameObject go = new GameObject("MouseSensitivity");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MouseSensitivity>();
    }

    // 전투 중(게임 씬에서 시간이 흐르고 연출이 아닐 때)이고 감도가 100%가 아닐 때만
    static bool Active =>
        Mathf.Abs(GameSettings.MouseSensitivity - 1f) > 0.01f && Application.isFocused && !GameInput.Auto
        && Time.timeScale > 0f && !StoryDirector.Playing && CursorSkin.InGameScene();      // 1.5.9~ 씬 이름 글자를 매 프레임 새로 만들지 않게 (씬이 바뀔 때만 읽음)

    void Update()
    {
        if (!Active || !Read(out Vector2Int cursor, out Vector2Int size))
        {
            tracking = false;
            return;
        }
        // 처음이거나 다시 시작할 때는 지금 자리에서 출발
        if (!tracking)
        {
            tracking = true;
            last = cursor;
            pos = cursor;
            return;
        }
        Vector2Int delta = cursor - last;
        if (delta == Vector2Int.zero) return;

        pos += (Vector2)delta * GameSettings.MouseSensitivity;
        pos.x = Mathf.Clamp(pos.x, 0f, size.x - 1);
        pos.y = Mathf.Clamp(pos.y, 0f, size.y - 1);
        Vector2Int target = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        if (target != cursor) Move(target);
        last = target;
    }
}
