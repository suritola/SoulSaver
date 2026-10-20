using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.2~ 망자의 전령 (설계 3-2 ①): 쓰러진 뒤 결과 화면 전에 한 줄 (PlayerController.DeathSequence 의 1.1초 연출 뒤 · LoadScene 앞)
//   시간은 멈춘 채(timeScale 0) 화면 아래 검은 띠(보스 등장 띠와 같은 210) + 왼쪽 「망자의 전령」 이름표 + 타자 효과 한 줄
//   2.5 ~ 4초 (영웅 줄은 전령 → 영웅의 답 두 줄이라 5초 안) · [상호작용] · 클릭 · 패드 A · ESC · 패드 B 어느 것이든 바로 넘김 (넘기면 바로 결과 화면)
//   설정 → 게임 「망자의 전령」 끄면 나오지 않음 · 「포기하고 결과 보기」(무한 · 일일)는 짧은 한 줄
//   자동 플레이 · 촬영 · 배치 · 튜토리얼: 없음 · 체험판: 「체」 표시 줄만
//   고르는 규칙: 조건이 많이 · 구체적으로 맞는 줄(영웅+원인+보스 > 원인+보스 > 원인 > 그 밖 > 장 > 공용) 중 최근 20줄에 없던 것
//     같은 단계가 여럿이면 무작위, 가끔(4판 중 1판쯤) 영웅 줄 · 공용 줄로 섞어 「또 뜨네」가 덜하게
//   줄 표 Herald.Lines.cs (조건) · 글 LocTable.Herald (4개 언어) — 둘 다 docs/review/2026-10-16-한판더-글.md 1절에서 뽑음
public static partial class Herald
{
    public static int LowHpBossAt = -1;         // 체력 10% 아래로 장 보스를 잡은 장 번호 (RunCountersHost) — 다음 장에서 쓰러지면 「lowhp_boss」
    public static bool ShownThisRun { get; private set; }
    public static bool Playing { get; private set; }            // ESC 메뉴가 같은 ESC 로 열리지 않게
    public static bool Enabled => GameSettings.Herald && !RunCounters.Off && !GameInput.Auto && !StoryDirector.ShouldSkipAll;
    const string RecentKey = "herald.recent";
    const int RecentKeep = 20;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name != "GameScene") return;
            ShownThisRun = false;
            LowHpBossAt = -1;
        };
    }

    // ================================================================= 판의 사실 (쓰러진 순간)
    sealed class Facts
    {
        public string hero, ch, stage, cause, boss;
        public string causeHead, skill;     // 「개발자 · 라이브 코딩」 → 「개발자」 · 「라이브 코딩」 (조건이 「 · 」로 나뉘어 원인 전체를 조건에 쓸 수 없음)
        public int chNum;
        public readonly HashSet<string> extra = new HashSet<string>();
    }

    static readonly string[] BossNames = { "lich", "demon", "slime", "mirror" };

    static Facts Gather(bool gaveUp)
    {
        Facts f = new Facts();
        f.hero = CharacterData.Selected.ToString();
        bool endless = GameMode.IsEndless;
        f.chNum = RunStats.LastHurtStage > 0 ? RunStats.LastHurtStage : RunStats.Stage;
        f.ch = endless ? "E" : f.chNum.ToString();
        int stageIdx = Mathf.Clamp(Chapters.CurrentStage, 0, Chapters.StageNames.Length - 1);
        f.stage = Chapters.StageNames[stageIdx];
        f.cause = RunStats.LastHurtBy ?? "";
        int dot = f.cause.IndexOf(" · ");
        f.causeHead = dot > 0 ? f.cause.Substring(0, dot) : f.cause;
        f.skill = dot > 0 ? f.cause.Substring(dot + 3) : "";
        // 보스전 중: 살아 있는 장 보스 (개발자 · 엄마는 원인 이름으로 따로)
        foreach (bosss b in bosss.Live)
        {
            if (b == null || b.IsDead || b.bossKind < 0 || b.bossKind >= BossNames.Length) continue;
            f.boss = BossNames[b.bossKind];
            if (b.Enraged) { f.extra.Add("half"); f.extra.Add("lastphase"); }
            break;
        }
        if (gaveUp) f.extra.Add("giveup");
        // 횟수 (영웅 · 장 · 연속 · 클리어 뒤)
        string hk = "death." + (int)CharacterData.Selected;
        if (Prefs.GetInt(hk, 0) == 0 && !gaveUp) f.extra.Add("first");
        if (!gaveUp && Prefs.GetInt("death.afterclear", 0) == 1) f.extra.Add("afterclear");
        if (!endless && !gaveUp && (Prefs.GetInt("death.ch." + f.chNum, 0) + 1) % 5 == 0) f.extra.Add("ch5");
        if (!gaveUp && StreakAfterThis() >= 3) f.extra.Add("streak3");
        // 판 안
        if (Curse.Active) f.extra.Add("cursed");
        if (LastStand.Used > 0) f.extra.Add("laststand");
        if (LowHpBossAt > 0 && f.chNum == LowHpBossAt + 1) f.extra.Add("lowhp_boss");
        if (MidBossNear()) f.extra.Add("midboss");
        string ev = ChapterEvents.ActiveId;
        if (!string.IsNullOrEmpty(ev)) { f.extra.Add("event"); f.extra.Add("event:" + ev); if (ev == ContentRegistry.Altar) f.extra.Add("altar"); }
        LevelShop shop = Object.FindFirstObjectByType<LevelShop>(FindObjectsInactive.Include);
        if (shop != null && shop.PendingBadge != null && shop.PendingBadge.activeInHierarchy) f.extra.Add("levelup");
        PlayerController p = Hostile.Player;
        if (p != null && p.skillGauge != null && p.skillGauge.IsFull()) f.extra.Add("ultfull");
        if (RunStats.Level >= LevelShop.BaseLevelCap) f.extra.Add("lv50");
        if (SoulShards.Amount >= 40) f.extra.Add("shards unspent");
        foreach (SoulBrandDef b in SoulBrand.Current) f.extra.Add("brand:" + b.id);
        if (endless)
        {
            float t = GameMode.EndlessSeconds;
            if (t < 300f) f.extra.Add("t<5");
            if (t < 600f) f.extra.Add("t<10");
            if (t < 900f) f.extra.Add("t<15"); else f.extra.Add("t>=15");
            if (EndlessMode.Instance != null && EndlessMode.Instance.Beyond) f.extra.Add("beyond");
        }
        return f;
    }

    static bool MidBossNear()
    {
        PlayerController p = Hostile.Player;
        if (p == null) return false;
        foreach (MidBossMark m in Object.FindObjectsByType<MidBossMark>(FindObjectsSortMode.None))
            if (m != null && m.isActiveAndEnabled && m.GetComponent<GoldenGoblin>() == null && (m.transform.position - p.transform.position).sqrMagnitude < 10f * 10f) return true;
        return false;
    }

    // 같은 영웅 · 난이도로 이번까지 몇 번 연달아 쓰러졌는지 (이번 죽음을 더한 값 · 결과 화면 추천 줄도 씀)
    static string StreakId => (int)CharacterData.Selected + "|" + (int)GameMode.Current;
    static int StreakAfterThis()
    {
        string[] s = Prefs.GetString("death.streak", "").Split('|');
        if (s.Length >= 3 && s[0] + "|" + s[1] == StreakId && int.TryParse(s[2], out int n)) return n + 1;
        return 1;
    }

    // 결과 화면 추천 (같은 영웅 · 난이도 3연속) · 같은 원인 3연속
    public static int Streak { get; private set; }
    public static bool SameCause3 { get; private set; }

    // 쓰러짐을 적음 (전령을 껐어도 · 횟수 · 연속 · 기억 조각 조건에 씀) — DeathSequence 가 한 번
    public static void RecordDeath()
    {
        if (RunCounters.Off) return;
        string hk = "death." + (int)CharacterData.Selected;
        Prefs.SetInt(hk, Prefs.GetInt(hk, 0) + 1);
        Prefs.SetInt("death.afterclear", 0);
        if (!GameMode.IsEndless)
        {
            int ch = RunStats.LastHurtStage > 0 ? RunStats.LastHurtStage : RunStats.Stage;
            Prefs.SetInt("death.ch." + ch, Prefs.GetInt("death.ch." + ch, 0) + 1);
        }
        int n = DailyChallenge.Active || TutorialRun.Active ? 0 : StreakAfterThis();
        string[] s = Prefs.GetString("death.streak", "").Split('|');
        string cause = RunStats.LastHurtBy ?? "";
        int sameCause = s.Length >= 5 && n > 1 && s[3] == cause && int.TryParse(s[4], out int c) ? c + 1 : 1;
        Prefs.SetString("death.streak", StreakId + "|" + n + "|" + cause + "|" + sameCause);
        Streak = n;
        SameCause3 = n >= 3 && sameCause >= 3;
        Prefs.Save();
        MemoryShards.Check(true);
    }

    // 클리어: 연속 사망을 끊고 「클리어 뒤 첫 사망」을 켬 (StageManager.Cleared)
    public static void RecordClear()
    {
        Prefs.SetString("death.streak", "");
        Prefs.SetInt("death.afterclear", 1);
        Streak = 0;
        SameCause3 = false;
    }

    // ================================================================= 줄 고르기
    static bool Matches(string cond, Facts f, out int score)
    {
        score = 0;
        if (string.IsNullOrEmpty(cond) || cond.Trim() == "*") return true;
        foreach (string raw in cond.Split('·'))
        {
            string t = raw.Trim();
            if (t.Length == 0 || t == "*") continue;
            if (t.StartsWith("ch>="))
            {
                if (f.ch == "E" || !int.TryParse(t.Substring(4), out int need) || f.chNum < need) return false;
                score += 2;
                continue;
            }
            int eq = t.IndexOf('=');
            if (eq < 0) return false;
            string k = t.Substring(0, eq).Trim(), v = t.Substring(eq + 1).Trim();
            switch (k)
            {
                case "hero": if (v != "*" && v != f.hero) return false; score += 3; break;
                case "ch": if (v != "*" && v != f.ch) return false; score += v == "*" ? 0 : 2; break;
                case "stage": if (v != f.stage) return false; score += 2; break;
                // 원인이 정확히 같으면 8점 · 「 · 」 앞(보스 이름)만 같으면 6점 (개발자 · 엄마 줄이 고유 공격 사망에도 나옴)
                case "cause": if (v != "*" && v != f.cause && v != f.causeHead) return false; score += v == "*" ? 1 : v == f.cause ? 8 : 6; break;
                case "skill": if (string.IsNullOrEmpty(f.skill) || v != f.skill) return false; score += 8; break;
                case "boss": if (f.boss == null || (v != "*" && v != f.boss)) return false; score += v == "*" ? 2 : 4; break;
                case "extra": if (!f.extra.Contains(v)) return false; score += 6; break;
                default: return false;
            }
        }
        return true;
    }

    static List<string> Recent() => new List<string>(Prefs.GetString(RecentKey, "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));

    static string Pick(Facts f)
    {
        List<string> recent = Recent();
        bool giveup = f.extra.Contains("giveup");
        List<(string key, int score, bool hero)> all = new List<(string, int, bool)>();
        foreach ((string key, string cond, bool demo) in Table)
        {
            if (Demo.On && !demo) continue;
            if (!Matches(cond, f, out int score)) continue;
            bool isGiveup = cond.Contains("giveup");
            if (giveup != isGiveup) continue;                   // 포기면 포기 줄만 · 쓰러짐이면 포기 줄은 빼기
            if (!Loc.Has(key)) continue;
            all.Add((key, score, key.StartsWith("herald.hero.")));
        }
        if (all.Count == 0) return null;
        List<(string key, int score, bool hero)> fresh = all.FindAll(c => !recent.Contains(c.key));
        if (fresh.Count == 0) fresh = all;
        // 4판 중 1판쯤은 영웅 줄 (있으면) · 그다음 가장 높은 단계
        List<(string key, int score, bool hero)> pool;
        List<(string key, int score, bool hero)> heroLines = fresh.FindAll(c => c.hero);
        if (heroLines.Count > 0 && Random.value < 0.25f) pool = heroLines;
        else
        {
            int best = int.MinValue;
            foreach (var c in fresh) best = Mathf.Max(best, c.score);
            // 같은 원인이 늘 같은 줄로 고정되지 않게 가장 높은 단계 하나 아래까지 30% 로 섞음
            int floor = Random.value < 0.3f ? best - 6 : best;
            pool = fresh.FindAll(c => c.score >= floor && !c.hero);
            if (pool.Count == 0) pool = fresh;
        }
        string pick = pool[Random.Range(0, pool.Count)].key;
        recent.Remove(pick);
        recent.Add(pick);
        while (recent.Count > RecentKeep) recent.RemoveAt(0);
        Prefs.SetString(RecentKey, string.Join(",", recent));
        return pick;
    }

    // ================================================================= 보여 주기
    // 쓰러짐 (gaveUp = 포기하고 결과 보기) — 다 끝나면 (넘겨도) 돌아옴, 결과 화면은 부른 쪽이 엶
    public static IEnumerator Play(bool gaveUp)
    {
        if (!Enabled || ShownThisRun) yield break;
        string key = Pick(Gather(gaveUp));
        if (key == null) yield break;
        ShownThisRun = true;
        RunLog.HeraldShown = true;          // 1.2~ 판 기록 herald_shown (RunLog)
        Playing = true;
        float before = Time.timeScale;
        Time.timeScale = 0f;
        Band band = Band.Create();
        try { yield return band.Run(Loc.T(key), key.StartsWith("herald.hero.")); }
        finally
        {
            Playing = false;
            if (band != null) Object.Destroy(band.gameObject);
            Time.timeScale = before;
        }
    }

    // 판 안의 다른 부품 없이 띄운 뒤 then (ESC 메뉴 「포기하고 결과 보기」)
    public static void RunThen(bool gaveUp, System.Action then)
    {
        GameObject go = new GameObject("HeraldRunner");
        go.AddComponent<Runner>().StartCoroutine(Chain(gaveUp, then));
    }

    static IEnumerator Chain(bool gaveUp, System.Action then)
    {
        yield return Play(gaveUp);
        then?.Invoke();
    }

    class Runner : MonoBehaviour { }

    class Band : MonoBehaviour
    {
        Canvas canvas;
        RectTransform bar;
        TMP_Text nameTag, line, reply, hint;
        CanvasGroup group;
        bool skip;
        float openedAt;

        public static Band Create()
        {
            GameObject go = new GameObject("HeraldBand", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Band b = go.AddComponent<Band>();
            b.canvas = go.GetComponent<Canvas>();
            b.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            b.canvas.sortingOrder = 31500;
            CanvasScaler cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            UiScaler.Fit(cs);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            UIKit.EnsureStyle();
            b.group = go.AddComponent<CanvasGroup>();
            b.group.alpha = 0f;

            // 화면 전체를 살짝 어둡게 (뒤의 붉은 화면은 그대로 보이게) + 아래 띠
            RectTransform shade = UIKit.Rect("Shade", go.transform, Vector2.zero, Vector2.zero);
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero;
            Image si = shade.gameObject.AddComponent<Image>();
            si.color = new Color(0.02f, 0.01f, 0.03f, 0.35f);
            b.bar = UIKit.Rect("Band", go.transform, Vector2.zero, new Vector2(0f, 210f));
            b.bar.anchorMin = new Vector2(0f, 0f); b.bar.anchorMax = new Vector2(1f, 0f); b.bar.pivot = new Vector2(0.5f, 0f);
            b.bar.anchoredPosition = Vector2.zero;
            b.bar.sizeDelta = new Vector2(0f, 210f);
            Image bi = b.bar.gameObject.AddComponent<Image>();
            bi.color = new Color(0f, 0f, 0f, 0.92f);

            // 왼쪽: 이름표 · 쓰러진 영웅 그림 (어둡게)
            Sprite face = CharacterUI.Portrait(CharacterData.Selected);
            if (face != null)
            {
                RectTransform fr = UIKit.Rect("Hero", b.bar, new Vector2(0f, 105f), new Vector2(150f, 150f));
                fr.anchorMin = fr.anchorMax = new Vector2(0f, 0f);
                fr.anchoredPosition = new Vector2(120f, 105f);
                Image fi = fr.gameObject.AddComponent<Image>();
                fi.sprite = face;
                fi.preserveAspect = true;
                fi.color = new Color(0.32f, 0.3f, 0.38f, 0.9f);
                fi.raycastTarget = false;
            }
            b.nameTag = UIKit.Text(b.bar, "herald.name", 26f, new Color(0.62f, 0.78f, 1f), Vector2.zero, new Vector2(420f, 36f), TextAlignmentOptions.Left);
            Anchor(b.nameTag.rectTransform, new Vector2(230f, 172f));
            b.line = UIKit.Text(b.bar, "", 30f, new Color(0.93f, 0.91f, 0.86f), Vector2.zero, new Vector2(1320f, 92f), TextAlignmentOptions.TopLeft);
            Anchor(b.line.rectTransform, new Vector2(230f, 104f));
            b.reply = UIKit.Text(b.bar, "", 27f, Color.Lerp(AttackLook.HeroColor(), Color.white, 0.35f), Vector2.zero, new Vector2(1320f, 44f), TextAlignmentOptions.TopLeft);
            Anchor(b.reply.rectTransform, new Vector2(230f, 40f));
            foreach (TMP_Text t in new[] { b.line, b.reply }) { t.enableAutoSizing = true; t.fontSizeMin = 18f; t.fontSizeMax = t.fontSize; t.enableWordWrapping = true; UIKit.Forget(t); }
            b.hint = UIKit.Text(go.transform, "", 20f, new Color(0.7f, 0.66f, 0.6f), Vector2.zero, new Vector2(520f, 30f), TextAlignmentOptions.Right);
            RectTransform hr = b.hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(1f, 0f);
            hr.pivot = new Vector2(1f, 0.5f);
            hr.anchoredPosition = new Vector2(-30f, 24f);
            b.hint.text = Loc.T(GamePad.Active ? "herald.skip.pad" : "herald.skip");
            UIKit.Forget(b.hint);
            b.openedAt = Time.unscaledTime;
            return b;
        }

        static void Anchor(RectTransform r, Vector2 at)
        {
            r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = at;
        }

        void Update()
        {
            // 쓰러질 때 누르던 입력이 바로 넘기지 않게 0.25초 뒤부터
            if (Time.unscaledTime - openedAt < 0.25f) return;
            if (KeyBindings.Down(GameAction.Interact) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                || GameInput.EscDown || GamePad.ADown || GamePad.CancelDown) skip = true;
        }

        public IEnumerator Run(string text, bool twoLines)
        {
            string first = text, second = null;
            if (twoLines)
            {
                int nl = text.IndexOf('\n');
                if (nl > 0) { first = text.Substring(0, nl); second = text.Substring(nl + 1); }
            }
            for (float t = 0f; t < 0.25f && !skip; t += Time.unscaledDeltaTime) { group.alpha = t / 0.25f; yield return null; }
            group.alpha = 1f;
            // 글자 수에 맞춘 시간: 한 줄 2.5 ~ 4초, 두 줄 5초 안 (타자 · 머무름)
            float budget = second != null ? 4.6f : Mathf.Clamp(1.6f + first.Length * 0.02f, 2.5f, 3.8f);
            float typeShare = second != null ? 0.55f : 0.5f;
            int total = first.Length + (second?.Length ?? 0);
            float perChar = Mathf.Min(0.035f, budget * typeShare / Mathf.Max(1, total));
            yield return Type(line, first, perChar);
            if (second != null && !skip) yield return Type(reply, second, perChar);
            float hold = Mathf.Max(0.8f, budget - total * perChar - 0.25f);
            for (float t = 0f; t < hold && !skip; t += Time.unscaledDeltaTime) yield return null;
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / 0.2f; yield return null; }
        }

        IEnumerator Type(TMP_Text t, string s, float perChar)
        {
            t.text = s;
            t.maxVisibleCharacters = 0;
            for (int n = 0; n <= s.Length && !skip; n++)
            {
                t.maxVisibleCharacters = n;
                if (n < s.Length && s[n] != ' ' && n % 3 == 0) Hostile.Play("pew", 0.08f, 1.6f);
                float until = Time.unscaledTime + perChar;
                while (Time.unscaledTime < until && !skip) yield return null;
            }
            t.maxVisibleCharacters = 99999;
        }
    }
}
