using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 장의사 고유 스킬 (2.0.0) · 패시브(묘지기의 장화는 여기서 · 조의금 · 밤샘 · 부장품은 CharacterKit.Undertaker 가 직접)
//   y.lid 관 뚜껑 · y.trench 도랑 파기 · y.hand 유령 손 · y.wall 흙담 · y.bier 운구
//   y.mourn 추모 · y.empty 빈 무덤 · y.notice 부고 · y.ledger 명부 · y.alive 산 자의 무덤
// 진화: ye.mass 집단 매장 · ye.keeper 묘지기의 담 · ye.catacomb 지하 묘도 · ye.state 국장 · ye.robber 무덤 털이 · ye.haven 안식처 · ye.antlion 개미지옥
// 훅은 CharacterKit.Undertaker 가 부름 (구멍 · 묘비는 Kit.UtGraveList 로 읽음)
//   갈래 5개(Paths/Paths.Undertaker.cs · CharacterKit.Undertaker.Paths.cs)가 이 훅의 값을 채움 (UtNoRest · UtMassFuneral · UtFuneralPad · UtGraveLifeExtra · 비문 +10%)
public partial class SignatureSkills
{
    bool IsUt => who == CharacterId.Undertaker;

    public static int UtLv(string key) => Instance != null && Instance.IsUt ? Instance.L(key) : 0;
    public static bool UtEvo(string key) => Instance != null && Instance.IsUt && Instance.E(key);

    float utHandAt, utNoticeAt, utAliveCd;
    int utLedger;
    const int UtLedgerMax = 25;

    // ---------------- 정적 훅 (CharacterKit.Undertaker 가 부름)
    public static void UtDugStart(Vector2 c, float rMax) { }
    public static void UtDugDone(Vector2 pos, float r, float rMax) { }
    public static bool UtNoRest() => Instance != null && Instance.IsUt && Instance.Kit != null && Instance.Kit.UtNoRestNow;
    public static float UtTrapExtra() => 0f;
    public static float UtTrenchLen() => Instance != null && Instance.IsUt ? V(Instance.L("y.trench"), 3f, 1f) * (UtEvo("ye.catacomb") ? 1f : 1f) : 0f;
    public static void UtTrenchMade(Vector2 a, Vector2 b) { if (Instance != null && Instance.IsUt) Instance.Flair("y.trench", (Vector3)((a + b) * 0.5f), 3f, new Color(0.72f, 0.55f, 0.36f)); }
    public static void UtPitSprung(Vector2 pos) { }
    public static void UtPitEnded(Vector2 pos) { }
    public static float UtBuryMul(Vector2 pos, int trapped, bool boss) => 1f;
    public static int UtGraveHpExtra() => 0;
    public static float UtGraveLifeExtra() => Instance != null && Instance.IsUt && Instance.Kit != null ? Instance.Kit.UtPathGraveLife : 0f;
    public static void UtGraveBroke(Vector2 pos, bool temp) { }
    public static void UtGraveExpired(Vector2 pos) { }
    public static void UtBossBroke(Vector2 pos) { }
    public static float UtFuneralPad() => UndertakerPaths.On(UndertakerPaths.Funeral) ? UndertakerPaths.FuneralRangePad : 0f;
    public static bool UtMassFuneral() => UndertakerPaths.CrossIs(UndertakerPaths.MassFuneralCross);
    public static void UtFuneralBell(Vector3 at, int fell, int extra) { }

    // 흙담 이음 수 (흙담 2 · 3 · 4 · 6 / 묘지기의 담 5칸까지)
    public static int UtWallLinks()
    {
        if (Instance == null || !Instance.IsUt) return 0;
        int lv = Instance.L("y.wall");
        if (lv <= 0) return 0;
        int n = lv == 1 ? 2 : lv == 2 ? 3 : lv == 3 ? 4 : 6;
        return UtEvo("ye.keeper") ? Mathf.Max(n, 5) : n;
    }

    // 명부: 다음 장례의 피해 배율 (한 번 쓰면 비움)
    public static float UtLedgerTake()
    {
        if (Instance == null || !Instance.IsUt) return 1f;
        int lv = Instance.L("y.ledger");
        float m = lv > 0 ? 1f + Instance.utLedger * V(lv, 0.02f, 0.01f) : 1f;
        Instance.utLedger = 0;
        return m;
    }

    public static void UtBuryStart(int holes, Vector2 aim) { if (Instance != null && Instance.IsUt) Instance.OnUtBuryStart(aim); }
    public static void UtBuryEnd(int holes, int trappedKilled, bool fromUlt)
    {
        if (Instance == null || !Instance.IsUt) return;
        if (!fromUlt) UndertakerPaths.BuryGauge(trappedKilled);       // 상주: 묻은 적마다 게이지 +0.5%
        Instance.OnUtBuryEnd(trappedKilled, fromUlt);
    }
    public static void UtBuriedHole(Vector2 pos, float r, int trapped, int killed, bool fromUlt) { if (Instance != null && Instance.IsUt) Instance.OnUtBuriedHole(pos, r, trapped, fromUlt); }
    public static void UtEmptyBuried(Vector2 pos) { if (Instance != null && Instance.IsUt) Instance.OnUtEmpty(pos); }
    public static void UtGraveRedug(Vector2 pos) { if (Instance != null && Instance.IsUt) Instance.OnUtRedug(pos); }
    public static void UtGraveRose(Vector2 pos, bool big, bool temp) { }
    public static void UtFuneralEnd(int killed, int graves) { if (Instance != null && Instance.IsUt) Instance.OnUtFuneralEnd(killed); }

    // ---------------- 매장이 시작되는 순간: 운구 (가장 오래된 묘비가 관이 되어 커서 쪽으로 8칸)
    void OnUtBuryStart(Vector2 aim)
    {
        int lv = L("y.bier");
        if (lv <= 0 || Kit == null) return;
        UndertakerGrave g = Kit.UtOldestGrave();
        if (g == null) return;
        Vector2 from = g.Pos;
        Vector2 dir = aim - from;
        dir = dir.sqrMagnitude > 0.25f ? dir.normalized : ((Vector2)(player.transform.position) - from).normalized * -1f;
        Kit.UtTakeGrave(g);
        StartCoroutine(UtCoffin(new Vector3(from.x, from.y, 0f), dir, Atk * V(lv, 1f, 0.4f) * P("y.bier")));
    }

    IEnumerator UtCoffin(Vector3 from, Vector2 dir, float dmg)
    {
        float len = 8f, dur = 0.5f;
        HashSet<int> hit = new HashSet<int>();
        FxAnim a = Fx.Play("fx_coffin", from, 1.6f, Tint("y.bier", Color.white), 8f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 14, true, dur + 0.2f);
        Hostile.Play("thump", 0.4f, 0.6f);
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            Vector3 p = from + (Vector3)(dir * (len * t / dur));
            if (a != null) a.transform.position = p;
            Circle(p, 0.9f, 0f, 0f, c => { if (c != null && hit.Add(c.GetInstanceID())) Deal(c.gameObject, dmg, dir, 3f); });
            yield return null;
        }
        if (a != null) Destroy(a.gameObject);
        Fx.Spawn("fx_grave_break", from + (Vector3)(dir * len), 2f, Color.white, 14f);
        Flair("y.bier", from + (Vector3)(dir * len), 3f, new Color(0.725f, 0.796f, 0.894f));
    }

    // ---------------- 매장이 끝난 순간: 부고 (게이지) · 명부 (쌓기)
    void OnUtBuryEnd(int killed, bool fromUlt)
    {
        if (fromUlt || killed <= 0) return;
        int ln = L("y.notice");
        if (ln > 0 && Time.time >= utNoticeAt && player != null && player.skillGauge != null)
        {
            utNoticeAt = Time.time + 0.6f;
            float pct = Mathf.Min(0.08f, killed * V(ln, 0.004f, 0.002f));
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * pct);
            Flair("y.notice", player.transform.position, 2.5f, new Color(0.725f, 0.796f, 0.894f));
        }
        if (L("y.ledger") > 0) utLedger = Mathf.Min(UtLedgerMax, utLedger + killed);
    }

    // 장례가 끝난 순간: 국장 (쓰러진 적마다 게이지 1% · 최대 25%)
    void OnUtFuneralEnd(int killed)
    {
        if (!E("ye.state") || player == null || player.skillGauge == null || killed <= 0) return;
        player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * Mathf.Min(0.25f, 0.01f * killed));
    }

    // ---------------- 구멍 하나가 덮이는 순간: 관 뚜껑 (한 구멍에 세 마리 넘게)
    void OnUtBuriedHole(Vector2 pos, float r, int trapped, bool fromUlt)
    {
        int lv = L("y.lid");
        if (lv <= 0 || trapped <= 3) return;
        Vector3 p = new Vector3(pos.x, pos.y, 0f);
        float rad = r + 1.5f, dmg = Atk * V(lv, 0.6f, 0.2f) * P("y.lid");
        Circle(p, rad, dmg, 3f);
        Fx.Spawn("fx_shock", p, rad * 2f, Tint("y.lid", new Color(0.725f, 0.796f, 0.894f, 0.8f)), 20f);
        Hostile.Play("thump", 0.5f, 0.5f);
        Flair("y.lid", p, 3f, new Color(0.725f, 0.796f, 0.894f));
        if (E("ye.mass")) StartCoroutine(UtLidAgain(p, rad, dmg));
    }

    IEnumerator UtLidAgain(Vector3 p, float rad, float dmg)
    {
        yield return new WaitForSeconds(0.4f);
        if (!Alive) yield break;
        Circle(p, rad, dmg, 3f);
        Fx.Spawn("fx_shock", p, rad * 2f, new Color(0.725f, 0.796f, 0.894f, 0.8f), 20f);
    }

    // ---------------- 아무도 빠지지 않은 구멍을 덮음: 빈 무덤 (흙을 덮은 함정 · 처음 밟은 적이 빠짐)
    void OnUtEmpty(Vector2 pos)
    {
        int lv = L("y.empty");
        if (lv <= 0 || Kit == null) return;
        Kit.UtAddPit(pos, lv, E("ye.antlion") ? 3 : 1);
    }

    // ---------------- 되파기로 묘비가 무너짐: 무덤 털이 (코인 1개 + 관 하나)
    void OnUtRedug(Vector2 pos)
    {
        if (!E("ye.robber")) return;
        Coin coin = Cache<Coin>.Get;
        if (coin != null) coin.AddCoin(1);
        Transform near = null;
        List<Transform> list = Nearest(new Vector3(pos.x, pos.y, 0f), 10f, 1);
        if (list.Count > 0) near = list[0];
        Vector2 dir = near != null ? ((Vector2)near.position - pos).normalized : Vector2.right;
        float lv = Mathf.Max(1, L("y.bier"));
        StartCoroutine(UtCoffin(new Vector3(pos.x, pos.y, 0f), dir, Atk * V((int)lv, 1f, 0.4f)));
    }

    // ---------------- 맞는 피해: 추모 (묘비 1.5칸 안) · 산 자의 무덤 (체력 30% 아래로 떨어지는 순간)
    float UtTaken(float dmg)
    {
        if (Kit == null || player == null) return dmg;
        int lm = L("y.mourn");
        if (lm > 0 && UtGraveNear(player.transform.position, 1.5f))
            dmg *= 1f - V(lm, 0.08f, 0.04f) * (E("ye.haven") ? 2f : 1f);
        int la = L("y.alive");
        if (la > 0 && Time.time >= utAliveCd && dmg > 0f && player.PlayerHealth - dmg < player.PlayerMaxHealth * 0.3f && player.PlayerHealth > 0f)
        {
            utAliveCd = Time.time + (E("ye.haven") ? 40f : 60f);
            float sec = Mathf.Min(1.5f, V(la, 0.8f, 0.2f));
            player.GrantInvincibility(sec);
            StartCoroutine(UtRise(sec));
            return 0f;          // 이 한 방은 땅속에서 맞지 않음
        }
        return dmg;
    }

    bool UtGraveNear(Vector3 p, float r)
    {
        foreach (UndertakerGrave g in Kit.UtGraveList)
            if (g != null && !g.isWall && Vector2.Distance(g.Pos, p) <= r) return true;
        return false;
    }

    IEnumerator UtRise(float sec)
    {
        Vector3 p = player.transform.position;
        Fx.Spawn("fx_tunnel", p, 3f, Tint("y.alive", Color.white), 14f, 0f, 16);
        Hostile.Play("thump", 0.5f, 0.6f);
        yield return new WaitForSeconds(sec);
        if (!Alive) yield break;
        p = player.transform.position;
        Circle(p, 2f, Atk * 0.5f * P("y.alive"), 9f);          // 나오며 둘레 2칸 적을 밀침
        Fx.Spawn("fx_tunnel", p, 3.4f, Tint("y.alive", Color.white), 14f, 0f, 16);
        Fx.Spawn("fx_shock", p, 4.6f, new Color(0.725f, 0.796f, 0.894f, 0.8f), 20f);
        Flair("y.alive", p, 3f, new Color(0.725f, 0.796f, 0.894f));
        // 안식처: 나온 자리에 큰 묘비
        if (E("ye.haven") && Kit != null) Kit.UtSpawnGrave((Vector2)p + Vector2.right * 1.4f, true, false);
    }

    // ---------------- 적이 받는 피해: 비문 (묘비 둘레 2칸 · +10 / 15 / 20%)
    float UtOutgoing(GameObject target)
    {
        if (Kit == null || target == null) return 1f;
        // 비문 카드 +10 / 15 / 20% + 묘역 설계 공명 I 기본 +10% (더함)
        float bonus = (Kit.card[5] > 0 ? 0.05f * (Kit.card[5] + 1) : 0f)
                    + (UndertakerPaths.On(UndertakerPaths.Yard) && Paths.Res(UndertakerPaths.Yard) >= 1 ? UndertakerPaths.YardRes1Vuln : 0f);
        if (bonus <= 0f) return 1f;
        foreach (UndertakerGrave g in Kit.UtGraveList)
            if (g != null && !g.isWall && Vector2.Distance(g.Pos, target.transform.position) <= 2f)
                return 1f + bonus;
        return 1f;
    }

    float UtMove()
    {
        if (Sp == null || !Sp.Has(SpecialAbilities.KitGraveBoots)) return 1f;
        return Sp.IsEvolved(SpecialAbilities.KitGraveBoots) ? 1.14f : 1.08f;
    }

    // ---------------- 매 프레임: 유령 손 (묘비에서 1초마다 3칸 안 가장 가까운 적의 발목을 잡음)
    void UtTick()
    {
        if (Kit == null || player == null) return;
        int lh = L("y.hand");
        if (lh <= 0 || Time.time < utHandAt) return;
        utHandAt = Time.time + 1f;
        float sec = V(lh, 0.4f, 0.2f) * P("y.hand");
        int hands = E("ye.robber") ? 2 : 1;
        int used = 0;
        foreach (UndertakerGrave g in Kit.UtGraveList)
        {
            if (g == null || g.isWall) continue;
            if (used >= 6) break;
            Vector3 gp = new Vector3(g.Pos.x, g.Pos.y, 0f);
            List<Transform> list = Nearest(gp, 3f, hands);
            foreach (Transform t in list)
            {
                if (t == null || IsBoss(t.gameObject)) continue;
                if (!t.TryGetComponent(out EnermyController e) || e.IsDead || e.holder != null) continue;
                e.Slow(0f, sec);
                Fx.Spawn("fx_hand", t.position, 1.8f, Tint("y.hand", Color.white), 12f, 0f, 15);
                used++;
            }
        }
        if (used > 0) Hostile.Play("whoosh", 0.2f, 0.5f);
    }

    string UtStack(string key)
    {
        if (L(key) <= 0) return null;
        if (key == "y.ledger") return utLedger > 0 ? "x" + utLedger : null;
        if (key == "y.alive") return Time.time < utAliveCd ? Mathf.CeilToInt(utAliveCd - Time.time) + "s" : null;
        return null;
    }
}
