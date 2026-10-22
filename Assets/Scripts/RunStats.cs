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
    public static readonly List<KeyValuePair<string, float>> SkillEvos = new List<KeyValuePair<string, float>>();     // 1.1.2~ 스킬 진화 (번역한 이름 · 시각)
    static string LogSkillEvos = "";        // 1.1.2~ balance_log 의 skill_evos: "키@초|키@초"
    public static int[] Branches = new int[6];
    public static int ShardsTotal, ShardsSpent;
    public static int Refines;              // 영혼 정련 횟수 (1.0.4~)
    static int LogWeight;                   // 1.2~ 판 기록 칸: 영혼의 무게 · 각인 saveCode · 유물 saveCode (Capture 가 판 안에서 채움)
    static string LogBrands = "", LogRelics = "";
    // 준 피해 · 받은 피해 (출처별 합), 마지막으로 맞은 공격과 그때의 장 (2.1.1~)
    public static readonly Dictionary<string, float> DamageDealt = new Dictionary<string, float>();
    public static readonly Dictionary<string, float> DamageTaken = new Dictionary<string, float>();
    public static string LastHurtBy = "";
    public static int LastHurtStage, Stage;
    // 1.1~ 장별 소요 시간 (balance_log 의 chapter_secs): 장에 들어온 시각 "장@초|장@초" · 이어하기는 RunSave 가 되살림
    public static string ChapterLog = "";
    static int chapterSeen;
    static bool runActive, cleared;
    // 이번 판을 끝까지 깼는지 (결과 화면이 「클리어!」로 · 1.0.4~ 엔딩 뒤에도 결과 화면을 거침)
    public static bool Cleared => cleared;
    public static void MarkCleared() => cleared = true;
    // 1.1.9~ 무한 모드 · 일일 도전에서 「포기하고 결과 보기」로 끝냄 (결과 화면이 「여기까지!」, 쓰러진 곳 줄 없음)
    public static bool GaveUp;

    float refresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        StageManager.Cleared += d => cleared = true;
        SceneManager.sceneLoaded += (s, m) =>
        {
            // 게임 씬을 떠날 때 (게임 오버 · 메인 메뉴 · 다시 시작) 이번 판을 밸런스 기록에 한 줄
            bool ended = runActive;
            if (runActive)
            {
                runActive = false;
                if (s.name == "GameOver") RunRecords.CommitBest();       // 1.1.9~ 내 최고 기록 (결과 화면 쪽에서도 부르지만 한 번만)
                WriteBalanceLog(cleared ? "clear" : GaveUp ? "quit" : s.name == "GameOver" ? "death" : "quit");
            }
            // 1.1.9~ 무한 모드를 메인 메뉴 · 다시 시작으로 나가면 지난 기록이 남아 다음 이야기 모드 결과 화면에 섞여 나왔음
            if (s.name != "GameOver") EndlessMode.LastSeconds = -1f;
            RunLog.OnScene(s.name, ended);          // 1.2~ 판 사이 기록 (다시 하기 방식 · 메뉴에서 보낸 시간 · 세션) — 위 줄을 쓴 뒤에 이번 판 값을 새로 정함
            if (s.name != "GameScene") return;
            runActive = true;
            cleared = false;
            GaveUp = false;
            Evolutions.Clear();
            SkillEvos.Clear();
            LogSkillEvos = "";
            Branches = new int[6];
            ShardsTotal = ShardsSpent = 0;
            Refines = 0;
            LogWeight = 0;
            LogBrands = LogRelics = "";
            Seconds = 0f;
            Kills = 0;
            Level = 1;
            Abilities.Clear();
            Cards.Clear();
            DamageDealt.Clear();
            DamageTaken.Clear();
            LastHurtBy = "";
            LastHurtStage = Stage = 0;
            ChapterLog = "";
            chapterSeen = 0;
            DamageSource.Current = null;
            ResetCombatStats();
            new GameObject("RunStats").AddComponent<RunStats>();
        };
    }

    // ================================================================= 1.1.9~ 전투 통계 (편의성 3차 2): 최근 10초 초당 피해 · 최근 1분 분당 처치
    // 0.1초 칸 100개 고리 · 게임 시간(Time.time) 기준이라 멈추면 멈춤 · 피해가 없던 칸은 읽을 때도 비움 (적이 없으면 0 으로 내려감)
    static readonly float[] dpsRing = new float[100];
    static int dpsAt = int.MinValue;
    static readonly Queue<float> killTimes = new Queue<float>();

    static void AdvanceDps()
    {
        int now = Mathf.FloorToInt(Time.time * 10f);
        if (dpsAt == int.MinValue || now - dpsAt >= dpsRing.Length || now < dpsAt) { System.Array.Clear(dpsRing, 0, dpsRing.Length); dpsAt = now; return; }
        while (dpsAt < now) { dpsAt++; dpsRing[((dpsAt % 100) + 100) % 100] = 0f; }
    }

    public static float Dps
    {
        get
        {
            AdvanceDps();
            float s = 0f;
            foreach (float v in dpsRing) s += v;
            return s / 10f;
        }
    }

    public static float KillsPerMin
    {
        get
        {
            while (killTimes.Count > 0 && Time.time - killTimes.Peek() > 60f) killTimes.Dequeue();
            return killTimes.Count;
        }
    }

    static void ResetCombatStats()
    {
        System.Array.Clear(dpsRing, 0, dpsRing.Length);
        dpsAt = int.MinValue;
        killTimes.Clear();
    }

    // 적 · 보스가 실제로 잃은 체력 (넘친 피해는 빼고): 지금 공격 중인 출처에
    public static void Dealt(float amount)
    {
        if (!runActive || amount <= 0f) return;
        AdvanceDps();
        dpsRing[((dpsAt % 100) + 100) % 100] += amount;
        string key = DamageSource.Current ?? DamageSource.Other;
        DamageDealt.TryGetValue(key, out float v);
        DamageDealt[key] = v + amount;
    }

    // 플레이어가 받은 피해: 때린 코드가 있는 파일로 출처를 가림 (PlayerController.TryHit 의 CallerFilePath)
    public static void Hurt(float amount, string callerFile)
    {
        if (!runActive || amount <= 0f) return;
        string file = System.IO.Path.GetFileNameWithoutExtension(callerFile ?? "");
        // 1.0.4~ BossSkills.Reborn · BossSkills.Developer 처럼 나눈 파일도 보스 스킬로 (리치 왕 영혼구 · 뼈 감옥이 「기타」로 나왔음)
        if (file.StartsWith("BossSkills")) file = "BossSkills";
        if (file.StartsWith("BossUltimate")) file = "BossUltimate";      // 1.2.5~ BossUltimate.Domain · BossUltimate.Developer 도 보스 결계
        // 1.0.1: 보스 · 적 스킬은 스킬 이름까지 (AttackLabel) — 예전엔 공용 판정을 거치면 보스 공격도 모두 「적 스킬」
        string named = file == "EnemySkill" || file == "BossSkills" || file == "BossUltimate" || file == "SpaceErosion"
            ? (AttackLabel.Pending ?? AttackLabel.FromStack()) : null;
        string key = named ?? file switch
        {
            "EnermyController" => "적과 부딪힘",
            "EnemySkill" => "적 스킬",
            "BossSkills" => "보스 스킬",
            "BossUltimate" => "보스 결계",
            "SpaceErosion" => "공간 침식",
            "PlayerController" => "보스와 부딪힘",
            "EndlessTerrain" => AttackLabel.Pending ?? "용암 웅덩이",      // 1.2.3~ 무한 지형
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
    static void OnKill(Vector3 pos)
    {
        if (!EnermyController.KillCounts) return;
        Kills++;
        while (killTimes.Count > 0 && Time.time - killTimes.Peek() > 60f) killTimes.Dequeue();      // 전투 통계를 꺼도 큐가 판 내내 커지지 않게
        killTimes.Enqueue(Time.time);
    }     // 1.2~ 정화로 쓰러진 적 절반은 세지 않음

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
        Difficulty = GameMode.Name(GameMode.Current) + SoulWeight.RunTag() + SoulBrand.RunTag();      // 1.2~ 「· 무게 3 (최고 4) · 각인 유리 대포, 철벽」
        // 1.2~ 판 기록(balance_log)에 남길 무게 · 각인 · 유물 (기록은 씬이 바뀔 때 쓰므로 판 안에서 미리 잡아 둠 — 메인 메뉴로 나가면 각인 목록이 먼저 비워짐)
        LogWeight = SoulWeight.Level;
        LogBrands = SoulBrand.LogCodes();
        RunLog.NotePoints(CharacterData.RunPoints);     // 1.2~ points_run (같은 씬을 다시 열면 다른 쪽이 먼저 0 으로 비울 수 있어 미리 잡아 둠)
        SpecialAbilities rs = SpecialAbilities.SharedInstance;
        if (rs != null)
        {
            List<string> relicCodes = new List<string>();
            foreach (int id in rs.Relics) { RelicDef r = ContentRegistry.RelicByAbility(id); if (r != null) relicCodes.Add(r.saveCode); }
            LogRelics = string.Join("|", relicCodes);
        }
        PlayerController p = Hostile.Player;
        if (p != null) Level = p.level;
        if (StageManager.Instance != null) Stage = Chapters.Number(StageManager.Instance.CurrentStage);
        NoteChapter();

        // 0.5초마다 씬을 뒤지지 않게 한 번 찾아 두고 씀
        if (sp == null) sp = FindFirstObjectByType<SpecialAbilities>();
        if (sp != null)
        {
            Evolutions.Clear();
            Evolutions.AddRange(sp.EvolutionLog);
            Branches = sp.OwnedPerBranch();
            ShardsTotal = SoulShards.Total;
            ShardsSpent = SoulShards.Spent;
            Refines = sp.Refines;
        }
        if (sp != null && sp.abilities != null)
        {
            Abilities.Clear();
            foreach (int id in sp.EquippedIds)
                if (id >= 0 && id < sp.abilities.Length && sp.abilities[id] != null)
                    Abilities.Add(Loc.T(sp.abilities[id].name) + (sp.IsEvolved(id) ? "+" : "") + (sp.IsRelic(id) ? " <color=#C9A2FF>(" + Loc.T("유물") + ")</color>" : ""));     // 1.2~ 석관에서 얻은 유물 표시
        }
        if (shop == null) shop = FindFirstObjectByType<LevelShop>(FindObjectsInactive.Include);
        if (shop != null && shop.ability_level != null && shop.ability_name != null)
        {
            Cards.Clear();
            for (int i = 0; i < shop.ability_level.Length && i < shop.ability_name.Length; i++)
                if (shop.ability_level[i] > 0 && !string.IsNullOrEmpty(shop.ability_name[i]))
                    Cards.Add(shop.ability_name[i] + " Lv" + shop.ability_level[i]);
            // 끝없는 성장 카드 (1.0.4~, LevelShop.Growth)
            for (int k = 0; k < LevelShop.GrowthCount; k++)
                if (shop.GrowthLevel(k) > 0) Cards.Add(LevelShop.GrowthName(k) + " Lv" + shop.GrowthLevel(k));
            // 1.1.2~ 스킬 진화
            SkillEvos.Clear();
            List<string> codes = new List<string>();
            foreach (KeyValuePair<LevelShop.SkillEvo, float> kv in shop.EvoLog)
            {
                SkillEvos.Add(new KeyValuePair<string, float>(Loc.T(kv.Key.name), kv.Value));
                codes.Add(kv.Key.key + "@" + kv.Value.ToString("0"));
            }
            LogSkillEvos = string.Join("|", codes);
        }
    }

    // 1.1.9~ 게임 씬을 떠나 결과 화면으로 가는 순간 (쓰러짐 첫 줄 · 포기 · 엔딩 끝): 기록 + 빌드 자세히 보기 스냅샷
    public static void CaptureForResult()
    {
        Capture();
        BuildView.TakeSnapshot();
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
           + "\n" + Loc.T("영혼 조각") + " " + ShardsTotal + "  (" + Loc.T("씀") + " " + ShardsSpent + ")"
           + (Refines > 0 ? "     " + Loc.T("영혼 정련") + " x" + Refines : "");      // 1.0.4~
        s += "\n\n" + gold + Loc.T("특수 능력") + end + "\n" + (Abilities.Count > 0 ? string.Join(",  ", Abilities) : "-");
        s += "\n\n" + gold + Loc.T("레벨업 카드") + end + "\n" + (Cards.Count > 0 ? string.Join(",  ", Cards) : "-");
        // 1.1.2~ 스킬 진화: 이름 (시각)
        if (SkillEvos.Count > 0)
        {
            List<string> se = new List<string>();
            foreach (KeyValuePair<string, float> kv in SkillEvos) se.Add("<color=#C9A0FF>" + kv.Key + "</color> <color=#A89C86>(" + EndlessMode.Clock(kv.Value) + ")</color>");
            s += "\n\n" + gold + Loc.T("스킬 진화") + end + "\n" + string.Join(",  ", se);
        }
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
        RectTransform box = UIKit.Rect("DamagePanel", canvas, Vector2.zero, new Vector2(480f, 600f));     // 2026-10-22~ 520 → 600 (도트 눈금 줄)
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(1f, 0.5f);
        box.anchoredPosition = new Vector2(-60f, 0f);
        // 2026-10-22~ 도트 패널 판 뒤에 깔기 (사용자 「그리기로 그린 막대 말고 분위기 어울리는 도트로」)
        if (UIKit.PanelSprite != null)
        {
            RectTransform pr = UIKit.Rect("DamagePanelBack", box, Vector2.zero, Vector2.zero);
            pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
            pr.offsetMin = new Vector2(-24f, -24f); pr.offsetMax = new Vector2(24f, 24f);
            pr.SetAsFirstSibling();
            Image pi = pr.gameObject.AddComponent<Image>();
            pi.sprite = UIKit.PanelSprite;
            pi.type = Image.Type.Sliced;
            pi.pixelsPerUnitMultiplier = 1f;
            pi.color = new Color(1f, 1f, 1f, 0.92f);
            if (!UiRules.Legacy && UiRules.Sprite("ui2_panel") != null) { pi.sprite = UiRules.Sprite("ui2_panel"); pi.type = Image.Type.Tiled; pi.color = Color.white; }     // UI 개편 P0-4c
            pi.raycastTarget = false;
        }
        float y = 0f;
        y = Section(box, y, Loc.T("준 피해"), Top(DamageDealt, 6), new Color(0.96f, 0.72f, 0.3f));
        y = Section(box, y - 20f, Loc.T("받은 피해"), Top(DamageTaken, 4), new Color(0.92f, 0.32f, 0.3f));
        // 쓰러졌으면 마지막으로 맞은 공격과 장
        if (!cleared && !GaveUp && !string.IsNullOrEmpty(LastHurtBy) && SceneManager.GetActiveScene().name == "GameOver")
        {
            string where = GameMode.IsEndless ? Loc.T("무한 모드") + "  ·  " : LastHurtStage > 0 ? Loc.T("{0}장").Replace("{0}", LastHurtStage.ToString()) + "  ·  " : "";
            TMP_Text t = Line(box, y - 14f, "<color=#F5D478>" + Loc.T("쓰러진 곳") + "</color>  " + where + SourceName(LastHurtBy), 22f);
            t.color = new Color(0.93f, 0.9f, 0.84f);
        }
    }

    // 1.1.9~ 빌드 자세히 보기(BuildView)도 같은 막대를 넓게 그림 (width, 결과 화면은 480)
    internal static float Section(RectTransform box, float y, string title, List<KeyValuePair<string, float>> rows, Color bar, float width = 480f)
    {
        if (rows.Count == 0) return y;
        float total = 0f;
        foreach (KeyValuePair<string, float> kv in rows) total += kv.Value;
        float best = Mathf.Max(1f, rows[0].Value);
        foreach (KeyValuePair<string, float> kv in rows) best = Mathf.Max(best, kv.Value);
        Line(box, y, "<color=#F5D478><size=110%>" + title + "</size></color>   <color=#A89C86>" + Mathf.RoundToInt(total).ToString("N0") + "</color>", 24f, width);
        y -= 36f;
        // 2026-10-22~ 도트 눈금 게이지 (사용자 「그리기로 그린 막대 말고 도트로」): 제목 아래 도트 구분선 · 줄마다 이름 + 오른쪽 비율, 그 아래 12칸 눈금
        //   (예전: 작은 도트 그림을 늘려 붙인 매끈한 막대 · 이름이 막대 위에 겹침) · 도트 그림이 없으면 예전 막대
        if (UIKit.DividerSprite != null && UIKit.BarFillSprite != null)
        {
            RectTransform dv = UIKit.Rect("Divider", box, Vector2.zero, new Vector2(width, 8f));
            dv.anchorMin = dv.anchorMax = dv.pivot = new Vector2(0f, 1f);
            dv.anchoredPosition = new Vector2(0f, y);
            Image di = dv.gameObject.AddComponent<Image>();
            di.sprite = UIKit.DividerSprite;
            di.color = new Color(1f, 1f, 1f, 0.85f);
            di.raycastTarget = false;
            y -= 14f;
            const int Seg = 12;
            const float SegGap = 4f, SegH = 10f;
            float segW = (width - SegGap * (Seg - 1)) / Seg;
            foreach (KeyValuePair<string, float> kv in rows)
            {
                TMP_Text nm = UIKit.Text(box, "", 20f, new Color(0.95f, 0.92f, 0.86f), Vector2.zero, new Vector2(width - 120f, 26f), TextAlignmentOptions.Left);
                nm.rectTransform.anchorMin = nm.rectTransform.anchorMax = nm.rectTransform.pivot = new Vector2(0f, 1f);
                nm.rectTransform.anchoredPosition = new Vector2(0f, y);
                nm.text = SourceName(kv.Key);
                nm.enableAutoSizing = true; nm.fontSizeMin = 12f; nm.fontSizeMax = 20f; nm.raycastTarget = false;
                TMP_Text pc = UIKit.Text(box, "", 20f, new Color(0.96f, 0.86f, 0.55f), Vector2.zero, new Vector2(width > 480f ? 260f : 110f, 26f), TextAlignmentOptions.Right);
                pc.rectTransform.anchorMin = pc.rectTransform.anchorMax = pc.rectTransform.pivot = new Vector2(1f, 1f);
                pc.rectTransform.anchoredPosition = new Vector2(0f, y);
                pc.text = (width > 480f ? "<color=#A89C86>" + Mathf.RoundToInt(kv.Value).ToString("N0") + "</color>   " : "") + (kv.Value / Mathf.Max(1f, total) * 100f).ToString("0") + "%";
                pc.raycastTarget = false;
                y -= 26f;
                int on = Mathf.Clamp(Mathf.CeilToInt(Seg * kv.Value / best - 0.001f), kv.Value > 0f ? 1 : 0, Seg);
                for (int i = 0; i < Seg; i++)
                {
                    RectTransform sg = UIKit.Rect("Seg", box, Vector2.zero, new Vector2(segW, SegH));
                    sg.anchorMin = sg.anchorMax = sg.pivot = new Vector2(0f, 1f);
                    sg.anchoredPosition = new Vector2(i * (segW + SegGap), y);
                    Image si = sg.gameObject.AddComponent<Image>();
                    si.sprite = UIKit.BarFillSprite;
                    si.type = Image.Type.Sliced;
                    si.pixelsPerUnitMultiplier = 1f;
                    si.color = i < on ? new Color(bar.r, bar.g, bar.b, 0.95f) : new Color(0.3f, 0.26f, 0.34f, 0.55f);
                    si.raycastTarget = false;
                }
                y -= SegH + 8f;
            }
            return y;
        }
        y -= 4f;
        foreach (KeyValuePair<string, float> kv in rows)
        {
            // 막대 (가장 많은 것을 꽉 채운 길이로) · 이름 · 비율
            RectTransform back = UIKit.Rect("Bar", box, Vector2.zero, new Vector2(width, 30f));
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0f, 1f);
            back.anchoredPosition = new Vector2(0f, y);
            Image bg = back.gameObject.AddComponent<Image>();
            // 1.4.8~ 새 도트 스타일: 옅은 흰 칸 대신 막대 틀(ui_bar_frame · 테두리 반 두께 3) · 채움(ui_bar_fill)은 틀 안쪽 홈에 · 길이 비율은 그대로
            //   그림이 없으면 예전 모습
            bool art = UIKit.BarFrameSprite != null && UIKit.BarFillSprite != null;
            float inset = art ? 3f : 0f;
            if (art)
            {
                bg.sprite = UIKit.BarFrameSprite;
                bg.type = Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = 2f;
                bg.color = new Color(1f, 1f, 1f, 0.9f);
            }
            else bg.color = new Color(1f, 1f, 1f, 0.06f);
            bg.raycastTarget = false;
            RectTransform fill = UIKit.Rect("Fill", back, Vector2.zero, new Vector2((width - inset * 2f) * kv.Value / best, 30f - inset * 2f));
            fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = new Vector2(inset, 0f);
            Image fi = fill.gameObject.AddComponent<Image>();
            if (art) fi.sprite = UIKit.BarFillSprite;
            fi.color = new Color(bar.r, bar.g, bar.b, art ? 0.6f : 0.45f);
            fi.raycastTarget = false;
            TMP_Text name = UIKit.Text(back, "", 20f, new Color(0.95f, 0.92f, 0.86f), new Vector2(10f, 0f), new Vector2(width - 140f, 30f), TextAlignmentOptions.Left);
            name.rectTransform.anchorMin = name.rectTransform.anchorMax = name.rectTransform.pivot = new Vector2(0f, 0.5f);
            name.text = SourceName(kv.Key);
            name.enableAutoSizing = true;
            name.fontSizeMin = 12f;
            name.fontSizeMax = 20f;
            TMP_Text pct = UIKit.Text(back, "", 20f, new Color(0.95f, 0.92f, 0.86f), new Vector2(-10f, 0f), new Vector2(110f, 30f), TextAlignmentOptions.Right);
            pct.text = (width > 480f ? "<color=#A89C86>" + Mathf.RoundToInt(kv.Value).ToString("N0") + "</color>   " : "") + (kv.Value / Mathf.Max(1f, total) * 100f).ToString("0") + "%";      // 넓은 판(빌드 보기)은 총량도
            if (width > 480f) pct.rectTransform.sizeDelta = new Vector2(260f, 30f);
            pct.rectTransform.anchorMin = pct.rectTransform.anchorMax = pct.rectTransform.pivot = new Vector2(1f, 0.5f);
            y -= 36f;
        }
        return y;
    }

    static TMP_Text Line(RectTransform box, float y, string text, float size, float width = 480f)
    {
        TMP_Text t = UIKit.Text(box, "", size, new Color(0.93f, 0.9f, 0.84f), Vector2.zero, new Vector2(width, 34f), TextAlignmentOptions.Left);
        t.text = text;
        RectTransform r = t.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(0f, y);
        return t;
    }

    // ================================================================= 밸런스 기록
    // 판이 끝날 때마다 한 줄 (persistentDataPath/balance_log.csv): 영혼 조각 · 진화 시점 · 가지별 칸 수를 숫자로 보고 조정하려고
    public static string BalanceLogPath => System.IO.Path.Combine(Application.persistentDataPath, "balance_log.csv");

    // 1.1~ 새 장에 들어오면 그 시각을 적어 둠
    static void NoteChapter()
    {
        if (Stage <= 0 || Stage == chapterSeen) return;
        chapterSeen = Stage;
        ChapterLog += (ChapterLog.Length > 0 ? "|" : "") + Stage + "@" + Seconds.ToString("0");
    }

    // 이어하기 (RunSave): 저장해 둔 장 기록으로 바꾸고, 마지막으로 적은 장부터 이어서
    public static void RestoreChapterLog(string log)
    {
        ChapterLog = log ?? "";
        chapterSeen = 0;
        int at = ChapterLog.LastIndexOf('|') + 1;
        int sep = ChapterLog.IndexOf('@', at);
        if (sep > at) int.TryParse(ChapterLog.Substring(at, sep - at), out chapterSeen);
    }

    // 장마다 머문 시간 "장:초|장:초" (마지막 장은 판이 끝날 때까지)
    static string ChapterDurations()
    {
        if (string.IsNullOrEmpty(ChapterLog)) return "";
        string[] parts = ChapterLog.Split('|');
        List<string> outs = new List<string>();
        for (int i = 0; i < parts.Length; i++)
        {
            string[] a = parts[i].Split('@');
            if (a.Length < 2 || !float.TryParse(a[1], out float from)) continue;
            float to = Seconds;
            if (i + 1 < parts.Length)
            {
                string[] b = parts[i + 1].Split('@');
                if (b.Length > 1 && float.TryParse(b[1], out float next)) to = next;
            }
            outs.Add(a[0] + ":" + Mathf.Max(0f, to - from).ToString("0"));
        }
        return string.Join("|", outs);
    }

    static void WriteBalanceLog(string result)
    {
        if (GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) return;     // 테스트 · 자동 플레이는 기록하지 않음
        try
        {
            string path = BalanceLogPath;
            // 1.2~ 끝에 판 사이 기록 여섯 칸 (RunLog.Columns) · 1.2~ 무게 · 각인 · 유물 세 칸 · 1.1.2~ 스킬 진화 한 칸
            const string Header = "date,version,character,difficulty,endless,result,seconds,level,kills,shards_total,shards_spent,evo1,evo1_sec,evo2,evo2_sec,weapon,ultimate,skill,survival,soul,wealth,cards,stage,dealt_total,dealt_by,taken_total,taken_by,last_hit,chapter_secs,weight,brands,relics,skill_evos," + RunLog.Columns;
            // 2.1.1 에서 칸이 늘어남: 예전 머리줄의 기록은 따로 남겨 두고 새로 시작
            // 1.2~ 끝에 칸만 더해진 머리줄이면 따로 떼지 않고 머리줄만 바꿔 한 파일로 이어 씀 (옛 줄은 뒤 칸이 비어 있음 · 합쳐 읽기는 tools/balance/merge_logs.py)
            if (System.IO.File.Exists(path))
            {
                string first;
                using (System.IO.StreamReader r = new System.IO.StreamReader(path)) first = r.ReadLine();
                if (first != Header && !RunLog.UpgradeHeader(path, first, Header))
                    System.IO.File.Move(path, System.IO.Path.Combine(Application.persistentDataPath, "balance_log_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"));
            }
            bool fresh = !System.IO.File.Exists(path);
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            if (fresh) b.AppendLine(Header);
            int rowStart = b.Length;                         // 1.4.4~ 플레이 데이터 보내기 (Telemetry) 에 이번 판 줄만 넘김
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
             .Append(',').Append(result == "death" ? LastHurtBy : "")
             .Append(',').Append(ChapterDurations())       // 1.1~ 장별 소요 시간
             .Append(',').Append(LogWeight).Append(',').Append(LogBrands).Append(',').Append(LogRelics)     // 1.2~ 무게 · 각인 · 유물
             .Append(',').Append(LogSkillEvos)        // 1.1.2~ 스킬 진화 "키@초|…"
             .Append(',').Append(RunLog.Row()).AppendLine();       // 1.2~ 판 사이 기록 (retry_kind … points_run)
            Telemetry.RecordRun(Header, b.ToString(rowStart, b.Length - rowStart).TrimEnd('\r', '\n'));     // 1.4.4~ 동의한 이용자만 (안에서 확인)
            System.IO.File.AppendAllText(path, b.ToString(), new System.Text.UTF8Encoding(true));
            RunLog.OnRunLogged(result, Seconds);      // 1.2~ 세션 기록(sessions.csv)의 판 수
        }
        catch (System.Exception ex) { Debug.LogWarning("balance log: " + ex.Message); }
    }

    // ================================================================= ESC 메뉴 오른쪽의 이번 판 요약
    static GameObject summaryPanel;

    // 1.4.8~ ESC 양옆 패널(왼쪽 「지금 가진 스킬」 PauseBuild · 오른쪽 「이번 판」)의 위끝 · 아래 여유 — 캔버스 위에서 280 (영혼 트리 버튼 · 스킬 칸 넘김 아래),
    //   아래에서 175 (탄창 · 레벨 막대 위) 사이. 위끝은 둘이 같고, 높이는 내용만큼 (오른쪽은 남는 만큼 · 최대 720)
    public const float PausePanelTop = 280f, PausePanelBottom = 175f, PausePanelW = 440f, PausePad = 24f;
    public static float PausePanelMaxH(RectTransform canvas) => Mathf.Clamp(canvas.rect.height - PausePanelTop - PausePanelBottom, 360f, 720f);

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
        // 1.4.8~ 위 오른쪽에 붙임 (예전 가운데 기준 (-30, -20) 440×560 과 위끝 같음) · 높이는 캔버스에 맞춰 (UI 크기 110 · 120% 에서도 탄창 칸과 안 겹침)
        float h = PausePanelMaxH((RectTransform)canvas.transform);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-30f, -PausePanelTop);
        r.sizeDelta = new Vector2(PausePanelW, h);           // 4:3 화면에서도 ESC 창(폭 720)과 안 겹치게
        Image bg = summaryPanel.GetComponent<Image>();
        UIKit.EnsureStyle();
        // 1.4.8~ 새 도트 스타일: 단색 판 대신 창 판(ui_panel: 금 이중 테두리 · 모서리 보석) · 그림이 없으면 예전 단색
        if (UIKit.PanelSprite != null)
        {
            bg.sprite = UIKit.PanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(1f, 1f, 1f, 0.94f);
        }
        else bg.color = new Color(0.06f, 0.05f, 0.08f, 0.88f);
        bg.raycastTarget = true;        // 1.1.9~ 이 판 위에서는 아래 HUD 툴팁을 띄우지 않음 (PauseInspect) · 빈 곳을 눌러도 아무 일 없음
        float innerW = PausePanelW - PausePad * 2f;

        // 제목 「이번 판」 (소제목 28)
        TMP_Text title = UIKit.Text(r, "이번 판", 28f, new Color(0.96f, 0.83f, 0.47f), Vector2.zero, new Vector2(innerW, 38f), TextAlignmentOptions.Left);
        title.fontSizeMin = 20f;
        RectTransform ttr = title.rectTransform;
        ttr.anchorMin = ttr.anchorMax = ttr.pivot = new Vector2(0f, 1f);
        ttr.anchoredPosition = new Vector2(PausePad, -PausePad + 6f);

        // 1.4.8~ 예전엔 한 글(22 · 자동 크기 하한 12)이라 항목이 많으면 14 안팎으로 줄었음 → 항목(빈 줄로 나뉜 덩이)마다 글 하나 + 사이 구분선,
        //   22 → 21 → 20 중 다 들어가는 가장 큰 크기 (20 에서도 넘치면 마지막 덩이 끝을 … 로 — 전부는 「빌드 자세히 보기」)
        const float BtnH = 52f, SecGap = 16f;
        float top = PausePad + 38f + 10f;                    // 제목 아래
        float bottom = PausePad + BtnH + 16f;                 // 버튼 줄 위
        float avail = h - top - bottom;
        string[] parts = Summary(true).Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
        foreach (string part in parts)
        {
            TextMeshProUGUI t = (TextMeshProUGUI)UIKit.Text(r, "", 22f, new Color(0.93f, 0.9f, 0.84f), Vector2.zero, new Vector2(innerW, 30f), TextAlignmentOptions.TopLeft);
            t.enableAutoSizing = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.text = part;
            texts.Add(t);
        }
        float size = 20f;
        foreach (float s in new[] { 22f, 21f, 20f })
        {
            float sum = SecGap * Mathf.Max(0, texts.Count - 1);
            foreach (TextMeshProUGUI t in texts) { t.fontSize = s; sum += t.GetPreferredValues(t.text, innerW, Mathf.Infinity).y; }
            if (sum <= avail) { size = s; break; }
        }
        float y = top;
        for (int i = 0; i < texts.Count; i++)
        {
            TextMeshProUGUI t = texts[i];
            t.fontSize = size;
            float th = Mathf.Ceil(t.GetPreferredValues(t.text, innerW, Mathf.Infinity).y);
            float room = h - bottom - y;
            if (room < size * 1.3f) { Destroy(t.gameObject); continue; }      // 한 줄도 안 들어가면 뺌 (위 덩이 끝이 …)
            th = Mathf.Min(th, room);
            RectTransform tr = t.rectTransform;
            tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0f, 1f);
            tr.sizeDelta = new Vector2(innerW, th);
            tr.anchoredPosition = new Vector2(PausePad, -y);
            y += th;
            if (i < texts.Count - 1 && h - bottom - (y + SecGap) >= size * 1.3f)
            {
                // 항목 사이 구분선 (가운데 · 흐린 보라)
                RectTransform line = UIKit.Rect("Divider", r, Vector2.zero, new Vector2(innerW, 2f));
                line.anchorMin = line.anchorMax = line.pivot = new Vector2(0f, 1f);
                line.anchoredPosition = new Vector2(PausePad, -(y + SecGap * 0.5f - 1f));
                Image li = line.gameObject.AddComponent<Image>();
                li.color = new Color(0.79f, 0.63f, 1f, 0.22f);
                li.raycastTarget = false;
            }
            y += SecGap;
        }

        // 1.1.9~ 「빌드 자세히 보기」 · 「영웅 안내」 · 1.4.8~ 패널 안쪽 여백 24 에 맞춰 반씩 · 사이 12 · 글씨 22 (하한 20)
        Transform root = canvas.transform;
        float bw = (innerW - 12f) * 0.5f, by = -h * 0.5f + PausePad + BtnH * 0.5f;
        Button build = UIKit.MakeButton(r, "빌드 자세히 보기", new Vector2(-(bw + 12f) * 0.5f, by), new Vector2(bw, BtnH), () => BuildView.Open(root), 22f);
        Button guide = UIKit.MakeButton(r, "영웅 안내", new Vector2((bw + 12f) * 0.5f, by), new Vector2(bw, BtnH), () => HeroGuide.OpenView(root), 22f);
        foreach (Button b in new[] { build, guide })
        {
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            TMP_Text bt = b.GetComponentInChildren<TMP_Text>();
            if (bt != null) { bt.fontSizeMin = 20f; bt.fontSizeMax = 22f; }
        }
    }
}

// 처음 한 번만 뜨는 짧은 도움말 (설정에서 끌 수 있음). 화면 아래 가운데, HUD 위
// 1.0.4~: 도움말을 줄 세워 하나씩 차례로 보여 줌 (뒤에 온 것이 앞의 것을 덮지 않음)
//   시간이 멈춘 동안(카드 · 보상 · 영혼 트리 · ESC 창)은 숨기고 남은 시간도 멈춤, 다 보여 준 뒤(또는 닫은 뒤)에만 '봤음'으로 저장
//   설정 「도움말 다시 보기」(ResetAll)로 처음부터 다시 볼 수 있음
// 1.1.2~ 안내 메시지 개편 (Notice): 1040 폭 패널 · ? 아이콘 · 28pt 본문(키 · 숫자 색 강조) · 남은 시간 막대 · [Tab] 닫기 · 7초 · 아래에서 올라옴
public static class Hints
{
    static Notice.TipPanel box;
    static float left;                  // 지금 도움말이 남은 시간 (시간이 흐를 때만 줄어듦)
    static float shown;                 // 지금 도움말을 보여 준 시간 (나타나는 움직임)
    static string current;              // 지금 띄운 도움말 키 (null = 없음)
    static HintRunner runner;
    static int sceneHandle = -1;
    static readonly List<(string key, string ko)> queue = new List<(string key, string ko)>();
    const float Duration = 7f;

    // 설정 「도움말 다시 보기」가 지우는 키 (Hints.Show 를 부르는 곳의 키를 모두 적음)
    static readonly string[] Keys = { "move", "levelup", "swap", "skill", "ult", "shop", "laststand", "soultree_buy", "soultree_btn", "upgrade",
                                             "bossult.gauge", "growth", "laststand.hud", "targetmark", "transcend",     // 1.1~ 빠져 있던 키
                                             "relic", "mapprop",         // 1.2~ 유물 · 맵 물체
                                             "terrain", "endless.bossqueue",     // 1.2.3~ 무한 지형 변화 · 보스는 기다리지 않는다
                                             "bossult.shared",                   // 1.2.5~ 통합 결계 게이지 · 공간 침식
                                             "dev.code", "dev.popup", "mom.medal", "mom.lights",     // 개발자 · 엄마 고유 기본 공격
                                             "paths.seed", "paths.crossroads",                 // 2026-10-19~ 빌드 갈래 입문 · 갈림길
                                             "endless.slowlevel", "endless.phaseshard" };      // 1.4.2~ 무한 후반 레벨 곡선 · 단계 보상 조각 (도움말 다시 보기에 걸리게)

    public static void ResetAll()
    {
        foreach (string k in Keys) Prefs.DeleteKey("hint." + k);
        Prefs.DeleteKey("hint.pauseinspect");       // 1.1.9~ 일시정지 중 설명 안내 (처음 다섯 번)
        HeroGuide.ResetAll();                       // 1.1.9~ 영웅 첫 판 안내 카드
        Prefs.Save();
    }

    public static void Show(string key, string ko)
    {
        if (!GameSettings.Hints || GameInput.Auto || Application.isBatchMode || GameInput.TrailerRunning || TutorialRun.Active) return;     // 튜토리얼은 자기 안내를 씀
        if (Prefs.GetInt("hint." + key, 0) == 1) return;
        // 씬이 바뀌면 지난 씬에서 줄 선 도움말은 버림 (상자는 캔버스와 함께 사라짐)
        int scene = SceneManager.GetActiveScene().handle;
        if (scene != sceneHandle) { sceneHandle = scene; queue.Clear(); current = null; box = null; }
        if (current == key) return;
        foreach (var q in queue) if (q.key == key) return;
        if (UIKit.HudCanvas() == null) return;
        queue.Add((key, ko));
        if (runner == null) runner = new GameObject("HintRunner").AddComponent<HintRunner>();       // 상자가 꺼져 있어도 줄을 살핌 (씬과 함께 사라짐)
    }

    static bool Build()
    {
        if (box != null && box.r != null) return true;
        box = Notice.BuildTip();
        return box != null;
    }

    // 최적화: 패드 · Tab 키 사용 여부 · 언어가 바뀔 때만 다시 만듦 (예전엔 도움말이 떠 있는 동안 매 프레임 Replace)
    static string closeText;
    static bool closePad, closeFree;
    static Loc.Lang closeLang;
    static string CloseText
    {
        get
        {
            bool pad = GamePad.Active, free = Notice.TipCloseKeyFree;
            if (closeText == null || pad != closePad || free != closeFree || Loc.Current != closeLang)
            {
                closePad = pad; closeFree = free; closeLang = Loc.Current;
                closeText = pad || !free ? "" : Loc.T("notice.tip.close").Replace("{0}", "Tab");
            }
            return closeText;
        }
    }

    public static void Tick()
    {
        bool paused = Notice.Held;
        if (current == null)
        {
            if (paused || queue.Count == 0 || !GameSettings.Hints) return;
            if (!Build()) { queue.Clear(); return; }
            var next = queue[0];
            queue.RemoveAt(0);
            if (Prefs.GetInt("hint." + next.key, 0) == 1) return;
            current = next.key;
            Notice.FillTip(box, Loc.T("도움말"), Loc.T(next.ko), CloseText);
            left = Duration;
            shown = 0f;
        }
        if (box == null || box.r == null) { current = null; return; }
        // 시간이 멈춘 동안 · 창이 열린 동안은 숨기고 남은 시간도 그대로 (창 뒤에서 흘러가 버리지 않게)
        if (box.r.gameObject.activeSelf == paused) box.r.gameObject.SetActive(!paused);
        if (paused) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        shown += dt;
        left -= dt;
        // 아래에서 살짝 올라오며 나타남 · 처음 0.5초 테두리가 밝게
        float k = Mathf.Clamp01(shown / 0.25f);
        box.cg.alpha = k;
        box.r.anchoredPosition = new Vector2(0f, 236f - 30f * (1f - k) * (1f - k) * GameSettings.Motion);      // 1.1.9~ 「움직임 줄이기」면 제자리 페이드
        float glow = GameSettings.Flashes ? Mathf.Clamp01(1f - shown / 0.5f) : 0f;
        box.rim.color = Color.Lerp(new Color(Notice.Sky.r, Notice.Sky.g, Notice.Sky.b, 0.95f), Color.white, glow * 0.7f);
        RectTransform bar = box.bar.rectTransform;
        bar.localScale = new Vector3(Mathf.Clamp01(left / Duration), 1f, 1f);
        bool close = CloseText.Length > 0 && Input.GetKeyDown(KeyCode.Tab);
        if (left > 0f && !close) return;
        Prefs.SetInt("hint." + current, 1);         // 다 보여 준 뒤(또는 닫은 뒤)에 '봤음'
        current = null;
        box.r.gameObject.SetActive(false);
    }
}

public class HintRunner : MonoBehaviour
{
    void Update() => Hints.Tick();
}

// 게임 오버 화면: [R]로 같은 캐릭터 · 난이도로 바로 다시 시작 (1.0.4~ 재장전 키를 바꿨으면 그 키)
public class QuickRestart : MonoBehaviour
{
    // 1.0.4~: 결과 화면이 뜨고 1초 동안은 받지 않음 (재장전 키와 같은 [R] 을 누르던 중이면 결과를 보기도 전에 새 판이 시작됐음)
    const float Delay = 1f;
    float readyAt;

    void Start() => readyAt = Time.unscaledTime + Delay;

    void Update()
    {
        if (Time.unscaledTime < readyAt || !KeyBindings.Down(GameAction.Reload) || SettingsUI.IsOpen || LeaderboardUI.IsOpen || BuildView.IsOpen || RunHistoryUI.IsOpen) return;     // 1.0.4~ 바꾼 재장전 키 · 뜬 뒤 1초 · 1.1.5~ 순위 창이 떠 있으면 받지 않음
        RunLog.NextKind = "quick";          // 1.2~ 판 기록 retry_kind
        SceneFlow.Go("GameScene");
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
