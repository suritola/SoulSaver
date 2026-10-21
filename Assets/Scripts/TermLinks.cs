using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 1.4.8~ 용어 툴팁 (docs/review/2026-10-21-UI-정리-기획.md 4절)
// TermLinks.Apply(글칸) 한 줄: 글 속 용어(Glossary)를 밑줄 링크로 감싸고, 마우스를 링크에 올리면 공용 툴팁(TooltipUI)에 정의를 띄움
//   글이 바뀌면(다른 코드가 .text 를 새로 넣으면) 다음 LateUpdate 에서 한 번만 다시 감쌈 — 그대로면 문자열 참조 비교만 (할당 없음)
//   링크 찾기는 포인터가 이 글칸 안에 있을 때만 (EventSystem 의 들어옴 · 나감 → 위를 덮은 창 · 스크롤 마스크도 그대로 따름)
//   링크 id 문자열은 올린 링크가 바뀔 때만 만듦 · 패드를 쓰는 동안은 링크 감지를 끔 (가상 커서가 글 위를 지나며 툴팁이 깜빡이지 않게)
//   툴팁은 내가 띄운 것만 닫음 (TooltipUI.Version) — 다른 칸의 툴팁을 지우지 않음
[DisallowMultipleComponent]
public class TermLinks : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    TMP_Text text;
    string source, wrapped;         // 마지막으로 감싼 원문 · 감싼 결과 (= 지금 text.text 이면 그대로)
    bool inside;
    int hoverLink = -1;
    int shownVersion = -1;
    Vector2 lastPointer = new Vector2(float.NaN, float.NaN);

    // 글칸에 붙임 (이미 붙어 있으면 지금 글을 다시 감쌈) · raycastTarget 을 켬 — 클릭 · 드래그는 부모(버튼 · 스크롤)로 그대로 올라감
    public static TermLinks Apply(TMP_Text t)
    {
        if (t == null) return null;
        TermLinks k = t.GetComponent<TermLinks>();
        if (k == null) k = t.gameObject.AddComponent<TermLinks>();
        k.text = t;
        t.richText = true;
        t.raycastTarget = true;
        k.Refresh();
        return k;
    }

    void Refresh()
    {
        string now = text.text;
        if (ReferenceEquals(now, wrapped)) return;
        if (wrapped != null && now == source)
        {
            text.text = wrapped;                // 같은 원문을 다시 넣음 → 지난 결과를 그대로 (다시 찾지 않음)
        }
        else
        {
            source = now;
            wrapped = Glossary.Link(now);       // 용어가 없으면 같은 참조
            if (!ReferenceEquals(wrapped, now)) text.text = wrapped;
        }
        hoverLink = -1;
        lastPointer = new Vector2(float.NaN, float.NaN);
    }

    void LateUpdate()
    {
        if (text == null) return;
        if (!ReferenceEquals(text.text, wrapped)) { Refresh(); return; }       // 새 글 → 이번 프레임은 감싸기만 (글자 배치가 다음에 갱신됨)
        if (!inside) return;
        if (GamePad.Active) { HideMine(); return; }
        Vector2 p = GameInput.PointerPosition;
        if (p == lastPointer) return;
        lastPointer = p;
        TMP_TextInfo info = text.textInfo;
        int li = info != null && info.linkCount > 0 ? TMP_TextUtilities.FindIntersectingLink(text, p, Cam()) : -1;
        if (li == hoverLink) return;
        hoverLink = li;
        int term = li >= 0 && li < info.linkCount ? Glossary.FromLinkId(info.linkInfo[li].GetLinkID()) : -1;
        if (term < 0) { HideMine(); return; }
        TooltipUI.Ensure(transform);
        TooltipUI.ShowRaw(Glossary.Name(term), Glossary.Def(term));
        shownVersion = TooltipUI.Version;
    }

    Camera Cam()
    {
        Canvas c = text.canvas;
        return c == null || c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        inside = true;
        lastPointer = new Vector2(float.NaN, float.NaN);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inside = false;
        HideMine();
    }

    void OnDisable()
    {
        inside = false;
        HideMine();
    }

    void HideMine()
    {
        hoverLink = -1;
        if (shownVersion < 0) return;
        TooltipUI.HideIf(shownVersion);
        shownVersion = -1;
    }
}
