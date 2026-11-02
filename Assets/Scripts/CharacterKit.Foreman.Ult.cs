using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공사 반장 필살기 「철거 공고」 (2.0.0) — 우클릭 · 게이지 가득 · 누르고 있다 뗌 (기존 영웅과 같은 방식, 밀고 있는 중에도 조준 가능 · 더미는 유지)
//   누르고 있음: 커서 자리(사거리 14칸) 착지 고리 + 같은 방향으로 이어지는 튕김 고리 3 + 굴림길
//   뗌: 0.35초 뒤 쇠공 착지 — 반지름 2.6칸 공격력 400% · 넉백 6 → 같은 방향으로 4칸씩 세 번 튕김 (튕김마다 반지름 2.2칸 250% · 굴림길 폭 1.6칸 60%)
//       총 약 1,330% · 튕긴 자리마다 「공사 중」 바리케이드가 4초 (동시 9개까지 · 오래된 것부터 무너짐)
public partial class CharacterKit
{
    bool fmAiming, fmWrecking;
    LineRenderer fmUltLine, fmUltRing;
    PixelMarks fmPx;           // 2.0.0~ 굴림 길 · 쏟을 범위 도트 점선 (안 그린 프레임엔 저절로 숨음)
    readonly LineRenderer[] fmUltBounce = new LineRenderer[6];       // 3 + 갈래 철거 1 (+ 여유)
    Vector3 fmAimLand;
    Vector2 fmAimDir = Vector2.right;

    public bool FmUltEngaged => fmAiming || fmWrecking;

    void UpdateWreckBall(SkillGauge gauge, bool full)
    {
        if (fmWrecking && !fmAiming) return;
        // 자동 조작: 눌린 프레임에 게이지가 가득이면 바로 발동 (누르고 있기 판정 생략) — 커서 = 가장 가까운 무리
        if (!fmAiming && GameInput.Auto && GameInput.UltDown && full)
        {
            FmAimUpdate();
            FmStartWreck(gauge);
            return;
        }
        if (!fmAiming && GameInput.UltDown && full && Time.timeScale > 0f) { fmAiming = true; Play("hum", 0.4f, 0.7f); }
        if (!fmAiming) return;
        if (Time.timeScale == 0f) { FmHideAim(); return; }
        FmAimUpdate();
        FmDrawAim();
        if (AddedUltShown) DrawUltAim(false, true);
        if (!GameInput.UltUp && GameInput.UltHeld) return;
        FmHideAim();
        CancelUltAim();
        FmStartWreck(gauge);
    }

    void FmAimUpdate()
    {
        Vector3 me = transform.position;
        fmAimLand = ClampRange(me, Mouse, FmWreckRange);
        Vector2 d = (Vector2)(fmAimLand - me);
        fmAimDir = d.sqrMagnitude > 0.25f ? d.normalized : fmBlade;
    }

    void FmStartWreck(SkillGauge gauge)
    {
        Vector3 land = fmAimLand;
        Vector2 dir = fmAimDir;
        StartCoroutine(FmWreckRoutine(land, dir));
        // 갈래 철거 극의: 쇠공이 두 개 (두 번째는 15도 옆 · 피해 70%)
        if (FmOn(ForemanPaths.Demolish) && Paths.Mastered(ForemanPaths.Demolish))
        {
            Vector3 me = transform.position;
            Vector2 d2 = Rotate2(dir, ForemanPaths.DemolishTwinAngle);
            Vector3 land2 = ClampRange(me, me + (Vector3)(d2 * Vector3.Distance(me, land)), FmWreckRange);
            StartCoroutine(FmWreckRoutine(land2, d2, ForemanPaths.DemolishTwin * Paths.Power(ForemanPaths.Demolish), true));
        }
        KitUltAdd(0.35f, land);
        Spend(gauge);
        if (FmOn(ForemanPaths.Demolish)) gauge.AddSkillPoint(gauge.MaxSkillPoint * (1f - ForemanPaths.DemolishGauge));       // 갈래 철거: 게이지 15% 덜 듦
        PlayerMotion.Of(this).Cast(0.5f);
        Hostile.Play("chime", 0.5f, 0.7f);
        // 쓰는 순간 말풍선 「아 지금 하잖아요!」 (첫 사용 · 이후 30% 확률, 1.2초)
        if (!fmShoutDone || Random.value < 0.3f)
        {
            fmShoutDone = true;
            SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 2.2f, Loc.T("아 지금 하잖아요!"), Color.white, 5f, 0f, 1.2f);
        }
    }
    bool fmShoutDone;

    void FmDrawAim()
    {
        Color hero = HeroAim.ColorOf(HeroAimKind.Foreman);
        float pulse = 0.55f + 0.3f * Mathf.Sin(Time.time * 14f);
        if (fmUltRing == null) { fmUltRing = Hostile.NewLine("WreckLand", hero, 0.12f, 25); fmUltRing.loop = true; }
        Color c = new Color(hero.r, hero.g, hero.b, pulse);
        fmUltRing.enabled = true;
        Hostile.SetArc(fmUltRing, fmAimLand, 2.6f, 0f, 360f);
        fmUltRing.startColor = fmUltRing.endColor = c;
        fmUltRing.startWidth = fmUltRing.endWidth = 0.14f;
        HeroAim.Mark(fmUltRing, HeroAimKind.Foreman);
        int bounces = FmBounceCount;
        // 굴림길: 플레이어 → 착지 → 튕김마다 (판정과 같은 점들) · 2.0.0~ 얇은 단색 꺾은선 → 도트 점선
        //   플레이어 → 착지는 곧은 점선 · 튕김 사이는 작은 포물선 · 마지막에 화살촉 (안 그린 프레임엔 저절로 숨음)
        PixelMarks pm = PixelMarks.Ensure(ref fmPx, this);
        Color pc = new Color(hero.r, hero.g, hero.b, Mathf.Max(0.55f, pulse * 0.9f));
        pm.DotPath(transform.position, fmAimLand, 0f, new Color(pc.r, pc.g, pc.b, pc.a * 0.7f), 0.6f, false, default, true);
        Vector3 prev = fmAimLand;
        for (int i = 0; i < bounces; i++)
        {
            Vector3 nx = Hostile.ClampArena(fmAimLand + (Vector3)(fmAimDir * 4f * (i + 1)));
            pm.DotPath(prev, nx, 0.9f, pc, 0.55f, i == bounces - 1);
            prev = nx;
        }
        for (int i = 0; i < fmUltBounce.Length; i++)
        {
            if (fmUltBounce[i] == null) { fmUltBounce[i] = Hostile.NewLine("WreckBounce" + i, hero, 0.1f, 25); fmUltBounce[i].loop = true; }
            LineRenderer lr = fmUltBounce[i];
            bool on = i < bounces;
            lr.enabled = on;
            if (!on) continue;
            Hostile.SetArc(lr, Hostile.ClampArena(fmAimLand + (Vector3)(fmAimDir * 4f * (i + 1))), 2.2f, 0f, 360f);
            lr.startColor = lr.endColor = new Color(hero.r, hero.g, hero.b, pulse * 0.7f);
            lr.startWidth = lr.endWidth = 0.1f;
            HeroAim.Mark(lr, HeroAimKind.Foreman, HeroAimFx.None);
        }
    }

    void FmHideAim()
    {
        fmAiming = false;
        if (fmUltLine != null) fmUltLine.enabled = false;
        if (fmUltRing != null) fmUltRing.enabled = false;
        for (int i = 0; i < fmUltBounce.Length; i++) if (fmUltBounce[i] != null) fmUltBounce[i].enabled = false;
    }

    int FmBounceCount => Mathf.Min(fmUltBounce.Length, 3 + SignatureSkills.FmBounceBonus() + (FmOn(ForemanPaths.Demolish) ? ForemanPaths.DemolishBounces - 3 : 0));

    // 쇠공: 착지 → 튕김 세 번 (고유 스킬 크레인은 튕길 때마다 둘레 적을 끌어당김)
    IEnumerator FmWreckRoutine(Vector3 land, Vector2 dir, float mul = 1f, bool twin = false)
    {
        fmWrecking = true;
        try
        {
            float baseDmg = Damage * UltMul * mul;
            land = Hostile.ClampArena(land);
            Friendly.Circle(land, 2.6f, 0.35f, FmYellow);
            FxAnim ball = Fx.Play(FFx("fx_wreckball", "fx_explosion"), land + Vector3.up * 8f, 2.4f, Color.white, 14f, 0f, 22, true, 1.6f);
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                if (ball != null) ball.transform.position = Vector3.Lerp(land + Vector3.up * 8f, land, t / 0.35f);
                yield return null;
            }
            // 첫 착지
            DamageCircle(land, 2.6f, baseDmg * 4f, 6f);
            Fx.Spawn("fx_explosion", land, 6f, new Color(1f, 0.85f, 0.4f), 18f);
            Fx.Spawn("fx_shock", land, 6f, new Color(1f, 0.9f, 0.5f, 0.8f), 20f);
            Hostile.Shake(0.3f);
            Play("boom", 0.7f, 0.8f);
            if (!twin) Impact.Hit(ImpactTier.Ult, land, dir);
            SignatureSkills.FmUlt();
            RunCounters.Both("b.wreck");
            Vector3 prev = land;
            int bounces = FmBounceCount;
            int bossBounces = 0;
            for (int k = 1; k <= bounces; k++)
            {
                if (player == null) break;
                Vector3 to = Hostile.ClampArena(land + (Vector3)(dir * 4f * k));
                if (Hostile.IsWall(to)) to = prev;
                // 굴림길 폭 1.6칸 공격력 60% (적마다 구간당 한 번)
                FmLineDamage(prev, to, 0.8f, baseDmg * 0.6f, dir);
                for (float t = 0f; t < 0.28f; t += Time.deltaTime)
                {
                    if (ball != null) ball.transform.position = Vector3.Lerp(prev, to, t / 0.28f) + Vector3.up * Mathf.Sin(t / 0.28f * Mathf.PI) * 1.5f;
                    yield return null;
                }
                // 튕김: 반지름 2.2칸 공격력 250%
                SignatureSkills.FmBounce(to);
                DamageCircle(to, 2.2f, baseDmg * 2.5f, 3f);
                Fx.Spawn("fx_explosion", to, 4.4f, new Color(1f, 0.85f, 0.4f), 18f);
                Play("thump", 0.6f, 0.8f);
                Hostile.Shake(0.15f);
                FmPathBossBounce(to, baseDmg, ref bossBounces);       // 갈래 철거 공명 II: 보스에 맞으면 한 번 더 튕김
                Vector2 along = new Vector2(-dir.y, dir.x);
                FmAddFence(to, along, 3.2f, FmFenceLife * SignatureSkills.FmFenceLifeMul());
                prev = to;
            }
            // 통행 금지(진화): 바리케이드 사이 중간에 두 개 더
            if (SignatureSkills.FmEvo("be.closed"))
            {
                Vector2 al = new Vector2(-dir.y, dir.x);
                FmAddFence(Hostile.ClampArena(land + (Vector3)(dir * 2f)), al, 3.2f, FmFenceLife);
                FmAddFence(Hostile.ClampArena(land + (Vector3)(dir * 6f)), al, 3.2f, FmFenceLife);
            }
            if (ball != null) Destroy(ball.gameObject);
        }
        finally { fmWrecking = false; }
    }

    // 선분 근처(폭 width) 적에게 한 번씩
    void FmLineDamage(Vector3 a, Vector3 b, float width, float dmg, Vector2 dir)
    {
        fmTmp.Clear();
        Specials.Overlap((a + b) * 0.5f, Vector3.Distance(a, b) * 0.5f + width + 1f, fmHits);
        List<GameObject> targets = new List<GameObject>();
        for (int i = 0; i < fmHits.Count; i++)
        {
            Collider2D c = fmHits[i];
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            if (Hostile.DistanceToSegment(c.transform.position, a, b) > width + FmBodyR(c) * 0.5f) continue;
            if (c.TryGetComponent(out EnermyController e) && (e.holder != null || e.IsDead)) continue;
            targets.Add(c.gameObject);
        }
        foreach (GameObject g in targets) if (g != null) Specials.Damage(g, dmg, dir, 1f);
    }
}
