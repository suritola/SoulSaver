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
    // 1.0.7: 상점 카운트다운이 일시정지 메뉴가 열린 동안 끝나면 여기를 1로 (닫을 때 멈춘 채 남지 않게)
    static float timeScaleBeforeOpen = 1f;
    public static void ResumeAt(float scale) => timeScaleBeforeOpen = scale;
    // Start is called before the first frame update
    void Start()
    {
        shop = FindFirstObjectByType<Shop>();
    }

    // Update is called once per frame
    Shop shop;
    void Update()
    {
        isShopOpen = shop.isShopOpen;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // 설정 창이 열려 있으면 설정 창이 ESC를 처리함 (키 입력 취소 · 창 닫기), 연출 중에는 ESC가 건너뛰기
        if (SettingsUI.IsOpen || SettingsUI.EscHandledFrame == Time.frameCount || StoryDirector.Playing) return;
        // 영혼 트리는 ESC로 닫히고, 무기 진화는 고를 때까지 멈춤
        if (SoulTreeUI.IsOpen || WeaponEvolutionUI.Open || ChoiceUI.Open) return;     // 고르기 창(보물 상자 · 보스 보상 · 저주 제단)도 고를 때까지 멈춤
        // 메인 메뉴 확인 창이 떠 있으면 ESC 는 그 창만 닫음
        if (confirm != null) { CloseConfirm(); return; }
        // 1.0.7: 상점을 닫은 뒤 1초 카운트다운 동안은 ESC 를 받지 않음
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
    void OnApplicationFocus(bool focus)
    {
        if (focus || isEscOpen || GameInput.Auto || Application.isBatchMode || StoryDirector.Playing) return;
        if (Time.timeScale == 0f || (shop != null && shop.isShopOpen)) return;
        ToggleEsc();
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
        if (confirm != null) return;
        Transform root = escMenu != null ? escMenu.transform.root : transform.root;
        RectTransform win = UIKit.Modal(root, "ConfirmMainMenu", new Vector2(780f, 340f), out confirm);
        SoulTreeUI.OnTop(confirm, 620);
        UIKit.Text(win, "정말로 메인 메뉴로 가시겠습니까?", 38f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 78f), new Vector2(720f, 60f));
        // 2.1.9: 판이 저장되어 메인 메뉴 「이어하기」로 계속할 수 있음 (저장하지 않는 판은 예전 안내)
        UIKit.Text(win, RunSave.CanSaveNow ? "지금 판은 저장되어 메인 메뉴의 「이어하기」로 계속할 수 있습니다." : "지금 진행 중인 판이 끝납니다.",
                   26f, new Color(0.92f, 0.88f, 0.8f), new Vector2(0f, 20f), new Vector2(720f, 40f));
        UIKit.MakeButton(win, "메인 메뉴로", new Vector2(-150f, -90f), new Vector2(260f, 76f), GoMainMenu, 28f);
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

    public static bool IsOpen { get; private set; }
    // 레벨업 카드 창이 일시정지 창을 가려 버튼을 못 누르던 문제: 일시정지 동안 카드 창을 숨겼다가 닫으면 다시 띄움
    LevelShop hiddenLevelShop;

    void ToggleEsc()
    {
        CloseConfirm();
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
            // 1.0.7: 되돌릴 값이 「멈춤」인데 실제로 멈춰 둘 창(상점 · 레벨업 · 고르기 · 영혼 트리 · 진화 · 연출)이 없으면 다시 흐르게 (어떤 길로든 영영 멈추지 않게)
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
