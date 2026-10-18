using System.Collections.Generic;
using UnityEngine;

// 연금술사 갈래 5개 (기획서 3-5) — 데이터 + 효과
//   평타 플라스크는 CharacterKit.Alchemist.cs 의 PathThrowReagent 가 던지고(KitAttack), 터진 뒤의 갈래 효과는 이 파일 (Shatter · AfterBlast · Puddle)
//   고유 스킬 쪽 연결(원소 붕괴 순서 · 얼음 파편 · 인화성 기체 상한 · 조수 개조 시약)은 SignatureSkills.Alchemist.cs
// 새 카드 31번 「서리 장막」: 카드 글 · 단계 · 아이콘 · 뽑기는 틀이 맡음. 효과만 여기 (Taken, 단계 = Paths.Level(Paths.ExtraId))
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 안전 (안내서 6절)
//   상태는 모두 끝나는 시각(until) 값 · 무적은 Paths.Invuln(자폭 공명 II 0.5초, 4초에 한 번)만 · 보스 invulnerable 은 건드리지 않음
//   만드는 것: 조수 최대 4 (호문쿨루스 · 쌍둥이 조수 포함) · 웅덩이 최대 40 (Paths.Trim, 공명 II 는 겹친 웅덩이를 합침)
//   static 상태 없음 (판마다 새 인스턴스) · 쓰러지는 중에는 조수 · 웅덩이 · 자폭이 움직이지 않음
//
// 「연쇄 폭발 × 조수」 짝에는 교차가 둘 (기존 진화 유리 폭풍 + 새 효과 폭탄 조수) — 둘이 같이 켜져도 겹치지 않게:
//   유리 폭풍 = 대폭발 플라스크(필살기)가 터진 자리에 3초 동안 유리 비 (SignatureSkills.OnBigFlask)
//   폭탄 조수 = 조수의 평소 플라스크 세 번에 한 번이 불안정 (AideLanded) — 발동 자리가 달라 한 번의 투척이 두 효과를 함께 받지 않음
//   폭탄 조수의 불안정 폭발은 내 불안정도 · 자폭 · 연쇄 반응 · 연쇄 대폭발 · 조수 재투척을 부르지 않음 (Paths.Circle = 연쇄 방지 피해)
//   유리 비 방울은 조수 플라스크로 세지 않음 (폭탄 조수 횟수 · 시약 따라 던지기 없음)
public sealed class AlchemistPaths : HeroPaths
{
    public const int Swamp = 0, Frost = 1, Chain = 2, Twins = 3, Catalyst = 4;
    public const int XIceBog = 2, XBombHelper = 3;          // 새 교차 번호 (Crosses 배열 순서)

    // ================================================================= 수치 (카드 글 · 번역의 숫자와 같게)
    // 늪
    public const float SwampBlastCut = 0.3f;                // 대가: 폭발 피해 −30%
    const float SwampPuddleR = 0.6f;                        // 웅덩이 반지름 = 폭발 반지름 × 0.6
    const float SwampPuddleLife = 2f;                       // 2초 (극의 4초)
    const float SwampPuddleTick = 0.07f;                    // 0.5초마다 그 플라스크 폭발 피해의 7%
    const float SwampRes1 = 1.15f;                          // 공명 I: 웅덩이 위 받는 피해 +15%
    const float IceBogMul = 1.3f;                           // 얼음 늪(교차): 웅덩이 위의 얼린 적 +30%
    const float PuddleMaxR = 4.5f;                          // 공명 II: 합쳐진 웅덩이 최대 반지름
    const int MaxPuddles = 40;
    static readonly Color PuddleTint = new Color(0.55f, 0.95f, 0.3f, 0.55f);
    // 빙결
    public const float FrostFireCut = 0.4f;                 // 대가: 화염 피해 −40%
    public const float FrostFreeze = 0.6f;                  // 급속 냉동이 없을 때 빙결 시약이 얼리는 시간
    public const float BossChill = 1.5f;                    // 보스는 얼지 않음 — 빙결 시약에 맞은 뒤 이 시간 동안 「얼어붙은 것으로」 셈
    const float ShatterPct = 0.08f, ShatterPctM = 0.12f;    // 산산조각: 최대 체력 8% (극의 12%)
    const float ShatterBossPct = 0.02f, ShatterBossPctM = 0.03f, ShatterBossCap = 0.35f;     // 보스 · 중간 보스: 최대 체력 2% (극의 3%) · 그 플라스크 폭발 피해의 35%까지
    const float ShatterFloor = 0.25f;                       // 일반 적: 적어도 폭발 피해의 25%
    const float CurtainRange = 3f;                          // 서리 장막: 얼어붙은 적 3칸 안
    static readonly Color Ice = new Color(0.6f, 0.9f, 1f);
    // 연쇄 폭발
    public const float ChainBlastRadius = 1.5f;             // 불안정 폭발 범위 ×1.5
    public const float SelfBlastMul = 4f;                   // 갈래 자폭 주변 피해 공격력 400% (원래 500% · 두 배 잦아서)
    public const float BigBlastBonus = 3f;                  // 공명 II 거대 폭발 +300% (반지름 6 → 9)
    public const float SelfBlastHurt = 0.16f;               // 대가: 자폭 내 피해 8% → 16% (극의 0)
    public const float BlastInvuln = 0.5f;
    // 조수
    const float TwinsRate = 0.7f;                           // 대가: 내 투척 속도 −30%
    const float AideMul = 0.8f;                             // 갈래 조수 한 마리의 플라스크 = 호문쿨루스의 80%
    const float RethrowMul = 0.5f, RethrowCd = 1.6f;        // 공명 II 재투척: 호문쿨루스 플라스크의 50% · 1.6초에 한 번
    const int MaxAides = 4;                                 // 호문쿨루스 · 쌍둥이 조수 포함
    public static readonly Color AideTint = new Color(0.75f, 0.85f, 1f);
    static readonly float[] AideOffset = { Mathf.PI * 0.5f, Mathf.PI * 1.5f, Mathf.PI };     // 호문쿨루스(0) · 쌍둥이 조수(π)와 겹치지 않게 맴도는 자리
    // 원소 촉매
    public const float CatalystSeedMul = 1.5f;              // 입문 붕괴: 공격력 150% (촉매 반응을 배우면 그 스킬 피해로)
    public const float CatalystRes1 = 1.15f, CatalystRes1Time = 4f;      // 공명 I: 붕괴한 적 4초 동안 받는 피해 +15%

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
            seed = "빙결 전문", seedDesc = "빙결 시약이 두 번에 한 번 나와 적을 얼리고, 얼어붙은 적을 플라스크로 맞히면 산산조각 납니다. (체력 8% · 보스는 체력 2%까지)",
            cost = "화염 피해가 40% 줄어듭니다.", strong = "제어 · 생존 · 보스 약화", weak = "직접 화력",
            cards = new[] { M, S(1), X }, evo = "le.frost",
            res1 = "얼린 적 둘레의 적이 느려집니다.",
            res2 = "산산조각이 얼음 파편 3개를 흩뿌리고, 얼음 파편이 10% 확률로 다시 얼립니다.",
            mastery = "산산조각 피해가 체력 12%가 됩니다. (보스 3%)",
            color = new Color(0.55f, 0.85f, 1f) },
        new PathDef {
            key = "l.chain", name = "연쇄 폭발", tagline = "자폭까지 무기로 · 무리",
            seed = "불안정 연구", seedDesc = "불안정도가 2배로 오르고, 불안정 폭발이 1.5배 넓어집니다.",
            cost = "자폭 피해가 2배가 됩니다.", strong = "무리", weak = "자해",
            cards = new[] { P, S(3), S(7), G }, evo = "le.chain",
            res1 = "불안정 플라스크로 처치할 때마다 불안정도가 10% 내려갑니다. (한 번에 최대 30%)",
            res2 = "자폭하는 순간 0.5초 무적이 되고 거대 폭발이 일어납니다.",
            mastery = "자폭 피해가 없어집니다.",
            color = new Color(1f, 0.55f, 0.25f), form = "수은 구슬" },
        new PathDef {
            key = "l.twins", name = "조수", tagline = "처음부터 호문쿨루스 · 자동 전투",
            seed = "조수 공방", seedDesc = "처음부터 호문쿨루스가 함께하고, 조수가 내 시약 순서를 따라 던집니다.",
            cost = "내 투척 속도가 30% 느려집니다.", strong = "자동 전투 · 기동", weak = "보스 · 화력 분산",
            cards = new[] { K, S(4), S(5) }, evo = "le.twins",
            res1 = "조수가 하나 더 생깁니다. (완성하면 쌍둥이 조수로 바뀜)",
            res2 = "조수가 내 착탄점으로 날아가 한 번 더 던집니다.",
            mastery = "조수가 넷이 됩니다.",
            power = 0.85f,          // 조수가 늘어나는 갈래라 조수 플라스크 피해를 조금 낮춤 (보스 화력 ±8%)
            color = new Color(0.9f, 0.6f, 1f) },
        new PathDef {
            key = "l.catalyst", name = "원소 촉매", tagline = "순서 맞춰 붕괴 · 보스",
            seed = "정밀 조제", seedDesc = "한 적이 화염 → 빙결 → 산성 순서대로 맞으면 원소 붕괴가 일어납니다. (공격력 150%)",
            cost = "불안정한 플라스크가 나오지 않습니다.", strong = "보스", weak = "무리 · 정밀",
            cards = new[] { A, S(2), S(9) }, evo = "le.collapse",
            res1 = "붕괴한 적은 4초 동안 받는 피해가 15% 늘어납니다.",
            res2 = "붕괴가 가까운 적 둘에게 원소를 하나씩 묻힙니다.",
            mastery = "원소 둘만 맞아도 붕괴합니다.",
            color = new Color(1f, 0.85f, 0.35f), form = "폭죽" },
    };

    // 기획서 3-5 그대로: 연쇄 폭발 × 조수 짝에 기존 진화(유리 폭풍)와 새 효과(폭탄 조수)가 함께 있음 (겹침 처리는 위 머리말)
    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Chain, Twins, "le.glass"),                    // 유리 폭풍 (G · S5)
        Cross(Swamp, Catalyst, "le.march"),                 // 불꽃 행진 (S8 · S9)
        Cross(Frost, Swamp, "l.x.icebog", "얼음 늪", "웅덩이 위의 얼린 적이 받는 피해가 30% 늘어납니다."),
        Cross(Twins, Chain, "l.x.bombhelper", "폭탄 조수", "조수의 플라스크가 세 번에 한 번 불안정해져 크게 터집니다."),
    };

    // 이번 판 연금술사 갈래 효과 (갈래 규칙 판에서만 · 다른 영웅 판이면 null)
    public static AlchemistPaths Live => Paths.Active ? Paths.Hero as AlchemistPaths : null;

    // ================================================================= 상태 (판마다 새로)
    readonly List<GameObject> puddles = new List<GameObject>();                                        // 늪 웅덩이 (DamageZone)
    readonly Dictionary<GameObject, (DamageZone z, float born)> pud = new Dictionary<GameObject, (DamageZone, float)>();
    readonly Dictionary<GameObject, float> frozen = new Dictionary<GameObject, float>();               // 얼어붙은 적 → 풀리는 시각
    readonly List<GameObject> prune = new List<GameObject>(), shatterBuf = new List<GameObject>();

    class Aide { public GameObject go; public float next, visitUntil, throwAt, throwDmg; public Vector3 visit; }
    readonly List<Aide> aides = new List<Aide>();                                                      // 갈래 조수 (호문쿨루스 · 쌍둥이 조수 말고)
    float aideSpawnAt, rethrowAt;
    int rethrowTurn, aideFlasks;

    // ================================================================= 판 흐름
    // 입문 · 공명 · 극의 · 교차는 모두 Paths.Has · Res · Mastered · CrossOn 을 그때그때 물음 (이어하기도 같은 상태 · 만들 것은 Tick 이 늦게 만듦)
    public override void End()
    {
        ClearAides();
        puddles.Clear();
        pud.Clear();
        frozen.Clear();
    }

    // ================================================================= 전투 훅
    // 평타: 갈래 규칙 판의 연금술사 평타는 모두 PathThrowReagent (원래 평타와 같고 갈래 규칙만 끼움)
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots)
    {
        if (kit == null || kit.Id != CharacterId.Alchemist) return false;
        kit.AlchPathAttack(start, dmg, shots);
        return true;
    }

    // 조수 공방의 대가: 내 투척 속도 −30%
    public override float RateMul() => Paths.Has(Twins) ? TwinsRate : 1f;

    // 늪 공명 I: 웅덩이 위 +15% · 얼음 늪(교차): 웅덩이 위의 얼린 적 +30%
    public override float Outgoing(GameObject target, float dmg)
    {
        if (target == null || puddles.Count == 0) return 1f;
        bool res1 = Paths.Has(Swamp) && Paths.Res(Swamp) >= 1;
        bool bog = Paths.CrossOn(XIceBog);
        if (!res1 && !bog) return 1f;
        if (!InPuddle(target.transform.position)) return 1f;
        float m = res1 ? SwampRes1 : 1f;
        if (bog && IsFrozen(target)) m *= IceBogMul;
        return m;
    }

    // 31번 서리 장막: 얼어붙은 적 3칸 안이면 받는 피해 −10 · −15 · −20% (외길 4단계 −25%)
    public override float Taken(float dmg)
    {
        int lv = Paths.Level(Paths.ExtraId);
        if (lv <= 0 || frozen.Count == 0) return dmg;
        PlayerController pl = Hostile.Player;
        if (pl == null) return dmg;
        Vector3 me = pl.transform.position;
        float now = Time.time;
        foreach (KeyValuePair<GameObject, float> kv in frozen)
        {
            if (kv.Key == null || kv.Value <= now) continue;
            if ((kv.Key.transform.position - me).sqrMagnitude > CurtainRange * CurtainRange) continue;
            if (kv.Key.TryGetComponent(out EnermyController e) && e.IsDead) continue;
            return dmg * (1f - Mathf.Min(0.4f, 0.05f + 0.05f * lv));
        }
        return dmg;
    }

    // 조수: 갈래 조수를 띄우고 맴돌며 던짐 (만들기는 여기서만 — 입문 · 이어하기 때 바로 만들지 않음)
    public override void Tick()
    {
        if (!Paths.Has(Twins)) { if (aides.Count > 0) ClearAides(); return; }
        CharacterKit kit = CharacterKit.Instance;
        if (kit == null || kit.Id != CharacterId.Alchemist || !kit.AlchAlive) return;
        kit.AlchEnsureHomunculus();
        int have = (kit.AlchHomunculus != null ? 1 : 0) + (SignatureSkills.AlchTwin != null ? 1 : 0);
        int want = Paths.Mastered(Twins) ? 4 : Paths.Res(Twins) >= 1 ? 2 : 1;
        int mine = Mathf.Clamp(want - have, 0, MaxAides - have);
        aides.RemoveAll(a => a.go == null);
        while (aides.Count > mine)
        {
            Aide last = aides[aides.Count - 1];
            if (last.go != null) Object.Destroy(last.go);
            aides.RemoveAt(aides.Count - 1);
        }
        if (aides.Count < mine && Time.time >= aideSpawnAt) SpawnAide(kit);

        Vector3 me = kit.transform.position;
        float t = Time.time;
        for (int i = 0; i < aides.Count; i++)
        {
            Aide a = aides[i];
            if (a.go == null) continue;
            Transform tr = a.go.transform;
            float off = AideOffset[i % AideOffset.Length];
            bool visiting = t < a.visitUntil;
            Vector3 home = visiting ? a.visit + new Vector3(0f, 1.2f, 0f)
                                    : me + new Vector3(Mathf.Cos(t * 2f + off) * 1.6f, 1.3f + Mathf.Sin(t * 4f + off) * 0.2f, 0f);
            tr.position = Vector3.Lerp(tr.position, home, (visiting ? 12f : 8f) * Time.deltaTime);
            // 공명 II: 착탄점으로 날아간 뒤 던짐
            if (a.throwAt > 0f && t >= a.throwAt)
            {
                a.throwAt = 0f;
                AideThrow(kit, tr.position, a.visit, a.throwDmg);
            }
            if (t < a.next) continue;
            Transform e = Specials.NearestEnemy(tr.position, 12f);
            if (e == null) { a.next = t + 0.3f; continue; }
            a.next = t + Mathf.Max(1f, 3.4f - 0.7f * kit.card[2]);      // 호문쿨루스 카드 단계를 따름 (외길 4단계에서도 1초 아래로는 안 내려감)
            AideThrow(kit, tr.position, e.position, kit.AlchDamage * 1.4f * SignatureSkills.HomunculusMul * AideMul * Paths.Power(Twins));
        }
    }

    // ================================================================= 평타 플라스크가 터진 뒤 (CharacterKit.PathThrowReagent 가 부름)
    // 빙결: 터지기 전에 이미 얼어 있던 적은 산산조각 (이 플라스크가 얼린 적은 다음 플라스크에서)
    public void Shatter(Vector3 p, float r, float hit)
    {
        if (!Paths.Has(Frost) || frozen.Count == 0) return;
        shatterBuf.Clear();
        Paths.Circle(p, r, 0f, 0f, null, c => { if (IsFrozen(c.gameObject)) shatterBuf.Add(c.gameObject); });
        if (shatterBuf.Count == 0) return;
        bool m = Paths.Mastered(Frost);
        float pw = Paths.Power(Frost);
        int shards = 0;
        foreach (GameObject g in shatterBuf)
        {
            if (g == null) continue;
            frozen.Remove(g);
            bool boss = Paths.IsBoss(g);
            float max = MaxHp(g);
            float d = boss ? Mathf.Min(max * (m ? ShatterBossPctM : ShatterBossPct), hit * ShatterBossCap)
                           : Mathf.Max(max * (m ? ShatterPctM : ShatterPct), hit * ShatterFloor);
            Vector3 at = g.transform.position;
            Paths.Deal(g, d * pw, Vector3.zero, 0f, "산산조각");
            Fx.Spawn("fx_shock", at, boss ? 3.4f : 2.4f, Ice, 22f);
            Fx.Spawn("fx_spark", at, 1.8f, Color.white, 24f);
            // 공명 II: 얼음 파편 3개 (한 플라스크에 두 곳까지 — 탄이 너무 많아지지 않게)
            if (Paths.Res(Frost) >= 2 && shards < 2) { shards++; ShatterShards(at); }
        }
        Hostile.Play("shatter", 0.5f, 1.75f);
        shatterBuf.Clear();
    }

    public void AfterBlast(Vector3 p, float r, float hit, int kind, bool unstable)
    {
        CharacterKit kit = CharacterKit.Instance;
        if (kit == null || !kit.AlchAlive) return;
        // 늪지기: 모든 플라스크가 작은 산성 웅덩이 (산성 시약은 원래 웅덩이를 갈래 웅덩이로 바꿔 둠 — PathThrowReagent)
        if (Paths.Has(Swamp) && (unstable || kind != 2)) Puddle(p, r * SwampPuddleR, SwampPuddleLife, hit * SwampPuddleTick, true);
        // 빙결 공명 I: 얼린 자리 둘레의 적이 느려짐
        if (Paths.Has(Frost) && Paths.Res(Frost) >= 1 && kind == 1 && !unstable)
            Paths.Circle(p, r + 2.5f, 0f, 0f, null, c => { if (!IsFrozen(c.gameObject) && c.TryGetComponent(out EnermyController e) && !e.IsDead) e.Slow(0.6f, 1.5f); });
        // 조수 공명 II: 조수가 착탄점으로 가서 한 번 더
        if (Paths.Has(Twins) && Paths.Res(Twins) >= 2 && Time.time >= rethrowAt) Rethrow(kit, p);
    }

    // ================================================================= 늪 웅덩이 (돌려주는 값 = 실제로 남는 시간)
    // 산성 장판(DamageZone)과 같은 그림 · 0.5초마다 tickDmg · 공명 II 면 겹친 웅덩이에 합쳐짐 · 최대 40 (가장 오래된 것부터 지움)
    public float Puddle(Vector3 p, float r, float life, float tickDmg, bool addAcid)
    {
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        if (sp == null || r <= 0f) return life;
        if (Paths.Mastered(Swamp)) life *= 2f;
        tickDmg *= Paths.Power(Swamp);
        puddles.RemoveAll(g => g == null);
        if (Paths.Res(Swamp) >= 2)
        {
            foreach (GameObject g in puddles)
            {
                if (!pud.TryGetValue(g, out var e) || e.z == null) continue;
                DamageZone z = e.z;
                Vector3 c = g.transform.position;
                if ((c - p).sqrMagnitude > (z.radius + r) * (z.radius + r) * 0.5625f) continue;      // 반지름 합의 75% 안
                float nr = Mathf.Min(PuddleMaxR, Mathf.Sqrt(z.radius * z.radius + r * r));        // 넓이를 더함
                float k = r * r / (z.radius * z.radius + r * r);
                g.transform.position = Vector3.Lerp(c, p, k);
                g.transform.localScale *= nr / Mathf.Max(0.01f, z.radius);
                z.radius = nr;
                float age = Time.time - e.born;
                z.duration = Mathf.Max(z.duration, age + life);
                z.tickDamage = Mathf.Max(z.tickDamage, tickDmg);
                if (addAcid) SignatureSkills.AddAcid(g.transform.position, nr, Time.time + (z.duration - age));
                return z.duration - age;
            }
        }
        Paths.Trim(puddles, MaxPuddles);
        if (pud.Count > puddles.Count + 16)
        {
            prune.Clear();
            foreach (GameObject g in pud.Keys) if (g == null) prune.Add(g);
            foreach (GameObject g in prune) pud.Remove(g);
            prune.Clear();
        }
        DamageZone nz;
        using (DamageSource.As("늪 웅덩이")) nz = sp.SpawnZone(p, r, life, tickDmg, PuddleTint);
        if (nz == null) return life;
        puddles.Add(nz.gameObject);
        pud[nz.gameObject] = (nz, Time.time);
        if (addAcid) SignatureSkills.AddAcid(p, r, Time.time + life);
        return life;
    }

    bool InPuddle(Vector3 pos)
    {
        foreach (GameObject g in puddles)
        {
            if (g == null || !pud.TryGetValue(g, out var e) || e.z == null) continue;
            if ((pos - g.transform.position).sqrMagnitude <= e.z.radius * e.z.radius) return true;
        }
        return false;
    }

    // ================================================================= 빙결
    public void Freeze(GameObject g, float seconds)
    {
        if (g == null || seconds <= 0f) return;
        if (frozen.Count > 200) PruneFrozen();
        float until = Time.time + seconds;
        if (!frozen.TryGetValue(g, out float u) || u < until) frozen[g] = until;
    }

    public bool IsFrozen(GameObject g) => g != null && frozen.TryGetValue(g, out float u) && u > Time.time;

    void PruneFrozen()
    {
        prune.Clear();
        foreach (KeyValuePair<GameObject, float> kv in frozen) if (kv.Key == null || kv.Value <= Time.time) prune.Add(kv.Key);
        foreach (GameObject g in prune) frozen.Remove(g);
        prune.Clear();
        if (frozen.Count > 200) frozen.Clear();
    }

    static float MaxHp(GameObject g)
    {
        if (g.TryGetComponent(out bosss b)) return b.setEnemyHP;
        if (g.TryGetComponent(out EnermyController e)) return e.setEnemyHP;
        return 0f;
    }

    void ShatterShards(Vector3 at)
    {
        SignatureSkills s = SignatureSkills.Instance;
        if (s == null) return;
        float dmg = Paths.Atk * 0.35f * Paths.Power(Frost);
        float a0 = Random.Range(0f, 120f);
        for (int i = 0; i < 3; i++)
        {
            Vector2 d = Quaternion.Euler(0f, 0f, a0 + i * 120f) * Vector2.right;
            Bullet b = s.Shot(at, d, dmg, 2, 22f, 6f, "fx_spark", 0.6f, Ice);
            if (b != null) b.onHitEnemy += (bullet, col) => ShardFreeze(col);
        }
    }

    // 얼음 파편(서리 결정 · 산산조각)이 맞힌 적: 공명 II 면 10% 로 다시 얼림
    public static void ShardHit(Collider2D col) => Live?.ShardFreeze(col);

    void ShardFreeze(Collider2D col)
    {
        if (col == null || !Paths.Has(Frost) || Paths.Res(Frost) < 2 || Random.value >= 0.1f) return;
        if (col.TryGetComponent(out EnermyController e))
        {
            if (e.IsDead) return;
            e.Slow(0f, 0.8f);
            Freeze(col.gameObject, 0.8f);
        }
        else if (Paths.IsBoss(col.gameObject)) Freeze(col.gameObject, BossChill);
        else return;
        Fx.Spawn("fx_sparkle", col.transform.position, 1.2f, Ice, 24f);
    }

    // ================================================================= 조수
    void SpawnAide(CharacterKit kit)
    {
        aideSpawnAt = Time.time + 0.5f;
        Sprite[] f = Fx.Frames("fx_flask");
        if (f == null || f.Length == 0) return;
        // 개수 상한 (Paths.Trim 과 같은 규칙: 사라진 것을 치우고 넘치면 가장 오래된 것부터)
        aides.RemoveAll(a => a.go == null);
        while (aides.Count >= MaxAides)
        {
            if (aides[0].go != null) Object.Destroy(aides[0].go);
            aides.RemoveAt(0);
        }
        Vector3 at = kit.transform.position + Vector3.up;
        GameObject go = SpecialAbilities.MakeSprite("AlchAide", f[0], at, 1.25f / f[0].bounds.size.y, AideTint, "Character", 2);
        if (go == null) return;
        aides.Add(new Aide { go = go, next = Time.time + 1f + 0.5f * aides.Count });
        Fx.Spawn("fx_sparkle", at, 1.4f, AideTint, 22f);
    }

    void ClearAides()
    {
        foreach (Aide a in aides) if (a.go != null) Object.Destroy(a.go);
        aides.Clear();
    }

    void AideThrow(CharacterKit kit, Vector3 from, Vector3 to, float dmg)
    {
        using (DamageSource.As("조수"))
            FlaskLob.Throw(from, to, 0.45f, 0.55f, AideTint, q => { if (kit != null) kit.AlchAideLand(q, dmg, true); });
    }

    void Rethrow(CharacterKit kit, Vector3 p)
    {
        rethrowAt = Time.time + RethrowCd;
        float dmg = kit.AlchDamage * 1.4f * SignatureSkills.HomunculusMul * RethrowMul * Paths.Power(Twins);
        aides.RemoveAll(a => a.go == null);
        if (aides.Count > 0)
        {
            // 갈래 조수가 착탄점으로 날아가서 던짐 (끝나는 시각으로만 · Tick 이 움직임)
            rethrowTurn = (rethrowTurn + 1) % 1000;
            Aide a = aides[rethrowTurn % aides.Count];
            a.visit = p;
            a.visitUntil = Time.time + 0.8f;
            a.throwAt = Time.time + 0.3f;
            a.throwDmg = dmg;
            return;
        }
        // 갈래 조수가 없으면 (완성 뒤 호문쿨루스 + 쌍둥이 조수) 그 자리에서 던짐
        GameObject from = kit.AlchHomunculus != null ? kit.AlchHomunculus : SignatureSkills.AlchTwin;
        if (from != null) AideThrow(kit, from.transform.position, p, dmg);
    }

    // 조수 플라스크가 떨어진 뒤 (호문쿨루스 · 갈래 조수 · 재투척 모두, 유리 비 · 쌍둥이 조수는 빼고)
    public void AideLanded(Vector3 q, float dmg)
    {
        if (!Paths.Has(Twins)) return;
        float pw = Paths.Power(Twins);
        CharacterKit kit = CharacterKit.Instance;
        int k = kit != null ? kit.AlchLastKind : -1;
        // 조수 공방: 내가 마지막에 던진 시약을 따라 (조수 개조를 배웠으면 SignatureSkills.TinkerLand 가 같은 순서로 맡음 — 두 번 붙지 않게)
        if (k >= 0 && !SignatureSkills.AlchTinker)
        {
            if (k == 0) Paths.Circle(q, 1.4f, 0f, 0f, null, c => Burn.Apply(c.gameObject, dmg * 0.3f * pw, 2f));
            else if (k == 1) Paths.Circle(q, 1.4f, 0f, 0f, null, c => { if (c.TryGetComponent(out EnermyController e) && !e.IsDead) e.Slow(0.5f, 1.2f); });
            else if (Paths.Has(Swamp)) Puddle(q, 1.1f, 2f, dmg * 0.15f, true);
            else if (SpecialAbilities.SharedInstance != null) SpecialAbilities.SharedInstance.SpawnZone(q, 1.2f, 2.5f, dmg * 0.2f * pw, new Color(0.45f, 1f, 0.3f, 0.6f));
        }
        // 폭탄 조수 (교차): 세 번에 한 번 불안정 — 연쇄 방지 피해라 다른 폭발을 부르지 않음
        if (Paths.CrossOn(XBombHelper))
        {
            aideFlasks = (aideFlasks + 1) % 3;
            if (aideFlasks == 0)
            {
                Paths.Circle(q, 2.4f, dmg * 0.6f * pw, 2f, "폭탄 조수");
                Fx.Spawn("fx_alchemyblast", q, 5.6f, SkinFx.Tint(new Color(0.9f, 0.55f, 1f)), 16f);
                Fx.Spawn("fx_shock", q, 5f, new Color(0.85f, 0.5f, 1f, 0.8f), 20f);
                Hostile.Play("boom", 0.45f, 1.25f);
            }
        }
    }
}
