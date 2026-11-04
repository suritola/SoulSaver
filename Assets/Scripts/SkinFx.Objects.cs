using System.Collections.Generic;
using UnityEngine;

// 스킨 물체 칸 (기획 1-2 D1 「설명 약속」 · 4-12 ~ 4-14 「물체」 · 6-1 무기 > 캐릭터 > 기본) — 2026-11-04 마무리
//   묘비 · 더미 · 손 그림: 「<원래 이름>__<스킨 id>」 그림이 있으면 그 그림 (판정 물체는 같은 칸 크기 · 같은 피벗 — tools/pixelart/make_skin_objects.py 상자 검사)
//   물체 순간(매장 · 파기 · 던지기 · 찍힘)은 표의 그 칸 레시피를 그 자리에 · 인형사 줄은 결(재질) · 색만 · 인화 사본은 흰 테 · 파노라마는 화면 모서리 썸네일
public partial class SkinFx
{
    // ================================================================= 물체 그림 시트 (Resources/FX)
    static readonly Dictionary<string, string> objSheet = new Dictionary<string, string>();
    static string objSkins;

    // 무기 스킨 > 캐릭터 스킨 > 기본 — 그 스킨 이름의 시트가 있을 때만 (스킨이 바뀌면 표를 비움)
    //   ObjectLook(칸 값)과 달리 그림이 없으면 다음 순서로 넘어감: 무기 스킨에는 묘비 그림이 없으니 무기 스킨을 껴도 캐릭터 스킨 묘비는 보임 (의도)
    public static string ObjectSheet(string name)
    {
        if (Off || name == null) return name;
        string w = WeaponSkin != null ? WeaponSkin.id : null, c = CharSkin != null ? CharSkin.id : null;
        string key = w + "|" + c;
        if (objSkins != key) { objSheet.Clear(); objSkins = key; }
        if (objSheet.TryGetValue(name, out string got)) return got;
        got = name;
        foreach (string id in new[] { w, c })
        {
            if (id == null) continue;
            string want = name + "__" + id;
            Sprite[] f = Fx.Frames(want);
            if (f != null && f.Length > 0) { got = want; break; }
        }
        objSheet[name] = got;
        return got;
    }

    // ================================================================= 물체 순간 (키트가 부름: bury 매장 · dig 파기 · throw 던지기)
    //   무기 표 · 캐릭터 표에 그 칸이 있으면 둘 다 (그림은 그 자리 · 같은 칸은 0.1초에 한 번)
    public static void OnObject(string slot, Vector3 at, Vector2 dir = default, float vol = 0.45f)
    {
        try { OnObjectCore(slot, at, dir, vol); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnObjectCore(string slot, Vector3 at, Vector2 dir, float vol)
    {
        SkinFx f = Instance;
        if (f == null || Off || f.Hushed) return;
        bool w = WeaponLook != null && WeaponLook.Has(slot), c = CharLook != null && CharLook.Has(slot);
        if (!w && !c) return;
        if (!f.SlotReady(slot, 0.1f)) return;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        if (w) f.Slot(WeaponSkin, WeaponLook, slot, at, dir, false, 30, Bud.Rep, vol, 4f, SkinAudio.Prio.Lvl);
        if (c) f.Slot(CharSkin, CharLook, slot, at, dir, false, 30, Bud.Rep, vol, 4f, SkinAudio.Prio.Lvl);
    }

    // ================================================================= 사진에 찍힌 적 (CharacterKit.PhShoot — 진짜 셔터만)
    //   표의 snap 칸: "soul" = 그 적 그림이 희미한 영혼으로 뒤에 겹쳐 떠오름 (유령 사진가) · 그 밖은 그림 레시피 (F12 픽셀 테)
    public static void OnSnap(Component enemy)
    {
        try { OnSnapCore(enemy); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnSnapCore(Component enemy)
    {
        SkinFx f = Instance;
        if (f == null || Off || enemy == null || f.Hushed) return;
        string r = ObjectLook("snap");
        if (r == null) return;
        SkinLook look = WeaponSkin != null ? WeaponLook : CharLook;
        if (r != "soul") { f.Emit(r, look, enemy.transform.position, Vector2.up, false, 24, Bud.Rep); return; }
        if (!enemy.TryGetComponent(out SpriteRenderer sr) || sr.sprite == null || !sr.enabled || Allow(Bud.Rep, 1) <= 0) return;
        Color c = look.main;
        c.a = 0.4f * GameSettings.MyFxAlpha;
        // 적 판정 · 그림은 그대로 · 뒤(Character −1)에 같은 그림 한 장이 어긋나 떠오르다 사라짐 (0.6초)
        SkinMote m = SkinMote.Make(sr.sprite, sr.transform.position + new Vector3(0.2f, 0.15f, 0f), sr.bounds.size.y, c, "Character", -1, 0.6f, false, true);
        m?.Flip(sr.flipX).Move(new Vector3(0.35f, 0.8f, 0f)).Hold(0.25f);
    }

    // ================================================================= 인형사 줄 (판정 줄 — 같은 굵기 · 결 · 색만)
    //   표의 string 칸 「결 #색」: paper 종이끈 · cable 케이블 · soul 영혼불 실 · plain 색만
    //   매 프레임 불러도 됨 (같은 칸 문자열이면 재질을 다시 찾지 않음) — 릴레이 교대 · 상점에서 스킨이 바뀌면 줄 결도 따라 바뀜 (예전엔 만들 때 한 번만 입혀 앞 주자 결이 남았음)
    static string stringMatHex;
    static Material stringMatWant;
    public static void StringLook(LineRenderer lr)
    {
        if (lr == null) return;
        try
        {
            string s = Off || CharLook == null ? null : CharLook.Fx("string");
            if (!ReferenceEquals(s, stringMatHex))
            {
                stringMatHex = s;
                int sp = s != null ? s.IndexOf(' ') : -1;
                stringMatWant = s == null ? null : StringMat(sp > 0 ? s.Substring(0, sp) : s);
            }
            Material m = stringMatWant;
            if (m != null)
            {
                if (lr.sharedMaterial != m) { lr.sharedMaterial = m; lr.textureMode = LineTextureMode.Tile; }
            }
            else if (lr.sharedMaterial != null && stringMats.ContainsValue(lr.sharedMaterial)) PixelLine.Apply(lr);       // 스킨 결이 남은 줄 → 기본 도트 획으로
        }
        catch (System.Exception e) { GuardFail(e); }
    }

    // 줄 색: 조준색(판정 줄을 알아보는 색)을 반쯤 스킨 색으로
    static string stringHex;
    static bool stringHas;
    static Color stringTint;
    public static Color StringColor(Color aim)
    {
        if (Off || CharLook == null) return aim;
        string s = CharLook.Fx("string");
        if (!ReferenceEquals(s, stringHex))      // 매 프레임 불려서 같은 문자열이면 파싱 결과를 다시 씀
        {
            stringHex = s;
            int sp = s != null ? s.IndexOf(' ') : -1;
            stringHas = sp >= 0 && ColorUtility.TryParseHtmlString(s.Substring(sp + 1).Trim(), out stringTint);
        }
        if (!stringHas) return aim;
        Color t = stringTint;
        return new Color(Mathf.Lerp(aim.r, t.r, 0.5f), Mathf.Lerp(aim.g, t.g, 0.5f), Mathf.Lerp(aim.b, t.b, 0.5f), aim.a);
    }

    static readonly Dictionary<string, Material> stringMats = new Dictionary<string, Material>();
    static Material soulMat;

    static Material StringMat(string kind)
    {
        if (kind == "plain") return null;
        if (stringMats.TryGetValue(kind, out Material m) && m != null) return m;
        // 도트 획과 같은 꼴: 두께 방향 8줄 중 바깥 1줄씩 어두운 테 (PixelLine.Widen 그대로) · 길이 방향 16칸 무늬를 이어 붙임
        const int W = 16, H = 8;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, name = "sk_string_" + kind };
        Color32[] px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float v;
                if (y == 0 || y == H - 1) v = 0.18f;                                   // 바깥 테
                else if (kind == "paper") v = ((x + y * 2) % 8) < 4 ? 1f : 0.7f;        // 꼰 종이끈: 비스듬한 결
                else if (kind == "cable") v = x < 2 ? 0.4f : y == 3 ? 1f : (y == 1 || y == H - 2) ? 0.6f : 0.85f;   // 피복 + 묶음 띠
                else if (kind == "soul") v = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin((x + (y == 3 || y == 4 ? 0 : 4)) * Mathf.PI / 8f));    // 일렁이는 불 결
                else v = 1f;
                byte b = (byte)(Mathf.Clamp01(v) * 255f);
                px[y * W + x] = new Color32(b, b, b, 255);
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        m = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, hideFlags = HideFlags.DontUnloadUnusedAsset, name = "SkinString_" + kind };
        m.mainTextureScale = new Vector2(kind == "cable" ? 1.2f : 2.5f, 1f);       // 1칸에 결 몇 번
        stringMats[kind] = m;
        if (kind == "soul") soulMat = m;
        return m;
    }

    // 영혼불 실은 결이 줄을 따라 흐름 (재질 하나 — 줄마다 아님)
    static void StringTick()
    {
        if (soulMat != null && Time.timeScale > 0f) soulMat.mainTextureOffset = new Vector2(-Time.time * 1.5f, 0f);
    }

    // ================================================================= 인화 사본 (즉석 카메라 「흰 테 사진」) — 사본(미끼) 그림 · 자리 · 크기는 그대로, 뒤에 흰 테 사진 판
    public static void DecorateCopy(SpriteRenderer copy)
    {
        try
        {
            if (copy == null || copy.sprite == null || ObjectLook("copy") != "polaroid") return;
            Sprite frame = SkinArt.Get("polaroid");
            if (frame == null) return;
            GameObject g = new GameObject("SkinPolaroid");
            g.transform.SetParent(copy.transform, false);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = frame;
            sr.sortingLayerID = copy.sortingLayerID;
            sr.sortingOrder = copy.sortingOrder - 1;
            Bounds b = copy.sprite.bounds;
            Vector2 fs = frame.bounds.size;
            float w = b.size.x * 1.25f, h = b.size.y * 1.4f;
            g.transform.localScale = new Vector3(w / Mathf.Max(0.01f, fs.x), h / Mathf.Max(0.01f, fs.y), 1f);
            g.transform.localPosition = new Vector3(b.center.x, b.center.y - b.size.y * 0.1f, 0f);       // 아래 테가 두꺼운 즉석 사진
        }
        catch (System.Exception e) { GuardFail(e); }
    }

    // ================================================================= 파노라마 「저장됨」 썸네일 (F12) — 필살기 시전 때 화면 모서리 (글자 없음 · 체크 표시)
    void CornerOnUlt()
    {
        string r = CharLook != null ? CharLook.Fx("corner") : null;
        if (r != null && !Hushed) SkinCorner.Show(r, CharLook.main);
    }
}

// 화면 오른쪽 아래 모서리 썸네일 (카메라 자식 · 게임 시간 — 일시정지면 멈춤) — 「글리프 글리프」 두 장을 겹침
public sealed class SkinCorner : MonoBehaviour
{
    const float In = 0.15f, Stay = 1.2f, Out = 0.35f;
    static SkinCorner instance;
    readonly SpriteRenderer[] parts = new SpriteRenderer[2];
    Camera cam;
    float t = 99f;
    Color color;

    public static void Show(string recipe, Color c)
    {
        if (SkinFx.Off || string.IsNullOrEmpty(recipe)) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        if (instance == null || instance.cam != cam)
        {
            GameObject go = new GameObject("SkinCorner");
            go.transform.SetParent(cam.transform, false);
            instance = go.AddComponent<SkinCorner>();
            instance.cam = cam;
            for (int i = 0; i < 2; i++)
            {
                GameObject g = new GameObject("Part" + i);
                g.transform.SetParent(go.transform, false);
                SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Effect";
                sr.sortingOrder = 38 + i;
                instance.parts[i] = sr;
            }
        }
        string[] p = recipe.Split(' ');
        for (int i = 0; i < 2; i++) instance.parts[i].sprite = i < p.Length ? SkinArt.Get(p[i]) : null;
        instance.color = c;
        instance.t = 0f;
        instance.gameObject.SetActive(true);
    }

    void LateUpdate()
    {
        if (cam == null) { Destroy(gameObject); return; }
        t += Time.deltaTime;
        float total = In + Stay + Out;
        if (t >= total) { gameObject.SetActive(false); return; }
        float h = cam.orthographicSize, w = h * cam.aspect;
        float slide = t < In ? 1f - t / In : 0f;
        float a = t < In + Stay ? 1f : 1f - (t - In - Stay) / Out;
        float size = h * 0.22f;
        Vector3 at = new Vector3(w - size * 0.8f + slide * size, -h + size * 0.8f, 1f);
        for (int i = 0; i < 2; i++)
        {
            SpriteRenderer sr = parts[i];
            if (sr == null || sr.sprite == null) continue;
            float sh = sr.sprite.bounds.size.y;
            float k = i == 0 ? size : size * 0.55f;
            sr.transform.localPosition = at + (i == 0 ? Vector3.zero : new Vector3(size * 0.3f, -size * 0.25f, 0f));
            sr.transform.localScale = Vector3.one * (sh > 0f ? k / sh : 1f);
            Color c = i == 0 ? Color.white : color;
            sr.color = new Color(c.r, c.g, c.b, a * 0.95f * GameSettings.MyFxAlpha);
        }
    }
}
