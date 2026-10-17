using UnityEngine;
using UnityEngine.UI;

// 체력바 타격감 공용: 흰색 지연 게이지(잃은 양) · 연속 피해 때 머무름 연장(사슬) · 피해 누적 등급 · 흔들림 · 번쩍임 · 두께 펀치
// 플레이어 HUD(PlayerHP) · 보스 HUD(bossbar) · 적 머리 위(EnemyHealthBar) 가 하나씩 들고 씀. MonoBehaviour 아님 · Tick 에 할당 없음
// 기획: docs/review/2026-10-17-체력바-흰색-지연-게이지.md
public sealed class HealthBarFeel
{
    public enum Kind { Player, Boss, Enemy }
    public enum Tier { Small = 0, Medium = 1, Big = 2, Deadly = 3 }

    // ---- 누적 창 (피해 숫자 합산과 같은 값 · DamagePopup 이 이 상수를 읽음)
    public const float WindowPlayer = 1.0f;   // 맞은 뒤 무적(0.6 + 재정비 최대 0.3 = 0.9초)보다 길어야 다음 피해와 합쳐짐
    public const float WindowBoss   = 0.6f;
    public const float WindowEnemy  = 0.4f;
    public const float WindowMax    = 1.5f;   // 첫 피해부터 이 시간이 지나면 창을 새로 시작 (도트 · 접촉 피해가 영원히 쌓여 치명이 되지 않게)

    // ---- 등급 문턱 (누적 피해 / 최대 체력) : [Medium, Big, Deadly]
    static readonly float[] TierPlayer = { 0.05f, 0.15f, 0.30f };   // 기본 체력 100: 5 · 15 · 30 (보스 접촉 25 = Big · 보스 상한 40 = Deadly)
    static readonly float[] TierBoss   = { 0.02f, 0.06f, 0.12f };   // 보스는 체력이 커서 낮게 (필살기 · 진화 폭발이 Big~Deadly)
    static readonly float[] TierEnemy  = { 0.15f, 0.35f, 0.60f };   // 잡몹은 한 방이 크므로 높게 · 쓰러지는 피해는 연출 없음 (바가 꺼짐)

    // ---- 흰 게이지 : 머무름 [Small, Medium, Big, Deadly] · 사슬 상한 · 내려오는 시간 [Player, Boss, Enemy]
    static readonly float[] HoldPlayer = { 0.65f, 0.75f, 0.90f, 1.10f };   // Small 0.65 > 피격 무적 0.6 → 접촉 피해가 한 사슬로 이어짐
    static readonly float[] HoldBoss   = { 0.45f, 0.50f, 0.60f, 0.80f };
    static readonly float[] HoldEnemy  = { 0.40f, 0.45f, 0.50f, 0.65f };
    static readonly float[] ChainCap   = { 3.0f, 1.5f, 1.2f };     // 첫 피해부터 이 시간이 지나면 더 연장하지 않음 (군중 · 연사 속에서도 내려오게)
    static readonly float[] FullBar    = { 1.6f, 1.2f, 1.0f };     // 바 전체 길이를 내려오는 데 걸리는 시간
    static readonly float[] DrainMin   = { 0.35f, 0.30f, 0.25f };  // 아무리 작은 구간도 이만큼은 움직임
    static readonly float[] DrainMax   = { 0.80f, 0.70f, 0.50f };
    public const float DtMax = 0.1f;                                // 프레임 끊김 뒤 한 번에 건너뛰지 않게

    // ---- 강조 (등급별)
    static readonly float[] FlashAmp  = { 0.25f, 0.45f, 0.70f, 1f };   // 채움 번쩍임 (흰색 섞기)
    public const float FlashTime = 0.16f, FlashPeak = 0.035f, FlashGap = 0.34f, FillFlashMul = 0.7f, FillFlashMulCB = 0.5f;
    static readonly float[] BackFlash = { 0f, 0f, 0.35f, 0.65f };      // HUD 프레임 붉은 틴트
    static readonly float[] BackFlashEnemy = { 0f, 0f, 0.5f, 0.85f };  // 적 테두리(Back) 흰색 강조
    public const float IgniteTime = 0.30f, IgniteGap = 0.12f;         // 흰 게이지 점화 (순백 → 쉼 흰색)
    // 흔들림: 진폭은 「막대 높이 대비 비율」 → 그리는 쪽이 자기 단위로 환산 (HUD 는 px, 적 바는 월드)
    static readonly float[] ShakeAmp  = { 0f, 0f, 0.45f, 0.80f };
    static readonly float[] ShakeTime = { 0f, 0f, 0.28f, 0.42f };
    public const float ShakeHz = 20f;                                  // 60fps 에서 3프레임/주기 → 사인파가 보임 (34Hz 는 지터처럼 보였음)
    static readonly float[] PunchAmp  = { 0f, 0f, 0.12f, 0.20f };      // 세로 두께 펀치
    public const float PunchTime = 0.16f;
    public const int EnemyShakeMax = 4;     // 적 「큰 피해 묶음」(테두리 · 흔들림 · 펀치) 동시 상한 (보스 · 플레이어는 제한 없음)

    // 등급 → 피해 숫자 크기 배율 (DamagePopup 이 씀)
    public static readonly float[] PopupScale = { 0.9f, 1f, 1.1f, 1.25f };     // 1.2.2~ 피해 숫자가 너무 크다는 의견 → 1 / 1.2 / 1.55 / 2 에서 줄임

    static readonly Color GhostRest = new Color(0.90f, 0.91f, 0.93f, 0.94f);

    // 체력바 시계: 일시정지(timeScale 0)엔 멈추고, 히트스톱 · 슬로모션엔 실제 시간
    public static float Dt => PlayerController.IsPaused ? 0f : Mathf.Min(Time.unscaledDeltaTime, DtMax);
    // 흰 게이지 색 (ignite 0 = 쉼 흰색 · 1 = 순백). 흰색은 모든 색각에서 명도 대비로 보여 색약 분기 없음
    public static Color GhostColor(float ignite) => Color.Lerp(GhostRest, Color.white, ignite);

    public readonly Kind kind;
    public HealthBarFeel(Kind kind) { this.kind = kind; }

    enum Phase { Closed, Hold, Drain }

    // 상태 (전부 값 타입)
    float clock;                  // 자기 시계 (Tick 의 dt 누적)
    Phase phase;
    float ghostHp = -1f;          // 흰 게이지 윗끝 (체력 단위로 저장 — 최대 체력이 바뀌어도 비율이 맞음)
    float holdUntil, chainStart, drainFrom, drainStart, drainDur;
    float accum, accumFirst = -99f, accumLast = -99f;   // 누적 피해 · 창 시작 · 마지막 피해 (Time.time)
    float shakeAmp, shakeT, shakeLen;
    float flashAmp, flashT = FlashTime, nextFlashAt;
    float backAmp, backT = FlashTime;
    float igniteT = IgniteTime;
    float punchAmp, punchT = PunchTime;
    bool shaking;                 // 「큰 피해 묶음」 상한 집계용
    static int enemyShaking;      // 지금 묶음 연출 중인 잡몹 바 수

    // 씬이 바뀔 때 한 번 (지난 씬에서 집계가 남지 않게)
    public static void ResetStatics() => enemyShaking = 0;

    // 흰 게이지 막대 만들기: 채움 막대와 같은 자리 · 크기 · 그림의 Image 하나만 새로 만들어 바로 뒤(형제 순서 한 칸 앞)에 둠. 스크립트 · 자식은 전혀 복제하지 않음
    // 1.2.0: 1.1.8~1.2.0 은 채움 막대 오브젝트를 Instantiate 로 복제했는데, 게임 씬에서 PlayerHP 가 HPgauge 자신에 붙어 있어(HPgauge 필드 = 자기 오브젝트)
    //   복제본에도 PlayerHP 가 따라붙고 그 Start 가 또 자신을 복제… → 프레임마다 HPghost 가 하나씩 늘어(1분에 약 3600개) Update · Damaged 구독이 쌓여
    //   판이 갈수록 · 일시정지 중에도 프레임이 떨어졌음 (메인 메뉴엔 PlayerHP 가 없어 멀쩡). 복제 대신 Image 만 손으로 만들어 재발 경로를 없앰
    public static Image MakeGhost(Image fill, string name)
    {
        if (fill == null) return null;
        RectTransform src = fill.rectTransform;
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(src.parent, false);
        r.SetSiblingIndex(src.GetSiblingIndex());        // 채움 막대 한 칸 앞 = 바로 뒤에 그려짐
        r.anchorMin = src.anchorMin; r.anchorMax = src.anchorMax; r.pivot = src.pivot;
        r.anchoredPosition = src.anchoredPosition; r.sizeDelta = src.sizeDelta;
        r.localScale = src.localScale; r.localRotation = src.localRotation;
        Image img = go.GetComponent<Image>();
        img.sprite = fill.sprite; img.type = fill.type; img.preserveAspect = fill.preserveAspect; img.fillCenter = fill.fillCenter;
        img.fillMethod = fill.fillMethod; img.fillOrigin = fill.fillOrigin; img.fillClockwise = fill.fillClockwise;
        img.pixelsPerUnitMultiplier = fill.pixelsPerUnitMultiplier; img.maskable = fill.maskable;
        if (fill.material != fill.defaultMaterial) img.material = fill.material;
        img.raycastTarget = false;
        img.color = GhostColor(0f);
        img.enabled = false;
        return img;
    }

    // 그리는 쪽이 「변화 없음 → 갱신 생략」 판단에 씀 (필드 비교 몇 개)
    public bool Active => shaking || (ghostHp > 0f && (phase != Phase.Closed || shakeT < shakeLen || flashT < FlashTime
        || backT < FlashTime || igniteT < IgniteTime || punchT < PunchTime));

    public float Window => kind == Kind.Player ? WindowPlayer : kind == Kind.Boss ? WindowBoss : WindowEnemy;
    float[] Thresholds => kind == Kind.Player ? TierPlayer : kind == Kind.Boss ? TierBoss : TierEnemy;
    float[] Hold => kind == Kind.Player ? HoldPlayer : kind == Kind.Boss ? HoldBoss : HoldEnemy;

    // 씬 시작 · 풀에서 다시 꺼냄 · 부활 · 이어하기: 흰 게이지를 지금 체력에 붙임 · 사슬 · 효과 모두 끔
    public void Reset(float hp)
    {
        ghostHp = Mathf.Max(0f, hp);
        phase = Phase.Closed; holdUntil = chainStart = 0f;
        accum = 0f; accumFirst = accumLast = -99f;
        if (shaking && kind == Kind.Enemy && enemyShaking > 0) enemyShaking--;
        shaking = false; shakeT = shakeLen = shakeAmp = 0f;
        flashT = backT = FlashTime; flashAmp = backAmp = 0f;
        igniteT = IgniteTime; punchT = PunchTime; punchAmp = 0f;
    }
    public void Cancel() => Reset(ghostHp);     // 오브젝트가 꺼질 때 (OnDisable) 상한 집계에서 빠지게

    // 피해 한 번. hpBefore: 깎기 전 체력 · visible: 화면에 보이는가 (false 면 흰 게이지 · 사슬만, 연출은 예약 안 함)
    // 반환: 이번 누적의 등급 (피해 숫자 크기에 씀)
    public Tier OnDamaged(float amount, float hpBefore, float maxHp, bool visible = true)
    {
        // 화면 밖 경로의 아주 작은 감소(프레임마다 조금씩 줄어드는 지속 소모): 사슬을 열거나 늘리지 않음 → 흰 게이지가 끝없이 머물지 않게
        if (!visible && maxHp > 0f && amount < maxHp * 0.02f) return Tier.Small;
        float now = Time.time;
        if (now - accumLast > Window || now - accumFirst > WindowMax) { accum = 0f; accumFirst = now; }
        accum += amount; accumLast = now;
        float[] th = Thresholds;
        float r = maxHp > 0f ? accum / maxHp : 1f;
        Tier tier = r >= th[2] ? Tier.Deadly : r >= th[1] ? Tier.Big : r >= th[0] ? Tier.Medium : Tier.Small;
        bool lethal = hpBefore - amount <= 0f;
        if (kind == Kind.Player && lethal) tier = Tier.Deadly;          // 플레이어가 쓰러지는 피해는 가장 센 연출

        // 흰 게이지 · 사슬: 닫혀 있으면 새 사슬, 열려 있으면 그 자리 유지 (내려오던 중이면 그 자리에서 멈춤)
        if (phase == Phase.Closed || ghostHp < 0f) { ghostHp = hpBefore; chainStart = clock; holdUntil = clock; }
        else ghostHp = Mathf.Max(ghostHp, hpBefore);
        int kk = (int)kind;
        float cap = chainStart + ChainCap[kk];
        if (clock < cap) { holdUntil = Mathf.Max(holdUntil, Mathf.Min(clock + Hold[(int)tier], cap)); phase = Phase.Hold; }
        else if (phase == Phase.Drain)
        {
            // 상한 뒤 내려오던 중: 연장 없이 지금 자리에서 새 목표로 다시 내려옴 (목표가 갑자기 낮아져 흰 게이지가 순간이동하지 않게)
            float hpAfter = Mathf.Max(0f, hpBefore - amount);
            drainFrom = ghostHp; drainStart = clock;
            drainDur = Mathf.Clamp(FullBar[kk] * (ghostHp - hpAfter) / Mathf.Max(1f, maxHp), DrainMin[kk], DrainMax[kk]);
        }
        // 상한 뒤 머무는 중이면 상한 시각에 내려옴

        if (!visible || (lethal && kind != Kind.Player)) return tier;  // 안 보이거나 바가 꺼질 피해: 연출 생략
        int t = (int)tier;
        if (GameSettings.Flashes && igniteT >= IgniteGap) igniteT = 0f;  // 점화 (모든 등급 · 1초 8회 이하)
        if (GameSettings.Flashes && FlashAmp[t] > 0f)                     // 채움 번쩍임 (간격 안이면 새 정점 없이 세기만 올림)
        {
            if (Time.unscaledTime >= nextFlashAt) { nextFlashAt = Time.unscaledTime + FlashGap; flashAmp = FlashAmp[t]; flashT = 0f; }
            else flashAmp = Mathf.Max(flashAmp, FlashAmp[t]);
        }
        if (t >= (int)Tier.Big && (kind != Kind.Enemy || shaking || enemyShaking < EnemyShakeMax))   // 큰 피해 묶음 (적은 동시 4개 상한)
        {
            if (!shaking && kind == Kind.Enemy) enemyShaking++;
            shaking = true;
            if (GameSettings.Flashes) { backAmp = (kind == Kind.Enemy ? BackFlashEnemy : BackFlash)[t]; backT = 0f; }
            if (GameSettings.ScreenShake)
            {
                float left = shakeLen > 0f ? 1f - Mathf.Clamp01(shakeT / shakeLen) : 0f;
                shakeAmp = Mathf.Max(shakeAmp * left, ShakeAmp[t]);      // 남은 흔들림보다 센 것만 덮어씀
                shakeLen = Mathf.Max(ShakeTime[t], shakeLen * left); shakeT = 0f;
                float pLeft = 1f - Mathf.Clamp01(punchT / PunchTime);
                punchAmp = Mathf.Max(punchAmp * pLeft, PunchAmp[t]); punchT = 0f;
            }
        }
        return tier;
    }

    public struct Frame
    {
        public float front;          // 채움 비율 0~1
        public float ghost;          // 흰 게이지 윗끝 비율 (front 와 같으면 숨김)
        public bool ghostShown;
        public float shakeX, shakeY; // 막대 높이 대비 비율 (그리는 쪽이 × 높이)
        public float flash;          // 채움에 섞을 흰색 0~1 (이미 ×0.7 · 색약 ×0.5 적용 · Deadly 정점은 1.0)
        public float backFlash;      // 프레임 · 테두리 강조 0~1
        public float punch;          // 세로 스케일 증가분 (0 = 평소)
        public float ignite;         // 흰 게이지 밝기 0~1 → GhostColor(ignite)
    }

    static float Shape(float t) => t < FlashPeak ? 1f : 1f - Mathf.Clamp01((t - FlashPeak) / (FlashTime - FlashPeak));

    // 매 프레임. dt 는 HealthBarFeel.Dt (일시정지엔 0 → 흰 게이지 · 효과가 그대로 얼어 있음)
    public void Tick(float hp, float maxHp, float dt, out Frame f)
    {
        hp = Mathf.Max(0f, hp);
        clock += dt;
        // 맞은 직후(일시정지 중 등) 설정을 끄면 남은 연출을 바로 끝냄
        if (!GameSettings.ScreenShake) { shakeT = shakeLen; punchT = PunchTime; }
        if (!GameSettings.Flashes) { flashT = backT = FlashTime; igniteT = IgniteTime; }
        if (ghostHp < 0f) { ghostHp = hp; phase = Phase.Closed; }      // 첫 프레임 동기화
        if (maxHp > 0f && ghostHp > maxHp) ghostHp = maxHp;            // 최대 체력이 줄었을 때
        if (hp >= ghostHp) { ghostHp = hp; phase = Phase.Closed; }     // 회복 · 부활 · 최대 체력 증가: 즉시 닫힘 (회복 잔상은 두지 않음)
        else if (phase == Phase.Closed) ghostHp = hp;                   // 안전장치 (OnDamaged 없이 내려간 체력)
        else if (phase == Phase.Hold && clock >= holdUntil)
        {
            phase = Phase.Drain; drainFrom = ghostHp; drainStart = clock;
            int k = (int)kind;
            drainDur = Mathf.Clamp(FullBar[k] * (ghostHp - hp) / Mathf.Max(1f, maxHp), DrainMin[k], DrainMax[k]);
        }
        if (phase == Phase.Drain)
        {
            float u = (clock - drainStart) / drainDur;
            if (u >= 1f) { ghostHp = hp; phase = Phase.Closed; }
            else { float s = u * u * (3f - 2f * u); ghostHp = Mathf.Lerp(drainFrom, hp, s); }     // smoothstep: 천천히 떨어지기 시작 → 부드럽게 붙음
        }
        f.front = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
        f.ghost = maxHp > 0f ? Mathf.Clamp01(ghostHp / maxHp) : 0f;
        f.ghostShown = f.ghost > f.front + 0.002f;

        // 점화 (ease-out)
        f.ignite = 0f;
        if (igniteT < IgniteTime) { igniteT += dt; float r = 1f - Mathf.Clamp01(igniteT / IgniteTime); f.ignite = r * r; }
        // 채움 번쩍임 (정점 2프레임 + 선형 감소) · 프레임 · 테두리 강조는 같은 모양 · 자기 타이머
        f.flash = 0f;
        if (flashT < FlashTime)
        {
            flashT += dt;
            float mul = GameSettings.ColorBlind ? FillFlashMulCB : FillFlashMul;
            f.flash = flashAmp >= 1f && flashT < FlashPeak ? (GameSettings.ColorBlind ? mul : 1f) : flashAmp * Shape(flashT) * mul;
        }
        f.backFlash = 0f;
        if (backT < FlashTime) { backT += dt; f.backFlash = backAmp * Shape(backT); }
        // 흔들림 (20Hz 감쇠 사인 · (1−t/len)² · 두 축 위상 다르게 — Random 안 씀)
        f.shakeX = f.shakeY = 0f;
        if (shakeT < shakeLen)
        {
            shakeT += dt;
            float k = 1f - Mathf.Clamp01(shakeT / shakeLen); k *= k;
            float w = shakeT * ShakeHz * 6.2832f;
            float mo = GameSettings.Motion;         // 1.1.9~ 「움직임 줄이기」: 흔들림만 없앰 (잃은 양 조각 · 번쩍임 규칙은 그대로)
            f.shakeX = Mathf.Sin(w) * shakeAmp * k * mo;
            f.shakeY = Mathf.Sin(w * 0.77f + 1.3f) * shakeAmp * k * 0.6f * mo;
        }
        // 두께 펀치
        f.punch = 0f;
        if (punchT < PunchTime) { punchT += dt; float r = 1f - Mathf.Clamp01(punchT / PunchTime); f.punch = punchAmp * r * r; }
        // 「큰 피해 묶음」 집계 해제: 흔들림 · 펀치 · 테두리가 모두 끝났을 때
        if (shaking && shakeT >= shakeLen && punchT >= PunchTime && backT >= FlashTime)
        {
            shaking = false;
            if (kind == Kind.Enemy && enemyShaking > 0) enemyShaking--;
        }
    }
}
