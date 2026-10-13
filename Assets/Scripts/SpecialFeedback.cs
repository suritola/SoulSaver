using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 특수 능력의 소리·조준 표시·알림 글자·화면 흔들림
// 소리는 사운드 파일 없이 코드에서 파형을 만들어 씀
public class SpecialFeedback : MonoBehaviour
{
    const int Rate = 44100;

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    AudioSource oneShot;
    AudioSource loop;
    Material lineMaterial;
    Material dashMaterialTemplate;
    TMP_FontAsset font;
    Material fontMaterial;
    Coroutine shake;

    public float volume = 0.45f;

    public void Init(TMP_FontAsset font, Material fontMaterial)
    {
        this.font = font;
        this.fontMaterial = fontMaterial;

        oneShot = gameObject.AddComponent<AudioSource>();
        oneShot.playOnAwake = false;
        loop = gameObject.AddComponent<AudioSource>();
        loop.playOnAwake = false;
        loop.loop = true;

        lineMaterial = new Material(Shader.Find("Sprites/Default"));

        // 점선: 절반은 채우고 절반은 비운 텍스처를 선 길이만큼 반복
        Texture2D dash = new Texture2D(8, 1, TextureFormat.RGBA32, false);
        for (int x = 0; x < 8; x++) dash.SetPixel(x, 0, x < 5 ? Color.white : Color.clear);
        dash.wrapMode = TextureWrapMode.Repeat;
        dash.filterMode = FilterMode.Point;
        dash.Apply();
        dashMaterialTemplate = new Material(lineMaterial) { mainTexture = dash };

        BuildClips();
    }

    // ================================================================= sound
    public void Play(string name, float vol = 1f, float pitch = 1f)
    {
        SkinAudio.Adjust(this, ref name, ref vol, ref pitch);      // 스킨: 공격 · 코인 소리 음색
        pitch *= 1f + Random.Range(-0.05f, 0.05f);                  // 1.0.2: 같은 소리가 반복돼도 단조롭지 않게 음높이를 조금씩
        PlayRaw(name, vol, pitch);
    }

    // 스킨 조정 없이 그대로 (스킨 덧소리 · 피격 소리)
    public void PlayRaw(string name, float vol = 1f, float pitch = 1f)
    {
        if (oneShot == null || !clips.TryGetValue(name, out AudioClip c)) return;
        PitchSource(pitch).PlayOneShot(c, vol * volume * GameSettings.SfxVolume);
    }

    // 소스의 pitch 는 이미 울리고 있는 소리에도 걸리므로 (처치 '퐁'마다 레벨업 · 보스 소리 음이 흔들리던 문제)
    // 기본 음높이는 한 소스에서, 음높이를 바꾼 소리는 돌려 쓰는 소스 묶음에서 냄
    const int PitchedCount = 8;
    readonly List<AudioSource> pitched = new List<AudioSource>();
    int pitchedNext;

    AudioSource PitchSource(float pitch)
    {
        if (Mathf.Approximately(pitch, 1f)) { oneShot.pitch = 1f; return oneShot; }
        // 같은 음높이로 울리는 소스가 있으면 함께, 아니면 쉬고 있는 소스, 다 바쁘면 가장 오래된 소스
        for (int i = 0; i < pitched.Count; i++)
            if (Mathf.Approximately(pitched[i].pitch, pitch)) return pitched[i];
        AudioSource s = null;
        for (int i = 0; i < pitched.Count; i++)
            if (!pitched[i].isPlaying) { s = pitched[i]; break; }
        if (s == null)
        {
            if (pitched.Count < PitchedCount)
            {
                s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                pitched.Add(s);
            }
            else
            {
                s = pitched[pitchedNext];
                pitchedNext = (pitchedNext + 1) % PitchedCount;
                s.Stop();
            }
        }
        s.pitch = pitch;
        return s;
    }

    public void StartLoop(string name, float vol = 1f, float pitch = 1f)
    {
        if (loop == null || !clips.TryGetValue(name, out AudioClip c)) return;
        if (loop.clip != c || !loop.isPlaying)
        {
            loop.clip = c;
            loop.Play();
        }
        loop.volume = vol * volume * GameSettings.SfxVolume;
        loop.pitch = pitch;
    }

    public void StopLoop()
    {
        if (loop != null && loop.isPlaying) loop.Stop();
    }

    delegate float Wave(float t, float dur, System.Random rng);

    AudioClip Make(string name, float dur, Wave wave)
    {
        int n = Mathf.CeilToInt(dur * Rate);
        float[] data = new float[n];
        System.Random rng = new System.Random(name.GetHashCode());
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate, dur, rng), -1f, 1f);
        AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        clips[name] = clip;
        return clip;
    }

    // 밖에서 소리를 더함 (스킨 소리 · SkinAudio). 이미 있으면 그대로
    public void MakeClip(string name, float dur, System.Func<float, float, System.Random, float> wave)
    {
        if (clips.ContainsKey(name)) return;
        Make(name, dur, (t, d, r) => wave(t, d, r));
    }

    static float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
    static float Noise(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
    static float Decay(float t, float dur, float power = 3f) => Mathf.Pow(Mathf.Clamp01(1f - t / dur), power);

    void BuildClips()
    {
        BuildReloadClips();
        BuildHeroClips();
        // 충전 윙윙 (1초 반복, 180Hz 정수 주기라 끊김 없음)
        Make("hum", 1f, (t, d, r) => (Sin(180f, t) * 0.5f + Sin(360f, t) * 0.25f + Sin(540f, t) * 0.1f) * (0.8f + 0.2f * Sin(6f, t)));
        // 완충 띵
        Make("ding", 0.45f, (t, d, r) => (Sin(1318f, t) + Sin(2637f, t) * 0.35f) * Decay(t, d, 2.5f) * 0.7f);
        // 적 처치: 짧게 떨어지는 퐁 + 푹 터지는 잡음
        Make("pop", 0.18f, (t, d, r) => (Sin(Mathf.Lerp(420f, 110f, t / d), t) * 0.55f + Noise(r) * 0.35f * Decay(t, 0.05f, 2f)) * Decay(t, d, 2f));
        // 레벨업: 도 · 미 · 솔 · 도 아르페지오 + 반짝임
        Make("levelup", 0.7f, (t, d, r) =>
        {
            float f = t < 0.08f ? 523f : t < 0.16f ? 659f : t < 0.24f ? 784f : 1047f;
            float shimmer = t > 0.24f ? Sin(2093f, t) * 0.15f * Sin(9f, t) : 0f;
            return (Sin(f, t) + Sin(f * 2f, t) * 0.3f + shimmer) * Decay(t, d, 1.3f) * 0.45f;
        });
        // 보스 목소리: 한 음절씩 (리치 = 텅 빈 울림, 군주 = 낮은 으르렁, 슬라임 = 뽀글뽀글)
        Make("voice_lich", 0.11f, (t, d, r) =>
        {
            float f = 180f + 40f * Mathf.Sin(t * 60f);
            return (Sin(f, t) * 0.6f + Sin(f * 2.01f, t) * 0.3f + Sin(f * 3.02f, t) * 0.15f) * Mathf.Sin(Mathf.PI * t / d) * (0.8f + 0.2f * Sin(30f, t));
        });
        float growl = 0f;
        Make("voice_demon", 0.12f, (t, d, r) =>
        {
            growl += (Noise(r) - growl) * 0.2f;
            float f = 95f + 25f * Mathf.Sin(t * 45f);
            float saw = 2f * (t * f - Mathf.Floor(t * f + 0.5f));
            return (saw * 0.45f + growl * 0.6f + Sin(f * 0.5f, t) * 0.3f) * Mathf.Sin(Mathf.PI * t / d);
        });
        Make("voice_slime", 0.1f, (t, d, r) =>
        {
            float f = Mathf.Lerp(300f, 620f, t / d);
            return Sin(f, t) * 0.6f * Mathf.Sin(Mathf.PI * t / d) * (0.7f + 0.3f * Sin(55f, t));
        });
        // 코인 반짝
        Make("sparkle", 0.22f, (t, d, r) => (t < 0.07f ? Sin(2093f, t) : Sin(2637f, t)) * Decay(t, d, 2f) * 0.25f);
        // 준비 알림 (두 음)
        Make("chime", 0.35f, (t, d, r) => (t < 0.12f ? Sin(880f, t) : Sin(1320f, t)) * Decay(t, d, 1.5f) * 0.5f);
        // 경고 (쿨타임, 부족)
        Make("buzz", 0.16f, (t, d, r) => Mathf.Sign(Sin(110f, t)) * 0.25f * Decay(t, d, 1f));
        // 폭발
        float low = 0f;
        Make("boom", 0.5f, (t, d, r) => { low += (Noise(r) - low) * 0.08f; return (low * 2.2f + Sin(55f, t) * 0.6f) * Decay(t, d, 2.5f); });
        // 가벼운 발사음 (주파수가 내려감)
        Make("pew", 0.12f, (t, d, r) => Mathf.Sign(Sin(Mathf.Lerp(1400f, 500f, t / d), t)) * 0.3f * Decay(t, d, 2f));
        // 저격 발사 (날카로운 파열음)
        Make("crack", 0.3f, (t, d, r) => (Noise(r) * 0.7f * Decay(t, 0.08f, 2f) + Sin(Mathf.Lerp(900f, 200f, t / d), t) * 0.5f) * Decay(t, d, 2f));
        // 전기
        Make("zap", 0.18f, (t, d, r) => (Noise(r) * 0.4f + Mathf.Sign(Sin(2200f + 800f * Sin(40f, t), t)) * 0.3f) * Decay(t, d, 1.5f));
        // 바람 (소리가 커졌다 작아짐)
        float wlow = 0f;
        Make("whoosh", 0.35f, (t, d, r) => { wlow += (Noise(r) - wlow) * 0.15f; return wlow * 1.6f * Mathf.Sin(Mathf.PI * t / d); });
        // 불 타는 소리 (1초 반복)
        Make("crackle", 1f, (t, d, r) => { float n = Noise(r); return (r.NextDouble() < 0.004 ? n : n * 0.12f) * 0.8f; });
        // 화염 방사 (1초 반복): 낮게 울리는 불길 + 딱딱 튀는 소리
        float roar = 0f, roar2 = 0f;
        Make("flame", 1f, (t, d, r) =>
        {
            float n = Noise(r);
            roar += (n - roar) * 0.04f;          // 낮은 굉음
            roar2 += (n - roar2) * 0.35f;        // 쉬익거리는 바람
            float pop = r.NextDouble() < 0.006 ? Noise(r) * 0.8f : 0f;
            float swell = 0.85f + 0.15f * Sin(3f, t);
            return (roar * 2.4f + roar2 * 0.35f) * swell + pop;
        });
        // 점화 화악 (소리가 빠르게 커졌다 잦아듦)
        float ign = 0f;
        Make("ignite", 0.4f, (t, d, r) =>
        {
            ign += (Noise(r) - ign) * Mathf.Lerp(0.05f, 0.3f, t / d);
            return ign * 2f * Mathf.Min(1f, t / 0.05f) * Decay(t, d, 1.5f);
        });
        // 과열 치익
        Make("hiss", 0.6f, (t, d, r) => Noise(r) * 0.35f * Decay(t, d, 1.2f));
        // 신비로운 화음 (마법)
        Make("shimmer", 0.6f, (t, d, r) => (Sin(660f, t) + Sin(880f * (1f + t * 0.5f), t) * 0.6f + Sin(1320f, t) * 0.3f) * Decay(t, d, 1.5f) * 0.35f);
        // 쇠사슬 철컥
        Make("clank", 0.2f, (t, d, r) => (Sin(1870f, t) + Sin(2630f, t) * 0.7f + Sin(3410f, t) * 0.5f + Noise(r) * 0.3f) * Decay(t, d, 4f) * 0.35f);
        // 유탄 발사 퉁
        Make("thump", 0.18f, (t, d, r) => Sin(Mathf.Lerp(160f, 60f, t / d), t) * Decay(t, d, 2f) * 0.8f);
        // 낮은 울림 (희생)
        Make("pulse", 0.7f, (t, d, r) => (Sin(70f, t) * 0.7f + Sin(140f, t) * 0.3f) * Mathf.Sin(Mathf.PI * t / d) * 0.8f);

        // ---------------- 강렬한 소리 (총성 · 필살기)
        // 권총 총성 "탕": 날카로운 파열 + 짧은 저음 킥 + 잔향
        float gl = 0f;
        Make("gunshot", 0.32f, (t, d, r) =>
        {
            float n = Noise(r);
            gl += (n - gl) * 0.25f;
            float crack = n * Decay(t, 0.025f, 1.5f) * 1.2f;
            float body = gl * 1.6f * Decay(t, 0.12f, 2f);
            float kick = Sin(Mathf.Lerp(180f, 50f, Mathf.Clamp01(t / 0.08f)), t) * Decay(t, 0.14f, 2f) * 0.9f;
            float tail = gl * 0.35f * Decay(t, d, 3f);
            return crack + body + kick + tail;
        });
        // 산탄총 "쾅": 더 두껍고 긴 굉음
        float sl = 0f, sl2 = 0f;
        Make("shotgun", 0.7f, (t, d, r) =>
        {
            float n = Noise(r);
            sl += (n - sl) * 0.12f; sl2 += (n - sl2) * 0.03f;
            float crack = n * Decay(t, 0.04f, 1.2f) * 1.1f;
            float kick = Sin(Mathf.Lerp(120f, 35f, Mathf.Clamp01(t / 0.15f)), t) * Decay(t, 0.3f, 1.8f) * 1.1f;
            return crack + sl * 1.8f * Decay(t, 0.25f, 2f) + sl2 * 2.5f * Decay(t, d, 2f) + kick;
        });
        // 큰 폭발: 긴 저음 굉음 + 파편
        float bl = 0f, bl2 = 0f;
        Make("bigboom", 1.2f, (t, d, r) =>
        {
            float n = Noise(r);
            bl += (n - bl) * 0.05f; bl2 += (n - bl2) * 0.012f;
            float debris = r.NextDouble() < 0.01 ? Noise(r) * Decay(t, d, 1f) * 0.6f : 0f;
            float kick = Sin(Mathf.Lerp(90f, 28f, Mathf.Clamp01(t / 0.3f)), t) * Decay(t, 0.6f, 1.5f) * 1.2f;
            return n * Decay(t, 0.05f, 1.2f) + bl * 2.2f * Decay(t, 0.5f, 1.5f) + bl2 * 3.5f * Decay(t, d, 1.5f) + kick + debris;
        });
        // 레일건 충전 (0.5초): 올라가는 전자음
        Make("railcharge", 0.55f, (t, d, r) =>
        {
            float f = Mathf.Lerp(200f, 1800f, t / d);
            return (Sin(f, t) * 0.4f + Sin(f * 1.5f, t) * 0.2f + Noise(r) * 0.1f * t / d) * Mathf.Min(1f, t / 0.05f);
        });
        // 레일건 발사: 강한 전기 파열 + 긴 윙
        Make("railgun", 1f, (t, d, r) =>
        {
            float n = Noise(r);
            float zap = (n * 0.8f + Mathf.Sign(Sin(1600f - 1200f * t, t)) * 0.5f) * Decay(t, 0.25f, 1.5f);
            float boom = Sin(Mathf.Lerp(110f, 40f, t), t) * Decay(t, 0.5f, 1.5f);
            float ring = Sin(3200f, t) * 0.15f * Decay(t, d, 2f);
            return zap + boom + ring;
        });
        // 천둥: 찢어지는 파열 + 구르는 저음
        float th = 0f, th2 = 0f;
        Make("thunder", 1.3f, (t, d, r) =>
        {
            float n = Noise(r);
            th += (n - th) * 0.2f; th2 += (n - th2) * 0.01f;
            float crack = (th * 1.8f) * Decay(t, 0.18f, 1.2f) * (0.6f + 0.4f * Mathf.Sign(Sin(23f, t)));
            return crack + th2 * 4f * Decay(t, d, 1.2f) * (0.7f + 0.3f * Sin(3f, t));
        });
        // 금속 베기 "슈악": 바람 + 쇳소리
        float sw = 0f;
        Make("slash", 0.35f, (t, d, r) =>
        {
            sw += (Noise(r) - sw) * 0.4f;
            float metal = (Sin(2400f, t) + Sin(3700f, t) * 0.6f) * 0.25f * Decay(t, d, 2.5f);
            return sw * 1.4f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / (d * 0.6f))) + metal;
        });
        // 회오리 굉음 (1초 반복)
        float ro = 0f, ro2 = 0f;
        Make("roar", 1f, (t, d, r) =>
        {
            float n = Noise(r);
            ro += (n - ro) * 0.02f; ro2 += (n - ro2) * 0.2f;
            return (ro * 4f + ro2 * 0.4f) * (0.75f + 0.25f * Sin(4f, t));
        });
        // 활시위 "퉁": 굵은 현을 튕긴 소리 (배음이 빨리 사라지고 음이 살짝 내려감) + 손끝 딸깍
        Make("bowtwang", 0.4f, (t, d, r) =>
        {
            float f = Mathf.Lerp(125f, 108f, t / d);
            float str = (Sin(f, t) * 0.6f + Sin(f * 2f, t) * 0.35f * Decay(t, d, 6f) + Sin(f * 3f, t) * 0.2f * Decay(t, d, 9f)) * Decay(t, d, 3.5f);
            return str + Noise(r) * 0.5f * Decay(t, 0.012f, 2f);
        });
        // 화살이 날아가는 "슉": 높은 바람 소리가 빠르게 지나감
        float alp = 0f;
        Make("arrowfly", 0.26f, (t, d, r) =>
        {
            float n = Noise(r);
            alp += (n - alp) * 0.35f;
            float hp = n - alp;
            return hp * 0.55f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / d)) * (1f - 0.6f * t / d);
        });
        // 유리병 "달그락": 병과 병이 부딪히는 높은 소리 두 번
        Make("glassclink", 0.22f, (t, d, r) =>
        {
            float a = (Sin(2950f, t) + Sin(4420f, t) * 0.5f) * Decay(t, 0.12f, 3f);
            float b = t > 0.06f ? (Sin(3500f, t) + Sin(5200f, t) * 0.4f) * Decay(t - 0.06f, 0.14f, 3f) * 0.7f : 0f;
            return (a + b) * 0.28f;
        });
        // 유리 깨짐 "쨍그랑": 날카로운 잡음 + 여러 파편이 흩어지는 짤랑임
        Make("shatter", 0.45f, (t, d, r) =>
        {
            float burst = Noise(r) * 0.6f * Decay(t, 0.05f, 2f);
            float tinkle = 0f;
            for (int k = 0; k < 5; k++)
            {
                float at = 0.02f + k * 0.06f;
                if (t > at) tinkle += Sin(2600f + 900f * k, t) * Decay(t - at, 0.12f, 4f);
            }
            return burst + tinkle * 0.22f;
        });
        // 부글부글: 방울이 올라오며 톡톡 터지는 소리 (1초 반복)
        Make("bubble", 1f, (t, d, r) =>
        {
            float ph = (t * 9f) % 1f;
            float f = 180f + 520f * ph;
            return Sin(f, t) * Mathf.Pow(1f - ph, 2f) * 0.35f + Sin(260f, t) * 0.08f;
        });
        // 치이익: 약품이 거품을 내며 녹는 소리
        float fz = 0f;
        Make("fizz", 0.7f, (t, d, r) =>
        {
            float n = Noise(r);
            fz += (n - fz) * 0.6f;
            float pop = r.NextDouble() < 0.01 ? n : 0f;
            return ((n - fz) * 0.35f + pop * 0.5f) * Decay(t, d, 1.5f);
        });
        // 시위를 당기는 "끼이익": 나무 활이 휘는 낮은 삐걱임
        float cr = 0f;
        Make("bowdraw", 0.6f, (t, d, r) =>
        {
            cr += (Noise(r) - cr) * 0.05f;
            float creak = Sin(Mathf.Lerp(70f, 150f, t / d) + 25f * cr, t) * (0.35f + 0.5f * Mathf.Abs(cr));
            return creak * 0.5f * Mathf.Clamp01(t / 0.08f) * (1f - 0.5f * t / d);
        });
    }

    // ================================================================= 장전 소리 (무기마다 다름)
    // at 초에 시작해 len 초 동안 줄어드는 짧은 소리 조각
    static float Hit(float t, float at, float len, float power = 3f) => t < at ? 0f : Decay(t - at, len, power);

    void BuildReloadClips()
    {
        // 리볼버 탄창 · 도적 표창: 쇳조각이 짤랑 (높은 금속음 세 번)
        Make("rl_stars", 0.4f, (t, d, r) =>
        {
            float s = 0f;
            for (int k = 0; k < 3; k++) s += (Sin(3100f + k * 420f, t) + Sin(4700f + k * 300f, t) * 0.5f) * Hit(t, k * 0.08f, 0.1f, 3f);
            return s * 0.22f + Noise(r) * 0.05f * Hit(t, 0f, 0.3f);
        });
        // 산탄총: 펌프 "철-컥"
        Make("rl_shotgun", 0.42f, (t, d, r) =>
        {
            float n = Noise(r);
            return (n * 0.7f + Sin(140f, t) * 0.6f) * Hit(t, 0f, 0.07f, 2f) + (n * 0.8f + Sin(210f, t) * 0.7f) * Hit(t, 0.22f, 0.09f, 2f);
        });
        // 저격총: 노리쇠 "딸깍 - 스르륵 - 철컥"
        Make("rl_sniper", 0.55f, (t, d, r) =>
        {
            float n = Noise(r);
            float click = Sin(2400f, t) * Hit(t, 0f, 0.03f);
            float slide = t > 0.08f && t < 0.3f ? n * 0.25f * Mathf.Sin(Mathf.PI * (t - 0.08f) / 0.22f) : 0f;
            float clack = (n * 0.6f + Sin(900f, t) * 0.5f) * Hit(t, 0.36f, 0.06f, 2f);
            return click * 0.5f + slide + clack;
        });
        // 쌍권총: 탄창 두 개를 번갈아 "착 · 착"
        Make("rl_dual", 0.45f, (t, d, r) =>
        {
            float s = 0f;
            for (int k = 0; k < 4; k++) s += (Sin(k % 2 == 0 ? 1500f : 1150f, t) + Noise(r) * 0.4f) * Hit(t, k * 0.1f, 0.04f);
            return s * 0.35f;
        });
        // 유도 지팡이: 영혼이 차오르는 반짝임 (올라가는 음)
        Make("rl_seeker", 0.5f, (t, d, r) => (Sin(Mathf.Lerp(500f, 1300f, t / d), t) + Sin(Mathf.Lerp(1000f, 2600f, t / d), t) * 0.3f) * Mathf.Sin(Mathf.PI * t / d) * 0.3f);
        // 번개 사슬총: 전기가 충전되는 "지이이-탁"
        Make("rl_chain", 0.5f, (t, d, r) =>
        {
            float buzz = Mathf.Sign(Sin(Mathf.Lerp(90f, 260f, t / d), t)) * 0.18f * Mathf.Clamp01(t / 0.35f) * (t < 0.4f ? 1f : 0f);
            return buzz + Noise(r) * 0.6f * Hit(t, 0.4f, 0.05f, 2f);
        });
        // 유탄 발사기: 꺾어 열고 "텅" 탄을 넣고 "철컥"
        Make("rl_grenade", 0.55f, (t, d, r) =>
        {
            float n = Noise(r);
            return (n * 0.5f + Sin(120f, t)) * 0.5f * Hit(t, 0f, 0.08f, 2f)
                 + Sin(Mathf.Lerp(90f, 60f, (t - 0.2f) / 0.2f), t) * 0.8f * Hit(t, 0.2f, 0.15f, 2f)
                 + (n * 0.6f + Sin(700f, t) * 0.4f) * Hit(t, 0.42f, 0.06f, 2f);
        });
        // 독침 대롱: 나무 대롱을 "톡톡" 두드리고 침을 넣는 "슥"
        Make("rl_blowgun", 0.4f, (t, d, r) =>
        {
            float tap = Sin(520f, t) * (Hit(t, 0f, 0.04f) + Hit(t, 0.1f, 0.04f));
            float slide = t > 0.2f && t < 0.35f ? Noise(r) * 0.2f * Mathf.Sin(Mathf.PI * (t - 0.2f) / 0.15f) : 0f;
            return tap * 0.45f + slide;
        });
        // 도박 카드: 카드를 "촤르륵" 섞고 "탁"
        Make("rl_cards", 0.5f, (t, d, r) =>
        {
            float riffle = 0f;
            if (t < 0.38f)
            {
                float rate = Mathf.Lerp(40f, 90f, t / 0.38f);
                float ph = (t * rate) % 1f;
                riffle = Noise(r) * (ph < 0.25f ? 0.5f : 0.05f);
            }
            return riffle * 0.6f + (Noise(r) * 0.7f + Sin(300f, t) * 0.5f) * Hit(t, 0.42f, 0.05f, 2f);
        });
        // 그물 활: 밧줄을 "끼이익" 당겨 묶는 소리
        float rope = 0f;
        Make("rl_netbow", 0.5f, (t, d, r) =>
        {
            rope += (Noise(r) - rope) * 0.08f;
            float creak = Sin(Mathf.Lerp(110f, 180f, t / d) + 40f * rope, t) * (0.3f + 0.6f * Mathf.Abs(rope));
            return creak * 0.5f * Mathf.Sin(Mathf.PI * t / d) + Sin(260f, t) * 0.4f * Hit(t, 0.42f, 0.06f);
        });
        // 속사 활: 화살통 "부스럭" + 화살 세 대 "딱딱딱"
        Make("rl_burstbow", 0.5f, (t, d, r) =>
        {
            float rustle = t < 0.25f ? Noise(r) * 0.18f * (0.6f + 0.4f * Sin(30f, t)) : 0f;
            float clicks = 0f;
            for (int k = 0; k < 3; k++) clicks += Sin(1800f - k * 200f, t) * Hit(t, 0.28f + k * 0.06f, 0.03f);
            return rustle + clicks * 0.35f;
        });
        // 수은 구슬: 액체가 "출렁 · 뽁"
        Make("rl_quicksilver", 0.5f, (t, d, r) =>
        {
            float s = 0f;
            for (int k = 0; k < 4; k++) s += Sin(Mathf.Lerp(700f + k * 150f, 1400f + k * 200f, Mathf.Clamp01((t - k * 0.09f) / 0.08f)), t) * Hit(t, k * 0.09f, 0.08f, 2f);
            return s * 0.3f;
        });
        // 자석 폭탄: 자력이 "우우웅" 올라가다 "철컥" 붙음
        Make("rl_magnet", 0.55f, (t, d, r) =>
        {
            float hum = (Sin(Mathf.Lerp(60f, 140f, t / 0.45f), t) + Sin(Mathf.Lerp(120f, 280f, t / 0.45f), t) * 0.5f) * (t < 0.45f ? Mathf.Clamp01(t / 0.3f) : 0f);
            return hum * 0.35f + (Noise(r) * 0.6f + Sin(1600f, t) * 0.5f) * Hit(t, 0.45f, 0.06f, 2f);
        });
        // 폭죽 발사기: 심지가 "치이익" 타고 마개가 "뽕"
        Make("rl_firework", 0.55f, (t, d, r) =>
        {
            float fuse = t < 0.42f ? Noise(r) * 0.22f * (0.5f + 0.5f * Mathf.Clamp01(t / 0.3f)) * (0.7f + 0.3f * Sin(55f, t)) : 0f;
            return fuse + Sin(Mathf.Lerp(900f, 400f, (t - 0.44f) / 0.08f), t) * 0.5f * Hit(t, 0.44f, 0.08f, 2f);
        });
    }

    // ================================================================= 1.0.2 새 효과음 (새 캐릭터 · 필살기 시작 · 진화)
    void BuildHeroClips()
    {
        // 잉크: 젖은 붓이 쓸고 가는 소리
        float ink = 0f;
        Make("ink", 0.22f, (t, d, r) => { ink += (Noise(r) - ink) * 0.35f; return (ink * 0.9f + Sin(Mathf.Lerp(300f, 180f, t / d), t) * 0.15f) * Mathf.Sin(Mathf.PI * t / d) * 0.8f; });
        // 종이 접기: 바스락 + 접히는 둔탁함
        Make("paperfold", 0.4f, (t, d, r) => (r.NextDouble() < 0.08 ? Noise(r) : Noise(r) * 0.15f) * Decay(t, d, 1.2f) * 0.6f + Sin(Mathf.Lerp(140f, 60f, t / d), t) * 0.45f * Decay(t, 0.15f, 2f));
        // 째깍
        Make("tick", 0.06f, (t, d, r) => Sin(3200f, t) * Decay(t, 0.03f, 3f) * 0.5f + Sin(1600f, t) * Decay(t, 0.05f, 3f) * 0.3f);
        // 깊은 종
        Make("bell", 1.4f, (t, d, r) => (Sin(330f, t) + Sin(911f, t) * 0.5f + Sin(1782f, t) * 0.25f + Sin(165f, t) * 0.4f) * Decay(t, d, 2f) * 0.42f);
        // 톱니 (짧은 딸깍 넷)
        Make("ratchet", 0.18f, (t, d, r) => { float ph = (t * 25f) % 1f; return (ph < 0.15f ? Noise(r) * 0.5f + Sin(2400f, t) * 0.4f : 0f) * Decay(t, d, 1f); });
        // 되감기: 거꾸로 빨려 드는 소리
        float rw = 0f;
        Make("rewind", 0.55f, (t, d, r) => { rw += (Noise(r) - rw) * 0.2f; float k = t / d; return (rw * 0.8f + Sin(Mathf.Lerp(200f, 1200f, k * k), t) * 0.3f) * k * k * (t > d - 0.03f ? 0f : 1f); });
        // 편지 휙
        float fp = 0f;
        Make("flick", 0.1f, (t, d, r) => { float n = Noise(r); float hp = n - fp; fp = n; return hp * Decay(t, d, 3f) * 0.45f; });
        // 도장 쿵
        Make("stamp", 0.15f, (t, d, r) => Sin(Mathf.Lerp(120f, 50f, t / d), t) * Decay(t, d, 3f) * 0.8f + Noise(r) * Decay(t, 0.02f, 2f) * 0.4f);
        // 곡소리: 내려앉는 유령 울음
        Make("wail", 0.8f, (t, d, r) => { float f = Mathf.Lerp(700f, 350f, t / d) * (1f + 0.02f * Sin(6f, t)); return (Sin(f, t) + Sin(f * 1.5f, t) * 0.3f) * Mathf.Sin(Mathf.PI * t / d) * 0.32f; });
        // 얼음: 쨍한 결정
        Make("freeze", 0.4f, (t, d, r) => (Sin(2600f, t) + Sin(3900f, t) * 0.6f + Sin(5200f, t) * 0.4f) * Decay(t, d, 3f) * 0.28f + Noise(r) * 0.2f * Decay(t, 0.05f, 2f));
        // 빙의: 낮게 부풀어 오름
        Make("possess", 0.9f, (t, d, r) => (Sin(90f, t) + Sin(135f, t) * 0.5f + Noise(r) * 0.1f) * Mathf.Sin(Mathf.PI * t / d) * 0.6f);
        // 필살기 시작: 힘 있게 솟는 화음
        float uc = 0f;
        Make("ultcast", 0.6f, (t, d, r) => { uc += (Noise(r) - uc) * 0.1f; return ((Sin(220f, t) + Sin(330f, t) * 0.7f + Sin(440f, t) * 0.5f) * 0.32f + uc * 0.5f) * Mathf.Min(1f, t / 0.04f) * Decay(t, d, 1.5f); });
        // 진화: 올라가는 장조 화음
        Make("evolve", 0.9f, (t, d, r) =>
        {
            float s = 0f;
            float[] f = { 523f, 659f, 784f, 1047f };
            for (int i = 0; i < f.Length; i++) { float st = i * 0.08f; if (t >= st) s += Sin(f[i], t) * Decay(t - st, 0.6f, 2f); }
            return s * 0.22f;
        });
        // 진화 스킬이 터질 때 반짝임
        Make("evoflair", 0.35f, (t, d, r) => (Sin(1568f, t) + Sin(2093f, t) * 0.7f) * Decay(t, d, 2f) * 0.22f + Sin(3136f, t) * Decay(t, 0.1f, 2f) * 0.15f);
        // 회전 베기 휘몰아침
        float sp = 0f;
        Make("spin", 0.5f, (t, d, r) => { sp += (Noise(r) - sp) * 0.25f; return sp * (0.6f + 0.4f * Sin(14f, t)) * Mathf.Sin(Mathf.PI * t / d) * 1.2f; });
        // 활시위 튕김 (화살비)
        Make("pluck", 0.25f, (t, d, r) => (Sin(440f, t) + Sin(880f, t) * 0.5f + Sin(1320f, t) * 0.2f) * Decay(t, d, 4f) * 0.45f);
    }

    // ================================================================= lines
    public LineRenderer NewLine(string name, bool dashed, int order = 20)
    {
        GameObject go = new GameObject(name);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = dashed ? new Material(dashMaterialTemplate) : lineMaterial;
        lr.textureMode = LineTextureMode.Stretch;
        lr.sortingLayerName = "Effect";
        lr.sortingOrder = order;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.enabled = false;
        return lr;
    }

    // 점선 한 칸의 길이 (월드 단위)
    const float DashLength = 0.9f;

    void SetDashScale(LineRenderer lr, float length)
    {
        if (lr.material.mainTexture != null) lr.material.mainTextureScale = new Vector2(Mathf.Max(1f, length / DashLength), 1f);
    }

    public void SetLine(LineRenderer lr, Vector3 a, Vector3 b, Color color, float width)
    {
        // 점선용 선은 도트 화살촉으로 그림 (Sprites/Default 셰이더가 텍스처 반복을 무시해 실선으로 보였음)
        if (lr.material != null && lr.material.mainTexture != null)
        {
            lr.enabled = false;
            SetDots(lr, a, b, color, width);
            return;
        }
        HideDots(lr);
        lr.enabled = true;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        SetDashScale(lr, Vector3.Distance(a, b));
    }

    public void SetRing(LineRenderer lr, Vector3 center, float radius, Color color, float width)
    {
        const int seg = 48;
        lr.enabled = true;
        lr.positionCount = seg + 1;
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius);
        }
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        SetDashScale(lr, 2f * Mathf.PI * radius);
    }

    public void SetCone(LineRenderer lr, Vector3 origin, Vector2 dir, float halfAngle, float range, Color color, float width)
    {
        const int seg = 16;
        lr.enabled = true;
        lr.positionCount = seg + 3;
        lr.SetPosition(0, origin);
        for (int i = 0; i <= seg; i++)
        {
            float a = Mathf.Lerp(-halfAngle, halfAngle, i / (float)seg);
            lr.SetPosition(i + 1, origin + (Vector3)((Vector2)(Quaternion.Euler(0, 0, a) * dir) * range));
        }
        lr.SetPosition(seg + 2, origin);
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        SetDashScale(lr, range * 2f + range * halfAngle * Mathf.Deg2Rad * 2f);
    }

    public static void Hide(LineRenderer lr)
    {
        if (lr != null) lr.enabled = false;
        HideDots(lr);
    }

    // ================================================================= dotted lines (fx_trail_dot)
    static readonly Dictionary<LineRenderer, List<SpriteRenderer>> dotPools = new Dictionary<LineRenderer, List<SpriteRenderer>>();
    static Sprite dotSprite;
    const float DotSpacing = 0.75f;

    static void HideDots(LineRenderer lr)
    {
        if (lr == null || !dotPools.TryGetValue(lr, out List<SpriteRenderer> pool)) return;
        foreach (SpriteRenderer s in pool) if (s != null) s.enabled = false;
    }

    // a → b 를 가리키는 화살촉이 흘러가는 점선 (굵기가 클수록 큰 점)
    void SetDots(LineRenderer lr, Vector3 a, Vector3 b, Color color, float width)
    {
        if (dotSprite == null)
        {
            Sprite[] frames = Fx.Frames("fx_trail_dot");
            if (frames.Length == 0) return;
            dotSprite = frames[0];
        }
        if (!dotPools.TryGetValue(lr, out List<SpriteRenderer> pool))
        {
            pool = new List<SpriteRenderer>();
            dotPools[lr] = pool;
        }
        pool.RemoveAll(s => s == null);

        Vector3 d = b - a;
        float len = d.magnitude;
        int count = len < 0.01f ? 0 : Mathf.Min(120, Mathf.FloorToInt(len / DotSpacing) + 1);
        float size = Mathf.Clamp(width * 4.5f, 0.35f, 1.2f);
        float scale = size / dotSprite.bounds.size.y;
        float rot = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        float shift = Mathf.Repeat(Time.unscaledTime * 2.5f, 1f) * DotSpacing;

        while (pool.Count < count)
        {
            GameObject go = new GameObject("AimDot");
            go.transform.SetParent(lr.transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.sortingLayerName = "Effect";
            pool.Add(sr);
        }
        for (int i = 0; i < pool.Count; i++)
        {
            SpriteRenderer sr = pool[i];
            if (i >= count) { sr.enabled = false; continue; }
            float dist = Mathf.Min(len, i * DotSpacing + shift);
            float k = dist / Mathf.Max(0.01f, len);
            sr.enabled = true;
            sr.sortingOrder = lr.sortingOrder;
            sr.transform.position = a + d * k;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, rot);
            sr.transform.localScale = Vector3.one * scale;
            // 끝으로 갈수록 살짝 옅게
            sr.color = new Color(color.r, color.g, color.b, color.a * Mathf.Lerp(1f, 0.55f, k));
        }
    }

    // ================================================================= floating text
    readonly Dictionary<string, float> lastText = new Dictionary<string, float>();

    // 캐릭터 위로 떠오르며 사라지는 글자 (같은 글자는 잠깐 동안 한 번만)
    public void FloatText(Vector3 pos, string text, Color color, float size = 5f, float throttle = 0.6f)
    {
        if (lastText.TryGetValue(text, out float t) && Time.time - t < throttle) return;
        lastText[text] = Time.time;

        GameObject go = new GameObject("FloatText");
        go.transform.position = pos + new Vector3(0f, 3.2f, 0f);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        if (fontMaterial != null) tmp.fontSharedMaterial = fontMaterial;
        tmp.text = text;
        tmp.fontSize = size * 1.6f;         // 화면(세로 40칸)에서 작아 잘 안 보였음 · 부르는 쪽 크기 비율은 그대로
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.sortingLayerID = SortingLayer.NameToID("Effect");
        tmp.sortingOrder = 30;
        go.AddComponent<FloatUp>();
    }

    // ================================================================= screen shake
    public void Shake(float strength = 0.25f, float duration = 0.12f)
    {
        if (!GameSettings.ScreenShake) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        if (shake != null) StopCoroutine(shake);
        shake = StartCoroutine(ShakeRoutine(cam.transform, strength, duration));
    }

    Vector3 camRest;
    bool camRestSet;

    IEnumerator ShakeRoutine(Transform cam, float strength, float duration)
    {
        if (!camRestSet) { camRest = cam.localPosition; camRestSet = true; }
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / duration;
            shakeOffset = Random.insideUnitCircle * strength * k;
            yield return null;
        }
        shakeOffset = Vector2.zero;
        shake = null;
    }

    // ================================================================= camera hold
    // 카메라는 플레이어의 자식이라 원래는 플레이어를 따라감.
    // HoldCamera 동안에는 월드의 한 점에 머물고, ReleaseCamera 하면 부드럽게 플레이어에게 돌아감
    Vector2 shakeOffset;
    Vector3 holdTarget, holdPos;
    bool holding;
    float returnBlend = 1f;             // 1 = 플레이어에 붙음

    public void HoldCamera(Vector3 worldCenter)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (!camRestSet) { camRest = cam.transform.localPosition; camRestSet = true; }
        if (!holding) holdPos = cam.transform.position;
        holdTarget = new Vector3(worldCenter.x, worldCenter.y, cam.transform.position.z);
        holding = true;
        returnBlend = 0f;
    }

    public void ReleaseCamera() => holding = false;

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || !camRestSet) return;
        Transform t = cam.transform;
        float dt = Time.unscaledDeltaTime;
        if (holding)
        {
            holdPos = Vector3.Lerp(holdPos, holdTarget, 1f - Mathf.Exp(-12f * dt));
            t.position = holdPos + (Vector3)shakeOffset;
            return;
        }
        if (returnBlend < 1f)
        {
            // 붙잡혀 있던 자리에서 플레이어 쪽으로 돌아감
            returnBlend = Mathf.Min(1f, returnBlend + dt / 0.25f);
            Vector3 follow = t.parent != null ? t.parent.TransformPoint(camRest) : camRest;
            float k = 1f - (1f - returnBlend) * (1f - returnBlend);
            holdPos = Vector3.Lerp(holdPos, follow, k);
            t.position = holdPos + (Vector3)shakeOffset;
            return;
        }
        t.localPosition = camRest + (Vector3)shakeOffset;
    }
}

// 위로 떠오르며 사라짐
public class FloatUp : MonoBehaviour
{
    public float duration = 0.9f;
    float t;
    TextMeshPro tmp;
    Color c;

    void Start()
    {
        tmp = GetComponent<TextMeshPro>();
        c = tmp.color;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        transform.position += Vector3.up * 2.2f * Time.unscaledDeltaTime;
        tmp.color = new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01(1f - (t - duration * 0.5f) / (duration * 0.5f)));
        if (t >= duration) Destroy(gameObject);
    }
}
