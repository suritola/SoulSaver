using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 보스 스킬: 스킬을 번갈아 쓰고, 체력이 절반 아래면 특수 스킬도 씀
// 2.2.1~ 1 · 2 · 3장 보스(킹 슬라임 · 리치 왕 · 지옥의 군주)의 패턴 · 단계는 BossSkills.Reborn.cs, 여기엔 거울의 군주와 함께 쓰는 패턴만
// 모든 공격은 경고를 먼저 보여줘서 움직이면 피할 수 있음
// kind 0 = 리치 왕 (2장 지하 묘역), 1 = 지옥의 군주 (3장 불타는 지옥), 2 = 킹 슬라임 (1장 초원), 3 = 거울의 군주 (4장 · 2.1.1) — 2.1.2~ 장 순서 (Chapters)
public partial class BossSkills : MonoBehaviour
{
    public int kind;
    // 킹 슬라임 대점프로 공중에 떠 있는 동안 · 떨어질 자리
    [HideInInspector] public bool airborne;
    [HideInInspector] public Vector3 landing;

    // 보스 공격 피해 배율 (체력을 줄인 대신 공격을 세게) · 1.0.1: 3장부터 장마다 더 세게 (Chapters.BossPowerMul)
    static float Power => 1.25f * Chapters.BossPowerMul;

    static readonly Color Soul = new Color(0.55f, 0.95f, 1f, 0.95f);
    static readonly Color Curse = new Color(0.75f, 0.4f, 1f, 0.9f);
    static readonly Color Fire = new Color(1f, 0.45f, 0.12f, 0.95f);
    static readonly Color Danger = new Color(1f, 0.25f, 0.2f, 0.9f);

    bosss boss;
    SpriteRenderer sr;
    float next;
    float nextSpecial;
    int step;
    bool busy;

    bool Alive => boss != null && !boss.IsDead;

    // 스킬이 만든 오래 가는 표시 (룬 고리 · 등불 · 불의 고리 · 빔 선)
    // 보스가 쓰러져 1초 뒤 오브젝트가 지워지면 스킬 코루틴이 중간에 끊겨 정리 코드가 돌지 않으므로, 꺼질 때 한꺼번에 지움
    readonly List<GameObject> owned = new List<GameObject>();
    T Own<T>(T c) where T : Component { if (c != null) owned.Add(c.gameObject); return c; }
    GameObject Own(GameObject g) { if (g != null) owned.Add(g); return g; }

    void OnDisable()
    {
        foreach (GameObject g in owned) if (g != null) Destroy(g);
        owned.Clear();
        OwnOnDisable();         // 개발자 · 엄마 고유 공격 상태 (ownRoot 는 위에서 지워짐 · BossSkills.OwnKit)
    }

    void Start()
    {
        boss = GetComponent<bosss>();
        sr = GetComponent<SpriteRenderer>();
        // 개발자 · 엄마는 첫 고유 공격(1.6 · 1.2초)이 먼저 읽히게 첫 영웅 필살기를 4.5초 뒤로 (BossSkills.OwnKit)
        next = Time.time + (boss != null && boss.developer ? HeroFirstFinal : 3f);
    }

    void Update()
    {
        // 1.2.7~ 안전장치: 무적 · 공중 · 휘청 배율은 스킬이 도는 동안에만 (스킬이 어떤 길로 끊겨도 남지 않게 · 개발자는 이 상태를 쓰지 않음)
        if (!busy && boss != null && !boss.developer && (boss.invulnerable || airborne || groundAt.HasValue || boss.damageTakenMul != 1f)) ClearSkillState();
        if (busy && boss != null && !boss.developer && Time.time - busySince > BusyWatchdog) CancelForErosion();
        if (Alive && kind != AbyssStage.MirrorKind) RebornTick();
        // 개발자 · 엄마 고유 공격 안전장치 (위 두 줄은 개발자에게 돌지 않음): 결계 · 쓰러짐에 끊기 · 감시 12초 · 메달 (안에서 try/catch)
        if (boss != null && boss.developer) OwnGuard();
        if (busy || !Alive || BossUltimate.Active || BossUltimate.DevHold || BossUltimate.ErosionPending) return;      // 필살기 결계 동안은 결계 공격만 · 개발자 · 엄마 결계 대기 중엔 새 필살기 없음
        PlayerController p = Hostile.Player;
        if (p == null) return;
        if (boss.developer) { DeveloperTurn(p); return; }      // 무한 모드 최종 보스 (BossSkills.Developer.cs)
        if (kind != AbyssStage.MirrorKind) { RebornTurn(p); return; }      // 1 · 2 · 3장 보스 (BossSkills.Reborn.cs)

        // 체력 절반 아래: 특수 스킬 (처음 한 번은 바로)
        if (boss.Enraged && Time.time >= nextSpecial)
        {
            nextSpecial = Time.time + 12f * GameMode.SkillCooldownMul * Chapters.BossSkillCooldownMul;
            // 되비친 기억: 이번 판에 가장 많이 쓴 공격 (RunStats)
            string top = TopSource();
            if (StageManager.Instance != null)
                Notice.Big(MirrorLine(RunStats.SourceName(top)), NoticeIcon.Boss, 2.2f, NoticeLevel.Urgent);
            StartCoroutine(Run(ReflectedMemory(p, top)));
            return;
        }

        if (Time.time < next) return;
        next = Time.time + (boss.Enraged ? 2.6f : 3.6f) * GameMode.SkillCooldownMul * Chapters.BossSkillCooldownMul;     // 앞쪽 장 보스는 드물게 (2.1.5)
        step = (step + 1) % 8;     // 배운 스킬 흉내 두 번 · 필살기 흉내 한 번이 섞임
        IEnumerator skill = step == 0 ? MirrorStrike(3) : step == 1 ? MirrorSkill(true, true, false, true) : step == 2 ? MirrorClones(p) : step == 3 ? BrandCross(p)
            : step == 4 ? MirrorSkill(true, true, boss.Enraged, true) : step == 5 ? ShardRings() : step == 6 ? MirrorStrike(boss.Enraged ? 5 : 4) : MirrorUlt(boss.Enraged, true);
        StartCoroutine(Run(skill));
    }

    IEnumerator Run(IEnumerator routine)
    {
        busy = true;
        busySince = Time.time;
        boss.casting = true;
        yield return StartCoroutine(routine);
        if (boss != null) boss.casting = false;
        if (sr != null && Alive) sr.color = Color.white;
        owned.RemoveAll(g => g == null);
        busy = false;
        next = Mathf.Max(next, Time.time + 1.2f);
    }

    // 1.2.5~ 공간 침식이 대기 3초를 넘겨 시작될 때 돌던 스킬을 끊음 (개발자 · 엄마는 따로 · SpaceErosion.Run)
    // 끊긴 스킬은 침식 피해 배율 · 한 대 상한 밖이고, 돌진 · 점프가 domainHold 와 상관없이 몸을 옮기므로
    // 스킬이 남기는 상태(시전 중 · 공중 · 몸 색 · 기울기 · 받는 피해 배율)를 되돌리고 스킬이 만든 오브젝트를 지움
    public void CancelForErosion()
    {
        if (boss == null || boss.developer) return;
        // 1.2.7~ 스킬이 남기는 상태는 스킬이 돌지 않아도 늘 되돌림 (예전엔 busy 일 때만이라, 상태만 남은 보스는 그대로였음)
        ClearSkillState();
        if (!busy) return;
        StopAllCoroutines();
        busy = false;
        boss.casting = false;
        transform.rotation = Quaternion.identity;
        if (sr != null && Alive) sr.color = Color.white;
        foreach (GameObject g in owned) if (g != null) Destroy(g);
        owned.Clear();
        next = Mathf.Max(next, Time.time + 1.2f);
    }

    // 1.2.7~ 끊긴 스킬이 남긴 상태 되돌리기
    //   리치 「망자의 의식」 도중에 끊기면 무적이 풀리지 않아 보스가 영원히 무적 · 공간 침식이 영원히 대기 · 15분 최후의 시간도 열리지 않았음
    //   → 무적을 풀고 의식은 나중에 다시 · 대점프 공중에서 끊기면 14칸 위에 떠 있지 않게 떨어질 곳에 내려놓음 · 받는 피해 배율(휘청 1.5배)도 1로
    void ClearSkillState()
    {
        boss.invulnerable = false;
        if (ritualRunning) { ritualRunning = false; specialDone = false; ritualRetryAt = Time.time + RitualRetryDelay; }
        if (airborne) { transform.position = landing; airborne = false; }
        if (groundAt.HasValue) { transform.position = groundAt.Value; groundAt = null; }     // 지옥의 군주 내려찍기 · 공중 탄막 · 리치 의식
        boss.damageTakenMul = 1f;
    }

    // 1.2.7~ 스킬 감시: 스킬 코루틴이 예외로 죽으면 Run 이 끝나지 않아 busy · casting · 무적이 영원히 남음 (안전장치는 !busy 일 때만 봄)
    //   가장 긴 스킬은 의식 22초 + 휘청 4초 → BusyWatchdog 초가 넘게 도는 스킬은 끊고 정리
    const float BusyWatchdog = 40f;
    float busySince;

    // 1.2.6~ 한국어면 이름 받침에 맞춰 「을」 · 「를」 (예전엔 「을(를)」이 그대로 보였음) · 다른 언어는 번역 그대로
    static string MirrorLine(string name)
    {
        string line = Loc.T("거울의 군주가 「{0}」을(를) 되비춘다!").Replace("{0}", name);
        if (Loc.Current != Loc.Lang.Korean || string.IsNullOrEmpty(name)) return line;
        char last = name[name.Length - 1];
        if (last < '가' || last > '힣') return line;      // 한글 음절이 아니면 그대로
        return line.Replace("을(를)", (last - 0xAC00) % 28 != 0 ? "을" : "를");
    }

    IEnumerator Windup(Color color, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            sr.color = Color.Lerp(boss.baseTint, color, Mathf.PingPong(Time.time * 6f, 1f));     // 1.0.4~ 몸 색(개발자 엄마 분홍)에서 깜빡임
            yield return null;
        }
        if (Alive) sr.color = Color.white;
    }

    Vector2 DirTo(PlayerController p) => ((Vector2)(p.transform.position - transform.position)).normalized;

    // ================================================================= 거울의 군주 (4장 · 2.1.1)
    static readonly Color Mirror = new Color(0.78f, 0.9f, 1f, 0.95f);

    // 거울의 군주의 모든 공격 한 대는 최대 체력의 MaxHitShare 를 넘지 않음 (2.1.8, 흉내 공격과 같은 규칙 · 즉사 없음)
    float Capped(float raw)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) return raw;
        float scale = Mathf.Max(0.01f, GameMode.DamageMul * Chapters.HurtMul);     // TryHit 이 곱하는 배율
        return Mathf.Min(raw, pl.PlayerMaxHealth * MaxHitShare / scale);
    }

    static Vector2 Rotate(Vector2 v, float deg)
    {
        float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    // 거울 공격: 지금 캐릭터의 평타를 흉내 냄 (거너 연사 · 검사 돌진 베기 · 도적 표창 부채 · 궁수 관통 화살 · 연금술사 플라스크)
    IEnumerator MirrorStrike(int rounds)
    {
        CharacterId who = CharacterData.Selected;
        yield return Windup(Mirror, 0.5f);
        for (int r = 0; r < rounds && Alive; r++)
        {
            PlayerController p = Hostile.Player;
            if (p == null) yield break;
            Vector2 d = DirTo(p);
            Vector3 c = transform.position;
            switch (who)
            {
                case CharacterId.Swordsman:
                    {
                        Vector3 at = Hostile.ClampArena(c + (Vector3)(d * 4.5f));
                        if (Hostile.IsWall(at)) at = c;
                        Hostile.Circle(at, 3.6f, 0.6f, Mirror);
                        yield return new WaitForSeconds(0.6f);
                        if (!Alive) yield break;
                        transform.position = at;
                        Fx.Spawn("fx_slash", at, 8f, new Color(0.85f, 0.95f, 1f), 30f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 14);
                        Hostile.HitCircle(at, 3.6f, Capped(22f * Power));
                        Hostile.Play("whoosh", 0.7f, 0.8f);
                        break;
                    }
                case CharacterId.Rogue:
                    for (int i = -3; i <= 3; i++) Hostile.Shoot(c, Rotate(d, i * 12f), 12f, Capped(14f * Power), 0.5f, Mirror, 1.3f, 3f);
                    Hostile.Play("whoosh", 0.5f, 1.4f);
                    yield return new WaitForSeconds(0.55f);
                    break;
                case CharacterId.Archer:
                    {
                        Hostile.Line(c, c + (Vector3)(d * 30f), 1.2f, 0.65f, Mirror);
                        yield return new WaitForSeconds(0.65f);
                        if (!Alive) yield break;
                        Hostile.Shoot(transform.position, d, 28f, Capped(24f * Power), 0.7f, Mirror, 2f, 2f);
                        Hostile.Play("zap", 0.5f, 1.3f);
                        yield return new WaitForSeconds(0.2f);
                        break;
                    }
                case CharacterId.Alchemist:
                    {
                        Vector3 at = Hostile.ClampArena(p.transform.position + (Vector3)(Random.insideUnitCircle * 2f));
                        Hostile.Circle(at, 2.6f, 0.8f, Mirror);
                        yield return new WaitForSeconds(0.8f);
                        if (!Alive) yield break;
                        Fx.Spawn("fx_explosion", at, 5.5f, Mirror, 18f);
                        Hostile.HitCircle(at, 2.6f, Capped(20f * Power));
                        HazardZone.Spawn(at, 2.2f, 3f, Capped(6f * Power), new Color(0.7f, 0.85f, 1f, 0.7f), "fx_puddle", 0.7f);
                        Hostile.Play("boom", 0.5f, 1.2f);
                        break;
                    }
                case CharacterId.Cartographer:
                    {
                        // 지도 제작자: 잉크 획 (경고 선 뒤 굵은 잉크 탄)
                        Hostile.Line(c, c + (Vector3)(d * 14f), 1.4f, 0.55f, Mirror);
                        yield return new WaitForSeconds(0.55f);
                        if (!Alive) yield break;
                        Hostile.Shoot(transform.position, d, 30f, Capped(20f * Power), 0.8f, Mirror, 2f, 0.6f);
                        Hostile.Play("whoosh", 0.5f, 1.6f);
                        break;
                    }
                case CharacterId.Ghost:
                    {
                        // 유령: 차가운 손길 (짧은 경고 뒤 앞으로 뻗음)
                        Vector3 end = c + (Vector3)(d * 7f);
                        Hostile.Line(c, end, 1.6f, 0.5f, Mirror);
                        yield return new WaitForSeconds(0.5f);
                        if (!Alive) yield break;
                        Fx.Beam(transform.position, end, 1.2f, new Color(0.7f, 0.95f, 1f, 0.8f), 0.2f);
                        PlayerController pp = Hostile.Player;
                        if (pp != null && Hostile.HurtToSegment(pp, transform.position, end) < 0.9f) pp.TryHit(Capped(18f * Power));
                        Hostile.Play("whoosh", 0.5f, 0.7f);
                        break;
                    }
                case CharacterId.Postman:
                    // 우체부: 플레이어를 쫓는 편지 넷 (느리게 꺾임)
                    for (int i = 0; i < 4; i++) Hostile.Shoot(c, Rotate(d, (i - 1.5f) * 22f), 10f, Capped(13f * Power), 0.5f, Mirror, 1.3f, 3f);
                    Hostile.Play("pop", 0.5f, 1.5f);
                    yield return new WaitForSeconds(0.4f);
                    break;
                case CharacterId.Clockmaker:
                    // 시계공: 튕기는 톱니 셋
                    for (int i = -1; i <= 1; i++) Hostile.Shoot(c, Rotate(d, i * 18f), 14f, Capped(15f * Power), 0.6f, Mirror, 1.6f, 3f);
                    Hostile.Play("clank", 0.5f, 1.3f);
                    yield return new WaitForSeconds(0.45f);
                    break;
                default:    // 거너: 세 발 연사
                    for (int i = 0; i < 3 && Alive; i++)
                    {
                        Hostile.Shoot(transform.position, DirTo(p), 18f, Capped(15f * Power), 0.5f, Mirror, 1.4f, 3f);
                        Hostile.Play("zap", 0.35f, 1.6f);
                        yield return new WaitForSeconds(0.12f);
                    }
                    break;
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    // 분신술: 플레이어 둘레 세 자리에 거울상이 나타나고 그중 진짜(보스)만 맞음. 모두 거울 조각을 쏜 뒤 가짜는 깨짐
    IEnumerator MirrorClones(PlayerController p)
    {
        Vector3 center = p.transform.position;
        float start = Random.Range(0f, 360f);
        int real = Random.Range(0, 3);
        List<GameObject> fakes = new List<GameObject>();
        Vector3[] spots = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            float a = (start + i * 120f) * Mathf.Deg2Rad;
            spots[i] = Hostile.ClampArena(center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 8f);
            if (Hostile.IsWall(spots[i])) spots[i] = Hostile.ClampArena(center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 4f);
        }
        Fx.Spawn("fx_soulburst", transform.position, 9f, Mirror, 16f);
        Hostile.Play("shimmer", 0.6f, 0.8f);
        for (int i = 0; i < 3; i++)
        {
            Fx.Spawn("fx_shock", spots[i], 6f, Mirror, 20f);
            if (i == real) { transform.position = spots[i]; continue; }
            GameObject g = Own(new GameObject("MirrorClone"));
            g.transform.position = spots[i];
            g.transform.localScale = transform.localScale;
            SpriteRenderer s = g.AddComponent<SpriteRenderer>();
            s.sprite = sr.sprite;
            s.sortingLayerID = sr.sortingLayerID;
            s.sortingOrder = sr.sortingOrder;
            s.flipX = spots[i].x > center.x;
            s.color = new Color(0.85f, 0.92f, 1f, 0.9f);
            AbyssBody body = g.AddComponent<AbyssBody>();
            body.frames = AbyssStage.Frames("mirrorlord");
            fakes.Add(g);
        }
        yield return Windup(Mirror, 0.9f);
        for (int i = 0; i < 3; i++)
        {
            Vector3 from = i == real ? transform.position : spots[i];
            PlayerController pl = Hostile.Player;
            if (pl == null) break;
            Vector2 d = ((Vector2)(pl.transform.position - from)).normalized;
            for (int k = -1; k <= 1; k++) Hostile.Shoot(from, Rotate(d, k * 14f), 11f, Capped(15f * Power), 0.5f, Mirror, 1.4f, 3f);
        }
        Hostile.Play("zap", 0.5f, 1.2f);
        yield return new WaitForSeconds(2.2f);
        foreach (GameObject g in fakes)
        {
            if (g == null) continue;
            Fx.Spawn("fx_spark", g.transform.position, 5f, Mirror, 18f);
            Destroy(g);
        }
        Hostile.Play("crack", 0.5f, 1.4f);
    }

    // 낙인: 플레이어 곁으로 순간이동한 뒤 자기 자리에 십자 광선
    IEnumerator BrandCross(PlayerController p)
    {
        Vector3 dest = Hostile.ClampArena(p.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 5f));
        if (Hostile.IsWall(dest)) dest = transform.position;
        Hostile.Circle(dest, 2.5f, 0.6f, Mirror);
        Fx.Spawn("fx_soulburst", transform.position, 8f, Mirror, 16f);
        for (float t = 0f; t < 0.6f && Alive; t += Time.deltaTime)
        {
            sr.color = new Color(1f, 1f, 1f, 1f - t / 0.6f);
            yield return null;
        }
        if (!Alive) yield break;
        transform.position = dest;
        sr.color = Color.white;
        Fx.Spawn("fx_shock", dest, 7f, Mirror, 20f);

        const float half = 16f;
        int arms = boss.Enraged ? 2 : 1;
        for (int n = 0; n < arms && Alive; n++)
        {
            float tilt = n * 45f;
            Vector3 c = transform.position;
            Vector3[] ends = new Vector3[4];
            for (int i = 0; i < 2; i++)
            {
                float a = (tilt + i * 90f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * half;
                ends[i * 2] = c - d;
                ends[i * 2 + 1] = c + d;
                Hostile.Line(c - d, c + d, 1.8f, 0.9f, Mirror);
            }
            yield return Windup(Mirror, 0.9f);
            if (!Alive) yield break;
            for (int i = 0; i < 2; i++) Fx.Beam(ends[i * 2], ends[i * 2 + 1], 1.6f, new Color(0.8f, 0.92f, 1f), 0.35f);
            PlayerController pl = Hostile.Player;
            if (pl != null)
                for (int i = 0; i < 2; i++)
                    if (Hostile.HurtToSegment(pl, ends[i * 2], ends[i * 2 + 1]) < 1f) { pl.TryHit(Capped(24f * Power)); break; }
            Hostile.Play("zap", 0.7f, 0.8f);
            Hostile.Shake(0.15f);
        }
    }

    // 거울 조각 고리: 세 겹으로 엇갈려 퍼지는 조각 (틈으로 피함)
    IEnumerator ShardRings()
    {
        yield return Windup(Mirror, 0.6f);
        for (int w = 0; w < 3 && Alive; w++)
        {
            const int n = 14;
            float off = w * (180f / n);
            for (int i = 0; i < n; i++)
            {
                float a = (off + i * 360f / n) * Mathf.Deg2Rad;
                Hostile.Shoot(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 7.5f, Capped(14f * Power), 0.5f, Mirror, 1.5f, 4f);
            }
            Hostile.Play("shimmer", 0.4f, 1.3f + w * 0.1f);
            yield return new WaitForSeconds(0.55f);
        }
    }

    // ---------------------------------------------------------------- 필살기 · 스킬 흉내 (2.1.4)
    // 지금 캐릭터의 우클릭 필살기를 되비춤 (거너 조준 사격 · 검사 회전 베기 · 도적 출혈 돌진 · 궁수 화살비 · 연금술사 대폭발)
    // 모든 판정은 경고를 먼저 보여 주고 움직이면 피할 수 있음. 한 번 쓸 때 최대 UltMaxHits 번만 맞아서
    // 판정이 겹치거나 가만히 서 있어도 한꺼번에 큰 피해를 받지 않음 (한 대 16 × Power = 20, 최대 40)
    // 한 대는 어떤 난이도 · 장에서도 최대 체력의 MaxHitShare 를 넘지 않음 (즉사 없음)
    const int UltMaxHits = 2;
    const float UltHitDamage = 16f;
    const float MaxHitShare = 0.2f;      // 1.0.1: 0.15 → 0.2 (4장 보스를 세게 올린 만큼)
    int ultHits;

    bool UltHit(bool inside)
    {
        PlayerController pl = Hostile.Player;
        if (!inside || pl == null || pl.IsDying || ultHits >= (boss != null && boss.developer ? DevMaxHits : UltMaxHits)) return false;
        float scale = Mathf.Max(0.01f, GameMode.DamageMul * Chapters.HurtMul);     // TryHit 이 곱하는 배율
        // 1.0.4~ 개발자 · 개발자 엄마: 받은 피해 · 쓰러진 곳에 보스 이름 · 엄마는 피해 2배 (BossSkills.Developer)
        bool dev = boss != null && boss.developer;
        if (dev) AttackLabel.Pending = DevName;
        bool hit = pl.TryHit(Mathf.Min(UltHitDamage * Power, pl.PlayerMaxHealth * MaxHitShare / scale) * (dev ? DevPow : 1f));
        if (dev) AttackLabel.Pending = null;
        if (!hit) return false;
        ultHits++;
        return true;
    }

    bool UltInCircle(Vector3 at, float radius)
    {
        PlayerController pl = Hostile.Player;
        return pl != null && Hostile.HurtDistance(pl, at) < radius + 0.2f;      // 1.4.4~ 몸 아래쪽 판정
    }

    // strong: 체력 절반 아래 · 되비친 기억이면 한 번 더 · banner: 따로 알림을 띄우지 않았을 때만
    IEnumerator MirrorUlt(bool strong, bool banner)
    {
        ultHits = 0;
        if (banner) Notice.Big(Loc.T("거울의 군주가 필살기를 흉내 낸다!"), NoticeIcon.Boss, 1.8f, NoticeLevel.Urgent);
        Fx.Spawn("fx_soulburst", transform.position, 9f, Mirror, 16f);
        Hostile.Play("shimmer", 0.6f, 0.7f);
        yield return Windup(Mirror, 0.8f);
        if (!Alive) yield break;
        switch (CharacterData.Selected)
        {
            case CharacterId.Swordsman: yield return UltSpin(strong ? 3 : 2); break;
            case CharacterId.Rogue: yield return UltDash(strong ? 4 : 3); break;
            case CharacterId.Archer: yield return UltRain(strong ? 3 : 2); break;
            case CharacterId.Alchemist: yield return UltBlast(strong ? 2 : 1); break;
            case CharacterId.Cartographer: yield return UltFold(strong ? 3 : 2); break;
            case CharacterId.Clockmaker: yield return UltClock(strong ? 5 : 4); break;
            case CharacterId.Postman: yield return UltExpress(strong ? 10 : 7); break;
            case CharacterId.Ghost: yield return UltPossess(strong ? 3 : 2); break;
            default: yield return UltLockOn(strong ? 6 : 4); break;
        }
    }

    // 검사 · 회전 베기: 플레이어 쪽으로 다가선 자리에 둥근 경고, 1초 뒤 둘레를 벰
    IEnumerator UltSpin(int spins)
    {
        const float r = 4f;
        for (int n = 0; n < spins && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            float gap = Vector2.Distance(transform.position, pl.transform.position);
            Vector3 at = Hostile.ClampArena(transform.position + (Vector3)(DirTo(pl) * Mathf.Min(3.5f, gap)));
            if (Hostile.IsWall(at)) at = transform.position;
            Hostile.Circle(at, r, 1f, Mirror);
            yield return new WaitForSeconds(1f);
            if (!Alive) yield break;
            transform.position = at;
            Fx.Spawn("fx_slash", at, 10f, new Color(0.85f, 0.95f, 1f), 30f, 0f, 14);
            Fx.Spawn("fx_slash", at, 10f, new Color(0.85f, 0.95f, 1f), 30f, 180f, 14);
            UltHit(UltInCircle(at, r));
            Hostile.Play("whoosh", 0.8f, 0.7f);
            Hostile.Shake(0.15f);
            yield return new WaitForSeconds(0.35f);
        }
    }

    // 도적 · 출혈 돌진: 돌진 길을 먼저 그은 뒤 그 길을 꿰뚫음
    IEnumerator UltDash(int dashes)
    {
        for (int n = 0; n < dashes && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector2 d = DirTo(pl);
            Vector3 from = transform.position;
            Vector3 to = from;
            foreach (float len in new[] { 11f, 7f, 4f })
            {
                Vector3 end = Hostile.ClampArena(from + (Vector3)(d * len));
                if (!Hostile.IsWall(end)) { to = end; break; }
            }
            Hostile.Line(from, to, 2f, 0.75f, Mirror);
            yield return Windup(Mirror, 0.75f);
            if (!Alive) yield break;
            for (float t = 0f; t < 0.15f && Alive; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(from, to, t / 0.15f);
                yield return null;
            }
            if (!Alive) yield break;
            transform.position = to;
            Fx.Spawn("fx_slash", to, 7f, new Color(0.85f, 0.95f, 1f), 30f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 14);
            pl = Hostile.Player;
            UltHit(pl != null && Hostile.HurtToSegment(pl, from, to) < 1.2f);
            Hostile.Play("whoosh", 0.7f, 1.2f);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // 궁수 · 화살비: 플레이어 자리와 그 둘레에 화살이 떨어질 자리 (사이 틈으로 피함)
    IEnumerator UltRain(int waves)
    {
        const float r = 2.2f;
        const int n = 5;
        Vector3[] spots = new Vector3[n];
        for (int w = 0; w < waves && Alive; w++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 c = pl.transform.position;
            float start = Random.Range(0f, 360f);
            for (int i = 0; i < n; i++)
            {
                float a = (start + i * 360f / (n - 1)) * Mathf.Deg2Rad;
                spots[i] = i == 0 ? c : Hostile.ClampArena(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(3.5f, 5f));
                Hostile.Circle(spots[i], r, 1f, Mirror);
            }
            Hostile.Play("whoosh", 0.5f, 1.5f);
            yield return new WaitForSeconds(1f);
            if (!Alive) yield break;
            bool inside = false;
            for (int i = 0; i < n; i++)
            {
                Fx.Spawn("fx_spark", spots[i], 5f, Mirror, 20f);
                inside |= UltInCircle(spots[i], r);
            }
            UltHit(inside);
            Hostile.Play("zap", 0.6f, 1.1f);
            yield return new WaitForSeconds(0.35f);
        }
    }

    // 연금술사 · 대폭발: 플레이어 자리에 커다란 경고, 오래 기다린 뒤 터짐 (밖으로 나가면 피함)
    IEnumerator UltBlast(int blasts)
    {
        for (int n = 0; n < blasts && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            float r = n == 0 ? 4.5f : 3.5f;
            Vector3 at = pl.transform.position;
            Hostile.Circle(at, r, 1.3f, Mirror);
            yield return Windup(Mirror, 1.3f);
            if (!Alive) yield break;
            Fx.Spawn("fx_explosion", at, r * 2.2f, Mirror, 18f);
            UltHit(UltInCircle(at, r));
            Hostile.Play("boom", 0.8f, 0.9f);
            Hostile.Shake(0.25f);
            yield return new WaitForSeconds(0.4f);
        }
    }

    // 거너 · 조준 사격: 플레이어 자리에 차례로 조준 표식, 1초 뒤 표식마다 한 발씩 (계속 움직이면 피함)
    IEnumerator UltLockOn(int marks)
    {
        for (int i = 0; i < marks && Alive; i++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) break;
            Vector3 at = pl.transform.position;
            Hostile.Circle(at, 1.5f, 1f, Mirror);
            Hostile.Play("zap", 0.3f, 2f);
            StartCoroutine(LockShot(at));
            yield return new WaitForSeconds(0.3f);
        }
        yield return new WaitForSeconds(1.1f);
    }

    IEnumerator LockShot(Vector3 at)
    {
        yield return new WaitForSeconds(1f);
        if (!Alive) yield break;
        Fx.Beam(transform.position, at, 0.6f, new Color(0.85f, 0.95f, 1f), 0.2f);
        Fx.Spawn("fx_spark", at, 4f, Mirror, 20f);
        UltHit(UltInCircle(at, 1.5f));
        Hostile.Play("zap", 0.5f, 1.4f);
    }

    // ---------------------------------------------------------------- 레벨업 스킬 · 특수 능력 흉내 (2.1.4)
    // 플레이어가 이번 판에 배운 스킬(캐릭터 전용 카드 · 고유 레벨업 스킬)과 고른 특수 능력 중 하나를 골라
    // 그 스킬 이름을 띄우고 닮은 공격으로 되비춤. 스킬마다 아래 모양 중 하나 (판정은 모두 UltHit 으로 횟수 · 피해 제한)
    enum Pat { None, Mines, Echo, Volley, Lines, Trail, Rain, Blast, Pull, Ring, Clones }

    // 고유 레벨업 스킬 10종 (LevelShop.SigKeys 순서) · 캐릭터 순서 = CharacterId
    static readonly Pat[][] SigPats =
    {
        // 거너: 탄피 지뢰 · 처치 파편 · 총열 과열 · 영혼 탄환 · 섬광 장전 · 반격 사격 · 속사 장전 · 달리며 장전 · 전리품 탄약 · 위협 사격
        new[] { Pat.Mines, Pat.Ring, Pat.Volley, Pat.Lines, Pat.Blast, Pat.Volley, Pat.Volley, Pat.Trail, Pat.Mines, Pat.Pull },
        // 검사: 역습 · 검의 궤적 · 혈갑 · 회전 가속 · 강철 의지 · 검무 · 균열 · 거인 사냥꾼 · 잔향 베기 · 영혼 흡수
        new[] { Pat.Echo, Pat.Trail, Pat.Blast, Pat.Blast, Pat.Pull, Pat.Trail, Pat.Echo, Pat.Blast, Pat.Echo, Pat.Volley },
        // 도적: 연막 · 피의 향연 · 출혈 폭발 · 급습 표창 · 회전 표창 · 궤적 칼날 · 던지기 연습 · 넘치는 기세 · 그림자 매듭 · 빈 주머니의 비
        new[] { Pat.Mines, Pat.Echo, Pat.Mines, Pat.Lines, Pat.Lines, Pat.Trail, Pat.Volley, Pat.Ring, Pat.Lines, Pat.Rain },
        // 궁수: 바람 읽기 · 집중 호흡 · 가시 씨앗 · 울림 화살촉 · 표식 전염 · 꿰뚫는 시선 · 후퇴 사격 · 매복 · 높은 자리 · 유성 화살
        new[] { Pat.Lines, Pat.Volley, Pat.Mines, Pat.Echo, Pat.Echo, Pat.Lines, Pat.Trail, Pat.Mines, Pat.Rain, Pat.Rain },
        // 연금술사: 인화성 기체 · 서리 결정 · 촉매 반응 · 증폭 용액 · 조수 개조 · 유리 비 · 응급 연고 · 폭발 정제 · 연소 흔적 · 시약 농축
        new[] { Pat.Mines, Pat.Ring, Pat.Echo, Pat.Blast, Pat.Volley, Pat.Rain, Pat.Mines, Pat.Blast, Pat.Trail, Pat.Blast },
        // 지도 제작자: 범례 · 등고선 · 축척 · 북극성 · 여백 · 지름길 · 지우개 · 보물 지도 · 나침도 · 잉크 폭풍
        new[] { Pat.Blast, Pat.Ring, Pat.Lines, Pat.Volley, Pat.Lines, Pat.Trail, Pat.Pull, Pat.Mines, Pat.Ring, Pat.Rain },
        // 시계공: 과회전 · 자명종 · 맞물린 톱니 · 회중시계 · 분침 · 시간의 메아리 · 태엽 감기 · 시간 고리 · 빌린 시간 · 스톱워치
        new[] { Pat.Volley, Pat.Ring, Pat.Lines, Pat.Pull, Pat.Blast, Pat.Echo, Pat.Mines, Pat.Ring, Pat.Volley, Pat.Echo },
        // 우체부: 특급 · 소인 · 우편 가방 · 행운의 편지 · 배달 경로 · 영수증 · 궂은 날씨 · 익일 배송 · 정확한 주소 · 광고 전단
        new[] { Pat.Volley, Pat.Echo, Pat.Trail, Pat.Volley, Pat.Lines, Pat.Ring, Pat.Blast, Pat.Mines, Pat.Lines, Pat.Volley },
        // 유령: 원한 · 곡소리 · 한기 · 생기 흡수 · 실체 없음 · 도깨비불 · 저주 · 폴터가이스트 · 동상 · 망령의 행렬
        new[] { Pat.Lines, Pat.Ring, Pat.Mines, Pat.Pull, Pat.Trail, Pat.Volley, Pat.Blast, Pat.Rain, Pat.Mines, Pat.Clones },
    };

    // 캐릭터 전용 레벨업 카드 7장 (LevelShop.KitIds 순서: 관통 · 멀티 샷 · 밀어내기 · 노려보는 눈빛 · 사냥의 기세 · 추가 A · 추가 B 자리)
    static readonly Pat[][] KitPats =
    {
        new[] { Pat.Lines, Pat.Echo, Pat.Mines, Pat.Volley, Pat.None, Pat.Lines, Pat.Blast },       // 도탄 사격 · 폭발 탄두 · 소각탄 · 유도 탄두 · - · 전기탄 · 장전 충격파
        new[] { Pat.Lines, Pat.Echo, Pat.Blast, Pat.Ring, Pat.None, Pat.Blast, Pat.Echo },         // 날아가는 검기 · 흡혈 베기 · 쳐내기 · 칼바람 · - · 굳건한 자세 · 연속 베기
        new[] { Pat.Lines, Pat.Volley, Pat.Clones, Pat.Ring, Pat.Volley, Pat.Echo, Pat.Volley },   // 도탄 표창 · 갈고리 표창 · 그림자 분신 · 표창 폭풍 · 사냥의 기세 · 급소 노리기 · 표창 회수
        new[] { Pat.Lines, Pat.Lines, Pat.Blast, Pat.Mines, Pat.None, Pat.Volley, Pat.Echo },      // 분열 화살 · 메아리 화살 · 강풍 화살 · 가시 덤불 · - · 정조준 · 사냥감 표식
        new[] { Pat.Echo, Pat.Ring, Pat.Volley, Pat.Rain, Pat.None, Pat.Blast, Pat.Mines },        // 연쇄 반응 · 급속 냉동 · 호문쿨루스 · 파편 플라스크 · - · 원소 융합 · 끈적한 산성
        new[] { Pat.Mines, Pat.Lines, Pat.Volley, Pat.Trail, Pat.None, Pat.Echo, Pat.Ring },       // 번지는 잉크 · 굵은 펜촉 · 나침반 바늘 · 접힌 자국 · - · 측량 표식 · 끊기지 않는 선
        new[] { Pat.Lines, Pat.Echo, Pat.Blast, Pat.Mines, Pat.None, Pat.Ring, Pat.Volley },       // 튕기는 톱니 · 빠른 태엽 · 시계추 · 남은 시각 · - · 정각의 종 · 녹슨 톱니
        new[] { Pat.Lines, Pat.Blast, Pat.Volley, Pat.Rain, Pat.None, Pat.Lines, Pat.Echo },       // 등기 우편 · 소포 · 우표 수집 · 답장 · - · 빠른 우편 · 반송
        new[] { Pat.Lines, Pat.Mines, Pat.Echo, Pat.Ring, Pat.None, Pat.Volley, Pat.Blast },       // 꿰뚫는 냉기 · 얼어붙는 손길 · 원혼 · 빙의 여운 · - · 영혼 수호 · 얼음 파편
    };

    // 특수 능력 (SpecialAbilities 번호): 무기 0 ~ 7 · 스킬 8 ~ 15 · 패시브 16 ~ 19, 캐릭터 전용 특수 능력(20 ~)은 종류로
    static Pat SpecialPat(int id)
    {
        switch (id)
        {
            case SpecialAbilities.ShotgunId: case SpecialAbilities.SniperId: case SpecialAbilities.ChainId: case SpecialAbilities.ScytheId: return Pat.Lines;
            case SpecialAbilities.DualId: case SpecialAbilities.SeekerId: return Pat.Volley;
            case SpecialAbilities.FlameId: case SpecialAbilities.DashId: return Pat.Trail;
            case SpecialAbilities.GrenadeId: return Pat.Rain;
            case SpecialAbilities.FireZoneId: case SpecialAbilities.CurseId: return Pat.Mines;
            case SpecialAbilities.SkeletonsId: case SpecialAbilities.MirrorId: return Pat.Clones;
            case SpecialAbilities.HookId: return Pat.Pull;
            case SpecialAbilities.OrbsId: return Pat.Ring;
            case SpecialAbilities.UndyingId: return Pat.Echo;
            case SpecialAbilities.TimeWarpId: case SpecialAbilities.PactId: case SpecialAbilities.SoulBurstId: case SpecialAbilities.ThornsId: return Pat.Blast;
        }
        if (!SpecialAbilities.IsKit(id)) return Pat.Blast;
        switch (SpecialAbilities.KitKind(id))
        {
            case SpecialKind.Weapon: return id % 2 == 0 ? Pat.Lines : Pat.Echo;
            case SpecialKind.Skill: return id % 2 == 0 ? Pat.Rain : Pat.Mines;
            default: return Pat.Blast;
        }
    }

    struct Mimic
    {
        public string key;      // 피해 출처 키 (특수 능력 이름) · 카드는 null
        public string name;     // 띄울 이름 (번역됨)
        public Pat pat;
        public int weight;      // 단계가 높을수록 자주
    }

    // 지금 플레이어가 가진 스킬 (levelUp: 레벨업 카드 · 고유 스킬, special: 특수 능력)
    static List<Mimic> Mimics(bool levelUp, bool special)
    {
        List<Mimic> list = new List<Mimic>();
        CharacterId who = CharacterData.Selected;
        int c = Mathf.Clamp((int)who, 0, SigPats.Length - 1);
        LevelShop shop = levelUp ? FindFirstObjectByType<LevelShop>() : null;
        if (shop != null)
        {
            for (int i = 0; i < LevelShop.KitIds.Length && i < KitPats[c].Length; i++)
            {
                int id = LevelShop.KitIds[i], lv = shop.LevelOf(id);
                string n = LevelShop.CardName(who, id);
                if (lv > 0 && KitPats[c][i] != Pat.None && !string.IsNullOrEmpty(n)) list.Add(new Mimic { name = n, pat = KitPats[c][i], weight = lv });
            }
            for (int k = 0; k < LevelShop.SigCount && k < SigPats[c].Length; k++)
            {
                int id = LevelShop.SigFirstId + k, lv = shop.LevelOf(id);
                string n = LevelShop.CardName(who, id);
                if (lv > 0 && !string.IsNullOrEmpty(n)) list.Add(new Mimic { name = n, pat = SigPats[c][k], weight = lv + 1 });
            }
        }
        SpecialAbilities sp = special ? SpecialAbilities.SharedInstance : null;
        if (sp != null && sp.abilities != null)
            foreach (int id in sp.EquippedIds)
            {
                string raw = id >= 0 && id < sp.abilities.Length && sp.abilities[id] != null ? sp.abilities[id].name : SpecialAbilities.KitName(id);
                if (!string.IsNullOrEmpty(raw)) list.Add(new Mimic { key = raw, name = Loc.T(raw), pat = SpecialPat(id), weight = sp.IsEvolved(id) ? 4 : 3 });
            }
        return list;
    }

    static Mimic Pick(List<Mimic> list)
    {
        int total = 0;
        foreach (Mimic m in list) total += Mathf.Max(1, m.weight);
        int r = Random.Range(0, total);
        foreach (Mimic m in list)
        {
            r -= Mathf.Max(1, m.weight);
            if (r < 0) return m;
        }
        return list[list.Count - 1];
    }

    // 배운 스킬 하나를 골라 흉내 (배운 게 없으면 조각 고리)
    // strong: 한 번 더 · banner: 이름 알림 · top: 되비친 기억이면 가장 많이 쓴 출처 (특수 능력 이름과 맞으면 그것을)
    IEnumerator MirrorSkill(bool levelUp, bool special, bool strong, bool banner, string top = null)
    {
        List<Mimic> list = Mimics(levelUp, special);
        if (list.Count == 0) { yield return ShardRings(); yield break; }
        Mimic m = list.Find(x => top != null && x.key == top);
        if (m.name == null) m = Pick(list);
        if (banner && StageManager.Instance != null)
            Notice.Big(MirrorLine(m.name), NoticeIcon.Boss, 2f, NoticeLevel.Urgent);
        ultHits = 0;
        Fx.Spawn("fx_sparkle", transform.position, 5f, Mirror, 20f);
        Hostile.Play("shimmer", 0.5f, 1.1f);
        int times = strong ? 2 : 1;
        switch (m.pat)
        {
            case Pat.Mines: yield return SkMines(times + 1); break;
            case Pat.Echo: yield return SkEcho(times + 1); break;
            case Pat.Volley: yield return UltLockOn(strong ? 5 : 3); break;
            case Pat.Lines: yield return SkLines(times + 1); break;
            case Pat.Trail: yield return UltDash(times + 1); break;
            case Pat.Rain: yield return UltRain(times); break;
            case Pat.Pull: yield return SkPull(); break;
            case Pat.Ring: yield return SkRing(times + 1); break;
            case Pat.Clones: if (Hostile.Player != null) yield return MirrorClones(Hostile.Player); break;
            default: yield return SkBlast(times); break;
        }
    }

    // 지뢰 · 장판: 플레이어 자리와 둘레에 작은 원들 → 잠시 뒤 한꺼번에 터짐
    IEnumerator SkMines(int waves)
    {
        const float r = 1.6f;
        const int n = 6;
        Vector3[] spots = new Vector3[n];
        for (int w = 0; w < waves && Alive; w++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 c = pl.transform.position;
            for (int i = 0; i < n; i++)
            {
                spots[i] = i == 0 ? c : Hostile.ClampArena(c + (Vector3)(Random.insideUnitCircle * 4.5f));
                Hostile.Circle(spots[i], r, 1.1f, Mirror);
            }
            yield return new WaitForSeconds(1.1f);
            if (!Alive) yield break;
            bool inside = false;
            for (int i = 0; i < n; i++)
            {
                Fx.Spawn("fx_explosion", spots[i], r * 2.2f, Mirror, 18f);
                inside |= UltInCircle(spots[i], r);
            }
            UltHit(inside);
            Hostile.Play("boom", 0.5f, 1.3f);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // 메아리: 플레이어 자리에 터진 뒤 같은 자리에서 한 번 더 (처음 터진 자리에 머물면 또 맞음)
    IEnumerator SkEcho(int times)
    {
        const float r = 2.6f;
        for (int n = 0; n < times && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 at = pl.transform.position;
            Hostile.Circle(at, r, 0.9f, Mirror);
            yield return new WaitForSeconds(0.9f);
            if (!Alive) yield break;
            Fx.Spawn("fx_shock", at, r * 2.4f, Mirror, 20f);
            UltHit(UltInCircle(at, r));
            Hostile.Play("crack", 0.5f, 1.1f);
            Hostile.Circle(at, r * 0.85f, 0.5f, Mirror);
            yield return new WaitForSeconds(0.5f);
            if (!Alive) yield break;
            Fx.Spawn("fx_shock", at, r * 2f, Mirror, 20f);
            UltHit(UltInCircle(at, r * 0.85f));
            Hostile.Play("crack", 0.4f, 1.4f);
            yield return new WaitForSeconds(0.25f);
        }
    }

    // 꿰뚫는 줄기: 보스에게서 플레이어 쪽으로 부채꼴 세 줄 → 광선 (줄 사이로 피함)
    IEnumerator SkLines(int volleys)
    {
        const float length = 22f;
        Vector3[] ends = new Vector3[3];
        for (int v = 0; v < volleys && Alive; v++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 c = transform.position;
            Vector2 d = DirTo(pl);
            for (int i = 0; i < 3; i++)
            {
                ends[i] = c + (Vector3)(Rotate(d, (i - 1) * 22f) * length);
                Hostile.Line(c, ends[i], 1.4f, 0.9f, Mirror);
            }
            yield return Windup(Mirror, 0.9f);
            if (!Alive) yield break;
            pl = Hostile.Player;
            bool inside = false;
            for (int i = 0; i < 3; i++)
            {
                Fx.Beam(c, ends[i], 1.1f, new Color(0.85f, 0.95f, 1f), 0.25f);
                if (pl != null && Hostile.HurtToSegment(pl, c, ends[i]) < 0.9f) inside = true;
            }
            UltHit(inside);
            Hostile.Play("zap", 0.6f, 1f);
            yield return new WaitForSeconds(0.35f);
        }
    }

    // 둘레 폭발: 보스 둘레에 큰 원 + 플레이어 자리에 작은 원 (보스에게서 떨어져 비켜서면 피함)
    IEnumerator SkBlast(int times)
    {
        for (int n = 0; n < times && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 me = transform.position, at = pl.transform.position;
            Hostile.Circle(me, 4.5f, 1.1f, Mirror);
            Hostile.Circle(at, 2.2f, 1.1f, Mirror);
            yield return Windup(Mirror, 1.1f);
            if (!Alive) yield break;
            Fx.Spawn("fx_soulburst", me, 10f, Mirror, 18f);
            Fx.Spawn("fx_shock", at, 5f, Mirror, 20f);
            UltHit(UltInCircle(me, 4.5f) || UltInCircle(at, 2.2f));
            Hostile.Play("boom", 0.7f, 1f);
            Hostile.Shake(0.2f);
            yield return new WaitForSeconds(0.4f);
        }
    }

    // 끌어당김: 플레이어를 천천히 끌어당긴 뒤 보스 둘레가 터짐 (걸어서 버티면 벗어남)
    IEnumerator SkPull()
    {
        const float r = 3.4f;
        Hostile.Circle(transform.position, r, 1.4f, Mirror);
        for (float t = 0f; t < 1.4f && Alive; t += Time.deltaTime)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null || pl.IsDying) break;
            Vector3 next = Hostile.ClampArena(pl.transform.position + (transform.position - pl.transform.position).normalized * 2.2f * Time.deltaTime);
            if (!Hostile.IsWall(next)) pl.transform.position = next;
            yield return null;
        }
        if (!Alive) yield break;
        Fx.Spawn("fx_soulburst", transform.position, r * 2.6f, Mirror, 18f);
        UltHit(UltInCircle(transform.position, r));
        Hostile.Play("boom", 0.7f, 0.9f);
        Hostile.Shake(0.2f);
    }

    // 고리: 보스 둘레 고리 위의 원들이 차례로 터짐 (안쪽 고리 → 바깥 고리, 원 사이 틈으로 피함)
    IEnumerator SkRing(int rings)
    {
        Vector3 c = transform.position;
        const float r = 1.3f;
        for (int w = 0; w < rings && Alive; w++)
        {
            float radius = 3.5f + w * 3f;
            int n = 8 + w * 4;
            float off = Random.Range(0f, 360f);
            Vector3[] spots = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = (off + i * 360f / n) * Mathf.Deg2Rad;
                spots[i] = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Hostile.Circle(spots[i], r, 0.8f, Mirror);
            }
            yield return new WaitForSeconds(0.8f);
            if (!Alive) yield break;
            bool inside = false;
            foreach (Vector3 at in spots)
            {
                Fx.Spawn("fx_spark", at, 3f, Mirror, 20f);
                inside |= UltInCircle(at, r);
            }
            UltHit(inside);
            Hostile.Play("shimmer", 0.4f, 1.2f + w * 0.1f);
        }
    }

    // 이번 판에 가장 많이 준 피해의 출처 (없으면 평타)
    static string TopSource()
    {
        string top = DamageSource.Basic;
        float best = 0f;
        foreach (KeyValuePair<string, float> kv in RunStats.DamageDealt)
            if (kv.Key != DamageSource.Other && kv.Value > best) { best = kv.Value; top = kv.Key; }
        return top;
    }

    // 되비친 기억: 가장 많이 쓴 공격을 거꾸로 돌려줌
    IEnumerator ReflectedMemory(PlayerController p, string top)
    {
        if (top == DamageSource.Basic) { yield return MirrorStrike(6); yield break; }
        // 고유 스킬: 배운 레벨업 스킬을 세게 흉내
        if (top == DamageSource.Signature) { yield return MirrorSkill(true, false, true, true); yield break; }
        // 필살기: 캐릭터의 필살기를 한 번 더 길게 흉내 (예전 거대한 광선은 한 방 42 피해라 너무 셌음)
        if (top == DamageSource.Ult) { yield return MirrorUlt(true, false); yield break; }
        if (top == DamageSource.Tree)
        {
            // 영혼 트리: 플레이어를 끌어당기며 조각 고리
            StartCoroutine(ShardRings());
            for (float t = 0f; t < 2f && Alive; t += Time.deltaTime)
            {
                PlayerController pl = Hostile.Player;
                if (pl == null || pl.IsDying) break;
                Vector3 next = Hostile.ClampArena(pl.transform.position + (transform.position - pl.transform.position).normalized * 2.2f * Time.deltaTime);
                if (!Hostile.IsWall(next)) pl.transform.position = next;
                yield return null;
            }
            yield break;
        }
        // 특수 능력 (무기 · 스킬 이름이 출처): 그 능력을 세게 흉내 (예전 나선 조각은 쏟아지는 탄이라 피해 제한이 없었음)
        if (top != DamageSource.Other) { yield return MirrorSkill(false, true, true, true, top); yield break; }
        // 그 밖: 네 갈래 나선 조각
        yield return Windup(Mirror, 0.6f);
        float spin = Random.value < 0.5f ? 1f : -1f;
        for (float t = 0f; t < 2.6f && Alive; t += 0.11f)
        {
            for (int k = 0; k < 4; k++)
            {
                float a = (t * 75f * spin + k * 90f) * Mathf.Deg2Rad;
                Hostile.Shoot(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 8f, Capped(13f * Power), 0.45f, Mirror, 1.3f, 4f);
            }
            if (Mathf.Repeat(t, 0.44f) < 0.11f) Hostile.Play("shimmer", 0.25f, 1.5f);
            yield return new WaitForSeconds(0.11f);
        }
    }

    static readonly Vector3[] runeInBuf = new Vector3[61];
    static readonly Vector3[] runeOutBuf = new Vector3[61];

    // 두 겹의 룬 고리: 안쪽은 끊긴 점선처럼, 바깥은 반대로 회전
    static void DrawRunes(LineRenderer inner, LineRenderer outer, Vector3 center, float r1, float r2, float spin, float alpha)
    {
        const int seg = 60;
        if (inner.positionCount != seg + 1) inner.positionCount = seg + 1;
        if (outer.positionCount != seg + 1) outer.positionCount = seg + 1;
        // 1.2.4: 점마다 SetPosition(매 프레임 122번) → 버퍼에 채워 SetPositions 두 번 (같은 좌표)
        for (int i = 0; i <= seg; i++)
        {
            float k = i / (float)seg * Mathf.PI * 2f;
            // 안쪽 고리는 여섯 번 출렁이는 룬 모양
            float wobble = 1f + 0.08f * Mathf.Sin(k * 6f + spin * Mathf.Deg2Rad * 2f);
            float a1 = k + spin * Mathf.Deg2Rad;
            float a2 = k - spin * Mathf.Deg2Rad * 0.6f;
            runeInBuf[i] = center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * r1 * wobble;
            runeOutBuf[i] = center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2)) * r2;
        }
        inner.SetPositions(runeInBuf);
        outer.SetPositions(runeOutBuf);
        inner.startColor = inner.endColor = new Color(0.75f, 0.4f, 1f, alpha);
        outer.startColor = outer.endColor = new Color(0.55f, 0.95f, 1f, alpha * 0.8f);
    }

    // ================================================================= 킹 슬라임 (2.2.1~ 분열 대신 맞을수록 작고 빠름 · BossSkills.Reborn.cs)
    static readonly Color Acid = new Color(0.55f, 1f, 0.35f, 0.9f);
    float SlimeSize => Mathf.Max(0.4f, transform.localScale.x / Mathf.Max(1f, slimeSwell) / 27f);      // 1.4.4~ 뱉기 예고의 부풀기는 겉모습만

    // 대점프: 떨어질 곳에 그림자 원 → 높이 뛰어올라 내려찍고 산성 웅덩이
    IEnumerator SlimeLeap(PlayerController p, float warn)
    {
        float r = 3.6f * SlimeSize;
        Vector3 target = Hostile.ClampArena(p.transform.position);
        Hostile.Circle(target, r, warn + 0.35f, Acid);
        yield return Windup(Acid, warn * 0.6f);
        if (!Alive) yield break;

        Vector3 start = transform.position;
        Hostile.Play("whoosh", 0.7f, 0.6f);
        Fx.Spawn("fx_puddle", start, 3f * SlimeSize, Acid, 14f);
        airborne = true;
        landing = target;
        // 위로 솟구침
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            transform.position = start + Vector3.up * 14f * (t / 0.3f);
            yield return null;
        }
        yield return new WaitForSeconds(Mathf.Max(0f, warn * 0.4f - 0.2f));
        // 떨어짐
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(target + Vector3.up * 14f, target, t / 0.25f);
            yield return null;
        }
        transform.position = target;
        airborne = false;
        if (!Alive) yield break;
        Hostile.HitCircle(target, r, 30f * Power);
        Fx.Spawn("fx_shock", target, r * 2.6f, Acid, 18f);
        Fx.Spawn("fx_puddle", target, r * 1.3f, Acid, 12f);
        HazardZone.Spawn(target, r * 0.8f, 3f, 10f, Acid, "fx_puddle", 0.7f);
        Hostile.Play("thump", 1f, 0.6f);
        Hostile.Shake(0.2f);           // 킹 슬라임의 유일한 화면 흔들림
    }

    // 구르기 돌진: 경로를 보여준 뒤 굴러가며 산성 자국을 남김
    IEnumerator SlimeRoll(PlayerController p, float warn)
    {
        Vector3 start = transform.position;
        Vector3 end = Hostile.ClampArena(start + (Vector3)(DirTo(p) * 20f));
        Hostile.Line(start, end, 5.2f * SlimeSize + 1.2f, warn, Acid);     // 1.0.4~ 띠 너비 = 판정 지름 (예전 3.5S+1 은 판정 2.6S+0.6 보다 좁아 띠 밖에서 맞았음)
        yield return Windup(Acid, warn);
        if (!Alive) yield break;
        bool hit = false;
        float trail = 0f;
        Hostile.Play("whoosh", 0.8f, 0.5f);
        for (float t = 0f; t < 0.55f; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            transform.position = Vector3.Lerp(start, end, t / 0.55f);
            transform.Rotate(0f, 0f, -900f * Time.deltaTime);
            trail += Time.deltaTime;
            if (trail > 0.08f)
            {
                trail = 0f;
                HazardZone.Spawn(transform.position, 1.4f, 2.5f, 8f, Acid, "fx_puddle", 0.8f);
            }
            if (!hit && Hostile.HurtDistance(p, transform.position) < 2.6f * SlimeSize + 0.6f) hit = p.TryHit(28f * Power);
            yield return null;
        }
        if (!hit && p != null && !p.IsDying) RunCounters.AddLife("roll.dodge");      // 1.2~ 보드 「굴러오는 길」
        transform.rotation = Quaternion.identity;
        Fx.Spawn("fx_shock", transform.position, 6f * SlimeSize, Acid, 18f);
    }

    // ================================================================= 지옥의 군주
    // 화염 돌진: 굵은 경로를 보여준 뒤 돌진
    IEnumerator FlameCharge(PlayerController p, float warn)
    {
        Vector3 start = transform.position;
        Vector3 end = Hostile.ClampArena(start + (Vector3)(DirTo(p) * 18f));
        Hostile.Line(start, end, 4.8f, warn, Fire);     // 1.0.4~ 띠 너비 = 판정 지름 (예전 3.2 는 판정 반지름 2.4 보다 좁아 띠 밖에서 맞았음)
        yield return Windup(Fire, warn);
        if (!Alive) yield break;

        Hostile.Play("whoosh", 0.8f, 0.6f);
        bool hit = false;
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            transform.position = Vector3.Lerp(start, end, t / 0.45f);
            if (Hostile.Glow != null)
                FlameParticle.Spawn(Hostile.Glow, transform.position + (Vector3)Random.insideUnitCircle, Random.insideUnitCircle * 2f, 0.5f, 0.08f, 0.35f, false);
            if (Random.value < 0.3f) Fx.Spawn("fx_explosion", transform.position + (Vector3)(Random.insideUnitCircle * 1.2f), 2.2f, Color.white, 20f);
            if (!hit && Hostile.HurtDistance(p, transform.position) < 2.4f) hit = p.TryHit(35f * Power);
            yield return null;
        }
        Hostile.Burst(transform.position, 3f, Fire, true);
        Fx.Spawn("fx_shock", transform.position, 7f, new Color(1f, 0.55f, 0.2f), 18f);
    }

    // 운석 낙하: 플레이어 주변에 차례로 떨어지는 운석
    IEnumerator MeteorRain(PlayerController p)
    {
        yield return Windup(Fire, 0.5f);
        for (int i = 0; i < 7 && Alive; i++)
        {
            Vector3 spot = i == 0 ? p.transform.position : p.transform.position + (Vector3)(Random.insideUnitCircle * 7f);
            StartCoroutine(Meteor(Hostile.ClampArena(spot)));
            yield return new WaitForSeconds(0.22f);
        }
        yield return new WaitForSeconds(1.2f);
    }

    IEnumerator Meteor(Vector3 spot)
    {
        const float warn = 1.3f;
        const float r = 2.6f;
        Hostile.Circle(spot, r, warn, Fire);
        yield return new WaitForSeconds(warn - 0.4f);
        FxAnim meteor = Fx.Play("fx_meteor", spot + Vector3.up * 10f, 2.6f, Color.white, 14f, 0f, 12, true, 0.5f);
        GameObject rock = meteor != null ? meteor.gameObject : null;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            if (rock != null)
            {
                rock.transform.position = Vector3.Lerp(spot + Vector3.up * 10f, spot, t / 0.4f);
                FlameParticle.Spawn(Hostile.Glow, rock.transform.position, Vector2.up * 3f + Random.insideUnitCircle, 0.3f, 0.05f, 0.25f, false);
            }
            yield return null;
        }
        if (rock != null) Destroy(rock);
        Hostile.HitCircle(spot, r, 28f * Power);
        Fx.Spawn("fx_explosion", spot, r * 2.6f, Color.white, 16f);
        Hostile.Burst(spot, r, Fire, true);
        Hostile.Play("boom", 0.6f, 0.9f);
        Hostile.Shake(0.15f);          // 지옥의 군주의 유일한 화면 흔들림
    }

    // 화염 파동: 틈이 있는 불의 고리가 퍼져 나감 (틈으로 피함)
    IEnumerator FireWave()
    {
        const int gaps = 3;
        const float gapSize = 40f;
        float gapStart = Random.Range(0f, 360f);
        float[] gapAt = new float[gaps];
        for (int i = 0; i < gaps; i++) gapAt[i] = gapStart + i * 360f / gaps;

        // 경고: 불이 지나갈 부분을 보여줌 (틈은 비어 있음)
        LineRenderer[] warn = Arcs(gapAt, gapSize, 3f, new Color(1f, 0.45f, 0.12f, 0.6f), 0.25f);
        yield return Windup(Fire, 1f);
        foreach (LineRenderer l in warn) Destroy(l.gameObject);
        if (!Alive) yield break;

        Hostile.Play("boom", 0.8f, 0.6f);
        Fx.Spawn("fx_explosion", transform.position, 5f, Color.white, 16f);
        LineRenderer[] wave = Arcs(gapAt, gapSize, 1f, Fire, 1.2f);
        bool hit = false;
        Vector3 center = transform.position;
        for (float radius = 1f; radius < 22f && Alive; radius += 10f * Time.deltaTime)
        {
            for (int i = 0; i < gaps; i++)
                Hostile.SetArc(wave[i], center, radius, gapAt[i] + gapSize * 0.5f, gapAt[i] + 360f / gaps - gapSize * 0.5f);

            PlayerController p = Hostile.Player;
            if (!hit && p != null)
            {
                Vector2 to = p.HurtCenter - (Vector2)center;      // 1.4.4~ 몸 아래쪽 판정
                if (Mathf.Abs(to.magnitude - radius) < 0.9f - PlayerController.HurtShrink && !InGap(Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg, gapAt, gapSize))
                    hit = p.TryHit(30f * Power);
            }
            yield return null;
        }
        foreach (LineRenderer l in wave) Destroy(l.gameObject);
    }

    LineRenderer[] Arcs(float[] gapAt, float gapSize, float radius, Color color, float width)
    {
        LineRenderer[] arcs = new LineRenderer[gapAt.Length];
        for (int i = 0; i < gapAt.Length; i++)
        {
            arcs[i] = Own(Hostile.NewLine("FireArc", color, width, 17));
            Hostile.SetArc(arcs[i], transform.position, radius, gapAt[i] + gapSize * 0.5f, gapAt[i] + 360f / gapAt.Length - gapSize * 0.5f);
        }
        return arcs;
    }

    static bool InGap(float angle, float[] gapAt, float gapSize)
    {
        foreach (float g in gapAt)
            if (Mathf.Abs(Mathf.DeltaAngle(angle, g)) < gapSize * 0.5f) return true;
        return false;
    }

}

// 영혼 조각: 한 점으로 빨려 들어가거나(수렴) 퍼져 나가며 사라짐
public class SoulWisp : MonoBehaviour
{
    Vector3 from, to;
    float t, duration;
    SpriteRenderer sr;
    Color color;

    // 1.2.4: 다 날아간 조각은 꺼 두었다가 다시 씀 (폭발 1회에 8개씩 생성 · 파괴하던 것)
    static readonly Stack<SoulWisp> pool = new Stack<SoulWisp>();
    int spawnFrame = -1;

    public static void Spawn(Vector3 from, Vector3 to, Color color, bool burst = false)
    {
        if (Hostile.Glow == null) return;
        SoulWisp w = null;
        while (pool.Count > 0 && w == null) w = pool.Pop();     // 장면이 바뀌어 지워진 것은 건너뜀
        if (w == null)
        {
            GameObject go = SpecialAbilities.MakeSprite("SoulWisp", Hostile.Glow, from, 0.07f, color, "Effect", 10);
            w = go.AddComponent<SoulWisp>();
            w.sr = go.GetComponent<SpriteRenderer>();
        }
        else
        {
            // MakeSprite 가 넣던 값으로 되돌림
            Transform tr = w.transform;
            tr.position = from;
            tr.localScale = Vector3.one * 0.07f;
            w.sr.sprite = Hostile.Glow;
            w.sr.color = color;
            w.t = 0f;
            w.spawnFrame = Time.frameCount;
        }
        w.from = from;
        w.to = to;
        w.duration = burst ? Random.Range(0.4f, 0.7f) : Random.Range(0.5f, 0.8f);
        w.color = color;
        if (!w.gameObject.activeSelf) w.gameObject.SetActive(true);
    }

    void Update()
    {
        if (Time.frameCount == spawnFrame) return;
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        float ease = k * k;
        // 살짝 휘어서 날아감
        Vector3 side = Vector3.Cross(to - from, Vector3.forward).normalized * Mathf.Sin(k * Mathf.PI) * 1.2f;
        transform.position = Vector3.Lerp(from, to, ease) + side;
        transform.localScale = Vector3.one * Mathf.Lerp(0.07f, 0.03f, k);
        sr.color = new Color(color.r, color.g, color.b, color.a * Mathf.Sin(k * Mathf.PI));
        if (k >= 1f)
        {
            gameObject.SetActive(false);
            pool.Push(this);
        }
    }
}
