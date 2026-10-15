using TMPro;
using UnityEngine;

// 1.1~ 레벨업 템포 (로드맵 1-1)
//   쌓인 레벨업 이어 고르기: 쌓인 레벨업을 열면 다 고를 때까지 창을 바로 다시 열고, 창 위에 「레벨업 카드 3/5」
//   판을 3번 미만 한 사람은 설정 「레벨업 바로 열기」가 꺼져 있어도 저절로 엶 (튜토리얼은 배지를 짚어 주므로 제외)
public partial class LevelShop
{
    int chainIndex;             // 이어 고르는 중 지금 몇 번째 창인지 (0 = 이어 고르는 중이 아님)
    TextMeshProUGUI chainText;
    int shownChainIndex = -1, shownChainTotal = -1;
    Loc.Lang shownChainLang;

    static bool AutoOpenLevelUp => (GameSettings.AutoLevelUp || (Juice.NewPlayer && !TutorialRun.Active)) && !GameInput.Auto && !ESCmenu.IsOpen;

    // 이어 고르는 중이면 다음 창을 바로 (상점 가판대 옆이어도)
    bool ChainNext => chainIndex > 0 && !GameInput.Auto && !ESCmenu.IsOpen;

    void UpdateChain()
    {
        if (chainIndex > 0 && !IsOpen && PendingLevels <= 0) chainIndex = 0;
        int total = chainIndex + PendingLevels;
        bool show = IsOpen && chainIndex > 0 && total > 1;
        if (show && chainText == null) BuildChainText();
        if (chainText == null) return;
        if (chainText.gameObject.activeSelf != show) chainText.gameObject.SetActive(show);
        // 1.1~ 진화 첫 안내 줄(LevelShop.EvoUI, y 448)이 떠 있으면 그 위로 (같은 자리에 겹쳐 둘 다 읽을 수 없었음)
        if (show)
        {
            float y = evoIntroShowing && evoIntroText != null && evoIntroText.gameObject.activeSelf ? 496f : 445f;
            RectTransform cr = chainText.rectTransform;
            if (!Mathf.Approximately(cr.anchoredPosition.y, y)) cr.anchoredPosition = new Vector2(0f, y);
        }
        if (!show || (shownChainIndex == chainIndex && shownChainTotal == total && shownChainLang == Loc.Current)) return;
        shownChainIndex = chainIndex;
        shownChainTotal = total;
        shownChainLang = Loc.Current;
        chainText.text = Loc.T("juice.chain").Replace("{0}", chainIndex.ToString()).Replace("{1}", total.ToString());
    }

    void BuildChainText()
    {
        if (LvshopPanel == null) return;
        LocTable.EnsureJuice();
        GameObject go = new GameObject("ChainCount", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(LvshopPanel.transform, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(700f, 48f);
        r.anchoredPosition = new Vector2(0f, 445f);        // 제목(380, 0.66배) 바로 위
        chainText = go.GetComponent<TextMeshProUGUI>();
        if (FirstTitle != null)
        {
            chainText.font = FirstTitle.font;
            chainText.fontSharedMaterial = FirstTitle.fontSharedMaterial;
        }
        chainText.fontSize = 34f;
        chainText.fontStyle = FontStyles.Bold;
        chainText.outlineWidth = 0.22f;
        chainText.outlineColor = new Color32(0, 0, 0, 255);
        chainText.color = new Color(1f, 0.86f, 0.4f);
        chainText.enableWordWrapping = false;
        chainText.alignment = TextAlignmentOptions.Center;
        chainText.raycastTarget = false;
        go.SetActive(false);
    }
}
