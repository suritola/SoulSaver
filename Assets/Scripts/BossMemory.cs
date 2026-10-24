using System.Collections.Generic;
using UnityEngine;

// 기억하는 보스 (차별점 기획 2절): 보스 체력이 정해진 값 아래로 처음 내려갈 때, 이번 보스전에 이 보스에게 가장 많은 피해를 준 공격을 기억해 잠시 막음
//   평타 종류(평타 · 지금 든 무기) → 기억의 방패: 정면 120° 로 들어온 그 공격 피해 ×0.2, 몇 초마다 경고 뒤 플레이어 쪽으로 다시 돎
//   그 밖(필살기 · 특수 능력 · 고유 스킬 …) → 기억의 결: 그 출처 피해 ×0.35 (방향 무관)
//   다른 공격으로 최대 체력 5% 를 깎으면 기억이 깨지고 1.5초 휘청 (받는 피해 +25%), 끝까지 버티면 「기억이 흐려졌다.」
// 지난 판 기록은 쓰지 않음(일일 도전 · 순위표 공정) · 저장하지 않음(이어하기는 지금 체력보다 높은 값을 이미 쓴 것으로) · RunSave.Version 그대로
// bosss.Start 가 BossUltimate 다음에 붙임 (튜토리얼 제외) · 집계 사전은 보스마다 하나라 보스가 지워지면 같이 사라짐
public class BossMemory : MonoBehaviour
{
    // ================================================================= 표 (2-1 · 2-3 · 2-4)
    static readonly float[] SlimeAt = { 0.6f }, LichAt = { 0.75f, 0.3f }, HellAt = { 0.5f, 0.2f }, MirrorAt = { 0.75f, 0.3f },
        DevAt = { 0.66f, 0.33f }, MomAt = { 0.75f, 0.5f, 0.25f }, NoneAt = new float[0];

    // 보스바 기억 눈금 자리 (bossbar · 개발자 9 · 엄마 10 포함 · 정적 배열이라 프레임마다 할당 없음)
    public static float[] Thresholds(int kind) => kind switch
    {
        0 => LichAt,
        1 => HellAt,
        2 => SlimeAt,
        3 => MirrorAt,
        bossbar.DeveloperKind => DevAt,
        bossbar.DeveloperMomKind => MomAt,
        _ => NoneAt,
    };

    static Color ColorOf(int kind) => kind switch
    {
        0 => new Color(0.88f, 0.92f, 0.96f),     // 창백한 뼈
        1 => new Color(1f, 0.52f, 0.16f),        // 불의 비늘
        2 => new Color(0.55f, 1f, 0.35f),        // 점액 초록
        3 => new Color(0.82f, 0.9f, 1f),         // 거울 은빛
        bossbar.DeveloperKind => new Color(0.3f, 1f, 0.45f),       // 초록 코드 줄
        bossbar.DeveloperMomKind => new Color(1f, 0.5f, 0.8f),     // 분홍
        _ => Color.white,
    };

    static string LineKey(int kind) => kind switch
    {
        0 => "bossmem.line.0",
        1 => "bossmem.line.1",
        2 => "bossmem.line.2",
        3 => "bossmem.line.3",
        bossbar.DeveloperMomKind => "bossmem.line.mom",
        _ => "bossmem.line.dev",
    };

    static readonly Color Blocked = new Color(0.62f, 0.64f, 0.68f);
    static readonly Color ShardColor = new Color(1f, 0.55f, 0.5f, 0.95f);      // 되쏘는 거울 조각 (경고 색)
    const string ShardLabel = "기억의 거울 조각";

    // ================================================================= 진행 중인 기억은 한 번에 하나 (무한 모드 동시 보스)
    static BossMemory running;
    static float nextAllowedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        running = null;
        nextAllowedAt = 0f;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        running = null;
        nextAllowedAt = 0f;
    }

    public static BossMemory Attach(bosss b)
    {
        if (b == null || TutorialRun.Active) return null;
        BossMemory m = b.GetComponent<BossMemory>();
        if (m == null) m = b.gameObject.AddComponent<BossMemory>();
        m.Init(b);
        return m;
    }

    // ================================================================= 상태
    bosss boss;
    int kind;
    float[] at = NoneAt;
    int nextIdx;            // 아직 넘지 않은 첫 체력 값
    int pending;            // 넘었지만 미뤄진 발동 수

    // 출처 → 이 보스에게 준 피해 (보스전 전체 · 지난 기억이 끝난 뒤)
    readonly Dictionary<string, float> total = new Dictionary<string, float>();
    readonly Dictionary<string, float> since = new Dictionary<string, float>();
    readonly List<string> keys = new List<string>();
    float sinceSum;

    bool active, shield, lastBlocked;
    string remembered;
    float timeLeft, breakAccum, breakNeed;
    float blockMul;
    Vector2 shieldDir = Vector2.down;
    float retargetLeft, retargetEvery, warnTime;
    float staggerUntil, staggerFxAt, sparkAt, reflectAt;
    string[] barTexts;
    Loc.Lang barLang;
    string barNoun;
    Color color;
    float bodyR = 2f;
    LineRenderer arc;
    GameObject veil;
    SpriteRenderer veilSr;

    public bool Active => active;

    // 이번 피해가 탄(Bullet.Hit)인지 — 거울의 군주는 정면에 막힌 평타 「탄」만 되쏨 (베기 · 손길 · 잉크 획 · 폭발은 아님)
    //   Bullet 이 boss.TakeDamage 앞뒤로 켜고 끔 · Incoming 이 읽자마자 끔 (같은 TakeDamage 안에서 이어 들어온 다른 피해가 물려받지 않게)
    public static bool FromBullet;

    void Init(bosss b)
    {
        boss = b;
        kind = b.developer ? (b.developerMom ? bossbar.DeveloperMomKind : bossbar.DeveloperKind) : b.bossKind;
        at = Thresholds(kind);
        color = ColorOf(kind);
        EnemyHitbox hb = b.GetComponent<EnemyHitbox>();
        bodyR = Mathf.Max(1f, hb != null ? hb.BodyWorldRadius : 2f);
        // 이어하기: 지금 체력보다 높은 값은 이미 쓴 것으로 (BossUltimate 의 「40% 아래면 결계를 쓴 것으로」와 같은 방식 · 저장 칸 없음)
        float frac = b.setEnemyHP > 0 ? b.EnemyHealth / b.setEnemyHP : 1f;
        nextIdx = 0;
        while (nextIdx < at.Length && frac <= at[nextIdx]) nextIdx++;
        pending = 0;
    }

    // ================================================================= bosss.TakeDamage 가 부름
    // 1) 피해 배율: 기억한 출처면 방패(정면) · 결로 줄임
    public float Incoming(string source, Vector3 dir)
    {
        lastBlocked = false;
        bool bullet = FromBullet;
        FromBullet = false;
        if (!active || boss == null) return 1f;
        string key = source ?? DamageSource.Other;
        if (key != remembered) return 1f;
        Vector3 bp = boss.transform.position;
        Vector2 from = new Vector2(-dir.x, -dir.y);        // 「때린 쪽」 (탄은 날아온 방향 · 근접은 플레이어 쪽)
        if (from.sqrMagnitude < 0.0001f)
        {
            PlayerController p = Hostile.Player;
            from = p != null ? (Vector2)(p.transform.position - bp) : shieldDir;
        }
        if (shield && Vector2.Angle(from, shieldDir) > 60f) return 1f;      // 옆 · 뒤로 들어옴 (깨기에 셈)
        lastBlocked = true;
        if (Time.time >= sparkAt)
        {
            sparkAt = Time.time + 0.08f;
            Vector2 n = shield ? shieldDir : (from.sqrMagnitude > 0.0001f ? from.normalized : Vector2.up);
            Vector3 spot = bp + (Vector3)(n * bodyR * (shield ? 1.35f : 0.9f));
            Fx.Spawn("fx_spark", spot, 1.6f, Blocked, 26f);
            SpecialFeedback fx = SpecialAbilities.SharedFx;
            if (fx != null) fx.FloatText(spot, Loc.T("bossmem.blocked"), Blocked, 3.5f, 0.25f);
        }
        if (shield && kind == 3 && bullet) Reflect(bp);
        return blockMul;
    }

    // 2) 실제로 잃은 체력 (넘친 피해는 뺀 값 · RunStats.Dealt 와 같은 열쇠)
    public void Tally(string source, float amount)
    {
        if (amount <= 0f) return;
        string key = source ?? DamageSource.Other;
        if (!total.TryGetValue(key, out float v)) keys.Add(key);
        total[key] = v + amount;
        since.TryGetValue(key, out float s);
        since[key] = s + amount;
        sinceSum += amount;
    }

    // 3) 체력이 줄어든 뒤: 깨기 누적 · 체력 값 넘음
    public void OnHealth(string source, float before, float after)
    {
        bool blocked = lastBlocked;
        lastBlocked = false;
        if (boss == null) return;
        float max = Mathf.Max(1f, boss.setEnemyHP);
        if (active && !blocked && before > after)
        {
            breakAccum += before - Mathf.Max(0f, after);
            if (breakAccum >= breakNeed * max && after > 0f) { Break(); return; }
        }
        if (after <= 0f) return;
        float frac = after / max;
        while (nextIdx < at.Length && frac <= at[nextIdx]) { nextIdx++; pending++; }
    }

    // ================================================================= 매 프레임
    void Update()
    {
        if (boss == null || boss.IsDead)
        {
            if (active) Stop(false);
            return;
        }
        if (boss.memoryTakenMul != 1f && Time.time >= staggerUntil) boss.memoryTakenMul = 1f;
        else if (boss.memoryTakenMul != 1f && Time.time >= staggerFxAt)
        {
            staggerFxAt = Time.time + 0.25f;
            Fx.Spawn("fx_soulburst", boss.transform.position, bodyR * 1.6f, new Color(1f, 1f, 1f, 0.7f), 18f);
        }

        if (!active)
        {
            if (pending > 0 && CanStart()) Trigger();
            return;
        }

        float dt = Time.deltaTime;
        if (!BossUltimate.Active) timeLeft -= dt;       // 결계가 기억을 먹지 않게
        if (shield)
        {
            retargetLeft -= dt;
            if (retargetLeft <= 0f)
            {
                retargetLeft = retargetEvery;
                PlayerController p = Hostile.Player;
                if (p != null)
                {
                    Vector2 d = p.transform.position - boss.transform.position;
                    if (d.sqrMagnitude > 0.0001f) shieldDir = d.normalized;
                }
            }
        }
        if (timeLeft <= 0f) { Stop(true); return; }
        Draw();
    }

    bool CanStart()
    {
        if (BossUltimate.Active || BossUltimate.CutscenePlaying || StoryDirector.Playing || boss.invulnerable) return false;
        if (Time.timeScale == 0f || Time.time < nextAllowedAt) return false;
        if (running != null && running != this) return false;
        return Hostile.Player != null;
    }

    // ================================================================= 발동
    void Trigger()
    {
        float max = Mathf.Max(1f, boss.setEnemyHP);
        string pick = Top(sinceSum >= max * 0.01f ? since : total);
        if (pick == null) pick = Top(total);
        // 아직 기억할 공격이 없음 (지금까지 피해가 모두 「기타」 — 장판 · 출처 없는 피해): 이 체력 값은 버리지 않고 미뤄 둠
        //   다음에 기억할 출처가 생기면 그때 발동 (Top 은 출처 수만큼만 돌아 매 프레임 다시 봐도 가벼움)
        if (pick == null) return;
        pending--;

        string weapon = null;
        SpecialAbilities sa = SpecialAbilities.SharedInstance;
        int w = sa != null ? sa.CurrentWeapon : -1;
        if (sa != null && sa.abilities != null && w >= 0 && w < sa.abilities.Length && sa.abilities[w] != null) weapon = sa.abilities[w].name;
        shield = pick == DamageSource.Basic || (weapon != null && pick == weapon);
        remembered = pick;

        Difficulty d = GameMode.Current;
        bool easy = d == Difficulty.Easy, hard = d == Difficulty.Hard;
        blockMul = shield ? (easy ? 0.4f : hard ? 0.15f : 0.2f) : (easy ? 0.5f : hard ? 0.3f : 0.35f);
        if (kind == 2) blockMul = 1f - (1f - blockMul) * 0.5f;      // 킹 슬라임: 막는 몫 절반
        float duration = kind == 2 || kind == bossbar.DeveloperMomKind || easy ? 10f : 12f;
        breakNeed = kind == bossbar.DeveloperMomKind ? 0.04f : easy ? 0.03f : 0.05f;
        retargetEvery = kind == 2 ? 6f : kind == 1 ? 3f : 4f;
        warnTime = easy ? 1f : 0.8f;
        timeLeft = duration;
        breakAccum = 0f;
        active = true;
        running = this;
        RunCounters.Add("bossmem.trig");

        PlayerController p = Hostile.Player;
        Vector3 bp = boss.transform.position;
        Vector2 toP = p != null ? (Vector2)(p.transform.position - bp) : Vector2.down;
        shieldDir = toP.sqrMagnitude > 0.0001f ? toP.normalized : Vector2.down;
        retargetLeft = retargetEvery;

        // 연출: 흰빛(보스 몸 0.6초 + 이펙트) · 약한 흔들림 · 소리 · 큰 알림 (게임을 멈추지 않음)
        boss.MemoryWhite(0.6f, 0f);
        using (DamageSource.As(DamageSource.Other))     // 1.6.0~ 적 연출 (지금은 Update 에서만 불리지만 피해 호출 안에서 불려도 「내 것」이 되지 않게)
        {
            Fx.Spawn("fx_soulburst", bp, bodyR * 2.6f, Color.white, 16f);
            Fx.Spawn("fx_shock", bp, bodyR * 3f, color, 18f);
        }
        Hostile.Shake(0.25f);
        Hostile.Play("shimmer", 0.7f, 0.55f);

        barNoun = Noun(pick);
        BuildBarTexts();
        string secs = Mathf.RoundToInt(duration).ToString();
        string rule = Loc.T(shield ? "bossmem.sub.shield" : "bossmem.sub.ward").Replace("{0}", barNoun).Replace("{1}", secs);
        string line = Loc.T(LineKey(kind)).Replace("{0}", barNoun);
        if (kind == bossbar.DeveloperKind)      // 개발자: 제목은 패치 노트 농담, 대사는 부제 첫 줄
            Notice.Big(Loc.T("bossmem.dev.title").Replace("{0}", barNoun), line + "\n" + rule, NoticeIcon.Boss, 3f, NoticeLevel.Urgent, color);
        else
            Notice.Big(line, rule, NoticeIcon.Boss, 3f, NoticeLevel.Urgent, color);
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        if (fx != null) fx.FloatText(bp + Vector3.up * bodyR, Loc.T(shield ? "bossmem.shield.name" : "bossmem.ward.name"), color, 5f, 0.5f, 1.4f);
        Draw();
    }

    // 사전에서 1위 (기타 제외) — 발동 순간에만
    string Top(Dictionary<string, float> from)
    {
        string best = null;
        float bestV = 0f;
        for (int i = 0; i < keys.Count; i++)
        {
            string k = keys[i];
            if (k == DamageSource.Other) continue;
            if (from.TryGetValue(k, out float v) && v > bestV) { bestV = v; best = k; }
        }
        return best;
    }

    // 기억한 공격의 이름말 (평타는 영웅마다 · 필살기는 「일격」 · 무기 · 능력은 그 이름)
    static string Noun(string key)
    {
        if (key == DamageSource.Basic)
        {
            return Loc.T(CharacterData.Selected switch
            {
                CharacterId.Swordsman => "bossmem.noun.swordsman",
                CharacterId.Rogue => "bossmem.noun.rogue",
                CharacterId.Archer => "bossmem.noun.archer",
                CharacterId.Alchemist => "bossmem.noun.alchemist",
                CharacterId.Cartographer => "bossmem.noun.cartographer",
                CharacterId.Clockmaker => "bossmem.noun.clockmaker",
                CharacterId.Postman => "bossmem.noun.postman",
                CharacterId.Ghost => "bossmem.noun.ghost",
                _ => "bossmem.noun.gunner",
            });
        }
        if (key == DamageSource.Ult) return Loc.T("bossmem.noun.ult");
        if (string.IsNullOrEmpty(key) || key == DamageSource.Other) return Loc.T("bossmem.noun.other");
        return AttackLabel.Translate(key);
    }

    // 보스바 「기억: {공격} {n}초」 — 0 ~ 15초 문자열을 발동 때 미리 (프레임마다 할당 없음 · 언어가 바뀌면 다시)
    void BuildBarTexts()
    {
        if (barTexts == null) barTexts = new string[16];
        barLang = Loc.Current;
        string fmt = Loc.T("bossmem.bar").Replace("{0}", barNoun);
        for (int i = 0; i < barTexts.Length; i++) barTexts[i] = fmt.Replace("{1}", i.ToString());
    }

    // bossbar 가 읽음: 진행 중이면 글자, 아니면 null
    public string BarText
    {
        get
        {
            if (!active || barTexts == null) return null;
            if (barLang != Loc.Current) { barNoun = Noun(remembered); BuildBarTexts(); }
            return barTexts[Mathf.Clamp(Mathf.CeilToInt(timeLeft), 0, barTexts.Length - 1)];
        }
    }

    // ================================================================= 끝: 깨짐 · 흐려짐
    void Break()
    {
        Vector3 bp = boss.transform.position;
        Stop(false);
        boss.memoryTakenMul = 1.25f;
        staggerUntil = Time.time + 1.5f;
        boss.MemoryWhite(1.5f, 0.25f);      // 휘청: 몸이 하얗게 깜빡 (「지금 더 아프다」를 몸으로)
        staggerFxAt = 0f;
        RunCounters.Add("bossmem.break");
        // 1.6.0~ 깨짐은 플레이어 피해 호출(OnHealth) 안이라 「내 것」으로 잡혀 적 아래 층 · 내 이펙트 불투명도 · 장식 수명이 걸렸음 → 적 쪽 연출로
        using (DamageSource.As(DamageSource.Other))
        {
            Fx.Spawn("fx_shock", bp, bodyR * 3.4f, Color.white, 16f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                Fx.Spawn("fx_spark", bp + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * bodyR * 1.35f, 2.2f, Color.Lerp(color, Color.white, 0.5f), 18f);
            }
        }
        Hostile.Play("shatter", 0.8f, 0.8f);
        Hostile.Shake(0.3f);
        Notice.Big(Loc.T("bossmem.break.title"), Loc.T("bossmem.break.sub"), NoticeIcon.Boss, 2.4f, NoticeLevel.Urgent, color);
    }

    void Stop(bool faded)
    {
        active = false;
        lastBlocked = false;
        if (running == this) running = null;
        nextAllowedAt = Time.time + 3f;
        // 두 번째부터는 지난 기억이 끝난 뒤의 1위
        for (int i = 0; i < keys.Count; i++) since[keys[i]] = 0f;
        sinceSum = 0f;
        if (arc != null) arc.enabled = false;
        if (veil != null) veil.SetActive(false);
        if (faded) Notice.Toast(NoticeIcon.Boss, Loc.T("bossmem.fade"));
    }

    void OnDestroy()
    {
        if (running == this) running = null;
    }

    // ================================================================= 그리기 (방패 띠 · 결의 막)
    void Draw()
    {
        Vector3 bp = boss.transform.position;
        if (shield)
        {
            if (arc == null)
            {
                arc = Hostile.NewLine("BossMemShield", color, 0.35f, 21);
                arc.transform.SetParent(transform, true);
            }
            if (!arc.enabled) arc.enabled = true;
            if (veil != null && veil.activeSelf) veil.SetActive(false);
            float mid = Mathf.Atan2(shieldDir.y, shieldDir.x) * Mathf.Rad2Deg;
            Hostile.SetArc(arc, bp, bodyR * 1.35f, mid - 60f, mid + 60f);
            // 다시 도는 순간 직전엔 깜빡임 (경고)
            bool warn = retargetLeft <= warnTime;
            float a = warn ? (Mathf.Repeat(Time.time * 8f, 1f) < 0.5f ? 0.25f : 1f) : 0.85f + 0.15f * Mathf.Sin(Time.time * 6f);
            Color c = new Color(color.r, color.g, color.b, a);
            arc.startColor = arc.endColor = c;
            arc.startWidth = arc.endWidth = warn ? 0.45f : 0.35f;
        }
        else
        {
            if (arc != null && arc.enabled) arc.enabled = false;
            if (veil == null && Hostile.Glow != null)
            {
                veil = SpecialAbilities.MakeSprite("BossMemVeil", Hostile.Glow, bp, 1f, new Color(color.r, color.g, color.b, 0.3f), "Effect", 2);
                veil.transform.SetParent(transform, true);
                veilSr = veil.GetComponent<SpriteRenderer>();
            }
            if (veil == null) return;
            if (!veil.activeSelf) veil.SetActive(true);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
            veil.transform.position = bp;
            float unit = veilSr != null && veilSr.sprite != null ? Mathf.Max(0.01f, veilSr.sprite.bounds.size.x) : 1f;
            float size = bodyR * 2.6f / unit * (1f + 0.06f * pulse);      // 몸 지름보다 조금 크게
            veil.transform.localScale = new Vector3(size, size, 1f);
            if (veilSr != null) veilSr.color = new Color(color.r, color.g, color.b, 0.22f + 0.12f * pulse);
        }
    }

    // ================================================================= 거울의 군주: 정면에 막힌 평타를 느린 거울 조각으로 되쏨 (초당 4발까지)
    void Reflect(Vector3 bp)
    {
        if (Time.time < reflectAt) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        reflectAt = Time.time + 0.25f;
        Vector3 from = bp + (Vector3)(shieldDir * bodyR * 1.35f);
        Vector2 d = p.transform.position - from;
        if (d.sqrMagnitude < 0.0001f) d = shieldDir;
        HostileProjectile h = Hostile.Shoot(from, d, 8f, Capped(9f * Chapters.BossPowerMul), 0.5f, ShardColor, 1.2f, 4f);
        if (h != null) h.label = ShardLabel;
    }

    // 거울의 군주의 다른 공격과 같은 상한 (BossSkills.Capped 와 같은 식 · 그쪽은 private): 플레이어 최대 체력 × 0.2 ÷ 받는 피해 배율
    static float Capped(float raw)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) return raw;
        float scale = Mathf.Max(0.01f, GameMode.DamageMul * Chapters.HurtMul);
        return Mathf.Min(raw, pl.PlayerMaxHealth * 0.2f / scale);
    }
}
