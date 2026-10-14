using UnityEngine;

// 마우스 · 이동 입력을 한곳에서 읽음
// 평소에는 Unity Input 그대로, 트레일러 촬영(Auto)일 때는 TrailerDirector 가 넣어 주는 값을 씀
public static class GameInput
{
    public static bool Auto;                // 자동 조종 중

    // 트레일러 촬영 중인지 (TrailerDirector는 에디터 전용이라 출시 빌드에서는 항상 false)
#if UNITY_EDITOR
    public static bool TrailerRunning => TrailerDirector.Running;
#else
    public static bool TrailerRunning => false;
#endif

    // 자동 조종 값 (TrailerDirector 가 채움)
    public static Vector2 AutoMove;         // 이동 방향 (-1 ~ 1)
    public static Vector3 AutoAim;          // 조준할 월드 좌표
    public static bool AutoFire;            // 좌클릭 누르고 있음
    public static bool AutoUlt;             // 우클릭 누르고 있음
    // 눌렀다 / 뗐다 신호는 "이 프레임에 일어남"으로 표시 (다른 스크립트의 Update 순서와 상관없게)
    public static int FireDownFrame = -1, FireUpFrame = -1, UltDownFrame = -1, UltUpFrame = -1;

    // 1.0.5~ 패드로 싸울 때는 오른쪽 스틱이 정한 플레이어 둘레의 가상 조준점 (GamePad)
    public static Vector3 MousePosition
    {
        get
        {
            if (!Auto)
            {
                if (GamePad.Aiming)
                {
                    Camera main = Camera.main;
                    if (main != null) return main.WorldToScreenPoint(GamePad.AimWorld);
                }
                return Input.mousePosition;
            }
            Camera cam = Camera.main;
            return cam != null ? cam.WorldToScreenPoint(AutoAim) : Vector3.zero;
        }
    }

    // 1.0.4~ 공격 · 필살기 키는 설정 → 조작에서 바꿀 수 있음 (기본 좌 · 우클릭, KeyBindings)
    public static bool FireDown => Auto ? Time.frameCount == FireDownFrame : KeyBindings.Down(GameAction.Attack);
    public static bool FireHeld => Auto ? AutoFire : KeyBindings.Held(GameAction.Attack);
    public static bool FireUp => Auto ? Time.frameCount == FireUpFrame : KeyBindings.Up(GameAction.Attack);

    public static bool UltDown => Auto ? Time.frameCount == UltDownFrame : KeyBindings.Down(GameAction.Ultimate);
    public static bool UltHeld => Auto ? AutoUlt : KeyBindings.Held(GameAction.Ultimate);
    public static bool UltUp => Auto ? Time.frameCount == UltUpFrame : KeyBindings.Up(GameAction.Ultimate);

    // 1.0.5~ UI 를 가리키는 자리: 패드로 메뉴를 다루면 가상 커서, 아니면 마우스 (툴팁 · 영혼 트리 확대 기준)
    public static Vector3 PointerPosition => GamePad.InMenu ? (Vector3)GamePad.CursorPosition : Input.mousePosition;

    // 1.0.5~ ESC 와 같은 취소 · 뒤로: ESC 키 · 패드 Menu(어디서나) · 패드 B(전투가 아닐 때)
    public static bool EscDown => Input.GetKeyDown(KeyCode.Escape) || GamePad.CancelDown;
}
