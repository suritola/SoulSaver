using System.Collections;
using UnityEngine;

// 거울의 군주 · 개발자가 흉내 내는 사진사 (2.0.0) — hero-photographer.md 12절
//   평타 「찰칵」(MirrorStrike 의 case Photographer): 플레이어 둘레에 사각 틀 예고 셋(3.6 x 2.7 · 0.6초) -> 찰칵 -> 틀 안이면 피해 + 0.5초 이동 -40% (종이가 됨 표시). 틀이 고정이라 걸어 나가면 피함
//   필살기 「거울 파노라마」(UltPanorama): 화면 가로 띠 예고 두 줄(위 · 아래 · 1초) -> 커튼이 지나감 + 탄막. 띠 사이가 안전 구역
//   개발자 순환 「스크린샷 담당」(DevPanorama): 같은 띠를 두 번
public partial class BossSkills
{
    static bool InFrame(Vector3 at, PlayerController p, float w, float h)
    {
        Vector2 q = p.HurtCenter;
        return Mathf.Abs(q.x - at.x) <= w * 0.5f + 0.2f && Mathf.Abs(q.y - at.y) <= h * 0.5f + 0.2f;
    }

    // 평타: 둘레 사각 틀 셋
    IEnumerator MirrorShutter()
    {
        PlayerController p = Hostile.Player;
        if (p == null) yield break;
        Vector3 center = p.transform.position;
        Vector3[] spots = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            float a = Random.value * 360f;
            spots[i] = i == 0 ? center : Hostile.ClampArena(center + (Vector3)(Rotate(Vector2.right, a) * Random.Range(2.5f, 4.5f)));
            Hostile.Line(spots[i] + Vector3.left * 0.45f, spots[i] + Vector3.right * 0.45f, 2.7f, 0.6f, Mirror);
            Fx.Spawn("fx_viewfinder", spots[i], 2.8f, new Color(Mirror.r, Mirror.g, Mirror.b, 0.8f), 1f, 0f, 14);
        }
        yield return new WaitForSeconds(0.6f);
        if (!Alive) yield break;
        for (int i = 0; i < 3; i++)
        {
            Fx.Spawn("fx_flashframe", spots[i], 2.8f, Color.white, 24f, 0f, 20);
            PlayerController pl = Hostile.Player;
            if (pl != null && InFrame(spots[i], pl, 3.6f, 2.7f) && pl.TryHit(Capped(15f * Power))) pl.Slow(0.6f, 0.5f);
        }
        Hostile.Play("imp_shutter", 0.6f, 1f);
        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator PanoramaBands(Color col, bool strong, float warn)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        float y = pl.transform.position.y;
        float gap = strong ? 3.2f : 4f;           // 띠 사이 안전 구역
        float thick = 2.6f;
        float[] ys = { y + gap * 0.5f + thick * 0.5f, y - gap * 0.5f - thick * 0.5f };
        Camera cam = Camera.main;
        float cx = cam != null ? cam.transform.position.x : pl.transform.position.x;
        float half = cam != null ? cam.orthographicSize * cam.aspect + 2f : 30f;
        for (int i = 0; i < 2; i++) Hostile.Line(new Vector3(cx - half, ys[i], 0f), new Vector3(cx + half, ys[i], 0f), thick, warn, col);
        yield return new WaitForSeconds(warn);
        if (!Alive) yield break;
        Hostile.Play("imp_curtain", 0.7f, 1f);
        Hostile.Shake(0.25f);
        for (int i = 0; i < 2; i++)
        {
            Fx.Spawn("fx_curtain", new Vector3(cx, ys[i], 0f), thick * 1.2f, new Color(col.r, col.g, col.b, 0.8f), 14f, 0f, 24, false, 0.5f);
            PlayerController p2 = Hostile.Player;
            if (p2 != null && Mathf.Abs(p2.HurtCenter.y - ys[i]) <= thick * 0.5f + 0.2f && p2.TryHit(Capped((boss != null && boss.developer ? 12f : 18f) * Power))) p2.Slow(0.6f, 0.5f);
        }
    }

    IEnumerator UltPanorama(bool strong)
    {
        yield return PanoramaBands(Mirror, strong, 1f);
        if (!Alive) yield break;
        StartCoroutine(MirrorShutterVolley(strong ? 3 : 2));
        yield return new WaitForSeconds(0.6f);
    }

    IEnumerator MirrorShutterVolley(int rounds)
    {
        for (int i = 0; i < rounds && Alive; i++)
        {
            PlayerController p = Hostile.Player;
            if (p == null) yield break;
            Vector2 d = ((Vector2)(p.transform.position - transform.position)).normalized;
            for (int k = -1; k <= 1; k++) Hostile.Shoot(transform.position, Rotate(d, k * 14f), 14f, Capped(12f * Power), 0.5f, Mirror, 1.3f, 3f);
            Hostile.Play("imp_shutter", 0.35f, 1.4f);
            yield return new WaitForSeconds(0.4f);
        }
    }

    // 개발자: 「스크린샷 담당」 — 같은 띠 두 번
    IEnumerator DevPanorama(bool strong)
    {
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.FloatText(transform.position + Vector3.up * 2.4f, Loc.T("스크린샷 저장 중..."), Dev, 6f, 0f, 1.4f);
        float warn = DevTele(1f);
        yield return PanoramaBands(Dev, strong, warn);
        if (!Alive) yield break;
        yield return PanoramaBands(Dev, strong, warn);
    }
}
