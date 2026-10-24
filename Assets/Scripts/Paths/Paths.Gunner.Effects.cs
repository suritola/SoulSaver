using System.Collections.Generic;
using UnityEngine;

// 거너 갈래 효과 본체 (Paths.Gunner.cs 의 나머지): 사격 · 장전 · 열 · 돌진 · 튕김 · 화상 · 포탑 관리
// 안전 (기획서 6-3): 시간 상태는 끝나는 시각 · 무적은 Paths.Invuln 만 · 만드는 것은 개수 상한 · 쓰러진 뒤 동작 없음 · 자기 복제 Instantiate 없음
public sealed partial class GunnerPaths
{
    // ================================================================= 사격 (PlayerController.Shoot)
    // 연사 간격 배율: 난사 ×1/1.35 · 포탑 ×1.25 (공격 속도 −20%)
    public float IntervalMul
    {
        get
        {
            float m = 1f;
            if (Paths.Has(Ricochet)) m /= RicochetRate;
            if (Paths.Has(Turret)) m *= TurretSlow;
            return m;
        }
    }

    // 과열 사격: 탄이 줄지 않음 (장전 대신 열 게이지 · 장전 키도 쓰지 않음)
    //   튜토리얼 판은 예외 (재장전 단계가 [재장전] 키를 기다리므로 막히지 않게 탄창 · 장전을 그대로 둠)
    public bool NoAmmo => Paths.Has(Overheat) && !TutorialRun.Active;

    public Vector2 Spread(Vector2 dir) => Paths.Has(Ricochet) ? (Vector2)(Quaternion.Euler(0f, 0f, Random.Range(-RicochetSpread, RicochetSpread)) * dir) : dir;

    // 이번 한 발 (BeginShot 이 정하고 FireOverride · Dress · EndShot 이 씀)
    bool curSnipe, curBurn, curSlug;
    float curExtraMul = 1f;

    // 진화 덤 사격(쌍발 · 산탄 · 화염 총구) 피해 배율 — 산탄 갈래 펠릿을 쏜 발이면 펠릿 비율 (덤이 본체보다 세던 것)
    public float ExtraShotMul => curExtraMul;

    // 과열 사격: 과열까지 남은 발 수 (이 발 포함 · 1 이면 이 발로 과열) — 탄창 저주 · 슬러그가 「마지막 발」로 봄
    public int HeatShotsLeft => Mathf.Max(1, Mathf.CeilToInt((1f - heat) / HeatPerShot - 0.001f));

    // 이번 한 발의 피해 배율 (대가 · 저격탄 · 아슬아슬한 열) · 저격탄 · 화상 · 슬러그 판정
    public float BeginShot(bool moving, bool lastShell)
    {
        float mul = 1f;
        curSnipe = curBurn = curSlug = false;
        curExtraMul = 1f;
        if (Paths.Has(Snipe))
        {
            if (moving) mul *= MovingMul;
            if (aim >= 1f)
            {
                curSnipe = true;
                aim = 0f;
                mul *= 1f + (SnipeMul - 1f) * Paths.Power(Snipe);
            }
        }
        if (Paths.Has(Ricochet)) mul *= RicochetCost;
        if (Paths.Has(Overheat))
        {
            mul *= OverheatCost;
            curBurn = heat >= 0.5f;
            if (Paths.Res(Overheat) >= 2 && heat >= 0.9f) mul *= 1f + (HotMul - 1f) * Paths.Power(Overheat);
        }
        if (Paths.Has(Shotgun))
        {
            curSlug = !curSnipe && lastShell && Paths.Res(Shotgun) >= 2;
            if (Time.time < boostUntil)         // 공명 I: 돌진에 부딪힌 뒤 다음 산탄 +40%
            {
                boostUntil = 0f;
                mul *= 1f + (RamBoost - 1f) * Paths.Power(Shotgun);
            }
        }
        return mul;
    }

    // 산탄 · 슬러그: 평타 탄을 대신 만들면 true (저격탄은 산탄 대신 저격탄 한 발 → false)
    public bool FireOverride(PlayerController pc, Vector3 start, Vector2 dir, float dmg, bool cursed)
    {
        if (!Paths.Has(Shotgun) || curSnipe || pc == null) return false;
        SpecialAbilities sp = pc.special;
        if (curSlug)
        {
            // 공명 II: 탄창 마지막 발 = 일직선으로 꿰뚫는 슬러그 (공격력 300%)
            Bullet s = pc.CreateBullet(start, dir, Paths.Atk * SlugRate * Paths.Power(Shotgun) * (cursed ? 3f : 1f), 999, 0f, false);
            if (s == null) return true;
            sp?.CurseBullet(s, cursed);
            sp?.ApplyGunCards(s, false);
            s.pene = 999;
            s.speed *= 1.4f;
            s.lifetime = SlugRange / Mathf.Max(1f, s.speed);
            s.transform.localScale *= 1.5f;
            if (s.TryGetComponent(out SpriteRenderer ssr)) ssr.color = SlugColor;
            Fx.Bolt(start, start + (Vector3)(dir * 3f), 0.6f, SlugColor, 0.12f);
            Hostile.Play("boom", 0.35f, 1.6f);
            if (curBurn) Tap(s, false, true);
            return true;
        }
        curExtraMul = PelletRate;
        int n = Pellets + Mathf.Max(0, pc.multiShot - 1) * 2;
        float each = dmg * PelletRate * Paths.Power(Shotgun);
        for (int i = 0; i < n; i++)
        {
            float a = -PelletArc * 0.5f + PelletArc * (n > 1 ? i / (float)(n - 1) : 0.5f) + Random.Range(-2f, 2f);
            Vector2 d = Quaternion.Euler(0f, 0f, a) * dir;
            Bullet b = pc.CreateBullet(start, d, each, pc.pene, 0f, false, 1f / n + 0.1f);
            if (b == null) continue;
            // 1.4.1~ 저주 폭발 · 공격력 기준 명중 효과(전기탄 · 용광로 · 추적 구슬)는 첫 펠릿만 (펠릿마다 붙어 공격력 150% 폭발이 6번 터지던 것)
            //   나머지 펠릿은 저주탄 색만 (피해 ×3 은 each 에 이미 들어 있음)
            if (i == 0) sp?.CurseBullet(b, cursed);
            else if (cursed && b.TryGetComponent(out SpriteRenderer csr)) csr.color = VisualRules.Kit(new Color(1f, 0.3f, 0.3f), new Color(0.39f, 0.3f, 1f));
            sp?.ApplyGunCards(b, false, i == 0, i > 0);
            b.speed *= Random.Range(1.05f, 1.2f);
            b.lifetime = PelletRange / Mathf.Max(1f, b.speed);       // 사거리 6칸 (가까울수록 많이 맞음)
            b.transform.localScale *= 0.8f;
            if (curBurn) Tap(b, false, true);
            if (Paths.Has(Ricochet)) AttachRicochet(b, 0);
        }
        Fx.Spawn("fx_shock", start + (Vector3)(dir * 0.6f), 1.6f, new Color(1f, 0.75f, 0.45f, 0.8f), 30f);
        return true;
    }

    // 평타 탄 꾸밈 (ApplyGunCards 뒤): 저격탄 · 화상탄 · 튕김
    public void Dress(Bullet b)
    {
        if (b == null) return;
        if (curSnipe) MakeSniper(b, curBurn);
        else if (curBurn) Tap(b, false, true);
        if (Paths.Has(Ricochet)) AttachRicochet(b, 0);
    }

    // 쏜 뒤: 열 (화염 과열)
    public void EndShot(PlayerController pc)
    {
        if (!Paths.Has(Overheat) || pc == null) return;
        heat = Mathf.Min(1f, heat + HeatPerShot);
        lastShotAt = Time.time;
        if (heat >= 0.999f) pc.ForceReload();       // 과열 → 장전 코루틴이 사격 불가 시간을 맡음 (ReloadStarted 가 폭발)
    }

    // 일제 사격 총알 (관통 저격 극의: 모두 저격탄)
    public void UltShot(Bullet b)
    {
        if (b == null || !Paths.Mastered(Snipe)) return;
        b.damage *= 1f + (SnipeMul - 1f) * Paths.Power(Snipe);
        MakeSniper(b, false);
    }

    void MakeSniper(Bullet b, bool burn)
    {
        b.pene = 999;
        b.speed *= SnipeSpeed;
        b.transform.localScale *= 1.25f;
        if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = SnipeColor;
        Tap(b, true, burn || Paths.CrossOn(CrossApInc));
        Vector3 p = b.transform.position;
        Fx.Bolt(p, p + (Vector3)(b.Direction * 4f), 0.45f, SnipeColor, 0.12f);
        Fx.Spawn("fx_spark", p, 1.4f, SnipeColor, 26f);
    }

    // ================================================================= 장전 (PlayerController.Reload)
    //   과열 사격(NoAmmo)이면 장전 = 과열 → 카드 문구대로 2초 · 0.8초 (산탄 장전 · 포탑 곁 배율을 받지 않음)
    //   튜토리얼 판(NoAmmo 아님)은 보통 장전 그대로
    public float ReloadTime(float normal) => NoAmmo ? (Paths.Mastered(Overheat) ? OverheatTimeMastery : OverheatTime) : normal;

    public float ReloadRate
    {
        get
        {
            float r = 1f;
            if (NoAmmo) return r;
            if (Paths.Has(Shotgun)) r *= ShotgunReload;
            if (Paths.Has(Turret) && Paths.Res(Turret) >= 1 && NearTurret(4f)) r *= 2f;       // 공명 I: 포탑 곁에서 장전 2배
            return r;
        }
    }

    int extraDashes;
    bool boomAtDashEnd;

    // 장전 시작 (SignatureSkills.OnReloadStart · 과열이면 과열 시작)
    // 1.4.1~ pistol = 권총 장전 코루틴(PlayerController.Reload)에서 부름 — 과열(열 100% · 폭발)은 권총 장전 때만
    //   (특수 총기 장전도 같은 훅을 불러, 대가 없이 과열 폭발이 터지던 것)
    public void ReloadStarted(bool pistol)
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        bool dashed = false;
        if (Paths.Has(Shotgun)) dashed = StartDash(p, 0);
        if (Paths.Has(Overheat) && pistol)
        {
            heat = 1f;
            extraDashes = 0;
            // 화염 돌진 (교차): 폭발은 돌진 끝에서
            if (dashed && Paths.CrossOn(CrossFlameDash)) boomAtDashEnd = true;
            else OverheatBoom(p.transform.position, false);
            if (Paths.Mastered(Overheat)) boom2At = Time.time + 0.35f;      // 극의: 폭발 두 번
        }
    }

    // 장전 끝 (SignatureSkills.OnReloadEnd · 과열이면 과열이 풀림)
    // 1.4.1~ pistol = 권총 장전이 끝남 — 열은 권총 장전(과열)이 끝날 때만 0 (특수 총기 장전이 권총 열을 지우지 않게)
    public void ReloadEnded(bool pistol)
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        if (Paths.Has(Overheat) && pistol) { heat = 0f; lastShotAt = Time.time; }
        if (Paths.Has(Turret)) SpawnTurret(p.transform.position);
    }

    // ================================================================= 관통 저격: 조준 모으기
    float aim;

    void SnipeTick(PlayerController p)
    {
        float before = aim;
        bool still = p.MoveInput.sqrMagnitude < 0.01f && !dashing;
        if (still) aim = Mathf.Min(1f, aim + Time.deltaTime / AimTime);
        else aim = Mathf.Max(0f, aim - Time.deltaTime * 2.5f);
        if (before < 1f && aim >= 1f && Time.time >= readyFxAt)
        {
            readyFxAt = Time.time + 0.5f;
            ShockRing.Spawn(p.transform.position, 0.3f, 1.8f, 0.22f, new Color(SnipeColor.r, SnipeColor.g, SnipeColor.b, 0.8f), 0.1f);
            Hostile.Play("shimmer", 0.18f, 2f);
        }
    }
    float readyFxAt;

    // 저격탄 · 화상탄이 맞힌 뒤 (Hit 훅)
    void OnTapHit(GunHitTap info, GameObject target, bool weak)
    {
        if (info.snipe)
        {
            if (weak && Paths.Res(Snipe) >= 1)
            {
                Vector3 at = target.transform.position;
                Paths.Circle(at, 1.6f, Paths.Atk * WeakBoom * Paths.Power(Snipe), 0.5f, "저격탄");
                Fx.Spawn("fx_explosion", at, 3f, SnipeColor, 24f);
                Fx.Spawn("fx_sparkle", at, 2f, Color.white, 24f);
            }
            if (Paths.Res(Snipe) >= 2) aim = Mathf.Min(1f, aim + PierceAim);      // 꿰뚫은 적 하나마다 다음 조준 20%
            if (Paths.CrossOn(CrossApInc)) { AddBurn(target, 3, 3); return; }       // 철갑 소이탄: 지나간 줄 전체 화상 3중첩
        }
        if (info.burn) AddBurn(target, 1, Paths.Res(Overheat) >= 1 ? 3 : 1);
    }

    // ================================================================= 화염 과열: 열 · 화상 중첩 · 폭발
    float heat, lastShotAt, boom2At, pruneAt;

    void HeatTick(PlayerController p, float now)
    {
        if (p.IsReloading)
        {
            // 화염 돌진 (교차): 과열 중에도 장전 키로 돌진 (한 번 과열에 두 번까지)
            if (Paths.CrossOn(CrossFlameDash) && Paths.Has(Shotgun) && !dashing && extraDashes < 2 && !PlayerController.IsPaused && KeyBindings.Down(GameAction.Reload))
                if (StartDash(p, 0)) extraDashes++;
            return;
        }
        if (heat > 0f && now - lastShotAt > CoolDelay) heat = Mathf.Max(0f, heat - CoolRate * Time.deltaTime);
        // 열 50% 위: 총구에 불티 (0.25초에 한 번)
        if (heat >= 0.5f && now >= emberAt)
        {
            emberAt = now + 0.25f;
            Fx.Spawn("fx_spark", p.MuzzlePosition, 0.7f + heat * 0.6f, FireColor, 24f);
        }
    }
    float emberAt;

    void OverheatBoom(Vector3 at, bool second)
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        float dmg = Paths.Atk * BoomRate * Paths.Power(Overheat);
        int cap = Paths.Res(Overheat) >= 1 ? 3 : 1;
        Paths.Circle(at, BoomRadius, dmg, 2.5f, "과열 폭발", c => AddBurn(c.gameObject, 1, cap));
        Fx.Spawn("fx_explosion", at, BoomRadius * 2.2f, FireColor, 20f);
        Fx.Spawn("fx_firepillar", at + Vector3.up * 0.8f, 4f, Color.white, 22f);
        ShockRing.Spawn(at, 0.5f, BoomRadius + 0.5f, 0.35f, new Color(1f, 0.55f, 0.2f, 0.9f), 0.3f);
        Hostile.Play(second ? "boom" : "bigboom", 0.6f, second ? 1.2f : 1f);
        Hostile.Shake(second ? 0.1f : 0.18f);
        if (Paths.Res(Overheat) >= 2 && !second) FireFloor(at);
    }

    // 공명 II: 불 장판 4초 (상한 40 · 실제로는 과열마다 하나)
    readonly List<GameObject> floors = new List<GameObject>();

    void FireFloor(Vector3 at)
    {
        Paths.Trim(floors, 40);
        float dmg = Paths.Atk * FireFloorRate * Paths.Power(Overheat);
        SigZone z = Paths.Zone(at, FireFloorRadius, FireFloorTime, 0.5f, zone =>
        {
            PlayerController pl = Hostile.Player;
            if (pl == null || pl.IsDying) return;
            Paths.Circle(zone.transform.position, FireFloorRadius, dmg, 0f, "과열 폭발");
        });
        if (z == null) return;
        FireGround.Attach(z.gameObject, FireFloorRadius, FireFloorTime);
        floors.Add(z.gameObject);
    }

    // 화상 중첩 (Burn 은 한 겹이라 겹 수만큼 초당 피해를 곱해 다시 붙임)
    readonly Dictionary<int, (int n, float until)> burns = new Dictionary<int, (int, float)>();
    readonly List<int> pruneBuf = new List<int>();

    void AddBurn(GameObject g, int add, int cap)
    {
        if (g == null) return;
        int id = g.GetInstanceID();
        float now = Time.time;
        int n = burns.TryGetValue(id, out var had) && had.until > now ? had.n : 0;
        n = Mathf.Clamp(n + add, 1, Mathf.Max(1, cap));
        burns[id] = (n, now + BurnTime);
        Burn.Apply(g, Paths.Atk * BurnRate * Paths.Power(Overheat) * n, BurnTime);
    }

    void PruneBurns(float now)
    {
        if (burns.Count == 0) return;
        pruneBuf.Clear();
        foreach (var kv in burns) if (kv.Value.until < now) pruneBuf.Add(kv.Key);
        foreach (int k in pruneBuf) burns.Remove(k);
        pruneBuf.Clear();
    }

    // ================================================================= 산탄 돌격: 장전 돌진 (끝나는 시각 방식 · 매 프레임 Tick 이 옮김)
    bool dashing;
    float dashT0, dashT1, boostUntil;
    Vector3 dashFrom, dashTo;
    Vector2 dashDir;
    int dashChain;
    bool dashRammed;
    readonly HashSet<int> dashHit = new HashSet<int>();

    // chain: 극의의 두 번째 돌진 (1) · 아니면 0
    bool StartDash(PlayerController p, int chain)
    {
        Vector2 d = chain > 0 ? dashDir : (Vector2)p.MoveInput;
        if (d.sqrMagnitude < 0.04f) return false;       // 멈춰 있으면 돌진하지 않음 (이동 방향으로만)
        d.Normalize();
        Vector3 from = p.transform.position;
        float len = 0f;
        for (float s = 0.25f; s <= DashDist + 0.001f; s += 0.25f)
        {
            Vector3 q = from + (Vector3)(d * s);
            if (Hostile.IsWall(q) || !Hostile.InArena(q)) break;
            len = s;
        }
        if (len < 0.5f) return false;
        dashFrom = from;
        dashTo = from + (Vector3)(d * len);
        dashDir = d;
        dashT0 = Time.time;
        dashT1 = Time.time + DashTime * (len / DashDist);
        dashLastPos = from;
        dashing = true;
        dashChain = chain;
        if (chain == 0) { dashRammed = false; dashHit.Clear(); }
        Paths.Invuln(DashInvuln);
        PlayerMotion.Of(p).Lunge(DashTime + 0.05f, d);
        Fx.Spawn("fx_shock", from, 2f, VisualRules.Kit(new Color(1f, 0.7f, 0.5f, 0.7f), new Color(1f, 0.84f, 0.5f, 0.7f)), 26f);
        Hostile.Play("whoosh", 0.6f, 1.5f);
        return true;
    }

    // 돌진을 조용히 끝냄 (위치를 옮기지 않고 끝 효과도 없음): 쓰러짐 · 너무 늦음 · 다른 것이 위치를 옮김 (결계 가두기 · 연출)
    void CancelDash()
    {
        dashing = false;
        boomAtDashEnd = false;
    }

    Vector3 dashLastPos;

    void DashTick(PlayerController p, float now)
    {
        if (p.IsDying || now > dashT1 + 0.2f) { CancelDash(); return; }
        // 지난 프레임에 놓은 자리에서 크게 벗어남 = 결계 가두기 · 연출 등이 옮김 → 그 자리에서 돌진만 끝 (끝 효과는 그 자리에서)
        if (now > dashT0 && ((Vector2)(p.transform.position - dashLastPos)).sqrMagnitude > 0.5f * 0.5f)
        {
            dashing = false;
            dashTo = p.transform.position;
            DashEnded(p);
            return;
        }
        float t = dashT1 > dashT0 ? Mathf.Clamp01((now - dashT0) / (dashT1 - dashT0)) : 1f;
        Vector3 pos = Vector3.Lerp(dashFrom, dashTo, t);
        p.transform.position = pos;
        dashLastPos = pos;
        Fx.Spawn("fx_spark", pos - (Vector3)(dashDir * 0.5f), 0.9f, new Color(1f, 0.75f, 0.5f, 0.8f), 30f);
        if (Paths.Res(Shotgun) >= 1)
        {
            // 공명 I: 부딪힌 적을 밀침 (한 번 돌진에 한 번씩)
            float dmg = Paths.Atk * RamRate * Paths.Power(Shotgun);
            Vector2 dir = dashDir;
            Paths.Circle(pos, 1.2f, 0f, 0f, null, c =>
            {
                if (c == null || !dashHit.Add(c.GetInstanceID())) return;
                Paths.Deal(c.gameObject, dmg, dir, 3f, "산탄 돌격");
                dashRammed = true;
            });
        }
        if (t < 1f && now < dashT1 + 0.5f) return;
        dashing = false;
        p.transform.position = dashTo;
        DashEnded(p);
    }

    void DashEnded(PlayerController p)
    {
        if (dashRammed && Paths.Res(Shotgun) >= 1) boostUntil = Time.time + 4f;
        if (boomAtDashEnd)
        {
            boomAtDashEnd = false;
            OverheatBoom(dashTo, false);
        }
        if (!Paths.Mastered(Shotgun)) return;
        // 극의: 돌진 두 번 연달아 + 끝에 충격파
        if (dashChain == 0 && StartDash(p, 1)) return;
        Vector3 at = p.transform.position;
        Paths.Circle(at, 3.5f, Paths.Atk * QuakeRate * Paths.Power(Shotgun), 3f, "산탄 돌격");
        ShockRing.Spawn(at, 0.4f, 4f, 0.35f, VisualRules.Kit(new Color(1f, 0.5f, 0.4f, 0.9f), new Color(1f, 0.81f, 0.4f, 0.9f)), 0.3f);
        Fx.Spawn("fx_shock", at, 8f, VisualRules.Kit(new Color(1f, 0.6f, 0.45f, 0.85f), new Color(1f, 0.83f, 0.45f, 0.85f)), 22f);
        Hostile.Play("thump", 0.6f, 1f);
        Hostile.Shake(0.12f);
    }

    // ================================================================= 도탄 난사: 튕김 사슬
    readonly List<(Bullet b, int g)> bounces = new List<(Bullet, int)>();
    readonly List<Collider2D> bounceHits = new List<Collider2D>(64);
    float fragBudgetAt;
    int fragBudget;

    int BounceCap()
    {
        if (Paths.Mastered(Ricochet)) return BounceGenMax;
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        int card = sp != null ? sp.gunCard[SpecialAbilities.GunRicochet] : 0;
        return Mathf.Clamp(1 + card, 1, BounceGenMax);
    }

    // gen: 이 탄이 몇 번째로 튕긴 탄인가 (원래 탄 0)
    void AttachRicochet(Bullet b, int gen)
    {
        // 1.4.1~ 이 탄의 튕김은 갈래가 맡음 표시 (카드 튕김은 이 탄만 건너뜀 — 특수 총기 · 진화 덤 탄은 「도탄 사격」 카드 튕김 그대로)
        if (!b.TryGetComponent(out GunPathBounce mark)) mark = b.gameObject.AddComponent<GunPathBounce>();
        mark.gen = b.generation;
        bool bounced = false;
        b.onHitEnemy += (bullet, col) =>
        {
            if (bounced || bullet == null || col == null) return;
            bounced = true;
            Bounce(bullet, col, gen);
        };
        if (Paths.Res(Ricochet) < 2) return;
        // 공명 II: 벽 · 경계에서 되튕김 (두 번까지) · 다 쓰고 사라질 때 파편 둘
        if (b.onHitWall == null)
        {
            int walls = 0;
            b.onHitWall = () =>
            {
                if (b == null) return;
                if (++walls > 2) { b.onHitWall = null; b.Release(); return; }
                Reflect(b);
            };
        }
        if (b.onSpent == null)
            b.onSpent = () =>
            {
                if (b != null) Fragments(b.transform.position, b.Direction);
                return false;
            };
    }

    static void Reflect(Bullet b)
    {
        Vector2 p = b.transform.position;
        Vector2 d = b.Direction;
        bool wx = Mathf.Abs(d.x) > 0.01f && Hostile.IsWall(p + new Vector2(Mathf.Sign(d.x) * 0.5f, 0f));
        bool wy = Mathf.Abs(d.y) > 0.01f && Hostile.IsWall(p + new Vector2(0f, Mathf.Sign(d.y) * 0.5f));
        if (wx) d.x = -d.x;
        if (wy) d.y = -d.y;
        if (!wx && !wy) d = -d;
        b.Dir = d;
        b.transform.position = (Vector3)(p + d.normalized * 0.35f);
        b.Halt();
        Fx.Spawn("fx_spark", p, 1f, BounceColor, 26f);
    }

    void Fragments(Vector3 at, Vector2 dir)
    {
        if (Time.time > fragBudgetAt) { fragBudgetAt = Time.time + 0.2f; fragBudget = 24; }
        if (fragBudget < 2 || dir.sqrMagnitude < 0.0001f) return;
        fragBudget -= 2;
        float dmg = Paths.Atk * FragRate * Paths.Power(Ricochet);
        for (int i = 0; i < 2; i++)
        {
            Vector2 d = Quaternion.Euler(0f, 0f, (i == 0 ? 35f : -35f) + Random.Range(-10f, 10f)) * dir;
            PathShot(at, d, dmg, 1, 22f, 6f, "fx_spark", 0.6f, BounceColor, "도탄 파편");
        }
    }

    void Bounce(Bullet from, Collider2D hit, int gen)
    {
        if (gen >= BounceCap()) return;
        if (bounces.Count >= BounceLiveMax)
        {
            bounces.RemoveAll(x => x.b == null || !x.b.gameObject.activeSelf || x.b.generation != x.g);
            if (bounces.Count >= BounceLiveMax) return;
        }
        PlayerController pc = Hostile.Player;
        if (pc == null || pc.IsDying) return;
        Vector3 at = hit.transform.position;
        Collider2D best = null;
        float bestD = 81f;
        foreach (Collider2D c in Specials.Overlap(at, 9f, bounceHits))
        {
            if (c == null || c == hit || !c.CompareTag("enermy")) continue;
            if (c.TryGetComponent(out EnermyController ce) && ce.IsDead) continue;
            float d = ((Vector2)(c.transform.position - at)).sqrMagnitude;
            if (d < bestD) { bestD = d; best = c; }
        }
        bounceHits.Clear();
        if (best == null) return;
        SpecialAbilities sp = pc.special;
        float rate;
        if (gen == 0)
        {
            int card = sp != null ? sp.gunCard[SpecialAbilities.GunRicochet] : 0;
            rate = (BounceBase + BounceCard * card) * Paths.Power(Ricochet);
        }
        else rate = 1f;
        if (gen >= 1 && gen <= 3 && Paths.Res(Ricochet) >= 1) rate *= BounceGrow;       // 공명 I: 튕길 때마다 +15% (3회까지)
        if (gen >= 1 && Paths.Mastered(Ricochet)) rate *= BounceDecay;                   // 극의: 무제한 대신 −10%씩
        Vector2 dir = ((Vector2)(best.transform.position - at)).normalized;
        Bullet nb = pc.CreateBullet(at + (Vector3)(dir * 0.6f), dir, from.damage * rate, 1, 0f, false, 0.3f);
        if (nb == null) return;
        nb.skillCharge = 0f;
        int hitId = hit.TryGetComponent(out EnermyController he) ? he.GetInstanceID() : 0;
        nb.hitOnce = new HashSet<int> { hitId };
        nb.transform.localScale *= 0.75f;
        if (nb.TryGetComponent(out SpriteRenderer sr)) sr.color = BounceColor;
        if (sp != null) nb.onHitEnemy += (bullet, col) => sp.GunCardHit(col, bullet.damage, bullet.Direction, false, false);     // 튕긴 탄에도 불 · 번개
        AttachRicochet(nb, gen + 1);
        bounces.Add((nb, nb.generation));
        Fx.Spawn("fx_spark", at, 0.9f, BounceColor, 26f);
    }

    // ================================================================= 갈래 효과 투사체 (평타 탄 모양 · 피해 출처 이름이 판 기록 dealt_by 에 남음)
    internal Bullet PathShot(Vector3 start, Vector2 dir, float dmg, int pene, float speed, float range, string fx, float size, Color tint, string source)
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying || dmg <= 0f) return null;
        Bullet b;
        using (DamageSource.As(source)) b = p.CreateBullet(start, dir, dmg, pene, 0f, true, 0.3f);
        if (b == null) return null;
        b.speed = speed;
        b.lifetime = range / Mathf.Max(1f, speed);
        b.skillCharge = 0f;
        if (b.TryGetComponent(out SpriteRenderer sr))
        {
            Sprite[] f = fx != null ? Fx.Frames(fx) : null;
            if (f != null && f.Length > 0)
            {
                sr.sprite = f[0];
                b.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, f[0].bounds.size.y));
                FrameLoop loop = b.gameObject.AddComponent<FrameLoop>();
                loop.frames = f;
            }
            sr.color = tint;
        }
        return b;
    }

    // ================================================================= 포탑 설치
    readonly List<GameObject> turrets = new List<GameObject>();
    float turretSoundAt;

    internal bool TurretSound()
    {
        if (Time.time < turretSoundAt) return false;
        turretSoundAt = Time.time + 0.12f;
        return true;
    }

    int TurretMax => Paths.Mastered(Turret) ? 4 : Paths.Res(Turret) >= 1 ? 3 : 2;

    bool NearTurret(float r)
    {
        PlayerController p = Hostile.Player;
        if (p == null) return false;
        Vector3 me = p.transform.position;
        for (int i = 0; i < turrets.Count; i++)
            if (turrets[i] != null && ((Vector2)(turrets[i].transform.position - me)).sqrMagnitude <= r * r) return true;
        return false;
    }

    void SpawnTurret(Vector3 at)
    {
        int max = TurretMax;
        turrets.RemoveAll(t => t == null);
        // 넘치면 가장 오래된 것부터 (공명 II 면 터지며 사라짐)
        while (turrets.Count >= max && turrets.Count > 0)
        {
            GameObject old = turrets[0];
            turrets.RemoveAt(0);
            if (old != null && old.TryGetComponent(out GunTurret ot)) ot.Expire(Paths.Res(Turret) >= 2);
            else if (old != null) Object.Destroy(old);
        }
        Paths.Trim(turrets, max);       // 안전 상한 (위에서 이미 맞춤)
        GameObject go = new GameObject("GunTurret");
        go.transform.position = new Vector3(at.x, at.y, 0f);
        GunTurret tur = go.AddComponent<GunTurret>();
        tur.Init(this);
        turrets.Add(go);
        ShockRing.Spawn(at, 0.2f, 1.8f, 0.3f, new Color(TurretColor.r, TurretColor.g, TurretColor.b, 0.85f), 0.14f);
        Fx.Spawn("fx_sparkle", at, 1.6f, TurretColor, 22f);
        Hostile.Play("clank", 0.45f, 1.2f);
    }

    // 총잡이의 춤 (완성): 장전을 마치면 포탑도 가까운 적 여섯에게 한 발씩
    public void TurretVolley()
    {
        if (!Paths.Has(Turret)) return;
        for (int i = 0; i < turrets.Count; i++)
            if (turrets[i] != null && turrets[i].TryGetComponent(out GunTurret t)) t.Volley(Paths.Atk * DanceRate);
    }
}
