using UnityEngine;

// 장 순서 (2.1.1~): 초원 → 지하 묘역 → 불타는 지옥 → 영혼의 심연
// 스테이지 번호(맵 · 적 구성 · 음악 · 도감)는 그대로 두고 "몇 번째 장인지"만 바꿈 (StageManager · EnemySpawner)
// 원래 3장이던 초원이 1장으로 오면 너무 세므로, 적 체력 · 주는 피해 · 보상 · 보스 체력을 그 자리(몇 번째 장)의 원래 세기에 맞춤
// 체험판(1장 초원 · 킹 슬라임에서 끝)은 정식판과 같은 순서 · 무한 모드는 예전 순서 그대로
public static class Chapters
{
    // 스테이지 번호: 0 지하 묘역 · 1 불타는 지옥 · 2 초원 · 3 영혼의 심연
    static readonly int[] Normal = { 2, 0, 1, 3 };
    static readonly int[] Classic = { 0, 1, 2, 3 };
    public static bool IsClassic => GameMode.IsEndless || GameInput.TrailerRunning || Application.isBatchMode;     // 촬영 · 화면 점검 도구는 장을 직접 건너뜀
    static int[] Order => IsClassic ? Classic : Normal;

    public static readonly string[] StageNames = { "지하 묘역", "불타는 지옥", "초원", "영혼의 심연" };

    public static int First => Order[0];

    // 이 스테이지가 몇 번째 장인지 (0부터) · 다음 장의 스테이지 (마지막이면 -1)
    public static int SlotOf(int stage)
    {
        int[] o = Order;
        for (int i = 0; i < o.Length; i++) if (o[i] == stage) return i;
        return stage;
    }
    public static int Next(int stage)
    {
        int s = SlotOf(stage) + 1;
        return s < Order.Length ? Order[s] : -1;
    }
    public static int Number(int stage) => SlotOf(stage) + 1;

    // 「2장 · 지하 묘역」
    public static string Title(int stage) =>
        Loc.T("{0}장").Replace("{0}", Number(stage).ToString()) + " · " + Loc.T(StageNames[Mathf.Clamp(stage, 0, StageNames.Length - 1)]);

    // ---------------------------------------------------------------- 세기 맞추기
    // 장마다 원래 세기 (스테이지 번호 순서): 잡몹 평균 체력 · 적이 주는 피해 · 처치 보상 · 보스 체력(킹 슬라임은 2.2.1~ 예전 분열 합계를 한 몸에 · 프리팹도 5950) · 보스까지 처치 수
    static readonly float[] Hp = { 9f, 51f, 53f, 63f };
    static readonly float[] Hurt = { 8.3f, 16.6f, 17.2f, 19.6f };
    static readonly float[] Reward = { 32f, 51f, 73f, 85f };
    static readonly float[] Boss = { 480f, 2400f, 5950f, 4200f };
    static readonly int[] Kills = { 60, 80, 80, 85 };

    // 지금 자리의 세기 ÷ 원래 자리의 세기 (예전 순서면 1)
    static float Ratio(float[] t, int stage)
    {
        if (IsClassic || stage < 0 || stage >= t.Length) return 1f;
        return t[Mathf.Clamp(SlotOf(stage), 0, t.Length - 1)] / t[stage];
    }

    // ---------------------------------------------------------------- 앞쪽 장 완화 (2.1.5)
    // 초원이 1장으로 온 뒤 체력 · 피해만 맞춰서는 너무 어려웠음 (적이 나오는 간격 · 한 화면 최대 수 · 속도 · 스킬은 원래 3장 그대로였음)
    // 2.1.6: 그래도 1장이 어려워서 한 단계 더 낮춤
    // → 1장은 확 낮추고 2장은 조금, 3장부터 원래 세기 (자리 순서: 1장 · 2장 · 3장 · 4장)
    // 2.2.0: 곡선을 더 가파르게 — 1 · 2장은 더 쉽게, 3장은 원래보다 조금 · 4장은 확실히 세게
    // 1.0.2: 2장부터 잡몹(중간 보스 포함)이 너무 빨리 녹아서 두 배로 (0.8 · 1.08 · 1.2 → 1.6 · 2.16 · 2.4)
    // 1.1.3 밸런스 점검: 1장 → 2장 절벽이 너무 가팔랐음 (잡몹 평균 체력 6 → 135 ×22 · 받는 피해 ×4, 기록 사망 13건 중 8건이 2장 · 시뮬 9영웅 중 8이 2장 사망)
    //   → 1장 조금 올리고(0.42 → 0.55) 2 · 3장 내림 (1.6 → 1.2 · 2.16 → 2.0): 체력 8 → 101 → 188 → 274, 받는 피해 0.8 → 0.65 · 1.08 → 1.0
    static readonly float[] EaseHp = { 0.55f, 1.2f, 2.0f, 2.4f };
    static readonly float[] EaseHurt = { 0.42f, 0.65f, 1.0f, 1.2f };
    // 1.0.1: 3장부터 보스가 약하게 느껴져서 확 올림 (1장 264 < 2장 2112 < 3장 4760 → 6843 < 4장 5040 → 6930)
    // 1.1.3: 2장 리치 왕이 결계 15초 · 소환까지 더해 가장 긴 보스전(보통 약 3분)이라 0.88 → 0.75 (2112 → 1800)
    //        4장 거울의 군주는 공식 계산(약 2.5분)과 시뮬(약 40초)이 엇갈려 작게만 1.65 → 1.8 (6930 → 7560)
    static readonly float[] EaseBoss = { 0.55f, 0.75f, 1.15f, 1.8f };
    static readonly float[] EaseReward = { 1.15f, 1.08f, 1f, 1f };       // 앞쪽 장은 레벨이 조금 빨리 오름
    // 1.1~ 첫 3분이 비어 보여서 1장만 더 빽빽하게 (간격 1.9 → 1.3 · 최대 수 0.5 → 0.8, 적 체력 0.42배는 그대로 · 영혼 부스러기 무리는 SwarmWave)
    static readonly float[] EaseSpawn = { 1.3f, 1.2f, 0.95f, 0.88f };     // 적이 나오는 간격 배율 (클수록 드묾)
    static readonly float[] EaseAlive = { 0.8f, 0.8f, 1.05f, 1.12f };     // 한 화면 최대 적 수 배율
    static readonly float[] EaseSpeed = { 0.75f, 0.9f, 1f, 1.04f };
    static readonly float[] EaseSkill = { 1.8f, 1.2f, 0.95f, 0.88f };      // 적 · 보스 스킬 간격 배율 (클수록 드묾)

    // 보스만 (1.0.1): 공격 피해 배율 · 스킬 간격 배율 — 3장부터 급격하게 (잡몹 EaseSkill 과 따로)
    // 1.1.3: 2 · 3장 보스는 거의 모든 공격이 한 대 상한(최대 체력 40%, PlayerController.BossHitShare)에 걸려 난이도 · 피해 감소가 보스전에 아무 영향이 없었음
    //   → 2장 1 → 0.85 · 3장 1.3 → 1.1 로 작은 패턴은 상한 아래로 (리치 왕 영혼구 보통 52 → 34, 쉬움 27)
    static readonly float[] EaseBossPower = { 1f, 0.85f, 1.1f, 1.45f };
    static readonly float[] EaseBossSkill = { 1.8f, 1.2f, 0.72f, 0.64f };

    // 1.1~ 판 후반 보상(3장 각성 · 초월 카드)으로 세진 만큼 3 · 4장 잡몹(중간 보스 포함) 체력을 조금 더 — 3 · 4장 각각 3~4분 목표 (보스 체력은 그대로)
    static readonly float[] LateHp = { 1f, 1f, 1.12f, 1.15f };

    static float Ease(float[] t, int stage) => IsClassic || stage < 0 || stage >= t.Length ? 1f : t[Mathf.Clamp(SlotOf(stage), 0, t.Length - 1)];

    // 1.1.3 빌드 뒤: 「잡몹 체력 20~40% 올려」 — 잡몹 · 엘리트 · 중간 보스(EnermyController) 체력을 장마다 1장 +20% · 2장 +25% · 3장 +30% · 4장 +40%, 무한 모드 +40%
    //   체험판 · 클래식 순서(IsClassic)에도 걸리게 Ease 와 따로 (보스는 그대로)
    static readonly float[] MobHpUp = { 1.2f, 1.25f, 1.3f, 1.4f };
    public static float MobHpBoost(int stage) => GameMode.IsEndless ? 1.4f : stage < 0 ? 1f : MobHpUp[Mathf.Clamp(IsClassic ? stage : SlotOf(stage), 0, MobHpUp.Length - 1)];
    // 1.4.7~ 사용자 「전체적으로 적들의 체력이 너무 낮다」: 이야기 모드 잡몹(중간 보스 포함) · 보스 모두 1장 +30% · 2장 +40% · 3장 +70% · 4장 +100%
    //   무한 모드는 잡몹 +100% · 보스 +80% (개발자 · 엄마는 EndlessMode.SummonDeveloper 가 체력을 따로 정하고 difficultyApplied 로 boss.Start 배율을 건너뛰어 그대로)
    static readonly float[] StoryHpUp = { 1.3f, 1.4f, 1.7f, 2f };
    const float EndlessMobHpUp = 2f, EndlessBossHpUp = 1.8f;
    static float StoryHpUpOf(int stage) => stage < 0 ? 1f : StoryHpUp[Mathf.Clamp(IsClassic ? stage : SlotOf(stage), 0, StoryHpUp.Length - 1)];
    public static float HpUp147(int stage, bool boss) => GameMode.IsEndless ? (boss ? EndlessBossHpUp : EndlessMobHpUp) : StoryHpUpOf(stage);
    public static float EnemyHpMul(int stage) => Ratio(Hp, stage) * Ease(EaseHp, stage) * Ease(LateHp, stage) * MobHpBoost(stage) * HpUp147(stage, false);
    public static float RewardMul(int stage) => Ratio(Reward, stage) * Ease(EaseReward, stage);
    public static float BossHpMul(int stage) => Ratio(Boss, stage) * Ease(EaseBoss, stage) * HpUp147(stage, true);
    // 플레이어가 받는 모든 피해 (몸통 · 적 스킬 · 보스 스킬 · 결계): 지금 장 기준
    public static float HurtMul => StageManager.Instance != null ? HurtMulOf(StageManager.Instance.CurrentStage) : 1f;
    public static float HurtMulOf(int stage) => Ratio(Hurt, stage) * Ease(EaseHurt, stage);
    public static float SpawnIntervalMul(int stage) => Ease(EaseSpawn, stage);
    public static int MaxAlive(int stage, int count) => Mathf.Max(3, Mathf.RoundToInt(count * Ease(EaseAlive, stage)));
    public static float EnemySpeedMul(int stage) => Ease(EaseSpeed, stage);
    public static float SkillCooldownMul => Ease(EaseSkill, CurrentStage);
    public static float BossSkillCooldownMul => Ease(EaseBossSkill, CurrentStage);
    public static float BossPowerMul => Ease(EaseBossPower, CurrentStage);
    public static int BossKills(int stage, int original) => IsClassic || stage < 0 || stage >= Kills.Length ? original : Kills[Mathf.Clamp(SlotOf(stage), 0, Kills.Length - 1)];
    public static int CurrentStage => StageManager.Instance != null ? StageManager.Instance.CurrentStage : 0;
}
