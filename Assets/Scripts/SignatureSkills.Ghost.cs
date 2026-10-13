using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 유령 고유 스킬 (1.1.2) · 패시브(서늘한 기운 · 떠도는 혼 · 영혼 수확 · 명계의 문)
//   h.grudge 원한 · h.wail 곡소리 · h.chill 한기 · h.drain 생기 흡수 · h.phase 실체 없음
//   h.wisp 도깨비불 · h.curse 저주 · h.polter 폴터가이스트 · h.frostbite 동상 · h.procession 망령의 행렬
// 진화: he.blizzard 눈보라 · he.banshee 밴시 · he.vampire 흡혈귀 · he.specter 망령 · he.doom 파멸
//       he.house 소란한 집 · he.glacier 빙하
public partial class SignatureSkills
{
    static readonly Color GhostC = new Color(0.65f, 0.92f, 1f);

    bool IsGhost => who == CharacterId.Ghost;

    // ---------------- 훅 (CharacterKit.Ghost 가 부름)
    public static float HandReachMul() => 1f;
    public static bool HandTrail() => Instance != null && Instance.IsGhost && Instance.E("he.blizzard");
    public static float WispChanceBonus() => Instance != null && Instance.IsGhost && Sp != null && Sp.Has(SpecialAbilities.KitHarvest) ? (Sp.IsEvolved(SpecialAbilities.KitHarvest) ? 0.35f : 0.2f) : 0f;
    public static bool ShardsFreeze() => Instance != null && Instance.IsGhost && Instance.E("he.glacier");
    public static void Touched(Collider2D c) { if (Instance != null && Instance.IsGhost) Instance.OnTouched(c); }
    public static void Frozen(GameObject g) { if (Instance != null && Instance.IsGhost) Instance.OnFrozen(g); }
    public static void WispBlocked(Vector3 p) { if (Instance != null && Instance.IsGhost) Instance.OnWispBlocked(p); }
    public static void Possessed(Vector3 at, bool killed) { if (Instance != null && Instance.IsGhost) Instance.OnPossessed(at, killed); }

    float drainAt, polterAt, underworldReady;

    void OnTouched(Collider2D c)
    {
        if (c == null || Kit == null) return;
        // 한기: 맞힌 적 둘레의 적에게도 냉기
        if (L("h.chill") > 0)
        {
            List<Transform> near = Nearest(c.transform.position, 1.8f * Fs("h.chill"), L("h.chill") + 1, c.gameObject);
            foreach (Transform t in near) if (t != null) Kit.AddChill(t.gameObject, 1);
        }
        // 생기 흡수: 얼어붙은 적을 맞히면 회복 (흡혈귀: 두 배)
        if (L("h.drain") > 0 && Kit.IsFrozen(c.gameObject) && Time.time >= drainAt)
        {
            drainAt = Time.time + 0.2f;
            Heal(V(L("h.drain"), 0.5f, 0.25f) * (E("he.vampire") ? 2f : 1f));
        }
    }

    void OnFrozen(GameObject g)
    {
        if (E("he.vampire")) Heal(1f);
    }

    void OnWispBlocked(Vector3 p)
    {
        // 곡소리: 영혼이 막을 때 둘레를 밀치고 피해 (밴시: 얼림)
        if (L("h.wail") <= 0) return;
        float r = 3.5f * Fs("h.wail");
        Circle(p, r, Atk * V(L("h.wail"), 0.8f, 0.2f), 3f, c => { if (E("he.banshee") && Kit != null) Kit.AddChill(c.gameObject, 9); });
        ShockRing.Spawn(p, 0.4f, r, 0.35f, Tint("h.wail", GhostC), 0.25f);
        Flair("h.wail", p, r * 0.7f, GhostC);
        Hostile.Play("roar", 0.4f, 1.8f);
    }

    void OnPossessed(Vector3 at, bool killed)
    {
        // 망령의 행렬: 빙의하면 영혼이 바로 생김
        if (L("h.procession") > 0 && Kit != null) Kit.AddWisps(L("h.procession") + 1);
        // 저주: 빙의한 적이 쓰러지면 둘레 적에게 저주 (파멸: 화면 전체)
        if (killed && L("h.curse") > 0)
        {
            float mul = 1f + V(L("h.curse"), 0.2f, 0.05f);
            if (E("he.doom")) Circle(at, 20f, 0f, 0f, c => Mark(c.gameObject, 5f, mul));
            else foreach (Transform t in Nearest(at, 9f, 3)) Mark(t.gameObject, 5f, mul);
            Fx.Spawn("fx_markskull", at + Vector3.up, 2.4f * Fs("h.curse"), Tint("h.curse", new Color(0.7f, 0.5f, 1f)), 14f);
            Flair("h.curse", at, 4f, new Color(0.7f, 0.5f, 1f));
        }
    }

    // ---------------- 공통 훅에서
    float GhostOutgoing(GameObject target)
    {
        float m = MarkMul(target);
        if (Kit == null) return m;
        bool frozen = Kit.IsFrozen(target);
        if (L("h.grudge") > 0) m *= 1f + V(L("h.grudge"), 0.03f, 0.01f) * (frozen ? 5 : Kit.ChillOf(target));
        if (L("h.wisp") > 0) m *= 1f + V(L("h.wisp"), 0.04f, 0.01f) * Kit.Wisps;
        if (L("h.frostbite") > 0 && frozen) m *= 1f + V(L("h.frostbite"), 0.15f, 0.07f);
        return m;
    }

    float GhostTaken(float dmg)
    {
        if (dmg <= 0f || Kit == null) return dmg;
        // 실체 없음: 움직이는 동안 확률로 통과 (망령: 두 배)
        if (L("h.phase") > 0 && Kit.Moving && Random.value < V(L("h.phase"), 0.1f, 0.04f) * (E("he.specter") ? 2f : 1f))
        {
            Fx.Spawn("fx_stealth", transform.position, 2f, new Color(GhostC.r, GhostC.g, GhostC.b, 0.6f), 18f);
            return 0f;
        }
        // 영혼이 대신 사라지며 막음
        if (Kit.TryWispBlock()) return 0f;
        // 명계의 문 (패시브): 체력 20% 아래로 떨어질 피해면 3초 무적 (60초 · 진화 40초)
        if (Sp != null && Sp.Has(SpecialAbilities.KitUnderworld) && player != null && Time.time >= underworldReady && player.PlayerHealth - dmg < player.PlayerMaxHealth * 0.2f)
        {
            underworldReady = Time.time + (Sp.IsEvolved(SpecialAbilities.KitUnderworld) ? 40f : 60f);
            player.GrantInvincibility(3f);
            Fx.Spawn("fx_soulburst", transform.position, 5f, new Color(0.5f, 0.4f, 0.9f), 16f);
            SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 1.5f, Loc.T("명계의 문!"), new Color(0.7f, 0.6f, 1f), 4.5f, 0f);
            return 0f;
        }
        return dmg;
    }

    float GhostMove()
    {
        float m = 1f;
        if (Sp != null && Sp.Has(SpecialAbilities.KitWander)) m *= Sp.IsEvolved(SpecialAbilities.KitWander) ? 1.12f : 1.06f;
        if (E("he.specter") && Kit != null) m *= 1f + 0.02f * Kit.Wisps;
        return m;
    }

    void GhostTick()
    {
        if (player == null) return;
        Vector3 p = transform.position;
        // 서늘한 기운 (패시브): 둘레 적이 느려짐
        if (Sp != null && Sp.Has(SpecialAbilities.KitChillAura) && Time.frameCount % 10 == 0)
        {
            float slow = Sp.IsEvolved(SpecialAbilities.KitChillAura) ? 0.75f : 0.85f;
            Circle(p, 2.5f, 0f, 0f, c => { if (c.TryGetComponent(out EnermyController e)) e.Slow(slow, 0.3f); });
        }
        // 폴터가이스트: 3초마다 가까운 적을 들어 올렸다 내리꽂음 (소란한 집: 셋)
        if (L("h.polter") > 0 && Time.time >= polterAt)
        {
            polterAt = Time.time + 3f;
            foreach (Transform t in Nearest(p, 8f, E("he.house") ? 3 : 1)) StartCoroutine(Poltergeist(t));
        }
    }

    IEnumerator Poltergeist(Transform t)
    {
        if (t == null) yield break;
        bool lift = t.CompareTag("enermy");
        Vector3 start = t.position;
        for (float k = 0f; k < 0.35f && t != null; k += Time.deltaTime)
        {
            if (lift) t.position = start + Vector3.up * Mathf.Sin(k / 0.35f * Mathf.PI * 0.5f) * 1.6f;
            yield return null;
        }
        if (t == null) yield break;
        if (lift) t.position = start;
        Deal(t.gameObject, Atk * V(L("h.polter"), 1f, 0.4f), Vector3.zero, 0f);
        Fx.Spawn("fx_shock", start, 3f * Fs("h.polter"), Tint("h.polter", GhostC), 20f);
        Flair("h.polter", start, 2f, GhostC);
        Hostile.Play("thump", 0.5f, 0.9f);
    }

    string GhostStack(string key) => key switch
    {
        "h.wisp" => Kit != null && Kit.Wisps > 0 ? "x" + Kit.Wisps : null,
        "h.polter" => Time.time < polterAt ? (polterAt - Time.time).ToString("0.0") + "s" : null,
        _ => null,
    };
}
