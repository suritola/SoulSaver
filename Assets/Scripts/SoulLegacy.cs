using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 2.0.6~ 영혼 제단 · 유산 (docs/plans/2026-11-20-big-update/plan.md A): 조건을 이루면 열리는 특수 능력 — 포인트로 사지 않음 · 새 조작 키 없음 (모두 저절로 발동)
//   열린 유산 중 최대 3개를 장착 (Prefs legacy.eq = 비트) · 모든 영웅 · 모든 모드
//   체험판 · 페스티벌 · 튜토리얼 · 자동 봇 · 배치(밸런스 측정)에서는 꺼짐 (On)
//   효과 자리: PlayerController.TryHit (AdjustTaken · TryEndure) · bosss.TakeDamage (BossDamageMul) · bosss.Die (OnBossDown)
//             LevelShop 레벨업 (OnLevelUp) · 처치 (EnermyController.Killed) · 판 시작 카드 (StartPicks, StageManager)
public static class SoulLegacy
{
    public enum Id { Blood, Endure, Hunter, Harvest, Breath, Pioneer, Echo, Rush }
    const int Count = 8;
    public const int Slots = 3;
    const string EquipKey = "legacy.eq";

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

    // ================================================================= 열리는 조건 (지금 값 · 목표)
    static int HeroesCleared()
    {
        int n = 0;
        for (int i = 0; i < CharacterData.All.Length; i++)
            if (Mastery.Level((CharacterId)i) >= 1 || RunCounters.Life(RunCounters.Hero("clear", (CharacterId)i)) > 0) n++;
        return n;
    }

    static int MasteredHeroes()
    {
        int n = 0;
        for (int i = 0; i < CharacterData.All.Length; i++) if (Mastery.Level((CharacterId)i) >= Mastery.Max) n++;
        return n;
    }

    static int B(bool b) => b ? 1 : 0;

    public static void Progress(Id id, out int now, out int goal)
    {
        switch (id)
        {
            case Id.Blood: now = Prefs.GetInt(SteamManager.TotalKillsKey, 0); goal = 20000; break;
            case Id.Endure: now = B(GameMode.HasCleared(Difficulty.Hard)); goal = 1; break;
            case Id.Hunter: now = HeroesCleared(); goal = 5; break;
            case Id.Harvest: now = B(EndlessMode.EverCleared); goal = 1; break;
            case Id.Breath: now = B(GameMode.HasCleared(Difficulty.Easy)); goal = 1; break;
            case Id.Pioneer: now = MasteredHeroes(); goal = 1; break;
            case Id.Echo: now = B(RefluxRun.EverClearedReflux); goal = 1; break;
            default: now = B(BossRushRun.EverCleared); goal = 1; break;
        }
        now = Mathf.Min(now, goal);
    }

    public static bool Unlocked(Id id) { Progress(id, out int now, out int goal); return now >= goal; }
    static int Mask => Prefs.GetInt(EquipKey, 0);
    public static bool Equipped(Id id) => (Mask & (1 << (int)id)) != 0 && Unlocked(id);
    static bool Live(Id id) => On && Equipped(id);

    static int EquippedCount()
    {
        int n = 0;
        for (int i = 0; i < Count; i++) if (Equipped((Id)i)) n++;
        return n;
    }

    static bool Toggle(Id id)
    {
        int m = Mask, bit = 1 << (int)id;
        if ((m & bit) != 0) m &= ~bit;
        else if (Unlocked(id) && EquippedCount() < Slots) m |= bit;
        else return false;
        Prefs.SetInt(EquipKey, m);
        Prefs.Save();
        return true;
    }

    // ================================================================= 효과
    const float BloodChance = 0.03f, BloodHeal = 0.02f, EndureHp = 0.3f, HunterMul = 1.15f, BreathHeal = 0.03f, EchoBelow = 0.3f, EchoCut = 0.25f, RushHeal = 0.2f;
    static readonly System.Random rng = new System.Random();     // 게임 난수(일일 도전 시드)는 건드리지 않게 따로
    static bool endureUsed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        EnermyController.Killed += OnKill;
        PlayerController.UltUsed += HeroAwaken.OnUlt;
        SceneManager.sceneLoaded += (s, m) => { if (s.name == "GameScene") endureUsed = false; };
    }

    static void Heal(PlayerController p, float frac)
    {
        if (p == null || p.IsDying || p.PlayerHealth <= 0f) return;
        p.PlayerHealth = Mathf.Min(p.PlayerMaxHealth, p.PlayerHealth + p.PlayerMaxHealth * frac);
    }

    static void OnKill(Vector3 at)
    {
        if (!Live(Id.Blood) || rng.NextDouble() >= BloodChance) return;
        Heal(Hostile.Player, BloodHeal);
    }

    // 받는 피해 (TryHit 의 감소들 뒤 · 한 방 상한 앞): 역류의 잔향 · 영웅 각성 4단계
    public static float AdjustTaken(PlayerController p, float taken)
    {
        if (p == null) return taken;
        float frac = p.PlayerHealth / Mathf.Max(1f, p.PlayerMaxHealth);
        if (Live(Id.Echo) && frac <= EchoBelow) taken *= 1f - EchoCut;
        return HeroAwaken.AdjustTaken(frac, taken);
    }

    // 쓰러질 피해 (불사의 맹세 뒤): 불굴 — 판마다 한 번 체력 30% 로 · 2초 무적은 부르는 쪽
    public static bool TryEndure()
    {
        if (endureUsed || !Live(Id.Endure)) return false;
        endureUsed = true;
        Notice.Toast(NoticeIcon.Up, Loc.T("legacy.endure.proc"), 2.5f);
        return true;
    }
    public static float EndureHealth(float max) => Mathf.Max(1f, Mathf.Round(max * EndureHp));

    public static float BossDamageMul => Live(Id.Hunter) ? HunterMul : 1f;

    // 보스전을 끝내는 마지막 한 마리 (bosss.Die)
    public static void OnBossDown()
    {
        PlayerController p = Hostile.Player;
        if (Live(Id.Harvest))
        {
            SkillGauge g = Object.FindFirstObjectByType<SkillGauge>();
            if (g != null) g.AddSkillPoint(g.MaxSkillPoint);
        }
        if (Live(Id.Rush)) Heal(p, RushHeal);
    }

    public static void OnLevelUp(PlayerController p) { if (Live(Id.Breath)) Heal(p, BreathHeal); }

    public static int StartPicks => Live(Id.Pioneer) ? 1 : 0;

    // ================================================================= 창 (메인 메뉴 「영혼 제단」)
    static GameObject open;
    static readonly TMP_Text[] info = new TMP_Text[Count];
    static readonly Button[] btn = new Button[Count];
    static TMP_Text slotText;
    static Color Gold => UiRules.Legacy ? new Color(0.96f, 0.83f, 0.47f) : UiTokens.Text1;
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static string Key(Id id) => "legacy." + id.ToString().ToLowerInvariant();

    public static void Open(Transform root)
    {
        if (open != null) return;
        RectTransform win = UIKit.Modal(root, "SoulAltar", new Vector2(1240f, 900f), out open);
        UIKit.EscCancel(open, Close);
        UIKit.Text(win, "meta.title", 46f, Gold, new Vector2(0f, 395f), new Vector2(700f, 64f));
        UIKit.MakeButton(win, "닫기", new Vector2(530f, 395f), new Vector2(150f, 58f), Close, 22f);
        UIKit.Text(win, "meta.sub", 22f, Dim, new Vector2(0f, 340f), new Vector2(1140f, 32f));
        slotText = UIKit.Text(win, "", 26f, Gold, new Vector2(0f, 300f), new Vector2(700f, 36f));
        for (int i = 0; i < Count; i++)
        {
            Id id = (Id)i;
            float y = 235f - i * 82f;
            info[i] = UIKit.Text(win, "", 22f, Parch, new Vector2(-110f, y), new Vector2(940f, 76f), TextAlignmentOptions.Left);
            info[i].enableWordWrapping = true;
            info[i].fontSizeMin = 15f;
            btn[i] = UIKit.MakeButton(win, "", new Vector2(480f, y), new Vector2(200f, 60f), () => { if (Toggle(id)) { Hostile.Play("chime", 0.6f, 1.25f); Refresh(); } }, 22f);
        }
        Refresh();
    }

    static void Refresh()
    {
        if (open == null) return;
        int used = EquippedCount();
        slotText.text = Loc.T("legacy.slots").Replace("{0}", used.ToString()).Replace("{1}", Slots.ToString());
        for (int i = 0; i < Count; i++)
        {
            Id id = (Id)i;
            Progress(id, out int now, out int goal);
            bool unlocked = now >= goal, eq = Equipped(id);
            string head = (eq ? "<color=#9CFF9C>✓</color> " : "") + "<color=" + (unlocked ? "#F5D478" : "#8C8472") + ">" + Loc.T(Key(id)) + "</color>  " + Loc.T(Key(id) + ".d");
            string cond = Loc.T(Key(id) + ".c") + (goal > 1 ? "  (" + now.ToString("N0", CultureInfo.InvariantCulture) + " / " + goal.ToString("N0", CultureInfo.InvariantCulture) + ")" : "");
            info[i].text = head + "\n<size=85%><color=" + (unlocked ? "#7FE08A" : "#A89C86") + ">" + (unlocked ? Loc.T("legacy.open") : Loc.T("legacy.cond")) + " · " + cond + "</color></size>";
            TMP_Text bt = btn[i].GetComponentInChildren<TMP_Text>(true);
            if (bt != null) bt.text = !unlocked ? Loc.T("잠김") : eq ? Loc.T("legacy.unequip") : Loc.T("legacy.equip");
            btn[i].interactable = unlocked && (eq || used < Slots);
        }
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }
}
