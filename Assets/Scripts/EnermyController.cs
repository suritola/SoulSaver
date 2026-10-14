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
    // 받는 피해를 바꾸는 능력 (약점 간파 · 급소 노리기 · 사냥감 표식)
    public static System.Func<EnermyController, float, float> DamageHook;

    private CircleCollider2D bodyCollider;
    // 그림 가운데에 맞춘 히트박스 (맞는 판정은 넉넉하게, 몸통 피해는 몸 안쪽만)
    private EnemyHitbox hitbox;
    // 살아 있는 적의 콜라이더 → 적 (밀어내기 계산용)
    static readonly System.Collections.Generic.Dictionary<Collider2D, EnermyController> byCollider = new System.Collections.Generic.Dictionary<Collider2D, EnermyController>();
    private static readonly Collider2D[] nearby = new Collider2D[24];

    // 난이도 배율을 이미 적용했는지 (복제돼도 두 번 곱하지 않게)
    [HideInInspector] public bool difficultyApplied;
    // 보스가 나오기 전에 생긴 적 (처치 수로 보스를 부름): 처치 수가 늘어난 만큼 보상 · 영혼 조각을 나눔
    [HideInInspector] public bool countsTowardBoss;

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
        EnemyHealthBar.Attach(this, spriteRenderer);

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

    bool Blocked(Vector3 pos)
    {
        float r = bodyCollider != null ? BodyRadius(bodyCollider) * 0.8f : 0.5f;
        Vector2 center = (Vector2)pos + (bodyCollider != null ? bodyCollider.offset * (Vector2)transform.lossyScale : Vector2.zero);
        int n = Physics2D.OverlapCircleNonAlloc(center, r, wallHits);
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

        if (DamageHook != null) damage = DamageHook(this, damage);
        damage = SignatureSkills.Outgoing(gameObject, damage);     // 고유 스킬 피해 배율 (위협 · 역습 · 표식 …)

        // 체력 감소
        RunStats.Dealt(Mathf.Min(damage, Mathf.Max(0f, EnemyHealth)));      // 이번 판 출처별 피해 (넘친 피해는 빼고)
        EnemyHealth -= damage;
        SignatureSkills.Hit(gameObject, damage, EnemyHealth <= 0);
        DamagePopup.Show(transform, damage, spriteRenderer);
        SkinFx.OnEnemyHit(transform.position);      // 이펙트 스킨 명중 불꽃

        transform.position += dir * knockBack * knockBackTaken;

        // 피격 애니메이션
        //animator.SetTrigger("hit");

        // 피격 색상 효과
        StartCoroutine(HitEffect());


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

    IEnumerator HitEffect()
    {
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(0.1f);

        // 죽지 않았을 때만 원래 색으로
        if (!isDead) spriteRenderer.color = baseColor;
    }
    PlayerController playerC;
    
    void Die(int a)
    {
        if (isDead) return;

        isDead = true;
        // 영혼 트리 '저주 전이': 걸려 있던 화상 · 독 · 출혈을 가까운 적에게 (상태 이상이 아직 붙어 있을 때)
        if (a == 1 && SpecialAbilities.SharedInstance != null) SpecialAbilities.SharedInstance.FateOnEnemyDie(this);
        if (a == 1) Killed?.Invoke(transform.position);
        if (a == 1) onKilled?.Invoke();
        if (a == 1) Juice.EnemyDied(transform.position, survivesContact ? 1.8f : 1f);
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

            if (point != null) point.AddPoint(10);

            // 영혼 조각: 강한 적 · 중간 보스일수록 많이 (중간 보스는 크게 알림)
            SoulShards.Add(SoulShards.ForEnemy(this), transform.position, GetComponent<MidBossMark>() != null);
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
            

            enemySpawner.killedEnemy++;
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

    void TouchPlayer(Collider2D collision)
    {
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
