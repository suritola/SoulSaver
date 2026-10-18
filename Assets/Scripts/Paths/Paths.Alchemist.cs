using UnityEngine;

// 연금술사 갈래 5개 (기획서 3-5) — 데이터는 채워 둠 · 효과 훅은 연금술사 담당이 구현
// 연금술사 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Alchemist.cs · 연금술사 기존 파일(SignatureSkills.Alchemist.cs · 새로 만들 CharacterKit.Alchemist.cs ·
//   CharacterKit.cs 안의 연금술사 전용 부분만) — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 새 카드 31번 「서리 장막」: 카드 글 · 단계 · 아이콘 · 뽑기는 틀이 맡음. 효과만 구현 (단계 = Paths.Level(Paths.ExtraId))
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
public sealed class AlchemistPaths : HeroPaths
{
    public const int Swamp = 0, Frost = 1, Chain = 2, Twins = 3, Catalyst = 4;

    public override CharacterId Who => CharacterId.Alchemist;
    public override string IconKey => "alchemist";

    public override ExtraCardDef Extra => new ExtraCardDef
    {
        name = "서리 장막", desc = "얼어붙은 적 3칸 안에 있으면 받는 피해가 줄어듭니다.", stat = "받는 피해",
        values = new[] { "|-10%|", "|-15%|", "|-20%|" }, fallbackIcon = 73,
    };

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "l.swamp", name = "늪", tagline = "모든 플라스크가 웅덩이 · 지역",
            seed = "늪지기", seedDesc = "모든 플라스크가 작은 산성 웅덩이를 남깁니다. (2초)",
            cost = "폭발 피해가 30% 줄어듭니다.", strong = "지역 · 생존", weak = "이동 보스",
            cards = new[] { B, S(0), S(6), S(8) }, evo = "le.swamp",
            res1 = "웅덩이 위의 적은 받는 피해가 15% 늘어납니다.",
            res2 = "웅덩이끼리 겹치면 합쳐져 커집니다.",
            mastery = "웅덩이가 2배 오래 남습니다.",
            color = new Color(0.6f, 0.95f, 0.3f), form = "자석 폭탄" },
        new PathDef {
            key = "l.frost", name = "빙결", tagline = "얼리고 산산조각 · 제어",
            seed = "빙결 전문", seedDesc = "빙결 시약이 두 번에 한 번 나오고, 얼어붙은 적을 맞히면 산산조각 납니다. (체력 8% · 보스 2% 추가 피해)",
            cost = "화염 피해가 40% 줄어듭니다.", strong = "제어 · 생존 · 보스 약화", weak = "직접 화력",
            cards = new[] { M, S(1), X }, evo = "le.frost",
            res1 = "얼린 적 둘레의 적이 느려집니다.",
            res2 = "얼음 파편이 10% 확률로 다시 얼립니다.",
            mastery = "산산조각 피해가 체력 12%가 됩니다.",
            color = new Color(0.55f, 0.85f, 1f) },
        new PathDef {
            key = "l.chain", name = "연쇄 폭발", tagline = "자폭까지 무기로 · 무리",
            seed = "불안정 연구", seedDesc = "불안정도가 2배로 오르고, 불안정 폭발이 1.5배 커집니다.",
            cost = "자폭 피해가 2배가 됩니다.", strong = "무리", weak = "자해",
            cards = new[] { P, S(3), S(7), G }, evo = "le.chain",
            res1 = "불안정 플라스크로 처치하면 불안정도가 10% 내려갑니다.",
            res2 = "자폭하는 순간 0.5초 무적이 되고 거대 폭발이 일어납니다.",
            mastery = "자폭 피해가 없어집니다.",
            color = new Color(1f, 0.55f, 0.25f), form = "수은 구슬" },
        new PathDef {
            key = "l.twins", name = "조수", tagline = "처음부터 호문쿨루스 · 자동 전투",
            seed = "조수 공방", seedDesc = "처음부터 호문쿨루스가 함께하고, 조수가 내 시약 순서를 따라 던집니다.",
            cost = "내 투척 속도가 30% 느려집니다.", strong = "자동 전투 · 기동", weak = "보스 · 화력 분산",
            cards = new[] { K, S(4), S(5) }, evo = "le.twins",
            res1 = "조수가 하나 더 생깁니다. (임시)",
            res2 = "조수가 내 착탄점으로 가서 한 번 더 던집니다.",
            mastery = "조수가 넷이 됩니다.",
            color = new Color(0.9f, 0.6f, 1f) },
        new PathDef {
            key = "l.catalyst", name = "원소 촉매", tagline = "순서 맞춰 붕괴 · 보스",
            seed = "정밀 조제", seedDesc = "한 적이 화염 → 빙결 → 산성 순서대로 맞으면 원소 붕괴가 일어납니다. (공격력 150%)",
            cost = "불안정한 플라스크가 나오지 않습니다.", strong = "보스", weak = "무리 · 정밀",
            cards = new[] { A, S(2), S(9) }, evo = "le.collapse",
            res1 = "붕괴한 적은 5초 동안 받는 피해가 20% 늘어납니다.",
            res2 = "붕괴가 가까운 적 둘에게 원소를 하나씩 묻힙니다.",
            mastery = "원소 둘만 맞아도 붕괴합니다.",
            color = new Color(1f, 0.85f, 0.35f), form = "폭죽" },
    };

    // 기획서 3-5 그대로: 연쇄 폭발 × 조수 짝에 기존 진화(유리 폭풍)와 새 효과(폭탄 조수)가 함께 있음
    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Chain, Twins, "le.glass"),                    // 유리 폭풍 (G · S5)
        Cross(Swamp, Catalyst, "le.march"),                 // 불꽃 행진 (S8 · S9)
        Cross(Frost, Swamp, "l.x.icebog", "얼음 늪", "웅덩이 위의 얼린 적이 받는 피해가 30% 늘어납니다."),
        Cross(Twins, Chain, "l.x.bombhelper", "폭탄 조수", "조수의 플라스크도 불안정해집니다."),
    };

    // ================================================================= 효과 (연금술사 담당이 채움 · 비어 있어도 게임은 그대로 진행)
    public override void OnSeed(int path, bool replay) { }
    public override void OnSingle(int path, bool replay) { }
    public override void OnResonance(int path, int tier, bool replay) { }
    public override void OnComplete(int path, bool replay) { }
    public override void OnMastery(int path, bool replay) { }
    public override void OnCross(int cross, bool replay) { }
    public override void OnPick(int id, bool replay) { }
    public override void End() { }

    public override float Outgoing(GameObject target, float dmg) => 1f;
    public override void Hit(GameObject target, float dmg, bool killed, bool proc) { }
    public override float Taken(float dmg) => dmg;
    public override void Killed(Vector3 pos) { }
    public override float MoveMul() => 1f;
    public override void Tick() { }
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
