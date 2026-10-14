using TMPro;
using UnityEngine;

// 레벨업 · 영혼 트리 알림 (1.0.1): 「레벨업 [Space]」 · 「영혼 트리 [T]」가 화면 아래 · 구석에만 있어 싸우다 까먹는다는 의견
// → 싸우는 동안 보는 곳(캐릭터 머리 위)에 통통 튀며 반짝이는 글자, 오래 놓아두면 소리와 함께 한 번 더 알림
// StageManager 가 붙임
public class GrowthPrompts : MonoBehaviour
{
    const float LevelNagAfter = 15f, LevelNagEvery = 20f, TreeNagAfter = 30f, TreeNagEvery = 40f;
    // 1.0.4~: 20 ~ 40초마다 종소리와 함께 끝없이 되풀이돼 성가셨음 → 한 번 쌓일 때마다 배너는 최대 MaxNags 번 (머리 위 글자는 그대로)
    const int MaxNags = 2;
    int levelNags, treeNags;

    TextMeshPro tag;
    LevelShop shop;
    float levelSince = -1f, treeSince = -1f, nextLevelNag, nextTreeNag;
    string shownText;

    void Start() => shop = FindFirstObjectByType<LevelShop>();

    void LateUpdate()
    {
        StageManager sm = StageManager.Instance;
        PlayerController p = Hostile.Player;
        bool can = sm != null && sm.Started && p != null && !p.IsDying && shop != null && Time.timeScale > 0f
                   && !ESCmenu.IsOpen && !ChoiceUI.Open && !SoulTreeUI.IsOpen && !TutorialRun.Active && !GameInput.TrailerRunning;
        bool level = can && shop.PendingLevels > 0 && !shop.IsOpen && !GameSettings.AutoLevelUp;
        bool tree = can && sm.TreeAffordable;

        float now = Time.unscaledTime;
        Nag(level, ref levelSince, ref nextLevelNag, ref levelNags, LevelNagAfter, LevelNagEvery, now, sm,
            Loc.T("레벨업 카드를 아직 고르지 않았습니다!") + "\n[" + KeyBindings.Name(GameAction.Interact) + "] " + Loc.T("카드 고르기"));     // 1.0.4~: 용어 통일
        Nag(tree, ref treeSince, ref nextTreeNag, ref treeNags, TreeNagAfter, TreeNagEvery, now, sm,
            Loc.T("영혼 트리에서 배울 칸이 있습니다!") + "\n[" + KeyBindings.Name(GameAction.Upgrade) + "] " + Loc.T("영혼 트리"));

        if (!level && !tree)
        {
            if (tag != null && tag.gameObject.activeSelf) tag.gameObject.SetActive(false);
            return;
        }
        if (tag == null) Build();
        if (!tag.gameObject.activeSelf) tag.gameObject.SetActive(true);

        string text = "";
        if (level)
            text += "<color=#FFD966>" + Loc.T("레벨업!") + " [" + KeyBindings.Name(GameAction.Interact) + "]" + (shop.PendingLevels > 1 ? " x" + shop.PendingLevels : "") + "</color>";
        if (tree)
            text += (text.Length > 0 ? "\n" : "") + "<color=#C9A0FF>" + Loc.T("영혼 트리") + " [" + KeyBindings.Name(GameAction.Upgrade) + "]</color>";
        if (text != shownText) { tag.text = text; shownText = text; }

        // 머리 위에서 통통 튀며 숨 쉬듯 커졌다 작아짐
        float bounce = Mathf.Abs(Mathf.Sin(now * 4f));
        // 1.0.4~: 영웅 머리 위 막대 · 연금술사 시약 이름이 있으면 그 위에 (예전엔 셋이 겹칠 수 있었음)
        float baseY = p.transform.position.y + 2.1f;
        if (CharacterKit.Instance != null) baseY = Mathf.Max(baseY, CharacterKit.Instance.OverheadTop);
        tag.transform.position = new Vector3(p.transform.position.x, baseY + 0.25f * bounce, p.transform.position.z);
        tag.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(now * 8f));
    }

    // 오래 놓아두면 소리와 배너로 한 번 더
    // 1.0.4~: 최대 MaxNags 번 · 다른 알림(보스 · 상점 · 해금)이 떠 있으면 덮지 않고 잠시 미룸 · 보스전에는 종소리 없이
    static void Nag(bool on, ref float since, ref float next, ref int count, float after, float every, float now, StageManager sm, string banner)
    {
        if (!on) { since = -1f; count = 0; return; }
        if (since < 0f) { since = now; next = now + after; return; }
        if (now < next || count >= MaxNags) return;
        if (sm != null && sm.BannerBusy) { next = now + 3f; return; }
        next = now + every;
        count++;
        if (sm != null) sm.ShowBanner(banner, 2.2f);
        if (bosss.LiveCount == 0) Hostile.Play("chime", 0.8f, 1.2f);
    }

    void Build()
    {
        UIKit.EnsureStyle();
        GameObject go = new GameObject("GrowthPrompt");
        tag = go.AddComponent<TextMeshPro>();
        if (UIKit.Font != null) tag.font = UIKit.Font;
        if (UIKit.FontMaterial != null) tag.fontSharedMaterial = UIKit.FontMaterial;
        tag.fontSize = 6.5f;
        tag.fontStyle = FontStyles.Bold;
        tag.alignment = TextAlignmentOptions.Bottom;
        tag.enableWordWrapping = false;
        tag.outlineWidth = 0.25f;
        tag.outlineColor = new Color32(20, 12, 28, 255);
        tag.rectTransform.sizeDelta = new Vector2(12f, 3f);
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sortingLayerID = PlayerLook.TopLayer;        // 1.0.4~ 머리 위 알림도 플레이어와 같이 모든 이펙트 위
        mr.sortingOrder = 50;
    }

    void OnDestroy()
    {
        if (tag != null) Destroy(tag.gameObject);
    }
}
