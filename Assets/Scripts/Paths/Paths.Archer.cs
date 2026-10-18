using UnityEngine;

// 궁수 갈래 5개 (기획서 3-4) — 데이터는 채워 둠 · 효과 훅은 궁수 담당이 구현
// 궁수 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Archer.cs · 궁수 기존 파일(SignatureSkills.Archer.cs · 새로 만들 CharacterKit.Archer.cs ·
//   CharacterKit.cs 안의 궁수 전용 부분만) — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
public sealed class ArcherPaths : HeroPaths
{
    public const int Long = 0, Rapid = 1, Trap = 2, Wind = 3, Sky = 4;

    public override CharacterId Who => CharacterId.Archer;
    public override string IconKey => "archer";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "a.long", name = "장궁", tagline = "길게 당긴 관통 화살",
            seed = "장궁", seedDesc = "당기는 시간이 40% 늘고, 「완벽!」 화살이 피해 ×1.8로 모든 적을 꿰뚫습니다.",
            cost = "덜 당긴 화살의 피해가 40% 줄어듭니다.", strong = "보스 · 일렬", weak = "근접 압박",
            cards = new[] { P, S(5), S(1), A }, evo = "ae.bow",
            res1 = "완벽 화살이 꿰뚫은 적 수만큼 다음 당김이 빨라집니다.",
            res2 = "집중 호흡 5중첩이면 완벽 화살이 굵은 관통 광선이 됩니다.",
            mastery = "완벽 판정 폭이 2배가 됩니다.",
            color = new Color(0.45f, 0.8f, 1f), form = "투창" },
        new PathDef {
            key = "a.rapid", name = "속사", tagline = "누르는 동안 연사 · 기동 무리",
            seed = "단궁 속사", seedDesc = "당기지 않고 누르는 동안 초당 4발을 쏩니다. (피해 40%) 당길 때 느려지지 않고 숨 고르기도 없습니다.",
            cost = "관통이 없어지고, 「완벽 화살」 카드는 「다섯 발마다」로 바뀝니다.", strong = "기동 · 무리", weak = "보스 한 발",
            cards = new[] { M, S(3), S(4) }, evo = "ae.echo",
            res1 = "메아리 화살이 세 발마다 나갑니다.",
            res2 = "같은 적에 열 발을 맞히면 울림 범위가 2배가 됩니다.",
            mastery = "초당 6발을 쏩니다.",
            color = new Color(1f, 0.85f, 0.4f), form = "속사 활" },
        new PathDef {
            key = "a.trap", name = "덫", tagline = "완벽 화살이 덫 · 지역 장악",
            seed = "덫 놓기", seedDesc = "완벽 화살이 떨어진 자리에 덫을 놓습니다. (최대 5개 · 20초 · 묶고 공격력 120%)",
            cost = "화살의 직접 피해가 20% 줄어듭니다.", strong = "지역 장악 · 카이팅 생존", weak = "움직이는 보스",
            cards = new[] { G, S(2), S(7) }, evo = "ae.forest",
            res1 = "덫이 터지면 가시 덤불이 자랍니다.",
            res2 = "묶인 적 둘이 가까우면 덫 사이에 가시줄이 이어집니다.",
            mastery = "덫이 10개까지 놓이고, 보스도 0.5초 묶습니다.",
            color = new Color(0.55f, 0.9f, 0.4f), form = "그물 활" },
        new PathDef {
            key = "a.wind", name = "바람", tagline = "당기며 달리기 · 생존",
            seed = "바람 걸음", seedDesc = "당기는 동안 느려지지 않고, 완벽 화살마다 1.5초 동안 이동 속도가 20% 오릅니다.",
            cost = "숨이 2배 빨리 닳습니다.", strong = "기동 · 생존", weak = "화력 중간",
            cards = new[] { K, S(0), S(6) }, evo = "ae.storm",
            res1 = "바람길 위에서 당기면 바로 완벽 화살이 됩니다.",
            res2 = "후퇴 사격 뒤 1초 동안 투사체가 몸을 통과합니다.",
            mastery = "바람길이 이어지면 저절로 연사합니다.",
            color = new Color(0.7f, 1f, 0.9f) },
        new PathDef {
            key = "a.sky", name = "천공", tagline = "화살비 중심 · 지역 섬멸",
            seed = "하늘 사수", seedDesc = "화살비 게이지 +50%, 범위 ×1.4, 평타 명중마다 게이지가 더 찹니다.",
            cost = "평타 피해가 25% 줄어듭니다.", strong = "지역 섬멸", weak = "게이지 공백",
            cards = new[] { B, S(8), S(9) }, evo = "ae.sky",
            res1 = "화살비 안의 적에게 사냥감 표식이 저절로 붙습니다.",
            res2 = "화살비 동안 평타가 하늘로 쏴져 화살비 안의 적에게 떨어집니다.",
            mastery = "화살비가 두 개 동시에 쏟아집니다.",
            color = new Color(0.75f, 0.6f, 1f) },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Long, Trap, "ae.master"),                     // 명궁의 호흡 (A · S1 · S7)
        Cross(Sky, Rapid, "ae.brand"),                      // 사냥꾼의 낙인 (B · S4)
        Cross(Long, Sky, "a.x.meteorpierce", "유성 관통", "완벽 화살이 화살비 게이지를 10% 채웁니다."),
        Cross(Wind, Trap, "a.x.gusttrap", "돌풍 덫", "물러날 때 발밑에 덫을 놓습니다."),
    };

    // ================================================================= 효과 (궁수 담당이 채움 · 비어 있어도 게임은 그대로 진행)
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
    public override void Killed(Vector3 pos) { }
    public override float MoveMul() => 1f;
    public override void Tick() { }
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
