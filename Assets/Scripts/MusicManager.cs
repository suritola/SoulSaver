using UnityEngine;
using UnityEngine.SceneManagement;

// 배경음악: 메뉴 / 1장 / 2장 / 3장 / 4장 / 보스전 / 결계 / 무한 모드 최종 보스(개발자 · 엄마) 곡을 상황에 맞게 부드럽게 바꿔 틀어줌
// 게임이 시작되면 스스로 만들어지고 씬이 바뀌어도 유지됨 (Resources/Music/bgm_*.wav)
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    // 음악은 효과음보다 조금 작게 (전체 볼륨은 설정의 AudioListener.volume, 음악 볼륨은 여기에 곱함)
    const float MusicGain = 0.5f;
    const float FadeTime = 1.2f;

    AudioSource current, previous;
    string playing;
    float currentGain = 1f;

    // 1.1~: 곡마다 음량 차이(RMS -10.5 ~ -19.8 dBFS)를 맞추는 보정 — 약 -15 dBFS 기준 (결계 곡만 조금 크게 -13)
    // 장면이 바뀔 때 음악이 갑자기 커지거나 작아지지 않게. MusicGain 0.5 를 곱해도 1 을 넘지 않음
    static float TrackGain(string name)
    {
        switch (name)
        {
            case "bgm_cave": return 1.74f;      // -19.8 → +4.8 dB
            case "bgm_abyss": return 1.35f;     // -17.6 → +2.6 dB
            case "bgm_hell": return 1.26f;      // -17.0 → +2.0 dB
            case "bgm_boss": return 1.07f;      // -15.6 → +0.6 dB
            case "bgm_meadow": return 0.98f;    // -14.8
            case "bgm_desert": return 0.93f;    // -14.4
            case "bgm_menu": return 0.76f;      // -12.6 → -2.4 dB
            case "bgm_domain": return 0.75f;    // -10.5 → -2.5 dB
            // 1.1.2~ 무한 모드 최종 보스 곡 (tools/audio/make_developer_bgm.py 가 RMS -15.0 으로 맞춰 냄): 기준보다 1 dB 크게 — 보스 곡과 결계 곡(-13) 사이
            case "bgm_developer": return 1.12f;
            case "bgm_developer_mom": return 1.12f;
            default: return 1f;
        }
    }

    // 1.1~: 곡마다 마지막 재생 위치 — 결계가 끝나 보스 곡으로 돌아오면 처음부터가 아니라 이어서 (1분 안에 돌아올 때만)
    readonly System.Collections.Generic.Dictionary<AudioClip, int> resumeAt = new System.Collections.Generic.Dictionary<AudioClip, int>();
    readonly System.Collections.Generic.Dictionary<AudioClip, float> leftAt = new System.Collections.Generic.Dictionary<AudioClip, float>();
    EnemySpawner spawner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("MusicManager");
        DontDestroyOnLoad(go);
        go.AddComponent<MusicManager>();
    }

    void Awake()
    {
        Instance = this;
        current = NewSource();
        previous = NewSource();
        SceneManager.sceneLoaded += (s, m) => { Hook(null); spawner = null; bossLatched = false; };
    }

    AudioSource NewSource()
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.loop = true;
        s.playOnAwake = false;
        s.volume = 0f;
        s.spatialBlend = 0f;
        // 1.1.2~ 음악은 가장 높은 우선순위 (0) — 보스전처럼 효과음이 동시에 32개(실제 재생 목소리 수)를 넘으면
        // 우선순위가 같은(128) 효과음보다 작게 들리는 음악 쪽이 먼저 가상화돼 잠깐 끊기거나 늦게 돌아왔음
        s.priority = 0;
        return s;
    }

    int sceneHandle = -1;
    string sceneName = "";

    // 지금 틀어야 할 곡
    string Wanted()
    {
        // 장면 이름은 가져올 때마다 새 문자열이 생기므로, 장면이 바뀔 때만 다시 가져옴
        Scene active = SceneManager.GetActiveScene();
        if (active.handle != sceneHandle) { sceneHandle = active.handle; sceneName = active.name; }
        string scene = sceneName;
        if (scene != "GameScene" || StoryDirector.EndingPlaying) return "bgm_menu";
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        Hook(spawner);
        // 1.1.2~ 무한 모드 최종 보스 (개발자 · 엄마) 전용 곡 — 등장 배너 1초 전부터 쓰러질 때까지 (EndlessMode.FinalBossMusic · 둘이 함께면 엄마 곡)
        // 결계 곡보다 앞에 둠: 1.2.5~ 개발자 · 엄마도 결계(디버그 모드 · 엄마의 대청소)를 쓰지만 결계 동안에도 전용 곡을 그대로 둠,
        // 혹시 켜지더라도 24초짜리 결계 곡이 1분 넘게 고조되는 최종 곡을 끊지 않게. 보스 곡 붙잡아 두기(BossFight)는 이 아래에서만 평가되므로 서로 건드리지 않음
        if (GameMode.IsEndless && EndlessMode.Instance != null)
        {
            int final = EndlessMode.Instance.FinalBossMusic;
            if (final == 2) return "bgm_developer_mom";
            if (final == 1) return "bgm_developer";
        }
        if (BossUltimate.Active) return "bgm_domain";          // 보스 필살기 결계
        // 보스전이면 다른 무엇보다 보스 곡 (판단은 BossFight 한 곳에서만)
        // 1.1.2~: 예전엔 무한 모드 보스전에서 보스 곡 4바퀴(102.4초)마다 4장 곡(bgm_abyss)과 번갈아 틀어서,
        //         보스와 싸우는 중에 「갑자기 일반 배경음으로 바뀌는」 것처럼 들렸음 → 보스전 내내 보스 곡
        if (BossFight) return "bgm_boss";
        // 1.1~: 무한 모드는 같은 짧은 곡만 수십 분 반복되지 않게 돌려 씀 (보스가 없을 때만)
        // 1.2.5~ 무한 모드는 「보스를 쓰러뜨릴 때마다」 다음 곡으로 (사막 → 동굴 → 지옥 → 초원 → 심연 → …)
        //   예전엔 판 시간 128초마다 바꿔서, 첫 보스(4분)가 나오기도 전인 2분 8초쯤 아무 일도 없는데 사막 곡이 곡 중간에서 1.2초 만에 동굴 곡으로 바뀌었음
        //   (판 시간은 레벨업 · 상점 · 일시정지 창에서 멈추지만 음악은 계속 흘러 곡 끝과도 맞지 않았음). 이제 곡은 보스 곡을 사이에 두고만 바뀌고,
        //   처치 수는 이어하기에 저장되므로 이어해도 같은 곡. 일일 도전도 같음
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 0;
        if (GameMode.IsEndless)
        {
            if (EndlessMode.Instance == null) return EndlessTracks[0];      // 일일 도전도 같은 규칙 (예전 128초 시간 순환은 곡 중간에 갑자기 바뀌었음)
            return EndlessTracks[EndlessMode.Instance.BossesDefeated % EndlessTracks.Length];
        }
        return stage == 0 ? "bgm_cave" : stage == 1 ? "bgm_hell" : stage == 2 ? "bgm_meadow" : "bgm_abyss";
    }

    // 지금 보스전인가 (1.1.2~ 배경음의 유일한 판단 기준)
    // - 처치 수로 보스가 불렸고 아직 쓰러뜨리지 않음 (EnemySpawner 의 bossSpawned · bossCleared — 처치 · 장 이동 · 이어하기에서만 바뀜)
    // - 또는 살아 있는 보스가 하나라도 있음 (무한 모드에서 여러 보스가 함께 · 따로 쓰러질 때, 깃발이 잠깐 어긋나도 보스 곡 유지)
    // - 또는 무한 모드 최종 보스(개발자 · 엄마)가 내려오는 중 (앞 보스를 다 잡은 뒤 등장 연출 몇 초 동안 일반 곡으로 갔다 오지 않게)
    // 장 중반 이벤트 · 중간 보스 · 엘리트 · 습격 · 선택 창 · 결계 해제 등은 이 값을 바꾸지 않으므로 보스 곡을 덮을 수 없음
    // 1.1.2~ 한 번 보스 곡이 켜지면 아래 경우에만 내림 (위 조건이 한 프레임이라도 어긋나 일반 곡으로 튀지 않게)
    //   ① 보스 처치 (EnemySpawner.onBossDefeated — 처치 연출이 시작되는 순간)  ② 장이 바뀜  ③ 씬이 바뀜 (사망 · 메인 메뉴 · 이어하기)
    //   ④ 안전장치: 살아 있는 보스가 하나도 없고 위 조건도 모두 거짓인 채로 게임 시간 10초 (트레일러 촬영처럼 깃발만 직접 내리는 경우에 보스 곡이 남지 않게)
    bool bossLatched;           // 보스 곡을 틀고 있음
    bool bossDownSignal;        // 보스 곡을 켠 뒤 보스 처치 알림을 받음
    int latchStage = -1;        // 보스 곡을 켤 때의 장
    float looseSince = -1f;     // 조건이 모두 거짓이 된 게임 시각 (안전장치)
    const float LooseRelease = 10f;
    EnemySpawner hooked;

    void Hook(EnemySpawner sp)
    {
        if (hooked == sp) return;
        if (hooked != null) hooked.onBossDefeated -= OnBossDown;
        hooked = sp;
        if (hooked != null) hooked.onBossDefeated += OnBossDown;
    }

    void OnBossDown(int stage) { if (bossLatched) bossDownSignal = true; }

    void OnDestroy() => Hook(null);

    bool BossFight
    {
        get
        {
            int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : -1;
            if (BossFightNow)
            {
                if (!bossLatched) { bossLatched = true; bossDownSignal = false; }
                latchStage = stage;
                looseSince = -1f;
                return true;
            }
            if (!bossLatched) return false;
            if (looseSince < 0f) looseSince = Time.time;
            if (bossDownSignal || stage != latchStage || Time.time - looseSince >= LooseRelease)
            {
                bossLatched = false;
                bossDownSignal = false;
                looseSince = -1f;
                return false;
            }
            return true;
        }
    }

    bool BossFightNow
    {
        get
        {
            if (spawner != null && spawner.bossSpawned && !spawner.bossCleared) return true;
            if (bosss.LiveCount > 0) return true;
            return GameMode.IsEndless && EndlessMode.Instance != null && EndlessMode.Instance.FinalBossIncoming;
        }
    }

    static readonly string[] EndlessTracks = { "bgm_desert", "bgm_cave", "bgm_hell", "bgm_meadow", "bgm_abyss" };

    void Remember(AudioSource s)
    {
        if (s.clip == null || !s.isPlaying) return;
        resumeAt[s.clip] = s.timeSamples;
        leftAt[s.clip] = Time.unscaledTime;
    }

    // 1.1.2~ 최종 보스 등장 연출: 흐르던 곡을 0.35초에 끊고, 바뀐 순간부터 1초 뒤 무음 속에서 새 곡이 첫 타격(곡 머리)과 함께 바로 최대 음량으로 시작
    // (EndlessMode 가 배너 1초 전에 FinalBossMusic 을 켜므로 타격이 「최종 보스 「개발자」가 강림한다!」 배너와 맞물림)
    const float StrikeFade = 0.35f, StrikeSilence = 1.0f;
    float strikeAt = -1f;           // 무음 뒤 새 곡을 시작할 시각 (unscaled) · 음수면 없음
    float fadeOut = FadeTime;       // 이전 곡이 사라지는 데 걸리는 시간 (보통 FadeTime)

    static bool IsFinalBossTrack(string name) => name == "bgm_developer" || name == "bgm_developer_mom";

    void Update()
    {
        string want = Wanted();
        if (want != playing)
        {
            AudioClip clip = Resources.Load<AudioClip>("Music/" + want);
            if (clip != null)
            {
                bool strike = IsFinalBossTrack(want) && !IsFinalBossTrack(playing);
                // 두 소스를 바꿔 가며 교차 페이드
                AudioSource t = previous; previous = current; current = t;
                // 1.1.2~ 페이드 도중 또 바뀌면 (A→B→C) 새 곡에 쓸 소스로 둘 중 더 작은 쪽을 고름 — 아직 크게 들리던 곡이 딸깍 끊기지 않게
                if (current.clip != clip && current.isPlaying && previous.clip != clip && current.volume > previous.volume)
                {
                    t = previous; previous = current; current = t;
                }
                Remember(previous);
                if (current.clip != clip) Remember(current);     // 곡을 갈아 끼우기 전에 위치를 적어 둠 (곧 돌아오면 이어서)
                if (strike)
                {
                    // 최종 보스 곡은 무음 뒤에 처음(첫 타격)부터 — 이어서 재생하지 않음
                    current.Stop();
                    current.clip = clip;
                    current.volume = 0f;
                    strikeAt = Time.unscaledTime + StrikeSilence;
                    fadeOut = StrikeFade;
                }
                else
                {
                    strikeAt = -1f;
                    fadeOut = FadeTime;
                    // 페이드아웃 중인 소스가 바로 그 곡이면 끊지 않고 그대로 되살림 (A→B→A 로 빠르게 바뀔 때)
                    if (current.clip != clip || !current.isPlaying)
                    {
                        current.clip = clip;
                        current.volume = 0f;
                        current.Play();
                        if (resumeAt.TryGetValue(clip, out int pos) && leftAt.TryGetValue(clip, out float left)
                            && Time.unscaledTime - left < 60f && clip.samples > 0)
                            current.timeSamples = pos % clip.samples;
                    }
                }
                currentGain = TrackGain(want);
            }
            playing = want;
        }

        float step = Time.unscaledDeltaTime / FadeTime * MusicGain * Mathf.Max(1f, currentGain);
        // 전투 중 일시정지 · 레벨업 · 상점 창이 열려 멈춘 동안에는 조금 작게 (1.1~: 보스 결계 컷신은 연출이라 제외)
        float duck = sceneName == "GameScene" && Time.timeScale == 0f && !StoryDirector.Playing && !BossUltimate.CutscenePlaying ? 0.55f : 1f;
        float target = MusicGain * currentGain * GameSettings.MusicVolume * duck;
        if (strikeAt >= 0f)
        {
            // 무음 대기 → 때가 되면 페이드 없이 바로 시작
            if (Time.unscaledTime >= strikeAt)
            {
                strikeAt = -1f;
                current.Play();
                current.volume = target;
            }
            else current.volume = 0f;
        }
        else
        {
            // 설정에서 줄이면 페이드를 기다리지 않고 바로 따라감
            if (current.volume > target && duck == 1f) current.volume = target;
            current.volume = Mathf.MoveTowards(current.volume, target, step);
        }
        previous.volume = Mathf.MoveTowards(previous.volume, 0f, step * FadeTime / fadeOut);
        if (previous.volume <= 0f && previous.isPlaying) previous.Stop();
    }
}
