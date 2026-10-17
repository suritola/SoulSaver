using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 1.1.9~ 화면 밖 대상을 가리키는 가장자리 표시의 자리 (보물 상자 · 떠돌이 상점 제단 · 황금 고블린 · 중간 보스)
public static class EdgePointer
{
    // 카메라 안이면 false. 밖이면 화면 가장자리 안쪽 — 위 16% · 아래 14% · 옆 5% (최소 1.2칸), 체력 패널 · 경험치 바 같은 HUD 밑에 숨지 않게
    public static bool EdgePoint(Vector3 target, out Vector3 at)
    {
        at = target;
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return false;
        Vector3 c = cam.transform.position;
        float hh = cam.orthographicSize, hw = hh * cam.aspect;
        Vector2 d = (Vector2)(target - c);
        if (Mathf.Abs(d.x) < hw - 0.5f && Mathf.Abs(d.y) < hh - 0.5f) return false;
        float mx = Mathf.Max(1.2f, hw * 2f * 0.05f), my = Mathf.Max(1.2f, hh * 2f * (d.y > 0f ? 0.16f : 0.14f));
        float sx = (hw - mx) / Mathf.Max(0.001f, Mathf.Abs(d.x)), sy = (hh - my) / Mathf.Max(0.001f, Mathf.Abs(d.y));
        Vector2 edge = d * Mathf.Min(sx, sy);
        at = new Vector3(c.x + edge.x, c.y + edge.y, 0f);
        return true;
    }
}

// 1.1.9~ 가장자리 표식 한 개 (편의성 3차 8): 대상 그림 + 뒤 빛 + (있으면) 남은 초 · 깜빡임
// 상자 · 상점 · 고블린 · 중간 보스가 한 코드 · 설정 「화면 밖 표시」로 모두 끔 · 한 번에 가까운 것 5개까지 · 보스 등장 연출 중엔 숨김
public class EdgeMarker : MonoBehaviour
{
    public const int MaxShown = 5;
    static readonly List<EdgeMarker> all = new List<EdgeMarker>();
    static int rankedFrame = -1;

    Transform target;
    System.Func<string> label;
    System.Func<bool> blink, alive;
    SpriteRenderer icon;
    TextMeshPro text;
    float baseScale, pulseAmp;
    Color iconColor;
    bool outside, allowed;
    Vector3 edgeAt;
    float dist;

    // icon · iconColor · scale: 표식 그림 (대상 그림을 그대로) · glow: 뒤 빛 크기 · 색 · pulse: 맥동 폭 (움직임 줄이기면 0)
    public static EdgeMarker Attach(Transform target, Sprite iconSprite, Color iconColor, float scale, float glowSize, Color glowColor, float pulse,
                                    System.Func<string> label = null, System.Func<bool> blink = null, System.Func<bool> alive = null, TMP_FontAsset font = null, Material fontMat = null)
    {
        if (target == null) return null;
        GameObject go = SpecialAbilities.MakeSprite("EdgeMarker", iconSprite != null ? iconSprite : Hostile.Glow, target.position, scale, iconColor, "Effect", 40);
        GameObject glow = SpecialAbilities.MakeSprite("EdgeMarkerGlow", Hostile.Glow, target.position, glowSize, glowColor, "Effect", 39);
        glow.transform.SetParent(go.transform, true);
        EdgeMarker m = go.AddComponent<EdgeMarker>();
        m.target = target;
        m.icon = go.GetComponent<SpriteRenderer>();
        m.iconColor = iconColor;
        m.baseScale = scale;
        m.pulseAmp = pulse;
        m.label = label;
        m.blink = blink;
        m.alive = alive;
        if (label != null)
        {
            GameObject t = new GameObject("EdgeMarkerText", typeof(TextMeshPro));
            t.transform.SetParent(go.transform, false);
            t.transform.localPosition = new Vector3(0f, -1.1f, 0f);
            m.text = t.GetComponent<TextMeshPro>();
            UIKit.EnsureStyle();
            m.text.font = font != null ? font : UIKit.Font;
            if (fontMat != null) m.text.fontSharedMaterial = fontMat;
            else if (font == null && UIKit.FontMaterial != null) m.text.fontSharedMaterial = UIKit.FontMaterial;
            m.text.fontSize = 5f;
            m.text.fontStyle = FontStyles.Bold;
            m.text.color = new Color(1f, 0.9f, 0.55f);
            m.text.alignment = TextAlignmentOptions.Center;
            m.text.sortingLayerID = SortingLayer.NameToID("Effect");
            m.text.sortingOrder = 41;
            m.text.rectTransform.sizeDelta = new Vector2(4f, 1.4f);
        }
        m.SetVisible(false);
        all.Add(m);
        return m;
    }

    void OnDestroy() => all.Remove(this);

    void SetVisible(bool on)
    {
        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(on);
        if (icon != null) icon.enabled = on;
    }

    // 프레임마다 한 번: 화면 밖에 있는 것 가운데 가까운 순 MaxShown 개만 보이게
    static void Rank()
    {
        if (rankedFrame == Time.frameCount) return;
        rankedFrame = Time.frameCount;
        Camera cam = Camera.main;
        Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
        List<EdgeMarker> live = new List<EdgeMarker>();
        foreach (EdgeMarker m in all)
        {
            if (m == null || m.target == null) continue;
            m.outside = EdgePointer.EdgePoint(m.target.position, out m.edgeAt);
            m.dist = ((Vector2)(m.target.position - c)).sqrMagnitude;
            m.allowed = false;
            if (m.outside) live.Add(m);
        }
        live.Sort((a, b) => a.dist.CompareTo(b.dist));
        for (int i = 0; i < live.Count && i < MaxShown; i++) live[i].allowed = true;
    }

    void LateUpdate()
    {
        if (target == null || (alive != null && !alive())) { Destroy(gameObject); return; }
        Rank();
        bool show = outside && allowed && GameSettings.EdgeMarkers && !StoryDirector.Playing;
        SetVisible(show);
        if (!show) return;
        transform.position = edgeAt;
        transform.localScale = Vector3.one * (baseScale + pulseAmp * Mathf.Sin(Time.time * 8f) * GameSettings.Motion);
        bool dim = blink != null && blink() && Mathf.PingPong(Time.time * 6f, 1f) < 0.5f;
        icon.color = dim ? new Color(iconColor.r, iconColor.g, iconColor.b, iconColor.a * 0.4f) : iconColor;
        if (text != null && label != null) text.text = label();
    }

    // 대상이 사라질 때 같이 (상자 · 상점처럼 대상이 직접 지울 때)
    public void Remove() { if (this != null) Destroy(gameObject); }
}
