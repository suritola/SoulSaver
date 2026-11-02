using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 사진사 진화 무기 궁극기 (2.0.0 · 필살기 「파노라마」에 덧붙음) — 불꽃 플래시 · 감시 카메라 · 상영회
//   표시 % = 코드 × KitPower 0.8 (KitUlts 글과 맞춤: 160 / 60 / 50%)
public partial class SpecialAbilities
{
    IEnumerator PhotographerKitUlt(int id, float D, Vector3 me, Vector3 mouse, Vector2 dir, bool evo)
    {
        Color flash = new Color(0.78f, 0.83f, 1f);
        switch (id)
        {
            case KitBulb:
                {
                    // 불꽃 플래시: 내 둘레에 전구 여덟 개가 차례로 터짐 (공격력 160%씩)
                    PlayerMotion.Of(player).Cast(0.8f);
                    int n = evo ? 10 : 8;
                    float r = evo ? 4.5f : 3.8f;
                    float a0 = Random.value * 360f;
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 c = player != null ? player.transform.position : me;
                        float a = (a0 + i * 360f / n) * Mathf.Deg2Rad;
                        Vector3 p = KitClamp(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r);
                        DamageCircle(p, 2.2f, D * 2f, 2f);
                        Fx.Spawn("fx_bulb", p, 4.2f, flash, 18f, 0f, 22);
                        fx.Play("pop", 0.35f, 1.2f + 0.05f * i);
                        yield return new WaitForSeconds(0.08f);
                    }
                    Hostile.Shake(0.15f);
                    break;
                }
            case KitTripodCam:
                {
                    // 감시 카메라: 카메라 네 대가 마우스 둘레에 서서 4초 동안 0.6초마다 찍음 (공격력 60%씩)
                    Vector3 at = KitClamp(mouse);
                    int cams = evo ? 5 : 4;
                    List<GameObject> gos = new List<GameObject>();
                    Sprite[] f = Fx.Frames("fx_tripodcam");
                    for (int i = 0; i < cams; i++)
                    {
                        float a = (i * 360f / cams + 45f) * Mathf.Deg2Rad;
                        Vector3 p = KitClamp(at + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 3.2f);
                        GameObject go = new GameObject("PhUltCam");
                        go.transform.position = p;
                        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                        if (f != null && f.Length > 0) { sr.sprite = f[0]; go.transform.localScale = Vector3.one * (1.6f / Mathf.Max(0.1f, f[0].bounds.size.y)); }
                        sr.sortingLayerID = SortingLayer.NameToID("Character");
                        sr.sortingOrder = -Mathf.RoundToInt(p.y * 10f);
                        gos.Add(go);
                    }
                    for (float t = 0f; t < 4f; t += 0.6f)
                    {
                        foreach (GameObject go in gos)
                        {
                            if (go == null) continue;
                            Vector3 target = Vector3.Lerp(go.transform.position, at, 0.75f);
                            using (var ovl = PhysHits.Box(target, new Vector2(3.6f, 2.7f), 0f)) foreach (Collider2D col in ovl)
                                if (col != null && (col.CompareTag("enermy") || col.CompareTag("boss"))) Specials.Damage(col.gameObject, D * 0.75f, Vector3.zero, 0f);
                            Fx.Spawn("fx_flashframe", target, 2.7f, new Color(1f, 1f, 1f, 0.8f), 24f, 0f, 20);
                        }
                        fx.Play("pop", 0.3f, 1.6f);
                        yield return new WaitForSeconds(0.6f);
                    }
                    foreach (GameObject go in gos) if (go != null) Destroy(go);
                    break;
                }
            case KitProjector:
                {
                    // 상영회: 마우스 자리에 4초 동안 빛 기둥 화면이 서서 안의 적을 0.3초마다 벰 (공격력 50%)
                    Vector3 at = KitClamp(mouse);
                    Vector2 size = evo ? new Vector2(5f, 6f) : new Vector2(4.2f, 5f);
                    FxAnim screen = Fx.Play("fx_flashframe", at, size.y, new Color(1f, 1f, 1f, 0.45f), 8f, 0f, 19, true, 4.2f);
                    for (float t = 0f; t < 4f; t += 0.3f)
                    {
                        using (var ovl = PhysHits.Box(at, size, 0f)) foreach (Collider2D col in ovl)
                            if (col != null && (col.CompareTag("enermy") || col.CompareTag("boss"))) Specials.Damage(col.gameObject, D * 0.625f, Vector3.zero, 0f);
                        fx.Play("hiss", 0.15f, 1.5f);
                        yield return new WaitForSeconds(0.3f);
                    }
                    if (screen != null) Destroy(screen.gameObject);
                    break;
                }
        }
    }
}
