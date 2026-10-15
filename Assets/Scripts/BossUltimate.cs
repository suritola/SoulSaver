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
public class BossUltimate : MonoBehaviour
{
    public static bool Active { get; private set; }
    // 결계 이름 · 대사 연출 중 (게임이 멈춰 있음) — 일시정지 메뉴를 닫을 때 다시 흐르게 하지 않도록 (ESCmenu)
    public static bool CutscenePlaying { get; private set; }

    // 보스 종류마다 게이지 (0 ~ 1)
    static readonly float[] gauge = new float[4];
    static readonly float[] refHp = new float[4];
    // 마지막으로 맞은 때 (3초 동안 안 맞으면 게이지가 아주 서서히 줄어듦) · 이번 보스전에 결계를 펼쳤는지
    static readonly float[] lastHit = new float[4];
    static readonly bool[] usedThisFight = new bool[4];
    static int decayFrame = -1;
    public static float Gauge(int kind) => kind >= 0 && kind < gauge.Length ? gauge[kind] : 0f;
    // 이번 보스전에 결계를 펼쳤는지 (업적: 거울의 군주를 결계 전에)
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
    };

    bosss boss;
    int kind;
    float spawnedAt;
    static AudioSource sfx;

    void Start()
    {
        boss = GetComponent<bosss>();
        kind = boss != null ? Mathf.Clamp(boss.bossKind, 0, 3) : 0;
        spawnedAt = Time.time;
        // 새 보스 (킹 슬라임은 첫 덩어리)가 나오면 게이지를 비우고 기준 체력을 정함
        if (boss != null && (kind != 2 || boss.slimeGen == 1))
        {
            gauge[kind] = 0f;
            refHp[kind] = Mathf.Max(1f, boss.setEnemyHP);
            usedThisFight[kind] = false;
            lastHit[kind] = Time.time;
        }
    }

    void OnDestroy()
    {
        if (running) { CutscenePlaying = false; EndNow(); }
    }

    // 보스가 맞을 때 (bosss.TakeDamage): 피해 비율의 1.2제곱 → 약한 공격 여러 번보다 강한 한 방이 더 많이 채움
    // (예전 5 · 1.3제곱은 너무 늦게 차서, 한 판에 약한 공격만으로도 두세 번 · 센 공격 위주면 네댓 번 차게)
    public static void OnBossHit(bosss b, float damage)
    {
        if (b == null || Active || b.IsDead || b.developer || damage <= 0f) return;     // 개발자는 결계 대신 필살기만
        int k = Mathf.Clamp(b.bossKind, 0, 3);
        float f = damage / Mathf.Max(1f, refHp[k] > 0f ? refHp[k] : b.setEnemyHP);
        // 약한 공격도 피해 비율의 3.5배는 채움 (1.2제곱만이면 평타가 거의 안 차서 · 보스 체력 3% 이상의 센 한 방은 그대로)
        float gain = Mathf.Max(7f * Mathf.Pow(f, 1.2f), 3.5f * f);
        gauge[k] = Mathf.Min(1f, gauge[k] + Mathf.Min(0.5f, gain));
        lastHit[k] = Time.time;
    }

    void Update()
    {
        if (!Active && boss != null && !boss.IsDead)
        {
            // 3초 동안 안 맞으면 게이지가 아주 서서히 (초당 0.4%) 줄어듦 · 갈라진 슬라임은 한 번만 줄게
            if (decayFrame != Time.frameCount && Time.time - lastHit[kind] > 3f && gauge[kind] < 1f)
            {
                decayFrame = Time.frameCount;
                gauge[kind] = Mathf.Max(0f, gauge[kind] - 0.004f * Time.deltaTime);
            }
            // 보스전마다 적어도 한 번: 아직 펼치지 않았는데 체력이 40% 아래로 내려가면 게이지가 가득 참
            if (!usedThisFight[kind] && boss.setEnemyHP > 0 && boss.EnemyHealth < boss.setEnemyHP * 0.4f)
                gauge[kind] = 1f;
        }
        if (Active || boss == null || boss.IsDead || boss.developer || gauge[kind] < 1f) return;
        if (Time.time - spawnedAt < 4f || Time.time < restUntil || Time.timeScale == 0f || StoryDirector.Playing || boss.casting) return;
        if (SkillEvolutionUI.Open || WeaponEvolutionUI.Open || ChoiceUI.Open || ESCmenu.IsOpen) return;
        // 킹 슬라임: 살아 있는 덩어리 중 체력이 가장 많은 하나만 씀
        if (kind == 2 && !IsStrongestSlime()) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        StartCoroutine(Run());
    }

    bool IsStrongestSlime()
    {
        List<bosss> live = new List<bosss>();
        bosss.CollectLiveSlimes(live);
        foreach (bosss s in live) if (s != boss && s.EnemyHealth > boss.EnemyHealth) return false;
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
        float until = Time.time + (kind == 2 ? SlimeDuration : Duration);
        int hurtBefore = PlayerController.HurtCount;
        IEnumerator attack = kind == 0 ? LichBarrage(until) : kind == 1 ? HellBarrage(until) : kind == 2 ? SlimeBarrage(until) : MirrorBarrage(until);
        StartCoroutine(attack);
        while (Time.time < until && boss != null && !boss.IsDead && player != null && !player.IsDying)
        {
            Confine(player);
            RepelMobs(Radius);
            Animate(info.color);
            yield return null;
        }
        StopCoroutine(attack);
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
        HideWalls(false);
        restUntil = Time.time + 15f;
        if (domainRoot != null) Destroy(domainRoot);
        if (aura != null) Destroy(aura);
        if (boss != null) boss.casting = false;
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
        Hostile.Play("roar", 1f, kind == 3 ? 0.5f : kind == 2 ? 0.75f : kind == 1 ? 0.6f : 0.8f);
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
    static readonly bool[] cutSeen = new bool[4];

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

        MeshRenderer dark = DarkMesh(domainRoot.transform, color);
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

    void Animate(Color color)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
        if (ring != null)
        {
            Hostile.SetArc(ring, center, Radius, 0f, 360f);
            ring.startWidth = ring.endWidth = 0.8f + 0.25f * pulse;
            ring.startColor = ring.endColor = Color.Lerp(color, Color.white, 0.25f * pulse);
        }
        if (ring2 != null) Hostile.SetArc(ring2, center, Radius - 0.4f, Time.time * 40f, Time.time * 40f + 360f);
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
            Fx.Spawn("fx_spark", center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius, 2f, color, 18f);
        }
    }

    // 결계 안의 잡몹은 가장자리 밖으로 튕겨 나가고, 바깥 잡몹은 벽에 막혀 들어오지 못함 (보스와 1:1)
    EnermyController[] mobs = new EnermyController[0];
    float mobsAt;

    void RepelMobs(float r)
    {
        if (Time.time > mobsAt)
        {
            mobsAt = Time.time + 0.2f;
            mobs = FindObjectsOfType<EnermyController>();
        }
        float edge = r + 0.9f;
        int sparks = 0;
        foreach (EnermyController e in mobs)
        {
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
            Vector3 at = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius;
            Fx.Spawn("fx_spark", at, 2.6f, Color.Lerp(color, Color.white, 0.5f), 16f);
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
    // 망자의 묘역 (리치 왕): 회전하는 영혼 광선 넷 + 틈이 있는 영혼 고리 + 발밑 뼈 가시
    IEnumerator LichBarrage(float until)
    {
        Color soul = Info[0].color, curse = new Color(0.75f, 0.4f, 1f);
        float angle = Random.Range(0f, 90f), spin = Random.value < 0.5f ? 22f : -22f;
        float nextRing = Time.time + 0.6f, nextSpike = Time.time;
        LineRenderer[] warn = new LineRenderer[4];
        for (int i = 0; i < 4; i++) { warn[i] = Hostile.NewLine("LichBeamWarn", new Color(soul.r, soul.g, soul.b, 0.3f), 0.18f, 30); warn[i].transform.SetParent(domainRoot.transform, true); }
        float live = Time.time + 1.4f, beamHit = 0f;
        for (int i = 0; i < 4; i++)
        {
            float a0 = (angle + i * 90f) * Mathf.Deg2Rad;
            Warn((boss != null ? boss.transform.position : center) + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * 7f, 1.4f, 1.6f);
        }
        FxAnim[] beams = new FxAnim[4];
        while (Time.time < until)
        {
            angle += spin * Time.deltaTime;
            Vector3 c = boss != null ? boss.transform.position : center;
            bool on = Time.time > live;
            for (int i = 0; i < 4; i++)
            {
                float a = (angle + i * 90f) * Mathf.Deg2Rad;
                Vector3 end = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius * 2f;
                warn[i].positionCount = 2;
                warn[i].SetPosition(0, c);
                warn[i].SetPosition(1, end);
                if (on)
                {
                    if (beams[i] != null) Destroy(beams[i].gameObject);
                    beams[i] = Fx.Beam(c, end, 1.4f, soul, 0.08f, 22);
                }
            }
            PlayerController p = Hostile.Player;
            if (on && p != null && (beamHit -= Time.deltaTime) <= 0f)
                for (int i = 0; i < 4; i++)
                {
                    float a = (angle + i * 90f) * Mathf.Deg2Rad;
                    Vector3 end = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius * 2f;
                    if (Hostile.DistanceToSegment(p.transform.position, c, end) < 0.85f) { p.TryHit(16f * Power); beamHit = 0.4f; break; }
                }
            if (Time.time >= nextRing)
            {
                nextRing = Time.time + 1.75f;       // 1.0.1: 2장 결계 살짝 완화 (1.5 → 1.75)
                int n = 26, gap = Random.Range(0, n);
                for (int i = 0; i < n; i++)
                {
                    if (Mathf.Abs(i - gap) <= 2 || Mathf.Abs(i - gap) >= n - 2) continue;     // 다섯 칸짜리 틈
                    float a = i * Mathf.PI * 2f / n;
                    Hostile.Shoot(c, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 6.5f, 14f * Power, 0.5f, curse, 1.6f, 3f);
                }
                Hostile.Play("shimmer", 0.5f, 0.6f);
            }
            if (Time.time >= nextSpike && p != null)
            {
                nextSpike = Time.time + 0.68f;      // 1.0.1: 0.55 → 0.68 · 예고 0.75 → 0.85
                Vector3 at = p.transform.position + (Vector3)(Random.insideUnitCircle * 1.5f);
                StartCoroutine(Spike(at, 1.7f, 0.85f, soul, 18f * Power, "fx_spike"));
            }
            yield return null;
        }
        foreach (FxAnim b in beams) if (b != null) Destroy(b.gameObject);
    }

    // 연옥 낙화 (지옥의 군주) — 1.0.1 개편: 화염 고리 · 용암 줄기가 운석과 한꺼번에 겹쳐 피하기 불쾌했음
    // 불을 빼고 운석만 세 박자로 번갈아: ① 곳곳에 한꺼번에 떨어지는 운석 비 → ② 내 자리의 거대 운석 → ③ 틈이 있는 운석 행진
    // 1.0.1: 너무 쉬웠음 → 운석 비가 더 많고 일부는 나를 노림 · 거대 운석은 둘레를 막고 한 번 더 · 행진이 빨라짐,
    //        그리고 내내 내 발밑으로 작은 불씨 운석이 떨어져 가만히 서 있을 수 없음 (예고는 모두 0.9초 이상)
    IEnumerator HellBarrage(float until)
    {
        Color fire = Info[1].color;
        StartCoroutine(HellEmbers(until, fire));
        int beat = 0;
        while (Time.time < until)
        {
            PlayerController p = Hostile.Player;
            Vector3 me = p != null ? p.transform.position : center;
            switch (beat++ % 3)
            {
                case 0:     // 운석 비: 결계 곳곳 12개 + 내 둘레 5개 (내 발밑 바로는 아님)
                    for (int i = 0; i < 12; i++)
                    {
                        Vector3 at = center + (Vector3)(Random.insideUnitCircle * (Radius - 2f));
                        StartCoroutine(Spike(at, 2.6f, 1.05f, fire, 22f * Power, "fx_meteor"));
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        float a = (i * 72f + Random.Range(0f, 72f)) * Mathf.Deg2Rad;
                        StartCoroutine(Spike(me + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(3f, 6f), 2.4f, 1.05f, fire, 22f * Power, "fx_meteor"));
                    }
                    Hostile.Play("whoosh", 0.6f, 0.7f);
                    yield return new WaitForSeconds(1.7f);
                    break;
                case 1:     // 거대 운석: 지금 내 자리 (반지름 6, 예고 1.3초) + 둘레 여섯 곳 → 피한 자리로 한 번 더
                {
                    StartCoroutine(Spike(me, 6f, 1.3f, fire, 30f * Power, "fx_meteor"));
                    float off = Random.Range(0f, 60f);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = (off + i * 60f) * Mathf.Deg2Rad;
                        StartCoroutine(Spike(me + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 9.5f, 2.2f, 1.3f, fire, 22f * Power, "fx_meteor"));
                    }
                    Hostile.Play("whoosh", 0.8f, 0.5f);
                    yield return new WaitForSeconds(1.4f);
                    p = Hostile.Player;
                    if (p != null) StartCoroutine(Spike(p.transform.position, 4.5f, 1.1f, fire, 26f * Power, "fx_meteor"));
                    Hostile.Play("whoosh", 0.8f, 0.6f);
                    yield return new WaitForSeconds(1.3f);
                    break;
                }
                default:    // 운석 행진: 한쪽에서 반대쪽으로 줄지어 떨어짐, 줄 가운데 한 칸은 비움
                {
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang));
                    Vector3 side = new Vector3(-dir.y, dir.x);
                    int gap = Random.Range(-2, 3);
                    for (int row = -6; row <= 6; row++)
                    {
                        Vector3 rowAt = me + dir * (row * 4f);
                        for (int k = -4; k <= 4; k++)
                        {
                            if (k == gap) continue;                 // 피할 틈
                            StartCoroutine(Spike(rowAt + side * (k * 4f), 2f, 1f, fire, 22f * Power, "fx_meteor"));
                        }
                        yield return new WaitForSeconds(0.16f);
                    }
                    yield return new WaitForSeconds(0.8f);
                    break;
                }
            }
        }
    }

    // 연옥 낙화 내내: 0.9초마다 내 자리에 작은 불씨 운석 (멈춰 있으면 맞음)
    IEnumerator HellEmbers(float until, Color fire)
    {
        yield return new WaitForSeconds(1.2f);
        while (Time.time < until && running)        // 결계가 일찍 끝나면 (보스 처치 · 쓰러짐) 같이 멈춤
        {
            PlayerController p = Hostile.Player;
            if (p != null) StartCoroutine(Spike(p.transform.position, 1.6f, 0.9f, fire, 16f * Power, "fx_meteor"));
            yield return new WaitForSeconds(0.9f);
        }
    }

    // 거울의 방 (거울의 군주 · 2.1.1): 결계 둘레의 거울 여덟 개가 차례로 플레이어를 노려 광선을 되쏘고,
    // 깨진 거울 조각이 발밑에 쏟아지며, 가끔 보스에게서 반사 조각 고리가 퍼짐
    IEnumerator MirrorBarrage(float until)
    {
        Color glass = Info[3].color;
        const int count = 8;
        Vector3[] mirrors = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float a = (i * 360f / count + 22.5f) * Mathf.Deg2Rad;
            mirrors[i] = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (Radius - 2f);
            FxAnim m = Fx.Play("fx_rune", mirrors[i], 4f, new Color(glass.r, glass.g, glass.b, 0.85f), 1f, 0f, 20, true, until - Time.time);
            if (m != null) { m.spin = 60f; m.transform.SetParent(domainRoot.transform, true); }
        }
        float nextBeam = Time.time + 0.6f, nextShard = Time.time, nextRing = Time.time + 2.5f;
        int turn = 0;
        while (Time.time < until)
        {
            PlayerController p = Hostile.Player;
            if (Time.time >= nextBeam && p != null)
            {
                nextBeam = Time.time + 1.25f;
                // 마주 보는 거울 둘이 동시에 (사이로 빠져나갈 수 있게 한 쌍씩)
                StartCoroutine(MirrorBeam(mirrors[turn % count], p.transform.position, glass));
                StartCoroutine(MirrorBeam(mirrors[(turn + count / 2 + 1) % count], p.transform.position, glass));
                turn += 3;
            }
            if (Time.time >= nextShard && p != null)
            {
                nextShard = Time.time + 0.45f;
                Vector3 at = Random.value < 0.6f ? p.transform.position + (Vector3)(Random.insideUnitCircle * 3f) : center + (Vector3)(Random.insideUnitCircle * (Radius - 1f));
                StartCoroutine(Spike(at, 1.8f, 1f, glass, 18f * Power, "fx_spike"));
            }
            if (Time.time >= nextRing && boss != null)
            {
                nextRing = Time.time + 3.6f;
                const int n = 18;
                float off = Random.Range(0f, 360f);
                for (int i = 0; i < n; i++)
                {
                    float a = (off + i * 360f / n) * Mathf.Deg2Rad;
                    Hostile.Shoot(boss.transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 6.5f, 14f * Power, 0.5f, glass, 1.6f, 5f);
                }
                Hostile.Play("shimmer", 0.5f, 1.2f);
            }
            yield return null;
        }
    }

    IEnumerator MirrorBeam(Vector3 from, Vector3 aim, Color glass)
    {
        Vector3 dir = (aim - from).normalized;
        Vector3 to = from + dir * Radius * 2.1f;
        Hostile.Line(from, to, 2f, 1.15f, Bright(glass));
        Warn(Vector3.Lerp(from, aim, 0.5f), 1.15f, 1.6f);
        yield return new WaitForSeconds(1.15f);
        Fx.Beam(from, to, 1.8f, new Color(0.85f, 0.95f, 1f), 0.35f);
        PlayerController p = Hostile.Player;
        if (p != null && Hostile.DistanceToSegment(p.transform.position, from, to) < 1.1f) p.TryHit(22f * Power);
        Hostile.Play("zap", 0.6f, 0.9f);
    }

    // 산성 범람 (킹 슬라임): 결계 벽에 튕기는 슬라임 덩어리 + 쏟아지는 산성 비 (2.2.1~ 9초 · 덩어리 최대 3 · 줄무늬 간헐천 없음)
    IEnumerator SlimeBarrage(float until)
    {
        Color acid = Info[2].color;
        float nextBall = Time.time, nextRain = Time.time + 0.3f;
        List<GameObject> balls = new List<GameObject>();
        while (Time.time < until)
        {
            PlayerController p = Hostile.Player;
            // 피하기 쉽게: 튕기는 산성 덩어리 1.4초마다 (최대 3), 산성 비 1.3초마다 3곳 · 예고 1.3초
            if (Time.time >= nextBall && balls.Count < 3)
            {
                nextBall = Time.time + 1.4f;
                balls.Add(Bouncer(boss != null ? boss.transform.position : center, acid, until - Time.time));
            }
            if (Time.time >= nextRain && p != null)
            {
                nextRain = Time.time + 1.3f;
                for (int i = 0; i < 3; i++)
                {
                    Vector3 at = i == 0 ? p.transform.position : p.transform.position + (Vector3)(Random.insideUnitCircle * 5f);
                    StartCoroutine(Spike(at, 1.8f, 1.3f, acid, 16f * Power, "fx_geyser"));
                }
            }
            balls.RemoveAll(g => g == null);
            yield return null;
        }
        foreach (GameObject g in balls) if (g != null) Destroy(g);
    }

    GameObject Bouncer(Vector3 from, Color acid, float life)
    {
        if (Hostile.Glow == null) return null;
        GameObject g = SpecialAbilities.MakeSprite("AcidBall", Hostile.Glow, from, 0.22f, new Color(acid.r, acid.g, acid.b, 0.95f), "Effect", 20);
        HostileProjectile.Dress(g, acid, 1.6f);       // 1.0.1: 다른 적 탄처럼 테두리 구슬로 또렷하게
        DomainBall b = g.AddComponent<DomainBall>();
        b.center = center;
        b.radius = Radius - 0.6f;
        b.velocity = Random.insideUnitCircle.normalized * 5.5f;
        b.damage = 15f * Power;
        b.life = Mathf.Max(1f, life);
        return g;
    }

    // 경고 원 → 위에서 떨어져(또는 솟아) 터짐
    IEnumerator Spike(Vector3 at, float r, float warn, Color color, float damage, string fx)
    {
        if ((at - center).magnitude > Radius - 0.5f) at = center + (at - center).normalized * (Radius - 0.5f);
        Hostile.Circle(at, r, warn, Bright(color));
        Hostile.Circle(at, r * 0.55f, warn, new Color(1f, 1f, 1f, 0.75f));
        Warn(at, warn, r);
        yield return new WaitForSeconds(warn);
        Fx.Spawn(fx, at + Vector3.up * (fx == "fx_meteor" ? 0.5f : 1.2f), r * 2.2f, Color.white, 18f, 0f, 14);
        Fx.Spawn("fx_explosion", at, r * 1.8f, color, 20f);
        Hostile.HitCircle(at, r, damage);
    }

    // ================================================================= 도우미
    // 결계 공격 예고: 크게 흔들리는 느낌표 + 번쩍이는 원 (어두운 결계 안에서도 바로 보이게)
    static void Warn(Vector3 at, float duration, float size)
    {
        FxAnim a = Fx.Play("fx_warn", at + Vector3.up * 0.3f, Mathf.Clamp(size * 0.9f, 1.3f, 2.4f), Color.white, 6f, 0f, 45, true, duration);
        if (a != null) a.gameObject.AddComponent<WarnPop>();
        Fx.Spawn("fx_shock", at, Mathf.Max(2f, size * 2.2f), new Color(1f, 0.3f, 0.25f, 0.85f), 22f);
    }

    // 예고 색을 더 진하고 밝게
    static Color Bright(Color c) => new Color(Mathf.Min(1f, c.r * 1.15f + 0.1f), Mathf.Min(1f, c.g * 1.15f + 0.05f), Mathf.Min(1f, c.b * 1.15f + 0.05f), 0.95f);

    // 결계 밖을 덮는 어둠 (둥근 구멍이 뚫린 큰 고리 메시): 안쪽은 보스 색으로 살짝 물듦
    MeshRenderer DarkMesh(Transform parent, Color color)
    {
        GameObject go = new GameObject("DomainDark", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
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
        go.GetComponent<MeshFilter>().mesh = m;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
        // 바닥 층 맨 위 (바닥 타일 · 장판 위, 캐릭터 · 적 · 적 탄 아래): 맵만 어두워지고 싸우는 것들은 또렷하게
        // (예전엔 이펙트 층이라 결계 안의 캐릭터까지 어둡게 덮어 잘 보이지 않았음)
        mr.sortingLayerName = "Background";
        mr.sortingOrder = 40;
        darkTint = color;
        return mr;
    }

    Color darkTint;

    void SetDark(MeshRenderer mr, float k)
    {
        if (mr == null) return;
        MeshFilter f = mr.GetComponent<MeshFilter>();
        Mesh m = f.mesh;
        int seg = m.vertexCount / 4;
        Color inner = new Color(darkTint.r * 0.25f, darkTint.g * 0.15f, darkTint.b * 0.3f, 0.32f * k);
        Color edge = new Color(0f, 0f, 0f, 0.55f * k);
        Color outer = new Color(0.01f, 0f, 0.02f, 0.9f * k);
        Color[] c = new Color[m.vertexCount];
        for (int i = 0; i < c.Length; i++)
        {
            int ri = i / seg;
            c[i] = ri <= 1 ? (ri == 0 ? inner : Color.Lerp(inner, edge, 0.3f)) : ri == 2 ? outer : outer;
        }
        m.colors = c;
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
    float age, trail;

    void Update()
    {
        age += Time.deltaTime;
        if (age >= life || !BossUltimate.Active) { Destroy(gameObject); return; }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        Vector3 d = transform.position - center;
        if (d.magnitude > radius)
        {
            Vector2 n = d.normalized;
            velocity = Vector2.Reflect(velocity, -n);
            transform.position = center + (Vector3)(n * radius);
        }
        transform.localScale = Vector3.one * (0.46f + 0.04f * Mathf.Sin(age * 12f));     // HostileProjectile.Dress 의 빛 크기 (지름 1.6 × 2.3 ÷ 8)
        trail += Time.deltaTime;
        if (trail > 0.12f) { trail = 0f; Fx.Spawn("fx_puddle", transform.position, 1.2f, new Color(0.55f, 1f, 0.35f, 0.6f), 14f); }
        PlayerController p = Hostile.Player;
        if (p != null && Vector2.Distance(p.transform.position, transform.position) < 1.1f) p.TryHit(damage);
    }
}

// 결계 공격 예고 느낌표: 튀어나왔다가 두근거림
public class WarnPop : MonoBehaviour
{
    Vector3 baseScale;
    float t;

    void Start() => baseScale = transform.localScale;

    void Update()
    {
        t += Time.deltaTime;
        float pop = t < 0.15f ? Mathf.Lerp(1.8f, 1f, t / 0.15f) : 1f + 0.12f * Mathf.Sin(t * 18f);
        transform.localScale = baseScale * pop;
    }
}
