using System.Collections.Generic;
using UnityEngine;

// 검사 갈래 5개 (기획서 3-2) — 데이터 · 효과
// 검사 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Swordsman.cs · 검사 기존 파일(SignatureSkills.Swordsman.cs · CharacterKit.Swordsman.cs ·
//   CharacterKit.cs 안의 검사 전용 부분만) — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 효과가 붙는 자리
//   평타 앞뒤: CharacterKit.Swing → CharacterKit.Swordsman(SwordPathPre · SwordPathPost) → 아래 SwingPre · SwingPost
//   받는 피해 · 매 프레임 · 필살기 · 명중: HeroPaths 훅 (Taken · Tick · Ult · Hit)
// 안전 (안내서 6절)
//   상태는 모두 「끝나는 시각」(until) 값 · 코루틴 없음 · static 상태 없음 (Cur 는 Paths.Hero 를 꺼낼 뿐)
//   무적은 Paths.Invuln 만 (막기 0.6초 · 검무 극의 0.1초) · 보스 invulnerable 은 건드리지 않음
//   만드는 것: 장판(칼날 잔상 12 · 소용돌이 4, Paths.Trim) · 미끄러질 때 잔상(FadeOut 으로 스스로 사라짐) · 나중에 터질 것 24
//   갈래 효과 피해에는 모두 Paths.Power(갈래) 를 곱함
// 밸런스 (1.3.7 기준 · 보스 화력 지수 ±8% 목표, 1차안): 기획서 수치에서 바꾼 것
//   검기 방출 70% → 60%(+「날아가는 검기」 단계마다 5% · 카드 검기를 대신함) · 피의 맹세 피해 +45% → +35%
//   받아치기: 막기는 1.2초에 한 번 · 성공하면 2초에 한 번 (마구 눌러 늘 막히지 않게 — 긴장감)
//   회오리: 「모으며 60% 속도로 걷기」 → 지금도 모으며 걸을 수 있어 뜻이 없음 → 「모으는 동안 0.5초마다 둘레 베기(50%)」
//   회오리 극의 「계속 돌기」 → 회전 베기 뒤 3초 동안 0.3초마다 둘레 베기(25%)
public sealed class SwordsmanPaths : HeroPaths
{
    public const int Wave = 0, Parry = 1, Blood = 2, Dance = 3, Whirl = 4;
    // 새 교차 번호 (Crosses 배열 순서)
    public const int CrossBloodWave = 2, CrossBladeStorm = 3;

    public override CharacterId Who => CharacterId.Swordsman;
    public override string IconKey => "swordsman";

    // 이번 판 검사 갈래 효과 (갈래 규칙 판이 아니거나 다른 영웅이면 null) — 상태를 static 에 두지 않고 판마다 새로 만든 Paths.Hero 를 꺼냄
    public static SwordsmanPaths Cur => Paths.Active ? Paths.Hero as SwordsmanPaths : null;

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "s.wave", name = "검기", tagline = "휘두를 때마다 검기 · 안전 거리",
            seed = "검기 방출", seedDesc = "휘두를 때마다 검기가 반드시 날아갑니다. (9칸 · 베기 피해 60% · 관통 · 「날아가는 검기」 단계마다 +5%) 휘두르기 범위는 20% 줄어듭니다.",
            cost = "근접 특성(휘두르는 동안 받는 피해 절반)이 사라집니다.", strong = "안전 거리 · 일렬 · 보스", weak = "근접 생존력",
            cards = new[] { P, S(1), S(6) }, evo = "se.trail",
            res1 = "검기로 같은 적을 세 번 맞히면 균열이 바로 터집니다.",
            res2 = "검기가 두 줄기 X자로 날아가고(한 줄기 70% 세기), 교차점에서 잔상이 터집니다. (베기 피해 30%)",
            mastery = "검기가 끝에서 되돌아옵니다. (80% 세기)",
            color = new Color(0.6f, 0.8f, 1f), form = "채찍검" },
        new PathDef {
            key = "s.parry", name = "받아치기", tagline = "막기 0.25초 · 반격",
            seed = "받아치기", seedDesc = "우클릭을 누른 첫 0.25초가 막기가 됩니다. 그사이 맞으면 피해 0 · 0.6초 무적 · 다음 베기 ×3. 실패해도 손해는 없지만, 막기는 1.2초에 한 번(성공하면 2초에 한 번)입니다.",
            cost = "회전 베기를 끝까지 모으는 시간이 30% 늘어납니다.", strong = "보스 탄막 · 생존", weak = "무리 · 손 기술",
            cards = new[] { K, S(0), A, S(7) }, evo = "se.counter",
            res1 = "막기에 성공하면 둘레의 적 투사체를 모두 되돌려 보냅니다.",
            res2 = "3초 안에 두 번 막으면 4초 동안 역습 자세가 됩니다. (사거리 ×1.4 · 피해 ×1.5)",
            mastery = "막기가 0.4초로 늘고, 성공할 때마다 필살기 게이지가 20% 찹니다.",
            color = new Color(1f, 0.9f, 0.5f) },
        new PathDef {
            key = "s.blood", name = "피의 맹세", tagline = "체력을 바쳐 흡혈 · 근접 지속",
            seed = "피의 맹세", seedDesc = "휘두를 때마다 지금 체력의 2%를 바치고, 베기 피해가 35% 늘며 흡혈이 2배가 됩니다.",
            cost = "물약 · 재생 회복이 50% 줄어듭니다.", strong = "근접 무리 지속", weak = "원거리 탄막",
            cards = new[] { M, S(2), S(9) }, evo = "se.blood",
            res1 = "체력이 50% 아래면 피해 +25%, 25% 아래면 +50%입니다.",
            res2 = "혈갑 보호막이 가득 차면 베기가 피의 파동이 되어 길게 뻗습니다. (9칸 · 베기 피해 50%)",
            mastery = "장마다 한 번 체력 1로 버티고, 3초 동안 흡혈이 3배가 됩니다.",
            color = new Color(0.95f, 0.25f, 0.3f) },
        new PathDef {
            key = "s.dance", name = "검무", tagline = "미끄러지며 베기 · 기동",
            seed = "검무 보법", seedDesc = "움직이며 휘두르면 진행 방향으로 1.5칸 미끄러지며 벱니다. 움직이는 동안 공격 속도 +15%.",
            cost = "멈춰서 휘두르면 공격 속도가 20% 느려집니다.", strong = "기동 · 무리", weak = "서서 싸우는 보스 · 결계",
            cards = new[] { B, S(5), S(3) }, evo = "se.dance",
            res1 = "미끄러진 길에 칼날 잔상이 1초 동안 남습니다. (0.25초마다 베기 피해 20%)",
            res2 = "검무가 최대 중첩이면 다섯 번째 휘두르기마다 회전 베기가 저절로 나갑니다. (30% 세기)",
            mastery = "미끄러질 때 0.1초 무적이 되고, 2.5칸을 미끄러집니다.",
            color = new Color(0.55f, 1f, 0.75f), form = "기창" },
        new PathDef {
            key = "s.whirl", name = "회오리", tagline = "원형 베기 · 모으며 베기",
            seed = "회전 검사", seedDesc = "평타가 몸 둘레 원형 베기가 됩니다. (반경 85% · 피해 80%) 회전 베기를 모으는 동안 0.5초마다 둘레를 벱니다. (베기 피해 50%)",
            cost = "한 적에게 주는 피해가 낮습니다.", strong = "포위 무리", weak = "보스 단일",
            cards = new[] { G, S(4), S(8) }, evo = "se.eye",
            res1 = "모으는 동안 강철 의지 끌어당김이 기본 4칸으로 생깁니다.",
            res2 = "회전 베기가 끝난 자리에 2초 동안 소용돌이가 남아 적을 끌어당기고 벱니다.",
            mastery = "회전 베기가 끝나도 3초 동안 계속 돌며 둘레를 벱니다. (0.3초마다 베기 피해 25%)",
            color = new Color(0.75f, 0.6f, 1f), form = "전쟁 망치" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Wave, Parry, "se.giant"),                     // 거인 처단 (S6 · S7)
        Cross(Whirl, Blood, "se.vortex"),                   // 영혼 회오리 (S8 · S9)
        Cross(Wave, Blood, "s.x.bloodwave", "피의 검기", "검기가 맞힌 적마다 체력을 0.3 흡혈하고(1초에 최대 4), 피의 맹세의 피해 증가가 검기에도 붙습니다."),
        Cross(Dance, Whirl, "s.x.bladestorm", "칼날 폭풍", "미끄러지기가 끝날 때 원형 베기가 일어납니다. (베기 피해 50%)"),
    };

    // ================================================================= 수치 (카드 글과 같은 값 — 바꾸면 위 문장 · LocTable.Paths.Swordsman 도 같이)
    // 검기
    const float WaveMul = 0.6f, WavePerCard = 0.05f, WaveRange = 9f, WaveSpeed = 22f, WaveReach = 0.8f;
    const float XWaveMul = 0.7f, XBurstMul = 0.3f, XBurstR = 2.4f, XOffset = 1.5f, XMeet = 4.5f, ReturnMul = 0.8f;
    // 받아치기
    const float ParryWindow = 0.25f, ParryWindowM = 0.4f, ParryInvuln = 0.6f, ParryCounter = 3f, ParryCd = 1.2f, ParryCdHit = 2f, ParryChargeSlow = 1.3f;
    const float StanceWithin = 3f, StanceTime = 4f, StanceReach = 1.4f, StanceDmg = 1.5f, ReflectR = 6f, ReflectMul = 0.6f, MasteryGauge = 0.2f;
    const int ReflectMax = 12;
    // 피의 맹세
    const float BloodPay = 0.02f, BloodDmg = 0.35f, BloodLow50 = 0.25f, BloodLow25 = 0.5f, HealCut = 0.5f, BloodWaveMul = 0.5f, RageTime = 3f;
    const float PotionHealRate = 0.4f;          // Shop.Potions 의 회복 물약 (최대 체력 40% × 영혼의 무게) — 물약으로 오른 체력의 상한
    // 검무
    const float SlideDist = 1.5f, SlideDistM = 2.5f, SlideTime = 0.12f, SlideTimeM = 0.15f, MoveRate = 1.15f, StillRate = 0.8f;
    const float TrailMul = 0.2f, TrailLife = 1f, TrailWidth = 1.1f, DanceSpin = 0.3f, SlideInvuln = 0.1f;
    // 회오리
    const float WhirlReach = 0.85f, WhirlDmg = 0.8f, SweepMul = 0.5f, SweepGap = 0.5f, SweepR = 4f, PullR = 4f;
    const float VortexTime = 2f, VortexR = 3.5f, VortexMul = 0.15f, SpinOnTime = 3f, SpinOnGap = 0.3f, SpinOnMul = 0.25f;
    const float FullSpin = 5.5f;                // 회전 베기를 끝까지 모은 세기 (CharacterKit.UpdateCharged: 2 + 3.5)
    // 교차
    const float BladeStormMul = 0.5f, BloodWaveHeal = 0.3f, BloodWaveHealCap = 4f;
    // 개수 상한
    const int TrailMax = 12, VortexMax = 4, LaterMax = 24;

    // 판 기록 dealt_by 이름 (= 번역 키가 있는 입문 · 교차 이름) · 검기 명중을 가려내는 데도 씀
    const string SrcWave = "검기 방출", SrcParry = "받아치기", SrcBlood = "피의 맹세", SrcDance = "검무 보법", SrcWhirl = "회전 검사", SrcStorm = "칼날 폭풍";

    static readonly Color WaveTint = new Color(0.7f, 0.9f, 1f, 0.9f);
    static readonly Color BloodTint = new Color(1f, 0.35f, 0.4f, 0.95f);
    static readonly Color Gold = new Color(1f, 0.9f, 0.5f);
    static readonly Color DanceTint = new Color(0.55f, 1f, 0.75f);
    static readonly Color WhirlTint = new Color(0.8f, 0.7f, 1f);

    // ================================================================= 상태 (판마다 새 인스턴스 · 모두 until 값)
    // 검기: 같은 적을 맞힌 수 (공명 I) · 나중에 터질 것 (교차점 · 되돌아오는 검기)
    readonly Dictionary<int, int> waveHits = new Dictionary<int, int>();
    struct Later { public float at; public Vector3 pos; public float dmg; public bool back; }
    readonly List<Later> later = new List<Later>();
    // 받아치기
    float parryUntil, parryReadyAt, lastParryAt = -99f, stanceUntil;
    bool counterSwing;
    SkillGauge gauge;
    // 피의 맹세
    float bloodMul = 1f, rageUntil, lastHp = -1f, healWindowAt, healedInWindow;
    int rageStage = int.MinValue, potionSeen = -1;
    // 검무
    bool sliding, hasLast;
    float slideUntil, ghostAt, slideReach;
    Vector3 slideFrom, slideVel, lastPos;
    Vector2 moveDir;
    int danceSwings;
    readonly List<GameObject> trails = new List<GameObject>();
    // 회오리
    bool wasCharging;
    float sweepAt, spinOnUntil, spinOnAt;
    readonly List<GameObject> vortexes = new List<GameObject>();
    // HUD 배지 (값이 바뀔 때만 새 글)
    string badge;
    int badgeKey = int.MinValue;

    static PlayerController Player => Hostile.Player;
    static bool Alive(PlayerController p) => p != null && !p.IsDying;

    // ================================================================= 판 흐름 훅 (상태는 Paths.* 에서 다시 계산하므로 대부분 비움)
    public override void OnSeed(int path, bool replay) { }
    public override void OnSingle(int path, bool replay) { }
    public override void OnResonance(int path, int tier, bool replay) { }
    public override void OnComplete(int path, bool replay) { }
    public override void OnMastery(int path, bool replay) { }
    public override void OnCross(int cross, bool replay) { }

    // 판이 끝남: 바꿔 둔 플레이어 값은 없음 (미끄러지기 · 자세는 until 값이라 저절로 풀림) — 남은 장판만 정리
    public override void End()
    {
        foreach (GameObject g in trails) if (g != null) Object.Destroy(g);
        foreach (GameObject g in vortexes) if (g != null) Object.Destroy(g);
        trails.Clear();
        vortexes.Clear();
        later.Clear();
        waveHits.Clear();
        sliding = false;
    }

    // ================================================================= 평타 앞 (CharacterKit.Swing → SwordPathPre)
    public void SwingPre(CharacterKit kit, Vector2 dir, ref float reach, ref float half, ref float sig)
    {
        PlayerController p = Player;
        if (!Alive(p)) return;
        // 검기 방출: 휘두르기 범위 −20%
        if (Paths.Has(Wave)) reach *= WaveReach;
        // 회전 검사: 몸 둘레 원형 (반경 85% · 피해 80%)
        if (Paths.Has(Whirl))
        {
            reach *= WhirlReach;
            half = 180f;
            sig *= WhirlDmg;
        }
        // 받아치기: 역습 자세 (공명 II) · 막은 뒤 다음 베기 ×3
        if (Paths.Has(Parry))
        {
            float pw = Paths.Power(Parry);
            if (Time.time < stanceUntil)
            {
                reach *= StanceReach;
                sig *= 1f + (StanceDmg - 1f) * pw;
            }
            if (counterSwing)
            {
                counterSwing = false;
                sig *= 1f + (ParryCounter - 1f) * pw;
                Vector3 at = p.transform.position;
                Fx.Spawn("fx_shock", at, 4f, new Color(Gold.r, Gold.g, Gold.b, 0.85f), 22f);
                Fx.Spawn("fx_sparkle", at + (Vector3)(dir * 1.5f), 2f, Gold, 22f);
                Hostile.Play("crack", 0.55f, 1.1f);
            }
        }
        // 피의 맹세: 지금 체력 2% 를 바치고 베기 피해 +35% (공명 I: 체력 50% 아래 +25% · 25% 아래 +50%)
        bloodMul = 1f;
        if (Paths.Has(Blood))
        {
            if (p.PlayerHealth > 1f) p.PlayerHealth = Mathf.Max(1f, p.PlayerHealth - p.PlayerHealth * BloodPay);
            float bonus = BloodDmg;
            if (Paths.Res(Blood) >= 1 && p.PlayerMaxHealth > 0f)
            {
                float hp = p.PlayerHealth / p.PlayerMaxHealth;
                bonus += hp < 0.25f ? BloodLow25 : hp < 0.5f ? BloodLow50 : 0f;
            }
            bloodMul = 1f + bonus * Paths.Power(Blood);
            sig *= bloodMul;
            Fx.Spawn("fx_bleed", p.transform.position, 1.6f, new Color(1f, 1f, 1f, 0.7f), 20f);
        }
    }

    // ================================================================= 평타 뒤 (CharacterKit.Swing → SwordPathPost)
    public void SwingPost(CharacterKit kit, Vector3 origin, Vector2 dir, float reach, int hits)
    {
        PlayerController p = Player;
        if (!Alive(p) || kit == null) return;
        float dmg = kit.SwordDamage;

        if (Paths.Has(Wave)) FireWaves(kit, origin, dir, dmg);

        if (Paths.Has(Blood))
        {
            // 흡혈 2배 (근접 특성 회복 · 흡혈 베기를 한 번 더, 극의 분노 중엔 3배)
            if (hits > 0)
            {
                float heal = Mathf.Min(hits, 3) * (0.1f + 0.35f * kit.card[1]);     // CharacterKit.SwingCards 의 회복과 같은 식
                Heal(p, heal * (Time.time < rageUntil ? 2f : 1f));
            }
            // 공명 II: 혈갑 보호막이 가득이면 피의 파동
            if (Paths.Res(Blood) >= 2 && SignatureSkills.BloodShieldFull)
            {
                kit.SwordWave(origin + (Vector3)(dir * 1f), dir, dmg * BloodWaveMul * Paths.Power(Blood) * bloodMul, WaveRange, BloodTint, SrcBlood);
                Hostile.Play("whoosh", 0.45f, 0.7f);
            }
        }

        // 회전 검사: 원형 베기 그림
        if (Paths.Has(Whirl))
            Fx.Spawn("fx_spinslash", origin, reach * 2.4f, new Color(WhirlTint.r, WhirlTint.g, WhirlTint.b, 0.7f), 26f);

        if (Paths.Has(Dance))
        {
            if (kit.SwordMoving) StartSlide(kit, p, reach);
            // 공명 II: 검무 최대 중첩이면 다섯 번째마다 회전 베기 (30% 세기)
            if (Paths.Res(Dance) >= 2 && SignatureSkills.DanceFull)
            {
                if (++danceSwings >= 5)
                {
                    danceSwings = 0;
                    AutoSpin(kit, p);
                }
            }
            else danceSwings = 0;
        }
    }

    // ---------------- 검기
    void FireWaves(CharacterKit kit, Vector3 origin, Vector2 dir, float dmg)
    {
        float mul = (WaveMul + WavePerCard * kit.card[0]) * Paths.Power(Wave);
        bool bloody = Paths.CrossOn(CrossBloodWave);
        if (bloody) mul *= bloodMul;                // 피의 검기: 피의 맹세 피해 증가가 검기에도
        Color tint = bloody ? new Color(1f, 0.55f, 0.6f, 0.9f) : WaveTint;
        bool back = Paths.Mastered(Wave);
        if (Paths.Res(Wave) >= 2)
        {
            // 공명 II: 두 줄기가 앞에서 엇갈려 X자 · 교차점에서 잔상이 터짐
            Vector2 side = new Vector2(-dir.y, dir.x);
            Vector3 meet = origin + (Vector3)(dir * XMeet);
            float time = 0f;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 start = origin + (Vector3)(dir * 0.6f + side * (XOffset * s));
                Vector2 d = ((Vector2)(meet - start)).normalized;
                kit.SwordWave(start, d, dmg * mul * XWaveMul, WaveRange, tint, SrcWave);
                time = Vector3.Distance(start, meet) / WaveSpeed;
                if (back) AddLater(new Later { at = Time.time + WaveRange / WaveSpeed, pos = start + (Vector3)(d * WaveRange), dmg = dmg * mul * XWaveMul * ReturnMul, back = true });
            }
            AddLater(new Later { at = Time.time + time, pos = meet, dmg = dmg * XBurstMul * Paths.Power(Wave), back = false });
        }
        else
        {
            Vector3 start = origin + (Vector3)(dir * 1f);
            kit.SwordWave(start, dir, dmg * mul, WaveRange, tint, SrcWave);
            if (back) AddLater(new Later { at = Time.time + WaveRange / WaveSpeed, pos = start + (Vector3)(dir * WaveRange), dmg = dmg * mul * ReturnMul, back = true });
        }
    }

    void AddLater(Later l)
    {
        while (later.Count >= LaterMax) later.RemoveAt(0);
        later.Add(l);
    }

    void TickLater(CharacterKit kit, PlayerController p)
    {
        for (int i = later.Count - 1; i >= 0; i--)
        {
            if (i >= later.Count) continue;
            Later l = later[i];
            if (Time.time < l.at) continue;
            later.RemoveAt(i);
            if (l.back)
            {
                // 극의: 끝에서 영웅 쪽으로 되돌아옴
                Vector2 d = ((Vector2)(p.transform.position - l.pos)).normalized;
                if (d.sqrMagnitude < 0.01f) continue;
                Bullet b = kit.SwordWave(l.pos, d, l.dmg, WaveRange, new Color(0.85f, 0.95f, 1f, 0.8f), SrcWave);
                if (b != null) b.hitOnce = new HashSet<int>();
            }
            else
            {
                // 공명 II: 교차점 잔상 폭발
                Paths.Circle(l.pos, XBurstR, l.dmg, 0.8f, SrcWave);
                Fx.Spawn("fx_spinslash", l.pos, XBurstR * 2.4f, new Color(0.75f, 0.9f, 1f, 0.85f), 26f);
                Fx.Spawn("fx_shock", l.pos, XBurstR * 2f, new Color(0.6f, 0.8f, 1f, 0.8f), 22f);
                Hostile.Play("slash", 0.4f, 1.4f);
            }
        }
    }

    // ---------------- 피의 맹세
    static void Heal(PlayerController p, float amount)
    {
        if (!Alive(p) || amount <= 0f || p.PlayerHealth <= 0f) return;
        p.PlayerHealth = Mathf.Min(p.PlayerMaxHealth, p.PlayerHealth + amount);
    }

    // 대가: 물약 · 재생 회복 −50% (PlayerController 재생 · Shop 물약이 더한 만큼의 절반을 되돌림 — 공용 파일을 고치지 않고)
    void TickBlood(PlayerController p)
    {
        if (p.regenPerSecond > 0f && p.PlayerHealth > 1f && p.PlayerHealth < p.PlayerMaxHealth)
            p.PlayerHealth = Mathf.Max(1f, p.PlayerHealth - p.regenPerSecond * HealCut * Time.deltaTime);
        int bought = RunCounters.Get("potion");
        if (potionSeen >= 0 && bought > potionSeen && lastHp >= 0f)
        {
            float gain = p.PlayerHealth - lastHp;
            float cap = (bought - potionSeen) * p.PlayerMaxHealth * PotionHealRate * SoulWeight.HealMul;
            if (gain > 0f) p.PlayerHealth = Mathf.Max(1f, p.PlayerHealth - Mathf.Min(gain, cap) * HealCut);
        }
        potionSeen = bought;
        lastHp = p.PlayerHealth;
    }

    // 교차 「피의 검기」: 검기 명중마다 흡혈 (1초에 최대 4)
    void BloodWaveLeech(PlayerController p)
    {
        if (Time.time >= healWindowAt) { healWindowAt = Time.time + 1f; healedInWindow = 0f; }
        if (healedInWindow >= BloodWaveHealCap) return;
        float h = Mathf.Min(BloodWaveHeal, BloodWaveHealCap - healedInWindow);
        healedInWindow += h;
        Heal(p, h);
    }

    // ---------------- 검무
    void StartSlide(CharacterKit kit, PlayerController p, float reach)
    {
        if (moveDir.sqrMagnitude < 0.01f) return;
        if (sliding) EndSlide(kit, p);
        bool m = Paths.Mastered(Dance);
        float dist = m ? SlideDistM : SlideDist, time = m ? SlideTimeM : SlideTime;
        slideFrom = p.transform.position;
        slideVel = (Vector3)(moveDir * (dist / time));
        slideUntil = Time.time + time;
        slideReach = reach;
        sliding = true;
        ghostAt = 0f;
        if (m) Paths.Invuln(SlideInvuln);           // 극의: 미끄러질 때 0.1초 무적
        Hostile.Play("whoosh", 0.4f, 1.6f);
    }

    void TickSlide(CharacterKit kit, PlayerController p)
    {
        if (Time.time >= slideUntil) { EndSlide(kit, p); return; }
        Vector3 pos = p.transform.position;
        Vector3 next = pos + slideVel * Time.deltaTime;
        if (Hostile.IsWall(next) || !Hostile.InArena(next)) { EndSlide(kit, p); return; }
        p.transform.position = next;
        if (Time.time >= ghostAt)
        {
            ghostAt = Time.time + 0.04f;
            Sprite s = kit.SwordBodySprite;
            if (s != null)
            {
                GameObject g = SpecialAbilities.MakeSprite("SlideGhost", s, pos, 1f, new Color(DanceTint.r, DanceTint.g, DanceTint.b, 0.5f), "Character", -1);
                if (g != null)
                {
                    g.transform.localScale = p.transform.lossyScale;
                    if (g.TryGetComponent(out SpriteRenderer sr)) sr.flipX = kit.SwordFlipX;
                    g.AddComponent<FadeOut>().duration = 0.25f;
                }
            }
        }
    }

    void EndSlide(CharacterKit kit, PlayerController p)
    {
        sliding = false;
        Vector3 to = p.transform.position;
        float dmg = kit.SwordDamage;
        // 공명 I: 미끄러진 길에 칼날 잔상
        if (Paths.Res(Dance) >= 1 && (to - slideFrom).sqrMagnitude > 0.25f) BladeTrail(slideFrom, to, dmg * TrailMul * Paths.Power(Dance));
        // 교차 「칼날 폭풍」: 미끄러지기 끝에 원형 베기
        if (Paths.CrossOn(CrossBladeStorm))
        {
            float r = Mathf.Max(2f, slideReach * 0.8f);
            Paths.Circle(to, r, dmg * BladeStormMul * Paths.Power(Dance), 1.5f, SrcStorm);
            Fx.Spawn("fx_spinslash", to, r * 2.4f, new Color(0.7f, 0.85f, 0.95f, 0.85f), 26f);
            Hostile.Play("slash", 0.5f, 1.3f);
        }
    }

    void BladeTrail(Vector3 from, Vector3 to, float dmg)
    {
        Paths.Trim(trails, TrailMax);
        Vector3 mid = (from + to) * 0.5f;
        float len = Vector3.Distance(from, to);
        float reachR = len * 0.5f + TrailWidth;
        SigZone z = Paths.Zone(mid, TrailWidth, TrailLife, 0.25f, zone =>
        {
            if (!Alive(Player)) return;             // 쓰러진 뒤에는 베지 않음
            using var list = PhysHits.Circle(mid, reachR, true);
            foreach (Collider2D c in list)
            {
                if (c == null || !Paths.IsFoe(c)) continue;
                if (Hostile.DistanceToSegment(c.transform.position, from, to) > TrailWidth) continue;
                Paths.Deal(c.gameObject, dmg, Vector3.zero, 0f, SrcDance);
            }
        });
        if (z == null) return;
        z.segment = true;
        z.a = from;
        z.b = to;
        float rot = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        FxAnim a = Fx.Play("fx_swordwave", mid, len + 1.5f, new Color(DanceTint.r, DanceTint.g, DanceTint.b, 0.6f), 12f, rot, 13, true, TrailLife);
        if (a != null) a.transform.SetParent(z.transform, true);
        trails.Add(z.gameObject);
    }

    void AutoSpin(CharacterKit kit, PlayerController p)
    {
        Vector3 at = p.transform.position;
        float r = 5f * kit.SwordUltMul;
        Paths.Circle(at, r, kit.SwordDamage * FullSpin * DanceSpin * kit.SwordUltMul * Paths.Power(Dance), 2f, SrcDance);
        Fx.Spawn("fx_spinslash", at, r * 2.4f, new Color(DanceTint.r, DanceTint.g, DanceTint.b, 0.85f), 22f);
        Fx.Spawn("fx_shock", at, r * 2f, new Color(0.6f, 1f, 0.8f, 0.7f), 20f);
        Hostile.Play("spin", 0.6f, 1.2f);
    }

    // ---------------- 회오리
    void TickWhirl(CharacterKit kit, PlayerController p)
    {
        Vector3 pos = p.transform.position;
        bool ch = kit.Charging;
        if (ch && !wasCharging) sweepAt = Time.time + SweepGap;
        wasCharging = ch;
        if (ch)
        {
            // 입문: 모으는 동안 0.5초마다 둘레 베기
            if (Time.time >= sweepAt)
            {
                sweepAt = Time.time + SweepGap;
                Paths.Circle(pos, SweepR, kit.SwordDamage * SweepMul * Paths.Power(Whirl), 0.8f, SrcWhirl);
                Fx.Spawn("fx_spinslash", pos, SweepR * 2.4f, new Color(WhirlTint.r, WhirlTint.g, WhirlTint.b, 0.6f), 26f);
                Hostile.Play("whoosh", 0.3f, 1.4f);
            }
            // 공명 I: 강철 의지가 없어도 기본 4칸 끌어당김 (있으면 강철 의지가 더 넓게 끌어당김 · SignatureSkills.SwordTick)
            if (Paths.Res(Whirl) >= 1 && Paths.Level(LevelShop.SigFirstId + 4) <= 0)
            {
                using var list = PhysHits.Circle(pos, PullR, true);
                foreach (Collider2D c in list)
                {
                    if (c == null || !c.CompareTag("enermy")) continue;
                    if ((c.transform.position - pos).sqrMagnitude < 1.4f) continue;
                    c.transform.position = Vector3.MoveTowards(c.transform.position, pos, 2.5f * Time.deltaTime);
                }
            }
        }
        // 극의: 회전 베기 뒤 계속 돎
        if (Time.time < spinOnUntil && Time.time >= spinOnAt)
        {
            spinOnAt = Time.time + SpinOnGap;
            float r = 5f * kit.SwordUltMul * 0.8f;
            Paths.Circle(pos, r, kit.SwordDamage * SpinOnMul * Paths.Power(Whirl), 1f, SrcWhirl);
            Fx.Spawn("fx_spinslash", pos, r * 2.4f, new Color(WhirlTint.r, WhirlTint.g, WhirlTint.b, 0.55f), 28f);
            Hostile.Play("whoosh", 0.25f, 1.6f);
        }
    }

    void Vortex(CharacterKit kit, Vector3 at)
    {
        Paths.Trim(vortexes, VortexMax);
        float dmg = kit.SwordDamage * VortexMul * Paths.Power(Whirl);
        SigZone z = Paths.Zone(at, VortexR, VortexTime, 0.25f, zone =>
        {
            if (zone == null || !Alive(Player)) return;
            Vector3 c0 = zone.transform.position;
            using var list = PhysHits.Circle(c0, VortexR, true);
            foreach (Collider2D c in list)
            {
                if (c == null || !Paths.IsFoe(c)) continue;
                if (c.CompareTag("enermy")) c.transform.position = Vector3.MoveTowards(c.transform.position, c0, 0.6f);      // 보스는 끌지 않음
                Paths.Deal(c.gameObject, dmg, Vector3.zero, 0f, SrcWhirl);
            }
        }, "fx_tornado", new Color(WhirlTint.r, WhirlTint.g, WhirlTint.b, 0.55f), VortexR * 2.2f);
        if (z != null) vortexes.Add(z.gameObject);
    }

    // ---------------- 받아치기
    void TickParry(CharacterKit kit, PlayerController p)
    {
        if (GameInput.UltDown && Time.time >= parryReadyAt)
        {
            float window = Paths.Mastered(Parry) ? ParryWindowM : ParryWindow;
            parryUntil = Time.time + window;
            parryReadyAt = Time.time + ParryCd;
            ShockRing.Spawn(p.transform.position, 2.4f, 1.2f, window, new Color(Gold.r, Gold.g, Gold.b, 0.85f), 0.18f);
            Hostile.Play("whoosh", 0.35f, 1.8f);
        }
        // 대가: 회전 베기를 끝까지 모으는 시간 +30% (CharacterKit.UpdateCharged 가 더한 만큼에서 덜어 냄 · 1.5초 → 1.95초)
        if (kit.Charging && kit.SwordCharge < 1f)
            kit.SwordCharge = kit.SwordCharge - Time.deltaTime / 1.5f * (1f - 1f / ParryChargeSlow);
    }

    void ParrySuccess(PlayerController p)
    {
        parryUntil = 0f;
        parryReadyAt = Time.time + ParryCdHit;
        Paths.Invuln(ParryInvuln);
        counterSwing = true;
        SignatureSkills.PathParried();          // 역습 · 완벽한 반격
        if (Paths.Res(Parry) >= 2 && Time.time - lastParryAt <= StanceWithin) stanceUntil = Time.time + StanceTime;
        lastParryAt = Time.time;

        Vector3 at = p.transform.position;
        Fx.Spawn("fx_shock", at, 5f, new Color(Gold.r, Gold.g, Gold.b, 0.9f), 24f);
        Fx.Spawn("fx_spark", at, 2.6f, Color.white, 24f);
        ShockRing.Spawn(at, 0.3f, 3.6f, 0.3f, new Color(1f, 0.95f, 0.7f, 0.95f), 0.25f);
        Hostile.Play("ding", 0.8f, 1.5f);
        Hostile.Play("clank", 0.5f, 1.2f);
        Hostile.Shake(0.08f);
        SpecialAbilities.SharedFx?.FloatText(at + Vector3.up * 1.6f, Loc.T(Time.time < stanceUntil ? "역습 자세!" : "막기!"), Gold, 4.5f, 0.2f);

        if (Paths.Res(Parry) >= 1) Reflect(at);
        if (Paths.Mastered(Parry))
        {
            if (gauge == null) gauge = Object.FindFirstObjectByType<SkillGauge>();
            if (gauge != null && !gauge.IsFull()) gauge.AddSkillPoint(gauge.MaxSkillPoint * MasteryGauge);
        }
    }

    // 공명 I: 둘레 적 투사체를 되돌려 보냄 (가까운 적에게 · 없으면 온 방향으로)
    void Reflect(Vector3 at)
    {
        SignatureSkills ss = SignatureSkills.Ensure();
        CharacterKit kit = CharacterKit.Instance;
        if (ss == null || kit == null) return;
        float dmg = kit.SwordDamage * ReflectMul * Paths.Power(Parry);
        int n = 0;
        for (int i = HostileProjectile.Live.Count - 1; i >= 0 && n < ReflectMax; i--)
        {
            if (i >= HostileProjectile.Live.Count) continue;
            HostileProjectile hp = HostileProjectile.Live[i];
            if (hp == null) continue;
            Vector3 from = hp.transform.position;
            if ((from - at).sqrMagnitude > ReflectR * ReflectR) continue;
            Vector2 d = -hp.dir;
            List<Transform> near = Paths.Nearest(from, 14f, 1);
            if (near.Count > 0 && near[0] != null) d = (Vector2)(near[0].position - from);
            if (d.sqrMagnitude < 0.01f) d = Random.insideUnitCircle;
            ss.Shot(from, d.normalized, dmg, 1, 26f, 14f, "fx_swordwave", 1.6f, new Color(Gold.r, Gold.g, Gold.b, 0.95f));
            Fx.Spawn("fx_spark", from, 1.2f, Gold, 24f);
            Object.Destroy(hp.gameObject);
            n++;
        }
    }

    // ================================================================= 전투 훅
    public override float Taken(float dmg)
    {
        if (dmg <= 0f) return dmg;
        PlayerController p = Player;
        if (!Alive(p)) return dmg;
        // 받아치기: 막기 판정 중이면 피해 0 · 0.6초 무적 (무적은 Paths.Invuln 으로만, 시간이 지나면 저절로 풀림)
        if (Paths.Has(Parry) && Time.time < parryUntil)
        {
            ParrySuccess(p);
            return 0f;
        }
        // 피의 맹세 극의: 장마다 한 번 체력 1로 버티고 3초 흡혈 3배
        if (Paths.Mastered(Blood) && dmg >= p.PlayerHealth && p.PlayerHealth > 1f && rageStage != Chapters.CurrentStage)
        {
            rageStage = Chapters.CurrentStage;
            rageUntil = Time.time + RageTime;
            Vector3 at = p.transform.position;
            Fx.Spawn("fx_bleed", at, 5f, Color.white, 16f);
            Fx.Spawn("fx_shock", at, 8f, new Color(BloodTint.r, BloodTint.g, BloodTint.b, 0.85f), 20f);
            Hostile.Play("thump", 0.8f, 0.7f);
            SpecialAbilities.SharedFx?.FloatText(at + Vector3.up * 1.6f, Loc.T("피의 맹세"), BloodTint, 5f, 0f);
            return Mathf.Max(0f, p.PlayerHealth - 1f);
        }
        return dmg;
    }

    public override void Hit(GameObject target, float dmg, bool killed, bool proc)
    {
        // 갈래 검기의 명중만 (총알이 만들어질 때 붙인 출처 이름 · 갈래 효과가 준 피해는 proc == false)
        if (!proc || target == null || DamageSource.Current != SrcWave || !Paths.Has(Wave)) return;
        PlayerController p = Player;
        if (!Alive(p)) return;
        if (Paths.CrossOn(CrossBloodWave)) BloodWaveLeech(p);
        if (Paths.Res(Wave) < 1 || killed) return;
        int id = target.GetInstanceID();
        waveHits.TryGetValue(id, out int n);
        if (++n >= 3)
        {
            n = 0;
            SignatureSkills.CrackNow(target, Paths.Power(Wave));
        }
        if (waveHits.Count > 200) waveHits.Clear();
        waveHits[id] = n;
    }

    public override void Ult()
    {
        if (!Paths.Has(Whirl)) return;
        PlayerController p = Player;
        CharacterKit kit = CharacterKit.Instance;
        if (!Alive(p) || kit == null) return;
        Vector3 at = p.transform.position;
        if (Paths.Res(Whirl) >= 2) Vortex(kit, at);      // 공명 II: 끝난 자리에 소용돌이
        if (Paths.Mastered(Whirl))
        {
            spinOnUntil = Time.time + SpinOnTime;
            spinOnAt = Time.time + SpinOnGap;
            FxAnim t = Fx.Play("fx_tornado", at, 10f * kit.SwordUltMul, new Color(WhirlTint.r, WhirlTint.g, WhirlTint.b, 0.5f), 14f, 0f, 13, true, SpinOnTime);
            if (t != null) t.follow = p.transform;
        }
    }

    // 검무: 움직이는 동안 공격 속도 +15%, 멈춰 휘두르면 −20%
    public override float RateMul()
    {
        if (!Paths.Has(Dance)) return 1f;
        CharacterKit kit = CharacterKit.Instance;
        return kit != null && kit.SwordMoving ? MoveRate : StillRate;
    }

    public override float Outgoing(GameObject target, float dmg) => 1f;
    public override void Killed(Vector3 pos) { }
    public override float MoveMul() => 1f;
    public override bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;

    public override void Tick()
    {
        PlayerController p = Player;
        if (!Alive(p)) return;
        CharacterKit kit = CharacterKit.Instance;
        if (kit == null || kit.Id != CharacterId.Swordsman) return;
        // 진행 방향 (미끄러지는 동안은 그대로)
        Vector3 pos = p.transform.position;
        if (hasLast && !sliding)
        {
            Vector2 d = pos - lastPos;
            if (d.sqrMagnitude > 0.000025f) moveDir = d.normalized;
        }
        lastPos = pos;
        hasLast = true;

        if (Paths.Has(Parry)) TickParry(kit, p);
        if (Paths.Has(Blood)) TickBlood(p);
        if (sliding) TickSlide(kit, p);
        if (Paths.Has(Whirl)) TickWhirl(kit, p);
        if (later.Count > 0) TickLater(kit, p);
    }

    // HUD 받아치기 칸: 역습 자세 남은 시간 · 다음 베기 ×3 (값이 바뀔 때만 새 글)
    public override string Badge(int path)
    {
        if (path != Parry || !Paths.Has(Parry)) return null;
        if (Time.time < stanceUntil)
        {
            int k = Mathf.CeilToInt((stanceUntil - Time.time) * 10f);
            if (k != badgeKey) { badgeKey = k; badge = (k / 10f).ToString("0.0") + "s"; }
            return badge;
        }
        return counterSwing ? "x3" : null;
    }
}
