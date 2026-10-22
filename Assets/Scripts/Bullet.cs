using UnityEngine;

public class Bullet : MonoBehaviour
{
    string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    // 날아가는 도중 피해 출처를 바꿈 (회전 표창: 평타 표창이 돌아올 때는 고유 스킬 피해)
    //   출처가 바뀐 탄(돌아오는 표창 등)은 빈틈 후보가 아님
    public void SetSource(string source) { dmgSource = source; aimMul = 0f; }

    // 빈틈 (WeakSpot): 조준 범위 안에서 커서 쪽으로 쏜 탄만 배율을 받음 (0 = 후보 아님 · CreateBullet 이 채움)
    [System.NonSerialized] public float aimMul;
    [System.NonSerialized] public float aimUntil;       // 0 이 아니면 이 시각까지만 후보 (낫: 가는 길만)
    [System.NonSerialized] public bool perfect;         // 궁수 「완벽!」 화살 (빈틈 ×2.1)
    // 이번 명중의 지난 자리 · 나는 방향 (Sweep 이 Hit 직전에 채움 · 트리거 길은 지금 자리 − 방향)
    Vector2 hitFrom, hitWay;
    public float speed = 10f;

    // 일반 총알은 1
    // 스킬 총알은 500
    public float damage = 1f;

    //관통 개수
    public int pene = 1;

    public float blood = 0f;

    // 스킬로 쏜 총알은 스킬 게이지를 채우지 않음
    public bool isSkill = false;

    // 적을 맞혔을 때 스킬 게이지가 차는 양 (멀티샷이면 발당 비율만큼)
    public float skillCharge = 1f;

    // 맞은 적을 밀어내는 거리 (쏠 때 플레이어가 정해 줌)
    public float knockBack = 0.3f;

    PlayerController playerC;

    private Vector2 dir;

    public Vector2 Direction => dir;
    // 적을 맞혔을 때 추가 효과 (연쇄 번개, 폭발 등)
    public System.Action<Bullet, Collider2D> onHitEnemy;
    // 채워 두면 같은 적은 한 번만 맞힘 (부메랑 낫: 돌아올 때 비워서 한 번 더)
    public System.Collections.Generic.HashSet<int> hitOnce;
    // 채워 두면 벽에 닿아도 사라지지 않고 이걸 부름 (부메랑 낫: 벽에서 되돌아옴)
    public System.Action onHitWall;
    // 채워 두면 관통 수가 다했을 때 이걸 부르고, true 면 사라지지 않음 (회전 표창: 그 자리에서 꺾여 돌아옴)
    public System.Func<bool> onSpent;
    public float lifetime = 2f;
    // 치명타 총알 (맞힌 피해 숫자를 크게 · 금색으로)
    public bool isCrit;
    // 쏠 때 이미 치명타를 굴린 총알 (거너 평타). 아니면 맞힐 때 피해 훅이 굴림 (스킬 · 필살기 · 다른 캐릭터)
    public bool critRolled;

    public Vector2 Dir
    {
        set
        {
            dir = value.normalized;

            // 총알이 날아가는 방향을 바라보도록 회전
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // 그림은 돌리지 않고 나는 방향만 바꿈 (도는 표창이 매 프레임 방향을 틀 때 · 밀어내는 방향도 이걸로)
    public void Steer(Vector2 d) { if (d.sqrMagnitude > 0.0001f) dir = d.normalized; }

    // 관통 수를 새로 채움 (회전 표창이 돌아올 때)
    public void ResetPene(int n) { pene = n; remainPene = n; }

    // 이번 프레임 스윕을 여기서 멈춤 (방향을 꺾었으니 같은 프레임 남은 길의 적은 맞히지 않게)
    public void Halt() => halt = true;
    bool halt;

    private int remainPene;

    void Awake()
    {
        // 플레이어 탄 전용 층: 스윕 검사가 다른 플레이어 탄을 건너뛰게 (충돌 행렬은 모든 층과 그대로 닿음)
        if (shotLayer == -2) shotLayer = LayerMask.NameToLayer("PlayerShot");
        if (shotLayer >= 0) gameObject.layer = shotLayer;
    }

    void Start()
    {
        if (!begun) Begin();
    }

    // 쏜 쪽이 값을 다 넣은 뒤 첫 갱신 전에 한 번 (새로 만든 탄은 Start, 다시 쓰는 탄은 첫 Update)
    //   예전 Destroy(gameObject, lifetime) 대신 남은 수명을 직접 셈 (다 되면 풀로 돌려보냄)
    void Begin()
    {
        begun = true;
        remainPene = pene;
        playerC = Hostile.Player;                  // 총알마다 씬 전체를 뒤지지 않게 (캐시)
        mainCamera = Camera.main;
        lifeLeft = lifetime;
        lastPos = transform.position;
        if (TryGetComponent(out BulletGlow g)) g.FitTracerHit();      // 예광탄 판정을 첫 검사 전에 보이는 몸통으로
        RefreshSweepShape();
    }

    // =========================
    // 풀링 (최적화 8): 평타 탄을 지우지 않고 꺼 두었다가 다시 씀
    // =========================
    // 꺼낼 때 프리팹에서 처음 만든 값으로 모두 되돌리고(Revive), 돌려보낼 때 쏜 쪽이 붙인 부속 컴포넌트 · 자식을 지움(Release)
    // 돌려보낸 그 프레임에는 다시 꺼내지 않음 (지운 컴포넌트 · 자식은 프레임 끝에야 실제로 사라지므로)
    // 밖에서 Destroy(gameObject) 해도 그냥 풀에 안 들어갈 뿐 (안전)
    const int PoolMax = 256;
    static readonly System.Collections.Generic.Queue<Bullet> pool = new System.Collections.Generic.Queue<Bullet>();
    static readonly System.Collections.Generic.List<Component> compBuf = new System.Collections.Generic.List<Component>(16);
    static GameObject poolPrefab;
    static bool snapped;
    // 프리팹 값 (처음 만든 탄에서 읽음)
    static float pSpeed, pDamage, pBlood, pSkillCharge, pKnockBack, pLifetime;
    static int pPene, pLayer, pSortLayer, pSortOrder;
    static bool pIsSkill, pSrEnabled, pFlipX, pFlipY, pBoxEnabled, pBoxTrigger;
    static Vector3 pScale;
    static Sprite pSprite;
    static Color pColor;
    static Material pMat;
    static SpriteDrawMode pDrawMode;
    static Vector2 pBoxSize, pBoxOffset;
    static RigidbodyType2D pBodyType;
    static string pTag;

    bool begun, pooled, inPool;
    int reviveFrame = -1, releaseFrame = -1;
    float lifeLeft;
    SpriteRenderer ownSr;
    BoxCollider2D ownBox;
    Rigidbody2D ownBody;
    BulletGlow ownGlow;
    // 다시 쓸 때마다 늘어남 (프레임을 넘겨 탄을 붙잡는 쪽이 같은 탄인지 비교용)
    [System.NonSerialized] public int generation;

    // 평타 탄 하나 (풀에 있으면 다시 씀, 없으면 프리팹으로 새로)
    public static Bullet Spawn(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return null;
        if (prefab != poolPrefab)
        {
            // 프리팹이 바뀌면 꺼 둔 옛 탄은 지움 (풀에서만 빼면 꺼진 채로 장면에 남음)
            while (pool.Count > 0) { Bullet old = pool.Dequeue(); if (old != null) Destroy(old.gameObject); }
            poolPrefab = prefab; snapped = false;
        }
        while (pool.Count > 0)
        {
            Bullet b = pool.Peek();
            if (b == null || !b.inPool) { pool.Dequeue(); continue; }     // 장면이 바뀌어 지워진 것은 건너뜀
            if (b.releaseFrame >= Time.frameCount) break;               // 이번 프레임에 돌아온 것은 다음 프레임부터
            pool.Dequeue();
            b.Revive(pos);
            return b;
        }
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        Bullet nb = go.GetComponent<Bullet>();
        if (nb == null) return null;
        nb.Own();
        if (!snapped) nb.Snap();
        nb.pooled = true;
        return nb;
    }

    void Own()
    {
        ownSr = GetComponent<SpriteRenderer>();
        ownBox = GetComponent<BoxCollider2D>();
        ownBody = GetComponent<Rigidbody2D>();
    }

    void Snap()
    {
        snapped = true;
        pSpeed = speed; pDamage = damage; pBlood = blood; pSkillCharge = skillCharge; pKnockBack = knockBack; pLifetime = lifetime;
        pPene = pene; pIsSkill = isSkill;
        pLayer = gameObject.layer;
        pTag = gameObject.tag;
        pScale = transform.localScale;
        if (ownSr != null)
        {
            pSprite = ownSr.sprite; pColor = ownSr.color; pMat = ownSr.sharedMaterial; pSrEnabled = ownSr.enabled;
            pSortLayer = ownSr.sortingLayerID; pSortOrder = ownSr.sortingOrder; pFlipX = ownSr.flipX; pFlipY = ownSr.flipY;
            pDrawMode = ownSr.drawMode;
        }
        if (ownBox != null) { pBoxSize = ownBox.size; pBoxOffset = ownBox.offset; pBoxEnabled = ownBox.enabled; pBoxTrigger = ownBox.isTrigger; }
        if (ownBody != null) pBodyType = ownBody.bodyType;
    }

    // 꺼낼 때: 프리팹에서 막 만든 탄과 같게
    void Revive(Vector3 pos)
    {
        inPool = false;
        begun = false;
        gone = halt = false;
        reviveFrame = Time.frameCount;
        dmgSource = DamageSource.Current;
        speed = pSpeed; damage = pDamage; blood = pBlood; skillCharge = pSkillCharge; knockBack = pKnockBack; lifetime = pLifetime;
        pene = pPene; remainPene = 0; isSkill = pIsSkill;
        isCrit = critRolled = false;
        aimMul = aimUntil = 0f;
        perfect = false;
        dir = Vector2.zero;
        onHitEnemy = null; onHitWall = null; onSpent = null; hitOnce = null;
        touchCount = 0;
        sweepCol = null;
        lifeLeft = pLifetime;
        Transform tr = transform;
        if (tr.parent != null) tr.SetParent(null, false);       // 지난 삶에 어딘가에 붙었던 탄이면 떼어 냄 (새로 만든 탄은 부모 없음)
        tr.SetPositionAndRotation(pos, Quaternion.identity);
        tr.localScale = pScale;
        lastPos = pos;
        if (gameObject.layer != pLayer) gameObject.layer = pLayer;
        if (pTag != null && !gameObject.CompareTag(pTag)) gameObject.tag = pTag;
        if (ownSr != null)
        {
            ownSr.sprite = pSprite; ownSr.color = pColor; ownSr.sharedMaterial = pMat; ownSr.enabled = pSrEnabled;
            ownSr.sortingLayerID = pSortLayer; ownSr.sortingOrder = pSortOrder; ownSr.flipX = pFlipX; ownSr.flipY = pFlipY;
            if (ownSr.drawMode != pDrawMode) ownSr.drawMode = pDrawMode;
        }
        if (ownBox != null) { ownBox.size = pBoxSize; ownBox.offset = pBoxOffset; ownBox.isTrigger = pBoxTrigger; ownBox.enabled = pBoxEnabled; }
        if (ownGlow != null) ownGlow.Rearm();
        gameObject.SetActive(true);
        if (ownBody != null)
        {
            if (ownBody.bodyType != pBodyType) ownBody.bodyType = pBodyType;
            ownBody.linearVelocity = Vector2.zero;
            ownBody.angularVelocity = 0f;
        }
    }

    // 사라질 때 (명중 · 벽 · 수명 끝): 풀로 돌려보냄 (풀에서 꺼낸 탄이 아니거나 풀이 가득이면 지움)
    public void Release()
    {
        if (this == null) return;
        gone = true;
        if (inPool) return;
        if (!pooled || poolPrefab == null || pool.Count >= PoolMax || !gameObject.activeSelf) { Destroy(gameObject); return; }
        inPool = true;
        releaseFrame = Time.frameCount;
        generation++;
        if (ownGlow == null) TryGetComponent(out ownGlow);
        if (ownGlow != null) ownGlow.Recycle();      // 꾸밈 자식은 지우지 않고 꺼 둠
        // 쏜 쪽이 붙인 부속 컴포넌트(유도 · 낫 · 회전 표창 · 프레임 넘김 · 원 판정 등)는 지움 — 지울 때의 정리(OnDestroy)도 예전처럼 돎
        GetComponents(compBuf);
        for (int i = 0; i < compBuf.Count; i++)
        {
            Component c = compBuf[i];
            if (c == null || c is Transform || c == this || c == ownSr || c == ownBox || c == ownBody || c == ownGlow) continue;
            Destroy(c);
        }
        compBuf.Clear();
        // 쏜 쪽이 붙인 자식(낫 날 · 빛 등)도 지움
        Transform tr = transform;
        for (int i = tr.childCount - 1; i >= 0; i--)
        {
            Transform ch = tr.GetChild(i);
            if (ownGlow != null && ownGlow.Owns(ch)) continue;
            Destroy(ch.gameObject);
        }
        onHitEnemy = null; onHitWall = null; onSpent = null; hitOnce = null;     // 붙잡은 클로저 · 대상 놓기
        sweepCol = null;
        gameObject.SetActive(false);
        pool.Enqueue(this);
    }

    // 쏜 쪽이 BulletGlow 를 붙인 뒤 (CreateBullet) — 다음에 다시 쓸 때 같이 되돌림
    public BulletGlow Glow
    {
        get { if (ownGlow == null) TryGetComponent(out ownGlow); return ownGlow; }
    }


    // 1.0.4~ 개발자(무한 모드 최종 보스)가 거너 조준 사격을 쓰는 동안 플레이어 탄이 느려짐 (PlayerController.HostileTimeSlow)
    // 겹쳐 써도 곱해 쌓이지 않고 끝나는 때만 늘어남
    public static float SlowUntil;
    public static float SlowMul = 1f;

    void Update()
    {
        // 다시 쓰는 탄: 새로 만든 오브젝트처럼 켜진 그 프레임에는 움직이지 않음
        if (Time.frameCount == reviveFrame) return;
        if (!begun) Begin();
        float slow = Time.time < SlowUntil ? SlowMul : 1f;
        transform.position += (Vector3)dir * speed * slow * Time.deltaTime;
        Sweep();
        if (gone || inPool) return;
        lifeLeft -= Time.deltaTime;
        if (lifeLeft <= 0f) Release();
    }

    // =========================
    // 스윕 검사 (1.1.2 뒤: 판정을 보이는 크기로 줄여서)
    // =========================
    // 총알은 Kinematic · 이산 충돌이라 판정이 짧으면 프레임 사이에 적을 뚫고 지나감 (거너 예광탄 몸통 길이 ≈ 0.6, 한 프레임에 1칸 넘게 날아감)
    // → 매 프레임 지난 자리 → 지금 자리를 판정 모양(상자 · 원) 그대로 쓸어(BoxCast · CircleCast) 그 길의 적 · 보스 · 벽을 가까운 순서로 맞힘
    //   맞음 처리는 예전 OnTriggerEnter2D 그대로(Hit) — 관통 수 · 한 번만(hitOnce) · 보스 · BossPart · 벽(onHitWall)
    //   「들어올 때만」: 지난 프레임에 이미 닿아 있던 콜라이더는 다시 맞히지 않음 (트리거 Enter 와 같게, 떨어졌다 다시 닿으면 또)
    //   다른 사람이 옮기는 탄(부메랑 낫 · 유도탄 등)도 지난 자리 → 지금 자리를 쓸어서 같음
    //   할당 없음: 결과 버퍼 · 필터는 모든 탄이 같이 씀, 닿아 있는 목록은 탄마다 작은 배열 두 개(처음 닿을 때 만듦)
    //   상자 · 원이 아닌 판정(없음)이면 예전처럼 트리거로
    static int shotLayer = -2;
    static readonly RaycastHit2D[] castBuf = new RaycastHit2D[48];
    static readonly Collider2D[] overlapBuf = new Collider2D[48];
    static ContactFilter2D filter;
    static int filterLayer = -99;
    const float TeleportDist = 30f;        // 이보다 멀리 한 번에 옮겨졌으면(되돌아오기 · 자리 옮김) 지금 자리만 검사

    Vector3 lastPos;
    Collider2D sweepCol;                    // 쓸 판정 (켜진 상자 · 원), 없으면 트리거
    int[] touching, touchingNext;
    int touchCount;
    bool gone;                              // 이번 프레임에 사라지기로 함 (Destroy 는 프레임 끝이라)

    // 판정 모양이 바뀌었을 때 (AttackLook.Shape · BulletGlow.FitTracerHit)
    public void RefreshSweepShape()
    {
        sweepCol = null;
        if (TryGetComponent(out CircleCollider2D c) && c.enabled) sweepCol = c;
        else if (TryGetComponent(out BoxCollider2D b) && b.enabled) sweepCol = b;
    }

    static ContactFilter2D Filter(int layer)
    {
        if (filterLayer != layer)
        {
            filterLayer = layer;
            int mask = Physics2D.GetLayerCollisionMask(layer);
            if (shotLayer >= 0) mask &= ~(1 << shotLayer);           // 플레이어 탄끼리는 건너뜀
            filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = mask };
        }
        return filter;
    }

    void Sweep()
    {
        Vector3 now = transform.position;
        Vector2 from = lastPos;
        lastPos = now;
        halt = false;
        if (gone || sweepCol == null || !sweepCol.enabled) return;

        Vector2 d = (Vector2)now - from;
        float dist = d.magnitude;
        if (dist > TeleportDist) { from = now; dist = 0f; }
        Vector2 way = dist > 0.0001f ? d / dist : (Vector2)transform.right;
        float angle = transform.eulerAngles.z;
        Vector3 ls = transform.lossyScale;
        ContactFilter2D f = Filter(gameObject.layer);
        int n;
        bool cast = dist > 0.0001f;

        if (sweepCol is BoxCollider2D box)
        {
            Vector2 size = new Vector2(Mathf.Abs(box.size.x * ls.x), Mathf.Abs(box.size.y * ls.y));
            Vector2 off = transform.TransformVector(box.offset);
            n = cast ? Physics2D.BoxCast(from + off, size, angle, way, f, castBuf, dist)
                     : Physics2D.OverlapBox(from + off, size, angle, f, overlapBuf);
        }
        else
        {
            CircleCollider2D circle = (CircleCollider2D)sweepCol;
            float r = circle.radius * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
            Vector2 off = transform.TransformVector(circle.offset);
            n = cast ? Physics2D.CircleCast(from + off, r, way, f, castBuf, dist)
                     : Physics2D.OverlapCircle(from + off, r, f, overlapBuf);
        }

        // 가까운 순서로 (벽 뒤의 적은 맞지 않게, 관통 수가 다하면 거기서 끝)
        if (cast)
        {
            for (int i = 1; i < n; i++)
            {
                RaycastHit2D h = castBuf[i];
                int j = i - 1;
                while (j >= 0 && castBuf[j].distance > h.distance) { castBuf[j + 1] = castBuf[j]; j--; }
                castBuf[j + 1] = h;
            }
        }

        int next = 0;
        hitFrom = from;
        hitWay = way;
        for (int i = 0; i < n; i++)
        {
            Collider2D c = cast ? castBuf[i].collider : overlapBuf[i];
            if (c == null || c == sweepCol) continue;
            if (!c.CompareTag("enermy") && !c.CompareTag("boss") && !c.CompareTag("Wall")) continue;
            int id = c.GetInstanceID();
            if (touching == null) { touching = new int[8]; touchingNext = new int[8]; }
            bool was = false;
            for (int k = 0; k < touchCount; k++) if (touching[k] == id) { was = true; break; }
            if (next == touchingNext.Length) { System.Array.Resize(ref touching, next * 2); System.Array.Resize(ref touchingNext, next * 2); }
            touchingNext[next++] = id;
            if (was) continue;
            Hit(c);
            if (gone || halt || this == null) break;
        }
        for (int i = 0; i < n; i++) { castBuf[i] = default; overlapBuf[i] = null; }       // 지운 콜라이더를 붙잡고 있지 않게
        if (touching != null)
        {
            int[] t = touching; touching = touchingNext; touchingNext = t;
            touchCount = next;
        }
    }

    void Gone()
    {
        gone = true;
        Release();
    }

    // 관통 수가 다함: 붙은 훅이 살려 두면(onSpent → true) 그대로 둠
    void Spent()
    {
        if (onSpent != null && onSpent()) return;
        Gone();
    }

    private Camera mainCamera;
    private void OnTriggerEnter2D(
        Collider2D collision
    )
    {
        if (sweepCol != null) return;          // 스윕 검사가 맞힘 (Sweep)
        hitWay = dir.sqrMagnitude > 0.0001f ? dir : (Vector2)transform.right;
        hitFrom = (Vector2)transform.position - hitWay;
        Hit(collision);
    }

    // 빈틈 판정 모양 (없으면 배율 0 — WeakHit.Apply 가 그대로 돌려줌)
    //   끝까지 쫓는 유도(유도탄 · 영혼 추적자, nearMiss 0)와 낫이 돌아오는 길은 제외 · 거너 카드 「유도 탄두」(nearMiss > 0)는 선 판정으로만 거름
    WeakShape WeakRay()
    {
        float mul = aimMul > 0f ? (perfect ? 2.1f : aimMul) : 0f;
        if (mul > 0f && aimUntil > 0f && Time.time > aimUntil) mul = 0f;
        if (mul > 0f && TryGetComponent(out Homing h) && h.nearMiss <= 0f) mul = 0f;
        float half = 0.1f;
        if (sweepCol is BoxCollider2D box)
        {
            Vector3 ls = transform.lossyScale;
            half = 0.5f * Mathf.Min(Mathf.Abs(box.size.x * ls.x), Mathf.Abs(box.size.y * ls.y));
        }
        else if (sweepCol is CircleCollider2D circle)
        {
            Vector3 ls = transform.lossyScale;
            half = circle.radius * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
        }
        return WeakShape.Ray(hitFrom, transform.position, hitWay, half, mul);
    }

    void Hit(Collider2D collision)
    {
        if (gone) return;
        using var source = DamageSource.As(dmgSource);
        // =========================
        // 벽에 맞으면 삭제
        // =========================

        if (collision.CompareTag("Wall"))
        {
            if (onHitWall != null) { onHitWall(); return; }
            Gone();

            return;
        }


        // =========================
        // 적에 맞으면
        // =========================

        if (collision.CompareTag("enermy"))
        {
            EnermyController enemy = collision.GetComponent<EnermyController>();
        

            if (enemy != null)
            {
                // 적에게 데미지
                if (enemy.EnemyHealth <= 0f) return;
                if (hitOnce != null && !hitOnce.Add(enemy.GetInstanceID())) return;

                DamagePopup.NextCrit = isCrit;
                SpecialAbilities.CritRolled = critRolled;
                float hit = aimMul > 0f ? WeakHit.Apply(enemy.gameObject, WeakRay(), damage) : damage;     // 중간 보스 빈틈 (WeakSpot 이 붙은 적만)
                enemy.TakeDamage(hit, knockBack, dir);
                WeakHit.End();
                SpecialAbilities.CritRolled = false;
                DamagePopup.NextCrit = false;
                remainPene--;
                Fx.Spawn("fx_spark", transform.position, 1.2f, SkinFx.HitColor(Color.white), 26f);
                Juice.BulletHit();          // 1.1~ 명중음 (간격 0.03초 · 동시 최대 4개)


                // =========================
                // 스킬 포인트 추가
                // =========================

                // 스킬 게이지는 이제 시간으로 참 (SkillGauge)

                onHitEnemy?.Invoke(this, collision);
            }


            // 총알 삭제
            if (remainPene < 1)
            Spent();
        }
        if (collision.CompareTag("boss"))
        {
            // 맞은 그 보스 (분열한 킹 슬라임이 여럿일 때 다른 슬라임이 맞던 문제)
            bosss enemy = collision.GetComponent<bosss>();
            if (enemy == null) enemy = collision.GetComponentInParent<bosss>();

            if (enemy != null)
            {
                // 적에게 데미지
                if (enemy.EnemyHealth <= 0f) return;
                if (hitOnce != null && !hitOnce.Add(enemy.GetInstanceID())) return;

                DamagePopup.NextCrit = isCrit;
                SpecialAbilities.CritRolled = critRolled;
                float hit = aimMul > 0f ? WeakHit.Apply(enemy.gameObject, WeakRay(), damage) : damage;     // 보스 빈틈
                BossMemory.FromBullet = true;          // 거울의 군주 되쏘기는 탄만
                enemy.TakeDamage(hit, knockBack, dir);
                BossMemory.FromBullet = false;
                WeakHit.End();
                SpecialAbilities.CritRolled = false;
                DamagePopup.NextCrit = false;
                remainPene--;
                Fx.Spawn("fx_spark", transform.position, 1.2f, SkinFx.HitColor(Color.white), 26f);
                Juice.BulletHit();          // 1.1~ 명중음 (간격 0.03초 · 동시 최대 4개)


                // =========================
                // 스킬 포인트 추가
                // =========================

                // 스킬 게이지는 이제 시간으로 참 (SkillGauge)
            }
            else if (collision.TryGetComponent(out BossPart part))
            {
                // 보스가 만든 부술 수 있는 것 (리치 왕의 등불 · 묘비 · 영혼구, 2.2.1)
                if (part.Broken) return;
                if (hitOnce != null && !hitOnce.Add(part.GetInstanceID())) return;
                part.Hit(damage);
                remainPene--;
            }


            // 총알 삭제
            if (remainPene < 1)
                Spent();
        }
    }
    public void arisePene()
    {
        pene++;
        remainPene++;
}
}