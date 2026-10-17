using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // 저장된 판이 있으면 한 번 묻고 새 판 (2.1.9 판 중간 저장: 새 판을 시작하면 저장된 판은 지워짐)
    GameObject confirm;

    // 1.0.4~: 튜토리얼을 한 번도 안 해 본 첫 플레이어에게 처음 한 번 튜토리얼을 권함
    const string TutorialAskedKey = "tutorial.play.asked";

    public void OnPressStart()
    {
        if (confirm != null) return;
        if (!RunSave.Has)
        {
            if (ShouldSuggestTutorial()) { SuggestTutorial(); return; }
            SceneFlow.Go("GameScene");
            return;
        }
        RectTransform win = Window("ConfirmNewRun", new Vector2(820f, 340f));
        UIKit.Text(win, "새 게임을 시작할까요?", 38f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 78f), new Vector2(760f, 60f));
        UIKit.Text(win, "저장된 판(이어하기)은 지워집니다.", 26f, new Color(0.92f, 0.88f, 0.8f), new Vector2(0f, 20f), new Vector2(760f, 40f));
        UnityEngine.UI.Button ok = UIKit.MakeButton(win, "새 게임", new Vector2(-150f, -90f), new Vector2(260f, 76f), () =>
        {
            RunSave.Delete();
            SceneFlow.Go("GameScene");
        }, 28f);
        UnityEngine.UI.Button cancel = UIKit.MakeButton(win, "취소", new Vector2(150f, -90f), new Vector2(260f, 76f), CloseConfirm, 28f);
        UIKit.ConfirmKeys(win, ok, cancel, -143f, false);        // 저장된 판이 지워지므로 Enter 한 번에 확정되지 않게 기본 선택은 하지 않음 (게임 종료와 같게)
    }

    // 1.0.4~: 튜토리얼 기록이 없고 한 판도 해 본 적 없으면 (체험판 포함) · 한 번 고르면 다시 묻지 않음
    bool ShouldSuggestTutorial() =>
        !TutorialRun.Seen && Prefs.GetInt(TutorialAskedKey, 0) == 0 && !PatchNotesUI.PlayedBefore
        && !GameInput.Auto && !Application.isBatchMode;

    void SuggestTutorial()
    {
        RectTransform win = Window("SuggestTutorial", new Vector2(900f, 380f));
        UIKit.Text(win, "튜토리얼부터 해 볼까요?", 38f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 105f), new Vector2(840f, 60f));
        UIKit.Text(win, "실제 게임 화면에서 이동 · 공격 · 재장전 · 필살기 · 레벨업을 하나씩 직접 해 봅니다. 언제든 그만둘 수 있습니다.",
                   24f, new Color(0.92f, 0.88f, 0.8f), new Vector2(0f, 20f), new Vector2(820f, 90f));
        UnityEngine.UI.Button tut = UIKit.MakeButton(win, "튜토리얼", new Vector2(-165f, -110f), new Vector2(290f, 80f), () =>
        {
            Asked();
            TutorialRun.Begin();
        }, 28f);
        UnityEngine.UI.Button now = UIKit.MakeButton(win, "바로 시작", new Vector2(165f, -110f), new Vector2(290f, 80f), () =>
        {
            Asked();
            SceneFlow.Go("GameScene");
        }, 28f);
        UIKit.ConfirmKeys(win, tut, now, -164f);
    }

    static void Asked()
    {
        Prefs.SetInt(TutorialAskedKey, 1);
        Prefs.Save();
    }

    // 1.0.4~: 게임 종료도 한 번 묻고 (설정 칸 바로 옆이라 잘못 누르기 쉬움)
    public void OnPressExit()
    {
        if (confirm != null) return;
        RectTransform win = Window("ConfirmQuit", new Vector2(760f, 300f));
        UIKit.Text(win, "게임을 종료할까요?", 38f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 60f), new Vector2(700f, 60f));
        UnityEngine.UI.Button quit = UIKit.MakeButton(win, "게임 종료", new Vector2(-150f, -70f), new Vector2(260f, 76f), () =>
        {
            Prefs.Save();
            Application.Quit();
        }, 28f);
        UnityEngine.UI.Button cancel = UIKit.MakeButton(win, "취소", new Vector2(150f, -70f), new Vector2(260f, 76f), CloseConfirm, 28f);
        UIKit.ConfirmKeys(win, quit, cancel, 0f, false);         // 1.1.9~ Enter 한 번에 게임이 꺼지지 않게 기본 선택은 하지 않음 (←→ 이동만)
    }

    // 확인 창 하나 (ESC = 취소 · 1.0.4~ 예전엔 ESC 로 안 닫혔음)
    RectTransform Window(string name, Vector2 size)
    {
        GameObject start = GameObject.Find("GameStartButton");
        Transform root = start != null ? start.transform.root : transform.root;
        UIKit.EnsureStyle();
        RectTransform win = UIKit.Modal(root, name, size, out confirm);
        UIKit.EscCancel(confirm, CloseConfirm);
        return win;
    }

    void CloseConfirm()
    {
        if (confirm != null) Destroy(confirm);
        confirm = null;
    }
}
