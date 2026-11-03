using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 장의사 필살기 「장례」 (2.0.0) — 우클릭 · 게이지 가득 · 누르고 있다 뗌 (기존 영웅과 같은 방식 · 조준 없음 — 누르는 시간은 연출만)
//   누르고 있음: 화면 가장자리에 은빛 테가 차오르고 화면 위 가운데에 종이 흔들림 (화면 백색 금지 원칙 — 테두리만) · 판 구멍은 함께 밝아짐
//   뗌: 종 「댕」 → 판 구멍이 있으면 먼저 그대로 매장 → 화면 안(카메라 시야 + 1칸) 보스 아닌 적의 발밑이 열려 떨어짐(공격력 100%) → 1초 뒤 일제 매장(220%)
//       보스 · 중간 보스는 떨어지지 않고 종소리 진동(공격력 320% 한 번) · 쓰러진 자리에 임시 묘비(4초 · 12개까지 가까운 순)
//   상한 60마리 (넘으면 가까운 순) · 손맛 Ult 는 일제 매장 순간 한 번
public partial class CharacterKit
{
    bool utBell;
    float utBellAt;
    GameObject utBellGo;
    SpriteRenderer utBellSr;
    Sprite[] utBellFrames;
    bool utFuneralBusy;
    static readonly List<EnermyController> utFall = new List<EnermyController>(64);

    public bool UtUltEngaged => utBell || utFuneralBusy;

    void UpdateFuneral(SkillGauge gauge, bool full)
    {
        if (utFuneralBusy && !utBell) return;
        // 자동 조작: 눌린 프레임에 게이지가 가득이면 바로 발동 (누르고 있기 판정 생략)
        if (!utBell && GameInput.Auto && GameInput.UltDown && full)
        {
            UtStartFuneral(gauge);
            return;
        }
        if (!utBell && GameInput.UltDown && Time.timeScale > 0f)
        {
            if (!full) { Play("thump", 0.15f, 1.8f); return; }     // 게이지가 덜 찼음: 작은 「툭」 (게이지를 쓰지 않음)
            utBell = true;
            utBellAt = Time.time;
            Play("hum", 0.4f, 0.6f);
        }
        if (!utBell) return;
        if (Time.timeScale == 0f) { UtHideUltAim(); utBell = false; return; }
        UtDrawUltAim();
        if (AddedUltShown) DrawUltAim(false, true);
        if (!GameInput.UltUp && GameInput.UltHeld) return;
        UtHideUltAim();
        utBell = false;
        CancelUltAim();
        UtStartFuneral(gauge);
    }

    void UtStartFuneral(SkillGauge gauge)
    {
        Spend(gauge);
        PlayerMotion.Of(this).Cast(0.6f);
        StartCoroutine(UtFuneralRoutine(false));
        if (UtpOn(UndertakerPaths.Funeral) && Paths.Mastered(UndertakerPaths.Funeral)) StartCoroutine(UtFuneralEcho());       // 장례식 극의: 한 번 더
        KitUltAdd(UndertakerTuning.FuneralFall);
    }

    // 카메라 시야 (+ pad 칸) 사각형
    static bool UtView(float pad, out Rect r)
    {
        r = default;
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return false;
        float h = cam.orthographicSize + pad, w = cam.orthographicSize * cam.aspect + pad;
        Vector3 c = cam.transform.position;
        r = new Rect(c.x - w, c.y - h, 2f * w, 2f * h);
        return true;
    }

    // echo: 장례식 극의의 두 번째 장례 (피해 50% · 판 구멍 매장 · 묘비 · 명부 · 후속 효과 없음)
    IEnumerator UtFuneralRoutine(bool echo)
    {
        utFuneralBusy = true;      // 2.0.0~ 검토: 두 번째 장례(echo)도 잡음 — 안 잡으면 그 사이 새 장례가 static utFall 을 지워 붙잡힌 적이 안 풀렸음
        Vector3 me = transform.position;
        float mul = echo ? UltMul * UndertakerPaths.FuneralMasteryMul : UltMul * SignatureSkills.UtLedgerTake();
        float massMul = 1f, delay = UndertakerTuning.FuneralFall;
        bool mass = SignatureSkills.UtMassFuneral();
        if (mass) { delay = UndertakerPaths.MassFuneralDelay; massMul = UndertakerPaths.MassFuneralMul; }       // 교차 합동 장례
        bool ye = UtEvo("ye.state");
        Hostile.Play("chime", 0.8f, 0.45f);
        Hostile.Play("gong", 0.5f, 0.8f);
        Fx.Spawn("fx_bell_ring", me, 9f, UtSilver, 12f, 0f, 20);
        Hostile.Shake(0.15f);
        // 1) 판 구멍이 있으면 먼저 그대로 매장
        List<UtHole> own = new List<UtHole>();
        foreach (UtHole h in utHoles) if (!h.pitfall && !h.burying) own.Add(h);
        if (!echo && own.Count > 0)
        {
            foreach (UtHole h in own) { h.burying = true; h.growing = false; utHoles.Remove(h); }
            utCur = null; utDigging = false;
            StartCoroutine(UtBuryRoutine(own, false));
        }
        // 2) 화면 안 적 모으기: 보스 · 중간 보스는 종소리 진동만, 나머지는 가까운 순 60마리
        float pad = 1f + SignatureSkills.UtFuneralPad();
        List<(float d, EnermyController e)> normal = new List<(float, EnermyController)>(80);
        List<GameObject> big = new List<GameObject>();
        if (UtView(pad, out Rect view))
        {
            Vector2 vc = view.center;
            float rad = Mathf.Sqrt(view.width * view.width + view.height * view.height) * 0.5f + 1f;
            using (var list = PhysHits.Circle(new Vector3(vc.x, vc.y, 0f), rad, true))
            {
                foreach (Collider2D c in list)
                {
                    if (c == null) continue;
                    if (!view.Contains(c.transform.position)) continue;
                    if (c.CompareTag("boss")) { big.Add(c.gameObject); continue; }
                    if (!c.CompareTag("enermy") || !c.TryGetComponent(out EnermyController e) || e.IsDead) continue;
                    if (e.IsMidBoss) { big.Add(c.gameObject); continue; }
                    if (e.holder != null || e.swarm != null) continue;
                    normal.Add((((Vector2)e.transform.position - (Vector2)me).sqrMagnitude, e));
                }
            }
        }
        normal.Sort((a, b) => a.d.CompareTo(b.d));
        utFall.Clear();
        List<Vector3> spots = new List<Vector3>(normal.Count);
        List<FxAnim> pits = new List<FxAnim>(normal.Count);
        int fell = 0;
        using (DamageSource.As(DamageSource.Ult))
        {
            for (int i = 0; i < normal.Count && fell < UndertakerTuning.FuneralCap; i++)
            {
                EnermyController e = normal[i].e;
                if (e == null || !EnemyHold.TryTake(e, this)) continue;
                fell++;
                utFall.Add(e);
                Vector3 p = e.transform.position;
                spots.Add(p);
                FxAnim pit = Fx.Play("fx_hole", p, UtBodyR(e) * 3.4f, Color.white, 8f, 0f, 9, true, delay + 0.4f, "Background");
                if (pit != null) pits.Add(pit);
                Specials.Damage(e.gameObject, Damage * 1.0f * mul * formPower, Vector3.zero, 0f);
                if (e != null && !e.IsDead) EnemyHold.Raise(e, body, 0);
            }
            // 보스 · 중간 보스: 종소리 진동 (공격력 320% 한 번)
            foreach (GameObject g in big)
            {
                if (g == null) continue;
                Specials.Damage(g, Damage * UndertakerTuning.FuneralBoss * mul * formPower, ((Vector2)(g.transform.position - me)).normalized, 1f);
                Fx.Spawn("fx_bell_ring", g.transform.position, 5f, UtSilver, 14f, 0f, 20);
            }
        }
        RunCounters.Both("fall", fell);
        Play("thump", 0.6f, 0.6f);
        SignatureSkills.UtFuneralBell(me, fell, 0);
        if (ye && !echo) StartCoroutine(UtStateBells(me, mul));
        // 3) 일제 매장 (delay 뒤 한 번에)
        yield return StartCoroutine(UtFuneralWait(delay, mass, spots, pits, Damage * 1.0f * mul * formPower));
        if (player == null) { utFuneralBusy = false; yield break; }
        int killed = 0;
        List<Vector3> dead = new List<Vector3>(fell);
        using (DamageSource.As(DamageSource.Ult))
        {
            for (int i = 0; i < utFall.Count; i++)
            {
                EnermyController e = utFall[i];
                if (e == null) { dead.Add(spots[i]); killed++; continue; }
                EnemyHold.Release(e, this);
                if (e.IsDead) { dead.Add(spots[i]); killed++; continue; }
                Specials.Damage(e.gameObject, Damage * UndertakerTuning.FuneralBury * mul * massMul * UtBuryMul, Vector3.zero, 0f);
                if (e == null || e.IsDead) { dead.Add(spots[i]); killed++; }
                else e.Slow(0f, 1f);
            }
        }
        for (int i = 0; i < spots.Count; i++) Fx.Spawn("fx_bury", spots[i], 2.6f, Color.white, 12f, 0f, 16);
        foreach (FxAnim a in pits) if (a != null) Destroy(a.gameObject);
        utFall.Clear();
        if (!Impact.UltRecent) Hostile.Shake(0.3f);       // 2.0.1~ 메아리 장례로 1초 안에 두 번째면 큰 흔들림 없이 소리만
        Play("thump", 0.8f, 0.5f);
        Impact.Hit(ImpactTier.Ult, me, Vector2.down);            // 일제 매장 순간 한 번 (종 순간이 아님)
        RunCounters.Both("bury", killed);
        if (echo) { utFuneralBusy = false; yield break; }          // 두 번째 장례는 묘비 · 후속 효과 없음
        // 4) 쓰러진 자리마다 임시 묘비 (4초 · 12개까지 가까운 순)
        List<Vector3> graves = new List<Vector3>();
        dead.Sort((a, b) => ((Vector2)(a - me)).sqrMagnitude.CompareTo(((Vector2)(b - me)).sqrMagnitude));
        List<Vector2> placed = new List<Vector2>();
        foreach (Vector3 d in dead)
        {
            if (placed.Count >= UndertakerTuning.TempGraveCap) break;
            bool near = false;
            foreach (Vector2 q in placed) if (Vector2.Distance(q, d) < 1.1f) { near = true; break; }
            if (near) continue;
            UndertakerGrave g = UtSpawnGrave(d, false, true);
            if (g != null) placed.Add(d);
        }
        SignatureSkills.UtFuneralEnd(killed, placed.Count);
        if (card[3] > 0 && placed.Count > 0) StartCoroutine(UtBellKeeper(placed, card[3], mul));
        UtPathFuneralDone();
        utFuneralBusy = false;
    }

    // 종지기 (카드): 장례로 솟은 묘비마다 종이 한 번 더 — 둘레 2.5칸 적에게 공격력 60% (0.6초 간격 · 1 / 2 / 3번)
    IEnumerator UtBellKeeper(List<Vector2> spots, int times, float mul)
    {
        for (int k = 0; k < times; k++)
        {
            yield return new WaitForSeconds(0.6f);
            if (player == null || player.IsDying) yield break;
            using (DamageSource.As(DamageSource.Ult))
                foreach (Vector2 p in spots)
                {
                    UtDamageCircle(p, 2.5f, Damage * 0.6f * mul * formPower, 1f, DamageSource.Ult, true);
                    Fx.Spawn("fx_bell_ring", new Vector3(p.x, p.y, 0f), 3.2f, UtSilver, 14f, 0f, 20);
                }
            Play("chime", 0.35f, 1.1f + 0.15f * k);
        }
    }

    // 국장 (진화): 종이 두 번 더 울림 — 둘레 적에게 공격력 40%
    IEnumerator UtStateBells(Vector3 at, float mul)
    {
        for (int k = 0; k < 2; k++)
        {
            yield return new WaitForSeconds(0.5f);
            if (player == null || player.IsDying) yield break;
            Vector3 c = transform.position;
            using (DamageSource.As(DamageSource.Ult)) UtDamageCircle(c, 7f, Damage * 0.4f * mul * formPower, 1.5f, DamageSource.Ult, true);
            Fx.Spawn("fx_bell_ring", c, 12f, UtSilver, 14f, 0f, 20);
            Play("chime", 0.5f, 0.7f + 0.2f * k);
        }
    }

    // ================================================================= 누르는 동안 표시
    void UtDrawUltAim()
    {
        float held = Time.time - utBellAt;
        Color hero = HeroAim.ColorOf(HeroAimKind.Undertaker);
        if (UtView(-0.35f, out Rect r))
        {
            // 2.0.0~ 도트 부고 테: 판정 사각형(화면) 안쪽 가장자리에 은빛 줄 · 구슬 박힌 그늘 띠 + 네 모서리 꺾쇠 · 십자
            //   (예전: 화면 가장자리를 두른 얇은 단색 네모 선) · 안 그린 프레임엔 저절로 숨음
            utBellRectOn = true;
            float a = Mathf.Clamp01(0.3f + held * 0.7f) * (GameSettings.ReduceMotion ? 0.9f : 0.75f + 0.25f * Mathf.Sin(Time.time * 10f));
            Color hc = new Color(hero.r, hero.g, hero.b, a);
            PixelMarks pm = PixelMarks.Ensure(ref utPx, this);
            pm.EdgeBands(r, PixelMarkArt.Mourning(false), PixelMarkArt.Mourning(true), PixelMarkArt.MourningThick, hc, true, true, 0);
            pm.Corners(r.center, r.width * 0.5f, r.height * 0.5f, hc);
            float k = PixelMarkArt.MourningThick + 0.45f;
            pm.Put(PixelMarkArt.Cross, new Vector2(r.xMin + k, r.yMax - k), hc);
            pm.Put(PixelMarkArt.Cross, new Vector2(r.xMax - k, r.yMax - k), hc);
            pm.Put(PixelMarkArt.Cross, new Vector2(r.xMin + k, r.yMin + k), hc);
            pm.Put(PixelMarkArt.Cross, new Vector2(r.xMax - k, r.yMin + k), hc);
            // 화면 위 가운데 종 (오래 누를수록 크게 흔들림 — 효과는 같음)
            if (utBellGo == null)
            {
                utBellGo = new GameObject("UtBell");
                utBellSr = utBellGo.AddComponent<SpriteRenderer>();
                utBellSr.sortingLayerID = PlayerLook.TopLayer;
                utBellSr.sortingOrder = 70;
                utBellFrames = Fx.Frames("fx_bell_hang");
            }
            if (utBellFrames != null && utBellFrames.Length > 0)
            {
                utBellGo.SetActive(true);
                float sway = Mathf.Sin(Time.time * (8f + 6f * Mathf.Clamp01(held))) * (6f + 18f * Mathf.Clamp01(held));
                utBellSr.sprite = utBellFrames[(int)(Time.time * 4f) % utBellFrames.Length];
                float h = utBellSr.sprite.bounds.size.y;
                utBellGo.transform.localScale = Vector3.one * (h > 0.01f ? 2.6f / h : 1f);
                utBellGo.transform.position = new Vector3(r.center.x, r.yMax - 1.6f, 0f);
                utBellGo.transform.rotation = Quaternion.Euler(0f, 0f, sway);
            }
        }
    }

    void UtUltTick()
    {
        if (!utBell && (utBellRectOn || (utBellGo != null && utBellGo.activeSelf))) UtHideUltAim();
    }

    PixelMarks utPx;
    bool utBellRectOn;

    void UtHideUltAim()
    {
        utBellRectOn = false;
        if (utBellGo != null && utBellGo.activeSelf) utBellGo.SetActive(false);
    }

    void UtUltCleanup()
    {
        if (utPx != null) Destroy(utPx.gameObject);
        if (utBellGo != null) Destroy(utBellGo);
        foreach (EnermyController e in utFall) if (e != null) EnemyHold.Release(e, this);
        utFall.Clear();
    }
}
