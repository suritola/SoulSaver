using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 특수 능력이 쓰는 도우미 (Specials) 와 실행 중에 붙이는 부품들 (화상 · 낫 · 유탄 · 장판 · 해골 · 회오리 …)
// SpecialAbilities.cs 에서 나눔 (2.1.1). 모두 AddComponent 로만 붙이므로 파일 이름과 클래스 이름이 달라도 됨
// ===================================================================== helpers
public static class Specials
{
    // 일반 적과 보스 모두에게 피해
    public static void Damage(GameObject go, float damage, Vector3 dir, float knock)
    {
        if (go == null) return;
        EnermyController e = go.GetComponent<EnermyController>();
        if (e != null)
        {
            if (!e.IsDead) e.TakeDamage(damage, knock, dir);
            return;
        }
        bosss b = go.GetComponent<bosss>();
        if (b != null) { b.TakeDamage(damage, knock, dir); return; }
        // 보스가 만든 부술 수 있는 것 (리치 왕의 등불 · 묘비 · 영혼구, 2.2.1)
        if (go.TryGetComponent(out BossPart part)) part.Hit(damage);
    }

    // 물리 검색 결과를 담는 재사용 목록 (OverlapCircleAll 은 부를 때마다 새 배열을 만듦)
    // OverlapCircleAll 과 같은 조건 (모든 레이어 · 깊이, 트리거 포함 여부는 물리 설정 그대로)
    public static List<Collider2D> Overlap(Vector3 pos, float radius, List<Collider2D> into)
    {
        into.Clear();
        ContactFilter2D filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        Physics2D.OverlapCircle(pos, radius, filter, into);
        return into;
    }

    static readonly List<Collider2D> nearestHits = new List<Collider2D>(64);

    // 최적화: 보스(EnermyController 없음)는 GetComponent 생략 · 거리는 제곱으로 비교 (고르는 적은 같음)
    public static Transform NearestEnemy(Vector3 from, float range)
    {
        Transform best = null;
        float bestSq = range * range;
        Vector2 f = from;
        List<Collider2D> hits = Overlap(from, range, nearestHits);
        for (int i = 0; i < hits.Count; i++)
        {
            Collider2D c = hits[i];
            bool enemy = c.CompareTag("enermy");
            if (!enemy && !c.CompareTag("boss")) continue;
            if (enemy && c.TryGetComponent(out EnermyController e) && e.IsDead) continue;
            Transform t = c.transform;
            float d = ((Vector2)t.position - f).sqrMagnitude;
            if (d < bestSq) { bestSq = d; best = t; }
        }
        return best;
    }

    // 피해를 주는 광역 판정용 공용 목록 (피해 콜백이 다시 질의해도 돌던 목록이 바뀌지 않게 복사본을 돎)
    static readonly List<Collider2D> areaHits = new List<Collider2D>(64);
    static readonly List<List<Collider2D>> copyPool = new List<List<Collider2D>>();
    static int copyDepth;

    // OverlapCircleAll 과 같은 결과를 복사본 목록으로 (다 쓰면 반드시 ReleaseCopy · 중첩 호출 가능)
    internal static List<Collider2D> OverlapCopy(Vector3 pos, float radius)
    {
        Overlap(pos, radius, areaHits);
        if (copyDepth >= copyPool.Count) copyPool.Add(new List<Collider2D>(64));
        List<Collider2D> copy = copyPool[copyDepth++];
        copy.Clear();
        copy.AddRange(areaHits);
        return copy;
    }

    internal static void ReleaseCopy(List<Collider2D> copy)
    {
        copy.Clear();
        if (copyDepth > 0) copyDepth--;
    }

    // 표적이 아직 쓸 만한지 (살아 있고 켜져 있음)
    internal static bool StillTarget(Transform t)
    {
        if (t == null || !t.gameObject.activeInHierarchy) return false;
        return !(t.TryGetComponent(out EnermyController e) && e.IsDead);
    }
}

// 불타는 적: 초당 피해
public class Burn : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    public static Sprite FireSprite;

    public float dps;
    public float until;
    float tick;
    float puff;
    GameObject flame;
    SpriteRenderer hostSr;
    float seed;

    public static void Apply(GameObject target, float dps, float duration)
    {
        if (target == null) return;
        Burn b = target.GetComponent<Burn>();
        if (b == null) b = target.AddComponent<Burn>();
        b.dps = Mathf.Max(b.dps, dps);
        b.until = Time.time + duration;
    }

    void Start()
    {
        hostSr = GetComponent<SpriteRenderer>();
        seed = (GetInstanceID() & 1023) * 0.61f;
        // 몸에서 타오르는 불빛
        if (FireSprite != null)
            flame = SpecialAbilities.MakeSprite("BurnFlame", FireSprite, transform.position, 0.3f, new Color(1f, 0.5f, 0.15f, 0.7f), "Effect", 2);
    }

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        if (Time.time > until) { Destroy(this); return; }

        if (flame != null)
        {
            flame.transform.position = transform.position + new Vector3(0f, 0.3f, 0f);
            // 최적화: 매 프레임 난수 대신 빠른 떨림 함수 (같은 범위 0.26~0.36 · 적마다 다른 위상)
            float flick = Mathf.Sin(Time.time * 37f + seed) * 0.6f + Mathf.Sin(Time.time * 23f + seed * 1.7f) * 0.4f;
            flame.transform.localScale = Vector3.one * (0.31f + 0.05f * flick);
        }
        puff += Time.deltaTime;
        if (puff >= 0.1f && FireSprite != null)
        {
            puff = 0f;
            // 최적화: 화면 밖 적은 불꽃 입자를 만들지 않음 (보이지 않음 · 피해는 그대로)
            if (hostSr == null || hostSr.isVisible)
            FlameParticle.Spawn(FireSprite, transform.position + (Vector3)(Random.insideUnitCircle * 0.8f),
                                new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(3f, 5f)), 0.45f, 0.04f, 0.28f, false);
        }

        tick += Time.deltaTime;
        if (tick >= 0.25f)
        {
            tick = 0f;
            SignatureSkills.DotTick = true;        // 지속 피해 (맞힐 때 효과가 붙지 않게)
            Specials.Damage(gameObject, dps * 0.25f, Vector3.zero, 0f);
            SignatureSkills.DotTick = false;
        }
    }

    void OnDestroy()
    {
        if (flame != null) Destroy(flame);
    }
}

// 불꽃 입자: 흰노랑 → 주황 → 빨강 → 연기로 변하며 커지고 느려짐
public class FlameParticle : FxTicked
{
    Vector2 velocity;
    float life;
    float age;
    float startScale;
    float endScale;
    bool smoke;
    bool ember;
    SpriteRenderer sr;

    // 다 탄 입자는 지우지 않고 꺼 두었다가 다시 씀 (화염 방사 · 폭발마다 수십 개씩 만들고 지우지 않게)
    static readonly Stack<FlameParticle> pool = new Stack<FlameParticle>();
    static int effectLayer = -1;

    // 그래픽 품질 낮음(L5): 동시에 150개까지 (넘치면 새 입자 생략)
    static int active;
    const int LowCap = 150;
    static bool Capped => GraphicsQuality.Low && active >= LowCap;

    protected override void OnEnable() { base.OnEnable(); active++; }
    protected override void OnDisable() { base.OnDisable(); if (active > 0) active--; }

    static FlameParticle Take(string name, Sprite sprite, Vector3 pos, float scale, Color color, int order)
    {
        FlameParticle p = null;
        while (pool.Count > 0 && p == null) p = pool.Pop();     // 장면이 바뀌어 지워진 것은 건너뜀
        if (p == null)
        {
            GameObject go = SpecialAbilities.MakeSprite(name, sprite, pos, scale, color, "Effect", order);
            p = go.AddComponent<FlameParticle>();
            p.sr = go.GetComponent<SpriteRenderer>();
            return p;
        }
        if (effectLayer < 0) effectLayer = SortingLayer.NameToID("Effect");
        p.sr.sprite = sprite;
        p.sr.color = color;
        p.sr.sortingLayerID = effectLayer;
        p.sr.sortingOrder = order;
        p.transform.position = pos;
        p.transform.localScale = Vector3.one * scale;
        p.age = 0f;
        p.smoke = p.ember = false;
        p.spawnFrame = Time.frameCount;
        p.gameObject.SetActive(true);
        return p;
    }

    public static void Spawn(Sprite sprite, Vector3 pos, Vector2 velocity, float life, float startScale, float endScale, bool smoke)
    {
        if (Capped) return;
        FlameParticle p = Take(smoke ? "Smoke" : "Flame", sprite, pos, startScale, Color.white, smoke ? 1 : 4);
        p.velocity = velocity;
        p.life = life;
        p.startScale = startScale;
        p.endScale = endScale;
        p.smoke = smoke;
        p.Tint(0f);
    }

    public static void SpawnEmber(Sprite sprite, Vector3 pos, Vector2 velocity)
    {
        if (Capped) return;
        FlameParticle p = Take("Ember", sprite, pos, 0.025f, new Color(1f, 0.9f, 0.5f), 5);
        p.velocity = velocity;
        p.life = Random.Range(0.4f, 0.7f);
        p.startScale = p.endScale = 0.025f;
        p.ember = true;
    }

    void Tint(float k)
    {
        if (smoke)
        {
            sr.color = new Color(0.3f, 0.27f, 0.28f, 0.55f * (1f - k));
            return;
        }
        Color c;
        if (k < 0.2f) c = Color.Lerp(new Color(1f, 0.97f, 0.75f), new Color(1f, 0.75f, 0.25f), k / 0.2f);
        else if (k < 0.55f) c = Color.Lerp(new Color(1f, 0.75f, 0.25f), new Color(1f, 0.42f, 0.1f), (k - 0.2f) / 0.35f);
        else if (k < 0.8f) c = Color.Lerp(new Color(1f, 0.42f, 0.1f), new Color(0.75f, 0.15f, 0.08f), (k - 0.55f) / 0.25f);
        else c = Color.Lerp(new Color(0.75f, 0.15f, 0.08f), new Color(0.25f, 0.2f, 0.2f), (k - 0.8f) / 0.2f);
        c.a = k < 0.8f ? 0.85f : 0.85f * (1f - (k - 0.8f) / 0.2f);
        sr.color = c;
    }

    int spawnFrame = -1;

    internal override void Tick()
    {
        // 새로 만든 오브젝트처럼, 켜진 그 프레임에는 움직이지 않음
        if (Time.frameCount == spawnFrame) return;
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / life);
        // 앞으로 나가다 점점 느려지고, 끝에서는 위로 떠오름
        velocity *= 1f - Mathf.Min(1f, Time.deltaTime * (ember ? 1.5f : 4f));
        if (smoke || k > 0.6f) velocity.y += Time.deltaTime * 4f;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, Mathf.Sqrt(k));

        if (ember) sr.color = new Color(1f, Random.Range(0.6f, 0.95f), 0.4f, 1f - k);
        else Tint(k);

        if (k >= 1f)
        {
            gameObject.SetActive(false);
            pool.Push(this);
        }
    }
}

// 가장 가까운 적을 향해 방향을 틀며 날아감
public class Homing : MonoBehaviour
{
    public float turnSpeed = 360f;
    // 스치면 보정 (2.2.3, 거너 카드 「유도 탄두」): 총알은 조준한 대로 곧게 날아가다가,
    // 앞쪽 가까이(5칸 안)에서 날아가는 길과 적 사이가 nearMiss 칸 안이면 그 적에게만 빠르게 꺾여 맞음
    //   멀리서부터 미리 꺾지 않아 노린 적을 놓치거나 옆의 적에게 끌려가지 않음 · 지나치면(60° 넘게) 다시 곧게
    // nearMiss 가 0이면 예전 방식(날아가며 가장 가까운 적으로 꺾음 · 추적탄 무기 · 영혼 추적자 등)
    public float nearMiss;
    const float LookAhead = 5f, LockLost = 60f, SnapTurn = 900f;
    Bullet bullet;
    Transform target;
    static readonly List<Collider2D> grazeHits = new List<Collider2D>(64);
    Transform seek;          // 예전 방식 유도: 지금 꺾어 가는 적 (살아 있으면 0.1초마다만 더 가까운 적을 찾음)
    float retargetAt;

    float trail;

    void Start() => bullet = GetComponent<Bullet>();

    static bool Alive(Collider2D c)
    {
        if (c == null || !(c.CompareTag("enermy") || c.CompareTag("boss"))) return false;
        EnermyController e = c.GetComponent<EnermyController>();
        return e == null || !e.IsDead;
    }

    // 날아가는 길 바로 옆에 있는 적 (앞쪽 · 길에서 가장 가까운)
    Transform FindGrazed()
    {
        Vector2 pos = transform.position, dir = bullet.Direction;
        Transform best = null;
        float bestSide = float.MaxValue;
        // 최적화: 공용 목록으로 (OverlapCircleAll 은 부를 때마다 새 배열 · 피해를 주지 않고 고르기만 함)
        foreach (Collider2D c in Specials.Overlap(pos, LookAhead, grazeHits))
        {
            if (!Alive(c)) continue;
            Vector2 to = (Vector2)c.transform.position - pos;
            float along = Vector2.Dot(to, dir);
            if (along <= 0.2f) continue;                                   // 뒤 · 바로 옆은 아님
            float side = Mathf.Abs(dir.x * to.y - dir.y * to.x);           // 길에서 떨어진 거리
            if (side > nearMiss || side >= bestSide) continue;
            bestSide = side;
            best = c.transform;
        }
        return best;
    }

    void NearMiss()
    {
        if (target != null && (!target.gameObject.activeInHierarchy || (target.TryGetComponent(out EnermyController e) && e.IsDead))) target = null;
        if (target == null) target = FindGrazed();
        if (target == null) return;
        Vector2 want = ((Vector2)(target.position - transform.position)).normalized;
        float angle = Vector2.SignedAngle(bullet.Direction, want);
        if (Mathf.Abs(angle) > LockLost) { target = null; return; }        // 지나쳤으면 다시 곧게 (뒤로 꺾지 않음)
        float turn = Mathf.Max(turnSpeed, SnapTurn) * Time.deltaTime;
        bullet.Dir = Quaternion.Euler(0, 0, Mathf.Clamp(angle, -turn, turn)) * bullet.Direction;
    }

    void Update()
    {
        if (bullet == null) return;
        // 보랏빛 꼬리 (스치면 보정은 꺾일 때만)
        trail += Time.deltaTime;
        if (trail >= 0.03f && SpecialAbilities.GlowSprite != null && (nearMiss <= 0f || target != null))
        {
            trail = 0f;
            FadeSprite.Spawn("SeekerTrail", SpecialAbilities.GlowSprite, transform.position, 0.06f, new Color(0.75f, 0.5f, 1f, 0.55f), "Effect", 4, 0.3f);
        }
        if (nearMiss > 0f) { NearMiss(); return; }
        // 최적화: 표적이 살아 있으면 0.1초마다만 가장 가까운 적을 다시 찾음 (표적이 죽거나 꺼지면 바로)
        if (!Specials.StillTarget(seek) || Time.time >= retargetAt)
        {
            seek = Specials.NearestEnemy(transform.position, 20f);
            retargetAt = Time.time + 0.1f;
        }
        Transform nearest = seek;
        if (nearest == null) return;
        Vector2 dirTo = ((Vector2)(nearest.position - transform.position)).normalized;
        float a = Vector2.SignedAngle(bullet.Direction, dirTo);
        float step = Mathf.Clamp(a, -turnSpeed * Time.deltaTime, turnSpeed * Time.deltaTime);
        bullet.Dir = Quaternion.Euler(0, 0, step) * bullet.Direction;
    }
}

// 부메랑 낫: 날아갔다가 주인에게 돌아옴
public class Scythe : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    public Transform owner;
    public Vector2 direction;
    public float distance = 12f;
    public float outTime = 0.45f;
    float t;
    Vector3 start;
    bool returning;

    void Start()
    {
        start = transform.position;
        // 벽은 그냥 통과함 (벽에 닿아 사라지면 바로 회수돼 벽 앞에서 연사가 되던 문제)
        Bullet b = GetComponent<Bullet>();
        if (b != null) b.onHitWall = () => { };
    }

    // 끝까지 날아가면 주인에게 돌아옴 (돌아오는 길에 같은 적을 한 번 더 벨 수 있음)
    void StartReturn()
    {
        if (returning) return;
        returning = true;
        Bullet b = GetComponent<Bullet>();
        if (b != null && b.hitOnce != null) b.hitOnce.Clear();
    }

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        transform.Rotate(0f, 0f, -900f * Time.deltaTime);
        if (owner == null) { Destroy(gameObject); return; }

        t += Time.deltaTime;
        if (!returning)
        {
            float k = Mathf.Clamp01(t / outTime);
            transform.position = start + (Vector3)direction * distance * Mathf.Sin(k * Mathf.PI * 0.5f);
            if (k >= 1f) StartReturn();
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, owner.position, 30f * Time.deltaTime);
            if (Vector2.Distance(transform.position, owner.position) < 1f) Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        SpecialAbilities s = Object.FindFirstObjectByType<SpecialAbilities>();
        if (s != null) s.ScytheReturned();
    }
}

// 용암 유탄: 불꼬리를 끌며 포물선으로 날아가 폭발하고 끓는 용암 웅덩이를 남김
public class Grenade : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    public SpecialAbilities owner;
    public Vector3 target;
    public float damage;
    public float radius = 3.5f;
    public bool cluster;        // 진화: 폭발 후 작은 용암탄 3개로 흩어짐
    public bool mini;           // 흩어진 작은 용암탄 (장판 없음)
    public float weakMul;       // 빈틈: 떨어진 점이 보스 · 중간 보스 빈틈 위면 이 배율 (직격점만 · 0 = 없음, FireGrenade 가 넣음)
    public float flightTime = 0.5f;
    Vector3 start;
    float t;
    float puff;
    float baseScale;
    SpriteRenderer sr;
    Color baseColor;
    GameObject shadow;
    LineRenderer marker;

    static readonly Color Hot = new Color(1f, 0.92f, 0.55f);
    static readonly Color Lava = new Color(1f, 0.45f, 0.1f, 0.9f);

    void Start()
    {
        start = transform.position;
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        baseScale = transform.localScale.x;
        // 떨어질 곳의 그림자와 범위 고리
        shadow = SpecialAbilities.MakeSprite("GrenadeShadow", sr.sprite, target, 0.05f, new Color(0f, 0f, 0f, 0.4f), "Effect", 0);
        marker = Hostile.NewLine("GrenadeMark", new Color(1f, 0.45f, 0.1f, 0.6f), mini ? 0.06f : 0.1f, 1);
        marker.loop = true;
    }

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / flightTime);
        Vector3 p = Vector3.Lerp(start, target, k);
        p.y += Mathf.Sin(k * Mathf.PI) * (mini ? 1.5f : 2.8f);     // 포물선
        transform.position = p;

        // 달아오른 핵: 회전하며 깜빡임
        transform.Rotate(0f, 0f, -720f * Time.deltaTime);
        float pulse = Mathf.PingPong(Time.time * 12f, 1f);
        transform.localScale = Vector3.one * baseScale * (0.85f + 0.3f * pulse);
        sr.color = Color.Lerp(baseColor, Hot, pulse);

        // 불꽃 꼬리와 연기
        puff += Time.deltaTime;
        if (puff >= 0.02f)
        {
            puff = 0f;
            FlameParticle.Spawn(sr.sprite, p, Random.insideUnitCircle * 1.5f, Random.Range(0.25f, 0.4f), 0.05f, mini ? 0.15f : 0.25f, false);
            if (Random.value < 0.35f) FlameParticle.Spawn(sr.sprite, p, Vector2.up * 1.5f, 0.6f, 0.04f, 0.2f, true);
            if (Random.value < 0.3f) FlameParticle.SpawnEmber(sr.sprite, p, Random.insideUnitCircle * 6f);
        }

        if (shadow != null) shadow.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, radius * 0.12f, k);
        if (marker != null)
        {
            Hostile.SetArc(marker, target, radius * Mathf.Lerp(0.3f, 1f, k), 0f, 354f);
            marker.startColor = marker.endColor = new Color(1f, 0.45f, 0.1f, 0.3f + 0.5f * k);
        }

        if (k >= 1f) Blast();
    }

    void Blast()
    {
        owner.Explode(target, radius, damage, 1.5f, Lava, mini ? 0f : weakMul);     // 직격점 빈틈: 폭발 피해가 그 보스에게 들어갈 때 늘림 (한 대로 · 치명타 · 결계 게이지 · 소환 수 한 번)
        if (!mini) owner.GunAreaHit(target, radius, damage);
        Sprite glow = sr.sprite;

        // 하얗게 달아오른 중심 섬광
        FadeSprite.Spawn("GrenadeCore", glow, target, radius * 1.1f / 8f, new Color(1f, 0.97f, 0.8f, 1f), "Effect", 6, 0.14f);
        // 두 겹의 충격파
        ShockRing.Spawn(target, radius * 0.2f, radius * 1.2f, 0.3f, new Color(1f, 0.8f, 0.4f, 0.95f), mini ? 0.2f : 0.35f);
        ShockRing.Spawn(target, radius * 0.1f, radius * 0.9f, 0.5f, new Color(0.8f, 0.2f, 0.05f, 0.8f), mini ? 0.3f : 0.6f);
        // 사방으로 튀는 불꽃, 불티, 연기
        int flames = mini ? 7 : 16;
        for (int i = 0; i < flames; i++)
        {
            Vector2 v = Random.insideUnitCircle.normalized * Random.Range(radius * 3f, radius * 6f);
            FlameParticle.Spawn(glow, target, v, Random.Range(0.4f, 0.65f), 0.07f, Random.Range(0.35f, 0.6f), false);
        }
        for (int i = 0; i < (mini ? 5 : 12); i++)
            FlameParticle.SpawnEmber(glow, target, Random.insideUnitCircle.normalized * Random.Range(9f, 20f));
        for (int i = 0; i < (mini ? 2 : 6); i++)
            FlameParticle.Spawn(glow, target + (Vector3)(Random.insideUnitCircle * radius * 0.5f), Vector2.up * Random.Range(1.5f, 3f) + Random.insideUnitCircle,
                                Random.Range(0.9f, 1.4f), 0.1f, Random.Range(0.4f, 0.7f), true);
        // 그을음 자국
        FadeSprite.Spawn("Scorch", glow, target, radius * 1.6f / 8f, new Color(0.12f, 0.04f, 0.02f, 0.55f), "Background", 6, 3f);

        if (!mini)
        {
            DamageZone zone = owner.SpawnZone(target, radius * 0.7f, 2.5f, damage * 0.25f, new Color(1f, 0.35f, 0.05f, 0.7f));
            if (zone != null) zone.lava = true;
        }
        if (cluster)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject g = SpecialAbilities.MakeSprite("LavaShard", glow, target, baseScale * 0.6f, baseColor, "Effect", 5);
                Grenade m = g.AddComponent<Grenade>();
                m.owner = owner;
                m.target = target + (Vector3)(Quaternion.Euler(0, 0, i * 120f + Random.Range(-20f, 20f)) * Vector2.right * Random.Range(2.5f, 4f));
                m.damage = damage * 0.5f;
                m.radius = radius * 0.6f;
                m.mini = true;
                m.flightTime = 0.35f;
            }
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        using var source = DamageSource.As(dmgSource);
        if (shadow != null) Destroy(shadow);
        if (marker != null) Destroy(marker.gameObject);
    }
}

// 일정 시간 동안 안에 있는 적에게 0.5초마다 피해
public class DamageZone : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    public float radius = 3f;
    public float duration = 4f;
    public float tickDamage = 1f;
    public bool lava;           // 끓어오르는 용암 (불꽃과 불티가 올라옴)
    float t;
    float tick;
    float bubble;
    SpriteRenderer sr;
    Color baseColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        t += Time.deltaTime;
        tick += Time.deltaTime;
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (0.75f + Mathf.Sin(t * 8f) * 0.25f) * Mathf.Clamp01((duration - t) * 2f));
        if (lava)
        {
            bubble += Time.deltaTime;
            if (bubble >= 0.06f)
            {
                bubble = 0f;
                Vector3 at = transform.position + (Vector3)(Random.insideUnitCircle * radius * 0.85f);
                FlameParticle.Spawn(sr.sprite, at, new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(2f, 4f)), Random.Range(0.35f, 0.55f), 0.04f, Random.Range(0.15f, 0.3f), false);
                if (Random.value < 0.3f) FlameParticle.SpawnEmber(sr.sprite, at, new Vector2(Random.Range(-2f, 2f), Random.Range(4f, 8f)));
            }
        }
        if (tick >= 0.5f)
        {
            tick = 0f;
            // 최적화: 공용 목록의 복사본을 돎 (피해 콜백이 다시 질의해도 안전 · 할당 없음)
            List<Collider2D> hits = Specials.OverlapCopy(transform.position, radius);
            for (int i = 0; i < hits.Count; i++)
            {
                Collider2D c = hits[i];
                if (c == null) continue;
                if (c.CompareTag("enermy") || c.CompareTag("boss")) Specials.Damage(c.gameObject, tickDamage, Vector3.zero, 0f);
            }
            Specials.ReleaseCopy(hits);
        }
        if (t >= duration) Destroy(gameObject);
    }
}

// 아군 해골: 가까운 적에게 달려가 자폭
public class AllySkeleton : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    public SpecialAbilities owner;
    public float damage;
    public float speed = 16f;
    public float life = 10f;
    float t;
    SpriteRenderer sr;
    Transform seek;
    float retargetAt;

    void Start() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        t += Time.deltaTime;
        // 최적화: 표적이 살아 있으면 0.1초마다만 가장 가까운 적을 다시 찾음 (표적이 죽거나 꺼지면 바로)
        if (!Specials.StillTarget(seek) || Time.time >= retargetAt)
        {
            seek = Specials.NearestEnemy(transform.position, 30f);
            retargetAt = Time.time + 0.1f;
        }
        Transform target = seek;
        if (target != null)
        {
            Vector3 d = target.position - transform.position;
            if (sr != null) sr.flipX = d.x < 0f;
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            if (d.magnitude < 1.5f) { Blow(); return; }
        }
        if (t >= life) Blow();
    }

    void Blow()
    {
        if (owner != null) owner.Explode(transform.position, 2.5f, damage, 1.5f, new Color(0.6f, 0.95f, 1f, 0.9f));
        Destroy(gameObject);
    }
}

// 잠깐 커지며 사라지는 빛
public class FadeOut : MonoBehaviour
{
    public float duration = 0.3f;
    float t;
    SpriteRenderer sr;
    Color c;
    Vector3 s;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        c = sr.color;
        s = transform.localScale;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        sr.color = new Color(c.r, c.g, c.b, c.a * (1f - k));
        transform.localScale = s * (0.8f + 0.4f * k);
        if (k >= 1f) Destroy(gameObject);
    }
}

// 옅어지며 사라지는 빛 조각 (총알 꼬리 · 섬광 · 터짐): MakeSprite + FadeOut 과 보이는 모습이 같음
// 다 사라지면 지우지 않고 꺼 두었다가 다시 씀 (총알마다 초당 수십 개씩 만들고 지우지 않게)
public class FadeSprite : FxTicked
{
    static readonly Stack<FadeSprite> pool = new Stack<FadeSprite>();
    static readonly Dictionary<string, int> layerIds = new Dictionary<string, int>();

    // 그래픽 품질 낮음(L1): 잔상 · 꼬리(평타 · 적 탄 · 유도탄 · 예전 탄 꼬리)는 2개 중 1개만
    //   건너뛴 호출은 꺼져 있는 빈 조각(sink)을 돌려줌 — 돌려받은 것에 Shape 를 바로 부르는 호출자가 있어 null 대신
    static readonly HashSet<string> Decor = new HashSet<string> { "AtkTrail", "Trail", "SeekerTrail", "BulletTrail" };
    static int decorTurn;
    static FadeSprite sink;
    static FadeSprite Sink
    {
        get
        {
            if (sink == null)
            {
                GameObject go = new GameObject("FadeSink");
                go.SetActive(false);
                sink = go.AddComponent<FadeSprite>();
                sink.sr = go.AddComponent<SpriteRenderer>();
            }
            return sink;
        }
    }

    SpriteRenderer sr;
    float duration, t;
    Color c;
    Vector3 s;

    // 돌려받은 것으로 회전 · 축마다 다른 크기를 줄 수 있음 (Shape, 평타 잔상)
    public static FadeSprite Spawn(string name, Sprite sprite, Vector3 pos, float scale, Color color, string layer, int order, float duration)
    {
        if (GraphicsQuality.Low && Decor.Contains(name) && (++decorTurn & 1) == 0) return Sink;
        FadeSprite f = null;
        while (pool.Count > 0 && f == null) f = pool.Pop();     // 장면이 바뀌어 지워진 것은 건너뜀
        if (f == null)
        {
            GameObject go = new GameObject(name);
            f = go.AddComponent<FadeSprite>();
            f.sr = go.AddComponent<SpriteRenderer>();
        }
        if (!layerIds.TryGetValue(layer, out int layerId)) layerIds[layer] = layerId = SortingLayer.NameToID(layer);
        f.sr.sprite = sprite;
        f.sr.sortingLayerID = layerId;
        f.sr.sortingOrder = order;
        f.c = color;
        f.s = Vector3.one * scale;
        f.duration = Mathf.Max(0.0001f, duration);
        f.t = 0f;
        Transform tr = f.transform;
        tr.position = pos;
        tr.rotation = Quaternion.identity;
        // 첫 프레임은 MakeSprite 그대로 (FadeOut 도 다음 프레임부터 흐려지고 작아짐)
        f.sr.color = color;
        tr.localScale = f.s;
        f.spawnFrame = Time.frameCount;
        f.gameObject.SetActive(true);
        return f;
    }

    // 1.1.2~ 평타 잔상: 몸 그림의 회전 · 크기 그대로 (Spawn 바로 뒤에)
    public void Shape(Quaternion rotation, Vector3 scale)
    {
        s = scale;
        transform.rotation = rotation;
        transform.localScale = s;
    }

    int spawnFrame = -1;

    void Apply(float k)
    {
        sr.color = new Color(c.r, c.g, c.b, c.a * (1f - k));
        transform.localScale = s * (0.8f + 0.4f * k);
    }

    internal override void Tick()
    {
        // 새로 만든 오브젝트처럼, 켜진 그 프레임에는 움직이지 않음
        if (Time.frameCount == spawnFrame) return;
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        Apply(k);
        if (k >= 1f)
        {
            gameObject.SetActive(false);
            pool.Push(this);
        }
    }
}

// 계속 회전
public class Spin : MonoBehaviour
{
    public float speed = 360f;
    void Update() => transform.Rotate(0f, 0f, speed * Time.deltaTime);
}

// 선이 가늘어지며 사라짐
public class LineFade : MonoBehaviour
{
    public float duration = 0.2f;
    float t;
    LineRenderer lr;
    Color a, b;
    float w0, w1;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        a = lr.startColor; b = lr.endColor;
        w0 = lr.startWidth; w1 = lr.endWidth;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        lr.startColor = new Color(a.r, a.g, a.b, a.a * (1f - k));
        lr.endColor = new Color(b.r, b.g, b.b, b.a * (1f - k));
        lr.startWidth = w0 * (1f - k * 0.5f);
        lr.endWidth = w1 * (1f - k * 0.5f);
        if (k >= 1f) Destroy(gameObject);
    }
}

// 화염 회오리: 가까운 적에게 천천히 다가가며 주변 적을 빨아들이고 태움 (4초)
public class FireTornado : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    static readonly List<Collider2D> pullHits = new List<Collider2D>(32);
    public SpecialAbilities owner;
    public float damage;
    public float life = 4f;
    const float Radius = 3f;
    float age, tick;
    FxAnim body;
    Transform seek;
    float retargetAt;

    void Start()
    {
        body = Fx.Play("fx_tornado", transform.position + Vector3.up * 1.8f, 5f, Color.white, 14f, 0f, 14, true, life);
        FxAnim rune = Fx.Play("fx_rune", transform.position, Radius * 2f, new Color(1f, 0.5f, 0.15f, 0.55f), 1f, 0f, 1, true, life);
        if (rune != null) { rune.follow = transform; rune.spin = 120f; }
    }

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        age += Time.deltaTime;
        // 최적화: 표적이 살아 있으면 0.1초마다만 가장 가까운 적을 다시 찾음 (표적이 죽거나 꺼지면 바로)
        if (!Specials.StillTarget(seek) || Time.time >= retargetAt)
        {
            seek = Specials.NearestEnemy(transform.position, 14f);
            retargetAt = Time.time + 0.1f;
        }
        Transform target = seek;
        if (target != null) transform.position = Vector3.MoveTowards(transform.position, target.position, 5f * Time.deltaTime);
        if (body != null) body.transform.position = transform.position + Vector3.up * 1.8f;

        // 빨아들임 (재사용 목록, 적을 옮기기만 해서 목록이 바뀌지 않음)
        foreach (Collider2D c in Specials.Overlap(transform.position, Radius * 1.6f, pullHits))
        {
            if (!c.CompareTag("enermy")) continue;
            c.transform.position = Vector3.MoveTowards(c.transform.position, transform.position, 4f * Time.deltaTime);
        }

        tick += Time.deltaTime;
        if (tick >= 0.25f)
        {
            tick = 0f;
            // 최적화: 공용 목록의 복사본을 돎 (피해 콜백이 다시 질의해도 안전 · 할당 없음)
            List<Collider2D> hits = Specials.OverlapCopy(transform.position, Radius);
            for (int i = 0; i < hits.Count; i++)
            {
                Collider2D c = hits[i];
                if (c == null) continue;
                if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                Specials.Damage(c.gameObject, damage, Vector3.zero, 0f);
                Burn.Apply(c.gameObject, damage, 2f);
            }
            Specials.ReleaseCopy(hits);
            if (SpecialAbilities.GlowSprite != null)
                for (int i = 0; i < 4; i++)
                    FlameParticle.SpawnEmber(SpecialAbilities.GlowSprite, transform.position, Random.insideUnitCircle.normalized * Random.Range(6f, 12f));
        }
        if (age >= life)
        {
            Fx.Spawn("fx_explosion", transform.position, 5f, Color.white, 16f);
            Destroy(gameObject);
        }
    }
}

// 영혼 떼: 구슬들이 주인 주위를 돌다가 번갈아 표적에게 달려들고 돌아옴 (4초)
public class SoulSwarm : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    public Transform owner;
    public List<EnermyController> targets;
    public float damage;
    public Color color = Color.white;
    public float life = 4f;
    public int count = 5;
    int Count => count;
    FxAnim[] orbs;
    Transform[] chasing;
    float[] dashT;
    float age, next;
    int turn;

    void Start()
    {
        orbs = new FxAnim[Count];
        chasing = new Transform[Count];
        dashT = new float[Count];
        for (int i = 0; i < Count; i++) orbs[i] = Fx.Play("fx_orb", owner.position, 1.1f, color, 12f, 0f, 18, true, life);
    }

    Vector3 Home(int i) => owner.position + (Vector3)(Quaternion.Euler(0, 0, age * 200f + i * 360f / Count) * Vector2.right * 2.2f);

    Transform PickTarget()
    {
        targets.RemoveAll(e => e == null || e.IsDead);
        if (targets.Count > 0) return targets[Random.Range(0, targets.Count)].transform;
        return Specials.NearestEnemy(owner.position, 14f);
    }

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        if (owner == null) { Destroy(gameObject); return; }
        age += Time.deltaTime;

        // 0.25초마다 한 구슬씩 출격
        if (age >= next)
        {
            next = age + 0.25f;
            int i = turn++ % Count;
            if (chasing[i] == null) { chasing[i] = PickTarget(); dashT[i] = 0f; }
        }

        for (int i = 0; i < Count; i++)
        {
            if (orbs[i] == null) continue;
            Transform t = chasing[i];
            if (t == null)
            {
                orbs[i].transform.position = Vector3.MoveTowards(orbs[i].transform.position, Home(i), 30f * Time.deltaTime);
                continue;
            }
            orbs[i].transform.position = Vector3.MoveTowards(orbs[i].transform.position, t.position, 28f * Time.deltaTime);
            dashT[i] += Time.deltaTime;
            if (Vector2.Distance(orbs[i].transform.position, t.position) < 0.6f || dashT[i] > 0.8f)
            {
                if (dashT[i] <= 0.8f)
                {
                    Specials.Damage(t.gameObject, damage, Vector3.zero, 0.5f);
                    Fx.Spawn("fx_soulburst", t.position, 2f, Color.white, 22f);
                }
                chasing[i] = null;
            }
        }
        if (age >= life) Destroy(gameObject);
    }
}
