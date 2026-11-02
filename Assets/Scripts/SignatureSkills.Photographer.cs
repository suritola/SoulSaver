using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 사진사 고유 스킬 (2.0.0) — hero-photographer.md 5 · 6절
//   x.flash 플래시 반사 · x.selfie 셀카 · x.timer 셀프 타이머 · x.cutout 오려 붙이기 · x.overexpose 노출 과다
//   x.motion 흔들린 사진 · x.album 앨범 · x.chance 셔터 찬스 · x.darkroom 현상소 · x.group 기념 사진
// 진화: xe.night 야경 · xe.track 추적 초점 · xe.studio 스튜디오 · xe.confetti 색종이 폭풍 · xe.typhoon 태풍 파노라마 · xe.flipbook 플립북 · xe.memorial 기념 앨범
// 훅은 CharacterKit.Photographer 가 부름 (틀 · 사본 · 종이는 Kit 쪽 값)
public partial class SignatureSkills
{
    bool IsPh => who == CharacterId.Photographer;

    public static int PhLv(string key) => Instance != null && Instance.IsPh ? Instance.L(key) : 0;
    public static bool PhEvo(string key) => Instance != null && Instance.IsPh && Instance.E(key);

    float phSelfieAt, phFlashInvAt;
    int phKills;
    bool phNightOn;
    readonly HashSet<string> phFirst = new HashSet<string>();
    static readonly Color PhTint = new Color(0.78f, 0.83f, 1f);

    // ---------------- 정적 훅
    public static float PhMotionExtra() => Instance != null && Instance.IsPh ? V(Instance.L("x.motion"), 1f, 0.5f) : 0f;
    public static bool PhTrack() => PhEvo("xe.track");
    public static bool PhTyphoon() => PhEvo("xe.typhoon");
    public static bool PhConfetti() => PhEvo("xe.confetti");
    public static float PhLureExtra() => 0f;

    // 기념 사진: 보스와 함께 찍힌 일반 적 수만큼 보스가 더 다침
    public static float PhGroupMul(int n)
    {
        int lv = PhLv("x.group");
        return lv <= 0 ? 1f : 1f + n * V(lv, 0.04f, 0.01f) * Instance.P("x.group");
    }

    // 셔터 찬스: 예고(시전) 중인 적을 찍으면 그 스킬이 끊기고 더 아픔 (보스는 끊기지 않음 · 예고 판별 불가라 일반 적만)
    public static float PhChance(EnermyController e)
    {
        if (Instance == null || !Instance.IsPh || e == null) return 1f;
        int lv = Instance.L("x.chance");
        bool track = Instance.E("xe.track") && Instance.Kit != null && Instance.Kit.PhLocked == e;      // 추적 초점: 자동 초점이 잠근 적만
        if (lv <= 0 && !track) return 1f;
        EnemySkill sk = e.GetComponent<EnemySkill>();
        if (sk == null || !sk.Winding) return 1f;
        sk.CancelCast();
        if (track) e.Slow(0f, 1f);
        Instance.Flair("x.chance", e.transform.position, 2.5f, PhTint);
        Instance.FirstImpact("x.chance", e.transform.position);
        return lv > 0 ? V(lv, 1.4f, 0.2f) * Instance.P("x.chance") : 1.4f;
    }

    // 오려 붙이기: 이미 종이인 적을 다시 찍으면 종이비행기가 되어 가까운 다른 적에게 (서른 비행기 ... 색종이 폭풍은 셋을 꿰뚫음)
    public static void PhCutout(EnermyController e)
    {
        if (Instance == null || !Instance.IsPh || e == null) return;
        int lv = Instance.L("x.cutout");
        if (lv <= 0) return;
        Instance.PhPlane(e, lv);
    }

    void PhPlane(EnermyController from, int lv)
    {
        int count = E("xe.confetti") ? 3 : 1;
        Vector3 p = from.transform.position;
        List<Transform> near = Nearest(p, 9f, count, from.gameObject);
        foreach (Transform t in near)
        {
            if (t == null) continue;
            Fx.Spawn(Fx.Frames("fx_paperplane").Length > 0 ? "fx_paperplane" : "fx_sparkle", Vector3.Lerp(p, t.position, 0.5f), 1.6f, Color.white, 14f, Mathf.Atan2(t.position.y - p.y, t.position.x - p.x) * Mathf.Rad2Deg, 22);
            Deal(t.gameObject, Atk * V(lv, 0.6f, 0.2f) * P("x.cutout"), (t.position - p).normalized, 0f);
            EnermyController e = t != null ? t.GetComponent<EnermyController>() : null;
            if (e != null && !e.IsDead && Kit != null) Kit.PhPaperTake(e, 1.5f, false);
        }
        FirstImpact("x.cutout", p);
    }

    // 셔터를 누른 순간: 플래시 반사 · 셀카 · 노출 과다 · 플립북
    public static void PhOnShutter(Vector2 c, Vector2 size, int n, bool master)
    {
        if (Instance == null || !Instance.IsPh) return;
        Instance.OnPhShutter(c, size, n, master);
    }

    void OnPhShutter(Vector2 c, Vector2 size, int n, bool master)
    {
        int lf = L("x.flash");
        bool fpath = PhotographerPaths.On(PhotographerPaths.Flash);      // 플래시 갈래: 투사체 지우기가 기본 (플래시 반사 1단계 몫)
        if (lf > 0 || fpath)
        {
            float k = lf > 0 ? 1f + 0.1f * (lf - 1) : 1f;
            Rect r = new Rect(c - size * k * 0.5f, size * k);
            int erased = 0;
            for (int i = HostileProjectile.Live.Count - 1; i >= 0; i--)
            {
                HostileProjectile hp = HostileProjectile.Live[i];
                if (hp == null || !r.Contains(hp.transform.position)) continue;
                Fx.Spawn("fx_papertear", hp.transform.position, 1.2f, Color.white, 18f, 0f, 22);
                Destroy(hp.gameObject);
                erased++;
            }
            if (erased > 0) FirstImpact("x.flash", new Vector3(c.x, c.y, 0f));
            // 플래시 공명 II: 지운 투사체마다 파노라마 게이지 1% (한 장에 최대 5%)
            if (erased > 0 && PhotographerPaths.ResAt(PhotographerPaths.Flash, 2) && player != null && player.skillGauge != null)
                player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * Mathf.Min(PhotographerPaths.FlashGaugeCap, erased * PhotographerPaths.FlashGauge));
        }
        // 플래시 공명 I: 틀 안에 내 몸을 넣고 찍으면 0.4초 무적 (2초에 한 번)
        if (PhotographerPaths.ResAt(PhotographerPaths.Flash, 1) && Time.time >= phFlashInvAt && player != null && !player.IsDying
            && new Rect(c - size * 0.5f, size).Contains(player.transform.position))
        {
            phFlashInvAt = Time.time + PhotographerPaths.FlashRes1Gap;
            Paths.Invuln(PhotographerPaths.FlashRes1Invuln);
        }
        int ls = L("x.selfie");
        if (ls > 0 && Time.time >= phSelfieAt && player != null)
        {
            Rect r = new Rect(c - size * 0.5f, size);
            if (r.Contains(player.transform.position))
            {
                phSelfieAt = Time.time + 2f;
                Heal(V(ls, 1f, 0.5f) * P("x.selfie"));
                player.GrantInvincibility(0.3f);
                Flair("x.selfie", player.transform.position, 3f, PhTint);
            }
        }
        if (master)
        {
            int lo = L("x.overexpose");
            if (lo > 0) StartCoroutine(PhBurn(c, size, Atk * V(lo, 0.25f, 0.1f) * P("x.overexpose")));
            if (E("xe.flipbook") && Kit != null) StartCoroutine(PhFlip(c, size));
        }
    }

    IEnumerator PhBurn(Vector2 c, Vector2 size, float dmg)
    {
        FirstImpact("x.overexpose", new Vector3(c.x, c.y, 0f));
        for (float t = 0f; t < 2f; t += 0.5f)
        {
            if (!Alive) yield break;
            Fx.Spawn("fx_flashframe", new Vector3(c.x, c.y, 0f), size.y, new Color(1f, 1f, 1f, 0.6f), 12f, 0f, 20);
            using (var ovl = PhysHits.Box(c, size, 0f)) foreach (Collider2D col in ovl)
                if (col != null && IsFoe(col)) Deal(col.gameObject, dmg, Vector3.zero, 0f);
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator PhFlip(Vector2 c, Vector2 size)
    {
        FirstEvoImpact("xe.flipbook", new Vector3(c.x, c.y, 0f));
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.4f);
            if (!Alive || Kit == null) yield break;
            Kit.PhShootExtra(c, size, 0.4f, false);
        }
    }

    // 야경: 남은 틀이 커서를 따라 끌려가며 1초 동안 빛 궤적 (0.3초마다 공격력 25%)
    public static void PhOnNight(Vector2 size)
    {
        if (Instance == null || !Instance.IsPh || !Instance.E("xe.night") || Instance.phNightOn) return;
        Instance.StartCoroutine(Instance.PhNight(size));
    }

    IEnumerator PhNight(Vector2 size)
    {
        phNightOn = true;
        FirstEvoImpact("xe.night", Kit != null ? (Vector3)Kit.PhCursorNow : transform.position);
        float dmg = Atk * 0.25f * P("x.motion");
        for (float t = 0f; t < 1f && Alive && Kit != null; t += 0.3f)
        {
            Vector2 c = Kit.PhCursorNow;
            Fx.Spawn("fx_flashframe", new Vector3(c.x, c.y, 0f), size.y, new Color(0.8f, 0.85f, 1f, 0.5f), 12f, 0f, 19);
            using (var ovl = PhysHits.Box(c, size, 0f)) foreach (Collider2D col in ovl)
                if (col != null && IsFoe(col)) Deal(col.gameObject, dmg, Vector3.zero, 0f);
            yield return new WaitForSeconds(0.3f);
        }
        phNightOn = false;
    }

    // 필름 감기 시작: 셀프 타이머 (커서 자리에 카메라가 남아 2초 뒤 한 번 더 찍음 · 기념 앨범은 두 대)
    public static void PhOnRewind(Vector2 cursor)
    {
        if (Instance == null || !Instance.IsPh) return;
        int lv = Instance.L("x.timer");
        if (lv <= 0) return;
        int cams = Instance.E("xe.memorial") ? 2 : 1;
        for (int i = 0; i < cams; i++) Instance.StartCoroutine(Instance.PhTimer(cursor + (i == 0 ? Vector2.zero : Vector2.right * 2.5f), lv));
    }

    IEnumerator PhTimer(Vector2 at, int lv)
    {
        GameObject cam = new GameObject("PhTimerCam");
        cam.transform.position = new Vector3(at.x, at.y, 0f);
        SpriteRenderer sr = cam.AddComponent<SpriteRenderer>();
        Sprite[] f = Fx.Frames("fx_tripodcam");
        if (f != null && f.Length > 0) { sr.sprite = f[0]; cam.transform.localScale = Vector3.one * (1.6f / Mathf.Max(0.1f, f[0].bounds.size.y)); }
        sr.sortingLayerID = SortingLayer.NameToID("Character");
        sr.sortingOrder = -Mathf.RoundToInt(at.y * 10f);
        yield return new WaitForSeconds(2f);
        if (cam != null) Destroy(cam);
        if (!Alive || Kit == null) yield break;
        FirstImpact("x.timer", new Vector3(at.x, at.y, 0f));
        Kit.PhShootExtra(at, new Vector2(PhotographerTuning.FrameW, PhotographerTuning.FrameH), V(lv, 0.6f, 0.2f) * P("x.timer"), true);
    }

    // 사본이 인화됨: 스튜디오 (조명이 3초 서서 사본에 끌린 적을 0.5초마다 1초 종이로)
    public static void PhOnPrint(Vector2 at, int copies)
    {
        if (Instance == null || !Instance.IsPh || !Instance.E("xe.studio")) return;
        Instance.StartCoroutine(Instance.PhStudio(at));
    }

    IEnumerator PhStudio(Vector2 at)
    {
        FirstEvoImpact("xe.studio", new Vector3(at.x, at.y, 0f));
        for (float t = 0f; t < 3f && Alive; t += 0.5f)
        {
            Fx.Spawn("fx_flashframe", new Vector3(at.x, at.y, 0f), 6f, new Color(1f, 0.95f, 0.8f, 0.35f), 8f, 0f, 19);
            using (var ovl = PhysHits.Circle(at, 3.5f)) foreach (Collider2D col in ovl)
            {
                if (col == null || !col.CompareTag("enermy")) continue;
                EnermyController e = EnermyController.FromCollider(col);
                if (e != null && !e.IsDead && Kit != null) Kit.PhPaperTake(e, 1f, false);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // 사본이 찢어짐: 현상소 (둘레 적을 끌어당김 · 스튜디오는 끌려온 적을 모두 종이로)
    public static void PhOnTear(Vector2 pos)
    {
        if (Instance == null || !Instance.IsPh) return;
        int lv = Instance.L("x.darkroom");
        if (lv <= 0) return;
        Instance.PhPull(pos, V(lv, 1.5f, 0.5f) * Instance.P("x.darkroom"));
    }

    void PhPull(Vector2 pos, float r)
    {
        Vector3 p = pos;
        bool studio = E("xe.studio");
        using (var ovl = PhysHits.Circle(p, r + 1.5f)) foreach (Collider2D col in ovl)
        {
            if (col == null || !col.CompareTag("enermy")) continue;
            EnermyController e = EnermyController.FromCollider(col);
            if (e == null || e.IsDead || e.IsMidBoss || e.swarm != null || e.holder != null) continue;
            Vector3 to = p - e.transform.position;
            float step = Mathf.Min(to.magnitude, r);
            Vector3 np = Hostile.ClampArena(e.transform.position + to.normalized * step);
            if (!Hostile.IsWall(np)) EnemyHold.Place(e, np);
            if (studio && Kit != null) Kit.PhPaperTake(e, 1.5f, false);
        }
        FirstImpact("x.darkroom", p);
    }

    // 종이 적 처치 누적: 앨범 (열 마리마다 필름 1장 + 게이지)
    public static void PhOnPaperKill(int total)
    {
        if (Instance == null || !Instance.IsPh) return;
        Instance.phKills = total;
        int lv = Instance.L("x.album");
        if (lv <= 0 || total % 10 != 0) return;
        PlayerController p = Instance.player;
        if (p == null) return;
        if (p.NowBullet < p.MaxBullet) p.NowBullet++;
        if (p.skillGauge != null) p.skillGauge.AddSkillPoint(p.skillGauge.MaxSkillPoint * V(lv, 0.02f, 0.01f) * Instance.P("x.album"));
        Instance.Flair("x.album", p.transform.position, 3f, PhTint);
        Instance.FirstImpact("x.album", p.transform.position);
    }

    void FirstImpact(string key, Vector3 at)
    {
        if (!phFirst.Add(key)) return;
        Impact.Hit(ImpactTier.Skill, at, Vector2.zero);
    }

    void FirstEvoImpact(string key, Vector3 at)
    {
        if (!phFirst.Add(key)) return;
        Impact.Hit(ImpactTier.Evolve, at, Vector2.zero);
    }

    // ---------------- 적이 받는 피해: 종이 적 x1.4 (흑백 사진 x1.6) · 보스 초점 표식 (+6%마다)
    float PhOutgoing(GameObject target)
    {
        if (Kit == null || target == null) return 1f;
        float m = 1f;
        EnermyController e = target.GetComponent<EnermyController>();
        if (e != null && Kit.PhIsPaper(e)) m *= Kit.PhPaperTakenMul;
        else if (target.CompareTag("boss") || (e != null && e.IsMidBoss)) m *= 1f + PhotographerTuning.MarkBonus * Kit.PhMarkCount;
        return m;
    }

    // 맞은 종이 적은 크게 밀림 (넉백 x2.5 에 해당)
    void PhHit(GameObject target, float dmg, bool killed, bool proc)
    {
        if (killed || Kit == null || target == null || player == null) return;
        EnermyController e = target.GetComponent<EnermyController>();
        if (e == null || !Kit.PhIsPaper(e)) return;
        Kit.PhKick(e, ((Vector2)(e.transform.position - player.transform.position)).normalized * 4f);
    }

    void PhTick() { }

    string PhStack(string key)
    {
        if (key == "x.album" && L(key) > 0) return (phKills % 10) + "/10";
        if (key == "x.selfie" && L(key) > 0 && Time.time < phSelfieAt) return (phSelfieAt - Time.time).ToString("0.0") + "s";
        return null;
    }
}
