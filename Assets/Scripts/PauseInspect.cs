using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1.1.9~ ESC 일시정지 중 「멈춰서 읽기」 (편의성 2차 15 · 3차 1)
// 일시정지 패널(전체 화면 Image · raycastTarget)이 그 아래 HUD 로 가는 포인터를 먹어 HUD 칸 툴팁이 하나도 안 떴음
// → 패널의 레이캐스트는 그대로 두고(클릭은 계속 막음) 포인터 아래를 「읽기만」 해서 툴팁을 띄움
//   HUD 칸(TooltipTrigger)이 먼저, UI 가 없으면 세상(적 · 보스 몸, WorldProvider)
// 툴팁(TooltipUI.Show/Hide)은 멈춘 동안 이 클래스만 부름 (두 코드가 따로 부르면 서로 지움)
public class PauseInspect : MonoBehaviour
{
    // 세상 위의 것 (적 · 보스) — 기본은 EnemyPeek
    public static System.Func<Vector2, (string title, string body)?> WorldProvider = EnemyPeek;

    static PauseInspect instance;
    ESCmenu esc;
    readonly List<RaycastResult> hits = new List<RaycastResult>();
    PointerEventData ped;
    EventSystem pedEs;
    bool shown;
    string shownTitle, shownBody;
    TMP_Text hintLine;

    const string HintKey = "hint.pauseinspect";
    const int HintTimes = 5;

    public static void Install(ESCmenu esc)
    {
        if (instance != null || esc == null) return;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        instance = canvas.gameObject.AddComponent<PauseInspect>();
        instance.esc = esc;
        InstallHudTips(canvas);
    }

    void OnDestroy() { if (instance == this) instance = null; }

    // ESC 메뉴가 열릴 때 (ESCmenu.ToggleEsc): 처음 다섯 번은 재개 안내 아래 한 줄
    public static void OnPauseOpened()
    {
        if (instance == null || instance.esc == null || instance.esc.escMenu == null) return;
        int n = Prefs.GetInt(HintKey, 0);
        if (n >= HintTimes || GameInput.Auto) { if (instance.hintLine != null) instance.hintLine.gameObject.SetActive(false); return; }
        Prefs.SetInt(HintKey, n + 1);
        instance.EnsureHint();
    }

    void EnsureHint()
    {
        Transform hint = esc.escMenu.transform.Find("ResumeHint");
        if (hintLine == null)
        {
            RectTransform at = hint as RectTransform;
            Vector2 pos = at != null ? at.anchoredPosition - new Vector2(0f, 34f) : new Vector2(0f, -300f);
            hintLine = UIKit.Text(esc.escMenu.transform, "", 18f, new Color(0.66f, 0.61f, 0.53f), pos, new Vector2(700f, 30f));
            hintLine.gameObject.AddComponent<LocRefresh>().refresh = WriteHint;
        }
        hintLine.gameObject.SetActive(true);
        WriteHint();
    }

    bool hintPad;

    void WriteHint()
    {
        if (hintLine == null) return;
        hintPad = GamePad.Active;
        hintLine.text = Loc.T(hintPad ? "멈춘 동안 왼쪽 스틱으로 커서를 아이콘이나 적 위에 두면 설명이 보입니다."
                                      : "멈춘 동안 화면의 아이콘이나 적 위에 마우스를 올리면 설명이 보입니다.");
    }

    void Update()
    {
        if (hintLine != null && hintLine.gameObject.activeInHierarchy && hintPad != GamePad.Active) WriteHint();     // 장치가 바뀌면 다시 씀
        bool on = ESCmenu.IsOpen && !ESCmenu.ConfirmOpen && !SettingsUI.IsOpen && !CodexUI.IsOpen && !SkillEvoCodexUI.IsOpen && !MemoryShards.ReaderOpen && !BuildView.IsOpen && Time.timeScale <= 0f;
        if (!on) { HideMine(); return; }
        EventSystem es = EventSystem.current;
        if (es == null) { HideMine(); return; }
        Vector2 at = GameInput.PointerPosition;      // 패드 가상 커서도 같은 자리 (TooltipUI.Follow 와 같은 기준)
        // 포인터가 그대로면 0.25초마다만 다시 봄 (멈춘 동안 프레임마다 레이캐스트 · 툴팁 글 만들기를 하지 않게)
        if (at == lastAt && Time.unscaledTime < nextLook) return;
        lastAt = at;
        nextLook = Time.unscaledTime + 0.25f;
        if (ped == null || pedEs != es) { ped = new PointerEventData(es); pedEs = es; }
        ped.position = at;
        hits.Clear();
        es.RaycastAll(ped, hits);                    // 깊이 순 — ESC 패널이 맨 앞이지만 건너뜀
        Transform escRoot = esc != null && esc.escMenu != null ? esc.escMenu.transform : null;
        foreach (RaycastResult h in hits)
        {
            if (h.gameObject == null) continue;
            if (escRoot != null && h.gameObject.transform.IsChildOf(escRoot)) continue;      // ESC 패널 · 그 버튼 · 안내 글
            TooltipTrigger t = h.gameObject.GetComponentInParent<TooltipTrigger>();
            if (t != null && t.isActiveAndEnabled)
            {
                ShowMine(t.titleProvider != null ? t.titleProvider() : t.title, t.bodyProvider != null ? t.bodyProvider() : t.body);
                return;
            }
            HideMine();                               // 툴팁 없는 UI(요약 · 각인 패널 바탕, 확인 창)가 가리면 그 아래는 안 봄
            return;
        }
        var w = WorldProvider?.Invoke(at);           // UI 가 없으면 세상(적)
        if (w.HasValue) { ShowMine(w.Value.title, w.Value.body); return; }
        HideMine();
    }

    Vector2 lastAt = new Vector2(float.NaN, float.NaN);
    float nextLook;

    void ShowMine(string title, string body)
    {
        if (shown && title == shownTitle && body == shownBody) return;
        TooltipUI.Ensure(transform);
        TooltipUI.Show(title, body);
        shown = true;
        shownTitle = title;
        shownBody = body;
    }

    void HideMine()
    {
        if (!shown) return;
        shown = false;
        shownTitle = shownBody = null;
        TooltipUI.Hide();
    }

    void OnDisable() => HideMine();

    // ================================================================= 멈춘 동안 적 위에 마우스 → 적 설명 (3차 1)
    static readonly Collider2D[] found = new Collider2D[16];

    static (string title, string body)? EnemyPeek(Vector2 screen)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        int n = Physics2D.OverlapPointNonAlloc(world, found);
        bosss bestBoss = null;
        EnermyController best = null;
        float bestD = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            Collider2D c = found[i];
            if (c == null) continue;
            bosss b = c.GetComponentInParent<bosss>();
            if (b != null && !b.IsDead) { bestBoss = b; break; }
            EnermyController e = EnermyController.FromCollider(c);
            if (e == null || e.IsDead) continue;
            float d = ((Vector2)e.transform.position - (Vector2)world).sqrMagnitude;
            if (d < bestD) { bestD = d; best = e; }
        }
        if (bestBoss != null)
        {
            int kind = bestBoss.developer ? (bestBoss.developerMom ? bossbar.DeveloperMomKind : bossbar.DeveloperKind) : bestBoss.bossKind;
            string desc = bestBoss.developer ? "" : CodexUI.BossDescOf(bestBoss.bossKind);
            return (bossbar.BossName(kind), (desc.Length > 0 ? desc + "\n\n" : "") + HpLine(bestBoss.EnemyHealth, bestBoss.setEnemyHP));
        }
        if (best == null) return null;
        string prefab = best.name.Replace("(Clone)", "").Trim();
        string title;
        if (best.GetComponent<GoldenGoblin>() != null) title = Loc.T("event.goblin.name");
        else
        {
            title = CodexUI.EnemyName(prefab);
            if (title.Length == 0) title = prefab;
            if (best.IsMidBoss) title = Loc.T("중간 보스 이름표") + " · " + title;
        }
        string body = CodexUI.EnemyDesc(prefab);
        string skill = best.skill != EnemySkillType.None ? Loc.T(EnemySkill.SkillName(best.skill)) : "";
        if (skill.Length > 0) body += (body.Length > 0 ? "\n" : "") + "<color=#FF9A7A>" + skill + "</color>";
        body += (body.Length > 0 ? "\n\n" : "") + HpLine(best.EnemyHealth, best.setEnemyHP);
        return (title, body);
    }

    static string HpLine(float now, float max) =>
        "<color=#F5D478>" + Loc.T("체력") + " " + Mathf.CeilToInt(Mathf.Max(0f, now)).ToString("N0") + " / " + Mathf.RoundToInt(max).ToString("N0") + "</color>";

    // ================================================================= 툴팁이 없던 HUD 에 설명 (2차 15)
    static void InstallHudTips(Canvas canvas)
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        PlayerHP hp = Object.FindFirstObjectByType<PlayerHP>(FindObjectsInactive.Include);
        if (hp != null)
            Tip(hp.gameObject, () => Loc.T("체력"), () => Loc.T("지금 체력과 최대 체력입니다. 0이 되면 쓰러집니다.")
                + (player != null ? Now(Mathf.CeilToInt(player.PlayerHealth) + " / " + Mathf.RoundToInt(player.PlayerMaxHealth)) : ""));
        Level lv = Object.FindFirstObjectByType<Level>(FindObjectsInactive.Include);
        if (lv != null)
        {
            System.Func<string> title = () => Loc.T("경험치");
            System.Func<string> body = () => Loc.T("다음 레벨까지의 경험치입니다. 가득 차면 레벨업 카드를 고를 수 있습니다.")
                + (player != null ? Now(Loc.T("레벨 ") + player.level + "   " + Mathf.FloorToInt(player.nowEXP) + " / " + Mathf.CeilToInt(player.needEXP)) : "");
            Tip(lv.gameObject, title, body);
            Transform plate = Find(canvas.transform, "LevelPlate");
            if (plate != null) Tip(plate.gameObject, title, body);
        }
        SkillGauge g = Object.FindFirstObjectByType<SkillGauge>(FindObjectsInactive.Include);
        if (g != null)
        {
            System.Func<string> title = () => Loc.T("필살기 게이지");
            System.Func<string> body = () => Loc.T("적을 쓰러뜨리면 차고, 가득 차면 [{ULT}]로 필살기를 씁니다.")
                + Now(Mathf.RoundToInt(g.SkillPoint / Mathf.Max(1f, g.MaxSkillPoint) * 100f) + "%");
            Tip(g.gameObject, title, body);
            Transform frame = Find(canvas.transform, "SkillFrame");
            if (frame != null) Tip(frame.gameObject, title, body);
        }
        Transform ammo = canvas.transform.Find("AmmoPanel");
        if (ammo != null)
            Tip(ammo.gameObject, () => Loc.T("탄창"), () => Loc.T("남은 탄과 탄창 크기입니다. [{RELOAD}]로 재장전하고, 다 쓰면 저절로 재장전합니다."));
        Coin coin = Object.FindFirstObjectByType<Coin>(FindObjectsInactive.Include);
        Transform coinPanel = Find(canvas.transform, "CoinPanel");
        GameObject coinGo = coinPanel != null ? coinPanel.gameObject : coin != null ? coin.gameObject : null;
        if (coinGo != null)
            Tip(coinGo, () => Loc.T("코인"), () => Loc.T("이번 판에 모은 코인입니다. 떠돌이 상점에서 쓰고, 판이 끝나면 사라집니다.")
                + (coin != null ? Now(coin.coins.ToString("N0")) : ""));
        Point point = Object.FindFirstObjectByType<Point>(FindObjectsInactive.Include);
        if (point != null)
            Tip(point.gameObject, () => Loc.T("포인트"),
                () => Loc.T("적을 처치하면 쌓이고 판이 끝나도 남습니다. 메인 메뉴에서 새 영웅과 스킨을 사는 데 씁니다. (판 안에서 떠돌이 상점에 쓰는 코인과는 다릅니다)"));
        Transform clock = Find(canvas.transform, "RunClockBox");
        if (clock != null)
            Tip(clock.gameObject, () => Loc.T("플레이 시간 ").Trim(), () => Loc.T("이번 판의 플레이 시간입니다. 멈춘 시간은 빼고 셉니다."));
    }

    // 영혼 트리 버튼 (StageManager 가 늦게 만들므로 따로 부름) — 거너가 아니면 영혼 조각 수도 이 버튼에 보임
    public static void TipTreeButton(GameObject button)
    {
        if (button == null) return;
        Transform face = button.transform.Find("Face");
        Tip(face != null ? face.gameObject : button, () => Loc.T("영혼 트리"),
            () => Loc.T("[{UPGRADE}]로 영혼 트리를 엽니다. 배울 수 있는 칸이 있으면 버튼이 빛납니다.")
                + "\n\n" + Loc.T("영혼 트리에서 칸을 배우는 데 씁니다. 적을 쓰러뜨리면 떨어집니다.")
                + Now(Loc.T("영혼 조각") + " " + SoulShards.Amount.ToString("N0")));
    }

    static string Now(string v) => "\n\n<color=#F5D478>" + v + "</color>";

    static Transform Find(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static void Tip(GameObject go, System.Func<string> title, System.Func<string> body)
    {
        if (go == null || go.GetComponent<TooltipTrigger>() != null) return;
        Graphic g = go.GetComponent<Graphic>();
        if (g == null)
        {
            Image img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            g = img;
        }
        g.raycastTarget = true;
        TooltipTrigger t = go.AddComponent<TooltipTrigger>();
        t.titleProvider = title;
        t.bodyProvider = body;
        t.hud = true;            // 전투 중 조준을 막지 않음 (PlayerController.PointerOverUI) · 전투 중엔 잠깐 머물러야
    }
}
