using System.Collections.Generic;
using UnityEngine;

// 우체부 갈래 5개 (기획서 3-8)
// 우체부 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Postman.cs · 기존 파일(CharacterKit.Postman.cs · SignatureSkills.Postman.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 효과가 사는 곳
//   편지 한 통의 일(순회 · 소포 · 사거리 · 치명 · 멈춤 대가 · 우표)  → CharacterKit.Postman (PostAttack · LetterFlight · Deliver)
//   속달(표식 속도 · 게이지 · 공명 · 극의)                           → CharacterKit.Postman (UpdateExpress · ExpressRoutine)
//   매 프레임 일(자전거 자동 편지 · 달린 거리 · 밀치기 · 달리는 소포) · 이동 · 공격 속도 · 받는 피해 표식 → 이 파일
// 상태는 모두 「끝나는 시각」 값이거나 카드 단계 · Paths.* 에서 다시 계산 (켜고 끄는 깃발 없음 · 플레이어 값을 직접 바꾸지 않음 → End 에서 되돌릴 것 없음)
public sealed class PostmanPaths : HeroPaths
{
    public const int Round = 0, Parcel = 1, Express = 2, Bike = 3, LongHaul = 4;
    // 새 교차 번호 (Crosses 배열 순서)
    public const int RollingCross = 2, NationwideCross = 3;

    // ================================================================= 수치 (카드 · 번역 문장의 숫자와 같게)
    // 순회 배달
    public const int RoundMax = 4, RoundMasteryMax = 8;              // 받는 이 수 (처음 포함)
    public const float RoundDecay = 0.15f;                           // 다음 받는 이로 갈 때마다 피해 -15%
    public const float RoundNationwideMin = 0.2f;                    // 전국 배송: 15칸 이상 떨어진 다음 받는 이면 감쇠 15% × 0.2 = 3%
    public const float RoundSpeed = 0.85f;                           // 대가: 편지 속도 -15%
    public const float RoundMarkMul = 1.2f, RoundMarkSec = 4f;       // 공명 II 소인: 받는 피해 +20% · 4초
    // 무거운 소포
    public const float ParcelDamage = 1.8f, ParcelRadius = 2f, ParcelRate = 0.65f, ParcelSpeed = 0.5f;
    public const float ParcelCardDamage = 0.12f, ParcelCardRadius = 0.1f;    // 「소포」 카드 단계마다 소포 피해 +12% · 반경 +10% (대신 작은 폭발은 없음)
    public const float ParcelShard = 0.3f;                           // 공명 I 우표 조각 (소포 피해의 30%)
    public const float ParcelBigDamage = 1.25f, ParcelBigRadius = 1.4f;  // 공명 II 대형 소포
    public const float ParcelMasteryRadius = 1.5f, ParcelRadiusCap = 6f;
    // 순회 배달 + 무거운 소포 (교차 표에 없는 조합 · 2026-10-19 검토): 이어 간 소포(hop>0)의 폭발 · 우표 조각은 이어 갈수록 절반 (×0.5^hop)
    //   그리고 둘레 폭발 · 우표 조각이 보스 · 중간 보스를 치지 않음 (받는 이가 보스면 그 한 대는 그대로)
    //   → 예전 단일 소포의 약 3.2배(극의 4.5배) → 약 1.7배 · 보스 화력은 단일 소포와 같음 (±8% 원칙)
    public const float ParcelRouteFalloff = 0.5f;
    // 속달
    public const float ExpressCharge = 2f, ExpressRefund = 0.25f, ExpressLetterCost = 0.75f;
    public const float ExpressMarkMul = 1.15f, ExpressBlastGauge = 0.1f, ExpressEcho = 0.6f, ExpressEchoDelay = 0.5f;
    // 자전거 배달
    public const float BikeMove = 1.2f, BikeGap = 0.5f, BikeAutoRate = 0.18f, BikeAutoRange = 10f, BikeStillCost = 0.6f;
    public const float BikeStampRun = 10f;                           // 공명 I: 10칸마다 우표 1
    public const float BikeRushAfter = 1f, BikeRushGap = 0.25f, BikeRushRadius = 1.2f, BikeRushRate = 0.25f, BikeRushKnock = 5f;
    // 장거리 배송
    public const float LongRange = 20f, LongMasteryRange = 40f, LongDamage = 1.5f, LongRate = 0.75f;
    public const float LongCloseDist = 5f, LongCloseCost = 0.6f, LongCritDist = 20f, LongCrit = 1.5f;
    // 달리는 소포 (자전거 × 무거운 소포)
    public const float RollingGap = 1f, RollingFuse = 0.7f, RollingRadius = 2f, RollingRate = 0.9f;
    public const int RollingCap = 6;

    // 판 기록 dealt_by 에 남는 출처 이름 (번역 키)
    public const string ShardSource = "우표 조각", AutoSource = "자동 편지", RushSource = "자전거 충돌", RollingSource = "달리는 소포", EchoSource = "두 번째 속달";

    public override CharacterId Who => CharacterId.Postman;
    public override string IconKey => "postman";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "p.round", name = "순회 배달", tagline = "편지가 네 명을 돎 · 무리",
            seed = "순회 배달", seedDesc = "편지가 닿은 뒤 다음으로 가장 먼 적에게 계속 갑니다. (최대 4명 · 15%씩 약해짐)",
            cost = "편지 속도가 15% 느려집니다.", strong = "무리", weak = "보스 (약해짐)",
            cards = new[] { P, B, S(4) }, evo = "pe.return",
            res1 = "마지막 받는 이가 쓰러지면 처음 적에게 돌아갑니다.",
            res2 = "끝까지 다 돌면 마지막 받는 이에 소인이 찍혀 4초 동안 받는 피해가 20% 늘어납니다.",
            mastery = "최대 8명까지 돕니다.",
            color = new Color(0.95f, 0.55f, 0.45f), form = "비둘기 우편" },
        new PathDef {
            key = "p.parcel", name = "무거운 소포", tagline = "느린 폭발 소포 · 밀집 무리",
            seed = "무거운 소포", seedDesc = "편지 대신 느린 소포를 보냅니다. (피해 ×1.8 · 반경 2칸 폭발 · 「소포」 카드가 폭발을 키움)",
            cost = "공격 속도가 35% 느려집니다.", strong = "밀집 무리", weak = "빠른 적 · 근접",
            cards = new[] { M, S(3), S(9) }, evo = "pe.parcel",
            res1 = "터진 자리 둘레에서 우표 조각 두 개가 작게 터집니다. (소포 피해 30%)",
            res2 = "세 번째 소포마다 대형 소포가 됩니다. (피해 +25% · 반경 1.4배)",
            mastery = "폭발 반경이 1.5배가 됩니다.",
            power = 0.92f,          // 보스 화력 ±8% (공격 속도 0.65 × 피해 1.8 = 1.17 → 1.08 · 「소포」 3단계면 기존 소포 폭발과 비슷)
            color = new Color(0.8f, 0.6f, 0.35f), form = "소포 상자" },
        new PathDef {
            key = "p.express", name = "속달", tagline = "표식 폭발 중심 · 무리 폭딜",
            seed = "속달 전문", seedDesc = "표식이 2배 빨리 붙고, 속달 게이지가 25% 덜 듭니다.",
            cost = "평타 편지의 피해가 25% 줄어듭니다.", strong = "무리 폭딜", weak = "평타 공백",
            cards = new[] { G, S(7), S(0), S(1) }, evo = "pe.reply",
            res1 = "표식이 붙은 적은 받는 피해가 15% 늘어납니다. (터진 뒤 4초까지)",
            res2 = "속달이 터질 때마다 다음 속달 게이지가 10% 찹니다.",
            mastery = "속달이 0.5초 뒤 같은 표식에서 한 번 더 터집니다. (피해 60%)",
            color = new Color(1f, 0.85f, 0.4f), form = "우체통" },
        new PathDef {
            key = "p.bike", name = "자전거 배달", tagline = "달리면 편지가 저절로 · 기동",
            seed = "자전거 배달", seedDesc = "이동 속도 +20%, 움직이는 동안 0.5초마다 가장 가까운 적에게 편지가 저절로 날아갑니다. (편지 피해 18%)",
            cost = "멈추면 편지 피해가 40% 줄어듭니다.", strong = "기동 · 생존", weak = "서서 싸우는 보스 · 결계",
            cards = new[] { A, S(2), S(5) }, evo = "pe.express",
            res1 = "10칸 달릴 때마다 우표가 1 쌓입니다. (우표 수집이 없어도 중첩당 피해 +3%)",
            res2 = "1초 넘게 달리면 부딪힌 적을 밀쳐 냅니다. (공격력 25% · 보스 제외)",
            mastery = "자동 편지가 두 통씩 날아갑니다.",
            color = new Color(0.5f, 0.9f, 0.6f) },
        new PathDef {
            key = "p.long", name = "장거리 배송", tagline = "사거리 20 · 보스 · 원거리",
            seed = "장거리 배송", seedDesc = "사거리가 12칸에서 20칸으로 늘고, 피해가 1.5배가 됩니다.",
            cost = "공격 속도 −25%, 5칸 안의 적에게는 피해 −40%.", strong = "보스 · 원거리", weak = "근접 포위",
            cards = new[] { S(8), S(6), K }, evo = "pe.collector",
            res1 = "20칸 넘게 날아간 편지는 치명타가 됩니다. (피해 ×1.5)",
            res2 = "보스에게 배달하면 우표가 2중첩 쌓입니다.",
            mastery = "사거리가 무한이 됩니다.",
            power = 0.9f,           // 보스 화력 ±8% (공격 속도 0.75 × 피해 1.5 = 1.13 → 1.01 · 먼 편지 치명타 몫을 남김)
            color = new Color(0.55f, 0.75f, 1f) },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Round, Express, "pe.registered"),             // 내용증명 (P · S1)
        Cross(LongHaul, Parcel, "pe.flyers"),               // 전단 폭탄 (S8 · S9)
        Cross(Bike, Parcel, "p.x.rollingparcel", "달리는 소포", "달리는 동안 1초마다 발밑에 소포를 떨어뜨립니다. (0.7초 뒤 반경 2칸 폭발 · 공격력 90%)"),
        Cross(Round, LongHaul, "p.x.nationwide", "전국 배송", "순회 배달로 이어 갈 적이 멀수록 덜 약해집니다. (15칸 넘게 떨어지면 3%씩만)"),
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
    public override void End() { }          // 플레이어 값을 직접 바꾸지 않음 (이동 · 공격 속도는 매 프레임 배율로)

    // ---------------- 받는 피해 표식 (순회 공명 II 소인 · 속달 공명 I) — 끝나는 시각만 기억
    readonly Dictionary<GameObject, float> roundMarks = new Dictionary<GameObject, float>();
    readonly Dictionary<GameObject, float> expressMarks = new Dictionary<GameObject, float>();
    readonly List<GameObject> pruneList = new List<GameObject>();
    float pruneAt;

    static PostmanPaths Me => Paths.Active ? Paths.Hero as PostmanPaths : null;

    public static void MarkRound(GameObject g)
    {
        PostmanPaths h = Me;
        if (h == null || g == null) return;
        h.roundMarks[g] = Time.time + RoundMarkSec;
    }

    public static void MarkExpress(GameObject g, float seconds)
    {
        PostmanPaths h = Me;
        if (h == null || g == null) return;
        float until = Time.time + seconds;
        if (h.expressMarks.TryGetValue(g, out float u) && u > until) until = u;
        h.expressMarks[g] = until;
    }

    void Prune(Dictionary<GameObject, float> marks)
    {
        pruneList.Clear();
        foreach (KeyValuePair<GameObject, float> kv in marks) if (kv.Key == null || kv.Value < Time.time) pruneList.Add(kv.Key);
        foreach (GameObject g in pruneList) marks.Remove(g);
        pruneList.Clear();
    }

    public override float Outgoing(GameObject target, float dmg)
    {
        if (target == null) return 1f;
        float m = 1f;
        if (roundMarks.Count > 0 && Paths.Res(Round) >= 2 && roundMarks.TryGetValue(target, out float r) && r > Time.time) m *= RoundMarkMul;
        if (expressMarks.Count > 0 && Paths.Res(Express) >= 1 && expressMarks.TryGetValue(target, out float e) && e > Time.time) m *= ExpressMarkMul;
        return m;
    }

    public override void Hit(GameObject target, float dmg, bool killed, bool proc) { }
    public override float Taken(float dmg) => dmg;
    public override void Killed(Vector3 pos) { }

    // ---------------- 이동 · 공격 속도 (매 프레임 계산해 돌려줌 — 플레이어 값을 바꾸지 않음)
    public override float MoveMul() => Paths.Has(Bike) ? BikeMove : 1f;
    public override float RateMul() => (Paths.Has(Parcel) ? ParcelRate : 1f) * (Paths.Has(LongHaul) ? LongRate : 1f);

    // ---------------- 매 프레임 (멈춘 동안 · 쓰러지는 중은 안 불림)
    Vector3 lastPos;
    bool hasLast;
    float movingSince = -1f, autoAt, rushAt, dropAt, ran;

    public override void Tick()
    {
        if (Time.time >= pruneAt)
        {
            pruneAt = Time.time + 2f;
            if (roundMarks.Count > 0) Prune(roundMarks);
            if (expressMarks.Count > 0) Prune(expressMarks);
        }
        bool bike = Paths.Has(Bike), rolling = Paths.CrossOn(RollingCross);
        if (!bike && !rolling) return;
        CharacterKit kit = CharacterKit.Instance;
        PlayerController p = Hostile.Player;
        if (kit == null || p == null || p.IsDying) return;

        Vector3 pos = p.transform.position;
        float step = hasLast ? ((Vector2)(pos - lastPos)).magnitude : 0f;
        lastPos = pos;
        hasLast = true;
        bool moving = kit.Moving;
        if (!moving) movingSince = -1f;
        else if (movingSince < 0f) movingSince = Time.time;

        if (bike)
        {
            // 입문: 움직이는 동안 0.5초마다 가장 가까운 적에게 편지 (극의: 두 통)
            if (moving && Time.time >= autoAt)
            {
                autoAt = Time.time + BikeGap;
                kit.PostAutoLetter(Paths.Mastered(Bike) ? 2 : 1);
            }
            // 공명 I: 달린 거리 10칸마다 우표 1 (순간 이동 · 되감기처럼 한 프레임에 크게 옮겨진 것은 빼고)
            if (Paths.Res(Bike) >= 1 && step < 2f)
            {
                ran += step;
                if (ran >= BikeStampRun)
                {
                    ran -= BikeStampRun;
                    kit.PostAddStamp(1);
                }
            }
            // 공명 II: 1초 넘게 달리면 부딪힌 적을 밀침 (보스 · 중간 보스는 빼고)
            if (Paths.Res(Bike) >= 2 && moving && Time.time - movingSince >= BikeRushAfter && Time.time >= rushAt)
            {
                rushAt = Time.time + BikeRushGap;
                Rush(pos);
            }
        }
        // 교차 · 달리는 소포: 달리는 동안 1초마다 발밑에 소포
        if (rolling && moving && Time.time >= dropAt)
        {
            dropAt = Time.time + RollingGap;
            kit.PostDropParcel(pos, Paths.Atk * RollingRate * Paths.Power(Parcel), RollingRadius);
        }
    }

    void Rush(Vector3 pos)
    {
        float dmg = Paths.Atk * BikeRushRate * Paths.Power(Bike);
        int n = 0;
        Paths.Circle(pos, BikeRushRadius, 0f, 0f, null, c =>
        {
            if (Paths.IsBoss(c.gameObject)) return;
            Paths.Deal(c.gameObject, dmg, (c.transform.position - pos).normalized, BikeRushKnock, RushSource);
            n++;
        });
        if (n > 0)
        {
            Fx.Spawn("fx_spark", pos, 1.4f, new Color(0.6f, 1f, 0.7f), 26f);
            Hostile.Play("thump", 0.2f, 1.4f);
        }
    }

    // HUD 입문 칸: 무거운 소포 공명 II 이면 대형 소포까지 남은 수 (문자열은 미리 만들어 둔 것)
    static readonly string[] ParcelBadge = { "0/3", "1/3", "2/3" };
    public override string Badge(int path)
    {
        if (path == Parcel && Paths.Res(Parcel) >= 2 && CharacterKit.Instance != null)
            return ParcelBadge[CharacterKit.Instance.PostParcelCount % 3];
        return null;
    }

    // 평타 모양은 CharacterKit.PostAttack 안에서 갈래를 물어 바꿈 (편지 · 소포 모두 같은 배달 흐름을 써야 순회 · 등기 · 우표가 이어짐)
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
}
