using System.Collections;
using UnityEngine;

// 2.0.0~ 영웅 손맛 연출 (특수효과) — 평타 특수 조건 · 고유 스킬 · 진화 스킬 · 필살기가 「터지는 순간」의 짧고 강한 쾌감
//   사용자: 「화면이 줌되며 슬로우 걸리고 그 후에 풀리며 화면이 크게 흔들리는 효과 + 콰가강 소리」
//           「단 게임 진행을 루즈하게 하거나 귀찮고 거슬리게 하면 안 됨. 짜릿한 도파민만」
//   그래서 규칙 (어떤 영웅 · 단계든 아래를 넘지 못함 — 단계 표 Caps):
//     ① 짧게: 슬로우는 실제 시간 평타 0.1초 · 스킬 0.12초 · 진화 0.16초 · 필살기 0.28초가 끝, 완전히 멈추지 않고 0.3배까지만 (조작이 계속 먹힘)
//     ② 드물게: 단계마다 쉬는 시간 (평타 특수 조건 3초 · 스킬 1.5초 · 진화 2.5초) — 반복될수록 무뎌지는 것 막기
//     ③ 센 쪽이 먼저: 방금(0.45초 안) 더 센 연출이 있었으면 약한 연출은 시간 · 줌을 건드리지 않고 소리 · 흔들림만
//     ④ 화면을 가리지 않음: 줌은 조금 당기는 정도(최대 16%), 번쩍임은 옅은 영웅색 한순간 (번쩍임 효과를 끄면 없음)
//     ⑤ 흐름을 막지 않음: 레벨업 창 · 일시정지 · 이야기 · 거너 조준 · 죽는 중이면 시간 · 줌은 건너뛰고, 터지는 순간 시간이 멈춰 있으면(창이 열림) 흔들림 · 소리도 건너뜀
//   설정 「특수효과」(GameSettings.SpecialFx): 꺼짐 = 아무것도 · 약하게 = 슬로우 없음 · 줌 · 흔들림 절반 · 보통 · 강하게 = 1.3배 (상한도 1.25배까지만)
//   화면 흔들림 · 번쩍임 효과 · 움직임 줄이기 설정을 그대로 따름 (흔들림을 끄면 줌도 끔 — Juice.CounterFreeze 와 같은 관례)
public enum ImpactTier { Attack, Skill, Evolve, Ult }

[System.Serializable]
public struct ImpactStyle
{
    public float zoom;          // 당기는 비율 (0.08 = 8% 당김), 0 = 줌 없음
    public float slow;          // 슬로우 길이 (실제 초), 0 = 슬로우 없이 바로 터짐
    public float shake;         // 터질 때 흔들림 세기 (월드 단위, 기존 Shake 와 같은 눈금)
    public float shakeFor;      // 흔들림 길이 (실제 초)
    public bool directional;    // 맞은 방향으로 튕기는 흔들림 (아니면 사방)
    public Color flash;         // 번쩍임 색 (알파 = 세기, 0 이면 없음)
    public string pre;          // 슬로우가 시작될 때 소리 (빨려 드는 소리), 없으면 null
    public string sound;        // 터질 때 소리
    public float vol, pitch;

    public static ImpactStyle Make(float zoom, float slow, float shake, float shakeFor, string sound, float vol = 0.8f, float pitch = 1f,
                                   string pre = null, Color flash = default, bool directional = false)
        => new ImpactStyle { zoom = zoom, slow = slow, shake = shake, shakeFor = shakeFor, sound = sound, vol = vol, pitch = pitch,
                             pre = pre, flash = flash, directional = directional };
}

public static class Impact
{
    // 단계별 상한 · 쉬는 시간 (위 규칙 ① ②)
    struct Cap { public float slow, zoom, shake, cooldown; }
    static readonly Cap[] Caps =
    {
        new Cap { slow = 0.10f, zoom = 0.06f, shake = 0.35f, cooldown = 3.0f },     // 평타 특수 조건
        new Cap { slow = 0.12f, zoom = 0.08f, shake = 0.45f, cooldown = 1.5f },     // 고유 스킬
        new Cap { slow = 0.16f, zoom = 0.10f, shake = 0.55f, cooldown = 2.5f },     // 진화 스킬
        new Cap { slow = 0.28f, zoom = 0.16f, shake = 0.90f, cooldown = 0.0f },     // 필살기 (게이지가 이미 드물게 함)
    };
    const float SlowScale = 0.3f, Recent = 0.45f;
    // 2.0.1~ 슬로 · 줌은 단계와 상관없이 4.5초에 한 번 (단계별 쉬는 시간이 따로라 진화 장식 · 스킬이 번갈아 약 1.4초마다 느려졌음) · 필살기는 예외
    const float SlowRest = 4.5f, UltRepeat = 1f;
    static float lastSlowAt = -99f;
    // 방금(1초 안) 필살기 연출이 있었는지 — 연달아 터지는 필살기(태풍 되감기 등)의 두 번째 큰 흔들림을 호출처가 건너뛰려고
    public static bool UltRecent => Time.unscaledTime - lastAt[(int)ImpactTier.Ult] < UltRepeat;
    static readonly float[] lastAt = { -99f, -99f, -99f, -99f };
    static int lastTier = -1;
    static float lastTierAt = -99f;

    public static int Level => GameSettings.SpecialFx;
    public static bool Off => Level == 0 || Application.isBatchMode;

    // 지금 영웅의 단계 연출 (HeroImpacts 표) — 호출하는 곳은 「무엇이 언제」만 알면 됨
    public static void Hit(ImpactTier tier, Vector3 at, Vector2 dir = default) => Play(tier, HeroImpacts.Get(CharacterData.Selected, tier), at, dir);

    // 쉬는 시간 안이면 거짓 (호출처가 무거운 계산을 건너뛰고 싶을 때)
    public static bool Ready(ImpactTier tier) => !Off && Time.unscaledTime - lastAt[(int)tier] >= Caps[(int)tier].cooldown;

    public static void Play(ImpactTier tier, ImpactStyle s, Vector3 at, Vector2 dir = default)
    {
        if (Off || !Ready(tier)) return;
        int ti = (int)tier;
        Cap cap = Caps[ti];
        float now = Time.unscaledTime;
        float prevAt = lastAt[ti];      // 2.0.0 검토: 터지기 전에 건너뛰면 쉬는 시간을 되돌리려고
        lastAt[ti] = now;

        // 세기 단계 (꺼짐은 위에서 끝남)
        int lv = Level;
        float mul = lv == 1 ? 0.5f : lv == 3 ? 1.3f : 1f, capMul = lv == 3 ? 1.25f : 1f;
        float slow = lv == 1 ? 0f : Mathf.Min(s.slow * (lv == 3 ? 1.25f : 1f), cap.slow * capMul);
        float zoom = Mathf.Min(s.zoom * mul, cap.zoom * capMul);
        float shake = Mathf.Min(s.shake * mul, cap.shake * capMul);
        if (!GameSettings.ScreenShake) shake = zoom = 0f;
        if (GameSettings.ReduceMotion) zoom = 0f;
        Color flash = GameSettings.Flashes ? new Color(s.flash.r, s.flash.g, s.flash.b, Mathf.Min(0.35f, s.flash.a * mul)) : Color.clear;

        // 규칙 ③: 방금 더 센 연출이 있었으면 시간 · 줌은 건드리지 않음
        bool weaker = lastTier > ti && now - lastTierAt < Recent;
        bool timeOk = !weaker && Juice.CanSlow && Time.timeScale > 0f;
        if (ti < (int)ImpactTier.Ult && now - lastSlowAt < SlowRest) timeOk = false;
        // 2.0.1~ 필살기가 1초 안에 또 터지면 두 번째는 소리만 (슬로 · 줌 · 큰 흔들림 · 번쩍임 없음)
        if (ti == (int)ImpactTier.Ult && now - prevAt < UltRepeat) { timeOk = false; shake = 0f; flash = Color.clear; }
        if (!timeOk) { slow = 0f; zoom = 0f; }
        if (slow > 0f || zoom > 0f) lastSlowAt = now;
        if (!weaker) { lastTier = ti; lastTierAt = now; }

        SpecialFeedback fx = SpecialAbilities.SharedFx;
        if (fx != null) Sounds.Register(fx);
        Juice.Shared.StartCoroutine(Run(fx, s, slow, zoom, shake, flash, at, dir, lv, ti, prevAt));      // 2026-11-04~ 위치 at 도 (스킨 하이라이트 · 가장자리)
    }

    static IEnumerator Run(SpecialFeedback fx, ImpactStyle s, float slow, float zoom, float shake, Color flash, Vector3 at, Vector2 dir, int lv, int ti, float prevAt)
    {
        float vol = s.vol * (lv == 1 ? 0.8f : 1f);
        float now0 = Time.unscaledTime;
        PlayerController p = Hostile.Player;
        if (slow > 0f)
        {
            // 1) 빨려 듦: 0.3배 슬로우 + 화면이 살짝 당겨짐 (실제 시간) — 줌은 슬로우 동안 당기고 터진 뒤 0.18초에 걸쳐 돌아옴
            Juice.MarkStop();
            Juice.Shared.Hold(SlowScale, slow);
            if (zoom > 0f && p != null && p.MainCamera != null) Juice.Shared.ZoomPunch(p.MainCamera, p.normalZoom, 1f - zoom, slow, 0.18f);
            if (fx != null && s.pre != null) fx.PlayRaw(s.pre, vol * 0.7f, s.pitch);
            for (float w = 0f; w < slow; w += Time.unscaledDeltaTime) yield return null;
        }
        else if (zoom > 0f && p != null && p.MainCamera != null)
            Juice.Shared.ZoomPunch(p.MainCamera, p.normalZoom, 1f - zoom * 0.6f, 0.05f, 0.15f);

        // 2) 터짐: 그 사이 창이 열려 시간이 멈췄으면(레벨업 · 일시정지) 조용히 건너뜀 — 창 위에서 화면이 흔들리거나 소리가 나지 않게
        if (Time.timeScale == 0f) { if (lastAt[ti] > prevAt) lastAt[ti] = prevAt; yield break; }      // 건너뛴 연출은 쉬는 시간을 쓰지 않음
        SkinFx.OnImpact((ImpactTier)ti, at, dir, ti == (int)ImpactTier.Ult && now0 - prevAt < UltRepeat);      // 2026-11-04~ 스킨: 필살기 가장자리(C10) · 무기 하이라이트(W7) — 특수효과 꺼짐 · 쉬는 시간 · 창 규칙을 그대로 따름
        if (shake > 0f && fx != null) fx.Shake(shake, Mathf.Max(0.08f, s.shakeFor), s.directional ? dir : Vector2.zero);
        if (flash.a > 0.01f) Juice.Shared.Flash(flash.a, flash);
        if (fx != null && s.sound != null) fx.PlayRaw(s.sound, vol, s.pitch);
    }

    // ================================================================= 합성 효과음 (소리 파일 없이 — SpecialFeedback.MakeClip)
    //   이름은 imp_ 로 시작 · 영웅마다 음높이 · 조합으로 서명이 달라짐 (HeroImpacts)
    public static class Sounds
    {
        static SpecialFeedback registeredOn;
        public const string Kwagang = "imp_kwagang", Boom = "imp_boom", Crack = "imp_crack", Thud = "imp_thud", Zing = "imp_zing",
                            Chime = "imp_chime", Glass = "imp_glass", Gong = "imp_gong", Tick = "imp_tick", Paper = "imp_paper",
                            Whoosh = "imp_whoosh", Suck = "imp_suck", Wail = "imp_wail", Stamp = "imp_stamp",
                            Cheer = "imp_cheer", SandSlam = "imp_sandslam",       // 2.0.0~ 씨름꾼: 관중 함성 · 모래판 쿠웅
                            Squeal = "imp_squeal", Crunch = "imp_crunch",          // 2.0.0~ 공사 반장: 끼이익 · 빠각
                            String = "imp_string", Applause = "imp_applause",      // 2.0.0~ 인형사: 현 뜯는 소리 · 박수
                            Dirt = "imp_dirt", Bell = "imp_bell", Rumble = "imp_rumble",      // 2.0.0~ 장의사: 흙더미 쏟아짐 · 종 · 땅울림
                            Shutter = "imp_shutter", Flash = "imp_flash", Curtain = "imp_curtain";      // 2.0.0~ 사진사: 찰칵 · 휘이잉—팡 · 드르르륵—찰칵

        static float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
        static float N(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
        static float Dec(float t, float len, float pow) => Mathf.Pow(Mathf.Clamp01(1f - t / len), pow);
        // 높이가 미끄러지는 사인의 위상 (f0 → f1, len 초 동안 지수로)
        static float Glide(float f0, float f1, float len, float t)
        {
            float k = Mathf.Log(f1 / f0) / len;
            return Mathf.Abs(k) < 1e-4f ? f0 * t : f0 * (Mathf.Exp(k * Mathf.Min(t, len)) - 1f) / k + (t > len ? f1 * (t - len) : 0f);
        }
        static float SinP(float phase) => Mathf.Sin(2f * Mathf.PI * phase);
        static float Clip(float x) => x / (1f + Mathf.Abs(x));      // 부드러운 포화 (콰가강의 거친 맛)

        public static void Register(SpecialFeedback fx)
        {
            if (fx == null || registeredOn == fx) return;
            registeredOn = fx;
            // 콰가강: 쇳덩이가 부딪히는 소리 — 잡음 터짐 + 내려가는 두 줄 사각에 가까운 저음 + 쇠 울림 두 줄, 포화로 거칠게
            fx.MakeClip(Kwagang, 0.75f, (t, d, r) =>
            {
                float body = SinP(Glide(190f, 55f, 0.35f, t)) + 0.6f * SinP(Glide(203f, 58f, 0.35f, t));
                float ring = (Sin(1180f, t) + 0.7f * Sin(1910f, t) + 0.4f * Sin(2730f, t)) * Dec(t, 0.7f, 2.2f);
                float hit = N(r) * Dec(t, 0.09f, 2f);
                return Clip((body * 1.8f * Dec(t, 0.55f, 1.6f) + ring * 0.55f + hit * 1.6f)) * 0.9f;
            });
            // 쿠웅: 아주 낮은 몸통 + 짧은 깨짐
            fx.MakeClip(Boom, 0.9f, (t, d, r) =>
                Clip(SinP(Glide(85f, 32f, 0.6f, t)) * 2.2f * Dec(t, 0.85f, 1.8f) + N(r) * Dec(t, 0.12f, 2.5f) * 0.9f) * 0.95f);
            // 짝: 날카롭게 갈라지는 소리
            fx.MakeClip(Crack, 0.28f, (t, d, r) =>
                (N(r) * Dec(t, 0.06f, 1.5f) * 0.9f + Sin(2100f, t) * Dec(t, 0.2f, 3f) * 0.35f + SinP(Glide(420f, 160f, 0.1f, t)) * Dec(t, 0.12f, 2f) * 0.6f));
            // 퍽: 묵직한 주먹
            fx.MakeClip(Thud, 0.38f, (t, d, r) =>
                Clip(SinP(Glide(135f, 48f, 0.18f, t)) * 2f * Dec(t, 0.36f, 2f) + N(r) * Dec(t, 0.05f, 2f) * 0.7f) * 0.95f);
            // 챙: 칼날이 울리는 맑은 소리 (떨림)
            fx.MakeClip(Zing, 0.65f, (t, d, r) =>
            {
                float vib = 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 9f * t);
                return (Sin(2650f * vib, t) + 0.6f * Sin(3980f * vib, t) + 0.3f * Sin(5300f, t)) * Dec(t, 0.62f, 2.4f) * 0.4f + N(r) * Dec(t, 0.03f, 1f) * 0.6f;
            });
            // 딩: 종을 친 소리 (어긋난 배음)
            fx.MakeClip(Chime, 0.95f, (t, d, r) =>
                (Sin(880f, t) + 0.55f * Sin(1347f, t) + 0.35f * Sin(2213f, t) + 0.2f * Sin(3170f, t)) * Dec(t, 0.92f, 2.6f) * 0.45f + N(r) * Dec(t, 0.015f, 1f) * 0.4f);
            // 쨍그랑: 유리 · 얼음이 흩어짐 — 짧은 높은 조각들이 무작위로
            fx.MakeClip(Glass, 0.6f, (t, d, r) =>
            {
                float s = 0f;
                for (int k = 0; k < 7; k++)
                {
                    float at = k * 0.045f + 0.012f * (k % 3), len = 0.12f;
                    if (t < at || t > at + len) continue;
                    s += Sin(2900f + 470f * ((k * 37) % 9), t - at) * Dec(t - at, len, 2.5f);
                }
                return s * 0.32f + N(r) * Dec(t, 0.05f, 1.5f) * 0.5f;
            });
            // 지잉: 큰 징 (긴 저음 배음)
            fx.MakeClip(Gong, 1.3f, (t, d, r) =>
                (Sin(98f, t) + 0.7f * Sin(98f * 2.76f, t) + 0.45f * Sin(98f * 5.4f, t) * Dec(t, 0.6f, 2f)) * Dec(t, 1.28f, 1.5f) * Mathf.Clamp01(t / 0.01f) * 0.55f
                + N(r) * Dec(t, 0.04f, 1.5f) * 0.5f);
            // 째깍: 묵직한 시계 두 번
            fx.MakeClip(Tick, 0.32f, (t, d, r) =>
            {
                float a = t < 0.05f ? Dec(t, 0.05f, 3f) : t > 0.14f && t < 0.2f ? Dec(t - 0.14f, 0.06f, 3f) * 1.2f : 0f;
                return (Sin(1650f, t) * 0.6f + N(r) * 0.8f) * a + SinP(Glide(320f, 140f, 0.1f, Mathf.Max(0f, t - 0.14f))) * (t > 0.14f ? Dec(t - 0.14f, 0.16f, 2f) : 0f) * 0.7f;
            });
            // 촤악: 종이 · 지도가 크게 펼쳐짐 — 띠 잡음이 부풀었다 꺼짐
            fx.MakeClip(Paper, 0.45f, (t, d, r) =>
            {
                float env = Mathf.Clamp01(t / 0.04f) * Dec(t, 0.44f, 1.7f);
                return N(r) * env * 0.75f * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 31f * t)) + Sin(240f, t) * env * 0.25f;
            });
            // 쾅 도장: 짧고 단단한 나무 · 고무
            fx.MakeClip(Stamp, 0.3f, (t, d, r) =>
                Clip(SinP(Glide(260f, 90f, 0.05f, t)) * 1.8f * Dec(t, 0.22f, 2.5f) + N(r) * Dec(t, 0.025f, 1f) * 1.2f) * 0.9f);
            // 빨려 듦 (슬로우 시작): 잡음이 차오르며 높아짐
            fx.MakeClip(Whoosh, 0.28f, (t, d, r) => N(r) * Mathf.Pow(t / 0.28f, 1.6f) * 0.5f);
            fx.MakeClip(Suck, 0.26f, (t, d, r) => SinP(Glide(180f, 760f, 0.26f, t)) * Mathf.Pow(t / 0.26f, 1.4f) * 0.45f + N(r) * (t / 0.26f) * 0.15f);
            // 와아아—: 관중 함성 (0.9초) — 띠 잡음 다섯 줄(450 · 700 · 1100 · 1600 · 2300Hz, 각 ±4% 천천히 흔들림)을 사람 목소리 모음처럼 겹치고 0.15초에 부풀어 0.9초에 꺼짐, 밑에 120Hz 웅성
            fx.MakeClip(Cheer, 0.9f, (t, d, r) =>
            {
                float env = Mathf.Clamp01(t / 0.15f) * Dec(t, 0.9f, 1.4f);
                float s = 0f;
                float[] fs = { 450f, 700f, 1100f, 1600f, 2300f };
                float[] ws = { 1f, 0.85f, 0.6f, 0.35f, 0.2f };
                for (int k = 0; k < 5; k++)
                {
                    float f = fs[k] * (1f + 0.04f * Mathf.Sin(2f * Mathf.PI * (0.8f + 0.27f * k) * t + k));
                    s += ws[k] * Sin(f, t) * (0.55f + 0.45f * N(r));
                }
                return Clip((s * 0.45f + Sin(120f, t) * 0.12f) * env * 1.4f) * 0.75f;
            });
            // 쿠—웅 쏴아: Boom 몸통(85 → 30Hz)을 조금 더 낮고 짧게(0.6초) + 0.12초부터 모래가 흩어지는 쏴아(3~6kHz, 0.8초에 걸쳐 작아짐)
            fx.MakeClip(SandSlam, 1.0f, (t, d, r) =>
            {
                float body = SinP(Glide(80f, 28f, 0.5f, t)) * 2.2f * Dec(t, 0.6f, 1.8f) + N(r) * Dec(t, 0.1f, 2.5f) * 0.9f;
                float sand = t > 0.12f ? N(r) * Sin(4600f, t) * Dec(t - 0.12f, 0.8f, 1.5f) * 0.45f : 0f;
                return Clip(body) * 0.9f + sand;
            });
            // 끼이익: 금속이 긁히는 소리 (0.32초) — 2.2kHz 부근 띠 잡음 + 2,300 → 1,500Hz 로 미끄러지는 사인에 ±3% 떨림(23Hz), 끝으로 갈수록 커짐 (빨려 드는 소리 자리라 pre 로 씀)
            fx.MakeClip(Squeal, 0.32f, (t, d, r) =>
            {
                float grow = 0.35f + 0.65f * (t / 0.32f);
                float tremble = 1f + 0.03f * Mathf.Sin(2f * Mathf.PI * 23f * t);
                float tone = SinP(Glide(2300f, 1500f, 0.32f, t) * tremble);
                float hiss = N(r) * Sin(2200f, t) * 0.7f;
                return Clip((tone * 0.55f + hiss * 0.5f) * grow * 1.3f) * 0.6f;
            });
            // 빠각: 묵직한 저음(150 → 50Hz) + 자갈 같은 짧은 잡음 터짐 다섯 개(0.02초 간격 · 높이 제각각) + 포화 — Thud 보다 거칠고 Crack 보다 낮음
            fx.MakeClip(Crunch, 0.42f, (t, d, r) =>
            {
                float body = SinP(Glide(150f, 50f, 0.3f, t)) * 2f * Dec(t, 0.4f, 2f);
                float gravel = 0f;
                for (int k = 0; k < 5; k++)
                {
                    float at = k * 0.02f;
                    if (t < at || t > at + 0.04f) continue;
                    gravel += N(r) * Sin(900f + 700f * ((k * 53) % 5), t - at) * Dec(t - at, 0.04f, 1.5f) * 1.3f;
                }
                return Clip(body + gravel + N(r) * Dec(t, 0.03f, 2f) * 0.8f) * 0.95f;
            });
            // 씽: 현을 뜯는 소리 — 880Hz 에서 0.5초 동안 830Hz(반음 아래)로 미끄러지는 톱니에 가까운 배음 셋(1 · 2 · 3배 = 1 · 0.6 · 0.3) + 6Hz 떨림 0.4% + 첫 0.01초 손톱 잡음
            fx.MakeClip(String, 0.55f, (t, d, r) =>
            {
                float vib = 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                float ph = Glide(880f, 830f, 0.5f, t) * vib;
                float tone = SinP(ph) + 0.6f * SinP(ph * 2f) + 0.3f * SinP(ph * 3f);
                float nail = t < 0.01f ? N(r) * 0.8f : 0f;
                return tone * Dec(t, 0.55f, 2.2f) * 0.38f + nail;
            });
            // 짝짝짝: 박수 여럿 — 0.85초 안의 박수 18번(앞쪽이 촘촘 · 뒤로 성김, 각 0.025초 띠 잡음) + 첫 0.1초 낮은 쿵
            fx.MakeClip(Applause, 0.85f, (t, d, r) =>
            {
                float s = 0f;
                for (int k = 0; k < 18; k++)
                {
                    float at = 0.85f * Mathf.Pow((k + 0.5f * ((k * 37) % 5) / 5f) / 18f, 1.5f);
                    if (t < at || t > at + 0.025f) continue;
                    s += N(r) * Sin(2400f + 300f * ((k * 53) % 5), t - at) * Dec(t - at, 0.025f, 2f) * 1.4f;
                }
                float thump = t < 0.1f ? SinP(Glide(110f, 55f, 0.1f, t)) * Dec(t, 0.1f, 1.5f) * 0.6f : 0f;
                return Clip(s + thump) * 0.9f;
            });
            // 쏴르르—쿵: 흙더미가 쏟아져 덮임 (0.55초) — 0~0.3초 띠 잡음(낮은 쪽으로 눌린 잡음) + 무작위 알갱이 딸깍 스무 개, 0.28초에 110 → 42Hz 짧은 쿵 + 포화
            fx.MakeClip(Dirt, 0.55f, (t, d, r) =>
            {
                float hiss = t < 0.3f ? N(r) * (0.3f + 0.2f * Sin(190f, t)) * Dec(t, 0.3f, 0.7f) : 0f;
                float grains = 0f;
                for (int k = 0; k < 20; k++)
                {
                    float at = 0.3f * (((k * 37) % 20) / 20f);
                    if (t >= at && t < at + 0.006f) grains += N(r) * 0.9f;
                }
                float thump = t >= 0.28f ? SinP(Glide(110f, 42f, 0.27f, t - 0.28f)) * 1.8f * Dec(t - 0.28f, 0.27f, 2f) : 0f;
                return Clip(hiss + grains + thump) * 0.95f;
            });
            // 댕——: 종 (1.6초) — 기본 196Hz + 어긋난 배음(×2.0 · ×2.74 · ×4.1 · ×5.3) · 198Hz 맥놀이 · 아주 짧은 망치 잡음 (Gong 98Hz 보다 높고 맑음, Chime 880Hz 보다 낮고 무거움)
            fx.MakeClip(Bell, 1.6f, (t, d, r) =>
            {
                float[] ratio = { 1f, 2f, 2.74f, 4.1f, 5.3f };
                float[] w = { 1f, 0.55f, 0.4f, 0.25f, 0.15f };
                float s = 0f;
                for (int k = 0; k < 5; k++) s += w[k] * Sin(196f * ratio[k], t) * Dec(t, 1.6f, 1.3f + 0.4f * k);
                s += 0.5f * Sin(198f, t) * Dec(t, 1.6f, 1.5f);
                float hammer = t < 0.012f ? N(r) * 0.8f : 0f;
                return s * 0.36f + hammer;
            });
            // 쿠르릉: 40 ~ 60Hz 떨리는 저음이 차오름 + 잔돌 잡음 (0.3초 · Suck 자리)
            fx.MakeClip(Rumble, 0.3f, (t, d, r) =>
                Sin(40f + 20f * (t / 0.3f), t) * (1f + 0.25f * Sin(28f, t)) * Mathf.Clamp01(t / 0.25f) * 0.8f + N(r) * (t / 0.3f) * 0.18f);
            // 찰칵 (0.18초): 딸깍 두 번(0 · 0.06초, 두 번째가 크고 낮음) + 2.8kHz 짧은 울림
            fx.MakeClip(Shutter, 0.18f, (t, d, r) =>
            {
                float c1 = t < 0.012f ? N(r) * 0.7f : 0f;
                float c2 = t >= 0.06f && t < 0.08f ? (N(r) * 0.95f + Sin(900f, t) * 0.4f) * Dec(t - 0.06f, 0.02f, 1f) : 0f;
                return Clip(c1 + c2 + Sin(2800f, t) * 0.25f * Dec(t, 0.12f, 2f));
            });
            // 휘이잉—팡 (0.5초): 1.2kHz -> 6kHz 로 올라가는 얇은 사인(충전) 0.35초 + 끝에 잡음 터짐
            fx.MakeClip(Flash, 0.5f, (t, d, r) =>
            {
                float charge = t < 0.35f ? SinP(Glide(1200f, 6000f, 0.35f, t)) * 0.3f * Mathf.Clamp01(t / 0.05f) : 0f;
                float pop = t >= 0.35f ? N(r) * 0.9f * Dec(t - 0.35f, 0.15f, 2f) : 0f;
                return Clip(charge + pop);
            });
            // 드르르륵—찰칵 (0.55초): 40Hz 로 떨리는 띠 잡음 0.4초 + 끝에 딸깍
            fx.MakeClip(Curtain, 0.55f, (t, d, r) =>
            {
                float slide = t < 0.4f ? N(r) * (0.3f + 0.2f * Sin(40f, t)) * Mathf.Clamp01(t / 0.05f) : 0f;
                float click = t >= 0.4f && t < 0.42f ? N(r) * 0.9f : 0f;
                float ring = t >= 0.4f ? Sin(2800f, t) * 0.25f * Dec(t - 0.4f, 0.12f, 2f) : 0f;
                return Clip(slide + click + ring);
            });
            // 우우웅: 유령의 울음 (떨리며 내려감)
            fx.MakeClip(Wail, 0.7f, (t, d, r) =>
            {
                float f = 520f * (1f - 0.35f * t / 0.7f) * (1f + 0.03f * Mathf.Sin(2f * Mathf.PI * 7f * t));
                return (Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 1.5f * t)) * Mathf.Clamp01(t / 0.05f) * Dec(t, 0.7f, 1.5f) * 0.45f;
            });
        }
    }
}
