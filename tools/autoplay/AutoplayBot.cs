using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// 자동 플레이 봇 (복사본 전용 · tools/autoplay/run.ps1 이 Assets/Scripts/Autoplay 에 넣음)
// 「보통~숙련 플레이어」 흉내: 반응 지연 · 가끔 못 본 탄 · 조준 흔들림을 실력(skill 0~1) 하나로 조절
//   이동: 방향 17개(16방향 + 멈춤)를 점수로 골라 위험(적 몸 · 탄 · 예고 · 바닥 위험 · 결계 · 맵 끝 · 벽)을 피하고
//         표적과 알맞은 거리 · 원 그리며 돌기 · 구슬 · 회복 · 상자 줍기를 더함
//   공격: 영웅마다 평타 방식 (궁수 당겼다 놓기 「완벽」 · 연금술사 불안정 관리 · 과열 거너 쉬기) · 필살기는 무리 · 보스에
//   창: AutoplayBot.Windows.cs (레벨업 · 갈래 · 진화 · 고르기 · 영혼 트리 · 상점 …)
public partial class AutoplayBot : MonoBehaviour
{
    AutoplayRunner.RunRecord rec;
    CharacterId hero;
    string want1, want2;
    float skill;
    System.Random rnd;

    // 실력에서 나오는 값
    float reaction;          // 새 위협을 「보기」까지 걸리는 게임 초 (사람 0.2~0.3)
    float missChance;        // 새 위협 하나를 아예 못 볼 확률
    float decideEvery;       // 이동 방향을 다시 고르는 간격
    float aimErrDeg;         // 조준 흔들림 (표준편차, 도)
    float scoreNoise;        // 방향 점수에 섞는 잡음 (판단 실수)

    public void Begin(AutoplayRunner.RunRecord r, CharacterId id, string p1, string p2, float s, int seed)
    {
        rec = r; hero = id; want1 = p1; want2 = p2; skill = Mathf.Clamp01(s);
        rnd = new System.Random(seed * 7919 + 13);
        reaction = Mathf.Lerp(0.42f, 0.14f, skill);
        missChance = Mathf.Lerp(0.30f, 0.0f, skill);
        decideEvery = Mathf.Lerp(0.2f, 0.06f, skill);
        aimErrDeg = Mathf.Lerp(16f, 1.5f, skill);
        scoreNoise = Mathf.Lerp(2.2f, 0.15f, skill);
        orbitDir = rnd.Next(2) == 0 ? 1f : -1f;
        // 궁수는 손으로 놓아 「완벽!」을 노림 (Auto 기본은 가득 차면 저절로 쏨 · 덫 · 장궁 · 바람 효과는 완벽에서만)
        CharacterKit.ManualBowInAuto = true;
        WindowsBegin();
    }

    void OnDestroy()
    {
        CharacterKit.ManualBowInAuto = false;
        GameInput.AutoMove = Vector2.zero; GameInput.AutoFire = false; GameInput.AutoUlt = false;
    }

    float R01() => (float)rnd.NextDouble();
    float Gauss() { double u1 = 1.0 - rnd.NextDouble(), u2 = rnd.NextDouble(); return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2)); }

    // ================================================================= 리플렉션 (게임 코드의 private 값 읽기)
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
    static readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();
    public static FieldInfo Fld(Type t, string n)
    {
        string k = t.FullName + "." + n;
        if (!fields.TryGetValue(k, out FieldInfo f))
        {
            for (Type x = t; x != null && f == null; x = x.BaseType) f = x.GetField(n, Any | BindingFlags.DeclaredOnly);
            fields[k] = f;
        }
        return f;
    }
    public static T Get<T>(object o, string n, T d = default)
    {
        if (o == null) return d;
        FieldInfo f = Fld(o.GetType(), n);
        if (f != null) return f.GetValue(o) is T v ? v : d;
        PropertyInfo p = o.GetType().GetProperty(n, Any);
        return p != null && p.GetValue(o) is T pv ? pv : d;
    }
    public static T SGet<T>(Type t, string n, T d = default)
    {
        FieldInfo f = Fld(t, n);
        if (f != null) return f.GetValue(null) is T v ? v : d;
        PropertyInfo p = t.GetProperty(n, Any);
        return p != null && p.GetValue(null) is T pv ? pv : d;
    }
    public static void Set(object o, string n, object v) { FieldInfo f = Fld(o.GetType(), n); if (f != null) f.SetValue(o, v); else o.GetType().GetProperty(n, Any)?.GetSetMethod(true)?.Invoke(o, new[] { v }); }
    public static object Call(object o, string n, params object[] a)
    {
        Type t = o is Type st ? st : o.GetType();
        string k = t.FullName + "." + n + "/" + a.Length;
        if (!methods.TryGetValue(k, out MethodInfo m))
        {
            for (Type x = t; x != null && m == null; x = x.BaseType)
                m = x.GetMethods(Any | BindingFlags.DeclaredOnly).FirstOrDefault(mm => mm.Name == n && mm.GetParameters().Length == a.Length);
            methods[k] = m;
        }
        if (m == null) throw new Exception("함수 없음 " + t.Name + "." + n);
        return m.Invoke(o is Type ? null : o, a);
    }

    // ================================================================= 판 진행 기록 (러너가 매 프레임)
    public void Track(EndlessMode em, float s)
    {
        int stage = Get(em, "finalStage", 0);
        bool devSummoned = Get(em, "devSummoned", false), momSummoned = Get(em, "momSummoned", false);
        bool devDown = Get(em, "devDown", false), momDown = Get(em, "momDown", false);
        if (stage >= 1 && rec.finalStartSec < 0f) rec.finalStartSec = s;
        if (devSummoned && rec.fourBossSec < 0f && rec.finalStartSec >= 0f) rec.fourBossSec = s;      // 15분 보스 넷을 모두 쓰러뜨려 개발자를 부름
        bosss dev = Get<bosss>(em, "developer"), mom = Get<bosss>(em, "mom");
        if (dev != null && rec.devSummonSec < 0f) rec.devSummonSec = s;
        if (mom != null && rec.momSummonSec < 0f) rec.momSummonSec = s;
        if (devDown && rec.devKillSec < 0f) rec.devKillSec = s;
        if (momDown && rec.momKillSec < 0f) rec.momKillSec = s;
        rec.bosses = em.BossesDefeated;
        TrackGrowth(em, s, stage);
    }

    // 1.4.2~ 무한 30분 성장 속도 측정 (기획 2026-10-19 10-1) — 게임 필드가 없거나 이름이 바뀌면 반사 기본값이라 그 열만 빈칸 · 0
    bool lastBarrier, lastWave2Due;
    void TrackGrowth(EndlessMode em, float s, int stage)
    {
        PlayerController p = Hostile.Player;
        if (shop == null) shop = FindFirstObjectByType<LevelShop>();
        if (p != null && shop != null)
        {
            // 기준 레벨 = earned + 1 (요구량 기준 · 쌓아 둔 레벨업 포함) — 게임 쪽 ExpLevel 이 있으면 그것도, 없으면 지금 레벨 + 쌓인 수로 받침
            int lv = Mathf.Max(Get(shop, "earned", 0) + 1, Mathf.Max(Get(shop, "ExpLevel", 0), p.level + shop.PendingLevels));
            if (lv >= 30 && rec.lv30Sec < 0f) rec.lv30Sec = s;
            if (lv >= 40 && rec.lv40Sec < 0f) rec.lv40Sec = s;
            if (lv >= 45 && rec.lv45Sec < 0f) rec.lv45Sec = s;
            if (lv >= 50 && rec.lv50Sec < 0f) rec.lv50Sec = s;
            int tr = 0;
            for (int k = 0; k < LevelShop.TranscendCount; k++) tr += shop.TranscendLevel(k);
            rec.transcends = Mathf.Max(rec.transcends, tr);
        }
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        if (sp != null)
        {
            rec.refines = Mathf.Max(rec.refines, sp.Refines);
            if (sp.Refines > 0 && rec.treeDoneSec < 0f) rec.treeDoneSec = s;
        }
        if (s >= 900f && rec.bossesAt15 < 0) { rec.bossesAt15 = em.BossesDefeated; rec.deathsPre15 = rec.deaths; }
        // 뒤 무리 (B1): finalWave 1 → 2 가 된 시각 · 앞 무리를 다 잡아서(wave2Due)가 아니라 60초 타이머로 나왔으면 1
        //   같은 프레임에 앞 무리 마지막 왕이 쓰러지고 바로 나온 경우는 wave2Due 를 못 봐서, 앞 무리 시작 59초 전이면 처치로 봄
        int wave = Get(em, "finalWave", 0);
        if (stage == 1 && wave == 1) lastWave2Due = Get(em, "wave2Due", false);
        if (wave >= 2 && rec.wave2Sec < 0f && rec.finalStartSec >= 0f)
        {
            rec.wave2Sec = s;
            rec.wave2Timer = !lastWave2Due && s - rec.finalStartSec >= 59f ? 1 : 0;
        }
        // 단계 보상 (A3): phaseRewards 비트 수 = 받은 단계 보상 수
        int bits = Get(em, "phaseRewards", 0), n = 0;
        for (; bits != 0; bits &= bits - 1) n++;
        rec.phaseShards = Mathf.Max(rec.phaseShards, n);
        // 결계 · 침식이 켜진 횟수 (꺼짐 → 켜짐)
        bool barrier = BossUltimate.Active;
        if (barrier && !lastBarrier) rec.barriers++;
        lastBarrier = barrier;
    }

    public static void FillPaths(AutoplayRunner.RunRecord r)
    {
        try
        {
            string row = Paths.LogRow();          // path1,path2,tiers,cross,cards
            string[] c = row.Split(',');
            if (c.Length >= 4) { r.path1 = c[0]; r.path2 = c[1]; r.pathTiers = c[2]; r.cross = c[3]; }
        }
        catch { }
    }

    public string StateLine()
    {
        PlayerController p = Hostile.Player;
        EndlessMode em = EndlessMode.Instance;
        string hp = p != null ? Mathf.RoundToInt(p.PlayerHealth) + "/" + Mathf.RoundToInt(p.PlayerMaxHealth) : "-";
        return "체력 " + hp + " · Lv " + (p != null ? p.level : 0) + " · 보스 " + (em != null ? em.BossesDefeated : 0) + " · 단계 " + (em != null ? Get(em, "finalStage", 0) : -1)
             + " · 적 " + EnermyController.Live.Count + " · 보스 살아 있음 " + bosss.LiveCount + " · 탄 " + HostileProjectile.Live.Count
             + " · 갈래 " + Paths.A + "/" + (Paths.Single ? "외길" : Paths.B.ToString()) + " · timeScale " + Time.timeScale.ToString("0.00") + " · 창 " + OpenWindowName()
             + " · 사망 " + rec.deaths + " · 빈 손 " + (Hostile.Player != null && PlayerController.Cinematic ? "컷신" : "");
    }

    // ================================================================= 매 프레임
    void Update()
    {
        PlayerController p = Hostile.Player;
        if (p == null) return;
        // 멈춘 화면(카드 · 창) — 공격 · 필살기를 떼고 창을 처리 (누르고 있으면 카드 선택이 잠김)
        if (Time.timeScale <= 0f || AnyWindowOpen())
        {
            ReleaseAll();
            HandleWindows();
            return;
        }
        HandleWindows();          // 멈추지 않는 창(쌓인 레벨업 열기 · 영혼 트리 사기 등)
        if (p.IsDying || PlayerController.Cinematic || StoryDirector.Playing) { ReleaseAll(); return; }
        Perceive(p);
        Vector3 me = p.transform.position;
        MeasureSpeed(me);
        ChooseTarget(me);
        Move(p, me);
        Aim(me);
        Fire(p, me);
        Ult(p, me);
        NoteHp(p);
        Trace(p, me);
    }

    // AP_TRACE=1: 1초(게임)마다 봇 상태 한 줄 (조정용)
    static readonly bool TraceOn = System.Environment.GetEnvironmentVariable("AP_TRACE") == "1";
    float nextTrace;
    Vector3 traceFrom;
    void Trace(PlayerController p, Vector3 me)
    {
        if (!TraceOn || Time.time < nextTrace) return;
        float moved = ((Vector2)(me - traceFrom)).magnitude / Mathf.Max(0.01f, Time.time - (nextTrace - 1f));
        traceFrom = me; nextTrace = Time.time + 1f;
        AutoplayRunner.Log("T " + EndlessMode.Clock(GameMode.EndlessSeconds) + " 자리 " + ((Vector2)me).ToString("0.0") + " 이동입력 " + GameInput.AutoMove.ToString("0.00") + " 실제속도 " + moved.ToString("0.0") + " 추정 " + speedEst.ToString("0.0")
            + " · 가까운적 " + nearestMob.ToString("0.0") + " 8칸안 " + nearCount + " · 위험 " + lastDanger.ToString("0.0") + " · 표적 " + (hasTarget ? (targetBoss ? "보스 " : "몹 ") + ((Vector2)(targetPos - me)).magnitude.ToString("0.0") : "-")
            + " · 쏨 " + firing + " · 체력 " + Mathf.RoundToInt(p.PlayerHealth) + " · dt " + Time.deltaTime.ToString("0.000") + " uDt " + Time.unscaledDeltaTime.ToString("0.000") + " · 탄 " + projs.Count + " 예고 " + warns.Count + " 장판 " + zones.Count);
    }

    void ReleaseAll()
    {
        if (GameInput.AutoFire) { GameInput.AutoFire = false; GameInput.FireUpFrame = Time.frameCount + 1; }
        if (GameInput.AutoUlt) { GameInput.AutoUlt = false; GameInput.UltUpFrame = Time.frameCount + 1; }
        firing = false; ultHolding = false;
        GameInput.AutoMove = Vector2.zero;
    }

    // ================================================================= 보기 (반응 지연 · 못 본 위협)
    // 위협(탄 · 예고)은 처음 나타난 뒤 reaction 초가 지나야 피하기 시작 · 몇몇은 끝까지 못 봄
    readonly Dictionary<long, float> seenAt = new Dictionary<long, float>();
    readonly HashSet<long> blind = new HashSet<long>();
    float nextSeenClean;

    bool Noticed(long key)
    {
        float now = Time.time;
        if (!seenAt.TryGetValue(key, out float t0))
        {
            float delay = reaction * Mathf.Clamp(1f + Gauss() * 0.25f, 0.6f, 1.6f);
            seenAt[key] = now + delay;
            if (R01() < missChance) blind.Add(key);
            return false;
        }
        return now >= t0 && !blind.Contains(key);
    }

    struct Proj { public Vector2 p, v; public float r; }
    struct Warn { public bool circle; public Vector2 a, b; public float r, fill; }
    struct Zone { public Vector2 c; public float r, w; }
    readonly List<Proj> projs = new List<Proj>(256);
    readonly List<Warn> warns = new List<Warn>(32);
    readonly List<Zone> zones = new List<Zone>(32);
    readonly List<Vector3> mobs = new List<Vector3>(256);
    struct MobInfo { public Vector2 p, v; public float r; }
    readonly List<MobInfo> mobInfo = new List<MobInfo>(256);
    readonly Dictionary<int, (Vector2 p, float t, Vector2 v, float r)> mobTrack = new Dictionary<int, (Vector2, float, Vector2, float)>(512);
    float playerR = 0.5f;
    readonly List<(Vector3 c, float r)> bossBodies = new List<(Vector3, float)>(8);
    float nextSlowScan;
    Telegraph[] telegraphs = new Telegraph[0];
    HazardZone[] hazards = new HazardZone[0];
    bool domainOn;
    Vector2 domainC;
    float domainR;

    void Perceive(PlayerController p)
    {
        Vector3 me = p.transform.position;
        if (Time.time >= nextSeenClean)
        {
            nextSeenClean = Time.time + 5f;
            if (seenAt.Count > 4000) { seenAt.Clear(); blind.Clear(); }
        }
        mobs.Clear(); mobInfo.Clear();
        Collider2D pc = p.GetComponent<Collider2D>();
        if (pc != null) playerR = Mathf.Clamp(Mathf.Min(pc.bounds.extents.x, pc.bounds.extents.y), 0.2f, 1.2f);
        float now = Time.time;
        foreach (EnermyController e in EnermyController.Live)
        {
            if (e == null || e.IsDead) continue;
            Vector3 q = e.transform.position;
            if (((Vector2)(q - me)).sqrMagnitude > 30f * 30f) continue;
            mobs.Add(q);
            int id = e.GetInstanceID();
            Vector2 v = Vector2.zero; float r;
            if (mobTrack.TryGetValue(id, out var tr))
            {
                float dt = now - tr.t;
                v = dt > 0.0001f ? Vector2.Lerp(tr.v, ((Vector2)q - tr.p) / dt, 0.5f) : tr.v;
                if (v.sqrMagnitude > 40f * 40f) v = Vector2.zero;     // 순간이동 · 밀려남
                r = tr.r;
            }
            else
            {
                Collider2D c = e.GetComponent<Collider2D>();
                r = c != null ? Mathf.Clamp(Mathf.Max(c.bounds.extents.x, c.bounds.extents.y), 0.25f, 3f) : 0.5f;
            }
            mobTrack[id] = (q, now, v, r);
            mobInfo.Add(new MobInfo { p = q, v = v, r = r });
        }
        if (mobTrack.Count > 2000) mobTrack.Clear();
        bossBodies.Clear();
        foreach (bosss b in bosss.Live)
        {
            if (b == null || b.IsDead) continue;
            Collider2D col = b.GetComponent<Collider2D>();
            Vector3 c = col != null && col.enabled ? col.bounds.center : b.transform.position;
            float r = col != null && col.enabled ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.y) * 0.85f : 1.5f;
            c.z = 0f;
            bossBodies.Add((c, r));
        }
        projs.Clear();
        foreach (HostileProjectile h in HostileProjectile.Live)
        {
            if (h == null || !h.isActiveAndEnabled) continue;
            Vector2 hp = h.transform.position;
            if ((hp - (Vector2)me).sqrMagnitude > 22f * 22f) continue;
            if (!Noticed(((long)h.GetInstanceID() << 20) ^ h.Serial)) continue;
            projs.Add(new Proj { p = hp, v = h.dir.normalized * h.speed, r = h.radius });
        }
        // 예고 · 바닥 위험 · 결계는 0.1초마다 찾음 (FindObjectsByType 이 비쌈)
        if (Time.time >= nextSlowScan)
        {
            nextSlowScan = Time.time + Mathf.Max(0.1f, Time.deltaTime * 3.5f);
            telegraphs = FindObjectsByType<Telegraph>(FindObjectsSortMode.None);
            hazards = FindObjectsByType<HazardZone>(FindObjectsSortMode.None);
            domainOn = false;
            if (BossUltimate.Active)
            {
                BossUltimate bu = FindFirstObjectByType<BossUltimate>();
                if (bu != null) { domainOn = true; domainC = Get(bu, "center", Vector3.zero); domainR = Get(bu, "radiusNow", 0f); if (domainR <= 0.1f) domainOn = false; }
            }
            ScanTerrain(me);
            ScanSpecial(me);
        }
        warns.Clear();
        foreach (Telegraph t in telegraphs)
        {
            if (t == null || !t.isActiveAndEnabled) continue;
            if (!Noticed(((long)t.GetInstanceID() << 20) ^ t.UseId)) continue;
            bool circle = Get(t, "circle", false);
            float dur = Mathf.Max(0.01f, Get(t, "duration", 1f));
            float fill = Mathf.Clamp01(Get(t, "t", 0f) / dur);
            if (circle) warns.Add(new Warn { circle = true, a = t.transform.position, r = Get(t, "radius", 1f), fill = fill });
            else warns.Add(new Warn { circle = false, a = Get(t, "a", Vector3.zero), b = Get(t, "b", Vector3.zero), r = Get(t, "width", 1f) * 0.5f, fill = fill });
        }
        zones.Clear();
        foreach (HazardZone z in hazards)
        {
            if (z == null || !z.isActiveAndEnabled) continue;
            zones.Add(new Zone { c = z.transform.position, r = z.radius, w = 10f });
        }
        AddTerrainZones();
        foreach (var b in beams) warns.Add(new Warn { circle = false, a = b.a, b = b.b, r = 1.3f, fill = 1f });
    }

    // ================================================================= Telegraph 로 안 잡히는 패턴
    // 리치 왕 결계 회전 광선 (LineRenderer "LichBeamWarn" · 선분 0.85 안이면 맞음) → 위험 직선
    // 엄마 「불 끄고 자!」 확인 단계 (플레이어 머리 위 「…」 · 띠 4.5 안에서 움직이면 맞음) → 멈춰 섬
    readonly List<(Vector2 a, Vector2 b)> beams = new List<(Vector2, Vector2)>();
    bool lightsOut;
    void ScanSpecial(Vector3 me)
    {
        beams.Clear();
        if (BossUltimate.Active)
            foreach (LineRenderer lr in FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
                if (lr.name == "LichBeamWarn" && lr.enabled && lr.gameObject.activeInHierarchy && lr.positionCount >= 2)
                    beams.Add((lr.GetPosition(0), lr.GetPosition(1)));
        lightsOut = false;
        bool mom = false;
        foreach (bosss b in bosss.Live) if (b != null && !b.IsDead && b.developerMom) mom = true;
        if (mom)
            foreach (TMPro.TextMeshPro t in FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None))
                if (t.isActiveAndEnabled && t.text == "…" && ((Vector2)(t.transform.position - me)).magnitude < 6f && Noticed(((long)t.GetInstanceID() << 20) | 3))
                { lightsOut = true; break; }
    }

    // ================================================================= 속도 재기 (게임이 입력을 정규화하고 속도는 영웅 · 카드마다 다름)
    Vector3 lastPos;
    float speedEst = 7f;
    bool hadPos;
    void MeasureSpeed(Vector3 me)
    {
        if (hadPos && Time.deltaTime > 0f && GameInput.AutoMove.sqrMagnitude > 0.5f)
        {
            float v = ((Vector2)(me - lastPos)).magnitude / Time.deltaTime;
            if (v < 60f) speedEst = Mathf.Lerp(speedEst, Mathf.Max(2f, v), 0.05f);
        }
        lastPos = me; hadPos = true;
    }

    // ================================================================= 표적
    bool targetBoss;
    Vector3 targetPos;
    bool hasTarget;
    float nextTargetPick, aimErr;
    int nearCount;              // 8칸 안 잡몹 수
    float nearestMob = 99f;

    bool Melee => hero == CharacterId.Swordsman || hero == CharacterId.Ghost || (hero == CharacterId.Gunner && Paths.Has(GunnerPaths.Shotgun));
    float AttackRange => hero switch
    {
        CharacterId.Swordsman => Paths.Has(SwordsmanPaths.Wave) ? 7f : 4.6f,
        CharacterId.Ghost => 4.3f,
        CharacterId.Alchemist => 9.5f,
        CharacterId.Cartographer => 8.5f,
        CharacterId.Clockmaker => 14f,
        CharacterId.Postman => Paths.Has(PostmanPaths.LongHaul) ? 18f : 11f,
        CharacterId.Gunner when Paths.Has(GunnerPaths.Shotgun) => 5.5f,
        _ => 16f,
    };

    void ChooseTarget(Vector3 me)
    {
        nearCount = 0; nearestMob = 99f;
        Vector3 nearest = me;
        foreach (Vector3 m in mobs)
        {
            float d = ((Vector2)(m - me)).magnitude;
            if (d < 8f) nearCount++;
            if (d < nearestMob) { nearestMob = d; nearest = m; }
        }
        if (Time.time < nextTargetPick && hasTarget) return;
        nextTargetPick = Time.time + Mathf.Lerp(0.45f, 0.2f, skill);
        aimErr = Gauss() * aimErrDeg;
        hasTarget = false; targetBoss = false;
        // 보스 우선 (빈틈이 열려 있으면 빈틈을) — 잡몹이 바짝 붙었으면 그놈 먼저
        float bestB = 1e9f;
        foreach (var b in bossBodies)
        {
            float d = ((Vector2)(b.c - me)).sqrMagnitude;
            if (d < bestB && d < 34f * 34f) { bestB = d; targetPos = b.c; hasTarget = true; targetBoss = true; }
        }
        if (targetBoss)
        {
            foreach (WeakSpot w in WeakSpot.Active)
            {
                if (w == null || !w.Open || w.IsMid) continue;
                Vector2 c = w.Center(rnd.Next(w.Count));
                if (((Vector2)targetPos - c).sqrMagnitude < 12f * 12f) { targetPos = c; break; }
            }
            if (nearestMob < (Melee ? 2.2f : 3f)) { targetPos = nearest; targetBoss = false; }
            return;
        }
        if (mobs.Count == 0) return;
        hasTarget = true;
        if (Melee || hero == CharacterId.Gunner || hero == CharacterId.Rogue || nearestMob < 4f) { targetPos = nearest; return; }
        // 범위 공격 영웅: 사거리 안에서 가장 몰린 곳
        float range = AttackRange, best = -1e9f;
        targetPos = nearest;
        int step = Mathf.Max(1, mobs.Count / 60);
        for (int i = 0; i < mobs.Count; i += step)
        {
            Vector3 a = mobs[i];
            float d = ((Vector2)(a - me)).magnitude;
            if (d > range) continue;
            int n = 0;
            for (int j = 0; j < mobs.Count; j += step) if (((Vector2)(mobs[j] - a)).sqrMagnitude < 6.25f) n++;
            float s = n - d * 0.15f;
            if (s > best) { best = s; targetPos = a; }
        }
    }

    // ================================================================= 이동 (방향 17개 점수)
    float nextDecide, orbitDir = 1f, nextFlip;
    Vector2 chosen;
    static readonly Vector2[] Dirs = MakeDirs();
    static Vector2[] MakeDirs() { var d = new Vector2[17]; for (int i = 0; i < 16; i++) { float a = i * Mathf.PI / 8f; d[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); } d[16] = Vector2.zero; return d; }
    float lastDanger;

    bool MustKeepMoving => (hero == CharacterId.Postman && Paths.Has(PostmanPaths.Bike)) || (hero == CharacterId.Ghost && Paths.Has(GhostPaths.Phase))
                         || (hero == CharacterId.Swordsman && Paths.Has(SwordsmanPaths.Dance)) || (hero == CharacterId.Cartographer && Paths.Has(CartographerPaths.March));
    bool LikesStanding => hero == CharacterId.Gunner && Paths.Has(GunnerPaths.Snipe);

    void Move(PlayerController p, Vector3 me)
    {
        if (lightsOut) { chosen = Vector2.zero; GameInput.AutoMove = Vector2.zero; return; }
        if (Time.time < nextDecide) { GameInput.AutoMove = chosen; return; }
        nextDecide = Time.time + decideEvery * Mathf.Clamp(1f + Gauss() * 0.2f, 0.6f, 1.5f);
        if (Time.time >= nextFlip) { nextFlip = Time.time + 4f + R01() * 5f; if (R01() < 0.5f) orbitDir = -orbitDir; }

        Vector2 m2 = me;
        Vector2 toT = hasTarget ? (Vector2)targetPos - m2 : Vector2.zero;
        float dT = toT.magnitude;
        Vector2 toTn = dT > 0.01f ? toT / dT : Vector2.zero;
        Vector2 side = new Vector2(-toTn.y, toTn.x) * orbitDir;
        float near, far;
        if (Melee) { near = targetBoss ? 2.5f : 1.6f; far = AttackRange * 0.85f; }
        else { near = targetBoss ? 7f : 5.5f; far = Mathf.Min(AttackRange * 0.85f, targetBoss ? 11f : 9f); }
        Vector2 pick = PickupGoal(m2, p);

        float best = -1e9f; int bi = 16; float bestDanger = 0f;
        for (int i = 0; i < Dirs.Length; i++)
        {
            Vector2 d = Dirs[i];
            float danger = 0f;
            foreach (float t in new[] { 0.15f, 0.35f, 0.6f })
            {
                Vector2 q = m2 + d * speedEst * t;
                danger += Danger(q, t, m2) * (t < 0.2f ? 1.3f : t < 0.4f ? 1f : 0.6f);
            }
            float score = -danger;
            if (hasTarget)
            {
                Vector2 q = m2 + d * speedEst * 0.35f;
                float dq = ((Vector2)targetPos - q).magnitude;
                score -= dq < near ? (near - dq) * 0.5f : dq > far ? (dq - far) * 0.35f : 0f;
                score += Vector2.Dot(d, side) * 0.45f;
            }
            if (!Melee && mobInfo.Count > 0)
            {
                // 원거리: 가장 가까운 적과 near 이상 떨어지기 (지금 속도로 다가오는 것 포함)
                Vector2 q = m2 + d * speedEst * 0.35f;
                float minD = 99f;
                foreach (MobInfo m in mobInfo) { float dd = (q - (m.p + m.v * 0.35f)).magnitude - m.r; if (dd < minD) minD = dd; }
                if (minD < near) score -= (near - minD) * 0.6f;
            }
            if (pick != Vector2.zero) score += Vector2.Dot(d, pick) * pickWeight;
            if (i == 16) score += MustKeepMoving ? -1.5f : LikesStanding && lastDanger < 0.3f && nearestMob > 6f ? 0.8f : -0.25f;
            score += Gauss() * scoreNoise * 0.3f;
            if (score > best) { best = score; bi = i; bestDanger = danger; }
        }
        lastDanger = bestDanger;
        chosen = Dirs[bi];
        GameInput.AutoMove = chosen;
    }

    // 한 자리의 위험 점수 (t: 그 자리에 닿는 시각)
    float Danger(Vector2 q, float t, Vector2 me)
    {
        float s = 0f;
        // 잡몹 몸 (닿으면 피해) — 근접 영웅은 덜 꺼림
        // 몸이 닿는 거리 = 적 반지름 + 내 반지름, 그 바깥 여유(근접은 작게) 안이면 점점 위험 · 적은 지금 속도로 t 초 움직인 자리
        float margin = Melee ? 0.9f : 2.2f;
        foreach (MobInfo m in mobInfo)
        {
            Vector2 mp = m.p + m.v * Mathf.Min(t, 0.45f);
            float touch = m.r + playerR;
            float dd = (q - mp).magnitude;
            if (dd < touch + margin)
            {
                float k = 1f - Mathf.Max(0f, dd - touch) / margin;
                s += dd < touch ? 6f : k * k * 2.5f;
            }
        }
        foreach (var b in bossBodies)
        {
            float dd = (q - (Vector2)b.c).magnitude - b.r;
            if (dd < 2.5f) s += (2.5f - dd) * 3f;
        }
        // 탄: t 시각 근처(앞뒤) 탄이 지나는 선분과의 거리
        foreach (Proj pr in projs)
        {
            Vector2 a = pr.p + pr.v * Mathf.Max(0f, t - 0.12f), b = pr.p + pr.v * (t + 0.08f);
            float dd = Hostile.DistanceToSegment(q, a, b);
            float lim = pr.r + 0.55f;
            if (dd < lim + 0.6f) s += dd < lim ? 12f : (lim + 0.6f - dd) * 6f;
        }
        // 예고 (다 찰수록 위험)
        foreach (Warn w in warns)
        {
            float dd = w.circle ? (q - w.a).magnitude - w.r : Hostile.DistanceToSegment(q, w.a, w.b) - w.r;
            if (dd < 0.7f) s += (6f + 14f * w.fill) * (dd < 0f ? 1f : 0.5f);
        }
        foreach (Zone z in zones)
        {
            float dd = (q - z.c).magnitude - z.r;
            if (dd < 0.5f) s += z.w * (dd < 0f ? 1f : 0.4f);
        }
        // 결계 안쪽 유지
        if (domainOn)
        {
            float lim = Mathf.Max(domainR * 0.6f, domainR - 5f);
            float out_ = (q - domainC).magnitude - lim;
            if (out_ > 0f) s += out_ * 2.5f;
        }
        // 맵 끝 · 벽
        Vector3 c = Hostile.ClampArena(q);
        float edge = ((Vector2)c - q).magnitude;
        if (edge > 0f) s += 8f + edge * 4f;
        else if (EdgeDist(q) < 3f) s += (3f - EdgeDist(q)) * 1.2f;
        if (t < 0.2f && Hostile.IsWall(q)) s += 25f;
        return s;
    }

    EnemySpawner spawner;
    float EdgeDist(Vector2 q)
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null) return 99f;
        Vector2 lo = spawner.spawnAreaMin, hi = spawner.spawnAreaMax + new Vector2(0f, 1.5f);
        return Mathf.Min(Mathf.Min(q.x - lo.x, hi.x - q.x), Mathf.Min(q.y - lo.y, hi.y - q.y));
    }

    // ================================================================= 조준
    Vector3 aimPoint;
    void Aim(Vector3 me)
    {
        Vector3 at = hasTarget ? targetPos : me + (Vector3)(chosen.sqrMagnitude > 0f ? chosen : Vector2.right) * 5f;
        Vector2 off = at - me;
        off = Quaternion.Euler(0f, 0f, aimErr) * off;
        // 투척 · 획 영웅은 최대 사거리 안으로
        float maxR = hero == CharacterId.Alchemist ? 9.8f : hero == CharacterId.Cartographer ? 8.8f : 99f;
        if (off.magnitude > maxR) off = off.normalized * maxR;
        aimPoint = me + (Vector3)off;
        GameInput.AutoAim = aimPoint;
    }

    // ================================================================= 평타
    bool firing;
    float restUntil, drawFullAt = -1f, releaseAfter, rehold;
    void Press() { if (!firing) { firing = true; GameInput.AutoFire = true; GameInput.FireDownFrame = Time.frameCount + 1; } }
    void Lift() { if (firing) { firing = false; GameInput.AutoFire = false; GameInput.FireUpFrame = Time.frameCount + 1; } }

    void Fire(PlayerController p, Vector3 me)
    {
        bool want = hasTarget && (((Vector2)(targetPos - me)).magnitude <= AttackRange * 1.15f || targetBoss);
        if (!hasTarget) want = false;
        if (Time.time < restUntil) want = false;
        CharacterKit kit = CharacterKit.Instance;
        if (ultHolding && kit != null && (hero == CharacterId.Swordsman || hero == CharacterId.Alchemist)) want = false;
        // 연금술사: 불안정이 높으면 잠깐 쉼 (자폭 피하기 · 실력이 낮으면 덜 신경 씀)
        if (hero == CharacterId.Alchemist && kit != null && kit.Instability > Mathf.Lerp(97f, 78f, skill)) { restUntil = Time.time + Mathf.Lerp(0.6f, 1.4f, skill); want = false; }
        // 과열 거너: 과열 직전이면 쉼
        if (hero == CharacterId.Gunner && Paths.Has(GunnerPaths.Overheat))
        {
            GunnerPaths g = GunnerPaths.Live;
            if (g != null && g.HeatShotsLeft <= 1 && R01() < 0.6f + 0.4f * skill) { restUntil = Time.time + 0.9f; want = false; }
        }
        // 궁수: 가득 당긴 뒤 「완벽」 창(0.5초) 안에 놓기 — 실력이 낮으면 늦거나 이르게
        if (hero == CharacterId.Archer && kit != null && !Paths.Has(ArcherPaths.Rapid))
        {
            // 숨이 다하면(Breathless) 놓고 숨을 고름
            if (kit.Breathless) want = false;
            if (!want) { Lift(); drawFullAt = -1f; return; }
            if (Time.time < rehold) return;
            if (!firing) { Press(); drawFullAt = -1f; return; }
            if (kit.Draw >= 1f && drawFullAt < 0f)
            {
                drawFullAt = Time.time;
                releaseAfter = Mathf.Max(0f, 0.22f + Gauss() * Mathf.Lerp(0.35f, 0.06f, skill));
            }
            if (drawFullAt >= 0f && Time.time - drawFullAt >= releaseAfter) { Lift(); drawFullAt = -1f; rehold = Time.time + 0.03f; }
            return;
        }
        if (want) Press(); else Lift();
    }

    // ================================================================= 필살기
    bool ultHolding;
    float ultStart, ultHoldFor, fullSince = -1f, lastParry = -9f;
    void UltDown() { GameInput.AutoUlt = true; GameInput.UltDownFrame = Time.frameCount + 1; ultHolding = true; ultStart = Time.time; ultStartUnscaled = Time.unscaledTime; }
    void UltUp() { GameInput.AutoUlt = false; GameInput.UltUpFrame = Time.frameCount + 1; ultHolding = false; }
    float ultStartUnscaled;

    void Ult(PlayerController p, Vector3 me)
    {
        SkillGauge g = p.skillGauge != null ? p.skillGauge : FindFirstObjectByType<SkillGauge>();
        bool full = g != null && g.IsFull();
        if (ultHolding)
        {
            float held = hero == CharacterId.Gunner ? Time.unscaledTime - ultStartUnscaled : Time.time - ultStart;
            bool done = held >= ultHoldFor;
            if (hero == CharacterId.Gunner && p.AimedCount >= Mathf.Min(6, Mathf.Max(1, nearCount)) && held > 0.5f) done = true;
            if (done) UltUp();
            return;
        }
        // 검사 받아치기: 탄 · 보스 공격이 곧 닿으면 짧게 눌러 막기 (게이지가 가득이면 누른 채 회전 베기까지 모음)
        if (hero == CharacterId.Swordsman && Paths.Has(SwordsmanPaths.Parry) && Time.time - lastParry > 1.25f && ThreatSoon(me, 0.22f) && R01() < 0.5f + 0.5f * skill)
        {
            lastParry = Time.time;
            UltDown();
            ultHoldFor = full ? 1.95f : 0.05f;
            return;
        }
        if (!full) { fullSince = -1f; return; }
        if (fullSince < 0f) fullSince = Time.time;
        bool bossNear = false;
        foreach (var b in bossBodies) if (((Vector2)(b.c - me)).magnitude < 16f) bossNear = true;
        bool good = bossNear || nearCount >= 10 || Time.time - fullSince > 10f;
        // 시계공 되감기: 최근 3초에 크게 맞았거나 위험할 때
        if (hero == CharacterId.Clockmaker)
        {
            good = HpLost(3f) > p.PlayerMaxHealth * 0.18f || p.PlayerHealth < p.PlayerMaxHealth * 0.35f || (lastDanger > 6f && Time.time - fullSince > 4f) || Time.time - fullSince > 25f;
        }
        if (hero == CharacterId.Ghost && !hasTarget) good = false;
        if (hero == CharacterId.Gunner && p.IsReloading) good = false;
        if (!good) return;
        // 사람이 바로 누르지는 않음
        if (Time.time - fullSince < reaction) return;
        ultHoldFor = hero switch
        {
            CharacterId.Gunner => 2.2f,
            CharacterId.Swordsman => 1.5f,
            CharacterId.Alchemist => 1.5f,
            CharacterId.Cartographer => 1.2f,
            CharacterId.Postman => Paths.Has(PostmanPaths.Express) ? 0.6f : 1.2f,
            CharacterId.Rogue => 0.12f,
            CharacterId.Archer => 0.15f,
            CharacterId.Ghost => 0.15f,
            CharacterId.Clockmaker => 0.05f,
            _ => 0.5f,
        };
        UltDown();
    }

    // 탄 · 예고가 within 초 안에 몸에 닿을까
    bool ThreatSoon(Vector3 me, float within)
    {
        Vector2 m = me;
        foreach (Proj pr in projs)
        {
            Vector2 a = pr.p, b = pr.p + pr.v * within;
            if (Hostile.DistanceToSegment(m, a, b) < pr.r + 0.6f) return true;
        }
        foreach (Warn w in warns)
        {
            if (w.fill < 0.8f) continue;
            float dd = w.circle ? (m - w.a).magnitude - w.r : Hostile.DistanceToSegment(m, w.a, w.b) - w.r;
            if (dd < 0.3f) return true;
        }
        return false;
    }

    // ================================================================= 체력 기록 (시계공 되감기 판단)
    readonly Queue<(float t, float hp)> hpLog = new Queue<(float, float)>();
    void NoteHp(PlayerController p)
    {
        hpLog.Enqueue((Time.time, p.PlayerHealth));
        while (hpLog.Count > 0 && hpLog.Peek().t < Time.time - 3.5f) hpLog.Dequeue();
    }
    float HpLost(float secs)
    {
        float max = 0f, now = Hostile.Player != null ? Hostile.Player.PlayerHealth : 0f;
        foreach (var h in hpLog) if (h.t >= Time.time - secs) max = Mathf.Max(max, h.hp);
        return Mathf.Max(0f, max - now);
    }
}
