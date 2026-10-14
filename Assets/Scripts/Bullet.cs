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
    void Start()
    {
        remainPene = pene;
        playerC = Hostile.Player;                  // 총알마다 씬 전체를 뒤지지 않게 (캐시)
        mainCamera = Camera.main;
        Destroy(gameObject, lifetime);
    }


    // 1.0.4~ 개발자(무한 모드 최종 보스)가 거너 조준 사격을 쓰는 동안 플레이어 탄이 느려짐 (PlayerController.HostileTimeSlow)
    // 겹쳐 써도 곱해 쌓이지 않고 끝나는 때만 늘어남
    public static float SlowUntil;
    public static float SlowMul = 1f;

    void Update()
    {
        float slow = Time.time < SlowUntil ? SlowMul : 1f;
        transform.position += (Vector3)dir * speed * slow * Time.deltaTime;
    }

    private Camera mainCamera;
    private void OnTriggerEnter2D(
        Collider2D collision
    )
    {
        using var source = DamageSource.As(dmgSource);
        // =========================
        // 벽에 맞으면 삭제
        // =========================

        if (collision.CompareTag("Wall"))
        {
            if (onHitWall != null) { onHitWall(); return; }
            Destroy(gameObject);

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


                // =========================
                // 스킬 포인트 추가
                // =========================

                // 스킬 게이지는 이제 시간으로 참 (SkillGauge)

                onHitEnemy?.Invoke(this, collision);
            }


            // 총알 삭제
            if (remainPene < 1)
            Destroy(gameObject);
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
                Destroy(gameObject);
        }
    }
    public void arisePene()
    {
        pene++;
        remainPene++;
}
}