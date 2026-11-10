using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 2.0.4~ 게임을 켤 때 한 번: 개발사 「민공방 게임즈」 로고 애니메이션 → 끝나면 기존 타이틀 시작 연출(TitleIntro)
//   종이 조각이 아래부터 툭툭 떨어져 쌓여 '민' → 재생 단추 톡 → 불꽃이 켜지며 올라가는 네 음 → 아래에 이름 → 3.1초에 0.38초 동안 사라짐
//   원본(수치 · 식 · 소리 모두 여기서 옮김, 바뀌면 같이 고침): Deepfold 저장소 Marketing/brand/ident/ident_data.json · ident.js (render · audio)
//   아무 키 · 클릭으로 넘김 · 자동 봇 · 배치 · 트레일러는 건너뜀 · 메인 메뉴로 돌아올 때는 다시 안 나옴
//   떠 있는 동안 TitleIntro.Showing 이 참 → ESC · 패치노트 · 재개 카운트다운이 기다림, 막(정렬 31600)이 누름을 막음
// shortcut: 원본의 종이 가장자리 거칠기(feTurbulence 변위)와 가장자리 부드럽게(AA)는 없음, 계단이 거슬리면 가장자리 띠를 덧댐
public class StudioIdent : Graphic
{
    // ---------------------------------------------------------------- ident_data.json
    static readonly Color32 Paper = new Color32(0xF1, 0xEA, 0xDB, 255), Ink = new Color32(0x22, 0x25, 0x3A, 255), Red = new Color32(0xC4, 0x47, 0x2C, 255);
    const int Drop = 0, Unfold = 1, Pop = 2;
    struct Piece
    {
        public float[] p; public bool red; public int kind; public float t, rot; public int note;
        public Piece(bool red, int kind, float t, float rot, int note, params float[] p) { this.red = red; this.kind = kind; this.t = t; this.rot = rot; this.note = note; this.p = p; }
    }
    // 그리는 순서 = polys 순서, 시각 · 회전 · 음은 timeline 의 같은 id
    static readonly Piece[] Pieces =
    {
        new Piece(false, Drop, 790f, -4f, 69, 10.48f, 14.22f, 51.94f, 14.11f, 39.55f, 25.79f, 22.11f, 25.71f),      // m_top
        new Piece(false, Drop, 700f, 5f, 66, 51.99f, 14.85f, 51.4f, 55.25f, 39.85f, 43.88f, 40.52f, 25.95f),        // m_right
        new Piece(false, Drop, 520f, 4f, 62, 11.06f, 56.36f, 22.63f, 44.43f, 39.17f, 43.77f, 51.5f, 55.36f),        // m_bottom
        new Piece(false, Drop, 610f, -6f, 64, 9.81f, 14.24f, 21.18f, 26.42f, 21.63f, 43.92f, 10.17f, 55.63f),       // m_left
        new Piece(true, Unfold, 470f, 0f, 0, 10.46f, 56.48f, 21.5f, 56.06f, 22.09f, 64.24f, 10.83f, 63.92f),        // conn_a
        new Piece(false, Drop, 270f, 6f, 57, 10.06f, 64.19f, 21.5f, 64.01f, 21.87f, 79.54f, 10.56f, 91.36f),        // n_vert
        new Piece(false, Drop, 100f, -5f, 50, 11.05f, 92.3f, 21.99f, 79.8f, 75.91f, 80.09f, 87.98f, 91.8f),         // n_horiz
        new Piece(true, Unfold, 1010f, 0f, 0, 75.73f, 56.65f, 88.1f, 56.26f, 87.39f, 91.29f, 76.82f, 79.83f),       // conn_b
        new Piece(false, Drop, 1090f, 4f, 71, 75.78f, 26.23f, 87.36f, 26.03f, 88.12f, 55.13f, 76.27f, 55.43f),      // candle
        new Piece(false, Pop, 1330f, 0f, 0, 27.44f, 30.23f, 36.54f, 35.16f, 27.36f, 39.94f),                        // play (tick)
    };
    // wick: (81.9, 21.2) → (82.3, 25.6) 굵기 1.5 둥근 끝 → 끝을 반 굵기만큼 늘인 네모, 중심 (82.1, 23.4)
    static readonly float[] Wick = { 81.09f, 20.52f, 82.58f, 20.39f, 83.11f, 26.28f, 81.62f, 26.41f };
    const float WickT = 1330f, WickX = 82.1f, WickY = 23.4f;
    static readonly float[] FlamePath = { 83f, 0.5f, 87.2f, 7.2f, 90.3f, 11.2f, 89.8f, 16.2f, 89.3f, 21.2f, 85.6f, 23.6f, 81.8f, 23.4f, 77.6f, 23.2f, 74.2f, 20.4f, 74.3f, 15.6f, 74.4f, 11.4f, 78.6f, 7.4f, 83f, 0.5f };
    static readonly float[] FlameInnerPath = { 82.3f, 10.4f, 84.6f, 13.2f, 85.8f, 15.2f, 85.5f, 17.2f, 85.2f, 19.2f, 83.7f, 20.2f, 82f, 20.1f, 80.2f, 20f, 78.9f, 18.7f, 79f, 16.8f, 79.1f, 14.8f, 80.6f, 13f, 82.3f, 10.4f };
    const float FlameX = 82f, FlameY = 23.4f, FlameT = 1620f;
    static readonly int[] Arp = { 74, 78, 81, 86 };
    const float DropMs = 330f, FallPart = 0.7f, DropHeight = 62f, Bounce = 1.8f, UnfoldMs = 170f, PopMs = 210f, FlameMs = 280f, ArpStepMs = 75f;
    const float WordT = 1760f, WordFadeMs = 320f, HoldMs = 3100f, FadeOutMs = 380f;

    // 화면 배치: 웹(1080p 에서 로고 폭 320px · viewBox 0 -8 100 108 · 아래 30px 띄우고 46px 이름 · 14px 영문)을 1920x1080 기준 캔버스로
    const float S = 3.2f, Top = 226.5f, KoY = -174f, EnY = -217f;

    static bool played;
    static StudioIdent live;
    public static bool Showing => live != null;

    CanvasGroup group;
    TMP_Text ko, en;
    AudioSource src;
    AudioClip clip;
    float[] flame, flameInner;
    float t, shapeT, waited, startAt = -1f, outAt = -1f;
    UnityEngine.EventSystems.EventSystem navEs;
    bool navWas = true, navOff;

    public static void PlayOnce()
    {
        if (played) return;
        played = true;
        if (GameInput.Auto || Application.isBatchMode || GameInput.TrailerRunning) { TitleIntro.PlayOnce(); return; }
        GameObject go = null;
        try
        {
            UIKit.EnsureStyle();
            go = new GameObject("StudioIdent", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(go);
            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 31600;                 // 타이틀 시작 연출(31500) 위 · 씬 전환 막(32000) 아래
            CanvasScaler cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            RectTransform root = (RectTransform)go.transform;
            RectTransform bg = UIKit.Rect("Back", root, Vector2.zero, Vector2.zero);
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = bg.offsetMax = Vector2.zero;
            bg.gameObject.AddComponent<Image>().color = Paper;      // 누름도 막음
            RectTransform art = UIKit.Rect("Art", root, Vector2.zero, new Vector2(100f * S, 108f * S));
            art.gameObject.AddComponent<StudioIdent>().Build(root);
        }
        catch (System.Exception e)
        {
            // 로고 때문에 메뉴가 막히는 일은 없게: 못 만들면 지우고 바로 타이틀 연출
            Debug.LogWarning("[StudioIdent] 만들기 실패, 건너뜀: " + e.Message);
            if (go != null) Destroy(go);
            TitleIntro.PlayOnce();
        }
    }

    void Build(RectTransform root)
    {
        raycastTarget = false;
        group = root.gameObject.AddComponent<CanvasGroup>();
        flame = Flatten(FlamePath);
        flameInner = Flatten(FlameInnerPath);
        ko = Word(root, "민공방 게임즈", 46f, KoY, -3f);
        ko.fontStyle = FontStyles.Bold;
        en = Word(root, "MINGONGBANG GAMES", 14f, EnY, 42f);
        clip = BuildClip();
        src = root.gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.clip = clip;
        live = this;
        navEs = UnityEngine.EventSystems.EventSystem.current;
        if (navEs != null) { navWas = navEs.sendNavigationEvents; navEs.sendNavigationEvents = false; navOff = true; }
        Apply();
    }

    static TMP_Text Word(RectTransform root, string s, float size, float y, float spacing)
    {
        TMP_Text w = UIKit.Text(root, "", size, Ink, new Vector2(0f, y), new Vector2(1200f, size * 1.6f));
        w.enableAutoSizing = false;
        w.fontSize = size;
        w.characterSpacing = spacing;
        w.text = s;
        UIKit.Forget(w);
        return w;
    }

    void RestoreNav()
    {
        if (navOff && navEs != null) navEs.sendNavigationEvents = navWas;
        navOff = false;
    }

    // 사라지기 시작: 아래에 타이틀 연출을 깔아 둠 (메뉴가 비치지 않게) — 방향키 설정은 TitleIntro 가 다시 잡으므로 먼저 되돌림
    void BeginOut()
    {
        outAt = t;
        RestoreNav();
        TitleIntro.PlayOnce();
    }

    protected override void OnDestroy()
    {
        if (live == this) live = null;
        RestoreNav();
        if (clip != null) Destroy(clip);
        base.OnDestroy();
    }

    void Update()
    {
        try
        {
            // 로딩 막이 걷힌 뒤 시작 (막 아래서 혼자 재생되지 않게 · 막이 안 걷혀도 12초 뒤엔 시작)
            if (startAt < 0f)
            {
                waited += Time.unscaledDeltaTime;
                if (SceneFlow.Loading && waited < 12f) return;
                startAt = Time.realtimeSinceStartup;
                src.volume = GameSettings.SfxVolume;
                src.Play();
            }
            t = (Time.realtimeSinceStartup - startAt) * 1000f;      // 소리와 어긋나지 않게 실제 시각 (timeScale · 끊김과 무관)
            if (outAt < 0f && t > 250f && (Input.anyKeyDown || Input.GetMouseButtonDown(0))) BeginOut();
            if (outAt < 0f && t >= HoldMs) BeginOut();
            if (outAt >= 0f && t - outAt >= FadeOutMs) { Destroy(transform.parent.gameObject); return; }
            Apply();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[StudioIdent] 재생 중 오류, 닫음: " + e.Message);
            enabled = false;
            Destroy(transform.parent.gameObject);
            TitleIntro.PlayOnce();
        }
    }

    void Apply()
    {
        shapeT = GameSettings.ReduceMotion ? HoldMs : t;        // 모션 줄이기: 다 쌓인 그림만
        SetVerticesDirty();
        float a = Mathf.Clamp01((shapeT - WordT) / WordFadeMs), e = 1f - (1f - a) * (1f - a) * (1f - a);
        ko.alpha = e;
        en.alpha = 0.62f * e;
        ko.rectTransform.anchoredPosition = new Vector2(0f, KoY - 8f * (1f - e));
        en.rectTransform.anchoredPosition = new Vector2(0f, EnY - 8f * (1f - e));
        float fade = outAt < 0f ? 1f : 1f - Mathf.Clamp01((t - outAt) / FadeOutMs);
        group.alpha = fade;
        if (src != null) src.volume = GameSettings.SfxVolume * fade;
    }

    // ---------------------------------------------------------------- 그림 (ident.js render)
    static float Back(float u) { u = Mathf.Clamp01(u) - 1f; const float c = 1.9f; return 1f + (c + 1f) * u * u * u + c * u * u; }

    // 2x3 변환: x' = a x + c y + e, y' = b x + d y + f (로고 좌표, y 아래로)
    struct M { public float a, b, c, d, e, f; }

    static M ScaleAbout(float ox, float oy, float sx, float sy, float skew = 0f)
    {
        M m; m.a = sx; m.b = 0f; m.c = skew * sy; m.d = sy; m.e = ox - sx * ox - skew * sy * oy; m.f = oy - sy * oy;
        return m;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float now = shapeT;
        foreach (Piece s in Pieces)
        {
            if (now < s.t) continue;
            float[] p = s.p;
            int n = p.Length / 2;
            float cx = 0f, cy = 0f, maxY = -1e9f;
            for (int i = 0; i < n; i++) { cx += p[i * 2]; cy += p[i * 2 + 1]; maxY = Mathf.Max(maxY, p[i * 2 + 1]); }
            cx /= n; cy /= n;
            M m; float op = 1f;
            if (s.kind == Drop)
            {
                float u = (now - s.t) / DropMs, y = 0f, r = 0f;
                if (u < FallPart) { float q = u / FallPart; y = -DropHeight * (1f - q * q); r = s.rot * (1f - q); op = Mathf.Clamp01(q * 4f); }
                else if (u < 1f) { float q = (u - FallPart) / (1f - FallPart); float k = Mathf.Sin(Mathf.PI * q) * (1f - q); y = Bounce * k; r = -s.rot * 0.18f * k; }
                float cs = Mathf.Cos(r * Mathf.Deg2Rad), sn = Mathf.Sin(r * Mathf.Deg2Rad);
                m.a = cs; m.b = sn; m.c = -sn; m.d = cs; m.e = cx - cx * cs + cy * sn; m.f = cy - cx * sn - cy * cs + y;
            }
            else if (s.kind == Unfold) m = ScaleAbout(0f, maxY, 1f, Mathf.Max(0.001f, Back((now - s.t) / UnfoldMs)));
            else { float k = Mathf.Max(0.001f, Back((now - s.t) / PopMs)); m = ScaleAbout(cx, cy, k, k); }
            Fan(vh, p, m, s.red ? Red : Ink, op);
        }
        if (now >= WickT) { float k = Mathf.Max(0.001f, Back((now - WickT) / PopMs)); Fan(vh, Wick, ScaleAbout(WickX, WickY, k, k), Ink, 1f); }
        if (now >= FlameT)
        {
            float g = Back((now - FlameT) / FlameMs), tt = now - FlameT - FlameMs, sy = g, sk = 0f;
            if (tt > 0f) { sy = 1f + 0.05f * Mathf.Sin(tt / 95f) + 0.03f * Mathf.Sin(tt / 41f); sk = 2.2f * Mathf.Sin(tt / 140f); }
            M m = ScaleAbout(FlameX, FlameY, Mathf.Max(0.001f, Mathf.Min(g, 1f)), Mathf.Max(0.001f, sy), Mathf.Tan(sk * Mathf.Deg2Rad));
            Fan(vh, flame, m, Red, 1f);
            Fan(vh, flameInner, m, Paper, 1f);
        }
    }

    // 첫 점을 축으로 부채꼴 삼각형 (조각은 볼록, 불꽃은 첫 점이 무게중심)
    void Fan(VertexHelper vh, float[] p, M m, Color32 col, float op)
    {
        int n = p.Length / 2, at = vh.currentVertCount;
        col.a = (byte)(255f * op);
        for (int i = 0; i < n; i++)
        {
            float x = p[i * 2], y = p[i * 2 + 1];
            float lx = m.a * x + m.c * y + m.e, ly = m.b * x + m.d * y + m.f;
            vh.AddVert(new Vector3((lx - 50f) * S, Top - (ly + 8f) * S), col, Vector2.zero);
        }
        for (int i = 1; i < n - 1; i++) vh.AddTriangle(at, at + i, at + i + 1);
    }

    // "M x y C ..." 삼차 곡선 4개 → [무게중심, 곡선마다 8점 ..., 첫 점]
    static float[] Flatten(float[] c)
    {
        const int Steps = 8;
        int curves = (c.Length - 2) / 6, n = curves * Steps;
        float[] o = new float[(n + 2) * 2];
        float sx = 0f, sy = 0f;
        for (int k = 0; k < curves; k++)
            for (int j = 0; j < Steps; j++)
            {
                int b = k * 6;
                float u = j / (float)Steps, v = 1f - u;
                float w0 = v * v * v, w1 = 3f * v * v * u, w2 = 3f * v * u * u, w3 = u * u * u;
                float x = w0 * c[b] + w1 * c[b + 2] + w2 * c[b + 4] + w3 * c[b + 6];
                float y = w0 * c[b + 1] + w1 * c[b + 3] + w2 * c[b + 5] + w3 * c[b + 7];
                int i = 1 + k * Steps + j;
                o[i * 2] = x; o[i * 2 + 1] = y;
                sx += x; sy += y;
            }
        o[0] = sx / n; o[1] = sy / n;
        o[(n + 1) * 2] = o[2]; o[(n + 1) * 2 + 1] = o[3];
        return o;
    }

    // ---------------------------------------------------------------- 소리 (ident.js audio): 한 번에 구워 둔 클립 하나
    const int Rate = 44100;

    static AudioClip BuildClip()
    {
        float[] d = new float[(int)(Rate * 3.7f)];
        System.Random rng = new System.Random(4);
        foreach (Piece s in Pieces)
        {
            if (s.kind == Drop) { float at = (s.t + DropMs * FallPart) / 1000f; Thud(d, at, 0.55f, 620f, rng); Tone(d, at, s.note, 0.34f, 0.55f); }
            else if (s.kind == Pop) Thud(d, (s.t + 40f) / 1000f, 0.4f, 2600f, rng);        // 재생 단추 '톡'
        }
        for (int i = 0; i < Arp.Length; i++)
        {
            bool last = i == Arp.Length - 1;
            Tone(d, (FlameT + 60f + i * ArpStepMs) / 1000f, Arp[i], last ? 0.2f : 0.26f, last ? 1.6f : 0.9f);
        }
        for (int i = 0; i < d.Length; i++) d[i] = Mathf.Clamp(d[i] * 0.5f, -1f, 1f);      // 원본 out.gain 0.5
        AudioClip clip = AudioClip.Create("StudioIdent", d.Length, 1, Rate, false);
        clip.SetData(d, 0);
        return clip;
    }

    // 사인 기본음(4ms 에 올라 len 까지 지수로 사라짐) + 4배음(3ms 에 올라 0.09초에 사라짐)
    static void Tone(float[] d, float at, int midi, float vol, float len)
    {
        double hz = 440.0 * System.Math.Pow(2.0, (midi - 69) / 12.0);
        int start = (int)(at * Rate), n = (int)(len * Rate);
        float v2 = vol * 0.22f;
        for (int i = 0; i < n && start + i < d.Length; i++)
        {
            double s = i / (double)Rate;
            double env = s < 0.004 ? vol * s / 0.004 : vol * System.Math.Pow(0.0001 / vol, (s - 0.004) / (len - 0.004));
            double v = env * System.Math.Sin(2.0 * System.Math.PI * hz * s);
            if (s < 0.09)
            {
                double env2 = s < 0.003 ? v2 * s / 0.003 : v2 * System.Math.Pow(0.0001 / v2, (s - 0.003) / 0.087);
                v += env2 * System.Math.Sin(2.0 * System.Math.PI * hz * 4.0 * s);
            }
            d[start + i] += (float)v;
        }
    }

    // 0.07초 잡음 × (1 - i/n)^2.2 → 저역 통과(WebAudio lowpass, Q 1dB)
    static void Thud(float[] d, float at, float vol, float cut, System.Random rng)
    {
        int start = (int)(at * Rate), n = (int)(Rate * 0.07f), tail = (int)(Rate * 0.02f);
        double w0 = 2.0 * System.Math.PI * cut / Rate, cs = System.Math.Cos(w0);
        double alpha = System.Math.Sin(w0) / (2.0 * System.Math.Pow(10.0, 1.0 / 20.0));
        double a0 = 1.0 + alpha, b0 = (1.0 - cs) / 2.0 / a0, b1 = (1.0 - cs) / a0, a1 = -2.0 * cs / a0, a2 = (1.0 - alpha) / a0;
        double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
        for (int i = 0; i < n + tail && start + i < d.Length; i++)
        {
            double x = i < n ? (rng.NextDouble() * 2.0 - 1.0) * System.Math.Pow(1.0 - i / (double)n, 2.2) : 0.0;
            double y = b0 * x + b1 * x1 + b0 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x; y2 = y1; y1 = y;
            d[start + i] += (float)(y * vol);
        }
    }
}
