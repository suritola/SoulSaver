using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 장착한 스킨을 게임에 입힘 (SkinData). 판정 · 크기는 그대로, 겉모습과 소리만
// 2026-11-04~ 스킨 시청각 전면 개편 (docs/plans/2026-11-04-skin-av-overhaul.md · 사용자 「스킨 FX 는 세게 넣어도 된다」)
//   칸(걸음 · 평타 몸짓 · 피격 · 레벨업 · 대기 · 필살기 시전 · 사망 · 승리 · …)마다 SkinLooks 표의 그림 · 소리 레시피를 냄
//   남기는 규칙 셋: ① 적 탄 · 경고보다 아래 층(GroundFx · Character −1 · Effect 23~38 · 몸 안쪽만 Player) ② 범위 문법(고리 · 원판 · 부채꼴 …) 금지
//                   ③ 성능 — 모두 풀 입자 SkinMote 한 통로(Emit) · 0.05초 창 예산 · 드문 사건은 1초에 1번
//   파일: SkinFx.cs 핵심 · SkinFx.Char.cs 캐릭터 칸 · SkinFx.Effect.cs 이펙트 칸 · SkinFx.Weapon.cs 무기 칸 · SkinFx.Legacy.cs 개편 전 모습
public partial class SkinFx : MonoBehaviour
{
    public static SkinDef CharSkin, WeaponSkin, EffectSkin;
    public static SkinLook CharLook, WeaponLook, EffectLook;
    public static SkinFx Instance { get; private set; }

    // 새 칸을 끄는 경우: 개편 전 비교 스위치 · 자동 봇 배치 측정 (6-5 · 6-6절)
    //   InCapture: 배치 모드 녹화 도구(tools/trailer)가 켬 — 영상엔 새 연출 · 소리가 나와야 함
    public static bool InCapture;
    public static bool Off => VisualRules.Legacy || (Application.isBatchMode && !InCapture);

    PlayerController player;
    SpriteRenderer body;
    SkinBody skinBody;
    Vector3 lastPos;
    float ghostAt, killFxAt, killSoundAt;
    readonly List<Transform> orbs = new List<Transform>();
    GameObject groundGlow;
    float auraFxAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Refresh();
        SceneManager.sceneLoaded += (s, m) =>
        {
            Refresh();
            if (s.name != "GameScene") return;
            PlayerController p = Object.FindFirstObjectByType<PlayerController>();
            if (p != null && p.GetComponent<SkinFx>() == null) p.gameObject.AddComponent<SkinFx>();
        };
    }

    // 장착한 스킨 다시 읽기 (게임 시작 · 상점에서 바꾼 뒤)
    public static void Refresh()
    {
        CharSkin = SkinData.Equipped(SkinKind.Character);
        WeaponSkin = SkinData.Equipped(SkinKind.Weapon);
        EffectSkin = SkinData.Equipped(SkinKind.Effect);
        RefreshLooks();
    }

    // 상점 미리보기처럼 정적 스킨 값을 직접 바꾼 뒤
    public static void RefreshLooks()
    {
        CharLook = SkinLooks.Get(CharSkin);
        WeaponLook = SkinLooks.Get(WeaponSkin);
        EffectLook = SkinLooks.Get(EffectSkin);
    }

    // D3: 영혼 릴레이 교대 끝 — 새 주자의 스킨으로 정적 값 · 오라 · 몸 덧칠 · 소리 준비를 다시 (RelaySwap.Run 7단계)
    //   replaying(기록 재생)이어도 값은 다시 읽음 · 연출 · 소리는 부르는 쪽이 건너뜀
    public static void Rebind()
    {
        Refresh();
        if (Instance != null) Instance.Rebuild();
        if (!Off) SkinAudio.EnsureAll();
    }

    void Start()
    {
        Instance = this;
        player = GetComponent<PlayerController>();
        body = GetComponent<SpriteRenderer>();
        lastPos = transform.position;
        RefreshLooks();
        if (SpecialAbilities.SharedFx != null) SkinAudio.Register(SpecialAbilities.SharedFx);
        if (!Off) SkinAudio.EnsureAll();
        EnermyController.Killed += OnKill;
        PlayerController.UltUsed += OnUlt;
        PlayerController.AttackFired += OnAttackFired;
        LastStand.Fired += OnLastStand;
        StageManager.Cleared += OnCleared;
        SpecialAbilities.Evolved += OnEvolved;
        Rebuild();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        EnermyController.Killed -= OnKill;
        PlayerController.UltUsed -= OnUlt;
        PlayerController.AttackFired -= OnAttackFired;
        LastStand.Fired -= OnLastStand;
        StageManager.Cleared -= OnCleared;
        SpecialAbilities.Evolved -= OnEvolved;
        ClearAura();
    }

    // 오라 · 몸 덧칠 · 칸 시계를 지금 스킨으로 (Start · 릴레이 교대)
    void Rebuild()
    {
        ClearAura();
        if (skinBody != null) skinBody.Clear();
        if (body != null && !Off && CharLook != null) skinBody = SkinBody.For(body);
        crisisOn = false;
        stepAcc = stillFor = moveFor = 0f;
        idleAt = Time.time + 3f;
        auraFxAt = 0f;
        slotAt.Clear();
        if (CharSkin != null && CharSkin.tier >= 3) BuildAura();
    }

    void ClearAura()
    {
        // 오라 구슬 · 발밑 빛은 몸 밖 오브젝트라 같이 지움 (상점 미리보기 무대를 닫을 때 · 릴레이 교대 때 남던 것)
        foreach (Transform o in orbs) if (o != null) Destroy(o.gameObject);
        orbs.Clear();
        if (groundGlow != null) Destroy(groundGlow);
        groundGlow = null;
    }

    void BuildAura()
    {
        Sprite glow = SpecialAbilities.GlowSprite;
        if (glow == null) return;
        Color c = CharSkin.color;
        float orbAlpha = VisualRules.Legacy ? 1f : 0.7f;
        int count = 3;
        switch (CharSkin.id)
        {
            case "cartographer__blank": count = 2; break;
            case "clockmaker__stopped": count = 4; break;
            case "ghost__deleted": count = 4; break;
            case "wrestler__referee": count = 2; break;
            case "foreman__beta": count = 4; break;
            case "puppeteer__devhand": count = 3; break;
            case "undertaker__todo": count = 2; break;
            case "photographer__f12": count = 4; break;
        }
        for (int i = 0; i < count; i++)
        {
            Color oc = c;
            if (CharSkin.id == "ghost__deleted" && i % 2 == 1) oc = new Color(0.2f, 0.95f, 0.95f);
            if (CharSkin.id == "foreman__beta") oc = i % 2 == 0 ? new Color(0.7f, 0.72f, 0.78f) : new Color(0.3f, 0.5f, 0.95f);
            if (CharSkin.id == "puppeteer__devhand") oc = i % 2 == 0 ? new Color(0.95f, 0.97f, 1f) : new Color(0.25f, 0.5f, 0.95f);
            if (CharSkin.id == "undertaker__todo") oc = i == 0 ? new Color(0.78f, 0.78f, 0.82f) : new Color(0.95f, 0.85f, 0.35f);
            if (CharSkin.id == "photographer__f12") oc = i % 2 == 0 ? new Color(0.25f, 0.95f, 0.95f) : new Color(0.95f, 0.97f, 1f);
            if (CharSkin.id == "wrestler__referee") oc = i == 0 ? new Color(0.97f, 0.97f, 1f) : new Color(0.15f, 0.14f, 0.2f);
            if (CharSkin.id == "clockmaker__stopped") oc = i == 0 ? new Color(0.85f, 0.15f, 0.2f) : new Color(0.85f, 0.88f, 0.9f);
            GameObject o = SpecialAbilities.MakeSprite("SkinAura", glow, transform.position, 0.13f, new Color(oc.r, oc.g, oc.b, orbAlpha), "Effect", 2);
            orbs.Add(o.transform);
        }
        Color gc = c;
        if (CharSkin.id == "cartographer__blank") gc = new Color(0.8f, 0.88f, 0.95f);
        if (CharSkin.id == "clockmaker__stopped") gc = new Color(0.75f, 0.78f, 0.82f);
        if (CharSkin.id == "wrestler__referee") gc = new Color(0.91f, 0.76f, 0.48f);
        if (CharSkin.id == "foreman__beta") gc = new Color(0.7f, 0.72f, 0.78f);
        if (CharSkin.id == "puppeteer__devhand") gc = new Color(0.55f, 0.6f, 0.72f);
        if (CharSkin.id == "undertaker__todo") gc = new Color(0.8f, 0.8f, 0.85f);
        if (CharSkin.id == "photographer__f12") gc = new Color(0.25f, 0.9f, 0.9f);
        groundGlow = SpecialAbilities.MakeSprite("SkinGround", glow, transform.position, 0.5f, new Color(gc.r, gc.g, gc.b, 0.42f), "Background", 50);
    }

    void Update()
    {
        if (Time.timeScale == 0f || body == null) return;
        Vector3 p = transform.position;
        Vector3 delta = p - lastPos;
        bool moving = delta.sqrMagnitude > 0.0004f;
        lastPos = p;
        // 영웅 이상: 움직일 때 몸 잔상 (영웅 0.09초 · 전설 0.07초 — 2-4절)
        bool legend = CharSkin != null && CharSkin.tier >= 3;
        if (CharSkin != null && CharSkin.tier >= 2 && moving && Time.time >= ghostAt && body.enabled && body.sprite != null)
        {
            ghostAt = Time.time + (legend && !Off ? 0.07f : 0.09f);
            if (!(GraphicsQuality.Low && (++ghostTurn & 1) == 0))
            {
                Color gcol = !Off && CharLook != null ? CharLook.main : CharSkin.color;
                FadeSprite f = FadeSprite.Spawn("SkinGhost", body.sprite, p, 1f, new Color(gcol.r, gcol.g, gcol.b, Off ? 0.6f : 0.55f), "Character", -1, legend && !Off ? 0.5f : 0.45f);
                Vector3 ls = transform.lossyScale;
                if (body.flipX) ls.x = -ls.x;
                f.Shape(Quaternion.identity, ls);
            }
        }
        // 전설: 오라 (작은 빛 셋이 천천히 돎)
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null) continue;
            float a = Time.time * 1.6f + i * Mathf.PI * 2f / orbs.Count;
            orbs[i].position = p + new Vector3(Mathf.Cos(a) * 1.5f, Mathf.Sin(a) * 0.75f + 0.6f, 0f);
            float pulse = 1f + 0.25f * Mathf.Sin(Time.time * 5f + i);
            orbs[i].localScale = Vector3.one * 0.13f * pulse;
        }
        if (groundGlow != null)
        {
            groundGlow.transform.position = p + new Vector3(0f, -0.9f, 0f);
            groundGlow.transform.localScale = Vector3.one * 0.5f * (1f + 0.08f * Mathf.Sin(Time.time * 3f));
        }
        if (Off)
        {
            if (legend && Time.time >= auraFxAt) LegacyAuraAmbient(p);
            return;
        }
        CharUpdate(p, delta, moving);
    }
    int ghostTurn;

    // 진행 경로(사망 · 레벨업 · 처치 · 명중 · 장전 …)에서 불리는 진입점은 예외를 여기서 삼킴 — 스킨 연출이 게임 진행을 막지 않게 (1.2.7 · 1.5.9 사고 교훈)
    static int guardLogs;
    static void GuardFail(System.Exception e)
    {
        if (guardLogs++ < 5) Debug.LogException(e);
    }

    // ================================================================= 그림 한 통로 (Emit)
    // 예산 칸 (2-4절): 반복 사건은 0.05초 창의 앞 몫만, 드문 사건은 사건마다 ≤32 (같은 칸은 1초에 1번 — 부르는 쪽 SlotReady)
    public enum Bud { Rep = 5, Kill = 8, Big = 10, Rare = 32 }
    static float budWindow = -1f;
    static int budCount;

    static int Allow(Bud b, int want)
    {
        if (b == Bud.Rare) return Mathf.Min(want, GraphicsQuality.Low ? 16 : 32);
        float now = Time.unscaledTime;
        if (now - budWindow >= 0.05f || now < budWindow) { budWindow = now; budCount = 0; }
        int cap = (int)b;
        if (GraphicsQuality.Low) cap = Mathf.Max(1, cap / 2);
        int got = Mathf.Clamp(cap - budCount, 0, want);
        budCount += got;
        return got;
    }

    // 같은 칸은 간격 안에 한 번만 (드문 사건 1초 · 평타 몸짓 0.12초 …)
    readonly Dictionary<string, float> slotAt = new Dictionary<string, float>();
    bool SlotReady(string slot, float gap)
    {
        if (slotAt.TryGetValue(slot, out float t) && Time.unscaledTime < t) return false;
        slotAt[slot] = Time.unscaledTime + gap;
        return true;
    }

    // 몸 기준점: 발 · 머리 · 가운데 (몸 그림 경계로 — 상점 무대에서도 같음)
    Vector3 BodyAnchor(char at, Vector3 hand)
    {
        if (body == null || body.sprite == null) return transform.position + (at == 'f' ? Vector3.down * 0.9f : at == 'h' ? Vector3.up * 1f : Vector3.zero);
        Bounds b = body.bounds;
        switch (at)
        {
            case 'f': return new Vector3(b.center.x, b.min.y + 0.12f, 0f);
            case 'h': return new Vector3(b.center.x, b.max.y, 0f);
            case 'n': return hand;
            default: return new Vector3(b.center.x, b.center.y, 0f);
        }
    }

    static Color CueColor(Cue c, SkinLook look)
    {
        Color main = look != null ? look.main : Color.white, acc = look != null ? look.accent : Color.white;
        Color col = c.cc switch
        {
            'a' => acc,
            'w' => Color.white,
            'k' => new Color(0.12f, 0.1f, 0.14f),
            'r' => Color.HSVToRGB(Random.value, 0.5f, 1f),
            'c' => c.custom,
            _ => main,
        };
        col.a = Mathf.Clamp01(c.al) * GameSettings.MyFxAlpha;
        return col;
    }

    // 바닥 흔적에 적대색(#FF4033 계열) 금지 (L8) — 붉은 스킨의 발자국은 어두운 중립색
    static Color Neutral(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        if ((h < 0.05f || h > 0.93f) && s > 0.45f && v > 0.5f) return new Color(0.2f, 0.18f, 0.2f, c.a);
        return c;
    }

    Vector2 moveDir = Vector2.up;
    bool stepSide;

    // 레시피 하나를 그림 (onBody: 몸 기준점 · 아니면 at 그 자리) — 반환: 그린 입자 수
    public int Emit(string recipe, SkinLook look, Vector3 at, Vector2 dir, bool onBody, int order, Bud bud, bool unscaled = false)
    {
        Cue[] cues = SkinLooks.Parse(recipe);
        if (cues == null) return 0;
        int drawn = 0;
        foreach (Cue c in cues) drawn += EmitOne(c, look, at, dir, onBody, order, bud, unscaled);
        return drawn;
    }

    int EmitOne(Cue c, SkinLook look, Vector3 at, Vector2 dir, bool onBody, int order, Bud bud, bool unscaled)
    {
        if (c.m == CueMotion.Tint)
        {
            if (skinBody != null) skinBody.Tint(CueColor(c, look), c.al, c.life);
            return 0;
        }
        int n = Allow(bud, c.n);
        if (n <= 0) return 0;
        Vector3 basePos = onBody ? BodyAnchor(c.at, at) : at + (c.at == 'h' ? Vector3.up * 1f : c.at == 'f' ? Vector3.down * 0.4f : Vector3.zero);
        basePos += new Vector3(c.x, c.y, 0f);
        float R = c.r >= 0f ? c.r : 1.2f, H = c.h >= 0f ? c.h : 3f;
        string layer = "Effect";
        bool bodyClone = c.m == CueMotion.Freeze || c.m == CueMotion.Shrink || c.m == CueMotion.Ghost;
        Sprite[] anim = null;
        Sprite sp = bodyClone ? (body != null ? body.sprite : null) : SpriteOf(c.glyph, out anim);
        if (sp == null) return 0;
        Vector2 d = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        int made = 0;
        for (int i = 0; i < n; i++)
        {
            Color col = CueColor(c, look);
            float sz = c.size * Random.Range(0.85f, 1.15f);
            SkinMote m = null;
            switch (c.m)
            {
                case CueMotion.Mark:
                    {
                        Vector2 md = moveDir;
                        Vector3 side = new Vector3(-md.y, md.x, 0f) * 0.18f * (stepSide ? 1f : -1f);
                        float ang = Mathf.Atan2(md.y, md.x) * Mathf.Rad2Deg - 90f;
                        m = SkinMote.Make(sp, basePos + side + (Vector3)(Random.insideUnitCircle * 0.05f * i), c.size, Neutral(col), VisualRules.GroundFx, 6, c.life, unscaled);
                        m?.Spin(0f, ang + c.rot).Hold(0.6f);
                        break;
                    }
                case CueMotion.Burst:
                    {
                        Vector2 v = Random.insideUnitCircle.normalized * R / c.life * Random.Range(0.6f, 1.2f) * c.v;
                        m = SkinMote.Make(sp, basePos, sz, col, layer, order, c.life * Random.Range(0.85f, 1.1f), unscaled);
                        m?.Move(v + Vector2.up * (c.g > 0f ? c.g * c.life * 0.35f : 0f), c.g, 1.2f).Spin(c.sp != 0f ? c.sp * Random.Range(0.6f, 1.2f) * (Random.value < 0.5f ? -1f : 1f) : Random.Range(-220f, 220f), Random.Range(0f, 360f)).Grow(1f, 0.6f);
                        break;
                    }
                case CueMotion.Puff:
                    m = SkinMote.Make(sp, basePos + (Vector3)(Random.insideUnitCircle * R * 0.3f), sz, col, layer, order, c.life, unscaled);
                    m?.Move(Random.insideUnitCircle * R / c.life * 0.6f + Vector2.up * 0.4f * c.v, 0f, 2f).Grow(0.6f, 1.5f).Hold(0.3f).Spin(Random.Range(-60f, 60f), Random.Range(0f, 360f));
                    break;
                case CueMotion.Rise:
                    m = SkinMote.Make(sp, basePos + (Vector3)(Random.insideUnitCircle * R), sz, col, layer, order, c.life * Random.Range(0.8f, 1.1f), unscaled);
                    m?.Move(Vector3.up * H / c.life * c.v * Random.Range(0.7f, 1.1f)).Sway(0.15f, 6f).Grow(1f, 0.7f).Spin(c.sp, 0f);
                    break;
                case CueMotion.Column:
                    m = SkinMote.Make(sp, basePos + Vector3.up * (i / (float)n) * H * 0.25f, sz, col, layer, order, c.life, unscaled);
                    m?.Helix(R, 7f * c.v * (i % 2 == 0 ? 1f : -1f), H / c.life, i * 6.283f / n + Random.value * 0.6f).Grow(1f, 0.6f).Spin(c.sp, Random.Range(0f, 360f));
                    break;
                case CueMotion.Fall:
                    m = SkinMote.Make(sp, basePos + new Vector3(Random.Range(-R, R), H * Random.Range(0.8f, 1.1f), 0f), sz, col, layer, order, c.life * Random.Range(0.8f, 1.1f), unscaled);
                    m?.Move(Vector3.down * H / c.life * 0.9f * c.v).Sway(0.25f, 4f).Spin(c.sp != 0f ? c.sp : Random.Range(-120f, 120f), Random.Range(0f, 360f)).Hold(0.7f);
                    break;
                case CueMotion.Fountain:
                    {
                        float up = 2.2f * H / c.life * c.v;
                        m = SkinMote.Make(sp, basePos, sz, col, layer, order, c.life * Random.Range(0.85f, 1.1f), unscaled);
                        m?.Move(new Vector3(Random.Range(-R, R) / c.life, up * Random.Range(0.75f, 1.05f), 0f), (c.g > 0f ? c.g : 2f * up / (c.life * 0.9f))).Spin(Random.Range(-200f, 200f), Random.Range(0f, 360f)).Hold(0.7f);
                        break;
                    }
                case CueMotion.Roll:
                    {
                        float s = Random.value < 0.5f ? -1f : 1f;
                        m = SkinMote.Make(sp, basePos + new Vector3(-R * s, 0f, 0f), sz, col, layer, order, c.life, unscaled);
                        m?.Move(new Vector3(2f * R / c.life * s, 0f, 0f)).Spin(-(c.sp != 0f ? c.sp : 400f) * s, 0f).Hold(0.75f);
                        break;
                    }
                case CueMotion.Pop:
                    {
                        float g = c.g > 0f ? c.g : 20f, up = Mathf.Sqrt(2f * g * H);
                        m = SkinMote.Make(sp, basePos, sz, col, layer, order, c.life, unscaled);
                        m?.Move(new Vector3(Random.Range(-0.6f, 0.6f) * R, up, 0f), g).Spin(c.sp != 0f ? c.sp : Random.Range(-90f, 90f), 0f).Hold(0.8f);
                        break;
                    }
                case CueMotion.Spin:
                    m = SkinMote.Make(sp, basePos + new Vector3((i - (n - 1) * 0.5f) * R, 0f, 0f), c.size, col, layer, order, c.life, unscaled);
                    m?.Spin(c.sp != 0f ? c.sp : 180f, c.rot).Grow(0.5f, 1f).Hold(0.7f);
                    break;
                case CueMotion.Float:
                    m = SkinMote.Make(sp, basePos + new Vector3((i - (n - 1) * 0.5f) * R, 0f, 0f), c.size, col, layer, order, c.life, unscaled);
                    m?.Move(Vector3.up * 0.35f * c.v).Sway(0.08f, 3f).Grow(0.8f, 1.05f).Hold(0.7f).Spin(c.sp, c.rot);
                    break;
                case CueMotion.Flash:
                    {
                        if (!GameSettings.Flashes) col.a *= 0.3f;
                        m = SkinMote.Make(sp, basePos + (Vector3)(Random.insideUnitCircle * R * (i > 0 ? 1f : 0f)), c.size, col, layer, order, c.life, unscaled);
                        m?.Grow(0.6f, 1.3f).Hold(0.2f);
                        break;
                    }
                case CueMotion.Beam:
                    {
                        float sign = i % 2 == 0 ? 1f : -1f;
                        float rot = c.rot * sign;
                        Vector3 foot = basePos + new Vector3((i - (n - 1) * 0.5f) * R, 0f, 0f);
                        Vector3 mid = foot + Quaternion.Euler(0f, 0f, rot) * Vector3.up * H * 0.5f;
                        m = SkinMote.Make(sp, mid, c.size, col, layer, order, c.life, unscaled);
                        m?.Stretch(H / Mathf.Max(0.05f, c.size)).Spin(c.sp * sign, rot).Hold(0.6f);
                        break;
                    }
                case CueMotion.Spray:
                    {
                        Vector2 dd = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f)) * (d * c.dir);
                        m = SkinMote.Make(sp, basePos, sz, col, layer, order, c.life * Random.Range(0.8f, 1.1f), unscaled);
                        m?.Move(dd * R / c.life * Random.Range(0.6f, 1.1f) * c.v, c.g, 2f).Spin(Random.Range(-200f, 200f), Random.Range(0f, 360f)).Grow(1f, 0.7f);
                        break;
                    }
                case CueMotion.Absorb:
                    m = SkinMote.Make(sp, basePos + (Vector3)(Random.insideUnitCircle * R * 0.4f), sz, col, layer, order, c.life * Random.Range(0.9f, 1.15f), unscaled);
                    if (m != null) { m.Absorb(transform, Vector3.up * 0.3f, Random.Range(0.6f, 1.4f)).Spin(Random.Range(-200f, 200f), 0f).Grow(1f, 0.5f).Hold(0.85f); }
                    break;
                case CueMotion.Freeze:
                case CueMotion.Shrink:
                case CueMotion.Ghost:
                    {
                        if (body == null) break;
                        Vector3 ls = transform.lossyScale;
                        float h = sp.bounds.size.y * Mathf.Abs(ls.y);
                        bool flip = body.flipX ^ (ls.x < 0f);
                        bool ghost = c.m == CueMotion.Ghost;
                        if (ghost) col.a = Mathf.Min(col.a, 0.55f);       // L6: 몸 복제는 알파 ≤0.55
                        m = SkinMote.Make(sp, transform.position + new Vector3(c.x, c.y, 0f) * (ghost ? 1f : 0f), h, col,
                                          ghost ? "Character" : SortingLayer.IDToName(body.sortingLayerID), ghost ? -1 : body.sortingOrder + 2, c.life, unscaled, !ghost);
                        if (m == null) break;
                        m.Flip(flip).Hold(c.m == CueMotion.Freeze ? 0.7f : 0.3f);
                        if (c.m == CueMotion.Shrink) m.Grow(1f, 0.05f);      // 회전 없음 — Player 층 몸 복제가 실루엣 밖으로 나가지 않게
                        if (ghost) m.Move(new Vector3(c.x, c.y, 0f) / c.life);
                        break;
                    }
            }
            if (m == null) continue;
            if (anim != null && !bodyClone) m.Anim(anim, anim.Length / Mathf.Max(0.05f, c.life * 0.8f));
            made++;
        }
        return made;
    }

    // 그림 이름 → 스프라이트 (기존 Fx 시트면 프레임 묶음도)
    static Sprite SpriteOf(string glyph, out Sprite[] frames)
    {
        frames = null;
        if (glyph != null && glyph.StartsWith("fx_"))
        {
            Sprite[] f = Fx.Frames(glyph);
            if (f == null || f.Length == 0) return SkinArt.Get("dot");
            if (f.Length > 1) frames = f;
            return f[0];
        }
        return SkinArt.Get(glyph);
    }

    // 칸 그림 + 소리를 한 번에 (스킨 · 표가 없으면 아무것도 안 함) — 그림을 그렸거나 소리를 냈으면 true
    bool Slot(SkinDef s, SkinLook look, string slot, Vector3 at, Vector2 dir, bool onBody, int order, Bud bud, float vol, float pitchPct, SkinAudio.Prio prio, bool unscaled = false)
    {
        if (look == null) return false;
        bool any = false;
        string fx = look.Fx(slot);
        if (fx != null) any |= Emit(fx, look, at, dir, onBody, order, bud, unscaled) > 0;
        if (look.Sfx(slot) != null && !RunSave.Replaying) any |= SkinAudio.Play(s, slot, vol, pitchPct, prio);
        return any;
    }

    // ================================================================= 게임 코드가 부르는 도우미
    // 명중 불꽃 색 (Bullet)
    public static Color HitColor(Color baseColor)
    {
        if (EffectSkin == null) return baseColor;
        return EffectSkin.id == "skin_starlight" ? Color.HSVToRGB((Time.time * 0.8f) % 1f, 0.45f, 1f) : EffectSkin.color;
    }

    // 코인 반짝임 색 (Juice)
    public static Color CoinColor(Color baseColor) => EffectSkin == null ? baseColor
        : EffectSkin.id == "skin_starlight" ? Color.HSVToRGB(Random.value, 0.4f, 1f) : EffectSkin.color;

    // 기본 처치 효과(영혼 폭발) 색: 이펙트 스킨이 있으면 스킨 색 (Juice)
    public static Color KillColor(Color stageColor)
    {
        if (EffectSkin == null) return stageColor;
        return EffectSkin.id == "skin_starlight" ? Color.HSVToRGB(Random.value, 0.4f, 1f) : EffectSkin.color;
    }

    // 평타 이펙트 색 (검 베기 · 플라스크 폭발): 무기 스킨 색으로 물들임
    public static Color Tint(Color c) => WeaponSkin == null ? c : Color.Lerp(c, new Color(WeaponSkin.color.r, WeaponSkin.color.g, WeaponSkin.color.b, c.a), 0.6f);

    // 사진사 셔터 번쩍임 색 (설명 약속 — 신문 기자 「조금 노랗게」 · 유령 사진가 「보라」): 무기 스킨이 없을 때 캐릭터 스킨 표의 flash 색
    public static Color FlashTint(Color c)
    {
        if (Off) return c;
        string hex = ObjectLook("flash");
        if (hex == null || !ColorUtility.TryParseHtmlString(hex, out Color t)) return c;
        return new Color(Mathf.Lerp(c.r, t.r, 0.7f), Mathf.Lerp(c.g, t.g, 0.7f), Mathf.Lerp(c.b, t.b, 0.7f), c.a);
    }

    // 키트 물체(더미 · 묘비 · 사본 · 구멍) 겉모습 (6-1절: 무기 스킨 > 캐릭터 스킨 > 기본) — 그 칸 이름의 값 (없으면 null)
    public static string ObjectLook(string slot)
    {
        if (Off) return null;
        if (WeaponSkin != null) return WeaponLook?.Fx(slot);       // 무기 스킨을 끼면 캐릭터 스킨 물체 칸은 꺼짐
        return CharLook?.Fx(slot);
    }
}

// 총알 · 투사체 색: 만들어진 뒤(색이 다 정해진 다음) 무기 스킨 색으로 물들임
public class SkinTint : MonoBehaviour
{
    bool done;

    void Start() => ApplyNow();

    // 다시 쓰는 총알(풀링)은 Start 보다 BulletGlow 꾸미기가 먼저 올 수 있어 거기서도 부름 (한 번만)
    public void ApplyNow()
    {
        if (done) return;
        done = true;
        if (SkinFx.WeaponSkin == null) { Destroy(this); return; }
        if (TryGetComponent(out SpriteRenderer sr) && sr.enabled) sr.color = SkinFx.Tint(sr.color);
        Destroy(this);
    }
}
