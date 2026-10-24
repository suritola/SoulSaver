using UnityEngine;

public class TargetMark : MonoBehaviour
{
    [Header("타겟")]
    public Transform target;


    [Header("회전")]
    public float rotationSpeed = 180f;

    [Header("크기")]
    public float startScale = 2f;
    public float endScale = 1f;

    [Header("색상")]
    public Color weakColor = Color.gray;

    private SpriteRenderer spriteRenderer;
    private EnermyController enemy;
    private PlayerController player;
    // 1.5.9~ 보스에 붙은 표식 (PlayerController.FindNextTarget 이 bosss 에도 붙임) — EnermyController 만 찾아 늘 처음 크기 · 색으로 멈춰 있었음
    private bosss boss;
    private bool looked;
    // PlayerController.GunUltBossMul 과 같게 (보스에게는 필살기 총알 한 발이 절반)
    const float BossShotMul = 0.5f;

    private Color normalColor;

    private bool damageEnough = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        player = FindFirstObjectByType<PlayerController>();

        if (spriteRenderer != null) normalColor = spriteRenderer.color;

        // 처음에는 200%
        transform.localScale = Vector3.one * startScale;

        // 1.0.4~: 표식의 색 · 크기 · 회전 뜻을 처음 한 번 알려 줌
        Hints.Show("targetmark", "필살기 조준 표식: 회색으로 크게 보이면 아직 필살기 한 방에 쓰러뜨리지 못하고, 작아질수록 가까워지며, 색이 돌아와 빙글빙글 돌면 필살기로 처치할 수 있는 적입니다.");
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 적을 따라감
        transform.position = target.position;

        // 1.5.9~ 대상은 붙을 때 정해진 뒤 바뀌지 않으므로 한 번만 찾음 (매 프레임 GetComponent 하던 것)
        if (!looked)
        {
            looked = true;
            enemy = target.GetComponent<EnermyController>();
            if (enemy == null) boss = target.GetComponentInParent<bosss>();
        }

        if (player == null) player = FindFirstObjectByType<PlayerController>();

        if (player == null) return;
        if (enemy == null)
        {
            if (boss != null && !boss.IsDead) ShowBoss();
            return;
        }

        // =====================================
        // 데미지가 아직 부족한 상태
        // =====================================

        // 1.0.4~: 매 프레임 다시 판단 (예전엔 한 번 처치 가능이 되면 적이 회복해도 계속 도는 표식으로 남았음)
        bool enough = player.skillDamage >= enemy.EnemyHealth;
        if (!enough)
        {
            // 회색
            if (spriteRenderer != null) spriteRenderer.color = weakColor;

            // 현재 데미지 / 적 체력 비율 (0 ~ 1)
            float damageRatio = Mathf.Clamp01(player.skillDamage / Mathf.Max(0.01f, enemy.EnemyHealth));

            // 데미지 비율에 따라 크기 결정 (작아질수록 처치에 가까움)
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, damageRatio);
            damageEnough = false;

            // 아직 부족하면 회전하지 않음
            return;
        }
        if (!damageEnough)
        {
            damageEnough = true;
            if (spriteRenderer != null) spriteRenderer.color = normalColor;
            transform.localScale = Vector3.one * endScale;
        }

        // =====================================
        // 데미지가 충분해진 이후
        // =====================================

        // 현재 크기 그대로 유지
        // 회전만 시작
        transform.Rotate(Vector3.forward,rotationSpeed * Time.unscaledDeltaTime);
    }

    // 1.5.9~ 보스: 실제 피해는 방어 · 기억 · 무적 같은 배율을 거쳐 미리 알 수 없으므로 「처치 가능(색 돌아와 돎)」은 띄우지 않고
    //   회색 그대로 크기로만 가까움을 보여 줌 (총알 한 발 = skillDamage × 0.5 × 필살기 강화, ShotsPerTarget 발 · 공통 피해 배율)
    void ShowBoss()
    {
        if (spriteRenderer != null) spriteRenderer.color = weakColor;
        float power = player.special != null ? player.special.UltPower(SpecialAbilities.PistolUlt) : 1f;
        float dmg = player.skillDamage * BossShotMul * power * Mathf.Max(1, player.ShotsPerTarget) * GameMode.PlayerDamageMul;
        float damageRatio = Mathf.Clamp01(dmg / Mathf.Max(0.01f, boss.EnemyHealth));
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, damageRatio);
        damageEnough = false;
    }


}
