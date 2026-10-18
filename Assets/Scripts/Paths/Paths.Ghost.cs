using System.Collections.Generic;
using UnityEngine;

// 유령 갈래 5개 (기획서 3-9) — 데이터 + 입문 · 공명 I · II · 극의 · 새 교차 효과 (2026-10-19)
// 유령 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Ghost.cs · 기존 파일(CharacterKit.Ghost.cs · SignatureSkills.Ghost.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 사령술이 빙의를 늘리므로 PossessRoutine 의 빙의 상태 · 이동 속도 · 반투명을 「끝나는 시각」 방식으로 바꿈 (기획서 6-3 (1) 5 · CharacterKit.Ghost)
// 1.3.5 · 1.3.7 너프(「유령이 너무 안 죽음」)를 되돌리지 않게: 사령술 무적은 처음 2초만 · 유체 이탈 최대 체력 −30% · 닿지 않는 피해 +30%
// 보스 화력은 갈래마다 ±8% 안으로 (균열 · 영혼 돌진 · 원혼 수치를 작게, 강점은 무리 처리 · 제어 · 생존)
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
public sealed class GhostPaths : HeroPaths
{
    public const int Frost = 0, Shatter = 1, Legion = 2, Necro = 3, Phase = 4;

    public override CharacterId Who => CharacterId.Ghost;
    public override string IconKey => "ghost";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "h.frost", name = "혹한", tagline = "냉기 두 배 · 무리 제어",
            seed = "서리 손", seedDesc = "냉기가 두 배로 쌓이고 손길이 1칸 길어집니다.",
            cost = "차가운 손길 피해가 25% 줄어듭니다.", strong = "무리 제어", weak = "보스 (얼지 않음)",
            cards = new[] { P, S(2), S(0) }, evo = "he.blizzard",
            res1 = "얼어붙은 적 둘레 2칸에 냉기 오라가 생겨 0.6초마다 냉기 1을 쌓습니다.",
            res2 = "얼음이 풀릴 때 냉기 파동이 퍼져 둘레 3칸의 적에게 피해(공격력 40%)와 냉기 2를 줍니다.",
            mastery = "눈보라의 냉기 안개가 2배 오래 남습니다.",
            color = new Color(0.6f, 0.9f, 1f) },
        new PathDef {
            key = "h.shatter", name = "산산조각", tagline = "얼린 적 부수기 · 보스 균열",
            seed = "산산조각", seedDesc = "얼어붙은 적을 손길로 치면 부서집니다. (잡몹 즉사 · 큰 적은 최대 체력 6%) 보스는 냉기 5중첩마다 균열이 터집니다. (공격력 80% · 같은 보스에 2초에 한 번)",
            cost = "얼어붙는 시간이 50% 줄어듭니다.", strong = "보스 · 처형", weak = "냉기 준비 시간",
            cards = new[] { M, B, S(8) }, evo = "he.glacier",
            res1 = "부서질 때 얼음 파편 4개가 기본으로 튑니다.",
            res2 = "보스 균열 세 번째마다 큰 균열이 터집니다. (공격력 200% · 둘레 3칸의 적에게도 80%)",
            mastery = "냉기 3중첩이면 균열이 터집니다.",
            color = new Color(0.75f, 0.95f, 1f), form = "얼음 유령" },
        new PathDef {
            key = "h.legion", name = "영혼 군단", tagline = "영혼이 돌진 공격 · 자동 화력",
            seed = "영혼 거느림", seedDesc = "영혼이 1.2초마다 하나씩 가까운 적에게 돌진했다 돌아오고 (공격력 30%), 영혼이 2배로 잘 생깁니다.",
            cost = "영혼이 피해를 막을 확률이 50%가 됩니다.", strong = "자동 화력 · 무리", weak = "방패 약화",
            cards = new[] { A, S(1), S(5), S(9) }, evo = "he.banshee",
            res1 = "거느릴 수 있는 영혼이 2 늘어납니다. (도깨비불의 피해 증가는 영혼 6까지)",
            res2 = "영혼이 5 이상이면 모든 영혼이 서로 다른 적에게 한꺼번에 돌진합니다.",
            mastery = "영혼 상한이 10이 됩니다.",
            color = new Color(0.7f, 0.75f, 1f), form = "사슬 망령" },
        new PathDef {
            key = "h.necro", name = "사령술", tagline = "긴 빙의 · 무적 근접 폭딜",
            seed = "사령술", seedDesc = "빙의가 3초에서 4.5초로 늘고, 빙의한 몸이 부딪친 적에게 냉기와 피해(공격력 30%)를 주며, 빙의 게이지가 25% 덜 듭니다.",
            cost = "평타 피해가 20% 줄고, 빙의 중 무적은 처음 2초뿐입니다.", strong = "무적 시간 · 보스 근접 폭딜", weak = "빙의 밖 화력",
            cards = new[] { G, S(6), K }, evo = "he.doom",
            res1 = "빙의 폭발 범위가 1.5배가 되고, 폭발에 휘말린 적(보스 제외)에게 저주가 걸립니다.",
            res2 = "터진 몸에서 원혼 셋이 나와 가까운 적을 노립니다. (공격력 50%)",
            mastery = "터진 뒤 가까운 적에게 한 번 바로 다시 빙의합니다. (3초 · 무적 1.5초 · 폭발 피해 50%)",
            color = new Color(0.6f, 0.45f, 0.85f) },
        new PathDef {
            key = "h.phase", name = "유체 이탈", tagline = "움직이면 접촉 무시 · 기동 생존",
            seed = "유체 이탈", seedDesc = "움직이는 동안 적의 몸을 통과하고, 몸에 닿는 피해를 받지 않습니다.",
            cost = "최대 체력 −30%, 몸에 닿는 것이 아닌 피해(투사체 · 패턴)를 30% 더 받습니다.", strong = "기동 · 접촉 생존", weak = "탄막 보스",
            cards = new[] { S(3), S(4), S(7) }, evo = "he.specter",
            res1 = "통과한 적에게 냉기 1을 쌓습니다.",
            res2 = "폴터가이스트가 내가 통과한 적을 먼저 노립니다.",
            mastery = "멈춰도 0.5초 동안 유지됩니다.",
            color = new Color(0.85f, 0.9f, 1f), form = "그림자 유령" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Necro, Phase, "he.house"),                    // 소란한 집 (K · S7)
        Cross(Shatter, Phase, "he.vampire"),                // 흡혈귀 (M · S3)
        Cross(Frost, Legion, "h.x.frostlegion", "서리 군단", "영혼의 돌진이 냉기 2를 쌓습니다."),
        Cross(Necro, Shatter, "h.x.innerfreeze", "안에서 얼리기", "빙의 폭발이 둘레의 적을 얼립니다."),
    };

    // ================================================================= 효과
    // 수치 (카드 글 · 공명 글과 같은 값 — 고치면 위 문장 · LocTable.Paths.Ghost 도 같이)
    public const float FrostReach = 1f;              // 혹한: 손길 +1칸
    const float FrostHandMul = 0.75f;                // 혹한 대가: 손길 피해 −25%
    const float FrostAuraR = 2f, FrostAuraEvery = 0.6f;     // 혹한 공명 I: 얼어붙은 적 둘레 2칸 · 0.6초마다 냉기 1
    const float FrostWaveR = 3f, FrostWaveAtk = 0.4f;       // 혹한 공명 II: 풀릴 때 둘레 3칸 · 공격력 40% · 냉기 2
    public const float ShatterFreezeMul = 0.5f;      // 산산조각 대가: 얼어붙는 시간 −50%
    const float ShatterBigShare = 0.06f;             // 산산조각: 큰 적은 최대 체력 6%
    const float CrackAtk = 0.8f, BigCrackAtk = 2f, BigCrackR = 3f, CrackGap = 2f;     // 보스 균열 공격력 80% · 큰 균열 200% (둘레 3칸) · 같은 보스에 2초에 한 번
    const float LegionEvery = 1.2f, LegionAtk = 0.3f, LegionRange = 9f;    // 영혼 군단: 1.2초마다 · 공격력 30% · 9칸 안
    const int LegionBonus = 2, LegionCap = 10, LegionVolley = 5;           // 공명 I +2 · 극의 상한 10 · 공명 II 일제 돌진 5 이상
    public const float NecroRefund = 0.25f;          // 사령술: 빙의 게이지 25% 덜 듦
    const float NecroTime = 4.5f, NecroSafe = 2f, NecroHandMul = 0.8f;     // 사령술: 빙의 4.5초 · 대가: 무적은 처음 2초 · 평타 −20%
    const float NecroBumpAtk = 0.3f, NecroBumpEvery = 0.25f;               // 부딪친 적에게 공격력 30% · 냉기 1 (0.25초마다)
    const float NecroBlastMul = 1.5f, NecroGrudgeAtk = 0.5f;               // 공명 I 폭발 범위 1.5배 · 공명 II 원혼 공격력 50%
    const float ChainShare = 0.5f;                   // 극의: 다시 빙의한 폭발 피해 50%
    const float ChainTime = 3f, ChainSafe = 1.5f, ChainRange = 8f;         // 극의: 다시 빙의 3초 · 무적 1.5초 · 8칸 안
    const float PhaseHpMul = 0.7f, PhaseHitMul = 1.3f, PhaseLinger = 0.5f; // 유체 이탈 대가: 최대 체력 −30% · 닿는 것이 아닌 피해 +30% · 극의 0.5초
    const float PhasePassEvery = 0.8f;               // 공명 I: 같은 적은 0.8초에 한 번 냉기 1
    const int CrossFrostLegion = 2, CrossInnerFreeze = 3;                  // 새 교차 번호 (BuildCrosses 순서)

    static readonly Color Ice = new Color(0.6f, 0.9f, 1f, 0.9f);
    static readonly Color Soul = new Color(0.75f, 0.8f, 1f, 0.9f);

    // 이번 판의 유령 갈래 효과 (갈래 규칙이 아니거나 다른 영웅이면 null) — 상태는 모두 인스턴스 필드 (판마다 새로)
    static GhostPaths Me => Paths.Active ? Paths.Hero as GhostPaths : null;
    static CharacterKit Kit => CharacterKit.Instance != null && CharacterKit.Instance.Id == CharacterId.Ghost ? CharacterKit.Instance : null;

    // 갈래 효과 코드의 예외가 평타 · 빙의 흐름을 끊지 않게 (Paths 와 같이 1초에 한 번만 로그)
    static float errAt = -9f;
    static void Report(System.Exception e)
    {
        if (Time.unscaledTime - errAt < 1f) return;
        errAt = Time.unscaledTime;
        Debug.LogException(e);
    }

    // ---------------- 판 흐름
    public override void OnSeed(int path, bool replay)
    {
        // 유체 이탈 대가: 최대 체력 −30% (이어하기도 기록 순서대로 다시 곱하므로 같은 값)
        if (path == Phase)
        {
            PlayerController p = Hostile.Player;
            if (p != null)
            {
                p.PlayerMaxHealth = Mathf.Max(1f, Mathf.Round(p.PlayerMaxHealth * PhaseHpMul));
                p.PlayerHealth = Mathf.Min(p.PlayerHealth, p.PlayerMaxHealth);
            }
        }
    }

    public override void End()
    {
        Unghost();
        pendingDash.Clear();
        thaws.Clear();
        passed.Clear();
        crack.Clear();
    }

    // ---------------- 묻기 (CharacterKit.Ghost · SignatureSkills.Ghost 가 부름 · 갈래가 아니면 원래 값)
    // 평타(차가운 손길) 배율: 혹한 −25% · 사령술 −20%
    public static float HandMul() => (Paths.Has(Frost) ? FrostHandMul : 1f) * (Paths.Has(Necro) ? NecroHandMul : 1f);

    // 영혼 상한: 영혼 군단 공명 I +2 · 극의 10
    public static int WispCap(int n)
    {
        if (!Paths.Has(Legion)) return n;
        if (Paths.Mastered(Legion)) return Mathf.Max(n, LegionCap);
        return Paths.Res(Legion) >= 1 ? n + LegionBonus : n;
    }

    // 영혼 군단 대가: 영혼이 막을 확률 50%
    public static bool WispBlockFails() => Paths.Has(Legion) && Random.value >= 0.5f;

    // 혹한 극의: 눈보라 냉기 안개 2배
    public static float MistMul => Paths.Mastered(Frost) ? 2f : 1f;

    // 사령술: 빙의 길이 · 그중 무적인 앞부분 · 폭발 범위
    public static float PossessLength(float normal, bool chained) => chained ? ChainTime : Paths.Has(Necro) ? NecroTime : normal;
    public static float PossessSafeLength(float len, bool chained) => chained ? ChainSafe : Paths.Has(Necro) ? Mathf.Min(len, NecroSafe) : len;
    public static float BlastMul => Paths.Res(Necro) >= 1 ? NecroBlastMul : 1f;
    public static bool InnerFreeze => Paths.CrossOn(CrossInnerFreeze);
    public static bool ChainPossess => Paths.Mastered(Necro);
    public static float ChainDamage => ChainShare * Paths.Power(Necro);

    // ================================================================= 혹한
    readonly List<GameObject> frozenNow = new List<GameObject>(16);
    readonly List<(GameObject g, float at)> thaws = new List<(GameObject g, float at)>(32);
    readonly List<Vector3> waveAt = new List<Vector3>(8);
    float auraAt, waveBudgetAt;
    int waveBudget;

    // 얼어붙음 (CharacterKit.AddChill): 공명 II 면 풀리는 시각을 기억
    public static void Froze(GameObject g, float until)
    {
        GhostPaths me = Me;
        if (me == null || g == null || !Paths.Has(Frost) || Paths.Res(Frost) < 2) return;
        for (int i = 0; i < me.thaws.Count; i++)
            if (me.thaws[i].g == g) { me.thaws[i] = (g, until); return; }
        if (me.thaws.Count >= 64) me.thaws.RemoveAt(0);         // 상한 (가장 오래된 것부터)
        me.thaws.Add((g, until));
    }

    void FrostTick(CharacterKit kit)
    {
        float now = Time.time;
        // 공명 I: 얼어붙은 적 둘레에 냉기 오라 (얼어붙은 적 12마리까지)
        if (Paths.Res(Frost) >= 1 && now >= auraAt)
        {
            auraAt = now + FrostAuraEvery;
            int n = kit.FrozenNow(frozenNow, 12);
            for (int i = 0; i < n; i++)
            {
                GameObject src = frozenNow[i];
                if (src == null) continue;
                using var hits = PhysHits.Circle(src.transform.position, FrostAuraR, true);
                foreach (Collider2D c in hits)
                    if (c != null && Paths.IsFoe(c) && c.gameObject != src) kit.AddChill(c.gameObject, 1);
                Fx.Spawn("fx_sparkle", src.transform.position, 1.1f, new Color(Ice.r, Ice.g, Ice.b, 0.5f), 14f);
            }
            frozenNow.Clear();
        }
        // 공명 II: 얼음이 풀릴 때 냉기 파동 (0.5초에 6번까지 · 연쇄로 화면이 뒤덮이지 않게)
        if (thaws.Count == 0) return;
        if (now >= waveBudgetAt) { waveBudgetAt = now + 0.5f; waveBudget = 6; }
        waveAt.Clear();
        for (int i = thaws.Count - 1; i >= 0; i--)
        {
            (GameObject g, float at) = thaws[i];
            if (g == null) { thaws.RemoveAt(i); continue; }
            if (now < at) continue;
            thaws.RemoveAt(i);
            if (!g.activeInHierarchy || (g.TryGetComponent(out EnermyController e) && e.IsDead) || kit.IsFrozen(g)) continue;
            if (waveBudget <= 0) continue;
            waveBudget--;
            waveAt.Add(g.transform.position);
        }
        foreach (Vector3 p in waveAt) ColdWave(kit, p);       // 목록을 다 고른 뒤에 (파동이 또 얼려 thaws 가 바뀌어도 안전)
        waveAt.Clear();
    }

    void ColdWave(CharacterKit kit, Vector3 p)
    {
        float dmg = Paths.Atk * FrostWaveAtk * Paths.Power(Frost);
        Paths.Circle(p, FrostWaveR, dmg, 1f, "냉기 파동", c => { if (kit != null && c != null) kit.AddChill(c.gameObject, 2); });
        ShockRing.Spawn(p, 0.3f, FrostWaveR, 0.35f, Ice, 0.2f);
        Fx.Spawn("fx_cloud", p, FrostWaveR * 1.1f, new Color(0.8f, 0.95f, 1f, 0.45f), 14f);
        Hostile.Play("freeze", 0.3f, 1.5f);
    }

    // ================================================================= 산산조각
    readonly Dictionary<GameObject, (int chill, float ready, int count)> crack = new Dictionary<GameObject, (int, float, int)>();

    // 얼어붙은 적을 손길로 침 (CharacterKit.Hand): 잡몹 즉사 · 큰 적 최대 체력 6% · 보스는 균열로 따로
    public static void TryShatter(CharacterKit kit, GameObject g, Vector2 dir)
    {
        if (kit == null || g == null || !Paths.Has(Shatter) || g.CompareTag("boss")) return;
        try
        {
            if (!g.TryGetComponent(out EnermyController e) || e.IsDead) return;
            Vector3 pos = g.transform.position;
            Collider2D col = g.GetComponent<Collider2D>();
            bool big = g.GetComponent<MidBossMark>() != null || CharacterKit.BodyRadiusOf(col) >= 1.4f;
            if (big)
            {
                Paths.Deal(g, Mathf.Max(1f, e.setEnemyHP) * ShatterBigShare * Paths.Power(Shatter), dir, 0.5f, "산산조각");
            }
            else
            {
                // 잡몹: 즉사 (남은 체력보다 크게 · 그래도 남으면 스스로 터진 것으로)
                Paths.Deal(g, Mathf.Max(1f, e.EnemyHealth) * 4f + 10f, dir, 0.5f, "산산조각");
                if (e != null && !e.IsDead) e.KillBySkill();
            }
            // 공명 I: 부서질 때 얼음 파편 4개 (얼음 파편 카드가 있으면 잡몹은 카드 파편이 이미 튐)
            if (Paths.Res(Shatter) >= 1 && (big || kit.card[6] <= 0)) kit.IceShards(pos, 4, Paths.Power(Shatter));
            kit.Thaw(g);
            Fx.Spawn("fx_sparkle", pos, big ? 2.6f : 1.8f, new Color(0.85f, 0.97f, 1f), 22f);
            ShockRing.Spawn(pos, 0.2f, big ? 2.2f : 1.4f, 0.22f, Ice, 0.16f);
            Hostile.Play("shatter", 0.45f, Random.Range(1.05f, 1.3f));
        }
        catch (System.Exception ex) { Report(ex); }
    }

    // 보스 냉기 (CharacterKit.AddChill, 얼어 있는 동안도 셈): 5중첩(극의 3)마다 균열 · 같은 보스에 2초에 한 번 · 공명 II 세 번째마다 큰 균열
    public static void CrackChill(CharacterKit kit, GameObject g, int n)
    {
        GhostPaths me = Me;
        if (me == null || g == null || n <= 0 || !Paths.Has(Shatter) || !g.CompareTag("boss")) return;
        try
        {
            int need = Paths.Mastered(Shatter) ? 3 : 5;
            me.crack.TryGetValue(g, out var st);
            st.chill = Mathf.Min(st.chill + n, need);           // 기다리는 동안은 한 번 몫까지만 모아 둠
            if (st.chill >= need && Time.time >= st.ready)
            {
                st.chill = 0;
                st.ready = Time.time + CrackGap;
                st.count++;
                me.crack[g] = st;
                me.Crack(g, Paths.Res(Shatter) >= 2 && st.count % 3 == 0);
                return;
            }
            me.crack[g] = st;
            if (me.crack.Count > 16) me.PruneCrack();
        }
        catch (System.Exception ex) { Report(ex); }
    }

    void Crack(GameObject boss, bool big)
    {
        if (boss == null) return;
        Vector3 p = boss.TryGetComponent(out Collider2D col) ? (Vector3)col.bounds.center : boss.transform.position;
        float power = Paths.Power(Shatter);
        Paths.Deal(boss, Paths.Atk * (big ? BigCrackAtk : CrackAtk) * power, Vector3.zero, 0f, "균열");
        if (big)
        {
            // 큰 균열: 둘레 3칸의 다른 적에게도 (보스는 위에서 한 번만)
            float splash = Paths.Atk * CrackAtk * power;
            Paths.Circle(p, BigCrackR, 0f, 0f, null, c => { if (c != null && c.gameObject != boss && !c.CompareTag("boss")) Paths.Deal(c.gameObject, splash, (c.transform.position - p).normalized, 1.5f, "균열"); });
            ShockRing.Spawn(p, 0.4f, BigCrackR, 0.35f, Ice, 0.25f);
            Hostile.Shake(0.15f);
        }
        Fx.Spawn("fx_fissure", p, big ? 3.4f : 2.2f, new Color(0.8f, 0.96f, 1f), 18f, Random.Range(0f, 360f));
        Fx.Spawn("fx_sparkle", p, big ? 2.4f : 1.5f, Ice, 20f);
        Hostile.Play(big ? "shatter" : "crack", big ? 0.6f : 0.4f, big ? 0.8f : 1.3f);
    }

    void PruneCrack()
    {
        frozenNow.Clear();
        foreach (var kv in crack) if (kv.Key == null || !kv.Key.activeInHierarchy) frozenNow.Add(kv.Key);
        foreach (GameObject g in frozenNow) crack.Remove(g);
        frozenNow.Clear();
    }

    // ================================================================= 영혼 군단
    readonly List<(Transform t, float at)> pendingDash = new List<(Transform t, float at)>(16);
    float dashAt;
    int dashNext;
    readonly string[] wispBadge = new string[LegionCap + 4];

    void LegionTick(CharacterKit kit, Vector3 p)
    {
        float now = Time.time;
        // 닿는 순간 피해 (영혼 그림이 반쯤 갔을 때)
        for (int i = pendingDash.Count - 1; i >= 0; i--)
        {
            (Transform t, float at) = pendingDash[i];
            if (now < at) continue;
            pendingDash.RemoveAt(i);
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            if (t.TryGetComponent(out EnermyController e) && e.IsDead) continue;
            GameObject g = t.gameObject;
            Paths.Deal(g, Paths.Atk * LegionAtk * Paths.Power(Legion), (t.position - p).normalized, 0.4f, "영혼 돌진");
            if (Paths.CrossOn(CrossFrostLegion) && g != null) kit.AddChill(g, 2);        // 교차 서리 군단
            Fx.Spawn("fx_soulburst", t.position, 1.2f, Soul, 22f);
        }
        if (kit.Wisps <= 0 || now < dashAt) return;
        dashAt = now + LegionEvery;
        int wisps = kit.Wisps;
        // 공명 II: 영혼 5 이상이면 모두 서로 다른 적에게 (적이 모자라면 남는 영혼은 쉼) · 아니면 하나씩 돌아가며
        bool volley = Paths.Res(Legion) >= 2 && wisps >= LegionVolley;
        List<Transform> targets = Paths.Nearest(p, LegionRange, volley ? wisps : 1);
        for (int i = 0; i < targets.Count; i++)
        {
            int slot = volley ? i : dashNext++ % wisps;
            Dash(kit, slot, targets[i]);
        }
        if (targets.Count > 0) Hostile.Play("whoosh", volley ? 0.35f : 0.2f, 1.6f);
    }

    void Dash(CharacterKit kit, int slot, Transform t)
    {
        if (t == null) return;
        if (pendingDash.Count >= 16) pendingDash.RemoveAt(0);       // 상한 16 (영혼 상한 10 + 여유)
        kit.WispDash(slot, t);
        pendingDash.Add((t, Time.time + CharacterKit.WispDashTime * 0.5f));
    }

    // ================================================================= 사령술
    float bumpAt;

    // 빙의한 몸이 부딪친 적 (CharacterKit.PossessRoutine 이 매 프레임): 0.25초마다 냉기 1 · 공격력 30%
    public static void PossessBump(CharacterKit kit, Transform host)
    {
        GhostPaths me = Me;
        if (me == null || kit == null || host == null || !Paths.Has(Necro) || Time.time < me.bumpAt) return;
        me.bumpAt = Time.time + NecroBumpEvery;
        try
        {
            Collider2D own = host.GetComponentInChildren<Collider2D>();
            Vector3 at = own != null ? (Vector3)own.bounds.center : host.position;
            float r = CharacterKit.BodyRadiusOf(own) + 1f;
            float dmg = Paths.Atk * NecroBumpAtk * Paths.Power(Necro);
            int n = 0;
            using var hits = PhysHits.Circle(at, r + 1.5f, true);
            foreach (Collider2D c in hits)
            {
                if (c == null || !Paths.IsFoe(c) || c.transform == host || c.transform.IsChildOf(host)) continue;
                if ((c.bounds.center - at).magnitude > r + CharacterKit.BodyRadiusOf(c)) continue;
                Paths.Deal(c.gameObject, dmg, (c.transform.position - at).normalized, 1.5f, "빙의 충돌");
                if (c != null) kit.AddChill(c.gameObject, 1);
                if (++n >= 12) break;
            }
            if (n > 0) Fx.Spawn("fx_shock", at, r * 1.6f, new Color(0.6f, 0.45f, 0.85f, 0.6f), 22f);
        }
        catch (System.Exception ex) { Report(ex); }
    }

    // 공명 I: 폭발에 휘말린 적에게 저주 (보스 · 중간 보스 제외)
    public static void BlastCurse(GameObject g)
    {
        if (g == null || Paths.Res(Necro) < 1 || Paths.IsBoss(g)) return;
        SignatureSkills.PathCurse(g);
    }

    // 공명 II: 터진 몸에서 원혼 셋 (가까운 적 셋 · 공격력 50%)
    public static void PossessBlast(CharacterKit kit, Vector3 at, Transform host)
    {
        if (Paths.Res(Necro) < 2) return;
        try
        {
            float dmg = Paths.Atk * NecroGrudgeAtk * Paths.Power(Necro);
            foreach (Transform t in Paths.Nearest(at, 10f, 3, host != null ? host.gameObject : null))
            {
                if (t == null) continue;
                Fx.Bolt(at, t.position, 0.6f, new Color(0.7f, 0.5f, 1f, 0.9f), 0.22f);
                Fx.Spawn("fx_markskull", t.position + Vector3.up * 0.6f, 1.3f, new Color(0.7f, 0.5f, 1f), 16f);
                Paths.Deal(t.gameObject, dmg, (t.position - at).normalized, 0.8f, "원혼");
            }
        }
        catch (System.Exception ex) { Report(ex); }
    }

    // 극의: 다시 빙의할 가까운 적 (방금 터진 몸은 빼고 · 8칸 안)
    public static Transform ChainTarget(Vector3 at, Transform host)
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return null;
        List<Transform> near = Paths.Nearest(at, ChainRange, 1, host != null ? host.gameObject : null);
        return near.Count > 0 ? near[0] : null;
    }

    // ================================================================= 유체 이탈
    // 움직이는 동안 둘레 적의 몸과 내 몸의 충돌을 끔 (Physics2D.IgnoreCollision — 접촉 피해 · 몸통 밀침이 생기지 않음)
    //   투사체 · 패턴(거리로 맞히는 것)은 그대로 맞음. 멈추면 모두 되돌림 → 붙어 있던 적이 바로 다시 닿음
    //   켜진 동안만 · 끄면 바로 되돌림 · End 에서도 되돌림 (남는 상태 없음)
    readonly List<Collider2D> ghosted = new List<Collider2D>(64);
    readonly HashSet<Collider2D> ghostedSet = new HashSet<Collider2D>();
    readonly HashSet<Collider2D> notBody = new HashSet<Collider2D>();      // 몸통이 아닌 것 (보스 부품 · 영혼구 · 부스러기) — 한 번 물어 둠
    readonly Dictionary<GameObject, float> passed = new Dictionary<GameObject, float>();
    readonly List<GameObject> passedStale = new List<GameObject>(16);
    Collider2D[] own;
    PlayerController ownOf;
    float lastMoveAt = -9f, phasePruneAt;
    bool phaseOn;

    bool PhaseActive(CharacterKit kit, PlayerController p)
    {
        if (!Paths.Has(Phase) || kit == null || p == null || p.IsDying) return false;
        if (kit.Moving) { lastMoveAt = Time.time; return true; }
        return Paths.Mastered(Phase) && Time.time < lastMoveAt + PhaseLinger;      // 극의: 멈춰도 0.5초
    }

    void PhaseTick(CharacterKit kit, PlayerController p)
    {
        phaseOn = PhaseActive(kit, p);
        if (!phaseOn) { if (ghosted.Count > 0) Unghost(); return; }
        if (own == null || ownOf != p) { own = p.GetComponents<Collider2D>(); ownOf = p; }
        Vector3 at = p.transform.position;
        float now = Time.time;
        bool chillPass = Paths.Res(Phase) >= 1;
        float myR = own.Length > 0 && own[0] != null ? CharacterKit.BodyRadiusOf(own[0]) : 0.5f;
        using (var hits = PhysHits.Circle(at, 3.5f, true))
        {
            foreach (Collider2D c in hits)
            {
                if (c == null || !Paths.IsFoe(c) || notBody.Contains(c)) continue;
                // 몸통만 (보스 부품 · 영혼구처럼 닿으면 스스로 터지는 것은 투사체처럼 그대로 맞음)
                if (!ghostedSet.Contains(c) && !IsBody(c)) { notBody.Add(c); continue; }
                if (ghostedSet.Add(c))
                {
                    if (ghosted.Count >= 256) RestoreOldest();
                    for (int i = 0; i < own.Length; i++) if (own[i] != null) Physics2D.IgnoreCollision(own[i], c, true);
                    ghosted.Add(c);
                }
                // 지금 몸이 겹친 적 = 통과 중
                if ((c.bounds.center - at).magnitude > myR + CharacterKit.BodyRadiusOf(c) + 0.2f) continue;
                GameObject g = c.gameObject;
                if (passed.TryGetValue(g, out float last) && now - last < PhasePassEvery) continue;
                passed[g] = now;
                if (chillPass) kit.AddChill(g, 1);         // 공명 I: 통과한 적에게 냉기 1
            }
        }
        if (now >= phasePruneAt)
        {
            phasePruneAt = now + 0.5f;
            // 멀어졌거나 사라진 적은 되돌리고 목록에서 뺌 (목록이 끝없이 늘지 않게)
            for (int i = ghosted.Count - 1; i >= 0; i--)
            {
                Collider2D c = ghosted[i];
                if (c != null && c.gameObject.activeInHierarchy && (c.transform.position - at).sqrMagnitude < 49f) continue;
                Restore(c);
                ghosted.RemoveAt(i);
            }
            if (notBody.Count > 64) notBody.Clear();
            passedStale.Clear();
            foreach (var kv in passed) if (kv.Key == null || now - kv.Value > 4f) passedStale.Add(kv.Key);
            foreach (GameObject g in passedStale) passed.Remove(g);
            passedStale.Clear();
        }
    }

    static bool IsBody(Collider2D c)
    {
        if (c.GetComponent<BossPart>() != null) return false;
        if (c.TryGetComponent(out EnermyController e)) return c.GetComponent<SwarmMote>() == null;
        return c.GetComponentInParent<bosss>() != null;
    }

    void Restore(Collider2D c)
    {
        ghostedSet.Remove(c);
        if (c == null || own == null) return;
        for (int i = 0; i < own.Length; i++)
        {
            if (own[i] == null) continue;
            try { Physics2D.IgnoreCollision(own[i], c, false); }
            catch (System.Exception e) { Report(e); }
        }
    }

    void RestoreOldest()
    {
        if (ghosted.Count == 0) return;
        Restore(ghosted[0]);
        ghosted.RemoveAt(0);
    }

    void Unghost()
    {
        for (int i = 0; i < ghosted.Count; i++) Restore(ghosted[i]);
        ghosted.Clear();
        ghostedSet.Clear();
        notBody.Clear();
        phaseOn = false;
    }

    // 공명 II: 폴터가이스트가 내가 최근(3초) 통과한 적을 먼저 노림 (모자라면 가까운 적으로 채움 · 아니면 null = 원래대로)
    public static List<Transform> PolterTargets(Vector3 p, int n)
    {
        GhostPaths me = Me;
        if (me == null || !Paths.Has(Phase) || Paths.Res(Phase) < 2 || n <= 0) return null;
        List<Transform> res = new List<Transform>(n);
        float now = Time.time;
        foreach (var kv in me.passed)
        {
            if (res.Count >= n) break;
            GameObject g = kv.Key;
            if (g == null || now - kv.Value > 3f || !g.activeInHierarchy || (g.transform.position - p).sqrMagnitude > 100f) continue;
            if (g.TryGetComponent(out EnermyController e) && e.IsDead) continue;
            res.Add(g.transform);
        }
        if (res.Count < n)
            foreach (Transform t in Paths.Nearest(p, 8f, n))
                if (res.Count < n && t != null && !res.Contains(t)) res.Add(t);
        return res;
    }

    // ---------------- 전투 훅
    public override float Taken(float dmg)
    {
        if (dmg <= 0f || !Paths.Has(Phase)) return dmg;
        // 유체 이탈 대가: 몸에 닿는 것이 아닌 피해(투사체 · 패턴) +30%
        //   켜진 동안은 몸이 닿지 않으므로 모든 피해가 「닿는 것이 아닌 피해」 · 멈춰 있을 땐 몸이 겹친 적이 있으면 접촉으로 봄
        if (phaseOn || !Touching()) return dmg * PhaseHitMul;
        return dmg;
    }

    bool Touching()
    {
        PlayerController p = Hostile.Player;
        if (p == null) return false;
        Vector3 at = p.transform.position;
        Collider2D mine = p.GetComponent<Collider2D>();
        float myR = mine != null ? CharacterKit.BodyRadiusOf(mine) : 0.5f;
        using var hits = PhysHits.Circle(at, myR + 2.5f, true);
        foreach (Collider2D c in hits)
            if (c != null && Paths.IsFoe(c) && (c.bounds.center - at).magnitude <= myR + CharacterKit.BodyRadiusOf(c) + 0.35f) return true;
        return false;
    }

    public override void Tick()
    {
        CharacterKit kit = Kit;
        PlayerController p = Hostile.Player;
        if (kit == null || p == null) { if (ghosted.Count > 0) Unghost(); return; }
        if (Paths.Has(Phase) || ghosted.Count > 0) PhaseTick(kit, p);
        if (p.IsDying) return;
        if (Paths.Has(Frost)) FrostTick(kit);
        if (Paths.Has(Legion)) LegionTick(kit, p.transform.position);
    }

    // HUD 입문 칸 숫자: 영혼 군단 = 지금 영혼 수 (문자열은 처음 한 번만 만들어 둠)
    public override string Badge(int path)
    {
        if (path != Legion) return null;
        CharacterKit kit = Kit;
        int n = kit != null ? kit.Wisps : 0;
        if (n <= 0 || n >= wispBadge.Length) return null;
        return wispBadge[n] ??= "x" + n;
    }
}
