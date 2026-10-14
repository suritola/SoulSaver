using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 1.0.5~ 게임패드 (엑스박스 배열 · 스팀 덱 · 플레이스테이션 패드는 스팀 입력으로 같은 배열)
// 옛 Input Manager 만 씀: 버튼은 JoystickButton0~9, 스틱 · 트리거 · 십자키는 InputManager.asset 의 「Pad ...」 축
// 마지막으로 쓴 장치(패드 / 키보드 · 마우스)를 기억해서 안내 글자(KeyBindings.Name) · 커서를 바꿈
//   전투: 왼쪽 스틱 이동, 오른쪽 스틱 조준 + 평타 자동, RT 공격 · LT 필살기, A 상호작용, X 재장전, Y 무기 교체,
//         RB · LB · L3 스킬 1 · 2 · 3, View 영혼 트리, Menu 일시정지 (버튼은 KeyBindings 가 키보드 키와 함께 읽음)
//   메뉴 · 멈춘 화면: 왼쪽 스틱으로 화면 커서, 십자키로 가까운 버튼에 붙음, A = 좌클릭, B = ESC, 오른쪽 스틱 = 스크롤
//   가상 커서는 EventSystem 입력(BaseInput, GamePadPointer)을 바꿔 끼워 버튼 · 슬라이더 · 끌기 · 스크롤 · 툴팁이 마우스와 똑같이 동작
// 트레일러 자동 조종(GameInput.Auto) · 배치 모드(자동 테스트)에서는 아무것도 하지 않음
[DefaultExecutionOrder(-900)]
public class GamePad : MonoBehaviour
{
    // 버튼 번호 (윈도우 XInput 의 엑스박스 배열)
    const KeyCode BtnA = KeyCode.JoystickButton0, BtnB = KeyCode.JoystickButton1, BtnX = KeyCode.JoystickButton2, BtnY = KeyCode.JoystickButton3,
                  BtnLB = KeyCode.JoystickButton4, BtnRB = KeyCode.JoystickButton5, BtnView = KeyCode.JoystickButton6, BtnMenu = KeyCode.JoystickButton7,
                  BtnL3 = KeyCode.JoystickButton8, BtnR3 = KeyCode.JoystickButton9;
    const int ButtonCount = 10;         // JoystickButton0 ~ 9 (아무 버튼이나 눌렀는지)

    // InputManager.asset 의 축 이름 (위 = +1 이 되게 Y 는 뒤집어 둠)
    const string AxLX = "Pad LX", AxLY = "Pad LY", AxRX = "Pad RX", AxRY = "Pad RY", AxDX = "Pad DX", AxDY = "Pad DY", AxLT = "Pad LT", AxRT = "Pad RT";

    const float StickDead = 0.2f;       // 스틱 둥근 데드존
    const float StickFull = 0.9f;       // 이만큼 밀면 꽉 참
    const float TriggerOn = 0.4f;       // 트리거를 이만큼 당기면 누름
    const float AimOn = 0.3f;           // 오른쪽 스틱을 이만큼 기울이면 조준 방향을 바꿈
    const float AimFireOn = 0.5f;       // 이만큼 기울이면 평타 자동 발사
    const float AimDistance = 5f;       // 가상 조준점까지 거리 (월드)
    const float AssistRange = 11f;      // 보조 조준: 이 거리 안의 가장 가까운 적
    const float AssistDelay = 0.4f;     // 스틱을 놓은 직후엔 마지막 방향 그대로 (활을 놓아 쏠 때 엉뚱한 적으로 돌지 않게)
    const float CursorSpeed = 1500f;    // 가상 커서 속도 (1080p 기준 픽셀/초)
    const float ScrollSpeed = 20f;      // 오른쪽 스틱 스크롤 (휠 칸/초)

    static GamePad instance;

    // 마지막으로 쓴 장치가 패드인지 (안내 글자 · 커서가 이걸 따름)
    public static bool Active { get; private set; }

    static bool broken;                 // 축 이름이 없음 (InputManager 설정이 빠진 빌드) → 패드 끔
    static bool first = true;           // 처음 읽을 때는 「방금 누름」으로 치지 않음 (꽂혀 있던 트리거 · 스틱으로 갑자기 패드가 되지 않게)
    static int polled = -1;
    static Vector2 left, right, leftPrev, rightPrev;
    static int dx, dy, dxPrev, dyPrev;  // 십자키 (-1 · 0 · 1)
    static bool rt, lt, rtPrev, ltPrev, stickFire, stickFirePrev;
    static Vector3 lastMouse;
    static bool mouseKnown, localeDirty;
    static int dpadTakenFrame = -1;     // 카드 창이 십자키 좌우를 가져간 프레임 (그 프레임엔 버튼 붙기를 안 함)

    // 가상 커서 (화면 좌표, 아래 왼쪽이 0)
    static Vector2 cursor;
    static bool clickHeld;              // A 를 클릭으로 누르고 있음
    static int clickDownFrame = -1, clickUpFrame = -1, interactFrame = -1;

    // 전투 조준
    static Vector2 aimDir = Vector2.right;
    static float stickAimAt = -9f;
    static Vector3 aimWorld;
    static bool hasAim;

    static bool inGameScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (Application.isBatchMode || instance != null) return;
        GameObject go = new GameObject("GamePad");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GamePad>();
        instance.useGUILayout = false;          // OnGUI 는 커서 그림만 (배치 계산 생략)
        SceneManager.sceneLoaded += (s, m) => SceneChanged();
        SceneManager.activeSceneChanged += (a, b) => SceneChanged();
        SceneChanged();
    }

    static void SceneChanged()
    {
        inGameScene = SceneManager.GetActiveScene().name == "GameScene";
        hookedEs = null;
    }

    static bool Usable => instance != null && !broken && !GameInput.Auto && !Application.isBatchMode;

    // 전투 중 (게임 씬에서 시간이 흐르고 연출이 아닐 때) — 아니면 메뉴처럼 커서를 씀
    static bool Combat => inGameScene && Time.timeScale > 0f && !StoryDirector.Playing;

    // 패드로 메뉴 · 창을 다루는 중 (가상 커서가 보임)
    public static bool InMenu => Active && !Combat;

    // 패드로 싸우는 중 (GameInput.MousePosition 이 가상 조준점이 됨)
    public static bool Aiming { get { Poll(); return Active && hasAim; } }
    public static Vector3 AimWorld { get { Poll(); return aimWorld; } }

    public static Vector2 CursorPosition { get { Poll(); return cursor; } }

    // ================================================================= 매 프레임 읽기
    // 다른 스크립트가 먼저 물어봐도 한 프레임에 한 번만 (지난 프레임 값과 비교해 「방금 누름」을 앎)
    static void Poll()
    {
        int f = Time.frameCount;
        if (polled == f) return;
        polled = f;
        leftPrev = left; rightPrev = right;
        dxPrev = dx; dyPrev = dy;
        rtPrev = rt; ltPrev = lt;
        stickFirePrev = stickFire;

        if (!Usable)
        {
            Clear();
            if (Active) SetActive(false);
            return;
        }
        try
        {
            left = Stick(Input.GetAxisRaw(AxLX), Input.GetAxisRaw(AxLY));
            right = Stick(Input.GetAxisRaw(AxRX), Input.GetAxisRaw(AxRY));
            dx = Digit(Input.GetAxisRaw(AxDX));
            dy = Digit(Input.GetAxisRaw(AxDY));
            rt = Input.GetAxisRaw(AxRT) > TriggerOn;
            lt = Input.GetAxisRaw(AxLT) > TriggerOn;
        }
        catch (System.ArgumentException)
        {
            broken = true;
            Clear();
            if (Active) SetActive(false);
            return;
        }
        if (first)
        {
            first = false;
            leftPrev = left; rightPrev = right; dxPrev = dx; dyPrev = dy; rtPrev = rt; ltPrev = lt;
        }

        // 장치 감지: 패드 버튼 · 스틱 · 트리거 · 십자키를 새로 움직이면 패드, 마우스를 움직이거나 키를 누르면 키보드 · 마우스
        bool padButton = AnyButtonDown();
        bool padMove = Moved(left, leftPrev) || Moved(right, rightPrev) || Edge(dx, dxPrev) != 0 || Edge(dy, dyPrev) != 0
                       || (rt && !rtPrev) || (lt && !ltPrev);
        Vector3 mp = Input.mousePosition;
        bool mouseMoved = mouseKnown && (mp - lastMouse).sqrMagnitude > 9f;
        lastMouse = mp;
        mouseKnown = true;
        bool keyboardMouse = mouseMoved || Input.mouseScrollDelta.sqrMagnitude > 0f || (Input.anyKeyDown && !padButton);
        if (padButton || padMove) SetActive(true);
        else if (keyboardMouse) SetActive(false);

        if (!Active)
        {
            stickFire = false;
            hasAim = false;
            return;
        }

        if (!Combat)
        {
            // 메뉴: 왼쪽 스틱으로 커서 (살짝 기울이면 천천히)
            if (left != Vector2.zero)
                cursor += left * left.magnitude * CursorSpeed * (Screen.height / 1080f) * Time.unscaledDeltaTime;
            cursor = ClampToScreen(cursor);
            // A: 버튼 · 카드 위면 좌클릭, 빈 곳이면 상호작용(확정) 키
            if (Input.GetKeyDown(BtnA))
            {
                if (OverClickable(cursor)) { clickHeld = true; clickDownFrame = f; }
                else interactFrame = f;
            }
            stickFire = false;
            hasAim = false;
        }
        else
        {
            if (Input.GetKeyDown(BtnA)) interactFrame = f;
            UpdateAim();
        }
        if (clickHeld && !Input.GetKey(BtnA)) { clickHeld = false; clickUpFrame = f; }
    }

    static void Clear()
    {
        left = right = Vector2.zero;
        dx = dy = 0;
        rt = lt = stickFire = hasAim = false;
        if (clickHeld) { clickHeld = false; clickUpFrame = Time.frameCount; }
    }

    static void SetActive(bool on)
    {
        if (Active == on) return;
        Active = on;
        if (on) cursor = ClampToScreen(Input.mousePosition);       // 마우스가 있던 자리에서 출발
        else if (clickHeld) { clickHeld = false; clickUpFrame = Time.frameCount; }
        Cursor.visible = !on;           // 패드 동안은 OS 커서를 숨기고 게임 커서 그림을 직접 그림 (OnGUI)
        localeDirty = true;             // 안내 글자를 다음 LateUpdate 에서 새로 (다른 스크립트가 글자를 만드는 도중에 바꾸지 않게)
    }

    static void UpdateAim()
    {
        PlayerController p = Hostile.Player;
        stickFire = right.magnitude > AimFireOn;
        if (p == null) { hasAim = false; return; }
        float m = right.magnitude;
        if (m > AimOn) { aimDir = right / m; stickAimAt = Time.unscaledTime; }
        Vector3 from = p.transform.position;
        Vector3 to = from + (Vector3)(aimDir * AimDistance);
        // 스틱을 놓고 RT · LT 만 누르고 있으면 가장 가까운 적을 겨눔 (없으면 마지막 방향)
        if (m <= AimOn && (rt || lt) && Time.unscaledTime - stickAimAt > AssistDelay)
        {
            Transform e = Specials.NearestEnemy(from, AssistRange);
            if (e != null)
            {
                Vector2 d = e.position - from;
                if (d.sqrMagnitude > 0.01f) { aimDir = d.normalized; to = e.position; }
            }
        }
        to.z = from.z;
        aimWorld = to;
        hasAim = true;
    }

    static Vector2 Stick(float x, float y)
    {
        Vector2 v = new Vector2(x, y);
        float m = v.magnitude;
        if (m < StickDead) return Vector2.zero;
        return v / m * Mathf.Clamp01((m - StickDead) / (StickFull - StickDead));
    }

    static int Digit(float v) => v > 0.5f ? 1 : v < -0.5f ? -1 : 0;
    static int Edge(int now, int before) => now != 0 && now != before ? now : 0;

    // 새로 기울였거나, 기울인 채로 움직이는 중 (한 값에 멈춰 있는 고장 난 축은 패드로 치지 않음)
    static bool Moved(Vector2 now, Vector2 before) =>
        now.sqrMagnitude > 0.25f && (before.sqrMagnitude <= 0.25f || (now - before).sqrMagnitude > 0.0025f);

    static bool AnyButtonDown()
    {
        for (int i = 0; i < ButtonCount; i++)
            if (Input.GetKeyDown(KeyCode.JoystickButton0 + i)) return true;
        return false;
    }

    static Vector2 ClampToScreen(Vector2 p) =>
        new Vector2(Mathf.Clamp(p.x, 0f, Mathf.Max(0f, Screen.width - 1f)), Mathf.Clamp(p.y, 0f, Mathf.Max(0f, Screen.height - 1f)));

    // ================================================================= 동작별 버튼 (KeyBindings 가 키보드 키와 함께 읽음)
    // 이동(위 · 아래 · 왼쪽 · 오른쪽)은 스틱이라 버튼이 없음 → Move
    static readonly KeyCode[] Buttons =
    {
        KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None,
        BtnX, BtnY,                     // 재장전 · 무기 교체
        BtnRB, BtnLB, BtnL3,            // 스킬 1 · 2 · 3
        BtnA, BtnView,                  // 상호작용 · 영혼 트리
        BtnX,                           // 다시 뽑기 (레벨업 창에서만, 키보드의 R 처럼 재장전과 같은 버튼)
        KeyCode.None, KeyCode.None,     // 공격 · 필살기 = RT · LT (트리거 축)
    };

    static KeyCode PadButton(GameAction a) => (int)a < Buttons.Length ? Buttons[(int)a] : KeyCode.None;

    static bool AttackNow => rt || stickFire;
    static bool AttackBefore => rtPrev || stickFirePrev;

    public static bool Held(GameAction a)
    {
        if (!Usable) return false;
        Poll();
        if (!Active) return false;
        if (a == GameAction.Attack) return AttackNow;
        if (a == GameAction.Ultimate) return lt;
        KeyCode k = PadButton(a);
        return k != KeyCode.None && Input.GetKey(k);
    }

    public static bool Down(GameAction a)
    {
        if (!Usable) return false;
        Poll();
        if (!Active) return false;
        if (a == GameAction.Attack) return AttackNow && !AttackBefore;
        if (a == GameAction.Ultimate) return lt && !ltPrev;
        if (a == GameAction.Interact) return interactFrame == Time.frameCount;     // 메뉴에서 버튼 위의 A 는 클릭이라 확정으로 치지 않음
        KeyCode k = PadButton(a);
        return k != KeyCode.None && Input.GetKeyDown(k);
    }

    public static bool Up(GameAction a)
    {
        if (!Usable) return false;
        Poll();
        if (a == GameAction.Attack) return !AttackNow && AttackBefore;
        if (a == GameAction.Ultimate) return !lt && ltPrev;
        KeyCode k = PadButton(a);
        return k != KeyCode.None && Input.GetKeyUp(k);
    }

    // 버튼 이름 (안내 글자 {ATTACK} 등)
    public static string ButtonName(GameAction a)
    {
        switch (a)
        {
            case GameAction.Up: case GameAction.Down: case GameAction.Left: case GameAction.Right: return Loc.T("왼쪽 스틱");
            case GameAction.Attack: return "RT";
            case GameAction.Ultimate: return "LT";
            case GameAction.Reload: case GameAction.Reroll: return "X";
            case GameAction.Swap: return "Y";
            case GameAction.Skill1: return "RB";
            case GameAction.Skill2: return "LB";
            case GameAction.Skill3: return "L3";
            case GameAction.Interact: return "A";
            case GameAction.Upgrade: return "View";
        }
        return "-";
    }

    // 왼쪽 스틱 + 십자키 이동 (길이 0 ~ 1, 살짝 기울이면 천천히) — 전투에서만
    public static Vector2 Move
    {
        get
        {
            if (!Usable) return Vector2.zero;
            Poll();
            if (!Active) return Vector2.zero;
            Vector2 v = left + new Vector2(dx, dy);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }
    }

    public static Vector2 RightStick { get { if (!Usable) return Vector2.zero; Poll(); return Active ? right : Vector2.zero; } }

    // 확대(+) · 축소(-): RT · LT (영혼 트리)
    public static float ZoomAxis { get { if (!Usable) return 0f; Poll(); return !Active ? 0f : (rt ? 1f : 0f) - (lt ? 1f : 0f); } }

    // A 를 방금 누름 (대사 넘기기 · 연출 건너뛰기처럼 클릭 · 확정 어느 쪽이든 되는 곳)
    public static bool ADown { get { if (!Usable) return false; Poll(); return Input.GetKeyDown(BtnA); } }

    // R3 (오른쪽 스틱 누르기)를 방금 누름 — 튜토리얼 「이 단계 건너뛰기」 (그 버튼은 전투 중에만 보여 패드 커서로 누를 수 없음)
    public static bool R3Down { get { if (!Usable) return false; Poll(); return Input.GetKeyDown(BtnR3); } }

    // ESC 와 같은 취소: Menu 는 어디서나(전투 중엔 일시정지), B 는 전투가 아닐 때만 (싸우다 B 로 메뉴가 열리지 않게)
    public static bool CancelDown
    {
        get
        {
            if (!Usable) return false;
            Poll();
            return Input.GetKeyDown(BtnMenu) || (Input.GetKeyDown(BtnB) && !Combat);
        }
    }

    // 지금 이 프레임에 패드 A 로 클릭이 들어감 (onClick 은 뗄 때 불림) — 고른 카드를 A 로 한 번 더 누르면 확정하는 데 씀
    public static bool PadClickNow => Usable && clickUpFrame == Time.frameCount;

    // 가상 커서용 좌클릭 (GamePadPointer)
    public static bool ClickDown { get { Poll(); return clickDownFrame == Time.frameCount; } }
    public static bool ClickUp { get { Poll(); return clickUpFrame == Time.frameCount; } }
    public static bool ClickHeld { get { Poll(); return clickHeld; } }
    public static Vector2 Scroll
    {
        get
        {
            if (!Usable) return Vector2.zero;
            Poll();
            return InMenu ? right * ScrollSpeed * Time.unscaledDeltaTime : Vector2.zero;
        }
    }

    // ================================================================= 카드 창 (레벨업 · 보상 · 무기 진화)
    // 십자키 좌우를 그 창이 씀 (-1 · 0 · 1, 방금 누른 쪽) — 이 프레임엔 커서를 가까운 버튼에 붙이지 않음
    public static int TakeDpadX()
    {
        if (!Usable) return 0;
        Poll();
        if (!Active) return 0;
        dpadTakenFrame = Time.frameCount;
        return Edge(dx, dxPrev);
    }

    // 커서를 그 UI 가운데로 (십자키로 고른 카드)
    public static void WarpTo(RectTransform rt)
    {
        if (!Active || rt == null || !ScreenCenter(rt, out Vector2 c)) return;
        cursor = ClampToScreen(c);
    }

    // ================================================================= 프레임 처리
    void Update()
    {
        Poll();
        Hook();
    }

    void LateUpdate()
    {
        if (localeDirty)
        {
            localeDirty = false;
            Loc.RaiseChanged();         // 「[R]」 ↔ 「[X]」 같은 안내 글자를 새 장치로
        }
        Poll();
        if (!InMenu) return;
        // 십자키: 그쪽의 가장 가까운 버튼으로 커서를 옮김 (카드 창이 좌우를 가져갔으면 위아래만)
        int sx = dpadTakenFrame == Time.frameCount ? 0 : Edge(dx, dxPrev);
        int sy = Edge(dy, dyPrev);
        if (sx != 0) Snap(new Vector2(sx, 0f));
        else if (sy != 0) Snap(new Vector2(0f, sy));
    }

    // 씬마다 EventSystem 의 입력을 가상 커서도 읽는 것으로 바꿔 끼움
    static EventSystem hookedEs;

    static void Hook()
    {
        EventSystem es = EventSystem.current;
        if (es == null || es == hookedEs) return;
        hookedEs = es;
        GamePadPointer p = es.GetComponent<GamePadPointer>();
        if (p == null) p = es.gameObject.AddComponent<GamePadPointer>();
        foreach (BaseInputModule m in es.GetComponents<BaseInputModule>()) m.inputOverride = p;
    }

    // 게임 커서(CursorSkin) 모양을 직접 그림: 메뉴는 화살표, 전투는 가상 조준점에 조준점
    void OnGUI()
    {
        if (!Active || Event.current.type != EventType.Repaint) return;
        bool menu = InMenu;
        if (!menu && !Aiming) return;
        Texture2D tex = CursorSkin.PadTexture(!menu, out Vector2 hot);
        if (tex == null) return;
        Vector2 at = menu ? cursor : (Vector2)GameInput.MousePosition;
        GUI.depth = -1000;
        GUI.DrawTexture(new Rect(at.x - hot.x, Screen.height - at.y - hot.y, tex.width, tex.height), tex);
    }

    // ================================================================= UI 찾기
    static readonly List<RaycastResult> hits = new List<RaycastResult>();
    static PointerEventData ped;
    static EventSystem pedEs;
    static Selectable[] pool = new Selectable[128];

    // 그 자리에서 맨 위에 있는 UI
    static GameObject TopAt(Vector2 pos)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return null;
        if (ped == null || pedEs != es) { ped = new PointerEventData(es); pedEs = es; }
        ped.position = pos;
        hits.Clear();
        es.RaycastAll(ped, hits);
        GameObject g = hits.Count > 0 ? hits[0].gameObject : null;
        hits.Clear();
        return g;
    }

    // 누르거나 끌 수 있는 것 (버튼 · 카드 · 슬라이더 · 스크롤 목록) 위인지
    static bool OverClickable(Vector2 pos)
    {
        GameObject g = TopAt(pos);
        if (g == null) return false;
        return ExecuteEvents.GetEventHandler<IPointerClickHandler>(g) != null
            || ExecuteEvents.GetEventHandler<IPointerDownHandler>(g) != null
            || ExecuteEvents.GetEventHandler<IDragHandler>(g) != null;
    }

    static bool ScreenCenter(RectTransform rt, out Vector2 c)
    {
        c = Vector2.zero;
        if (rt == null) return false;
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        Canvas root = canvas.rootCanvas;
        Camera cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : (root.worldCamera != null ? root.worldCamera : Camera.main);
        c = RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
        return true;
    }

    // 그 방향으로 가장 가까운, 화면에 보이고 다른 창에 가려지지 않은 누를 수 있는 UI 로 커서를 옮김
    static void Snap(Vector2 dir)
    {
        int n = Selectable.allSelectableCount;
        if (pool.Length < n) pool = new Selectable[n * 2];
        n = Selectable.AllSelectablesNoAlloc(pool);
        Vector2 best = cursor;
        float bestScore = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            Selectable s = pool[i];
            if (s == null || !s.IsInteractable()) continue;
            if (!ScreenCenter(s.transform as RectTransform, out Vector2 c)) continue;
            if (c.x < 0f || c.y < 0f || c.x > Screen.width || c.y > Screen.height) continue;
            Vector2 d = c - cursor;
            float along = Vector2.Dot(d, dir);
            if (along < 10f) continue;
            float side = Mathf.Abs(d.x * dir.y - d.y * dir.x);
            if (side > along * 3f) continue;            // 너무 옆쪽은 그 방향으로 치지 않음
            float score = along + side * 2f;
            if (score >= bestScore) continue;
            GameObject top = TopAt(c);
            if (top == null || !top.transform.IsChildOf(s.transform)) continue;      // 다른 창 · 어두운 막에 가려짐
            bestScore = score;
            best = c;
        }
        System.Array.Clear(pool, 0, n);
        cursor = ClampToScreen(best);
    }
}

// 1.0.5~ EventSystem 이 읽는 마우스를, 패드로 메뉴를 다룰 때는 가상 커서로 (StandaloneInputModule 의 inputOverride)
// 키보드 · 마우스만 쓸 때는 기본 입력과 똑같음
public class GamePadPointer : BaseInput
{
    public override Vector2 mousePosition => GamePad.InMenu ? GamePad.CursorPosition : base.mousePosition;

    public override Vector2 mouseScrollDelta
    {
        get
        {
            Vector2 s = GamePad.Scroll;
            return s != Vector2.zero ? s : base.mouseScrollDelta;
        }
    }

    public override bool mousePresent => GamePad.InMenu || base.mousePresent;

    public override bool GetMouseButtonDown(int button) => base.GetMouseButtonDown(button) || (button == 0 && GamePad.ClickDown);
    public override bool GetMouseButtonUp(int button) => base.GetMouseButtonUp(button) || (button == 0 && GamePad.ClickUp);
    public override bool GetMouseButton(int button) => base.GetMouseButton(button) || (button == 0 && GamePad.ClickHeld);

    // 기본 Horizontal · Vertical 축에는 패드 왼쪽 스틱도 들어 있어, 커서를 움직일 때 선택된 버튼이 같이 옮겨 다니지 않게 패드 동안은 0
    public override float GetAxisRaw(string axisName) => GamePad.Active ? 0f : base.GetAxisRaw(axisName);
}
