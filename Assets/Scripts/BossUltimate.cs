using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보스 필살기 "결계" (2.1~): 보스를 때릴 때마다 필살기 게이지가 차고 (한 번에 세게 때릴수록 훨씬 많이),
// 가득 차면 → 게임이 멈추고 카메라가 보스에게 → 화면 가운데에 결계 이름 · 대사가 흔들리며 터짐 (웅장한 외침)
// → 맵 전체가 어두워지고 보스 둘레에 둥근 결계가 펼쳐져 플레이어가 그 안에 갇힘 → 9초 동안 보스마다 다른 공격이 쏟아짐
// (모두 경고를 먼저 보여 줘서 피할 수는 있음) → 결계가 깨지며 끝
// 킹 슬라임은 갈라져도 게이지를 함께 씀 (한 번에 하나만)
public partial class BossUltimate : MonoBehaviour
{
    public static bool Active { get; private set; }
    // 결계 이름 · 대사 연출 중 (게임이 멈춰 있음) — 일시정지 메뉴를 닫을 때 다시 흐르게 하지 않도록 (ESCmenu)
    public static bool CutscenePlaying { get; private set; }

    // 게이지 칸 (0 ~ 1): 0 ~ 3 보스 종류 · 4 개발자 · 5 개발자 엄마
    // (개발자 · 엄마는 bossKind 가 거울의 군주와 같은 3이라, 종류 번호 그대로 쓰면 거울의 군주 칸 · 「거울의 군주를 결계 전에」 업적이 오염됨)
    public const int Slots = 6, DevSlot = 4, MomSlot = 5;
    const float DevGaugeMul = 2.5f, MomGaugeMul = 4f;      // 1.2.7~ 개발자 · 엄마 결계 게이지 차는 속도 (OnBossHit)
    static readonly float[] gauge = new float[Slots];
    static readonly float[] refHp = new float[Slots];
    // 마지막으로 맞은 때 (3초 동안 안 맞으면 게이지가 아주 서서히 줄어듦) · 이번 보스전에 결계를 펼쳤는지
    static readonly float[] lastHit = new float[Slots];
    static readonly bool[] usedThisFight = new bool[Slots];
    static readonly int[] decayFrame = { -1, -1, -1, -1, -1, -1 };      // 칸마다 (보스가 둘이면 한 칸만 줄던 것)
    public static int SlotOf(bosss b) => b == null ? 0 : b.developer ? (b.developerMom ? MomSlot : DevSlot) : Mathf.Clamp(b.bossKind, 0, 3);
    // 보스바 종류 번호(0 ~ 3 · 개발자 9 · 엄마 10)로 그 칸의 게이지
    public static float Gauge(int kind)
    {
        int s = kind == bossbar.DeveloperKind ? DevSlot : kind == bossbar.DeveloperMomKind ? MomSlot : kind;
        return s >= 0 && s < gauge.Length ? gauge[s] : 0f;
    }
    // 이번 보스전에 결계를 펼쳤는지 (업적: 거울의 군주를 결계 전에 · 칸 번호, 0 ~ 3 은 보스 종류와 같음)
    public static bool UsedThisFight(int kind) => kind >= 0 && kind < usedThisFight.Length && usedThisFight[kind];

    const float Radius = 35f;             // 13 은 너무 좁아 피할 틈이 없었음 (화면보다 넓게)
    const float Duration = 15f;
    const float SlimeDuration = 9f;     // 2.2.1: 1장 킹 슬라임 결계는 짧고 단순하게 (튕기는 덩어리 최대 3 · 산성 비만)
    static float Power => 1.25f * Chapters.BossPowerMul;      // BossSkills 와 같은 피해 배율 (3장부터 더 셈)

    static readonly (string title, string line, Color color)[] Info =
    {
        ("망자의 묘역", "이 안에서는, 죽은 자만이 걷는다.", new Color(0.55f, 0.95f, 1f)),
        ("연옥 낙화", "하늘이 무너져 내린다.", new Color(1f, 0.45f, 0.12f)),
        ("산성 범람", "전부, 녹아내려라.", new Color(0.55f, 1f, 0.35f)),
        ("거울의 방", "네가 쌓아 온 모든 것을, 그대로 돌려주마.", new Color(0.78f, 0.9f, 1f)),
        ("디버그 모드", "이 세계의 규칙은, 내가 쓴다.", new Color(1f, 0.85f, 0.35f)),                 // 개발자 (BossUltimate.Developer.cs)
        ("엄마의 대청소", "다 치울 때까지, 아무 데도 못 나가.", new Color(1f, 0.6f, 0.78f)),          // 개발자 엄마 (EndlessMode.MomTint)
    };

    bosss boss;
    int kind;
    float spawnedAt;
    static AudioSource sfx;

    void Start()
    {
        boss = GetComponent<bosss>();
        kind = SlotOf(boss);
        spawnedAt = Time.time;
        // 새 보스 (킹 슬라임은 첫 덩어리)가 나오면 게이지를 비우고 기준 체력을 정함
        if (boss != null && (kind != 2 || boss.slimeGen == 1))
        {
            gauge[kind] = 0f;
            refHp[kind] = Mathf.Max(1f, boss.restoreMaxHp > 0 ? boss.restoreMaxHp : boss.setEnemyHP);
            // 1.2.3~ 무한 이어하기로 되살린 보스: 저장 때 이미 체력 40% 아래였으면 결계를 썼던 것으로 (이어하자마자 4초 만에 다시 펼치지 않게 · 게이지는 저장하지 않음)
            usedThisFight[kind] = boss.restoreMaxHp > 0 && boss.restoreHp > 0f && boss.restoreHp < boss.restoreMaxHp * 0.4f;
            lastHit[kind] = Time.time;
        }
    }

    void OnDestroy()
    {
        if (running)
        {
            // 컷신 도중 장면이 바뀌면 (ESC 「메인 메뉴로」 등) 멈춘 시간을 되돌림
            if (CutscenePlaying) Time.timeScale = 1f;
            CutscenePlaying = false;
            EndNow();
        }
        DevHoldRelease(true);
    }

    // 보스가 맞을 때 (bosss.TakeDamage): 피해 비율의 1.2제곱 → 약한 공격 여러 번보다 강한 한 방이 더 많이 채움
    // (예전 5 · 1.3제곱은 너무 늦게 차서, 한 판에 약한 공격만으로도 두세 번 · 센 공격 위주면 네댓 번 차게)
    public static void OnBossHit(bosss b, float damage)
    {
        if (b == null || Active || b.IsDead || damage <= 0f) return;     // 개발자 · 엄마도 자기 칸(4 · 5)에 참 (체력 비율 기준이라 체력이 커도 같은 비율로)
        int k = SlotOf(b);
        float f = damage / Mathf.Max(1f, refHp[k] > 0f ? refHp[k] : b.setEnemyHP);
        // 약한 공격도 피해 비율의 3.5배는 채움 (1.2제곱만이면 평타가 거의 안 차서 · 보스 체력 3% 이상의 센 한 방은 그대로)
        float gain = Mathf.Max(7f * Mathf.Pow(f, 1.2f), 3.5f * f) * SoulWeight.BarrierGaugeMul;     // 1.2~ 영혼의 무게 3: 더 빨리 참
        // 1.2.7~ 개발자 · 엄마는 체력이 10만 · 20만이라 비율로만 차면 결계가 너무 드묾 → 개발자 2.5배 · 엄마 4배 빨리
        // (약한 공격 기준 한 번 차는 데 드는 피해: 보통 보스 체력 28.6% → 개발자 11.4% · 엄마 7.1%)
        if (k == DevSlot) gain *= DevGaugeMul; else if (k == MomSlot) gain *= MomGaugeMul;
        if (shared) { SharedHit(b, k, Mathf.Min(0.5f, gain)); return; }      // 1.2.5~ 보스가 둘 이상이면 통합 게이지 (BossUltimate.Erosion.cs)
        gauge[k] = Mathf.Min(1f, gauge[k] + Mathf.Min(0.5f, gain));
        lastHit[k] = Time.time;
    }

    void Update()
    {
        // 1.2.5~ 통합 게이지 (한 프레임 한 번) · 통합 모드면 개별 줄어듦 · 40% · 발동은 통합 게이지가 맡음 (BossUltimate.Erosion.cs)
        SharedTick();
        if (shared) { if (kind >= DevSlot) DevHoldRelease(true); return; }
        if (!Active && boss != null && !boss.IsDead)
        {
            // 3초 동안 안 맞으면 게이지가 아주 서서히 (초당 0.4%) 줄어듦 · 갈라진 슬라임은 한 번만 줄게
            if (decayFrame[kind] != Time.frameCount && Time.time - lastHit[kind] > 3f && gauge[kind] < 1f)
            {
                decayFrame[kind] = Time.frameCount;
                gauge[kind] = Mathf.Max(0f, gauge[kind] - 0.004f * Time.deltaTime);
            }
            // 보스전마다 적어도 한 번: 아직 펼치지 않았는데 체력이 40% 아래로 내려가면 게이지가 가득 참
            if (!usedThisFight[kind] && boss.setEnemyHP > 0 && boss.EnemyHealth < boss.setEnemyHP * 0.4f)
                gauge[kind] = 1f;
        }
        if (kind >= DevSlot) DevHoldRelease();      // 개발자 · 엄마 대기가 풀릴 일 (쓰러짐 · 다른 결계) — BossUltimate.Developer.cs
        if (Active || boss == null || boss.IsDead || gauge[kind] < 1f) { castWaitSince = -1f; return; }
        // 개발자 · 엄마는 겹쳐 쓰는 영웅 필살기로 casting 이 늘 켜져 있어 아래 DevReady 가 따로 기다림 (최대 3초)
        if (Time.time - spawnedAt < 4f || Time.time < restUntil || Time.timeScale == 0f || StoryDirector.Playing) return;
        if (boss.casting && kind < DevSlot)
        {
            // 1.2.6~ 게이지가 찬 뒤 3초가 지나도 시전이 이어지면 끊고 펼침 (리치처럼 시전이 쉬지 않고 이어지는 보스는 결계가 20초 넘게 늦었음 · DevReady 와 같은 3초)
            if (castWaitSince < 0f) castWaitSince = Time.time;
            if (Time.time - castWaitSince < 3f || (kind == 2 && !IsStrongestSlime()) || !boss.TryGetComponent(out BossSkills sk)) return;
            sk.CancelForErosion();
            if (boss.casting) return;
        }
        if (SkillEvolutionUI.Open || WeaponEvolutionUI.Open || ChoiceUI.Open || ESCmenu.IsOpen) return;
        // 킹 슬라임: 살아 있는 덩어리 중 체력이 가장 많은 하나만 씀
        if (kind == 2 && !IsStrongestSlime()) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        if (kind >= DevSlot && !DevReady()) return;
        castWaitSince = -1f;
        StartCoroutine(Run());
    }

    float castWaitSince = -1f;      // 1.2.6~ 게이지가 찬 채 시전이 끝나기를 기다린 시작 시각

    static readonly List<bosss> slimeBuf = new List<bosss>(4);      // 1.2.5: 부를 때마다 new List 를 만들었음 → 하나를 비워 다시 씀

    bool IsStrongestSlime()
    {
        bosss.CollectLiveSlimes(slimeBuf);
        foreach (bosss s in slimeBuf) if (s != boss && s.EnemyHealth > boss.EnemyHealth) return false;
        return true;
    }

    // ================================================================= 진행
    bool running;
    Vector3 center;
    GameObject domainRoot;
    LineRenderer ring, ring2;
    FxAnim rune;
    GameObject aura;
    GameObject spot;
    LineRenderer spotRing;

    IEnumerator Run()
    {
        running = true;
        Active = true;
        // 개발자 · 엄마: 돌던 고유 공격을 끊고 표시 · 경고 · 메달 · 멈춘 Ctrl+Z 탄을 정리 (컷신 뒤 결계 안에 남지 않게 · BossSkills.OwnKit)
        if (boss != null && boss.developer && boss.TryGetComponent(out BossSkills ownSk)) ownSk.CancelOwn();
        SetWindowPending();         // 컷신 · 펼침 동안 보스바는 가득 (BossUltimate.Erosion.cs)
        gauge[kind] = 0f;
        usedThisFight[kind] = true;
        boss.casting = true;
        PlayerController player = Hostile.Player;
        player.CancelSkill();
        var info = Info[kind];

        yield return Cutscene(info.title, info.line, info.color);

        center = Hostile.ClampArena(boss.transform.position);
        // 멈췄던 화면이 풀리는 순간 옆의 잡몹 · 날아오던 탄 · 보스 몸통에 한꺼번에 맞지 않게 (결계가 다 펼쳐지고 1.2초까지 무적)
        if (player != null) player.GrantInvincibility(2f);
        HideWalls(true);
        yield return Expand(info.color);
        float until = Time.time + (kind == 2 ? SlimeDuration : kind == DevSlot ? DevDuration : kind == MomSlot ? MomDuration : Duration);
        SetActiveWindow(Time.time, until);      // 1.2.5~ 보스바가 결계 중 남은 시간을 그림 (BossUltimate.Erosion.cs)
        int hurtBefore = PlayerController.HurtCount;
        IEnumerator attack;
        if (kind >= DevSlot) attack = DevDomainAttack(until);
        else
        {
            // 1.2.5~ 결계 공격은 맥락(DomainCtx) 기반 static (공간 침식과 같은 코드 · 개별은 tempo 1 이라 예전과 같음)
            soloCtx.Reset(kind, boss, this, center, Radius, until, false, info.color, domainRoot.transform);
            attack = BarrageFor(soloCtx);
            soloCtx.routine = attack;
        }
        StartCoroutine(attack);
        while (Time.time < until && boss != null && !boss.IsDead && player != null && !player.IsDying)
        {
            // 엄마 「통금 시간」이 결계 반지름 · 고리 색을 바꿈 (다른 결계는 늘 Radius · 결계 색)
            radiusNow = kind >= DevSlot ? devCtx.radius : Radius;
            Confine(player, radiusNow);
            RepelMobs(radiusNow);
            Animate(kind >= DevSlot ? devCtx.ringTint : info.color);
            yield return null;
        }
        StopCoroutine(attack);
        DevDomainStop();
        FadeLichBeams();
        // 업적: 결계 중 보스를 쓰러뜨림 · 끝까지 버팀 · 한 번도 맞지 않음
        if (boss == null || boss.IsDead) SteamAchievements.Unlock(SteamAchievements.BarrierBreak);
        else if (player != null && !player.IsDying && Time.time >= until)
        {
            SteamAchievements.Unlock(SteamAchievements.BarrierSurvive);
            if (PlayerController.HurtCount == hurtBefore) SteamAchievements.Unlock(SteamAchievements.BarrierNoHit);
        }
        yield return Shatter(info.color);
        EndNow();
    }

    // 결계가 끝난 뒤 쉬는 시간 (게이지가 빨리 차도 연달아 펼치지 않게)
    static float restUntil;

    void EndNow()
    {
        FadeLichBeams();
        soloCtx.running = false;        // 남은 불씨 운석 등 같이 멈춤 (이미 예고된 땅 타격은 끝까지 떨어짐)
        DevDomainStop();
        HideWalls(false);
        restUntil = Time.time + 15f;
        if (domainRoot != null) Destroy(domainRoot);
        if (aura != null) Destroy(aura);
        if (boss != null) boss.casting = false;
        radiusNow = Radius;
        Active = false;
        running = false;
    }

    // ---------------- 1. 외침: 멈춤 → 카메라가 보스로 → 결계 이름 · 대사
    IEnumerator Cutscene(string title, string line, Color color)
    {
        float before = Time.timeScale;
        Time.timeScale = 0f;
        CutscenePlaying = true;
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        fx?.HoldCamera(boss.transform.position);

        Canvas canvas = NewCanvas("BossUltimateCanvas", 30500);
        RectTransform root = (RectTransform)canvas.transform;
        Image shade = Img(root, "Shade", new Color(0f, 0f, 0f, 0f));
        Stretch(shade.rectTransform);
        Image flash = Img(root, "Flash", new Color(1f, 1f, 1f, 0f));
        Stretch(flash.rectTransform);
        Image band = Img(root, "Band", new Color(0f, 0f, 0f, 0f));
        band.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        band.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        band.rectTransform.sizeDelta = new Vector2(0f, 0f);

        UIKit.EnsureStyle();
        TMP_Text tag = UIKit.Text(root, "결계 침식", 34f, new Color(0.85f, 0.8f, 0.9f), new Vector2(0f, 120f), new Vector2(1400f, 50f));
        TMP_Text name = UIKit.Text(root, title, 120f, color, new Vector2(0f, 25f), new Vector2(1800f, 160f));
        name.fontStyle = FontStyles.Bold;
        name.outlineWidth = 0.25f;
        name.outlineColor = new Color32(0, 0, 0, 255);
        TMP_Text say = UIKit.Text(root, line, 44f, new Color(0.95f, 0.92f, 0.88f), new Vector2(0f, -105f), new Vector2(1600f, 70f));
        // 1.0.4~ 건너뛰기 안내 (화면 아래) · 1.1~ 패드면 [A] (예전엔 패드여도 「클릭 · Space」)
        TMP_Text skip = UIKit.Text(root, GamePad.Active ? "[A]: 건너뛰기" : "클릭 · Space: 건너뛰기", 24f, new Color(0.75f, 0.72f, 0.8f), new Vector2(0f, -330f), new Vector2(900f, 36f));
        skip.alpha = 0f;
        tag.alpha = name.alpha = say.alpha = 0f;

        // 1.0.4~ 이 보스의 결계를 이번 실행에서 이미 봤으면 짧게 (3.3초 → 1초), 클릭 · Space · Enter 로 건너뜀
        // 설정 「번쩍임」을 끄면 섬광 · 어두운 막 깜빡임 없음, 「화면 흔들림」을 끄면 글자 떨림 없음
        bool brief = cutSeen[kind];
        cutSeen[kind] = true;
        float darken = brief ? 0.2f : 0.7f, hold = brief ? 0.6f : 2.3f;
        float flashA = GameSettings.Flashes ? 0.55f : 0f, flicker = GameSettings.Flashes ? 0.1f : 0f;
        float shakeMul = GameSettings.ScreenShake ? 1f : 0f;
        bool skipped = false;

        // 카메라가 보스 쪽으로 움직이는 동안 화면이 어두워짐
        for (float t = 0f; t < darken && !skipped; t += CutDt(canvas))
        {
            float k = t / darken;
            shade.color = new Color(0f, 0f, 0f, 0.55f * k);
            if (boss != null) fx?.HoldCamera(boss.transform.position);
            if (t > 0.1f) skipped = SkipPressed();      // 컷신이 시작된 그 순간의 공격 클릭으로는 넘어가지 않게
            yield return null;
        }

        // 쾅! 이름이 내려꽂히며 흔들림 + 외침
        PlaySfx("domain_shout", 1f);
        Hostile.Play("roar", 1f, kind == MomSlot ? 1.15f : kind == DevSlot ? 0.95f : kind == 3 ? 0.5f : kind == 2 ? 0.75f : kind == 1 ? 0.6f : 0.8f);
        fx?.Shake(0.9f, 0.6f);
        for (float t = 0f; t < hold && !skipped; t += CutDt(canvas))
        {
            float k = Mathf.Clamp01(t / 0.25f);
            flash.color = new Color(color.r, color.g, color.b, flashA * (1f - Mathf.Clamp01(t / 0.35f)));
            band.rectTransform.sizeDelta = new Vector2(0f, Mathf.Lerp(0f, 380f, EaseOut(k)));
            band.color = new Color(0f, 0f, 0f, 0.75f);
            tag.alpha = k;
            name.alpha = k;
            skip.alpha = k;
            name.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, EaseOut(k));
            float shake = Mathf.Lerp(18f, 4f, Mathf.Clamp01(t / 1.5f)) * shakeMul;
            name.rectTransform.anchoredPosition = new Vector2(0f, 25f) + Random.insideUnitCircle * shake;
            say.alpha = brief ? k : Mathf.Clamp01((t - 0.45f) / 0.3f);
            say.rectTransform.anchoredPosition = new Vector2(0f, -105f) + Random.insideUnitCircle * shake * 0.4f;
            shade.color = new Color(color.r * 0.15f, color.g * 0.05f, color.b * 0.1f, 0.6f + flicker * Mathf.Sin(t * 20f));
            if (boss != null) fx?.HoldCamera(boss.transform.position);
            if (t > 0.1f) skipped = SkipPressed();      // 이름이 뜨자마자 누른 공격 클릭으로 넘어가지 않게
            yield return null;
        }
        flash.color = Color.clear;
        skip.alpha = 0f;
        float fade = brief || skipped ? 0.2f : 0.3f;
        for (float t = 0f; t < fade; t += CutDt(canvas))
        {
            float k = 1f - t / fade;
            tag.alpha = name.alpha = say.alpha = k;
            band.color = new Color(0f, 0f, 0f, 0.75f * k);
            shade.color = new Color(0f, 0f, 0f, 0.6f * k);
            yield return null;
        }
        Destroy(canvas.gameObject);
        fx?.ReleaseCamera();
        CutscenePlaying = false;
        Time.timeScale = before > 0f ? before : 1f;
    }

    // 1.0.4~ 보스 종류마다 결계 컷신을 이번 실행에서 본 적 있는지 (두 번째부터 짧게)
    static readonly bool[] cutSeen = new bool[Slots];

    // 1.0.4~ 컷신 건너뛰기: 클릭 · Space · Enter (일시정지 메뉴가 열려 있을 때 · 트레일러 자동 조종 중엔 아님)
    static bool SkipPressed()
    {
        if (ESCmenu.IsOpen || GameInput.Auto) return false;
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || GamePad.ADown;     // 1.0.5~ 패드 A
    }

    // 1.0.2: 대사 중 ESC 로 일시정지하면 연출이 멈춘 시간에도 계속 흘러, 끝나면서 게임 속도를 되돌려 일시정지가 풀렸음
    // → 일시정지 메뉴가 열려 있는 동안은 연출 시계를 멈추고 화면도 감춤 (메뉴를 덮지 않게)
    static float CutDt(Canvas canvas)
    {
        bool paused = ESCmenu.IsOpen;
        if (canvas != null && canvas.enabled == paused) canvas.enabled = !paused;
        return paused ? 0f : Time.unscaledDeltaTime;
    }

    // ---------------- 2. 결계 침식: 맵이 어두워지고 둥근 결계가 퍼짐
    IEnumerator Expand(Color color)
    {
        domainRoot = new GameObject("BossDomain");
        domainRoot.transform.position = center;
        PlaySfx("domain_open", 1f);
        Hostile.Shake(0.5f);

        MeshRenderer dark = DarkMesh(domainRoot.transform, DarkTintOf(kind, color));     // 개발자는 터미널 초록 · 엄마는 집 불빛으로 안쪽이 물듦
        ring = Hostile.NewLine("DomainRing", color, 0.9f, 40);
        ring.loop = true;
        ring.transform.SetParent(domainRoot.transform, true);
        ring2 = Hostile.NewLine("DomainRing2", Color.white, 0.25f, 41);
        ring2.loop = true;
        ring2.transform.SetParent(domainRoot.transform, true);
        rune = Fx.Play("fx_rune", center, Radius * 2f, new Color(color.r, color.g, color.b, 0.3f), 1f, 0f, 41, true, -1f, "Background");
        if (rune != null) { rune.spin = 12f; rune.transform.SetParent(domainRoot.transform, true); }
        if (Hostile.Glow != null)
        {
            aura = SpecialAbilities.MakeSprite("BossDomainAura", Hostile.Glow, boss.transform.position, 1.2f, new Color(color.r, color.g, color.b, 0.5f), "Effect", 1);
        }
        Fx.Spawn("fx_shock", center, Radius * 2.6f, color, 14f);
        Fx.Spawn("fx_soulburst", center, 12f, Color.white, 14f);
        // 플레이어 발밑의 밝은 빛 + 고리: 어두운 결계 안에서도 내 위치가 바로 보이게
        if (Hostile.Glow != null)
        {
            spot = SpecialAbilities.MakeSprite("DomainSpot", Hostile.Glow, center, 0.55f, new Color(1f, 0.97f, 0.88f, 0.42f), "Background", 42);
            spot.transform.SetParent(domainRoot.transform, true);
        }
        spotRing = Hostile.NewLine("DomainSpotRing", Color.white, 0.12f, 43);
        spotRing.loop = true;
        spotRing.sortingLayerName = "Background";
        spotRing.transform.SetParent(domainRoot.transform, true);

        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            float k = EaseOut(t / 0.8f);
            float r = Mathf.Lerp(0.5f, Radius, k);
            SetDark(dark, Mathf.Lerp(0f, 1f, k));
            Hostile.SetArc(ring, center, r, 0f, 360f);
            Hostile.SetArc(ring2, center, r - 0.35f, 0f, 360f);
            // 결계 밖에 있으면 안쪽으로 끌려 들어옴
            PlayerController p = Hostile.Player;
            if (p != null && (p.transform.position - center).magnitude > r - 0.8f && r > 4f) Confine(p, r);
            RepelMobs(r);
            yield return null;
        }
        SetDark(dark, 1f);
    }

    LineRenderer ringDrawn;     // 1.2.4: 가득 펼친 고리를 이미 그린 선 (Expand 가 새 고리를 만들면 다시 그림)

    void Animate(Color color)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
        if (ring != null)
        {
            // 1.2.4: 중심 · 반지름이 결계 내내 그대로라 처음 한 번만 그림 (폭 · 색만 맥동)
            // 엄마 「통금 시간」에는 반지름이 바뀌는 동안만 다시 그림 · 어둠 메시도 크기만 같이 (새 메시 없음)
            if (ringDrawn != ring || ringDrawnR != radiusNow)
            {
                Hostile.SetArc(ring, center, radiusNow, 0f, 360f);
                ringDrawn = ring;
                ringDrawnR = radiusNow;
                Transform dk = domainRoot != null ? domainRoot.transform.Find("DomainDark") : null;     // DarkMesh 가 만든 자식
                if (dk != null) dk.localScale = Vector3.one * (radiusNow / Radius);
            }
            ring.startWidth = ring.endWidth = 0.8f + 0.25f * pulse;
            ring.startColor = ring.endColor = Color.Lerp(color, Color.white, 0.25f * pulse);
        }
        if (ring2 != null) Hostile.SetArc(ring2, center, radiusNow - 0.4f, Time.time * 40f, Time.time * 40f + 360f);
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
                spotRing.startColor = spotRing.endColor = Color.Lerp(Color.white, color, 0.35f);
            }
        }
        if (aura != null && boss != null)
        {
            aura.transform.position = boss.transform.position;
            aura.transform.localScale = Vector3.one * (1.2f + 0.2f * pulse);
        }
        // 결계 가장자리에서 튀는 불꽃
        if (Random.value < 0.4f)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Fx.Spawn("fx_spark", center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radiusNow, 2f, color, 18f);
        }
    }

    float radiusNow = Radius, ringDrawnR = -1f;     // 지금 결계 반지름 (엄마 「통금 시간」만 바꿈)

    // 결계 안의 잡몹은 가장자리 밖으로 튕겨 나가고, 바깥 잡몹은 벽에 막혀 들어오지 못함 (보스와 1:1)
    // 1.2.5~ 켜진 잡몹 전부 = EnermyController.Live (예전 0.2초마다 FindObjectsByType 과 같은 대상 · 장면 검색 · 배열 할당 없음 · 새로 들어온 잡몹도 바로)
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
            Vector3 to = EdgeInArena(d, edge);
            to.z = pos.z;
            // 안쪽 깊이 있던 잡몹이 튕겨 나갈 때만 불꽃 (벽에 막히는 잡몹은 조용히)
            if ((to - pos).sqrMagnitude > 1.5f * 1.5f && sparks++ < 6)
            {
                Fx.Spawn("fx_spark", to, 2.2f, Color.white, 18f);
            }
            e.transform.position = to;
        }
        // 1.2.3~ 무한 모드에서 겹쳐 나온 다른 보스도 결계 밖으로 (결계는 펼친 보스와 1:1 · 다른 보스의 기술 · 부하는 Active 동안 이미 멈춤, 이동 · 몸통만 남았었음)
        float bossEdge = r + 2.2f;
        foreach (bosss o in bosss.Live)
        {
            if (o == null || o == boss || o.IsDead) continue;
            Vector3 pos = o.transform.position;
            Vector3 d = pos - center;
            d.z = 0f;
            if (d.sqrMagnitude >= bossEdge * bossEdge) continue;
            if (d.sqrMagnitude < 0.01f) d = Vector3.left;
            Vector3 to = EdgeInArena(d, bossEdge);
            to.z = pos.z;
            o.transform.position = to;
        }
    }

    // 결계 가장자리 중 맵 안인 자리 (2.2.0: 결계가 맵 가장자리 근처면 바깥 방향 그대로 밀어 맵 밖으로 나갔음)
    // 밀던 방향에서 좌우로 조금씩 돌려 가며 맵 안인 가장자리를 찾고, 없으면 맵 안으로 맞춤
    Vector3 EdgeInArena(Vector3 d, float edge)
    {
        Vector3 n = d.normalized;
        for (int step = 0; step <= 12; step++)
        {
            float a = 15f * ((step + 1) / 2) * (step % 2 == 0 ? 1f : -1f);
            Vector3 to = center + Quaternion.Euler(0f, 0f, a) * n * edge;
            if (Hostile.InArena(to)) return to;
        }
        return Hostile.ClampArena(center + n * edge);
    }

    void Confine(PlayerController p, float r = Radius)
    {
        Vector3 d = p.transform.position - center;
        float max = r - 0.8f;
        if (d.magnitude > max) p.transform.position = center + d.normalized * max;
    }

    // 1.0.1: 결계 안은 다른 공간 — 맵 테두리 · 구조물(벽)을 끄고 감춤 (구석에서 펼치면 벽 때문에 피할 자리가 없었음)
    // 끝나면 되살리고, 맵 밖이나 벽 속에 있으면 가장 가까운 맵 안 빈자리로 옮김
    static readonly List<Collider2D> hiddenWalls = new List<Collider2D>();
    static readonly List<Renderer> hiddenWallArt = new List<Renderer>();

    static void HideWalls(bool hide)
    {
        if (hide)
        {
            if (hiddenWalls.Count > 0) return;
            foreach (Collider2D c in FindObjectsOfType<Collider2D>())
            {
                if (c == null || c.isTrigger || !c.enabled || !c.CompareTag("Wall")) continue;
                c.enabled = false;
                hiddenWalls.Add(c);
                Renderer r = c.GetComponent<Renderer>();
                if (r != null && r.enabled) { r.enabled = false; hiddenWallArt.Add(r); }
            }
            return;
        }
        if (hiddenWalls.Count == 0) return;
        foreach (Collider2D c in hiddenWalls) if (c != null) c.enabled = true;
        foreach (Renderer r in hiddenWallArt) if (r != null) r.enabled = true;
        hiddenWalls.Clear();
        hiddenWallArt.Clear();
        PlayerController p = Hostile.Player;
        if (p != null && p.isActiveAndEnabled) p.StartCoroutine(ReturnInside(p));
    }

    static IEnumerator ReturnInside(PlayerController p)
    {
        yield return new WaitForFixedUpdate();          // 되살린 벽이 물리에 들어간 뒤 검사
        if (p == null) yield break;
        Vector3 pos = p.transform.position;
        if (Hostile.InArena(pos) && !Hostile.IsWall(pos)) yield break;
        Vector3 to = FreeSpot(Hostile.ClampArena(pos));
        Fx.Spawn("fx_soulburst", pos, 3f, Color.white, 16f);
        p.transform.position = new Vector3(to.x, to.y, pos.z);
        Fx.Spawn("fx_soulburst", to, 3f, Color.white, 16f);
        p.GrantInvincibility(1f);
    }

    // 맵 안이면서 벽이 아닌 가장 가까운 자리 (가까운 고리부터 돌아가며 찾음)
    static Vector3 FreeSpot(Vector3 from)
    {
        if (!Hostile.IsWall(from) && !Hostile.WallNear(from, 0.45f)) return from;
        for (float r = 0.75f; r <= 14f; r += 0.75f)
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8f;
                Vector3 at = from + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (Hostile.InArena(at) && !Hostile.IsWall(at) && !Hostile.WallNear(at, 0.45f)) return at;
            }
        return from;
    }

    // ---------------- 3. 결계 파열
    IEnumerator Shatter(Color color)
    {
        PlaySfx("domain_break", 1f);
        Hostile.Shake(0.4f);
        for (int i = 0; i < 36; i++)
        {
            float a = i * 10f * Mathf.Deg2Rad;
            Vector3 at = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radiusNow;
            // 개발자 「디버그 모드」는 초록 · 흰색이 번갈아 튀는 글리치 불꽃
            Color sc = kind == DevSlot ? (i % 2 == 0 ? DevTerminal : Color.white) : Color.Lerp(color, Color.white, 0.5f);
            Fx.Spawn("fx_spark", at, 2.6f, sc, 16f);
        }
        Fx.Spawn("fx_shock", center, Radius * 2.4f, Color.white, 14f);
        MeshRenderer dark = domainRoot != null ? domainRoot.GetComponentInChildren<MeshRenderer>() : null;
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            float k = 1f - t / 0.6f;
            if (dark != null) SetDark(dark, k);
            if (ring != null) ring.startColor = ring.endColor = new Color(color.r, color.g, color.b, k);
            if (ring2 != null) ring2.startColor = ring2.endColor = new Color(1f, 1f, 1f, k);
            yield return null;
        }
    }

    // ================================================================= 보스마다 다른 공격
    // 1.2.5~ 네 결계 공격은 BossUltimate.Domain.cs (DomainCtx 기반 static · 공간 침식과 같은 코드)
    readonly DomainCtx soloCtx = new DomainCtx();      // 개별 결계 맥락 (결계마다 Reset 해서 다시 씀)

    // 공격이 도중에 멈추면 (결계 끝 · 보스 사라짐): 매 프레임 새로 만들던 때처럼 마지막 빔이 0.08초 동안 흐려지며 스스로 사라짐
    void FadeLichBeams() => soloCtx.FadeBeams();
    // 튕기는 덩어리 · 땅 타격(예전 Spike 코루틴)은 BossUltimate.Domain.cs 의 Bouncer · DomainStrikes (타격마다 코루틴을 만들지 않음)

    // ================================================================= 도우미
    // 결계 공격 예고: 크게 흔들리는 느낌표 + 번쩍이는 원 (어두운 결계 안에서도 바로 보이게)
    // 1.2.5: 예고마다 Fx.Play(새 오브젝트) + AddComponent<WarnPop> 이던 것 → 풀(Fx.SpawnPop · FxAnim.pop)로 (공간 침식은 초당 5 ~ 8개)
    internal static void Warn(Vector3 at, float duration, float size)
    {
        Fx.SpawnPop("fx_warn", at + Vector3.up * 0.3f, Mathf.Clamp(size * 0.9f, 1.3f, 2.4f), Color.white, 6f, 45, duration);
        Fx.Spawn("fx_shock", at, Mathf.Max(2f, size * 2.2f), new Color(1f, 0.3f, 0.25f, 0.85f), 22f);
    }

    // 예고 색을 더 진하고 밝게
    internal static Color Bright(Color c) => new Color(Mathf.Min(1f, c.r * 1.15f + 0.1f), Mathf.Min(1f, c.g * 1.15f + 0.05f), Mathf.Min(1f, c.b * 1.15f + 0.05f), 0.95f);

    // 결계 밖을 덮는 어둠 (둥근 구멍이 뚫린 큰 고리 메시): 안쪽은 보스 색으로 살짝 물듦
    MeshRenderer DarkMesh(Transform parent, Color color)
    {
        GameObject go = new GameObject("DomainDark", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        // 1.2.4: 결계마다 새 Mesh · Material 을 만들고 지우지 않아 결계 횟수만큼 쌓였음 → 한 번만 만들어 같이 씀
        //   (반지름이 상수라 정점은 늘 같음 · 결계는 한 번에 하나 · 색은 SetDark 가 처음부터 다시 씀)
        if (darkMesh == null) { darkMesh = BuildDarkMesh(); darkMesh.hideFlags = HideFlags.DontUnloadUnusedAsset; }     // 장면 전환 정리에 지워지지 않게 (판끼리 같이 씀)
        go.GetComponent<MeshFilter>().sharedMesh = darkMesh;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (darkMaterial == null) darkMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontUnloadUnusedAsset };
        mr.sharedMaterial = darkMaterial;
        // 바닥 층 맨 위 (바닥 타일 · 장판 위, 캐릭터 · 적 · 적 탄 아래): 맵만 어두워지고 싸우는 것들은 또렷하게
        // (예전엔 이펙트 층이라 결계 안의 캐릭터까지 어둡게 덮어 잘 보이지 않았음)
        mr.sortingLayerName = "Background";
        mr.sortingOrder = 40;
        darkTint = color;
        return mr;
    }

    static Mesh darkMesh;
    static Material darkMaterial;
    static Color[] darkColors;

    static Mesh BuildDarkMesh()
    {
        Mesh m = new Mesh();
        const int seg = 96;
        float[] radii = { 0f, Radius - 0.2f, Radius + 1.8f, 400f };
        List<Vector3> v = new List<Vector3>();
        List<int> tri = new List<int>();
        for (int ri = 0; ri < radii.Length; ri++)
            for (int s = 0; s < seg; s++)
            {
                float a = s * Mathf.PI * 2f / seg;
                v.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radii[ri]);
            }
        for (int ri = 0; ri < radii.Length - 1; ri++)
            for (int s = 0; s < seg; s++)
            {
                int a = ri * seg + s, b = ri * seg + (s + 1) % seg, c = (ri + 1) * seg + s, d = (ri + 1) * seg + (s + 1) % seg;
                tri.AddRange(new[] { a, c, b, b, c, d });
            }
        m.SetVertices(v);
        m.SetTriangles(tri, 0);
        return m;
    }

    Color darkTint;

    void SetDark(MeshRenderer mr, float k)
    {
        if (mr == null) return;
        MeshFilter f = mr.GetComponent<MeshFilter>();
        Mesh m = f.sharedMesh;         // 1.2.4: .mesh 는 복제본을 만들 수 있음 → 같이 쓰는 메시 그대로
        if (m == null) return;
        int seg = m.vertexCount / 4;
        Color inner = new Color(darkTint.r * 0.25f, darkTint.g * 0.15f, darkTint.b * 0.3f, 0.32f * k);
        Color edge = new Color(0f, 0f, 0f, 0.55f * k);
        Color outer = new Color(0.01f, 0f, 0.02f, 0.9f * k);
        // 1.2.4: 열릴 때 · 부서질 때 매 프레임 Color[384] 를 새로 만들던 것 → 한 번 만든 버퍼에 채움
        if (darkColors == null || darkColors.Length != m.vertexCount) darkColors = new Color[m.vertexCount];
        Color[] c = darkColors;
        for (int i = 0; i < c.Length; i++)
        {
            int ri = i / seg;
            c[i] = ri <= 1 ? (ri == 0 ? inner : Color.Lerp(inner, edge, 0.3f)) : ri == 2 ? outer : outer;
        }
        m.SetColors(c);
    }

    static void PlaySfx(string name, float vol)
    {
        AudioClip clip = Resources.Load<AudioClip>("Sounds/" + name);
        if (clip == null) return;
        if (sfx == null)
        {
            GameObject go = new GameObject("BossUltimateAudio");
            DontDestroyOnLoad(go);
            sfx = go.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            // 1.1~: ignoreListenerPause 를 빼서 「창이 비활성일 때 소리 끄기」를 따름 (컷신의 timeScale=0 은 AudioListener.pause 와 무관)
            // 장면이 바뀌면 (결계 중 사망 · 메뉴로 나감) 남은 결계 소리를 멈춤
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (sfx != null) sfx.Stop(); };
        }
        sfx.PlayOneShot(clip, vol * GameSettings.SfxVolume);
    }

    static Canvas NewCanvas(string name, int order)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        UiScaler.Fit(cs);               // 설정 「UI 크기」 (2.1.9)
        cs.matchWidthOrHeight = 0.5f;
        return c;
    }

    static Image Img(RectTransform parent, string name, Color c)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        Image i = go.GetComponent<Image>();
        i.color = c;
        i.raycastTarget = false;
        return i;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static float EaseOut(float k) => 1f - (1f - Mathf.Clamp01(k)) * (1f - Mathf.Clamp01(k));
}

// 결계 벽에 튕기며 굴러다니는 산성 덩어리 (닿으면 피해)
public class DomainBall : MonoBehaviour
{
    public Vector3 center;
    public float radius, damage, life;
    public Vector2 velocity;
    // 1.2.5~ 공간 침식 · 개발자 버그가 같이 씀: 맥락(있으면 중심 · 반지름을 매 프레임 여기서 · 엄마 통금), 닿는 거리, 자국 색 · 그림, 받은 피해 이름
    public DomainCtx ctx;
    public float hitRadius = 1.1f;
    public Color trailColor = new Color(0.55f, 1f, 0.35f, 0.6f);
    public string trailFx = "fx_puddle";
    public string label;
    float age, trail;

    // 결계 전체에서 튕기는 것 (침식 최대 4 · 개별 3) — 켜질 때 넣고 꺼질 때 빼서 쌓이지 않음
    public static readonly List<DomainBall> Live = new List<DomainBall>(8);
    void OnEnable() => Live.Add(this);
    void OnDisable() => Live.Remove(this);

    // 이 맥락이 만든 것만 지움 (공격이 끝날 때)
    public static void DestroyOf(DomainCtx d)
    {
        for (int i = Live.Count - 1; i >= 0; i--)
            if (Live[i] != null && Live[i].ctx == d) Destroy(Live[i].gameObject);
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= life || !BossUltimate.Active) { Destroy(gameObject); return; }
        if (ctx != null) { center = ctx.center; radius = ctx.radius - 0.6f; }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        Vector3 d = transform.position - center;
        if (d.magnitude > radius)
        {
            Vector2 n = d.normalized;
            velocity = Vector2.Reflect(velocity, -n);
            transform.position = center + (Vector3)(n * radius);
        }
        transform.localScale = Vector3.one * ((0.46f + 0.04f * Mathf.Sin(age * 12f)) * HostileProjectile.LookMul);     // HostileProjectile.Dress 의 빛 크기 (지름 1.6 × 1.2 × 2.3 ÷ 8)
        trail += Time.deltaTime;
        if (trail > 0.12f) { trail = 0f; Fx.Spawn(trailFx, transform.position, 1.2f, trailColor, 14f); }
        PlayerController p = Hostile.Player;
        if (p != null && Vector2.Distance(p.transform.position, transform.position) < hitRadius)
        {
            AttackLabel.Pending = label;
            p.TryHit(damage);
            AttackLabel.Pending = null;
        }
    }
}

// 결계 공격 예고 느낌표의 튀어나왔다가 두근거림은 1.2.5~ 풀 이펙트의 FxAnim.pop (Fx.SpawnPop) — 예전 WarnPop 컴포넌트는 예고마다 새 오브젝트에 붙었음
