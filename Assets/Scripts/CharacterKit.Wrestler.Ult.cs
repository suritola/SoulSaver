using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 씨름꾼 필살기 「천하장사」 (2.0.0) — 우클릭 · 게이지 가득 · 누르고 있다 뗌 (기존 영웅과 같은 방식)
//   누르고 있음: 14칸 안에서 가장 무거운 적(보스 > 중간 보스 > 무거움 > …)에 황소 뿔 표식 · 충격파 원(7) · 남을 모래판 원(5.5)
//   뗌: 0.35초 뛰어듦(무적) → 들어 올림 0.3초(보스는 들썩 연출만) → 메침: 대상 공격력 800% · 둘레 7칸 충격파 300% → 가장자리 50% · 바깥으로 3.5칸 밀침
//       메친 자리에 모래판 10초 · 밀려 경계를 넘은 잡몹 · 큰 적은 링아웃
//   14칸 안에 적이 없으면 제자리에서 메침 (충격파 · 모래판만)
public partial class CharacterKit
{
    bool wrAiming, wrSlamming;
    LineRenderer wrUltLine, wrUltRing, wrUltRing2;
    Transform wrUltTarget;
    const float WrUltRange = 14f;

    public bool WrUltEngaged => wrAiming || wrSlamming;

    // 가장 무거운 적 (같은 무게면 커서에 가까운 쪽 · 커서가 다른 큰 적 가까이면 그 적)
    Transform WrUltPick()
    {
        Vector3 me = transform.position, cursor = Mouse;
        Transform best = null;
        int bestRank = -1;
        float bestCd = float.MaxValue;
        bool nearCursor = false;
        // 커서 2.5칸 안에 큰 적이 있으면 그쪽만 대상
        foreach (Collider2D c in Specials.Overlap(me, WrUltRange, wrHits))
        {
            if (c == null) continue;
            if (!c.CompareTag("boss") && !c.CompareTag("enermy")) continue;
            if (c.TryGetComponent(out EnermyController ec) && (ec.IsDead || ec.holder != null || ec.swarm != null)) continue;
            if (((Vector2)(c.transform.position - cursor)).magnitude < 2.5f) { nearCursor = true; break; }
        }
        foreach (Collider2D c in Specials.Overlap(me, WrUltRange, wrHits))
        {
            if (c == null) continue;
            bool boss = c.CompareTag("boss");
            if (!boss && !c.CompareTag("enermy")) continue;
            int rank = 4;
            if (!boss)
            {
                if (!c.TryGetComponent(out EnermyController ec) || ec.IsDead || ec.holder != null || ec.swarm != null) continue;
                rank = WrTier(ec);        // 중간 보스 3 · 무거움 2 · 보통 1 · 가벼움 0
            }
            float cd = ((Vector2)(c.transform.position - cursor)).magnitude;
            if (nearCursor && cd >= 2.5f) continue;
            if (rank > bestRank || (rank == bestRank && cd < bestCd)) { bestRank = rank; bestCd = cd; best = c.transform; }
        }
        return best;
    }

    void UpdateGrandSlam(SkillGauge gauge, bool full)
    {
        if (wrSlamming) { WrHideUltAim(); return; }
        // 자동 조작: 눌린 프레임에 게이지가 가득이면 바로 발동 (누르고 있기 판정 생략)
        if (!wrAiming && GameInput.Auto && GameInput.UltDown && full)
        {
            StartCoroutine(WrGrandSlam(WrUltPick()));
            KitUltAdd(0.5f);
            Spend(gauge);
            return;
        }
        if (!wrAiming && GameInput.UltDown && full && Time.timeScale > 0f) { wrAiming = true; Play("hum", 0.4f, 0.6f); }
        if (!wrAiming) return;
        if (Time.timeScale == 0f) { WrHideUltAim(); return; }
        wrUltTarget = WrUltPick();
        WrDrawUltAim();
        if (AddedUltShown) DrawUltAim(false, true);
        if (!GameInput.UltUp && GameInput.UltHeld) return;
        Transform tg = wrUltTarget;
        WrHideUltAim();
        CancelUltAim();
        wrUltTarget = null;
        StartCoroutine(WrGrandSlam(tg));
        KitUltAdd(0.5f);
        Spend(gauge);
    }

    void WrDrawUltAim()
    {
        Color hero = HeroAim.ColorOf(HeroAimKind.Wrestler);
        float pulse = 0.55f + 0.3f * Mathf.Sin(Time.time * 14f);
        if (wrUltRing == null) { wrUltRing = Hostile.NewLine("SlamWave", hero, 0.12f, 25); wrUltRing.loop = true; }
        if (wrUltRing2 == null) { wrUltRing2 = Hostile.NewLine("SlamRing", hero, 0.12f, 25); wrUltRing2.loop = true; }
        Vector3 me = transform.position;
        Vector3 at = wrUltTarget != null ? wrUltTarget.position : me;
        float ringR = (WrestlerPaths.On(WrestlerPaths.Ring) ? WrestlerPaths.RingRadiusSeed : WrRingRadius) * (SignatureSkills.IsEvo("wre.dohyo") ? 1.4f : 1f);
        // 뛰어갈 길: 2.0.0~ 곧은 단색 선 → 높이 뛰는 도트 점 포물선 + 끝 화살촉 (안 그린 프레임엔 저절로 숨음)
        if (wrUltTarget != null)
        {
            float jump = Vector2.Distance(me, at);
            PixelMarks.Ensure(ref wrPx, this).DotPath(me, at, Mathf.Min(2.4f, 0.35f * jump), new Color(hero.r, hero.g, hero.b, Mathf.Max(0.6f, pulse + 0.1f)), 0.55f, true, default, false, 2f);
        }
        // 착지 충격파 원(7) — 황소 뿔 표식이 가운데에 (HeroAim 장식) · 남을 모래판 원(5.5)
        wrUltRing.enabled = true;
        Hostile.SetArc(wrUltRing, at, WrSlamRadius, 0f, 360f);
        wrUltRing.startColor = wrUltRing.endColor = new Color(hero.r, hero.g, hero.b, pulse);
        wrUltRing.startWidth = wrUltRing.endWidth = 0.14f;
        HeroAim.Mark(wrUltRing, HeroAimKind.Wrestler);
        wrUltRing2.enabled = true;
        Hostile.SetArc(wrUltRing2, at, ringR, 0f, 360f);
        wrUltRing2.startColor = wrUltRing2.endColor = new Color(WrSand.r, WrSand.g, WrSand.b, 0.7f);
        wrUltRing2.startWidth = wrUltRing2.endWidth = 0.1f;
        HeroAim.Mark(wrUltRing2, HeroAimKind.Wrestler, HeroAimFx.None);
    }

    void WrHideUltAim()
    {
        wrAiming = false;
        if (wrUltLine != null) wrUltLine.enabled = false;
        if (wrUltRing != null) wrUltRing.enabled = false;
        if (wrUltRing2 != null) wrUltRing2.enabled = false;
    }

    IEnumerator WrGrandSlam(Transform tg)
    {
        wrSlamming = true;
        try
        {
            if (player == null) yield break;
            WrReleaseCarried(true);            // 들고 있던 적은 놓고 시작
            Vector3 from = transform.position;
            EnermyController e = tg != null ? tg.GetComponent<EnermyController>() : null;
            bool boss = tg != null && e == null;
            Vector3 aimDir = tg != null ? (tg.position - from) : (Vector3)Vector2.right;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector3.right;
            aimDir.Normalize();
            // 뛰어듦 0.35초 (무적: 뛰는 동안 + 착지 뒤 0.3초)
            Paths.Invuln(0.35f + 0.3f);
            Vector3 to = from;
            if (tg != null)
            {
                float stop = Mathf.Max(0f, Vector2.Distance(from, tg.position) - (WrBodyR(tg) + 1.1f));
                to = Hostile.ClampArena(from + aimDir * stop);
                if (Hostile.IsWall(to)) to = from;
            }
            PlayerMotion.Of(this).Leap(0.35f, 2.2f, aimDir);
            Play("whoosh", 0.7f, 0.6f);
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                if (player == null || player.IsDying) yield break;
                float k = t / 0.35f;
                transform.position = Vector3.Lerp(from, to, k * k * (3f - 2f * k));
                yield return null;
            }
            transform.position = to;
            // 들어 올림 0.3초 (보스는 들썩 연출만)
            bool taken = false;
            if (e != null && EnemyHold.TryTake(e, this, true))
            {
                taken = true;
                EnemyHold.Raise(e, body, 2);
                Vector3 grab = e.transform.position;
                for (float t = 0f; t < 0.3f; t += Time.deltaTime)
                {
                    if (e == null || e.IsDead || player == null || player.IsDying) break;
                    e.transform.position = Vector3.Lerp(grab, WrOverhead(WrBodyR(e)), Mathf.Clamp01(t / 0.3f));
                    yield return null;
                }
            }
            else if (boss && tg != null)
            {
                Fx.Spawn(WFx("fx_liftshadow", "fx_sparkle"), tg.position + Vector3.down * 0.4f, 4f, new Color(0f, 0f, 0f, 0.5f), 10f, 0f, 3);
                yield return new WaitForSeconds(0.3f);
            }
            else yield return new WaitForSeconds(0.1f);
            if (player == null || player.IsDying) yield break;
            // 메침
            Vector3 center = tg != null && !boss ? transform.position + (Vector3)Vector2.down * 0.2f : transform.position;
            if (boss && tg != null) center = tg.position + (Vector3)((Vector2)(transform.position - tg.position)).normalized * 1.2f;
            WrSlamAt(center, e != null && taken ? e : null, boss && tg != null ? tg.gameObject : null);      // 2.0.0~ 검토: 기다리는 사이 보스가 사라졌으면 tg 가 파괴된 참조
            // 황소 장사(진화): 천하장사로 든 적이 보스가 아니면 메친 뒤에도 4초 동안 머리 위 방패로 남음
            //   2.0.0~ 검토: WrSlamAt 이 먼저 Release 하므로 다시 잡아야 함 (holder 없이 Raise 하면 그림자 · 정렬을 되돌릴 길이 없어 망가진 채 남았음)
            if (taken && e != null && !e.IsDead && SignatureSkills.IsEvo("wre.giantbull") && EnemyHold.TryTake(e, this, true))
            {
                wrCarried = e;
                wrW = WrWeightOf(e);
                wrGrabPos = e.transform.position;
                wrGrabAt = Time.time;
                wrLiftDone = Time.time + 0.05f;
                wrCarryEnd = Time.time + 4f;
                wrLiftFired = true;
                wrGiantBullHeld = e;
                EnemyHold.Raise(e, body, 2);
            }
            else if (taken && e != null) EnemyHold.Release(e, this);
            yield return new WaitForSeconds(0.3f);
        }
        finally { wrSlamming = false; }
    }

    // 메침: 대상 공격력 800% · 둘레 7칸 충격파 300% → 가장자리 50% · 바깥으로 3.5칸 · 모래판
    void WrSlamAt(Vector3 center, EnermyController held, GameObject bossTarget)
    {
        float baseDmg = Damage * UltMul;
        // 모래판을 먼저 깔아야 밀려 경계를 넘은 적을 링아웃으로 셈
        WrStartRing(center);
        // 대상
        GameObject targetGo = held != null ? held.gameObject : bossTarget;
        if (held != null) { held.transform.position = EnemyHold.FreeSpot(center + Vector3.up * 0.8f); EnemyHold.Release(held, this); }
        if (targetGo != null) Specials.Damage(targetGo, baseDmg * 8f, Vector2.down, 1f);
        // 충격파
        List<EnermyController> push = new List<EnermyController>();
        List<Vector3> before = new List<Vector3>();
        using (var ovl = PhysHits.Circle(center, WrSlamRadius, true)) foreach (Collider2D c in ovl)
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            if (targetGo != null && c.gameObject == targetGo) continue;
            float d = Vector2.Distance(center, c.transform.position);
            float mul = Mathf.Lerp(3f, 0.5f, Mathf.Clamp01(d / WrSlamRadius));
            Vector2 away = ((Vector2)(c.transform.position - center)).normalized;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitCircle.normalized;
            EnermyController ec = c.GetComponent<EnermyController>();
            if (ec != null && (ec.IsDead || ec.holder != null)) continue;
            Specials.Damage(c.gameObject, baseDmg * mul, away, 0f);
            if (ec != null && !ec.IsDead) { push.Add(ec); before.Add(ec.transform.position); }
        }
        // 모두 바깥으로 3.5칸 밀침 (경계를 넘으면 링아웃)
        for (int i = 0; i < push.Count; i++)
        {
            EnermyController ec = push[i];
            if (ec == null || ec.IsDead) continue;
            Vector3 b0 = before[i];
            Vector2 away = ((Vector2)(b0 - center)).normalized;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitCircle.normalized;
            Vector3 np = Hostile.ClampArena(b0 + (Vector3)(away * 3.5f));
            if (!Hostile.IsWall(np)) ec.transform.position = np;
            bool wasIn = ((Vector2)(b0 - center)).sqrMagnitude <= wrRingR * wrRingR;
            if (wasIn && !WrInRing(ec.transform.position)) WrRingOut(ec, ec.transform.position);
        }
        Fx.Spawn(WFx("fx_sandwave", "fx_shock"), center, WrSlamRadius * 2.2f, new Color(WrSand.r, WrSand.g, WrSand.b, 0.9f), 18f);
        Fx.Spawn(WFx("fx_sandpuff", "fx_explosion"), center, 5f, WrSand, 18f);
        Hostile.Shake(0.35f);
        Impact.Hit(ImpactTier.Ult, center, Vector2.up);
        SignatureSkills.WrUlt();
    }
}
