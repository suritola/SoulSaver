using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.2~ 기억 조각 (설계 3-2 ②): 영웅마다 6장 = 54장 · 도감 「기억」 탭에서 한 장씩 읽음 (도감 수집률 · 수집 보상에는 넣지 않음 — 「기억 n/54」 따로)
//   ① 이 영웅으로 첫 사망 ② 사망 5회 ③ 첫 클리어 ④ 어려움 또는 영혼의 무게 1 클리어 ⑤ 무한 모드 10분 또는 계약 보드에서 이 영웅의 칸 모두 ⑥ 숙련 3
//   한 번 돌아온 기억은 남음 (Prefs "memory.<영웅>.<n>") · 기존 기록(숙련 · 무게)으로 셀 수 있는 것은 처음부터 돌아와 있음
//   54장을 모두 되찾으면 칭호 「기억을 되찾은 자」 + 5,000 P 한 번
//   체험판: 거너 · 검사의 ①만 · 글: LocTable.Memory 의 memory.<영웅>.<n>.title · .text (줄은 \n)
public static class MemoryShards
{
    public const int PerHero = 6, Heroes = 9, Total = PerHero * Heroes, AllBonus = 5000;
    const string NewKey = "memory.new";         // 아직 읽지 않은 기억 (쉼표) — 도감 버튼 · 탭 반짝임

    static string Key(CharacterId h, int n) => "memory." + Contracts.HeroKey(h) + "." + n;
    public static bool Has(CharacterId h, int n) => Prefs.GetInt(Key(h, n), 0) == 1;
    public static string Title(CharacterId h, int n) => Loc.T(Key(h, n) + ".title");
    public static string Text(CharacterId h, int n) => Loc.T(Key(h, n) + ".text");

    public static int Count
    {
        get
        {
            int c = 0;
            for (int h = 0; h < Heroes; h++) for (int n = 1; n <= PerHero; n++) if (Has((CharacterId)h, n)) c++;
            return c;
        }
    }

    static bool InDemo(CharacterId h, int n) => n == 1 && Demo.CharacterAllowed(h);

    public static int Deaths(CharacterId h) => Prefs.GetInt("death." + (int)h, 0);
    static int L(string key, CharacterId h) => RunCounters.Life(RunCounters.Hero(key, h));

    // 조건 (지금 기록으로)
    static bool Met(CharacterId h, int n) => n switch
    {
        1 => Deaths(h) >= 1,
        2 => Deaths(h) >= 5,
        3 => Mastery.Level(h) >= 1 || L("clear", h) > 0,
        4 => L("clearhard", h) > 0 || SoulWeight.Best(h) >= 1,
        5 => L("endless.best", h) >= 600 || ContractBoard.ColumnDone(h) >= ContractBoard.HeroRows,
        6 => Mastery.Level(h) >= Mastery.Max,
        _ => false,
    };

    // 조건 글 (잠긴 카드) — 진행이 있으면 수치까지
    public static string CondText(CharacterId h, int n) => n switch
    {
        2 => Loc.T("memory.cond.2").Replace("{0}", Mathf.Min(Deaths(h), 5).ToString()),
        5 => Loc.T("memory.cond.5b").Replace("{0}", ContractBoard.ColumnDone(h).ToString()).Replace("{1}", ContractBoard.HeroRows.ToString()),
        6 => Loc.T("memory.cond.6").Replace("{0}", Mastery.Level(h).ToString()),
        _ => Loc.T("memory.cond." + n),
    };

    // 새로 돌아온 기억을 적음 (쓰러짐 · 클리어 · 보드 · 메뉴) — inRun 이면 알림은 결과 화면 줄로 (판 안에서는 조용히)
    public static void Check(bool inRun)
    {
        if (RunCounters.Off) return;
        List<string> fresh = New();
        bool changed = false;
        for (int h = 0; h < Heroes; h++)
            for (int n = 1; n <= PerHero; n++)
            {
                CharacterId id = (CharacterId)h;
                if (Has(id, n) || (Demo.On && !InDemo(id, n)) || !Met(id, n)) continue;
                Prefs.SetInt(Key(id, n), 1);
                fresh.Add(Key(id, n));
                changed = true;
            }
        if (!changed) return;
        Prefs.SetString(NewKey, string.Join(",", fresh));
        if (!Demo.On && Count >= Total && Prefs.GetInt("memory.allbonus", 0) == 0)
        {
            Prefs.SetInt("memory.allbonus", 1);
            CharacterData.AddPoints(AllBonus);
            Titles.Grant("memory.all", "memory.title.all");
            Notice.Big(Loc.T("memory.title.all.got"), NoticeIcon.Star, 5f, NoticeLevel.Important);
        }
        Prefs.Save();
    }

    static List<string> New() => new List<string>(Prefs.GetString(NewKey, "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
    public static bool HasNew => New().Count > 0;
    public static bool IsNew(CharacterId h, int n) => New().Contains(Key(h, n));
    static void MarkRead(CharacterId h, int n)
    {
        List<string> l = New();
        if (!l.Remove(Key(h, n))) return;
        Prefs.SetString(NewKey, string.Join(",", l));
        Prefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        StageManager.Cleared += d =>
        {
            if (GameMode.IsEndless) return;
            RunCounters.AddLife(RunCounters.Hero("clear"));
            if (d == Difficulty.Hard) RunCounters.AddLife(RunCounters.Hero("clearhard"));
            Herald.RecordClear();
            RunCounters.Flush();
            Check(true);
        };
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (s.name == "MainMenu") Check(false); };
    }

    // ================================================================= 도감 「기억」 탭 (CodexUI 가 부름 · 카드 격자 grid 에 그림)
    public static string HeaderLine => Loc.T("memory.count").Replace("{0}", Count.ToString()).Replace("{1}", Total.ToString());

    public static void FillCodex(RectTransform grid, float cardH, Transform root)
    {
        for (int h = 0; h < Heroes; h++)
        {
            CharacterId id = (CharacterId)h;
            if (!CharacterData.IsDeveloped(id)) continue;
            Sprite face = CharacterUI.Portrait(id);
            for (int n = 1; n <= PerHero; n++)
            {
                bool has = Has(id, n);
                bool demoLocked = Demo.On && !InDemo(id, n);
                RectTransform card = UIKit.Rect("Memory", grid, Vector2.zero, new Vector2(756f, cardH));
                Image bg = card.gameObject.AddComponent<Image>();
                bg.sprite = UIKit.ButtonSprite;
                bg.type = Image.Type.Sliced;
                bg.color = has ? new Color(0.27f, 0.22f, 0.34f, 1f) : new Color(0.16f, 0.14f, 0.2f, 1f);
                RectTransform inner = UIKit.Rect("Inner", card, Vector2.zero, new Vector2(740f, cardH - 16f));
                inner.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.16f, 0.97f);
                if (face != null)
                {
                    Image fi = UIKit.Rect("Face", inner, new Vector2(-282f, 0f), new Vector2(140f, 140f)).gameObject.AddComponent<Image>();
                    fi.sprite = face;
                    fi.preserveAspect = true;
                    fi.color = has ? Color.white : new Color(0.25f, 0.24f, 0.3f, 0.9f);
                    fi.raycastTarget = false;
                }
                TMP_Text t = UIKit.Text(inner, "", 30f, has ? new Color(0.96f, 0.83f, 0.47f) : new Color(0.55f, 0.52f, 0.6f), new Vector2(90f, 72f), new Vector2(540f, 44f), TextAlignmentOptions.Left);
                t.text = has ? Title(id, n) + (IsNew(id, n) ? "  <size=60%><color=#7CFF9A>NEW</color></size>" : "") : Loc.T("memory.locked");
                UIKit.Forget(t);
                TMP_Text tag = UIKit.Text(inner, "", 18f, new Color(0.79f, 0.63f, 1f), new Vector2(90f, 34f), new Vector2(540f, 28f), TextAlignmentOptions.Left);
                tag.text = Loc.T(CharacterData.Def(id).name) + "  ·  " + Loc.T("memory.section") + " " + n + " / " + PerHero;
                UIKit.Forget(tag);
                TMP_Text body = UIKit.Text(inner, "", 19f, new Color(0.88f, 0.85f, 0.78f), new Vector2(90f, -40f), new Vector2(540f, 110f), TextAlignmentOptions.TopLeft);
                body.enableAutoSizing = true;
                body.fontSizeMin = 13f;
                body.fontSizeMax = 19f;
                UIKit.Forget(body);
                if (has)
                {
                    string first = Text(id, n);
                    int nl = first.IndexOf('\n');
                    body.text = (nl > 0 ? first.Substring(0, nl) : first) + "\n<color=#A89C86><size=85%>" + Loc.T("memory.read") + "</size></color>";
                    Button b = card.gameObject.AddComponent<Button>();
                    CharacterId hid = id;
                    int num = n;
                    b.onClick.AddListener(() => { MarkRead(hid, num); Reader.Open(root, hid, num); });
                }
                else body.text = "<color=#A89C86>" + (demoLocked ? Loc.T("정식판에서 만날 수 있습니다") : CondText(id, n)) + "</color>";
            }
        }
    }

    // ================================================================= 읽기 창 (한 줄씩 타자 효과 · 클릭 · [상호작용]으로 넘김 · 판을 멈추지 않음 — 메뉴에서만)
    class Reader : MonoBehaviour
    {
        TMP_Text body;
        string[] lines;
        int shownLines;
        bool advance;

        public static void Open(Transform root, CharacterId h, int n)
        {
            RectTransform win = UIKit.Modal(root, "MemoryReader", new Vector2(1240f, 760f), out GameObject blocker);
            UIKit.Text(win, "", 40f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 320f), new Vector2(1100f, 56f)).text = Title(h, n);
            TMP_Text sub = UIKit.Text(win, "", 20f, new Color(0.79f, 0.63f, 1f), new Vector2(0f, 272f), new Vector2(1100f, 30f));
            sub.text = Loc.T(CharacterData.Def(h).name) + "  ·  " + Loc.T("memory.section") + " " + n + " / " + PerHero;
            UIKit.Forget(sub);
            Reader r = blocker.AddComponent<Reader>();
            r.body = UIKit.Text(win, "", 26f, new Color(0.93f, 0.9f, 0.84f), new Vector2(0f, -10f), new Vector2(1080f, 500f), TextAlignmentOptions.TopLeft);
            r.body.enableAutoSizing = true;
            r.body.fontSizeMin = 16f;
            r.body.fontSizeMax = 26f;
            r.body.lineSpacing = 14f;
            UIKit.Forget(r.body);
            r.lines = Text(h, n).Split('\n');
            UIKit.MakeButton(win, "닫기", new Vector2(0f, -320f), new Vector2(220f, 60f), () => Object.Destroy(blocker), 24f);
            UIKit.EscCancel(blocker, () => Object.Destroy(blocker));
            r.StartCoroutine(r.Play());
        }

        void Update()
        {
            if (KeyBindings.Down(GameAction.Interact) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || GamePad.ADown) advance = true;
        }

        IEnumerator Play()
        {
            string done = "";
            for (shownLines = 0; shownLines < lines.Length; shownLines++)
            {
                string line = lines[shownLines];
                advance = false;
                for (int k = 0; k <= line.Length; k++)
                {
                    body.text = done + line.Substring(0, k);
                    if (advance) { body.text = done + line; break; }
                    if (k < line.Length && line[k] != ' ' && k % 3 == 0) Hostile.Play("pew", 0.06f, 1.8f);
                    float until = Time.unscaledTime + 0.025f;
                    while (Time.unscaledTime < until && !advance) yield return null;
                }
                done += line + "\n\n";
                body.text = done;
                advance = false;
                for (float t = 0f; t < 0.5f && !advance; t += Time.unscaledDeltaTime) yield return null;
            }
        }
    }
}
