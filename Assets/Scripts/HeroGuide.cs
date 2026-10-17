using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.1.9~ 영웅 첫 판 안내 카드 (편의성 2차 3): 새 영웅을 처음 고른 판에서 싸우기 전에 한 번 — 평타 · 필살기 · 머리 위 막대
// 시작 이야기(StoryDirector.Intro) 바로 뒤 · 무한 모드 특수 능력 · 영혼 각인 고르기 전 · 시간 멈춤
// 일시정지 요약 패널 「영웅 안내」로 언제든 다시 (그때는 시간 · 기록을 건드리지 않음)
public static class HeroGuide
{
    static GameObject open;
    public static bool IsOpen => open != null;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);

    static string Key(CharacterId id) => "hero.guide." + (int)id;

    // 머리 위 막대 한 줄 (막대가 없는 영웅은 null)
    static string BarLine(CharacterId id) => id switch
    {
        CharacterId.Archer => "숨 — 활을 당기는 동안 줄고, 바닥나면 활이 떨려 위력이 절반이 됩니다. 「완벽!」 발사가 숨을 되돌립니다.",
        CharacterId.Alchemist => "불안정 — 던질수록 올라 폭발이 커지고, 가득 차면 발밑에서 자폭합니다.",
        CharacterId.Cartographer => "다음 획 — 세 번째 획마다 삼각형이 닫혀 안쪽 적이 크게 다칩니다.",
        CharacterId.Clockmaker => "눈금 — 맞힐 때마다 차고, 다 차면 다음 톱니가 모든 적을 꿰뚫는 「정각」 톱니가 됩니다.",
        CharacterId.Postman => "배달 — 직접 보낸 편지를 열 번 배달하면 코인과 필살기 게이지를 받습니다. 표식 — 속달을 누르고 있는 동안 붙는 편지 수입니다.",
        CharacterId.Ghost => "영혼 — 쓰러뜨린 적에서 생겨 몸 둘레를 돌며, 맞을 때 체력 대신 하나씩 사라집니다.",
        _ => null,
    };

    // 캐릭터 상세 창도 같은 줄을 씀 (CharacterUI)
    public static string BarText(CharacterId id) { string s = BarLine(id); return s != null ? Loc.T(s) : null; }

    // 설정 「도움말 다시 보기」가 함께 지움 (Hints.ResetAll)
    public static void ResetAll()
    {
        for (int i = 0; i < CharacterData.All.Length; i++) Prefs.DeleteKey("hero.guide." + i);
    }

    // StageManager.Opening 이 부름: 이 영웅으로 처음이면 한 번 (이어하기 · 튜토리얼은 Opening 이 먼저 끝나 닿지 않음)
    public static IEnumerator Show(MonoBehaviour host)
    {
        CharacterId id = CharacterData.Selected;
        if (RunSave.Pending != null || GameInput.Auto || Application.isBatchMode || GameInput.TrailerRunning || TutorialRun.Active) yield break;
        if (Prefs.GetInt(Key(id), 0) != 0) yield break;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) yield break;
        // 시작 카드 창이 떠 있으면 보여 주는 동안 숨김 · 시간은 이전 값으로 되돌림 (첫 카드 창이 열려 있으면 0 이 맞음)
        LevelShop ls = Object.FindFirstObjectByType<LevelShop>();
        bool hid = ls != null && ls.LvshopPanel != null && ls.LvshopPanel.activeSelf;
        if (hid) ls.LvshopPanel.SetActive(false);
        float before = Time.timeScale;
        Time.timeScale = 0f;
        bool closed = false;
        Build(canvas.transform, id, true, () => closed = true);
        while (!closed && open != null) yield return null;
        Prefs.SetInt(Key(id), 1);
        Prefs.Save();
        if (hid && ls != null && ls.LvshopPanel != null) ls.LvshopPanel.SetActive(true);
        Time.timeScale = before;
    }

    // 일시정지 요약 패널 「영웅 안내」: 시간 · 기록은 건드리지 않음
    public static void OpenView(Transform root)
    {
        if (open != null || root == null) return;
        Build(root, CharacterData.Selected, false, null);
    }

    public static void CloseView()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }

    static void Build(Transform root, CharacterId id, bool first, System.Action onClose)
    {
        CharacterDef d = CharacterData.Def(id);
        RectTransform win = UIKit.Modal(root, "HeroGuide", new Vector2(1180f, 680f), out open);
        SoulTreeUI.OnTop(open, 620);                     // 레벨업 창 · 알림판 · 일시정지 패널보다 위
        GameObject mine = open;
        System.Action close = () =>
        {
            if (mine != null) Object.Destroy(mine);
            if (open == mine) open = null;
            onClose?.Invoke();
        };

        // 왼쪽: 초상 · 이름 · 칭호
        RectTransform pr = UIKit.Rect("Portrait", win, new Vector2(-380f, 60f), new Vector2(300f, 300f));
        Image img = pr.gameObject.AddComponent<Image>();
        img.sprite = CharacterUI.Portrait(id);
        img.preserveAspect = true;
        img.raycastTarget = false;
        if (d.body == null)
        {
            RectTransform gr = UIKit.Rect("Gun", pr, new Vector2(72f, -27f), new Vector2(84f, 42f));
            Image gi = gr.gameObject.AddComponent<Image>();
            gi.sprite = Resources.Load<Sprite>("Weapons/weapon_pistol");
            gi.preserveAspect = true;
            gi.raycastTarget = false;
        }
        UIKit.Text(win, d.name, 40f, Gold, new Vector2(-380f, -120f), new Vector2(380f, 54f));
        UIKit.Text(win, d.title, 22f, Dim, new Vector2(-380f, -164f), new Vector2(380f, 32f));

        // 오른쪽: 기본 무기 · 필살기 · (머리 위 막대)
        string bar = BarLine(id);
        float bodyH2 = bar != null ? 130f : 200f;
        Block(win, "기본 무기", Loc.T(d.weapon) + " — " + Loc.T(d.attack), 230f, 130f);
        Block(win, "{ULT}", Loc.T(d.skill) + " — " + Loc.T(d.skillDesc), 80f, bodyH2);
        if (bar != null) Block(win, "머리 위 막대", Loc.T(bar), -70f, 90f);

        Button ok = UIKit.MakeButton(win, first ? "시작!" : "닫기", new Vector2(0f, -250f), new Vector2(300f, 72f), () => close(), 30f);
        ok.GetComponent<Image>().color = new Color(1f, 0.88f, 0.55f);
        if (first) UIKit.Text(win, "일시정지 메뉴의 「영웅 안내」에서 언제든 다시 볼 수 있습니다.", 18f, Dim, new Vector2(0f, -306f), new Vector2(1100f, 28f));
        UIKit.SetDefault(ok);
        UIKit.EscCancel(open, () => close());           // ESC · 패드 B
        WindowFit.Attach(win, new Vector2(1200f, 700f));
        if (first) Hostile.Play("chime", 0.5f);
    }

    // 제목 26pt Gold + 본문 22pt Parch (가운데 y, 칸 높이)
    static void Block(RectTransform win, string titleKo, string body, float y, float h)
    {
        const float x = 120f, w = 760f;
        float top = y + h * 0.5f;
        TMP_Text t = UIKit.Text(win, titleKo, 26f, Gold, new Vector2(x, top - 18f), new Vector2(w, 36f), TextAlignmentOptions.Left);
        t.fontSizeMin = 16f;
        TMP_Text b = UIKit.Text(win, "", 22f, Parch, new Vector2(x, top - 40f - (h - 40f) * 0.5f), new Vector2(w, h - 40f), TextAlignmentOptions.TopLeft);
        b.fontSizeMin = 16f;
        b.enableWordWrapping = true;
        b.text = body;
    }
}
