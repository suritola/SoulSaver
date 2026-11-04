using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 사진사 (2.0.0): 마감 직전의 사진사 — docs/plans/2026-11-02-big-update/hero-photographer.md
//   평타 「셔터」: 커서 자리의 뷰파인더(3.6 x 2.7칸) 안 적을 모두 찍음 · 가운데 초점 원에 닿은 가장 가까운 하나는 초점 피해 · 틀 안 일반 적은 흰 테 종이가 됨
//     6마리 이상이면 걸작 (필름 반환 통마다 최대 2장 · 게이지 3%) · 보스는 종이가 되지 않고 초점 표식 (+6% 최대 5)
//   필름 6장 = 탄창 (PlayerController 의 탄창 흐름) · 다 쓰면(또는 재장전 키) 감기 시작하는 순간 이번 통에서 가장 많이 찍힌 사진을 커서 자리에 인화 (종이 사본 = 미끼)
//   종이 상태(PaperRoll): EnemyHold 로 붙잡고 owner 가 직접 움직임 (속도 30% + 펄럭임) · 본체 렌더러를 끄고 자식 렌더러에 같은 그림 (흑백 + 흰 테 재질, 세로 0.6 계단 배율)
//   필살기 「파노라마」: CharacterKit.Photographer.Ult · 갈래 5개는 범위 밖 (카드 · 고유 스킬 · 진화 · 특수 능력 · 강화까지)
//   상태는 끝나는 시각 값 · 저장하지 않음 (이어하면 빈 통)
// 레벨업 카드 (card): 0 장노출 · 1 광각 렌즈 · 2 현상액 · 3 바람에 날린 사진 · 5 자동 초점 · 6 인화지 추가 / 형태: 플래시 전구 · 삼각대 카메라 · 영사기 / 강화: 이중 노출 · 흑백 사진 · 보정
// 갈래 5개(연사 · 망원 · 암실 · 종이 공작 · 플래시)의 효과는 CharacterKit.Photographer.Paths (이 파일은 그 값을 읽는 자리만)
// 간소화: 조준 장식 필름 구멍 흐름 생략 · 파노라마 되튕김은 두 번째 바람으로 대신

// 판 안에서만 쓰는 「최고의 사진」 기록 (결과 화면 D-P6: 저장해 둔 스프라이트와 배치로 UI 에 다시 그림 · 화면 캡처 없음)
public static class PhotographerAlbum
{
    public struct Cut { public Sprite sprite; public bool flip; public Vector2 rel; }
    public static readonly Cut[] Best = new Cut[6];
    public static int BestN, BestCount;
    public static void Reset() { BestN = 0; BestCount = 0; }
    public static bool Has => BestN > 0;

    // 결과 화면 「이번 판 최고의 사진」 (D-P6): 저장해 둔 스프라이트와 배치로 흰 폴라로이드 틀 안에 UI Image 로 다시 그림 — 렌더 텍스처 · 캡처 없음 · 판 끝에 한 번
    public static void BuildResult(Transform canvas)
    {
        if (canvas == null || !Has) return;
        UnityEngine.UI.Image frame = new GameObject("PhotoBest", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        frame.transform.SetParent(canvas, false);
        frame.raycastTarget = false;
        frame.color = new Color(0.96f, 0.96f, 0.94f, 1f);
        RectTransform fr = frame.rectTransform;
        fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(0.5f, 0.5f);
        fr.anchoredPosition = new Vector2(640f, -10f);
        fr.sizeDelta = new Vector2(250f, 300f);
        fr.localRotation = Quaternion.Euler(0f, 0f, 3f);
        UnityEngine.UI.Image pic = new GameObject("Pic", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        pic.transform.SetParent(frame.transform, false);
        pic.raycastTarget = false;
        pic.color = new Color(0.16f, 0.17f, 0.2f, 1f);
        RectTransform pr = pic.rectTransform;
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = new Vector2(0f, 24f);
        pr.sizeDelta = new Vector2(214f, 214f);
        pic.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        for (int i = 0; i < BestN; i++)
        {
            Cut c = Best[i];
            if (c.sprite == null) continue;
            UnityEngine.UI.Image im = new GameObject("Cut" + i, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            im.transform.SetParent(pic.transform, false);
            im.raycastTarget = false;
            im.sprite = c.sprite;
            im.preserveAspect = true;
            RectTransform r = im.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(c.rel.x * 26f, c.rel.y * 26f);
            r.sizeDelta = new Vector2(60f, 60f);
            r.localScale = new Vector3(c.flip ? -1f : 1f, 1f, 1f);
        }
        TMPro.TMP_Text t = UIKit.Text(frame.transform, Loc.T("이번 판 최고의 사진") + "  ×" + BestCount, 22f, new Color(0.12f, 0.12f, 0.16f), new Vector2(0f, -122f), new Vector2(230f, 40f));
        t.name = "PhotoBestCaption";
    }
}

public class PhShot
{
    public readonly PhotographerAlbum.Cut[] cuts = new PhotographerAlbum.Cut[6];
    public int n, count;
    public float time;
}

public static class PhotographerTuning
{
    public const float FrameW = 3.6f, FrameH = 2.7f, FocusR = 0.7f, FocusMul = 1.6f, PaperSec = 2.5f, MidPaperSec = 1.2f, PaperImmune = 1.5f;
    public const float PaperMove = 0.3f, PaperTaken = 1.4f, MarkBonus = 0.06f, MarkLife = 4f;
    public const int FilmDefault = 6, PaperCap = 40, Masterpiece = 6, MarkMax = 5, RefundMax = 2, CopyCap = 12;
    public const float LureR = 7f, LureSec = 3f, CopyTearR = 1.4f, CopyTearMul = 0.8f, CopyLife = 3f;
}

public partial class CharacterKit
{
    static readonly Color PhFlash = new Color(0.78f, 0.83f, 1f);

    // ---------------- 종이 상태
    sealed class PhPaper
    {
        public EnermyController e;
        public SpriteRenderer body, child;
        public Material origMat;
        public float until, born, seed, flyUntil, nextHit;
        public Vector2 vel, flyVel;
        public readonly HashSet<int> flyHit = new HashSet<int>();
    }

    readonly Dictionary<EnermyController, PhPaper> phPapers = new Dictionary<EnermyController, PhPaper>(48);
    readonly List<PhPaper> phPaperList = new List<PhPaper>(48);
    readonly Dictionary<EnermyController, float> phImmune = new Dictionary<EnermyController, float>(48);
    readonly Stack<SpriteRenderer> phChildPool = new Stack<SpriteRenderer>(16);
    float phPruneAt;
    static readonly float[] PhWidthSteps = { 1f, 0.75f, 0.5f, 0.75f };

    // ---------------- 사진 · 통 · 보스 표식
    readonly List<PhShot> phRoll = new List<PhShot>(12);
    int phRefund, phMarks, phExpose, phPaperKills;
    float phMarkUntil;
    Vector2 phLastShotPos;
    bool phHasShot;
    float phStreakPitch = 1f;
    bool phInit;
    float phReloadBase = 1.5f;
    int phLastMax = 6;

    // ---------------- 뷰파인더 그림
    PixelMarks phPx;        // 2.0.0~ 도트 뷰파인더 · 필름 띠 · 영사기 필름 구멍 (안 그린 프레임엔 저절로 숨음)
    LineRenderer phFocusRing;
    TMPro.TextMeshPro phCountText;
    int phCount, phCountKey = -1;
    float phCountAt;
    Vector2 phCursorCache;
    float phCursorAt;
    Vector3 phPrevPos;
    Vector2 phMoveDir;

    static readonly List<EnermyController> phNormals = new List<EnermyController>(40);
    static readonly List<GameObject> phBosses = new List<GameObject>(4);
    static readonly List<GameObject> phMotes = new List<GameObject>(16);

    // ================================================================= 밖에서 읽는 값
    public bool PhIsPaper(EnermyController e) => e != null && phPapers.ContainsKey(e);
    public int PhMarkCount => Time.time < phMarkUntil ? phMarks : 0;
    public Vector2 PhCursorNow => PhCursor(true);
    public float PhFocusMul => (PhotographerPaths.On(PhotographerPaths.Tele) ? PhotographerPaths.TeleFocus : PhotographerTuning.FocusMul) + (augment == AugRetouch ? 0.3f : 0f);
    float PhFocusR => (PhotographerPaths.On(PhotographerPaths.Tele) ? PhotographerPaths.TeleFocusR : PhotographerTuning.FocusR) * (augment == AugRetouch ? 1.5f : 1f);
    public float PhPaperTakenMul => augment == AugMonochrome ? 1.6f : PhotographerTuning.PaperTaken;
    int PhFilmMax => PhFilmBase + (Special != null && Special.Has(SpecialAbilities.KitFilmCase) ? (Special.IsEvolved(SpecialAbilities.KitFilmCase) ? 2 : 1) : 0);

    // 틀 크기: 멀티샷 +12% / 광각 렌즈 +12% 씩 · 흔들린 사진은 이동 방향으로 늘어남
    Vector2 PhFrameSize(int shots)
    {
        float k = (1f + 0.12f * Mathf.Max(0, shots - 1)) * (1f + 0.12f * card[1]) * PhPathFrame;
        Vector2 s = new Vector2(PhotographerTuning.FrameW, PhotographerTuning.FrameH) * k;
        float ext = SignatureSkills.PhMotionExtra();
        if (ext > 0f && phMoveDir.sqrMagnitude > 0.01f) s += new Vector2(Mathf.Abs(phMoveDir.x), Mathf.Abs(phMoveDir.y)) * ext;
        return s;
    }

    // ================================================================= 커서 (사거리 안 · 자동 초점이 큰 적에게 당김)
    Vector2 PhCursor(bool fresh = false)
    {
        if (!fresh && Time.unscaledTime - phCursorAt < 0.05f) return phCursorCache;
        phCursorAt = Time.unscaledTime;
        Vector3 me = transform.position;
        Vector3 m = ClampRange(me, Mouse, PhRange);
        float pull = card[5] > 0 ? 1f + 0.5f * card[5] : 0f;
        phLocked = null;
        if (pull > 0f)
        {
            bool hp = SignatureSkills.PhTrack();
            EnermyController best = null;
            float bestScore = float.MinValue;
            using (var ovl = PhysHits.Circle(m, pull + 1f)) foreach (Collider2D c in ovl)
            {
                if (!c.CompareTag("enermy")) continue;
                EnermyController e = EnermyController.FromCollider(c);
                if (e == null || e.IsDead || e.swarm != null) continue;
                if (Vector2.Distance(m, e.transform.position) > pull) continue;
                float score = hp ? e.EnemyHealth : c.bounds.size.x * c.bounds.size.y;
                if (score > bestScore) { bestScore = score; best = e; }
            }
            if (best != null) { m = ClampRange(me, best.transform.position, PhRange); phLocked = hp ? best : null; }
        }
        phCursorCache = new Vector2(m.x, m.y);
        return phCursorCache;
    }

    // ================================================================= 평타 「셔터」
    void PhotoAttack(float dmg, int shots)
    {
        dmg *= PhotographerPaths.ShutterMul;        // 연사 −35% · 종이 공작 −30%
        Vector2 c = PhCursor(true);
        Vector2 size = PhFrameSize(shots);
        PlayerController.RaiseAttackFired(transform.position, (c - (Vector2)transform.position).normalized);
        int n = PhShoot(c, size, dmg, true, true, -1f);
        // 연속 셔터 소리: 장마다 피치 +2% (감기에서 처음으로)
        phStreakPitch = Mathf.Min(1.12f, phStreakPitch + 0.02f);
        // 장노출 (카드): 틀이 잠시 남아 지나가는 적도 찍음 (피해 30%)
        if (card[0] > 0) StartCoroutine(PhLongExposure(c, size, 0.3f + 0.3f * card[0]));
        // 이중 노출 (강화): 0.3초 뒤 같은 틀 한 번 더 (피해 50% · 종이 없음)
        if (augment == AugDoubleExposure) StartCoroutine(PhAgain(c, size, dmg * 0.5f, 0.3f, false));
        // 플래시 전구 (형태): 틀 둘레 1칸 테두리에도 전구 불꽃 (피해 40%)
        if (form == SpecialAbilities.KitBulb) PhBulbRim(c, size, dmg * 0.4f);
        // 셔터 소리 (패시브): 내 둘레 2칸 밀쳐 냄
        if (Special != null && Special.Has(SpecialAbilities.KitShutterSound)) PhShove(Special.IsEvolved(SpecialAbilities.KitShutterSound) ? 3f : 2f);
        SignatureSkills.PhOnNight(size);
        PhPathStrobe(c, size);
        phLastShotPos = c;
        phHasShot = true;
        if (n <= 0) return;
    }

    // 한 번 찍기. real = 진짜 셔터 (사진 기록 · 걸작 · 카운터) · paper = 종이로 만들기 · 반환: 틀 안 일반 적 수
    public int PhShoot(Vector2 c, Vector2 size, float dmg, bool paper, bool real, float paperSec)
    {
        phNormals.Clear(); phBosses.Clear(); phMotes.Clear();
        using (var ovl = PhysHits.Box(c, size, 0f)) foreach (Collider2D col in ovl)
        {
            if (col == null) continue;
            if (col.CompareTag("boss")) { phBosses.Add(col.gameObject); continue; }
            if (!col.CompareTag("enermy")) continue;
            EnermyController e = EnermyController.FromCollider(col);
            if (e == null || e.IsDead) continue;
            if (e.swarm != null) { phMotes.Add(e.gameObject); continue; }
            if (!phNormals.Contains(e)) phNormals.Add(e);
        }
        int n = phNormals.Count;
        bool master = real && n >= PhotographerTuning.Masterpiece;
        // 초점: 초점 원에 닿은 가장 가까운 하나 (일반 적 · 보스) · 망원 극의는 둘
        float fr = PhFocusR + 0.5f;
        EnermyController focusE = null, focusE2 = null; GameObject focusB = null, focusB2 = null;
        PhPickFocus(c, fr, null, null, out focusE, out focusB);
        if (PhotographerPaths.Master(PhotographerPaths.Tele)) PhPickFocus(c, fr, focusE, focusB, out focusE2, out focusB2);
        float groupMul = phBosses.Count > 0 && n > 0 ? SignatureSkills.PhGroupMul(Mathf.Min(6, n)) : 1f;
        Vector3 c3 = new Vector3(c.x, c.y, 0f);
        Vector3 me3 = transform.position;
        bool tele = PhotographerPaths.On(PhotographerPaths.Tele);
        PhotographerAlbum.Cut[] cuts = real ? new PhotographerAlbum.Cut[6] : null;
        int cutN = 0;
        for (int i = 0; i < phNormals.Count; i++)
        {
            EnermyController e = phNormals[i];
            if (e == null || e.IsDead) continue;
            Vector3 ep = e.transform.position;
            bool wasPaper = phPapers.ContainsKey(e);
            float d = dmg;
            bool isFocus = e == focusE || e == focusE2;
            if (isFocus) { d *= PhFocusMul; DamagePopup.NextCrit = true; if (real) RunCounters.Both("focus"); }
            // 망원 대가: 나에게서 6칸 안은 초점이 안 맞아 피해 −50% · 종이가 안 됨
            bool blur = tele && Vector2.Distance(ep, me3) < PhotographerPaths.TeleNear;
            if (blur) d *= PhotographerPaths.TeleNearCost;
            if (real) d *= SignatureSkills.PhChance(e);
            if (real && cutN < 6)
            {
                SpriteRenderer sr = e.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) cuts[cutN++] = new PhotographerAlbum.Cut { sprite = sr.sprite, flip = sr.flipX, rel = (Vector2)ep - c };
            }
            Specials.Damage(e.gameObject, d, (ep - c3).normalized, 0f);
            if (e == null || e.IsDead) continue;
            if (real) PhPathSnap(e, ep - c3);
            if (e == null || e.IsDead) continue;
            if (paper && !blur)
            {
                if (wasPaper && real) SignatureSkills.PhCutout(e);
                float sec = paperSec > 0f ? paperSec : (e.IsMidBoss ? PhotographerTuning.MidPaperSec : PhPaperSec);
                if (isFocus && PhotographerPaths.ResAt(PhotographerPaths.Tele, 1)) sec *= PhotographerPaths.TeleRes1Paper;
                PhPaperTake(e, sec, false);
            }
        }
        foreach (GameObject mote in phMotes) if (mote != null) Specials.Damage(mote, dmg, Vector3.zero, 0f);
        foreach (GameObject b in phBosses)
        {
            if (b == null) continue;
            float d = dmg * groupMul;
            bool isFocus = b == focusB || b == focusB2;
            if (isFocus) { d *= PhFocusMul; DamagePopup.NextCrit = true; }
            if (tele && Vector2.Distance(b.transform.position, me3) < PhotographerPaths.TeleNear) d *= PhotographerPaths.TeleNearCost;
            Specials.Damage(b, d, ((Vector2)b.transform.position - c).normalized, 0f);
            if (real) PhBossFocus(b, isFocus);
        }
        // 틀에 든 인화 사본은 찢어짐
        PhTearCopiesIn(c, size);
        Fx.Spawn("fx_flashframe", c3, size.y * 1.05f, PhFlashSafe(SkinFx.FlashTint(new Color(1f, 1f, 1f, 0.9f))), 24f, 0f, 20);      // 신문 기자 스킨: 조금 노랗게 (설명 약속)
        if (!real) return n;

        RunCounters.Both("photo");
        if (phBosses.Count > 0 && n > 0) RunCounters.Both("photo.withboss");
        Hostile.Play("shutter", 0.3f, 1.7f * phStreakPitch);      // 2026-11-04~ D2: 무기 스킨(일회용 · 필름 카메라)이 셔터 소리를 알아보게 (파형은 clank 그대로)
        PhRecord(cuts, cutN, n);
        SignatureSkills.PhOnShutter(c, size, n, master);
        if (master)
        {
            RunCounters.Both("photo.masterpiece");
            if (phRefund < PhotographerTuning.RefundMax && player.NowBullet < player.MaxBullet) { player.NowBullet++; phRefund++; }
            if (player.skillGauge != null) player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * 0.03f);
            if (Special != null && Special.Has(SpecialAbilities.KitModel)) PhHeal(Special.IsEvolved(SpecialAbilities.KitModel) ? 4f : 2f);
            Impact.Hit(ImpactTier.Attack, c3, ((Vector2)(c3 - transform.position)).normalized);
            PhStar(c3);
        }
        return n;
    }

    void PhStar(Vector3 at) => Fx.Spawn("fx_bulb", at, 2.4f, PhFlash, 20f, 0f, 22);

    void PhHeal(float amount)
    {
        if (player == null) return;
        player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + amount);
    }

    void PhAddMarks(int add)
    {
        if (Time.time >= phMarkUntil) phMarks = 0;
        phMarks = Mathf.Min(PhotographerTuning.MarkMax, phMarks + add);
        phMarkUntil = Time.time + PhotographerTuning.MarkLife;
    }

    // 사진 기록: 이번 통의 후보로 저장 · 판 안 최고 기록은 결과 화면용
    void PhRecord(PhotographerAlbum.Cut[] cuts, int cutN, int count)
    {
        PhShot s = new PhShot { n = cutN, count = count, time = Time.time };
        for (int i = 0; i < cutN; i++) s.cuts[i] = cuts[i];
        if (phRoll.Count >= 12) phRoll.RemoveAt(0);
        phRoll.Add(s);
        if (count > 0 && count >= PhotographerAlbum.BestCount)
        {
            PhotographerAlbum.BestCount = count;
            PhotographerAlbum.BestN = cutN;
            for (int i = 0; i < cutN; i++) PhotographerAlbum.Best[i] = cuts[i];
        }
    }

    IEnumerator PhLongExposure(Vector2 c, Vector2 size, float sec)
    {
        if (phExpose >= (PhotographerPaths.On(PhotographerPaths.Burst) ? 5 : 3)) yield break;
        phExpose++;
        HashSet<int> done = new HashSet<int>();
        PhExpose me = new PhExpose { rect = new Rect(c - size * 0.5f, size) };
        phExposes.Add(me);
        float dmg = Damage * 0.3f * formPower;
        Fx.Spawn("fx_flashframe", new Vector3(c.x, c.y, 0f), size.y, PhFlashSafe(new Color(1f, 1f, 1f, 0.35f)), 8f, 0f, 19);
        for (float t = 0f; t < sec; t += 0.1f)
        {
            yield return new WaitForSeconds(0.1f);
            if (player == null || player.IsDying) break;
            using (var ovl = PhysHits.Box(c, size, 0f)) foreach (Collider2D col in ovl)
            {
                if (col == null || (!col.CompareTag("enermy") && !col.CompareTag("boss"))) continue;
                if (!done.Add(col.GetInstanceID())) continue;
                Specials.Damage(col.gameObject, dmg * PhExposeOverlap(me, col.transform.position), Vector3.zero, 0f);
            }
        }
        phExposes.Remove(me);
        phExpose--;
    }

    IEnumerator PhAgain(Vector2 c, Vector2 size, float dmg, float delay, bool paper)
    {
        yield return new WaitForSeconds(delay);
        if (player == null || player.IsDying) yield break;
        PhShoot(c, size, dmg, paper, false, -1f);
    }

    // 다른 곳(고유 스킬 · 진화)에서 같은 틀을 한 번 더 찍을 때
    public void PhShootExtra(Vector2 c, Vector2 size, float dmgMul, bool paper) => PhShoot(c, size, Damage * dmgMul * formPower, paper, false, -1f);

    void PhBulbRim(Vector2 c, Vector2 size, float dmg)
    {
        HashSet<int> inner = new HashSet<int>();
        using (var ovl = PhysHits.Box(c, size, 0f)) foreach (Collider2D col in ovl) if (col != null) inner.Add(col.GetInstanceID());
        using (var ovl = PhysHits.Box(c, size + Vector2.one * 2f, 0f)) foreach (Collider2D col in ovl)
        {
            if (col == null || (!col.CompareTag("enermy") && !col.CompareTag("boss")) || inner.Contains(col.GetInstanceID())) continue;
            Specials.Damage(col.gameObject, dmg, Vector3.zero, 0f);
            Fx.Spawn("fx_bulb", col.transform.position, 1.2f, PhFlash, 20f, 0f, 20);
        }
    }

    void PhShove(float r)
    {
        Vector3 me = transform.position;
        using (var ovl = PhysHits.Circle(me, r)) foreach (Collider2D col in ovl)
        {
            if (col == null || !col.CompareTag("enermy")) continue;
            EnermyController e = EnermyController.FromCollider(col);
            if (e == null || e.IsDead || e.IsMidBoss || e.swarm != null || e.holder != null) continue;
            Vector3 to = (e.transform.position - me);
            Vector3 p = Hostile.ClampArena(e.transform.position + to.normalized * 0.8f);
            if (!Hostile.IsWall(p)) EnemyHold.Place(e, p);
        }
    }

    // 2.0.1~ 셔터 흰 번쩍임(fx_flashframe): 설정 「번쩍임 효과」를 끄면 알파 0.25 까지만 (광과민 접근성 — 예전엔 0.9 그대로 번쩍였음)
    public static Color PhFlashSafe(Color c) => GameSettings.Flashes ? c : new Color(c.r, c.g, c.b, Mathf.Min(c.a, 0.25f));

    // ================================================================= 종이 (PaperRoll)
    // 2.0.1~ renew: 이미 종이인 적의 시간을 늘림 — 명시한 경로(필름 끊김 공명 · 필살기)만 · 상한 처음 종이 된 시각 + 기본 시간 + sec · 중간 보스는 늘리지 않음
    //   (예전엔 다시 찍을 때마다 until 만 늘려 무한히 종이로 묶였고, 중간 보스도 시전이 계속 막혔음)
    public bool PhPaperTake(EnermyController e, float sec, bool ignoreImmune, bool renew = false)
    {
        if (e == null || e.IsDead || e.swarm != null) return false;
        float now = Time.time;
        if (phPapers.TryGetValue(e, out PhPaper cur))
        {
            if (renew && !e.IsMidBoss) cur.until = Mathf.Min(Mathf.Max(cur.until, now + sec), cur.born + PhPaperSec + sec);
            return true;
        }
        if (!ignoreImmune && phImmune.TryGetValue(e, out float im) && now < im) return false;
        if (phPaperList.Count >= PhotographerTuning.PaperCap) PhPaperEnd(phPaperList[0], false);
        if (e.casting)
        {
            EnemySkill sk = e.GetComponent<EnemySkill>();
            if (sk != null) sk.CancelCast();      // 시전 중에 종이가 되면 그 시전은 취소
        }
        if (!EnemyHold.TryTake(e, this, true)) return false;
        SpriteRenderer body = e.GetComponent<SpriteRenderer>();
        if (body == null) { EnemyHold.Release(e, this); return false; }
        SpriteRenderer child = null;
        while (phChildPool.Count > 0 && child == null) child = phChildPool.Pop();
        if (child == null)
        {
            GameObject go = new GameObject("PhPaper");
            child = go.AddComponent<SpriteRenderer>();
        }
        child.gameObject.SetActive(true);
        child.transform.SetParent(e.transform, false);
        child.transform.localPosition = Vector3.zero;
        Material pm = SpriteOutline.Paper;
        if (pm != null) child.sharedMaterial = pm;
        PhPaper p = new PhPaper { e = e, body = body, child = child, origMat = body.sharedMaterial, until = now + sec, born = now, seed = Random.value * 4f };
        phPapers[e] = p;
        phPaperList.Add(p);
        body.enabled = false;
        PhSyncChild(p, true);
        Fx.Spawn("fx_paperize", e.transform.position, 1.8f, Color.white, 18f, 0f, 22);
        RunCounters.Both("paper");
        return true;
    }

    // 본체의 그림 · 방향 · 정렬을 베끼고 계단 배율로 납작하게 (콜라이더는 그대로)
    void PhSyncChild(PhPaper p, bool force)
    {
        SpriteRenderer b = p.body, ch = p.child;
        if (b == null || ch == null) return;
        ch.sprite = b.sprite;
        ch.flipX = b.flipX;
        ch.sortingLayerID = b.sortingLayerID;
        ch.sortingOrder = b.sortingOrder + 1;
        bool flashing = b.sharedMaterial != p.origMat && b.sharedMaterial != null;
        Material pm = SpriteOutline.Paper;
        if (flashing) ch.sharedMaterial = b.sharedMaterial;        // 피격 흰 번쩍임을 같이
        else if (pm != null) ch.sharedMaterial = pm;
        int step = (int)((Time.time - p.born) / 0.15f + p.seed) % 4;
        ch.transform.localScale = new Vector3(PhWidthSteps[step], 0.6f, 1f);
        ch.transform.localRotation = Quaternion.Euler(0f, 0f, step == 1 ? 8f : step == 3 ? -8f : 0f);
    }

    void PhPaperEnd(PhPaper p, bool killed)
    {
        if (p == null) return;
        phPapers.Remove(p.e);
        phPaperList.Remove(p);
        if (p.body != null) p.body.enabled = true;
        if (p.child != null)
        {
            p.child.gameObject.SetActive(false);
            p.child.transform.SetParent(transform, false);
            phChildPool.Push(p.child);
        }
        if (p.e != null)
        {
            EnemyHold.Release(p.e, this);
            phImmune[p.e] = Time.time + PhotographerTuning.PaperImmune;
        }
    }

    void PhPapersTick()
    {
        float dt = Time.deltaTime;
        Vector3 me = transform.position;
        for (int i = phPaperList.Count - 1; i >= 0; i--)
        {
            PhPaper p = phPaperList[i];
            EnermyController e = p.e;
            if (e == null || e.IsDead || e.holder != (object)this || !e.isActiveAndEnabled || Time.time >= p.until || p.body == null)
            {
                PhPaperEnd(p, e == null || e.IsDead);
                continue;
            }
            Vector3 pos = e.transform.position;
            Vector2 move;
            bool flying = Time.time < p.flyUntil;
            if (flying)
            {
                move = p.flyVel;
                if (Time.time >= p.nextHit) { p.nextHit = Time.time + 0.06f; PhFlyHit(p, pos); }
            }
            else
            {
                Vector2 to = ((Vector2)(me - pos));
                Vector2 dir = to.sqrMagnitude > 0.04f ? to.normalized : Vector2.zero;
                Vector2 side = new Vector2(-dir.y, dir.x) * (Mathf.Cos((Time.time + p.seed) * 2f * Mathf.PI * 1.6f) * 0.6f * 2f * Mathf.PI * 1.6f);
                move = dir * (e.speed * PhotographerTuning.PaperMove * EnermyController.GlobalSpeedMultiplier) + side * 0.35f + p.vel;
                p.vel = Vector2.MoveTowards(p.vel, Vector2.zero, 8f * dt);
            }
            Vector3 np = pos + (Vector3)(move * dt);
            np = Hostile.ClampArena(np);
            if (!Hostile.IsWall(np)) EnemyHold.Place(e, np);
            else if (flying) p.flyUntil = 0f;
            PhSyncChild(p, false);
        }
        if (Time.time >= phPruneAt)
        {
            phPruneAt = Time.time + 2f;
            List<EnermyController> gone = null;
            foreach (var kv in phImmune) if (kv.Key == null || Time.time >= kv.Value) (gone ??= new List<EnermyController>()).Add(kv.Key);
            if (gone != null) foreach (EnermyController g in gone) phImmune.Remove(g);
        }
    }

    // 날아가는 종이 적의 충돌: 만난 일반 적과 둘 다 피해 (바람에 날린 사진 카드는 맞은 적도 종이)
    void PhFlyHit(PhPaper p, Vector3 pos)
    {
        float mul = card[3] > 0 ? 0.4f + 0.2f * card[3] : 0.5f;
        using (var ovl = PhysHits.Circle(pos, 0.8f)) foreach (Collider2D col in ovl)
        {
            if (col == null || !col.CompareTag("enermy")) continue;
            EnermyController o = EnermyController.FromCollider(col);
            if (o == null || o == p.e || o.IsDead || o.swarm != null || !p.flyHit.Add(o.GetInstanceID())) continue;
            Vector3 d = (o.transform.position - pos).normalized;
            Specials.Damage(o.gameObject, Damage * mul * formPower, d, 0f);
            if (p.e != null && !p.e.IsDead) Specials.Damage(p.e.gameObject, Damage * mul * formPower, -d, 0f);
            if (card[3] > 0 && o != null && !o.IsDead) PhPaperTake(o, 1.5f, true);
            Fx.Spawn("fx_paperize", pos, 1.6f, Color.white, 18f, 0f, 22);
            RunCounters.Both("wind.hit");
            if (p.e == null || p.e.IsDead) break;
        }
    }

    // 맞은 종이 적이 밀림 (SignatureSkills.PhHit · 넉백 x2.5 에 해당)
    public void PhKick(EnermyController e, Vector2 vel)
    {
        if (e != null && phPapers.TryGetValue(e, out PhPaper p) && p.vel.sqrMagnitude < 1f) p.vel = vel;
    }

    // 날려 보내기 (파노라마의 바람)
    public void PhFly(EnermyController e, Vector2 dir, float dist, float sec)
    {
        if (e == null || !phPapers.TryGetValue(e, out PhPaper p)) return;
        p.flyVel = dir.normalized * (dist / Mathf.Max(0.1f, sec));
        p.flyUntil = Time.time + sec;
        p.flyHit.Clear();
        p.until = Mathf.Max(p.until, Time.time + sec + 0.2f);
    }

    // 종이 적 처치 (OnKill): 찢김 조각 · 현상액 · 앨범
    void PhKilled(Vector3 pos)
    {
        EnermyController dying = EnermyController.Dying;
        if (dying == null || !phPapers.ContainsKey(dying)) return;
        Fx.Spawn("fx_papertear", pos, 2.2f, Color.white, 16f, 0f, 22);
        Play("whoosh", 0.2f, 2f);
        RunCounters.Both("paper.kill");
        phPaperKills++;
        SignatureSkills.PhOnPaperKill(phPaperKills);
        int shards = PhShardCount;
        if (shards > 0) PhShards(pos, shards, 1);
    }

    // 현상액: 종잇조각이 가까운 적에게 날아가 벰 (공격력 50% · 맞은 적 0.8초 종이) · 색종이 폭풍은 한 세대 더
    public void PhShards(Vector3 from, int count, int gen)
    {
        List<Transform> near = SignatureSkills.Nearest(from, 8f, count, EnermyController.Dying != null ? EnermyController.Dying.gameObject : null);
        foreach (Transform t in near)
        {
            if (t == null) continue;
            Fx.Spawn("fx_papertear", t.position, 1.4f, Color.white, 18f, 0f, 22);
            Specials.Damage(t.gameObject, Damage * 0.5f * formPower, (t.position - from).normalized, 0f);
            EnermyController e = t != null ? t.GetComponent<EnermyController>() : null;
            if (e != null && !e.IsDead) PhPaperTake(e, PhShardPaper, false);
            if (gen < PhShardGens && (SignatureSkills.PhConfetti() || PhotographerPaths.Master(PhotographerPaths.Craft)) && t != null) PhShards(t.position, 1, gen + 1);
        }
    }

    // ================================================================= 인화 (필름 감기 시작) · 종이 사본 미끼
    sealed class PhCopy
    {
        public Transform tf;
        public SpriteRenderer sr;
        public float until, born;
        public Vector2 pos;
        public readonly List<EnermyController> lured = new List<EnermyController>(8);
    }
    readonly List<PhCopy> phCopies = new List<PhCopy>(16);
    float phCopyCheckAt;

    // PlayerController.Reload 가 감기 시작할 때 부름
    public void PhReloadStart()
    {
        if (Id != CharacterId.Photographer) return;      // 2.0.0~ 검토: 탄창 영웅(도적 등)의 장전에도 불려 「clank」 소리가 났음
        // 이번 통의 사진을 많이 찍힌 순서로 (같으면 나중 것이 앞) — 암실은 두 장 · 극의는 세 장 인화
        List<PhShot> picks = new List<PhShot>(3);
        foreach (PhShot s in phRoll) if (s.count > 0) picks.Add(s);
        picks.Sort((a, b) => a.count != b.count ? b.count.CompareTo(a.count) : b.time.CompareTo(a.time));
        phRoll.Clear();
        phRefund = 0;
        phStreakPitch = 1f;
        SignatureSkills.PhOnRewind(PhCursor(true));
        if (picks.Count == 0) { Play("clank", 0.2f, 0.6f); return; }
        Vector2 cur = PhCursor(true);
        int prints = Mathf.Min(picks.Count, PhPrintCount);
        for (int i = 0; i < prints; i++)
            PhPrint(picks[i], i == 0 ? cur : cur + (Vector2)(Quaternion.Euler(0f, 0f, 120f * i) * Vector3.right * 4f));
    }

    void PhPrint(PhShot s, Vector2 at)
    {
        Vector3 at3 = Hostile.ClampArena(new Vector3(at.x, at.y, 0f));
        at = at3;
        RunCounters.Both("print");
        Fx.Spawn("fx_print", at3 + Vector3.up * 1.2f, 3f, Color.white, 14f, 0f, 22);
        Friendly.Circle(at3, PhotographerTuning.LureR, 0.5f, new Color(PhFlash.r, PhFlash.g, PhFlash.b, 0.6f));
        int extra = card[6];
        int total = Mathf.Min(PhPerPrintCap, Mathf.Max(1, s.n) + extra);
        int cap = PhCopyCap;
        while (phCopies.Count + total > cap && phCopies.Count > 0) PhRemoveCopy(phCopies[0], false);
        List<PhCopy> made = new List<PhCopy>(total);
        float life = (PhotographerPaths.On(PhotographerPaths.Dark) ? PhotographerPaths.DarkLure : PhotographerTuning.LureSec) + SignatureSkills.PhLureExtra() + 0.5f * card[6];
        for (int i = 0; i < total; i++)
        {
            PhotographerAlbum.Cut cut = s.n > 0 ? s.cuts[i % s.n] : default;
            Vector2 rel = i < s.n ? cut.rel * 0.8f : Random.insideUnitCircle * 1.4f;
            Vector3 p = Hostile.ClampArena(at3 + (Vector3)rel);
            if (Hostile.IsWall(p)) p = at3;
            GameObject go = new GameObject("PhCopy");
            go.transform.position = p;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = cut.sprite;
            sr.flipX = cut.flip;
            Material pm = SpriteOutline.Paper;
            if (pm != null) sr.sharedMaterial = pm;
            sr.sortingLayerID = SortingLayer.NameToID("Character");
            sr.sortingOrder = -Mathf.RoundToInt(p.y * 10f);
            go.transform.localScale = new Vector3(0.75f, 0.6f, 1f);
            if (cut.sprite == null) { sr.sprite = Hostile.Glow; sr.color = new Color(1f, 1f, 1f, 0.8f); go.transform.localScale = Vector3.one * 0.6f; }
            PhCopy c = new PhCopy { tf = go.transform, sr = sr, until = Time.time + life + 0.2f, born = Time.time, pos = p };
            phCopies.Add(c);
            made.Add(c);
        }
        // 미끼: 둘레 7칸 일반 적은 가장 가까운 사본을 쫓음 (보스 · 중간 보스 제외)
        using (var ovl = PhysHits.Circle(at3, PhotographerTuning.LureR)) foreach (Collider2D col in ovl)
        {
            if (col == null || !col.CompareTag("enermy")) continue;
            EnermyController e = EnermyController.FromCollider(col);
            if (e == null || e.IsDead || e.IsMidBoss || e.swarm != null) continue;
            PhCopy nearest = NearestCopy(e.transform.position, made);
            if (nearest == null) continue;
            e.lure = nearest.tf;
            e.lureUntil = Time.time + life;
            nearest.lured.Add(e);
        }
        Play("whoosh", 0.3f, 1.8f);
        SignatureSkills.PhOnPrint(at, made.Count);
    }

    static PhCopy NearestCopy(Vector3 from, List<PhCopy> list)
    {
        PhCopy best = null; float bd = float.MaxValue;
        foreach (PhCopy c in list)
        {
            if (c.tf == null) continue;
            float d = ((Vector2)from - c.pos).sqrMagnitude;
            if (d < bd) { bd = d; best = c; }
        }
        return best;
    }

    void PhCopiesTick()
    {
        if (phCopies.Count == 0 || Time.time < phCopyCheckAt) return;
        phCopyCheckAt = Time.time + 0.1f;
        for (int i = phCopies.Count - 1; i >= 0; i--)
        {
            if (i >= phCopies.Count) continue;
            PhCopy c = phCopies[i];
            if (c.tf == null) { phCopies.RemoveAt(i); continue; }
            if (Time.time >= c.until) { PhRemoveCopy(c, true); continue; }
            if (PhotographerPaths.CrossIs(PhotographerPaths.PuppetCross)) PhPuppetStep(c);
            bool touched = false;
            using (var ovl = PhysHits.Circle(c.pos, 0.7f)) foreach (Collider2D col in ovl)
                if (col != null && col.CompareTag("enermy")) { EnermyController e = EnermyController.FromCollider(col); if (e != null && !e.IsDead && e.holder == null) { touched = true; break; } }
            if (touched) PhRemoveCopy(c, true);
        }
    }

    void PhTearCopiesIn(Vector2 c, Vector2 size)
    {
        if (phCopies.Count == 0) return;
        Rect r = new Rect(c - size * 0.5f, size);
        for (int i = phCopies.Count - 1; i >= 0; i--)
            if (i < phCopies.Count && phCopies[i].tf != null && r.Contains(phCopies[i].pos)) PhRemoveCopy(phCopies[i], true);
    }

    // 사본이 찢어짐: 그 자리 종이 베임 (반지름 1.4 · 공격력 80% · 맞은 일반 적 종이 1.2초)
    void PhRemoveCopy(PhCopy c, bool tear)
    {
        phCopies.Remove(c);
        Vector2 pos = c.pos;
        // 이 사본을 쫓던 적은 남은 사본으로
        foreach (EnermyController e in c.lured)
        {
            if (e == null || e.lure != c.tf) continue;
            PhCopy other = null; float bd = float.MaxValue;
            foreach (PhCopy o in phCopies) { if (o.tf == null) continue; float d = (o.pos - (Vector2)e.transform.position).sqrMagnitude; if (d < bd) { bd = d; other = o; } }
            e.lure = other != null ? other.tf : null;
            if (other != null) other.lured.Add(e);
        }
        if (c.tf != null) Destroy(c.tf.gameObject);
        if (!tear) return;
        Vector3 p3 = new Vector3(pos.x, pos.y, 0f);
        Fx.Spawn("fx_papercopy_tear", p3, PhotographerTuning.CopyTearR * 2.2f, Color.white, 16f, 0f, 22);
        Play("whoosh", 0.25f, 2.2f);
        RunCounters.Both("print.tear");
        float dmg = Damage * PhotographerTuning.CopyTearMul * formPower * (PhotographerPaths.On(PhotographerPaths.Dark) ? PhotographerPaths.DarkTear : 1f);
        using (var ovl = PhysHits.Circle(p3, PhotographerTuning.CopyTearR)) foreach (Collider2D col in ovl)
        {
            if (col == null || (!col.CompareTag("enermy") && !col.CompareTag("boss"))) continue;
            Specials.Damage(col.gameObject, dmg, (col.transform.position - p3).normalized, 0f);
            if (col != null && col.CompareTag("enermy"))
            {
                EnermyController e = EnermyController.FromCollider(col);
                if (e != null && !e.IsDead && !e.IsMidBoss) PhPaperTake(e, 1.2f, false);
            }
        }
        SignatureSkills.PhOnTear(pos);
        PhPathCurtain(p3);
    }

    // ================================================================= 형태: 삼각대 카메라 · 영사기
    readonly List<(Transform tf, float until)> phTripods = new List<(Transform, float)>(2);
    float phTripodAt, phProjAt;
    readonly List<EnermyController> phStrip = new List<EnermyController>(6);
    LineRenderer phStripLine;

    void PhFormTick()
    {
        if (form == SpecialAbilities.KitTripodCam && phHasShot && Time.time >= phTripodAt)
        {
            phTripodAt = Time.time + 3f;
            for (int i = phTripods.Count - 1; i >= 0; i--) if (phTripods[i].tf == null || Time.time >= phTripods[i].until) phTripods.RemoveAt(i);
            if (phTripods.Count < 2)
            {
                Vector2 at = phLastShotPos;
                GameObject go = new GameObject("PhTripodCam");
                go.transform.position = new Vector3(at.x, at.y, 0f);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                Sprite[] f = Fx.Frames("fx_tripodcam");
                if (f != null && f.Length > 0) { sr.sprite = f[0]; go.transform.localScale = Vector3.one * (1.6f / Mathf.Max(0.1f, f[0].bounds.size.y)); }
                sr.sortingLayerID = SortingLayer.NameToID("Character");
                sr.sortingOrder = -Mathf.RoundToInt(at.y * 10f);
                phTripods.Add((go.transform, Time.time + 5.2f));
                StartCoroutine(PhTripodShoots(at, go));
            }
        }
        // 영사기: 한 장에 찍힌 종이 적들이 필름 줄로 이어져 줄을 가로지르는 적이 0.4초마다 베임 (피해 30%)
        if (form == SpecialAbilities.KitProjector)
        {
            if (Time.time >= phProjAt)
            {
                phProjAt = Time.time + 0.4f;
                phStrip.Clear();
                foreach (PhPaper p in phPaperList) if (p.e != null && !p.e.IsDead && phStrip.Count < 6) phStrip.Add(p.e);
                for (int i = 0; i + 1 < phStrip.Count; i++)
                {
                    Vector3 a = phStrip[i].transform.position, b = phStrip[i + 1].transform.position;
                    if ((a - b).sqrMagnitude > 100f) continue;
                    using (var ovl = PhysHits.Circle((a + b) * 0.5f, Vector3.Distance(a, b) * 0.5f + 0.6f)) foreach (Collider2D col in ovl)
                    {
                        if (col == null || (!col.CompareTag("enermy") && !col.CompareTag("boss"))) continue;
                        if (Hostile.DistanceToSegment(col.transform.position, a, b) > 0.6f) continue;
                        Specials.Damage(col.gameObject, Damage * 0.3f * formPower, Vector3.zero, 0f);
                    }
                }
            }
            // 2.0.0~ 필름 줄: 어두운 필름 띠(굵게) + 띠 위 필름 구멍이 흐름 (예전: 반투명 흰 얇은 선)
            if (phStripLine == null) { phStripLine = Hostile.NewLine("PhStrip", new Color(0.09f, 0.08f, 0.13f, 0.8f), 0.3f, 18); }
            int n = phStrip.Count;
            phStripLine.enabled = n >= 2;
            if (n >= 2)
            {
                int cnt = 0;
                phStripLine.positionCount = n;
                for (int i = 0; i < n; i++) { if (phStrip[i] != null) { phStripLine.SetPosition(cnt++, phStrip[i].transform.position); } }
                phStripLine.positionCount = Mathf.Max(2, cnt);
                PixelMarks pm = PixelMarks.Ensure(ref phPx, this);
                Color hole = new Color(PhFlash.r, PhFlash.g, PhFlash.b, 0.85f);
                for (int i = 0; i + 1 < cnt; i++)
                {
                    Vector3 a = phStripLine.GetPosition(i), b = phStripLine.GetPosition(i + 1);
                    if ((a - b).sqrMagnitude <= 100f) pm.Perforate(a, b, hole);       // 피해가 나는 마디만 (10칸 넘게 떨어지면 베지 않음)
                }
            }
        }
        else if (phStripLine != null && phStripLine.enabled) phStripLine.enabled = false;
    }

    IEnumerator PhTripodShoots(Vector2 at, GameObject cam)
    {
        for (float t = 0f; t < 5f; t += 1.2f)
        {
            yield return new WaitForSeconds(1.2f);
            if (cam == null || player == null || player.IsDying) break;
            PhShoot(at, PhFrameSize(1), Damage * 0.5f * formPower, true, false, -1f);
        }
        if (cam != null) Destroy(cam);
    }

    // ================================================================= 매 프레임 (LateUpdate)
    void PhInit()
    {
        phInit = true;
        PhotographerAlbum.Reset();
        phReloadBase = 1.5f;
        player.reloadTime = phReloadBase;
        phPrevPos = transform.position;
    }

    void PhLate()
    {
        if (player == null) return;
        if (!phInit) PhInit();
        if (player.IsDying) { PhHideAim(); return; }
        bool paused = Time.timeScale == 0f;
        float dt = Time.deltaTime;
        if (!paused && dt > 0f)
        {
            Vector3 d = transform.position - phPrevPos;
            phMoveDir = Vector2.Lerp(phMoveDir, (Vector2)d / dt / Mathf.Max(0.1f, player.speed), 0.3f);
            phPrevPos = transform.position;
        }
        // 필름 보관함 (+1 · +2장) · 빠른 현상 (감기 20% · 35% 빠르게)
        int max = PhFilmMax;
        if (max != phLastMax) { if (max > phLastMax) player.NowBullet += max - phLastMax; phLastMax = max; }
        player.MaxBullet = max;
        float quick = Special != null && Special.Has(SpecialAbilities.KitQuickDev) ? (Special.IsEvolved(SpecialAbilities.KitQuickDev) ? 0.35f : 0.2f) : 0f;
        player.reloadTime = PhReloadBaseNow * (1f - quick);
        if (!paused)
        {
            PhPapersTick();
            PhCopiesTick();
            PhFormTick();
            PhPathTick();
        }
        PhDrawAim(paused);
        PhUltLate();
    }

    void PhHideAim()
    {
        if (phFocusRing != null) phFocusRing.enabled = false;
        if (phCountText != null) phCountText.gameObject.SetActive(false);
    }

    // 뷰파인더: 꺾쇠 네 모서리 + 가운데 초점 원 + 틀 안 수 「x7」 (6 이상이면 ★)
    void PhDrawAim(bool paused)
    {
        if (phPano || paused) { PhHideAim(); return; }
        Vector2 c = PhCursor();
        Vector2 size = PhFrameSize(Mathf.Max(1, player.multiShot));
        Color col = PhFlash; col.a = 0.7f;
        float hw = size.x * 0.5f, hh = size.y * 0.5f;
        // 2.0.0~ 도트 뷰파인더 (예전: 얇은 선 꺾쇠 넷 → 네모로만 보였음) — 판정 틀(size)과 같은 사각형에
        //   안쪽 가장자리 디더 그늘 · 삼분할 점선 · 외곽 있는 꺾쇠 넷 · 가장자리 가운데 눈금 · 가운데 초점 점
        PixelMarks pm = PixelMarks.Ensure(ref phPx, this);
        Rect frame = new Rect(c.x - hw, c.y - hh, size.x, size.y);
        pm.EdgeBands(frame, PixelMarkArt.Vignette(false), PixelMarkArt.Vignette(true), PixelMarkArt.VignetteThick, Color.white, true, true, -2);
        Color third = new Color(PhFlash.r, PhFlash.g, PhFlash.b, 0.3f);
        for (int k = -1; k <= 1; k += 2)
        {
            pm.Tiled(PixelMarkArt.Third(true), new Vector2(c.x + k * size.x / 6f, c.y), new Vector2(1f / HeroAimArt.PPU, size.y - 0.6f), third, false, false, -1);
            pm.Tiled(PixelMarkArt.Third(false), new Vector2(c.x, c.y + k * size.y / 6f), new Vector2(size.x - 0.6f, 1f / HeroAimArt.PPU), third, false, false, -1);
        }
        Color edge = new Color(PhFlash.r, PhFlash.g, PhFlash.b, 0.92f);
        pm.Corners(c, hw, hh, edge);
        pm.Put(PixelMarkArt.TickV, new Vector2(c.x, c.y + hh - 1f / HeroAimArt.PPU), edge);
        pm.Put(PixelMarkArt.TickV, new Vector2(c.x, c.y - hh + 1f / HeroAimArt.PPU), edge, false, true);
        pm.Put(PixelMarkArt.TickH, new Vector2(c.x - hw + 1f / HeroAimArt.PPU, c.y), edge);
        pm.Put(PixelMarkArt.TickH, new Vector2(c.x + hw - 1f / HeroAimArt.PPU, c.y), edge, true);
        pm.Put(PixelMarkArt.Focus, c, new Color(PhFlash.r, PhFlash.g, PhFlash.b, 0.75f));
        if (phFocusRing == null)
        {
            phFocusRing = Hostile.NewLine("PhFocus", col, 0.1f, 25);
            phFocusRing.loop = true;
            phFocusRing.positionCount = 20;
        }
        phFocusRing.enabled = true;
        float fr = PhFocusR;
        for (int k = 0; k < 20; k++)
        {
            float a = k * Mathf.PI * 2f / 20f;
            phFocusRing.SetPosition(k, new Vector3(c.x + Mathf.Cos(a) * fr, c.y + Mathf.Sin(a) * fr, 0f));
        }
        phFocusRing.startColor = phFocusRing.endColor = col;
        HeroAim.Mark(phFocusRing, HeroAimKind.Photographer, HeroAimFx.None);
        // 틀 안 수 (0.1초마다)
        if (Time.time >= phCountAt)
        {
            phCountAt = Time.time + 0.1f;
            int n = 0;
            using (var ovl = PhysHits.Box(c, size, 0f)) foreach (Collider2D cc in ovl)
                if (cc != null && cc.CompareTag("enermy")) { EnermyController e = EnermyController.FromCollider(cc); if (e != null && !e.IsDead && e.swarm == null) n++; }
            phCount = n;
        }
        if (phCountText == null)
        {
            GameObject go = new GameObject("PhCount", typeof(TMPro.TextMeshPro));
            phCountText = go.GetComponent<TMPro.TextMeshPro>();
            UIKit.EnsureStyle();
            if (UIKit.Font != null) phCountText.font = UIKit.Font;
            if (UIKit.FontMaterial != null) phCountText.fontSharedMaterial = UIKit.FontMaterial;
            phCountText.fontSize = 6f;
            phCountText.fontStyle = TMPro.FontStyles.Bold;
            phCountText.alignment = TMPro.TextAlignmentOptions.Right;
            phCountText.enableWordWrapping = false;
            phCountText.outlineWidth = 0.3f;
            phCountText.outlineColor = new Color32(0, 0, 0, 255);
            phCountText.rectTransform.sizeDelta = new Vector2(4f, 1.2f);
            phCountText.sortingLayerID = PlayerLook.TopLayer;
            phCountText.sortingOrder = 41;
        }
        phCountText.gameObject.SetActive(phCount > 0);
        if (phCount > 0)
        {
            bool star = phCount >= PhotographerTuning.Masterpiece;
            // 2.0.1~ 글이 바뀔 때만 새로 만듦 (매 프레임 문자열 할당하던 것)
            int dots = player.NowBullet <= 2 && player.NowBullet > 0 && Time.time % 0.6f < 0.3f ? player.NowBullet : 0;
            int key = phCount * 4 + dots;
            if (key != phCountKey) { phCountKey = key; phCountText.text = (star ? "★ " : "") + "×" + phCount + (dots > 0 ? "\n" + new string('●', dots) : ""); }
            phCountText.color = star ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 1f, 1f, 0.9f);
            phCountText.transform.position = new Vector3(c.x + hw - 2f, c.y + hh + 0.8f, 0f);
        }
    }

    void OnDestroyPhotographer()
    {
        EnemyHold.ReleaseAll(this);
        // 2.0.0~ 검토: 종이였던 적에 붙은 납작 사본(child)도 지움 — 안 지우면 그 판 내내 적이 두 겹으로 보였음
        for (int i = phPaperList.Count - 1; i >= 0; i--)
        {
            if (phPaperList[i].body != null) phPaperList[i].body.enabled = true;
            if (phPaperList[i].child != null) Destroy(phPaperList[i].child.gameObject);
        }
        phPaperList.Clear(); phPapers.Clear();
        foreach (var t in phTripods) if (t.tf != null) Destroy(t.tf.gameObject);      // 삼각대 카메라
        phTripods.Clear();
        foreach (SpriteRenderer sr in phChildPool) if (sr != null) Destroy(sr.gameObject);
        foreach (PhCopy c in phCopies) if (c.tf != null) Destroy(c.tf.gameObject);
        phCopies.Clear();
        if (phPx != null) Destroy(phPx.gameObject);
        if (phFocusRing != null) Destroy(phFocusRing.gameObject);
        if (phStripLine != null) Destroy(phStripLine.gameObject);
        if (phCountText != null) Destroy(phCountText.gameObject);
        PhUltCleanup();
    }
}
