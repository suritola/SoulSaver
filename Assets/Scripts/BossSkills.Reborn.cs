using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 2.2.1 보스 개편 (1 · 2 · 3장) — 장마다 확실히 어려워지고, 보스마다 고유한 파훼법이 하나씩
//   1장 킹 슬라임 (배우는 보스): 분열 없음 · 체력바 하나 · 맞을수록 작아지고 빨라짐, 느리고 예고가 긴 패턴 셋, 절반에서 작은 슬라임을 한 번 뱉음
//   2장 리치 왕 (공간을 막는 보스): 추적 영혼구(쏴서 터뜨림) · 뼈 감옥(틈으로 탈출) · 저주 묘비(부수기 전까지 터짐) · 순간이동,
//        절반에서 「망자의 의식」: 무적 + 영혼 등불 셋 → 다 부수면 무적이 풀리고 휘청(받는 피해 1.5배)
//   3장 지옥의 군주 (몰아치는 보스, 3단계): 돌진 연타 · 운석 · 불의 파도 → 66% 날개(공중 탄막 · 내려찍기) → 33% 둘레가 용암으로 좁아지고 빨라짐
// 4장 거울의 군주는 그대로 (BossSkills.cs)
public partial class BossSkills
{
    int phase;              // 지옥의 군주 단계 (0 · 1 · 2)
    bool specialDone;       // 킹 슬라임 뱉기 · 리치 왕 의식 (보스전마다 한 번)

    float HpFrac => boss.setEnemyHP > 0 ? Mathf.Clamp01(boss.EnemyHealth / boss.setEnemyHP) : 1f;
    static float Cool(float seconds) => seconds * GameMode.SkillCooldownMul * Chapters.BossSkillCooldownMul;
    float PartHp(float share) => Mathf.Max(5f, boss.setEnemyHP * share);
    static Sprite GlowSprite => Hostile.Glow;
    static Sprite SwirlSprite => SpecialAbilities.SwirlSprite != null ? SpecialAbilities.SwirlSprite : Hostile.Glow;

    // 2단계 묶음 6~ 리치 왕 의식의 부술 것 (영혼구 · 저주 묘비 · 영혼 등불): 쌍선형 빛 덩어리 → 「부술 수 있는 물건」으로 읽히는 도트 그림
    //   Resources/Bosses/ritual_* (PPU 6 — 그림 1칸 = 월드 1/6칸), BossPart 가 곱하는 보스 색은 불투명으로 (반투명 금지)
    //   판정(worldRadius)은 그대로, 그림이 없거나 VisualRules.Legacy 면 예전 빛 덩어리 · 배율 · 색
    static readonly Dictionary<string, Sprite> ritualArt = new Dictionary<string, Sprite>();
    static Sprite RitualArt(string name)
    {
        if (VisualRules.Legacy) return null;
        if (!ritualArt.TryGetValue(name, out Sprite s))
        {
            s = Resources.Load<Sprite>("Bosses/" + name);
            if (s == null) Debug.LogWarning("[BossSkills] 의식 물건 그림 없음: Resources/Bosses/" + name + " — 예전 빛 덩어리로");
            ritualArt[name] = s;
        }
        return s;
    }
    // 의식 물건 만들기: 새 그림이면 배율 = 원하는 월드 높이 ÷ 그림 높이 · 색 불투명, 아니면 예전 그림 · 배율 · 색
    static BossPart RitualPart(string name, string art, float worldHeight, Sprite legacySprite, float legacyScale, Vector3 at, Color color, float hp, float worldRadius)
    {
        Sprite s = RitualArt(art);
        if (s == null) return BossPart.Make(name, legacySprite, at, legacyScale, color, hp, worldRadius);
        return BossPart.Make(name, s, at, worldHeight / Mathf.Max(0.01f, s.bounds.size.y), new Color(color.r, color.g, color.b, 1f), hp, worldRadius);
    }

    static void Banner(string ko)
    {
        Notice.Big(Loc.T(ko), NoticeIcon.Boss, 2.2f, NoticeLevel.Urgent);
    }

    // 스킬을 쓰지 않는 동안에도 매 프레임 (슬라임 몸 크기)
    void RebornTick()
    {
        if (kind == 2) SlimeBody();
    }

    void RebornTurn(PlayerController p)
    {
        if (kind == 2) SlimeTurn(p);
        else if (kind == 0) LichTurn(p);
        else HellTurn(p);
    }

    // ================================================================= 1장 킹 슬라임
    Vector3 slimeBaseScale;
    float slimeBaseSpeed = -1f;
    float slimeSwellUntil;      // 1.4.4~ 뱉기 예고로 몸이 부푸는 끝 시각
    float slimeSwell = 1f;      // 1.4.4~ 지금 부푼 배율 (SlimeSize 에서 나눠 빼서 판정 크기는 그대로)

    // 맞을수록 작아지고 (65%까지) 빨라짐 (1.6배까지)
    void SlimeBody()
    {
        if (boss == null) return;
        if (slimeBaseSpeed < 0f) { slimeBaseScale = transform.localScale; slimeBaseSpeed = boss.speed; }
        float f = HpFrac;
        // 1.4.4~ 뱉기 예고 동안 몸이 부풀었다 줄었다 (시간 기준 · 끝나면 저절로 1배)
        float swell = Time.time < slimeSwellUntil ? 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.time * 14f)) : 1f;
        slimeSwell = swell;
        transform.localScale = slimeBaseScale * Mathf.Lerp(0.65f, 1f, f) * swell;
        boss.speed = slimeBaseSpeed * Mathf.Lerp(1.6f, 1f, f);
    }

    void SlimeTurn(PlayerController p)
    {
        if (!specialDone && boss.Enraged) { specialDone = true; StartCoroutine(Run(SlimeSpit())); return; }
        if (Time.time < next) return;
        next = Time.time + Cool(boss.Enraged ? 3f : 3.8f);
        step = (step + 1) % 3;
        StartCoroutine(Run(step == 0 ? SlimeLeap(p, 1.3f) : step == 1 ? GooBlobs(p) : SlimeRoll(p, 1.1f)));
    }

    // 점액 방울: 플레이어 쪽 부채꼴로 느린 방울 다섯 (사이로 피함) · 절반 아래면 엇갈린 두 번째 부채꼴
    IEnumerator GooBlobs(PlayerController p)
    {
        yield return Windup(Acid, 0.7f);
        int waves = boss.Enraged ? 2 : 1;
        for (int w = 0; w < waves && Alive; w++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector2 aim = DirTo(pl);
            for (int i = -2; i <= 2; i++)
                Hostile.Shoot(transform.position, Rotate(aim, i * 18f + w * 9f), 5.5f, 12f * Power, 0.55f, Acid, 1.3f, 3.5f);
            Fx.Spawn("fx_puddle", transform.position, 3f * SlimeSize, Acid, 14f);
            Hostile.Play("hiss", 0.5f, 1.3f);
            yield return new WaitForSeconds(0.6f);
        }
    }

    // 절반 아래 한 번: 작은 슬라임(일반 잡몹) 넷을 뱉음 (예전의 분열 대신)
    // 1.4.4~ 글 배너(「킹 슬라임이 작은 슬라임을 뱉어 낸다!」) 대신 화면에서 바로 보이게 (플레이테스트: 2페에 글로 알려 주면 피하느라 못 읽음)
    //   몸이 부풀며 깜빡임 + 머리 위 짧은 「분열!」 → 떨어질 네 자리에 초록 예고 원 → 원 자리로 점액 줄기가 뻗고 그 자리에 작은 슬라임
    IEnumerator SlimeSpit()
    {
        const int count = 4;
        const float warn = 1.1f;
        Vector3[] spots = new Vector3[count];
        float off = Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
        {
            float a = (off + i * 360f / count) * Mathf.Deg2Rad;
            spots[i] = Hostile.ClampArena(transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(3.5f, 5f));
            Hostile.Circle(spots[i], 1.1f, warn, Acid);
        }
        slimeSwellUntil = Time.time + warn;      // 시간으로만 (끊겨도 몸 크기가 남지 않음)
        SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 2.4f * Mathf.Max(1f, SlimeSize), Loc.T("분열!"), Acid, 6f, 0f, warn);
        Hostile.Play("hiss", 0.6f, 0.7f);
        yield return Windup(Acid, warn);
        if (!Alive) yield break;
        EnemySpawner sp = FindFirstObjectByType<EnemySpawner>();
        for (int i = 0; i < count; i++)
        {
            LineRenderer lr = Hostile.NewLine("SlimeSpitGoo", Acid, 0.35f);
            lr.positionCount = 2;
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, transform.position);
            StartCoroutine(GooReach(lr, transform.position, spots[i]));      // 1.4.4~ 보스에서 원 쪽으로 0.15초 동안 뻗음
            Destroy(lr.gameObject, 0.3f);       // 코루틴이 끊겨도 줄은 사라짐
            if (sp != null) sp.SummonMinionAt(spots[i]);
            Fx.Spawn("fx_puddle", spots[i], 2.2f, Acid, 14f);
        }
        Fx.Spawn("fx_shock", transform.position, 7f, Acid, 18f);
        Hostile.Play("thump", 0.8f, 0.9f);
    }

    // 점액 줄기 끝점을 from → to 로 0.15초 동안 늘림 (줄이 먼저 지워지면 그만)
    IEnumerator GooReach(LineRenderer lr, Vector3 from, Vector3 to)
    {
        float t0 = Time.time;
        while (lr != null)
        {
            float k = Mathf.Clamp01((Time.time - t0) / 0.15f);
            lr.SetPosition(1, Vector3.Lerp(from, to, k));
            if (k >= 1f) yield break;
            yield return null;
        }
    }

    // ================================================================= 2장 리치 왕
    readonly List<BossPart> tombs = new List<BossPart>();
    static readonly Color Bone = new Color(1f, 0.95f, 0.8f, 0.9f);

    void LichTurn(PlayerController p)
    {
        if (!specialDone && boss.Enraged && Time.time >= ritualRetryAt) { specialDone = true; StartCoroutine(Run(LichRitual())); return; }
        if (Time.time < next) return;
        next = Time.time + Cool(boss.Enraged ? 2.8f : 3.6f);
        step = (step + 1) % 4;
        tombs.RemoveAll(t => t == null || t.Broken);
        IEnumerator s = step == 0 ? SoulOrbs() : step == 1 ? BonePrison(p) : step == 2 ? (tombs.Count < 3 ? CursedTombs(p) : SoulOrbs()) : Blink(p);
        StartCoroutine(Run(s));
    }

    // 추적 영혼구: 천천히 쫓아오는 영혼구 셋(절반 아래 넷) — 쏴서 터뜨리거나 피해 다니면 7초 뒤 제자리에서 터짐
    IEnumerator SoulOrbs()
    {
        yield return Windup(Soul, 0.6f);
        int n = boss.Enraged ? 4 : 3;
        float off = Random.Range(0f, 360f);
        for (int i = 0; i < n && Alive; i++)
        {
            float a = (off + i * 360f / n) * Mathf.Deg2Rad;
            Vector3 at = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 2.2f;
            BossPart orb = Own(RitualPart("SoulOrb", "ritual_orb", 2f, GlowSprite, 0.5f, at, Soul, PartHp(0.015f), 0.7f));
            StartCoroutine(OrbChase(orb));
        }
        Hostile.Play("chime", 0.6f, 0.8f);
        yield return new WaitForSeconds(0.6f);
    }

    IEnumerator OrbChase(BossPart orb)
    {
        for (float t = 0f; t < 7f; t += Time.deltaTime)
        {
            if (orb == null || orb.Broken || !Alive) yield break;
            PlayerController p = Hostile.Player;
            if (p == null) yield break;
            Vector3 to = p.transform.position - orb.transform.position;
            to.z = 0f;
            orb.transform.position += to.normalized * 3.2f * Time.deltaTime;
            if (Hostile.HurtDistance(p, orb.transform.position) < 0.9f)     // 1.4.4~ 몸 아래쪽 판정 (쫓는 곳은 그대로 transform 위치)
            {
                p.TryHit(16f * Power);
                Fx.Spawn("fx_soulburst", orb.transform.position, 3f, Soul, 18f);
                Destroy(orb.gameObject);
                yield break;
            }
            yield return null;
        }
        if (orb == null || orb.Broken) yield break;
        Vector3 at = orb.transform.position;
        Hostile.Circle(at, 2f, 0.6f, Soul);
        yield return new WaitForSeconds(0.6f);
        if (orb == null || orb.Broken) yield break;
        Hostile.HitCircle(at, 2f, 14f * Power);
        Hostile.Burst(at, 2f, Soul);
        Destroy(orb.gameObject);
    }

    // 뼈 감옥: 플레이어를 둘러싼 뼈 가시 벽(틈 하나) → 안쪽이 터짐. 틈으로 빠져나감 (벽을 넘으면 맞음)
    IEnumerator BonePrison(PlayerController p)
    {
        const float r = 4.5f, gap = 70f;
        Vector3 c = p.transform.position;
        float gapAt = Random.Range(0f, 360f);
        float[] gaps = { gapAt };
        LineRenderer wall = Own(Hostile.NewLine("BonePrison", new Color(Bone.r, Bone.g, Bone.b, 0.45f), 0.25f, 17));
        Hostile.SetArc(wall, c, r, gapAt + gap * 0.5f, gapAt + 360f - gap * 0.5f);
        Hostile.Circle(c, r - 0.6f, 2.6f, Curse);
        Hostile.Play("crack", 0.6f, 0.8f);
        yield return Windup(Bone, 0.8f);
        if (!Alive) { Destroy(wall.gameObject); yield break; }

        // 벽이 솟음: 1.8초 동안 벽을 넘으면 맞음
        wall.startWidth = wall.endWidth = 0.6f;
        wall.startColor = wall.endColor = Bone;
        for (int i = 0; i <= 24; i++)
        {
            float a = (gapAt + gap * 0.5f + i * (360f - gap) / 24f) * Mathf.Deg2Rad;
            Fx.Spawn("fx_spike", c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r + Vector3.up * 0.5f, 2f, new Color(1f, 0.97f, 0.88f), 18f);
        }
        Hostile.Play("crack", 0.8f, 1.1f);
        float hitCd = 0f;
        for (float t = 0f; t < 1.8f && Alive; t += Time.deltaTime)
        {
            hitCd -= Time.deltaTime;
            PlayerController pl = Hostile.Player;
            if (pl != null && hitCd <= 0f)
            {
                Vector2 to = pl.HurtCenter - (Vector2)c;      // 1.4.4~ 몸 아래쪽 판정
                if (Mathf.Abs(to.magnitude - r) < 0.7f - PlayerController.HurtShrink && !InGap(Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg, gaps, gap) && pl.TryHit(14f * Power)) hitCd = 0.6f;
            }
            yield return null;
        }
        if (wall != null) Destroy(wall.gameObject);
        if (!Alive) yield break;
        Hostile.HitCircle(c, r - 0.6f, 24f * Power);
        Hostile.Burst(c, r - 0.6f, Curse);
        Hostile.Play("boom", 0.7f, 1.1f);
    }

    // 저주 묘비: 플레이어 근처에 묘비 둘(절반 아래 셋) — 부수기 전까지 3초마다 둘레가 터짐 (최대 14초)
    IEnumerator CursedTombs(PlayerController p)
    {
        Hostile.Play("crack", 0.6f, 0.7f);
        yield return Windup(Curse, 0.6f);
        int n = boss.Enraged ? 3 : 2;
        for (int i = 0; i < n && Alive; i++)
        {
            Vector3 at = Hostile.ClampArena(p.transform.position + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(4f, 7f)));
            BossPart tomb = Own(RitualPart("CursedTomb", "ritual_tomb", 3f, GlowSprite, 0.9f, at, Curse, PartHp(0.03f), 0.9f));
            // 묘비 밑 도는 룬은 Legacy 전용 (1단계 「적 쪽 도는 룬 없음」 — 묘비 그림이 저주 문양을 직접 가짐)
            if (VisualRules.Legacy)
            {
                FxAnim rune = Fx.Play("fx_rune", at, 2.6f, new Color(0.75f, 0.4f, 1f, 0.8f), 1f, 0f, 11, true, 14f);
                if (rune != null) { rune.spin = 60f; rune.transform.SetParent(tomb.transform, true); }
            }
            tombs.Add(tomb);
            Fx.Spawn("fx_spike", at + Vector3.up * 0.5f, 2.4f, Curse, 18f);
            StartCoroutine(TombPulse(tomb));
        }
    }

    IEnumerator TombPulse(BossPart tomb)
    {
        float end = Time.time + 14f;
        yield return new WaitForSeconds(1.2f);
        while (tomb != null && !tomb.Broken && Alive && Time.time < end)
        {
            Vector3 at = tomb.transform.position;
            Hostile.Circle(at, 3f, 1f, Curse);
            yield return new WaitForSeconds(1f);
            if (tomb == null || tomb.Broken || !Alive) yield break;
            Hostile.HitCircle(at, 3f, 13f * Power);
            Hostile.Burst(at, 3f, Curse);
            Hostile.Play("boom", 0.35f, 1.5f);
            yield return new WaitForSeconds(2f);
        }
        if (tomb != null && !tomb.Broken)
        {
            Fx.Spawn("fx_soulburst", tomb.transform.position, 3f, Curse, 18f);
            Destroy(tomb.gameObject);
        }
    }

    // 순간이동: 사라졌다 플레이어에게서 떨어진 곳에 나타나며 사방으로 영혼 화살
    IEnumerator Blink(PlayerController p)
    {
        Vector3 from = transform.position, to = from;
        for (int k = 0; k < 8; k++)
        {
            Vector3 c = Hostile.ClampArena(p.transform.position + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(6f, 9f)));
            if (Vector2.Distance(c, p.transform.position) > 5f) { to = c; break; }
        }
        Hostile.Circle(to, 1.6f, 0.8f, Soul);
        Hostile.Play("shimmer", 0.7f, 1.1f);
        for (float t = 0f; t < 0.5f && Alive; t += Time.deltaTime)
        {
            sr.color = new Color(1f, 1f, 1f, 1f - t / 0.5f);
            yield return null;
        }
        if (!Alive) yield break;
        Fx.Spawn("fx_soulburst", from, 3f, Soul, 18f);
        transform.position = to;
        for (float t = 0f; t < 0.3f && Alive; t += Time.deltaTime)
        {
            sr.color = new Color(1f, 1f, 1f, t / 0.3f);
            yield return null;
        }
        sr.color = Color.white;
        if (!Alive) yield break;
        int n = boss.Enraged ? 16 : 12;
        float off = Random.Range(0f, 360f);
        for (int i = 0; i < n; i++) Hostile.Shoot(transform.position, Rotate(Vector2.right, off + i * 360f / n), 6.5f, 12f * Power, 0.45f, Soul, 1.1f, 3f);
        Hostile.Play("chime", 0.6f, 1.2f);
        yield return new WaitForSeconds(0.4f);
    }

    // 망자의 의식 (절반 아래 한 번): 무적 + 영혼 등불 셋, 등불과 이어진 동안 나선 탄막
    // 22초 안에 등불을 다 부수면 무적이 풀리고 4초 휘청 (받는 피해 1.5배), 못 부수면 남은 등불이 터지고 무적만 풀림
    // 1.2.7~ 의식이 도는 중 (공간 침식 · 결계가 스킬을 끊으면 CancelForErosion 이 무적을 풀고 의식을 다시 할 수 있게 함)
    //   끊긴 의식은 RitualRetryDelay 초 뒤에 다시 (결계 · 침식이 끝나는 바로 그 프레임에 다시 무적이 되지 않게)
    bool ritualRunning;
    float ritualRetryAt;
    const float RitualRetryDelay = 8f;
    // 몸을 띄우는 스킬(의식 · 공중 탄막 · 내려찍기)이 끊기면 돌아갈 땅 자리 (ClearSkillState)
    Vector3? groundAt;

    IEnumerator LichRitual()
    {
        Banner("리치 왕이 망자의 의식을 시작한다! 영혼 등불을 부숴라!");
        ritualRunning = true;
        boss.invulnerable = true;
        Vector3 home = transform.position;
        groundAt = home;
        LineRenderer runeIn = Own(Hostile.NewLine("RuneRing", Curse, 0.14f, 2));
        LineRenderer runeOut = Own(Hostile.NewLine("RuneRing", Soul, 0.1f, 2));
        List<BossPart> lanterns = new List<BossPart>();
        List<LineRenderer> tethers = new List<LineRenderer>();
        float a0 = Random.Range(0f, 360f);
        for (int i = 0; i < 3; i++)
        {
            float a = (a0 + i * 120f) * Mathf.Deg2Rad;
            Vector3 at = Hostile.ClampArena(home + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 7f);
            lanterns.Add(Own(RitualPart("SoulLantern", "ritual_lantern", 3f, SwirlSprite, 0.6f, at, i % 2 == 0 ? Soul : Curse, PartHp(0.05f), 0.9f)));
            tethers.Add(Own(Hostile.NewLine("LanternTether", new Color(0.7f, 0.6f, 1f, 0.5f), 0.12f, 3)));
            Fx.Spawn("fx_soulburst", at, 3f, Soul, 18f);
        }
        Hostile.Play("shimmer", 0.9f, 0.5f);
        Hostile.Play("pulse", 0.8f, 0.6f);

        float spin = 0f, volley = 0f, nextVolley = Time.time + 1.5f, until = Time.time + 22f;
        while (Alive && Time.time < until)
        {
            lanterns.RemoveAll(l => l == null || l.Broken);
            if (lanterns.Count == 0) break;
            spin += 80f * Time.deltaTime;
            DrawRunes(runeIn, runeOut, home, 2.6f, 4f, spin, 0.8f);
            transform.position = home + Vector3.up * (0.5f + Mathf.Sin(Time.time * 2.5f) * 0.2f);
            sr.color = Color.Lerp(Color.white, Curse, 0.3f + 0.2f * Mathf.Sin(Time.time * 6f));
            for (int i = 0; i < tethers.Count; i++)
            {
                bool on = i < lanterns.Count;
                tethers[i].enabled = on;
                if (!on) continue;
                tethers[i].positionCount = 2;
                tethers[i].SetPosition(0, transform.position);
                tethers[i].SetPosition(1, lanterns[i].transform.position);
            }
            if (Time.time >= nextVolley)
            {
                nextVolley = Time.time + 1.6f;
                for (int i = 0; i < 10; i++)
                    Hostile.Shoot(transform.position, Rotate(Vector2.right, volley + i * 36f), 5f, 12f * Power, 0.45f, i % 2 == 0 ? Soul : Curse, 1.1f, 4f);
                volley += 13f;
                Hostile.Play("chime", 0.4f, 1.4f);
            }
            yield return null;
        }
        Destroy(runeIn.gameObject);
        Destroy(runeOut.gameObject);
        foreach (LineRenderer t in tethers) if (t != null) Destroy(t.gameObject);
        transform.position = home;
        groundAt = null;
        sr.color = Color.white;
        boss.invulnerable = false;
        ritualRunning = false;
        if (!Alive) yield break;

        if (lanterns.Count == 0)
        {
            if (Time.time - (until - 22f) <= 15f) RunCounters.LifeMax("lanterns15", 1);      // 1.2~ 보드 「등불 셋, 열다섯 초」
            Banner("의식이 깨졌다! 리치 왕이 휘청인다!");
            Fx.Spawn("fx_soulburst", home, 8f, Color.white, 14f);
            ShockRing.Spawn(home, 0.5f, 6f, 0.5f, Soul, 0.4f);
            Hostile.Play("shatter", 0.8f, 0.8f);
            boss.damageTakenMul = 1.5f;
            for (float t = 0f; t < 4f && Alive; t += Time.deltaTime)
            {
                sr.color = Color.Lerp(Color.white, new Color(0.6f, 0.6f, 1f), Mathf.PingPong(Time.time * 3f, 1f));
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 5f) * 8f);
                yield return null;
            }
            transform.rotation = Quaternion.identity;
            sr.color = Color.white;
            if (boss != null) boss.damageTakenMul = 1f;
            yield break;
        }
        // 시간 안에 못 부숨: 남은 등불이 터짐
        foreach (BossPart l in lanterns) if (l != null) Hostile.Circle(l.transform.position, 3.5f, 1f, Curse);
        yield return new WaitForSeconds(1f);
        foreach (BossPart l in lanterns)
        {
            if (l == null) continue;
            Vector3 at = l.transform.position;
            Hostile.HitCircle(at, 3.5f, 22f * Power);
            Hostile.Burst(at, 3.5f, Curse);
            Destroy(l.gameObject);
        }
        Hostile.Play("boom", 0.9f, 0.7f);
    }

    // ================================================================= 3장 지옥의 군주
    void HellTurn(PlayerController p)
    {
        int want = HpFrac <= 0.33f ? 2 : HpFrac <= 0.66f ? 1 : 0;
        if (want > phase)
        {
            phase = want;
            // 1.0.2: 마지막 단계의 「지옥이 무너진다」(용암 가장자리)는 피할 수 없다는 의견으로 뺌 — 단계만 오르고 스킬 간격이 짧아짐
            if (phase == 1) { StartCoroutine(Run(HellWings())); return; }
            // 1.0.4~ 마지막 단계(33%)도 알림 — 갑자기 빨라진 이유를 알 수 있게
            if (phase == 2) { Banner("지옥의 군주가 분노한다! 공격이 빨라진다!"); Hostile.Play("roar", 0.8f, 0.6f); }
        }
        if (Time.time < next) return;
        next = Time.time + Cool(phase == 2 ? 2.2f : phase == 1 ? 2.8f : 3.4f);
        step++;
        IEnumerator s;
        if (phase == 0) s = (step % 3) switch { 0 => DashCombo(2), 1 => MeteorRain(p), _ => FireWave() };
        else if (phase == 1) s = (step % 4) switch { 0 => AirBarrage(), 1 => DashCombo(3), 2 => DiveSlam(p), _ => MeteorRain(p) };
        else s = (step % 5) switch { 0 => AirBarrage(), 1 => DashCombo(3), 2 => DiveSlam(p), 3 => FireWave(), _ => MeteorRain(p) };
        StartCoroutine(Run(s));
    }

    // 돌진 연타: 짧은 예고로 여러 번 연달아 돌진
    IEnumerator DashCombo(int dashes)
    {
        for (int i = 0; i < dashes && Alive; i++)
        {
            PlayerController p = Hostile.Player;
            if (p == null) yield break;
            yield return StartCoroutine(FlameCharge(p, i == 0 ? 0.8f : 0.6f));
            yield return new WaitForSeconds(0.2f);
        }
    }

    // 66%: 날개를 펴며 주위를 밀어내는 불꽃 충격파
    IEnumerator HellWings()
    {
        Banner("지옥의 군주가 날개를 편다!");
        Vector3 c = transform.position;
        Hostile.Circle(c, 4f, 1f, Fire);
        Hostile.Play("roar", 1f, 0.6f);
        for (float t = 0f; t < 1f && Alive; t += Time.deltaTime)
        {
            sr.color = Color.Lerp(Color.white, Fire, Mathf.PingPong(Time.time * 8f, 1f));
            yield return null;
        }
        sr.color = Color.white;
        if (!Alive) yield break;
        Hostile.HitCircle(c, 4f, 18f * Power);
        ShockRing.Spawn(c, 1f, 9f, 0.6f, Fire, 0.6f);
        Fx.Spawn("fx_explosion", c, 7f, Color.white, 16f);
        Hostile.Shake(0.2f);
    }

    // 공중 탄막: 떠올라 플레이어 쪽으로 엇갈린 부채꼴 불탄 세 번(마지막 단계 네 번)
    IEnumerator AirBarrage()
    {
        Vector3 home = transform.position;
        groundAt = home;        // 1.2.7~ 끊기면 여기로 (ClearSkillState)
        Hostile.Play("whoosh", 0.8f, 0.5f);
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            transform.position = home + Vector3.up * 2.5f * (t / 0.4f);
            yield return null;
        }
        int waves = phase == 2 ? 4 : 3;
        for (int w = 0; w < waves && Alive; w++)
        {
            yield return Windup(Fire, 0.35f);
            PlayerController pl = Hostile.Player;
            if (pl == null || !Alive) break;
            Vector2 aim = DirTo(pl);
            for (int i = 0; i < 7; i++)
            {
                HostileProjectile b = Hostile.Shoot(transform.position, Rotate(aim, (i - 3) * 12f + (w % 2) * 6f), 8f, 14f * Power, 0.5f, Fire, 1.2f, 3.5f);
                if (b != null) b.fiery = true;
            }
            Hostile.Play("flame", 0.6f, 1.1f);
            yield return new WaitForSeconds(0.45f);
        }
        Vector3 top = transform.position;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(top, home, t / 0.3f);
            yield return null;
        }
        transform.position = home;
        groundAt = null;
    }

    // 내려찍기: 플레이어 자리에 큰 원 → 솟구쳐 내려찍고 불길이 잠시 남음
    IEnumerator DiveSlam(PlayerController p)
    {
        const float r = 4f;
        Vector3 target = Hostile.ClampArena(p.transform.position);
        Hostile.Circle(target, r, 1.2f, Fire);
        yield return Windup(Fire, 0.5f);
        if (!Alive) yield break;
        Vector3 start = transform.position;
        groundAt = target;      // 1.2.7~ 솟구친 채 끊기면 내려찍을 자리로 (12칸 위에 떠 있지 않게 · ClearSkillState)
        Hostile.Play("whoosh", 0.8f, 0.5f);
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            transform.position = start + Vector3.up * 12f * (t / 0.3f);
            yield return null;
        }
        yield return new WaitForSeconds(0.15f);
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(target + Vector3.up * 12f, target, t / 0.25f);
            yield return null;
        }
        transform.position = target;
        groundAt = null;
        if (!Alive) yield break;
        Hostile.HitCircle(target, r, 32f * Power);
        Fx.Spawn("fx_explosion", target, r * 2.4f, Color.white, 16f);
        Hostile.Burst(target, r, Fire, true);
        ShockRing.Spawn(target, 1f, r * 1.8f, 0.5f, Fire, 0.4f);
        HazardZone.Spawn(target, r * 0.7f, 3f, 10f, Fire, "fire", 0.8f);     // 1.3.3~ 불 바닥 연출 (FireGround)
        Hostile.Play("boom", 0.9f, 0.7f);
        Hostile.Shake(0.2f);
    }
}
