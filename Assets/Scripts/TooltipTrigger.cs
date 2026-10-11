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

    bool hovering;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        TooltipUI.Ensure(transform);
        TooltipUI.Show(titleProvider != null ? titleProvider() : title,
                       bodyProvider != null ? bodyProvider() : body);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        TooltipUI.Hide();
    }

    void OnDisable()
    {
        if (hovering) TooltipUI.Hide();
        hovering = false;
    }
}
