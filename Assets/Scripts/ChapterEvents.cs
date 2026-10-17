using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// 장 중반 이벤트 (1.0.1): 판 기록에서 중간에 그만둔 판이 많아(12판 중 5판), 장 한가운데에 굴곡을 넣음
//   처치 수 30%: 「습격」 — 둘레에 적 무리가 한꺼번에 나타나고, 모두 쓰러뜨리면 보물 상자
//   처치 수 55%: 1장에도 엘리트(중간 보스) 하나 — 2장부터는 원래 페이즈마다 중간 보스가 나옴
//   중간 보스 · 엘리트를 쓰러뜨리면 보물 상자 (TreasureChest)
// 1.1~: 고정 30% · 55% 대신 ContentRegistry.Events 에서 장마다 무작위로 뽑음
//   처치 20~35% · 45~60% · (3 · 4장만) 70~80% 구간에 하나씩 · 같은 이벤트는 한 판에 최대 2번 · 시작 3초 전에 배너와 화살표
//   습격 · 엘리트 · 황금 고블린 · 저주받은 상자(2장~) · 영혼 제단 지키기 · 정예 무리
//   시드 · 이번 장 계획 · 쓴 이벤트는 RunSave 에 기록 (RunContent) · 옛 저장에서 이어하면 30% · 55% 고정 그대로
// 무한 모드 · 튜토리얼 · 자동 촬영에서는 쉼 (1.0.3: 체험판도 정식판처럼 켬). StageManager 가 붙임
public class ChapterEvents : MonoBehaviour
{
    // 1.0.1: 둘레 8 → 40칸 (훨씬 넓게 둘러쌈) · 1.0.4~ 40 → 30칸 (화면 바로 밖, 40칸은 너무 멀어 오기까지 한참 걸렸음)
    const float AmbushTimeout = 50f, AmbushRadius = 30f;
    const float LegacyAmbushAt = 0.3f, LegacyEliteAt = 0.55f;      // 옛 저장 (1.0.x) 고정 시점
    // 1.1~ 이벤트 풀
    const float WarnSeconds = 3f;
    const float GoblinSeconds = 20f, CursedWaitSeconds = 40f, CursedSeconds = 45f, PackSeconds = 60f;
    const float AltarRadius = 4.5f, AltarNeed = 12f, AltarSeconds = 50f, AltarWaveEvery = 6f;
    const int AltarWaves = 4;
    static readonly Vector2[] Windows = { new Vector2(0.2f, 0.35f), new Vector2(0.45f, 0.6f), new Vector2(0.7f, 0.8f) };
    static readonly Color WarnColor = new Color(1f, 0.82f, 0.4f);
    static readonly Color CursedColor = new Color(0.75f, 0.45f, 1f);
    static readonly Color PackColor = new Color(1f, 0.5f, 0.4f);
    static readonly Color AltarColor = new Color(0.55f, 0.9f, 1f);

    // 이번 장에 나올 이벤트 하나
    // savedDone: 이어하기로 불러온 계획의 「실제로 열었는지」 기록 (-1 = 옛 저장이라 모름 · 0 = 안 열림 · 1 = 열림)
    public class Planned { public EventDef def; public float at; public bool done; public int savedDone = -1; }

    EnemySpawner sp;
    int stage = -1;
    List<GameObject> ambush;
    float ambushUntil;
    readonly List<TreasureChest> chests = new List<TreasureChest>();

    // 이어하기 (RunSave.Restore): 진행 중이던 습격 · 살아 있던 1장 엘리트는 다시, 안 연 보물 상자는 옆에 다시 (1.0.4~)
    static bool resumeElite;
    static int resumeAmbush, resumeChests;
    public static void Resume(int ambushLeft, bool eliteAlive, int chestCount)
    {
        resumeAmbush = Mathf.Max(0, ambushLeft);
        resumeElite = eliteAlive;
        resumeChests = Mathf.Clamp(chestCount, 0, 5);
    }

    // 판 중간 저장 (RunSave.Save): 남은 습격 적 수
    public int AmbushLeft { get { if (ambush == null) return 0; int n = 0; foreach (GameObject g in ambush) if (g != null) n++; return n; } }
    public int UnopenedChests { get { int n = 0; foreach (TreasureChest c in chests) if (c != null) n++; return n; } }

    static bool Off => GameMode.IsEndless || TutorialRun.Active || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode;

    // ================================================================= 판 단위 계획 (1.1~)
    static int seed;                                        // 0 = 옛 저장 (30% · 55% 고정)
    static readonly List<string> used = new List<string>(); // 이번 판에 계획한 이벤트 (saveCode, 장마다 쌓임)
    static readonly List<Planned> plan = new List<Planned>();
    static int planStage = -1;
    static int resumeActive = -1;                           // 이어하기: 진행 중이던 이벤트를 처음부터 다시 (계획 번호)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (s.name == "GameScene") NewRun(); };
        // 에디터에서 GameScene 을 바로 재생하면 첫 sceneLoaded 가 Boot 보다 먼저 지나가 seed 가 0(옛 고정 방식)으로 남으므로 여기서 한 번
        if (seed == 0 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene") NewRun();
    }

    static void NewRun()
    {
        seed = Random.Range(1, int.MaxValue);
        used.Clear();
        plan.Clear();
        planStage = -1;
        resumeActive = -1;
    }

    public static void SaveRun(RunSave.Data d)
    {
        d.eventSeed = seed;
        d.eventStage = planStage;
        d.eventPlan = new List<string>();
        // 「코드@처치 비율」 뒤에 실제로 열었으면 '!' · 아니면 '?' (처치 비율이 지났어도 앞 이벤트가 도는 동안 못 연 이벤트를 이어하기에서 건너뛰지 않게)
        foreach (Planned p in plan) d.eventPlan.Add(p.def.saveCode + "@" + p.at.ToString("0.####", CultureInfo.InvariantCulture) + (p.done ? "!" : "?"));
        d.eventUsed = new List<string>(used);
        ChapterEvents ev = FindFirstObjectByType<ChapterEvents>();
        d.eventActive = ev != null && planStage == d.stage ? ev.RestartIndex + 1 : 0;
    }

    public static void RestoreRun(RunSave.Data d)
    {
        seed = d.eventSeed;
        used.Clear();
        if (d.eventUsed != null) used.AddRange(d.eventUsed);
        plan.Clear();
        planStage = -1;
        resumeActive = -1;
        // 이 장 계획이 저장돼 있으면 그대로 (보스를 잡고 문에 들어가기 전에 나갔으면 다음 장은 새로 뽑음)
        if (seed != 0 && d.eventPlan != null && d.eventStage == d.stage)
        {
            foreach (string s in d.eventPlan)
            {
                string[] w = s.Split('@');
                EventDef def = ContentRegistry.EventByCode(w[0]);
                if (def == null || w.Length < 2) continue;
                string num = w[1];
                int savedDone = -1;
                if (num.EndsWith("!")) { savedDone = 1; num = num.Substring(0, num.Length - 1); }
                else if (num.EndsWith("?")) { savedDone = 0; num = num.Substring(0, num.Length - 1); }
                if (!float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out float at)) continue;
                plan.Add(new Planned { def = def, at = at, savedDone = savedDone });
            }
            planStage = d.stage;
            resumeActive = d.eventActive - 1;
        }
    }

    static int UsedCount(string code)
    {
        int n = 0;
        foreach (string u in used) if (u == code) n++;
        return n;
    }

    static void GeneratePlan(int forStage)
    {
        plan.Clear();
        planStage = forStage;
        int slot = Chapters.SlotOf(forStage);
        if (seed == 0)
        {
            // 옛 저장: 30% 습격 · 55% 1장 엘리트 (1.0.x 그대로)
            plan.Add(new Planned { def = ContentRegistry.EventById(ContentRegistry.Raid), at = LegacyAmbushAt });
            if (slot == 0) plan.Add(new Planned { def = ContentRegistry.EventById(ContentRegistry.Elite), at = LegacyEliteAt });
            return;
        }
        System.Random rng = new System.Random(unchecked(seed + 7919 * (slot + 1)));
        int windows = slot >= 2 ? 3 : 2;
        List<EventDef> open = new List<EventDef>();
        for (int w = 0; w < windows; w++)
        {
            open.Clear();
            float total = 0f;
            foreach (EventDef e in ContentRegistry.Events)
            {
                if (slot < e.minSlot || UsedCount(e.saveCode) >= e.maxPerRun || plan.Exists(p => p.def == e)) continue;
                open.Add(e);
                total += e.weight;
            }
            if (open.Count == 0 || total <= 0f) continue;
            float roll = (float)rng.NextDouble() * total;
            EventDef pick = open[open.Count - 1];
            foreach (EventDef e in open)
            {
                roll -= e.weight;
                if (roll < 0f) { pick = e; break; }
            }
            Vector2 win = Windows[w];
            plan.Add(new Planned { def = pick, at = win.x + (float)rng.NextDouble() * (win.y - win.x) });
            used.Add(pick.saveCode);
        }
    }

    // ================================================================= 진행 중인 이벤트
    Planned current;
    int currentIndex = -1;
    bool started;
    float startAt;
    Vector3 where;
    bool hasWhere;
    string labelText;

    // 최적화: 진행 줄 글자는 표시 숫자(남은 적 · 초 · %) · 언어가 바뀔 때만 다시 조립 (예전엔 매 프레임 Format · Replace)
    string lblKey, lblCache;
    int lblA = int.MinValue, lblB;
    Loc.Lang lblLang;
    string CachedLabel(string key, int a, int b) => key == lblKey && a == lblA && b == lblB && Loc.Current == lblLang ? lblCache : null;
    string StoreLabel(string key, int a, int b, string text)
    {
        lblKey = key; lblA = a; lblB = b; lblLang = Loc.Current; lblCache = text;
        return text;
    }
    float markerAt;

    List<GameObject> group;             // 정예 무리 · 저주받은 적
    float groupUntil;
    GameObject goblin;
    float goblinUntil;
    bool goblinCaught;
    Vector3 goblinAt;
    TreasureChest cursedChest;
    bool cursedOpened;
    float cursedUntil;
    GameObject altar;
    LineRenderer altarRing, arrow, arrowShade;     // arrowShade: 1.1.2~ 화살표 뒤 진한 그림자 (금빛 물체 · 이펙트 위에서도 보이게)
    float altarProgress, altarUntil, altarWaveAt;
    int altarWaves;

    // 저장할 때 처음부터 다시 할 이벤트 (예고 중이거나, 습격 · 엘리트가 아닌 진행 중 이벤트 — 습격 · 엘리트는 원래대로 남은 수 · 살아 있음으로 이어짐)
    public int RestartIndex
    {
        get
        {
            if (current == null || currentIndex < 0) return -1;
            if (!started) return currentIndex;
            return current.def.id == ContentRegistry.Raid || current.def.id == ContentRegistry.Elite ? -1 : currentIndex;
        }
    }

    void Start()
    {
        sp = FindFirstObjectByType<EnemySpawner>();
        if (sp != null) sp.onMidBossDefeated += OnMidBossDefeated;
    }

    void OnDestroy()
    {
        if (sp != null) sp.onMidBossDefeated -= OnMidBossDefeated;
        Finish();
        if (arrow != null) Destroy(arrow.gameObject);
        if (arrowShade != null) Destroy(arrowShade.gameObject);
    }

    // 1.1~ 중간 보스 · 엘리트 상자는 정예 상자 (전설 15%)
    void OnMidBossDefeated()
    {
        if (Off) return;
        PlayerController p = Hostile.Player;
        if (p != null) chests.Add(TreasureChest.Spawn(p.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 2.5f), true));
    }

    // 1.0.4~ 습격 중 화면 위에 「남은 적 n · 남은 시간」 (알림판 아래) · 1.1~ 모든 이벤트 진행 표시
    // 1.1.2~ 안내 메시지 개편: 맨 글자 24pt → 어두운 알약 바탕 · 강조색 막대 · 28pt · 숫자 색 강조 (Notice.Objective, 창이 열리면 숨김)
    void UpdateLabel(string text) => Notice.Objective(text, current != null && !started ? WarnColor : Notice.Gold);

    void OnDisable()
    {
        UpdateLabel(null);
        ShowArrow(false, Vector3.zero, Vector3.zero, WarnColor);
    }

    static int SecondsLeft(float until) => Mathf.Max(0, Mathf.CeilToInt(until - Time.time));

    static int Alive(List<GameObject> list)
    {
        if (list == null) return 0;
        list.RemoveAll(g => g == null || (g.TryGetComponent(out EnermyController e) && e.IsDead));
        return list.Count;
    }

    void Update()
    {
        StageManager sm = StageManager.Instance;
        if (Off || sp == null || sm == null || !sm.Started) { UpdateLabel(null); ShowArrow(false, Vector3.zero, Vector3.zero, WarnColor); return; }

        // 장이 바뀌면 새로 (지난 장의 안 연 상자는 새 장으로 옮김, 1.0.4~)
        if (sm.CurrentStage != stage) OnStageChanged(sm);

        if (sp.bossSpawned || sp.bossCleared)
        {
            // 보스가 나오면 진행 중이던 이벤트는 보상 없이 끝 (예전 습격과 같음)
            if (current != null) Finish(true);
            UpdateLabel(null);
            ShowArrow(false, Vector3.zero, Vector3.zero, WarnColor);
            return;
        }
        if (Time.timeScale == 0f) return;

        PlayerController p = Hostile.Player;
        if (p == null) return;
        float k = sp.killedEnemy / (float)Mathf.Max(1, sp.BossKillTarget);

        if (current == null)
            for (int i = 0; i < plan.Count; i++)
                if (!plan[i].done && k >= plan[i].at) { Announce(i, p, sm); break; }

        labelText = null;
        if (current != null) Tick(p, sm);
        UpdateLabel(labelText);
    }

    void OnStageChanged(StageManager sm)
    {
        stage = sm.CurrentStage;
        Finish(true);
        if (planStage != stage) GeneratePlan(stage);
        // 이어하기로 장 중간부터 시작하면 이미 지난 이벤트는 다시 하지 않음 (1.0.4~, 예전엔 이어하자마자 습격 · 엘리트가 한꺼번에)
        float at = sp.killedEnemy / (float)Mathf.Max(1, sp.BossKillTarget);
        // 저장에 「열었는지」가 있으면 그대로 (처치 비율은 지났지만 앞 이벤트 때문에 아직 못 연 이벤트는 다시 차례가 옴), 옛 저장은 처치 비율로
        for (int i = 0; i < plan.Count; i++)
        {
            bool opened = plan[i].savedDone >= 0 ? plan[i].savedDone == 1 : plan[i].at <= at;
            plan[i].done = opened && i != resumeActive;
            plan[i].savedDone = -1;
        }
        ambush = null;
        // 1.0.4~: 안 연 보물 상자는 말없이 사라지지 않고 새 장에서 옆에 다시 놓음 (예전엔 문에 들어가면 지워졌음)
        int carried = UnopenedChests;
        foreach (TreasureChest c in chests) if (c != null) Destroy(c.gameObject);
        chests.Clear();
        resumeChests = Mathf.Clamp(resumeChests + carried, 0, 5);
        // 이어하기: 진행 중이던 습격은 남은 수만큼 (배너 없이), 살아 있던 1장 엘리트는 다시, 안 연 상자는 옆에 다시
        PlayerController owner = Hostile.Player;
        if (owner != null)
        {
            if (resumeAmbush > 0)
            {
                ambush = sp.SpawnAmbush(owner.transform.position, resumeAmbush, AmbushRadius);
                ambushUntil = Time.time + AmbushTimeout;
                current = new Planned { def = ContentRegistry.EventById(ContentRegistry.Raid), done = true };
                currentIndex = -1;
                started = true;
            }
            if (resumeElite) sp.SpawnMidBoss();
            for (int i = 0; i < resumeChests; i++)
                chests.Add(TreasureChest.Spawn(owner.transform.position + (Vector3)(Random.insideUnitCircle.normalized * (2.5f + i))));
        }
        resumeAmbush = resumeChests = 0;
        resumeElite = false;
        resumeActive = -1;
    }

    // 플레이어에게서 떨어진 맵 안 자리
    static Vector3 AwayPoint(Vector3 from, float min, float max)
    {
        Vector3 best = Hostile.ClampArena(from);
        float bestDist = -1f;
        for (int i = 0; i < 10; i++)
        {
            Vector3 at = from + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(min, max));
            Vector3 clamped = Hostile.ClampArena(at);
            float d = Vector2.Distance(clamped, from);
            if (Hostile.InArena(at)) return at;
            if (d > bestDist) { bestDist = d; best = clamped; }
        }
        return best;
    }

    // 3초 전 예고: 배너 + 화살표 (자리가 있는 이벤트)
    void Announce(int index, PlayerController p, StageManager sm)
    {
        current = plan[index];
        current.done = true;
        currentIndex = index;
        started = false;
        startAt = Time.time + WarnSeconds;
        markerAt = 0f;
        Vector3 me = p.transform.position;
        switch (current.def.id)
        {
            case ContentRegistry.Goblin: where = AwayPoint(me, 10f, 14f); hasWhere = true; break;
            case ContentRegistry.CursedChest: where = AwayPoint(me, 8f, 11f); hasWhere = true; break;
            case ContentRegistry.Altar: where = AwayPoint(me, 9f, 12f); hasWhere = true; break;
            case ContentRegistry.Pack: where = AwayPoint(me, 15f, 18f); hasWhere = true; break;
            default: where = me; hasWhere = false; break;
        }
        // 1.1.2~ 급한 큰 알림(Urgent): 줄 맨 앞 · 끝까지 보임 (예고 3초) · 긴 설명은 띠가 아래로 늘어남 (Notice)
        Notice.Big(Loc.T("event.soon").Replace("{0}", current.def.Name).Replace("{1}", current.def.Desc), NoticeIcon.Warn, WarnSeconds, NoticeLevel.Urgent);
        Hostile.Play("pulse", 0.6f, 0.8f);
    }

    void Tick(PlayerController p, StageManager sm)
    {
        Vector3 me = p.transform.position;
        int slot = Chapters.SlotOf(stage);
        if (!started)
        {
            labelText = Loc.T("event.soon.label").Replace("{0}", current.def.Name).Replace("{1}", SecondsLeft(startAt).ToString());
            if (hasWhere)
            {
                ShowArrow(true, me, where, WarnColor);
                if (Time.time >= markerAt)
                {
                    markerAt = Time.time + 0.7f;
                    ShockRing.Spawn(where, 0.3f, 2.6f, 0.6f, WarnColor, 0.15f);
                }
            }
            else if (Time.time >= markerAt)
            {
                markerAt = Time.time + 1f;
                ShockRing.Spawn(me, 0.5f, 6f, 0.7f, WarnColor, 0.15f);
            }
            if (Time.time >= startAt) Begin(p, sm, slot);
            return;
        }
        switch (current.def.id)
        {
            case ContentRegistry.Raid: TickRaid(me, sm); break;
            case ContentRegistry.Goblin: TickGoblin(me, sm, slot); break;
            case ContentRegistry.CursedChest: TickCursed(me, sm); break;
            case ContentRegistry.Altar: TickAltar(me, sm, slot); break;
            case ContentRegistry.Pack: TickGroup(me, sm, "event.pack.label", "event.pack.fail", PackColor); break;
            default: Finish(); break;
        }
    }

    void Begin(PlayerController p, StageManager sm, int slot)
    {
        started = true;
        ShowArrow(false, Vector3.zero, Vector3.zero, WarnColor);
        Vector3 me = p.transform.position;
        switch (current.def.id)
        {
            case ContentRegistry.Raid:
                ambush = sp.SpawnAmbush(me, 14 + 3 * slot, AmbushRadius);
                ambushUntil = Time.time + AmbushTimeout;
                Notice.Big(Loc.T("습격!\n모두 쓰러뜨리면 보물 상자가 나타납니다"), NoticeIcon.Warn, 3f, NoticeLevel.Urgent);
                Hostile.Play("roar", 0.6f, 1.1f);
                break;
            case ContentRegistry.Elite:
                sp.SpawnMidBoss();                      // 등장 배너 · 처치 상자는 원래 중간 보스 길 그대로 (정예 상자)
                Finish();
                break;
            case ContentRegistry.Goblin:
                {
                    List<GameObject> one = sp.SpawnAmbush(where, 1, 0.5f);
                    goblin = one.Count > 0 ? one[0] : null;
                    if (goblin == null) { Finish(); break; }
                    goblinCaught = false;
                    goblinUntil = Time.time + GoblinSeconds;
                    if (goblin.TryGetComponent(out EnermyController e))
                    {
                        e.setEnemyHP = Mathf.Max(20, e.setEnemyHP * 6);
                        e.speed = -Mathf.Abs(e.speed) * 1.15f;          // 플레이어에게서 달아남
                        e.contactDamage = 0f;
                        e.survivesContact = true;
                        e.knockBackTaken *= 0.3f;
                        e.skill = EnemySkillType.None;
                        e.baseColor = new Color(1f, 0.85f, 0.25f);
                        e.chanceofHP = 0;
                        e.onKilled += () => { goblinCaught = true; if (goblin != null) goblinAt = goblin.transform.position; };
                    }
                    goblin.transform.localScale *= 1.3f;
                    goblin.AddComponent<GoldenGoblin>();
                    MidBossMark.Attach(goblin, Loc.T("event.goblin.name"), false);     // 1.1~ 「중간 보스 · 」 없이 이름만
                    Notice.Big(Loc.T("event.goblin.start"), NoticeIcon.Coin, 2.5f, NoticeLevel.Urgent);
                    Hostile.Play("chime", 0.8f, 1.5f);
                    break;
                }
            case ContentRegistry.CursedChest:
                cursedOpened = false;
                cursedUntil = Time.time + CursedWaitSeconds;
                cursedChest = TreasureChest.SpawnCursed(where, () => OnCursedOpen(slot));
                Notice.Big(Loc.T("event.cursed.appear"), NoticeIcon.Curse, 2.5f, NoticeLevel.Urgent);
                break;
            case ContentRegistry.Altar:
                altarProgress = 0f;
                altarWaves = 0;
                altarUntil = Time.time + AltarSeconds;
                altarWaveAt = Time.time + 1f;
                altar = SpecialAbilities.MakeSprite("SoulAltar", Hostile.Glow, where, 1.4f, new Color(AltarColor.r, AltarColor.g, AltarColor.b, 0.8f), "Effect", 7);
                altarRing = Hostile.NewLine("SoulAltarRing", new Color(AltarColor.r, AltarColor.g, AltarColor.b, 0.6f), 0.12f, 7);
                altarRing.loop = true;
                Hostile.SetArc(altarRing, where, AltarRadius, 0f, 360f);
                Fx.Spawn("fx_soulburst", where, 4f, AltarColor, 18f);
                Notice.Big(Loc.T("event.altar.start"), NoticeIcon.Soul, 2.5f, NoticeLevel.Urgent);
                Hostile.Play("shimmer", 0.7f, 1f);
                break;
            case ContentRegistry.Pack:
                {
                    int n = 3 + (slot + 1) / 2;
                    group = sp.SpawnAmbush(where, n, 2.5f);
                    foreach (GameObject g in group) Buff(g, 3f, 1.35f, PackColor, Loc.T("event.pack.tag"));
                    groupUntil = Time.time + PackSeconds;
                    Notice.Big(Loc.T("event.pack.start"), NoticeIcon.Warn, 2.5f, NoticeLevel.Urgent);
                    Hostile.Play("roar", 0.6f, 0.9f);
                    break;
                }
            default: Finish(); break;
        }
    }

    // 정예 · 저주받은 적: 단단하고 크고 보상이 많음 (Start 전에 바꿔서 난이도 배율이 그 위에 곱해짐)
    static void Buff(GameObject go, float hp, float scale, Color tint, string tag)
    {
        if (go == null || !go.TryGetComponent(out EnermyController e)) return;
        e.setEnemyHP = Mathf.Max(1, Mathf.RoundToInt(e.setEnemyHP * hp));
        e.expReward *= 3;
        e.coinDrop = Mathf.Max(3, e.coinDrop * 3);
        e.knockBackTaken *= 0.5f;
        e.baseColor = tint;
        e.chanceofHP = 50;
        go.transform.localScale *= scale;
        if (!string.IsNullOrEmpty(tag)) SkillTag.Show(go.transform, tag, 1.8f * scale);
    }

    void TickRaid(Vector3 me, StageManager sm)
    {
        if (ambush == null) { Finish(); return; }
        int left = Alive(ambush);
        int raidSec = SecondsLeft(ambushUntil);
        labelText = CachedLabel("raid", left, raidSec) ?? StoreLabel("raid", left, raidSec, string.Format(Loc.T("습격 · 남은 적 {0} · {1}초"), left, raidSec));
        if (left == 0)
        {
            ambush = null;
            chests.Add(TreasureChest.Spawn(me + (Vector3)(Random.insideUnitCircle.normalized * 2.5f)));
            Finish();
        }
        else if (Time.time > ambushUntil)
        {
            ambush = null;        // 너무 오래 걸리면 (멀리 도망친 적) 상자 없이 끝
            Notice.Toast(NoticeIcon.Warn, Loc.T("습격 실패\n시간 안에 모두 쓰러뜨리지 못해 보물 상자가 없습니다"), 4.5f);     // 1.0.4~ 조용히 끝나지 않게
            Finish();
        }
    }

    void TickGoblin(Vector3 me, StageManager sm, int slot)
    {
        if (goblinCaught)
        {
            int coins = 40 + 20 * slot;
            Coin coin = FindFirstObjectByType<Coin>();
            if (coin != null) coin.AddCoin(coins);
            SoulShards.Add(10 + 5 * slot, goblinAt, true);
            for (int i = 0; i < 6; i++) Fx.Spawn("fx_sparkle", goblinAt + (Vector3)(Random.insideUnitCircle * 1.5f), 1.4f, new Color(1f, 0.85f, 0.3f), 20f);
            Hostile.Play("chime", 0.9f, 1.6f);
            Notice.Toast(NoticeIcon.Coin, Loc.T("event.goblin.caught").Replace("{0}", coins.ToString()), 4f);
            goblin = null;
            Finish();
            return;
        }
        if (goblin == null) { Finish(); return; }
        int goblinSec = SecondsLeft(goblinUntil);
        labelText = CachedLabel("event.goblin.label", goblinSec, 0) ?? StoreLabel("event.goblin.label", goblinSec, 0, Loc.T("event.goblin.label").Replace("{0}", goblinSec.ToString()));
        ShowArrow(true, me, goblin.transform.position, new Color(1f, 0.85f, 0.3f));
        if (Time.time > goblinUntil)
        {
            if (goblin.TryGetComponent(out EnermyController e)) e.Purge();
            goblin = null;
            Notice.Toast(NoticeIcon.Warn, Loc.T("event.goblin.escaped"), 4f);
            Finish();
        }
    }

    void TickCursed(Vector3 me, StageManager sm)
    {
        if (!cursedOpened)
        {
            if (cursedChest == null) { Finish(); return; }
            int cursedSec = SecondsLeft(cursedUntil);
            labelText = CachedLabel("event.cursed.wait", cursedSec, 0) ?? StoreLabel("event.cursed.wait", cursedSec, 0, Loc.T("event.cursed.wait").Replace("{0}", cursedSec.ToString()));
            ShowArrow(true, me, where, CursedColor);
            if (Time.time > cursedUntil)
            {
                Destroy(cursedChest.gameObject);
                cursedChest = null;
                Notice.Toast(NoticeIcon.Curse, Loc.T("event.cursed.gone"), 4f);
                Finish();
            }
            return;
        }
        TickGroup(me, sm, "event.cursed.label", "event.cursed.fail", CursedColor);
    }

    void OnCursedOpen(int slot)
    {
        cursedOpened = true;
        cursedChest = null;
        group = sp != null ? sp.SpawnAmbush(where, 8 + 2 * slot, 9f) : new List<GameObject>();
        foreach (GameObject g in group) Buff(g, 1.6f, 1.15f, CursedColor, null);
        groupUntil = Time.time + CursedSeconds;
        Fx.Spawn("fx_soulburst", where, 5f, CursedColor, 16f);
        ShockRing.Spawn(where, 0.5f, 9f, 0.6f, CursedColor, 0.3f);
        Notice.Big(Loc.T("event.cursed.open"), NoticeIcon.Curse, 2.5f, NoticeLevel.Urgent);
    }

    // 무리를 모두 쓰러뜨리면 정예 상자 (정예 무리 · 저주받은 상자)
    void TickGroup(Vector3 me, StageManager sm, string labelKey, string failKey, Color color)
    {
        if (group == null) { Finish(); return; }
        int left = Alive(group);
        int groupSec = SecondsLeft(groupUntil);
        labelText = CachedLabel(labelKey, left, groupSec) ?? StoreLabel(labelKey, left, groupSec, Loc.T(labelKey).Replace("{0}", left.ToString()).Replace("{1}", groupSec.ToString()));
        if (left == 0)
        {
            chests.Add(TreasureChest.Spawn(me + (Vector3)(Random.insideUnitCircle.normalized * 2.5f), true));
            Finish();
            return;
        }
        // 가장 가까운 남은 적 쪽 화살표 (화면 밖으로 흩어졌을 때)
        Vector3 near = group[0].transform.position;
        float best = float.MaxValue;
        foreach (GameObject g in group)
        {
            float d = (g.transform.position - me).sqrMagnitude;
            if (d < best) { best = d; near = g.transform.position; }
        }
        ShowArrow(best > 12f * 12f, me, near, color);
        if (Time.time > groupUntil)
        {
            Notice.Toast(NoticeIcon.Warn, Loc.T(failKey), 4f);
            Finish();
        }
    }

    void TickAltar(Vector3 me, StageManager sm, int slot)
    {
        bool inside = Vector2.Distance(me, where) <= AltarRadius;
        if (inside) altarProgress += Time.deltaTime / AltarNeed;
        ShowArrow(!inside, me, where, AltarColor);
        if (altarRing != null)
        {
            float a = inside ? 0.75f + 0.25f * Mathf.Sin(Time.time * 8f) : 0.45f;
            altarRing.startColor = altarRing.endColor = new Color(AltarColor.r, AltarColor.g, AltarColor.b, a);
        }
        if (altar != null) altar.transform.localScale = Vector3.one * (1.4f + 1.6f * altarProgress + 0.1f * Mathf.Sin(Time.time * 5f));
        if (inside && Random.value < Time.deltaTime * 6f)
            Fx.Spawn("fx_sparkle", where + (Vector3)(Random.insideUnitCircle * AltarRadius), 1.1f, AltarColor, 20f);
        // 지키는 동안 둘레에서 적이 몰려옴
        if (Time.time >= altarWaveAt && altarWaves < AltarWaves)
        {
            altarWaves++;
            altarWaveAt = Time.time + AltarWaveEvery;
            sp.SpawnAmbush(where, 3 + slot, 13f);
        }
        int altarPct = Mathf.Clamp(Mathf.FloorToInt(altarProgress * 100f), 0, 100), altarSec = SecondsLeft(altarUntil);
        labelText = CachedLabel("event.altar.label", altarPct, altarSec) ?? StoreLabel("event.altar.label", altarPct, altarSec,
            Loc.T("event.altar.label").Replace("{0}", altarPct.ToString()).Replace("{1}", altarSec.ToString()));
        if (altarProgress >= 1f)
        {
            int shards = 25 + 10 * slot;
            SoulShards.Add(shards, where, true);
            chests.Add(TreasureChest.Spawn(where));
            Fx.Spawn("fx_soulburst", where, 6f, AltarColor, 16f);
            Notice.Toast(NoticeIcon.Shards, Loc.T("event.altar.done").Replace("{0}", shards.ToString()), 4f);
            Finish();
        }
        else if (Time.time > altarUntil)
        {
            Notice.Toast(NoticeIcon.Warn, Loc.T("event.altar.fail"), 4f);
            Finish();
        }
    }

    // 이벤트 끝: 연출 정리 (cancel: 보스 등장 · 장 바뀜 — 남은 고블린 · 안 연 저주받은 상자도 치움)
    void Finish(bool cancel = false)
    {
        if (cancel)
        {
            if (goblin != null && goblin.TryGetComponent(out EnermyController e) && !e.IsDead) e.Purge();
            if (cursedChest != null) Destroy(cursedChest.gameObject);
            ambush = null;
        }
        goblin = null;
        cursedChest = null;
        group = null;
        if (altar != null) Destroy(altar);
        if (altarRing != null) Destroy(altarRing.gameObject);
        altar = null;
        altarRing = null;
        current = null;
        currentIndex = -1;
        started = false;
        ShowArrow(false, Vector3.zero, Vector3.zero, WarnColor);
    }

    // 플레이어 둘레에 목표 쪽을 가리키는 꺾쇠 화살표 (가까우면 숨김)
    void ShowArrow(bool show, Vector3 from, Vector3 to, Color color)
    {
        Vector3 d = to - from;
        d.z = 0f;
        if (!show || d.sqrMagnitude < 16f)
        {
            if (arrow != null && arrow.enabled) arrow.enabled = false;
            if (arrowShade != null && arrowShade.enabled) arrowShade.enabled = false;
            return;
        }
        if (arrow == null)
        {
            arrow = Hostile.NewLine("EventArrow", color, 0.26f, 41);
            arrowShade = Hostile.NewLine("EventArrowShade", new Color(0.05f, 0.03f, 0.06f, 0.75f), 0.46f, 40);
            arrowShade.positionCount = 3;
            arrow.positionCount = 3;
        }
        arrow.enabled = true;
        Vector3 dir = d.normalized, side = new Vector3(-dir.y, dir.x, 0f);
        float pulse = 0.25f * Mathf.Sin(Time.time * 8f);
        Vector3 tip = from + dir * (3.2f + pulse);
        arrow.SetPosition(0, tip - dir * 0.75f + side * 0.6f);
        arrow.SetPosition(1, tip);
        arrow.SetPosition(2, tip - dir * 0.75f - side * 0.6f);
        arrowShade.enabled = true;
        for (int i = 0; i < 3; i++) arrowShade.SetPosition(i, arrow.GetPosition(i));
        arrow.startColor = arrow.endColor = new Color(color.r, color.g, color.b, 0.75f + 0.25f * Mathf.Sin(Time.time * 8f));
    }
}

// 황금 고블린 (1.1~): 맵 밖으로 나가지 않고, 달아나는 쪽을 바라보며 금빛을 흘림
public class GoldenGoblin : MonoBehaviour
{
    SpriteRenderer sr;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void LateUpdate()
    {
        transform.position = Hostile.ClampArena(transform.position);
        PlayerController p = Hostile.Player;
        if (sr != null && p != null) sr.flipX = p.transform.position.x > transform.position.x;     // 적 Update 는 플레이어 쪽을 보게 하므로 달아나는 쪽으로
        if (Random.value < Time.deltaTime * 8f)
            Fx.Spawn("fx_sparkle", transform.position + (Vector3)(Random.insideUnitCircle * 0.6f), 0.9f, new Color(1f, 0.85f, 0.3f), 20f);
    }
}
