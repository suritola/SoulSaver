using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.1.9~ 내 최고 기록 (편의성 2차 11) · 결과 복사 (편의성 3차 10)
// 최고 기록: 이야기 모드 쉬움 · 보통 · 어려움만, 영웅 × 난이도마다 Prefs "best.<영웅>.<난이도>.level|kills|secs|clearsecs"
//   secs = 쓰러진 판의 가장 긴 생존 시간 · clearsecs = 클리어 판의 가장 짧은 시간 · level · kills 는 결과와 무관하게 최대
//   결과 화면을 그리는 쪽(MenuExtras.InstallGameOver)과 판 기록을 쓰는 쪽(RunStats.Boot)의 sceneLoaded 순서가 정해져 있지 않아
//   CommitBest 를 두 곳에서 부르되 한 판에 한 번만 · 비교용 옛값은 Prev 에 남김
public static class RunRecords
{
    public struct Best { public int level, kills, secs, clearsecs; public bool Any => level > 0 || kills > 0 || secs > 0 || clearsecs > 0; }

    public static Best Prev;                 // 이번 판 전의 최고 (결과 화면 비교용)
    static bool committed, eligible;
    static bool newLevel, newKills, newTime;
    public static bool AnyNew => eligible && Prev.Any && (newLevel || newKills || newTime);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (s.name == "GameScene") { committed = eligible = false; newLevel = newKills = newTime = false; } };
    }

    static string Key(CharacterId c, Difficulty d) => "best." + (int)c + "." + (int)d + ".";

    public static Best Load(CharacterId c, Difficulty d)
    {
        string k = Key(c, d);
        return new Best { level = Prefs.GetInt(k + "level", 0), kills = Prefs.GetInt(k + "kills", 0), secs = Prefs.GetInt(k + "secs", 0), clearsecs = Prefs.GetInt(k + "clearsecs", 0) };
    }

    static bool Eligible => !GameMode.IsEndless && !DailyChallenge.Active && !TutorialRun.Active && !Demo.On
                            && GameMode.Current != Difficulty.Endless && !RunStats.GaveUp;

    // 결과 화면에 왔을 때 한 번 (쓰러짐 · 클리어 판)
    public static void CommitBest()
    {
        if (committed) return;
        committed = true;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameOver" || !Eligible || GameInput.Auto || Application.isBatchMode) return;
        eligible = true;
        CharacterId c = CharacterData.Selected;
        Difficulty d = GameMode.Current;
        Prev = Load(c, d);
        string k = Key(c, d);
        int secs = Mathf.RoundToInt(RunStats.Seconds);
        newLevel = RunStats.Level > Prev.level;
        newKills = RunStats.Kills > Prev.kills;
        if (newLevel) Prefs.SetInt(k + "level", RunStats.Level);
        if (newKills) Prefs.SetInt(k + "kills", RunStats.Kills);
        if (RunStats.Cleared)
        {
            newTime = Prev.clearsecs <= 0 || secs < Prev.clearsecs;
            if (newTime) Prefs.SetInt(k + "clearsecs", secs);
        }
        else
        {
            newTime = secs > Prev.secs;
            if (newTime) Prefs.SetInt(k + "secs", secs);
        }
        Prefs.Save();
    }

    // 결과 글 둘째 줄(시간 · 레벨 · 처치)에 최고 기록 괄호 또는 「새 기록!」 — 첫 판(옛 기록 없음)은 그대로
    public static string WithBest(string summary)
    {
        if (!eligible || !Prev.Any) return summary;
        string[] lines = summary.Split('\n');
        if (lines.Length < 2) return summary;
        string dim = "<size=80%><color=#A89C86>", end = "</color></size>";
        string rec = " <size=80%><color=#F5D478>" + Loc.T("weight.newrecord") + "</color></size>";
        string Paren(string ko, string v) => " " + dim + "(" + Loc.T(ko).Replace("{0}", v) + ")" + end;
        string time = Loc.T("플레이 시간 ") + EndlessMode.Clock(RunStats.Seconds);
        if (RunStats.Cleared)
            time += newTime ? rec : Paren("최단 클리어 {0}", EndlessMode.Clock(Prev.clearsecs));
        else
            time += newTime ? rec : Prev.secs > 0 ? Paren("최고 {0}", EndlessMode.Clock(Prev.secs)) : "";
        string level = Loc.T("레벨 ") + RunStats.Level + (newLevel ? rec : Paren("최고 {0}", Prev.level.ToString()));
        string kills = Loc.T("처치 ") + RunStats.Kills + (newKills ? rec : Paren("최고 {0}", Prev.kills.ToString("N0")));
        lines[1] = time + "   " + level + "   " + kills;
        return string.Join("\n", lines);
    }

    // 새 기록이면 결과 화면이 뜬 0.6초 뒤 종소리 한 번 (결과 씬에는 SharedFx 가 없어 캔버스에 하나 붙임)
    public static void PlayNewRecordSound(Transform canvas)
    {
        if (!AnyNew || canvas == null || GameInput.Auto) return;
        GameObject a = new GameObject("RecordAudio");
        a.transform.SetParent(canvas, false);
        SpecialFeedback fx = a.AddComponent<SpecialFeedback>();
        fx.Init(UIKit.Font, UIKit.FontMaterial);
        a.AddComponent<RecordChime>().fx = fx;
    }

    class RecordChime : MonoBehaviour
    {
        public SpecialFeedback fx;
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            if (fx != null) fx.Play("chime", 0.6f, 1.3f);
        }
    }

    // ================================================================= 결과 복사 (3차 10)
    static readonly Regex Tags = new Regex("<.*?>");
    public static string Plain(string s) => Tags.Replace(s ?? "", "");

    // 「Soul Saver v1.1.9 · 거너 · 보통 · 클리어 · 32:10 · Lv 48 · 처치 2,310」 + 무기 진화 · 카드 · 특수 능력 · 스킬 진화 · 준 피해
    public static string ShareText()
    {
        string result = Loc.T(RunStats.Cleared ? "클리어" : RunStats.GaveUp ? "포기" : "쓰러짐");
        System.Text.StringBuilder b = new System.Text.StringBuilder();
        b.Append("Soul Saver v").Append(Application.version).Append(Plain(Demo.Label))
         .Append(" · ").Append(RunStats.Character).Append(" · ").Append(Plain(RunStats.Difficulty))
         .Append(" · ").Append(result).Append(" · ").Append(EndlessMode.Clock(RunStats.Seconds))
         .Append(" · Lv ").Append(RunStats.Level).Append(" · ").Append(Loc.T("처치 ")).Append(RunStats.Kills.ToString("N0"));
        string evo = Loc.T("기본 무기");
        foreach (KeyValuePair<string, float> e in RunStats.Evolutions) evo += " → " + e.Key + " (" + EndlessMode.Clock(e.Value) + ")";
        b.Append('\n').Append(Loc.T("무기 진화")).Append(": ").Append(evo);
        if (RunStats.Cards.Count > 0) b.Append('\n').Append(Loc.T("레벨업 카드")).Append(": ").Append(Plain(string.Join(", ", RunStats.Cards)));
        if (RunStats.Abilities.Count > 0) b.Append('\n').Append(Loc.T("특수 능력")).Append(": ").Append(Plain(string.Join(", ", RunStats.Abilities)));
        if (RunStats.SkillEvos.Count > 0)
        {
            List<string> se = new List<string>();
            foreach (KeyValuePair<string, float> kv in RunStats.SkillEvos) se.Add(kv.Key);
            b.Append('\n').Append(Loc.T("스킬 진화")).Append(": ").Append(string.Join(", ", se));
        }
        if (RunStats.DamageDealt.Count > 0)
        {
            float total = 0f;
            foreach (float v in RunStats.DamageDealt.Values) total += v;
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, float> kv in RunStats.Top(RunStats.DamageDealt, 5))
                parts.Add(Plain(RunStats.SourceName(kv.Key)) + " " + (kv.Value / Mathf.Max(1f, total) * 100f).ToString("0") + "%");
            b.Append('\n').Append(Loc.T("준 피해")).Append(": ").Append(string.Join(" · ", parts));
        }
        return b.ToString();
    }

    // 결과 화면 오른쪽 아래 구석 (-60, 40) 220×56 — 아래 가운데 글 · 획득 포인트 글보다 오른쪽, 오른쪽 피해 패널보다 아래
    public static void InstallCopyButton(Transform canvas, System.Func<string> text)
    {
        if (canvas == null) return;
        Button b = CopyButton(canvas, "결과 복사", new Vector2(220f, 56f), text, 22f);
        RectTransform r = (RectTransform)b.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
        r.anchoredPosition = new Vector2(-60f, 40f);
        b.name = "CopyResultButton";
        b.navigation = new Navigation { mode = Navigation.Mode.None };      // 결과 화면 버튼 탐색 순서(가운데 열)에는 넣지 않음
    }

    // 누르면 클립보드에 · 1.5초 「복사했습니다」 뒤 원래 글자로
    public static Button CopyButton(Transform parent, string labelKo, Vector2 size, System.Func<string> text, float fontSize)
    {
        TMP_Text label = null;
        Button b = null;
        b = UIKit.MakeButton(parent, labelKo, Vector2.zero, size, () =>
        {
            string s = null;
            try { s = text?.Invoke(); } catch (System.Exception e) { Debug.LogWarning("[RunRecords] 복사 글 만들기 실패: " + e.Message); }
            if (string.IsNullOrEmpty(s)) return;
            GUIUtility.systemCopyBuffer = s;
            if (label != null)
            {
                label.text = Loc.T("복사했습니다");
                UIKit.Remember(label, "복사했습니다");
                b.gameObject.AddComponent<CopyRevert>().Arm(label, labelKo);
            }
            if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }, fontSize);
        label = b.GetComponentInChildren<TMP_Text>();
        if (label != null) label.fontSizeMin = 14f;
        return b;
    }

    class CopyRevert : MonoBehaviour
    {
        TMP_Text label;
        string ko;
        float until;
        public void Arm(TMP_Text t, string k)
        {
            foreach (CopyRevert other in GetComponents<CopyRevert>()) if (other != this) Destroy(other);
            label = t; ko = k; until = Time.unscaledTime + 1.5f;
        }
        void Update()
        {
            if (Time.unscaledTime < until) return;
            if (label != null) { label.text = Loc.T(ko); UIKit.Remember(label, ko); }
            Destroy(this);
        }
    }
}
