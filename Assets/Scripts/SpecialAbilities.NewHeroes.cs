using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 1.0.2 새 캐릭터의 진화 무기 궁극기 (필살기에 덧붙음 · SpecialAbilities.KitUlts 의 나머지)
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
                        Fx.Links("fx_surveylink", tri[i], tri[(i + 1) % 3], 0.45f, Color.white, 0.5f, 16);      // 놋쇠 측량 사슬 (예전 단색 선 Fx.Beam)
                        Fx.Spawn("fx_surveystake", tri[i], 1.6f, Color.white, 10f, 0f, 17, true, 0.6f);     // 꼭짓점 측량 말뚝 · 깃발
                        DamageLine(tri[i], tri[(i + 1) % 3], 0.9f, D * 1.1f, 1f);
                    }
                    using (var ovl = PhysHits.Circle(c, r)) foreach (Collider2D col in ovl)
                    {
                        if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                        if (!InTriangle(col.transform.position, tri[0], tri[1], tri[2])) continue;
                        Specials.Damage(col.gameObject, D * 2.2f, (col.transform.position - c).normalized, 1.5f);
                    }
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
                    // 그림: 깨지는 잉크병 → 말려 오는 먹물 파도 마루(앞줄을 따라감) · 뒤로 튀는 먹 (예전 원 웅덩이 fx_puddle 을 뿌림)
                    float waveRot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    Fx.Spawn("fx_inkshatter", start + (Vector3)(dir * 1.2f), 3f, Color.white, 16f);
                    FxAnim crest = Fx.Play("fx_inkwave", start, width * 2f, Color.white, 14f, waveRot, 14, true, 0.6f);
                    Vector2 side = new Vector2(-dir.y, dir.x);
                    for (float t = 0f; t < 0.5f; t += Time.deltaTime)
                    {
                        Vector3 front = KitClamp(start + (Vector3)(dir * len * (t / 0.5f)));
                        if (crest != null) crest.transform.position = front;
                        if (Random.value < 0.5f) Fx.Spawn("fx_inkspatter", front - (Vector3)(dir * 1.2f) + (Vector3)(side * Random.Range(-width, width)), 1.4f, Color.white, 16f, Random.Range(0f, 360f));
                        using (var ovl = PhysHits.Circle(front, width)) foreach (Collider2D col in ovl)
                        {
                            if ((!col.CompareTag("enermy") && !col.CompareTag("boss")) || !hit.Add(col)) continue;
                            Specials.Damage(col.gameObject, D * 3f, dir, 5f);
                            if (col.TryGetComponent(out EnermyController e)) e.Slow(0.4f, 2.5f);
                        }
                        yield return null;
                    }
                    if (crest != null) Destroy(crest.gameObject);
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
                    for (int i = 0; i < n; i++) globes.Add(Fx.Play("fx_globe", player.transform.position, 1.6f, Color.white, 12f, 0f, 20, true, life));      // 도는 지구본 (예전 파란 구슬 fx_orb)
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

            // ---------------- 우체부
            case KitPigeon:
                {
                    // 비둘기 떼: 스무 마리가 마우스 쪽으로 퍼지며 날아감
                    PlayerMotion.Of(player).Cast(0.6f);
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 d = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f)) * dir;
                        Bullet b;
                        using (WeakHit.AimScope(1.75f)) b = KitProjectile(player.MuzzlePosition, d, D * 0.9f, 2, 26f, "fx_falcon", 1.1f, new Color(0.92f, 0.92f, 1f), false);     // 빈틈: 커서 쪽으로 곧게 (±25° 라 CreateBullet 의 25° 규칙 안 · yield 앞에서 닫힘)
                        if (b != null) b.lifetime = (evo ? 20f : 16f) / 26f;
                        if (i % 4 == 0) fx.Play("pop", 0.3f, 1.8f);
                        yield return new WaitForSeconds(0.05f);
                    }
                    break;
                }
            case KitParcel:
                {
                    // 택배 폭격: 상자 열 개
                    Vector3 at = KitClamp(mouse);
                    float r = evo ? 7f : 6f;
                    for (int i = 0; i < 10; i++)
                    {
                        Vector3 p = KitClamp(at + (Vector3)(Random.insideUnitCircle * r));
                        Friendly.Circle(p, 1.8f, 0.35f, new Color(0.9f, 0.7f, 0.45f, 0.8f));
                        StartCoroutine(ParcelDrop(p, D * 1.8f));
                        yield return new WaitForSeconds(0.1f);
                    }
                    break;
                }
            case KitPostbox:
                if (Kit != null) Kit.Postboxes(5, 4f);
                fx.Play("chime", 0.6f, 1.3f);
                break;

            // ---------------- 유령
            case KitSoulChain:
                {
                    // 영혼 사슬: 둘레 적을 묶고 0.5초마다
                    float r = evo ? 10f : 8f;
                    List<Transform> bound = NearestEnemies(player.transform.position, r, 30);
                    for (int k = 0; k < 6; k++)
                    {
                        Vector3 c = player.transform.position;
                        foreach (Transform t in bound)
                        {
                            if (t == null) continue;
                            if (k == 0) Fx.Bolt(c, t.position, 0.5f, new Color(0.75f, 0.95f, 1f), 0.3f);
                            Specials.Damage(t.gameObject, D * 0.7f, Vector3.zero, 0f);
                            if (t != null && t.TryGetComponent(out EnermyController e)) e.Slow(0.5f, 0.6f);
                        }
                        fx.Play("zap", 0.3f, 0.8f);
                        yield return new WaitForSeconds(0.5f);
                    }
                    break;
                }
            case KitIceGhost:
                {
                    // 절대 영도: 둘레 모두 얼림
                    float r = evo ? 11f : 9f;
                    Vector3 c = player.transform.position;
                    using (var ovl = PhysHits.Circle(c, r)) foreach (Collider2D col in ovl)
                    {
                        if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                        Specials.Damage(col.gameObject, D * 3f, (col.transform.position - c).normalized, 0f);
                        if (col != null && col.TryGetComponent(out EnermyController e)) e.Slow(0f, 2f);
                    }
                    Fx.Spawn("fx_shock", c, r * 2.2f, new Color(0.7f, 0.95f, 1f, 0.85f), 16f);
                    Fx.Spawn("fx_cloud", c, r * 2f, new Color(0.85f, 0.97f, 1f, 0.6f), 10f);
                    fx.Play("shatter", 0.9f, 0.6f);
                    Hostile.Shake(0.25f);
                    break;
                }
            case KitShadowGhost:
                {
                    // 그림자 군단: 그림자 넷이 둘레에서 손길
                    List<FxAnim> shades = new List<FxAnim>();
                    for (int i = 0; i < 4; i++) shades.Add(Fx.Play("fx_stealth", player.transform.position, 2.4f, new Color(0.4f, 0.5f, 0.8f, 0.8f), 10f, 0f, 18, true, 3f));
                    for (float t = 0f; t < 3f; t += 0.4f)
                    {
                        Vector3 c = player.transform.position;
                        for (int i = 0; i < 4; i++)
                        {
                            float a = (t * 90f + 90f * i) * Mathf.Deg2Rad;
                            Vector3 sp = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 3f;
                            if (shades[i] != null) shades[i].transform.position = sp;
                            Transform target = Specials.NearestEnemy(sp, 6f);
                            if (target == null) continue;
                            Fx.Beam(sp, target.position, 0.6f, new Color(0.5f, 0.6f, 0.95f, 0.8f), 0.15f);
                            Specials.Damage(target.gameObject, D * 0.8f, (target.position - sp).normalized, 0.5f);
                        }
                        fx.Play("whoosh", 0.3f, 0.8f);
                        yield return new WaitForSeconds(0.4f);
                    }
                    foreach (FxAnim s in shades) if (s != null) Destroy(s.gameObject);
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
                        using (var ovl = PhysHits.Circle(at, r)) foreach (Collider2D col in ovl)
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

    IEnumerator ParcelDrop(Vector3 p, float dmg)
    {
        yield return new WaitForSeconds(0.35f);
        DamageCircle(p, 1.8f, dmg, 2f);
        Fx.Spawn("fx_explosion", p, 4f, new Color(0.95f, 0.7f, 0.4f), 18f);
        fx.Play("boom", 0.35f, 1.4f);
    }

    static bool InTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        float s1 = Cross(b - a, p - a), s2 = Cross(c - b, p - b), s3 = Cross(a - c, p - c);
        bool neg = s1 < 0f || s2 < 0f || s3 < 0f, pos = s1 > 0f || s2 > 0f || s3 > 0f;
        return !(neg && pos);
    }

    static float Cross(Vector3 u, Vector3 v) => u.x * v.y - u.y * v.x;
}
