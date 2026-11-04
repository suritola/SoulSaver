using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// 메뉴 초상 움직임 (C17 · 스킨 시청각 개편 마무리 2026-11-04): 메인 메뉴(MenuHeroPanel) · 영웅 창(CharacterUI) 초상 둘레에 영웅 · 전설 캐릭터 스킨의 테마 움직임
//   SkinLooks 표의 「menu:」 칸 — 「움직임 글리프 개수 크기 키=값」 을 ; 로 이음 (크기 · x · y 는 초상 칸 크기 P 의 비율, 0 = 초상 가운데)
//   움직임: rise 아래에서 떠오름 · fall 위에서 떨어짐 · drift 뒤에서 옆으로 흐름 · twinkle 뒤에서 반짝 · orbit 둘레를 돎 · spin 뒤에서 천천히 돎
//           tremble 뒤에서 떨림 · blink 4초마다 깜빡 · pop 4초마다 톡 튀어나옴 · sway 흔들림 · pulse 뒤 빛이 맥동 · ghost 초상 잔상이 어긋났다 겹침
//           glitch 4초마다 0.1초 초상 어긋남 · shadow 발밑 그림자 일렁
//   UI 이미지 몇 장만 (풀 · 생성 없음) · 실제 시간 · 저사양 · 개편 전 비교 스위치면 없음
public sealed class SkinPortraitFx : MonoBehaviour
{
    sealed class Part
    {
        public string m;
        public RectTransform rt;
        public Image img;
        public Color c;
        public float size, x, y, phase, speed, al = 1f;
    }

    readonly List<Part> parts = new List<Part>();
    RectTransform portrait;
    Image face;
    float P;

    // 초상(RectTransform · Image)에 스킨 움직임을 붙임 — 영웅 · 전설 캐릭터 스킨이고 표에 menu 칸이 있을 때만
    public static void Attach(RectTransform portrait, Image face, SkinDef skin)
    {
        if (portrait == null || skin == null || skin.tier < 2 || SkinFx.Off || GraphicsQuality.Low) return;
        SkinLook look = SkinLooks.Get(skin);
        string recipe = look != null ? look.Fx("menu") : null;
        if (recipe == null) return;
        try
        {
            SkinPortraitFx fx = portrait.gameObject.AddComponent<SkinPortraitFx>();
            fx.portrait = portrait;
            fx.face = face;
            fx.P = Mathf.Max(64f, portrait.sizeDelta.x);
            fx.Build(recipe, look);
        }
        catch (System.Exception e) { Debug.LogException(e); }       // 메뉴 장식이 메뉴를 막지 않게
    }

    RectTransform back, front;

    void Build(string recipe, SkinLook look)
    {
        // 뒤 = 초상 바로 앞 형제 (초상보다 먼저 그려짐) · 앞 = 초상 자식
        back = new GameObject("SkinPortraitBack", typeof(RectTransform)).GetComponent<RectTransform>();
        back.SetParent(portrait.parent, false);
        back.anchorMin = portrait.anchorMin; back.anchorMax = portrait.anchorMax; back.pivot = portrait.pivot;
        back.anchoredPosition = portrait.anchoredPosition;
        back.sizeDelta = portrait.sizeDelta;
        back.SetSiblingIndex(portrait.GetSiblingIndex());
        front = new GameObject("SkinPortraitFront", typeof(RectTransform)).GetComponent<RectTransform>();
        front.SetParent(portrait, false);
        front.anchorMin = front.anchorMax = new Vector2(0.5f, 0.5f);
        front.sizeDelta = portrait.sizeDelta;

        foreach (string raw in recipe.Split(';'))
        {
            string[] t = raw.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (t.Length < 2) continue;
            string m = t[0], glyph = t[1];
            int n = 1; float size = 0.06f, x = 0f, y = 0f, al = 1f; char cc = 'm'; Color custom = Color.white;
            int pos = 0;
            for (int i = 2; i < t.Length; i++)
            {
                string w = t[i];
                int eq = w.IndexOf('=');
                if (eq < 0)
                {
                    float.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out float v);
                    if (pos++ == 0) n = Mathf.Clamp(Mathf.RoundToInt(v), 1, 12); else size = v;
                    continue;
                }
                string k = w.Substring(0, eq), val = w.Substring(eq + 1);
                float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float f);
                if (k == "x") x = f; else if (k == "y") y = f; else if (k == "al") al = f;
                else if (k == "c") { if (val.StartsWith("#") && ColorUtility.TryParseHtmlString(val, out Color hc)) { cc = 'c'; custom = hc; } else cc = val.Length > 0 ? val[0] : 'm'; }
            }
            Color col = cc == 'a' ? look.accent : cc == 'w' ? Color.white : cc == 'k' ? new Color(0.1f, 0.08f, 0.12f) : cc == 'c' ? custom : look.main;
            bool behind = m == "drift" || m == "twinkle" || m == "spin" || m == "tremble" || m == "pulse" || m == "ghost" || m == "shadow" || m == "glitch";
            for (int i = 0; i < n; i++)
            {
                Part p = new Part { m = m, c = col, size = size, x = x, y = y, al = al, phase = i / (float)n + Random.value * 0.15f, speed = Random.Range(0.85f, 1.15f) };
                Sprite sp = m == "ghost" || m == "glitch" ? (face != null ? face.sprite : null) : m == "pulse" || m == "shadow" ? SkinArt.Glow : SkinArt.Get(glyph);
                if (sp == null) continue;
                GameObject g = new GameObject("SkinPortrait_" + m, typeof(RectTransform), typeof(Image));
                p.rt = g.GetComponent<RectTransform>();
                p.rt.SetParent(behind ? back : front, false);
                p.rt.anchorMin = p.rt.anchorMax = new Vector2(0.5f, 0.5f);
                p.img = g.GetComponent<Image>();
                p.img.sprite = sp;
                p.img.raycastTarget = false;
                p.img.preserveAspect = true;
                if (m == "ghost" || m == "glitch") p.rt.sizeDelta = portrait.sizeDelta;
                else
                {
                    float h = size * P, aspect = sp.rect.height > 0f ? sp.rect.width / sp.rect.height : 1f;
                    p.rt.sizeDelta = new Vector2(h * aspect, h);
                }
                if (m == "twinkle" || m == "drift") { p.x = Random.Range(-0.42f, 0.42f); p.y = Random.Range(-0.3f, 0.4f); }
                if (m == "rise" || m == "fall") p.x = x + Random.Range(-0.38f, 0.38f);
                parts.Add(p);
            }
        }
        Tick();
    }

    void Update() => Tick();

    void Tick()
    {
        float now = Time.unscaledTime;
        foreach (Part p in parts)
        {
            if (p.rt == null) continue;
            float a = 1f, rot = 0f, sc = 1f, px = p.x, py = p.y;
            float cyc = (now / 4f * p.speed + p.phase) % 1f;        // 4초 주기 (기획 309줄)
            switch (p.m)
            {
                case "rise":
                    py = Mathf.Lerp(-0.42f, -0.05f, cyc) + p.y; a = Mathf.Sin(cyc * Mathf.PI); px += Mathf.Sin(now * 1.3f + p.phase * 6f) * 0.02f; break;
                case "fall":
                    py = Mathf.Lerp(0.45f, -0.35f, cyc) + p.y; a = Mathf.Sin(cyc * Mathf.PI); rot = now * 60f * p.speed + p.phase * 360f; px += Mathf.Sin(now * 1.7f + p.phase * 6f) * 0.04f; break;
                case "drift":
                    px = Mathf.Repeat(p.x + now * 0.04f * p.speed + 0.5f, 1f) - 0.5f; a = 0.55f * Mathf.Sin(Mathf.Repeat(px + 0.5f, 1f) * Mathf.PI); break;
                case "twinkle":
                    a = Mathf.Max(0f, Mathf.Sin((now * 1.6f * p.speed + p.phase * 6.283f))); a *= a; break;
                case "orbit":
                    { float ang = (now * 0.5f + p.phase) * 6.283f; px = p.x + Mathf.Cos(ang) * 0.4f; py = p.y + Mathf.Sin(ang) * 0.1f; a = Mathf.Sin(ang) < 0f ? 1f : 0.55f; break; }
                case "spin":
                    rot = -now * 12f; a = 0.45f; break;
                case "tremble":
                    rot = Mathf.Sin(now * 31f) * 2.5f; a = 0.6f; break;
                case "blink":
                    a = cyc < 0.15f ? 1f : cyc < 0.25f ? 1f - (cyc - 0.15f) / 0.1f : 0.25f; break;
                case "pop":
                    { float k = cyc / 0.22f; a = cyc < 0.22f ? 1f : 0f; sc = k < 0.3f ? k / 0.3f * 1.25f : k < 0.5f ? Mathf.Lerp(1.25f, 1f, (k - 0.3f) / 0.2f) : 1f; if (cyc >= 0.17f && cyc < 0.22f) a = 1f - (cyc - 0.17f) / 0.05f; break; }
                case "sway":
                    rot = Mathf.Sin(now * 2.2f) * 10f; break;
                case "pulse":
                    a = 0.25f + 0.25f * Mathf.Sin(now * 1.57f); sc = 1f + 0.06f * Mathf.Sin(now * 1.57f); break;
                case "ghost":
                    px = p.x + Mathf.Sin(now * 0.8f) * 0.035f; a = 0.35f; break;
                case "glitch":
                    { bool on = cyc < 0.025f; a = on ? 0.7f : 0f; px = on ? (Random.value - 0.5f) * 0.06f : 0f; break; }
                case "shadow":
                    sc = 1f + 0.12f * Mathf.Sin(now * 1.3f); a = 0.5f; break;
            }
            p.rt.anchoredPosition = new Vector2(px * P, py * P);
            p.rt.localEulerAngles = new Vector3(0f, 0f, rot);
            p.rt.localScale = p.m == "shadow" ? new Vector3(1.6f * sc, 0.35f, 1f) : Vector3.one * sc;
            p.img.color = new Color(p.c.r, p.c.g, p.c.b, Mathf.Clamp01(a) * p.al);
        }
    }

    void OnDestroy()
    {
        if (back != null) Destroy(back.gameObject);
    }
}
