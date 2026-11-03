using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 2.0.0~ 영혼 릴레이 유품 (modes.md 1-6): 쓰러진 앞 영웅 동사의 메아리 — 지금 영웅의 조작을 막지 않는 자동 발동 작은 기술 14종
//   앞 영웅의 키트가 사라져도 돌아야 하므로 키트와 따로 이 파일에 둠 · 피해는 「지금 공격력 × 배율」 · 출처 이름 「유품」 (결과 화면 피해 출처)
//   둘째 주자는 하나, 셋째 주자는 둘을 가짐 (1 · 2번 주자 것) · 같은 동사 계열이어도 둘 다 발동
//   수치는 첫 구현값 — 봇 측정으로 판 화력 +6~10% 띠에 맞춤
public class RelayHeirlooms : MonoBehaviour
{
    public const string Source = "유품";
    public static RelayHeirlooms Instance { get; private set; }

    static readonly List<int> owned = new List<int>();          // 가진 유품 (앞 영웅 번호, 받은 순서)
    readonly List<Heirloom> live = new List<Heirloom>();
    PlayerController player;

    // ================================================================= 목록 (정적)
    public static void Clear()
    {
        owned.Clear();
        if (Instance != null) Instance.Sync();
    }

    public static void Add(CharacterId hero)
    {
        owned.Add((int)hero);
        Ensure()?.Sync();
    }

    // 이어하기: 저장한 판의 지금 주자 앞 영웅들 (RelayRun.RestoreAfterReplay)
    public static void Rebuild()
    {
        owned.Clear();
        for (int k = 0; k < RelayRun.Index; k++) owned.Add(RelayRun.Order[k]);
        Ensure()?.Sync();
    }

    static RelayHeirlooms Ensure()
    {
        if (Instance != null) return Instance;
        PlayerController p = Hostile.Player;
        if (p == null) return null;
        // 2.0.1~ ?? 는 Unity 의 가짜 null(지워진 컴포넌트)을 null 로 보지 않아 에디터에서 AddComponent 를 건너뛰었음 → TryGetComponent
        return p.TryGetComponent(out RelayHeirlooms h) ? h : p.gameObject.AddComponent<RelayHeirlooms>();
    }

    public static string NameOf(CharacterId hero) => Loc.T("relay.heir." + (int)hero + ".name");
    public static string DescOf(CharacterId hero) => Loc.T("relay.heir." + (int)hero + ".desc");

    // 주자 띠 아래 한 줄: 「유품 · 마지막 탄창 · 녹슨 장검 4s」
    public static string HudLine()
    {
        if (owned.Count == 0) return Loc.T("relay.heir.none");
        System.Text.StringBuilder b = new System.Text.StringBuilder(Loc.T("relay.heir.label"));
        foreach (int id in owned)
        {
            b.Append(" · ").Append(NameOf((CharacterId)id));
            Heirloom h = Instance != null ? Instance.live.Find(x => (int)x.hero == id) : null;
            if (h != null && h.Remaining > 0.05f) b.Append(" <color=#A89C86>").Append(Mathf.CeilToInt(h.Remaining)).Append("s</color>");
        }
        return b.ToString();
    }

    // ================================================================= 구독 · 한 프레임
    void Awake()
    {
        Instance = this;
        player = GetComponent<PlayerController>();
        EnermyController.Killed += OnKilled;
        PlayerController.UltUsed += OnUlt;
        PlayerController.Damaged += OnHurt;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        EnermyController.Killed -= OnKilled;
        PlayerController.UltUsed -= OnUlt;
        PlayerController.Damaged -= OnHurt;
        foreach (Heirloom h in live) h.Dispose();
        live.Clear();
    }

    // 가진 유품과 켜진 유품을 맞춤 — 2.0.1~ 이미 켜진 것은 그대로 두고 빠진 것만 끄고 새 것만 만듦
    //   (전엔 매번 전부 지우고 새로 만들어 둘째 교대 때 첫 유품의 쿨 · 처치 카운트가 초기화되고, 샅바가 메치던 적은 놓였다가 코루틴이 계속 붙잡는 등 꼬였음)
    //   새 판 · 이어하기는 장면이 바뀌어 live 가 비어 있으므로 여전히 새로 만들어짐
    void Sync()
    {
        for (int i = live.Count - 1; i >= 0; i--)
        {
            if (owned.Contains((int)live[i].hero)) continue;
            live[i].Dispose();
            live.RemoveAt(i);
        }
        foreach (int id in owned)
        {
            if (live.Exists(x => (int)x.hero == id)) continue;
            Heirloom h = Make((CharacterId)id);
            if (h == null) continue;
            h.host = this;
            h.hero = (CharacterId)id;
            h.Init();
            live.Add(h);
        }
    }

    void Update()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (player == null || player.IsDying || Time.timeScale <= 0f || RelayRun.Swapping) return;
        using (DamageSource.As(Source))
            for (int i = 0; i < live.Count; i++)
            {
                try { live[i].Tick(Time.deltaTime); }
                catch (System.Exception e) { Debug.LogException(e); live[i].Dispose(); live.RemoveAt(i--); }
            }
    }

    void OnKilled(Vector3 pos)
    {
        if (player == null || player.IsDying) return;
        using (DamageSource.As(Source)) foreach (Heirloom h in live) h.OnKill(pos);
    }

    void OnUlt()
    {
        if (player == null || player.IsDying) return;
        using (DamageSource.As(Source)) foreach (Heirloom h in live) h.OnUlt();
    }

    void OnHurt(float taken, float before)
    {
        if (player == null) return;
        using (DamageSource.As(Source)) foreach (Heirloom h in live) h.OnHurt();
    }

    // 코루틴도 시작할 때의 피해 출처(유품)를 이어 감
    public new Coroutine StartCoroutine(IEnumerator routine)
    {
        using (DamageSource.As(Source)) return base.StartCoroutine(DamageSource.Keep(routine));
    }

    public PlayerController Player => player;

    static Heirloom Make(CharacterId hero)
    {
        switch (hero)
        {
            case CharacterId.Gunner: return new LastMagazine();
            case CharacterId.Swordsman: return new RustyBlade();
            case CharacterId.Rogue: return new ShadowSteps();
            case CharacterId.Archer: return new BrokenBow();
            case CharacterId.Alchemist: return new CrackedFlask();
            case CharacterId.Cartographer: return new FoldedMap();
            case CharacterId.Clockmaker: return new StoppedWatch();
            case CharacterId.Postman: return new LastLetter();
            case CharacterId.Ghost: return new ColdHand();
            case CharacterId.Wrestler: return new Waistband();
            case CharacterId.Foreman: return new WorkSign();
            case CharacterId.Puppeteer: return new LeftoverString();
            case CharacterId.Undertaker: return new OneShovel();
            case CharacterId.Photographer: return new LastFilm();
        }
        return null;
    }

    // ================================================================= 공통 바탕
    abstract class Heirloom
    {
        public RelayHeirlooms host;
        public CharacterId hero;
        protected float readyAt;                            // 쿨이 끝나는 시각 (Time.time)
        public float Remaining => Mathf.Max(0f, readyAt - Time.time);

        protected PlayerController P => host.player;
        protected Vector3 Pos => P.transform.position;
        protected float Atk => P.damage * P.damageMultiplier;
        protected Vector3 Aim
        {
            get
            {
                SpecialAbilities s = SpecialAbilities.SharedInstance;
                Vector2 a = s != null ? s.AimDirection : Vector2.right;
                return a.sqrMagnitude < 0.01f ? Vector3.right : (Vector3)a.normalized;
            }
        }
        protected Vector3 Cursor => SpecialAbilities.SharedInstance != null ? SpecialAbilities.SharedInstance.MouseWorldPos : Pos + Aim * 4f;

        public void Init() { readyAt = Time.time + 2f; Begin(); }        // 이어받은 직후 2초는 쉼
        public virtual void Begin() { }
        public virtual void Tick(float dt) { }
        public virtual void OnKill(Vector3 pos) { }
        public virtual void OnUlt() { }
        public virtual void OnHurt() { }
        public virtual void Dispose() { }

        // 쿨 타이머: 시간이 되면 true 를 돌려주고 다음 시각을 정함
        protected bool Ready(float cooldown)
        {
            if (Time.time < readyAt) return false;
            readyAt = Time.time + cooldown;
            return true;
        }
        protected bool Due => Time.time >= readyAt;

        // ---- 적 찾기 · 때리기 (EnermyController.Live · bosses)
        protected static void Foes(Vector3 c, float r, List<GameObject> into, bool bosses = true, bool mid = true)
        {
            into.Clear();
            float r2 = r * r;
            foreach (EnermyController e in EnermyController.Live)
            {
                if (e == null || e.IsDead || !e.isActiveAndEnabled || (!mid && e.IsMidBoss)) continue;
                Vector3 d = e.transform.position - c; d.z = 0f;
                if (d.sqrMagnitude <= r2) into.Add(e.gameObject);
            }
            if (!bosses) return;
            foreach (bosss b in bosss.Live)
            {
                if (b == null || b.IsDead) continue;
                Vector3 d = b.transform.position - c; d.z = 0f;
                if (d.sqrMagnitude <= r2) into.Add(b.gameObject);
            }
        }
        protected static readonly List<GameObject> tmp = new List<GameObject>(64);

        protected static void Hit(GameObject g, float amount, Vector3 from, float knock = 0.4f)
        {
            if (g == null) return;
            Vector3 d = g.transform.position - from; d.z = 0f;
            Specials.Damage(g, amount, d.sqrMagnitude < 0.0001f ? Vector3.right : d.normalized, knock);
        }

        protected static bool OnScreen(Vector3 w)
        {
            Camera c = Camera.main;
            if (c == null) return true;
            Vector3 v = c.WorldToViewportPoint(w);
            return v.z > 0f && v.x > -0.02f && v.x < 1.02f && v.y > -0.02f && v.y < 1.02f;
        }

        protected static void Ring(Vector3 at, float radius, Color c, float seconds = 0.3f) => ShockRing.Spawn(at, radius * 0.3f, radius, seconds, c, 0.35f);

        // 화면 안 적 (거리순)
        protected List<EnermyController> OnScreenFoes(bool mid = true)
        {
            List<EnermyController> r = new List<EnermyController>();
            foreach (EnermyController e in EnermyController.Live)
                if (e != null && !e.IsDead && e.isActiveAndEnabled && (mid || !e.IsMidBoss) && OnScreen(e.transform.position)) r.Add(e);
            Vector3 p = Pos;
            r.Sort((a, b) => (a.transform.position - p).sqrMagnitude.CompareTo((b.transform.position - p).sqrMagnitude));
            return r;
        }
    }

    // ================================================================= 14종
    // 거너 · 마지막 탄창: 필살기를 쓸 때마다 화면 안 적 최대 6마리에 리볼버 탄 한 발씩 (공격력 120%)
    sealed class LastMagazine : Heirloom
    {
        public override void OnUlt()
        {
            int n = 0;
            foreach (EnermyController e in OnScreenFoes())
            {
                if (n++ >= 6) break;
                Hit(e.gameObject, Atk * 1.2f, Pos, 0.5f);
                Fx.Spawn("fx_sparkle", e.transform.position, 1.4f, new Color(1f, 0.9f, 0.55f), 20f);
            }
            if (n > 0) Hostile.Play("pulse", 0.4f, 1.8f);
        }
    }

    // 검사 · 녹슨 장검: 4초마다 둘레 2.5칸에 적이 셋 이상이면 회전 베기 한 바퀴 (공격력 80%)
    sealed class RustyBlade : Heirloom
    {
        public override void Tick(float dt)
        {
            if (!Due) return;
            Foes(Pos, 2.5f, tmp);
            if (tmp.Count < 3) return;
            readyAt = Time.time + 4f;
            foreach (GameObject g in tmp.ToArray()) Hit(g, Atk * 0.8f, Pos, 1f);
            Ring(Pos, 2.7f, new Color(0.85f, 0.7f, 0.5f), 0.25f);
            Hostile.Play("pulse", 0.3f, 1.4f);
        }
    }

    // 도적 · 그림자 발자국: 맞을 때마다(쿨 6초) 가장 가까운 적 쪽으로 출혈 선 4칸 (지나간 적 출혈 3초, 초당 공격력 40%)
    sealed class ShadowSteps : Heirloom
    {
        public override void OnHurt()
        {
            if (!Ready(6f)) return;
            Vector3 dir = Aim;
            List<EnermyController> near = OnScreenFoes();
            if (near.Count > 0) { dir = near[0].transform.position - Pos; dir.z = 0f; dir.Normalize(); }
            Foes(Pos, 5f, tmp);
            foreach (GameObject g in tmp.ToArray())
            {
                Vector3 rel = g.transform.position - Pos; rel.z = 0f;
                float along = Vector3.Dot(rel, dir);
                if (along < -0.3f || along > 4f || (rel - dir * along).magnitude > 1.1f) continue;
                Bleed.Apply(g, Atk * 0.4f, 3f);
            }
            for (int i = 1; i <= 4; i++) Fx.Spawn("fx_bleed", Pos + dir * i, 1.6f, new Color(0.7f, 0.5f, 0.9f), 14f);
        }
    }

    // 궁수 · 꺾인 활: 10초마다 커서 쪽으로 가득 당긴 화살 한 발 자동 (모두 관통, 공격력 250%)
    sealed class BrokenBow : Heirloom
    {
        public override void Tick(float dt)
        {
            if (!Ready(10f)) return;
            Vector3 dir = Cursor - Pos; dir.z = 0f;
            dir = dir.sqrMagnitude < 0.01f ? Aim : dir.normalized;
            const float Len = 22f;
            Foes(Pos, Len, tmp);
            foreach (GameObject g in tmp.ToArray())
            {
                Vector3 rel = g.transform.position - Pos; rel.z = 0f;
                float along = Vector3.Dot(rel, dir);
                if (along < 0f || along > Len || (rel - dir * along).magnitude > 1f) continue;
                Hit(g, Atk * 2.5f, Pos, 0.8f);
            }
            LineRenderer lr = Hostile.NewLine("HeirloomArrow", new Color(0.8f, 1f, 0.6f), 0.28f, 20);
            lr.positionCount = 2;
            lr.SetPosition(0, Pos); lr.SetPosition(1, Pos + dir * Len);
            Object.Destroy(lr.gameObject, 0.18f);
            Hostile.Play("pulse", 0.4f, 2f);
        }
    }

    // 연금술사 · 깨진 플라스크: 적 12마리를 처치할 때마다 마지막 처치 자리에서 플라스크 폭발 (반경 2.5, 공격력 150%)
    sealed class CrackedFlask : Heirloom
    {
        int kills;
        public override void OnKill(Vector3 pos)
        {
            if (++kills % 12 != 0) return;
            Foes(pos, 2.5f, tmp);
            foreach (GameObject g in tmp.ToArray()) Hit(g, Atk * 1.5f, pos, 0.8f);
            Ring(pos, 2.5f, new Color(0.5f, 1f, 0.6f), 0.3f);
            Fx.Spawn("fx_shock", pos, 4.5f, new Color(0.5f, 1f, 0.6f), 16f);
        }
    }

    // 지도 제작자 · 접힌 지도: 걸은 자리에 잉크 꼬리 3초 (닿은 적 초당 공격력 30%)
    sealed class FoldedMap : Heirloom
    {
        struct Dot { public Vector3 pos; public float until; }
        readonly List<Dot> trail = new List<Dot>();
        LineRenderer line;
        float tickAt;
        Vector3 last;

        public override void Begin() { last = Pos; }
        public override void Tick(float dt)
        {
            if ((Pos - last).magnitude >= 0.5f) { trail.Add(new Dot { pos = Pos, until = Time.time + 3f }); last = Pos; }
            trail.RemoveAll(d => Time.time > d.until);
            if (line == null) { line = Hostile.NewLine("HeirloomInk", new Color(0.35f, 0.3f, 0.6f, 0.9f), 0.3f, 6); }
            line.positionCount = trail.Count;
            for (int i = 0; i < trail.Count; i++) line.SetPosition(i, trail[i].pos);
            if (Time.time < tickAt || trail.Count == 0) return;
            tickAt = Time.time + 0.25f;
            HashSet<GameObject> hit = new HashSet<GameObject>();
            foreach (Dot d in trail)
            {
                Foes(d.pos, 0.9f, tmp);
                foreach (GameObject g in tmp) hit.Add(g);
            }
            foreach (GameObject g in hit) Hit(g, Atk * 0.3f * 0.25f, Pos, 0f);
        }
        public override void Dispose() { if (line != null) Object.Destroy(line.gameObject); }
    }

    // 시계공 · 멈춘 회중시계: 15초마다 「째깍」 둘레 4칸 적 0.6초 정지 (보스 제외 · 시간 자체는 건드리지 않음 — 손맛 원칙)
    sealed class StoppedWatch : Heirloom
    {
        public override void Tick(float dt)
        {
            if (!Ready(15f)) return;
            Foes(Pos, 4f, tmp, false, false);
            foreach (GameObject g in tmp.ToArray()) { EnermyController e = g.GetComponent<EnermyController>(); if (e != null) e.Slow(0f, 0.6f); }
            Ring(Pos, 4f, new Color(0.7f, 0.8f, 1f), 0.35f);
            Hostile.Play("chime", 0.4f, 1.6f);
        }
    }

    // 우체부 · 마지막 우편: 10번째 처치마다 화면 안 가장 먼 적에게 소포 (착탄 반경 2, 공격력 200%)
    sealed class LastLetter : Heirloom
    {
        int kills;
        public override void OnKill(Vector3 pos)
        {
            if (++kills % 10 != 0) return;
            List<EnermyController> far = OnScreenFoes();
            if (far.Count == 0) return;
            Vector3 target = far[far.Count - 1].transform.position;
            host.StartCoroutine(Land(target));
        }
        IEnumerator Land(Vector3 at)
        {
            Fx.Spawn("fx_sparkle", at, 2.4f, new Color(1f, 0.85f, 0.5f), 14f);
            yield return new WaitForSeconds(0.45f);
            if (host == null || host.player == null) yield break;
            Foes(at, 2f, tmp);
            foreach (GameObject g in tmp.ToArray()) Hit(g, Atk * 2f, at, 0.8f);
            Ring(at, 2f, new Color(1f, 0.8f, 0.4f), 0.3f);
            Hostile.Play("bigboom", 0.3f, 1.7f);
        }
    }

    // 유령 · 차가운 손: 지금 영웅 평타에 맞은 적에게 냉기 1겹 (0.5초에 한 번) — 다섯 겹이면 얼림 (유령 냉기 규칙)
    //   평타 피해가 늘었는지(RunStats 출처 「평타」)로 「맞았다」를 읽음 · 냉기 겹은 적 쪽 상태가 아니라 이 유품이 셈
    sealed class ColdHand : Heirloom
    {
        readonly Dictionary<EnermyController, int> chill = new Dictionary<EnermyController, int>();
        float lastBasic, nextAt;
        public override void Begin() { lastBasic = Basic; }
        static float Basic => RunStats.DamageDealt.TryGetValue(DamageSource.Basic, out float v) ? v : 0f;
        public override void Tick(float dt)
        {
            if (Time.time < nextAt) return;
            float b = Basic;
            if (b <= lastBasic + 0.01f) return;
            lastBasic = b;
            nextAt = Time.time + 0.5f;
            EnermyController best = null;
            float bd = 100f;
            Vector3 cur = Cursor;
            foreach (EnermyController e in OnScreenFoes(false))
            {
                float d = (e.transform.position - cur).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            if (best == null || bd > 36f) return;
            chill.TryGetValue(best, out int n);
            n++;
            Fx.Spawn("fx_sparkle", best.transform.position, 1.4f, new Color(0.6f, 0.9f, 1f), 20f);
            if (n >= 5) { n = 0; best.Slow(0f, 1f); Ring(best.transform.position, 1.6f, new Color(0.6f, 0.9f, 1f), 0.25f); }
            chill[best] = n;
            if (chill.Count > 48) { List<EnermyController> gone = new List<EnermyController>(); foreach (var kv in chill) if (kv.Key == null || kv.Key.IsDead) gone.Add(kv.Key); foreach (var k in gone) chill.Remove(k); }
        }
    }

    // 씨름꾼 · 샅바: 8초마다 몸에 닿은 일반 적 하나를 들어 반대편 4칸으로 메침 (공격력 200% · 떨어진 자리 반경 1.5 같은 피해)
    sealed class Waistband : Heirloom
    {
        public override void Tick(float dt)
        {
            if (!Due) return;
            EnermyController pick = null;
            float bd = 2.4f * 2.4f;
            foreach (EnermyController e in EnermyController.Live)
            {
                if (e == null || e.IsDead || e.IsMidBoss || !EnemyHold.CanTake(e)) continue;
                float d = (e.transform.position - Pos).sqrMagnitude;
                if (d < bd) { bd = d; pick = e; }
            }
            if (pick == null || !EnemyHold.TryTake(pick, host)) return;
            readyAt = Time.time + 8f;
            host.StartCoroutine(Throw(pick));
        }
        IEnumerator Throw(EnermyController e)
        {
            Vector3 from = e.transform.position;
            Vector3 away = Pos - from; away.z = 0f;
            Vector3 to = Hostile.ClampArena(Pos + (away.sqrMagnitude < 0.01f ? -Aim : away.normalized) * 4f);
            float t = 0f;
            while (t < 0.45f && e != null && !e.IsDead && host != null && host.player != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.45f);
                EnemyHold.Place(e, Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.6f);
                yield return null;
            }
            if (e == null) yield break;
            EnemyHold.Release(e, host);
            if (e.IsDead) yield break;
            Vector3 land = EnemyHold.FreeSpot(to);
            EnemyHold.Place(e, land);
            Foes(land, 1.5f, tmp);
            foreach (GameObject g in tmp.ToArray()) Hit(g, Atk * 2f, land, 0.8f);
            e.Slow(0f, 0.8f);
            Ring(land, 1.5f, new Color(0.9f, 0.7f, 0.4f), 0.3f);
            Hostile.Play("bigboom", 0.35f, 1.5f);
        }
        public override void Dispose() { EnemyHold.ReleaseAll(host); }
    }

    // 공사 반장 · 공사 중 표지판: 필살기를 쓸 때마다 영웅 뒤쪽 2칸에 「공사 중」 바리케이드 2개 6초
    //   (적 길막 → 센 감속 + 처음 닿을 때 공격력 50% — 사람 몸이 막는 물리 벽은 영웅 이동도 막을 수 있어 감속으로 대신)
    sealed class WorkSign : Heirloom
    {
        class Fence { public GameObject go; public float until; public HashSet<GameObject> touched = new HashSet<GameObject>(); }
        readonly List<Fence> fences = new List<Fence>();
        public override void OnUlt()
        {
            Vector3 back = -Aim;
            Vector3 side = new Vector3(-back.y, back.x, 0f);
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 at = Pos + back * 2f + side * (0.9f * i);
                GameObject go = new GameObject("HeirloomFence");
                go.transform.position = at;
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpecialAbilities.GlowSprite;
                sr.color = new Color(1f, 0.6f, 0.15f, 0.9f);
                sr.sortingLayerName = "Effect";
                sr.sortingOrder = 9;
                go.transform.localScale = new Vector3(1.4f, 0.5f, 1f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(side.y, side.x) * Mathf.Rad2Deg);
                fences.Add(new Fence { go = go, until = Time.time + 6f });
            }
            Hostile.Play("pulse", 0.3f, 0.8f);
        }
        public override void Tick(float dt)
        {
            for (int i = fences.Count - 1; i >= 0; i--)
            {
                Fence f = fences[i];
                if (f.go == null || Time.time > f.until) { if (f.go != null) Object.Destroy(f.go); fences.RemoveAt(i); continue; }
                Foes(f.go.transform.position, 1.1f, tmp);
                foreach (GameObject g in tmp.ToArray())
                {
                    EnermyController e = g.GetComponent<EnermyController>();
                    if (e != null) e.Slow(0.1f, 0.3f);
                    if (f.touched.Add(g)) Hit(g, Atk * 0.5f, f.go.transform.position, 0.3f);
                }
            }
        }
        public override void Dispose() { foreach (Fence f in fences) if (f.go != null) Object.Destroy(f.go); fences.Clear(); }
    }

    // 인형사 · 남은 줄: 작은 더미 하나가 영웅 2칸 뒤를 따라다니며 3초마다 둘레 1.5칸 휘두름 (공격력 70%)
    sealed class LeftoverString : Heirloom
    {
        GameObject doll;
        Vector3 lastPos, dir = Vector3.left;
        public override void Begin()
        {
            doll = new GameObject("HeirloomDoll");
            SpriteRenderer sr = doll.AddComponent<SpriteRenderer>();
            sr.sprite = SpecialAbilities.GlowSprite;
            sr.color = new Color(0.8f, 0.55f, 1f, 0.95f);
            sr.sortingLayerName = "Effect";
            sr.sortingOrder = 10;
            doll.transform.localScale = Vector3.one * 0.7f;
            doll.transform.position = Pos - Vector3.right * 2f;
            lastPos = Pos;
        }
        public override void Tick(float dt)
        {
            if (doll == null) return;
            Vector3 mv = Pos - lastPos; mv.z = 0f;
            if (mv.sqrMagnitude > 0.0004f) dir = -mv.normalized;
            lastPos = Pos;
            doll.transform.position = Vector3.Lerp(doll.transform.position, Pos + dir * 2f, 1f - Mathf.Exp(-6f * dt));
            if (!Ready(3f)) return;
            Vector3 at = doll.transform.position;
            Foes(at, 1.5f, tmp);
            foreach (GameObject g in tmp.ToArray()) Hit(g, Atk * 0.7f, at, 0.6f);
            Ring(at, 1.5f, new Color(0.8f, 0.55f, 1f), 0.25f);
        }
        public override void Dispose() { if (doll != null) Object.Destroy(doll); }
    }

    // 장의사 · 삽 한 자루: 적 처치 때 20% 확률로 그 자리 구멍 2초 (지나던 적이 빠져 1초 못 나옴)
    sealed class OneShovel : Heirloom
    {
        class Hole { public GameObject go; public float until; }
        readonly List<Hole> holes = new List<Hole>();
        readonly Dictionary<EnermyController, float> fallen = new Dictionary<EnermyController, float>();
        public override void OnKill(Vector3 pos)
        {
            if (Random.value > 0.2f || holes.Count >= 10) return;
            GameObject go = new GameObject("HeirloomHole");
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpecialAbilities.GlowSprite;
            sr.color = new Color(0.08f, 0.05f, 0.1f, 0.95f);
            sr.sortingLayerName = "Effect";
            sr.sortingOrder = 3;
            go.transform.localScale = new Vector3(1.8f, 1.2f, 1f);
            holes.Add(new Hole { go = go, until = Time.time + 2f });
        }
        public override void Tick(float dt)
        {
            for (int i = holes.Count - 1; i >= 0; i--)
            {
                Hole h = holes[i];
                if (h.go == null || Time.time > h.until) { if (h.go != null) Object.Destroy(h.go); holes.RemoveAt(i); continue; }
                Foes(h.go.transform.position, 0.9f, tmp, false, false);
                foreach (GameObject g in tmp.ToArray())
                {
                    EnermyController e = g.GetComponent<EnermyController>();
                    if (e == null || (fallen.TryGetValue(e, out float at) && Time.time < at + 3f)) continue;
                    fallen[e] = Time.time;
                    e.Slow(0f, 1f);
                }
            }
            if (fallen.Count > 64) { List<EnermyController> gone = new List<EnermyController>(); foreach (var kv in fallen) if (kv.Key == null || Time.time > kv.Value + 3f) gone.Add(kv.Key); foreach (var k in gone) fallen.Remove(k); }
        }
        public override void Dispose() { foreach (Hole h in holes) if (h.go != null) Object.Destroy(h.go); holes.Clear(); }
    }

    // 사진사 · 마지막 필름: 20초마다 커서 자리 3×3칸 셔터 한 번 — 틀 안 적이 종이 (2초 · 받는 피해 +30%)
    //   받는 피해 +30% 는 사진사 키트의 종이 상태에 묶여 있어, 유품은 그 몫을 즉시 피해(공격력 60%)와 2초 감속으로 대신함
    sealed class LastFilm : Heirloom
    {
        public override void Tick(float dt)
        {
            if (!Ready(20f)) return;
            Vector3 c = Cursor; c.z = 0f;
            Vector3 d = c - Pos; d.z = 0f;
            if (d.magnitude > 12f) c = Pos + d.normalized * 12f;
            Foes(c, 2.2f, tmp);
            foreach (GameObject g in tmp.ToArray())
            {
                Vector3 r = g.transform.position - c;
                if (Mathf.Abs(r.x) > 1.5f || Mathf.Abs(r.y) > 1.5f) continue;
                Hit(g, Atk * 0.6f, c, 0f);
                EnermyController e = g.GetComponent<EnermyController>();
                if (e != null && !e.IsMidBoss) e.Slow(0.15f, 2f);
            }
            LineRenderer lr = Hostile.NewLine("HeirloomFrame", Color.white, 0.12f, 20);
            lr.positionCount = 5;
            lr.SetPosition(0, c + new Vector3(-1.5f, -1.5f)); lr.SetPosition(1, c + new Vector3(1.5f, -1.5f));
            lr.SetPosition(2, c + new Vector3(1.5f, 1.5f)); lr.SetPosition(3, c + new Vector3(-1.5f, 1.5f)); lr.SetPosition(4, c + new Vector3(-1.5f, -1.5f));
            Object.Destroy(lr.gameObject, 0.3f);
            Hostile.Play("chime", 0.4f, 1.9f);
        }
    }
}
