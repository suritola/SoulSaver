using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bosss : MonoBehaviour
{
    public float speed = 0.2f;
    public int setEnemyHP = 300;
    public float EnemyHealth;


    private Transform player;
    private Vector3 move;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    public int KilledEnemy;

    public GameObject coin;

    public int hitCount = 0;

    [Header("부하 소환")]
    // 이만큼 맞을 때마다 부하를 추가로 소환
    public int hitsPerSummon = 15;
    public int summonOnHitCount = 2;
    // 일정 시간마다 소환 (체력이 절반 아래면 더 자주, 더 많이)
    public float summonInterval = 5f;
    public int summonCount = 3;
    public float enragedSummonInterval = 3f;
    public int enragedSummonCount = 4;

    [Header("보상 / 피해")]
    public float contactDamage = 25f;
    public int expReward = 200;
    public int coinDrop = 50;

    [Header("넉백 저항")]
    // 총알 넉백을 이 비율만큼만 받음
    public float knockBackTaken = 0.25f;

    [Header("스킬")]
    public int bossKind = 0;            // 0 = 리치 왕, 1 = 지옥의 군주
    // 스킬 시전 중에는 걸어서 움직이지 않음
    [HideInInspector] public bool casting;

    float summonTimer;
    EnemySpawner spawner;

    public bool Enraged => EnemyHealth <= setEnemyHP * 0.5f;

    // 2.2.1 보스 개편: 리치 왕 「망자의 의식」 동안 무적 · 의식이 깨진 뒤 휘청이는 동안 받는 피해 배율 (BossSkills.Reborn)
    [HideInInspector] public bool invulnerable;
    [HideInInspector] public float damageTakenMul = 1f;

    // 킹 슬라임 (bossKind 2): 2.2.1~ 분열하지 않음 (예전 1 → 2 → 3마리 분열 때문에 1장 보스가 가장 세게 느껴졌음)
    // 보스바 · 결계가 쓰는 슬라임 목록과 번호는 그대로 둠 (한 마리만 들어감)
    [HideInInspector] public int slimeGen = 1;
    // 분열한 슬라임마다 고정 번호 (보스바 줄 · 이름 「킹 슬라임 n」이 끝까지 이 개체를 따라감)
    [HideInInspector] public int barSlot;
    static readonly List<bosss> slimes = new List<bosss>();
    static bool slimeBossCleared;    // 보스 처치는 한 번만
    // 난이도 배율을 이미 적용했는지
    [HideInInspector] public bool difficultyApplied;
    // 무한 모드 최종 보스 「개발자」 (1.0.2 · EndlessMode 가 Start 전에 정함): 이름 · 스킬(BossSkills.Developer)만 다르고 결계는 쓰지 않음
    [HideInInspector] public bool developer;
    bool IsSlime => bossKind == 2;

    // 남은 체력 합계
    static int SlimeRemaining()
    {
        int sum = 0;
        foreach (bosss s in slimes)
            if (s != null && !s.isDead) sum += Mathf.CeilToInt(Mathf.Max(0f, s.EnemyHealth));
        return sum;
    }

    // 살아 있는 킹 슬라임 (위쪽 보스 체력바가 분열한 슬라임마다 따로 그림)
    public static void CollectLiveSlimes(List<bosss> into)
    {
        into.Clear();
        foreach (bosss s in slimes) if (s != null && !s.isDead) into.Add(s);
        into.Sort((a, b) => a.barSlot.CompareTo(b.barSlot));
    }

    public bool IsDead => isDead;

    // 살아 있는 보스 (1.0.2: 무한 모드 후반엔 여러 보스가 함께 나옴) · 맨 앞 보스가 위쪽 보스 체력바 첫 줄을 씀
    public static readonly List<bosss> Live = new List<bosss>();
    public static int LiveCount { get { Live.RemoveAll(b => b == null || b.isDead); return Live.Count; } }
    public static bool KindAlive(int kind) { foreach (bosss b in Live) if (b != null && !b.isDead && b.bossKind == kind) return true; return false; }
    bool IsPrimary { get { foreach (bosss b in Live) if (b != null && !b.isDead) return b == this; return true; } }
    void OnEnable() { if (!Live.Contains(this)) Live.Add(this); }
    void OnDestroy() => Live.Remove(this);

    void Summon(int count)
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null) spawner.SummonMinions(transform.position, count);
    }

    // 생긴 프레임에 맞아도 (분열한 슬라임 등) 색 바꾸기 · 피해 숫자가 깨지지 않게 미리
    void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();
    // Start 가 끝나 체력이 정해졌는지 (그 전에 맞으면 체력 0 에서 깎여 바로 죽을 수 있음)
    bool ready;

    void Start()
    {
        SpriteOutline.Enemy(gameObject);
        if (!difficultyApplied)
        {
            difficultyApplied = true;
            // 2.1.1~: 장 순서가 바뀌어도 그 자리(몇 번째 장)의 원래 세기로 (Chapters)
            int chapter = Chapters.CurrentStage;
            float hpMul = GameMode.BossHpMul * Chapters.BossHpMul(chapter), rw = Chapters.RewardMul(chapter);
            setEnemyHP = Mathf.Max(1, Mathf.RoundToInt(setEnemyHP * hpMul));
            // 보스는 처치 수와 상관없이 정해진 수만 나오므로 고정 배율
            expReward = Mathf.RoundToInt(expReward * GameMode.FixedRewardMul * rw);
            coinDrop = Mathf.RoundToInt(coinDrop * GameMode.FixedRewardMul * rw);
        }
        EnemyHealth = setEnemyHP;

        // 플레이어 찾기
        player = FindFirstObjectByType<PlayerController>().transform;

        // 컴포넌트 가져오기
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        EnemyHitbox.Fit(gameObject);        // 히트박스를 그림 가운데에 맞추고 넉넉하게
        KilledEnemy = 0;
        bossbar = FindFirstObjectByType<bossbar>();
        // 처음 등장: 게임을 멈추고 카메라가 보스 쪽으로 이동한 뒤 대사 (보스마다 한 판에 한 번)
        if (!developer) StoryDirector.PlayBossIntro(transform, bossKind);
        BossSkills skills = GetComponent<BossSkills>();
        if (skills == null) skills = gameObject.AddComponent<BossSkills>();
        skills.kind = bossKind;
        if (GetComponent<BossUltimate>() == null) gameObject.AddComponent<BossUltimate>();     // 필살기 "결계"
        if (IsSlime)
        {
            slimes.Clear();
            slimeBossCleared = false;
            slimes.Add(this);
        }
        ready = true;
    }
    bossbar bossbar;
    void Update()
    {
        // 죽었으면 아무것도 하지 않음
        if (isDead) return;

        if (IsPrimary)
        {
            bossbar.bossSpawn = true;
            bossbar.bossKind = developer ? bossbar.DeveloperKind : bossKind;
            // 킹 슬라임도 개체마다 자기 체력만 (갈라질 몫을 합치지 않음 · 갈라진 뒤에는 bossbar 가 슬라임마다 따로 그림)
            bossbar.MaxHealth = setEnemyHP;
            bossbar.NowHealth = Mathf.CeilToInt(EnemyHealth);
        }

        if (player == null) return;

        // 주기적으로 부하 소환 (필살기 결계 중에는 쉼)
        if (!BossUltimate.Active) summonTimer += Time.deltaTime;
        if (summonTimer >= (Enraged ? enragedSummonInterval : summonInterval))
        {
            summonTimer = 0f;
            Summon(Enraged ? enragedSummonCount : summonCount);
        }

        // 적 → 플레이어 방향 계산
        move = (EnermyController.Decoy != null ? EnermyController.Decoy.position : player.position) - transform.position;

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
        if (!isDead && player != null && !casting) transform.Translate(move * speed * EnermyController.GlobalSpeedMultiplier * Time.fixedDeltaTime);
    }

    public void TakeDamage(float damage, float knockBack, Vector3 dir)
    {
        if (isDead || !ready) return;
        if (invulnerable)
        {
            // 의식 중 무적: 맞은 자리에 막히는 불꽃만
            Fx.Spawn("fx_spark", transform.position + (Vector3)(Random.insideUnitCircle * 0.6f), 1f, new Color(0.7f, 0.6f, 1f), 26f);
            return;
        }
        damage *= damageTakenMul;

        // 치명타: 보스도 모든 캐릭터의 공격 · 스킬에 치명타를 맞음 (거너 평타 총알은 쏠 때 이미 굴림)
        if (!SpecialAbilities.CritRolled && SpecialAbilities.SharedInstance != null) damage = SpecialAbilities.SharedInstance.RollCrit(damage);

        damage = SignatureSkills.Outgoing(gameObject, damage);     // 고유 스킬 피해 배율 (거인 사냥꾼 · 표식 …)

        // 체력 감소
        RunStats.Dealt(Mathf.Min(damage, Mathf.Max(0f, EnemyHealth)));      // 이번 판 출처별 피해 (넘친 피해는 빼고)
        EnemyHealth -= damage;
        SignatureSkills.Hit(gameObject, damage, EnemyHealth <= 0);
        if (EnemyHealth > 0) BossUltimate.OnBossHit(this, damage);       // 필살기 게이지 (센 한 방일수록 많이)
        DamagePopup.Show(transform, damage, spriteRenderer, true);
        SkinFx.OnEnemyHit(transform.position);      // 이펙트 스킨 명중 불꽃

        transform.position += dir * knockBack * knockBackTaken;

        // 피격 애니메이션
        //animator.SetTrigger("hit");

        // 피격 색상 효과
        flashUntil = Time.time + 0.1f;


        hitCount++;
        if (hitCount >= hitsPerSummon)
        {
            Summon(summonOnHitCount);
            hitCount = 0;
        }

        // 체력이 0 이하이면 사망
        if (EnemyHealth <= 0) Die(1);
    }

    // 피격 번쩍임: 스킬 예고(BossSkills.Windup 등)가 매 프레임 몸 색을 바꿔도 덮이지 않게 맨 마지막(LateUpdate)에 칠함
    // (예전엔 맞은 순간 한 번만 빨갛게 칠해서, 스킬을 준비하던 슬라임은 맞아도 번쩍이지 않았음)
    float flashUntil;
    bool flashing;

    void LateUpdate()
    {
        if (isDead) { flashing = false; return; }
        if (Time.time < flashUntil)
        {
            spriteRenderer.color = Color.red;
            flashing = true;
        }
        else if (flashing)
        {
            flashing = false;
            spriteRenderer.color = Color.white;
        }
    }
    PlayerController playerC;

    void Die(int a)
    {
        if (isDead) return;

        isDead = true;

        bool lastOne = !IsSlime || SlimeRemaining() <= 0;
        if (lastOne && LiveCount == 0) bossbar.bossSpawn = false;      // 다른 보스가 남아 있으면 체력바는 그 보스로
        spriteRenderer.color = Color.white;
        if (a == 1)
        {
            
            playerC = Cache<PlayerController>.Get;
            LevelShop levelS = Cache<LevelShop>.Get;
            Level lv = Cache<Level>.Get;

            float exp = expReward * (lv != null ? lv.bonusEXP : 1f);
            if (levelS != null) levelS.GainExp(playerC, exp);
            else playerC.nowEXP += exp;

            Point point = Cache<Point>.Get;

            if (point != null) point.AddPoint(10);

            // 영혼 조각: 보스전이 끝나는 마지막 한 마리는 크게, 슬라임 분열체는 조금
            if (lastOne) SoulShards.Add(SoulShards.ForBoss(expReward), transform.position, true);
            else SoulShards.Add(Mathf.Max(1, Mathf.RoundToInt(expReward / 30f)), transform.position, false);
        }

        if (animator != null) animator.SetTrigger("death");

        StartCoroutine(Death(a));
    }

    public GameObject hp;
    [Header("힐팩")]
    public int chanceofHP = 30;

    IEnumerator Death(int a)
    {
        // 죽음 애니메이션 시간
        yield return new WaitForSeconds(1f);
        EnemySpawner enemySpawner = Cache<EnemySpawner>.Get;
        // 총알에 죽었을 때만 코인 생성
        if (a == 1)
        {


            enemySpawner.killedEnemy++;
            if (Random.Range(0, 100) < chanceofHP) Instantiate(hp, Hostile.ClampArena(transform.position), Quaternion.identity);     // 코인 · 회복약은 맵 안에만 (2.2.0)
            // 1.0.4~: 한 번에 수십 ~ 수백 개를 따로 만들던 것을 값이 큰 코인 최대 40개로 나눔 (줍는 양은 그대로)
            CoinTag.Drop(coin, transform.position, coinDrop, new Vector2(5f, 5f), false, CoinTag.BossPieces);
        }

        // 스테이지 진행 (신전 문 열기 등) - 슬라임은 모두 쓰러졌을 때만
        bool allDown = !IsSlime || SlimeRemaining() <= 0;
        if (IsSlime)
        {
            slimes.Remove(this);
            if (allDown) { if (slimeBossCleared) allDown = false; else slimeBossCleared = true; }
        }
        // 무한 모드에서 함께 나온 다른 보스가 아직 살아 있으면: 처치 수만 세고 처치 연출 · 진화는 마지막 보스 때
        if (allDown && GameMode.IsEndless && EndlessMode.Instance != null && (LiveCount > 0 || (enemySpawner != null && !enemySpawner.bossSpawned)))     // 거의 같이 쓰러져 연출이 이미 시작됐을 때도
            EndlessMode.Instance.OnExtraBossDown();
        else if (enemySpawner != null && allDown) enemySpawner.OnBossDefeated();

        Destroy(gameObject);
    }



}
