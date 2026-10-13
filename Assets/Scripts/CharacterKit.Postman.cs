using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 우체부 (1.1.2): 저승 배달부
//   평타 · 편지 배달: 마우스 쪽(앞 70°) 사거리 안에서 가장 먼 적을 「받는 이」로 정해 편지를 날림.
//                     편지는 다른 적을 지나쳐 받는 이를 쫓아가고 (쓰러지면 가까운 적으로 다시), 닿으면 배달 완료
//                     배달 10번마다 우편 보너스 (코인 2 · 필살기 게이지 5%)
//   우클릭 · 속달: 누르고 있는 동안 둘레 사거리 안의 적에게 표식이 차례로 붙고 (오래 누를수록 많이),
//                   떼면 1초 뒤 표식이 모두 한꺼번에 터짐
// 레벨업 카드 (card): 0 등기 우편 · 1 소포 · 2 우표 수집 · 3 답장 · 5 빠른 우편 · 6 반송
// 형태: 비둘기 우편 · 소포 상자 · 우체통 / 강화: 두꺼운 봉투 · 다발 배송 · 등기 표식
public partial class CharacterKit
{
    static readonly Color Letter = new Color(1f, 0.96f, 0.85f);
    static readonly Color PostRed = new Color(0.95f, 0.35f, 0.3f);

    public int Deliveries { get; private set; }
    int stampStacks;
    Transform lastAddressee;
    float postboxAt;
    static readonly List<Collider2D> postHits = new List<Collider2D>(64);

    float LetterSpeed => 24f * (1f + 0.2f * card[5]);
    float LetterTurn => 540f * (1f + 0.2f * card[5]);
    int StampMax => SignatureSkills.StampMax();

    void PostAttack(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        int n = Mathf.Max(1, shots) + (augment == AugBundle ? 2 : 0) + SignatureSkills.ExtraLetters();
        List<Transform> taken = new List<Transform>();
        for (int i = 0; i < n; i++)
        {
            Transform to = Addressee(start, dir, taken);
            if (to != null) taken.Add(to);
            float d = dmg * (i >= Mathf.Max(1, shots) ? 0.5f : 1f);
            Vector2 launch = Quaternion.Euler(0f, 0f, (i - (n - 1) * 0.5f) * 14f) * dir;
            bool pigeon = form == SpecialAbilities.KitPigeon && i == 0;
            StartCoroutine(LetterFlight(start, launch, to, d, pigeon, 0));
        }
        SignatureSkills.LettersSent(start, dir, dmg);
        Play("whoosh", 0.35f, 1.7f);
        Play("pop", 0.2f, 2f);
    }

    // 받는 이: 마우스 쪽 앞 70° 안에서 가장 먼 적 (이미 받는 이가 된 적은 빼고), 없으면 그 방향 가장 가까운 적
    Transform Addressee(Vector3 from, Vector2 dir, List<Transform> except)
    {
        float range = 14f;
        Transform far = null, any = null;
        float farD = -1f, anyD = float.MaxValue;
        foreach (Collider2D c in Specials.Overlap(from, range, postHits))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss")) || except.Contains(c.transform)) continue;
            if (c.TryGetComponent(out EnermyController e) && e.IsDead) continue;
            Vector2 to = c.transform.position - from;
            float d = to.magnitude;
            if (Vector2.Angle(dir, to) <= 70f) { if (d > farD) { farD = d; far = c.transform; } }
            else if (d < anyD) { anyD = d; any = c.transform; }
        }
        return far != null ? far : any;
    }

    IEnumerator LetterFlight(Vector3 from, Vector2 dir, Transform target, float dmg, bool pigeon, int depth)
    {
        bool parcel = form == SpecialAbilities.KitParcel;
        FxAnim a = Fx.Play(pigeon ? "fx_falcon" : "fx_card", from, pigeon ? 1.4f : (parcel ? 1.1f : 0.9f), pigeon ? new Color(0.9f, 0.9f, 1f) : (parcel ? new Color(0.8f, 0.6f, 0.4f) : SkinFx.Tint(Letter)), 16f, 0f, 16, true, 3f);
        Vector3 p = from;
        Vector2 v = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.right;
        float life = 1.6f;
        for (float t = 0f; t < life; t += Time.deltaTime)
        {
            if (target == null || (target.TryGetComponent(out EnermyController te) && te.IsDead))
                target = t > 0.05f ? Specials.NearestEnemy(p, 8f) : target;
            if (target != null)
            {
                Vector2 want = ((Vector2)(target.position - p)).normalized;
                float ang = Vector2.SignedAngle(v, want);
                v = Quaternion.Euler(0f, 0f, Mathf.Clamp(ang, -LetterTurn * Time.deltaTime, LetterTurn * Time.deltaTime)) * v;
            }
            p += (Vector3)(v * LetterSpeed * Time.deltaTime);
            if (a != null)
            {
                a.transform.position = p;
                a.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90f + Mathf.Sin(t * 30f) * 12f);
            }
            if (Hostile.IsWall(p)) break;
            if (target != null && (target.position - p).sqrMagnitude < 0.55f)
            {
                Deliver(target, p, v, dmg, pigeon, depth);
                break;
            }
            yield return null;
        }
        if (a != null) Destroy(a.gameObject);
    }

    // 배달 완료
    void Deliver(Transform target, Vector3 at, Vector2 v, float dmg, bool pigeon, int depth)
    {
        GameObject g = target.gameObject;
        float m = formPower * (1f + 0.04f * card[2] * stampStacks) * SignatureSkills.LetterMul(g, target == lastAddressee, (at - transform.position).magnitude);
        if (augment == AugThickEnvelope) m *= 1.25f;
        bool wasAlive = !(g.TryGetComponent(out EnermyController e) && e.IsDead);
        Specials.Damage(g, dmg * m, v, 0.8f);
        if (pigeon) StartCoroutine(PigeonPeck(target, dmg * 0.5f));
        lastAddressee = target;
        Deliveries++;
        if (card[2] > 0) stampStacks = Mathf.Min(StampMax, stampStacks + 1);
        if (Deliveries % 10 == 0) PostBonus(at);
        Fx.Spawn("fx_sparkle", at, 1f, Letter, 24f);
        // 소포 (카드) · 소포 상자 (형태): 맞은 자리에서 터짐
        if (card[1] > 0 || form == SpecialAbilities.KitParcel)
        {
            float r = (card[1] > 0 ? 0.8f + 0.4f * card[1] : 1.2f) * SignatureSkills.ParcelMul();
            DamageCircle(at, r, dmg * (card[1] > 0 ? 0.5f : 0.6f), 1f);
            Fx.Spawn("fx_explosion", at, r * 2f, new Color(0.9f, 0.7f, 0.45f), 20f);
        }
        bool killed = wasAlive && g != null && g.TryGetComponent(out EnermyController e2) && e2.IsDead;
        // 등기 우편: 가까운 다른 적에게 다시 배달
        if (depth < card[0])
        {
            Transform next = NearestOther(at, 8f, target, v, 360f);
            if (next != null) StartCoroutine(LetterFlight(at, ((Vector2)(next.position - at)).normalized, next, dmg * 0.8f, false, depth + 1));
        }
        // 반송: 받는 이가 쓰러지면 다른 적에게 한 번 더
        if (killed && card[6] > 0 && depth < 1 + SignatureSkills.ReturnDepth())
        {
            Transform next = NearestOther(at, 9f, target, v, 360f);
            if (next != null) StartCoroutine(LetterFlight(at, ((Vector2)(next.position - at)).normalized, next, dmg * (0.3f + 0.2f * card[6]), false, depth + 1));
        }
        Play("pop", 0.3f, 1.5f);
        SignatureSkills.Delivered(g, at, killed, depth);
    }

    // 다른 효과가 보내는 편지 (행운의 편지 · 답장 폭주): 다시 배달 · 반송은 하지 않음
    public void SendLetter(Vector3 from, Transform to, float dmg)
    {
        if (to != null) StartCoroutine(LetterFlight(from, ((Vector2)(to.position - from)).normalized, to, dmg, false, 9));
    }

    // 우체통 행렬 (궁극기): 둘레에 우체통 n 개
    public void Postboxes(int n, float life)
    {
        for (int i = 0; i < n; i++)
        {
            float a = (360f / n * i) * Mathf.Deg2Rad;
            StartCoroutine(Postbox(transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 3f, life));
        }
    }

    // 우편 보너스: 배달 10번마다
    void PostBonus(Vector3 at)
    {
        Coin coin = Cache<Coin>.Get;
        if (coin != null) coin.AddCoin(2);
        if (player.skillGauge != null) player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * 0.05f);
        SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 1.6f, Loc.T("배달 완료!") + " ×" + Deliveries, PostRed, 4f, 0.5f);
        Play("chime", 0.5f, 1.5f);
        SignatureSkills.PostBonus();
    }

    IEnumerator PigeonPeck(Transform t, float dmg)
    {
        for (int i = 0; i < 2; i++)
        {
            yield return new WaitForSeconds(0.2f);
            if (t == null) yield break;
            Specials.Damage(t.gameObject, dmg, Vector3.zero, 0.2f);
            Fx.Spawn("fx_spark", t.position, 0.8f, Color.white, 26f);
        }
    }

    // 우체통 (형태): 3초마다 발밑에 우체통이 서서 4초 동안 가까운 적에게 편지를 던짐
    void PostboxTick()
    {
        if (form != SpecialAbilities.KitPostbox || Time.time < postboxAt) return;
        postboxAt = Time.time + 3f;
        StartCoroutine(Postbox(transform.position, 4f));
    }

    IEnumerator Postbox(Vector3 at, float life)
    {
        FxAnim box = Fx.Play("fx_keycap", at, 1.4f, PostRed, 8f, 0f, 8, true, life);
        for (float t = 0f; t < life; t += 0.6f)
        {
            Transform target = Specials.NearestEnemy(at, 10f);
            if (target != null) StartCoroutine(LetterFlight(at + Vector3.up * 0.6f, ((Vector2)(target.position - at)).normalized, target, Damage * 0.6f * formPower, false, 9));
            yield return new WaitForSeconds(0.6f);
        }
        if (box != null) Destroy(box.gameObject);
    }

    // ================================================================= 우클릭: 속달
    bool expressing;
    float expressCharge;
    readonly List<Transform> expressMarks = new List<Transform>();
    readonly List<FxAnim> expressIcons = new List<FxAnim>();
    LineRenderer expressRing;
    const float ExpressRange = 15f;

    int ExpressMax => Mathf.RoundToInt((8 + 22 * expressCharge) * (augment == AugRegistered ? 1.5f : 1f));

    void UpdateExpress(SkillGauge gauge, bool full)
    {
        PostboxTick();
        if (!expressing && GameInput.UltDown && full)
        {
            expressing = true;
            expressCharge = 0f;
            expressMarks.Clear();
            Play("hum", 0.5f, 1.4f);
        }
        if (!expressing) return;
        expressCharge = Mathf.Min(1f, expressCharge + Time.deltaTime / 1.2f);
        Vector3 me = transform.position;
        if (expressRing == null) { expressRing = Hostile.NewLine("ExpressRing", PostRed, 0.12f, 25); expressRing.loop = true; }
        expressRing.enabled = true;
        Hostile.SetArc(expressRing, me, ExpressRange, 0f, 360f);
        expressRing.startColor = expressRing.endColor = new Color(PostRed.r, PostRed.g, PostRed.b, 0.35f + 0.3f * Mathf.Sin(Time.time * 12f));
        // 표식: 가까운 적부터 하나씩
        expressMarks.RemoveAll(t => t == null);
        if (expressMarks.Count < ExpressMax)
        {
            Transform best = null;
            float bd = float.MaxValue;
            foreach (Collider2D c in Specials.Overlap(me, ExpressRange, postHits))
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss")) || expressMarks.Contains(c.transform)) continue;
                if (c.TryGetComponent(out EnermyController e) && e.IsDead) continue;
                float d = (c.transform.position - me).sqrMagnitude;
                if (d < bd) { bd = d; best = c.transform; }
            }
            if (best != null)
            {
                expressMarks.Add(best);
                FxAnim icon = Fx.Play("fx_card", best.position + Vector3.up * 1.2f, 0.8f, PostRed, 12f, 0f, 24, true, 5f);
                if (icon != null) { icon.follow = best; expressIcons.Add(icon); }
                Play("pop", 0.15f, 2.2f);
            }
        }
        if (AddedUltShown) DrawUltAim(false, true);

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        expressing = false;
        expressRing.enabled = false;
        CancelUltAim();
        float dmg = Damage * (2.5f + 2.5f * expressCharge) * UltMul;
        StartCoroutine(ExpressRoutine(new List<Transform>(expressMarks), new List<FxAnim>(expressIcons), dmg));
        expressMarks.Clear();
        expressIcons.Clear();
        PlayerMotion.Of(this).Throw(0.3f, Vector2.up);
        KitUltAdd(1f, transform.position);
        Spend(gauge);
    }

    IEnumerator ExpressRoutine(List<Transform> marks, List<FxAnim> icons, float dmg)
    {
        Play("whoosh", 0.8f, 1.2f);
        yield return new WaitForSeconds(1f);
        foreach (FxAnim i in icons) if (i != null) Destroy(i.gameObject);
        List<Vector3> spots = new List<Vector3>();
        foreach (Transform t in marks)
        {
            if (t == null) continue;
            Vector3 p = t.position;
            spots.Add(p);
            Specials.Damage(t.gameObject, dmg, Vector3.zero, 1f);
            Fx.Spawn("fx_explosion", p, 3.2f, new Color(1f, 0.55f, 0.4f), 20f);
            // 답장 (카드): 터진 자리에서 작은 편지가 가까운 적에게
            for (int k = 0; k < card[3]; k++)
            {
                Transform next = Specials.NearestEnemy(p + (Vector3)(Random.insideUnitCircle * 2f), 8f);
                if (next != null) StartCoroutine(LetterFlight(p, Random.insideUnitCircle.normalized, next, Damage * 0.6f, false, 9));
            }
        }
        if (spots.Count > 0) Hostile.Shake(Mathf.Min(0.35f, 0.1f + 0.01f * spots.Count));
        Play("boom", 0.8f, 1.1f);
        Play("crackle", 0.7f, 1.2f);
        SignatureSkills.Expressed(spots, dmg);
    }

    void OnDestroyPostman()
    {
        if (expressRing != null) Destroy(expressRing.gameObject);
        foreach (FxAnim i in expressIcons) if (i != null) Destroy(i.gameObject);
    }
}
