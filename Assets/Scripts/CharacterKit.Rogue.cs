using UnityEngine;

// 도적 갈래 효과 중 평타 · 돌진에 붙는 부분 (2026-10-19 · Paths/Paths.Rogue.cs 가 갈래 데이터 · 나머지 효과)
//   CharacterKit.cs 의 도적 줄이 부르는 곳: UpdateUlt 도적 분기(RpUlt) · DashEnd(RogueDashDist) · Cut(RpCutBleed) · ShadowClone(RpCloneShot)
//   CharacterKit.Forms 의 RogueThrow 가 부르는 곳: 표창마다 RpShot · 던진 뒤 RpVolley
// 안전: 상태는 숫자 · until 만 (코루틴으로 켜고 끄지 않음) · 정적 상태 없음 · 쓰러진 뒤엔 아무것도 안 함
public partial class CharacterKit
{
    // ---------------- 그림자: 돌진 모아 두기 (게이지가 가득 찬 뒤에도 한 번 더 채워 둠 → 2번 · 극의 3번, RpUlt)
    int rpStored;               // 모아 둔 돌진 수 (게이지 밖)
    bool rpArmed;               // 이번 프레임 「한 번 더 돌진」(recastUntil)을 모아 둔 돌진 때문에 열어 두었나
    bool rpChaining;            // 질주 암살 극의: 지금 이어서 다시 돌진을 시작하는 중 (같은 호출 안에서만 · try/finally)
    // 한 번에 던진 표창 묶음 (회귀 칼날: 받으면 탄창 한 발은 묶음마다 한 번)
    RogueVolley rpVolley;
    int rpVolleyFrame = -1;
    int rpVolleyCount;          // 1.4.2~ 지금까지 만든 묶음 수 (회귀 칼날 탄창 환급은 두 묶음에 한 번 — 환급이 장전을 지워 연사가 ×1.3~1.75 되던 문제)

    public int RpStored => Paths.Has(RoguePaths.Shadow) ? rpStored : 0;
    public bool RpChaining => rpChaining;

    // ---------------- 1.4.2~ 게이지 돌진 묶음 (게이지 · 모아 둔 돌진으로 시작 → 숙련 4단계 재돌진 · 질주 암살 극의 연속 돌진이 이어 씀)
    //   도적 쪽 게이지 반환(질주 암살 30% · 공명 I 25% · 숙련 5단계 4%/명)은 묶음 하나에 모두 합쳐 40% 까지
    //   (넷이 상한 없이 더해져 돌진 뒤 게이지가 60~125% 남아 무적 돌진이 입력 한계마다 나가던 문제) · 영혼 트리 잔불은 모든 영웅 공통이라 상한 밖
    //   상태는 숫자 둘뿐 · 묶음을 새로 열 때(BleedDash) 늘 되돌림 → 끊겨도 남는 상태 없음
    public const float RogueBackCap = 0.4f;
    float rogueBackLeft = RogueBackCap;     // 이번 묶음에서 아직 돌려받을 수 있는 몫 (게이지 최대치 비율)
    bool rpGaugeDash = true;                // 지금(또는 마지막) 돌진이 묶음을 새로 연 돌진인가 — RoguePaths.DashStart 가 물음
    public bool RpGaugeDash => rpGaugeDash;

    // 돌진 게이지 반환 (frac = 최대치 비율) — 이번 묶음의 남은 몫까지만
    public void RogueGaugeBack(float frac)
    {
        if (frac <= 0f || rogueBackLeft <= 0f) return;
        if (gaugeRef == null) gaugeRef = FindFirstObjectByType<SkillGauge>();
        if (gaugeRef == null) return;
        float f = Mathf.Min(frac, rogueBackLeft);
        rogueBackLeft -= f;
        gaugeRef.AddSkillPoint(gaugeRef.MaxSkillPoint * f);
    }

    // 질주 암살: 돌진 거리 ×1.5 (조준 미리보기 · 실제 돌진이 같은 값)
    float RogueDashDist => Paths.Has(RoguePaths.Dash) ? RoguePaths.DashDistMul : 1f;

    // UpdateUlt 도적 분기 맨 앞에서 매 프레임
    //   게이지가 가득 찬 동안에도 같은 빠르기로 「다음 돌진」을 하나 더 채워 모아 둠 (게이지는 가득 찬 그대로 — 넘치는 기세 · 사냥의 절정이 그대로 동작)
    //   빠르기 = 게이지가 차지 않았을 때 잰 초당 충전량 (처치 가속 · 영혼 트리 포함, 반환처럼 한꺼번에 늘어난 것은 뺌)
    //   쓸 때: 게이지가 가득이면 게이지부터, 아니면 모아 둔 돌진 (그림자 숙련 4단계의 「한 번 더 돌진」 창을 빌려 씀)
    float rpBank, rpRate, rpLastPoint = -1f, rpAcc, rpAccT;
    void RpUlt(SkillGauge gauge)
    {
        rpArmed = false;
        if (!Paths.Has(RoguePaths.Shadow) || gauge == null) { rpStored = 0; rpBank = 0f; return; }
        if (player == null || player.IsDying) return;
        int cap = Paths.Mastered(RoguePaths.Shadow) ? 2 : 1;        // 게이지 밖에 모아 둘 수 (게이지와 합쳐 2번 · 극의 3번)
        float pt = gauge.SkillPoint, dt = Time.deltaTime;
        bool full = gauge.IsFull();
        if (!full)
        {
            // 1.4.1~ 빠르기를 2초 동안 모인 양으로 잼 — 도적 게이지는 처치 · 명중으로 덩어리째 차서, 예전처럼 한 프레임으로 재면
            //   값이 너무 커 늘 버려졌고(rpRate 0) 돌진이 영영 모이지 않았음. 반환처럼 한꺼번에 크게 는 것(최대치의 절반 넘게)만 뺌
            if (rpLastPoint >= 0f && dt > 0f)
            {
                float g = pt - rpLastPoint;
                if (g > 0f && g <= gauge.MaxSkillPoint * 0.5f) rpAcc += g;
                rpAccT += dt;
                if (rpAccT >= 2f)
                {
                    float r = rpAcc / rpAccT;
                    rpRate = rpRate <= 0f ? r : Mathf.Lerp(rpRate, r, 0.5f);
                    rpAcc = 0f; rpAccT = 0f;
                }
            }
            rpBank = 0f;
        }
        else if (rpStored < cap && Time.timeScale > 0f && rpRate > 0f)
        {
            rpBank += rpRate * dt;
            if (rpBank >= gauge.MaxSkillPoint)
            {
                rpBank = 0f;
                rpStored++;
                Fx.Spawn("fx_sparkle", transform.position, 1.6f, new Color(0.75f, 0.6f, 1f), 22f);
                Play("chime", 0.35f, 1.6f);
            }
        }
        rpLastPoint = pt;
        // 게이지가 비었고 모아 둔 돌진이 있으면 「한 번 더 돌진」 창을 열어 둠 (그림자 숙련 4단계가 연 창은 줄이지 않음)
        if (rpStored > 0 && !full && recastUntil < Time.time + 0.1f)
        {
            recastUntil = Time.time + 0.1f;
            rpArmed = true;
        }
    }

    // RoguePaths.DashStart (BleedDash 첫 프레임) — 모아 둔 돌진을 썼으면 하나 줄이고 게이지 돌진과 같게 (덧붙는 궁극기 · 필살기 사용 알림)
    public void RpDashBegan()
    {
        if (rpChaining || !rpArmed || rpStored <= 0) return;
        rpArmed = false;
        rpStored--;
        KitUltAdd(0.25f, Mouse);
        lastUlt = Time.time;
        rpStoredUlt = true;                 // 게이지를 쓰지 않은 돌진: 질주 암살의 30% 환급은 받지 않음 (RoguePaths.Ult)
        try { player.RaiseUltUsed(); }
        finally { rpStoredUlt = false; }
    }

    // 지금 필살기 사용 알림이 모아 둔 돌진(게이지를 쓰지 않음)에서 왔나 — RaiseUltUsed 안에서만 true
    bool rpStoredUlt;
    public bool RpStoredUlt => rpStoredUlt;

    // 질주 암살 극의: 돌진 중 처치하면 커서 쪽으로 바로 다시 돌진
    public void RpChainDash()
    {
        if (player == null || player.IsDying || Time.timeScale == 0f) return;
        Vector2 d = ((Vector2)(Mouse - transform.position)).normalized;
        rpChaining = true;
        try { StartCoroutine(BleedDash(d, false)); }        // 1.4.2~ 같은 묶음 (반환 상한 · 연속 횟수를 이어 씀)
        finally { rpChaining = false; }
    }

    // 돌진이 벤 적의 출혈 (Cut): 질주 암살이면 출혈 대신 직접 피해, 그림자면 출혈 −30%
    //   bossMul: 1.4.2~ 재돌진 · 연속 돌진이 보스를 벨 때 0.6 (Cut) — 질주 암살 직접 피해에만. 출혈은 그대로
    //   (출혈은 센 쪽만 남아 게이지 돌진의 출혈을 이어 받을 뿐이고, 줄이면 약한 출혈이 되어 시간도 못 이어 이중으로 깎임)
    void RpCutBleed(GameObject g, float dps, Vector2 dir, float bossMul = 1f)
    {
        if (g == null) return;
        if (Paths.Has(RoguePaths.Dash))
        {
            Paths.Deal(g, Damage * RoguePaths.DashCutMul * UltMul * Paths.Power(RoguePaths.Dash) * bossMul
                * (Paths.Has(RoguePaths.Bleed) ? RoguePaths.BleedDirectMul : 1f), dir, 0.5f, "질주 암살");     // 1.4.2~ 출혈 대가(직접 피해 −35%)가 이 돌진에도 (Paths.Deal 이라 빠져 있었음)
            return;
        }
        if (Paths.Has(RoguePaths.Shadow)) dps *= RoguePaths.ShadowDashBleedMul;
        Bleed.Apply(g, dps, 4f);
    }

    // 분신 표창 (그림자 분신 카드 · 미끼 분신): 교차 「그림자 출혈」이면 출혈 1중첩 더 (명중 훅의 1중첩과 합쳐 2중첩)
    void RpCloneShot(Bullet b)
    {
        if (b == null || !Paths.CrossOn(RoguePaths.CrossShadowBleed)) return;
        b.onHitEnemy += (s, c) =>
        {
            RoguePaths rp = RoguePaths.Cur;
            if (rp != null && c != null && s != null) rp.BleedOn(c.gameObject, s.damage, 1);
        };
    }

    // 미끼 분신이 가까운 적에게 표창 하나 (던졌으면 true)
    public bool RpDecoyThrow(Vector3 at)
    {
        if (player == null || player.IsDying) return false;
        Transform e = Specials.NearestEnemy(at, 14f);
        if (e == null) return false;
        Vector2 d = ((Vector2)(e.position - at)).normalized;
        RpCloneShot(ThrowOne(at, d, Damage * 0.7f * Paths.Power(RoguePaths.Shadow), 0.75f));
        Play("whoosh", 0.2f, 2f);
        return true;
    }

    // RogueThrow: 던진 표창 하나마다 (center = 가운데 표창 · 회전 표창이 쓰는 그 표창)
    //   echo: 1.4.2~ 잔상 표창 (0.15초 뒤라 새 묶음이 생겨 환급이 두 번 되던 것 → 원래 묶음 echoVolley 를 같이 씀 · 회귀 극의 두 번 왕복도 받지 않고 한 번)
    void RpShot(Bullet b, bool center, bool echo = false, RogueVolley echoVolley = null)
    {
        if (b == null || !Paths.Active || Paths.A < 0) return;
        // 광란 대가: 표창 피해 −20%
        if (Paths.Has(RoguePaths.Frenzy)) b.damage *= RoguePaths.FrenzyShotMul;
        // 그림자 공명 II: 연막 안에서 던지면 치명타 ×2
        if (Paths.Has(RoguePaths.Shadow) && Paths.Res(RoguePaths.Shadow) >= 2)
        {
            SignatureSkills s = SignatureSkills.Instance;
            if (s != null && s.RogueInSmoke(transform.position))
            {
                b.damage *= 2f;
                b.isCrit = true;
                b.critRolled = true;        // 영혼 트리 치명타를 한 번 더 굴리지 않음
            }
        }
        // 회귀 칼날: 8칸에서 꺾여 돌아옴
        if (Paths.Has(RoguePaths.Boomerang))
        {
            RogueVolley vol;
            if (echo) vol = echoVolley ?? new RogueVolley { refunded = true };      // 원래 묶음을 못 찾으면 환급 없음
            else
            {
                if (rpVolleyFrame != Time.frameCount || rpVolley == null)
                {
                    // 1.4.2~ 탄창 환급은 두 묶음에 한 번: 홀수 번째 묶음은 처음부터 환급한 것으로
                    rpVolleyCount++;
                    rpVolley = new RogueVolley { refunded = (rpVolleyCount & 1) == 1 };
                    rpVolleyFrame = Time.frameCount;
                }
                vol = rpVolley;
            }
            SignatureSkills s = SignatureSkills.Instance;
            float back = b.damage * RoguePaths.ReturnDmgMul * Paths.Power(RoguePaths.Boomerang);
            bool twin = false;
            if (center && s != null)
            {
                back += s.RogueSpinBack;            // 회전 표창 카드가 있으면 그 피해도 (가운데 표창 — 회전 표창은 따로 붙지 않음)
                twin = s.RogueEvolved("re.return");
            }
            b.gameObject.AddComponent<ReturningShuriken>().ThrowPath(b, transform, b.transform.position, RoguePaths.ReturnDist, back, twin, vol,
                Paths.Res(RoguePaths.Boomerang) >= 1, Paths.Res(RoguePaths.Boomerang) >= 2, Paths.Mastered(RoguePaths.Boomerang) && !echo ? 2 : 1);
        }
    }

    // RogueThrow 뒤: 광란 공명 I — 10중첩이면 같은 수의 표창을 조금 비껴 한 번 더
    void RpVolley(Vector3 start, Vector2 dir, float dmg, int shots, float step)
    {
        if (!Paths.Has(RoguePaths.Frenzy) || Paths.Res(RoguePaths.Frenzy) < 1) return;
        RoguePaths rp = RoguePaths.Cur;
        if (rp == null || rp.FrenzyStacks < RoguePaths.FrenzyMax) return;
        Vector2 d2 = Quaternion.Euler(0f, 0f, step * 0.5f) * dir;
        foreach (Vector2 d in Spread(d2, shots, step))
            RpShot(ThrowOne(start, d, dmg * RoguePaths.FrenzyTwinMul * Paths.Power(RoguePaths.Frenzy), 0.85f), false);
    }
}

// 한 번에 던진 표창 묶음 (회귀 칼날: 돌아온 표창을 받으면 탄창 한 발은 묶음마다 한 번 · 1.4.2~ 홀수 번째 묶음은 refunded = true 로 시작해 두 묶음에 한 번)
public class RogueVolley
{
    public bool refunded;
}
