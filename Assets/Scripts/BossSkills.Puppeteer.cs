using System.Collections;
using UnityEngine;

// 거울의 군주 · 개발자가 흉내 내는 인형사 (2.0.0) — hero-puppeteer.md 12-4
//   평타 「거울 휘두름」(MirrorStrike 의 case Puppeteer): 플레이어가 0.6초 뒤 있을 자리(이동 방향 예측)에 거울빛 더미가 떠서 0.6초 예고 원(반지름 1.7) 뒤 한 바퀴 휘두름 +
//       군주 ↔ 더미 사이 줄이 0.4초 예고선 뒤 칼날. 두 번 되풀이
//   필살기 「거울 커튼콜」(UltCurtain): 플레이어 둘레 네 곳(반지름 3.5)에 위에서 줄 예고선 0.8초 → 거울 더미 넷이 내려와 차례로(0.35초 간격) 휘두름 → 박수와 함께 올라가며 반지름 4.5 고리 피해 (강해지면 여섯)
//   개발자 순환 「임시 더미 배치」: 「나중에 바꿀게!」 말풍선과 함께 T 자세 더미 5(강하면 7)를 플레이어 둘레에 떨어뜨림(착지 예고 원 · 착지 피해) →
//       1초 뒤 더미끼리 줄로 이어져 0.5초 칼날(줄 예고선) · 더미 사이 빈 칸으로 피함
public partial class BossSkills
{
    // 평타: 두 번 되풀이
    IEnumerator MirrorPuppetSwing()
    {
        for (int rep = 0; rep < 2 && Alive; rep++)
        {
            PlayerController p = Hostile.Player;
            if (p == null) yield break;
            // 이동 방향 예측: 0.1초 사이 움직임으로 0.6초 뒤 자리를 어림 (최대 5칸)
            Vector3 p0 = p.transform.position;
            yield return new WaitForSeconds(0.1f);
            if (!Alive) yield break;
            p = Hostile.Player;
            if (p == null) yield break;
            Vector3 p1 = p.transform.position;
            Vector3 vel = (p1 - p0) / 0.1f;
            Vector3 spot = Hostile.ClampArena(p1 + Vector3.ClampMagnitude(vel * 0.5f, 5f));
            if (Hostile.IsWall(spot)) spot = p1;
            Hostile.Circle(spot, 1.7f, 0.6f, Mirror);
            Fx.Spawn("fx_sparkle", spot + Vector3.up * 0.8f, 1.6f, Mirror, 22f);
            yield return new WaitForSeconds(0.2f);
            if (!Alive) yield break;
            Hostile.Line(transform.position, spot, 0.8f, 0.4f, Mirror);          // 줄 예고선 (군주 ↔ 더미)
            yield return new WaitForSeconds(0.4f);
            if (!Alive) yield break;
            Fx.Spawn(Fx.Frames("fx_puppet_swing").Length > 0 ? "fx_puppet_swing" : "fx_shock", spot, 3.4f, Mirror, 20f);
            Fx.Stretch(Fx.Frames("fx_string_twang").Length > 0 ? "fx_string_twang" : "fx_shock", transform.position, spot, 0.8f, Mirror, 0.2f, 15, 24f);
            Hostile.HitCircle(spot, 1.7f, Capped(15f * Power));
            PlayerController pl = Hostile.Player;
            if (pl != null && Hostile.HurtToSegment(pl, transform.position, spot) < 0.7f) pl.TryHit(Capped(13f * Power));
            Hostile.Play("whoosh", 0.5f, 0.8f);
            yield return new WaitForSeconds(0.15f);
        }
        yield return new WaitForSeconds(0.2f);
    }

    // 필살기 「거울 커튼콜」: 네 곳(강하면 여섯) 줄 예고 → 차례로 휘두름 → 박수와 함께 고리 피해
    IEnumerator UltCurtain(int count)
    {
        Color col = boss != null && boss.developer ? Dev : Mirror;
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        Vector3 center = pl.transform.position;
        Vector3[] spots = new Vector3[count];
        float start = Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
        {
            Vector2 d = Rotate(Vector2.right, start + 360f / count * i);
            spots[i] = Hostile.ClampArena(center + (Vector3)(d * 3.5f));
            if (Hostile.IsWall(spots[i])) spots[i] = center;
            Hostile.Line(spots[i] + Vector3.up * 9f, spots[i], 0.5f, 0.8f, col);        // 위에서 내려오는 줄 예고선
            Hostile.Circle(spots[i], 1.7f, 0.8f + 0.35f * i, col);
        }
        Hostile.Circle(center, 4.5f, 0.8f + 0.35f * count + 0.2f, col);
        if (boss != null) boss.noContactUntil = Time.time + 0.8f + 0.35f * count + 0.6f;
        yield return new WaitForSeconds(0.8f);
        for (int i = 0; i < count && Alive; i++)
        {
            Fx.Spawn(Fx.Frames("fx_puppet_swing").Length > 0 ? "fx_puppet_swing" : "fx_shock", spots[i], 3.4f, col, 20f);
            Fx.Spawn(Fx.Frames("fx_curtain_string").Length > 0 ? "fx_curtain_string" : "fx_sparkle", spots[i], 3f, col, 20f);
            UltHit(UltInCircle(spots[i], 1.7f));
            Hostile.Play("thump", 0.4f, 1f + 0.06f * i);
            yield return new WaitForSeconds(0.35f);
        }
        if (!Alive) yield break;
        // 끝: 박수와 함께 더미가 올라가며 반지름 4.5 고리
        Fx.Spawn("fx_shock", center, 9f, col, 20f);
        UltHit(UltInCircle(center, 4.5f));
        Hostile.Play("chime", 0.6f, 1.2f);
        Hostile.Shake(0.25f);
        yield return new WaitForSeconds(0.2f);
    }

    // 개발자: 임시 더미 떨어뜨림 → 줄로 이어짐
    IEnumerator DevDummies(int count)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.FloatText(transform.position + Vector3.up * 2.4f, Loc.T("나중에 바꿀게!"), Dev, 6f, 0f, 1.4f);
        Vector3 center = pl.transform.position;
        Vector3[] spots = new Vector3[count];
        float start = Random.Range(0f, 360f);
        float warn = DevTele(1f);
        for (int i = 0; i < count; i++)
        {
            Vector2 d = Rotate(Vector2.right, start + 360f / count * i);
            spots[i] = Hostile.ClampArena(center + (Vector3)(d * Random.Range(4f, 6.5f)));
            if (Hostile.IsWall(spots[i])) spots[i] = Hostile.ClampArena(center + (Vector3)(d * 4f));
            Hostile.Circle(spots[i], 1.5f, warn, Dev);
            Fx.Spawn("fx_target_rune", spots[i], 3f, Dev, 12f);
        }
        yield return new WaitForSeconds(warn);
        if (!Alive) yield break;
        for (int i = 0; i < count; i++)
        {
            Fx.Spawn("fx_shock", spots[i], 3.2f, Dev, 20f);
            Fx.Spawn(Fx.Frames("fx_puppet_swing").Length > 0 ? "fx_puppet_swing" : "fx_explosion", spots[i], 3f, Dev, 20f);
            Hostile.HitCircle(spots[i], 1.5f, Capped(12f * Power) * DevPow);
        }
        Hostile.Play("thump", 0.5f, 0.8f);
        // 1초 뒤 더미끼리 줄로 이어짐 (예고선 → 칼날 0.5초)
        yield return new WaitForSeconds(1f);
        if (!Alive) yield break;
        float tele = DevTele(0.8f);
        for (int i = 0; i < count; i++) Hostile.Line(spots[i], spots[(i + 1) % count], 1f, tele, Dev);
        yield return new WaitForSeconds(tele);
        for (float t = 0f; t < 0.5f && Alive; t += 0.1f)
        {
            PlayerController now = Hostile.Player;
            if (now != null)
                for (int i = 0; i < count; i++)
                    if (Hostile.HurtToSegment(now, spots[i], spots[(i + 1) % count]) < 0.55f) { now.TryHit(Capped(12f * Power) * DevPow); break; }
            yield return new WaitForSeconds(0.1f);
        }
        Hostile.Play("whoosh", 0.5f, 1.4f);
    }
}
