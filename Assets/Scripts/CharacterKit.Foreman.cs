using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공사 반장 (2.0.0): 영원한 공사 중 — docs/plans/2026-11-02-big-update/hero-foreman.md
//   평타 · 밀기: 누르는 동안 삽날(마우스 쪽, 회전 속도 제한)로 닿는 잡몹을 앞에 한 더미로 쌓음 (EnemyHold) → 평타 틱마다 더미 전체를 짓누름 (무거울수록 한 마리당 피해 ↑)
//               더미 앞이 벽 · 경계 · 바리케이드 · 보스에 닿으면 압착 ×1.5, 움직이다 처음 부딪히면 「빠각」 · 보스는 잡지 않고 들이받기(단일)
//   떼면 덤프: 무게 3 이상이면 마우스 쪽으로 부채꼴 산사태(0.35초) · 3 미만이면 흩뿌림(피해 없음)
//   우클릭 · 철거 공고: 누르고 있으면 착지 고리 + 튕김 고리 3 → 떼면 쇠공이 떨어져 세 번 튕기며 굴러가고 튕긴 자리마다 4초 바리케이드
//   상태는 「끝나는 시각」 값만 (코루틴 켜고 끄는 깃발 없음) · 더미 · 바리케이드는 저장하지 않음 (이어하면 빈 더미)
// 레벨업 카드 (card): 0 긴 삽날 · 1 잔해 · 2 빠각! · 3 공사 구간 · 5 안전 수칙 · 6 적재함 확장 / 형태: 굴착기 버킷 · 착암기 · 철거 쇠공 / 강화: 뒷날 · 유압 실린더 · 안전 펜스
public partial class CharacterKit
{
    static readonly Color FmYellow = new Color(0.95f, 0.88f, 0.35f);
    static readonly Color FmOrange = new Color(1f, 0.6f, 0.15f);
    static readonly Color FmDust = new Color(0.8f, 0.7f, 0.5f);

    // 2.0.1~ 판정 넓힘 (폭 2.2 → 3.4 · 두께 1 → 2): 붙은 적을 못 담고 부딪혀 맞던 것
    const float FmBladeAt = 0.9f, FmBladeDepth = 2f, FmBladeWidth = 3.4f, FmBodyGrab = 1.1f;
    const float FmCrushBase = 0.30f, FmCrushPerMass = 0.03f, FmBashRate = 0.85f;
    const int FmFenceCap = 9, FmPileBase = 12;
    public const float FmWreckRange = 14f;

    // ---------------- 더미 한 칸 (적 한 마리 또는 잔해 하나)
    sealed class FmItem
    {
        public EnermyController e;      // 적 (null 이면 잔해)
        public int mass;
        public int cement;              // 시멘트 (고유 스킬 시멘트 · 0~3)
        public Transform rubble;        // 잔해 그림
        public float until;             // 잔해가 사라지는 시각
    }

    // ---------------- 바리케이드
    sealed class FmFence
    {
        public Vector3 pos;
        public Vector2 along, normal;
        public float half = 1.6f, thick = 0.3f, until;
        public bool wall;               // 교차 「콘크리트 벽」
        public GameObject go;
        public SpriteRenderer sr;
        public LineRenderer line;
    }

    readonly List<FmItem> fmItems = new List<FmItem>(32);
    readonly List<FmFence> fmFences = new List<FmFence>(FmFenceCap + 2);
    static readonly List<Collider2D> fmHits = new List<Collider2D>(64);
    readonly List<EnermyController> fmTmp = new List<EnermyController>(32);

    bool fmHolding;
    float fmHoldStart, fmNextTick, fmAbsorbAt, fmCrashAt, fmReleaseAt = -9f, fmBlockAt, fmOverAt, fmBackAt, fmSnackAt, fmOrbitAt;
    Vector2 fmBlade = Vector2.right;
    Vector3 fmCenter, fmLastPos;
    Vector2 fmMoveDir = Vector2.right;
    float fmR;
    int fmMass, fmDumps;
    bool fmWall;                   // 더미 앞이 벽 · 경계 · 바리케이드 · 보스에 닿아 있음
    bool fmWallPrev;
    GameObject fmBossTarget;       // 삽날에 닿은 보스 · 중간 보스 (들이받기 대상)
    LineRenderer fmPreview;
    GameObject fmOrbitBall;
    float fmOrbitAngle;

    // ---------------- 밖에서 읽는 값
    public bool Plows => Id == CharacterId.Foreman;
    public bool PlowActive => fmHolding;
    public bool FmHolding => fmHolding;
    public int FmMass => fmMass;
    public Vector3 FmCenter => fmCenter;
    public Vector2 FmBlade => fmBlade;
    public float FmRadius => fmR;
    public int FmDumpCount => Mathf.Max(fmDumps, RunCounters.Get("b.dump"));
    public int FmFenceCount => fmFences.Count;
    public int FmPileCount => fmItems.Count;
    public int FmHardCount { get { int n = 0; foreach (FmItem it in fmItems) if (it.cement >= 3) n++; return n; } }

    // 더미 상한 (긴 삽날 +3씩 · 불도저 진화 ×1.5)
    public int FmPileMax => FmPathPileMax(Mathf.RoundToInt((FmPileBase + 3 * card[0]) * (SignatureSkills.FmEvo("be.dozer") ? 1.5f : 1f)));       // 갈래 불도저 +4 (극의 30)
    float FmBladeW => FmBladeWidth + 0.4f * card[0] + (SignatureSkills.FmEvo("be.dozer") ? 0.4f : 0f);
    int FmMassCap => FmPathMassCap(augment == AugHydraulic ? 18 : 12);       // 갈래 안전 제일 8
    float FmFenceLife => 4f + card[3] + FmPathFenceExtra;
    bool FmSlowing => card[3] > 0;
    int FmRubbleMax => (card[1] * 2) * (SignatureSkills.FmEvo("be.concrete") ? 2 : 1);

    static string FFx(string wanted, string fallback) => Fx.Frames(wanted).Length > 0 ? wanted : fallback;

    // 몸 충돌 반지름 (EnemyHitbox 기준 — 없으면 콜라이더)
    static float FmBodyR(Component c)
    {
        if (c == null) return 0.6f;
        EnemyHitbox hb = c.GetComponent<EnemyHitbox>();
        if (hb != null && hb.BodyWorldRadius > 0.05f) return hb.BodyWorldRadius;
        return c.TryGetComponent(out Collider2D col) ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.y) : 0.6f;
    }

    // 무게: round((r / 0.45)^2) 을 1~4 로
    static int FmMassOf(EnermyController e) => Mathf.Clamp(Mathf.RoundToInt(Mathf.Pow(FmBodyR(e) / 0.45f, 2f)), 1, 4);

    // 이동 필터 (PlayerController 가 부름): 밀고 있는 동안 느려짐 — 앞 1 · 옆 0.75 · 뒤 0.5 배(사이는 선형) × 무게 감속. 떼고 0.15초는 그대로 (회피가 늘 되게)
    public void FilterMove(ref Vector3 mv)
    {
        if (Id != CharacterId.Foreman || mv.sqrMagnitude < 0.0001f) return;
        if (!fmHolding && Time.time > fmReleaseAt + 0.15f) return;
        float hold = Mathf.Max(0.7f, 0.85f - 0.015f * fmMass);
        float c = Vector2.Dot(((Vector2)mv).normalized, fmBlade);
        float dirF = Mathf.Clamp(0.75f + 0.5f * c, 0.5f, 1f);
        float f = hold * dirF;
        f = FmPathMoveFilter(f, c);       // 갈래 불도저: 앞으로는 느려지지 않음
        // 안전화(패시브): 느려지는 정도가 절반 · 진화는 느려지지 않음
        if (Special != null && Special.Has(SpecialAbilities.KitBoots)) f = Special.IsEvolved(SpecialAbilities.KitBoots) ? 1f : 1f - (1f - f) * 0.5f;
        mv *= f;
    }

    // ================================================================= 입력 (PlayerController 가 매 프레임 부름)
    public void UpdatePlow(bool canAct, float interval)
    {
        if (player == null) return;
        if (player.IsDying) { if (fmHolding) FmEndHold(false); return; }
        if (Time.timeScale == 0f) return;       // 창이 열림: 더미는 그대로 멈춰 있고 떼기 판정은 시간이 흐를 때만 (창이 닫힌 뒤 버튼이 떨어져 있으면 그때 덤프)
        bool held = GameInput.FireHeld;
        // 자동 조작: 무게 6 이상이거나 누른 지 2.5초면 저절로 덤프 (AutoFire 가 계속 참이라 떼는 순간이 없으므로)
        bool auto = GameInput.Auto || GameInput.TrailerRunning;
        if (auto && fmHolding && (fmMass >= 6 || Time.time - fmHoldStart > 2.5f)) held = false;
        bool want = canAct && held;
        if (!fmHolding)
        {
            if (!want) return;
            fmHolding = true;
            fmHoldStart = Time.time;
            fmNextTick = Time.time + Mathf.Min(0.2f, interval * 0.4f);
            fmAbsorbAt = 0f;
            fmWall = fmWallPrev = false;
            Vector2 first = (Vector2)(Mouse - transform.position);
            if (first.sqrMagnitude > 0.04f) fmBlade = first.normalized;
            if (FmTryRedump()) return;       // 31번 카드 작업 지시: 덤프 직후 다시 누름
            Play("hum", 0.25f, 1.3f);
        }
        else if (!want) { FmEndHold(true); return; }

        // 삽날 방향: 조준 쪽으로 회전 속도 제한 (300°/초 − 12°/초 × 무게, 최저 150) · 패드는 오른 스틱이 가운데면 이동 방향
        Vector2 aim = ((Vector2)(Mouse - transform.position));
        if (GamePad.Active && !GamePad.Aiming && fmMoveDir.sqrMagnitude > 0.01f) aim = fmMoveDir;
        if (aim.sqrMagnitude > 0.04f)
        {
            float turn = Mathf.Max(150f, 300f - 12f * fmMass) * Time.deltaTime;
            float cur = Mathf.Atan2(fmBlade.y, fmBlade.x) * Mathf.Rad2Deg;
            float goal = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            float next = Mathf.MoveTowardsAngle(cur, goal, turn) * Mathf.Deg2Rad;
            fmBlade = new Vector2(Mathf.Cos(next), Mathf.Sin(next));
        }
        player.FaceTowards(transform.position + (Vector3)fmBlade * 3f);

        if (Time.time >= fmAbsorbAt) { fmAbsorbAt = Time.time + 0.1f; FmAbsorb(); }
        if (Time.time >= fmNextTick)
        {
            fmNextTick = Time.time + Mathf.Max(0.25f, interval);
            FmCrushTick();
        }
    }

    void FmEndHold(bool dump)
    {
        fmHolding = false;
        fmReleaseAt = Time.time;
        if (fmPreview != null) fmPreview.enabled = false;
        if (dump) FmDump();
        else FmReleaseAll(true);
    }

    // ================================================================= 흡수 (0.1초마다)
    void FmAbsorb()
    {
        FmClean();
        Vector3 me = transform.position;
        float reachAt = FmBladeAt + (SignatureSkills.FmEvo("be.dozer") ? 1f : 0f);
        Vector3 bc = me + (Vector3)(fmBlade * reachAt);
        float halfW = FmBladeW * 0.5f, halfD = FmBladeDepth * 0.5f + (SignatureSkills.FmEvo("be.dozer") ? 0.5f : 0f);
        Vector2 side = new Vector2(-fmBlade.y, fmBlade.x);
        int max = FmPileMax;
        fmBossTarget = null;
        bool touchHard = false;
        Specials.Overlap(bc, Mathf.Max(Mathf.Max(halfW, halfD), reachAt + FmBodyGrab) + 1.4f, fmHits);
        for (int i = 0; i < fmHits.Count; i++)
        {
            Collider2D c = fmHits[i];
            if (c == null) continue;
            bool boss = c.CompareTag("boss");
            if (!boss && !c.CompareTag("enermy")) continue;
            Vector2 rel = (Vector2)(c.transform.position - bc);
            float r = FmBodyR(c);
            bool inBlade = Mathf.Abs(Vector2.Dot(rel, fmBlade)) <= halfD + r && Mathf.Abs(Vector2.Dot(rel, side)) <= halfW + r;
            // 몸에 붙은 잡몹도 방향 상관없이 더미로 (보스 · 중간 보스는 삽날 앞일 때만 벽 취급)
            bool onBody = !boss && ((Vector2)(c.transform.position - me)).magnitude <= FmBodyGrab + r;
            if (!inBlade && !onBody) continue;
            if (boss) { fmBossTarget = c.gameObject; touchHard = true; continue; }
            if (!c.TryGetComponent(out EnermyController e) || e.IsDead || e.holder != null) continue;
            if (e.IsMidBoss) { if (inBlade) { fmBossTarget = c.gameObject; touchHard = true; } continue; }       // 중간 보스는 잡지 않음 (삽날 앞이면 벽 취급)
            if (e.swarm != null) continue;
            if (e.casting) { e.transform.position += (Vector3)(rel.normalized * 0.6f); continue; }     // 시전 중인 적은 끊지 않고 밀쳐 냄
            if (FmPathShove(e, rel)) continue;       // 갈래 불도저 공명 I: 무게 10 이상이면 밀쳐 길을 냄
            int m = FmMassOf(e);
            if (fmMass + m <= max && EnemyHold.TryTake(e, this))
            {
                fmItems.Add(new FmItem { e = e, mass = m });
                EnemyHold.Raise(e, body, 2);
                fmMass += m;
                RunCounters.Max("b.pile.max", fmMass);
                Fx.Spawn(FFx("fx_dust", "fx_sparkle"), e.transform.position, 1.4f, FmDust, 18f);
            }
            else if (fmMass + m > max)
            {
                // 가득 참: 닿은 적은 밀쳐 냄 (넉백 4 · 공격력 15%) — 과적(고유 스킬)은 넘친 적이 굴러떨어지며 둘레를 다치게 함
                Specials.Damage(e.gameObject, Damage * 0.15f, rel.normalized, 4f);
                SignatureSkills.FmOverflow(e.transform.position);
            }
        }
        if (fmBossTarget != null) touchHard = true;
        FmUpdateWall(touchHard);
    }

    // 더미 앞이 단단한 것에 닿았나 → 압착 ×1.5 · 처음 닿는 순간 빠각
    void FmUpdateWall(bool touchHard)
    {
        Vector3 front = transform.position + (Vector3)(fmBlade * (FmBladeAt + 0.5f + 2f * fmR + (fmItems.Count > 0 ? 0.3f : 0f)));
        FmFence fence = FmFenceAt(front, 0.6f);
        bool hard = touchHard || FmBlocked(front) || fence != null;
        fmWall = hard && (fmItems.Count > 0 || fmBossTarget != null);
        if (fmWall && !fmWallPrev && Time.time >= fmCrashAt)
        {
            // 더미가 처음 부딪히는 순간 (0.6초에 한 번)
            if (fmItems.Count > 0) FmCrash(front, fmBossTarget, fence != null);
            if (fmWall) RunCounters.Add("b.wall");
        }
        fmWallPrev = fmWall;
        FmPathWall();       // 갈래 불도저 공명 II: 완전 압착
    }

    // ================================================================= 압착 (평타 틱)
    void FmCrushTick()
    {
        FmClean();
        Vector3 center = fmCenter;
        int M = fmMass;
        float rate = FmCrushBase + FmCrushPerMass * (Mathf.Min(M, FmMassCap) - 1);
        if (fmWall) rate *= 1.5f;
        if (form == SpecialAbilities.KitJackhammer) rate *= 1.2f;
        float each = Damage * rate * formPower * SignatureSkills.FmCrushMul(fmWall) * FmPathCrushMul();
        PlayerController.RaiseAttackFired(transform.position, fmBlade);
        PlayerLook.Fired(-1);
        float total = 0f;
        int hit = 0;
        fmTmp.Clear();
        foreach (FmItem it in fmItems) if (it.e != null) fmTmp.Add(it.e);
        using (DamagePopup.Quiet())
        {
            foreach (EnermyController e in fmTmp)
            {
                if (e == null || e.IsDead) continue;
                Vector3 p = e.transform.position;
                float eachNow = each * FmHardMul(e);       // 갈래 콘크리트: 굳은 적 +50%
                Specials.Damage(e.gameObject, eachNow, fmBlade, 0f);
                total += eachNow;
                hit++;
                if (form == SpecialAbilities.KitJackhammer) e.Slow(0f, 0.3f);
                if (e.IsDead) { RunCounters.Both("b.crushkill"); SignatureSkills.FmPileKilled(p, FmCementOf(e) >= FmHardAt); FmPathBurst(p); }
            }
            // 들이받기: 삽날 상자의 보스 · 중간 보스에게 단일 피해 (현장 소장은 SignatureSkills 가 아닌 특수 능력이라 여기서)
            if (fmBossTarget != null)
            {
                float bash = Damage * FmBashRate * FmBossMul() * FmPathBossMul() * formPower;
                Specials.Damage(fmBossTarget, bash, fmBlade, 0.3f);
                FmPathBossHit();
                total += bash;
                hit++;
                RunCounters.Both("b.bash");
            }
        }
        // 압착 한 틱 = 더미 위에 합산 숫자 하나 (−324 ×12)
        if (hit > 0 && GameSettings.DamageNumbers != 2 && SpecialAbilities.SharedFx != null)
            SpecialAbilities.SharedFx.FloatText(center + Vector3.up * (fmR + 0.8f), "-" + Mathf.RoundToInt(total) + (hit > 1 ? " ×" + hit : ""), FmYellow, 4.5f, 0.1f);
        if (hit > 0)
        {
            Play("thump", 0.3f, 1.2f - 0.02f * Mathf.Min(M, 12));
            Fx.Spawn(FFx("fx_dust", "fx_sparkle"), center, 1.6f + fmR * 0.5f, FmDust, 18f);
            if (fmWall) Hostile.Shake(0.04f);
        }
        // 시멘트 (고유 스킬): 짓누를 때마다 더미 안 적이 굳음
        FmCementTick();       // 고유 스킬 시멘트 · 갈래 콘크리트
        SignatureSkills.FmCrushed(center, M, fmWall);
        FmBackBlade();
    }

    int FmCementOf(EnermyController e)
    {
        foreach (FmItem it in fmItems) if (it.e == e) return it.cement;
        return 0;
    }

    // 현장 소장(패시브): 보스 · 중간 보스에게 주는 들이받기 · 빠각 피해 +20% (진화 +35%)
    float FmBossMul()
    {
        if (Special == null || !Special.Has(SpecialAbilities.KitManager)) return 1f;
        return Special.IsEvolved(SpecialAbilities.KitManager) ? 1.35f : 1.2f;
    }

    // 뒷날(강화): 삽날 뒤에도 작은 날 — 뒤에서 닿는 적을 밀쳐내고 다치게 함 (공격력 40% · 0.5초에 한 번)
    void FmBackBlade()
    {
        if (augment != AugBackBlade || Time.time < fmBackAt) return;
        fmBackAt = Time.time + 0.5f;
        Vector3 at = transform.position - (Vector3)(fmBlade * 1.0f);
        bool any = false;
        Specials.Overlap(at, 1.4f, fmHits);
        for (int i = 0; i < fmHits.Count; i++)
        {
            Collider2D c = fmHits[i];
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            if (c.TryGetComponent(out EnermyController e) && (e.holder != null || e.IsDead)) continue;
            Specials.Damage(c.gameObject, Damage * 0.4f * formPower, -fmBlade, 3f);
            any = true;
        }
        if (any) { Fx.Spawn(FFx("fx_crunch", "fx_shock"), at, 2f, FmYellow, 20f); Play("thump", 0.3f, 1.3f); }
    }

    // ================================================================= 빠각
    void FmCrash(Vector3 at, GameObject target, bool onFence)
    {
        fmCrashAt = Time.time + 0.6f;
        int M = fmMass;
        float all = Damage * 0.6f * formPower;
        fmTmp.Clear();
        foreach (FmItem it in fmItems) if (it.e != null) fmTmp.Add(it.e);
        using (DamagePopup.Quiet())
        {
            foreach (EnermyController e in fmTmp) if (e != null && !e.IsDead) Specials.Damage(e.gameObject, all, fmBlade, 0f);
            if (target != null) { Specials.Damage(target, Damage * 0.25f * Mathf.Min(M, 12) * FmBossMul() * FmPathBossMul() * formPower, fmBlade, 0.5f); FmPathBossHit(); }
        }
        RunCounters.Both("b.crash");
        Fx.Spawn(FFx("fx_crunch", "fx_shock"), at, 3.2f, Color.white, 20f);
        Play("crack", 0.6f, 0.7f);
        Play("thump", 0.7f, 0.8f);
        Hostile.Shake(0.1f);
        if (M >= 6) Impact.Hit(ImpactTier.Attack, at, fmBlade);
        // 빠각! (카드): 부딪힌 자리에서 충격파 (공격력 80%)
        if (card[2] > 0)
        {
            float r = 1.2f + 0.4f * card[2];
            int waves = SignatureSkills.FmEvo("be.pile") ? 3 : 1;
            for (int w = 0; w < waves; w++) StartCoroutine(FmShockLater(at, r, Damage * 0.8f * formPower, 0.15f * w));
        }
        SignatureSkills.FmCrashed(at, onFence, M);
    }

    IEnumerator FmShockLater(Vector3 at, float r, float dmg, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        DamageCircle(at, r, dmg, 2f);
        Fx.Spawn("fx_shock", at, r * 2f, new Color(1f, 0.9f, 0.4f, 0.8f), 20f);
    }

    // ================================================================= 덤프 (떼기)
    void FmDump()
    {
        FmClean();
        int M = fmMass;
        if (fmItems.Count == 0) { fmMass = 0; return; }
        Vector2 dir = fmBlade;
        Vector3 origin = fmCenter;
        // 패드: 삽날 방향 15° 안에 적이 3마리 이상 모인 쪽이 있으면 그 가운데로 덤프 방향을 끌어당김 (사람이 그쪽을 향했을 때만 돕는 빈틈 끌림)
        if (GamePad.Active) dir = FmAssistDir(origin, dir);
        FmItem[] items = fmItems.ToArray();
        fmItems.Clear();
        fmMass = 0;
        // 교차 콘크리트 벽: 상한까지 찬 더미는 쏟는 대신 벽으로 굳음
        if (M >= FmPileMax && Paths.CrossOn(ForemanPaths.WallCross)) { FmMakeWall(items, origin, dir); return; }
        if (M < 3)
        {
            // 흩뿌림: 1.5칸 밀쳐 내고 0.3초 기절 · 피해 없음 (짧게 눌렀다 떼는 연타가 이득이 되지 않게)
            foreach (FmItem it in items)
            {
                if (it.rubble != null) { Destroy(it.rubble.gameObject); continue; }
                if (it.e == null) continue;
                Vector3 np = EnemyHold.FreeSpot(it.e.transform.position + (Vector3)(dir * 1.5f));
                EnemyHold.Place(it.e, np);
                EnemyHold.Release(it.e, this);
                it.e.Slow(0f, 0.3f);
            }
            Play("whoosh", 0.3f, 1.2f);
            return;
        }
        float dist = (3.5f + 0.25f * Mathf.Min(M, 14)) * (1f + 0.2f * card[6]) * (form == SpecialAbilities.KitBucket ? 1.3f : 1f) * SignatureSkills.FmDumpDistMul();
        dist = Mathf.Min(dist, 7f * (1f + 0.2f * card[6]) * (form == SpecialAbilities.KitBucket ? 1.3f : 1f));
        dist *= FmPathDumpDist();       // 갈래: 불도저 −40% · 콘크리트 −50% · 산사태 ×1.6
        float redumpMul = fmRedumpNext;       // 작업 지시의 두 번째 덤프 피해
        fmRedumpNext = 1f;
        fmDumps++;
        RunCounters.Both("b.dump");
        PlayerMotion.Of(this).Throw(0.25f, dir);
        // 새참(패시브): 덤프할 때마다 체력 회복 (1초에 한 번)
        if (Special != null && Special.Has(SpecialAbilities.KitSnack) && Time.time >= fmSnackAt)
        {
            fmSnackAt = Time.time + 1f;
            float heal = Special.IsEvolved(SpecialAbilities.KitSnack) ? 2f : 1f;
            player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + heal);
        }
        SignatureSkills.FmDumped(origin, dir, M, dist);
        StartCoroutine(FmDumpRoutine(items, origin, dir, M, dist, redumpMul));
    }

    Vector2 FmAssistDir(Vector3 origin, Vector2 dir)
    {
        Vector2 sum = Vector2.zero;
        int n = 0;
        Specials.Overlap(origin, 10f, fmHits);
        for (int i = 0; i < fmHits.Count; i++)
        {
            Collider2D c = fmHits[i];
            if (c == null || !c.CompareTag("enermy")) continue;
            if (c.TryGetComponent(out EnermyController e) && (e.holder != null || e.IsDead)) continue;
            Vector2 to = c.transform.position - origin;
            if (Vector2.Angle(dir, to) > 15f) continue;
            sum += to.normalized;
            n++;
        }
        return n >= 3 ? sum.normalized : dir;
    }

    // 산사태: 50° 부채꼴로 0.35초 미끄러짐 — 지나간 길의 적 (적마다 한 번) · 착지에서 피해 + 묻힘(기절)
    IEnumerator FmDumpRoutine(FmItem[] items, Vector3 origin, Vector2 dir, int M, float dist, float redumpMul)
    {
        const float T = 0.35f;
        int n = items.Length;
        Vector3[] off = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 cur = items[i].e != null ? items[i].e.transform.position : items[i].rubble != null ? items[i].rubble.position : origin;
            off[i] = cur - origin;
        }
        HashSet<int> swept = new HashSet<int>();
        float dmgMul = FmPathDumpDmg * redumpMul;       // 갈래 산사태 +40% · 작업 지시 두 번째 덤프
        float sw = FmPathSweepWidth;       // 갈래 산사태: 폭 x0.7
        float pathDmg = Damage * (0.6f + 0.04f * Mathf.Min(M, 12)) * formPower * dmgMul;
        float landDmg = Damage * (0.5f + 0.04f * Mathf.Min(M, 12)) * formPower * dmgMul;
        bool crashFence = false;
        bool bucket = form == SpecialAbilities.KitBucket;
        Vector3 head = origin;
        bool crashed = false;
        Vector3 crashAt = origin;
        fmDumping.Add(items);      // 2.0.0~ 검토: 날아가는 중 교대하면 fmItems 에는 이미 없는 잔해가 남았음 — 정리 대상에 둠
        try
        {
            Play("whoosh", 0.4f, 0.6f);
            Fx.Spawn(FFx("fx_landslide", "fx_dust"), origin + (Vector3)(dir * dist * 0.5f), Mathf.Max(3f, dist), new Color(1f, 1f, 1f, 0.85f), 14f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 6);
            for (float t = 0f; t < T; t += Time.deltaTime)
            {
                if (player == null || player.IsDying) yield break;
                float k = t / T;
                float s = dist * (1f - (1f - k) * (1f - k));
                Vector3 nh = origin + (Vector3)(dir * s);
                // 벽에 닿으면 멈추고 빠각
                Vector3 lead = nh + (Vector3)(dir * (fmR + 0.4f));
                if (FmBlocked(lead) || FmFenceAt(lead, 0.4f) != null) { crashed = true; crashAt = lead; crashFence = FmFenceAt(lead, 0.4f) != null; break; }
                head = nh;
                float lob = bucket ? Mathf.Sin(Mathf.PI * k) * 1.6f : 0f;
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = head + off[i] * (1f + 0.5f * k) + Vector3.up * lob;
                    if (items[i].e != null) { if (!items[i].e.IsDead) EnemyHold.Place(items[i].e, p); }
                    else if (items[i].rubble != null) items[i].rubble.position = p;
                }
                // 지나간 길의 적
                Specials.Overlap(head, (1.8f + fmR * 0.6f) * sw, fmHits);
                for (int i = 0; i < fmHits.Count; i++)
                {
                    Collider2D c = fmHits[i];
                    if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                    if (c.TryGetComponent(out EnermyController ee) && (ee.holder != null || ee.IsDead)) continue;
                    Vector2 to = c.transform.position - origin;
                    if (to.sqrMagnitude > 2.25f && Vector2.Angle(dir, to) > 25f * sw) continue;
                    if (!swept.Add(c.gameObject.GetInstanceID())) continue;
                    Specials.Damage(c.gameObject, pathDmg, dir, 3f);
                    FmPathSwept(c.gameObject, M);
                }
                yield return null;
            }
        }
        finally
        {
            // 착지: 모두 풀어 줌 (예외로 끊겨도 holder 가 남지 않게)
            Vector3 land = crashed ? crashAt - (Vector3)(dir * (fmR + 0.4f)) : head;
            try { FmLanding(items, land, off, M, landDmg, crashed, crashAt, dir, bucket, new FmDumpInfo { origin = origin, pathDmg = pathDmg, fence = crashFence, redump = redumpMul < 0.999f }); }
            catch (System.Exception ex) { Debug.LogException(ex); foreach (FmItem it in items) if (it.e != null) EnemyHold.Release(it.e, this); }
            fmDumping.Remove(items);
        }
    }
    readonly List<FmItem[]> fmDumping = new List<FmItem[]>(2);

    void FmLanding(FmItem[] items, Vector3 land, Vector3[] off, int M, float landDmg, bool crashed, Vector3 crashAt, Vector2 dir, bool bucket, FmDumpInfo info)
    {
        float stun = 0.6f + 0.2f * card[6];
        using (DamagePopup.Quiet())
        {
            for (int i = 0; i < items.Length; i++)
            {
                FmItem it = items[i];
                Vector3 spot = land + off[i] * 1.5f;
                if (it.rubble != null)
                {
                    // 잔해는 돌로 쏟아져 착지 자리 반지름 1칸에 공격력 50%
                    DamageCircle(it.rubble.position, 1f, Damage * 0.5f, 1.5f);
                    Fx.Spawn(FFx("fx_rubble", "fx_sparkle"), it.rubble.position, 1.6f, Color.white, 16f);
                    Destroy(it.rubble.gameObject);
                    continue;
                }
                EnermyController e = it.e;
                if (e == null) continue;
                if (e.IsDead) { EnemyHold.Release(e, this); continue; }
                EnemyHold.Place(e, EnemyHold.FreeSpot(spot));
                EnemyHold.Release(e, this);
                float d = landDmg * SignatureSkills.FmCementDumpMul(it.cement);
                Specials.Damage(e.gameObject, d, dir, 0f);
                if (!e.IsDead) e.Slow(0f, stun);      // 묻힘 (보스 0.5배 규칙은 Slow 가 아닌 잡몹만이라 해당 없음)
                else { RunCounters.Both("b.crushkill"); SignatureSkills.FmPileKilled(e.transform.position, it.cement >= FmHardAt); }
            }
        }
        if (crashed)
        {
            Fx.Spawn(FFx("fx_crunch", "fx_shock"), crashAt, 3.2f, Color.white, 20f);
            Play("crack", 0.6f, 0.7f);
            RunCounters.Both("b.crash");
            if (M >= 6) Impact.Hit(ImpactTier.Attack, crashAt, dir);
        }
        Fx.Spawn(FFx("fx_dust", "fx_explosion"), land, 4f + M * 0.2f, FmDust, 14f);
        Play("thump", 0.7f, 0.7f);
        Hostile.Shake(0.1f + 0.01f * Mathf.Min(M, 12));
        // 굴착기 버킷(형태): 착지할 때 터짐 (공격력 80% · 반지름 1.6칸)
        if (bucket)
        {
            DamageCircle(land, 1.6f, Damage * 0.8f * formPower, 3f);
            Fx.Spawn("fx_explosion", land, 3.4f, new Color(1f, 0.85f, 0.35f), 20f);
        }
        // 안전 펜스(강화): 덤프한 자리에 2초 동안 짧은 바리케이드 (폭 2칸)
        if (augment == AugFence)
        {
            Vector2 side = new Vector2(-dir.y, dir.x);
            FmAddFence(land + (Vector3)(dir * 0.5f), side, 2f, 1f);
        }
        FmPathLanded(info, land, dir, M, crashed, crashAt, items);       // 갈래 효과
        SignatureSkills.FmDumpLanded(land, dir, M);
    }

    // ================================================================= 정리 · 위치 (LateUpdate)
    void FmClean()
    {
        int mass = 0;
        for (int i = fmItems.Count - 1; i >= 0; i--)
        {
            FmItem it = fmItems[i];
            if (it.rubble != null)
            {
                if (Time.time >= it.until) { Destroy(it.rubble.gameObject); fmItems.RemoveAt(i); continue; }
            }
            else if (it.e == null || it.e.IsDead || it.e.holder != (object)this)
            {
                // 더미 안에서 쓰러진 적은 잔해로 남음 (카드 잔해)
                fmItems.RemoveAt(i);
                continue;
            }
            mass += it.mass;
        }
        fmMass = mass;
    }

    void FmReleaseAll(bool scatter)
    {
        foreach (FmItem it in fmItems)
        {
            if (it.rubble != null) { Destroy(it.rubble.gameObject); continue; }
            if (it.e != null) EnemyHold.Release(it.e, this);
        }
        fmItems.Clear();
        fmMass = 0;
        EnemyHold.ReleaseAll(this);
    }

    // 잔해 한 칸을 더미에 더함 (카드 잔해 · 콘크리트 진화) — 5초 동안 더미 무게를 채움
    public void FmAddRubble(Vector3 at, int mass = 1, bool hardened = false)
    {
        bool pathRubble = hardened && FmOn(ForemanPaths.Concrete);       // 갈래 콘크리트: 굳은 적이 쓰러지면 잔해
        if ((card[1] <= 0 && !pathRubble) || !fmHolding) return;
        int have = 0;
        foreach (FmItem it in fmItems) if (it.rubble != null) have += it.mass;
        if (have + mass > (pathRubble ? Mathf.Max(FmRubbleMax, 6) : FmRubbleMax) || fmMass + mass > FmPileMax) return;
        GameObject go = new GameObject("FmRubble");
        go.transform.position = at;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite[] f = Fx.Frames("fx_rubble");
        if (f != null && f.Length > 0) { sr.sprite = f[Random.Range(0, f.Length)]; float h = sr.sprite.bounds.size.y; go.transform.localScale = Vector3.one * (h > 0f ? 0.9f * mass / h : 1f); }
        sr.sortingLayerID = body.sortingLayerID;
        sr.sortingOrder = body.sortingOrder + 1;
        fmItems.Add(new FmItem { mass = mass, rubble = go.transform, until = Time.time + ForemanPaths.RubbleLife });
        fmMass += mass;
    }

    void FmLate()
    {
        if (player == null) return;
        if (player.IsDying) { FmReleaseAll(false); FmClearFences(); return; }
        Vector3 pos = transform.position;
        Vector2 step = pos - fmLastPos;
        fmLastPos = pos;
        if (step.sqrMagnitude > 0.0004f && Time.timeScale > 0f) fmMoveDir = step.normalized;
        if (fmItems.Count > 0) { FmClean(); FmLayout(); }
        else { fmMass = 0; fmR = 0f; fmCenter = pos + (Vector3)(fmBlade * (FmBladeAt + 0.9f)); }
        FmTickFences();
        FmBlockShots();
        FmOrbit();
        FmPathTick();
        FmDrawPreview();
    }

    void FmLayout()
    {
        int n = fmItems.Count;
        float R = 0.45f * Mathf.Sqrt(Mathf.Max(1, fmMass));
        fmR = R;
        float reach = FmBladeAt + 0.5f + R + (SignatureSkills.FmEvo("be.dozer") ? 0.5f : 0f);
        Vector3 me = transform.position;
        Vector3 c = me + (Vector3)(fmBlade * reach);
        // 벽 · 경계 안으로 당김 (플레이어 쪽으로)
        for (int guard = 0; guard < 8 && (FmBlocked(c) || FmFenceAt(c, R * 0.3f) != null) && reach > 1f; guard++)
        {
            reach -= 0.4f;
            c = me + (Vector3)(fmBlade * reach);
        }
        fmCenter = c;
        float ang = Mathf.Atan2(fmBlade.y, fmBlade.x);
        float k = Time.deltaTime > 0f ? 1f - Mathf.Exp(-Time.deltaTime / 0.08f) : 0f;
        Vector2 rot = fmBlade, rotY = new Vector2(-fmBlade.y, fmBlade.x);
        for (int i = 0; i < n; i++)
        {
            float rr = R * Mathf.Sqrt((i + 0.5f) / n), a = 2.4f * i;
            float lx = rr * Mathf.Cos(a), ly = rr * Mathf.Sin(a) * 0.88f;
            Vector3 target = c + (Vector3)(rot * lx + rotY * ly);
            FmItem it = fmItems[i];
            if (it.e != null) EnemyHold.Place(it.e, Vector3.Lerp(it.e.transform.position, target, k));
            else if (it.rubble != null) it.rubble.position = Vector3.Lerp(it.rubble.position, target, k);
        }
        _ = ang;
    }

    static bool FmBlocked(Vector3 p) => Hostile.IsWall(p) || !Hostile.InArena(p);

    // ================================================================= 투사체 막기 (안전 수칙)
    void FmBlockShots()
    {
        bool wrap = Time.time < fmWrapUntil;                    // 갈래 안전 제일 공명 II: 맞은 뒤 0.6초 반장 둘레를 감쌈
        bool anyPath = FmOn(ForemanPaths.Safety);              // 갈래 안전 제일: 무게와 상관없이 막음
        if ((card[5] <= 0 && !anyPath && !wrap) || Time.time < fmBlockAt) return;
        bool pile = fmItems.Count > 0;
        if (!pile && !wrap) return;
        fmBlockAt = Time.time + 0.05f;
        int need = anyPath ? 0 : Mathf.Max(0, 8 - 2 * card[5]);
        bool pileOn = pile && fmMass >= need && (card[5] > 0 || anyPath);
        if (!pileOn && !wrap) return;
        List<HostileProjectile> list = HostileProjectile.Live;
        Vector3 me = transform.position;
        int reflected = 0;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            HostileProjectile hp = list[i];
            if (hp == null) continue;
            Vector3 hpPos = hp.transform.position;
            bool hit = false;
            if (pileOn)
            {
                Vector2 rel = hpPos - fmCenter;
                float r = fmR + 0.3f + hp.radius * 0.5f;
                // 앞쪽 반원만 (뒤에서 온 탄은 못 막음)
                hit = rel.sqrMagnitude <= r * r && Vector2.Dot((Vector2)(hpPos - me), fmBlade) >= 0f;
            }
            if (!hit && wrap)
            {
                float r2 = 1.5f + hp.radius * 0.5f;
                hit = ((Vector2)(hpPos - me)).sqrMagnitude <= r2 * r2;
            }
            if (!hit) continue;
            Vector3 at = hpPos;
            Vector2 hdir = hp.dir;
            Destroy(hp.gameObject);
            Fx.Spawn("fx_sparkle", at, 1.4f, FmYellow, 24f);
            Play("clank", 0.3f, 0.8f);
            bool first = reflected < 4;
            if (first) { reflected++; SignatureSkills.FmBlocked(at, hdir); }
            FmPathBlocked(at, hdir, first);
        }
    }

    // ================================================================= 철거 쇠공(형태): 더미 둘레를 사슬 쇠공이 돎
    void FmOrbit()
    {
        if (form != SpecialAbilities.KitWreckBall || fmItems.Count == 0)
        {
            if (fmOrbitBall != null) fmOrbitBall.SetActive(false);
            return;
        }
        if (fmOrbitBall == null)
        {
            fmOrbitBall = new GameObject("FmOrbitBall");
            SpriteRenderer sr = fmOrbitBall.AddComponent<SpriteRenderer>();
            Sprite[] f = Fx.Frames("fx_wreckball");
            if (f != null && f.Length > 0) { sr.sprite = f[0]; float h = sr.sprite.bounds.size.y; fmOrbitBall.transform.localScale = Vector3.one * (h > 0f ? 1.1f / h : 1f); }
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder + 3;
        }
        fmOrbitBall.SetActive(true);
        fmOrbitAngle += 300f * Time.deltaTime;
        float rad = fmR + 1f;
        fmOrbitBall.transform.position = fmCenter + new Vector3(Mathf.Cos(fmOrbitAngle * Mathf.Deg2Rad), Mathf.Sin(fmOrbitAngle * Mathf.Deg2Rad)) * rad;
        if (Time.time < fmOrbitAt) return;
        fmOrbitAt = Time.time + 0.1f;
        // 닿는 적을 침 (공격력 60% · 적마다 0.5초에 한 번)
        Specials.Overlap(fmOrbitBall.transform.position, 0.9f, fmHits);
        for (int i = 0; i < fmHits.Count; i++)
        {
            Collider2D c = fmHits[i];
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            if (!c.TryGetComponent(out EnermyController e)) { if (!FmSeenOk(c.GetInstanceID(), c)) continue; Specials.Damage(c.gameObject, Damage * 0.6f * formPower, Vector3.zero, 0.5f); continue; }
            if (e.holder != null || e.IsDead || !FmSeenOk(e.GetInstanceID(), e)) continue;
            Specials.Damage(e.gameObject, Damage * 0.6f * formPower, ((Vector2)(e.transform.position - fmCenter)).normalized, 1f);
        }
    }

    readonly Dictionary<int, float> fmHitAt = new Dictionary<int, float>();
    bool FmSeenOk(int id, Component c)
    {
        if (fmHitAt.TryGetValue(id, out float t) && Time.time < t) return false;
        if (fmHitAt.Count > 200) fmHitAt.Clear();
        fmHitAt[id] = Time.time + 0.5f;
        return true;
    }

    // ================================================================= 바리케이드
    FmFence FmAddFence(Vector3 pos, Vector2 along, float width, float life, float thick = 0.3f)
    {
        while (fmFences.Count >= FmFenceCap) FmCollapse(fmFences[0], false);
        FmFence f = new FmFence { pos = pos, along = along.normalized, half = width * 0.5f, thick = thick, until = Time.time + life };
        f.normal = new Vector2(-f.along.y, f.along.x);
        GameObject go = new GameObject("FmFence");
        go.transform.position = pos;
        float rot = Mathf.Atan2(f.along.y, f.along.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rot);
        Sprite[] frames = Fx.Frames("fx_barricade");
        if (frames != null && frames.Length > 0)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            float w = sr.sprite.bounds.size.x;
            go.transform.localScale = Vector3.one * (w > 0f ? width / w : 1f);
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder - 1;
            f.sr = sr;
        }
        else
        {
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, pos - (Vector3)(f.along * f.half));
            lr.SetPosition(1, pos + (Vector3)(f.along * f.half));
            lr.startWidth = lr.endWidth = 0.5f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = FmYellow;
            f.line = lr;
        }
        f.go = go;
        fmFences.Add(f);
        Fx.Spawn(FFx("fx_dust", "fx_sparkle"), pos, 2f, FmDust, 18f);
        return f;
    }

    FmFence FmFenceAt(Vector3 p, float margin)
    {
        for (int i = 0; i < fmFences.Count; i++)
        {
            FmFence f = fmFences[i];
            Vector2 rel = p - f.pos;
            if (Mathf.Abs(Vector2.Dot(rel, f.along)) <= f.half + margin && Mathf.Abs(Vector2.Dot(rel, f.normal)) <= f.thick + margin) return f;
        }
        return null;
    }

    void FmCollapse(FmFence f, bool ruin)
    {
        fmFences.Remove(f);
        if (f.go != null)
        {
            Sprite[] frames = Fx.Frames("fx_barricade");
            if (f.sr != null && frames != null && frames.Length > 2) f.sr.sprite = frames[2];
            Destroy(f.go, f.sr != null ? 0.35f : 0f);
        }
        if (ruin) { SignatureSkills.FmFenceGone(f.pos, f.along); FmPathFenceGone(f); }
        // 무너진 담(패시브): 바리케이드가 사라질 때 둘레 2칸 적에게 피해 (공격력 100% · 진화 180%)
        if (ruin && Special != null && Special.Has(SpecialAbilities.KitRuin))
        {
            DamageCircle(f.pos, 2f, Damage * (Special.IsEvolved(SpecialAbilities.KitRuin) ? 1.8f : 1f), 2f);
            Fx.Spawn(FFx("fx_dust", "fx_explosion"), f.pos, 3.5f, FmDust, 16f);
            Play("thump", 0.4f, 0.9f);
        }
    }

    void FmClearFences()
    {
        for (int i = fmFences.Count - 1; i >= 0; i--) if (fmFences[i].go != null) Destroy(fmFences[i].go);
        fmFences.Clear();
    }

    float fmFenceTickAt;
    // 바리케이드는 물리 층을 쓰지 않고 0.1초마다 안에 든 잡몹을 가까운 바깥면으로 밀어냄 · 보스는 부수고 지나감 · 플레이어는 지나감(갇히지 않게)
    void FmTickFences()
    {
        if (fmFences.Count == 0) return;
        for (int i = fmFences.Count - 1; i >= 0; i--)
        {
            FmFence f = fmFences[i];
            if (f.go == null || Time.time >= f.until) { FmCollapse(f, true); continue; }
        }
        if (Time.time < fmFenceTickAt) return;
        fmFenceTickAt = Time.time + 0.1f;
        for (int i = fmFences.Count - 1; i >= 0; i--)
        {
            FmFence f = fmFences[i];
            Specials.Overlap(f.pos, f.half + 1.5f, fmHits);
            bool broken = false;
            for (int j = 0; j < fmHits.Count && !broken; j++)
            {
                Collider2D c = fmHits[j];
                if (c == null) continue;
                bool boss = c.CompareTag("boss");
                if (!boss && !c.CompareTag("enermy")) continue;
                EnermyController e = boss ? null : c.GetComponent<EnermyController>();
                if (!boss && (e == null || e.IsDead || e.holder != null || e.swarm != null)) continue;
                Vector2 rel = c.transform.position - f.pos;
                float u = Vector2.Dot(rel, f.along), v = Vector2.Dot(rel, f.normal);
                float r = FmBodyR(c);
                if (Mathf.Abs(u) > f.half + r * 0.4f || Mathf.Abs(v) > f.thick + r) continue;
                if (boss || e.IsMidBoss)
                {
                    // 보스 · 중간 보스는 부수고 지나감 (그 보스에게 공격력 100% 빠각)
                    Specials.Damage(c.gameObject, Damage * UltMul * FmBossMul(), f.normal, 0.5f);
                    Fx.Spawn(FFx("fx_crunch", "fx_shock"), f.pos, 3f, Color.white, 20f);
                    Play("crack", 0.5f, 0.8f);
                    FmCollapse(f, true);
                    broken = true;
                    continue;
                }
                float sgn = v >= 0f ? 1f : -1f;
                Vector3 np = f.pos + (Vector3)(f.along * u + f.normal * sgn * (f.thick + r + 0.05f));
                if (!FmBlocked(np)) { e.transform.position = np; }
                if (FmSlowing) e.Slow(0.7f, 0.5f);
                SignatureSkills.FmFenced(e, f.pos);
            }
        }
    }

    // ================================================================= 범위 표시 · 머리 위 무게 칸
    void FmDrawPreview()
    {
        bool show = fmHolding && fmMass >= 3 && !PlayerController.Cinematic;
        if (!show) { if (fmPreview != null) fmPreview.enabled = false; return; }
        float dist = Mathf.Min(7f, 3.5f + 0.25f * Mathf.Min(fmMass, 14)) * (1f + 0.2f * card[6]) * (form == SpecialAbilities.KitBucket ? 1.3f : 1f) * FmPathDumpDist();
        Vector3 o = fmCenter;
        Vector2 l = Rotate2(fmBlade, 25f * FmPathSweepWidth), r = Rotate2(fmBlade, -25f * FmPathSweepWidth);
        // 2.0.0~ 쏟을 범위: 얇은 단색 V 선 → 노랑 · 검정이 번갈아 드는 안전 테이프 점선 두 가장자리 + 끝 둘레 작은 점 호
        //   (판정과 같은 각 · 거리 · 안 그린 프레임엔 저절로 숨음)
        Color c = HeroAim.ColorOf(HeroAimKind.Foreman);
        Color tape = new Color(c.r, c.g, c.b, 0.8f), dark = new Color(0.12f, 0.1f, 0.08f, 0.8f);
        PixelMarks pm = PixelMarks.Ensure(ref fmPx, this);
        pm.DotPath(o, o + (Vector3)(l * dist), 0f, tape, 0.45f, false, dark);
        pm.DotPath(o, o + (Vector3)(r * dist), 0f, tape, 0.45f, false, dark);
        float half = Vector2.Angle(fmBlade, l);
        int arcDots = Mathf.Clamp(Mathf.CeilToInt(2f * half * Mathf.Deg2Rad * dist / 0.5f), 2, 24);
        for (int i = 1; i < arcDots; i++)
            pm.Put(PixelMarkArt.DotSmall, (Vector2)o + Rotate2(fmBlade, Mathf.Lerp(half, -half, i / (float)arcDots)) * dist, new Color(c.r, c.g, c.b, 0.6f));
    }

    static Vector2 Rotate2(Vector2 v, float deg)
    {
        float a = deg * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }

    // 머리 위 막대 (CharacterKit.Rules): 더미 무게 / 상한 — 상한 근처 주황, 가득 차면 노랑 · 검정 줄무늬처럼 깜빡임
    void FmRuleBar()
    {
        int max = FmPileMax;
        bool show = fmHolding || fmItems.Count > 0;
        float k = max > 0 ? fmMass / (float)max : 0f;
        Color c = k >= 0.999f ? Color.Lerp(FmYellow, Color.black, 0.5f + 0.5f * Mathf.Sin(Time.time * 18f))
                : k >= 0.75f ? FmOrange : CharacterData.Ally(CharacterId.Foreman);
        DrawRuleBar(k, c, show, "무게", fmMass, max);
    }

    // 맞는 피해 배율: 안전 제일(고유 스킬)은 SignatureSkills.Taken 이 처리 — 여기서는 없음
    float FmTakenMul() => 1f;

    void OnDestroyForeman()
    {
        EnemyHold.ReleaseAll(this);
        foreach (FmItem it in fmItems) if (it.rubble != null) Destroy(it.rubble.gameObject);
        fmItems.Clear();
        foreach (FmItem[] arr in fmDumping) foreach (FmItem it in arr) if (it.rubble != null) Destroy(it.rubble.gameObject);
        fmDumping.Clear();
        FmClearFences();
        if (fmPreview != null) Destroy(fmPreview.gameObject);
        if (fmOrbitBall != null) Destroy(fmOrbitBall);
        if (fmUltLine != null) Destroy(fmUltLine.gameObject);
        if (fmPx != null) Destroy(fmPx.gameObject);
        if (fmUltRing != null) Destroy(fmUltRing.gameObject);
        for (int i = 0; i < fmUltBounce.Length; i++) if (fmUltBounce[i] != null) Destroy(fmUltBounce[i].gameObject);
    }
}
