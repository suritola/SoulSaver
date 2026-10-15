using UnityEngine;

public class Bullet : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
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

    private int remainPene;

    void Awake()
    {
        // 플레이어 탄 전용 층: 스윕 검사가 다른 플레이어 탄을 건너뛰게 (충돌 행렬은 모든 층과 그대로 닿음)
        if (shotLayer == -2) shotLayer = LayerMask.NameToLayer("PlayerShot");
        if (shotLayer >= 0) gameObject.layer = shotLayer;
    }

    void Start()
    {
        remainPene = pene;
        playerC = Hostile.Player;                  // 총알마다 씬 전체를 뒤지지 않게 (캐시)
        mainCamera = Camera.main;
        Destroy(gameObject, lifetime);
        lastPos = transform.position;
        if (TryGetComponent(out BulletGlow g)) g.FitTracerHit();      // 예광탄 판정을 첫 검사 전에 보이는 몸통으로
        RefreshSweepShape();
    }


    // 1.0.4~ 개발자(무한 모드 최종 보스)가 거너 조준 사격을 쓰는 동안 플레이어 탄이 느려짐 (PlayerController.HostileTimeSlow)
    // 겹쳐 써도 곱해 쌓이지 않고 끝나는 때만 늘어남
    public static float SlowUntil;
    public static float SlowMul = 1f;

    void Update()
    {
        float slow = Time.time < SlowUntil ? SlowMul : 1f;
        transform.position += (Vector3)dir * speed * slow * Time.deltaTime;
        Sweep();
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
            if (gone || this == null) break;
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
        Destroy(gameObject);
    }

    private Camera mainCamera;
    private void OnTriggerEnter2D(
        Collider2D collision
    )
    {
        if (sweepCol != null) return;          // 스윕 검사가 맞힘 (Sweep)
        Hit(collision);
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
                enemy.TakeDamage(damage, knockBack, dir);
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
            Gone();
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
                enemy.TakeDamage(damage, knockBack, dir);
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
                Gone();
        }
    }
    public void arisePene()
    {
        pene++;
        remainPene++;
}
}