using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 영혼 트리 화면 (거너): 가운데 = 지금 무기, 여섯 가지가 사방으로 뻗음
//   무기(위) · 필살기(오른쪽 위) · 스킬(오른쪽 아래) · 생존(아래) · 영혼(왼쪽 아래) · 재물(왼쪽 위)
// 배운 칸과 바로 다음 칸만 보이고 그 너머는 숨김 (배울수록 트리가 펼쳐짐)
// 트리는 화면보다 커서 끌어서 움직이고(좌 · 우클릭 드래그, WASD · 방향키) 휠로 확대 · 축소
// 칸을 누르면 영혼 조각으로 바로 배움. 게임은 멈춘 채로 열림
public class SoulTreeUI : MonoBehaviour
{
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Soul = new Color(0.72f, 0.58f, 1f);
    static readonly Color Dim = new Color(0.45f, 0.42f, 0.5f);
    static readonly Color[] BranchColor =
    {
        new Color(1f, 0.62f, 0.3f),     // 무기
        new Color(0.5f, 0.9f, 1f),      // 필살기
        new Color(0.45f, 0.75f, 1f),    // 스킬
        new Color(0.55f, 0.95f, 0.5f),  // 생존
        Soul,                           // 영혼
        new Color(1f, 0.85f, 0.35f),    // 재물
    };
    static readonly float[] BranchAngle = { 90f, 30f, -30f, -90f, -150f, 150f };
    const float Sector = 54f;           // 가지 하나가 쓸 수 있는 각도
    const float R0 = 210f, RStep = 135f; // 깊이 1 반지름 · 깊이마다 늘어나는 거리
    const float Gap = 124f;             // 옆 칸과의 최소 거리 (칸 78 + 아래 가격 글자 + 여유)
    const float MinZoom = 0.2f, MaxZoom = 1.4f;

    public static SoulTreeUI Instance { get; private set; }
    public static bool IsOpen => Instance != null;

    SpecialAbilities sp;
    System.Action onClose;
    RectTransform root, viewport, board;
    TMP_Text shardText, footText, affordText;
    RectTransform shardBox;
    float shardFlash;
    // 영혼 정련 (1.0.4~): 트리를 모두 배운 뒤 남는 조각을 쓰는 버튼 (조각 상자 아래)
    Button refineBtn;
    TMP_Text refineText;
    bool refineReady;
    readonly List<NodeView> views = new List<NodeView>();
    readonly List<GameObject> decor = new List<GameObject>();
    List<SpecialAbilities.SoulNode> nodes;
    Dictionary<string, Vector2> pos;
    Dictionary<int, float> branchScale;
    int shownShards = -1;
    float zoom = 0.7f;
    static Vector2 lastPan;             // 다시 열 때 보던 자리 그대로
    static float lastZoom = 0.7f;

    class NodeView
    {
        public SpecialAbilities.SoulNode node;
        public RectTransform rect;
        public Image frame, icon, glow, line, badge;
        public TMP_Text cost;
    }

    public static void Open(SpecialAbilities sp, System.Action onClose)
    {
        if (Instance != null || sp == null) return;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        UIKit.EnsureStyle();
        GameObject go = new GameObject("SoulTree", typeof(RectTransform));
        SoulTreeUI ui = go.AddComponent<SoulTreeUI>();
        ui.sp = sp;
        ui.onClose = onClose;
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.rootCanvas.transform, false);
        Stretch(r);
        r.SetAsLastSibling();
        OnTop(go, 500);
        ui.root = r;
        Instance = ui;
        ui.Build();
        SpecialAbilities.SharedFx?.Play("shimmer", 0.5f, 1.2f);
    }

    public static void Close()
    {
        if (Instance == null) return;
        TooltipUI.Hide();
        SoulTreeUI ui = Instance;
        lastPan = ui.board.anchoredPosition;
        lastZoom = ui.zoom;
        Instance = null;
        Destroy(ui.gameObject);
        ui.onClose?.Invoke();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    int openFrame;

    void Update()
    {
        if (GameInput.EscDown)
        {
            SettingsUI.EscHandledFrame = Time.frameCount;      // 일시정지 메뉴가 같이 열리지 않게
            Close();
            return;
        }
        if (KeyBindings.Down(GameAction.Upgrade) && Time.frameCount > openFrame) { Close(); return; }

        // 휠: 마우스 위치를 기준으로 확대 · 축소
        float wheel = Input.mouseScrollDelta.y;
        if (wheel != 0f) ZoomAt(Input.mousePosition, zoom * (wheel > 0f ? 1.12f : 1f / 1.12f));
        // 1.0.5~ 패드: RT 확대 · LT 축소 (가상 커서 기준)
        float padZoom = GamePad.ZoomAxis;
        if (padZoom != 0f) ZoomAt(GameInput.PointerPosition, zoom * Mathf.Pow(2f, padZoom * 1.5f * Time.unscaledDeltaTime));
        // 키보드로 이동 (이동 키 · 방향키, 1.0.4~ 바꾼 이동 키를 따름), 가운데로 (Home · C)
        Vector2 k = Vector2.zero;
        if (KeyBindings.Held(GameAction.Left)) k.x += 1f;
        if (KeyBindings.Held(GameAction.Right)) k.x -= 1f;
        if (KeyBindings.Held(GameAction.Up)) k.y -= 1f;
        if (KeyBindings.Held(GameAction.Down)) k.y += 1f;
        k -= GamePad.RightStick;            // 1.0.5~ 패드 오른쪽 스틱으로 이동 (기울인 만큼)
        if (k != Vector2.zero) Pan(k * 900f * Time.unscaledDeltaTime);
        if (Input.GetKeyDown(KeyCode.Home) || Input.GetKeyDown(KeyCode.C)) Recenter();
        if (Input.GetKeyDown(KeyCode.F)) Fit();
        // 끌어서 이동: 빈 곳에서 좌클릭 · 휠 클릭 · 우클릭을 누른 채 끌기 (칸 위에서 누르면 배우기라 끌지 않음)
        Vector2 mouse = GameInput.PointerPosition;      // 1.0.5~ 패드 가상 커서도 화면 끝에 대면 그쪽으로 이동
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1)) && hovered.Count == 0) dragging = true;
        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2)) dragging = false;
        if (dragging) Pan(mouse - lastMouse);
        // 마우스를 화면 끝에 대면 그쪽으로 천천히 이동
        else if (Application.isFocused && mouse.x >= 0f && mouse.y >= 0f && mouse.x <= Screen.width && mouse.y <= Screen.height)
        {
            const float edge = 18f;
            Vector2 e = Vector2.zero;
            if (mouse.x < edge) e.x += 1f;
            if (mouse.x > Screen.width - edge) e.x -= 1f;
            if (mouse.y < edge) e.y += 1f;
            if (mouse.y > Screen.height - edge) e.y -= 1f;
            if (e != Vector2.zero) Pan(e * 700f * Time.unscaledDeltaTime);
        }
        lastMouse = mouse;

        if (shownShards != SoulShards.Amount) Refresh();
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
        foreach (NodeView v in views)
        {
            bool can = sp.CanBuy(v.node);
            v.glow.enabled = can;
            if (!can) continue;
            Color c = BranchColor[v.node.branch];
            v.glow.color = new Color(Mathf.Lerp(c.r, 1f, 0.3f), Mathf.Lerp(c.g, 1f, 0.3f), Mathf.Lerp(c.b, 1f, 0.3f), 0.45f + 0.45f * pulse);
            v.glow.rectTransform.sizeDelta = Vector2.one * (170f + 40f * pulse);
            v.frame.color = Color.Lerp(new Color(1f, 0.9f, 0.5f), Color.white, pulse);
            if (!hovered.Contains(v)) v.rect.localScale = Vector3.one * (1f + 0.06f * pulse);
        }
        // 정련할 수 있으면 버튼이 숨 쉬듯 (1.0.4~)
        if (refineBtn != null) refineBtn.transform.localScale = Vector3.one * (refineReady ? 1f + 0.04f * pulse : 1f);
        // 조각이 바뀌면 숫자 상자가 잠깐 번쩍
        if (shardBox != null)
        {
            shardFlash = Mathf.Max(0f, shardFlash - Time.unscaledDeltaTime * 2.5f);
            shardBox.localScale = Vector3.one * (1f + 0.12f * shardFlash);
        }
    }

    Vector2 lastMouse;
    bool dragging;
    Rect bounds = new Rect(-200f, -200f, 400f, 400f);   // 보이는 칸들이 퍼진 범위 (보드 좌표)
    static bool fittedOnce;                             // 처음 열 때는 전체가 보이게 맞춤
    readonly HashSet<NodeView> hovered = new HashSet<NodeView>();

    // ================================================================= 이동 · 확대
    float CanvasScale => root.lossyScale.x > 0f ? root.lossyScale.x : 1f;

    void Pan(Vector2 screenDelta)
    {
        board.anchoredPosition += screenDelta / CanvasScale;
        ClampPan();
    }

    void ZoomAt(Vector2 screenPoint, float newZoom)
    {
        newZoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(board, screenPoint, null, out Vector2 before);
        zoom = newZoom;
        board.localScale = Vector3.one * zoom;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(board, screenPoint, null, out Vector2 after);
        board.anchoredPosition += (after - before) * zoom;
        ClampPan();
    }

    void Recenter()
    {
        board.anchoredPosition = Vector2.zero;
        zoom = 0.7f;
        board.localScale = Vector3.one * zoom;
    }

    // 이동 한도: 보이는 칸 가운데 가장 바깥 칸까지 화면 가운데로 끌어올 수 있게 (그 너머로는 못 감)
    void ClampPan()
    {
        const float margin = 300f;
        Vector2 p = board.anchoredPosition;
        board.anchoredPosition = new Vector2(
            Mathf.Clamp(p.x, -(bounds.xMax + margin) * zoom, -(bounds.xMin - margin) * zoom),
            Mathf.Clamp(p.y, -(bounds.yMax + margin) * zoom, -(bounds.yMin - margin) * zoom));
    }

    // 전체 보기: 보이는 칸이 한 화면에 모두 들어오게 확대 · 축소하고 가운데를 맞춤
    void Fit()
    {
        const float w = 1760f, h = 780f;                // 위 · 아래 띠를 뺀 화면
        float z = Mathf.Min(w / Mathf.Max(1f, bounds.width + 160f), h / Mathf.Max(1f, bounds.height + 160f));
        zoom = Mathf.Clamp(z, MinZoom, 1f);
        board.localScale = Vector3.one * zoom;
        board.anchoredPosition = -bounds.center * zoom + new Vector2(0f, -36f);
        ClampPan();
    }

    // ================================================================= 만들기
    void Build()
    {
        openFrame = Time.frameCount;
        lastMouse = GameInput.PointerPosition;
        Image dim = Img("Dim", root, Vector2.zero, Vector2.zero, null, new Color(0.03f, 0.02f, 0.05f, 0.97f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        viewport = UIKit.Rect("Viewport", root, Vector2.zero, Vector2.zero);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        board = UIKit.Rect("Board", viewport, Vector2.zero, new Vector2(100f, 100f));
        board.anchoredPosition = lastPan;
        zoom = lastZoom;
        board.localScale = Vector3.one * zoom;

        // 위 · 아래 띠: 트리가 그 밑으로 지나가도 글이 잘 보이게
        Image top = Img("TopBar", root, Vector2.zero, Vector2.zero, null, new Color(0.05f, 0.03f, 0.08f, 0.92f));
        top.rectTransform.anchorMin = new Vector2(0f, 1f); top.rectTransform.anchorMax = Vector2.one;
        top.rectTransform.pivot = new Vector2(0.5f, 1f); top.rectTransform.sizeDelta = new Vector2(0f, 136f);
        top.rectTransform.anchoredPosition = Vector2.zero;
        Image bottom = Img("BottomBar", root, Vector2.zero, Vector2.zero, null, new Color(0.05f, 0.03f, 0.08f, 0.92f));
        bottom.rectTransform.anchorMin = Vector2.zero; bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottom.rectTransform.pivot = new Vector2(0.5f, 0f); bottom.rectTransform.sizeDelta = new Vector2(0f, 64f);
        bottom.rectTransform.anchoredPosition = Vector2.zero;

        TMP_Text title = UIKit.Text(root, "영혼 트리", 54f, Gold, new Vector2(0f, 480f), new Vector2(800f, 70f));
        title.fontStyle = FontStyles.Bold;
        UIKit.Pin(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f));
        shardBox = UIKit.Rect("Shards", root, new Vector2(-735f, 470f), new Vector2(400f, 108f));
        Image sb = shardBox.gameObject.AddComponent<Image>();
        sb.sprite = UIKit.ButtonSprite;
        sb.type = Image.Type.Sliced;
        sb.color = new Color(0.55f, 0.42f, 0.85f);
        sb.raycastTarget = false;
        UIKit.Pin(shardBox, new Vector2(0f, 1f), new Vector2(225f, -70f));
        Img("Inner", shardBox, Vector2.zero, new Vector2(380f, 88f), null, new Color(0.08f, 0.05f, 0.14f, 0.97f));
        Img("OrbGlow", shardBox, new Vector2(-148f, 0f), new Vector2(110f, 110f), sp.glowSprite, new Color(0.75f, 0.55f, 1f, 0.7f));
        Img("Orb", shardBox, new Vector2(-148f, 0f), new Vector2(44f, 44f), sp.glowSprite, new Color(0.95f, 0.9f, 1f));
        UIKit.Text(shardBox, "영혼 조각", 20f, new Color(0.8f, 0.7f, 1f), new Vector2(20f, 26f), new Vector2(300f, 28f), TextAlignmentOptions.Left);
        shardText = UIKit.Text(shardBox, "", 50f, Color.white, new Vector2(20f, -6f), new Vector2(300f, 56f), TextAlignmentOptions.Left);
        shardText.fontStyle = FontStyles.Bold;
        affordText = UIKit.Text(root, "", 22f, new Color(0.7f, 1f, 0.6f), new Vector2(-735f, 400f), new Vector2(400f, 30f), TextAlignmentOptions.Center);
        UIKit.Pin(affordText.rectTransform, new Vector2(0f, 1f), new Vector2(225f, -140f));
        refineBtn = UIKit.MakeButton(root, "", Vector2.zero, new Vector2(400f, 64f), RefineClick, 22f);
        refineBtn.name = "RefineButton";
        UIKit.Pin((RectTransform)refineBtn.transform, new Vector2(0f, 1f), new Vector2(225f, -196f));
        refineText = refineBtn.GetComponentInChildren<TMP_Text>();
        refineText.enableWordWrapping = true;
        refineText.enableAutoSizing = true;
        refineText.fontSizeMin = 12f;
        refineText.fontSizeMax = 22f;
        TooltipTrigger refineTip = refineBtn.gameObject.AddComponent<TooltipTrigger>();
        refineTip.titleProvider = () => Loc.T("영혼 정련");
        refineTip.bodyProvider = RefineTip;
        // 위 띠 오른쪽: 화면 오른쪽 끝에 붙여서 화면 비율이 달라도 잘리지 않게
        Button closeBtn = UIKit.MakeButton(root, "", Vector2.zero, new Vector2(230f, 62f), Close, 24f);
        closeBtn.GetComponentInChildren<TMP_Text>().text = Loc.T("닫기") + " [" + KeyBindings.Name(GameAction.Upgrade) + "]";
        UIKit.Pin((RectTransform)closeBtn.transform, Vector2.one, new Vector2(-170f, -60f));
        Button fitBtn = UIKit.MakeButton(root, "", Vector2.zero, new Vector2(200f, 62f), Fit, 22f);
        fitBtn.GetComponentInChildren<TMP_Text>().text = Loc.T("전체 보기") + " [F]";
        UIKit.Pin((RectTransform)fitBtn.transform, Vector2.one, new Vector2(-405f, -60f));
        footText = UIKit.Text(root, "", 22f, Dim, Vector2.zero, new Vector2(1800f, 36f));
        UIKit.Pin(footText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 32f));       // 아래 띠(64) 가운데
        // 가지 색 범례 (트리 위에 이름을 띄우면 칸과 겹쳐서, 위 띠에 한 줄로)
        string legend = "";
        for (int b = 0; b < SpecialAbilities.BranchCount; b++)
            legend += (b > 0 ? "   " : "") + "<color=#" + ColorUtility.ToHtmlStringRGB(BranchColor[b]) + ">" + Loc.T(SpecialAbilities.BranchNames[b]) + "</color>";
        TMP_Text legendText = UIKit.Text(root, "", 22f, Parch, Vector2.zero, new Vector2(1000f, 30f));
        legendText.text = legend;
        UIKit.Pin(legendText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f));

        Rebuild();
        // 처음 열 때는 전체 보기, 그 뒤로는 보던 자리 (새 범위 안으로)
        if (!fittedOnce) { fittedOnce = true; Fit(); }
        else ClampPan();
    }

    void Rebuild()
    {
        foreach (NodeView v in views) { if (v.rect != null) Destroy(v.rect.gameObject); if (v.line != null) Destroy(v.line.gameObject); }
        views.Clear();
        hovered.Clear();
        foreach (GameObject g in decor) if (g != null) Destroy(g);
        decor.Clear();

        nodes = sp.BuildSoulTree();
        nodes.RemoveAll(n => n.hidden);
        pos = Layout(nodes);

        // 보이는 칸만 (배운 칸 + 바로 다음 칸). 선 먼저 (칸 아래에 깔리게)
        int hiddenCount = 0;
        List<SpecialAbilities.SoulNode> shown = new List<SpecialAbilities.SoulNode>();
        foreach (SpecialAbilities.SoulNode n in nodes)
        {
            if (sp.IsVisibleNode(n)) shown.Add(n);
            else hiddenCount++;
        }
        Dictionary<string, Image> lines = new Dictionary<string, Image>();
        foreach (SpecialAbilities.SoulNode n in shown)
        {
            Vector2 from = n.parent != null && pos.ContainsKey(n.parent) ? pos[n.parent] : Vector2.zero;
            lines[n.key] = Line(from, pos[n.key]);
        }
        BuildCenter();
        foreach (SpecialAbilities.SoulNode n in shown)
        {
            NodeView v = BuildNode(n, pos[n.key]);
            v.line = lines[n.key];
            views.Add(v);
        }
        footText.text = Loc.T(TutorialRun.Active ? "칸을 누르면 배웁니다 · 금빛 표시는 추천 칸" : "칸을 두 번 누르면 배웁니다 · 금빛 표시는 추천 칸") + "  ·  " + Loc.T("숨은 칸") + " " + hiddenCount
                      + "  ·  " + Loc.T(GamePad.Active ? "오른쪽 스틱 · 화면 끝으로 이동, RT · LT 확대 · 축소, 십자키로 칸 고르기"      // 1.0.5~ 패드 안내
                                                       : "끌기 · {MOVE} · 화면 끝으로 이동, 휠 확대, [C] 가운데로, [F] 전체 보기")
                      + "  ·  " + Loc.T("[Shift]+[{UPGRADE}] 트리를 열지 않고 빠르게 배우기");      // 1.0.4~ 숨은 단축키 안내 · 바꾼 키를 따름
        // 보이는 칸의 범위 (이동 한도 · 전체 보기)
        bounds = new Rect(-120f, -120f, 240f, 240f);
        foreach (NodeView v in views)
        {
            Vector2 q = v.rect.anchoredPosition;
            bounds.xMin = Mathf.Min(bounds.xMin, q.x - 60f); bounds.xMax = Mathf.Max(bounds.xMax, q.x + 60f);
            bounds.yMin = Mathf.Min(bounds.yMin, q.y - 80f); bounds.yMax = Mathf.Max(bounds.yMax, q.y + 60f);
        }
        Refresh();
    }

    void BuildCenter()
    {
        RectTransform c = UIKit.Rect("Center", board, Vector2.zero, new Vector2(170f, 170f));
        decor.Add(c.gameObject);
        Img("Glow", c, Vector2.zero, new Vector2(340f, 340f), sp.glowSprite, new Color(Soul.r, Soul.g, Soul.b, 0.35f));
        Image ring = Img("Ring", c, Vector2.zero, new Vector2(170f, 170f), UIKit.ButtonSprite, Gold);
        ring.type = Image.Type.Sliced;
        Img("Inner", c, Vector2.zero, new Vector2(150f, 150f), null, new Color(0.1f, 0.07f, 0.15f, 1f));
        Sprite w = sp.MainWeaponIcon;
        Image icon = Img("Weapon", c, new Vector2(0f, 14f), new Vector2(104f, 104f), w, Color.white);
        icon.preserveAspect = true;
        TMP_Text name = UIKit.Text(c, "", 22f, Gold, new Vector2(0f, -52f), new Vector2(160f, 30f));
        name.text = sp.MainWeaponName;
    }

    NodeView BuildNode(SpecialAbilities.SoulNode n, Vector2 at)
    {
        NodeView v = new NodeView { node = n };
        v.rect = UIKit.Rect("Node_" + n.key, board, at, new Vector2(78f, 78f));
        v.glow = Img("Glow", v.rect, Vector2.zero, new Vector2(150f, 150f), sp.glowSprite, Color.clear);
        v.frame = Img("Frame", v.rect, Vector2.zero, new Vector2(78f, 78f), UIKit.ButtonSprite, Color.white);
        v.frame.type = Image.Type.Sliced;
        v.frame.raycastTarget = true;
        Img("Back", v.rect, Vector2.zero, new Vector2(64f, 64f), null, new Color(0.08f, 0.06f, 0.12f, 1f));
        v.icon = Img("Icon", v.rect, Vector2.zero, new Vector2(56f, 56f), n.icon, Color.white);
        v.icon.preserveAspect = true;
        v.cost = UIKit.Text(v.rect, "", 20f, Parch, new Vector2(0f, -54f), new Vector2(120f, 28f));
        // 추천 칸: 오른쪽 위 금빛 마름모 (아직 안 배운 칸만)
        v.badge = Img("Badge", v.rect, new Vector2(34f, 34f), new Vector2(18f, 18f), null, Gold);
        v.badge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        v.badge.enabled = n.recommended && !sp.OwnsNode(n.key);

        Button b = v.frame.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() => Buy(v));
        EventTrigger et = v.frame.gameObject.AddComponent<EventTrigger>();
        AddTrigger(et, EventTriggerType.PointerEnter, () => { TooltipUI.Show(n.name, Tip(n)); hovered.Add(v); v.rect.localScale = Vector3.one * 1.15f; });
        AddTrigger(et, EventTriggerType.PointerExit, () => { TooltipUI.Hide(); hovered.Remove(v); if (v.rect != null) v.rect.localScale = Vector3.one; });
        return v;
    }

    // 1.0.4~: 칸을 한 번 누르면 바로 배워져 실수로 사는 일이 있었음 → 첫 클릭은 고르기만, 같은 칸을 4초 안에 한 번 더 누르면 배움
    //   튜토리얼은 안내 문구가 「누르면 배움」이라 예전처럼 한 번에
    NodeView armed;
    float armedUntil;

    void Buy(NodeView v)
    {
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        if (!sp.CanBuy(v.node))
        {
            armed = null;
            fx?.Play("buzz", 0.5f);
            StartCoroutine(Shake(v.rect));
            TooltipUI.Show(v.node.name, Tip(v.node));
            return;
        }
        if (!TutorialRun.Active && (armed != v || Time.unscaledTime > armedUntil))
        {
            armed = v;
            armedUntil = Time.unscaledTime + 4f;
            v.rect.localScale = Vector3.one * 1.25f;
            TooltipUI.Show(v.node.name, Loc.T(Tip(v.node)) + "\n\n<color=#7CFF9A>" + Loc.T("한 번 더 누르면 배웁니다") + "</color>");
            return;
        }
        armed = null;
        string key = v.node.key;
        Vector2 at = v.rect.anchoredPosition;
        Color c = BranchColor[v.node.branch];
        if (!sp.BuyNode(v.node)) return;
        // 새 칸이 드러나고 (패시브 장착 등으로) 설명이 바뀌므로 다시 만듦. 칸 자리는 그대로
        Rebuild();
        for (int i = 0; i < 14; i++) StartCoroutine(Burst(at, c));
        TooltipUI.Hide();
        foreach (NodeView nv in views) if (nv.node.key == key) nv.rect.localScale = Vector3.one * 1.12f;
    }

    void Refresh()
    {
        if (shownShards >= 0 && shownShards != SoulShards.Amount) shardFlash = 1f;
        shownShards = SoulShards.Amount;
        shardText.text = SoulShards.Amount.ToString();
        int canCount = 0;
        foreach (NodeView v in views) if (sp.CanBuy(v.node)) canCount++;
        bool complete = nodes != null && sp.TreeComplete(nodes);
        affordText.text = canCount > 0 ? Loc.T("지금 배울 수 있는 칸") + " " + canCount : complete ? Loc.T("모든 칸을 배웠습니다") : Loc.T("조각을 더 모으세요");
        affordText.color = canCount > 0 ? new Color(0.7f, 1f, 0.6f) : complete ? Gold : new Color(0.6f, 0.56f, 0.62f);
        RefreshRefine(complete);
        foreach (NodeView v in views)
        {
            SpecialAbilities.SoulNode n = v.node;
            bool owned = sp.OwnsNode(n.key);
            bool can = sp.CanBuy(n);
            bool blocked = !owned && sp.BlockReason(n) != null;
            Color bc = BranchColor[n.branch];
            // 배운 칸 = 금색, 살 수 있는 칸 = 맥박치는 흰 금빛 (Update), 조각이 모자란 칸 = 흐리게
            v.frame.color = owned ? Gold : blocked ? new Color(0.35f, 0.32f, 0.38f) : can ? Color.white : new Color(bc.r * 0.55f, bc.g * 0.55f, bc.b * 0.55f);
            v.icon.color = owned || can ? Color.white : new Color(0.55f, 0.53f, 0.58f, 0.85f);
            if (!can && !hovered.Contains(v)) v.rect.localScale = Vector3.one;
            v.cost.text = owned ? "" : n.cost.ToString();           // 배운 칸은 금색 테두리로 충분 (글자는 지저분해서 뺌)
            v.cost.fontStyle = can ? FontStyles.Bold : FontStyles.Normal;
            v.cost.color = owned ? Gold : can ? new Color(0.6f, 1f, 0.5f) : new Color(1f, 0.5f, 0.45f);
            if (v.line != null) v.line.color = owned ? new Color(Gold.r, Gold.g, Gold.b, 0.9f) : new Color(bc.r, bc.g, bc.b, 0.55f);
        }
    }

    // ================================================================= 영혼 정련 (1.0.4~)
    void RefreshRefine(bool complete)
    {
        if (refineBtn == null) return;
        int cost = sp.RefineCost;
        refineReady = complete && SoulShards.Amount >= cost;
        refineText.text = "<b>" + Loc.T("영혼 정련") + (complete ? "  " + cost : "") + (sp.Refines > 0 ? "  <color=#C9A0FF>x" + sp.Refines + "</color>" : "") + "</b>\n<size=72%>"
                        + (complete ? Loc.T("모든 무기 피해 +2% · 최대 체력 +4") : Loc.T("트리를 모두 배우면 열립니다")) + "</size>";
        refineText.color = refineReady ? Color.white : complete ? new Color(1f, 0.6f, 0.55f) : new Color(0.6f, 0.56f, 0.62f);
        Image bg = refineBtn.GetComponent<Image>();
        if (bg != null) bg.color = refineReady ? new Color(0.78f, 0.62f, 1f) : complete ? new Color(0.5f, 0.42f, 0.66f) : new Color(0.34f, 0.31f, 0.4f);
    }

    void RefineClick()
    {
        if (!sp.Refine(nodes))
        {
            SpecialAbilities.SharedFx?.Play("buzz", 0.5f);
            StartCoroutine(Shake((RectTransform)refineBtn.transform));
            return;
        }
        Refresh();
        TooltipUI.Show(Loc.T("영혼 정련"), RefineTip());
    }

    string RefineTip()
    {
        bool complete = nodes != null && sp.TreeComplete(nodes);
        int cost = sp.RefineCost;
        string state = !complete ? "<color=#8a8494>" + Loc.T("트리를 모두 배우면 열립니다") + "</color>"
            : SoulShards.Amount >= cost ? "<color=#b8f5a0>" + Loc.T("눌러서 정련하기") + "  (" + cost + ")</color>"
            : "<color=#ff8a80>" + Loc.T("영혼 조각이 모자랍니다") + "  (" + SoulShards.Amount + " / " + cost + ")</color>";
        return Loc.T("영혼 트리를 모두 배운 뒤 남는 영혼 조각으로 작은 능력치를 끝없이 올립니다. 할 때마다 비용이 10씩 오릅니다.")
             + "\n<color=#F5D478>" + Loc.T("모든 무기 피해 +2% · 최대 체력 +4") + "</color>"
             + "\n" + Loc.T("정련 횟수") + " " + sp.Refines + "\n" + state;
    }

    string Tip(SpecialAbilities.SoulNode n)
    {
        string block = sp.BlockReason(n);
        string state = sp.OwnsNode(n.key) ? "<color=#F5D478>" + Loc.T("배움") + "</color>"
            : block != null ? "<color=#8a8494>" + block + "</color>"
            : SoulShards.Amount >= n.cost ? "<color=#b8f5a0>" + Loc.T("눌러서 배우기") + "  (" + n.cost + ")</color>"
            : "<color=#ff8a80>" + Loc.T("영혼 조각이 모자랍니다") + "  (" + SoulShards.Amount + " / " + n.cost + ")</color>";
        string rec = n.recommended && !sp.OwnsNode(n.key) ? "\n<color=#F5D478>" + Loc.T("추천: 지금 무기에 잘 맞는 칸") + "</color>" : "";
        return n.desc + rec + "\n" + state;
    }

    // ================================================================= 배치
    // 방사형: 가지마다 정해진 방향을 중심으로 부채꼴. 칸마다 필요한 폭(각도)을 아래부터 쌓아 올려
    // 같은 깊이의 칸끼리 Gap 보다 가까워지지 않게 함. 가지가 넓으면 그 가지만 바깥으로 밀어 냄 (트리는 끌어서 봄)
    Dictionary<string, Vector2> Layout(List<SpecialAbilities.SoulNode> list)
    {
        Dictionary<string, List<SpecialAbilities.SoulNode>> kids = new Dictionary<string, List<SpecialAbilities.SoulNode>>();
        HashSet<string> keys = new HashSet<string>();
        foreach (SpecialAbilities.SoulNode n in list) keys.Add(n.key);
        foreach (SpecialAbilities.SoulNode n in list)
        {
            string p = n.parent != null && keys.Contains(n.parent) ? n.parent : "#" + n.branch;
            if (!kids.TryGetValue(p, out List<SpecialAbilities.SoulNode> l)) kids[p] = l = new List<SpecialAbilities.SoulNode>();
            l.Add(n);
        }
        Dictionary<string, Vector2> result = new Dictionary<string, Vector2>();
        for (int b = 0; b < SpecialAbilities.BranchCount; b++)
        {
            string rootKey = "#" + b;
            if (!kids.ContainsKey(rootKey)) continue;
            // 1) 배율 1로 필요한 폭을 재고, 부채꼴을 넘으면 그만큼 바깥으로 (폭은 반지름에 반비례)
            Dictionary<string, float> width = new Dictionary<string, float>();
            float total = Width(rootKey, 0, 1f, kids, width);
            float scale = Mathf.Max(1f, total / (Sector * Mathf.Deg2Rad));
            if (scale > 1f) { width.Clear(); total = Width(rootKey, 0, scale, kids, width); }
            // 2) 폭만큼 각도를 나눠 줌
            float start = BranchAngle[b] * Mathf.Deg2Rad - total / 2f;
            Assign(rootKey, 0, start, scale, kids, width, result);
        }
        return result;
    }

    static float Radius(int depth, float scale) => (R0 + RStep * (depth - 1)) * scale;

    // 이 칸(과 아래 칸들)이 필요로 하는 각도: 자식들 폭의 합과 자기 칸 폭 중 큰 것
    static float Width(string key, int depth, float scale, Dictionary<string, List<SpecialAbilities.SoulNode>> kids, Dictionary<string, float> width)
    {
        float own = depth > 0 ? Gap / Radius(depth, scale) : 0f;
        float sum = 0f;
        if (kids.TryGetValue(key, out List<SpecialAbilities.SoulNode> l))
            foreach (SpecialAbilities.SoulNode n in l) sum += Width(n.key, depth + 1, scale, kids, width);
        float w = Mathf.Max(own, sum);
        width[key] = w;
        return w;
    }

    static void Assign(string key, int depth, float start, float scale, Dictionary<string, List<SpecialAbilities.SoulNode>> kids,
                       Dictionary<string, float> width, Dictionary<string, Vector2> result)
    {
        if (!kids.TryGetValue(key, out List<SpecialAbilities.SoulNode> l)) return;
        float sum = 0f;
        foreach (SpecialAbilities.SoulNode n in l) sum += width[n.key];
        // 자식들이 부모 폭보다 좁으면 가운데로 모음
        float a = start + (width[key] - sum) / 2f;
        foreach (SpecialAbilities.SoulNode n in l)
        {
            float w = width[n.key];
            float mid = a + w / 2f;
            result[n.key] = new Vector2(Mathf.Cos(mid), Mathf.Sin(mid)) * Radius(depth + 1, scale);
            Assign(n.key, depth + 1, a, scale, kids, width, result);
            a += w;
        }
    }

    Image Line(Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        Image l = Img("Line", board, (a + b) / 2f, new Vector2(d.magnitude, 6f), null, Dim);
        l.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        l.rectTransform.SetAsFirstSibling();
        return l;
    }

    // ================================================================= 연출
    System.Collections.IEnumerator Burst(Vector2 at, Color c)
    {
        Image s = Img("Spark", board, at, Vector2.one * Random.Range(14f, 30f), sp.glowSprite, c);
        Vector2 v = Random.insideUnitCircle.normalized * Random.Range(150f, 420f);
        for (float t = 0f; t < 0.5f && s != null; t += Time.unscaledDeltaTime)
        {
            s.rectTransform.anchoredPosition = at + v * t;
            s.color = new Color(c.r, c.g, c.b, 1f - t / 0.5f);
            yield return null;
        }
        if (s != null) Destroy(s.gameObject);
    }

    System.Collections.IEnumerator Shake(RectTransform r)
    {
        Vector2 home = r.anchoredPosition;
        for (float t = 0f; t < 0.25f && r != null; t += Time.unscaledDeltaTime)
        {
            r.anchoredPosition = home + new Vector2(Mathf.Sin(t * 80f) * 8f * (1f - t / 0.25f), 0f);
            yield return null;
        }
        if (r != null) r.anchoredPosition = home;
    }

    // 따로 그리기 순서를 가진 캔버스: HUD의 다른 캔버스보다 위에, 클릭도 받음
    public static void OnTop(GameObject go, int order)
    {
        Canvas c = go.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = order;
        go.AddComponent<GraphicRaycaster>();
    }

    static void AddTrigger(EventTrigger et, EventTriggerType type, System.Action a)
    {
        EventTrigger.Entry e = new EventTrigger.Entry { eventID = type };
        e.callback.AddListener(_ => a());
        et.triggers.Add(e);
    }

    static Image Img(string name, Transform parent, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        RectTransform r = UIKit.Rect(name, parent, pos, size);
        Image i = r.gameObject.AddComponent<Image>();
        i.sprite = sprite;
        i.color = color;
        i.raycastTarget = false;
        return i;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
