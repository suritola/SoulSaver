using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 연금술사 고유 스킬 · 진화
//   l.ignite 인화성 기체 · l.frost 서리 결정 · l.catalyst 촉매 반응 · l.amplify 증폭 용액 · l.tinker 조수 개조
//   l.glassrain 유리 비 · l.salve 응급 연고 · l.refine 폭발 정제 · l.embers 연소 흔적 · l.concentrate 시약 농축
public partial class SignatureSkills
{
    static readonly Color Ice = new Color(0.6f, 0.9f, 1f);
    static readonly Color AcidGreen = new Color(0.55f, 1f, 0.35f);
    static readonly Color Ember = new Color(1f, 0.55f, 0.2f);

    readonly List<(Vector3 p, float r, float until)> acids = new List<(Vector3, float, float)>();
    readonly Dictionary<GameObject, float> chilled = new Dictionary<GameObject, float>();
    readonly Dictionary<GameObject, (int mask, float until)> elements = new Dictionary<GameObject, (int, float)>();
    readonly List<SigZone> embers = new List<SigZone>();
    readonly HashSet<GameObject> popped = new HashSet<GameObject>();
    int amplify;
    int throws;
    float glassAt, salveTick;
    Vector3 lastStep;
    float stepDist;
    GameObject twin;
    float twinAt;
    int tinkerKind;

    // ---------------- 게임 코드가 부르는 연금술사 훅
    // 평타 플라스크를 던지기 직전: 시약 농축이면 피해 · 범위 배율 (1 = 보통)
    public static float FlaskThrow() => Instance != null && Instance.who == CharacterId.Alchemist ? Instance.OnFlaskThrow() : 1f;
    // 평타 플라스크가 터진 뒤 (kills: 이 폭발로 쓰러진 적 수)
    public static void Flask(Vector3 p, float r, int kind, bool unstable, List<Collider2D> inside, int kills, bool concentrated)
    { if (Instance != null && Instance.who == CharacterId.Alchemist) Instance.OnFlask(p, r, kind, unstable, inside, kills, concentrated); }
    public static float BigFlaskMul() => Instance != null && Instance.who == CharacterId.Alchemist ? Instance.TakeAmplify() : 1f;
    public static void BigFlask(Vector3 p, float r) { if (Instance != null && Instance.who == CharacterId.Alchemist) Instance.OnBigFlask(p, r); }
    public static void ChainPopped(Vector3 p, float hit) { if (Instance != null && Instance.who == CharacterId.Alchemist) Instance.OnChainPop(p, hit, 0); }
    public static float HomunculusMul => Instance != null && Instance.L("l.tinker") > 0 ? 1f + V(Instance.L("l.tinker"), 0.3f, 0.2f) : 1f;
    public static void HomunculusLand(Vector3 q, float dmg) { if (Instance != null && Instance.L("l.tinker") > 0) Instance.TinkerLand(q, dmg); }
    // 2026-10-19~ 갈래 (Paths/Paths.Alchemist.cs · CharacterKit.Alchemist.cs 가 씀)
    public static GameObject AlchTwin => Instance != null && Instance.who == CharacterId.Alchemist ? Instance.twin : null;       // 쌍둥이 조수 (조수 수 세기)
    public static bool AlchTinker => Instance != null && Instance.L("l.tinker") > 0;                                               // 조수 개조를 배웠나 (시약 따라 던지기를 한 곳에서만)
    // 늪 웅덩이도 산성 웅덩이로 셈 (응급 연고 · 인화성 기체)
    public static void AddAcid(Vector3 p, float r, float until)
    {
        if (Instance == null || Instance.who != CharacterId.Alchemist) return;
        if (Instance.acids.Count >= 60) Instance.acids.RemoveAll(a => a.until < Time.time);
        if (Instance.acids.Count >= 60) Instance.acids.RemoveAt(0);
        Instance.acids.Add((p, r, until));
    }

    float OnFlaskThrow()
    {
        int c = L("l.concentrate");
        if (c <= 0) return 1f;
        throws++;
        if (throws % 4 != 0) return 1f;           // 세 가지 시약을 한 바퀴 던진 뒤 네 번째
        Fx.Spawn("fx_sparkle", player.MuzzlePosition, 1.6f, new Color(1f, 0.9f, 0.5f), 20f);
        return 1f + V(c, 0.4f, 0.2f);
    }

    void OnFlask(Vector3 p, float r, int kind, bool unstable, List<Collider2D> inside, int kills, bool concentrated)
    {
        if (unstable && L("l.refine") > 0) Heal(V(L("l.refine"), 2f, 1f));
        if (L("l.amplify") > 0 && kills > 0) amplify = Mathf.Min(20, amplify + kills);

        if (!unstable && kind == 2) acids.Add((p, r * 0.8f, Time.time + 2.5f + (Kit != null ? Kit.card[6] : 0)));
        if (!unstable && kind == 0 && L("l.ignite") > 0) Ignite(p, r);

        foreach (Collider2D c in inside)
        {
            if (c == null) continue;
            GameObject g = c.gameObject;
            if (!unstable && kind == 1 && L("l.frost") > 0)
            {
                chilled[g] = Time.time + 3f;
                if (E("le.frost")) Mark(g, 3f, 1.3f);
            }
            if (!unstable && (L("l.catalyst") > 0 || Paths.Has(AlchemistPaths.Catalyst))) AddElement(g, 1 << kind);     // 원소 촉매 입문: 촉매 반응 없이도 붕괴
        }
        // 불꽃 행진: 농축 플라스크가 터진 자리에 불길 고리
        if (concentrated && E("le.march"))
        {
            using var tier = Fx.Tier(3);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                Vector3 at = p + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r * 1.2f;
                EmberPatch(at, 1.1f);
            }
            ShockRing.Spawn(p, 0.3f, r * 1.4f, 0.35f, Ember, 0.25f);
        }
    }

    // 인화성 기체: 화염 시약이 산성 웅덩이에 닿으면 웅덩이가 불길로 터짐 (타오르는 늪: 4초 동안 계속 탐)
    void Ignite(Vector3 p, float r)
    {
        acids.RemoveAll(a => a.until < Time.time);
        // 늪 갈래는 웅덩이가 많아 한 번에 여러 개가 같이 터지지 않게 두 개까지
        int cap = Paths.Has(AlchemistPaths.Swamp) ? 2 : int.MaxValue, lit = 0;
        for (int i = acids.Count - 1; i >= 0 && lit < cap; i--)
        {
            var a = acids[i];
            if ((a.p - p).sqrMagnitude > (a.r + r) * (a.r + r)) continue;
            acids.RemoveAt(i);
            lit++;
            using var tier = EvoFx("l.ignite");
            Circle(a.p, a.r * 1.3f, Atk * V(L("l.ignite"), 1.2f, 0.5f) * P("l.ignite"), 1.5f, c => Burn.Apply(c.gameObject, Atk * 0.3f, 2f));      // 1.1.2~ 타오르는 늪: ×1.6
            Fx.Spawn("fx_firepillar", a.p + Vector3.up * 1.2f, a.r * 2.4f * Fs("l.ignite"), Color.white, 18f);
            Fx.Spawn("fx_explosion", a.p, a.r * 2.6f * Fs("l.ignite"), new Color(1f, 0.7f, 0.4f), 18f);
            Flair("l.ignite", a.p, a.r * 1.5f, Ember);
            Hostile.Play("ignite", 0.6f, 0.8f);
            if (E("le.swamp"))
            {
                float rr = a.r;
                Zone(a.p, rr, 4f, 0.4f, zone =>
                {
                    foreach (Collider2D c in Inside(zone)) { Burn.Apply(c.gameObject, Atk * 0.35f, 1.5f); Deal(c.gameObject, Atk * 0.15f, Vector3.zero, 0f); }
                }, "fx_puddle", new Color(1f, 0.5f, 0.2f, 0.75f), rr * 2.2f);
            }
        }
    }

    // 촉매 반응: 화염 · 빙결 · 산성이 모두 묻으면 원소 붕괴
    //   2026-10-19~ 원소 촉매 갈래 (Paths/Paths.Alchemist.cs): 촉매 반응을 배우기 전에도 입문만으로 붕괴 — 대신 화염 → 빙결 → 산성 순서대로만 쌓임
    //     (촉매 반응을 배우면 원래처럼 순서 없이 · 그 스킬 피해로) · 공명 I 붕괴한 적 받는 피해 +15% · 공명 II 가까운 적 둘에게 원소 · 극의 원소 둘이면 붕괴
    void AddElement(GameObject g, int bit)
    {
        bool skill = L("l.catalyst") > 0;
        bool seed = Paths.Has(AlchemistPaths.Catalyst);
        int mask = elements.TryGetValue(g, out var e) && e.until > Time.time ? e.mask : 0;
        if (!skill && seed)
        {
            if (bit == 1) mask = 1;                                     // 화염이면 처음부터
            else if (bit == 2 && (mask & 1) != 0) mask |= 2;            // 화염 뒤의 빙결만
            else if (bit == 4 && (mask & 3) == 3) mask |= 4;            // 화염 · 빙결 뒤의 산성만
        }
        else mask |= bit;
        int count = (mask & 1) + ((mask >> 1) & 1) + ((mask >> 2) & 1);
        bool ready = mask == 7 || (Paths.Mastered(AlchemistPaths.Catalyst) && count >= 2);
        if (!ready) { elements[g] = (mask, Time.time + 6f); return; }
        elements.Remove(g);
        Vector3 p = g.transform.position;
        float dmg = skill ? Atk * V(L("l.catalyst"), 2.5f, 1f) * P("l.catalyst")        // 1.1.2~ 삼원소 붕괴: ×1.5
                          : Atk * AlchemistPaths.CatalystSeedMul;
        if (seed)
        {
            dmg *= Paths.Power(AlchemistPaths.Catalyst);
            if (Paths.Res(AlchemistPaths.Catalyst) >= 1) Mark(g, AlchemistPaths.CatalystRes1Time, AlchemistPaths.CatalystRes1);
            if (Paths.Res(AlchemistPaths.Catalyst) >= 2)
            {
                // 가까운 적 둘에게 원소 하나씩 (순서 규칙이면 첫 원소인 화염 · 아니면 아무 원소) — 붕괴는 그 적이 다음에 맞을 때
                foreach (Transform t in Nearest(p, 5f, 2, g))
                {
                    if (t == null) continue;
                    GameObject o = t.gameObject;
                    int had = elements.TryGetValue(o, out var oe) && oe.until > Time.time ? oe.mask : 0;
                    int add = !skill ? 1 : 1 << Random.Range(0, 3);
                    elements[o] = (!skill ? Mathf.Max(had, 1) : had | add, Time.time + 6f);
                    Fx.Spawn("fx_sparkle", t.position, 1.2f, new Color(1f, 0.85f, 0.45f), 24f);
                }
            }
        }
        bool big = E("le.collapse");
        using var tier = EvoFx("l.catalyst");
        Deal(g, dmg, Vector3.zero, 0f);
        Circle(p, big ? 4f : 2f, dmg * 0.5f, 1.5f, c =>
        {
            if (!big || c.gameObject == g) return;
            int two = new[] { 3, 5, 6 }[Random.Range(0, 3)];
            elements[c.gameObject] = (two, Time.time + 6f);
        });
        Fx.Spawn("fx_alchemyblast", p, (big ? 9f : 5f) * Fs("l.catalyst"), new Color(0.95f, 0.75f, 1f), 16f);
        Flair("l.catalyst", p, big ? 6f : 4f, new Color(0.95f, 0.75f, 1f));
        ShockRing.Spawn(p, 0.3f, big ? 4.5f : 2.5f, 0.35f, new Color(1f, 0.8f, 1f, 0.9f), 0.2f);
        Hostile.Play("boom", 0.6f, 1.2f);
        if (elements.Count > 300) elements.Clear();
    }

    float TakeAmplify()
    {
        int a = L("l.amplify");
        if (a <= 0 || amplify <= 0) return 1f;
        float mul = 1f + amplify * V(a, 0.03f, 0.01f);
        amplify = 0;
        return mul;
    }

    void OnBigFlask(Vector3 p, float r)
    {
        if (E("le.glass")) StartCoroutine(GlassStorm(p, r * 1.4f, 3f));
    }

    IEnumerator GlassStorm(Vector3 p, float r, float seconds)
    {
        for (float t = 0f; t < seconds && Alive; t += 0.25f)
        {
            Vector3 at = p + (Vector3)(Random.insideUnitCircle * r);
            GlassDrop(player.MuzzlePosition, at);
            yield return new WaitForSeconds(0.25f);
        }
    }

    void GlassDrop(Vector3 from, Vector3 at)
    {
        float dmg = Atk * 0.5f * P("l.glassrain");        // 1.1.2~ 유리 폭풍: ×1.6
        float fs = Fs("l.glassrain");
        FlaskLob.Throw(from, at, 0.35f, 0.5f * fs, new Color(0.85f, 0.95f, 1f), q =>
        {
            using var tier = EvoFx("l.glassrain");
            Circle(q, 1.4f, dmg, 0.6f);
            Fx.Spawn("fx_alchemyblast", q, 3f * fs, new Color(0.85f, 0.95f, 1f), 20f);
            Flair("l.glassrain", q, 1.6f, new Color(0.85f, 0.95f, 1f));
        });
    }

    // 연쇄 대폭발: 연쇄 폭발로 쓰러진 적도 다시 터짐 (최대 3번) + 조금 회복
    void OnChainPop(Vector3 p, float hit, int depth)
    {
        if (!E("le.chain")) return;
        Heal(1f);
        if (depth >= 3) return;
        if (popped.Count > 200) popped.Clear();
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(p, 1.8f, hits)))
        {
            if (c == null) continue;
            EnermyController e = c.GetComponent<EnermyController>();
            if (e == null || !e.IsDead || !popped.Add(c.gameObject)) continue;
            Vector3 at = c.transform.position;
            Circle(at, 1.5f, hit * 0.5f, 1f);
            Fx.Spawn("fx_alchemyblast", at, 3f, new Color(1f, 0.75f, 0.4f), 22f);
            OnChainPop(at, hit * 0.7f, depth + 1);
        }
    }

    // 조수 개조: 조수 플라스크에 시약을 번갈아
    void TinkerLand(Vector3 q, float dmg)
    {
        int k = tinkerKind;
        tinkerKind = (tinkerKind + 1) % 3;
        // 2026-10-19~ 조수 공방(갈래): 조수가 내가 마지막에 던진 시약을 따라 던짐
        if (Paths.Has(CharacterId.Alchemist, AlchemistPaths.Twins) && Kit != null && Kit.AlchLastKind >= 0) k = Kit.AlchLastKind;      // 영웅 확인 (조수 개조는 영웅 확인 없이 불림)
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(q, 1.6f, hits)))
        {
            if (c == null || !IsFoe(c)) continue;
            if (k == 0) Burn.Apply(c.gameObject, dmg * 0.3f, 2f);
            else if (k == 1) { EnermyController e = c.GetComponent<EnermyController>(); if (e != null) e.Slow(0.5f, 1.2f); }
        }
        if (k == 2 && SpecialAbilities.SharedInstance != null) SpecialAbilities.SharedInstance.SpawnZone(q, 1.2f, 2.5f, dmg * 0.2f, new Color(0.45f, 1f, 0.3f, 0.6f));
        Color[] tint = { new Color(1f, 0.55f, 0.25f), Ice, AcidGreen };
        Fx.Spawn("fx_alchemyblast", q, 3.4f, tint[k], 20f);
    }

    float AlchemistOutgoing(GameObject target) => MarkMul(target);

    void AlchemistHit(GameObject target, float dmg, bool killed, bool proc)
    {
        if (!killed) return;
        // 서리 결정: 빙결 시약에 맞은 적이 쓰러지면 얼음 파편
        int f = L("l.frost");
        if (f > 0 && chilled.TryGetValue(target, out float until) && until > Time.time)
        {
            chilled.Remove(target);
            Vector3 p = target.transform.position;
            int n = (int)V(f, 4f, 1f);
            using var tier = EvoFx("l.frost");
            for (int i = 0; i < n; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, i * 360f / n + Random.Range(-10f, 10f)) * Vector2.right;
                Bullet b = Shot(p, d, Atk * 0.5f * P("l.frost"), 2, 22f, 7f, "fx_spark", 0.7f * Fs("l.frost"), Tint("l.frost", Ice));       // 1.1.2~ 영구 동토: ×1.6
                if (b != null && Paths.Res(AlchemistPaths.Frost) >= 2) b.onHitEnemy += (bullet, col) => Paths.Safe(() => AlchemistPaths.ShardHit(col));     // 빙결 공명 II: 10% 로 다시 얼림
                if (b != null && E("le.frost"))
                    b.onHitEnemy += (bullet, col) =>
                    {
                        EnermyController e = col.GetComponent<EnermyController>();
                        if (e != null) e.Slow(0f, 0.8f);
                        Mark(col.gameObject, 3f, 1.3f);
                    };
            }
            Fx.Spawn("fx_shock", p, 3.6f * Fs("l.frost"), Ice, 22f);
            Flair("l.frost", p, 3f, Ice);
            Hostile.Play("shatter", 0.35f, 1.6f);
        }
        if (chilled.Count > 300) chilled.Clear();
    }

    void EmberPatch(Vector3 at, float r)
    {
        embers.RemoveAll(z => z == null);
        if (embers.Count >= 10) { if (embers[0] != null) Destroy(embers[0].gameObject); embers.RemoveAt(0); }
        float dmg = Atk * V(Mathf.Max(1, L("l.embers")), 0.15f, 0.07f) * 0.5f * P("l.embers");       // 1.1.2~ 불꽃 행진: ×1.6
        embers.Add(Zone(at, r, 2f, 0.5f, zone =>
        {
            foreach (Collider2D c in Inside(zone)) Deal(c.gameObject, dmg, Vector3.zero, 0f);
        }, "fx_puddle", Big("l.embers") ? new Color(1f, 0.6f, 0.2f, 0.55f) : new Color(1f, 0.5f, 0.15f, 0.4f), r * 1.8f, true));        // 바닥에 옅게 (시야를 가리지 않게) · 진화하면 조금 더 밝게
    }

    void AlchemistTick()
    {
        Vector3 me = transform.position;
        // 연소 흔적: 걸어간 자리에 불길
        if (L("l.embers") > 0)
        {
            stepDist += (me - lastStep).magnitude;
            if (stepDist >= 1.4f)
            {
                stepDist = 0f;
                EmberPatch(me, E("le.march") ? 1.6f : 0.8f);
            }
        }
        lastStep = me;
        // 응급 연고: 산성 웅덩이 위에서 회복
        int s = L("l.salve");
        if (s > 0)
        {
            acids.RemoveAll(a => a.until < Time.time);
            bool on = false;
            foreach (var a in acids) if ((a.p - me).sqrMagnitude <= a.r * a.r) { on = true; break; }
            if (on && Time.time >= salveTick)
            {
                salveTick = Time.time + 0.5f;
                Heal(V(s, 1f, 0.5f) * 0.5f);
            }
        }
        // 유리 비: 대폭발 플라스크를 모으는 동안
        int g = L("l.glassrain");
        CharacterKit k = Kit;
        if (g > 0 && k != null && k.Charging && Time.time >= glassAt)
        {
            glassAt = Time.time + V(g, 0.6f, -0.1f);
            List<Transform> near = Nearest(me, 9f, 5);
            if (near.Count > 0) GlassDrop(player.MuzzlePosition, near[Random.Range(0, near.Count)].position);
        }
        // 쌍둥이 조수
        if (E("le.twins")) TwinTick();
    }

    void TwinTick()
    {
        if (twin == null)
        {
            Sprite[] f = Fx.Frames("fx_flask");
            if (f.Length == 0) return;
            twin = SpecialAbilities.MakeSprite("HomunculusTwin", f[0], transform.position, 1.25f / f[0].bounds.size.y, new Color(1f, 0.75f, 0.5f), "Character", 2);
            twinAt = Time.time + 1f;
        }
        float t = Time.time;
        Vector3 home = transform.position + new Vector3(Mathf.Cos(t * 2f + Mathf.PI) * 1.4f, 1.3f + Mathf.Sin(t * 4f + 1f) * 0.2f, 0f);
        twin.transform.position = Vector3.Lerp(twin.transform.position, home, 8f * Time.deltaTime);
        if (Time.time < twinAt) return;
        List<Transform> near = Nearest(twin.transform.position, 12f, 1);
        if (near.Count == 0) { twinAt = Time.time + 0.3f; return; }
        twinAt = Time.time + 3f;
        float dmg = Atk * 1.4f * HomunculusMul;
        FlaskLob.Throw(twin.transform.position, near[0].position, 0.45f, 0.55f, new Color(1f, 0.75f, 0.5f), q =>
        {
            Circle(q, 1.4f, dmg, 1f);
            Fx.Spawn("fx_alchemyblast", q, 3f, new Color(1f, 0.75f, 0.45f), 18f);
            TinkerLand(q, dmg);
        });
    }

    void OnDisable()
    {
        if (twin != null) Destroy(twin);
    }
}
