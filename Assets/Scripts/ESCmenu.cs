using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ESCmenu : MonoBehaviour
{
    public GameObject escMenu;
    public GameObject shopPanel;
    private bool isEscOpen = false;
    private bool isShopOpen = false;
    // 열기 전 시간 배율 (레벨업 창 위에서 열었다 닫아도 그대로 멈춰 있도록)
    // 1.0.1: 상점 카운트다운이 일시정지 메뉴가 열린 동안 끝나면 여기를 1로 (닫을 때 멈춘 채 남지 않게)
    static float timeScaleBeforeOpen = 1f;
    public static void ResumeAt(float scale) => timeScaleBeforeOpen = scale;
    // Start is called before the first frame update
    void Start()
    {
        shop = FindFirstObjectByType<Shop>();
        // 1.1.9~ 무한 모드 · 일일 도전: 「메인 메뉴로」 → 「포기하고 결과 보기」 (씬 버튼 글자를 덮어씀 · 언어를 바꿔도 이 글자로)
        if (GiveUpMode && escMenu != null)
            foreach (UnityEngine.UI.Button b in escMenu.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                bool mainMenu = false;
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                    if (b.onClick.GetPersistentMethodName(i) == nameof(onPressMainMenu)) mainMenu = true;
                TMPro.TMP_Text t = mainMenu ? b.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
                if (t == null) continue;
                UIKit.Remember(t, "포기하고 결과 보기");
                t.text = Loc.T("포기하고 결과 보기");
                t.enableAutoSizing = true;          // 영어 · 일본어는 「메인 메뉴로」보다 길어 버튼을 넘지 않게
                t.fontSizeMax = t.fontSize;
                t.fontSizeMin = Mathf.Min(t.fontSize, 18f);
            }
    }

    // Update is called once per frame
    Shop shop;
    void Update()
    {
        isShopOpen = shop.isShopOpen;
        if (!GameInput.EscDown) return;

        // 설정 창이 열려 있으면 설정 창이 ESC를 처리함 (키 입력 취소 · 창 닫기), 연출 중에는 ESC가 건너뛰기
        if (SettingsUI.IsOpen || SettingsUI.EscHandledFrame == Time.frameCount || StoryDirector.Playing) return;
        // 영혼 트리는 ESC로 닫히고, 무기 진화는 고를 때까지 멈춤
        if (SoulTreeUI.IsOpen || WeaponEvolutionUI.Open || ChoiceUI.Open || SkillEvolutionUI.Open) return;     // 고르기 창(보물 상자 · 보스 보상 · 저주 제단)도 고를 때까지 멈춤 · 1.1~ 스킬 진화 연출 중에도 (메뉴가 연출 뒤에 깔렸음)
        // 메인 메뉴 확인 창이 떠 있으면 ESC 는 그 창만 닫음
        if (confirm != null) { CloseConfirm(); return; }
        // 1.0.1: 상점을 닫은 뒤 1초 카운트다운 동안은 ESC 를 받지 않음
        // (이때 일시정지 메뉴를 열면 「멈춘 상태」를 기억했다가 닫을 때 그대로 되돌려 게임이 영영 멈췄음)
        if (shop != null && shop.isPause) return;
        if (isShopOpen)
        {
            shop.isShopOpen = false;
            StartCoroutine(shop.StartGameCountdown());
            shopPanel.SetActive(false);
        }
        else ToggleEsc();
    }

    // 창이 포커스를 잃으면 (Alt+Tab 등) 일시정지 메뉴를 열어 둠
    void OnApplicationFocus(bool focus) { if (!focus) PauseFromOutside(); }

    // 1.1.9~ 바깥 요인(포커스 잃음 · 스팀 오버레이 · 게임패드 분리)으로 멈춰야 할 때 — 이미 멈춘 화면 · 연출 중이면 그대로
    public void PauseFromOutside()
    {
        if (isEscOpen || GameInput.Auto || Application.isBatchMode || StoryDirector.Playing) return;
        if (Time.timeScale == 0f || (shop != null && (shop.isShopOpen || shop.isPause))) return;
        if (Hostile.Player != null && Hostile.Player.IsDying) return;      // 쓰러지는 연출 중엔 결과 화면으로 그대로
        ToggleEsc();
    }

    // 게임 씬이 아니면 (메인 메뉴 · 결과 화면) ESCmenu 가 없어 아무 일 없음
    public static void RequestPause() => RequestPause(null);

    // reasonKo: 왜 멈췄는지 일시정지 화면 위에 한 줄 (토스트는 멈춘 동안 숨겨져 보이지 않음) · 메뉴를 열었으면 true
    public static bool RequestPause(string reasonKo)
    {
        ESCmenu m = FindFirstObjectByType<ESCmenu>();
        if (m == null) return false;
        m.PauseFromOutside();
        if (!m.isEscOpen) return false;
        if (!string.IsNullOrEmpty(reasonKo)) m.ShowReason(reasonKo);
        return true;
    }

    // 1.1.9~ 바깥 요인으로 멈춘 이유 (화면 위 가운데 띠 · 메뉴를 닫으면 사라짐)
    GameObject reasonBanner;

    void ShowReason(string ko)
    {
        HideReason();
        Transform root = escMenu != null ? escMenu.transform.root : transform.root;
        RectTransform r = UIKit.Rect("PauseReason", root, Vector2.zero, new Vector2(760f, 60f));
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -120f);
        SoulTreeUI.OnTop(r.gameObject, 610);              // 일시정지 메뉴 위 · 확인 창(620) 아래
        UnityEngine.UI.Image bg = r.gameObject.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.25f, 0.1f, 0.05f, 0.92f);
        bg.raycastTarget = false;
        UIKit.Text(r, ko, 30f, new Color(1f, 0.75f, 0.45f), Vector2.zero, new Vector2(720f, 52f));
        reasonBanner = r.gameObject;
    }

    void HideReason()
    {
        if (reasonBanner != null) Destroy(reasonBanner);
        reasonBanner = null;
    }

    void OnDestroy() => IsOpen = false;

    // 일시정지 메뉴 첫 버튼: 다시 시작 대신 계속하기 (ESC 를 다시 누른 것과 같음)
    public void onPressResume()
    {
        if (isEscOpen) ToggleEsc();
    }
    // 메인 메뉴로: 진행 중인 판이 끝나므로 한 번 더 묻기
    GameObject confirm;

    public void onPressMainMenu()
    {
        // 1.1.9~ 무한 모드 · 일일 도전: 메인 메뉴로 바로 가면 결과 화면(생존 시간 · 순위 · 획득 포인트)을 못 봤음 → 포기하고 결과 화면으로
        if (GiveUpMode)
        {
            Confirm("ConfirmGiveUp", "여기서 포기하고 결과를 볼까요?", "지금까지의 생존 시간 · 보스 처치 수가 기록되고 순위표에 올라갑니다.", "포기하고 결과 보기", GiveUp);
            return;
        }
        // 2.1.9: 판이 저장되어 메인 메뉴 「이어하기」로 계속할 수 있음 (저장하지 않는 판은 예전 안내)
        Confirm("ConfirmMainMenu", "정말로 메인 메뉴로 가시겠습니까?", SaveNote(), "메인 메뉴로", GoMainMenu);
    }

    // 1.0.4~: 「다시 시작」 — 지금 판을 버리고 같은 캐릭터 · 난이도로 새 판 (튜토리얼이면 튜토리얼을 처음부터)
    public void onPressRestart()
    {
        Confirm("ConfirmRestart", "정말로 다시 시작하시겠습니까?", "지금 판을 버리고 같은 캐릭터 · 난이도로 새로 시작합니다.", "다시 시작", Restart);
    }

    // 1.0.4~: 「게임 종료」 — 저장되는 판이면 저장하고 끔
    public void onPressQuit()
    {
        Confirm("ConfirmQuit", "정말로 게임을 종료하시겠습니까?", SaveNote(), "게임 종료", QuitGame);
    }

    // 나가면 지금 판이 어떻게 되는지 (1.0.4~: 쓰러지는 중 · 마지막 보스를 쓰러뜨린 뒤는 「저장됨」이라고 하지 않음)
    static string SaveNote()
    {
        if (RunSave.CanSaveNow) return "지금 판은 저장되어 메인 메뉴의 「이어하기」로 계속할 수 있습니다.";
        if (RunSave.FinalBossDown) return "클리어 기록은 이미 저장되었습니다.";
        return "지금 진행 중인 판이 끝납니다.";
    }

    // 한 번 더 묻는 창 (메인 메뉴로 · 다시 시작 · 게임 종료가 같이 씀)
    void Confirm(string name, string question, string note, string okLabel, UnityEngine.Events.UnityAction ok)
    {
        if (confirm != null) return;
        Transform root = escMenu != null ? escMenu.transform.root : transform.root;
        RectTransform win = UIKit.Modal(root, name, new Vector2(780f, 340f), out confirm);
        SoulTreeUI.OnTop(confirm, 620);
        UIKit.Text(win, question, 38f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 78f), new Vector2(720f, 60f));
        // 안내는 두 줄까지 (포기 안내처럼 긴 영어 · 일본어 문구가 한 줄 높이에 맞추느라 아주 작아지지 않게) — 제목(48~) · 버튼(~-52) 사이
        UIKit.Text(win, note, 26f, new Color(0.92f, 0.88f, 0.8f), new Vector2(0f, 14f), new Vector2(720f, 66f));
        UIKit.MakeButton(win, okLabel, new Vector2(-150f, -90f), new Vector2(260f, 76f), ok, 28f);
        UIKit.MakeButton(win, "취소", new Vector2(150f, -90f), new Vector2(260f, 76f), CloseConfirm, 28f);
    }

    void CloseConfirm()
    {
        if (confirm != null) Destroy(confirm);
        confirm = null;
    }

    void GoMainMenu()
    {
        CloseConfirm();
        RunSave.Save();             // 판 중간 저장 (2.1.9)
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1f;
    }

    static bool GiveUpMode => GameMode.IsEndless || DailyChallenge.Active;

    // 결과 화면으로 (EndlessMode.OnDestroy 가 생존 시간 · 최고 기록 · 순위표 제출, 결과 화면이 그대로 보여 줌)
    void GiveUp()
    {
        CloseConfirm();
        RunStats.GaveUp = true;
        RunSave.Abandon();          // 무한 모드 · 일일 도전은 저장하지 않으므로 따로 남은 이야기 판 저장(「이어하기」)은 지우지 않음
        Time.timeScale = 1f;
        IsOpen = false;
        SceneManager.LoadScene("GameOver");
    }

    void Restart()
    {
        CloseConfirm();
        RunSave.Abandon();          // 저장해 둔 이 판은 지움 (메인 메뉴 「이어하기」에 옛 판이 남지 않게)
        Time.timeScale = 1f;
        IsOpen = false;
        SceneManager.LoadScene("GameScene");
    }

    void QuitGame()
    {
        CloseConfirm();
        RunSave.Save();             // OnApplicationQuit 에서도 저장하지만, 에디터 · 끄기 실패에 대비해 먼저
        Application.Quit();
    }

    public static bool IsOpen { get; private set; }
    // 레벨업 카드 창이 일시정지 창을 가려 버튼을 못 누르던 문제: 일시정지 동안 카드 창을 숨겼다가 닫으면 다시 띄움
    LevelShop hiddenLevelShop;

    void ToggleEsc()
    {
        CloseConfirm();
        HideReason();
        isEscOpen = !isEscOpen;
        IsOpen = isEscOpen;

        if (isEscOpen)
        {
            LevelShop ls = FindFirstObjectByType<LevelShop>();
            if (ls != null && ls.IsOpen)
            {
                ls.LvshopPanel.SetActive(false);
                hiddenLevelShop = ls;
            }
        }
        else if (hiddenLevelShop != null)
        {
            hiddenLevelShop.LvshopPanel.SetActive(true);
            hiddenLevelShop = null;
        }

        if (escMenu != null) escMenu.SetActive(isEscOpen);
        RunStats.ShowSummary(isEscOpen);

        if (isEscOpen)
        {
            // 필살기 조준 중(시간이 느려진 상태)이면 조준을 취소하고 멈춤 (게이지는 그대로)
            PlayerController player = Hostile.Player;
            if (player != null) player.CancelSkill();
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            // 1.0.1: 되돌릴 값이 「멈춤」인데 실제로 멈춰 둘 창(상점 · 레벨업 · 고르기 · 영혼 트리 · 진화 · 연출)이 없으면 다시 흐르게 (어떤 길로든 영영 멈추지 않게)
            float back = timeScaleBeforeOpen;
            if (back <= 0f && !SomethingHoldsPause()) back = 1f;
            Time.timeScale = back;
        }
    }

    bool SomethingHoldsPause()
    {
        LevelShop ls = FindFirstObjectByType<LevelShop>();
        return (ls != null && ls.IsOpen) || (shop != null && (shop.isShopOpen || shop.isPause)) || ChoiceUI.Open || SoulTreeUI.IsOpen
               || WeaponEvolutionUI.Open || SkillEvolutionUI.Open || StoryDirector.Playing || BossUltimate.CutscenePlaying;
    }
}
