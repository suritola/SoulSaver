using UnityEngine;
using UnityEngine.SceneManagement;

// 배경음악: 메뉴 / 1장 / 2장 / 3장 / 4장 / 보스전 곡을 상황에 맞게 부드럽게 바꿔 틀어줌
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
        SceneManager.sceneLoaded += (s, m) => spawner = null;
    }

    AudioSource NewSource()
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.loop = true;
        s.playOnAwake = false;
        s.volume = 0f;
        s.spatialBlend = 0f;
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
        if (BossUltimate.Active) return "bgm_domain";          // 보스 필살기 결계
        // 1.1~: 무한 모드는 같은 짧은 곡만 수십 분 반복되지 않게 시간에 따라 돌려 씀 (보스전은 보스 곡 4바퀴마다 4장 곡과 번갈아)
        float played = Time.timeSinceLevelLoad;
        if (spawner != null && spawner.bossSpawned && !spawner.bossCleared)
            return GameMode.IsEndless && ((int)(played / 102.4f)) % 2 == 1 ? "bgm_abyss" : "bgm_boss";
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 0;
        if (GameMode.IsEndless) return EndlessTracks[((int)(played / 128f)) % EndlessTracks.Length];
        return stage == 0 ? "bgm_cave" : stage == 1 ? "bgm_hell" : stage == 2 ? "bgm_meadow" : "bgm_abyss";
    }

    static readonly string[] EndlessTracks = { "bgm_desert", "bgm_cave", "bgm_hell", "bgm_meadow", "bgm_abyss" };

    void Update()
    {
        string want = Wanted();
        if (want != playing)
        {
            AudioClip clip = Resources.Load<AudioClip>("Music/" + want);
            if (clip != null)
            {
                // 두 소스를 바꿔 가며 교차 페이드
                AudioSource t = previous; previous = current; current = t;
                if (previous.clip != null && previous.isPlaying)
                {
                    resumeAt[previous.clip] = previous.timeSamples;
                    leftAt[previous.clip] = Time.unscaledTime;
                }
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
                currentGain = TrackGain(want);
            }
            playing = want;
        }

        float step = Time.unscaledDeltaTime / FadeTime * MusicGain * Mathf.Max(1f, currentGain);
        // 전투 중 일시정지 · 레벨업 · 상점 창이 열려 멈춘 동안에는 조금 작게 (1.1~: 보스 결계 컷신은 연출이라 제외)
        float duck = sceneName == "GameScene" && Time.timeScale == 0f && !StoryDirector.Playing && !BossUltimate.CutscenePlaying ? 0.55f : 1f;
        float target = MusicGain * currentGain * GameSettings.MusicVolume * duck;
        // 설정에서 줄이면 페이드를 기다리지 않고 바로 따라감
        if (current.volume > target && duck == 1f) current.volume = target;
        current.volume = Mathf.MoveTowards(current.volume, target, step);
        previous.volume = Mathf.MoveTowards(previous.volume, 0f, step);
        if (previous.volume <= 0f && previous.isPlaying) previous.Stop();
    }
}
