using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 연금술사 갈래 (2026-10-19 · docs/review/2026-10-19-빌드-시너지-기획.md 3-5): 평타 플라스크 · 조수 플라스크 · 갈래 자폭
//   갈래 규칙 판에서는 평타를 이 파일의 PathThrowReagent 가 던짐 (AlchemistPaths.KitAttack → AlchPathAttack, CharacterKit.cs 는 그대로)
//   원래 ThrowReagent(CharacterKit.cs)와 같은 흐름에 갈래 규칙만 끼움:
//     늪 — 폭발 피해 −30% · 모든 플라스크가 웅덩이 / 빙결 — 빙결 시약이 두 번에 한 번 · 얼린 적 산산조각 · 화염 −40%
//     연쇄 폭발 — 불안정도 2배 · 불안정 폭발 범위 1.5배 · 자폭 피해 2배 / 원소 촉매 — 불안정 플라스크 없음
//   ※ ThrowReagent 를 고치면 PathThrowReagent 도 같이 맞출 것 (같은 줄 순서로 써 둠)
// 효과 상태(웅덩이 · 얼린 적 · 조수)는 Paths/Paths.Alchemist.cs (AlchemistPaths) 가 들고 있음
public partial class CharacterKit
{
    int alchLastKind = -1;          // 마지막으로 던진 시약 (조수 공방: 조수가 따라 던짐)
    bool alchFrostAlt;              // 빙결 갈래: 빙결 다음에 화염 · 산성을 번갈아
    float alchBlastBusyUntil;       // 갈래 자폭이 터지는 중 (끝나는 시각 — 끊겨도 저절로 풀림)
    float alchBlastInvulnAt;        // 자폭 무적(공명 II)을 다시 줄 수 있는 시각
    float alchHomunculusRetryAt;    // 조수 공방: 호문쿨루스를 다시 부를 수 있는 시각
    static readonly Color AlchUnstableTint = new Color(0.85f, 0.4f, 1f);

    public int AlchLastKind => alchLastKind;
    public float AlchDamage => Damage;
    public GameObject AlchHomunculus => homunculus;
    public bool AlchAlive => player != null && !player.IsDying;

    // 조수 공방: 처음부터 호문쿨루스 (호문쿨루스 카드와 같은 조수 · 이미 있으면 그대로)
    public void AlchEnsureHomunculus()
    {
        if (Id != CharacterId.Alchemist || homunculus != null || Time.time < alchHomunculusRetryAt || !AlchAlive) return;
        alchHomunculusRetryAt = Time.time + 2f;          // 그림이 없어 못 만들면 2초마다만 다시
        CardPicked(2);
    }

    // ================================================================= 평타 (CharacterKit.Attack 의 연금술사 분기와 같음)
    public void AlchPathAttack(Vector3 start, float dmg, int shots)
    {
        Vector3 target = Mouse;
        for (int i = 0; i < shots; i++)
        {
            Vector3 land = ClampRange(start, target, def.range) + (i == 0 ? Vector3.zero : (Vector3)(Random.insideUnitCircle * 1.6f));
            PathThrowReagent(start, land, dmg, i == 0 && WeakHit.InAim ? 1.6f : 0f);
        }
        Play("glassclink", 0.6f, Random.Range(0.9f, 1.15f));
        Play("whoosh", 0.25f, 1.5f);
    }

    // 다음 시약: 빙결 갈래면 화염 → 빙결 → 산성 → 빙결 → 화염 … (빙결이 두 번에 한 번)
    int NextPathReagent(int kind)
    {
        if (!Paths.Has(AlchemistPaths.Frost)) return (kind + 1) % 3;
        if (kind != 1) return 1;
        alchFrostAlt = !alchFrostAlt;
        return alchFrostAlt ? 2 : 0;
    }

    void PathThrowReagent(Vector3 from, Vector3 land, float dmg, float weakMul)
    {
        int kind = reagent;
        reagent = NextPathReagent(kind);
        alchLastKind = kind;
        bool swamp = Paths.Has(AlchemistPaths.Swamp), frost = Paths.Has(AlchemistPaths.Frost);
        bool chain = Paths.Has(AlchemistPaths.Chain), catalyst = Paths.Has(AlchemistPaths.Catalyst);
        bool unstable = Special != null ? (Special.KitStoneActive || Random.value < Special.KitUnstableChance) : Random.value < 0.15f;
        if (ForceUnstable()) unstable = true;
        if (catalyst) unstable = false;                 // 정밀 조제의 대가: 불안정 플라스크가 나오지 않음
        float instR = InstRadiusMul, instD = InstDamageMul;
        AlchAddInstability(unstable ? 12f : 5f);
        Color tint = unstable ? AlchUnstableTint : ReagentColors[kind];
        float conc = SignatureSkills.FlaskThrow();
        FlaskLob.Throw(from, land, 0.4f, (unstable ? 1.05f : 0.8f) * (conc > 1f ? 1.35f : 1f), tint, (p) =>
        {
            if (this == null) return;                   // 판이 끝나 플레이어가 사라진 뒤 떨어진 플라스크
            AlchemistPaths ap = AlchemistPaths.Live;
            float r = 1.65f * blastMul * (unstable ? 1.6f : 1f) * FlaskRadiusMul * conc * instR;
            float hit = dmg * 1.2f * (unstable ? 1.6f : 1f) * FlaskDamageMul * conc * instD;
            if (unstable && chain) r *= AlchemistPaths.ChainBlastRadius;              // 불안정 연구: 불안정 폭발 1.5배 크게
            if (swamp) hit *= 1f - AlchemistPaths.SwampBlastCut;                       // 늪지기의 대가: 폭발 피해 −30%
            if (frost && kind == 0 && !unstable) hit *= 1f - AlchemistPaths.FrostFireCut;     // 빙결 전문의 대가: 화염 −40%
            FlaskExtras(p, r, hit, 0);
            Play("shatter", 0.55f, Random.Range(0.9f, 1.2f));
            ap?.Shatter(p, r, hit);                     // 빙결: 이 플라스크가 얼리기 전에 얼어 있던 적만
            if (unstable)
            {
                RunCounters.Both("unstable");
                using var caughtHits = PhysHits.Circle(p, r);
                List<Collider2D> caught = caughtHits.List;
                DamageCircle(p, r, hit, 2.2f, weakMul);
                foreach (Collider2D c in caught)
                {
                    if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                    EnermyController e = c.GetComponent<EnermyController>();
                    if (e != null && e.IsDead) ChainPop(c.transform.position, hit);
                    else if (Special != null && Special.KitUnstableBurns) Burn.Apply(c.gameObject, hit * 0.3f, 3f);
                }
                Fx.Spawn("fx_alchemyblast", p, r * 2.4f, SkinFx.Tint(new Color(0.9f, 0.55f, 1f)), 16f, 0f, AttackLook.AreaOrder - 2);
                Fx.Spawn("fx_shock", p, r * 2.2f, new Color(0.85f, 0.5f, 1f, 0.8f), 20f);
                Hostile.Shake(0.18f);
                Play("boom", 0.7f, 1.1f);
                int dead = DeadCount(caught);
                // 연쇄 폭발 공명 I: 불안정 플라스크로 처치할 때마다 불안정도 −10 (한 번에 최대 −30)
                if (chain && Paths.Res(AlchemistPaths.Chain) >= 1 && dead > 0) Instability = Mathf.Max(0f, Instability - 10f * Mathf.Min(3, dead));
                SignatureSkills.Flask(p, r, kind, true, new List<Collider2D>(caught), dead, conc > 1f);
                ap?.AfterBlast(p, r, hit, kind, true);
                return;
            }
            using var insideHits = PhysHits.Circle(p, r);
            List<Collider2D> inside = insideHits.List;
            DamageCircle(p, r, hit, 1.2f, weakMul);
            Fx.Spawn("fx_alchemyblast", p, r * 2.2f, SkinFx.Tint(ReagentColors[kind]), 18f, 0f, AttackLook.AreaOrder - 2);
            foreach (Collider2D c in inside)
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && e.IsDead) { ChainPop(c.transform.position, hit); continue; }
                if (kind == 0) Burn.Apply(c.gameObject, hit * 0.3f, 2f);
                if (kind == 1)
                {
                    float freeze = card[1] > 0 ? 0.5f + 0.3f * card[1] : AlchemistPaths.FrostFreeze;
                    if (frost)
                    {
                        // 빙결 전문: 급속 냉동이 없어도 잠깐 얼림 (보스는 얼지 않고 「얼어붙은 것으로」 셈 · 산산조각 대상)
                        if (e != null) e.Slow(0f, freeze);
                        ap?.Freeze(c.gameObject, e != null ? freeze : AlchemistPaths.BossChill);
                    }
                    else if (e != null)
                    {
                        e.Slow(card[1] > 0 ? 0f : 0.5f, card[1] > 0 ? freeze : 1.5f);
                        if (card[1] > 0) ap?.Freeze(c.gameObject, freeze);
                    }
                }
                if (kind == 1 && card[5] > 0)
                {
                    Burn burn = c.GetComponent<Burn>();
                    if (burn != null)
                    {
                        Destroy(burn);
                        DamageCircle(c.transform.position, 1.8f, Damage * (0.8f + 0.7f * card[5]), 1.5f);
                        Fx.Spawn("fx_cloud", c.transform.position, 3.6f, new Color(1f, 1f, 1f, 0.8f), 16f);
                        Play("hiss", 0.5f, 1.4f);
                    }
                }
            }
            SignatureSkills.Flask(p, r, kind, false, new List<Collider2D>(inside), DeadCount(inside), conc > 1f);
            if (kind == 0) Play("ignite", 0.3f, 1.3f);
            if (kind == 1) Play("shimmer", 0.35f, 1.5f);
            if (kind == 2)
            {
                float acidTime = 2.5f + card[6];
                // 늪: 산성 웅덩이도 갈래 웅덩이로 (공명 · 극의가 같이 붙고 겹치면 합쳐짐 · 개수 상한)
                if (swamp && ap != null) acidTime = ap.Puddle(p, r * 0.8f, acidTime, hit * 0.25f, false);
                else if (Special != null) Special.SpawnZone(p, r * 0.8f, acidTime, hit * 0.25f, new Color(0.45f, 1f, 0.3f, 0.6f));
                if (card[6] > 0) StartCoroutine(StickyAcid(p, r * 0.8f, acidTime));
                Play("fizz", 0.45f, 1f);
            }
            ap?.AfterBlast(p, r, hit, kind, false);
        });
    }

    // ================================================================= 조수 플라스크가 떨어짐
    // own: 갈래 조수(AlchemistPaths 가 띄운 조수)의 플라스크 — 피해 · 그림까지 여기서 / false: 호문쿨루스(CharacterKit.Homunculus)가 이미 터뜨린 뒤 덧붙는 것만
    public void AlchAideLand(Vector3 q, float dmg, bool own)
    {
        if (this == null || !Paths.Active || !AlchAlive) return;
        if (own)
        {
            DamageCircle(q, 1.4f, dmg, 1f);
            SignatureSkills.HomunculusLand(q, dmg);
            Fx.Spawn("fx_alchemyblast", q, 3f, AlchemistPaths.AideTint, 18f);
            Play("shatter", 0.3f, 1.4f);
        }
        AlchemistPaths.Live?.AideLanded(q, dmg);
    }

    // ================================================================= 연쇄 폭발: 불안정도 2배 · 갈래 자폭
    // 갈래가 아니면 원래 AddInstability (CharacterKit.Rules) 그대로
    void AlchAddInstability(float n)
    {
        if (!Paths.Has(AlchemistPaths.Chain)) { AddInstability(n); return; }
        Instability = Mathf.Min(InstabilityMax, Instability + n * 2f);
        instabilityRestAt = Time.time + 0.8f;
        if (Instability >= InstabilityMax && !selfBlastQueued && Time.time >= alchBlastBusyUntil) StartCoroutine(AlchSelfBlast());
    }

    // 원래 자폭(CharacterKit.Rules.SelfBlast)과 같은 흐름 · 상태는 끝나는 시각(alchBlastBusyUntil) 하나 — 끊겨도 2초 뒤 저절로 풀림
    //   입문: 내 피해 8% → 16% · 주변 피해 공격력 500% → 400% (자폭이 두 배 잦으므로)
    //   공명 II: 반지름 6 → 9 · 공격력 +300% · 0.5초 무적 (4초에 한 번) / 극의: 내 피해 0
    IEnumerator AlchSelfBlast()
    {
        alchBlastBusyUntil = Time.time + 2f;
        SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 1.6f, Loc.T("폭주!"), new Color(0.9f, 0.45f, 1f), 5f, 0f);
        Play("bubble", 0.8f, 1.6f);
        for (int i = 0; i < 3; i++)
        {
            Fx.Spawn("fx_sparkle", transform.position + (Vector3)(Random.insideUnitCircle * 0.8f), 1.2f, new Color(0.9f, 0.5f, 1f), 24f);
            yield return new WaitForSeconds(0.15f);
        }
        Instability = 0f;
        if (!AlchAlive) yield break;                    // 쓰러지는 중이면 터지지 않음
        bool big = Paths.Res(AlchemistPaths.Chain) >= 2;
        Vector3 p = transform.position;
        float r = big ? 9f : 6f;
        float d = Damage * AlchemistPaths.SelfBlastMul;
        if (big) d += Damage * AlchemistPaths.BigBlastBonus * Paths.Power(AlchemistPaths.Chain);
        DamageCircle(p, r, d, big ? 5f : 4f);
        Fx.Spawn("fx_alchemyblast", p, r * 2.35f, SkinFx.Tint(new Color(0.9f, 0.55f, 1f)), 14f);
        Fx.Spawn("fx_shock", p, r * 2.2f, new Color(0.85f, 0.5f, 1f, 0.85f), 18f);
        ShockRing.Spawn(p, 0.5f, r, 0.45f, new Color(0.9f, 0.55f, 1f, 0.9f), 0.3f);
        if (big) ShockRing.Spawn(p, 0.3f, r * 0.6f, 0.35f, new Color(1f, 0.85f, 1f, 0.8f), 0.22f);
        Hostile.Shake(big ? 0.45f : 0.35f);
        Play("bigboom", 0.9f, big ? 0.85f : 1f);
        if (!Paths.Mastered(AlchemistPaths.Chain) && !player.IsInvincible)
        {
            player.PlayerHealth = Mathf.Max(1f, player.PlayerHealth - player.PlayerMaxHealth * AlchemistPaths.SelfBlastHurt);
            DamageFlash.Show(0.5f);
        }
        // 내 피해를 먼저 받은 뒤에 무적 (무적이 자폭 피해를 지우지 않게)
        if (big && Time.time >= alchBlastInvulnAt)
        {
            alchBlastInvulnAt = Time.time + 4f;
            Paths.Invuln(AlchemistPaths.BlastInvuln);
        }
    }
}
