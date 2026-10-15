using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 시계공 (1.0.2): 톱니바퀴를 던져 싸우는 장인
//   평타 · 톱니 투척: 회전하는 톱니. 맞힐 때마다 시계 눈금이 하나씩 차고 (발밑 고리), 다 차면 다음 톱니가 「정각」 —
//                     크고 금빛인 톱니가 모든 적을 꿰뚫고 처음 맞힌 자리에서 터짐
//   우클릭 · 되감기: 누르고 있으면 3초 전의 내 모습이 보이고, 떼면 그 자리로 돌아감.
//                   지나온 길의 적을 베고, 그동안 잃은 체력의 절반을 되찾고, 잃은 체력의 세 배를 둘레 적에게 되갚음
// 레벨업 카드 (card): 0 튕기는 톱니 · 1 빠른 태엽 · 2 시계추 · 3 남은 시각 · 5 정각의 종 · 6 녹슨 톱니
// 형태: 뻐꾸기 시계 · 모래시계 · 괘종시계 / 강화: 정밀 톱니 · 이중 톱니 · 시간 균열
public partial class CharacterKit
{
    static readonly Color Brass = new Color(1f, 0.78f, 0.35f);
    static readonly Color TimeBlue = new Color(0.55f, 0.85f, 1f, 0.9f);

    int clockTicks;
    float tickSoundAt;
    int gearThrows;
    LineRenderer clockRing, clockDial;
    public int ClockNeed => Mathf.Max(4, 12 - 2 * card[1] - (augment == AugPrecise ? 2 : 0));
    public bool ClockReady => clockTicks >= ClockNeed;
    public void AddClockTicks(float n) { clockTickFrac += n; while (clockTickFrac >= 1f) { clockTickFrac -= 1f; if (clockTicks < ClockNeed) clockTicks++; } }
    float clockTickFrac;

    void ClockAttack(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        bool big = ClockReady;
        if (big) clockTicks = 0;
        gearThrows++;
        float d = dmg * (form == SpecialAbilities.KitGrandfather ? 1.4f : 1f);
        foreach (Vector2 v in Spread(dir, shots + (augment == AugTwinGear ? 1 : 0), 10f))
            Gear(start, v, d * (augment == AugTwinGear && shots > 0 ? 0.85f : 1f), card[0], big);
        SignatureSkills.GearThrown(start, dir, d, big);
        if (card[2] > 0 && gearThrows % 5 == 0) StartCoroutine(Pendulum(dir));
        if (big)
        {
            Play("chime", 0.8f, 0.9f);
            SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 1.6f, Loc.T("정각!"), Brass, 4.5f, 0f);
        }
        Play("ratchet", 0.5f, big ? 0.7f : 1.2f);
        Play("clank", 0.25f, big ? 0.8f : 1.5f);
        Play("whoosh", 0.3f, 1.3f);
    }

    // 톱니 하나: 맞히면 눈금 · 튕기는 톱니 · 녹슨 톱니 · 정각 폭발
    Bullet Gear(Vector3 start, Vector2 dir, float dmg, int bounces, bool big)
    {
        bool grand = form == SpecialAbilities.KitGrandfather;
        float size = (big ? 1.9f : 1.05f) * (grand ? 1.3f : 1f);
        float speed = (big ? 30f : 38f) * (grand ? 0.8f : 1f);
        Bullet b = Projectile(start, dir, dmg * (big ? 3f * SignatureSkills.BigGearMul() : 1f), big ? 9999 : player.pene, speed, 16f, "fx_shuriken", size, big ? Brass : new Color(0.85f, 0.8f, 0.7f), true);
        if (b == null) return null;
        if (big) b.hitOnce = new HashSet<int>();
        bool exploded = false;
        b.onHitEnemy += (s, c) =>
        {
            if (c == null) return;
            if (!big) { AddClockTicks(1f); if (Time.time >= tickSoundAt) { tickSoundAt = Time.time + 0.08f; Play("tick", 0.35f, 0.9f + 0.05f * clockTicks); } }
            if (card[6] > 0 && c.TryGetComponent(out EnermyController e)) e.Slow(1f - (0.05f + 0.1f * card[6]), 2f);
            if (form == SpecialAbilities.KitHourglass && c.TryGetComponent(out EnermyController e2)) e2.Slow(0.5f, 1f);
            SignatureSkills.GearHit(c, s.damage, big);
            if (big && !exploded)
            {
                exploded = true;
                Vector3 p = c.transform.position;
                DamageCircle(p, 2.6f, s.damage * 0.6f, 2f);
                Fx.Spawn("fx_explosion", p, 5f, Brass, 18f);
                ShockRing.Spawn(p, 0.4f, 3.2f, 0.3f, Brass, 0.2f);
                if (card[5] > 0)
                {
                    // 정각의 종: 멈추는 범위(4칸)를 종소리 고리로 보여 줌 (예전엔 범위가 안 보였음)
                    ShockRing.Spawn(p, 0.5f, 4f, 0.45f, new Color(1f, 0.95f, 0.75f, 0.9f), 0.22f);
                    foreach (Collider2D col in new List<Collider2D>(Specials.Overlap(p, 4f, clockHits)))
                        if (col != null && col.TryGetComponent(out EnermyController ce)) ce.Slow(0f, 0.2f + 0.3f * card[5]);
                }
                if (augment == AugRift) StartCoroutine(TimeRift(p));
                if (form == SpecialAbilities.KitCuckoo) StartCoroutine(Cuckoos(p, 3));
                SignatureSkills.BigGear(p);
                Play("bell", 0.7f, 1f);
            }
        };
        if (bounces > 0)
        {
            bool bounced = false;
            b.onHitEnemy += (s, c) =>
            {
                if (bounced || c == null) return;
                Transform next = NearestOther(c.transform.position, 9f, c.transform, s.Direction, 360f);
                if (next == null) return;
                bounced = true;
                SignatureSkills.GearBounced();
                Bullet r = Gear(c.transform.position, (next.position - c.transform.position).normalized, s.damage * 0.85f * SignatureSkills.BounceMul(), bounces - 1, false);
                EnermyController e = c.GetComponent<EnermyController>();
                if (r != null && e != null) r.hitOnce = new HashSet<int> { e.GetInstanceID() };
            };
        }
        return b;
    }
    static readonly List<Collider2D> clockHits = new List<Collider2D>(64);

    // 시계추 (카드): 앞쪽을 크게 휩씀
    IEnumerator Pendulum(Vector2 dir)
    {
        float r = 5f + card[2];
        Vector3 o = transform.position;
        float a0 = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Fx.Spawn("fx_swordswing", o, r * 2f, new Color(1f, 0.95f, 0.8f, 0.9f), 22f, a0, AttackLook.AreaOrder - 1);      // 1.1.2: 색까지 그린 베기 그림이라 곱하는 색은 옅게
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(o, r, clockHits)))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            Vector2 to = c.transform.position - o;
            if (Vector2.Angle(dir, to) > 70f) continue;
            Specials.Damage(c.gameObject, Damage * 1.5f, to.normalized, 2.5f);
        }
        Play("whoosh", 0.7f, 0.6f);
        yield break;
    }

    // 뻐꾸기 시계 (형태): 정각마다 뻐꾸기가 가까운 적을 쪼음
    IEnumerator Cuckoos(Vector3 from, int n)
    {
        foreach (Transform t in SignatureSkills.Nearest(from, 10f, n))
        {
            if (t == null) continue;
            Fx.Spawn("fx_falcon", t.position, 2f, new Color(0.9f, 0.7f, 0.45f), 20f);
            Specials.Damage(t.gameObject, Damage * formPower, Vector3.zero, 0.5f);
            Play("pop", 0.4f, 1.8f);
            yield return new WaitForSeconds(0.1f);
        }
    }

    // 시간 균열 (2차): 정각 톱니가 터진 자리에 2초 균열 (안의 적은 느려지고 0.4초마다 다침)
    IEnumerator TimeRift(Vector3 p)
    {
        FxAnim a = Fx.Play("fx_vortex", p, 4f, new Color(0.55f, 0.85f, 1f, 0.7f), 12f, 0f, 6, true, 2f);
        for (float t = 0f; t < 2f; t += 0.4f)
        {
            foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(p, 2f, clockHits)))
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                Specials.Damage(c.gameObject, Damage * 0.4f, Vector3.zero, 0f);
                if (c != null && c.TryGetComponent(out EnermyController e)) e.Slow(0.3f, 0.5f);
            }
            yield return new WaitForSeconds(0.4f);
        }
        if (a != null) Destroy(a.gameObject);
    }

    // 발밑 시계 눈금 고리
    void DrawClockRing()
    {
        // 1.0.2: 이펙트 · 적에 묻혀 안 보이던 것 — 어두운 문자판 테두리 위에 굵고 밝은 눈금, 스킬 이펙트(+40)보다 위 순서로
        if (clockRing == null)
        {
            clockDial = Hostile.NewLine("ClockDial", new Color(0.08f, 0.05f, 0.12f, 0.75f), 0.3f, 58);
            clockDial.loop = true;
            clockRing = Hostile.NewLine("ClockRing", Brass, 0.18f, 59);
            clockRing.loop = false;
        }
        float k = Mathf.Clamp01(clockTicks / (float)ClockNeed);
        bool show = !PlayerController.Cinematic;
        clockDial.enabled = show;
        clockRing.enabled = show && k > 0f;
        if (!show) return;
        Vector3 at = transform.position + Vector3.down * 0.9f;
        Hostile.SetArc(clockDial, at, 1.1f, 0f, 360f);
        if (k <= 0f) return;
        Hostile.SetArc(clockRing, at, 1.1f, 90f, 90f - 360f * k);
        float pulse = ClockReady ? 0.75f + 0.25f * Mathf.Sin(Time.time * 12f) : 0.95f;
        Color c = ClockReady ? new Color(1f, 0.9f, 0.4f, pulse) : new Color(Mathf.Lerp(Brass.r, 1f, 0.35f), Mathf.Lerp(Brass.g, 1f, 0.35f), Mathf.Lerp(Brass.b, 1f, 0.35f), pulse);
        clockRing.startColor = clockRing.endColor = c;
        clockRing.startWidth = clockRing.endWidth = ClockReady ? 0.26f : 0.18f;
    }

    // ================================================================= 우클릭: 되감기
    // 지난 4초의 자리 · 체력 (0.1초마다)
    readonly Queue<(float t, Vector3 p, float hp)> timeLog = new Queue<(float, Vector3, float)>();
    float nextLog;
    bool rewinding;
    GameObject rewindGhost;
    LineRenderer rewindPath, rewindPathCore, rewindRing;
    Vector3 lastLogPos;
    const float RewindSeconds = 3f;
    const float RewindPathHalf = 1.6f, RewindArriveRadius = 5f;      // 지나온 길 판정 폭(반) · 도착 둘레 반경 (미리 보기도 같은 값)

    void LogTime()
    {
        if (Time.time < nextLog) return;
        nextLog = Time.time + 0.1f;
        // 순간이동(스테이지 넘어감 · 보스 기술로 끌려감)은 기록을 새로 시작 — 예전엔 지난 자리로 되감길 수 있었음
        if (timeLog.Count > 0 && (transform.position - lastLogPos).sqrMagnitude > 6f * 6f) timeLog.Clear();
        lastLogPos = transform.position;
        timeLog.Enqueue((Time.time, transform.position, player.PlayerHealth));
        while (timeLog.Count > 0 && Time.time - timeLog.Peek().t > RewindSeconds + 1f) timeLog.Dequeue();
    }

    // 3초 전 (기록이 그보다 짧으면 가장 오래된 것) · 지금 벽인 자리(결계가 끝나 되살아난 벽 등)는 건너뜀
    (Vector3 p, float hp) PastSelf()
    {
        foreach (var e in timeLog)
        {
            if (Time.time - e.t > RewindSeconds || Hostile.IsWall(e.p)) continue;
            return (e.p, e.hp);
        }
        return (transform.position, player.PlayerHealth);
    }

    // LateUpdate (CharacterKit.Rules) 에서: 멈춤 · 연출 · 사망 중에도 불림 — 발밑 시계 · 되감기 미리 보기 정리
    void ClockLate()
    {
        DrawClockRing();
        if (rewinding && (PlayerController.IsPaused || PlayerController.Cinematic || StoryDirector.Playing || player == null || player.IsDying || player.PlayerHealth <= 0f))
            CancelRewind();
    }

    // 1.0.2: 멈춤 · 연출 · 사망으로 우클릭 처리가 끊기면 잔상 · 경로가 화면에 남고, 다시 움직일 때 저절로 되감기던 것 — 취소
    void CancelRewind()
    {
        rewinding = false;
        ShowRewindPreview(false);
        CancelUltAim();
    }

    void ShowRewindPreview(bool on)
    {
        if (rewindGhost != null) rewindGhost.SetActive(on);
        if (rewindPath != null) rewindPath.enabled = on;
        if (rewindPathCore != null) rewindPathCore.enabled = on;
        if (rewindRing != null) rewindRing.enabled = on;
    }

    void UpdateRewind(SkillGauge gauge, bool full)
    {
        LogTime();
        if (!rewinding && GameInput.UltDown && full)
        {
            rewinding = true;
            Play("clank", 0.5f, 2.2f);
        }
        if (!rewinding) { ShowRewindPreview(false); return; }

        var past = PastSelf();
        DrawRewindPreview(past.p, past.hp);
        if (AddedUltShown) DrawUltAim(false, true);

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        rewinding = false;
        ShowRewindPreview(false);
        CancelUltAim();
        StartCoroutine(RewindRoutine(past.p, past.hp));
        KitUltAdd(0.15f, past.p);
        Spend(gauge);
    }

    // 되감기 미리 보기: 3초 전의 내 모습 · 지나온 길(판정 폭 그대로) · 도착 둘레(멈춤 · 되갚기 범위)
    // 1.0.2: 예전엔 경로가 판정(폭 3.2)보다 가늘게(1.2) 그려졌고, 무기 진화 뒤 덧붙는 궁극기 범위를 그리면 경로가 꺼졌으며, 잔상이 스킬 이펙트에 묻혔음
    void DrawRewindPreview(Vector3 to, float pastHp)
    {
        if (rewindGhost == null)
        {
            rewindGhost = new GameObject("RewindGhost");
            SpriteRenderer g = rewindGhost.AddComponent<SpriteRenderer>();
            g.sortingLayerName = "Effect";
            g.sortingOrder = 57;
        }
        if (rewindPath == null)
        {
            rewindPath = Hostile.NewLine("RewindPath", TimeBlue, RewindPathHalf * 2f, 55);
            rewindPathCore = Hostile.NewLine("RewindPathCore", Color.white, 0.14f, 56);
            rewindRing = Hostile.NewLine("RewindRing", TimeBlue, 0.16f, 56);
            rewindPath.loop = rewindPathCore.loop = false;
            rewindRing.loop = true;
        }
        ShowRewindPreview(true);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
        SpriteRenderer gs = rewindGhost.GetComponent<SpriteRenderer>();
        gs.sprite = body.sprite;
        gs.flipX = body.flipX;
        gs.color = new Color(0.6f, 0.92f, 1f, 0.55f + 0.25f * pulse);
        rewindGhost.transform.position = to;
        rewindGhost.transform.localScale = transform.lossyScale;

        Vector3 me = transform.position;
        rewindPath.positionCount = rewindPathCore.positionCount = 2;
        rewindPath.SetPosition(0, me); rewindPath.SetPosition(1, to);
        rewindPathCore.SetPosition(0, me); rewindPathCore.SetPosition(1, to);
        rewindPath.startColor = rewindPath.endColor = new Color(0.55f, 0.85f, 1f, 0.16f + 0.08f * pulse);
        rewindPathCore.startColor = rewindPathCore.endColor = new Color(0.8f, 0.95f, 1f, 0.6f + 0.3f * pulse);
        // 되찾을 체력이 있으면 도착 고리가 초록빛 (잃은 체력의 절반을 되찾음)
        bool heals = pastHp - player.PlayerHealth > 1f;
        Color ring = heals ? new Color(0.5f, 1f, 0.65f, 0.6f + 0.35f * pulse) : new Color(0.55f, 0.88f, 1f, 0.55f + 0.35f * pulse);
        Hostile.SetArc(rewindRing, to, RewindArriveRadius, 0f, 360f);
        rewindRing.startColor = rewindRing.endColor = ring;
    }

    IEnumerator RewindRoutine(Vector3 to, float pastHp)
    {
        Vector3 from = transform.position;
        float lost = Mathf.Max(0f, pastHp - player.PlayerHealth);
        player.GrantInvincibility(0.6f);
        // 거꾸로 감기는 모습: 잔상이 지나온 길에 남음
        for (int i = 0; i <= 5; i++)
            Fx.Spawn("fx_stealth", Vector3.Lerp(from, to, i / 5f), 2.2f, new Color(0.55f, 0.85f, 1f, 0.6f - 0.08f * i), 16f);
        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, to, t / 0.18f);
            yield return null;
        }
        transform.position = to;
        timeLog.Clear();            // 되감은 뒤엔 기록을 새로 (다음 되감기가 되감기 전 자리로 튀지 않게)
        lastLogPos = to;
        float dmg = Damage * 4f * UltMul;
        // 지나온 길의 적
        Vector3 mid = (from + to) * 0.5f;
        List<Collider2D> caught = new List<Collider2D>();
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(mid, (to - from).magnitude * 0.5f + RewindPathHalf, clockHits)))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            if (Hostile.DistanceToSegment(c.transform.position, from, to) > RewindPathHalf) continue;
            Specials.Damage(c.gameObject, dmg, ((Vector2)(to - from)).normalized, 1f);
            caught.Add(c);
        }
        // 도착: 둘레 적이 잠깐 멈추고, 잃은 체력의 세 배를 되갚음
        float payback = lost * 3f * UltMul;
        List<Collider2D> near = new List<Collider2D>();
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(to, RewindArriveRadius, clockHits)))
            if (c != null && (c.CompareTag("enermy") || c.CompareTag("boss"))) near.Add(c);
        foreach (Collider2D c in near)
        {
            if (c == null) continue;
            Specials.Damage(c.gameObject, dmg * 0.6f + (near.Count > 0 ? payback / near.Count : 0f), (c.transform.position - to).normalized, 2f);
            if (c != null && c.TryGetComponent(out EnermyController e)) e.Slow(0f, 1f);
        }
        float heal = lost * 0.5f * SignatureSkills.RewindHealMul();
        if (heal > 0.5f && player.PlayerHealth > 0f && !player.IsDying)
        {
            player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + heal);
            SpecialAbilities.SharedFx?.FloatText(to, "+" + Mathf.RoundToInt(heal), new Color(0.5f, 1f, 0.6f), 4f, 0f);
        }
        Fx.Spawn("fx_vortex", to, 6f, TimeBlue, 18f);
        Fx.Spawn("fx_shock", to, 10f, new Color(0.55f, 0.85f, 1f, 0.8f), 18f);
        ShockRing.Spawn(to, 0.5f, RewindArriveRadius, 0.4f, Brass, 0.25f);
        Hostile.Shake(0.2f);
        Play("rewind", 0.9f, 1f);
        Play("bell", 0.6f, 0.8f);
        SignatureSkills.Rewound(from, to, lost, caught);
        if (card[3] > 0) StartCoroutine(LeftClock(from, 1f + card[3]));
    }

    // 남은 시각 (카드): 떠난 자리에 시계가 남아 0.5초마다 둘레를 벰
    IEnumerator LeftClock(Vector3 p, float seconds)
    {
        FxAnim a = Fx.Play("fx_target_rune", p, 5f, new Color(1f, 0.8f, 0.4f, 0.75f), 12f, 0f, 5, true, seconds);
        if (a != null) a.spin = -120f;
        float mul = SignatureSkills.LeftClockMul();
        for (float t = 0f; t < seconds; t += 0.5f)
        {
            yield return new WaitForSeconds(0.5f);
            foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(p, 2.5f, clockHits)))
                if (c != null && (c.CompareTag("enermy") || c.CompareTag("boss")))
                    Specials.Damage(c.gameObject, Damage * 0.6f * mul, Vector3.zero, 0f);
            Play("clank", 0.25f, 2.6f);
        }
        if (a != null) Destroy(a.gameObject);
    }

    void OnDestroyClockmaker()
    {
        if (clockRing != null) Destroy(clockRing.gameObject);
        if (clockDial != null) Destroy(clockDial.gameObject);
        if (rewindGhost != null) Destroy(rewindGhost);
        if (rewindPath != null) Destroy(rewindPath.gameObject);
        if (rewindPathCore != null) Destroy(rewindPathCore.gameObject);
        if (rewindRing != null) Destroy(rewindRing.gameObject);
    }
}
