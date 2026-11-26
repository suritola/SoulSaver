using System.Globalization;
using TMPro;
using UnityEngine;

// 2.0.6~ 영웅 각성 (docs/plans/2026-11-20-big-update/plan.md B): 그 영웅으로 조건을 이루면 열리는 영구 진화 5단계 — 포인트로 사지 않음 · 새 조작 키 없음
//   각 단계는 따로 열리고 열린 것은 그 영웅에게 늘 켜짐 (판 밖 진화 · 장착 없음)
//   1 이야기 모드 클리어 → 판 시작 레벨업 카드 +1 (이야기 판, 숙련과 합산)
//   2 이야기 모드 보스 네 종 처치 → 필살기를 쓰면 체력 8% 회복
//   3 필살기 300번 → 판 시작 때 필살기 게이지 절반
//   4 무한 모드 15분 버팀 → 체력 40% 이하에서 받는 피해 -20%
//   5 숙련 최대 → 판 시작부터 1차 무기 진화
//   효과는 지금 영웅(CharacterData.Selected)으로 그때그때 읽음 — 영혼 릴레이 교대도 따로 할 일 없음 · 꺼지는 판은 SoulLegacy.On 과 같음
public static class HeroAwaken
{
    public const int Max = 5;
    const float UltHeal = 0.08f, LowBelow = 0.4f, LowCut = 0.2f, StartGauge = 0.5f;
    public const int UltGoal = 300, EndlessGoal = 900;

    static int Life(string k, CharacterId id) => RunCounters.Life(RunCounters.Hero(k, id));

    public static void Progress(CharacterId id, int tier, out int now, out int goal)
    {
        switch (tier)
        {
            case 0: now = Mastery.Level(id) >= 1 || Life("clear", id) > 0 ? 1 : 0; goal = 1; break;
            case 1: now = RunCounters.BitCount(RunCounters.Hero("bosskinds", id)); goal = 4; break;
            case 2: now = Life("ult", id); goal = UltGoal; break;
            case 3: now = Life("endless.best", id); goal = EndlessGoal; break;
            default: now = Mastery.Level(id); goal = Mastery.Max; break;
        }
        now = Mathf.Min(now, goal);
    }

    public static bool Has(CharacterId id, int tier) { Progress(id, tier, out int now, out int goal); return now >= goal; }
    public static int Level(CharacterId id) { int n = 0; for (int t = 0; t < Max; t++) if (Has(id, t)) n++; return n; }
    static bool Live(int tier) => SoulLegacy.On && Has(CharacterData.Selected, tier);

    public static int FreePicks(CharacterId id) => SoulLegacy.On && Has(id, 0) ? 1 : 0;
    public static void OnUlt() { if (Live(1)) { PlayerController p = Hostile.Player; if (p != null && !p.IsDying) p.PlayerHealth = Mathf.Min(p.PlayerMaxHealth, p.PlayerHealth + p.PlayerMaxHealth * UltHeal); } }
    public static float StartGaugeFrac => Live(2) ? StartGauge : 0f;
    public static float AdjustTaken(float hpFrac, float taken) => Live(3) && hpFrac <= LowBelow ? taken * (1f - LowCut) : taken;
    public static bool StartEvo(CharacterId id) => SoulLegacy.On && Has(id, 4) && !ModeRun.Reflux && !ModeRun.Relay;      // 역류는 진화가 없음 · 2.0.7~ 릴레이는 빼기 (첫 주자 몫 시작 진화가 교대 때 다음 주자에게 넘어가던 것)

    // ================================================================= 창 (영웅 상세 「각성」 버튼)
    static GameObject open;
    static Color Gold => UiRules.Legacy ? new Color(0.96f, 0.83f, 0.47f) : UiTokens.Text1;
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);

    public static string Label(CharacterId id) => Loc.T("awaken.btn").Replace("{0}", Level(id).ToString()).Replace("{1}", Max.ToString());

    public static void Open(Transform root, CharacterId id)
    {
        if (open != null) return;
        RectTransform win = UIKit.Modal(root, "HeroAwaken", new Vector2(1080f, 760f), out open);
        UIKit.EscCancel(open, Close);
        TMP_Text title = UIKit.Text(win, "", 44f, Gold, new Vector2(0f, 330f), new Vector2(760f, 60f));
        title.text = Loc.T("awaken.title").Replace("{0}", Loc.T(CharacterData.Def(id).name));
        UIKit.MakeButton(win, "닫기", new Vector2(460f, 330f), new Vector2(130f, 56f), Close, 22f);
        UIKit.Text(win, "awaken.sub", 22f, Dim, new Vector2(0f, 272f), new Vector2(1000f, 32f));
        for (int t = 0; t < Max; t++)
        {
            Progress(id, t, out int now, out int goal);
            bool on = now >= goal;
            string head = (on ? "<color=#9CFF9C>✓</color> " : "<color=#6E6758>·</color> ") + "<color=" + (on ? "#F5D478" : "#8C8472") + ">" + Loc.T("awaken.tier").Replace("{0}", (t + 1).ToString()) + "</color>  " + Loc.T("awaken.e" + (t + 1));
            string cond = Loc.T("awaken.c" + (t + 1)) + (goal > 1 ? "  (" + Shown(t, now) + " / " + Shown(t, goal) + ")" : "");
            TMP_Text row = UIKit.Text(win, "", 26f, Parch, new Vector2(0f, 180f - t * 100f), new Vector2(980f, 90f), TextAlignmentOptions.Left);
            row.enableWordWrapping = true;
            row.fontSizeMin = 16f;
            row.text = head + "\n<size=80%><color=" + (on ? "#7FE08A" : "#A89C86") + ">" + Loc.T(on ? "legacy.open" : "legacy.cond") + " · " + cond + "</color></size>";
        }
    }

    // 무한 버팀은 초 → 분:초 · 나머지는 수
    static string Shown(int tier, int v) => tier == 3 ? EndlessMode.Clock(v) : v.ToString("N0", CultureInfo.InvariantCulture);

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }
}
