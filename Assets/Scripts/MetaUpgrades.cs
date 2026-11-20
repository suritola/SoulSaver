using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 2.0.6~ 영혼 제단 (docs/plans/2026-11-20-big-update/plan.md A): 포인트로 사는 영구 강화 — 모든 영웅 · 모든 모드
//   공격력 · 최대 체력 · 이동 속도 · 경험치 · 포인트 · 영혼 조각, 항목마다 10단계 · 단계 n(0부터) 비용 800 × 1.5ⁿ (100 단위)
//   저장 meta.<항목> = 단계 · 체험판 · 페스티벌 · 튜토리얼 · 자동 봇 · 배치(밸런스 측정)는 강화 0 으로
//   적용: 체력 · 공격 · 이동은 PlayerController.Start 한 번 (곱이라 영웅 배율 · 이어하기 기록 · 릴레이 교대와 순서 무관)
//         경험치 LevelShop.GainExp · 영혼 조각 SoulShards.Add · 포인트 Point.RegisterMul
public static class MetaUpgrades
{
    public enum Stat { Attack, Health, Speed, Exp, Points, Shards }
    public const int Max = 10;
    static readonly float[] Step = { 0.03f, 0.04f, 0.02f, 0.03f, 0.05f, 0.04f };
    static readonly string[] Names = { "meta.atk", "meta.hp", "meta.spd", "meta.exp", "meta.pts", "meta.shard" };
    const int Count = 6;

    static string Key(Stat s) => "meta." + (int)s;

    public static bool On
    {
        get
        {
#if SOULSAVER_FESTIVAL
            return false;
#else
            return !Demo.On && !TutorialRun.Active && !GameInput.Auto && !GameInput.TrailerRunning && !Application.isBatchMode;
#endif
        }
    }

    public static int Level(Stat s) => Mathf.Clamp(Prefs.GetInt(Key(s), 0), 0, Max);
    public static float Bonus(Stat s, int level) => Step[(int)s] * level;
    public static float Mul(Stat s) => On ? 1f + Bonus(s, Level(s)) : 1f;
    public static int Cost(int level) => Mathf.RoundToInt(800f * Mathf.Pow(1.5f, level) / 100f) * 100;

    public static bool Buy(Stat s)
    {
        int lv = Level(s);
        if (lv >= Max || Demo.On || !CharacterData.SpendPoints(Cost(lv))) return false;     // SpendPoints 가 저장까지
        Prefs.SetInt(Key(s), lv + 1);
        Prefs.Save();
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() => Point.RegisterMul("meta", () => Mul(Stat.Points));

    // PlayerController.Start: 영웅 배율을 곱한 뒤 한 번
    public static void ApplyStart(PlayerController p)
    {
        if (p == null || !On) return;
        CharacterId hero = CharacterData.Selected;
        p.damage *= Mul(Stat.Attack) * HeroAwaken.AtkMul(hero);       // + 영웅 각성 1 · 3단계
        p.PlayerMaxHealth = Mathf.Round(p.PlayerMaxHealth * Mul(Stat.Health) * HeroAwaken.HpMul(hero));
        p.PlayerHealth = p.PlayerMaxHealth;
        float spd = Mul(Stat.Speed);
        p.speed *= spd;
        p.BaseMoveSpeed *= spd;      // 속도 상한 기준도 같이 (강화한 만큼은 상한에 걸리지 않게)
    }

    // ================================================================= 창 (메인 메뉴 「영혼 제단」)
    static GameObject open;
    static readonly TMP_Text[] lvText = new TMP_Text[Count], effText = new TMP_Text[Count];
    static readonly Button[] buyBtn = new Button[Count];
    static TMP_Text pointsText;
    static Color Gold => UiRules.Legacy ? new Color(0.96f, 0.83f, 0.47f) : UiTokens.Text1;
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);

    public static void Open(Transform root)
    {
        if (open != null) return;
        RectTransform win = UIKit.Modal(root, "MetaAltar", new Vector2(1100f, 860f), out open);
        UIKit.EscCancel(open, Close);
        UIKit.Text(win, "meta.title", 46f, Gold, new Vector2(0f, 380f), new Vector2(700f, 64f));
        UIKit.MakeButton(win, "닫기", new Vector2(460f, 380f), new Vector2(150f, 58f), Close, 22f);
        UIKit.Text(win, "meta.sub", 22f, Dim, new Vector2(0f, 320f), new Vector2(1000f, 34f));
        pointsText = UIKit.Text(win, "", 28f, Gold, new Vector2(0f, 270f), new Vector2(700f, 40f));
        for (int i = 0; i < Count; i++)
        {
            Stat s = (Stat)i;
            float y = 180f - i * 100f;
            RectTransform row = UIKit.Rect("Row" + i, win, new Vector2(0f, y), new Vector2(1000f, 88f));
            UIKit.Text(row, Names[i], 30f, Parch, new Vector2(-330f, 12f), new Vector2(320f, 40f), TextAlignmentOptions.Left);
            lvText[i] = UIKit.Text(row, "", 22f, Dim, new Vector2(-330f, -24f), new Vector2(320f, 30f), TextAlignmentOptions.Left);
            effText[i] = UIKit.Text(row, "", 26f, Parch, new Vector2(30f, 0f), new Vector2(360f, 40f));
            buyBtn[i] = UIKit.MakeButton(row, "", new Vector2(370f, 0f), new Vector2(240f, 66f), () => { if (Buy(s)) { Hostile.Play("chime", 0.7f, 1.2f); Refresh(); } }, 24f);
        }
        Refresh();
    }

    static string Pct(float f) => "+" + Mathf.RoundToInt(f * 100f).ToString(CultureInfo.InvariantCulture) + "%";

    static void Refresh()
    {
        if (open == null) return;
        int pts = CharacterData.Points;
        pointsText.text = Loc.T("meta.points").Replace("{0}", pts.ToString("N0", CultureInfo.InvariantCulture));
        for (int i = 0; i < Count; i++)
        {
            Stat s = (Stat)i;
            int lv = Level(s);
            lvText[i].text = Loc.T("meta.level").Replace("{0}", lv.ToString()).Replace("{1}", Max.ToString());
            effText[i].text = lv >= Max ? Pct(Bonus(s, lv)) : Pct(Bonus(s, lv)) + "  →  <color=#7FE08A>" + Pct(Bonus(s, lv + 1)) + "</color>";
            int cost = lv < Max ? Cost(lv) : 0;
            TMP_Text bt = buyBtn[i].GetComponentInChildren<TMP_Text>(true);
            if (bt != null) bt.text = lv >= Max ? Loc.T("meta.max") : cost.ToString("N0", CultureInfo.InvariantCulture) + "P";
            buyBtn[i].interactable = lv < Max && pts >= cost && !Demo.On;
        }
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }
}
