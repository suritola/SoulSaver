using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 1.1.2 새 캐릭터의 진화 무기 궁극기 (필살기에 덧붙음 · SpecialAbilities.KitUlts 의 나머지)
public partial class SpecialAbilities
{
    IEnumerator HeroKitUlt(int id, float D, Vector3 me, Vector3 mouse, Vector2 dir, bool evo)
    {
        switch (id)
        {
            // ---------------- 지도 제작자
            case KitSurveyChain:
                // 삼각 측량: 내 둘레에 삼각형 셋을 차례로 그려 안쪽을 벰
                PlayerMotion.Of(player).Cast(0.9f);
                for (int k = 0; k < 3; k++)
                {
                    Vector3 c = player.transform.position;
                    float r = (evo ? 9f : 8f) * (0.6f + 0.2f * k);
                    float spin = Random.Range(0f, 360f);
                    Vector3[] tri = new Vector3[3];
                    for (int i = 0; i < 3; i++)
                    {
                        float a = (spin + 120f * i) * Mathf.Deg2Rad;
                        tri[i] = KitClamp(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        Fx.Beam(tri[i], tri[(i + 1) % 3], 0.7f, new Color(1f, 0.82f, 0.35f, 0.95f), 0.5f, 16);
                        DamageLine(tri[i], tri[(i + 1) % 3], 0.9f, D * 1.1f, 1f);
                    }
                    foreach (Collider2D col in Physics2D.OverlapCircleAll(c, r))
                    {
                        if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                        if (!InTriangle(col.transform.position, tri[0], tri[1], tri[2])) continue;
                        Specials.Damage(col.gameObject, D * 2.2f, (col.transform.position - c).normalized, 1.5f);
                    }
                    Fx.Spawn("fx_shock", c, r * 2f, new Color(0.45f, 0.6f, 1f, 0.75f), 18f);
                    fx.Play("chime", 0.6f, 1f + 0.15f * k);
                    yield return new WaitForSeconds(0.3f);
                }
                break;
            case KitInkpot:
                {
                    // 먹물 해일: 마우스 쪽으로 거대한 파도가 밀려감
                    PlayerMotion.Of(player).Throw(0.4f, dir);
                    float len = evo ? 20f : 16f, width = evo ? 4f : 3.2f;
                    HashSet<Collider2D> hit = new HashSet<Collider2D>();
                    Vector3 start = me;
                    for (float t = 0f; t < 0.5f; t += Time.deltaTime)
                    {
                        Vector3 front = KitClamp(start + (Vector3)(dir * len * (t / 0.5f)));
                        if (Random.value < 0.7f) Fx.Spawn("fx_puddle", front + (Vector3)(Random.insideUnitCircle * width * 0.5f), 3f, new Color(0.18f, 0.25f, 0.75f, 0.85f), 18f);
                        foreach (Collider2D col in Physics2D.OverlapCircleAll(front, width))
                        {
                            if ((!col.CompareTag("enermy") && !col.CompareTag("boss")) || !hit.Add(col)) continue;
                            Specials.Damage(col.gameObject, D * 3f, dir, 5f);
                            if (col.TryGetComponent(out EnermyController e)) e.Slow(0.4f, 2.5f);
                        }
                        yield return null;
                    }
                    fx.Play("whoosh", 1f, 0.55f);
                    fx.Play("fizz", 0.8f, 0.6f);
                    Hostile.Shake(0.2f);
                    break;
                }
            case KitGlobe:
                {
                    // 자전: 지구본 넷이 내 둘레를 돎
                    int n = 4;
                    float r = evo ? 4f : 3.2f, life = evo ? 4f : 3f;
                    List<FxAnim> globes = new List<FxAnim>();
                    for (int i = 0; i < n; i++) globes.Add(Fx.Play("fx_orb", player.transform.position, 1.6f, new Color(0.45f, 0.75f, 1f), 12f, 0f, 20, true, life));
                    float nextHit = 0f;
                    for (float t = 0f; t < life; t += Time.deltaTime)
                    {
                        Vector3 c = player.transform.position;
                        for (int i = 0; i < n; i++)
                        {
                            float a = (t * 260f + 360f / n * i) * Mathf.Deg2Rad;
                            Vector3 p = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r;
                            if (globes[i] != null) globes[i].transform.position = p;
                            if (t >= nextHit) DamageCircle(p, 1.2f, D * 0.6f, 1.5f);
                        }
                        if (t >= nextHit) nextHit = t + 0.25f;
                        yield return null;
                    }
                    foreach (FxAnim g in globes) if (g != null) Destroy(g.gameObject);
                    break;
                }

            // ---------------- 시계공
            case KitCuckoo:
                {
                    // 뻐꾸기 떼: 열두 마리가 차례로 덮침
                    PlayerMotion.Of(player).Cast(1f);
                    List<Transform> targets = NearestEnemies(player.transform.position, 12f, 12);
                    for (int i = 0; i < 12; i++)
                    {
                        Transform t = targets.Count > 0 ? targets[i % targets.Count] : null;
                        if (t == null) break;
                        Fx.Spawn("fx_falcon", t.position, 2.2f, new Color(0.9f, 0.7f, 0.45f), 20f);
                        Specials.Damage(t.gameObject, D * 1.5f, (t.position - player.transform.position).normalized, 1f);
                        fx.Play("pop", 0.4f, 1.6f + 0.05f * i);
                        yield return new WaitForSeconds(0.08f);
                    }
                    break;
                }
            case KitHourglass:
                {
                    // 모래 폭풍: 마우스 자리에 4초 회오리 (끌어당기며 벰)
                    Vector3 at = KitClamp(mouse);
                    float r = evo ? 7f : 6f;
                    FxAnim storm = Fx.Play("fx_tornado", at, r * 1.8f, new Color(1f, 0.85f, 0.55f, 0.85f), 14f, 0f, 14, true, 4f);
                    for (float t = 0f; t < 4f; t += 0.3f)
                    {
                        foreach (Collider2D col in Physics2D.OverlapCircleAll(at, r))
                        {
                            if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                            if (col.CompareTag("enermy")) col.transform.position = Vector3.MoveTowards(col.transform.position, at, 0.8f);
                            Specials.Damage(col.gameObject, D * 0.6f, Vector3.zero, 0f);
                        }
                        fx.Play("hiss", 0.25f, 0.8f);
                        yield return new WaitForSeconds(0.3f);
                    }
                    if (storm != null) Destroy(storm.gameObject);
                    break;
                }
            case KitGrandfather:
                {
                    // 열두 번의 종: 둘레로 고리 열두 번
                    PlayerMotion.Of(player).Cast(1.6f);
                    float r = evo ? 8f : 7f;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector3 c = player.transform.position;
                        DamageCircle(c, r, D * 0.8f, 1.5f);
                        ShockRing.Spawn(c, 0.5f, r, 0.35f, new Color(1f, 0.8f, 0.4f, 0.85f), 0.22f);
                        fx.Play("chime", 0.45f, 0.5f + 0.04f * i);
                        yield return new WaitForSeconds(0.13f);
                    }
                    Hostile.Shake(0.2f);
                    break;
                }
        }
    }

    static bool InTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        float s1 = Cross(b - a, p - a), s2 = Cross(c - b, p - b), s3 = Cross(a - c, p - c);
        bool neg = s1 < 0f || s2 < 0f || s3 < 0f, pos = s1 > 0f || s2 > 0f || s3 > 0f;
        return !(neg && pos);
    }

    static float Cross(Vector3 u, Vector3 v) => u.x * v.y - u.y * v.x;
}
