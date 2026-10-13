using System.Collections.Generic;
using UnityEngine;

// 장 중반 이벤트 (1.0.1): 판 기록에서 중간에 그만둔 판이 많아(12판 중 5판), 장 한가운데에 굴곡을 넣음
//   처치 수 30%: 「습격」 — 둘레에 적 무리가 한꺼번에 나타나고, 모두 쓰러뜨리면 보물 상자
//   처치 수 55%: 1장에도 엘리트(중간 보스) 하나 — 2장부터는 원래 페이즈마다 중간 보스가 나옴
//   중간 보스 · 엘리트를 쓰러뜨리면 보물 상자 (TreasureChest)
// 무한 모드 · 튜토리얼 · 체험판 · 자동 촬영에서는 쉼. StageManager 가 붙임
public class ChapterEvents : MonoBehaviour
{
    const float AmbushAt = 0.3f, EliteAt = 0.55f, AmbushTimeout = 50f, AmbushRadius = 40f;     // 1.0.1: 둘레 8 → 40칸 (훨씬 넓게 둘러쌈)

    EnemySpawner sp;
    int stage = -1;
    bool ambushDone, eliteDone;
    List<GameObject> ambush;
    float ambushUntil;
    readonly List<TreasureChest> chests = new List<TreasureChest>();

    static bool Off => GameMode.IsEndless || TutorialRun.Active || Demo.On || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode;

    void Start()
    {
        sp = FindFirstObjectByType<EnemySpawner>();
        if (sp != null) sp.onMidBossDefeated += OnMidBossDefeated;
    }

    void OnDestroy()
    {
        if (sp != null) sp.onMidBossDefeated -= OnMidBossDefeated;
    }

    void OnMidBossDefeated()
    {
        if (Off) return;
        PlayerController p = Hostile.Player;
        if (p != null) chests.Add(TreasureChest.Spawn(p.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 2.5f)));
    }

    void Update()
    {
        StageManager sm = StageManager.Instance;
        if (Off || sp == null || sm == null || !sm.Started) return;

        // 장이 바뀌면 새로 (지난 장의 상자는 치움)
        if (sm.CurrentStage != stage)
        {
            stage = sm.CurrentStage;
            ambushDone = eliteDone = false;
            ambush = null;
            foreach (TreasureChest c in chests) if (c != null) Destroy(c.gameObject);
            chests.Clear();
        }
        if (sp.bossSpawned || sp.bossCleared || Time.timeScale == 0f) return;

        float k = sp.killedEnemy / (float)Mathf.Max(1, sp.BossKillTarget);
        PlayerController p = Hostile.Player;
        if (p == null) return;

        if (!ambushDone && k >= AmbushAt)
        {
            ambushDone = true;
            int slot = Chapters.SlotOf(stage);
            ambush = sp.SpawnAmbush(p.transform.position, 14 + 3 * slot, AmbushRadius);
            ambushUntil = Time.time + AmbushTimeout;
            sm.ShowBanner(Loc.T("습격!\n모두 쓰러뜨리면 보물 상자가 나타납니다"), 3f);
            Hostile.Play("roar", 0.6f, 1.1f);
        }
        if (ambush != null)
        {
            ambush.RemoveAll(g => g == null);
            if (ambush.Count == 0)
            {
                ambush = null;
                chests.Add(TreasureChest.Spawn(p.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 2.5f)));
            }
            else if (Time.time > ambushUntil) ambush = null;        // 너무 오래 걸리면 (멀리 도망친 적) 상자 없이 끝
        }

        if (!eliteDone && k >= EliteAt)
        {
            eliteDone = true;
            if (Chapters.SlotOf(stage) == 0) sp.SpawnMidBoss();      // 2장부터는 원래 페이즈마다 중간 보스
        }
    }
}
