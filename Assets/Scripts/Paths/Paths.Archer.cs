using System.Collections.Generic;
using UnityEngine;

// 궁수 갈래 5개 (기획서 3-4) — 데이터 · 판 상태 · 덫 · 바람 · 천공 효과
// 궁수 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Archer.cs · 궁수 기존 파일(SignatureSkills.Archer.cs · CharacterKit.Archer.cs ·
//   CharacterKit.cs 안의 궁수 전용 부분만) — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
// 활 쪽(당기기 · 연사 · 화살 · 화살비)은 CharacterKit.Archer.cs, 바람길 · 울림 · 후퇴는 SignatureSkills.Archer.cs 가 이 클래스를 부름
// 안전: 상태는 모두 「끝나는 시각」 값 · 무적은 Paths.Invuln 만 · 덫 최대 10 (Paths.Trim) · static 상태 없음 (static 은 상수 · 묻기 함수뿐)
public sealed class ArcherPaths : HeroPaths
{
    public const int Long = 0, Rapid = 1, Trap = 2, Wind = 3, Sky = 4;
    public const int XMeteor = 2, XGust = 3;          // 새 교차 번호 (Crosses 배열 순서)

    // ================================================================= 수치 (카드 글과 같게 · 바꾸면 아래 문장 · LocTable.Paths.Archer 도)
    // 장궁 — 보스형: 완벽 화살 ×1.35 · 당기기 +40% 로 보스 화력 ±0~6% (완벽 비율 85~100%), 덜 당긴 화살 −40%
    public const float LongDrawMul = 1.4f, LongPerfectMul = 1.35f, LongWeakMul = 0.6f;
    public const float LongHaste = 0.08f;             // 공명 I: 꿰뚫은 적(보스 제외) 하나마다 다음 당김 8% 빠르게 (최대 5)
    public const int LongHasteMax = 5;
    public const float LongBeamWidth = 0.8f, LongBeamLength = 30f;      // 공명 II: 관통 광선 (피해는 그 화살 그대로 · 보스 화력 그대로) · 1.4.3~ 폭 = 보이는 광선 반폭 (그림 두께 1.6 = 폭 × 2 · 판정은 여기에 적 몸 크기를 더함) — 예전엔 발밑 기준점에서 1.1 이라 몸이 걸려도 안 맞거나 밖인데 맞았음
    // 속사 — 무리형: 당기지 않고 연사 (CharacterKit.Archer 의 RapidStep · RapidPower), 다섯 발마다 「가득 당긴 화살」
    public const int RapidFullEvery = 5, RapidEchoRes = 3, RapidRingHits = 10;
    public const float RapidRingMul = 2f;
    // 덫 — 완벽 화살이 떨어진 자리에 덫 (한 번 터짐)
    public const int TrapCap = 5, TrapMasterCap = 10, LineCap = 8;
    public const float TrapLife = 20f, TrapRadius = 1.3f, TrapBurstRadius = 2.2f, TrapPower = 1.2f, TrapRoot = 1.5f;
    public const float TrapBossRoot = 0.5f, TrapSnareMul = 1.1f, TrapSnareTime = 2f;           // 극의: 중간 보스 0.5초 묶음 · 보스는 2초 받는 피해 +10%
    public const float LineLife = 3f, LineWidth = 0.7f, LinePower = 0.35f, LineRange = 9f;     // 공명 II: 가시줄 (0.3초마다 공격력 35%)
    // 바람 — 생존 · 기동
    public const float DrawSlow = 0.6f;               // CharacterKit.MoveMul 의 당기는 동안 느려짐 (되돌림)
    public const float WindSpeed = 0.2f, WindSpeedTime = 1.5f;
    public const float WindDodge = 0.8f, WindDodgeCd = 2.5f;                // 공명 II: 후퇴 사격 뒤 0.8초 무적 (2.5초에 한 번 · Paths.Invuln)
    public const float WindAutoStep = 0.5f, WindAutoPower = 0.25f;          // 극의: 바람길 위 0.5초마다 공격력 25% 화살
    // 천공 — 화살비
    public const float SkyGaugeBonus = 0.25f, SkyRainRadius = 1.4f, SkyCost = 0.75f;
    public const float SkyHitGauge = 0.004f;          // 평타 명중: 평타 공격력 1배 피해마다 게이지 0.4% (완벽 화살 한 대에 약 1%)
    public const float SkyLinger = 3f, SkyEchoRain = 0.4f;
    public const int SkyDropTargets = 3;
    // 교차
    public const float MeteorGauge = 0.05f, GustTrapCd = 1.2f;

    public override CharacterId Who => CharacterId.Archer;
    public override string IconKey => "archer";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "a.long", name = "장궁", tagline = "길게 당긴 관통 화살",
            seed = "장궁", seedDesc = "당기는 시간이 40% 늘고, 「완벽!」 화살이 피해 ×1.35로 모든 적을 꿰뚫습니다.",
            cost = "덜 당긴 화살의 피해가 40% 줄어듭니다.", strong = "보스 · 일렬", weak = "근접 압박",
            cards = new[] { P, S(5), S(1), A }, evo = "ae.bow",
            res1 = "완벽 화살이 꿰뚫은 적 하나마다 다음 당김이 8% 빨라집니다. (최대 5)",
            res2 = "집중 호흡 5중첩이면 완벽 화살이 굵은 관통 광선이 됩니다.",
            mastery = "완벽 판정 폭이 2배가 됩니다.",
            color = new Color(0.45f, 0.8f, 1f), form = "투창" },
        new PathDef {
            key = "a.rapid", name = "속사", tagline = "누르는 동안 연사 · 기동 무리",
            seed = "단궁 속사", seedDesc = "당기지 않고 누르는 동안 초당 4발을 쏩니다. (한 발 피해 35%) 당길 때 느려지지 않고 숨도 닳지 않습니다.",
            cost = "관통이 없어지고, 가득 당긴 화살의 효과와 메아리 화살은 다섯 발마다 한 번 나갑니다.", strong = "기동 · 무리", weak = "보스 한 발",
            cards = new[] { M, S(3), S(4) }, evo = "ae.echo",
            res1 = "메아리 화살이 세 발마다 나갑니다.",
            res2 = "같은 적에 열 발을 맞히면 울림 화살촉의 범위가 2배가 됩니다.",
            mastery = "초당 6발을 쏩니다. (한 발 피해 25%)",
            color = new Color(1f, 0.85f, 0.4f), form = "속사 활" },
        new PathDef {
            key = "a.trap", name = "덫", tagline = "완벽 화살이 덫 · 지역 장악",
            seed = "덫 놓기", seedDesc = "완벽 화살이 떨어진 자리에 덫을 놓습니다. (최대 5개 · 20초 · 묶고 공격력 120%)",
            cost = "화살의 직접 피해가 20% 줄어듭니다.", strong = "지역 장악 · 카이팅 생존", weak = "움직이는 보스",
            cards = new[] { G, S(2), S(7) }, evo = "ae.forest",
            res1 = "덫이 터지면 그 자리에 가시 덤불이 자랍니다.",
            res2 = "적을 묶은 덫 둘이 3초 안에 가까이 터지면 그 사이에 가시줄이 이어집니다.",
            mastery = "덫이 10개까지 놓이고, 중간 보스도 0.5초 묶습니다. 보스는 2초 동안 받는 피해가 10% 늘어납니다.",
            color = new Color(0.55f, 0.9f, 0.4f), form = "그물 활" },
        new PathDef {
            key = "a.wind", name = "바람", tagline = "당기며 달리기 · 생존",
            seed = "바람 걸음", seedDesc = "당기는 동안 느려지지 않고, 완벽 화살마다 1.5초 동안 이동 속도가 20% 오르며 짧은 바람길이 남습니다.",
            cost = "숨이 2배 빨리 닳습니다.", strong = "기동 · 생존", weak = "화력 중간",
            cards = new[] { K, S(0), S(6) }, evo = "ae.storm",
            res1 = "바람길 위에서는 「완벽!」 판정 폭이 2배가 됩니다.",     // 1.4.2~ 「언제 놓아도 완벽」 → 판정 폭 2배 (손 기술 확인을 남김)
            res2 = "후퇴 사격으로 물러난 뒤 0.8초 동안 공격이 몸을 통과합니다. (2.5초에 한 번)",
            mastery = "바람길 위에 있으면 0.5초마다 가까운 적에게 화살을 저절로 쏩니다.",
            color = new Color(0.7f, 1f, 0.9f) },
        new PathDef {
            key = "a.sky", name = "천공", tagline = "화살비 중심 · 지역 섬멸",
            seed = "하늘 사수", seedDesc = "화살비 게이지가 25% 빨리 차고 범위가 1.4배가 되며, 평타가 맞힐 때마다 게이지가 조금 더 찹니다.",
            cost = "평타 피해가 25% 줄어듭니다.", strong = "지역 섬멸", weak = "게이지 공백",
            cards = new[] { B, S(8), S(9) }, evo = "ae.sky",
            res1 = "화살비를 쏜 자리의 적에게 3초 동안 사냥감 표식이 저절로 붙습니다.",
            res2 = "화살비를 쏜 뒤 3초 동안 평타가 하늘로 쏴져 화살비 자리의 적 셋에게 떨어집니다.",
            mastery = "화살비를 쏠 때 다른 무리 위에 화살비가 하나 더 쏟아집니다. (피해 40%)",
            color = new Color(0.75f, 0.6f, 1f) },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Long, Trap, "ae.master"),                     // 명궁의 호흡 (A · S1 · S7)
        Cross(Sky, Rapid, "ae.brand"),                      // 사냥꾼의 낙인 (B · S4)
        Cross(Long, Sky, "a.x.meteorpierce", "유성 관통", "완벽 화살이 화살비 게이지를 5% 채웁니다."),
        Cross(Wind, Trap, "a.x.gusttrap", "돌풍 덫", "후퇴 사격으로 물러나거나 뒤로 달리며 완벽 화살을 쏘면 발밑에 덫을 놓습니다."),
    };

    // ================================================================= 이번 판 상태 (판마다 새 인스턴스)
    // 지금 판의 궁수 갈래 (갈래 규칙이 아니면 null) — 활 · 고유 스킬 코드가 물음
    public static ArcherPaths Me => Paths.Active ? Paths.Hero as ArcherPaths : null;

    int longBank;                       // 장궁 공명 I: 지난 완벽 화살이 꿰뚫은 적 수 (다음 당김에 씀 · 놓을 때 비움)
    public int LongBank => longBank;
    float windUntil;                    // 바람: 완벽 화살 뒤 이동 속도 + 끝나는 시각
    float dodgeReadyAt;                 // 바람 공명 II: 다시 무적이 될 수 있는 시각
    float gustTrapAt;                   // 돌풍 덫: 다음 덫을 놓을 수 있는 시각
    float autoShotAt;                   // 바람 극의: 다음 자동 화살 시각
    Vector3 lastPos;
    Vector2 vel;                        // 영웅의 지금 움직임 (돌풍 덫: 뒤로 달리는지)
    bool hasPos;
    Vector3 skyAt;                      // 천공: 마지막 화살비 자리
    float skyR, skyUntil;
    float pruneAt;

    readonly List<GameObject> traps = new List<GameObject>();       // 놓인 덫 (SigZone) — 최대 5 · 극의 10
    readonly List<GameObject> lines = new List<GameObject>();       // 가시줄 — 최대 8
    readonly List<(Vector3 p, float until)> bursts = new List<(Vector3, float)>();     // 적을 묶은 덫이 터진 자리 (3초)
    readonly Dictionary<GameObject, int> rapidHits = new Dictionary<GameObject, int>();        // 속사 공명 II: 같은 적에 맞힌 수
    readonly Dictionary<GameObject, float> snares = new Dictionary<GameObject, float>();       // 덫 극의: 보스 받는 피해 + 끝나는 시각
    readonly List<GameObject> scratch = new List<GameObject>();

    // 덫을 놓을 화살 (맞히면 그 적 자리, 0.4초 안에 못 맞히면 겨눈 자리)
    sealed class PendingTrap { public Vector3 at; public float due; public bool done; }
    readonly List<PendingTrap> pending = new List<PendingTrap>();

    string badgeText;
    int badgeKey = -1;

    int TrapMax => Paths.Mastered(Trap) ? TrapMasterCap : TrapCap;

    // ================================================================= 판 흐름 (상태는 Paths.Has · Res · Mastered · CrossOn 을 그때그때 물으므로 비움)
    public override void OnSeed(int path, bool replay) { }
    public override void OnSingle(int path, bool replay) { }
    public override void OnResonance(int path, int tier, bool replay) { }
    public override void OnComplete(int path, bool replay) { }
    public override void OnMastery(int path, bool replay) { }
    public override void OnCross(int cross, bool replay) { }

    // 판이 끝남: 남은 덫 · 가시줄을 치움
    public override void End()
    {
        foreach (GameObject g in traps) if (g != null) Object.Destroy(g);
        foreach (GameObject g in lines) if (g != null) Object.Destroy(g);
        traps.Clear();
        lines.Clear();
        pending.Clear();
        bursts.Clear();
        rapidHits.Clear();
        snares.Clear();
    }

    // ================================================================= 전투 훅
    // 덫 극의: 덫에 걸린 보스는 2초 동안 받는 피해 +10%
    public override float Outgoing(GameObject target, float dmg)
    {
        if (snares.Count == 0 || target == null) return 1f;
        return snares.TryGetValue(target, out float until) && Time.time < until ? TrapSnareMul : 1f;
    }

    public override void Hit(GameObject target, float dmg, bool killed, bool proc) { }
    public override float Taken(float dmg) => dmg;
    public override void Killed(Vector3 pos) { }

    // 바람: 당기는 동안 느려지지 않음 · 완벽 화살 뒤 1.5초 +20%
    public override float MoveMul()
    {
        if (!Paths.Has(Wind)) return 1f;
        float m = 1f;
        CharacterKit k = CharacterKit.Instance;
        if (k != null && k.BowDrawing) m /= DrawSlow;
        if (Time.time < windUntil) m *= 1f + WindSpeed;
        return m;
    }

    public override void Tick()
    {
        if (!Paths.Active) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        float now = Time.time;

        // 영웅의 움직임 (돌풍 덫: 겨눈 반대쪽으로 달리는지)
        Vector3 pos = p.transform.position;
        if (hasPos && Time.deltaTime > 0f) vel = Vector2.Lerp(vel, (pos - lastPos) / Time.deltaTime, 0.25f);
        lastPos = pos;
        hasPos = true;

        // 덫: 완벽 화살이 아무도 못 맞히면 겨눈 자리에
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            PendingTrap pt = pending[i];
            if (pt.done) { pending.RemoveAt(i); continue; }
            if (now < pt.due) continue;
            pt.done = true;
            pending.RemoveAt(i);
            PlaceTrap(pt.at);
        }

        // 천공: 화살비 게이지가 25% 빨리 참 (SkillGauge 의 시간 충전과 같은 식 × 0.25)
        if (Paths.Has(Sky)) SkyCharge(p);

        // 바람 극의: 바람길 위에서 저절로 연사
        if (Paths.Mastered(Wind) && now >= autoShotAt && SignatureSkills.OnWind) AutoShot(p);

        if (now >= pruneAt) Prune(now);
    }

    void Prune(float now)
    {
        pruneAt = now + 2f;
        bursts.RemoveAll(b => b.until < now);
        scratch.Clear();
        foreach (var kv in rapidHits) if (kv.Key == null) scratch.Add(kv.Key);
        foreach (GameObject g in scratch) rapidHits.Remove(g);
        scratch.Clear();
        foreach (var kv in snares) if (kv.Key == null || kv.Value < now) scratch.Add(kv.Key);
        foreach (GameObject g in scratch) snares.Remove(g);
        scratch.Clear();
        traps.RemoveAll(g => g == null);
        lines.RemoveAll(g => g == null);
    }

    // HUD 입문 칸 오른쪽 위: 덫 갈래는 놓인 덫 수 (값이 바뀔 때만 글을 만듦)
    public override string Badge(int path)
    {
        if (path != Trap) return null;
        int n = 0;
        for (int i = 0; i < traps.Count; i++) if (traps[i] != null) n++;
        if (n == 0) return null;
        int max = TrapMax;
        int key = n * 100 + max;
        if (key != badgeKey) { badgeKey = key; badgeText = n + "/" + max; }
        return badgeText;
    }

    // ================================================================= 활이 부름 (CharacterKit.Archer)
    // 시위를 놓음 (속사는 한 발마다 · perfect: 완벽 화살 또는 속사의 다섯 번째 화살)
    public void Released(Vector3 start, Vector2 dir, bool perfect)
    {
        longBank = 0;                   // 장궁 공명 I: 이번 당김에 썼으니 비움 (방금 쏜 화살이 꿰뚫으면 다시 참)
        if (!perfect) return;
        if (Paths.Has(Wind))
        {
            windUntil = Time.time + WindSpeedTime;
            SignatureSkills.PathWindTrail(start, dir);
        }
        // 유성 관통: 완벽 화살이 화살비 게이지 5%
        if (Paths.CrossOn(XMeteor)) AddGauge(MeteorGauge);
        // 돌풍 덫: 겨눈 반대쪽으로 달리며 쏘면 발밑에 덫
        if (Paths.CrossOn(XGust) && vel.sqrMagnitude > 2.25f && Vector2.Dot(vel.normalized, dir) < -0.3f)
        {
            PlayerController p = Hostile.Player;
            if (p != null) GustTrap(p.transform.position);
        }
    }

    // 장궁 공명 I: 완벽 화살이 꿰뚫은 적 (보스 · 중간 보스는 세지 않음 — 보스 화력은 그대로)
    public void LongPierced(Collider2D c)
    {
        if (c == null || longBank >= LongHasteMax || Paths.IsBoss(c.gameObject)) return;
        longBank++;
    }

    // 덫 놓기: 완벽 화살이 맞힌 적 자리 (못 맞히면 0.4초 뒤 fallback)
    public void ArmTrap(Bullet b, Vector3 fallback)
    {
        if (b == null || !Paths.Has(Trap)) return;
        PendingTrap pt = new PendingTrap { at = fallback, due = Time.time + 0.4f };
        if (pending.Count >= 8) pending.RemoveAt(0);
        pending.Add(pt);
        b.onHitEnemy += (arrow, c) =>
        {
            if (pt.done || c == null) return;
            pt.done = true;
            PlaceTrap(c.transform.position);
        };
    }

    // 1.4.1~ 덫을 바로 놓음 (장궁 공명 II 관통 광선 · 하늘 화살처럼 화살을 거둘 때 — 맞힌 적 자리를 기다릴 화살이 없음)
    public void TrapAt(Vector3 at) => PlaceTrap(at);

    // 천공 공명 II: 화살비 뒤 3초 동안 평타가 하늘로 → 화살비 자리의 적 셋에게 (그 화살 피해 그대로 · 보스 하나면 한 대)
    //   떨어뜨렸으면 true (화살은 거둠)
    public bool SkyDrop(Bullet b)
    {
        if (b == null || Time.time >= skyUntil || Paths.Res(Sky) < 2) return false;
        List<Transform> ts = Paths.Nearest(skyAt, skyR, SkyDropTargets);
        if (ts.Count == 0) return false;
        float dmg = b.damage;
        Fx.Spawn("fx_sparkle", b.transform.position, 1f, new Color(0.85f, 0.75f, 1f), 24f);
        b.Release();
        foreach (Transform t in ts)
        {
            if (t == null) continue;
            Fx.Spawn("fx_arrowrain", t.position + Vector3.up * 1f, 1.6f, new Color(0.85f, 0.75f, 1f), 20f);
            Paths.Deal(t.gameObject, dmg, Vector3.down, 0.3f, "하늘 화살");
        }
        Hostile.Play("arrowfly", 0.3f, 0.8f);
        return true;
    }

    // 화살비 게이지 (최대치의 비율)
    public static void AddGauge(float share)
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.skillGauge == null || share <= 0f || p.skillGauge.IsFull()) return;
        p.skillGauge.AddSkillPoint(p.skillGauge.MaxSkillPoint * share);
    }

    void SkyCharge(PlayerController p)
    {
        SkillGauge g = p.skillGauge;
        if (g == null || g.IsFull() || p.IsSkillUsing) return;
        float tree = SpecialAbilities.SharedInstance != null ? SpecialAbilities.SharedInstance.TreeGaugeMul : 1f;
        g.AddSkillPoint(g.pointsPerSecond * GameMode.GaugeMul / Mathf.Max(0.1f, CharacterData.Current.gauge) * (g.Boosted ? g.killBoost : 1f) * tree * SkyGaugeBonus * Time.deltaTime);
    }

    void AutoShot(PlayerController p)
    {
        autoShotAt = Time.time + WindAutoStep;
        SignatureSkills s = SignatureSkills.Instance;
        if (s == null) return;
        List<Transform> t = Paths.Nearest(p.transform.position, 13f, 1);
        if (t.Count == 0 || t[0] == null) return;
        Vector3 start = p.MuzzlePosition;
        Vector2 d = ((Vector2)(t[0].position - start)).normalized;
        if (d.sqrMagnitude < 0.01f) return;
        s.Shot(start, d, Paths.Atk * WindAutoPower * Paths.Power(Wind), 1, 65f, 16f, "fx_arrow", 0.4f, new Color(0.7f, 1f, 0.9f));
        Hostile.Play("arrowfly", 0.2f, 1.5f);
    }

    // ================================================================= 고유 스킬이 부름 (SignatureSkills.Archer)
    // 후퇴 사격으로 물러난 뒤 (살아 있을 때만 · from: 물러나기 전 자리)
    public static void Retreated(Vector3 from)
    {
        ArcherPaths me = Me;
        if (me == null) return;
        if (Paths.Res(Wind) >= 2 && Time.time >= me.dodgeReadyAt)
        {
            me.dodgeReadyAt = Time.time + WindDodgeCd;
            Paths.Invuln(WindDodge);                    // 한 곳(PlayerController)의 무적 끝 시각에 Max 로만 · 0.8초
            PlayerController p = Hostile.Player;
            if (p != null) Fx.Spawn("fx_smoke", p.transform.position, 2f, new Color(0.75f, 1f, 0.95f, 0.7f), 20f);
        }
        if (Paths.CrossOn(XGust)) me.GustTrap(from);
    }

    // 화살비를 쏨 (반지름은 천공 범위까지 곱한 값)
    public static void Rained(Vector3 center, float radius)
    {
        ArcherPaths me = Me;
        if (me == null || !Paths.Has(Sky)) return;
        me.skyAt = center;
        me.skyR = radius;
        me.skyUntil = Time.time + SkyLinger;
        // 공명 I: 화살비 자리의 적에게 사냥감 표식 (3초 동안 0.3초마다 다시)
        if (Paths.Res(Sky) >= 1)
            Paths.Zone(center, radius, SkyLinger, 0.3f, z => Paths.Circle(center, radius, 0f, 0f, null, c => HuntMark.Apply(c.gameObject, 4f)));
    }

    // 속사 공명 II: 화살이 같은 적을 맞힌 수
    public static void CountHit(Collider2D c)
    {
        ArcherPaths me = Me;
        if (me == null || c == null || Paths.Res(Rapid) < 2) return;
        GameObject g = c.gameObject;
        me.rapidHits.TryGetValue(g, out int n);
        if (n < 999) me.rapidHits[g] = n + 1;
    }

    // 울림 화살촉 범위 배율 (속사 공명 II: 같은 적에 열 발 이상)
    public static float RingMul(GameObject g)
    {
        ArcherPaths me = Me;
        if (me == null || g == null || Paths.Res(Rapid) < 2) return 1f;
        return me.rapidHits.TryGetValue(g, out int n) && n >= RapidRingHits ? RapidRingMul : 1f;
    }

    // ================================================================= 덫
    void GustTrap(Vector3 at)
    {
        if (Time.time < gustTrapAt) return;
        gustTrapAt = Time.time + GustTrapCd;
        PlaceTrap(at);
    }

    void PlaceTrap(Vector3 at)
    {
        if (!Paths.Has(Trap)) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        at = Hostile.ClampArena(at);
        at.z = 0f;
        Paths.Trim(traps, TrapMax);                     // 넘치면 가장 오래된 덫부터
        bool[] sprung = { false };
        SigZone z = Paths.Zone(at, TrapRadius, TrapLife, 0.1f, zone => TrapTick(zone, sprung), "fx_net", new Color(0.7f, 0.95f, 0.55f, 0.75f), TrapRadius * 2.4f, true);
        if (z == null) return;
        traps.Add(z.gameObject);
        Fx.Spawn("fx_sparkle", at, 1.2f, new Color(0.7f, 1f, 0.55f), 22f);
        Hostile.Play("clank", 0.3f, 1.3f);
    }

    // 살아 있는 적이 덫에 들어오면 한 번 터짐
    void TrapTick(SigZone z, bool[] sprung)
    {
        if (z == null || sprung[0]) return;
        Vector3 p = z.transform.position;
        bool found = false;
        using (var list = PhysHits.Circle(p, TrapRadius, true))
            foreach (Collider2D c in list)
            {
                if (c == null || !Paths.IsFoe(c)) continue;
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && e.IsDead) continue;
                found = true;
                break;
            }
        if (!found) return;
        sprung[0] = true;
        Object.Destroy(z.gameObject);
        Burst(p);
    }

    void Burst(Vector3 p)
    {
        float dmg = Paths.Atk * TrapPower * Paths.Power(Trap);
        bool master = Paths.Mastered(Trap);
        int rooted = 0;
        Paths.Circle(p, TrapBurstRadius, dmg, 0f, "덫", c =>
        {
            GameObject g = c.gameObject;
            bool boss = Paths.IsBoss(g);
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && !e.IsDead && (!boss || master) && RootFree(g)) { e.Slow(0f, boss ? TrapBossRoot : TrapRoot); MarkRooted(g, e.SlowUntil); rooted++; }
            else if (boss && master && e == null) snares[g] = Time.time + TrapSnareTime;     // 보스 (몸을 묶을 수 없음): 받는 피해 +
            Fx.Spawn("fx_net", c.transform.position, 2f, new Color(0.8f, 1f, 0.6f, 0.9f), 16f);
        });
        Fx.Spawn("fx_shock", p, TrapBurstRadius * 2f, new Color(0.6f, 1f, 0.45f, 0.7f), 20f);
        ShockRing.Spawn(p, 0.3f, TrapBurstRadius, 0.3f, new Color(0.65f, 1f, 0.5f, 0.9f), 0.14f);
        Hostile.Play("clank", 0.5f, 0.9f);
        // 공명 I: 터진 자리에 가시 덤불 (가시 덤불 카드 단계만큼 오래)
        if (Paths.Res(Trap) >= 1 && CharacterKit.Instance != null) CharacterKit.Instance.BowPathThorns(p);
        // 공명 II: 적을 묶은 덫 둘이 3초 안에 가까이 터지면 가시줄 (한 번에 둘까지)
        if (Paths.Res(Trap) >= 2 && rooted > 0)
        {
            float now = Time.time;
            int links = 0;
            for (int i = bursts.Count - 1; i >= 0 && links < 2; i--)
            {
                var b = bursts[i];
                if (b.until < now || (b.p - p).sqrMagnitude > LineRange * LineRange || (b.p - p).sqrMagnitude < 0.25f) continue;
                ThornLine(b.p, p);
                links++;
            }
            if (bursts.Count >= 10) bursts.RemoveAt(0);
            bursts.Add((p, now + LineLife));
        }
    }

    // 속박 뒤 짧은 면역: 묶임이 풀린 뒤 RootImmune 초 동안은 다시 묶지 않음 (가시줄 감속 + 줄 위 덫으로 계속 묶이는 것 방지 · 피해는 그대로)
    const float RootImmune = 2f;
    readonly Dictionary<GameObject, float> rootFreeAt = new Dictionary<GameObject, float>();

    // 1.4.1~ 덫에 묶여 있는 동안은 가시 덤불 · 가시줄 감속이 묶임(속도 0)을 이어 붙이지 않게 (EnermyController.Slow 가 더 느린 쪽 · 더 늦은 끝을 남김
    //   → 묶인 적이 덤불 밖으로 못 나가 덤불이 사라질 때까지 묶이던 것) — 묶임 끝 = 면역 끝 − RootImmune
    public static bool Rooted(GameObject g)
    {
        ArcherPaths me = Me;
        return me != null && g != null && me.rootFreeAt.TryGetValue(g, out float t) && Time.time < t - RootImmune;
    }

    bool RootFree(GameObject g)
    {
        float now = Time.time;
        if (g == null || (rootFreeAt.TryGetValue(g, out float t) && now < t)) return false;
        if (rootFreeAt.Count >= 64)
        {
            scratch.Clear();
            foreach (var kv in rootFreeAt) if (kv.Key == null || kv.Value <= now) scratch.Add(kv.Key);
            foreach (GameObject k in scratch) rootFreeAt.Remove(k);
            scratch.Clear();
        }
        return true;
    }

    // 1.4.1~ 면역 끝은 실제 묶임 끝(EnermyController.SlowUntil) 기준 — 다른 감속으로 묶임이 길어져도 면역이 먼저 끝나 다음 덫이 바로 다시 묶지 않게
    void MarkRooted(GameObject g, float rootUntil)
    {
        if (g == null) return;
        rootFreeAt[g] = Mathf.Max(Time.time, rootUntil) + RootImmune;
    }

    void ThornLine(Vector3 a, Vector3 b)
    {
        Paths.Trim(lines, LineCap);
        float dmg = Paths.Atk * LinePower * Paths.Power(Trap);
        SigZone z = Paths.Zone((a + b) * 0.5f, LineWidth, LineLife, 0.3f, zone => LineTick(zone, dmg));
        if (z == null) return;
        z.segment = true;
        z.a = a;
        z.b = b;
        FxAnim beam = Fx.Beam(a, b, 0.5f, new Color(0.55f, 0.9f, 0.4f, 0.85f), LineLife, 9);
        if (beam != null) beam.transform.SetParent(z.transform, true);
        lines.Add(z.gameObject);
        Hostile.Play("whoosh", 0.3f, 1.5f);
    }

    static void LineTick(SigZone z, float dmg)
    {
        SignatureSkills s = SignatureSkills.Instance;
        if (s == null || z == null) return;
        foreach (Collider2D c in s.Inside(z))
        {
            if (c == null) continue;
            Paths.Deal(c.gameObject, dmg, Vector3.zero, 0f, "가시줄");
            if (c == null) continue;
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && !e.IsDead && !Paths.IsBoss(c.gameObject) && !Rooted(e.gameObject)) e.Slow(0.4f, 0.4f);      // 1.4.1~ 덫에 묶인 적은 건너뜀
        }
    }
}
