using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 시계공 고유 스킬 (1.0.2) · 패시브(기계 심장 · 예측 · 부품 수집 · 시간 여유)
//   t.overclock 과회전 · t.alarm 자명종 · t.cog 맞물린 톱니 · t.pocket 회중시계 · t.minute 분침
//   t.echo 시간의 메아리 · t.spring 태엽 감기 · t.loop 시간 고리 · t.borrowed 빌린 시간 · t.stopwatch 스톱워치
// 진화: te.gearstorm 톱니 폭풍 · te.chime 대종 · te.paradox 역설 · te.timeless 영원 · te.clockwork 태엽 장치
//       te.echoes 무한 반향 · te.debt 시간 빚
public partial class SignatureSkills
{
    static readonly Color ClockBrass = new Color(1f, 0.78f, 0.35f);
    static readonly Color ClockBlue = new Color(0.55f, 0.85f, 1f);

    bool IsClock => who == CharacterId.Clockmaker;

    // ---------------- 훅 (CharacterKit.Clockmaker 가 부름)
    public static void GearThrown(Vector3 start, Vector2 dir, float dmg, bool big) { if (Instance != null && Instance.IsClock) Instance.OnGearThrown(start, dir, dmg, big); }
    public static void GearHit(Collider2D c, float dmg, bool big) { }
    public static void GearBounced() { if (Instance != null && Instance.IsClock) Instance.OnGearBounced(); }
    public static float BounceMul() => Instance != null && Instance.IsClock && Instance.L("t.cog") > 0 ? 1f + V(Instance.L("t.cog"), 0.15f, 0.07f) : 1f;
    public static float BigGearMul() => Instance != null && Instance.IsClock && Instance.L("t.minute") > 0 ? 1f + V(Instance.L("t.minute"), 0.3f, 0.15f) : 1f;     // 1.1.3: +40~100% → +30~75% (LevelShop.NewHeroes 분침 카드 표시와 같게)
    public static void BigGear(Vector3 p) { if (Instance != null && Instance.IsClock) Instance.OnBigGear(p); }
    public static float RewindHealMul() => Instance != null && Instance.IsClock && Instance.L("t.pocket") > 0 ? 1f + V(Instance.L("t.pocket"), 0.2f, 0.1f) : 1f;
    public static float LeftClockMul() => Instance != null && Instance.IsClock && Instance.E("te.paradox") ? 2f : 1f;
    public static void Rewound(Vector3 from, Vector3 to, float lost, List<Collider2D> caught) { if (Instance != null && Instance.IsClock) Instance.OnRewound(from, to, lost, caught); }

    // ---------------- 상태
    float overclockUntil;
    float alarmReady;
    readonly Queue<(float t, float dmg)> dealtLog = new Queue<(float, float)>();
    float dealtSum;

    void OnGearThrown(Vector3 start, Vector2 dir, float dmg, bool big)
    {
        // 시간의 메아리: 0.6초 뒤 같은 방향으로 잔상 톱니 (무한 반향: 둘)
        if (L("t.echo") > 0)
        {
            int n = E("te.echoes") ? 2 : 1;
            for (int i = 0; i < n; i++) StartCoroutine(EchoGear(dir, dmg * V(L("t.echo"), 0.3f, 0.1f), 0.6f + 0.3f * i));
        }
    }

    IEnumerator EchoGear(Vector2 dir, float dmg, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!Alive || player == null) yield break;
        Bullet b = Shot(player.MuzzlePosition, dir, dmg, 2, 34f, 14f, "fx_shuriken", 1f * Fs("t.echo"), Tint("t.echo", new Color(0.6f, 0.85f, 1f, 0.7f)), true);
        if (b != null) b.onHitEnemy += (s, c) => { if (Kit != null) Kit.AddClockTicks(0.5f); };
    }

    void OnGearBounced()
    {
        // 톱니 폭풍: 튕길 때마다 작은 톱니가 하나 더 갈라져 나옴
        if (!E("te.gearstorm") || player == null) return;
        Transform t = Specials.NearestEnemy(player.transform.position, 10f);
        if (t == null) return;
        Vector3 from = t.position + (Vector3)(Random.insideUnitCircle.normalized * 2f);
        Shot(from, ((Vector2)(t.position - from)).normalized, Atk * 0.7f, 1, 30f, 6f, "fx_shuriken", 0.9f, Tint("t.cog", ClockBrass), true);       // 1.1.2~ 50 → 70%
    }

    void OnBigGear(Vector3 p)
    {
        // 과회전: 3초 동안 공격 속도 +
        if (L("t.overclock") > 0)
        {
            overclockUntil = Time.time + 3f;
            Fx.Spawn("fx_sparkle", transform.position, 2f, Tint("t.overclock", ClockBrass), 22f);
        }
        // 대종: 둘레 모든 적이 0.5초 멈춤
        if (E("te.chime"))
        {
            using var tier = Fx.Tier(3);
            Circle(p, 14f, 0f, 0f, c => Stun(c.gameObject, 0.5f));
            ShockRing.Spawn(p, 1f, 14f, 0.5f, new Color(ClockBrass.r, ClockBrass.g, ClockBrass.b, 0.8f), 0.3f);
            Flair(null, p, 6f, ClockBrass);
        }
        // 태엽 장치: 정각 톱니가 세 갈래로 갈라짐
        if (E("te.clockwork"))
        {
            float spin = Random.Range(0f, 360f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, spin + 120f * i) * Vector2.right;
                Shot(p, d, Atk * 1.2f, 3, 30f, 10f, "fx_shuriken", 1.2f, ClockBrass, true);
            }
        }
    }

    void OnRewound(Vector3 from, Vector3 to, float lost, List<Collider2D> caught)
    {
        // 시간 고리: 돌아간 자리에 4초 동안 느리게 하는 고리
        if (L("t.loop") > 0)
        {
            float slow = 1f - V(L("t.loop"), 0.3f, 0.1f);
            float r = 3.5f * Fs("t.loop");
            Zone(to, r, 4f, 0.25f, z => { foreach (Collider2D c in Inside(z)) if (c != null && c.TryGetComponent(out EnermyController e)) e.Slow(slow, 0.35f); },
                 "fx_target_rune", Tint("t.loop", new Color(ClockBlue.r, ClockBlue.g, ClockBlue.b, 0.55f)), r * 2f, true);
        }
        // 스톱워치: 지난 3초 동안 준 피해의 일부를 한 번 더 (시간 빚: 두 배 · 게이지 30%)
        // 2026-10-19~ 갈래 시간 빚 공명 II: 스톱워치가 없어도 1단계(20%)로 · 있으면 더 큰 값 (ClockmakerPaths.DebtStopwatchOn)
        bool debtWatch = ClockmakerPaths.DebtStopwatchOn;
        if ((L("t.stopwatch") > 0 || debtWatch) && dealtSum > 0f && Alive)
        {
            float pct = Mathf.Max(V(L("t.stopwatch"), 0.2f, 0.1f), debtWatch ? ClockmakerPaths.DebtStopwatch : 0f) * (E("te.debt") ? 2f : 1f);
            List<Transform> near = Nearest(to, 7f, 8);
            float each = near.Count > 0 ? dealtSum * pct / near.Count : 0f;
            // 다시 준 피해는 다음 스톱워치의 「지난 3초 피해」에 넣지 않음 (되감기가 잦아지면 스스로 불어나던 고리 · 갈래가 되감기를 늘려서 막음)
            stopwatchDealing = true;
            try
            {
                foreach (Transform t in near)
                {
                    if (t == null) continue;
                    Deal(t.gameObject, each, (t.position - to).normalized, 0.5f);
                    if (t != null) Fx.Spawn("fx_sparkle", t.position, 1.2f, Tint("t.stopwatch", ClockBlue), 24f);
                }
            }
            finally { stopwatchDealing = false; }
            Flair("t.stopwatch", to, 4f, ClockBlue);
        }
        if (E("te.debt") && player != null && player.skillGauge != null) player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * 0.3f);
        // 2026-10-19~ 갈래: 시간 여행(되감은 뒤 무적 · 선 위 적 멈춤) · 교차 정각 되감기 (Paths/Paths.Clockmaker)
        ClockmakerPaths.Live?.OnRewound(from, to, caught);
    }

    // ---------------- 공통 훅에서
    bool stopwatchDealing;      // 스톱워치가 다시 주는 피해 중 (try/finally 로만 켬)

    void ClockHit(GameObject target, float dmg)
    {
        if (stopwatchDealing) return;
        dealtLog.Enqueue((Time.time, dmg));
        dealtSum += dmg;
    }

    float ClockTaken(float dmg)
    {
        // 예측 (패시브): 체력이 절반 이상일 때 받는 피해 -
        if (Sp != null && Sp.Has(SpecialAbilities.KitForesight) && player != null && player.PlayerHealth >= player.PlayerMaxHealth * 0.5f)
            dmg *= Sp.IsEvolved(SpecialAbilities.KitForesight) ? 0.8f : 0.88f;
        return dmg;
    }

    float ClockRate() => L("t.overclock") > 0 && Time.time < overclockUntil ? 1f + V(L("t.overclock"), 0.2f, 0.1f) : 1f;

    void ClockKilled(Vector3 pos)
    {
        // 빌린 시간: 처치마다 필살기 게이지
        if (L("t.borrowed") > 0 && player != null && player.skillGauge != null)
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * V(L("t.borrowed"), 0.005f, 0.0025f));
        // 부품 수집 (패시브): 확률로 다음 톱니가 정각
        if (Sp != null && Sp.Has(SpecialAbilities.KitSpareParts) && Kit != null && Random.value < (Sp.IsEvolved(SpecialAbilities.KitSpareParts) ? 0.15f : 0.08f))
        {
            Kit.AddClockTicks(Kit.ClockNeed);
            Fx.Spawn("fx_sparkle", pos, 1.2f, ClockBrass, 22f);
        }
    }

    void ClockTick()
    {
        while (dealtLog.Count > 0 && Time.time - dealtLog.Peek().t > 3f) dealtSum -= dealtLog.Dequeue().dmg;
        if (dealtLog.Count == 0) dealtSum = 0f;
        float dt = Time.deltaTime;
        // 태엽 감기: 가만히 서 있으면 눈금이 저절로
        if (L("t.spring") > 0 && Kit != null && !Kit.Moving) Kit.AddClockTicks(V(L("t.spring"), 1f, 0.5f) * dt);
        // 기계 심장 (패시브): 초당 회복
        if (Sp != null && Sp.Has(SpecialAbilities.KitMechHeart) && player != null && player.PlayerHealth < player.PlayerMaxHealth)
            player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + (Sp.IsEvolved(SpecialAbilities.KitMechHeart) ? 1f : 0.5f) * dt);
        // 시간 여유 (패시브): 필살기 게이지가 더 빨리
        if (Sp != null && Sp.Has(SpecialAbilities.KitSpareTime) && player != null && player.skillGauge != null)
            player.skillGauge.AddSkillPoint(player.skillGauge.pointsPerSecond * (Sp.IsEvolved(SpecialAbilities.KitSpareTime) ? 0.18f : 0.1f) * dt);
        // 자명종: 체력이 30% 아래로 떨어지면 시간이 멈춤 (영원: 쿨타임 절반 · 멈춘 동안 회복)
        if (L("t.alarm") > 0 && player != null && Time.time >= alarmReady && player.PlayerHealth < player.PlayerMaxHealth * 0.3f)
        {
            float stop = V(L("t.alarm"), 1f, 0.5f);
            alarmReady = Time.time + (E("te.timeless") ? 15f : 30f);
            Vector3 p = transform.position;
            Circle(p, 30f, 0f, 0f, c => { if (c.TryGetComponent(out EnermyController e)) e.Slow(0f, stop); });
            foreach (HostileProjectile hp in new List<HostileProjectile>(HostileProjectile.Live)) if (hp != null) Destroy(hp.gameObject);
            ShockRing.Spawn(p, 0.5f, 18f, 0.6f, Tint("t.alarm", ClockBlue), 0.35f);
            Flair("t.alarm", p, 8f, ClockBlue);
            SpecialAbilities.SharedFx?.FloatText(p + Vector3.up * 1.5f, Loc.T("시간 정지!"), ClockBlue, 5f, 0f);
            Hostile.Play("chime", 1f, 0.5f);
            if (E("te.timeless")) StartCoroutine(TimelessHeal(stop));
        }
    }

    IEnumerator TimelessHeal(float seconds)
    {
        for (float t = 0f; t < seconds && Alive; t += 0.2f)        // 1.3.8~ 쓰러진 뒤엔 회복하지 않음
        {
            Heal(player.PlayerMaxHealth * 0.02f);
            yield return new WaitForSeconds(0.2f);
        }
    }

    string ClockStack(string key) => key switch
    {
        "t.overclock" => Time.time < overclockUntil ? (overclockUntil - Time.time).ToString("0.0") + "s" : null,
        "t.alarm" => Time.time < alarmReady ? Mathf.CeilToInt(alarmReady - Time.time) + "s" : null,
        "t.stopwatch" => dealtSum > 0.5f ? Mathf.RoundToInt(dealtSum).ToString() : null,
        _ => null,
    };
}
