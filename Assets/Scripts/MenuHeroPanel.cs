using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1.1.9~ 메인 메뉴 왼쪽 「지금 영웅」 패널 (편의성 2차 2)
// 예전엔 메뉴 어디에도 지금 고른 영웅이 안 보여 판에 들어가서야 알았음 → 씬의 고정 그림 자리에 초상 · 이름 · 칭호 · 숙련 + 「영웅 바꾸기」
// 패널 (-610, -120) 440×560 → x -830 ~ -390 · y -400 ~ 160 (난이도 「쉬움」 버튼 왼끝 -392 와 2px 떨어짐)
// 1.4.8~ 위끝 · 아래끝을 가운데 열의 게임 시작 위끝 · 아이콘 칸 아래끝(MenuExtras.SideTop · SideBottom)에 맞춤 → y -395 ~ 35
//   (오른쪽 「이번 판 계약」 패널과 같은 높이 · 그 위 난이도 설명 한 줄이 넓게 지나감) · 안쪽 여백 24 · 글씨 20 이상
public static class MenuHeroPanel
{
    static RectTransform panel;
    const float W = 440f;
    static Transform rootRef;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);
    static readonly Color Back = new Color(0.08f, 0.06f, 0.11f, 0f);
    static readonly Color BackHover = new Color(0.96f, 0.83f, 0.47f, 0.08f);

    public static void Install(Transform parent, Transform root)
    {
        rootRef = root;
        // 씬에 박힌 고정 그림은 이름이 아니라 자리로 찾음 (「Image」라는 이름은 여럿) — 못 찾으면 패널만 덧그림
        Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
        if (canvas != null)
            foreach (Image img in canvas.GetComponentsInChildren<Image>(true))
            {
                RectTransform r = img.rectTransform;
                if (Vector2.Distance(r.anchoredPosition, new Vector2(-600f, -170f)) < 1f && Vector2.Distance(r.sizeDelta, new Vector2(460f, 460f)) < 1f)
                {
                    if (r.parent != null) parent = r.parent;
                    img.gameObject.SetActive(false);
                    break;
                }
            }
        // 1.4.8~ 상자 없이 영웅을 크게 세움 (카펫 왼쪽) · 뒤판은 투명(누름 · 마우스 받기용) · 마우스를 올리면 은은하게 밝아짐
        panel = UIKit.Rect("HeroPanel", parent, new Vector2(-600f, -190f), new Vector2(W, 600f));
        Image bg = panel.gameObject.AddComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = Back;
        // 뒤판 어디를 눌러도 영웅 고르기 (마우스를 올리면 살짝 밝아짐)
        Button b = panel.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(OpenPicker);
        HoverTint hover = panel.gameObject.AddComponent<HoverTint>();
        hover.target = bg;
        hover.normal = Back;
        hover.hover = BackHover;
        panel.gameObject.AddComponent<LocRefresh>().refresh = Refresh;
        Refresh();
    }

    static void OpenPicker()
    {
        if (CharacterUI.IsOpen || rootRef == null) return;
        CharacterUI.Open(rootRef);
    }

    // 영웅 · 스킨 · 언어가 바뀌면 다시 그림 (CharacterUI.Close · SkinShopUI.Close)
    public static void Refresh()
    {
        if (panel == null) return;
        for (int i = panel.childCount - 1; i >= 0; i--) Object.Destroy(panel.GetChild(i).gameObject);
        CharacterId id = CharacterData.Selected;
        CharacterDef d = CharacterData.Def(id);
        // 1.4.8~ 위에서부터: 「지금 영웅」 · 초상 · 이름 · 칭호 · 숙련 · 「영웅 바꾸기」 (높이 430 · 위아래 여백 24)
        float top = panel.sizeDelta.y * 0.5f - 24f, bottom = -panel.sizeDelta.y * 0.5f + 24f;
        // 1.4.8~ 사용자 「너무 많다」: 「지금 영웅」 · 칭호 · 숙련 줄을 빼고 초상 · 이름 · 「영웅 바꾸기」만 (칭호 · 숙련은 이름에 마우스를 올리면)
        const float P = 420f;          // 1.4.8~ 도트 그림 둘레 여백이 커서 실제 몸이 작게 보였음 → 칸을 키움
        RectTransform pr = UIKit.Rect("Portrait", panel, new Vector2(0f, top - P * 0.5f), new Vector2(P, P));
        Image img = pr.gameObject.AddComponent<Image>();
        img.sprite = CharacterUI.Portrait(id);
        img.preserveAspect = true;
        img.raycastTarget = false;
        if (d.body == null)
        {
            // 거너는 권총을 따로 그려 줌 (캐릭터 그림에는 총이 없음 · 캐릭터 창과 같게) — 1.4.8~ 초상 크기(320 → 190)에 비례
            float k = P / 320f;
            RectTransform gr = UIKit.Rect("Gun", pr, new Vector2(80f, -30f) * k, new Vector2(90f, 45f) * k);
            Image gi = gr.gameObject.AddComponent<Image>();
            gi.sprite = Resources.Load<Sprite>("Weapons/weapon_pistol");
            gi.preserveAspect = true;
            gi.raycastTarget = false;
        }
        float y = top - P + (UiRules.Legacy ? 50f : -6f);        // 그림 아래 여백만큼 이름을 끌어올림 · UI 개편: 끌어올리면 이름이 다리를 덮어서 발아래로
        TMP_Text name = UIKit.Text(panel, d.name, UiRules.Legacy ? 36f : UiTokens.Heading, UiRules.Legacy ? Gold : UiTokens.Text1, new Vector2(0f, y - 22f), new Vector2(W - 48f, 44f));
        name.fontSizeMin = 20f;
        name.raycastTarget = true;
        string sub = d.title + "\n" + Mastery.Label(id);
        string weight = SoulWeight.CardTag(id);
        if (weight.Length > 0) sub += "  ·  " + weight;
        TooltipTrigger nameTip = name.gameObject.AddComponent<TooltipTrigger>();
        nameTip.titleProvider = () => d.name;
        nameTip.bodyProvider = () => sub;
        Button change = UIKit.MakeButton(panel, "영웅 바꾸기", new Vector2(0f, y - 76f), new Vector2(300f, 52f), OpenPicker, 24f);
        change.name = "ChangeHeroButton";
        if (!UiRules.Legacy) { UiV2.Restyle(change, UiV2.Kind.Secondary, UiTokens.Body); UiV2.Label(change, "result.retry.hero"); }      // UI 개편 P0-4b: 석판 보조 버튼 · P0-6 「영웅 변경」 (결과 화면과 같은 이름)
        else MenuItemFx.Apply(change, 26f, false, false);       // 1.4.8~ 메인 메뉴와 같은 글자 버튼
    }

    // 스킨 상점이 닫힌 다음 프레임에 (닫는 중에는 끼운 스킨이 아직 저장 중일 수 있음)
    public static void RefreshLater()
    {
        if (panel == null) return;
        MenuToast.RunLater(Refresh);
    }
}

// 마우스를 올리면 뒤판 색을 바꿈 (Button 색 전환 대신 · 투명한 판이 하얗게 번쩍이지 않게)
public class HoverTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image target;
    public Color normal, hover;
    public void OnPointerEnter(PointerEventData e) { if (target != null) target.color = hover; }
    public void OnPointerExit(PointerEventData e) { if (target != null) target.color = normal; }
}

// 1.1.9~ 메인 메뉴 아래 가운데 짧은 알림 (메뉴에는 토스트 캔버스가 없음) · 다음 프레임 / 몇 초 뒤 할 일
public class MenuToast : MonoBehaviour
{
    static MenuToast runner;

    static MenuToast Runner()
    {
        if (runner != null) return runner;
        GameObject go = new GameObject("MenuToastRunner");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<MenuToast>();
        return runner;
    }

    // delay 초 뒤 (unscaled · 0 이면 다음 프레임)
    public static void RunLater(System.Action action, float delay = 0f)
    {
        if (action == null) return;
        Runner().StartCoroutine(Later(action, delay));
    }

    static IEnumerator Later(System.Action action, float delay)
    {
        yield return null;
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        try { action(); } catch (System.Exception e) { Debug.LogWarning("[MenuToast] " + e.Message); }
    }

    // 캔버스 아래 가운데 (y 24) 1200×30 · 20pt · seconds 초 뒤 걷힘
    public static void Show(Transform canvas, string ko, float seconds)
    {
        if (canvas == null) return;
        TMP_Text t = UIKit.Text(canvas, ko, 20f, new Color(0.66f, 0.61f, 0.53f), Vector2.zero, new Vector2(1200f, 30f));
        RectTransform r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
        r.anchoredPosition = new Vector2(0f, 24f);
        Runner().StartCoroutine(Fade(t, seconds));
    }

    static IEnumerator Fade(TMP_Text t, float seconds)
    {
        float until = Time.unscaledTime + seconds;
        while (t != null && Time.unscaledTime < until) yield return null;
        for (float a = 1f; t != null && a > 0f; a -= Time.unscaledDeltaTime / 0.5f) { t.alpha = a; yield return null; }
        if (t != null) Object.Destroy(t.gameObject);
    }
}
