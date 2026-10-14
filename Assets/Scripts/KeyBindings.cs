using System.Text;
using UnityEngine;

// 바꿀 수 있는 키 (설정 → 조작)
public enum GameAction { Up, Down, Left, Right, Reload, Swap, Skill1, Skill2, Skill3, Interact, Upgrade, Reroll, Attack, Ultimate }     // Reroll: 레벨업 카드 다시 뽑기 (2.1.8~, 저장 번호가 밀리지 않게 맨 뒤) · Attack · Ultimate: 공격 · 필살기 (1.0.4~, 마우스 버튼도 지정 가능)

// 키 설정: PlayerPrefs에 저장. 이동은 방향키도 항상 같이 됨
// 화면 글자의 {SKILL1} 같은 표시는 Loc.T가 지금 키 이름으로 바꿔 줌
public static class KeyBindings
{
    public static readonly GameAction[] All = (GameAction[])System.Enum.GetValues(typeof(GameAction));

    // 스킬 3번은 상점 · 레벨업 확정(Space)과 겹치지 않게 C
    static readonly KeyCode[] Defaults =
    {
        KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D,
        KeyCode.R, KeyCode.Q,
        KeyCode.E, KeyCode.F, KeyCode.C,
        KeyCode.Space, KeyCode.T,
        KeyCode.R,          // 다시 뽑기는 레벨업 창(멈춘 동안)에서만 써서 재장전과 같은 R 이어도 겹치지 않음
        KeyCode.Mouse0, KeyCode.Mouse1,     // 1.0.4~ 공격 · 필살기 (좌 · 우클릭)
    };

    static readonly KeyCode[] Arrows = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };

    static KeyCode[] keys;

    static void Load()
    {
        if (keys != null) return;
        keys = (KeyCode[])Defaults.Clone();
        for (int i = 0; i < keys.Length; i++)
            keys[i] = (KeyCode)Prefs.GetInt("keys." + (GameAction)i, (int)Defaults[i]);
    }

    public static KeyCode Get(GameAction a) { Load(); return keys[(int)a]; }

    // 다른 동작이 이미 쓰는 키면 서로 맞바꿈
    public static void Set(GameAction a, KeyCode key)
    {
        Load();
        if (!CanBind(a, key)) return;
        KeyCode old = keys[(int)a];
        // 1.0.4~ 맞바꿀 키(좌클릭)를 받을 수 없는 동작은 그 동작의 기본 키로 (기본 키도 겹치면 바꾸지 않음)
        for (int i = 0; i < keys.Length; i++)
            if (i != (int)a && keys[i] == key && !Separate(a, (GameAction)i) && Swapped(i, old) == key) return;
        for (int i = 0; i < keys.Length; i++)
            if (i != (int)a && keys[i] == key && !Separate(a, (GameAction)i))
            {
                KeyCode give = Swapped(i, old);
                keys[i] = give;
                Prefs.SetInt("keys." + (GameAction)i, (int)give);
            }
        keys[(int)a] = key;
        Prefs.SetInt("keys." + a, (int)key);
        Loc.RaiseChanged();
    }

    static KeyCode Swapped(int i, KeyCode old) => CanBind((GameAction)i, old) ? old : Defaults[i];

    // 다시 뽑기(레벨업 창)와 전투 키는 같은 키를 써도 됨 (서로 다른 화면에서만 쓰임)
    static bool Separate(GameAction a, GameAction b) => (a == GameAction.Reroll) != (b == GameAction.Reroll) && a != GameAction.Interact && b != GameAction.Interact;

    // 1.0.4~ 좌클릭은 버튼 · 카드를 누르는 데도 쓰여서 공격 · 필살기에만 (다른 마우스 버튼은 어디든)
    public static bool CanBind(GameAction a, KeyCode key) =>
        key != KeyCode.Mouse0 || a == GameAction.Attack || a == GameAction.Ultimate;

    public static void ResetAll()
    {
        Load();
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i] = Defaults[i];
            Prefs.DeleteKey("keys." + (GameAction)i);
        }
        Loc.RaiseChanged();
    }

    // 1.0.5~ 패드 버튼도 같이 (GamePad: RT 공격 · LT 필살기 · A 상호작용 · X 재장전 …, 이동은 PlayerController 가 스틱으로 따로)
    public static bool Held(GameAction a)
    {
        if (Input.GetKey(Get(a))) return true;
        if (a <= GameAction.Right && Input.GetKey(Arrows[(int)a])) return true;
        return GamePad.Held(a);
    }
    public static bool Down(GameAction a) => Input.GetKeyDown(Get(a)) || GamePad.Down(a);
    // 키보드 · 패드 중 하나를 떼도 다른 쪽을 아직 누르고 있으면 뗀 것으로 치지 않음
    public static bool Up(GameAction a) => (Input.GetKeyUp(Get(a)) && !GamePad.Held(a)) || (GamePad.Up(a) && !Input.GetKey(Get(a)));

    // 1.0.5~ 패드를 쓰는 중이면 패드 버튼 이름 (RT · A …), 설정의 키 바꾸기 칸은 KeyName(Get(a)) 로 늘 키보드 이름
    public static string Name(GameAction a) => GamePad.Active ? GamePad.ButtonName(a) : KeyName(Get(a));

    public static string KeyName(KeyCode k)
    {
        if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return ((int)(k - KeyCode.Alpha0)).ToString();
        if (k >= KeyCode.Keypad0 && k <= KeyCode.Keypad9) return "Num" + (int)(k - KeyCode.Keypad0);
        // 1.0.4~ 마우스 버튼 (좌 · 우 · 휠 클릭, 옆 버튼은 「마우스 4」부터)
        if (k == KeyCode.Mouse0) return Loc.T("좌클릭");
        if (k == KeyCode.Mouse1) return Loc.T("우클릭");
        if (k == KeyCode.Mouse2) return Loc.T("휠 클릭");
        if (k >= KeyCode.Mouse3 && k <= KeyCode.Mouse6) return Loc.T("마우스") + " " + (int)(k - KeyCode.Mouse0 + 1);
        switch (k)
        {
            case KeyCode.None: return "-";        // 1.0.4~ 비어 있는 키
            case KeyCode.LeftShift: case KeyCode.RightShift: return "Shift";
            case KeyCode.LeftControl: case KeyCode.RightControl: return "Ctrl";
            case KeyCode.LeftAlt: case KeyCode.RightAlt: return "Alt";
            case KeyCode.UpArrow: return "↑";
            case KeyCode.DownArrow: return "↓";
            case KeyCode.LeftArrow: return "←";
            case KeyCode.RightArrow: return "→";
            case KeyCode.Return: return "Enter";
            case KeyCode.BackQuote: return "`";
            case KeyCode.Minus: return "-";
            case KeyCode.Equals: return "=";
            case KeyCode.LeftBracket: return "[";
            case KeyCode.RightBracket: return "]";
            case KeyCode.Semicolon: return ";";
            case KeyCode.Quote: return "'";
            case KeyCode.Comma: return ",";
            case KeyCode.Period: return ".";
            case KeyCode.Slash: return "/";
            case KeyCode.Backslash: return "\\";
        }
        return k.ToString();
    }

    // 글자 속 {MOVE} {RELOAD} {SWAP} {SKILL1~3} {INTERACT} {UPGRADE} {REROLL} {ATTACK} {ULT} 를 지금 키 이름으로
    public static string Apply(string s)
    {
        if (s == null || s.IndexOf('{') < 0) return s;
        StringBuilder b = new StringBuilder(s);
        b.Replace("{MOVE}", GamePad.Active ? GamePad.ButtonName(GameAction.Up)
                            : Name(GameAction.Up) + " " + Name(GameAction.Left) + " " + Name(GameAction.Down) + " " + Name(GameAction.Right));
        b.Replace("{RELOAD}", Name(GameAction.Reload));
        b.Replace("{SWAP}", Name(GameAction.Swap));
        b.Replace("{SKILL1}", Name(GameAction.Skill1));
        b.Replace("{SKILL2}", Name(GameAction.Skill2));
        b.Replace("{SKILL3}", Name(GameAction.Skill3));
        b.Replace("{INTERACT}", Name(GameAction.Interact));
        b.Replace("{UPGRADE}", Name(GameAction.Upgrade));
        b.Replace("{REROLL}", Name(GameAction.Reroll));       // 1.0.4~
        b.Replace("{ATTACK}", Name(GameAction.Attack));
        b.Replace("{ULT}", Name(GameAction.Ultimate));
        return b.ToString();
    }
}

// 씬에 하나뿐인 오브젝트를 매번 찾지 않고 기억해 둠
// FindFirstObjectByType과 같게, 기억한 오브젝트가 꺼지거나 사라지면 다시 찾음
public static class Cache<T> where T : Component
{
    static T cached;

    public static T Get
    {
        get
        {
            if (cached == null || !cached.gameObject.activeInHierarchy) cached = Object.FindFirstObjectByType<T>();
            return cached;
        }
    }
}
