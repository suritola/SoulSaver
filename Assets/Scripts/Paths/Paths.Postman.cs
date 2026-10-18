using UnityEngine;

// 우체부 갈래 5개 (기획서 3-8) — 데이터는 채워 둠 · 효과 훅은 우체부 담당이 구현
// 우체부 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Postman.cs · 기존 파일(CharacterKit.Postman.cs · SignatureSkills.Postman.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
public sealed class PostmanPaths : HeroPaths
{
    public const int Round = 0, Parcel = 1, Express = 2, Bike = 3, LongHaul = 4;

    public override CharacterId Who => CharacterId.Postman;
    public override string IconKey => "postman";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "p.round", name = "순회 배달", tagline = "편지가 네 명을 돎 · 무리",
            seed = "순회 배달", seedDesc = "편지가 닿은 뒤 다음으로 가장 먼 적에게 계속 갑니다. (최대 4번 · 15%씩 약해짐)",
            cost = "편지 속도가 15% 느려집니다.", strong = "무리", weak = "보스 (약해짐)",
            cards = new[] { P, B, S(4) }, evo = "pe.return",
            res1 = "마지막 받는 이가 쓰러지면 처음 적에게 돌아갑니다.",
            res2 = "네 번을 다 돌면 마지막 적에 소인이 찍힙니다.",
            mastery = "최대 8번 돕니다.",
            color = new Color(0.95f, 0.55f, 0.45f), form = "비둘기 우편" },
        new PathDef {
            key = "p.parcel", name = "무거운 소포", tagline = "느린 폭발 소포 · 밀집 무리",
            seed = "무거운 소포", seedDesc = "편지 대신 느린 소포를 보냅니다. (피해 ×1.8 · 반경 2칸 폭발)",
            cost = "공격 속도가 35% 느려집니다.", strong = "밀집 무리", weak = "빠른 적 · 근접",
            cards = new[] { M, S(3), S(9) }, evo = "pe.parcel",
            res1 = "터진 자리에서 우표 조각 두 개가 작게 터집니다.",
            res2 = "세 번째 소포마다 대형 소포가 됩니다.",
            mastery = "폭발 반경이 1.5배가 됩니다.",
            color = new Color(0.8f, 0.6f, 0.35f), form = "소포 상자" },
        new PathDef {
            key = "p.express", name = "속달", tagline = "표식 폭발 중심 · 무리 폭딜",
            seed = "속달 전문", seedDesc = "표식이 2배 빨리 붙고, 속달 게이지가 25% 덜 듭니다.",
            cost = "평타 편지의 피해가 25% 줄어듭니다.", strong = "무리 폭딜", weak = "평타 공백",
            cards = new[] { G, S(7), S(0), S(1) }, evo = "pe.reply",
            res1 = "표식이 붙은 적은 받는 피해가 15% 늘어납니다.",
            res2 = "속달이 터질 때마다 다음 속달 게이지가 10% 찹니다.",
            mastery = "속달을 두 번 연달아 씁니다.",
            color = new Color(1f, 0.85f, 0.4f), form = "우체통" },
        new PathDef {
            key = "p.bike", name = "자전거 배달", tagline = "달리면 편지가 저절로 · 기동",
            seed = "자전거 배달", seedDesc = "이동 속도 +20%, 움직이는 동안 0.5초마다 가장 가까운 적에게 편지가 저절로 날아갑니다.",
            cost = "멈추면 편지 피해가 40% 줄어듭니다.", strong = "기동 · 생존", weak = "서서 싸우는 보스 · 결계",
            cards = new[] { A, S(2), S(5) }, evo = "pe.express",
            res1 = "10칸 달릴 때마다 우표가 1 쌓입니다.",
            res2 = "가장 빠를 때 부딪힌 적을 밀칩니다.",
            mastery = "자동 편지가 두 통씩 날아갑니다.",
            color = new Color(0.5f, 0.9f, 0.6f) },
        new PathDef {
            key = "p.long", name = "장거리 배송", tagline = "사거리 20 · 보스 · 원거리",
            seed = "장거리 배송", seedDesc = "사거리가 12칸에서 20칸으로 늘고, 피해가 1.5배가 됩니다.",
            cost = "공격 속도 −25%, 5칸 안의 적에게는 피해 −40%.", strong = "보스 · 원거리", weak = "근접 포위",
            cards = new[] { S(8), S(6), K }, evo = "pe.collector",
            res1 = "20칸 넘게 날아간 편지는 치명타가 됩니다.",
            res2 = "보스에게 배달하면 우표가 2중첩 쌓입니다.",
            mastery = "사거리가 무한이 됩니다.",
            color = new Color(0.55f, 0.75f, 1f) },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Round, Express, "pe.registered"),             // 내용증명 (P · S1)
        Cross(LongHaul, Parcel, "pe.flyers"),               // 전단 폭탄 (S8 · S9)
        Cross(Bike, Parcel, "p.x.rollingparcel", "달리는 소포", "달리는 동안 발밑에 소포를 떨어뜨립니다."),
        Cross(Round, LongHaul, "p.x.nationwide", "전국 배송", "순회 배달이 먼 적일수록 덜 약해집니다."),
    };

    // ================================================================= 효과 (우체부 담당이 채움 · 비어 있어도 게임은 그대로 진행)
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
