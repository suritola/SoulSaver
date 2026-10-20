
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // =====================================
    // 사운드
    // =====================================

    [Header("사운드")]

    public AudioClip shotSound;
    public AudioClip hitSound;
    public AudioClip chargeSound;
    public AudioClip reloadSound;
    public AudioClip getCoin;

    // =====================================
    // 레벨 설정
    // =====================================

    [Header("레벨 설정")]

    public int level = 1;
    public float nowEXP = 0f;
    public float needEXP;

    // =====================================
    // 기본 설정
    // =====================================

    [Header("기본 설정")]

    public float speed = 3f;

    public GameObject bulletPrefab;

    public int isShop = 0;
    public int bonusCoin = 0;

    [Header("일반 발사 설정")]

    public float ShootSpeed = 0.8f;

    public float damage = 1f;

    public int pene = 1;

    public bool blood;

    public float getHP = 0f;

    public int multiShot = 1;

    // 멀티샷 발사 수별 한 발당 피해 비율 (1발 100%, 2발 70%, 3발 55% ...)
    // 전부 맞히면 총 피해는 100% / 140% / 165% / 180% / 200%
    public float[] multiShotDamageRates = { 1f, 0.7f, 0.55f, 0.45f, 0.4f };

    public float MultiShotDamageRate(int shots)
    {
        if (multiShotDamageRates == null || multiShotDamageRates.Length == 0) return 1f;
        return multiShotDamageRates[Mathf.Clamp(shots - 1, 0, multiShotDamageRates.Length - 1)];
    }

    public float knockBack = 0.3f;

    // =====================================
    // 탄약 / 재장전
    // =====================================

    [Header("탄약 / 재장전")]

    public int MaxBullet = 6;
    public int NowBullet = 6;

    public float reload = 0f;

    public float reloadTime = 3f;

    private bool isReloading = false;

    // =====================================
    // 체력
    // =====================================

    [Header("체력")]

    public float PlayerMaxHealth = 100f;
    public float PlayerHealth = 100f;

    // =====================================
    // 스킬 설정
    // =====================================

    [Header("스킬 설정")]

    public float targetRange = 8f;

    // 타겟팅 스킬로 조준할 수 있는 기본 적 수 (스킬 강화 상점에서 늘어남)
    public int maxTargets = 6;
    Color baseEffectColor;
    bool effectColorSaved;

    // 코인 자석 (레벨업 능력): 이 거리 안의 코인을 끌어옴
    [HideInInspector] public float coinMagnetRange = 0f;
    internal const float BaseCoinPull = 2.5f;        // 카드 없이도 끌어오는 코인 범위 (2.1.8) · 1.1.9~ 획득 범위 고리(PickupRing)도 씀

    public float targetInterval = 0.2f;

    public float shootDelay = 0.08f;

    public int ShotsPerTarget = 1;

    public float skillDamage = 500f;

    public float TimeSet = 0.3f;

    public float normalZoom = 15f;

    public float maxZoom = 30f;

    public float MaxCharge = 240f;

    public float NowCharge = 0f;

    public float Skill_setTime = 0.5f;

    // =====================================
    // 화면 효과
    // =====================================

    [Header("화면 효과")]

    public Image skillEffectPanel;

    public float screenFadeAlpha = 0.6f;

    public float screenFadeSpeed = 5f;

    // =====================================
    // 타겟 마크
    // =====================================

    [Header("타겟 마크")]

    public GameObject targetMarkPrefab;

    // =====================================
    // 스킬 게이지
    // =====================================

    [Header("필살기 게이지")]

    public SkillGauge skillGauge;

    // =====================================
    // 내부 변수
    // =====================================

    private bool isSkillUsing = false;

    // 특수 능력 (지옥 입장 시 선택)
    [HideInInspector] public SpecialAbilities special;
    // 희생의 계약 같은 일시 강화
    [HideInInspector] public float damageMultiplier = 1f;
    [HideInInspector] public float fireRateMultiplier = 1f;
    // 분신 사용 중 반투명 등
    [HideInInspector] public float bodyAlpha = 1f;
    // 탄약 표시 대신 보여줄 글자 (과열, 충전 등)
    [HideInInspector] public string ammoTextOverride;

    public bool CanShoot => !IsSkillUsing && !isReloading && NowBullet > 0;
    // 총알 한 발이 채우는 스킬 게이지 비율
    public const float GaugeRate = 0.35f;
    // 메뉴로 게임이 멈췄는지 (타겟팅 스킬의 느린 시간은 멈춘 것이 아님)
    public static bool IsPaused => Time.timeScale == 0f;
    // 보스 처치 연출 중 (1.0.2): 이동 · 공격 · 스킬 · 영혼 트리 입력을 모두 받지 않고 연출만 봄 (EnemySpawner.BossFinale)
    public static bool Cinematic;
    // 타겟팅 스킬 사용 중 + 끝난 뒤 자동 연사 중 (이때는 직접 쏠 수 없음)
    public bool IsSkillUsing => isSkillUsing || isVolleying;
    public bool IsAiming => isSkillUsing;       // 시간을 늦춘 조준 중 (일제 사격이 날아가는 중은 아님)
    public int AimedCount => targets.Count;      // 1.0.4~: 지금 조준한 적 수 (튜토리얼 필살기 단계)
    private bool isVolleying;
    public Camera MainCamera => mainCamera;

    private float nextTargetTime = 0f;

    private float nextShootTime = 0f;

    private List<EnermyController> targets =
        new List<EnermyController>();
    // 1.1.3: 조준한 보스 (예전엔 「enermy」 태그만 조준해 보스 단독 구간 — 1장 킹 슬라임 · 개발자 · 엄마 — 에서 기본 필살기가 아무것도 맞히지 못했음)
    private readonly List<bosss> bossTargets = new List<bosss>();

    private List<GameObject> targetMarks =
        new List<GameObject>();

    private Vector3 move;
    public Vector3 MoveInput => move;       // 1.2.3~ 지형 장애물 끼임 방지 (EndlessTerrain)

    private Animator animator;

    private SpriteRenderer spriteRenderer;

    private AudioSource audioSource;

    private Camera mainCamera;

    // 사격 후 잠깐 동안 쏜 방향을 바라봄
    public float faceShotTime = 0.35f;

    private float faceLockUntil = 0f;

    // =====================================
    // 시작
    // =====================================

    // 이동 속도 상한 (1.0.1): 빨라지는 효과(그림자 숙련 · 연쇄 처치 · 도망자의 발걸음 · 상점 · 물약 …)가 곱으로 쌓여
    // 후반 도적이 주체할 수 없이 빨라졌음 → 기본 속도의 1.3배까지는 그대로, 넘는 몫은 40%만, 최대 1.7배 (둔화는 그대로 적용)
    const float SpeedSoftCap = 1.3f, SpeedOverRate = 0.4f, SpeedHardCap = 1.7f;
    float baseMoveSpeed = -1f;
    public float BaseMoveSpeed => baseMoveSpeed > 0f ? baseMoveSpeed : speed;     // 엄마 결계 「청소기」 끄는 힘의 기준 (걸어서 빠져나갈 수 있게)
    float CappedSpeed(float raw)
    {
        if (baseMoveSpeed <= 0f) return raw;
        float r = raw / baseMoveSpeed;
        if (r > SpeedSoftCap) r = Mathf.Min(SpeedHardCap, SpeedSoftCap + (r - SpeedSoftCap) * SpeedOverRate);
        return baseMoveSpeed * r;
    }

    void Start()
    {
        baseScaleY = Mathf.Abs(transform.lossyScale.y);     // 1.4.4~ 판정 크기 배율 기준 (BodyScale)
        baseMoveSpeed = speed * CharacterData.Current.move;        // 영웅 기본 이동 배율까지 (CharacterKit 이 곱하는 값)
        EnermyController.Killed += HealOnKill;
        SpriteOutline.Player(gameObject);       // 1.8.7 밝은 윤곽선 (적 무리 속에서도 잘 보이게)

        needEXP = 100f;
        isShop = 0;

        NowBullet = MaxBullet;
        PlayerHealth = PlayerMaxHealth;
        NowCharge = 0f;

        animator = GetComponent<Animator>();

        spriteRenderer = GetComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();

        // 들고 있는 무기 모습 · 발사 연출
        PlayerLook.Attach(this);
        // 거너가 아닌 캐릭터: 스탯 · 몸 그림 · 평타 · 우클릭 스킬 (CharacterData)
        CharacterKit.Attach(this);
        // 거너: 공격력 배율 · 장전 1.8 → 1.3초(2.1.0, 1대 상대 피해가 가장 낮았음) → 1.5초(2.1.1)
        if (CharacterData.IsGunner)
        {
            damage *= CharacterData.Current.damage;
            reloadTime = Mathf.Min(reloadTime, 1.5f);         // 2.1.1: 1.3 → 1.5초 (1 · 2장에서 너무 셌음)
        }

        mainCamera = Camera.main;

        if (mainCamera != null) mainCamera.orthographicSize = normalZoom;

        if (skillGauge == null) skillGauge = FindFirstObjectByType<SkillGauge>();

        if (skillEffectPanel != null)
        {
            UnityEngine.Color color = skillEffectPanel.color;
            color.a = 0f;
            skillEffectPanel.color = color;
        }
    }



    // 레벨이 오를수록 조금씩 더 필요 (100, 150, 200 ...)
    // 1.0.1: 최고점은 그대로 두고 성장 속도만 늦춤 — 레벨마다 +50 → +65 (초반 +15%, 레벨 20쯤 +30%)
    // 1.4.2~ 무한 모드(일일 도전 포함)만 레벨 25 다음부터 요구량이 점점 늘어 35~49 는 ×4.5 (레벨 50 초월 막대 3,300 은 그대로)
    //   10분에 레벨 50 · 15분에 트리 완성이라 최후의 시간이 「다 큰 채 같은 보스를 오래」가 되던 것을 → 성장의 끝을 판 끝 쪽(약 23분)으로 옮김
    //   (docs/review/2026-10-19-무한-30분-성장속도-기획.md A1 · 이야기 모드 · 메뉴는 그대로)
    const int EndlessExpFrom = 25;
    const float EndlessExpMax = 4.5f, EndlessExpRamp = 10f;
    public static float NeedExp(int level) => (50 + level * 65) * EndlessExpMul(level);
    // 1.4.7~ 한계 돌파 레벨 50~99 는 ×6 (49 의 ×4.5 보다 늘 느리게 · 51 약 20,200 → 99 약 38,900 · 사용자 「49레벨때보단 더 느려지게」)
    const float BeyondExpNeedMul = 6f;
    static float EndlessExpMul(int level) => !GameMode.IsEndless || EndlessMode.Instance == null || level <= EndlessExpFrom ? 1f
        : level >= LevelShop.BaseLevelCap ? (EndlessMode.LevelBeyond && level < LevelShop.BeyondLevelCap ? BeyondExpNeedMul : 1f)     // 한계 돌파 전 50 · 100 의 초월 막대는 예전 그대로
        : 1f + (EndlessExpMax - 1f) * Mathf.Clamp01((level - EndlessExpFrom) / EndlessExpRamp);

    // 1.4.2~ 요구량의 기준 레벨: 무한 모드는 지금 레벨 + 쌓아 둔 레벨업 (LevelShop.ExpLevel) — 카드를 미뤄 두면 싼 요구량으로 계속 모이던 길을 막음
    //   이야기 모드는 예전처럼 고른 레벨 (숙련도 무료 카드로 레벨이 경험치보다 앞설 수 있어서)
    LevelShop expShop;
    int ExpLevel
    {
        get
        {
            if (GameMode.IsEndless && EndlessMode.Instance != null)
            {
                if (expShop == null) expShop = FindFirstObjectByType<LevelShop>(FindObjectsInactive.Include);      // 창이 꺼져 있어도 찾음
                if (expShop != null) return expShop.ExpLevel;
            }
            return level;
        }
    }

    public void addDamage(int a)
    {
        damage += a;
    }

    // =====================================
    // Update
    // =====================================

    void Update()
    {
        GuardScreenTint();
        ReloadWatchdog();       // 장전 코루틴이 끊겨 장전 상태만 남으면 풂 (재장전 부분)
        DamageSource.Current = DamageSource.Basic;      // 재장전 · 평타 (아래 우클릭 부분은 필살기)
        try { UpdateInput(); }
        finally { DamageSource.Current = null; }
    }

    // 코루틴도 시작할 때의 피해 출처(평타 · 필살기)를 이어 감 (RunStats)
    public new Coroutine StartCoroutine(IEnumerator routine) => base.StartCoroutine(DamageSource.Keep(routine));

    void UpdateInput()
    {

        needEXP = NeedExp(ExpLevel);        // 1.4.2~ 무한 모드는 쌓아 둔 레벨업까지 센 레벨 (위 ExpLevel)

        // 생명의 샘: 초당 체력 회복
        if (regenPerSecond > 0f && PlayerHealth > 0f && PlayerHealth < PlayerMaxHealth)
            PlayerHealth = Mathf.Min(PlayerMaxHealth, PlayerHealth + regenPerSecond * Time.deltaTime);

        // 코인 자석: 주변 코인이 날아옴 (2.1.8: 카드가 없어도 기본으로 2.5칸은 끌어옴, 카드는 거기에 더함)
        // 1.0.4~: 코인은 물리 트리거 대신 CoinTag 가 한 번에 끌어오고 주움 (코인이 많아지면 프레임이 떨어지던 문제)
        float pull = coinMagnetRange + BaseCoinPull;
        if (Time.timeScale > 0f)
        {
            if (coinBody == null) coinBody = GetComponent<Collider2D>();
            int units = CoinTag.Tick(transform, coinBody, pull, out int pieces);
            if (units > 0)
            {
                bool sound = GainCoins(units, pieces);
                var at = CoinTag.PickedAt;
                for (int i = 0; i < at.Count; i++) Juice.CoinPicked(at[i], sound && i == 0);
            }
        }

        // 상점 · ESC · 레벨업 등으로 멈춘 동안에는 입력을 받지 않음
        // (멈춘 화면에서 클릭하면 총이 나가거나 스킬이 시간을 다시 흐르게 하던 문제)
        // 시작 · 엔딩 연출 중에도 조작하지 않음
        if (IsPaused || StoryDirector.Playing || IsDying || Cinematic)
        {
            move = Vector3.zero;
            return;
        }

        // =========================
        // 재장전 입력
        // =========================

        bool holdingSpecial = special != null && special.WeaponActive;
        if (KeyBindings.Down(GameAction.Reload) && !holdingSpecial && !PathNoAmmo && (CharacterKit.Instance == null || CharacterKit.Instance.UsesAmmo)) if (NowBullet < MaxBullet) StartCoroutine(Reload());

        // =========================
        // 이동 입력
        // =========================

        move = Vector3.zero;

        if (KeyBindings.Held(GameAction.Left)) move += Vector3.left;

        if (KeyBindings.Held(GameAction.Right)) move += Vector3.right;

        if (KeyBindings.Held(GameAction.Up)) move += Vector3.up;

        if (KeyBindings.Held(GameAction.Down)) move += Vector3.down;

        if (GameInput.Auto) move = GameInput.AutoMove;

        move = move.normalized;

        // 1.0.5~ 패드 왼쪽 스틱 · 십자키 (키보드와 합침, 살짝 기울이면 천천히)
        if (!GameInput.Auto)
        {
            Vector2 pad = GamePad.Move;
            if (pad != Vector2.zero)
            {
                move += (Vector3)pad;
                if (move.sqrMagnitude > 1f) move.Normalize();
            }
        }

        // =========================
        // 플레이어 방향
        // =========================

        if (spriteRenderer != null && Time.time >= faceLockUntil)
        {
            if (move.x < 0 && !spriteRenderer.flipX) spriteRenderer.flipX = true;

            if (move.x > 0 && spriteRenderer.flipX) spriteRenderer.flipX = false;
        }

        // =========================
        // 좌클릭 일반 발사
        // =========================

        bool specialWeapon = special != null && special.WeaponActive;
        GameInput.TickAttackLock(PointerOverUIFn);          // 1.1.9~ 설정 「공격 고정」: 공격 키를 누를 때마다 걸기 · 풀기 (1.2.5~ 누른 프레임에만 UI 판정)
        CharacterKit kit = CharacterKit.Instance;
        if (kit != null)
        {
            // 다른 캐릭터: 누르고 있으면 공격 속도에 맞춰 계속 (탄창이 있으면 한 발씩 씀)
            bool ammo = kit.UsesAmmo;
            if (kit.DrawsBow)
            {
                // 궁수: 좌클릭을 누르고 있으면 시위를 당기고, 떼면 발사 (오래 당길수록 강하고 빠른 화살)
                // 공격 속도가 빠를수록 빨리 가득 당김
                kit.UpdateBow(!specialWeapon && !kit.Busy && Time.time >= nextShootTime && !PointerOverUI(),
                              ShootSpeed * 2f / (fireRateMultiplier * kit.AttackSpeedMul));
                // 쏜 뒤에는 가득 당기는 시간의 절반만큼 쉼 (연타가 끝까지 당기는 것보다 세지 않게)
                if (kit.BowFired) nextShootTime = Time.time + ShootSpeed / (fireRateMultiplier * kit.AttackSpeedMul);
            }
            else if (!specialWeapon && GameInput.FireHeld && !kit.Busy && Time.time >= nextShootTime && !PointerOverUI()
                && (!ammo || (NowBullet > 0 && !isReloading && !IsSkillUsing)))
            {
                kit.Attack();
                if (ammo)
                {
                    NowBullet--;
                    if (NowBullet <= 0) kit.OnMagEmpty();
                }
                nextShootTime = Time.time + ShootSpeed / (fireRateMultiplier * kit.AttackSpeedMul);
            }
            if (!specialWeapon) ammoTextOverride = ammo ? null : kit.WeaponName;
        }
        else if (!specialWeapon && GameInput.FireHeld && !IsSkillUsing && !isReloading && Time.time >= nextShootTime && !PointerOverUI()) Shoot();

        // =========================
        // 우클릭 스킬 시작
        // =========================

        DamageSource.Current = DamageSource.Ult;
        if (kit != null) kit.UpdateUlt(skillGauge);
        else
        {
            if (GameInput.UltDown && !IsSkillUsing && !isReloading)
            {
                if (skillGauge != null && skillGauge.IsFull())
                {
                    // 적을 고르지 않는 필살기: 누르고 있는 동안 범위만 보여 주고 떼면 발동 (줌 · 감속 없음)
                    if (special != null && special.IsInstantUlt) { instantAiming = true; special.BeginAim(); }
                    else StartSkill();
                }
            }

            if (instantAiming)
            {
                if (special == null || !special.IsInstantUlt || Time.timeScale == 0f) CancelInstantAim();
                else if (GameInput.UltHeld && !GameInput.UltUp) special.UpdateAim(noTargets, 0f);
                else
                {
                    CancelInstantAim();
                    if (skillGauge != null && skillGauge.IsFull() && !IsSkillUsing) StartCoroutine(InstantUlt());
                }
            }

            // =========================
            // 우클릭 유지 중
            // =========================

            if (GameInput.UltHeld && isSkillUsing)
            {
                NowCharge += Time.unscaledDeltaTime * 100f;

                // 충전할수록 강해짐: 공격력 x2 (즉시) ~ x8 (최대 충전)
                skillDamage = damage * (2f + 6f * Mathf.Clamp01(NowCharge / MaxCharge));

                if (mainCamera != null && mainCamera.orthographicSize < maxZoom) mainCamera.orthographicSize += 0.05f * Time.unscaledDeltaTime * 60f;

                if (Time.unscaledTime >= nextTargetTime)
                {
                    FindNextTarget();
                    nextTargetTime = Time.unscaledTime + targetInterval;
                }
                special?.UpdateAim(targets, Mathf.Clamp01(NowCharge / MaxCharge));

                // 끝까지 누르면 자동 발사. 조준 표시 · 표식을 다 그린 뒤에 끝내야 EndSkill 이 모두 지움
                // (먼저 끝내면 같은 프레임에 다시 그린 표시가 남고, 쏘는 중인 targets 에 적이 더 들어감)
                if (NowCharge >= MaxCharge)
                {
                    NowCharge = MaxCharge;
                    EndSkill();
                }
            }

            // =========================
            // 우클릭 해제
            // =========================

            if (GameInput.UltUp && isSkillUsing) EndSkill();
        }

        // =========================
        // 자동 재장전
        // =========================

        DamageSource.Current = DamageSource.Basic;
        if (NowBullet <= 0 && !isReloading && !PathNoAmmo && (CharacterKit.Instance == null || CharacterKit.Instance.UsesAmmo)) StartCoroutine(Reload());

        // =========================
        // 애니메이션
        // =========================

        if (!isSkillUsing) UpdateBodyAnimation();
    }

    // 몸 애니메이션 (거너): 트리거 대신 상태를 직접 고름
    // 사격 상태에는 나가는 전이가 없어 한 번 쏘면 달리기 · 대기로 돌아오지 못했고,
    // 매 프레임 건 Move · Stop 트리거가 쌓여 달리기가 계속 첫 장으로 되감기던 문제
    // 총은 PlayerLook 이 따로 그리므로 움직이는 동안은 항상 달리기, 서서 쏠 때만 잠깐 사격 자세
    const float ShootPoseTime = 0.4f;
    float shootPoseUntil;

    void UpdateBodyAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled) return;
        string want = move.sqrMagnitude > 0f ? "PlayerRun" : Time.time < shootPoseUntil ? "PlayerShoot" : "PlayerIdle";
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(want)) animator.Play(want, 0, 0f);
    }

    // =====================================
    // 이동
    // =====================================

    Rigidbody2D body;

    void FixedUpdate()
    {
        // 몸은 물리(Dynamic)지만 이동은 직접 함: 적과 부딪혀 생긴 속도 · 회전이 남아 플레이어가 휙 밀려나던(순간이동처럼 보이던) 문제
        // 벽 · 적과 겹치지 않게 밀어내는 것은 그대로, 남는 속도만 매 물리 단계마다 지움
        if (body == null) { body = GetComponent<Rigidbody2D>(); if (body != null) body.freezeRotation = true; }
        if (body != null) { body.velocity = Vector2.zero; body.angularVelocity = 0f; }

        float slowMul = Time.time < slowUntil ? slowFactor : 1f;
        float kitMul = CharacterKit.Instance != null ? CharacterKit.Instance.MoveMul : 1f;
        if (!isSkillUsing) transform.Translate(move * CappedSpeed(speed * kitMul * SignatureSkills.MoveMul) * slowMul * Time.fixedDeltaTime);

        // 1.4.6~ 결계 뒤 맵 테두리 벽 너머에 갇히지 않게 (0.5초마다 · BossUltimate.KeepInside)
        if (Time.time >= nextInsideCheck) { nextInsideCheck = Time.time + 0.5f; BossUltimate.KeepInside(this); }
    }
    float nextInsideCheck;

    // =====================================
    // 재장전
    // =====================================

    // 2026-10-19~ 빌드 갈래 (거너 「과열 사격」): 탄이 줄지 않고 장전 키 · 자동 장전을 쓰지 않음 (과열이 장전을 대신함)
    static bool PathNoAmmo => GunnerPaths.Live != null && GunnerPaths.Live.NoAmmo;

    // 과열: 장전 코루틴으로 사격 불가 시간을 냄 (GunnerPaths.EndShot) — 장전 표시 · 장전 시작 · 끝 훅을 그대로 씀
    public void ForceReload()
    {
        if (!isReloading && !IsDying && isActiveAndEnabled) StartCoroutine(Reload());
    }

    // 2026-10-19~ 장전 안전망 (1.2.7 「끊긴 코루틴이 상태를 남김」 사고와 같은 모양 막기)
    //   reloadGen: 장전마다 번호 · 안전망이 강제로 풀면 번호가 바뀌어 옛 코루틴은 아무것도 건드리지 않고 끝남
    //   reloadBeat: 장전 코루틴이 마지막으로 돈 시각 (멈춤 메뉴에서도 돎) — 3초 넘게 안 돌면 코루틴이 죽은 것으로 보고 풂
    int reloadGen;
    float reloadBeat;
    const float ReloadGrace = 3f;

    // 장전 중인데 코루틴이 죽었으면 (예외 · 비활성화로 끊김) 장전 상태를 풂 — Update 가 매 프레임 부름
    void ReloadWatchdog()
    {
        if (!isReloading || Time.unscaledTime - reloadBeat <= ReloadGrace) return;
        reloadGen++;
        isReloading = false;
        reload = 0f;
        NowBullet = MaxBullet;
    }

    // 갈래 코드가 예외를 내도 장전이 막히지 않게 (예외는 기록만 하고 기본값)
    static float SafeReloadTime(GunnerPaths gp, float normal)
    {
        if (gp == null) return normal;
        try { return Mathf.Max(0.05f, gp.ReloadTime(normal)); }
        catch (System.Exception e) { Debug.LogException(e); return normal; }
    }

    static float SafeReloadRate(GunnerPaths gp)
    {
        if (gp == null) return 1f;
        try { float r = gp.ReloadRate; return float.IsNaN(r) || r < 1f ? 1f : r; }
        catch (System.Exception e) { Debug.LogException(e); return 1f; }
    }

    static void SafeHook(System.Action a)
    {
        try { a(); }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        reload = 0f;
        int gen = ++reloadGen;
        reloadBeat = Time.unscaledTime;
        bool done = false;
        try
        {
            SafeHook(() => special?.ReloadShockwave(transform.position));
            SafeHook(() => SignatureSkills.ReloadStart(true));          // 탄피 지뢰 · 빈 주머니의 비 · (갈래) 산탄 돌진 · 과열 폭발 (1.4.1~ 권총 장전 표시)

            if (!CharacterData.IsGunner && SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.Play("rl_stars", 0.8f);
            else if (audioSource != null && reloadSound != null) audioSource.PlayOneShot(reloadSound, GameSettings.SfxVolume);

            // 2026-10-19~ 빌드 갈래 (거너): 과열 시간 · 산탄 장전 40% 빠름 · 포탑 곁 2배 — 진행 값(reload)은 그대로 0 → reloadTime (장전 표시가 맞게)
            GunnerPaths gp = GunnerPaths.Live;
            float need = SafeReloadTime(gp, reloadTime);
            float spent = 0f;           // 멈추지 않은 동안 실제로 흐른 시간 (대기 상한: 정해진 장전 시간 + 3초)
            while (reload < reloadTime)
            {
                if (gen != reloadGen) yield break;          // 안전망이 이미 풂
                reloadBeat = Time.unscaledTime;
                // 필살기 조준의 느린 시간에도 제 속도로, 메뉴로 멈춘 동안에는 멈춤
                if (!IsPaused)
                {
                    reload += Time.unscaledDeltaTime * (gp != null ? reloadTime / need * SafeReloadRate(gp) : 1f);
                    spent += Time.unscaledDeltaTime;
                    if (spent > (gp != null ? need : reloadTime) + ReloadGrace) break;
                }
                yield return null;
            }
            if (gen != reloadGen) yield break;

            NowBullet = MaxBullet;
            done = true;
        }
        finally
        {
            // 예외로 코루틴이 죽어도 장전 상태가 남지 않게 (안전망이 이미 풀었으면 건드리지 않음)
            if (gen == reloadGen)
            {
                reload = 0f;
                isReloading = false;
            }
        }

        if (done) SafeHook(() => SignatureSkills.ReloadEnd(true));            // 섬광 장전 · 속사 장전 (1.4.1~ 권총 장전 표시)
    }

    public bool IsReloading => isReloading;
    // 속사 장전: 다음 발을 거의 바로 쏠 수 있게
    public void QuickShot() { nextShootTime = Time.time + 0.05f; special?.QuickFire(); }      // 특수 총기도 (2.1.8)

    // =====================================
    // 일반 총알 발사
    // =====================================


void Shoot()
    {
        if (NowBullet <= 0 || isReloading) return;
        using var aim = WeakHit.AimScope(1.75f);       // 빈틈: 직접 조준한 평타 (WeakSpot)
        // 2026-10-19~ 빌드 갈래 (거너 · Paths/Paths.Gunner*.cs): 저격 · 난사 · 과열 · 산탄 · 포탑 — 갈래 규칙이 아니면 null (예전 그대로)
        GunnerPaths gp = GunnerPaths.Live;

        nextShootTime = Time.time + ShootSpeed / fireRateMultiplier * (gp != null ? gp.IntervalMul : 1f);
        // 탄창 저주: 마지막 한 발 강화
        // 과열 사격은 탄이 줄지 않으므로 「과열까지 남은 발 수」를 탄 수 대신 씀 (마지막 발 = 이 발로 과열) — 갈래 없으면 NowBullet 그대로
        bool noAmmo = gp != null && gp.NoAmmo;
        int shellsLeft = noAmmo ? gp.HeatShotsLeft : NowBullet;
        bool cursed = special != null && special.IsLastBulletCursed(shellsLeft);
        bool lastShell = shellsLeft == 1;
        if (noAmmo) NowBullet = MaxBullet;       // 과열 사격: 탄 대신 열 게이지
        else NowBullet--;

        // 서서 쏠 때는 쏠 때마다 사격 자세를 처음부터
        shootPoseUntil = Time.time + ShootPoseTime;
        if (animator != null && animator.isActiveAndEnabled && move.sqrMagnitude == 0f) animator.Play("PlayerShoot", 0, 0f);

        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(GameInput.MousePosition);
        mousePosition.z = 0;

        FaceTowards(mousePosition);
        PlayerLook.Fired(-1);

        if (!SkinAudio.PlayPistol() && audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound, GameSettings.SfxVolume);

        Vector3 startPosition = MuzzlePosition;

        Vector2 direction = (mousePosition - startPosition).normalized;
        if (gp != null) direction = gp.Spread(direction);         // 난사: 퍼짐 ±8°

        // 멀티샷 퍼지는 각도
        float spreadAngle = 10f;

        float bulletDamage = damage * damageMultiplier * MultiShotDamageRate(multiShot) * (cursed ? 3f : 1f);
        SignatureSkills.Attacked(startPosition, direction);
        float extraDamage = bulletDamage;           // 진화 덤 사격(쌍발 · 산탄 · 화염 총구)은 갈래 배율(저격 · 열 · 돌진 · 난사) 이전 값
        if (gp != null) bulletDamage *= gp.BeginShot(move.sqrMagnitude > 0.01f, lastShell);     // 대가 · 저격탄 · 아슬아슬한 열

        float shotRate = MultiShotDamageRate(multiShot);

        if (gp != null && gp.FireOverride(this, startPosition, direction, bulletDamage, cursed)) { }     // 산탄 · 슬러그
        else if (multiShot == 1)
        {
            Bullet one = CreateBullet(startPosition, direction, bulletDamage, pene, 0, false);
            special?.CurseBullet(one, cursed);
            special?.ApplyGunCards(one, false);
            gp?.Dress(one);
        }
        else
        {
            int shotCount = multiShot;

            float startAngle = -spreadAngle * (shotCount - 1) / 2f;

            for (int i = 0; i < shotCount; i++)
            {
                float angle = startAngle + spreadAngle * i;

                Vector2 shotDirection = Quaternion.Euler(0, 0, angle) * direction;

                Bullet b = CreateBullet(startPosition, shotDirection, bulletDamage, pene, 0, false, shotRate);
                special?.CurseBullet(b, cursed);
                special?.ApplyGunCards(b, false, i == 0);
                gp?.Dress(b);
            }
        }
        if (gp != null) extraDamage *= gp.ExtraShotMul;      // 산탄 갈래: 덤도 펠릿 비율 (덤이 본체보다 세지 않게)
        special?.EvoExtraShots(startPosition, direction, extraDamage / Mathf.Max(0.01f, cursed ? 3f : 1f));      // 쌍발 총구 · 산탄 총구
        gp?.EndShot(this);          // 과열 사격: 열 (가득 차면 과열)
    }



    // =====================================
    // 총알 생성
    // =====================================

    // 마우스가 버튼 같은 UI 위에 있으면 사격하지 않음
    // 1.0.4~: 전투 중엔 설명만 있는 HUD 칸(레벨업 스킬 · 특수 능력 칸, TooltipTrigger.hud)은 발사를 막지 않음
    // (예전엔 왼쪽 위 · 아래 칸 위를 겨누면 평타가 조용히 멈췄음) · 버튼 같은 진짜 UI 는 그대로 막음
    static readonly List<UnityEngine.EventSystems.RaycastResult> uiHits = new List<UnityEngine.EventSystems.RaycastResult>();
    static UnityEngine.EventSystems.PointerEventData uiPointer;
    static UnityEngine.EventSystems.EventSystem uiPointerEs;
    static int uiCheckFrame = -1;
    static bool uiBlocked;

    static readonly System.Func<bool> PointerOverUIFn = PointerOverUI;     // 1.2.5~ 매 프레임 대리자를 새로 만들지 않게

    static bool PointerOverUI()
    {
        UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
        if (GameInput.Auto || GamePad.Aiming || es == null || !es.IsPointerOverGameObject()) return false;     // 1.0.5~ 패드 조준은 멈춰 둔 마우스 자리와 상관없음
        if (Time.timeScale <= 0f) return true;
        if (uiCheckFrame == Time.frameCount) return uiBlocked;      // 한 프레임에 여러 번 불려도 한 번만 검사
        uiCheckFrame = Time.frameCount;
        if (uiPointer == null || uiPointerEs != es) { uiPointer = new UnityEngine.EventSystems.PointerEventData(es); uiPointerEs = es; }
        uiPointer.position = Input.mousePosition;
        uiHits.Clear();
        es.RaycastAll(uiPointer, uiHits);
        uiBlocked = false;
        if (uiHits.Count > 0 && uiHits[0].gameObject != null)
        {
            TooltipTrigger tip = uiHits[0].gameObject.GetComponentInParent<TooltipTrigger>();
            uiBlocked = tip == null || !tip.hud;
        }
        return uiBlocked;
    }

    // 잠깐 동안 목표 방향을 바라보게 함 (이동 입력보다 우선)
    public void FaceTowards(Vector3 worldPos)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.flipX = worldPos.x < transform.position.x;
        faceLockUntil = Time.time + faceShotTime;
    }

    public Bullet CreateBullet(Vector3 startPosition, Vector2 direction, float damage, int penes, float blood, bool isSkill, float knockBackRate = 1f)
    {
        if (bulletPrefab == null) return null;

        // 최적화 8: 사라진 탄을 지우지 않고 다시 씀 (Bullet.Spawn · Release, 꾸밈 자식도 BulletGlow 가 다시 씀)
        Bullet bullet = Bullet.Spawn(bulletPrefab, startPosition);
        if (bullet == null) return null;
        GameObject newBullet = bullet.gameObject;
        if (SkinFx.WeaponSkin != null) newBullet.AddComponent<SkinTint>();        // 무기 스킨: 총알 · 투사체 색

        {
            bullet.Dir = direction;
            bullet.pene = penes;
            bullet.blood = blood;
            bullet.damage = damage;
            bullet.isSkill = isSkill;
            bullet.knockBack = knockBack * knockBackRate;
            // 1.1.2: 납작한 흰 실선 대신 금빛 예광탄 (보이는 크기 3~4배, BulletGlow 예광탄 모드)
            //   판정 = 보이는 예광탄 몸통 (BulletGlow.FitTracerHit, 피해는 그대로) · 터널링은 Bullet 의 스윕 검사가 막음
            BulletGlow glow = bullet.Glow;
            if (glow == null) glow = newBullet.AddComponent<BulletGlow>();
            if (newBullet.TryGetComponent(out SpriteRenderer bodySr)) glow.tracerSprite = bodySr.sprite;
            glow.tracerBaseScale = newBullet.transform.localScale;
            // 스킬 게이지는 화염 방사기 기준으로 천천히 참
            bullet.skillCharge = knockBackRate * GaugeRate;
            // 빈틈 후보: 조준 범위 안에서, 그 순간 「플레이어 → 커서」 방향에서 25° 안으로 나간 탄만 (덤으로 흩뿌리는 탄 · 패드 보조 조준은 빠짐)
            //   출발점이 아니라 플레이어 기준: 커서 자리에서 출발하는 탄(지도 제작자 획 끝 등)이 어느 쪽으로 날아가든 통과하지 않게 · 커서가 플레이어 위면 통과 없음
            bullet.aimMul = 0f;
            if (WeakHit.InAim && mainCamera != null)
            {
                Vector3 cursor = mainCamera.ScreenToWorldPoint(GameInput.MousePosition);
                Vector2 toCursor = (Vector2)(cursor - transform.position);
                if (toCursor.sqrMagnitude >= 0.0001f && Vector2.Angle(direction, toCursor) <= 25f) bullet.aimMul = WeakHit.ScopeMul;
            }
        }

        return bullet;
    }

    // 총구 위치 (바라보는 쪽 손)
    public Vector3 MuzzlePosition => PlayerLook.Instance != null && PlayerLook.Instance.HasTip ? PlayerLook.Instance.TipPosition : BaseMuzzle;
    // 손에 든 무기 그림이 없을 때의 총구 (캐릭터 그림 기준)
    public Vector3 BaseMuzzle => transform.position + new Vector3(spriteRenderer != null && spriteRenderer.flipX ? -0.5f : 0.5f, -0.5f, 0);

    // 꽃가루 구름 등으로 잠깐 느려짐
    float slowUntil;
    float slowFactor = 1f;
    // 지금 이동 감속 배율 (읽기 전용 · 개발자 · 엄마 고유 공격이 「피하려면 움직여야 하는 공격」을 감속 중에 시작하지 않으려고)
    public float MoveSlowNow => Time.time < slowUntil ? slowFactor : 1f;

    public void Slow(float factor, float seconds)
    {
        // 1.2.3~ 감속이 걸려 있는 동안은 더 센 쪽을 남김 (점액 늪이 매 프레임 불러도 보스 감속이 약해지지 않게)
        slowFactor = Time.time < slowUntil ? Mathf.Min(slowFactor, factor) : factor;
        slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
    }

    // 1.0.4~ 개발자(무한 모드 최종 보스)가 거너 조준 사격을 흉내 낼 때: 플레이어 필살기와 같은 화면 색 · 충전 소리 · 카메라 멀어짐
    // 플레이어가 쓰면 시간이 느려져 적이 느려지는 것을 뒤집어, 느려지는 건 플레이어 · 플레이어 탄 (게임 시간 Time.timeScale 은 그대로)
    // 겹쳐 써도 쌓이지 않고 끝나는 때만 늘어남 (공격 주기가 빨라져도 끝없이 느려지거나 멈추지 않게)
    float hostileAimUntil;
    bool hostileAiming;

    public void HostileTimeSlow(float seconds)
    {
        if (IsDying || seconds <= 0f) return;
        hostileAimUntil = Mathf.Max(hostileAimUntil, Time.time + seconds);
        float mul = Mathf.Clamp(Skill_setTime, 0.2f, 1f);
        Slow(mul, hostileAimUntil - Time.time);
        Bullet.SlowMul = mul;
        Bullet.SlowUntil = hostileAimUntil;
        if (hostileAiming) return;
        hostileAiming = true;
        PlayUltSound(true);
        StartCoroutine(HostileAimRoutine());
    }

    IEnumerator HostileAimRoutine()
    {
        if (skillEffectPanel != null && !isSkillUsing)
        {
            if (!effectColorSaved) { baseEffectColor = skillEffectPanel.color; effectColorSaved = true; }
            Color tint = baseEffectColor;
            tint.a = skillEffectPanel.color.a;
            skillEffectPanel.color = tint;
            FadeTo(screenFadeAlpha);
        }
        while (Time.time < hostileAimUntil && !IsDying)
        {
            // 조준할 때처럼 카메라가 천천히 멀어짐 (플레이어가 그사이 자기 필살기를 조준하면 그쪽이 맡음)
            if (!isSkillUsing && Time.timeScale > 0f && mainCamera != null && mainCamera.orthographicSize < maxZoom)
                mainCamera.orthographicSize += 0.05f * Time.unscaledDeltaTime * 60f;
            yield return null;
        }
        hostileAiming = false;
        if (isSkillUsing || IsDying) yield break;       // 자기 필살기 조준 · 쓰러짐은 그쪽이 화면 · 줌을 되돌림
        if (audioSource != null) audioSource.Stop();
        FadeTo(0f);
        StartCoroutine(ResetZoom());
    }

    // 거너 필살기 소리 (charge: 조준 시작 · 아니면 한 발) — 개발자가 흉내 낼 때도 같은 소리
    public void PlayUltSound(bool charge)
    {
        AudioClip clip = charge ? chargeSound : shotSound;
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, GameSettings.SfxVolume);
    }

    public void GrantInvincibility(float seconds)
    {
        invincibleUntil = Mathf.Max(invincibleUntil, Time.time + seconds);
    }

    // =====================================
    // 스킬 시작
    // =====================================

    void StartSkill()
    {
        if (isSkillUsing) return;
        if (skillGauge == null || !skillGauge.IsFull()) return;
        isSkillUsing = true;
        UltUsed?.Invoke();
        NowCharge = 0f;
        skillDamage = damage * 2f;
        if (audioSource != null && chargeSound != null) audioSource.PlayOneShot(chargeSound, GameSettings.SfxVolume);

        ClearTargets();

        nextTargetTime = Time.unscaledTime;

        // 화면 색은 들고 있는 무기 색, 조준 연출도 무기마다 다름
        if (skillEffectPanel != null)
        {
            if (!effectColorSaved) { baseEffectColor = skillEffectPanel.color; effectColorSaved = true; }
            Color tint = special != null && special.UltReplaced ? Color.Lerp(baseEffectColor, special.AimTint, 0.6f) : baseEffectColor;
            tint.a = skillEffectPanel.color.a;
            skillEffectPanel.color = tint;
        }
        special?.BeginAim();

        FadeTo(screenFadeAlpha);

        Time.timeScale = Skill_setTime;
    }

    // =====================================
    // 한 마리씩 타겟팅
    // =====================================

    readonly List<Collider2D> targetHits = new List<Collider2D>(64);

    void FindNextTarget()
    {
        // 한 번에 조준할 수 있는 적 수 (스킬 강화로 늘어남)
        int limit = special != null ? special.MaxTargets(maxTargets) : maxTargets;
        if (targets.Count + bossTargets.Count >= limit) return;

        List<Collider2D> enemies = Specials.Overlap(transform.position, targetRange, targetHits);

        EnermyController closestEnemy = null;
        bosss closestBoss = null;

        float closestDistance = Mathf.Infinity;

        foreach (Collider2D enemyCollider in enemies)
        {
            // 1.1.3: 보스도 한 칸을 차지하는 조준 대상 (보스 하나는 한 번만 · 부위 콜라이더는 몸통의 bosss 로)
            if (enemyCollider.CompareTag("boss"))
            {
                bosss b = enemyCollider.GetComponentInParent<bosss>();
                if (b == null || b.IsDead || bossTargets.Contains(b)) continue;
                float bd = Vector2.Distance(transform.position, b.transform.position);
                if (bd < closestDistance) { closestDistance = bd; closestBoss = b; closestEnemy = null; }
                continue;
            }
            if (!enemyCollider.CompareTag("enermy")) continue;

            EnermyController enemy = enemyCollider.GetComponent<EnermyController>();

            if (enemy == null || targets.Contains(enemy)) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
                closestBoss = null;
            }
        }

        if (closestEnemy == null && closestBoss == null) return;

        Transform picked;
        if (closestEnemy != null) { targets.Add(closestEnemy); picked = closestEnemy.transform; }
        else { bossTargets.Add(closestBoss); picked = closestBoss.transform; }

        // 진화한 무기의 필살기면 무기다운 표식 (범위형 무기는 표식 없음)
        if (special != null && special.UltReplaced)
        {
            GameObject weaponMark = special.MarkTarget(picked);
            if (weaponMark != null) targetMarks.Add(weaponMark);
        }
        else if (targetMarkPrefab != null)
        {
            GameObject mark = Instantiate(targetMarkPrefab, picked.position, Quaternion.identity);
            TargetMark targetMark = mark.GetComponent<TargetMark>();
            if (targetMark != null) targetMark.target = picked;
            targetMarks.Add(mark);
            if (NowBullet < MaxBullet) NowBullet++;
        }
    }

    // =====================================
    // 스킬 종료
    // =====================================

    void EndSkill()
    {
        GameInput.ResetUltLatch();
        if (!isSkillUsing) return;
        // 1.2~ 영혼 계약 (거너 일제 사격): 한 번에 조준한 수 · 보스 명중
        RunCounters.Max("volley.marks", targets.Count + bossTargets.Count);
        RunCounters.Add("volley.boss", bossTargets.Count);

        isSkillUsing = false;

        if (audioSource != null) audioSource.Stop();

        Time.timeScale = 1f;

        FadeTo(0f);

        StartCoroutine(ResetZoom());

        foreach (GameObject mark in targetMarks) if (mark != null) Destroy(mark);

        targetMarks.Clear();
        special?.EndAim();

        StartCoroutine(ShootTargets());

        if (skillGauge != null) skillGauge.ResetSkillPoint();

        NowCharge = 0f;
    }

    // 즉발 필살기 조준 (누르고 있는 동안 범위 표시)
    bool instantAiming;
    public bool InstantAiming => instantAiming;
    static readonly List<EnermyController> noTargets = new List<EnermyController>();

    void CancelInstantAim()
    {
        if (!instantAiming) return;
        instantAiming = false;
        special?.EndAim();
    }

    // 조준을 쏘지 않고 취소 (일시정지할 때): 시간 · 화면 · 줌을 되돌리고 게이지는 그대로 둠
    public void CancelSkill()
    {
        GameInput.ResetUltLatch();
        CancelInstantAim();
        if (!isSkillUsing) return;

        isSkillUsing = false;

        if (audioSource != null) audioSource.Stop();

        Time.timeScale = 1f;

        FadeTo(0f);

        StartCoroutine(ResetZoom());

        foreach (GameObject mark in targetMarks) if (mark != null) Destroy(mark);

        targetMarks.Clear();
        targets.Clear();
        bossTargets.Clear();
        special?.EndAim();

        NowCharge = 0f;
    }

    // =====================================
    // 타겟들에게 순서대로 발사
    // =====================================

    // 필살기를 썼을 때 (업적 등)
    public static event System.Action UltUsed;
    // 다른 캐릭터의 우클릭 스킬도 필살기로 셈 (업적 등)
    public void RaiseUltUsed() => UltUsed?.Invoke();

    IEnumerator InstantUlt()
    {
        UltUsed?.Invoke();
        isVolleying = true;
        skillGauge.ResetSkillPoint();
        FaceTowards(mainCamera.ScreenToWorldPoint(GameInput.MousePosition));
        yield return StartCoroutine(special.WeaponVolley(new List<EnermyController>(), damage * 5f, 0f));
        isVolleying = false;
    }

    const float GunUltAddMul = 0.5f;     // CharacterKit.KitUltAddMul 과 같게
    // 1.1.3: 보스에게 쏘는 필살기 총알의 피해 (잡몹은 대부분 첫 발에 죽어 남은 발이 버려지지만 보스는 모든 발이 다 박히므로 절반)
    const float GunUltBossMul = 0.5f;

    IEnumerator ShootTargets()
    {
        isVolleying = true;

        // 필살기를 바꾸는 진화(쌍권총 · 총알 폭풍)면 그 무기다운 일제 사격
        if (special != null && special.UltReplaced)
        {
            yield return StartCoroutine(special.WeaponVolley(targets, skillDamage, getHP));
            yield return StartCoroutine(ShootBossTargets());
            targets.Clear();
            isVolleying = false;
            yield break;
        }

        // 1.0.2: 죽음의 춤은 조준한 적들 사이를 베는 필살기라, 총알 뒤에 덧붙으면 적이 먼저 죽어 거의 발동하지 않았음 → 춤을 먼저 추고 총알
        // 거너는 1차 · 2차 진화가 다르면 두 궁극기가 모두 덧붙음 (SpecialAbilities.AddedUltIds)
        List<int> adds = special != null && special.UltAdds ? special.AddedUltIds() : new List<int>();
        if (adds.Remove(SpecialAbilities.ScytheId))
            yield return StartCoroutine(special.WeaponVolley(new List<EnermyController>(targets), skillDamage * GunUltAddMul, getHP, SpecialAbilities.ScytheId));

        foreach (EnermyController picked in targets)
        {
            EnermyController target = picked;
            if (target == null || target.IsDead) continue;     // 1.0.2: 덧붙은 춤 · 앞선 총알에 이미 쓰러진 적은 건너뜀

            // 2.2.0: 명중률 — 방향을 첫 발 전에 한 번만 정하던 것을 매 발 다시 조준 (발밑 대신 몸 중심, 움직이는 적은 날아가는 동안 갈 자리를 앞질러)
            Collider2D body = target.GetComponent<Collider2D>();
            Vector3 lastPos = target.transform.position;
            float lastTime = Time.time;
            Vector2 velocity = Vector2.zero;

            for (int i = 0; i < ShotsPerTarget;  i++)
            {
                // 1.0.2: 쏘던 적이 쓰러지면 남은 총알을 시체(총알이 그냥 지나감)에 버리지 않고 가까운 살아 있는 적에게
                if (target == null || target.IsDead)
                {
                    Transform near = Specials.NearestEnemy(lastPos, 8f);
                    target = near != null ? near.GetComponent<EnermyController>() : null;
                    if (target == null) break;
                    body = target.GetComponent<Collider2D>();
                    lastPos = target.transform.position;
                    lastTime = Time.time;
                    velocity = Vector2.zero;
                }

                Vector3 now = target.transform.position;
                float dt = Time.time - lastTime;
                if (dt > 0.001f) velocity = Vector2.ClampMagnitude((now - lastPos) / dt, 8f);     // 밀려난 거리로 튀지 않게
                lastPos = now; lastTime = Time.time;

                Vector3 startPosition = transform.position + new Vector3(0, -0.5f, 0);
                Vector3 aim = body != null && body.enabled ? body.bounds.center : now;

                FaceTowards(aim);
                PlayerMotion.Of(this).Recoil(aim - startPosition, 0.6f);      // 몸동작 (1.0.1)

                if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound, GameSettings.SfxVolume);

                Bullet shot = CreateBullet(startPosition, ((Vector2)(aim - startPosition)).normalized, skillDamage * (special != null ? special.UltPower(SpecialAbilities.PistolUlt) : 1f), pene, getHP, true);
                // 2.2.1: 필살기 총알에도 총알 카드 · 고유 스킬(영혼 탄환 · 총열 과열 · 소각탄 · 전기탄 …), 덤 총알 · 유도 · 폭발탄은 빼고(light)
                SignatureSkills.UltShots = true;
                try { special?.ApplyGunCards(shot, true, false); }
                finally { SignatureSkills.UltShots = false; }      // 1.3.8~ 예외가 나도 켜진 채 남지 않게
                GunnerPaths.Live?.UltShot(shot);                   // 2026-10-19~ 관통 저격 극의: 일제 사격이 모두 저격탄
                if (shot != null)
                {
                    // 카드로 바뀐 탄속까지 반영해 앞질러 조준
                    float lead = Mathf.Min(0.6f, Vector2.Distance(startPosition, aim) / Mathf.Max(1f, shot.speed));
                    Vector2 dir = (Vector2)(aim + (Vector3)(velocity * lead) - startPosition);
                    if (dir.sqrMagnitude > 0.0001f) shot.Dir = dir.normalized;
                }

                yield return new WaitForSeconds(shootDelay);
            }
        }

        yield return StartCoroutine(ShootBossTargets());

        // 2.2.0: 나머지 진화 무기는 원래 조준 사격 뒤에 그 무기의 궁극기가 절반 세기로 덧붙음 (다른 캐릭터와 같게)
        foreach (int add in adds)
            yield return StartCoroutine(special.WeaponVolley(new List<EnermyController>(targets), skillDamage * GunUltAddMul, getHP, add));

        targets.Clear();
        isVolleying = false;
    }

    // 1.1.3: 조준한 보스에게 일제 사격 (몸 중심을 겨눠 ShotsPerTarget 발, 피해 GunUltBossMul 배 · 총알 카드 · 고유 스킬은 잡몹 사격과 같게)
    IEnumerator ShootBossTargets()
    {
        foreach (bosss b in bossTargets)
        {
            if (b == null || b.IsDead) continue;
            Collider2D body = b.GetComponent<Collider2D>();
            for (int i = 0; i < ShotsPerTarget; i++)
            {
                if (b == null || b.IsDead) break;
                Vector3 startPosition = transform.position + new Vector3(0, -0.5f, 0);
                Vector3 aim = body != null && body.enabled ? body.bounds.center : b.transform.position;
                FaceTowards(aim);
                PlayerMotion.Of(this).Recoil(aim - startPosition, 0.6f);
                if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound, GameSettings.SfxVolume);
                Bullet shot = CreateBullet(startPosition, ((Vector2)(aim - startPosition)).normalized,
                    skillDamage * GunUltBossMul * (special != null ? special.UltPower(SpecialAbilities.PistolUlt) : 1f), pene, getHP, true);
                SignatureSkills.UltShots = true;
                try { special?.ApplyGunCards(shot, true, false); }
                finally { SignatureSkills.UltShots = false; }      // 1.3.8~ 예외가 나도 켜진 채 남지 않게
                GunnerPaths.Live?.UltShot(shot);                   // 2026-10-19~ 관통 저격 극의
                yield return new WaitForSeconds(shootDelay);
            }
        }
        bossTargets.Clear();
    }

    // =====================================
    // 화면 페이드
    // =====================================

    // 1.0.1: 조준을 빨리 끝내면 아직 진하게 바뀌던 코루틴이 흐려지는 코루틴보다 늦게 끝나 파란 화면이 남았음
    // → 하나만 돌게 (새로 시작하면 앞의 것을 멈춤), 매 프레임 지금 색에서 움직임
    Coroutine screenFade;
    float screenFadeTarget;

    void FadeTo(float targetAlpha)
    {
        if (screenFade != null) StopCoroutine(screenFade);
        screenFadeTarget = targetAlpha;
        screenFade = StartCoroutine(FadeScreen(targetAlpha));
    }

    IEnumerator FadeScreen(float targetAlpha)
    {
        if (skillEffectPanel == null) yield break;
        while (skillEffectPanel != null && !Mathf.Approximately(skillEffectPanel.color.a, targetAlpha))
        {
            Color color = skillEffectPanel.color;
            color.a = Mathf.MoveTowards(color.a, targetAlpha, screenFadeSpeed * Time.unscaledDeltaTime);
            skillEffectPanel.color = color;
            yield return null;
        }
        screenFade = null;
    }

    // 안전장치: 조준 중이 아닌데 화면 색이 남아 있으면 흐려지게 (어떤 길로든 남지 않게)
    void GuardScreenTint()
    {
        if (skillEffectPanel == null || isSkillUsing || hostileAiming || screenFade != null) return;
        if (skillEffectPanel.color.a > 0.001f) FadeTo(0f);
    }

    // =====================================
    // 카메라 줌 원상복구
    // =====================================

    IEnumerator ResetZoom()
    {
        if (mainCamera == null) yield break;

        while (mainCamera.orthographicSize > normalZoom)
        {
            mainCamera.orthographicSize = Mathf.MoveTowards( mainCamera.orthographicSize, normalZoom, 10f * Time.unscaledDeltaTime);
            yield return null;
        }
    }

    // =====================================
    // 플레이어 피격
    // =====================================
    public float def = 0f;

    [Header("회복")]
    // 초당 체력 회복 (생명의 샘)
    public float regenPerSecond = 0f;
    // 적 처치 시 체력 회복 (피의 굶주림)
    public float healOnKill = 0f;

    // 1.0.2: 적이 한꺼번에 많이 쓰러지면 처치마다 회복이 쌓여 사실상 무적이었음 → 처치 회복은 모두 합쳐 1초에 최대 KillHealPerSecond
    const float KillHealPerSecond = 6f;
    float killHealSecond = -1f, killHealUsed;

    void HealOnKill(Vector3 pos)
    {
        if (healOnKill <= 0f || PlayerHealth <= 0f) return;
        float now = Mathf.Floor(Time.time);
        if (now != killHealSecond) { killHealSecond = now; killHealUsed = 0f; }
        float heal = Mathf.Min(healOnKill, KillHealPerSecond - killHealUsed);
        if (heal <= 0f) return;
        killHealUsed += heal;
        PlayerHealth = Mathf.Min(PlayerMaxHealth, PlayerHealth + heal);
    }

    void OnDestroy()
    {
        EnermyController.Killed -= HealOnKill;
    }

    int dropCoin;
    float lastCoinSound = -1f;
    int coinStreak;
    Collider2D coinBody;        // 코인 줍기 판정에 쓰는 몸 콜라이더 (CoinTag.Tick)

    // 코인 units 개 몫을 받음 (pieces: 이번에 주운 코인 개수) → 이번에 줍는 소리를 냈는지
    // 1.0.4~: 한 프레임에 주운 코인을 한 번에 더함 (값이 큰 코인도 하나씩 주울 때와 같은 양 · 황금 시대도 개수만큼)
    bool GainCoins(int units, int pieces)
    {
        Coin coin = Cache<Coin>.Get;
        if (coin == null || units <= 0) return false;
        int each = 1 + bonusCoin;
        coin.AddCoin(units * each);
        for (int i = 0; i < units; i++) SignatureSkills.CoinPicked(each);      // 황금 시대 (스킬 진화)
        // 한꺼번에 여러 개를 주우면 소리가 겹쳐 너무 커지므로: 0.07초에 한 번만, 연달아 주울수록 작게
        float now = Time.unscaledTime;
        coinStreak = (now - lastCoinSound < 0.6f ? coinStreak + 1 : 0) + Mathf.Max(0, pieces - 1);
        bool sound = now - lastCoinSound >= 0.07f;
        if (sound)
        {
            lastCoinSound = now;
            // 1.1~: 0.6초 안에 이어 주울수록 작게 하던 것 → 반음씩 높게 (최대 12단계 = 한 옥타브)
            float pitch = Mathf.Pow(2f, Mathf.Min(coinStreak, 12) / 12f);
            AudioSource src = CoinSource(pitch);
            if (src != null && getCoin != null)
            {
                src.pitch = pitch;
                // 1.1~ 오래 이어 주우면 조금씩 작게 (꼭대기 음높이에서 같은 크기로 초당 14번 울리던 것 · 최소 60%)
                src.PlayOneShot(getCoin, GameSettings.SfxVolume * 0.85f * Mathf.Max(0.6f, 1f / (1f + 0.04f * coinStreak)));
            }
        }
        return sound;
    }

    // 1.1~ 코인 소리 전용 소스 묶음 (음높이를 바꿔도 총소리 · 재장전 소리에 걸리지 않게)
    // 소스의 pitch 는 이미 울리고 있는 소리에도 걸리므로, 같은 음높이 소스 · 쉬고 있는 소스 · 가장 오래된 소스 순으로 돌려 씀
    // 1.1~ 4 → 8: 0.07초마다 하나씩 쓰면 4개는 0.28초에 한 바퀴라, 아직 울리는 코인 소리(약 0.47초)를 Stop() 으로 뚝 끊어 딸깍거렸음
    const int CoinSourceCount = 8;
    readonly List<AudioSource> coinSources = new List<AudioSource>();
    int coinSourceNext;
    AudioSource CoinSource(float pitch)
    {
        if (audioSource == null) return null;
        for (int i = 0; i < coinSources.Count; i++)
            if (Mathf.Approximately(coinSources[i].pitch, pitch)) return coinSources[i];
        for (int i = 0; i < coinSources.Count; i++)
            if (!coinSources[i].isPlaying) return coinSources[i];
        if (coinSources.Count < CoinSourceCount)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            s.spatialBlend = audioSource.spatialBlend;
            s.volume = audioSource.volume;
            s.priority = audioSource.priority;
            coinSources.Add(s);
            return s;
        }
        AudioSource old = coinSources[coinSourceNext];
        coinSourceNext = (coinSourceNext + 1) % CoinSourceCount;
        old.Stop();
        return old;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 물리가 켜진 채 남은 코인 (CoinTag 를 거치지 않은 것)
        if (collision.CompareTag("coin"))
        {
            if (Cache<Coin>.Get != null) Juice.CoinPicked(collision.transform.position, GainCoins(CoinTag.ValueOf(collision.gameObject), 1));
            CoinTag.Release(collision.gameObject);
        }

        HandleContact(collision);
    }

    // 적과 닿아 있는 동안에도 무적 시간 간격으로 계속 피해를 받음
    private void OnTriggerStay2D(Collider2D collision)
    {
        HandleContact(collision);
    }

    // 일반 적은 닿으면 스스로 피해를 주고 자폭함 (EnermyController)
    // 보스는 사라지지 않으므로 여기서 처리
    Collider2D ownCollider;
    Collider2D Body => ownCollider != null ? ownCollider : (ownCollider = GetComponent<Collider2D>());

    void HandleContact(Collider2D collision)
    {
        if (collision.CompareTag("boss"))
        {
            if (collision.GetComponent<BossPart>() != null) return;     // 등불 · 묘비 · 영혼구는 닿아도 몸통 피해 없음 (영혼구는 스스로 터짐)
            // 넉넉한 히트박스 가장자리에 스친 것만으로는 안 맞음 (몸 안쪽에 닿아야)
            if (collision.TryGetComponent(out EnemyHitbox hitbox) && !hitbox.Touching(ContactCenter, ContactR)) return;     // 1.4.4~ 몸 콜라이더 → 몸 아래쪽 접촉 원
            bosss boss = collision.GetComponent<bosss>();
            if (boss != null && boss.IsDead) return;     // 1.0.4~ 쓰러져 사라지는 중인 보스 몸에 닿아도 피해 없음
            TryHit(boss != null ? boss.contactDamage : bossContactDamage);
        }
    }

    [Header("피격")]
    public float bossContactDamage = 25f;
    // 맞은 뒤 이 시간 동안은 다시 맞지 않음 (여러 마리에게 동시에 맞는 것 방지)
    public float hurtInvincibleTime = 0.6f;
    public const float MaxDef = 0.7f;
    public const float BossHitShare = 0.4f;     // 1.0.4~ 보스전 중 한 대 피해 상한 (최대 체력 비율)

    // 1.4.4~ 피격 판정 한 곳 (플레이테스트: 「히트박스가 너무 엄격 · 탑다운이면 캐릭터 위쪽은 널널하게, 그림자를 판정으로」)
    //   몸 그림은 transform 위치(목 높이, 머리끝 약 +0.6)에서 발끝 약 −2.4 까지 — 예전 적 탄 · 패턴 판정은 transform 위치 기준 반지름 0.4 라
    //   머리 위로 스친 탄 · 머리 위쪽 장판도 맞았음 → 판정 중심을 아래(가슴 · 배)로 내리고 작게 (위쪽 가장자리 +0.4 → −0.12, 0.52 내려감)
    //   적의 조준 · 장판 위치는 모두 transform 위치를 노리므로 발끝까지는 내리지 않음 (가만히 서 있으면 조준탄 · 장판은 그대로 맞음)
    public const float HurtDropY = 0.42f;                   // 판정 중심: transform 위치에서 아래로 (월드, 예전 0)
    public const float HurtRadius = 0.3f;                   // 판정 반지름 (월드, 예전 0.4)
    public const float HurtShrink = 0.4f - HurtRadius;      // 너비 숫자에 예전 반지름 0.4 가 들어 있던 직선 · 원 판정은 거리에 이만큼 더해 같이 작아짐
    // 거대화 · 축소 물약처럼 몸 크기가 바뀌면 판정 자리(· 접촉 원 크기)도 같이 (시작 크기 대비 배율 · 탄 판정 반지름은 그대로)
    float baseScaleY;
    public float BodyScale => baseScaleY > 0f ? Mathf.Abs(transform.lossyScale.y) / baseScaleY : 1f;
    public Vector2 HurtCenter { get { Vector3 p = transform.position; return new Vector2(p.x, p.y - HurtDropY * BodyScale); } }
    // 몸통 접촉(적 · 보스 몸에 닿기): 예전엔 몸 콜라이더(중심 −1.15 · 반지름 약 1.47, 그림보다 넓음) 그대로 → 중심은 같고 반지름 1.0 (위끝 +0.32 → −0.15 · 좌우 폭은 덜 줄임)
    //   벽 · 이동 막힘 · 코인 줍기는 몸 콜라이더 그대로
    public const float ContactDropY = 1.15f, ContactRadius = 1f;
    public Vector2 ContactCenter { get { Vector3 p = transform.position; return new Vector2(p.x, p.y - ContactDropY * BodyScale); } }
    public float ContactR => ContactRadius * BodyScale;

    private float invincibleUntil = 0f;

    public bool IsInvincible => Time.time < invincibleUntil;

    // 피격 연출: 화면 가장자리 붉은 번쩍임, 흔들림
    // 받은 피해 숫자는 PlayerHP 가 Damaged 를 받아 DamagePopup.ShowHurt 로 띄움 (짧은 사이 피해를 숫자 하나로 합침 · 등급별 크기)
    void ShowHurt(float taken)
    {
        DamageFlash.Show(0.45f + taken / Mathf.Max(1f, PlayerMaxHealth) * 2.5f);
        if (SpecialAbilities.SharedFx != null)
            SpecialAbilities.SharedFx.Shake(0.2f + Mathf.Min(0.3f, taken / 60f), 0.15f);
    }

    // 실제로 체력이 깎였을 때 (taken, 깎기 전 체력) — HUD 체력바 · 받은 피해 숫자가 구독
    public static System.Action<float, float> Damaged;

    // 실제로 피해를 받은 횟수 (보스 결계를 맞지 않고 버텼는지 · 업적)
    public static int HurtCount;

    // 피해를 받았으면 true, 무적이라 무시됐으면 false
    // from: 때린 코드의 파일 (컴파일러가 채움) → 이번 판 받은 피해 출처
    public bool TryHit(float amount, [System.Runtime.CompilerServices.CallerFilePath] string from = "")
    {
        if (IsInvincible) return false;
        // 영혼 트리 보호막: 공격 한 번을 막음
        if (special != null && special.ConsumeBarrier()) { invincibleUntil = Time.time + 0.3f; return false; }
        // 1.2~ 초월 카드 「영혼 보호막」: 한동안 맞지 않았으면 한 번 막음
        if (TranscendRules.WardBlocks()) { invincibleUntil = Time.time + 0.5f; return false; }
        // 도적 회피 본능: 확률로 공격을 피함
        if (special != null && special.KitDodge()) return false;

        invincibleUntil = Time.time + hurtInvincibleTime + BossUltimate.DevHurtGraceBonus + BossUltimate.ErosionHurtGrace;     // 개발자 · 엄마 결계 중 +0.2초 (한 패턴에 연달아 맞지 않게)

        if (!SkinAudio.PlayHurt() && audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound, GameSettings.SfxVolume);      // 캐릭터 스킨 피격 소리

        // 피해 감소는 합쳐도 최대 70% (단단한 신체 + 강철 갑옷 + 거대화 물약이 겹쳐 무적이 되지 않게)
        float taken = amount * GameMode.DamageMul * Chapters.HurtMul * (1f - Mathf.Clamp(def, 0f, MaxDef));     // HurtMul: 장 자리에 맞춘 세기 (2.1.1)
        if (CharacterKit.Instance != null) taken *= CharacterKit.Instance.TakenMul;
        if (special != null) taken = special.AdjustTaken(taken);      // 영혼 트리: 강인함 · 완충
        taken = SignatureSkills.Taken(taken);                         // 혈갑 보호막 · 환영 연막
        // 1.0.4~ 보스전 한 방 즉사 보호: 보스가 살아 있는 동안 한 대는 최대 체력의 BossHitShare 까지
        // (3장 지옥의 군주 화염 돌진이 보통에서 약 103 으로 기본 체력 100 을 한 번에 넘었음 · 4장은 원래 20% 상한)
        // 1.2.6~ 공간 침식 중에는 결계 공격이 아닌 피해(돌진 · 몸통 · 잡몹 등)도 기획대로 한 대 25% 상한 (예전엔 40% 로 맞았음)
        float hitShare = BossUltimate.Active && BossUltimate.ErosionActive ? Mathf.Min(BossHitShare, SpaceErosion.HitShare) : BossHitShare;
        if (bosss.LiveCount > 0) taken = Mathf.Min(taken, PlayerMaxHealth * hitShare);
        if (taken <= 0f) return true;

        // 1.0.4~: 한 방에 쓰러질 피해라도 이번 장 「최후의 보루」가 남아 있으면 체력 1로 버티고 발동 (아래 LastStand.Check)
        // (예전엔 체력이 0 이하가 되면 발동하지 않아 「장마다 한 번 무적」이 한 방 사망에는 소용없었음) · 판마다 한 번인 불사의 맹세보다 먼저
        if (PlayerHealth - taken <= 0 && PlayerHealth > 0f && LastStand.Ready) taken = Mathf.Max(0f, PlayerHealth - 1f);

        // 불사의 맹세: 죽을 피해를 한 번 버팀
        if (PlayerHealth - taken <= 0 && special != null && special.TryUndying())
        {
            PlayerHealth = Mathf.Max(1f, special.UndyingReviveHealth(PlayerMaxHealth));
            invincibleUntil = Time.time + 3f;
            DamageFlash.Show(1f);
            return true;
        }

        RunStats.Hurt(Mathf.Min(taken, Mathf.Max(0f, PlayerHealth)), from);
        float before = PlayerHealth;
        PlayerHealth -= taken;
        HurtCount++;
        Damaged?.Invoke(taken, before);
        ShowHurt(taken);
        special?.OnPlayerHurt();
        SignatureSkills.Hurt(taken);
        LastStand.Check(this);      // 체력 30% 아래: 장마다 한 번 2초 무적 + 밀쳐 내기 (1.0.1)

        if (PlayerHealth <= 0) StartCoroutine(DeathSequence());

        return true;
    }

    // 쓰러지는 순간: 바로 화면을 바꾸지 않고 잠깐 느려지며 붉게 물든 뒤 결과 화면으로
    public bool IsDying { get; private set; }

    IEnumerator DeathSequence()
    {
        if (IsDying) yield break;
        IsDying = true;
        RunStats.CaptureForResult();        // 1.1.9~ 결과 화면 「빌드 자세히 보기」 스냅샷 (쓰러지는 순간의 빌드)
        GameInput.ResetAttackLock();        // 1.1.9~ 공격 고정은 쓰러지면 풀림
        RunSave.Abandon();          // 죽으면 이어할 판은 지움 (2.1.9) · 1.2.3~ 저장하는 판만 (일일 도전 · 튜토리얼에서 쓰러져도 남아 있던 저장은 그대로)
        invincibleUntil = float.MaxValue;
        CancelSkill();
        move = Vector3.zero;
        DamageFlash.Show(1f);
        Hostile.Play("bigboom", 0.7f, 0.6f);
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.Shake(0.5f, 0.3f);
        Fx.Spawn("fx_deathburst", transform.position, 4f, new Color(1f, 0.35f, 0.3f), 14f);

        // 일시정지 창이 열려도 실제 시간으로 흐름
        float t = 0f;
        while (t < 1.1f)
        {
            t += Time.unscaledDeltaTime;
            if (Time.timeScale > 0f) Time.timeScale = Mathf.Lerp(0.25f, 0.05f, t / 1.1f);
            yield return null;
        }
        Herald.RecordDeath();       // 1.2~ 영웅별 · 장별 · 연속 사망 (기억 조각 · 결과 화면 추천 · 전령 조건)
        yield return Herald.Play(false);        // 1.2~ 망자의 전령: 멈춘 채 한 줄 (넘기면 바로 · 설정에서 끔)
        SceneFlow.Go("GameOver");
    }

    // 무적 시간 동안 깜빡임
    void LateUpdate()
    {
        if (spriteRenderer == null) return;

        bool blinking = Time.time < invincibleUntil && !isSkillUsing && !IsDying;
        UnityEngine.Color c = spriteRenderer.color;
        c.a = (blinking && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.35f : 1f) * bodyAlpha;
        spriteRenderer.color = c;
    }

    // =====================================
    // 타겟 제거
    // =====================================

    void ClearTargets()
    {
        targets.Clear();

        foreach (GameObject mark in targetMarks) if (mark != null) Destroy(mark);

        targetMarks.Clear();
    }

    // =====================================
    // 타겟 범위 표시
    // =====================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere( transform.position, targetRange );
        // 1.4.4~ 피격 판정(빨강) · 몸통 접촉(주황) — 씬 뷰에서 플레이어를 고르면 보임
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(HurtCenter, HurtRadius);
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        Gizmos.DrawWireSphere(ContactCenter, ContactRadius);
        Gizmos.color = Color.white;
    }
}

