using System.Collections;
using UnityEngine;

// 1.2.7~ 무한 모드 · 일일 도전 중에 게임을 끄면 (ESC 「게임 종료」 · 창 닫기 · Alt+F4 · 스팀에서 끄기)
// 이어하기로 남기지 않고 그 자리에서 판을 끝내 생존 시간 · 보스 처치 수를 순위표에 올린 뒤 끈다.
// 순위표 올리기는 스팀 응답을 기다려야 해서 바로 끄면 사라졌음 → 다 올라가거나 최대 WaitMax 초가 지나면 끔.
// (메인 메뉴로는 이미 「포기하고 결과 보기」 — ESCmenu.GiveUp → EndlessMode.OnDestroy 가 올림)
public static class EndlessQuit
{
    const float WaitMax = 4f;
    static bool quitting, allowQuit;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Application.wantsToQuit -= WantsToQuit;
        Application.wantsToQuit += WantsToQuit;
    }

    // 끄려는 순간 진행 중인 무한 모드 · 일일 도전 판이 있으면 먼저 끝내고 올린 뒤 다시 끔
    static bool WantsToQuit()
    {
        if (allowQuit || !RunActive) return true;
        Quit();
        return false;
    }

    static bool RunActive => EndlessMode.Instance != null && !EndlessMode.Ended && !GameInput.Auto && !GameInput.TrailerRunning && !Application.isBatchMode;

    // ESC 「게임 종료」 (무한 모드 · 일일 도전)
    public static void Quit()
    {
        if (quitting) return;
        quitting = true;
        if (RunActive)
        {
            RunStats.GaveUp = true;             // 밸런스 기록에 「포기」로 (포기하고 결과 보기와 같음)
            RunSave.Abandon();                  // 이어하기 저장을 지움 — 판을 끝내기 전에 (끝난 뒤에는 RunSave 가 저장 대상이 아니라고 보고 지우지 않음)
            EndlessMode.EndRunNow();            // 기록 · 최고 기록 · 순위표 제출 (한 번만, 씬이 사라질 때 다시 올리지 않음)
        }
        new GameObject("EndlessQuit").AddComponent<Waiter>();
    }

    class Waiter : MonoBehaviour
    {
        void Start()
        {
            DontDestroyOnLoad(gameObject);
            if (SteamLeaderboards.Pending > 0) ShowNote();
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            // 일시정지(시간 0) 중에도 흐르게 실제 시간으로 · 스팀 콜백은 SteamManager 가 프레임마다 돌림
            float until = Time.unscaledTime + WaitMax;
            while (SteamLeaderboards.Pending > 0 && Time.unscaledTime < until) yield return null;
            allowQuit = true;
#if UNITY_EDITOR
            quitting = false;                   // 에디터는 Application.Quit 이 무시되므로 다음 시험을 위해 풀어 둠
            allowQuit = false;
            Destroy(gameObject);
#endif
            Application.Quit();
        }

        // 기다리는 동안 화면 가운데 아래에 「순위표에 기록을 올리는 중…」 (멈춘 줄 알지 않게)
        void ShowNote()
        {
            Canvas canvas = UIKit.HudCanvas();
            if (canvas == null) return;
            TMPro.TMP_Text t = UIKit.Text(canvas.transform, "순위표에 기록을 올리는 중…", 30f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, -220f), new Vector2(900f, 50f));
            SoulTreeUI.OnTop(t.gameObject, 900);
        }
    }
}
