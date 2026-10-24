using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 결계 하나의 맥락 (1.2.5~ 공간 침식): 개별 결계와 「공간 침식」이 같은 공격 코드를 씀
// 개별 결계는 tempo 1 · erosion false 라 예전과 같은 결과, 공간 침식은 보스마다 하나씩 (간격 × tempo · 피해 × 0.85 · 한 대 상한 25%)
// 결계를 시작할 때 미리 만든 것을 Reset 해서 다시 씀 (결계마다 새로 만들지 않음)
public class DomainCtx
{
    public int slot;                // 결계 칸 (0 리치 왕 · 1 지옥의 군주 · 2 킹 슬라임 · 3 거울의 군주 · 4 개발자 · 5 엄마)
    public bosss src;               // 공격을 쓰는 보스 (공간 침식이면 이 보스가 쓰러질 때 그 공격만 멈춤)
    public MonoBehaviour runner;    // 공격 코루틴을 돌리는 곳 (개별 = 그 BossUltimate · 침식 = SpaceErosion)
    public Vector3 center;
    public float radius = 35f;      // 결계 반지름 (Confine · 튕기는 것 · 고리가 모두 이 값을 봄)
    public float until;             // 본 진행이 끝나는 시각
    public float tempo = 1f;        // 다음 공격까지 간격 배율 (개별 1 · 침식 0.6 ~ 0.85)
    public bool erosion;            // 공간 침식 안의 공격
    public bool crowd;              // 침식 참가 보스 3 이상 (회전 광선 · 반사 고리 수를 줄임)
    public int members = 1;         // 침식 참가 보스 수 (개발자 · 엄마 패턴이 섞일 때 참고)
    public Color color;             // 이 결계(보스)의 색
    public Transform root;          // 결계에서 만든 것을 모두 이 아래에 (끝날 때 한 번에 지움)
    public bool running;            // 결계가 아직 진행 중 (EndNow 에서 꺼짐)
    public readonly FxAnim[] lichBeams = new FxAnim[4];
    public readonly LineRenderer[] lichWarn = new LineRenderer[4];   // 회전 광선 예고선 (리치 왕이 쓰러지면 빔과 함께 지움)
    public readonly List<GameObject> decor = new List<GameObject>(8);  // 이 보스 전용 장식 (거울 룬 등 · 침식에서 그 보스가 쓰러지면 지움)
    public DevDomainCtx dev;        // 개발자 · 엄마 칸이면 연결된 맥락 (공격을 멈출 때 함께 멈춤 · BossUltimate.Developer.cs)
    public int mirrorPairs;         // 날아가는 중인 거울 광선 쌍 (침식은 최대 2)
    public IEnumerator routine;     // 지금 도는 공격 코루틴

    public void Reset(int slot, bosss src, MonoBehaviour runner, Vector3 center, float radius, float until, bool erosion, Color color, Transform root)
    {
        FadeBeams();
        this.slot = slot;
        this.src = src;
        this.runner = runner;
        this.center = center;
        this.radius = radius;
        this.until = until;
        this.erosion = erosion;
        this.color = color;
        this.root = root;
        tempo = 1f;
        crowd = false;
        members = 1;
        mirrorPairs = 0;
        routine = null;
        dev = null;
        decor.Clear();              // 지난 결계의 장식은 root 와 함께 이미 지워짐
        running = true;
    }

    // 공격 루프 조건: 결계가 진행 중 (침식은 그 보스가 살아 있는 동안만)
    public bool Live => running && (!erosion || (src != null && !src.IsDead));
    public Vector3 SrcPos => src != null ? src.transform.position : center;
    // 다음 공격까지의 간격만 × tempo (예고 시간 · 판정 크기 · 탄 속도는 그대로)
    public float Gap(float seconds) => seconds * tempo;
    // 개발자 · 엄마 패턴의 간격 배율 (침식에서도 0.75 보다 빨라지지 않음)
    public float DevTempo => erosion ? Mathf.Max(tempo, SpaceErosion.DevMomTempoFloor) : 1f;
    public float ErosionMul => erosion ? SpaceErosion.DamageMul : 1f;
    // 결계 공격 피해: raw × 보스 배율 (침식이면 × 0.85 · 한 대 상한 25%)
    public float Dmg(float raw) => Cap(raw * BossUltimate.PowerMul * ErosionMul);
    // 침식 한 대 상한 = 최대 체력의 25% (거울 군주 Capped 와 같은 계산 · TryHit 이 곱하는 배율로 나눔)
    public float Cap(float dmg)
    {
        if (!erosion) return dmg;
        PlayerController p = Hostile.Player;
        if (p == null) return dmg;
        float scale = Mathf.Max(0.01f, GameMode.DamageMul * Chapters.HurtMul);
        return Mathf.Min(dmg, p.PlayerMaxHealth * SpaceErosion.HitShare / scale);
    }
    // 새 탄을 쏴도 되는지 (침식은 화면 탄 220 까지)
    public bool CanShoot => !erosion || HostileProjectile.Live.Count < SpaceErosion.BulletCap;
    // 한 묶음 n 발을 다 쏴도 침식 상한 220 을 넘지 않는지 (고리처럼 한꺼번에 쏘는 것)
    public bool CanShootMany(int n) => !erosion || HostileProjectile.Live.Count + n <= SpaceErosion.BulletCap;
    // 침식 안의 튕기는 것 전체 (산성 덩어리 + 개발자 버그 · 상한 4 를 같이 셈)
    public static int Bouncers => DomainBall.Live.Count + DevBug.Count;
    public string Label => erosion ? "공간 침식" : "보스 결계";

    // 땅을 치는 공격: 경고 원 → 떨어져 터짐 (대기열 DomainStrikes · 타격마다 코루틴을 만들지 않음)
    // aimed: 플레이어를 직접 노리는 타격 (침식에서는 0.35초에 하나만 플레이어 자리에)
    public void Strike(Vector3 at, float r, float warn, Color color, float damage, string fx, bool aimed = false)
        => DomainStrikes.Add(this, at, r, warn, color, damage, fx, aimed);

    public HostileProjectile Shoot(Vector3 pos, Vector2 dir, float speed, float damage, float radius, Color color, float scale, float life = 4f)
    {
        HostileProjectile h = Hostile.Shoot(pos, dir, speed, damage, radius, color, scale, life);
        if (h != null) h.label = Label;
        return h;
    }

    // 결계 공격이 플레이어를 직접 맞힐 때 (「받은 피해 · 쓰러진 곳」 이름이 보스 결계 · 공간 침식으로)
    public bool Hit(PlayerController p, float damage)
    {
        if (p == null) return false;
        AttackLabel.Pending = Label;
        bool hit = p.TryHit(damage);
        AttackLabel.Pending = null;
        return hit;
    }

    // 공격 코루틴을 멈춤 (이미 예고된 땅 타격은 끝까지 떨어짐)
    // 이 맥락이 만든 튕기는 것도 지우고, 개발자 · 엄마 칸이면 따로 띄운 하위 패턴 · 버그도 함께 멈춤 (코루틴이 끊겨 끝 정리가 돌지 않아도)
    public void StopAttack()
    {
        if (routine != null && runner != null) runner.StopCoroutine(routine);
        routine = null;
        if (dev != null) { dev.Stop(); dev = null; }
        DomainBall.DestroyOf(this);
    }

    // 이 보스 전용 장식을 지움 (침식 중 그 보스가 쓰러질 때 · 끝날 때는 root 와 함께 지워짐)
    public void ClearDecor()
    {
        for (int i = 0; i < decor.Count; i++) if (decor[i] != null) Object.Destroy(decor[i]);
        decor.Clear();
    }

    // 멈춘 리치 빔: 매 프레임 새로 만들던 때처럼 0.08초 동안 흐려지며 스스로 사라짐
    public void FadeBeams()
    {
        for (int i = 0; i < lichBeams.Length; i++)
        {
            FxAnim b = lichBeams[i];
            lichBeams[i] = null;
            if (b == null) continue;
            b.life = 0.08f;
            b.Restart();
        }
        // 예고선도 함께 (멈춘 각도에 남아 나오지 않을 광선을 예고하지 않게)
        for (int i = 0; i < lichWarn.Length; i++)
        {
            if (lichWarn[i] != null) Object.Destroy(lichWarn[i].gameObject);
            lichWarn[i] = null;
        }
    }
}

// 결계 땅 타격 대기열 (1.2.5~): 예전엔 타격마다 Spike 코루틴 + WaitForSeconds 를 만들었음 → 구조체 링 128칸
// SpaceErosion.Update 가 한 프레임 한 번 Tick (장면에 늘 하나 · 보스가 지워져도 이미 예고된 타격은 끝까지 떨어짐)
public static class DomainStrikes
{
    struct Entry
    {
        public bool on;
        public Vector3 at;
        public float r, fireAt, damage;
        public Color color;
        public string fx;
        public bool erosion;
        public bool aimed;
    }

    const int Capacity = 128, SoftCap = 96;
    static readonly Entry[] q = new Entry[Capacity];
    static int count, tickFrame = -1;
    static float aimNext;

    // 고요의 눈 (공간 침식의 떠도는 안전 지대 · SpaceErosion 이 매 프레임 적음): 땅 타격의 원이 눈과 겹치면 밖으로 밀어냄
    public static bool EyeOn;
    public static Vector3 EyePos;
    public const float EyeRadius = 3.5f;

    public static int Count => count;

    public static void Add(DomainCtx d, Vector3 at, float r, float warn, Color color, float damage, string fx, bool aimed)
    {
        SpaceErosion.Ensure();          // Tick 을 돌릴 곳 (장면에 하나)
        if (d.erosion)
        {
            warn = Mathf.Max(warn, SpaceErosion.WarnFloor);          // 예고는 줄이지 않고 최소 0.8초
            if (aimed)
            {
                // 노림 토큰: 0.35초에 하나만 플레이어 자리 · 그 사이 요청은 플레이어로부터 4 ~ 9 떨어진 자리로
                if (Time.time < aimNext)
                {
                    aimed = false;
                    PlayerController p = Hostile.Player;
                    Vector3 me = p != null ? p.transform.position : d.center;
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    at = me + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(4f, 9f);
                }
                else aimNext = Time.time + SpaceErosion.AimGap;
            }
            if (count >= SoftCap && !aimed) return;       // 동시 수 상한: 무작위 자리 타격부터 버림 (나를 노린 타격은 버리지 않음)
        }
        if (count >= Capacity) return;
        at.z = 0f;
        float edge = d.radius - 0.5f;
        if ((at - d.center).sqrMagnitude > edge * edge) at = d.center + (at - d.center).normalized * edge;
        if (d.erosion && EyeOn)
        {
            Vector3 off = at - EyePos;
            off.z = 0f;
            float clear = EyeRadius + r + 0.3f;
            if (off.sqrMagnitude < clear * clear)
            {
                if (off.sqrMagnitude < 0.0001f) off = Vector3.right;
                at = EyePos + off.normalized * clear;
                if ((at - d.center).sqrMagnitude > edge * edge) at = d.center + (at - d.center).normalized * edge;
            }
        }
        Hostile.Circle(at, r, warn, BossUltimate.Bright(color));
        // 1.6.0~ 안쪽 흰 원(장식 · 판정은 바깥 원 r)은 Legacy 일 때만 — 흰 원 + 흰 닫히는 고리가 겹쳐 어지러웠고, 침식에선 타격마다 경고 2개씩이라 렌더러도 많았음
        if (VisualRules.Legacy) Hostile.Circle(at, r * 0.55f, warn, new Color(1f, 1f, 1f, 0.75f));
        BossUltimate.Warn(at, warn, r);
        for (int i = 0; i < Capacity; i++)
        {
            if (q[i].on) continue;
            q[i] = new Entry { on = true, at = at, r = r, fireAt = Time.time + warn, damage = damage, color = color, fx = fx, erosion = d.erosion, aimed = aimed };
            count++;
            return;
        }
    }

    public static void Tick()
    {
        if (tickFrame == Time.frameCount) return;
        tickFrame = Time.frameCount;
        if (count == 0) return;
        float now = Time.time;
        for (int i = 0; i < Capacity; i++)
        {
            if (!q[i].on || now < q[i].fireAt) continue;
            Entry e = q[i];
            q[i].on = false;
            count--;
            Fx.Spawn(e.fx, e.at + Vector3.up * (e.fx == "fx_meteor" ? 0.5f : 1.2f), e.r * 2.2f, Color.white, 18f, 0f, 14);
            Fx.Spawn("fx_explosion", e.at, e.r * 1.8f, e.color, 20f);
            AttackLabel.Pending = e.erosion ? "공간 침식" : "보스 결계";
            Hostile.HitCircle(e.at, e.r, e.damage);
            AttackLabel.Pending = null;
        }
    }

    // 장면이 바뀔 때 (SpaceErosion.OnDestroy)
    public static void Clear()
    {
        System.Array.Clear(q, 0, q.Length);
        count = 0;
        aimNext = 0f;
        EyeOn = false;
    }
}

// ================================================================= 보스마다 다른 결계 공격 (ctx 기반 · static)
// 개별 결계: tempo 1 · erosion false → 예전 BossUltimate 인스턴스 메서드와 같은 결과
public partial class BossUltimate
{
    // 칸마다 결계 공격 (개발자 · 엄마 칸 4 · 5 는 BossUltimate.Developer.cs 가 DevMomBarrage 를 채움 · 없으면 null)
    internal static IEnumerator BarrageFor(DomainCtx d)
    {
        switch (d.slot)
        {
            case 0: return LichBarrage(d);
            case 1: return HellBarrage(d);
            case 2: return SlimeBarrage(d);
            case 3: return MirrorBarrage(d);
        }
        IEnumerator routine = null;
        DevMomBarrage(d, ref routine);
        return routine;
    }

    // 이 칸에 결계 공격이 있는지 (개발자 · 엄마 결계가 아직 없으면 게이지가 차도 펼치지 않음)
    internal static bool HasBarrage(int slot)
    {
        if (slot < 4) return true;
        bool has = false;
        DevMomHasBarrage(slot, ref has);
        return has;
    }

    // ---------------- 개발자 · 엄마 결계 연결 지점 (BossUltimate.Developer.cs 에서 구현)
    // routine = d.slot == 4 ? DevBarrage(d) : MomBarrage(d);  — 침식이면 d.erosion (강제 종료 · 통금 시간은 쓰지 않음) · 간격 × d.DevTempo
    static partial void DevMomBarrage(DomainCtx d, ref IEnumerator routine);
    // has = true;  (slot 4 · 5 결계 공격이 있음)
    static partial void DevMomHasBarrage(int slot, ref bool has);

    static readonly WaitForSeconds Wait016 = new WaitForSeconds(0.16f), Wait012 = new WaitForSeconds(0.12f), Wait115 = new WaitForSeconds(1.15f),
                                   Wait12 = new WaitForSeconds(1.2f), Wait14 = new WaitForSeconds(1.4f);

    // 망자의 묘역 (리치 왕): 회전하는 영혼 광선 넷 + 틈이 있는 영혼 고리 + 발밑 뼈 가시
    static IEnumerator LichBarrage(DomainCtx d)
    {
        Color soul = Info[0].color, curse = new Color(0.75f, 0.4f, 1f);
        int arms = d.erosion && d.crowd ? 3 : 4;
        float step = 360f / arms;
        float spinSpeed = d.erosion ? 22f * 1.25f : 22f;      // 침식: 회전만 × 1.25 (0.6배로 나누면 피하는 시간이 줄어서)
        float angle = Random.Range(0f, 90f), spin = Random.value < 0.5f ? spinSpeed : -spinSpeed;
        float nextRing = Time.time + 0.6f, nextSpike = Time.time;
        float len = Radius * 2f;
        LineRenderer[] warn = d.lichWarn;
        for (int i = 0; i < arms; i++) { warn[i] = Hostile.NewLine("LichBeamWarn", new Color(soul.r, soul.g, soul.b, 0.3f), 0.18f, 30); warn[i].transform.SetParent(d.root, true); }
        float live = Time.time + 1.4f, beamHit = 0f;
        for (int i = 0; i < arms; i++)
        {
            float a0 = (angle + i * step) * Mathf.Deg2Rad;
            Warn(d.SrcPos + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * 7f, 1.4f, 1.6f);
        }
        FxAnim[] beams = d.lichBeams;
        Color[] beamColors = new Color[4];
        while (Time.time < d.until && d.Live)
        {
            angle += spin * Time.deltaTime;
            Vector3 c = d.SrcPos;
            bool on = Time.time > live;
            for (int i = 0; i < arms; i++)
            {
                float a = (angle + i * step) * Mathf.Deg2Rad;
                Vector3 end = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * len;
                warn[i].positionCount = 2;
                warn[i].SetPosition(0, c);
                warn[i].SetPosition(1, end);
                if (on)
                {
                    // 1.2.4: 빔은 처음 한 번 만들고 매 프레임 자리 · 각도 · 길이만 옮김
                    if (beams[i] == null)
                    {
                        beams[i] = Fx.Beam(c, end, 1.4f, soul, -1f, 22);
                        if (beams[i] != null) beamColors[i] = beams[i].sr.color;
                    }
                    else PlaceBeam(beams[i], c, end, beamColors[i]);
                }
            }
            PlayerController p = Hostile.Player;
            if (on && p != null && (beamHit -= Time.deltaTime) <= 0f)
                for (int i = 0; i < arms; i++)
                {
                    float a = (angle + i * step) * Mathf.Deg2Rad;
                    Vector3 end = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * len;
                    if (Hostile.HurtToSegment(p, c, end) < 0.85f) { d.Hit(p, d.Dmg(16f)); beamHit = 0.4f; break; }
                }
            if (Time.time >= nextRing)
            {
                nextRing = Time.time + d.Gap(1.75f);       // 1.0.1: 2장 결계 살짝 완화 (1.5 → 1.75)
                if (d.CanShootMany(21))      // 26칸 가운데 틈 다섯을 뺀 21발
                {
                    int n = 26, gap = Random.Range(0, n);
                    for (int i = 0; i < n; i++)
                    {
                        if (Mathf.Abs(i - gap) <= 2 || Mathf.Abs(i - gap) >= n - 2) continue;     // 다섯 칸짜리 틈
                        float a = i * Mathf.PI * 2f / n;
                        d.Shoot(c, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 6.5f, d.Dmg(14f), 0.5f, curse, 1.6f, 3f);
                    }
                    Hostile.Play("shimmer", 0.5f, 0.6f);
                }
            }
            if (Time.time >= nextSpike && p != null)
            {
                nextSpike = Time.time + d.Gap(0.68f);      // 1.0.1: 0.55 → 0.68 · 예고 0.75 → 0.85
                Vector3 at = p.transform.position + (Vector3)(Random.insideUnitCircle * 1.5f);
                d.Strike(at, 1.7f, 0.85f, soul, d.Dmg(18f), "fx_spike", true);
            }
            yield return null;
        }
        for (int i = 0; i < beams.Length; i++) { if (beams[i] != null) Object.Destroy(beams[i].gameObject); beams[i] = null; }
    }

    // Fx.Beam 과 같은 계산으로 이미 있는 빔을 옮김 (두께 · 그림 · 색은 만들 때 그대로, 수명 · 프레임은 처음부터)
    static void PlaceBeam(FxAnim a, Vector3 from, Vector3 to, Color color)
    {
        Vector3 d = to - from;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        Vector3 mid = (from + to) * 0.5f;
        Transform tr = a.transform;
        tr.position = new Vector3(mid.x, mid.y, 0f);
        tr.rotation = Quaternion.Euler(0f, 0f, angle);
        if (a.frames != null && a.frames.Length > 0) a.sr.sprite = a.frames[0];
        float unit = a.sr.sprite.bounds.size.x;
        Vector3 s = tr.localScale;
        tr.localScale = new Vector3(d.magnitude / unit, s.y, 1f);
        a.sr.color = color;
        a.Restart();
    }

    // 연옥 낙화 (지옥의 군주): ① 곳곳에 한꺼번에 떨어지는 운석 비 → ② 내 자리의 거대 운석 → ③ 틈이 있는 운석 행진 + 내내 발밑 불씨 운석
    // 침식: 운석 비 8 + 3 · 행진 9줄 × 6 · 줄 간격 × 0.75 (밀도 조정)
    static IEnumerator HellBarrage(DomainCtx d)
    {
        Color fire = Info[1].color;
        d.runner.StartCoroutine(HellEmbers(d, fire));
        int beat = 0;
        int rain = d.erosion ? 8 : 12, around = d.erosion ? 3 : 5;
        int rows = d.erosion ? 4 : 6, cols = d.erosion ? 3 : 4;
        float arc = 360f / around;
        while (Time.time < d.until && d.Live)
        {
            PlayerController p = Hostile.Player;
            Vector3 me = p != null ? p.transform.position : d.center;
            switch (beat++ % 3)
            {
                case 0:     // 운석 비: 결계 곳곳 + 내 둘레 (내 발밑 바로는 아님)
                    for (int i = 0; i < rain; i++)
                    {
                        Vector3 at = d.center + (Vector3)(Random.insideUnitCircle * (d.radius - 2f));
                        d.Strike(at, 2.6f, 1.05f, fire, d.Dmg(22f), "fx_meteor");
                    }
                    for (int i = 0; i < around; i++)
                    {
                        float a = (i * arc + Random.Range(0f, arc)) * Mathf.Deg2Rad;
                        d.Strike(me + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(3f, 6f), 2.4f, 1.05f, fire, d.Dmg(22f), "fx_meteor");
                    }
                    Hostile.Play("whoosh", 0.6f, 0.7f);
                    yield return new WaitForSeconds(d.Gap(1.7f));
                    break;
                case 1:     // 거대 운석: 지금 내 자리 (반지름 6, 예고 1.3초) + 둘레 여섯 곳 → 피한 자리로 한 번 더
                {
                    d.Strike(me, 6f, 1.3f, fire, d.Dmg(30f), "fx_meteor");
                    float off = Random.Range(0f, 60f);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = (off + i * 60f) * Mathf.Deg2Rad;
                        d.Strike(me + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 9.5f, 2.2f, 1.3f, fire, d.Dmg(22f), "fx_meteor");
                    }
                    Hostile.Play("whoosh", 0.8f, 0.5f);
                    yield return Wait14;            // 후속까지는 침식에서도 그대로 (경고 1.3 + 0.1 보다 짧아지지 않게)
                    if (!d.Live) break;
                    p = Hostile.Player;
                    if (p != null) d.Strike(p.transform.position, 4.5f, 1.1f, fire, d.Dmg(26f), "fx_meteor");
                    Hostile.Play("whoosh", 0.8f, 0.6f);
                    yield return new WaitForSeconds(d.Gap(1.3f));
                    break;
                }
                default:    // 운석 행진: 한쪽에서 반대쪽으로 줄지어 떨어짐, 줄 가운데 한 칸은 비움
                {
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang));
                    Vector3 side = new Vector3(-dir.y, dir.x);
                    int gap = Random.Range(-2, 3);
                    for (int row = -rows; row <= rows && d.Live; row++)
                    {
                        Vector3 rowAt = me + dir * (row * 4f);
                        for (int k = -cols; k <= cols; k++)
                        {
                            if (k == gap) continue;                 // 피할 틈
                            d.Strike(rowAt + side * (k * 4f), 2f, 1f, fire, d.Dmg(22f), "fx_meteor");
                        }
                        yield return d.erosion ? Wait012 : Wait016;
                    }
                    yield return new WaitForSeconds(d.Gap(0.8f));
                    break;
                }
            }
        }
    }

    // 연옥 낙화 내내: 0.9초마다 내 자리에 작은 불씨 운석 (멈춰 있으면 맞음)
    static IEnumerator HellEmbers(DomainCtx d, Color fire)
    {
        yield return Wait12;
        while (Time.time < d.until && d.Live)        // 결계가 일찍 끝나면 (보스 처치 · 쓰러짐) 같이 멈춤
        {
            PlayerController p = Hostile.Player;
            if (p != null) d.Strike(p.transform.position, 1.6f, 0.9f, fire, d.Dmg(16f), "fx_meteor", true);
            yield return new WaitForSeconds(d.Gap(0.9f));
        }
    }

    // 거울의 방 (거울의 군주): 결계 둘레의 거울 여덟 개가 차례로 플레이어를 노려 광선을 되쏘고,
    // 깨진 거울 조각이 발밑에 쏟아지며, 가끔 보스에게서 반사 조각 고리가 퍼짐
    static IEnumerator MirrorBarrage(DomainCtx d)
    {
        Color glass = Info[3].color;
        const int count = 8;
        float ringR = Mathf.Min(d.radius, Radius) - 2f;     // 침식에서도 거울 고리는 35 기준 (침식 고리 끝 46까지 가지 않게)
        float beamLen = Mathf.Min(d.radius, Radius) * 2.1f;
        Vector3[] mirrors = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float a = (i * 360f / count + 22.5f) * Mathf.Deg2Rad;
            mirrors[i] = d.center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * ringR;
            FxAnim m = Fx.Play("fx_rune", mirrors[i], 4f, new Color(glass.r, glass.g, glass.b, 0.85f), 1f, 0f, 20, true, d.until - Time.time);
            if (m != null) { m.spin = 60f; m.transform.SetParent(d.root, true); d.decor.Add(m.gameObject); }
        }
        float nextBeam = Time.time + 0.6f, nextShard = Time.time, nextRing = Time.time + 2.5f;
        int turn = 0;
        while (Time.time < d.until && d.Live)
        {
            PlayerController p = Hostile.Player;
            if (Time.time >= nextBeam && p != null && (!d.erosion || d.mirrorPairs < 2))
            {
                nextBeam = Time.time + d.Gap(1.25f);
                // 마주 보는 거울 둘이 동시에 (사이로 빠져나갈 수 있게 한 쌍씩)
                d.mirrorPairs++;
                d.runner.StartCoroutine(MirrorBeams(d, mirrors[turn % count], mirrors[(turn + count / 2 + 1) % count], p.transform.position, glass, beamLen));
                turn += 3;
            }
            if (Time.time >= nextShard && p != null)
            {
                nextShard = Time.time + d.Gap(0.45f);
                bool near = Random.value < 0.6f;
                Vector3 at = near ? p.transform.position + (Vector3)(Random.insideUnitCircle * 3f) : d.center + (Vector3)(Random.insideUnitCircle * (d.radius - 1f));
                d.Strike(at, 1.8f, 1f, glass, d.Dmg(18f), "fx_spike", near);
            }
            if (Time.time >= nextRing && d.src != null)
            {
                nextRing = Time.time + d.Gap(3.6f);
                int n = d.erosion && d.crowd ? 14 : 18;
                if (d.CanShootMany(n))
                {
                    float off = Random.Range(0f, 360f);
                    for (int i = 0; i < n; i++)
                    {
                        float a = (off + i * 360f / n) * Mathf.Deg2Rad;
                        d.Shoot(d.src.transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 6.5f, d.Dmg(14f), 0.5f, glass, 1.6f, 5f);
                    }
                    Hostile.Play("shimmer", 0.5f, 1.2f);
                }
            }
            yield return null;
        }
    }

    // 마주 보는 거울 한 쌍의 광선 (예고 1.15초 → 발사)
    static IEnumerator MirrorBeams(DomainCtx d, Vector3 fromA, Vector3 fromB, Vector3 aim, Color glass, float len)
    {
        Vector3 toA = fromA + (aim - fromA).normalized * len, toB = fromB + (aim - fromB).normalized * len;
        Hostile.Line(fromA, toA, 2f, 1.15f, Bright(glass));
        Warn(Vector3.Lerp(fromA, aim, 0.5f), 1.15f, 1.6f);
        Hostile.Line(fromB, toB, 2f, 1.15f, Bright(glass));
        Warn(Vector3.Lerp(fromB, aim, 0.5f), 1.15f, 1.6f);
        yield return Wait115;
        d.mirrorPairs = Mathf.Max(0, d.mirrorPairs - 1);
        Color beam = new Color(0.85f, 0.95f, 1f);
        Fx.Beam(fromA, toA, 1.8f, beam, 0.35f);
        Fx.Beam(fromB, toB, 1.8f, beam, 0.35f);
        PlayerController p = Hostile.Player;
        if (p != null && Hostile.HurtToSegment(p, fromA, toA) < 1.1f) d.Hit(p, d.Dmg(22f));
        if (p != null && Hostile.HurtToSegment(p, fromB, toB) < 1.1f) d.Hit(p, d.Dmg(22f));
        Hostile.Play("zap", 0.6f, 0.9f);
        Hostile.Play("zap", 0.6f, 0.9f);
    }

    // 산성 범람 (킹 슬라임): 결계 벽에 튕기는 슬라임 덩어리 + 쏟아지는 산성 비 (2.2.1~ 9초 · 덩어리 최대 3 · 침식은 튕기는 것 전체 4)
    static IEnumerator SlimeBarrage(DomainCtx d)
    {
        Color acid = Info[2].color;
        float nextBall = Time.time, nextRain = Time.time + 0.3f;
        int cap = d.erosion ? SpaceErosion.BouncerCap : 3;
        while (Time.time < d.until && d.Live)
        {
            PlayerController p = Hostile.Player;
            if (Time.time >= nextBall && (d.erosion ? DomainCtx.Bouncers : DomainBall.Live.Count) < cap)
            {
                nextBall = Time.time + d.Gap(1.4f);
                Bouncer(d, d.SrcPos, acid, d.until - Time.time);
            }
            if (Time.time >= nextRain && p != null)
            {
                nextRain = Time.time + d.Gap(1.3f);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 at = i == 0 ? p.transform.position : p.transform.position + (Vector3)(Random.insideUnitCircle * 5f);
                    d.Strike(at, 1.8f, 1.3f, acid, d.Dmg(16f), "fx_geyser", i == 0);
                }
            }
            yield return null;
        }
        DomainBall.DestroyOf(d);
    }

    // 결계 벽에 튕기는 것 (산성 덩어리 · 개발자 버그 등이 같이 씀) — 새 오브젝트 + AddComponent (복제 아님)
    internal static DomainBall Bouncer(DomainCtx d, Vector3 from, Color color, float life, float speed = 5.5f, float damageRaw = 15f, float hitRadius = 1.1f)
    {
        if (Hostile.Glow == null) return null;
        GameObject g = SpecialAbilities.MakeSprite("AcidBall", Hostile.Glow, from, 0.22f, new Color(color.r, color.g, color.b, 0.95f), "Effect", 20);
        HostileProjectile.Dress(g, color, 1.6f * HostileProjectile.LookMul);       // 1.0.1: 다른 적 탄처럼 테두리 구슬로 또렷하게
        DomainBall b = g.AddComponent<DomainBall>();
        b.ctx = d;
        b.center = d.center;
        b.radius = d.radius - 0.6f;
        b.velocity = Random.insideUnitCircle.normalized * speed;
        b.damage = d.Dmg(damageRaw);
        b.hitRadius = hitRadius;
        b.life = Mathf.Max(1f, life);
        b.label = d.Label;
        return b;
    }
}
