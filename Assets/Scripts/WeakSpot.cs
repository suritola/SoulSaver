using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 빈틈 (기획서 docs/review/2026-10-18-차별점-구현.md 1절): 보스 · 중간 보스 몸의 빛나는 점
//   「직접 조준한 공격」(평타 · 커서 쪽으로 곧게 쏜 탄 · 근접 가운데 20° · 투척 직격)만 맞히고, 맞으면 피해 보너스 (한 대 상한 최대 체력 2.5% · 중간 보스 6%)
//   부르는 쪽(탄 · 근접 · 투척)이 모양(WeakShape)을 넘겨 WeakHit.Apply 로 늘린 피해를 받고 → TakeDamage → WeakHit.End
//   상한 · 판 기록은 bosss · EnermyController 의 TakeDamage 가 모든 배율(난이도 · 기억 방패 · 치명타 · 고유 스킬)을 곱한 뒤 WeakHit.Settle 로 (영웅마다 같은 기준)
//   보스: 이 파일의 관리자(게임 씬에서만)가 0.25초마다 bosss.Live 를 훑어 붙임 · 중간 보스: MidBossMark.Attach(prefix) 가 붙임
//   저장할 상태 없음 (이어하기로 되살린 보스도 관리자가 다시 붙임 · RunSave.Version 그대로)
public enum WeakKind : byte { Lich, Demon, Slime, Mirror, Developer, Mom, MidBoss }

public class WeakSpot : MonoBehaviour
{
    // 지금 살아 있는 빈틈 (OnEnable 추가 · OnDisable 제거 — 판이 갈수록 쌓이지 않음)
    public static readonly List<WeakSpot> Active = new List<WeakSpot>(8);

    // 판 기록(balance_log) 칸은 RunCounters 「weak.hit」 · 「weak.bonus」(더한 피해) · 「weak.dealt」(빈틈이 붙은 대상에게 준 피해)
    //   — 판 저장(RunSave counters)이 같이 되살려 이어하기도 판 전체 · 계약 weak30 과 같은 값. 여기는 정수로 넘기고 남은 소수만 (RunLog.BeginRun 때 0)
    static float bonusFrac, dealtFrac;
    public static void ResetRun() { bonusFrac = 0f; dealtFrac = 0f; }
    internal static void AddBonus(float v) { bonusFrac += v; int n = (int)bonusFrac; if (n > 0) { bonusFrac -= n; RunCounters.Add("weak.bonus", n); } }
    static void AddDealt(float v) { dealtFrac += v; int n = (int)dealtFrac; if (n > 0) { dealtFrac -= n; RunCounters.Add("weak.dealt", n); } }

    const string IntroKey = "weak.intro";
    const float MidDrawRange = 14f;

    public WeakKind Kind { get; private set; }
    public bool Open { get; private set; }
    public float Radius { get; private set; }
    public bool OutsideBody => Kind == WeakKind.Developer || Kind == WeakKind.Mom;
    public int Count => Kind == WeakKind.Mom ? 2 : 1;
    public Vector2 Center(int i) => centers[Mathf.Clamp(i, 0, 1)];
    // 몸 반지름 (월드) — 탄 판정 선분 길이 · 개발자 궤도
    public float BodyRadius { get; private set; }
    public bool IsMid => Kind == WeakKind.MidBoss;
    // 상한 계산용 최대 체력 (Start 에서 난이도 배율이 붙으므로 그때그때 읽음)
    public float MaxHp => boss != null ? boss.setEnemyHP : enemy != null ? enemy.setEnemyHP : 1f;
    float CapRate => IsMid ? 0.06f : 0.025f;
    // 지금 피해가 들어가지 않음 (망자의 의식 무적 · 죽음)
    bool Blocked => boss != null ? (boss.IsDead || boss.invulnerable || boss.EnemyHealth <= 0f) : enemy == null || enemy.IsDead;

    bosss boss;
    EnermyController enemy;
    BossSkills skills;
    SpriteRenderer body;
    EnemyHitbox hitbox;
    readonly Vector2[] centers = new Vector2[2];
    readonly Mark[] marks = new Mark[2];
    float bornAt, openedAt = -9f, flashUntil, sparkleAt = -9f, landAt = -9f, breatheUntil, lastChime = -9f, chimeAge;
    bool wasOpen, wasAir, wasCasting, evaluated;
    float lastHp = -1f;

    class Mark
    {
        public Transform root;
        public SpriteRenderer ring, outline, core, halo, brackets;
    }

    // 붙이기 (이미 있으면 그대로)
    public static WeakSpot Attach(GameObject host) => Attach(host, KindOf(host));

    public static WeakSpot Attach(GameObject host, WeakKind kind)
    {
        if (host == null) return null;
        if (host.TryGetComponent(out WeakSpot w)) return w;
        w = host.AddComponent<WeakSpot>();
        w.Init(kind);
        return w;
    }

    static WeakKind KindOf(GameObject host)
    {
        if (host != null && host.TryGetComponent(out bosss b))
        {
            if (b.developer) return b.developerMom ? WeakKind.Mom : WeakKind.Developer;
            switch (b.bossKind)
            {
                case 0: return WeakKind.Lich;
                case 1: return WeakKind.Demon;
                case 2: return WeakKind.Slime;
                case 3: return WeakKind.Mirror;
            }
        }
        return WeakKind.MidBoss;
    }

    void Init(WeakKind kind)
    {
        Kind = kind;
        bornAt = Time.time;
        TryGetComponent(out boss);
        TryGetComponent(out enemy);
        TryGetComponent(out skills);
        TryGetComponent(out hitbox);
        body = GetComponent<SpriteRenderer>();
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        for (int i = 0; i < Count; i++) marks[i] = BuildMark(i);
        Refresh();
    }

    void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
    void OnDisable()
    {
        Active.Remove(this);
        FlushDealt();
    }

    // ================================================================= 그림
    static Sprite white;
    static Sprite WhiteSprite
    {
        get
        {
            if (white == null) white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f, 4f);
            return white;
        }
    }

    Mark BuildMark(int i)
    {
        Mark m = new Mark();
        m.root = new GameObject("WeakSpotRing").transform;
        m.root.SetParent(transform, false);
        int layer = SortingLayer.NameToID("Effect");
        Sprite[] rune = Fx.Frames("fx_target_rune");
        Sprite ringSprite = rune != null && rune.Length > 0 ? rune[0] : SpecialAbilities.GlowSprite;
        m.halo = Part(m.root, "Halo", SpecialAbilities.GlowSprite != null ? SpecialAbilities.GlowSprite : WhiteSprite, layer, 36);
        m.outline = Part(m.root, "Outline", ringSprite, layer, 37);
        m.ring = Part(m.root, "Ring", ringSprite, layer, 38);
        m.core = Part(m.root, "Core", CoreSprite, layer, 39);
        m.brackets = Part(m.root, "Brackets", BracketSprite, layer, 40);
        m.root.gameObject.SetActive(false);
        return m;
    }

    // 1.2.7~ 「약점」처럼 보이게: 금이 간 심장 결정(붉은 껍질 · 빛나는 금 · 하얗게 달아오른 가운데) + 네 귀퉁이 조준 꺾쇠 (도트, 한 번만 만듦)
    static Sprite coreSprite, bracketSprite;
    static Sprite CoreSprite => coreSprite != null ? coreSprite : coreSprite = MakeCore();
    static Sprite BracketSprite => bracketSprite != null ? bracketSprite : bracketSprite = MakeBrackets();

    static Sprite PixelSprite(int n, Color32[] px)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0f, 0f, n, n), Vector2.one * 0.5f, n);
    }

    static Sprite MakeCore()
    {
        const int N = 15;
        Color32[] px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = x - 7f, dy = y - 7f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                Color32 c;
                if (r < 1.3f) c = new Color32(255, 255, 240, 255);           // 하얗게 달아오른 가운데
                else if (r < 2.4f) c = new Color32(255, 222, 110, 255);
                else if (r < 3.8f) c = new Color32(240, 110, 40, 255);
                else if (r < 5.2f) c = new Color32(196, 46, 34, 255);
                else if (r < 6.3f) c = new Color32(128, 20, 26, 255);         // 어두운 껍질 가장자리
                else if (r < 7.2f) c = new Color32(42, 6, 12, 255);           // 테두리 (밝은 배경에서도 보이게)
                else c = new Color32(0, 0, 0, 0);
                if (r >= 4.6f && r < 6.3f && (x + y) % 5 == 0) c = new Color32(232, 72, 44, 255);    // 껍질 결 (윗빛 점)
                px[y * N + x] = c;
            }
        // 가운데서 뻗는 빛나는 금 (세 갈래)
        int[] crack =
        {
            8,8, 8,9, 9,10, 9,11, 10,12,
            6,7, 5,7, 4,6, 3,6, 2,5,
            8,6, 9,5, 10,5, 11,4, 12,3,
            6,9, 5,10, 5,11,
        };
        for (int i = 0; i < crack.Length; i += 2)
        {
            int x = crack[i], y = crack[i + 1];
            if (px[y * N + x].a > 0) px[y * N + x] = new Color32(255, 236, 150, 255);
        }
        return PixelSprite(N, px);
    }

    static Sprite MakeBrackets()
    {
        const int N = 20, L = 6;
        Color32[] px = new Color32[N * N];
        Color32 ink = new Color32(30, 8, 8, 230), lit = new Color32(255, 255, 255, 255);
        // 귀퉁이 「ㄱ」 넷: 두께 2 · 길이 L (가장자리에서 1칸 안쪽), 어두운 테두리를 먼저 사방 1칸 넓게
        for (int pass = 0; pass < 2; pass++)
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    int cx = (x < N / 2 ? x : N - 1 - x) - 1, cy = (y < N / 2 ? y : N - 1 - y) - 1;      // 가장 가까운 귀퉁이 기준
                    int g = pass == 0 ? 1 : 0;
                    bool arm = (cy >= -g && cy <= 1 + g && cx >= -g && cx <= L - 1 + g) || (cx >= -g && cx <= 1 + g && cy >= -g && cy <= L - 1 + g);
                    if (arm) px[y * N + x] = pass == 0 ? ink : lit;
                }
        return PixelSprite(N, px);
    }

    // 두 번 뛰는 심장 박동 (0 ~ 1): 「두-근」 · 0.85초마다
    static float Heartbeat(float t)
    {
        float u = (t / 0.85f) % 1f;
        float a = (u - 0.1f) / 0.07f, b = (u - 0.3f) / 0.08f;
        return Mathf.Exp(-a * a) + 0.65f * Mathf.Exp(-b * b);
    }

    static SpriteRenderer Part(Transform parent, string name, Sprite sprite, int layer, int order)
    {
        SpriteRenderer sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(parent, false);
        sr.sprite = sprite;
        sr.sortingLayerID = layer;
        sr.sortingOrder = order;
        return sr;
    }

    // 스프라이트를 월드 지름 size 로 (부모 크기를 되돌려서)
    static void Fit(SpriteRenderer sr, float size, float parentScale)
    {
        if (sr == null || sr.sprite == null) return;
        float w = Mathf.Max(0.0001f, sr.sprite.bounds.size.x);
        sr.transform.localScale = Vector3.one * (size / w / parentScale);
    }

    // ================================================================= 매 프레임 (위치 · 크기 · 알파만 · 할당 없음)
    void LateUpdate()
    {
        FlushDealt();
        Refresh();
    }

    void Refresh()
    {
        if (body == null) { Open = false; HideMarks(); return; }
        Bounds b = body.bounds;
        float baseR = hitbox != null ? hitbox.BodyWorldRadius : 0f;
        if (baseR <= 0.01f) baseR = 0.25f * (b.size.x + b.size.y);
        BodyRadius = baseR;
        bool flip = body.flipX;
        float now = Time.time;
        float rMul = 1f;
        bool open;
        switch (Kind)
        {
            case WeakKind.Slime:
            {
                bool air = skills != null && skills.airborne;
                if (wasAir && !air) landAt = now;
                wasAir = air;
                open = !air;
                if (now - landAt < 1.5f) rMul = 1.4f;          // 착지 직후 크게 열림
                centers[0] = Frac(b, flip, 0.5f, 0.78f);
                break;
            }
            case WeakKind.Lich:
                if (boss != null && boss.damageTakenMul > 1f) { open = true; rMul = 1.3f; }     // 의식이 깨져 휘청이는 동안
                else open = boss != null && boss.casting;
                centers[0] = Frac(b, flip, 0.82f, 0.86f);
                break;
            case WeakKind.Demon:
            {
                bool casting = boss != null && boss.casting;
                float hp = boss != null ? boss.EnemyHealth / Mathf.Max(1f, boss.setEnemyHP) : 1f;
                if (wasCasting && !casting) breatheUntil = now + (hp < 0.33f ? 1.3f : 1.8f);     // 기술 뒤 「숨 고르기」
                wasCasting = casting;
                open = now < breatheUntil;
                centers[0] = hp < 0.66f ? Frac(b, flip, 0.42f, 0.75f) : Frac(b, flip, 0.6f, 0.55f);
                break;
            }
            case WeakKind.Mirror:
            {
                float t = now - bornAt;
                int slot = (int)(t / 6f) % 3;
                open = t % 6f >= 0.4f;                           // 옮겨 가는 0.4초는 닫힘
                centers[0] = slot == 0 ? Frac(b, flip, 0.35f, 0.72f) : slot == 1 ? Frac(b, flip, 0.62f, 0.5f) : Frac(b, flip, 0.42f, 0.28f);
                break;
            }
            case WeakKind.Developer:
            case WeakKind.Mom:
            {
                open = true;
                float speed = Kind == WeakKind.Mom ? 100f : 80f;
                float a = (now - bornAt) * speed * Mathf.Deg2Rad;
                Vector2 c = b.center;
                float orbit = baseR * 1.1f;
                centers[0] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * orbit;
                centers[1] = c - new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * orbit;
                break;
            }
            default:
                open = true;
                if (enemy != null && enemy.casting) rMul = 1.5f;      // 스킬 예고 중엔 크게
                centers[0] = Frac(b, flip, 0.5f, 0.8f);
                break;
        }
        if (Blocked) open = false;
        float k = Kind == WeakKind.Slime ? 0.42f : 0.32f, min = Kind == WeakKind.Slime ? 0.7f : 0.5f;
        Radius = Mathf.Max(min, baseR * k) * rMul;

        bool visible = open;
        PlayerController p = Hostile.Player;
        if (visible && IsMid && p != null && ((Vector2)p.transform.position - centers[0]).sqrMagnitude > MidDrawRange * MidDrawRange) visible = false;

        if (open && !wasOpen)
        {
            openedAt = now;
            // 닫힘 → 열림: 짧은 차임 (보스마다 1초에 한 번 · 붙은 첫 순간은 빼고)
            if (evaluated && visible && now - lastChime > 1f) { lastChime = now; Hostile.Play("chime", 0.12f, 2f); }
        }
        wasOpen = open;
        evaluated = true;
        Open = open;
        if (visible) Intro();
        Draw(visible, now);
    }

    static Vector2 Frac(Bounds b, bool flip, float fx, float fy)
    {
        if (flip) fx = 1f - fx;
        return new Vector2(Mathf.Lerp(b.min.x, b.max.x, fx), Mathf.Lerp(b.min.y, b.max.y, fy));
    }

    void HideMarks()
    {
        for (int i = 0; i < marks.Length; i++)
            if (marks[i] != null && marks[i].root.gameObject.activeSelf) marks[i].root.gameObject.SetActive(false);
    }

    void Draw(bool visible, float now)
    {
        if (!visible) { HideMarks(); return; }
        float motion = GameSettings.Motion;
        bool cb = GameSettings.ColorBlind;
        float ps = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float pop = now - openedAt < 0.2f ? 1f + 0.35f * (1f - (now - openedAt) / 0.2f) : 1f;
        float beat = Heartbeat(now - bornAt) * motion;                  // 1.2.7~ 숨쉬기(사인) 대신 「두-근」
        bool flash = now < flashUntil;
        float d = Radius * 2f * pop * (1f + 0.06f * beat);
        // 열릴 때 꺾쇠가 바깥(1.9배)에서 0.25초에 조여 들어와 「잠금」 (움직임 줄이기면 바로 제자리)
        float lockT = Mathf.Clamp01((now - openedAt) / 0.25f);
        float lockIn = motion <= 0f ? 1f : Mathf.Lerp(1.9f, 1f, 1f - (1f - lockT) * (1f - lockT));
        float spin = (now - bornAt) * 40f * motion;
        for (int i = 0; i < Count; i++)
        {
            Mark m = marks[i];
            if (m == null) continue;
            if (!m.root.gameObject.activeSelf) m.root.gameObject.SetActive(true);
            m.root.position = new Vector3(centers[i].x, centers[i].y, 0f);
            m.root.rotation = Quaternion.identity;
            // 고리만 천천히 돎 · 꺾쇠 · 결정은 똑바로
            m.ring.transform.localRotation = Quaternion.Euler(0f, 0f, spin);
            m.outline.transform.localRotation = m.ring.transform.localRotation;
            Fit(m.ring, d, ps);
            m.ring.color = flash ? Color.white : cb ? new Color(1f, 1f, 1f, 0.95f) : new Color(1f, 0.82f, 0.32f, 0.95f);
            // 조준 꺾쇠: 고리보다 조금 바깥 · 박동에 맞춰 살짝 조였다 풀림
            Fit(m.brackets, d * 1.3f * lockIn * (1f - 0.05f * beat), ps);
            m.brackets.color = flash ? Color.white : cb ? Color.white : new Color(1f, 0.93f, 0.62f, 1f);
            // 색각 이상 모드: 흰 고리 + 검은 테두리 + 가운데 마름모 (색이 아닌 모양으로) · 꺾쇠는 그대로
            m.outline.enabled = cb;
            m.halo.enabled = !cb;
            if (cb) { Fit(m.outline, d * 1.14f, ps); m.outline.color = new Color(0f, 0f, 0f, 0.9f); }
            if (cb)
            {
                if (m.core.sprite != WhiteSprite) m.core.sprite = WhiteSprite;
                Fit(m.core, Radius * 0.5f, ps);
                m.core.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                m.core.color = Color.white;
            }
            else
            {
                // 금이 간 심장 결정: 박동마다 부풀고, 주변 주황 빛이 함께 번짐
                if (m.core.sprite != CoreSprite) m.core.sprite = CoreSprite;
                Fit(m.core, Radius * (0.95f + 0.22f * beat) * (flash ? 1.15f : 1f), ps);
                m.core.transform.localRotation = Quaternion.identity;
                m.core.color = Color.white;              // 결정은 제 색 그대로 (맞을 때는 커지고 주변 빛이 하얗게)
                Fit(m.halo, d * (1.15f + 0.2f * beat), ps);
                m.halo.color = flash ? new Color(1f, 1f, 0.9f, 0.8f) : new Color(1f, 0.5f, 0.12f, 0.22f + 0.25f * beat);
            }
        }
    }

    // 처음 빈틈이 열렸을 때 한 번 (저장 데이터마다)
    static bool introShown;
    void Intro()
    {
        if (introShown || Time.time - bornAt < 1.5f || Time.timeScale <= 0f || StoryDirector.Playing) return;
        if (GameInput.Auto || GameInput.TrailerRunning || TutorialRun.Active || Application.isBatchMode) return;
        introShown = true;
        if (Prefs.GetInt(IntroKey, 0) == 1) return;
        Prefs.SetInt(IntroKey, 1);
        Prefs.SaveInBackground();
        Notice.Big(Loc.T("weak.intro.title"), Loc.T("weak.intro.sub"), NoticeIcon.Boss, 3.2f, NoticeLevel.Important);
    }

    // 맞았을 때: 고리 번쩍은 늘 새로, 반짝임은 빈틈마다 0.08초에 하나 (연사 · 관통 · 부채꼴이 맞힌 수만큼 이펙트가 켜지지 않게)
    internal void OnHit(Vector2 at)
    {
        float now = Time.time;
        flashUntil = now + 0.12f;
        if (now < sparkleAt) return;
        sparkleAt = now + 0.08f;
        Fx.Spawn("fx_sparkle", at, 1.3f, new Color(1f, 0.78f, 0.35f), 24f);      // 1.2.7~ 금빛 (빈틈 색)
    }

    // ================================================================= 판정
    internal bool Test(in WeakShape s, out Vector2 at)
    {
        at = centers[0];
        for (int i = 0; i < Count; i++)
        {
            Vector2 c = centers[i];
            if (Hits(s, c)) { at = c; return true; }
        }
        return false;
    }

    bool Hits(in WeakShape s, Vector2 c)
    {
        float r = Radius;
        switch (s.kind)
        {
            case WeakShapeKind.Ray:
            {
                // 무한 직선이 아님: 지난 자리 → 지금 자리 + 앞으로 몸 지름 (몸 밖 빈틈은 빈틈 반지름까지만 — 몸 뒤로 돈 버그가 걸리지 않게)
                float ahead = OutsideBody ? r : BodyRadius * 2f;
                Vector2 end = s.b + s.dir * ahead;
                return Hostile.DistanceToSegment(c, s.a, end) <= r + s.width;
            }
            case WeakShapeKind.Segment:
                return Hostile.DistanceToSegment(c, s.a, s.b) <= r + s.width;
            case WeakShapeKind.Point:
                if ((c - s.a).sqrMagnitude <= (r + s.width) * (r + s.width)) return true;
                return PadRayHits(c, r + s.width);      // 패드: 가상 커서가 늘 5칸 앞이라 거리 대신 조준 방향으로
            case WeakShapeKind.Arc:
            {
                Vector2 to = c - s.a;
                float dist = to.magnitude;
                if (dist <= r) return true;
                if (dist - r > s.reach) return false;
                float spread = Mathf.Asin(Mathf.Clamp01(r / dist)) * Mathf.Rad2Deg;
                return Vector2.Angle(s.dir, to) <= s.width + spread;
            }
            case WeakShapeKind.Marked:
                return true;
        }
        return false;
    }

    // 커서가 빈틈 원(반지름 × scale) 안인지 (우체부 「빈틈 지정」)
    //   패드를 오른쪽 스틱으로 직접 조준 중이면 거리 조건 없이 「조준 방향 반직선이 빈틈 원을 지나는가」 (가상 커서는 늘 5칸 앞에 고정)
    public static bool CursorOn(GameObject host, Vector2 cursor, float scale)
    {
        if (host == null || !host.TryGetComponent(out WeakSpot w) || !w.Open) return false;
        float r = w.Radius * scale;
        for (int i = 0; i < w.Count; i++)
            if ((w.centers[i] - cursor).sqrMagnitude <= r * r || PadRayHits(w.centers[i], r)) return true;
        return false;
    }

    // 패드 스틱 직접 조준(보조 조준 아님): 플레이어에서 조준 방향으로 30칸 반직선이 c 를 반지름 r 안으로 지나는가
    const float PadRayLength = 30f;
    static bool PadRayHits(Vector2 c, float r)
    {
        if (!GamePad.Aiming || GamePad.AssistAiming) return false;
        PlayerController p = Hostile.Player;
        if (p == null) return false;
        Vector2 from = p.transform.position;
        return Hostile.DistanceToSegment(c, from, from + GamePad.AimDir * PadRayLength) <= r;
    }

    internal float Cap => MaxHp * CapRate;

    // ================================================================= 판 기록 분모 (붙은 대상에게 준 피해 — 지난번 체력 − 지금 체력)
    void FlushDealt()
    {
        float hp = boss != null ? boss.EnemyHealth : enemy != null ? enemy.EnemyHealth : 0f;
        bool dead = boss != null ? boss.IsDead : enemy == null || enemy.IsDead;
        if (dead) hp = 0f;
        hp = Mathf.Max(0f, hp);
        if (lastHp < 0f)
        {
            if (hp > 0f) lastHp = hp;          // Start 가 체력을 정한 뒤부터
            return;
        }
        float d = lastHp - hp;
        if (d > 0f && RunCounters.Counting) AddDealt(d);
        lastHp = hp;
    }

    // 판 기록을 쓰기 직전: 이번 프레임에 줄어든 체력까지 (마지막 한 대와 기록이 같은 프레임일 때)
    public static void FlushAll()
    {
        for (int i = Active.Count - 1; i >= 0; i--) if (Active[i] != null) Active[i].FlushDealt();
    }

    // 빈틈으로 더한 피해 ÷ 빈틈이 붙은 보스 · 중간 보스에게 준 피해 (둘 다 배율을 곱한 뒤의 실제 값 · 판 저장으로 이어하기도 판 전체)
    public static float Share
    {
        get
        {
            int dealt = RunCounters.Get("weak.dealt");
            return dealt > 0 ? RunCounters.Get("weak.bonus") / (float)dealt : 0f;
        }
    }

    // ================================================================= 보스 붙이기 관리자 (게임 씬에서만)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        Active.Clear();
        introShown = false;
        DamagePopup.NextWeakTarget = null;
        if (s.name != "GameScene") return;
        if (Object.FindFirstObjectByType<WeakSpotManager>() != null) return;
        new GameObject("WeakSpots").AddComponent<WeakSpotManager>();
    }
}

// 0.25초마다 살아 있는 보스에 빈틈을 붙임 (boss.cs 수정 없음 · LiveCount 는 람다라 쓰지 않고 목록을 직접 돎)
public class WeakSpotManager : MonoBehaviour
{
    float next;

    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + 0.25f;
        List<bosss> live = bosss.Live;
        for (int i = 0; i < live.Count; i++)
        {
            bosss b = live[i];
            if (b == null || b.IsDead) continue;
            if (!b.TryGetComponent(out WeakSpot _)) WeakSpot.Attach(b.gameObject);
        }
    }
}

public enum WeakShapeKind : byte { Ray, Segment, Point, Arc, Marked }

// 판정 모양 (할당 없는 값 타입)
//   Ray: a = 지난 자리, b = 지금 자리, dir = 나는 방향, width = 탄 반폭
//   Segment: a → b, width = 반폭 · Point: a, width = 여유 · Arc: a = 원점, dir, width = 반각(도), reach · Marked: 미리 지정함 (편지)
public readonly struct WeakShape
{
    public readonly WeakShapeKind kind;
    public readonly Vector2 a, b, dir;
    public readonly float width, reach, mul;

    WeakShape(WeakShapeKind kind, Vector2 a, Vector2 b, Vector2 dir, float width, float reach, float mul)
    {
        this.kind = kind; this.a = a; this.b = b; this.dir = dir; this.width = width; this.reach = reach; this.mul = mul;
    }

    public static WeakShape Ray(Vector2 from, Vector2 now, Vector2 dir, float halfWidth, float mul) =>
        new WeakShape(WeakShapeKind.Ray, from, now, dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right, halfWidth, 0f, mul);
    public static WeakShape Segment(Vector2 a, Vector2 b, float halfWidth, float mul) => new WeakShape(WeakShapeKind.Segment, a, b, Vector2.zero, halfWidth, 0f, mul);
    public static WeakShape Point(Vector2 at, float slack, float mul) => new WeakShape(WeakShapeKind.Point, at, at, Vector2.zero, slack, 0f, mul);
    public static WeakShape Arc(Vector2 origin, Vector2 dir, float halfDeg, float reach, float mul) =>
        new WeakShape(WeakShapeKind.Arc, origin, origin, dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right, halfDeg, reach, mul);
    public static WeakShape Marked(float mul) => new WeakShape(WeakShapeKind.Marked, Vector2.zero, Vector2.zero, Vector2.zero, 0f, 0f, mul);
}

public static class WeakHit
{
    // ================================================================= 조준 범위: 평타 · 조준 무기 발사 함수 둘레 (using var _ = WeakHit.AimScope(1.75f))
    //   안에서 만든 탄만 빈틈 후보 (Bullet.aimMul) · 명중 콜백 · 코루틴의 덤 공격은 나중 프레임이라 저절로 범위 밖
    static float aimMul;
    public static float ScopeMul => aimMul;
    public static bool InAim => aimMul > 0f;
    // 패드 「스틱을 놓고 RT 만」 보조 조준은 자동 표적이라 제외
    public static bool AimAllowed => !(GamePad.AssistAiming && GamePad.Aiming);

    public readonly struct Scope : System.IDisposable
    {
        readonly float prev;
        public Scope(float mul) { prev = aimMul; aimMul = AimAllowed ? mul : 0f; }
        public void Dispose() => aimMul = prev;
    }

    public static Scope AimScope(float mul) => new Scope(mul);

    static float soundAt = -9f, gaugeAt = -9f;
    // Apply 가 늘린 이번 한 대 (Settle 이 TakeDamage 안에서 마지막 배율 뒤에 상한을 걸고 기록 · End 가 지움)
    static GameObject pendTarget;
    static float pendBase, pendBonus, pendCap;

    // 빈틈에 맞았으면 늘린 피해 (상한은 TakeDamage 안의 Settle 이 최종 피해 기준으로) + 이번 한 대 표시를 켬, 아니면 damage 그대로
    public static float Apply(GameObject target, in WeakShape shape, float damage)
    {
        if (target == null || damage <= 0f || shape.mul <= 1f) return damage;
        if (!target.TryGetComponent(out WeakSpot w) || !w.isActiveAndEnabled || !w.Open) return damage;
        if (!w.Test(shape, out Vector2 at)) return damage;
        float bonus = damage * (shape.mul - 1f);
        if (bonus <= 0f) return damage;
        pendTarget = target;
        pendBase = damage;
        pendBonus = bonus;
        pendCap = w.Cap;

        DamagePopup.NextWeakTarget = target.transform;
        DamagePopup.NextWeakPerfect = shape.mul >= 2f;
        w.OnHit(at);
        float now = Time.time;
        if (now - soundAt >= 0.06f) { soundAt = now; Hostile.Play("weakhit", 0.35f, 1f); }
        // 필살기 게이지: 최대치의 1%, 1초에 한 번 (게이지는 시간으로 차는 설계라 아주 조금만)
        if (now - gaugeAt >= 1f)
        {
            gaugeAt = now;
            PlayerController p = Hostile.Player;
            if (p != null && p.skillGauge != null) p.skillGauge.AddSkillPoint(p.skillGauge.MaxSkillPoint * 0.01f);
        }
        return damage + bonus;
    }

    // bosss · EnermyController.TakeDamage 가 모든 배율(난이도 · 기억 방패/결 · 휘청 · 치명타 · 고유 스킬)을 곱한 뒤 부름
    //   최종 피해를 기본 몫 · 빈틈 몫으로 같은 비율로 나눠, 빈틈 몫을 최대 체력 2.5%(중간 보스 6%)로 자름 → 치명타가 빈틈 상한을 넘기지 않고 영웅마다 같은 기준
    //   명중 수 · 더한 피해(실제로 들어간 값)도 여기서 셈 (무적 · 죽음으로 피해가 안 들어가면 세지 않음)
    public static float Settle(GameObject self, float damage)
    {
        if (pendTarget == null || pendTarget != self) return damage;
        pendTarget = null;
        float raw = pendBase + pendBonus;
        if (raw <= 0f || damage <= 0f) return damage;
        float ratio = damage / raw;
        float bonus = Mathf.Min(pendBonus * ratio, pendCap);
        if (RunCounters.Counting)
        {
            RunCounters.Add("weak.hit");
            WeakSpot.AddBonus(bonus);
        }
        return pendBase * ratio + bonus;
    }

    // TakeDamage 를 부른 바로 뒤 반드시 (무적 · 죽음으로 피해가 안 들어가도 다음 숫자에 빈틈 표시가 새지 않게)
    public static void End()
    {
        pendTarget = null;
        DamagePopup.NextWeakTarget = null;
        DamagePopup.NextWeakPerfect = false;
    }

    // 직접 피해 한 번: 빈틈 판정 → Specials.Damage → End (범위 확인 없음 — 부르는 쪽이 정함)
    public static void Hit(GameObject go, in WeakShape shape, float damage, Vector3 dir, float knock)
    {
        float d = Apply(go, shape, damage);
        Specials.Damage(go, d, dir, knock);
        End();
    }

    // 조준 범위 안이면 빈틈 판정, 아니면 그냥 피해
    public static void Damage(GameObject go, in WeakShape shape, float damage, Vector3 dir, float knock)
    {
        if (InAim) Hit(go, shape, damage, dir, knock);
        else Specials.Damage(go, damage, dir, knock);
    }

}
