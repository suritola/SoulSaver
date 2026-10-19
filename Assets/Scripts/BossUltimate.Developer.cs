using System.Collections;
using TMPro;
using UnityEngine;

// 개발자 · 개발자 엄마의 결계 (기획서 2026-10-18 공간 침식 D 절)
//   개발자 「디버그 모드」 12초 (금색 고리 · 터미널 초록 물듦): 브레이크포인트 · 한 줄씩 실행 · 버그 대량 발생 · 무한 루프 · 강제 종료
//   엄마 「엄마의 대청소」 14초 (분홍 고리 · 집 불빛 물듦): 등짝 스매싱 · 청소기 · 빨래 널기 · 잔소리 폭격 · 통금 시간
// 게이지는 칸 4 (개발자) · 5 (엄마) — 거울의 군주 칸 3 과 따로 (BossUltimate.SlotOf)
// 결계 공격은 영웅 흉내가 아닌 자기 고유의 힘: 결계 동안 영웅 필살기(DeveloperTurn)는 쓰지 않고, 끝나고 2초 뒤부터 다시 씀
// 한 대 = min(raw × Power, 최대 체력 × 0.2 / 배율) × DevPow → 개발자 16% · 엄마 32% 까지 (즉사 없음, 그 뒤 TryHit 의 보스전 40% 상한도 그대로)
// 예고는 DevTeleMul(0.6 · 0.5)을 쓰지 않고 표의 값 그대로 (바닥 0.6초 · 공간 침식 안에서는 0.8초)
//
// 공간 침식(SpaceErosion)이 섞어 쓰는 법
//   DevDomainCtx ctx = new DevDomainCtx();                 // 처음 한 번 만들고 다시 씀
//   ctx.Reset(src, runner, root, center, 48f, until, true); // erosion = true → 「강제 종료」 · 「통금 시간」 대신 「무한 루프」 · 「잔소리 폭격」 한 번 더
//   ctx.tempo = 0.6f;                                      // 1초마다 바꿔 넣음 (간격 × max(tempo, 0.75))
//   ctx.strike = Strike;                                   // 땅 타격을 침식 대기열로 (고요의 눈 · 노림 토큰 · 상한). damage 는 최종값(× 0.85 · 상한 적용 끝)
//   ctx.sharedBouncers = SlimeBallCount;                   // 튕기는 것 합계 상한 4 (개발자 버그와 같이 셈)
//   ctx.bulletCap = 220;
//   runner.StartCoroutine(BossUltimate.DevMomBarrage(ctx)); // 끝낼 때 ctx.Stop()
public delegate void DomainStrikeFn(Vector3 at, float r, float warn, Color color, float damage, string fx, bool aimed);

public sealed class DevDomainCtx
{
    public bosss src;
    public MonoBehaviour runner;            // 공격 코루틴을 돌리는 쪽 (개별 결계 = 그 BossUltimate · 침식 = SpaceErosion)
    public Transform root;                  // 결계 장식을 붙일 곳 (끝날 때 함께 지워짐)
    public Vector3 center;
    public float radius, baseRadius, until;
    public float tempo = 1f;                // 침식의 간격 배율 (개별 결계는 1)
    public bool erosion;
    public bool stopped;
    public Color ringTint;                  // 고리 색 (엄마 「통금 시간」에 빨갛게 깜빡임)
    public GameObject aura;                 // 보스 후광 (개발자는 노트북 화면처럼 초록으로 깜빡임)
    public DomainStrikeFn strike;           // null 이면 결계가 직접 떨어뜨림
    public System.Func<int> sharedBouncers; // 침식의 다른 튕기는 것 수 (null 이면 0)
    public int bulletCap = BossUltimate.DevDomainBulletCap;

    internal bool mom;
    internal float devPow = 0.8f;
    internal string label;
    internal Color tint;
    internal SpriteRenderer body;
    internal Vector3 lastPlayer, playerVel;
    internal float linesBusyUntil, xBusyUntil, curfewUntil, nagShownAt = -10f, dustAt;
    internal int bugPending, nagTurn;
    // 장식 (root 아래 · 결계마다 새로 만들고 함께 지워짐)
    internal TextMeshPro[] code;
    internal TextMeshPro comment, quitMark;
    internal TextMeshProUGUI nag;

    public bool Live => !stopped && Time.time < until && src != null && !src.IsDead && BossUltimate.PlayerOk;
    public float Tempo => erosion ? Mathf.Max(tempo, BossUltimate.DevMomTempoFloor) : 1f;
    public float WarnFloor => erosion ? 0.8f : 0.6f;

    public void Reset(bosss src, MonoBehaviour runner, Transform root, Vector3 center, float radius, float until, bool erosion)
    {
        this.src = src;
        this.runner = runner;
        this.root = root;
        this.center = center;
        this.radius = baseRadius = radius;
        this.until = until;
        this.erosion = erosion;
        stopped = false;
        tempo = 1f;
        strike = null;
        sharedBouncers = null;
        aura = null;
        bulletCap = erosion ? 220 : BossUltimate.DevDomainBulletCap;
        mom = src != null && src.developerMom;
        tint = mom ? EndlessMode.MomTint : BossUltimate.DevGold;
        ringTint = tint;
        // 받은 피해 · 쓰러진 곳 이름: 개별 결계는 BossSkills.DevName 과 같음 · 침식 안에서는 땅 타격 · 다른 보스 공격처럼 「공간 침식」 하나로
        label = erosion ? "공간 침식" : mom ? "개발자 엄마" : "개발자";
        BossSkills s = src != null ? src.GetComponent<BossSkills>() : null;
        devPow = s != null ? s.DevPower : (mom ? 1.6f : 0.8f);
        body = src != null ? src.GetComponent<SpriteRenderer>() : null;
        PlayerController p = Hostile.Player;
        lastPlayer = p != null ? p.transform.position : center;
        playerVel = Vector3.zero;
        linesBusyUntil = xBusyUntil = curfewUntil = dustAt = 0f;
        nagShownAt = -10f;
        bugPending = nagTurn = 0;
        code = null;
        comment = quitMark = null;
        nag = null;
    }

    public void Stop()
    {
        stopped = true;
        radius = baseRadius;
        ringTint = tint;
    }
}

public partial class BossUltimate
{
    public const float DevDuration = 12f, MomDuration = 14f;
    public const float DevMomTempoFloor = 0.75f;      // 침식 안에서 개발자 · 엄마 패턴 간격은 0.75배보다 빨라지지 않음
    public const int DevDomainBulletCap = 160;        // 결계가 새로 쏘는 탄 (BossSkills.DevBulletCap 400 도 그대로)
    const float DevHurtGrace = 0.2f;                  // 결계 중 피격 무적 +0.2초
    const float DevPendingMaxWait = 3f;               // 가득 찬 뒤 겹쳐 돌던 영웅 필살기를 기다리는 최대 시간
    const float DevHitShare = 0.2f;                   // BossSkills.MaxHitShare 와 같음 (× DevPow → 16% · 32%)
    const float ErosionDevDamageMul = 0.85f, ErosionDevHitShare = 0.25f;

    internal static readonly Color DevGold = new Color(1f, 0.85f, 0.35f);
    static readonly Color DevTerminal = new Color(0.2f, 1f, 0.45f);
    static readonly Color MomHome = new Color(1f, 0.78f, 0.55f);
    static readonly Color DevRed = new Color(1f, 0.25f, 0.2f);
    static readonly Color MomWhite = new Color(1f, 0.92f, 0.96f);

    // 결계 어둠 안쪽 물듦 색 (고리 색과 따로)
    static Color DarkTintOf(int slot, Color ring) => slot == DevSlot ? DevTerminal : slot == MomSlot ? MomHome : ring;

    internal static bool PlayerOk { get { PlayerController p = Hostile.Player; return p != null && !p.IsDying; } }

    // ---------------------------------------------------------------- 대기 (가득 찬 뒤 영웅 필살기를 멈추고 최대 3초)
    static BossUltimate devHolder;
    static float devHoldSince;
    public static bool DevHold => devHolder != null;

    bool DevReady()
    {
        if (devHolder != this) { devHolder = this; devHoldSince = Time.time; }
        BossSkills s = boss != null ? boss.GetComponent<BossSkills>() : null;
        return s == null || s.DevRunning == 0 || Time.time - devHoldSince >= DevPendingMaxWait;
    }

    void DevHoldRelease(bool force = false)
    {
        if (devHolder != this) return;
        if (force || Active || boss == null || boss.IsDead || gauge[kind] < 1f) devHolder = null;
    }

    // ---------------------------------------------------------------- 개별 결계 (BossUltimate.Run)
    DevDomainCtx devCtx;
    static bool devDomainLive;
    public static float DevHurtGraceBonus => devDomainLive ? DevHurtGrace : 0f;

    IEnumerator DevDomainAttack(float until)
    {
        if (devCtx == null) devCtx = new DevDomainCtx();
        devCtx.Reset(boss, this, domainRoot != null ? domainRoot.transform : null, center, Radius, until, false);
        devCtx.aura = aura;
        devDomainLive = true;
        return DevMomBarrage(devCtx);
    }

    void DevDomainStop()
    {
        devDomainLive = false;
        if (devCtx == null || devCtx.stopped) return;
        devCtx.Stop();
        BossSkills s = boss != null ? boss.GetComponent<BossSkills>() : null;
        if (s != null) s.AfterDomain(2f);       // 2초 뒤부터 다시 영웅 필살기
    }

    // 침식 · 개별 결계 모두 이걸로 (src 가 엄마면 엄마의 대청소)
    public static IEnumerator DevMomBarrage(DevDomainCtx d) => d.mom ? MomBarrage(d) : DevBarrage(d);

    // ---------------- 공간 침식 연결 (BossUltimate.Domain.cs 의 BarrageFor · HasBarrage 가 부르는 partial)
    static partial void DevMomHasBarrage(int slot, ref bool has) { if (slot == DevSlot || slot == MomSlot) has = true; }

    static partial void DevMomBarrage(DomainCtx d, ref IEnumerator routine)
    {
        if (d == null || (d.slot != DevSlot && d.slot != MomSlot)) return;
        routine = FromDomainCtx(d);
    }

    // 침식 맥락(DomainCtx)마다 개발자 · 엄마 맥락 하나씩 (미리 만든 것을 다시 씀)
    static readonly DevDomainCtx[] linkedCtx = new DevDomainCtx[2];
    static readonly DomainCtx[] linkedOwner = new DomainCtx[2];
    static readonly DomainStrikeFn[] linkedStrike = new DomainStrikeFn[2];

    static IEnumerator FromDomainCtx(DomainCtx d)
    {
        int i = d.slot == MomSlot ? 1 : 0;
        if (linkedCtx[i] == null) linkedCtx[i] = new DevDomainCtx();
        DevDomainCtx dc = linkedCtx[i];
        dc.Reset(d.src, d.runner, d.root, d.center, d.radius, d.until, d.erosion);
        // 땅 타격은 침식 대기열로 (고요의 눈 · 노림 토큰 · 상한 · 예고 0.8초) — 같은 DomainCtx 면 대리자를 다시 씀
        if (linkedOwner[i] != d || linkedStrike[i] == null)
        {
            linkedOwner[i] = d;
            linkedStrike[i] = (at, r, warn, color, damage, fx, aimed) => d.Strike(at, r, warn, color, damage, fx, aimed);
        }
        dc.strike = linkedStrike[i];
        dc.sharedBouncers = DomainBallCountFn;
        d.dev = dc;             // 침식이 이 보스의 공격을 바로 멈출 때 (StopAttack) 하위 패턴 · 버그도 함께 멈추게
        IEnumerator inner = DevMomBarrage(dc);
        while (true)
        {
            // 침식이 바꾸는 값을 그대로 (간격 배율 · 중심 · 끝 시각) · 그 보스가 쓰러지거나 침식이 끝나면 멈춤
            dc.tempo = d.tempo;
            dc.until = d.until;
            if (!d.Live) { dc.Stop(); yield break; }
            if (!inner.MoveNext()) { dc.Stop(); yield break; }
            yield return inner.Current;
        }
    }

    static int DomainBallCount() => DomainBall.Live.Count;
    static readonly System.Func<int> DomainBallCountFn = DomainBallCount;

    // ---------------------------------------------------------------- 공용 도우미
    static float Gap(DevDomainCtx d, float calm, float enraged) => (d.src != null && d.src.Enraged ? enraged : calm) * d.Tempo;

    // 한 대 피해 (UltHit 과 같은 계산 · 침식이면 × 0.85, 상한은 16% · 32% 와 25% 가운데 낮은 쪽 하나만)
    static float DevDamage(DevDomainCtx d, float raw)
    {
        float dmg = raw * Power * d.devPow;
        float share = DevHitShare * d.devPow;
        if (d.erosion) { dmg *= ErosionDevDamageMul; share = Mathf.Min(share, ErosionDevHitShare); }
        PlayerController pl = Hostile.Player;
        if (pl == null) return dmg;
        float scale = Mathf.Max(0.01f, GameMode.DamageMul * Chapters.HurtMul);     // TryHit 이 곱하는 배율
        return Mathf.Min(dmg, pl.PlayerMaxHealth * share / scale);
    }

    static bool DevHit(DevDomainCtx d, float damage)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null || pl.IsDying) return false;
        AttackLabel.Pending = d.label;
        bool hit = pl.TryHit(damage);
        AttackLabel.Pending = null;
        return hit;
    }

    static bool CanShoot(DevDomainCtx d) => HostileProjectile.Live.Count < Mathf.Min(d.bulletCap, 400);

    static void DevShot(DevDomainCtx d, Vector3 from, Vector2 dir, float speed, float raw, Color color, float life)
    {
        if (!CanShoot(d)) return;
        HostileProjectile h = Hostile.Shoot(from, dir, speed, DevDamage(d, raw), 0.5f, color, 1.5f, life);
        if (h != null) h.label = d.label;
    }

    // 결계 안 (침식은 그 반지름) 으로 당긴 자리
    static Vector3 Inside(DevDomainCtx d, Vector3 at, float margin)
    {
        Vector3 off = at - d.center;
        off.z = 0f;
        float max = d.radius - margin;
        if (off.magnitude > max) at = d.center + off.normalized * max;
        return at;
    }

    // 땅 타격: 침식이면 대기열로, 아니면 직접 (예고 원 둘 + 느낌표 → 이펙트 + 피해)
    static void DevStrike(DevDomainCtx d, Vector3 at, float r, float warn, Color color, float raw, string fx, bool aimed)
    {
        warn = Mathf.Max(warn, d.WarnFloor);
        at = Inside(d, at, 0.5f);
        float dmg = DevDamage(d, raw);
        if (d.strike != null) { d.strike(at, r, warn, color, dmg, fx, aimed); return; }
        Hostile.Circle(at, r, warn, Bright(color));
        Hostile.Circle(at, r * 0.55f, warn, new Color(1f, 1f, 1f, 0.75f));
        Warn(at, warn, r);
        if (d.runner != null) d.runner.StartCoroutine(DevLand(d, at, r, Time.time + warn, color, dmg, fx));
    }

    // 예고를 본 타격은 결계가 끝나도 끝까지 떨어짐 (다른 결계 Spike 와 같음)
    static IEnumerator DevLand(DevDomainCtx d, Vector3 at, float r, float fireAt, Color color, float dmg, string fx)
    {
        while (Time.time < fireAt) yield return null;
        Fx.Spawn(fx, at + Vector3.up * 0.4f, r * 2.2f, color, 18f, 0f, 14);
        Fx.Spawn("fx_explosion", at, r * 1.8f, color, 20f);
        if (fx == "fx_slash") { Fx.Spawn("fx_shock", at, r * 2.4f, color, 18f); Hostile.Play("boom", 0.7f, 1.4f); }     // 등짝 스매싱 「짝!」
        PlayerController p = Hostile.Player;
        if (p != null && Vector2.Distance(at, p.HurtCenter) <= r + PlayerController.HurtRadius) DevHit(d, dmg);      // 1.4.4~ 몸 아래쪽 판정
    }

    // 띠 타격 (한 줄씩 실행 · 빨래 · 강제 종료): 예고 선 → 빔 + 선분 거리 판정
    static void DevBandWarn(Vector3 a, Vector3 b, float width, float warn, Color color) => Hostile.Line(a, b, width, warn, Bright(color));

    static void DevBandFire(DevDomainCtx d, Vector3 a, Vector3 b, float width, float hitHalf, Color color, float raw)
    {
        Fx.Beam(a, b, width, color, 0.28f);
        PlayerController p = Hostile.Player;
        if (p != null && Hostile.HurtToSegment(p, a, b) < hitHalf) DevHit(d, DevDamage(d, raw));
    }

    // 보스 몸이 깜빡이며 기를 모음 (BossSkills.Windup 과 같은 모양)
    static IEnumerator DevWindup(DevDomainCtx d, Color color, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (!d.Live) yield break;
            if (d.body != null) d.body.color = Color.Lerp(d.src.baseTint, color, Mathf.PingPong(Time.time * 6f, 1f));
            yield return null;
        }
        if (d.body != null && d.src != null && !d.src.IsDead) d.body.color = Color.white;
    }

    static void TrackPlayer(DevDomainCtx d)
    {
        PlayerController p = Hostile.Player;
        if (p == null || Time.deltaTime <= 0f) return;
        Vector3 now = p.transform.position;
        Vector3 v = (now - d.lastPlayer) / Time.deltaTime;
        if (v.sqrMagnitude > 900f) v = Vector3.zero;     // 순간이동 · 밀려남은 빼고
        d.playerVel = Vector3.Lerp(d.playerVel, v, 1f - Mathf.Exp(-10f * Time.deltaTime));
        d.lastPlayer = now;
    }

    static TextMeshPro WorldText(Transform parent, string text, float size, Color color)
    {
        GameObject go = new GameObject("DomainText");
        if (parent != null) go.transform.SetParent(parent, false);
        TextMeshPro t = go.AddComponent<TextMeshPro>();
        if (UIKit.Font != null) t.font = UIKit.Font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.rectTransform.sizeDelta = new Vector2(30f, 4f);
        t.sortingLayerID = SortingLayer.NameToID("Background");
        t.sortingOrder = 44;
        return t;
    }

    // ================================================================= 개발자 「디버그 모드」
    static readonly string[] DevCode =
    {
        "player.hp--;", "while (true) { }", "if (hero) Nerf(hero);", "throw new NullReferenceException();",
        "// TODO: fix later", "Debug.Log(hero);", "Build #1.2.5 FAILED", "return 0xDEAD;",
    };

    public static IEnumerator DevBarrage(DevDomainCtx d)
    {
        float t0 = Time.time;
        float nextPoint = t0 + 0.6f, nextLines = t0 + 1.0f, nextBug = t0 + 0.5f, nextLoop = t0 + 2.5f;
        // 강제 종료: 8초에 한 번 · 분노면 4초에도 (침식에서는 쓰지 않고 그 자리에 무한 루프 한 번 더)
        float quitAt = t0 + 8f, quitEarly = t0 + 4f;
        bool quitDone = false, earlyDone = false;
        if (!d.erosion) DevDecor(d);
        while (d.Live)
        {
            float now = Time.time;
            TrackPlayer(d);
            DevDecorTick(d);
            if (now >= nextPoint) { nextPoint = now + Gap(d, 1.6f, 1.36f); Breakpoints(d); }
            if (now >= nextLines && now >= d.xBusyUntil)
            {
                nextLines = now + Gap(d, 4.2f, 3.6f);
                d.linesBusyUntil = now + 2.9f + 0.5f;           // 도는 동안과 끝 0.5초에는 강제 종료를 미룸
                d.runner.StartCoroutine(StepLines(d));
            }
            if (now >= nextBug)
            {
                nextBug = now + Gap(d, 2.4f, 2.0f);
                int others = d.sharedBouncers != null ? d.sharedBouncers() : 0;
                int live = DevBug.Count + d.bugPending;
                if (live < 3 && (!d.erosion || live + others < 4)) d.runner.StartCoroutine(SpawnBug(d));
            }
            if (now >= nextLoop) { nextLoop = now + Gap(d, 5.0f, 4.25f); d.runner.StartCoroutine(InfiniteLoop(d)); }
            bool early = !earlyDone && now >= quitEarly && d.src.Enraged;
            if (early || (!quitDone && now >= quitAt))
            {
                if (d.erosion) { if (early) earlyDone = true; else quitDone = true; d.runner.StartCoroutine(InfiniteLoop(d)); }
                else if (now >= d.linesBusyUntil)
                {
                    if (early) earlyDone = true; else quitDone = true;
                    d.xBusyUntil = now + 1.3f + 0.5f;
                    d.runner.StartCoroutine(ForceQuit(d));
                }
            }
            if (!earlyDone && now >= quitAt) earlyDone = true;     // 8초까지 분노하지 않았으면 이른 한 번은 없음
            yield return null;
        }
        if (d.body != null && d.src != null && !d.src.IsDead) d.body.color = Color.white;
    }

    // 디버그 격자 (선 하나로 지그재그) + 떠다니는 코드 8줄
    static void DevDecor(DevDomainCtx d)
    {
        if (d.root == null) return;
        const float cell = 6f;
        int n = Mathf.FloorToInt((d.radius - 1f) / cell);
        int lines = 2 * n + 1;
        Vector3[] pts = new Vector3[lines * 4];
        int k = 0;
        for (int pass = 0; pass < 2; pass++)
            for (int i = -n; i <= n; i++)
            {
                float off = i * cell;
                float half = Mathf.Sqrt(Mathf.Max(0f, (d.radius - 1f) * (d.radius - 1f) - off * off));
                bool flip = ((i + n) & 1) == 1;
                Vector3 a = pass == 0 ? new Vector3(off, -half) : new Vector3(-half, off);
                Vector3 b = pass == 0 ? new Vector3(off, half) : new Vector3(half, off);
                pts[k++] = d.center + (flip ? b : a);
                pts[k++] = d.center + (flip ? a : b);
            }
        LineRenderer grid = Hostile.NewLine("DebugGrid", new Color(DevTerminal.r, DevTerminal.g, DevTerminal.b, 0.10f), 0.08f, 41);
        grid.sortingLayerName = "Background";
        grid.transform.SetParent(d.root, true);
        grid.positionCount = k;
        grid.SetPositions(pts);

        d.code = new TextMeshPro[DevCode.Length];
        Color c = new Color(DevTerminal.r, DevTerminal.g, DevTerminal.b, 0.35f);
        for (int i = 0; i < d.code.Length; i++)
        {
            d.code[i] = WorldText(d.root, DevCode[i], 5f, c);
            d.code[i].transform.position = d.center + new Vector3(Random.Range(-0.6f, 0.6f) * d.radius, Mathf.Lerp(-0.75f, 0.75f, i / 7f) * d.radius);
        }
    }

    static void DevDecorTick(DevDomainCtx d)
    {
        if (d.code != null)
        {
            float top = d.center.y + d.radius * 0.8f, bottom = d.center.y - d.radius * 0.8f;
            for (int i = 0; i < d.code.Length; i++)
            {
                TextMeshPro t = d.code[i];
                if (t == null) continue;
                Vector3 p = t.transform.position;
                p.y += (0.7f + 0.1f * i) * Time.deltaTime;
                if (p.y > top) { p.y = bottom; p.x = d.center.x + Random.Range(-0.6f, 0.6f) * d.radius; }
                t.transform.position = p;
            }
        }
        // 노트북 화면처럼 0.5초마다 초록으로 깜빡이는 후광
        if (d.aura != null && d.aura.TryGetComponent(out SpriteRenderer sr))
        {
            Color c = Mathf.Repeat(Time.time, 1f) < 0.5f ? DevGold : DevTerminal;
            sr.color = new Color(c.r, c.g, c.b, 0.5f);
        }
    }

    // 1. 브레이크포인트: 지금 자리 · 0.6초 뒤 · 1.2초 뒤 예상 자리 (가만히 있으면 내 둘레 2.5 삼각형)
    static void Breakpoints(DevDomainCtx d)
    {
        PlayerController p = Hostile.Player;
        if (p == null) return;
        Vector3 me = p.transform.position;
        Vector3 v = d.playerVel;
        v.z = 0f;
        bool still = v.magnitude < 0.6f;
        float a0 = Random.Range(0f, 360f);
        for (int i = 0; i < 3; i++)
        {
            Vector3 at;
            if (still) { float a = (a0 + i * 120f) * Mathf.Deg2Rad; at = me + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 2.5f; }
            else at = me + v * (0.6f * i);
            at = Inside(d, at, 0.5f);      // 결계 원 안으로 (맵 경계로 자르면 맵 밖에 선 플레이어와 엉뚱한 자리에 떨어짐)
            Fx.Spawn("fx_target_rune", at, 3.6f, DevRed, 12f, 0f, 44, true, Mathf.Max(0.9f, d.WarnFloor));
            DevStrike(d, at, 1.7f, 0.9f, DevRed, 16f, "fx_spike", !still && i == 0);
        }
        Hostile.Play("zap", 0.5f, 1.6f);
    }

    // 2. 한 줄씩 실행: 내 둘레 가로 띠 7줄이 위에서부터 0.28초 차이로 · 무작위 한 줄은 「// 주석」(실행 안 됨)
    static IEnumerator StepLines(DevDomainCtx d)
    {
        PlayerController p = Hostile.Player;
        if (p == null) yield break;
        const int rows = 7;
        const float h = 3.2f, half = 22f, first = 0.9f, step = 0.28f;
        Vector3 o = p.transform.position;
        int comment = Random.Range(0, rows - 1);
        if (comment >= rows / 2) comment++;          // 지금 내가 있는 가운데 줄은 주석이 되지 않음
        float start = Time.time;
        for (int i = 0; i < rows; i++)
        {
            float y = o.y + (rows / 2 - i) * h;
            Color c = i == comment ? DevTerminal : DevGold;
            DevBandWarn(new Vector3(o.x - half, y), new Vector3(o.x + half, y), h, first + i * step, i == comment ? new Color(c.r, c.g, c.b, 0.55f) : c);
        }
        if (d.root != null)
        {
            if (d.comment == null) d.comment = WorldText(d.root, Loc.T("// 주석"), 9f, DevTerminal);
            d.comment.gameObject.SetActive(true);
            d.comment.transform.position = new Vector3(o.x - half + 5f, o.y + (rows / 2 - comment) * h, 0f);
        }
        for (int i = 0; i < rows; i++)
        {
            float fireAt = start + first + i * step;
            while (Time.time < fireAt) yield return null;
            if (!PlayerOk) break;
            if (i == comment) continue;
            float y = o.y + (rows / 2 - i) * h;
            DevBandFire(d, new Vector3(o.x - half, y), new Vector3(o.x + half, y), h, h * 0.5f + 0.2f, DevGold, 20f);
            Hostile.Play("clank", 0.45f, 1f + i * 0.06f);
        }
        if (d.comment != null) d.comment.gameObject.SetActive(false);
    }

    // 3. 버그 대량 발생: 결계 가장자리에서 느낌표 0.6초 → 벽에 튕기며 돌아다니는 버그 (최대 3)
    static IEnumerator SpawnBug(DevDomainCtx d)
    {
        d.bugPending++;
        float a = Random.Range(0f, Mathf.PI * 2f);
        Vector3 at = d.center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (d.radius - 2f);
        Warn(at, 0.6f, 1.6f);
        Hostile.Circle(at, 1f, 0.6f, Bright(DevTerminal));
        float t = Time.time + 0.6f;
        while (Time.time < t) yield return null;
        d.bugPending = Mathf.Max(0, d.bugPending - 1);
        if (!d.Live || Hostile.Glow == null) yield break;
        PlayerController p = Hostile.Player;
        Vector2 dir = p != null ? ((Vector2)(p.transform.position - at)).normalized : -(Vector2)(at - d.center).normalized;
        float spread = Random.Range(-20f, 20f) * Mathf.Deg2Rad, cs = Mathf.Cos(spread), sn = Mathf.Sin(spread);
        dir = new Vector2(dir.x * cs - dir.y * sn, dir.x * sn + dir.y * cs);
        GameObject g = SpecialAbilities.MakeSprite("DebugBug", Hostile.Glow, at, 0.22f, DevTerminal, "Effect", 20);
        HostileProjectile.Dress(g, DevTerminal, 1.4f * HostileProjectile.LookMul);
        DevBug b = g.AddComponent<DevBug>();
        b.ctx = d;
        b.velocity = dir * (d.src != null && d.src.Enraged ? 5.5f : 5f);
        b.damage = DevDamage(d, 14f);
        b.life = Mathf.Max(1f, d.until - Time.time);
        if (d.root != null) g.transform.SetParent(d.root, true);
        Hostile.Play("pop", 0.5f, 1.3f);
    }

    // 4. 무한 루프: 몸 깜빡임 0.5초 → 120° 간격 팔 셋이 도는 나선 (0.14초마다 · 12번 · 11°씩)
    static IEnumerator InfiniteLoop(DevDomainCtx d)
    {
        yield return DevWindup(d, DevTerminal, 0.5f);
        if (!d.Live) yield break;
        Hostile.Play("shimmer", 0.6f, 0.8f);
        float baseA = Random.Range(0f, 360f), turn = Random.value < 0.5f ? 11f : -11f;
        for (int w = 0; w < 12 && d.Live; w++)
        {
            Vector3 from = d.src.transform.position;
            for (int arm = 0; arm < 3; arm++)
            {
                float a = (baseA + arm * 120f + w * turn) * Mathf.Deg2Rad;
                DevShot(d, from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 7f, 12f, w % 2 == 0 ? DevTerminal : DevGold, 4f);
            }
            float t = Time.time + 0.14f;
            while (Time.time < t) yield return null;
        }
    }

    // 5. 강제 종료: 내 자리를 가운데로 거대한 X 띠 둘 (0 ~ 45° 회전) · 네 팔 사이로 피함
    static IEnumerator ForceQuit(DevDomainCtx d)
    {
        PlayerController p = Hostile.Player;
        if (p == null) yield break;
        const float half = 22f, width = 4f, warn = 1.3f;
        Vector3 o = p.transform.position;
        float rot = Random.Range(0f, 45f);
        Vector3 u = Quaternion.Euler(0f, 0f, rot + 45f) * Vector3.right, v = Quaternion.Euler(0f, 0f, rot - 45f) * Vector3.right;
        DevBandWarn(o - u * half, o + u * half, width, warn, DevGold);
        DevBandWarn(o - v * half, o + v * half, width, warn, DevGold);
        Warn(o, warn, 2.4f);
        Fx.Spawn("fx_shock", o, 7f, DevGold, 10f);
        if (d.root != null)
        {
            if (d.quitMark == null) d.quitMark = WorldText(d.root, "×\n<size=35%>" + Loc.T("강제 종료") + "</size>", 30f, DevGold);
            d.quitMark.gameObject.SetActive(true);
            d.quitMark.transform.position = o + Vector3.up * 0.5f;
        }
        float t = Time.time + warn;
        while (Time.time < t) yield return null;
        if (d.quitMark != null) d.quitMark.gameObject.SetActive(false);
        if (!PlayerOk) yield break;
        Fx.Beam(o - u * half, o + u * half, width, DevGold, 0.35f);
        Fx.Beam(o - v * half, o + v * half, width, DevGold, 0.35f);
        PlayerController pl = Hostile.Player;
        if (pl != null)
        {
            if (Hostile.HurtToSegment(pl, o - u * half, o + u * half) < width * 0.5f || Hostile.HurtToSegment(pl, o - v * half, o + v * half) < width * 0.5f)     // 1.4.4~ 몸 아래쪽 판정
                DevHit(d, DevDamage(d, 24f));
        }
        Hostile.Play("crack", 0.8f, 0.7f);
        Hostile.Shake(0.3f);
    }

    // ================================================================= 엄마 「엄마의 대청소」
    static readonly string[] MomNagKeys = { "방 좀 치워!", "밥 먹고 해!", "게임 좀 그만해!", "몇 시인데 아직도 안 자니!", "엄마가 몇 번을 말했니!" };
    static readonly string[] momNag = new string[5];        // 결계 시작 때 한 번 번역해 둠 (쓸 때는 꺼내기만)

    public static IEnumerator MomBarrage(DevDomainCtx d)
    {
        for (int i = 0; i < MomNagKeys.Length; i++) momNag[i] = Loc.T(MomNagKeys[i]);
        float t0 = Time.time;
        float nextSmash = t0 + 0.6f, nextLaundry = t0 + 1.5f, nextNag = t0 + 0.8f;
        // 청소기: 3초 · 10초 (분노면 6.5초 한 번 더) · 통금: 6.5초 (침식에서는 통금 대신 잔소리 한 번 더)
        float vac1 = t0 + 3f, vac2 = t0 + 10f, vacRage = t0 + 6.5f, curfewAt = t0 + 6.5f;
        bool v1 = false, v2 = false, vr = false, curfewDone = false;
        float vacBusyUntil = 0f;
        while (d.Live)
        {
            float now = Time.time;
            MomDecorTick(d);
            // 통금으로 고리가 줄어드는 동안(깜빡임 1초 + 줄어듦 2초)과 그 직전 0.8초에는 손바닥 · 빨랫줄을 새로 시작하지 않음
            // (끌려 들어가며 이미 예고된 자리 위에 놓여 피할 수 없이 맞지 않게 · 미룬 공격은 줄어듦이 끝나면 바로)
            bool pulling = !d.erosion && now >= curfewAt - 0.8f && now < curfewAt + 3f;
            if (now >= nextSmash && !pulling) { nextSmash = now + Gap(d, 2.0f, 1.7f); d.runner.StartCoroutine(BackSmash(d)); }
            if (now >= nextLaundry && !pulling) { nextLaundry = now + Gap(d, 4.6f, 3.9f); d.runner.StartCoroutine(Laundry(d)); }
            if (now >= nextNag) { nextNag = now + Gap(d, 2.6f, 2.2f); d.runner.StartCoroutine(Nag(d)); }
            if (!curfewDone && now >= curfewAt)
            {
                curfewDone = true;
                if (d.erosion) d.runner.StartCoroutine(Nag(d));
                else { d.curfewUntil = now + 1f + 2f + (d.src.Enraged ? 4f : 3f) + 1f; d.runner.StartCoroutine(Curfew(d)); }
            }
            // 청소기는 통금으로 줄어든 동안 쓰지 않음 (끝난 뒤 남은 시간이 넉넉하면 그때)
            bool shrunk = now < d.curfewUntil || (!d.erosion && !curfewDone && now + 3.6f > curfewAt + 1f);
            bool room = d.until - now >= 2.6f;
            if (!v1 && now >= vac1 && now >= vacBusyUntil && !shrunk) { v1 = true; if (room) { vacBusyUntil = now + 3.6f; d.runner.StartCoroutine(Vacuum(d)); } }
            if (!vr && now >= vacRage && d.src.Enraged && now >= vacBusyUntil && !shrunk) { vr = true; if (room) { vacBusyUntil = now + 3.6f; d.runner.StartCoroutine(Vacuum(d)); } }
            if (!v2 && now >= vac2 && now >= vacBusyUntil && !shrunk) { v2 = true; if (room) { vacBusyUntil = now + 3.6f; d.runner.StartCoroutine(Vacuum(d)); } }
            yield return null;
        }
        if (d.body != null && d.src != null && !d.src.IsDead) d.body.color = Color.white;
    }

    static void MomDecorTick(DevDomainCtx d)
    {
        // 결계 안을 떠도는 먼지 티끌 (풀 이펙트)
        // 0.11초마다 하나 (예전 매 프레임 15% 는 화면 주사율에 따라 수가 달라졌음 · 60fps 기준 초당 약 9개 그대로)
        if (Time.time >= d.dustAt)
        {
            d.dustAt = Time.time + 0.11f;
            Fx.Spawn("fx_spark", d.center + (Vector3)(Random.insideUnitCircle * (d.radius - 1f)), 0.9f, new Color(1f, 0.9f, 0.95f, 0.6f), 10f);
        }
        // 잔소리 글자는 1.6초 동안 보였다가 흐려짐
        if (d.nag != null)
        {
            // 1.2.6~ 일시정지 · 설정 · 선택 창 위에 남지 않게 (시간이 멈추면 흐려짐도 멈춰 창 위에 그대로 떠 있었음)
            bool paused = ESCmenu.IsOpen || SettingsUI.IsOpen || Time.timeScale == 0f;
            Canvas nc = d.nag.transform.parent != null ? d.nag.transform.parent.GetComponent<Canvas>() : null;      // Graphic.canvas 는 꺼진 캔버스를 못 찾아서 부모에서 직접
            if (nc != null && nc.enabled == paused) nc.enabled = !paused;
            float k = 1f - Mathf.Clamp01((Time.time - d.nagShownAt - 1.2f) / 0.4f);
            if (Mathf.Abs(d.nag.alpha - k) > 0.01f) d.nag.alpha = k;
        }
    }

    // 1. 등짝 스매싱: 내 자리에 커다란 손바닥 → 0.55초 뒤 그때 내 자리에 한 번 더 (작게)
    static IEnumerator BackSmash(DevDomainCtx d)
    {
        PlayerController p = Hostile.Player;
        if (p == null) yield break;
        DevStrike(d, p.transform.position, 4.2f, 0.85f, EndlessMode.MomTint, 22f, "fx_slash", true);
        float t = Time.time + 0.55f;
        while (Time.time < t) yield return null;
        p = Hostile.Player;
        if (!d.Live || p == null) yield break;
        DevStrike(d, p.transform.position, 3f, 0.75f, EndlessMode.MomTint, 18f, "fx_slash", true);
    }

    // 2. 청소기: 가장자리(나와 15 이상)에 입 → 1초 예고 → 2.6초 동안 입 쪽으로 끌어당김 (기본 이동 속도의 40%, 걸어서 빠져나갈 수 있음)
    static IEnumerator Vacuum(DevDomainCtx d)
    {
        PlayerController p = Hostile.Player;
        if (p == null) yield break;
        Vector3 me = p.transform.position;
        Vector3 mouth = d.center;
        float best = -1f, a0 = Random.Range(0f, 360f);
        for (int i = 0; i < 16; i++)
        {
            float a = (a0 + i * 22.5f) * Mathf.Deg2Rad;
            Vector3 at = Hostile.ClampArena(d.center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (d.radius - 2.5f));
            float dist = Vector2.Distance(at, me);
            if (dist >= 15f) { mouth = at; best = dist; break; }
            if (dist > best) { best = dist; mouth = at; }
        }
        const float mouthR = 2.2f, warn = 1f, pull = 2.6f;
        Color pink = EndlessMode.MomTint;
        Hostile.Line(mouth, me, 1.2f, warn, Bright(pink));
        Hostile.Circle(mouth, mouthR, warn + pull, Bright(pink));
        Warn(mouth, warn, mouthR);
        FxAnim vortex = Fx.Play("fx_vortex", mouth, mouthR * 2.4f, new Color(pink.r, pink.g, pink.b, 0.85f), 16f, 0f, 20, true, warn + pull);
        if (vortex != null) { vortex.spin = -220f; if (d.root != null) vortex.transform.SetParent(d.root, true); }
        float t = Time.time + warn;
        while (Time.time < t) { if (!d.Live) yield break; yield return null; }
        float end = Time.time + pull, nextDust = Time.time, nextBite = Time.time;
        while (Time.time < end && d.Live)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) break;
            Vector3 pos = pl.transform.position;
            Vector3 to = mouth - pos;
            to.z = 0f;
            if (to.magnitude > 0.3f)
            {
                // 결계 중에는 벽이 꺼져 결계 원이 맵 밖까지 닿으므로 맵 경계(ClampArena)로 자르지 않음 (아래에서 결계 원 안만 허용)
                Vector3 next = pos + to.normalized * pl.BaseMoveSpeed * 0.4f * Time.deltaTime;
                next.z = pos.z;
                if (!Hostile.IsWall(next) && (next - d.center).magnitude < d.radius - 0.8f) pl.transform.position = next;
            }
            if (Time.time >= nextDust)
            {
                nextDust = Time.time + 0.4f;
                Hostile.Play("whoosh", 0.35f, 0.55f);
                for (int i = 0; i < 3; i++)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 from = d.center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (d.radius - 1f);
                    Vector2 dir = mouth - from;
                    float life = Mathf.Max(0.5f, dir.magnitude / 6f);
                    DevShot(d, from, dir.normalized, 6f, 12f, MomWhite, life);
                }
            }
            if (Time.time >= nextBite)
            {
                nextBite = Time.time + 0.8f;
                if (Hostile.HurtDistance(pl, mouth) < mouthR + 0.2f) DevHit(d, DevDamage(d, 20f));
            }
            yield return null;
        }
        if (vortex != null) Object.Destroy(vortex.gameObject);
    }

    // 3. 빨래 널기: 나를 가로지르는 빨랫줄 4줄 (간격 5) → 0.6초 뒤 반 칸 엇갈린 4줄
    static IEnumerator Laundry(DevDomainCtx d)
    {
        PlayerController p = Hostile.Player;
        if (p == null) yield break;
        const float half = 22f, width = 2.4f, gap = 5f, warn = 1.1f;
        Vector3 o = p.transform.position;
        float a = Random.Range(0f, Mathf.PI);
        Vector3 along = new Vector3(Mathf.Cos(a), Mathf.Sin(a)), side = new Vector3(-along.y, along.x);
        float jitter = Random.Range(-1.25f, 1.25f);
        float start = Time.time;
        for (int wave = 0; wave < 2; wave++)
        {
            if (wave == 1)
            {
                float t = start + 0.6f;
                while (Time.time < t) yield return null;
                if (!d.Live) yield break;
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 c = o + side * ((i - 1.5f) * gap + jitter + wave * gap * 0.5f);
                DevBandWarn(c - along * half, c + along * half, width, warn, wave == 0 ? EndlessMode.MomTint : MomWhite);
            }
            Hostile.Play("whoosh", 0.5f, 1.1f);
            d.runner.StartCoroutine(LaundryFall(d, o, along, side, jitter + wave * gap * 0.5f, Time.time + warn, wave == 0 ? EndlessMode.MomTint : MomWhite));
        }
    }

    static IEnumerator LaundryFall(DevDomainCtx d, Vector3 o, Vector3 along, Vector3 side, float shift, float fireAt, Color color)
    {
        const float half = 22f, width = 2.4f, gap = 5f;
        while (Time.time < fireAt) yield return null;
        if (!PlayerOk) yield break;
        PlayerController p = Hostile.Player;
        bool hit = false;
        for (int i = 0; i < 4; i++)
        {
            Vector3 c = o + side * ((i - 1.5f) * gap + shift);
            Vector3 a = c - along * half, b = c + along * half;
            Fx.Beam(a, b, width, color, 0.3f);
            if (!hit && p != null && Hostile.HurtToSegment(p, a, b) < width * 0.5f + 0.2f) hit = true;
        }
        if (hit) DevHit(d, DevDamage(d, 18f));
    }

    // 4. 잔소리 폭격: 화면 아래쪽에 잔소리 한 줄 + 엄마 머리 위 「!」 → 몸 깜빡임 0.4초 → 나를 향한 부채꼴 7발 × 3번 (50°)
    static IEnumerator Nag(DevDomainCtx d)
    {
        if (d.root != null && d.nag == null)
        {
            UIKit.EnsureStyle();
            Canvas canvas = NewCanvas("MomNagCanvas", 30400);
            canvas.transform.SetParent(d.root, false);
            d.nag = (TextMeshProUGUI)UIKit.Text(canvas.transform, "", 58f, EndlessMode.MomTint, new Vector2(0f, -260f), new Vector2(1600f, 90f));      // 1.2.6: 화면 아래쪽 (y 400 은 보스 체력바를 덮었음)
            d.nag.fontStyle = FontStyles.Bold;
            d.nag.outlineWidth = 0.2f;
            d.nag.outlineColor = new Color32(0, 0, 0, 255);
        }
        if (d.nag != null)
        {
            d.nag.text = momNag[d.nagTurn++ % momNag.Length];
            d.nag.alpha = 1f;
            d.nagShownAt = Time.time;
        }
        if (d.src != null) Warn(d.src.transform.position + Vector3.up * 2.4f, 0.4f, 1.4f);
        Hostile.Play("pop", 0.55f, 1.1f);
        yield return DevWindup(d, EndlessMode.MomTint, 0.4f);
        for (int v = 0; v < 3 && d.Live; v++)
        {
            PlayerController p = Hostile.Player;
            if (p == null) yield break;
            Vector3 from = d.src.transform.position;
            Vector2 aim = ((Vector2)(p.transform.position - from)).normalized;
            float baseA = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            for (int i = 0; i < 7; i++)
            {
                float a = (baseA - 25f + i * 50f / 6f) * Mathf.Deg2Rad;
                DevShot(d, from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 8f, 12f, i % 2 == 0 ? EndlessMode.MomTint : MomWhite, 4f);
            }
            float t = Time.time + 0.22f;
            while (Time.time < t) yield return null;
        }
    }

    // 5. 통금 시간: 고리가 1초 빨갛게 깜빡임 → 2초 동안 35 → 17 (분노 15) → 3초 (분노 4초) 유지 → 1초 동안 원래대로 (피해 없음)
    static IEnumerator Curfew(DevDomainCtx d)
    {
        Notice.Big(Loc.T("통금 시간이다! 들어와!"), NoticeIcon.Boss, 2.2f, NoticeLevel.Urgent);
        Hostile.Play("roar", 0.35f, 1.2f);
        bool rage = d.src != null && d.src.Enraged;
        float small = rage ? 15f : 17f, hold = rage ? 4f : 3f, full = d.baseRadius;
        Color red = new Color(1f, 0.2f, 0.2f);
        float s0 = Time.time;
        float[] marks = { 1f, 2f, hold, 1f };
        for (int phase = 0; phase < 4; phase++)
        {
            float len = marks[phase], ps = Time.time;
            while (Time.time - ps < len)
            {
                if (!d.Live) { d.radius = full; d.ringTint = d.tint; d.curfewUntil = 0f; yield break; }
                float k = (Time.time - ps) / len;
                if (phase == 0) d.ringTint = Color.Lerp(d.tint, red, Mathf.PingPong((Time.time - s0) * 6f, 1f));
                else if (phase == 1) { d.radius = Mathf.Lerp(full, small, EaseOut(k)); d.ringTint = Color.Lerp(d.tint, red, 0.6f); }
                else if (phase == 2) { d.radius = small; d.ringTint = Color.Lerp(d.tint, red, 0.45f + 0.15f * Mathf.Sin(Time.time * 5f)); }
                else { d.radius = Mathf.Lerp(small, full, EaseOut(k)); d.ringTint = Color.Lerp(Color.Lerp(d.tint, red, 0.5f), d.tint, k); }
                yield return null;
            }
        }
        d.radius = full;
        d.ringTint = d.tint;
        d.curfewUntil = 0f;
    }
}

// 개발자 결계의 「버그」: 결계 벽에 튕기며 돌아다님 (초록 · 금색 깜빡임, 닿으면 피해) — 반지름은 결계 맥락에서 매 프레임
public class DevBug : MonoBehaviour
{
    public static int Count { get; private set; }
    public DevDomainCtx ctx;
    public Vector2 velocity;
    public float damage, life;
    float age, trail;
    SpriteRenderer body;

    void OnEnable() { Count++; body = GetComponent<SpriteRenderer>(); }
    void OnDisable() => Count = Mathf.Max(0, Count - 1);

    void Update()
    {
        age += Time.deltaTime;
        // 결계가 끝났거나, 침식에서 개발자가 먼저 쓰러져 그 공격이 멈추면 (ctx.Live) 함께 사라짐 — 튕기는 것 상한 칸도 비움
        if (age >= life || !BossUltimate.Active || ctx == null || !ctx.Live) { Destroy(gameObject); return; }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        Vector3 d = transform.position - ctx.center;
        d.z = 0f;
        float r = ctx.radius - 0.6f;
        if (d.magnitude > r)
        {
            Vector2 n = d.normalized;
            velocity = Vector2.Reflect(velocity, -n);
            transform.position = ctx.center + (Vector3)(n * r);
        }
        transform.localScale = Vector3.one * ((0.42f + 0.04f * Mathf.Sin(age * 14f)) * HostileProjectile.LookMul);
        if (body != null) body.color = Mathf.Repeat(age, 0.3f) < 0.15f ? new Color(0.2f, 1f, 0.45f, 0.95f) : new Color(1f, 0.85f, 0.35f, 0.95f);
        trail += Time.deltaTime;
        if (trail > 0.12f) { trail = 0f; Fx.Spawn("fx_spark", transform.position, 1.1f, new Color(0.2f, 1f, 0.45f, 0.7f), 16f); }
        PlayerController p = Hostile.Player;
        if (p != null && Hostile.HurtDistance(p, transform.position) < 1f)      // 1.4.4~ 몸 아래쪽 판정
        {
            AttackLabel.Pending = ctx.label;
            p.TryHit(damage);
            AttackLabel.Pending = null;
        }
    }
}
