using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 값(번호)은 프리팹에 저장되므로 순서를 바꾸지 않음
public enum EnemySkillType
{
    None, BoneSpike, Leap, Blink, XLaser, SelfDestruct, Pounce, GroundSlam, Whirlwind,
    AcidPool, PollenCloud, MushroomMines, Howl, RootLine,
    ChainPull, GazeBeam, ReapSweep, Fissure,        // 2.1.1 4장 심연 (AbyssStage)
}

// 적이 하나씩 쓰는 스킬. 투사체 없이 점프 · 레이저 · 장판 등으로 공격하고,
// 항상 경고(원 / 선 / 머리 위 !)를 먼저 보여준 뒤 발동해서 피할 수 있음
public class EnemySkill : MonoBehaviour
{
    public EnemySkillType type;
    public float cooldown;

    static readonly Color Danger = new Color(1f, 0.25f, 0.2f, 0.9f);

    EnermyController enemy;
    SpriteRenderer sr;
    float next;
    bool busy;

    bool Alive => enemy != null && !enemy.IsDead;
    // 중간 보스는 범위가 크고 더 자주 씀
    float Size => enemy.survivesContact ? 1.5f : 1f;
    // 1.1.3 밸런스 점검: 몸통 접촉은 contactDamage × 0.6 (EnermyController.ContactScale) 인데 스킬은 × 1 이라 스킬 한 방이 접촉의 1.7~2.7배,
    //   4장 거상 균열은 보통 58 (보스 한 대 20 의 3배) → 스킬 기준을 0.8 로 (자폭은 제 몸을 버리는 공격이라 그대로), 균열은 따로 ×0.75 (Fissure)
    const float SkillScale = 0.8f;
    float Dmg => enemy.contactDamage * (type == EnemySkillType.SelfDestruct ? 1f : SkillScale);

    // 스킬이 만든 표시 (기 모으는 빛 · 머리 위 스킬 이름). 적이 지워져 스킬이 중간에 끊겨도 남지 않게 꺼질 때 지움
    readonly List<GameObject> owned = new List<GameObject>();
    GameObject Own(GameObject g) { if (g != null) owned.Add(g); return g; }

    void ReleaseAbyss()
    {
        if (!abyssCounted) return;
        abyssCounted = false;
        abyssCasting = Mathf.Max(0, abyssCasting - 1);
    }

    void OnDisable()
    {
        ReleaseAbyss();
        foreach (GameObject g in owned) if (g != null) Destroy(g);
        owned.Clear();
    }

    void Start()
    {
        enemy = GetComponent<EnermyController>();
        sr = GetComponent<SpriteRenderer>();
        if (cooldown <= 0f) cooldown = DefaultCooldown(type);
        cooldown *= GameMode.SkillCooldownMul * Chapters.SkillCooldownMul;     // 앞쪽 장은 드물게 (2.1.5)
        next = Time.time + Random.Range(1.5f, cooldown);
    }

    static float DefaultCooldown(EnemySkillType t) => t switch
    {
        EnemySkillType.BoneSpike => 4.5f,
        EnemySkillType.Leap => 5f,
        EnemySkillType.Blink => 6.5f,
        EnemySkillType.XLaser => 5f,
        EnemySkillType.SelfDestruct => 1f,
        EnemySkillType.Pounce => 3.5f,
        EnemySkillType.GroundSlam => 6f,
        EnemySkillType.Whirlwind => 5.5f,
        EnemySkillType.AcidPool => 5f,
        EnemySkillType.PollenCloud => 6f,
        EnemySkillType.MushroomMines => 6.5f,
        EnemySkillType.Howl => 8f,
        EnemySkillType.RootLine => 5.5f,
        EnemySkillType.ChainPull => 6f,
        EnemySkillType.GazeBeam => 6.5f,
        EnemySkillType.ReapSweep => 4.5f,
        EnemySkillType.Fissure => 6.5f,
        _ => 5f,
    };

    bool InRange(float d) => type switch
    {
        EnemySkillType.BoneSpike => d < 12f,
        EnemySkillType.Leap => d > 4f && d < 10f,
        EnemySkillType.Blink => d > 7f && d < 22f,
        EnemySkillType.XLaser => d < 14f,
        EnemySkillType.SelfDestruct => d < 3.2f * Size,
        EnemySkillType.Pounce => d > 4f && d < 11f,
        EnemySkillType.GroundSlam => d < 7f * Size,
        EnemySkillType.Whirlwind => d < 8f,
        EnemySkillType.AcidPool => d < 6f,
        EnemySkillType.PollenCloud => d < 13f,
        EnemySkillType.MushroomMines => d < 9f,
        EnemySkillType.Howl => d < 18f,
        EnemySkillType.RootLine => d > 3f && d < 14f,
        EnemySkillType.ChainPull => d > 4f && d < 11f,
        EnemySkillType.GazeBeam => d < 13f,
        EnemySkillType.ReapSweep => d < 4.5f * Size,
        EnemySkillType.Fissure => d < 13f,
        _ => false,
    };

    // 1.5.8~ 4장 잡몹 넷은 사거리가 길고 예고가 커서 무리가 한꺼번에 쓰면 화면이 예고로 덮였음 → 동시에 3마리까지 (중간 보스는 빼고)
    const int MaxAbyssCasting = 3;
    static int abyssCasting;
    bool abyssCounted;
    bool IsAbyssSkill => type >= EnemySkillType.ChainPull && type <= EnemySkillType.Fissure && !enemy.IsMidBoss;

    void Update()
    {
        if (busy || !Alive || type == EnemySkillType.None) return;
        PlayerController p = Hostile.Player;
        if (p == null || Time.time < next) return;
        if (!InRange(Vector2.Distance(transform.position, p.transform.position))) return;
        if (IsAbyssSkill && abyssCasting >= MaxAbyssCasting) { next = Time.time + Random.Range(0.3f, 0.8f); return; }

        next = Time.time + cooldown * Random.Range(0.85f, 1.2f) * (enemy.survivesContact ? 0.7f : 1f);
        StartCoroutine(Run(p));
    }

    // 스킬 이름 (쓰는 동안 머리 위에 보여 줌)
    public static string SkillName(EnemySkillType t) => t switch
    {
        EnemySkillType.BoneSpike => "뼈 가시", EnemySkillType.Leap => "도약", EnemySkillType.Blink => "순간이동",
        EnemySkillType.XLaser => "십자 광선", EnemySkillType.SelfDestruct => "자폭", EnemySkillType.Pounce => "덮치기",
        EnemySkillType.GroundSlam => "지면 강타", EnemySkillType.Whirlwind => "회전 칼날", EnemySkillType.AcidPool => "산성 웅덩이",
        EnemySkillType.PollenCloud => "꽃가루", EnemySkillType.MushroomMines => "버섯 지뢰", EnemySkillType.Howl => "울부짖음",
        EnemySkillType.RootLine => "뿌리 가시", EnemySkillType.ChainPull => "사슬 던지기", EnemySkillType.GazeBeam => "공허의 시선",
        EnemySkillType.ReapSweep => "영혼 베기", EnemySkillType.Fissure => "심연 균열", _ => "",
    };

    static readonly System.Collections.Generic.Dictionary<EnemySkillType, int> nameShown = new System.Collections.Generic.Dictionary<EnemySkillType, int>();
    static bool NameSeen(EnemySkillType t)
    {
        nameShown.TryGetValue(t, out int n);
        if (n >= 3) return false;
        nameShown[t] = n + 1;
        return true;
    }

    IEnumerator Run(PlayerController p)
    {
        busy = true;
        enemy.casting = true;
        if (IsAbyssSkill) { abyssCasting++; abyssCounted = true; }
        // 1단계-12~ 스킬 이름표: 모든 적이 쓸 때마다 뜨던 것 → 중간 보스이거나, 그 스킬을 처음 3번 볼 때만 (무리 위 빨간 글자 잡음)
        GameObject tag = VisualRules.Legacy || enemy.IsMidBoss || NameSeen(type) ? Own(SkillTag.Show(transform, Loc.T(SkillName(type)) + "!", 1.6f * Size + 1.5f)) : null;
        IEnumerator routine = type switch
        {
            EnemySkillType.BoneSpike => BoneSpike(p),
            EnemySkillType.Leap => Leap(p),
            EnemySkillType.Blink => Blink(p),
            EnemySkillType.XLaser => XLaser(p),
            EnemySkillType.SelfDestruct => SelfDestruct(),
            EnemySkillType.Pounce => Pounce(p),
            EnemySkillType.GroundSlam => GroundSlam(),
            EnemySkillType.Whirlwind => Whirlwind(p),
            EnemySkillType.AcidPool => AcidPool(),
            EnemySkillType.PollenCloud => PollenCloud(p),
            EnemySkillType.MushroomMines => MushroomMines(),
            EnemySkillType.Howl => Howl(),
            EnemySkillType.RootLine => RootLine(p),
            EnemySkillType.ChainPull => ChainPull(p),
            EnemySkillType.GazeBeam => GazeBeam(p),
            EnemySkillType.ReapSweep => ReapSweep(),
            EnemySkillType.Fissure => Fissure(p),
            _ => null,
        };
        if (routine != null) yield return StartCoroutine(routine);
        ReleaseAbyss();
        if (tag != null) Destroy(tag);
        if (enemy != null) enemy.casting = false;
        if (sr != null && Alive) sr.color = enemy.baseColor;
        owned.RemoveAll(g => g == null);
        busy = false;
    }

    // 1.0.4~ 경고 원 · 띠 길이도 Windup 과 같은 난이도 배율 (보통 · 어려움에서 다 차기 전에 맞던 문제)
    static float Tw(float seconds) => seconds * GameMode.WindupMul;

    // 몸이 깜빡이며 기를 모음 (빛 + 머리 위 느낌표)
    IEnumerator Windup(Color color, float seconds, float speed = 8f)
    {
        seconds *= GameMode.WindupMul;      // 어려울수록 예고가 짧음
        GameObject aura = Own(Hostile.Glow != null
            ? SpecialAbilities.MakeSprite("WindupAura", Hostile.Glow, transform.position, 0.1f, new Color(color.r, color.g, color.b, 0.8f),
                VisualRules.Legacy ? "Effect" : "Character", VisualRules.Legacy ? 0 : -5) : null);      // 1단계-6~ 시전 빛은 몸 밑 (몸을 덮지 않게)
        FxAnim mark = Fx.Play("fx_warn", transform.position + Vector3.up * 1.6f * Size, 1.1f, Color.white, 1f, 0f, 30, true, seconds, VisualRules.WarnLayer);
        if (mark != null) mark.transform.SetParent(transform, true);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (!Alive) break;
            float pulse = Mathf.PingPong(Time.time * speed, 1f);
            sr.color = Color.Lerp(enemy.baseColor, color, pulse);
            if (aura != null)
            {
                aura.transform.position = transform.position;
                aura.transform.localScale = Vector3.one * Size * Mathf.Lerp(0.35f, 0.6f, t / seconds) * (0.85f + 0.3f * pulse);
            }
            yield return null;
        }
        if (aura != null) Destroy(aura);
        if (mark != null) Destroy(mark.gameObject);
        if (Alive) sr.color = enemy.baseColor;
    }

    Vector2 DirTo(PlayerController p) => ((Vector2)(p.transform.position - transform.position)).normalized;

    // ================================================================= skills
    // 뼈 가시: 플레이어 발밑에서 뼈 가시가 솟음
    IEnumerator BoneSpike(PlayerController p)
    {
        Vector3 at = Hostile.SpreadAim(p.transform.position, 1.4f);       // 1.4.4~ 여러 적이 한 점에 겹치지 않게
        Hostile.Circle(at, 1.4f, Tw(0.8f), new Color(1f, 0.95f, 0.8f, 0.9f));
        yield return Windup(new Color(1f, 1f, 0.8f), 0.8f);
        if (!Alive) yield break;
        Fx.Spawn("fx_spike", at + Vector3.up * 0.6f, 2.4f, new Color(1f, 0.97f, 0.88f), 18f);
        Hostile.HitCircle(at, 1.4f, Dmg);
        Hostile.Play("crack", 0.4f, 1.5f);
    }

    // 구울: 떨어질 곳에 원을 띄운 뒤 높이 뛰어 내려찍음
    IEnumerator Leap(PlayerController p)
    {
        float r = 1.9f * Size;
        Vector3 target = Hostile.ClampArena(Hostile.SpreadAim(p.transform.position, r));     // 1.4.4~ 착지점 흩뿌림
        if (Hostile.IsWall(target)) yield break;
        Hostile.Circle(target, r, Tw(0.7f) + 0.35f, Danger);   // 1.0.4~ 착지할 때까지 원을 남김
        yield return Windup(Danger, 0.7f);
        if (!Alive) yield break;

        Vector3 start = transform.position;
        Fx.Spawn("fx_smoke", start, 1.8f, new Color(0.8f, 0.7f, 0.6f), 14f);
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            float k = t / 0.35f;
            transform.position = Vector3.Lerp(start, target, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.5f;
            yield return null;
        }
        transform.position = target;
        if (!Alive) yield break;
        Hostile.HitCircle(target, r, Dmg * 1.2f);
        Fx.Spawn("fx_shock", target, r * 2.4f, new Color(0.9f, 0.75f, 0.55f), 20f);
        for (int i = 0; i < 4; i++) Fx.Spawn("fx_smoke", target + (Vector3)(Random.insideUnitCircle * r), 1.6f, new Color(0.75f, 0.65f, 0.55f), 12f);
        Hostile.Play("thump", 0.6f);
        Hostile.Shake(0.15f);
    }

    // 망령: 사라졌다가 표시한 곳에 나타나며 영혼 폭발
    IEnumerator Blink(PlayerController p)
    {
        Vector3 dest = Hostile.ClampArena(p.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 3f));
        if (Hostile.IsWall(dest)) yield break;
        float r = 1.8f * Size;
        Color purple = new Color(0.7f, 0.4f, 1f, 0.9f);
        Hostile.Circle(dest, r, 0.8f, purple);
        Hostile.Play("shimmer", 0.35f, 1.4f);
        Fx.Spawn("fx_soulburst", transform.position, 2.5f, Color.white, 18f);

        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            Color c = enemy.baseColor;
            c.a = 1f - Mathf.Clamp01(t / 0.3f);
            sr.color = c;
            yield return null;
        }
        if (!Alive) yield break;
        transform.position = dest;
        sr.color = enemy.baseColor;
        Hostile.HitCircle(dest, r, Dmg);
        Fx.Spawn("fx_soulburst", dest, r * 2.6f, Color.white, 16f);
        Fx.Spawn("fx_shock", dest, r * 2.4f, purple, 20f);
    }

    // 임프: 플레이어 자리에 X자 레이저 (두 줄기가 엇갈림)
    IEnumerator XLaser(PlayerController p)
    {
        // 1.4.4~ 교차점 흩뿌림: 0.9 × 0.8 = 최대 0.72 칸 → 가만히 선 플레이어는 판정(0.8) 안 그대로
        Vector3 c = Hostile.SpreadAim(p.transform.position, 0.9f);
        const float half = 7f;
        float tilt = Random.Range(0f, 90f);
        Vector3[] ends = new Vector3[4];
        for (int i = 0; i < 2; i++)
        {
            float a = (tilt + 45f + i * 90f) * Mathf.Deg2Rad;
            Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * half;
            ends[i * 2] = c - d;
            ends[i * 2 + 1] = c + d;
            Hostile.Line(c - d, c + d, 1.3f, Tw(0.9f), new Color(1f, 0.45f, 0.15f, 0.9f));
        }
        FxAnim rune = Fx.Play("fx_rune", c, 3f, new Color(1f, 0.5f, 0.2f, 0.8f), 1f, 0f, 1, true, Tw(0.9f) + 0.2f);
        if (rune != null) rune.spin = 120f;
        yield return Windup(new Color(1f, 0.6f, 0.2f), 0.9f, 12f);
        if (!Alive) yield break;

        Hostile.Play("zap", 0.7f, 0.7f);
        Hostile.Play("flame", 0.5f, 1.2f);
        for (int i = 0; i < 2; i++) Fx.Beam(ends[i * 2], ends[i * 2 + 1], 1.2f, new Color(1f, 0.55f, 0.2f), 0.35f);
        Fx.Spawn("fx_explosion", c, 2.5f, Color.white, 18f);
        PlayerController pl = Hostile.Player;
        if (pl != null)
            for (int i = 0; i < 2; i++)
                if (Hostile.HurtToSegment(pl, ends[i * 2], ends[i * 2 + 1]) < 0.8f) { pl.TryHit(Dmg * 1.1f); break; }
        Hostile.Shake(0.12f);
    }

    // 불꽃 해골: 가까이 오면 부풀어 오르다 폭발 (중간 보스는 폭발만)
    IEnumerator SelfDestruct()
    {
        float r = 2.8f * Size;
        Hostile.Circle(transform.position, r, 0.9f, new Color(1f, 0.5f, 0.1f, 0.9f));
        Hostile.Play("crackle", 0.5f, 1.3f);
        Vector3 baseScale = transform.localScale;
        // 1.1~: 위상을 누적해 점점 빨라지는 깜빡임 (Time.time × 배수는 판 후반에 프레임마다 흰색으로 튀었음), 번쩍임 끄면 흰색을 약하게
        float phase = 0f;
        float whiteMax = GameSettings.Flashes ? 1f : 0.4f;
        for (float t = 0f; t < 0.9f; t += Time.deltaTime)
        {
            if (!Alive) { transform.localScale = baseScale; yield break; }
            phase += Time.deltaTime * (8f + t * 20f);
            sr.color = Color.Lerp(enemy.baseColor, Color.white, whiteMax * Mathf.PingPong(phase, 1f));
            transform.localScale = baseScale * (1f + 0.25f * t / 0.9f);
            yield return null;
        }
        transform.localScale = baseScale;
        if (!Alive) yield break;
        Hostile.HitCircle(transform.position, r, Dmg * 1.6f);
        Fx.Spawn("fx_explosion", transform.position, r * 2.4f, Color.white, 16f);
        Fx.Spawn("fx_shock", transform.position, r * 2.6f, new Color(1f, 0.6f, 0.2f), 20f);
        Hostile.Play("boom", 0.6f, 1.2f);
        Hostile.Shake(0.2f);
        if (!enemy.survivesContact) enemy.KillBySkill();
    }

    // 지옥견: 경로를 보여준 뒤 일직선 돌진 (먼지 꼬리)
    IEnumerator Pounce(PlayerController p)
    {
        Vector2 dir = DirTo(p);
        const float length = 10f;
        Vector3 start = transform.position;
        Vector3 end = Hostile.ClampArena(start + (Vector3)(dir * length));
        Hostile.Line(start, end, 2.6f * Size, Tw(0.55f), Danger);   // 1.0.4~ 띠 너비 = 판정 지름 (1.3 x 2), 띠 밖에서 맞지 않게
        yield return Windup(Danger, 0.55f);
        if (!Alive) yield break;

        bool hit = false;
        float dust = 0f;
        Hostile.Play("whoosh", 0.4f, 0.9f);
        for (float t = 0f; t < 0.28f; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            Vector3 nextPos = Vector3.Lerp(start, end, t / 0.28f);
            if (Hostile.IsWall(nextPos)) break;
            transform.position = nextPos;
            dust += Time.deltaTime;
            if (dust > 0.04f) { dust = 0f; Fx.Spawn("fx_smoke", transform.position, 1.4f, new Color(0.7f, 0.5f, 0.45f), 16f); }
            if (!hit && Hostile.HurtDistance(p, transform.position) < 1.3f * Size) hit = p.TryHit(Dmg);
            yield return null;
        }
    }

    // 용암 골렘: 점점 넓어지는 세 번의 지면 파동 (가시가 솟음)
    IEnumerator GroundSlam()
    {
        float[] radii = { 2.5f * Size, 4.5f * Size, 6.5f * Size };
        Vector3 c = transform.position;
        yield return Windup(new Color(1f, 0.5f, 0.2f), 0.5f, 5f);
        for (int w = 0; w < radii.Length && Alive; w++)
        {
            float r = radii[w];
            float inner = w == 0 ? 0f : radii[w - 1];
            Hostile.Circle(c, r, 0.55f, new Color(1f, 0.45f, 0.15f, 0.9f));
            yield return new WaitForSeconds(0.55f);
            if (!Alive) yield break;
            PlayerController pl = Hostile.Player;
            if (pl != null)
            {
                float d = Vector2.Distance(pl.HurtCenter, c);     // 1.4.4~ 몸 아래쪽 판정
                if (d <= r + PlayerController.HurtRadius && d >= inner - PlayerController.HurtRadius) pl.TryHit(Dmg);
            }
            int n = 6 + w * 4;
            for (int i = 0; i < n; i++)
            {
                float a = (i / (float)n * 360f + w * 15f) * Mathf.Deg2Rad;
                Fx.Spawn("fx_spike", c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (r - 0.5f) + Vector3.up * 0.5f, 1.8f, new Color(1f, 0.6f, 0.35f), 18f);
            }
            Fx.Spawn("fx_shock", c, r * 2.2f, new Color(1f, 0.55f, 0.2f), 18f);
            Hostile.Play("boom", 0.5f, 0.7f + w * 0.1f);
            Hostile.Shake(0.15f);
        }
    }

    // 악마 기사: 칼날을 휘감고 회전하며 플레이어 쪽으로 미끄러짐
    IEnumerator Whirlwind(PlayerController p)
    {
        float r = 2.2f * Size;
        FxAnim ring = Fx.Play("fx_rune", transform.position, r * 2f, new Color(1f, 0.3f, 0.3f, 0.7f), 1f, 0f, 1, true, Tw(0.7f));
        if (ring != null) { ring.follow = transform; ring.spin = -200f; }
        yield return Windup(Danger, 0.7f);
        if (!Alive) yield break;

        Hostile.Play("whoosh", 0.6f, 0.7f);
        float slash = 0f;
        float lastHit = -1f;
        for (float t = 0f; t < 1.4f && Alive; t += Time.deltaTime)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) break;
            Vector3 step = (Vector3)(DirTo(pl) * enemy.speed * 0.9f * Time.deltaTime);
            if (!Hostile.IsWall(transform.position + step)) transform.position += step;
            slash += Time.deltaTime;
            if (slash > 0.12f)
            {
                slash = 0f;
                Fx.Spawn("fx_slash", transform.position, r * 2.2f, new Color(1f, 0.55f, 0.55f), 30f, Random.Range(0f, 360f), 14);
                Hostile.Play("whoosh", 0.2f, 1.4f);
            }
            if (Time.time - lastHit > 0.45f && Hostile.HurtDistance(pl, transform.position) < r)
            {
                lastHit = Time.time;
                pl.TryHit(Dmg * 0.8f);
            }
            yield return null;
        }
    }

    // 산성 웅덩이: 제자리에 밟으면 아픈 웅덩이를 남김 (4초)
    IEnumerator AcidPool()
    {
        float r = 2.2f * Size;
        Vector3 c = transform.position;
        Hostile.Circle(c, r, Tw(0.6f), new Color(0.5f, 1f, 0.3f, 0.9f));
        yield return Windup(new Color(0.6f, 1f, 0.4f), 0.6f);
        if (!Alive) yield break;
        HazardZone.Spawn(c, r, 4f, Dmg * 0.6f, new Color(0.55f, 1f, 0.35f, 0.9f), "fx_puddle", 0f);
        Hostile.Play("hiss", 0.5f, 1.2f);
    }

    // 꽃가루 구름: 플레이어 주변에 구름 → 안에 있으면 느려지고 조금씩 아픔
    IEnumerator PollenCloud(PlayerController p)
    {
        float r = 3f * Size;
        Vector3 c = Hostile.SpreadAim(p.transform.position, r);       // 1.4.4~ 여러 구름이 한 점에 겹치지 않게
        Hostile.Circle(c, r, Tw(0.9f), new Color(1f, 0.9f, 0.35f, 0.9f));
        yield return Windup(new Color(1f, 0.95f, 0.5f), 0.9f);
        if (!Alive) yield break;
        HazardZone z = HazardZone.Spawn(c, r, 4f, Dmg * 0.25f, new Color(1f, 0.92f, 0.45f, 0.75f), "fx_cloud", 0.55f);
        z.fps = 6f;
        Hostile.Play("shimmer", 0.4f, 0.8f);
    }

    // 버섯 지뢰: 주변에 버섯을 심고 잠시 뒤 차례로 터짐
    IEnumerator MushroomMines()
    {
        yield return Windup(new Color(1f, 0.4f, 0.35f), 0.5f);
        if (!Alive) yield break;
        int n = enemy.survivesContact ? 5 : 3;
        for (int i = 0; i < n; i++)
        {
            Vector3 at = Hostile.ClampArena(transform.position + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(2f, 4.5f)));
            StartCoroutine(Mine(at, 1.3f + i * 0.25f));
        }
        Hostile.Play("thump", 0.4f, 1.4f);
    }

    IEnumerator Mine(Vector3 at, float fuse)
    {
        const float r = 1.8f;
        FxAnim cap = Fx.Play("fx_mushroom", at, 1.1f, Color.white, 6f, 0f, 6, true, fuse);
        Hostile.Circle(at, r, fuse, new Color(1f, 0.35f, 0.3f, 0.8f));
        yield return new WaitForSeconds(fuse);
        Hostile.HitCircle(at, r, Dmg * 0.9f);
        Fx.Spawn("fx_cloud", at, r * 2.4f, new Color(1f, 0.55f, 0.5f), 18f);
        Fx.Spawn("fx_shock", at, r * 2.2f, new Color(1f, 0.4f, 0.35f), 20f);
        Hostile.Play("boom", 0.35f, 1.5f);
    }

    // 울부짖음: 주변 적이 잠시 빨라짐 (붉은 파동)
    IEnumerator Howl()
    {
        yield return Windup(new Color(1f, 0.3f, 0.3f), 0.6f, 14f);
        if (!Alive) yield break;
        const float r = 9f;
        Fx.Spawn("fx_shock", transform.position, r * 2f, new Color(1f, 0.35f, 0.3f), 14f);
        ShockRing.Spawn(transform.position, 0.5f, r, 0.5f, new Color(1f, 0.4f, 0.35f, 0.9f), 0.35f);
        Hostile.Play("pulse", 0.7f, 0.8f);
        // 1.2.4: OverlapCircleAll(부를 때마다 새 배열) → 공용 버퍼 (같은 조건 · 같은 결과, 이 안에서 다시 질의하지 않음)
        foreach (Collider2D c in Specials.Overlap(transform.position, r, Hostile.AreaBuf))
        {
            if (!c.TryGetComponent(out EnermyController e) || e.IsDead) continue;
            e.Enrage(1.6f, 3.5f);
            Fx.Spawn("fx_spark", e.transform.position + Vector3.up, 1.2f, new Color(1f, 0.4f, 0.35f), 14f);
        }
    }

    // 뿌리 가시 행렬: 플레이어 쪽으로 가시가 차례로 솟아오름
    IEnumerator RootLine(PlayerController p)
    {
        Vector2 dir = DirTo(p);
        const int count = 7;
        const float spacing = 1.8f;
        Vector3 start = transform.position + (Vector3)(dir * 1.5f);
        Hostile.Line(start, start + (Vector3)(dir * spacing * count), 1.6f, Tw(0.7f), new Color(0.6f, 0.9f, 0.3f, 0.9f));
        yield return Windup(new Color(0.6f, 1f, 0.4f), 0.7f);
        for (int i = 0; i < count && Alive; i++)
        {
            Vector3 at = start + (Vector3)(dir * spacing * i);
            Fx.Spawn("fx_spike", at + Vector3.up * 0.6f, 2.4f, new Color(0.65f, 0.5f, 0.3f), 18f);
            Fx.Spawn("fx_smoke", at, 1.2f, new Color(0.55f, 0.45f, 0.3f), 14f);
            Hostile.HitCircle(at, 1.1f, Dmg);
            Hostile.Play("crack", 0.25f, 1.2f + i * 0.05f);
            yield return new WaitForSeconds(0.09f);
        }
    }

    // ================================================================= 4장 심연 (2.1.1)
    static readonly Color Soul = new Color(0.55f, 0.92f, 1f, 0.9f);
    static readonly Color Void = new Color(0.72f, 0.4f, 1f, 0.9f);
    // 1.5.8~ 예고 색도 스킬마다 다르게 (사슬 = 쇳빛 · 시선 = 보라 · 베기 = 하늘 · 균열 = 청록 · AbyssWarn)
    static readonly Color Steel = new Color(0.85f, 0.88f, 1f, 0.9f);
    static readonly Color Rift = new Color(0.35f, 1f, 0.75f, 0.9f);

    // 사슬 망령: 경로를 보여 준 뒤 사슬을 던져, 맞으면 끌어당김 (적 앞 2.5칸까지만 · 끌린 뒤 피할 틈이 있게)
    IEnumerator ChainPull(PlayerController p)
    {
        Vector2 dir = DirTo(p);
        const float length = 11f;
        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(dir * length);
        Own(AbyssWarn.Chain(start, end, 1.5f, Tw(0.7f), Steel, enemy));
        yield return Windup(Steel, 0.7f);
        if (!Alive) yield break;

        Hostile.Play("clank", 0.5f, 0.8f);
        bool caught = false;
        for (float t = 0f; t < 0.22f && !caught; t += Time.deltaTime)
        {
            Vector3 tip = Vector3.Lerp(start, end, t / 0.22f);
            Fx.Beam(start, tip, 0.45f, new Color(0.8f, 0.82f, 0.95f), 0.05f);
            PlayerController pl = Hostile.Player;
            if (pl != null && Hostile.HurtToSegment(pl, start, tip) < 0.75f) caught = pl.TryHit(Dmg * 0.7f);
            yield return null;
        }
        if (!caught || !Alive) yield break;

        PlayerController target = Hostile.Player;
        if (target == null || target.IsDying) yield break;
        Vector3 from = target.transform.position;
        Vector3 to = transform.position + (Vector3)(((Vector2)(from - transform.position)).normalized * 2.5f);
        Hostile.Play("whoosh", 0.5f, 0.7f);
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            if (target == null) yield break;
            Vector3 next = Hostile.ClampArena(Vector3.Lerp(from, to, t / 0.25f));
            if (Hostile.IsWall(next)) break;
            target.transform.position = next;
            Fx.Beam(transform.position, target.transform.position, 0.4f, new Color(0.8f, 0.82f, 0.95f), 0.05f);
            yield return null;
        }
    }

    // 공허 눈알: 플레이어 쪽 부채꼴 양 끝을 보여 준 뒤 그 사이를 훑는 광선 (한 번만 맞음)
    IEnumerator GazeBeam(PlayerController p)
    {
        Vector2 dir = DirTo(p);
        const float length = 13f;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float sweep = Random.value < 0.5f ? 40f : -40f;
        Own(AbyssWarn.Fan(transform.position, length, baseAngle - sweep * 0.5f, sweep, Tw(0.85f), Void, enemy));
        yield return Windup(Void, 0.85f, 10f);
        if (!Alive) yield break;

        Hostile.Play("zap", 0.6f, 0.5f);
        bool hit = false;
        for (float t = 0f; t < 1.1f && Alive; t += Time.deltaTime)
        {
            float a = (baseAngle - sweep * 0.5f + sweep * t / 1.1f) * Mathf.Deg2Rad;
            Vector3 end = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * length;
            Fx.Beam(transform.position, end, 0.9f, new Color(0.8f, 0.45f, 1f), 0.05f);
            PlayerController pl = Hostile.Player;
            if (!hit && pl != null && Hostile.HurtToSegment(pl, transform.position, end) < 0.7f) hit = pl.TryHit(Dmg);
            yield return null;
        }
    }

    // 영혼 수확자: 둘레를 두 번 베어 냄 (처음은 가까이, 다음은 그 바깥 고리 · 두 번째 때는 안쪽이 안전)
    IEnumerator ReapSweep()
    {
        float r1 = 3.2f * Size, r2 = 5f * Size;
        Vector3 c = transform.position;
        Own(AbyssWarn.Reap(c, r1, r2, Tw(0.6f), 0.45f, Soul, enemy));
        yield return Windup(Soul, 0.6f);
        if (!Alive) yield break;
        Fx.Spawn("fx_slash", c, r1 * 2.2f, new Color(0.7f, 0.95f, 1f), 30f, Random.Range(0f, 360f), 14);
        Hostile.HitCircle(c, r1, Dmg);
        Hostile.Play("whoosh", 0.6f, 0.8f);

        yield return new WaitForSeconds(0.45f);
        if (!Alive) yield break;
        Fx.Spawn("fx_slash", c, r2 * 2.2f, new Color(0.7f, 0.95f, 1f), 30f, Random.Range(0f, 360f), 14);
        Fx.Spawn("fx_shock", c, r2 * 2.2f, Soul, 20f);
        PlayerController pl = Hostile.Player;
        if (pl != null)
        {
            float d = Vector2.Distance(pl.HurtCenter, c);     // 1.4.4~ 몸 아래쪽 판정
            if (d <= r2 + PlayerController.HurtRadius && d >= r1 - PlayerController.HurtRadius) pl.TryHit(Dmg);
        }
        Hostile.Play("whoosh", 0.6f, 0.6f);
    }

    // 심연 거상: 플레이어 쪽 세 갈래로 땅이 갈라지며 영혼빛 가시가 차례로 솟음 (한 번만 맞음)
    IEnumerator Fissure(PlayerController p)
    {
        Vector2 dir = DirTo(p);
        const int count = 7;
        const float spacing = 1.8f;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector3 c = transform.position;
        Vector2[] dirs = new Vector2[3];
        for (int k = 0; k < 3; k++)
        {
            float a = (baseAngle + (k - 1) * 22f) * Mathf.Deg2Rad;
            dirs[k] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }
        Own(AbyssWarn.Crack(c, dirs, 0.5f, 1.5f + spacing * (count - 1) + 1f, 2f, Tw(0.8f), Rift, enemy));
        yield return Windup(Rift, 0.8f, 5f);
        if (!Alive) yield break;
        Hostile.Shake(0.15f);
        bool hit = false;
        for (int i = 0; i < count && Alive; i++)
        {
            for (int k = 0; k < 3; k++)
            {
                Vector3 at = c + (Vector3)(dirs[k] * (1.5f + spacing * i));
                Fx.Spawn("fx_spike", at + Vector3.up * 0.6f, 2.3f, new Color(0.6f, 0.9f, 1f), 18f);
                if (!hit) hit = Hostile.HitCircle(at, 1f, Dmg * 0.75f);      // 1.1.3: 거상 접촉 30 이 가장 커서 균열만 더 낮춤 (보통 58 → 35)
            }
            Hostile.Play("crack", 0.3f, 0.9f + i * 0.05f);
            yield return new WaitForSeconds(0.08f);
        }
    }
}

// 밟으면 아픈 장판 (산성 웅덩이, 꽃가루 구름): 플레이어가 안에 있으면 틱 피해 + 감속
public class HazardZone : MonoBehaviour
{
    public float radius;
    public float life;
    public float tickDamage;
    public float slow;          // 0이면 감속 없음, 0.55 = 55% 속도
    public float fps = 8f;
    public string label;        // 받은 피해 출처 (AttackLabel, 만든 스킬 이름)
    float age;
    float tick;
    FxAnim visual;

    // caller · file: 만든 스킬 (컴파일러가 채움, 받은 피해 출처 이름 · 1.0.1 호출 스택을 뒤지지 않게)
    public static HazardZone Spawn(Vector3 pos, float radius, float life, float tickDamage, Color color, string fx, float slow,
        [System.Runtime.CompilerServices.CallerMemberName] string caller = "", [System.Runtime.CompilerServices.CallerFilePath] string file = "")
    {
        GameObject go = new GameObject("HazardZone");
        go.transform.position = pos;
        HazardZone z = go.AddComponent<HazardZone>();
        z.radius = radius;
        z.life = life;
        z.tickDamage = tickDamage;
        z.slow = slow;
        z.label = AttackLabel.FromCaller(file, caller);
        // 1.3.3~ fx "fire": 불 웅덩이 고품질 연출 (FireGround) — 예전엔 산성 웅덩이 그림을 주황으로 물들였음
        if (fx == "fire") { FireGround.Attach(go, radius, life, 6, true); return z; }      // 적 불 장판: 경고 테 (1단계-4)
        z.visual = Fx.Play(fx, pos, radius * (fx == "fx_puddle" ? 1.1f : 2.2f), color, z.fps, 0f, fx == "fx_puddle" ? 1 : 16, true, life);
        if (z.visual != null && fx == "fx_puddle")
        {
            // 웅덩이는 옆으로 넓은 그림이라 가로 비율 유지
            Vector3 s = z.visual.transform.localScale;
            z.visual.transform.localScale = new Vector3(s.x, s.y, 1f);
        }
        // 1단계-6~ 적 장판(산성 · 꽃가루 등) 가장자리 경고 테 — 판정 반지름 = 테 가운데 · 스크립트 없는 자식 그림 · 장판과 같이 사라짐
        if (!VisualRules.Legacy)
        {
            Sprite ring = PixelRingArt.Get(radius, -1f, true);
            if (ring != null)
            {
                GameObject rg = new GameObject("HazardRim");
                rg.transform.SetParent(go.transform, false);
                SpriteRenderer rr = rg.AddComponent<SpriteRenderer>();
                rr.sprite = ring;
                rr.color = new Color(color.r, color.g, color.b, 0.85f);
                rr.sortingLayerID = SortingLayer.NameToID(VisualRules.WarnLayer);
                rr.sortingOrder = 0;
            }
        }
        return z;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (visual != null) visual.fps = fps;
        PlayerController p = Hostile.Player;
        if (p != null && Vector2.Distance(p.HurtCenter, transform.position) < radius)      // 1.4.4~ 몸 아래쪽 판정
        {
            if (slow > 0f) p.Slow(slow, 0.2f);
            tick += Time.deltaTime;
            if (tick >= 0.6f)
            {
                tick = 0f;
                AttackLabel.Pending = label;
                p.TryHit(tickDamage);
                AttackLabel.Pending = null;
            }
        }
        if (age >= life) Destroy(gameObject);
    }
}

// ===================================================================== shared helpers (적 · 보스 공용)
public static class Hostile
{
    static PlayerController player;

    public static PlayerController Player
    {
        get
        {
            if (player == null) player = Object.FindFirstObjectByType<PlayerController>();
            return player;
        }
    }

    public static Sprite Glow => SpecialAbilities.GlowSprite;

    public static void Play(string sound, float vol = 1f, float pitch = 1f)
    {
        if (SpecialAbilities.SharedFx != null) { SpecialAbilities.SharedFx.Play(sound, vol, pitch); return; }
        // 1.2.6~ 메인 메뉴 · 결과 화면에는 SharedFx 가 없어 계약 · 사다리 · 기억 조각 · 결과 보상 소리가 전부 무음이었음
        //   → 장면을 넘어 남는 소리 전용 하나를 한 번만 만들어 돌려 씀 (합성 클립이 많아 다시 만들지 않음)
        if (menuFx == null)
        {
            GameObject go = new GameObject("MenuAudio");
            Object.DontDestroyOnLoad(go);
            menuFx = go.AddComponent<SpecialFeedback>();
            menuFx.Init(UIKit.Font, UIKit.FontMaterial);
        }
        menuFx.Play(sound, vol, pitch);
    }
    static SpecialFeedback menuFx;

    public static void Shake(float strength)
    {
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.Shake(strength, 0.15f);
    }

    //   layer: 1단계-6~ 적 경고는 VisualRules.WarnLayer (플레이어 조준선 · 시계공 · 지도 제작자도 이 함수를 써서 기본은 Effect 그대로)
    public static LineRenderer NewLine(string name, Color color, float width, int order = 18, string layer = "Effect")
    {
        GameObject go = new GameObject(name);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        // 1.5.1~ 단색 선 → 도트 획 (어두운 외곽 · 색 · 밝은 심, 색 부분 두께는 그대로 · PixelLine)
        PixelLine.Apply(lr);
        lr.useWorldSpace = true;
        lr.sortingLayerName = layer;
        lr.sortingOrder = order;
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        lr.numCapVertices = 2;
        return lr;
    }

    // 1.2.4: 점 개수별 좌표 버퍼 (SetPosition 을 점마다 부르던 것을 SetPositions 한 번으로 · 같은 좌표)
    static readonly Dictionary<int, Vector3[]> arcBufs = new Dictionary<int, Vector3[]>();
    public static Vector3[] PointBuffer(int count)
    {
        if (!arcBufs.TryGetValue(count, out Vector3[] buf)) arcBufs[count] = buf = new Vector3[count];
        return buf;
    }

    // 1.2.4: 적 스킬이 쓰는 범위 질의 공용 버퍼 (울부짖음 · 이 안에서 다시 질의하지 않는 곳만)
    public static readonly List<Collider2D> AreaBuf = new List<Collider2D>(64);

    public static void SetArc(LineRenderer lr, Vector3 center, float radius, float fromDeg, float toDeg)
    {
        int seg = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(toDeg - fromDeg) / 6f));
        int n = seg + 1;
        Vector3[] buf = PointBuffer(n);
        for (int i = 0; i <= seg; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, i / (float)seg) * Mathf.Deg2Rad;
            buf[i] = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }
        if (lr.positionCount != n) lr.positionCount = n;
        lr.SetPositions(buf);
    }

    // 원형 경고: 테두리 + 안쪽이 차오르는 원 (다 차면 발동)
    // 1.2.4: 끝난 경고는 꺼 두었다가 다시 씀 (원 · 직선 따로 · 자식 구성이 달라서)
    public static Telegraph Circle(Vector3 pos, float radius, float duration, Color color)
    {
        Telegraph t = Telegraph.Take(true);
        if (t == null)
        {
            GameObject go = new GameObject("WarnCircle");
            go.transform.position = pos;
            t = go.AddComponent<Telegraph>();
        }
        else t.transform.position = pos;
        t.InitCircle(radius, duration, color);
        return t;
    }

    // 직선 경고: 옅은 띠 + 앞으로 차오르는 띠
    public static Telegraph Line(Vector3 a, Vector3 b, float width, float duration, Color color)
    {
        Telegraph t = Telegraph.Take(false);
        if (t == null)
        {
            GameObject go = new GameObject("WarnLine");
            t = go.AddComponent<Telegraph>();
        }
        t.InitLine(a, b, width, duration, color);
        return t;
    }

    // 색각 이상 모드: 빨강 · 주황 → 노랑빛 주황, 초록 → 파랑, 보라 → 자홍 (밝기는 그대로)
    public static Color Accessible(Color c)
    {
        if (!GameSettings.ColorBlind) return c;
        Color.RGBToHSV(c, out float h, out float s, out float v);
        if (s < 0.2f) return c;
        Color to = h < 0.14f || h > 0.92f ? new Color(1f, 0.72f, 0.1f)
                 : h < 0.5f ? new Color(0.25f, 0.6f, 1f)
                 : h > 0.7f ? new Color(1f, 0.3f, 1f) : new Color(0.3f, 0.8f, 1f);
        return new Color(to.r, to.g, to.b, c.a);
    }

    public static bool HitCircle(Vector3 pos, float radius, float damage)
    {
        PlayerController p = Player;
        if (p == null || Vector2.Distance(pos, p.HurtCenter) > radius + PlayerController.HurtRadius) return false;     // 1.4.4~ 몸 아래쪽 판정 (예전 transform 위치 + 0.4)
        return p.TryHit(damage);
    }

    // 1.4.4~ 플레이어 피격 거리 (판정 중심 PlayerController.HurtCenter 에서 잼)
    //   각 패턴의 너비 · 반지름 숫자는 예전 플레이어 반지름 0.4 를 품고 있으므로 HurtShrink 를 더해 판정만 같이 작아지게 (숫자는 그대로)
    public static float HurtDistance(PlayerController p, Vector2 at) => Vector2.Distance(at, p.HurtCenter) + PlayerController.HurtShrink;
    public static float HurtToSegment(PlayerController p, Vector2 a, Vector2 b) => DistanceToSegment(p.HurtCenter, a, b) + PlayerController.HurtShrink;

    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float k = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * k);
    }

    public static HostileProjectile Shoot(Vector3 pos, Vector2 dir, float speed, float damage, float radius, Color color, float scale, float life = 4f,
        [System.Runtime.CompilerServices.CallerMemberName] string caller = "", [System.Runtime.CompilerServices.CallerFilePath] string file = "")
    {
        // 1.2.4: 사라진 탄(구슬 · 심 · 그림자 자식 포함)은 꺼 두었다가 다시 씀 — 개발자전 탄 400발 · 초당 수백 발 생성 · 파괴를 없앰
        HostileProjectile h = HostileProjectile.Take();
        bool reused = h != null;
        if (!reused)
        {
            GameObject go = SpecialAbilities.MakeSprite("HostileShot", Glow, pos, scale, color, "Effect", 9);
            h = go.AddComponent<HostileProjectile>();
            h.body = go.GetComponent<SpriteRenderer>();
        }
        else
        {
            // 새로 만든 것과 같은 상태로 되돌림 (MakeSprite 가 넣던 값 · 지난 탄의 회전)
            Transform tr = h.transform;
            tr.position = pos;
            tr.rotation = Quaternion.identity;
            tr.localScale = Vector3.one * scale;
            if (h.body != null) h.body.color = color;
        }
        h.dir = dir.normalized;
        h.speed = speed * GameMode.ProjectileSpeedMul;     // 어려울수록 탄이 빠름
        h.damage = damage;
        h.radius = radius;
        h.life = life;
        h.label = AttackLabel.FromCaller(file, caller) ?? "적 탄환";
        h.spin = false;
        h.fiery = false;
        h.pierceWalls = false;
        h.noTrail = false;          // 개발자 고유 공격(라이브 코딩)이 켬 · 다시 쓰는 탄은 늘 꺼 둠
        h.Prepare(reused);
        h.SetGlyph(null, Color.clear);
        return h;
    }

    // 폭발 연출: 빛 + 퍼지는 고리 (+ 불꽃)
    public static void Burst(Vector3 pos, float radius, Color color, bool fire = false)
    {
        // 도트 폭발: 불이면 화염, 아니면 색 입힌 충격파와 불꽃
        if (fire) Fx.Spawn("fx_explosion", pos, radius * 2.4f, Color.white, 16f);
        else
        {
            Fx.Spawn("fx_shock", pos, radius * 2.4f, color, 20f);
            Fx.Spawn("fx_spark", pos, radius * 1.2f, color, 18f);
        }
        if (Glow == null) return;
        FadeSprite.Spawn("Burst", Glow, pos, radius * 2f / 8f, color, "Effect", 3, 0.3f);
        ShockRing.Spawn(pos, radius * 0.3f, radius * 1.1f, 0.3f, color, 0.3f);
        ShockRing.Spawn(pos, radius * 0.1f, radius * 0.8f, 0.45f, Color.white, 0.15f);
        FadeSprite.Spawn("BurstCore", Glow, pos, radius * 0.8f / 8f, new Color(1f, 1f, 1f, 0.9f), "Effect", 4, 0.12f);
        for (int i = 0; i < 8; i++) SoulWisp.Spawn(pos, pos + (Vector3)(Random.insideUnitCircle.normalized * radius * 1.6f), color, true);
        if (!fire) return;
        for (int i = 0; i < 10; i++)
        {
            Vector2 v = Random.insideUnitCircle.normalized * Random.Range(radius * 2f, radius * 4f);
            FlameParticle.Spawn(Glow, pos, v, Random.Range(0.35f, 0.55f), 0.05f, Random.Range(0.3f, 0.5f), false);
        }
    }

    // 반지름 안에 벽(구조물)이 있는지
    // 1.1.9: 예전엔 OverlapCircleAll · OverlapPointAll 로 부를 때마다 배열을 새로 만듦(적 탄은 매 프레임 탄마다 → GC 끊김)
    //   → 트리거를 빼는 필터 + 공용 버퍼 (벽은 트리거가 아니므로 결과 같음 · 적 · 탄 · 코인 같은 트리거로 버퍼가 차지 않음)
    static readonly Collider2D[] wallBuf = new Collider2D[16];
    static ContactFilter2D solidFilter = new ContactFilter2D { useTriggers = false };

    public static bool WallNear(Vector3 pos, float radius)
    {
        int n = Physics2D.OverlapCircle(pos, radius, solidFilter, wallBuf);
        for (int i = 0; i < n; i++)
            if (!wallBuf[i].isTrigger && wallBuf[i].CompareTag("Wall")) return true;
        return false;
    }

    // 벽(맵 테두리, 구조물)인지
    public static bool IsWall(Vector3 pos)
    {
        int n = Physics2D.OverlapPoint(pos, solidFilter, wallBuf);
        for (int i = 0; i < n; i++)
            if (!wallBuf[i].isTrigger && wallBuf[i].CompareTag("Wall")) return true;
        return false;
    }

    // 1.4.4~ 플레이테스트 「여러 적이 한 캐릭터를 노리면 장판이 전부 한 점에 찍힘」 → 목표점을 플레이어 둘레에 흩뿌림
    //   exactChance 만큼은 정확히, 나머지는 radius × 0.4~0.8 떨어진 곳 (판정 원 안에 서 있는 플레이어는 그대로 걸침 · 움직이면 여러 장판을 피한 느낌)
    //   맵 밖 · 벽이면 원래 자리 그대로
    public static Vector3 SpreadAim(Vector3 target, float radius, float exactChance = 0.25f)
    {
        if (radius <= 0f || Random.value < exactChance) return target;
        Vector2 off = Random.insideUnitCircle.normalized * radius * Random.Range(0.4f, 0.8f);
        Vector3 at = ClampArena(target + (Vector3)off);
        return IsWall(at) ? target : at;
    }

    static EnemySpawner arenaSpawner;
    public static Vector3 ClampArena(Vector3 p)
    {
        // 결계 밀어내기 · 드롭처럼 자주 부르므로 찾은 스포너를 기억해 둠
        if (arenaSpawner == null) arenaSpawner = Object.FindFirstObjectByType<EnemySpawner>();
        EnemySpawner sp = arenaSpawner;
        if (sp == null) return p;
        return new Vector3(Mathf.Clamp(p.x, sp.spawnAreaMin.x, sp.spawnAreaMax.x), Mathf.Clamp(p.y, sp.spawnAreaMin.y, sp.spawnAreaMax.y + 1.5f), 0f);
    }
    // 맵 안인지 (가장자리 조금 안쪽까지)
    public static bool InArena(Vector3 p) => ((Vector2)(ClampArena(p) - p)).sqrMagnitude < 0.0001f;
}

// 경고 표시: 굵은 테두리 + 차오르는 안쪽 + 발동 순간까지 좁혀 드는 고리
public class Telegraph : MonoBehaviour
{
    float duration;
    float t;
    float blinkPhase;       // 1.1~: 깜빡임 위상을 누적 (Time.time × 배수는 판 후반에 프레임마다 지글거렸음)
    Color color;
    bool circle;
    float radius;
    Vector3 a, b;
    float width;
    LineRenderer outline;
    LineRenderer under, underL, underR;         // 어떤 바닥 색에서도 보이도록 색 테두리 밑에 까만 테두리
    LineRenderer closing;
    LineRenderer edgeL, edgeR;
    LineRenderer fillLine;
    SpriteRenderer fillDisc;
    SpriteRenderer glow;
    float groundScale, fillScale;               // 1.5.1~ 바닥 원판 · 채움 원판 크기 (도트 원판 = 판정 지름, 예전 빛이면 예전 배율)
    float groundBoost = 1f;                     // 도트 바닥 원판은 디더로 반쯤 비어 있어 진하기를 올려 예전 빛 이상으로 보이게
    SpriteRenderer band;                        // 1.5.1~ 직선 예고의 옅은 띠: 단색 선 → 도트 사선 줄무늬 타일 (판정 띠 크기 그대로)
    FxAnim rune;

    // 1.2.4: 끝난 경고는 꺼 두었다가 다시 씀 (원 · 직선은 자식 구성이 달라 따로)
    static readonly Stack<Telegraph> circlePool = new Stack<Telegraph>();
    static readonly Stack<Telegraph> linePool = new Stack<Telegraph>();
    int spawnFrame = -1;
    bool built;

    // 개발자 · 엄마 고유 공격(BossSkills.OwnKit)이 끊길 때 자기가 띄운 경고만 거두려고: 띄울 때마다 +1 되는 쓰임 번호
    int useId;
    public int UseId => useId;
    // 번호가 같을 때만(= 그 뒤 다른 공격이 다시 쓰지 않았을 때만) 다 찬 것처럼 풀로 돌림 · 이미 돌아간 경고는 그대로
    public void EndEarly(int id)
    {
        if (id != useId || !gameObject.activeSelf) return;
        Release();
    }

    public static Telegraph Take(bool circle)
    {
        Stack<Telegraph> pool = circle ? circlePool : linePool;
        Telegraph t = null;
        while (pool.Count > 0 && t == null) t = pool.Pop();     // 장면이 바뀌어 지워진 것은 건너뜀
        return t;
    }

    void Release()
    {
        gameObject.SetActive(false);
        (circle ? circlePool : linePool).Push(this);
    }

    // 다시 쓰는 경고: 새로 만든 것처럼 켜진 그 프레임에는 움직이지 않음
    void Reopen()
    {
        t = 0f;
        blinkPhase = 0f;
        spawnFrame = Time.frameCount;
        gameObject.SetActive(true);
    }

    public void InitCircle(float radius, float duration, Color color)
    {
        useId++;
        color = Hostile.Accessible(color);
        circle = true;
        this.radius = radius;
        this.duration = duration;
        this.color = color;
        bool reuse = built;
        built = true;
        if (!reuse)
        {
            under = Child(Hostile.NewLine("Under", new Color(0f, 0f, 0f, 0.8f), 0.42f, 17, VisualRules.WarnLayer));
            under.loop = true;
        }
        Hostile.SetArc(under, transform.position, radius, 0f, 354f);
        if (!reuse)
        {
            outline = Child(Hostile.NewLine("Outline", color, 0.24f, 18, VisualRules.WarnLayer));
            outline.loop = true;
        }
        else outline.startColor = outline.endColor = color;
        Hostile.SetArc(outline, transform.position, radius, 0f, 354f);
        if (!reuse)
        {
            closing = Child(Hostile.NewLine("Closing", Color.white, 0.12f, 19, VisualRules.WarnLayer));
            closing.loop = true;
        }
        else
        {
            // 새 선처럼 첫 프레임엔 고리 없음 (Update 가 그림)
            closing.positionCount = 0;
            closing.startColor = closing.endColor = Color.white;
        }
        // 바닥에 도는 도트 룬 마법진 (이펙트 단계 · 스킬 강조가 Play 에서 정해지므로 다시 쓸 때도 새로 만듦)
        if (rune != null) Destroy(rune.gameObject);
        rune = Fx.Play("fx_rune", transform.position, radius * 2f, new Color(color.r, color.g, color.b, 0.55f), 1f, 0f, 1, true, duration, VisualRules.WarnLayer);
        if (rune != null)
        {
            rune.spin = 70f;
            rune.transform.SetParent(transform, true);
        }
        // 1.5.1~ 바닥 빛(흐린 Glow · 판정보다 1.2배 번짐) → 도트 바닥 원판(체크 디더 + 테두리, 가장자리 = 판정 원)
        //   차오르는 채움도 흐린 빛 → 도트 원판 (다 차면 테두리와 딱 맞음). 그림이 없으면 예전 빛 그대로
        Sprite floorSp = PixelLine.Floor != null ? PixelLine.Floor : Hostile.Glow;
        Sprite discSp = PixelLine.Disc != null ? PixelLine.Disc : Hostile.Glow;
        if (floorSp != null && discSp != null)
        {
            groundScale = floorSp == Hostile.Glow ? radius * 2.4f / 8f : PixelLine.DiscScale(floorSp, radius * 2f);
            groundBoost = floorSp == Hostile.Glow ? 1f : 1.6f;
            fillScale = discSp == Hostile.Glow ? radius * 2f / 8f : PixelLine.DiscScale(discSp, radius * 2f);
            if (glow == null)
            {
                GameObject g = SpecialAbilities.MakeSprite("Ground", floorSp, transform.position, groundScale, new Color(color.r, color.g, color.b, 0.25f), VisualRules.WarnLayer, 0);
                g.transform.SetParent(transform, true);
                glow = g.GetComponent<SpriteRenderer>();
                glow.color = new Color(color.r, color.g, color.b, 0.25f * groundBoost);
            }
            else
            {
                glow.transform.localScale = Vector3.one * groundScale;
                glow.color = new Color(color.r, color.g, color.b, 0.25f * groundBoost);
            }
            if (fillDisc == null)
            {
                GameObject d = SpecialAbilities.MakeSprite("Fill", discSp, transform.position, 0.01f, new Color(color.r, color.g, color.b, 0.8f), VisualRules.WarnLayer, 1);
                d.transform.SetParent(transform, true);
                fillDisc = d.GetComponent<SpriteRenderer>();
            }
            else
            {
                fillDisc.transform.localScale = Vector3.one * 0.01f;
                fillDisc.color = new Color(color.r, color.g, color.b, 0.8f);
            }
        }
        if (reuse) Reopen();
    }

    public void InitLine(Vector3 a, Vector3 b, float width, float duration, Color color)
    {
        useId++;
        color = Hostile.Accessible(color);
        this.a = a;
        this.b = b;
        this.width = width;
        this.duration = duration;
        this.color = color;
        bool reuse = built;
        built = true;
        Vector3 side = Vector3.Cross(b - a, Vector3.forward).normalized * width * 0.5f;
        if (reuse)
        {
            outline.startColor = outline.endColor = new Color(color.r, color.g, color.b, 0.3f);
            outline.startWidth = outline.endWidth = width;
            outline.SetPosition(0, a);
            outline.SetPosition(1, b);
            underL.SetPosition(0, a + side); underL.SetPosition(1, b + side);
            underR.SetPosition(0, a - side); underR.SetPosition(1, b - side);
            edgeL.startColor = edgeL.endColor = edgeR.startColor = edgeR.endColor = color;
            edgeL.SetPosition(0, a + side); edgeL.SetPosition(1, b + side);
            edgeR.SetPosition(0, a - side); edgeR.SetPosition(1, b - side);
            fillLine.startColor = fillLine.endColor = new Color(color.r, color.g, color.b, 0.7f);
            fillLine.startWidth = fillLine.endWidth = width * 0.7f;
            fillLine.SetPosition(0, a);
            fillLine.SetPosition(1, a);
            PlaceBand(color);
            Reopen();
            return;
        }
        outline = Child(Hostile.NewLine("Band", new Color(color.r, color.g, color.b, 0.3f), width, 1, VisualRules.WarnLayer));
        outline.positionCount = 2;
        outline.SetPosition(0, a);
        outline.SetPosition(1, b);
        // 양쪽 가장자리 선
        underL = Child(Hostile.NewLine("Under", new Color(0f, 0f, 0f, 0.8f), 0.26f, 17, VisualRules.WarnLayer));
        underR = Child(Hostile.NewLine("Under", new Color(0f, 0f, 0f, 0.8f), 0.26f, 17, VisualRules.WarnLayer));
        underL.positionCount = underR.positionCount = 2;
        underL.SetPosition(0, a + side); underL.SetPosition(1, b + side);
        underR.SetPosition(0, a - side); underR.SetPosition(1, b - side);
        edgeL = Child(Hostile.NewLine("Edge", color, 0.14f, 18, VisualRules.WarnLayer));
        edgeR = Child(Hostile.NewLine("Edge", color, 0.14f, 18, VisualRules.WarnLayer));
        edgeL.positionCount = edgeR.positionCount = 2;
        edgeL.SetPosition(0, a + side); edgeL.SetPosition(1, b + side);
        edgeR.SetPosition(0, a - side); edgeR.SetPosition(1, b - side);
        fillLine = Child(Hostile.NewLine("Fill", new Color(color.r, color.g, color.b, 0.7f), width * 0.7f, 2, VisualRules.WarnLayer));
        fillLine.positionCount = 2;
        fillLine.SetPosition(0, a);
        fillLine.SetPosition(1, a);
        PlaceBand(color);
    }

    // 1.5.1~ 옅은 띠를 도트 사선 줄무늬 타일로: a → b 길이 × 판정 너비 그대로 깔아 씀 (끝을 둥글게 늘이던 선 마개 없이 판정 직사각형 그대로)
    //   그림이 없으면 예전 단색 띠(outline 선) 그대로
    void PlaceBand(Color color)
    {
        Sprite sp = PixelLine.Band;
        if (sp == null) { outline.enabled = true; return; }
        if (band == null)
        {
            GameObject go = new GameObject("BandTiles");
            go.transform.SetParent(transform, false);
            band = go.AddComponent<SpriteRenderer>();
            band.sprite = sp;
            band.drawMode = SpriteDrawMode.Tiled;
            band.tileMode = SpriteTileMode.Continuous;
            band.sortingLayerName = VisualRules.WarnLayer;
            band.sortingOrder = 1;
        }
        outline.enabled = false;
        Vector3 d = b - a;
        band.transform.position = (a + b) * 0.5f;
        band.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        band.transform.localScale = Vector3.one;
        band.size = new Vector2(Mathf.Max(0.01f, d.magnitude), Mathf.Max(0.01f, width));
        band.color = new Color(color.r, color.g, color.b, 0.3f);
    }

    LineRenderer Child(LineRenderer lr)
    {
        lr.transform.SetParent(transform, false);
        return lr;
    }

    void Update()
    {
        if (Time.frameCount == spawnFrame) return;
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        blinkPhase += Time.deltaTime * (4f + k * 14f);
        float blink = 0.65f + 0.35f * Mathf.PingPong(blinkPhase, 1f);
        Color c = new Color(color.r, color.g, color.b, color.a * blink);

        if (circle)
        {
            outline.startColor = outline.endColor = c;
            // 발동 순간에 테두리와 겹치도록 바깥에서 좁혀 들어오는 고리
            Hostile.SetArc(closing, transform.position, radius * Mathf.Lerp(1.8f, 1f, k), 0f, 354f);
            closing.startColor = closing.endColor = new Color(1f, 1f, 1f, 0.25f + 0.55f * k);
            if (fillDisc != null) fillDisc.transform.localScale = Vector3.one * fillScale * k;
            if (glow != null) glow.color = new Color(color.r, color.g, color.b, Mathf.Min(1f, (0.12f + 0.2f * k * blink) * groundBoost));
        }
        else
        {
            fillLine.SetPosition(1, Vector3.Lerp(a, b, k));
            outline.startColor = outline.endColor = new Color(color.r, color.g, color.b, 0.25f * blink + 0.1f);
            // 도트 띠는 줄무늬의 옅은 줄이 반쯤 비어 있어 조금 진하게 (예전 단색 띠 이상으로 보이게)
            if (band != null) band.color = new Color(color.r, color.g, color.b, (0.25f * blink + 0.1f) * 1.25f);
            edgeL.startColor = edgeL.endColor = edgeR.startColor = edgeR.endColor = c;
        }

        if (t >= duration)
        {
            // 발동 순간 짧은 섬광
            if (circle) ShockRing.Spawn(transform.position, radius * 0.9f, radius * 1.15f, 0.18f, Color.white, 0.18f);
            Release();
        }
    }
}

// 적이 스킬을 쓰는 동안 머리 위에 떠 있는 이름 (까만 테두리 글씨라 어떤 바닥에서도 보임)
public class SkillTag : MonoBehaviour
{
    Transform target;
    float height;
    TMPro.TextMeshPro text;

    // color: 없으면 빨강 (개발자 금색 · 엄마 분홍 고유 공격이 색을 줌)
    public static GameObject Show(Transform target, string label, float height, Color? color = null)
    {
        if (target == null || string.IsNullOrEmpty(label) || label == "!") return null;
        GameObject go = new GameObject("SkillTag", typeof(TMPro.TextMeshPro));
        TMPro.TextMeshPro t = go.GetComponent<TMPro.TextMeshPro>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) t.font = UIKit.Font;
        if (UIKit.FontMaterial != null) t.fontSharedMaterial = UIKit.FontMaterial;
        t.text = label;
        t.fontSize = 9f;            // 4.5 는 화면에서 너무 작아 안 보였음
        t.fontStyle = TMPro.FontStyles.Bold;
        t.color = Hostile.Accessible(color ?? new Color(1f, 0.35f, 0.3f));
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.outlineWidth = 0.3f;
        t.outlineColor = new Color32(0, 0, 0, 255);
        t.rectTransform.sizeDelta = new Vector2(20f, 3f);
        t.sortingLayerID = SortingLayer.NameToID(VisualRules.WarnLayer);
        t.sortingOrder = 41;
        SkillTag s = go.AddComponent<SkillTag>();
        s.target = target;
        s.height = height;
        s.text = t;
        s.LateUpdate();
        return go;
    }

    void LateUpdate()
    {
        if (target == null) { Destroy(gameObject); return; }
        transform.position = target.position + Vector3.up * height;
        float pop = 1f + 0.08f * Mathf.Sin(Time.time * 12f);
        transform.localScale = Vector3.one * pop;
    }
}

// 퍼져 나가며 사라지는 고리
public class ShockRing : MonoBehaviour
{
    float from, to, duration, t, width;
    Color color;
    LineRenderer lr;

    // 1.2.4: 끝난 고리는 꺼 두었다가 다시 씀 (폭발 · 내려찍기 · 경고 발동마다 생성 · 파괴하던 것)
    static readonly Stack<ShockRing> pool = new Stack<ShockRing>();
    int spawnFrame = -1;

    public static void Spawn(Vector3 pos, float from, float to, float duration, Color color, float width)
    {
        ShockRing s = null;
        while (pool.Count > 0 && s == null) s = pool.Pop();     // 장면이 바뀌어 지워진 것은 건너뜀
        bool reuse = s != null && s.lr != null;
        if (!reuse)
        {
            if (s != null) Destroy(s.gameObject);
            GameObject go = new GameObject("ShockRing");
            go.transform.position = pos;
            s = go.AddComponent<ShockRing>();
        }
        else s.transform.position = pos;
        s.from = from;
        s.to = to;
        s.duration = duration;
        s.color = color;
        s.width = width;
        s.t = 0f;
        if (!reuse)
        {
            s.lr = Hostile.NewLine("Ring", color, width, 19);
            s.lr.transform.SetParent(s.transform, false);
            s.lr.loop = true;
        }
        else
        {
            // 새 선처럼: 첫 프레임엔 고리 없음 (Update 가 그림)
            s.lr.positionCount = 0;
            s.lr.startColor = s.lr.endColor = color;
            s.lr.startWidth = s.lr.endWidth = width;
            s.spawnFrame = Time.frameCount;
            s.gameObject.SetActive(true);
        }
        // 1단계-6~ 적이 낸 고리만 경고 층 (내 필살기 · 스킬 고리는 Effect 그대로) — 다시 쓰는 고리도 매번 정함
        bool mine = DamageSource.Current != null && DamageSource.Current != DamageSource.Other;
        s.lr.sortingLayerName = mine ? "Effect" : VisualRules.WarnLayer;
    }

    void Update()
    {
        if (Time.frameCount == spawnFrame) return;
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        float ease = 1f - (1f - k) * (1f - k);
        Hostile.SetArc(lr, transform.position, Mathf.Lerp(from, to, ease), 0f, 354f);
        lr.startColor = lr.endColor = new Color(color.r, color.g, color.b, color.a * (1f - k));
        lr.startWidth = lr.endWidth = width * (1f - 0.6f * k);
        if (k >= 1f)
        {
            gameObject.SetActive(false);
            pool.Push(this);
        }
    }
}

// 적이 쏘는 탄: 플레이어에 닿으면 피해, 벽에 닿거나 시간이 지나면 사라짐
public class HostileProjectile : MonoBehaviour
{
    public Vector2 dir;
    public float speed = 10f;
    public float damage = 10f;
    public float radius = 0.5f;
    public float life = 4f;
    public bool spin;
    public bool fiery;
    public bool pierceWalls;
    public string label;        // 받은 피해 출처 (AttackLabel, 쏜 스킬 이름)
    public bool noTrail;        // 색깔 꼬리 없음 (개발자 「라이브 코딩」 글자 탄 · 오류 조각 — 줄이 많아 꼬리가 글자를 덮음)
    float puff;
    float trail;
    internal SpriteRenderer body;

    // 날고 있는 탄 목록 (쳐내기가 씬 전체를 뒤지지 않게)
    public static readonly System.Collections.Generic.List<HostileProjectile> Live = new System.Collections.Generic.List<HostileProjectile>();
    void OnEnable() => Live.Add(this);
    void OnDisable() => Live.Remove(this);

    // 1.0.1: 흐릿한 빛 덩어리라 배경 · 이펙트에 묻혔음 → 검은 테두리가 있는 또렷한 구슬 + 하얀 심 + 어두운 그림자로
    //   어떤 바닥(밝은 초원 · 어두운 묘역 · 붉은 지옥)에서도 테두리 대비로 바로 보이고, 두근거려 눈에 띔
    Transform orb;
    float orbSize, age;
    SpriteRenderer orbSr, glyph;

    // 개발자 고유 공격(BossSkills.DevOwn): 쏜 순서 번호 (Ctrl+Z 가 자기가 쏜 탄인지 · 그 사이 풀에서 다시 쓰이지 않았는지 확인) · 쏜 뒤 지난 시간
    static int serialCounter;
    public int Serial { get; private set; }
    internal float Age => age;

    // 구슬 위에 글자 그림 (코드 글꼴 fx_codefont) · null 이면 숨김 (Shoot 이 늘 숨김)
    public void SetGlyph(Sprite s, Color c)
    {
        if (s == null || orb == null)
        {
            if (glyph != null) glyph.enabled = false;
            return;
        }
        if (glyph == null)
        {
            GameObject g = SpecialAbilities.MakeSprite("Glyph", s, orb.position, 1f, c, EnemyShotLayer, 12);
            g.transform.SetParent(orb, true);
            glyph = g.GetComponent<SpriteRenderer>();
        }
        glyph.sprite = s;
        glyph.color = c;
        glyph.enabled = true;
        Transform gt = glyph.transform;
        gt.localPosition = Vector3.zero;
        gt.localRotation = Quaternion.identity;
        float h = s.bounds.size.y;
        float parent = orb.lossyScale.x;
        // 월드 크기 = 구슬 지름 × 0.6 (구슬의 두근거림은 부모라 함께 받음)
        if (h > 0f && parent > 0f) gt.localScale = Vector3.one * (orbSize * 0.6f / h / parent);
    }

    // 구슬 색을 잠깐 바꿈 (Ctrl+Z 깜빡임) · null 이면 원래 색 (다시 쓸 때 Dress 가 늘 원래 색으로 되돌림)
    internal void Flash(Color? c)
    {
        if (orbSr == null) return;
        orbSr.color = c ?? Vivid(body != null ? body.color : Color.red);
    }

    // 1.2.4: 사라진 탄은 꺼 두었다가 Hostile.Shoot 가 다시 씀 (밖에서 Destroy 한 탄은 그냥 지워짐 · 동작 같음)
    static readonly Stack<HostileProjectile> pool = new Stack<HostileProjectile>();
    int spawnFrame = -1;

    internal static HostileProjectile Take()
    {
        HostileProjectile h = null;
        while (pool.Count > 0 && h == null) h = pool.Pop();     // 장면이 바뀌어 지워진 것은 건너뜀
        return h;
    }

    void Release()
    {
        gameObject.SetActive(false);
        pool.Push(this);
    }

    // Shoot 이 값을 다 넣은 뒤: 꾸밈(예전 Start) · 다시 쓰는 탄은 새로 만든 것처럼 켜진 그 프레임에는 움직이지 않음
    internal void Prepare(bool reused)
    {
        puff = 0f;
        trail = 0f;
        age = 0f;
        orbSize = Mathf.Max(0.75f, radius * 2f + 0.35f) * LookMul;     // 1.1.9: 그림만 1.2배 (맞는 거리는 radius + 0.4 그대로)
        orb = Dress(gameObject, body != null ? body.color : Color.red, orbSize);
        if (orb != null && orbSr == null) orbSr = orb.GetComponent<SpriteRenderer>();
        Serial = ++serialCounter;
        if (reused)
        {
            spawnFrame = Time.frameCount;
            gameObject.SetActive(true);
        }
    }

    // 1.1~: 적 탄 구슬 · 심 · 그림자는 Effect 보다 위 레이어(HitScan, 다른 곳은 안 씀) — 내 스킬 이펙트 · 처치 폭발 · 피해 숫자 · 총알 빛 아래에 깔려 안 보였음
    //   (중간 보스 이름표 · 조준 표시의 TargetMark 보다는 아래)
    const string EnemyShotLayer = "HitScan";

    // 1.1.9 핫픽스: 「무한 사막 맵에서 총알이 잘 안 보여」 — 밝은 모래(≈ 0.95, 0.78, 0.5) 위에선 주황 · 노랑 · 하늘색 탄의 빛 무리와 흰 심이 모래와 섞이고
    //   그림자(지름 1.5배 · 0.7, 부드러운 빛 그림이라 구슬 가장자리에선 옅음)도 약해서 얇은 검은 테두리(반지름 0.7 바깥)만 남았음
    //   → 그림 1.2배 · 그림자 1.5 → 1.85배 · 0.7 → 0.88 · 검은 테두리 0.7 → 0.6 바깥 (판정은 HostileProjectile.radius · DomainBall 거리 그대로)
    public const float LookMul = 1.2f;

    // 탄 꾸미기 (산성 덩어리 같은 다른 적 탄도 같이 씀): 뿌연 빛은 작고 진하게 줄이고, 그 위에 테두리 구슬 · 하얀 심
    public static Transform Dress(GameObject go, Color color, float diameter)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        Color v = Vivid(color);
        float s = diameter * 2.3f / 8f;
        go.transform.localScale = Vector3.one * s;      // 빛 그림은 8칸 크기
        if (sr != null) sr.color = new Color(v.r, v.g, v.b, 0.8f);
        // 1.2.4: 다시 쓰는 탄은 이미 있는 그림자 · 구슬 · 심의 크기 · 색만 다시 넣음 (새로 만든 것과 같은 월드 크기)
        Transform tr = go.transform;
        Transform oldShadow = tr.childCount >= 2 ? tr.Find("Shadow") : null;
        Transform oldBall = oldShadow != null ? tr.Find("Orb") : null;
        if (oldBall != null && s > 0f)
        {
            oldShadow.localPosition = Vector3.zero;
            oldShadow.localRotation = Quaternion.identity;
            oldShadow.localScale = Vector3.one * (diameter * 1.85f / 8f / s);
            oldBall.localPosition = Vector3.zero;
            oldBall.localRotation = Quaternion.identity;
            oldBall.localScale = Vector3.one * (diameter / s);
            SpriteRenderer ballSr = oldBall.GetComponent<SpriteRenderer>();
            if (ballSr != null) ballSr.color = v;
            return oldBall;
        }
        Vector3 at = go.transform.position;
        GameObject shadow = SpecialAbilities.MakeSprite("Shadow", Hostile.Glow, at, diameter * 1.85f / 8f, new Color(0.02f, 0f, 0.04f, 0.88f), EnemyShotLayer, 7);
        shadow.transform.SetParent(go.transform, true);
        GameObject ball = SpecialAbilities.MakeSprite("Orb", OrbSprite, at, diameter, v, EnemyShotLayer, 10);
        ball.transform.SetParent(go.transform, true);
        GameObject core = SpecialAbilities.MakeSprite("Core", OrbSprite, at, diameter * 0.42f, Color.white, EnemyShotLayer, 11);
        core.transform.SetParent(ball.transform, true);
        return ball.transform;
    }

    // 원래 색의 색상은 그대로, 채도 · 밝기를 끌어올림 (반투명 · 탁한 색이 섞여도 선명하게)
    public static Color Vivid(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        Color o = Color.HSVToRGB(h, Mathf.Max(s, 0.65f), 1f);
        o.a = 1f;
        return o;
    }

    // 테두리 구슬 그림 (흰 속 + 검은 테두리 · 색은 SpriteRenderer 가 입힘)
    static Sprite orbSprite;
    static Sprite OrbSprite
    {
        get
        {
            if (orbSprite != null) return orbSprite;
            const int S = 64;
            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[S * S];
            float half = S * 0.5f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                    float a = Mathf.Clamp01((1f - d) * half * 0.5f);            // 바깥 가장자리 부드럽게
                    float ink = Mathf.Clamp01((d - 0.6f) * half * 0.5f);         // 0.6 바깥은 검은 테두리 (1.1.9: 0.7 → 0.6, 밝은 모래에서도 윤곽)
                    byte g = (byte)(255f * (1f - ink));
                    px[y * S + x] = new Color32(g, g, g, (byte)(255f * a));
                }
            tex.SetPixels32(px);
            tex.Apply();
            orbSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
            return orbSprite;
        }
    }

    void Update()
    {
        if (Time.frameCount == spawnFrame) return;
        // 시간 왜곡에 같이 느려짐
        transform.position += (Vector3)(dir * speed * EnermyController.GlobalSpeedMultiplier * Time.deltaTime);
        if (spin) transform.Rotate(0f, 0f, 720f * Time.deltaTime);
        age += Time.deltaTime;
        if (orb != null) orb.localScale = Vector3.one * (orbSize / transform.localScale.x) * (1f + 0.12f * Mathf.Sin(age * 16f));

        // 색깔 꼬리
        trail += Time.deltaTime;
        if (!fiery && !noTrail && body != null && Hostile.Glow != null && trail >= 0.05f)
        {
            trail = 0f;
            // 1.2.4: 화면 밖 탄은 꼬리를 만들지 않음 (꼬리는 탄 자리에 탄보다 작게 생겨 그 자리에서 사라지므로 보이는 것은 같음)
            if (body.isVisible)
            {
                Color c = body.color;
                FadeSprite.Spawn("Trail", Hostile.Glow, transform.position, transform.localScale.y * 0.6f, new Color(c.r, c.g, c.b, 0.75f), "Effect", 8, 0.25f);
            }
        }

        if (fiery && Hostile.Glow != null)
        {
            puff += Time.deltaTime;
            if (puff >= 0.04f)
            {
                puff = 0f;
                FlameParticle.Spawn(Hostile.Glow, transform.position, -dir * 2f + Random.insideUnitCircle, 0.3f, 0.04f, 0.18f, false);
            }
        }

        PlayerController p = Hostile.Player;
        AttackLabel.Pending = label;
        bool hitPlayer = p != null && Vector2.Distance(transform.position, p.HurtCenter) < radius + PlayerController.HurtRadius && p.TryHit(damage);     // 1.4.4~ 몸 아래쪽 판정
        AttackLabel.Pending = null;
        if (hitPlayer)
        {
            Hostile.Burst(transform.position, 1f, body != null ? body.color : GetComponent<SpriteRenderer>().color);
            Release();
            return;
        }

        life -= Time.deltaTime;
        if (life <= 0f || (!pierceWalls && Hostile.IsWall(transform.position))) Release();
    }
}
