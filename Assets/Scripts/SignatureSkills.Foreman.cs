using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공사 반장 고유 스킬 (2.0.0) · 패시브(안전화 · 현장 소장 · 무너진 담 · 새참은 CharacterKit.Foreman 이 직접 읽음)
//   b.overtime 야근 수당 · b.rebar 철근 · b.roller 롤러 자국 · b.crane 크레인 · b.safety 안전 제일
//   b.cement 시멘트 · b.overload 과적 · b.detour 우회로 · b.salvage 자재 회수 · b.later 추후 업데이트 예정
// 진화: be.dozer 불도저 · be.truck 덤프 트럭 · be.concrete 콘크리트 · be.closed 통행 금지 · be.zero 무재해 100일 · be.pile 항타기 · be.release 정식 출시
// 훅은 CharacterKit.Foreman 이 부름 (더미 상태는 Kit.FmMass · FmCenter · FmHolding 으로 읽음)
public partial class SignatureSkills
{
    bool IsFm => who == CharacterId.Foreman;

    // ---------------- 값 읽기 (정적 · 공사 반장이 아니면 0 / false)
    public static int FmLv(string key) => Instance != null && Instance.IsFm ? Instance.L(key) : 0;
    public static bool FmEvo(string key) => Instance != null && Instance.IsFm && Instance.E(key);

    float fmRollerAt, fmOverAt, fmBlockWinAt, fmDetourPurge, fmRollTickAt;
    int fmBlockWin;
    int fmLaterBase;
    bool fmReleaseShown;
    readonly List<(Vector3 p, float until)> fmRollers = new List<(Vector3, float)>(16);
    readonly Dictionary<int, float> fmDetourAt = new Dictionary<int, float>();
    static readonly List<Collider2D> fmRollHits = new List<Collider2D>(32);

    // ---------------- 정적 훅 (CharacterKit.Foreman 이 부름)
    public static float FmCrushMul(bool wall) => 1f;
    public static int FmBounceBonus() => 0;
    public static float FmFenceLifeMul() => 1f;
    public static float FmDumpDistMul() => Instance != null && Instance.IsFm ? Instance.FmDistMul() : 1f;
    public static float FmCementDumpMul(int cement) => cement >= 3 && Instance != null && Instance.IsFm ? Instance.FmCementMul() : 1f;
    public static void FmCrushed(Vector3 center, int mass, bool wall) { }
    public static void FmCrashed(Vector3 at, bool onFence, int mass) { }
    public static void FmUlt() { }
    public static void FmPileKilled(Vector3 pos, bool hardened) { if (Instance != null && Instance.IsFm) Instance.OnFmPileKilled(pos, hardened); }
    public static void FmDumped(Vector3 origin, Vector2 dir, int mass, float dist) { if (Instance != null && Instance.IsFm) Instance.OnFmDumped(origin, dir, mass, dist); }
    public static void FmDumpLanded(Vector3 land, Vector2 dir, int mass) { if (Instance != null && Instance.IsFm) Instance.OnFmLanded(land, dir, mass); }
    public static void FmBounce(Vector3 at) { if (Instance != null && Instance.IsFm) Instance.OnFmBounce(at); }
    public static void FmFenceGone(Vector3 pos, Vector2 along) { }
    public static void FmFenced(EnermyController e, Vector3 fencePos) { if (Instance != null && Instance.IsFm) Instance.OnFmFenced(e); }
    public static void FmOverflow(Vector3 pos) { if (Instance != null && Instance.IsFm) Instance.OnFmOverflow(pos); }
    public static void FmBlocked(Vector3 at, Vector2 dir) { if (Instance != null && Instance.IsFm) Instance.OnFmBlocked(at); }

    // ---------------- 도우미
    void FmLine(Vector3 a, Vector3 b, float w, float dmg, float knock)
    {
        using var list = PhysHits.Circle((a + b) * 0.5f, Vector3.Distance(a, b) * 0.5f + w + 1f, true);
        foreach (Collider2D c in list)
        {
            if (c == null || !IsFoe(c)) continue;
            if (c.TryGetComponent(out EnermyController e) && (e.holder != null || e.IsDead)) continue;
            if (Hostile.DistanceToSegment(c.transform.position, a, b) > w) continue;
            Deal(c.gameObject, dmg, (b - a).normalized, knock);
        }
    }

    // 시멘트: 굳은 적이 있는 덤프는 거리 +30% 고정
    float FmDistMul() => L("b.cement") > 0 && Kit != null && Kit.FmHardCount > 0 ? 1.3f : 1f;

    float FmCementMul() => 1f + V(L("b.cement"), 0.3f, 0.15f) * P("b.cement");

    // ---------------- 훅 본체
    // 더미 안에서 쓰러진 적: 자재 회수(코인) · 잔해 · 콘크리트(굳은 적은 무게 2 덩이)
    void OnFmPileKilled(Vector3 pos, bool hardened)
    {
        int ls = L("b.salvage");
        if (ls > 0 && Random.value < V(ls, 0.10f, 0.05f) * (E("be.release") ? 2f : 1f) * P("b.salvage"))
        {
            Coin coin = Cache<Coin>.Get;
            if (coin != null) coin.AddCoin(1);
            Fx.Spawn("fx_sparkle", pos, 1.2f, new Color(1f, 0.9f, 0.3f), 24f);
        }
        if (Kit != null) Kit.FmAddRubble(pos, hardened && E("be.concrete") ? 2 : 1, hardened);
    }

    // 덤프: 철근(꿰뚫음) · 덤프 트럭(철근 두 배) · 추후 업데이트 예정(공정률 쇠공)
    void OnFmDumped(Vector3 origin, Vector2 dir, int mass, float dist)
    {
        int lr = L("b.rebar");
        if (lr > 0)
        {
            int n = Mathf.RoundToInt(V(lr, 2f, 1f)) * (E("be.truck") ? 2 : 1);
            float len = dist + 7f;
            for (int i = 0; i < n; i++)
            {
                float a = n == 1 ? 0f : -18f + 36f * i / (n - 1);
                Vector2 d = Quaternion.Euler(0f, 0f, a) * dir;
                Vector3 to = Hostile.ClampArena(origin + (Vector3)(d * len));
                FmLine(origin, to, 0.6f, Atk * 0.7f * P("b.rebar"), 1.5f);
                Fx.Stretch("fx_rebar", origin, to, 0.7f, Tint("b.rebar", Color.white), 0.25f, 14, 24f);
            }
            Hostile.Play("clank", 0.4f, 1.2f);
            Flair("b.rebar", origin, 2.5f, new Color(0.8f, 0.8f, 0.9f));
        }
        int ll = L("b.later");
        if (ll > 0 && Kit != null)
        {
            int need = FmLaterNeed(ll);
            bool release = E("be.release");
            float progress = release ? 1f : Mathf.Clamp01((Kit.FmDumpCount - fmLaterBase) / (float)need);
            Vector3 at = Hostile.ClampArena(origin + (Vector3)(dir * dist));
            float dmg = Atk * 1.5f * Mathf.Max(0.1f, progress) * P("b.later");
            int balls = release ? 2 : 1;
            Vector2 side = new Vector2(-dir.y, dir.x);
            for (int i = 0; i < balls; i++) StartCoroutine(FmLaterBall(at + (Vector3)(side * (i == 0 ? 0f : 2.2f)), dmg, 0.4f + 0.1f * i));
            if (release)
            {
                if (!fmReleaseShown)
                {
                    fmReleaseShown = true;
                    SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 2.4f, Loc.T("얼리 액세스 종료!"), new Color(1f, 0.9f, 0.3f), 6f, 0f);
                }
            }
            else if (progress >= 1f)
            {
                fmLaterBase = Kit.FmDumpCount;
                SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 2.4f, Loc.T("완공!"), new Color(1f, 0.9f, 0.3f), 6f, 0f);
                Hostile.Play("chime", 0.7f, 0.8f);
            }
        }
    }

    static int FmLaterNeed(int level) => level >= 4 ? 12 : level == 3 ? 20 : level == 2 ? 30 : 40;

    IEnumerator FmLaterBall(Vector3 at, float dmg, float delay)
    {
        Friendly.Circle(at, 1.8f, delay, new Color(1f, 0.9f, 0.3f, 0.7f));
        FxAnim ball = Fx.Play("fx_wreckball", at + Vector3.up * 6f, 1.8f, Color.white, 14f, 0f, 22, true, delay + 0.3f);
        for (float t = 0f; t < delay; t += Time.deltaTime)
        {
            if (ball != null) ball.transform.position = Vector3.Lerp(at + Vector3.up * 6f, at, t / delay);
            yield return null;
        }
        if (ball != null) Destroy(ball.gameObject);
        Circle(at, 1.8f, dmg, 3f);
        Fx.Spawn("fx_explosion", at, 3.6f, new Color(1f, 0.9f, 0.4f), 18f);
        Hostile.Play("boom", 0.4f, 1.1f);
    }

    // 착지: 덤프 트럭(착지한 뒤 한 번 더 앞으로 쏟아짐)
    void OnFmLanded(Vector3 land, Vector2 dir, int mass)
    {
        if (!E("be.truck")) return;
        Vector3 to = Hostile.ClampArena(land + (Vector3)(dir * 5f));
        FmLine(land, to, 1.6f, Atk * 0.6f * (1f + 0.1f * Mathf.Min(mass, 12)) * P("b.rebar"), 3f);
        Fx.Spawn("fx_landslide", (land + to) * 0.5f, 5f, new Color(1f, 1f, 1f, 0.8f), 14f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 6);
        Flair("b.rebar", land, 3f, new Color(1f, 0.85f, 0.4f));
    }

    // 크레인: 쇠공이 튕길 때마다 둘레 적을 쇠공 쪽으로 끌어당김 (보스 제외)
    void OnFmBounce(Vector3 at)
    {
        int lc = L("b.crane");
        if (lc <= 0) return;
        float r = V(lc, 3f, 0.5f) * Fs("b.crane");
        Circle(at, r, 0f, 0f, c =>
        {
            if (c == null || IsBoss(c.gameObject)) return;
            if (!c.TryGetComponent(out EnermyController e) || e.holder != null || e.IsDead) return;
            e.transform.position = Vector3.Lerp(e.transform.position, at, 0.6f);
        });
        Fx.Spawn("fx_shock", at, r * 2f, Tint("b.crane", new Color(1f, 0.9f, 0.4f, 0.5f)), 20f);
        Flair("b.crane", at, 3f, new Color(1f, 0.85f, 0.3f));
    }

    // 우회로 · 통행 금지: 바리케이드에 부딪힌 적이 빠각 피해를 받고 반장 쪽으로 1.5칸 미끄러짐 (적마다 1초에 한 번)
    void OnFmFenced(EnermyController e)
    {
        int ld = L("b.detour");
        bool closed = E("be.closed");
        if ((ld <= 0 && !closed) || e == null || e.IsDead) return;
        int id = e.GetInstanceID();
        if (fmDetourAt.TryGetValue(id, out float t) && Time.time < t) return;
        if (Time.time > fmDetourPurge) { fmDetourPurge = Time.time + 20f; fmDetourAt.Clear(); }
        fmDetourAt[id] = Time.time + 1f;
        float dmg = ld > 0 ? Atk * V(ld, 0.6f, 0.2f) * P("b.detour") : Atk * 0.8f;
        Deal(e.gameObject, dmg, Vector3.zero, closed ? 3f : 0f);
        if (ld > 0 && !e.IsDead && player != null)
        {
            Vector3 np = Vector3.MoveTowards(e.transform.position, player.transform.position, 1.5f);
            if (!Hostile.IsWall(np)) e.transform.position = np;
        }
        Fx.Spawn("fx_crunch", e.transform.position, 1.8f, Color.white, 20f);
        Flair("b.detour", e.transform.position, 2f, new Color(1f, 0.85f, 0.3f));
    }

    // 과적: 가득 찬 더미에 넘친 적이 굴러떨어지며 둘레를 다치게 함 (0.25초에 한 번)
    void OnFmOverflow(Vector3 pos)
    {
        int lo = L("b.overload");
        if (lo <= 0 || Time.time < fmOverAt) return;
        fmOverAt = Time.time + 0.25f;
        float r = 1.8f * Fs("b.overload");
        Circle(pos, r, Atk * V(lo, 0.4f, 0.15f) * P("b.overload"), 2f);
        Fx.Spawn("fx_crunch", pos, r * 1.4f, Tint("b.overload", Color.white), 20f);
    }

    // 무재해 100일: 더미가 막은 적 투사체를 앞으로 되튕김 (공격력 80% · 0.2초에 최대 4발)
    void OnFmBlocked(Vector3 at)
    {
        if (!E("be.zero")) return;
        if (Time.time > fmBlockWinAt) { fmBlockWinAt = Time.time + 0.2f; fmBlockWin = 0; }
        if (fmBlockWin >= 4) return;
        fmBlockWin++;
        List<Transform> near = Nearest(at, 16f, 1);
        if (near.Count == 0 || near[0] == null) return;
        Fx.Stretch(Fx.Frames("fx_rubble").Length > 0 ? "fx_rubble" : "fx_sparkle", at, near[0].position, 0.5f, new Color(1f, 0.9f, 0.5f), 0.2f, 14, 20f);
        Deal(near[0].gameObject, Atk * 0.8f, (near[0].position - at).normalized, 1f);
    }

    // ---------------- 맞는 피해: 안전 제일 (밀고 있는 동안 감소 · 무재해 100일은 두 배)
    float FmTaken(float dmg)
    {
        int ls = L("b.safety");
        if (ls > 0 && Kit != null && (Kit.FmHolding || Kit.FmPileCount > 0))
            dmg *= 1f - Mathf.Min(0.6f, V(ls, 0.08f, 0.04f) * (E("be.zero") ? 2f : 1f) * P("b.safety"));
        return dmg;
    }

    // ---------------- 매 프레임: 야근 수당 · 롤러 자국
    void FmTick()
    {
        CharacterKit k = Kit;
        if (k == null || player == null) return;
        // 야근 수당: 무게 4 이상인 더미를 밀고 있는 동안 필살기 게이지가 참
        int lo = L("b.overtime");
        if (lo > 0 && k.FmHolding && k.FmMass >= 4 && player.skillGauge != null)
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * V(lo, 0.005f, 0.0025f) * Time.deltaTime * P("b.overtime"));
        // 롤러 자국: 무게 6 이상인 더미를 밀면 0.3초마다 자국이 남아 밟은 적을 느리게 함
        int lr = L("b.roller");
        if (lr <= 0) return;
        bool pile = E("be.pile");
        if (k.FmHolding && k.FmMass >= 6 && Time.time >= fmRollerAt)
        {
            fmRollerAt = Time.time + 0.3f;
            while (fmRollers.Count >= (pile ? 40 : 24)) fmRollers.RemoveAt(0);
            fmRollers.Add((k.FmCenter, Time.time + 1.5f * (pile ? 2f : 1f)));
            Fx.Spawn("fx_roller", k.FmCenter, pile ? 5f : 2.8f, Tint("b.roller", new Color(1f, 1f, 1f, 0.7f)), 12f, Mathf.Atan2(k.FmBlade.y, k.FmBlade.x) * Mathf.Rad2Deg, 5);
        }
        if (fmRollers.Count > 0 && Time.time >= fmRollTickAt)
        {
            fmRollTickAt = Time.time + 0.25f;
            float slow = 1f - Mathf.Min(0.8f, V(lr, 0.2f, 0.1f) * P("b.roller"));
            float r = pile ? 2.4f : 1.3f;
            for (int i = fmRollers.Count - 1; i >= 0; i--)
            {
                if (Time.time >= fmRollers[i].until) { fmRollers.RemoveAt(i); continue; }
                Specials.Overlap(fmRollers[i].p, r, fmRollHits);
                foreach (Collider2D c in fmRollHits)
                    if (c != null && c.CompareTag("enermy") && c.TryGetComponent(out EnermyController e) && e.holder == null && !e.IsDead) e.Slow(slow, 0.4f);
            }
        }
    }

    string FmStack(string key)
    {
        if (key == "b.later" && Kit != null && L(key) > 0)
        {
            float p = E("be.release") ? 1f : Mathf.Clamp01((Kit.FmDumpCount - fmLaterBase) / (float)FmLaterNeed(L(key)));
            return Mathf.RoundToInt(p * 100f) + "%";
        }
        return null;
    }
}
