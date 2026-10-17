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
    }

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
