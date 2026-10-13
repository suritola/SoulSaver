using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 지도 제작자 고유 스킬 (1.1.2) · 패시브(탐험가의 체력 · 현지 안내 · 미지의 영역)
//   c.legend 범례 · c.contour 등고선 · c.scale 축척 · c.northstar 북극성 · c.margin 여백
//   c.route 지름길 · c.erase 지우개 · c.treasure 보물 지도 · c.rose 나침도 · c.inkstorm 잉크 폭풍
// 진화: ke.world 세계 지도 · ke.relief 입체 지도 · ke.express 직행로 · ke.constellation 별자리
//       ke.blot 먹물 홍수 · ke.bold 대담한 필치 · ke.treasure 황금 항로
public partial class SignatureSkills
{
    static readonly Color MapInk = new Color(0.3f, 0.45f, 1f);
    static readonly Color MapGold = new Color(1f, 0.82f, 0.35f);

    bool IsMap => who == CharacterId.Cartographer;

    // ---------------- 훅 (CharacterKit.Cartographer 가 부름)
    public static float StrokeMul(float len, float maxLen) => Instance != null && Instance.IsMap ? Instance.OnStrokeMul(len, maxLen) : 1f;
    public static float StrokeWidthMul() => Instance != null && Instance.IsMap ? Instance.WidthMul() : 1f;
    public static float InkSpreadMul() => Instance != null && Instance.IsMap && Instance.E("ke.blot") ? 2f : 1f;
    public static float CloseMul() => Instance != null && Instance.IsMap && Instance.E("ke.world") ? 1.5f : 1f;
    public static void Stroke(Vector3 a, Vector3 b, int hits) { if (Instance != null && Instance.IsMap) Instance.OnStroke(a, b, hits); }
    public static void Closed(List<Vector3> poly, Vector3 center, float reach, List<Collider2D> inside) { if (Instance != null && Instance.IsMap) Instance.OnClosed(poly, center, reach, inside); }
    public static void Folded(Vector3 center, Vector3 a, Vector3 b, List<Collider2D> caught, float dmg) { if (Instance != null && Instance.IsMap) Instance.OnFolded(center, a, b, caught, dmg); }

    // ---------------- 상태
    float lastStroke = -99f;
    bool marginReady => Time.time - lastStroke >= 3f;
    int legendStacks;
    float legendUntil;
    // 나침도 (느리게 · 보물): 자리 · 반지름 · 끝나는 때
    readonly List<(Vector3 p, float r, float until)> roses = new List<(Vector3, float, float)>();

    float OnStrokeMul(float len, float maxLen)
    {
        float m = 1f;
        // 축척: 획이 길수록 (최대 길이에서 최대)
        if (L("c.scale") > 0) m *= 1f + V(L("c.scale"), 0.2f, 0.1f) * Mathf.Clamp01(len / Mathf.Max(1f, maxLen));
        // 여백: 3초 쉬면 다음 획이 강하게
        if (L("c.margin") > 0 && (marginReady || E("ke.bold"))) m *= 1f + V(L("c.margin"), 0.4f, 0.2f);
        return m;
    }

    float WidthMul() => (L("c.margin") > 0 && marginReady) || E("ke.bold") ? 2f : 1f;

    void OnStroke(Vector3 a, Vector3 b, int hits)
    {
        if (L("c.margin") > 0 && marginReady && !E("ke.bold"))
        {
            Fx.Spawn("fx_sparkle", b, 1.6f, Tint("c.margin", MapGold), 22f);
            Flair("c.margin", b, 2f, MapGold);
        }
        lastStroke = Time.time;
    }

    void OnClosed(List<Vector3> poly, Vector3 center, float reach, List<Collider2D> inside)
    {
        // 탐험가의 체력 (패시브)
        if (Sp != null && Sp.Has(SpecialAbilities.KitExplorer)) Heal(Sp.IsEvolved(SpecialAbilities.KitExplorer) ? 4f : 2f);
        // 범례: 4초 동안 공격력 + (최대 3중첩)
        if (L("c.legend") > 0)
        {
            legendStacks = Time.time < legendUntil ? Mathf.Min(3, legendStacks + 1) : 1;
            legendUntil = Time.time + 4f;
        }
        // 등고선: 둘레에서 고리 파동 (입체 지도: 세 겹)
        if (L("c.contour") > 0)
        {
            int rings = E("ke.relief") ? 3 : 1;
            StartCoroutine(Contours(center, Mathf.Max(2f, reach), rings, Atk * V(L("c.contour"), 0.6f, 0.2f)));
        }
        // 나침도: 3초 동안 안의 적을 느리게 (황금 항로: 두 배 넓게)
        if (L("c.rose") > 0 || L("c.treasure") > 0)
        {
            float r = Mathf.Max(2f, reach) * (E("ke.treasure") ? 2f : 1f);
            roses.Add((center, r, Time.time + 3f));
            // 1.1.2: 보물 지도만 있어도 표시가 보이게 (예전엔 나침도가 있어야만 그려져, 코인 받는 자리를 알 수 없었음)
            //        나침도는 금빛 · 보물 표시만이면 코인색으로 조금 더 진하게
            Color rc = L("c.rose") > 0 ? new Color(MapGold.r, MapGold.g, MapGold.b, 0.6f) : new Color(1f, 0.9f, 0.25f, 0.7f);
            FxAnim a = Fx.Play("fx_target_rune", center, r * 2f, rc, 10f, 0f, 3, true, 3f, "Background");
            if (a != null) a.spin = L("c.rose") > 0 ? 40f : 20f;
        }
        // 세계 지도: 안쪽 적이 두 번 더 다치고 표식이 주변으로 번짐
        if (E("ke.world"))
            StartCoroutine(WorldEcho(center, Mathf.Max(2f, reach), inside));
    }

    IEnumerator Contours(Vector3 c, float r, int rings, float dmg)
    {
        for (int i = 0; i < rings; i++)
        {
            float rr = r * (1f + 0.45f * i) * Fs("c.contour");
            Circle(c, rr, dmg, 1.5f);
            ShockRing.Spawn(c, rr * 0.6f, rr, 0.35f, Tint("c.contour", MapInk), 0.2f);
            Flair(i == rings - 1 ? "c.contour" : null, c, rr * 0.6f, MapInk);
            Hostile.Play("thump", 0.4f, 1.2f + 0.15f * i);
            yield return new WaitForSeconds(0.25f);
        }
    }

    IEnumerator WorldEcho(Vector3 c, float r, List<Collider2D> inside)
    {
        for (int k = 0; k < 2; k++)
        {
            yield return new WaitForSeconds(0.3f);
            foreach (Collider2D col in inside)
                if (col != null) Deal(col.gameObject, Atk * 1.3f, (col.transform.position - c).normalized, 0.5f);
            Fx.Spawn("fx_shock", c, r * 2.4f, new Color(MapGold.r, MapGold.g, MapGold.b, 0.7f), 20f);
        }
        Circle(c, r * 1.6f, 0f, 0f, col => Mark(col.gameObject, 4f, 1.2f));
        Flair(null, c, r, MapGold);
    }

    void OnFolded(Vector3 center, Vector3 a, Vector3 b, List<Collider2D> caught, float dmg)
    {
        // 북극성: 접힌 선 가운데에 별 (별자리: 셋)
        if (L("c.northstar") > 0)
        {
            int stars = E("ke.constellation") ? 3 : 1;
            for (int i = 0; i < stars; i++)
            {
                Vector3 p = stars == 1 ? center : Vector3.Lerp(a, b, (i + 0.5f) / stars);
                StartCoroutine(NorthStar(p, 4f, V(L("c.northstar"), 0.8f, -0.15f)));
            }
            if (stars > 1) StartCoroutine(StarLines(a, b, 4f));
        }
        // 잉크 폭풍: 접혀 쓰러진 적에게서 잉크가 튐
        if (L("c.inkstorm") > 0)
            foreach (Collider2D c in caught)
            {
                if (c == null) continue;
                EnermyController e = c.GetComponent<EnermyController>();
                if (e == null || !e.IsDead) continue;
                Vector3 p = c.transform.position;
                Circle(p, 2.2f * Fs("c.inkstorm"), Atk * V(L("c.inkstorm"), 0.5f, 0.2f), 1f);
                Fx.Spawn("fx_puddle", p, 3.5f * Fs("c.inkstorm"), Tint("c.inkstorm", new Color(0.2f, 0.28f, 0.8f, 0.9f)), 18f);
                if (E("ke.blot") && Sp != null) Sp.SpawnZone(p, 1.4f, 2.5f, Atk * 0.25f, new Color(0.2f, 0.3f, 0.85f, 0.55f));
            }
    }

    IEnumerator NorthStar(Vector3 p, float life, float every)
    {
        FxAnim star = Fx.Play("fx_sparkle", p + Vector3.up * 1.5f, 2.4f * Fs("c.northstar"), Tint("c.northstar", MapGold), 10f, 0f, 20, true, life);
        Flair("c.northstar", p, 2.5f, MapGold);
        for (float t = 0f; t < life && Alive; t += every)
        {
            Transform target = Specials.NearestEnemy(p, 9f);
            if (target != null)
            {
                Fx.Beam(p + Vector3.up * 1.5f, target.position, 0.35f, MapGold, 0.15f);
                Deal(target.gameObject, Atk * 0.5f, (target.position - p).normalized, 0.3f);
            }
            yield return new WaitForSeconds(Mathf.Max(0.2f, every));
        }
        if (star != null) Destroy(star.gameObject);
    }

    // 별자리: 별끼리 이어진 선이 닿는 적을 벰
    IEnumerator StarLines(Vector3 a, Vector3 b, float life)
    {
        Vector3 up = Vector3.up * 1.5f;
        FxAnim line = Fx.Beam(a + up, b + up, 0.3f, new Color(MapGold.r, MapGold.g, MapGold.b, 0.7f), life);
        for (float t = 0f; t < life && Alive; t += 0.4f)
        {
            foreach (Collider2D c in new List<Collider2D>(Specials.Overlap((a + b) * 0.5f + up, (b - a).magnitude * 0.5f + 0.8f, hits)))
                if (c != null && IsFoe(c) && Hostile.DistanceToSegment(c.transform.position, a + up, b + up) <= 0.8f)
                    Deal(c.gameObject, Atk * 0.4f, Vector3.zero, 0f);
            yield return new WaitForSeconds(0.4f);
        }
        if (line != null) Destroy(line.gameObject);
    }

    // ---------------- 공통 훅에서
    float MapOutgoing(GameObject target)
    {
        float m = 1f;
        if (L("c.legend") > 0 && Time.time < legendUntil) m *= 1f + legendStacks * V(L("c.legend"), 0.08f, 0.04f);
        // 미지의 영역 (패시브): 멀리 있는 적
        if (Sp != null && Sp.Has(SpecialAbilities.KitUncharted) && player != null && (target.transform.position - player.transform.position).sqrMagnitude > 36f)
            m *= Sp.IsEvolved(SpecialAbilities.KitUncharted) ? 1.35f : 1.2f;
        return m;
    }

    float MapTaken(float dmg)
    {
        bool moving = Kit != null && Kit.Moving;
        if (Sp != null && Sp.Has(SpecialAbilities.KitLocal) && moving) dmg *= Sp.IsEvolved(SpecialAbilities.KitLocal) ? 0.82f : 0.9f;
        if (E("ke.express") && Kit != null && Kit.MapChainActive) dmg *= 0.7f;
        return dmg;
    }

    float MapMove() => L("c.route") > 0 && Kit != null && Kit.MapChainActive ? 1f + V(L("c.route"), 0.08f, 0.04f) : 1f;

    void MapKilled(Vector3 pos)
    {
        // 지우개: 쓰러진 자리 둘레의 적 투사체를 지움
        if (L("c.erase") > 0)
        {
            float r = V(L("c.erase"), 2f, 0.5f) * Fs("c.erase");
            bool any = false;
            foreach (HostileProjectile hp in new List<HostileProjectile>(HostileProjectile.Live))
                if (hp != null && (hp.transform.position - pos).sqrMagnitude <= r * r) { Destroy(hp.gameObject); any = true; }
            if (any) Fx.Spawn("fx_smoke", pos, r * 1.6f, new Color(0.9f, 0.9f, 1f, 0.6f), 18f);
        }
        // 보물 지도: 나침도 안에서 쓰러진 적은 확률로 코인 3개 (황금 항로: 반드시)
        if (L("c.treasure") > 0)
        {
            foreach (var r in roses)
            {
                if (r.until < Time.time || (pos - r.p).sqrMagnitude > r.r * r.r) continue;
                if (E("ke.treasure") || Random.value < V(L("c.treasure"), 0.15f, 0.1f))
                {
                    Coin coin = Cache<Coin>.Get;
                    if (coin != null) coin.AddCoin(3);
                    CoinPicked(3);
                    SpecialAbilities.SharedFx?.FloatText(pos, "+3", MapGold, 3.5f, 0.2f);
                    Fx.Spawn("fx_sparkle", pos, 1.4f, MapGold, 22f);
                }
                break;
            }
        }
    }

    void MapTick()
    {
        // 나침도: 안의 적을 느리게
        if (roses.Count == 0) return;
        roses.RemoveAll(r => r.until < Time.time);
        if (L("c.rose") <= 0 || Time.frameCount % 10 != 0) return;
        float slow = 1f - V(L("c.rose"), 0.2f, 0.1f);
        foreach (var r in roses)
            Circle(r.p, r.r, 0f, 0f, c => { if (c.TryGetComponent(out EnermyController e)) e.Slow(slow, 0.3f); });
    }

    string MapStack(string key) => key switch
    {
        "c.legend" => Time.time < legendUntil && legendStacks > 0 ? "x" + legendStacks : null,
        "c.margin" => marginReady ? "!" : null,
        _ => null,
    };
}
