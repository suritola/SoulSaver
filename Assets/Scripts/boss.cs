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
    // 1.2.5~ 공간 침식 동안 제자리에 섬 (casting 과 따로 · 개발자 필살기가 casting 을 꺼도 걷지 않게, SpaceErosion)
    [System.NonSerialized] public bool domainHold;

    float summonTimer;
    EnemySpawner spawner;

    public bool Enraged => EnemyHealth <= setEnemyHP * 0.5f;
    bool enragedFired;              // 1.1.9~ 「분노!」 알림은 보스마다 한 번

    // 2.2.1 보스 개편: 리치 왕 「망자의 의식」 동안 무적 · 의식이 깨진 뒤 휘청이는 동안 받는 피해 배율 (BossSkills.Reborn)
    [HideInInspector] public bool invulnerable;
    [HideInInspector] public float damageTakenMul = 1f;
    // 1.2.3~ 무한 모드 「보스는 기다리지 않는다」 (EndlessMode · 직렬화하지 않음 — 프리팹 · 복제에 섞이지 않게)
    [System.NonSerialized] public float spawnedAt;        // 등장 시각 (Time.time) — 겹쳐 등장 · 모래 폭풍 풍화의 기준 · 이어하기가 되돌림
    [System.NonSerialized] public bool spawnTimed;        // spawnedAt 을 EndlessMode 가 이미 넣었음 (아니면 Start 가 지금 시각으로)
    [System.NonSerialized] public float erosionMul = 1f;  // 모래 폭풍 풍화: 받는 피해 배율 (EndlessMode.UpdateErosion 이 1초마다 · 개발자 · 엄마 1) — damageTakenMul 은 망자의 의식이 절댓값으로 덮어써서 따로 둠
    [System.NonSerialized] public int restoreMaxHp;       // 이어하기: 저장할 때의 최대 체력 (0 = 평소대로 · 난이도 배율 뒤에 덮어씀)
    [System.NonSerialized] public float restoreHp;        // 이어하기: 저장할 때의 체력 (0 = 가득)
    [System.NonSerialized] public bool restoreDying;      // 이어하기: 쓰러지는 연출 중에 저장된 보스 (체력 1 로 다시 · 경험치 · 포인트 · 영혼 조각은 그때 이미 받아서 다시 주지 않음)
    // 기억하는 보스 (BossMemory · 직렬화하지 않음 — 프리팹 · 복제에 섞이지 않게): 기억 · 깨진 뒤 휘청이는 동안 받는 피해 배율 (damageTakenMul 은 망자의 의식이 덮어써서 따로)
    [System.NonSerialized] public BossMemory memory;
    [System.NonSerialized] public float memoryTakenMul = 1f;
    // 기억 연출: 몸 흰빛 (발동 0.6초 · 깨진 뒤 휘청 1.5초 깜빡) — SpriteOutline 의 _Flash 공유 재질 (Juice.SetFlash · 피격 빨강과 따로)
    [System.NonSerialized] public float memWhiteUntil, memWhiteBlink;
    bool memWhite;
    public void MemoryWhite(float seconds, float blink)
    {
        memWhiteUntil = Time.time + seconds;
        memWhiteBlink = blink;
    }
    bool defeatHandled;                                   // Death 가 처치 처리(OnBossDefeated)를 불렀음
    // 1.2.3~ 쓰러졌지만 처치 처리가 아직 (연출 1초 중) — 저장이 이 보스를 빠뜨리면 처치가 사라져서 따로 적음 (EndlessMode.LiveBossesText)
    public bool DefeatPending => isDead && !defeatHandled && !developer;

    // 킹 슬라임 (bossKind 2): 2.2.1~ 분열하지 않음 (예전 1 → 2 → 3마리 분열 때문에 1장 보스가 가장 세게 느껴졌음)
    // 보스바 · 결계가 쓰는 슬라임 목록과 번호는 그대로 둠 (한 마리만 들어감)
    [HideInInspector] public int slimeGen = 1;
    // 분열한 슬라임마다 고정 번호 (보스바 줄 · 이름 「킹 슬라임 n」이 끝까지 이 개체를 따라감)
    [HideInInspector] public int barSlot;
    static readonly List<bosss> slimes = new List<bosss>();
    static bool slimeBossCleared;    // 보스 처치는 한 번만
    // 난이도 배율을 이미 적용했는지
    [HideInInspector] public bool difficultyApplied;
    // 무한 모드 최종 보스 「개발자」 (1.0.2 · EndlessMode 가 Start 전에 정함): 이름 · 스킬(BossSkills.Developer)이 다르고, 결계는 「디버그 모드」 · 「엄마의 대청소」(BossUltimate.Developer)
    [HideInInspector] public bool developer;
    // 1.0.4~ 30분의 「개발자 엄마」 (developer 도 함께 켜짐): 개발자의 모든 능력치 2배 · 몸 색 (피격 번쩍임 · 스킬 예고가 끝나면 이 색으로 돌아감)
    [HideInInspector] public bool developerMom;
    [HideInInspector] public Color baseTint = Color.white;
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
    // 체력바 타격감 (위쪽 HUD 줄이 이 보스를 그릴 때 씀 · 보스마다 따로)
    public readonly HealthBarFeel BarFeel = new HealthBarFeel(HealthBarFeel.Kind.Boss);
    public static bosss Primary;      // 지금 위쪽 한 줄이 그리는 보스 (IsPrimary 가 Update 에서 세움)
    bool IsPrimary { get { foreach (bosss b in Live) if (b != null && !b.isDead) return b == this; return true; } }
    void OnEnable() { if (!Live.Contains(this)) Live.Add(this); }
    void OnDestroy()
    {
        Live.Remove(this);
        if (dyingCounted) { dyingCounted = false; Dying--; }
    }
    // 1.2.3~ 쓰러지는 연출(1초) 중인 보스 수 — 처치 처리(EndlessMode.OnBossDefeated)가 다음 보스 시각을 정하기 전에 겹쳐 등장이 끼지 않게
    public static int Dying { get; private set; }
    bool dyingCounted;

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
        if (restoreMaxHp > 0) setEnemyHP = restoreMaxHp;      // 1.2.3~ 이어하기 (두 번째 보스 ×0.5 할인 · 그때의 체력 배율이 이미 들어 있음)
        EnemyHealth = restoreHp > 0f ? Mathf.Min(restoreHp, setEnemyHP) : setEnemyHP;
        if (!spawnTimed) { spawnTimed = true; spawnedAt = Time.time; }
        BarFeel.Reset(EnemyHealth);
        // 1.2.7~ 아래 준비 중 하나라도 예외가 나면 ready 가 켜지지 않아 보스가 영원히 맞지 않았음 → 체력이 정해진 뒤에는 무조건 켬
        try { StartSetup(); }
        finally { ready = true; }
    }

    void StartSetup()
    {
        // 플레이어 찾기 (못 찾으면 Update 가 다시 찾음 — 예전엔 여기서 예외가 나 보스가 무적이 될 수 있었음)
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        player = pc != null ? pc.transform : null;

        // 컴포넌트 가져오기
        // 1.2.7~ 순서: 진행에 꼭 필요한 것(체력바 · 스킬 · 결계 · 슬라임 목록)을 먼저, 실패해도 진행이 막히지 않는 것(히트박스 맞추기 · 기억 · 인트로)을 뒤에
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        KilledEnemy = 0;
        bossbar = FindFirstObjectByType<bossbar>();
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
        EnemyHitbox.Fit(gameObject);        // 히트박스를 그림 가운데에 맞추고 넉넉하게
        memory = BossMemory.Attach(this);       // 기억하는 보스 (튜토리얼 제외 · 이미 붙어 있으면 그대로)
        // 처음 등장: 게임을 멈추고 카메라가 보스 쪽으로 이동한 뒤 대사 (보스마다 한 판에 한 번)
        if (!developer) StoryDirector.PlayBossIntro(transform, bossKind);
    }
    bossbar bossbar;
    void Update()
    {
        // 죽었으면 아무것도 하지 않음
        if (isDead) return;

        if (IsPrimary)
        {
            if (bossbar == null) bossbar = FindFirstObjectByType<bossbar>();      // 1.2.7~ 못 찾았으면 다시 (예전엔 매 프레임 예외로 아래 처리가 모두 멈췄음)
            Primary = this;
        }
        if (IsPrimary && bossbar != null)
        {
            bossbar.bossSpawn = true;
            bossbar.bossKind = developer ? (developerMom ? bossbar.DeveloperMomKind : bossbar.DeveloperKind) : bossKind;     // 1.0.4~ 엄마
            // 킹 슬라임도 개체마다 자기 체력만 (갈라질 몫을 합치지 않음 · 갈라진 뒤에는 bossbar 가 슬라임마다 따로 그림)
            bossbar.MaxHealth = setEnemyHP;
            bossbar.erosion = erosionMul;       // 1.2.3~ 이름 뒤 「풍화 +n%」
            bossbar.NowHealth = Mathf.CeilToInt(EnemyHealth);
            Primary = this;
        }

        if (player == null)
        {
            PlayerController pc = Hostile.Player;
            if (pc == null) return;
            player = pc.transform;
        }

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
        if (!isDead && player != null && !casting && !domainHold) transform.Translate(move * speed * EnermyController.GlobalSpeedMultiplier * Time.fixedDeltaTime);
    }

    public void TakeDamage(float damage, float knockBack, Vector3 dir)
    {
        if (isDead || !ready) return;
        damage *= GameMode.PlayerDamageMul;      // 1.1.6~ 플레이어가 주는 모든 피해 공통 하향
        if (invulnerable)
        {
            // 의식 중 무적: 맞은 자리에 막히는 불꽃만
            Fx.Spawn("fx_spark", transform.position + (Vector3)(Random.insideUnitCircle * 0.6f), 1f, new Color(0.7f, 0.6f, 1f), 26f);
            return;
        }
        damage *= damageTakenMul;
        damage *= erosionMul;               // 1.2.3~ 모래 폭풍 풍화 (무한 모드 · 평소 1)
        // 기억하는 보스: 기억한 공격은 방패(정면) · 결로 줄고, 기억이 깨져 휘청이는 동안은 더 아픔 (Unity 오브젝트라 ?. 대신 != null)
        if (memory != null) damage *= memory.Incoming(DamageSource.Current, dir);
        damage *= memoryTakenMul;

        // 치명타: 보스도 모든 캐릭터의 공격 · 스킬에 치명타를 맞음 (거너 평타 총알은 쏠 때 이미 굴림)
        if (!SpecialAbilities.CritRolled && SpecialAbilities.SharedInstance != null) damage = SpecialAbilities.SharedInstance.RollCrit(damage);

        damage = SignatureSkills.Outgoing(gameObject, damage);     // 고유 스킬 피해 배율 (거인 사냥꾼 · 표식 …)
        damage = WeakHit.Settle(gameObject, damage);               // 빈틈 몫 상한 · 기록 (모든 배율 뒤 · 이번 한 대가 빈틈일 때만)

        // 체력 감소
        RunStats.Dealt(Mathf.Min(damage, Mathf.Max(0f, EnemyHealth)));      // 이번 판 출처별 피해 (넘친 피해는 빼고)
        if (memory != null) memory.Tally(DamageSource.Current, Mathf.Min(damage, Mathf.Max(0f, EnemyHealth)));     // 이번 보스전 · 이 보스 출처별 (같은 값)
        float before = EnemyHealth;
        EnemyHealth -= damage;
        if (memory != null) memory.OnHealth(DamageSource.Current, before, EnemyHealth);      // 체력 값 넘음 · 깨기 누적
        // 1.1.9~ 「분노!」 (편의성 2차 12): 체력이 절반 아래로 처음 내려간 순간 한 번 — 지옥의 군주(66% · 33% 자기 배너) · 개발자 · 엄마는 제외
        if (!enragedFired && Enraged && before > setEnemyHP * 0.5f && EnemyHealth > 0f && !developer && bossKind != 1)
        {
            enragedFired = true;
            Notice.Big(Loc.T("분노!"), Loc.T("{0}의 체력이 절반 아래입니다. 이제 특수 스킬을 씁니다.").Replace("{0}", bossbar.BossName(bossKind)), NoticeIcon.Boss, 2.4f, NoticeLevel.Urgent);
            Hostile.Play("roar", 0.6f, 1.2f);
            bossbar.FlashName();
        }
        HealthBarFeel.Tier tier = BarFeel.OnDamaged(damage, before, setEnemyHP);     // HUD 바는 늘 보임
        SignatureSkills.Hit(gameObject, damage, EnemyHealth <= 0);
        if (EnemyHealth > 0) BossUltimate.OnBossHit(this, damage);       // 필살기 게이지 (센 한 방일수록 많이)
        DamagePopup.Show(transform, damage, spriteRenderer, true, tier);
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
        // 기억 흰빛: 설정 「번쩍임 효과」가 꺼져 있으면 쓰지 않음 (이펙트는 그대로) · 쓰러지면 바로 거둠
        bool white = !isDead && Time.time < memWhiteUntil && GameSettings.Flashes
            && (memWhiteBlink <= 0f || Mathf.Repeat(Time.time, memWhiteBlink) < memWhiteBlink * 0.55f);
        if (white != memWhite) { memWhite = white; Juice.SetFlash(spriteRenderer, white); }
        if (isDead) { flashing = false; return; }
        if (Time.time < flashUntil)
        {
            spriteRenderer.color = Color.red;
            flashing = true;
        }
        else if (flashing)
        {
            flashing = false;
            spriteRenderer.color = baseTint;
        }
        // 1.0.4~ 몸 색이 있는 보스(개발자 엄마): 스킬 예고 등이 흰색으로 되돌려 놓으면 다시 몸 색으로
        else if (baseTint != Color.white && spriteRenderer.color == Color.white) spriteRenderer.color = baseTint;
    }
    PlayerController playerC;

    void Die(int a)
    {
        if (isDead) return;

        isDead = true;
        if (!dyingCounted) { dyingCounted = true; Dying++; }

        bool lastOne = !IsSlime || SlimeRemaining() <= 0;
        if (lastOne && LiveCount == 0 && bossbar != null) bossbar.bossSpawn = false;      // 다른 보스가 남아 있으면 체력바는 그 보스로
        spriteRenderer.color = Color.white;
        if (a == 1 && !restoreDying)
        {
            
            playerC = Cache<PlayerController>.Get;
            LevelShop levelS = Cache<LevelShop>.Get;
            Level lv = Cache<Level>.Get;

            float exp = expReward * (lv != null ? lv.bonusEXP : 1f);
            if (levelS != null) levelS.GainExp(playerC, exp);
            else playerC.nowEXP += exp;

            Point point = Cache<Point>.Get;

            // 1.0.4~: 보스도 일반 적과 같은 10P였음 → 보스전을 끝내는 마지막 한 마리는 +300P (슬라임 분열체는 그대로 10P)
            if (point != null) point.AddPoint(lastOne ? 300 : 10);
            if (lastOne && !developer) UnlockRoutes.OnBossDown(bossKind);      // 1.2~ 시계공 대체 루트: 이야기 모드 장 보스를 결계 전에 (무한 모드 · 일일 도전은 세지 않음)

            // 영혼 조각: 보스전이 끝나는 마지막 한 마리는 크게, 슬라임 분열체는 조금
            if (lastOne) SoulShards.Add(SoulShards.ForBoss(expReward), transform.position, true);
            else SoulShards.Add(Mathf.Max(1, Mathf.RoundToInt(expReward / 30f)), transform.position, false);
        }

        if (animator != null) animator.SetTrigger("death");
        // 1.1~ 손맛: 보스전을 끝내는 마지막 한 마리는 0.35초 슬로모션 + 흰 번쩍임, 슬라임 분열체 등은 0.06초 멈춤
        if (a == 1) { if (lastOne && LiveCount == 0) Juice.BossDown(); else Juice.HitStop(); }      // 다른 보스가 남아 있으면 멈춤만

        StartCoroutine(Death(a));
    }

    public GameObject hp;
    [Header("힐팩")]
    public int chanceofHP = 30;

    IEnumerator Death(int a)
    {
        // 죽음 애니메이션 시간
        yield return new WaitForSeconds(1f);
        // 1.2.7~ 아래 처치 처리 중 예외가 나도 보스는 반드시 지움 (남으면 bosss.Dying 이 0 으로 안 돌아와 다음 보스 · 최종 국면이 영원히 멈춤)
        try { DeathAfterFall(a); }
        finally { Destroy(gameObject); }
    }

    void DeathAfterFall(int a)
    {
        EnemySpawner enemySpawner = Cache<EnemySpawner>.Get;
        // 총알에 죽었을 때만 코인 생성
        if (a == 1)
        {
            if (enemySpawner != null) enemySpawner.killedEnemy++;
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
        // 1.2.3~ 쓰러진 왕의 영토가 이 자리에서 번짐 — 처치 처리(EndlessMode.OnBossDefeated → EndlessTerrain.OnBossDefeated)를 부르기 바로 앞에서
        //   같은 호출 안에서 바로 읽음 (겹쳐 나온 둘이 1초 안에 차례로 쓰러져도 앞 보스의 종류 · 자리가 뒤 보스 것으로 덮이지 않게 · 예전엔 Die 에서 적었음)
        defeatHandled = true;
        if (allDown && GameMode.IsEndless) EndlessTerrain.NoteBossDown(developer ? -1 : bossKind, transform.position);
        // 무한 모드에서 함께 나온 다른 보스가 아직 살아 있으면: 처치 수만 세고 처치 연출 · 진화는 마지막 보스 때
        if (allDown && GameMode.IsEndless && EndlessMode.Instance != null && (LiveCount > 0 || (enemySpawner != null && !enemySpawner.bossSpawned)))     // 거의 같이 쓰러져 연출이 이미 시작됐을 때도
            EndlessMode.Instance.OnExtraBossDown();
        else if (enemySpawner != null && allDown) enemySpawner.OnBossDefeated();
    }



}
