using System.Collections.Generic;
using UnityEngine;

// 4장 「영혼의 심연」 (2.1.1~): 씬 · 프리팹을 고치지 않고 게임을 시작할 때 코드로 조립
//   - 잡몹 5종 · 보스 거울의 군주: 3장 · 2장 프리팹을 꺼진 견본으로 복제해 애니메이터를 떼고 도트(Resources/Abyss) · 수치 · 스킬을 바꿈
//   - 맵: 지옥 맵 배치를 복제해 심연 타일로 다시 칠하고 소품 · 떠도는 영혼 불빛을 더함 (EndlessMode.ReskinHellMap)
// 스폰러의 4번째 스테이지(번호 3)가 되고, 무한 모드는 그 뒤(번호 4)
public static class AbyssStage
{
    public const int Index = 3;
    public static readonly Color Background = new Color(0.035f, 0.03f, 0.075f);
    public static readonly Color SoulColor = new Color(0.55f, 0.9f, 1f);

    // 잡몹 정의 (약한 적 → 강한 적). 수치는 3장보다 한 단계 위
    struct Def
    {
        public string name, art;
        public int hp, exp, coins, heal;
        public float speed, contact, scale, zigzag, dash, knock;
        public EnemySkillType skill;
    }

    static readonly Def[] Defs =
    {
        new Def { name = "AbyssWisp", art = "wisp", hp = 26, speed = 13f, contact = 15f, exp = 55, coins = 2, heal = 4, scale = 0.95f, zigzag = 0.6f, dash = 3f, knock = 1f, skill = EnemySkillType.None },
        new Def { name = "ChainWraith", art = "chainwraith", hp = 40, speed = 9.5f, contact = 17f, exp = 70, coins = 3, heal = 5, scale = 1.05f, knock = 0.8f, skill = EnemySkillType.ChainPull },
        new Def { name = "VoidEye", art = "voideye", hp = 34, speed = 8f, contact = 16f, exp = 70, coins = 3, heal = 5, scale = 0.95f, zigzag = 1f, knock = 0.9f, skill = EnemySkillType.GazeBeam },
        new Def { name = "SoulReaper", art = "reaper", hp = 55, speed = 10.5f, contact = 20f, exp = 90, coins = 3, heal = 6, scale = 1.1f, knock = 0.6f, skill = EnemySkillType.ReapSweep },
        new Def { name = "AbyssColossus", art = "colossus", hp = 160, speed = 6.5f, contact = 30f, exp = 140, coins = 6, heal = 10, scale = 1.35f, knock = 0.15f, skill = EnemySkillType.Fissure },
    };

    public const int MirrorHp = 4200;

    // 도감 카드 (이름 · 그림 · 수치)
    public static IEnumerable<(string name, string art, int hp, float speed, float contact)> CodexEnemies()
    {
        foreach (Def d in Defs) yield return (d.name, d.art, d.hp, d.speed, d.contact);
    }
    public static string ArtOf(string enemyName) { foreach (Def d in Defs) if (d.name == enemyName) return d.art; return enemyName == "MirrorLord" ? "mirrorlord" : null; }

    static readonly Dictionary<string, Sprite[]> frames = new Dictionary<string, Sprite[]>();

    // Resources/Abyss/<art>_0 ~ 3
    public static Sprite[] Frames(string art)
    {
        if (frames.TryGetValue(art, out Sprite[] f) && f[0] != null) return f;
        f = new Sprite[4];
        for (int i = 0; i < 4; i++) f[i] = Resources.Load<Sprite>("Abyss/" + art + "_" + i);
        frames[art] = f;
        return f;
    }

    // 스테이지 목록 끝에 4장을 붙임 (이미 있으면 그대로). 1 · 2 · 3장 프리팹이 있어야 견본을 만듦
    public static StageConfig[] Append(StageConfig[] stages)
    {
        if (stages == null || stages.Length != 3) return stages;
        StageConfig meadow = stages[2], hell = stages[1];
        if (meadow.enemies == null || meadow.enemies.Length == 0 || hell.bossPrefab == null) return stages;

        // 견본은 꺼진 부모 아래에 둠 (스스로 움직이지 않고, 스폰러가 복제할 때만 켜진 채로 나옴)
        GameObject root = new GameObject("AbyssTemplates");
        root.SetActive(false);

        GameObject[] enemies = new GameObject[Defs.Length];
        for (int i = 0; i < Defs.Length; i++) enemies[i] = MakeEnemy(Defs[i], meadow.enemies[Mathf.Min(i, meadow.enemies.Length - 1)], root.transform);
        GameObject boss = MakeBoss(hell.bossPrefab, root.transform);

        StageConfig abyss = new StageConfig
        {
            stageName = "심연",
            enemies = enemies,
            phases = new[]
            {
                new SpawnPhase { killsToEnter = 0, spawnInterval = 1.25f, maxAlive = 12, weights = new float[] { 70, 30, 0, 0, 0 } },
                new SpawnPhase { killsToEnter = 15, spawnInterval = 1.05f, maxAlive = 15, weights = new float[] { 45, 30, 25, 0, 0 } },
                new SpawnPhase { killsToEnter = 30, spawnInterval = 0.92f, maxAlive = 18, weights = new float[] { 30, 25, 25, 20, 0 } },
                new SpawnPhase { killsToEnter = 45, spawnInterval = 0.82f, maxAlive = 20, weights = new float[] { 22, 20, 20, 23, 15 } },
                new SpawnPhase { killsToEnter = 65, spawnInterval = 0.72f, maxAlive = 21, weights = new float[] { 18, 18, 18, 24, 22 } },
            },
            bossKills = 85,
            bossPrefab = boss,
            minionWeights = new float[] { 4, 2, 2, 1, 0 },
        };
        List<StageConfig> list = new List<StageConfig>(stages) { abyss };
        return list.ToArray();
    }

    static GameObject MakeEnemy(Def d, GameObject template, Transform root)
    {
        GameObject go = Object.Instantiate(template, root);
        go.name = d.name;
        go.transform.localScale = new Vector3(d.scale, d.scale, 0.7f);
        Animator anim = go.GetComponent<Animator>();
        if (anim != null) Object.DestroyImmediate(anim);
        Sprite[] f = Frames(d.art);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) { sr.sprite = f[0]; sr.color = Color.white; }
        CircleCollider2D col = go.GetComponent<CircleCollider2D>();
        if (col != null) { col.offset = Vector2.zero; col.radius = 1.15f; }

        EnermyController e = go.GetComponent<EnermyController>();
        e.setEnemyHP = d.hp;
        e.speed = d.speed;
        e.contactDamage = d.contact;
        e.expReward = d.exp;
        e.coinDrop = d.coins;
        e.chanceofHP = d.heal;
        e.skill = d.skill;
        e.skillCooldown = 0f;
        e.zigzagAmplitude = d.zigzag;
        e.dashInterval = d.dash;
        e.knockBackTaken = d.knock;
        e.baseColor = Color.white;
        foreach (EnemySkill s in go.GetComponents<EnemySkill>()) Object.DestroyImmediate(s);

        AbyssBody body = go.AddComponent<AbyssBody>();
        body.frames = f;
        body.fps = d.art == "colossus" ? 4f : 7f;
        return go;
    }

    // 거울의 군주: 지옥의 군주 프리팹을 바탕으로 (체력바 · 부하 소환 · 결계는 bosss 가 그대로)
    static GameObject MakeBoss(GameObject template, Transform root)
    {
        GameObject go = Object.Instantiate(template, root);
        go.name = "MirrorLord";
        go.transform.localScale = new Vector3(15f, 15f, 20f);
        Animator anim = go.GetComponent<Animator>();
        if (anim != null) Object.DestroyImmediate(anim);
        Sprite[] f = Frames("mirrorlord");
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) { sr.sprite = f[0]; sr.color = Color.white; }
        CircleCollider2D col = go.GetComponent<CircleCollider2D>();
        if (col != null) { col.offset = new Vector2(0f, -0.08f); col.radius = 0.3f; }

        bosss b = go.GetComponent<bosss>();
        b.bossKind = MirrorKind;
        b.setEnemyHP = MirrorHp;
        b.speed = 7.5f;
        b.contactDamage = 48f;
        b.expReward = 600;
        b.coinDrop = 100;
        b.summonInterval = 4.5f;
        b.summonCount = 3;
        b.enragedSummonInterval = 3f;
        b.enragedSummonCount = 4;
        b.hitsPerSummon = 14;
        b.knockBackTaken = 0.12f;
        foreach (BossSkills s in go.GetComponents<BossSkills>()) Object.DestroyImmediate(s);
        foreach (BossUltimate u in go.GetComponents<BossUltimate>()) Object.DestroyImmediate(u);

        AbyssBody body = go.AddComponent<AbyssBody>();
        body.frames = f;
        body.fps = 5f;
        return go;
    }

    public const int MirrorKind = 3;

    // ================================================================= 맵
    public static GameObject BuildMap(GameObject hellMap, Vector2 areaMin, Vector2 areaMax, Vector3 keepClear)
    {
        GameObject map = EndlessMode.ReskinHellMap(hellMap, "AbyssMap", "Abyss", "_abyss", out Dictionary<string, Sprite> art, out int sorting, out int order);
        if (map == null) return null;
        // 타일이 아닌 지옥 장식 그림도 심연 그림으로
        foreach (SpriteRenderer r in map.GetComponentsInChildren<SpriteRenderer>(true))
            if (r.sprite != null && art.TryGetValue(r.sprite.name.Replace("_hell", "_abyss"), out Sprite s)) r.sprite = s;
        ScatterProps(map.transform, art, areaMin, areaMax, keepClear, sorting, order + 1);
        map.AddComponent<AbyssAmbience>();
        map.SetActive(false);
        return map;
    }

    static void ScatterProps(Transform parent, Dictionary<string, Sprite> art, Vector2 min, Vector2 max, Vector3 keepClear, int sorting, int order)
    {
        (string name, float weight, float scale)[] kinds =
        {
            ("prop_grave", 3f, 1.6f), ("prop_bones", 3f, 1.6f), ("prop_crystal", 2.5f, 1.6f), ("prop_statue", 1.5f, 1.7f),
            ("prop_chain_pillar", 1.2f, 1.7f), ("prop_soul_brazier", 1.6f, 1.6f),
        };
        float total = 0f;
        foreach (var k in kinds) total += k.weight;

        Random.State saved = Random.state;
        Random.InitState(4049);                  // 매 판 같은 배치
        GameObject root = new GameObject("AbyssProps");
        root.transform.SetParent(parent, false);
        int placed = 0;
        for (int tries = 0; tries < 900 && placed < 85; tries++)
        {
            Vector3 pos = new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), 0f);
            if (Vector2.Distance(pos, keepClear) < 6f || Hostile.WallNear(pos, 1.2f)) continue;
            float roll = Random.Range(0f, total);
            var kind = kinds[kinds.Length - 1];
            foreach (var k in kinds) { roll -= k.weight; if (roll < 0f) { kind = k; break; } }
            if (!art.TryGetValue(kind.name, out Sprite sprite)) continue;

            GameObject go = new GameObject(kind.name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * kind.scale * Random.Range(0.85f, 1.15f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerID = sorting;
            sr.sortingOrder = order;
            sr.flipX = Random.value < 0.5f;
            if (kind.name == "prop_soul_brazier" || kind.name == "prop_crystal") go.AddComponent<SoulGlow>();
            placed++;
        }
        Random.state = saved;
    }
}

// 심연의 잡몹 · 보스 몸: 도트 4장을 돌려 보여 주고, 쓰러지면 영혼이 빠져나가며 옅어짐 (애니메이터 대신)
public class AbyssBody : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 7f;
    SpriteRenderer sr;
    EnermyController enemy;
    bosss boss;
    float t, dying;
    Vector3 baseScale;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        enemy = GetComponent<EnermyController>();
        boss = GetComponent<bosss>();
        t = Random.Range(0f, 1f);
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (sr == null || frames == null || frames.Length == 0) return;
        bool dead = (enemy != null && enemy.IsDead) || (boss != null && boss.IsDead);
        if (dead)
        {
            if (dying == 0f) Fx.Spawn("fx_soulburst", transform.position, boss != null ? 9f : 2.6f, AbyssStage.SoulColor, 16f);
            dying += Time.deltaTime;
            float k = Mathf.Clamp01(dying / 0.8f);
            Color c = sr.color;
            c.a = 1f - k;
            sr.color = c;
            transform.localScale = new Vector3(baseScale.x * (1f - 0.3f * k), baseScale.y * (1f + 0.25f * k), baseScale.z);
            return;
        }
        t += Time.deltaTime * fps;
        Sprite s = frames[(int)t % frames.Length];
        if (s != null && sr.sprite != s) sr.sprite = s;
    }
}

// 영혼 화로 · 수정: 숨 쉬듯 밝아졌다 어두워짐
public class SoulGlow : MonoBehaviour
{
    SpriteRenderer sr;
    float seed;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        seed = Random.Range(0f, 10f);
    }

    void Update()
    {
        if (sr == null) return;
        float k = 0.8f + 0.2f * Mathf.Sin(Time.time * 2.2f + seed);
        sr.color = new Color(k, k, 1f, 1f);
    }
}

// 심연 분위기: 화면 아래에서 영혼 불빛이 천천히 떠오르고 가끔 옅은 안개가 지나감
public class AbyssAmbience : MonoBehaviour
{
    const int Count = 36;
    readonly List<SpriteRenderer> motes = new List<SpriteRenderer>();
    readonly List<Vector3> vel = new List<Vector3>();
    readonly List<float> life = new List<float>();
    float nextMist;

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null || Time.timeScale == 0f || Hostile.Glow == null) return;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector3 c = cam.transform.position;
        while (motes.Count < Count)
        {
            GameObject go = new GameObject("SoulMote");
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Hostile.Glow;
            sr.sortingLayerName = VisualRules.AmbientLayer;     // 1단계-12~ 적 아래 (색이 적 「영혼」 경고색과 같아 적 위에서 헷갈렸음)
            if (!VisualRules.Legacy) sr.sortingOrder = 30;
            motes.Add(sr);
            vel.Add(Vector3.zero);
            life.Add(0f);
        }
        for (int i = 0; i < motes.Count; i++)
        {
            SpriteRenderer m = motes[i];
            if (m == null) continue;
            life[i] -= Time.deltaTime;
            if (life[i] <= 0f)
            {
                life[i] = Random.Range(4f, 8f);
                m.transform.position = new Vector3(c.x + Random.Range(-w, w), c.y + Random.Range(-h, h * 0.4f), 0f);
                vel[i] = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.4f, 1.1f), 0f);
                m.transform.localScale = Vector3.one * Random.Range(0.025f, 0.06f);
            }
            m.transform.position += (vel[i] + new Vector3(Mathf.Sin(Time.time * 1.3f + i) * 0.15f, 0f, 0f)) * Time.deltaTime;
            float a = Mathf.Clamp01(life[i] / 1.5f) * Mathf.Clamp01((8f - life[i]) / 1.5f) * 0.55f;
            m.color = new Color(0.55f, 0.92f, 1f, a);
        }
        if (Time.time >= nextMist)
        {
            nextMist = Time.time + Random.Range(3f, 6f) * VisualRules.AmbientRate;
            Fx.Spawn("fx_smoke", new Vector3(c.x + Random.Range(-w, w), c.y + Random.Range(-h, h), 0f), Random.Range(4f, 6f), new Color(0.45f, 0.4f, 0.75f, 0.25f), 8f, 0f, VisualRules.Legacy ? 12 : 30, false, -1f, VisualRules.AmbientLayer);
        }
    }
}
