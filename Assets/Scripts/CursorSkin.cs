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
        instance.useGUILayout = false;     // 2026-11-04~ 메뉴 조각만 그리는 OnGUI (레이아웃 단계 없음)
        Refresh();
    }

    // 설정 · 스킨이 바뀌었을 때
    public static void Refresh()
    {
        SkinDef s = SkinData.Equipped(SkinKind.Cursor);
        skinId = s != null ? s.id : Default;
        skin = s;
        look = SkinLooks.Get(s);
        if (hitTex != null) foreach (Texture2D t in hitTex) if (t != null) Object.Destroy(t);
        if (ultTex != null) Object.Destroy(ultTex);
        hitTex = null; ultTex = null;
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
                    && InGameScene()                        // 1.2.5~ 씬 이름을 매 프레임 읽지 않게 (아래 캐시)
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
        Texture2D tex;
        // 2026-11-04~ 커서 스킨 전투 반응: 평타 순간 3프레임 · 필살기 조준 중 조준점 (소프트웨어 커서라 SetCursor 를 그때만)
        tex = null;
        string special = aim ? SpecialAim(out tex) : null;
        if (special != null) name = special;
        if (ReferenceEquals(name, shown)) return;

        if (special == null) tex = Load(name) ?? Load(fallback[aim ? 1 : 0]);
        if (tex == null) return;
        // 1.0.4~ 32×32 이하 그림은 하드웨어 커서(마우스를 늦게 따라오지 않음), 그보다 크면 크기 제한이 없는 소프트웨어 커서
        Cursor.SetCursor(tex, aim ? (tex.width == 48 ? AimHot : new Vector2(tex.width * 0.5f, tex.height * 0.5f)) : ArrowHot, tex.width <= 32 && tex.height <= 32 ? CursorMode.Auto : CursorMode.ForceSoftware);
        shown = name;
    }

    // 최적화: 씬 이름 글자(매 호출 새 문자열)를 씬이 바뀔 때만 읽음 (1.2.5~ UpdateConfine 도 같이 쓰도록 static)
    static int sceneHandle = -1;
    static bool sceneIsGame;
    internal static bool InGameScene()      // 1.5.9~ MouseSensitivity 도 씀
    {
        Scene s = SceneManager.GetActiveScene();
        if (s.handle != sceneHandle) { sceneHandle = s.handle; sceneIsGame = s.name == "GameScene"; }
        return sceneIsGame;
    }

    // ================================================================= 2026-11-04~ 커서 스킨 연출 (기획 3-4절 · 4-16절)
    //   P2 클릭 반응: 메뉴 = 커서 끝 테마 조각(OnGUI · 실제 시간) + 클릭음 / 전투 = 평타 순간 조준점 3프레임
    //   P3 호버음 (UISound) · P4 꼬리(메뉴 · 멈춘 화면) · P5 필살기 조준점 · P6 메뉴 금가루 — 게임 커서를 끄면 모두 없음
    static SkinDef skin;
    static SkinLook look;
    static Texture2D[] hitTex;
    static Texture2D ultTex;
    static float hitAt = -9f;
    static bool ultWas;
    static readonly string[] hitNames = { "skin_aim_hit_0", "skin_aim_hit_1", "skin_aim_hit_2" };

    static bool SkinOn => GameSettings.GameCursor && look != null && !VisualRules.Legacy && !Application.isBatchMode;

    // 메뉴 클릭 · 호버 소리 (UISound) — 커서 스킨에 그 칸이 없으면 null
    public static AudioClip SkinSound(string slot) => SkinOn ? SkinAudio.Clip(skin, slot) : null;

    // 전투 평타 순간 (SkinFx.OnAttack)
    //   연사(화염방사 0.07초 등)에 SetCursor 가 초당 수십 번 불리지 않게 0.25초에 한 번
    public static void OnAttack() { if (SkinOn && Time.unscaledTime - hitAt >= 0.25f) hitAt = Time.unscaledTime; }

    static string SpecialAim(out Texture2D tex)
    {
        tex = null;
        if (!SkinOn) return null;
        // P5 필살기 조준 중 (거너 = 필살기 사격 중 · 영웅 키트 = 필살기 키를 누르는 동안)
        bool ult = look.Fx("ultaim") != null && UltAiming();
        // 조준 시작 순간 소리 (영혼 커서 낮은 「우웅」 · 황금 커서 「띵」 — 기획 4-16) · 조준이 끝나면 다시 준비
        if (ult && !ultWas && SpecialAbilities.SharedFx != null) SkinAudio.Play(skin, "ultaim", 0.4f, 0f, SkinAudio.Prio.Lvl);
        ultWas = ult;
        if (ult)
        {
            if (ultTex == null) ultTex = MakeUltAim();
            if (ultTex != null) { tex = ultTex; return "skin_aim_ult"; }
        }
        float k = (Time.unscaledTime - hitAt) / 0.1f;
        if (k < 0f || k >= 1f) return null;
        if (hitTex == null) hitTex = MakeHitFrames();
        if (hitTex == null) return null;
        int f = Mathf.Clamp((int)(k * 3f), 0, 2);
        tex = hitTex[f];
        return hitNames[f];
    }

    static bool UltAiming()
    {
        PlayerController p = Hostile.Player;
        if (p == null) return false;
        if (CharacterKit.Instance != null) return CharacterKit.Instance.UltAiming;
        return p.IsSkillUsing;
    }

    // 조준점 그림(읽기 가능한 커서 텍스처)에서 3프레임: shrink 오므라짐 · flicker 일렁 · glow 둘레 빛 · ting 금빛 번쩍
    static Texture2D[] MakeHitFrames()
    {
        Texture2D src = Load(NameFor(true)) ?? Load(fallback[1]);
        if (src == null || !src.isReadable) return null;
        string mode = look.Fx("aimhit") ?? "shrink";
        Color tint = mode == "ting" ? Color.white : look.accent;
        Texture2D[] o = new Texture2D[3];
        float[] scale = mode == "shrink" ? new[] { 0.78f, 0.86f, 0.94f } : mode == "ting" ? new[] { 1.08f, 1.04f, 1f } : new[] { 1f, 1f, 1f };
        for (int i = 0; i < 3; i++)
        {
            float glow = mode == "shrink" ? 0f : (3 - i) / 3f;
            o[i] = Remap(src, scale[i], mode == "flicker" ? (i % 2 == 0 ? 1 : -1) : 0, tint, glow);
        }
        return o;
    }

    // 필살기 조준점: 조준점 위에 테마 글리프(영혼 눈 · 왕관)를 겹침
    static Texture2D MakeUltAim()
    {
        Texture2D src = Load(NameFor(true)) ?? Load(fallback[1]);
        Sprite g = SkinArt.Get(look.Fx("ultaim"));
        if (src == null || !src.isReadable || g == null || !g.texture.isReadable) return null;
        Texture2D t = Remap(src, 1f, 0, look.accent, 0.3f);
        Texture2D gt = g.texture;
        Color[] gp = gt.GetPixels();
        int gw = gt.width, gh = gt.height, k = Mathf.Max(1, Mathf.Min(t.width / 2 / gw, t.height / 2 / gh));
        int ox = (t.width - gw * k) / 2, oy = (t.height - gh * k) / 2;
        for (int y = 0; y < gh * k; y++)
            for (int x = 0; x < gw * k; x++)
            {
                Color c = gp[(y / k) * gw + x / k];
                if (c.a < 0.5f) continue;
                t.SetPixel(ox + x, oy + y, new Color(c.r * look.accent.r, c.g * look.accent.g, c.b * look.accent.b, 1f));
            }
        t.Apply(false, false);
        return t;
    }

    static Texture2D Remap(Texture2D src, float scale, int shiftX, Color tint, float glow)
    {
        int w = src.width, h = src.height;
        Color[] p = src.GetPixels();
        Color[] o = new Color[p.Length];
        float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int sx = Mathf.RoundToInt(cx + (x - cx) / scale) - shiftX, sy = Mathf.RoundToInt(cy + (y - cy) / scale);
                Color c = sx >= 0 && sy >= 0 && sx < w && sy < h ? p[sy * w + sx] : Color.clear;
                if (glow > 0f && c.a > 0.1f) c = Color.Lerp(c, new Color(tint.r, tint.g, tint.b, c.a), glow * 0.7f);
                o[y * w + x] = c;
            }
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        t.SetPixels(o);
        t.Apply(false, false);
        return t;
    }

    // ---- 메뉴 조각 · 꼬리 · 금가루 (OnGUI · 실제 시간 — 패드 커서를 그리는 GamePad.OnGUI 와 같은 방식)
    struct Bit { public Vector2 p, v; public float age, life, size; public Texture2D tex; public Color c; }
    static readonly List<Bit> bits = new List<Bit>(64);
    static float tailAt, dustAt;
    static Vector2 lastMouse;

    static bool MenuLike => Time.timeScale == 0f || !InGameScene();

    // 메뉴 버튼을 눌렀을 때 (UISound.Click)
    public static void MenuClick()
    {
        if (!SkinOn) return;
        string r = look.Fx("click");
        if (r == null) return;
        string[] p = r.Split(' ');
        Sprite s = SkinArt.Get(p[0]);
        if (s == null) return;
        int n = p.Length > 1 && int.TryParse(p[1], out int k) ? Mathf.Clamp(k, 1, 12) : 3;
        float unit = Screen.height / 1080f;
        Vector2 at = ScreenMouse();
        for (int i = 0; i < n && bits.Count < 64; i++)
            bits.Add(new Bit { p = at, v = Random.insideUnitCircle.normalized * 160f * Random.Range(0.6f, 1.2f) * unit, life = Random.Range(0.22f, 0.3f), size = 14f * unit * Random.Range(0.8f, 1.2f), tex = s.texture, c = look.main });
    }

    static Vector2 ScreenMouse()
    {
        Vector3 m = GameInput.MousePosition;
        return new Vector2(m.x, Screen.height - m.y);
    }

    void OnGUI()
    {
        if (!SkinOn) { bits.Clear(); return; }
        if (Event.current.type != EventType.Repaint) return;
        float dt = Time.unscaledDeltaTime;
        Vector2 m = ScreenMouse();
        if (MenuLike)
        {
            // P4 꼬리: 커서가 움직이면 뒤로 테마 점
            string tail = look.Fx("tail");
            if (tail != null && (m - lastMouse).sqrMagnitude > 4f && Time.unscaledTime >= tailAt && bits.Count < 64)
            {
                tailAt = Time.unscaledTime + 0.035f;
                Sprite s = SkinArt.Get(tail.Split(' ')[0]);
                if (s != null) bits.Add(new Bit { p = m, v = Random.insideUnitCircle * 20f, life = 0.3f, size = 9f * Screen.height / 1080f, tex = s.texture, c = look.main });
            }
            // P6 메뉴 금가루: 커서 둘레에서 커서 쪽으로 천천히 모임
            string dust = look.Fx("dust");
            if (dust != null && Time.unscaledTime >= dustAt && bits.Count < 48)
            {
                dustAt = Time.unscaledTime + 0.12f;
                Sprite s = SkinArt.Get(dust);
                Vector2 from = m + Random.insideUnitCircle.normalized * Screen.height * 0.25f;
                if (s != null) bits.Add(new Bit { p = from, v = (m - from) / 1.2f, life = 1.2f, size = 7f * Screen.height / 1080f, tex = s.texture, c = look.accent });
            }
        }
        else bits.Clear();      // 전투 화면엔 OnGUI 덧그림 금지 (2-2절) — 멈춘 화면에서 만든 조각은 재개하면 지움
        lastMouse = m;
        if (bits.Count == 0) return;
        Color keep = GUI.color;
        for (int i = bits.Count - 1; i >= 0; i--)
        {
            Bit b = bits[i];
            b.age += dt;
            if (b.age >= b.life || b.tex == null) { bits.RemoveAt(i); continue; }
            b.p += b.v * dt;
            b.v *= Mathf.Max(0f, 1f - 3f * dt);
            bits[i] = b;
            float k = b.age / b.life;
            GUI.color = new Color(b.c.r, b.c.g, b.c.b, 1f - k);
            float sz = b.size * (1f - 0.4f * k);
            float aspect = b.tex.width / (float)Mathf.Max(1, b.tex.height);
            GUI.DrawTexture(new Rect(b.p.x - sz * aspect * 0.5f, b.p.y - sz * 0.5f, sz * aspect, sz), b.tex);
        }
        GUI.color = keep;
    }
}
