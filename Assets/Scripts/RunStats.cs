using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 이번 판 기록: 게임 씬에서 계속 적어 두고, 게임 오버 화면(결과)과 ESC 메뉴(요약)에서 보여 줌
public class RunStats : MonoBehaviour
{
    public static float Seconds;            // 멈춘 시간은 뺀 플레이 시간
    public static int Kills, Level = 1;
    public static string Character = "", Difficulty = "";
    public static readonly List<string> Abilities = new List<string>();     // 특수 능력 (진화하면 +)
    public static readonly List<string> Cards = new List<string>();         // 레벨업 카드 "이름 Lv n"
    // 무기 진화 · 영혼 트리 (1.7.9~): 결과 화면과 밸런스 기록에 씀
    public static readonly List<KeyValuePair<string, float>> Evolutions = new List<KeyValuePair<string, float>>();
    public static int[] Branches = new int[6];
    public static int ShardsTotal, ShardsSpent;
    // 준 피해 · 받은 피해 (출처별 합), 마지막으로 맞은 공격과 그때의 장 (2.1.1~)
    public static readonly Dictionary<string, float> DamageDealt = new Dictionary<string, float>();
    public static readonly Dictionary<string, float> DamageTaken = new Dictionary<string, float>();
    public static string LastHurtBy = "";
    public static int LastHurtStage, Stage;
    static bool runActive, cleared;

    float refresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        StageManager.Cleared += d => cleared = true;
        SceneManager.sceneLoaded += (s, m) =>
        {
            // 게임 씬을 떠날 때 (게임 오버 · 메인 메뉴 · 다시 시작) 이번 판을 밸런스 기록에 한 줄
            if (runActive) { runActive = false; WriteBalanceLog(cleared ? "clear" : s.name == "GameOver" ? "death" : "quit"); }
            if (s.name != "GameScene") return;
            runActive = true;
            cleared = false;
            Evolutions.Clear();
            Branches = new int[6];
            ShardsTotal = ShardsSpent = 0;
            Seconds = 0f;
            Kills = 0;
            Level = 1;
            Abilities.Clear();
            Cards.Clear();
            DamageDealt.Clear();
            DamageTaken.Clear();
            LastHurtBy = "";
            LastHurtStage = Stage = 0;
            DamageSource.Current = null;
            new GameObject("RunStats").AddComponent<RunStats>();
        };
    }

    // 적 · 보스가 실제로 잃은 체력 (넘친 피해는 빼고): 지금 공격 중인 출처에
    public static void Dealt(float amount)
    {
        if (!runActive || amount <= 0f) return;
        string key = DamageSource.Current ?? DamageSource.Other;
        DamageDealt.TryGetValue(key, out float v);
        DamageDealt[key] = v + amount;
    }

    // 플레이어가 받은 피해: 때린 코드가 있는 파일로 출처를 가림 (PlayerController.TryHit 의 CallerFilePath)
    public static void Hurt(float amount, string callerFile)
    {
        if (!runActive || amount <= 0f) return;
        string file = System.IO.Path.GetFileNameWithoutExtension(callerFile ?? "");
        // 1.0.1: 보스 · 적 스킬은 스킬 이름까지 (AttackLabel) — 예전엔 공용 판정을 거치면 보스 공격도 모두 「적 스킬」
        string named = file == "EnemySkill" || file == "BossSkills" || file == "BossUltimate"
            ? (AttackLabel.Pending ?? AttackLabel.FromStack()) : null;
        string key = named ?? file switch
        {
            "EnermyController" => "적과 부딪힘",
            "EnemySkill" => "적 스킬",
            "BossSkills" => "보스 스킬",
            "BossUltimate" => "보스 결계",
            "PlayerController" => "보스와 부딪힘",
            _ => DamageSource.Other,
        };
        DamageTaken.TryGetValue(key, out float v);
        DamageTaken[key] = v + amount;
        LastHurtBy = key;
        LastHurtStage = StageManager.Instance != null ? Chapters.Number(StageManager.Instance.CurrentStage) : 0;
    }

    // 많은 순서로 (이름, 양), 남는 것은 "기타"로 묶음
    public static List<KeyValuePair<string, float>> Top(Dictionary<string, float> from, int count)
    {
        List<KeyValuePair<string, float>> all = new List<KeyValuePair<string, float>>();
        float rest = 0f;
        foreach (KeyValuePair<string, float> kv in from)
            if (kv.Key == DamageSource.Other) rest += kv.Value;
            else all.Add(kv);
        all.Sort((a, b) => b.Value.CompareTo(a.Value));
        int keep = rest > 0f || all.Count > count ? count - 1 : count;
        for (int i = keep; i < all.Count; i++) rest += all[i].Value;
        if (all.Count > keep) all.RemoveRange(keep, all.Count - keep);
        if (rest > 0f) all.Add(new KeyValuePair<string, float>(DamageSource.Other, rest));
        return all;
    }

    static float Sum(Dictionary<string, float> d)
    {
        float s = 0f;
        foreach (float v in d.Values) s += v;
        return s;
    }

    // 피해 출처 이름 (특수 능력은 능력 이름 그대로 번역표에 있음)
    public static string SourceName(string key) => AttackLabel.Translate(key);

    void OnEnable() => EnermyController.Killed += OnKill;
    void OnDisable() => EnermyController.Killed -= OnKill;
    static void OnKill(Vector3 pos) => Kills++;

    void Update()
    {
        if (Time.timeScale > 0f) Seconds += Time.unscaledDeltaTime * Mathf.Min(1f, Time.timeScale);
        refresh -= Time.unscaledDeltaTime;
        if (refresh > 0f) return;
        refresh = 0.5f;
        Capture();
    }

    static SpecialAbilities sp;
    static LevelShop shop;

    public static void Capture()
    {
        CharacterDef d = CharacterData.Current;
        Character = Loc.T(d.name);
        Difficulty = GameMode.Name(GameMode.Current);
        PlayerController p = Hostile.Player;
        if (p != null) Level = p.level;
        if (StageManager.Instance != null) Stage = Chapters.Number(StageManager.Instance.CurrentStage);

        // 0.5초마다 씬을 뒤지지 않게 한 번 찾아 두고 씀
        if (sp == null) sp = FindFirstObjectByType<SpecialAbilities>();
        if (sp != null)
        {
            Evolutions.Clear();
            Evolutions.AddRange(sp.EvolutionLog);
            Branches = sp.OwnedPerBranch();
            ShardsTotal = SoulShards.Total;
            ShardsSpent = SoulShards.Spent;
        }
        if (sp != null && sp.abilities != null)
        {
            Abilities.Clear();
            foreach (int id in sp.EquippedIds)
                if (id >= 0 && id < sp.abilities.Length && sp.abilities[id] != null)
                    Abilities.Add(Loc.T(sp.abilities[id].name) + (sp.IsEvolved(id) ? "+" : ""));
        }
        if (shop == null) shop = FindFirstObjectByType<LevelShop>(FindObjectsInactive.Include);
        if (shop != null && shop.ability_level != null && shop.ability_name != null)
        {
            Cards.Clear();
            for (int i = 0; i < shop.ability_level.Length && i < shop.ability_name.Length; i++)
                if (shop.ability_level[i] > 0 && !string.IsNullOrEmpty(shop.ability_name[i]))
                    Cards.Add(shop.ability_name[i] + " Lv" + shop.ability_level[i]);
        }
    }

    // 여러 줄 요약 (ESC 메뉴 · 결과 화면이 같이 씀)
    public static string Summary(bool withStats)
    {
        Capture();
        string gold = "<color=#F5D478>", end = "</color>";
        string s = gold + Character + "  ·  " + Difficulty + end + "\n"
                 + Loc.T("플레이 시간 ") + EndlessMode.Clock(Seconds) + "     " + Loc.T("레벨 ") + Level + "     " + Loc.T("처치 ") + Kills;
        if (withStats)
        {
            PlayerController p = Hostile.Player;
            if (p != null)
                s += "\n\n" + gold + Loc.T("능력치") + end + "\n"
                   + Loc.T("공격력 ") + p.damage.ToString("0.##") + "   " + Loc.T("받는 피해 감소 ") + (Mathf.Min(p.def, PlayerController.MaxDef) * 100f).ToString("0") + "%"
                   + "   " + Loc.T("최대 체력 ") + Mathf.RoundToInt(p.PlayerMaxHealth);
        }
        // 무기 진화: 기본 무기 → 1차 (시각) → 2차 (시각)
        string evo = Loc.T("기본 무기");
        foreach (KeyValuePair<string, float> e in Evolutions) evo += "  \u2192  " + e.Key + " <color=#A89C86>(" + EndlessMode.Clock(e.Value) + ")</color>";
        s += "\n\n" + gold + Loc.T("무기 진화") + end + "\n" + evo;
        // 영혼 트리: 많이 배운 가지 순서 · 조각
        List<int> order = new List<int> { 0, 1, 2, 3, 4, 5 };
        order.Sort((a, b) => Branches[b].CompareTo(Branches[a]));
        string tree = "";
        foreach (int b in order)
            if (Branches[b] > 0) tree += (tree.Length > 0 ? ",  " : "") + Loc.T(SpecialAbilities.BranchNames[b]) + " " + Branches[b];
        s += "\n\n" + gold + Loc.T("영혼 트리") + end + "\n" + (tree.Length > 0 ? tree : "-")
           + "\n" + Loc.T("영혼 조각") + " " + ShardsTotal + "  (" + Loc.T("씀") + " " + ShardsSpent + ")";
        s += "\n\n" + gold + Loc.T("특수 능력") + end + "\n" + (Abilities.Count > 0 ? string.Join(",  ", Abilities) : "-");
        s += "\n\n" + gold + Loc.T("레벨업 카드") + end + "\n" + (Cards.Count > 0 ? string.Join(",  ", Cards) : "-");
        // ESC 메뉴: 피해 비율 상위 4 (결과 화면은 막대로 따로 보여 줌)
        if (withStats && DamageDealt.Count > 0)
        {
            float total = Mathf.Max(1f, Sum(DamageDealt));
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, float> kv in Top(DamageDealt, 4)) parts.Add(SourceName(kv.Key) + " " + (kv.Value / total * 100f).ToString("0") + "%");
            s += "\n\n" + gold + Loc.T("준 피해") + end + "\n" + string.Join(",  ", parts);
        }
        return s;
    }

    // ================================================================= 결과 화면 오른쪽: 준 피해 · 받은 피해 막대
    public static void BuildDamagePanel(Transform canvas)
    {
        if (DamageDealt.Count == 0 && DamageTaken.Count == 0) return;
        UIKit.EnsureStyle();
        RectTransform box = UIKit.Rect("DamagePanel", canvas, Vector2.zero, new Vector2(480f, 520f));
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(1f, 0.5f);
        box.anchoredPosition = new Vector2(-60f, 0f);
        float y = 0f;
        y = Section(box, y, Loc.T("준 피해"), Top(DamageDealt, 6), new Color(0.96f, 0.72f, 0.3f));
        y = Section(box, y - 20f, Loc.T("받은 피해"), Top(DamageTaken, 4), new Color(0.92f, 0.32f, 0.3f));
        // 쓰러졌으면 마지막으로 맞은 공격과 장
        if (!cleared && !string.IsNullOrEmpty(LastHurtBy) && SceneManager.GetActiveScene().name == "GameOver")
        {
            string where = GameMode.IsEndless ? Loc.T("무한 모드") + "  ·  " : LastHurtStage > 0 ? Loc.T("{0}장").Replace("{0}", LastHurtStage.ToString()) + "  ·  " : "";
            TMP_Text t = Line(box, y - 14f, "<color=#F5D478>" + Loc.T("쓰러진 곳") + "</color>  " + where + SourceName(LastHurtBy), 22f);
            t.color = new Color(0.93f, 0.9f, 0.84f);
        }
    }

    static float Section(RectTransform box, float y, string title, List<KeyValuePair<string, float>> rows, Color bar)
    {
        if (rows.Count == 0) return y;
        float total = 0f;
        foreach (KeyValuePair<string, float> kv in rows) total += kv.Value;
        float best = Mathf.Max(1f, rows[0].Value);
        foreach (KeyValuePair<string, float> kv in rows) best = Mathf.Max(best, kv.Value);
        Line(box, y, "<color=#F5D478><size=110%>" + title + "</size></color>   <color=#A89C86>" + Mathf.RoundToInt(total).ToString("N0") + "</color>", 24f);
        y -= 40f;
        foreach (KeyValuePair<string, float> kv in rows)
        {
            // 막대 (가장 많은 것을 꽉 채운 길이로) · 이름 · 비율
            RectTransform back = UIKit.Rect("Bar", box, Vector2.zero, new Vector2(480f, 30f));
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0f, 1f);
            back.anchoredPosition = new Vector2(0f, y);
            Image bg = back.gameObject.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.06f);
            bg.raycastTarget = false;
            RectTransform fill = UIKit.Rect("Fill", back, Vector2.zero, new Vector2(480f * kv.Value / best, 30f));
            fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            Image fi = fill.gameObject.AddComponent<Image>();
            fi.color = new Color(bar.r, bar.g, bar.b, 0.45f);
            fi.raycastTarget = false;
            TMP_Text name = UIKit.Text(back, "", 20f, new Color(0.95f, 0.92f, 0.86f), new Vector2(10f, 0f), new Vector2(340f, 30f), TextAlignmentOptions.Left);
            name.rectTransform.anchorMin = name.rectTransform.anchorMax = name.rectTransform.pivot = new Vector2(0f, 0.5f);
            name.text = SourceName(kv.Key);
            name.enableAutoSizing = true;
            name.fontSizeMin = 12f;
            name.fontSizeMax = 20f;
            TMP_Text pct = UIKit.Text(back, "", 20f, new Color(0.95f, 0.92f, 0.86f), new Vector2(-10f, 0f), new Vector2(110f, 30f), TextAlignmentOptions.Right);
            pct.text = (kv.Value / Mathf.Max(1f, total) * 100f).ToString("0") + "%";
            pct.rectTransform.anchorMin = pct.rectTransform.anchorMax = pct.rectTransform.pivot = new Vector2(1f, 0.5f);
            y -= 36f;
        }
        return y;
    }

    static TMP_Text Line(RectTransform box, float y, string text, float size)
    {
        TMP_Text t = UIKit.Text(box, "", size, new Color(0.93f, 0.9f, 0.84f), Vector2.zero, new Vector2(480f, 34f), TextAlignmentOptions.Left);
        t.text = text;
        RectTransform r = t.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(0f, y);
        return t;
    }

    // ================================================================= 밸런스 기록
    // 판이 끝날 때마다 한 줄 (persistentDataPath/balance_log.csv): 영혼 조각 · 진화 시점 · 가지별 칸 수를 숫자로 보고 조정하려고
    public static string BalanceLogPath => System.IO.Path.Combine(Application.persistentDataPath, "balance_log.csv");

    static void WriteBalanceLog(string result)
    {
        if (GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) return;     // 테스트 · 자동 플레이는 기록하지 않음
        try
        {
            string path = BalanceLogPath;
            const string Header = "date,version,character,difficulty,endless,result,seconds,level,kills,shards_total,shards_spent,evo1,evo1_sec,evo2,evo2_sec,weapon,ultimate,skill,survival,soul,wealth,cards,stage,dealt_total,dealt_by,taken_total,taken_by,last_hit";
            // 2.1.1 에서 칸이 늘어남: 예전 머리줄의 기록은 따로 남겨 두고 새로 시작
            if (System.IO.File.Exists(path))
            {
                string first;
                using (System.IO.StreamReader r = new System.IO.StreamReader(path)) first = r.ReadLine();
                if (first != Header) System.IO.File.Move(path, System.IO.Path.Combine(Application.persistentDataPath, "balance_log_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"));
            }
            bool fresh = !System.IO.File.Exists(path);
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            if (fresh) b.AppendLine(Header);
            string E(int i, bool time) => i < Evolutions.Count ? (time ? Evolutions[i].Value.ToString("0") : Evolutions[i].Key.Replace(",", " ")) : "";
            b.Append(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")).Append(',')
             .Append(Application.version).Append(',')
             .Append(CharacterData.Selected).Append(',')
             .Append(GameMode.Current).Append(',')
             .Append(GameMode.IsEndless ? 1 : 0).Append(',')
             .Append(result).Append(',')
             .Append(Seconds.ToString("0")).Append(',')
             .Append(Level).Append(',').Append(Kills).Append(',')
             .Append(ShardsTotal).Append(',').Append(ShardsSpent).Append(',')
             .Append(E(0, false)).Append(',').Append(E(0, true)).Append(',')
             .Append(E(1, false)).Append(',').Append(E(1, true));
            for (int i = 0; i < 6; i++) b.Append(',').Append(i < Branches.Length ? Branches[i] : 0);
            b.Append(',').Append(Cards.Count);
            // 출처별 피해는 "이름:양|이름:양" (많은 순)
            string By(Dictionary<string, float> d)
            {
                List<KeyValuePair<string, float>> all = new List<KeyValuePair<string, float>>(d);
                all.Sort((x, y) => y.Value.CompareTo(x.Value));
                List<string> parts = new List<string>();
                foreach (KeyValuePair<string, float> kv in all) parts.Add(kv.Key.Replace(",", " ").Replace("|", " ").Replace(":", " ") + ":" + kv.Value.ToString("0"));
                return string.Join("|", parts);
            }
            b.Append(',').Append(Stage)
             .Append(',').Append(Sum(DamageDealt).ToString("0")).Append(',').Append(By(DamageDealt))
             .Append(',').Append(Sum(DamageTaken).ToString("0")).Append(',').Append(By(DamageTaken))
             .Append(',').Append(result == "death" ? LastHurtBy : "").AppendLine();
            System.IO.File.AppendAllText(path, b.ToString(), new System.Text.UTF8Encoding(true));
        }
        catch (System.Exception ex) { Debug.LogWarning("balance log: " + ex.Message); }
    }

    // ================================================================= ESC 메뉴 오른쪽의 이번 판 요약
    static GameObject summaryPanel;

    public static void ShowSummary(bool show)
    {
        if (!show)
        {
            if (summaryPanel != null) Destroy(summaryPanel);
            return;
        }
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        if (summaryPanel != null) Destroy(summaryPanel);
        summaryPanel = new GameObject("RunSummary", typeof(RectTransform), typeof(Image));
        RectTransform r = summaryPanel.GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false);
        r.SetAsLastSibling();
        r.anchorMin = r.anchorMax = new Vector2(1f, 0.5f);
        r.pivot = new Vector2(1f, 0.5f);
        r.anchoredPosition = new Vector2(-30f, -20f);    // 위쪽 영혼 트리 버튼(위에서 222 ~ 266) 아래부터
        r.sizeDelta = new Vector2(440f, 560f);          // 4:3 화면에서도 ESC 창(폭 720)과 안 겹치게
        Image bg = summaryPanel.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.05f, 0.08f, 0.88f);
        bg.raycastTarget = false;

        GameObject tg = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform tr = tg.GetComponent<RectTransform>();
        tr.SetParent(r, false);
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(26f, 22f);
        tr.offsetMax = new Vector2(-26f, -22f);
        TextMeshProUGUI t = tg.GetComponent<TextMeshProUGUI>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) t.font = UIKit.Font;
        if (UIKit.FontMaterial != null) t.fontSharedMaterial = UIKit.FontMaterial;
        t.fontSize = 22f;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14f;
        t.fontSizeMax = 22f;
        t.color = new Color(0.93f, 0.9f, 0.84f);
        t.alignment = TextAlignmentOptions.TopLeft;
        t.raycastTarget = false;
        t.text = "<size=120%>" + Loc.T("이번 판") + "</size>\n\n" + Summary(true);
    }
}

// 처음 한 번만 뜨는 짧은 도움말 (설정에서 끌 수 있음). 화면 아래 가운데, HUD 위
// 1.0.4~: 도움말을 줄 세워 하나씩 차례로 보여 줌 (뒤에 온 것이 앞의 것을 덮지 않음)
//   시간이 멈춘 동안(카드 · 보상 · 영혼 트리 · ESC 창)은 숨기고 남은 시간도 멈춤, 5초를 다 보여 준 뒤에만 '봤음'으로 저장
//   설정 「도움말 다시 보기」(ResetAll)로 처음부터 다시 볼 수 있음
public static class Hints
{
    static GameObject box;
    static TextMeshProUGUI text;
    static float left;                  // 지금 도움말이 남은 시간 (시간이 흐를 때만 줄어듦)
    static string current;              // 지금 띄운 도움말 키 (null = 없음)
    static HintRunner runner;
    static int sceneHandle = -1;
    static readonly List<(string key, string ko)> queue = new List<(string key, string ko)>();
    const float Duration = 5f;

    // 설정 「도움말 다시 보기」가 지우는 키 (Hints.Show 를 부르는 곳의 키를 모두 적음)
    static readonly string[] Keys = { "move", "levelup", "swap", "skill", "ult", "shop", "laststand", "soultree_buy", "soultree_btn", "upgrade" };

    public static void ResetAll()
    {
        foreach (string k in Keys) Prefs.DeleteKey("hint." + k);
        Prefs.Save();
    }

    public static void Show(string key, string ko)
    {
        if (!GameSettings.Hints || GameInput.Auto || Application.isBatchMode || GameInput.TrailerRunning || TutorialRun.Active) return;     // 튜토리얼은 자기 안내를 씀
        if (Prefs.GetInt("hint." + key, 0) == 1) return;
        // 씬이 바뀌면 지난 씬에서 줄 선 도움말은 버림 (상자는 캔버스와 함께 사라짐)
        int scene = SceneManager.GetActiveScene().handle;
        if (scene != sceneHandle) { sceneHandle = scene; queue.Clear(); current = null; }
        if (current == key) return;
        foreach (var q in queue) if (q.key == key) return;
        if (UIKit.HudCanvas() == null) return;
        queue.Add((key, ko));
        if (runner == null) runner = new GameObject("HintRunner").AddComponent<HintRunner>();       // 상자가 꺼져 있어도 줄을 살핌 (씬과 함께 사라짐)
    }

    static bool Build()
    {
        if (box != null) return true;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return false;
        box = new GameObject("Hint", typeof(RectTransform), typeof(Image));
        RectTransform r = box.GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
        r.pivot = new Vector2(0.5f, 0f);
        r.anchoredPosition = new Vector2(0f, 236f);      // 레벨업 대기 배지(110 ~ 220) · 특수 강화 버튼 위
        r.sizeDelta = new Vector2(920f, 70f);      // 1.0.1: 720×56 → 920×70, 진한 바탕 + 하늘색 테두리 (반투명이라 묻혔음)
        Image bg = box.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.94f);
        bg.raycastTarget = false;
        Outline edge = box.AddComponent<Outline>();
        edge.effectColor = new Color(0.45f, 0.85f, 1f, 0.95f);
        edge.effectDistance = new Vector2(3f, -3f);
        GameObject tg = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform tr = tg.GetComponent<RectTransform>();
        tr.SetParent(r, false);
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(16f, 4f);
        tr.offsetMax = new Vector2(-16f, -4f);
        text = tg.GetComponent<TextMeshProUGUI>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) text.font = UIKit.Font;
        if (UIKit.FontMaterial != null) text.fontSharedMaterial = UIKit.FontMaterial;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = 30f;
        text.fontStyle = FontStyles.Bold;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color32(0, 0, 0, 255);
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        box.SetActive(false);
        return true;
    }

    public static void Tick()
    {
        bool paused = Time.timeScale == 0f;
        if (current == null)
        {
            if (paused || queue.Count == 0 || !GameSettings.Hints) return;
            if (!Build()) { queue.Clear(); return; }
            var next = queue[0];
            queue.RemoveAt(0);
            if (Prefs.GetInt("hint." + next.key, 0) == 1) return;
            current = next.key;
            text.text = "<color=#FFE066>" + Loc.T("도움말") + "</color>  " + Loc.T(next.ko);
            left = Duration;
        }
        if (box == null) { current = null; return; }
        // 시간이 멈춘 동안은 숨기고 남은 시간도 그대로 (창 뒤에서 흘러가 버리지 않게)
        if (box.activeSelf == paused) box.SetActive(!paused);
        if (paused) return;
        left -= Time.unscaledDeltaTime;
        if (left > 0f) return;
        Prefs.SetInt("hint." + current, 1);         // 다 보여 준 뒤에 '봤음'
        current = null;
        box.SetActive(false);
    }
}

public class HintRunner : MonoBehaviour
{
    void Update() => Hints.Tick();
}

// 게임 오버 화면: [R]로 같은 캐릭터 · 난이도로 바로 다시 시작
public class QuickRestart : MonoBehaviour
{
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.R) || SettingsUI.IsOpen) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }
}

// 지금 적에게 피해를 주는 출처: 공격이 시작되는 곳(평타 · 필살기 · 특수 능력 · 고유 스킬)에서 정하고,
// 총알 · 장판 · 지속 피해 · 코루틴처럼 나중에 피해를 주는 것은 만들어질 때의 출처를 이어받음 (RunStats.Dealt)
public static class DamageSource
{
    public static string Current;
    public const string Basic = "평타", Ult = "필살기", Special = "특수 능력", Signature = "고유 스킬", Tree = "영혼 트리", Other = "기타";

    public readonly struct Scope : System.IDisposable
    {
        readonly string prev;
        public Scope(string source) { prev = Current; Current = source; }
        public void Dispose() => Current = prev;
    }

    // using (DamageSource.As("...")) { ... } 동안만 그 출처
    public static Scope As(string source) => new Scope(source);

    // 코루틴이 멈췄다 이어질 때마다 시작할 때의 출처로 (안에서 기다리는 코루틴도 같이)
    public static IEnumerator Keep(IEnumerator routine) => Keep(routine, Current);

    static IEnumerator Keep(IEnumerator routine, string source)
    {
        while (true)
        {
            string prev = Current;
            Current = source;
            bool more;
            try { more = routine.MoveNext(); }
            finally { Current = prev; }
            if (!more) yield break;
            object y = routine.Current;
            yield return y is IEnumerator inner ? Keep(inner, source) : y;
        }
    }
}
