using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 스킨 상점 미리보기 무대 (2.2.x~): 고른 스킨을 실제 게임 연출 코드(SkinFx · Fx · SkinAudio)로 돌려 작은 카메라 → RawImage 로 비춤
//   따로 흉내 낸 그림이 아니라 게임과 같은 함수를 부르므로, 앞으로 SkinFx 연출을 바꾸면 미리보기도 그대로 따라옴
//   무대: 영웅이 좌우로 걸으며(잔상) 허수아비를 공격 → 명중 → 세 번째마다 처치 · 가끔 피격 · 필살기 (판정 · 크기는 그대로)
//   보는 동안만 SkinFx 의 장착 스킨 · SharedFx(소리) · GlowSprite 를 빌려 쓰고, 닫으면 모두 되돌림
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
    int hits;
    bool shotLive, foeDead;
    Vector3 shotFrom;
    float heroScale, foeScale;

    public static SkinPreview Open(RectTransform win, SkinDef s, SkinKind kind, int who, SpecialFeedback audio, System.Action onClose)
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
        TMP_Line(cover, Loc.T("skin.preview.note"), 22f, new Vector2(0f, -320f));
        UIKit.MakeButton(cover, "닫기", new Vector2(0f, -400f), new Vector2(220f, 60f), () => { onClose?.Invoke(); Destroy(cover.gameObject); }, 22f);
        p.Build();
        return p;
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
        // 메인 메뉴엔 게임 장면의 빛 그림이 없음 → 같은 크기(128px · 16 = 8칸)의 부드러운 점
        if (SpecialAbilities.GlowSprite == null) SpecialAbilities.GlowSprite = SoftGlow();

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
        g.transform.localScale = Vector3.one * (1.3f / Mathf.Max(0.01f, cursorSr.sprite.bounds.size.y));
    }

    static Sprite ToSprite(Texture2D t) => Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), Vector2.one * 0.5f, 16f);

    static Sprite softGlow;
    static Sprite SoftGlow()
    {
        if (softGlow != null) return softGlow;
        const int N = 64;
        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        Color32[] px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return softGlow = Sprite.Create(tex, new Rect(0f, 0f, N, N), Vector2.one * 0.5f, 8f);     // 64px · 8 = 8칸 (게임 빛 그림과 같은 크기)
    }

    void Update()
    {
        if (world == null) return;
        float t = Time.time - t0;

        // 영웅: 왼쪽에서 좌우로 걸음 (잔상은 SkinFx 가 움직임을 보고 냄) · 쏠 땐 오른쪽을 봄
        float x = -3.2f + Mathf.Sin(t * 1.3f) * 1.1f;
        float vx = Mathf.Cos(t * 1.3f);
        hero.position = Origin + new Vector3(x, Mathf.Abs(Mathf.Sin(t * 9f)) * 0.06f, 0f);
        heroSr.flipX = vx < -0.35f && !shotLive;
        if (heroFrames.Length > 0) heroSr.sprite = heroFrames[(int)(t * 8f) % heroFrames.Length];
        heroSr.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), Mathf.Clamp01(heroFlash -= Time.deltaTime * 4f));

        // 허수아비
        if (foeFrames.Length > 0 && foeFrames[0] != null) foeSr.sprite = foeFrames[(int)(t * 6f) % foeFrames.Length];
        foeSr.color = Color.Lerp(Color.white, new Color(1f, 0.5f, 0.5f), Mathf.Clamp01(foeFlash -= Time.deltaTime * 6f));
        if (foeDead && Time.time >= foeBackAt) { foeDead = false; foe.gameObject.SetActive(true); }
        if (!foeDead) foe.localScale = Vector3.one * foeScale * (1f + 0.04f * Mathf.Sin(t * 3f));

        // 공격 → 명중 (0.5초마다) · 세 번째 명중마다 처치
        if (!shotLive && !foeDead && Time.time >= nextShot) Fire();
        if (shotLive) FlyShot();

        // 피격 (4초마다) · 필살기 (7초마다)
        if (Time.time >= hurtAt)
        {
            hurtAt = Time.time + 4f;
            heroFlash = 1f;
            SkinAudio.PlayHurt();       // 기본(스킨 없음)은 무음 — 게임의 기본 피격음은 장면에 붙은 클립이라 메뉴에 없음
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
        }
    }

    void Fire()
    {
        shotLive = true;
        shotStart = Time.time;
        shotFrom = hero.position + new Vector3(0.6f, 0.3f, 0f);
        shot.gameObject.SetActive(shotSr.sprite != null);
        heroSr.flipX = false;
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
        if (cursorSr != null) audioFx?.PlayRaw("clank", 0.45f, 1.4f + 0.1f * (skin != null ? skin.tier : 0));
        // 명중: 이펙트 스킨이 있으면 게임 그대로(SkinFx.OnEnemyHit), 없으면 기본 흰 불꽃
        if (SkinFx.EffectSkin != null) SkinFx.OnEnemyHit(to);
        else Fx.Spawn("fx_spark", to, 2.4f, Color.white, 26f);
        if (++hits % 3 != 0) return;

        // 처치: 기본 영혼 폭발 색(KillColor) + 게임과 같은 스킨 처치 연출 · 소리
        Fx.Spawn("fx_sparkle", to, 2.8f, SkinFx.KillColor(new Color(0.6f, 0.85f, 1f)), 16f);
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
        SpecialAbilities.SharedFx = savedShared;
        SpecialAbilities.GlowSprite = savedGlow;
    }
}
