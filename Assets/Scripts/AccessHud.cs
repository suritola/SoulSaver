using TMPro;
using UnityEngine;

// 1.1.9~ 접근성 안내 한 줄 (편의성 2차 10): 화면 아래 가운데 y 300 (도움말 자리 236 위)
//   「공격 고정」이 걸려 있으면 「[공격] 다시 누르면 멈춤」, 「필살기 누르면 토글」로 조준 중이면 「[필살기] 다시 누르면 발동」 — 한 자리를 상태에 따라 번갈아
public class AccessHud : MonoBehaviour
{
    static AccessHud instance;
    TMP_Text line;
    int shown = -1;
    bool shownPad;
    float rewriteAt;

    public static void Install()
    {
        if (instance != null) return;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        instance = canvas.gameObject.AddComponent<AccessHud>();
        TMP_Text t = UIKit.Text(canvas.transform, "", 20f, new Color(1f, 0.9f, 0.62f), Vector2.zero, new Vector2(900f, 30f));
        RectTransform r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
        r.anchoredPosition = new Vector2(0f, 300f);
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(0, 0, 0, 255);
        t.gameObject.SetActive(false);
        instance.line = t;
    }

    void OnDestroy() { if (instance == this) instance = null; }

    void LateUpdate()
    {
        if (line == null) return;
        int want = 0;
        if (Time.timeScale > 0f && !StoryDirector.Playing && !GameInput.Auto)
        {
            PlayerController p = Hostile.Player;
            CharacterKit k = CharacterKit.Instance;
            bool aiming = p != null && (p.IsSkillUsing || p.InstantAiming) || k != null && k.UltEngaged;
            if (GameInput.UltLatched && aiming) want = 2;
            else if (GameInput.AttackLocked) want = 1;
        }
        bool on = want != 0;
        if (line.gameObject.activeSelf != on) line.gameObject.SetActive(on);
        if (!on) { shown = 0; return; }
        // 장치가 바뀌거나(패드 LT) 1초마다 (바꾼 키 이름) 다시 씀
        if (want == shown && shownPad == GamePad.Active && Time.unscaledTime < rewriteAt) return;
        shown = want;
        shownPad = GamePad.Active;
        rewriteAt = Time.unscaledTime + 1f;
        UIKit.Forget(line);
        line.text = Loc.T(want == 2 ? "[{ULT}] 다시 누르면 발동" : "[{ATTACK}] 다시 누르면 멈춤");
    }
}
