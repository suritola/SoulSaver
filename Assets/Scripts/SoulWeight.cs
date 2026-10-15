using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 영혼의 무게 (1.2~ · 로드맵 2-1 누적 저주 계단): 어려움을 한 번이라도 클리어하면 열림 (이야기 모드 어려움 판에만)
// 무게 0 ~ 6. 무게마다 항목이 하나씩 쌓임 (고른 무게까지의 항목이 모두 걸림) · 무게 1마다 포인트 +5% (Point.TotalMul)
//   1 적 체력 +15%                  — 보통 → 어려움 한 단계(+13%)만큼. 잡몹 · 엘리트만 (보스는 1~3분 안에 잡히게 그대로)
//   2 한 화면 적 수 +3 (그 뒤 무게마다 +3 더, 무게 6 = +15) — 어려움 상한 26 → 최대 41 (무한 모드는 +90까지 버팀)
//   3 보스 결계 게이지 +35% 빨리 참  — 보스전마다 결계를 한 번쯤 더 봄
//   4 회복 물약 · 고기 회복 -40%    — 물약 40% → 24%, 고기 15 → 9
//   5 레벨업 카드 다시 뽑기 없음
//   6 최후의 보루 없음               — 마지막 안전장치까지 뺌
// 무게 1은 열리면 모든 영웅이 바로 고를 수 있고, 그다음 무게는 그 영웅으로 바로 아래 무게를 클리어해야 열림
// 영웅마다 가장 무거운 클리어를 'weight.best.<번호>' 에 · 고른 무게는 'weight.selected' · 판 중간 저장은 RunSave.Data.weight
// 무한 모드 · 일일 도전 · 체험판 · 튜토리얼 · 자동 플레이에는 걸리지 않음 (Level 이 0)
public static class SoulWeight
{
    public const int Max = 6;
    const string SelectedKey = "weight.selected";
    const string BestKey = "weight.best.";          // + 영웅 번호 → 이 영웅으로 클리어한 가장 무거운 무게 (0 = 아직 없음)

    // 항목 수치
    const float HpRate = 1.15f;
    const int AlivePer = 3;
    const float BarrierRate = 1.35f;
    const float HealRate = 0.6f;
    const float PointPer = 0.05f;

    // 무게마다 새로 쌓이는 항목 (번호 = 무게 - 1)
    static readonly string[] Steps =
    {
        "weight.step.hp", "weight.step.alive", "weight.step.barrier", "weight.step.heal", "weight.step.reroll", "weight.step.laststand",
    };

    // 이어하기로 불러온 판의 무게 (RunSave.Continue 가 정하고 EndContinue 가 거둠, 메뉴에서 고른 무게를 덮어쓰지 않게)
    public static int? Override;

    // 체험판은 어려움이 없으므로 늘 닫힘
    public static bool Unlocked => !Demo.On && GameMode.HasCleared(Difficulty.Hard);

    public static int Best(CharacterId id) => Mathf.Clamp(Prefs.GetInt(BestKey + (int)id, 0), 0, Max);
    // 이 영웅이 고를 수 있는 가장 무거운 무게
    public static int MaxOpen(CharacterId id) => Unlocked ? Mathf.Min(Max, Best(id) + 1) : 0;

    // 메뉴에서 고른 무게 (영웅을 바꿔 그 영웅이 아직 못 연 무게면 열린 데까지만 씀)
    public static int Selected
    {
        get => Mathf.Clamp(Prefs.GetInt(SelectedKey, 0), 0, Max);
        set { Prefs.SetInt(SelectedKey, Mathf.Clamp(value, 0, Max)); Prefs.Save(); }
    }

    // 이 판에 무게가 걸리는지 (어려움 이야기 모드만)
    static bool Applies => GameMode.Current == Difficulty.Hard && !DailyChallenge.Active && !Demo.On && !TutorialRun.Active
                           && !GameInput.Auto && !GameInput.TrailerRunning && !Application.isBatchMode;

    // 지금 판의 무게 (적 체력 · 생성 등에서 자주 부르므로 한 프레임에 한 번만 계산)
    static int cacheFrame = -1, cached;
    public static int Level
    {
        get
        {
            if (cacheFrame == Time.frameCount) return cached;
            cacheFrame = Time.frameCount;
            if (!Applies) cached = 0;
            else if (Override.HasValue) cached = Mathf.Clamp(Override.Value, 0, Max);
            else cached = Mathf.Min(Selected, MaxOpen(CharacterData.Selected));
            return cached;
        }
    }
    public static void Invalidate() => cacheFrame = -1;

    // ================================================================= 항목 (Level 기준)
    public static float EnemyHpMul => Level >= 1 ? HpRate : 1f;
    public static int ExtraAlive => AliveAt(Level);
    public static float BarrierGaugeMul => Level >= 3 ? BarrierRate : 1f;
    public static float HealMul => Level >= 4 ? HealRate : 1f;
    public static bool NoReroll => Level >= 5;
    public static bool NoLastStand => Level >= 6;
    public static float PointMul => PointAt(Level);

    static int AliveAt(int w) => w >= 2 ? AlivePer * (w - 1) : 0;
    static float PointAt(int w) => 1f + PointPer * w;
    static string Pct(float v) => Mathf.RoundToInt(v * 100f).ToString();

    // ================================================================= 클리어 기록
    // 이번 판에서 가장 무거운 클리어를 새로 세웠는지 (결과 화면 「새 기록」)
    public static bool JustRaised { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
        {
            Invalidate();
            if (s.name == "GameScene") JustRaised = false;
        };
    }

    // 마지막 보스를 쓰러뜨렸을 때 (StageManager, GameMode.OnCleared 바로 뒤) · 안내할 글 (없으면 null)
    //   wasUnlocked: 이 클리어 전에 이미 열려 있었는지 (처음 어려움을 깼으면 「영혼의 무게가 열렸다」)
    public static string OnCleared(CharacterId id, Difficulty d, bool wasUnlocked)
    {
        Invalidate();
        if (d != Difficulty.Hard || !Applies) return null;
        if (!wasUnlocked) return Unlocked ? Loc.T("weight.unlocked") : null;
        int w = Level;
        if (w <= 0 || w <= Best(id)) return null;
        Prefs.SetInt(BestKey + (int)id, w);
        Prefs.Save();
        JustRaised = true;
        string s = Loc.T("weight.cleared").Replace("{0}", w.ToString());
        if (w < Max) s += "\n" + Loc.T("weight.next").Replace("{0}", (w + 1).ToString());
        return s;
    }

    // ================================================================= 글
    // 무게 w 에 걸리는 항목 모두 (수치까지)
    public static string Summary(int w)
    {
        if (w <= 0) return Loc.T("weight.none");
        System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
        if (w >= 1) parts.Add(Loc.T("weight.fx.hp").Replace("{0}", Pct(HpRate - 1f)));
        if (w >= 2) parts.Add(Loc.T("weight.fx.alive").Replace("{0}", AliveAt(w).ToString()));
        if (w >= 3) parts.Add(Loc.T("weight.fx.barrier").Replace("{0}", Pct(BarrierRate - 1f)));
        if (w >= 4) parts.Add(Loc.T("weight.fx.heal").Replace("{0}", Pct(1f - HealRate)));
        if (w >= 5) parts.Add(Loc.T("weight.step.reroll"));
        if (w >= 6) parts.Add(Loc.T("weight.step.laststand"));
        return string.Join(" · ", parts);
    }

    public static string PointText(int w) => Loc.T("weight.points").Replace("{0}", Pct(PointAt(w) - 1f));

    // 결과 화면 · ESC 메뉴의 「영웅 · 난이도」 뒤에 붙는 글 (무게가 걸린 판이거나 기록이 있을 때)
    public static string RunTag()
    {
        if (!Unlocked || !Applies) return "";
        int w = Level, best = Best(CharacterData.Selected);
        if (w <= 0 && best <= 0) return "";
        string s = "  ·  " + Loc.T("weight.short").Replace("{0}", w.ToString());
        if (best > 0) s += " <size=80%><color=#A89C86>(" + Loc.T("weight.best").Replace("{0}", best.ToString()) + ")</color></size>";
        if (JustRaised) s += " <color=#7CFF9A>" + Loc.T("weight.newrecord") + "</color>";
        return s;
    }

    // 판 시작 · 이어하기 알림판 아래 한 줄 (무게가 걸린 판만, 아니면 빈 글)
    public static string BannerLine()
    {
        int w = Level;
        if (w <= 0) return "";
        return "\n<size=80%><color=#C9A2FF>" + Loc.T("weight.run").Replace("{0}", w.ToString()).Replace("{1}", Pct(PointAt(w) - 1f)) + "</color></size>";
    }

    // 캐릭터 화면 칸 · 자세히 창 (기록이 없으면 빈 글)
    public static string CardTag(CharacterId id)
    {
        int best = Best(id);
        return Unlocked && best > 0 ? Loc.T("weight.short").Replace("{0}", best.ToString()) : "";
    }

    public static string DetailLine(CharacterId id)
    {
        if (!Unlocked) return "";
        int best = Best(id);
        return best > 0 ? Loc.T("weight.detail").Replace("{0}", best.ToString()).Replace("{1}", MaxOpen(id).ToString())
                        : Loc.T("weight.detail.none");
    }

    // ================================================================= 메인 메뉴 (난이도 줄 아래, 게임 시작 버튼 밑 빈 줄)
    // 어려움을 고르면 「영혼의 무게 N · 포인트 +x%」 버튼 → 누르면 무게 고르는 창
    public static void InstallMenu(Transform parent, Vector2 at)
    {
        if (!Unlocked) return;
        Button b = UIKit.MakeButton(parent, "", at, new Vector2(520f, 32f), () => Open(parent.root), 20f);     // 게임 시작 버튼 아래끝 ~ 아이콘 칸 위끝 사이 (34) 안
        b.name = "SoulWeightButton";
        TMP_Text label = b.GetComponentInChildren<TMP_Text>(true);
        MenuLabel ml = b.gameObject.AddComponent<MenuLabel>();
        ml.label = label;
        ml.image = b.GetComponent<Image>();
        ml.Refresh();
    }

    class MenuLabel : MonoBehaviour
    {
        public TMP_Text label;
        public Image image;
        int shown = int.MinValue;

        // 영웅 · 난이도 · 무게 · 언어가 바뀌면 다시 씀 (영웅 창 · 난이도 버튼이 이 버튼을 모름)
        void Update()
        {
            int key = ((int)CharacterData.Selected * 1000 + (int)GameMode.Current * 100 + Selected * 10 + MaxOpen(CharacterData.Selected)) * 10 + (int)Loc.Current;
            if (key != shown) Refresh();
        }

        public void Refresh()
        {
            Invalidate();
            shown = ((int)CharacterData.Selected * 1000 + (int)GameMode.Current * 100 + Selected * 10 + MaxOpen(CharacterData.Selected)) * 10 + (int)Loc.Current;
            bool hard = GameMode.Current == Difficulty.Hard;
            image.enabled = hard;
            GetComponent<Button>().enabled = hard;
            label.gameObject.SetActive(hard);
            if (!hard) return;
            int w = Mathf.Min(Selected, MaxOpen(CharacterData.Selected));
            label.text = Loc.T("weight.title") + " <color=#F5D478>" + w + "</color>"
                       + (w > 0 ? "  <size=85%>" + PointText(w) + "</size>" : "")
                       + "  <size=85%><color=#A89C86>(" + Loc.T("weight.change") + ")</color></size>";
            image.color = w > 0 ? new Color(0.62f, 0.45f, 0.85f) : new Color(0.45f, 0.42f, 0.52f);
        }
    }

    // 무게 고르는 창: 무게마다 새로 쌓이는 항목 · 포인트 · 잠김, 아래에 고른 무게의 항목 모두
    static GameObject window;

    public static void Open(Transform root)
    {
        if (window != null) return;
        CharacterId hero = CharacterData.Selected;
        int open = MaxOpen(hero);
        const float RowH = 64f, W = 1000f;
        float h = 300f + RowH * (Max + 1);
        RectTransform win = UIKit.Modal(root, "SoulWeightWindow", new Vector2(W + 40f, h), out window);
        GameObject blocker = window;
        Color gold = new Color(0.96f, 0.83f, 0.47f), parch = new Color(0.93f, 0.9f, 0.84f), dim = new Color(0.66f, 0.61f, 0.53f);
        float top = h * 0.5f - 12f;

        UIKit.Text(win, "weight.title", 44f, gold, new Vector2(0f, top - 40f), new Vector2(W, 60f));
        TMP_Text sub = UIKit.Text(win, "", 20f, dim, new Vector2(0f, top - 86f), new Vector2(W - 40f, 30f));
        sub.text = Loc.T(CharacterData.Def(hero).name) + " · " + Loc.T("weight.subtitle");

        TMP_Text summary = UIKit.Text(win, "", 20f, parch, new Vector2(0f, -h * 0.5f + 120f), new Vector2(W - 40f, 56f));
        summary.enableWordWrapping = true;
        Button[] rows = new Button[Max + 1];
        System.Action paint = null;
        for (int i = 0; i <= Max; i++)
        {
            int w = i;
            float y = top - 140f - RowH * i;
            Button b = UIKit.MakeButton(win, "", new Vector2(0f, y), new Vector2(W - 40f, RowH - 8f), () =>
            {
                if (w > open) return;
                Selected = w;
                Invalidate();
                paint();
            }, 22f);
            rows[i] = b;
            TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
            t.alignment = TextAlignmentOptions.Left;
            string step = w == 0 ? Loc.T("weight.step.zero") : Loc.T(Steps[w - 1]);
            string right = w > open ? Loc.T("weight.locked").Replace("{0}", (w - 1).ToString())
                         : w > 0 ? PointText(w) : "";
            t.text = "<color=#F5D478>" + Loc.T("weight.short").Replace("{0}", w.ToString()) + "</color>   " + step
                   + (right.Length > 0 ? "    <size=80%><color=#A89C86>" + right + "</color></size>" : "");
            if (w > open) t.color = new Color(0.55f, 0.52f, 0.5f);
            if (w > open) b.interactable = false;
        }
        paint = () =>
        {
            int sel = Mathf.Min(Selected, open);
            for (int i = 0; i <= Max; i++)
            {
                Image img = rows[i].GetComponent<Image>();
                img.color = i > open ? new Color(0.25f, 0.24f, 0.28f, 0.8f) : i == sel ? gold : i < sel ? new Color(0.62f, 0.45f, 0.85f) : new Color(0.6f, 0.58f, 0.65f);
            }
            summary.text = "<color=#F5D478>" + Loc.T("weight.short").Replace("{0}", sel.ToString()) + "</color>  " + Summary(sel)
                         + (sel > 0 ? "\n" + PointText(sel) : "");
        };
        paint();

        System.Action close = () =>
        {
            if (blocker != null) Object.Destroy(blocker);
            window = null;
        };
        UIKit.MakeButton(win, "확인", new Vector2(0f, -h * 0.5f + 52f), new Vector2(260f, 64f), () => close(), 28f);
        UIKit.EscCancel(blocker, () => close());
    }
}
