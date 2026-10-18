using UnityEngine;

// 도적 갈래 5개 (기획서 3-3) — 데이터는 채워 둠 · 효과 훅은 도적 담당이 구현
// 도적 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Rogue.cs · 도적 기존 파일(SignatureSkills.Rogue.cs · 새로 만들 CharacterKit.Rogue.cs ·
//   CharacterKit.cs 안의 도적 전용 부분만) — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
public sealed class RoguePaths : HeroPaths
{
    public const int Bleed = 0, Shadow = 1, Boomerang = 2, Dash = 3, Frenzy = 4;

    public override CharacterId Who => CharacterId.Rogue;
    public override string IconKey => "rogue";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "r.bleed", name = "출혈", tagline = "5중첩 출혈 · 전염",
            seed = "독 바른 칼날", seedDesc = "모든 표창이 출혈을 입히고, 출혈이 5중첩까지 쌓입니다.",
            cost = "직접 피해가 35% 줄어듭니다.", strong = "보스 지속 · 무리 전염", weak = "즉시 처치",
            cards = new[] { M, S(1), S(2), A }, evo = "re.sea",
            res1 = "5중첩 적을 돌진으로 지나가면 출혈 폭발이 바로 터집니다.",
            res2 = "출혈로 쓰러진 적이 둘레 둘에게 출혈 2중첩을 옮깁니다.",
            mastery = "출혈 피해가 1.6배가 되고, 보스에게는 10중첩까지 쌓입니다.",
            color = new Color(0.9f, 0.2f, 0.3f), form = "독침 대롱" },
        new PathDef {
            key = "r.shadow", name = "그림자", tagline = "돌진 2회 · 미끼 분신",
            seed = "그림자 걸음", seedDesc = "돌진을 2번 모아 둘 수 있고, 돌진을 시작한 자리에 미끼 분신이 남습니다.",
            cost = "돌진 출혈이 30% 줄어듭니다.", strong = "생존 · 회피 · 보스 시선 돌리기", weak = "화력",
            cards = new[] { K, S(0), S(8) }, evo = "re.phantom",
            res1 = "분신이 사라질 때 연막이 퍼집니다.",
            res2 = "연막 안에서 던진 표창은 치명타(×2)가 됩니다.",
            mastery = "돌진을 3번 모아 두고, 분신도 표창을 함께 던집니다.",
            color = new Color(0.55f, 0.4f, 0.85f), form = "살상 와이어" },
        new PathDef {
            key = "r.return", name = "회귀 칼날", tagline = "되돌아오는 표창 · 탄 경제",
            seed = "곡선 투척", seedDesc = "표창이 8칸 끝에서 되돌아오며 다시 벱니다. 돌아온 표창을 몸으로 받으면 탄창이 한 발 찹니다.",
            cost = "사거리가 무한에서 8칸으로 줄어듭니다.", strong = "근중거리 무리 · 탄 경제", weak = "원거리",
            cards = new[] { B, S(4), S(6) }, evo = "re.return",
            res1 = "왕복으로 같은 적을 맞히면 던지기 연습이 2중첩 쌓입니다.",
            res2 = "돌아올 때 몸 둘레를 한 바퀴 돈 뒤 돌아옵니다.",
            mastery = "표창이 두 번 왕복합니다.",
            color = new Color(0.5f, 0.85f, 0.95f) },
        new PathDef {
            key = "r.dash", name = "질주 암살", tagline = "긴 돌진 직접 피해 · 순간 화력",
            seed = "질주 암살", seedDesc = "돌진 거리 ×1.5, 돌진 게이지 −30%, 돌진 경로가 출혈 대신 직접 피해를 줍니다. (공격력 200%)",
            cost = "평타 공격 속도가 20% 느려집니다.", strong = "기동 · 관통 · 보스 순간 화력", weak = "게이지 의존",
            cards = new[] { G, S(5), S(3) }, evo = "re.road",
            res1 = "돌진으로 처치하면 게이지 25%를 돌려받습니다.",
            res2 = "궤적 칼날이 X자로 겹치면 교차점이 터집니다.",
            mastery = "돌진 중 처치하면 바로 다시 돌진합니다. (3연속)",
            color = new Color(0.95f, 0.75f, 0.3f) },
        new PathDef {
            key = "r.frenzy", name = "광란", tagline = "처치마다 빨라짐 · 무리",
            seed = "광란", seedDesc = "탄창 6 → 10, 공격 속도 +25%, 처치할 때마다 1.5초 동안 공격 속도 +3%. (10중첩)",
            cost = "받는 피해가 15% 늘어납니다.", strong = "무리", weak = "피격 위험",
            cards = new[] { H, S(7), S(9), P }, evo = "re.peak",
            res1 = "10중첩이면 표창을 두 개씩 던집니다.",
            res2 = "빈 주머니의 비가 10처치마다도 내립니다.",
            mastery = "맞아도 중첩이 떨어지지 않습니다.",
            color = new Color(1f, 0.45f, 0.2f), form = "도박 카드" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Bleed, Dash, "re.executioner"),               // 처형자의 길 (재료 변경: A · S3)
        Cross(Frenzy, Boomerang, "re.acrobat"),             // 칼날 곡예 (P · S6)
        Cross(Shadow, Bleed, "r.x.shadowbleed", "그림자 출혈", "분신의 표창이 출혈 2중첩을 입힙니다."),
        Cross(Dash, Frenzy, "r.x.stormdash", "폭풍 질주", "돌진이 광란 5중첩을 바로 채웁니다."),
    };

    // ================================================================= 효과 (도적 담당이 채움 · 비어 있어도 게임은 그대로 진행)
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
    public override void Attacked(Vector3 start, Vector2 dir) { }
    public override void Killed(Vector3 pos) { }
    public override float MoveMul() => 1f;
    public override void Tick() { }
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
