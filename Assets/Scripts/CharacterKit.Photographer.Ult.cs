using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 사진사 필살기 「파노라마」 (2.0.0) — 우클릭 · 게이지 가득 · 누르고 있다 뗌
//   누르고 있음: 화면 너비 전체를 가로지르는 촬영 띠가 커서 높이에 보임 (마우스로 위아래 · 오래 누를수록 8 -> 14칸 두껍게 · 0.8초) — 시간은 느려지지 않음
//   뗌: 셔터 커튼이 띠를 따라 한 번 지나감 (커서가 나보다 오른쪽이면 왼 -> 오) 0.4초 · 지나간 적에게 공격력 240% + 종이 4초 (면역 무시)
//       0.3초 뒤 바람: 종이 적이 커튼 진행 방향으로 약 6칸 날아가며 부딪힌 적과 서로 피해 (공격력 50%) · 보스는 피해 + 초점 표식 5중첩 (날아가지 않음)
//   태풍 파노라마(진화): 커튼이 되돌아오며 한 번 더 지나가고 바람도 반대로 한 번 더
public partial class CharacterKit
{
    bool phPano, phPanoBusy;
    float phPanoAt;
    LineRenderer phBand;
    static readonly List<(GameObject go, EnermyController e, float x)> phSweepList = new List<(GameObject, EnermyController, float)>(64);

    public bool PhUltEngaged => phPano || phPanoBusy;

    static bool PhView(out Rect r)
    {
        r = default;
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return false;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector3 c = cam.transform.position;
        r = new Rect(c.x - w, c.y - h, 2f * w, 2f * h);
        return true;
    }

    void UpdatePanorama(SkillGauge gauge, bool full)
    {
        if (phPanoBusy && !phPano) return;
        // 자동 조작: 눌린 프레임에 게이지가 가득이면 바로 (플레이어 높이 · 기본 두께)
        if (!phPano && GameInput.Auto && GameInput.UltDown && full)
        {
            PhStartPanorama(gauge, transform.position.y, 8f, Mouse.x >= transform.position.x ? 1f : -1f);
            return;
        }
        if (!phPano && GameInput.UltDown && Time.timeScale > 0f)
        {
            if (!full) { Play("thump", 0.15f, 1.8f); return; }
            phPano = true;
            phPanoAt = Time.time;
            Play("hum", 0.3f, 1.8f);
        }
        if (!phPano) return;
        if (Time.timeScale == 0f) { PhHideBand(); phPano = false; return; }
        float thick = Mathf.Lerp(8f, 14f, Mathf.Clamp01((Time.time - phPanoAt) / 0.8f));
        PhDrawBand(Mouse.y, thick);
        if (AddedUltShown) DrawUltAim(false, true);
        if (!GameInput.UltUp && GameInput.UltHeld) return;
        float y = Mouse.y;
        float dir = Mouse.x >= transform.position.x ? 1f : -1f;
        PhHideBand();
        phPano = false;
        CancelUltAim();
        PhStartPanorama(gauge, y, thick, dir);
    }

    void PhStartPanorama(SkillGauge gauge, float y, float thick, float dirX)
    {
        Spend(gauge);
        PlayerMotion.Of(this).Cast(0.5f);
        StartCoroutine(PhPanoRoutine(y, thick, dirX));
        KitUltAdd(0.4f);
    }

    void PhDrawBand(float y, float thick)
    {
        if (!PhView(out Rect r)) return;
        Color hero = HeroAim.ColorOf(HeroAimKind.Photographer);
        if (phBand == null) { phBand = Hostile.NewLine("PhBand", hero, 0.16f, 25); phBand.loop = true; phBand.positionCount = 4; }
        phBand.enabled = true;
        float y0 = y - thick * 0.5f, y1 = y + thick * 0.5f;
        phBand.SetPosition(0, new Vector3(r.xMin, y0, 0f));
        phBand.SetPosition(1, new Vector3(r.xMax, y0, 0f));
        phBand.SetPosition(2, new Vector3(r.xMax, y1, 0f));
        phBand.SetPosition(3, new Vector3(r.xMin, y1, 0f));
        float held = Time.time - phPanoAt;
        float a = Mathf.Clamp01(0.4f + held) * (0.7f + 0.3f * Mathf.Sin(Time.time * 10f));
        phBand.startColor = phBand.endColor = new Color(hero.r, hero.g, hero.b, a);
        phBand.startWidth = phBand.endWidth = 0.12f + 0.1f * Mathf.Clamp01(held);
        HeroAim.Mark(phBand, HeroAimKind.Photographer, HeroAimFx.None);
    }

    void PhHideBand() { if (phBand != null) phBand.enabled = false; }

    IEnumerator PhPanoRoutine(float y, float thick, float dirX)
    {
        phPanoBusy = true;
        bool back = SignatureSkills.PhTyphoon();
        yield return PhSweep(y, thick, dirX);
        if (back && player != null && !player.IsDying)
        {
            yield return new WaitForSeconds(0.1f);
            yield return PhSweep(y, thick, -dirX);
        }
        phPanoBusy = false;
    }

    // 커튼 한 번: 띠 안 적을 모아 두고 커튼이 닿는 순서대로 찍음 -> 0.3초 뒤 바람
    IEnumerator PhSweep(float y, float thick, float dirX)
    {
        if (!PhView(out Rect r)) yield break;
        float x0 = dirX > 0f ? r.xMin - 1f : r.xMax + 1f, x1 = dirX > 0f ? r.xMax + 1f : r.xMin - 1f;
        phSweepList.Clear();
        Vector2 bandC = new Vector2(r.center.x, y);
        using (var ovl = PhysHits.Box(bandC, new Vector2(r.width + 2f, thick), 0f)) foreach (Collider2D col in ovl)
        {
            if (col == null) continue;
            bool boss = col.CompareTag("boss");
            if (!boss && !col.CompareTag("enermy")) continue;
            EnermyController e = boss ? null : EnermyController.FromCollider(col);
            if (!boss && (e == null || e.IsDead)) continue;
            phSweepList.Add((col.gameObject, e, col.transform.position.x));
        }
        Vector3 mid = new Vector3(bandC.x, y, 0f);
        Impact.Hit(ImpactTier.Ult, mid, new Vector2(dirX, 0f));
        Hostile.Shake(0.3f);
        FxAnim cur = Fx.Play("fx_curtain", new Vector3(x0, y, 0f), thick, new Color(1f, 1f, 1f, 0.8f), 14f, dirX > 0f ? 0f : 0f, 24, true, 0.6f);
        float dmg = Damage * 2.4f * UltMul * formPower;
        List<EnermyController> flown = new List<EnermyController>();
        const float dur = 0.4f;
        int done = 0;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float x = Mathf.Lerp(x0, x1, t / dur);
            if (cur != null) cur.transform.position = new Vector3(x, y, 0f);
            for (int i = 0; i < phSweepList.Count; i++)
            {
                var s = phSweepList[i];
                if (s.go == null || (x - s.x) * dirX < 0f) continue;
                phSweepList[i] = (null, s.e, s.x);
                done++;
                Vector3 p = s.go.transform.position;
                bool boss = s.e == null;
                Specials.Damage(s.go, dmg, new Vector3(dirX, 0f, 0f), 0f);
                Fx.Spawn("fx_flashframe", p, 2.2f, Color.white, 24f, 0f, 20);
                if (boss) { PhAddMarks(PhotographerTuning.MarkMax); continue; }
                if (s.e != null && !s.e.IsDead && PhPaperTake(s.e, 4f, true)) flown.Add(s.e);
            }
            yield return null;
        }
        if (cur != null) Destroy(cur.gameObject);
        RunCounters.Both("panorama.hit", done);
        yield return new WaitForSeconds(0.3f);
        // 바람: 종이 적이 커튼이 지나간 쪽으로 약 6칸
        foreach (EnermyController e in flown) if (e != null && !e.IsDead) PhFly(e, new Vector2(dirX, 0f), 6f, 0.4f);
        Play("whoosh", 0.5f, 0.6f);
    }

    void PhUltLate()
    {
        if (!phPano && phBand != null && phBand.enabled) PhHideBand();
    }

    void PhUltCleanup()
    {
        if (phBand != null) Destroy(phBand.gameObject);
    }
}
