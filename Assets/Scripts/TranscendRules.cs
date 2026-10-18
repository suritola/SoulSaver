using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 초월 카드 「새 규칙」 (1.2~, LevelShop.Transcend 가 고른 단계를 들고 있고 여기서 판의 규칙을 바꿈)
// 기본 능력치(공격력 · 체력 · 공속 · 치명 · 이동 속도)는 건드리지 않고, 상황 · 조건에서 터지는 효과만
//   메아리 · 영혼 수확 · 시간의 틈 · 연금 손 · 그림자 분신 · 중력장 · 역전 · 맵 지배 · 연쇄 붕괴 · 영혼 보호막 · 영혼불 발자국
// LevelShop 오브젝트에 붙음 (판마다 새 씬이라 상태도 판마다 새로) · 다른 코드는 정적 입구(Coin · Ward · PropBroken)로 부름
public class TranscendRules : MonoBehaviour
{
    public const int Echo = 0, Reap = 1, Rift = 2, Alchemy = 3, Shadow = 4, Gravity = 5, Reversal = 6, Dominion = 7, Cascade = 8, Ward = 9, Embers = 10;
    public const int Count = 11;

    public static TranscendRules Instance { get; private set; }

    static readonly Color Gold = new Color(1f, 0.84f, 0.32f);
    static readonly Color SoulPurple = new Color(0.72f, 0.5f, 1f);
    static readonly Color TimeBlue = new Color(0.6f, 0.85f, 1f);
    static readonly Color AlchemyGreen = new Color(0.6f, 1f, 0.55f);
    static readonly Color ShadowTint = new Color(0.32f, 0.18f, 0.5f, 0.62f);
    static readonly Color EmberOrange = new Color(1f, 0.55f, 0.25f, 0.85f);
    static readonly Color SoulBlue = new Color(0.55f, 0.88f, 1f, 0.9f);       // 1.3.7~ 영혼불 발자국 불똥

    // ---------------- 수치 (카드 단계마다, [단계 - 1])
    const float EchoRadius = 9f, EchoMul = 6f, EchoFreeze = 1.5f, EchoDelay = 1.2f;
    static readonly int[] ReapEvery = { 150, 110, 75 };
    const float RiftAt = 0.4f, RiftMul = 0.12f;
    static readonly float[] RiftSeconds = { 3f, 4f, 4f };
    static readonly float[] RiftCooldown = { 60f, 40f, 40f };
    static readonly float[] AlchemyChance = { 0.10f, 0.18f, 0.25f };
    const float AlchemyExp = 0.04f;                     // 경험치 막대의 4%
    const float ShadowEvery = 1.2f, ShadowRange = 9f, ShadowMul = 0.8f, ShadowLag = 0.28f;
    const float GravityDelay = 1f, GravityPull = 2.4f, GravityMagnet = 14f;
    static readonly float[] GravityRadius = { 6f, 8f, 9.5f };
    const float ReversalRadius = 10f, ReversalMul = 12f, ReversalInvincible = 4f, ReversalHeal = 0.5f;
    static readonly float[] DominionRespawn = { 18f, 10f };
    static readonly float[] DominionRadius = { 4.5f, 6f };
    const float DominionMul = 5f;
    static readonly float[] CascadeChance = { 0.12f, 0.18f, 0.25f };
    static readonly float[] CascadeRadius = { 2.6f, 2.6f, 3.3f };
    const float CascadeMul = 2.5f;
    const int CascadePerSecond = 10;
    static readonly float[] WardEvery = { 8f, 5f, 5f };
    const float WardBlastRadius = 5f;
    static readonly float[] EmberLife = { 2.5f, 3.5f, 4.5f };
    static readonly float[] EmberRadius = { 1.2f, 1.2f, 1.6f };
    const float EmberDps = 0.6f, EmberStep = 1.1f;
    const int EmberMax = 26;

    LevelShop shop;
    PlayerController p;
    float lastHp = -1f;

    int L(int k) => shop != null ? shop.TranscendLevel(k) : 0;
    public static int Level(int k) => Instance != null ? Instance.L(k) : 0;

    public static TranscendRules Ensure(LevelShop s)
    {
        if (s == null) return null;
        TranscendRules r = s.GetComponent<TranscendRules>();
        if (r == null) r = s.gameObject.AddComponent<TranscendRules>();
        r.shop = s;
        Instance = r;
        return r;
    }

    void OnEnable()
    {
        EnermyController.Killed += OnKilled;
        PlayerController.UltUsed += OnUlt;
        LastStand.Fired += OnLastStand;
    }

    void OnDisable()
    {
        EnermyController.Killed -= OnKilled;
        PlayerController.UltUsed -= OnUlt;
        LastStand.Fired -= OnLastStand;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        foreach (ShadowClone c in shadows) if (c.go != null) Destroy(c.go);
        shadows.Clear();
        foreach (EmberPatch e in embers) if (e.fx != null) Destroy(e.fx.gameObject);
        embers.Clear();
        if (wardRing != null) Destroy(wardRing.gameObject);
        SetGravity(false);
        if (riftOn && Mathf.Approximately(EnermyController.GlobalSpeedMultiplier, RiftMul)) EnermyController.GlobalSpeedMultiplier = 1f;
    }

    // 카드를 고른 직후 (이어하기로 다시 적용할 때는 silent)
    public void OnPicked(int k, bool silent)
    {
        if (k == Reversal && L(Reversal) == 2) LastStand.Extra += 1;        // 2단계: 장마다 한 번 더
        if (k == Ward && L(Ward) == 1) { wardReady = false; wardAt = Time.time + 1.5f; }
        p = Hostile.Player;
        if (silent || p == null) return;
        Vector3 at = p.transform.position;
        ShockRing.Spawn(at, 0.5f, 7f, 0.6f, Gold, 0.4f);
        Fx.Spawn("fx_soulburst", at, 5f, Gold, 16f);
        Hostile.Play("evoflair", 0.7f, 1.1f);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        p = Hostile.Player;
        if (p == null || p.IsDying)
        {
            SetGravity(false);
            return;
        }
        bool hurt = lastHp >= 0f && p.PlayerHealth < lastHp - 0.01f;
        lastHp = p.PlayerHealth;

        if (reapPending) { reapPending = false; ReapPulse(); }
        TickRift(hurt);
        TickShadows();
        TickGravity();
        TickCascade();
        TickWard(hurt);
        TickEmbers();
    }

    // ================================================================= 공용: 둘레 적에게 피해 (보스 포함)
    static readonly List<Collider2D> hits = new List<Collider2D>(128);
    static readonly List<GameObject> targets = new List<GameObject>(64);

    static int Blast(Vector3 c, float r, float damage, float knock, float freeze, bool bosses = true)
    {
        targets.Clear();
        foreach (Collider2D col in Specials.Overlap(c, r, hits))
        {
            if (col == null) continue;
            bool boss = col.CompareTag("boss");
            if (!col.CompareTag("enermy") && !(boss && bosses)) continue;
            GameObject go = col.gameObject;
            if (targets.Contains(go)) continue;
            EnermyController e = go.GetComponent<EnermyController>();
            if (e != null && e.IsDead) continue;
            if (e == null && go.GetComponent<bosss>() == null) continue;
            targets.Add(go);
        }
        int n = 0;
        using (DamageSource.As(DamageSource.Special))
            foreach (GameObject go in targets.ToArray())
            {
                if (go == null) continue;
                Vector3 d = go.transform.position - c;
                d.z = 0f;
                if (d.sqrMagnitude < 0.01f) d = Vector3.right;
                if (freeze > 0f && go.TryGetComponent(out EnermyController e)) e.Slow(0f, freeze);
                Specials.Damage(go, damage, d.normalized, knock);
                n++;
            }
        return n;
    }

    static void ClearShots(Vector3 c, float r)
    {
        for (int i = HostileProjectile.Live.Count - 1; i >= 0; i--)
        {
            HostileProjectile h = HostileProjectile.Live[i];
            if (h != null && Vector2.Distance(h.transform.position, c) < r) Destroy(h.gameObject);
        }
    }

    static void Float(Vector3 at, string key, Color color, float size = 5.5f)
    {
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.FloatText(at + Vector3.up * 1.5f, Loc.T(key), color, size, 0f);
    }

    // ================================================================= 메아리: 필살기가 끝나고 잠시 뒤 그 자리에 영혼 메아리
    bool echoing;

    void OnUlt()
    {
        if (L(Echo) > 0 && !echoing && isActiveAndEnabled) StartCoroutine(EchoRoutine());
    }

    IEnumerator EchoRoutine()
    {
        echoing = true;
        yield return null;
        float guard = Time.unscaledTime + 12f;
        while (p != null && p.IsSkillUsing && Time.unscaledTime < guard) yield return null;      // 조준 · 일제 사격이 끝난 뒤
        int lv = L(Echo);
        int times = lv >= 3 ? 2 : 1;
        for (int i = 0; i < times; i++)
        {
            yield return new WaitForSeconds(i == 0 ? EchoDelay : 0.9f);
            if (p == null || p.IsDying) break;
            EchoPulse(p.transform.position, lv, i);
        }
        echoing = false;
    }

    void EchoPulse(Vector3 c, int lv, int wave)
    {
        float r = EchoRadius + wave * 2.5f;
        Blast(c, r, p.damage * EchoMul, 4f, lv >= 2 ? EchoFreeze : 0f);
        ClearShots(c, r);
        ShockRing.Spawn(c, 0.5f, r, 0.55f, Gold, 0.55f);
        ShockRing.Spawn(c, 0.5f, r * 0.65f, 0.4f, new Color(1f, 0.97f, 0.85f, 0.9f), 0.3f);
        Fx.Spawn("fx_shock", c, r * 2f, Gold, 14f);
        Fx.Spawn("fx_soulburst", c, 5.5f, Gold, 16f);
        if (lv >= 2) Fx.Spawn("fx_shock", c, r * 1.6f, TimeBlue, 12f);
        Hostile.Play("ultcast", 0.55f, 0.75f + wave * 0.15f);
        Hostile.Play("pulse", 0.8f, 0.6f);
        Hostile.Shake(0.2f);
        Float(c, "메아리!", Gold);
    }

    // ================================================================= 영혼 수확: 처치 수마다 둘레 잡몹을 쓸어 버리는 파동 (맵 물체 「정화」와 같은 규칙)
    int reapKills;
    bool reapPending, reaping;

    void OnKilled(Vector3 at)
    {
        if (Instance != this) return;
        int cl = L(Cascade);
        if (cl > 0 && cascadeQueue.Count < 24 && Random.value < CascadeChance[cl - 1]) cascadeQueue.Add(at);
        int rl = L(Reap);
        if (rl > 0 && !reaping && EnermyController.KillCounts && ++reapKills >= ReapEvery[rl - 1])
        {
            reapKills = 0;
            reapPending = true;                 // 죽는 도중에 다른 적을 죽이지 않게 다음 프레임에
        }
    }

    void ReapPulse()
    {
        if (p == null) return;
        Vector3 me = p.transform.position;
        reaping = true;
        try { MapProps.DropPurify(new MapProps.DropCtx { player = p, at = me }); }
        finally { reaping = false; }
        Fx.Spawn("fx_soulburst", me, 9f, SoulPurple, 14f);
        ShockRing.Spawn(me, 0.5f, MapProps.PurifyRadius * 1.1f, 0.8f, new Color(0.75f, 0.55f, 1f, 0.9f), 0.6f);
        Hostile.Play("wail", 0.45f, 1.3f);
        Float(me + Vector3.up * 0.8f, "영혼 수확!", SoulPurple, 6.5f);
    }

    // ================================================================= 시간의 틈: 체력이 낮을 때 맞으면 세상이 거의 멈춤
    float riftReadyAt;
    bool riftOn;

    void TickRift(bool hurt)
    {
        int lv = L(Rift);
        if (lv <= 0 || riftOn || !hurt || Time.time < riftReadyAt) return;
        if (p.PlayerHealth <= 0f || p.PlayerHealth > p.PlayerMaxHealth * RiftAt) return;
        StartCoroutine(RiftRoutine(lv));
    }

    IEnumerator RiftRoutine(int lv)
    {
        riftOn = true;
        float dur = RiftSeconds[lv - 1];
        riftReadyAt = Time.time + RiftCooldown[lv - 1];
        EnermyController.GlobalSpeedMultiplier = RiftMul;
        if (lv >= 3) p.GrantInvincibility(dur);
        Vector3 at = p.transform.position;
        ShockRing.Spawn(at, 18f, 1f, 0.7f, new Color(0.6f, 0.85f, 1f, 0.95f), 0.5f);       // 안으로 접혀 드는 고리
        Fx.Spawn("fx_shock", at, 14f, TimeBlue, 10f);
        FxAnim clock = Fx.Play("fx_rune", at, 12f, new Color(0.6f, 0.85f, 1f, 0.4f), 1f, 0f, 1, true, dur);
        if (clock != null) { clock.follow = p.transform; clock.spin = 70f; }
        Hostile.Play("rewind", 0.8f, 0.8f);
        Hostile.Play("freeze", 0.5f, 0.7f);
        Float(at, "시간의 틈!", TimeBlue, 6.5f);
        float end = Time.time + dur, tick = 0f;
        while (Time.time < end)
        {
            if (Time.time >= tick) { tick = Time.time + 0.5f; Hostile.Play("tick", 0.35f, 0.8f); }
            yield return null;
        }
        if (Mathf.Approximately(EnermyController.GlobalSpeedMultiplier, RiftMul)) EnermyController.GlobalSpeedMultiplier = 1f;     // 그 사이 다른 효과가 바꿨으면 그대로
        if (p != null) ShockRing.Spawn(p.transform.position, 1f, 12f, 0.4f, new Color(0.6f, 0.85f, 1f, 0.6f), 0.3f);
        Hostile.Play("tick", 0.5f, 1.3f);
        riftOn = false;
    }

    // ================================================================= 연금 손: 줍는 코인이 확률로 경험치로 연성 (Juice.CoinPicked 가 부름)
    float alchemyTextAt;

    public static void Coin(Vector3 at)
    {
        if (Instance != null) Instance.OnCoin(at);
    }

    void OnCoin(Vector3 at)
    {
        int lv = L(Alchemy);
        if (lv <= 0 || p == null || p.needEXP <= 0f || Random.value >= AlchemyChance[lv - 1]) return;
        p.nowEXP += p.needEXP * AlchemyExp;
        Fx.Spawn("fx_sparkle", at + Vector3.up * 0.3f, 1.4f, AlchemyGreen, 18f);
        if (lv >= 3) SoulShards.Add(1, at, false);
        if (Time.time < alchemyTextAt) return;
        alchemyTextAt = Time.time + 0.7f;
        Float(at, "연성!", AlchemyGreen, 4.5f);
        Hostile.Play("glassclink", 0.35f, 1.4f);
    }

    // ================================================================= 그림자 분신: 발자취를 따라오며 가까운 적에게 그림자 화살
    class ShadowClone
    {
        public GameObject go;
        public SpriteRenderer sr;
        public float fireAt;
    }

    readonly List<ShadowClone> shadows = new List<ShadowClone>();
    readonly List<Vector3> trail = new List<Vector3>();
    float trailAt, shadowSoundAt;
    SpriteRenderer playerSr;
    const float TrailStep = 0.04f;

    void TickShadows()
    {
        int lv = L(Shadow);
        if (lv <= 0) return;
        if (Time.time >= trailAt)
        {
            trailAt = Time.time + TrailStep;
            trail.Insert(0, p.transform.position);
            int keep = Mathf.CeilToInt(ShadowLag * 3f / TrailStep) + 2;
            if (trail.Count > keep) trail.RemoveRange(keep, trail.Count - keep);
        }
        if (playerSr == null) playerSr = p.GetComponent<SpriteRenderer>();
        while (shadows.Count < lv) shadows.Add(MakeShadow(shadows.Count));
        for (int i = 0; i < shadows.Count; i++)
        {
            ShadowClone c = shadows[i];
            if (c.go == null) { shadows[i] = c = MakeShadow(i); }
            int idx = Mathf.Min(trail.Count - 1, Mathf.RoundToInt(ShadowLag * (i + 1) / TrailStep));
            Vector3 at = idx >= 0 ? trail[idx] : p.transform.position;
            c.go.transform.position = at + new Vector3(0f, 0.05f * Mathf.Sin(Time.time * 4f + i), 0f);
            c.go.transform.localScale = p.transform.lossyScale;
            if (playerSr != null)
            {
                c.sr.sprite = playerSr.sprite;
                c.sr.flipX = playerSr.flipX;
                c.sr.sortingLayerID = playerSr.sortingLayerID;
                c.sr.sortingOrder = playerSr.sortingOrder - 1 - i;
            }
            if (Time.time < c.fireAt) continue;
            c.fireAt = Time.time + ShadowEvery;
            Transform t = Specials.NearestEnemy(at, ShadowRange);
            if (t == null) continue;
            Vector3 from = at + Vector3.up * 0.4f;
            Fx.Bolt(from, t.position, 0.45f, new Color(0.65f, 0.4f, 1f, 0.9f), 0.14f);
            Fx.Spawn("fx_spark", t.position, 1.1f, SoulPurple, 22f);
            Vector3 d = t.position - from;
            d.z = 0f;
            using (DamageSource.As(DamageSource.Special)) Specials.Damage(t.gameObject, p.damage * ShadowMul, d.normalized, 0.5f);
            if (Time.time >= shadowSoundAt) { shadowSoundAt = Time.time + 0.25f; Hostile.Play("zap", 0.22f, 1.6f); }
        }
    }

    ShadowClone MakeShadow(int i)
    {
        GameObject go = new GameObject("TranscendShadow" + i);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.color = ShadowTint;
        go.transform.position = p.transform.position;
        Fx.Spawn("fx_shadowdash", go.transform.position, 2.2f, SoulPurple, 18f);
        return new ShadowClone { go = go, sr = sr, fireAt = Time.time + 0.3f * (i + 1) };
    }

    // ================================================================= 중력장: 가만히 서 있으면 둘레 잡몹과 코인을 끌어당김
    Vector3 anchor;
    float stillSince, gravityTick, gravityRingAt;
    bool gravityOn;
    FxAnim gravityFx;

    void TickGravity()
    {
        int lv = L(Gravity);
        if (lv <= 0) return;
        Vector3 pos = p.transform.position;
        if ((pos - anchor).sqrMagnitude > 0.03f || p.IsSkillUsing)
        {
            anchor = pos;
            stillSince = Time.time;
            SetGravity(false);
            return;
        }
        if (Time.time - stillSince < GravityDelay) return;
        float r = GravityRadius[lv - 1];
        if (!gravityOn)
        {
            SetGravity(true);
            gravityFx = Fx.Play("fx_vortex", pos, r * 1.6f, new Color(0.72f, 0.5f, 1f, 0.28f), 10f, 0f, 1, true);
            if (gravityFx != null) { gravityFx.follow = p.transform; gravityFx.spin = -60f; }
            Hostile.Play("rl_magnet", 0.45f, 0.6f);
        }
        if (Time.time >= gravityRingAt)
        {
            gravityRingAt = Time.time + 0.9f;
            ShockRing.Spawn(pos, r, 0.8f, 0.8f, new Color(0.72f, 0.5f, 1f, 0.55f), 0.18f);
        }
        if (Time.time < gravityTick) return;
        gravityTick = Time.time + 0.1f;
        foreach (Collider2D col in Specials.Overlap(pos, r, hits))
        {
            if (col == null || !col.CompareTag("enermy")) continue;
            EnermyController e = col.GetComponent<EnermyController>();
            if (e == null || e.IsDead || e.swarm != null) continue;
            Vector3 d = pos - e.transform.position;
            d.z = 0f;
            float dist = d.magnitude;
            if (dist < 1.4f) continue;
            float step = Mathf.Min(GravityPull * 0.1f * (e.IsMidBoss ? 0.4f : 1f), dist - 1.4f);
            e.transform.position = Hostile.ClampArena(e.transform.position + d / dist * step);
            if (lv >= 3) e.Slow(0.5f, 0.25f);
        }
    }

    void SetGravity(bool on)
    {
        if (gravityOn == on) return;
        gravityOn = on;
        PlayerController pl = p != null ? p : Hostile.Player;
        if (pl != null) pl.coinMagnetRange += on ? GravityMagnet : -GravityMagnet;
        if (!on && gravityFx != null) { Destroy(gravityFx.gameObject); gravityFx = null; }
    }

    // ================================================================= 역전: 최후의 보루가 터지는 순간 대폭발 (LastStand.Fired)
    void OnLastStand(PlayerController pl, Vector3 c)
    {
        int lv = L(Reversal);
        if (lv <= 0 || pl == null) return;
        Blast(c, ReversalRadius, pl.damage * ReversalMul, 6f, 1f);
        pl.GrantInvincibility(ReversalInvincible);
        if (lv >= 3)
        {
            pl.PlayerHealth = Mathf.Min(pl.PlayerMaxHealth, pl.PlayerHealth + pl.PlayerMaxHealth * ReversalHeal * SoulWeight.HealMul);
            Fx.Spawn("fx_levelup", c + Vector3.up, 3.5f, new Color(1f, 0.6f, 0.55f), 16f);
        }
        Fx.Spawn("fx_explosion", c, ReversalRadius * 1.3f, Gold, 18f);
        Fx.Spawn("fx_soulburst", c, 7f, Color.white, 16f);
        ShockRing.Spawn(c, 1f, ReversalRadius * 1.4f, 0.6f, new Color(1f, 0.6f, 0.3f, 0.95f), 0.7f);
        for (int i = 0; i < 8; i++)
            Fx.Spawn("fx_spark", c + (Vector3)(Random.insideUnitCircle * ReversalRadius * 0.6f), 1.6f, Gold, 20f);
        Hostile.Play("bigboom", 0.7f, 0.85f);
        Hostile.Play("thunder", 0.4f, 1.1f);
        Hostile.Shake(0.45f);
        Float(c + Vector3.up * 0.8f, "역전!", Gold, 7f);
    }

    // ================================================================= 맵 지배: 부순 맵 물체가 터지고, 다른 자리에 다시 생김 (MapProps.OnBroken 이 부름)
    public static void PropBroken(PropDef def, Vector3 at)
    {
        if (Instance != null) Instance.OnPropBroken(def, at);
    }

    void OnPropBroken(PropDef def, Vector3 at)
    {
        int lv = L(Dominion);
        if (lv <= 0 || p == null) return;
        int i = Mathf.Min(lv, DominionRadius.Length) - 1;
        float r = DominionRadius[i];
        Blast(at, r, p.damage * DominionMul, 3f, 0f);
        Fx.Spawn("fx_explosion", at, r * 1.4f, Gold, 20f);
        ShockRing.Spawn(at, 0.4f, r, 0.4f, Gold, 0.35f);
        Hostile.Play("boom", 0.5f, 1.1f);
        StartCoroutine(Respawn(def, DominionRespawn[i]));
    }

    IEnumerator Respawn(PropDef def, float wait)
    {
        int stage = Chapters.CurrentStage;
        yield return new WaitForSeconds(wait);
        if (Chapters.CurrentStage != stage || MapProps.Instance == null) yield break;
        Vector3? at = MapProps.Instance.RespawnProp(def);
        if (at == null) yield break;
        Fx.Spawn("fx_soulburst", at.Value, 3f, Gold, 16f);
        ShockRing.Spawn(at.Value, 3f, 0.5f, 0.5f, Gold, 0.25f);
    }

    // ================================================================= 연쇄 붕괴: 쓰러진 적이 확률로 터져 둘레에 번짐 (터진 적이 또 터질 수 있음)
    readonly List<Vector3> cascadeQueue = new List<Vector3>();
    float cascadeWindow, cascadeSoundAt;
    int cascadeUsed;

    void TickCascade()
    {
        if (cascadeQueue.Count == 0) return;
        int lv = L(Cascade);
        if (lv <= 0) { cascadeQueue.Clear(); return; }
        if (Time.time >= cascadeWindow) { cascadeWindow = Time.time + 1f; cascadeUsed = 0; }
        int n = 0;
        while (cascadeQueue.Count > 0 && n < 3 && cascadeUsed < CascadePerSecond)
        {
            Vector3 at = cascadeQueue[0];
            cascadeQueue.RemoveAt(0);
            n++;
            cascadeUsed++;
            float r = CascadeRadius[lv - 1];
            Blast(at, r, p.damage * CascadeMul, 2f, 0f, false);
            Fx.Spawn("fx_soulburst", at, r * 1.3f, SoulPurple, 20f);
            Fx.Spawn("fx_spark", at, 1.4f, Color.white, 24f);
            if (Time.time >= cascadeSoundAt) { cascadeSoundAt = Time.time + 0.12f; Hostile.Play("crackle", 0.35f, 1.2f); }
        }
        if (cascadeUsed >= CascadePerSecond) cascadeQueue.Clear();     // 한꺼번에 몰리면 남은 것은 버림 (끝없이 번지지 않게)
    }

    // ================================================================= 영혼 보호막: 한동안 맞지 않으면 한 번 막는 막 (PlayerController.TryHit 이 부름)
    bool wardReady;
    float wardAt;
    LineRenderer wardRing;

    public static bool WardBlocks()
    {
        return Instance != null && Instance.ConsumeWard();
    }

    bool ConsumeWard()
    {
        int lv = L(Ward);
        if (lv <= 0 || !wardReady || p == null) return false;
        wardReady = false;
        wardAt = Time.time + WardEvery[lv - 1];
        Vector3 c = p.transform.position;
        Fx.Spawn("fx_shock", c, 4f, TimeBlue, 18f);
        Fx.Spawn("fx_sparkle", c, 2.4f, Color.white, 20f);
        Hostile.Play("shatter", 0.55f, 1.3f);
        Float(c, "막아 냄!", TimeBlue, 5f);
        if (lv >= 3)
        {
            Blast(c, WardBlastRadius, p.damage * 2f, 5f, 0.8f);
            ShockRing.Spawn(c, 0.5f, WardBlastRadius, 0.4f, TimeBlue, 0.4f);
        }
        return true;
    }

    void TickWard(bool hurt)
    {
        int lv = L(Ward);
        if (lv <= 0) return;
        if (hurt && !wardReady) wardAt = Mathf.Max(wardAt, Time.time + WardEvery[lv - 1]);       // 맞으면 다시 셈
        if (!wardReady && Time.time >= wardAt)
        {
            wardReady = true;
            Fx.Spawn("fx_sparkle", p.transform.position, 2f, TimeBlue, 18f);
            Hostile.Play("clank", 0.35f, 1.6f);
        }
        if (wardRing == null)
        {
            wardRing = Hostile.NewLine("TranscendWard", TimeBlue, 0.1f, 20);
            wardRing.loop = true;
        }
        wardRing.enabled = wardReady;
        if (!wardReady) return;
        float r = 1.05f + 0.06f * Mathf.Sin(Time.time * 5f);
        Hostile.SetArc(wardRing, p.transform.position + Vector3.up * 0.2f, r, 0f, 354f);
        wardRing.startColor = wardRing.endColor = new Color(TimeBlue.r, TimeBlue.g, TimeBlue.b, 0.55f + 0.25f * Mathf.Sin(Time.time * 5f));
    }

    // ================================================================= 영혼불 발자국: 지나간 자리에 영혼불이 남아 밟은 적을 태움
    class EmberPatch
    {
        public Vector3 at;
        public float until;
        public FxAnim fx;
    }

    readonly List<EmberPatch> embers = new List<EmberPatch>();
    Vector3 lastEmber;
    float emberTick;

    void TickEmbers()
    {
        int lv = L(Embers);
        if (lv <= 0) return;
        Vector3 pos = p.transform.position;
        if ((pos - lastEmber).sqrMagnitude >= EmberStep * EmberStep)
        {
            lastEmber = pos;
            if (embers.Count >= EmberMax) { EmberPatch old = embers[0]; if (old.fx != null) Destroy(old.fx.gameObject); embers.RemoveAt(0); }
            float life = EmberLife[lv - 1];
            // 1.3.7~ 전용 그림 fx_soulstep (푸른 영혼불 · tools/pixelart/make_soulstep.py) — 예전엔 산성 웅덩이를 주황으로 물들여 씀 · 판정 반지름은 그대로
            float size = EmberRadius[lv - 1] * 2.3f;
            FxAnim fx = Fx.Play("fx_soulstep", pos + Vector3.down * 0.3f + Vector3.up * (size * 0.125f), size, Color.white, 10f, 0f, 2, true, life);     // 그을린 자국 가운데가 그림 가운데보다 4/32 아래
            embers.Add(new EmberPatch { at = pos, until = Time.time + life, fx = fx });
            if (Random.value < 0.35f) Fx.Spawn("fx_spark", pos, 0.9f, SoulBlue, 20f);
        }
        if (Time.time < emberTick) return;
        emberTick = Time.time + 0.25f;
        float r = EmberRadius[lv - 1];
        for (int i = embers.Count - 1; i >= 0; i--)
        {
            EmberPatch e = embers[i];
            if (Time.time >= e.until) { embers.RemoveAt(i); continue; }
            using (DamageSource.As(DamageSource.Special))
                foreach (Collider2D col in Specials.Overlap(e.at, r, hits))
                {
                    if (col == null || (!col.CompareTag("enermy") && !col.CompareTag("boss"))) continue;
                    EnermyController en = col.GetComponent<EnermyController>();
                    if (en != null && en.IsDead) continue;
                    Burn.Apply(col.gameObject, p.damage * EmberDps, 1.2f);
                }
        }
    }
}
