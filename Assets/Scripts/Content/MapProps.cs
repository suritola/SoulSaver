using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 부서지는 맵 물체 · 봉인된 석관 (1.2~, 로드맵 2-3): ContentRegistry.Props · Drops · Relics
//   장마다 그 장 모양의 물체(항아리 · 화로 · 나무통 · 영혼 수정) 6~8개를 맵 곳곳에 놓음 · 닿으면 부서지며 하나를 떨어뜨림
//     코인(흩뿌림) · 고기(회복) · 금화(큰 코인 하나) · 자석(바닥 코인을 모두 끌어옴) · 시간 정지(적이 거의 멈춤) · 정화(둘레 잡몹 처치, 보스까지 처치 수 · 이번 판 처치 수에는 0.5배)
//   2장부터 판마다 60% 확률로 「봉인된 석관」 하나 (화살표로 위치 안내) · 곁에 1.5초 머물면 봉인이 깨지고 유물 2택 → [Q] 유물 칸 (한 판 최대 2개)
//   이어하기(RunSave): 장착한 유물 · 시드 · 이번 장에 부순 물체 수 · 석관을 연 장 (RunContent → SaveRun · RestoreRun)
// 무한 모드 · 튜토리얼 · 자동 촬영에서는 쉼 · 체험판은 1장뿐이라 석관은 나오지 않고 맵 물체만
public class MapProps : MonoBehaviour
{
    public const int MinProps = 6, MaxProps = 8;
    public const float SarcoChance = 0.6f;
    public const float SarcoHoldSeconds = 1.5f, SarcoRange = 2.8f;
    const float MinFromPlayer = 7f, MinBetween = 5f, SarcoFromPlayer = 16f;
    public const float PurifyRadius = 13f, PurifyKillShare = 0.5f;
    public const float TimeStopSeconds = 4f, TimeStopMul = 0.05f;
    public const float MagnetSeconds = 2.5f, MagnetRange = 200f;
    public const float MeatHeal = 0.2f;                         // 최대 체력의 20%
    public const float RespawnSeconds = 40f;                    // 1.2.1~ 부서진 통(물체)이 이 장 안에서 40초마다 하나씩 다시 생김 (처음 놓인 수까지)
    static readonly Color SarcoColor = new Color(0.75f, 0.5f, 1f);

    public static MapProps Instance { get; private set; }

    // ---------------- 판 단위 상태 (이어하기에 저장)
    static int seed;
    static int brokenStage = -1, brokenHere;                    // 이 장(스테이지 번호)에서 부순 물체 수
    static string sarcoOpened = "";                             // 석관을 이미 연 장 (자리 번호 글자)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) => { if (s.name == "GameScene") Begin(); };
        // 에디터에서 GameScene 을 바로 재생하면 첫 sceneLoaded 가 지나간 뒤라 여기서 한 번
        if (SceneManager.GetActiveScene().name == "GameScene" && Instance == null) Begin();
    }

    static void Begin()
    {
        seed = Random.Range(1, int.MaxValue);
        brokenStage = -1;
        brokenHere = 0;
        sarcoOpened = "";
        new GameObject("MapProps").AddComponent<MapProps>();
    }

    static bool Off => GameMode.IsEndless || TutorialRun.Active || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode;

    public static void SaveRun(RunSave.Data d)
    {
        d.propSeed = seed;
        d.propStage = brokenStage >= 0 && brokenStage == d.stage ? brokenStage + 1 : 0;      // 보스를 잡고 다음 장으로 저장하면 다음 장은 새로
        d.propsBroken = brokenHere;
        d.sarcoOpened = sarcoOpened;
        d.relics = new List<string>();
        SpecialAbilities sa = SpecialAbilities.SharedInstance;
        if (sa != null)
            foreach (int id in sa.Relics)
            {
                RelicDef r = ContentRegistry.RelicByAbility(id);
                if (r != null) d.relics.Add(r.saveCode);
            }
    }

    // 옛 저장(1.1.0 이전)은 필드가 비어 있음 → 유물 없음 · 새 시드 · 이번 장 물체는 새로
    public static void RestoreRun(RunSave.Data d)
    {
        if (d.propSeed != 0) seed = d.propSeed;
        brokenStage = d.propStage - 1;
        brokenHere = brokenStage >= 0 ? Mathf.Max(0, d.propsBroken) : 0;
        sarcoOpened = d.sarcoOpened ?? "";
        SpecialAbilities sa = SpecialAbilities.SharedInstance;
        if (sa != null && d.relics != null)
            foreach (string code in d.relics)
            {
                RelicDef r = ContentRegistry.RelicByCode(code);
                if (r != null) sa.EquipRelic(r.abilityId, true);
            }
        if (Instance != null) Instance.ClearField();            // 되살린 값으로 이 장 물체를 다시 놓음
    }

    // ================================================================= 이 장 물체
    readonly List<MapProp> props = new List<MapProp>();
    readonly List<MapPickup> pickups = new List<MapPickup>();
    MapProp sarco;
    int placedStage = -1;
    LineRenderer arrow;
    float hintAt = -1f;
    PropDef placedDef;          // 이 장에 놓은 물체 종류 · 처음 놓인 수 (다시 생김 상한)
    int placedFull;
    float nextRespawn = -1f;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (arrow != null) Destroy(arrow.gameObject);
    }

    void ClearField()
    {
        foreach (MapProp p in props) if (p != null) Destroy(p.gameObject);
        foreach (MapPickup p in pickups) if (p != null) Destroy(p.gameObject);
        props.Clear();
        pickups.Clear();
        sarco = null;
        placedStage = -1;
    }

    void Update()
    {
        StageManager sm = StageManager.Instance;
        EnemySpawner sp = Cache<EnemySpawner>.Get;
        PlayerController p = Hostile.Player;
        if (Off || sm == null || sp == null || p == null || !sm.Started || RunSave.Pending != null) { ShowArrow(false, Vector3.zero, Vector3.zero); return; }
        if (sm.CurrentStage != placedStage) Place(sm.CurrentStage, p, sp);
        TickRespawn();

        if (hintAt > 0f && Time.time >= hintAt && Time.timeScale > 0f)
        {
            hintAt = -1f;
            Hints.Show("mapprop", "맵 곳곳의 물체에 부딪치면 부서지며 코인 · 고기 · 자석 같은 것이 나옵니다.");
        }
        // 석관 쪽 화살표 (가까우면 숨김) · 보스전에도 그대로
        bool show = sarco != null && !sarco.Broken && Time.timeScale > 0f;
        ShowArrow(show, p.transform.position, show ? sarco.Home : Vector3.zero);
    }

    void Place(int stage, PlayerController player, EnemySpawner sp)
    {
        ClearField();
        placedStage = stage;
        int slot = Chapters.SlotOf(stage);
        System.Random rng = new System.Random(unchecked(seed + 7349 * (slot + 1) + 131 * stage));
        int count = rng.Next(MinProps, MaxProps + 1);
        placedFull = count;
        nextRespawn = Time.time + RespawnSeconds;
        bool sarcoRoll = rng.NextDouble() < SarcoChance;
        if (brokenStage == stage) count = Mathf.Max(0, count - brokenHere);
        else { brokenStage = stage; brokenHere = 0; }

        PropDef def = null;
        foreach (PropDef d in ContentRegistry.Props) if (!d.sarcophagus && d.stage == stage) { def = d; break; }
        if (def == null) foreach (PropDef d in ContentRegistry.Props) if (!d.sarcophagus && d.stage < 0) { def = d; break; }
        if (def == null) foreach (PropDef d in ContentRegistry.Props) if (!d.sarcophagus) { def = d; break; }

        List<Vector3> taken = new List<Vector3>();
        Vector3 me = player.transform.position;
        if (def != null)
            for (int i = 0; i < count; i++)
            {
                Vector3 at = Spot(sp, me, MinFromPlayer, taken, def.size * 0.5f + 0.8f);
                taken.Add(at);
                props.Add(MapProp.Make(def, at));
            }

        // 봉인된 석관: 2장부터 60% · 이 장에서 아직 안 열었고 유물 칸이 남았을 때 (체험판은 1장뿐이라 나오지 않음)
        SpecialAbilities sa = SpecialAbilities.SharedInstance;
        PropDef sd = ContentRegistry.PropById(ContentRegistry.Sarcophagus);
        bool room = sa != null && sa.Relics.Count < SpecialAbilities.MaxRelics && RelicCandidates(sa).Count > 0;
        if (sd != null && slot >= 1 && sarcoRoll && room && !Demo.On && sarcoOpened.IndexOf(SlotChar(slot)) < 0)
        {
            Vector3 at = Spot(sp, me, SarcoFromPlayer, taken, Mathf.Max(sd.size * 0.5f + 0.8f, 2f));      // 곁에 머물 자리까지 비게
            sarco = MapProp.Make(sd, at);
            props.Add(sarco);
            StartCoroutine(SarcoBanner());
        }
        if (def != null && count > 0) hintAt = Time.time + 8f;
        placedDef = def;
    }

    // 1.2.1~ 부서진 물체가 주기적으로 다시 생김: 게임 시간(일시정지 · 레벨업 창에선 멈춤)으로 40초마다, 안 부서진 물체가 처음 놓인 수보다 적으면 하나
    //   석관은 다시 생기지 않음 · 플레이어에게서 떨어진 빈자리(RespawnProp → Spot) · 자리를 못 찾으면 다음 주기
    void TickRespawn()
    {
        if (placedDef == null || placedFull <= 0 || Time.timeScale <= 0f) return;
        if (Time.time < nextRespawn) return;
        nextRespawn = Time.time + RespawnSeconds;
        int alive = 0;
        foreach (MapProp q in props) if (q != null && !q.Broken && !q.Def.sarcophagus) alive++;
        if (alive >= placedFull) return;
        Vector3? at = RespawnProp(placedDef);
        if (at.HasValue) ShockRing.Spawn(at.Value, 0.3f, 2.2f, 0.5f, new Color(1f, 0.9f, 0.6f), 0.25f);
    }

    static char SlotChar(int slot) => (char)('0' + Mathf.Clamp(slot, 0, 9));

    // 장 제목 알림이 지나간 뒤에 석관 알림
    IEnumerator SarcoBanner()
    {
        yield return new WaitForSeconds(4.5f);
        if (sarco == null || sarco.Broken) yield break;
        Notice.Big(Loc.T("prop.sarco.appear"), NoticeIcon.Relic, 3.5f, NoticeLevel.Important);
        Hostile.Play("pulse", 0.6f, 0.7f);
        ShockRing.Spawn(sarco.Home, 0.5f, 5f, 0.8f, SarcoColor, 0.3f);
    }

    // 맵 안 빈자리: 벽 · 구조물 · 용암 호수(태그 Wall, 적 생성 · 이동과 같은 판정 Hostile.WallNear)를 피하고, 플레이어 · 다른 물체에서 떨어진 곳
    //   못 찾으면 벽이 없는 자리 중 가장 나은 곳 · 그것도 없으면 (거의 없음) 거리만 맞춘 곳
    static Vector3 Spot(EnemySpawner sp, Vector3 me, float minFromPlayer, List<Vector3> taken, float clear)
    {
        Vector2 lo = sp.spawnAreaMin + Vector2.one * 2f, hi = sp.spawnAreaMax - Vector2.one * 2f;
        if (hi.x < lo.x) hi.x = lo.x;
        if (hi.y < lo.y) hi.y = lo.y;
        Vector3 best = Vector3.zero, anyBest = Hostile.ClampArena(me + Vector3.right * minFromPlayer);
        float bestScore = -1f, anyScore = -1f;
        for (int i = 0; i < 60; i++)
        {
            Vector3 at = new Vector3(Random.Range(lo.x, hi.x), Random.Range(lo.y, hi.y), 0f);
            float score = Mathf.Min(Vector2.Distance(at, me) / minFromPlayer, 2f);
            foreach (Vector3 t in taken) score = Mathf.Min(score, Vector2.Distance(at, t) / MinBetween);
            if (score > anyScore) { anyScore = score; anyBest = at; }
            if (Hostile.WallNear(at, clear)) continue;
            if (score >= 1f) return at;
            if (score > bestScore) { bestScore = score; best = at; }
        }
        return bestScore >= 0f ? best : anyBest;
    }

    // ================================================================= 부서짐 (MapProp 이 부름)
    public class DropCtx
    {
        public PlayerController player;
        public Vector3 at;
        public int slot;
        public MapProps host;
    }

    internal void OnBroken(MapProp prop)
    {
        int slot = Chapters.SlotOf(Chapters.CurrentStage);
        if (prop.Def.sarcophagus)
        {
            StartCoroutine(OpenSarcophagus(slot, prop.Home));
            return;
        }
        brokenHere++;
        TranscendRules.PropBroken(prop.Def, prop.Home);        // 1.2~ 초월 카드 「맵 지배」: 터지고 다시 생김
        DropDef drop = RollDrop();
        if (drop == null) return;
        DropCtx c = new DropCtx { player = Hostile.Player, at = prop.Home, slot = slot, host = this };
        if (string.IsNullOrEmpty(drop.icon)) drop.apply?.Invoke(c);
        else pickups.Add(MapPickup.Make(drop, c));
    }

    // 1.2~ 초월 카드 「맵 지배」: 부서진 물체를 이 장 안 빈자리에 다시 놓음 (부순 수도 하나 되돌려 이어하기 수와 맞춤) · 놓은 자리 (못 놓으면 null)
    public Vector3? RespawnProp(PropDef def)
    {
        StageManager sm = StageManager.Instance;
        EnemySpawner sp = Cache<EnemySpawner>.Get;
        PlayerController p = Hostile.Player;
        if (def == null || def.sarcophagus || Off || sm == null || sp == null || p == null || sm.CurrentStage != placedStage) return null;
        props.RemoveAll(q => q == null);
        List<Vector3> taken = new List<Vector3>();
        foreach (MapProp q in props) if (!q.Broken) taken.Add(q.Home);
        Vector3 at = Spot(sp, p.transform.position, MinFromPlayer, taken, def.size * 0.5f + 0.8f);
        props.Add(MapProp.Make(def, at));
        if (brokenStage == placedStage) brokenHere = Mathf.Max(0, brokenHere - 1);
        return at;
    }

    static DropDef RollDrop()
    {
        PlayerController p = Hostile.Player;
        bool low = p != null && p.PlayerHealth < p.PlayerMaxHealth * 0.4f;
        float total = 0f;
        foreach (DropDef d in ContentRegistry.Drops) total += d.weight * (d.heal && low ? 2f : 1f);
        float roll = Random.Range(0f, total);
        DropDef last = null;
        foreach (DropDef d in ContentRegistry.Drops)
        {
            last = d;
            roll -= d.weight * (d.heal && low ? 2f : 1f);
            if (roll < 0f) return d;
        }
        return last;
    }

    // ================================================================= 석관 → 유물 2택
    public static List<RelicDef> RelicCandidates(SpecialAbilities sa)
    {
        List<RelicDef> list = new List<RelicDef>();
        if (sa == null || sa.abilities == null) return list;
        foreach (RelicDef r in ContentRegistry.Relics)
        {
            int id = r.abilityId;
            if (id < 0 || id >= sa.abilities.Length || sa.abilities[id] == null || sa.abilities[id].kind != SpecialKind.Skill) continue;
            if (sa.Has(id) || CharacterData.InPool(id)) continue;     // 이미 가졌거나, 이 영웅이 보물 상자 「잠든 힘」으로 얻는 능력
            list.Add(r);
        }
        return list;
    }

    IEnumerator OpenSarcophagus(int slot, Vector3 at)
    {
        Fx.Spawn("fx_soulburst", at, 7f, SarcoColor, 16f);
        Fx.Spawn("fx_shock", at, 9f, SarcoColor, 16f);
        ShockRing.Spawn(at, 0.5f, 8f, 0.7f, SarcoColor, 0.4f);
        for (int i = 0; i < 10; i++) Fx.Spawn("fx_sparkle", at + (Vector3)(Random.insideUnitCircle * 2.2f), 1.6f, new Color(0.85f, 0.7f, 1f), 18f);
        Hostile.Play("boom", 0.5f, 0.7f);
        Hostile.Play("shimmer", 0.8f, 0.8f);
        Hostile.Shake(0.25f);

        SpecialAbilities sa = SpecialAbilities.SharedInstance;
        List<RelicDef> pool = RelicCandidates(sa);
        List<RelicDef> picks = new List<RelicDef>();
        while (picks.Count < 2 && pool.Count > 0)
        {
            float total = 0f;
            foreach (RelicDef r in pool) total += r.weight;
            float roll = Random.Range(0f, total);
            RelicDef pick = pool[pool.Count - 1];
            foreach (RelicDef r in pool) { roll -= r.weight; if (roll < 0f) { pick = r; break; } }
            picks.Add(pick);
            pool.Remove(pick);
        }
        if (sa == null || picks.Count == 0 || sa.Relics.Count >= SpecialAbilities.MaxRelics)
        {
            // 줄 유물이 없으면 금화로
            int coins = 40 + 20 * slot;
            DropCtx c = new DropCtx { player = Hostile.Player, at = at, slot = slot, host = this };
            GiveCoinsAt(c, coins, 1);
            Notice.Toast(NoticeIcon.Coin, Loc.T("prop.sarco.empty").Replace("{0}", coins.ToString()), 4f);
            MarkOpened(slot);
            yield break;
        }

        ChoiceUI.Option[] options = new ChoiceUI.Option[picks.Count];
        for (int i = 0; i < picks.Count; i++)
        {
            SpecialDef def = sa.abilities[picks[i].abilityId];
            // 1.1.2~ 어느 칸 · 키로 들어가는지 (몇 번째 유물인지 · 같은 키를 나눠 쓰는지 · 마지막 칸이면 그 뒤엔 어떻게 되는지)
            string desc = "<color=#C9A2FF>" + sa.RelicSlotNote() + "</color>\n" + SpecialAbilities.BodyText(picks[i].abilityId, def.description);
            options[i] = new ChoiceUI.Option(def.name, desc, SarcoColor, def.icon);
        }
        int chosen = 0;
        yield return ChoiceUI.Run("relic.title", "relic.pick", options, i => chosen = i);
        RelicDef got = picks[Mathf.Clamp(chosen, 0, picks.Count - 1)];
        sa.EquipRelic(got.abilityId, false);
        MarkOpened(slot);
    }

    static void MarkOpened(int slot)
    {
        char ch = SlotChar(slot);
        if (sarcoOpened.IndexOf(ch) < 0) sarcoOpened += ch;
    }

    // ================================================================= 떨어뜨리는 것 (ContentRegistry.Drops 의 apply)
    static GameObject coinPrefab;

    // 코인 그림 · 줍기는 적이 떨어뜨리는 코인 그대로 (씬에 있는 적의 코인 프리팹)
    static GameObject CoinPrefab
    {
        get
        {
            if (coinPrefab != null) return coinPrefab;
            foreach (EnermyController e in Object.FindObjectsByType<EnermyController>(FindObjectsSortMode.None))
                if (e != null && e.coin != null) { coinPrefab = e.coin; break; }
            return coinPrefab;
        }
    }

    static void GiveCoinsAt(DropCtx c, int units, int pieces)
    {
        GameObject prefab = CoinPrefab;
        if (prefab != null) { CoinTag.Drop(prefab, c.at, units, new Vector2(1.3f, 1.3f), pieces == 1, pieces); return; }
        Coin coin = Cache<Coin>.Get;
        if (coin != null) coin.AddCoin(units);
        Juice.CoinPicked(c.at, true);
    }

    // 1.1.2~ 안내 메시지 개편: 줍기 결과는 머리 위 글자(이펙트 · 적 사이에 묻힘) 대신 오른쪽 위 토스트 (그 물건 그림 · 색)
    static void Float(Vector3 at, string text, Color color, NoticeIcon icon = NoticeIcon.Coin)
    {
        Notice.Toast(icon, text, 3.5f, color);
    }

    public static void DropCoins(DropCtx c)
    {
        int n = 4 + 2 * c.slot;
        GiveCoinsAt(c, n, n);
        ShockRing.Spawn(c.at, 0.2f, 1.8f, 0.35f, new Color(1f, 0.82f, 0.3f, 0.9f), 0.15f);     // 1.1.2~ 바닥에서 금빛 고리
    }

    public static void DropGold(DropCtx c)
    {
        int n = 20 + 10 * c.slot;
        GiveCoinsAt(c, n, 1);                                   // 값이 큰 코인 하나 (크고 밝게 보임)
        Fx.Spawn("fx_sparkle", c.at, 2.4f, new Color(1f, 0.9f, 0.5f), 18f);
        ShockRing.Spawn(c.at, 0.3f, 2.4f, 0.45f, new Color(1f, 0.9f, 0.5f, 0.95f), 0.2f);
        Float(c.at + Vector3.up, Loc.T("drop.gold.name") + " +" + n, new Color(1f, 0.88f, 0.4f));
    }

    public static void DropMeat(DropCtx c)
    {
        PlayerController p = c.player;
        if (p == null) return;
        float heal = p.PlayerMaxHealth * MeatHeal * SoulWeight.HealMul;
        float before = p.PlayerHealth;
        p.PlayerHealth = Mathf.Min(p.PlayerMaxHealth, p.PlayerHealth + heal);
        Fx.Spawn("fx_levelup", p.transform.position + Vector3.up, 3f, new Color(1f, 0.55f, 0.5f), 16f);
        Float(p.transform.position + Vector3.up * 1.5f, Loc.T("drop.meat.text").Replace("{0}", Mathf.RoundToInt(p.PlayerHealth - before).ToString()), new Color(1f, 0.55f, 0.5f), NoticeIcon.Meat);
        Hostile.Play("chime", 0.6f, 1.2f);
    }

    public static void DropMagnet(DropCtx c)
    {
        if (c.player == null || c.host == null) return;
        c.host.StartCoroutine(Magnet(c.player));
        ShockRing.Spawn(c.player.transform.position, 0.5f, 16f, 0.6f, new Color(1f, 0.5f, 0.5f, 0.9f), 0.3f);
        Float(c.player.transform.position + Vector3.up * 1.5f, Loc.T("drop.magnet.text"), new Color(1f, 0.6f, 0.55f), NoticeIcon.Magnet);
        Hostile.Play("rl_magnet", 0.7f, 1f);
    }

    static IEnumerator Magnet(PlayerController p)
    {
        p.coinMagnetRange += MagnetRange;
        yield return new WaitForSeconds(MagnetSeconds);
        if (p != null) p.coinMagnetRange -= MagnetRange;
    }

    public static void DropTimeStop(DropCtx c)
    {
        if (c.player == null || c.host == null) return;
        c.host.StartCoroutine(TimeStop(c.player));
    }

    static IEnumerator TimeStop(PlayerController p)
    {
        EnermyController.GlobalSpeedMultiplier = TimeStopMul;
        Vector3 at = p.transform.position;
        Color blue = new Color(0.6f, 0.85f, 1f);
        ShockRing.Spawn(at, 1f, 18f, 0.8f, new Color(0.6f, 0.85f, 1f, 0.9f), 0.5f);
        Fx.Spawn("fx_shock", at, 16f, blue, 12f);
        FxAnim clock = Fx.Play("fx_rune", at, 14f, new Color(0.6f, 0.85f, 1f, 0.45f), 1f, 0f, 1, true, TimeStopSeconds);
        if (clock != null) { clock.follow = p.transform; clock.spin = -40f; }
        Float(at + Vector3.up * 1.5f, Loc.T("drop.timestop.text"), blue, NoticeIcon.Hourglass);
        Hostile.Play("shimmer", 0.9f, 0.5f);
        yield return new WaitForSeconds(TimeStopSeconds);
        if (Mathf.Approximately(EnermyController.GlobalSpeedMultiplier, TimeStopMul)) EnermyController.GlobalSpeedMultiplier = 1f;     // 그 사이 다른 효과가 바꿨으면 그대로
    }

    // 정화: 둘레 잡몹을 모두 처치 (중간 보스 · 엘리트 · 황금 고블린 · 부스러기 · 보스는 빼고)
    //   보스까지 처치 수(EnemySpawner.killedEnemy) · 이번 판 처치 수(RunStats.Kills) · 처치 이정표에는 0.5배만 셈 (올림, EnermyController.skipKillCount)
    public static void DropPurify(DropCtx c)
    {
        if (c.player == null) return;
        Vector3 me = c.player.transform.position;
        List<EnermyController> hit = new List<EnermyController>();
        foreach (EnermyController e in Object.FindObjectsByType<EnermyController>(FindObjectsSortMode.None))
        {
            if (e == null || e.IsDead || e.swarm != null || !e.CompareTag("enermy") || e.IsMidBoss || e.GetComponent<GoldenGoblin>() != null) continue;
            if (((Vector2)(e.transform.position - me)).sqrMagnitude > PurifyRadius * PurifyRadius) continue;
            hit.Add(e);
        }
        int counted = Mathf.CeilToInt(hit.Count * PurifyKillShare);
        Color white = new Color(1f, 0.97f, 0.8f);
        for (int i = 0; i < hit.Count; i++)
        {
            EnermyController e = hit[i];
            Fx.Spawn("fx_sparkle", e.transform.position, 1.6f, white, 20f);
            e.skipKillCount = i >= counted;         // 세지 않을 몫은 처치할 때 아예 오르지 않게 (미리 빼면 1초 동안 처치 수가 줄어 보이고 이정표가 두 번 나올 수 있었음)
            e.KillBySkill();
        }
        ShockRing.Spawn(me, 0.5f, PurifyRadius, 0.6f, new Color(1f, 0.97f, 0.8f, 0.95f), 0.5f);
        Fx.Spawn("fx_shock", me, PurifyRadius * 2f, white, 12f);
        Fx.Spawn("fx_soulburst", me, 6f, white, 16f);
        Float(me + Vector3.up * 1.5f, Loc.T("drop.purify.text").Replace("{0}", hit.Count.ToString()), white, NoticeIcon.Purify);
        Hostile.Play("pulse", 0.9f, 1.4f);
        Hostile.Shake(0.15f);
    }

    // ================================================================= 석관 화살표 (ChapterEvents 화살표보다 조금 바깥)
    void ShowArrow(bool show, Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        d.z = 0f;
        if (!show || d.sqrMagnitude < 25f)
        {
            if (arrow != null && arrow.enabled) arrow.enabled = false;
            return;
        }
        if (arrow == null)
        {
            arrow = Hostile.NewLine("SarcoArrow", SarcoColor, 0.22f, 41);
            arrow.positionCount = 3;
        }
        arrow.enabled = true;
        Vector3 dir = d.normalized, side = new Vector3(-dir.y, dir.x, 0f);
        float pulse = 0.25f * Mathf.Sin(Time.time * 7f);
        Vector3 tip = from + dir * (4f + pulse);
        arrow.SetPosition(0, tip - dir * 0.8f + side * 0.65f);
        arrow.SetPosition(1, tip);
        arrow.SetPosition(2, tip - dir * 0.8f - side * 0.65f);
        arrow.startColor = arrow.endColor = new Color(SarcoColor.r, SarcoColor.g, SarcoColor.b, 0.75f + 0.25f * Mathf.Sin(Time.time * 7f));
    }
}

// 부서지는 물체 하나 (MapProps 가 놓음): 닿으면 부서짐 · 석관은 곁에 머물러야 부서짐 (둘레 고리가 차오름)
// 1.1.2~ 적 떼 · 화려한 이펙트 속에서도 「부술 수 있는 것」으로 읽히게 (보물 상자 TreasureChest 와 같은 방식)
//   그림: 금빛 테두리(석관은 영혼빛) · 몸통 뒤 금빛 실루엣(prop_<이름>_glow)이 숨 쉬듯 맥동 · 적보다 위(Effect 9)에 그림
//   바닥: 은은히 맥동하는 빛 웅덩이 + 그림자 · 몸은 위아래로 살짝 둥실 · 가끔 반짝이는 별
//   가까이 가면 (7칸 안부터) 실루엣이 밝아지고 몸이 최대 12% 커짐 · 부서질 때 흰 번쩍임 + 파편 + 화면 살짝 흔들림
//   석관: 위로 솟는 영혼빛 기둥 · 머물 자리를 알리는 옅은 고리 · 굵고 밝아지는 진행 고리 · 25%마다 금 가는 소리
//   설정 「번쩍임 효과」가 꺼져 있으면 맥동 폭 · 반짝임을 줄이고 흰 번쩍임은 내지 않음
public class MapProp : MonoBehaviour
{
    const float ArtBody = 0.8f;                         // 그림 40칸 중 몸통 32칸 (def.size = 몸통 크기)
    const float Pulse = 2f * Mathf.PI / 1.6f;           // 맥동 1.6초
    const float Bob = 2f * Mathf.PI / 1.8f, BobHeight = 0.09f;     // 둥실 1.8초 · 위아래 0.09칸
    const float NearFrom = 7f, NearFull = 2.2f, NearGrow = 0.12f;  // 7칸 안부터 밝아지기 시작해 2.2칸에서 가장 밝고 12% 커짐
    static readonly Color Gold = new Color(1f, 0.85f, 0.35f);

    PropDef def;
    Vector3 home;
    GameObject shadow, glow, nameTag, ground, pillar, pillarCore;
    LineRenderer ring, zone;
    SpriteRenderer sr, rimSr, groundSr, pillarSr, coreSr, glowSr;
    Sprite rimArt;
    float hold, near, phase, scale;
    int ticks;
    bool broken;

    public PropDef Def => def;
    public Vector3 Home => home;
    public bool Broken => broken;

    Color RimColor => def.sarcophagus ? def.color : Gold;

    public static MapProp Make(PropDef def, Vector3 at)
    {
        at = Hostile.ClampArena(at);
        Sprite art = Resources.Load<Sprite>("Icons/" + def.icon);
        float scale = art != null ? def.size * ArtBody : def.size;
        // 적 떼에 묻히지 않게 적(Character)보다 위 Effect 층 · 보물 상자(8)와 같은 높이 · 스킬 이펙트(12~)보다는 아래
        GameObject go = SpecialAbilities.MakeSprite("MapProp_" + def.id, art != null ? art : Hostile.Glow, at, scale, art != null ? Color.white : def.color, "Effect", 9);
        MapProp p = go.AddComponent<MapProp>();
        p.def = def;
        p.home = at;
        p.scale = scale;
        p.phase = Random.Range(0f, 10f);                 // 여러 개가 한꺼번에 맥동하지 않게
        p.sr = go.GetComponent<SpriteRenderer>();
        Color rim = p.RimColor;

        // 몸통 뒤 금빛 실루엣 (테두리 바깥 2칸까지): 맥동 · 가까이 가면 밝아짐
        p.rimArt = Resources.Load<Sprite>("Icons/" + def.icon + "_glow");
        if (p.rimArt != null)
        {
            GameObject r = SpecialAbilities.MakeSprite("PropRim", p.rimArt, at, scale, new Color(rim.r, rim.g, rim.b, 0.5f), "Effect", 8);
            r.transform.SetParent(go.transform, true);
            p.rimSr = r.GetComponent<SpriteRenderer>();
        }
        // 바닥: 빛 웅덩이(맥동) + 그림자
        p.ground = SpecialAbilities.MakeSprite("PropGround", Hostile.Glow, at + Vector3.down * def.size * 0.38f, 1f, new Color(rim.r, rim.g, rim.b, 0.4f), "Background", 7);
        p.groundSr = p.ground.GetComponent<SpriteRenderer>();
        p.shadow = SpecialAbilities.MakeSprite("PropShadow", Hostile.Glow, at + Vector3.down * def.size * 0.42f, 1f, new Color(0f, 0f, 0f, 0.5f), "Background", 8);
        p.shadow.transform.localScale = new Vector3(def.size * 0.32f, def.size * 0.1f, 1f);
        if (def.sarcophagus)
        {
            p.glow = SpecialAbilities.MakeSprite("SarcoGlow", Hostile.Glow, at, 1f, new Color(def.color.r, def.color.g, def.color.b, 0.5f), "Background", 7);
            p.glowSr = p.glow.GetComponent<SpriteRenderer>();
            // 하늘로 솟는 영혼빛 기둥 (보물 상자 기둥보다 굵고 높게 · 바깥 옅은 빛 + 안쪽 밝은 심)
            p.pillar = SpecialAbilities.MakeSprite("SarcoPillar", Hostile.Glow, at + Vector3.up * 5f, 1f, new Color(def.color.r, def.color.g, def.color.b, 0.35f), "Effect", 6);
            p.pillar.transform.localScale = new Vector3(0.3f, 2.4f, 1f);
            p.pillarSr = p.pillar.GetComponent<SpriteRenderer>();
            p.pillarCore = SpecialAbilities.MakeSprite("SarcoPillarCore", Hostile.Glow, at + Vector3.up * 5f, 1f, new Color(0.95f, 0.9f, 1f, 0.5f), "Effect", 7);
            p.pillarCore.transform.localScale = new Vector3(0.08f, 2.2f, 1f);
            p.coreSr = p.pillarCore.GetComponent<SpriteRenderer>();
            // 머물 자리 (옅은 고리) · 봉인 진행 고리
            p.zone = Hostile.NewLine("SarcoZone", def.color, 0.08f, 7);
            p.zone.loop = true;
            Hostile.SetArc(p.zone, at, MapProps.SarcoRange, 0f, 354f);
            p.ring = Hostile.NewLine("SarcoRing", def.color, 0.26f, 8);
            p.ring.enabled = false;
            p.nameTag = SkillTag.Show(go.transform, def.Name, def.size * 0.75f);
            Fx.Spawn("fx_soulburst", at, 4f, def.color, 16f);
        }
        return p;
    }

    void Update()
    {
        if (broken || Time.timeScale == 0f) return;
        PlayerController p = Hostile.Player;
        bool flashes = GameSettings.Flashes;
        float t = Time.time + phase;
        float wave = 0.5f + 0.5f * Mathf.Sin(t * Pulse);          // 0~1
        float d = p != null ? Vector2.Distance(p.transform.position, home) : 99f;
        near = Mathf.MoveTowards(near, Mathf.Clamp01((NearFrom - d) / (NearFrom - NearFull)), Time.deltaTime * 4f);
        Color rim = RimColor;

        // 실루엣: 기본 맥동 (번쩍임 끄면 폭을 줄임) + 가까이 가면 밝고 흰빛으로
        if (rimSr != null)
        {
            float a = flashes ? 0.3f + 0.45f * wave : 0.5f + 0.15f * wave;
            a = Mathf.Lerp(a, 1f, near);
            Color c = Color.Lerp(rim, Color.white, 0.45f * near);
            rimSr.color = new Color(c.r, c.g, c.b, a);
        }
        if (groundSr != null)
        {
            float g = def.size * (0.28f + 0.04f * wave + 0.08f * near);       // 빛 스프라이트는 8칸 (옅게 퍼짐)
            ground.transform.localScale = new Vector3(g, g * 0.4f, 1f);
            groundSr.color = new Color(rim.r, rim.g, rim.b, 0.28f + 0.17f * wave + 0.2f * near);
        }

        if (def.sarcophagus) { UpdateSarco(p, d, t, wave, flashes); return; }

        // 둥실 · 가까이 가면 커짐 (그림자는 떠오를수록 조금 작게)
        float bob = Mathf.Sin(t * Bob);
        transform.position = home + Vector3.up * (BobHeight * (bob + 1f));
        transform.localScale = Vector3.one * scale * (1f + NearGrow * near + 0.02f * wave);
        if (shadow != null) shadow.transform.localScale = new Vector3(def.size * (0.32f - 0.02f * bob), def.size * 0.1f, 1f);
        if (Random.value < Time.deltaTime * (flashes ? 1.3f : 0.5f) * (1f + 2f * near))
            Fx.Spawn("fx_sparkle", home + new Vector3(Random.Range(-0.45f, 0.45f) * def.size, Random.Range(-0.1f, 0.5f) * def.size, 0f), 0.9f, Color.Lerp(Gold, Color.white, 0.4f), 20f);

        if (p == null || p.IsDying) return;
        if (d <= def.size * 0.4f + 0.9f) Break();
    }

    // 석관: 곁에 머무는 동안 봉인 고리가 차오르고 몸이 떨림 · 벗어나면 천천히 줄어듦
    void UpdateSarco(PlayerController p, float d, float t, float wave, bool flashes)
    {
        Color c = def.color;
        if (glowSr != null)
        {
            glow.transform.localScale = Vector3.one * (0.75f + 0.12f * Mathf.Sin(t * 3f) + 0.4f * hold);
            glowSr.color = new Color(c.r, c.g, c.b, 0.4f + 0.2f * Mathf.Sin(t * 3f) + 0.3f * hold);
        }
        if (pillarSr != null)
        {
            float shimmer = flashes ? 0.12f * Mathf.Sin(t * 2.5f) + 0.05f * Mathf.Sin(t * 9.3f) : 0.05f * Mathf.Sin(t * 2.5f);
            pillarSr.color = new Color(c.r, c.g, c.b, 0.3f + shimmer + 0.35f * hold);
            pillar.transform.localScale = new Vector3(0.3f + 0.04f * wave + 0.25f * hold, 2.4f, 1f);
            coreSr.color = new Color(0.95f, 0.9f, 1f, 0.45f + shimmer + 0.4f * hold);
        }
        if (zone != null)
        {
            zone.enabled = hold < 1f;
            zone.startColor = zone.endColor = new Color(c.r, c.g, c.b, 0.2f + 0.2f * wave + 0.25f * near);
        }
        if (Random.value < Time.deltaTime * ((flashes ? 4f : 1.5f) + 20f * hold))
            Fx.Spawn("fx_sparkle", home + (Vector3)(Random.insideUnitCircle * 1.6f), 1.1f, c, 20f);
        // 위로 오르는 영혼 불티 (기둥을 따라)
        if (Random.value < Time.deltaTime * (flashes ? 2.5f : 1f))
            Fx.Spawn("fx_sparkle", home + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(1.5f, 7f), 0f), 0.8f, new Color(0.9f, 0.8f, 1f), 18f);

        if (p == null || p.IsDying) return;
        bool inside = d <= MapProps.SarcoRange;
        float before = hold;
        hold = inside ? hold + Time.deltaTime / MapProps.SarcoHoldSeconds : Mathf.Max(0f, hold - Time.deltaTime * 0.5f);
        if (inside && before <= 0f) Hostile.Play("crack", 0.5f, 0.6f);
        int tick = Mathf.FloorToInt(Mathf.Clamp01(hold) * 4f);               // 25%마다 금 가는 소리 (점점 높게)
        if (tick > ticks && tick < 4) Hostile.Play("crack", 0.35f, 0.7f + 0.15f * tick);
        ticks = tick;
        transform.position = home + (Vector3)(Random.insideUnitCircle * 0.1f * hold);
        transform.localScale = Vector3.one * scale * (1f + 0.04f * near + 0.06f * hold);
        if (ring != null)
        {
            ring.enabled = hold > 0f;
            if (hold > 0f)
            {
                Hostile.SetArc(ring, home, MapProps.SarcoRange, 90f, 90f - 360f * Mathf.Clamp01(hold));
                Color rc = Color.Lerp(c, Color.white, 0.6f * hold);
                ring.startColor = ring.endColor = new Color(rc.r, rc.g, rc.b, 0.85f + 0.15f * wave);
                ring.startWidth = ring.endWidth = 0.26f + 0.12f * hold;
            }
        }
        if (hold >= 1f) Break();
    }

    void Break()
    {
        if (broken) return;
        broken = true;
        Color c = def.color;
        Fx.Spawn("fx_explosion", home, def.size * 1.3f, c, 22f);
        Fx.Spawn("fx_shock", home, def.size * 2.2f, c, 18f);
        ShockRing.Spawn(home, 0.3f, def.size * 1.2f, 0.35f, c, 0.25f);
        for (int i = 0; i < 6; i++) Fx.Spawn("fx_spark", home + (Vector3)(Random.insideUnitCircle * def.size * 0.4f), 0.9f, c, 22f);
        // 흰 번쩍임 (실루엣 그대로) · 파편 · 화면 살짝 흔들림
        if (GameSettings.Flashes && rimArt != null)
            FadeSprite.Spawn("PropFlash", rimArt, transform.position, transform.localScale.x * 1.05f, new Color(1f, 1f, 1f, 0.9f), "Effect", 20, 0.2f);
        PropShard.Burst(home, def.size, c, RimColor, def.sarcophagus ? 14 : 8);
        if (!def.sarcophagus) Hostile.Shake(0.05f);
        Hostile.Play("crack", 0.6f, def.sarcophagus ? 0.6f : Random.Range(1.1f, 1.3f));
        Hostile.Play("pop", 0.5f, Random.Range(0.85f, 1f));
        if (MapProps.Instance != null) MapProps.Instance.OnBroken(this);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (shadow != null) Destroy(shadow);
        if (ground != null) Destroy(ground);
        if (glow != null) Destroy(glow);
        if (pillar != null) Destroy(pillar);
        if (pillarCore != null) Destroy(pillarCore);
        if (nameTag != null) Destroy(nameTag);
        if (ring != null) Destroy(ring.gameObject);
        if (zone != null) Destroy(zone.gameObject);
    }
}

// 부서질 때 튀는 파편 (1.1.2~): 작은 네모 조각이 포물선으로 튀어 바닥에 한 번 튕긴 뒤 옅어짐 · 물체 색 · 어두운 색 · 테두리 색이 섞임
public class PropShard : MonoBehaviour
{
    const float Gravity = 26f, Life = 0.85f;
    static Sprite square;
    SpriteRenderer sr;
    Vector3 ground;
    Vector2 vel;
    float height, up, spin, age;
    Color color;
    bool bounced;

    static Sprite Square
    {
        get
        {
            if (square != null) return square;
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            square = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
            return square;
        }
    }

    public static void Burst(Vector3 at, float size, Color body, Color rim, int count)
    {
        Color dark = Color.Lerp(body, Color.black, 0.45f);
        for (int i = 0; i < count; i++)
        {
            Color c = i % 3 == 0 ? rim : i % 3 == 1 ? body : dark;
            float s = Random.Range(0.12f, 0.24f) * size;
            // 1.2.4: 다 튄 조각은 꺼 두었다가 다시 씀 (부술 때마다 8~14개 생성 · 파괴하던 것 · 무작위 값을 뽑는 순서는 그대로)
            PropShard sh = null;
            while (pool.Count > 0 && sh == null) sh = pool.Pop();     // 장면이 바뀌어 지워진 것은 건너뜀
            GameObject go;
            if (sh == null)
            {
                go = SpecialAbilities.MakeSprite("PropShard", Square, at, s, c, "Effect", 10);
                sh = go.AddComponent<PropShard>();
                sh.sr = go.GetComponent<SpriteRenderer>();
            }
            else
            {
                // MakeSprite 가 넣던 값으로 되돌림
                go = sh.gameObject;
                go.transform.position = at;
                go.transform.localScale = Vector3.one * s;
                sh.sr.sprite = Square;
                sh.sr.color = c;
                sh.age = 0f;
                sh.bounced = false;
                sh.spawnFrame = Time.frameCount;
            }
            sh.color = c;
            sh.ground = at + Vector3.down * size * Random.Range(0.1f, 0.4f);
            sh.height = at.y - sh.ground.y;
            sh.vel = Random.insideUnitCircle.normalized * Random.Range(2f, 5f) * Mathf.Sqrt(size / 2f);
            sh.up = Random.Range(5f, 9f);
            sh.spin = Random.Range(-720f, 720f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
            if (!go.activeSelf) go.SetActive(true);
        }
    }

    static readonly System.Collections.Generic.Stack<PropShard> pool = new System.Collections.Generic.Stack<PropShard>();
    int spawnFrame = -1;

    void Update()
    {
        if (Time.frameCount == spawnFrame) return;      // 새로 만든 것처럼 켜진 그 프레임에는 움직이지 않음
        float dt = Time.deltaTime;
        age += dt;
        ground += (Vector3)(vel * dt);
        up -= Gravity * dt;
        height += up * dt;
        if (height <= 0f)
        {
            height = 0f;
            if (!bounced) { bounced = true; up = -up * 0.35f; vel *= 0.4f; }
            else { up = 0f; vel *= 0.85f; spin *= 0.8f; }
        }
        transform.position = ground + Vector3.up * height;
        transform.Rotate(0f, 0f, spin * dt);
        float fade = Mathf.Clamp01((Life - age) / 0.3f);
        sr.color = new Color(color.r, color.g, color.b, color.a * fade);
        if (age >= Life)
        {
            gameObject.SetActive(false);
            pool.Push(this);
        }
    }
}

// 맵 물체가 떨어뜨린 것 (고기 · 자석 · 시간 정지 · 정화): 바닥에서 통통 튀며 빛나고, 닿으면 효과 · 40초 뒤 사라짐 (마지막 6초는 깜빡임)
// 1.1.2~ 종류별 색 테두리 그림(고기 초록 · 자석 빨강 · 시간 정지 하늘 · 정화 흰 금빛) · 바닥에 같은 색 빛 고리가 맥동 · 크기 1.3 → 1.5
public class MapPickup : MonoBehaviour
{
    const float PickRange = 1.5f, Life = 40f, Size = 1.5f;
    const float ArtBody = 32f / 36f;                    // 그림 36칸 중 몸통 32칸
    const float Pulse = 2f * Mathf.PI / 1.2f;           // 맥동 1.2초
    DropDef def;
    MapProps.DropCtx ctx;
    Vector3 home;
    GameObject glow;
    SpriteRenderer sr, glowSr;
    LineRenderer ring;
    float born;

    public static MapPickup Make(DropDef def, MapProps.DropCtx ctx)
    {
        Vector3 at = Hostile.ClampArena(ctx.at);
        Sprite art = Resources.Load<Sprite>("Icons/" + def.icon);
        GameObject go = SpecialAbilities.MakeSprite("MapPickup_" + def.id, art != null ? art : Hostile.Glow, at, art != null ? Size * ArtBody : Size, art != null ? Color.white : def.color, "Effect", 6);
        MapPickup m = go.AddComponent<MapPickup>();
        m.def = def;
        m.ctx = ctx;
        m.home = at;
        m.sr = go.GetComponent<SpriteRenderer>();
        m.born = Time.time;
        m.glow = SpecialAbilities.MakeSprite("PickupGlow", Hostile.Glow, at, 0.5f, new Color(def.color.r, def.color.g, def.color.b, 0.6f), "Effect", 5);
        m.glowSr = m.glow.GetComponent<SpriteRenderer>();
        m.ring = Hostile.NewLine("PickupRing", def.color, 0.08f, 5);
        m.ring.loop = true;
        ShockRing.Spawn(at, 0.2f, 1.6f, 0.3f, def.color, 0.15f);
        return m;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        float t = Time.time, age = t - born;
        float wave = 0.5f + 0.5f * Mathf.Sin(t * Pulse);
        bool flashes = GameSettings.Flashes;
        // 처음 0.35초는 튀어 오름, 그 뒤 통통
        float pop = age < 0.35f ? Mathf.Sin(age / 0.35f * Mathf.PI) * 0.9f : 0f;
        transform.position = home + Vector3.up * (pop + 0.15f * Mathf.Sin(t * 4f));
        bool blink = age > Life - 6f && Mathf.Repeat(t * 6f, 1f) <= 0.35f;
        if (sr != null) sr.enabled = !blink;
        if (glow != null)
        {
            glow.transform.position = home;
            glow.transform.localScale = Vector3.one * (0.5f + (flashes ? 0.1f : 0.04f) * wave);
            glowSr.color = new Color(def.color.r, def.color.g, def.color.b, 0.45f + 0.25f * wave);
        }
        if (ring != null)
        {
            ring.enabled = !blink;
            Hostile.SetArc(ring, home + Vector3.down * 0.35f, 0.75f + 0.12f * wave, 0f, 354f);
            ring.startColor = ring.endColor = new Color(def.color.r, def.color.g, def.color.b, 0.45f + 0.4f * wave);
        }
        if (age > Life) { Destroy(gameObject); return; }
        if (Random.value < Time.deltaTime * (flashes ? 3f : 1.2f)) Fx.Spawn("fx_sparkle", home + (Vector3)(Random.insideUnitCircle * 0.6f), 0.8f, def.color, 20f);

        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        if (Vector2.Distance(p.transform.position, home) > PickRange) return;
        ctx.player = p;
        ctx.at = home;
        def.apply?.Invoke(ctx);
        Fx.Spawn("fx_sparkle", home, 2f, def.color, 20f);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (glow != null) Destroy(glow);
        if (ring != null) Destroy(ring.gameObject);
    }
}
