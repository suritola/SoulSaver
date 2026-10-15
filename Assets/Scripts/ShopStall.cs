using TMPro;
using UnityEngine;

// 떠돌이 상점 제단: 일반 몹을 여러 마리 잡을 때마다 플레이어 근처에 나타나고,
// 가까이 가서 상호작용 키(기본 Space)를 누르면 상점이 열림. 15초가 지나면 사라짐 (Shop이 만듦, 놓치면 다음 상점이 빨리 옴)
public class ShopStall : MonoBehaviour
{
    public float lifetime = 15f;        // 1.0.4~ 10초 → 15초 (다가가기도 전에 사라져 억울했음)
    public float openDistance = 6f;

    // 플레이어가 제단 앞에 있으면 상호작용 키는 상점 열기 (스킬 대신)
    public static bool PlayerNear;

    Shop shop;
    float age;
    SpriteRenderer sr;
    LineRenderer timerRing;
    TextMeshPro label;
    GameObject glow;
    // 다가가면 뜨는 안내: 말풍선 + [SPACE] 키 + "상점 열기"
    GameObject prompt;
    FxAnim keycap;
    TextMeshPro promptText;
    LineRenderer nearRing;
    float promptShow;

    static Sprite stallSprite;

    public static ShopStall Spawn(Shop shop, Vector3 pos)
    {
        GameObject go = new GameObject("ShopStall");
        go.transform.position = pos;
        ShopStall s = go.AddComponent<ShopStall>();
        s.shop = shop;
        return s;
    }

    void Start()
    {
        if (SpecialAbilities.GlowSprite != null)
        {
            glow = SpecialAbilities.MakeSprite("StallGlow", SpecialAbilities.GlowSprite, transform.position, 0.45f, new Color(1f, 0.8f, 0.35f, 0.4f), "Effect", 0);
            glow.transform.SetParent(transform, true);
        }
        GameObject body = SpecialAbilities.MakeSprite("StallBody", StallSprite, transform.position, 1f, Color.white, "Character", 0);
        body.transform.SetParent(transform, true);
        sr = body.GetComponent<SpriteRenderer>();

        timerRing = Hostile.NewLine("StallTimer", new Color(1f, 0.85f, 0.4f, 0.9f), 0.14f, 3);
        timerRing.transform.SetParent(transform, false);

        GameObject t = new GameObject("StallLabel", typeof(TextMeshPro));
        t.transform.SetParent(transform, false);
        t.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        label = t.GetComponent<TextMeshPro>();
        if (shop.priceTextInput != null)
        {
            label.font = shop.priceTextInput.font;
            label.fontSharedMaterial = shop.priceTextInput.fontSharedMaterial;
        }
        label.fontSize = 6f;
        label.alignment = TextAlignmentOptions.Center;
        label.sortingLayerID = SortingLayer.NameToID("Effect");
        label.sortingOrder = 30;
        label.rectTransform.sizeDelta = new Vector2(10f, 2f);

        BuildPrompt();

        ShockRing.Spawn(transform.position, 0.3f, 3f, 0.4f, new Color(1f, 0.85f, 0.4f, 0.9f), 0.25f);
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.Play("chime", 0.8f, 1.2f);
    }

    // 1.0.1: 지속되는 필살기(일제 사격 · 총알 폭풍 · 화염 회오리 …) 중엔 상호작용 키가 무시돼 상점이 안 열렸음
    // → 날아가는 중에도 열림 (상점이 시간을 멈추고, 닫으면 이어서). 시간을 늦춘 조준 중에 누르면 기억했다가 끝나는 순간 엶
    bool pendingOpen;

    void Update()
    {
        age += Time.deltaTime;
        if (pendingOpen) age = Mathf.Min(age, lifetime - 1f);      // 기다리는 동안 사라지지 않게
        float left = lifetime - age;

        // 남은 시간 고리 (줄어듦) + 둥실거림
        Hostile.SetArc(timerRing, transform.position, 1.6f, 90f, 90f + 360f * Mathf.Clamp01(left / lifetime));
        if (sr != null)
        {
            sr.transform.position = transform.position + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.12f;
            // 마지막 3초는 깜빡임
            sr.color = left < 3f && Mathf.PingPong(Time.time * 6f, 1f) < 0.5f ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
        }
        if (glow != null) glow.transform.localScale = Vector3.one * (0.45f + Mathf.Sin(Time.time * 4f) * 0.05f);
        label.text = Loc.T("상점  ") + Mathf.CeilToInt(left);

        // 가까이 오면 안내가 나타나고, 상호작용 키로 상점 열기
        PlayerController p = Hostile.Player;
        bool near = p != null && Vector2.Distance(p.transform.position, transform.position) < openDistance;
        PlayerNear = near;
        UpdatePrompt(near);
        if (near && !PlayerController.Cinematic && KeyBindings.Down(GameAction.Interact)) pendingOpen = true;
        if (pendingOpen && (p == null || Vector2.Distance(p.transform.position, transform.position) > openDistance * 1.6f)) pendingOpen = false;
        if (pendingOpen && Time.timeScale == 1f && !p.IsAiming)
        {
            if (shop.OpenFromStall())
            {
                pendingOpen = false;
                PlayerNear = false;
                Destroy(gameObject);
                return;
            }
        }
        if (left <= 0f)
        {
            if (shop != null) shop.StallMissed();      // 1.0.4~ 놓쳤으면 다음 상점을 앞당김
            ShockRing.Spawn(transform.position, 1.5f, 0.2f, 0.3f, new Color(1f, 0.85f, 0.4f, 0.8f), 0.2f);
            PlayerNear = false;
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        PlayerNear = false;
    }

    void BuildPrompt()
    {
        prompt = new GameObject("StallPrompt");
        prompt.transform.SetParent(transform, false);
        prompt.transform.localPosition = new Vector3(0f, 3.6f, 0f);
        FxAnim bubble = Fx.Play("fx_prompt", prompt.transform.position, 1.8f, Color.white, 1f, 0f, 31, true);
        if (bubble != null) bubble.transform.SetParent(prompt.transform, true);
        keycap = Fx.Play("fx_keycap", prompt.transform.position + new Vector3(-1.45f, 0.25f, 0f), 0.95f, Color.white, 2.5f, 0f, 32, true);
        if (keycap != null) keycap.transform.SetParent(prompt.transform, true);

        GameObject t = new GameObject("PromptText", typeof(TextMeshPro));
        t.transform.SetParent(prompt.transform, false);
        t.transform.localPosition = new Vector3(1.35f, 0.25f, 0f);
        promptText = t.GetComponent<TextMeshPro>();
        if (shop.priceTextInput != null)
        {
            promptText.font = shop.priceTextInput.font;
            promptText.fontSharedMaterial = shop.priceTextInput.fontSharedMaterial;
        }
        promptText.text = Loc.T("상점 열기");
        promptText.fontSize = 5f;
        promptText.color = new Color(0.96f, 0.83f, 0.47f);
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.sortingLayerID = SortingLayer.NameToID("Effect");
        promptText.sortingOrder = 33;
        promptText.rectTransform.sizeDelta = new Vector2(4f, 1.2f);

        // 키 그림은 SPACE라서, 상호작용 키를 바꿨으면 그림 대신 글자로 키 이름을 보여 줌
        if (KeyBindings.Get(GameAction.Interact) != KeyCode.Space || GamePad.Active)     // 1.0.5~ 패드면 [A]
        {
            if (keycap != null) keycap.gameObject.SetActive(false);
            t.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            promptText.rectTransform.sizeDelta = new Vector2(6f, 1.2f);
            promptText.text = "[" + KeyBindings.Name(GameAction.Interact) + "] " + Loc.T("상점 열기");
        }

        nearRing = Hostile.NewLine("StallNearRing", new Color(1f, 0.85f, 0.4f, 0f), 0.16f, 2);
        nearRing.transform.SetParent(transform, false);
        nearRing.loop = true;
        prompt.transform.localScale = Vector3.zero;
    }

    // 범위 안이면 말풍선이 튀어나오고 키가 눌렸다 올라옴
    void UpdatePrompt(bool near)
    {
        promptShow = Mathf.MoveTowards(promptShow, near ? 1f : 0f, Time.unscaledDeltaTime * 6f);
        float pop = promptShow <= 0f ? 0f : 1f + 0.12f * Mathf.Sin(promptShow * Mathf.PI);
        prompt.transform.localScale = Vector3.one * promptShow * pop;
        prompt.transform.localPosition = new Vector3(0f, 3.6f + Mathf.Sin(Time.time * 3f) * 0.1f, 0f);
        label.gameObject.SetActive(promptShow < 0.5f);

        if (nearRing != null)
        {
            Hostile.SetArc(nearRing, transform.position, openDistance, 0f, 354f);
            float a = promptShow * (0.5f + 0.3f * Mathf.Sin(Time.time * 6f));
            nearRing.startColor = nearRing.endColor = new Color(1f, 0.85f, 0.4f, a);
        }
        if (glow != null && near) glow.transform.localScale = Vector3.one * (0.6f + Mathf.Sin(Time.time * 6f) * 0.08f);
    }

    // 금화 표지가 달린 작은 상인 제단 (코드로 그린 도트)
    public static Sprite StallSprite =>stallSprite != null ? stallSprite : (stallSprite = MakeStall());

    static Sprite MakeStall()
    {
        const int W = 32, H = 32;
        string[] rows =
        {
            "................................",
            "...........oooooooooo...........",
            "..........oYYYYYYYYYYo..........",
            ".........oYyyyyyyyyyyYo.........",
            ".........oYy..oooo..yYo.........",
            ".........oYy.oYYYYo.yYo.........",
            ".........oYy.oYooYo.yYo.........",
            ".........oYy.oYYYYo.yYo.........",
            ".........oYy..oooo..yYo.........",
            ".........oYyyyyyyyyyyYo.........",
            "..........oYYYYYYYYYYo..........",
            "...........oooo..oooo...........",
            "..............o..o..............",
            "......oooooooooooooooooooo......",
            ".....oRRrRRrRRrRRrRRrRRrRRo.....",
            "....oRRrRRrRRrRRrRRrRRrRRrRo....",
            "...oooooooooooooooooooooooooo...",
            "...oWWWWWWWWWWWWWWWWWWWWWWWWo...",
            "...oWwwwwwwwwwwwwwwwwwwwwwwWo...",
            "...oWwBBBBwwwwwwwwwwwwBBBBwWo...",
            "...oWwBGGBwwwwYYYYwwwwBGGBwWo...",
            "...oWwBBBBwwwYyyyYwwwwBBBBwWo...",
            "...oWwwwwwwwwwYYYYwwwwwwwwwWo...",
            "...oWWWWWWWWWWWWWWWWWWWWWWWWo...",
            "...oDDDDDDDDDDDDDDDDDDDDDDDDo...",
            "...oDddddddddddddddddddddddDo...",
            "...oDdoooDddddddddddddoooDdDo...",
            "...oDdo.oDddddddddddddo.oDdDo...",
            "...ooo..ooooooooooooooo..ooo....",
            "................................",
            "................................",
            "................................",
        };
        var pal = new System.Collections.Generic.Dictionary<char, Color>
        {
            ['o'] = new Color(0.12f, 0.06f, 0.05f),
            ['Y'] = new Color(1f, 0.82f, 0.3f), ['y'] = new Color(0.85f, 0.6f, 0.15f),
            ['R'] = new Color(0.75f, 0.15f, 0.15f), ['r'] = new Color(0.95f, 0.9f, 0.8f),
            ['W'] = new Color(0.55f, 0.35f, 0.2f), ['w'] = new Color(0.42f, 0.26f, 0.15f),
            ['B'] = new Color(0.25f, 0.15f, 0.1f), ['G'] = new Color(0.4f, 0.9f, 0.5f),
            ['D'] = new Color(0.35f, 0.22f, 0.13f), ['d'] = new Color(0.28f, 0.17f, 0.1f),
        };
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                char c = rows[H - 1 - y][x];
                tex.SetPixel(x, y, pal.TryGetValue(c, out Color col) ? col : new Color(0f, 0f, 0f, 0f));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.3f), 10f);
    }
}
