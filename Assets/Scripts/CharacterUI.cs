using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 메뉴 "캐릭터": 10칸 도감. 잠긴 캐릭터는 검은 실루엣과 ???, 누르면 스탯 · <선택> <취소> (잠겼으면 <구매>)
public static class CharacterUI
{
    static GameObject open;
    static Transform rootRef;
    static RectTransform win;
    static GameObject detail;
    static TMP_Text pointsText;
    public static bool IsOpen => open != null;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.6f, 0.58f, 0.65f);

    public static void Open(Transform root)
    {
        if (open != null) return;
        rootRef = root;
        win = UIKit.Modal(root, "CharacterPanel", new Vector2(1500f, 880f), out open);
        open.AddComponent<CharacterUIInput>();
        UIKit.Text(win, "캐릭터", 52f, Gold, new Vector2(0f, 380f), new Vector2(600f, 70f));
        pointsText = UIKit.Text(win, "", 28f, Parch, new Vector2(520f, 380f), new Vector2(420f, 50f), TextAlignmentOptions.Right);
        UIKit.MakeButton(win, "닫기", new Vector2(0f, -385f), new Vector2(240f, 70f), Close, 28f);
        BuildGrid();
    }

    public static void Close()
    {
        CharacterData.Save();
        if (open != null) Object.Destroy(open);
        open = null;
        detail = null;
    }

    static void RefreshPoints() => pointsText.text = Loc.T("보유 포인트: ") + CharacterData.Points.ToString("N0") + " P";

    static readonly List<GameObject> cards = new List<GameObject>();

    static void BuildGrid()
    {
        foreach (GameObject c in cards) if (c != null) Object.Destroy(c);
        cards.Clear();
        RefreshPoints();
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterId id = (CharacterId)i;
            Vector2 pos = new Vector2(-560f + 280f * (i % 5), i < 5 ? 150f : -200f);
            cards.Add(Card(id, pos));
        }
    }

    public static Sprite Portrait(CharacterId id)
    {
        CharacterDef d = CharacterData.Def(id);
        string sheet = d.body ?? "gunner_nogun";
        SkinDef skin = SkinData.Equipped(SkinKind.Character, (int)id);
        if (skin != null && Resources.LoadAll<Sprite>("Characters/" + skin.id).Length > 0) sheet = skin.id;
        Sprite best = null;
        foreach (Sprite s in Resources.LoadAll<Sprite>("Characters/" + sheet))
            if (best == null || string.CompareOrdinal(s.name, best.name) < 0) best = s;
        // 거너는 idle 첫 프레임 (시트의 맨 위 왼쪽)
        if (d.body == null)
            foreach (Sprite s in Resources.LoadAll<Sprite>("Characters/" + sheet))
                if (s.name == "players blue x1_0") best = s;
        return best;
    }

    static GameObject Card(CharacterId id, Vector2 pos)
    {
        CharacterDef d = CharacterData.Def(id);
        bool unlocked = CharacterData.IsUnlocked(id), developed = CharacterData.IsDeveloped(id);
        bool selected = unlocked && CharacterData.Selected == id;

        Button b = UIKit.MakeButton(win, "", pos, new Vector2(250f, 320f), () => ShowDetail(id), 20f);
        b.GetComponent<Image>().color = selected ? Gold : unlocked ? new Color(0.72f, 0.7f, 0.78f) : new Color(0.35f, 0.33f, 0.4f);
        Transform t = b.transform;

        // 그림 (잠겼으면 검은 실루엣)
        RectTransform pr = UIKit.Rect("Portrait", t, new Vector2(0f, 38f), new Vector2(200f, 200f));
        Image img = pr.gameObject.AddComponent<Image>();
        img.sprite = Portrait(id);
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = unlocked ? Color.white : new Color(0.02f, 0.02f, 0.03f, 1f);
        if (d.body == null && unlocked)
        {
            // 거너는 권총을 따로 그려 줌 (캐릭터 그림에는 총이 없음)
            RectTransform gr = UIKit.Rect("Gun", pr, new Vector2(48f, -18f), new Vector2(56f, 28f));
            Image gi = gr.gameObject.AddComponent<Image>();
            gi.sprite = Resources.Load<Sprite>("Weapons/weapon_pistol");
            gi.preserveAspect = true;
            gi.raycastTarget = false;
        }

        string name = unlocked || developed ? d.name : "???";       // 1.0.4~ 잠긴 캐릭터도 이름은 보임 (그림만 실루엣)
        TMP_Text nameText = UIKit.Text(t, name, 30f, unlocked ? Gold : Dim, new Vector2(0f, -100f), new Vector2(230f, 40f));
        string sub = selected ? "선택됨" : unlocked ? d.title : developed ? "잠김" : "준비 중";
        TMP_Text subText = UIKit.Text(t, sub, 20f, selected ? new Color(0.5f, 1f, 0.6f) : Dim, new Vector2(0f, -134f), new Vector2(230f, 30f));
        // 영웅 숙련도 (1.0.1): 마지막 보스를 쓰러뜨릴 때마다 +1 (단계마다 시작 레벨업 카드 +1) · 칸 위쪽 안
        // 1.0.4~: 0단계도 늘 보임 (예전엔 숨어 있어 숙련도가 있는 줄 몰랐음)
        if (unlocked)
        {
            TMP_Text star = UIKit.Text(t, "", 20f, Mastery.Level(id) > 0 ? new Color(1f, 0.85f, 0.35f) : Dim, new Vector2(0f, 142f), new Vector2(230f, 26f));
            star.text = Mastery.Label(id);
        }
        if (!unlocked && developed)
        {
            subText.text = Loc.T("잠김") + " · " + d.price.ToString("N0") + " P";
            subText.color = CharacterData.Points >= d.price && CharacterData.ConditionMet(id) ? new Color(1f, 0.85f, 0.4f) : Dim;
        }
        // 해제 조건 (1.0.2): 칸 아래쪽에 조건 · 달성하면 초록 체크
        if (!unlocked && d.condition != CharUnlock.None)
        {
            bool met = CharacterData.ConditionMet(id);
            TMP_Text cond = UIKit.Text(t, "", 15f, met ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.6f, 0.45f), new Vector2(0f, 144f), new Vector2(236f, 30f));
            cond.text = (met ? "★ " : "") + CharacterData.ConditionText(d.condition);     // 1.1~ ✔ 는 폰트에 없어 네모로 나왔음 → ★
            cond.enableWordWrapping = true;
            cond.enableAutoSizing = true;
            cond.fontSizeMin = 10f;
            cond.fontSizeMax = 15f;
        }
        return b.gameObject;
    }

    // ================================================================= 자세히 (스탯 · 선택 · 구매)
    static void ShowDetail(CharacterId id)
    {
        if (detail != null) Object.Destroy(detail);
        CharacterDef d = CharacterData.Def(id);
        bool unlocked = CharacterData.IsUnlocked(id), developed = CharacterData.IsDeveloped(id);

        // 창 위에 덮는 판
        detail = new GameObject("Detail", typeof(RectTransform), typeof(Image));
        RectTransform dr = detail.GetComponent<RectTransform>();
        dr.SetParent(win, false);
        dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one;
        dr.offsetMin = dr.offsetMax = Vector2.zero;
        detail.GetComponent<Image>().color = new Color(0.06f, 0.05f, 0.09f, 0.98f);

        RectTransform pr = UIKit.Rect("Portrait", dr, new Vector2(-470f, 60f), new Vector2(360f, 360f));
        Image img = pr.gameObject.AddComponent<Image>();
        img.sprite = Portrait(id);
        img.preserveAspect = true;
        img.color = unlocked ? Color.white : new Color(0.02f, 0.02f, 0.03f, 1f);
        if (d.body == null && unlocked)
        {
            RectTransform gr = UIKit.Rect("Gun", pr, new Vector2(88f, -32f), new Vector2(100f, 50f));
            Image gi = gr.gameObject.AddComponent<Image>();
            gi.sprite = Resources.Load<Sprite>("Weapons/weapon_pistol");
            gi.preserveAspect = true;
        }

        // 1.0.4~: 잠긴 캐릭터도 이름 · 한 줄 소개 · 스탯 · 무기 · 스킬을 보여 줌 (예전엔 모두 ??? 라 최대 15,000P를 모르고 써야 했음, 그림만 실루엣)
        bool known = unlocked || developed;
        UIKit.Text(dr, known ? d.name : "???", 50f, Gold, new Vector2(-470f, 300f), new Vector2(420f, 64f));
        TMP_Text titleText = UIKit.Text(dr, "", 24f, Dim, new Vector2(-470f, 255f), new Vector2(420f, 36f));
        titleText.text = unlocked ? Loc.T(d.title) : developed ? Loc.T(d.title) + " · " + Loc.T("잠김") : Loc.T("준비 중");
        // 영웅 숙련도: 단계 · 올리는 법 · 효과 (1.0.4~ 0단계도)
        if (unlocked)
        {
            TMP_Text mastery = UIKit.Text(dr, "", 18f, Mastery.Level(id) > 0 ? new Color(1f, 0.85f, 0.35f) : Dim, new Vector2(-470f, -160f), new Vector2(460f, 60f));
            mastery.text = Mastery.Label(id) + "\n" + Loc.T("마지막 보스를 쓰러뜨릴 때마다 +1 (무한 모드 제외) · 단계마다 시작 레벨업 카드 +1");
            mastery.enableWordWrapping = true;
        }

        // 해제 조건 (1.0.2)
        if (!unlocked && d.condition != CharUnlock.None)
        {
            bool met = CharacterData.ConditionMet(id);
            TMP_Text cond = UIKit.Text(dr, "", 24f, met ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.6f, 0.45f), new Vector2(0f, -220f), new Vector2(1100f, 34f));
            cond.text = Loc.T("해제 조건") + " : " + CharacterData.ConditionText(d.condition) + "  " + (met ? Loc.T("(달성)") : Loc.T("(미달성)"));
        }

        if (!developed)
        {
            UIKit.Text(dr, "아직 준비 중인 캐릭터입니다.", 30f, Parch, new Vector2(200f, 60f), new Vector2(800f, 60f));
            UIKit.MakeButton(dr, "취소", new Vector2(0f, -330f), new Vector2(240f, 70f), CloseDetail, 28f);
            return;
        }

        // 오른쪽: 설명 · 스탯 (1.0.4~ 잠겨도 보임)
        float x = 190f;
        UIKit.Text(dr, d.description, 24f, Parch, new Vector2(x, 290f), new Vector2(820f, 70f), TextAlignmentOptions.Left);
        float y = 200f;
        Stat(dr, "체력", d.hp, known, ref y);
        Stat(dr, "공격력", d.damage, known, ref y);
        Stat(dr, "공격 속도", d.attackSpeed, known, ref y);
        StatText(dr, "사거리", d.range > 0f ? Loc.T("약 ") + d.range.ToString("0") + Loc.T("칸") : Loc.T("무한"), known, ref y);
        // 필살기 충전은 1 / 게이지 (높을수록 빨리 참, 거너보다 낮으면 빨강) — 1.0.4~ 안내가 「높을수록 늦게 참」으로 반대였음
        UIKit.Text(dr, "높을수록 빨리 참", 15f, new Color(0.7f, 0.65f, 0.6f), new Vector2(300f, y - 20f), new Vector2(420f, 18f), TextAlignmentOptions.Center);
        Stat(dr, "필살기 충전", 1f / Mathf.Max(0.1f, d.gauge), known, ref y);
        Stat(dr, "이동 속도", d.move, known, ref y);
        StatText(dr, "탄창", d.mag > 0 ? d.mag + Loc.T("발") : Loc.T("무한"), known, ref y);
        y -= 10f;
        Info(dr, "기본 무기", Loc.T(d.weapon) + " — " + Loc.T(d.attack), ref y);
        Info(dr, "{ULT}", Loc.T(d.skill) + " — " + Loc.T(d.skillDesc), ref y);      // 1.0.4~ 바꾼 필살기 키 이름 (기본 우클릭) · 잠긴 영웅도 미리 보기

        if (unlocked)
        {
            bool selected = CharacterData.Selected == id;
            Button sel = UIKit.MakeButton(dr, selected ? "선택됨" : "선택", new Vector2(-140f, -330f), new Vector2(240f, 70f), () =>
            {
                CharacterData.Selected = id;
                Hostile.Play("chime", 0.6f, 1.2f);
                CloseDetail();
                BuildGrid();
            }, 28f);
            sel.interactable = !selected;
            UIKit.MakeButton(dr, "취소", new Vector2(140f, -330f), new Vector2(240f, 70f), CloseDetail, 28f);
        }
        else if (Demo.On)
        {
            // 체험판: 다른 영웅은 정식판에서
            UIKit.Text(dr, "정식판에서 만날 수 있습니다", 26f, Dim, new Vector2(0f, -270f), new Vector2(900f, 36f));
            UIKit.MakeButton(dr, "상점 페이지 보기", new Vector2(-140f, -330f), new Vector2(300f, 70f), Demo.OpenStore, 26f);     // 1.0.4~: 위시리스트 → 상점 페이지 (이미 파는 게임)
            UIKit.MakeButton(dr, "취소", new Vector2(190f, -330f), new Vector2(240f, 70f), CloseDetail, 28f);
        }
        else
        {
            bool can = CharacterData.Points >= d.price && CharacterData.ConditionMet(id);
            bool armed = false;
            TMP_Text bt = null;
            Button buy = UIKit.MakeButton(dr, "", new Vector2(-140f, -330f), new Vector2(300f, 70f), () =>
            {
                // 1.0.4~: 스킨처럼 두 번 눌러야 구매 (예전엔 캐릭터만 한 번에 결제됨)
                if (!armed) { armed = true; if (bt != null) bt.text = Loc.T("한 번 더 누르면 구매"); return; }
                if (!CharacterData.Buy(id)) return;
                Hostile.Play("levelup", 0.8f);
                BuildGrid();
                ShowDetail(id);
            }, 26f);
            bt = buy.GetComponentInChildren<TMP_Text>();
            bt.text = Loc.T("구매") + "  " + d.price.ToString("N0") + " P";
            buy.interactable = can;
            if (!can && CharacterData.ConditionMet(id)) UIKit.Text(dr, "포인트가 부족합니다. 게임에서 적을 처치하면 포인트가 쌓입니다.", 20f, Dim, new Vector2(0f, -270f), new Vector2(900f, 30f));
            UIKit.MakeButton(dr, "취소", new Vector2(190f, -330f), new Vector2(240f, 70f), CloseDetail, 28f);
        }
    }

    static void CloseDetail()
    {
        if (detail != null) Object.Destroy(detail);
        detail = null;
    }

    public static bool DetailOpen => detail != null;
    public static void Back()
    {
        if (detail != null) CloseDetail();
        else Close();
    }

    // 스탯 한 줄: 이름 · 막대(거너 = 절반) · 퍼센트
    static void Stat(RectTransform parent, string ko, float value, bool known, ref float y, bool lowerIsBetter = false)
    {
        float good = lowerIsBetter ? 2f - value : value;
        UIKit.Text(parent, ko, 26f, Parch, new Vector2(-40f, y), new Vector2(200f, 40f), TextAlignmentOptions.Left);
        RectTransform bg = UIKit.Rect("Bar", parent, new Vector2(300f, y), new Vector2(420f, 18f));
        bg.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.18f, 0.25f);
        if (known)
        {
            float k = Mathf.Clamp01(value / 2f);
            RectTransform fill = UIKit.Rect("Fill", bg, Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(k, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Color c = good > 1.01f ? new Color(0.5f, 0.9f, 0.5f) : good < 0.99f ?new Color(0.95f, 0.5f, 0.45f) : Gold;
            fill.gameObject.AddComponent<Image>().color = c;
            // 거너 기준선
            RectTransform mark = UIKit.Rect("Base", bg, Vector2.zero, new Vector2(3f, 26f));
            mark.anchorMin = mark.anchorMax = new Vector2(0.5f, 0.5f);
            mark.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);
        }
        UIKit.Text(parent, known ? Mathf.RoundToInt(value * 100f) + "%" : "???", 26f, Gold, new Vector2(580f, y), new Vector2(120f, 40f), TextAlignmentOptions.Right);
        y -= 46f;
    }

    static void StatText(RectTransform parent, string ko, string value, bool known, ref float y)
    {
        UIKit.Text(parent, ko, 26f, Parch, new Vector2(-40f, y), new Vector2(200f, 40f), TextAlignmentOptions.Left);
        TMP_Text t = UIKit.Text(parent, "", 26f, Gold, new Vector2(580f, y), new Vector2(300f, 40f), TextAlignmentOptions.Right);
        t.text = known ? value : "???";
        y -= 46f;
    }

    static void Info(RectTransform parent, string ko, string value, ref float y)
    {
        UIKit.Text(parent, ko, 24f, Gold, new Vector2(-40f, y), new Vector2(200f, 40f), TextAlignmentOptions.Left);
        TMP_Text t = UIKit.Text(parent, "", 22f, Parch, new Vector2(330f, y - 6f), new Vector2(680f, 70f), TextAlignmentOptions.TopLeft);
        t.text = value;
        y -= 78f;
    }
}

// 캐릭터 창: ESC로 자세히 → 목록 → 닫기
public class CharacterUIInput : MonoBehaviour
{
    void Update()
    {
        if (GameInput.EscDown) CharacterUI.Back();
    }
}
