using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공사 반장 진화 무기 궁극기 (2.0.0 · 필살기에 덧붙음) — 버킷 내려찍기 · 진동 다짐 · 쌍 쇠공
//   표시 % = 코드 × KitPower 0.8 (KitUlts 글과 맞춤: 128 / 72 / 56%)
public partial class SpecialAbilities
{
    IEnumerator ForemanKitUlt(int id, float D, Vector3 me, Vector3 mouse, Vector2 dir, bool evo)
    {
        switch (id)
        {
            case KitBucket:
                {
                    // 버킷 내려찍기: 마우스 둘레를 큰 버킷이 세 번 내려찍음 (공격력 160%씩 × KitPower)
                    PlayerMotion.Of(player).Cast(0.9f);
                    Vector3 at = KitClamp(mouse);
                    float r = evo ? 6f : 5f;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 p = i == 0 ? at : KitClamp(at + (Vector3)(Random.insideUnitCircle * r * 0.5f));
                        Friendly.Circle(p, r, 0.35f, new Color(1f, 0.88f, 0.3f, 0.8f));
                        yield return new WaitForSeconds(0.35f);
                        DamageCircle(p, r, D * 1.6f, 3f);
                        Fx.Spawn(Fx.Frames("fx_bucket").Length > 0 ? "fx_bucket" : "fx_explosion", p, r * 1.6f, new Color(1f, 0.9f, 0.4f), 16f);
                        Fx.Spawn("fx_shock", p, r * 2f, new Color(1f, 0.9f, 0.5f, 0.7f), 20f);
                        fx.Play("boom", 0.4f, 0.9f);
                        Hostile.Shake(0.15f);
                    }
                    break;
                }
            case KitJackhammer:
                {
                    // 진동 다짐: 내 둘레 6칸에 땅울림이 네 번 퍼져 적을 기절시킴 (공격력 90%씩 · 0.4초 기절)
                    PlayerMotion.Of(player).Cast(1f);
                    float r = evo ? 7f : 6f;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 c = player.transform.position;
                        foreach (EnermyController e in EnemiesIn(c, r)) if (e.holder == null) e.Slow(0f, 0.4f);
                        DamageCircle(c, r, D * 0.9f, 1.5f);
                        Fx.Spawn(Fx.Frames("fx_quake").Length > 0 ? "fx_quake" : "fx_shock", c, r * 2.2f, Color.white, 16f);
                        fx.Play("thump", 0.5f, 0.7f + 0.08f * i);
                        Hostile.Shake(0.12f);
                        yield return new WaitForSeconds(0.35f);
                    }
                    break;
                }
            case KitWreckBall:
                {
                    // 쌍 쇠공: 철거 공고 쇠공이 하나 더 떨어져 25° 옆으로 튕김 (피해 70%)
                    PlayerMotion.Of(player).Cast(0.6f);
                    Vector2 d2 = Quaternion.Euler(0f, 0f, 25f) * dir;
                    float reach = Mathf.Min(14f, Vector2.Distance(me, mouse));
                    Vector3 land = KitClamp(me + (Vector3)(d2 * Mathf.Max(3f, reach)));
                    Friendly.Circle(land, 2.2f, 0.35f, new Color(1f, 0.88f, 0.3f, 0.8f));
                    yield return new WaitForSeconds(0.35f);
                    DamageCircle(land, 2.2f, D * 0.7f, 5f);
                    Fx.Spawn("fx_explosion", land, 5f, new Color(1f, 0.85f, 0.4f), 18f);
                    fx.Play("boom", 0.5f, 0.9f);
                    Vector3 prev = land;
                    for (int k = 1; k <= 3; k++)
                    {
                        Vector3 to = KitClamp(land + (Vector3)(d2 * 4f * k));
                        DamageLine(prev, to, 0.8f, D * 0.7f * 0.3f, 1f);
                        yield return new WaitForSeconds(0.28f);
                        DamageCircle(to, 1.8f, D * 0.7f * 0.6f, 2f);
                        Fx.Spawn("fx_explosion", to, 3.6f, new Color(1f, 0.85f, 0.4f), 18f);
                        fx.Play("thump", 0.4f, 0.8f);
                        prev = to;
                    }
                    break;
                }
        }
    }
}
