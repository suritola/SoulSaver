using UnityEngine;

// 유령 갈래 5개 (기획서 3-9) — 데이터는 채워 둠 · 효과 훅은 유령 담당이 구현
// 유령 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Ghost.cs · 기존 파일(CharacterKit.Ghost.cs · SignatureSkills.Ghost.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 사령술이 빙의를 늘리므로 PossessRoutine 의 이동 속도 · 반투명 되돌리기를 until 방식으로 바꿀 것 (기획서 6-3 (1) 5)
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
public sealed class GhostPaths : HeroPaths
{
    public const int Frost = 0, Shatter = 1, Legion = 2, Necro = 3, Phase = 4;

    public override CharacterId Who => CharacterId.Ghost;
    public override string IconKey => "ghost";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "h.frost", name = "혹한", tagline = "냉기 두 배 · 무리 제어",
            seed = "서리 손", seedDesc = "냉기가 두 배로 쌓이고 손길이 1칸 길어집니다.",
            cost = "직접 피해가 25% 줄어듭니다.", strong = "무리 제어", weak = "보스 (얼지 않음)",
            cards = new[] { P, S(2), S(0) }, evo = "he.blizzard",
            res1 = "얼어붙은 적 둘레에 냉기 오라가 생깁니다.",
            res2 = "얼음이 풀릴 때 냉기 파동이 퍼집니다.",
            mastery = "냉기 안개가 2배 오래 남습니다.",
            color = new Color(0.6f, 0.9f, 1f) },
        new PathDef {
            key = "h.shatter", name = "산산조각", tagline = "얼린 적 부수기 · 보스 균열",
            seed = "산산조각", seedDesc = "얼어붙은 적을 손길로 치면 부서집니다. (잡몹 즉사 · 큰 적 체력 비례) 보스는 냉기 5중첩마다 균열이 터집니다. (공격력 300%)",
            cost = "얼어붙는 시간이 50% 줄어듭니다.", strong = "보스 · 처형", weak = "냉기 준비 시간",
            cards = new[] { M, B, S(8) }, evo = "he.glacier",
            res1 = "부서질 때 얼음 파편 4개가 기본으로 튑니다.",
            res2 = "보스 균열 세 번째마다 큰 균열이 터집니다.",
            mastery = "냉기 3중첩이면 균열이 터집니다.",
            color = new Color(0.75f, 0.95f, 1f), form = "얼음 유령" },
        new PathDef {
            key = "h.legion", name = "영혼 군단", tagline = "영혼이 돌진 공격 · 자동 화력",
            seed = "영혼 거느림", seedDesc = "영혼이 1.2초마다 가까운 적에게 돌진했다 돌아오고 (공격력 40%), 영혼이 2배로 생깁니다.",
            cost = "영혼이 피해를 막을 확률이 50%가 됩니다.", strong = "자동 화력 · 무리", weak = "방패 약화",
            cards = new[] { A, S(1), S(5), S(9) }, evo = "he.banshee",
            res1 = "거느릴 수 있는 영혼이 2 늘어납니다.",
            res2 = "영혼이 5 이상이면 한꺼번에 돌진합니다.",
            mastery = "영혼 상한이 10이 됩니다.",
            color = new Color(0.7f, 0.75f, 1f), form = "사슬 망령" },
        new PathDef {
            key = "h.necro", name = "사령술", tagline = "긴 빙의 · 무적 근접 폭딜",
            seed = "사령술", seedDesc = "빙의가 3초에서 4.5초로 늘고, 빙의한 몸이 부딪친 적에게 냉기와 피해를 주며, 빙의 게이지가 25% 덜 듭니다.",
            cost = "평타 피해가 20% 줄어듭니다.", strong = "무적 시간 · 보스 근접 폭딜", weak = "빙의 밖 화력",
            cards = new[] { G, S(6), K }, evo = "he.doom",
            res1 = "빙의 폭발 범위가 1.5배가 되고, 저주가 기본으로 걸립니다.",
            res2 = "터진 몸에서 원혼 셋이 나옵니다.",
            mastery = "터진 뒤 가까운 적에게 한 번 바로 다시 빙의합니다.",
            color = new Color(0.6f, 0.45f, 0.85f) },
        new PathDef {
            key = "h.phase", name = "유체 이탈", tagline = "움직이면 접촉 무시 · 기동 생존",
            seed = "유체 이탈", seedDesc = "움직이는 동안 몸에 닿는 적의 피해를 받지 않습니다.",
            cost = "최대 체력 −25%, 투사체에 받는 피해 +20%.", strong = "기동 · 접촉 생존", weak = "탄막 보스",
            cards = new[] { S(3), S(4), S(7) }, evo = "he.specter",
            res1 = "통과한 적에게 냉기 1을 쌓습니다.",
            res2 = "폴터가이스트가 내가 통과한 적을 노립니다.",
            mastery = "멈춰도 0.5초 동안 유지됩니다.",
            color = new Color(0.85f, 0.9f, 1f), form = "그림자 유령" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Necro, Phase, "he.house"),                    // 소란한 집 (K · S7)
        Cross(Shatter, Phase, "he.vampire"),                // 흡혈귀 (M · S3)
        Cross(Frost, Legion, "h.x.frostlegion", "서리 군단", "영혼의 돌진이 냉기 2를 쌓습니다."),
        Cross(Necro, Shatter, "h.x.innerfreeze", "안에서 얼리기", "빙의 폭발이 둘레의 적을 얼립니다."),
    };

    // ================================================================= 효과 (유령 담당이 채움 · 비어 있어도 게임은 그대로 진행)
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
