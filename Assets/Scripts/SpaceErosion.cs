using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.2.5~ 「공간 침식」: 보스가 둘 이상일 때 통합 결계 게이지가 가득 차면, 각자의 결계 대신 거대한 결계 하나가 펼쳐짐
// 대기(최대 3초) → 컷신 「결계 붕괴 → 공간 침식」 → 펼침 1.2초 → 15초 동안 모든 참가 보스의 결계 공격이 더 짧은 간격으로 함께 → 파열 0.8초 → 쉬는 시간 20초
// 공정성: 예고 시간 그대로(최소 0.8초) · 떠도는 안전 지대 「고요의 눈」 · 나를 노리는 타격 0.35초에 하나 · 피해 × 0.85 · 한 대 상한 25% · 피격 무적 +0.25초 · 탄 220 · 대기 타격 96 · 튕기는 것 4
// 장면에 하나 (처음 필요할 때 new GameObject + AddComponent · 복제 아님) — 결계 땅 타격 대기열(DomainStrikes)도 여기서 돌림
public class SpaceErosion : MonoBehaviour
{
    public const float Radius = 48f;            // 지금 결계 35 의 넓이 약 1.9배 (기본 아레나 105 × 65 의 대부분)
    public const float Duration = 15f;
    public const float Rest = 20f;
    static readonly float[] Tempo = { 1f, 0.85f, 0.6f, 0.7f, 0.8f };   // 살아 있는 참가 보스 수 0 ~ 4+ (4 이상은 마지막)
    public const float DevMomTempoFloor = 0.75f;
    public const float WarnFloor = 0.8f;
    public const float DamageMul = 0.85f;
    public const float HitShare = 0.25f;
    public const float HurtGrace = 0.25f;
    public const float AimGap = 0.35f;
    public const int BulletCap = 220;
    public const int BouncerCap = 4;
    const float EyeMoveEvery = 5f, EyePreview = 1f, EyeGlide = 0.6f, EyeStart = 1.5f;
    const float CutHop = 0.35f;
    const float MaxWait = 3f;             // 대기 최대 (돌던 스킬 · 개발자 필살기가 끝나지 않아도)
    const int MaxMembers = 6;

    static SpaceErosion instance;
    static bool erosionSeen;          // 이번 실행에서 본 적 있음 (두 번째부터 컷신 짧게)

    public static void Ensure()
    {
        if (instance != null) return;
        instance = new GameObject("SpaceErosion").AddComponent<SpaceErosion>();
    }

    // 통합 게이지가 가득 참 (BossUltimate.SharedTick)
    public static void Begin()
    {
        Ensure();
        if (!instance.running) instance.StartCoroutine(instance.Run());
    }

    bool running, started;
    readonly List<bosss> members = new List<bosss>(8);
    readonly DomainCtx[] ctxs = new DomainCtx[MaxMembers];
    readonly Color[] colors = new Color[MaxMembers];
    readonly GameObject[] auras = new GameObject[MaxMembers];
    readonly LineRenderer[] bossRings = new LineRenderer[MaxMembers];
    readonly bool[] down = new bool[MaxMembers];
    int count;
    Vector3 center;
    Color avg;
    GameObject root;
    MeshRenderer dark;
    LineRenderer ring, ring2, previewRing;
    readonly LineRenderer[] cracks = new LineRenderer[6];
    readonly LineRenderer[] eyeDash = new LineRenderer[12];
    GameObject spot, eyeGlow;
    LineRenderer spotRing;

    void Awake()
    {
        for (int i = 0; i < MaxMembers; i++) ctxs[i] = new DomainCtx();
    }

    void Update() => DomainStrikes.Tick();

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (started)
        {
            // 컷신 도중 장면이 바뀌면 (ESC 「메인 메뉴로」 등) 멈춘 시간을 되돌림
            if (BossUltimate.CutscenePlaying) Time.timeScale = 1f;
            BossUltimate.SetCutscene(false);
            EndNow();
        }
        else if (running) BossUltimate.SetErosionPending(false);
        DomainStrikes.Clear();
    }

    // ================================================================= 진행
    IEnumerator Run()
    {
        running = true;
        BossUltimate.SetErosionPending(true);
        float waitStart = Time.time;
        // 대기: 돌던 스킬이 끝나기를 (최대 3초) · 대기 중 하나가 쓰러져 하나만 남으면 대기를 풀고 남은 보스가 남은 게이지(1.0)를 그대로 받음
        while (true)
        {
            if (!BossUltimate.Shared || BossUltimate.SharedCount < 2) { BossUltimate.SetErosionPending(false); running = false; yield break; }
            if (Ready(Time.time - waitStart)) break;
            yield return null;
        }

        members.Clear();
        List<bosss> list = BossUltimate.Participants;
        for (int i = 0; i < list.Count && members.Count < MaxMembers; i++) if (list[i] != null && !list[i].IsDead) members.Add(list[i]);
        count = members.Count;
        avg = Color.black;
        for (int i = 0; i < count; i++)
        {
            colors[i] = BossUltimate.ColorOf(BossUltimate.SlotOf(members[i]));
            avg += colors[i] / count;
            down[i] = false;
            members[i].domainHold = true;           // 제자리에 섬 (casting 과 따로 · 개발자 DevRun 이 casting 을 꺼도 걷지 않게)
            // 대기 3초가 지나도 돌던 스킬: 끊음 (침식 피해 배율 · 상한 밖에서 계속 때리거나 돌진으로 자리를 옮기지 않게)
            // 1.2.7~ 무적(의식) · 공중(점프)인 채로 남은 보스도 끊음 (CancelForErosion 이 무적 · 공중 · 받는 피해 배율을 되돌림)
            if (!members[i].developer && members[i].TryGetComponent(out BossSkills sk) && (members[i].casting || members[i].invulnerable || sk.airborne)) sk.CancelForErosion();
            // 개발자 · 엄마: 고유 공격을 끊음 (CancelForErosion 은 개발자에게 돌지 않음 · 영웅 필살기는 지금처럼 계속)
            if (members[i].developer && members[i].TryGetComponent(out BossSkills devSk)) devSk.CancelOwn();
        }
        started = true;
        BossUltimate.BeginErosion();
        PlayerController player = Hostile.Player;
        if (player != null) player.CancelSkill();

        yield return Cutscene();

        player = Hostile.Player;
        Vector3 me = player != null ? player.transform.position : members[0].transform.position;
        Vector3 mid = Vector3.zero;
        for (int i = 0; i < count; i++) mid += members[i].transform.position / count;
        center = Hostile.ClampArena(Vector3.Lerp(me, mid, 0.35f));
        center.z = 0f;
        if (player != null) player.GrantInvincibility(2.4f);          // 펼침 1.2 + 여유 1.2
        BossUltimate.ErosionWalls(this, true);
        yield return Expand();

        float until = Time.time + Duration;
        BossUltimate.SetErosionWindow(Time.time, until);
        int hurtBefore = PlayerController.HurtCount;
        float tempo = Tempo[Mathf.Min(count, Tempo.Length - 1)];
        for (int i = 0; i < count; i++)
        {
            DomainCtx d = ctxs[i];
            d.Reset(BossUltimate.SlotOf(members[i]), members[i], this, center, Radius, until, true, colors[i], root.transform);
            d.members = count;
            d.crowd = count >= 3;
            d.tempo = tempo;
            IEnumerator r = BossUltimate.BarrageFor(d);
            if (r == null) continue;           // 결계 공격이 없는 칸 (개발자 · 엄마 결계가 아직 없을 때)
            d.routine = r;
            StartCoroutine(r);
        }
        float startAt = Time.time, nextTempo = Time.time + 1f;
        bool anyDown = false;
        while (Time.time < until && AnyAlive() && player != null && !player.IsDying)
        {
            for (int i = 0; i < count; i++)
            {
                if (down[i] || (members[i] != null && !members[i].IsDead)) continue;
                // 침식 중 쓰러진 보스: 그 공격만 멈춤 (이미 예고된 타격은 떨어짐) · 후광 · 보스 고리를 지움
                down[i] = true;
                anyDown = true;
                ctxs[i].StopAttack();       // 산성 덩어리 · 개발자 버그 · 하위 패턴도 함께
                ctxs[i].FadeBeams();        // 리치 빔 · 예고선
                ctxs[i].ClearDecor();       // 거울 룬 등
                if (auras[i] != null) Destroy(auras[i]);
                if (bossRings[i] != null) Destroy(bossRings[i].gameObject);
            }
            if (Time.time >= nextTempo)
            {
                // 1초마다 살아 있는 참가 보스 수로 간격 배율을 다시 (하나만 남으면 0.85)
                nextTempo = Time.time + 1f;
                int alive = 0;
                for (int i = 0; i < count; i++) if (!down[i]) alive++;
                float t = Tempo[Mathf.Min(alive, Tempo.Length - 1)];
                for (int i = 0; i < count; i++) ctxs[i].tempo = t;
            }
            Confine(player, Radius);
            RepelMobs(Radius);
            Animate();
            UpdateEye(Time.time - startAt);
            yield return null;
        }
        for (int i = 0; i < count; i++) { ctxs[i].StopAttack(); ctxs[i].FadeBeams(); }
        DomainStrikes.EyeOn = false;
        // 업적: 침식 중 보스를 쓰러뜨림 · 끝까지 버팀 · 한 번도 맞지 않음
        if (anyDown || !AnyAlive()) SteamAchievements.Unlock(SteamAchievements.BarrierBreak);
        if (player != null && !player.IsDying && Time.time >= until)
        {
            SteamAchievements.Unlock(SteamAchievements.BarrierSurvive);
            if (PlayerController.HurtCount == hurtBefore) SteamAchievements.Unlock(SteamAchievements.BarrierNoHit);
        }
        yield return Shatter();
        EndNow();
    }

    // 시작 조건: 창 · 연출 · 쉬는 시간 · 모든 참가 보스가 등장 4초 뒤 · 무적(망자의 의식) · 공중(대점프) 아님 · 돌던 스킬이 끝났거나 3초가 지남
    bool Ready(float waited)
    {
        if (Time.timeScale == 0f || StoryDirector.Playing || PlayerController.Cinematic) return false;
        if (SkillEvolutionUI.Open || WeaponEvolutionUI.Open || ChoiceUI.Open || ESCmenu.IsOpen) return false;
        if (Time.time < BossUltimate.RestUntil) return false;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return false;
        bool overdue = waited >= MaxWait;
        List<bosss> list = BossUltimate.Participants;
        for (int i = 0; i < list.Count; i++)
        {
            bosss b = list[i];
            if (b == null || b.IsDead) continue;
            if (Time.time - b.spawnedAt < 4f) return false;
            // 1.2.7~ 무적(망자의 의식 · 최대 22초) · 공중도 대기 3초가 지나면 기다리지 않음 — 시작할 때 돌던 스킬과 함께 끊음 (Run)
            //   예전엔 끝까지 기다려서, 의식이 끊겨 무적이 남은 리치가 있으면 게이지가 가득 차도 침식이 영원히 발동하지 않았음
            if (overdue) continue;
            if (b.invulnerable) return false;
            BossSkills sk = b.GetComponent<BossSkills>();
            if (sk != null && sk.airborne) return false;
            if (b.developer) { if (sk != null && sk.DevRunning > 0) return false; }
            else if (b.casting) return false;
        }
        return true;
    }

    bool AnyAlive()
    {
        for (int i = 0; i < count; i++) if (members[i] != null && !members[i].IsDead) return true;
        return false;
    }

    // 섞인 색: 참가 보스 색을 0.25초마다 차례로 (계산만 · 할당 없음)
    Color Cycle(float time)
    {
        if (count <= 0) return Color.white;
        float k = time / 0.25f;
        int a = Mathf.FloorToInt(k);
        return Color.Lerp(colors[((a % count) + count) % count], colors[(((a + 1) % count) + count) % count], k - a);
    }

    void EndNow()
    {
        for (int i = 0; i < MaxMembers; i++) { ctxs[i].StopAttack(); ctxs[i].FadeBeams(); ctxs[i].running = false; }
        DomainStrikes.EyeOn = false;
        BossUltimate.ErosionWalls(this, false);
        if (root != null) Destroy(root);
        root = null;
        for (int i = 0; i < MaxMembers; i++) { auras[i] = null; bossRings[i] = null; }
        for (int i = 0; i < count; i++)
        {
            bosss b = members[i];
            if (b == null) continue;
            b.domainHold = false;
            // 개발자 · 엄마: 2초 뒤부터 다시 영웅 필살기
            if (b.developer && b.TryGetComponent(out BossSkills sk)) sk.AfterDomain(2f);
        }
        BossUltimate.EndErosion(members, Rest);
        members.Clear();
        count = 0;
        started = false;
        running = false;
    }

    // ---------------- 1. 컷신 「결계 붕괴 → 공간 침식」: 보스를 차례로 비추며 결계 이름이 쌓이고 → 쾅! 큰 이름으로 빨려 듦
    IEnumerator Cutscene()
    {
        float before = Time.timeScale;
        Time.timeScale = 0f;
        BossUltimate.SetCutscene(true);
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        Vector3 focus = FocusPoint();
        fx?.HoldCamera(focus);

        Canvas canvas = BossUltimate.ErosionCanvas("SpaceErosionCanvas", 30500);
        RectTransform root = (RectTransform)canvas.transform;
        Image shade = BossUltimate.ErosionImg(root, "Shade", new Color(0f, 0f, 0f, 0f));
        BossUltimate.ErosionStretch(shade.rectTransform);
        Image flash = BossUltimate.ErosionImg(root, "Flash", new Color(1f, 1f, 1f, 0f));
        BossUltimate.ErosionStretch(flash.rectTransform);
        Image band = BossUltimate.ErosionImg(root, "Band", new Color(0f, 0f, 0f, 0f));
        band.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        band.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        band.rectTransform.sizeDelta = Vector2.zero;

        UIKit.EnsureStyle();
        TMP_Text tag = BossUltimate.ErosionText(root, "결계 붕괴", 34f, new Color(0.85f, 0.8f, 0.9f), new Vector2(0f, 130f), new Vector2(1400f, 50f));
        TMP_Text name = BossUltimate.ErosionText(root, "공간 침식", 130f, colors[0], new Vector2(0f, 30f), new Vector2(1800f, 170f));
        name.fontStyle = FontStyles.Bold;
        name.outlineWidth = 0.25f;
        name.outlineColor = new Color32(0, 0, 0, 255);
        TMP_Text say = BossUltimate.ErosionText(root, "모든 왕의 영역이, 하나로 무너져 내린다.", 44f, new Color(0.95f, 0.92f, 0.88f), new Vector2(0f, -100f), new Vector2(1600f, 70f));
        // 참가 보스 이름 줄 (컷신 시작 때 한 번)
        bool dev = false, mom = false;
        string names = "";
        for (int i = 0; i < count; i++)
        {
            bosss b = members[i];
            int barKind = b.developer ? (b.developerMom ? bossbar.DeveloperMomKind : bossbar.DeveloperKind) : b.bossKind;
            if (b.developer) { if (b.developerMom) mom = true; else dev = true; }
            names += (i > 0 ? " · " : "") + bossbar.BossName(barKind);
        }
        TMP_Text who = BossUltimate.ErosionText(root, names, 30f, new Color(0.8f, 0.75f, 0.85f), new Vector2(0f, -160f), new Vector2(1600f, 44f));
        TMP_Text aside = dev && mom ? BossUltimate.ErosionText(root, "…엄마, 지금 결계 쓰는 중이라니까요.", 24f, new Color(0.75f, 0.72f, 0.8f), new Vector2(0f, -205f), new Vector2(1600f, 36f)) : null;
        TMP_Text skip = BossUltimate.ErosionText(root, GamePad.Active ? "[A]: 건너뛰기" : "클릭 · Space: 건너뛰기", 24f, new Color(0.75f, 0.72f, 0.8f), new Vector2(0f, -330f), new Vector2(900f, 36f));
        TMP_Text[] subs = new TMP_Text[count];
        Vector2[] subFrom = new Vector2[count];
        // 이름 줄: 다섯 · 여섯 명이면 위에서 시작하고 간격을 줄여 「결계 붕괴」(y 130) 와 겹치지 않게 (마지막 줄 y 200 위)
        float subTop = count <= 4 ? 420f : 470f, subStep = count > 1 ? Mathf.Min(58f, 270f / (count - 1)) : 58f;
        for (int i = 0; i < count; i++)
        {
            int slot = BossUltimate.SlotOf(members[i]);
            subFrom[i] = new Vector2(0f, subTop - i * subStep);
            subs[i] = BossUltimate.ErosionText(root, BossUltimate.TitleOf(slot), 48f, colors[i], subFrom[i], new Vector2(1200f, 64f));
            subs[i].alpha = 0f;
        }
        skip.alpha = 0f;
        tag.alpha = name.alpha = say.alpha = who.alpha = 0f;
        if (aside != null) aside.alpha = 0f;

        bool brief = erosionSeen;
        erosionSeen = true;
        float darken = brief ? 0.2f : 0.7f, hold = brief ? 0.6f : 2.3f;
        float flashA = GameSettings.Flashes ? 0.55f : 0f, flicker = GameSettings.Flashes ? 0.1f : 0f;
        float shakeMul = GameSettings.ScreenShake ? 1f : 0f;
        bool skipped = false;

        for (float t = 0f; t < darken && !skipped; t += BossUltimate.ErosionCutDt(canvas))
        {
            shade.color = new Color(0f, 0f, 0f, 0.55f * (t / darken));
            fx?.HoldCamera(focus);
            if (t > 0.1f) skipped = BossUltimate.ErosionSkip();
            yield return null;
        }

        // 보스를 차례로 비춤 (보스마다 0.35초 · 울음 · 결계 이름이 위쪽에 쌓임) — 두 번째부터는 건너뜀
        if (!brief)
        {
            for (int i = 0; i < count && !skipped; i++)
            {
                bosss b = members[i];
                Hostile.Play("roar", 0.7f, BossUltimate.RoarPitch(BossUltimate.SlotOf(b)));
                for (float t = 0f; t < CutHop && !skipped; t += BossUltimate.ErosionCutDt(canvas))
                {
                    if (b != null) fx?.HoldCamera(b.transform.position);
                    subs[i].alpha = Mathf.Clamp01(t / 0.15f);
                    subs[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, BossUltimate.Ease(t / 0.2f));
                    skipped = BossUltimate.ErosionSkip();
                    yield return null;
                }
                subs[i].alpha = 1f;
            }
        }

        // 쾅! 큰 이름 「공간 침식」 (참가 보스 색을 차례로 섞음) · 쌓인 이름들이 가운데로 빨려 듦
        BossUltimate.ErosionSfx("domain_shout", 1f);
        fx?.Shake(1.1f, 0.6f);
        for (float t = 0f; t < hold && !skipped; t += BossUltimate.ErosionCutDt(canvas))
        {
            float k = Mathf.Clamp01(t / 0.25f);
            Color c = Cycle(Time.unscaledTime);
            flash.color = new Color(c.r, c.g, c.b, flashA * (1f - Mathf.Clamp01(t / 0.35f)));
            band.rectTransform.sizeDelta = new Vector2(0f, Mathf.Lerp(0f, 440f, BossUltimate.Ease(k)));
            band.color = new Color(0f, 0f, 0f, 0.75f);
            tag.alpha = name.alpha = skip.alpha = k;
            name.color = new Color(c.r, c.g, c.b, k);
            name.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, BossUltimate.Ease(k));
            float shake = Mathf.Lerp(20f, 4f, Mathf.Clamp01(t / 1.5f)) * shakeMul;
            name.rectTransform.anchoredPosition = new Vector2(0f, 30f) + Random.insideUnitCircle * shake;
            float late = brief ? k : Mathf.Clamp01((t - 0.45f) / 0.3f);
            say.alpha = who.alpha = late;
            if (aside != null) aside.alpha = brief ? k : Mathf.Clamp01((t - 0.8f) / 0.3f);
            say.rectTransform.anchoredPosition = new Vector2(0f, -100f) + Random.insideUnitCircle * shake * 0.4f;
            float pull = Mathf.Clamp01(t / 0.3f);
            for (int i = 0; i < count; i++)
            {
                if (brief) break;
                subs[i].rectTransform.anchoredPosition = Vector2.Lerp(subFrom[i], new Vector2(0f, 30f), BossUltimate.Ease(pull));
                subs[i].alpha = 1f - pull;
                subs[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, pull);
            }
            shade.color = new Color(c.r * 0.15f, c.g * 0.05f, c.b * 0.1f, 0.6f + flicker * Mathf.Sin(t * 20f));
            fx?.HoldCamera(focus);
            if (t > 0.1f) skipped = BossUltimate.ErosionSkip();
            yield return null;
        }
        flash.color = Color.clear;
        skip.alpha = 0f;
        for (int i = 0; i < count; i++) subs[i].alpha = 0f;
        float fade = brief || skipped ? 0.2f : 0.3f;
        for (float t = 0f; t < fade; t += BossUltimate.ErosionCutDt(canvas))
        {
            float k = 1f - t / fade;
            tag.alpha = name.alpha = say.alpha = who.alpha = k;
            if (aside != null) aside.alpha = k;
            band.color = new Color(0f, 0f, 0f, 0.75f * k);
            shade.color = new Color(0f, 0f, 0f, 0.6f * k);
            yield return null;
        }
        Destroy(canvas.gameObject);
        fx?.ReleaseCamera();
        BossUltimate.SetCutscene(false);
        Time.timeScale = before > 0f ? before : 1f;
    }

    Vector3 FocusPoint()
    {
        Vector3 mid = Vector3.zero;
        for (int i = 0; i < count; i++) if (members[i] != null) mid += members[i].transform.position / count;
        PlayerController p = Hostile.Player;
        return p != null ? Vector3.Lerp(p.transform.position, mid, 0.35f) : mid;
    }

    // ---------------- 2. 펼침 1.2초: 보스마다 자기 색 고리 → 가운데로 모이며 거대한 고리가 48 까지
    IEnumerator Expand()
    {
        root = new GameObject("SpaceErosionDomain");
        root.transform.position = center;
        BossUltimate.ErosionSfx("domain_open", 1f);
        Hostile.Shake(0.6f);

        dark = BossUltimate.ErosionDark(root.transform, Radius);
        ring = Hostile.NewLine("ErosionRing", colors[0], 1f, 40);
        ring.loop = true;
        ring.transform.SetParent(root.transform, true);
        ring2 = Hostile.NewLine("ErosionRing2", Color.white, 0.25f, 41);
        ring2.loop = true;
        ring2.transform.SetParent(root.transform, true);
        FxAnim rune = Fx.Play("fx_rune", center, Radius * 2f, new Color(avg.r, avg.g, avg.b, 0.25f), 1f, 0f, 41, true, -1f, "Background");
        if (rune != null) { rune.spin = 9f; rune.transform.SetParent(root.transform, true); }

        // 보스마다 고리 · 후광 (root 아래 · 끝날 때 함께 지워짐)
        Vector3[] from = new Vector3[count], to = new Vector3[count];
        PlayerController pl = Hostile.Player;
        Vector3 me = pl != null ? pl.transform.position : center;
        for (int i = 0; i < count; i++)
        {
            bosss b = members[i];
            bossRings[i] = Hostile.NewLine("ErosionBossRing", colors[i], 0.6f, 39);
            bossRings[i].loop = true;
            bossRings[i].transform.SetParent(root.transform, true);
            if (Hostile.Glow != null)
            {
                auras[i] = SpecialAbilities.MakeSprite("ErosionAura", Hostile.Glow, b.transform.position, 1.2f, new Color(colors[i].r, colors[i].g, colors[i].b, 0.5f), "Effect", 1);
                auras[i].transform.SetParent(root.transform, true);
            }
            // 반지름 − 6 밖에 있는 보스는 안쪽(반지름 − 8)으로 미끄러져 들어옴 (플레이어로부터 최소 6)
            from[i] = to[i] = b.transform.position;
            Vector3 off = b.transform.position - center;
            off.z = 0f;
            if (off.magnitude > Radius - 6f)
            {
                Vector3 dir = off.sqrMagnitude > 0.01f ? off.normalized : Vector3.right;
                Vector3 at = center + dir * (Radius - 8f);
                for (int step = 0; step < 12 && Vector2.Distance(at, me) < 6f; step++)
                    at = center + Quaternion.Euler(0f, 0f, 30f * (step + 1)) * dir * (Radius - 8f);
                to[i] = new Vector3(at.x, at.y, b.transform.position.z);
            }
        }
        // 공간의 금: 가운데에서 바깥으로 뻗는 지그재그 6줄 (좌표는 한 번만 · 이후 투명도만 맥동)
        for (int i = 0; i < cracks.Length; i++)
        {
            Color cc = colors[i % Mathf.Max(1, count)];
            cracks[i] = Hostile.NewLine("ErosionCrack", new Color(cc.r, cc.g, cc.b, 0f), 0.22f, 41);      // 어둠 메시(40) 위
            cracks[i].sortingLayerName = "Background";
            cracks[i].transform.SetParent(root.transform, true);
            cracks[i].positionCount = 8;
            float a = (i * 60f + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a)), side = new Vector3(-d.y, d.x);
            for (int k = 0; k < 8; k++)
            {
                float r = Mathf.Lerp(3f, Radius - 1f, k / 7f);
                cracks[i].SetPosition(k, center + d * r + side * (k == 0 ? 0f : Random.Range(-2.2f, 2.2f)));
            }
        }
        if (Hostile.Glow != null)
        {
            spot = SpecialAbilities.MakeSprite("DomainSpot", Hostile.Glow, center, 0.55f, new Color(1f, 0.97f, 0.88f, 0.42f), "Background", 42);
            spot.transform.SetParent(root.transform, true);
        }
        spotRing = Hostile.NewLine("DomainSpotRing", Color.white, 0.12f, 43);
        spotRing.loop = true;
        spotRing.sortingLayerName = "Background";
        spotRing.transform.SetParent(root.transform, true);
        Fx.Spawn("fx_soulburst", center, 14f, Color.white, 14f);

        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            if (t < 0.4f)
            {
                float k = BossUltimate.Ease(t / 0.4f);
                for (int i = 0; i < count; i++)
                    if (bossRings[i] != null && members[i] != null) Hostile.SetArc(bossRings[i], members[i].transform.position, Mathf.Lerp(0.5f, 8f, k), 0f, 360f);
            }
            else
            {
                float k = (t - 0.4f) / 0.8f, e = BossUltimate.Ease(k);
                float r = Mathf.Lerp(0.5f, Radius, e);
                BossUltimate.PaintDark(dark, avg, e);
                Hostile.SetArc(ring, center, r, 0f, 360f);
                Hostile.SetArc(ring2, center, r - 0.35f, 0f, 360f);
                Color rc = Cycle(Time.time);
                ring.startColor = ring.endColor = rc;
                for (int i = 0; i < count; i++)
                {
                    if (members[i] != null) members[i].transform.position = Vector3.Lerp(from[i], to[i], e);
                    if (bossRings[i] == null || members[i] == null) continue;
                    Vector3 at = Vector3.Lerp(members[i].transform.position, center, e);
                    Hostile.SetArc(bossRings[i], at, Mathf.Lerp(8f, 2f, e), 0f, 360f);
                    Color c = colors[i];
                    bossRings[i].startColor = bossRings[i].endColor = new Color(c.r, c.g, c.b, 1f - e);
                }
                for (int i = 0; i < cracks.Length; i++) { Color c = cracks[i].startColor; c.a = 0.6f * e; cracks[i].startColor = cracks[i].endColor = c; }
                PlayerController p = Hostile.Player;
                if (p != null && r > 4f && (p.transform.position - center).magnitude > r - 0.8f) Confine(p, r);
                RepelMobs(r);
            }
            yield return null;
        }
        BossUltimate.PaintDark(dark, avg, 1f);
        Hostile.SetArc(ring, center, Radius, 0f, 360f);
        for (int i = 0; i < count; i++)
        {
            if (members[i] != null) members[i].transform.position = to[i];
            if (bossRings[i] != null) { Destroy(bossRings[i].gameObject); bossRings[i] = null; }
        }
        Fx.Spawn("fx_shock", center, Radius * 2.4f, avg, 14f);
    }

    void Animate()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
        Color c = Cycle(Time.time);
        if (ring != null)
        {
            ring.startWidth = ring.endWidth = 0.9f + 0.3f * pulse;
            ring.startColor = ring.endColor = Color.Lerp(c, Color.white, 0.25f * pulse);
        }
        if (ring2 != null) Hostile.SetArc(ring2, center, Radius - 0.4f, Time.time * 30f, Time.time * 30f + 360f);
        for (int i = 0; i < cracks.Length; i++)
        {
            if (cracks[i] == null) continue;
            Color cc = cracks[i].startColor;
            cc.a = 0.35f + 0.3f * Mathf.Sin(Time.time * 3f + i);
            cracks[i].startColor = cracks[i].endColor = cc;
        }
        PlayerController pl = Hostile.Player;
        if (pl != null)
        {
            Vector3 at = pl.transform.position;
            if (spot != null)
            {
                spot.transform.position = at;
                spot.transform.localScale = Vector3.one * (0.55f + 0.05f * pulse);
            }
            if (spotRing != null)
            {
                Hostile.SetArc(spotRing, at, 1.6f + 0.1f * pulse, 0f, 360f);
                spotRing.startColor = spotRing.endColor = Color.Lerp(Color.white, c, 0.35f);
            }
        }
        for (int i = 0; i < count; i++)
        {
            if (auras[i] == null || members[i] == null) continue;
            auras[i].transform.position = members[i].transform.position;
            auras[i].transform.localScale = Vector3.one * (1.2f + 0.2f * pulse);
        }
        // 결계 가장자리에서 튀는 불꽃 (참가 보스 색 가운데 무작위)
        if (Random.value < 0.4f && count > 0)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Fx.Spawn("fx_spark", center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius, 2f, colors[Random.Range(0, count)], 18f);
        }
    }

    // ---------------- 고요의 눈: 땅을 치는 공격이 떨어지지 않는 떠도는 안전 지대 (5초마다 플레이어로부터 7 ~ 11 떨어진 새 자리로)
    Vector3 eyeFrom, eyeTo;
    float eyeNextMove, eyeGlideAt = -1f;
    bool eyePreview;

    void UpdateEye(float elapsed)
    {
        if (elapsed < EyeStart) return;
        float now = Time.time;
        if (!DomainStrikes.EyeOn)
        {
            if (eyeGlow == null && Hostile.Glow != null)
            {
                float unit = Mathf.Max(0.01f, Hostile.Glow.bounds.size.x);
                eyeGlow = SpecialAbilities.MakeSprite("ErosionEye", Hostile.Glow, center, DomainStrikes.EyeRadius * 2f / unit, new Color(1f, 1f, 1f, 0.22f), "Background", 44);
                eyeGlow.transform.SetParent(root.transform, true);
            }
            for (int i = 0; i < eyeDash.Length; i++)
            {
                eyeDash[i] = Hostile.NewLine("ErosionEyeDash", new Color(1f, 1f, 1f, 0.8f), 0.14f, 44);
                eyeDash[i].sortingLayerName = "Background";
                eyeDash[i].transform.SetParent(root.transform, true);
            }
            previewRing = Hostile.NewLine("ErosionEyePreview", new Color(1f, 1f, 1f, 0f), 0.1f, 44);
            previewRing.loop = true;
            previewRing.sortingLayerName = "Background";
            previewRing.transform.SetParent(root.transform, true);
            DomainStrikes.EyePos = PickEye();
            DomainStrikes.EyeOn = true;
            eyeNextMove = now + EyeMoveEvery;
            eyeGlideAt = -1f;
            eyePreview = false;
        }
        // 옮기기 1초 전: 새 자리에 점선 고리를 미리 보임 → 0.6초 동안 미끄러져 감
        if (!eyePreview && now >= eyeNextMove - EyePreview)
        {
            eyePreview = true;
            eyeTo = PickEye();
        }
        if (eyeGlideAt < 0f && now >= eyeNextMove)
        {
            eyeGlideAt = now;
            eyeFrom = DomainStrikes.EyePos;
        }
        if (eyeGlideAt >= 0f)
        {
            float k = (now - eyeGlideAt) / EyeGlide;
            DomainStrikes.EyePos = Vector3.Lerp(eyeFrom, eyeTo, BossUltimate.Ease(k));
            if (k >= 1f) { eyeGlideAt = -1f; eyePreview = false; eyeNextMove = now + EyeMoveEvery - EyeGlide; }
        }
        Vector3 at = DomainStrikes.EyePos;
        if (eyeGlow != null)
        {
            eyeGlow.transform.position = at;
            float unit = Mathf.Max(0.01f, Hostile.Glow.bounds.size.x);
            eyeGlow.transform.localScale = Vector3.one * (DomainStrikes.EyeRadius * 2f / unit) * (1f + 0.04f * Mathf.Sin(now * 3f));
        }
        float spin = now * 25f;
        for (int i = 0; i < eyeDash.Length; i++)
            if (eyeDash[i] != null) Hostile.SetArc(eyeDash[i], at, DomainStrikes.EyeRadius, spin + i * 30f, spin + i * 30f + 18f);
        if (previewRing != null)
        {
            Color pc = new Color(1f, 1f, 1f, eyePreview ? 0.35f + 0.25f * Mathf.Sin(now * 10f) : 0f);
            previewRing.startColor = previewRing.endColor = pc;
            if (eyePreview) Hostile.SetArc(previewRing, eyeTo, DomainStrikes.EyeRadius, 0f, 360f);
        }
    }

    Vector3 PickEye()
    {
        PlayerController p = Hostile.Player;
        Vector3 me = p != null ? p.transform.position : center;
        float inner = Radius - DomainStrikes.EyeRadius - 1f;
        Vector3 inside = Vector3.zero;
        bool any = false;
        for (int i = 0; i < 24; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 at = me + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(7f, 11f);
            at.z = 0f;
            if ((at - center).sqrMagnitude > inner * inner) continue;
            if (!EndlessTerrain.NearZone(at, DomainStrikes.EyeRadius + 0.5f)) return at;     // 안전 자리가 용암 · 늪 위에 생기지 않게
            if (!any) { any = true; inside = at; }
        }
        if (any) return inside;
        Vector3 toCenter = center - me;
        toCenter.z = 0f;
        Vector3 fallback = me + (toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector3.right) * 8f;
        fallback.z = 0f;
        return fallback;
    }

    // ---------------- 3. 파열 0.8초: 가장자리 불꽃 48 (참가 보스 색을 돌아가며) · 충격파 두 번
    IEnumerator Shatter()
    {
        BossUltimate.ErosionSfx("domain_break", 1f);
        Hostile.Shake(0.5f);
        for (int i = 0; i < 48; i++)
        {
            float a = i * 7.5f * Mathf.Deg2Rad;
            Vector3 at = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius;
            Color c = count > 0 ? colors[i % count] : Color.white;
            Fx.Spawn("fx_spark", at, 2.8f, Color.Lerp(c, Color.white, 0.4f), 16f);
        }
        Fx.Spawn("fx_shock", center, Radius * 2.4f, Color.white, 14f);
        bool second = false;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            if (!second && t >= 0.2f) { second = true; Fx.Spawn("fx_shock", center, Radius * 2.0f, avg, 14f); }
            float k = 1f - t / 0.8f;
            BossUltimate.PaintDark(dark, avg, k);
            Color c = Cycle(Time.time);
            if (ring != null) ring.startColor = ring.endColor = new Color(c.r, c.g, c.b, k);
            if (ring2 != null) ring2.startColor = ring2.endColor = new Color(1f, 1f, 1f, k);
            for (int i = 0; i < cracks.Length; i++)
                if (cracks[i] != null) { Color cc = cracks[i].startColor; cc.a = 0.6f * k; cracks[i].startColor = cracks[i].endColor = cc; }
            yield return null;
        }
    }

    // ================================================================= 가두기 · 잡몹 밀어내기 (침식에서는 이곳 한 곳에서만)
    void Confine(PlayerController p, float r)
    {
        if (p == null) return;
        Vector3 d = p.transform.position - center;
        d.z = 0f;
        float max = r - 0.8f;
        if (d.magnitude > max) p.transform.position = new Vector3(center.x, center.y, p.transform.position.z) + d.normalized * max;
    }

    // 켜진 적 목록(EnermyController.Live)을 그대로 씀 (장면 검색 · 배열 할당 없음)
    void RepelMobs(float r)
    {
        List<EnermyController> mobs = EnermyController.Live;
        float edge = r + 0.9f;
        int sparks = 0;
        for (int m = mobs.Count - 1; m >= 0; m--)
        {
            EnermyController e = mobs[m];
            if (e == null) continue;
            Vector3 pos = e.transform.position;
            Vector3 d = pos - center;
            d.z = 0f;
            if (d.sqrMagnitude >= edge * edge) continue;
            if (d.sqrMagnitude < 0.01f) d = Vector3.right;
            Vector3 to = BossUltimate.EdgeInArenaFrom(center, d, edge);
            to.z = pos.z;
            if ((to - pos).sqrMagnitude > 1.5f * 1.5f && sparks++ < 6) Fx.Spawn("fx_spark", to, 2.2f, Color.white, 18f);
            e.transform.position = to;
        }
        // 침식에 들지 않은 보스(뒤늦게 나온 보스 등)도 밖으로 (BossUltimate.RepelMobs 와 같은 방식)
        float bossEdge = r + 2.2f;
        foreach (bosss o in bosss.Live)
        {
            if (o == null || o.IsDead || members.Contains(o)) continue;
            Vector3 pos = o.transform.position;
            Vector3 d = pos - center;
            d.z = 0f;
            if (d.sqrMagnitude >= bossEdge * bossEdge) continue;
            if (d.sqrMagnitude < 0.01f) d = Vector3.left;
            Vector3 to = BossUltimate.EdgeInArenaFrom(center, d, bossEdge);
            to.z = pos.z;
            o.transform.position = to;
        }
    }
}
