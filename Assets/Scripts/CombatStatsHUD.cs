using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.1.9~ 전투 통계 (편의성 3차 2 · 설정 → 표시 · 기본 꺼짐): 최근 10초 초당 피해 · 최근 1분 분당 처치
// 자리: 위 가운데 글자(이야기 모드 시계 상자 · 무한 모드 EndlessHud)의 왼쪽 같은 줄 — 보스 이름(위 50~106px) · 체력바보다 위라 보스전에도 안 겹침
//   오른쪽 끝 x = -(가운데 글자 반 폭 + 12) · y -32 · 저주 표시는 시계 오른쪽이라 반대편
// 멈춘 동안은 흐리게(알파 0.5) · 보스 등장 연출 · 튜토리얼 · 자동 조종은 숨김
public class CombatStatsHUD : MonoBehaviour
{
    static CombatStatsHUD instance;

    RectTransform box;
    CanvasGroup group;
    TMP_Text dpsText, kpmText;
    Image dpsIcon, kpmIcon;
    RectTransform clock;
    TMP_Text endlessText;
    float nextTick, goldUntil;
    readonly Queue<float> recent = new Queue<float>();     // 0.25초마다 DPS (지난 10초 평균과 비교)
    int size = -1;

    static readonly Color Bg = new Color(0.05f, 0.04f, 0.07f, 0.7f);
    static readonly Color Text = new Color(1f, 0.92f, 0.75f);
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

    // 시계 · 무한 모드 글자를 만든 뒤 부름 (설정이 꺼져 있어도 만들어 두고 숨김 — 일시정지 중 설정을 켜면 바로 보이게)
    public static void Ensure()
    {
        if (instance != null) { instance.FindAnchor(); return; }
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        GameObject go = new GameObject("CombatStats", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false);
        Transform ammo = canvas.transform.Find("AmmoPanel");
        if (ammo != null) r.SetSiblingIndex(ammo.GetSiblingIndex() + 1);       // HUD 순서 안 (일시정지 · 보상 창 아래)
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(1f, 0.5f);
        Image bg = go.GetComponent<Image>();
        bg.color = Bg;
        bg.raycastTarget = false;
        CombatStatsHUD h = go.AddComponent<CombatStatsHUD>();
        h.box = r;
        h.group = go.GetComponent<CanvasGroup>();
        h.group.blocksRaycasts = false;
        h.group.interactable = false;
        h.dpsIcon = h.Icon(NoticeIcon.Boss);
        h.kpmIcon = h.Icon(NoticeIcon.Purify);
        h.dpsText = h.Label();
        h.kpmText = h.Label();
        h.FindAnchor();
        instance = h;
        Refresh();
    }

    // 설정이 바뀌면 (ESC 메뉴에서 바꿔도 바로)
    public static void Refresh()
    {
        if (instance == null) return;
        instance.ApplySize();
        instance.Tick(true);
    }

    void OnDestroy() { if (instance == this) instance = null; }

    void FindAnchor()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        Transform c = canvas.transform.Find("RunClockBox");
        clock = c as RectTransform;
        Transform e = canvas.transform.Find("EndlessHud");
        endlessText = e != null ? e.GetComponent<TMP_Text>() : null;
    }

    Image Icon(NoticeIcon kind)
    {
        RectTransform r = UIKit.Rect("Icon", transform, Vector2.zero, new Vector2(20f, 20f));
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f);
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = Notice.IconOf(kind);
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    TMP_Text Label()
    {
        TMP_Text t = UIKit.Text(transform, "", 20f, Text, Vector2.zero, new Vector2(80f, 36f), TextAlignmentOptions.Left);
        RectTransform r = t.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f);
        t.enableWordWrapping = false;
        t.fontSizeMin = 12f;
        return t;
    }

    void ApplySize()
    {
        int s = GameSettings.CombatStats;
        if (s == size || box == null) return;
        size = s;
        bool big = s >= 2;
        float w = big ? 280f : 220f, h = big ? 44f : 40f, icon = big ? 24f : 20f, font = big ? 24f : 20f;
        box.sizeDelta = new Vector2(w, h);
        float half = w * 0.5f;
        Place(dpsIcon.rectTransform, 10f, icon);
        Place(dpsText.rectTransform, 14f + icon, half - 18f - icon, h);
        Place(kpmIcon.rectTransform, half + 4f, icon);
        Place(kpmText.rectTransform, half + 8f + icon, half - 16f - icon, h);
        dpsText.fontSizeMax = kpmText.fontSizeMax = font;
        dpsText.fontSize = kpmText.fontSize = font;
    }

    static void Place(RectTransform r, float x, float size)
    {
        r.anchoredPosition = new Vector2(x, 0f);
        r.sizeDelta = new Vector2(size, size);
    }

    static void Place(RectTransform r, float x, float w, float h)
    {
        r.anchoredPosition = new Vector2(x, 0f);
        r.sizeDelta = new Vector2(w, h - 4f);
    }

    void Update() => Tick(false);

    void Tick(bool force)
    {
        bool show = GameSettings.CombatStats > 0 && !TutorialRun.Active && !GameInput.Auto && !StoryDirector.Playing;
        if (group == null) return;
        if (!show) { group.alpha = 0f; return; }
        ApplySize();
        group.alpha = Time.timeScale <= 0f ? 0.5f : 1f;           // 멈춘 동안은 흐리게 (값도 멈춤)
        if (!force && Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + 0.25f;
        // 가운데 글자 왼쪽에 붙임 (무한 모드 글자는 바뀌므로 매번 폭을 다시 잼)
        float half = 75f;
        if (clock == null && endlessText == null) FindAnchor();
        if (clock == null && endlessText != null && endlessText.gameObject.activeInHierarchy)
            half = Mathf.Max(75f, endlessText.GetPreferredValues(endlessText.text).x * 0.5f);
        box.anchoredPosition = new Vector2(-(half + 12f), -32f);
        if (Time.timeScale <= 0f && !force) return;
        float dps = RunStats.Dps;
        float avg = 0f;
        foreach (float v in recent) avg += v;
        avg = recent.Count > 0 ? avg / recent.Count : 0f;
        if (recent.Count >= 8 && avg > 1f && dps >= avg * 1.3f) goldUntil = Time.unscaledTime + 0.5f;      // 지난 10초보다 30% 넘게 높으면 금빛 (색만)
        recent.Enqueue(dps);
        while (recent.Count > 40) recent.Dequeue();
        dpsText.color = Time.unscaledTime < goldUntil ? Gold : Text;
        dpsText.text = Mathf.RoundToInt(dps).ToString("N0") + " <size=75%>" + Loc.T("/초") + "</size>";
        kpmText.text = Mathf.RoundToInt(RunStats.KillsPerMin).ToString("N0") + " <size=75%>" + Loc.T("/분") + "</size>";
    }
}
