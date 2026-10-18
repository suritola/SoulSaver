using UnityEngine;

// 난이도: 쉬움 → 보통 → 어려움 → 무한
public enum Difficulty { Easy = 0, Normal = 1, Hard = 2, Endless = 3 }

// 고른 난이도 · 잠금 해제 · 난이도별 수치 배율 (PlayerPrefs에 저장)
// 쉬움 = 원래 게임. 한 단계 오를 때마다 "이전 난이도를 세 판쯤 해 본 사람"에게 맞게 조금씩 어려워짐
// 무한 모드는 어려움 수치에서 시작해 시간이 지날수록 천천히 강해짐 (상한 있음)
public static class GameMode
{
    const string CurrentKey = "mode.difficulty";
    const string UnlockKey = "mode.unlocked";       // 0 = 쉬움만, 1 = 보통 · 어려움, 2 = 무한까지
    const string ClearedKey = "mode.cleared.";      // + 난이도 번호 → 1이면 클리어한 적 있음
    public const string UnlockNoticeKey = "mode.unlockNotice";     // 1.2~ 1 = 소급으로 무한 모드가 열려 메인 메뉴에서 알릴 차례 (ClaimRewards 가 알린 뒤 지움)

    // 1.2~ 메인 메뉴 (ClaimRewards): 소급 해금 알림이 남아 있으면 먼저 Load 를 거쳐 판정
    public static bool UnlockNoticePending { get { Load(); return Prefs.GetInt(UnlockNoticeKey, 0) == 1; } }

    public static readonly string[] Names = { "쉬움", "보통", "어려움", "무한" };

    // 난이도별 배율                                쉬움   보통   어려움  무한(시작)
    // 1.9.4: 어려울수록 판이 길어지기만 하던 것을 바꿈 — 체력 · 처치 수는 거의 같게,
    //        대신 적이 빠르고 · 공격을 자주 하고 · 예고가 짧고 · 탄이 빠르고 · 한꺼번에 많이 몰려옴 (피하고 맞히는 실력)
    static readonly float[] EnemyHp =            { 1f,   1.15f, 1.3f,   1.35f };
    static readonly float[] Damage =             { 1f,   1.25f, 1.5f,   1.2f };
    static readonly float[] EnemySpeed =         { 1f,   1.1f,  1.2f,   1.05f };
    static readonly float[] SkillCooldown =      { 1f,   0.75f, 0.55f,  0.85f };   // 작을수록 스킬을 자주 씀
    static readonly float[] Windup =             { 1f,   0.85f, 0.7f,   1f };      // 적 스킬 예고(기 모으기) 시간
    static readonly float[] ProjectileSpeed =    { 1f,   1.15f, 1.3f,   1f };      // 적 · 보스 탄속
    static readonly float[] SpawnInterval =      { 1f,   0.85f, 0.72f,  0.8f };
    static readonly int[] ExtraAlive =           { 0,    3,     6,      2 };
    static readonly float[] BossHp =             { 1f,   1.15f, 1.3f,   1.4f };    // 보스는 1~3분 안에 잡히게 (대신 보스 공격이 셈)
    static readonly float[] Gauge =              { 1f,   0.9f,  0.8f,   0.8f };    // 필살기 게이지 차는 속도
    static readonly float[] Reward =             { 1f,   1.05f, 1.1f,   1.3f };    // 경험치 · 코인 (많이 주면 어려움에서 레벨업 · 트리가 너무 빨리 다 참)
    // 템포: 보스까지 필요한 처치 수 · 적 생성 간격 (모든 난이도 20분 내외)
    // 1.8.9: 쉬움 8분 · 어려움 13분에 끝나던 판을 늘림 (처치 수 약 2.2배). 한 판의 경험치 · 코인 · 상점 횟수 총량은 그대로
    static readonly float[] Kills =              { 1.6f, 1.6f,  1.6f,   1f };
    // 보상 기준 (1.8.8까지의 처치 수 배율). 보스 · 중간 보스처럼 한 판에 나오는 수가 정해진 보상은 이 기준으로
    static readonly float[] RewardBasis =        { 0.7f, 0.8f,  0.9f,   1f };
    // 일반 적 영혼 조각을 이만큼만 (판이 길어져도 클리어 전에 영혼 트리가 다 차지 않게)
    const float KillShardRate = 0.6f;      // 1.0.1: 0.75 → 0.6 (영혼 트리가 천천히 차게, 보스 · 상자 보상은 그대로)
    static readonly float[] Tempo =              { 0.8f, 0.8f,  0.85f,  1f };     // 생성 간격 배율 (작을수록 빨리 나옴)
    // 1.1~ 포인트(영웅 · 스킨 재화) 배율: 어려울수록 더 벎 (무한 · 일일 도전은 처치 수가 많아 그대로)
    static readonly float[] PointRate =          { 1f,   1.3f,  1.5f,   1f };
    const float CursePointRate = 1.15f;     // 1.1~ 저주를 받은 장에서 얻는 포인트

    // 무한 모드 (1.8.7 조정): 초반이 너무 어렵고 후반이 지루하던 곡선을 바꿈
    //   처음 3분은 0.7배에서 1배로 워밍업, 그 뒤로는 멈추지 않고 가속 (10분 ≈ 2배, 20분 ≈ 3.6배, 30분 ≈ 6배)
    const float EndlessWarmup = 180f;
    const float EndlessStartMul = 0.7f;
    const float EndlessLinear = 0.06f;        // 1분마다
    const float EndlessQuad = 0.0035f;        // 1분² 마다 (갈수록 빨라짐)
    const float EndlessRampMax = 10f;

    static bool loaded;
    static Difficulty current;
    static int unlocked;

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        unlocked = Mathf.Clamp(Prefs.GetInt(UnlockKey, 0), 0, 2);
        // 1.2~ 난이도 사다리: 무한 모드는 「보통」 클리어로도 열림 → 이미 보통을 깼지만 어려움은 못 깬 저장은 소급 해금 + 메인 메뉴에서 한 번 알림
        //   (mode.unlocked 0 · 1 · 2 의 뜻은 그대로, 올리는 조건만 바뀜 · 체험판은 쉬움만이라 해당 없음)
        // 1.3.3~ 쉬움만 클리어해도 무한까지 열리므로 쉬움을 깬 저장도 소급 해금
        if (unlocked < 2 && !Demo.On && (Prefs.GetInt(ClearedKey + (int)Difficulty.Easy, 0) == 1 || Prefs.GetInt(ClearedKey + (int)Difficulty.Normal, 0) == 1))
        {
            unlocked = 2;
            Prefs.SetInt(UnlockKey, unlocked);
            Prefs.SetInt(UnlockNoticeKey, 1);
            Prefs.Save();
        }
        current = (Difficulty)Mathf.Clamp(Prefs.GetInt(CurrentKey, 0), 0, 3);
        if (!IsUnlocked(current)) current = Difficulty.Easy;
    }

    // 자동 테스트가 난이도를 정할 때 (저장값을 건드리지 않음)
    public static Difficulty? Override;

    public static Difficulty Current
    {
        get
        {
            if (Override.HasValue) return Override.Value;
            // 트레일러 촬영 · 배치 모드(자동 테스트)는 항상 원래 게임(쉬움)
            if (GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) return Difficulty.Easy;
            Load();
            return current;
        }
        set
        {
            Load();
            if (!IsUnlocked(value)) return;
            current = value;
            Prefs.SetInt(CurrentKey, (int)value);
            Prefs.Save();
        }
    }

    public static bool IsEndless => Current == Difficulty.Endless;

    public static bool IsUnlocked(Difficulty d)
    {
        Load();
        if (Demo.On) return d == Difficulty.Easy;        // 체험판은 쉬움만
        return d == Difficulty.Easy || (d <= Difficulty.Hard && unlocked >= 1) || (d == Difficulty.Endless && unlocked >= 2);
    }

    public static bool HasCleared(Difficulty d) => Prefs.GetInt(ClearedKey + (int)d, 0) == 1;

    // 3장 보스를 쓰러뜨렸을 때. 새로 열린 난이도가 있으면 그 이름 (없으면 null)
    public static string OnCleared(Difficulty d)
    {
        Load();
        if (GameInput.Auto || (Application.isBatchMode && !Override.HasValue)) return null;
        Prefs.SetInt(ClearedKey + (int)d, 1);
        string opened = null;
        // 1.3.3~ 쉬움을 클리어하면 보통 · 어려움 · 무한이 한꺼번에 열림 (예전 1.2: 쉬움 → 보통 · 어려움, 보통 → 무한)
        if (d == Difficulty.Easy && unlocked < 2) { opened = unlocked < 1 ? "보통 · 어려움 · 무한" : "무한"; unlocked = 2; }
        // 보통 · 어려움 클리어도 그대로 무한을 엶 (옛 저장 · 어려움 클리어의 보상은 영혼의 무게 · 포인트 ×1.5)
        if ((d == Difficulty.Normal || d == Difficulty.Hard) && unlocked < 2) { unlocked = 2; opened = "무한"; }
        Prefs.SetInt(UnlockKey, unlocked);
        Prefs.Save();
        return opened;
    }

    // ================================================================= 무한 모드 경과
    static float endlessStart;
    static bool endlessClockOn;
    // 1.2.3~ 이어하기는 저장한 경과 시간부터
    // 1.2.8~ 시작 시각은 음수일 수 있음 (게임을 켜자마자 이어하면 Time.time < 저장한 경과 시간) — 예전엔 「시작 시각 ≥ 0」으로 켜짐을 봐서
    //   실제 시간이 저장한 시간을 따라잡을 때까지 시계가 00:00 에 멈췄고 15분 · 적 증가 · 이정표도 0초로 계산됐음 → 켜짐은 따로 플래그로
    public static void StartEndlessClock(float elapsed = 0f)
    {
        endlessStart = Time.time - Mathf.Max(0f, elapsed);
        endlessClockOn = true;
    }
    // 무한 모드 판이 진행 중일 때만 (메뉴에서는 0)
    public static float EndlessSeconds => IsEndless && endlessClockOn && EndlessMode.Instance != null ? Mathf.Max(0f, Time.time - endlessStart) : 0f;
    static float Ramp
    {
        get
        {
            if (!IsEndless) return 1f;
            float s = EndlessSeconds, m = s / 60f;
            float warm = Mathf.Lerp(EndlessStartMul, 1f, Mathf.SmoothStep(0f, 1f, s / EndlessWarmup));
            return Mathf.Min(EndlessRampMax, warm * (1f + EndlessLinear * m + EndlessQuad * m * m));
        }
    }
    // 잡몹용 강도 (1.0.2): 예전 곡선은 처음 3분 동안 0.7 → 1 로 확 오른 뒤 직선 + 제곱이라 「극초반 쉬움 → 초반 너무 어려움 → 갑자기 확 강해짐」
    // → 꺾이는 데 없는 지수 곡선 하나: 처음엔 쉽고, 1분마다 약 11%씩 곱으로 불어나 갈수록 걷잡을 수 없이 어려워짐
    //   0분 0.6 · 3분 0.8 · 5분 1.0 · 10분 1.7 · 15분 2.8 · 20분 4.6 · 25분 7.7 · 약 27분에 최대 10
    // 체력 · 주는 피해 · 스킬 간격 · 나오는 간격 · 한 화면 수에 씀, 보스 체력과 보스 · 중간 보스 주기는 그대로 Ramp
    const float MobStart = 0.6f, MobGrowth = 0.102f;      // 1분마다 e^0.102 ≈ 1.107배
    static float MobRamp => IsEndless ? Mathf.Min(EndlessRampMax, MobStart * Mathf.Exp(MobGrowth * EndlessSeconds / 60f)) : 1f;

    // 최종 국면 (1.0.2 · 1.0.4~ 30분 → 15분, EndlessMode.FinalAt): 그때부터 적이 기하급수로 — 1분마다 2배씩 더 자주 · 더 많이 (약 4분에 최대 16배)
    //   1.0.4~ 개발자가 나온 뒤로는 잡몹 자체가 나오지 않음 (EnemySpawner.NoMobs)
    const float SurgeGrowth = 0.693f, SurgeMax = 16f;
    static float Surge => IsEndless && EndlessSeconds >= EndlessMode.FinalAt ? Mathf.Min(SurgeMax, Mathf.Exp(SurgeGrowth * (EndlessSeconds - EndlessMode.FinalAt) / 60f)) : 1f;

    // 1.1.7~ 무한 모드 초반 완화: 처음 5분은 잡몹 체력 ×0.5 · 받는 피해 ×0.6 · 보스 체력 ×0.5 · 적 스킬 간격 ×1.5 로 시작하고
    //   5분 → 9분 사이에 SmoothStep 으로 1 에 합류 (9분 뒤 · 최종 국면 · 개발자 · 엄마 · 한계 돌파는 그대로)
    //   1.1.4 잡몹 +40% · 1.1.6 피해 ×0.8 이 무한 초반에 겹쳐 처치 시간이 1.1.3 대비 ×1.75 였음 (이야기 모드의 1장 완화는 무한에 걸리지 않음)
    const float EarlyHold = 300f, EarlyEnd = 540f;
    const float EarlyHp = 0.5f, EarlyHurt = 0.6f, EarlyBoss = 0.5f, EarlySkill = 1.5f;
    static float EarlyT => IsEndless ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(EarlyHold, EarlyEnd, EndlessSeconds)) : 1f;
    static float Early(float mul) => Mathf.Lerp(mul, 1f, EarlyT);

    // 무한 모드 강도 (1 = 기본). 보스 간격 · 중간 보스 주기 등에 씀
    public static float EndlessIntensity => Ramp;

    // ================================================================= 배율 (지금 난이도 기준)
    // 1.8.7: 모든 난이도를 30% 어렵게 (적 · 보스 체력, 적이 주는 피해). 생성 속도 · 이동 속도는 그대로
    const float Harder = 1.3f;

    static int I => (int)Current;
    // 일일 도전의 그날 규칙 (DailyChallenge.Mul, 일일 도전이 아니면 1)
    static float D(DailyStat s) => DailyChallenge.Mul(s) * SoulBrand.Mul(s);     // 1.2~ 영혼 각인도 같은 배율 (각인 없는 판이면 1)
    public static float EnemyHpMul => EnemyHp[I] * MobRamp * Harder * D(DailyStat.EnemyHp) * Curse.HpMul * SoulWeight.EnemyHpMul * Early(EarlyHp);     // 저주 제단 (1.0.1) · 영혼의 무게 1 (1.2~)
    // 1.1.6~ 「전체적인 데미지 밸런스 하향」: 플레이어가 적 · 보스에게 주는 모든 피해(평타 · 스킬 · 필살기 · 진화 · 초월 · 유물)에 공통으로 곱함
    //   (EnermyController · bosss.TakeDamage 입구 한 곳씩) — 피해 숫자 표시도 줄어든 값
    public const float PlayerDamageMul = 0.8f;
    public static float DamageMul => Damage[I] * Mathf.Lerp(1f, MobRamp, 0.5f) * Harder * D(DailyStat.EnemyDamage) * Early(EarlyHurt);
    public static float EnemySpeedMul => EnemySpeed[I] * D(DailyStat.EnemySpeed);
    public static float WindupMul => Windup[I];
    public static float ProjectileSpeedMul => ProjectileSpeed[I];
    public static float SkillCooldownMul => SkillCooldown[I] / Mathf.Lerp(1f, MobRamp, 0.3f) * D(DailyStat.EnemySkill) * Early(EarlySkill);
    public static float SpawnIntervalMul => SpawnInterval[I] * Tempo[I] / Mathf.Lerp(1f, MobRamp, 0.4f) / Surge * D(DailyStat.Spawn) / (IsEndless ? EndlessMode.MobSpawnBoost : 1f);
    // 보스 · 페이즈까지 필요한 처치 수 배율
    public static float KillsMul => Kills[I];
    public static int ScaleKills(int kills) => kills >= int.MaxValue / 2 ? kills : Mathf.Max(1, Mathf.RoundToInt(kills * Kills[I]));
    // 무한 모드는 강도가 오를수록 한 번에 나오는 적도 늘어남
    public static int ExtraAliveCount => ExtraAlive[I] + (IsEndless ? Mathf.FloorToInt(Mathf.Max(0f, MobRamp - 1f) * 4f + (Surge - 1f) * 6f) : 0) + SoulWeight.ExtraAlive;     // 최종 국면 최대 +90 · 영혼의 무게 2~ (1.2~, 무한 모드는 0)
    public static float BossHpMul => BossHp[I] * Ramp * Harder * D(DailyStat.BossHp) * Curse.HpMul * Early(EarlyBoss);
    public static float GaugeMul => Gauge[I] * D(DailyStat.Gauge);
    // 일반 적 한 마리 보상 (처치 수가 늘어난 만큼 줄여 한 판 총량을 맞춤)
    public static float RewardMul => Reward[I] / Kills[I] * D(DailyStat.Reward) * Curse.RewardMul;
    // 보스 · 중간 보스 보상 (처치 수와 상관없이 나오는 수가 정해져 있음)
    public static float FixedRewardMul => Reward[I] / RewardBasis[I] * D(DailyStat.Reward) * Curse.RewardMul;
    // 1.1~ 포인트 배율: 난이도 × 이번 장 저주 (1.2~ 다른 배율과 곱하는 곳은 Point.TotalMul)
    public static float PointMul => PointRate[I] * (Curse.Active ? CursePointRate : 1f);
    public static float PointRateOf(Difficulty d) => PointRate[Mathf.Clamp((int)d, 0, PointRate.Length - 1)];     // 메뉴 난이도 설명용
    // 처치 수가 예전보다 몇 배인지 (떠돌이 상점 간격 등 처치 수로 세는 것을 맞출 때)
    public static float KillStretch => Kills[I] / RewardBasis[I];
    // 일반 적 영혼 조각 배율 (예전 한 마리 몫 기준, 무한 모드는 그대로)
    public static float KillShardMul(bool countsTowardBoss) => IsEndless ? 1f : KillShardRate / (countsTowardBoss ? KillStretch : 1f);

    // 소수 보상을 확률로 반올림 (2.4 → 40% 확률로 3, 아니면 2): 여러 번 모으면 평균이 맞음
    public static int RoundRandom(float v)
    {
        int n = Mathf.FloorToInt(v);
        return n + (Random.value < v - n ? 1 : 0);
    }

    public static string Name(Difficulty d) => Loc.T(Names[(int)d]);
}
