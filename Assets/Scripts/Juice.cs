using UnityEngine;

// 손맛 연출: 적 처치 · 레벨업 · 코인 획득 때 도트 이펙트와 소리 (tools/pixelart/make_fx.py 로 만든 시트)
public static class Juice
{
    static float lastPop;

    // 스테이지마다 영혼 색이 다름 (1장 청록 · 2장 불꽃 · 3장 풀빛 · 4장 보랏빛 · 무한 금빛)
    static Color StageColor()
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 0;
        return stage == 0 ? new Color(0.6f, 0.95f, 1f) : stage == 1 ? new Color(1f, 0.58f, 0.28f)
             : stage == 2 ? new Color(0.65f, 1f, 0.5f) : GameMode.IsEndless ? new Color(1f, 0.82f, 0.42f) : new Color(0.78f, 0.62f, 1f);
    }

    public static void EnemyDied(Vector3 pos, float size)
    {
        Color c = SkinFx.KillColor(StageColor());       // 이펙트 스킨이 있으면 스킨 색
        Fx.Spawn("fx_deathburst", pos, 2.4f * size, c, 22f);
        if (size > 1.2f) Fx.Spawn("fx_shock", pos, 3.5f * size, new Color(c.r, c.g, c.b, 0.7f), 18f);
        // 한꺼번에 많이 죽어도 소리가 뭉개지지 않게
        if (Time.time - lastPop > 0.045f)
        {
            lastPop = Time.time;
            Hostile.Play("pop", 0.35f, Random.Range(0.85f, 1.2f));
        }
        OnKill();       // 1.1~ 처치 수 이정표 · 판 수
    }

    public static void LevelUp(Vector3 pos)
    {
        Fx.Spawn("fx_levelup", pos + Vector3.up * 1f, 3.2f, Color.white, 14f, 0f, 30);
        Fx.Spawn("fx_sparkle", pos + new Vector3(-0.8f, 1.8f, 0f), 0.9f, new Color(1f, 0.9f, 0.5f), 12f, 0f, 31);
        Fx.Spawn("fx_sparkle", pos + new Vector3(0.9f, 2.4f, 0f), 0.7f, new Color(1f, 0.9f, 0.5f), 10f, 0f, 31);
        Hostile.Play("levelup", 0.8f);
    }

    // sound: 코인을 한꺼번에 여러 개 주우면 소리는 가끔만 (PlayerController 가 정함)
    public static void CoinPicked(Vector3 pos, bool sound = true)
    {
        Fx.Spawn("fx_sparkle", pos + (Vector3)(Random.insideUnitCircle * 0.3f), SkinFx.EffectSkin != null ? 1.1f : 0.8f, SkinFx.CoinColor(new Color(1f, 0.88f, 0.4f)), 16f, 0f, 20);
        if (sound) Hostile.Play("sparkle", 0.2f, Random.Range(0.95f, 1.15f));
    }

    // ================================================================= 1.1~ 손맛 묶음 (로드맵 1-1)
    static Juice() => LocTable.EnsureJuice();

    // ---------------- 히트스톱 · 슬로모션
    // 시간이 1배일 때만 걸고, 그 사이 다른 곳(일시정지 · 레벨업 · 필살기 조준 · 보스 필살기 창)이 시간을 바꾸면 JuiceRunner 가 손을 뗌
    // 0 으로 멈추면 「일시정지」로 여기는 곳(입력 · 음악 · 도움말)이 많아서, 멈춤도 0.02배로
    const float StopScale = 0.02f, SlowScale = 0.15f;
    static float lastStopAt = -9f;

    static bool CanStop()
    {
        if (GameInput.Auto || Application.isBatchMode || StoryDirector.Playing || PlayerController.Cinematic) return false;
        PlayerController p = Hostile.Player;
        if (p != null && p.IsDying) return false;
        return Time.timeScale == 1f || (runner != null && runner.Owns);
    }

    // 엘리트 · 중간 보스 처치: 0.06초 멈춤 (1초 안에는 한 번만)
    public static void HitStop(float seconds = 0.06f)
    {
        if (Time.unscaledTime - lastStopAt < 1f || !CanStop()) return;
        lastStopAt = Time.unscaledTime;
        Runner.Hold(StopScale, seconds);
    }

    // 장 보스 처치: 0.35초 슬로모션(0.15배) + 화면 흰 번쩍임 (방금 멈춤이 있었어도 보스는 덮어씀 · 1초 제한도 보지 않음)
    // 번쩍임 효과를 끈 설정(광과민 접근성)이면 번쩍임 없이 슬로모션만
    public static void BossDown()
    {
        if (Application.isBatchMode) return;
        if (GameSettings.Flashes) Runner.Flash(0.55f);
        if (!CanStop()) return;
        lastStopAt = Time.unscaledTime;
        Runner.Hold(SlowScale, 0.35f);
    }

    static JuiceRunner runner;
    static JuiceRunner Runner
    {
        get
        {
            if (runner == null) runner = new GameObject("JuiceRunner").AddComponent<JuiceRunner>();     // 씬과 함께 사라짐
            return runner;
        }
    }

    // ---------------- 적 피격 흰 번쩍임 (SpriteOutline 셰이더의 _Flash, 렌더러마다)
    public static readonly int FlashId = Shader.PropertyToID("_Flash");
    static MaterialPropertyBlock flashBlock;
    public static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

    public static void SetFlash(SpriteRenderer sr, bool on)
    {
        if (sr == null) return;
        // 1.1.1: 끌 때 SetPropertyBlock(null) 로 블록을 통째로 지우면 스프라이트 그림(_MainTex)까지 지워져 적이 하얗게 남았음 → 값만 0 으로
        if (flashBlock == null) flashBlock = new MaterialPropertyBlock();
        sr.GetPropertyBlock(flashBlock);
        flashBlock.SetFloat(FlashId, on ? 1f : 0f);
        sr.SetPropertyBlock(flashBlock);
    }

    // ---------------- 총알 명중음: 0.03초 간격, 동시에 최대 4개
    const float HitLen = 0.07f;
    static float lastHit = -9f;
    static readonly float[] hitTimes = { -9f, -9f, -9f, -9f };
    static int hitNext;

    public static void BulletHit()
    {
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        if (fx == null) return;
        float now = Time.unscaledTime;
        if (now - lastHit < 0.03f || now - hitTimes[hitNext] < HitLen) return;      // 가장 오래된 소리가 아직 울리면 4개가 겹친 것
        lastHit = now;
        hitTimes[hitNext] = now;
        hitNext = (hitNext + 1) % hitTimes.Length;
        fx.MakeClip("juice_hit", HitLen, HitWave);     // 이미 있으면 그대로
        fx.PlayRaw("juice_hit", 0.35f);                 // 음높이 1 그대로 (음높이 소스 묶음을 처치 소리와 다투지 않게)
    }

    static float HitWave(float t, float d, System.Random r)
    {
        float n = (float)(r.NextDouble() * 2.0 - 1.0);
        float k = Mathf.Clamp01(1f - t / d);
        return (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(700f, 220f, t / d) * t) * 0.55f + n * 0.5f * Mathf.Pow(Mathf.Clamp01(1f - t / 0.012f), 2f)) * k * k * k;
    }

    // ---------------- 처치 수 이정표: 100 · 250 · 500 · 1000 마다 배너와 코인
    // 처치 수(RunStats.Kills)는 이어하기에 저장되므로 따로 기록하지 않아도 같은 이정표가 두 번 나오지 않음
    static readonly int[] Milestones = { 100, 250, 500, 1000 };
    static readonly int[] MilestoneCoins = { 10, 25, 50, 100 };

    static void CheckMilestone()
    {
        int kills = RunStats.Kills;
        for (int i = 0; i < Milestones.Length; i++)
        {
            if (kills != Milestones[i]) continue;
            Coin coin = Cache<Coin>.Get;
            if (coin != null) coin.AddCoin(MilestoneCoins[i]);
            if (StageManager.Instance != null)
                StageManager.Instance.ShowBanner(Loc.T("juice.milestone").Replace("{0}", kills.ToString()).Replace("{1}", MilestoneCoins[i].ToString()), 2.2f);
            Hostile.Play("chime", 0.7f);
            return;
        }
    }

    // ---------------- 판 수: 3번 미만이면 레벨업 창이 저절로 열림 (LevelShop)
    const string RunsKey = "juice.runs";
    static bool playedThisRun;

    public static int RunsPlayed => Prefs.GetInt(RunsKey, 0);
    public static bool NewPlayer => RunsPlayed < 3;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootRuns()
    {
        // 처음 한 번: 예전 판 기록(balance_log.csv) 줄 수로 채움 (1.1 전부터 하던 사람에게 창이 저절로 열리지 않게)
        if (!Prefs.HasKey(RunsKey))
        {
            int seed = 0;
            try
            {
                string path = RunStats.BalanceLogPath;
                if (System.IO.File.Exists(path)) seed = Mathf.Max(0, System.IO.File.ReadAllLines(path).Length - 1);
            }
            catch { seed = 0; }
            Prefs.SetInt(RunsKey, seed);
        }
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
        {
            if (!playedThisRun) return;
            playedThisRun = false;
            Prefs.SetInt(RunsKey, RunsPlayed + 1);
            Prefs.Save();
        };
    }

    // 적 처치마다 (EnemyDied 에서): 이정표 · 판 수
    static void OnKill()
    {
        if (!TutorialRun.Active && !GameInput.Auto && !GameInput.TrailerRunning && !Application.isBatchMode) playedThisRun = true;
        if (EnermyController.KillCounts) CheckMilestone();      // 1.2~ 처치 수가 오르지 않은 처치(정화 절반)는 같은 수로 이정표를 또 보지 않게
    }
}

// 1.1~ 히트스톱 · 슬로모션 시간 관리와 보스 처치 화면 번쩍임 (실제 시간으로)
public class JuiceRunner : MonoBehaviour
{
    float scale = 1f, until;
    bool active;
    UnityEngine.UI.Image flash;
    float flashAt = -9f, flashStrength;
    const float FlashFor = 0.35f;

    // 지금 시간 배율이 우리가 건 값인지
    public bool Owns => active && Mathf.Abs(Time.timeScale - scale) < 0.0001f;

    // 물리 간격도 배율만큼 줄여 슬로모션 중 FixedUpdate 움직임이 뚝뚝 끊기지 않게 (원래 값은 처음 바꿀 때 한 번만 기억)
    float baseFixed;
    bool fixedChanged;

    public void Hold(float s, float seconds)
    {
        until = Mathf.Max(Owns ? until : 0f, Time.unscaledTime + seconds);
        scale = s;
        active = true;
        Time.timeScale = s;
        if (!fixedChanged) { baseFixed = Time.fixedDeltaTime; fixedChanged = true; }
        Time.fixedDeltaTime = baseFixed * s;
    }

    void Release()
    {
        active = false;
        if (fixedChanged) { Time.fixedDeltaTime = baseFixed; fixedChanged = false; }
    }

    public void Flash(float strength)
    {
        if (flash == null)
        {
            GameObject cg = new GameObject("JuiceFlash", typeof(Canvas));
            cg.transform.SetParent(transform, false);
            Canvas c = cg.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 900;           // HUD 위 · 툴팁(1000) · 이야기(31000) 아래
            GameObject ig = new GameObject("White", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            RectTransform r = ig.GetComponent<RectTransform>();
            r.SetParent(cg.transform, false);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            flash = ig.GetComponent<UnityEngine.UI.Image>();
            flash.raycastTarget = false;
            flash.color = new Color(1f, 1f, 1f, 0f);
        }
        flashAt = Time.unscaledTime;
        flashStrength = strength;
    }

    void Update()
    {
        if (flash != null)
        {
            float k = 1f - (Time.unscaledTime - flashAt) / FlashFor;
            float a = k > 0f ? k * k * flashStrength : 0f;
            if (flash.enabled != a > 0f) flash.enabled = a > 0f;
            if (a > 0f) flash.color = new Color(1f, 1f, 1f, a);
        }
    }

    // 시간 관리는 LateUpdate 에서: 다른 곳이 같은 프레임 Update 에서 시간을 바꿨으면 다음 물리 단계 전에 물리 간격을 되돌림
    void LateUpdate()
    {
        if (!active) return;
        float t = Time.timeScale;
        if (t == 0f) return;                                    // 다른 창이 멈춤 → 그 창이 우리 값으로 돌려놓으면 그때 1배로
        if (Mathf.Abs(t - scale) > 0.0001f) { Release(); return; }      // 다른 곳이 시간을 바꿈 → 배율은 손대지 않고 물리 간격만 되돌림
        if (Time.unscaledTime >= until) { Time.timeScale = 1f; Release(); }
    }

    // 씬이 바뀌어 사라질 때 우리 배율이 남아 있으면 되돌림 (물리 간격은 언제나 되돌림)
    void OnDestroy()
    {
        if (Owns) Time.timeScale = 1f;
        Release();
    }
}
