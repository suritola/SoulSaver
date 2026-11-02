using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 씨름꾼 진화 무기 궁극기 (2.0.0 · 필살기에 덧붙음) — 들돌 비 · 황소 떼 · 줄다리기
//   표시 % = 코드 × KitPower 0.8 (KitUlts 글과 맞춤: 128 / 192 / 192%)
public partial class SpecialAbilities
{
    IEnumerator WrestlerKitUlt(int id, float D, Vector3 me, Vector3 mouse, Vector2 dir, bool evo)
    {
        switch (id)
        {
            case KitLiftStone:
                {
                    // 들돌 비: 마우스 둘레에 들돌 여덟 개가 차례로 떨어져 터짐 (공격력 160%씩 × KitPower)
                    PlayerMotion.Of(player).Cast(0.9f);
                    Vector3 at = KitClamp(mouse);
                    float spread = evo ? 7f : 6f;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 p = KitClamp(at + (Vector3)(Random.insideUnitCircle * spread));
                        Friendly.Circle(p, 1.7f, 0.4f, new Color(0.95f, 0.75f, 0.45f, 0.8f));
                        StartCoroutine(StoneDrop(p, D * 1.6f));
                        yield return new WaitForSeconds(0.12f);
                    }
                    break;
                }
            case KitCowbell:
                {
                    // 황소 떼: 황소 다섯 마리가 마우스 쪽으로 달려가며 닿는 적을 날려 버림 (공격력 240%씩 · 밀침 6)
                    PlayerMotion.Of(player).Cast(0.7f);
                    float len = evo ? 22f : 18f;
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 d = Quaternion.Euler(0f, 0f, (i - 2) * 8f) * dir;
                        StartCoroutine(BullRun(player.transform.position + (Vector3)(d * 1.2f), d, len, D * 2.4f));
                        fx.Play("thump", 0.35f, 0.8f + 0.05f * i);
                        yield return new WaitForSeconds(0.12f);
                    }
                    yield return new WaitForSeconds(0.5f);
                    break;
                }
            case KitRope:
                {
                    // 줄다리기: 마우스 쪽으로 밧줄을 던져 한 줄의 적을 꿰어 당긴 뒤 내 등 뒤로 한꺼번에 넘겨 메침 (공격력 240%)
                    PlayerMotion.Of(player).Throw(0.4f, dir);
                    float len = evo ? 17f : 14f;
                    Vector3 a = player.MuzzlePosition, b = a + (Vector3)(dir * len);
                    Fx.Beam(a, b, 0.5f, new Color(0.85f, 0.7f, 0.4f, 0.9f), 0.35f, 18);
                    fx.Play("whoosh", 0.7f, 0.7f);
                    List<Transform> caught = new List<Transform>();
                    using (var ovl = PhysHits.Circle((a + b) * 0.5f, len * 0.5f + 1.5f)) foreach (Collider2D c in ovl)
                    {
                        if (c == null || !c.CompareTag("enermy")) continue;
                        if (!c.TryGetComponent(out EnermyController e) || e.IsDead || e.holder != null) continue;
                        if (Hostile.DistanceToSegment(c.transform.position, a, b) > 1.6f) continue;
                        caught.Add(c.transform);
                    }
                    yield return new WaitForSeconds(0.25f);
                    Vector3 behind = player.transform.position - (Vector3)(dir * 2.5f);
                    int n = 0;
                    foreach (Transform t in caught)
                    {
                        if (t == null) continue;
                        Vector3 spot = KitClamp(behind + (Vector3)(Random.insideUnitCircle * 1.6f));
                        if (!Hostile.IsWall(spot)) t.position = spot;
                        n++;
                    }
                    yield return new WaitForSeconds(0.15f);
                    foreach (Transform t in caught)
                    {
                        if (t == null) continue;
                        Specials.Damage(t.gameObject, D * 2.4f, Vector3.down, 1f);
                        Fx.Spawn("fx_shock", t.position, 2.4f, new Color(0.9f, 0.75f, 0.45f, 0.8f), 20f);
                    }
                    if (n > 0) { Hostile.Shake(0.2f); fx.Play("thump", 0.6f, 0.7f); }
                    break;
                }
        }
    }

    IEnumerator StoneDrop(Vector3 p, float dmg)
    {
        FxAnim stone = Fx.Play("fx_liftstone", p + Vector3.up * 7f, 1.8f, Color.white, 10f, 0f, 20, true, 0.45f);
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            if (stone != null) stone.transform.position = Vector3.Lerp(p + Vector3.up * 7f, p, t / 0.35f);
            yield return null;
        }
        if (stone != null) Destroy(stone.gameObject);
        DamageCircle(p, 1.7f, dmg, 2f);
        Fx.Spawn("fx_explosion", p, 3.4f, new Color(0.9f, 0.75f, 0.5f), 20f);
        fx.Play("boom", 0.3f, 1.2f);
    }

    IEnumerator BullRun(Vector3 from, Vector2 dir, float len, float dmg)
    {
        FxAnim bull = Fx.Play("fx_bullrun", from, 2.6f, Color.white, 12f, 0f, 20, true, len / 32f + 0.3f);
        HashSet<int> hit = new HashSet<int>();
        Vector3 p = from;
        for (float d = 0f; d < len; d += 32f * Time.deltaTime)
        {
            p = from + (Vector3)(dir * d);
            if (Hostile.IsWall(p)) break;
            if (bull != null) bull.transform.position = p;
            using (var ovl = PhysHits.Circle(p, 1.4f)) foreach (Collider2D c in ovl)
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss")) || !hit.Add(c.gameObject.GetInstanceID())) continue;
                if (c.TryGetComponent(out EnermyController e) && e.holder != null) continue;
                Specials.Damage(c.gameObject, dmg, dir, 6f);
            }
            yield return null;
        }
        if (bull != null) Destroy(bull.gameObject);
    }
}
