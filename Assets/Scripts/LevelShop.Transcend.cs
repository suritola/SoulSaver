using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 판 후반 보상 (1.1~): 초월 카드
// 최대 레벨(50 · 무한 모드 80, LevelCap)은 그대로 두고, 상한에 닿은 뒤 경험치 막대를 세 번 채울 때마다 (레벨은 오르지 않음, 로드맵 1-5 「3레벨마다」)
//   첫째 · 둘째 막대는 비우면서 「초월 충전 n / 3」만 알리고, 셋째 막대에서 창을 엶 (이어하기 기록은 tr9)
// 금테 「초월 카드」 세 장 중 하나를 고름 (보스 보상 · 보물 상자와 같은 ChoiceUI 창)
//   파괴: 모든 무기 피해 +10% (최대 5번) · 충전: 필살기 게이지 차는 속도 +25% (최대 3번) · 급소: 치명타 확률 +5%, 치명타 피해 +20% (최대 4번)
//   생명: 최대 체력 +25, 체력 모두 회복 (최대 4번) · 질풍: 공격 속도 +6% (최대 5번)
// 모든 카드가 최대 단계면 예전처럼 막대가 가득 찬 채 멈춤
// 고르면 막대를 비움 (복원은 RestoreLevels 가 저장한 막대 그대로 — 가득 찬 채 저장했으면 이어할 때 다시 고름), 이어하기 기록은 tr<번호>
public partial class LevelShop
{
    public const int TranscendCount = 5;
    static readonly string[] TranscendNames = { "초월: 파괴", "초월: 충전", "초월: 급소", "초월: 생명", "초월: 질풍" };
    static readonly string[] TranscendDescs =
    {
        "모든 무기 피해 +10% (기본 공격력 기준)",
        "필살기 게이지 차는 속도 +25%",
        "치명타 확률 +5%, 치명타 피해 +20%",
        "최대 체력 +25, 체력 모두 회복",
        "공격 속도 +6%",
    };
    // 중첩 상한 (카드마다)
    public static readonly int[] TranscendMax = { 5, 3, 4, 4, 5 };
    // Icons/choice_<이름> · 급소 · 질풍은 재화 그림(코인 · 영혼 조각)으로 보이지 않게 각성 카드와 같은 그림을 직접 (TranscendSprite)
    // 1.1~ 충전(필살기 게이지)도 영혼 조각 그림 대신 영혼 트리의 게이지 그림
    static readonly string[] TranscendIcons = { "might", "tree_u_gauge1", null, "life", null };
    static Sprite TranscendSprite(int k) => k == 2 ? SpecialAbilities.LvIcon(3) : k == 4 ? SpecialAbilities.StatIcon(2) : null;
    static readonly Color TranscendGold = new Color(1f, 0.84f, 0.32f);                                // 금테

    const float TranscendAttack = 0.10f;
    const float TranscendGauge = 0.25f;
    const float TranscendCrit = 0.05f, TranscendCritDamage = 0.2f;
    const float TranscendHp = 25f;
    const float TranscendRate = 0.06f;

    readonly int[] transcendLevel = new int[TranscendCount];

    // 막대를 몇 번 채워야 초월 카드 창이 열리는지 · 지난 카드 뒤 채운 막대 수 · 막대 하나 채움 기록 번호 (tr9)
    const int TranscendBarsPer = 3;
    const int TranscendBarCode = 9;
    int transcendBars;
    public int TranscendLevel(int k) => k >= 0 && k < TranscendCount ? transcendLevel[k] : 0;
    public int TranscendPicks
    {
        get
        {
            int s = 0;
            foreach (int n in transcendLevel) s += n;
            return s;
        }
    }
    public static string TranscendName(int k) => Loc.T(TranscendNames[Mathf.Clamp(k, 0, TranscendCount - 1)]);

    // 아직 더 올릴 수 있는 초월 카드가 있는지
    bool TranscendLeft
    {
        get
        {
            for (int k = 0; k < TranscendCount; k++) if (transcendLevel[k] < TranscendMax[k]) return true;
            return false;
        }
    }

    bool transcending;
    float transcendReadyAt = -1f;

    // LevelShop.Update 가 매 프레임 부름: 최대 레벨 + 막대가 가득 참 + 다른 창이 없을 때 초월 카드 창을 엶
    void UpdateTranscend()
    {
        if (transcending) return;
        if (bul == null) bul = FindFirstObjectByType<PlayerController>();
        if (bul == null || bul.PlayerHealth <= 0f || bul.needEXP <= 0f || bul.nowEXP < bul.needEXP || !AtLevelCap) { transcendReadyAt = -1f; return; }
        if (!TranscendLeft || TutorialRun.Active || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) return;
        if (IsOpen || PendingLevels > 0 || Time.timeScale != 1f || ChoiceUI.Open || ShopOpen || ESCmenu.IsOpen) return;
        StageManager sm = StageManager.Instance;
        if (sm != null && (sm.IsMenuOpen || !sm.Started)) return;
        // 마지막 보스를 쓰러뜨려 엔딩으로 가는 중이면 열지 않음 (엔딩 위에 창이 떠서 시간 배율을 되돌리거나 씬이 바뀌며 창이 열린 채 남던 문제)
        if (StoryDirector.EndingPlaying || (EndlessMode.Instance != null && EndlessMode.Instance.Cleared) || RunSave.FinalBossDown) { transcendReadyAt = -1f; return; }
        // 막대가 찬 순간 바로 덮지 않게 잠깐 뒤에
        if (transcendReadyAt < 0f) { transcendReadyAt = Time.unscaledTime + 0.6f; return; }
        if (Time.unscaledTime < transcendReadyAt) return;
        if (transcendBars < TranscendBarsPer - 1) { ChargeTranscend(); return; }
        StartCoroutine(TranscendPick());
    }

    // 셋째가 아닌 막대: 비우고 충전 단계만 알림
    void ChargeTranscend()
    {
        bul.nowEXP = Mathf.Max(0f, bul.nowEXP - bul.needEXP);
        transcendBars++;
        transcendReadyAt = -1f;
        RunSave.Record("tr" + TranscendBarCode);
        Juice.LevelUp(bul.transform.position);
        StageManager sm = StageManager.Instance;
        if (sm != null) sm.ShowBanner(Loc.T("초월 충전") + "  " + transcendBars + " / " + TranscendBarsPer, 1.5f);
    }

    IEnumerator TranscendPick()
    {
        transcending = true;
        // 아직 최대 단계가 아닌 카드 가운데 겹치지 않게 최대 세 장
        List<int> left = new List<int>();
        for (int k = 0; k < TranscendCount; k++) if (transcendLevel[k] < TranscendMax[k]) left.Add(k);
        List<int> ids = new List<int>();
        while (ids.Count < 3 && left.Count > 0)
        {
            int i = Random.Range(0, left.Count);
            ids.Add(left[i]);
            left.RemoveAt(i);
        }
        ChoiceUI.Option[] options = new ChoiceUI.Option[ids.Count];
        for (int i = 0; i < ids.Count; i++)
        {
            int k = ids[i];
            int n = transcendLevel[k];
            string desc = Loc.T(TranscendDescs[k]) + "\n<color=#FFD75A>" + Loc.T("초월 단계") + " " + n + " → " + (n + 1) + " / " + TranscendMax[k] + "</color>";
            Sprite sprite = TranscendSprite(k);
            options[i] = sprite != null ? new ChoiceUI.Option(TranscendNames[k], desc, TranscendGold, sprite)
                                        : new ChoiceUI.Option(TranscendNames[k], desc, TranscendGold, TranscendIcons[k] ?? "might");
        }
        int pick = 0;
        yield return ChoiceUI.Run("초월 카드", "최대 레벨 뒤 경험치 막대를 세 번 채웠습니다 · 하나를 고르세요", options, i => pick = i);
        int chosen = ids.Count > 0 ? ids[Mathf.Clamp(pick, 0, ids.Count - 1)] : -1;
        if (chosen >= 0)
        {
            if (bul != null) bul.nowEXP = Mathf.Max(0f, bul.nowEXP - bul.needEXP);
            ApplyTranscend(chosen);
            RunSave.Record("tr" + chosen);          // 판 중간 저장 (이어할 때 같은 초월 카드를 다시 적용)
            transcendBars = 0;
            if (bul != null) Juice.LevelUp(bul.transform.position);
            Hints.Show("transcend", "최대 레벨에 닿은 뒤에는 경험치 막대를 세 번 채울 때마다 초월 카드 세 장 중 하나를 고릅니다. (레벨은 오르지 않고, 카드마다 중첩 상한이 있습니다)");
        }
        transcendReadyAt = -1f;
        transcending = false;
    }

    // 이어하기 (RunSave 'tr')
    public void ReplayTranscend(int k)
    {
        if (k == TranscendBarCode) { transcendBars++; return; }
        ApplyTranscend(k);
        transcendBars = 0;
    }

    void ApplyTranscend(int k)
    {
        if (k < 0 || k >= TranscendCount || transcendLevel[k] >= TranscendMax[k]) return;
        if (bul == null) bul = FindFirstObjectByType<PlayerController>();
        if (bul == null) return;
        transcendLevel[k]++;
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        switch (k)
        {
            case 0:
                if (sp != null) sp.GrowAttack(TranscendAttack);         // 성장 카드 「힘」과 같은 기준 (처음 공격력의 10%를 더함)
                else bul.damage *= 1f + TranscendAttack;
                break;
            case 1:
                if (sp != null) sp.TranscendGauge(TranscendGauge);
                break;
            case 2:
                if (sp != null) sp.TranscendCrit(TranscendCrit, TranscendCritDamage);
                break;
            case 3:
                bul.PlayerMaxHealth += TranscendHp;
                bul.PlayerHealth = bul.PlayerMaxHealth;
                break;
            default:
                if (sp != null) sp.GrowRate(1f / (1f + TranscendRate));     // 모든 무기 발사 간격
                else bul.ShootSpeed /= 1f + TranscendRate;
                break;
        }
    }
}

// 초월 카드 효과 중 영혼 트리 수치에 더하는 것 (1.1~, LevelShop.Transcend)
public partial class SpecialAbilities
{
    public void TranscendGauge(float add) => TreeGaugeMul += add;

    public void TranscendCrit(float chance, float damage)
    {
        TreeCrit += chance;
        TreeCritDamage += damage;
    }
}
