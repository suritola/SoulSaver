using UnityEngine;

// 지도 제작자 갈래 5개 (기획서 3-6) — 데이터는 채워 둠 · 효과 훅은 지도 제작자 담당이 구현
// 지도 제작자 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Cartographer.cs · 기존 파일(CharacterKit.Cartographer.cs · SignatureSkills.Cartographer.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
public sealed class CartographerPaths : HeroPaths
{
    public const int Survey = 0, Stroke = 1, Ink = 2, Fold = 3, March = 4;

    public override CharacterId Who => CharacterId.Cartographer;
    public override string IconKey => "cartographer";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "c.survey", name = "정밀 측량", tagline = "네 획 사각형 · 무리 일괄",
            seed = "정밀 측량", seedDesc = "도형이 네 획마다 사각형으로 닫힙니다. (넓이 ×1.8 · 피해 ×1.6)",
            cost = "도형이 닫히는 주기가 느려집니다.", strong = "무리 일괄 · 보스 가두기", weak = "주기",
            cards = new[] { A, S(0), S(8) }, evo = "ke.world",
            res1 = "도형 안의 적에게 측량 표식이 저절로 붙습니다.",
            res2 = "직전 도형과 겹친 곳은 피해가 2배입니다.",
            mastery = "오각형까지 그립니다.",
            color = new Color(0.45f, 0.65f, 1f), form = "측량 사슬" },
        new PathDef {
            key = "c.stroke", name = "일필", tagline = "도형 대신 굵은 한 획",
            seed = "일필휘지", seedDesc = "도형이 닫히지 않는 대신 모든 획의 길이 ×1.4 · 굵기 ×1.5 · 피해 ×1.7.",
            cost = "도형 효과가 없습니다.", strong = "보스 · 일렬", weak = "포위",
            cards = new[] { M, S(2), S(4) }, evo = "ke.bold",
            res1 = "여백 대기 시간이 3초에서 1.5초로 줄어듭니다.",
            res2 = "획 끝에 먹이 튀며 작게 터집니다.",
            mastery = "획이 왕복합니다.",
            color = new Color(0.85f, 0.85f, 0.95f) },
        new PathDef {
            key = "c.ink", name = "먹물", tagline = "획이 잉크 웅덩이 · 지역",
            seed = "먹물병 엎기", seedDesc = "모든 획이 4초 동안 잉크 웅덩이를 남깁니다. (초당 공격력 30%)",
            cost = "획 피해가 30% 줄어듭니다.", strong = "지역 무리", weak = "이동 보스",
            cards = new[] { P, S(9), K }, evo = "ke.blot",
            res1 = "웅덩이 위의 적이 느려집니다.",
            res2 = "웅덩이 위에서 쓰러진 적에게서 잉크 폭풍이 일어납니다.",
            mastery = "웅덩이가 8초 남습니다.",
            color = new Color(0.35f, 0.4f, 0.8f), form = "잉크병" },
        new PathDef {
            key = "c.fold", name = "접지술", tagline = "지도 접기 중심 · 필살기 폭딜",
            seed = "접지술", seedDesc = "지도 접기 게이지 +60%, 도형을 닫을 때마다 게이지가 5% 찹니다.",
            cost = "획 피해가 15% 줄어듭니다.", strong = "필살기 폭딜 · 보스", weak = "주기 사이",
            cards = new[] { G, S(3), S(1) }, evo = "ke.constellation",
            res1 = "접힌 선이 3초 동안 남아 벱니다.",
            res2 = "지도 접기를 연달아 두 번 쓰면 교차점에 북극성이 뜹니다.",
            mastery = "지도 접기를 2번 모아 둡니다.",
            color = new Color(1f, 0.85f, 0.45f) },
        new PathDef {
            key = "c.march", name = "행군 측량", tagline = "걸어간 길이 선 · 기동 생존",
            seed = "행군 측량", seedDesc = "걸어간 길이 잉크 선이 되어 닿는 적을 0.3초마다 벱니다. (공격력 25%) 지나온 길이 고리로 닫히면 안쪽을 도형처럼 짓이깁니다.",
            cost = "평타 획 피해가 30% 줄어듭니다.", strong = "기동 · 생존 · 무리", weak = "보스 (계속 걸어야 함)",
            cards = new[] { B, S(5), S(6), S(7) }, evo = "ke.express",
            res1 = "닫힌 고리 안의 적 투사체를 지웁니다.",
            res2 = "고리를 닫으면 1.5초 동안 이동 속도 +30% · 받는 피해 −30%.",
            mastery = "고리 피해가 2배가 됩니다.",
            color = new Color(0.5f, 0.95f, 0.6f), form = "지구본" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Ink, Fold, "ke.relief"),                      // 입체 지도 (K · S1)
        Cross(March, Survey, "ke.treasure"),                // 황금 항로 (S7 · S8)
        Cross(March, Ink, "c.x.inkmarch", "잉크 행군", "걸어간 길이 잉크 웅덩이가 됩니다."),
        Cross(Survey, Stroke, "c.x.blueprint", "대형 도면", "여백 획 하나가 도형을 바로 닫습니다."),
    };

    // ================================================================= 효과 (지도 제작자 담당이 채움 · 비어 있어도 게임은 그대로 진행)
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
