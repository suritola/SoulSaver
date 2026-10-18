using System.Collections.Generic;
using UnityEngine;

// 거너 전용 레벨업 카드: 수치가 아니라 총알이 하는 일을 바꿈 (권총 · 진화한 무기 모두에 붙음)
//  0 도탄 사격 · 1 폭발 탄두 · 2 소각탄 · 3 유도 탄두 · 4 전기탄 · 5 장전 충격파
public partial class SpecialAbilities
{
    public const int GunRicochet = 0, GunExplosive = 1, GunIncendiary = 2, GunHoming = 3, GunShock = 4, GunReloadWave = 5;
    public readonly int[] gunCard = new int[6];
    int gunShotCount;
    readonly List<Collider2D> gunHits = new List<Collider2D>();

    // 새 총알에 카드 효과를 붙임. light = 화염 방사기처럼 아주 촘촘히 나가는 판정탄 (무거운 효과는 뺌)
    public void ApplyGunCards(Bullet b, bool light, bool countShot = true)
    {
        if (b == null || !CharacterData.IsGunner) return;
        SignatureSkills.GunBullet(b);               // 고유 스킬: 총열 과열 · 영혼 탄환
        b.critRolled = true;                        // 치명타는 여기서 한 번만 (피해 훅에서 또 굴리지 않게)
        // 영혼 트리: 치명타 (피해 2배, 금색) · 탄속
        if (TreeCrit > 0f && Random.value < TreeCrit) MakeCrit(b);
        ApplyEvoAugments(b, light, countShot);                 // 무기 진화 능력 (SpecialAbilities.EvoAugments)
        if (!light && TreeBulletSpeed != 1f) b.speed *= TreeBulletSpeed;
        if (TreeExecute > 0f) b.onHitEnemy += (bullet, col) => Execute(col, bullet.damage);
        bool boom = countShot && !light && GunBoomShot();
        // 유도: 부메랑 낫 · 이미 유도되는 추적탄에는 붙이지 않음
        bool curves = !(WeaponActive && (CurrentWeapon == ScytheId || CurrentWeapon == SeekerId));
        if (!light && curves && gunCard[GunHoming] > 0 && b.GetComponent<Homing>() == null)
        {
            // 2.2.3: 스치면 보정 — 곧게 날아가다 적 옆을 스치면 그 적에게 꺾여 맞음 (보정 범위 0.9 · 1.3 · 1.7칸)
            Homing h = b.gameObject.AddComponent<Homing>();
            h.nearMiss = 0.5f + 0.4f * gunCard[GunHoming];
        }
        if (boom && b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(1f, 0.6f, 0.2f);
        if (gunCard[GunRicochet] == 0 && gunCard[GunIncendiary] == 0 && gunCard[GunShock] == 0 && !boom) return;

        bool firstHit = true;
        b.onHitEnemy += (bullet, col) =>
        {
            GunCardHit(col, bullet.damage, bullet.Direction, light, firstHit && boom);
            // 2026-10-19~ 도탄 난사 갈래면 튕김은 갈래가 맡음 (카드 단계 = 튕김 횟수 · 피해, Paths/Paths.Gunner.Effects.cs)
            if (firstHit && !light && gunCard[GunRicochet] > 0 && !GunnerPaths.OwnsRicochet) Ricochet(bullet, col);
            firstHit = false;
        };
    }

    // 처형: 체력 20% 아래인 적에게 추가 피해
    void Execute(Collider2D col, float dmg)
    {
        if (col == null || !col.TryGetComponent(out EnermyController e) || e.IsDead) return;
        if (e.EnemyHealth > e.setEnemyHP * 0.2f) return;
        Specials.Damage(col.gameObject, dmg * TreeExecute, Vector3.zero, 0f);
    }

    // 폭발 탄두: 5 · 4 · 3번째 발마다 true
    bool GunBoomShot()
    {
        if (gunCard[GunExplosive] <= 0) return false;
        if (++gunShotCount < 6 - gunCard[GunExplosive]) return false;
        gunShotCount = 0;
        return true;
    }

    // 범위 공격(용암탄)에 맞은 적들에게 카드 효과
    public void GunAreaHit(Vector3 at, float radius, float dmg)
    {
        if (!CharacterData.IsGunner) return;
        bool boom = GunBoomShot();
        foreach (Collider2D c in Specials.Overlap(at, radius, gunHits))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            GunCardHit(c, dmg, Vector2.zero, false, boom);
            boom = false;
        }
    }

    // 총알이 아닌 공격(산탄 폭발 등)이 적을 맞혔을 때도 같은 효과
    public void GunCardHit(Collider2D col, float dmg, Vector2 dir, bool light, bool boom)
    {
        if (col == null || !CharacterData.IsGunner) return;
        Vector3 at = col.transform.position;
        int burn = gunCard[GunIncendiary];
        if (burn > 0) Burn.Apply(col.gameObject, Damage * (0.05f + 0.1f * burn), 2f);
        int shock = gunCard[GunShock];
        if (shock > 0 && Random.value < (0.05f + 0.1f * shock) * (light ? 0.15f : 1f))
            ChainLightning(at, col, Mathf.Max(dmg, Damage) * 0.5f, 2);
        if (boom) Explode(at, 2.5f, Damage * 0.8f, 1.2f, new Color(1f, 0.55f, 0.2f, 0.85f));
    }

    // 도탄: 처음 맞힌 적에게서 가장 가까운 다른 적에게 작은 탄이 튕겨 나감
    void Ricochet(Bullet from, Collider2D hit)
    {
        Vector3 at = hit.transform.position;
        Collider2D best = null;
        float bestD = 9f;
        foreach (Collider2D c in Specials.Overlap(at, 9f, gunHits))
        {
            if (c == hit || !c.CompareTag("enermy")) continue;
            float d = Vector2.Distance(at, c.transform.position);
            if (d < bestD) { bestD = d; best = c; }
        }
        if (best == null) return;
        Vector2 dir = ((Vector2)(best.transform.position - at)).normalized;
        float rate = 0.25f + 0.15f * gunCard[GunRicochet];         // 40% · 55% · 70%
        Bullet b = player.CreateBullet(at + (Vector3)(dir * 0.6f), dir, from.damage * rate, 1, 0, false, 0.3f);
        if (b == null) return;
        b.skillCharge = 0f;
        b.hitOnce = new HashSet<int> { hit.GetComponent<EnermyController>() is EnermyController e ? e.GetInstanceID() : 0 };
        b.transform.localScale *= 0.75f;
        if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(1f, 0.9f, 0.4f);
        // 튕긴 탄에도 불 · 번개는 붙지만 또 튕기지는 않음
        if (gunCard[GunIncendiary] > 0 || gunCard[GunShock] > 0)
            b.onHitEnemy += (bullet, col) => GunCardHit(col, bullet.damage, bullet.Direction, false, false);
        Flash(at, 0.9f, new Color(1f, 0.9f, 0.4f, 0.8f), 0.08f);
    }

    // 장전 충격파: 장전을 시작하면 몸 주변을 밀어내며 폭발
    public void ReloadShockwave(Vector3 at)
    {
        int lv = gunCard[GunReloadWave];
        if (lv <= 0 || !CharacterData.IsGunner) return;
        Explode(at, 3f + 0.5f * lv, Damage * (0.4f + 0.6f * lv), 3f, new Color(1f, 0.75f, 0.35f, 0.85f));
    }
}
