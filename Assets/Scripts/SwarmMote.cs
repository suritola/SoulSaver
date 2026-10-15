using UnityEngine;

// 1.1~ 영혼 부스러기 (SwarmWave 가 풀에서 꺼내 씀): 체력 1 · 몸통 피해 0, 무리를 지어 거의 직선으로 화면을 가로지름
// 플레이어 무기는 모두 「enermy 태그 + 콜라이더 + EnermyController」로 적을 찾으므로 같은 부품을 달아 그대로 맞게 함
//   (총알 OnTriggerEnter2D · 근접 · 범위 공격의 Physics2D.OverlapCircle · Specials.Damage · 자동 조준 전부)
// EnermyController.swarm 이 이 부품을 가리키면: 이동 · 밀어내기 · 체력바 · 몸통 닿기 · 죽은 뒤 처리(스포너 수 · 코인 · 포인트)를 하지 않고 여기로 넘김
// 플레이어에게 닿는 것은 거리로 검사해서 흡수 (처치로 셈) · 보스까지 처치 수(EnemySpawner.killedEnemy) · 한 화면 최대 수에는 넣지 않음
public class SwarmMote : MonoBehaviour
{
    public const float Speed = 6.5f;            // 초당 칸 (1장 잡몹보다 조금 느림 · 무리가 지나가는 동안 쓸어 담을 수 있게)
    const float TurnRate = 18f;                 // 초당 이만큼(도)만 플레이어 쪽으로 휨 (거의 직선)
    const float AbsorbRadius = 0.9f;            // 플레이어와 이만큼 가까워지면 흡수

    EnermyController ec;
    SpriteRenderer sr;
    CircleCollider2D col;
    Rigidbody2D rb;
    Vector2 dir;
    float age, life, exp, phase;
    bool dead;

    // 풀에 남아 있는지 (꺼져 있거나 막 죽어서 돌아갈 차례)
    public bool Free => this == null || !gameObject.activeSelf || dead;

    static Sprite sprite;

    // 풀에 넣을 부스러기 하나 (꺼진 채로 만듦 · 처음 켤 때 EnermyController.Start 가 돎)
    public static SwarmMote Create(Transform parent)
    {
        GameObject go = new GameObject("SoulMote");
        go.tag = "enermy";
        go.transform.SetParent(parent, false);

        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = MoteSprite();

        CircleCollider2D c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.35f;                       // EnemyHitbox 가 맞는 판정만 1.25 ~ 1.6배로 키움

        // 잡몹 프리팹과 같은 키네마틱 몸 (총알 트리거가 같은 방식으로 닿게)
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        SwarmMote m = go.AddComponent<SwarmMote>();
        m.sr = r;
        m.col = c;
        m.rb = body;

        // EnermyController 는 Awake 에서 콜라이더를 등록하므로 콜라이더 · 그림 다음에 붙임
        EnermyController e = go.AddComponent<EnermyController>();
        e.swarm = m;
        e.difficultyApplied = true;             // 난이도 · 장 배율로 체력 · 보상을 바꾸지 않음 (체력 1 고정 · 경험치는 SwarmWave 가 정함)
        e.setEnemyHP = 1;
        e.contactDamage = 0f;
        e.expReward = 0;
        e.coinDrop = 0;
        e.chanceofHP = 0;
        e.speed = 0f;
        e.separationSpeed = 0f;
        e.knockBackTaken = 0f;
        e.baseColor = Color.white;
        m.ec = e;

        go.SetActive(false);
        return m;
    }

    // 무리 안 한 자리에서 출발
    public void Launch(Vector3 pos, Vector2 heading, float lifetime, float expGain, float wobblePhase, int sortingLayer, int sortingOrder)
    {
        transform.position = pos;
        transform.localScale = Vector3.one;
        dir = heading.sqrMagnitude > 0.0001f ? heading.normalized : Vector2.right;
        life = lifetime;
        exp = expGain;
        phase = wobblePhase;
        age = 0f;
        dead = false;
        Face();

        sr.enabled = true;
        sr.color = Color.white;
        sr.sortingLayerID = sortingLayer;
        sr.sortingOrder = sortingOrder;
        col.enabled = true;
        rb.simulated = true;                    // 보스 처치 연출(EnemySpawner.Quiet)이 껐을 수 있음
        ec.enabled = true;
        gameObject.SetActive(true);
        ec.ReviveForPool();
    }

    void Update()
    {
        if (dead || !ec.enabled) return;        // 보스 처치 연출로 끌려가는 동안은 그쪽이 움직임
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 수명은 기본 속도로 화면을 지나는 시간이라, 적 이동 배율(시간 왜곡 등)만큼 늘려 셈 (느려져도 화면 한가운데서 사라지지 않게)
        age += dt * Mathf.Max(0f, EnermyController.GlobalSpeedMultiplier);
        if (age >= life) { Vanish(); return; }

        PlayerController p = Hostile.Player;
        Vector2 at = transform.position;
        if (p != null && !p.IsDying)
        {
            Vector2 to = (Vector2)p.transform.position - at;
            // 닿으면 흡수 (몸통 피해 없음 · 처치로 셈)
            if (to.sqrMagnitude <= AbsorbRadius * AbsorbRadius) { ec.KillBySkill(); return; }
            // 앞쪽에 있을 때만 아주 조금 휨 (지나친 뒤에는 그대로 빠져나감)
            if (Vector2.Dot(to, dir) > 0f)
                dir = Vector3.RotateTowards(dir, to.normalized, TurnRate * Mathf.Deg2Rad * dt, 0f);
        }

        // 무리가 뱀처럼 살짝 흔들리며 날아감
        Vector2 side = new Vector2(-dir.y, dir.x);
        float sway = Mathf.Cos(age * 5f + phase) * 0.9f;
        transform.position = at + (dir * Speed * EnermyController.GlobalSpeedMultiplier + side * sway) * dt;
        Face();
    }

    // 꼬리가 날아가는 반대쪽을 보게 (그림의 꼬리는 위쪽)
    void Face() => transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg - 90f);

    // EnermyController.Die 가 부름 (killed: 무기 · 흡수로 처치, 아니면 보스 처치 정리 Purge)
    public void Died(bool killed)
    {
        if (dead) return;
        dead = true;
        Vector3 pos = transform.position;
        sr.enabled = false;
        col.enabled = false;
        if (!killed) return;

        // 잡몹 처치와 같은 알림 (흡혈 · 고유 스킬 · 필살기 충전 · 이번 판 처치 수 · 업적), 보스까지 처치 수 · 코인 · 포인트 · 영혼 조각은 없음
        if (SpecialAbilities.SharedInstance != null) SpecialAbilities.SharedInstance.FateOnEnemyDie(ec);
        EnermyController.Killed?.Invoke(pos);
        Juice.EnemyDied(pos, 0.5f);

        PlayerController pc = Cache<PlayerController>.Get;
        if (pc == null || exp <= 0f) return;
        LevelShop shop = Cache<LevelShop>.Get;
        Level lv = Cache<Level>.Get;
        float gain = exp * (lv != null ? lv.bonusEXP : 1f);
        if (shop != null) shop.GainExp(pc, gain);
        else pc.nowEXP += gain;
    }

    // 맞지 않고 화면을 다 지나감: 보상 없이 풀로
    void Vanish()
    {
        dead = true;
        sr.enabled = false;
        col.enabled = false;
    }

    // 죽은 프레임 끝에 풀로 돌려놓음 (맞은 그 순간에 끄면 총알 · 범위 공격이 아직 이 적을 쓰고 있을 수 있음)
    void LateUpdate()
    {
        if (dead) Recycle();
    }

    void Recycle()
    {
        ec.PoolDead();
        // 걸려 있던 화상 · 표식 같은 부품 · 자식은 떼어 냄 (다음에 꺼낼 때 깨끗하게)
        foreach (Component c in GetComponents<Component>())
            if (!(c is Transform || c is SpriteRenderer || c is CircleCollider2D || c is Rigidbody2D || c is SwarmMote || c is EnermyController || c is EnemyHitbox))
                Destroy(c);
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        gameObject.SetActive(false);
    }

    // 코드로 그린 작은 영혼 불꽃 (12×12 · 둥근 머리 + 위로 가늘어지는 꼬리)
    static Sprite MoteSprite()
    {
        if (sprite != null) return sprite;
        const int S = 12;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        Color core = new Color(1f, 1f, 1f, 1f);
        Color mid = new Color(0.68f, 0.95f, 1f, 1f);
        Color rim = new Color(0.42f, 0.72f, 1f, 0.9f);
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - 5.5f, dy = y - 4f;
                float r = dy > 0f ? Mathf.Sqrt(dx * dx * (1f + dy * 0.35f) + dy * dy * 0.38f) : Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, r < 1.5f ? core : r < 2.9f ? mid : r < 4.1f ? rim : clear);
            }
        tex.Apply();
        sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 4.5f / S), 12f);
        sprite.name = "SoulMote";
        return sprite;
    }
}
