using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 2.0.0~ 「특별 모드」 창 (modes.md 0-3 · 1-3): 메인 메뉴 일일 도전 바로 아래 한 줄 → 왼쪽 「영혼 릴레이」 · 오른쪽 「역류」 카드
//   카드: 그림(128칸 Icons/mode_relay · mode_reflux) · 이름 · 규칙 세 줄 · 내 최고 기록 · [고르기] (잠겨 있으면 회색 + 여는 조건 한 줄)
//   영혼 릴레이 [고르기] → 「주자 고르기」 창 (영웅 셋을 순서대로) → RelayRun.Begin
//   역류는 아직 없음 — RefluxOpener 에 역류 준비 창을 여는 함수를 걸면 [고르기] 가 그것을 부름 (RefluxRun.Unlocked · Implemented 도 참이어야 함)
public static class ModeHubUI
{
    static GameObject open;
    static Color Gold => UiRules.Legacy ? new Color(0.96f, 0.83f, 0.47f) : UiTokens.Text1;
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);

    // 역류 작업자가 채움: 역류 준비 창 열기 (root)
    public static System.Action<Transform> RefluxOpener = RefluxPrepUI.Open;      // 2.0.0~ 역류 준비 창 (Modes/RefluxPrepUI.cs)

    // ================================================================= 새 모드 알림 (Prefs mode.hub.seen.<relay|reflux>)
    const string SeenRelay = "mode.hub.seen.relay", SeenReflux = "mode.hub.seen.reflux";
    static bool RelayNew => RelayRun.Unlocked && Prefs.GetInt(SeenRelay, 0) == 0;
    static bool RefluxNew => RefluxRun.Unlocked && RefluxRun.Implemented && Prefs.GetInt(SeenReflux, 0) == 0;
    public static bool HasNew => !Demo.On && (RelayNew || RefluxNew);

    // 메인 메뉴: 새로 열린 모드를 한 번 알림 (큰 알림 + 「특별 모드」 줄 반짝임은 HasNew)
    public static void MenuCheck()
    {
        if (Demo.On || !RelayRun.Unlocked || RelayRun.UnlockNoticed) return;
        RelayRun.UnlockNoticed = true;
        Notice.Big(Loc.T("relay.unlock.title"), Loc.T("relay.unlock.sub"), NoticeIcon.Unlock, 4f, NoticeLevel.Important);
    }

    static void MarkSeen()
    {
        if (RelayRun.Unlocked) Prefs.SetInt(SeenRelay, 1);
        if (RefluxRun.Unlocked && RefluxRun.Implemented) Prefs.SetInt(SeenReflux, 1);
        Prefs.Save();
        GameObject modes = GameObject.Find("ModesButton");
        if (modes != null && modes.GetComponent<NewGlow>() != null) modes.GetComponent<NewGlow>().Remove();
    }

    // ================================================================= 특별 모드 창
    public static void Open(Transform root)
    {
        if (open != null) return;
        // 2.0.1~ 메뉴 개편: 「도전 모드」 — 일일 도전 · 영혼 릴레이 · 역류 세 카드 + 아래 계약 보드 (메인 화면 버튼 둘을 하나로)
        RectTransform win = UIKit.Modal(root, "ModeHub", new Vector2(1320f, 900f), out open);
        UIKit.EscCancel(open, Close);
        TMP_Text title = UIKit.Text(win, "도전 모드", 46f, Gold, new Vector2(0f, 395f), new Vector2(700f, 64f));
        title.fontSizeMin = 24f;
        UIKit.MakeButton(win, "닫기", new Vector2(550f, 395f), new Vector2(150f, 58f), Close, 22f);
        Card(win, root, -430f, Kind.Daily);
        Card(win, root, 0f, Kind.Relay);
        Card(win, root, 430f, Kind.Reflux);
        if (!Demo.On)
        {
            string board = Loc.T("board.open") + (ContractBoard.Claimable > 0 ? "  <color=#7FE08A>+" + ContractBoard.Claimable + "</color>" : "");
            Button b = UIKit.MakeButton(win, "", new Vector2(-190f, -408f), new Vector2(360f, 54f), () => { Close(); ContractBoard.Open(root); }, 24f);
            if (b.GetComponentInChildren<TMP_Text>(true) is TMP_Text bt) bt.text = board;
            if (ContractBoard.Claimable > 0) b.gameObject.AddComponent<NewGlow>();
            UIKit.MakeButton(win, "기록", new Vector2(190f, -408f), new Vector2(360f, 54f), () => { Close(); RunHistoryUI.Open(root); }, 24f);      // 2.0.1~ 메인 화면 오른쪽 위에서 옮김
        }
        MarkSeen();
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }

    enum Kind { Daily, Relay, Reflux }
    const float CardW = 410f;

    // 카드 하나
    static void Card(RectTransform win, Transform root, float x, Kind kind)
    {
        bool demo = Demo.On;
        bool reflux = kind == Kind.Reflux, daily = kind == Kind.Daily;
        bool unlocked = daily ? DailyChallenge.Unlocked : reflux ? RefluxRun.Unlocked && RefluxRun.Implemented : RelayRun.Unlocked;
        RectTransform card = UIKit.Rect(daily ? "DailyCard" : reflux ? "RefluxCard" : "RelayCard", win, new Vector2(x, 0f), new Vector2(CardW, 680f));
        Image bg = card.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.18f);
        bg.raycastTarget = false;

        // 그림 128칸 (아직 안 만들었으면 비움)
        Sprite art = Resources.Load<Sprite>(daily ? "Icons/menu_daily" : reflux ? "Icons/mode_reflux" : "Icons/mode_relay");
        RectTransform ar = UIKit.Rect("Art", card, new Vector2(0f, 240f), new Vector2(160f, 160f));
        Image ai = ar.gameObject.AddComponent<Image>();
        ai.sprite = art;
        ai.preserveAspect = true;
        ai.raycastTarget = false;
        ai.enabled = art != null;
        if (!unlocked) ai.color = new Color(0.55f, 0.55f, 0.6f, 1f);

        TMP_Text name = UIKit.Text(card, daily ? "일일 도전" : reflux ? "역류" : "영혼 릴레이", 40f, Gold, new Vector2(0f, 128f), new Vector2(CardW - 40f, 54f));
        name.fontSizeMin = 24f;
        TMP_Text rules = UIKit.Text(card, daily ? "mode.card.daily" : reflux ? "relay.card.reflux" : "relay.card.relay", 22f, Parch, new Vector2(0f, -15f), new Vector2(CardW - 50f, 220f), TextAlignmentOptions.Top);
        rules.fontSizeMin = 16f;
        rules.lineSpacing = 6f;
        if (daily && unlocked)
        {
            // 오늘 영웅 · 규칙 (언어를 바꾸면 창을 다시 여므로 한 번만 씀)
            DailyChallenge.Rule r = DailyChallenge.RuleOf(DailyChallenge.Today);
            rules.text = Loc.T("mode.card.daily") + "\n\n<color=#F5D478>" + Loc.T(CharacterData.Def(DailyChallenge.CharacterOf(DailyChallenge.Today)).name)
                       + " · " + Loc.T(r.name) + "</color>";
            UIKit.Forget(rules);
        }

        float best = daily ? DailyChallenge.Best(DailyChallenge.Today) : reflux ? RefluxRun.Best : RelayRun.BestAll;
        TMP_Text rec = UIKit.Text(card, "", 24f, Parch, new Vector2(0f, -150f), new Vector2(CardW - 40f, 34f));
        rec.text = Loc.T(daily ? "오늘 내 최고 기록" : "relay.hub.best") + "  <color=#F5D478>" + (best > 0f ? EndlessMode.Clock(best) : "-") + "</color>";
        rec.fontSizeMin = 16f;

        Button pick = UIKit.MakeButton(card, daily ? "ui.btn.start" : "relay.hub.pick", new Vector2(0f, -225f), new Vector2(300f, 76f), () =>
        {
            Close();
            if (daily) DailyChallenge.Open(root);
            else if (reflux) RefluxOpener?.Invoke(root);
            else OpenRelay(root);
        }, 30f);
        pick.interactable = unlocked && (!reflux || RefluxOpener != null);
        if (!pick.interactable) GrayOut(pick);

        string note = demo ? "정식판에서 만날 수 있습니다" : unlocked ? "" : daily ? "ladder.lock.daily"
                    : reflux ? (RefluxRun.Implemented ? "reflux.hub.locked" : "relay.hub.soon") : "relay.hub.locked.relay";
        if (note.Length > 0) UIKit.Text(card, note, 20f, Dim, new Vector2(0f, -290f), new Vector2(CardW - 30f, 56f));

        if (demo || (reflux && !RefluxRun.Implemented)) return;
        LeaderboardUI.Board board = daily ? LeaderboardUI.Board.Daily : reflux ? LeaderboardUI.Board.Reflux : LeaderboardUI.Board.Relay;
        UIKit.MakeButton(card, "relay.hub.ranks", new Vector2(0f, -340f), new Vector2(260f, 50f), () =>
        {
            Close();
            LeaderboardUI.Open(root, board, SteamLeaderboards.Scope.Global, () => Open(root));
        }, 22f);
    }

    // 잠긴 버튼 (DailyChallenge.Open 과 같은 처리: 판이 기본 disabledColor 와 곱해져 사라지지 않게 · 글자는 흐리게)
    static void GrayOut(Button b)
    {
        b.GetComponent<Image>().color = new Color(0.25f, 0.24f, 0.28f, 0.8f);
        ColorBlock cb = b.colors;
        cb.disabledColor = Color.white;
        b.colors = cb;
        TMP_Text label = b.GetComponentInChildren<TMP_Text>();
        if (label != null) label.color = Dim;
    }

    // ================================================================= 주자 고르기 창
    static readonly int[] picked = new int[RelayRun.Runners];
    static readonly List<GameObject> cells = new List<GameObject>();

    public static void OpenRelay(Transform root)
    {
        if (open != null || !RelayRun.Unlocked) return;
        RectTransform win = UIKit.Modal(root, "RelayPick", new Vector2(1240f, 820f), out open);
        UIKit.EscCancel(open, () => { Close(); Open(root); });
        RelayPicker view = open.AddComponent<RelayPicker>();
        view.Build(win, root);
    }

    // 처음 열 때 기본값: 지난 릴레이 조합, 없으면 지금 고른 영웅 + 가진 영웅 앞 순서 둘
    static void DefaultPick()
    {
        for (int i = 0; i < picked.Length; i++) picked[i] = -1;
        int[] last = RelayRun.LastOrder;
        if (last != null) { for (int i = 0; i < picked.Length; i++) picked[i] = last[i]; return; }
        int n = 0;
        CharacterId now = CharacterData.Selected;
        if (CharacterData.IsUnlocked(now)) picked[n++] = (int)now;
        for (int i = 0; i < CharacterData.All.Length && n < picked.Length; i++)
            if (CharacterData.IsUnlocked((CharacterId)i) && System.Array.IndexOf(picked, i) < 0) picked[n++] = i;
    }

    class RelayPicker : MonoBehaviour
    {
        Transform root;
        readonly Image[] slotPortrait = new Image[RelayRun.Runners];
        readonly TMP_Text[] slotName = new TMP_Text[RelayRun.Runners];
        readonly TMP_Text[] slotHeir = new TMP_Text[RelayRun.Runners];
        readonly Image[] cellPortrait = new Image[16];
        readonly TMP_Text[] cellBadge = new TMP_Text[16];
        readonly TMP_Text[] cellName = new TMP_Text[16];
        Button go;
        TMP_Text recordText, boardText, hint;
        string shownCombo = "";
        List<SteamLeaderboards.Entry> entries;
        int total, request;
        float giveUp;
        bool failed;

        public void Build(RectTransform win, Transform rootTf)
        {
            root = rootTf;
            DefaultPick();
            TMP_Text title = UIKit.Text(win, "relay.pick.title", 46f, Gold, new Vector2(0f, 355f), new Vector2(700f, 64f));
            title.fontSizeMin = 24f;
            UIKit.MakeButton(win, "닫기", new Vector2(510f, 355f), new Vector2(150f, 58f), () => { Close(); Open(root); }, 22f);

            // 주자 칸 셋 (초상 96칸 · 이름 · 「넘겨줄 유품」)
            for (int k = 0; k < RelayRun.Runners; k++)
            {
                float x = -400f + 400f * k;
                RectTransform slot = UIKit.Rect("Slot" + k, win, new Vector2(x, 255f), new Vector2(360f, 130f));
                Image sbg = slot.gameObject.AddComponent<Image>();
                sbg.color = new Color(0f, 0f, 0f, 0.22f);
                sbg.raycastTarget = false;
                RectTransform pr = UIKit.Rect("Portrait", slot, new Vector2(-125f, 6f), new Vector2(96f, 96f));
                slotPortrait[k] = pr.gameObject.AddComponent<Image>();
                slotPortrait[k].preserveAspect = true;
                slotPortrait[k].raycastTarget = false;
                UIKit.Text(slot, "", 20f, Dim, new Vector2(35f, 40f), new Vector2(220f, 28f), TextAlignmentOptions.Left).text = Loc.T("relay.pick.slot").Replace("{0}", (k + 1).ToString());
                slotName[k] = UIKit.Text(slot, "", 30f, Gold, new Vector2(35f, 6f), new Vector2(220f, 40f), TextAlignmentOptions.Left);
                slotHeir[k] = UIKit.Text(slot, "", 18f, Dim, new Vector2(0f, -54f), new Vector2(350f, 44f));      // 2.0.1~ 두 줄: 유품 이름 + 설명 (RelayHeirlooms.DescOf · 넘치면 글자가 줄어듦)
                if (k < RelayRun.Runners - 1) UIKit.Text(win, "→", 40f, Dim, new Vector2(x + 200f, 255f), new Vector2(50f, 50f));
            }

            // 영웅 격자 (5칸 × 3줄 · 영웅 고르기 화면과 같은 순서)
            int total = CharacterData.All.Length;
            for (int i = 0; i < total && i < cellPortrait.Length; i++)
            {
                int hero = i;
                int col = i % 5, row = i / 5;
                Vector2 pos = new Vector2(-420f + 150f * col, 105f - 128f * row);
                Button b = UIKit.MakeButton(win, "", pos, new Vector2(140f, 118f), () => Toggle(hero), 20f);
                TMP_Text bl = b.GetComponentInChildren<TMP_Text>(true);
                if (bl != null) bl.gameObject.SetActive(false);
                RectTransform pr = UIKit.Rect("Portrait", b.transform, new Vector2(0f, 14f), new Vector2(76f, 76f));
                cellPortrait[i] = pr.gameObject.AddComponent<Image>();
                cellPortrait[i].preserveAspect = true;
                cellPortrait[i].raycastTarget = false;
                cellName[i] = UIKit.Text(b.transform, "", 18f, Parch, new Vector2(0f, -44f), new Vector2(130f, 24f));
                cellBadge[i] = UIKit.Text(b.transform, "", 30f, Gold, new Vector2(-48f, 36f), new Vector2(34f, 34f));
                cellBadge[i].fontStyle = FontStyles.Bold;
                b.interactable = CharacterData.IsUnlocked((CharacterId)i);
                b.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                if (!b.interactable)
                {
                    ColorBlock cb = b.colors;
                    cb.disabledColor = new Color(0.4f, 0.4f, 0.45f, 0.9f);
                    b.colors = cb;
                }
                cells.Add(b.gameObject);
            }

            // 오른쪽: 이 조합 최고 기록 · 조합 순위 상위 5
            RectTransform side = UIKit.Rect("Side", win, new Vector2(450f, 25f), new Vector2(330f, 330f));
            Image sbg2 = side.gameObject.AddComponent<Image>();
            sbg2.color = new Color(0f, 0f, 0f, 0.18f);
            sbg2.raycastTarget = false;
            recordText = UIKit.Text(side, "", 22f, Parch, new Vector2(0f, 135f), new Vector2(300f, 36f));
            recordText.fontSizeMin = 14f;
            UIKit.Text(side, "relay.pick.top", 20f, Gold, new Vector2(0f, 95f), new Vector2(300f, 28f));
            boardText = UIKit.Text(side, "", 20f, Parch, new Vector2(0f, -50f), new Vector2(300f, 220f), TextAlignmentOptions.TopLeft);
            boardText.enableAutoSizing = false;
            boardText.fontSize = 20f;
            boardText.lineSpacing = 6f;

            go = UIKit.MakeButton(win, "relay.pick.go", new Vector2(0f, -330f), new Vector2(340f, 80f), Depart, 32f);
            hint = UIKit.Text(win, "relay.pick.hint", 20f, Dim, new Vector2(0f, -385f), new Vector2(1000f, 28f));
            Refresh();
            StartCoroutine(Focus());
        }

        IEnumerator Focus()
        {
            yield return null;
            for (int i = 0; i < picked.Length; i++) { }
            if (cells.Count > 0 && cells[0] != null) GamePad.WarpTo((RectTransform)cells[0].transform);
        }

        void OnDestroy() { cells.Clear(); CloseConfirm(); }      // 2.0.1~ 고르기 창이 먼저 닫혀도 확인 창이 남지 않게

        void Toggle(int hero)
        {
            if (!CharacterData.IsUnlocked((CharacterId)hero)) return;
            int at = System.Array.IndexOf(picked, hero);
            if (at >= 0)
            {
                for (int i = at; i < picked.Length - 1; i++) picked[i] = picked[i + 1];     // 빠지고 뒤 칸이 당겨짐
                picked[picked.Length - 1] = -1;
            }
            else
            {
                int free = System.Array.IndexOf(picked, -1);
                if (free < 0) return;
                picked[free] = hero;
            }
            UISound.Click();
            Refresh();
        }

        GameObject confirm;

        void Depart()
        {
            if (System.Array.IndexOf(picked, -1) >= 0 || confirm != null) return;
            int[] order = (int[])picked.Clone();
            // 2.0.1~ 저장된 판이 있으면 일반 시작(MenuManager.OnPressStart)과 같은 확인 창 — 전엔 묻지 않고 새 릴레이 판이 이어하기 저장을 덮어썼음
            if (!RunSave.Has) { Close(); RelayRun.Begin(order); return; }
            RectTransform win = UIKit.Modal(root, "ConfirmNewRun", new Vector2(820f, 340f), out confirm);
            UIKit.EscCancel(confirm, CloseConfirm);
            UIKit.Text(win, "새 게임을 시작할까요?", 38f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 78f), new Vector2(760f, 60f));
            UIKit.Text(win, "저장된 판(이어하기)은 지워집니다.", 26f, Parch, new Vector2(0f, 20f), new Vector2(760f, 40f));
            Button ok = UIKit.MakeButton(win, "새 게임", new Vector2(-150f, -90f), new Vector2(260f, 76f), () =>
            {
                CloseConfirm();
                RunSave.Delete();
                Close();
                RelayRun.Begin(order);
            }, 28f);
            Button cancel = UIKit.MakeButton(win, "취소", new Vector2(150f, -90f), new Vector2(260f, 76f), CloseConfirm, 28f);
            UIKit.ConfirmKeys(win, ok, cancel, -143f, false);        // 저장된 판이 지워지므로 Enter 한 번에 확정되지 않게 (MenuManager 와 같게)
        }

        void CloseConfirm()
        {
            if (confirm != null) Destroy(confirm);
            confirm = null;
        }

        void Refresh()
        {
            bool full = System.Array.IndexOf(picked, -1) < 0;
            for (int k = 0; k < RelayRun.Runners; k++)
            {
                bool has = picked[k] >= 0;
                CharacterId id = has ? (CharacterId)picked[k] : CharacterId.Gunner;
                slotPortrait[k].sprite = has ? CharacterUI.MenuPortrait(id) : null;
                slotPortrait[k].enabled = has;
                slotName[k].text = has ? Loc.T(CharacterData.Def(id).name) : Loc.T("relay.pick.empty");
                slotName[k].color = has ? Gold : Dim;
                slotHeir[k].text = !has ? "" : k == RelayRun.Runners - 1 ? Loc.T("relay.pick.last") : Loc.T("relay.pick.heir").Replace("{0}", RelayHeirlooms.NameOf(id)) + "\n<size=78%>" + RelayHeirlooms.DescOf(id) + "</size>";
            }
            for (int i = 0; i < CharacterData.All.Length && i < cellPortrait.Length; i++)
            {
                bool known = CharacterData.IsUnlocked((CharacterId)i);
                cellPortrait[i].sprite = CharacterUI.MenuPortrait((CharacterId)i);
                cellPortrait[i].color = known ? Color.white : Color.black;            // 못 가진 영웅은 실루엣
                cellName[i].text = known ? Loc.T(CharacterData.Def((CharacterId)i).name) : (CharacterData.IsKnown((CharacterId)i) ? Loc.T(CharacterData.Def((CharacterId)i).name) : "???");
                int at = System.Array.IndexOf(picked, i);
                cellBadge[i].text = at >= 0 ? (at + 1).ToString() : "";           // 번호 배지 「1」 「2」 「3」
            }
            go.interactable = full;
            if (!full) GrayOut(go);
            else { go.GetComponent<Image>().color = Color.white; TMP_Text gl = go.GetComponentInChildren<TMP_Text>(); if (gl != null) gl.color = UiRules.Legacy ? Color.white : UiTokens.Text1; }
            ShowCombo(full ? RelayRun.ComboKey(picked) : "");
        }

        // 조합이 정해지면 그 조합의 최고 기록 · 순위 상위 5
        void ShowCombo(string combo)
        {
            if (combo == shownCombo) return;
            shownCombo = combo;
            request++;
            entries = null;
            failed = false;
            if (combo.Length == 0)
            {
                recordText.text = "";
                boardText.text = "";
                return;
            }
            float best = RelayRun.BestOfCombo(combo);
            recordText.text = Loc.T("relay.pick.best") + "  <color=#F5D478>" + (best > 0f ? EndlessMode.Clock(best) : "-") + "</color>";
            if (!SteamLeaderboards.Available) { boardText.text = "<color=#A89C86>" + Loc.T("스팀에 연결되어 있지 않아 순위를 볼 수 없습니다.") + "</color>"; return; }
            boardText.text = "<color=#A89C86>" + Loc.T("불러오는 중…") + "</color>";
            giveUp = Time.unscaledTime + 15f;
            int my = request;
            SteamLeaderboards.Top(SteamLeaderboards.Relay(combo), 5, (e, t) => { if (this == null || my != request) return; entries = e; total = t; failed = e == null; RenderBoard(); });
        }

        void RenderBoard()
        {
            if (boardText == null) return;
            Loc.EnsureCjkFallback(boardText);
            if (failed || entries == null) { boardText.text = "<color=#A89C86>" + Loc.T("순위를 불러오지 못했습니다. 창을 다시 열어 보세요.") + "</color>"; return; }
            if (entries.Count == 0) { boardText.text = "<color=#A89C86>" + Loc.T("아직 기록이 없습니다. 첫 번째 도전자가 되어 보세요!") + "</color>"; return; }
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            int shown = 0, myRank = 0;
            foreach (SteamLeaderboards.Entry e in entries)
            {
                // 2.0.1~ 「상위 5」인데 순위 밖 내 줄까지 6줄이었음 — 다섯 줄만, 순위 밖 내 기록은 아래 참가자 줄 옆에 「내 순위」로
                if (shown >= 5) { if (e.me) myRank = e.rank; continue; }
                shown++;
                string nm = SteamLeaderboards.DisplayName(e.user, 12);
                if (string.IsNullOrEmpty(nm)) nm = "…";
                string line = "#" + e.rank + "  " + nm + "  <color=#A89C86>" + EndlessMode.Clock(e.score) + "</color>";
                b.Append(e.me ? "<color=#F5D478>" + line + "</color>" : line).Append('\n');
            }
            b.Append("\n<color=#A89C86>").Append(Loc.T("참가자 ")).Append(total).Append("</color>");
            if (myRank > 0) b.Append("  <color=#F5D478>").Append(Loc.T("lb.myrank").Replace("{0}", myRank.ToString())).Append("</color>");
            boardText.text = b.ToString();
        }

        void Update()
        {
            // 스팀이 이름을 늦게 알려 줄 수 있어 1초마다 다시 씀
            if (entries == null && !failed && SteamLeaderboards.Available && shownCombo.Length > 0 && Time.unscaledTime > giveUp) { failed = true; RenderBoard(); }
            if (entries != null && Time.frameCount % 60 == 0) RenderBoard();
        }
    }
}
