using System.Collections.Generic;
using UnityEngine;

// 도적 갈래 5개 (기획서 3-3) — 데이터 · 효과
// 도적 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Rogue.cs · 도적 기존 파일(SignatureSkills.Rogue.cs · CharacterKit.Rogue.cs ·
//   CharacterKit.cs 안의 도적 전용 부분만) — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 효과가 어디서 나나 (2026-10-19)
//   출혈       Hit(명중마다 중첩 · RogueBleed) · Outgoing(직접 피해 −35%) · DashEnd(공명 I 즉시 폭발) · Hit(공명 II 전염)
//   그림자     CharacterKit.Rogue RpUlt(돌진 모아 두기) · DashStart(미끼 분신 RogueDecoy) · SignatureSkills.RoguePathSmoke(공명 I 연막)
//              CharacterKit.Rogue RpShot(공명 II 연막 안 치명) · CharacterKit.Rogue RpCutBleed(대가: 돌진 출혈 −30%)
//   회귀 칼날  CharacterKit.Rogue RpShot → ReturningShuriken.ThrowPath (8칸 왕복 · 받으면 탄창 · 공명 I 던지기 연습 · II 몸 둘레 한 바퀴 · 극의 두 번)
//   질주 암살  CharacterKit.Rogue RogueDashDist · RpCutBleed(출혈 대신 직접 피해) · Ult(게이지 30% 돌려받기 = 게이지 −30%) · RateMul(대가)
//              DashEnd(공명 I 게이지 25% · 공명 II 길이 엇갈린 자리 폭발 · 극의 연속 돌진)
//   광란       Tick(탄창 +4) · RateMul · Killed(중첩) · Taken(대가) · Hurt(중첩 잃음) · CharacterKit.Rogue RpVolley(공명 I 두 개씩) · Killed(공명 II 표창 비)
// 밸런스 (기획서 4-2 · 1.3.7 기준 보스 화력 ±8% 안을 목표로 한 계산 · 실측으로 맞출 것)
//   출혈: 직접 −35% ↔ 5중첩 출혈 = 이 적이 받는 직접 피해 흐름의 55% (0.65 + 0.65 × 0.55 ≈ 1.0) — 공격 속도 · 멀티샷과 상관없이 늘 같은 비율
//   광란: 공격 속도 +25% ↔ 표창 피해 −20% (보스 앞 1.0) · 탄창 10 이라 장전이 줄어 약 +6%. 처치 중첩(무리)이 강점
//   질주 암살: 평타 −20% ↔ 돌진 1.43배 자주 · 돌진 한 번 220% + 500% (예전 220% + 출혈 600%) → 약 −5%
//   회귀 칼날: 8칸 안 보스만 맞힘 ↔ 돌아오며 25% · 탄창이 거의 마르지 않음 → 약 +5% (거리를 지킬 때)
//   그림자: 돌진 출혈 −30% (약 −5%) ↔ 공명 II 연막 안 치명 ×2 (연막 안에 서 있는 동안만) → 약 +5%
public sealed class RoguePaths : HeroPaths
{
    public const int Bleed = 0, Shadow = 1, Boomerang = 2, Dash = 3, Frenzy = 4;
    // 새 교차 번호 (Crosses 배열 순서)
    public const int CrossShadowBleed = 2, CrossStormDash = 3;

    // ---------------- 수치 (카드 글 · LocTable.Paths.Rogue 와 같게)
    public const float BleedDirectMul = 0.65f;      // 출혈 대가: 직접 피해 −35%
    public const float BleedShare = 0.55f;          // 5중첩 출혈의 초당 피해 = 이 적이 받는 직접 피해 흐름 × 0.55
    public const int BleedCap = 5, BleedBossCap = 10;   // 극의: 보스는 10중첩 (6중첩부터는 하나에 +6%)
    public const float BleedMasteryMul = 1.3f;      // 극의: 출혈 피해 ×1.3
    public const float ShadowDashBleedMul = 0.7f;   // 그림자 대가: 돌진 출혈 −30%
    public const float DecoyLife = 1.5f;            // 미끼 분신이 적을 끄는 시간
    public const int MaxDecoys = 3;                 // 분신 상한 (안내서 6-3)
    public const float ReturnDist = 8f;             // 회귀 칼날: 8칸에서 꺾임
    public const float ReturnDmgMul = 0.25f;        // 돌아오는 길 피해 (던진 표창의 25%)
    public const float DashDistMul = 1.5f;          // 질주 암살: 돌진 거리 ×1.5
    public const float DashGaugeBack = 0.3f;        // 게이지 −30% (쓰자마자 30% 돌려받음)
    public const float DashCutMul = 5f;             // 돌진 경로 직접 피해 (공격력 500%, 원래 220% 에 더해)
    public const float DashRate = 0.8f;             // 대가: 평타 공격 속도 −20%
    public const float DashKillBack = 0.25f;        // 공명 I: 돌진으로 처치하면 게이지 25%
    public const int DashChainMax = 2;              // 극의: 다시 돌진 2번 (모두 3연속)
    public const int FrenzyMagAdd = 4;              // 광란: 탄창 6 → 10
    public const float FrenzyRate = 1.25f, FrenzyStackRate = 0.03f, FrenzyStackLife = 1.5f;
    public const int FrenzyMax = 10, FrenzyStormStacks = 5, FrenzyRainKills = 10;
    public const float FrenzyShotMul = 0.8f;        // 광란 대가: 표창 피해 −20%
    public const float FrenzyTakenMul = 1.15f;      // 광란 대가: 받는 피해 +15%
    public const float FrenzyTwinMul = 0.7f;        // 공명 I: 덧붙는 표창 피해

    public override CharacterId Who => CharacterId.Rogue;
    public override string IconKey => "rogue";

    // 지금 판의 도적 갈래 효과 (갈래 규칙이 아니면 null) — 도적 기존 코드(SignatureSkills.Rogue · CharacterKit.Rogue)가 물음
    public static RoguePaths Cur => Paths.Active ? Paths.Hero as RoguePaths : null;

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "r.bleed", name = "출혈", tagline = "5중첩 출혈 · 전염",
            seed = "독 바른 칼날", seedDesc = "모든 표창이 출혈을 입히고, 출혈이 5중첩까지 쌓입니다.",
            cost = "직접 피해가 35% 줄어듭니다.", strong = "보스 지속 · 무리 전염", weak = "즉시 처치",
            cards = new[] { M, S(1), S(2), A }, evo = "re.sea",
            res1 = "5중첩 적을 돌진으로 지나가면 출혈 폭발이 바로 터집니다.",
            res2 = "출혈로 쓰러진 적이 둘레 둘에게 출혈 2중첩을 옮깁니다.",
            mastery = "출혈 피해가 1.3배가 되고, 보스에게는 10중첩까지 쌓입니다.",
            color = new Color(0.9f, 0.2f, 0.3f), form = "독침 대롱" },
        new PathDef {
            key = "r.shadow", name = "그림자", tagline = "돌진 2회 · 미끼 분신",
            seed = "그림자 걸음", seedDesc = "돌진을 2번 모아 둘 수 있고, 돌진을 시작한 자리에 미끼 분신이 1.5초 동안 남아 적을 끌어들입니다.",
            cost = "돌진 출혈이 30% 줄어듭니다.", strong = "생존 · 회피 · 보스 시선 돌리기", weak = "화력",
            cards = new[] { K, S(0), S(8) }, evo = "re.phantom",
            res1 = "분신이 사라질 때 연막이 퍼집니다.",
            res2 = "연막 안에서 던진 표창은 치명타(×2)가 됩니다.",
            mastery = "돌진을 3번 모아 두고, 분신도 표창을 함께 던집니다.",
            color = new Color(0.55f, 0.4f, 0.85f), form = "살상 와이어" },
        new PathDef {
            key = "r.return", name = "회귀 칼날", tagline = "되돌아오는 표창 · 탄 경제",
            seed = "곡선 투척", seedDesc = "표창이 8칸 끝에서 되돌아오며 다시 벱니다. (피해 25%) 돌아온 표창을 몸으로 받으면 탄창이 한 발 찹니다. (던질 때마다 한 번)",
            cost = "사거리가 무한에서 8칸으로 줄어듭니다.", strong = "근중거리 무리 · 탄 경제", weak = "원거리",
            cards = new[] { B, S(4), S(6) }, evo = "re.return",
            res1 = "왕복으로 같은 적을 맞히면 던지기 연습이 2중첩 쌓입니다.",
            res2 = "돌아올 때 몸 둘레를 한 바퀴 돈 뒤 돌아옵니다.",
            mastery = "표창이 두 번 왕복합니다.",
            color = new Color(0.5f, 0.85f, 0.95f) },
        new PathDef {
            key = "r.dash", name = "질주 암살", tagline = "긴 돌진 직접 피해 · 순간 화력",
            seed = "질주 암살", seedDesc = "돌진 거리 ×1.5, 돌진 게이지 −30%, 돌진 경로가 출혈 대신 직접 피해를 줍니다. (공격력 500%)",
            cost = "평타 공격 속도가 20% 느려집니다.", strong = "기동 · 관통 · 보스 순간 화력", weak = "게이지 의존",
            cards = new[] { G, S(5), S(3) }, evo = "re.road",
            res1 = "돌진으로 처치하면 게이지 25%를 돌려받습니다.",
            res2 = "돌진한 길이 X자로 엇갈리면 교차점이 터집니다. (공격력 300%)",
            mastery = "돌진 중 처치하면 바로 다시 돌진합니다. (3연속)",
            color = new Color(0.95f, 0.75f, 0.3f) },
        new PathDef {
            key = "r.frenzy", name = "광란", tagline = "처치마다 빨라짐 · 무리",
            seed = "광란", seedDesc = "탄창 6 → 10, 공격 속도 +25%, 처치할 때마다 1.5초 동안 공격 속도 +3%. (10중첩)",
            cost = "받는 피해가 15% 늘고, 표창 피해가 20% 줄어듭니다.", strong = "무리", weak = "피격 위험",
            cards = new[] { H, S(7), S(9), P }, evo = "re.peak",
            res1 = "10중첩이면 표창을 두 개씩 던집니다.",
            res2 = "빈 주머니의 비가 10처치마다도 내립니다.",
            mastery = "맞아도 중첩이 떨어지지 않습니다.",
            color = new Color(1f, 0.45f, 0.2f), form = "도박 카드" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Bleed, Dash, "re.executioner"),               // 처형자의 길 (재료 변경: A · S3)
        Cross(Frenzy, Boomerang, "re.acrobat"),             // 칼날 곡예 (P · S6)
        Cross(Shadow, Bleed, "r.x.shadowbleed", "그림자 출혈", "분신이 표창을 던지고, 분신의 표창이 출혈 2중첩을 입힙니다."),
        Cross(Dash, Frenzy, "r.x.stormdash", "폭풍 질주", "돌진이 광란 5중첩을 바로 채웁니다."),
    };

    // ================================================================= 판 상태 (판마다 새 인스턴스 · static 상태 없음)
    // 광란
    int frenzy;                 // 지금 중첩 (0 ~ 10)
    float frenzyUntil;          // 중첩이 사라지는 시각 (처치마다 갱신 · until 방식)
    int frenzyKills;            // 공명 II: 빈 주머니의 비까지 남은 처치
    bool magAdded;              // 탄창 +4 를 넣었나 (End 에서 되돌림)
    // 질주 암살
    bool dashKilled;            // 이번 돌진 중 처치가 있었나
    int chain;                  // 극의: 이어서 다시 돌진한 수
    struct Trail { public Vector3 a, b; public float until; }
    readonly List<Trail> trails = new List<Trail>(8);
    // 그림자
    readonly List<GameObject> decoys = new List<GameObject>(MaxDecoys + 1);
    // 출혈 전염 (한 번에 너무 많이 번지지 않게: 0.25초에 12번까지)
    float spreadWindow;
    int spreadCount;
    // HUD 글 (매 프레임 새 문자열을 만들지 않게 미리)
    readonly string[] xText = { null, "x1", "x2", "x3", "x4", "x5", "x6", "x7", "x8", "x9", "x10" };
    readonly string[] plusText = { null, "+1", "+2" };

    public int FrenzyStacks => frenzy > 0 && Time.time < frenzyUntil ? frenzy : 0;

    // ================================================================= 판 흐름 훅
    public override void OnSeed(int path, bool replay) { }      // 효과는 모두 Paths.Has 로 물음 (광란 탄창은 Tick 에서 한 번)
    public override void OnSingle(int path, bool replay) { }
    public override void OnResonance(int path, int tier, bool replay) { }
    public override void OnComplete(int path, bool replay) { }
    public override void OnMastery(int path, bool replay) { }
    public override void OnCross(int cross, bool replay) { }

    public override void End()
    {
        foreach (GameObject g in decoys) if (g != null) Object.Destroy(g);
        decoys.Clear();
        trails.Clear();
        PlayerController p = Hostile.Player;
        if (magAdded && p != null) { p.MaxBullet = Mathf.Max(1, p.MaxBullet - FrenzyMagAdd); p.NowBullet = Mathf.Min(p.NowBullet, p.MaxBullet); }
        magAdded = false;
        frenzy = 0;
    }

    // ================================================================= 전투 훅
    public override float Outgoing(GameObject target, float dmg)
    {
        // 출혈 대가: 평타 · 돌진처럼 직접 맞히는 피해만 (출혈 · 지속 피해 · 고유 스킬 · 갈래 효과 피해는 그대로)
        if (Paths.Has(Bleed) && !SignatureSkills.Dealing && !SignatureSkills.DotTick) return BleedDirectMul;
        return 1f;
    }

    public override void Hit(GameObject target, float dmg, bool killed, bool proc)
    {
        if (target == null) return;
        if (Paths.Has(Bleed))
        {
            if (proc && !killed && dmg > 0f) BleedOn(target, dmg, 1);
            // 공명 II: 출혈로 쓰러진 적이 둘레 둘에게 2중첩
            if (killed && SignatureSkills.DotTick && Paths.Res(Bleed) >= 2) Spread(target);
        }
        if (killed && dashKilled == false)
        {
            CharacterKit k = CharacterKit.Instance;
            if (k != null && k.Dashing) dashKilled = true;
        }
    }

    public override float Taken(float dmg) => Paths.Has(Frenzy) ? dmg * FrenzyTakenMul : dmg;

    public override void Hurt(float taken)
    {
        // 광란: 맞으면 중첩을 잃음 (극의면 그대로)
        if (taken > 0f && Paths.Has(Frenzy) && !Paths.Mastered(Frenzy)) frenzy = 0;
    }

    public override void Killed(Vector3 pos)
    {
        if (!Paths.Has(Frenzy)) return;
        frenzy = Mathf.Min(FrenzyMax, FrenzyStacks + 1);
        frenzyUntil = Time.time + FrenzyStackLife;
        // 공명 II: 10처치마다 빈 주머니의 비 (고유 스킬이 없으면 아무것도 안 함)
        if (Paths.Res(Frenzy) >= 2 && ++frenzyKills >= FrenzyRainKills)
        {
            frenzyKills = 0;
            if (SignatureSkills.Instance != null) SignatureSkills.Instance.RoguePathRain();
        }
    }

    // 질주 암살: 게이지로 돌진할 때마다 30% 를 돌려받음 (= 돌진 게이지 −30%) · 그림자의 모아 둔 돌진도 같음
    public override void Ult()
    {
        if (!Paths.Has(Dash)) return;
        SkillGauge g = Cache<SkillGauge>.Get;
        if (g != null) g.AddSkillPoint(g.MaxSkillPoint * DashGaugeBack);
    }

    public override float RateMul()
    {
        float m = 1f;
        if (Paths.Has(Dash)) m *= DashRate;
        if (Paths.Has(Frenzy)) m *= FrenzyRate * (1f + FrenzyStackRate * FrenzyStacks);
        return m;
    }

    public override void Tick()
    {
        // 광란 탄창 +4: 플레이어 시작 값(CharacterKit.Start 의 탄창 6)이 정해진 뒤 한 번 (이어하기도 같음)
        if (!magAdded && Paths.Has(Frenzy))
        {
            PlayerController p = Hostile.Player;
            if (p != null && !p.IsDying)
            {
                magAdded = true;
                p.MaxBullet += FrenzyMagAdd;
                p.NowBullet = Mathf.Min(p.MaxBullet, p.NowBullet + FrenzyMagAdd);
            }
        }
        if (frenzy > 0 && Time.time >= frenzyUntil) frenzy = 0;
    }

    public override string Badge(int path)
    {
        if (path == Frenzy) { int n = FrenzyStacks; return n > 0 ? xText[Mathf.Clamp(n, 0, 10)] : null; }
        if (path == Shadow)
        {
            CharacterKit k = CharacterKit.Instance;
            int n = k != null ? k.RpStored : 0;
            return n > 0 ? plusText[Mathf.Clamp(n, 0, 2)] : null;
        }
        return null;
    }

    // ================================================================= 출혈 중첩 (RogueBleed)
    int BleedCapFor(GameObject g) => Paths.Mastered(Bleed) && Paths.IsBoss(g) ? BleedBossCap : BleedCap;

    // 출혈 세기 배율: 갈래 배율 · 극의 · 갈고리 표창(갈래 카드) 단계마다 +15%
    float BleedMul()
    {
        float m = Paths.Power(Bleed) * (Paths.Mastered(Bleed) ? BleedMasteryMul : 1f);
        CharacterKit k = CharacterKit.Instance;
        if (k != null) m *= 1f + 0.15f * k.card[1];
        return m;
    }

    public void BleedOn(GameObject target, float hitDmg, int add)
    {
        if (target == null || !Paths.Has(Bleed)) return;
        RogueBleed.Stack(target, hitDmg, add, BleedCapFor(target), BleedMul());
    }

    void Spread(GameObject dead)
    {
        if (!dead.TryGetComponent(out RogueBleed from) || !from.Live) return;
        if (Time.time >= spreadWindow) { spreadWindow = Time.time + 0.25f; spreadCount = 0; }
        if (++spreadCount > 12) return;
        Vector3 at = dead.transform.position;
        List<Transform> near = Paths.Nearest(at, 4.5f, 2, dead);
        foreach (Transform t in near)
        {
            if (t == null) continue;
            RogueBleed.Infect(t.gameObject, from, 2, BleedCapFor(t.gameObject), BleedMul());
            Fx.Beam(at, t.position, 0.2f, new Color(0.9f, 0.15f, 0.25f, 0.8f), 0.3f, 12);
            Fx.Spawn("fx_bleed", t.position, 1.4f, Color.white, 16f);
        }
    }

    // 공명 I: 5중첩(보스는 극의면 10중첩) 적을 돌진으로 지나가면 남은 출혈이 한꺼번에
    void BurstCut(HashSet<Collider2D> cut)
    {
        bool sound = false;
        foreach (Collider2D c in cut)
        {
            if (c == null) continue;
            GameObject g = c.gameObject;
            if (!g.TryGetComponent(out RogueBleed rb) || rb.Stacks < BleedCap) continue;
            float burst = 0f;
            if (g.TryGetComponent(out global::Bleed b))
            {
                burst = b.dps * Mathf.Max(0f, b.until - Time.time);       // 남은 출혈 그대로 (세기에 이미 갈래 배율이 들어 있음)
                Object.Destroy(b);
            }
            rb.Clear();
            if (burst <= 0f) continue;
            Vector3 p = g.transform.position;
            Paths.Deal(g, burst, Vector3.zero, 0f, "출혈 터짐");
            Fx.Spawn("fx_bleed", p, 3f, Color.white, 16f);
            Fx.Spawn("fx_shock", p, 3.6f, new Color(0.9f, 0.15f, 0.25f, 0.85f), 22f);
            sound = true;
        }
        if (sound) Hostile.Play("crack", 0.5f, 0.9f);
    }

    // ================================================================= 돌진 (SignatureSkills.Rogue 의 DashStart · DashEnd 가 부름)
    public void DashStart(Vector3 from)
    {
        try
        {
            CharacterKit k = CharacterKit.Instance;
            bool chained = k != null && k.RpChaining;
            if (!chained) chain = 0;
            dashKilled = false;
            if (k != null) k.RpDashBegan();                 // 그림자: 모아 둔 돌진을 썼으면 하나 줄임
            if (Paths.Has(Shadow)) SpawnDecoy(from);
            // 교차 「폭풍 질주」: 돌진이 광란 5중첩을 바로 채움 (돌진 시간만큼 더 오래)
            if (Paths.CrossOn(CrossStormDash))
            {
                frenzy = Mathf.Max(FrenzyStacks, FrenzyStormStacks);
                frenzyUntil = Mathf.Max(frenzyUntil, Time.time + FrenzyStackLife + 0.35f);
            }
        }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    public void DashEnd(Vector3 from, Vector3 to, HashSet<Collider2D> cut)
    {
        try
        {
            PlayerController p = Hostile.Player;
            if (p == null || p.IsDying) return;            // 쓰러진 뒤에는 아무것도 하지 않음
            if (cut != null && Paths.Has(Bleed) && Paths.Res(Bleed) >= 1) BurstCut(cut);
            if (!Paths.Has(Dash)) return;
            if (dashKilled && Paths.Res(Dash) >= 1)
            {
                SkillGauge g = Cache<SkillGauge>.Get;
                if (g != null) g.AddSkillPoint(g.MaxSkillPoint * DashKillBack);
                Fx.Spawn("fx_sparkle", to, 1.8f, new Color(1f, 0.85f, 0.4f), 22f);
            }
            if (Paths.Res(Dash) >= 2) CrossTrails(from, to);
            // 극의: 돌진 중 처치하면 바로 다시 (모두 3연속)
            if (dashKilled && Paths.Mastered(Dash) && chain < DashChainMax && Time.timeScale > 0f)
            {
                CharacterKit k = CharacterKit.Instance;
                if (k != null) { chain++; k.RpChainDash(); }
            }
        }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    // 공명 II: 새 돌진 길이 4초 안의 지난 길과 X자로 엇갈리면 그 자리가 터짐
    void CrossTrails(Vector3 a, Vector3 b)
    {
        if ((b - a).sqrMagnitude < 1f) return;
        float now = Time.time;
        trails.RemoveAll(t => t.until <= now);
        float dmg = Paths.Atk * 3f * Paths.Power(Dash);
        foreach (Trail t in trails)
        {
            if (!Intersect(a, b, t.a, t.b, out Vector3 at)) continue;
            Paths.Circle(at, 3.5f, dmg, 1.5f, "돌진 교차");
            using (Fx.Tier(3)) Fx.Spawn("fx_explosion", at, 7f, new Color(1f, 0.85f, 0.45f), 16f);
            ShockRing.Spawn(at, 0.3f, 4.5f, 0.4f, new Color(1f, 0.8f, 0.35f, 0.9f), 0.3f);
            Hostile.Play("boom", 0.6f, 1.2f);
        }
        while (trails.Count >= 6) trails.RemoveAt(0);
        trails.Add(new Trail { a = a, b = b, until = now + 4f });
    }

    static bool Intersect(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2, out Vector3 at)
    {
        at = Vector3.zero;
        Vector2 r = p2 - p1, s = q2 - q1;
        float den = r.x * s.y - r.y * s.x;
        if (Mathf.Abs(den) < 0.0001f) return false;          // 나란함
        Vector2 qp = q1 - p1;
        float t = (qp.x * s.y - qp.y * s.x) / den;
        float u = (qp.x * r.y - qp.y * r.x) / den;
        if (t < 0.05f || t > 0.95f || u < 0.05f || u > 0.95f) return false;     // 끝이 맞닿은 것은 X 가 아님
        at = p1 + (Vector3)(r * t);
        return true;
    }

    // 그림자: 돌진을 시작한 자리의 미끼 분신 (적이 쫓아옴 · 개수 상한 3)
    void SpawnDecoy(Vector3 at)
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        SpriteRenderer body = p.GetComponent<SpriteRenderer>();
        if (body == null || body.sprite == null) return;
        Paths.Trim(decoys, MaxDecoys);
        GameObject g = SpecialAbilities.MakeSprite("RogueDecoy", body.sprite, at, 1f, new Color(0.42f, 0.3f, 0.78f, 0.78f), "Character", -1);
        g.transform.localScale = p.transform.lossyScale;
        if (g.TryGetComponent(out SpriteRenderer sr)) sr.flipX = body.flipX;
        RogueDecoy d = g.AddComponent<RogueDecoy>();
        d.Begin(DecoyLife, Paths.Mastered(Shadow) || Paths.CrossOn(CrossShadowBleed), Paths.Res(Shadow) >= 1);
        decoys.Add(g);
    }
}

// 출혈 갈래: 적 하나의 출혈 중첩 (실제 피해는 기존 Bleed 컴포넌트가 줌 — 여기선 중첩 수와 세기만 셈)
//   세기 = 이 적이 받는 직접 피해 흐름(초당, 최근 평균) × 0.55 × 중첩/5 × 배율 → 공격 속도 · 멀티샷이 달라도 보스 앞 비율이 같음
//   다른 출혈(돌진 · 갈고리 표창)이 더 세면 그쪽을 그대로 둠 (Bleed.Apply 는 센 쪽을 남김)
//   상태는 until 하나 (3초 안에 다시 안 맞으면 중첩이 0) · Update 없음
public class RogueBleed : MonoBehaviour
{
    public const float Life = 3f;
    int stacks;
    float until, dmgEma, gapEma = 0.6f, lastHit = -99f, lastSet = -1f;

    public bool Live => stacks > 0 && Time.time < until;
    public int Stacks => Live ? stacks : 0;

    public static void Stack(GameObject g, float hitDmg, int add, int cap, float mul)
    {
        if (g == null || hitDmg <= 0f || add <= 0) return;
        if (!g.TryGetComponent(out RogueBleed rb)) rb = g.AddComponent<RogueBleed>();
        rb.Add(hitDmg, -1f, add, cap, mul);
    }

    // 전염: 퍼뜨린 적의 세기(피해 흐름)를 이어 받음
    public static void Infect(GameObject g, RogueBleed from, int add, int cap, float mul)
    {
        if (g == null || from == null) return;
        if (!g.TryGetComponent(out RogueBleed rb)) rb = g.AddComponent<RogueBleed>();
        rb.Add(from.dmgEma, from.gapEma, add, cap, mul);
    }

    public void Clear() { stacks = 0; until = 0f; lastSet = -1f; }

    void Add(float hitDmg, float gap, int add, int cap, float mul)
    {
        float now = Time.time;
        if (!Live)
        {
            stacks = 0;
            dmgEma = hitDmg;
            gapEma = gap > 0f ? gap : 0.6f;
        }
        else if (gap > 0f)
        {
            dmgEma = Mathf.Max(dmgEma, hitDmg);
            gapEma = Mathf.Min(gapEma, gap);
        }
        else
        {
            gapEma = Mathf.Lerp(gapEma, Mathf.Clamp(now - lastHit, 0.05f, 2f), 0.3f);
            dmgEma = Mathf.Lerp(dmgEma, hitDmg, 0.3f);
        }
        lastHit = now;
        stacks = Mathf.Clamp(stacks + add, 0, Mathf.Max(1, cap));
        until = now + Life;
        Push(mul);
    }

    void Push(float mul)
    {
        float flow = dmgEma / Mathf.Max(0.05f, gapEma);                                  // 초당 직접 피해 (최근)
        float k = Mathf.Min(stacks, RoguePaths.BleedCap) / (float)RoguePaths.BleedCap
                + Mathf.Max(0, stacks - RoguePaths.BleedCap) * 0.06f;                    // 6중첩부터 하나에 +6% (극의 · 보스)
        float dps = flow * RoguePaths.BleedShare * k * mul;
        if (dps <= 0f) return;
        Bleed b = GetComponent<Bleed>();
        bool mine = b == null || Mathf.Abs(b.dps - lastSet) <= 0.001f + lastSet * 0.001f;     // 지금 출혈이 내가 정한 세기 그대로면 낮출 수도 있음
        using (DamageSource.As("출혈 중첩")) Bleed.Apply(gameObject, dps, Life);
        if (b == null) b = GetComponent<Bleed>();
        if (b == null) return;
        if (mine) b.dps = dps;
        lastSet = b.dps;
    }
}

// 그림자 갈래의 미끼 분신: 1.5초 동안 적이 쫓아옴(EnermyController.Decoy) · 사라질 때 연막(공명 I) · 극의 · 교차면 표창을 던짐
//   상태는 until 하나 · 플레이어가 쓰러지면 바로 사라짐 · 다른 미끼(거울 분신)가 있으면 빼앗지 않음
public class RogueDecoy : MonoBehaviour
{
    float until, nextThrow;
    bool throws, smoke, ended;

    public void Begin(float life, bool throws, bool smoke)
    {
        until = Time.time + life;
        this.throws = throws;
        this.smoke = smoke;
        nextThrow = Time.time + 0.15f;
        Transform cur = EnermyController.Decoy;
        if (cur == null || cur.GetComponent<RogueDecoy>() != null) EnermyController.Decoy = transform;
        Fx.Spawn("fx_stealth", transform.position, 2.2f, new Color(0.75f, 0.6f, 1f), 18f);
        ShockRing.Spawn(transform.position, 0.3f, 2.6f, 0.3f, new Color(0.6f, 0.45f, 0.95f, 0.85f), 0.2f);
    }

    void Update()
    {
        if (ended) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) { Destroy(gameObject); return; }       // 쓰러진 뒤 동작 금지
        if (Time.time >= until) { Finish(); return; }
        if (throws && Time.time >= nextThrow)
        {
            CharacterKit k = CharacterKit.Instance;
            bool thrown = k != null && k.RpDecoyThrow(transform.position);
            nextThrow = Time.time + (thrown ? 0.35f : 0.1f);
        }
    }

    void Finish()
    {
        ended = true;
        if (EnermyController.Decoy == transform) EnermyController.Decoy = null;
        if (smoke && SignatureSkills.Instance != null) SignatureSkills.Instance.RoguePathSmoke(transform.position);
        gameObject.AddComponent<FadeOut>().duration = 0.3f;     // FadeOut 이 끝에 지움
    }

    void OnDestroy()
    {
        if (EnermyController.Decoy == transform) EnermyController.Decoy = null;
    }
}
