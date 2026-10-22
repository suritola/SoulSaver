using UnityEditor;

// UI 개편 새 모습 미리 보기 스위치 (UiRules.Legacy) — 켜 두면 다음 플레이부터 새 모습 (빌드는 실행 인자 -uiv2)
static class UiV2Toggle
{
    const string Menu = "Soul Saver/UI v2 미리보기";

    [MenuItem(Menu)]
    static void Toggle() => EditorPrefs.SetBool(UiRules.EditorPrefKey, !EditorPrefs.GetBool(UiRules.EditorPrefKey, false));

    [MenuItem(Menu, true)]
    static bool Check()
    {
        UnityEditor.Menu.SetChecked(Menu, EditorPrefs.GetBool(UiRules.EditorPrefKey, false));
        return true;
    }
}
