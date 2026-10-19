using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 궁수 갈래 (2026-10-19 · docs/review/2026-10-19-빌드-시너지-기획.md 3-4) — 활 쪽 효과
//   CharacterKit.cs 의 궁수 전용 줄(UpdateBow · ArrowRain · UpdateRainAim)이 한 줄씩 부름
//   판 상태 · 덫 · 바람 · 천공 게이지는 Paths/Paths.Archer.cs (ArcherPaths)
//   장궁: 당기기 +40% · 완벽 ×1.35 관통 / 속사: 당기지 않고 연사 / 덫: 완벽 화살 → 덫 / 바람: 당기며 달리기 / 천공: 화살비
// 상태는 「다음 시각」 값만 (코루틴으로 켜고 끄지 않음)
public partial class CharacterKit
{
    // ---------------- 속사: 한 발 간격 = 가득 당기는 시간 × 이 값 (기본 공격 속도에서 초당 약 4발 · 극의 6발), 한 발 피해 = 공격력 × 이 값
    //   평타 화력: 4발 × 35% ≈ 완벽 화살을 계속 쏘는 궁수의 95~100% (관통 없음 · 보스는 조금 약하게)
    const float RapidStep = 0.185f, RapidMasterStep = 0.123f;
    const float RapidPower = 0.35f, RapidMasterPower = 0.25f;
    // 한 발 간격 하한 (초당 최대 10번 놓음) — 공격 속도를 쌓아도 멀티 샷과 곱해져 초당 수십 발 · 명중 효과 폭주가 되지 않게 (예전 0.06초)
    const float RapidMinStep = 0.1f;
    float rapidNext;                    // 다음 화살을 쏠 수 있는 시각
    int rapidCount;                     // 쏜 수 (다섯 발마다 「가득 당긴 화살」)
    int bowPathFrame = -1;              // 한 번 놓을 때 덫이 화살마다 겹쳐 놓이지 않게

    // 시위를 당기는 중 (바람 갈래: 당기는 동안 느려지지 않게 MoveMul 을 되돌림)
    public bool BowDrawing => drawing;

    // ================================================================= 속사 (UpdateBow 맨 앞) — true 면 이번 프레임 활은 여기서 끝
    bool BowPathRapid(bool canDraw, float fullTime)
    {
        if (!Paths.Has(ArcherPaths.Rapid)) return false;
        if (drawing)
        {
            // 당기던 중에 속사에 들어옴: 시위를 그냥 놓음 (화살 없이)
            drawing = false;
            PlayerLook.Draw(0f);
        }
        SpecialFeedback.Hide(bowLine);
        if (!canDraw || !GameInput.FireHeld || Time.time < rapidNext) return true;

        bool master = Paths.Mastered(ArcherPaths.Rapid);
        rapidNext = Time.time + Mathf.Max(RapidMinStep, fullTime * (master ? RapidMasterStep : RapidStep));
        rapidCount = (rapidCount + 1) % 60;
        bool full = rapidCount % ArcherPaths.RapidFullEvery == 0;                 // 「가득 당긴 화살」 효과는 다섯 발마다
        // 1.4.1~ 사냥꾼의 집중(켜진 동안) · 사냥 본능(남은 발 수만큼)도 속사에서 듣게: 그 화살을 「가득 당긴 화살」로 (예전: UpdateBow 에 닿지 않아 효과 없음 · 본능 x3 이 안 줄었음)
        if (Special != null && (Special.KitFocusActive || Special.KitFreeDraws > 0))
        {
            if (!Special.KitFocusActive) Special.KitFreeDraws--;
            full = true;
        }
        int echoEvery = Paths.Res(ArcherPaths.Rapid) >= 1 ? ArcherPaths.RapidEchoRes : ArcherPaths.RapidFullEvery;
        bool echo = card[1] > 0 && rapidCount % echoEvery == 0;
        float k = full ? 1f : 0.4f;     // 화살 효과에 넘기는 당김 정도

        Vector3 start = player.MuzzlePosition;
        Vector3 target = Mouse;
        player.FaceTowards(target);
        Vector2 dir = ((Vector2)(target - start)).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = body != null && body.flipX ? Vector2.left : Vector2.right;
        PlayerLook.Fired(-1);
        int shots = Mathf.Max(1, player.multiShot);
        float dmg = Damage * player.MultiShotDamageRate(shots) * (master ? RapidMasterPower : RapidPower) * BowPathPower(full, k, true);
        int shotIndex = 0, mid = shots / 2;
        using (WeakHit.AimScope(1.75f))
        foreach (Vector2 d in Spread(dir, shots, 6f))
        {
            bool main = shotIndex++ == mid;     // 가운데 화살 — 덤 화살(멀티 샷 양옆)은 고유 스킬 명중 효과를 「가득 당긴 화살」(다섯 발마다)에만 (검토 2026-10-19)
            Bullet b = Projectile(start, d, dmg, 1, 70f, 0f, "fx_arrow", full ? 0.5f : 0.38f, full ? new Color(1f, 0.9f, 0.5f) : new Color(1f, 0.97f, 0.8f), false);
            if (b == null) continue;
            ArrowForm(b, k, start);
            if (full)
            {
                if (card[0] > 0) b.onHitEnemy += (arrow, c) => SplitArrow(arrow, c);
                if (card[6] > 0) b.onHitEnemy += (arrow, c) => HuntMark.Apply(c.gameObject, 4f);
                if (card[2] > 0) GustArrow(b);
            }
            b.onHitEnemy += (arrow, c) => Paths.Safe(() => ArcherPaths.CountHit(c));         // 울림 범위 2배 (공명 II) — 울림보다 먼저 셈
            SignatureSkills.Arrow(b, k, !main && !full);      // 덤 화살은 쪼개진 화살처럼 (울림 · 씨앗 같은 명중 효과 없이)
            b.onHitEnemy += (arrow, col) => arrow.damage *= PierceFalloff;
            BowPathArrow(b, full);
        }
        if (full) SignatureSkills.BowRelease(start, dir, 1f);          // 집중 호흡 · 매복 · 바람길 · 후퇴 사격 (가득 당긴 화살처럼)
        BowPathRelease(start, dir, full);
        if (echo) StartCoroutine(EchoArrow(dir, dmg * 0.5f, 70f, 0.38f, k));
        if (full) Play("bowtwang", 0.4f, 1.25f);
        else Play("pew", 0.3f, Random.Range(1.2f, 1.45f));
        return true;
    }

    // ================================================================= 당기는 동안 매 프레임 (가득 당기는 시간을 돌려줌)
    float BowPathFrame(float fullTime)
    {
        if (!Paths.Active) return fullTime;
        // 장궁: 당기는 시간 +40% (순식간에 당기는 버프는 그대로) · 공명 I: 지난 완벽 화살이 꿰뚫은 적마다 8% 빠르게
        if (Paths.Has(ArcherPaths.Long) && fullTime > 0.05f)
        {
            fullTime *= ArcherPaths.LongDrawMul;
            ArcherPaths me = ArcherPaths.Me;
            if (me != null && Paths.Res(ArcherPaths.Long) >= 1) fullTime /= 1f + ArcherPaths.LongHaste * me.LongBank;
            fullTime = Mathf.Max(0.06f, fullTime);
        }
        // 바람 대가: 숨이 2배 빨리 닳음 (UpdateBow 가 한 번 · 여기서 한 번 더)
        if (Paths.Has(ArcherPaths.Wind) && !GameInput.Auto && !GameInput.TrailerRunning) ConsumeBreath();
        if (fullPlayed && draw >= 1f)
        {
            // 바람 공명 I: 바람길 위에서는 가득 찬 뒤 언제 놓아도 완벽 (떨리지도 않음)
            if (Paths.Res(ArcherPaths.Wind) >= 1 && SignatureSkills.OnWind) fullAt = Time.time;
            // 장궁 극의: 완벽 판정 폭 2배 (가득 찬 뒤 흐르는 시간을 절반으로 셈 · 떨림도 그만큼 늦게)
            else if (Paths.Mastered(ArcherPaths.Long)) fullAt += Time.deltaTime * 0.5f;
        }
        return fullTime;
    }

    // 1.4.1~ 바람 공명 I: 지금 바람길 위라 언제 놓아도 완벽 (UpdateBow 가 폭풍의 길 순간 당김에 씀)
    bool BowPathWindPerfect => Paths.Active && Paths.Res(ArcherPaths.Wind) >= 1 && SignatureSkills.OnWind;

    // ================================================================= 화살 피해 배율 (입문 · 대가)
    float BowPathPower(bool perfect) => BowPathPower(perfect, draw, false);

    float BowPathPower(bool perfect, float k, bool rapid)
    {
        if (!Paths.Active) return 1f;
        float m = 1f;
        if (Paths.Has(ArcherPaths.Long))
        {
            if (perfect) m *= ArcherPaths.LongPerfectMul * Paths.Power(ArcherPaths.Long);
            else if (!rapid && k < 1f) m *= ArcherPaths.LongWeakMul;           // 대가: 덜 당긴 화살 −40% (속사 화살은 당기지 않으므로 빼고)
        }
        if (Paths.Has(ArcherPaths.Trap)) m *= 0.8f;                            // 대가: 화살 직접 피해 −20%
        if (Paths.Has(ArcherPaths.Sky)) m *= ArcherPaths.SkyCost;               // 대가: 평타 −25%
        return m;
    }

    // ================================================================= 화살 하나마다 (다른 효과를 모두 붙인 뒤 · perfect: 완벽 화살 또는 속사의 다섯 번째)
    void BowPathArrow(Bullet b, bool perfect)
    {
        if (b == null || !Paths.Active) return;
        ArcherPaths me = ArcherPaths.Me;
        if (me == null) return;
        bool first = bowPathFrame != Time.frameCount;
        bowPathFrame = Time.frameCount;

        // 천공: 평타가 맞힐 때마다 화살비 게이지 (피해에 비례 · 완벽 화살 한 대에 약 1%)
        if (Paths.Has(ArcherPaths.Sky))
        {
            float atk = Mathf.Max(0.01f, Damage);
            b.onHitEnemy += (arrow, c) => ArcherPaths.AddGauge(Mathf.Min(0.03f, arrow.damage / atk * ArcherPaths.SkyHitGauge));
        }

        if (perfect && Paths.Has(ArcherPaths.Long))
        {
            // 장궁: 완벽 화살은 모든 적을 꿰뚫음
            b.pene = 9999;
            b.hitOnce = new HashSet<int>();
            if (Paths.Res(ArcherPaths.Long) >= 1) b.onHitEnemy += (arrow, c) => me.LongPierced(c);
        }
        // 공명 II: 집중 호흡 5중첩이면 굵은 관통 광선 (그 화살의 피해 그대로)
        //   1.4.1~ 일찍 return 하지 않음 — 덫 놓기 · 천공 하늘 화살이 광선 때문에 판 끝까지 꺼지던 것 (명중 효과는 BowPathBeam 이 직접)
        bool beam = perfect && Paths.Has(ArcherPaths.Long) && Paths.Res(ArcherPaths.Long) >= 2 && SignatureSkills.FocusStacks >= 5;

        // 덫: 완벽 화살이 떨어진 자리에 (여러 발이어도 한 번 놓을 때 하나)
        bool trap = perfect && first && Paths.Has(ArcherPaths.Trap);
        Vector3 trapAt = Vector3.zero;
        if (trap)
        {
            Vector3 from = b.transform.position;
            float reach = Mathf.Min(11f, Vector2.Distance(from, Mouse));
            trapAt = from + (Vector3)(b.Direction * reach);
            if (!beam) me.ArmTrap(b, trapAt);
        }

        // 천공 공명 II: 화살비 뒤 3초 동안 평타가 하늘로 쏴져 화살비 자리의 적에게 떨어짐 (화살을 거두므로 광선보다 먼저 · 광선은 다음 화살부터)
        if (me.SkyDrop(b))
        {
            if (trap && beam) me.TrapAt(trapAt);
            return;
        }
        if (beam) BowPathBeam(b, trap, trapAt);
    }

    // 1.4.1~ 광선이 꿰뚫은 적 (장궁 공명 I 당김 가속 — BowPathRelease 의 Released 가 longBank 를 비운 뒤에 셈)
    readonly List<Collider2D> beamPierced = new List<Collider2D>();

    // 장궁 공명 II: 굵은 관통 광선 (레일건처럼 · 화살은 거둠)
    //   1.4.1~ 화살의 명중 효과가 사라지므로 천공 게이지 · 장궁 공명 I · 덫(첫 명중 자리, 없으면 겨눈 자리)을 여기서 직접
    void BowPathBeam(Bullet b, bool trap, Vector3 trapAt)
    {
        Vector3 from = b.transform.position;
        Vector2 dir = b.Direction;
        if (dir.sqrMagnitude < 0.01f)
        {
            if (trap) ArcherPaths.Me?.TrapAt(trapAt);
            return;
        }
        Vector3 to = from + (Vector3)(dir * ArcherPaths.LongBeamLength);
        float dmg = b.damage;
        b.Release();
        ArcherPaths me = ArcherPaths.Me;
        bool sky = Paths.Has(ArcherPaths.Sky);
        bool pierce = me != null && Paths.Res(ArcherPaths.Long) >= 1;
        float atk = Mathf.Max(0.01f, Damage);
        float nearest = float.MaxValue;
        using (var ovl = PhysHits.Circle((from + to) * 0.5f, ArcherPaths.LongBeamLength * 0.5f + ArcherPaths.LongBeamWidth, true))
            foreach (Collider2D c in ovl)
            {
                if (c == null || !Paths.IsFoe(c)) continue;
                if (Hostile.DistanceToSegment(c.transform.position, from, to) > ArcherPaths.LongBeamWidth) continue;
                Vector3 at = c.transform.position;
                float along = Vector2.Dot((Vector2)(at - from), dir);
                if (along < nearest) { nearest = along; trapAt = at; }
                if (pierce && beamPierced.Count < 32) beamPierced.Add(c);
                Paths.Deal(c.gameObject, dmg, dir, 2f, "관통 광선");
                if (sky) ArcherPaths.AddGauge(Mathf.Min(0.03f, dmg / atk * ArcherPaths.SkyHitGauge));
                Fx.Spawn("fx_spark", at, 1.6f, new Color(1f, 0.9f, 0.5f), 22f);
            }
        if (trap && me != null) me.TrapAt(trapAt);
        Fx.Beam(from, to, 1.6f, new Color(1f, 0.9f, 0.45f, 0.9f), 0.3f);
        Fx.Spawn("fx_shock", from, 3.2f, new Color(1f, 0.9f, 0.5f, 0.8f), 22f);
        SpecialAbilities.SharedFx?.Play("railgun", 0.45f, 1.4f);
        Hostile.Shake(0.12f);
    }

    // ================================================================= 시위를 놓은 뒤 (BowRelease 다음)
    void BowPathRelease(Vector3 start, Vector2 dir, bool perfect)
    {
        if (!Paths.Active) return;
        ArcherPaths me = ArcherPaths.Me;
        if (me != null) me.Released(start, dir, perfect);
        // 1.4.1~ 장궁 공명 I: 관통 광선이 꿰뚫은 적도 다음 당김을 빠르게 (Released 가 비운 뒤에 채움)
        if (me != null) foreach (Collider2D c in beamPierced) me.LongPierced(c);
        beamPierced.Clear();
    }

    // ================================================================= 화살비 (천공)
    // 범위 배율 (ArrowRain · 조준 원)
    float BowPathRainRadius => Paths.Has(ArcherPaths.Sky) ? ArcherPaths.SkyRainRadius : 1f;

    // 화살비를 쏜 뒤 (UpdateRainAim): 천공 극의 — 다른 무리 위에 화살비 하나 더 (피해 40% · 보스는 두 대까지)
    void BowPathRainCast(Vector3 at)
    {
        if (!Paths.Mastered(ArcherPaths.Sky)) return;
        float radius = 4f * UltMul * BowPathRainRadius;
        Vector3 second = at;
        bool found = false;
        foreach (Transform t in Paths.Nearest(transform.position, 16f, 12))
        {
            if (t == null || Paths.IsBoss(t.gameObject) || (t.position - at).sqrMagnitude < radius * radius * 2.5f) continue;
            second = t.position;
            found = true;
            break;
        }
        if (!found)
        {
            Vector2 away = (Vector2)(at - transform.position);
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitCircle;
            second = Hostile.ClampArena(at + (Vector3)(away.normalized * radius * 1.8f));
        }
        second.z = 0f;
        StartCoroutine(BowPathEchoRain(second, radius));
    }

    IEnumerator BowPathEchoRain(Vector3 center, float radius)
    {
        yield return new WaitForSeconds(0.25f);
        if (player == null || player.IsDying) yield break;
        Hostile.Circle(center, radius, 0.4f, new Color(0.75f, 0.6f, 1f, 0.6f));
        Play("whoosh", 0.6f, 1.1f);
        Dictionary<Collider2D, int> bossHits = new Dictionary<Collider2D, int>();
        for (int i = 0; i < 20; i++)
        {
            if (player == null || player.IsDying) yield break;
            float dmg = Damage * 2.5f * UltMul * ArcherPaths.SkyEchoRain * Paths.Power(ArcherPaths.Sky);
            Vector3 at = center + (Vector3)(Random.insideUnitCircle * radius);
            Fx.Spawn("fx_arrowrain", at + Vector3.up * 1f, 2f, new Color(0.85f, 0.75f, 1f), 18f);
            using (var ovl = PhysHits.Circle(at, 1.3f)) foreach (Collider2D c in ovl)
            {
                bool boss = c.CompareTag("boss");
                if (!boss && !c.CompareTag("enermy")) continue;
                if (boss)
                {
                    bossHits.TryGetValue(c, out int n);
                    if (n >= 2) continue;
                    bossHits[c] = n + 1;
                }
                Specials.Damage(c.gameObject, dmg, (c.transform.position - at).normalized, 0.5f);
            }
            if (i % 4 == 0) Play("arrowfly", 0.3f, Random.Range(0.9f, 1.3f));
            yield return new WaitForSeconds(0.06f);
        }
    }

    // ================================================================= 덫 공명 I: 터진 자리에 가시 덤불 (ArcherPaths 가 부름)
    public void BowPathThorns(Vector3 at)
    {
        if (!isActiveAndEnabled || player == null || player.IsDying) return;
        StartCoroutine(Thorns(at, 1.8f, 2f + card[3]));
    }
}
