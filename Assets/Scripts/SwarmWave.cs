using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 1.1~ 영혼 부스러기 무리: 체력 1 · 몸통 피해 0 인 부스러기(SwarmMote) 12 ~ 20마리가 화면 밖에서 한 줄로 몰려와 가로지름
// 1장은 25초마다, 2장부터(· 무한 모드)는 40초마다 · 장에 들어온 뒤 첫 무리는 그 절반 시간에
// 첫 3분이 비어 보이던 것을 채우려고 (첫 판 1분 30초까지 처치 약 60 · 레벨업 3번이 목표)
// 풀 40개를 돌려 씀 · 한 화면 최대 적 수(EnemySpawner.AliveLimit)와 보스까지 처치 수에는 넣지 않음 (경험치 · 이번 판 처치 수 · 처치 효과는 그대로)
// 나오지 않을 때: 장 보스전 · 보스 처치 뒤 · 장 전환 연출 · 무한 모드 개발자 등장 뒤 · 튜토리얼 · 촬영 · 메뉴가 열려 있을 때
public class SwarmWave : MonoBehaviour
{
    public const int PoolSize = 40;
    public const int MinCount = 12, MaxCount = 20;
    public const float FirstChapterEvery = 25f, LaterEvery = 40f;
    const float Spacing = 0.85f;                // 무리 안 부스러기 사이 (칸)
    const float ExpShare = 0.2f;                // 이 장에서 가장 약한 잡몹 경험치의 이만큼

    readonly List<SwarmMote> pool = new List<SwarmMote>(PoolSize);
    float timer;
    int lastStage = int.MinValue;
    // 「몰려온다!」 안내는 판에서 처음 한 번만: 씬을 다시 불러와도(이어하기) 남게 static
    // 새 판이면 지움, 이어하기면 첫 무리 시각(1장 절반 시간)을 지난 저장일 때 이미 본 것으로
    static bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name != "GameScene") return;
            RunSave.Data resume = RunSave.Pending;
            announced = resume != null && resume.seconds >= FirstChapterEvery * 0.5f;
            new GameObject("SwarmWave").AddComponent<SwarmWave>();
        };
    }

    // 지금 나와 있는 부스러기 수
    public int Alive
    {
        get
        {
            int n = 0;
            foreach (SwarmMote m in pool) if (m != null && !m.Free) n++;
            return n;
        }
    }

    static float Every => GameMode.IsEndless || Chapters.SlotOf(Chapters.CurrentStage) > 0 ? LaterEvery : FirstChapterEvery;

    void Update()
    {
        EnemySpawner sp = Cache<EnemySpawner>.Get;
        StageManager sm = StageManager.Instance;
        PlayerController p = Hostile.Player;
        if (sp == null || sm == null || p == null) return;

        // 장이 바뀌면 처음부터 (첫 무리는 절반 시간에)
        if (sp.stageIndex != lastStage)
        {
            lastStage = sp.stageIndex;
            timer = Every * 0.5f;
        }
        if (!CanSpawn(sp, sm, p)) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Every;
        Launch(sp, sm, p);
    }

    static bool CanSpawn(EnemySpawner sp, StageManager sm, PlayerController p)
    {
        if (Time.timeScale <= 0f || !sm.Started || sm.IsMenuOpen || PlayerController.Cinematic) return false;
        if (p.IsDying || p.PlayerHealth <= 0f) return false;
        if (TutorialRun.Active || GameInput.TrailerRunning) return false;
        if (!sp.spawningEnabled || sp.bossSpawned) return false;                       // 보스전에는 보스에 집중
        if (sp.bossCleared && !GameMode.IsEndless) return false;                       // 보스를 잡은 뒤 문에 들어가기 전
        if (GameMode.IsEndless && EndlessMode.Instance != null && EndlessMode.Instance.NoMobs) return false;     // 개발자 · 개발자 엄마 등장 뒤
        return true;
    }

    // 1.2~ 무리 하나를 모두 쓰러뜨렸는지 (영혼 계약 「수확」): 무리마다 번호 · 남은 수 · 놓친 것
    static int waveId, waveLeft;
    static bool waveMissed;
    public static void MoteDown(int wave, bool killed)
    {
        if (wave != waveId || waveLeft <= 0) return;
        if (!killed) waveMissed = true;
        if (--waveLeft == 0 && !waveMissed) RunCounters.Add("swarm");
    }

    void Launch(EnemySpawner sp, StageManager sm, PlayerController p)
    {
        int count = Mathf.Min(Random.Range(MinCount, MaxCount + 1), PoolSize - Alive);
        if (count < MinCount / 2) return;       // 지난 무리가 아직 많이 남음
        waveId++;
        waveLeft = 0;
        waveMissed = false;

        // 화면 대각선 밖에서 출발해 플레이어 쪽으로 (맵 안에서 출발하는 방향을 우선)
        float reach = 30f;
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float h = cam.orthographicSize, w = h * cam.aspect;
            reach = Mathf.Sqrt(h * h + w * w) + 2f;
        }
        Vector2 me = p.transform.position;
        // 8번 다 맵 밖이면 맵에 가장 가까운 출발점으로 (맵 밖을 오래 날다 수명이 끝나는 무리가 줄게)
        // 벽 · 구조물은 그대로 통과 (영혼 부스러기라 일부러)
        Vector2 dir = Vector2.right, start = me - dir * reach;
        float bestOut = float.MaxValue;
        for (int t = 0; t < 8; t++)
        {
            Vector2 d = Random.insideUnitCircle.normalized;
            if (d.sqrMagnitude < 0.5f) d = Vector2.right;
            Vector2 at = me - d * reach + new Vector2(-d.y, d.x) * Random.Range(-3f, 3f);
            float outBy = ((Vector2)Hostile.ClampArena(at) - at).sqrMagnitude;
            if (outBy < bestOut) { bestOut = outBy; dir = d; start = at; }
            if (outBy < 0.0001f) break;
        }

        float exp = MoteExp(sp);
        float life = (reach * 2f + count * Spacing) / SwarmMote.Speed + 1.5f;
        SortingOf(sp, out int layer, out int order);
        Vector2 side = new Vector2(-dir.y, dir.x);
        for (int i = 0; i < count; i++)
        {
            SwarmMote m = Take();
            if (m == null) break;
            Vector2 at = start - dir * (i * Spacing) + side * (Mathf.Sin(i * 0.8f) * 0.5f);
            m.Wave = waveId;
            waveLeft++;
            m.Launch(at, dir, life, exp, i * 0.8f, layer, order);
        }

        Hostile.Play("whoosh", 0.45f, 1.35f);
        // 처음 한 번은 무엇인지 알려 줌
        if (!announced && SpecialAbilities.SharedFx != null)
        {
            announced = true;
            SpecialAbilities.SharedFx.FloatText(p.transform.position + Vector3.up * 2.2f, Loc.T("swarm.incoming"), new Color(0.7f, 0.95f, 1f), 5f, 0f, 3f);     // 1.1~ 0.9초면 무리가 보이기 전에 사라졌음
        }
    }

    // 풀에서 쉬는 부스러기 (모자라면 40개까지 새로, 다음 장으로 넘어가며 지워진 자리는 다시 만듦)
    SwarmMote Take()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] == null) pool[i] = SwarmMote.Create(transform);
            if (!pool[i].gameObject.activeSelf) return pool[i];
        }
        if (pool.Count >= PoolSize) return null;
        SwarmMote m = SwarmMote.Create(transform);
        pool.Add(m);
        return m;
    }

    // 이 장에서 가장 약한 잡몹 경험치 × 장 보상 배율 × 잡몹 처치 보상 배율(GameMode.RewardMul, 보스까지 세는 잡몹과 같은 기준) × 0.2 (1장 보통 약 3)
    static float MoteExp(EnemySpawner sp)
    {
        int least = 0;
        GameObject[] enemies = sp.Stage.enemies;
        if (enemies != null)
            foreach (GameObject g in enemies)
            {
                if (g == null || !g.TryGetComponent(out EnermyController e) || e.expReward <= 0) continue;
                if (least == 0 || e.expReward < least) least = e.expReward;
            }
        if (least == 0) least = 20;
        return Mathf.Max(1f, Mathf.Round(least * Chapters.RewardMul(Chapters.CurrentStage) * GameMode.RewardMul * ExpShare));
    }

    // 이 장 잡몹과 같은 그림 층 (조금 위)
    static void SortingOf(EnemySpawner sp, out int layer, out int order)
    {
        layer = 0;
        order = 1;
        GameObject[] enemies = sp.Stage.enemies;
        if (enemies == null) return;
        foreach (GameObject g in enemies)
            if (g != null && g.TryGetComponent(out SpriteRenderer r)) { layer = r.sortingLayerID; order = r.sortingOrder + 1; return; }
    }
}
