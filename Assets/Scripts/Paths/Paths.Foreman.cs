using UnityEngine;

// 공사 반장 갈래 5개 (기획서 docs/plans/2026-11-02-big-update/hero-foreman.md 9절)
// 공사 반장 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Foreman.cs · 기존 파일(CharacterKit.Foreman*.cs · SignatureSkills.Foreman.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 효과가 사는 곳
//   불도저 · 산사태 · 콘크리트 · 철거 · 안전 제일의 입문 · 공명 · 극의 · 교차 효과 → CharacterKit.Foreman.Paths (더미 · 덤프 · 바리케이드 · 쇠공 훅)
//   이동 속도(콘크리트) · 받는 피해(안전 제일) · 맞았을 때(안전 제일 공명 II) → 이 파일
// 상태는 모두 「끝나는 시각」 값이거나 카드 단계 · Paths.* 에서 다시 계산 (켜고 끄는 깃발 없음 · 플레이어 값을 직접 바꾸지 않음 → End 에서 되돌릴 것 없음)
public sealed class ForemanPaths : HeroPaths
{
    public const int Bulldoze = 0, Slide = 1, Concrete = 2, Demolish = 3, Safety = 4;
    // 새 교차 번호 (Crosses 배열 순서)
    public const int WallCross = 2, RockfallCross = 3;

    // ================================================================= 수치 (카드 · 번역 문장의 숫자와 같게)
    // 불도저
    public const int BulldozeExtra = 4, BulldozeShoveAt = 10;
    public const float BulldozeDumpCost = 0.6f, BulldozeShove = 0.4f, BulldozeShoveDist = 3f;
    public const float FullCrushHold = 1f, FullCrushRate = 2.5f, FullCrushBoss = 0.25f, FullCrushGap = 2f, FullCrushMasteryHold = 0.5f;
    public const int FullCrushMassCap = 16, BulldozeMasteryMax = 30;
    // 산사태
    public const float SlideLen = 1.6f, SlideWidth = 0.7f, SlideStun = 0.5f, SlideDamage = 1.4f, SlideCrushCost = 0.65f;
    public const float SlideMoundLife = 2f, SlideMoundSlow = 0.4f, SlideSecond = 0.6f, SlideBoss = 0.15f, SlideEcho = 0.4f, SlideEchoDelay = 0.5f;
    public const int SlideSecondAt = 12;
    public const float RedumpWindow = 0.8f;          // 31번 「작업 지시」: 덤프 직후 이 안에 다시 누름
    // 콘크리트
    public const int CementMax = 3, ConcreteMasteryNeed = 2;
    public const float CementCrush = 1.5f, RubbleLife = 5f, ConcreteMove = 0.9f, ConcreteDumpCost = 0.5f;
    public const float ConcreteBurst = 0.6f, ConcreteBurstR = 1.5f, ConcreteBossMul = 1.25f, ConcreteBossSec = 4f;
    // 철거
    public const int DemolishBounces = 4, DemolishBossBounce = 2;
    public const float DemolishBarricadeExtra = 2f, DemolishGauge = 0.85f, DemolishCrushCost = 0.8f;
    public const float DemolishCrashMul = 2f, DemolishRollRate = 1.2f, DemolishRollLen = 4f, DemolishTwin = 0.7f, DemolishTwinAngle = 15f;
    // 안전 제일
    public const float SafetyTaken = 0.85f, SafetyBlockBonus = 0.05f, SafetyBlockMax = 0.5f, SafetyBlockSec = 3f;
    public const int SafetyMassCap = 8;
    public const float SafetyWrapSec = 0.6f, SafetyWrapGap = 1f, SafetyBossBonus = 0.1f, SafetyBossMax = 0.6f, SafetyReflect = 0.6f;
    // 교차
    public const float WallLife = 3f, RockfallRate = 0.6f;
    public const int WallCap = 2;

    // 판 기록 dealt_by 에 남는 출처 이름 (번역 키)
    public const string FullCrushSource = "완전 압착", SlideSource = "산사태", CementSource = "시멘트 폭발", RollSource = "굴러가는 쇠공",
        ReflectSource = "되튕긴 탄", WallSource = "콘크리트 벽", RockfallSource = "낙석";

    public override CharacterId Who => CharacterId.Foreman;
    public override string IconKey => "foreman";

    // 31번 새 카드 「작업 지시」: 카드 글 · 단계 · 뽑기 · HUD 는 틀이 맡음. 효과만 CharacterKit.Foreman.Paths (단계 = Paths.Level(Paths.ExtraId), 외길이면 4)
    public override ExtraCardDef Extra => new ExtraCardDef
    {
        name = "작업 지시", desc = "덤프한 직후 0.8초 안에 다시 누르면 「한 번 더!」 — 흩어진 더미를 삽날 앞으로 다시 끌어모아 두 번째 덤프를 합니다.", stat = "두 번째 덤프 피해",
        values = new[] { "|50%|", "|65%|", "|80%|" }, fallbackIcon = 755,
    };

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "b.bulldoze", name = "불도저", tagline = "앞으로 밀어붙임 · 큰 더미로 짓누름",
            seed = "불도저", seedDesc = "밀 때 앞으로는 느려지지 않고(옆 · 뒤로 느려지는 것도 절반), 더미 상한이 4 늘어납니다.",
            cost = "덤프 거리가 40% 줄어듭니다.", strong = "무리 · 생존", weak = "넓게 퍼진 적 · 원거리",
            cards = new[] { P, K, S(6) }, evo = "be.dozer",
            res1 = "더미 무게가 10 이상이면 앞의 적을 흡수하지 않고 3칸 밀쳐 길을 냅니다. (공격력 40%)",
            res2 = "벽 · 바리케이드 · 보스에 대고 1초 동안 누르면 완전 압착이 일어납니다. 더미 안 모두 공격력 250%를 한 번 받고, 상대가 보스면 무게 × 공격력 25%를 더 받습니다. (무게 상한 16 · 2초에 한 번)",
            mastery = "더미 상한이 30이 되고, 완전 압착이 0.5초 만에 일어납니다.",
            power = 1.0f,           // 완전 압착이 보스 길: 상한 16 × 25% = 2초에 400% (보스전에 잡몹이 있을 때만)
            color = new Color(0.95f, 0.8f, 0.2f), form = "삽날" },
        new PathDef {
            key = "b.slide", name = "산사태", tagline = "길고 좁은 흙 파도 · 무리 폭발",
            seed = "산사태", seedDesc = "덤프가 길고 좁은 흙 파도(길이 1.6배 · 폭 0.7배)가 되고, 지나간 적이 0.5초 묻히며, 덤프 피해가 40% 늘어납니다.",
            cost = "압착 피해가 35% 줄어듭니다.", strong = "무리 폭발 · 통로 정리", weak = "덤프 사이 공백",
            cards = new[] { B, S(1), S(0), X }, evo = "be.truck",
            res1 = "덤프가 닿은 자리에 흙더미가 2초 동안 남아 밟은 적이 40% 느려집니다.",
            res2 = "무게 12 이상으로 덤프하면 1칸 옆으로 두 번째 파도가 따라옵니다. (60%) 보스가 파도에 맞으면 무게 × 공격력 15%를 더 받습니다. (무게 상한 16)",
            mastery = "덤프 0.5초 뒤 남은 흙이 한 번 더 굴러갑니다. (40%)",
            power = 0.95f,          // 덤프 +40% · 압착 −35% 는 보스전(잡몹 적음)에 손해라 공명 II 무게 비례로 메우고 조금만 낮춤
            color = new Color(0.78f, 0.6f, 0.36f), form = "굴착기 버킷" },
        new PathDef {
            key = "b.concrete", name = "콘크리트", tagline = "적을 굳혀 오래 붙잡음 · 지속 피해",
            seed = "레미콘", seedDesc = "짓누를 때마다 더미 안 적에게 시멘트가 쌓여(최대 3), 다 굳은 적은 압착 피해를 50% 더 받고 쓰러지면 잔해(5초)로 남습니다.",
            cost = "이동 속도가 10% 줄고, 덤프 거리가 50% 줄어듭니다.", strong = "오래 붙잡는 지속 피해 · 경제", weak = "기동 · 빠른 적",
            cards = new[] { M, S(5), S(8) }, evo = "be.concrete",
            res1 = "굳은 적에 닿아 있는 더미 속 적도 짓누를 때마다 시멘트가 하나 더 쌓입니다.",
            res2 = "더미 안에서 쓰러진 적이 터지며 둘레 1.5칸에 공격력 60%와 시멘트 1을 줍니다. (한 번 연쇄) 보스는 들이받기 · 빠각마다 시멘트 1이 쌓여, 3이 되면 4초 동안 압착 · 빠각 · 들이받기 피해가 25% 늘어납니다.",
            mastery = "시멘트가 2만 쌓여도 굳습니다.",
            power = 0.95f,          // 보스 시멘트 +25% 가 거의 늘 켜짐
            color = new Color(0.68f, 0.7f, 0.74f), form = "착암기" },
        new PathDef {
            key = "b.demolish", name = "철거", tagline = "쇠공 길 만들기 · 필살기 폭딜",
            seed = "철거 반장", seedDesc = "쇠공이 튕기는 횟수가 3에서 4로 늘고, 바리케이드가 2초 더 서 있으며, 필살기 게이지가 15% 덜 듭니다.",
            cost = "압착 피해가 20% 줄어듭니다.", strong = "지형 만들기 · 필살기 폭딜", weak = "평타 공백",
            cards = new[] { G, S(7), S(3), S(9) }, evo = "be.closed",
            res1 = "덤프한 더미가 바리케이드에 부딪히면 빠각 피해가 두 배가 됩니다.",
            res2 = "바리케이드가 사라질 때 앞쪽으로 작은 쇠공이 4칸 굴러갑니다. (공격력 120%) 쇠공이 보스에 맞으면 그 자리에서 한 번 더 튕깁니다. (필살기마다 최대 2번)",
            mastery = "쇠공이 두 개 떨어집니다. (두 번째는 15° 옆 · 피해 70%)",
            power = 0.92f,          // 쇠공 4번 + 보스 재튕김 + 게이지 −15% = 필살기 몫 약 +45%
            color = new Color(0.92f, 0.42f, 0.24f), form = "철거 쇠공" },
        new PathDef {
            key = "b.safety", name = "안전 제일", tagline = "탄을 막는 더미 · 탄막 보스",
            seed = "안전 제일", seedDesc = "더미가 무게와 상관없이 앞쪽 적 투사체를 막고, 밀고 있는 동안 받는 피해가 15% 줄어듭니다.",
            cost = "압착에 쓰는 무게 상한이 12에서 8로 줄어듭니다.", strong = "생존 · 탄막 보스", weak = "무리 화력",
            cards = new[] { A, S(4), S(2) }, evo = "be.zero",
            res1 = "더미가 막은 투사체마다 3초 동안 압착 피해가 5% 늘어납니다. (최대 50%)",
            res2 = "맞으면(1초에 한 번) 더미가 0.6초 동안 반장 둘레를 감싸 사방을 막습니다. 보스 탄을 막을 때마다 다음 들이받기 · 빠각 피해가 10% 늘어납니다. (최대 60%)",
            mastery = "막은 투사체를 되튕깁니다. (공격력 60%)",
            power = 1.05f,          // 압착 상한 8 로 무리 화력이 깎이는 대신 보스 쪽 보너스가 작아 조금 올림
            color = new Color(0.42f, 0.82f, 0.48f), form = "삽날" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Bulldoze, Safety, "be.pile"),                 // 항타기 (K · S2)
        Cross(Concrete, Demolish, "be.release"),            // 정식 출시 (S8 · S9)
        Cross(Bulldoze, Concrete, "b.x.wall", "콘크리트 벽", "상한까지 찬 더미를 덤프하면 쏟는 대신 그 자리에 3초 동안 벽으로 굳습니다. 적을 막고, 벽 압착 · 빠각의 상대가 됩니다. (동시 2개)"),
        Cross(Slide, Demolish, "b.x.rockfall", "낙석 주의", "산사태 파도가 바리케이드에 닿으면 바리케이드가 무너지며 그 뒤로 산사태가 한 번 더 일어납니다. (60% · 바리케이드마다 한 번)"),
    };

    // ================================================================= 효과
    // 입문 · 공명 · 극의 · 교차는 효과 코드가 그때그때 Paths.Has · Res · Mastered · CrossOn 으로 물음 → 판 흐름 훅은 비워 둠
    //   (이어하기도 같은 상태가 됨 · replay 때 만드는 것 없음)
    public override void OnSeed(int path, bool replay) { }
    public override void OnSingle(int path, bool replay) { }
    public override void OnResonance(int path, int tier, bool replay) { }
    public override void OnComplete(int path, bool replay) { }
    public override void OnMastery(int path, bool replay) { }
    public override void OnCross(int cross, bool replay) { }
    public override void End() { }          // 플레이어 값을 직접 바꾸지 않음

    // 이 갈래에 들어왔나 (공사 반장 판일 때만 참 — 키트 코드가 늘 이것으로 물음)
    public static bool On(int path) => Paths.Has(CharacterId.Foreman, path);

    // 이동 속도: 콘크리트 대가 −10%
    public override float MoveMul() => On(Concrete) ? ConcreteMove : 1f;

    // 받는 피해: 안전 제일 — 더미를 밀고 있는 동안(또는 더미가 있는 동안) −15%
    public override float Taken(float dmg)
    {
        CharacterKit kit = CharacterKit.Instance;
        if (kit != null && On(Safety) && (kit.FmHolding || kit.FmPileCount > 0)) dmg *= SafetyTaken;
        return dmg;
    }

    // 맞은 뒤: 안전 제일 공명 II — 더미가 반장 둘레를 감쌈
    public override void Hurt(float taken)
    {
        CharacterKit kit = CharacterKit.Instance;
        if (kit != null && taken > 0f && On(Safety) && Paths.Res(Safety) >= 2) kit.FmPathHurt();
    }

    // HUD 입문 칸: 안전 제일 공명 I 이면 지금 쌓인 압착 보너스 칸 수, 콘크리트 공명 II 이면 보스 시멘트
    static readonly string[] Stacks = { "", "+5%", "+10%", "+15%", "+20%", "+25%", "+30%", "+35%", "+40%", "+45%", "+50%" };
    public override string Badge(int path)
    {
        CharacterKit kit = CharacterKit.Instance;
        if (kit == null) return null;
        if (path == Safety && Paths.Res(Safety) >= 1)
        {
            int n = kit.FmSafetyStacks;
            return n > 0 && n < Stacks.Length ? Stacks[n] : null;
        }
        return null;
    }

    // 평타는 원래 흐름 그대로 (밀기는 CharacterKit.UpdatePlow) — KitAttack 은 쓰지 않음
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
