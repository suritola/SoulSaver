using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.2~ 메인 메뉴 「이번 판 계약」 세 줄 (MenuExtras.InstallMainMenu 가 부름)
//   오른쪽 아래 빈자리(오른쪽 위 버튼 열 아래끝 -266 아래 · 아이콘 칸 x 370 오른쪽) — 왼쪽은 영웅 그림이라 오른쪽에 둠
//   1.4.8~ 위아래 끝을 왼쪽 영웅 패널과 같게 (y -395 ~ 35) · 오른쪽 위 버튼 열(1080p 아래끝 y 186)과는 떨어져 있음
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
        // 1.4.8~ 글씨가 13 ~ 19 로 작고 줄이 많았음 → 본문 20 ~ 24 · 계약마다 두 줄(이름 + 종류 / 조건) · 위아래 끝을 왼쪽 영웅 패널과 같게
        //   (게임 시작 위끝 ~ 아이콘 칸 아래끝, MenuExtras.SideTop · SideBottom) · 안쪽 여백 24 · 「판마다 세 가지를…」 설명은 패널에 마우스를 올리면
        const float H = MenuExtras.SideTop - MenuExtras.SideBottom, Pad = 24f;
        RectTransform box = UIKit.Rect("MenuContracts", parent, Vector2.zero, new Vector2(BoxW, H));
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(1f, 0.5f);
        box.anchoredPosition = new Vector2(-RightMargin, (MenuExtras.SideTop + MenuExtras.SideBottom) * 0.5f);
        Image bg = box.gameObject.AddComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.08f, 0.06f, 0.11f, 0.82f);
        TooltipTrigger tip = box.gameObject.AddComponent<TooltipTrigger>();
        tip.titleProvider = () => Loc.T("contract.run.title");
        tip.bodyProvider = () => Loc.T("contract.run.how");
        MenuContracts m = box.gameObject.AddComponent<MenuContracts>();
        const float HeadH = 40f, BtnH = 52f;
        float inner = BoxW - Pad * 2f;
        m.head = UIKit.Text(box, "contract.run.title", 30f, new Color(0.79f, 0.63f, 1f), new Vector2(-inner * 0.5f + inner * 0.3f, H / 2f - Pad - HeadH / 2f), new Vector2(inner * 0.6f, HeadH), TextAlignmentOptions.Left);
        m.head.fontSizeMin = 22f;
        m.badge = UIKit.Text(box, "", 20f, new Color(0.96f, 0.83f, 0.47f), new Vector2(inner * 0.5f - inner * 0.2f, H / 2f - Pad - HeadH / 2f), new Vector2(inner * 0.4f, HeadH), TextAlignmentOptions.Right);
        m.badge.fontSizeMin = 20f;
        UIKit.Forget(m.badge);
        // 목록: 머리 아래 12 ~ 버튼 위 12
        float listTop = H / 2f - Pad - HeadH - 12f, listBottom = -H / 2f + Pad + BtnH + 12f;
        m.list = UIKit.Text(box, "", 24f, new Color(0.93f, 0.9f, 0.84f), new Vector2(0f, (listTop + listBottom) * 0.5f), new Vector2(inner, listTop - listBottom), TextAlignmentOptions.TopLeft);
        m.list.enableAutoSizing = true;
        m.list.fontSizeMin = 20f;
        m.list.fontSizeMax = 24f;
        m.list.lineSpacing = 0f;
        UIKit.Forget(m.list);
        float bw = (inner - 12f) * 0.5f, by = -H / 2f + Pad + BtnH / 2f;
        m.reroll = UIKit.MakeButton(box, "", new Vector2(-(bw + 12f) * 0.5f, by), new Vector2(bw, BtnH), m.DoReroll, 22f);
        m.board = UIKit.MakeButton(box, "board.open", new Vector2((bw + 12f) * 0.5f, by), new Vector2(bw, BtnH), () => ContractBoard.Open(root), 22f);
        foreach (Button bt in new[] { m.reroll, m.board })
        {
            TMP_Text l = bt.GetComponentInChildren<TMP_Text>(true);
            if (l != null) l.fontSizeMin = 20f;
        }
        if (ContractBoard.Claimable > 0) m.board.gameObject.AddComponent<NewGlow>();
        ContractBoard.Changed += m.Write;
        m.box = box;
        m.Fit();
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
        Fit();
        if (Key() != shownFor) Write();
    }

    // UI 크기 110 · 120% 처럼 캔버스가 좁으면 상자를 줄여 아이콘 칸(가운데 기준 오른쪽 끝 x 370)과 겹치지 않게 (오른쪽 끝 기준으로 줄어듦)
    // 1.4.8~ 폭 470 → 440 (왼쪽 영웅 패널과 같게) · 넉넉하면 오른쪽 여백을 늘려 아이콘 칸과 사이를 영웅 패널 쪽과 같은 17 로 (1080p 에서 x 390 ~ 830, 영웅 패널 -830 ~ -390 과 좌우 대칭)
    RectTransform box;
    const float GridGap = 17f, RightMargin = 30f, BoxW = 440f, MaxMargin = 130f;
    void Fit()
    {
        if (box == null || !(box.parent is RectTransform p)) return;
        float half = p.rect.width * 0.5f, gridRight = MenuExtras.ColRight;
        float margin = Mathf.Clamp(half - gridRight - GridGap - BoxW, RightMargin, MaxMargin);
        float room = half - margin - gridRight - GridGap;
        float k = Mathf.Clamp(room / BoxW, 0.6f, 1f);
        if (!Mathf.Approximately(box.localScale.x, k)) box.localScale = new Vector3(k, k, 1f);
        if (!Mathf.Approximately(box.anchoredPosition.x, -margin)) box.anchoredPosition = new Vector2(-margin, box.anchoredPosition.y);
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
        // 1.4.8~ 계약마다 두 줄: 「이름  종류 · 포인트」(이름 금빛 · 종류 흐리게) / 조건 — 모두 같은 크기(20 아래로 줄지 않게), 계약 사이는 반 줄 띄움
        bool first = true;
        foreach (Contracts.Active a in now)
        {
            if (!first) b.Append("\n<size=40%> </size>\n");
            first = false;
            b.Append("<color=#F5D478>").Append(a.def.Name).Append("</color>  <color=#A89C86>").Append(Contracts.KindTag(a.def));
            if (a.carried) b.Append("  ·  <color=#C9A2FF>").Append(Loc.T("contract.carried.tag")).Append("</color>");
            b.Append("</color>\n").Append(a.def.Cond);
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
