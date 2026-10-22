using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 새 버전으로 처음 켰을 때 메인 메뉴에 이번 버전의 패치노트를 띄움 (1.0.4~, 예전엔 켤 때마다)
// 내용은 Resources/PatchNotes.txt (docs/patch-notes/v<버전>.md 를 그대로 복사 · 빌드 스크립트가 빌드할 때 덮어씀)
// 2.1.3~: 메인 메뉴 「패치노트」 버튼 → 지난 버전까지 왼쪽 목록에서 골라 봄 (Resources/PatchNotesAll.txt · tools/patchnotes-all.ps1)
public static class PatchNotesUI
{
    static GameObject open;
    // 이번 실행에서 이미 띄웠는지 (게임을 하고 메인 메뉴로 돌아왔을 때는 다시 안 띄움)
    static bool shown;

    static Color Gold => UiRules.Legacy ? LegacyGold : UiTokens.Text1;      // UI 개편 (Legacy 끔): 금색 글자 대신 뼈색 (기획서 C6 「금색 25벌」)
    static readonly Color LegacyGold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);

    // 1.0.4~: 마지막으로 본 버전을 저장해 새 버전일 때만 띄움 (예전엔 켤 때마다) · 처음 산 사람의 첫 실행에는 띄우지 않음
    const string SeenKey = "patchnotes.seen";

    public static void OpenOnLaunch(Transform root)
    {
        if (shown || GameInput.Auto || Application.isBatchMode) return;
        shown = true;
        string seen = Prefs.GetString(SeenKey, "");
        if (seen == Application.version) return;
        Prefs.SetString(SeenKey, Application.version);
        Prefs.Save();
        if (seen.Length == 0 && !PlayedBefore) return;         // 첫 실행 (업데이트로 처음 이 키를 만나는 기존 플레이어는 보여 줌)
        Open(root);
    }

    // 한 판이라도 해 본 적이 있는지 (처치 누적 · 캐릭터 포인트 기록)
    public static bool PlayedBefore => Prefs.HasKey("stats.totalKills") || Prefs.HasKey("char.points");

    public static void Open(Transform root)
    {
        if (open != null) return;
        TextAsset src = Resources.Load<TextAsset>("PatchNotes");
        if (src == null || string.IsNullOrWhiteSpace(src.text)) return;

        RectTransform win = UIKit.Modal(root, "PatchNotes", new Vector2(1240f, 900f), out open);
        TMP_Text title = UIKit.Text(win, "", 46f, Gold, new Vector2(0f, 395f), new Vector2(900f, 64f));
        title.text = Loc.T("패치노트") + "  <size=70%><color=#A89C86>v" + Application.version + "</color></size>";
        UIKit.MakeButton(win, "닫기", new Vector2(510f, 395f), new Vector2(150f, 58f), Close, 22f);

        RectTransform view = UIKit.Rect("Viewport", win, new Vector2(0f, -40f), new Vector2(1160f, 780f));
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        view.gameObject.AddComponent<RectMask2D>();
        RectTransform content = UIKit.Rect("Content", view, Vector2.zero, new Vector2(1110f, 0f));
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        TMP_Text body = UIKit.Text(content, "", 25f, Parch, Vector2.zero, new Vector2(1110f, 0f), TextAlignmentOptions.TopLeft);
        body.enableAutoSizing = false;
        body.fontSize = 25f;
        body.enableWordWrapping = true;
        body.lineSpacing = 8f;
        RectTransform br = body.rectTransform;
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 1f);
        br.anchoredPosition = Vector2.zero;
        body.text = Format(Localize(src.text));
        Pad(content.gameObject.AddComponent<VerticalLayoutGroup>());
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;

        open.AddComponent<EscCloser>();
    }

    // UI 개편 P1: 메인 메뉴 「새 소식」 카드 — 가장 최근 공지의 제목 (지금 언어 · 없으면 빈 글)
    public static string LatestTitle()
    {
        List<(string version, string text)> all = All();
        if (all.Count == 0) return "";
        foreach (string line in Localize(all[0].text).Replace("\r", "").Split('\n'))
            if (line.StartsWith("# ")) return line.Substring(2).Trim();
        return all[0].version;
    }

    // ================================================================= 지난 버전까지 (메인 메뉴 버튼)
    static List<(string version, string text)> All()
    {
        List<(string, string)> list = new List<(string, string)>();
        TextAsset src = Resources.Load<TextAsset>("PatchNotesAll");
        if (src == null) return list;
        string version = null;
        StringBuilder body = new StringBuilder();
        foreach (string raw in src.text.Replace("\r", "").Split('\n'))
        {
            if (raw.StartsWith("@@@ "))
            {
                if (version != null) list.Add((version, body.ToString()));
                version = raw.Substring(4).Trim();
                body.Clear();
                continue;
            }
            body.Append(raw).Append('\n');
        }
        if (version != null) list.Add((version, body.ToString()));
        return list;
    }

    public static void OpenHistory(Transform root)
    {
        if (open != null) return;
        List<(string version, string text)> notes = All();
        if (notes.Count == 0) { Open(root); return; }

        RectTransform win = UIKit.Modal(root, "PatchNotesHistory", new Vector2(1500f, 900f), out open);
        TMP_Text title = UIKit.Text(win, "", 46f, Gold, new Vector2(0f, 395f), new Vector2(900f, 64f));
        title.text = Loc.T("패치노트");
        UIKit.MakeButton(win, "닫기", new Vector2(640f, 395f), new Vector2(150f, 58f), Close, 22f);

        // 왼쪽: 버전 목록 (최신이 위)
        RectTransform listView = UIKit.Rect("Versions", win, new Vector2(-600f, -40f), new Vector2(240f, 780f));
        listView.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        listView.gameObject.AddComponent<RectMask2D>();
        RectTransform listContent = UIKit.Rect("Content", listView, Vector2.zero, new Vector2(220f, notes.Count * 62f + 10f));
        listContent.anchorMin = listContent.anchorMax = listContent.pivot = new Vector2(0.5f, 1f);
        listContent.anchoredPosition = Vector2.zero;
        ScrollRect listScroll = listView.gameObject.AddComponent<ScrollRect>();
        listScroll.content = listContent;
        listScroll.viewport = listView;
        listScroll.horizontal = false;
        listScroll.movementType = ScrollRect.MovementType.Clamped;
        listScroll.scrollSensitivity = 45f;

        // 오른쪽: 고른 버전의 내용
        RectTransform view = UIKit.Rect("Viewport", win, new Vector2(125f, -40f), new Vector2(1170f, 780f));
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        view.gameObject.AddComponent<RectMask2D>();
        RectTransform content = UIKit.Rect("Content", view, Vector2.zero, new Vector2(1120f, 0f));
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        TMP_Text body = UIKit.Text(content, "", 25f, Parch, Vector2.zero, new Vector2(1120f, 0f), TextAlignmentOptions.TopLeft);
        body.enableAutoSizing = false;
        body.fontSize = 25f;
        body.enableWordWrapping = true;
        body.lineSpacing = 8f;
        RectTransform br = body.rectTransform;
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 1f);
        br.anchoredPosition = Vector2.zero;
        Pad(content.gameObject.AddComponent<VerticalLayoutGroup>());
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;

        List<Image> tabs = new List<Image>();
        void Show(int i)
        {
            body.text = Format(Localize(notes[i].text));
            for (int k = 0; k < tabs.Count; k++) tabs[k].color = k == i ? Gold : new Color(0.72f, 0.7f, 0.78f);
            scroll.verticalNormalizedPosition = 1f;
        }
        for (int i = 0; i < notes.Count; i++)
        {
            int idx = i;
            Button b = UIKit.MakeButton(listContent, "", Vector2.zero, new Vector2(210f, 54f), () => Show(idx), 24f);
            RectTransform r = (RectTransform)b.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -8f - i * 62f);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            label.text = notes[i].version + (notes[i].version == "v" + Application.version ? "  <size=70%>" + Loc.T("지금") + "</size>" : "");
            tabs.Add(b.GetComponent<Image>());
        }
        Show(0);
        open.AddComponent<EscCloser>();
    }

    // 1.1~ 본문 위아래 여백은 레이아웃 안쪽 여백으로 (예전 content 를 -16 내리던 것은 Clamped 스크롤이 첫 프레임 뒤 되돌려 사라졌음)
    static void Pad(VerticalLayoutGroup v)
    {
        v.childControlHeight = true;
        v.padding = new RectOffset(0, 0, 16, 16);
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }

    // 마크다운 몇 가지만 TMP 서식으로: # 제목 · ## 소제목 · ### 작은 제목 · - 목록 · **굵게** (글꼴에 없는 이모지는 뺌)
    // 1.0.2: 언어별 노트 — 짧은 노트 파일 안에 "=== en" · "=== ja" · "=== zh" 줄로 나눈 블록 (맨 앞 블록은 한국어)
    // 지금 언어의 블록을 고르고, 없으면 한국어 (예전 노트도 그대로 보임)
    static readonly string[] LangTags = { "ko", "en", "ja", "zh" };
    static string Localize(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string want = LangTags[Mathf.Clamp((int)Loc.Current, 0, LangTags.Length - 1)];
        string current = "ko", ko = null, found = null;
        StringBuilder b = new StringBuilder();
        foreach (string raw in (text.Replace("\r", "") + "\n=== end").Split('\n'))
        {
            if (raw.StartsWith("=== "))
            {
                string block = b.ToString().Trim('\n');
                if (current == "ko") ko = block;
                if (current == want) found = block;
                current = raw.Substring(4).Trim();
                b.Clear();
                continue;
            }
            b.Append(raw).Append('\n');
        }
        return found ?? ko ?? text;
    }

    static string Format(string md)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string raw in md.Replace("\r", "").Split('\n'))
        {
            string line = Regex.Replace(raw, @"[\uD800-\uDFFF️‍]", "").TrimEnd();
            line = Regex.Replace(line, @"\*\*(.+?)\*\*", "<color=#F5D478>$1</color>");
            string t = line.TrimStart();
            if (t.StartsWith("### ")) sb.Append("<size=108%><color=#9FD8FF>").Append(t.Substring(4).Trim()).Append("</color></size>\n");    // 여러 버전을 합친 패치노트의 작은 제목
            else if (t.StartsWith("## ")) sb.Append("\n<size=120%><color=#C9A0FF>").Append(t.Substring(3).Trim()).Append("</color></size>\n");
            else if (t.StartsWith("# ")) sb.Append("<size=125%><color=#F5D478>").Append(t.Substring(2).Trim()).Append("</color></size>\n");
            // 1.1~ 들여쓴 하위 목록 (  - …): 한 단계 더 들여 작은 점으로 (예전엔 TrimStart 뒤라 상위 항목과 똑같이 보였음)
            else if (t.StartsWith("- ") && line.StartsWith("  ")) sb.Append("<indent=5%><color=#A89C86>·</color></indent><indent=8%>").Append(t.Substring(2)).Append("</indent>\n");
            else if (t.StartsWith("- ")) sb.Append("<indent=2%>•</indent><indent=5%>").Append(t.Substring(2)).Append("</indent>\n");
            else if (t.Length > 0 && line.StartsWith(" ")) sb.Append("<indent=5%><color=#A89C86>").Append(t).Append("</color></indent>\n");
            else if (t.Length > 0) sb.Append(t).Append('\n');
        }
        return sb.ToString().Trim();
    }

    // ESC 로도 닫힘
    class EscCloser : MonoBehaviour
    {
        void Update()
        {
            // 1.0.4~: 바로 앞 창(언어 고르기 등)을 닫은 그 ESC 로 같이 닫히지 않게
            if (!GameInput.EscDown || SettingsUI.EscHandledFrame == Time.frameCount) return;
            SettingsUI.EscHandledFrame = Time.frameCount;
            Close();
        }
    }
}
