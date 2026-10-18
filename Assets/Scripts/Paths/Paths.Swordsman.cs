using UnityEngine;

// 검사 갈래 5개 (기획서 3-2) — 데이터는 채워 둠 · 효과 훅은 검사 담당이 구현
// 검사 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Swordsman.cs · 검사 기존 파일(SignatureSkills.Swordsman.cs · 새로 만들 CharacterKit.Swordsman.cs ·
//   CharacterKit.cs 안의 검사 전용 부분만) — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
public sealed class SwordsmanPaths : HeroPaths
{
    public const int Wave = 0, Parry = 1, Blood = 2, Dance = 3, Whirl = 4;

    public override CharacterId Who => CharacterId.Swordsman;
    public override string IconKey => "swordsman";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "s.wave", name = "검기", tagline = "휘두를 때마다 검기 · 안전 거리",
            seed = "검기 방출", seedDesc = "휘두를 때마다 검기가 반드시 날아갑니다. (9칸 · 공격력 70% · 관통) 휘두르기 범위는 20% 줄어듭니다.",
            cost = "근접 특성(휘두르는 동안 받는 피해 절반)이 사라집니다.", strong = "안전 거리 · 일렬 · 보스", weak = "근접 생존력",
            cards = new[] { P, S(1), S(6) }, evo = "se.trail",
            res1 = "검기로 같은 적을 세 번 맞히면 균열이 바로 터집니다.",
            res2 = "검기가 두 줄기 X자로 날아가고, 교차점에서 잔상이 터집니다.",
            mastery = "검기가 끝에서 되돌아옵니다.",
            color = new Color(0.6f, 0.8f, 1f), form = "채찍검" },
        new PathDef {
            key = "s.parry", name = "받아치기", tagline = "막기 0.25초 · 반격",
            seed = "받아치기", seedDesc = "우클릭을 누른 첫 0.25초가 막기가 됩니다. 그사이 맞으면 피해 0 · 0.6초 무적 · 다음 베기 ×3. 실패해도 손해는 없습니다.",
            cost = "회전 베기를 끝까지 모으는 시간이 30% 늘어납니다.", strong = "보스 탄막 · 생존", weak = "무리 · 손 기술",
            cards = new[] { K, S(0), A, S(7) }, evo = "se.counter",
            res1 = "막기에 성공하면 둘레의 적 투사체를 모두 되돌려 보냅니다.",
            res2 = "3초 안에 두 번 막으면 4초 동안 역습 자세가 됩니다. (사거리 ×1.4 · 피해 ×1.5)",
            mastery = "막기가 0.4초로 늘고, 성공할 때마다 필살기 게이지가 20% 찹니다.",
            color = new Color(1f, 0.9f, 0.5f) },
        new PathDef {
            key = "s.blood", name = "피의 맹세", tagline = "체력을 바쳐 흡혈 · 근접 지속",
            seed = "피의 맹세", seedDesc = "휘두를 때마다 지금 체력의 2%를 바치고, 베기 피해가 45% 늘며 흡혈이 2배가 됩니다.",
            cost = "물약 · 재생 회복이 50% 줄어듭니다.", strong = "근접 무리 지속", weak = "원거리 탄막",
            cards = new[] { M, S(2), S(9) }, evo = "se.blood",
            res1 = "체력이 50% 아래면 피해 +25%, 25% 아래면 +50%입니다.",
            res2 = "혈갑 보호막이 가득 차면 베기가 피의 파동이 되어 길게 뻗습니다.",
            mastery = "장마다 한 번 체력 1로 버티고, 3초 동안 흡혈이 3배가 됩니다.",
            color = new Color(0.95f, 0.25f, 0.3f) },
        new PathDef {
            key = "s.dance", name = "검무", tagline = "미끄러지며 베기 · 기동",
            seed = "검무 보법", seedDesc = "움직이며 휘두르면 진행 방향으로 1.5칸 미끄러지며 벱니다. 움직이는 동안 공격 속도 +15%.",
            cost = "멈춰서 휘두르면 공격 속도가 20% 느려집니다.", strong = "기동 · 무리", weak = "서서 싸우는 보스 · 결계",
            cards = new[] { B, S(5), S(3) }, evo = "se.dance",
            res1 = "미끄러진 길에 칼날 잔상이 남습니다.",
            res2 = "검무가 최대 중첩이면 다섯 번째 휘두르기마다 회전 베기(30%)가 저절로 나갑니다.",
            mastery = "미끄러질 때 0.1초 무적이 되고, 2.5칸을 미끄러집니다.",
            color = new Color(0.55f, 1f, 0.75f), form = "기창" },
        new PathDef {
            key = "s.whirl", name = "회오리", tagline = "원형 베기 · 걸으며 모으기",
            seed = "회전 검사", seedDesc = "평타가 몸 둘레 원형 베기가 됩니다. (반경 85% · 피해 80%) 회전 베기를 모으는 동안 60% 속도로 걸을 수 있습니다.",
            cost = "한 적에게 주는 피해가 낮습니다.", strong = "포위 무리", weak = "보스 단일",
            cards = new[] { G, S(4), S(8) }, evo = "se.eye",
            res1 = "모으는 동안 강철 의지 끌어당김이 기본 4칸으로 생깁니다.",
            res2 = "회전 베기가 끝난 자리에 2초 동안 소용돌이가 남습니다.",
            mastery = "회전 베기를 놓지 않고 게이지를 쓰며 계속 돕니다.",
            color = new Color(0.75f, 0.6f, 1f), form = "전쟁 망치" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Wave, Parry, "se.giant"),                     // 거인 처단 (S6 · S7)
        Cross(Whirl, Blood, "se.vortex"),                   // 영혼 회오리 (S8 · S9)
        Cross(Wave, Blood, "s.x.bloodwave", "피의 검기", "검기가 흡혈하고, 체력을 바치는 대신 검기가 강해집니다."),
        Cross(Dance, Whirl, "s.x.bladestorm", "칼날 폭풍", "미끄러질 때 원형 베기가 일어납니다."),
    };

    // ================================================================= 효과 (검사 담당이 채움 · 비어 있어도 게임은 그대로 진행)
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
    public override float RateMul() => 1f;
    public override void Tick() { }
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
