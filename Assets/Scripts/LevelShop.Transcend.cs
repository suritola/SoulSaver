using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 판 후반 보상 (1.1~): 초월 카드
// 최대 레벨(50 · 무한 모드 80, LevelCap)은 그대로 두고, 상한에 닿은 뒤 경험치 막대를 세 번 채울 때마다 (레벨은 오르지 않음, 로드맵 1-5 「3레벨마다」)
//   첫째 · 둘째 막대는 비우면서 「새 규칙까지 ◆◆◆」만 알리고, 셋째 막대에서 창을 엶 (이어하기 기록은 tr9)
// 금테 「초월 카드」 세 장 중 하나를 고름 (보스 보상 · 보물 상자와 같은 ChoiceUI 창)
// 1.2~ 숫자만 오르던 다섯 장(파괴 · 충전 · 급소 · 생명 · 질풍)을 「판의 규칙이 바뀌는」 열한 장으로 갈아엎음 (효과는 TranscendRules)
//   메아리 · 영혼 수확 · 시간의 틈 · 연금 손 · 그림자 분신 · 중력장 · 역전 · 맵 지배 · 연쇄 붕괴 · 영혼 보호막 · 영혼불 발자국
//   같은 카드를 다시 고르면 다음 단계 (최대 3단계, 맵 지배는 2단계) · 역전은 최후의 보루가 없는 영혼의 무게 6, 맵 지배는 맵 물체가 없는 무한 모드에서 나오지 않음
// 모든 카드가 최대 단계면 예전처럼 막대가 가득 찬 채 멈춤
// 고르면 막대를 비움 (복원은 RestoreLevels 가 저장한 막대 그대로 — 가득 찬 채 저장했으면 이어할 때 다시 고름)
// 이어하기 기록: tr<10 + 번호> (1.2~) · 옛 기록 tr0~tr4 는 대응하는 새 카드로 바꿔 적용 (OldTranscendMap)
public partial class LevelShop
{
    public const int TranscendCount = TranscendRules.Count;
    static readonly string[] TranscendNames =
    {
        "초월: 메아리", "초월: 영혼 수확", "초월: 시간의 틈", "초월: 연금 손", "초월: 그림자 분신", "초월: 중력장",
        "초월: 역전", "초월: 맵 지배", "초월: 연쇄 붕괴", "초월: 영혼 보호막", "초월: 영혼불 발자국",
    };
    // 단계마다 이번에 생기는 규칙 ([카드][고르기 전 단계])
    static readonly string[][] TranscendDescs =
    {
        new[] { "필살기를 쓰고 나면 잠시 뒤 그 자리에 영혼 메아리가 울려 둘레 적을 휩쓸고 적 탄을 지웁니다", "메아리에 휩쓸린 적이 1.5초 동안 얼어붙습니다", "메아리가 한 번 더, 더 넓게 울립니다" },
        new[] { "적 150마리를 쓰러뜨릴 때마다 영혼 파동이 둘레 잡몹을 쓸어 버립니다", "이제 110마리마다 영혼 파동이 일어납니다", "이제 75마리마다 영혼 파동이 일어납니다" },
        new[] { "체력이 40% 아래일 때 맞으면 3초 동안 세상이 거의 멈춥니다 (60초마다)", "시간의 틈이 4초로 길어지고 40초마다 열립니다", "시간의 틈 동안 피해를 받지 않습니다" },
        new[] { "코인을 주울 때 10% 확률로 경험치로 연성됩니다 (다음 초월 카드가 빨라짐)", "연성 확률이 18%로 오릅니다", "연성 확률이 25%로 오르고, 연성할 때 영혼 조각도 하나 생깁니다" },
        new[] { "그림자 분신 하나가 발자취를 따라오며 가까운 적에게 그림자 화살을 쏩니다", "그림자 분신이 둘이 됩니다", "그림자 분신이 셋이 됩니다" },
        new[] { "1초 동안 가만히 서 있으면 중력장이 생겨 둘레 잡몹과 코인을 끌어당깁니다", "중력장이 더 넓어집니다", "중력장이 더 넓어지고, 끌려온 적이 느려집니다" },
        new[] { "최후의 보루가 발동하면 그 자리에서 대폭발이 일고 무적이 4초로 늘어납니다", "최후의 보루가 장마다 한 번 더 발동합니다", "역전할 때 체력의 50%를 회복합니다" },
        new[] { "부순 맵 물체가 18초 뒤 다른 자리에 다시 생기고, 부술 때 폭발합니다", "맵 물체가 10초 만에 다시 생기고 폭발이 커집니다" },
        new[] { "쓰러진 적이 12% 확률로 터져 둘레 적에게 번집니다 (터진 적도 다시 터짐)", "연쇄 확률이 18%로 오릅니다", "연쇄 확률이 25%로 오르고 폭발이 커집니다" },
        new[] { "8초 동안 맞지 않으면 공격 한 번을 막는 영혼 보호막이 생깁니다", "보호막이 5초 만에 다시 생깁니다", "보호막이 깨질 때 둘레 적을 밀쳐 내고 얼립니다" },
        new[] { "움직이면 지나간 자리에 영혼불이 남아 밟은 적을 태웁니다", "영혼불이 더 오래 남습니다", "영혼불이 더 넓고 오래 남습니다" },
    };
    // 중첩 상한 (카드마다)
    public static readonly int[] TranscendMax = { 3, 3, 3, 3, 3, 3, 3, 2, 3, 3, 3 };
    // Icons/choice_tr_<이름> (tools/pixelart/make_transcend_icons.py)
    static readonly string[] TranscendIcons = { "tr_echo", "tr_reap", "tr_rift", "tr_alchemy", "tr_shadow", "tr_gravity", "tr_reversal", "tr_dominion", "tr_cascade", "tr_ward", "tr_embers" };
    public static string TranscendIcon(int k) => TranscendIcons[Mathf.Clamp(k, 0, TranscendCount - 1)];
    static readonly Color TranscendGold = new Color(1f, 0.84f, 0.32f);                                // 금테

    // 옛 기록(1.1, tr0~tr4: 파괴 · 충전 · 급소 · 생명 · 질풍) → 새 카드
    static readonly int[] OldTranscendMap = { TranscendRules.Cascade, TranscendRules.Echo, TranscendRules.Shadow, TranscendRules.Ward, TranscendRules.Embers };
    const int TranscendCodeBase = 10;

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

    // 1.1.9~ 빌드 자세히 보기: 지금까지 생긴 규칙 (단계마다 한 줄)
    internal static string TranscendBody(int k, int level)
    {
        if (k < 0 || k >= TranscendCount) return "";
        string[] d = TranscendDescs[k];
        List<string> lines = new List<string>();
        for (int i = 0; i < level && i < d.Length; i++) lines.Add(Loc.T(d[i]));
        return string.Join("\n", lines);
    }

    // 이번 판에서 나올 수 있는 카드인지 (역전: 최후의 보루가 있어야 · 맵 지배: 맵 물체가 있는 이야기 모드)
    static bool TranscendFits(int k)
    {
        if (k == TranscendRules.Reversal) return !SoulWeight.NoLastStand;
        if (k == TranscendRules.Dominion) return !GameMode.IsEndless && MapProps.Instance != null;
        return true;
    }

    // 단계 표시: 짧은 글 (「n → n+1 / 최대」 숫자는 한눈에 안 들어와서) · now = 고르기 전 단계
    static string TranscendStageText(int k, int now)
    {
        if (now == 0) return Loc.T("새 규칙!");
        if (now + 1 >= TranscendMax[k]) return Loc.T("최대 단계!");
        return Loc.T((now + 1) + "단계째");
    }

    // 아직 더 올릴 수 있는 초월 카드가 있는지
    bool TranscendLeft
    {
        get
        {
            for (int k = 0; k < TranscendCount; k++) if (transcendLevel[k] < TranscendMax[k] && TranscendFits(k)) return true;
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
        // 칸이 채워지는 모양 (금빛 ◆ = 채운 막대)
        string pips = "<color=#FFD75A>" + new string('◆', transcendBars) + "</color><color=#5E5272>" + new string('◆', TranscendBarsPer - transcendBars) + "</color>";
        if (sm != null) Notice.Toast(NoticeIcon.Up, Loc.T("새 규칙까지") + "  " + pips, 3f);
    }

    IEnumerator TranscendPick()
    {
        transcending = true;
        // 아직 최대 단계가 아닌 카드 가운데 겹치지 않게 최대 세 장
        List<int> left = new List<int>();
        for (int k = 0; k < TranscendCount; k++) if (transcendLevel[k] < TranscendMax[k] && TranscendFits(k)) left.Add(k);
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
            string desc = Loc.T(TranscendDescs[k][Mathf.Min(n, TranscendDescs[k].Length - 1)]) + "\n<color=#FFD75A>" + TranscendStageText(k, n) + "</color>";
            options[i] = new ChoiceUI.Option(TranscendNames[k], desc, TranscendGold, TranscendIcons[k]);
        }
        int pick = 0;
        yield return ChoiceUI.Run("초월: 새 규칙", "판의 규칙 하나를 바꿉니다 · 같은 카드를 다시 고르면 다음 단계", options, i => pick = i);
        int chosen = ids.Count > 0 ? ids[Mathf.Clamp(pick, 0, ids.Count - 1)] : -1;
        if (chosen >= 0)
        {
            if (bul != null) bul.nowEXP = Mathf.Max(0f, bul.nowEXP - bul.needEXP);
            int before = transcendLevel[chosen];
            ApplyTranscend(chosen, false);
            RunSave.Record("tr" + (TranscendCodeBase + chosen));        // 판 중간 저장 (이어할 때 같은 초월 카드를 다시 적용)
            transcendBars = 0;
            if (bul != null) Juice.LevelUp(bul.transform.position);
            StageManager sm = StageManager.Instance;
            if (sm != null) Notice.Toast(NoticeIcon.Up, TranscendName(chosen) + "  <color=#FFD75A>" + TranscendStageText(chosen, before) + "</color>", 3.5f);
            Hints.Show("transcend", "최대 레벨에 닿은 뒤에는 경험치 막대를 세 번 채울 때마다 초월 카드로 판의 규칙 하나를 바꿉니다. (레벨은 오르지 않고, 같은 카드를 다시 고르면 다음 단계가 됩니다)");
        }
        transcendReadyAt = -1f;
        transcending = false;
    }

    // 이어하기 (RunSave 'tr'): 9 = 막대 하나 · 10~ = 새 카드 · 0~4 = 1.1 옛 카드 (대응하는 새 카드로, 상한을 넘는 몫은 버림)
    public void ReplayTranscend(int code)
    {
        if (code == TranscendBarCode) { transcendBars++; return; }
        int k = code >= TranscendCodeBase ? code - TranscendCodeBase : code >= 0 && code < OldTranscendMap.Length ? OldTranscendMap[code] : -1;
        if (k >= 0 && k < TranscendCount) ApplyTranscend(k, true);
        transcendBars = 0;
    }

    // 단계만 올리고, 규칙은 TranscendRules 가 단계를 보고 바꿈 (기본 능력치는 건드리지 않음)
    void ApplyTranscend(int k, bool silent)
    {
        if (k < 0 || k >= TranscendCount || transcendLevel[k] >= TranscendMax[k]) return;
        if (bul == null) bul = FindFirstObjectByType<PlayerController>();
        transcendLevel[k]++;
        TranscendRules rules = TranscendRules.Ensure(this);
        if (rules != null) rules.OnPicked(k, silent);
    }
}
