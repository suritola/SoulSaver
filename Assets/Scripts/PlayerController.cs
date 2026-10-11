
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
    const float BaseCoinPull = 2.5f;        // 카드 없이도 끌어오는 코인 범위 (2.1.8)

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
    // 타겟팅 스킬 사용 중 + 끝난 뒤 자동 연사 중 (이때는 직접 쏠 수 없음)
    public bool IsSkillUsing => isSkillUsing || isVolleying;
    private bool isVolleying;
    public Camera MainCamera => mainCamera;

    private float nextTargetTime = 0f;

    private float nextShootTime = 0f;

    private List<EnermyController> targets =
        new List<EnermyController>();

    private List<GameObject> targetMarks =
        new List<GameObject>();

    private Vector3 move;

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

    // 이동 속도 상한 (1.0.5): 빨라지는 효과(그림자 숙련 · 연쇄 처치 · 도망자의 발걸음 · 상점 · 물약 …)가 곱으로 쌓여
    // 후반 도적이 주체할 수 없이 빨라졌음 → 기본 속도의 1.3배까지는 그대로, 넘는 몫은 40%만, 최대 1.7배 (둔화는 그대로 적용)
    const float SpeedSoftCap = 1.3f, SpeedOverRate = 0.4f, SpeedHardCap = 1.7f;
    float baseMoveSpeed = -1f;
    float CappedSpeed(float raw)
    {
        if (baseMoveSpeed <= 0f) return raw;
        float r = raw / baseMoveSpeed;
        if (r > SpeedSoftCap) r = Mathf.Min(SpeedHardCap, SpeedSoftCap + (r - SpeedSoftCap) * SpeedOverRate);
        return baseMoveSpeed * r;
    }

    void Start()
    {
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
    // 1.0.6: 최고점은 그대로 두고 성장 속도만 늦춤 — 레벨마다 +50 → +65 (초반 +15%, 레벨 20쯤 +30%)
    public static float NeedExp(int level) => 50 + level * 65;

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
        DamageSource.Current = DamageSource.Basic;      // 재장전 · 평타 (아래 우클릭 부분은 필살기)
        try { UpdateInput(); }
        finally { DamageSource.Current = null; }
    }

    // 코루틴도 시작할 때의 피해 출처(평타 · 필살기)를 이어 감 (RunStats)
    public new Coroutine StartCoroutine(IEnumerator routine) => base.StartCoroutine(DamageSource.Keep(routine));

    void UpdateInput()
    {

        needEXP = NeedExp(level);

        // 생명의 샘: 초당 체력 회복
        if (regenPerSecond > 0f && PlayerHealth > 0f && PlayerHealth < PlayerMaxHealth)
            PlayerHealth = Mathf.Min(PlayerMaxHealth, PlayerHealth + regenPerSecond * Time.deltaTime);

        // 코인 자석: 주변 코인이 날아옴 (2.1.8: 카드가 없어도 기본으로 2.5칸은 끌어옴, 카드는 거기에 더함)
        float pull = coinMagnetRange + BaseCoinPull;
        if (pull > 0f && Time.timeScale > 0f)
        {
            // 떨어진 코인 목록 (매 프레임 씬 전체를 태그로 뒤지던 것을 대신함)
            var coins = CoinTag.All;
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                Transform c = coins[i];
                if (c == null) { coins.RemoveAt(i); continue; }
                float d = Vector2.Distance(c.position, transform.position);
                if (d < pull) c.position = Vector3.MoveTowards(c.position, transform.position, (10f + (pull - d) * 3f) * Time.deltaTime);
            }
        }

        // 상점 · ESC · 레벨업 등으로 멈춘 동안에는 입력을 받지 않음
        // (멈춘 화면에서 클릭하면 총이 나가거나 스킬이 시간을 다시 흐르게 하던 문제)
        // 시작 · 엔딩 연출 중에도 조작하지 않음
        if (IsPaused || StoryDirector.Playing || IsDying)
        {
            move = Vector3.zero;
            return;
        }

        // =========================
        // 재장전 입력
        // =========================

        bool holdingSpecial = special != null && special.WeaponActive;
        if (KeyBindings.Down(GameAction.Reload) && !holdingSpecial && (CharacterKit.Instance == null || CharacterKit.Instance.UsesAmmo)) if (NowBullet < MaxBullet) StartCoroutine(Reload());

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
        if (NowBullet <= 0 && !isReloading && (CharacterKit.Instance == null || CharacterKit.Instance.UsesAmmo)) StartCoroutine(Reload());

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
    }

    // =====================================
    // 재장전
    // =====================================

    IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        reload = 0f;
        special?.ReloadShockwave(transform.position);
        SignatureSkills.ReloadStart();          // 탄피 지뢰 · 빈 주머니의 비

        if (!CharacterData.IsGunner && SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.Play("rl_stars", 0.8f);
        else if (audioSource != null && reloadSound != null) audioSource.PlayOneShot(reloadSound, GameSettings.SfxVolume);

        while (reload < reloadTime)
        {
            // 필살기 조준의 느린 시간에도 제 속도로, 메뉴로 멈춘 동안에는 멈춤
            if (!IsPaused) reload += Time.unscaledDeltaTime;
            yield return null;
        }

        NowBullet = MaxBullet;

        reload = 0f;

        isReloading = false;
        SignatureSkills.ReloadEnd();            // 섬광 장전 · 속사 장전
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

        nextShootTime = Time.time + ShootSpeed / fireRateMultiplier;
        // 탄창 저주: 마지막 한 발 강화
        bool cursed = special != null && special.IsLastBulletCursed(NowBullet);
        NowBullet--;

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

        // 멀티샷 퍼지는 각도
        float spreadAngle = 10f;

        float bulletDamage = damage * damageMultiplier * MultiShotDamageRate(multiShot) * (cursed ? 3f : 1f);
        SignatureSkills.Attacked(startPosition, direction);

        float shotRate = MultiShotDamageRate(multiShot);

        if (multiShot == 1)
        {
            Bullet one = CreateBullet(startPosition, direction, bulletDamage, pene, 0, false);
            special?.CurseBullet(one, cursed);
            special?.ApplyGunCards(one, false);
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
            }
        }
        special?.EvoExtraShots(startPosition, direction, bulletDamage / Mathf.Max(0.01f, cursed ? 3f : 1f));      // 쌍발 총구 · 산탄 총구
    }



    // =====================================
    // 총알 생성
    // =====================================

    // 마우스가 버튼 같은 UI 위에 있으면 사격하지 않음
    static bool PointerOverUI()
    {
        return !GameInput.Auto && UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
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

        GameObject newBullet = Instantiate(bulletPrefab, startPosition, Quaternion.identity);

        Bullet bullet = newBullet.GetComponent<Bullet>();
        if (SkinFx.WeaponSkin != null) newBullet.AddComponent<SkinTint>();        // 무기 스킨: 총알 · 투사체 색

        if (bullet != null)
        {
            bullet.Dir = direction;
            bullet.pene = penes;
            bullet.blood = blood;
            bullet.damage = damage;
            bullet.isSkill = isSkill;
            bullet.knockBack = knockBack * knockBackRate;
            newBullet.AddComponent<BulletGlow>();
            // 스킬 게이지는 화염 방사기 기준으로 천천히 참
            bullet.skillCharge = knockBackRate * GaugeRate;
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

    public void Slow(float factor, float seconds)
    {
        slowFactor = factor;
        slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
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
        if (targets.Count >= limit) return;

        List<Collider2D> enemies = Specials.Overlap(transform.position, targetRange, targetHits);

        EnermyController closestEnemy = null;

        float closestDistance = Mathf.Infinity;

        foreach (Collider2D enemyCollider in enemies)
        {
            if (!enemyCollider.CompareTag("enermy")) continue;

            EnermyController enemy = enemyCollider.GetComponent<EnermyController>();

            if (enemy == null || targets.Contains(enemy)) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy == null) return;

        targets.Add(closestEnemy);

        // 진화한 무기의 필살기면 무기다운 표식 (범위형 무기는 표식 없음)
        if (special != null && special.UltReplaced)
        {
            GameObject weaponMark = special.MarkTarget(closestEnemy.transform);
            if (weaponMark != null) targetMarks.Add(weaponMark);
        }
        else if (targetMarkPrefab != null)
        {
            GameObject mark = Instantiate(targetMarkPrefab, closestEnemy.transform.position, Quaternion.identity);
            TargetMark targetMark = mark.GetComponent<TargetMark>();
            if (targetMark != null) targetMark.target = closestEnemy.transform;
            targetMarks.Add(mark);
            if (NowBullet < MaxBullet) NowBullet++;
        }
    }

    // =====================================
    // 스킬 종료
    // =====================================

    void EndSkill()
    {
        if (!isSkillUsing) return;

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

    IEnumerator ShootTargets()
    {
        isVolleying = true;

        // 필살기를 바꾸는 진화(쌍권총 · 총알 폭풍)면 그 무기다운 일제 사격
        if (special != null && special.UltReplaced)
        {
            yield return StartCoroutine(special.WeaponVolley(targets, skillDamage, getHP));
            targets.Clear();
            isVolleying = false;
            yield break;
        }

        foreach (EnermyController target in targets)
        {
            if (target == null) continue;

            // 2.2.0: 명중률 — 방향을 첫 발 전에 한 번만 정하던 것을 매 발 다시 조준 (발밑 대신 몸 중심, 움직이는 적은 날아가는 동안 갈 자리를 앞질러)
            Collider2D body = target.GetComponent<Collider2D>();
            Vector3 lastPos = target.transform.position;
            float lastTime = Time.time;
            Vector2 velocity = Vector2.zero;

            for (int i = 0; i < ShotsPerTarget;  i++)
            {
                if (target == null) break;

                Vector3 now = target.transform.position;
                float dt = Time.time - lastTime;
                if (dt > 0.001f) velocity = Vector2.ClampMagnitude((now - lastPos) / dt, 8f);     // 밀려난 거리로 튀지 않게
                lastPos = now; lastTime = Time.time;

                Vector3 startPosition = transform.position + new Vector3(0, -0.5f, 0);
                Vector3 aim = body != null && body.enabled ? body.bounds.center : now;

                FaceTowards(aim);
                PlayerMotion.Of(this).Recoil(aim - startPosition, 0.6f);      // 몸동작 (1.0.5)

                if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound, GameSettings.SfxVolume);

                Bullet shot = CreateBullet(startPosition, ((Vector2)(aim - startPosition)).normalized, skillDamage * (special != null ? special.UltPower(SpecialAbilities.PistolUlt) : 1f), pene, getHP, true);
                // 2.2.1: 필살기 총알에도 총알 카드 · 고유 스킬(영혼 탄환 · 총열 과열 · 소각탄 · 전기탄 …), 덤 총알 · 유도 · 폭발탄은 빼고(light)
                SignatureSkills.UltShots = true;
                special?.ApplyGunCards(shot, true, false);
                SignatureSkills.UltShots = false;
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

        // 2.2.0: 나머지 진화 무기는 원래 조준 사격 뒤에 그 무기의 궁극기가 절반 세기로 덧붙음 (다른 캐릭터와 같게)
        if (special != null && special.UltAdds)
            yield return StartCoroutine(special.WeaponVolley(new List<EnermyController>(targets), skillDamage * GunUltAddMul, getHP));

        targets.Clear();
        isVolleying = false;
    }

    // =====================================
    // 화면 페이드
    // =====================================

    // 1.0.7: 조준을 빨리 끝내면 아직 진하게 바뀌던 코루틴이 흐려지는 코루틴보다 늦게 끝나 파란 화면이 남았음
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
        if (skillEffectPanel == null || isSkillUsing || screenFade != null) return;
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

    void HealOnKill(Vector3 pos)
    {
        if (healOnKill > 0f && PlayerHealth > 0f) PlayerHealth = Mathf.Min(PlayerMaxHealth, PlayerHealth + healOnKill);
    }

    void OnDestroy()
    {
        EnermyController.Killed -= HealOnKill;
    }

    int dropCoin;
    float lastCoinSound = -1f;
    int coinStreak;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("coin"))
        {
            Coin coin = Cache<Coin>.Get;

            if (coin != null)
            {
                coin.AddCoin(1 + bonusCoin);
                SignatureSkills.CoinPicked(1 + bonusCoin);      // 황금 시대 (스킬 진화)
                // 한꺼번에 여러 개를 주우면 소리가 겹쳐 너무 커지므로: 0.07초에 한 번만, 연달아 주울수록 작게
                float now = Time.unscaledTime;
                coinStreak = now - lastCoinSound < 0.6f ? coinStreak + 1 : 0;
                bool sound = now - lastCoinSound >= 0.07f;
                if (sound)
                {
                    lastCoinSound = now;
                    float vol = Mathf.Max(0.4f, 1f / (1f + 0.15f * coinStreak));
                    if (audioSource != null && getCoin != null) audioSource.PlayOneShot(getCoin, GameSettings.SfxVolume * vol);
                }
                Juice.CoinPicked(collision.transform.position, sound);
            }
            Destroy( collision.gameObject );
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
    void HandleContact(Collider2D collision)
    {
        if (collision.CompareTag("boss"))
        {
            if (collision.GetComponent<BossPart>() != null) return;     // 등불 · 묘비 · 영혼구는 닿아도 몸통 피해 없음 (영혼구는 스스로 터짐)
            bosss boss = collision.GetComponent<bosss>();
            TryHit(boss != null ? boss.contactDamage : bossContactDamage);
        }
    }

    [Header("피격")]
    public float bossContactDamage = 25f;
    // 맞은 뒤 이 시간 동안은 다시 맞지 않음 (여러 마리에게 동시에 맞는 것 방지)
    public float hurtInvincibleTime = 0.6f;
    public const float MaxDef = 0.7f;

    private float invincibleUntil = 0f;

    public bool IsInvincible => Time.time < invincibleUntil;

    // 피격 연출: 화면 가장자리 붉은 번쩍임, 흔들림, 머리 위 피해 숫자
    void ShowHurt(float taken)
    {
        DamageFlash.Show(0.45f + taken / Mathf.Max(1f, PlayerMaxHealth) * 2.5f);
        if (SpecialAbilities.SharedFx != null)
        {
            SpecialAbilities.SharedFx.Shake(0.2f + Mathf.Min(0.3f, taken / 60f), 0.15f);
            SpecialAbilities.SharedFx.FloatText(transform.position, "-" + Mathf.CeilToInt(taken), new Color(1f, 0.3f, 0.28f), 5f, 0f);
        }
    }

    // 실제로 피해를 받은 횟수 (보스 결계를 맞지 않고 버텼는지 · 업적)
    public static int HurtCount;

    // 피해를 받았으면 true, 무적이라 무시됐으면 false
    // from: 때린 코드의 파일 (컴파일러가 채움) → 이번 판 받은 피해 출처
    public bool TryHit(float amount, [System.Runtime.CompilerServices.CallerFilePath] string from = "")
    {
        if (IsInvincible) return false;
        // 영혼 트리 보호막: 공격 한 번을 막음
        if (special != null && special.ConsumeBarrier()) { invincibleUntil = Time.time + 0.3f; return false; }
        // 도적 회피 본능: 확률로 공격을 피함
        if (special != null && special.KitDodge()) return false;

        invincibleUntil = Time.time + hurtInvincibleTime;

        if (!SkinAudio.PlayHurt() && audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound, GameSettings.SfxVolume);      // 캐릭터 스킨 피격 소리

        // 피해 감소는 합쳐도 최대 70% (단단한 신체 + 강철 갑옷 + 거대화 물약이 겹쳐 무적이 되지 않게)
        float taken = amount * GameMode.DamageMul * Chapters.HurtMul * (1f - Mathf.Clamp(def, 0f, MaxDef));     // HurtMul: 장 자리에 맞춘 세기 (2.1.1)
        if (CharacterKit.Instance != null) taken *= CharacterKit.Instance.TakenMul;
        if (special != null) taken = special.AdjustTaken(taken);      // 영혼 트리: 강인함 · 완충
        taken = SignatureSkills.Taken(taken);                         // 혈갑 보호막 · 환영 연막
        if (taken <= 0f) return true;

        // 불사의 맹세: 죽을 피해를 한 번 버팀
        if (PlayerHealth - taken <= 0 && special != null && special.TryUndying())
        {
            PlayerHealth = Mathf.Max(1f, special.UndyingReviveHealth(PlayerMaxHealth));
            invincibleUntil = Time.time + 3f;
            DamageFlash.Show(1f);
            return true;
        }

        RunStats.Hurt(Mathf.Min(taken, Mathf.Max(0f, PlayerHealth)), from);
        PlayerHealth -= taken;
        HurtCount++;
        ShowHurt(taken);
        special?.OnPlayerHurt();
        SignatureSkills.Hurt(taken);
        LastStand.Check(this);      // 체력 30% 아래: 장마다 한 번 2초 무적 + 밀쳐 내기 (1.0.5)

        if (PlayerHealth <= 0) StartCoroutine(DeathSequence());

        return true;
    }

    // 쓰러지는 순간: 바로 화면을 바꾸지 않고 잠깐 느려지며 붉게 물든 뒤 결과 화면으로
    public bool IsDying { get; private set; }

    IEnumerator DeathSequence()
    {
        if (IsDying) yield break;
        IsDying = true;
        RunSave.Delete();           // 죽으면 이어할 판은 지움 (2.1.9)
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
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameOver");
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
    }
}

