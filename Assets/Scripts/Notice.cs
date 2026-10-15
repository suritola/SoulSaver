using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 게임 중 안내 메시지 (1.1.2~ 전면 개편): 예전엔 알림판(StageManager.ShowBanner) · 도움말 · 키 알림 · 머리 위 글자가 저마다 다른 자리 · 크기 · 색이라
//   적 수십 마리 · 이펙트 · 평타 · 맵 물체 금빛 맥동 속에서 640×72 반투명 띠의 14~26pt 글자가 묻혔음 → 한 체계로
//
//   ① 큰 알림 Notice.Big   — 보스 · 이벤트 · 새 스킬/유물 · 각인 · 무게 · 클리어 · 해금처럼 꼭 봐야 하는 것
//        화면 가운데 위(가운데에서 +170) 가로 전체 어두운 띠 + 위아래 강조색 줄 · 아이콘(또는 키캡) · 제목 56pt · 설명 30pt
//        튀어나오며 번쩍(「번쩍임 효과」를 끄면 번쩍 · 튐 없이 서서히) · 효과음 · 한 번에 하나, 나머지는 줄을 섬 (급한 것 먼저)
//   ② 토스트 Notice.Toast  — 이정표 · 도감 · 줍기 결과 · 상자 · 영혼 트리 같은 보통 소식
//        오른쪽 위(코인 · 키 안내 아래)에 아이콘 + 글 카드, 화면 밖 오른쪽에서 밀려 들어와 아래로 쌓임 · 최대 4개, 넘치면 오래된 것부터
//   ③ 도움말 Hints.Show(RunStats.cs) — 화면 아래 가운데 패널 · 키캡 색 강조 · [Tab] 닫기 · 처음 한 번 (그림은 여기 TipPanel 을 씀)
//   + 진행 줄 Notice.Objective — 장 중반 이벤트의 「남은 적 · 남은 시간」 (위 가운데 알약 모양 한 줄)
//
//   모든 글: 진한 외곽선 + 그림자(TMP outline · underlay) + 어두운 바탕 · 숫자 · 「」 · [키] 는 색 강조 · 1080p 기준 MinFont(22) 아래로 줄지 않음
//   고르기 창 · 레벨업 창 · 영혼 트리 · 일시정지 · 이야기 연출 중(Held)에는 숨기고 시간을 멈췄다가 닫히면 이어서
//   설정 「안내 메시지 크기」(보통 0.86 · 크게 1 · 아주 크게 1.18, 기본 크게) = GameSettings.NoticeSize
public enum NoticeIcon { None, Info, Warn, Boss, Star, Weight, Up, Chest, Coin, Shards, Soul, Curse, Codex, Evolve, Unlock, Relic, Shield, Revive, Tree, Magnet, Purify, Meat, Hourglass, Shop }

// 큰 알림 우선순위: Urgent(이벤트 예고 · 보스 · 부활처럼 지금 일어나는 일)는 줄 맨 앞, 앞 알림을 짧게 끊음 · Important(해금 · 클리어)는 끝까지 보임
public enum NoticeLevel { Normal, Important, Urgent }

public static class Notice
{
    // ================================================================= 설정 · 기준
    public static readonly string[] SizeNames = { "notice.size.0", "notice.size.1", "notice.size.2" };
    static readonly float[] SizeMul = { 0.86f, 1f, 1.18f };
    public static float Scale => SizeMul[Mathf.Clamp(GameSettings.NoticeSize, 0, SizeMul.Length - 1)];
    public const float MinFont = 22f;          // 1080p 기준 화면에 보이는 가장 작은 글자 (크기 배율을 곱한 뒤)
    public static float Min(float baseMin) => Mathf.Max(baseMin, MinFont / Scale);

    const float BigY = 170f;                   // 큰 알림 띠 가운데 (화면 가운데에서 위로) — 위쪽 시계 · 보스 체력바 · 플레이어 머리와 겹치지 않는 높이
    const float ToastTop = -214f, ToastRight = -24f, ToastW = 470f, ToastGap = 8f;      // 오른쪽 위 코인(96) · 포인트 · 키 안내(-193까지) 아래
    const int MaxToasts = 4, MaxBigQueue = 6;
    const float ObjectiveY = -150f;

    public static readonly Color Gold = new Color(1f, 0.84f, 0.35f), Red = new Color(1f, 0.4f, 0.34f), Orange = new Color(1f, 0.62f, 0.25f),
        Violet = new Color(0.78f, 0.62f, 1f), Sky = new Color(0.45f, 0.85f, 1f), Green = new Color(0.5f, 1f, 0.62f), Teal = new Color(0.45f, 0.95f, 0.85f),
        Pink = new Color(1f, 0.5f, 0.85f), Parch = new Color(0.95f, 0.92f, 0.86f);
    const string NumHex = "#FFD966", KeyHex = "#FFE07A";
    static readonly Color BgDark = new Color(0.035f, 0.025f, 0.06f, 0.9f);

    public static Color AccentOf(NoticeIcon i)
    {
        switch (i)
        {
            case NoticeIcon.Warn: return Orange;
            case NoticeIcon.Boss: return Red;
            case NoticeIcon.Weight: case NoticeIcon.Soul: case NoticeIcon.Shards: case NoticeIcon.Tree: case NoticeIcon.Relic: return Violet;
            case NoticeIcon.Curse: return Pink;
            case NoticeIcon.Codex: case NoticeIcon.Hourglass: return Teal;
            case NoticeIcon.Info: case NoticeIcon.Revive: return Sky;
            case NoticeIcon.Up: case NoticeIcon.Meat: return Green;
            case NoticeIcon.Magnet: return new Color(1f, 0.55f, 0.5f);
            case NoticeIcon.Purify: return new Color(1f, 0.97f, 0.8f);
            default: return Gold;
        }
    }

    // ================================================================= 공개 API
    // 큰 알림: 첫 줄 = 제목, 나머지 = 설명 (예전 알림판 글 「제목\n설명」을 그대로 넘겨도 됨)
    public static void Big(string text, NoticeIcon icon = NoticeIcon.Star, float seconds = 3f, NoticeLevel level = NoticeLevel.Normal, Color? accent = null, Sprite sprite = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        int nl = text.IndexOf('\n');
        Big(nl < 0 ? text : text.Substring(0, nl), nl < 0 ? null : text.Substring(nl + 1), icon, seconds, level, accent, sprite);
    }

    public static void Big(string title, string sub, NoticeIcon icon, float seconds = 3f, NoticeLevel level = NoticeLevel.Normal, Color? accent = null, Sprite sprite = null)
    {
        Push(new BigItem
        {
            title = Clean(title), sub = Clean(sub), icon = sprite != null ? sprite : IconOf(icon),
            accent = accent ?? AccentOf(icon), seconds = Mathf.Max(1.2f, seconds), level = level,
            warn = icon == NoticeIcon.Warn || icon == NoticeIcon.Boss,
        });
    }

    // 키캡이 달린 큰 알림 (새 스킬 · 유물: 「[E] 전투 함성 / E 키를 눌러 사용」) — tag 는 제목 위 작은 글 (「새 스킬」)
    public static void BigKey(string key, string tag, string title, string sub, Color accent, float seconds = 4.2f)
    {
        // 빈 키 판정은 부르는 쪽(KeyCode 로)에서 — 「-」는 '-'(Minus) 키의 정상 이름이기도 해서 여기서 바꾸지 않음
        Push(new BigItem { key = string.IsNullOrEmpty(key) ? "?" : key, tag = tag, title = Clean(title), sub = Clean(sub), accent = accent, seconds = seconds, level = NoticeLevel.Important });
    }

    // 토스트: 첫 줄 = 굵게, 둘째 줄부터 작게 · 같은 글이 떠 있으면 새로 띄우지 않고 시간을 늘리며 「×2」
    public static void Toast(NoticeIcon icon, string text, float seconds = 3.5f, Color? accent = null, Sprite sprite = null)
    {
        if (string.IsNullOrEmpty(text) || !Ensure()) return;
        text = Clean(text);
        foreach (ToastItem t in toasts)
            if (!t.dying && t.key == text)
            {
                t.count++;
                t.life = Mathf.Max(t.life, t.age + seconds);
                t.bump = 0f;
                t.label.text = ToastText(t);
                return;
            }
        ToastItem n = BuildToast(text, sprite != null ? sprite : IconOf(icon), accent ?? AccentOf(icon), seconds);
        if (n == null) return;
        toasts.Insert(0, n);
        int alive = 0;
        for (int i = 0; i < toasts.Count; i++)
            if (!toasts[i].dying && ++alive > MaxToasts) toasts[i].dying = true;
        if (!Held) Sound("toast"); else n.soundPending = true;
    }

    // 장 중반 이벤트 진행 줄 (매 프레임 불러도 됨 · null 이면 숨김)
    public static void Objective(string text, Color? accent = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            if (objective != null && objective.gameObject.activeSelf) objective.gameObject.SetActive(false);
            objectiveText = null;
            return;
        }
        if (!Ensure()) return;
        if (objective == null) { BuildObjective(); objectiveText = null; }
        Color a = accent ?? Gold;
        if (text != objectiveText || a != objectiveAccent)
        {
            objectiveText = text;
            objectiveAccent = a;
            objectiveLabel.text = Emph(text, ColorHex(a));
            float S = Scale;
            objectiveLabel.fontSizeMin = Min(22f);
            objectiveLabel.enableAutoSizing = false;
            objectiveLabel.fontSize = objectiveLabel.fontSizeMax;
            float w = Mathf.Min(1000f, objectiveLabel.GetPreferredValues(objectiveLabel.text, 1000f, 0f).x);
            objectiveLabel.enableAutoSizing = true;
            objective.sizeDelta = new Vector2(w + 56f, 50f);
            objective.localScale = Vector3.one * S;
            objectiveBar.color = a;
        }
        bool show = !Held;
        if (objective.gameObject.activeSelf != show) objective.gameObject.SetActive(show);
    }

    // 큰 알림이 떠 있거나 기다리는 중 (재촉 알림처럼 급하지 않은 것은 이때 미룸)
    public static bool Busy => bigNow != null || bigQueue.Count > 0;

    // 옛 알림판 (StageManager.ShowBanner) — 짧은 한 줄은 토스트, 나머지는 큰 알림
    public static void Legacy(string text, float seconds, bool keep)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (!keep && seconds <= 1.6f && text.IndexOf('\n') < 0) { Toast(NoticeIcon.Info, text, Mathf.Max(2.5f, seconds + 1.2f)); return; }
        Big(text, NoticeIcon.None, Mathf.Max(2.2f, seconds), keep ? NoticeLevel.Important : NoticeLevel.Normal);
    }

    // 창 · 일시정지 · 연출 중: 알림을 숨기고 시간을 멈춤
    static LevelShop shop;
    static float shopFindAt;
    public static bool Held
    {
        get
        {
            if (Time.timeScale == 0f || ChoiceUI.Open || ESCmenu.IsOpen || SoulTreeUI.IsOpen || SettingsUI.IsOpen || StoryDirector.Playing
                || SkillEvolutionUI.Open || WeaponEvolutionUI.Open) return true;
            StageManager sm = StageManager.Instance;
            if (sm != null && sm.IsMenuOpen) return true;
            if (shop == null && Time.unscaledTime >= shopFindAt)
            {
                shopFindAt = Time.unscaledTime + 1f;
                shop = Object.FindFirstObjectByType<LevelShop>();
            }
            return shop != null && shop.IsOpen;
        }
    }

    // ================================================================= 글자 모양 (다른 곳에서도 씀: 도움말 · 튜토리얼 띠)
    static Material textMat;
    static TMP_FontAsset textMatFont;

    // 진한 외곽선 + 아래 오른쪽 그림자: 밝은 이펙트 · 금빛 물체 위에서도 글자 가장자리가 보임
    public static Material TextMaterial
    {
        get
        {
            UIKit.EnsureStyle();
            TMP_FontAsset f = UIKit.Font;
            if (f == null || f.material == null) return UIKit.FontMaterial;
            if (textMat != null && textMatFont == f) return textMat;
            textMatFont = f;
            textMat = new Material(f.material) { name = "NoticeText" };
            textMat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            textMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.24f);
            textMat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.03f, 0.02f, 0.05f, 1f));
            textMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
            if (textMat.HasProperty(ShaderUtilities.ID_UnderlayColor))
            {
                textMat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                textMat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.8f));
                textMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.55f);
                textMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.65f);
                textMat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.3f);
                textMat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.3f);
            }
            return textMat;
        }
    }

    // 월드 글자(머리 위 「전투 함성!」 같은 것 · SpecialFeedback.FloatText)용: 같은 외곽선을 월드 글자 셰이더 재질로 (폰트마다 하나)
    static readonly Dictionary<TMP_FontAsset, Material> worldMats = new Dictionary<TMP_FontAsset, Material>();
    public static Material WorldMaterial(TMP_FontAsset f)
    {
        if (f == null || f.material == null) return null;
        if (worldMats.TryGetValue(f, out Material m) && m != null) return m;
        m = new Material(f.material) { name = "NoticeWorldText" };
        m.EnableKeyword(ShaderUtilities.Keyword_Outline);
        m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.3f);
        m.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.05f, 0.03f, 0.06f, 1f));
        m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
        worldMats[f] = m;
        return m;
    }

    public static void Style(TMP_Text t, float size, float min = 0f)
    {
        if (t == null) return;
        UIKit.EnsureStyle();
        if (UIKit.Font != null) t.font = UIKit.Font;
        Material m = TextMaterial;
        if (m != null) t.fontSharedMaterial = m;
        t.fontSize = size;
        t.fontSizeMax = size;
        t.fontSizeMin = Min(min > 0f ? min : size * 0.75f);
        t.fontStyle = FontStyles.Bold;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
        t.richText = true;
        t.raycastTarget = false;
    }

    static TextMeshProUGUI NewText(Transform parent, string name, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        Style(t, size);
        t.color = color;
        t.alignment = align;
        return t;
    }

    static Image NewImage(Transform parent, string name, Sprite sprite, Color color, bool sliced = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (sliced && sprite != null) img.type = Image.Type.Sliced;
        return img;
    }

    static void Stretch(RectTransform r, Vector2 min, Vector2 max)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = min;
        r.offsetMax = max;
    }

    static readonly Regex SizeTag = new Regex("</?size[^>]*>");
    // 예전 알림 글에 박혀 있던 <size=70%> 같은 크기 태그는 지움 (크기는 단계마다 이 체계가 정함)
    static string Clean(string s) => string.IsNullOrEmpty(s) ? s : SizeTag.Replace(s, "").Trim('\n', ' ');

    public static string ColorHex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    // 숫자(+30% · ×2 · 1/3) · 「이름」 · [키] 에 색을 입힘 (이미 색 태그 안인 글자 · 태그 안은 그대로)
    public static string Emph(string s, string accentHex)
    {
        if (string.IsNullOrEmpty(s)) return s;
        StringBuilder sb = new StringBuilder(s.Length + 32);
        int colorDepth = 0;
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '<')
            {
                int j = s.IndexOf('>', i);
                if (j < 0) { sb.Append(s, i, s.Length - i); break; }
                string tag = s.Substring(i, j - i + 1);
                if (tag.StartsWith("<color")) colorDepth++;
                else if (tag.StartsWith("</color")) colorDepth = Mathf.Max(0, colorDepth - 1);
                sb.Append(tag);
                i = j + 1;
                continue;
            }
            if (colorDepth > 0) { sb.Append(c); i++; continue; }
            if (c == '「')
            {
                int j = s.IndexOf('」', i);
                if (j > i && s.IndexOf('<', i, j - i) < 0)
                {
                    sb.Append("<color=").Append(accentHex).Append('>').Append(s, i, j - i + 1).Append("</color>");
                    i = j + 1;
                    continue;
                }
            }
            if (c == '[')
            {
                int j = s.IndexOf(']', i);
                if (j > i && j - i <= 16 && s.IndexOf('<', i, j - i) < 0 && s.IndexOf('\n', i, j - i) < 0)
                {
                    sb.Append("<color=").Append(KeyHex).Append(">").Append(s, i, j - i + 1).Append("</color>");
                    i = j + 1;
                    continue;
                }
            }
            bool sign = (c == '+' || c == '-' || c == '×' || c == 'x') && i + 1 < s.Length && char.IsDigit(s[i + 1]) && (i == 0 || !char.IsLetterOrDigit(s[i - 1]));
            if (char.IsDigit(c) || sign)
            {
                int j = i + 1;
                while (j < s.Length && (char.IsDigit(s[j]) || ((s[j] == '.' || s[j] == ',' || s[j] == '/') && j + 1 < s.Length && char.IsDigit(s[j + 1])))) j++;
                if (j < s.Length && s[j] == '%') j++;
                sb.Append("<color=").Append(NumHex).Append('>').Append(s, i, j - i).Append("</color>");
                i = j;
                continue;
            }
            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    // ================================================================= 판 · 바탕
    static RectTransform root;
    static NoticeRunner runner;
    static int sceneHandle = -1;
    public static RectTransform Root => Ensure() ? root : null;

    static bool Ensure()
    {
        int s = SceneManager.GetActiveScene().handle;
        if (s != sceneHandle)
        {
            sceneHandle = s;
            bigQueue.Clear();
            bigNow = null;
            toasts.Clear();
            objectiveText = null;
        }
        if (runner != null) return true;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return false;
        UIKit.EnsureStyle();
        GameObject go = new GameObject("Notices", typeof(RectTransform), typeof(CanvasGroup));
        root = (RectTransform)go.transform;
        root.SetParent(canvas.transform, false);
        // HUD 순서 안 (탄약 패널 바로 뒤): 일시정지 · 고르기 창은 이 위에 그려짐
        Transform ammo = canvas.transform.Find("AmmoPanel");
        if (ammo != null) root.SetSiblingIndex(ammo.GetSiblingIndex() + 1);
        else root.SetAsLastSibling();
        Stretch(root, Vector2.zero, Vector2.zero);
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        runner = go.AddComponent<NoticeRunner>();
        // 지난 판(이 판의 UI 가 사라짐)의 것은 버림
        toasts.Clear();
        bigNow = null;
        bigView = null;
        toastCol = null;
        objective = null;
        objectiveText = null;
        return true;
    }

    // ================================================================= ① 큰 알림
    sealed class BigItem
    {
        public string title, sub, tag, key;
        public Sprite icon;
        public Color accent;
        public float seconds;
        public NoticeLevel level;
        public bool warn;
    }

    sealed class BigView
    {
        public RectTransform band, content, icon, cap;
        public CanvasGroup cg;
        public Image bg, lineTop, lineBot, flash, iconImg, glow, capImg;
        public TextMeshProUGUI tag, title, sub, capText;
    }

    static readonly List<BigItem> bigQueue = new List<BigItem>();
    static BigItem bigNow;
    static BigView bigView;
    static float bigT, bigEnd, bigGap;

    static void Push(BigItem it)
    {
        if (it == null || string.IsNullOrEmpty(it.title) || !Ensure()) return;
        // 같은 글이 떠 있거나 이미 줄에 있으면 또 넣지 않음
        if (bigNow != null && bigNow.title == it.title && bigNow.sub == it.sub) return;
        foreach (BigItem q in bigQueue) if (q.title == it.title && q.sub == it.sub) return;
        // 급한 것 먼저: 같은 등급끼리는 온 차례대로
        int at = bigQueue.Count;
        while (at > 0 && bigQueue[at - 1].level < it.level) at--;
        bigQueue.Insert(at, it);
        if (bigQueue.Count > MaxBigQueue)
        {
            // 가장 덜 중요한 것 중 가장 오래 기다린 것을 버림
            int drop = -1;
            for (int i = 0; i < bigQueue.Count; i++)
                if (drop < 0 || bigQueue[i].level < bigQueue[drop].level) drop = i;
            bigQueue.RemoveAt(drop);
        }
    }

    static BigView BuildBig()
    {
        BigView v = new BigView();
        GameObject go = new GameObject("BigNotice", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        v.band = (RectTransform)go.transform;
        v.band.SetParent(root, false);
        v.band.anchorMin = new Vector2(0f, 0.5f);
        v.band.anchorMax = new Vector2(1f, 0.5f);
        v.band.pivot = new Vector2(0.5f, 0.5f);
        v.band.anchoredPosition = new Vector2(0f, BigY);
        v.band.sizeDelta = new Vector2(0f, 150f);
        v.cg = go.GetComponent<CanvasGroup>();
        v.cg.blocksRaycasts = false;
        v.cg.interactable = false;
        v.bg = go.GetComponent<Image>();
        v.bg.sprite = BandSprite;
        v.bg.color = new Color(0.03f, 0.02f, 0.06f, 0.88f);
        v.bg.raycastTarget = false;

        v.lineTop = NewImage(v.band, "LineTop", BandSprite, Gold);
        v.lineBot = NewImage(v.band, "LineBottom", BandSprite, Gold);
        foreach (Image l in new[] { v.lineTop, v.lineBot })
        {
            RectTransform r = l.rectTransform;
            bool top = l == v.lineTop;
            r.anchorMin = new Vector2(0f, top ? 1f : 0f);
            r.anchorMax = new Vector2(1f, top ? 1f : 0f);
            r.sizeDelta = new Vector2(0f, 4f);
            r.anchoredPosition = Vector2.zero;
        }
        v.flash = NewImage(v.band, "Flash", BandSprite, new Color(1f, 1f, 1f, 0f));
        Stretch(v.flash.rectTransform, Vector2.zero, Vector2.zero);

        v.content = UIKit.Rect("Content", v.band, Vector2.zero, new Vector2(1000f, 120f));
        // 아이콘 뒤 강조색 빛 (아이콘보다 먼저 그림)
        v.glow = NewImage(v.content, "Glow", SpecialAbilities.GlowSprite, new Color(1f, 1f, 1f, 0f));
        v.glow.rectTransform.sizeDelta = new Vector2(190f, 190f);
        v.iconImg = NewImage(v.content, "Icon", null, Color.white);
        v.iconImg.preserveAspect = true;
        v.icon = v.iconImg.rectTransform;
        // 키캡 (새 스킬 · 유물)
        v.capImg = NewImage(v.content, "Cap", SpecialAbilities.KeyCapSprite, Color.white, true);
        v.capImg.pixelsPerUnitMultiplier = 0.3f;          // 테두리 · 옆면을 굵게
        v.cap = v.capImg.rectTransform;
        v.capText = NewText(v.cap, "CapText", 54f, new Color(0.1f, 0.08f, 0.12f), TextAlignmentOptions.Center);
        if (UIKit.Font != null && UIKit.Font.material != null) v.capText.fontSharedMaterial = UIKit.Font.material;     // 밝은 키캡 위 진한 글자는 외곽선 없이
        v.capText.enableWordWrapping = false;
        v.capText.enableAutoSizing = true;
        v.capText.fontSizeMin = 18f;
        Stretch(v.capText.rectTransform, new Vector2(10f, 22f), new Vector2(-10f, -8f));

        v.tag = NewText(v.content, "Tag", 26f, Gold, TextAlignmentOptions.Center);
        v.title = NewText(v.content, "Title", 56f, new Color(1f, 0.97f, 0.9f), TextAlignmentOptions.Center);
        v.sub = NewText(v.content, "Sub", 30f, Parch, TextAlignmentOptions.Center);
        go.SetActive(false);
        return v;
    }

    static void ShowBig(BigItem it)
    {
        if (bigView == null || bigView.band == null) bigView = BuildBig();
        BigView v = bigView;
        v.band.gameObject.SetActive(true);
        float S = Scale;
        string hex = ColorHex(Color.Lerp(it.accent, Color.white, 0.25f));
        bool hasCap = !string.IsNullOrEmpty(it.key);
        bool hasIcon = !hasCap && it.icon != null;
        bool hasTag = !string.IsNullOrEmpty(it.tag);
        bool hasSub = !string.IsNullOrEmpty(it.sub);

        // 키캡 폭은 글자 길이에 맞춰 (한글 · 가나는 글자당 넓게)
        float capW = 0f;
        if (hasCap)
        {
            float est = 0f;
            foreach (char ch in it.key) est += ch >= 0x2E80 ? 46f : 30f;
            capW = Mathf.Clamp(44f + est, 104f, 240f);
        }
        float iconW = hasCap ? capW : hasIcon ? 104f : 0f;
        float gap = iconW > 0f ? 30f : 0f;
        float screenW = root.rect.width > 10f ? root.rect.width : 1920f;
        float textMax = Mathf.Clamp((screenW - 160f) / S - iconW - gap, 360f, 1300f);
        TextAlignmentOptions align = iconW > 0f ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;

        // 글자 크기 · 상자: 원래 크기로 재고(줄바꿈 포함), 설명이 3줄을 넘으면 그 높이 안에서 줄임
        v.tag.gameObject.SetActive(hasTag);
        v.sub.gameObject.SetActive(hasSub);
        v.tag.color = it.accent;
        v.tag.text = hasTag ? it.tag : "";
        v.title.text = Emph(it.title, hex);
        v.sub.text = hasSub ? Emph(it.sub, hex) : "";
        foreach (TextMeshProUGUI t in new[] { v.tag, v.title, v.sub })
        {
            t.enableAutoSizing = false;
            t.fontSize = t.fontSizeMax;
            t.alignment = align;
        }
        v.title.fontSizeMin = Min(38f);
        v.sub.fontSizeMin = Min(24f);
        v.tag.fontSizeMin = Min(22f);
        Vector2 tp = v.title.GetPreferredValues(v.title.text, textMax, 0f);
        float titleW = Mathf.Min(textMax, tp.x), titleH = Mathf.Min(tp.y, 150f);
        float subW = 0f, subH = 0f;
        if (hasSub)
        {
            Vector2 sp = v.sub.GetPreferredValues(v.sub.text, textMax, 0f);
            subW = Mathf.Min(textMax, sp.x);
            subH = Mathf.Min(sp.y, 128f);
            if (sp.y > 128f) { v.sub.enableAutoSizing = true; subW = textMax; }
        }
        if (tp.y > 150f) { v.title.enableAutoSizing = true; titleW = textMax; }
        float tagW = 0f, tagH = 0f;
        if (hasTag)
        {
            Vector2 gp = v.tag.GetPreferredValues(v.tag.text, textMax, 0f);
            tagW = Mathf.Min(textMax, gp.x);
            tagH = gp.y;
        }
        float blockW = Mathf.Max(titleW, Mathf.Max(subW, tagW)) + 8f;
        float blockH = tagH + titleH + (hasSub ? 6f + subH : 0f);
        float contentW = iconW + gap + blockW;
        float contentH = Mathf.Max(blockH, iconW > 0f ? 108f : 0f);
        v.band.sizeDelta = new Vector2(0f, (contentH + 44f) * S);
        v.content.sizeDelta = new Vector2(contentW, contentH);
        v.content.localScale = Vector3.one * S;

        float left = -contentW * 0.5f;
        v.icon.gameObject.SetActive(hasIcon);
        if (v.glow.sprite == null) v.glow.sprite = SpecialAbilities.GlowSprite;
        v.glow.gameObject.SetActive(hasIcon && v.glow.sprite != null);
        v.cap.gameObject.SetActive(hasCap);
        if (hasIcon)
        {
            v.iconImg.sprite = it.icon;
            v.icon.sizeDelta = new Vector2(104f, 104f);
            v.icon.anchoredPosition = new Vector2(left + 52f, 0f);
            v.glow.rectTransform.anchoredPosition = v.icon.anchoredPosition;
            v.glow.color = new Color(it.accent.r, it.accent.g, it.accent.b, 0.5f);
        }
        if (hasCap)
        {
            v.cap.sizeDelta = new Vector2(capW, 104f);
            v.cap.anchoredPosition = new Vector2(left + capW * 0.5f, 2f);
            v.capText.text = it.key;
            capRest = v.cap.anchoredPosition;
        }
        float bx = left + iconW + gap + blockW * 0.5f, y = blockH * 0.5f;
        if (hasTag) { Place(v.tag.rectTransform, bx, y - tagH * 0.5f, blockW, tagH); y -= tagH; }
        Place(v.title.rectTransform, bx, y - titleH * 0.5f, blockW, titleH);
        y -= titleH + 6f;
        if (hasSub) Place(v.sub.rectTransform, bx, y - subH * 0.5f, blockW, subH);

        v.lineTop.color = v.lineBot.color = it.accent;
        v.bg.color = Color.Lerp(new Color(0.03f, 0.02f, 0.06f, 0.88f), new Color(it.accent.r * 0.3f, it.accent.g * 0.3f, it.accent.b * 0.3f, 0.9f), 0.35f);
        v.cg.alpha = 0f;
        Sound(it.warn ? "warn" : "big");
    }

    static Vector2 capRest;

    static void Place(RectTransform r, float x, float y, float w, float h)
    {
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float k = x - 1f;
        return 1f + c3 * k * k * k + c1 * k * k;
    }

    static float EaseOut(float x) => 1f - (1f - x) * (1f - x) * (1f - x);

    const float BigIn = 0.32f, BigOutTime = 0.3f;

    internal static void TickBig(bool held, float dt)
    {
        if (bigNow == null)
        {
            if (held || bigQueue.Count == 0) { if (bigView != null && bigView.band != null && bigView.band.gameObject.activeSelf) bigView.band.gameObject.SetActive(false); return; }
            if (bigGap > 0f) { bigGap -= dt; return; }
            bigNow = bigQueue[0];
            bigQueue.RemoveAt(0);
            bigT = 0f;
            bigEnd = bigNow.seconds;
            ShowBig(bigNow);
        }
        BigView v = bigView;
        if (v == null || v.band == null) { bigNow = null; return; }
        if (held) { v.cg.alpha = 0f; return; }       // 숨기고 시간을 멈춤 (창을 닫으면 이어서)
        bigT += dt;

        // 기다리는 알림이 있으면 짧게 끊음: 보통 1.5초 · 급한 것이 오면 0.8초 · 중요(해금 · 클리어)는 급한 것이 올 때만 1.2초 · 급한 것은 끝까지
        if (bigQueue.Count > 0 && bigT < bigEnd)
        {
            NoticeLevel next = bigQueue[0].level;
            float min = bigNow.level == NoticeLevel.Urgent ? bigEnd
                      : bigNow.level == NoticeLevel.Important ? (next == NoticeLevel.Urgent ? 1.2f : bigEnd)
                      : (next == NoticeLevel.Urgent ? 0.8f : 1.5f);
            float cut = Mathf.Max(min, bigT);
            if (cut < bigEnd) bigEnd = cut;
        }

        bool fx = GameSettings.Flashes;
        float S = Scale;
        if (bigT < BigIn)
        {
            float k = bigT / BigIn;
            v.cg.alpha = Mathf.Clamp01(bigT / 0.12f);
            float open = fx ? EaseOut(Mathf.Clamp01(bigT / 0.16f)) : 1f;
            v.band.localScale = new Vector3(1f, Mathf.Max(0.05f, open), 1f);
            float pop = fx ? Mathf.LerpUnclamped(1.35f, 1f, EaseOutBack(k)) : 1f;
            v.content.localScale = Vector3.one * (S * pop);
            v.content.anchoredPosition = Vector2.zero;
            v.flash.color = new Color(1f, 1f, 1f, fx ? 0.5f * (1f - k) : 0f);
        }
        else if (bigT < bigEnd)
        {
            v.cg.alpha = 1f;
            v.band.localScale = Vector3.one;
            v.content.localScale = Vector3.one * S;
            v.flash.color = new Color(1f, 1f, 1f, 0f);
        }
        else
        {
            float o = (bigT - bigEnd) / BigOutTime;
            if (o >= 1f)
            {
                v.band.gameObject.SetActive(false);
                bigNow = null;
                bigGap = 0.1f;
                return;
            }
            v.cg.alpha = 1f - o;
            v.content.anchoredPosition = new Vector2(0f, 18f * o);
        }
        // 강조색 줄이 은은히 숨 쉬고, 아이콘이 살짝 들썩 · 키캡은 0.9초마다 눌리는 시늉
        float breath = fx ? 0.5f + 0.5f * Mathf.Sin(bigT * 5f) : 1f;
        Color a = bigNow.accent;
        v.lineTop.color = v.lineBot.color = new Color(a.r, a.g, a.b, 0.7f + 0.3f * breath);
        if (v.icon.gameObject.activeSelf) v.icon.localScale = Vector3.one * (fx ? 1f + 0.05f * Mathf.Sin(bigT * 4f) : 1f);
        if (v.cap.gameObject.activeSelf)
        {
            bool pressed = bigT > 0.5f && Mathf.Repeat(bigT - 0.5f, 0.9f) < 0.12f;
            v.cap.anchoredPosition = capRest + (pressed ? new Vector2(0f, -6f) : Vector2.zero);
            v.capImg.color = pressed ? new Color(0.84f, 0.84f, 0.84f) : Color.white;
        }
    }

    // ================================================================= ② 토스트
    sealed class ToastItem
    {
        public string key, text;
        public Color accent;
        public float life, age, dieT, y, h, bump = 1f;
        public int count = 1;
        public bool dying, placed, soundPending;
        public RectTransform r;
        public CanvasGroup cg;
        public Image bg, bar;
        public TextMeshProUGUI label;
    }

    static readonly List<ToastItem> toasts = new List<ToastItem>();       // [0] = 가장 새것 (맨 위)
    static RectTransform toastCol;
    static CanvasGroup toastGroup;

    static string ToastText(ToastItem t)
    {
        string hex = ColorHex(Color.Lerp(t.accent, Color.white, 0.2f));
        int nl = t.text.IndexOf('\n');
        string s = nl < 0 ? Emph(t.text, hex) : Emph(t.text.Substring(0, nl), hex) + "\n<size=82%><color=#DCD5C8>" + Emph(t.text.Substring(nl + 1), hex) + "</color></size>";
        if (t.count > 1) s += " <color=" + NumHex + ">×" + t.count + "</color>";
        return s;
    }

    static ToastItem BuildToast(string text, Sprite icon, Color accent, float seconds)
    {
        if (toastCol == null)
        {
            GameObject cgo = new GameObject("Toasts", typeof(RectTransform), typeof(CanvasGroup));
            toastCol = (RectTransform)cgo.transform;
            toastCol.SetParent(root, false);
            toastCol.anchorMin = toastCol.anchorMax = toastCol.pivot = new Vector2(1f, 1f);
            toastCol.anchoredPosition = new Vector2(ToastRight, ToastTop);
            toastCol.sizeDelta = new Vector2(ToastW, 400f);
            toastGroup = cgo.GetComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
        }
        toastCol.localScale = Vector3.one * Scale;

        ToastItem t = new ToastItem { key = text, text = text, accent = accent, life = seconds };
        GameObject go = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        t.r = (RectTransform)go.transform;
        t.r.SetParent(toastCol, false);
        t.r.anchorMin = t.r.anchorMax = t.r.pivot = new Vector2(1f, 1f);
        t.cg = go.GetComponent<CanvasGroup>();
        t.cg.alpha = 0f;
        t.bg = go.GetComponent<Image>();
        t.bg.sprite = PanelSprite;
        t.bg.type = Image.Type.Sliced;
        t.bg.color = BgDark;
        t.bg.raycastTarget = false;
        // 테두리: 강조색 얇은 판을 뒤에 (바탕보다 3px 크게)
        Image rim = NewImage(t.r, "Rim", PanelSprite, new Color(accent.r, accent.g, accent.b, 0.55f), true);
        Stretch(rim.rectTransform, new Vector2(-2f, -2f), new Vector2(2f, 2f));
        rim.transform.SetAsFirstSibling();
        Image fill = NewImage(t.r, "Fill", PanelSprite, BgDark, true);
        Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);
        t.bg.color = new Color(0f, 0f, 0f, 0f);         // 실제 바탕은 Fill (Rim 위)
        t.bg = fill;
        t.bar = NewImage(t.r, "Bar", PanelSprite, accent, true);
        RectTransform br = t.bar.rectTransform;
        br.anchorMin = new Vector2(0f, 0f);
        br.anchorMax = new Vector2(0f, 1f);
        br.pivot = new Vector2(0f, 0.5f);
        br.sizeDelta = new Vector2(7f, -8f);
        br.anchoredPosition = new Vector2(4f, 0f);

        bool hasIcon = icon != null;
        float textLeft = hasIcon ? 74f : 24f;
        if (hasIcon)
        {
            Image ic = NewImage(t.r, "Icon", icon, Color.white);
            ic.preserveAspect = true;
            RectTransform ir = ic.rectTransform;
            ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
            ir.sizeDelta = new Vector2(46f, 46f);
            ir.anchoredPosition = new Vector2(44f, 0f);
        }
        t.label = NewText(t.r, "Text", 26f, Parch, TextAlignmentOptions.Left);
        t.label.fontSizeMin = Min(22f);
        t.label.text = ToastText(t);
        float tw = ToastW - textLeft - 16f;
        float th = t.label.GetPreferredValues(t.label.text, tw, 0f).y;
        if (th > 110f) { th = 110f; t.label.enableAutoSizing = true; }
        t.h = Mathf.Max(62f, th + 20f);
        RectTransform lr = t.label.rectTransform;
        lr.anchorMin = lr.anchorMax = new Vector2(0f, 0.5f);
        lr.pivot = new Vector2(0f, 0.5f);
        lr.sizeDelta = new Vector2(tw, th);
        lr.anchoredPosition = new Vector2(textLeft, 0f);
        t.r.sizeDelta = new Vector2(ToastW, t.h);
        return t;
    }

    internal static void TickToasts(bool held, float dt)
    {
        if (toastCol == null) return;
        toastGroup.alpha = held ? 0f : 1f;
        if (held || toasts.Count == 0) return;
        bool fx = GameSettings.Flashes;
        float y = 0f;
        for (int i = 0; i < toasts.Count; i++)
        {
            ToastItem t = toasts[i];
            if (t.r == null) { toasts.RemoveAt(i); i--; continue; }
            if (t.soundPending) { t.soundPending = false; Sound("toast"); }
            t.age += dt;
            t.bump += dt;
            if (!t.dying && t.age >= t.life) t.dying = true;
            float alpha, slide, room = 1f;
            if (t.dying)
            {
                t.dieT += dt;
                float k = t.dieT / 0.3f;
                if (k >= 1f) { Object.Destroy(t.r.gameObject); toasts.RemoveAt(i); i--; continue; }
                alpha = 1f - k;
                slide = 60f * k;
                room = 1f - k;
            }
            else
            {
                float k = Mathf.Clamp01(t.age / 0.24f);
                alpha = k;
                slide = (1f - EaseOut(k)) * (ToastW * 0.6f);      // 화면 밖 오른쪽에서 밀려 들어옴
            }
            if (!t.placed) { t.y = y; t.placed = true; }
            t.y = Mathf.Lerp(t.y, y, 1f - Mathf.Exp(-14f * dt));
            t.r.anchoredPosition = new Vector2(slide, -t.y);
            t.cg.alpha = alpha;
            // 들어온 순간 · 「×2」 때 바탕이 강조색으로 번쩍
            float f = fx ? Mathf.Clamp01(1f - Mathf.Min(t.age, t.bump) / 0.45f) : 0f;
            t.bg.color = Color.Lerp(BgDark, new Color(t.accent.r * 0.55f, t.accent.g * 0.55f, t.accent.b * 0.55f, 0.95f), f * 0.6f);
            float pop = fx && t.bump < 0.15f ? 1f + 0.06f * (1f - t.bump / 0.15f) : 1f;
            t.r.localScale = Vector3.one * pop;
            y += (t.h + ToastGap) * room;
        }
    }

    // ================================================================= 진행 줄
    static RectTransform objective;
    static TextMeshProUGUI objectiveLabel;
    static Image objectiveBar;
    static string objectiveText;
    static Color objectiveAccent;

    static void BuildObjective()
    {
        GameObject go = new GameObject("Objective", typeof(RectTransform), typeof(Image));
        objective = (RectTransform)go.transform;
        objective.SetParent(root, false);
        objective.anchorMin = objective.anchorMax = objective.pivot = new Vector2(0.5f, 1f);
        objective.anchoredPosition = new Vector2(0f, ObjectiveY);
        Image bg = go.GetComponent<Image>();
        bg.sprite = PanelSprite;
        bg.type = Image.Type.Sliced;
        bg.color = BgDark;
        bg.raycastTarget = false;
        objectiveBar = NewImage(objective, "Bar", PanelSprite, Gold, true);
        RectTransform br = objectiveBar.rectTransform;
        br.anchorMin = new Vector2(0f, 0f);
        br.anchorMax = new Vector2(0f, 1f);
        br.pivot = new Vector2(0f, 0.5f);
        br.sizeDelta = new Vector2(7f, -10f);
        br.anchoredPosition = new Vector2(6f, 0f);
        objectiveLabel = NewText(objective, "Text", 28f, new Color(1f, 0.9f, 0.72f), TextAlignmentOptions.Center);
        objectiveLabel.enableWordWrapping = false;
        objectiveLabel.enableAutoSizing = true;
        Stretch(objectiveLabel.rectTransform, new Vector2(22f, 2f), new Vector2(-14f, -2f));
    }

    internal static void TickObjective(bool held)
    {
        if (objective == null || objectiveText == null) return;
        if (objective.gameObject.activeSelf == held) objective.gameObject.SetActive(!held);
    }

    // ================================================================= ③ 도움말 패널 (Hints 가 내용 · 줄 · 저장을 맡음)
    public sealed class TipPanel
    {
        public RectTransform r;
        public CanvasGroup cg;
        public TextMeshProUGUI head, body, close;
        public Image bar, rim;
        public float h;
    }

    const float TipW = 1040f, TipY = 236f;

    public static TipPanel BuildTip()
    {
        if (!Ensure()) return null;
        TipPanel p = new TipPanel();
        GameObject go = new GameObject("Tip", typeof(RectTransform), typeof(CanvasGroup));
        p.r = (RectTransform)go.transform;
        p.r.SetParent(root, false);
        p.r.anchorMin = p.r.anchorMax = p.r.pivot = new Vector2(0.5f, 0f);
        p.r.anchoredPosition = new Vector2(0f, TipY);      // 레벨업 대기 배지(110 ~ 220) · 특수 강화 버튼 위
        p.cg = go.GetComponent<CanvasGroup>();
        p.cg.blocksRaycasts = false;
        p.rim = NewImage(p.r, "Rim", PanelSprite, new Color(Sky.r, Sky.g, Sky.b, 0.95f), true);
        Stretch(p.rim.rectTransform, new Vector2(-3f, -3f), new Vector2(3f, 3f));
        Image fill = NewImage(p.r, "Fill", PanelSprite, new Color(0.04f, 0.04f, 0.09f, 0.96f), true);
        Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

        Image icon = NewImage(p.r, "Icon", IconOf(NoticeIcon.Info), Color.white);
        icon.preserveAspect = true;
        RectTransform ir = icon.rectTransform;
        ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
        ir.sizeDelta = new Vector2(60f, 60f);
        ir.anchoredPosition = new Vector2(52f, 4f);

        p.head = NewText(p.r, "Head", 24f, Sky, TextAlignmentOptions.TopLeft);
        p.head.enableWordWrapping = false;
        RectTransform hr = p.head.rectTransform;
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0f, 1f);
        hr.sizeDelta = new Vector2(500f, 30f);
        hr.anchoredPosition = new Vector2(98f, -12f);
        p.close = NewText(p.r, "Close", 22f, new Color(0.78f, 0.76f, 0.82f), TextAlignmentOptions.TopRight);
        p.close.enableWordWrapping = false;
        RectTransform cr = p.close.rectTransform;
        cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(1f, 1f);
        cr.sizeDelta = new Vector2(300f, 30f);
        cr.anchoredPosition = new Vector2(-20f, -12f);
        p.body = NewText(p.r, "Body", 28f, Color.white, TextAlignmentOptions.TopLeft);
        p.body.fontSizeMin = Min(22f);
        RectTransform tr = p.body.rectTransform;
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0f, 1f);
        tr.anchoredPosition = new Vector2(98f, -46f);

        p.bar = NewImage(p.r, "Time", BandSprite, Sky);
        RectTransform pr = p.bar.rectTransform;
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0f);
        pr.anchoredPosition = new Vector2(0f, 6f);
        go.SetActive(false);
        return p;
    }

    // 도움말 글을 넣고 크기를 맞춤 (closeKey: 「[Tab] 닫기」 · 패드면 빈 글)
    public static void FillTip(TipPanel p, string head, string body, string closeKey)
    {
        if (p == null || p.r == null) return;
        float S = Scale;
        p.r.gameObject.SetActive(true);         // 글자 크기를 재려면 켜져 있어야 함 (Hints.Tick 이 곧바로 나타나는 움직임을 맡음)
        p.cg.alpha = 0f;
        p.r.localScale = Vector3.one * S;
        p.head.text = head;
        p.close.text = closeKey;
        p.body.enableAutoSizing = false;
        p.body.fontSize = p.body.fontSizeMax;
        p.body.fontSizeMin = Min(22f);
        p.body.text = Emph(body, ColorHex(Sky));
        float bw = TipW - 98f - 28f;
        float bh = p.body.GetPreferredValues(p.body.text, bw, 0f).y;
        if (bh > 140f) { bh = 140f; p.body.enableAutoSizing = true; }
        p.body.rectTransform.sizeDelta = new Vector2(bw, bh);
        p.h = Mathf.Max(104f, 46f + bh + 24f);
        p.r.sizeDelta = new Vector2(TipW, p.h);
        p.bar.rectTransform.sizeDelta = new Vector2(TipW - 40f, 4f);
    }

    // 도움말 닫기 키: [Tab] (다른 동작에 Tab 을 묶었으면 없음) · 패드는 시간이 지나면 저절로
    public static bool TipCloseKeyFree
    {
        get
        {
            for (int i = 0; i <= (int)GameAction.Relic; i++)
                if (KeyBindings.Get((GameAction)i) == KeyCode.Tab) return false;
            return true;
        }
    }

    // ================================================================= 소리 (코드로 만든 짧은 음)
    static void Sound(string kind)
    {
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        if (fx == null || Application.isBatchMode) return;
        switch (kind)
        {
            case "big":
                fx.MakeClip("nt_big", 0.6f, BigWave);
                fx.PlayRaw("nt_big", 0.55f);
                break;
            case "warn":
                fx.MakeClip("nt_warn", 0.5f, WarnWave);
                fx.PlayRaw("nt_warn", 0.6f);
                break;
            default:
                fx.MakeClip("nt_toast", 0.16f, ToastWave);
                fx.PlayRaw("nt_toast", 0.4f);
                break;
        }
    }

    static float Sn(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);

    // 쓸어 올리는 바람 소리 + 미 · 시 두 음 종
    static float BigWave(float t, float d, System.Random r)
    {
        float n = (float)(r.NextDouble() * 2.0 - 1.0);
        float whoosh = n * 0.18f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.16f)) * Mathf.Clamp01(1f - t / 0.16f);
        float a = (Sn(659f, t) + Sn(1318f, t) * 0.25f) * Mathf.Exp(-t * 7f);
        float b = t < 0.09f ? 0f : (Sn(988f, t) + Sn(1976f, t) * 0.2f) * Mathf.Exp(-(t - 0.09f) * 5f);
        return whoosh + (a * 0.32f + b * 0.36f) * Mathf.Clamp01(t / 0.01f);
    }

    // 낮게 두 번 울리는 경고음
    static float WarnWave(float t, float d, System.Random r)
    {
        float s = 0f;
        foreach (float at in new[] { 0f, 0.2f })
        {
            float u = t - at;
            if (u < 0f || u > 0.16f) continue;
            float env = Mathf.Clamp01(u / 0.008f) * Mathf.Pow(1f - u / 0.16f, 1.5f);
            s += (Sn(440f, u) * 0.5f + Mathf.Sign(Sn(220f, u)) * 0.12f + Sn(110f, u) * 0.4f) * env;
        }
        return s * 0.55f;
    }

    static float ToastWave(float t, float d, System.Random r) => (Sn(1760f, t) * 0.25f + Sn(2637f, t) * 0.1f) * Mathf.Exp(-t * 28f) * Mathf.Clamp01(t / 0.004f);

    // ================================================================= 그림 (코드로 그린 도트 · 판)
    static readonly Dictionary<NoticeIcon, Sprite> icons = new Dictionary<NoticeIcon, Sprite>();

    public static Sprite IconOf(NoticeIcon i)
    {
        if (i == NoticeIcon.None) return null;
        if (icons.TryGetValue(i, out Sprite s) && s != null) return s;
        string file = null;
        switch (i)
        {
            case NoticeIcon.Chest: file = "choice_chest"; break;
            case NoticeIcon.Coin: case NoticeIcon.Shop: file = "menu_coin"; break;
            case NoticeIcon.Shards: file = "choice_shards"; break;
            case NoticeIcon.Soul: file = "choice_soul"; break;
            case NoticeIcon.Curse: file = "choice_curse"; break;
            case NoticeIcon.Codex: file = "menu_codex"; break;
            case NoticeIcon.Evolve: file = "menu_evolution"; break;
            case NoticeIcon.Unlock: file = "menu_character"; break;
            case NoticeIcon.Relic: file = "prop_sarcophagus"; break;
            case NoticeIcon.Shield: file = "tree_s_laststand"; break;
            case NoticeIcon.Revive: file = "tree_s_revive"; break;
            case NoticeIcon.Tree: file = "tree_o_root"; break;
            case NoticeIcon.Magnet: file = "drop_magnet"; break;
            case NoticeIcon.Purify: file = "drop_purify"; break;
            case NoticeIcon.Meat: file = "drop_meat"; break;
            case NoticeIcon.Hourglass: file = "drop_hourglass"; break;
        }
        if (file != null) s = Resources.Load<Sprite>("Icons/" + file);
        if (s == null) s = DrawIcon(i);
        icons[i] = s;
        return s;
    }

    public static Sprite IconFile(string file) => string.IsNullOrEmpty(file) ? null : Resources.Load<Sprite>("Icons/" + file);

    // 20×20 도트 아이콘: 모양 함수가 0 = 빔 · 1 = 채움 · 2 = 진한 무늬, 채움 가장자리는 저절로 진한 테두리 · 위로 갈수록 밝게
    static Sprite DrawIcon(NoticeIcon i)
    {
        const int N = 20;
        System.Func<float, float, int> shape;
        Color main;
        switch (i)
        {
            case NoticeIcon.Boss: shape = Skull; main = new Color(0.93f, 0.9f, 0.84f); break;
            case NoticeIcon.Warn: shape = WarnTri; main = Orange; break;
            case NoticeIcon.Weight: shape = Kettle; main = Violet; break;
            case NoticeIcon.Up: shape = UpArrow; main = Green; break;
            case NoticeIcon.Info: shape = InfoDot; main = Sky; break;
            default: shape = Star; main = Gold; break;
        }
        int[] m = new int[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                m[y * N + x] = shape(x + 0.5f, y + 0.5f);
        int At(int x, int y) => x < 0 || y < 0 || x >= N || y >= N ? 0 : m[y * N + x];
        Color ink = new Color(0.08f, 0.05f, 0.08f, 1f);
        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "NoticeIcon_" + i };
        Color[] px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                int v = At(x, y);
                if (v == 0) { px[y * N + x] = new Color(0f, 0f, 0f, 0f); continue; }
                bool rim = At(x - 1, y) == 0 || At(x + 1, y) == 0 || At(x, y - 1) == 0 || At(x, y + 1) == 0;
                if (v == 2 || rim) { px[y * N + x] = ink; continue; }
                float k = y / (float)(N - 1);
                Color c = Color.Lerp(main * 0.7f, Color.Lerp(main, Color.white, 0.45f), k);
                c.a = 1f;
                px[y * N + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply(false, true);
        Sprite s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        s.name = tex.name;
        return s;
    }

    static float D(float x, float y, float cx, float cy) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

    static int Skull(float x, float y)
    {
        bool head = D(x, y, 10f, 11.5f) <= 8.2f && y >= 6f;
        bool jaw = x >= 5f && x <= 15f && y >= 1.5f && y < 8f;
        if (!head && !jaw) return 0;
        if (D(x, y, 6.9f, 10.2f) <= 2.3f || D(x, y, 13.1f, 10.2f) <= 2.3f) return 2;       // 눈
        if (y >= 6.5f && y <= 8f && Mathf.Abs(x - 10f) <= 0.9f) return 2;                   // 코
        if (y >= 2f && y <= 5f && (Mathf.Abs(x - 8f) < 0.5f || Mathf.Abs(x - 12f) < 0.5f)) return 2;     // 이
        return 1;
    }

    static int WarnTri(float x, float y)
    {
        // 꼭짓점이 위인 삼각형 + 가운데 「!」
        if (y < 1.5f || y > 18.8f) return 0;
        float half = (18.8f - y) * 0.55f;
        if (Mathf.Abs(x - 10f) > half + 0.5f) return 0;
        if (Mathf.Abs(x - 10f) <= 1.2f && ((y >= 7f && y <= 14.5f) || (y >= 3.2f && y <= 5.2f))) return 2;
        return 1;
    }

    static int Kettle(float x, float y)
    {
        float r = D(x, y, 10f, 7.5f);
        if (r <= 6.8f) return y > 6.5f && y < 8.5f && Mathf.Abs(x - 10f) < 3.5f ? 2 : 1;
        float h = D(x, y, 10f, 14.2f);
        if (y >= 12f && h <= 5f && h >= 2.4f) return 1;      // 손잡이 고리
        if (y >= 12f && h < 2.4f) return 0;
        return 0;
    }

    static int UpArrow(float x, float y)
    {
        if (y >= 10f && y <= 19f && Mathf.Abs(x - 10f) <= (19f - y) * 1f + 0.4f) return 1;     // 머리
        if (y >= 1.5f && y < 10.5f && Mathf.Abs(x - 10f) <= 3.2f) return 1;                   // 자루
        return 0;
    }

    static int InfoDot(float x, float y)
    {
        if (D(x, y, 10f, 10f) > 9.2f) return 0;
        if (Mathf.Abs(x - 10f) <= 1.3f && ((y >= 3.5f && y <= 11.5f) || (y >= 13.5f && y <= 16f))) return 2;
        return 1;
    }

    static int Star(float x, float y)
    {
        // 다섯 꼭짓점 별 (바깥 9.4 · 안 4)
        float a = Mathf.Atan2(y - 9.6f, x - 10f) - Mathf.PI * 0.5f;
        float r = D(x, y, 10f, 9.6f);
        float seg = Mathf.PI * 2f / 5f;
        float u = Mathf.Repeat(a, seg) / seg;           // 0 ~ 1 (0 · 1 = 꼭짓점)
        float edge = Mathf.Lerp(9.4f, 4f, 1f - Mathf.Abs(u * 2f - 1f));
        return r <= edge ? 1 : 0;
    }

    static Sprite bandSprite, panelSprite;

    // 가로로 양 끝이 투명하게 빠지는 띠 (큰 알림 바탕 · 강조 줄 · 번쩍임)
    static Sprite BandSprite
    {
        get
        {
            if (bandSprite != null) return bandSprite;
            const int W = 128, H = 4;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "NoticeBand" };
            Color[] px = new Color[W * H];
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.2f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) / 0.2f));
                for (int y = 0; y < H; y++) px[y * W + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            bandSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
            bandSprite.name = "NoticeBand";
            return bandSprite;
        }
    }

    // 모서리가 둥근 흰 판 (9칸 늘림, 토스트 · 도움말 · 진행 줄 바탕)
    static Sprite PanelSprite
    {
        get
        {
            if (panelSprite != null) return panelSprite;
            const int N = 16;
            const float R = 5f;
            Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "NoticePanel" };
            Color[] px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, R, N - R), cy = Mathf.Clamp(y + 0.5f, R, N - R);
                    float d = D(x + 0.5f, y + 0.5f, cx, cy);
                    px[y * N + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(R - d + 0.5f));
                }
            tex.SetPixels(px);
            tex.Apply(false, true);
            panelSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6f, 6f, 6f, 6f));
            panelSprite.name = "NoticePanel";
            return panelSprite;
        }
    }
}

// 알림을 움직이는 일꾼 (HUD 캔버스의 「Notices」 판에 붙음 · 씬과 함께 사라짐)
public class NoticeRunner : MonoBehaviour
{
    void Update()
    {
        bool held = Notice.Held;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        Notice.TickBig(held, dt);
        Notice.TickToasts(held, dt);
        Notice.TickObjective(held);
    }
}
