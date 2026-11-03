using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 인형사 필살기 「커튼콜」 (2.0.0) — 우클릭 · 필살기 게이지 가득 + 0.25초 넘게 누르고 있음 (짧게 누르면 자리 바꾸기 — CharacterKit.Puppeteer)
//   조준: 더미 둘레 반지름 3.5칸 고리 + 고리 위 매달린 작은 더미 넷 (HeroAim) → 떼면 발동
//   0.2초: 줄 넷이 내려오며 더미 넷이 둘레 네 방향에 착지 (착지 둘레 1.7칸 공격력 40%)
//   2.5초: 다섯 더미(본 더미 + 넷)가 커서를 따라 대형을 유지 — 좌클릭 휘두름이 모두에서 동시에 (각 40%) · 안 누르면 0.6초마다 저절로 (각 30%) · 세로 줄도 칼날 (0.45초에 한 번 15%)
//   끝: 박수와 함께 더미 넷이 위로 끌려 올라가며 반지름 4.5칸 공격력 150% (손맛 서명) · 모든 피해 × 필살기 배율 · 가장자리 붉은 벨벳 막 5% 가 2.5초 동안 (조작 가리지 않음)
//   진화 무기 궁극기(싹둑 · 실타래 · 행진)는 KitUltAdd 로 덧붙음 · 극단 진화는 더미 여섯 · 앙코르(고유 스킬)는 끝난 뒤 더미가 남음
public partial class CharacterKit
{
    const float PupAimR = 3.5f, PupEndR = 4.5f, PupCurtainSec = 2.5f;

    bool pupAiming, pupPressed;
    float pupPressAt, pupCurtainAutoAt, pupVertAt, pupCurtainBegin = -9f;
    float pupCurtainUntil, pupEncoreUntil;
    bool pupCurtainOn;
    Vector2 pupAimAt;
    LineRenderer pupAimRing;

    sealed class PupExtra
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Transform bodyTf;
        public LineRenderer vline;
        public float ang;           // 대형 안 각도 (도)
        public Vector2 pos;
        public bool landed, encore;
        public float swingAt = -9f;
    }
    readonly List<PupExtra> pupExtras = new List<PupExtra>(8);
    readonly Dictionary<int, float> pupVertCutAt = new Dictionary<int, float>();

    // 커튼콜 · 앙코르 더미 위치 (진화 무기 궁극기 실타래 · 행진이 읽음)
    public int PupExtraCount => pupExtras.Count;
    public Vector2 PupExtraPos(int i) => i >= 0 && i < pupExtras.Count ? pupExtras[i].pos : pupPos;
    public bool PupCurtainActive => Time.time < pupCurtainUntil;
    public bool PupUltEngaged => pupAiming;

    // ---- 캔버스 (가장자리 벨벳 막)
    Canvas pupEdgeCanvas;
    Image pupEdgeL, pupEdgeR;
    float pupEdgeK, pupEdgeLift = -1f;

    // ================================================================= 입력 (UpdateUlt 의 인형사 갈래)
    void UpdatePuppetUlt(SkillGauge gauge, bool full)
    {
        if (Time.timeScale == 0f || player == null || player.IsDying) { PupHideAim(); pupPressed = false; return; }
        bool toggle = GameSettings.UltToggle && !GameInput.Auto;
        if (pupAiming)
        {
            if (!full) { PupHideAim(); GameInput.ResetUltLatch(); return; }
            PupAimUpdate();
            PupDrawAim();
            if (AddedUltShown) DrawUltAim(false, true);
            if (!GameInput.UltUp && GameInput.UltHeld) return;
            PupHideAim();
            CancelUltAim();
            PupStartCurtain(gauge);
            return;
        }
        if (GameInput.UltDown)
        {
            if (full)
            {
                // 게이지 가득: 0.25초 안에 떼면 자리 바꾸기, 넘기면 조준
                // 2.0.1~ 토글 설정도 같게 (예전엔 첫 누름 = 조준이라 게이지가 가득이면 자리를 못 바꿨음) — 떼는 순간은 실제 키로 잼
                pupPressed = true; pupPressAt = Time.time;
            }
            else PupTrySwap(gauge);
        }
        if (pupPressed)
        {
            bool held = toggle ? KeyBindings.Held(GameAction.Ultimate) : GameInput.UltHeld && !GameInput.UltUp;
            if (!held)
            {
                pupPressed = false;
                if (Time.time - pupPressAt <= 0.25f)
                {
                    PupTrySwap(gauge);
                    if (toggle) GameInput.ResetUltLatch();      // 토글 걸쇠가 「누른 채」로 남지 않게
                }
            }
            else if (Time.time - pupPressAt > 0.25f) { pupPressed = false; pupAiming = true; Play("hum", 0.3f, 1.2f); }
        }
    }

    void PupAimUpdate() => pupAimAt = pupPos;

    void PupHideAim()
    {
        pupAiming = false;
        if (pupAimRing != null) pupAimRing.enabled = false;
    }

    void PupDrawAim()
    {
        if (pupAimRing == null) { pupAimRing = Hostile.NewLine("PupAim", PupSky, 0.12f, 25); pupAimRing.loop = true; }
        pupAimRing.enabled = true;
        Color c = HeroAim.ColorOf(HeroAimKind.Puppeteer, 0.5f + 0.3f * Mathf.Sin(Time.time * 14f));
        pupAimRing.startColor = pupAimRing.endColor = c;
        pupAimRing.startWidth = pupAimRing.endWidth = 0.14f;
        Hostile.SetArc(pupAimRing, new Vector3(pupAimAt.x, pupAimAt.y, 0f), PupAimR, 0f, 360f);
        HeroAim.Mark(pupAimRing, HeroAimKind.Puppeteer);
    }

    // ================================================================= 발동
    void PupStartCurtain(SkillGauge gauge)
    {
        if (gauge == null || !gauge.IsFull() || player == null) return;
        PupEnsure();
        PupCurtainCleanup();
        int n = PupEvo("me.troupe") ? 6 : 4;
        for (int i = 0; i < n; i++) pupExtras.Add(PupMakeExtra(45f + 360f / n * i, false));
        pupCurtainBegin = Time.time;
        pupCurtainUntil = Time.time + PupCurtainSec;
        pupCurtainOn = true;
        pupEncoreUntil = 0f;
        pupCurtainAutoAt = Time.time + 0.6f;
        // 진화 무기 궁극기: 싹둑은 끝 순간에 · 실타래 · 행진은 커튼콜이 도는 동안
        KitUltAdd(form == SpecialAbilities.KitScissorDummy ? PupCurtainSec : 0.25f, new Vector3(pupPos.x, pupPos.y, 0f));
        Spend(gauge);
        PlayerMotion.Of(this).Cast(0.6f);
        Hostile.Play("chime", 0.5f, 0.8f);
        Hostile.Play("whoosh", 0.5f, 0.6f);
        PupEdgeShow();
        RunCounters.Both("pup.curtain");
    }

    PupExtra PupMakeExtra(float ang, bool encore)
    {
        PupExtra ex = new PupExtra { ang = ang, encore = encore };
        ex.go = new GameObject("PuppetExtra");
        GameObject b = new GameObject("Body");
        b.transform.SetParent(ex.go.transform, false);
        ex.bodyTf = b.transform;
        ex.sr = b.AddComponent<SpriteRenderer>();
        ex.sr.sortingLayerID = PlayerLook.TopLayer;
        ex.sr.sortingOrder = 29;
        ex.sr.color = new Color(1f, 1f, 1f, 0.95f);
        if (pupFrames != null && pupFrames.Length > 0)
        {
            ex.sr.sprite = pupFrames[0];
            float h = pupFrames[0].bounds.size.y;
            b.transform.localScale = Vector3.one * (h > 0.01f ? 2.1f / h : 1f);
        }
        ex.pos = PupFormation(ang, encore);
        ex.go.transform.position = new Vector3(ex.pos.x, ex.pos.y, 0f);
        ex.vline = Hostile.NewLine("PupVString", PupSky, 0.07f, 23);
        ex.vline.loop = false;
        ex.vline.positionCount = 2;
        return ex;
    }

    Vector2 PupFormation(float angDeg, bool encore)
    {
        float r = encore ? 2.2f : PupAimR;
        float a = (angDeg + (encore ? Time.time * 40f : 0f)) * Mathf.Deg2Rad;
        Vector2 p = pupPos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        Vector3 c = Hostile.ClampArena(p);
        return c;
    }

    // ================================================================= 매 프레임 (PupLate 가 부름)
    void PupCurtainTick()
    {
        bool curtain = Time.time < pupCurtainUntil;
        if (pupCurtainOn && !curtain) PupEndCurtain();
        if (pupEncoreUntil > 0f && Time.time >= pupEncoreUntil) { pupEncoreUntil = 0f; PupCurtainCleanup(); }
        PupEdgeTick();
        if (pupExtras.Count == 0) return;
        float since = Time.time - pupCurtainBegin;
        for (int i = pupExtras.Count - 1; i >= 0; i--)
        {
            PupExtra ex = pupExtras[i];
            if (ex.go == null) { pupExtras.RemoveAt(i); continue; }
            ex.pos = PupFormation(ex.ang, ex.encore);
            ex.go.transform.position = new Vector3(ex.pos.x, ex.pos.y, 0f);
            float bob = 0.55f + 0.06f * Mathf.Sin(Time.time * 4.5f + i);
            float drop = ex.encore ? 0f : 8f * (1f - Mathf.Clamp01(since / 0.2f));
            ex.bodyTf.localPosition = new Vector3(0f, bob + 0.5f + drop, 0f);
            // 착지: 둘레 1.7칸 공격력 40%
            if (!ex.landed && !ex.encore && since >= 0.2f)
            {
                ex.landed = true;
                using (DamageSource.As(DamageSource.Ult)) DamageCircle(new Vector3(ex.pos.x, ex.pos.y, 0f), PupSwingR0, Damage * 0.4f * UltMul * formPower, 1.2f);
                Fx.Spawn(FFxPup("fx_puppet_swing", "fx_shock"), ex.pos, PupSwingR0 * 2f, PupSky, 20f);
                Play("thump", 0.25f, 1.1f + 0.05f * i);
            }
            else if (ex.encore) ex.landed = true;
            if (pupFrames != null && pupFrames.Length > 1)
            {
                float sw = Time.time - ex.swingAt;
                Sprite s = pupFrames[sw < 0.25f ? Mathf.Min(pupFrames.Length - 1, 1 + Mathf.Min(2, (int)(sw / 0.25f * 3f))) : 0];      // 2.0.0~ 검토: 프레임이 2~3장인 스킨에서도 범위를 넘지 않게
                if (ex.sr.sprite != s) ex.sr.sprite = s;
            }
            // 세로 줄: 더미에서 화면 위로
            if (ex.vline != null)
            {
                ex.vline.enabled = true;
                Color c = HeroAim.ColorOf(HeroAimKind.Puppeteer, ex.encore ? 0.35f : 0.7f);
                ex.vline.startColor = ex.vline.endColor = c;
                float top = ex.pos.y + bob + 0.5f + drop + 2.4f;
                ex.vline.SetPosition(0, new Vector3(ex.pos.x, top - 1.2f, 0f));
                ex.vline.SetPosition(1, new Vector3(ex.pos.x, ex.pos.y + 11f, 0f));
                HeroAim.Mark(ex.vline, HeroAimKind.Puppeteer, HeroAimFx.None);
            }
        }
        if (Time.timeScale == 0f) return;
        // 커튼콜 중: 좌클릭을 안 누르고 있으면 0.6초마다 저절로 (각 30%)
        if (curtain && !GameInput.FireHeld && Time.time >= pupCurtainAutoAt)
        {
            pupCurtainAutoAt = Time.time + 0.6f;
            PupSwingExtras(Damage * 0.75f * formPower);
        }
        // 세로 줄 베기: 0.1초마다 · 같은 적은 0.45초에 한 번 · 공격력 15%
        if (Time.time >= pupVertAt)
        {
            pupVertAt = Time.time + 0.1f;
            PupVerticalCut();
        }
    }

    // 좌클릭 휘두름이 다섯 더미 모두에서 (PupSwing 이 부름 — 반환: 맞힌 수)
    int PupExtrasSwing(float baseDmg)
    {
        if (pupExtras.Count == 0) return 0;
        return PupSwingExtras(baseDmg);
    }

    int PupSwingExtras(float baseDmg)
    {
        float rate = Time.time < pupCurtainUntil ? 0.4f * UltMul : 0.5f;       // 앙코르는 50%
        int hits = 0;
        float r = PupSwingR;
        foreach (PupExtra ex in pupExtras)
        {
            if (ex.go == null || (!ex.landed && !ex.encore)) continue;
            ex.swingAt = Time.time;
            Vector3 at = new Vector3(ex.pos.x, ex.pos.y, 0f);
            using (DamageSource.As(ex.encore ? DamageSource.Signature : DamageSource.Ult))
                hits += DamageCircle(at, r, baseDmg * rate, 1.2f);
            Fx.Spawn(FFxPup("fx_puppet_swing", "fx_shock"), at, r * 2f, PupSky, 20f);
        }
        return hits;
    }

    void PupVerticalCut()
    {
        if (Time.time >= pupCurtainUntil) return;      // 앙코르 더미의 세로 줄은 베지 않음
        float dmg = Damage * 0.15f * UltMul * formPower;
        foreach (PupExtra ex in pupExtras)
        {
            if (ex.go == null || !ex.landed || ex.encore) continue;
            Vector2 mid = ex.pos + Vector2.up * 4.5f;
            using var list = PhysHits.Circle(mid, 5.8f);
            foreach (Collider2D c in list)
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                Vector2 p = c.transform.position;
                if (p.y < ex.pos.y - 0.5f || p.y > ex.pos.y + 9f) continue;
                if (Mathf.Abs(p.x - ex.pos.x) > 0.35f + PupBodyR(c)) continue;
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && (e.IsDead || e.swarm != null)) continue;
                int id = c.gameObject.GetInstanceID();
                if (pupVertCutAt.TryGetValue(id, out float next) && Time.time < next) continue;
                pupVertCutAt[id] = Time.time + PupCutGap;
                using (DamageSource.As(DamageSource.Ult)) Specials.Damage(c.gameObject, dmg, Vector3.zero, 0f);
                Fx.Spawn(FFxPup("fx_string_cut", "fx_spark"), p, 1.2f, PupSky, 24f);
            }
        }
        if (pupVertCutAt.Count > 160) pupVertCutAt.Clear();
    }

    // ================================================================= 끝: 박수 · 위로 끌려 올라감 · 크게 벰
    void PupEndCurtain()
    {
        pupCurtainOn = false;
        Vector3 c = new Vector3(pupPos.x, pupPos.y, 0f);
        using (DamageSource.As(DamageSource.Ult))
            DamageCircle(c, PupEndR, Damage * 1.5f * UltMul * formPower, 3f);
        Fx.Spawn("fx_shock", c, PupEndR * 2f, PupSky, 20f);
        Fx.Spawn(FFxPup("fx_curtain_drop", "fx_explosion"), c, PupEndR * 1.4f, Color.white, 16f);
        Hostile.Shake(0.3f);
        Impact.Hit(ImpactTier.Ult, c, Vector2.up);          // 손맛 서명: 빨려 듦 「씽」 → 박수
        PupEdgeHide();
        // 앙코르(고유 스킬): 더미 하나(극단은 둘)가 남아 함께 휘두름
        int keep = SignatureSkills.PupEncore(out float sec);
        for (int i = pupExtras.Count - 1; i >= 0; i--)
        {
            if (i < keep) { pupExtras[i].encore = true; pupExtras[i].ang = 360f / Mathf.Max(1, keep) * i; continue; }
            if (pupExtras[i].go != null) { Fx.Spawn(FFxPup("fx_curtain_string", "fx_spark"), pupExtras[i].pos, 3f, Color.white, 20f); Destroy(pupExtras[i].go); }
            if (pupExtras[i].vline != null) Destroy(pupExtras[i].vline.gameObject);
            pupExtras.RemoveAt(i);
        }
        if (keep > 0 && sec > 0f) pupEncoreUntil = Time.time + sec;
        else PupCurtainCleanup();
    }

    void PupCurtainCleanup()
    {
        foreach (PupExtra ex in pupExtras)
        {
            if (ex.go != null) Destroy(ex.go);
            if (ex.vline != null) Destroy(ex.vline.gameObject);
        }
        pupExtras.Clear();
        pupCurtainOn = false;
        pupCurtainUntil = 0f;
        pupEncoreUntil = 0f;
        PupHideAim();
        PupEdgeHide();
    }

    // ================================================================= 가장자리 벨벳 막 (화면 너비 5% · 입력 가로막지 않음)
    void PupEdgeShow()
    {
        if (GameInput.Auto || Application.isBatchMode) return;
        if (pupEdgeCanvas == null)
        {
            GameObject cg = new GameObject("PupCurtainEdge", typeof(Canvas));
            cg.transform.SetParent(transform, false);
            pupEdgeCanvas = cg.GetComponent<Canvas>();
            pupEdgeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pupEdgeCanvas.sortingOrder = 880;            // HUD 위 · 번쩍임(900) 아래
            Sprite sp = Resources.Load<Sprite>("UI/ui_curtain_edge");
            pupEdgeL = PupEdgeImage(cg.transform, sp, false);
            pupEdgeR = PupEdgeImage(cg.transform, sp, true);
        }
        pupEdgeCanvas.enabled = true;
        pupEdgeK = 0f;
        pupEdgeLift = -1f;
    }

    static Image PupEdgeImage(Transform parent, Sprite sp, bool right)
    {
        GameObject go = new GameObject(right ? "Right" : "Left", typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image im = go.GetComponent<Image>();
        im.raycastTarget = false;
        if (sp != null) { im.sprite = sp; im.type = Image.Type.Sliced; im.color = new Color(1f, 1f, 1f, 0.85f); }
        else im.color = new Color(0.55f, 0.06f, 0.12f, 0.85f);
        if (right) rt.localScale = new Vector3(-1f, 1f, 1f);
        return im;
    }

    void PupEdgeHide()
    {
        if (pupEdgeCanvas != null && pupEdgeCanvas.enabled && pupEdgeLift < 0f) pupEdgeLift = 0f;       // 위로 걷힘 시작
    }

    // 2.0.1~ 바로 끔 (사망 · 정리 경로) — 걷힘 애니메이션을 기다리지 않음
    void PupEdgeOff()
    {
        if (pupEdgeCanvas != null) pupEdgeCanvas.enabled = false;
        pupEdgeLift = -1f;
    }

    // 2.0.1~ 일시정지 동안 막 그림만 숨김 (상태는 그대로 — 풀리면 다시 보임)
    void PupEdgePause(bool paused)
    {
        if (pupEdgeL != null && pupEdgeL.enabled == paused) pupEdgeL.enabled = !paused;
        if (pupEdgeR != null && pupEdgeR.enabled == paused) pupEdgeR.enabled = !paused;
    }

    void PupEdgeTick()
    {
        if (pupEdgeCanvas == null || !pupEdgeCanvas.enabled) return;
        bool reduce = GameSettings.ReduceMotion;
        const float W = 0.05f;
        float dt = Time.unscaledDeltaTime;
        float lift = 0f, alpha = 0.85f;
        if (pupEdgeLift >= 0f)
        {
            pupEdgeLift += dt / 0.25f;
            if (pupEdgeLift >= 1f) { pupEdgeCanvas.enabled = false; pupEdgeLift = -1f; return; }
            lift = reduce ? 0f : pupEdgeLift;
            alpha = 0.85f * (reduce ? 1f - pupEdgeLift : 1f);
        }
        else pupEdgeK = Mathf.Min(1f, pupEdgeK + dt / 0.2f);
        float slide = reduce ? 0f : W * (1f - pupEdgeK);          // 「움직임 줄이기」면 미끄러짐 대신 알파만
        if (reduce && pupEdgeLift < 0f) alpha = 0.85f * pupEdgeK;
        PupEdgeRect(pupEdgeL, -slide, W - slide, lift, alpha);
        PupEdgeRect(pupEdgeR, 1f - W + slide, 1f + slide, lift, alpha);
    }

    static void PupEdgeRect(Image im, float x0, float x1, float lift, float alpha)
    {
        if (im == null) return;
        RectTransform rt = im.rectTransform;
        rt.anchorMin = new Vector2(x0, lift);
        rt.anchorMax = new Vector2(x1, 1f + lift);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Color c = im.color;
        c.a = alpha;
        im.color = c;
    }
}
