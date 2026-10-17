#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX) && !DISABLESTEAMWORKS
#define STEAM
#endif

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if STEAM
using Steamworks;
#endif

// 체험판 (1.9.2~): 빌드할 때 -demo 를 주면 SOULSAVER_DEMO 가 켜짐 (BuildScript, tools/steam-upload.ps1 -Demo)
//   1장 초원 + 킹 슬라임까지 (정식판과 같은 첫 장 · 예전엔 지하 묘역 + 리치 왕) · 거너 · 검사 · 유령 · 쉬움 난이도 · 장 중반 이벤트(습격 · 엘리트 · 보물 상자)는 정식판과 같음
//   킹 슬라임을 쓰러뜨리면 (거너는 무기 진화까지 보여 준 뒤) 「정식판에서 계속」 화면과 상점 페이지 버튼 (1.0.4~, 예전엔 위시리스트)
//   제품 이름이 Soul Saver Demo 라 저장 데이터는 정식판과 따로
public static class Demo
{
#if SOULSAVER_DEMO
    public static bool On => true;
#else
    public static bool On => false;
#endif

    public static string StoreUrl => "https://store.steampowered.com/app/" + SteamManager.FullAppId;

    // 체험판에서 고를 수 있는 캐릭터 (유령: 「오래전 이 초원에서 죽은 유령」이라 초원 체험판과 이야기가 맞음)
    public static bool CharacterAllowed(CharacterId id) => !On || id == CharacterId.Gunner || id == CharacterId.Swordsman || id == CharacterId.Ghost;

    // 정식판 상점 페이지 (스팀 오버레이가 되면 오버레이, 아니면 브라우저)
    public static void OpenStore()
    {
#if STEAM
        if (SteamManager.Initialized && SteamUtils.IsOverlayEnabled())
        {
            SteamFriends.ActivateGameOverlayToStore(new AppId_t(SteamManager.FullAppId), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
            return;
        }
#endif
        Application.OpenURL(StoreUrl);
    }

    // ================================================================= 체험판 끝 화면
    static GameObject endPanel;

    public static void ShowEnd()
    {
        if (endPanel != null) return;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        PlayerController p = Hostile.Player;
        if (p != null) p.CancelSkill();
        Time.timeScale = 0f;

        RectTransform win = UIKit.Modal(canvas.transform, "DemoEnd", new Vector2(1100f, 700f), out endPanel);
        Color gold = new Color(0.96f, 0.83f, 0.47f), parch = new Color(0.93f, 0.9f, 0.84f);
        UIKit.Text(win, "체험판은 여기까지입니다", 54f, gold, new Vector2(0f, 255f), new Vector2(980f, 80f));
        // 차별점 구현 3-4: 체험판이 초원 · 킹 슬라임에서 끝남 → 새 키 (LocTable.DemoMeadow.cs · 옛 리치 왕 문구는 죽은 줄로 남김)
        UIKit.Text(win, "demo.end.thanks", 30f, parch, new Vector2(0f, 180f), new Vector2(980f, 50f));
        TMP_Text full = UIKit.Text(win, "demo.end.full", 26f, parch, new Vector2(0f, 85f), new Vector2(1000f, 120f));
        full.enableAutoSizing = true;       // 1.0.3: 정식판 소개가 길어져 (영어는 더 긺) 창 안에 맞춤
        full.fontSizeMin = 16f;
        full.fontSizeMax = 26f;
        UIKit.Text(win, "demo.end.heroes", 26f, gold, new Vector2(0f, -5f), new Vector2(1000f, 42f));
        // 1.0.4~: 이미 파는 게임이라 「출시할 때 알려 드립니다」 대신 상점 안내 · 체험판 기록이 정식판으로 이어지지 않는다는 말 (저장 폴더가 따로)
        UIKit.Text(win, "정식판은 스팀 상점 페이지에서 만날 수 있습니다.", 26f, new Color(0.6f, 0.9f, 1f), new Vector2(0f, -60f), new Vector2(980f, 44f));
        UIKit.Text(win, "체험판 기록(포인트 · 해금)은 정식판으로 이어지지 않습니다.", 22f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, -108f), new Vector2(980f, 36f));

        Button wish = UIKit.MakeButton(win, "상점 페이지 보기", new Vector2(-200f, -210f), new Vector2(360f, 90f), OpenStore, 30f);
        wish.GetComponent<Image>().color = new Color(1f, 0.85f, 0.45f);
        UIKit.MakeButton(win, "메인 메뉴로", new Vector2(200f, -210f), new Vector2(300f, 90f), () =>
        {
            SceneFlow.Go("MainMenu");
        }, 30f);
        Hostile.Play("levelup", 0.8f);
    }

    // 메인 메뉴 버전 글 옆 표시
    public static string Label => On ? "  " + Loc.T("체험판") : "";
}
