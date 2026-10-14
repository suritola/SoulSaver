using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 우체부 (1.0.2): 저승 배달부
//   평타 · 편지 배달: 마우스 쪽(앞 70°) 사거리 안에서 가장 먼 적을 「받는 이」로 정해 편지를 날림.
//                     편지는 다른 적을 지나쳐 받는 이를 쫓아가고 (쓰러지면 가까운 적으로 다시), 닿으면 배달 완료
//                     배달 10번마다 우편 보너스 (코인 2 · 필살기 게이지 4%)
//   1.0.6~ 하향: 배달 횟수는 평타로 직접 보낸 편지만 셈 (등기 · 반송 · 행운의 편지 · 우체통 · 답장 편지는 안 셈 →
//                파생 편지가 우편 보너스 · 특급 게이지를 끝없이 돌려 속달을 쉬지 않고 쓰던 것), 사거리 14 → 12칸
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
    const float ExtraLetterRate = 0.4f;         // 1.0.6~ 다발 배송 · 총알 배송으로 덧붙은 편지 피해 50% → 40%
    const float RegisteredRate = 0.65f;         // 1.0.6~ 등기 우편 다시 배달 피해 80% → 65% (3회면 한 통이 약 2.95배 → 2.35배)
    const int ReplyCap = 10;                    // 1.0.6~ 답장: 속달 한 번에 최대 10통 (예전엔 표식 수 × 답장 수, 최대 60통)

    void PostAttack(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        int n = Mathf.Max(1, shots) + (augment == AugBundle ? 2 : 0) + SignatureSkills.ExtraLetters();
        List<Transform> taken = new List<Transform>();
        for (int i = 0; i < n; i++)
        {
            Transform to = Addressee(start, dir, taken);
            if (to != null) { taken.Add(to); MarkAddressee(to); }
            float d = dmg * (i >= Mathf.Max(1, shots) ? ExtraLetterRate : 1f);
            // 1.0.2: 받는 이 쪽으로 출발 (앞 70° 밖 · 바로 옆의 적이면 마우스 쪽으로 출발한 편지가 받는 이 둘레를 맴돌기만 하던 것)
            Vector2 aim = to != null ? (Vector2)(to.position - start) : dir;
            if (aim.sqrMagnitude < 0.01f) aim = dir;
            Vector2 launch = Quaternion.Euler(0f, 0f, (i - (n - 1) * 0.5f) * 14f) * aim.normalized;
            bool pigeon = form == SpecialAbilities.KitPigeon && i == 0;
            StartCoroutine(LetterFlight(start, launch, to, d, pigeon, 0));
        }
        SignatureSkills.LettersSent(start, dir, dmg);
        Play("flick", 0.6f, 1f);
        Play("whoosh", 0.25f, 1.7f);
    }

    // 받는 이: 마우스 쪽 앞 70° 안에서 가장 먼 적 (이미 받는 이가 된 적은 빼고), 없으면 그 방향 가장 가까운 적
    // 1.0.6~ 찾는 범위 = 캐릭터 사거리 (14 → 12칸, 화면 끝의 적을 안전한 거리에서 먼저 따던 것)
    Transform Addressee(Vector3 from, Vector2 dir, List<Transform> except)
    {
        float range = def.range > 0f ? def.range : 12f;
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
            // 받는 이가 쓰러지거나 사라지면 바로 가까운 적으로 다시 (예전엔 처음 0.05초 동안 쓰러진 적을 쫓아 시체에 배달하기도 했음)
            if (!Alive(target) && (target != null || t > 0.05f))
                target = Specials.NearestEnemy(p, 8f);
            if (target != null)
            {
                Vector2 to = target.position - p;
                Vector2 want = to.normalized;
                float ang = Vector2.SignedAngle(v, want);
                // 가까워질수록 더 세게 꺾음 (꺾는 반지름보다 가까운 받는 이 둘레를 맴돌기만 하던 것)
                float turn = LetterTurn * (1f + 4f * Mathf.Clamp01(1f - to.magnitude / 4f) + t) * Time.deltaTime;
                v = Quaternion.Euler(0f, 0f, Mathf.Clamp(ang, -turn, turn)) * v;
            }
            p += (Vector3)(v * LetterSpeed * Time.deltaTime);
            if (a != null)
            {
                a.transform.position = p;
                a.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90f + Mathf.Sin(t * 30f) * 12f);
            }
            if (Hostile.IsWall(p)) break;
            // 받는 이 몸에 닿으면 배달 (예전엔 몸 크기와 상관없이 중심 0.74칸 안이어야 해서 큰 적 · 보스는 몸에 닿고도 지나쳤음)
            if (target != null && Touches(target, p) && Alive(target))
            {
                Deliver(target, p, v, dmg, pigeon, depth);
                break;
            }
            yield return null;
        }
        if (a != null) Destroy(a.gameObject);
    }

    static bool Alive(Transform t) => t != null && !(t.TryGetComponent(out EnermyController e) && e.IsDead);

    // 편지가 받는 이 몸(충돌 범위)에 닿았나: 중심 0.74칸 안이거나, 몸 가장자리에서 0.45칸 안
    static bool Touches(Transform t, Vector3 p)
    {
        if (((Vector2)(t.position - p)).sqrMagnitude < 0.55f) return true;
        if (!t.TryGetComponent(out Collider2D col) || !col.enabled) return false;
        return ((Vector2)p - col.ClosestPoint(p)).sqrMagnitude < 0.2f;
    }

    // 받는 이 표시: 편지를 보낼 때 받는 이에게 붉은 조준 표식이 잠깐 (누구에게 가는지 보이게)
    void MarkAddressee(Transform to)
    {
        FxAnim r = Fx.Play("fx_reticle", to.position, 1.3f, new Color(1f, 0.45f, 0.4f, 0.95f), 12f, 0f, 50, true, 0.45f);
        if (r != null) { r.follow = to; r.spin = 220f; }
    }

    // 배달 완료
    void Deliver(Transform target, Vector3 at, Vector2 v, float dmg, bool pigeon, int depth)
    {
        GameObject g = target.gameObject;
        // 1.0.6~ 우표 중첩당 +4/8/12% → +3/6/9% (맞으면 절반 떨어짐 · LoseStamps)
        float m = formPower * (1f + 0.03f * card[2] * stampStacks) * SignatureSkills.LetterMul(g, target == lastAddressee, (at - transform.position).magnitude);
        if (augment == AugThickEnvelope) m *= 1.25f;
        bool wasAlive = !(g.TryGetComponent(out EnermyController e) && e.IsDead);
        Specials.Damage(g, dmg * m, v, 0.8f);
        if (pigeon) StartCoroutine(PigeonPeck(target, dmg * 0.5f));
        lastAddressee = target;
        // 1.0.6~ 배달 횟수 · 우편 보너스는 평타로 직접 보낸 편지(depth 0)만
        if (depth == 0)
        {
            Deliveries++;
            if (Deliveries % 10 == 0) PostBonus(at);
        }
        if (card[2] > 0) stampStacks = Mathf.Min(StampMax, stampStacks + 1);
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
            if (next != null) StartCoroutine(LetterFlight(at, ((Vector2)(next.position - at)).normalized, next, dmg * RegisteredRate, false, depth + 1));
        }
        // 반송: 받는 이가 쓰러지면 다른 적에게 한 번 더
        if (killed && card[6] > 0 && depth < 1 + SignatureSkills.ReturnDepth())
        {
            Transform next = NearestOther(at, 9f, target, v, 360f);
            if (next != null) StartCoroutine(LetterFlight(at, ((Vector2)(next.position - at)).normalized, next, dmg * (0.3f + 0.2f * card[6]), false, depth + 1));
        }
        Play("stamp", 0.35f, 1.3f);
        SignatureSkills.Delivered(g, at, killed, depth);
    }

    // 우표 수집 (1.0.6~): 맞으면 쌓인 우표가 절반 떨어짐 (예전엔 한 번 쌓이면 끝까지 +120% · 수집가 +240%)
    public void LoseStamps() => stampStacks /= 2;

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
        if (player.skillGauge != null) player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * 0.04f);      // 1.0.6~ 5% → 4%
        SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 1.6f, Loc.T("배달 완료!") + " ×" + Deliveries, PostRed, 4f, 0.5f);
        Play("chime", 0.5f, 1.5f);
        SignatureSkills.PostBonus();
    }

    IEnumerator PigeonPeck(Transform t, float dmg)
    {
        for (int i = 0; i < 2; i++)
        {
            yield return new WaitForSeconds(0.2f);
            if (!Alive(t)) yield break;
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
            if (target != null) StartCoroutine(LetterFlight(at + Vector3.up * 0.6f, ((Vector2)(target.position - at)).normalized, target, Damage * 0.6f, false, 9));      // 형태 숙련(formPower)은 배달할 때 곱함 (예전엔 두 번 곱했음)
            yield return new WaitForSeconds(0.6f);
        }
        if (box != null) Destroy(box.gameObject);
    }

    // ================================================================= 우클릭: 속달
    bool expressing;
    float expressCharge;
    readonly List<Transform> expressMarks = new List<Transform>();
    readonly List<FxAnim> expressIcons = new List<FxAnim>();        // expressMarks 와 같은 순서 (표식 아이콘, 없으면 null)
    readonly List<FxAnim> expressPending = new List<FxAnim>();      // 떼고 터지기 전 1초 동안의 아이콘 (캐릭터가 사라져도 지우게)
    LineRenderer expressRing;
    const float ExpressRange = 15f;

    int ExpressMaxAt(float charge) => Mathf.RoundToInt((5 + 10 * charge) * (augment == AugRegistered ? 1.5f : 1f));      // 1.0.2 조정: 8 ~ 30 → 6 ~ 20, 1.0.6~ 6 ~ 20 → 5 ~ 15 (등기 표식 9 ~ 30 → 8 ~ 22)
    int ExpressMax => ExpressMaxAt(expressCharge);
    // 머리 위 막대: 속달을 누르는 동안 붙은 표식 수 / 끝까지 눌렀을 때 최대
    public bool Expressing => expressing;
    public float ExpressFill => expressMarks.Count / (float)Mathf.Max(1, ExpressMaxAt(1f));

    // 표식 아이콘: 적 머리 위에서 두근거리고, 오래 누를수록 커짐 (FxAnim.follow 는 높이를 못 줘서 직접 옮김)
    static void PlaceExpressIcon(FxAnim icon, Transform t, float k, float scale)
    {
        if (icon == null || t == null) return;
        icon.transform.position = t.position + Vector3.up * 1.2f;
        icon.transform.localScale = Vector3.one * scale * (1f + 0.45f * k + 0.12f * Mathf.Sin(Time.time * 14f));
    }

    void UpdateExpress(SkillGauge gauge, bool full)
    {
        PostboxTick();
        if (!expressing && GameInput.UltDown && full)
        {
            expressing = true;
            expressCharge = 0f;
            ClearExpressIcons(expressIcons);
            expressMarks.Clear();
            Play("hum", 0.5f, 1.4f);
        }
        if (!expressing) return;
        expressCharge = Mathf.Min(1f, expressCharge + Time.deltaTime / 1.2f);
        Vector3 me = transform.position;
        // 1.0.2: 둘레 고리를 스킬 이펙트(+40) 위 순서로 · 더 굵고 진하게, 다 차면 금빛으로
        if (expressRing == null) { expressRing = Hostile.NewLine("ExpressRing", PostRed, 0.16f, 55); expressRing.loop = true; }
        expressRing.enabled = true;
        Hostile.SetArc(expressRing, me, ExpressRange, 0f, 360f);
        Color rc = expressCharge >= 1f ? Color.Lerp(PostRed, new Color(1f, 0.85f, 0.4f), 0.6f) : PostRed;
        expressRing.startColor = expressRing.endColor = new Color(rc.r, rc.g, rc.b, 0.5f + 0.3f * Mathf.Sin(Time.time * 12f));
        expressRing.startWidth = expressRing.endWidth = 0.16f + 0.08f * expressCharge;
        // 1.0.2: 쓰러지거나 사라진 적의 표식은 떼고 아이콘도 지움 (예전엔 쓰러진 적이 표식 자리를 차지해 새 적에게 안 붙었음)
        for (int i = expressMarks.Count - 1; i >= 0; i--)
        {
            if (Alive(expressMarks[i])) continue;
            if (i < expressIcons.Count) { if (expressIcons[i] != null) Destroy(expressIcons[i].gameObject); expressIcons.RemoveAt(i); }
            expressMarks.RemoveAt(i);
        }
        // 표식: 가까운 적부터 하나씩
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
                // 1.0.2: 수명 5초 → 직접 지울 때까지 (5초 넘게 누르면 표식 아이콘이 사라졌음), 스킬 이펙트 위 순서
                FxAnim icon = Fx.Play("fx_card", best.position + Vector3.up * 1.2f, 0.9f, PostRed, 12f, 0f, 24, true, -1f);
                expressIcons.Add(icon);
                Fx.Spawn("fx_spark", best.position, 0.9f, PostRed, 26f);
                Play("pop", 0.15f, 2.2f + 0.02f * expressMarks.Count);
            }
        }
        for (int i = 0; i < expressIcons.Count && i < expressMarks.Count; i++)
            if (expressIcons[i] != null) PlaceExpressIcon(expressIcons[i], expressMarks[i], expressCharge, IconScale(expressIcons[i]));
        if (AddedUltShown) DrawUltAim(false, true);

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        expressing = false;
        expressRing.enabled = false;
        CancelUltAim();
        float dmg = Damage * (1.8f + 1.4f * expressCharge) * UltMul;       // 1.0.6~ 공격력 200 ~ 400% → 180 ~ 320%
        if (expressMarks.Count > 0)
            SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 1.8f, Loc.T("속달") + " ×" + expressMarks.Count, PostRed, 4.5f, 0f);
        List<FxAnim> icons = new List<FxAnim>(expressIcons);
        expressPending.AddRange(icons);
        StartCoroutine(ExpressRoutine(new List<Transform>(expressMarks), icons, dmg, expressCharge));
        expressMarks.Clear();
        expressIcons.Clear();
        PlayerMotion.Of(this).Throw(0.3f, Vector2.up);
        KitUltAdd(1f, transform.position);
        Spend(gauge);
    }

    // 아이콘의 원래 크기 (처음 만들 때의 크기를 기억)
    readonly Dictionary<FxAnim, float> iconBase = new Dictionary<FxAnim, float>();
    float IconScale(FxAnim icon)
    {
        if (!iconBase.TryGetValue(icon, out float b)) iconBase[icon] = b = icon.transform.localScale.x;
        return b;
    }

    void ClearExpressIcons(List<FxAnim> icons)
    {
        foreach (FxAnim i in icons) if (i != null) { iconBase.Remove(i); Destroy(i.gameObject); }
        icons.Clear();
    }

    IEnumerator ExpressRoutine(List<Transform> marks, List<FxAnim> icons, float dmg, float charge)
    {
        Play("whoosh", 0.8f, 1.2f);
        // 터지기 전 1초: 표식이 적을 따라다니며 점점 빠르게 두근거림
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            for (int i = 0; i < icons.Count && i < marks.Count; i++)
                if (icons[i] != null) PlaceExpressIcon(icons[i], marks[i], charge + t, IconScale(icons[i]));
            yield return null;
        }
        foreach (FxAnim i in icons) expressPending.Remove(i);
        ClearExpressIcons(icons);
        List<Vector3> spots = new List<Vector3>();
        int replies = 0;
        foreach (Transform t in marks)
        {
            if (!Alive(t)) continue;        // 1.0.2: 그사이 쓰러진 적의 시체에서 터지지 않게
            Vector3 p = t.position;
            spots.Add(p);
            Specials.Damage(t.gameObject, dmg, Vector3.zero, 1f);
            // 표식 하나만 맞음 → 판정 범위처럼 보이는 폭발은 적 몸 크기로, 화려함은 장식 불꽃으로 (오래 눌러 셀수록 더 크게)
            Fx.Spawn("fx_explosion", p, 2f, new Color(1f, 0.55f, 0.4f), 20f);
            Fx.Spawn("fx_sparkle", p, 1.6f + 1.2f * charge, new Color(1f, 0.85f, 0.6f), 22f);
            // 답장 (카드): 터진 자리에서 작은 편지가 가까운 적에게
            for (int k = 0; k < card[3] && replies < ReplyCap; k++)
            {
                replies++;
                Transform next = Specials.NearestEnemy(p + (Vector3)(Random.insideUnitCircle * 2f), 8f);
                if (next != null) StartCoroutine(LetterFlight(p, Random.insideUnitCircle.normalized, next, Damage * 0.6f, false, 9));
            }
        }
        if (spots.Count > 0) Hostile.Shake(Mathf.Min(0.35f, 0.1f + 0.01f * spots.Count));
        Play("boom", 0.8f, 1.1f);
        Play("stamp", 0.9f, 0.7f);
        Play("crackle", 0.7f, 1.2f);
        SignatureSkills.Expressed(spots, dmg);
    }

    void OnDestroyPostman()
    {
        if (expressRing != null) Destroy(expressRing.gameObject);
        ClearExpressIcons(expressIcons);
        ClearExpressIcons(expressPending);
    }
}
