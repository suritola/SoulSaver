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
    private static readonly Collider2D[] nearby = new Collider2D[24];
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
    // 1.1~ 영혼 부스러기 무리 (SwarmWave): 있으면 이동 · 몸통 닿기 · 죽은 뒤 처리를 부스러기가 맡음 (체력바 없음 · 한 화면 최대 수 · 보스까지 처치 수와 따로 셈)
    [HideInInspector] public SwarmMote swarm;

    // 생기자마자 대응표에 등록 (같은 물리 단계의 다른 적도 바로 알아보게), 사라지면 지움
    void Awake()
    {
        CircleCollider2D c = GetComponent<CircleCollider2D>();
        if (c != null) byCollider[c] = this;
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
                expReward = Mathf.RoundToInt(expReward * GameMode.RewardMul);
                coinDrop = GameMode.RoundRandom(coinsBefore / GameMode.KillStretch);
                // 회복약도 처치 수가 늘어난 만큼 덜 나오게 (한 판 총량을 1.8.8 수준으로)
                hpChance = chanceofHP / GameMode.KillStretch;
            }
            else
            {
                expReward = Mathf.RoundToInt(expReward * GameMode.FixedRewardMul);
                coinDrop = coinsBefore;
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
        if (move.x < 0) spriteRenderer.flipX = true;
        else if (move.x > 0) spriteRenderer.flipX = false;
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
            if (dashInterval > 0f && Mathf.Repeat(moveTime, dashInterval) < dashDuration) currentSpeed *= dashSpeedMultiplier;

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
    }

    static readonly Collider2D[] wallHits = new Collider2D[8];
    static ContactFilter2D solidOnly = new ContactFilter2D { useTriggers = false };

    bool Blocked(Vector3 pos)
    {
        float r = bodyCollider != null ? BodyRadius(bodyCollider) * 0.8f : 0.5f;
        Vector2 center = (Vector2)pos + (bodyCollider != null ? bodyCollider.offset * (Vector2)transform.lossyScale : Vector2.zero);
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
    Vector3 Separation()
    {
        if (bodyCollider == null) return Vector3.zero;

        Vector2 center = bodyCollider.bounds.center;
        float myRadius = BodyRadius(bodyCollider);

        // 가장 큰 적까지 잡히도록 넉넉하게 검색
        int count = Physics2D.OverlapCircleNonAlloc(center, myRadius * 3f, nearby);

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

    IEnumerator HitEffect()
    {
        hitFxRunning = true;
        bool white = false;
        while (!isDead && Time.time - hitAt < RedFor)
        {
            bool w = Time.time - whiteAt < WhiteFor;
            if (w != white) { white = w; Juice.SetFlash(spriteRenderer, w); }
            spriteRenderer.color = w ? baseColor : Color.red;
            yield return null;
        }
        if (white) Juice.SetFlash(spriteRenderer, false);

        // 죽지 않았을 때만 원래 색으로
        if (!isDead) spriteRenderer.color = baseColor;
        hitFxRunning = false;
    }

    // 오브젝트가 꺼지면 코루틴이 멈추므로 번쩍임을 거둠
    void OnDisable()
    {
        if (!hitFxRunning) return;
        hitFxRunning = false;
        Juice.SetFlash(spriteRenderer, false);
    }

    void LateUpdate()
    {
        if (isDead || Time.time - hitAt >= SquashFor) return;
        float k = 1f - (Time.time - hitAt) / SquashFor;
        Vector3 s = transform.localScale;
        squashRestore = s;
        transform.localScale = new Vector3(s.x * Mathf.Lerp(1f, 1.15f, k), s.y * Mathf.Lerp(1f, 0.85f, k), s.z);
        squashed.Add(this);
        QueueUnsquash();
    }

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
            if (squashed[i] != null) squashed[i].transform.localScale = squashed[i].squashRestore;
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
            if (point != null) { if (IsMidBoss) point.AddPoint(100); else point.AddMobPoint(10); }     // 1.2~ 일반 적만 무한 모드 처치 수 체감 (Point.MobKillMul)

            // 영혼 조각: 강한 적 · 중간 보스일수록 많이 (중간 보스는 크게 알림)
            SoulShards.Add(SoulShards.ForEnemy(this), transform.position, IsMidBoss);
        }

        if (animator != null) animator.SetTrigger("death");

        StartCoroutine(Death(a));
    }

    public GameObject hp;
    [Header("힐팩")]
    public int chanceofHP = 30;
    float hpChance = -1f;               // 난이도를 반영한 실제 확률 (%), 음수면 chanceofHP 그대로

    IEnumerator Death(int a)
    {
        // 죽음 애니메이션 시간
        yield return new WaitForSeconds(1f);
        EnemySpawner enemySpawner = Cache<EnemySpawner>.Get;
        enemySpawner.spawnedEnemys--;
        // 총알에 죽었을 때만 코인 생성
        if (a == 1)
        {
            

            if (!skipKillCount) enemySpawner.killedEnemy++;       // 1.2~ 정화로 쓰러진 적 절반은 보스까지 처치 수에 넣지 않음
            if (Random.value * 100f < (hpChance >= 0f ? hpChance : chanceofHP)) Instantiate(hp, Hostile.ClampArena(transform.position), Quaternion.identity);     // 코인 · 회복약은 맵 안에만 (2.2.0)
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
    public bool IsMidBoss => GetComponent<MidBossMark>() != null && GetComponent<GoldenGoblin>() == null;

    void TouchPlayer(Collider2D collision)
    {
        if (swarm != null) return;      // 1.1~ 부스러기는 몸통 피해 0 (닿으면 SwarmMote 가 거리로 흡수)
        if (contactDamage <= 0f) return;    // 1.1.0 다시 올림: 황금 고블린(피해 0)에 닿으면 보호막이 사라지고 피격음 · 무적이 붙었음
        if (isDead || !collision.CompareTag("Player")) return;
        if (hitbox != null && !hitbox.Touching(collision)) return;      // 넉넉한 히트박스 가장자리에 스친 것만으로는 안 맞음

        PlayerController target = collision.GetComponent<PlayerController>();
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
