using System.Collections.Generic;
using UnityEngine;

// 떨어진 코인 목록: 코인 자석이 매 프레임 씬 전체를 태그로 뒤지지 않도록 코인이 스스로 등록 · 해제
// 1.0.4~ 후반(무한 모드 · 4장 · 보스 처치 뒤) 코인이 쌓이면 프레임이 떨어지던 문제:
//  - 코인마다 붙은 큰 트리거 콜라이더(반지름 약 2.6칸) · Rigidbody2D 를 끔. 예전엔 근처 적의 OnTriggerStay2D,
//    지나가는 총알의 OnTriggerEnter2D, 스킬의 겹침 검사(Specials.Overlap)가 코인 수만큼 매번 불렸음
//  - 줍기 · 자석은 여기서 한 번에 거리 제곱으로 계산 (줍는 거리는 원래 콜라이더 크기 그대로)
//  - 한 번에 많이 떨어지면 값이 큰 코인 몇 개로 나눠 떨어뜨림 (크기 · 빛으로 구분, 줍는 양은 그대로)
//  - 바닥 코인 수 상한: 넘으면 가장 오래된 코인의 값을 가장 가까운 코인에 합침 (값은 그대로 옮겨 감)
//  - 주운 코인은 지우지 않고 다시 씀 (풀링)
public class CoinTag : MonoBehaviour
{
    public static readonly List<Transform> All = new List<Transform>();
    static readonly List<CoinTag> Live = new List<CoinTag>();         // All 과 같은 순서 (오래된 것부터)
    static readonly Dictionary<GameObject, Stack<CoinTag>> pool = new Dictionary<GameObject, Stack<CoinTag>>();

    // 1.5.5~: 바닥 코인이 화면을 덮었음 → 개수만 확 줄임 (220 · 8 · 40 → 50 · 2 · 10, 합쳐도 값은 그대로라 획득량은 같음)
    public const int MaxOnField = 50;          // 바닥에 동시에 있는 코인 수 상한
    public const int EnemyPieces = 2;          // 일반 적 · 엘리트가 한 번에 떨어뜨리는 코인 개수 상한
    public const int BossPieces = 10;          // 보스가 한 번에 떨어뜨리는 코인 개수 상한
    const int MaxSparkles = 3;                 // 한 프레임에 줍는 반짝임 이펙트 수 상한

    public int value = 1;                      // 이 코인 하나가 코인 몇 개 몫인지

    bool ready, held, pooled;
    GameObject prefab;
    Vector3 baseScale;
    SpriteRenderer sr;
    Color baseColor = Color.white;
    PickupGlow glow;
    float pickRadius = 0.5f;                   // 원래 트리거 콜라이더의 월드 반지름
    Vector2 pickOffset;                        // 원래 트리거 콜라이더의 중심 (위치 기준)

    // 값 단계: 1 · 2~4 · 5~14 · 15 이상 (크기 · 그림 색 · 빛 무리)
    static readonly float[] TierScale = { 1f, 1.2f, 1.45f, 1.7f };
    static readonly Color[] TierTint = { Color.white, Color.white, new Color(1f, 0.9f, 0.72f), Color.white };
    static readonly Color[] TierGlow = { new Color(1f, 0.82f, 0.3f), new Color(1f, 0.82f, 0.3f), new Color(1f, 0.66f, 0.22f), new Color(1f, 0.95f, 0.7f) };
    static readonly float[] TierHalo = { 1.1f, 1.35f, 1.7f, 2.1f };

    // 예전 방식 (이미 만든 코인을 등록) — 값 1, 풀에는 돌려놓지 않음
    public static void Register(GameObject coin)
    {
        if (coin == null) return;
        CoinTag t = Setup(coin, null);
        t.SetValue(Mathf.Max(1, t.value));
        LimitField(t);
    }

    // count 개 몫의 코인을 떨어뜨림: maxPieces 개를 넘으면 값이 큰 코인으로 나눠 담음 (합은 정확히 count)
    public static void Drop(GameObject prefab, Vector3 center, int count, Vector2 spread, bool firstAtCenter, int maxPieces)
    {
        if (prefab == null || count <= 0) return;
        int pieces = Mathf.Clamp(maxPieces, 1, count);
        int each = count / pieces, rem = count % pieces;
        for (int i = 0; i < pieces; i++)
        {
            Vector3 at = center;
            if (!firstAtCenter || i > 0) at += new Vector3(Random.Range(-spread.x, spread.x), Random.Range(-spread.y, spread.y), 0f);
            Spawn(prefab, Hostile.ClampArena(at), each + (i < rem ? 1 : 0));
        }
    }

    public static GameObject Spawn(GameObject prefab, Vector3 pos, int value)
    {
        if (prefab == null) return null;
        CoinTag t = null;
        if (pool.TryGetValue(prefab, out Stack<CoinTag> stack))
            while (stack.Count > 0 && t == null) t = stack.Pop();       // 장면이 바뀌어 지워진 것은 건너뜀
        GameObject g;
        if (t != null)
        {
            g = t.gameObject;
            g.transform.SetPositionAndRotation(pos, Quaternion.identity);
        }
        else g = Instantiate(prefab, pos, Quaternion.identity);
        t = Setup(g, prefab);
        t.SetValue(value);
        if (!g.activeSelf) g.SetActive(true);         // 풀에서 꺼낸 것: OnEnable 에서 목록에 다시 들어감
        LimitField(t);
        return g;
    }

    static CoinTag Setup(GameObject g, GameObject prefab)
    {
        if (!g.TryGetComponent(out CoinTag t)) t = g.AddComponent<CoinTag>();
        if (!t.ready)
        {
            t.ready = true;
            t.prefab = prefab;
            t.baseScale = g.transform.localScale;
            t.sr = g.GetComponent<SpriteRenderer>();
            if (t.sr != null) t.baseColor = t.sr.color;
            // 줍는 거리는 원래 트리거 콜라이더 그대로 (끄기 전에 잼)
            Vector3 s = g.transform.lossyScale;
            if (g.TryGetComponent(out CircleCollider2D circle))
            {
                t.pickRadius = circle.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
                t.pickOffset = Vector2.Scale(circle.offset, s);
            }
            else if (g.TryGetComponent(out Collider2D col) && col.enabled)
            {
                Bounds b = col.bounds;
                t.pickRadius = Mathf.Max(b.extents.x, b.extents.y);
                t.pickOffset = b.center - g.transform.position;
            }
            foreach (Collider2D c in g.GetComponents<Collider2D>()) c.enabled = false;
            if (g.TryGetComponent(out Rigidbody2D rb)) rb.simulated = false;
            PickupGlow.Add(g, TierGlow[0], TierHalo[0]);       // 1.0.2: 스킬 이펙트에 묻히지 않게
            t.glow = g.GetComponent<PickupGlow>();
        }
        t.held = false;
        t.pooled = false;
        return t;
    }

    void SetValue(int v)
    {
        value = Mathf.Max(1, v);
        int tier = value >= 15 ? 3 : value >= 5 ? 2 : value >= 2 ? 1 : 0;
        transform.localScale = baseScale * TierScale[tier];
        if (sr != null) sr.color = baseColor * TierTint[tier];
        if (glow != null) glow.SetLook(TierGlow[tier], TierHalo[tier]);
    }

    // 상한을 넘으면 가장 오래된 코인을 가장 가까운 코인에 합침 (keep: 방금 떨어진 코인은 합쳐 없애지 않음)
    static void LimitField(CoinTag keep)
    {
        while (Live.Count > MaxOnField)
        {
            CoinTag old = null;
            for (int i = 0; i < Live.Count; i++)
            {
                CoinTag c = Live[i];
                if (c != null && c != keep && !c.held) { old = c; break; }
            }
            if (old == null) return;
            CoinTag near = null;
            float best = float.MaxValue;
            Vector3 p = old.transform.position;
            for (int i = 0; i < Live.Count; i++)
            {
                CoinTag c = Live[i];
                if (c == null || c == old || c.held) continue;
                float d = (c.transform.position - p).sqrMagnitude;
                if (d < best) { best = d; near = c; }
            }
            if (near == null) return;
            near.SetValue(near.value + old.value);
            old.Release();
        }
    }

    // 다 쓴 코인: 풀에 돌려놓음 (풀이 없는 코인은 지움)
    void Release()
    {
        if (pooled) return;
        pooled = true;                                 // 두 번 세지 않게 (지우는 것은 프레임 끝에 일어나므로 목록에서 바로 뺌)
        held = false;
        value = 1;
        if (prefab == null) { All.Remove(transform); Live.Remove(this); Destroy(gameObject); return; }
        gameObject.SetActive(false);                   // OnDisable 에서 목록에서 빠짐
        if (!pool.TryGetValue(prefab, out Stack<CoinTag> stack)) pool[prefab] = stack = new Stack<CoinTag>();
        stack.Push(this);
    }

    public static void Release(GameObject coin)
    {
        if (coin == null) return;
        if (coin.TryGetComponent(out CoinTag t)) t.Release();
        else Destroy(coin);
    }

    // 이 코인이 코인 몇 개 몫인지 (CoinTag 가 없으면 1)
    public static int ValueOf(GameObject coin) => coin != null && coin.TryGetComponent(out CoinTag t) ? Mathf.Max(1, t.value) : 1;

    // 연출이 직접 옮기는 코인 (보스 처치 소용돌이): 줍기 · 자석 · 합치기 · 자동 회수에서 빠짐
    public static void Hold(GameObject coin)
    {
        if (coin != null && coin.TryGetComponent(out CoinTag t)) t.held = true;
    }

    // 바닥 코인을 모두 회수하고 몇 개 몫인지 돌려줌 (연출이 옮기는 코인은 그대로)
    public static int TakeAll()
    {
        int units = 0;
        for (int i = Live.Count - 1; i >= 0; i--)
        {
            if (i >= Live.Count) continue;
            CoinTag c = Live[i];
            if (c == null) { Live.RemoveAt(i); continue; }
            if (c.held) continue;
            units += Mathf.Max(1, c.value);
            c.Release();
        }
        return units;
    }

    // 이번 프레임에 주운 코인 위치 (반짝임 이펙트용, 최대 MaxSparkles 개)
    static readonly List<Vector3> pickedAt = new List<Vector3>(MaxSparkles);
    public static List<Vector3> PickedAt => pickedAt;

    // 플레이어가 한 프레임에 한 번: 자석으로 끌어오고, 몸에 닿은 코인을 주움 → 주운 코인 몇 개 몫인지 (pieces: 주운 코인 개수)
    // 예전 물리 트리거와 같은 조건: 코인 콜라이더 원과 플레이어 몸 콜라이더가 겹치면 주움
    public static int Tick(Transform player, Collider2D body, float pull, out int pieces)
    {
        pieces = 0;
        pickedAt.Clear();
        if (player == null || Live.Count == 0) return 0;
        Vector3 me = player.position;
        bool canPick = true;
        Vector2 bc = me;
        float br = 0.5f;
        if (body != null)
        {
            Rigidbody2D rb = body.attachedRigidbody;
            if (!body.enabled || !body.gameObject.activeInHierarchy || (rb != null && !rb.simulated)) canPick = false;
            else
            {
                Bounds b = body.bounds;
                bc = b.center;
                br = Mathf.Max(b.extents.x, b.extents.y);
            }
        }
        float pull2 = pull * pull, dt = Time.deltaTime;
        int units = 0;
        for (int i = Live.Count - 1; i >= 0; i--)
        {
            if (i >= Live.Count) continue;
            CoinTag c = Live[i];
            if (c == null) { Live.RemoveAt(i); continue; }
            if (c.held) continue;
            Transform t = c.transform;
            Vector3 p = t.position;
            // 코인 자석 (거리 제곱으로 먼저 걸러 냄, 끌려오는 속도는 예전 그대로)
            if (pull > 0f)
            {
                float dx = p.x - me.x, dy = p.y - me.y, sq = dx * dx + dy * dy;
                if (sq < pull2)
                {
                    float d = Mathf.Sqrt(sq);
                    p = Vector3.MoveTowards(p, me, (10f + (pull - d) * 3f) * dt);
                    t.position = p;
                }
            }
            if (!canPick) continue;
            float rx = p.x + c.pickOffset.x - bc.x, ry = p.y + c.pickOffset.y - bc.y, rr = c.pickRadius + br;
            if (rx * rx + ry * ry >= rr * rr) continue;
            units += Mathf.Max(1, c.value);
            pieces++;
            if (pickedAt.Count < MaxSparkles) pickedAt.Add(p);
            c.Release();
        }
        return units;
    }

    void OnEnable()
    {
        All.Add(transform);
        Live.Add(this);
    }

    void OnDisable()
    {
        All.Remove(transform);
        Live.Remove(this);
    }
}
