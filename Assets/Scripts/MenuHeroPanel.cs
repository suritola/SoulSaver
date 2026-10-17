using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1.1.9~ 메인 메뉴 왼쪽 「지금 영웅」 패널 (편의성 2차 2)
// 예전엔 메뉴 어디에도 지금 고른 영웅이 안 보여 판에 들어가서야 알았음 → 씬의 고정 그림 자리에 초상 · 이름 · 칭호 · 숙련 + 「영웅 바꾸기」
// 패널 (-610, -120) 440×560 → x -830 ~ -390 · y -400 ~ 160 (난이도 「쉬움」 버튼 왼끝 -392 와 2px 떨어짐)
public static class MenuHeroPanel
{
    static RectTransform panel;
    static Transform rootRef;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);
    static readonly Color Back = new Color(0.08f, 0.06f, 0.11f, 0.6f);
    static readonly Color BackHover = new Color(0.12f, 0.1f, 0.16f, 0.7f);

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
        panel = UIKit.Rect("HeroPanel", parent, new Vector2(-610f, -120f), new Vector2(440f, 560f));
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
        UIKit.Text(panel, "지금 영웅", 18f, Dim, new Vector2(0f, 262f), new Vector2(400f, 26f));
        RectTransform pr = UIKit.Rect("Portrait", panel, new Vector2(0f, 90f), new Vector2(320f, 320f));
        Image img = pr.gameObject.AddComponent<Image>();
        img.sprite = CharacterUI.Portrait(id);
        img.preserveAspect = true;
        img.raycastTarget = false;
        if (d.body == null)
        {
            // 거너는 권총을 따로 그려 줌 (캐릭터 그림에는 총이 없음 · 캐릭터 창과 같게)
            RectTransform gr = UIKit.Rect("Gun", pr, new Vector2(80f, -30f), new Vector2(90f, 45f));
            Image gi = gr.gameObject.AddComponent<Image>();
            gi.sprite = Resources.Load<Sprite>("Weapons/weapon_pistol");
            gi.preserveAspect = true;
            gi.raycastTarget = false;
        }
        TMP_Text name = UIKit.Text(panel, d.name, 40f, Gold, new Vector2(0f, -110f), new Vector2(400f, 50f));
        name.fontSizeMin = 20f;
        UIKit.Text(panel, d.title, 22f, Dim, new Vector2(0f, -148f), new Vector2(400f, 30f));
        TMP_Text star = UIKit.Text(panel, "", 18f, Mastery.Level(id) > 0 ? new Color(1f, 0.85f, 0.35f) : Dim, new Vector2(0f, -178f), new Vector2(400f, 26f));
        star.text = Mastery.Label(id);
        string weight = SoulWeight.CardTag(id);
        if (weight.Length > 0) star.text += "  ·  <color=#C9A0FF>" + weight + "</color>";
        Button change = UIKit.MakeButton(panel, "영웅 바꾸기", new Vector2(0f, -235f), new Vector2(300f, 60f), OpenPicker, 24f);
        change.name = "ChangeHeroButton";
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
