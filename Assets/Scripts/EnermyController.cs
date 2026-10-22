using System.Collections;
using UnityEngine;

public class EnermyController : MonoBehaviour
{
    public float speed = 2f;
    public int setEnemyHP = 10;
    public float EnemyHealth;


    private Transform player;
    private Vector3 move;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    public int KilledEnemy;

    public GameObject coin;

    [Header("보상 / 피해")]
    public float contactDamage = 5f;   // 플레이어와 닿았을 때 주는 피해
    // 부딪혀도 적이 죽지 않고 계속 붙어 있으므로 실제로는 이만큼만
    public const float ContactScale = 0.6f;
    public int expReward = 20;          // 처치 경험치
    public int coinDrop = 1;            // 떨어뜨리는 코인 수

    [Header("움직임")]
    // 좌우로 흔들리며 다가옴 (0이면 직선)
    public float zigzagAmplitude = 0f;
    public float zigzagFrequency = 3f;
    // 주기적으로 빠르게 돌진 (0이면 없음)
    public float dashInterval = 0f;
    public float dashDuration = 0.4f;
    public float dashSpeedMultiplier = 2.5f;
    // 총알 넉백을 이 비율만큼만 받음
    public float knockBackTaken = 1f;

    float moveTime;

    [Header("겹침 방지")]
    // 다른 적과 겹친 만큼 밀어내는 속도 (초당)
    public float separationSpeed = 4f;

    [Header("중간 보스")]
    // 닿아도 자폭하지 않고 계속 싸움
    public bool survivesContact = false;
    // 평소 몸 색 (피격 후 이 색으로 돌아옴)
    public Color baseColor = Color.white;
    // 이 적이 처치됐을 때
    public System.Action onKilled;

    [Header("스킬")]
    public EnemySkillType skill = EnemySkillType.None;
    public float skillCooldown = 0f;       // 0이면 스킬 기본값
    // 스킬 시전 중에는 걸어서 움직이지 않음
    [HideInInspector] public bool casting;

    public bool IsDead => isDead;

    // 잠깐 느려짐 (빙결 플라스크 · 연막탄 · 덫: 0 이면 묶임)
    float slowUntil, slowMul = 1f;
    public float SlowUntil => slowUntil;       // 1.4.1~ 덫 묶임 면역 끝 계산 (ArcherPaths.MarkRooted)
    public void Slow(float mul, float seconds)
    {
        slowMul = Time.time < slowUntil ? Mathf.Min(slowMul, mul) : mul;
        slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
    }

    // 시간 왜곡 (적 전체 감속)
    public static float GlobalSpeedMultiplier = 1f;
    // 거울 분신이 있으면 플레이어 대신 분신을 쫓음
    public static Transform Decoy;
    // 적이 처치됐을 때 (영혼 모으기 등)
    public static System.Action<Vector3> Killed;
    // 1.2~ 지금 알리는 처치가 처치 수에 들어가는지 (RunStats.Kills · Juice 처치 이정표가 봄) — 맵 물체 「정화」로 쓰러진 적 중 절반은 false
    public static bool KillCounts { get; private set; } = true;
    // 1.2~ 이 적은 처치 수(보스까지 처치 수 · 이번 판 처치 수 · 이정표)에 넣지 않음 (MapProps.DropPurify 가 정함, 경험치 · 코인 · 포인트는 그대로)
    [HideInInspector] public bool skipKillCount;
    // 받는 피해를 바꾸는 능력 (약점 간파 · 급소 노리기 · 사냥감 표식)
    public static System.Func<EnermyController, float, float> DamageHook;

    private CircleCollider2D bodyCollider;
    // 그림 가운데에 맞춘 히트박스 (맞는 판정은 넉넉하게, 몸통 피해는 몸 안쪽만)
    private EnemyHitbox hitbox;
    // 살아 있는 적의 콜라이더 → 적 (밀어내기 계산용)
    static readonly System.Collections.Generic.Dictionary<Collider2D, EnermyController> byCollider = new System.Collections.Generic.Dictionary<Collider2D, EnermyController>();
    // 48칸: 뭉친 무리에서도 이웃 적을 놓치지 않게 (예전 24칸은 탄 · 코인으로 차서 밀어내기가 빠지기도 했음)
    private static readonly Collider2D[] nearby = new Collider2D[48];
    static int EnemyLayer = -2;
    // 밀어내기 검색: 적 층만 (층이 없으면 플레이어 탄 층만 뺌) · 트리거 포함
    static ContactFilter2D separationFilter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = ~0 };
    // 1.1.9~ 콜라이더로 적 찾기 (멈춘 동안 적 위에 마우스 → 설명, PauseInspect)
    public static EnermyController FromCollider(Collider2D c)
    {
        if (c == null) return null;
        if (byCollider.TryGetValue(c, out EnermyController e) && e != null) return e;
        return c.GetComponentInParent<EnermyController>();
    }

    // 난이도 배율을 이미 적용했는지 (복제돼도 두 번 곱하지 않게)
    [HideInInspector] public bool difficultyApplied;
    // 보스가 나오기 전에 생긴 적 (처치 수로 보스를 부름): 처치 수가 늘어난 만큼 보상 · 영혼 조각을 나눔
    [HideInInspector] public bool countsTowardBoss;
    // 1.2.3~ 처치 포인트 배율 (무한 모드 개발자 · 엄마전 약한 잡몹 0.25 · 경험치 · 처치 수 · 영혼 조각은 그대로)
    [System.NonSerialized] public float pointMul = 1f;
    [System.NonSerialized] public bool finalMob;           // 1.2.3~ 규칙 E 잡몹 (EnemySpawner.finalMobs 로 따로 셈)
    // 1.1~ 영혼 부스러기 무리 (SwarmWave): 있으면 이동 · 몸통 닿기 · 죽은 뒤 처리를 부스러기가 맡음 (체력바 없음 · 한 화면 최대 수 · 보스까지 처치 수와 따로 셈)
    [HideInInspector] public SwarmMote swarm;

    // 생기자마자 대응표에 등록 (같은 물리 단계의 다른 적도 바로 알아보게), 사라지면 지움
    void Awake()
    {
        CircleCollider2D c = GetComponent<CircleCollider2D>();
        if (c != null) byCollider[c] = this;
        // 적 전용 층 (밀어내기 검색이 적만 보게 · 코인 · 회복약과의 접촉은 충돌 행렬에서 끔) — 프리팹 · 코드로 만든 적 모두
        if (EnemyLayer == -2)
        {
            EnemyLayer = LayerMask.NameToLayer("Enemy");
            int shot = LayerMask.NameToLayer("PlayerShot");
            separationFilter.SetLayerMask(EnemyLayer >= 0 ? 1 << EnemyLayer : shot >= 0 ? ~(1 << shot) : ~0);
        }
        if (EnemyLayer >= 0 && gameObject.layer == 0) gameObject.layer = EnemyLayer;
        // 생긴 프레임에 맞아도 색 바꾸기 · 피해 숫자가 깨지지 않게 미리
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start 가 끝나 체력이 정해졌는지 (그 전에 맞으면 체력 0 에서 깎여 바로 죽던 문제)
    bool ready;

    void OnDestroy()
    {
        CircleCollider2D c = bodyCollider != null ? bodyCollider : GetComponent<CircleCollider2D>();
        if (c != null && byCollider.TryGetValue(c, out EnermyController e) && e == this) byCollider.Remove(c);
    }

    void Start()
    {
        SpriteOutline.Enemy(gameObject);        // 1.8.7 짙은 윤곽선 (배경과 분리)
        midBoss = -1;                           // 스폰 직후 붙은 중간 보스 표식 · 황금 고블린을 다시 봄
        if (!difficultyApplied)
        {
            difficultyApplied = true;
            // 2.1.1~: 장 순서가 바뀌어도 그 자리(몇 번째 장)의 원래 세기로 (Chapters)
            int chapter = Chapters.CurrentStage;
            float rw = Chapters.RewardMul(chapter);
            if (rw != 1f) { expReward = Mathf.Max(1, Mathf.RoundToInt(expReward * rw)); coinDrop = Mathf.Max(1, Mathf.RoundToInt(coinDrop * rw)); }
            setEnemyHP = Mathf.Max(1, Mathf.RoundToInt(setEnemyHP * GameMode.EnemyHpMul * Chapters.EnemyHpMul(chapter)));
            speed *= GameMode.EnemySpeedMul * Chapters.EnemySpeedMul(chapter);     // 앞쪽 장은 느리게 (2.1.5)
            // 보스까지 세는 적은 늘어난 처치 수만큼 보상을 나눔 (한 판 총량은 그대로)
            // 보스전 중 부하 · 중간 보스 · 소환된 적은 시간에 따라 나오므로 예전 배율 그대로
            int coinsBefore = Mathf.Max(coinDrop, Mathf.RoundToInt(coinDrop * GameMode.FixedRewardMul));
            if (countsTowardBoss)
            {
                expReward = Mathf.Max(expReward > 0 ? 1 : 0, Mathf.RoundToInt(expReward * GameMode.RewardMul / EnemySpawner.DensityNow));      // 1.4.7~ 이야기 모드 적 수가 늘어난 만큼 한 마리 경험치는 줄임 (Chapters.StoryDensity)
                // 1.4.8~ 사용자 「코인 · 물약이 너무 많이 나온다」: 이야기 모드 적 수 배율만큼 나누고(경험치와 같게) · 모든 모드 절반 (DropCut)
                float density = EnemySpawner.DensityNow;
                coinDrop = GameMode.RoundRandom(coinsBefore / GameMode.KillStretch / density * DropCut);
                // 회복약도 처치 수가 늘어난 만큼 덜 나오게 (한 판 총량을 1.8.8 수준으로)
                hpChance = chanceofHP / GameMode.KillStretch / density * DropCut;
            }
            else
            {
                expReward = Mathf.RoundToInt(expReward * GameMode.FixedRewardMul);
                coinDrop = GameMode.RoundRandom(coinsBefore * DropCut);
                hpChance = chanceofHP * DropCut;
            }
        }
        EnemyHealth = setEnemyHP;
        bodyCollider = GetComponent<CircleCollider2D>();
        hitbox = EnemyHitbox.Fit(gameObject);

        // 플레이어 찾기
        PlayerController pc = Hostile.Player;      // 적마다 씬 전체를 뒤지지 않게 (캐시)
        player = pc != null ? pc.transform : null;

        // 컴포넌트 가져오기
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        KilledEnemy = 0;
        spriteRenderer.color = baseColor;

        // 머리 위 체력바
        if (swarm == null) healthBar = EnemyHealthBar.Attach(this, spriteRenderer);       // 1.1~ 부스러기는 체력 1 이라 체력바 없음

        if (skill != EnemySkillType.None)
        {
            EnemySkill s = gameObject.AddComponent<EnemySkill>();
            s.type = skill;
            s.cooldown = skillCooldown;
        }
        ready = true;
    }

    void Update()
    {
        // 죽었으면 아무것도 하지 않음
        if (isDead) return;

        if (player == null) return;

        // 적 → 플레이어 방향 계산
        move = (Decoy != null ? Decoy.position : player.position) - transform.position;

        // z축 제거
        move.z = 0;

        // 방향의 길이를 1로 맞춤
        move = move.normalized;

        // 좌우 방향 변경
        if (move.x < 0) { if (!spriteRenderer.flipX) spriteRenderer.flipX = true; }
        else if (move.x > 0) { if (spriteRenderer.flipX) spriteRenderer.flipX = false; }
    }

    void FixedUpdate()
    {
        if (swarm != null) return;      // 1.1~ 부스러기는 SwarmMote 가 직선으로 움직임 (밀어내기 · 벽 검사 없음)
        // 죽지 않았을 때만 이동
        if (!isDead && player != null && !casting)
        {
            moveTime += Time.fixedDeltaTime;

            Vector3 dir = move;
            if (zigzagAmplitude > 0f)
            {
                // 진행 방향의 수직으로 흔들림
                Vector3 side = new Vector3(-move.y, move.x, 0f);
                dir = (move + side * Mathf.Sin(moveTime * zigzagFrequency) * zigzagAmplitude).normalized;
            }

            float currentSpeed = speed * GlobalSpeedMultiplier * (Time.time < enrageUntil ? enrageMul : 1f) * (Time.time < slowUntil ? slowMul : 1f);
            // 1.4.4~ 돌진 예고 (플레이테스트 「심연 물방울처럼 대시하는 적의 시전 동작이 안 보임」): 돌진 직전 DashWarn 초 동안
            //   머리 위 「!」 + 돌진 방향 짧은 예고선 + 몸이 붉게 깜빡이며 뒤로 움츠렸다 떨림 → 주기(dashInterval) · 돌진 시간 · 배율은 그대로
            //   주기를 DashWarn 만큼 밀어 첫 돌진에도 예고가 먼저 옴 (예전엔 나오자마자 돌진)
            bool warnNow = false;
            if (dashInterval > 0f)
            {
                float warn = DashWarn;
                // 스폰 직후엔 돌진 직후 자리(+ 마리마다 다른 여유)에서 시작 — 나오자마자 「!」가 한꺼번에 뜨지 않게
                if (dashLead < 0f) dashLead = dashDuration + Random.Range(0f, Mathf.Max(0f, dashInterval - warn - dashDuration) * 0.5f);
                float phase = Mathf.Repeat(moveTime + dashLead, dashInterval);
                if (phase < dashDuration) currentSpeed *= dashSpeedMultiplier;
                else if (phase >= dashInterval - warn)
                {
                    warnNow = true;
                    if (!dashWarning) BeginDashWarn(warn);
                    // 뒤로 조금 물러나며 옆으로 떨림 (앞으로는 가지 않음)
                    Vector3 side = new Vector3(-move.y, move.x, 0f);
                    dir = (-move + side * Mathf.Sin(moveTime * 70f) * 0.8f).normalized;
                    currentSpeed *= 0.2f;
                    if (!hitFxRunning) spriteRenderer.color = Color.Lerp(baseColor, DashWarnTint, Mathf.PingPong(moveTime * 12f, 1f));
                }
            }
            if (!warnNow && dashWarning) EndDashWarn();

            Vector3 step = (dir * currentSpeed + Separation() * separationSpeed) * Time.fixedDeltaTime;

            // 구조물에 막히면 벽을 따라 미끄러짐 (이미 끼어 있으면 빠져나오도록 그대로 이동)
            // 다음 자리부터 확인 (막힌 경우가 드물어서 대부분 검색 한 번으로 끝남 · 결과는 같음)
            if (Blocked(transform.position + step) && !Blocked(transform.position))
            {
                Vector3 alongX = new Vector3(step.x, 0f, 0f);
                Vector3 alongY = new Vector3(0f, step.y, 0f);
                if (!Blocked(transform.position + alongX)) step = alongX;
                else if (!Blocked(transform.position + alongY)) step = alongY;
                else step = Vector3.zero;
            }
            transform.Translate(step);
        }
        else if (dashWarning) EndDashWarn();      // 시전 · 죽음으로 멈추면 예고를 거둠 (색 · 「!」가 남지 않게)
    }

    // 1.4.4~ 돌진 예고 (위 FixedUpdate) — 어려울수록 짧음 (스킬 예고와 같은 배율)
    float DashWarn => 0.3f * GameMode.WindupMul;
    static readonly Color DashWarnTint = new Color(1f, 0.35f, 0.3f);
    bool dashWarning;
    FxAnim dashMark;
    float dashLead = -1f;       // 돌진 주기 시작 자리 (처음 한 번 정함)

    void BeginDashWarn(float warn)
    {
        dashWarning = true;
        float top = spriteRenderer != null ? spriteRenderer.bounds.max.y : transform.position.y + 1f;
        dashMark = Fx.Play("fx_warn", new Vector3(transform.position.x, top + 0.6f, 0f), 1.1f, Color.white, 1f, 0f, 30, true, warn + 0.1f, VisualRules.WarnLayer);
        if (dashMark != null) dashMark.transform.SetParent(transform, true);
        // 예고선: 지금 방향으로 돌진 거리만큼 (돌진 중에도 플레이어를 따라 꺾이므로 짧게 · 최대 5칸)
        float len = Mathf.Clamp(speed * GlobalSpeedMultiplier * dashSpeedMultiplier * dashDuration, 2f, 5f);
        Vector3 from = transform.position;
        Hostile.Line(from, from + move * len, 0.5f, warn, new Color(1f, 0.3f, 0.25f, 0.75f));
    }

    void EndDashWarn()
    {
        dashWarning = false;
        if (dashMark != null) Destroy(dashMark.gameObject);
        dashMark = null;
        if (!isDead && !hitFxRunning && spriteRenderer != null) spriteRenderer.color = baseColor;
    }

    static readonly Collider2D[] wallHits = new Collider2D[16];
    static ContactFilter2D solidOnly = new ContactFilter2D { useTriggers = false };

    // 벽 검사 생략: 넓게(몸 + WallMargin) 한 번 봐서 벽이 없으면, 그 원 안에 들어가는 자리는 짧은 동안 다시 검색하지 않음 (결과 같음)
    // 넓은 원에 벽이 있으면 한동안은 예전처럼 매번 정확히 검색
    const float WallMargin = 0.75f, WallClearFor = 0.1f;
    Vector2 wallClearAt;
    float wallClearR, wallClearUntil = -1f, wallNearUntil = -1f;

    bool Blocked(Vector3 pos)
    {
        float r = bodyCollider != null ? BodyRadius(bodyCollider) * 0.8f : 0.5f;
        Vector2 center = (Vector2)pos + (bodyCollider != null ? bodyCollider.offset * (Vector2)transform.lossyScale : Vector2.zero);
        float now = Time.time;
        if (now < wallClearUntil)
        {
            float room = wallClearR - r;
            if (room > 0f && (center - wallClearAt).sqrMagnitude <= room * room) return false;
        }
        if (now >= wallNearUntil)
        {
            if (!WallIn(center, r + WallMargin))
            {
                wallClearAt = center;
                wallClearR = r + WallMargin;
                wallClearUntil = now + WallClearFor;
                return false;
            }
            wallNearUntil = now + 0.2f;
        }
        return WallIn(center, r);
    }

    static bool WallIn(Vector2 center, float r)
    {
        // 1.1.9: 트리거(주변 적 · 탄 · 코인)는 처음부터 빼고 검색 — 벽은 트리거가 아니라 결과 같음 · 뭉친 무리에서 8칸이 적으로 차 벽을 놓치던 것도 없어짐
        int n = Physics2D.OverlapCircle(center, r, solidOnly, wallHits);
        for (int i = 0; i < n; i++)
            if (!wallHits[i].isTrigger && wallHits[i].CompareTag("Wall")) return true;
        return false;
    }

    // 울부짖음 등으로 잠깐 빨라짐
    float enrageUntil;
    float enrageMul = 1f;

    public void Enrage(float mul, float seconds)
    {
        enrageMul = mul;
        enrageUntil = Time.time + seconds;
    }

    // 스킬로 스스로 터졌을 때 (처치로 인정)
    public void KillBySkill()
    {
        EnemyHealth = 0;
        Die(1);
    }

    // 1.1~ 영혼 부스러기 풀 (SwarmMote): 다시 꺼낼 때 죽은 표시를 풀고 체력을 채움 · 풀로 돌아갈 때 죽은 것으로 (꺼진 채 남은 참조가 때리지 않게)
    EnemyHealthBar healthBar;       // 머리 위 체력바 (타격감 · 부스러기는 없음)

    public void ReviveForPool()
    {
        isDead = false;
        skipKillCount = false;
        EnemyHealth = setEnemyHP;
        if (healthBar != null) healthBar.ResetFeel(EnemyHealth);
        if (bodyCollider != null) bodyCollider.enabled = true;
    }
    public void PoolDead() => isDead = true;

    // 몸 반지름 (월드): 키운 히트박스가 아니라 예전 크기 — 밀어내기 · 벽 판정이 예전처럼
    // 아직 Start 전이면 (막 소환된 적) 받은 콜라이더로
    float BodyRadius(CircleCollider2D fallback) => hitbox != null ? hitbox.BodyWorldRadius : fallback != null ? WorldRadius(fallback) : 0.5f;

    // 원 콜라이더의 월드 반지름
    static float WorldRadius(CircleCollider2D c)
    {
        Vector3 s = c.transform.lossyScale;
        return c.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
    }

    // 주변 적과 겹친 정도에 비례해 바깥으로 밀어내는 방향
    // 밀어내기는 두 물리 단계에 한 번 계산하고 사이 단계는 지난 값을 그대로 씀 (적마다 번갈아 — 연속량이라 눈에 같음)
    Vector3 lastPush;
    int lastPushStep = int.MinValue;

    Vector3 Separation()
    {
        if (bodyCollider == null) return Vector3.zero;

        int stepNo = Mathf.RoundToInt(Time.fixedTime / Time.fixedDeltaTime);
        if (lastPushStep == stepNo - 1 && ((stepNo + sepPhase) & 1) == 1) { lastPushStep = stepNo; return lastPush; }
        lastPush = ComputeSeparation();
        lastPushStep = stepNo;
        return lastPush;
    }

    int sepPhase => GetHashCode() & 1;

    Vector3 ComputeSeparation()
    {
        Vector2 center = bodyCollider.bounds.center;
        float myRadius = BodyRadius(bodyCollider);

        // 가장 큰 적까지 잡히도록 넉넉하게 검색 (적 층만 — 탄 · 코인 · 회복약이 칸을 차지하지 않게)
        int count = Physics2D.OverlapCircle(center, myRadius * 3f, separationFilter, nearby);

        Vector2 push = Vector2.zero;
        for (int i = 0; i < count; i++)
        {
            Collider2D other = nearby[i];
            if (other == bodyCollider || !other.CompareTag("enermy")) continue;

            CircleCollider2D otherCircle = other as CircleCollider2D;
            if (otherCircle == null) continue;

            // 콜라이더 → 적 대응표 (주변 적마다 GetComponent 를 부르지 않게)
            if (!byCollider.TryGetValue(other, out EnermyController otherEnemy) || otherEnemy == null || otherEnemy.IsDead) continue;

            Vector2 away = center - (Vector2)other.bounds.center;
            float dist = away.magnitude;
            float minDist = myRadius + otherEnemy.BodyRadius(otherCircle);
            if (dist >= minDist) continue;

            // 완전히 같은 위치면 임의 방향으로 벌림
            if (dist < 0.001f) away = Random.insideUnitCircle.normalized;
            else away /= dist;

            push += away * ((minDist - dist) / minDist);
        }

        return Vector2.ClampMagnitude(push, 1.5f);
    }

    public void TakeDamage(float damage, float knockBack, Vector3 dir)
    {
        if (isDead || !ready) return;
        damage *= GameMode.PlayerDamageMul;      // 1.1.6~ 플레이어가 주는 모든 피해 공통 하향

        if (DamageHook != null) damage = DamageHook(this, damage);
        damage = SignatureSkills.Outgoing(gameObject, damage);     // 고유 스킬 피해 배율 (위협 · 역습 · 표식 …)
        damage = WeakHit.Settle(gameObject, damage);               // 중간 보스 빈틈 몫 상한 · 기록 (모든 배율 뒤)

        // 체력 감소
        RunStats.Dealt(Mathf.Min(damage, Mathf.Max(0f, EnemyHealth)));      // 이번 판 출처별 피해 (넘친 피해는 빼고)
        float before = EnemyHealth;
        EnemyHealth -= damage;
        HealthBarFeel.Tier tier = healthBar != null ? healthBar.OnDamaged(damage, before) : HealthBarFeel.Tier.Small;
        SignatureSkills.Hit(gameObject, damage, EnemyHealth <= 0);
        DamagePopup.Show(transform, damage, spriteRenderer, false, tier);
        SkinFx.OnEnemyHit(transform.position);      // 이펙트 스킨 명중 불꽃

        transform.position += dir * knockBack * knockBackTaken;

        // 피격 애니메이션
        //animator.SetTrigger("hit");

        // 피격 색상 효과
        StartHitEffect();


        // 체력이 0 이하이면 사망
        if (EnemyHealth <= 0) Die(1);
    }

    // 보스가 쓰러질 때 화면 정리: 보상 없이 영혼이 되어 흩어짐
    public void Purge()
    {
        if (isDead) return;
        Fx.Spawn("fx_soulburst", transform.position, 2.6f * Mathf.Max(1f, transform.localScale.x / 3f), new Color(0.75f, 0.95f, 1f), 18f);
        SoulWisp.Spawn(transform.position, transform.position + Vector3.up * 3f + (Vector3)(Random.insideUnitCircle * 1.5f), new Color(0.7f, 0.95f, 1f), true);
        Die(0);
    }

    // 1.1~ 손맛: 맞으면 0.05초 하얗게 번쩍이고 (SpriteOutline 셰이더 _Flash) 그 뒤 0.05초는 예전처럼 빨갛게
    // 몸은 가로로 찌그러졌다(1.15 / 0.85) 0.08초에 돌아옴 — 그리는 동안만 크기를 바꾸고 프레임 끝에 되돌림 (다른 코드가 읽는 크기는 그대로)
    // 1.1~ 이어 맞는 중(직전에 맞은 지 WhiteGap 안)이면 흰 번쩍임 없이 빨강만 이어 감: 화염방사기처럼 0.07초마다 맞으면 흰색 · 빨강이 약 14Hz 로 깜빡였음
    //   설정 「번쩍임 효과」(GameSettings.Flashes)가 꺼져 있으면 흰 번쩍임 없이 빨강만
    const float WhiteFor = 0.05f, RedFor = 0.1f, SquashFor = 0.08f, WhiteGap = 0.25f;
    float hitAt = -9f, whiteAt = -9f;
    bool hitFxRunning;
    Vector3 squashRestore;

    // 1.1.9: 이미 돌고 있으면 코루틴을 만들기 전에 시간만 새로 (예전엔 맞을 때마다 코루틴 객체를 만들고 바로 끝내서 GC 가 쌓였음)
    void StartHitEffect()
    {
        if (Time.time - hitAt >= WhiteGap && GameSettings.Flashes) whiteAt = Time.time;
        hitAt = Time.time;
        if (hitFxRunning) return;
        StartCoroutine(HitEffect());
    }

    // 그래픽 개편 1단계-10 (2026-10-22~): 피격 빨강 → 몸을 어둡게 물들임 (계속 맞는 무리가 시뻘건 덩어리가 되어 적 시전 깜빡임 · 위험색과 섞였음)
    Color HitTint => VisualRules.Legacy ? Color.red : new Color(baseColor.r * 0.55f, baseColor.g * 0.4f, baseColor.b * 0.42f, baseColor.a);

    IEnumerator HitEffect()
    {
        hitFxRunning = true;
        bool white = false;
        while (!isDead && Time.time - hitAt < RedFor)
        {
            bool w = Time.time - whiteAt < WhiteFor;
            if (w != white) { white = w; Juice.SetFlash(spriteRenderer, w); }
            spriteRenderer.color = w ? baseColor : HitTint;
            yield return null;
        }
        if (white) Juice.SetFlash(spriteRenderer, false);

        // 죽지 않았을 때만 원래 색으로
        if (!isDead) spriteRenderer.color = baseColor;
        hitFxRunning = false;
    }

    // 1.2.5~ 켜진 적 목록 (결계 · 공간 침식이 0.2초마다 장면 전체를 찾아 새 배열을 받지 않게)
    // 켜질 때 넣고 꺼질 때 빼서 쌓이지 않음 (자리 번호를 기억해 끝 칸과 바꿔 뺌)
    public static readonly System.Collections.Generic.List<EnermyController> Live = new System.Collections.Generic.List<EnermyController>(256);
    int liveIndex = -1;

    void OnEnable()
    {
        if (liveIndex >= 0) return;
        liveIndex = Live.Count;
        Live.Add(this);
    }

    void LeaveLive()
    {
        int i = liveIndex;
        liveIndex = -1;
        if (i < 0 || i >= Live.Count || Live[i] != this) { Live.Remove(this); return; }
        int last = Live.Count - 1;
        if (i != last) { EnermyController moved = Live[last]; Live[i] = moved; moved.liveIndex = i; }
        Live.RemoveAt(last);
    }

    // 오브젝트가 꺼지면 코루틴이 멈추므로 번쩍임을 거둠
    void OnDisable()
    {
        LeaveLive();
        if (!hitFxRunning) return;
        hitFxRunning = false;
        Juice.SetFlash(spriteRenderer, false);
    }

    void LateUpdate()
    {
        // 1.2.6~ 지난 프레임의 되돌림이 아직 안 됐으면(프레임 끝 코루틴이 돌지 않는 환경) 먼저 되돌림 — 찌그러짐이 쌓여 크기가 무한대로 가지 않게
        if (squashPending) { transform.localScale = squashRestore; squashPending = false; }
        if (isDead || Time.time - hitAt >= SquashFor) return;
        float k = 1f - (Time.time - hitAt) / SquashFor;
        Vector3 s = transform.localScale;
        squashRestore = s;
        transform.localScale = new Vector3(s.x * Mathf.Lerp(1f, 1.15f, k), s.y * Mathf.Lerp(1f, 0.85f, k), s.z);
        squashPending = true;
        squashed.Add(this);
        QueueUnsquash();
    }
    bool squashPending;

    // 찌그러진 적을 한 목록에 모아 프레임 끝에 한 번에 되돌림 (적마다 프레임마다 코루틴을 만들지 않게 · 광역기로 수백 마리를 때려도 코루틴은 프레임당 하나)
    // 적이 중간에 꺼져도(풀로 돌아감) 크기는 여기서 되돌아감
    static readonly System.Collections.Generic.List<EnermyController> squashed = new System.Collections.Generic.List<EnermyController>();
    static SquashRestorer squashHost;
    static bool unsquashQueued;

    static void QueueUnsquash()
    {
        if (unsquashQueued && squashHost != null) return;
        if (squashHost == null)
            squashHost = new GameObject("SquashRestorer").AddComponent<SquashRestorer>();      // 씬과 함께 사라짐 → 다음에 다시 만듦
        unsquashQueued = true;
        squashHost.StartCoroutine(UnsquashAll());
    }

    static IEnumerator UnsquashAll()
    {
        yield return Juice.EndOfFrame;
        for (int i = 0; i < squashed.Count; i++)
            if (squashed[i] != null && squashed[i].squashPending) { squashed[i].transform.localScale = squashed[i].squashRestore; squashed[i].squashPending = false; }
        squashed.Clear();
        unsquashQueued = false;
    }
    PlayerController playerC;
    
    void Die(int a)
    {
        if (isDead) return;

        isDead = true;
        if (swarm != null) { swarm.Died(a == 1); return; }      // 1.1~ 부스러기: 경험치 · 처치 알림 · 풀로 돌려놓기는 SwarmMote (스포너 수 · 코인 · 포인트 없음)
        // 영혼 트리 '저주 전이': 걸려 있던 화상 · 독 · 출혈을 가까운 적에게 (상태 이상이 아직 붙어 있을 때)
        if (a == 1 && SpecialAbilities.SharedInstance != null) SpecialAbilities.SharedInstance.FateOnEnemyDie(this);
        KillCounts = !skipKillCount;
        try
        {
            if (a == 1) Killed?.Invoke(transform.position);
            if (a == 1) onKilled?.Invoke();
            if (a == 1) Juice.EnemyDied(transform.position, survivesContact ? 1.8f : 1f);
        }
        finally { KillCounts = true; }
        if (a == 1 && IsMidBoss) Juice.HitStop();      // 1.1~ 엘리트 · 중간 보스 처치: 0.06초 멈춤
        if (a == 1 && IsMidBoss)
        {
            // 1.2~ 영혼 계약 · 보드: 중간 보스 (20초 안 · 평타로)
            RunCounters.Both("midboss");
            MidBossMark mark = GetComponent<MidBossMark>();
            if (mark != null && Time.time - mark.BornAt <= 20f) RunCounters.Add("midboss.fast");
            if (DamageSource.Current == DamageSource.Basic) RunCounters.Add("midboss.basic");
        }
        spriteRenderer.color = baseColor;

        // 사망 연출 중에는 총알이나 플레이어와 부딪히지 않음
        if (bodyCollider != null) bodyCollider.enabled = false;

        if (a == 1)
        {
            playerC = Cache<PlayerController>.Get;
            LevelShop levelS = Cache<LevelShop>.Get;
            Level lv = Cache<Level>.Get;

            // 경험치 막대가 잠깐 꺼져 있어도 멈추지 않게
            float exp = expReward * (lv != null ? lv.bonusEXP : 1f);
            if (levelS != null) levelS.GainExp(playerC, exp);
            else playerC.nowEXP += exp;

            Point point = Cache<Point>.Get;

            // 1.0.4~: 중간 보스는 +100P (예전엔 일반 적과 같은 10P)
            if (point != null) { if (IsMidBoss) point.AddPoint(100, pointMul); else point.AddMobPoint(10, pointMul); }     // 1.2~ 일반 적만 무한 모드 처치 수 체감 (Point.MobKillMul) · 1.2.3~ pointMul: 적마다 따로 거는 배율

            // 영혼 조각: 강한 적 · 중간 보스일수록 많이 (중간 보스는 크게 알림)
            SoulShards.Add(SoulShards.ForEnemy(this), transform.position, IsMidBoss);
        }

        if (animator != null) animator.SetTrigger("death");

        StartCoroutine(Death(a));
    }

    public GameObject hp;
    [Header("힐팩")]
    public int chanceofHP = 30;
    const float DropCut = 0.5f;         // 1.4.8~ 코인 · 회복약 공통 ×0.5 (위 난이도 처리에서 곱함)
    float hpChance = -1f;               // 난이도를 반영한 실제 확률 (%), 음수면 chanceofHP 그대로
    static readonly WaitForSeconds DeathWait = new WaitForSeconds(1f);     // 처치마다 새로 만들지 않게

    IEnumerator Death(int a)
    {
        // 죽음 애니메이션 시간
        yield return DeathWait;
        EnemySpawner enemySpawner = Cache<EnemySpawner>.Get;
        enemySpawner.spawnedEnemys--;
        if (finalMob && enemySpawner.finalMobs > 0) enemySpawner.finalMobs--;
        // 총알에 죽었을 때만 코인 생성
        if (a == 1)
        {
            

            if (!skipKillCount) enemySpawner.killedEnemy++;       // 1.2~ 정화로 쓰러진 적 절반은 보스까지 처치 수에 넣지 않음
            if (HPpack.Live.Count < HPpack.Cap && Random.value * 100f < (hpChance >= 0f ? hpChance : chanceofHP)) Instantiate(hp, Hostile.ClampArena(transform.position), Quaternion.identity);     // 코인 · 회복약은 맵 안에만 (2.2.0)
            // 1.0.4~: 많이 떨어뜨리면(엘리트 등) 값이 큰 코인 몇 개로 나눔 · 풀에서 꺼내 씀 (줍는 양은 그대로)
            CoinTag.Drop(coin, transform.position, coinDrop, new Vector2(1.5f, 1.5f), true, CoinTag.EnemyPieces);
        }
        Destroy(gameObject);
    }

    // 플레이어와 닿으면 피해를 주고 살짝 튕겨 나감 (죽지 않음)
    // 플레이어가 무적이면 붙어 있다가 무적이 끝나는 순간 피해를 줌
    private void OnTriggerEnter2D(Collider2D collision)
    {
        TouchPlayer(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TouchPlayer(collision);
    }

    // 1.1.0 다시 올림: 황금 고블린은 이름표 때문에 MidBossMark 가 붙지만 중간 보스 보상(+100P · 멈춤 · 큰 조각)은 주지 않음 (SignatureSkills.IsBoss 와 같은 기준)
    // 처음 물을 때 한 번만 계산 (처치마다 3번 · 중력 규칙 0.1초마다 GetComponent 2회씩 하던 것) — 표식은 스폰 직후 붙고, Start · MarkMidBoss 에서 다시 계산
    sbyte midBoss = -1;
    public bool IsMidBoss
    {
        get
        {
            if (midBoss < 0) midBoss = (sbyte)(GetComponent<MidBossMark>() != null && GetComponent<GoldenGoblin>() == null ? 1 : 0);
            return midBoss == 1;
        }
    }
    public void MarkMidBoss() => midBoss = -1;

    void TouchPlayer(Collider2D collision)
    {
        if (swarm != null) return;      // 1.1~ 부스러기는 몸통 피해 0 (닿으면 SwarmMote 가 거리로 흡수)
        if (contactDamage <= 0f) return;    // 1.1.0 다시 올림: 황금 고블린(피해 0)에 닿으면 보호막이 사라지고 피격음 · 무적이 붙었음
        if (isDead || !collision.CompareTag("Player")) return;
        PlayerController target = collision.GetComponent<PlayerController>();
        // 넉넉한 히트박스 가장자리에 스친 것만으로는 안 맞음 · 1.4.4~ 플레이어 쪽도 몸 콜라이더 대신 몸 아래쪽 접촉 원 (위쪽은 널널하게)
        if (hitbox != null && !(target != null ? hitbox.Touching(target.ContactCenter, target.ContactR) : hitbox.Touching(collision))) return;

        if (target != null && target.TryHit(contactDamage * ContactScale))
        {
            Vector3 away = transform.position - target.transform.position;
            away.z = 0f;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitCircle;
            transform.position += away.normalized * 1.2f * knockBackTaken;
        }
    }


}

// 1.1~ 적 찌그러짐을 프레임 끝에 한 번에 되돌리는 코루틴 자리 (EnermyController.QueueUnsquash)
public class SquashRestorer : MonoBehaviour { }
