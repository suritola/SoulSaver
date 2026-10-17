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
    // 1.1.9~ 설정 「공격 고정」이 걸려 있으면 손을 떼도 누르고 있는 것으로 (편의성 2차 10)
    public static bool FireHeld => Auto ? AutoFire : KeyBindings.Held(GameAction.Attack) || AttackLockNow;
    public static bool FireKeyHeld => Auto ? AutoFire : KeyBindings.Held(GameAction.Attack);        // 키만 (툴팁 지연 판정)
    public static bool FireUp => Auto ? Time.frameCount == FireUpFrame : KeyBindings.Up(GameAction.Attack);

    // ================================================================= 1.1.9~ 공격 고정 (접근성)
    // 공격 키를 한 번 누르면 계속 쏘고, 다시 누르면 멈춤 — 쏘기 시작 · 멈춤 · 조준은 모두 사람이 정함 (피해 · 연사는 누르고 있을 때와 같음)
    static bool attackLocked;
    public static bool AttackLocked => attackLocked && GameSettings.AttackLock && !GamePad.Active && !TutorialRun.Active && !Auto;
    // 지금 실제로 쏘는 상태인지: 멈춘 화면 · 이야기 · 필살기 조준 · 쓰러짐 · 궁수 숨 고르기 동안은 걸린 채 쉼
    public static bool AttackLockNow
    {
        get
        {
            if (!AttackLocked || Time.timeScale <= 0f || StoryDirector.Playing) return false;
            PlayerController p = Hostile.Player;
            if (p == null || p.IsSkillUsing || p.IsDying) return false;
            CharacterKit k = CharacterKit.Instance;
            return !(k != null && k.DrawsBow && k.Breathless);
        }
    }
    public static void ResetAttackLock() => attackLocked = false;

    // PlayerController.Update 의 발사 처리 앞에서 한 번: 공격 키를 누를 때마다 걸기 · 풀기 (UI 를 가리키며 누른 것은 무시)
    // 1.2.5~ pointerOverUI 는 공격 키를 누른 프레임에만 부름 (UI 레이캐스트가 매 프레임 돌지 않게 · 넘기는 쪽은 대리자를 캐시)
    static int lockTickFrame = -1;
    public static void TickAttackLock(System.Func<bool> pointerOverUI)
    {
        if (lockTickFrame == Time.frameCount) return;
        lockTickFrame = Time.frameCount;
        if (Auto || !GameSettings.AttackLock || GamePad.Active || TutorialRun.Active) { attackLocked = false; return; }
        if (Time.timeScale <= 0f || !KeyBindings.Down(GameAction.Attack) || (pointerOverUI != null && pointerOverUI())) return;
        attackLocked = !attackLocked;
    }

    // ================================================================= 필살기 (1.1.9~ 「필살기 누르면 토글」: 첫 누름 = 조준 시작, 두 번째 누름 = 발동)
    public static bool UltDown => Auto ? Time.frameCount == UltDownFrame : GameSettings.UltToggle ? UltTick().down : KeyBindings.Down(GameAction.Ultimate);
    public static bool UltHeld => Auto ? AutoUlt : GameSettings.UltToggle ? UltTick().held : KeyBindings.Held(GameAction.Ultimate);
    public static bool UltUp => Auto ? Time.frameCount == UltUpFrame : GameSettings.UltToggle ? UltTick().up : KeyBindings.Up(GameAction.Ultimate);
    public static bool UltLatched => ultLatched && GameSettings.UltToggle && !Auto;
    public static void ResetUltLatch() => ultLatched = false;

    static bool ultLatched;
    static int ultFrame = -1, latchedAt = -1;
    static (bool down, bool held, bool up) ultNow;

    // 한 프레임에 여러 스크립트가 읽어도 같은 답 (누름은 프레임마다 한 번만 판정)
    static (bool down, bool held, bool up) UltTick()
    {
        if (ultFrame == Time.frameCount) return ultNow;
        ultFrame = Time.frameCount;
        bool down = false, up = false;
        // 눌렀는데 아무것도 시작하지 않았으면 (게이지가 덜 참 등) 다음 프레임에 풀어 둠 — 다음 누름이 「발동」으로 헛돌지 않게
        if (ultLatched && Time.frameCount > latchedAt + 1 && !UltEngaged()) ultLatched = false;
        if (Time.timeScale > 0f && KeyBindings.Down(GameAction.Ultimate))
        {
            if (!ultLatched) { ultLatched = true; latchedAt = Time.frameCount; down = true; }
            else { ultLatched = false; up = true; }
        }
        ultNow = (down, ultLatched, up);
        return ultNow;
    }

    static bool UltEngaged()
    {
        PlayerController p = Hostile.Player;
        if (p == null) return false;
        if (p.IsSkillUsing || p.InstantAiming) return true;
        CharacterKit k = CharacterKit.Instance;
        return k != null && k.UltEngaged;
    }

    // 1.0.5~ UI 를 가리키는 자리: 패드로 메뉴를 다루면 가상 커서, 아니면 마우스 (툴팁 · 영혼 트리 확대 기준)
    public static Vector3 PointerPosition => GamePad.InMenu ? (Vector3)GamePad.CursorPosition : Input.mousePosition;

    // 1.0.5~ ESC 와 같은 취소 · 뒤로: ESC 키 · 패드 Menu(어디서나) · 패드 B(전투가 아닐 때)
    public static bool EscDown => Input.GetKeyDown(KeyCode.Escape) || GamePad.CancelDown;
}
