using UnityEngine;

// 인형사 갈래 5개 (기획서 docs/plans/2026-11-02-big-update/hero-puppeteer.md 9절)
// 인형사 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Puppeteer.cs · 기존 파일(CharacterKit.Puppeteer*.cs · SignatureSkills.Puppeteer.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD)은 고치지 않음 (진화 글만 LevelShop.Signature 의 CharEvos 한 줄씩)
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 효과가 사는 곳
//   팽팽한 줄 · 호위 인형 · 바꿔치기 · 거울 인형 · 꼭두각시의 입문 · 공명 · 극의 · 교차 효과 → CharacterKit.Puppeteer.Paths (줄 베기 · 휘두름 · 자리 바꾸기 · 거울 더미 · 꼭두각시)
//   받는 피해(호위 인형 공명 I) · 주는 피해(호위 연타) · 공격 속도(바꿔치기 대가) · HUD 숫자 → 이 파일
// 상태는 모두 「끝나는 시각」 값이거나 카드 단계 · Paths.* 에서 다시 계산 (켜고 끄는 깃발 없음 · 플레이어 값을 직접 바꾸지 않음 → End 에서 되돌릴 것은 꼭두각시 놓기뿐)
public sealed class PuppeteerPaths : HeroPaths
{
    public const int Taut = 0, Guard = 1, Swap = 2, Mirror = 3, Marionette = 4;
    // 새 교차 번호 (Crosses 배열 순서: 0 가르는 퇴장 · 1 인형 호위대는 기존 진화)
    public const int CrossDuet = 2, CrossStandIn = 3;

    // ================================================================= 수치 (카드 · 번역 문장의 숫자와 같게)
    // 팽팽한 줄
    public const float TautCut = 1.6f, TautWidth = 1.5f, TautMinTension = 0.5f, TautSwingCost = 0.7f;
    public const float TautTipFrom = 0.667f, TautTipMul = 1.3f;                 // 공명 I: 줄 끝 3분의 1
    public const float TautSawGap = 0.25f, TautSawRate = 0.25f;                 // 공명 II: 보스 톱질
    public const float TautSecondAngle = 10f, TautSecondRate = 0.7f;            // 극의: 둘째 가닥
    // 호위 인형
    public const float GuardNear = 4f, GuardSwing = 1.4f, GuardBodyR = 0.8f, GuardMaxLen = 6f, GuardCutCost = 0.7f;
    public const float GuardTaken = 0.88f;                                      // 공명 I
    public const float GuardComboGap = 1.2f, GuardComboStep = 0.08f;            // 공명 II
    public const int GuardComboMax = 5;
    public const float GuardBackDelay = 0.2f, GuardBackRate = 0.6f;             // 극의
    // 바꿔치기 (기본은 CharacterKit.Puppeteer: SwapCost 0.2 · SwapGap 1.5 · SwapCut 1.2)
    public const float SwapPathCost = 0.12f, SwapPathGap = 0.8f, SwapCutMul = 1.5f, SwapRateCost = 0.8f;
    public const float SwapMasteryRefund = 0.04f, SwapMasteryRefundMax = 0.08f;     // 극의
    public const float SwapEntrance = 0.8f, SwapEntranceMul = 1.6f;                 // 공명 I
    public const int SwapKnotNeed = 3;                                              // 공명 II
    public const float SwapKnotRate = 3.5f, SwapKnotGap = 6f;
    // 거울 인형
    public const float MirrorRate = 0.65f, MirrorOverlap = 1.2f, MirrorMarkMul = 1.15f, MirrorMarkSec = 3f, MirrorDuetRate = 0.95f, MirrorThirdRate = 0.5f;
    // 꼭두각시
    public const int ThreadMax = 4, PuppetCap = 4, PuppetMasteryCap = 6;
    public const float PuppetLife = 4f, PuppetMasteryLife = 6f, PuppetPull = 6f;
    public const float PuppetSwingR = 1.2f, PuppetSwingRate = 0.4f, PuppetEndRate = 0.8f, MarionetteCutCost = 0.6f;
    public const float PuppetBowl = 0.3f;                                           // 공명 I
    public const float TangleSec = 2f, TangleSlow = 0.35f, TangleMul = 1.15f, TangleGap = 12f;      // 공명 II
    // 교차
    public const float DuetGap = 0.4f, StandInSec = 2f;

    // 판 기록 dealt_by 에 남는 출처 이름 (번역 키)
    public const string CutSource = "줄 베기", SwapSource = "가르기", PuppetSource = "꼭두각시", SawSource = "톱질", KnotSource = "꼬인 실";

    public override CharacterId Who => CharacterId.Puppeteer;
    public override string IconKey => "puppeteer";

    // 31번 새 카드는 없음 (기획서 9절 표에 없음)

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "m.taut", name = "팽팽한 줄", tagline = "줄이 칼날 · 무리 쓸기",
            seed = "팽팽한 줄", seedDesc = "줄 베기 피해가 60% 늘고 판정이 1.5배 굵어지며, 줄이 늘 팽팽하게 유지됩니다. (팽팽함 하한 50%)",
            cost = "더미 휘두름 피해가 30% 줄어듭니다.", strong = "무리 쓸기 · 일렬", weak = "근접 포위 · 짧은 줄",
            cards = new[] { P, S(1), S(9) }, evo = "me.harp",
            res1 = "줄 끝 3분의 1(더미 쪽)으로 벤 피해가 30% 늘어납니다.",
            res2 = "줄이 보스 몸에 걸쳐 있는 동안 0.25초마다 톱질합니다. (공격력 25%)",
            mastery = "줄이 한 가닥 더 갈라져 10° 옆으로 함께 벱니다. (피해 70%)",
            power = 1.0f,           // 보스 약 102: 휘두름 0.7 + 줄 ×1.6 · 톱질
            color = new Color(0.75f, 0.65f, 1f), form = "실감개 인형" },
        new PathDef {
            key = "m.guard", name = "호위 인형", tagline = "곁에 두는 더미 · 생존",
            seed = "호위 인형", seedDesc = "더미가 영웅 4칸 안에 있으면 「곁에 둔 더미」가 되어 휘두름 피해가 40% 늘고, 더미 몸에 닿은 적 투사체가 사라집니다.",
            cost = "최대 줄 길이가 9칸에서 6칸으로 줄고, 줄 베기 피해가 30% 줄어듭니다.", strong = "생존 · 보스 근접", weak = "넓은 무리 · 원거리 적",
            cards = new[] { M, S(8), S(3) }, evo = "me.armor",
            res1 = "곁에 둔 더미가 있으면 받는 피해가 12% 줄어듭니다.",
            res2 = "같은 적을 1.2초 안에 이어 휘두르면 「연타」로 휘두를 때마다 피해가 8% 늘어납니다. (최대 5번)",
            mastery = "휘두름이 0.2초 뒤 한 번 더 이어집니다. (피해 60%)",
            power = 0.95f,          // 근접 보스 약 112 → 연타 증가분에 power 를 곱해 약 106
            color = new Color(0.6f, 0.85f, 1f), form = "가위 손 인형" },
        new PathDef {
            key = "m.swap", name = "바꿔치기", tagline = "싼 자리 바꾸기 · 기동",
            seed = "바꿔치기", seedDesc = "자리 바꾸기에 드는 필살기 게이지가 20%에서 12%로, 최소 간격이 1.5초에서 0.8초로 줄고 가르기 피해가 50% 늘어납니다.",
            cost = "공격 속도가 20% 느려집니다.", strong = "기동 · 생존 · 보스", weak = "서서 쌓는 화력 · 늦은 커튼콜",
            cards = new[] { G, A, S(4), S(5) }, evo = "me.quickchange",
            res1 = "자리를 바꾼 직후 0.8초 안의 첫 휘두름이 60% 더 아픕니다. (등장)",
            res2 = "줄이 보스를 가로지르게 바꾸면 보스에 「꼬인 실」이 1 쌓이고, 3이 되면 조여 공격력 350%를 줍니다. (보스당 6초에 한 번)",
            mastery = "가르기로 적을 쓰러뜨리면 필살기 게이지 4%를 돌려받습니다. (바꾸기 한 번에 최대 8%)",
            power = 1.0f,           // 공속 −20% + 등장 ×1.6 + 꼬인 실 → 약 101 (커튼콜 몫이 빠지는 대신)
            color = new Color(1f, 0.85f, 0.5f), form = "대역 배우" },
        new PathDef {
            key = "m.mirror", name = "거울 인형", tagline = "좌우 대칭 두 더미 · 포위",
            seed = "거울 인형", seedDesc = "영웅을 기준으로 커서의 좌우를 뒤집은 자리에 거울 더미가 하나 더 떠서, 같은 줄로 같이 휘두릅니다.",
            cost = "두 더미의 휘두름 피해가 각각 65%가 됩니다.", strong = "양옆 포위 · 보스 겹치기", weak = "조작 난도 · 기본 단일 화력",
            cards = new[] { B, S(6), S(0) }, evo = "me.troupe",
            res1 = "두 더미가 1.2칸 안으로 겹쳐 같은 적을 치면 그 적이 3초 동안 받는 피해가 15% 늘어납니다.",
            res2 = "더미가 겹친 채 휘두르면 「합주」로 각 피해가 65%에서 95%가 됩니다. (커서를 영웅 바로 위나 아래에 두면 겹칩니다)",
            mastery = "커서의 위아래를 뒤집은 자리에 세 번째 더미가 나옵니다. (피해 50%)",
            power = 0.95f,          // 합주 약 112 → 거울 · 세 번째 더미 피해에 곱해 약 106 (겹치기 손 기술 보상은 남김)
            color = new Color(0.85f, 0.95f, 1f), form = "두 번째 실" },
        new PathDef {
            key = "m.marionette", name = "꼭두각시", tagline = "적을 실에 매달기 · 제어",
            seed = "꼭두각시 실", seedDesc = "줄에 베일 때마다 적에게 실이 1 감깁니다. (최대 4) 실이 4가 된 적은 4초 동안 꼭두각시가 되어 더미 쪽으로 끌려오고, 더미가 휘두를 때 제자리에서 둘레를 벱니다. (공격력 40%) 풀릴 때 한 번 더 터집니다. (80% · 동시 4)",
            cost = "줄 베기 피해가 40% 줄어듭니다.", strong = "무리 제어 · 자동 화력", weak = "보스 · 순간 폭딜",
            cards = new[] { K, S(2), S(7) }, evo = "me.strings",
            res1 = "끌려가는 꼭두각시에 부딪힌 적이 피해를 입습니다. (공격력 30%)",
            res2 = "보스와 중간 보스에 실이 4가 되면 「얽힘」 2초: 이동이 35% 느려지고 받는 피해가 15% 늘어납니다. (12초에 한 번 · 보스를 옮기지는 않습니다)",
            mastery = "꼭두각시가 동시에 6마리까지, 6초 동안 이어집니다.",
            power = 0.92f,          // 꼭두각시 휘두름(무리 몫)에만 걸어 무리 쏠림을 막음
            color = new Color(0.95f, 0.7f, 0.9f), form = "태엽 병정" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Swap, Taut, "me.exit"),                           // 가르는 퇴장 (A · S9)
        Cross(Guard, Marionette, "me.bodyguard"),               // 인형 호위대 (S3 · S2)
        Cross(Mirror, Taut, "m.x.duet", "이중주", "거울 더미 쪽 줄도 팽팽함이 적용되고, 두 줄이 0.4초 안에 같은 적을 베면 그 한 번이 두 배가 됩니다."),
        Cross(Swap, Marionette, "m.x.standin", "대역", "자리를 바꿀 때 남은 더미 대신 가장 가까운 꼭두각시가 그 자리로 끌려와 2초 동안 적을 끕니다. 더미는 바로 커서로 돌아가 무기가 묶이는 대가가 사라집니다."),
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

    // 판이 끝남: 붙잡은 꼭두각시 · 거울 더미 정리 (키트가 이미 사라졌으면 할 일 없음)
    public override void End()
    {
        CharacterKit kit = CharacterKit.Instance;
        if (kit != null) kit.PupPathCleanup(false);
    }

    // 이 갈래에 들어왔나 (인형사 판일 때만 참 — 키트 코드가 늘 이것으로 물음)
    public static bool On(int path) => Paths.Has(CharacterId.Puppeteer, path);

    // 공격 속도: 바꿔치기 대가 −20%
    public override float RateMul() => On(Swap) ? SwapRateCost : 1f;

    // 받는 피해: 호위 인형 공명 I — 곁에 둔 더미가 있으면 −12%
    public override float Taken(float dmg)
    {
        CharacterKit kit = CharacterKit.Instance;
        if (kit != null && On(Guard) && Paths.Res(Guard) >= 1 && kit.PupGuardNear) dmg *= GuardTaken;
        return dmg;
    }

    // 주는 피해: 호위 인형 공명 II 연타 (휘두름으로 줄 때만)
    public override float Outgoing(GameObject target, float dmg)
    {
        CharacterKit kit = CharacterKit.Instance;
        return kit != null ? kit.PupPathComboMul(target) : 1f;
    }

    // HUD 입문 칸: 바꿔치기 = 지금 게이지로 바꿀 수 있는 횟수 · 꼭두각시 = 꼭두각시 수
    static readonly string[] Swaps = MakeSwaps();
    static readonly string[,] Puppets = MakePuppets();

    static string[] MakeSwaps()
    {
        string[] a = new string[13];
        for (int i = 0; i < a.Length; i++) a[i] = "x" + i;
        return a;
    }

    static string[,] MakePuppets()
    {
        string[,] a = new string[PuppetMasteryCap + 1, PuppetMasteryCap + 1];
        for (int n = 0; n <= PuppetMasteryCap; n++)
            for (int c = 0; c <= PuppetMasteryCap; c++) a[n, c] = n + "/" + c;
        return a;
    }

    public override string Badge(int path)
    {
        CharacterKit kit = CharacterKit.Instance;
        if (kit == null) return null;
        if (path == Swap) return Swaps[Mathf.Clamp(kit.PupSwapsLeft, 0, Swaps.Length - 1)];
        if (path == Marionette)
        {
            int cap = Paths.Mastered(Marionette) ? PuppetMasteryCap : PuppetCap;
            return Puppets[Mathf.Clamp(kit.PupPuppetCount, 0, PuppetMasteryCap), cap];
        }
        return null;
    }

    // 평타(휘두름)는 원래 흐름 그대로 (CharacterKit.PuppetAttack) — KitAttack 은 쓰지 않음
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
