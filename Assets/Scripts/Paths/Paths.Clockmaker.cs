using System.Collections.Generic;
using UnityEngine;

// 시계공 갈래 5개 (기획서 3-7)
// 시계공 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Clockmaker.cs · 기존 파일(CharacterKit.Clockmaker.cs · SignatureSkills.Clockmaker.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 효과가 사는 곳
//   톱니 모양 · 수 · 정각 · 되감기 수치 → CharacterKit.Clockmaker 가 아래 static 묻기(GearsPer · NormalMul · BigMul · RewindBack …)를 읽음
//   잔상 · 갈라진 톱니 · 녹 · 끌어당김 · 대정각 · 게이지 · 되감은 뒤 무적 → 이 파일 (판마다 새 인스턴스 · 상태는 until 시각)
//   스톱워치 기본(시간 빚 공명 II) → SignatureSkills.Clockmaker.OnRewound
// 안전 (기획서 6-3)
//   코루틴으로 켜고 끄는 상태 없음 (모두 Time.time 비교) · 무적은 Paths.Invuln 만 · 보스 invulnerable 은 건드리지 않음
//   새 오브젝트는 SignatureSkills.Shot(탄 풀) 만 · 잔상 동시 30 · 잔상 세대 2 · 튕김 깊이 12 · 갈라짐 초당 16 (코드에 박음)
//   쓰러지는 중엔 Tick 이 안 불리고, 되감기 · 피격 훅도 IsDying 이면 아무것도 안 함
public sealed class ClockmakerPaths : HeroPaths
{
    public const int Swarm = 0, Grand = 1, Travel = 2, Echo = 3, Debt = 4;
    // 새 교차 번호 (BuildCrosses 순서)
    public const int XGearEcho = 2, XNoonRewind = 3;

    // ---------------- 수치 (카드 글과 같게 · 바꾸면 아래 문장과 LocTable.Paths.Clockmaker 도 같이)
    // 톱니 떼
    public const float SwarmGearMul = 0.65f, SwarmTripleMul = 0.55f;    // 두 개씩 65% · 극의 세 개씩 55%
    public const float TwinBossMul = 0.45f;                             // 덧붙은 톱니가 보스에게 (−55%) — 보스 화력 ±8% 묶기
    public const float SwarmSpread = 6f;                                // 한 묶음 톱니 사이 각도
    public const int SwarmNeed = 4, SwarmBounce = 1, SwarmRes2Bounce = 2, SplitHop = 3;
    public const float RustPer = 0.04f, RustTime = 4f;
    public const int RustMax = 5;
    public const float SplitMul = 0.5f;
    const int SplitPerSec = 16;                                         // 갈라진 톱니 초당 상한 (탄 폭증 방지)
    public const int MaxHop = 12;                                       // 튕김 세대 상한 (기획서 6-3 (2))
    // 대시계
    public const float GrandSize = 2f, GrandDmg = 1.5f, GrandNormalMul = 0.75f;
    public const float PullTime = 0.5f, PullRadius = 4.5f, PullSpeed = 7f;
    public const float GrandNoonRadius = 6.5f, GrandNoonMul = 1f;       // 대정각: 화면 1/3 쯤 · 정각 톱니 피해 100% (보통 정각 폭발 60%)
    // 시간 여행
    public const float TravelGauge = 1f / 0.6f, TravelBack = 2f, TravelInvuln = 1f, TravelInvulnGap = 4f;
    public const float TravelDmgMul = 0.6f, TravelLowHp = 0.6f, TravelStop = 0.5f;
    // 잔상
    public const float EchoDelay = 0.6f, EchoMul = 0.35f, EchoMasteryMul = 0.5f, EchoMainMul = 0.7f, EchoGen2Mul = 0.5f;
    public const int EchoMaxGen = 2, EchoMaxLive = 30, EchoMaxQueue = 60;
    // 시간 빚
    public const float DebtGauge = 2f, DebtLoss = 0.1f, DebtRate = 1.2f, DebtStopwatch = 0.2f;
    // 정각 되감기 (교차)
    public const float NoonRewindMul = 2f;
    const float GaugeCap = 3f;                                          // 시간 여행 × 시간 빚 × 낮은 체력이 겹쳐도 게이지 최대 3배

    // 피해 출처 (판 기록 dealt_by · 피해 표 이름)
    public const string TwinSrc = "덧붙은 톱니", EchoSrc = "잔상 톱니", SplitSrc = "갈라진 톱니", GrandNoonSrc = "대정각", NoonRewindSrc = "정각 되감기";

    public override CharacterId Who => CharacterId.Clockmaker;
    public override string IconKey => "clockmaker";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "t.swarm", name = "톱니 떼", tagline = "두 개씩 튕기기 · 무리",
            seed = "톱니 떼", seedDesc = "톱니를 두 개씩 던지고 (한 개당 피해 65%), 튕김이 1회 늘어납니다.",
            cost = "정각까지 필요한 눈금이 4 늘어나고, 덧붙은 톱니는 보스에게 피해가 55% 줄어듭니다.", strong = "무리", weak = "보스",
            cards = new[] { P, S(2), B }, evo = "te.gearstorm",
            res1 = "튕길 때마다 맞은 적에게 녹이 쌓여 받는 피해가 4%씩 늘어납니다. (최대 5중첩 · 4초)",
            res2 = "튕김이 2회 더 늘고, 세 번 이상 튕긴 톱니는 맞힐 때 작은 톱니 둘로 갈라집니다. (피해 50%)",
            mastery = "톱니를 세 개씩 던집니다. (한 개당 피해 55%)",
            color = new Color(0.85f, 0.75f, 0.55f), form = "뻐꾸기 시계" },
        new PathDef {
            key = "t.grand", name = "대시계", tagline = "거대 정각 톱니 · 보스 폭딜",
            seed = "대시계", seedDesc = "정각 톱니의 크기와 폭발 범위가 2배, 피해가 1.5배가 됩니다.",
            cost = "보통 톱니의 피해가 25% 줄어듭니다.", strong = "보스 · 폭딜", weak = "주기 공백",
            cards = new[] { M, S(4), S(6), A }, evo = "te.clockwork",
            res1 = "정각 톱니가 날아가는 0.5초 동안 둘레 적을 끌어당깁니다. (보스 제외)",
            res2 = "세 번째 정각마다 대정각이 터집니다. (화면 1/3을 덮는 폭발 · 보스에 닿아도 터짐)",
            mastery = "세 번째 톱니마다 정각이 됩니다.",
            color = new Color(1f, 0.8f, 0.35f), form = "괘종시계" },
        new PathDef {
            key = "t.travel", name = "시간 여행", tagline = "잦은 되감기 · 무적 · 생존",
            seed = "시간 여행", seedDesc = "되감기 재사용 대기 −40%, 3초 전 대신 2초 전으로 돌아가고, 되감은 뒤 1초 동안 무적입니다. (무적은 4초에 한 번)",
            cost = "되감기가 주는 피해와 되갚는 피해가 40% 줄어듭니다.", strong = "생존", weak = "화력",
            cards = new[] { S(1), S(3), S(7) }, evo = "te.timeless",
            res1 = "출발점에서 도착점까지 선 위의 적이 0.5초 멈춥니다.",
            res2 = "체력이 낮을수록 되감기 게이지가 빨리 찹니다. (체력이 바닥에 가까우면 최대 +60%)",
            mastery = "되감기를 2번 모아 둡니다. (게이지가 가득 찬 뒤 한 번 더 찹니다)",
            color = new Color(0.6f, 0.8f, 1f), form = "모래시계" },
        new PathDef {
            key = "t.echo", name = "잔상", tagline = "모든 톱니가 두 번 · 균형",
            seed = "잔상 장인", seedDesc = "모든 톱니가 0.6초 뒤 던진 자리에서 잔상으로 한 번 더 날아갑니다. (피해 35%)",
            cost = "본 톱니의 피해가 30% 줄어듭니다.", strong = "무리 · 보스 균형", weak = "빠른 적 (지연)",
            cards = new[] { K, S(5), G }, evo = "te.echoes",
            res1 = "잔상도 정각 눈금을 채웁니다.",
            res2 = "잔상이 한 번 더 잔상을 남깁니다. (피해 절반)",
            mastery = "잔상 피해가 50%가 됩니다.",
            color = new Color(0.8f, 0.65f, 1f) },
        new PathDef {
            key = "t.debt", name = "시간 빚", tagline = "필살기 두 배 · 맞으면 손해",
            seed = "시간 저당", seedDesc = "필살기 게이지가 2배로 찹니다.",
            cost = "맞으면 필살기 게이지를 10% 잃습니다.", strong = "필살기 회전 · 보스", weak = "피격",
            cards = new[] { S(8), S(9), S(0) }, evo = "te.debt",
            res1 = "게이지가 가득 차 있으면 공격 속도가 20% 오릅니다.",
            res2 = "되감기가 지난 3초 동안 준 피해의 20%를 둘레 적에게 다시 줍니다. (스톱워치 1단계 · 이미 있으면 더 큰 값)",
            mastery = "맞아도 게이지를 잃지 않습니다.",
            color = new Color(1f, 0.5f, 0.4f) },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Grand, Debt, "te.chime"),                     // 대종 (A · S0)
        Cross(Echo, Travel, "te.paradox"),                  // 역설 (G · S7)
        Cross(Swarm, Echo, "t.x.gearecho", "톱니 메아리", "튕기거나 갈라진 톱니도 잔상을 남깁니다."),
        Cross(Grand, Travel, "t.x.noonrewind", "정각 되감기", "되감은 자리에서 정각 톱니가 터집니다. (공격력 200%)"),
    };

    // ================================================================= 묻기 (CharacterKit.Clockmaker · SignatureSkills.Clockmaker 가 부름)
    // 이번 판이 시계공 갈래 규칙일 때의 효과 (아니면 null) — 다른 영웅 판에서 Paths.Has(0) 이 참이어도 시계공 효과가 켜지지 않게 형을 확인
    public static ClockmakerPaths Live => Paths.Active ? Paths.Hero as ClockmakerPaths : null;
    public static bool On(int path) => Paths.Has(path) && Paths.Hero is ClockmakerPaths;

    // 정각까지 더 필요한 눈금 (톱니 떼 대가)
    public static int ExtraNeed => On(Swarm) ? SwarmNeed : 0;
    // 튕김 + (톱니 떼 입문 1 · 공명 II 2)
    public static int ExtraBounce => On(Swarm) ? SwarmBounce + (Paths.Res(Swarm) >= 2 ? SwarmRes2Bounce : 0) : 0;
    // 한 방향에 던지는 보통 톱니 수 (톱니 떼 2 · 극의 3)
    public static int GearsPer => On(Swarm) ? (Paths.Mastered(Swarm) ? 3 : 2) : 1;
    // 묶음 톱니 하나의 피해 배율
    public static float PerGearMul => On(Swarm) ? (Paths.Mastered(Swarm) ? SwarmTripleMul : SwarmGearMul) * Paths.Power(Swarm) : 1f;
    public static bool SplitOn => On(Swarm) && Paths.Res(Swarm) >= 2;
    // 정각 톱니 크기 · 폭발 범위 배율 (대시계)
    public static float BigSize => On(Grand) ? GrandSize : 1f;
    // 갈래가 더하는 피해 (대가 빼고): 정각 = 대시계 ×1.5, 보통 = 톱니 떼 묶음 배율
    public static float GainMul(bool big, int per) => big ? (On(Grand) ? GrandDmg * Paths.Power(Grand) : 1f) : per > 1 ? PerGearMul : 1f;
    // 대가: 대시계 보통 톱니 −25%
    public static float GrandCost(bool big) => !big && On(Grand) ? GrandNormalMul : 1f;
    // 대가: 잔상 본 톱니 −30%
    public static float EchoCost => On(Echo) ? EchoMainMul : 1f;
    public static bool EchoOn => On(Echo);
    public static bool GearEchoOn => On(Echo) && Paths.CrossOn(XGearEcho);
    // 되감기: 몇 초 전으로 · 피해 배율 (시간 여행)
    public static float RewindBack => On(Travel) ? TravelBack : 3f;
    public static float RewindDmgMul => On(Travel) ? TravelDmgMul : 1f;
    // 시간 빚 공명 II: 스톱워치 기본
    public static bool DebtStopwatchOn => On(Debt) && Paths.Res(Debt) >= 2;

    // 덧붙은 톱니 표시 (보스에게 피해 −55%: Outgoing 이 출처로 알아봄) — 빈틈 조준 배율은 그대로 둠
    public static void MarkTwin(Bullet b)
    {
        if (b == null) return;
        float aim = b.aimMul;
        b.SetSource(TwinSrc);
        b.aimMul = aim;
    }

    // 예외가 나도 톱니 · 되감기 흐름이 끊기지 않게 (Paths 의 Safe 와 같은 뜻 · 1초에 한 번만 로그)
    float errAt = -9f;
    void Report(System.Exception e)
    {
        if (Time.unscaledTime - errAt < 1f) return;
        errAt = Time.unscaledTime;
        Debug.LogException(e);
    }

    // ================================================================= 상태 (판마다 새 인스턴스 · 모두 시각 값)
    // 대시계
    int noonCount;                  // 정각 수 (대정각 = 세 번째마다)
    sealed class NoonTrack { public Bullet b; public int gen; public float pullUntil, until, dmg; public bool grand, done; public Vector3 last; }
    readonly List<NoonTrack> noons = new List<NoonTrack>();
    // 잔상
    struct EchoJob { public float at; public Vector3 start; public Vector2 dir; public float dmg; public bool big, twin; public int gen; }
    readonly List<EchoJob> echoQ = new List<EchoJob>();
    readonly List<(Bullet b, int gen)> echoLive = new List<(Bullet, int)>();
    float echoFxAt;
    // 톱니 떼
    readonly Dictionary<int, (float until, int n)> rust = new Dictionary<int, (float, int)>();
    float rustPruneAt;
    float splitWindow;
    int splitCount;
    // 시간 여행 · 시간 빚
    float invulnReadyAt;
    float lastSp = -1f;
    float bank;                     // 시간 여행 극의: 두 번째 되감기 충전 (0 ~ 1)

    // ================================================================= 대시계
    // 극의: 세 번째 톱니마다 정각 (throwIndex = 이번이 몇 번째 던지기인지, 1부터)
    public bool ForceNoon(int throwIndex) => On(Grand) && Paths.Mastered(Grand) && throwIndex % 3 == 0;

    // 정각을 던질 때마다 부름 → 이번이 대정각인지 (공명 II · 세 번째마다)
    public bool NextNoonIsGrand()
    {
        if (!On(Grand) || Paths.Res(Grand) < 2) return false;
        noonCount++;
        return noonCount % 3 == 0;
    }

    // 정각 톱니 따라가기: 공명 I 끌어당김 · 대정각이 적에게 못 닿으면 보스에 닿거나 날아간 끝에서 터짐
    public void TrackNoon(Bullet b, bool grand)
    {
        if (b == null) return;
        bool pull = On(Grand) && Paths.Res(Grand) >= 1;
        if (!pull && !grand) return;
        noons.RemoveAll(t => t.done && Time.time >= t.pullUntil);
        if (noons.Count >= 8) noons.RemoveAt(0);
        noons.Add(new NoonTrack
        {
            b = b, gen = b.generation, last = b.transform.position, dmg = b.damage, grand = grand, done = !grand,
            pullUntil = pull ? Time.time + PullTime : 0f,
            until = Time.time + Mathf.Max(0.1f, b.lifetime) + 0.05f,
        });
    }

    // 대정각 폭발 (명중 자리 · 보스에 닿은 자리 · 날아간 끝). from: 터뜨린 탄 (따라가던 것이면 끝냄)
    public void GrandNoonBlast(Vector3 p, float dmg, Bullet from)
    {
        foreach (NoonTrack t in noons) if (from != null && t.b == from && t.gen == from.generation) t.done = true;
        Paths.Circle(p, GrandNoonRadius, dmg * GrandNoonMul, 3f, GrandNoonSrc);
        Color brass = new Color(1f, 0.8f, 0.35f);
        Fx.Spawn("fx_explosion", p, GrandNoonRadius * 2.2f, brass, 14f);
        ShockRing.Spawn(p, 0.6f, GrandNoonRadius, 0.5f, new Color(brass.r, brass.g, brass.b, 0.9f), 0.35f);
        ShockRing.Spawn(p, 0.3f, GrandNoonRadius * 0.6f, 0.35f, new Color(1f, 1f, 0.9f, 0.8f), 0.2f);
        Hostile.Shake(0.25f);
        Hostile.Play("bell", 0.9f, 0.6f);
        SpecialAbilities.SharedFx?.FloatText(p + Vector3.up * 1.8f, Loc.T("대정각!"), brass, 5.5f, 0f);
    }

    void NoonTick()
    {
        for (int i = noons.Count - 1; i >= 0; i--)
        {
            NoonTrack t = noons[i];
            bool alive = t.b != null && t.b.gameObject.activeInHierarchy && t.b.generation == t.gen;
            if (alive) t.last = t.b.transform.position;
            if (alive && Time.time < t.pullUntil) Pull(t.last);
            if (!t.done && (!alive || Time.time >= t.until || BossNear(t.last, 1.6f)))
            {
                t.done = true;
                GrandNoonBlast(t.last, t.dmg, null);
                SignatureSkills.BigGear(t.last);
            }
            if (t.done && (!alive || Time.time >= t.pullUntil)) noons.RemoveAt(i);
        }
    }

    static void Pull(Vector3 p)
    {
        float step = PullSpeed * Time.deltaTime;
        using var list = PhysHits.Circle(p, PullRadius, true);
        foreach (Collider2D c in list)
        {
            if (c == null || !c.CompareTag("enermy") || Paths.IsBoss(c.gameObject)) continue;
            Vector3 at = c.transform.position;
            if ((at - p).sqrMagnitude < 0.5f) continue;
            c.transform.position = Vector3.MoveTowards(at, p, step);
        }
    }

    static bool BossNear(Vector3 p, float r)
    {
        using var list = PhysHits.Circle(p, r, true);
        foreach (Collider2D c in list) if (c != null && c.CompareTag("boss")) return true;
        return false;
    }

    // ================================================================= 잔상
    // full: 잔상의 바탕 피해 (잔상 대가를 빼기 전 톱니 피해) — 여기서 잔상 비율 · Power 를 곱함
    public void QueueEcho(Vector3 start, Vector2 dir, float full, bool big, bool twin, int gen)
    {
        if (!On(Echo) || gen < 1 || gen > EchoMaxGen || full <= 0f) return;
        float ratio = (Paths.Mastered(Echo) ? EchoMasteryMul : EchoMul) * Paths.Power(Echo);
        if (gen > 1) ratio = EchoGen2Mul;           // 2세대: 1세대 잔상 피해의 절반 (full 에 이미 1세대 피해가 들어 옴)
        if (echoQ.Count >= EchoMaxQueue) echoQ.RemoveAt(0);
        echoQ.Add(new EchoJob { at = Time.time + EchoDelay, start = start, dir = dir, dmg = full * ratio, big = big, twin = twin, gen = gen });
    }

    void EchoTick()
    {
        int n = 0;
        while (echoQ.Count > 0 && echoQ[0].at <= Time.time && n < 12)       // 한 프레임에 최대 12개 (나머지는 다음 프레임)
        {
            EchoJob j = echoQ[0];
            echoQ.RemoveAt(0);
            n++;
            SpawnEcho(j);
        }
    }

    void SpawnEcho(EchoJob j)
    {
        SignatureSkills ss = SignatureSkills.Instance;
        PlayerController p = Hostile.Player;
        if (ss == null || p == null || p.IsDying) return;
        echoLive.RemoveAll(e => e.b == null || !e.b.gameObject.activeInHierarchy || e.b.generation != e.gen);
        if (echoLive.Count >= EchoMaxLive) return;          // 동시 30 (넘치면 새 잔상을 건너뜀 · 풀 탄이라 지우지 않음)
        Color tint = j.big ? new Color(1f, 0.85f, 0.55f, 0.6f) : new Color(0.8f, 0.68f, 1f, 0.62f);
        Bullet b = ss.Shot(j.start, j.dir, j.dmg, j.big ? 9999 : Mathf.Max(1, p.pene), j.big ? 30f : 38f, 16f, "fx_shuriken", j.big ? 1.9f * BigSize : 1f, tint, true);
        if (b == null) return;
        b.SetSource(j.twin ? TwinSrc : EchoSrc);
        if (j.big) b.hitOnce = new HashSet<int>();
        if (!j.big)
            b.onHitEnemy += (s, c) =>
            {
                // 공명 I: 잔상도 정각 눈금을 채움
                if (c != null && On(Echo) && Paths.Res(Echo) >= 1 && CharacterKit.Instance != null) CharacterKit.Instance.AddClockTicks(1f);
            };
        echoLive.Add((b, b.generation));
        if (Time.time >= echoFxAt)
        {
            echoFxAt = Time.time + 0.06f;
            Fx.Spawn("fx_stealth", j.start, j.big ? 2.4f : 1.5f, tint, 20f);
        }
        // 공명 II: 잔상이 한 번 더 잔상을 (세대 2 까지 · EchoMaxGen)
        if (j.gen < EchoMaxGen && Paths.Res(Echo) >= 2) QueueEcho(j.start, j.dir, j.dmg, j.big, j.twin, j.gen + 1);
    }

    // ================================================================= 톱니 떼
    // 공명 I: 튕길 때 그 적에게 녹 (받는 피해 +4% · 최대 5 · 4초)
    public void Rust(GameObject g)
    {
        if (g == null || !On(Swarm) || Paths.Res(Swarm) < 1) return;
        int id = g.GetInstanceID();
        int n = rust.TryGetValue(id, out var r) && r.until > Time.time ? r.n : 0;
        rust[id] = (Time.time + RustTime, Mathf.Min(RustMax, n + 1));
        if (n + 1 >= RustMax) Fx.Spawn("fx_spark", g.transform.position, 1.2f, new Color(0.85f, 0.45f, 0.2f), 22f);
    }

    // 공명 II: 세 번 이상 튕긴 톱니가 맞히면 작은 톱니 둘 (보스 쪽으로도 감)
    public void Split(Vector3 at, Vector2 dir, float dmg, GameObject except)
    {
        if (!SplitOn || dmg <= 0f) return;
        if (Time.time >= splitWindow) { splitWindow = Time.time + 1f; splitCount = 0; }
        if (splitCount >= SplitPerSec) return;
        splitCount += 2;
        SignatureSkills ss = SignatureSkills.Instance;
        if (ss == null) return;
        List<Transform> near = Paths.Nearest(at, 8f, 2, except);
        Color brass = new Color(0.95f, 0.75f, 0.45f);
        for (int i = 0; i < 2; i++)
        {
            Vector2 d = i < near.Count && near[i] != null
                ? ((Vector2)(near[i].position - at)).normalized
                : (Vector2)(Quaternion.Euler(0f, 0f, i == 0 ? 40f : -40f) * dir);
            if (d.sqrMagnitude < 0.01f) d = dir;
            float sd = dmg * SplitMul;
            Bullet b = ss.Shot(at, d, sd, 1, 32f, 9f, "fx_shuriken", 0.7f, brass, true);
            if (b == null) continue;
            b.SetSource(SplitSrc);
            if (except != null && except.TryGetComponent(out EnermyController e)) b.hitOnce = new HashSet<int> { e.GetInstanceID() };
            if (GearEchoOn) QueueEcho(at, d, sd, false, false, 1);       // 교차 톱니 메아리
        }
    }

    // ================================================================= 시간 여행 · 정각 되감기 (SignatureSkills.Rewound 가 부름)
    public void OnRewound(Vector3 from, Vector3 to, List<Collider2D> caught)
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        try
        {
            if (On(Travel))
            {
                // 되감은 뒤 무적 (Paths.Invuln 만 · 1초 · 4초에 한 번 — 잦은 되감기가 무적 연속이 되지 않게)
                if (Time.time >= invulnReadyAt)
                {
                    invulnReadyAt = Time.time + TravelInvulnGap;
                    Paths.Invuln(TravelInvuln);
                }
                // 공명 I: 지나온 선 위의 적 0.5초 멈춤 (보스는 EnermyController 가 없어 그대로)
                if (Paths.Res(Travel) >= 1 && caught != null)
                {
                    int fx = 0;
                    foreach (Collider2D c in caught)
                    {
                        if (c == null || !c.TryGetComponent(out EnermyController e)) continue;
                        e.Slow(0f, TravelStop);
                        if (fx++ < 8) Fx.Spawn("fx_stealth", c.transform.position, 1.3f, new Color(0.6f, 0.85f, 1f, 0.7f), 20f);
                    }
                }
            }
            // 교차 · 정각 되감기: 도착 자리에서 정각 톱니가 터짐
            if (CrossOn(XNoonRewind))
            {
                float r = 2.6f * BigSize;
                Paths.Circle(to, r, Paths.Atk * NoonRewindMul * Paths.Power(Grand), 2f, NoonRewindSrc);
                Color brass = new Color(1f, 0.8f, 0.35f);
                Fx.Spawn("fx_explosion", to, r * 2f, brass, 18f);
                ShockRing.Spawn(to, 0.4f, r, 0.35f, brass, 0.22f);
                Hostile.Play("bell", 0.7f, 0.85f);
            }
        }
        catch (System.Exception e) { Report(e); }
    }

    static bool CrossOn(int x) => Paths.CrossOn(x) && Paths.Hero is ClockmakerPaths;

    // ================================================================= 게이지 (시간 여행 −40% 재사용 대기 · 낮은 체력 · 시간 빚 ×2 · 극의 2회 충전)
    // SkillGauge 를 고치지 않고: 지난 프레임보다 늘어난 만큼(자연 충전 · 처치 가속 · 빌린 시간)에 배율을 더 붙임
    //   한 번에 크게 들어온 것(시간 빚 진화 30% 환급 · 극의 충전)은 덩어리라 배율을 붙이지 않음
    float GaugeMul(PlayerController p)
    {
        float m = 1f;
        if (On(Debt)) m *= DebtGauge;
        if (On(Travel))
        {
            m *= TravelGauge;
            if (Paths.Res(Travel) >= 2 && p.PlayerMaxHealth > 0f)
                m *= 1f + TravelLowHp * (1f - Mathf.Clamp01(p.PlayerHealth / p.PlayerMaxHealth));
        }
        return Mathf.Min(m, GaugeCap);
    }

    void GaugeTick()
    {
        PlayerController p = Hostile.Player;
        SkillGauge g = p != null ? p.skillGauge : null;
        if (g == null) { lastSp = -1f; return; }
        float mul = GaugeMul(p);
        float sp = g.SkillPoint;
        if (mul > 1.001f && lastSp >= 0f && sp > lastSp && !p.IsSkillUsing)
        {
            float gain = sp - lastSp;
            float lump = g.MaxSkillPoint * 0.04f + g.pointsPerSecond * 4f * Time.deltaTime;
            if (gain <= lump) g.AddSkillPoint(gain * (mul - 1f));
        }
        // 시간 여행 극의: 가득 찬 뒤에도 같은 빠르기로 한 번 더 모음
        if (On(Travel) && Paths.Mastered(Travel) && g.IsFull() && bank < 1f && !p.IsSkillUsing)
        {
            float tree = SpecialAbilities.SharedInstance != null ? SpecialAbilities.SharedInstance.TreeGaugeMul : 1f;
            float rate = g.pointsPerSecond * GameMode.GaugeMul / Mathf.Max(0.1f, CharacterData.Current.gauge) * (g.Boosted ? g.killBoost : 1f) * tree * mul;
            bank = Mathf.Min(1f, bank + rate * Time.deltaTime / Mathf.Max(1, g.MaxSkillPoint));
        }
        lastSp = g.SkillPoint;
    }

    // 필살기(되감기)를 쓴 직후 (게이지는 이미 비움): 극의로 모아 둔 한 번을 채움
    public override void Ult()
    {
        if (!On(Travel) || !Paths.Mastered(Travel) || bank < 1f) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying || p.skillGauge == null) return;
        bank = 0f;
        p.skillGauge.AddSkillPoint(p.skillGauge.MaxSkillPoint);
        lastSp = p.skillGauge.SkillPoint;
    }

    // ================================================================= 판 흐름 훅 (상태는 Paths.* 묻기로 다시 계산되므로 비움)
    public override void OnSeed(int path, bool replay) { }
    public override void OnSingle(int path, bool replay) { }
    public override void OnResonance(int path, int tier, bool replay) { }
    public override void OnComplete(int path, bool replay) { }
    public override void OnMastery(int path, bool replay) { }
    public override void OnCross(int cross, bool replay) { }

    public override void End()
    {
        // 플레이어 값을 직접 바꾼 것이 없음 — 붙잡은 탄 · 대기열만 놓음
        noons.Clear();
        echoQ.Clear();
        echoLive.Clear();
        rust.Clear();
    }

    // ================================================================= 전투 훅
    public override float Outgoing(GameObject target, float dmg)
    {
        float m = 1f;
        // 톱니 떼 대가: 덧붙은 톱니는 보스에게 −55% (출처 문자열을 먼저 비교 · 보스 판정은 그 뒤에만)
        if (DamageSource.Current == TwinSrc && Paths.IsBoss(target)) m *= TwinBossMul;
        // 톱니 떼 공명 I: 녹
        if (rust.Count > 0 && target != null && rust.TryGetValue(target.GetInstanceID(), out var r) && r.until > Time.time)
            m *= 1f + RustPer * r.n;
        return m;
    }

    public override void Hurt(float taken)
    {
        // 시간 빚 대가: 맞으면 게이지 10% (극의면 잃지 않음)
        if (taken <= 0f || !On(Debt) || Paths.Mastered(Debt)) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying || p.skillGauge == null) return;
        p.skillGauge.AddSkillPoint(-p.skillGauge.MaxSkillPoint * DebtLoss);
        lastSp = p.skillGauge.SkillPoint;
    }

    public override float RateMul()
    {
        // 시간 빚 공명 I: 게이지가 가득이면 공격 속도 +20%
        if (!On(Debt) || Paths.Res(Debt) < 1) return 1f;
        PlayerController p = Hostile.Player;
        return p != null && p.skillGauge != null && p.skillGauge.IsFull() ? DebtRate : 1f;
    }

    public override void Tick()
    {
        if (Paths.Hero != this) return;
        if (On(Debt) || On(Travel)) GaugeTick();
        if (noons.Count > 0) NoonTick();
        if (echoQ.Count > 0) EchoTick();
        if (rust.Count > 0 && Time.time >= rustPruneAt)
        {
            rustPruneAt = Time.time + 2f;
            pruneKeys.Clear();
            foreach (var kv in rust) if (kv.Value.until <= Time.time) pruneKeys.Add(kv.Key);
            foreach (int k in pruneKeys) rust.Remove(k);
        }
    }
    readonly List<int> pruneKeys = new List<int>();

    // HUD 입문 칸 오른쪽 위: 대시계 공명 II = 대정각까지 · 시간 여행 극의 = 모아 둔 되감기 (문자열은 상수라 새로 만들지 않음)
    public override string Badge(int path)
    {
        if (path == Grand && On(Grand) && Paths.Res(Grand) >= 2) return (noonCount % 3) switch { 1 => "1/3", 2 => "2/3", _ => "0/3" };
        if (path == Travel && On(Travel) && Paths.Mastered(Travel) && bank >= 1f) return "x2";
        return null;
    }

    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;     // 평타는 톱니 그대로 (모양은 CharacterKit.Clockmaker 가 묻기로 바꿈)
}
