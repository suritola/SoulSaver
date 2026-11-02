using UnityEngine;

// 장의사 갈래 5개 (기획서 docs/plans/2026-11-02-big-update/hero-undertaker.md 9절)
// 장의사 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Undertaker.cs · 기존 파일(CharacterKit.Undertaker*.cs · SignatureSkills.Undertaker.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD)은 고치지 않음 (진화 재료는 모두 한 갈래 안이라 PathEvoParts 도 그대로)
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 효과가 사는 곳
//   생매장 · 묘역 설계 · 땅굴 · 장례식 · 도굴꾼의 입문 · 공명 · 극의 · 교차 효과 → CharacterKit.Undertaker.Paths (갇힘 · 구멍 · 묘비 · 땅굴 · 장례 · 되파기)
//   받는 피해(땅굴 대가) · 필살기 게이지 충전(상주) · HUD 숫자 → 이 파일
// 상태는 모두 「끝나는 시각」 값이거나 카드 단계 · Paths.* 에서 다시 계산 (켜고 끄는 깃발 없음 · 플레이어 값을 직접 바꾸지 않음)
// 31번 새 카드는 없음 (도랑은 갈래가 아니라 고유 스킬 한 장 — 기획서 0절 4번)
public sealed class UndertakerPaths : HeroPaths
{
    public const int Live = 0, Yard = 1, Tunnel = 2, Funeral = 3, Robber = 4;
    // 새 교차 번호 (Crosses 배열 순서: 0 안식처 · 1 개미지옥은 기존 진화)
    public const int MassFuneralCross = 2, UnderworldCross = 3;

    // ================================================================= 수치 (카드 · 번역 문장의 숫자와 같게)
    // 생매장
    public const float LiveTrap = 2.2f, LiveBuryMul = 1.35f, LiveDigSpeed = 0.7f, LiveRes1Grow = 0.5f;
    public const float LiveRes2PerTrapped = 0.15f, LiveRes2Cap = 0.6f, LiveMasteryTrap = 3f;
    public const int LiveRes1Need = 2, LiveMasteryHoles = 1;
    // 묘역 설계
    public const int YardEmptyGraveHp = 4, YardCap = 14, YardMasteryCap = 20, YardMasteryHpMul = 2;
    public const float YardLife = 35f, YardBuryCost = 0.7f, YardRes1Vuln = 0.1f, YardRes2BossHit = 2.5f, YardRes2Gap = 1f;
    // 땅굴
    public const float TunnelGap = 2f, TunnelInvuln = 0.25f, TunnelPop = 1f, TunnelPopR = 2f, TunnelPopKnock = 5f, TunnelTaken = 1.1f;
    public const float TunnelRes2Slow = 0.5f, TunnelRes2Sec = 1f, TunnelRes2Hit = 0.6f, TunnelRes2Width = 1.2f, TunnelMasteryGap = 0.8f;
    public const int TunnelHoleCost = 1;
    // 장례식
    public const float FuneralGauge = 1.4f, FuneralPerBury = 0.005f, FuneralHoleLife = 3f;
    public const int FuneralRangePad = 4;
    public const float FuneralRes1Sec = 6f, FuneralRes1Dig = 2f, FuneralRes2Hit = 0.8f, FuneralMasteryDelay = 1.5f, FuneralMasteryMul = 0.5f;
    // 도굴꾼
    public const float RobberBreak = 1.5f, RobberBreakR = 2f, RobberGraveLife = 8f, RobberShardHit = 0.4f, RobberRes2Range = 4f, RobberRes2Mul = 1.5f, RobberChainR = 2f;
    public const int RobberShards = 3, RobberChainMax = 4;
    // 새 교차
    public const float MassFuneralDelay = 2f, MassFuneralMul = 1.25f, UnderworldSec = 4f;

    // 판 기록 dealt_by 에 남는 출처 이름 (번역 키)
    public const string CollapseSource = "묘비 무너짐", TunnelSource = "땅굴";

    public override CharacterId Who => CharacterId.Undertaker;
    public override string IconKey => "undertaker";

    // 31번 새 카드는 없음 (기획서 9절 표에 없음)

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "y.live", name = "생매장", tagline = "오래 가두고 한꺼번에 · 무리 매장",
            seed = "깊이 파기", seedDesc = "갇힘 시간이 1.4초에서 2.2초로 늘고, 갇힌 적에게 주는 매장 피해가 35% 늘어납니다.",
            cost = "구멍이 넓어지는 속도가 30% 느려집니다.", strong = "밀집 무리", weak = "흩어진 적 · 보스 단독",
            cards = new[] { P, K, S(0) }, evo = "ye.mass",
            res1 = "갇힌 적이 2마리 이상인 구멍은 반지름이 0.5칸 늘어납니다.",
            res2 = "보스가 구멍 위에 있으면 매장할 때 그 구멍에 갇힌 적 하나당 보스 피해가 15% 늘어납니다. (최대 60%)",
            mastery = "갇힘 시간이 3초가 되고, 구멍을 한 개 더 둘 수 있습니다.",
            power = 1.0f,           // 입문 ×1.35 는 갇힌 적(=잡몹)만 · 보스 길은 공명 II
            color = new Color(0.851f, 0.702f, 0.549f), form = "묘지 까마귀" },
        new PathDef {
            key = "y.yard", name = "묘역 설계", tagline = "묘비로 짓는 담 · 길 막기",
            seed = "묘지기", seedDesc = "빈 구멍을 매장해도 빈 묘비(내구 4)가 솟고, 묘비 상한이 8개에서 14개로, 수명이 20초에서 35초로 늘어납니다.",
            cost = "매장 피해가 30% 줄어듭니다.", strong = "생존 · 길 막기 · 엄폐", weak = "화력 · 빠른 무리",
            cards = new[] { A, S(3), S(5) }, evo = "ye.keeper",
            res1 = "묘비 둘레 2칸 안의 적이 받는 피해가 기본으로 10% 늘어납니다. (비문 카드와 더해집니다)",
            res2 = "보스가 묘비를 부수면 무너진 흙이 보스에게 공격력 250%를 줍니다. (1초에 한 번)",
            mastery = "묘비 내구가 두 배가 되고 묘비 상한이 20개가 됩니다.",
            power = 0.95f,          // 공명 II 250% 가 「보스가 묘비 부술 때마다」라 보스전 지속 피해 큼
            color = new Color(0.604f, 0.69f, 0.784f), form = "석관" },
        new PathDef {
            key = "y.tunnel", name = "땅굴", tagline = "구멍 사이를 오감 · 기동",
            seed = "땅굴꾼", seedDesc = "내 구멍 위로 걸어 들어가면 땅속으로 들어가(0.25초 무적) 가장 최근에 판 다른 구멍에서 튀어나와, 둘레 2칸 적에게 공격력 100%를 주고 밀칩니다. (2초에 한 번)",
            cost = "최대 구멍이 1개 줄고, 받는 피해가 10% 늘어납니다.", strong = "기동 · 탈출 · 생존", weak = "무리 처리",
            cards = new[] { M, S(1), S(9) }, evo = "ye.catacomb",
            res1 = "튀어나온 구멍이 곧바로 가장 크게 넓어집니다.",
            res2 = "들어간 구멍에서 나온 구멍까지 직선 위의 적이 흙에 걸려 1초 동안 50% 느려지고 공격력 60%를 받습니다. (보스는 피해만)",
            mastery = "땅굴 쉬는 시간이 2초에서 0.8초로 줄어듭니다.",
            power = 1.0f,           // 피해가 거의 없는 기동 갈래
            color = new Color(0.549f, 0.478f, 0.4f), form = "두 번 파기" },
        new PathDef {
            key = "y.funeral", name = "장례식", tagline = "필살기가 주인공 · 섬멸",
            seed = "상주", seedDesc = "필살기 게이지 충전이 40% 빨라지고, 장례 범위가 화면 + 4칸으로 넓어지며, 매장으로 묻은 적 하나당 게이지가 0.5% 찹니다.",
            cost = "구멍 수명이 6초에서 3초로 줄어듭니다.", strong = "필살기 섬멸 · 보스 순간", weak = "게이지 사이 공백",
            cards = new[] { G, S(7), S(8) }, evo = "ye.state",
            res1 = "장례 뒤 6초 동안 구멍이 두 배 빨리 넓어지고 매장 뒤 쉼이 없어집니다.",
            res2 = "장례 임시 묘비가 무너질 때 둘레 2칸 적(보스 포함)에게 공격력 80%를 줍니다.",
            mastery = "장례가 1.5초 뒤 한 번 더 일어납니다. (피해 50% · 묘비 없음)",
            power = 0.92f,          // 게이지 ×1.4 → 필살기 보스 320% 가 약 1.4배 자주 = 보스 +12% → 0.92 로 +3%
            color = new Color(0.784f, 0.784f, 0.847f), form = "상여 종" },
        new PathDef {
            key = "y.robber", name = "도굴꾼", tagline = "묘비를 터뜨림 · 보스 순간 화력",
            seed = "도굴", seedDesc = "묘비 위에 구멍을 파면 묘비가 무너지며 둘레 2칸 적에게 공격력 150%를 주고(되파기 카드와 더해집니다), 그 구멍은 곧바로 가장 크게 열립니다.",
            cost = "묘비 수명이 20초에서 8초로 줄어듭니다.", strong = "보스 순간 화력 · 코인", weak = "생존 (벽이 오래 못 감)",
            cards = new[] { B, S(4), S(2), S(6) }, evo = "ye.robber",
            res1 = "무너진 묘비 파편 셋이 가까운 적에게 날아갑니다. (공격력 40%씩)",
            res2 = "보스 4칸 안의 묘비가 무너지면 그 피해가 50% 늘어납니다.",
            mastery = "무너짐이 2칸 안 다른 묘비로 이어집니다. (최대 4개)",
            power = 0.95f,          // 묘비 무너짐 150% × 공명 II 1.5 가 보스 옆에서 반복
            color = new Color(0.878f, 0.753f, 0.439f), form = "무거운 흙" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Yard, Tunnel, "ye.haven"),                        // 안식처 (S5 · S9)
        Cross(Live, Robber, "ye.antlion"),                      // 개미지옥 (K · S6)
        Cross(Live, Funeral, "y.x.mass", "합동 장례", "장례로 떨어진 적의 일제 매장이 1초가 아닌 2초 뒤로 늦춰지는 대신 매장 피해가 25% 늘고, 그사이 지나가던 다른 적도 같은 구멍에 빠집니다."),
        Cross(Tunnel, Funeral, "y.x.underworld", "저승길", "장례 뒤 4초 동안 땅굴이 화면 안 아무 묘비나 구멍에서(커서에 가장 가까운 곳) 튀어나오고 쉬는 시간이 없어집니다."),
    };

    // ================================================================= 효과
    // 입문 · 공명 · 극의 · 교차는 효과 코드가 그때그때 Paths.Has · Res · Mastered · CrossOn 으로 물음 → 판 흐름 훅은 비워 둠
    //   (이어하기도 같은 상태가 됨 · replay 때 만드는 것 없음)

    // 이 갈래에 들어왔나 (장의사 판일 때만 참 — 키트 코드가 늘 이것으로 물음)
    public static bool On(int path) => Paths.Has(CharacterId.Undertaker, path);

    // 교차가 켜졌나 (장의사 판일 때만)
    public static bool CrossIs(int cross) => Paths.Hero is UndertakerPaths && Paths.CrossOn(cross);

    // 받는 피해: 땅굴 대가 +10%
    public override float Taken(float dmg) => On(Tunnel) ? dmg * TunnelTaken : dmg;

    // ---------------- 필살기 게이지 충전 ×1.4 (상주): 지난 프레임보다 늘어난 만큼(자연 충전 · 처치 가속)에 배율을 더 붙임
    //   한 번에 크게 들어온 것(부고 · 국장)은 덩어리라 배율을 붙이지 않음 (Paths.Clockmaker 와 같은 방식)
    float lastSp = -1f;

    public override void Tick()
    {
        PlayerController p = Hostile.Player;
        SkillGauge g = p != null ? p.skillGauge : null;
        if (g == null || !On(Funeral)) { lastSp = -1f; return; }
        float sp = g.SkillPoint;
        if (lastSp >= 0f && sp > lastSp && !p.IsSkillUsing)
        {
            float gain = sp - lastSp;
            float lump = g.MaxSkillPoint * 0.04f + g.pointsPerSecond * 4f * Time.deltaTime;
            if (gain <= lump) g.AddSkillPoint(gain * (FuneralGauge - 1f));
        }
        lastSp = g.SkillPoint;
    }

    // 매장으로 묻은 적마다 게이지 +0.5% (상주 · 덩어리라 충전 배율을 붙이지 않게 지난 값도 같이 올림)
    public static void BuryGauge(int killed)
    {
        if (killed <= 0 || !On(Funeral)) return;
        PlayerController p = Hostile.Player;
        SkillGauge g = p != null ? p.skillGauge : null;
        if (g == null) return;
        g.AddSkillPoint(g.MaxSkillPoint * FuneralPerBury * killed);
        if (Paths.Hero is UndertakerPaths u) u.lastSp = g.SkillPoint;
    }

    // HUD 입문 칸: 묘역 설계 = 「묘비 7/14」 · 땅굴 = 쉬는 시간이 남았으면 「1.2」 (값이 바뀔 때만 문자열 — 미리 만든 표)
    static readonly string[,] Graves = MakeGraves();
    static readonly string[] Waits = MakeWaits();

    static string[,] MakeGraves()
    {
        string[,] a = new string[YardMasteryCap + 1, YardMasteryCap + 1];
        for (int n = 0; n <= YardMasteryCap; n++)
            for (int c = 0; c <= YardMasteryCap; c++) a[n, c] = n + "/" + c;
        return a;
    }

    static string[] MakeWaits()
    {
        string[] a = new string[21];
        for (int i = 0; i < a.Length; i++) a[i] = (i / 10f).ToString("0.0");
        return a;
    }

    public override string Badge(int path)
    {
        CharacterKit kit = CharacterKit.Instance;
        if (kit == null) return null;
        if (path == Yard)
            return Graves[Mathf.Clamp(kit.UtGraveCount, 0, YardMasteryCap), Mathf.Clamp(kit.UtGraveMax, 0, YardMasteryCap)];
        if (path == Tunnel)
        {
            float left = kit.UtTunnelLeft;
            return left > 0.05f ? Waits[Mathf.Clamp(Mathf.CeilToInt(left * 10f), 0, Waits.Length - 1)] : null;
        }
        return null;
    }

    // 평타(파기 · 매장)는 원래 흐름 그대로 (CharacterKit.UpdateDig) — KitAttack 은 쓰지 않음
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
