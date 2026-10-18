using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 도적 고유 스킬 · 진화
//   r.smoke 연막 · r.feast 피의 향연 · r.burst 출혈 폭발 · r.ambush 급습 표창 · r.boomerang 회전 표창
//   r.path 궤적 칼날 · r.practice 던지기 연습 · r.overflow 넘치는 기세 · r.knot 그림자 매듭 · r.rain 빈 주머니의 비
public partial class SignatureSkills
{
    static readonly Color Shade = new Color(0.7f, 0.5f, 1f);

    readonly List<SigZone> smokes = new List<SigZone>();
    readonly Dictionary<GameObject, float> bleedSince = new Dictionary<GameObject, float>();
    readonly Dictionary<GameObject, float> knots = new Dictionary<GameObject, float>();
    float ambushUntil;
    GameObject practiceTarget;
    int practiceStacks;
    float practiceAt;
    float acrobatAt, peakAt;
    bool gaugeWasFull;

    // ---------------- 게임 코드가 부르는 도적 훅
    public static void DashStart(Vector3 from) { if (Instance != null && Instance.who == CharacterId.Rogue) Instance.OnDashStart(from); }
    public static void DashEnd(Vector3 from, Vector3 to, HashSet<Collider2D> cut) { if (Instance != null && Instance.who == CharacterId.Rogue) Instance.OnDashEnd(from, to, cut); }

    float RogueRange => CharacterData.Current.range > 0f ? CharacterData.Current.range : 12f;

    void OnDashStart(Vector3 from)
    {
        RoguePaths.Cur?.DashStart(from);        // 2026-10-19~ 갈래: 미끼 분신 · 모아 둔 돌진 · 폭풍 질주 (Paths.Rogue)
        int s = L("r.smoke");
        if (s <= 0) return;
        smokes.RemoveAll(z => z == null);
        // 1.2.3~ 판정 반지름도 그림 배율(Fs: 진화하면 1.7배)을 따름 — 예전엔 그림만 커지고 판정은 3.5 그대로라 연막 안에 서 있어도 「환영 연막」이 안 걸렸음
        SigZone z = Zone(from, 3.5f * Fs("r.smoke"), V(s, 2f, 1f), 0.2f, zone =>
        {
            foreach (Collider2D c in Inside(zone))
            {
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null) e.Slow(0.3f, 0.35f);
            }
        }, "fx_cloud", Big("r.smoke") ? new Color(0.55f, 0.4f, 0.8f, 0.8f) : new Color(0.35f, 0.3f, 0.45f, 0.75f), 8.5f * Fs("r.smoke"));
        Flair("r.smoke", from, 4f, Shade);
        smokes.Add(z);
        Fx.Spawn("fx_smoke", from, 5f, new Color(0.5f, 0.45f, 0.6f), 14f);
    }

    void OnDashEnd(Vector3 from, Vector3 to, HashSet<Collider2D> cut)
    {
        if (L("r.ambush") > 0) ambushUntil = Time.time + 2f;
        // 궤적 칼날: 지나간 길에 칼날 자국
        int p = L("r.path");
        if (p > 0 && (to - from).sqrMagnitude > 0.5f)
        {
            float dmg = Atk * 0.5f * P("r.path");       // 1.1.2~ 칼날 폭풍길: ×1.6
            SigZone z = Zone((from + to) * 0.5f, 1.2f, V(p, 1.5f, 0.5f), 0.3f, null);
            z.segment = true;
            z.a = from;
            z.b = to;
            float shotAt = 0f;
            z.onTick = zone =>
            {
                foreach (Collider2D c in Inside(zone)) Deal(c.gameObject, dmg, Vector3.zero, 0f);
                // 칼날 폭풍길: 자국에서 표창이 솟구쳐 가까운 적에게
                if (E("re.road") && zone.age >= shotAt)
                {
                    shotAt = zone.age + 0.3f;
                    Vector3 at = Vector3.Lerp(zone.a, zone.b, Random.value);
                    List<Transform> near = Nearest(at, 10f, 1);
                    if (near.Count > 0) Shot(at, ((Vector2)(near[0].position - at)).normalized, Atk * 0.8f, 1, 30f, 12f, "fx_shuriken", 0.9f, Vivid(new Color(0.75f, 0.45f, 1f)), true);
                }
            };
            LineRenderer lr = Hostile.NewLine("BladePath", Big("r.path") ? new Color(1f, 0.75f, 1f, 0.85f) : new Color(0.75f, 0.45f, 1f, 0.6f), 0.5f * Fs("r.path"), 3);
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.transform.SetParent(z.transform, true);
        }
        // 그림자 매듭: 돌진으로 벤 적들을 4초 동안 묶음
        int k = L("r.knot");
        if (k > 0 && cut != null && cut.Count >= 2)
        {
            foreach (Collider2D c in cut)
                if (c != null) knots[c.gameObject] = Time.time + 4f;
            DrawKnots(cut);
        }
        RoguePaths.Cur?.DashEnd(from, to, cut);     // 2026-10-19~ 갈래: 출혈 즉시 폭발 · 게이지 반환 · 길 교차 폭발 · 연속 돌진 (맨 끝 — 다시 돌진할 수 있음)
    }

    void DrawKnots(HashSet<Collider2D> cut)
    {
        Collider2D prev = null;
        foreach (Collider2D c in cut)
        {
            if (c == null) continue;
            if (prev != null) Fx.Beam(prev.transform.position, c.transform.position, 0.25f * Fs("r.knot"), new Color(0.55f, 0.3f, 0.9f, 0.8f), 0.6f, 12);
            prev = c;
        }
    }

    void RogueAttack(Vector3 start, Vector2 dir)
    {
        // 회전 표창: 방금 던진 그 평타 표창이 화면 끝(최소 RogueRange × 0.8 · 최대 × FarMul)까지 날아가(또는 관통 수가 다하거나 벽에 닿으면 그 자리에서) 꺾여 돌아옴
        //   예전엔 0.35초 뒤 그 자리에 새 표창을 만들어 날려 엉뚱한 데서 나타나 보였음
        Bullet thrown = Kit != null ? Kit.TakeRogueThrow() : null;
        int b = L("r.boomerang");
        // 회귀 칼날 갈래면 이미 모든 표창이 돌아옴 (CharacterKit.RpShot 이 회전 표창 피해까지 실어 붙임) — 두 번 붙이지 않음
        if (b > 0 && thrown != null && thrown.isActiveAndEnabled && Alive && thrown.GetComponent<ReturningShuriken>() == null)      // 풀로 돌아간 표창(꺼짐)에는 붙이지 않음 (다시 쓰일 때 남지 않게)
            thrown.gameObject.AddComponent<ReturningShuriken>().Throw(thrown, transform, start, RogueRange * 0.8f, RogueRange * ReturningShuriken.FarMul, Atk * V(b, 0.4f, 0.15f), E("re.return"));
        // 처형자의 길: 급습 시간 동안 꿰뚫는 그림자 표창이 함께
        if (E("re.executioner") && Time.time < ambushUntil)
        {
            Bullet s = Shot(start, dir, Atk * 1f, 999, 34f, RogueRange * 1.3f, "fx_shuriken", 1.1f, Vivid(Shade), true);      // 1.1.2~ 80 → 100%
            if (s != null) s.hitOnce = new HashSet<int>();
        }
    }

    // 귀환하는 칼날: 돌아온 표창 한 번(평타 한 번)마다 탄창에 한 발
    public void ShurikenBack()
    {
        if (player != null && who == CharacterId.Rogue && (Sp == null || !Sp.RefundHeldAmmo(1)) && player.NowBullet < player.MaxBullet) player.NowBullet++;     // 들고 있는 무기의 탄창에 (2.1.8)
    }

    // 표창을 다 던져 주머니가 빔 (장전 시작)
    void RogueEmpty()
    {
        int r = L("r.rain");
        if (r <= 0) return;
        List<Transform> near = Nearest(transform.position, 11f, 6);
        int n = (int)V(r, 4f, 2f);
        for (int i = 0; i < n; i++)
        {
            Vector3 at = near.Count > 0 ? near[i % near.Count].position + (Vector3)(Random.insideUnitCircle * 0.8f)
                                        : transform.position + (Vector3)(Random.insideUnitCircle * 6f);
            StartCoroutine(Drop(at, "fx_shuriken", 1.4f * Fs("r.rain"), Tint("r.rain", new Color(0.8f, 0.9f, 1f)), 1.2f, Atk * P("r.rain"), i * 0.06f));       // 1.1.2~ 사냥의 절정: ×1.5
        }
        Hostile.Play("whoosh", 0.5f, 1.6f);
    }

    float RogueOutgoing(GameObject target)
    {
        float mul = 1f;
        int a = L("r.ambush");
        if (a > 0 && Time.time < ambushUntil) mul *= 1f + V(a, 0.3f, 0.15f);
        int p = L("r.practice");
        if (p > 0 && !Dealing && !DotTick)
        {
            if (target == practiceTarget && Time.time - practiceAt < 2f) practiceStacks = Mathf.Min(5, practiceStacks + 1);
            else { practiceTarget = target; practiceStacks = 0; }
            practiceAt = Time.time;
            mul *= 1f + practiceStacks * V(p, 0.04f, 0.02f);
        }
        return mul;
    }

    void RogueHit(GameObject target, float dmg, bool killed, bool proc)
    {
        Bleed bleed = target.GetComponent<Bleed>();
        // 피의 향연
        if (killed && bleed != null && L("r.feast") > 0) Heal(V(L("r.feast"), 2f, 1f));
        // 출혈 폭발: 출혈이 3초 넘게 이어지면 남은 출혈이 한꺼번에
        int bu = L("r.burst");
        if (bu > 0 && !killed && bleed != null)
        {
            if (!bleedSince.TryGetValue(target, out float since)) bleedSince[target] = Time.time;
            else if (Time.time - since >= 3f)
            {
                bleedSince.Remove(target);
                float burst = bleed.dps * Mathf.Max(0f, bleed.until - Time.time) * V(bu, 0.8f, 0.2f) * P("r.burst");     // 1.1.2~ 피바다: ×1.5
                Vector3 p = target.transform.position;
                Destroy(bleed);
                if (burst > 0f)
                {
                    using var tier = EvoFx("r.burst");
                    Deal(target, burst, Vector3.zero, 0f);
                    if (E("re.sea")) { Circle(p, 3f, burst, 0.5f); Heal(3f); }
                    Fx.Spawn("fx_bleed", p, 3.4f * Fs("r.burst"), Color.white, 16f);
                    Fx.Spawn("fx_shock", p, 4f * Fs("r.burst"), Blood, 22f);
                    Flair("r.burst", p, 3f, Blood);
                    Hostile.Play("crack", 0.5f, 0.9f);
                }
            }
        }
        if (bleed == null) bleedSince.Remove(target);
        if (bleedSince.Count > 200) bleedSince.Clear();

        // 그림자 매듭: 묶인 적이 받은 피해를 나머지도
        int k = L("r.knot");
        if (k > 0 && !Dealing && knots.TryGetValue(target, out float until) && until > Time.time)
        {
            float share = dmg * V(k, 0.15f, 0.07f);
            foreach (var kv in new List<KeyValuePair<GameObject, float>>(knots))
            {
                if (kv.Key == null || kv.Key == target || kv.Value < Time.time) continue;
                Deal(kv.Key, share, Vector3.zero, 0f);
            }
            if (killed && E("re.executioner")) ambushUntil = Time.time + 2f;
        }
        if (killed) knots.Remove(target);

        // 칼날 곡예: 던지기 연습이 최대일 때 칼날 고리
        if (proc && E("re.acrobat") && practiceStacks >= 5 && Time.time >= acrobatAt)
        {
            acrobatAt = Time.time + 0.5f;
            Vector3 p = target.transform.position;
            using var tier = Fx.Tier(3);
            Circle(p, 2.6f, Atk * 2f, 0.6f);        // 1.1.2~ 150 → 200%
            Fx.Spawn("fx_spinslash", p, 8f, new Color(1f, 0.8f, 1f, 0.95f), 24f);
            Flair(null, p, 3.5f, Shade);
        }
        // 사냥의 절정: 게이지가 가득 찬 동안 처치하면 표창 비
        // 2.2.1: 필살기 출혈 돌진으로 잡은 적도 (돌진을 시작하며 게이지가 비어 예전엔 돌진 처치에는 안 터졌음)
        if (killed && E("re.peak") && Time.time >= peakAt)
        {
            SkillGauge g = Cache<SkillGauge>.Get;
            if ((g != null && g.IsFull()) || (Kit != null && Kit.Dashing))
            {
                peakAt = Time.time + 0.3f;
                Vector3 p = target.transform.position;
                for (int i = 0; i < 4; i++)
                    StartCoroutine(Drop(p + (Vector3)(Random.insideUnitCircle * 2.5f), "fx_shuriken", 1.5f, Vivid(Shade), 1.1f, Atk, i * 0.08f));
            }
        }
    }

    // 환영 연막: 연막 안에 서 있는 동안 받는 피해 -90%
    // 1.2.3 모두 빗나감(사실상 무적) → 40% 확률 빗나감 → 1.2.5~ 사용자 결정으로 확률 대신 받는 피해 90% 감소 (늘 같은 효과 · 피격 연출은 그대로)
    public const float PhantomTakenMul = 0.1f;
    float RogueTaken(float dmg)
    {
        if (!E("re.phantom")) return dmg;
        foreach (SigZone z in smokes)
            if (z != null && z.Contains(transform.position)) return dmg * PhantomTakenMul;
        return dmg;
    }

    void RogueTick()
    {
        // 넘치는 기세: 게이지가 가득 차는 순간 표창이 사방으로
        int o = L("r.overflow");
        SkillGauge g = Cache<SkillGauge>.Get;
        bool full = g != null && g.IsFull();
        if (o > 0 && full && !gaugeWasFull)
        {
            int n = (int)V(o, 6f, 3f);
            Vector3 p = transform.position;
            for (int i = 0; i < n; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, i * 360f / n) * Vector2.right;
                Shot(p, d, Atk * P("r.overflow"), 1, 26f, RogueRange, "fx_shuriken", 0.7f * Fs("r.overflow"), Tint("r.overflow", Color.white), true);      // 1.1.2~ 사냥의 절정: ×1.5
            }
            using var tier = EvoFx("r.overflow");
            Fx.Spawn("fx_shock", p, 6f * Fs("r.overflow"), Shade, 20f);
            Flair("r.overflow", p, 5f, Shade);
            Hostile.Play("whoosh", 0.6f, 1.4f);
        }
        gaugeWasFull = full;
    }

    // ================================================================= 갈래 효과가 쓰는 도적 도우미 (2026-10-19 · Paths/Paths.Rogue.cs · CharacterKit.Rogue.cs)
    // 그림자 공명 I: 미끼 분신이 사라진 자리에 연막 (적 느리게 · 환영 연막 · 공명 II 치명 판정에 같이 셈)
    public void RoguePathSmoke(Vector3 at)
    {
        if (!Alive) return;
        smokes.RemoveAll(z => z == null);
        while (smokes.Count >= 12) { if (smokes[0] != null) Destroy(smokes[0].gameObject); smokes.RemoveAt(0); }     // 연막 상한
        SigZone z = Zone(at, 3f * Fs("r.smoke"), 2.5f, 0.2f, zone =>
        {
            foreach (Collider2D c in Inside(zone))
            {
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null) e.Slow(0.3f, 0.35f);
            }
        }, "fx_cloud", new Color(0.4f, 0.32f, 0.55f, 0.7f), 7.5f * Fs("r.smoke"));
        smokes.Add(z);
        Fx.Spawn("fx_smoke", at, 4f, new Color(0.5f, 0.45f, 0.6f), 14f);
    }

    // 이 자리가 연막 안인가 (그림자 공명 II)
    public bool RogueInSmoke(Vector3 p)
    {
        foreach (SigZone z in smokes)
            if (z != null && z.Contains(p)) return true;
        return false;
    }

    // 회귀 칼날 공명 I: 왕복으로 같은 적을 맞힘 → 던지기 연습 중첩을 더 (명중 피해 훅의 +1 과 합쳐 2)
    public void RoguePracticeBump(GameObject target, int n)
    {
        if (target == null || L("r.practice") <= 0) return;
        if (target == practiceTarget && Time.time - practiceAt < 2f) practiceStacks = Mathf.Min(5, practiceStacks + n);
        else { practiceTarget = target; practiceStacks = Mathf.Min(5, n); }
        practiceAt = Time.time;
    }

    // 광란 공명 II: 10처치마다 빈 주머니의 비 (고유 스킬이 없으면 아무것도 안 함)
    public void RoguePathRain() { if (Alive && who == CharacterId.Rogue) RogueEmpty(); }

    // 회전 표창 카드의 돌아오는 피해 (카드가 없으면 0) · 진화 여부
    public float RogueSpinBack => L("r.boomerang") > 0 ? Atk * V(L("r.boomerang"), 0.4f, 0.15f) : 0f;
    public bool RogueEvolved(string key) => E(key);
}

// 회전 표창: 던진 평타 표창에 붙어, 날아가다 꺾여 주인에게 그대로 돌아옴 (SignatureSkills.RogueAttack)
//   가는 길: 평타 그대로 (피해 · 관통 · 카드 효과). 멀리 날았거나(화면 끝에 닿음 · 아니면 RogueRange × FarMul) · 관통 수가 다했거나 · 벽에 닿으면 그 자리에서 꺾임
//            예전엔 RogueRange × 0.8 (도적 기본 12 → 9.6칸, 0.2초)만 날면 꺾여, 화면(가로 반 폭 약 27칸) 한가운데 빈 곳에서 아무것도 안 맞고 돌아오는 것처럼 보였음
//            → 9.6칸은 「이보다 일찍은 안 꺾임」 최소로만 남기고, 화면 안에서는 끝까지 날아간 뒤 꺾임
//   오는 길: 예전 회전 표창 그대로 — 고유 스킬 피해(Atk × V(b, 0.4, 0.15)) · 관통 2 · 속도 26 · 같은 적은 한 번만(가는 길에 벤 적도 다시) · 카드 효과 없음
//            벽은 지나침, 매 프레임 주인 쪽으로 방향을 틂(트는 빠르기가 점점 빨라져 주인 둘레를 맴돌지 않음), 주인에게 닿으면 사라짐
//   귀환하는 칼날(re.return): 꺾이는 순간 하나 더 갈라져 둘이 양쪽으로 휘어 돌아옴, 처음 표창이 끝나면 탄창에 한 발 (평타 한 번에 한 발 — 예전과 같음)
//   안전: 돌아오는 길은 최대 3초 (그 뒤 사라짐) · 매 프레임 할당 없음 · 복제(Instantiate) 없음 (갈라지는 표창도 평타 표창처럼 새로 만듦)
//   회귀 칼날 갈래(ThrowPath, 2026-10-19~): 모든 평타 표창이 8칸에서 꺾임 · 몸으로 받으면 탄창 한 발(던진 묶음마다 한 번)
//            공명 I 가는 길에 벤 적을 오는 길에 또 베면 던지기 연습 +1 더 · 공명 II 받기 전에 몸 둘레를 한 바퀴 · 극의 받은 뒤 처음 방향으로 한 번 더 왕복
public class ReturningShuriken : MonoBehaviour
{
    const float BackSpeed = 26f, CatchDist = 1.2f, MaxBack = 3f, BaseTurn = 540f, TurnGrow = 1440f, Split = 40f, Curl = 20f;
    const int BackPene = 2;
    // 가는 길 최대 거리 (RogueRange 배율 — 화면 끝을 못 찾거나 아주 넓은 화면이어도 여기서는 꺾임)
    public const float FarMul = 1.8f;
    const float EdgeInset = 0.03f;          // 화면 가장자리 안쪽 여유 (보이는 동안 꺾이게)
    static readonly System.Action PassWall = () => { };
    static Camera cam;

    Bullet bullet;
    Transform owner;
    Vector3 origin;
    float minDistSq, outDistSq, backDmg, backAge;
    bool returning, twin, refund;
    // 회귀 칼날 갈래
    const float OrbitR = 2.4f, OrbitTime = 0.45f;
    const int OrbitPene = 4;
    bool pathMode, practice, orbit, orbiting, orbited;
    RogueVolley volley;
    int trips, outPene;
    float orbitT, orbitAng, outDmg, outSpeed;
    Vector2 outDir0;
    System.Collections.Generic.HashSet<int> outHits;

    // 던진 평타 표창에 (가는 길부터) — minDist 보다 일찍은 안 꺾이고, 화면 끝에 닿거나 maxDist 를 날면 꺾임
    public void Throw(Bullet b, Transform owner, Vector3 from, float minDist, float maxDist, float dmg, bool twin)
    {
        bullet = b;
        this.owner = owner;
        origin = from;
        minDistSq = minDist * minDist;
        outDistSq = maxDist * maxDist;
        backDmg = dmg;
        this.twin = twin;
        refund = twin;
        b.onSpent = Spent;          // 관통 수가 다해도 사라지지 않고 그 자리에서 꺾임
        b.onHitWall = TurnBack;     // 벽에 닿으면 거기서 꺾임
    }

    // 회귀 칼날 갈래: 모든 평타 표창 (dist 에서 꺾임 · 받으면 묶음마다 탄창 한 발 · trips 번 왕복)
    public void ThrowPath(Bullet b, Transform owner, Vector3 from, float dist, float dmg, bool twin, RogueVolley volley, bool practice, bool orbit, int trips)
    {
        Throw(b, owner, from, dist, dist, dmg, twin);
        refund = false;             // 예전 「사라져도 한 발」 대신 몸으로 받을 때만
        pathMode = true;
        this.volley = volley;
        this.practice = practice;
        this.orbit = orbit;
        this.trips = Mathf.Clamp(trips, 1, 2);
        outDmg = b.damage;
        outSpeed = b.speed;
        outPene = Mathf.Max(1, b.pene);
        outDir0 = b.Direction;
        if (practice)
        {
            outHits = new System.Collections.Generic.HashSet<int>();
            b.onHitEnemy += RecordOut;
        }
    }

    void RecordOut(Bullet s, Collider2D c)
    {
        if (!returning && c != null && outHits != null) outHits.Add(c.gameObject.GetInstanceID());
    }

    // 오는 길: 가는 길에 벤 적이면 던지기 연습 +1 더 (명중 피해 훅의 +1 과 합쳐 2중첩)
    void BackHit(Bullet s, Collider2D c)
    {
        if (c == null || outHits == null || !outHits.Contains(c.gameObject.GetInstanceID())) return;
        SignatureSkills sig = SignatureSkills.Instance;
        if (sig != null) sig.RoguePracticeBump(c.gameObject, 1);
    }

    // 갈라진 표창 (처음부터 돌아오는 길)
    void Back(Bullet b, Transform owner, float dmg)
    {
        bullet = b;
        this.owner = owner;
        backDmg = dmg;
        returning = true;
        b.SetSource(DamageSource.Signature);
        b.hitOnce = new System.Collections.Generic.HashSet<int>();
        b.onHitWall = PassWall;
    }

    bool Spent()
    {
        if (returning) return false;
        TurnBack();
        return true;
    }

    void TurnBack()
    {
        if (returning || bullet == null) return;
        returning = true;
        backAge = 0f;
        Vector2 outDir = bullet.Direction;
        bullet.SetSource(DamageSource.Signature);
        bullet.damage = backDmg;
        bullet.ResetPene(BackPene);
        if (bullet.hitOnce != null) bullet.hitOnce.Clear();
        else bullet.hitOnce = new System.Collections.Generic.HashSet<int>();
        bullet.onHitEnemy = null;           // 카드 효과(출혈 · 도탄 · 독 등)는 가는 길에만
        if (pathMode && practice) bullet.onHitEnemy = BackHit;
        bullet.onSpent = null;              // 오는 길에 관통 수가 다하면 사라짐
        bullet.onHitWall = PassWall;        // 오는 길엔 벽을 지나침 (벽 앞에서 막혀 못 돌아오지 않게)
        bullet.isCrit = false;
        bullet.critRolled = false;
        bullet.speed = BackSpeed;
        bullet.Halt();                      // 이번 프레임 남은 길의 적은 오는 표창이 맞히지 않게
        // 한 방향으로 휘어 돌아옴 (둘이면 양쪽으로 갈라져)
        bullet.Steer(Quaternion.Euler(0f, 0f, twin ? -Split : -Curl) * outDir);
        if (twin)
        {
            CharacterKit kit = CharacterKit.Instance;
            Bullet t = kit != null ? kit.TwinShuriken(transform.position, Quaternion.Euler(0f, 0f, Split) * outDir, bullet) : null;
            if (t != null) t.gameObject.AddComponent<ReturningShuriken>().Back(t, owner, backDmg);
        }
    }

    void Update()
    {
        if (bullet == null) return;
        if (!returning)
        {
            Vector3 p = transform.position;
            float d = (p - origin).sqrMagnitude;
            if (d >= outDistSq || (d >= minDistSq && OffScreen(p))) TurnBack();
            return;
        }
        if (orbiting) { Orbit(); return; }
        backAge += Time.deltaTime;
        if (owner == null || backAge > MaxBack) { Finish(false); return; }
        Vector2 to = owner.position - transform.position;
        if (to.sqrMagnitude <= CatchDist * CatchDist) { Caught(); return; }
        float max = (BaseTurn + TurnGrow * backAge) * Time.deltaTime;
        float a = Mathf.Clamp(Vector2.SignedAngle(bullet.Direction, to), -max, max);
        bullet.Steer(Quaternion.Euler(0f, 0f, a) * bullet.Direction);
    }

    // 화면 끝에 닿았는지 (가장자리 조금 안쪽) — 카메라를 못 찾으면 거리(outDistSq)로만
    static bool OffScreen(Vector3 p)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return false;
        Vector3 v = cam.WorldToViewportPoint(p);
        return v.x < EdgeInset || v.x > 1f - EdgeInset || v.y < EdgeInset || v.y > 1f - EdgeInset;
    }

    // 주인에게 닿음: 갈래면 한 바퀴(공명 II) → 탄창 한 발 → 한 번 더 왕복(극의), 아니면 예전처럼 사라짐
    void Caught()
    {
        if (!pathMode) { Finish(true); return; }
        if (orbit && !orbited) { StartOrbit(); return; }
        PathRefund();
        if (trips > 1) { Relaunch(); return; }
        Finish(true);
    }

    void StartOrbit()
    {
        orbiting = orbited = true;
        orbitT = 0f;
        Vector2 rel = transform.position - owner.position;
        orbitAng = rel.sqrMagnitude > 0.0001f ? Mathf.Atan2(rel.y, rel.x) : 0f;
        if (bullet.hitOnce != null) bullet.hitOnce.Clear();
        bullet.ResetPene(OrbitPene);
    }

    // 몸 둘레 한 바퀴 (주인이 움직여도 따라감 · OrbitTime 뒤 받음)
    void Orbit()
    {
        if (owner == null) { Finish(false); return; }
        orbitT += Time.deltaTime;
        if (orbitT >= OrbitTime) { orbiting = false; Caught(); return; }
        float ang = orbitAng + orbitT / OrbitTime * Mathf.PI * 2f;
        Vector3 want = owner.position + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * OrbitR;
        Vector2 d = want - transform.position;
        bullet.speed = Mathf.Clamp(d.magnitude / Mathf.Max(Time.deltaTime, 0.001f), 4f, 90f);
        bullet.Steer(d);
    }

    // 극의: 받은 자리에서 처음 던진 방향으로 한 번 더 (가는 길 피해 · 관통 그대로)
    void Relaunch()
    {
        trips--;
        returning = orbiting = orbited = false;
        backAge = 0f;
        origin = transform.position;
        bullet.damage = outDmg;
        bullet.speed = outSpeed;
        bullet.ResetPene(outPene);
        if (bullet.hitOnce != null) bullet.hitOnce.Clear();
        bullet.isCrit = bullet.critRolled = false;
        bullet.onSpent = Spent;
        bullet.onHitWall = TurnBack;
        bullet.onHitEnemy = null;
        if (practice && outHits != null) { outHits.Clear(); bullet.onHitEnemy = RecordOut; }
        bullet.Steer(outDir0);
        Fx.Spawn("fx_spark", transform.position, 1.2f, new Color(0.6f, 0.9f, 1f), 26f);
    }

    // 회귀 칼날: 몸으로 받으면 탄창 한 발 (같이 던진 묶음에서 한 번만)
    void PathRefund()
    {
        if (volley == null || volley.refunded) return;
        volley.refunded = true;
        SignatureSkills s = SignatureSkills.Instance;
        if (s != null) s.ShurikenBack();
    }

    void Finish(bool caught)
    {
        if (caught) Fx.Spawn("fx_spark", owner.position, 1f, new Color(0.85f, 0.8f, 1f), 26f);
        Refund();
        Destroy(gameObject);
    }

    void Refund()
    {
        if (!refund) return;
        refund = false;
        SignatureSkills s = SignatureSkills.Instance;
        if (s != null) s.ShurikenBack();
    }

    // 오는 길에 관통 수가 다했거나 수명이 끝나 사라져도 탄창 한 발은 (예전처럼 평타 한 번에 한 발)
    void OnDestroy() => Refund();
}
