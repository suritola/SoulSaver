using UnityEngine;

// 사진사 갈래 5개 (기획서 docs/plans/2026-11-02-big-update/hero-photographer.md 9절)
// 사진사 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Photographer.cs · 기존 파일(CharacterKit.Photographer*.cs · SignatureSkills.Photographer.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD)은 고치지 않음 (진화 재료는 기획서 6절 그대로 갈래 안 · 교차에 걸쳐 있어 PathEvoParts 도 그대로)
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 효과가 사는 곳
//   연사 · 망원 · 암실 · 종이 공작 · 플래시의 입문 · 공명 · 극의 · 교차 효과 → CharacterKit.Photographer.Paths (필름 · 틀 · 초점 · 인화 · 종이 · 스트로보)
//   플래시 반사(투사체 지우기) · 무적 · 게이지 → SignatureSkills.Photographer (OnPhShutter)
//   셔터 간격(연사 ×0.6 · 플래시 +25%) · 보스 받는 피해(암실 공명 II) · HUD 숫자 → 이 파일
// 상태는 모두 「끝나는 시각」 값이거나 카드 단계 · Paths.* 에서 다시 계산 (켜고 끄는 깃발 없음 · 플레이어 값을 직접 바꾸지 않음)
// 31번 새 카드는 없음 (기획서 9절 표에 없음)
public sealed class PhotographerPaths : HeroPaths
{
    public const int Burst = 0, Tele = 1, Dark = 2, Craft = 3, Flash = 4;
    // 새 교차 번호 (Crosses 배열 순서: 0 플립북 · 1 기념 앨범은 기존 진화)
    public const int PuppetCross = 2, StrobeCross = 3;

    // ================================================================= 수치 (카드 · 번역 문장의 숫자와 같게)
    // 연사
    public const int BurstMag = 12, BurstSnapN = 3;
    public const float BurstRate = 0.6f, BurstFrame = 0.8f, BurstPaper = 1.5f, BurstCost = 0.65f;
    public const float BurstSnapWin = 1f, BurstSnapMul = 1f, BurstOverlapMul = 2f, BurstMasteryWind = 0.6f, BurstWindMul = 0.5f;
    // 망원
    public const float TeleRange = 18f, TeleFrame = 0.65f, TeleFocusR = 1.2f, TeleFocus = 2.2f, TeleNear = 6f, TeleNearCost = 0.5f;
    public const float TeleRes1Paper = 2f, TeleCloseUp = 4f;
    public const int TeleRes1Marks = 3;
    // 암실
    public const int DarkPrints = 2, DarkFilmLess = 2, DarkMasteryPrints = 3, DarkMasteryPerPrint = 4, DarkMasteryTotal = 30;
    public const float DarkLure = 4.5f, DarkTear = 1.6f, DarkWind = 0.3f, DarkCurtainSec = 0.8f, DarkCurtainR = 1.4f, DarkBossMul = 1.15f, DarkBossR = 3f;
    // 종이 공작
    public const int CraftShards = 2, CraftPileN = 5;
    public const float CraftPaper = 4f, CraftCost = 0.7f, CraftRes1Paper = 1.5f, CraftPileR = 3f, CraftPileMul = 2f, CraftPileBossR = 4f, CraftPileBossMul = 0.5f;
    // 플래시
    public const float FlashRim = 1.5f, FlashStop = 0.5f, FlashStopGap = 3f, FlashRate = 1.25f;
    public const float FlashRes1Invuln = 0.4f, FlashRes1Gap = 2f, FlashGauge = 0.01f, FlashGaugeCap = 0.05f, FlashMasteryRim = 3f, FlashMasteryStop = 1f;
    // 새 교차
    public const int StrobeEvery = 3;
    public const float PuppetSpeed = 1f, StrobeRim = 2f;

    // 판 기록 dealt_by 에 남는 출처 이름 (번역 키)
    public const string CloseUpSource = "클로즈업", PileSource = "종이 더미", SnapSource = "필름 끊김";

    public override CharacterId Who => CharacterId.Photographer;
    public override string IconKey => "photographer";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "x.burst", name = "연사", tagline = "자주 찍고 걸으며 처리 · 무리 · 기동",
            seed = "연속 촬영", seedDesc = "필름이 6장에서 12장으로 늘고, 셔터 간격이 40% 짧아지고, 틀이 20% 작아지며, 종이가 되는 시간이 2.5초에서 1.5초로 줄어듭니다.",
            cost = "셔터 피해가 35% 줄어듭니다.", strong = "무리 · 기동", weak = "보스 · 감기 공백",
            cards = new[] { P, S(5), S(6) }, evo = "xe.night",
            res1 = "1초 안에 같은 적을 세 번 찍으면 「필름 끊김」으로 공격력 100%를 한 번 더 주고 종이 시간을 새로 시작합니다.",
            res2 = "장노출 틀이 겹친 자리는 보스에게도 피해가 2배가 됩니다.",
            mastery = "필름 감기가 1.5초에서 0.6초로 줄고, 감는 동안에도 셔터를 누를 수 있습니다. (피해 50%)",
            power = 0.95f,          // 피해/초 ×1.08 · 넓이 ×0.64 → 무리 총량은 기본과 비슷 · 보스 공명 II 가 겹침 조건이라 0.95
            color = new Color(1f, 0.69f, 0.38f), form = "삼각대 카메라" },
        new PathDef {
            key = "x.tele", name = "망원", tagline = "멀리서 초점 한 장 · 보스 · 원거리",
            seed = "망원 렌즈", seedDesc = "사거리가 11칸에서 18칸으로 늘고, 틀이 35% 작아지며, 초점 원이 0.7칸에서 1.2칸으로, 초점 피해가 1.6배에서 2.2배로 커집니다.",
            cost = "나에게서 6칸 안의 적은 초점이 맞지 않아 피해가 50% 줄고 종이가 되지 않습니다.", strong = "보스 · 원거리", weak = "근접 포위",
            cards = new[] { A, S(7), S(9) }, evo = "xe.track",
            res1 = "초점이 맞은 적은 종이 시간이 2배가 되고, 보스는 초점 표식을 3개 얻습니다.",
            res2 = "표식 5개가 쌓인 보스를 초점으로 찍으면 「클로즈업」으로 표식을 모두 써서 공격력 400%를 줍니다.",
            mastery = "한 장에 초점이 둘이 됩니다.",
            power = 1.0f,           // 표식 5 를 쌓는 데 약 4장 → 장당 +100% 몫 · 보스 ±8% 안
            color = new Color(0.561f, 0.706f, 1f), form = null },
        new PathDef {
            key = "x.dark", name = "암실", tagline = "사본으로 홀리고 끌어당김 · 생존 · 제어",
            seed = "암실", seedDesc = "인화가 두 장(최고 · 두 번째)이 되고, 미끼가 3초에서 4.5초로 늘며, 사본이 찢어질 때 피해가 60% 늘어납니다.",
            cost = "필름이 6장에서 4장으로 줄고 필름 감기가 0.3초 느려집니다.", strong = "생존 · 제어", weak = "순간 화력",
            cards = new[] { B, S(2), S(8) }, evo = "xe.studio",
            res1 = "사본이 찢어진 자리에 0.8초 동안 암실 커튼이 서서 반지름 1.4칸 안의 적 투사체를 지웁니다.",
            res2 = "사본 3칸 안의 보스는 받는 피해가 15% 늘어납니다. (보스의 움직임은 그대로)",
            mastery = "한 번에 인화하는 사본 상한이 6장에서 10장이 되고, 인화가 세 장이 됩니다.",
            power = 1.0f,           // 필름 4장 = 감기가 1.5배 잦음 → 셔터 가동률 −12% 를 인화 2장 × 찢김 ×1.6 이 메움
            color = new Color(0.753f, 0.549f, 1f), form = null },
        new PathDef {
            key = "x.craft", name = "종이 공작", tagline = "종이를 퍼뜨려 연쇄 · 무리",
            seed = "종이 공작", seedDesc = "종이가 되는 시간이 2.5초에서 4초로 늘고, 종이가 된 적이 쓰러지면 종잇조각 2장이 기본으로 날아갑니다. (현상액이 더 늘립니다)",
            cost = "셔터 피해가 30% 줄어듭니다.", strong = "무리 연쇄", weak = "보스 · 빠른 적",
            cards = new[] { K, S(3), S(4) }, evo = "xe.confetti",
            res1 = "종잇조각에 맞은 적의 종이 시간이 0.8초에서 1.5초로 늘어납니다.",
            res2 = "종이 적 다섯 이상이 3칸 안에 모이면 「종이 더미」로 접혀 터집니다. (공격력 200% · 둘레 4칸 안 보스에게는 그 50%)",
            mastery = "종잇조각 수가 2배가 되고, 조각이 맞은 적에게서 한 번 더 튑니다.",
            power = 0.95f,          // 무리형 · 종이 약화 ×1.4 가 더 오래 붙어 −30% 를 메움 · 공명 II 가 보스 길
            color = new Color(0.949f, 0.949f, 0.949f), form = "영사기" },
        new PathDef {
            key = "x.flash", name = "플래시", tagline = "번쩍임으로 멈추고 지움 · 생존 · 탄막",
            seed = "스트로보", seedDesc = "셔터 순간 틀 바깥 1.5칸 테두리까지 번쩍여 그 안의 일반 적이 0.5초 멈추고(같은 적은 3초에 한 번), 틀 안의 적 투사체가 기본으로 지워집니다.",
            cost = "셔터 간격이 25% 늘어납니다.", strong = "생존 · 탄막 보스", weak = "화력",
            cards = new[] { S(0), S(1), M, G }, evo = "xe.typhoon",
            res1 = "틀 안에 내 몸을 넣고 찍으면 0.4초 동안 무적이 됩니다. (2초에 한 번)",
            res2 = "플래시로 지운 투사체마다 파노라마 게이지가 1% 찹니다. (셔터 한 장에 최대 5%)",
            mastery = "멈춤이 1초가 되고, 테두리가 3칸으로 넓어집니다.",
            power = 1.05f,          // 화력 대가(간격 +25%)를 power 로 조금 돌려줌 · 탄막 보스일수록 필살기가 잦음
            color = new Color(0.91f, 0.941f, 1f), form = "플래시 전구" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Burst, Craft, "xe.flipbook"),                     // 플립북 (S6 · S4)
        Cross(Tele, Dark, "xe.memorial"),                       // 기념 앨범 (S9 · S2)
        Cross(Dark, Craft, "x.x.puppet", "종이 인형극", "인화 사본이 종이 적처럼 펄럭이며 가까운 적에게 초당 1칸씩 다가가고, 닿으면 찢어집니다."),
        Cross(Burst, Flash, "x.x.strobe", "스트로보 연사", "세 번째 셔터마다 스트로보 테두리가 두 배 넓게 터집니다."),
    };

    // ================================================================= 묻기 (키트 · 고유 스킬 코드가 늘 이것으로 물음)
    public static bool On(int path) => Paths.Has(CharacterId.Photographer, path);
    public static bool ResAt(int path, int tier) => On(path) && Paths.Res(path) >= tier;
    public static bool Master(int path) => On(path) && Paths.Mastered(path);
    public static bool CrossIs(int cross) => Paths.Hero is PhotographerPaths && Paths.CrossOn(cross);

    // 셔터(평타) 피해 배율: 연사 −35% · 종이 공작 −30%
    public static float ShutterMul => (On(Burst) ? BurstCost : 1f) * (On(Craft) ? CraftCost : 1f);

    // ================================================================= 효과
    // 셔터 간격: 연사 ×0.6 (= 공속 ÷ 0.6) · 플래시 +25%
    public override float RateMul() => (On(Burst) ? 1f / BurstRate : 1f) * (On(Flash) ? 1f / FlashRate : 1f);

    // 암실 공명 II: 사본 3칸 안의 보스는 받는 피해 +15%
    public override float Outgoing(GameObject target, float dmg)
    {
        if (target == null || !ResAt(Dark, 2) || !Paths.IsBoss(target)) return 1f;
        CharacterKit kit = CharacterKit.Instance;
        return kit != null && kit.PhCopyNear(target.transform.position, DarkBossR) ? DarkBossMul : 1f;
    }

    // HUD 입문 칸: 망원 공명 II 면 보스 초점 표식 「3/5」 (미리 만든 표)
    static readonly string[] Marks = { "0/5", "1/5", "2/5", "3/5", "4/5", "5/5" };

    public override string Badge(int path)
    {
        if (path != Tele || Paths.Res(Tele) < 2) return null;
        CharacterKit kit = CharacterKit.Instance;
        if (kit == null) return null;
        int n = Mathf.Clamp(kit.PhMarkCount, 0, 5);
        return n > 0 ? Marks[n] : null;
    }

    // 셔터는 원래 흐름 그대로 (CharacterKit.PhotoAttack) — KitAttack 은 쓰지 않음
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
