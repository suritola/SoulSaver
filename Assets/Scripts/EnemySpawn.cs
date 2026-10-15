using System.Collections;
using UnityEngine;

// 스테이지 설정(SpawnPhase, StageConfig)은 StageConfig.cs
// 이 파일에는 컴포넌트 클래스만 둔다 (Unity가 파일의 첫 클래스를 스크립트로 연결함)
public class EnemySpawner : MonoBehaviour
{
    [Header("스테이지")]
    public StageConfig[] stages;
    public int stageIndex = 0;

    [Header("진행 상황")]
    public int killedEnemy = 0;
    public int paze = 1;
    public bool bossSpawned = false;
    public bool bossCleared = false;
    public int spawnedEnemys = 0;
    // 스테이지 전환 연출 중에는 생성을 멈춤
    public bool spawningEnabled = true;

    // 보스전에는 부하가 더 나올 수 있도록 최대 수를 늘림
    public int bossExtraAlive = 10;

    [Header("스폰 위치")]
    // 적이 생성될 수 있는 맵 안쪽 범위 (벽 안쪽에서 조금 띄움)
    public Vector2 spawnAreaMin = new Vector2(-52f, -36f);
    public Vector2 spawnAreaMax = new Vector2(53f, 29f);
    // 플레이어와 최소 이만큼 떨어진 곳에만 생성
    public float minSpawnDistance = 14f;
    // 보스가 나오는 신전 문 위치
    public Vector2 bossSpawnPoint = new Vector2(0.5f, 32f);

    // 보스를 쓰러뜨렸을 때 (스테이지 번호)
    public System.Action<int> onBossDefeated;

    [Header("중간 보스 (2장부터, 페이즈가 오를 때마다)")]
    public int midBossFromStage = 1;
    public float midBossHpMultiplier = 7f;
    public float midBossDamageMultiplier = 1.5f;
    public float midBossScale = 1.8f;
    public Color midBossColor = new Color(1f, 0.72f, 0.5f);
    // 중간 보스가 나왔을 때 / 쓰러졌을 때
    public System.Action onMidBossSpawned;
    public System.Action onMidBossDefeated;
    int midBossPhase = 1;
    int midBossCount = 0;

    PlayerController playerC;
    bossbar bossbar;

    public StageConfig Stage => stages[Mathf.Clamp(stageIndex, 0, stages.Length - 1)];
    SpawnPhase Phase => Stage.phases[Mathf.Clamp(paze - 1, 0, Stage.phases.Length - 1)];
    // 보스를 뺀 적은 (소환 포함) 최대 20마리
    public const int AliveLimit = 20;
    // 1.0.4~ 무한 모드 최종 보스 「개발자」를 부르기 시작한 뒤로는 습격 · 중간 보스 · 보스 부하가 나오지 않음 (1.0.7~ 일반 적은 다시 나옴)
    static bool NoMobs => GameMode.IsEndless && EndlessMode.Instance != null && EndlessMode.Instance.NoMobs;
    // 화면에 남은 잡몹을 가까운 것부터 영혼으로 흩음 (개발자 · 개발자 엄마가 나올 때)
    public void ClearMobs() => StartCoroutine(PurgeAll());
    // 앞쪽 장은 한 화면 최대 수를 줄임 (Chapters · 2.1.5)
    int MaxAlive => Mathf.Min(AliveLimit + GameMode.ExtraAliveCount, Chapters.MaxAlive(stageIndex, Phase.maxAlive) + GameMode.ExtraAliveCount + (bossSpawned ? bossExtraAlive : 0));

    void Start()
    {
        if (stages == null || stages.Length == 0 || stages[0].enemies == null || stages[0].enemies.Length == 0)
        {
            Debug.LogError("EnemySpawner: 스테이지 설정(Stages)이 비어 있습니다. 씬을 저장하지 말고 GameScene을 다시 열어 주세요.", this);
            enabled = false;
            return;
        }

        playerC = FindFirstObjectByType<PlayerController>();
        bossbar = FindFirstObjectByType<bossbar>();

        // 게임 시작 시 적 1마리 생성
        SpawnEnemy(false, transform.position);

        // 페이즈에 따라 간격이 짧아지는 적 생성
        StartCoroutine(SpawnLoop());
        InvokeRepeating("clear", 30, 30);
    }

    // 떨어진 코인을 자동으로 회수
    void clear()
    {
        Coin coin1 = Cache<Coin>.Get;
        if (coin1 == null || playerC == null) return;
        // 1.0.4~: 합쳐진 코인은 값만큼 (하나씩 주울 때와 같은 양) · 씬을 태그로 뒤지지 않고 코인 목록에서
        int units = CoinTag.TakeAll();
        if (units <= 0) return;
        int each = 1 + playerC.bonusCoin;
        coin1.AddCoin(units * each);
        for (int i = 0; i < units; i++) SignatureSkills.CoinPicked(each);
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Phase.spawnInterval * GameMode.SpawnIntervalMul * Chapters.SpawnIntervalMul(stageIndex));     // 앞쪽 장은 드물게 (2.1.5)
            if (spawningEnabled) SpawnEnemy(false, transform.position);
        }
    }

    public void SpawnEnemy(bool boss, Vector3 here)
    {
        if (boss)
        {
            SummonMinions(here, 3);
            return;
        }

        if (GameMode.IsEndless && EndlessMode.Instance != null && EndlessMode.Instance.FinalBossAlive) return;     // 1.1.2~ 개발자 · 엄마와 싸우는 동안엔 잡몹도 없음
        if (spawnedEnemys >= MaxAlive) return;      // 1.0.7~ 한 화면 최대 수 (1.1.2~ 개발자 · 엄마전 중엔 위에서 잡몹까지 막음)
        if (playerC == null) playerC = FindFirstObjectByType<PlayerController>();

        spawnedEnemys++;
        Vector3 randomPosition = GetSpawnPosition(playerC.transform.position);
        GameObject spawned = Instantiate(Pick(Phase.weights), randomPosition, Quaternion.identity);
        // 보스 전에 나온 적은 처치 수로 보스를 부르므로, 늘어난 처치 수만큼 보상을 나눠 받음 (Start 전에 정함)
        if (!bossSpawned && !bossCleared && spawned.TryGetComponent(out EnermyController ec)) ec.countsTowardBoss = true;

        UpdatePhase();

        if (killedEnemy >= GameMode.ScaleKills(Chapters.BossKills(stageIndex, Stage.bossKills)) && !bossSpawned && !bossCleared && Stage.bossPrefab != null)
        {
            if (bossbar == null) bossbar = FindFirstObjectByType<bossbar>();
            if (bossbar != null) bossbar.bossSpawn = true;
            bossSpawned = true;

            // 보스는 신전 문에서 등장
            Instantiate(Stage.bossPrefab, bossSpawnPoint, Quaternion.identity);
        }
    }

    // 판 중간 저장 (RunSave, 2.1.9~): 보스가 나오는 처치 수 · 저장할 때의 처치 수로 되돌림 (지난 페이즈의 중간 보스는 다시 나오지 않게)
    public int BossKillTarget => GameMode.ScaleKills(Chapters.BossKills(stageIndex, Stage.bossKills));

    // midBossAlive: 나갈 때 이 페이즈의 중간 보스가 살아 있었음 → 다음에 적이 나올 때 다시 나옴 (1.0.4~)
    public void RestoreKills(int kills, bool midBossAlive = false)
    {
        killedEnemy = Mathf.Clamp(kills, 0, BossKillTarget);
        int phase = 1;
        for (int i = 1; i < Stage.phases.Length; i++)
            if (killedEnemy >= GameMode.ScaleKills(Stage.phases[i].killsToEnter)) phase = i + 1;
        paze = phase;
        midBossPhase = midBossAlive ? phase - 1 : phase;
    }

    // 처치 수에 맞는 페이즈로 올림
    void UpdatePhase()
    {
        int phase = 1;
        for (int i = 1; i < Stage.phases.Length; i++)
        {
            if (killedEnemy >= GameMode.ScaleKills(Stage.phases[i].killsToEnter) && GameMode.EndlessSeconds >= Stage.phases[i].minSeconds) phase = i + 1;
            else break;
        }
        paze = phase;

        if (Chapters.SlotOf(stageIndex) >= midBossFromStage && phase > midBossPhase      // 몇 번째 장인지로 (2.1.1~ 장 순서)
            && (!GameMode.IsEndless || GameMode.EndlessSeconds >= EndlessMode.MidBossPhaseFrom))      // 1.1.7~ 무한 모드 1:30 전에는 페이즈가 올라도 중간 보스 없음
        {
            midBossPhase = phase;
            SpawnMidBoss();
        }
    }

    // 지금 페이즈에 나오는 적 중 하나를 크고 단단하게 만들어 소환 (처치하면 특수 능력 포인트)
    public void SpawnMidBoss()
    {
        if (NoMobs) return;
        var candidates = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < Stage.enemies.Length; i++)
            if (Weight(Phase.weights, i) > 0f && Stage.enemies[i] != null) candidates.Add(Stage.enemies[i]);
        if (candidates.Count == 0) return;
        // 체력이 높은 순으로, 나올 때마다 다른 종류
        candidates.Sort((a, b) => HpOf(b).CompareTo(HpOf(a)));
        GameObject prefab = candidates[midBossCount % candidates.Count];
        midBossCount++;

        if (playerC == null) playerC = FindFirstObjectByType<PlayerController>();
        GameObject go = Instantiate(prefab, GetSpawnPosition(playerC.transform.position), Quaternion.identity);
        go.name = "MidBoss_" + prefab.name;
        go.transform.localScale *= midBossScale;
        spawnedEnemys++;

        EnermyController e = go.GetComponent<EnermyController>();
        if (e != null)
        {
            e.setEnemyHP = Mathf.RoundToInt(e.setEnemyHP * midBossHpMultiplier);
            e.contactDamage *= midBossDamageMultiplier;
            e.expReward *= 8;
            e.coinDrop = Mathf.Max(8, e.coinDrop * 6);
            e.knockBackTaken *= 0.15f;
            e.chanceofHP = 100;
            e.survivesContact = true;
            e.baseColor = midBossColor;
            e.onKilled += () => onMidBossDefeated?.Invoke();
            MidBossMark.Attach(go, CodexUI.EnemyName(prefab.name));
        }
        onMidBossSpawned?.Invoke();
    }

    static int HpOf(GameObject prefab)
    {
        EnermyController e = prefab.GetComponent<EnermyController>();
        return e != null ? e.setEnemyHP : 0;
    }

    // 비율에 따라 적 종류를 고름 (비율이 없는 적은 나오지 않음)
    GameObject Pick(float[] weights)
    {
        GameObject[] enemies = Stage.enemies;
        float total = 0f;
        for (int i = 0; i < enemies.Length; i++) total += Weight(weights, i);

        if (total <= 0f) return enemies[0];

        float roll = Random.Range(0f, total);
        for (int i = 0; i < enemies.Length; i++)
        {
            roll -= Weight(weights, i);
            if (roll < 0f) return enemies[i];
        }
        return enemies[enemies.Length - 1];
    }

    static float Weight(float[] weights, int i) => weights != null && i < weights.Length ? weights[i] : 0f;

    // 장 중반 「습격」 (ChapterEvents, 1.0.1): 둘레에 지금 페이즈의 적을 한꺼번에 (최대 수와 상관없이)
    public System.Collections.Generic.List<GameObject> SpawnAmbush(Vector3 center, int count, float radius)
    {
        var list = new System.Collections.Generic.List<GameObject>();
        if (NoMobs) return list;
        float off = Random.Range(0f, Mathf.PI * 2f);
        for (int i = 0; i < count; i++)
        {
            float a = off + i * Mathf.PI * 2f / count;
            Vector3 at = Hostile.ClampArena(center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (radius + Random.Range(-1f, 1f)));
            GameObject go = Instantiate(Pick(Phase.weights), at, Quaternion.identity);
            if (go.TryGetComponent(out EnermyController ec)) ec.countsTowardBoss = true;
            Fx.Spawn("fx_smoke", at, 2f, new Color(0.8f, 0.7f, 0.9f), 18f);
            spawnedEnemys++;
            list.Add(go);
        }
        return list;
    }

    // 보스가 부르는 부하: 처치 시 수가 줄어들므로 여기서도 세어야 함
    public void SummonMinions(Vector3 here, int count)
    {
        if (NoMobs) return;
        for (int i = 0; i < count; i++)
        {
            if (spawnedEnemys >= MaxAlive) return;

            spawnedEnemys++;
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
            Instantiate(Pick(GameMode.IsEndless ? Phase.weights : Stage.minionWeights), here + (Vector3)offset, Quaternion.identity);     // 1.1.7~ 무한 모드 부하는 지금 나오는 등급에서
        }
    }

    public void OnBossDefeated()
    {
        bossCleared = true;
        bossSpawned = false;
        // 보스를 쓰러뜨리면 화면의 잡몹이 영혼이 되어 흩어지고, 이 스테이지에서는 더 나오지 않음
        // (무한 모드는 끝없이 이어지므로 화면만 정리하고 계속 나옴 · 다음 스테이지는 StartStage 에서 다시 켬)
        if (!GameMode.IsEndless) spawningEnabled = false;
        StartCoroutine(BossFinale());       // 1.0.2: 그 자리에서 사라지던 것을 소용돌이 연출로
        onBossDefeated?.Invoke(stageIndex);
    }

    // 보스 처치 연출 (1.0.2): 화면의 모든 적 · 코인 · 회복약이 소용돌이치며 나에게 빨려 들고, 캐릭터는 빙글빙글 돌다 영혼 폭발로 마무리
    // 적은 나에게 닿으면 영혼이 되어 사라지고, 코인 · 회복약은 몸에 닿아 저절로 주워짐 · 그동안 무적
    class Pulled { public Transform t; public EnermyController e; public bool coin; public float angle, dist; }
    const float FinaleTime = 2.2f;
    const int PurgePerFrame = 6;        // 한 프레임에 영혼이 되는 적 수 (한꺼번에 사라지면 이펙트가 몰려 끊겼음)

    // 렉 줄이기 (1.0.2): 끌려오는 동안 적 · 코인의 충돌체와 물리를 꺼서 위치만 옮기고(물리 계산 없음),
    // 코인은 닿을 때마다 하나씩 줍는 대신 도착하면 지우고 마지막에 한 번에 더함, 적은 프레임마다 나눠서 영혼으로
    IEnumerator BossFinale()
    {
        if (playerC == null) playerC = FindFirstObjectByType<PlayerController>();
        if (playerC == null || playerC.IsDying) { yield return PurgeAll(); yield break; }
        Transform me = playerC.transform;
        var list = new System.Collections.Generic.List<Pulled>();
        var purge = new System.Collections.Generic.Queue<EnermyController>();
        void Add(Transform t, EnermyController e, bool coin)
        {
            if (t == null) return;
            Vector2 d = t.position - me.position;
            list.Add(new Pulled { t = t, e = e, coin = coin, angle = Mathf.Atan2(d.y, d.x), dist = d.magnitude });
            if (e != null || coin) Quiet(t.gameObject);
            if (coin) CoinTag.Hold(t.gameObject);      // 1.0.4~: 끌려오는 동안 플레이어가 따로 줍거나 합쳐지지 않게
            if (e != null) e.enabled = false;           // 끌려오는 동안 쫓아오거나 스킬을 쓰지 않음 (영혼이 될 때 다시 켬)
        }
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("enermy"))
            if (g != null && g.TryGetComponent(out EnermyController e) && !e.IsDead) Add(g.transform, e, false);
        foreach (Transform c in new System.Collections.Generic.List<Transform>(CoinTag.All)) Add(c, null, true);
        // 회복약은 체력이 가득 차 있으면 끌어오지 않음 (닿으면 저절로 먹혀 낭비됨) · 몇 개 안 되므로 그대로 닿아서 먹힘
        if (playerC.PlayerHealth < playerC.PlayerMaxHealth)
            foreach (HPpack hp in FindObjectsByType<HPpack>(FindObjectsSortMode.None)) Add(hp.pack != null ? hp.pack.transform : hp.transform, null, false);

        // 연출 동안은 조작하지 않고 감상만 (이동 · 공격 · 스킬 · 영혼 트리 입력을 막음)
        PlayerController.Cinematic = true;
        playerC.CancelSkill();
        playerC.GrantInvincibility(FinaleTime + 1f);
        PlayerMotion.Of(playerC).Spin(FinaleTime, 7f);
        Hostile.Play("shimmer", 0.9f, 0.6f);
        Hostile.Play("whoosh", 0.9f, 0.5f);
        Hostile.Shake(0.3f);
        Color soul = new Color(0.7f, 0.95f, 1f);
        FxAnim rune = Fx.Play("fx_rune", me.position, 7f, new Color(soul.r, soul.g, soul.b, 0.8f), 1f, 0f, 30, true, FinaleTime);
        if (rune != null) { rune.spin = 240f; rune.follow = me; }

        int coins = 0;
        float spark = 0f, coinSound = 0f;
        for (float t = 0f; t < FinaleTime; t += Time.deltaTime)
        {
            float k = t / FinaleTime, ease = k * k;
            Vector3 c = me != null ? me.position : Vector3.zero;
            foreach (Pulled p in list)
            {
                if (p.t == null) continue;
                if (p.e != null && p.e.IsDead) { p.t = null; continue; }
                float d = Mathf.Lerp(p.dist, 0f, ease);
                float a = p.angle + ease * Mathf.PI * 3f;          // 한 바퀴 반 감기며 빨려 듦
                p.t.position = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * d;
                if (d > 1.3f) continue;
                if (p.e != null) { purge.Enqueue(p.e); p.t = null; }
                else if (p.coin) { coins += CoinTag.ValueOf(p.t.gameObject); CoinTag.Release(p.t.gameObject); p.t = null; }
            }
            PurgeSome(purge);
            if (coins > 0 && (coinSound -= Time.deltaTime) <= 0f) { coinSound = 0.12f; Hostile.Play("chime", 0.25f, Random.Range(1.5f, 1.8f)); }
            if ((spark -= Time.deltaTime) <= 0f)
            {
                spark = 0.12f;
                Fx.Spawn("fx_spark", c + (Vector3)(Random.insideUnitCircle * 3f), 1.6f, soul, 24f);
            }
            yield return null;
        }
        // 마무리: 남은 것은 모두 내 자리로 (적은 영혼으로, 코인은 줍기) + 영혼 폭발
        Vector3 at = me != null ? me.position : Vector3.zero;
        foreach (Pulled p in list)
        {
            if (p.t == null) continue;
            if (p.e != null) { if (!p.e.IsDead) purge.Enqueue(p.e); }
            else if (p.coin) { coins += CoinTag.ValueOf(p.t.gameObject); CoinTag.Release(p.t.gameObject); }
            else p.t.position = at;
        }
        GiveCoins(coins);
        Fx.Spawn("fx_soulburst", at, 9f, Color.white, 16f);
        Fx.Spawn("fx_shock", at, 16f, soul, 18f);
        Hostile.Play("boom", 0.7f, 0.7f);
        Hostile.Play("chime", 0.6f, 1.1f);
        Hostile.Shake(0.6f);
        for (float t = 0f; t < 0.4f || purge.Count > 0; t += Time.deltaTime)      // 남은 영혼을 나눠서 정리하고, 폭발을 본 뒤 조작
        {
            PurgeSome(purge);
            yield return null;
        }
        PlayerController.Cinematic = false;
    }

    // 끌려오는 동안 충돌 · 물리 끄기 (매 프레임 옮겨도 물리 계산이 없게, 플레이어와 부딪히지 않게)
    static void Quiet(GameObject g)
    {
        foreach (Collider2D col in g.GetComponents<Collider2D>()) col.enabled = false;
        if (g.TryGetComponent(out Rigidbody2D rb)) rb.simulated = false;
    }

    static void PurgeSome(System.Collections.Generic.Queue<EnermyController> q)
    {
        for (int n = 0; n < PurgePerFrame && q.Count > 0; n++)
        {
            EnermyController e = q.Dequeue();
            if (e == null || e.IsDead) { n--; continue; }
            e.enabled = true;
            e.Purge();
        }
    }

    // 끌려온 코인을 한 번에 (줍는 효과 · 황금 시대도 그대로, 하나씩 주울 때와 같은 값)
    void GiveCoins(int count)
    {
        Coin bank = Cache<Coin>.Get;
        if (count <= 0 || bank == null || playerC == null) return;
        int each = 1 + playerC.bonusCoin;
        bank.AddCoin(count * each);
        SignatureSkills.CoinPicked(count * each);
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.FloatText(playerC.transform.position + Vector3.up, "+" + (count * each), new Color(1f, 0.85f, 0.3f), 5f, 0f);
    }

    // 연출 중에 스포너가 꺼지거나 장면이 바뀌어도 조작이 막힌 채 남지 않게
    void OnDisable() => PlayerController.Cinematic = false;

    IEnumerator PurgeAll()
    {
        if (playerC == null) playerC = FindFirstObjectByType<PlayerController>();
        Vector3 center = playerC != null ? playerC.transform.position : transform.position;
        var list = new System.Collections.Generic.List<EnermyController>();
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("enermy"))
            if (g != null && g.TryGetComponent(out EnermyController e) && !e.IsDead) list.Add(e);
        // 가까운 적부터 차례로 퍼져 나가듯
        list.Sort((a, b) => (a.transform.position - center).sqrMagnitude.CompareTo((b.transform.position - center).sqrMagnitude));
        if (list.Count > 0) Hostile.Play("shimmer", 0.8f, 0.7f);
        float started = Time.time;
        foreach (EnermyController e in list)
        {
            if (e == null || e.IsDead) continue;
            float wait = Mathf.Min(1.2f, Vector3.Distance(e.transform.position, center) * 0.03f) - (Time.time - started);
            if (wait > 0f) yield return new WaitForSeconds(wait);
            if (e == null || e.IsDead) continue;
            e.Purge();
        }
    }

    // 다음 스테이지 시작: 남은 적과 코인을 정리하고 처음 페이즈부터
    public void StartStage(int index)
    {
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("enermy")) Destroy(enemy);
        foreach (GameObject coin in GameObject.FindGameObjectsWithTag("coin")) Destroy(coin);

        stageIndex = Mathf.Clamp(index, 0, stages.Length - 1);
        killedEnemy = 0;
        paze = 1;
        midBossPhase = 1;
        bossSpawned = false;
        bossCleared = false;
        spawnedEnemys = 0;
        spawningEnabled = true;
    }

    // 맵 안에서 플레이어와 충분히 떨어진 위치를 고름
    // 가능하면 화면 밖에서 나오게 하고, 못 찾으면 거리 조건만 지킴
    Vector3 GetSpawnPosition(Vector3 playerPos)
    {
        Camera cam = Camera.main;
        Vector3 fallback = Vector3.zero;
        bool hasFallback = false;

        for (int i = 0; i < 40; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                0);

            if (Vector2.Distance(pos, playerPos) < minSpawnDistance) continue;
            // 구조물이나 용암 호수 안에는 만들지 않음
            if (Hostile.WallNear(pos, 1.5f)) continue;

            if (cam == null) return pos;

            Vector3 vp = cam.WorldToViewportPoint(pos);
            bool offScreen = vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f;
            if (offScreen) return pos;

            if (!hasFallback)
            {
                fallback = pos;
                hasFallback = true;
            }
        }

        if (hasFallback) return fallback;

        // 조건에 맞는 곳이 없으면 플레이어 반대편 맵 끝에서 생성
        return new Vector3(
            playerPos.x < 0f ? spawnAreaMax.x : spawnAreaMin.x,
            playerPos.y < 0f ? spawnAreaMax.y : spawnAreaMin.y,
            0);
    }
}
