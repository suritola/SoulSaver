using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 2.0.6~ 영웅 각성 (docs/plans/2026-11-20-big-update/plan.md B): 영웅마다 판 밖에서 포인트로 올리는 영구 진화 5단계
//   1 공격력 +5% · 2 판 시작 레벨업 카드 +1 (숙련과 합산 · 이야기 판) · 3 최대 체력 +10% · 4 필살기 게이지 충전 +15% · 5 판 시작부터 1차 무기 진화
//   저장 hero.awaken.<영웅번호> · 강화가 꺼지는 판(MetaUpgrades.On 거짓)에서는 효과 없음
//   영혼 릴레이: 교대 때 앞 영웅 몫을 나누고 새 영웅 몫을 곱함 (RelaySwap)
public static class HeroAwaken
{
    public const int Max = 5;
    static readonly int[] Costs = { 3000, 6000, 12000, 20000, 35000 };
    const float Atk = 1.05f, Hp = 1.10f, Ult = 1.15f;

    static string Key(CharacterId id) => "hero.awaken." + (int)id;
    public static int Level(CharacterId id) => Mathf.Clamp(Prefs.GetInt(Key(id), 0), 0, Max);
    static int Live(CharacterId id) => MetaUpgrades.On ? Level(id) : 0;
    public static int Cost(int level) => level < Max ? Costs[level] : 0;

    public static float AtkMul(CharacterId id) => Live(id) >= 1 ? Atk : 1f;
    public static int FreePicks(CharacterId id) => Live(id) >= 2 ? 1 : 0;
    public static float HpMul(CharacterId id) => Live(id) >= 3 ? Hp : 1f;
    public static float UltMul(CharacterId id) => Live(id) >= 4 ? Ult : 1f;
    public static bool StartEvo(CharacterId id) => Live(id) >= 5 && !ModeRun.Reflux;      // 역류는 진화가 없음

    public static bool Buy(CharacterId id)
    {
        int lv = Level(id);
        if (lv >= Max || Demo.On || !CharacterData.IsUnlocked(id) || !CharacterData.SpendPoints(Cost(lv))) return false;
        Prefs.SetInt(Key(id), lv + 1);
        Prefs.Save();
        return true;
    }

    // ================================================================= 창 (영웅 상세 「각성」 버튼)
    static GameObject open;
    static readonly TMP_Text[] rows = new TMP_Text[Max];
    static TMP_Text pointsText;
    static Button buy;
    static System.Action changed;
    static Color Gold => UiRules.Legacy ? new Color(0.96f, 0.83f, 0.47f) : UiTokens.Text1;
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);
    static readonly string[] Effects = { "awaken.e1", "awaken.e2", "awaken.e3", "awaken.e4", "awaken.e5" };

    public static string Label(CharacterId id) => Loc.T("awaken.btn").Replace("{0}", Level(id).ToString()).Replace("{1}", Max.ToString());

    public static void Open(Transform root, CharacterId id, System.Action onChanged)
    {
        if (open != null) return;
        changed = onChanged;
        RectTransform win = UIKit.Modal(root, "HeroAwaken", new Vector2(960f, 760f), out open);
        UIKit.EscCancel(open, Close);
        TMP_Text title = UIKit.Text(win, "", 44f, Gold, new Vector2(0f, 330f), new Vector2(700f, 60f));
        title.text = Loc.T("awaken.title").Replace("{0}", Loc.T(CharacterData.Def(id).name));
        UIKit.MakeButton(win, "닫기", new Vector2(400f, 330f), new Vector2(130f, 56f), Close, 22f);
        UIKit.Text(win, "awaken.sub", 22f, Dim, new Vector2(0f, 272f), new Vector2(880f, 32f));
        pointsText = UIKit.Text(win, "", 26f, Gold, new Vector2(0f, 225f), new Vector2(700f, 36f));
        for (int i = 0; i < Max; i++)
        {
            rows[i] = UIKit.Text(win, "", 26f, Parch, new Vector2(0f, 150f - i * 74f), new Vector2(860f, 60f), TextAlignmentOptions.Left);
            rows[i].enableWordWrapping = true;
        }
        buy = UIKit.MakeButton(win, "", new Vector2(0f, -300f), new Vector2(360f, 72f), () =>
        {
            if (!Buy(id)) return;
            Hostile.Play("chime", 0.8f, 1.1f);
            Refresh(id);
            changed?.Invoke();
        }, 26f);
        Refresh(id);
    }

    static void Refresh(CharacterId id)
    {
        int lv = Level(id), pts = CharacterData.Points;
        pointsText.text = Loc.T("meta.points").Replace("{0}", pts.ToString("N0", CultureInfo.InvariantCulture));
        for (int i = 0; i < Max; i++)
        {
            string mark = i < lv ? "<color=#9CFF9C>✓</color> " : i == lv ? "<color=#F5D478>→</color> " : "<color=#6E6758>·</color> ";
            string cost = i < lv ? "" : "  <color=#A89C86>(" + Cost(i).ToString("N0", CultureInfo.InvariantCulture) + "P)</color>";
            rows[i].text = mark + Loc.T("awaken.tier").Replace("{0}", (i + 1).ToString()) + "  " + Loc.T(Effects[i]) + cost;
        }
        TMP_Text bt = buy.GetComponentInChildren<TMP_Text>(true);
        if (bt != null) bt.text = lv >= Max ? Loc.T("meta.max") : Loc.T("awaken.buy").Replace("{0}", Cost(lv).ToString("N0", CultureInfo.InvariantCulture));
        buy.interactable = lv < Max && pts >= Cost(lv) && !Demo.On;
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
        changed = null;
    }
}
