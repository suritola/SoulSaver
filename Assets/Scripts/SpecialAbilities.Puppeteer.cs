using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 인형사 진화 무기 궁극기 (2.0.0 · 필살기 「커튼콜」에 덧붙음) — 싹둑 · 실타래 · 행진
//   표시 % = 코드 × KitPower 0.8 (KitUlts 글과 맞춤: 400 / 56 / 56%)
//   싹둑은 커튼콜이 끝나는 순간(KitUltAdd 지연 2.5초) · 실타래 · 행진은 커튼콜이 도는 동안 (CharacterKit.Puppeteer.Ult 의 KitUltAdd)
public partial class SpecialAbilities
{
    IEnumerator PuppeteerKitUlt(int id, float D, Vector3 me, Vector3 mouse, Vector2 dir, bool evo)
    {
        CharacterKit k = Kit;
        Vector3 center = k != null ? new Vector3(k.PupPos.x, k.PupPos.y, 0f) : KitClamp(mouse);
        switch (id)
        {
            case KitScissorDummy:
                {
                    // 싹둑: 커튼콜이 끝날 때 더미 자리에 거대한 가위가 닫힘 (공격력 500% × KitPower)
                    PlayerMotion.Of(player).Cast(0.5f);
                    float r = evo ? 4.5f : 3.5f;
                    center = KitClamp(center);
                    Friendly.Circle(center, r, 0.35f, new Color(0.62f, 0.72f, 1f, 0.8f));
                    yield return new WaitForSeconds(0.35f);
                    DamageCircle(center, r, D * 5f, 4f);
                    Fx.Spawn(Fx.Frames("fx_scissor_snip").Length > 0 ? "fx_scissor_snip" : "fx_explosion", center, r * 2.4f, Color.white, 16f);
                    Fx.Spawn("fx_shock", center, r * 2f, new Color(0.7f, 0.8f, 1f, 0.7f), 20f);
                    fx.Play("shatter", 0.6f, 1.4f);
                    fx.Play("clank", 0.5f, 1.5f);
                    Hostile.Shake(0.25f);
                    break;
                }
            case KitSpoolDummy:
                {
                    // 실타래: 커튼콜 더미끼리 실이 이어져 그물이 되고, 그물 안 적이 느려지며 0.45초마다 벰 (공격력 70% × KitPower)
                    float r = (evo ? 5f : 4.5f);
                    for (float t = 0f; t < 2.2f; t += 0.45f)
                    {
                        if (k == null || !k.PupCurtainActive) break;
                        center = new Vector3(k.PupPos.x, k.PupPos.y, 0f);
                        int n = k.PupExtraCount;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 a = new Vector3(k.PupExtraPos(i).x, k.PupExtraPos(i).y, 0f);
                            Vector3 b = new Vector3(k.PupExtraPos((i + 1) % n).x, k.PupExtraPos((i + 1) % n).y, 0f);
                            Fx.Stretch(Fx.Frames("fx_string_twang").Length > 0 ? "fx_string_twang" : "fx_spark", a, b, 0.7f, new Color(0.75f, 0.85f, 1f, 0.9f), 0.45f, 15, 24f);
                            Fx.Stretch(Fx.Frames("fx_string_twang").Length > 0 ? "fx_string_twang" : "fx_spark", a, center, 0.5f, new Color(0.75f, 0.85f, 1f, 0.6f), 0.45f, 15, 24f);
                        }
                        foreach (EnermyController e in EnemiesIn(center, r)) if (e.holder == null) e.Slow(0.5f, 0.6f);
                        DamageCircle(center, r, D * 0.7f, 0.3f);
                        fx.Play("whoosh", 0.3f, 1.5f);
                        yield return new WaitForSeconds(0.45f);
                    }
                    break;
                }
            case KitWindupDummy:
                {
                    // 행진: 병정 다섯이 한 줄로 커서 쪽으로 행진하며 지나간 길을 벰 (공격력 70% × KitPower · 0.3초마다)
                    PlayerMotion.Of(player).Cast(0.4f);
                    float reach = evo ? 12f : 9f;
                    Vector3 from = me;
                    Vector3 to = KitClamp(from + (Vector3)(dir * reach));
                    float len = Vector3.Distance(from, to);
                    for (float t = 0f; t < 2.1f; t += 0.3f)
                    {
                        for (int s = 0; s < 5; s++)
                        {
                            float f = Mathf.Repeat((t / 2.1f) * len + s * 1.6f, Mathf.Max(1f, len));
                            Vector3 p = from + (to - from).normalized * f;
                            DamageCircle(p, 1.4f, D * 0.7f, 1f);
                            Fx.Spawn(Fx.Frames("fx_puppet_swing").Length > 0 ? "fx_puppet_swing" : "fx_shock", p, 2.4f, new Color(0.62f, 0.72f, 1f), 20f);
                        }
                        fx.Play("clank", 0.25f, 1.1f);
                        yield return new WaitForSeconds(0.3f);
                    }
                    break;
                }
        }
    }
}
