using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHP : MonoBehaviour
{

    public GameObject HPgauge;
    // 체력바 위에 표시되는 "현재 / 최대" 텍스트
    public TextMeshProUGUI hpText;
    public float HPmaxWidth;
    public float playerHP;
    public float playerMaxHP;
    RectTransform gauge;
    int shownHp = -1, shownMax = -1, shownGuard = -1;
    bool guardHintTried;

    // 타격감 (HealthBarFeel): 앞 막대는 실제 체력 · 잃은 양은 뒤에 흰색 지연 게이지(HPghost)로 머물렀다 부드럽게 따라 내려옴 (연속 피해면 머무름 연장)
    readonly HealthBarFeel feel = new HealthBarFeel(HealthBarFeel.Kind.Player);
    RectTransform ghost;
    Image ghostImg, gaugeImg;
    Color gaugeColor;
    RectTransform self;              // 흔들림 · 두께 펀치 대상 = HPbackground (프레임 · 흰 게이지 · 채움 · 글자가 한 몸으로)
    Vector2 restPos;                 // 흔들림 기준 (HPbackground 자기 위치)
    Vector3 baseScale = Vector3.one;
    Image frameImg;                  // HPbackground 프레임 (Big 이상에서 붉은 틴트)
    Color frameColor = Color.white;
    static readonly Color FrameHit = new Color(1f, 0.72f, 0.72f);
    static readonly Color FrameHitCB = new Color(1f, 0.80f, 0.50f);   // 색각 모드: 빨강 대신 주황
    float shownIgnite = -1f;
    float lastHp = -1f, shownFront = -1f, shownGhost = -1f;
    bool damagedThisFrame;
    float hpTextPop;                 // 치명 등급 피해 때 체력 숫자가 잠깐 튐
    bool hpTextPopped;

    // Start is called before the first frame update
    void Start()
    {
        HPmaxWidth = GetComponent<RectTransform>().sizeDelta.x;
        playerHP = FindFirstObjectByType<PlayerController>().PlayerHealth;
        playerMaxHP = FindFirstObjectByType<PlayerController>().PlayerMaxHealth;

        HealthBarFeel.ResetStatics();       // 씬이 바뀌면 흔들리는 적 바 집계를 새로
        gauge = HPgauge.GetComponent<RectTransform>();
        // 스크립트가 HPgauge 자신에 붙어 있어 예전엔 채움만 흔들렸음(어긋남) → 채움의 부모(HPbackground)를 움직임
        self = gauge != null ? gauge.parent as RectTransform : null;
        if (self != null)
        {
            restPos = self.anchoredPosition;
            baseScale = self.localScale;
            frameImg = self.GetComponent<Image>();
            if (frameImg != null) frameColor = frameImg.color;
        }
        gaugeImg = HPgauge.GetComponent<Image>();
        if (gaugeImg != null) gaugeColor = gaugeImg.color;
        // 조각: 채움 막대(HPgauge)와 같은 자리 · 크기의 Image 를 새로 만들어 바로 뒤(형제 순서 한 칸 앞)에 — 길이만 다르게 하면 됨
        // 1.2.0: Instantiate(HPgauge) 로 복제하던 것을 바꿈 — 씬에서 PlayerHP 가 HPgauge 자신에 붙어 있어 복제본의 PlayerHP.Start 가 또 복제하는 연쇄로
        //   프레임마다 HPghost 가 하나씩 늘었음 (판이 갈수록 · 일시정지에도 렉) → HealthBarFeel.MakeGhost 는 스크립트를 복제하지 않음
        ghostImg = HealthBarFeel.MakeGhost(gaugeImg, "HPghost");
        ghost = ghostImg != null ? ghostImg.rectTransform : null;
        feel.Reset(playerHP);
        lastHp = playerHP;
    }

    // 이어하기 (RunSave.Restore 가 체력을 정한 뒤): 조각 · 누적을 지금 체력에 붙임 (가득 → 저장 체력으로 떨어지는 조각이 나오지 않게)
    public void ResetFeel()
    {
        PlayerController p = FindFirstObjectByType<PlayerController>();
        if (p == null) return;
        playerHP = p.PlayerHealth;
        feel.Reset(playerHP);
        lastHp = playerHP;
        damagedThisFrame = false;
    }

    void OnEnable() { PlayerController.Damaged += OnDamaged; }
    void OnDisable() { PlayerController.Damaged -= OnDamaged; feel.Cancel(); }

    // 실제로 체력이 깎였을 때 (PlayerController.TryHit) — 등급을 매기고 받은 피해 숫자를 합산해 띄움
    void OnDamaged(float taken, float before)
    {
        PlayerController p = Cache<PlayerController>.Get;
        if (p == null) return;
        damagedThisFrame = true;
        HealthBarFeel.Tier tier = feel.OnDamaged(taken, before, p.PlayerMaxHealth);
        DamagePopup.ShowHurt(p.transform, taken, tier);
        if (tier == HealthBarFeel.Tier.Deadly) hpTextPop = 0.12f;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerController player = Cache<PlayerController>.Get;
        playerHP = player.PlayerHealth;
        playerMaxHP = player.PlayerMaxHealth;
        if (gauge == null) gauge = HPgauge.GetComponent<RectTransform>();

        // TryHit 밖에서 체력이 내려간 경우 (자가 소모 · 되감기): 조각만 (등급 효과 없음)
        if (!damagedThisFrame && lastHp >= 0f && playerHP < lastHp - 0.01f) feel.OnDamaged(lastHp - playerHP, lastHp, playerMaxHP, visible: false);
        damagedThisFrame = false;
        lastHp = playerHP;

        feel.Tick(playerHP, playerMaxHP, HealthBarFeel.Dt, out HealthBarFeel.Frame f);
        // 값이 같으면 안 씀 (예전엔 매 프레임 sizeDelta 를 썼음)
        if (f.front != shownFront) { shownFront = f.front; gauge.sizeDelta = new Vector2(f.front * HPmaxWidth, gauge.sizeDelta.y); }
        if (ghostImg != null && (f.ghost != shownGhost || ghostImg.enabled != f.ghostShown))     // 맞는 순간엔 흰 게이지 값(맞기 전 체력)이 그대로라 켜짐 여부도 같이 봄
        {
            shownGhost = f.ghost;
            if (ghostImg.enabled != f.ghostShown) { ghostImg.enabled = f.ghostShown; shownIgnite = -1f; }
            if (f.ghostShown) ghost.sizeDelta = new Vector2(f.ghost * HPmaxWidth, gauge.sizeDelta.y);
        }
        if (self != null)
        {
            float h = gauge.sizeDelta.y;                                   // 20px → Big 9px · Deadly 16px
            Vector2 pos = restPos + new Vector2(f.shakeX * h, f.shakeY * h);
            if (pos != self.anchoredPosition) self.anchoredPosition = pos;
            Vector3 sc = new Vector3(baseScale.x, baseScale.y * (1f + f.punch), baseScale.z);
            if (sc != self.localScale) self.localScale = sc;
            if (frameImg != null)
            {
                Color fc = f.backFlash > 0f ? Color.Lerp(frameColor, GameSettings.ColorBlind ? FrameHitCB : FrameHit, f.backFlash) : frameColor;
                if (fc != frameImg.color) frameImg.color = fc;
            }
        }
        if (gaugeImg != null)
        {
            Color gc = f.flash > 0f ? Color.Lerp(gaugeColor, Color.white, f.flash) : gaugeColor;
            if (gc != gaugeImg.color) gaugeImg.color = gc;
        }
        if (ghostImg != null && ghostImg.enabled && f.ignite != shownIgnite) { shownIgnite = f.ignite; ghostImg.color = HealthBarFeel.GhostColor(f.ignite); }
        if (hpText != null && (hpTextPop > 0f || hpTextPopped))
        {
            hpTextPop -= Time.unscaledDeltaTime;
            hpTextPopped = hpTextPop > 0f;
            hpText.transform.localScale = Vector3.one * (hpTextPopped ? 1.2f : 1f);
        }

        // 숫자가 바뀔 때만 글자를 새로 (매 프레임 문자열을 만들지 않게)
        int hp = Mathf.CeilToInt(Mathf.Max(playerHP, 0)), max = Mathf.CeilToInt(playerMaxHP);
        int guard = LastStand.Left;
        if (hpText != null && (hp != shownHp || max != shownMax || guard != shownGuard))
        {
            shownHp = hp;
            shownMax = max;
            shownGuard = guard;
            // 1.0.4~: 이번 장에 「최후의 보루」가 남았으면 숫자 옆에 금빛 ◆ (둘 이상이면 x2)
            hpText.text = hp + " / " + max + (guard > 0 ? "  <color=#FFD95A>◆" + (guard > 1 ? "x" + guard : "") + "</color>" : "");
        }
        // 처음 맞았을 때 한 번: ◆ 의 뜻 (튜토리얼 · 힌트 끄기면 Hints 가 알아서 건너뜀)
        if (!guardHintTried && guard > 0 && PlayerController.HurtCount > 0)
        {
            guardHintTried = true;
            Hints.Show("laststand.hud", "체력 숫자 옆 금빛 ◆ 는 이번 장에 남은 「최후의 보루」입니다. 체력이 30% 아래로 떨어지거나 한 방에 쓰러질 피해를 받으면 발동해, 체력 1 이상으로 버티고 2초 동안 무적이 됩니다.");
        }
    }
}
