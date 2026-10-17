using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 실제 게임 플레이 트레일러 + 스팀 스크린샷 (1.8.6~, tools/trailer/record.ps1 로 프로젝트 복사본에서 배치 실행)
// 게임 씬을 자동 조종으로 플레이하며 매 프레임 카메라를 MP4로 인코딩하고, 중요한 순간은 PNG 스크린샷으로 저장
// 흐름 (차별점 구현 3-1 · 45초): 초원 거너 「자동 조준 없음」 커서 조준 · 8발 · 저절로 재장전 → 유령 · 시계공 · 궁수 1초 컷
//       → 킹 슬라임 결계 · 처치 · 무기 진화 → 리치 왕 빈틈 → 지옥의 군주 기억 → 거울의 군주 → 네 세계 → 개발자
//       → 스킬 진화 → 영혼 트리 · 결계 깨짐 → 로고 · 「무료 체험판」
// 빈틈(WeakSpot) · 기억(BossMemory) · 궁수 수동 시위(CharacterKit.ManualBowInAuto)는 리플렉션으로만 봄 — 없으면 그 컷을 건너뜀 (게임 코드와 컴파일 의존 없음)
// 화면에는 게임 커서(Resources/Cursors)를 겹쳐 그리고, 총소리 · 장전 소리는 음악과 함께 영상 소리에 섞음
// 2026-10-18~ 트레일러는 합성 곡 TrailerMusic + 게임 효과음(처치 · 보스 타격 · 영웅 평타 · 필살기 · 결계 · 진화 · 장면 전환 휘익)을 같은 순간에 얹음 (Rec.rich) · 컷마다 [CUE] 로그
public class GameplayCapture
{
    static readonly string Out = Environment.GetEnvironmentVariable("CAPTURE_OUT") ?? "C:/Temp/SoulSaverCapture";
    static readonly int Fps = int.TryParse(Environment.GetEnvironmentVariable("CAPTURE_FPS"), out int f) ? f : 30;
    static readonly string Quality = Environment.GetEnvironmentVariable("CAPTURE_QUALITY") ?? "Medium";
    static readonly string Lang = Environment.GetEnvironmentVariable("CAPTURE_LANG") ?? "en";   // ko · en · ja · zh
    // 2026-10-18~ 상점 트레일러는 자막 없이 (사용자 지시). 자막을 넣고 싶을 때만 CAPTURE_CAPTIONS=1 (record.ps1 -Captions)
    static readonly bool NoCaptions = Environment.GetEnvironmentVariable("CAPTURE_CAPTIONS") != "1";

    // 트레일러 자막 (언어별) — 0~9 는 옛 콘티(하이라이트 · 다시 쓰는 장면 함수가 읽음)라 그대로 두고, 새 콘티 줄은 10번부터 뒤에 덧붙임
    static readonly Dictionary<string, string[]> Captions = new Dictionary<string, string[]>
    {
        ["en"] = new[] { "THE DEAD HAVE RISEN.", "BUILD YOUR RUN.", "DEFEAT A BOSS.  EVOLVE YOUR WEAPON.", "GROW A SOUL TREE OF 100+ NODES.", "FIVE HEROES.  FOUR WORLDS.  FOUR KINGS.", "20 CHARACTER SKINS.  10 WEAPON SKINS.", "WISHLIST NOW ON STEAM",
                         "MAX OUT SKILLS.  FUSE 37 EVOLUTIONS.", "SURVIVE THE BOSS'S BARRIER.", "THE FINAL ABYSS.  A MIRROR THAT COPIES YOUR EVERY SKILL.",
                         "NO AUTO-AIM.", "AIM.  RELOAD.", "9 HEROES.  9 HANDS.", "BEAT THE BOSS.  EVOLVE ON THE SPOT.", "HIT THE WEAK POINT.", "THE BOSS REMEMBERS YOUR MOVES.",
                         "A KING WHO MIRRORS YOUR BUILD.", "4 WORLDS.  4 KINGS.", "...AND THE DEVELOPER.", "37 SKILL EVOLUTIONS.", "100+ NODE SOUL TREE.", "AIM.  RELOAD.  EVOLVE.", "FREE DEMO ON STEAM" },
        ["ko"] = new[] { "망자들이 깨어났다.", "나만의 빌드를 만들어라.", "보스를 쓰러뜨리고, 무기를 진화시켜라.", "100칸이 넘는 영혼 트리.", "다섯 영웅.  네 개의 세계.  네 명의 왕.", "캐릭터 스킨 20종 · 무기 스킨 10종", "지금 스팀에서 찜하세요",
                         "스킬을 모아 합쳐라.  37가지 스킬 진화.", "보스의 결계를 버텨라.", "마지막 심연.  너의 모든 기술을 흉내 내는 거울.",
                         "자동 조준은 없다.", "조준.  장전.", "영웅 9명.  9개의 손.", "쓰러뜨리면, 그 자리에서 진화.", "빈틈을 노려라.", "보스는 너의 공격을 기억한다.",
                         "너의 빌드를 비추는 거울의 군주.", "네 세계.  네 왕.", "…그리고 개발자.", "스킬 진화 37가지.", "100칸이 넘는 영혼 트리.", "조준 · 장전 · 진화", "스팀에서 무료 체험판" },
        ["ja"] = new[] { "死者が目覚めた。", "自分だけのビルドを。", "ボスを倒し、武器を進化させろ。", "100マスを超える魂のツリー。", "五人の英雄。四つの世界。四人の王。", "キャラクタースキン20種・武器スキン10種", "Steamでウィッシュリストに追加",
                         "スキルを極め、合わせろ。37のスキル進化。", "ボスの結界を耐え抜け。", "最後の深淵。お前の技をすべて真似る鏡。",
                         "オートエイムはない。", "狙え。装填しろ。", "9人の英雄。9つの手触り。", "倒せば、その場で進化。", "隙を撃ち抜け。", "ボスはお前の攻撃を覚える。",
                         "お前のビルドを映す鏡の君主。", "4つの世界。4人の王。", "……そして開発者。", "37のスキル進化。", "100マスを超える魂のツリー。", "照準・装填・進化", "Steamで無料体験版" },
        ["zh"] = new[] { "亡者已苏醒。", "打造你的流派。", "击败首领，进化武器。", "超过100个节点的灵魂树。", "五位英雄。四个世界。四位王者。", "20款角色皮肤 · 10款武器皮肤", "立即在Steam上添加愿望单",
                         "练满技能，融合进化。37种技能进化。", "撑过首领的结界。", "最后的深渊。模仿你每一招的镜子。",
                         "没有自动瞄准。", "瞄准。换弹。", "9位英雄。9种手感。", "击败首领，当场进化。", "击中破绽。", "首领会记住你的招式。",
                         "映照你构筑的镜之君主。", "4个世界。4位王者。", "……还有开发者。", "37种技能进化。", "超过100格的灵魂树。", "瞄准 · 换弹 · 进化", "Steam免费试玩版" },
    };
    // 새 콘티 자막 번호 (순서 실수를 막으려고 이름을 붙임)
    const int C_SkillFuse = 7, C_Barrier = 8, C_Mirror = 9;
    const int C_NoAutoAim = 10, C_AimReload = 11, C_Heroes9 = 12, C_EvolveOnSpot = 13, C_WeakPoint = 14, C_Remembers = 15,
              C_MirrorsBuild = 16, C_Worlds4 = 17, C_Developer = 18, C_SkillEvo37 = 19, C_SoulTree100 = 20, C_EndTag = 21, C_FreeDemo = 22;
    static string C(int i) => (Captions.TryGetValue(Lang, out string[] c) ? c : Captions["en"])[i];

    // 게임 언어 바꾸기 (GameSettings.Language: 0 한국어 · 1 English · 2 日本語 · 3 中文)
    static void SetGameLanguage(string lang)
    {
        int i = lang == "ko" ? 0 : lang == "ja" ? 2 : lang == "zh" ? 3 : 1;
        Type l = T("Loc+Lang");
        T("GameSettings").GetProperty("Language").SetValue(null, Enum.ToObject(l, i));
    }
    const int W = 1920, H = 1080;
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);

    static Type T(string n) => Type.GetType(n + ", Assembly-CSharp");
    static object Get(object o, string f) => o.GetType().GetField(f, Any)?.GetValue(o);
    static void Set(object o, string f, object v) => o.GetType().GetField(f, Any).SetValue(o, v);
    static object Call(object o, string m, params object[] a) => o.GetType().GetMethod(m, Any).Invoke(o, a);
    static void In(string f, object v) => T("GameInput").GetField(f).SetValue(null, v);
    static UnityEngine.Object Find(string type) => UnityEngine.Object.FindFirstObjectByType(T(type));

    // 정적 칸 · 속성 읽기 / 쓰기 (다른 갈래가 아직 안 넣은 이름이면 null · false)
    static object StaticGet(string type, string name)
    {
        Type t = T(type);
        if (t == null) return null;
        FieldInfo f = t.GetField(name, Any);
        if (f != null) return f.GetValue(null);
        PropertyInfo p = t.GetProperty(name, Any);
        return p != null ? p.GetValue(null) : null;
    }
    static bool SetStatic(string type, string name, object v)
    {
        Type t = T(type);
        if (t == null) return false;
        FieldInfo f = t.GetField(name, Any);
        if (f != null) { f.SetValue(null, v); return true; }
        PropertyInfo p = t.GetProperty(name, Any);
        if (p == null || !p.CanWrite) return false;
        p.SetValue(null, v);
        return true;
    }

    // 게임 합성음 (SpecialFeedback 의 private 사전 clips · SpecialAbilities.SharedFx)
    static AudioClip FxClip(string name)
    {
        object fx = StaticGet("SpecialAbilities", "SharedFx");
        return fx != null && Get(fx, "clips") is IDictionary d && d.Contains(name) ? d[name] as AudioClip : null;
    }

    // 상점 트레일러(rich)에서만 게임 합성음을 얹음 (하이라이트 · 레딧 영상은 그대로)
    static void Sx(string name, float vol, float pitch = 1f)
    {
        if (rec != null && rec.rich) rec.SfxVar(FxClip(name), vol, pitch);
    }
    static void SxRes(string path, float vol)
    {
        if (rec != null && rec.rich) rec.Sfx(Resources.Load<AudioClip>(path), vol);
    }

    // 지금 영웅 이름 (CharacterData.Selected · Load 가 Override 로 정함)
    static string Hero() => StaticGet("CharacterData", "Selected")?.ToString() ?? "Gunner";

    // 컷 시각 기록 (배경음 타이밍 검증용 · 녹화 프레임) — swish 면 장면이 바뀌는 「휘익」도
    static void Cue(string name, bool swish = false)
    {
        if (rec == null) return;
        Debug.Log("[CUE] " + name + " " + rec.frames);
        if (swish && rec.rich) rec.Sfx(Swish(), 0.4f);
    }

    // 장면 전환 「휘익」: 게임에 맞는 소리가 없어 직접 합성 (거친 소음이 빠르게 커졌다가 차단 주파수가 내려가며 사라짐 + 낮은 쿵)
    static AudioClip swishClip;
    static AudioClip Swish()
    {
        if (swishClip != null) return swishClip;
        const int rate = 48000;
        const float dur = 0.45f;
        int n = Mathf.CeilToInt(dur * rate);
        float[] d = new float[n];
        var r = new System.Random(4511);
        float lp = 0f, lp2 = 0f, peak = 0.0001f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate, k = t / dur;
            float env = k < 0.22f ? Mathf.Pow(k / 0.22f, 2f) : Mathf.Pow(1f - (k - 0.22f) / 0.78f, 2.2f);
            float cut = Mathf.Lerp(0.45f, 0.03f, k);                       // 지나가며 어두워짐
            float noise = (float)(r.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * cut;
            lp2 += (lp - lp2) * cut;
            float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 45f, k) * t) * 0.35f * Mathf.Clamp01((k - 0.15f) * 4f);
            d[i] = (lp2 * 2.2f + sub) * env;
            peak = Mathf.Max(peak, Mathf.Abs(d[i]));
        }
        for (int i = 0; i < n; i++) d[i] *= 0.85f / peak;
        swishClip = AudioClip.Create("trailer_swish", n, 1, rate, false);
        swishClip.SetData(d, 0);
        return swishClip;
    }

    // ================================================================= 녹화기 (씬이 바뀌어도 남음)
    class Rec : MonoBehaviour
    {
        public bool recording, autopilot, levelUpAllowed;
        public float musicVolume = 0.9f;
        public string musicPath = "Music/bgm_boss";
        public int frames;
        // 하이라이트 영상 연출 (2.2.1~): 펀치 줌 · 화면 흔들림 · 흰 번쩍임 (찍을 때만 카메라에 얹고 바로 되돌림)
        public float baseZoom = 1f, punch, shake, flashAmt;
        // 자동 조종이 적과 두는 거리 (잡몹 · 보스, 가까우면 물러나고 멀면 다가감)
        //   humanMove 가 거짓(하이라이트 · 레딧)이면 예전 그대로 이 값만 씀 · 참(상점 트레일러)이면 근접(검사)만 이 값을 씀
        public float nearMob = 3.5f, farMob = 7f, nearBoss = 6.5f, farBoss = 9f;
        // 2026-10-18~ 상점 트레일러: 사람이 하는 플레이처럼 (원거리 영웅은 거리를 두고 카이팅 · 부드러운 방향 전환 · 멈춰 조준 · 짧은 옆걸음)
        public bool humanMove;
        public float rangedMobNear = 6f, rangedMobFar = 9f, rangedBossNear = 8f, rangedBossFar = 11f;
        public Image flash;
        public Canvas captionCanvas;
        public TextMeshProUGUI caption, logo, sub;
        public Image black;

        // ---------------- 사람 같은 조준 (차별점 구현 3-2): 표적이 바뀌면 커서를 aimSpeed 초 동안 smoothstep 으로 옮김
        public bool manualFire;                 // 참이면 자동 누르기를 끄고 장면 함수가 직접 쏨
        public Transform aimOverride;           // 이 표적을 겨눔 (없으면 가까운 적)
        public bool aimFixed;                   // 참이면 aimPoint 를 겨눔 (빈틈 중심 등)
        public Vector3 aimPoint;
        public float aimSpeed = 0.25f;
        public Vector3 aimNow;
        Vector3 aimFrom, aimTo;
        float aimT = 1f;
        bool aimInit;

        // ---------------- 커서 · 키 그림 (게임이 보여 주는 커서를 영상에도 그림)
        public bool showCursor;
        public RawImage cursorImage;
        public GameObject keyCap;               // 「R」 키 그림 (빈 탄창 → 저절로 재장전할 때 0.6초)
        public bool keyHint;
        int keyCapFrames;

        // ---------------- 효과음 섞기: 총소리 · 장전 소리 (gunSfx 가 참이면 탄 수 · 장전 상태가 바뀔 때 저절로)
        public bool gunSfx;
        // 2026-10-18~ 상점 트레일러: 게임이 실제로 내는 소리를 같은 순간에 얹음 (rich 가 참일 때만 · 처치 · 보스 피격 · 평타 · 진화 연출 등은 상태 변화로 저절로)
        public bool rich;
        public bool weakSfx;                    // 빈틈 컷: 보스 피격 소리를 「빈틈」 타격음으로
        public int quietBossDownUntil;          // 결계가 깨지는 프레임: domain_break 와 곡 충격음이 겹치므로 쓰러질 때 폭발 · 울음은 건너뜀
        const int Voices = 16;                  // 8 → 16 (처치 · 타격 · 평타가 겹쳐도 큰 소리가 잘리지 않게)
        readonly float[][] vData = new float[Voices][];
        readonly double[] vPos = new double[Voices];
        readonly float[] vStep = new float[Voices], vVol = new float[Voices];
        readonly Dictionary<AudioClip, float[]> monoCache = new Dictionary<AudioClip, float[]>();
        readonly System.Random sfxRnd = new System.Random(1018);           // 음높이 흔들기 (매번 같은 영상이 나오게 고정 씨앗)
        readonly Dictionary<string, int> gapAt = new Dictionary<string, int>();
        readonly List<(int at, AudioClip clip, float vol, float pitch)> later = new List<(int, AudioClip, float, float)>();
        int lastBullets = -1;
        bool lastReloading;

        public void Sfx(AudioClip clip, float vol, float pitch = 1f)
        {
            float[] d = Mono(clip);
            if (d == null || d.Length == 0) return;
            int pick = 0;
            for (int i = 0; i < Voices; i++)
            {
                if (vData[i] == null) { pick = i; break; }
                if (vPos[i] / vData[i].Length > vPos[pick] / vData[pick].Length) pick = i;     // 다 차 있으면 가장 많이 울린 소리를 끊음
            }
            vData[pick] = d;
            vPos[pick] = 0;
            vStep[pick] = clip.frequency / (float)Rate * Mathf.Clamp(pitch, 0.25f, 4f);
            vVol[pick] = vol;
        }

        // 게임(SpecialFeedback.Play)처럼 음높이를 ±2.5% 간격 5단계로 살짝 흔듦 — 같은 소리가 반복돼도 단조롭지 않게
        public void SfxVar(AudioClip clip, float vol, float pitch = 1f) => Sfx(clip, vol, pitch * (1f + sfxRnd.Next(-2, 3) * 0.025f));
        public float Rand(float a, float b) => a + (float)sfxRnd.NextDouble() * (b - a);

        // 같은 갈래 소리는 seconds(녹화 시간) 안에 한 번만 — 기관총처럼 겹치지 않게
        public bool Gap(string tag, float seconds)
        {
            if (gapAt.TryGetValue(tag, out int at) && frames - at < Mathf.RoundToInt(seconds * Fps)) return false;
            gapAt[tag] = frames;
            return true;
        }

        // seconds(녹화 시간) 뒤에 울림
        public void Later(float seconds, AudioClip clip, float vol, float pitch = 1f)
        {
            if (clip != null) later.Add((frames + Mathf.Max(0, Mathf.RoundToInt(seconds * Fps)), clip, vol, pitch));
        }

        // 끝 카드 직전: 곡이 일부러 비워 둔 정적에 남은 효과음 꼬리 · 예약 소리를 모두 끊음
        public void Hush()
        {
            for (int i = 0; i < Voices; i++) vData[i] = null;
            later.Clear();
        }

        void RunLater()
        {
            for (int i = later.Count - 1; i >= 0; i--)
                if (later[i].at <= frames) { Sfx(later[i].clip, later[i].vol, later[i].pitch); later.RemoveAt(i); }
        }

        // 효과음을 한 갈래(모노)로 한 번만 꺼내 둠 (압축 · 스트리밍이라 못 꺼내면 빈 배열 → 그 소리만 빠짐)
        // Sounds/domain_* 은 preloadAudioData 가 꺼져 있어 먼저 LoadAudioData — 아직 불러오는 중이면 다음에 다시 시도
        float[] Mono(AudioClip clip)
        {
            if (clip == null) return null;
            if (monoCache.TryGetValue(clip, out float[] m)) return m;
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            m = new float[0];
            try
            {
                float[] raw = new float[clip.samples * clip.channels];
                if (clip.GetData(raw, 0))
                {
                    m = new float[clip.samples];
                    for (int i = 0; i < clip.samples; i++)
                    {
                        float s = 0f;
                        for (int c = 0; c < clip.channels; c++) s += raw[i * clip.channels + c];
                        m[i] = s / clip.channels;
                    }
                }
            }
            catch (Exception) { }
            if (m.Length > 0 || clip.loadState != AudioDataLoadState.Loading) monoCache[clip] = m;
            return m;
        }

        float MixSfx()
        {
            float s = 0f;
            for (int i = 0; i < Voices; i++)
            {
                float[] d = vData[i];
                if (d == null) continue;
                int at = (int)vPos[i];
                if (at >= d.Length) { vData[i] = null; continue; }
                s += d[at] * vVol[i];
                vPos[i] += vStep[i];
            }
            return s;
        }

        MediaEncoder encoder;
        NativeArray<float> audio;
        RenderTexture rt;
        Texture2D frame;
        float[] music;
        int musicCh, musicRate, musicFrames, cursor, perFrame;
        const int Rate = 48000;
        float nextTap;
        bool held;

        public void Begin(string path)
        {
            rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            frame = new Texture2D(W, H, TextureFormat.RGBA32, false);
            AudioClip clip = Resources.Load<AudioClip>(musicPath);
            if (clip == null) { Debug.Log("[CAP] 음악 " + musicPath + " 없음 — Music/bgm_boss 로"); clip = Resources.Load<AudioClip>("Music/bgm_boss"); }
            if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();
            // 결계 소리는 미리 불러 둠 (preloadAudioData 꺼짐 · 처음 쓰는 순간 빈 소리가 되지 않게)
            foreach (string s in new[] { "Sounds/domain_open", "Sounds/domain_break", "Sounds/domain_shout" })
                Resources.Load<AudioClip>(s)?.LoadAudioData();
            music = new float[clip.samples * clip.channels];
            clip.GetData(music, 0);
            musicCh = clip.channels; musicRate = clip.frequency; musicFrames = clip.samples;
            perFrame = Rate / Fps;
            var mode = (UnityEditor.VideoBitrateMode)Enum.Parse(typeof(UnityEditor.VideoBitrateMode), Quality);
            var v = new VideoTrackAttributes { frameRate = new MediaRational(Fps), width = W, height = H, includeAlpha = false, bitRateMode = mode };
            var a = new AudioTrackAttributes { sampleRate = new MediaRational(Rate), channelCount = (ushort)musicCh, language = "en" };
            encoder = new MediaEncoder(path, v, a);
            audio = new NativeArray<float>(perFrame * musicCh, Allocator.Persistent);
        }

        public void End()
        {
            recording = false;
            encoder?.Dispose();
            encoder = null;
            if (audio.IsCreated) audio.Dispose();
        }

        // 매 프레임 게임 처리가 끝난 뒤 카메라를 그려 영상에 붙임 (UI 캔버스도 카메라로 옮겨서 함께 찍힘)
        // 배치 모드에서는 WaitForEndOfFrame 이 오지 않을 수 있어 LateUpdate 에서 찍음
        void Capture()
        {
            {
                if (!recording || encoder == null) return;
                Texture2D t = Grab(true);
                encoder.AddFrame(t);
                for (int i = 0; i < perFrame; i++)
                {
                    double pos = (cursor + i) * (double)musicRate / Rate;
                    int i0 = (int)pos % musicFrames, i1 = (i0 + 1) % musicFrames;
                    float k = (float)(pos - Math.Floor(pos));
                    float fx = MixSfx();
                    // 리미터 (2026-10-18~): 음악 + 효과음이 겹쳐 1 에 닿던 찌그러짐을 막음 — 바로 줄이고 0.12초에 걸쳐 돌아옴, 천장 0.89(-1dB · AAC 넘침 여유)
                    float peak = 0f;
                    for (int c = 0; c < musicCh && c < 8; c++)
                    {
                        mixTmp[c] = Mathf.Lerp(music[i0 * musicCh + c], music[i1 * musicCh + c], k) * musicVolume + fx;
                        peak = Mathf.Max(peak, Mathf.Abs(mixTmp[c]));
                    }
                    if (peak * limGain > LimCeil) limGain = LimCeil / peak;
                    else limGain += (1f - limGain) * LimRelease;
                    for (int c = 0; c < musicCh; c++)
                        audio[i * musicCh + c] = SoftClip(mixTmp[Mathf.Min(c, 7)] * limGain);
                }
                cursor += perFrame;
                encoder.AddSamples(audio);
                frames++;
            }
        }

        // 큰 소리가 겹친 꼭대기만 부드럽게 눌러 「지직」 깨지지 않게 (0.8 까지는 그대로)
        readonly float[] mixTmp = new float[8];
        float limGain = 1f;
        const float LimCeil = 0.89f;
        static readonly float LimRelease = 1f - Mathf.Exp(-1f / (0.12f * Rate));

        static float SoftClip(float x)
        {
            float a = Mathf.Abs(x);
            if (a <= 0.8f) return x;
            float y = 0.8f + 0.2f * (float)Math.Tanh((a - 0.8f) / 0.2f);
            return x < 0f ? -y : y;
        }

        public Texture2D Grab(bool withCaption)
        {
            Camera cam = Camera.main;
            if (cam == null) return frame;
            CleanUi();
            cam.cullingMask |= 1 << 5;
            CanvasesToCamera(cam);
            captionCanvas.worldCamera = cam;
            captionCanvas.enabled = withCaption;
            RenderTexture old = cam.targetTexture;
            float size = cam.orthographicSize;
            Vector3 pos = cam.transform.position;
            cam.orthographicSize = size * baseZoom * (1f - 0.2f * Mathf.Clamp01(punch));
            cam.transform.position = pos + (Vector3)(UnityEngine.Random.insideUnitCircle * shake * 0.6f);
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RestoreUi();
            cam.targetTexture = old;
            cam.orthographicSize = size;
            cam.transform.position = pos;
            captionCanvas.enabled = true;
            RenderTexture.active = rt;
            frame.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            frame.Apply();
            RenderTexture.active = null;
            return frame;
        }

        // ---------------- 촬영용 화면 정리 (2026-10-18~): 홍보 영상에 어울리지 않는 게임 UI · 화면 덮개를 찍는 순간에만 숨김 (게임 코드는 그대로 · 리플렉션)
        //   1) 거너 필살기 조준 · 개발자 조준 흉내(PlayerController.HostileTimeSlow)의 화면 색(skillEffectPanel, 알파 0.6 까지 · 조준하는 동안 계속)
        //      → 켜질 때 최대 TintMax 로 보였다가 TintFade 초 안에 사라지게 (개발자 장면에서 2.6초 넘게 화면이 하얗게 바래던 원인)
        //   2) 장 제목 배너(StageManager 시작 → Notice.Big 「1장 · 지하 묘역」 — 촬영은 장을 나중에 바꿔 초원에서도 묘역으로 나옴) · 토스트 · 도움말 → 알림 묶음(Notice.root)을 투명하게
        //   3) 조작 안내: 결계 컷신 「클릭 · Space: 건너뛰기」 · 스킬 진화 「클릭하거나 [Space] 를 눌러 계속」 · 레벨업 배지 「레벨업! [Space] 카드 고르기」
        //      · 머리 위 「레벨업! [Space]」(GrowthPrompts) · 「레벨업!」 띠(LevelShop.LvUpPanel) · 카드 창 확정 안내 · 「이번 레벨업 포기」 버튼 · 영혼 트리 아래 조작 도움말
        //   4) 피격 붉은 테두리(DamageFlash) — 촬영 중엔 체력을 늘 채우므로 맞은 티만 남음
        public bool cleanUi = true;
        const float TintMax = 0.35f, TintFade = 0.2f;
        int tintFrames, cleanFrame = -1;
        float tintCap = TintMax;
        string[] hideExact;
        string hidePrefix;

        static string LocT(string ko)
        {
            MethodInfo m = T("Loc")?.GetMethod("T", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            return m != null ? (string)m.Invoke(null, new object[] { ko }) : ko;
        }

        // 찍는 동안(cam.Render 한 번)만 끄고 찍은 뒤 그대로 되돌림 → 게임 상태 · 다음 프레임 동작은 촬영하지 않을 때와 같음
        //   (예전: 끈 채로 두어 GrowthPrompts 가 영영 꺼지고, Herald 가 레벨업 배지 켜짐을 읽는 등 게임 쪽 판단이 달라질 수 있었음)
        readonly List<GameObject> hiddenGo = new List<GameObject>();
        readonly List<Behaviour> hiddenBh = new List<Behaviour>();
        Image tintPanel;
        float tintAlpha = -1f;
        CanvasGroup noticeGroup;
        float noticeAlpha;

        void Hide(object o)
        {
            GameObject g = o as GameObject ?? (o as Component)?.gameObject;
            if (g != null && g.activeSelf) { g.SetActive(false); hiddenGo.Add(g); }
        }

        void Off(Behaviour b)
        {
            if (b != null && b.enabled) { b.enabled = false; hiddenBh.Add(b); }
        }

        void RestoreUi()
        {
            for (int i = hiddenGo.Count - 1; i >= 0; i--) if (hiddenGo[i] != null) hiddenGo[i].SetActive(true);
            foreach (Behaviour b in hiddenBh) if (b != null) b.enabled = true;
            hiddenGo.Clear();
            hiddenBh.Clear();
            if (tintPanel != null && tintAlpha >= 0f) { Color c = tintPanel.color; c.a = tintAlpha; tintPanel.color = c; }
            tintPanel = null;
            tintAlpha = -1f;
            if (noticeGroup != null) noticeGroup.alpha = noticeAlpha;
            noticeGroup = null;
        }

        void CleanUi()
        {
            if (!cleanUi) return;
            bool tick = cleanFrame != Time.frameCount;      // 한 프레임에 여러 번 찍어도(스크린샷) 시간은 한 번만 셈
            cleanFrame = Time.frameCount;

            // 1) 필살기 조준 화면 색 — 조준이 시작되면 TintMax 에서 TintFade 초 안에 0 으로,
            //    조준이 끝나도 게임 쪽 색(실제 값은 그대로 0.6 에서 내려감)이 다 빠질 때까지 계속 0
            object p = Find("PlayerController");
            if (p != null && Get(p, "skillEffectPanel") is Image panel && panel != null)
            {
                bool aiming = Get(p, "screenFadeTarget") is float target && target > 0.001f;
                if (tick)
                {
                    if (aiming) tintFrames++;
                    else if (panel.color.a <= 0.001f) tintFrames = 0;
                    tintCap = TintMax * Mathf.Clamp01(1f - tintFrames / (TintFade * Fps));
                }
                Color c = panel.color;
                if (tintFrames > 0 && c.a > tintCap) { tintPanel = panel; tintAlpha = c.a; c.a = tintCap; panel.color = c; }
            }

            // 4) 피격 붉은 테두리
            if (StaticGet("DamageFlash", "instance") is Component df && df != null) Off(Get(df, "image") as Image);

            // 2) 알림 묶음 (장 제목 · 토스트 · 도움말 · 목표 줄) — 찍기 직전마다 다시 찾으므로 장면 로드 뒤 늦게 만들어진 배너도 그 프레임부터 숨음
            if (StaticGet("Notice", "root") is RectTransform nr && nr != null && nr.GetComponent<CanvasGroup>() is CanvasGroup ncg && ncg != null)
            {
                noticeGroup = ncg;
                noticeAlpha = ncg.alpha;
                ncg.alpha = 0f;
            }

            // 3) 조작 안내
            object shop = Find("LevelShop");
            if (shop != null)
            {
                Hide(Get(shop, "pendingBadge"));
                Hide(Get(shop, "LvUpPanel"));
                Hide(Get(shop, "selectHint"));
                Hide(Get(shop, "skipButton"));
            }
            if (FindExists("GrowthPrompts")) Hide(Get(Find("GrowthPrompts"), "tag"));
            object tree = T("SoulTreeUI")?.GetProperty("Instance")?.GetValue(null);
            if (tree != null) Hide(Get(tree, "footText"));
            if (hideExact == null)
            {
                hideExact = new[] { LocT("클릭 · Space: 건너뛰기"), LocT("[A]: 건너뛰기"), LocT("herald.skip"), LocT("herald.skip.pad") };
                hidePrefix = LocT("클릭하거나 [");
            }
            if (FindExists("BossUltimate") || FindExists("SkillEvolutionUI") || StaticGet("Herald", "Playing") is bool hp && hp)
                foreach (TMP_Text t in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                {
                    if (!t.enabled) continue;
                    string s = t.text;
                    if (string.IsNullOrEmpty(s) || s.Length > 80) continue;
                    if (Array.IndexOf(hideExact, s) >= 0 || (hidePrefix.Length > 0 && s.StartsWith(hidePrefix))) Off(t);
                }
        }

        static bool FindExists(string type) => T(type) != null && Find(type) != null;

        // ---------------- 자동 조종: 가까운 적(보스 먼저)을 겨누고 옆으로 돌며 싸움
        void Update()
        {
            if (!autopilot) return;
            Component player = (Component)Find("PlayerController");
            if (player == null) return;
            Vector3 me = player.transform.position;
            Vector3? target = Nearest(me, out bool boss);
            Vector3 desired;
            if (humanMove) desired = HumanMove(player, me, target, boss);
            else if (target.HasValue)
            {
                Vector2 to = target.Value - me;
                float d = to.magnitude;
                Vector2 side = new Vector2(-to.y, to.x).normalized;
                // 보스는 덩치가 커서 멀리서 싸움 (영웅이 보스 그림에 가려지지 않게)
                float near = boss ? nearBoss : nearMob, far = boss ? farBoss : farMob;
                desired = target.Value;
                In("AutoMove", side * 0.75f + (d < near ? -to.normalized * 1.2f : d > far ? to.normalized * 0.7f : Vector2.zero));
            }
            else
            {
                In("AutoMove", Vector2.zero);
                desired = me + Vector3.right * 5f;
            }
            if (aimFixed) desired = aimPoint;
            else if (aimOverride != null) desired = aimOverride.position;
            // 표적이 바뀌면(1.5칸 넘게 튐) 지금 커서 자리에서 새 표적까지 미끄러지듯 옮김 · 같은 표적이면 따라감
            if (!aimInit) { aimNow = aimTo = desired; aimInit = true; }
            if ((desired - aimTo).sqrMagnitude > 1.5f * 1.5f) { aimFrom = aimNow; aimT = 0f; }
            aimTo = desired;
            aimT = Mathf.Min(1f, aimT + 1f / (Fps * Mathf.Max(0.02f, aimSpeed)));
            aimNow = aimT >= 1f ? aimTo : Vector3.Lerp(aimFrom, aimTo, Mathf.SmoothStep(0f, 1f, aimT));
            In("AutoAim", aimNow);
            // 눌렀다 떼기를 반복 (총 · 베기 · 활 시위 · 플라스크 모두 쏘게)
            if (!manualFire && Time.time >= nextTap)
            {
                held = !held;
                if (held) { In("FireDownFrame", Time.frameCount + 1); In("AutoFire", true); nextTap = Time.time + 0.42f; }
                else { In("FireUpFrame", Time.frameCount + 1); In("AutoFire", false); nextTap = Time.time + 0.06f; }
            }
        }

        // 죽지 않고, 보여 주려는 때가 아니면 레벨업 창이 뜨지 않게
        void LateUpdate()
        {
            punch *= 0.86f;
            shake *= 0.85f;
            // 번쩍임 (2026-10-18~ 0.8 → 0.62 · 0.05 아래는 0): 화면이 선형 색 공간이라 알파 0.1~0.2 만 남아도 어두운 장면이 회색으로 뿌옇게 떠 보였음
            //   (0.8 이면 장면 전환 · 필살기 · 결계 깨짐마다 0.5초(15프레임) 넘게 HUD 까지 뿌연 막) → 이제 0.25초(7프레임) 안에 사라짐
            flashAmt *= 0.62f;
            if (flashAmt < 0.05f) flashAmt = 0f;
            if (flash != null) flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(flashAmt));
            object p = Find("PlayerController");
            if (p != null)
            {
                Set(p, "PlayerHealth", Get(p, "PlayerMaxHealth"));
                if (!levelUpAllowed) Set(p, "nowEXP", 0f);
            }
            // 1.1~ 판 시작 카드 · 쌓인 레벨업 이어 고르기 창이 녹화 중에 화면을 덮지 않게 바로 확정
            if (!levelUpAllowed && Find("LevelShop") is object shop && (bool)shop.GetType().GetProperty("IsOpen").GetValue(shop))
                ConfirmCard(shop, 0);
            GunSounds(p);
            GameSounds(p);
            RunLater();
            DrawCursor();
            Capture();
        }

        // ---------------- 인게임 소리 (rich): 게임이 그 순간 내는 소리를 상태 변화로 알아내 같은 클립으로 얹음
        //   볼륨은 게임 값(SpecialFeedback.volume 0.45 × 부른 값)에 맞추되 음악을 덮지 않게 0.25~0.7
        int lastAlive = -1;
        float lastShoot = -1f, lastKick;
        object lastPlayer;
        UnityEngine.Object bossObj;
        float bossHp;
        bool bossDead, bossMem, weakFirst, lastWevoTitle, lastSevoBurst;
        static PropertyInfo enemyDead;

        void GameSounds(object p)
        {
            if (!rich) return;
            // 진화 연출은 녹화와 상관없이 상태를 따라감 (녹화를 멈춘 동안 바뀌면 그 순간 소리는 빠짐)
            object wevo = Find("WeaponEvolutionUI");
            bool wTitle = wevo != null && Get(wevo, "title") is Component tc && tc != null && tc.gameObject.activeSelf;
            object sevo = Find("SkillEvolutionUI");
            bool sBurst = sevo != null && ((Component)sevo).transform.Find("EvoGlow") != null;
            if (!recording)
            {
                lastAlive = -1; lastShoot = -1f; lastPlayer = null; bossObj = null;
                lastWevoTitle = wTitle; lastSevoBurst = sBurst;
                return;
            }

            // 무기 진화 (WeaponEvolutionUI.Play): 제목이 켜지는 순간 무기가 부서짐 → 0.45초 뒤 카드 펼침
            if (wTitle && !lastWevoTitle)
            {
                Cue("weapon_evolve_burst");
                Sfx(FxClip("shatter"), 0.55f, 0.8f);
                Sfx(FxClip("bigboom"), 0.5f, 1.1f);
                Later(0.45f, FxClip("whoosh"), 0.4f, 0.9f);
            }
            lastWevoTitle = wTitle;
            // 스킬 진화 (SkillEvolutionUI.Play): 진화 아이콘이 생기는 순간 「번쩍!」
            if (sBurst && !lastSevoBurst)
            {
                Cue("skill_evolve_burst");
                Sfx(FxClip("shatter"), 0.55f, 1.1f);
                Sfx(FxClip("bigboom"), 0.45f, 1.3f);
                Sfx(FxClip("levelup"), 0.5f, 1f);
            }
            lastSevoBurst = sBurst;

            // 잡몹 처치 (Juice 처치 「퐁」 · 한꺼번에 많이 죽으면 한 번만 크게 + 쓸려 나가는 「쿵」)
            Type et = T("EnermyController");
            if (et != null)
            {
                if (enemyDead == null) enemyDead = et.GetProperty("IsDead");
                int alive = 0;
                foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(et, FindObjectsSortMode.None))
                    if (!(enemyDead?.GetValue(o) is bool dead && dead)) alive++;
                if (lastAlive >= 0 && alive < lastAlive)
                {
                    int n = lastAlive - alive;
                    if (Gap("kill", 0.07f)) Sfx(FxClip("pop"), Mathf.Min(0.6f, 0.3f + 0.05f * (n - 1)), Rand(0.85f, 1.2f));
                    if (n >= 5 && Gap("wipe", 0.5f)) SfxVar(FxClip("boom"), 0.45f, 0.9f);
                }
                lastAlive = alive;
            }

            if (p != null) Attacks(p);
            BossSounds();
        }

        // 평타 · 진화 무기: 다음 사격 시각(nextShootTime)이 늘면 한 번 쏜 것 · 진화 무기는 손 반동(PlayerLook.kick)이 튀면
        void Attacks(object p)
        {
            if (!ReferenceEquals(p, lastPlayer)) { lastPlayer = p; lastShoot = -1f; lastKick = 0f; }
            object sm = Find("StageManager");
            object sa = sm != null ? Get(sm, "specials") : null;
            bool weapon = sa != null && sa.GetType().GetProperty("WeaponActive")?.GetValue(sa) is bool w && w;
            float next = Get(p, "nextShootTime") is float ns ? ns : -1f;
            string hero = Hero();
            if (!weapon && lastShoot >= 0f && next > lastShoot + 0.001f) HeroAttack(hero, p);
            lastShoot = next;
            object look = StaticGet("PlayerLook", "Instance");
            float kick = look != null && Get(look, "kick") is float k ? k : 0f;
            if (weapon && kick > lastKick + 0.02f && sa.GetType().GetProperty("CurrentWeapon")?.GetValue(sa) is int id) WeaponShot(id, hero, p);
            lastKick = kick;
        }

        // 영웅 평타 (CharacterKit.Attack 이 내는 소리)
        public void HeroAttack(string hero, object p)
        {
            if (hero == "Gunner" && gunSfx) return;                // 거너는 탄 수 감시(GunSounds)가 이미 냄
            if (!Gap("atk", 0.12f)) return;
            switch (hero)
            {
                case "Gunner": SfxVar((Get(p, "shotSound") as AudioClip) ?? FxClip("gunshot"), 0.3f); break;
                case "Swordsman": SfxVar(FxClip("slash"), 0.45f, 0.95f); SfxVar(FxClip("whoosh"), 0.3f, 0.8f); break;
                case "Archer": SfxVar(FxClip("bowtwang"), 0.45f); SfxVar(FxClip("arrowfly"), 0.3f, 1.2f); break;
                case "Alchemist": SfxVar(FxClip("glassclink"), 0.4f, Rand(0.9f, 1.15f)); SfxVar(FxClip("whoosh"), 0.25f, 1.5f); break;
                case "Ghost": SfxVar(FxClip("whoosh"), 0.35f, 0.75f); SfxVar(FxClip("freeze"), 0.25f, 1.4f); break;
                case "Clockmaker": SfxVar(FxClip("ratchet"), 0.35f, 1.2f); SfxVar(FxClip("clank"), 0.25f, 1.5f); SfxVar(FxClip("whoosh"), 0.25f, 1.3f); break;
                default: SfxVar(FxClip("whoosh"), 0.35f, 1.4f); SfxVar(FxClip("pew"), 0.25f, 1.2f); break;
            }
        }

        // 진화 무기 (SpecialAbilities.Weapons 의 발사 소리 · 20번부터는 영웅 전용 무기라 그 영웅 평타 소리)
        void WeaponShot(int id, string hero, object p)
        {
            if (id >= 20) { HeroAttack(hero, p); return; }
            if (!Gap("wpn", id == 3 ? 0.6f : 0.08f)) return;
            switch (id)
            {
                case 0: SfxVar(FxClip("shotgun"), 0.5f, 1.05f); break;
                case 1: SfxVar(FxClip("crack"), 0.5f, 0.9f); break;
                case 2: SfxVar(FxClip("gunshot"), 0.35f, Rand(1.05f, 1.2f)); break;
                case 3: SfxVar(FxClip("ignite"), 0.4f); break;
                case 4: SfxVar(FxClip("whoosh"), 0.3f, 1.6f); break;
                case 5: SfxVar(FxClip("zap"), 0.35f, 0.9f); SfxVar(FxClip("gunshot"), 0.2f, 1.4f); break;
                case 6: SfxVar(FxClip("whoosh"), 0.4f, 0.9f); break;
                case 7: SfxVar(FxClip("thump"), 0.5f); break;
                default: SfxVar(FxClip("gunshot"), 0.3f); break;
            }
        }

        // 보스: 맞을 때 타격음 (센 한 방은 「쩍」) · 쓰러질 때 큰 폭발 · 기억(BossMemory)이 켜지면 막는 소리 · 깨지면 「쨍」
        void BossSounds()
        {
            UnityEngine.Object b = Find("bosss");
            Component mem = null;
            Type mt = T("BossMemory");
            if (b != null && mt != null) mem = ((Component)b).GetComponent(mt);
            bool memOn = mem != null && mt.GetProperty("Active")?.GetValue(mem) is bool a && a;
            if (b == null) { bossObj = null; return; }
            float hp = Get(b, "EnemyHealth") is float h ? h : 0f;
            float max = Mathf.Max(1f, Convert.ToSingle(Get(b, "setEnemyHP") ?? 1));     // int 칸
            bool dead = b.GetType().GetProperty("IsDead")?.GetValue(b) is bool d && d;
            if (b != bossObj) { bossObj = b; bossHp = hp; bossDead = dead; bossMem = memOn; weakFirst = false; return; }
            if (dead && !bossDead)
            {
                Cue("boss_down");
                if (frames >= quietBossDownUntil)
                {
                    SfxVar(FxClip("bigboom"), 0.55f, 0.9f);
                    Sfx(FxClip("roar"), 0.35f, 0.6f);
                }
            }
            else if (!dead && hp < bossHp - 0.01f)
            {
                float drop = (bossHp - hp) / max;
                if (weakSfx)
                {
                    if (Gap("weak", 0.1f)) SfxVar(FxClip("weakhit"), 0.35f);       // 0.06초 · 0.45 는 따다닥 소리가 곡을 덮어서
                    if (!weakFirst) { weakFirst = true; Cue("weak_hit"); Sfx(FxClip("ding"), 0.45f, 1.6f); }
                }
                else if (memOn) { if (Gap("block", 0.12f)) SfxVar(FxClip("clank"), 0.3f, 1.3f); }       // 기억한 공격을 막음
                else if (drop > 0.03f && Gap("bosscrack", 0.25f)) SfxVar(FxClip("crack"), 0.45f, 0.8f);
                else if (Gap("bosshit", 0.11f)) SfxVar(FxClip("thump"), 0.3f, 1.1f);
            }
            if (memOn && !bossMem) { Cue("memory_on"); Sfx(FxClip("shimmer"), 0.55f, 0.55f); Sfx(FxClip("clank"), 0.35f, 0.7f); }
            if (!memOn && bossMem && !dead) { Cue("memory_break"); Sfx(FxClip("shatter"), 0.6f, 0.8f); }
            bossHp = hp;
            bossDead = dead;
            bossMem = memOn;
        }

        // 탄이 줄면 총소리, 장전이 시작되면 장전 소리 · 「R」 키 그림
        void GunSounds(object p)
        {
            if (p == null) { lastBullets = -1; return; }
            int bullets = Convert.ToInt32(Get(p, "NowBullet"));
            bool reloading = p.GetType().GetProperty("IsReloading")?.GetValue(p) is bool r && r;
            if (gunSfx && recording)
            {
                if (lastBullets >= 0 && bullets < lastBullets) Sfx((Get(p, "shotSound") as AudioClip) ?? FxClip("gunshot"), 0.55f);
                if (reloading && !lastReloading)
                {
                    Sfx((Get(p, "reloadSound") as AudioClip) ?? FxClip("clank"), 0.6f);
                    if (keyHint) keyCapFrames = Mathf.RoundToInt(0.6f * Fps);
                }
            }
            lastBullets = bullets;
            lastReloading = reloading;
            if (keyCap != null)
            {
                keyCap.SetActive(keyCapFrames > 0);
                if (keyCapFrames > 0) keyCapFrames--;
            }
        }

        // 커서: 지금 겨누는 월드 좌표를 화면 비율로 옮겨 위치만 바꿈 (자동 조종 · 녹화 중일 때만 보임)
        void DrawCursor()
        {
            if (cursorImage == null) return;
            Camera cam = Camera.main;
            bool on = showCursor && autopilot && recording && cam != null;
            Vector3 v = on ? cam.WorldToViewportPoint(aimNow) : Vector3.zero;
            on &= v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f;
            cursorImage.enabled = on;
            if (!on) return;
            RectTransform r = cursorImage.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(v.x, v.y);
            r.anchoredPosition = Vector2.zero;
        }

        // ================= 사람 같은 움직임 (humanMove · 상점 트레일러만)
        //   원거리: 무리 · 보스 둘레를 원을 그리며 돎(카이팅) · 다가오면 물러나며 쏨 · 가끔 멈춰 서서 조준 · 탄이 오면 짧은 옆걸음
        //   근접(검사): 예전 거리(nearMob · farMob …)로 붙어서 베기 — 방향 전환 보간 · 맵 가장자리 · 벽 · 탄 피하기만 같이
        //   이동 입력은 몇 프레임에 걸쳐 보간(MoveRate) · 무작위는 장면마다 고정 씨앗(System.Random) → 다시 찍어도 같은 영상
        const float MoveRate = 7f, DodgeRate = 20f;       // 입력 보간 빠르기 (1/초 · 7 ≈ 0.3초에 90%, 옆걸음은 더 날쌔게)
        static readonly HashSet<string> MeleeHeroes = new HashSet<string> { "Swordsman", "Ghost" };   // 유령 평타도 4.5칸 손 → 붙어서 싸움
        System.Random moveRnd = new System.Random(4210);
        Component movePlayer;
        int sceneNo, moveFrame, pauseAt, pauseLeft, dodgeLeft, dodgeCool, sidestepAt, flipCool, flipAt, aimPickAt;
        float orbitDir = 1f;
        Vector2 moveCur, dodgeDir, wallEscDir;
        int wallEscLeft;
        Transform aimMob;
        readonly List<Transform> mobs = new List<Transform>();

        int RndFrames(float a, float b) => Mathf.RoundToInt((a + (float)moveRnd.NextDouble() * (b - a)) * Fps);

        Vector3 HumanMove(Component player, Vector3 me, Vector3? target, bool boss)
        {
            // 장면(씬)이 바뀌면 씨앗 · 타이머를 처음부터 (어느 장면을 다시 찍어도 같게)
            if (player != movePlayer)
            {
                movePlayer = player;
                sceneNo++;
                moveRnd = new System.Random(4210 + sceneNo * 97);
                moveFrame = 0; moveCur = Vector2.zero; dodgeLeft = dodgeCool = pauseLeft = flipCool = wallEscLeft = 0; aimMob = null; aimPickAt = 0;
                pauseAt = RndFrames(1.0f, 2.0f); sidestepAt = RndFrames(0.8f, 1.5f); flipAt = RndFrames(3.5f, 6f);
                orbitDir = moveRnd.Next(2) == 0 ? 1f : -1f;
            }
            moveFrame++;
            if (dodgeCool > 0) dodgeCool--;
            if (flipCool > 0) flipCool--;

            string hero = Hero();
            bool melee = MeleeHeroes.Contains(hero);
            float near = boss ? (melee ? nearBoss : rangedBossNear) : (melee ? nearMob : rangedMobNear);
            float far = boss ? (melee ? farBoss : rangedBossFar) : (melee ? farMob : rangedMobFar);
            // 유령: 평타(손)가 4.5칸이라 검사보다 바짝 · 연금술사: 플라스크가 10칸에서 잘려 보스 앞에 떨어지지 않게
            if (hero == "Ghost") { near = boss ? 5f : 3f; far = boss ? 7f : 4.3f; }
            else if (hero == "Alchemist" && boss) far = Mathf.Min(far, 9.5f);

            // 살아 있는 잡몹 (가까운 것 · 무리 가운데)
            mobs.Clear();
            Transform nearest = null;
            float dNear = 999f, wSum = 0f;
            Vector2 centroid = Vector2.zero;
            foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None))
            {
                if (o.GetType().GetProperty("IsDead")?.GetValue(o) is bool dead && dead) continue;
                Transform t = ((Component)o).transform;
                float d = ((Vector2)(t.position - me)).magnitude;
                if (d > 16f) continue;
                mobs.Add(t);
            }
            // FindObjectsByType(None) 순서에 기대지 않게 자리로 정렬 (같은 판이면 같은 표적 · 같은 무작위 배정)
            mobs.Sort((a, b) => a.position.x != b.position.x ? a.position.x.CompareTo(b.position.x) : a.position.y.CompareTo(b.position.y));
            foreach (Transform t in mobs)
            {
                float d = ((Vector2)(t.position - me)).magnitude;
                if (d < dNear) { dNear = d; nearest = t; }
                float w = 1f / (1f + d);
                centroid += (Vector2)t.position * w; wSum += w;
            }
            if (wSum > 0f) centroid /= wSum;

            Vector2 desired = Vector2.zero;
            Vector3 aim = me + Vector3.right * 5f;
            if (target.HasValue)
            {
                Vector2 center = boss || wSum <= 0f ? (Vector2)target.Value : centroid;
                Vector2 to = center - (Vector2)me;
                float dC = Mathf.Max(0.01f, to.magnitude);
                Vector2 toN = to / dC, side = new Vector2(-toN.y, toN.x);
                if (melee)
                {
                    // 검사: 예전과 같은 붙어 베기 (가장 가까운 적 · 보스 기준)
                    Vector2 tt = target.Value - me;
                    float d = tt.magnitude;
                    Vector2 s = new Vector2(-tt.y, tt.x).normalized;
                    desired = s * 0.75f + (d < near ? -tt.normalized * 1.2f : d > far ? tt.normalized * 0.7f : Vector2.zero);
                }
                else
                {
                    // 거리 고리: 무리는 「가장 가까운 놈」이 near 안으로 오면 물러남 · 무리 가운데가 far 밖이면 다가감 / 보스는 보스까지 거리
                    float dIn = boss ? dC : dNear;
                    float radial = dIn < near ? -Mathf.Clamp((near - dIn) / 2f, 0.45f, 1.3f) : dC > far ? Mathf.Clamp((dC - far) / 2f, 0.3f, 0.8f) : 0f;
                    // 원을 그리며 돎 (가까울 땐 덜 돌고 더 물러남) → 무리가 한쪽으로 뭉쳐 따라옴
                    desired = side * orbitDir * (dIn < near * 0.7f ? 0.55f : 0.9f) + toN * radial;
                    // 바짝 붙은 잡몹은 밀어내듯 피함 (보스 장면의 부하도)
                    float repelR = boss ? 4f : near * 0.75f;
                    foreach (Transform m in mobs)
                    {
                        Vector2 away = (Vector2)(me - m.position);
                        float d = away.magnitude;
                        if (d < repelR && d > 0.01f) desired += away / d * (1f - d / repelR) * 1.6f;
                    }
                    // 가끔 도는 방향을 바꿈 (늘 같은 쪽으로만 돌면 기계 같음)
                    if (moveFrame >= flipAt) { orbitDir = -orbitDir; flipAt = moveFrame + RndFrames(3.5f, 6.5f); }
                    // 가끔 멈춰 서서 조준 사격 (가까운 적이 없을 때만)
                    if (moveFrame >= pauseAt)
                    {
                        bool safe = boss ? dNear > 4.5f && dC > near * 0.85f : dNear > near * 0.85f;
                        if (safe) { pauseLeft = RndFrames(0.3f, 0.6f); pauseAt = moveFrame + pauseLeft + RndFrames(1.6f, 3.0f); }
                        else pauseAt = moveFrame + RndFrames(0.25f, 0.45f);
                    }
                    // 정해진 간격의 짧은 옆걸음 (보스 장면은 더 자주 · 무작위지만 씨앗 고정)
                    if (moveFrame >= sidestepAt)
                    {
                        if (dodgeLeft <= 0) StartDodge((moveRnd.Next(2) == 0 ? side : -side) * 0.9f - toN * 0.25f, RndFrames(0.18f, 0.3f));
                        sidestepAt = moveFrame + (boss ? RndFrames(1.3f, 2.4f) : RndFrames(2.2f, 3.6f));
                    }
                }

                // 조준: 보스 우선 · 무리는 몰린 곳 · 바짝 붙은 놈이 있으면 그놈 먼저 (0.4~0.7초마다 다시 고름 → 커서가 표적 사이를 옮겨 다님)
                if (boss) aim = target.Value;
                else
                {
                    bool lost = aimMob == null || !mobs.Contains(aimMob);
                    if (melee) aimMob = nearest;      // 근접은 매 프레임 가장 가까운 놈 (반대쪽으로 베지 않게)
                    else if (lost || moveFrame >= aimPickAt)
                    {
                        aimMob = PickAim(me, nearest, dNear, near);
                        aimPickAt = moveFrame + RndFrames(0.4f, 0.7f);
                    }
                    aim = aimMob != null ? aimMob.position : target.Value;
                }
            }
            bool pausing = pauseLeft > 0;
            if (pausing) { pauseLeft--; desired = Vector2.zero; }

            // 날아오는 적 탄 · 바닥 위험 구역을 보고 피함 (근접 · 원거리 모두)
            if (dodgeCool <= 0 && Incoming(me, out Vector2 dodge)) StartDodge(dodge, Mathf.RoundToInt(0.24f * Fps));
            desired += Hazard(me) * 1.6f;
            if (dodgeLeft > 0) { dodgeLeft--; desired = dodgeDir + desired * 0.25f; pauseLeft = 0; pausing = false; }

            // 맵 가장자리 · 결계 안쪽 유지 (가장자리로 밀리면 도는 방향도 바꿈)
            Vector2 inward = Inward(me);
            if (inward.sqrMagnitude > 0.09f && flipCool <= 0 && target.HasValue)
            {
                Vector2 to = (Vector2)(target.Value - me);
                Vector2 side = new Vector2(-to.y, to.x).normalized * orbitDir;
                if (Vector2.Dot(side, inward) < 0f) { orbitDir = -orbitDir; flipCool = Fps * 3 / 2; }
            }
            desired += inward;
            if (desired.sqrMagnitude > 1f) desired.Normalize();

            // 벽에 박히지 않게: 가려는 쪽이 벽이면 ±50° · ±100° 로 틀고, 다 막히면 돌아섬
            //   돌아선 방향은 0.4~0.6초 붙들어 둠 (매 프레임 다시 고르면 구석에서 벽 쪽 ↔ 반대쪽을 오가며 제자리에서 떪)
            if (wallEscLeft > 0)
            {
                wallEscLeft--;
                if (Wall(me + (Vector3)(wallEscDir * 1.4f))) wallEscLeft = 0;
                else desired = wallEscDir * Mathf.Max(0.6f, desired.magnitude);
            }
            if (wallEscLeft <= 0 && desired.sqrMagnitude > 0.01f && Wall(me + (Vector3)(desired.normalized * 1.4f)))
            {
                bool found = false;
                foreach (float a in new[] { 50f, -50f, 100f, -100f })
                {
                    Vector2 r = Quaternion.Euler(0f, 0f, a * orbitDir) * desired;
                    if (!Wall(me + (Vector3)(r.normalized * 1.4f))) { desired = r; found = true; break; }
                }
                if (!found)
                {
                    desired = -desired;
                    wallEscDir = desired.normalized; wallEscLeft = RndFrames(0.4f, 0.6f);
                    if (flipCool <= 0) { orbitDir = -orbitDir; flipCool = Fps; }
                }
            }

            // 이동 입력 보간 (방향이 몇 프레임에 걸쳐 돌아감 · 멈출 때도 서서히) — 게임이 입력을 정규화하므로 아주 작아지면 0
            float k = 1f - Mathf.Exp(-(dodgeLeft > 0 ? DodgeRate : MoveRate) / Fps);
            // 멈춰 조준할 땐 바로 섬 (게임이 입력을 늘 최대 속도로 바꾸므로 서서히 줄이면 0.2초 넘게 그대로 달림)
            moveCur = pausing ? Vector2.zero : Vector2.Lerp(moveCur, desired, k);
            In("AutoMove", moveCur.sqrMagnitude < 0.04f ? Vector2.zero : moveCur);
            return aim;
        }

        void StartDodge(Vector2 dir, int frames)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            dodgeDir = dir.normalized;
            dodgeLeft = Mathf.Max(1, frames);
            dodgeCool = dodgeLeft + Fps / 3;
        }

        // 무리에서 조준할 놈: 바짝 붙은 놈이 있으면 그놈 · 아니면 2.5칸 안에 이웃이 많은(몰린) 놈 (먼 놈은 덜)
        Transform PickAim(Vector3 me, Transform nearest, float dNear, float near)
        {
            if (nearest != null && dNear < near * 0.7f) return nearest;
            Transform best = nearest;
            float bs = -999f;
            foreach (Transform a in mobs)
            {
                Vector2 off = (Vector2)(a.position - me);
                if (Mathf.Abs(off.x) > 22f || Mathf.Abs(off.y) > 12.5f) continue;      // 화면 밖 적은 쏘지 않음 (카메라 15칸)
                int n = 0;
                foreach (Transform b in mobs) if (b != a && ((Vector2)(a.position - b.position)).sqrMagnitude < 2.5f * 2.5f) n++;
                float s = n - off.magnitude * 0.25f + (float)moveRnd.NextDouble() * 0.4f;
                if (s > bs) { bs = s; best = a; }
            }
            return best;
        }

        // 적 탄(HostileProjectile.Live)이 0.6초 안에 몸에 닿을 길로 오면, 탄 길에서 벗어나는 옆 방향
        static FieldInfo liveF, pDirF, pSpeedF, pRadF;
        bool Incoming(Vector3 me, out Vector2 dir)
        {
            dir = Vector2.zero;
            Type hp = T("HostileProjectile");
            if (hp == null) return false;
            if (liveF == null) { liveF = hp.GetField("Live", Any); pDirF = hp.GetField("dir", Any); pSpeedF = hp.GetField("speed", Any); pRadF = hp.GetField("radius", Any); }
            if (!(liveF?.GetValue(null) is IList live) || pDirF == null || pSpeedF == null) return false;
            float best = 99f;
            foreach (object o in live)
            {
                if (!(o is Component c) || c == null || !c.gameObject.activeInHierarchy) continue;
                Vector2 v = ((Vector2)pDirF.GetValue(o)).normalized * (float)pSpeedF.GetValue(o);
                float vv = v.sqrMagnitude;
                if (vv < 0.01f) continue;
                Vector2 r = (Vector2)(me - c.transform.position);
                float t = Vector2.Dot(r, v) / vv;
                if (t <= 0f || t > 0.6f || t >= best) continue;
                Vector2 miss = r - v * t;
                float rad = pRadF != null ? (float)pRadF.GetValue(o) : 0.5f;
                if (miss.magnitude > rad + 0.9f) continue;
                best = t;
                Vector2 perp = new Vector2(-v.y, v.x).normalized;
                dir = miss.sqrMagnitude < 0.0004f ? perp * orbitDir : Vector2.Dot(perp, miss) >= 0f ? perp : -perp;
            }
            return best < 99f;
        }

        // 바닥 위험 구역(HazardZone) 안이면 바깥쪽으로
        static Vector2 Hazard(Vector3 me)
        {
            Type hz = T("HazardZone");
            if (hz == null) return Vector2.zero;
            Vector2 v = Vector2.zero;
            foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(hz, FindObjectsSortMode.None))
            {
                float r = Get(o, "radius") is float f ? f + 0.6f : 0f;
                Vector2 off = (Vector2)(me - ((Component)o).transform.position);
                float d = off.magnitude;
                if (d < r && d > 0.01f) v += off / d * (1f - d / r);
            }
            return Vector2.ClampMagnitude(v, 1f);
        }

        // 맵 가장자리(EnemySpawner 의 spawnArea · Hostile.ClampArena 와 같은 범위) · 결계 원(BossUltimate) 안쪽으로 미는 힘
        static Vector2 Inward(Vector3 me)
        {
            Vector2 v = Vector2.zero;
            const float m = 3.5f;
            object sp = Find("EnemySpawner");
            if (sp != null && Get(sp, "spawnAreaMin") is Vector2 lo && Get(sp, "spawnAreaMax") is Vector2 hi)
            {
                if (me.x < lo.x + m) v.x += (lo.x + m - me.x) / m;
                if (me.x > hi.x - m) v.x -= (me.x - (hi.x - m)) / m;
                if (me.y < lo.y + m) v.y += (lo.y + m - me.y) / m;
                if (me.y > hi.y + 1.5f - m) v.y -= (me.y - (hi.y + 1.5f - m)) / m;
            }
            Type bu = T("BossUltimate");
            if (bu != null && bu.GetProperty("Active", Any)?.GetValue(null) is bool on && on && Find("BossUltimate") is object ult
                && Get(ult, "center") is Vector3 c && Get(ult, "radiusNow") is float r)
            {
                float lim = Mathf.Max(r * 0.6f, r - 6f);
                Vector2 off = (Vector2)(me - c);
                if (off.magnitude > lim) v -= off.normalized * Mathf.Clamp01((off.magnitude - lim) / 3f) * 1.5f;
            }
            return Vector2.ClampMagnitude(v, 1.5f);
        }

        static MethodInfo isWallM;
        static bool Wall(Vector3 p)
        {
            if (isWallM == null) isWallM = T("Hostile")?.GetMethod("IsWall", Any, null, new[] { typeof(Vector3) }, null);
            return isWallM != null && isWallM.Invoke(null, new object[] { p }) is bool w && w;
        }

        static Vector3? Nearest(Vector3 from, out bool boss)
        {
            Vector3? best = null; float bd = 16f * 16f;
            boss = false;
            foreach (string type in new[] { "bosss", "EnermyController" })
            {
                boss = type == "bosss";
                foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(T(type), FindObjectsSortMode.None))
                {
                    if (o.GetType().GetProperty("IsDead")?.GetValue(o) is bool dead && dead) continue;
                    Vector3 p = ((Component)o).transform.position;
                    float d = (p - from).sqrMagnitude;
                    if (d < bd) { bd = d; best = p; }
                }
                if (best.HasValue) return best;
            }
            return best;
        }
    }

    static void CanvasesToCamera(Camera cam)
    {
        SortingLayer[] layers = SortingLayer.layers;
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 5f;
                c.overrideSorting = true;
                c.sortingLayerName = layers[layers.Length - 1].name;
                c.sortingOrder = Mathf.Min(32000, 30000 + c.sortingOrder);
            }
            // 진화 카드 · 영혼 트리처럼 자체 정렬을 쓰는 하위 캔버스도 가장 위 레이어로 (안 그러면 맵 그림 아래로 깔림)
            else if (!c.isRootCanvas && c.overrideSorting && c.sortingLayerName != layers[layers.Length - 1].name)
            {
                c.sortingLayerName = layers[layers.Length - 1].name;
                c.sortingOrder = Mathf.Min(32400, 31000 + c.sortingOrder);
            }
    }

    // ================================================================= 연출 도구
    static Rec rec;

    // 녹화 중에는 게임 시간이 1/Fps 씩 흐르므로 프레임 수로 기다림
    static IEnumerator F(float seconds)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Fps));
        for (int i = 0; i < n; i++) yield return null;
    }

    static void Shot(string name)
    {
        Texture2D t = rec.Grab(false);
        string path = Path.Combine(Out, "screenshots", name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, t.EncodeToPNG());
        Debug.Log("[CAP] " + name);
    }

    static IEnumerator Fade(float from, float to, float time)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(time * Fps));
        for (int i = 0; i <= n; i++) { rec.black.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, i / (float)n)); yield return null; }
    }

    static IEnumerator Caption(string text, float time)
    {
        rec.caption.text = NoCaptions ? "" : text;
        int n = Mathf.RoundToInt(time * Fps);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Fps;
            rec.caption.alpha = Mathf.Min(Mathf.Clamp01(t / 0.3f), Mathf.Clamp01((time - t) / 0.35f));
            yield return null;
        }
        rec.caption.alpha = 0f;
    }

    static void Say(string text, float time) => rec.StartCoroutine(Caption(text, time));

    static IEnumerator Load(string hero, int stage)
    {
        Type cd = T("CharacterData"), cid = T("CharacterId");
        cd.GetField("Override", Any).SetValue(null, Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(cid), Enum.Parse(cid, hero)));
        SceneManager.LoadScene("GameScene");
        yield return null; yield return null; yield return null;
        yield return CloseLevelUps();
        if (stage > 0) GoStage(stage);
        Swarm(stage, 12);
        yield return null;
        // 새 콘티의 컷은 검은 화면 없이 바로 이어 붙임 (불러오는 동안은 녹화하지 않음 · 옛 장면은 Fade(1 → 0)로 시작하므로 그대로)
        if (rec != null && rec.black != null && !rec.recording) rec.black.color = new Color(0f, 0f, 0f, 0f);
    }

    // 레벨업 카드 확정 (게임에서는 카드를 누른 뒤 [Space])
    static void ConfirmCard(object shop, int slot)
    {
        string f = slot == 0 ? "first" : slot == 1 ? "second" : "third";
        Set(shop, "pendingSlot", -1);
        Call(shop, "onSelect", Get(shop, f));
    }

    // 판을 시작하면 뜨는 첫 카드 창과 쌓인 레벨업을 정리
    static IEnumerator CloseLevelUps()
    {
        for (int i = 0; i < 6; i++)
        {
            yield return null;
            object shop = Find("LevelShop");
            if (shop != null && (bool)shop.GetType().GetProperty("IsOpen").GetValue(shop)) ConfirmCard(shop, 0);
        }
        Time.timeScale = 1f;
    }

    static void GoStage(int stage)
    {
        object sm = Find("StageManager");
        Call(sm, "ShowStageMap", stage);            // 0 묘역 · 1 지옥 · 2 초원 · 3 심연 (맵 켜기 · 배경색)
        sm.GetType().GetProperty("CurrentStage", Any).SetValue(sm, stage);
        object sp = Get(sm, "spawner");
        sp.GetType().GetMethod("StartStage").Invoke(sp, new object[] { stage });
        object bar = Find("bossbar");
        bar.GetType().GetField("bossSpawn").SetValue(bar, false);
        ((Transform)Get(sm, "player")).position = (Vector2)Get(sm, "stage2PlayerStart");
    }

    // 주인공 둘레에 그 스테이지 적을 깔아 둠 (화면이 비지 않게)
    static void Swarm(int stage, int count)
    {
        object data = Resources.Load("CodexData");
        var enemies = (GameObject[])Get(data, "enemies");
        var stages = (int[])Get(data, "enemyStage");
        var pool = new List<GameObject>();
        for (int i = 0; i < enemies.Length; i++) if (stages[i] == stage) pool.Add(enemies[i]);
        if (pool.Count == 0 && StageConfig(stage) is object cfg) pool.AddRange((GameObject[])Get(cfg, "enemies"));     // 4장 심연: 도감 대신 스폰러의 견본
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            float a = i / (float)count * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
            UnityEngine.Object.Instantiate(pool[i % pool.Count], me + new Vector3(Mathf.Cos(a) * 1.4f, Mathf.Sin(a)) * UnityEngine.Random.Range(5.5f, 8.5f), Quaternion.identity);
        }
    }

    // 스폰러의 그 장 설정 (StageConfig)
    static object StageConfig(int stage)
    {
        object sp = Get(Find("StageManager"), "spawner");
        Array stages = sp != null ? (Array)Get(sp, "stages") : null;
        return stages != null && stage < stages.Length ? stages.GetValue(stage) : null;
    }

    static IEnumerator Ult(float hold)
    {
        object g = Find("SkillGauge");
        if (g != null) g.GetType().GetField("SkillPoint").SetValue(g, 99999f);
        yield return null;
        In("AutoUlt", true);
        In("UltDownFrame", Time.frameCount + 1);
        UltSfx(true);
        yield return F(Mathf.Max(hold, 0.05f));
        In("AutoUlt", false);
        In("UltUpFrame", Time.frameCount + 1);
        UltSfx(false);
        yield return null; yield return null;
    }

    // 필살기 소리 (UltCastFlair 「ultcast」 + 영웅마다 필살기 소리 · press: 누름 / 아니면 뗌)
    static void UltSfx(bool press)
    {
        if (rec == null || !rec.rich || !rec.recording) return;
        string hero = Hero();
        object p = Find("PlayerController");
        if (press)
        {
            Cue("ult_" + hero);
            Sx("ultcast", 0.6f);
            Sx("whoosh", 0.35f, 0.6f);
        }
        switch (hero)
        {
            case "Gunner":
                if (press) rec.Sfx((p != null ? Get(p, "chargeSound") as AudioClip : null) ?? FxClip("railcharge"), 0.45f);
                else { rec.Sfx((p != null ? Get(p, "shotSound") as AudioClip : null) ?? FxClip("gunshot"), 0.6f, 0.85f); Sx("bigboom", 0.45f, 1.1f); }
                break;
            case "Swordsman":
                if (press) { Sx("spin", 0.55f); Sx("slash", 0.6f, 0.8f); }
                else { Sx("boom", 0.45f, 0.9f); Sx("shatter", 0.4f, 0.8f); }
                break;
            case "Archer":
                if (press) { Sx("bowtwang", 0.6f, 0.85f); Sx("whoosh", 0.4f, 0.9f); }
                else for (int i = 0; i < 5; i++) rec.Later(0.08f * i, FxClip("arrowfly"), 0.25f, rec.Rand(0.8f, 1.2f));      // 화살 비
                break;
            case "Alchemist":
                if (press) { Sx("glassclink", 0.5f, 0.8f); Sx("fizz", 0.4f); }
                else { Sx("boom", 0.5f, 0.9f); Sx("shatter", 0.4f, 0.8f); }
                break;
            case "Ghost":
                if (press) { Sx("possess", 0.6f); Sx("whoosh", 0.45f, 0.5f); }
                else Sx("wail", 0.4f, 0.8f);
                break;
            case "Clockmaker":
                if (press) { Sx("rewind", 0.6f); Sx("bell", 0.45f, 0.8f); }
                break;
            default:
                if (!press) Sx("boom", 0.45f);
                break;
        }
    }

    static object SpawnBoss(int stage, float hp)
    {
        object data = Resources.Load("CodexData");
        GameObject[] bosses = (GameObject[])Get(data, "bosses");
        GameObject prefab = stage < bosses.Length ? bosses[stage] : (GameObject)Get(StageConfig(stage), "bossPrefab");     // 4장 거울의 군주는 스폰러에만 있음
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        GameObject go = UnityEngine.Object.Instantiate(prefab, me + new Vector3(8.5f, 3.5f, 0f), Quaternion.identity);
        object bar = Find("bossbar");
        bar.GetType().GetField("bossSpawn").SetValue(bar, true);
        object sp = Get(Find("StageManager"), "spawner");
        sp.GetType().GetField("bossSpawned", Any)?.SetValue(sp, true);
        object b = go.GetComponent(T("bosss"));
        if (b != null) Set(b, "EnemyHealth", Convert.ToSingle(Get(b, "setEnemyHP")) * hp);
        // 보스는 제자리에서 기술만 씀 (쫓아와서 영웅을 덮으면 화면에서 영웅이 안 보임)
        if (b != null && b.GetType().GetField("speed", Any) is FieldInfo speed) speed.SetValue(b, Convert.ChangeType(0, speed.FieldType));
        return b;
    }

    // 전설 스킨 장착 (복사본 에디터 PlayerPrefs 에만 저장됨 · 끝나면 지움)
    static readonly List<string> skinKeys = new List<string>();
    static void WearLegend(int who)
    {
        Type sd = T("SkinData");
        foreach (object s in (Array)sd.GetField("All").GetValue(null))
        {
            if (Convert.ToInt32(Get(s, "tier")) != 3 || Convert.ToInt32(Get(s, "who")) != who || Get(s, "kind").ToString() == "Effect") continue;
            string id = (string)Get(s, "id");
            PlayerPrefs.SetInt("skin.own." + id, 1);
            skinKeys.Add("skin.own." + id);
            skinKeys.Add("skin.eq." + Convert.ToInt32(Get(s, "kind")) + "." + who);
            sd.GetMethod("Equip").Invoke(null, new[] { s });
        }
    }

    static void BuildOverlay()
    {
        GameObject go = new GameObject("CaptureOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.layer = 5;
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.planeDistance = 1f;
        c.overrideSorting = true;
        SortingLayer[] layers = SortingLayer.layers;
        c.sortingLayerName = layers[layers.Length - 1].name;
        c.sortingOrder = 32500;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(W, H);
        cs.matchWidthOrHeight = 1f;
        rec.captionCanvas = c;

        rec.black = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        rec.black.transform.SetParent(go.transform, false);
        RectTransform br = rec.black.rectTransform;
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
        rec.black.color = Color.black;

        rec.flash = new GameObject("Flash", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        rec.flash.transform.SetParent(go.transform, false);
        RectTransform fr = rec.flash.rectTransform;
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
        rec.flash.color = new Color(1f, 1f, 1f, 0f);
        rec.flash.raycastTarget = false;

        TMP_FontAsset font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x.name.StartsWith("Cafe24"));
        Material outline = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name.StartsWith("Cafe24") && m.name.Contains("Outline"));
        rec.caption = Text(go.transform, font, outline, 66f, new Vector2(0f, -400f));
        rec.logo = Text(go.transform, font, outline, 230f, new Vector2(0f, 60f));
        rec.logo.text = "Soul Saver";
        rec.sub = Text(go.transform, font, null, 54f, new Vector2(0f, -150f));
        rec.sub.text = "WISHLIST NOW ON STEAM";
        rec.sub.color = new Color(0.92f, 0.88f, 0.8f);
        rec.caption.alpha = rec.logo.alpha = rec.sub.alpha = 0f;

        // 커서 (게임의 금빛 조준 커서 · 없으면 기본) · 화면 비율 앵커로 옮김 (DrawCursor)
        Texture2D cur = Resources.Load<Texture2D>("Cursors/cursor_gold_aim_0") ?? Resources.Load<Texture2D>("Cursors/cursor_default_aim");
        if (cur != null)
        {
            RawImage ci = new GameObject("Cursor", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            ci.gameObject.layer = 5;
            ci.transform.SetParent(go.transform, false);
            ci.texture = cur;
            ci.raycastTarget = false;
            ci.rectTransform.sizeDelta = new Vector2(64f, 64f);
            ci.enabled = false;
            rec.cursorImage = ci;
        }

        // 「R」 키 그림 (화면 아래 · 자막 대신)
        GameObject key = new GameObject("KeyR", typeof(RectTransform), typeof(Image));
        key.layer = 5;
        key.transform.SetParent(go.transform, false);
        Image ki = key.GetComponent<Image>();
        ki.color = new Color(0.08f, 0.07f, 0.1f, 0.85f);
        ki.raycastTarget = false;
        RectTransform kr = ki.rectTransform;
        kr.anchoredPosition = new Vector2(0f, -300f);
        kr.sizeDelta = new Vector2(96f, 96f);
        Outline ko = key.AddComponent<Outline>();
        ko.effectColor = Gold;
        ko.effectDistance = new Vector2(4f, -4f);
        TextMeshProUGUI kt = Text(key.transform, font, outline, 64f, Vector2.zero);
        kt.rectTransform.sizeDelta = new Vector2(96f, 96f);
        kt.text = "R";
        key.SetActive(false);
        rec.keyCap = key;
    }

    static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, Material mat, float size, Vector2 pos)
    {
        TextMeshProUGUI t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.gameObject.layer = 5;
        t.transform.SetParent(parent, false);
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = new Vector2(1800f, size * 1.3f);
        if (font != null) t.font = font;
        if (mat != null) t.fontSharedMaterial = mat;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Gold;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    // ================================================================= 장면
    // 한 영웅 장면: 불러오기(녹화 멈춤) → 싸움 → 필살기
    static IEnumerator Hero(string hero, int who, int stage, int boss, float fight, float ultHold, string shot)
    {
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        object b = boss >= 0 ? SpawnBoss(boss, 0.75f) : null;
        rec.autopilot = true;
        yield return F(1.2f);                       // 적이 다가올 시간 (녹화 안 함)
        rec.recording = true;
        yield return Fade(1f, 0f, 0.2f);
        yield return F(fight * 0.6f);
        if (shot != null) Shot(shot);
        yield return F(fight * 0.4f);
        yield return Ult(ultHold);
        yield return F(1.1f);
        yield return Fade(0f, 1f, 0.2f);
    }

    // ---------------- 스킬 진화: 진화에 한 단계 남은 카드를 가운데에 놓고 골라서 진화 연출까지
    // 거너 「천둥 장전」 = 장전 충격파(13, 3단계) + 탄피 지뢰(15, 4단계) + 섬광 장전(19, 4단계)
    static void ForceCards(object shop, int a, int b, int c)
    {
        string[] names = (string[])Get(shop, "ability_name");
        string[] contents = (string[])Get(shop, "ability_content");
        Set(shop, "first", a); Set(shop, "second", b); Set(shop, "third", c);
        foreach (var (title, body, icon, id) in new[] { ("FirstTitle", "FirstAbility", "FirstIcon", a), ("SecondTitle", "SecondAbility", "SecondIcon", b), ("ThirdTitle", "ThirdAbility", "ThirdIcon", c) })
        {
            ((TMP_Text)Get(shop, title)).text = names[id];
            ((TMP_Text)Get(shop, body)).text = contents[id];
            Call(shop, "SetCardIcon", Get(shop, icon), id);
        }
        Call(shop, "ShowEvoHints");
    }

    static IEnumerator SkillEvolution(int caption = C_SkillFuse)
    {
        object shop = Find("LevelShop");
        int[] lv = (int[])Get(shop, "ability_level");
        bool[] sel = (bool[])Get(shop, "ability_selected");
        // 진화 재료 단계를 미리 채워 둠 (거너 카드: 장전 충격파 3/3 · 탄피 지뢰 4/4 · 섬광 장전 3/4, 그 밖에 진행 중인 조합 몇 개)
        void Lv(int id, int n) { lv[id] = n; if (n >= (int)T("LevelShop").GetMethod("MaxLevelOf").Invoke(null, new object[] { Enum.Parse(T("CharacterId"), "Gunner"), id })) sel[id] = true; }
        Lv(13, 3); Lv(15, 4); Lv(19, 3); Lv(0, 2); Lv(16, 2); Lv(17, 1); Lv(6, 2);
        rec.levelUpAllowed = true;
        Call(shop, "openLevelShop");
        Call(shop, "setAbilitys");
        ForceCards(shop, 16, 19, 17);
        Sx("levelup", 0.45f);                       // 레벨업 카드 창 (Juice.LevelUp)
        Sx("ding", 0.35f, 0.8f);                    // 진화 힌트 (LevelShop.EvoUI)
        Say(C(caption), 4.6f);
        yield return F(0.9f);
        Call(shop, "onSecondButton");
        Sx("pop", 0.4f, 1.3f);                      // 카드 누름
        yield return F(0.5f);
        Shot("09_skill_evolution_cards");
        yield return F(0.6f);
        ConfirmCard(shop, 1);                       // 마지막 재료 → 진화 연출
        Cue("skill_evo_confirm");
        Sx("chime", 0.45f, 1.3f);
        Sx("pulse", 0.45f, 0.9f);                   // SkillEvolutionUI: 어두워지며 재료가 모임
        Sx("shimmer", 0.4f, 0.8f);
        rec.levelUpAllowed = false;
        yield return F(2.9f);
        Shot("10_skill_evolution");
        yield return F(0.8f);
        object ui = Find("SkillEvolutionUI");
        if (ui != null) Set(ui, "closeRequested", true);
        yield return F(0.6f);
    }

    // ---------------- 보스 필살기 결계: 외침 화면 → 어두워진 맵 · 둥근 결계 안의 공격
    static IEnumerator Barrier(string hero, int who, int stage, int caption = C_Barrier)
    {
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        object b = SpawnBoss(stage, 0.8f);
        rec.autopilot = true;
        yield return F(1.2f);
        rec.recording = true;
        yield return Fade(1f, 0f, 0.2f);
        yield return F(1.4f);
        Component bc = (Component)b;
        Component ult = bc.GetComponent(T("BossUltimate"));
        if (ult == null) ult = bc.gameObject.AddComponent(T("BossUltimate"));
        yield return null;
        ((MonoBehaviour)ult).StartCoroutine((IEnumerator)Call(ult, "Run"));
        Say(C(caption), 4f);
        yield return F(1.7f);
        Shot("11_boss_barrier_title");
        yield return F(4.4f);                      // 외침(약 3.3초) → 결계가 펼쳐지고 공격이 쏟아지는 중
        Shot("12_boss_barrier");
        yield return F(1.6f);
        yield return Fade(0f, 1f, 0.2f);
    }

    // ---------------- 4장 영혼의 심연 (2.1.1~): 거울의 군주가 지금 영웅의 필살기를 흉내 냄 (2.1.4~)
    // brief: 새 콘티(16~21초)용 짧은 판 — 앞 대기 · 뒤 싸움을 줄임 (약 4.5초)
    static IEnumerator Mirror(string hero, int who, int caption = C_Mirror, bool brief = false)
    {
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, 3);
        Component b = (Component)SpawnBoss(3, 0.8f);
        yield return null;
        Component skills = b != null ? b.GetComponent(T("BossSkills")) : null;
        if (skills != null) Set(skills, "next", Time.time + 999f);     // 흉내를 쓰기 전에 다른 기술을 꺼내지 않게
        rec.autopilot = true;
        yield return F(1.2f);
        rec.recording = true;
        Cue("mirror", true);
        yield return Fade(1f, 0f, 0.2f);
        Say(C(caption), brief ? 4.2f : 4.6f);
        yield return F(brief ? 0.4f : 1.6f);
        Shot("13_abyss_mirror_sovereign");
        if (skills != null)
        {
            ((MonoBehaviour)skills).StartCoroutine((IEnumerator)Call(skills, "Run", Call(skills, "MirrorUlt", true, true)));
            Cue("mirror_ult");
            Sx("roar", 0.4f, 0.5f);                 // 거울의 군주가 내 필살기를 흉내 냄
            Sx("ultcast", 0.5f, 0.7f);
        }
        yield return F(brief ? 2.0f : 2.4f);
        Shot("14_mirror_mimics_ult");
        yield return F(brief ? 0.6f : 2.2f);
        yield return Ult(brief ? 0.4f : 0.6f);
        yield return F(brief ? 0.5f : 0.9f);
        yield return Fade(0f, 1f, 0.2f);
    }

    // ================================================================= 새 콘티 장면 (차별점 구현 3-1 · 3-2)
    // 녹화된 프레임으로 세는 자막 (불러오기처럼 녹화를 멈춘 동안에는 시간이 흐르지 않아, 여러 컷에 걸쳐도 이어 보임)
    static IEnumerator CaptionRec(string text, float time)
    {
        rec.caption.text = NoCaptions ? "" : text;
        int n = Mathf.RoundToInt(time * Fps), start = rec.frames;
        for (int guard = 0; rec.frames - start < n && guard < n * 8; guard++)
        {
            float t = (rec.frames - start) / (float)Fps;
            rec.caption.alpha = Mathf.Min(Mathf.Clamp01(t / 0.3f), Mathf.Clamp01((time - t) / 0.35f));
            yield return null;
        }
        rec.caption.alpha = 0f;
    }
    static void SayRec(int caption, float time) => rec.StartCoroutine(CaptionRec(C(caption), time));

    // 녹화된 길이가 seconds 가 될 때까지 (컷 길이를 콘티에 맞춤)
    const int EndcardFrame = 1246;   // make_trailer_music.py CUE["endcard"] 와 같은 값 (41.53초)

    static IEnumerator UntilRecorded(int start, float seconds)
    {
        int n = Mathf.RoundToInt(seconds * Fps);
        for (int guard = 0; rec.frames - start < n && guard < n * 8; guard++) yield return null;
    }

    static void StopInput()
    {
        In("AutoMove", Vector2.zero);
        In("AutoFire", false);
    }

    // 눌렀다 떼기 한 번 (거너 리볼버는 누르고 있는 동안 쏨 · 영웅 무기는 누른 프레임)
    static IEnumerator Tap()
    {
        In("FireDownFrame", Time.frameCount + 1);
        In("AutoFire", true);
        yield return null; yield return null;
        In("AutoFire", false);
        In("FireUpFrame", Time.frameCount + 1);
    }

    // skip 번째로 가까운 살아 있는 잡몹 (표적마다 커서가 옮겨 다니게 가까운 둘을 번갈아)
    static Transform AimPick(Vector3 me, int skip)
    {
        Transform best = null, second = null;
        float bd = 13f * 13f, sd = bd;
        foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None))
        {
            if (o.GetType().GetProperty("IsDead")?.GetValue(o) is bool dead && dead) continue;
            Transform t = ((Component)o).transform;
            float d = (t.position - me).sqrMagnitude;
            if (d < bd) { second = best; sd = bd; best = t; bd = d; }
            else if (d < sd) { second = t; sd = d; }
        }
        return skip % 2 == 1 && second != null ? second : best;
    }

    // 커서를 표적으로 옮긴 뒤(0.25초) 한 발
    static IEnumerator AimShot(int n)
    {
        Component p = (Component)Find("PlayerController");
        if (p == null) yield break;
        rec.aimFixed = false;
        rec.aimOverride = AimPick(p.transform.position, n);
        yield return F(rec.aimSpeed + 0.03f);
        yield return Tap();
        yield return F(0.04f);
    }

    static bool Reloading()
    {
        object p = Find("PlayerController");
        return p != null && p.GetType().GetProperty("IsReloading")?.GetValue(p) is bool r && r;
    }

    // from 크기로 시작해 delay 뒤 ramp 초 동안 to 로 (트레일러 곡은 크기 흐름을 곡이 맡아 to = 1)
    static IEnumerator MusicIn(float delay, float ramp, float from = 0f, float to = 0.9f)
    {
        rec.musicVolume = from;
        yield return F(delay);
        int n = Mathf.Max(1, Mathf.RoundToInt(ramp * Fps));
        for (int i = 1; i <= n; i++) { rec.musicVolume = Mathf.Lerp(from, to, i / (float)n); yield return null; }
    }

    // ---------------- 0~4초: 초원 · 거너. 검은 화면 → 십자 커서가 슬라임 위로 → 한 발(음악 없이 총소리만) → 8발 · 빈 탄창 → 저절로 재장전
    static IEnumerator AimOpening()
    {
        rec.manualFire = true;
        rec.gunSfx = true;
        rec.keyHint = true;
        rec.recording = true;
        int start = rec.frames;
        Cue("open");
        // 곡은 처음부터 한 크기(0.85) — 도입이 원래 작게 만들어져 있어 더 줄이면 첫 사격 타이코 · 심장 박동이 묻힘
        SayRec(C_NoAutoAim, 1.5f);
        yield return Fade(1f, 0f, 0.3f);
        yield return AimShot(0);
        yield return UntilRecorded(start, 1.0f);
        Shot("01_aim_meadow");
        yield return UntilRecorded(start, 1.5f);
        SayRec(C_AimReload, 2.5f);
        // 표적마다 커서가 옮겨 다니며 — 탄창이 비면 PlayerController 가 저절로 재장전 (R 키 그림 0.6초)
        for (int n = 1; n < 8 && !Reloading(); n++) yield return AimShot(n);
        yield return UntilRecorded(start, 4.0f);
        rec.manualFire = false;
        rec.keyHint = false;
        rec.aimOverride = null;
    }

    // ---------------- 영웅 1초 컷 (불러오는 동안은 녹화를 멈추고 바로 이어 붙임)
    static IEnumerator HeroCut(string hero, int who, int stage, float seconds, float ultHold)
    {
        rec.recording = false;
        rec.autopilot = false;
        rec.gunSfx = false;
        WearLegend(who);
        yield return Load(hero, stage);
        rec.autopilot = true;
        yield return F(1.0f);                       // 적이 다가올 시간 (녹화 안 함)
        rec.recording = true;
        int start = rec.frames;
        Cue("hero_" + hero, true);
        Hit(0.6f, 0.3f, 0.4f);
        yield return F(seconds * 0.2f);
        yield return Ult(ultHold);
        yield return UntilRecorded(start, seconds);
    }

    // ---------------- 궁수: 시위 → 가득 찬 뒤 0.2초에 놓아 「완벽!」 (CharacterKit.ManualBowInAuto 가 없으면 보통 완충 사격)
    static IEnumerator ArcherCut(float seconds)
    {
        rec.recording = false;
        rec.autopilot = false;
        rec.gunSfx = false;
        WearLegend(3);
        yield return Load("Archer", 1);
        bool manual = SetStatic("CharacterKit", "ManualBowInAuto", true);
        rec.manualFire = manual;
        rec.autopilot = true;
        yield return F(1.0f);
        rec.recording = true;
        int start = rec.frames;
        Cue("hero_Archer", true);
        if (manual)
        {
            object kit = StaticGet("CharacterKit", "Instance");
            In("FireDownFrame", Time.frameCount + 1);
            In("AutoFire", true);
            Sx("bowdraw", 0.4f);                    // 시위를 당김
            bool full = false;
            for (int i = 0; i < Fps * 2; i++)
            {
                if (kit != null && Get(kit, "draw") is float d && d >= 1f) { full = true; break; }
                yield return null;
            }
            if (full) Sx("ding", 0.4f, 1.6f);       // 가득 참 — 지금 놓으라는 신호 (CharacterKit 과 같은 소리)
            yield return F(0.2f);
            In("AutoFire", false);
            In("FireUpFrame", Time.frameCount + 1);
            Sx("chime", 0.45f, 1.8f);               // 「완벽!」 (놓는 소리 · 화살 소리는 평타 감시가 냄)
        }
        yield return UntilRecorded(start, seconds);
        if (manual) SetStatic("CharacterKit", "ManualBowInAuto", false);
        rec.manualFire = false;
    }

    // 결계(보스 필살기)를 띄우고 외침 연출이 끝날 때까지 기다림 (녹화 안 함)
    static IEnumerator RaiseBarrier(object b)
    {
        Component bc = (Component)b;
        Component ult = bc.GetComponent(T("BossUltimate"));
        if (ult == null) ult = bc.gameObject.AddComponent(T("BossUltimate"));
        yield return null;
        ((MonoBehaviour)ult).StartCoroutine((IEnumerator)Call(ult, "Run"));
        yield return F(0.3f);
        PropertyInfo cut = T("BossUltimate").GetProperty("CutscenePlaying", Any);
        if (cut == null) { yield return F(3.3f); yield break; }
        for (int i = 0; i < Fps * 6 && (bool)cut.GetValue(null); i++) yield return null;
        yield return F(0.4f);                       // 결계가 펼쳐지는 순간
    }

    // ---------------- 7~10초: 초원 킹 슬라임 결계 「산성 범람」 → 틈으로 피함 → 처치 슬로모 → 무기가 부서지고 진화 카드 3장
    static IEnumerator BarrierKill(string hero, int who, int stage, int caption)
    {
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        object b = SpawnBoss(stage, 0.8f);
        rec.autopilot = true;
        rec.gunSfx = hero == "Gunner";
        yield return F(0.8f);
        yield return RaiseBarrier(b);
        rec.recording = true;
        int start = rec.frames;
        Cue("barrier_kill", true);
        SxRes("Sounds/domain_open", 0.5f);         // 결계가 펼쳐짐 (BossUltimate.Expand)
        Sx("roar", 0.35f, 0.8f);
        Hit(0.8f, 0.6f, 0.6f);
        SayRec(caption, 3f);
        yield return F(1.2f);
        KillBoss(b);     // 결계가 깨지는 순간
        Cue("barrier_break");
        SxRes("Sounds/domain_break", 0.7f);        // BossUltimate.Shatter
        rec.quietBossDownUntil = rec.frames + 3;   // 같은 순간 보스 폭발 · 울음까지 겹치면 찌그러짐
        Hit(1.5f, 2f, 1f);
        rec.StartCoroutine(SlowMo(0.3f, 0.5f));
        yield return UntilRecorded(start, 1.5f);
        Shot("02_barrier_kill");
        // 무기 진화 3택: 보스를 쓰러뜨리면 StageManager 가 띄움 · 안 뜨면 직접
        object evo = null;
        for (int i = 0; i < Fps && evo == null; i++) { evo = Find("WeaponEvolutionUI"); if (evo == null) yield return null; }
        rec.autopilot = false;
        StopInput();
        if (evo == null)
        {
            object sm = Find("StageManager");
            if (sm != null) ((MonoBehaviour)sm).StartCoroutine((IEnumerator)Call(sm, "Evolution", false));
            yield return F(0.5f);
            evo = Find("WeaponEvolutionUI");
        }
        yield return UntilRecorded(start, 2.5f);
        if (evo != null) { Call(evo, "Select", 0); Sx("ding", 0.4f); }          // WeaponEvolutionUI.Select
        yield return UntilRecorded(start, 3.0f);
        if (evo != null)
        {
            Set(evo, "confirmed", true);
            Cue("weapon_evolve_confirm");
            Sx("railgun", 0.45f, 0.8f);            // 고른 카드가 가운데로 모이고 빛이 터짐
            Sx("shimmer", 0.45f);
        }
        rec.gunSfx = false;
    }

    // ---------------- 10~13초: 리치 왕 주문 중 · 커서가 지팡이 끝 빈틈을 노려 「빈틈!」 (WeakSpot 이 없으면 건너뜀)
    static IEnumerator WeakCut(int caption)
    {
        Type ws = T("WeakSpot");
        if (ws == null) { Debug.Log("[CAP] WeakSpot 없음 — 빈틈 컷 건너뜀"); yield break; }
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(0);
        yield return Load("Gunner", 0);
        object b = SpawnBoss(0, 0.8f);
        rec.autopilot = true;
        rec.gunSfx = true;
        FieldInfo casting = b.GetType().GetField("casting", Any);
        // 리치 왕이 주문을 시작할 때까지 (녹화 안 함 · 최대 4초)
        for (int i = 0; i < Fps * 4 && casting != null && !(bool)casting.GetValue(b); i++) yield return null;
        Component spot = ((Component)b).GetComponent(ws);
        if (spot == null || !WeakCenter(spot, out Vector3 at)) { rec.gunSfx = false; yield break; }
        rec.manualFire = true;
        rec.aimFixed = true;
        rec.aimPoint = at;
        rec.recording = true;
        rec.weakSfx = true;
        int start = rec.frames;
        Cue("weak", true);
        SayRec(caption, 2.8f);
        for (int f = 0; rec.frames - start < Fps * 2.6f; f++)
        {
            if (WeakCenter(spot, out Vector3 now)) rec.aimPoint = now;
            if (f >= Mathf.RoundToInt(rec.aimSpeed * Fps) && f % 6 == 0) rec.StartCoroutine(Tap());     // 커서가 닿은 뒤 0.2초마다 한 발
            if (f == Fps * 3 / 2) Shot("03_weak_point");
            yield return null;
        }
        rec.aimFixed = false;
        rec.manualFire = false;
        rec.gunSfx = false;
        rec.weakSfx = false;
    }

    static Vector3 ToV3(object v) => v is Vector2 v2 ? (Vector3)v2 : v is Vector3 v3 ? v3 : Vector3.zero;

    // 빈틈 중심 (WeakSpot.Center(int) 함수 · 속성 어느 쪽이든 · 첫 빈틈)
    static bool WeakCenter(Component spot, out Vector3 at)
    {
        at = Vector3.zero;
        if (spot == null) return false;
        Type t = spot.GetType();
        MethodInfo m = t.GetMethod("Center", Any, null, new[] { typeof(int) }, null);
        if (m != null) { at = ToV3(m.Invoke(spot, new object[] { 0 })); return true; }
        PropertyInfo p = t.GetProperty("Center", Any);
        if (p != null) { at = ToV3(p.GetValue(spot)); return true; }
        return false;
    }

    // ---------------- 13~16초: 지옥의 군주가 탄환을 기억해 방패 → 필살기로 깸 (BossMemory 가 없으면 건너뜀)
    static IEnumerator MemoryCut(int caption)
    {
        if (T("BossMemory") == null) { Debug.Log("[CAP] BossMemory 없음 — 기억 컷 건너뜀"); yield break; }
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(0);
        yield return Load("Gunner", 1);
        object b = SpawnBoss(1, 0.62f);
        // SpawnBoss 가 넣은 체력은 bosss.Start 가 가득으로 다시 채움 → Start 뒤에 66%(날개) 아래 · 50%(기억) 위로
        yield return null;
        yield return null;
        float max = Convert.ToSingle(Get(b, "setEnemyHP"));
        Set(b, "EnemyHealth", max * 0.62f);
        // 날개 단계(66%) 전환이 끝날 때까지 기다림 (녹화 · 사격 안 함 · 최대 5초) — 기억 컷 3초 안에 날개 연출 · 빈틈 자리 이동 · 알림이 겹치지 않게
        Component sk = ((Component)b).GetComponent(T("BossSkills"));
        FieldInfo phase = sk != null ? sk.GetType().GetField("phase", Any) : null;
        FieldInfo casting = b.GetType().GetField("casting", Any);
        for (int i = 0; i < Fps * 5; i++)
        {
            bool winged = phase == null || Convert.ToInt32(phase.GetValue(sk)) >= 1;
            bool busy = casting != null && (bool)casting.GetValue(b);
            if (winged && !busy && i >= Fps / 2) break;
            yield return null;
        }
        rec.autopilot = true;
        rec.gunSfx = true;
        yield return F(1.0f);                       // 평타(탄환)를 쌓아 「가장 많이 맞은 공격」으로 (녹화 안 함)
        Set(b, "EnemyHealth", max * 0.505f);        // 지옥의 군주 기억 체력 50% 바로 위
        rec.recording = true;
        int start = rec.frames;
        Cue("memory", true);
        SayRec(caption, 3f);
        yield return F(0.3f);
        Call(b, "TakeDamage", max * 0.012f, 0f, Vector3.zero);     // 50% 를 넘음 → 기억 발동
        yield return UntilRecorded(start, 1.5f);
        Shot("04_boss_memory");
        yield return F(0.3f);
        yield return Ult(0.5f);                     // 기억하지 않은 공격으로 깸
        yield return UntilRecorded(start, 3.0f);
        rec.gunSfx = false;
    }

    // ---------------- 21~27초: 네 세계 1.5초씩 — 진화 무기 필살기로 무리 쓸기
    static IEnumerator WorldCut(string hero, int who, int stage, int evolve, float seconds)
    {
        rec.recording = false;
        rec.autopilot = false;
        rec.gunSfx = false;
        WearLegend(who);
        yield return Load(hero, stage);
        Evolve(evolve);
        Swarm(stage, 40);
        Swarm(stage, 40);
        rec.autopilot = true;
        yield return F(1.0f);
        rec.baseZoom = 0.92f;
        rec.recording = true;
        int start = rec.frames;
        Cue("world_" + stage, true);
        Hit(0.8f, 0.4f, 0.5f);
        yield return F(0.2f);
        yield return Ult(0.3f);
        Hit(1.1f, 1.2f, 0.4f);
        rec.StartCoroutine(SlowMo(0.4f, 0.6f));
        yield return UntilRecorded(start, seconds);
        rec.baseZoom = 1f;
    }

    // ---------------- 27~31초: 개발자 등장 (무한 모드 판에서만 · EndlessMode.SpawnFinalBoss 는 private → 리플렉션)
    static IEnumerator DeveloperCut(int caption)
    {
        Type dt = T("Difficulty");
        FieldInfo ov = T("GameMode")?.GetField("Override", Any);
        if (dt == null || ov == null) yield break;
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(0);
        ov.SetValue(null, Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(dt), Enum.Parse(dt, "Endless")));
        yield return Load("Gunner", 0);
        object em = null;
        for (int i = 0; i < 10 && em == null; i++) { em = StaticGet("EndlessMode", "Instance"); if (em == null) yield return null; }
        object dev = null;
        try { dev = em != null ? Call(em, "SpawnFinalBoss", false) : null; } catch (Exception e) { Debug.Log("[CAP] 개발자 소환 실패 " + e.Message); }
        if (dev == null) { ov.SetValue(null, null); yield break; }
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        ((Component)dev).transform.position = me + new Vector3(7f, 3f, 0f);
        object bar = Find("bossbar");
        if (bar != null) bar.GetType().GetField("bossSpawn").SetValue(bar, true);
        rec.autopilot = true;
        rec.gunSfx = true;
        yield return F(1.0f);
        rec.recording = true;
        int start = rec.frames;
        Cue("developer", true);
        Sx("roar", 0.45f, 0.95f);                   // 개발자 등장
        Sx("bell", 0.35f, 0.6f);
        SayRec(caption, 3.6f);
        yield return F(1.4f);
        yield return Ult(0.8f);
        yield return UntilRecorded(start, 4.0f);
        rec.gunSfx = false;
        ov.SetValue(null, null);                    // 다음 장면부터 원래 판
    }

    // ---------------- 31~36초: 스킬 진화 카드 → 발동
    static IEnumerator SkillEvoCut(int caption)
    {
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(0);
        yield return Load("Gunner", 2);
        rec.autopilot = true;
        yield return F(0.6f);
        rec.autopilot = false;
        StopInput();
        rec.recording = true;
        Cue("skill_evo", true);
        yield return SkillEvolution(caption);
    }

    // ---------------- 36~40초: 영혼 트리 2초 → 결계 깨짐 슬로모
    static IEnumerator TreeCut(int caption)
    {
        object sm = Find("StageManager");
        if (sm == null) yield break;
        rec.autopilot = false;
        StopInput();
        T("SoulShards").GetMethod("Add").Invoke(null, new object[] { 4000, Vector3.zero, false });
        Call(sm, "OpenSoulTree");
        rec.recording = true;
        Cue("tree", true);
        Sx("shimmer", 0.45f, 1.2f);                 // SoulTreeUI 열림
        SayRec(caption, 2.2f);
        yield return F(0.4f);
        object specials = Get(sm, "specials");
        object tree = T("SoulTreeUI").GetProperty("Instance").GetValue(null);
        for (int i = 0; i < 6 && tree != null; i++)
        {
            object pick = null;
            foreach (object v in (IList)Get(tree, "views"))
            {
                object n = Get(v, "node");
                if (!(bool)Call(specials, "CanBuy", n)) continue;
                if (pick == null) pick = v;
                if (Convert.ToInt32(Get(n, "branch")) == i % 6) { pick = v; break; }
            }
            if (pick != null) { Call(tree, "Buy", pick); Sx("chime", 0.35f, 1f + 0.08f * i); Sx("sparkle", 0.3f, 1f + 0.05f * i); }
            yield return F(0.26f);
        }
        T("SoulTreeUI").GetMethod("Close").Invoke(null, null);
        yield return F(0.1f);
    }

    static IEnumerator BarrierShatter(string hero, int who, int stage, int evolve)
    {
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        Evolve(evolve);
        object b = SpawnBoss(stage, 0.8f);
        rec.autopilot = true;
        yield return F(0.8f);
        yield return RaiseBarrier(b);
        rec.baseZoom = 0.95f;
        rec.recording = true;
        Cue("barrier_shatter", true);              // domain_open 은 넣지 않음 (4.2초 꼬리가 끝 카드 앞 정적 · 로고까지 덮음)
        Hit(1f, 1f, 1f);
        yield return F(0.7f);
        KillBoss(b);
        Cue("barrier_break_final");
        SxRes("Sounds/domain_break", 0.75f);
        rec.quietBossDownUntil = rec.frames + 3;
        Hit(1.5f, 2f, 1f);
        rec.StartCoroutine(SlowMo(0.25f, 1.3f));
        yield return F(1.4f);
        rec.baseZoom = 1f;
    }

    // ================================================================= 끝 카드 (2026-10-18~ 자막 없이 · 진짜 게임 트레일러처럼)
    // 검은 화면에 영혼 불씨가 떠오름 → 가운데서 가로 빛줄기가 번짐 → 「쾅」 흰 번쩍임과 함께 도트 로고(steam_assets/library_logo.png)가
    // 크게 박혔다가 제자리로 · 충격파 고리 · 뒤에서 청록 · 금빛 후광 → 로고 위를 비스듬한 빛이 한 번 훑고 → 천천히 밀어 들어가며 소리 · 화면이 잦아듦
    static Texture2D Radial(int size, bool ring)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[size * size];
        float h = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - h) * (x - h) + (y - h) * (y - h)) / h;     // 0 가운데 · 1 가장자리
                float a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) / 0.08f) : Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    static T Ui<T>(string name, Transform parent, Vector2 size, Vector2 pos) where T : Graphic
    {
        GameObject g = new GameObject(name, typeof(RectTransform), typeof(T));
        g.layer = 5;
        g.transform.SetParent(parent, false);
        T gr = g.GetComponent<T>();
        gr.raycastTarget = false;
        gr.rectTransform.sizeDelta = size;
        gr.rectTransform.anchoredPosition = pos;
        return gr;
    }

    static float BackOut(float x) { const float c = 1.9f; x -= 1f; return 1f + (c + 1f) * x * x * x + c * x * x; }

    static IEnumerator EndCard()
    {
        Transform root = rec.captionCanvas.transform;
        rec.logo.alpha = rec.sub.alpha = rec.caption.alpha = 0f;
        Texture2D logoTex = null;
        string logoPath = Environment.GetEnvironmentVariable("CAPTURE_LOGO");
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
        {
            logoTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            logoTex.LoadImage(File.ReadAllBytes(logoPath));
            logoTex.filterMode = FilterMode.Point;                         // 도트가 뭉개지지 않게
            logoTex.wrapMode = TextureWrapMode.Clamp;
        }
        if (logoTex == null)                                                // 로고 그림이 없으면 예전처럼 글자 로고
        {
            for (int i = 0; i <= Fps; i++) { float k = Mathf.SmoothStep(0f, 1f, i / (float)Fps); rec.logo.alpha = k; rec.logo.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, k); yield return null; }
            yield return F(3f);
            yield break;
        }

        Texture2D soft = Radial(256, false), ringTex = Radial(512, true);
        GameObject card = new GameObject("EndCard", typeof(RectTransform));
        card.layer = 5;
        card.transform.SetParent(root, false);
        card.transform.SetSiblingIndex(rec.black.transform.GetSiblingIndex() + 1);   // 검은 막 바로 위 · 흰 번쩍임 아래
        RectTransform cardRt = (RectTransform)card.transform;
        cardRt.anchorMin = Vector2.zero; cardRt.anchorMax = Vector2.one; cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;

        // 바닥 안개 (아래쪽에서 올라오는 보랏빛)
        RawImage fog = Ui<RawImage>("Fog", card.transform, new Vector2(2600f, 900f), new Vector2(0f, -620f));
        fog.texture = soft; fog.color = new Color(0.36f, 0.2f, 0.62f, 0f);
        // 후광 (청록 위 · 금빛 아래 — 로고의 두 줄 색)
        Vector2 logoPos = new Vector2(0f, 30f), logoSize = new Vector2(1180f, 664f);
        RawImage haloC = Ui<RawImage>("HaloCyan", card.transform, new Vector2(1500f, 900f), logoPos + new Vector2(0f, 120f));
        haloC.texture = soft; haloC.color = new Color(0.35f, 0.85f, 1f, 0f);
        RawImage haloG = Ui<RawImage>("HaloGold", card.transform, new Vector2(1600f, 900f), logoPos + new Vector2(0f, -140f));
        haloG.texture = soft; haloG.color = new Color(1f, 0.72f, 0.3f, 0f);
        // 충격파 고리
        RawImage ring = Ui<RawImage>("Shock", card.transform, new Vector2(200f, 200f), logoPos);
        ring.texture = ringTex; ring.color = new Color(0.85f, 0.95f, 1f, 0f);
        // 가로 빛줄기 (가운데서 양옆으로 번짐)
        RawImage streak = Ui<RawImage>("Streak", card.transform, new Vector2(10f, 14f), logoPos);
        streak.texture = soft; streak.color = new Color(0.9f, 0.97f, 1f, 0f);
        RawImage streakCore = Ui<RawImage>("StreakCore", card.transform, new Vector2(10f, 3f), logoPos);
        streakCore.color = new Color(1f, 1f, 1f, 0f);

        // 영혼 불씨 (도트 네모 · 청록과 금빛 · 아래에서 위로 흔들리며)
        const int EmberN = 70;
        RawImage[] emb = new RawImage[EmberN];
        Vector2[] ePos = new Vector2[EmberN];
        float[] eSpd = new float[EmberN], ePh = new float[EmberN];
        var rnd = new System.Random(1018);
        for (int i = 0; i < EmberN; i++)
        {
            float sz = 4f + 4f * rnd.Next(0, 3);                            // 4 · 8 · 12px (도트 느낌)
            emb[i] = Ui<RawImage>("Ember", card.transform, new Vector2(sz, sz), Vector2.zero);
            bool gold = rnd.NextDouble() < 0.45;
            emb[i].color = gold ? new Color(1f, 0.78f, 0.38f, 0f) : new Color(0.5f, 0.9f, 1f, 0f);
            ePos[i] = new Vector2((float)(rnd.NextDouble() * 2000.0 - 1000.0), (float)(rnd.NextDouble() * 1200.0 - 640.0));
            eSpd[i] = 40f + (float)rnd.NextDouble() * 110f;
            ePh[i] = (float)(rnd.NextDouble() * Math.PI * 2.0);
        }

        // 로고 + 박히는 순간 하얗게 탄 로고(실루엣) + 빛 훑기 — 둘 다 로고 알파를 그대로 따르는 작은 텍스처로 (UI Mask 는 투명 부분까지 네모로 칠해져서 쓰지 않음)
        RawImage logoImg = Ui<RawImage>("LogoArt", card.transform, logoSize, logoPos);
        logoImg.texture = logoTex; logoImg.color = new Color(1f, 1f, 1f, 0f);
        const int SW = 320, SH = 180;
        byte[] logoA = new byte[SW * SH];
        Color32[] lp = logoTex.GetPixels32();
        for (int y = 0; y < SH; y++)
            for (int x = 0; x < SW; x++)
                logoA[y * SW + x] = lp[(y * logoTex.height / SH) * logoTex.width + (x * logoTex.width / SW)].a;
        Texture2D silTex = new Texture2D(SW, SH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        Texture2D shineTex = new Texture2D(SW, SH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        Color32[] sp = new Color32[SW * SH];
        for (int i = 0; i < sp.Length; i++) sp[i] = new Color32(255, 255, 255, logoA[i]);
        silTex.SetPixels32(sp); silTex.Apply();
        RawImage burn = Ui<RawImage>("LogoBurn", logoImg.transform, logoSize, Vector2.zero);
        burn.texture = silTex; burn.color = new Color(1f, 1f, 1f, 0f);
        RawImage sweep = Ui<RawImage>("LogoShine", logoImg.transform, logoSize, Vector2.zero);
        sweep.texture = shineTex; sweep.color = new Color(1f, 1f, 1f, 0f);
        float lastSw = -1f;

        AudioClip boom = Resources.Load<AudioClip>("Sounds/domain_break");
        int total = Mathf.RoundToInt(5.2f * Fps), slam = Mathf.RoundToInt(0.75f * Fps);
        for (int f = 0; f < total; f++)
        {
            float t = f / (float)Fps, ts = (f - slam) / (float)Fps;       // ts: 로고가 박힌 뒤 흐른 시간
            float fadeOut = 1f - Mathf.Clamp01((t - 4.6f) / 0.6f);
            rec.black.color = Color.black;
            // 불씨: 처음부터 천천히 떠오름 (박힐 때 잠깐 바깥으로 밀림)
            float push = ts >= 0f ? Mathf.Exp(-ts * 5f) * 260f * Mathf.Clamp01(ts * 6f) : 0f;
            for (int i = 0; i < EmberN; i++)
            {
                Vector2 p = ePos[i] + new Vector2(Mathf.Sin(t * 1.3f + ePh[i]) * 18f, eSpd[i] * t);
                p.y = Mathf.Repeat(p.y + 640f, 1280f) - 640f;
                p += p.normalized * push;
                emb[i].rectTransform.anchoredPosition = new Vector2(Mathf.Round(p.x / 4f) * 4f, Mathf.Round(p.y / 4f) * 4f);   // 4px 격자 (도트)
                float tw = 0.55f + 0.45f * Mathf.Sin(t * 3.1f + ePh[i] * 2f);
                Color c = emb[i].color; c.a = Mathf.Clamp01(t / 0.6f) * tw * (ts >= 0f ? 0.9f : 0.5f) * fadeOut;
                emb[i].color = c;
            }
            fog.color = new Color(fog.color.r, fog.color.g, fog.color.b, 0.28f * Mathf.Clamp01(t / 1f) * fadeOut);

            if (ts < 0f)
            {
                // 박히기 전: 빛줄기가 가운데서 양옆으로 번지며 밝아짐
                float k = Mathf.Clamp01(t / (slam / (float)Fps));
                float w = Mathf.Lerp(10f, 2400f, k * k);
                streak.rectTransform.sizeDelta = new Vector2(w, Mathf.Lerp(14f, 60f, k));
                streakCore.rectTransform.sizeDelta = new Vector2(w * 0.8f, Mathf.Lerp(2f, 6f, k));
                streak.color = new Color(0.9f, 0.97f, 1f, 0.9f * k);
                streakCore.color = new Color(1f, 1f, 1f, k);
            }
            else
            {
                if (f == slam)
                {
                    Cue("logo");
                    if (boom != null) rec.Sfx(boom, 0.4f);      // 곡에도 LOGO 충격음 · 타이코 · 금관이 있어 0.8 → 0.4 (합이 커지면 SoftClip 에 뭉개짐)
                }
                if (f == slam + Mathf.RoundToInt(0.9f * Fps)) Sx("shimmer", 0.3f, 1.2f);      // 빛 훑기
                // 흰 번쩍임 (0.18초) · 빛줄기는 금방 사라짐
                rec.flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - ts / 0.18f) * 0.85f);
                float sk = Mathf.Clamp01(1f - ts / 0.25f);
                streak.color = new Color(0.9f, 0.97f, 1f, 0.9f * sk);
                streakCore.color = new Color(1f, 1f, 1f, sk);
                streak.rectTransform.sizeDelta = new Vector2(2400f, Mathf.Lerp(4f, 60f, sk));
                // 로고: 1.45배 → 1배로 튕기듯 · 박힌 직후 흔들림 · 이후 아주 천천히 밀어 들어감
                float pop = Mathf.Clamp01(ts / 0.38f);
                float scale = Mathf.Lerp(1.45f, 1f, BackOut(pop)) * (1f + 0.035f * Mathf.Clamp01((ts - 0.4f) / 4f));
                float shakeAmp = 22f * Mathf.Exp(-ts * 9f);
                Vector2 jitter = new Vector2(Mathf.Sin(ts * 91f) * shakeAmp, Mathf.Cos(ts * 77f) * shakeAmp * 0.6f);
                logoImg.rectTransform.anchoredPosition = logoPos + jitter;
                logoImg.rectTransform.localScale = Vector3.one * scale;
                logoImg.color = new Color(1f, 1f, 1f, Mathf.Clamp01(ts / 0.06f) * fadeOut);
                burn.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - ts / 0.32f) * 0.9f * fadeOut);
                // 후광: 박힐 때 크게 번쩍였다가 숨 쉬듯 잔잔하게
                float breathe = 0.5f + 0.5f * Mathf.Sin(ts * 2.2f);
                float haloA = (0.55f * Mathf.Exp(-ts * 3f) + 0.22f + 0.06f * breathe) * fadeOut;
                haloC.color = new Color(haloC.color.r, haloC.color.g, haloC.color.b, haloA);
                haloG.color = new Color(haloG.color.r, haloG.color.g, haloG.color.b, haloA * 0.9f);
                haloC.rectTransform.localScale = haloG.rectTransform.localScale = Vector3.one * (1f + 0.25f * Mathf.Exp(-ts * 4f));
                // 충격파 고리: 빠르게 퍼지며 옅어짐
                float rk = Mathf.Clamp01(ts / 0.7f);
                float rs = Mathf.Lerp(300f, 3200f, 1f - (1f - rk) * (1f - rk));
                ring.rectTransform.sizeDelta = new Vector2(rs, rs * 0.62f);
                ring.color = new Color(0.85f, 0.95f, 1f, 0.75f * (1f - rk));
                // 빛 훑기: 박히고 0.9초 뒤 왼쪽에서 오른쪽으로 0.7초 (비스듬한 띠 · 로고 안에서만)
                float sw = Mathf.Clamp01((ts - 0.9f) / 0.7f);
                if (sw > 0f && sw < 1f && sw != lastSw)
                {
                    lastSw = sw;
                    float pos = Mathf.Lerp(-60f, SW + 60f, Mathf.SmoothStep(0f, 1f, sw));
                    for (int y = 0; y < SH; y++)
                        for (int x = 0; x < SW; x++)
                        {
                            float d = (x + y * 0.45f) - pos;                       // 비스듬히 (오른쪽 위로 기운 띠)
                            float band = Mathf.Exp(-d * d / 180f);
                            sp[y * SW + x] = new Color32(255, 255, 255, (byte)(logoA[y * SW + x] * band * 0.8f));
                        }
                    shineTex.SetPixels32(sp); shineTex.Apply();
                }
                sweep.color = new Color(1f, 1f, 1f, (sw > 0f && sw < 1f ? 1f : 0f) * fadeOut);
                // 끝 무렵: 곡이 스스로 잦아들므로 코드는 화면과 함께 마지막 0.6초만 닫음 (예전 3.4초부터 1.8초 페이드는 곡과 겹쳐 두 번 줄어듦)
                rec.musicVolume = 0.85f * fadeOut;
            }
            yield return null;
        }
        rec.flash.color = new Color(1f, 1f, 1f, 0f);
        UnityEngine.Object.Destroy(card);
    }

    [UnityTest]
    public IEnumerator Capture()
    {
        LogAssert.ignoreFailingMessages = true;
        Directory.CreateDirectory(Out);
        Time.captureFramerate = Fps;
        In("Auto", true);                          // 업적 · 누적 기록이 풀리지 않게
        AudioListener.volume = 0f;                 // 촬영하는 동안 스피커로 소리를 내지 않음 (영상 음악 · 효과음은 아래에서 파일로 직접 넣음)
        SetGameLanguage(Lang);
        GameObject host = new GameObject("CaptureRec");
        UnityEngine.Object.DontDestroyOnLoad(host);
        rec = host.AddComponent<Rec>();
        BuildOverlay();
        rec.showCursor = true;                     // 트레일러만 커서를 그림 (하이라이트 · 레딧은 숨김)
        rec.humanMove = true;                      // 트레일러만 사람 같은 움직임 (원거리 영웅은 잡몹 6~9칸 · 보스 8~11칸 거리 카이팅, 검사는 예전처럼 붙어 베기)
        rec.sub.text = NoCaptions ? "" : C(C_EndTag) + "\n" + C(C_FreeDemo);
        rec.sub.rectTransform.sizeDelta = new Vector2(1800f, 160f);
        rec.sub.rectTransform.anchoredPosition = new Vector2(0f, -190f);

        // ---------------- 0~4초) 초원 · 거너: 자동 조준 없음 → 조준 · 장전
        yield return Load("Gunner", 2);
        rec.autopilot = true;
        rec.manualFire = true;
        // 커서는 빈 땅에서 시작해 첫 슬라임 위로 미끄러져 감
        rec.aimFixed = true;
        rec.aimPoint = ((Component)Find("PlayerController")).transform.position + new Vector3(-3f, -1.5f, 0f);
        yield return F(1.2f);
        rec.musicPath = "TrailerMusic";             // 2026-10-18~ 직접 합성한 긴박 · 웅장한 트레일러 곡 (make_trailer_music.py · 콘티 장면 전환에 충격음)
        rec.rich = true;                            // 인게임 효과음을 다채롭게 (처치 · 타격 · 평타 · 필살기 · 결계 · 진화 · 장면 전환)
        rec.Begin(Path.Combine(Out, "SoulSaver_Trailer_" + Lang + ".mp4"));
        rec.musicVolume = 0.85f;                    // 곡이 이미 최대 0.93 으로 눌려 있어 1.0 은 효과음 얹을 여유가 없음
        yield return AimOpening();

        // ---------------- 4~7초) 영웅 9명, 9개의 손 — 유령 빙의 · 시계공 되감기 · 궁수 「완벽!」
        SayRec(C_Heroes9, 3f);
        yield return HeroCut("Ghost", 8, 2, 1f, 0.4f);
        yield return HeroCut("Clockmaker", 6, 0, 1f, 0.3f);
        yield return ArcherCut(1f);

        // ---------------- 7~10초) 킹 슬라임 결계 → 처치 → 그 자리에서 진화
        yield return BarrierKill("Gunner", 0, 2, C_EvolveOnSpot);

        // ---------------- 10~16초) 빈틈 · 기억 (갈래 A · B 가 없으면 건너뜀)
        yield return WeakCut(C_WeakPoint);
        yield return MemoryCut(C_Remembers);

        // ---------------- 16~21초) 거울의 군주가 내 필살기를 되쏨
        yield return Mirror("Swordsman", 1, C_MirrorsBuild, true);

        // ---------------- 21~27초) 네 세계 · 네 왕 (진화 무기 필살기로 무리 쓸기)
        SayRec(C_Worlds4, 5.6f);
        yield return WorldCut("Gunner", 0, 2, 2, 1.5f);                                // 초원 · 쌍권총
        yield return WorldCut("Swordsman", 1, 0, KitWeapon("Swordsman", 0), 1.5f);    // 지하 묘역
        yield return WorldCut("Archer", 3, 1, KitWeapon("Archer", 1), 1.5f);          // 불타는 지옥
        yield return WorldCut("Gunner", 0, 3, 5, 1.5f);                                // 영혼의 심연 · 뇌운

        // ---------------- 27~31초) …그리고 개발자
        yield return DeveloperCut(C_Developer);

        // ---------------- 31~36초) 스킬 진화 37가지
        yield return SkillEvoCut(C_SkillEvo37);

        // ---------------- 36~40초) 100칸이 넘는 영혼 트리 → 결계 깨짐 슬로모
        yield return TreeCut(C_SoulTree100);
        yield return BarrierShatter("Alchemist", 4, 1, KitWeapon("Alchemist", 1));
        T("CharacterData").GetField("Override", Any).SetValue(null, null);

        // ---------------- 40~45초) 끝 카드 — 도트 로고가 빛줄기 · 번쩍임 · 충격파와 함께 박힘 (EndCard)
        rec.autopilot = false;
        StopInput();
        yield return Fade(0f, 1f, 0.4f);
        // 끝 카드는 늘 같은 녹화 프레임에서 시작 (곡의 로고 「쾅」 · 정적과 맞춤 — 앞 장면 길이가 몇 프레임 흔들려도 검은 화면으로 기다림)
        yield return UntilRecorded(0, EndcardFrame / (float)Fps);
        rec.Hush();                                 // 곡이 비워 둔 정적 — 결계 소리 꼬리를 끊음
        Cue("endcard");                             // 휘익 없이 (곡이 가장 조용한 순간)
        yield return EndCard();

        rec.End();
        Time.timeScale = 1f;
        foreach (string k in skinKeys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
        Time.captureFramerate = 0;
        In("Auto", false);
        Debug.Log("[CAP] done frames=" + rec.frames + " (" + (rec.frames / (float)Fps).ToString("0.0") + "s)");
        yield return null;
    }

    // ================================================================= 하이라이트 영상 (2.2.1~, record.ps1 -Mode highlight)
    // 자막 없이 긴박한 음악 + 펀치 줌 · 흔들림 · 번쩍임 · 슬로 모션, 가장 화려하게 터지는 장면만 빠르게 이어 붙임
    // 결계를 깨는 마지막 한 방 (2026-10-18~): 예전엔 TakeDamage(9999999) 라 머리 위에 「8000000」(× PlayerDamageMul 0.8) 거대 숫자가 떴음
    //   → 남은 체력을 게임 배율(PlayerDamageMul · damageTakenMul · erosionMul · memoryTakenMul)로 되짚어 「남은 체력 + 조금」만 줌 (실제 막타처럼 보이는 숫자)
    //   기억 방패 등으로 덜 들어가 살아남으면 체력을 거의 0 으로 두고 같은 한 방을 한 번 더 (숫자는 여전히 그럴듯한 크기)
    static void KillBoss(object b)
    {
        float hp = Mathf.Max(1f, Get(b, "EnemyHealth") is float h ? h : 1f);
        float k = StaticGet("GameMode", "PlayerDamageMul") is float pm ? pm : 1f;
        foreach (string f in new[] { "damageTakenMul", "erosionMul", "memoryTakenMul" })
            if (Get(b, f) is float m && m > 0.01f) k *= m;
        float d = Mathf.Ceil((hp * 1.03f + 1f) / Mathf.Max(0.05f, k));
        Call(b, "TakeDamage", d, 0f, Vector3.zero);
        if (Get(b, "isDead") is bool dead && !dead)
        {
            Set(b, "EnemyHealth", 0.01f);
            Call(b, "TakeDamage", d, 0f, Vector3.zero);
        }
    }

    static void Hit(float p, float s, float f) { rec.punch = Mathf.Max(rec.punch, p); rec.shake = Mathf.Max(rec.shake, s); rec.flashAmt = Mathf.Max(rec.flashAmt, f); }

    // 슬로 모션 (게임 시간만 느려지고 음악은 그대로)
    static IEnumerator SlowMo(float scale, float seconds)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Fps));
        for (int i = 0; i < n; i++) { Time.timeScale = Mathf.Lerp(scale, 1f, Mathf.Pow(i / (float)n, 3f)); yield return null; }
        Time.timeScale = 1f;
    }

    static void Evolve(int id)
    {
        if (id < 0) return;
        object sp = Get(Find("StageManager"), "specials");
        if (sp != null) Call(sp, "EvolveWeapon", id);
    }

    // 그 영웅의 n번째 전용 무기 (무기 진화 형태)
    static int KitWeapon(string hero, int nth)
    {
        Type sa = T("SpecialAbilities");
        object def = T("CharacterData").GetMethod("Def").Invoke(null, new[] { Enum.Parse(T("CharacterId"), hero) });
        int k = 0;
        foreach (int id in (int[])Get(def, "pool"))
            if ((bool)sa.GetMethod("KitIsWeapon").Invoke(null, new object[] { id }) && k++ == nth) return id;
        return -1;
    }

    // 빽빽한 적 무리 한가운데서 진화 무기 필살기 → 한꺼번에 쓸려 나감
    static IEnumerator Blast(string hero, int who, int stage, int evolve, int swarm, float lead, float hold, float zoom)
    {
        Debug.Log("[CAP] blast " + hero + " stage " + stage + " evolve " + evolve + " frames " + (rec != null ? rec.frames : 0));
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        Evolve(evolve);
        Swarm(stage, swarm);
        Swarm(stage, swarm / 2);
        rec.autopilot = true;
        yield return F(1.1f);                       // 적이 몰려올 시간 (녹화 안 함)
        rec.baseZoom = zoom;
        rec.recording = true;
        Hit(1f, 0.6f, 0.9f);                       // 장면이 바뀔 때마다 번쩍 · 쿵
        yield return F(lead);
        yield return Ult(hold);
        Hit(1.2f, 1.4f, 0.5f);
        rec.StartCoroutine(SlowMo(0.3f, 1.1f));
        yield return F(1.5f);
        Hit(0.6f, 0.8f, 0f);
        yield return F(0.7f);
    }

    // 보스 결계가 펼쳐지고 → 보스를 쓰러뜨려 결계가 산산조각
    static IEnumerator BarrierBreak(string hero, int who, int stage, int evolve)
    {
        Debug.Log("[CAP] barrier " + hero + " stage " + stage + " frames " + (rec != null ? rec.frames : 0));
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        Evolve(evolve);
        object b = SpawnBoss(stage, 0.8f);
        rec.autopilot = true;
        yield return F(0.8f);
        Component bc = (Component)b;
        Component ult = bc.GetComponent(T("BossUltimate"));
        if (ult == null) ult = bc.gameObject.AddComponent(T("BossUltimate"));
        yield return null;
        ((MonoBehaviour)ult).StartCoroutine((IEnumerator)Call(ult, "Run"));
        yield return F(3.6f);                       // 외침 화면(글자)은 찍지 않음
        rec.baseZoom = 0.95f;
        rec.recording = true;
        Hit(1f, 1f, 1f);
        yield return F(2.6f);                       // 결계 안에서 쏟아지는 공격
        yield return Ult(0.3f);
        yield return F(0.5f);
        KillBoss(b);     // 결계가 깨지는 순간
        Hit(1.5f, 2f, 1f);
        rec.StartCoroutine(SlowMo(0.25f, 1.6f));
        yield return F(2.2f);
    }

    [UnityTest]
    public IEnumerator HighlightReel()
    {
        Debug.Log("[CAP] highlight start");
        LogAssert.ignoreFailingMessages = true;
        Directory.CreateDirectory(Out);
        Time.captureFramerate = Fps;
        In("Auto", true);
        AudioListener.volume = 0f;
        SetGameLanguage(Lang);
        GameObject host = new GameObject("CaptureRec");
        UnityEngine.Object.DontDestroyOnLoad(host);
        rec = host.AddComponent<Rec>();
        rec.musicPath = "TrailerMusic";             // 직접 합성한 웅장한 트레일러 음악 (make_trailer_music.py, 장면 전환 · 필살기 · 결계 파괴에 충격음을 맞춤)
        rec.musicVolume = 1f;
        BuildOverlay();
        rec.sub.text = "";
        rec.black.color = new Color(0f, 0f, 0f, 0f);

        yield return Load("Gunner", 1);
        Debug.Log("[CAP] loaded");
        rec.Begin(Path.Combine(Out, "SoulSaver_Highlight.mp4"));

        int dual = 2, shotgun = 0, chain = 5, grenade = 7;      // SpecialAbilities: DualId · ShotgunId · ChainId · GrenadeId
        yield return Blast("Gunner", 0, 1, dual, 34, 0.5f, 0.05f, 0.9f);            // 총알 폭풍
        yield return Blast("Swordsman", 1, 2, KitWeapon("Swordsman", 0), 30, 0.6f, 0.6f, 0.85f);
        yield return Blast("Alchemist", 4, 0, KitWeapon("Alchemist", 1), 30, 0.6f, 0.5f, 0.9f);
        yield return BarrierBreak("Archer", 3, 1, KitWeapon("Archer", 0));          // 지옥의 군주 결계 파괴
        yield return Blast("Gunner", 0, 3, chain, 30, 0.5f, 1.0f, 0.9f);            // 뇌운
        yield return Blast("Rogue", 2, 1, KitWeapon("Rogue", 0), 30, 0.5f, 0.5f, 0.85f);
        yield return Blast("Archer", 3, 2, KitWeapon("Archer", 1), 30, 0.5f, 0.9f, 0.9f);
        yield return BarrierBreak("Swordsman", 1, 0, KitWeapon("Swordsman", 1));    // 리치 왕 결계 파괴
        yield return Blast("Gunner", 0, 1, grenade, 40, 0.4f, 0.8f, 0.85f);         // 용암 융단폭격으로 마무리
        yield return Blast("Gunner", 0, 2, shotgun, 40, 0.4f, 0.8f, 0.85f);

        // 로고만 (자막 없음)
        rec.autopilot = false;
        yield return Fade(0f, 1f, 0.3f);
        Hit(0f, 0f, 1f);
        for (int i = 0; i <= Fps; i++)
        {
            float k = Mathf.SmoothStep(0f, 1f, i / (float)Fps);
            rec.logo.alpha = k;
            rec.logo.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, k);
            yield return null;
        }
        int tail = Fps * 2;
        for (int i = 0; i < tail; i++) { rec.musicVolume = 1f - i / (float)tail; yield return null; }

        rec.End();
        foreach (string k in skinKeys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
        Time.captureFramerate = 0;
        Time.timeScale = 1f;
        In("Auto", false);
        Debug.Log("[CAP] done frames=" + rec.frames + " (" + (rec.frames / (float)Fps).ToString("0.0") + "s)");
        yield return null;
    }

    // ================================================================= 레딧 홍보 영상 15초 (record.ps1 -Mode reddit)
    // 트레일러처럼 적에게 딱 붙지 않고 실제 플레이처럼 거리를 두고 돌며 싸움 · 영웅 필살기 · 보스 궁극기(결계) · 거울의 흉내만 빠르게
    static IEnumerator Clip(string hero, int who, int stage, int evolve, int swarm, float lead, float hold, float after, float zoom)
    {
        Debug.Log("[CAP] clip " + hero + " stage " + stage + " frames " + (rec != null ? rec.frames : 0));
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        Evolve(evolve);
        Swarm(stage, swarm);
        Swarm(stage, swarm / 2);
        rec.autopilot = true;
        yield return F(1.3f);                       // 적이 몰려오고 영웅이 거리를 벌릴 시간 (녹화 안 함)
        rec.baseZoom = zoom;
        rec.recording = true;
        Hit(0.8f, 0.4f, 0.6f);
        yield return F(lead);                       // 거리를 두고 돌며 쏘는 평소 전투
        yield return Ult(hold);
        Hit(1.1f, 1.2f, 0.4f);
        rec.StartCoroutine(SlowMo(0.35f, 0.9f));
        yield return F(after);
    }

    // 보스 궁극기: 결계 안에 쏟아지는 공격을 거리를 두고 피하며 버티다 필살기 → 보스가 쓰러지며 결계가 깨짐
    static IEnumerator ClipBarrier(string hero, int who, int stage, int evolve)
    {
        Debug.Log("[CAP] clip barrier " + hero + " stage " + stage + " frames " + (rec != null ? rec.frames : 0));
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        Evolve(evolve);
        object b = SpawnBoss(stage, 0.8f);
        rec.autopilot = true;
        yield return F(0.8f);
        Component bc = (Component)b;
        Component ult = bc.GetComponent(T("BossUltimate"));
        if (ult == null) ult = bc.gameObject.AddComponent(T("BossUltimate"));
        yield return null;
        ((MonoBehaviour)ult).StartCoroutine((IEnumerator)Call(ult, "Run"));
        yield return F(3.6f);                       // 외침 화면(글자)은 찍지 않음
        rec.baseZoom = 1.05f;
        rec.recording = true;
        Hit(1f, 1f, 1f);
        yield return F(2.0f);                       // 결계 안에서 쏟아지는 보스 공격
        yield return Ult(0.3f);
        yield return F(0.4f);
        KillBoss(b);
        Hit(1.5f, 2f, 1f);
        rec.StartCoroutine(SlowMo(0.25f, 1.3f));
        yield return F(1.5f);
    }

    // 거울의 군주가 영웅의 필살기를 그대로 흉내 냄 → 맞받아 필살기
    static IEnumerator ClipMirror(string hero, int who)
    {
        Debug.Log("[CAP] clip mirror " + hero + " frames " + (rec != null ? rec.frames : 0));
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, 3);
        Component b = (Component)SpawnBoss(3, 0.8f);
        yield return null;
        Component skills = b != null ? b.GetComponent(T("BossSkills")) : null;
        if (skills != null) Set(skills, "next", Time.time + 999f);
        rec.autopilot = true;
        yield return F(1.0f);
        rec.baseZoom = 1.05f;
        rec.recording = true;
        Hit(0.8f, 0.5f, 0.7f);
        if (skills != null)
        {
            ((MonoBehaviour)skills).StartCoroutine((IEnumerator)Call(skills, "Run", Call(skills, "MirrorUlt", true, true)));
            Cue("mirror_ult");
            Sx("roar", 0.4f, 0.5f);                 // 거울의 군주가 내 필살기를 흉내 냄
            Sx("ultcast", 0.5f, 0.7f);
        }
        yield return F(1.6f);                       // 보스가 내 필살기를 흉내
        yield return Ult(0.4f);
        Hit(1f, 1f, 0.3f);
        yield return F(0.6f);
    }

    [UnityTest]
    public IEnumerator RedditClip()
    {
        Debug.Log("[CAP] reddit start");
        LogAssert.ignoreFailingMessages = true;
        Directory.CreateDirectory(Out);
        Time.captureFramerate = Fps;
        In("Auto", true);
        AudioListener.volume = 0f;
        SetGameLanguage(Lang);
        GameObject host = new GameObject("CaptureRec");
        UnityEngine.Object.DontDestroyOnLoad(host);
        rec = host.AddComponent<Rec>();
        rec.musicPath = "TrailerMusic";
        rec.musicVolume = 1f;
        // 실제 플레이처럼 거리를 둠 (잡몹 5.5 ~ 8.5칸, 보스 7.5 ~ 10칸)
        rec.nearMob = 5.5f; rec.farMob = 8.5f; rec.nearBoss = 7.5f; rec.farBoss = 10f;
        BuildOverlay();
        rec.sub.text = "";
        rec.black.color = new Color(0f, 0f, 0f, 0f);

        yield return Load("Gunner", 1);
        rec.Begin(Path.Combine(Out, "SoulSaver_Reddit15.mp4"));

        int dual = 2, grenade = 7;                  // SpecialAbilities: DualId · GrenadeId
        yield return Clip("Gunner", 0, 1, dual, 34, 0.7f, 0.05f, 1.4f, 1.05f);                       // 약 2.2초: 쌍권총 총알 폭풍
        yield return ClipBarrier("Archer", 3, 1, KitWeapon("Archer", 0));                           // 약 4초: 지옥의 군주 결계 → 파괴
        yield return Clip("Swordsman", 1, 2, KitWeapon("Swordsman", 0), 30, 0.6f, 0.5f, 1.1f, 1.0f); // 약 2.2초
        yield return ClipMirror("Rogue", 2);                                                       // 약 2.6초: 거울의 군주가 필살기 흉내
        yield return Clip("Gunner", 0, 2, grenade, 40, 0.5f, 0.7f, 1.1f, 1.0f);                     // 약 2.3초: 융단폭격 마무리

        // 로고 (약 1.3초)
        rec.autopilot = false;
        yield return Fade(0f, 1f, 0.2f);
        Hit(0f, 0f, 1f);
        int n = Fps * 2 / 3;
        for (int i = 0; i <= n; i++)
        {
            float k = Mathf.SmoothStep(0f, 1f, i / (float)n);
            rec.logo.alpha = k;
            rec.logo.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, k);
            yield return null;
        }
        for (int i = 0; i < n; i++) { rec.musicVolume = 1f - i / (float)n; yield return null; }

        rec.End();
        foreach (string k in skinKeys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
        Time.captureFramerate = 0;
        Time.timeScale = 1f;
        In("Auto", false);
        Debug.Log("[CAP] done frames=" + rec.frames + " (" + (rec.frames / (float)Fps).ToString("0.0") + "s)");
        yield return null;
    }

    static IEnumerator Delayed(float s, Action a) { yield return F(s); a(); }
}
