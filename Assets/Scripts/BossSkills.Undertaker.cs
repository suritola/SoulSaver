using System.Collections;
using UnityEngine;

// 거울의 군주 · 개발자가 흉내 내는 장의사 (2.0.0) — hero-undertaker.md 12-4
//   평타 「함몰」(MirrorStrike 의 case Undertaker): 플레이어의 진행 방향 앞 2 · 4칸과 발밑에 함몰 예고 원 셋(반지름 1.3 · 0.6초 경고) → 원 안이면 피해 + 0.4초 이동 50% 느려짐 (묶지 않음 — 멈추면 회피가 무너짐)
//   필살기 「거울 장례」(UltFuneral): 종 「댕」 → 플레이어 둘레 5칸에 묘비 6개가 한쪽을 비운 반원으로 솟음(보스 것이라 플레이어도 막힘 · 2초 뒤 저절로 무너짐) + 0.8초 뒤 발밑 큰 함몰 원(반지름 2.2) — 빈 쪽으로 빠져나가는 것이 답
//   개발자 순환 「개발자의 장례」: 화면 곳곳 함몰 원 12개(강하면 16)가 0.15초 간격으로 차례로 + 마지막에 종 한 번 + 개발자 둘레 임시 묘비 4개(3초)
public partial class BossSkills
{
    // 평타: 앞 2 · 4칸과 발밑에 함몰
    IEnumerator MirrorSinkholes()
    {
        PlayerController p = Hostile.Player;
        if (p == null) yield break;
        Vector3 p0 = p.transform.position;
        yield return new WaitForSeconds(0.1f);
        if (!Alive) yield break;
        p = Hostile.Player;
        if (p == null) yield break;
        Vector3 p1 = p.transform.position;
        Vector2 way = (p1 - p0) / 0.1f;
        Vector2 dir = way.sqrMagnitude > 1f ? way.normalized : ((Vector2)(p1 - transform.position)).normalized;
        Vector3[] spots = new Vector3[3];
        spots[0] = p1;
        spots[1] = Hostile.ClampArena(p1 + (Vector3)(dir * 2f));
        spots[2] = Hostile.ClampArena(p1 + (Vector3)(dir * 4f));
        for (int i = 0; i < 3; i++)
        {
            if (Hostile.IsWall(spots[i])) spots[i] = p1;
            Hostile.Circle(spots[i], 1.3f, 0.6f, Mirror);
            Fx.Spawn("fx_dirt", spots[i], 1.6f, new Color(Mirror.r, Mirror.g, Mirror.b, 0.6f), 16f, 0f, 12);
        }
        yield return new WaitForSeconds(0.6f);
        if (!Alive) yield break;
        for (int i = 0; i < 3; i++)
        {
            Fx.Spawn("fx_hole", spots[i], 2.8f, Color.white, 8f, 0f, 9);
            Fx.Spawn("fx_bury", spots[i], 3f, Mirror, 12f, 0f, 14);
            if (Hostile.HitCircle(spots[i], 1.3f, Capped(14f * Power)))
            {
                PlayerController pl = Hostile.Player;
                if (pl != null) pl.Slow(0.5f, 0.4f);
            }
        }
        Hostile.Play("thump", 0.5f, 0.9f);
        yield return new WaitForSeconds(0.25f);
    }

    // 필살기 「거울 장례」: 한쪽을 비운 반원 묘비 + 발밑 큰 함몰
    IEnumerator UltFuneral(bool strong)
    {
        Color col = boss != null && boss.developer ? Dev : Mirror;
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        Vector3 center = pl.transform.position;
        Hostile.Play("chime", 0.7f, 0.45f);
        Hostile.Play("gong", 0.5f, 0.8f);
        Fx.Spawn("fx_bell_ring", transform.position, 8f, col, 12f, 0f, 20);
        float open = Random.Range(0f, 360f);
        int graves = strong ? 7 : 6;
        for (int i = 0; i < graves; i++)
        {
            // 한쪽(open ± 60°)을 비우고 나머지 240° 에 고르게
            float a = open + 60f + 240f * (i / (float)(graves - 1));
            Vector3 at = Hostile.ClampArena(center + (Vector3)(Rotate(Vector2.right, a) * 5f));
            Fx.Spawn("fx_grave_rise", at, 2.2f, col, 10f, 0f, 14);
            UndertakerGrave g = UndertakerGrave.Make(at, false, false, true, 2f, 6);
            if (g != null && g.sr != null) g.sr.color = new Color(col.r, col.g, col.b, 0.95f);
        }
        if (boss != null) boss.noContactUntil = Time.time + 1.6f;
        // 0.8초 뒤 발밑 큰 함몰 (이 사이 빈 쪽으로 빠져나가면 피함)
        Hostile.Circle(center, 2.2f, 0.8f, col);
        yield return new WaitForSeconds(0.8f);
        if (!Alive) yield break;
        Fx.Spawn("fx_hole", center, 5f, Color.white, 8f, 0f, 9);
        Fx.Spawn("fx_bury", center, 5.4f, col, 12f, 0f, 14);
        UltHit(UltInCircle(center, 2.2f));
        Hostile.Play("thump", 0.8f, 0.5f);
        Hostile.Shake(0.3f);
        yield return new WaitForSeconds(0.6f);
    }

    // 개발자: 화면 곳곳 함몰 원 12개(강하면 16)가 차례로 + 종 한 번 + 개발자 둘레 임시 묘비 4개
    IEnumerator DevFuneral(int count)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.FloatText(transform.position + Vector3.up * 2.4f, Loc.T("장례 일정이 잡혔습니다."), Dev, 6f, 0f, 1.4f);
        float warn = DevTele(0.9f);
        Vector3[] spots = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 around = i % 3 == 0 ? pl.transform.position : transform.position;
            spots[i] = Hostile.ClampArena(around + (Vector3)(Random.insideUnitCircle * Random.Range(3f, 9f)));
            if (Hostile.IsWall(spots[i])) spots[i] = pl.transform.position;
            Hostile.Circle(spots[i], 1.5f, warn + 0.15f * i, Dev);
        }
        yield return new WaitForSeconds(warn);
        for (int i = 0; i < count && Alive; i++)
        {
            Fx.Spawn("fx_hole", spots[i], 3.2f, Color.white, 8f, 0f, 9);
            Fx.Spawn("fx_bury", spots[i], 3.4f, Dev, 12f, 0f, 14);
            Hostile.HitCircle(spots[i], 1.5f, Capped(12f * Power) * DevPow);
            Hostile.Play("thump", 0.35f, 0.7f + 0.03f * (i % 6));
            yield return new WaitForSeconds(0.15f);
        }
        if (!Alive) yield break;
        // 마지막 종 한 번 + 개발자 둘레 임시 묘비 4개 (3초)
        Hostile.Play("chime", 0.7f, 0.5f);
        Fx.Spawn("fx_bell_ring", transform.position, 9f, Dev, 12f, 0f, 20);
        for (int i = 0; i < 4; i++)
        {
            Vector3 at = Hostile.ClampArena(transform.position + (Vector3)(Rotate(Vector2.right, 45f + 90f * i) * 2.6f));
            UndertakerGrave.Make(at, false, true, true, 3f, 4);
        }
        yield return new WaitForSeconds(0.2f);
    }
}
