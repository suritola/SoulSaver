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
        // 회전 표창: 방금 던진 그 평타 표창이 RogueRange × 0.8 만큼 날아가(또는 관통 수가 다하거나 벽에 닿으면 그 자리에서) 꺾여 돌아옴
        //   예전엔 0.35초 뒤 그 자리에 새 표창을 만들어 날려 엉뚱한 데서 나타나 보였음
        Bullet thrown = Kit != null ? Kit.TakeRogueThrow() : null;
        int b = L("r.boomerang");
        if (b > 0 && thrown != null && thrown.isActiveAndEnabled && Alive)      // 풀로 돌아간 표창(꺼짐)에는 붙이지 않음 (다시 쓰일 때 남지 않게)
            thrown.gameObject.AddComponent<ReturningShuriken>().Throw(thrown, transform, start, RogueRange * 0.8f, Atk * V(b, 0.4f, 0.15f), E("re.return"));
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

    // 환영 연막: 연막 안에 있으면 공격이 40% 확률로 빗나감
    // 1.2.3~ 모두 빗나감(사실상 무적 — 돌진마다 연막이 깔려 그 안에 서 있으면 피해 0)이 너무 강하다는 의견 → 맞을 때마다 40% 확률
    public const float PhantomMissChance = 0.4f;
    //   1.2.3~ 피해 계산 뒤(무적 시간 · 피격 소리 · 맞음 처리 뒤)가 아니라 맞기 전에 판정 — 예전엔 빗나가도 맞은 것처럼 처리됐음 (PlayerController.TryHit → SignatureSkills.Evades)
    bool RogueEvade()
    {
        if (!E("re.phantom")) return false;
        foreach (SigZone z in smokes)
            if (z != null && z.Contains(transform.position))
            {
                if (Random.value >= PhantomMissChance) return false;
                SpecialAbilities.SharedFx?.FloatText(transform.position, Loc.T("빗나감"), new Color(0.8f, 0.7f, 1f), 3.5f, 0.4f);
                return true;
            }
        return false;
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
}

// 회전 표창: 던진 평타 표창에 붙어, 날아가다 꺾여 주인에게 그대로 돌아옴 (SignatureSkills.RogueAttack)
//   가는 길: 평타 그대로 (피해 · 관통 · 카드 효과). RogueRange × 0.8 을 날았거나 · 관통 수가 다했거나 · 벽에 닿으면 그 자리에서 꺾임
//   오는 길: 예전 회전 표창 그대로 — 고유 스킬 피해(Atk × V(b, 0.4, 0.15)) · 관통 2 · 속도 26 · 같은 적은 한 번만(가는 길에 벤 적도 다시) · 카드 효과 없음
//            벽은 지나침, 매 프레임 주인 쪽으로 방향을 틂(트는 빠르기가 점점 빨라져 주인 둘레를 맴돌지 않음), 주인에게 닿으면 사라짐
//   귀환하는 칼날(re.return): 꺾이는 순간 하나 더 갈라져 둘이 양쪽으로 휘어 돌아옴, 처음 표창이 끝나면 탄창에 한 발 (평타 한 번에 한 발 — 예전과 같음)
//   안전: 돌아오는 길은 최대 3초 (그 뒤 사라짐) · 매 프레임 할당 없음 · 복제(Instantiate) 없음 (갈라지는 표창도 평타 표창처럼 새로 만듦)
public class ReturningShuriken : MonoBehaviour
{
    const float BackSpeed = 26f, CatchDist = 1.2f, MaxBack = 3f, BaseTurn = 540f, TurnGrow = 1440f, Split = 40f, Curl = 20f;
    const int BackPene = 2;
    static readonly System.Action PassWall = () => { };

    Bullet bullet;
    Transform owner;
    Vector3 origin;
    float outDistSq, backDmg, backAge;
    bool returning, twin, refund;

    // 던진 평타 표창에 (가는 길부터)
    public void Throw(Bullet b, Transform owner, Vector3 from, float outDist, float dmg, bool twin)
    {
        bullet = b;
        this.owner = owner;
        origin = from;
        outDistSq = outDist * outDist;
        backDmg = dmg;
        this.twin = twin;
        refund = twin;
        b.onSpent = Spent;          // 관통 수가 다해도 사라지지 않고 그 자리에서 꺾임
        b.onHitWall = TurnBack;     // 벽에 닿으면 거기서 꺾임
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
            if ((transform.position - origin).sqrMagnitude >= outDistSq) TurnBack();
            return;
        }
        backAge += Time.deltaTime;
        if (owner == null || backAge > MaxBack) { Finish(false); return; }
        Vector2 to = owner.position - transform.position;
        if (to.sqrMagnitude <= CatchDist * CatchDist) { Finish(true); return; }
        float max = (BaseTurn + TurnGrow * backAge) * Time.deltaTime;
        float a = Mathf.Clamp(Vector2.SignedAngle(bullet.Direction, to), -max, max);
        bullet.Steer(Quaternion.Euler(0f, 0f, a) * bullet.Direction);
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
