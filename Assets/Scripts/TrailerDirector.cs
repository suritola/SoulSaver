#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 트레일러 자동 촬영 모드 (에디터 전용 · 출시 빌드에는 들어가지 않음)
// 메뉴 Soul Saver → 트레일러 녹화 로 켜면 게임 씬에서 주인공이 스스로 싸우며 정해진 장면을 연기함
// 차별점 구현 3-3: 상점 트레일러(tools/trailer/GameplayCapture.cs)와 첫 10초 · 자막을 같게 — 거너만으로 되는 부분
// 「NO AUTO-AIM.」 커서가 보이는 단발 조준 → 8발 · 저절로 재장전 「AIM. RELOAD.」 → 무기 8종 「9 HEROES. 9 HANDS.」
// → 스킬 트리 · 진화 「37 SKILL EVOLUTIONS.」 → 첫 보스 결계 · 보스 4종 「4 WORLDS. 4 KINGS.」 → 로고 · 「FREE DEMO ON STEAM」 (약 58초)
// Unity Recorder 는 OS 커서를 찍지 않으므로 게임 커서 그림을 화면 위에 겹쳐 그림
public class TrailerDirector : MonoBehaviour
{
    public const string PrefKey = "trailer.run";
    public static bool Running { get; private set; }
    public static bool Finished { get; private set; }
    public static float Length = 58f;          // 대략 길이 (녹화 도구가 참고)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Running = false;
        Finished = false;
        SceneManager.sceneLoaded += (s, m) => TryStart();
        TryStart();
    }

    static void TryStart()
    {
        if (Running || Prefs.GetInt(PrefKey, 0) != 1) return;
        if (SceneManager.GetActiveScene().name != "GameScene") return;
        Prefs.SetInt(PrefKey, 0);
        Prefs.Save();
        new GameObject("TrailerDirector").AddComponent<TrailerDirector>();
    }

    // ================================================================= 준비
    PlayerController player;
    SpecialAbilities specials;
    StageManager stages;
    EnemySpawner spawner;
    Camera cam;
    Vector3 caveStart;
    Color caveBackground;
    CodexData data;

    Canvas overlay;
    Image black;
    TextMeshProUGUI caption, logo, sub;

    float nextShot;
    bool holdFire;          // 특수 무기 연사
    bool autopilot = true;  // 가까운 적을 조준 · 사격 · 거리 유지
    bool manualFire;        // 참이면 자동 사격을 끄고 AimShots 가 한 발씩 쏨
    Transform aimTarget;    // manualFire 중 겨눌 표적
    RawImage cursorImage;
    // 사람 같은 조준: 표적이 바뀌면 커서를 0.25초 smoothstep 으로 옮김 (예전엔 표적에 바로 붙어 조준이 안 보였음)
    const float AimTime = 0.25f;
    Vector3 aimNow, aimFrom, aimTo;
    float aimT = 1f;

    void Start()
    {
        Running = true;
        Finished = false;
        player = FindFirstObjectByType<PlayerController>();
        specials = FindFirstObjectByType<SpecialAbilities>();
        stages = StageManager.Instance;
        spawner = FindFirstObjectByType<EnemySpawner>();
        cam = Camera.main;
        caveStart = player.transform.position;
        caveBackground = cam.backgroundColor;
        data = CodexData.Load();
        GameInput.Auto = true;
        BuildOverlay();
        StartCoroutine(Play());
    }

    void OnDestroy()
    {
        Running = false;
        GameInput.Auto = false;
    }

    // ================================================================= 장면 순서
    IEnumerator Play()
    {
        SetBlack(1f);
        Swarm(0, 7);
        manualFire = true;
        aimNow = aimTo = player.transform.position + new Vector3(-3f, -1.5f, 0f);     // 커서는 빈 땅에서 시작해 첫 표적으로 미끄러져 감
        yield return Fade(1f, 0f, 0.6f);

        // 1) 자동 조준은 없다 — 커서가 보이는 단발 조준 3발
        StartCoroutine(Caption("NO AUTO-AIM.", 1.8f));
        yield return AimShots(3);
        yield return Wait(0.3f);

        // 2) 8발 → 빈 탄창이면 PlayerController 가 저절로 재장전
        StartCoroutine(Caption("AIM.  RELOAD.", 2.6f));
        yield return AimShots(8);
        yield return Wait(1.0f);
        manualFire = false;
        aimTarget = null;
        yield return Ult(1.4f);
        yield return Wait(0.8f);

        // 3) 무기 8종 필살기 (지옥 4 · 초원 4)
        specials.Equip(new[] { 0, 1, 3, 2, 5, 4, 7, 6, 16 });
        yield return Stage(1);
        StartCoroutine(Caption("9 HEROES.  9 HANDS.", 5f));
        foreach (int w in new[] { 0, 1, 3, 2 }) yield return WeaponShowcase(w);
        yield return Stage(2);
        foreach (int w in new[] { 5, 4, 7, 6 }) yield return WeaponShowcase(w);

        // 4) 스킬 트리 · 진화
        stages.specialPoints = 3;
        autopilot = false;
        GameInput.AutoMove = Vector2.zero;
        GameInput.AutoFire = false;
        stages.OpenUpgrade();
        StartCoroutine(Caption("37 SKILL EVOLUTIONS.", 3f));
        yield return Wait(2.6f);
        stages.CloseUpgradeNow();
        autopilot = true;
        specials.Evolve(5);
        specials.Evolve(16, 1);
        yield return Wait(1.2f);

        // 5) 보스 4종 (첫 보스는 결계를 바로 띄움)
        for (int b = 0; b < 4; b++)
        {
            yield return Stage(b);
            if (b == 0) StartCoroutine(Caption("4 WORLDS.  4 KINGS.", 2.5f));
            yield return Boss(b, b == 0);
        }

        // 6) 로고 · 체험판
        autopilot = false;
        GameInput.AutoMove = Vector2.zero;
        GameInput.AutoFire = false;
        yield return Fade(0f, 1f, 0.5f);
        logo.gameObject.SetActive(true);
        sub.gameObject.SetActive(true);
        for (float t = 0f; t < 0.8f; t += Dt)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 0.8f);
            logo.alpha = k; sub.alpha = Mathf.Clamp01(k * 1.5f - 0.5f);
            logo.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, k);
            yield return null;
        }
        yield return Wait(3.4f);
        Finished = true;
        Debug.Log("[Trailer] 끝");
    }

    // 무기 하나: 잠깐 쏘고 → 필살기
    IEnumerator WeaponShowcase(int w)
    {
        specials.SelectWeapon(w);
        holdFire = true;
        yield return Wait(w == 1 ? 0.9f : 0.7f);
        holdFire = false;
        yield return Wait(0.1f);
        bool instant = w == SpecialAbilities.ShotgunId || w == SpecialAbilities.DualId || w == SpecialAbilities.GrenadeId;
        yield return Ult(instant ? 0f : 0.9f);
        yield return Wait(instant ? 1.1f : 1.0f);
    }

    // 게이지를 채우고 우클릭 (hold 초 동안 누르고 뗌 · 0이면 즉발)
    IEnumerator Ult(float hold)
    {
        if (player.skillGauge != null) player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint);
        yield return null;
        GameInput.AutoUlt = true;
        GameInput.UltDownFrame = Time.frameCount + 1;
        yield return Wait(Mathf.Max(hold, 0.05f));
        GameInput.AutoUlt = false;
        GameInput.UltUpFrame = Time.frameCount + 1;
        yield return null;
        yield return null;
    }

    // 커서를 표적으로 옮긴 뒤(0.25초) 한 발씩 — 탄창이 비면 저절로 재장전이 시작되어 멈춤
    IEnumerator AimShots(int count)
    {
        for (int i = 0; i < count && !player.IsReloading; i++)
        {
            aimTarget = Pick(player.transform.position, i % 2 == 1);
            yield return Wait(AimTime + 0.03f);
            GameInput.FireDownFrame = Time.frameCount + 1;
            GameInput.AutoFire = true;
            yield return null;
            yield return null;
            GameInput.AutoFire = false;
            GameInput.FireUpFrame = Time.frameCount + 1;
            yield return Wait(0.04f);
        }
    }

    // 가장 가까운 잡몹 · 두 번째로 가까운 잡몹 (표적마다 커서가 옮겨 다니게)
    static Transform Pick(Vector3 me, bool second)
    {
        Transform a = null, b = null;
        float da = 13f * 13f, db = da;
        foreach (EnermyController e in FindObjectsByType<EnermyController>(FindObjectsSortMode.None))
        {
            if (e.IsDead) continue;
            float d = (e.transform.position - me).sqrMagnitude;
            if (d < da) { b = a; db = da; a = e.transform; da = d; }
            else if (d < db) { b = e.transform; db = d; }
        }
        return second && b != null ? b : a;
    }

    IEnumerator Boss(int stage, bool barrier = false)
    {
        GameObject prefab = stage < data.bosses.Length ? data.bosses[stage] : null;
        if (prefab == null && spawner.stages != null && stage < spawner.stages.Length) prefab = spawner.stages[stage].bossPrefab;     // 4장 거울의 군주는 스폰러에만 있음
        if (prefab == null) yield break;
        Vector3 at = player.transform.position + new Vector3(6.5f, 3.5f, 0f);
        GameObject boss = Instantiate(prefab, at, Quaternion.identity);
        spawner.bossSpawned = true;                       // 보스전 음악
        bossbar bar = FindFirstObjectByType<bossbar>();
        if (bar != null) bar.bossSpawn = true;
        bosss b = boss.GetComponent<bosss>();
        yield return null;
        if (b != null) b.EnemyHealth = b.setEnemyHP * 0.48f;   // 바로 특수 패턴
        // 첫 보스전: 결계(보스 필살기)를 바로 띄움 — GameplayCapture 의 Barrier 와 같은 방법 (Run 은 private 이라 리플렉션)
        if (barrier && b != null)
        {
            BossUltimate ult = b.GetComponent<BossUltimate>();
            if (ult == null) ult = b.gameObject.AddComponent<BossUltimate>();
            yield return null;
            if (typeof(BossUltimate).GetMethod("Run", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(ult, null) is IEnumerator run)
                ult.StartCoroutine(run);
            yield return Wait(0.3f);
            for (float t = 0f; t < 6f && BossUltimate.CutscenePlaying; t += Dt) yield return null;
            yield return Wait(1.6f);                      // 결계 안에서 쏟아지는 공격을 피함
        }
        specials.SelectWeapon(stage == 0 ? -1 : stage == 1 ? 3 : 5);
        holdFire = true;
        yield return Wait(2.2f);
        holdFire = false;
        yield return Ult(stage == 0 ? 1.2f : 0.8f);
        yield return Wait(1.3f);
        foreach (bosss s in FindObjectsByType<bosss>(FindObjectsSortMode.None)) Destroy(s.gameObject);
        spawner.bossSpawned = false;
        if (bar != null) bar.bossSpawn = false;
    }

    // 검은 화면으로 스테이지 이동 + 적 무리 배치
    IEnumerator Stage(int stage)
    {
        yield return Fade(0f, 1f, 0.25f);
        stages.JumpToStage(stage, caveStart);
        if (stage == 0) cam.backgroundColor = caveBackground;
        foreach (EnermyController e in FindObjectsByType<EnermyController>(FindObjectsSortMode.None)) Destroy(e.gameObject);
        yield return null;
        Swarm(stage, 8);
        yield return Fade(1f, 0f, 0.25f);
    }

    // 주인공 둘레에 그 스테이지 적을 미리 깔아 둠 (화면이 비지 않게)
    void Swarm(int stage, int count)
    {
        if (data == null) return;
        List<GameObject> pool = new List<GameObject>();
        for (int i = 0; i < data.enemies.Length; i++) if (data.enemyStage[i] == stage) pool.Add(data.enemies[i]);
        Vector3 p = player.transform.position;
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            float a = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            Instantiate(pool[i % pool.Count], p + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(6f, 9f), Quaternion.identity);
        }
    }

    // ================================================================= 자동 조종
    void Update()
    {
        if (!autopilot) return;
        Vector3 me = player.transform.position;
        Transform target = Nearest(me);
        Vector3 desired = me + Vector3.right * 5f;
        if (manualFire && aimTarget != null) desired = aimTarget.position;
        else if (target != null) desired = target.position;
        if ((desired - aimTo).sqrMagnitude > 1.5f * 1.5f) { aimFrom = aimNow; aimT = 0f; }
        aimTo = desired;
        aimT = Mathf.Min(1f, aimT + Dt / AimTime);
        aimNow = aimT >= 1f ? aimTo : Vector3.Lerp(aimFrom, aimTo, Mathf.SmoothStep(0f, 1f, aimT));
        GameInput.AutoAim = aimNow;
        if (target != null)
        {
            Vector2 to = target.position - me;
            float d = to.magnitude;
            // 가까우면 물러나고, 멀면 다가가고, 옆으로 돌며 싸움
            Vector2 side = new Vector2(-to.y, to.x).normalized;
            Vector2 move = side * 0.8f + (d < 4f ? -to.normalized : d > 7f ? to.normalized * 0.7f : Vector2.zero);
            GameInput.AutoMove = move;
        }
        else
        {
            GameInput.AutoMove = Vector2.zero;
        }

        if (manualFire) return;
        GameInput.AutoFire = holdFire && target != null;
        if (target != null && Time.time >= nextShot && !specials.WeaponActive)
        {
            GameInput.FireDownFrame = Time.frameCount + 1;
            nextShot = Time.time + 0.22f;
        }
    }

    // 죽지 않고, 레벨업 창이 뜨지 않게
    void LateUpdate()
    {
        player.PlayerHealth = player.PlayerMaxHealth;
        player.nowEXP = 0f;
        GameObject shop = GameObject.Find("LevelShopPanel");
        if (shop != null && shop.activeSelf) shop.SetActive(false);

        // 커서: 지금 겨누는 자리를 화면 비율 앵커로 옮김 (위치만 바꿈)
        if (cursorImage != null)
        {
            Vector3 v = autopilot && cam != null ? cam.WorldToViewportPoint(aimNow) : Vector3.zero;
            bool on = autopilot && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f;
            cursorImage.enabled = on;
            if (on)
            {
                cursorImage.rectTransform.anchorMin = cursorImage.rectTransform.anchorMax = new Vector2(v.x, v.y);
                cursorImage.rectTransform.anchoredPosition = Vector2.zero;
            }
        }
    }

    Transform Nearest(Vector3 from)
    {
        Transform best = null; float bestD = 18f * 18f;
        foreach (bosss b in FindObjectsByType<bosss>(FindObjectsSortMode.None))
        {
            if (b.IsDead) continue;
            float d = (b.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = b.transform; }
        }
        if (best != null) return best;
        foreach (EnermyController e in FindObjectsByType<EnermyController>(FindObjectsSortMode.None))
        {
            if (e.IsDead) continue;
            float d = (e.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = e.transform; }
        }
        return best;
    }

    // ================================================================= 화면 연출
    void BuildOverlay()
    {
        GameObject go = new GameObject("TrailerOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        overlay = go.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 32000;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 1f;
        UiScaler.Fit(cs);               // UiScaler 가 1초마다 찾지 않으므로 만들 때 맞춤

        black = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        black.transform.SetParent(go.transform, false);
        Stretch(black.rectTransform);
        black.color = Color.black;
        black.raycastTarget = false;

        TMP_FontAsset font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name.StartsWith("Cafe24"));
        Material outline = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name.StartsWith("Cafe24") && m.name.Contains("Outline"));
        caption = Text(go.transform, font, outline, 64f, new Vector2(0f, -330f), new Vector2(1700f, 110f));
        logo = Text(go.transform, font, outline, 230f, new Vector2(0f, 60f), new Vector2(1700f, 280f));
        logo.text = "Soul Saver";
        sub = Text(go.transform, font, null, 54f, new Vector2(0f, -150f), new Vector2(1700f, 90f));
        sub.text = "AIM.  RELOAD.  EVOLVE.\nFREE DEMO ON STEAM";
        sub.rectTransform.anchoredPosition = new Vector2(0f, -190f);
        sub.rectTransform.sizeDelta = new Vector2(1700f, 160f);
        sub.color = new Color(0.92f, 0.88f, 0.8f);
        caption.alpha = 0f;
        logo.gameObject.SetActive(false);
        sub.gameObject.SetActive(false);

        // 게임 커서 그림 (금빛 조준 커서 · 없으면 기본) — 검은 화면 · 자막보다 아래
        Texture2D cur = Resources.Load<Texture2D>("Cursors/cursor_gold_aim_0");
        if (cur == null) cur = Resources.Load<Texture2D>("Cursors/cursor_default_aim");
        if (cur != null)
        {
            cursorImage = new GameObject("Cursor", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            cursorImage.transform.SetParent(go.transform, false);
            cursorImage.transform.SetSiblingIndex(0);
            cursorImage.texture = cur;
            cursorImage.raycastTarget = false;
            cursorImage.rectTransform.sizeDelta = new Vector2(64f, 64f);
            cursorImage.enabled = false;
        }
    }

    static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, Material mat, float size, Vector2 pos, Vector2 box)
    {
        TextMeshProUGUI t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = box;
        if (font != null) t.font = font;
        if (mat != null) t.fontSharedMaterial = mat;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.96f, 0.83f, 0.47f);
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    void SetBlack(float a) => black.color = new Color(0f, 0f, 0f, a);

    IEnumerator Fade(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Dt) { SetBlack(Mathf.Lerp(from, to, t / time)); yield return null; }
        SetBlack(to);
    }

    IEnumerator Caption(string text, float time)
    {
        caption.text = text;
        for (float t = 0f; t < time; t += Dt)
        {
            caption.alpha = Mathf.Min(Mathf.Clamp01(t / 0.3f), Mathf.Clamp01((time - t) / 0.4f));
            yield return null;
        }
        caption.alpha = 0f;
    }

    // 녹화 중(Time.captureFramerate > 0)에는 실제 시간이 아니라 프레임 수로 시간을 셈
    // (컴퓨터가 느려도 영상 속 길이가 정확하게)
    static float Dt => Time.captureFramerate > 0 ? 1f / Time.captureFramerate : Time.unscaledDeltaTime;

    static IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Dt) yield return null;
    }
}
#endif
