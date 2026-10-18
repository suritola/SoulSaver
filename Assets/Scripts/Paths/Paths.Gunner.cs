using UnityEngine;

// 거너 갈래 5개 (기획서 3-1) — 데이터는 채워 둠 · 효과 훅은 거너 담당이 구현
// 거너 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Gunner.cs · 거너 기존 파일(SignatureSkills.Gunner.cs · SpecialAbilities.GunCards.cs ·
//   PlayerController.cs 의 거너 사격 부분) — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
public sealed class GunnerPaths : HeroPaths
{
    public const int Snipe = 0, Ricochet = 1, Overheat = 2, Shotgun = 3, Turret = 4;

    public override CharacterId Who => CharacterId.Gunner;
    public override string IconKey => "gunner";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "g.snipe", name = "관통 저격", tagline = "멈춰서 모아 쏘는 관통 저격탄",
            seed = "저격 자세", seedDesc = "멈춰 있으면 0.6초마다 조준이 모여 다음 탄이 저격탄이 됩니다. (모든 적 관통 · 피해 ×2.2 · 탄속 ×1.6)",
            cost = "움직이며 쏜 탄의 피해가 25% 줄고, 탄창이 8발에서 6발로 줄어듭니다.", strong = "보스 · 일렬", weak = "기동 · 포위",
            cards = new[] { G, S(3), S(9) }, evo = "ge.seeker",
            res1 = "저격탄이 약점에 맞으면 피해 ×1.5와 함께 작은 폭발이 일어납니다.",
            res2 = "저격탄이 꿰뚫은 적 수만큼 다음 조준이 빨라집니다. (5마리면 즉시)",
            mastery = "일제 사격(우클릭)이 모두 저격탄이 됩니다.",
            color = new Color(0.55f, 0.85f, 1f), form = "영혼 저격총" },
        new PathDef {
            key = "g.ricochet", name = "도탄 난사", tagline = "큰 탄창 · 튕기는 난사",
            seed = "난사", seedDesc = "탄창이 14발로 늘고 연사가 1.35배 빨라지며, 모든 탄이 한 번 튕깁니다. (퍼짐 ±8°)",
            cost = "한 발의 피해가 30% 줄어듭니다.", strong = "무리", weak = "보스 단일 · 탄 관리",
            cards = new[] { P, S(1), A, S(8) }, evo = "ge.storm",
            res1 = "튕길 때마다 다음 튕김의 피해가 15% 늘어납니다. (최대 3회)",
            res2 = "투기장 경계에 닿은 탄도 되튕기고, 사라질 때 파편 두 개가 튑니다.",
            mastery = "튕김 횟수가 무제한이 됩니다. (튕길 때마다 피해 −10%)",
            color = new Color(1f, 0.85f, 0.35f), form = "쌍권총" },
        new PathDef {
            key = "g.overheat", name = "화염 과열", tagline = "장전 대신 열 게이지 · 과열 폭발",
            seed = "과열 사격", seedDesc = "장전 대신 열 게이지가 찹니다. 열 50% 위에서는 모든 탄이 화상을 입히고, 100%가 되면 2초 동안 과열되며 몸 둘레에 화염 폭발이 일어납니다. (공격력 250%)",
            cost = "과열된 2초 동안 쏠 수 없고, 직접 피해가 15% 줄어듭니다.", strong = "지속 광역 · 보스 화상", weak = "과열 순간",
            cards = new[] { M, K, S(2) }, evo = "ge.furnace",
            res1 = "화상이 3중첩까지 쌓입니다.",
            res2 = "과열 폭발 자리에 4초 동안 불 장판이 남고, 열 90~100%에서 쏜 탄은 피해 ×1.5입니다.",
            mastery = "과열 시간이 0.8초로 줄고 폭발이 두 번 일어납니다.",
            color = new Color(1f, 0.5f, 0.2f), form = "화염 방사기" },
        new PathDef {
            key = "g.shotgun", name = "산탄 돌격", tagline = "탄창 4 산탄 · 장전 돌진",
            seed = "산탄 장전", seedDesc = "평타가 6발 산탄이 됩니다. (사거리 6칸 · 40° · 가까울수록 셈) 탄창은 4발, 장전은 40% 빠르고, 장전을 시작하면 이동 방향으로 2.5칸 돌진합니다. (0.15초 무적)",
            cost = "6칸 밖의 적은 거의 맞히지 못합니다.", strong = "근접 무리 · 생존", weak = "원거리 · 탄막 보스",
            cards = new[] { B, S(4), S(7) }, evo = "ge.thunder",
            res1 = "돌진에 부딪힌 적을 밀치고 다음 산탄의 피해가 40% 늘어납니다.",
            res2 = "탄창의 마지막 발이 일직선으로 꿰뚫는 슬러그가 됩니다. (공격력 300%)",
            mastery = "돌진을 두 번 연달아 하고, 끝에 충격파가 터집니다.",
            color = new Color(1f, 0.4f, 0.35f), form = "산탄총" },
        new PathDef {
            key = "g.turret", name = "포탑 설치", tagline = "장전할 때 포탑 · 진지 방어",
            seed = "거치 포탑", seedDesc = "장전을 마칠 때마다 발밑에 포탑이 서서 0.4초마다 가까운 적을 쏩니다. (공격력 45% · 최대 2개 · 12초)",
            cost = "내 공격 속도가 20% 느려집니다.", strong = "진지 방어 · 서 있는 보스", weak = "이동 강제 (침식 · 결계)",
            cards = new[] { S(0), S(5), S(6) }, evo = "ge.dance",
            res1 = "포탑이 3개까지 서고, 포탑 4칸 안에 서 있으면 장전이 2배 빨라집니다.",
            res2 = "포탑이 0.8초마다 적 투사체 하나를 쏴서 지우고, 사라질 때 폭발합니다.",
            mastery = "포탑이 사라지지 않고 4개까지 서며, 탄피 지뢰를 깝니다.",
            color = new Color(0.5f, 0.95f, 0.5f), form = "용암 유탄" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Ricochet, Turret, "ge.thunderback"),          // 뇌격 반격 (A · S5)
        Cross(Ricochet, Snipe, "ge.outlaw"),                // 무법자 (S8 · S9)
        Cross(Snipe, Overheat, "g.x.apinc", "철갑 소이탄", "저격탄이 지나간 줄 전체에 화상이 3중첩으로 붙습니다."),
        Cross(Overheat, Shotgun, "g.x.flamedash", "화염 돌진", "과열 폭발이 돌진 끝에서 터지고, 과열 중에도 돌진할 수 있습니다."),
    };

    // ================================================================= 효과 (거너 담당이 채움 · 비어 있어도 게임은 그대로 진행)
    // 입문 · 공명 · 극의 · 교차가 켜졌는지는 언제든 Paths.Has(Snipe) · Paths.Res(Snipe) >= 1 · Paths.Mastered(Snipe) · Paths.CrossOn(2) 로 물어도 됨
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
}
