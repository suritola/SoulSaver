using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 이 UI에 마우스를 올리면 TooltipUI로 설명을 보여줌
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string title;
    [TextArea(2, 6)]
    public string body;

    // 내용이 바뀌는 경우 (예: 능력 레벨) 코드에서 채워 넣음
    public Func<string> titleProvider;
    public Func<string> bodyProvider;

    // 1.0.4~: 전투 HUD 칸 (레벨업 스킬 · 특수 능력 칸) — 발사를 막지 않고 (PlayerController.PointerOverUI),
    // 전투 중엔 공격 버튼을 떼고 잠깐 머물러야 설명이 뜸 (구석의 적을 쏘다가 설명창이 화면을 가리지 않게) · 멈춘 동안은 바로
    public bool hud;
    const float HudDelay = 0.35f;

    bool hovering, shown;
    float hoverSince;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        hoverSince = Time.unscaledTime;
        if (!hud || HudReady()) ShowNow();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        if (shown) TooltipUI.Hide();
        shown = false;
    }

    void Update()
    {
        if (!hud || !hovering) return;
        // 공격을 누르는 동안엔 계속 기다림 (떼고 다시 머물면 뜸)
        if (Time.timeScale > 0f && GameInput.FireHeld) hoverSince = Time.unscaledTime;
        bool ready = HudReady();
        if (ready && !shown) ShowNow();
        else if (!ready && shown) { TooltipUI.Hide(); shown = false; }
    }

    bool HudReady() => Time.timeScale <= 0f || (!GameInput.FireHeld && Time.unscaledTime - hoverSince >= HudDelay);

    void ShowNow()
    {
        shown = true;
        TooltipUI.Ensure(transform);
        TooltipUI.Show(titleProvider != null ? titleProvider() : title,
                       bodyProvider != null ? bodyProvider() : body);
    }

    void OnDisable()
    {
        if (hovering && shown) TooltipUI.Hide();
        hovering = false;
        shown = false;
    }
}
