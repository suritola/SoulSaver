using UnityEngine;

// 시계공 갈래 5개 (기획서 3-7) — 데이터는 채워 둠 · 효과 훅은 시계공 담당이 구현
// 시계공 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Clockmaker.cs · 기존 파일(CharacterKit.Clockmaker.cs · SignatureSkills.Clockmaker.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
public sealed class ClockmakerPaths : HeroPaths
{
    public const int Swarm = 0, Grand = 1, Travel = 2, Echo = 3, Debt = 4;

    public override CharacterId Who => CharacterId.Clockmaker;
    public override string IconKey => "clockmaker";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "t.swarm", name = "톱니 떼", tagline = "두 개씩 튕기기 · 무리",
            seed = "톱니 떼", seedDesc = "톱니를 두 개씩 던지고 (피해 65%), 튕김이 1회 늘어납니다.",
            cost = "정각까지 필요한 눈금이 4 늘어납니다.", strong = "무리", weak = "보스",
            cards = new[] { P, S(2), B }, evo = "te.gearstorm",
            res1 = "튕길 때마다 녹이 쌓입니다.",
            res2 = "다섯 번 넘게 튕긴 톱니가 갈라집니다.",
            mastery = "톱니를 세 개씩 던집니다.",
            color = new Color(0.85f, 0.75f, 0.55f), form = "뻐꾸기 시계" },
        new PathDef {
            key = "t.grand", name = "대시계", tagline = "거대 정각 톱니 · 보스 폭딜",
            seed = "대시계", seedDesc = "정각 톱니의 크기와 폭발 범위가 2배, 피해가 1.5배가 됩니다.",
            cost = "보통 톱니의 피해가 25% 줄어듭니다.", strong = "보스 · 폭딜", weak = "주기 공백",
            cards = new[] { M, S(4), S(6), A }, evo = "te.clockwork",
            res1 = "정각 톱니가 터지기 0.5초 전에 적을 끌어당깁니다.",
            res2 = "정각이 두 번 이어지면 세 번째는 대정각이 됩니다. (화면 1/3)",
            mastery = "세 번째 톱니마다 정각이 됩니다.",
            color = new Color(1f, 0.8f, 0.35f), form = "괘종시계" },
        new PathDef {
            key = "t.travel", name = "시간 여행", tagline = "잦은 되감기 · 무적 · 생존",
            seed = "시간 여행", seedDesc = "되감기 재사용 대기 −40%, 3초 전 대신 2초 전으로 돌아가고, 되감은 뒤 1초 동안 무적입니다.",
            cost = "되갚는 피해가 40% 줄어듭니다.", strong = "생존", weak = "화력",
            cards = new[] { S(1), S(3), S(7) }, evo = "te.timeless",
            res1 = "출발점에서 도착점까지 선 위의 적이 0.5초 멈춥니다.",
            res2 = "체력이 낮을수록 되감기 게이지가 빨리 찹니다.",
            mastery = "되감기를 2번 모아 둡니다.",
            color = new Color(0.6f, 0.8f, 1f), form = "모래시계" },
        new PathDef {
            key = "t.echo", name = "잔상", tagline = "모든 톱니가 두 번 · 균형",
            seed = "잔상 장인", seedDesc = "모든 톱니가 0.6초 뒤 잔상으로 한 번 더 날아갑니다. (피해 50%)",
            cost = "본 톱니의 피해가 20% 줄어듭니다.", strong = "무리 · 보스 균형", weak = "빠른 적 (지연)",
            cards = new[] { K, S(5), G }, evo = "te.echoes",
            res1 = "잔상도 정각 눈금을 채웁니다.",
            res2 = "잔상이 한 번 더 잔상을 남깁니다.",
            mastery = "잔상 피해가 80%가 됩니다.",
            color = new Color(0.8f, 0.65f, 1f) },
        new PathDef {
            key = "t.debt", name = "시간 빚", tagline = "필살기 두 배 · 맞으면 손해",
            seed = "시간 저당", seedDesc = "필살기 게이지가 2배로 찹니다.",
            cost = "맞으면 필살기 게이지를 10% 잃습니다.", strong = "필살기 회전 · 보스", weak = "피격",
            cards = new[] { S(8), S(9), S(0) }, evo = "te.debt",
            res1 = "게이지가 가득 차 있으면 공격 속도가 20% 오릅니다.",
            res2 = "되감기가 지난 3초의 피해를 다시 줍니다. (스톱워치가 기본으로)",
            mastery = "맞아도 게이지를 잃지 않습니다.",
            color = new Color(1f, 0.5f, 0.4f) },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Grand, Debt, "te.chime"),                     // 대종 (A · S0)
        Cross(Echo, Travel, "te.paradox"),                  // 역설 (G · S7)
        Cross(Swarm, Echo, "t.x.gearecho", "톱니 메아리", "갈라진 톱니도 잔상을 남깁니다."),
        Cross(Grand, Travel, "t.x.noonrewind", "정각 되감기", "되감은 자리에서 정각 톱니가 터집니다."),
    };

    // ================================================================= 효과 (시계공 담당이 채움 · 비어 있어도 게임은 그대로 진행)
    public override void OnSeed(int path, bool replay) { }
    public override void OnSingle(int path, bool replay) { }
    public override void OnResonance(int path, int tier, bool replay) { }
    public override void OnComplete(int path, bool replay) { }
    public override void OnMastery(int path, bool replay) { }
    public override void OnCross(int cross, bool replay) { }
    public override void End() { }

    public override float Outgoing(GameObject target, float dmg) => 1f;
    public override void Hit(GameObject target, float dmg, bool killed, bool proc) { }
    public override float Taken(float dmg) => dmg;
    public override void Hurt(float taken) { }
    public override void Killed(Vector3 pos) { }
    public override float RateMul() => 1f;
    public override void Tick() { }
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
