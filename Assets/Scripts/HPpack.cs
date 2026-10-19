using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPpack : MonoBehaviour
{
    public GameObject pack;
    PlayerController playerC;
    public float healAmount = 15f;
    // Start is called before the first frame update
    void Start()
    {
        playerC = Hostile.Player;           // 1.1.9: 생성마다 씬 전체 검색 대신 캐시
        PickupGlow.Add(pack != null ? pack : gameObject, new Color(1f, 0.35f, 0.4f), 1.4f);      // 1.0.2: 스킬 이펙트에 묻히지 않게
        Destroy(pack != null ? pack : gameObject, Life);       // 1.4.1~ 45초 뒤 사라짐 (예전엔 영원히 남아 한계 돌파 후반에 수백 개가 쌓여 렉)
    }

    // 1.4.1~ 바닥에 깔린 회복약 (상한 · 검색용) — 사용자 「개발자 엄마를 잡은 뒤 후반에 체력 물약 때문에 렉이 너무 심해」
    public static readonly List<HPpack> Live = new List<HPpack>();
    public const int Cap = 24;              // 동시에 이만큼 넘게는 떨어뜨리지 않음 (EnermyController.Death)
    const float Life = 45f;
    void OnEnable() { Live.Add(this); }
    void OnDisable() { Live.Remove(this); }

    // Update is called once per frame
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // =========================
        // 힐팩 충돌
        // =========================

        if (used || !collision.CompareTag("Player")) return;
        if (playerC == null) playerC = Hostile.Player;
        if (playerC == null || playerC.IsDying) return;

        // 같은 프레임에 두 번 닿아도 한 번만 회복
        used = true;
        RunCounters.Add("meat");        // 1.2~ 영혼 계약 「든든한 한 끼」
        Destroy(pack);
        float before = playerC.PlayerHealth;
        playerC.PlayerHealth = Mathf.Min(playerC.PlayerMaxHealth, playerC.PlayerHealth + healAmount * SoulWeight.HealMul);     // 1.2~ 영혼의 무게 4: -40%

        // 회복한 양을 머리 위에 초록 숫자로 + 짧은 소리
        int healed = Mathf.CeilToInt(playerC.PlayerHealth - before);
        if (healed > 0 && SpecialAbilities.SharedFx != null)
            SpecialAbilities.SharedFx.FloatText(playerC.transform.position, "+" + healed, new Color(0.45f, 1f, 0.5f), 5f, 0f);
        Hostile.Play("chime", 0.45f, 1.25f);
    }

    bool used;
}
