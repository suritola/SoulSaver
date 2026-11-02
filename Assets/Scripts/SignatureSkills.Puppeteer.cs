using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 인형사 고유 스킬 (2.0.0) · 패시브(무대 체질 · 긴 팔은 CharacterKit.Puppeteer 가 직접 · 만원 관객 · 텅 빈 몸은 여기서)
//   m.spotlight 무대 조명 · m.twang 현 울림 · m.voice 복화술 · m.stuffing 솜 채우기 · m.tightrope 줄타기
//   m.curtaindrop 막 내림 · m.encore 앙코르 · m.tangle 얽힌 실 · m.tpose T 자세 · m.snap 실 퉁기기
// 진화: me.harp 하프 현 · me.armor 갑옷 인형 · me.quickchange 퀵 체인지 · me.troupe 극단 · me.strings 천 가닥의 실 · me.exit 가르는 퇴장 · me.bodyguard 인형 호위대
// 훅은 CharacterKit.Puppeteer 가 부름 (줄 길이 · 더미 자리는 Kit.PupPos · PupOverstretch 로 읽음)
//   갈래(꼭두각시 포함)의 효과는 CharacterKit.Puppeteer.Paths — 천 가닥의 실 · 인형 호위대의 「꼭두각시」 부분도 거기서 (여기서는 PupHoldVoiced · PupTakeVoicedKill 을 부름)
public partial class SignatureSkills
{
    bool IsPup => who == CharacterId.Puppeteer;

    // ---------------- 값 읽기 (인형사가 아니면 0 / false)
    public static int PupLv(string key) => Instance != null && Instance.IsPup ? Instance.L(key) : 0;
    public static bool PupEvo(string key) => Instance != null && Instance.IsPup && Instance.E(key);

    float pupTwangCool, pupVoiceAt, pupSnapCool, pupStuffAt, pupTightUntil, pupCrowdAt, pupSpotAt;
    bool pupSnapWasOver, pupCrowd;
    readonly List<float> pupTwangTimes = new List<float>(8);
    readonly Dictionary<int, float> pupBindAt = new Dictionary<int, float>();

    // ---------------- 정적 훅 (CharacterKit.Puppeteer 가 부름)
    public static float PupCutWidthMul() => 1f;
    public static float PupSwapCutMul() => PupEvo("me.exit") ? 2f : 1f;           // 가르는 퇴장: 바꿀 때 가르는 피해 두 배
    public static float PupDecoyMul() => 1f;
    // 표식 (갈래: 거울 인형 겹침 · 꼭두각시 얽힘 — 받는 피해 +)
    public static void PupMark(GameObject go, float sec, float mul) { if (Instance != null && Instance.IsPup) Instance.Mark(go, sec, mul); }

    // T 자세: 쉰 시간이 문턱(1.5초 · 갑옷 인형 0.8초)을 넘으면 다음 휘두름이 범위 1.5배 · 피해 +40~100% (눌러 휘두를 때만)
    public static float PupTPose(float rest, out float rMul)
    {
        rMul = 1f;
        if (Instance == null || !Instance.IsPup) return 1f;
        int lv = Instance.L("m.tpose");
        if (lv <= 0 || rest < (Instance.E("me.armor") ? 0.8f : 1.5f)) return 1f;
        rMul = 1.5f;
        return 1f + V(lv, 0.4f, 0.2f) * Instance.P("m.tpose");
    }

    public static void PupSwung(Vector3 at, float r, int hits) { if (Instance != null && Instance.IsPup) Instance.OnPupSwung(at, r, hits); }
    public static void PupSwingKill(Vector3 pos) { if (Instance != null && Instance.IsPup) Instance.OnPupSwingKill(pos); }
    public static void PupCut(GameObject go, Vector3 at, Vector2 along) { if (Instance != null && Instance.IsPup) Instance.OnPupCut(go, at, along); }
    public static void PupSwapped(Vector3 from, Vector3 to, int cut) { if (Instance != null && Instance.IsPup) Instance.OnPupSwapped(from, to, cut); }
    public static int PupEncore(out float sec)
    {
        sec = 0f;
        if (Instance == null || !Instance.IsPup) return 0;
        int lv = Instance.L("m.encore");
        if (lv <= 0) return 0;
        sec = V(lv, 3f, 1f);
        return Instance.E("me.troupe") ? 2 : 1;
    }

    // ---------------- 도우미
    void PupLine(Vector3 a, Vector3 b, float w, float dmg, float knock)
    {
        using var list = PhysHits.Circle((a + b) * 0.5f, Vector3.Distance(a, b) * 0.5f + w + 1.5f, true);
        foreach (Collider2D c in list)
        {
            if (c == null || !IsFoe(c)) continue;
            if (c.TryGetComponent(out EnermyController e) && e.IsDead) continue;
            if (Hostile.DistanceToSegment(c.transform.position, a, b) > w) continue;
            Deal(c.gameObject, dmg, (b - a).normalized, knock);
        }
    }

    Vector3 PupStringA => player != null ? player.transform.position + Vector3.up * 0.9f : Vector3.zero;

    // ---------------- 휘두름 직후: 무대 조명 (휘두른 자리에 2초 동안 조명 · 안의 적이 받는 피해 +)
    void OnPupSwung(Vector3 at, float r, int hits)
    {
        int ls = L("m.spotlight");
        if (ls <= 0 || hits <= 0 || Time.time < pupSpotAt) return;
        pupSpotAt = Time.time + 0.5f;          // 연속 휘두름이 조명을 끝없이 쌓지 않게
        float mul = 1f + V(ls, 0.08f, 0.04f) * P("m.spotlight");
        Zone(at, r, 2f, 0.25f, z =>
        {
            foreach (Collider2D c in Inside(z)) if (c != null) Mark(c.gameObject, 0.5f, mul);
        }, "fx_spotlight", Tint("m.spotlight", new Color(1f, 0.95f, 0.6f, 0.5f)), r * 2.2f, true);
        Flair("m.spotlight", at, 2.5f, new Color(1f, 0.95f, 0.6f));
    }

    // 솜 채우기: 더미의 휘두름으로 쓰러뜨리면 체력 회복 (0.3초에 한 번 · 인형 호위대 진화는 끌려온 적을 쓰러뜨릴 때 두 배)
    void OnPupSwingKill(Vector3 pos)
    {
        int ls = L("m.stuffing");
        if (ls <= 0 || Time.time < pupStuffAt) return;
        pupStuffAt = Time.time + 0.3f;
        Heal(V(ls, 0.5f, 0.25f) * (E("me.bodyguard") && Kit != null && Kit.PupTakeVoicedKill(pos) ? 2f : 1f) * P("m.stuffing"));      // 인형 호위대: 복화술로 끌려와 붙잡힌 적을 쓰러뜨리면 두 배
        Fx.Spawn("fx_sparkle", pos, 1.2f, new Color(1f, 0.95f, 0.85f), 24f);
    }

    // 줄에 벤 적: 얽힌 실(느려짐) · 실감개 인형(묶기) · 현 울림(0.4초 안에 다섯 이상)
    void OnPupCut(GameObject go, Vector3 at, Vector2 along)
    {
        EnermyController e = go != null ? go.GetComponent<EnermyController>() : null;
        int lt = L("m.tangle");
        if (lt > 0 && e != null && !e.IsDead && e.holder == null)
            e.Slow(1f - Mathf.Min(0.8f, V(lt, 0.15f, 0.05f) * P("m.tangle")), 1.5f);
        if (e != null && !e.IsDead && e.holder == null && Kit != null && Kit.form == SpecialAbilities.KitSpoolDummy && !e.IsMidBoss)
        {
            int id = e.GetInstanceID();
            if (!pupBindAt.TryGetValue(id, out float t) || Time.time >= t)
            {
                pupBindAt[id] = Time.time + 3f;
                e.Slow(0f, 0.6f);
                Fx.Spawn("fx_sparkle", at, 1.4f, new Color(0.8f, 0.85f, 1f), 24f);
                if (pupBindAt.Count > 80) pupBindAt.Clear();
            }
        }
        int lw = L("m.twang");
        if (lw <= 0) return;
        pupTwangTimes.Add(Time.time);
        while (pupTwangTimes.Count > 0 && Time.time - pupTwangTimes[0] > 0.4f) pupTwangTimes.RemoveAt(0);
        if (pupTwangTimes.Count < 5 || Time.time < pupTwangCool) return;
        pupTwangCool = Time.time + 1f;
        pupTwangTimes.Clear();
        PupTwangWave(lw, 0f);
        if (E("me.harp")) StartCoroutine(PupTwangLater(lw, 0.3f));
    }

    IEnumerator PupTwangLater(int lw, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (Alive) PupTwangWave(lw, 0f);
    }

    // 현 울림: 줄을 따라 파동이 한 번 퍼짐 (굵게)
    void PupTwangWave(int lw, float extra)
    {
        if (Kit == null) return;
        Vector3 a = PupStringA, b = new Vector3(Kit.PupPos.x, Kit.PupPos.y, 0f);
        PupLine(a, b, 1.1f, Atk * V(lw, 0.6f, 0.2f) * P("m.twang"), 1.5f);
        Fx.Stretch(Fx.Frames("fx_string_twang").Length > 0 ? "fx_string_twang" : "fx_shock", a, b, 1.6f, Tint("m.twang", new Color(0.8f, 0.85f, 1f, 0.9f)), 0.3f, 15, 24f);
        Hostile.Play("whoosh", 0.4f, 1.6f);
        Flair("m.twang", (a + b) * 0.5f, 3f, new Color(0.7f, 0.8f, 1f));
        Impact.Hit(ImpactTier.Skill, (a + b) * 0.5f);
    }

    // 자리 바꾼 직후: 줄타기(이동 속도 · 피해 감소) · 막 내림(떠난 자리의 적 투사체 지우기) · 가르는 퇴장(실 퉁기기가 저절로)
    void OnPupSwapped(Vector3 from, Vector3 to, int cut)
    {
        if (L("m.tightrope") > 0) pupTightUntil = Time.time + 1.5f;
        int lc = L("m.curtaindrop");
        if (lc > 0)
        {
            float r = V(lc, 2f, 0.5f) * (E("me.quickchange") ? 2f : 1f) * Fs("m.curtaindrop");
            for (int i = HostileProjectile.Live.Count - 1; i >= 0; i--)
            {
                HostileProjectile hp = HostileProjectile.Live[i];
                if (hp == null) continue;
                if (((Vector2)(hp.transform.position - from)).sqrMagnitude > r * r) continue;
                Fx.Spawn("fx_spark", hp.transform.position, 1.2f, new Color(0.9f, 0.4f, 0.4f), 24f);
                Destroy(hp.gameObject);
            }
            Fx.Spawn(Fx.Frames("fx_curtain_drop").Length > 0 ? "fx_curtain_drop" : "fx_shock", from, r * 1.6f, Tint("m.curtaindrop", Color.white), 16f);
            Flair("m.curtaindrop", from, 2.5f, new Color(0.9f, 0.4f, 0.4f));
        }
        if (E("me.exit") && L("m.snap") > 0) PupSnapBurst();
    }

    // 실 퉁기기: 줄 전체를 한 번에 벰
    void PupSnapBurst()
    {
        int ls = L("m.snap");
        if (ls <= 0 || Kit == null) return;
        Vector3 a = PupStringA, b = new Vector3(Kit.PupPos.x, Kit.PupPos.y, 0f);
        PupLine(a, b, 0.9f, Atk * V(ls, 0.8f, 0.3f) * P("m.snap"), 1.5f);
        Fx.Stretch(Fx.Frames("fx_string_twang").Length > 0 ? "fx_string_twang" : "fx_shock", a, b, 1.8f, Tint("m.snap", new Color(1f, 1f, 1f, 0.95f)), 0.35f, 15, 24f);
        Hostile.Play("whoosh", 0.5f, 1.8f);
        Hostile.Shake(0.1f);
        Flair("m.snap", (a + b) * 0.5f, 3f, new Color(0.8f, 0.85f, 1f));
        Impact.Hit(ImpactTier.Skill, (a + b) * 0.5f);
    }

    // ---------------- 맞는 피해: 줄타기(바꾼 뒤 1.5초 −30%) · 텅 빈 몸(더미가 6칸 밖이면 −10%)
    float PupTaken(float dmg)
    {
        if (Time.time < pupTightUntil) dmg *= 0.7f;
        if (Sp != null && Sp.Has(SpecialAbilities.KitHollow) && Kit != null && Kit.PupDistance > 6f)
            dmg *= Sp.IsEvolved(SpecialAbilities.KitHollow) ? 0.82f : 0.9f;
        return dmg;
    }

    float PupMove() => Time.time < pupTightUntil ? 1f + V(L("m.tightrope"), 0.12f, 0.06f) * P("m.tightrope") : 1f;

    // 적이 받는 피해: 무대 조명 표식 · 만원 관객(둘레 6칸 적 열 넘게면 주는 피해 +)
    float PupOutgoing(GameObject target)
    {
        float m = MarkMul(target);
        if (pupCrowd && Sp != null && Sp.Has(SpecialAbilities.KitAudience)) m *= Sp.IsEvolved(SpecialAbilities.KitAudience) ? 1.18f : 1.1f;
        return m;
    }

    // ---------------- 매 프레임: 복화술 · 실 퉁기기 · 만원 관객 셈
    void PupTick()
    {
        CharacterKit k = Kit;
        if (k == null || player == null) return;
        // 만원 관객: 둘레 6칸 적 수를 0.25초마다 셈
        if (Sp != null && Sp.Has(SpecialAbilities.KitAudience) && Time.time >= pupCrowdAt)
        {
            pupCrowdAt = Time.time + 0.25f;
            int n = 0;
            foreach (Collider2D c in Specials.Overlap(player.transform.position, 6f, hits)) if (c != null && c.CompareTag("enermy")) n++;
            pupCrowd = n > 10;
        }
        // 복화술: 4초마다 더미가 「여기야!」 외쳐 둘레 4칸의 적을 더미 쪽으로 끌어당김 (보스 제외)
        int lv = L("m.voice");
        if (lv > 0 && Time.time >= pupVoiceAt)
        {
            Vector3 at = new Vector3(k.PupPos.x, k.PupPos.y, 0f);
            List<Transform> nearList = Nearest(at, 4f, 1);
            Transform near = nearList.Count > 0 ? nearList[0] : null;
            if (near != null)
            {
                pupVoiceAt = Time.time + 4f;
                float pull = V(lv, 1.5f, 0.5f) * P("m.voice") * Fs("m.voice");
                bool guard = E("me.bodyguard");
                Circle(at, 4f, 0f, 0f, c =>
                {
                    if (c == null || IsBoss(c.gameObject)) return;
                    if (!c.TryGetComponent(out EnermyController e) || e.holder != null || e.IsDead) return;
                    Vector3 np = Vector3.MoveTowards(e.transform.position, at, pull);
                    if (!Hostile.IsWall(np)) e.transform.position = Hostile.ClampArena(np);
                    if (guard) k.PupHoldVoiced(e);       // 인형 호위대: 끌려온 적은 1초 동안 꼭두각시처럼 영웅을 치지 않음 (EnemyHold)
                });
                Fx.Spawn("fx_shock", at, 8f, Tint("m.voice", new Color(0.8f, 0.85f, 1f, 0.5f)), 20f);
                Hostile.Play("chime", 0.35f, 1.3f);
                if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.FloatText(at + Vector3.up * 2.4f, Loc.T("여기야!"), Color.white, 5f, 0f, 1f);
                Flair("m.voice", at, 3f, new Color(0.8f, 0.85f, 1f));
            }
            else pupVoiceAt = Time.time + 0.5f;       // 끌 적이 없으면 곧 다시 살핌
        }
        // 실 퉁기기: 커서를 줄 길이보다 멀리 끌면 「팅」 (1.5초에 한 번 · 끌고 있는 동안 한 번만)
        if (L("m.snap") > 0)
        {
            float over = k.PupOverstretch;
            if (over > 0.8f && !pupSnapWasOver && Time.time >= pupSnapCool)
            {
                pupSnapCool = Time.time + 1.5f;
                PupSnapBurst();
            }
            pupSnapWasOver = over > 0.3f;
        }
    }

    string PupStack(string key)
    {
        if (Kit == null || L(key) <= 0) return null;
        if (key == "m.tpose") return Kit.PupRestTime >= (E("me.armor") ? 0.8f : 1.5f) ? "!" : null;
        if (key == "m.snap") return Time.time < pupSnapCool ? (pupSnapCool - Time.time).ToString("0.0") + "s" : null;
        return null;
    }
}
