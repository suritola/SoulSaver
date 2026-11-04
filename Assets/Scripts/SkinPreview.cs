using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 스킨 상점 미리보기 무대 (2.2.x~): 고른 스킨을 실제 게임 연출 코드(SkinFx · Fx · SkinAudio)로 돌려 작은 카메라 → RawImage 로 비춤
//   따로 흉내 낸 그림이 아니라 게임과 같은 함수를 부르므로, 앞으로 SkinFx 연출을 바꾸면 미리보기도 그대로 따라옴
//   무대: 영웅이 좌우로 걸으며(잔상) 허수아비를 공격 → 명중 → 세 번째마다 처치 · 가끔 피격 · 필살기 (판정 · 크기는 그대로)
//   보는 동안만 SkinFx 의 장착 스킨 · SharedFx(소리) · GlowSprite 를 빌려 쓰고, 닫으면 모두 되돌림
//   2026-11-04~ 스킨 시청각 개편: 걸음(발자국 · 걸음 소리 — SkinFx 가 움직임을 보고 냄) · 평타 몸짓 · 피격 · 레벨업 · 대기 · 승리 · 사망 ·
//   무기 장전 · 하이라이트 · 이펙트 코인 · 연쇄 · 큰 적 · 커서 클릭 · 조준점 순간을 차례로 (12초 한 바퀴)
public class SkinPreview : MonoBehaviour
{
    static readonly Vector3 Origin = new Vector3(5000f, 5000f, 0f);     // 메인 메뉴 장면 물건과 겹치지 않는 먼 자리
    const float HeroH = 2.2f, FoeH = 2.4f;

    SkinDef skin;
    SkinKind kind;
    int who;
    SpecialFeedback audioFx;

    Camera cam;
    RenderTexture rt;
    GameObject world;
    Transform hero, foe, shot;
    SpriteRenderer heroSr, foeSr, shotSr, cursorSr;
    Sprite[] heroFrames, foeFrames, cursorFrames;
    SkinFx fx;

    SkinDef savedChar, savedWeapon, savedEffect;
    SpecialFeedback savedShared;
    Sprite savedGlow;

    float t0, nextShot, shotStart, foeBackAt, hurtAt, ultAt, heroFlash, foeFlash;
    float cycleAt, pauseUntil, pauseX;
    int beat;
    int hits;
    bool shotLive, foeDead;
    Vector3 shotFrom;
    float heroScale, foeScale;

    public static SkinPreview Open(RectTransform win, SkinDef s, SkinKind kind, int who, SpecialFeedback audio, System.Action onClose,
                                   IList<(string key, string name)> chips = null, string replay = null)
    {
        // 창 전체를 덮는 막(뒤 카드 클릭 막음) + 무대
        RectTransform cover = UIKit.Rect("SkinPreviewCover", win, Vector2.zero, new Vector2(1640f, 960f));
        Image dim = cover.gameObject.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.015f, 0.04f, 0.94f);
        SkinPreview p = cover.gameObject.AddComponent<SkinPreview>();
        p.skin = s; p.kind = kind; p.who = who; p.audioFx = audio;

        string name = s != null ? Loc.T(s.name) : Loc.T("기본");
        TMP_Line(cover, name + "  ·  " + Loc.T("미리보기"), 40f, new Vector2(0f, 420f));
        p.rt = new RenderTexture(1280, 640, 16) { name = "SkinPreviewRT" };
        RawImage stage = UIKit.Rect("Stage", cover, new Vector2(0f, 30f), new Vector2(1280f, 640f)).gameObject.AddComponent<RawImage>();
        stage.texture = p.rt;
        stage.raycastTarget = false;
        // 2026-11-04~ 「바뀌는 칸」 칩: 누르면 그 순간을 무대에서 다시 (기획 7절 — 한 바퀴 12초를 기다리지 않게)
        bool hasChips = chips != null && chips.Count > 0;
        if (hasChips) p.ChipRow(cover, chips);
        TMP_Line(cover, Loc.T("skin.preview.note"), hasChips ? 18f : 22f, new Vector2(0f, hasChips ? -398f : -320f));
        UIKit.MakeButton(cover, "닫기", new Vector2(0f, hasChips ? -440f : -400f), new Vector2(220f, hasChips ? 48f : 60f), () => { onClose?.Invoke(); Destroy(cover.gameObject); }, 22f);
        p.Build();
        p.pendingReplay = replay;       // 무대 SkinFx 의 Start 가 돈 다음 프레임에 (Instance 가 잡힌 뒤)
        return p;
    }

    // 칩 버튼 줄 (무대 아래 · 넘치면 두 줄)
    void ChipRow(RectTransform cover, IList<(string key, string name)> chips)
    {
        const float RowW = 1280f, H = 36f, Gap = 8f;
        List<(string key, string label, float w)> items = new List<(string, string, float)>();
        foreach (var c in chips)
        {
            string label = Loc.T(c.name);
            items.Add((c.key, label, 28f + 15f * label.Length));
        }
        float x = 0f, y = -312f;
        List<(string key, string label, float w, float x, float y)> placed = new List<(string, string, float, float, float)>();
        int rowStart = 0;
        void Flush()
        {
            float total = 0f;
            for (int i = rowStart; i < placed.Count; i++) total += placed[i].w + Gap;
            float off = -(total - Gap) * 0.5f;
            for (int i = rowStart; i < placed.Count; i++) { var q = placed[i]; placed[i] = (q.key, q.label, q.w, off + q.x + q.w * 0.5f, q.y); }
            rowStart = placed.Count;
        }
        foreach (var it in items)
        {
            if (x + it.w > RowW && x > 0f) { Flush(); x = 0f; y -= H + 6f; }
            placed.Add((it.key, it.label, it.w, x, y));
            x += it.w + Gap;
        }
        Flush();
        foreach (var q in placed)
        {
            string key = q.key;
            UIKit.MakeButton(cover, q.label, new Vector2(q.x, q.y), new Vector2(q.w, H), () => Replay(key), 16f);
        }
    }

    // 칩 하나의 순간을 지금 다시 (게임과 같은 SkinFx 길) — 한 바퀴 순서는 잠깐 미룸
    public void Replay(string key)
    {
        if (world == null || fx == null) return;
        Vector3 foeAt = foe.position + new Vector3(0f, 0.3f, 0f);
        cycleAt = Time.time + 2.5f;
        switch (key)
        {
            case "step": case "start": case "trail":
                pauseUntil = 0f;
                fx.PreviewSlot("start", hero.position);
                break;
            case "obj":
                ShowProp();
                if (!shotLive && !foeDead) { nextShot = Time.time; Fire(); }
                break;
            case "acc": case "fire": case "whit": case "wtrail": case "hit": case "click":
                if (!shotLive && !foeDead) { nextShot = Time.time; Fire(); }
                break;
            case "kill": case "absorb": case "kill.basic":
                if (!shotLive && !foeDead) { hits = hits - hits % 3 + 2; nextShot = Time.time; Fire(); }
                break;
            case "hurt":
                hurtAt = Time.time;
                break;
            case "ultc": case "edge": case "ultaim":
                ultAt = Time.time;
                break;
            case "idle": case "widle": case "death": case "revive":
                pauseUntil = Time.time + 1.6f;
                pauseX = hero.position.x - Origin.x;
                if (!fx.PreviewSlot(key, hero.position) && key == "idle") fx.PreviewSlot("widle", hero.position);
                break;
            case "high": case "combo": case "big": case "coin": case "chest":
                fx.PreviewSlot(key, foeAt);
                break;
            case "hover":
                SkinAudio.Play(skin, "hover", 0.5f, 0f, SkinAudio.Prio.Cursor, audioFx);
                break;
            default:
                fx.PreviewSlot(key, hero.position);
                break;
        }
    }

    static void TMP_Line(Transform parent, string text, float size, Vector2 pos)
    {
        TMPro.TMP_Text t = UIKit.Text(parent, "", size, new Color(0.92f, 0.88f, 0.8f), pos, new Vector2(1400f, size + 16f));
        t.text = text;
    }

    void Build()
    {
        // ---- 빌려 쓰는 전역 (닫을 때 되돌림)
        savedChar = SkinFx.CharSkin; savedWeapon = SkinFx.WeaponSkin; savedEffect = SkinFx.EffectSkin;
        savedShared = SpecialAbilities.SharedFx;
        savedGlow = SpecialAbilities.GlowSprite;
        // 고른 칸은 고른 스킨, 나머지 칸은 그 영웅이 지금 낀 것 (같이 꼈을 때 모습)
        int heroId = SkinData.IsGlobal(kind) ? (int)CharacterData.Selected : who;
        SkinFx.CharSkin = kind == SkinKind.Character ? skin : SkinData.Equipped(SkinKind.Character, heroId);
        SkinFx.WeaponSkin = kind == SkinKind.Weapon ? skin : SkinData.Equipped(SkinKind.Weapon, heroId);
        SkinFx.EffectSkin = kind == SkinKind.Effect ? skin : SkinData.Equipped(SkinKind.Effect);
        SpecialAbilities.SharedFx = audioFx;
        SkinFx.RefreshLooks();
        SkinAudio.EnsureAll(kind == SkinKind.Cursor ? skin : null);      // 고른 카드 것만 소리를 만듦 (5-4절)
        // 메인 메뉴엔 게임 장면의 빛 그림이 없음 → 같은 크기(128px · 16 = 8칸)의 부드러운 점
        if (SpecialAbilities.GlowSprite == null) SpecialAbilities.GlowSprite = SkinArt.Glow;

        world = new GameObject("SkinPreviewWorld");
        world.transform.position = Origin;

        GameObject c = new GameObject("SkinPreviewCam");
        c.transform.SetParent(world.transform, false);
        c.transform.localPosition = new Vector3(0f, 0.6f, -10f);
        cam = c.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 3.6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.055f, 0.1f, 1f);
        cam.targetTexture = rt;
        cam.depth = -50f;

        // 바닥 띠 (무대 느낌만 · 판정 없음)
        SpriteRenderer floor = SpecialAbilities.MakeSprite("Floor", SpecialAbilities.GlowSprite, Origin + new Vector3(0f, -1.3f, 0f), 1f, new Color(0.35f, 0.3f, 0.45f, 0.35f), "Background", 0).GetComponent<SpriteRenderer>();
        floor.transform.SetParent(world.transform, true);
        floor.transform.localScale = new Vector3(2.2f, 0.25f, 1f);

        // ---- 영웅 (캐릭터 스킨 그림 · 달리기)
        heroFrames = SkinShopUI.RunFrames(heroId, SkinFx.CharSkin);
        hero = Body("Hero", heroFrames, HeroH, out heroSr, out heroScale);
        hero.position = Origin + new Vector3(-3.2f, 0f, 0f);
        // 실제 연출 코드: 잔상 · 오라 · 처치 · 필살기
        fx = hero.gameObject.AddComponent<SkinFx>();

        // ---- 허수아비 (사슬 망령)
        foeFrames = new Sprite[4];
        for (int i = 0; i < 4; i++) foeFrames[i] = Resources.Load<Sprite>("Abyss/chainwraith_" + i);
        foe = Body("Dummy", foeFrames, FoeH, out foeSr, out foeScale);
        foe.position = Origin + new Vector3(3.4f, 0.1f, 0f);
        foeSr.flipX = true;

        // ---- 투사체 (무기 그림이 돌며 날아감 · 무기 스킨 색 꼬리)
        shot = new GameObject("Shot").transform;
        shot.SetParent(world.transform, true);
        shotSr = shot.gameObject.AddComponent<SpriteRenderer>();
        shotSr.sortingLayerName = "Effect";
        shotSr.sortingOrder = 30;
        string held = CharacterData.Def((CharacterId)heroId).held ?? "pistol";
        shotSr.sprite = Resources.Load<Sprite>("Weapons/weapon_" + (SkinFx.WeaponSkin != null ? SkinFx.WeaponSkin.id : held));
        if (shotSr.sprite != null) shot.localScale = Vector3.one * (0.9f / Mathf.Max(0.01f, shotSr.sprite.bounds.size.y));
        shot.gameObject.SetActive(false);

        // ---- 커서 스킨: 허수아비 위 조준점
        if (kind == SkinKind.Cursor) BuildCursor();

        t0 = Time.time;
        nextShot = t0 + 0.6f;
        hurtAt = t0 + 3.1f;
        ultAt = t0 + 5.3f;
        cycleAt = t0 + 1.5f;
    }

    Transform Body(string name, Sprite[] frames, float h, out SpriteRenderer sr, out float scale)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(world.transform, true);
        sr = g.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Character";
        sr.sortingOrder = 5;
        sr.sprite = frames.Length > 0 ? frames[0] : null;
        scale = sr.sprite != null ? h / Mathf.Max(0.01f, sr.sprite.bounds.size.y) : 1f;
        g.transform.localScale = Vector3.one * scale;
        return g.transform;
    }

    void BuildCursor()
    {
        string id = skin != null ? skin.id : "cursor_default";
        List<Sprite> l = new List<Sprite>();
        Texture2D one = Resources.Load<Texture2D>("Cursors/" + id + "_aim");
        if (one != null) l.Add(ToSprite(one));
        else for (int i = 0; i < 4; i++) { Texture2D f = Resources.Load<Texture2D>("Cursors/" + id + "_aim_" + i); if (f != null) l.Add(ToSprite(f)); }
        cursorFrames = l.ToArray();
        if (cursorFrames.Length == 0) return;
        GameObject g = new GameObject("Cursor");
        g.transform.SetParent(world.transform, true);
        cursorSr = g.AddComponent<SpriteRenderer>();
        cursorSr.sortingLayerName = "Effect";
        cursorSr.sortingOrder = 60;
        cursorSr.sprite = cursorFrames[0];
        g.transform.localScale = Vector3.one * (cursorBase = 1.3f / Mathf.Max(0.01f, cursorSr.sprite.bounds.size.y));
    }
    float cursorBase = 1f, cursorPunch;

    // 한 바퀴의 다음 순간 (게임과 같은 SkinFx 칸 레시피 — 소리 포함)
    void Beat()
    {
        Vector3 foeAt = foe.position + new Vector3(0f, 0.3f, 0f);
        switch (beat++ % 6)
        {
            case 0:
                if (!fx.PreviewSlot("lvl", hero.position) && kind == SkinKind.Weapon) fx.PreviewSlot("reload", hero.position);
                fx.PreviewSlot("coin", foeAt + Vector3.left * 1.5f);
                cycleAt = Time.time + 2f;
                break;
            case 1:
                // 대기: 1.6초 멈춰 서서
                pauseUntil = Time.time + 1.6f;
                pauseX = hero.position.x - Origin.x;
                if (!fx.PreviewSlot("idle", hero.position)) fx.PreviewSlot("widle", hero.position);
                cycleAt = Time.time + 2.2f;
                break;
            case 2:
                fx.PreviewSlot("high", foeAt);
                fx.PreviewSlot("combo", foeAt);
                if (kind == SkinKind.Cursor) CursorSkin.MenuClick();
                cycleAt = Time.time + 2f;
                break;
            case 3:
                fx.PreviewSlot("big", foeAt);
                fx.PreviewSlot("start", hero.position);
                ShowProp();
                cycleAt = Time.time + 2f;
                break;
            case 4:
                fx.PreviewSlot("win", hero.position);
                fx.PreviewSlot("evo", hero.position);
                cycleAt = Time.time + 2.4f;
                break;
            default:
                pauseUntil = Time.time + 1.4f;
                pauseX = hero.position.x - Origin.x;
                if (!fx.PreviewSlot("death", hero.position)) fx.PreviewSlot("revive", hero.position);
                cycleAt = Time.time + 2.2f;
                break;
        }
    }

    // 스킨 물체 그림 (더미 · 묘비 · F12 키캡 — 게임과 같은 고르는 길)을 허수아비 옆에 잠깐 세움
    void ShowProp()
    {
        Sprite sp = PropSprite();
        if (sp == null) return;
        Vector3 at = foe.position + new Vector3(-1.7f, -0.1f, 0f);
        FadeSprite.Spawn("SkinPreviewProp", sp, at, 1.7f / Mathf.Max(0.01f, sp.bounds.size.y), Color.white, "Character", 4, 1.8f);
    }

    Sprite PropSprite()
    {
        int heroId = SkinData.IsGlobal(kind) ? (int)CharacterData.Selected : who;
        string held = CharacterData.Def((CharacterId)heroId).held ?? "pistol";
        string path = SkinFx.WeaponSkin != null ? (held == "dummy" ? "Weapons/weapon_" + SkinFx.WeaponSkin.id : null)
                    : SkinFx.CharSkin != null ? "Weapons/weapon_" + held + "__" + SkinFx.CharSkin.id : null;
        if (path != null)
        {
            Sprite best = null;
            foreach (Sprite s in Resources.LoadAll<Sprite>(path)) if (best == null || string.CompareOrdinal(s.name, best.name) < 0) best = s;
            if (best != null) return best;
        }
        string g = SkinFx.ObjectSheet("prop_grave");
        if (g == "prop_grave") return null;
        Sprite[] f = Fx.Frames(g);
        return f.Length > 0 ? f[0] : null;
    }

    static Sprite ToSprite(Texture2D t) => Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), Vector2.one * 0.5f, 16f);

    string pendingReplay;

    void Update()
    {
        if (world == null) return;
        if (pendingReplay != null && Time.time > t0) { string k = pendingReplay; pendingReplay = null; Replay(k); }
        float t = Time.time - t0;

        // 영웅: 왼쪽에서 좌우로 걸음 (잔상은 SkinFx 가 움직임을 보고 냄) · 쏠 땐 오른쪽을 봄
        float x = -3.2f + Mathf.Sin(t * 1.3f) * 1.1f;
        float vx = Mathf.Cos(t * 1.3f);
        bool paused = Time.time < pauseUntil;       // 대기 동작을 보이려고 잠깐 멈춤
        if (paused) x = pauseX;
        hero.position = Origin + new Vector3(x, paused ? 0f : Mathf.Abs(Mathf.Sin(t * 9f)) * 0.06f, 0f);
        heroSr.flipX = vx < -0.35f && !shotLive && !paused;
        if (heroFrames.Length > 0) heroSr.sprite = heroFrames[paused ? 0 : (int)(t * 8f) % heroFrames.Length];
        if (Time.time >= cycleAt) Beat();
        heroSr.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), Mathf.Clamp01(heroFlash -= Time.deltaTime * 4f));

        // 허수아비
        if (foeFrames.Length > 0 && foeFrames[0] != null) foeSr.sprite = foeFrames[(int)(t * 6f) % foeFrames.Length];
        foeSr.color = Color.Lerp(Color.white, new Color(1f, 0.5f, 0.5f), Mathf.Clamp01(foeFlash -= Time.deltaTime * 6f));
        if (foeDead && Time.time >= foeBackAt) { foeDead = false; foe.gameObject.SetActive(true); }
        if (!foeDead) foe.localScale = Vector3.one * foeScale * (1f + 0.04f * Mathf.Sin(t * 3f));

        // 공격 → 명중 (0.5초마다) · 세 번째 명중마다 처치
        if (!shotLive && !foeDead && Time.time >= nextShot && !paused) Fire();
        if (shotLive) FlyShot();

        // 피격 (4초마다) · 필살기 (7초마다)
        if (Time.time >= hurtAt)
        {
            hurtAt = Time.time + 4f;
            heroFlash = 1f;
            SkinAudio.PlayHurt();       // 기본(스킨 없음)은 무음 — 게임의 기본 피격음은 장면에 붙은 클립이라 메뉴에 없음
            fx.PreviewSlot("hurt", hero.position);
        }
        if (Time.time >= ultAt)
        {
            ultAt = Time.time + 7f;
            fx.OnUlt();
        }

        if (cursorSr != null)
        {
            cursorSr.transform.position = foe.position + new Vector3(Mathf.Sin(t * 2.1f) * 0.25f, 0.2f + Mathf.Cos(t * 1.7f) * 0.2f, 0f);
            cursorSr.sprite = cursorFrames[(int)(t * 8f) % cursorFrames.Length];
            // 전투 평타 반응: 조준점이 「딱」 오므라졌다 돌아옴 (게임은 커서 그림 3프레임)
            cursorPunch = Mathf.Max(0f, cursorPunch - Time.deltaTime * 10f);
            cursorSr.transform.localScale = Vector3.one * cursorBase * (1f - 0.2f * cursorPunch);
        }
    }

    void Fire()
    {
        shotLive = true;
        shotStart = Time.time;
        shotFrom = hero.position + new Vector3(0.6f, 0.3f, 0f);
        shot.gameObject.SetActive(shotSr.sprite != null);
        heroSr.flipX = false;
        SkinFx.OnAttack(shotFrom, Vector2.right);       // 평타 몸짓 · 무기 발사 그림 (게임과 같은 길)
        SkinFx.OnObject("throw", shotFrom, Vector2.right);      // 키트 물체 순간 (씨름꾼 메치기)
        // 공격 소리 = 상점 예전 미리듣기와 같은 규칙 (무기 스킨 음높이 · 덧소리)
        SkinDef w = SkinFx.WeaponSkin;
        int heroId = SkinData.IsGlobal(kind) ? (int)CharacterData.Selected : who;
        audioFx?.PlayRaw(w != null && w.attackSound != null ? w.attackSound : SkinShopUI.BaseAttack(heroId), 0.55f, w != null ? w.pitch : 1f);
        if (w != null && w.layer != null) audioFx?.PlayRaw(w.layer, 0.25f);
    }

    void FlyShot()
    {
        const float Dur = 0.28f;
        float k = Mathf.Clamp01((Time.time - shotStart) / Dur);
        Vector3 to = foe.position + new Vector3(0f, 0.3f, 0f);
        Vector3 p = Vector3.Lerp(shotFrom, to, k) + new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 0.4f, 0f);
        shot.position = p;
        shot.Rotate(0f, 0f, -900f * Time.deltaTime);
        // 꼬리: 무기 스킨 색 (없으면 흰빛) — 게임의 SkinTint 와 같은 색
        if (SpecialAbilities.GlowSprite != null && Time.frameCount % 2 == 0)
        {
            Color tc = SkinFx.WeaponSkin != null ? SkinFx.WeaponSkin.color : new Color(1f, 0.95f, 0.8f);
            FadeSprite.Spawn("SkinPreviewTrail", SpecialAbilities.GlowSprite, p, 0.07f, new Color(tc.r, tc.g, tc.b, 0.8f), "Effect", 20, 0.25f);
        }
        if (k < 1f) return;

        shotLive = false;
        shot.gameObject.SetActive(false);
        nextShot = Time.time + 0.5f;
        foeFlash = 1f;
        if (cursorSr != null && !SkinAudio.Play(skin, "click", 0.5f, 4f, SkinAudio.Prio.Cursor)) audioFx?.PlayRaw("clank", 0.45f, 1.4f + 0.1f * (skin != null ? skin.tier : 0));
        if (cursorSr != null) cursorPunch = 1f;
        // 명중: 이펙트 스킨이 있으면 게임 그대로(SkinFx.OnEnemyHit), 없으면 기본 흰 불꽃
        if (SkinFx.EffectSkin != null) SkinFx.OnEnemyHit(to);
        else Fx.Spawn("fx_spark", to, 2.4f, Color.white, 26f);
        SkinFx.OnObject("dig", to);         // 키트 물체 순간 (장의사 파기)
        SkinFx.OnSnap(foe);                 // 사진사 찍힘 (영혼 · 픽셀 테)
        if (++hits % 3 != 0) return;

        // 처치: 기본 영혼 폭발 색(KillColor) + 게임과 같은 스킨 처치 연출 · 소리
        Fx.Spawn("fx_sparkle", to, 2.8f, SkinFx.KillColor(new Color(0.6f, 0.85f, 1f)), 16f);
        SkinFx.OnObject("bury", to);        // 장의사 매장 (미완성 묘비 「저장 중」)
        fx.OnKill(to);
        foeDead = true;
        foe.gameObject.SetActive(false);
        foeBackAt = Time.time + 0.9f;
        nextShot = foeBackAt + 0.3f;
    }

    void OnDestroy()
    {
        if (world != null) Destroy(world);      // 영웅이 지워지면 SkinFx.OnDestroy 가 오라 구슬 · 발밑 빛도 지움
        if (cam != null) cam.targetTexture = null;
        if (rt != null) { rt.Release(); Destroy(rt); }
        SkinFx.CharSkin = savedChar; SkinFx.WeaponSkin = savedWeapon; SkinFx.EffectSkin = savedEffect;
        SkinFx.RefreshLooks();
        SpecialAbilities.SharedFx = savedShared;
        SpecialAbilities.GlowSprite = savedGlow;
    }
}
