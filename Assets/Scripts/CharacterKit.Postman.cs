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
    const float ExtraLetterRate = 0.55f;        // 1.0.6~ 다발 배송 · 총알 배송으로 덧붙은 편지 피해 50% → 40% · 1.1.3: 40% → 55% (무리 화력이 중앙값 −40~60%, 1.0.6 하향의 원인이던 파생 편지 보너스 무한 순환은 따로 막혀 있음)
    const float RegisteredRate = 0.65f;         // 1.0.6~ 등기 우편 다시 배달 피해 80% → 65% (3회면 한 통이 약 2.95배 → 2.35배)
    const int ReplyCap = 10;                    // 1.0.6~ 답장: 속달 한 번에 최대 10통 (예전엔 표식 수 × 답장 수, 최대 60통)

    void PostAttack(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        int n = Mathf.Max(1, shots) + (augment == AugBundle ? 2 : 0) + SignatureSkills.ExtraLetters();
        List<Transform> taken = new List<Transform>();
        // 2026-10-19~ 갈래 입문 (Paths/Paths.Postman.cs): 속달 대가 · 장거리 피해 · 순회 배달 · 무거운 소포
        bool round = Paths.Has(PostmanPaths.Round), heavy = Paths.Has(PostmanPaths.Parcel);
        if (Paths.Has(PostmanPaths.Express)) dmg *= PostmanPaths.ExpressLetterCost;
        if (Paths.Has(PostmanPaths.LongHaul)) dmg *= PostmanPaths.LongDamage * Paths.Power(PostmanPaths.LongHaul);
        // 빈틈 지정: 편지는 받는 이를 따라가는 유도라, 던질 때 커서가 받는 이(보스 · 중간 보스)의 빈틈 위일 때만
        bool aimed = WeakHit.InAim;
        Vector3 cursor = aimed ? Mouse : Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            Transform to = Addressee(start, dir, taken);
            if (to != null) { taken.Add(to); MarkAddressee(to); }
            bool weak = aimed && to != null && WeakSpot.CursorOn(to.gameObject, cursor, 1.3f);
            float d = dmg * (i >= Mathf.Max(1, shots) ? ExtraLetterRate : 1f);
            // 1.0.2: 받는 이 쪽으로 출발 (앞 70° 밖 · 바로 옆의 적이면 마우스 쪽으로 출발한 편지가 받는 이 둘레를 맴돌기만 하던 것)
            Vector2 aim = to != null ? (Vector2)(to.position - start) : dir;
            if (aim.sqrMagnitude < 0.01f) aim = dir;
            Vector2 launch = Quaternion.Euler(0f, 0f, (i - (n - 1) * 0.5f) * 14f) * aim.normalized;
            bool pigeon = form == SpecialAbilities.KitPigeon && i == 0;
            PostRoute route = null;
            if (round || (heavy && !pigeon))
            {
                route = new PostRoute { max = round ? (Paths.Mastered(PostmanPaths.Round) ? PostmanPaths.RoundMasteryMax : PostmanPaths.RoundMax) : 1, first = to, heavy = heavy && !pigeon };
                if (to != null) route.visited.Add(to);
                if (route.heavy)
                {
                    PostParcelCount++;
                    route.big = Paths.Res(PostmanPaths.Parcel) >= 2 && PostParcelCount % 3 == 0;
                }
            }
            StartCoroutine(LetterFlight(start, launch, to, d, pigeon, 0, weak, route));
        }
        SignatureSkills.LettersSent(start, dir, dmg);
        Play("flick", 0.6f, 1f);
        Play("whoosh", 0.25f, 1.7f);
    }

    // 받는 이: 마우스 쪽 앞 70° 안에서 가장 먼 적 (이미 받는 이가 된 적은 빼고), 없으면 그 방향 가장 가까운 적
    // 1.0.6~ 찾는 범위 = 캐릭터 사거리 (14 → 12칸, 화면 끝의 적을 안전한 거리에서 먼저 따던 것)
    Transform Addressee(Vector3 from, Vector2 dir, List<Transform> except)
    {
        float range = PostRange;
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

    //   weak: 던질 때 빈틈을 짚은 편지 — 처음 받는 이에게 닿았을 때 빈틈이 아직 열려 있으면 명중 (받는 이가 바뀌면 지정이 풀림)
    //   route: 순회 배달 · 무거운 소포 (갈래) — 평타로 직접 보낸 편지만, 없으면 null
    IEnumerator LetterFlight(Vector3 from, Vector2 dir, Transform target, float dmg, bool pigeon, int depth, bool weak = false, PostRoute route = null)
    {
        Transform marked = weak ? target : null;
        bool parcel = form == SpecialAbilities.KitParcel;
        bool heavy = route != null && route.heavy;
        // 1.1.2: 새 편지 그림(흰 종이 · 금빛 테 · 붉은 봉인 · 어두운 외곽선) + 금빛 빛 무리 · 잔상, 보이는 크기 2.4 → 2.7 → 3배 (닿는 판정은 그대로)
        bool art = !pigeon && Fx.Frames("atk_letter").Length > 0;
        float mul = AttackLook.BigMul(AttackLook.Tier);
        FxAnim a = heavy ? PostParcelFx(from, (route.big ? 1.9f : 1.4f) * mul, 8f)
                 : Fx.Play(pigeon ? "fx_falcon" : art ? "atk_letter" : "fx_card", from, (pigeon ? 1.4f : (parcel ? 1.1f : 0.9f)) * mul,
                           pigeon ? new Color(0.9f, 0.9f, 1f) : (parcel ? new Color(0.85f, 0.65f, 0.45f) : SkinFx.Tint(Letter)), 16f, 0f, AttackLook.Order, true, 3f);
        if (a != null && !heavy)
        {
            BulletGlow glow = a.gameObject.AddComponent<BulletGlow>();
            glow.styled = true;
            glow.keepColor = true;          // 색까지 그린 그림: 빛 무리 · 잔상만 금빛
            glow.frames = a.frames;
            glow.haloMul = 1.2f;
        }
        // 1.1.2 뒤: 닿는 거리 = 보이는 편지 · 비둘기 몸통 반지름 (가로 · 세로 평균, 외곽선 뺌) — 예전엔 크기와 상관없이 0.45칸
        //   2026-10-22~ 그림 배율(VisualRules)이 바뀌어도 닿는 거리는 예전 그대로 (Fx.JudgeKeep)
        string touchFx = pigeon ? "fx_falcon" : art ? "atk_letter" : "fx_card";
        float touchR = heavy ? (route.big ? 0.8f : 0.6f) : a != null ? AttackLook.TouchRadius(a.sr, touchFx) * Fx.JudgeKeep(touchFx) : 0.45f;
        Vector3 p = from;
        Vector2 v = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.right;
        // 갈래: 순회 배달 -15% · 무거운 소포 절반 속도 (꺾임도 무뎌짐) · 사거리가 늘면 그만큼 오래 날아감
        float speed = LetterSpeed * PostPathSpeed(heavy), turnMul = heavy ? 0.6f : 1f;
        float life = Mathf.Max(1.6f, PostRange * 1.4f / Mathf.Max(1f, speed));
        float traveled = 0f;
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
                float turn = LetterTurn * turnMul * (1f + 4f * Mathf.Clamp01(1f - to.magnitude / 4f) + t) * Time.deltaTime;
                v = Quaternion.Euler(0f, 0f, Mathf.Clamp(ang, -turn, turn)) * v;
            }
            p += (Vector3)(v * speed * Time.deltaTime);
            traveled += speed * Time.deltaTime;
            if (a != null)
            {
                a.transform.position = p;
                if (heavy) a.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 9f) * 18f);      // 소포: 무겁게 흔들림
                else a.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - (art ? 0f : 90f) + Mathf.Sin(t * 30f) * 12f);      // 새 편지 그림은 가로로 누운 봉투
            }
            if (Hostile.IsWall(p)) break;
            // 받는 이 몸에 닿으면 배달 (예전엔 몸 크기와 상관없이 중심 0.74칸 안이어야 해서 큰 적 · 보스는 몸에 닿고도 지나쳤음)
            if (target != null && Touches(target, p, touchR) && Alive(target))
            {
                Deliver(target, p, v, dmg, pigeon, depth, marked != null && target == marked, route, traveled);
                break;
            }
            yield return null;
        }
        if (a != null) Destroy(a.gameObject);
    }

    static bool Alive(Transform t) => t != null && !(t.TryGetComponent(out EnermyController e) && e.IsDead);

    // 편지가 받는 이 몸(충돌 범위)에 닿았나: 중심 0.74칸 안이거나, 몸 가장자리에서 0.45칸 안
    static bool Touches(Transform t, Vector3 p, float r)
    {
        if (((Vector2)(t.position - p)).sqrMagnitude < 0.55f) return true;
        if (!t.TryGetComponent(out Collider2D col) || !col.enabled) return false;
        return ((Vector2)p - col.ClosestPoint(p)).sqrMagnitude < r * r;
    }

    // 받는 이 표시: 편지를 보낼 때 받는 이에게 붉은 조준 표식이 잠깐 (누구에게 가는지 보이게)
    void MarkAddressee(Transform to)
    {
        FxAnim r = Fx.Play("fx_reticle", to.position, 1.3f, new Color(1f, 0.45f, 0.4f, 0.95f), 12f, 0f, 50, true, 0.45f);
        if (r != null) { r.follow = to; r.spin = 220f; }
    }

    // 배달 완료
    void Deliver(Transform target, Vector3 at, Vector2 v, float dmg, bool pigeon, int depth, bool weak = false, PostRoute route = null, float traveled = 0f)
    {
        GameObject g = target.gameObject;
        float dist = (at - transform.position).magnitude;
        bool heavy = route != null && route.heavy;
        // 1.0.6~ 우표 중첩당 +4/8/12% → +3/6/9% (맞으면 절반 떨어짐 · LoseStamps)
        float m = formPower * (1f + 0.03f * PostStampLevel * stampStacks) * SignatureSkills.LetterMul(g, target == lastAddressee, dist);
        if (augment == AugThickEnvelope) m *= 1.25f;
        m *= PostPathMul(dist, traveled, route, out bool crit);
        bool wasAlive = !(g.TryGetComponent(out EnermyController e) && e.IsDead);
        if (heavy) PostParcelBlast(g, at, v, dmg * m, route.big, weak, route.hop, crit);       // 무거운 소포: 받는 이 + 둘레 폭발 (순회로 이어 간 소포는 감쇠 · 1.4.1~ 먼 소포는 치명 표시)
        else
        {
            if (crit) DamagePopup.NextCrit = true;      // 장거리 배송 공명 I: 먼 편지 치명타 (금색 숫자)
            if (weak) WeakHit.Hit(g, WeakShape.Marked(1.75f), dmg * m, v, 0.8f);       // 빈틈 지정 편지 (열려 있을 때만 배율)
            else Specials.Damage(g, dmg * m, v, 0.8f);
        }
        if (pigeon) StartCoroutine(PigeonPeck(target, dmg * 0.5f));
        lastAddressee = target;
        // 1.0.6~ 배달 횟수 · 우편 보너스는 평타로 직접 보낸 편지(depth 0)만 (순회 배달은 첫 받는 이만)
        if (depth == 0 && (route == null || route.hop == 0))
        {
            Deliveries++;
            RunCounters.Both("letter");             // 1.2~ 영혼 계약 · 보드
            if (Deliveries % 10 == 0) { PostBonus(at); RunCounters.Add("post.bonus"); }
        }
        // 장거리 배송 공명 II: 보스에게 배달하면 우표 2중첩
        //   1.4.1~ 우표 수집(card[2]) 없이 공명 II 에 닿아도 보스 배달이면 쌓임 (전엔 card[2]==0 이면 0 그대로)
        int stampGain = Paths.Res(PostmanPaths.LongHaul) >= 2 && Paths.IsBoss(g) ? 2 : (card[2] > 0 ? 1 : 0);
        if (stampGain > 0) stampStacks = Mathf.Min(StampMax, stampStacks + stampGain);
        Fx.Spawn("fx_sparkle", at, 1f, Letter, 24f);
        // 소포 (카드) · 소포 상자 (형태): 맞은 자리에서 터짐 (무거운 소포는 이미 크게 터지므로 대신 소포를 키움 · ParcelBlast)
        if (!heavy && (card[1] > 0 || form == SpecialAbilities.KitParcel))
        {
            // 1.4.1~ 카드와 형태가 겹치면 더함 (예전엔 카드를 배우는 순간 형태 폭발 60% 가 카드 폭발 50% 로 바뀌어 오히려 약해졌음)
            //   반경 = 카드 반경(없으면 1.2칸) × 소포 상자 1.15 (무거운 소포 PostParcelRadius 와 같은 배율) · 피해 = 60%(상자) / 50%(카드) + 상자일 때 카드 단계마다 +10%
            bool box = form == SpecialAbilities.KitParcel;
            float r = (card[1] > 0 ? 0.8f + 0.4f * card[1] : 1.2f) * (box && card[1] > 0 ? 1.15f : 1f) * SignatureSkills.ParcelMul();
            DamageCircle(at, r, dmg * (box ? 0.6f + 0.1f * card[1] : 0.5f), 1f);
            Fx.Spawn("fx_explosion", at, r * 2f, new Color(0.9f, 0.7f, 0.45f), 20f);
        }
        bool killed = wasAlive && g != null && g.TryGetComponent(out EnermyController e2) && e2.IsDead;
        // 순회 배달: 다음 받는 이로 이어 감 (이어 가면 등기 · 반송은 순회가 끝난 마지막 받는 이에서만)
        if (PostContinueRoute(route, target, at, dmg, killed))
        {
            Play("stamp", 0.25f, 1.45f);
            SignatureSkills.Delivered(g, at, killed, depth);
            return;
        }
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

    // ================================================================= 갈래 (2026-10-19 · Paths/Paths.Postman.cs 의 수치)
    // 순회 배달 · 무거운 소포가 이어 가는 한 통 (평타로 직접 보낸 편지마다 하나 · 같은 통이 받는 이를 옮겨 가므로 한 줄로만 이어짐)
    sealed class PostRoute
    {
        public int hop;                 // 지금 몇 번째 받는 이 (0 = 처음)
        public int max = 1;             // 최대 받는 이 수 (순회 배달 4 · 극의 8 · 순회가 아니면 1)
        public Transform first;         // 처음 받는 이 (순회 공명 I: 돌아갈 곳)
        public bool heavy;              // 무거운 소포
        public bool big;                // 대형 소포 (무거운 소포 공명 II · 세 번째마다)
        public bool returned;           // 순회 공명 I 로 이미 한 번 돌아감
        public readonly List<Transform> visited = new List<Transform>(8);
    }

    static readonly Color ParcelTint = new Color(0.85f, 0.65f, 0.45f);
    const string ParcelIcon = "postman_parcel";      // Resources/Icons/Paths/postman_parcel.png (없으면 갈색 편지 그림)
    static Sprite[] parcelFrames;
    readonly List<GameObject> droppedParcels = new List<GameObject>();

    // 무거운 소포를 보낸 수 (공명 II: 세 번째마다 대형 · HUD 입문 칸)
    public int PostParcelCount { get; private set; }

    // 편지가 받는 이를 찾는 사거리 (장거리 배송 20 · 극의 40 = 화면 밖까지)
    float PostRange => Paths.Has(PostmanPaths.LongHaul)
        ? (Paths.Mastered(PostmanPaths.LongHaul) ? PostmanPaths.LongMasteryRange : PostmanPaths.LongRange)
        : (def.range > 0f ? def.range : 12f);

    // 우표 한 장의 값 (우표 수집 단계 · 자전거 공명 I 이면 우표 수집이 없어도 1단계 값)
    //   1.4.1~ 장거리 배송 공명 II 도 우표 수집 없이 1단계 값 (보스 배달로 쌓인 우표가 +0% 이던 것)
    int PostStampLevel => Mathf.Max(card[2], Paths.Res(PostmanPaths.Bike) >= 1 || Paths.Res(PostmanPaths.LongHaul) >= 2 ? 1 : 0);

    float PostPathSpeed(bool heavy) => (Paths.Has(PostmanPaths.Round) ? PostmanPaths.RoundSpeed : 1f) * (heavy ? PostmanPaths.ParcelSpeed : 1f);

    // 배달 한 번의 갈래 배율: 자전거 멈춤 대가 · 장거리 근접 대가 · 장거리 공명 I 치명타 · 순회로 이어 간 편지의 갈래 배율
    float PostPathMul(float dist, float traveled, PostRoute route, out bool crit)
    {
        crit = false;
        if (!Paths.Active) return 1f;
        float m = 1f;
        if (Paths.Has(PostmanPaths.Bike) && !Moving) m *= PostmanPaths.BikeStillCost;
        if (Paths.Has(PostmanPaths.LongHaul))
        {
            if (dist < PostmanPaths.LongCloseDist) m *= PostmanPaths.LongCloseCost;
            if (Paths.Res(PostmanPaths.LongHaul) >= 1 && traveled > PostmanPaths.LongCritDist) { m *= PostmanPaths.LongCrit; crit = true; }
        }
        if (route != null && route.hop > 0 && Paths.Has(PostmanPaths.Round)) m *= Paths.Power(PostmanPaths.Round);
        return m;
    }

    // 소포 그림: 갈래 아이콘 폴더의 소포 그림 (없으면 갈색으로 물들인 편지 그림)
    FxAnim PostParcelFx(Vector3 at, float size, float life)
    {
        Sprite s = Paths.Icon(ParcelIcon);
        FxAnim a = Fx.Play("fx_card", at, size, s != null ? Color.white : ParcelTint, 8f, 0f, AttackLook.Order, true, life);
        if (a == null || s == null) return a;
        if (parcelFrames == null || parcelFrames[0] != s) parcelFrames = new[] { s };
        a.frames = parcelFrames;
        if (a.sr != null) a.sr.sprite = s;
        float h = s.bounds.size.y;
        a.transform.localScale = Vector3.one * (h > 0f ? size / h : 1f);
        return a;
    }

    // 무거운 소포 폭발 반경 (「소포」 카드 · 대형 소포 진화 · 소포 상자 형태 · 공명 II 대형 · 극의, 상한 6칸)
    float PostParcelRadius(bool big)
    {
        float r = PostmanPaths.ParcelRadius * (1f + PostmanPaths.ParcelCardRadius * card[1])
                * (SignatureSkills.ParcelMul() > 1f ? 1.3f : 1f) * (form == SpecialAbilities.KitParcel ? 1.15f : 1f);
        if (big) r *= PostmanPaths.ParcelBigRadius;
        if (Paths.Mastered(PostmanPaths.Parcel)) r *= PostmanPaths.ParcelMasteryRadius;
        return Mathf.Min(r, PostmanPaths.ParcelRadiusCap);
    }

    // 무거운 소포가 받는 이에게 닿음: 받는 이(빈틈 지정이면 빈틈 배율) + 둘레 적 모두 같은 피해 (평타 피해라 명중 효과가 그대로 붙음)
    //   hop: 순회 배달로 몇 번째 이어 간 소포인지 (0 = 처음) — 이어 간 소포는 폭발 · 우표 조각이 ×0.5^hop 이고 둘레 폭발 · 조각이 보스를 치지 않음
    //   crit: 1.4.1~ 장거리 배송 공명 I 먼 소포 — 피해는 이미 ×1.5 (PostPathMul), 받는 이 · 둘레 숫자를 금색 치명으로
    void PostParcelBlast(GameObject target, Vector3 at, Vector2 v, float dmg, bool big, bool weak, int hop = 0, bool crit = false)
    {
        float r = PostParcelRadius(big);
        float d = dmg * PostmanPaths.ParcelDamage * (1f + PostmanPaths.ParcelCardDamage * card[1]) * Paths.Power(PostmanPaths.Parcel) * (big ? PostmanPaths.ParcelBigDamage : 1f);
        bool relayed = hop > 0;
        if (relayed) d *= Mathf.Pow(PostmanPaths.ParcelRouteFalloff, Mathf.Min(hop, 8));
        float knock = big ? 2.5f : 1.5f;
        if (target != null)
        {
            if (crit) DamagePopup.NextCrit = true;
            if (weak) WeakHit.Hit(target, WeakShape.Marked(1.75f), d, v, knock);
            else Specials.Damage(target, d, v, knock);
        }
        using (var hits = PhysHits.Circle(at, r, true))
            foreach (Collider2D c in hits)
            {
                if (c == null || c.gameObject == target || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                if (relayed && Paths.IsBoss(c.gameObject)) continue;       // 이어 간 소포의 둘레 폭발은 보스 · 중간 보스 제외
                if (crit) DamagePopup.NextCrit = true;
                Specials.Damage(c.gameObject, d, (c.transform.position - at).normalized, knock);
            }
        Fx.Spawn("fx_explosion", at, r * 2f, ParcelTint, 20f);
        if (big) { Fx.Spawn("fx_sparkle", at, r * 1.6f, new Color(1f, 0.85f, 0.55f), 22f); Hostile.Shake(0.12f); Play("boom", 0.5f, 1.05f); }
        else Play("thump", 0.45f, 1.1f);
        // 공명 I: 터진 자리 둘레에서 우표 조각 둘 (받는 이는 빼고 · 갈래 효과 피해라 다시 명중 효과를 부르지 않음)
        if (Paths.Res(PostmanPaths.Parcel) >= 1) StartCoroutine(PostStampShards(at, r, d * PostmanPaths.ParcelShard, target, relayed));
    }

    //   skipBoss: 순회로 이어 간 소포의 조각 — 보스 · 중간 보스는 치지 않음
    IEnumerator PostStampShards(Vector3 at, float r, float dmg, GameObject skip, bool skipBoss = false)
    {
        yield return new WaitForSeconds(0.2f);
        if (player == null || player.IsDying) yield break;
        float a0 = Random.value * 360f;
        for (int i = 0; i < 2; i++)
        {
            float a = (a0 + 180f * i) * Mathf.Deg2Rad;
            Vector3 p = at + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Mathf.Max(1.4f, r * 0.85f);
            Paths.Circle(p, 1.1f, 0f, 0f, null, c =>
            {
                if (c.gameObject != skip && !(skipBoss && Paths.IsBoss(c.gameObject))) Paths.Deal(c.gameObject, dmg, (c.transform.position - p).normalized, 0.8f, PostmanPaths.ShardSource);
            });
            Fx.Spawn("fx_explosion", p, 2.2f, new Color(1f, 0.6f, 0.5f), 22f);
        }
        Play("pop", 0.35f, 1.6f);
    }

    // 순회 배달: 받는 이에게 닿은 뒤 다음으로 가장 먼 적에게 (이어 가면 true — 등기 · 반송은 순회가 끝난 곳에서만)
    bool PostContinueRoute(PostRoute r, Transform target, Vector3 at, float dmg, bool killed)
    {
        if (r == null || !Paths.Has(PostmanPaths.Round)) return false;
        if (r.hop + 1 < r.max)
        {
            Transform next = PostFarthest(at, PostRange, r.visited);
            if (next != null)
            {
                r.hop++;
                r.visited.Add(next);
                MarkAddressee(next);
                StartCoroutine(LetterFlight(at, ((Vector2)(next.position - at)).normalized, next, dmg * (1f - PostRouteDecay(at, next.position)), false, 0, false, r));
                return true;
            }
        }
        // 공명 II: 끝까지 다 돌면 마지막 받는 이에 소인 (4초 동안 받는 피해 +20%)
        if (r.hop + 1 >= r.max && Paths.Res(PostmanPaths.Round) >= 2 && target != null && Alive(target))
        {
            PostmanPaths.MarkRound(target.gameObject);
            Fx.Spawn("fx_target_rune", target.position, 1.6f, new Color(0.95f, 0.45f, 0.4f), 14f);
        }
        // 공명 I: 마지막 받는 이가 쓰러지면 처음 적에게 한 번 돌아감
        if (killed && !r.returned && Paths.Res(PostmanPaths.Round) >= 1 && r.first != null && r.first != target && Alive(r.first))
        {
            r.returned = true;
            r.hop++;
            MarkAddressee(r.first);
            StartCoroutine(LetterFlight(at, ((Vector2)(r.first.position - at)).normalized, r.first, dmg * (1f - PostRouteDecay(at, r.first.position)), false, 0, false, r));
            return true;
        }
        return false;
    }

    // 순회의 감쇠 15% (교차 · 전국 배송: 다음 받는 이가 멀수록 줄어 15칸 넘게 떨어지면 3%)
    static float PostRouteDecay(Vector3 from, Vector3 to)
    {
        float k = 1f;
        if (Paths.CrossOn(PostmanPaths.NationwideCross))
            k = Mathf.Lerp(1f, PostmanPaths.RoundNationwideMin, Mathf.Clamp01((((Vector2)(to - from)).magnitude - 3f) / 12f));
        return PostmanPaths.RoundDecay * k;
    }

    // 둘레에서 가장 먼 살아 있는 적 (이미 받은 적은 빼고)
    Transform PostFarthest(Vector3 from, float range, List<Transform> except)
    {
        Transform far = null;
        float farD = -1f;
        foreach (Collider2D c in Specials.Overlap(from, range, postHits))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss")) || except.Contains(c.transform)) continue;
            if (c.TryGetComponent(out EnermyController e) && e.IsDead) continue;
            float d = ((Vector2)(c.transform.position - from)).sqrMagnitude;
            if (d > farD) { farD = d; far = c.transform; }
        }
        return far;
    }

    // 자전거 배달 입문: 가장 가까운 적에게 저절로 편지 (n 통 · 다시 배달 · 반송 · 배달 횟수 없음)
    public void PostAutoLetter(int n)
    {
        if (player == null || player.IsDying || n <= 0) return;
        Vector3 from = transform.position + Vector3.up * 0.4f;
        List<Transform> to = SignatureSkills.Nearest(from, PostmanPaths.BikeAutoRange, n);
        if (to.Count == 0) return;
        float d = Damage * PostmanPaths.BikeAutoRate * Paths.Power(PostmanPaths.Bike);
        using var source = DamageSource.As(PostmanPaths.AutoSource);       // 코루틴이 이 출처를 이어 감 (판 기록 dealt_by)
        for (int i = 0; i < n; i++)
        {
            Transform t = to[i % to.Count];
            Vector2 aim = (Vector2)(t.position - from);
            if (aim.sqrMagnitude < 0.01f) aim = Vector2.up;
            Vector2 launch = Quaternion.Euler(0f, 0f, (i - (n - 1) * 0.5f) * 16f) * aim.normalized;
            StartCoroutine(LetterFlight(from, launch, t, d, false, 9));
        }
        Play("flick", 0.25f, 1.3f);
    }

    // 자전거 배달 공명 I: 달린 거리로 우표
    public void PostAddStamp(int n)
    {
        if (PostStampLevel <= 0 || n <= 0) return;
        int before = stampStacks;
        stampStacks = Mathf.Min(StampMax, stampStacks + n);
        if (stampStacks > before) Fx.Spawn("fx_sparkle", transform.position + Vector3.up * 0.8f, 0.9f, PostRed, 24f);
    }

    // 교차 · 달리는 소포: 발밑에 소포를 두고 잠시 뒤 터짐 (동시 6개 상한 · 갈래 효과 피해)
    public void PostDropParcel(Vector3 at, float dmg, float r)
    {
        if (player == null || player.IsDying) return;
        Paths.Trim(droppedParcels, PostmanPaths.RollingCap);
        FxAnim box = PostParcelFx(at, 1.1f, PostmanPaths.RollingFuse + 1f);
        if (box != null) droppedParcels.Add(box.gameObject);
        StartCoroutine(PostDroppedParcel(at, box, dmg, r));
    }

    IEnumerator PostDroppedParcel(Vector3 at, FxAnim box, float dmg, float r)
    {
        yield return new WaitForSeconds(PostmanPaths.RollingFuse);
        if (box != null) Destroy(box.gameObject);
        if (player == null || player.IsDying) yield break;
        Paths.Circle(at, r, dmg, 1.2f, PostmanPaths.RollingSource);
        Fx.Spawn("fx_explosion", at, r * 2f, ParcelTint, 20f);
        Play("thump", 0.3f, 1.2f);
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
        // 속달 갈래 입문: 표식이 2배 빨리 (다 차는 데 1.2초 → 0.6초)
        expressCharge = Mathf.Min(1f, expressCharge + Time.deltaTime / 1.2f * (Paths.Has(PostmanPaths.Express) ? PostmanPaths.ExpressCharge : 1f));
        Vector3 me = transform.position;
        // 1.0.2: 둘레 고리를 스킬 이펙트(+40) 위 순서로 · 더 굵고 진하게, 다 차면 금빛으로
        if (expressRing == null) { expressRing = Hostile.NewLine("ExpressRing", PostRed, 0.16f, 55); expressRing.loop = true; }
        expressRing.enabled = true;
        Hostile.SetArc(expressRing, me, ExpressRange, 0f, 360f);
        Color post = HeroAim.ColorOf(HeroAimKind.Postman);         // 1.5.0~ 범위표시 색: 우편 빨강 (다 차면 금빛은 그대로)
        Color rc = expressCharge >= 1f ? Color.Lerp(post, new Color(1f, 0.85f, 0.4f), 0.6f) : post;
        expressRing.startColor = expressRing.endColor = new Color(rc.r, rc.g, rc.b, 0.5f + 0.3f * Mathf.Sin(Time.time * 12f));
        expressRing.startWidth = expressRing.endWidth = 0.16f + 0.08f * expressCharge;
        // 우표 문양 고리 · 표식 붙은 적 → 내 쪽으로 편지가 날아옴 (HeroAimDecor · 목록은 참조만)
        HeroAim.Mark(expressRing, HeroAimKind.Postman);
        HeroAim.SetTargets(expressRing, expressMarks);
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
                if (Paths.Res(PostmanPaths.Express) >= 1) PostmanPaths.MarkExpress(best.gameObject, 6f);     // 공명 I: 표식이 붙은 적 받는 피해 +15%
                // 1.0.2: 수명 5초 → 직접 지울 때까지 (5초 넘게 누르면 표식 아이콘이 사라졌음), 스킬 이펙트 위 순서
                FxAnim icon = Fx.Play("fx_card", best.position + Vector3.up * 1.2f, 0.9f, PostRed, 12f, 0f, 24, true, -1f);
                expressIcons.Add(icon);
                Fx.Spawn("fx_spark", best.position, 0.9f, PostRed, 26f);
                Play("pop", 0.15f, 2.2f + 0.02f * expressMarks.Count);
            }
        }
        for (int i = 0; i < expressIcons.Count && i < expressMarks.Count; i++)
            if (expressIcons[i] != null) PlaceExpressIcon(expressIcons[i], expressMarks[i], expressCharge, IconScale(expressIcons[i]));
        // 1.4.1~ 공명 I: 누르고 있는 동안 표식 대상의 받는 피해 +15% 를 계속 이어 감 (처음 건 6초가 누르는 중에 끝나던 것 · Max 로 합침)
        if (Paths.Res(PostmanPaths.Express) >= 1)
            foreach (Transform t in expressMarks)
                if (t != null) PostmanPaths.MarkExpress(t.gameObject, 1f);
        if (AddedUltShown) DrawUltAim(false, true);

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        expressing = false;
        expressRing.enabled = false;
        CancelUltAim();
        float dmg = Damage * (1.8f + 1.4f * expressCharge) * UltMul;       // 1.0.6~ 공격력 200 ~ 400% → 180 ~ 320%
        RunCounters.Max("express.marks", expressMarks.Count);      // 1.2~ 영혼 계약 「여덟 장의 표식」
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
        if (Paths.Has(PostmanPaths.Express)) gauge.AddSkillPoint(gauge.MaxSkillPoint * PostmanPaths.ExpressRefund);      // 속달 갈래 입문: 게이지 25% 덜 씀
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
            if (Paths.Res(PostmanPaths.Express) >= 1 && t != null) PostmanPaths.MarkExpress(t.gameObject, 4f);      // 공명 I: 터진 뒤 4초까지
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
        // 속달 공명 II: 터질 때마다 다음 속달 게이지 10%
        if (spots.Count > 0 && Paths.Res(PostmanPaths.Express) >= 2 && player != null && !player.IsDying && player.skillGauge != null)
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * PostmanPaths.ExpressBlastGauge);
        // 속달 극의: 0.5초 뒤 같은 표식에서 한 번 더 (피해 60% · 답장 · 익일 배송은 다시 안 남)
        if (spots.Count > 0 && Paths.Mastered(PostmanPaths.Express))
        {
            yield return new WaitForSeconds(PostmanPaths.ExpressEchoDelay);
            if (player == null || player.IsDying) yield break;
            float echo = dmg * PostmanPaths.ExpressEcho * Paths.Power(PostmanPaths.Express);
            int n = 0;
            foreach (Transform t in marks)
            {
                if (!Alive(t)) continue;
                Vector3 p = t.position;
                Paths.Deal(t.gameObject, echo, Vector3.zero, 1f, PostmanPaths.EchoSource);
                Fx.Spawn("fx_explosion", p, 1.8f, new Color(1f, 0.85f, 0.4f), 22f);
                n++;
            }
            if (n > 0) { Play("boom", 0.6f, 1.35f); Play("stamp", 0.7f, 0.85f); }
        }
    }

    void OnDestroyPostman()
    {
        if (expressRing != null) Destroy(expressRing.gameObject);
        ClearExpressIcons(expressIcons);
        ClearExpressIcons(expressPending);
        foreach (GameObject g in droppedParcels) if (g != null) Destroy(g);
        droppedParcels.Clear();
    }
}
