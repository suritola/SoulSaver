using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 메뉴 스킨 상점: 종류(캐릭터 · 무기 · 이펙트 · 커서) → 캐릭터 → 스킨 카드. 카드를 누르면 아래에 설명 · 소리 듣기 · 구매/장착
// 비싼 물건이라 구매는 두 번 눌러야 함 (처음 누르면 "한 번 더 누르면 구매")
public class SkinShopUI : MonoBehaviour
{
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.6f, 0.56f, 0.62f);

    static GameObject open;
    RectTransform win;
    TMP_Text pointsText, descText, titleText, newHeroNote;
    Button actionButton, soundButton;
    TMP_Text actionLabel;
    readonly List<GameObject> dynamic = new List<GameObject>();
    readonly List<Button> tabButtons = new List<Button>(), whoButtons = new List<Button>();
    readonly List<(Image img, Sprite[] frames)> anims = new List<(Image, Sprite[])>();
    readonly List<(Image frame, Color c)> pulses = new List<(Image, Color)>();
    SkinKind kind = SkinKind.Character;
    int who;
    SkinDef selected;           // null = 기본
    bool confirm;
    float confirmUntil;
    SpecialFeedback audioFx;
    Coroutine preview;          // 1.1~ 소리 듣기를 연타하면 미리듣기가 겹쳐 쌓였음 → 하나만

    public static void Open(Transform root)
    {
        if (open != null) return;
        RectTransform w = UIKit.Modal(root, "SkinShop", new Vector2(1640f, 960f), out open);
        // 1.1~ UI 크기 120% 면 캔버스가 1600x900 이라 창(1640x960)의 위아래 · 좌우가 화면 밖으로 잘렸음 → 화면에 맞게 통째로 줄임
        Rect area = ((RectTransform)open.transform).rect;
        float fit = Mathf.Min(1f, (area.width - 16f) / 1640f, (area.height - 16f) / 960f);
        if (fit > 0f && fit < 1f) w.parent.localScale = Vector3.one * fit;
        SkinShopUI ui = open.AddComponent<SkinShopUI>();
        ui.win = w;
        // 1.0.4~: 새 영웅(아직 스킨 없음)을 고른 채 열면 「기본」 한 장만 보였음 → 거너 칸으로 열고 「스킨 준비 중」 안내
        ui.who = CharacterData.IsNewHero(CharacterData.Selected) ? (int)CharacterId.Gunner : (int)CharacterData.Selected;
        ui.Build();
    }

    public static void Close()
    {
        MenuHeroPanel.RefreshLater();       // 1.1.9~ 메인 메뉴 「지금 영웅」 패널 (끼운 스킨 그림)
        if (open == null) return;
        Destroy(open);
        open = null;
        SkinFx.Refresh();
    }

    void OnDestroy()
    {
        if (audioFx != null) Destroy(audioFx.gameObject);
    }

    void Build()
    {
        UIKit.Text(win, "스킨 상점", 50f, Gold, new Vector2(0f, 425f), new Vector2(600f, 70f));
        UIKit.MakeButton(win, "닫기", new Vector2(720f, 425f), new Vector2(150f, 58f), Close, 22f);
        pointsText = UIKit.Text(win, "", 26f, Gold, new Vector2(-590f, 425f), new Vector2(420f, 50f), TextAlignmentOptions.Left);

        string[] tabs = { "캐릭터", "무기", "이펙트", "커서" };
        for (int i = 0; i < tabs.Length; i++)
        {
            SkinKind k = (SkinKind)i;
            tabButtons.Add(UIKit.MakeButton(win, tabs[i], new Vector2(-520f + 220f * i, 345f), new Vector2(200f, 56f), () => { kind = k; selected = null; Rebuild(); }, 24f));
        }
        // 캐릭터 고르기 (해금한 캐릭터만)
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterId c = (CharacterId)i;
            if (!CharacterData.IsDeveloped(c) || !CharacterData.IsUnlocked(c) || CharacterData.IsNewHero(c)) continue;     // 1.0.2 새 캐릭터는 아직 스킨이 없음
            int id = i;
            Button b = UIKit.MakeButton(win, CharacterData.Def(c).name, new Vector2(-560f + 190f * whoButtons.Count, 270f), new Vector2(175f, 50f), () => { who = id; selected = null; Rebuild(); }, 20f);
            b.name = "Who_" + id;
            whoButtons.Add(b);
        }

        if (CharacterData.IsNewHero(CharacterData.Selected))
        {
            newHeroNote = UIKit.Text(win, "", 18f, Dim, new Vector2(0f, 222f), new Vector2(1400f, 28f));
            newHeroNote.text = Loc.T("「{0}」의 스킨은 준비 중입니다").Replace("{0}", Loc.T(CharacterData.Def(CharacterData.Selected).name));
        }

        // 아래 설명줄
        Image bar = UIKit.Rect("DetailBar", win, new Vector2(0f, -360f), new Vector2(1560f, 150f)).gameObject.AddComponent<Image>();
        bar.color = new Color(0.05f, 0.04f, 0.08f, 0.9f);
        titleText = UIKit.Text(win, "", 30f, Gold, new Vector2(-280f, -318f), new Vector2(980f, 44f), TextAlignmentOptions.Left);
        descText = UIKit.Text(win, "", 22f, Parch, new Vector2(-280f, -382f), new Vector2(980f, 80f), TextAlignmentOptions.TopLeft);
        descText.fontSizeMin = 14f;         // 1.0.4~ 구매 확인 때 경고 한 줄이 붙어도 칸 안에 (자동 크기)
        soundButton = UIKit.MakeButton(win, "소리 듣기", new Vector2(390f, -360f), new Vector2(200f, 64f), PlayPreview, 22f);
        actionButton = UIKit.MakeButton(win, "", new Vector2(640f, -360f), new Vector2(260f, 64f), OnAction, 22f);
        actionLabel = actionButton.GetComponentInChildren<TMP_Text>();

        // 소리 미리 듣기용 합성기 (메인 메뉴에는 게임의 합성기가 없음)
        GameObject a = new GameObject("SkinPreviewAudio");
        audioFx = a.AddComponent<SpecialFeedback>();
        audioFx.Init(UIKit.Font, UIKit.FontMaterial);
        SkinAudio.Register(audioFx);

        Rebuild();
    }

    void Rebuild()
    {
        foreach (GameObject g in dynamic) if (g != null) Destroy(g);
        dynamic.Clear();
        StopPreview();          // 1.1~ 다른 카드 · 탭으로 바꾸면 돌던 미리듣기도 멈춤
        anims.Clear();
        pulses.Clear();
        confirm = false;
        pointsText.text = Loc.T("보유 포인트: ") + CharacterData.Points.ToString("N0") + " P";
        for (int i = 0; i < tabButtons.Count; i++) tabButtons[i].GetComponent<Image>().color = (int)kind == i ? Gold : new Color(0.72f, 0.7f, 0.78f);
        if (newHeroNote != null) newHeroNote.gameObject.SetActive(!SkinData.IsGlobal(kind));
        foreach (Button b in whoButtons)
        {
            b.gameObject.SetActive(!SkinData.IsGlobal(kind));
            b.GetComponent<Image>().color = b.name == "Who_" + who ? Gold : new Color(0.72f, 0.7f, 0.78f);
        }

        // 카드: 기본 + 이 종류 · 캐릭터의 스킨
        List<SkinDef> list = new List<SkinDef> { null };
        foreach (SkinDef s in SkinData.All)
            if (s.kind == kind && (SkinData.IsGlobal(kind) || s.who == who)) list.Add(s);
        float w = 290f, gap = 20f;
        float x0 = -(list.Count - 1) * (w + gap) / 2f;
        for (int i = 0; i < list.Count; i++) dynamic.Add(Card(list[i], new Vector2(x0 + i * (w + gap), SkinData.IsGlobal(kind) ? 60f : 20f)));
        ShowDetail();
    }

    SkinDef EquippedNow => SkinData.Equipped(kind, who);

    GameObject Card(SkinDef s, Vector2 pos)
    {
        int tier = s != null ? s.tier : 0;
        Color tc = s != null ? SkinData.TierColors[tier] : new Color(0.5f, 0.48f, 0.55f);
        bool owned = s == null || SkinData.Owns(s.id);
        bool equipped = s == null ? EquippedNow == null : EquippedNow != null && EquippedNow.id == s.id;
        bool isSel = s == selected;

        Button b = UIKit.MakeButton(win, "", pos, new Vector2(290f, 360f), () => { selected = s; confirm = false; Rebuild(); }, 20f);
        Image frame = b.GetComponent<Image>();
        frame.color = isSel ? Color.white : tc;
        if (tier == 3 && !isSel) pulses.Add((frame, tc));      // 전설: 금빛 테두리가 은은하게 (고른 카드는 흰 테두리 그대로)
        Transform t = b.transform;
        Image back = UIKit.Rect("Back", t, Vector2.zero, new Vector2(262f, 332f)).gameObject.AddComponent<Image>();
        back.color = new Color(0.08f, 0.06f, 0.11f, 0.97f);
        back.raycastTarget = false;
        // 1.1~ 등급 띠: 예전엔 같은 색 반투명 띠 위에 같은 색 글자라 희귀 · 영웅이 대비 2~2.5:1 로 흐렸음
        //   → 띠는 등급색을 어둡게, 글자는 등급색을 조금 밝게 + 굵게 (대비 6:1 이상)
        Image tierBand = UIKit.Rect("Tier", t, new Vector2(0f, 150f), new Vector2(262f, 32f)).gameObject.AddComponent<Image>();
        tierBand.color = new Color(tc.r * 0.22f, tc.g * 0.22f, tc.b * 0.22f, 1f);
        tierBand.raycastTarget = false;
        TMP_Text tierText = UIKit.Text(t, "", 20f, Color.Lerp(tc, Color.white, 0.25f), new Vector2(0f, 150f), new Vector2(250f, 30f));
        tierText.fontStyle = FontStyles.Bold;
        tierText.text = s != null ? Loc.T(SkinData.TierNames[tier]) : Loc.T("기본");

        // 미리보기 그림 (캐릭터는 달리는 모습을 되풀이)
        Image pic = UIKit.Rect("Preview", t, new Vector2(0f, 40f), new Vector2(170f, 170f)).gameObject.AddComponent<Image>();
        pic.preserveAspect = true;
        pic.raycastTarget = false;
        Sprite[] frames = PreviewFrames(s);
        if (frames.Length > 0) pic.sprite = frames[0];
        if (frames.Length > 1) anims.Add((pic, frames));

        TMP_Text name = UIKit.Text(t, "", 26f, s != null && tier == 3 ? Gold : Parch, new Vector2(0f, -80f), new Vector2(260f, 36f));
        name.text = s != null ? Loc.T(s.name) : Loc.T("기본");
        TMP_Text state = UIKit.Text(t, "", 22f, equipped ? new Color(0.6f, 1f, 0.5f) : owned ? Parch : Gold, new Vector2(0f, -130f), new Vector2(260f, 32f));
        state.text = equipped ? Loc.T("장착 중") : owned ? Loc.T("보유") : s.Price.ToString("N0") + " P";
        return b.gameObject;
    }

    Sprite[] PreviewFrames(SkinDef s)
    {
        switch (kind)
        {
            case SkinKind.Character:
                {
                    CharacterDef d = CharacterData.Def((CharacterId)who);
                    string sheet = s != null ? s.id : d.body ?? "gunner_nogun";
                    List<Sprite> l = new List<Sprite>(Resources.LoadAll<Sprite>("Characters/" + sheet));
                    l.Sort((a, b) => Num(a.name).CompareTo(Num(b.name)));
                    // 달리기 프레임 (다른 캐릭터 2~5)
                    if (d.body != null && l.Count >= 6) return new[] { l[2], l[3], l[4], l[5] };
                    // 거너 (1.0.1): 시트가 줄마다 동작이고 줄 끝에 「idle」 · 「Run」 같은 빨간 글씨가 그려져 있어,
                    // 이름 순서로 앞 네 장을 쓰면 서 있기 · 글씨 · 말하기가 섞여 이상한 프레임이 끼었음 → 위에서 네 번째 줄(달리기)의 네 장만
                    List<Sprite> run = l.FindAll(sp => sp.texture != null && Mathf.RoundToInt(sp.rect.y) == sp.texture.height - 32 * 4 && sp.rect.x < 32 * 4);
                    run.Sort((a2, b2) => a2.rect.x.CompareTo(b2.rect.x));
                    if (run.Count > 0) return run.ToArray();
                    return l.Count > 0 ? new[] { l[0] } : new Sprite[0];
                }
            case SkinKind.Weapon:
                {
                    string held = CharacterData.Def((CharacterId)who).held ?? "pistol";
                    Sprite sp = Resources.Load<Sprite>("Weapons/weapon_" + (s != null ? s.id : held));
                    return sp != null ? new[] { sp } : new Sprite[0];
                }
            case SkinKind.Cursor:
                {
                    Sprite sp = Resources.Load<Sprite>("Icons/" + (s != null ? s.id : "cursor_default"));
                    return sp != null ? new[] { sp } : new Sprite[0];
                }
            default:
                {
                    Sprite sp = s != null ? Resources.Load<Sprite>("Icons/" + s.id) : null;
                    if (sp == null) { Sprite[] f = Fx.Frames("fx_spark"); return f.Length > 0 ? new[] { f[0] } : new Sprite[0]; }
                    return new[] { sp };
                }
        }
    }

    static int Num(string n)
    {
        int i = n.LastIndexOf('_');
        return i >= 0 && int.TryParse(n.Substring(i + 1), out int v) ? v : 0;
    }

    void ShowDetail()
    {
        SkinDef s = selected;
        bool owned = s == null || SkinData.Owns(s.id);
        bool equipped = s == null ? EquippedNow == null : EquippedNow != null && EquippedNow.id == s.id;
        titleText.text = s != null ? Loc.T(s.name) + "  <size=70%><color=#" + ColorUtility.ToHtmlStringRGB(SkinData.TierColors[s.tier]) + ">" + Loc.T(SkinData.TierNames[s.tier]) + "</color></size>"
                                   : Loc.T("기본");
        descText.text = s != null ? Loc.T(s.desc) : Loc.T("원래 모습 · 원래 소리");
        soundButton.gameObject.SetActive(s != null);
        if (equipped) actionLabel.text = Loc.T("장착 중");
        else if (owned) actionLabel.text = Loc.T("장착");
        else if (confirm)
        {
            actionLabel.text = Loc.T("한 번 더 누르면 구매");
            // 1.0.4~: 스킨은 최대 100,000P인데 영웅과 같은 포인트라는 경고가 없었음 → 다음 영웅 가격을 함께
            string warn = NextHeroWarning();
            if (warn != "") descText.text += "\n<color=#FFB37A>" + warn + "</color>";
        }
        else actionLabel.text = Loc.T("구매") + "  " + s.Price.ToString("N0") + " P";
        actionButton.interactable = !equipped && (owned || CharacterData.Points >= s.Price);
        actionLabel.color = !owned && CharacterData.Points < (s != null ? s.Price : 0) ? new Color(1f, 0.5f, 0.45f) : new Color(0.96f, 0.9f, 0.8f);
        if (!owned && CharacterData.Points < s.Price) actionLabel.text = Loc.T("포인트 부족") + "  " + s.Price.ToString("N0") + " P";
    }

    // 아직 안 산 영웅 중 가장 싼 값 (체험판 · 모두 샀으면 빈 글)
    static string NextHeroWarning()
    {
        if (Demo.On) return "";
        CharacterDef next = null;
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterId c = (CharacterId)i;
            CharacterDef d = CharacterData.Def(c);
            if (d.price <= 0 || !CharacterData.IsDeveloped(c) || CharacterData.IsUnlocked(c)) continue;
            if (next == null || d.price < next.price) next = d;
        }
        if (next == null) return "";
        return Loc.T("포인트는 새 영웅 해금에도 씁니다 · 다음 영웅 「{0}」 {1} P").Replace("{0}", Loc.T(next.name)).Replace("{1}", next.price.ToString("N0"));
    }

    void OnAction()
    {
        SkinDef s = selected;
        if (s == null) { SkinData.Unequip(kind, who); Done("clank"); return; }
        if (SkinData.Owns(s.id)) { SkinData.Equip(s); Done("clank"); return; }
        if (!confirm) { confirm = true; confirmUntil = Time.unscaledTime + 3f; ShowDetail(); return; }
        if (SkinData.Buy(s)) Done("levelup");
    }

    void Done(string sound)
    {
        audioFx?.PlayRaw(sound, 0.6f);
        confirm = false;
        SkinFx.Refresh();
        CursorSkin.Refresh();
        Rebuild();
    }

    void PlayPreview()
    {
        if (selected == null || audioFx == null) return;
        StopPreview();
        preview = StartCoroutine(Preview(selected));
    }

    void StopPreview()
    {
        if (preview != null) StopCoroutine(preview);
        preview = null;
    }

    IEnumerator Preview(SkinDef s)
    {
        switch (s.kind)
        {
            case SkinKind.Character:
                if (s.hurtSound != null) audioFx.PlayRaw(s.hurtSound, 0.8f);
                if (s.tier >= 3) { yield return new WaitForSecondsRealtime(0.6f); audioFx.PlayRaw(SkinAudio.UltSound(s.id), 0.6f); }
                break;
            case SkinKind.Weapon:
                for (int i = 0; i < 3; i++)
                {
                    audioFx.PlayRaw(s.attackSound ?? BaseAttack(s.who), 0.7f, s.pitch);
                    if (s.layer != null) audioFx.PlayRaw(s.layer, 0.25f);
                    yield return new WaitForSecondsRealtime(0.35f);
                }
                break;
            case SkinKind.Cursor:
                audioFx.PlayRaw("clank", 0.5f, 1.4f + 0.1f * s.tier);
                break;
            default:
                float p = s.id == "skin_sakura" ? 1.3f : s.id == "skin_starlight" ? 1.5f : 1f;
                for (int i = 0; i < 3; i++) { audioFx.PlayRaw("sparkle", 0.4f, p); yield return new WaitForSecondsRealtime(0.12f); }
                if (s.killSound != null) audioFx.PlayRaw(s.killSound, 0.5f);
                break;
        }
    }

    static string BaseAttack(int who) => (CharacterId)who switch
    {
        CharacterId.Swordsman => "slash",
        CharacterId.Rogue => "whoosh",
        CharacterId.Archer => "bowtwang",
        CharacterId.Alchemist => "glassclink",
        _ => "gunshot",
    };

    void Update()
    {
        if (GameInput.EscDown) { SettingsUI.EscHandledFrame = Time.frameCount; Close(); return; }
        int f = (int)(Time.unscaledTime * 8f);
        foreach (var a in anims) if (a.img != null) a.img.sprite = a.frames[f % a.frames.Length];
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
        foreach (var p in pulses) if (p.frame != null) p.frame.color = Color.Lerp(p.c, Color.white, pulse * 0.6f);
        if (confirm && Time.unscaledTime > confirmUntil) { confirm = false; ShowDetail(); }
    }
}
