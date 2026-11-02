using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 장의사 진화 무기 궁극기 (2.0.0 · 필살기 「장례」에 덧붙음) — 까마귀 떼 · 저녁 종 · 석관 봉인
//   표시 % = 코드 × KitPower 0.8 (KitUlts 글과 맞춤: 120 / 100 / 350%)
public partial class SpecialAbilities
{
    IEnumerator UndertakerKitUlt(int id, float D, Vector3 me, Vector3 mouse, Vector2 dir, bool evo)
    {
        CharacterKit k = Kit;
        switch (id)
        {
            case KitCrow:
                {
                    // 까마귀 떼: 화면 안 적 열두 마리에게 차례로 내리꽂힘 (공격력 120%씩 · 보스는 네 번)
                    PlayerMotion.Of(player).Cast(0.8f);
                    List<Transform> targets = NearestEnemies(me, 14f, evo ? 16 : 12);
                    for (int i = 0; i < targets.Count; i++)
                    {
                        Transform t = targets[i];
                        if (t == null) continue;
                        bool boss = t.CompareTag("boss") || (t.TryGetComponent(out EnermyController e) && e.IsMidBoss);
                        int hits = boss ? 4 : 1;
                        Fx.SpawnFacing(Fx.Frames("fx_crow").Length > 0 ? "fx_crow" : "fx_sparkle", t.position, 2.2f, t.position.x < me.x, Color.white, 16f);
                        for (int h = 0; h < hits; h++) Specials.Damage(t.gameObject, D * 1.5f, (t.position - me).normalized, 0.5f);
                        fx.Play("pop", 0.35f, 0.8f + 0.04f * i);
                        yield return new WaitForSeconds(0.08f);
                    }
                    break;
                }
            case KitBell:
                {
                    // 저녁 종: 0.5초 간격으로 종 세 번 — 둘레 12칸 공격력 100% · 0.5초 멈춤 (보스 제외)
                    float r = evo ? 14f : 12f;
                    for (int n = 0; n < 3; n++)
                    {
                        Vector3 c = player != null ? player.transform.position : me;
                        using (var ovl = PhysHits.Circle(c, r)) foreach (Collider2D col in ovl)
                        {
                            if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                            Specials.Damage(col.gameObject, D * 1.25f, (col.transform.position - c).normalized, 0.5f);
                            if (col != null && col.CompareTag("enermy") && col.TryGetComponent(out EnermyController e) && !e.IsMidBoss && e.holder == null) e.Slow(0f, 0.5f);
                        }
                        Fx.Spawn("fx_bell_ring", c, r * 1.6f, new Color(0.725f, 0.796f, 0.894f), 14f, 0f, 20);
                        fx.Play("chime", 0.6f, 0.55f + 0.1f * n);
                        Hostile.Shake(0.12f);
                        yield return new WaitForSeconds(0.5f);
                    }
                    break;
                }
            case KitSarcophagus:
                {
                    // 석관 봉인: 커서 자리에 큰 석관이 떨어져 반지름 3칸 공격력 350% + 내구 20 · 10초짜리 큰 석관
                    PlayerMotion.Of(player).Cast(0.5f);
                    Vector3 at = KitClamp(mouse);
                    float r = evo ? 3.6f : 3f;
                    Friendly.Circle(at, r, 0.4f, new Color(0.725f, 0.796f, 0.894f, 0.8f));
                    yield return new WaitForSeconds(0.4f);
                    DamageCircle(at, r, D * 4.375f, 4f);
                    Fx.Spawn("fx_shock", at, r * 2f, new Color(0.8f, 0.85f, 0.95f, 0.8f), 20f);
                    Fx.Spawn(Fx.Frames("fx_bury").Length > 0 ? "fx_bury" : "fx_explosion", at, r * 1.6f, Color.white, 14f);
                    fx.Play("shatter", 0.6f, 0.6f);
                    Hostile.Shake(0.25f);
                    if (k != null) k.UtSpawnGrave(new Vector2(at.x, at.y), true, true, 10f, 20);
                    break;
                }
        }
    }
}
