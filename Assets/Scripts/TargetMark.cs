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

        if (enemy == null) enemy = target.GetComponent<EnermyController>();

        if (player == null) player = FindFirstObjectByType<PlayerController>();

        if (enemy == null || player == null) return;

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


}
