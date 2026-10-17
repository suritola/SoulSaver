using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.2~ 메인 메뉴 「이번 판 계약」 세 줄 (MenuExtras.InstallMainMenu 가 부름)
//   오른쪽 아래 빈자리(오른쪽 위 버튼 열 아래끝 -266 아래 · 아이콘 칸 x 370 오른쪽) — 왼쪽은 영웅 그림이라 오른쪽에 둠
//   누르면 계약 보드 (체험판은 「정식판에서」 안내) · 다시 뽑기 하루 3번 · 받을 보상이 있으면 배지
//   고른 난이도(무한 모드면 무한 전용 풀) · 고른 영웅이 바뀌면 다시 씀 (LocRefresh · 0.5초마다 확인)
public class MenuContracts : MonoBehaviour
{
    TMP_Text head, list, badge;
    Button reroll, board;
    string shownFor = "";
    float check;

    public static void Install(Transform parent, Transform root)
    {
        if (TutorialRun.Active) return;
        Contracts.EnsureEnded();
        ContractBoard.Refresh(false);       // 기존 기록으로 채운 칸을 먼저 적어 둠 (받을 보상 배지 · 판 안에서 옛 기록 토스트가 뜨지 않게)
        UIKit.EnsureStyle();
        const float W = 470f, H = 300f;
        RectTransform box = UIKit.Rect("MenuContracts", parent, Vector2.zero, new Vector2(W, H));
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(1f, 0.5f);
        box.anchoredPosition = new Vector2(-30f, -190f);
        Image bg = box.gameObject.AddComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.08f, 0.06f, 0.11f, 0.82f);
        MenuContracts m = box.gameObject.AddComponent<MenuContracts>();
        m.head = UIKit.Text(box, "contract.run.title", 26f, new Color(0.79f, 0.63f, 1f), new Vector2(-60f, H / 2f - 28f), new Vector2(W - 150f, 36f), TextAlignmentOptions.Left);
        m.list = UIKit.Text(box, "", 19f, new Color(0.93f, 0.9f, 0.84f), new Vector2(0f, -6f), new Vector2(W - 36f, 170f), TextAlignmentOptions.TopLeft);
        m.list.enableAutoSizing = true;
        m.list.fontSizeMin = 13f;
        m.list.fontSizeMax = 19f;
        m.list.lineSpacing = 4f;
        UIKit.Forget(m.list);
        TMP_Text how = UIKit.Text(box, "contract.run.how", 14f, new Color(0.62f, 0.58f, 0.52f), new Vector2(0f, -H / 2f + 66f), new Vector2(W - 36f, 34f));
        how.fontSizeMin = 11f;
        m.reroll = UIKit.MakeButton(box, "", new Vector2(-W / 2f + 112f, -H / 2f + 28f), new Vector2(200f, 44f), m.DoReroll, 18f);
        m.board = UIKit.MakeButton(box, "board.open", new Vector2(W / 2f - 112f, -H / 2f + 28f), new Vector2(200f, 44f), () => ContractBoard.Open(root), 18f);
        m.badge = UIKit.Text(box, "", 16f, new Color(0.96f, 0.83f, 0.47f), new Vector2(W / 2f - 80f, H / 2f - 28f), new Vector2(150f, 30f), TextAlignmentOptions.Right);
        UIKit.Forget(m.badge);
        if (ContractBoard.Claimable > 0) m.board.gameObject.AddComponent<NewGlow>();
        ContractBoard.Changed += m.Write;
        m.Write();
        box.gameObject.AddComponent<LocRefresh>().refresh = m.Write;
    }

    void OnDestroy() => ContractBoard.Changed -= Write;

    void DoReroll()
    {
        if (!Contracts.Reroll(GameMode.Current == Difficulty.Endless, CharacterData.Selected))
        {
            Notice.Toast(NoticeIcon.Info, Loc.T("contract.reroll.none"), 3f);
            return;
        }
        Hostile.Play("pop", 0.6f, 1.2f);
        Write();
    }

    void Update()
    {
        check -= Time.unscaledDeltaTime;
        if (check > 0f) return;
        check = 0.5f;
        if (Key() != shownFor) Write();
    }

    string Key() => (int)GameMode.Current + "|" + (int)CharacterData.Selected;

    void Write()
    {
        if (this == null) return;
        shownFor = Key();
        bool endless = GameMode.Current == Difficulty.Endless;
        List<Contracts.Active> now = Contracts.Preview(endless, false, CharacterData.Selected);
        Prefs.Save();
        System.Text.StringBuilder b = new System.Text.StringBuilder();
        foreach (Contracts.Active a in now)
        {
            b.Append("<color=#F5D478><size=80%>").Append(Contracts.KindTag(a.def));
            if (a.carried) b.Append("  ·  <color=#C9A2FF>").Append(Loc.T("contract.carried.tag")).Append("</color>");
            b.Append("</size></color>\n").Append(a.def.Name).Append("  <size=82%><color=#A89C86>").Append(a.def.Cond).Append("</color></size>\n");
        }
        list.text = b.ToString().TrimEnd('\n');
        TMP_Text rt = reroll.GetComponentInChildren<TMP_Text>();
        if (rt != null) { UIKit.Forget(rt); rt.text = Loc.T("contract.reroll").Replace("{0}", Contracts.RerollsLeft.ToString()); }
        reroll.interactable = Contracts.RerollsLeft > 0;
        int claim = ContractBoard.Claimable;
        badge.text = claim > 0 ? Loc.T("claim.badge").Replace("{0}", claim.ToString()) : "";
        board.interactable = true;
        if (Demo.On)
        {
            TMP_Text bt = board.GetComponentInChildren<TMP_Text>();
            if (bt != null) bt.color = new Color(0.6f, 0.58f, 0.6f);
        }
    }
}
