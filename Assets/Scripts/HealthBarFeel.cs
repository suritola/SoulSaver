using UnityEngine;
using UnityEngine.UI;

// 체력바 타격감 공용: 두 겹 막대(잃은 양 조각) · 피해 누적 등급 · 흔들림 · 번쩍임 시간표
// 플레이어 HUD(PlayerHP) · 보스 HUD(bossbar) · 적 머리 위(EnemyHealthBar) 가 하나씩 들고 씀. MonoBehaviour 아님 · Tick 에 할당 없음
public sealed class HealthBarFeel
{
    public enum Kind { Player, Boss, Enemy }
    public enum Tier { Small = 0, Medium = 1, Big = 2, Deadly = 3 }

    // ---- 누적 창 (피해 숫자 합산과 같은 값 · DamagePopup 이 이 상수를 읽음)
    public const float WindowPlayer = 1.0f;   // 새로 (예전 받은 피해 숫자는 합산 없음) · 맞은 뒤 무적(0.6 + 재정비 최대 0.3 = 0.9초)보다 길어야 다음 피해와 합쳐짐
    public const float WindowBoss   = 0.6f;   // DamagePopup.BossMerge 0.35 → 0.6
    public const float WindowEnemy  = 0.4f;   // DamagePopup.Merge     0.15 → 0.4
    public const float WindowMax    = 1.5f;   // 첫 피해부터 이 시간이 지나면 창을 새로 시작 (도트 · 접촉 피해가 영원히 쌓여 치명이 되지 않게)

    // ---- 등급 문턱 (누적 피해 / 최대 체력) : [Medium, Big, Deadly]
    static readonly float[] TierPlayer = { 0.05f, 0.15f, 0.30f };   // 기본 체력 100: 5 · 15 · 30 (보스 접촉 25 = Big · 보스 상한 40 = Deadly)
    static readonly float[] TierBoss   = { 0.02f, 0.06f, 0.12f };   // 보스는 체력이 커서 낮게 (필살기 · 진화 폭발이 Big~Deadly)
    static readonly float[] TierEnemy  = { 0.15f, 0.35f, 0.60f };   // 잡몹은 한 방이 크므로 높게 · 쓰러지는 피해는 연출 없음 (바가 꺼짐)

    // ---- 조각
    public const float GhostHold = 0.30f;         // 마지막 피해 후 머무는 시간 (Deadly 는 0.45)
    public const float GhostHoldDeadly = 0.45f;
    public const float GhostFallRate = 8f;        // 1초에 남은 간격의 8배 속도 (지수 감쇠 · 큰 조각은 빨리, 끝은 부드럽게)
    public const float GhostFallMin = 1.5f;       // 최소 속도: 최대 체력의 150%/초 (아무리 작아도 0.2초 안에 붙음)

    // ---- 흔들림 (등급별 · Small/Medium 0) : 진폭은 「막대 높이 대비 비율」 → 그리는 쪽이 자기 단위로 환산 (HUD 는 px, 적 바는 월드)
    static readonly float[] ShakeAmp  = { 0f, 0f, 0.25f, 0.5f };    // Big: 막대 높이 25% · Deadly 50%
    static readonly float[] ShakeTime = { 0f, 0f, 0.18f, 0.30f };
    public const float ShakeHz = 34f;             // 감쇠 사인 (두 축 위상 다르게) — Random 안 씀 (할당 · 지터 없음)
    public const int EnemyShakeMax = 4;           // 잡몹 바가 한 화면에서 동시에 흔들리는 상한 (보스 · 플레이어는 제한 없음)

    // ---- 번쩍임 (등급별 · 막대 채움을 흰색으로 섞는 세기) · 빈도 제한
    static readonly float[] FlashAmp  = { 0f, 0.35f, 0.7f, 1f };    // Medium 살짝, Big 또렷, Deadly 완전 흰색 + 배경까지
    public const float FlashTime = 0.09f;         // 흰색 → 원래 색으로 돌아오는 시간
    public const float FlashGap  = 0.34f;         // 한 막대 1초 3회 이하

    // 등급 → 피해 숫자 크기 배율 (DamagePopup 이 씀)
    public static readonly float[] PopupScale = { 1f, 1.2f, 1.55f, 2f };

    public readonly Kind kind;
    public HealthBarFeel(Kind kind) { this.kind = kind; }

    // 상태 (전부 값 타입)
    float ghostHp = -1f;          // 조각 윗끝 (체력 단위로 저장 — 최대 체력이 바뀌어도 비율이 맞음)
    float holdUntil;              // 이 시각까지 조각이 머무름 (Time.unscaledTime)
    float accum, accumFirst = -99f, accumLast = -99f;   // 누적 피해 · 창 시작 · 마지막 피해 (Time.time)
    float shakeAmp, shakeT, shakeLen;     // 지금 흔들림 (막대 높이 비율 · 경과 · 길이)
    float flashAmp, flashT = FlashTime, nextFlashAt;   // 지금 번쩍임
    bool shaking;                 // 상한 집계용
    static int enemyShaking;      // 지금 흔들리는 잡몹 바 수

    // 씬이 바뀔 때 한 번 (지난 씬에서 집계가 남지 않게)
    public static void ResetStatics() => enemyShaking = 0;

    // 조각(잃은 양) 막대 만들기: 채움 막대와 같은 자리 · 크기 · 그림의 Image 하나만 새로 만들어 바로 뒤(형제 순서 한 칸 앞)에 둠. 스크립트 · 자식은 전혀 복제하지 않음
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

    // 그리는 쪽이 「변화 없음 → 갱신 생략」 판단에 씀
    public bool Active => ghostHp > 0f && (holdUntil > 0f || shakeT < shakeLen || flashT < FlashTime);

    public float Window => kind == Kind.Player ? WindowPlayer : kind == Kind.Boss ? WindowBoss : WindowEnemy;
    float[] Thresholds => kind == Kind.Player ? TierPlayer : kind == Kind.Boss ? TierBoss : TierEnemy;

    // 씬 시작 · 풀에서 다시 꺼냄 · 부활 · 이어하기: 조각을 지금 체력에 붙임 · 효과 모두 끔
    public void Reset(float hp)
    {
        ghostHp = Mathf.Max(0f, hp);
        holdUntil = 0f; accum = 0f; accumFirst = accumLast = -99f;
        if (shaking && kind == Kind.Enemy && enemyShaking > 0) enemyShaking--;
        shaking = false; shakeT = shakeLen = 0f; flashT = FlashTime; flashAmp = 0f;
    }
    public void Cancel() => Reset(ghostHp);     // 오브젝트가 꺼질 때 (OnDisable) 상한 집계에서 빠지게

    // 피해 한 번. hpBefore: 깎기 전 체력 · visible: 화면에 보이는가 (false 면 조각만 두고 흔들림 · 번쩍임은 예약 안 함)
    // 반환: 이번 누적의 등급 (피해 숫자 크기에 씀)
    public Tier OnDamaged(float amount, float hpBefore, float maxHp, bool visible = true)
    {
        float now = Time.time;
        if (now - accumLast > Window || now - accumFirst > WindowMax) { accum = 0f; accumFirst = now; }
        accum += amount; accumLast = now;
        float[] th = Thresholds;
        float r = maxHp > 0f ? accum / maxHp : 1f;
        Tier tier = r >= th[2] ? Tier.Deadly : r >= th[1] ? Tier.Big : r >= th[0] ? Tier.Medium : Tier.Small;

        // 조각: 맞기 직전 체력 자리에 (떨어지던 중이면 그 자리에서 다시 멈춤) · 다시 기다림
        ghostHp = Mathf.Max(ghostHp < 0f ? hpBefore : ghostHp, hpBefore);
        holdUntil = Time.unscaledTime + (tier == Tier.Deadly ? GhostHoldDeadly : GhostHold);

        if (!visible || hpBefore - amount <= 0f) return tier;      // 안 보이거나 이 피해로 쓰러짐(바가 꺼짐): 연출 생략

        int t = (int)tier;
        if (ShakeAmp[t] > 0f && GameSettings.ScreenShake)
        {
            if (kind != Kind.Enemy || shaking || enemyShaking < EnemyShakeMax)
            {
                if (!shaking && kind == Kind.Enemy) enemyShaking++;
                shaking = true;
                float left = shakeLen > 0f ? 1f - Mathf.Clamp01(shakeT / shakeLen) : 0f;
                shakeAmp = Mathf.Max(shakeAmp * left, ShakeAmp[t]);      // 남은 흔들림보다 센 것만 덮어씀
                shakeT = 0f; shakeLen = Mathf.Max(ShakeTime[t], shakeLen * left);
            }
        }
        if (FlashAmp[t] > 0f && GameSettings.Flashes && Time.unscaledTime >= nextFlashAt)
        {
            nextFlashAt = Time.unscaledTime + FlashGap;
            flashAmp = FlashAmp[t]; flashT = 0f;
        }
        return tier;
    }

    public struct Frame
    {
        public float front;      // 앞 막대 비율 0~1
        public float ghost;      // 조각 윗끝 비율 (front 와 같으면 조각 숨김)
        public bool ghostShown;
        public float shakeX, shakeY;   // 막대 높이 대비 비율 (그리는 쪽이 × 높이)
        public float flash;      // 0~1 채움 색에 섞을 흰색
        public bool flashFull;   // Deadly 번쩍임 중 (배경까지 번쩍임)
    }

    // 매 프레임. 실제 시간(unscaledDt)으로 — 사망 슬로모션 · 멈춤 중에도 자연스럽게 (DamageFlash 와 같은 원칙)
    public void Tick(float hp, float maxHp, float unscaledDt, out Frame f)
    {
        hp = Mathf.Max(0f, hp);
        if (ghostHp < 0f) ghostHp = hp;                       // 첫 프레임 동기화
        if (maxHp > 0f && ghostHp > maxHp) ghostHp = maxHp;   // 최대 체력이 줄었을 때
        if (hp >= ghostHp) { ghostHp = hp; holdUntil = 0f; }  // 회복 · 부활 · 최대 체력 증가: 조각 없이 즉시 (회복 조각은 두지 않음)
        else if (Time.unscaledTime >= holdUntil)
        {
            float gap = ghostHp - hp;
            ghostHp = Mathf.MoveTowards(ghostHp, hp, Mathf.Max(gap * GhostFallRate, maxHp * GhostFallMin) * unscaledDt);
            if (ghostHp <= hp + 0.001f) { ghostHp = hp; holdUntil = 0f; }
        }
        f.front = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
        f.ghost = maxHp > 0f ? Mathf.Clamp01(ghostHp / maxHp) : 0f;
        f.ghostShown = f.ghost > f.front + 0.002f;

        f.shakeX = f.shakeY = 0f;
        if (shakeT < shakeLen)
        {
            shakeT += unscaledDt;
            float k = 1f - Mathf.Clamp01(shakeT / shakeLen);
            float w = shakeT * ShakeHz * 6.2832f;
            f.shakeX = Mathf.Sin(w) * shakeAmp * k;
            f.shakeY = Mathf.Sin(w * 0.77f + 1.3f) * shakeAmp * k * 0.6f;
            if (shakeT >= shakeLen && shaking) { shaking = false; if (kind == Kind.Enemy && enemyShaking > 0) enemyShaking--; }
        }
        f.flash = 0f;
        if (flashT < FlashTime) { flashT += unscaledDt; f.flash = flashAmp * (1f - Mathf.Clamp01(flashT / FlashTime)); }
        f.flashFull = f.flash > 0f && flashAmp >= 1f;
    }

    // 조각 색 (색약 모드면 흰빛 — 주황이 빨강 · 초록 막대와 섞여 안 보이지 않게)
    public static Color GhostColor(float flash) => GameSettings.ColorBlind
        ? new Color(0.96f, 0.96f, 0.9f, 0.95f)
        : Color.Lerp(new Color(1f, 0.62f, 0.22f, 0.95f), new Color(1f, 0.92f, 0.8f, 1f), flash);
}
