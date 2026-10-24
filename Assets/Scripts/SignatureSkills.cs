using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 캐릭터 고유 레벨업 스킬 (LevelShop.Signature) 과 스킬 진화의 실제 효과
// 플레이어에 붙어서, 게임 곳곳의 훅(공격 · 명중 · 처치 · 피격 · 장전 · 돌진 · 플라스크 …)을 받아 효과를 냄
// 캐릭터별 효과는 SignatureSkills.<캐릭터>.cs
public partial class SignatureSkills : MonoBehaviour
{
    public static SignatureSkills Instance { get; private set; }

    PlayerController player;
    CharacterKit kit;
    LevelShop shop;
    CharacterId who;
    readonly Dictionary<string, int> lv = new Dictionary<string, int>();

    // 이 스크립트가 주는 피해 중 (명중 효과가 다시 명중 효과를 부르지 않게)
    public static bool Dealing;
    // 화상 · 독 · 출혈 같은 지속 피해 중 (맞힐 때 효과는 평타 · 스킬에만)
    public static bool DotTick;

    public static SignatureSkills Ensure()
    {
        if (Instance != null) return Instance;
        PlayerController p = Hostile.Player != null ? Hostile.Player : FindFirstObjectByType<PlayerController>();
        if (p == null) return null;
        Instance = p.GetComponent<SignatureSkills>();
        if (Instance == null) Instance = p.gameObject.AddComponent<SignatureSkills>();
        return Instance;
    }

    void Awake()
    {
        Instance = this;
        player = GetComponent<PlayerController>();
        who = CharacterData.Selected;
        EnermyController.Killed += OnKilled;
        PlayerController.UltUsed += OnUlt;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        EnermyController.Killed -= OnKilled;
        PlayerController.UltUsed -= OnUlt;
        Dealing = DotTick = false;
        Paths.End();            // 2026-10-19~ 갈래 효과가 바꿔 둔 값 되돌림 (Paths.<영웅>.End)
    }

    // 레벨업 · 진화가 바뀔 때마다 단계를 다시 읽음
    public void Refresh(LevelShop s)
    {
        shop = s;
        lv.Clear();
        evolvedKeys.Clear();
        evoPow.Clear();
        foreach (LevelShop.SkillEvo e in s.MyEvos)
            if (s.IsEvolved(e.key))
            {
                foreach (int p in e.parts)
                {
                    string k = LevelShop.SigKey(who, p);
                    if (k != null) evolvedKeys.Add(k);
                }
                // 1.1.2~ 진화 강화: 재료 고유 스킬 피해 배율 (LevelShop.EvoPowTable)
                float mul = LevelShop.EvoPowMul(e);
                foreach (int p in LevelShop.EvoPowParts(e))
                {
                    string k = LevelShop.SigKey(who, p);
                    if (k != null) evoPow[k] = Mathf.Max(mul, evoPow.TryGetValue(k, out float had) ? had : 1f);
                }
            }
        for (int id = LevelShop.SigFirstId; id < LevelShop.SigFirstId + LevelShop.SigCount; id++)
        {
            string key = LevelShop.SigKey(who, id);
            if (key != null) lv[key] = s.LevelOf(id);
        }
    }

    int L(string key) => lv.TryGetValue(key, out int v) ? v : 0;

    // 1.1.2~ 진화 강화: 이 고유 스킬이 진화 재료가 되어 받는 피해 배율 (진화 전 · 배율 없는 진화는 1)
    readonly Dictionary<string, float> evoPow = new Dictionary<string, float>();
    float P(string key) => evoPow.TryGetValue(key, out float m) ? m : 1f;
    // 진화한 스킬이 만드는 이펙트는 가장 화려한 단계(_t3) 그림으로 (using (EvoFx("g.mine")) { ... })
    Fx.TierScope EvoFx(string key) => Fx.Tier(Big(key) ? 3 : 0);

    // ---------------- 진화한 스킬의 이펙트: 가장 크고 화려하게
    // 진화의 재료가 된 고유 스킬 (그 스킬로 생기는 이펙트를 키우고 그 스킬의 색으로 장식을 덧붙임)
    readonly HashSet<string> evolvedKeys = new HashSet<string>();
    bool Big(string key) => evolvedKeys.Contains(key);
    // 이펙트 크기 배율 (진화했으면 1.7배)
    float Fs(string key) => Big(key) ? 1.7f : 1f;
    static readonly Color EvoGold = new Color(1f, 0.85f, 0.4f);

    // 스킬마다 제 색을 더 진하고 밝게 (전부 금색이면 어느 진화인지 구분이 안 됨 · 흰색 계열은 그대로)
    static Color Vivid(Color c, float a = 1f)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        if (s < 0.12f) return new Color(1f, 1f, 1f, a);
        Color o = Color.HSVToRGB(h, Mathf.Min(1f, s * 1.6f + 0.25f), 1f);     // 2.1.6: 더 진하게
        o.a = a;
        return o;
    }
    // 진화했으면 제 색을 진하게, 아니면 원래 색
    Color Tint(string key, Color c) => Big(key) ? Vivid(c, Mathf.Max(c.a, 0.8f)) : c;

    // 반대편 색 (대비색): 같은 밝기로 색상환 반대쪽 · 흰색 계열이면 금색
    static Color Complement(Color c, float a = 1f)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        if (s < 0.12f) return new Color(EvoGold.r, EvoGold.g, EvoGold.b, a);
        float ch = Mathf.Repeat(h + 0.5f, 1f), cs = Mathf.Min(1f, s + 0.2f);
        // 2단계 키트 색 (2026-10-25~): 보색이 「적 신호 색」(345°~25° · 265°~320°, 채도 ≥ 0.5)이면 가까운 허용 색상으로 밂
        //   청록 171° → 351° 빨강 · 하늘 203° → 23° 주홍 고리가 적 경고처럼 보였음 → 빨강 쪽은 금 40° · 보라 쪽은 비전 248° (적대 대역 경계 250° 안쪽)
        //   밝기는 늘 1 이라 채도만 봄. Legacy 면 예전 그대로
        if (!VisualRules.Legacy && cs >= 0.5f)
        {
            float deg = ch * 360f;
            if (deg >= 345f || deg <= 25f) ch = 40f / 360f;
            else if (deg >= 265f && deg <= 320f) ch = 248f / 360f;
        }
        Color o = Color.HSVToRGB(ch, cs, 1f);
        o.a = a;
        return o;
    }

    // 진화한 스킬이 터질 때 덧붙이는 장식 (진화가 아니면 아무것도 안 함 · 판정은 그대로, 보이기만 함)
    // 2.1.6: 누가 봐도 진화로 보이게 — 크게(1.4배) · 그 스킬 색 + 대비색 + 금색 세 겹 고리, 가운데 흰 섬광, 사방으로 튀는 별빛
    void Flair(string key, Vector3 p, float size, Color c)
    {
        if (key != null && !Big(key)) return;
        using var tier = Fx.Tier(3);       // 1.1.2~ 진화 장식은 늘 가장 화려한 단계 그림으로
        if (Time.time >= flairSoundAt) { flairSoundAt = Time.time + 0.15f; Hostile.Play("evoflair", 0.5f, Random.Range(0.9f, 1.2f)); }      // 1.0.2
        Color v = Vivid(c, 0.95f), comp = Complement(v, 0.9f);
        Color gold = new Color(EvoGold.r, EvoGold.g, EvoGold.b, 0.95f);
        Fx.Spawn("fx_shock", p, size * 2.6f, v, 20f);
        Fx.Spawn("fx_shock", p, size * 1.6f, comp, 24f);
        Fx.Spawn("fx_soulburst", p, size * 1.2f, new Color(v.r, v.g, v.b, 0.9f), 20f);
        Fx.Spawn("fx_sparkle", p, size * 1.4f, Color.Lerp(Color.white, v, 0.3f), 22f);
        for (int i = 0; i < 3; i++)
            Fx.Spawn("fx_sparkle", p + (Vector3)(Random.insideUnitCircle * size * 0.9f), size * 0.55f, i == 1 ? comp : gold, 24f);
        ShockRing.Spawn(p, 0.2f, size * 1.75f, 0.45f, v, 0.32f);
        ShockRing.Spawn(p, 0.15f, size * 1.25f, 0.35f, comp, 0.24f);
        ShockRing.Spawn(p, 0.05f, size * 0.85f, 0.25f, gold, 0.16f);
        if (Hostile.Glow != null) FadeSprite.Spawn("EvoFlash", Hostile.Glow, p, size * 0.9f / 8f, new Color(1f, 1f, 1f, 0.9f), "Effect", 30, 0.14f);
    }
    float flairSoundAt;
    bool E(string key) => shop != null && shop.IsEvolved(key);
    // 단계별 값: 1단계 a, 단계마다 + step (0단계면 0)
    static float V(int level, float a, float step) => level <= 0 ? 0f : a + step * (level - 1);

    CharacterKit Kit => kit != null ? kit : (kit = CharacterKit.Instance);
    // 특수 능력 (들고 있는 특수 무기의 탄창 · 장전, 2.1.8~)
    static SpecialAbilities Sp => SpecialAbilities.SharedInstance;
    // 기준 공격력 (캐릭터 평타와 같은 값)
    // 고유 스킬은 공격 속도와 상관없이 터지므로 캐릭터 공격력 배율(느린 평타 보상) 대신 공통 기준을 씀
    //   지금 공격력 ÷ 캐릭터 배율 × 고유 스킬 배율 (레벨업 · 영혼 트리 강화는 그대로 반영)
    //   고유 스킬 배율은 1.3.7~ CharacterDef.sig (영웅마다 · 30분 밸런스)
    float Atk => player != null ? player.damage * player.damageMultiplier / Mathf.Max(0.1f, CharacterData.Current.damage) * SigPower : 1f;
    float SigPower => CharacterData.Def(who).sig;     // 1.3.7~ 영웅 데이터로 옮김 (예전: 검사 0.98 · 도적 0.85 · 나머지 1)
    public float BaseAtk => Atk;                       // 2026-10-19~ 갈래 효과도 같은 기준 (Paths.Atk)
    bool Alive => player != null && !player.IsDying;

    // ================================================================= 훅 (게임 코드가 부름 · 없으면 아무것도 안 함)
    public static float Outgoing(GameObject target, float dmg) => Instance != null && target != null ? Instance.OutgoingMul(target, dmg) : dmg;
    public static void Hit(GameObject target, float dmg, bool killed) { if (Instance != null && target != null) Instance.OnHit(target, dmg, killed); }
    public static float Taken(float dmg) => Instance != null ? Instance.TakenMul(dmg) : dmg;
    public static void Hurt(float taken) { if (Instance != null) Instance.OnHurt(taken); }
    public static void Attacked(Vector3 start, Vector2 dir) { if (Instance != null) Instance.OnAttack(start, dir); }
    public static float MoveMul => Instance != null ? Instance.MoveMultiplier() : 1f;
    public static float AttackRateMul => Instance != null ? Instance.RateMultiplier() : 1f;
    public static void CoinPicked(int n) { if (Instance != null) Instance.OnCoins(n); }

    float OutgoingMul(GameObject target, float dmg)
    {
        float mul = 1f;
        switch (who)
        {
            case CharacterId.Gunner: mul *= GunnerOutgoing(target); break;
            case CharacterId.Swordsman: mul *= SwordOutgoing(target); break;
            case CharacterId.Rogue: mul *= RogueOutgoing(target); break;
            case CharacterId.Archer: mul *= ArcherOutgoing(target); break;
            case CharacterId.Alchemist: mul *= AlchemistOutgoing(target); break;
            case CharacterId.Cartographer: mul *= MapOutgoing(target); break;
            case CharacterId.Postman: mul *= PostOutgoing(target); break;
            case CharacterId.Ghost: mul *= GhostOutgoing(target); break;
        }
        mul *= Paths.Outgoing(target, dmg);         // 2026-10-19~ 갈래 효과 (모든 영웅 · Paths.<영웅>.cs)
        return dmg * mul;
    }

    void OnHit(GameObject target, float dmg, bool killed)
    {
        bool proc = !Dealing && !DotTick;
        switch (who)
        {
            case CharacterId.Gunner: GunnerHit(target, dmg, killed, proc); break;
            case CharacterId.Swordsman: SwordHit(target, dmg, killed, proc); break;
            case CharacterId.Rogue: RogueHit(target, dmg, killed, proc); break;
            case CharacterId.Archer: ArcherHit(target, dmg, killed, proc); break;
            case CharacterId.Alchemist: AlchemistHit(target, dmg, killed, proc); break;
            case CharacterId.Clockmaker: ClockHit(target, dmg); break;
        }
        Paths.Hit(target, dmg, killed, proc);
    }

    float TakenMul(float dmg)
    {
        switch (who)
        {
            case CharacterId.Swordsman: dmg = SwordTaken(dmg); break;
            case CharacterId.Rogue: dmg = RogueTaken(dmg); break;
            case CharacterId.Cartographer: dmg = MapTaken(dmg); break;
            case CharacterId.Clockmaker: dmg = ClockTaken(dmg); break;
            case CharacterId.Postman: dmg = PostTaken(dmg); break;
            case CharacterId.Ghost: dmg = GhostTaken(dmg); break;
        }
        return Paths.Taken(dmg);
    }

    void OnHurt(float taken)
    {
        if (who == CharacterId.Gunner) GunnerHurt(taken);
        else if (who == CharacterId.Swordsman) SwordHurt(taken);
        else if (who == CharacterId.Postman) PostHurt(taken);       // 1.0.6~ 맞으면 우표 절반
        Paths.Hurt(taken);
    }

    void OnAttack(Vector3 start, Vector2 dir)
    {
        if (who == CharacterId.Gunner) GunnerAttack();
        else if (who == CharacterId.Rogue) RogueAttack(start, dir);
        Paths.Attacked(start, dir);
    }

    void OnKilled(Vector3 pos)
    {
        if (!Alive) return;
        if (who == CharacterId.Gunner) GunnerKilled(pos);
        else if (who == CharacterId.Swordsman) SwordKilled(pos);
        else if (who == CharacterId.Cartographer) MapKilled(pos);
        else if (who == CharacterId.Clockmaker) ClockKilled(pos);
        Paths.Killed(pos);
    }

    void OnUlt()
    {
        if (!Alive) return;
        if (who == CharacterId.Gunner) GunnerUlt();
        Paths.Ult();
    }

    float MoveMultiplier()
    {
        float m = 1f;
        if (who == CharacterId.Gunner) m *= GunnerMove();
        else if (who == CharacterId.Swordsman) m *= SwordMove();
        else if (who == CharacterId.Archer) m *= ArcherMove();
        else if (who == CharacterId.Cartographer) m *= MapMove();
        else if (who == CharacterId.Postman) m *= PostMove();
        else if (who == CharacterId.Ghost) m *= GhostMove();
        return m * Paths.MoveMul();
    }

    float RateMultiplier() => (who == CharacterId.Swordsman ? SwordRate() : who == CharacterId.Clockmaker ? ClockRate() : 1f) * Paths.RateMul();

    // 2.1.3: 고유 스킬이 그리는 이펙트 · 코루틴도 출처를 이어 감 (Fx 가 스킬 이펙트를 눈에 띄게)
    public new Coroutine StartCoroutine(System.Collections.IEnumerator routine) => base.StartCoroutine(DamageSource.Keep(routine));

    void Update()
    {
        DamageSource.Current = DamageSource.Signature;
        try { SignatureTick(); }
        finally { DamageSource.Current = null; }
    }

    void SignatureTick()
    {
        if (!Alive || Time.timeScale == 0f) return;
        try {   // 영웅 Tick 예외가 같은 프레임 갈래 Tick 까지 건너뛰지 않게 (예외는 그대로 위로)
        switch (who)
        {
            case CharacterId.Gunner: GunnerTick(); break;
            case CharacterId.Swordsman: SwordTick(); break;
            case CharacterId.Rogue: RogueTick(); break;
            case CharacterId.Archer: ArcherTick(); break;
            case CharacterId.Alchemist: AlchemistTick(); break;
            case CharacterId.Cartographer: MapTick(); break;
            case CharacterId.Clockmaker: ClockTick(); break;
            case CharacterId.Ghost: GhostTick(); break;
        }
        CommonTick();
        }
        finally { Paths.Tick(); }   // 2026-10-19~ 갈래 효과 (멈춘 동안 · 쓰러지는 중은 위에서 걸러짐)
    }

    // ================================================================= 쌓이는 스킬의 지금 상태 (HUD 칸 오른쪽 위 숫자)
    // 비어 있으면 null (표시 안 함)
    public static string StackText(string key) => Instance != null && key != null ? Instance.Stack(key) : null;

    string Stack(string key)
    {
        if (L(key) <= 0) return null;
        switch (key)
        {
            case "g.heat": return heat > 0 && Time.time - lastShot <= 1.5f ? "+" + Mathf.RoundToInt(V(L(key), 0.2f, 0.1f) * heat / HeatMax * 100f) + "%" : null;
            case "g.quick": return quickShots > 0 ? "x" + quickShots : null;
            case "s.bloodguard": return shield > 0.5f ? Mathf.CeilToInt(shield).ToString() : null;
            case "s.dance": return danceStacks > 0 && Time.time < danceUntil ? "x" + danceStacks : null;
            case "s.soul": return souls > 0 ? "x" + souls : null;
            case "s.whirl": return Time.time < rateUntil ? (rateUntil - Time.time).ToString("0.0") + "s" : null;
            case "r.practice": return practiceStacks > 0 && Time.time - practiceAt < 2f ? "x" + practiceStacks : null;
            case "r.ambush": return Time.time < ambushUntil ? (ambushUntil - Time.time).ToString("0.0") + "s" : null;
            case "a.breath": return breath > 0 ? "x" + breath : null;
            case "l.amplify": return amplify > 0 ? "+" + Mathf.RoundToInt(amplify * V(L(key), 0.03f, 0.01f) * 100f) + "%" : null;
            case "l.concentrate": return (throws % 4) + "/3";
        }
        if (who == CharacterId.Cartographer) return MapStack(key);
        if (who == CharacterId.Clockmaker) return ClockStack(key);
        if (who == CharacterId.Postman) return PostStack(key);
        if (who == CharacterId.Ghost) return GhostStack(key);
        return null;
    }

    // ================================================================= 진화하는 순간 (1.1.2~)
    // 진화 화면(SkillEvolutionUI)을 닫은 뒤 게임 안에서: 짧은 슬로모션 · 몸 둘레 큰 진화 장식 · 화면 번쩍임(설정 「번쩍임 효과」) · 「진화!」 글자
    public static void EvolvedMoment(LevelShop.SkillEvo e)
    {
        if (Instance == null || e == null || !Instance.Alive) return;
        Instance.StartCoroutine(Instance.EvolvedRoutine(e));
    }

    IEnumerator EvolvedRoutine(LevelShop.SkillEvo e)
    {
        // 다른 창이 시간을 멈춰 두었으면 (레벨업이 줄줄이 · 일시정지) 풀릴 때까지 기다림 — 기다리는 동안 실제 시간
        float waitUntil = Time.unscaledTime + 8f;
        while (Time.timeScale == 0f && Time.unscaledTime < waitUntil) yield return null;
        if (!Alive || Time.timeScale == 0f) yield break;
        Vector3 p = transform.position;
        Color c = new Color(0.79f, 0.63f, 1f);
        using (DamageSource.As(DamageSource.Signature))
        {
            Flair(null, p, 9f, c);
            ShockRing.Spawn(p, 0.5f, 11f, 0.6f, new Color(1f, 0.85f, 0.4f, 0.9f), 0.4f);
            using (Fx.Tier(3)) Fx.Spawn("fx_soulburst", p, 10f, new Color(0.85f, 0.7f, 1f, 0.95f), 16f);
        }
        if (GameSettings.Flashes && Hostile.Glow != null) FadeSprite.Spawn("EvoFlash", Hostile.Glow, p, 4f, new Color(1f, 0.95f, 1f, 0.8f), "Effect", 60, 0.3f);
        SpecialAbilities.SharedFx?.FloatText(p + Vector3.up * 2f, Loc.T("진화!") + " " + Loc.T(e.name), new Color(1f, 0.85f, 0.45f), 6f, 0f);
        Hostile.Play("evolve", 0.7f, 1.3f);
        Hostile.Shake(0.25f);
        Juice.Evolve();         // 슬로모션 · 번쩍임 (다른 창이 시간을 바꾸면 JuiceRunner 가 손을 뗌)
    }

    // ================================================================= 공통 진화 (공용 카드끼리)
    // 황금 시대: 코인을 주울 때 경험치 · 40개마다 황금 파동 / 불멸의 육체: 체력이 가득하면 3초마다 심장 파동
    int goldCount;
    float heartAt;

    void OnCoins(int n)
    {
        if (n <= 0 || !E("ce.gold") || player == null) return;
        LevelShop ls = Cache<LevelShop>.Get;
        if (ls != null) ls.GainExp(player, 2f * n);
        goldCount += n;
        if (goldCount < 40) return;
        goldCount = 0;
        Vector3 p = transform.position;
        using var tier = Fx.Tier(3);
        Circle(p, 7f, Atk * 3f, 2f);
        Fx.Spawn("fx_shock", p, 20f, new Color(1f, 0.85f, 0.3f, 0.9f), 18f);
        Fx.Spawn("fx_soulburst", p, 9f, new Color(1f, 0.9f, 0.4f), 18f);
        Flair(null, p, 8f, EvoGold);
        ShockRing.Spawn(p, 0.5f, 8f, 0.45f, new Color(1f, 0.85f, 0.35f, 0.9f), 0.3f);
        Hostile.Play("chime", 0.8f, 1.3f);
    }

    void CommonTick()
    {
        if (!E("ce.body") || player == null) return;
        if (player.PlayerHealth < player.PlayerMaxHealth - 0.01f) { heartAt = Time.time + 3f; return; }
        if (Time.time < heartAt) return;
        heartAt = Time.time + 3f;
        Vector3 p = transform.position;
        using var tier = Fx.Tier(3);
        Circle(p, 5f, player.PlayerMaxHealth * 0.25f, 3f);
        ShockRing.Spawn(p, 0.4f, 6f, 0.4f, VisualRules.Kit(new Color(1f, 0.35f, 0.4f, 0.9f), new Color(0.35f, 1f, 0.57f, 0.9f)), 0.3f);
        Fx.Spawn("fx_shock", p, 16f, VisualRules.Kit(new Color(1f, 0.4f, 0.45f, 0.8f), new Color(0.4f, 1f, 0.6f, 0.8f)), 20f);
        Flair(null, p, 6f, VisualRules.Kit(new Color(1f, 0.4f, 0.45f), new Color(0.4f, 1f, 0.6f)));
        Hostile.Play("thump", 0.6f, 0.8f);
    }

    // ================================================================= 도우미
    // 이 스크립트의 피해 (명중 효과가 연쇄되지 않게)
    public void Deal(GameObject g, float dmg, Vector3 dir, float knock)
    {
        if (g == null) return;
        bool was = Dealing;
        Dealing = true;
        using var source = DamageSource.As(DamageSource.Signature);
        try { Specials.Damage(g, dmg, dir, knock); }
        finally { Dealing = was; }
    }

    static readonly List<Collider2D> hits = new List<Collider2D>(64);

    // 반지름 안의 적 · 보스 모두에게
    public int Circle(Vector3 pos, float r, float dmg, float knock, System.Action<Collider2D> each = null)
    {
        int n = 0;
        // 빌린 목록으로 (Specials.Overlap 과 같은 조건 · 피해 · each 콜백 안에서 또 검색해도 안전 · 호출마다 복사본을 만들지 않음)
        using var list = PhysHits.Circle(pos, r, true);
        foreach (Collider2D c in list)
        {
            if (c == null || !IsFoe(c)) continue;
            if (dmg > 0f) Deal(c.gameObject, dmg, (c.transform.position - pos).normalized, knock);      // 0이면 기절 · 표식만
            if (c != null) each?.Invoke(c);
            n++;
        }
        return n;
    }

    static bool IsFoe(Collider2D c) => c.CompareTag("enermy") || c.CompareTag("boss");
    // 황금 고블린(이벤트)은 이름표 때문에 MidBossMark 가 붙지만 보스 대상 효과는 받지 않음 (20초 안에 잡는 난이도 유지)
    static bool IsBoss(GameObject g) => g.CompareTag("boss") || (g.GetComponent<MidBossMark>() != null && g.GetComponent<GoldenGoblin>() == null);

    // 가까운 적 (살아 있는) 여럿
    public static List<Transform> Nearest(Vector3 pos, float r, int count, GameObject except = null)
    {
        // 정렬용 목록 · 비교는 공용 (이 안에서는 다른 검색을 부르지 않음) — 결과 목록은 호출자가 들고 쓰므로 새로
        List<(float d, Transform t)> all = nearestSort;
        all.Clear();
        foreach (Collider2D c in Specials.Overlap(pos, r, hits))
        {
            if (c == null || !IsFoe(c) || c.gameObject == except) continue;
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && e.IsDead) continue;
            all.Add(((c.transform.position - pos).sqrMagnitude, c.transform));
        }
        all.Sort(ByDistance);
        List<Transform> res = new List<Transform>(Mathf.Clamp(count, 0, all.Count));
        for (int i = 0; i < all.Count && res.Count < count; i++) res.Add(all[i].t);
        all.Clear();
        return res;
    }

    static readonly List<(float d, Transform t)> nearestSort = new List<(float d, Transform t)>(64);
    static readonly System.Comparison<(float d, Transform t)> ByDistance = (a, b) => a.d.CompareTo(b.d);

    // 도트 그림을 입힌 투사체 (Bullet 판정 그대로, 스킬 총알이라 게이지를 채우지 않음)
    public Bullet Shot(Vector3 start, Vector2 dir, float dmg, int pene, float speed, float range, string fx, float size, Color tint, bool spin = false)
    {
        if (player == null) return null;
        using var source = DamageSource.As(DamageSource.Signature);     // 총알이 이 출처를 기억함
        Bullet b = player.CreateBullet(start, dir, dmg, pene, 0f, true);
        if (b == null) return null;
        b.speed = speed;
        b.lifetime = range > 0f ? range / speed : 3f;
        SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
        Sprite[] f = Fx.Frames(fx);
        if (sr != null && f.Length > 0)
        {
            sr.sprite = f[0];
            sr.color = tint;
            b.transform.localScale = Vector3.one * (size / f[0].bounds.size.y);
            FrameLoop loop = b.gameObject.AddComponent<FrameLoop>();
            loop.frames = f;
            loop.spin = spin ? -900f : 0f;
            if (fx == "fx_swordwave") SlashTrail.Attach(b);      // 검기: 총알 빛 대신 초승달 잔상 (1.0.1)
        }
        return b;
    }

    // 지속 장판 (원 또는 선): tick 마다 onTick, 끝나면 onEnd. 그림은 반복 이펙트
    // ground: 그림을 바닥 층에 (캐릭터 · 적을 가리지 않게)
    public SigZone Zone(Vector3 pos, float r, float life, float tick, System.Action<SigZone> onTick, string fx = null, Color? tint = null, float fxSize = 0f, bool ground = false)
    {
        GameObject go = new GameObject("SigZone");
        go.transform.position = pos;
        SigZone z = go.AddComponent<SigZone>();
        z.radius = r;
        z.life = life;
        z.tick = tick;
        z.onTick = onTick;
        if (fx != null)
        {
            FxAnim a = Fx.Play(fx, pos, fxSize > 0f ? fxSize : r * 2.2f, tint ?? Color.white, 8f, 0f, ground ? 8 : 3, true, life, ground ? "Background" : "Effect");
            if (a != null) { a.transform.SetParent(go.transform, true); z.visual = a; }
        }
        return z;
    }

    // 장판 안의 적 (원이면 반지름, 선이면 선분과의 거리)
    public List<Collider2D> Inside(SigZone z)
    {
        List<Collider2D> res = new List<Collider2D>();
        Vector3 c = z.segment ? (z.a + z.b) * 0.5f : z.transform.position;
        float r = z.segment ? Vector3.Distance(z.a, z.b) * 0.5f + z.radius : z.radius;
        foreach (Collider2D col in Specials.Overlap(c, r, hits))
        {
            if (col == null || !IsFoe(col)) continue;
            if (z.segment && Hostile.DistanceToSegment(col.transform.position, z.a, z.b) > z.radius) continue;
            res.Add(col);
        }
        return res;
    }

    // 하늘에서 떨어지는 것 (그림이 위에서 내려와 닿는 순간 범위 피해)
    IEnumerator Drop(Vector3 at, string fx, float size, Color tint, float r, float dmg, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        FxAnim a = Fx.Play(fx, at + Vector3.up * 7f, size, tint, 16f, 0f, 20, true, 0.4f);
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            if (a != null) a.transform.position = Vector3.Lerp(at + Vector3.up * 7f, at, t / 0.25f);
            yield return null;
        }
        if (a != null) Destroy(a.gameObject);
        Circle(at, r, dmg, 0.6f);
        Fx.Spawn("fx_spark", at, r * 1.6f, tint, 22f);
    }

    IEnumerator FlairLater(Vector3 p, float delay, float size, Color c)
    {
        yield return new WaitForSeconds(delay);
        Flair(null, p, size, c);
    }

    void Heal(float amount)
    {
        if (player == null || amount <= 0f) return;
        float before = player.PlayerHealth;
        player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + amount);
        if (player.PlayerHealth > before + 0.5f && SpecialAbilities.SharedFx != null)
            SpecialAbilities.SharedFx.FloatText(transform.position, "+" + Mathf.RoundToInt(player.PlayerHealth - before), new Color(0.5f, 1f, 0.5f), 3.5f, 0.3f);
    }

    // 적 기절 · 감속 (보스는 감속만 짧게)
    static void Stun(GameObject g, float seconds, float bossMul = 0.5f)
    {
        EnermyController e = g != null ? g.GetComponent<EnermyController>() : null;
        if (e != null) e.Slow(0f, seconds);
    }

    // 표식 (몇 초 동안 받는 피해 +): 위협 · 표식 전염 · 덤불 · 빙결 등에서 같이 씀
    readonly Dictionary<GameObject, (float until, float mul)> marks = new Dictionary<GameObject, (float, float)>();

    void Mark(GameObject g, float seconds, float mul)
    {
        if (g == null) return;
        if (marks.TryGetValue(g, out var m) && m.until > Time.time) mul = Mathf.Max(mul, m.mul);
        marks[g] = (Time.time + seconds, mul);
    }

    float MarkMul(GameObject g) => g != null && marks.TryGetValue(g, out var m) && m.until > Time.time ? m.mul : 1f;
    bool Marked(GameObject g) => g != null && marks.TryGetValue(g, out var m) && m.until > Time.time;

    float pruneAt;
    void PruneMarks()
    {
        if (Time.time < pruneAt) return;
        pruneAt = Time.time + 2f;
        List<GameObject> dead = new List<GameObject>();
        foreach (var kv in marks) if (kv.Key == null || kv.Value.until < Time.time) dead.Add(kv.Key);
        foreach (GameObject g in dead) marks.Remove(g);
    }

    void LateUpdate() => PruneMarks();
}

// 스킬이 남기는 장판 (원 또는 선분)
public class SigZone : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    public float radius, life, tick;
    public bool segment;
    public Vector3 a, b;
    public System.Action<SigZone> onTick, onEnd;
    public FxAnim visual;
    public float age;
    float next;

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        age += Time.deltaTime;
        if (age >= life)
        {
            if (onEnd != null) Paths.Safe(() => onEnd(this));      // 예외여도 장판은 사라짐 (매 프레임 onEnd 반복 방지)
            Destroy(gameObject);
            return;
        }
        if (tick > 0f && age >= next)
        {
            next = age + tick;
            if (onTick != null) Paths.Safe(() => onTick(this));    // 갈래 장판 람다 예외를 잡음 (Paths.Zone)
        }
    }

    public bool Contains(Vector3 p) => segment ? Hostile.DistanceToSegment(p, a, b) <= radius : (p - transform.position).sqrMagnitude <= radius * radius;
}
