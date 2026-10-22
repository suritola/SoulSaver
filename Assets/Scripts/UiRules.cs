// UI 전면 개편 (docs/art/UI-개편-기획서.md · 8절 비판 검토가 기준) 의 되돌리기 스위치 · 공통 규칙 — 2026-10-22~
//   VisualRules.Legacy 와 같은 방식: Legacy = true 면 개편 전 화면 그대로 (화면 단위로 새 모습을 붙여 가다가 승인 ③ 뒤 기본값을 false 로)
using UnityEngine;

public static class UiRules
{
    // 기본 모습: 이 한 줄이 출시 기본값 (승인 ③ 에서 false) — 실행할 때 Init 이 이 값에서 시작해 아래 스위치로만 바뀜 (검토: 필드만 바꾸면 Init 이 덮어썼음)
    public const bool DefaultLegacy = true;
    public static bool Legacy = DefaultLegacy;

    // 스위치 (판마다가 아니라 실행할 때 한 번):
    //   새 모습 강제: 실행 인자 -uiv2 · 에디터 메뉴 「Soul Saver/UI v2 미리보기」(EditorPrefs) · UserSettings/uiv2.on
    //   옛 모습 강제: 실행 인자 -uilegacy · UserSettings/uilegacy.on (옛 모습이 우선 — 문제가 생겼을 때 되돌리기용)
    //   배치 실행(자동 봇 · 측정)은 에디터 설정을 보지 않고 실행 인자 · 기본값만
    public const string EditorPrefKey = "SoulSaver.UiV2";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        bool v2 = System.Array.IndexOf(args, "-uiv2") >= 0;
        bool legacy = System.Array.IndexOf(args, "-uilegacy") >= 0;
#if UNITY_EDITOR
        if (!Application.isBatchMode)
        {
            v2 |= UnityEditor.EditorPrefs.GetBool(EditorPrefKey, false) || System.IO.File.Exists("UserSettings/uiv2.on");
            legacy |= System.IO.File.Exists("UserSettings/uilegacy.on");
        }
#endif
        Legacy = legacy || (DefaultLegacy && !v2);
        sprites.Clear();
    }

    // v2 그림 (Assets/Resources/UI/v2 · tools/pixelart/make_ui_v2.py) — 없으면 null (부르는 쪽이 옛 모습으로)
    static readonly System.Collections.Generic.Dictionary<string, Sprite> sprites = new System.Collections.Generic.Dictionary<string, Sprite>();
    public static Sprite Sprite(string name)
    {
        if (!sprites.TryGetValue(name, out Sprite s) || s == null) sprites[name] = s = Resources.Load<Sprite>("UI/v2/" + name);
        return s;
    }

    // UI 1도트 = 1080p · UI 크기 100% 에서 3px (PPU 33.333 그림 · pixelsPerUnitMultiplier 1) — 월드 격자와는 따로 (카메라 22 에서 월드 1도트 ≈ 3.07px)
    public const float PixelsPerUnit = 100f / 3f;
    public const float Dot = 3f;

    // 안전 상자: 전면 화면은 가로 min(1920, 논리 캔버스 폭) 안에 — UI 크기 110 · 120% 면 UiScaler.Fit 이 기준 해상도를 나눠 논리 폭이 1745 · 1600 으로 줄어듦 (스팀 덱 기본 110%)
    public static float SafeWidth(RectTransform canvasRoot) => canvasRoot == null ? 1920f : Mathf.Min(1920f, canvasRoot.rect.width);
    public static float SafeHeight(RectTransform canvasRoot) => canvasRoot == null ? 1080f : Mathf.Min(1080f, canvasRoot.rect.height);

    // 고르기 창 입력 잠금은 등장 연출 길이와 떼어 둠 — 연출을 짧게 해도 앞 창에서 연타한 클릭이 다음 창 카드를 고르지 않게 (지금 약 0.52초)
    public const float MinInputLock = 0.45f;
    public static float LockUntil(float introEnd) => Mathf.Max(introEnd, Time.unscaledTime + MinInputLock);
}
