using System.Collections;
using UnityEngine;

// 거울의 군주 · 개발자가 흉내 내는 씨름꾼 (2.0.0) — hero-wrestler.md 12절
//   평타 「샅바 잡기」(BossSkills.MirrorStrike 의 case Wrestler): 플레이어 쪽으로 다가서며 플레이어 자리에 잡기 예고 원(반지름 2.2 · 0.55초) →
//       안에 있으면 4칸 밀쳐짐 + 피해. 조작은 막지 않음 (들어 올리지 않음 — 플레이어 조작을 빼앗는 기술 금지)
//   필살기 「천하장사」(UltSandSlam): 높이 뛰어 플레이어 자리에 그림자(반지름 6 · 1초 경고) → 내려찍기 충격파 + 그 자리에 「거울 모래판」 5초 —
//       플레이어가 안에 있으면 1초마다 바깥으로 1.5칸 밀어내고, 경계를 넘을 때 피해 ×1.3 「장외!」
//   개발자 순환: 세 번(분노 넷) 뛰어 플레이어 쪽에 내려찍고 착지마다 모래 탄 고리(DevRing 12발) + DevBulletStorm (DeveloperUlt 의 case Wrestler)
public partial class BossSkills
{
    // 샅바 잡기 한 번 (strong = 예고 원 둘)
    IEnumerator MirrorGrab(bool strong)
    {
        int circles = strong ? 2 : 1;
        for (int k = 0; k < circles && Alive; k++)
        {
            PlayerController p = Hostile.Player;
            if (p == null) yield break;
            Vector3 at = Hostile.ClampArena(p.transform.position + (k == 0 ? Vector3.zero : (Vector3)(Random.insideUnitCircle.normalized * 2.6f)));
            Hostile.Circle(at, 2.2f, 0.55f, Mirror);
            // 플레이어 쪽으로 다가섬
            Vector3 step = Hostile.ClampArena(transform.position + (Vector3)(DirTo(p) * 3f));
            if (!Hostile.IsWall(step)) { if (boss != null) boss.noContactUntil = Time.time + 0.9f; transform.position = step; }
            yield return new WaitForSeconds(0.55f);
            if (!Alive) yield break;
            Fx.Spawn("fx_shock", at, 4.4f, Mirror, 20f);
            Hostile.Play("thump", 0.6f, 0.8f);
            p = Hostile.Player;
            if (p != null && Hostile.HurtDistance(p, at) < 2.2f + 0.2f)
            {
                // 2.0.1~ 무적(구르기 · 피격 직후 등) 중이거나 밀리지 않는 씨름꾼(통뼈 · 모래판 · 철벽 샅바)이면 밀치지 않음 — 예전엔 피해만 막히고 4칸 밀려났음
                bool pushable = !p.IsInvincible && !CharacterKit.HeroUnpushable;
                Hostile.HitCircle(at, 2.2f, Capped(16f * Power));
                Vector3 away = (Vector3)((Vector2)(p.transform.position - transform.position)).normalized * 4f;
                Vector3 np = Hostile.ClampArena(p.transform.position + away);
                if (pushable && !Hostile.IsWall(np)) p.transform.position = np;       // 4칸 밀쳐짐 (조작은 그대로)
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    // 필살기 「천하장사」: 높이 뛰어 플레이어 자리에 내려찍음 (그림자 1초 경고) + 거울 모래판 5초
    IEnumerator UltSandSlam(int times)
    {
        Color col = boss != null && boss.developer ? Dev : Mirror;
        for (int n = 0; n < times && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 at = Hostile.ClampArena(pl.transform.position);
            if (Hostile.IsWall(at)) at = transform.position;
            Hostile.Circle(at, 6f, 1f, col);
            Fx.Spawn("fx_target_rune", at, 12f, col, 12f);
            if (boss != null) boss.noContactUntil = Time.time + 1f + 0.35f + 0.6f;
            yield return Windup(col, 1f);
            if (!Alive) yield break;
            transform.position = at;
            Fx.Spawn("fx_shock", at, 13f, col, 18f);
            Fx.Spawn("fx_soulburst", at, 8f, col, 16f);
            UltHit(UltInCircle(at, 6f));
            Hostile.Play("boom", 0.7f, 0.7f);
            Hostile.Shake(0.25f);
            StartCoroutine(MirrorSandRing(at, col));
            yield return new WaitForSeconds(0.35f);
        }
    }

    // 거울 모래판 5초: 안에 있으면 1초마다 바깥으로 1.5칸 밀어냄 · 경계를 넘을 때 피해 ×1.3 「장외!」
    IEnumerator MirrorSandRing(Vector3 at, Color col)
    {
        const float r = 5.5f;
        LineRenderer ring = Hostile.NewLine("MirrorSand", new Color(col.r, col.g, col.b, 0.8f), 0.18f, 19);
        Own(ring.gameObject);
        ring.loop = true;
        Hostile.SetArc(ring, at, r, 0f, 360f);
        for (int s = 0; s < 5; s++)
        {
            yield return new WaitForSeconds(1f);
            if (!Alive) break;
            PlayerController p = Hostile.Player;
            if (p == null || ring == null) break;
            Vector2 off = p.transform.position - at;
            if (off.magnitude > r) continue;
            Vector2 away = off.sqrMagnitude < 0.01f ? Random.insideUnitCircle.normalized : off.normalized;
            Vector3 np = Hostile.ClampArena(p.transform.position + (Vector3)(away * 1.5f));
            if (p.IsInvincible || CharacterKit.HeroUnpushable) continue;       // 2.0.1~ 무적 · 밀리지 않는 씨름꾼이면 밀어내지 않음 (장외 피해도 없음)
            if (!Hostile.IsWall(np)) p.transform.position = np;
            Fx.Spawn("fx_sparkle", p.transform.position, 1.6f, col, 22f);
            if (((Vector2)(p.transform.position - at)).magnitude > r)
            {
                if (boss != null && boss.developer) AttackLabel.Pending = DevName;
                p.TryHit(Capped(UltHitDamage * Power * 1.3f) * (boss != null && boss.developer ? DevPow : 1f));       // 장외!
                if (boss != null && boss.developer) AttackLabel.Pending = null;
                SpecialAbilities.SharedFx?.FloatText(p.transform.position + Vector3.up * 1.4f, Loc.T("장외!"), new Color(1f, 0.82f, 0.3f), 5f, 0.2f);
            }
        }
        if (ring != null) Destroy(ring.gameObject);
    }

    // 개발자: 세 번(분노 넷) 뛰어 플레이어 쪽에 내려찍고 착지마다 모래 탄 고리 + 탄막
    IEnumerator DevSandSlam(int times)
    {
        for (int n = 0; n < times && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 at = Hostile.ClampArena(pl.transform.position);
            if (Hostile.IsWall(at)) at = transform.position;
            float warn = DevTele(1f);
            Hostile.Circle(at, 5f, warn, Dev);
            if (boss != null) boss.noContactUntil = Time.time + warn + 0.35f + 0.6f;
            yield return Windup(Dev, warn);
            if (!Alive) yield break;
            transform.position = at;
            Fx.Spawn("fx_shock", at, 11f, Dev, 18f);
            UltHit(UltInCircle(at, 5f));
            DevRing(12, 8f, n * 15f, 12f);          // 착지마다 모래 탄 고리
            Hostile.Play("boom", 0.6f, 0.75f);
            Hostile.Shake(0.2f);
            yield return new WaitForSeconds(0.35f);
        }
    }
}
