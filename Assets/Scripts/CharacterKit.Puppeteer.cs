using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 인형사 (2.0.0): 줄 끝의 연출가 — docs/plans/2026-11-02-big-update/hero-puppeteer.md
//   평타 · 휘두름: 더미가 커서 자리(영웅에게서 1.2 ~ 9칸)에 떠서 따라오고, 누르면 그 자리에서 팔을 돌려 반지름 1.7칸을 벰
//   줄: 영웅 조종대 ↔ 더미를 잇는 선분이 칼날 — 적이 줄을 서로 지나칠 때(부호가 바뀔 때) 또는 큰 적 몸에 걸쳐 있을 때 0.45초에 한 번 벰 (공격력 15% ~ 35%, 팽팽할수록 아픔)
//   우클릭 짧게: 필살기 게이지 20% 로 영웅 ↔ 더미 자리 바꾸기 (가르기 120% · 남은 더미가 1초 미끼) / 게이지 가득 + 꾹: 커튼콜 (CharacterKit.Puppeteer.Ult)
//   상태는 「끝나는 시각」 값만 (코루틴 켜고 끄는 깃발 없음) · 더미는 저장하지 않음 (이어하면 새로 만듦)
// 레벨업 카드 (card): 0 실 자국 · 1 팔 벌린 인형 · 2 끊어진 실 · 3 빈자리의 인형 · 5 짧은 막간 · 6 관객의 박수 / 형태: 가위 손 · 실감개 · 태엽 병정 / 강화: 대역 배우 · 두 번째 실 · 무거운 머리
// 갈래 5개 (팽팽한 줄 · 호위 인형 · 바꿔치기 · 거울 인형 · 꼭두각시)의 효과는 CharacterKit.Puppeteer.Paths.cs — 여기서는 값 몇 곳에서 그쪽 PupPath* 를 부름
// 간소화: 사막 비밀은 범위 밖
public partial class CharacterKit
{
    static readonly Color PupSky = new Color(0.62f, 0.72f, 1f);        // 연보라빛 하늘 #9FB8FF (아군색 · 줄)

    const float PupMinLen = 1.2f, PupBaseLen = 9f, PupFollowTime = 0.06f, PupFollowSpeed = 34f;
    const float PupSwingR0 = 1.7f, PupHalfW = 0.35f, PupSlack = 2f, PupCutGap = 0.45f;
    const float PupSwapGap = 1.5f, PupSwapCut = 1.2f, PupDecoyBase = 1f, PupSwapInvuln = 0.2f;

    // ---------------- 더미 (늘 떠 있음)
    GameObject pupRoot;
    SpriteRenderer pupBody, pupShadow;
    Transform pupBodyTf;
    Sprite[] pupFrames;
    int pupFramesForm = -2;
    Vector2 pupPos, pupVel, pupHold, pupPadDir = Vector2.right;
    Vector2 pupRaw;                     // 커서 원값 (줄 길이로 자르기 전)
    float pupLockUntil, pupSwingAt = -9f, pupSwapNextAt, pupLastSwingAt, pupAutoAt, pupBuzzAt, pupSwapAt = -9f, pupFlashAt = -9f;
    bool pupMade, pupLocked;
    LineRenderer pupLine, pupLine2, pupRing, pupGuide;
    PixelMarks pupPx;          // 2.0.0~ 줄이 모자랄 때 안내 도트 점선 (안 그린 프레임엔 저절로 숨음)
    readonly List<LineRenderer> pupTraceLines = new List<LineRenderer>(24);

    // ---------------- 줄 판정 기억 (적의 지난 쪽 부호 · 다시 벨 시각) — 2초마다 정리
    struct PupSideMemo { public sbyte side; public int frame; }
    // 줄 가닥별 기억: 0 줄 · 1 두 번째 실(강화) · 2 팽팽한 줄 극의 가닥 · 3 거울 더미 줄 (실 자국은 같은 열쇠 공간의 7)
    readonly Dictionary<int, PupSideMemo>[] pupSide = { new Dictionary<int, PupSideMemo>(), new Dictionary<int, PupSideMemo>(), new Dictionary<int, PupSideMemo>(), new Dictionary<int, PupSideMemo>() };
    readonly Dictionary<int, float> pupCutAt = new Dictionary<int, float>();
    readonly List<float> pupCutTimes = new List<float>(8);
    float pupPruneAt, pupSweepCool, pupCutPitchAt, pupCutPitch = 1f;
    bool pupSwinging, pupCutting, pupThreading;      // pupThreading: 2.0.0~ 검토 — 끊어진 실이 도는 동안 (실로 죽인 적에서 또 튀는 재귀를 막음)
    int pupCutsThisFrame;

    // ---------------- 실 자국 (카드 0) — 지나간 줄이 잠시 남음
    struct PupTrace { public Vector2 a, b; public float until; }
    readonly List<PupTrace> pupTraces = new List<PupTrace>(24);
    float pupTraceDropAt, pupTraceCheckAt;

    // ---------------- 밖에서 읽는 값
    public Vector2 PupPos => pupPos;
    public bool PupIsLocked => pupLocked;
    public float PupRestTime => Time.time - pupLastSwingAt;
    public float PupSinceSwap => Time.time - pupSwapAt;
    public float PupDistance => Id == CharacterId.Puppeteer ? Vector2.Distance(pupPos, transform.position) : 0f;
    public float PupStringLen => Mathf.Min(PupDistance, PupMaxLen);
    // 커서를 줄 길이보다 얼마나 멀리 끌었는지 (실 퉁기기 · 패드는 스틱을 끝까지 밀면 1)
    public float PupOverstretch
    {
        get
        {
            if (Id != CharacterId.Puppeteer) return 0f;
            if (GamePad.Active && !GameInput.Auto) return GamePad.RightStick.magnitude > 0.97f ? 1.5f : 0f;
            return Mathf.Max(0f, Vector2.Distance(pupRaw, transform.position) - PupMaxLen);
        }
    }
    public float PupTaut => PupTautFor(PupStringLen, true);
    float PupTautFor(float len, bool tautPath)
    {
        float t = Mathf.Clamp01((len - PupSlack) / (PupMaxLen - PupSlack));
        return tautPath && PupOn(PuppeteerPaths.Taut) ? Mathf.Max(t, PuppeteerPaths.TautMinTension) : t;      // 팽팽한 줄: 늘 팽팽 (하한 0.5)
    }

    // 최대 줄 길이 (긴 팔 패시브 +1 · 진화 +2)
    public float PupMaxLen => PupPathBaseLen + (Special != null && Special.Has(SpecialAbilities.KitLongArm) ? (Special.IsEvolved(SpecialAbilities.KitLongArm) ? 2f : 1f) : 0f);
    // 휘두름 반지름: 팔 벌린 인형 (+20% / 단계) · 무거운 머리 +30%
    float PupSwingR => PupSwingR0 * (1f + 0.2f * card[1]) * (augment == AugHeavyHead ? 1.3f : 1f);
    // 자리 바꾸기 게이지 비용: 짧은 막간 (20% → 17 / 14 / 11% · 4단계 8%)
    public float PupSwapCost => Mathf.Max(0.08f, (PupOn(PuppeteerPaths.Swap) ? PuppeteerPaths.SwapPathCost : 0.2f) - 0.03f * card[5]);
    // 남은 더미가 끄는 시간: 빈자리의 인형 (1.4 / 1.8 / 2.2초 = 1 + 0.4 × 단계)
    float PupDecoySec => PupDecoyBase + 0.4f * card[3];
    public int PupSwapsLeft
    {
        get
        {
            SkillGauge g = player != null ? player.skillGauge : null;
            if (g == null || g.MaxSkillPoint <= 0) return 0;
            return Mathf.Clamp(Mathf.FloorToInt(g.SkillPoint / (g.MaxSkillPoint * PupSwapCost) + 0.001f), 0, 12);
        }
    }

    static bool PupEvo(string key) => SignatureSkills.PupEvo(key);

    // ================================================================= 더미 만들기
    void PupEnsure()
    {
        if (pupMade && pupRoot != null) return;
        pupMade = true;
        pupRoot = new GameObject("Puppet");
        pupRoot.transform.position = transform.position + Vector3.right * 2.5f;
        GameObject b = new GameObject("Body");
        b.transform.SetParent(pupRoot.transform, false);
        pupBody = b.AddComponent<SpriteRenderer>();
        pupBodyTf = b.transform;
        pupBody.sortingLayerID = PlayerLook.TopLayer;
        pupBody.sortingOrder = 30;
        GameObject sh = new GameObject("Shadow");
        sh.transform.SetParent(pupRoot.transform, false);
        pupShadow = sh.AddComponent<SpriteRenderer>();
        pupShadow.sprite = Hostile.Glow;
        pupShadow.color = new Color(0f, 0f, 0f, 0.35f);
        pupShadow.sortingLayerID = PlayerLook.TopLayer;
        pupShadow.sortingOrder = 28;
        sh.transform.localScale = new Vector3(1.3f, 0.5f, 1f);
        pupPos = (Vector2)transform.position + Vector2.right * 2.5f;
        pupLastSwingAt = Time.time;
        PupLoadFrames();
    }

    // 2026-11-04~ 스킨 연출(SkinFx)이 쓰는 더미 자리 — 휘두를 때 솜 · 나무 조각 · 뼈 달그락이 영웅 손이 아니라 더미에서 (더미가 없으면 false)
    public bool PupDummyAt(out Vector3 p)
    {
        p = default;
        if (Id != CharacterId.Puppeteer || pupRoot == null || !pupRoot.activeSelf || pupBodyTf == null) return false;
        p = pupBodyTf.position;
        return true;
    }

    // 2026-11-04~ 스킨 설명 약속 (그림자극 「더미도 그림자 인형」): 더미 그림 색만 — 그림 · 크기 · 판정은 그대로
    //   종이 인형 · 해골 인형 · 손가락 커서는 그림 자체가 바뀜 (SkinData.HeldSprite 가 weapon_dummy__<캐릭터 스킨> 을 찾음 — 무기 스킨이 먼저)
    static string pupHex;
    static Color pupHexColor = Color.white;
    static Color PupSkinColor()
    {
        string hex = SkinFx.ObjectLook("dummy");
        if (!ReferenceEquals(hex, pupHex)) { pupHex = hex; pupHexColor = hex != null && ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white; }
        return pupHexColor;
    }

    // 더미 그림 4프레임 (형태가 바뀌면 전용 시트로 · 없으면 기본 더미 · 그것도 없으면 빛 무리로 — 예외 없이)
    void PupLoadFrames()
    {
        pupFramesForm = form;
        string path = form == SpecialAbilities.KitScissorDummy ? "Weapons/weapon_dummy_scissor"
                    : form == SpecialAbilities.KitSpoolDummy ? "Weapons/weapon_dummy_spool"
                    : form == SpecialAbilities.KitWindupDummy ? "Weapons/weapon_dummy_windup"
                    : SkinData.HeldSprite("dummy");          // 2.0.0~ 무기 스킨(곰 인형 · 목각 인형)이 더미에 입혀짐 (형태가 바뀌면 형태 그림이 먼저)
        Sprite[] f = Resources.LoadAll<Sprite>(path);
        if (f == null || f.Length == 0) f = Resources.LoadAll<Sprite>("Weapons/weapon_dummy");
        if (f != null && f.Length > 1) System.Array.Sort(f, (x, y) => Index(x.name).CompareTo(Index(y.name)));
        pupFrames = f;
        if (pupBody != null) pupBody.sprite = null;       // 다음 PupShowFrame 이 크기까지 새로 맞춤
    }

    void PupShowFrame(int i)
    {
        if (pupBody == null || pupFrames == null || pupFrames.Length == 0) return;
        Sprite s = pupFrames[Mathf.Clamp(i, 0, pupFrames.Length - 1)];
        if (pupBody.sprite != s)
        {
            pupBody.sprite = s;
            float h = s.bounds.size.y;
            pupBodyTf.localScale = Vector3.one * (h > 0.01f ? 2.4f / h : 1f);
        }
    }

    // ================================================================= 커서 (마우스 · 패드 오른 스틱)
    Vector2 PupCursor(Vector2 me)
    {
        if (GamePad.Active && !GameInput.Auto)
        {
            Vector2 rs = GamePad.RightStick;
            float m = rs.magnitude;
            if (m > 0.2f)
            {
                pupPadDir = rs / m;
                float len = PupMinLen + (PupMaxLen - PupMinLen) * Mathf.Clamp01((m - 0.2f) / 0.75f);       // 기울기 = 줄 길이
                return me + pupPadDir * len;
            }
            // RT 만 누른 보조 조준: 가까운 적 자리 (11칸 안 · 줄 길이로 자름)
            if (GamePad.AssistAiming)
            {
                Transform e = Specials.NearestEnemy(me, 11f);
                if (e != null) return e.position;
            }
            Vector2 ad = GamePad.AimDir;
            if (ad.sqrMagnitude > 0.01f) pupPadDir = ad.normalized;
            return me + pupPadDir * 2.5f;       // 스틱을 놓으면 마지막 방향 곁으로
        }
        return Mouse;
    }

    // 카메라 화면 안 (가장자리 0.8칸 안쪽)으로 자름
    static Vector2 PupInView(Vector2 p)
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return p;
        float h = cam.orthographicSize - 0.8f, w = cam.orthographicSize * cam.aspect - 0.8f;
        Vector3 c = cam.transform.position;
        p.x = Mathf.Clamp(p.x, c.x - w, c.x + w);
        p.y = Mathf.Clamp(p.y, c.y - h, c.y + h);
        return p;
    }

    // 몸 충돌 반지름 (EnemyHitbox 기준 — 없으면 콜라이더)
    static float PupBodyR(Component c)
    {
        if (c == null) return 0.6f;
        EnemyHitbox hb = c.GetComponent<EnemyHitbox>();
        if (hb != null && hb.BodyWorldRadius > 0.05f) return hb.BodyWorldRadius;
        return c.TryGetComponent(out Collider2D col) ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.y) : 0.6f;
    }

    // ================================================================= 매 프레임 (LateUpdate — 영웅이 움직인 뒤)
    void PupLate()
    {
        if (player == null) return;
        PupEdgePause(Time.timeScale == 0f);         // 2.0.1~ 일시정지 · 창 위에 붉은 막(정렬 880)이 덮이지 않게
        if (player.IsDying) { PupHide(); return; }
        PupEnsure();
        if (pupRoot == null) return;
        if (!pupRoot.activeSelf) pupRoot.SetActive(true);
        if (pupFramesForm != form) PupLoadFrames();
        bool paused = Time.timeScale == 0f;
        Vector2 me = transform.position;
        float dt = paused ? 0f : Time.deltaTime;

        // 목표점: 커서를 영웅에게서 1.2 ~ 최대 줄 길이로 · 화면 안 · 경기장 안
        pupRaw = PupCursor(me);
        Vector2 d = pupRaw - me;
        float len = d.magnitude;
        Vector2 dir = len > 0.05f ? d / len : (pupPos - me).sqrMagnitude > 0.01f ? (pupPos - me).normalized : Vector2.right;
        Vector2 target = me + dir * Mathf.Clamp(len, PupMinLen, PupMaxLen);
        target = PupInView(target);
        Vector3 t3 = Hostile.ClampArena(target);
        target = t3;

        pupLocked = Time.time < pupLockUntil;
        if (pupLocked) { pupPos = pupHold; pupVel = Vector2.zero; }
        else if (dt > 0f)
        {
            float speedMul = augment == AugHeavyHead ? 0.8f : 1f;
            pupPos = Vector2.SmoothDamp(pupPos, target, ref pupVel, PupFollowTime / speedMul, PupFollowSpeed * speedMul, dt);
        }
        PupLockTick(paused);

        // 그림
        pupRoot.transform.position = new Vector3(pupPos.x, pupPos.y, 0f);
        float bob = 0.6f + 0.08f * Mathf.Sin(Time.time * (2f * Mathf.PI / 1.4f));
        if (pupBodyTf != null) pupBodyTf.localPosition = new Vector3(0f, bob + 0.5f, 0f);
        float sw = Time.time - pupSwingAt;
        PupShowFrame(sw < 0.25f ? 1 + Mathf.Min(2, (int)(sw / 0.25f * 3f)) : 0);
        if (pupBody != null) pupBody.color = Time.time < pupFlashAt ? new Color(1f, 0.6f, 0.6f) : PupSkinColor();

        PupPathLate(me, target, dt, paused);
        PupDrawString(me);
        PupDrawRing(sw);
        PupDrawGuide();
        if (paused) return;

        // 줄 판정
        pupCutsThisFrame = 0;
        Vector2 a = me + Vector2.up * 0.9f;
        PupCutSegment(0, a, pupPos, 1f, PupTaut, true);
        if (augment == AugSecondString)
        {
            Vector2 side = (pupPos - a).sqrMagnitude > 0.01f ? new Vector2(-(pupPos - a).y, (pupPos - a).x).normalized * 0.6f : Vector2.zero;
            PupCutSegment(1, a + side, pupPos, 0.6f, PupTaut, true);
        }
        PupPathCutSegments(a);
        PupTraceTick(a);
        if (Time.time > pupPruneAt) PupPrune();

        // 태엽 병정: 좌클릭을 떼고 있으면 0.6초마다 저절로 휘두름 (피해 60%)
        if (form == SpecialAbilities.KitWindupDummy && !GameInput.FireHeld && Time.time >= pupAutoAt && !player.IsDying)
        {
            pupAutoAt = Time.time + 0.6f;
            PupSwing(Damage * 0.6f, false);
        }
        PupCurtainTick();
        PupPathTick();
    }

    void PupHide()
    {
        if (pupRoot != null && pupRoot.activeSelf) pupRoot.SetActive(false);
        if (pupLine != null) pupLine.enabled = false;
        if (pupLine2 != null) pupLine2.enabled = false;
        if (pupRing != null) pupRing.enabled = false;
        if (pupGuide != null) pupGuide.enabled = false;
        PupReleaseDecoy();
        PupCurtainCleanup();
        PupPathCleanup();
        PupEdgeOff();       // 2.0.1~ 걷힘 연출(PupEdgeTick)은 죽는 중엔 돌지 않아 막이 사망 · 결과 화면까지 남았음 → 바로 끔
    }

    // ================================================================= 그리기
    void PupDrawString(Vector2 me)
    {
        Vector2 a = me + Vector2.up * 0.9f;
        PupDrawLine(ref pupLine, "PupString", a, pupPos);
        if (augment == AugSecondString)
        {
            Vector2 ab = pupPos - a;
            Vector2 side = ab.sqrMagnitude > 0.01f ? new Vector2(-ab.y, ab.x).normalized * 0.6f : Vector2.zero;
            PupDrawLine(ref pupLine2, "PupString2", a + side, pupPos);
        }
        else if (pupLine2 != null) pupLine2.enabled = false;
    }

    void PupDrawLine(ref LineRenderer lr, string name, Vector2 a, Vector2 b)
    {
        if (lr == null) { lr = Hostile.NewLine(name, PupSky, 0.1f, 24); lr.loop = false; SkinFx.StringLook(lr); }      // 스킨 줄 결 (종이끈 · 케이블 · 영혼불 실 — 같은 굵기)
        lr.enabled = true;
        float L = Vector2.Distance(a, b);
        Color c = SkinFx.StringColor(HeroAim.ColorOf(HeroAimKind.Puppeteer));
        if (L < PupSlack)
        {
            // 늘어진 줄: 아래로 처진 곡선 (판정 없음)
            int n = 7;
            lr.positionCount = n;
            float sag = (PupSlack - L) * 0.5f + 0.2f;
            for (int i = 0; i < n; i++)
            {
                float k = i / (n - 1f);
                Vector2 p = Vector2.Lerp(a, b, k) + Vector2.down * (Mathf.Sin(k * Mathf.PI) * sag);
                lr.SetPosition(i, new Vector3(p.x, p.y, 0f));
            }
            lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, 0.45f);
            lr.startWidth = lr.endWidth = 0.08f;
        }
        else
        {
            // 판정이 있는 줄: 늘 곧은 선 · 팽팽할수록 진하고 굵음
            float t = PupTaut;
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(a.x, a.y, 0f));
            lr.SetPosition(1, new Vector3(b.x, b.y, 0f));
            float al = t < 0.3f ? 0.55f : Mathf.Lerp(0.6f, 1f, Mathf.Clamp01((t - 0.3f) / 0.5f));
            lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, al);
            lr.startWidth = lr.endWidth = 0.1f + 0.06f * t;
        }
        HeroAim.Mark(lr, HeroAimKind.Puppeteer, HeroAimFx.None);
    }

    // 더미 발밑 휘두름 범위 고리 (늘 옅게 · 휘두르는 순간 진하게)
    void PupDrawRing(float sinceSwing)
    {
        if (pupRing == null) { pupRing = Hostile.NewLine("PupRing", PupSky, 0.08f, 22); pupRing.loop = true; }
        pupRing.enabled = !PlayerController.Cinematic;
        Color c = HeroAim.ColorOf(HeroAimKind.Puppeteer);
        float al = sinceSwing < 0.12f ? 0.85f : 0.25f;
        pupRing.startColor = pupRing.endColor = new Color(c.r, c.g, c.b, al);
        pupRing.startWidth = pupRing.endWidth = sinceSwing < 0.12f ? 0.14f : 0.07f;
        Hostile.SetArc(pupRing, new Vector3(pupPos.x, pupPos.y, 0f), PupSwingR, 0f, 360f);
        HeroAim.Mark(pupRing, HeroAimKind.Puppeteer, HeroAimFx.None);
    }

    // 커서가 줄 길이 밖이면 더미 → 커서 점선 (줄이 모자람)
    void PupDrawGuide()
    {
        bool far = !pupLocked && !GamePad.Active && Vector2.Distance(pupRaw, pupPos) > 1f && PupOverstretch > 0.05f;
        if (!far) { if (pupGuide != null) pupGuide.enabled = false; return; }
        // 2.0.0~ 주석대로 점선: 얇은 단색 선 → 작은 도트 점이 커서 쪽으로 흐름 + 끝 화살촉 (안 그린 프레임엔 저절로 숨음)
        Color c = HeroAim.ColorOf(HeroAimKind.Puppeteer);
        PixelMarks.Ensure(ref pupPx, this).DotPath(pupPos, pupRaw, 0f, new Color(c.r, c.g, c.b, 0.55f), 0.45f, true, default, true, 0.8f);
    }

    // ================================================================= 평타: 휘두름
    void PuppetAttack(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        PupSwing(dmg, true);
        for (int i = 1; i < shots; i++) StartCoroutine(PupSwingLater(0.12f * i, dmg));      // 멀티샷: 0.12초 간격으로 한 번씩 더 (범위는 그대로)
    }

    IEnumerator PupSwingLater(float delay, float dmg)
    {
        yield return new WaitForSeconds(delay);
        if (player != null && !player.IsDying) PupSwing(dmg, false);
    }

    // 한 번 휘두름. user: 눌러서 휘두른 것 (T 자세 · 박수 · 가위 손은 눌러서 휘두를 때만 — 자동 휘두름에도 쓸지는 아래 주석)
    void PupSwing(float dmg, bool user, bool echo = false)
    {
        PupEnsure();
        if (pupRoot == null) return;
        float dmgIn = dmg;
        float rMul = 1f, big = 1f;
        // T 자세: 쉬었다가 휘두르면 범위 1.5배 · 피해 +% (고유 스킬 T 자세 · 갑옷 인형은 쉬는 시간 0.8초)
        if (user) big = SignatureSkills.PupTPose(PupRestTime, out rMul);
        float r = PupSwingR * rMul;
        dmg *= formPower * (augment == AugHeavyHead ? 1.2f : 1f) * big;
        float pathMul = PupPathSwingMul(echo, out float mirRate);        // 갈래: 팽팽한 줄 −30% · 호위 +40% · 등장 ×1.6 · 거울 65%
        float baseDmg = dmg * pathMul;
        dmg = baseDmg * mirRate;
        pupSwingSerial++;
        pupSwingAt = Time.time;
        pupLastSwingAt = Time.time;
        Vector3 at = new Vector3(pupPos.x, pupPos.y, 0f);
        pupSwinging = true;
        int hits;
        using (DamageSource.As(DamageSource.Basic))     // 자동 휘두름(LateUpdate)도 평타 출처
        try
        {
            hits = DamageCircle(at, r, dmg, 1.2f, 1.75f);
            hits += PupPathExtraSwings(at, r, baseDmg, mirRate);        // 거울 · 세 번째 더미, 꼭두각시 휘두름
            // 가위 손 인형: 더미 바로 아래(0.8칸 안) 적 하나에게 두 배 (+100%)
            if (form == SpecialAbilities.KitScissorDummy)
            {
                Transform near = Specials.NearestEnemy(at, 0.8f);
                if (near != null) { Specials.Damage(near.gameObject, dmg, (near.position - at).normalized, 0f); Fx.Spawn(FFxPup("fx_scissor_snip", "fx_spark"), near.position, 1.6f, Color.white, 24f); }
            }
            // 커튼콜 · 앙코르 더미: 다섯 더미 모두에서 동시에 (각 40% / 앙코르 50%)
            hits += PupExtrasSwing(Damage * formPower * (user ? 1f : 0.75f));
        }
        finally { pupSwinging = false; }
        if (!echo && PupOn(PuppeteerPaths.Guard) && Paths.Mastered(PuppeteerPaths.Guard)) StartCoroutine(PupBackSwing(dmgIn));      // 호위 극의: 0.2초 뒤 한 번 더
        Fx.Spawn(FFxPup("fx_puppet_swing", "fx_shock"), at, r * 2f, PupSky, 20f);
        Hostile.Play("whoosh", 0.35f, 0.9f + 0.1f * Random.value);
        if (big > 1.01f)
        {
            // 큰 휘두름: 갑옷 인형 진화는 둘레의 적 투사체를 쳐냄
            if (PupEvo("me.armor")) PupClearShots(at, r + 0.3f);
            Hostile.Shake(0.1f);
        }
        SignatureSkills.PupSwung(at, r, hits);
        // 관객의 박수: 한 번 휘둘러 적 셋 이상을 맞히면 필살기 게이지 (0.5% + 0.5% × 단계)
        if (card[6] > 0 && hits >= 3 && player.skillGauge != null && !player.skillGauge.IsFull() && Time.time - lastUlt >= 1f)
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * (0.005f + 0.005f * card[6]));
    }

    // 둘레의 적 투사체 지우기
    static void PupClearShots(Vector3 at, float r)
    {
        for (int i = HostileProjectile.Live.Count - 1; i >= 0; i--)
        {
            HostileProjectile hp = HostileProjectile.Live[i];
            if (hp == null) continue;
            if (((Vector2)(hp.transform.position - at)).sqrMagnitude > r * r) continue;
            Fx.Spawn("fx_spark", hp.transform.position, 1.2f, new Color(0.8f, 0.9f, 1f), 24f);
            Destroy(hp.gameObject);
        }
    }

    // ================================================================= 줄 베기
    // seg: 0 = 줄 · 1 = 두 번째 실 · mul: 피해 배율
    void PupCutSegment(int seg, Vector2 a, Vector2 b, float mul, float t, bool tautPath)
    {
        Vector2 ab = b - a;
        float L = ab.magnitude;
        if (L < PupSlack) { if (pupSide[seg].Count > 0) pupSide[seg].Clear(); return; }       // 늘어진 줄은 베지 않음
        Vector2 n = ab / L;
        float dmg = Damage * (0.15f + 0.20f * t) * mul * formPower * PupPathCutMul(tautPath);       // 갈래: 팽팽한 줄 ×1.6 · 호위 −30% · 꼭두각시 −40%
        float halfW = PupHalfW * SignatureSkills.PupCutWidthMul() * (tautPath && PupOn(PuppeteerPaths.Taut) ? PuppeteerPaths.TautWidth : 1f);
        Vector2 mid = (a + b) * 0.5f;
        using var list = PhysHits.Circle(mid, L * 0.5f + 2.2f);
        foreach (Collider2D c in list)
        {
            if (c == null) continue;
            bool boss = c.CompareTag("boss");
            if (!boss && !c.CompareTag("enermy")) continue;
            EnermyController e = boss ? null : c.GetComponent<EnermyController>();
            if (e != null && (e.IsDead || e.swarm != null)) continue;
            Vector2 p = c.transform.position;
            Vector2 ap = p - a;
            float proj = Vector2.Dot(ap, n) / L;
            float cross = ab.x * ap.y - ab.y * ap.x;
            sbyte s = (sbyte)(cross > 0f ? 1 : -1);
            float dist = Mathf.Abs(cross) / L;
            float rr = PupBodyR(c);
            int id = c.gameObject.GetInstanceID();
            bool hit = false;
            bool crossed = false;
            if (pupSide[seg].TryGetValue(id, out PupSideMemo prev) && prev.side != s && Time.frameCount - prev.frame <= 3
                && proj >= -0.05f && proj <= 1.05f && dist <= halfW + rr) hit = crossed = true;       // 서로 지나침 (줄이 쓸었든 적이 걸었든)
            pupSide[seg][id] = new PupSideMemo { side = s, frame = Time.frameCount };
            bool big = boss || (e != null && e.IsMidBoss) || rr > 1.1f;
            if (!hit && big && proj >= 0f && proj <= 1f && dist <= halfW + rr * 0.8f) hit = true;     // 큰 적은 몸에 걸쳐 있으면 0.45초마다
            if (!hit) continue;
            // 팽팽한 줄 공명 II: 보스 몸에 걸쳐 있는 동안은 0.25초마다 톱질 (줄 베기 0.45초 간격 대신)
            bool saw = !crossed && seg == 0 && tautPath && PupPathSawOn && Paths.IsBoss(c.gameObject);
            int key = id * 8 + seg;
            if (pupCutAt.TryGetValue(key, out float next) && Time.time < next) continue;
            pupCutAt[key] = Time.time + (saw ? PuppeteerPaths.TautSawGap : PupCutGap);
            if (saw) { PupPathSaw(c.gameObject, p, n); continue; }
            float hitDmg = dmg * PupPathCutHitMul(seg, id, proj, tautPath);       // 줄 끝 ×1.3 · 이중주 두 배
            PupApplyCut(c.gameObject, e, p, hitDmg, n, true);
        }
    }

    // 줄에 벤 적에게 실제 피해 · 효과 · 기록
    void PupApplyCut(GameObject go, EnermyController e, Vector3 p, float dmg, Vector2 along, bool count)
    {
        Vector2 sweep = new Vector2(-along.y, along.x);
        using (DamageSource.As(DamageSource.Basic))
        {
            pupCutting = true;
            try { Specials.Damage(go, dmg, Vector3.zero, 0f); }
            finally { pupCutting = false; }
        }
        Fx.Spawn(FFxPup("fx_string_cut", "fx_spark"), p, 1.5f, PupSky, 24f);
        // 연속 소리는 피치를 올리는 변주 (0.45초 안에 이어지면 +4%씩 · 최대 +24%)
        pupCutPitch = Time.time - pupCutPitchAt < PupCutGap ? Mathf.Min(1.24f, pupCutPitch + 0.04f) : 1f;
        pupCutPitchAt = Time.time;
        if (Time.frameCount % 2 == 0) Play("whoosh", 0.18f, 1.4f * pupCutPitch);
        if (!count) return;
        RunCounters.Both("pup.cut");
        pupCutsThisFrame++;
        SignatureSkills.PupCut(go, p, along);
        PupPathCutThread(go, e);        // 꼭두각시: 줄에 베일 때마다 실 1
        // 한 번 쓸기(0.3초)에 다섯 이상: 손맛 서명 · 계약
        pupCutTimes.Add(Time.time);
        while (pupCutTimes.Count > 0 && Time.time - pupCutTimes[0] > 0.3f) pupCutTimes.RemoveAt(0);
        if (pupCutTimes.Count >= 5 && Time.time >= pupSweepCool)
        {
            pupSweepCool = Time.time + 1f;
            pupCutTimes.Clear();
            RunCounters.Both("pup.sweep5");
            Impact.Hit(ImpactTier.Attack, p, sweep);
        }
    }

    // ---------------- 실 자국 (카드 0): 줄이 지나간 자리가 잠시 남아 닿는 적을 한 번 더 벰 (줄 베기의 60% · 하프 현 진화는 100%)
    void PupTraceTick(Vector2 a)
    {
        int lv = card[0];
        if (lv <= 0)
        {
            if (pupTraces.Count > 0) { pupTraces.Clear(); PupTraceLinesOff(0); }
            return;
        }
        float life = 0.2f + 0.3f * lv;
        if (PupStringLen >= PupSlack && Time.time >= pupTraceDropAt)
        {
            pupTraceDropAt = Time.time + 0.1f;
            while (pupTraces.Count >= 24) pupTraces.RemoveAt(0);       // 조각 24개 상한 (가장 오래된 것 지움)
            pupTraces.Add(new PupTrace { a = a, b = pupPos, until = Time.time + life });
        }
        for (int i = pupTraces.Count - 1; i >= 0; i--) if (Time.time >= pupTraces[i].until) pupTraces.RemoveAt(i);
        PupTraceLines();
        if (Time.time < pupTraceCheckAt) return;
        pupTraceCheckAt = Time.time + 0.1f;
        float mul = PupEvo("me.harp") ? 1f : 0.6f;
        float dmg = Damage * (0.15f + 0.20f * PupTaut) * mul * formPower * PupPathCutMul(true);
        for (int i = 0; i < pupTraces.Count; i++)
        {
            PupTrace tr = pupTraces[i];
            Vector2 ab = tr.b - tr.a;
            float L = ab.magnitude;
            if (L < PupSlack) continue;
            using var list = PhysHits.Circle((tr.a + tr.b) * 0.5f, L * 0.5f + 1.5f);
            foreach (Collider2D c in list)
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && (e.IsDead || e.swarm != null)) continue;
                if (Hostile.DistanceToSegment(c.transform.position, tr.a, tr.b) > PupHalfW + PupBodyR(c)) continue;
                int key = c.gameObject.GetInstanceID() * 8 + 7;
                if (pupCutAt.TryGetValue(key, out float next) && Time.time < next) continue;
                pupCutAt[key] = Time.time + PupCutGap;
                PupApplyCut(c.gameObject, e, c.transform.position, dmg, ab / L, false);
            }
        }
    }

    void PupTraceLines()
    {
        Color c = HeroAim.ColorOf(HeroAimKind.Puppeteer);
        for (int i = 0; i < pupTraces.Count; i++)
        {
            if (i >= pupTraceLines.Count)
            {
                LineRenderer lr = Hostile.NewLine("PupTrace" + i, c, 0.06f, 20);
                lr.loop = false;
                lr.positionCount = 2;
                pupTraceLines.Add(lr);
            }
            LineRenderer l = pupTraceLines[i];
            l.enabled = true;
            l.SetPosition(0, new Vector3(pupTraces[i].a.x, pupTraces[i].a.y, 0f));
            l.SetPosition(1, new Vector3(pupTraces[i].b.x, pupTraces[i].b.y, 0f));
            float left = Mathf.Clamp01((pupTraces[i].until - Time.time) / (0.2f + 0.3f * card[0]));
            l.startColor = l.endColor = new Color(c.r, c.g, c.b, 0.5f * left);
            HeroAim.Mark(l, HeroAimKind.Puppeteer, HeroAimFx.None);
        }
        PupTraceLinesOff(pupTraces.Count);
    }

    void PupTraceLinesOff(int from)
    {
        for (int i = from; i < pupTraceLines.Count; i++) if (pupTraceLines[i] != null) pupTraceLines[i].enabled = false;
    }

    // ---------------- 줄 베기로 쓰러뜨림 · 휘두름으로 쓰러뜨림 (OnKill 이 부름)
    void PupKilled(Vector3 pos)
    {
        if (pupCutting && !pupThreading && card[2] > 0) PupBrokenThread(pos, PupEvo("me.strings") ? 2 : 1);
        if (pupSwinging) SignatureSkills.PupSwingKill(pos);
        if (pupSwapCutting) pupSwapKills++;
    }

    // 끊어진 실 (카드 2): 줄로 쓰러뜨린 적에게서 끊긴 실이 튀어 가까운 적을 벰 (공격력 60% + 20% × 단계) — 천 가닥의 실은 두 번
    void PupBrokenThread(Vector3 from, int jumps)
    {
        float dmg = Damage * (0.4f + 0.2f * card[2]) * formPower;
        Vector3 cur = from;
        HashSet<Transform> hit = new HashSet<Transform>();
        pupThreading = true;
        try
        {
        for (int j = 0; j < jumps; j++)
        {
            Transform best = null;
            float bd = 6f * 6f;
            foreach (Collider2D c in Specials.Overlap(cur, 6f, pupHits))
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss")) || hit.Contains(c.transform)) continue;
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && e.IsDead) continue;
                float d = ((Vector2)(c.transform.position - cur)).sqrMagnitude;
                if (d < bd) { bd = d; best = c.transform; }
            }
            if (best == null) break;
            hit.Add(best);
            Fx.Stretch(FFxPup("fx_string_twang", "fx_spark"), cur, best.position, 0.6f, PupSky, 0.2f, 14, 24f);
            Specials.Damage(best.gameObject, dmg, (best.position - cur).normalized, 0.5f);
            cur = best.position;
        }
        }
        finally { pupThreading = false; }
    }
    static readonly List<Collider2D> pupHits = new List<Collider2D>(64);

    static string FFxPup(string wanted, string fallback) => Fx.Frames(wanted).Length > 0 ? wanted : fallback;

    // ---------------- 2초마다 정리
    void PupPrune()
    {
        pupPruneAt = Time.time + 2f;
        for (int s = 0; s < pupSide.Length; s++)
        {
            if (pupSide[s].Count < 64) continue;
            List<int> old = null;
            foreach (var kv in pupSide[s]) if (Time.frameCount - kv.Value.frame > 60) (old ??= new List<int>()).Add(kv.Key);
            if (old != null) foreach (int k in old) pupSide[s].Remove(k);
        }
        if (pupCutAt.Count > 96)
        {
            List<int> old = null;
            foreach (var kv in pupCutAt) if (kv.Value < Time.time) (old ??= new List<int>()).Add(kv.Key);
            if (old != null) foreach (int k in old) pupCutAt.Remove(k);
        }
    }

    // ================================================================= 우클릭 짧게: 자리 바꾸기
    // 게이지 20% 이상이면 실행 (게이지가 가득이면 누르는 순간이 아니라 0.25초 안에 뗄 때 — 커튼콜 조준과 구분)
    bool PupTrySwap(SkillGauge gauge)
    {
        if (gauge == null || Time.timeScale == 0f || player == null || player.IsDying) return false;
        float cost = PupSwapCost;
        float gap = PupOn(PuppeteerPaths.Swap) ? PuppeteerPaths.SwapPathGap : PupSwapGap;
        if (Time.time < pupSwapNextAt || gauge.SkillPoint + 0.0001f < gauge.MaxSkillPoint * cost) { PupBuzz(); return false; }
        Vector2 hero = transform.position;
        if (!PupFindLanding(pupPos, hero, out Vector2 land)) { PupBuzz(); return false; }       // 도착점이 벽 · 경기장 밖이면 줄을 따라 되짚음 (끝내 없으면 게이지 안 씀)
        if (!gauge.TrySpend(cost)) { PupBuzz(); return false; }
        pupSwapNextAt = Time.time + gap;
        pupSwapAt = Time.time;

        // 가르기: 옛 영웅 자리 → 새 영웅 자리 선분에 닿는 적 모두에게 공격력 120% (보스 포함 · 한 번)
        float cutMul = SignatureSkills.PupSwapCutMul() * (PupOn(PuppeteerPaths.Swap) ? PuppeteerPaths.SwapCutMul : 1f);
        pupSwapKills = 0;
        pupSwapCutting = true;
        float dmg = Damage * PupSwapCut * cutMul * formPower;
        int cut = 0;
        try
        {
        using (DamageSource.As(DamageSource.Ult))
        {
            Vector2 ab = land - hero;
            float L = ab.magnitude;
            using var list = PhysHits.Circle((hero + land) * 0.5f, L * 0.5f + 2.2f);
            foreach (Collider2D c in list)
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && (e.IsDead || e.swarm != null)) continue;
                if (Hostile.DistanceToSegment(c.transform.position, hero, land) > 0.6f + PupBodyR(c) * 0.6f) continue;
                Specials.Damage(c.gameObject, dmg, ab.normalized, 0f);
                Fx.Spawn(FFxPup("fx_string_cut", "fx_spark"), c.transform.position, 1.6f, Color.white, 24f);
                cut++;
                PupPathSwapCutHit(c.gameObject);        // 바꿔치기 공명 II: 보스에 꼬인 실
            }
        }
        }
        finally { pupSwapCutting = false; }
        PupPathSwapDone(cut);                           // 바꿔치기: 등장 · 극의 게이지 돌려받음
        Fx.Stretch(FFxPup("fx_string_twang", "fx_shock"), hero + Vector2.up * 0.9f, land + Vector2.up * 0.9f, 0.8f, PupSky, 0.25f, 15, 24f);
        Fx.Spawn(FFxPup("fx_swap_poof", "fx_sparkle"), hero, 3f, Color.white, 20f);
        Fx.Spawn(FFxPup("fx_swap_poof", "fx_sparkle"), land, 3f, Color.white, 20f);
        Play("whoosh", 0.5f, 1.5f);
        Play("pop", 0.4f, 1.2f);
        if (cut >= 3) RunCounters.Both("pup.swap.cut3");
        RunCounters.Both("pup.swap");
        if (cut > 0) Hostile.Shake(0.08f);

        // 자리 바꿈: 영웅은 더미 자리로 · 더미는 옛 영웅 자리에 붙박이 (그동안 미끼 · 휘두름은 붙박인 더미 자리에서)
        Vector3 z = transform.position;
        transform.position = new Vector3(land.x, land.y, z.z);
        if (PupPathStandIn(hero)) { }       // 대역(바꿔치기 × 꼭두각시): 꼭두각시가 그 자리에서 끌고 더미는 바로 커서로 (붙박이 · 무기 묶임 없음)
        else
        {
            pupHold = hero;
            pupPos = hero;
            pupVel = Vector2.zero;
            float lockSec = SignatureSkills.PupDecoyMul() * PupDecoySec;
            pupLockUntil = Time.time + lockSec;
            pupDecoyTick = Time.time + 0.4f;
            pupLockEnd = true;
            pupLocked = true;
            if (pupRoot != null) pupRoot.transform.position = new Vector3(hero.x, hero.y, 0f);
            if (EnermyController.Decoy == null) { EnermyController.Decoy = pupRoot != null ? pupRoot.transform : null; EnermyController.DecoyBossToo = false; }
        }
        player.GrantInvincibility(PupSwapInvuln);
        // 무대 체질(패시브): 가를 때마다 게이지 1% (한 번에 최대 3% · 진화 1.5% / 5%)
        if (cut > 0 && Special != null && Special.Has(SpecialAbilities.KitStageborn) && player.skillGauge != null)
        {
            bool evo = Special.IsEvolved(SpecialAbilities.KitStageborn);
            float each = evo ? 0.015f : 0.01f, cap = evo ? 0.05f : 0.03f;
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * Mathf.Min(cap, each * cut));
        }
        // 대역 배우(강화): 남은 더미가 0.5초 뒤 터짐 (공격력 150%)
        if (augment == AugUnderstudy) StartCoroutine(PupUnderstudy(hero));
        SignatureSkills.PupSwapped(hero, land, cut);
        Impact.Hit(ImpactTier.Skill, land, (land - hero).normalized);
        PlayerMotion.Of(this).Cast(0.25f);
        return true;
    }
    float pupDecoyTick;
    bool pupSwapCutting;
    int pupSwapKills;

    IEnumerator PupUnderstudy(Vector2 at)
    {
        yield return new WaitForSeconds(0.5f);
        if (player == null || player.IsDying) yield break;
        using (DamageSource.As(DamageSource.Ult))
            DamageCircle(at, 2.2f, Damage * 1.5f * formPower, 2f);
        Fx.Spawn("fx_explosion", at, 4.4f, PupSky, 18f);
        Play("boom", 0.4f, 1.2f);
    }

    void PupBuzz()
    {
        if (Time.unscaledTime - pupBuzzAt < 0.3f) return;
        pupBuzzAt = Time.unscaledTime;
        pupFlashAt = Time.time + 0.12f;
        Play("pew", 0.25f, 0.5f);       // 「삐걱」
    }

    // 도착점이 걸을 수 없는 곳이면 줄을 따라 영웅 쪽으로 0.5칸씩 되짚어 첫 걸을 수 있는 점 (최대 18번)
    static bool PupFindLanding(Vector2 dst, Vector2 hero, out Vector2 land)
    {
        Vector2 d = hero - dst;
        float L = d.magnitude;
        Vector2 dir = L > 0.01f ? d / L : Vector2.zero;
        for (int i = 0; i <= 18; i++)
        {
            float off = 0.5f * i;
            if (off > L) break;
            Vector2 p = dst + dir * off;
            if (!Hostile.IsWall(p) && Hostile.InArena(p)) { land = p; return true; }
        }
        land = hero;
        return false;
    }

    // 붙박이 더미가 있는 동안: 빈자리의 인형(0.4초마다 둘레 벰) · 끝나면 미끼 풀기 · 퀵 체인지(끝날 때 터짐)
    void PupLockTick(bool paused)
    {
        if (paused) return;
        if (pupLocked)
        {
            if (card[3] > 0 && Time.time >= pupDecoyTick)
            {
                pupDecoyTick = Time.time + 0.4f;
                using (DamageSource.As(DamageSource.Basic))
                    DamageCircle(new Vector3(pupHold.x, pupHold.y, 0f), PupSwingR, Damage * 0.3f * formPower, 0.6f);
                Fx.Spawn(FFxPup("fx_puppet_swing", "fx_shock"), pupHold, PupSwingR * 1.6f, PupSky, 20f);
            }
            return;
        }
        if (pupLockEnd)
        {
            pupLockEnd = false;
            PupReleaseDecoy();
            if (PupEvo("me.quickchange"))
            {
                Vector3 at = new Vector3(pupHold.x, pupHold.y, 0f);
                using (DamageSource.As(DamageSource.Ult)) DamageCircle(at, 3f, Damage * 2f * formPower, 3f);
                Fx.Spawn("fx_explosion", at, 6f, PupSky, 18f);
                Play("boom", 0.5f, 1.1f);
            }
        }
    }
    bool pupLockEnd;        // 붙박이가 끝나길 기다리는 중 (끝나면 미끼 풀기 · 퀵 체인지)

    void PupReleaseDecoy()
    {
        if (pupRoot != null && EnermyController.Decoy == pupRoot.transform)
        {
            EnermyController.Decoy = null;
            EnermyController.DecoyBossToo = true;
        }
    }

    // ================================================================= 머리 위 막대: 남은 바꾸기 횟수
    void PupRuleBar()
    {
        int n = Mathf.Min(5, PupSwapsLeft);
        bool ready = Time.time >= pupSwapNextAt && n > 0;
        Color c = ready ? PupSky : new Color(0.45f, 0.5f, 0.7f);
        DrawRuleBar(n / 5f, c, true, "바꾸기", n, 5);
    }

    // ================================================================= 정리
    void OnDestroyPuppeteer()
    {
        PupReleaseDecoy();
        PupCurtainCleanup();
        PupPathCleanup(true);
        if (pupRoot != null) Destroy(pupRoot);
        foreach (LineRenderer lr in new[] { pupLine, pupLine2, pupRing, pupGuide }) if (lr != null) Destroy(lr.gameObject);
        if (pupPx != null) Destroy(pupPx.gameObject);
        foreach (LineRenderer lr in pupTraceLines) if (lr != null) Destroy(lr.gameObject);
        // 2.0.0~ 검토: 커튼콜 가장자리 붉은 막(플레이어 자식 캔버스) · 궁극기 조준 고리가 교대 뒤에도 남았음
        if (pupEdgeCanvas != null) Destroy(pupEdgeCanvas.gameObject);
        if (pupAimRing != null) Destroy(pupAimRing.gameObject);
    }
}
