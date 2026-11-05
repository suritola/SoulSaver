#if SOULSAVER_FESTIVAL
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// SW페스티벌 심사용 「테스터 메뉴」 (BuildScript -festival 빌드에만 · 정식판 · 체험판 · 스팀 빌드에는 이 파일 전체가 컴파일에서 빠짐)
//   메인 메뉴 오른쪽 위 [테스터 메뉴] → 영웅 · 난이도 · 갈래 · 스킬 레벨 · 무기 진화 · 영혼 트리 · 보스를 골라 [바로 시작]
//   흐름 (녹화 도구 GameplayCapture 의 리플렉션 흐름을 실제 타입으로):
//     1) CharacterData.Override · GameMode.Override 를 걸고 GameScene (역류 · 릴레이와 같은 방식, 메인 메뉴로 돌아오면 풂)
//     2) StageManager.Opening 이 이야기 · 영웅 안내를 건너뜀 (TesterRun.Active) — 영혼 각인 · 특수 능력 창은 게임 그대로
//     3) 시작 순서가 끝나면 (OpeningDone) 입문 창을 닫고 LevelShop.TesterFill 로 몸을 채움 (역류 자동 채우기와 같은 우선순위 · 실제 진화 재료)
//     4) 영혼 트리 (이 판에만 · 저장된 것 없음) → 보스: 이야기 판은 그 장으로 JumpToStage 뒤 처치 수를 보스 직전으로 (원래 소환 길 그대로)
//        무한 판은 EndlessMode.SpawnBossKind / 개발자 · 엄마는 원래 강림 코루틴 (EndlessMode.TesterSummon)
//   판 중간 저장 · 텔레메트리는 끔 (RunSave.Disabled · Telemetry.Blocked) — 테스터 판이 「이어하기」와 밸런스 자료에 섞이지 않게
public static class TesterRun
{
    public const int BossNone = -1, BossDeveloper = 4, BossMom = 5;
    // 보스 번호 0 ~ 3 = 스테이지 번호 = bosss.bossKind (0 리치 왕 · 1 지옥의 군주 · 2 킹 슬라임 · 3 거울의 군주)

    public class Config
    {
        public CharacterId hero = CharacterId.Gunner;
        public Difficulty difficulty = Difficulty.Normal;
        public int path = -1;           // -1 = 카드로 직접 (평소처럼 입문 창) · 0 ~ 4 갈래
        public int levels;              // 자동으로 올릴 레벨업 수 (0 · 10 · 20 · 39) — 갈래를 골랐을 때만
        public int freePicks;           // 직접 고를 레벨업 카드 (쌓인 레벨업으로 · [Space])
        public int form;                // 1차 무기 형태 번호 (RefluxPrepUI.FormNames 순서)
        public int evoTier;             // 무기 진화 단계 0 없음 · 1 1차 · 2 2차 · 3 각성
        public int tree;                // 영혼 트리 0 없음 · 1 추천 · 2 전부
        public int boss = BossNone;
    }

    public static bool Active { get; private set; }
    public static Config Cfg { get; private set; } = new Config();
    static bool disabledSave;

    public static void Begin(Config c)
    {
        if (c == null) return;
        ModeRun.EndAll();
        Cfg = c;
        if (c.boss == BossDeveloper || c.boss == BossMom) c.difficulty = Difficulty.Endless;     // 개발자 · 엄마는 무한 모드에만 있음
        Active = true;
        GameMode.Override = c.difficulty;
        CharacterData.Override = c.hero;
        if (!RunSave.Disabled) { RunSave.Disabled = true; disabledSave = true; }
        SceneFlow.Go("GameScene");
    }

    public static void End()
    {
        if (!Active) return;
        Active = false;
        GameMode.Override = null;
        CharacterData.Override = null;
        if (disabledSave) { RunSave.Disabled = false; disabledSave = false; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name == "MainMenu") End();
            else if (s.name == "GameScene" && Active)     // 「다시 시작」도 같은 구성으로
            {
                GameObject go = new GameObject("TesterDirector");
                SceneManager.MoveGameObjectToScene(go, s);      // 그 판 장면과 함께 사라지게
                go.AddComponent<TesterDirector>();
            }
        };
    }

    // ================================================================= 추천 구성 (게임 데이터에서)
    // 갈래: 기존 스킬 진화로 이어지는 교차(CrossDef.evo)가 가장 많은 갈래 — 갈래 하나로 닿는 실제 진화 조합이 가장 많음 (같으면 앞 번호)
    public static int RecommendedPath(CharacterId who)
    {
        HeroPaths hp = Paths.Data(who);
        if (hp == null || !Paths.Supported(who)) return 0;
        int best = 0, bestScore = -1;
        for (int p = 0; p < Paths.Count && p < hp.Defs.Length; p++)
        {
            int score = !string.IsNullOrEmpty(hp.Defs[p].evo) ? 1 : 0;
            foreach (CrossDef x in hp.Crosses) if (!string.IsNullOrEmpty(x.evo) && (x.a == p || x.b == p)) score++;
            if (score > bestScore) { bestScore = score; best = p; }
        }
        return best;
    }

    // 갈래 · 레벨 40(39번 레벨업) · 무기 진화 각성까지 · 추천 영혼 트리 — 카드는 TesterFill 이 진화 재료(MyEvos) 우선으로 고름
    public static void FillRecommended(Config c)
    {
        c.path = RecommendedPath(c.hero);
        c.levels = 39;
        c.form = 0;             // 막힌 형태(지도 제작자 일필 + 지구본)는 판 안에서 NextEvolutionOptions 가 거르고 첫 선택지로
        c.evoTier = 3;
        c.tree = 1;
    }
}

// ===================================================================== 판 안: 구성 적용 → 보스 (GameScene 마다 새로 · 장면과 함께 사라짐)
public class TesterDirector : MonoBehaviour
{
    const float OpeningWait = 180f;     // 시작 순서(영혼 각인 등) 대기 상한 — 넘으면 아무것도 바꾸지 않고 끝 (1.2.7 교훈: 대기에는 상한)

    IEnumerator Start()
    {
        TesterRun.Config c = TesterRun.Cfg;
        float until = Time.realtimeSinceStartup + OpeningWait;
        StageManager sm = null;
        while (Time.realtimeSinceStartup < until)
        {
            sm = StageManager.Instance;
            if (sm != null && sm.OpeningDone) break;
            yield return null;
        }
        if (sm == null || !sm.OpeningDone) { Debug.LogWarning("[테스터] 시작 순서가 끝나지 않아 구성을 적용하지 않음"); yield break; }
        LevelShop shop = FindFirstObjectByType<LevelShop>();
        PlayerController player = Hostile.Player;
        if (shop == null || player == null) { Debug.LogWarning("[테스터] LevelShop · 플레이어가 없음"); yield break; }

        // 1) 몸: 갈래를 골랐으면 입문 창 대신 자동 채우기 (CancelOpening 이 시간도 1 로 되돌림)
        if (c.path >= 0 || c.evoTier > 0)
        {
            if (c.path >= 0) shop.CancelOpening();
            shop.TesterFill(c.hero, c.path, c.path >= 0 ? c.levels : 0, c.evoTier, c.form);
        }
        if (c.freePicks > 0) shop.GrantFreePicks(c.freePicks);

        // 2) 영혼 트리 (무기 진화 뒤 — 칸이 지금 무기를 따름) · 이 판에만
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        int learned = c.tree > 0 && sp != null ? LearnTree(sp, c.tree == 2) : 0;

        Notice.Big(Loc.T("테스터 구성 적용"), Summary(c, learned), NoticeIcon.Star, 2.5f, NoticeLevel.Important);

        // 3) 보스
        if (c.boss == TesterRun.BossNone) yield break;
        if (GameMode.IsEndless)
        {
            EndlessMode em = null;
            for (int i = 0; i < 30 && (em = EndlessMode.Instance) == null; i++) yield return null;
            if (em == null) { Debug.LogWarning("[테스터] EndlessMode 가 없음"); yield break; }
            yield return new WaitForSeconds(1f);
            if (c.boss == TesterRun.BossDeveloper || c.boss == TesterRun.BossMom) em.TesterSummon(c.boss == TesterRun.BossMom);
            else em.SpawnBossKind(c.boss, 1f);
        }
        else
        {
            if (sm.CurrentStage != c.boss) sm.JumpToStage(c.boss);
            // 처치 수를 보스 직전으로 (RestoreKills 가 페이즈 · 중간 보스 단계도 맞춤 → 중간 보스가 함께 튀어나오지 않음) → 다음 적 생성 때 원래 길로 보스 등장
            sm.spawner.RestoreKills(sm.spawner.BossKillTarget);
            player.GrantInvincibility(1.5f);
        }
    }

    // 영혼 트리: all = 열린 칸을 모두 · 아니면 추천 칸(지금 무기에 맞는 금빛 칸 SoulNode.recommended)과 무기 · 생존 가지
    //   배울 수 없는 칸(BlockReason)은 건너뜀 · 칸을 배우면 다음 칸이 열리므로 바뀌지 않을 때까지 되풀이 (상한 40바퀴)
    static int LearnTree(SpecialAbilities sp, bool all)
    {
        int n = 0;
        for (int round = 0; round < 40; round++)
        {
            bool any = false;
            foreach (SpecialAbilities.SoulNode node in sp.BuildSoulTree())
            {
                if (!sp.IsOpenNode(node) || sp.BlockReason(node) != null) continue;
                if (!all && !node.recommended && node.branch != 0 && node.branch != 3) continue;
                sp.ReplayNode(node.key);
                if (sp.OwnsNode(node.key)) { n++; any = true; }
            }
            if (!any) break;
        }
        return n;
    }

    static string Summary(TesterRun.Config c, int learned)
    {
        string s = Loc.T(CharacterData.Def(c.hero).name);
        PathDef d = c.path >= 0 ? Paths.DefOf(c.hero, c.path) : null;
        if (d != null) s += " · " + Loc.T(d.name) + " · Lv " + (1 + c.levels);
        if (c.evoTier > 0) s += " · " + Loc.T("무기 진화") + " " + c.evoTier;
        if (learned > 0) s += " · " + Loc.T("영혼 트리") + " " + learned;
        return s;
    }
}

// ===================================================================== 몸 채우기 (LevelShop.AutoFill 과 같은 길 · 레벨 수 · 진화 단계를 고를 수 있게)
//   AutoFill(역류)은 39번 · 각성까지 · 역류의 축복이 고정이라 그 함수를 바꾸지 않고 여기 따로 둠 (정식판 동작 그대로)
public partial class LevelShop
{
    public void TesterFill(CharacterId who, int path, int levels, int evoTier, int form)
    {
        bul = FindFirstObjectByType<PlayerController>();
        skill = FindFirstObjectByType<SkillGauge>();
        if (lv == null) lv = FindFirstObjectByType<Level>();
        if (bul == null) return;
        Random.State saved = Random.state;
        Random.InitState(RefluxRun.SeedOf(who, Mathf.Max(0, path), form));
        RunSave.SetAutoFill(true);          // 연출 · 소리 · 알림 없이 (이어하기 재적용과 같은 길) — finally 에서 반드시 끔
        try
        {
            if (path >= 0)
            {
                path = Mathf.Clamp(path, 0, Paths.Count - 1);
                if (Paths.Active && Paths.A < 0) ReplayPick(Paths.SeedFirstId + path, true);
                for (int step = 0; step < levels; step++)
                {
                    PathWindow w = PathWindowDue();
                    if (w == PathWindow.Crossroads) { ReplayPick(Paths.SingleId, false); continue; }
                    if (w == PathWindow.Seed) { ReplayPick(Paths.SeedFirstId + path, false); continue; }
                    int pick = RefluxAutoPick(who, path);       // 갈래 카드 > 덜 찬 스킬 진화 재료 > 가진 카드 > 생존
                    if (pick < 0) { if (bul.level < LevelCap) bul.level++; continue; }
                    ReplayPick(pick, false);
                }
            }
            SpecialAbilities sp = SpecialAbilities.SharedInstance;
            if (sp != null)
            {
                int[] forms = sp.RefluxFormIds();
                int formId = forms != null && forms.Length > 0 ? forms[Mathf.Clamp(form, 0, forms.Length - 1)] : -1;
                for (int t = 0; t < evoTier; t++)
                {
                    int[] opts = sp.NextEvolutionOptions();
                    if (opts == null || opts.Length == 0) break;
                    int want = t == 0 ? formId : t == 2 ? SpecialAbilities.AwakenFirst : -1;
                    bool first = sp.EvolutionTier == 0;
                    sp.EvolveWeapon(System.Array.IndexOf(opts, want) >= 0 ? want : opts[0]);
                    if (first) sp.OnEvolvedTier1();
                }
            }
            bul.PlayerHealth = bul.PlayerMaxHealth;
        }
        finally
        {
            RunSave.SetAutoFill(false);
            Random.state = saved;
        }
        setAbilitys();
        RefluxRefreshHud();
    }
}

// ===================================================================== 메인 메뉴 창
public static class TesterMenuUI
{
    static GameObject open;
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);
    static readonly Color Picked = new Color(1f, 0.9f, 0.55f);

    // MenuExtras.InstallMainMenu: 오른쪽 위 포인트 칸 아래
    public static void InstallButton(Transform parent, Transform root)
    {
        if (parent == null || parent.Find("TesterButton") != null) return;
        Button b = UIKit.MakeButton(parent, "테스터 메뉴", Vector2.zero, new Vector2(280f, 56f), () => Open(root), 24f);
        b.name = "TesterButton";
        RectTransform r = (RectTransform)b.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-30f, -100f);
    }

    public static void Open(Transform root)
    {
        if (open != null) return;
        RectTransform win = UIKit.Modal(root, "TesterMenu", new Vector2(1560f, 900f), out open);
        UIKit.EscCancel(open, Close);
        open.AddComponent<Window>().Build(win);
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }

    class Row
    {
        public Button[] buttons;
        public System.Func<int> get;    // 지금 고른 칸 번호
    }

    class Window : MonoBehaviour
    {
        readonly TesterRun.Config c = new TesterRun.Config();
        readonly List<Row> rows = new List<Row>();
        readonly Image[] portraits = new Image[16];
        Row heroRow, pathRow, formRow;
        TMP_Text pathInfo, summary;

        static readonly string[] DiffNames = { "쉬움", "보통", "어려움", "무한" };
        static readonly int[] LevelSteps = { 0, 10, 20, 39 };
        static readonly int[] FreeSteps = { 0, 3, 10 };
        static readonly string[] EvoNames = { "없음", "1차", "2차", "각성" };
        static readonly string[] TreeNames = { "없음", "추천", "전부 배움" };
        static readonly int[] BossIds = { TesterRun.BossNone, 2, 0, 1, 3, TesterRun.BossDeveloper, TesterRun.BossMom };     // 장 순서대로 (킹 슬라임 → 리치 왕 → 지옥의 군주 → 거울의 군주)

        public void Build(RectTransform win)
        {
            c.hero = CharacterData.Selected;
            c.difficulty = GameMode.Current;
            UIKit.Text(win, "테스터 메뉴", 44f, Gold, new Vector2(0f, 405f), new Vector2(700f, 60f));
            UIKit.MakeButton(win, "닫기", new Vector2(660f, 405f), new Vector2(150f, 56f), Close, 22f);

            // 영웅 14명 (7칸 × 2줄)
            int total = Mathf.Min(CharacterData.All.Length, portraits.Length);
            heroRow = new Row { buttons = new Button[total], get = () => (int)c.hero };
            for (int i = 0; i < total; i++)
            {
                int who = i;
                Vector2 pos = new Vector2(-700f + 100f * (i % 7), 325f - 96f * (i / 7));
                Button b = UIKit.MakeButton(win, "", pos, new Vector2(94f, 90f), () => PickHero((CharacterId)who), 16f);
                TMP_Text bl = b.GetComponentInChildren<TMP_Text>(true);
                if (bl != null) bl.gameObject.SetActive(false);
                RectTransform pr = UIKit.Rect("Portrait", b.transform, new Vector2(0f, 10f), new Vector2(58f, 58f));
                portraits[i] = pr.gameObject.AddComponent<Image>();
                portraits[i].preserveAspect = true;
                portraits[i].raycastTarget = false;
                portraits[i].sprite = CharacterUI.MenuPortrait((CharacterId)i);
                TMP_Text name = UIKit.Text(b.transform, CharacterData.Def((CharacterId)i).name, 14f, Parch, new Vector2(0f, -34f), new Vector2(90f, 20f));
                name.fontSizeMin = 9f;
                heroRow.buttons[i] = b;
            }
            rows.Add(heroRow);

            Button rec = UIKit.MakeButton(win, "추천 구성", new Vector2(420f, 325f), new Vector2(420f, 80f), () => { TesterRun.FillRecommended(c); UISound.Click(); Refresh(); }, 30f);
            rec.name = "TesterRecommend";
            UIKit.Text(win, "이 영웅에 맞는 갈래 · 레벨 40 · 무기 각성 · 추천 영혼 트리를 한 번에", 18f, Dim, new Vector2(420f, 262f), new Vector2(620f, 28f));

            Choice(win, "난이도", 165f, DiffNames, () => (int)c.difficulty, i =>
            {
                c.difficulty = (Difficulty)i;
                if (c.difficulty != Difficulty.Endless && (c.boss == TesterRun.BossDeveloper || c.boss == TesterRun.BossMom)) c.boss = TesterRun.BossNone;
            });
            pathRow = Choice(win, "갈래", 100f, new[] { "카드로 직접", "", "", "", "", "" }, () => c.path + 1, i => c.path = i - 1);
            pathInfo = UIKit.Text(win, "", 18f, Dim, new Vector2(60f, 50f), new Vector2(1300f, 40f));
            Choice(win, "스킬 (자동)", -5f, new[] { "Lv 1", "Lv 11", "Lv 21", "Lv 40" }, () => System.Array.IndexOf(LevelSteps, c.levels), i => c.levels = LevelSteps[i]);
            Choice(win, "직접 고를 카드", -75f, new[] { "없음", "+3장", "+10장" }, () => System.Array.IndexOf(FreeSteps, c.freePicks), i => c.freePicks = FreeSteps[i]);
            formRow = Choice(win, "1차 무기", -145f, new[] { "", "", "" }, () => c.form, i => c.form = i);
            Choice(win, "무기 진화", -215f, EvoNames, () => c.evoTier, i => c.evoTier = i);
            Choice(win, "영혼 트리", -285f, TreeNames, () => c.tree, i => c.tree = i);
            string[] bossNames = new string[BossIds.Length];
            for (int i = 0; i < BossIds.Length; i++)
                bossNames[i] = BossIds[i] == TesterRun.BossNone ? "보스 없이"
                             : bossbar.BossName(BossIds[i] == TesterRun.BossDeveloper ? bossbar.DeveloperKind : BossIds[i] == TesterRun.BossMom ? bossbar.DeveloperMomKind : BossIds[i]);
            Choice(win, "보스", -355f, bossNames, () => System.Array.IndexOf(BossIds, c.boss), i =>
            {
                c.boss = BossIds[i];
                if (c.boss == TesterRun.BossDeveloper || c.boss == TesterRun.BossMom) c.difficulty = Difficulty.Endless;
            });

            summary = UIKit.Text(win, "", 18f, Dim, new Vector2(-260f, -415f), new Vector2(980f, 40f));
            UIKit.MakeButton(win, "바로 시작", new Vector2(560f, -415f), new Vector2(360f, 64f), () => { Close(); TesterRun.Begin(c); }, 30f);
            Refresh();
        }

        Row Choice(Transform win, string label, float y, string[] names, System.Func<int> get, System.Action<int> set)
        {
            UIKit.Text(win, label, 22f, Gold, new Vector2(-660f, y), new Vector2(200f, 50f), TextAlignmentOptions.Right);
            Row row = new Row { buttons = new Button[names.Length], get = get };
            float w = names.Length > 6 ? 168f : 190f;
            for (int i = 0; i < names.Length; i++)
            {
                int pick = i;
                row.buttons[i] = UIKit.MakeButton(win, names[i], new Vector2(-470f + (w + 8f) * i + w * 0.5f, y), new Vector2(w, 56f), () => { set(pick); UISound.Click(); Refresh(); }, 20f);
                TMP_Text t = row.buttons[i].GetComponentInChildren<TMP_Text>(true);
                if (t != null) { t.enableAutoSizing = true; t.fontSizeMin = 10f; }
            }
            rows.Add(row);
            return row;
        }

        void PickHero(CharacterId who)
        {
            if (c.hero != who) { c.hero = who; c.path = -1; c.form = 0; }      // 갈래 · 무기는 영웅마다 다름
            UISound.Click();
            Refresh();
        }

        static void SetLabel(Button b, string text)
        {
            TMP_Text l = b.GetComponentInChildren<TMP_Text>(true);
            if (l != null) l.text = text;
        }

        void Refresh()
        {
            CharacterId who = c.hero;
            for (int p = 0; p < Paths.Count; p++)
            {
                PathDef d = Paths.DefOf(who, p);
                SetLabel(pathRow.buttons[p + 1], d != null ? Loc.T(d.name) : "-");
                pathRow.buttons[p + 1].interactable = d != null;
            }
            if (c.path >= 0 && Paths.DefOf(who, c.path) == null) c.path = -1;
            List<string> forms = RefluxPrepUI.FormNames(who);
            if (c.form >= forms.Count) c.form = 0;
            for (int f = 0; f < formRow.buttons.Length; f++)
            {
                SetLabel(formRow.buttons[f], f < forms.Count ? Loc.T(forms[f]) : "-");
                formRow.buttons[f].interactable = f < forms.Count;
            }
            PathDef pd = c.path >= 0 ? Paths.DefOf(who, c.path) : null;
            pathInfo.text = pd != null ? Loc.T(pd.tagline) + "  <color=#A89C86>(" + Loc.T(pd.strong) + " / " + Loc.T(pd.weak) + ")</color>"
                                       : Loc.T("갈래를 고르지 않으면 평소처럼 입문 카드 창에서 고르고, 스킬 자동 레벨은 쓰지 않습니다");
            foreach (Row r in rows)
            {
                int on = r.get();
                for (int i = 0; i < r.buttons.Length; i++)
                {
                    Image img = r.buttons[i].GetComponent<Image>();
                    if (img != null) img.color = i == on ? Picked : Color.white;
                }
            }
            bool final = c.boss == TesterRun.BossDeveloper || c.boss == TesterRun.BossMom;
            summary.text = c.boss == TesterRun.BossNone ? Loc.T("1장부터 일반 판으로 시작합니다")
                         : final ? Loc.T("무한 모드에서 시작해 곧바로 강림합니다")
                         : c.difficulty == Difficulty.Endless ? Loc.T("무한 모드 사막에서 곧바로 이 보스가 나옵니다")
                         : Loc.T("이 보스의 장에서 시작해 곧바로 보스가 나옵니다");
        }
    }
}
#endif
