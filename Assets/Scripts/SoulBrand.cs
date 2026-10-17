using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 영혼 각인 (1.2~): 이야기 모드 판 규칙 카드 — 「쉬움」을 한 번 클리어하면 열림
//   판을 시작할 때 3택 1, 2장 · 3장 보스를 쓰러뜨릴 때 한 장씩 더 (한 판 최대 3장, 같은 각인은 한 번만)
//   각인 목록 · 수치는 ContentRegistry.SoulBrands, 글은 LocTable.SoulBrand, 그림은 Icons/choice_brand_<id>
//   수치형 효과는 GameMode 가 일일 도전 배율과 함께 곱함 (Mul) · 포인트 배율은 Point.AddPoint 가 곱함 (PointMul)
//   공격력 · 최대 체력은 고를 때 한 번 바꾸고 판 중간 저장 기록 'a' · 'h' 를 남김 (이어할 때 RunSave 가 같은 순서로 다시 적용)
//   이어하기: RunSave.Data.soulBrandOn · soulBrands (옛 저장은 각인 없는 판) · 고르기 전에 나갔으면 이어할 때 다시 고름
//   무한 모드 · 일일 도전 · 튜토리얼 · 체험판(정식판에서만 · 저주 제단 · 보스 보상처럼) · 자동 플레이에서는 나오지 않음
public static class SoulBrand
{
    public const int MaxPerRun = 3;
    const int Choices = 3;

    static readonly List<SoulBrandDef> run = new List<SoulBrandDef>();
    static bool picking;

    // 이번 판이 각인을 쓰는 판인지 (판을 시작할 때 정함, 이어하기는 저장값)
    public static bool RunOn { get; private set; }
    public static IReadOnlyList<SoulBrandDef> Current => run;

    // 영혼 각인이 열렸는지 (이야기 모드 「쉬움」 클리어, 체험판은 정식판에서만)
    public static bool FeatureUnlocked => !Demo.On && GameMode.HasCleared(Difficulty.Easy);

    static bool Off => GameMode.IsEndless || DailyChallenge.Active || TutorialRun.Active || Demo.On
                       || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode;

    // ================================================================= 해금 조건에 쓰는 기록 (모두 이미 있는 기록)
    public const int GreedKills = 5000;
    public static int TotalKills => Prefs.GetInt(SteamManager.TotalKillsKey, 0);
    public static int SkillEvoCount => Prefs.GetString("stats.skillEvos", "").Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries).Length;

    // ================================================================= 배율 (GameMode · Point)
    public static float Mul(DailyStat stat)
    {
        if (run.Count == 0) return 1f;
        float m = 1f;
        foreach (SoulBrandDef b in run)
            foreach ((DailyStat s, float mul) in b.muls) if (s == stat) m *= mul;
        return m;
    }

    public static float PointMul
    {
        get
        {
            float m = 1f;
            foreach (SoulBrandDef b in run) m *= b.pointMul;
            return m;
        }
    }

    // 이 장에 있을 때 가져야 하는 각인 수: 1장 · 2장 1장, 2장 보스 뒤(3장) 2장, 3장 보스 뒤(4장) 3장
    static int Expected(int stage) => stage < 0 ? 0 : 1 + Mathf.Clamp(Chapters.SlotOf(stage) - 1, 0, MaxPerRun - 1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Point.RegisterMul("brand", () => PointMul);         // 포인트 배율 (각인 없는 판이면 1)
        SceneManager.sceneLoaded += (s, m) =>
        {
            // 새 판 · 메인 메뉴에서 지난 판 각인을 지움 (결과 화면에서는 남겨 둠)
            if (s.name != "GameScene" && s.name != "MainMenu") return;
            run.Clear();
            RunOn = false;
            picking = false;
            if (s.name == "GameScene") new GameObject("SoulBrandHud").AddComponent<Hud>();
        };
    }

    // ================================================================= 판 안
    // StageManager.Opening: 시작 이야기 뒤, 첫 카드 창 전에 3택 1
    public static IEnumerator RunStart()
    {
        run.Clear();
        RunOn = false;
        if (Off || !FeatureUnlocked) yield break;
        RunOn = true;
        LevelShop shop = Object.FindFirstObjectByType<LevelShop>();
        GameObject cards = shop != null ? shop.LvshopPanel : null;
        bool cardsWere = cards != null && cards.activeSelf;
        if (cards != null) cards.SetActive(false);          // 첫 카드 창은 각인을 고른 뒤에
        float timeWas = Time.timeScale;                     // ChoiceUI 는 멈춤(0)이었으면 1 로 되돌리므로 닫은 뒤 다시 맞춤
        yield return Pick("brand.pick.start", true);
        // 첫 카드 창을 다시 띄우면 창이 멈춰 둔 그대로 (0) · 카드 창이 없었으면 멈춘 채 굳지 않게 흐르는 시간으로
        if (cards != null && cardsWere)
        {
            cards.SetActive(true);
            Time.timeScale = 0f;
        }
        else Time.timeScale = timeWas > 0f ? timeWas : 1f;
    }

    // 보스를 쓰러뜨린 뒤 (StageManager.EvolutionThenPortal, 다음 장 번호) · 장에 들어갈 때 · 이어할 때: 모자란 각인을 고름
    public static IEnumerator CatchUp(int stage)
    {
        if (!RunOn || Off || run.Count >= Expected(stage)) yield break;
        yield return new WaitForSecondsRealtime(0.4f);
        while (run.Count < Expected(stage))
        {
            PlayerController p = Hostile.Player;
            if (p == null || p.IsDying || p.PlayerHealth <= 0f || RunSave.FinalBossDown) yield break;
            StageManager sm = StageManager.Instance;
            // 다른 창(진화 · 보스 보상 · 저주 제단 · 장 넘어가기 · ESC · 연출)이 닫힐 때까지
            //   이어하기 복원(RunSave.Restore: 진화 → 보스 보상 → 문)이 끝나기 전에도 기다림 — 보스 보상이 0.5초 뒤에 뜨는 사이에 각인이 먼저 끼어들지 않게
            if (picking || Time.timeScale == 0f || ChoiceUI.Open || ESCmenu.IsOpen || StoryDirector.Playing || (sm != null && (sm.IsMenuOpen || !sm.Started)))
            {
                if (picking) yield break;                   // 다른 곳에서 이미 고르는 중
                yield return null;
                continue;
            }
            int before = run.Count;
            yield return Pick(Loc.T("brand.pick.boss").Replace("{0}", (run.Count + 1) + "/" + MaxPerRun));
            if (run.Count == before) yield break;           // 고를 각인이 없음
        }
    }

    // 1.2~ 지난 판에 처음 고른 각인 (「지난 판 각인 그대로」 · 판 시작 1장만, 2 · 3장 보스 뒤 각인은 새로) — 각인 없는 판(일일 · 무한 · 첫 판)이면 없음
    const string LastKey = "brand.last";
    static SoulBrandDef LastStart
    {
        get
        {
            SoulBrandDef b = ContentRegistry.SoulBrandByCode(Prefs.GetString(LastKey, ""));
            return b != null && b.Unlocked ? b : null;
        }
    }

    static IEnumerator Pick(string sub, bool start = false)
    {
        if (picking || run.Count >= MaxPerRun) yield break;
        SoulBrandDef same = start ? LastStart : null;
        List<SoulBrandDef> pool = new List<SoulBrandDef>();
        foreach (SoulBrandDef b in ContentRegistry.SoulBrands) if (b.Unlocked && !run.Contains(b)) pool.Add(b);
        if (pool.Count == 0) yield break;
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        int n = Mathf.Min(Choices, pool.Count);
        ChoiceUI.Option[] options = new ChoiceUI.Option[n];
        for (int i = 0; i < n; i++) options[i] = new ChoiceUI.Option(pool[i].NameKey, CardText(pool[i]), pool[i].color, pool[i].Icon);
        picking = true;
        int pick = 0;
        if (same != null) { ChoiceUI.ExtraLabel = "brand.same"; ChoiceUI.ExtraTip = "brand.same.tip"; }
        try { yield return ChoiceUI.Run("brand.title", sub, options, k => pick = k); }
        finally { picking = false; }
        SoulBrandDef got = pick >= n && same != null ? same : pool[Mathf.Clamp(pick, 0, n - 1)];
        if (start) { Prefs.SetString(LastKey, got.saveCode); Prefs.Save(); }
        Apply(got);
    }

    // 카드 · 목록 설명: 효과 + 포인트 배율 (1보다 클 때만)
    static string CardText(SoulBrandDef b) =>
        b.Desc + (b.pointMul > 1.001f ? "\n<color=#F5D478>" + Loc.T("brand.points").Replace("{0}", MulText(b.pointMul)) + "</color>" : "");

    static string MulText(float m) => m.ToString("0.0#", CultureInfo.InvariantCulture);

    static void Apply(SoulBrandDef b)
    {
        if (b == null || run.Contains(b)) return;
        run.Add(b);
        CodexSeen.Mark("brand." + b.saveCode, b.Name);          // 1.2~ 발견형 도감
        PlayerController p = Hostile.Player;
        if (p != null)
        {
            if (b.attack != 0)
            {
                p.damage *= 1f + b.attack / 100f;
                RunSave.Record("a" + b.attack.ToString(CultureInfo.InvariantCulture));       // 보스 보상 「전설의 힘」과 같은 기록
            }
            if (b.maxHp != 0)
            {
                float delta = Mathf.Round(p.PlayerMaxHealth * b.maxHp / 100f);
                if (p.PlayerMaxHealth + delta < 1f) delta = 1f - p.PlayerMaxHealth;
                p.PlayerMaxHealth += delta;
                p.PlayerHealth = Mathf.Clamp(p.PlayerHealth + Mathf.Max(0f, delta), 1f, p.PlayerMaxHealth);
                RunSave.Record("h" + delta.ToString("0", CultureInfo.InvariantCulture));     // 보물 상자 「생명」과 같은 기록 (음수도 됨)
            }
        }
        Hostile.Play("roar", 0.4f, 1.4f);
        if (p != null) Fx.Spawn("fx_soulburst", p.transform.position, 5f, b.color, 14f);
        Notice.Big(Loc.T("brand.got").Replace("{0}", b.Name), b.Desc.Replace("\n", " · "), NoticeIcon.Soul, 3.5f, NoticeLevel.Important, b.color, Notice.IconFile("choice_" + b.Icon));
        Hud.Dirty = true;
    }

    // 결과 화면 · ESC 요약 줄의 「영웅 · 난이도 · 무게」 뒤에 붙는 글 (각인이 없으면 빈 글, SoulWeight.RunTag 와 같은 자리)
    public static string RunTag()
    {
        if (run.Count == 0) return "";
        List<string> names = new List<string>();
        foreach (SoulBrandDef b in run) names.Add(b.Name);
        return "  ·  " + Loc.T("brand.tag").Replace("{0}", string.Join(", ", names));
    }

    // 판 기록(balance_log) 칸: saveCode 를 | 로 (없으면 빈 글)
    public static string LogCodes()
    {
        List<string> codes = new List<string>();
        foreach (SoulBrandDef b in run) codes.Add(b.saveCode);
        return string.Join("|", codes);
    }

    // ================================================================= 이어하기 (RunContent)
    public static void SaveRun(RunSave.Data d)
    {
        d.soulBrandOn = RunOn;
        List<string> codes = new List<string>();
        foreach (SoulBrandDef b in run) codes.Add(b.saveCode);
        d.soulBrands = string.Join(",", codes);
    }

    public static void RestoreRun(RunSave.Data d)
    {
        run.Clear();
        RunOn = d.soulBrandOn && !Off;
        if (!RunOn) return;
        foreach (string code in (d.soulBrands ?? "").Split(','))
        {
            SoulBrandDef b = ContentRegistry.SoulBrandByCode(code.Trim());
            if (b != null && !run.Contains(b) && run.Count < MaxPerRun) run.Add(b);      // 효과(공격력 · 최대 체력)는 기록 'a' · 'h' 가 이미 다시 적용함
        }
        Hud.Dirty = true;
        // 고르기 전에 나갔으면 (판 시작 · 보스 뒤 · 장에 들어온 직후) 이어할 때 고름
        if (run.Count < Expected(d.stage) && StageManager.Instance != null) StageManager.Instance.StartCoroutine(CatchUp(d.stage));
    }

    // ================================================================= ESC 메뉴: 왼쪽에 이번 판 각인
    class Hud : MonoBehaviour
    {
        public static bool Dirty;
        GameObject panel;

        void Update()
        {
            bool want = ESCmenu.IsOpen && run.Count > 0;
            if (want && (panel == null || Dirty)) Build();
            else if (!want && panel != null) { Destroy(panel); panel = null; }
        }

        void OnDestroy()
        {
            if (panel != null) Destroy(panel);
        }

        void Build()
        {
            Dirty = false;
            if (panel != null) Destroy(panel);
            Canvas canvas = UIKit.HudCanvas();
            if (canvas == null) return;
            UIKit.EnsureStyle();
            const float rowH = 118f;
            float h = 110f + rowH * run.Count;
            panel = new GameObject("SoulBrandPanel", typeof(RectTransform), typeof(Image));
            RectTransform r = panel.GetComponent<RectTransform>();
            r.SetParent(canvas.transform, false);
            r.SetAsLastSibling();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(30f, -20f);        // 오른쪽 「이번 판」 요약과 마주 보게 · ESC 창(가운데 폭 720)과 안 겹치게
            r.sizeDelta = new Vector2(440f, h);
            Image bg = panel.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.05f, 0.08f, 0.88f);
            bg.raycastTarget = false;

            UIKit.Text(r, "brand.esc", 28f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, h / 2f - 34f), new Vector2(400f, 40f));
            for (int i = 0; i < run.Count; i++)
            {
                SoulBrandDef b = run[i];
                float y = h / 2f - 80f - rowH * (i + 0.5f) + 10f;
                Sprite icon = Resources.Load<Sprite>(ChoiceUI.IconPath(b.Icon));
                if (icon != null)
                {
                    Image ii = UIKit.Rect("Icon", r, new Vector2(-160f, y), new Vector2(72f, 72f)).gameObject.AddComponent<Image>();
                    ii.sprite = icon;
                    ii.preserveAspect = true;
                    ii.raycastTarget = false;
                }
                UIKit.Text(r, b.NameKey, 24f, b.color, new Vector2(46f, y + 30f), new Vector2(320f, 34f), TextAlignmentOptions.Left);
                TMP_Text d = UIKit.Text(r, "", 18f, new Color(0.93f, 0.9f, 0.84f), new Vector2(46f, y - 20f), new Vector2(320f, 64f), TextAlignmentOptions.TopLeft);
                d.text = CardText(b);
            }
            TMP_Text total = UIKit.Text(r, "", 22f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, -h / 2f + 28f), new Vector2(400f, 34f));
            total.text = Loc.T("brand.esc.total").Replace("{0}", MulText(PointMul));
        }
    }

    // ================================================================= 메인 메뉴: 각인 목록 · 해금 조건
    const string SeenKey = "brand.seen";        // 열린 것을 본 각인 saveCode (쉼표) — 새로 열린 각인이 있으면 버튼이 반짝임

    static HashSet<string> Seen() => new HashSet<string>(Prefs.GetString(SeenKey, "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));

    static bool HasUnseen()
    {
        if (!FeatureUnlocked) return false;
        HashSet<string> seen = Seen();
        foreach (SoulBrandDef b in ContentRegistry.SoulBrands) if (b.Unlocked && !seen.Contains(b.saveCode)) return true;
        return false;
    }

    static void MarkSeen()
    {
        if (!FeatureUnlocked) return;
        HashSet<string> seen = Seen();
        bool changed = false;
        foreach (SoulBrandDef b in ContentRegistry.SoulBrands) if (b.Unlocked && seen.Add(b.saveCode)) changed = true;
        if (!changed) return;
        Prefs.SetString(SeenKey, string.Join(",", seen));
        Prefs.Save();
    }

    // MenuExtras.InstallMainMenu: 오른쪽 위 패치노트 버튼 아래
    public static void InstallMenuButton(Transform parent, Transform root)
    {
        if (parent == null || parent.Find("SoulBrandButton") != null) return;
        Button b = UIKit.MakeButton(parent, "brand.title", Vector2.zero, new Vector2(220f, 56f), () => Open(root), 24f);
        b.name = "SoulBrandButton";
        RectTransform br = (RectTransform)b.transform;
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(1f, 1f);
        br.anchoredPosition = new Vector2(-30f, -170f);
        if (!FeatureUnlocked)
        {
            Image img = b.GetComponent<Image>();
            if (img != null) img.color = new Color(0.55f, 0.53f, 0.6f);
            TMP_Text t = b.GetComponentInChildren<TMP_Text>();
            if (t != null) t.color = new Color(0.66f, 0.61f, 0.53f);
        }
        else if (HasUnseen()) b.gameObject.AddComponent<NewGlow>();
    }

    static GameObject open;
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);

    public static void Open(Transform root)
    {
        if (open != null) return;
        bool feature = FeatureUnlocked;
        RectTransform win = UIKit.Modal(root, "SoulBrand", new Vector2(1600f, 900f), out open);
        UIKit.EscCancel(open, Close);
        UIKit.Text(win, "brand.title", 46f, Gold, new Vector2(0f, 400f), new Vector2(900f, 64f));
        UIKit.MakeButton(win, "닫기", new Vector2(680f, 400f), new Vector2(150f, 58f), Close, 22f);
        TMP_Text how = UIKit.Text(win, "brand.menu.how", 22f, Parch, new Vector2(0f, 330f), new Vector2(1440f, 70f));
        how.enableAutoSizing = true;
        int opened = 0;
        foreach (SoulBrandDef b in ContentRegistry.SoulBrands) if (b.Unlocked) opened++;
        TMP_Text status = UIKit.Text(win, "", 22f, feature ? Gold : new Color(1f, 0.6f, 0.5f), new Vector2(0f, 280f), new Vector2(1440f, 32f));
        status.text = feature ? Loc.T("brand.menu.count").Replace("{0}", opened.ToString()).Replace("{1}", ContentRegistry.SoulBrands.Count.ToString())
                              : Loc.T(Demo.On ? "정식판에서 만날 수 있습니다" : "brand.menu.locked");

        // 4 × 3 칸
        const float w = 360f, h = 176f, gapX = 20f, gapY = 18f;
        List<SoulBrandDef> all = ContentRegistry.SoulBrands;
        for (int i = 0; i < all.Count; i++)
        {
            SoulBrandDef b = all[i];
            bool unlocked = b.Unlocked;
            Vector2 at = new Vector2(-1.5f * (w + gapX) + (i % 4) * (w + gapX), 165f - (i / 4) * (h + gapY));
            RectTransform card = UIKit.Rect("Brand_" + b.id, win, at, new Vector2(w, h));
            Image ci = card.gameObject.AddComponent<Image>();
            ci.sprite = UIKit.ButtonSprite;
            ci.type = Image.Type.Sliced;
            ci.color = unlocked ? Color.Lerp(new Color(0.16f, 0.13f, 0.2f), b.color, 0.22f) : new Color(0.13f, 0.12f, 0.15f);
            ci.raycastTarget = false;

            Sprite icon = Resources.Load<Sprite>(ChoiceUI.IconPath(b.Icon));
            if (icon != null)
            {
                Image ii = UIKit.Rect("Icon", card, new Vector2(-128f, 18f), new Vector2(76f, 76f)).gameObject.AddComponent<Image>();
                ii.sprite = icon;
                ii.preserveAspect = true;
                ii.raycastTarget = false;
                if (!unlocked) ii.color = new Color(0.3f, 0.3f, 0.34f, 0.8f);       // 잠긴 각인은 그림자처럼
            }
            UIKit.Text(card, b.NameKey, 26f, unlocked ? b.color : Dim, new Vector2(40f, 56f), new Vector2(250f, 36f), TextAlignmentOptions.Left);
            TMP_Text desc = UIKit.Text(card, "", 17f, unlocked ? Parch : Dim, new Vector2(40f, -6f), new Vector2(255f, 80f), TextAlignmentOptions.TopLeft);
            if (unlocked) desc.text = b.Desc;
            else
            {
                string prog = b.progress != null ? "\n<color=#A89C86>" + b.progress() + "</color>" : "";
                desc.text = "<color=#F5D478>" + Loc.T("brand.menu.unlock") + "</color>\n" + b.Hint + prog;
            }
            TMP_Text foot = UIKit.Text(card, "", 18f, unlocked ? Gold : Dim, new Vector2(0f, -66f), new Vector2(w - 30f, 28f));
            foot.text = unlocked ? (b.pointMul > 1.001f ? Loc.T("brand.points").Replace("{0}", MulText(b.pointMul)) : "") : Loc.T("잠김");
        }
        MarkSeen();
        GameObject button = GameObject.Find("SoulBrandButton");
        NewGlow glow = button != null ? button.GetComponent<NewGlow>() : null;
        if (glow != null) glow.enabled = false;         // 다음에 메뉴를 열 때는 반짝이지 않음 (NEW 배지는 메뉴를 다시 그릴 때 사라짐)
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }
}
