using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1.1.5~ 스팀 순위 창: 메인 메뉴 「순위표」 · 무한 모드 결과 화면 「순위 보기」 · 일일 도전 창 「전체 순위」
// 순위표 2개(무한 모드 ENDLESS_BEST · 오늘의 일일 도전 DAILY_yyyyMMdd) × 탭 3개(전체 상위 1~100 · 내 주변 -5~+5 · 친구)
// 줄마다 순위 · 영웅 아이콘 · 스팀 이름 · 보스 처치 · 버틴 시간, 내 줄은 금빛. 받는 일은 SteamLeaderboards 가 함
// 스팀이 없거나(에디터 · GitHub 빌드 · 오프라인) 체험판이면 안내 문구만 (SteamLeaderboards.Available)
public static class LeaderboardUI
{
    public enum Board { Endless, Daily }

    static GameObject open;
    static Action onClosed;
    public static bool IsOpen => open != null;

    const float W = 1500f, H = 880f;

    // closed: 닫힌 뒤 부름 (일일 도전 창에서 열었으면 그 창으로 돌아감) · day: 일일 도전 날 (없으면 결과 화면이면 그 판의 날, 아니면 오늘)
    public static void Open(Transform root, Board board, SteamLeaderboards.Scope tab, Action closed = null)
    {
        if (open != null || root == null) return;
        onClosed = closed;
        RectTransform win = UIKit.Modal(root, "Leaderboard", new Vector2(W, H), out open);
        WindowFit.Attach(win, new Vector2(W + 60f, H + 60f));        // 좁은 화면 · 큰 UI 크기에서도 창이 화면 안에
        UIKit.EscCancel(open, Close);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);      // 뒤의 메뉴 버튼이 방향키로 골라지지 않게
        open.AddComponent<View>().Build(win, board, tab);
    }

    public static void Close()
    {
        if (open == null) return;
        UnityEngine.Object.Destroy(open);
        open = null;
        Action then = onClosed;
        onClosed = null;
        then?.Invoke();
    }

    // 메인 메뉴 오른쪽 위 (영혼 각인 아래) · 체험판은 무한 모드가 없어 숨김
    public static void InstallMenuButton(Transform parent, Transform root)
    {
        if (Demo.On || parent == null || parent.Find("LeaderboardButton") != null) return;
        Button b = UIKit.MakeButton(parent, "lb.button", Vector2.zero, new Vector2(220f, 56f), () => Open(root, Board.Endless, SteamLeaderboards.Scope.Global), 24f);
        b.name = "LeaderboardButton";
        RectTransform r = (RectTransform)b.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-30f, -238f);
    }

    // 영웅 아이콘 (다른 사람 줄이라 내가 낀 스킨이 아닌 기본 그림 · CharacterUI.Portrait 와 같은 고르기)
    static readonly Dictionary<int, Sprite> icons = new Dictionary<int, Sprite>();
    static Sprite HeroIcon(int id)
    {
        if (id < 0 || id >= CharacterData.All.Length) return null;
        if (icons.TryGetValue(id, out Sprite known)) return known;
        CharacterDef d = CharacterData.Def((CharacterId)id);
        Sprite best = null;
        foreach (Sprite s in Resources.LoadAll<Sprite>("Characters/" + (d.body ?? "gunner_nogun")))
        {
            if (d.body == null && s.name == "players blue x1_0") { best = s; break; }
            if (best == null || string.CompareOrdinal(s.name, best.name) < 0) best = s;
        }
        icons[id] = best;
        return best;
    }

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);
    static readonly Color TabOff = new Color(0.72f, 0.7f, 0.78f);
    static readonly Color[] Podium = { new Color(1f, 0.84f, 0.35f), new Color(0.82f, 0.85f, 0.9f), new Color(0.86f, 0.6f, 0.4f) };

    // 판마다 한 번: 이 PC 의 최고 기록이 스팀보다 좋으면 먼저 올림 (SteamLeaderboards.SyncBest)
    static readonly HashSet<string> synced = new HashSet<string>();

    // 열 위치 (줄 가운데 기준)
    const float RowW = 1400f, RowH = 52f, ViewH = 560f;
    const float RankX = -625f, IconX = -515f, NameX = -130f, BossX = 340f, TimeX = 575f;

    class Row
    {
        public RectTransform r;
        public Image bg, icon;
        public TMP_Text rank, name, bosses, time;
        public SteamLeaderboards.Entry entry;
    }

    class View : MonoBehaviour
    {
        Board board;
        SteamLeaderboards.Scope tab;
        string boardName;
        float localBest;

        readonly Button[] boardButtons = new Button[2];
        readonly Button[] tabButtons = new Button[3];
        TMP_Text title, summary, status, hint;
        Button jump;
        RectTransform viewport, content;
        Camera cam;
        readonly List<Row> rows = new List<Row>();

        List<SteamLeaderboards.Entry> entries;
        int total = -1, request;
        bool failed, waitUpload, namesDirty, timedOut;
        float giveUp, nextNames, waitUntil;

        public void Build(RectTransform win, Board b, SteamLeaderboards.Scope t)
        {
            Canvas canvas = win.GetComponentInParent<Canvas>();
            cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            title = UIKit.Text(win, "", 44f, Gold, new Vector2(0f, 385f), new Vector2(1000f, 60f));
            UIKit.MakeButton(win, "닫기", new Vector2(650f, 385f), new Vector2(150f, 56f), Close, 22f);

            boardButtons[0] = UIKit.MakeButton(win, "무한 모드", new Vector2(-575f, 315f), new Vector2(230f, 56f), () => SetBoard(Board.Endless), 22f);
            boardButtons[1] = UIKit.MakeButton(win, "일일 도전", new Vector2(-335f, 315f), new Vector2(230f, 56f), () => SetBoard(Board.Daily), 22f);
            string[] tabKeys = { "lb.tab.global", "lb.tab.around", "lb.tab.friends" };
            for (int i = 0; i < 3; i++)
            {
                SteamLeaderboards.Scope s = (SteamLeaderboards.Scope)i;
                tabButtons[i] = UIKit.MakeButton(win, tabKeys[i], new Vector2(230f + 210f * i, 315f), new Vector2(200f, 56f), () => SetTab(s), 22f);
            }

            summary = UIKit.Text(win, "", 24f, Parch, new Vector2(0f, 258f), new Vector2(RowW, 36f), TextAlignmentOptions.Left);

            // 머리줄
            RectTransform head = UIKit.Rect("Head", win, new Vector2(0f, 212f), new Vector2(RowW, 36f));
            UIKit.Text(head, "lb.col.rank", 20f, Dim, new Vector2(RankX, 0f), new Vector2(120f, 30f));
            UIKit.Text(head, "lb.col.name", 20f, Dim, new Vector2(NameX, 0f), new Vector2(700f, 30f), TextAlignmentOptions.Left);
            UIKit.Text(head, "lb.col.bosses", 20f, Dim, new Vector2(BossX, 0f), new Vector2(180f, 30f));
            UIKit.Text(head, "lb.col.time", 20f, Dim, new Vector2(TimeX, 0f), new Vector2(220f, 30f));

            // 목록 (스크롤 · 마우스 휠 · 끌기)
            viewport = UIKit.Rect("Viewport", win, new Vector2(0f, -90f), new Vector2(RowW, ViewH));
            Image vbg = viewport.gameObject.AddComponent<Image>();
            vbg.color = new Color(0f, 0f, 0f, 0.18f);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = UIKit.Rect("Content", viewport, Vector2.zero, new Vector2(RowW, 0f));
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = RowH;
            status = UIKit.Text(viewport, "", 26f, Dim, Vector2.zero, new Vector2(RowW - 120f, 200f));

            hint = UIKit.Text(win, "", 20f, Dim, new Vector2(-150f, -398f), new Vector2(1100f, 32f), TextAlignmentOptions.Left);
            jump = UIKit.MakeButton(win, "lb.jump", new Vector2(600f, -398f), new Vector2(220f, 48f), JumpToMe, 20f);

            gameObject.AddComponent<LocRefresh>().refresh = () => { RefreshChrome(); Render(); };
            SteamLeaderboards.NamesChanged += OnNames;

            board = b;
            tab = t;
            waitUntil = Time.unscaledTime + 10f;        // 올리기가 10초 넘게 안 끝나면 기다리지 않고 받음
            Load();
            StartCoroutine(FocusFirst());
        }

        System.Collections.IEnumerator FocusFirst()
        {
            yield return null;
            GamePad.WarpTo((RectTransform)tabButtons[(int)tab].transform);
        }

        void OnDestroy() => SteamLeaderboards.NamesChanged -= OnNames;
        void OnNames() => namesDirty = true;

        void SetBoard(Board b)
        {
            if (b == board) return;
            board = b;
            Load();
        }

        void SetTab(SteamLeaderboards.Scope t)
        {
            if (t == tab) return;
            tab = t;
            Load();
        }

        // 고른 순위표 · 탭 강조, 제목 · 안내
        void RefreshChrome()
        {
            for (int i = 0; i < 2; i++) boardButtons[i].GetComponent<Image>().color = i == (int)board ? Gold : TabOff;
            for (int i = 0; i < 3; i++) tabButtons[i].GetComponent<Image>().color = i == (int)tab ? Gold : TabOff;
            DateTime day = Day();
            title.text = board == Board.Endless ? Loc.T("무한 모드 순위")
                       : Loc.T("일일 도전") + "  <size=60%><color=#A89C86>" + day.ToString("yyyy-MM-dd") + " (UTC) · " + Loc.T(DailyChallenge.RuleOf(day).name) + "</color></size>";
            hint.text = Loc.T(GamePad.Active ? "lb.hint.pad" : "lb.hint.keys");
        }

        // 일일 도전 날: 그 판의 결과 화면이면 시작한 날(자정을 넘겨도), 아니면 오늘
        static DateTime Day()
        {
            if (DailyChallenge.Active && DailyChallenge.LastBoard == DailyChallenge.RunBoard)
            {
                string key = DailyChallenge.RunBoard.Substring(SteamLeaderboards.Daily("").Length);
                if (DateTime.TryParseExact(key, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime d)) return d;
            }
            return DailyChallenge.Today;
        }

        void Load()
        {
            request++;
            int my = request;
            entries = null;
            total = -1;
            failed = false;
            waitUpload = false;
            timedOut = false;
            DateTime day = Day();
            boardName = board == Board.Endless ? SteamLeaderboards.Endless : SteamLeaderboards.Daily(DailyChallenge.Key(day));
            localBest = board == Board.Endless ? EndlessMode.BestSeconds : DailyChallenge.Best(day);
            RefreshChrome();
            content.anchoredPosition = Vector2.zero;
            if (!SteamLeaderboards.Available) { Render(); return; }
            giveUp = Time.unscaledTime + 15f;
            // 결과 화면에서 바로 열면 이번 판 기록을 다 올린 다음에 받음 (Update 가 다시 부름)
            if (SteamLeaderboards.Pending > 0 && Time.unscaledTime < waitUntil) { waitUpload = true; Render(); return; }
            Action fetch = () =>
            {
                if (this == null || my != request) return;
                int count = tab == SteamLeaderboards.Scope.AroundMe ? 5 : 100;
                SteamLeaderboards.Fetch(boardName, tab, count, (list, t) =>
                {
                    if (this == null || my != request) return;
                    entries = list;
                    total = t;
                    failed = list == null;
                    Render();
                    if (tab == SteamLeaderboards.Scope.AroundMe || tab == SteamLeaderboards.Scope.Friends) JumpToMe();
                });
            };
            Render();
            if (synced.Add(boardName)) SteamLeaderboards.SyncBest(boardName, localBest, fetch);
            else fetch();
        }

        void Render()
        {
            if (title == null) return;
            Loc.EnsureCjkFallback(status);       // 스팀 이름의 한자 · 가나가 □ 로 안 보이게 (일일 도전 순위와 같은 방식)
            int mineIdx = entries != null ? entries.FindIndex(e => e.me) : -1;

            // 위 요약: 내 최고 기록 · 내 순위 / 전체
            string best = Loc.T("lb.mybest") + "  <color=#F5D478>" + (localBest >= 1f ? EndlessMode.Clock(localBest) : "-") + "</color>";
            if (mineIdx >= 0)
            {
                SteamLeaderboards.Entry m = entries[mineIdx];
                if (m.score > localBest) best = Loc.T("lb.mybest") + "  <color=#F5D478>" + EndlessMode.Clock(m.score) + "</color>";
                best += "     " + Loc.T("lb.myrank").Replace("{0}", m.rank.ToString("N0"));
            }
            if (total > 0) best += "     <color=#A89C86>" + Loc.T("lb.total").Replace("{0}", total.ToString("N0")) + "</color>";
            summary.text = best;
            jump.interactable = mineIdx >= 0;

            string msg = null;
            if (!SteamLeaderboards.Available) msg = "스팀에 연결되어 있지 않아 순위를 볼 수 없습니다.";
            else if (failed) msg = "순위를 불러오지 못했습니다. 창을 다시 열어 보세요.";
            else if (entries == null) msg = waitUpload ? "lb.uploading" : timedOut ? "순위를 불러오지 못했습니다. 창을 다시 열어 보세요." : "불러오는 중…";
            else if (entries.Count == 0)
                msg = tab == SteamLeaderboards.Scope.AroundMe ? "lb.empty.me"
                    : tab == SteamLeaderboards.Scope.Friends ? "lb.empty.friends"
                    : "아직 기록이 없습니다. 첫 번째 도전자가 되어 보세요!";
            status.text = msg != null ? Loc.T(msg) : "";
            status.gameObject.SetActive(msg != null);

            int n = msg == null ? entries.Count : 0;
            while (rows.Count < n) rows.Add(MakeRow());
            for (int i = 0; i < rows.Count; i++)
            {
                bool on = i < n;
                rows[i].r.gameObject.SetActive(on);
                if (on) Fill(rows[i], entries[i], i);
            }
            content.sizeDelta = new Vector2(RowW, n * RowH);
            namesDirty = false;
            nextNames = Time.unscaledTime + 1f;
        }

        Row MakeRow()
        {
            Row row = new Row();
            row.r = UIKit.Rect("Row", content, Vector2.zero, new Vector2(RowW, RowH - 4f));
            row.r.anchorMin = row.r.anchorMax = new Vector2(0.5f, 1f);
            row.bg = row.r.gameObject.AddComponent<Image>();        // 받는 판이라 휠 · 끌기가 목록으로 감
            row.rank = UIKit.Text(row.r, "", 26f, Parch, new Vector2(RankX, 0f), new Vector2(120f, 40f));
            RectTransform ir = UIKit.Rect("Hero", row.r, new Vector2(IconX, 0f), new Vector2(44f, 44f));
            row.icon = ir.gameObject.AddComponent<Image>();
            row.icon.preserveAspect = true;
            row.icon.raycastTarget = false;
            row.name = UIKit.Text(row.r, "", 26f, Parch, new Vector2(NameX, 0f), new Vector2(700f, 40f), TextAlignmentOptions.Left);
            row.name.enableWordWrapping = false;
            row.name.overflowMode = TextOverflowModes.Ellipsis;
            row.bosses = UIKit.Text(row.r, "", 24f, Parch, new Vector2(BossX, 0f), new Vector2(180f, 40f));
            row.time = UIKit.Text(row.r, "", 28f, Parch, new Vector2(TimeX, 0f), new Vector2(220f, 40f));
            Loc.EnsureCjkFallback(row.name);
            return row;
        }

        void Fill(Row row, SteamLeaderboards.Entry e, int i)
        {
            row.entry = e;
            row.r.anchoredPosition = new Vector2(0f, -RowH * (i + 0.5f));
            row.bg.color = e.me ? new Color(0.96f, 0.83f, 0.47f, 0.24f) : new Color(1f, 1f, 1f, i % 2 == 0 ? 0.05f : 0.02f);
            Color c = e.me ? Gold : Parch;
            row.rank.text = "#" + e.rank.ToString("N0");
            row.rank.color = e.rank >= 1 && e.rank <= 3 ? Podium[e.rank - 1] : c;
            Sprite icon = HeroIcon(e.character);
            row.icon.sprite = icon;
            row.icon.enabled = icon != null;
            string name = SteamLeaderboards.DisplayName(e.user, 20);
            row.name.text = string.IsNullOrEmpty(name) ? "…" : name;
            row.name.color = c;
            row.bosses.text = e.bosses >= 0 ? e.bosses.ToString() : "-";
            row.bosses.color = c;
            row.time.text = EndlessMode.Clock(e.score);
            row.time.color = e.me ? Gold : new Color(1f, 0.8f, 0.45f);
        }

        // 이름만 다시 (스팀이 늦게 알려 줌)
        void RefreshNames()
        {
            if (entries == null) { nextNames = Time.unscaledTime + 1f; return; }
            bool missing = false;
            foreach (Row row in rows)
            {
                if (!row.r.gameObject.activeSelf) continue;
                string name = SteamLeaderboards.DisplayName(row.entry.user, 20);
                if (string.IsNullOrEmpty(name)) { missing = true; continue; }
                if (row.name.text != name) row.name.text = name;
            }
            namesDirty = false;
            nextNames = missing ? Time.unscaledTime + 1f : float.MaxValue;
        }

        void JumpToMe()
        {
            if (entries == null) return;
            int i = entries.FindIndex(e => e.me);
            if (i < 0) return;
            ScrollTo(RowH * (i + 0.5f) - ViewH * 0.5f);
        }

        void ScrollTo(float y)
        {
            float max = Mathf.Max(0f, content.sizeDelta.y - ViewH);
            content.anchoredPosition = new Vector2(0f, Mathf.Clamp(y, 0f, max));
        }

        void Update()
        {
            if (waitUpload && (SteamLeaderboards.Pending == 0 || Time.unscaledTime >= waitUntil)) Load();
            if (namesDirty || Time.unscaledTime >= nextNames) RefreshNames();
            // 15초 안에 답이 없으면 「불러오는 중…」에 멈추지 않고 실패 안내
            if (!timedOut && entries == null && !failed && !waitUpload && SteamLeaderboards.Available && Time.unscaledTime > giveUp) { timedOut = true; Render(); }

            // 탭: ← → (패드 십자키 좌우) · Tab = 순위표 바꾸기
            int dx = GamePad.TakeDpadX();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) dx = -1;
            if (Input.GetKeyDown(KeyCode.RightArrow)) dx = 1;
            if (dx != 0)
            {
                SetTab((SteamLeaderboards.Scope)(((int)tab + dx + 3) % 3));
                GamePad.WarpTo((RectTransform)tabButtons[(int)tab].transform);
            }
            if (Input.GetKeyDown(KeyCode.Tab)) SetBoard(board == Board.Endless ? Board.Daily : Board.Endless);

            // 목록: ↑ ↓ 한 줄 · PageUp/Down 한 쪽 · Home 맨 위 · End 내 줄 · 패드 오른쪽 스틱 (커서가 목록 위면 ScrollRect 가 받음)
            float y = content.anchoredPosition.y;
            if (Input.GetKeyDown(KeyCode.DownArrow)) ScrollTo(y + RowH);
            if (Input.GetKeyDown(KeyCode.UpArrow)) ScrollTo(y - RowH);
            if (Input.GetKeyDown(KeyCode.PageDown)) ScrollTo(y + ViewH - RowH);
            if (Input.GetKeyDown(KeyCode.PageUp)) ScrollTo(y - ViewH + RowH);
            if (Input.GetKeyDown(KeyCode.Home)) ScrollTo(0f);
            if (Input.GetKeyDown(KeyCode.End)) JumpToMe();
            Vector2 pad = GamePad.Scroll;
            if (pad.y != 0f && !RectTransformUtility.RectangleContainsScreenPoint(viewport, GameInput.PointerPosition, cam))
                ScrollTo(content.anchoredPosition.y - pad.y * RowH);
        }
    }
}
