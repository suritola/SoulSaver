using UnityEngine;

// 창 제목을 게임 이름(Soul Saver)으로
// 제품 이름(productName)도 Soul Saver (1.7.3부터, 옛 저장은 SaveMigration 이 옮김)
public static class WindowTitle
{
    public const string GameName = "Soul Saver";

    // 메인 메뉴 오른쪽 아래 "v2.0" 같은 버전 글을 실제 버전으로
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void HookVersion()
    {
        FixVersion();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => FixVersion();
        Loc.Changed += Relabel;         // 1.1~ 언어를 바꾸면 「체험판」도 새 언어로
    }

    // 1.1~ 바꿔 둔 버전 글 (언어가 바뀌면 다시 씀)
    static readonly System.Collections.Generic.List<TMPro.TMP_Text> labels = new System.Collections.Generic.List<TMPro.TMP_Text>();

    static void FixVersion()
    {
        labels.RemoveAll(t => t == null);
        foreach (TMPro.TMP_Text t in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
            if (System.Text.RegularExpressions.Regex.IsMatch(t.text, @"^v\d+(\.\d+)+$") && !labels.Contains(t)) labels.Add(t);
        Relabel();
    }

    static void Relabel()
    {
        string v = "v" + Application.version.TrimStart('v');
        foreach (TMPro.TMP_Text t in labels)
            if (t != null) t.text = v + Demo.Label;      // 체험판이면 "v1.9.2  체험판"
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern bool SetWindowTextW(System.IntPtr hWnd, string text);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern System.IntPtr GetActiveWindow();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Apply()
    {
        Set();
        // 시작할 때 창이 아직 앞에 없을 수 있어 씬이 바뀔 때마다 한 번 더
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => Set();
    }

    static void Set()
    {
        System.IntPtr h = GetActiveWindow();
        if (h != System.IntPtr.Zero) SetWindowTextW(h, Demo.On ? GameName + " Demo" : GameName);
    }
#endif
}
