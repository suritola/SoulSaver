using System.Collections;
using UnityEngine;

// 거울의 군주 · 개발자가 흉내 내는 공사 반장 (2.0.0) — hero-foreman.md 12절
//   평타 「쏟아붓기」(MirrorStrike 의 case Foreman): 군주 앞에 거울 조각 탄 6발이 반원으로 모임(경고 부채꼴 0.6초) → 플레이어 쪽으로 한꺼번에 쏟아짐
//   필살기 「철거 공고」(UltWreck): 플레이어 쪽으로 이어지는 경고 원 3~4개, 0.5초 간격으로 쇠공이 떨어짐(반지름 2) +
//       착지 자리에 1.5초 「공사 중」 탄막 줄 (빈틈 한 칸 · 플레이어 몸 위에는 세우지 않음 — 가두지 않게)
//   개발자 순환: 플레이어 둘레 세 방향에 「추후 업데이트 예정」 표지판이 박히고 1.2초 뒤 표지판 앞 부채꼴로 흙더미 탄이 쏟아짐 (빈 방향 하나로 피함)
public partial class BossSkills
{
    // 쏟아붓기 한 번
    IEnumerator MirrorDump()
    {
        PlayerController p = Hostile.Player;
        if (p == null) yield break;
        Vector2 d = DirTo(p);
        Vector3 c = transform.position;
        Hostile.Line(c, c + (Vector3)(d * 12f), 5f, 0.6f, Mirror);       // 경고 부채꼴 (넓은 띠)
        // 반원으로 모임: 군주 앞 1.6칸에 조각 6개가 부채 모양으로 뜸
        for (int i = 0; i < 6; i++)
        {
            Vector2 side = Rotate(d, -90f + 36f * i);
            Fx.Spawn("fx_sparkle", c + (Vector3)(side * 1.6f), 1.2f, Mirror, 22f);
        }
        yield return new WaitForSeconds(0.6f);
        if (!Alive) yield break;
        p = Hostile.Player;
        if (p != null) d = DirTo(p);
        for (int i = 0; i < 6; i++)
            Hostile.Shoot(transform.position, Rotate(d, (i - 2.5f) * 18f), 14f, Capped(12f * Power), 0.5f, Mirror, 1.4f, 3f);
        Hostile.Play("thump", 0.5f, 0.9f);
        yield return new WaitForSeconds(0.3f);
    }

    // 필살기 「철거 공고」: 플레이어 쪽으로 이어지는 경고 원 n개 → 0.5초 간격으로 쇠공 + 공사 중 탄막 줄
    IEnumerator UltWreck(int count)
    {
        Color col = boss != null && boss.developer ? Dev : Mirror;
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        Vector3 from = transform.position, to = pl.transform.position;
        Vector3[] spots = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float k = (i + 1f) / count;
            spots[i] = Hostile.ClampArena(Vector3.Lerp(from, to, k) + (Vector3)(Random.insideUnitCircle * 0.8f));
            if (Hostile.IsWall(spots[i])) spots[i] = Hostile.ClampArena(to);
            Hostile.Circle(spots[i], 2f, 1f + 0.5f * i, col);
        }
        if (boss != null) boss.noContactUntil = Time.time + 1f + 0.5f * count + 0.4f;
        yield return Windup(col, 1f);
        for (int i = 0; i < count && Alive; i++)
        {
            Vector3 at = spots[i];
            Fx.Spawn("fx_shock", at, 4.4f, col, 20f);
            Fx.Spawn("fx_explosion", at, 3f, col, 18f);
            UltHit(UltInCircle(at, 2f));
            Hostile.Play("boom", 0.5f, 0.8f);
            Hostile.Shake(0.15f);
            // 공사 중 탄막 줄: 착지 자리에서 굴림 방향에 수직으로 7개, 빈틈 한 칸, 플레이어 몸 위에는 세우지 않음
            Vector2 roll = ((Vector2)(to - from)).sqrMagnitude < 0.01f ? Vector2.right : ((Vector2)(to - from)).normalized;
            Vector2 perp = new Vector2(-roll.y, roll.x);
            int gap = Random.Range(1, 6);
            PlayerController now = Hostile.Player;
            for (int j = 0; j < 7; j++)
            {
                if (j == gap) continue;
                Vector3 sp = at + (Vector3)(perp * (j - 3) * 1.2f);
                if (Hostile.IsWall(sp) || (now != null && Vector2.Distance(sp, now.transform.position) < 1.6f)) continue;
                if (HostileProjectile.Live.Count >= 120) break;
                Hostile.Shoot(sp, roll, 0.6f, Capped(10f * Power), 0.5f, col, 1.3f, 1.5f);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // 개발자: 플레이어 둘레 세 방향에 표지판이 박히고 1.2초 뒤 흙더미 탄이 부채꼴로 쏟아짐 (빈 방향 하나)
    IEnumerator DevSigns(int times)
    {
        for (int n = 0; n < times && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 center = pl.transform.position;
            float start = Random.Range(0f, 360f);
            int open = Random.Range(0, 3);
            float warn = DevTele(1.2f);
            Vector3[] pos = new Vector3[3];
            for (int k = 0; k < 3; k++)
            {
                Vector2 dir = Rotate(Vector2.right, start + 120f * k);
                pos[k] = Hostile.ClampArena(center + (Vector3)(dir * 5f));
                if (k == open) continue;
                Hostile.Line(pos[k], pos[k] + (Vector3)(-dir * 8f), 3f, warn, Dev);
                Fx.Spawn("fx_target_rune", pos[k], 3f, Dev, 12f);
            }
            yield return new WaitForSeconds(warn);
            if (!Alive) yield break;
            for (int k = 0; k < 3; k++)
            {
                if (k == open) continue;
                Vector2 inward = ((Vector2)(center - pos[k])).normalized;
                for (int i = 0; i < 5; i++)
                {
                    if (HostileProjectile.Live.Count >= DevBulletCap) break;
                    HostileProjectile h = Hostile.Shoot(pos[k], Rotate(inward, (i - 2f) * 9f), 9f, Capped(12f * Power) * DevPow, 0.5f, Dev, 1.5f, 3f);
                    if (h != null) h.label = DevName;
                }
            }
            Hostile.Play("thump", 0.5f, 0.8f);
            yield return new WaitForSeconds(0.4f);
        }
    }
}
