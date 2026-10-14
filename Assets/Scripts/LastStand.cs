using UnityEngine;

// 최후의 보루 (1.0.1): 체력이 30% 아래로 떨어지면 장마다 한 번 자동으로 2초 무적 + 둘레의 적을 밀쳐 내는 금빛 충격파 · 날아오던 탄을 지움
// 판 기록에서 생존 가지가 거의 안 찍혀 위기에 빠져나올 수단이 없었음 → 모든 영웅 기본으로. 영혼 트리 생존 가지 끝 칸이 횟수를 늘림 (Extra)
public static class LastStand
{
    const float Threshold = 0.3f, Radius = 6f, Invincible = 2f;
    static readonly Color Gold = new Color(1f, 0.85f, 0.35f);

    static int used;
    public static int Extra;            // 영혼 트리 「불굴」 (장마다 +1회)
    public static int Charges => 1 + Extra;

    public static void ResetRun() { used = 0; Extra = 0; }
    public static void ResetChapter() => used = 0;
    // 판 중간 저장: 이번 장에서 쓴 횟수 (이어하기로 다시 채워지지 않게, 1.0.4~)
    public static int Used { get => used; set => used = Mathf.Max(0, value); }

    // PlayerController.TryHit 에서 피해를 받은 직후
    public static void Check(PlayerController p)
    {
        if (p == null || p.PlayerHealth <= 0f || p.PlayerHealth > p.PlayerMaxHealth * Threshold || used >= Charges) return;
        used++;
        p.GrantInvincibility(Invincible);
        Vector3 c = p.transform.position;

        // 둘레의 적을 밀쳐 내고 조금 아프게
        foreach (EnermyController e in Object.FindObjectsByType<EnermyController>(FindObjectsSortMode.None))
        {
            if (e == null || e.IsDead) continue;
            Vector3 d = e.transform.position - c;
            d.z = 0f;
            if (d.sqrMagnitude > Radius * Radius) continue;
            if (d.sqrMagnitude < 0.01f) d = Vector3.right;
            e.transform.position = Hostile.ClampArena(c + d.normalized * (Radius + 0.5f));
            Specials.Damage(e.gameObject, p.damage * 2f, d.normalized, 3f);
        }
        // 날아오던 탄은 사라짐
        for (int i = HostileProjectile.Live.Count - 1; i >= 0; i--)
        {
            HostileProjectile h = HostileProjectile.Live[i];
            if (h != null && Vector2.Distance(h.transform.position, c) < Radius + 3f) Object.Destroy(h.gameObject);
        }

        ShockRing.Spawn(c, 0.5f, Radius * 1.3f, 0.45f, Gold, 0.5f);
        Fx.Spawn("fx_shock", c, Radius * 2.2f, Gold, 16f);
        Fx.Spawn("fx_sparkle", c, 3f, Color.white, 20f);
        DamageFlash.Show(0.4f);
        Hostile.Play("chime", 0.8f, 0.8f);
        Hostile.Play("pulse", 0.7f, 0.7f);
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.FloatText(c + Vector3.up * 1.5f, Loc.T("최후의 보루!"), Gold, 6f, 0f);
        Hints.Show("laststand", "체력이 30% 아래로 떨어지면 장마다 한 번 「최후의 보루」가 발동해 2초 동안 무적이 되고 둘레의 적을 밀쳐 냅니다.");
    }
}
