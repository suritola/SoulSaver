using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 우체부 고유 스킬 (1.0.2) · 패시브(튼튼한 다리 · 단골 손님 · 우편 요금 · 개 조심)
//   p.express 특급 · p.postmark 소인 · p.bag 우편 가방 · p.chain 행운의 편지 · p.route 배달 경로
//   p.receipt 영수증 · p.weather 궂은 날씨 · p.overnight 익일 배송 · p.address 정확한 주소 · p.junk 광고 전단
// 진화: pe.registered 내용증명 · pe.parcel 대형 소포 · pe.collector 우표 수집가 · pe.reply 답장 폭주
//       pe.express 총알 배송 · pe.return 반송 불가 · pe.flyers 전단 폭탄
public partial class SignatureSkills
{
    static readonly Color PostRedC = new Color(0.95f, 0.35f, 0.3f);
    static readonly Color PaperC = new Color(1f, 0.96f, 0.85f);
    const float ReceiptGap = 0.3f;       // 1.0.6~ 영수증 회복 간격
    float receiptAt;

    bool IsPost => who == CharacterId.Postman;

    // ---------------- 훅 (CharacterKit.Postman 이 부름)
    public static int StampMax() => Instance != null && Instance.IsPost && Instance.E("pe.collector") ? 15 : 10;      // 1.0.6~ 우표 수집가 20 → 15중첩
    public static int ExtraLetters() => Instance != null && Instance.IsPost && Instance.E("pe.express") && Instance.Kit != null && Instance.Kit.Moving ? 1 : 0;
    public static float ParcelMul() => Instance != null && Instance.IsPost && Instance.E("pe.parcel") ? 2f : 1f;
    public static int ReturnDepth() => Instance != null && Instance.IsPost && Instance.E("pe.return") ? 1 : 0;       // 1.0.6~ 반송 불가: 최대 3번 → 2번
    public static float LetterMul(GameObject target, bool sameAsLast, float dist) => Instance != null && Instance.IsPost ? Instance.OnLetterMul(sameAsLast, dist) : 1f;
    public static void LettersSent(Vector3 start, Vector2 dir, float dmg) { if (Instance != null && Instance.IsPost) Instance.OnLettersSent(start, dir, dmg); }
    public static void Delivered(GameObject g, Vector3 at, bool killed, int depth) { if (Instance != null && Instance.IsPost) Instance.OnDelivered(g, at, killed, depth); }
    public static void PostBonus() { if (Instance != null && Instance.IsPost) Instance.OnPostBonus(); }
    public static void Expressed(List<Vector3> spots, float dmg) { if (Instance != null && Instance.IsPost) Instance.OnExpressed(spots, dmg); }

    float OnLetterMul(bool sameAsLast, float dist)
    {
        float m = 1f;
        if (L("p.bag") > 0 && Kit != null && Kit.Moving) m *= 1f + V(L("p.bag"), 0.1f, 0.05f);
        if (L("p.route") > 0 && !sameAsLast) m *= 1f + V(L("p.route"), 0.1f, 0.05f);         // 1.0.6~ +15/22/29/36% → +10/15/20/25% (편지가 여러 통이면 거의 늘 켜져 있었음)
        if (L("p.weather") > 0 && player != null && player.PlayerHealth < player.PlayerMaxHealth * 0.5f) m *= 1f + V(L("p.weather"), 0.2f, 0.1f);
        if (L("p.address") > 0 && dist > 8f) m *= 1f + V(L("p.address"), 0.2f, 0.1f);
        return m;
    }

    void OnLettersSent(Vector3 start, Vector2 dir, float dmg)
    {
        // 광고 전단: 20% 확률로 부채꼴 전단 셋 (전단 폭탄: 맞으면 터짐)
        if (L("p.junk") <= 0 || Random.value >= 0.2f) return;
        for (int i = -1; i <= 1; i++)
        {
            Bullet b = Shot(start, Quaternion.Euler(0f, 0f, i * 20f) * dir, Atk * V(L("p.junk"), 0.3f, 0.1f) * P("p.junk"), 1, 22f, 10f, "fx_card", 0.8f * Fs("p.junk"), Tint("p.junk", new Color(1f, 0.85f, 0.5f)), true);
            if (b != null && E("pe.flyers"))
                b.onHitEnemy += (s, c) => { if (c != null) { using var tier = Fx.Tier(3); Circle(c.transform.position, 1.6f, Atk * 0.6f, 1f); Fx.Spawn("fx_explosion", c.transform.position, 3.4f, new Color(1f, 0.8f, 0.4f), 20f); } };       // 1.1.2~ 진화 폭발은 가장 화려한 단계 (전단 피해 ×1.6 은 위 Shot)
        }
    }

    void OnDelivered(GameObject g, Vector3 at, bool killed, int depth)
    {
        // 소인: 받는 피해 + (내용증명: 두 배)
        if (L("p.postmark") > 0 && g != null)
            Mark(g, 4f, 1f + V(L("p.postmark"), 0.08f, 0.04f) * (E("pe.registered") ? 2f : 1f));
        // 영수증: 회복 (1.0.6~ 0.3초에 한 번 — 예전엔 등기 · 반송 · 소포까지 배달마다 회복해 초당 수십을 회복했음)
        if (L("p.receipt") > 0 && Time.time >= receiptAt)
        {
            receiptAt = Time.time + ReceiptGap;
            Heal(V(L("p.receipt"), 0.5f, 0.25f));
        }
        // 우편 요금 (패시브): 확률로 코인 1
        if (Sp != null && Sp.Has(SpecialAbilities.KitPostage) && Random.value < (Sp.IsEvolved(SpecialAbilities.KitPostage) ? 0.1f : 0.05f))
        {
            Coin coin = Cache<Coin>.Get;
            if (coin != null) coin.AddCoin(1);
        }
        // 행운의 편지: 쓰러뜨리면 확률로 새 편지 (대형 소포: 50%)
        if (killed && L("p.chain") > 0 && Kit != null && Random.value < (E("pe.parcel") ? 0.5f : 0.3f))
        {
            int n = L("p.chain") >= 3 ? 3 : 2;
            foreach (Transform t in Nearest(at, 9f, n))
                Kit.SendLetter(at, t, Atk * 0.7f);
            Flair("p.chain", at, 2f, PaperC);
        }
    }

    void OnPostBonus()
    {
        // 특급: 배달 열 번마다 필살기 게이지 (우표 수집가: 1.5배)
        // 1.0.6~ +10/15/20/25% → +8/11/14/17%, 우표 수집가 두 배 → 1.5배 (배달 열 번에 게이지 55% 까지 차 속달을 쉬지 않고 쓰던 것)
        if (L("p.express") > 0 && player != null && player.skillGauge != null)
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * V(L("p.express"), 0.08f, 0.03f) * (E("pe.collector") ? 1.5f : 1f));
    }

    // 우표 수집 (1.0.6~): 맞으면 쌓인 우표가 절반 떨어짐
    void PostHurt(float taken)
    {
        if (taken > 0f && Kit != null) Kit.LoseStamps();
    }

    void OnExpressed(List<Vector3> spots, float dmg)
    {
        // 익일 배송: 1초 뒤 같은 자리가 한 번 더 터짐 (답장 폭주: 그때도 답장 편지)
        if (L("p.overnight") > 0 && spots.Count > 0) StartCoroutine(Overnight(new List<Vector3>(spots), dmg * V(L("p.overnight"), 0.3f, 0.1f) * P("p.overnight")));      // 1.1.2~ 답장 폭주: ×1.5
    }

    IEnumerator Overnight(List<Vector3> spots, float dmg)
    {
        yield return new WaitForSeconds(1f);
        using var tier = EvoFx("p.overnight");
        foreach (Vector3 p in spots)
        {
            Circle(p, 1.8f * Fs("p.overnight"), dmg, 1f);
            Fx.Spawn("fx_explosion", p, 3f * Fs("p.overnight"), Tint("p.overnight", VisualRules.Kit(new Color(1f, 0.6f, 0.45f), new Color(1f, 0.71f, 0.45f))), 20f);
            if (E("pe.reply") && Kit != null)
            {
                Transform t = Specials.NearestEnemy(p, 8f);
                if (t != null) Kit.SendLetter(p, t, Atk * 0.6f);
            }
        }
        if (spots.Count > 0) { Flair("p.overnight", spots[0], 3f, VisualRules.Kit(PostRedC, new Color(0.95f, 0.74f, 0.3f))); Hostile.Play("boom", 0.6f, 1.3f); }
    }

    // ---------------- 공통 훅에서
    float PostOutgoing(GameObject target)
    {
        float m = MarkMul(target);
        if (Sp != null && Sp.Has(SpecialAbilities.KitRegular) && IsBoss(target)) m *= Sp.IsEvolved(SpecialAbilities.KitRegular) ? 1.25f : 1.15f;
        return m;
    }

    float PostTaken(float dmg)
    {
        // 개 조심 (패시브): 둘레 3칸에 적이 넷 이상이면 받는 피해 -
        if (Sp != null && Sp.Has(SpecialAbilities.KitWatchDog) && player != null)
        {
            int n = 0;
            foreach (Collider2D c in Specials.Overlap(player.transform.position, 3f, hits)) if (c != null && IsFoe(c)) n++;
            if (n >= 4) dmg *= Sp.IsEvolved(SpecialAbilities.KitWatchDog) ? 0.82f : 0.9f;
        }
        return dmg;
    }

    float PostMove() => Sp != null && Sp.Has(SpecialAbilities.KitSturdyLegs) ? (Sp.IsEvolved(SpecialAbilities.KitSturdyLegs) ? 1.14f : 1.08f) : 1f;

    string PostStack(string key) => key switch
    {
        "p.express" => Kit != null ? (Kit.Deliveries % 10) + "/10" : null,
        _ => null,
    };
}
