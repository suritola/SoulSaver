using UnityEngine;

// 씨름꾼 갈래 5개 (기획서 docs/plans/2026-11-02-big-update/hero-wrestler.md 9절)
// 씨름꾼 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Wrestler.cs · 기존 파일(CharacterKit.Wrestler*.cs · SignatureSkills.Wrestler.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 효과가 사는 곳
//   던지기 · 볼 날아가기 · 착지 · 모래판 · 들기 · 막기 · 배지기  → CharacterKit.Wrestler (+ CharacterKit.Wrestler.Paths : 엎어치기 · 웅덩이 · 미끄러짐 · 따라오기)
//   주는 피해 배율(모래판 밖 대가 · 모래판 안 보스 · 들썩임) · HUD 숫자 → 이 파일
// 상태는 모두 「끝나는 시각」 값이거나 카드 단계 · Paths.* 에서 다시 계산 (켜고 끄는 깃발 없음 · 플레이어 값을 직접 바꾸지 않음 → End 에서 되돌릴 것 없음)
public sealed class WrestlerPaths : HeroPaths
{
    public const int Bowl = 0, Ring = 1, Shield = 2, Slam = 3, Giant = 4;
    // 새 교차 번호 (Crosses 배열 순서)
    public const int TampCross = 2, GiantBowlCross = 3;

    // ================================================================= 수치 (카드 · 번역 문장의 숫자와 같게)
    // 볼링
    public const float BowlPinCapMul = 2f, BowlRange = 1.25f, BowlPinPush = 3.5f, BowlSelfCost = 0.5f;
    public const int BowlStrikePins = 6;
    public const float BowlStrikeRadius = 2.5f, BowlStrikeRate = 2f, BowlBossPerPin = 0.1f, BowlBossCap = 0.8f;
    // 모래판
    public const float RingRadiusSeed = 7f, RingLifeSeed = 14f, RingHeavySeed = 0.35f, RingGaugeSeed = 0.005f, RingGaugeGap = 0.5f, RingOutsideCost = 0.7f;
    public const float RingFollowDelay = 0.6f;       // 31번 「모래판 끌기」
    public const float RingSlideGap = 0.5f, RingSlideStep = 1f, RingBossTaken = 1.15f;
    public const int RingRefillEvery = 8;
    // 인간 방패
    public const float ShieldHoldMax = 10f, ShieldTaken = 0.7f, ShieldWear = 0.4f, ShieldThrowCost = 0.75f;
    public const float ShieldReflectRate = 0.5f, ShieldBurstRadius = 2.5f, ShieldBurstRate = 1.5f, ShieldBossGauge = 0.01f, ShieldBossGaugeCap = 0.1f;
    // 엎어치기
    public const float SlamAhead = 1.5f, SlamRadius = 2f, SlamRadiusPerTier = 0.4f, SlamRate = 1.6f, SlamPush = 2f;
    public const float SlamMudLife = 2f, SlamMudSlow = 0.6f, SlamChainWindow = 2f, SlamFlipRadius = 1.6f, SlamFlipRate = 2f, SlamBossNear = 2f, SlamBossMul = 1.5f;
    // 장사
    public const float GiantWeightAdd = 0.5f, GiantBossGrapple = 1.4f, GiantLightCost = 0.6f, GiantLiftCost = 1.5f;
    public const float GiantPullRadius = 1.5f, GiantRattleSec = 3f, GiantRattleTaken = 1.15f, GiantMasteryMul = 2f;
    public const int GiantRattleEvery = 3;
    // 교차
    public const float TampEdge = 1f, TampPush = 1.5f;

    // 판 기록 dealt_by 에 남는 출처 이름 (번역 키)
    public const string BurstSource = "방패 폭발", StrikeSource = "볼 폭발", ReflectSource = "되튕김", SlamSource = "엎어치기";

    public override CharacterId Who => CharacterId.Wrestler;
    public override string IconKey => "wrestler";

    // 31번 새 카드 「모래판 끌기」: 카드 글 · 단계 · 뽑기 · HUD 는 틀이 맡음. 효과만 CharacterKit.Wrestler.Paths (단계 = Paths.Level(Paths.ExtraId), 외길이면 4)
    public override ExtraCardDef Extra => new ExtraCardDef
    {
        name = "모래판 끌기", desc = "내가 모래판 밖으로 나가면 모래판이 내 쪽으로 미끄러져 따라옵니다. (경계가 나에게 닿을 때까지 0.6초 · 남은 지속 그대로)", stat = "따라오는 횟수",
        values = new[] { "|1|회", "|2|회", "|3|회" }, fallbackIcon = 743,
    };

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "wr.bowl", name = "볼링", tagline = "던진 적이 핀을 휩씀 · 일렬 무리",
            seed = "볼링 메치기", seedDesc = "던진 적이 쓰러뜨릴 수 있는 핀이 두 배로 늘고, 던짐 거리가 25% 늘며, 핀이 옆으로 3.5칸 튕깁니다.",
            cost = "던진 적 자신의 메침 피해가 50% 줄어듭니다.", strong = "일렬 무리 · 경험치", weak = "보스 (볼이 멈춤)",
            cards = new[] { P, S(8), B, S(0) }, evo = "wre.strike",
            res1 = "볼이 벽 · 경기장 끝 · 모래판 경계에 닿으면 한 번 되튕겨 계속 굴러갑니다.",
            res2 = "핀을 6마리 이상 쓰러뜨리면 볼이 터집니다. (반경 2.5칸 · 공격력 200%) 보스를 맞힌 볼은 쓰러뜨린 핀 하나마다 보스 피해가 10% 늘어납니다. (최대 80%)",
            mastery = "볼이 끝에서 되돌아옵니다. (왕복 · 핀 상한이 새로 참)",
            power = 0.95f,          // 보스 화력 ±8% (공명 II 보스 길 +80% 상한 × 볼이 보스에 닿는 비율 약 30% 와 대가 메침 −50% 가 거의 상쇄 · 무리 몫만 남아 0.95)
            color = new Color(0.949f, 0.757f, 0.306f), form = "밧줄 샅바" },
        new PathDef {
            key = "wr.ring", name = "모래판", tagline = "넓고 오래 가는 모래판 · 지역 장악",
            seed = "도효 장인", seedDesc = "모래판이 반지름 7칸 · 지속 14초로 커지고, 무거운 적 링아웃이 최대 체력 35%로 늘며, 링아웃마다 필살기 게이지가 0.5% 찹니다. (0.5초에 한 번)",
            cost = "모래판 밖에 있는 동안 주는 피해가 30% 줄어듭니다.", strong = "지역 장악 · 생존", weak = "이동 강제 (침식 · 결계 · 돌진 보스)",
            cards = new[] { G, S(1), S(4), X }, evo = "wre.dohyo",
            res1 = "경계에 걸친 잡몹이 0.5초마다 바깥으로 1칸 미끄러집니다. (밖으로 나가면 링아웃)",
            res2 = "링아웃 8번마다 모래판 지속이 가득 차고, 모래판 안의 보스는 받는 피해가 15% 늘어납니다.",
            mastery = "모래판이 둘 깔립니다. (새 천하장사가 옛 모래판을 지우지 않음 · 가장 오래된 것부터 최대 2개)",
            power = 1.0f,           // 밖에서 −30% 대가가 보스전 이동 때 걸리고 안의 보스 +15% 와 맞섬
            color = new Color(0.91f, 0.761f, 0.478f), form = "워낭" },
        new PathDef {
            key = "wr.shield", name = "인간 방패", tagline = "든 적으로 탄을 막음 · 탄막 보스",
            seed = "인간 방패", seedDesc = "들고 있는 시간이 10초로 늘고, 들고 있는 동안 받는 피해가 30% 줄며, 막을 때 방패가 닳는 피해가 40%로 늘어납니다.",
            cost = "던짐 · 핀 피해가 25% 줄어듭니다.", strong = "탄막 보스 · 생존", weak = "무리 처리 속도",
            cards = new[] { K, S(3), S(5) }, evo = "wre.wall",
            res1 = "막은 투사체가 커서 쪽으로 되튕깁니다. (공격력 50%)",
            res2 = "방패가 닳아 쓰러지면 터지고(반경 2.5칸 · 공격력 150%) 다음 들기가 즉시 됩니다. 보스 탄을 막을 때마다 필살기 게이지가 1% 찹니다. (한 번 들 때 최대 10%)",
            mastery = "막아도 방패가 닳지 않습니다.",
            power = 1.0f,           // 던짐 −25% 를 게이지 되돌림(천하장사 800%)이 메움
            color = new Color(0.498f, 0.659f, 1f), form = "뒤집기" },
        new PathDef {
            key = "wr.slam", name = "엎어치기", tagline = "바닥에 내리꽂음 · 포위 무리",
            seed = "엎어치기", seedDesc = "던지는 대신 앞 1.5칸 바닥에 내리꽂습니다. (반경 2칸 + 무게 단계마다 0.4칸 · 공격력 160% · 바깥으로 2칸 밀침)",
            cost = "던질 수 없어 먼 적이나 일렬 무리를 맞힐 수 없고, 핀이 없습니다.", strong = "포위 무리 · 근접", weak = "원거리 적 · 탄막",
            cards = new[] { M, S(2), S(6) }, evo = "wre.slam",
            res1 = "메친 자리에 2초 동안 모래 웅덩이가 남아 안의 적이 40% 느려집니다.",
            res2 = "2초 안에 세 번 메치면 세 번째가 뒤집기가 됩니다. (반경 1.6배 · 피해 2배) 보스 2칸 안에서 메치면 보스도 충격을 1.5배로 받습니다.",
            mastery = "메친 적이 튀어 올라 한 번 더 메쳐집니다.",
            power = 1.0f,           // 보스 곁 메치기 ×1.5 가 「던질 수 없음」의 보스 손해를 메움
            color = new Color(0.961f, 0.722f, 0.353f), form = "안다리" },
        new PathDef {
            key = "wr.giant", name = "장사", tagline = "거구를 들어 던짐 · 보스 · 큰 적",
            seed = "거구 사냥", seedDesc = "중간 보스를 들 수 있고, 무거운 적과 중간 보스의 무게 배율이 0.5 늘며, 보스 배지기 피해가 40% 늘어납니다.",
            cost = "가벼운 적의 던짐 피해가 40% 줄고, 들기 시간이 1.5배가 됩니다.", strong = "보스 · 큰 적", weak = "잔챙이 무리 (15분 전 경험치)",
            cards = new[] { A, S(7), S(9) }, evo = "wre.bull",
            res1 = "무거운 적이나 중간 보스를 던지면 길 둘레 1.5칸의 적이 볼 쪽으로 끌려와 핀이 됩니다.",
            res2 = "보스 배지기를 세 번 할 때마다 보스가 들썩여 3초 동안 받는 피해가 15% 늘어납니다.",
            mastery = "중간 보스를 던져 벽꿍 · 링아웃시키면 피해가 두 배가 됩니다.",
            power = 0.96f,          // 배지기 +40% · 들썩 +15% 가 겹쳐 +8% 를 조금 넘음
            color = new Color(0.851f, 0.643f, 0.255f), form = "들돌" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Bowl, Ring, "wre.ringstrike"),                // 장외 스트라이크 (B · S4)
        Cross(Shield, Giant, "wre.giantbull"),              // 황소 장사 (S5 · S9)
        Cross(Slam, Ring, "wr.x.tamp", "도효 다지기", "모래판 안에서 메친 충격이 경계까지 번져, 경계 1칸 안의 적을 바깥으로 밀어냅니다. (잡몹이 넘으면 링아웃)"),
        Cross(Bowl, Giant, "wr.x.giantbowl", "거구 볼링", "무거운 적 · 중간 보스를 던지면 핀 상한이 없어지고, 멈출 때 반드시 스트라이크가 터집니다."),
    };

    // ================================================================= 효과
    // 입문 · 공명 · 극의 · 교차는 효과 코드가 그때그때 Paths.Has · Res · Mastered · CrossOn 으로 물음 → 판 흐름 훅은 비워 둠
    //   (이어하기도 같은 상태가 됨 · replay 때 만드는 것 없음)
    public override void OnSeed(int path, bool replay) { }
    public override void OnSingle(int path, bool replay) { }
    public override void OnResonance(int path, int tier, bool replay) { }
    public override void OnComplete(int path, bool replay) { }
    public override void OnMastery(int path, bool replay) { }
    public override void OnCross(int cross, bool replay) { }
    public override void End() { }          // 플레이어 값을 직접 바꾸지 않음

    // 이 갈래에 들어왔나 (씨름꾼 판일 때만 참 — 키트 코드가 늘 이것으로 물음)
    public static bool On(int path) => Paths.Has(CharacterId.Wrestler, path);

    static WrestlerPaths Me => Paths.Active ? Paths.Hero as WrestlerPaths : null;

    // ---------------- 보스 들썩임 (장사 공명 II) — 끝나는 시각만 기억
    GameObject rattleTarget;
    float rattleUntil;

    public static void Rattle(GameObject boss)
    {
        WrestlerPaths h = Me;
        if (h == null || boss == null) return;
        h.rattleTarget = boss;
        h.rattleUntil = Time.time + GiantRattleSec;
    }

    // 주는 피해 배율: 모래판 밖 대가(−30%) · 모래판 안 보스(+15%) · 들썩이는 보스(+15%)
    public override float Outgoing(GameObject target, float dmg)
    {
        if (target == null) return 1f;
        float m = 1f;
        CharacterKit kit = CharacterKit.Instance;
        if (kit != null && kit.WrRingOn)
        {
            if (Paths.Has(Ring))
            {
                PlayerController p = Hostile.Player;
                if (p != null && !kit.WrInRing(p.transform.position)) m *= RingOutsideCost;
            }
            if (Paths.Res(Ring) >= 2 && kit.WrInRing(target.transform.position) && Paths.IsBoss(target)) m *= RingBossTaken;
        }
        if (rattleTarget == target && Time.time < rattleUntil) m *= GiantRattleTaken;
        return m;
    }

    // HUD 입문 칸: 모래판 공명 II 이면 링아웃 8번까지 센 수 (문자열은 미리 만들어 둔 것)
    static readonly string[] RingBadge = { "0/8", "1/8", "2/8", "3/8", "4/8", "5/8", "6/8", "7/8" };
    public override string Badge(int path)
    {
        if (path == Ring && Paths.Res(Ring) >= 2 && CharacterKit.Instance != null)
            return RingBadge[CharacterKit.Instance.WrRingOutAll % RingRefillEvery];
        return null;
    }

    // 평타 모양은 CharacterKit.WrThrow 안에서 갈래를 물어 바꿈 (엎어치기는 던지기 대신 내리꽂기) — 원래 평타 흐름을 그대로 씀
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
