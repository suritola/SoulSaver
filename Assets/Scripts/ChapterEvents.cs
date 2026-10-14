using System.Collections.Generic;
using UnityEngine;

// 장 중반 이벤트 (1.0.1): 판 기록에서 중간에 그만둔 판이 많아(12판 중 5판), 장 한가운데에 굴곡을 넣음
//   처치 수 30%: 「습격」 — 둘레에 적 무리가 한꺼번에 나타나고, 모두 쓰러뜨리면 보물 상자
//   처치 수 55%: 1장에도 엘리트(중간 보스) 하나 — 2장부터는 원래 페이즈마다 중간 보스가 나옴
//   중간 보스 · 엘리트를 쓰러뜨리면 보물 상자 (TreasureChest)
// 무한 모드 · 튜토리얼 · 자동 촬영에서는 쉼 (1.0.3: 체험판도 정식판처럼 켬). StageManager 가 붙임
public class ChapterEvents : MonoBehaviour
{
    const float AmbushAt = 0.3f, EliteAt = 0.55f, AmbushTimeout = 50f, AmbushRadius = 40f;     // 1.0.1: 둘레 8 → 40칸 (훨씬 넓게 둘러쌈)

    EnemySpawner sp;
    int stage = -1;
    bool ambushDone, eliteDone;
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
            // 이어하기로 장 중간부터 시작하면 이미 지난 이벤트는 다시 하지 않음 (1.0.4~, 예전엔 이어하자마자 습격 · 엘리트가 한꺼번에)
            float at = sp.killedEnemy / (float)Mathf.Max(1, sp.BossKillTarget);
            ambushDone = at >= AmbushAt;
            eliteDone = at >= EliteAt && !resumeElite;
            ambush = null;
            foreach (TreasureChest c in chests) if (c != null) Destroy(c.gameObject);
            chests.Clear();
            // 이어하기: 진행 중이던 습격은 남은 수만큼 (배너 없이), 안 연 상자는 옆에 다시
            PlayerController owner = Hostile.Player;
            if (owner != null)
            {
                if (resumeAmbush > 0 && ambushDone)
                {
                    ambush = sp.SpawnAmbush(owner.transform.position, resumeAmbush, AmbushRadius);
                    ambushUntil = Time.time + AmbushTimeout;
                }
                for (int i = 0; i < resumeChests; i++)
                    chests.Add(TreasureChest.Spawn(owner.transform.position + (Vector3)(Random.insideUnitCircle.normalized * (2.5f + i))));
            }
            resumeAmbush = resumeChests = 0;
            resumeElite = false;
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
