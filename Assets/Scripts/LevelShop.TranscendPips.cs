using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 1.2~ 초월 충전 표시 (설계 3-5 ① g): 상한 레벨에 닿은 뒤 HUD 경험치 막대 오른쪽 끝에 「◆◆◆」 (금빛 = 채운 막대)
//   예전엔 막대를 채울 때 뜨는 토스트로만 보여 「다음 규칙까지 얼마」를 알 수 없었음 · 간격(막대 수)은 TranscendBarsPer 그대로 따라감
//   초월이 없는 판(튜토리얼 · 자동 플레이 · 올릴 카드가 없음)에서는 숨김 · 마우스를 올리면 한 줄 설명
public partial class LevelShop
{
    public int TranscendBarsFilled => transcendBars;
    public int TranscendBarsNeeded => TranscendBarsPer;
    public bool TranscendCharging => AtLevelCap && TranscendLeft && !TutorialRun.Active && !GameInput.Auto;
}

public class TranscendPips : MonoBehaviour
{
    TMP_Text text;
    LevelShop shop;
    float tick;
    string shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) => { if (s.name == "GameScene") new GameObject("TranscendPips").AddComponent<TranscendPips>(); };
    }

    void Update()
    {
        tick -= Time.unscaledDeltaTime;
        if (tick > 0f) return;
        tick = 0.25f;
        if (text == null && !Build()) return;
        if (shop == null) shop = FindFirstObjectByType<LevelShop>(FindObjectsInactive.Include);
        bool on = shop != null && shop.TranscendCharging;
        if (text.gameObject.activeSelf != on) text.gameObject.SetActive(on);
        if (!on) return;
        int have = Mathf.Clamp(shop.TranscendBarsFilled, 0, shop.TranscendBarsNeeded), need = shop.TranscendBarsNeeded;
        string s = "<color=#FFD75A>" + new string('◆', have) + "</color><color=#5E5272>" + new string('◆', Mathf.Max(0, need - have)) + "</color>";
        if (s != shown) { text.text = s; shown = s; }
    }

    // 경험치 막대(Level) 오른쪽 끝 바깥에 붙임
    bool Build()
    {
        Level lv = FindFirstObjectByType<Level>();
        if (lv == null) return false;
        UIKit.EnsureStyle();
        // Level 은 채움 막대 자체라(폭이 경험치에 따라 바뀜) 그 틀(부모)의 오른쪽 끝에
        RectTransform bar = lv.transform.parent as RectTransform ?? (RectTransform)lv.transform;
        text = UIKit.Text(bar, "", 24f, Color.white, Vector2.zero, new Vector2(120f, 34f), TextAlignmentOptions.Left);
        UIKit.Forget(text);
        RectTransform r = text.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(1f, 0.5f);
        r.pivot = new Vector2(0f, 0.5f);
        r.anchoredPosition = new Vector2(10f, 0f);
        text.raycastTarget = true;
        TooltipTrigger tip = text.gameObject.AddComponent<TooltipTrigger>();
        tip.titleProvider = () => Loc.T("초월 카드");
        tip.bodyProvider = () => shop == null ? "" : Loc.T("transcend.pips2").Replace("{0}", shop.TranscendBarsFilled.ToString()).Replace("{1}", shop.TranscendBarsNeeded.ToString())
                                                    + (GameMode.IsEndless ? " · " + GameMode.Name(Difficulty.Endless) : "");
        text.gameObject.SetActive(false);
        return true;
    }
}
