using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// UI 배치 확인용 스크린샷 (tools/uicheck/capture.ps1 로 프로젝트 복사본에서 배치 실행)
// 해상도는 CAPTURE_W · CAPTURE_H (16:9 · 4:3 등), 게임 언어는 한국어
public class UiCapture
{
    static readonly string Out = Environment.GetEnvironmentVariable("CAPTURE_OUT") ?? "C:/Temp/SoulSaverUi";
    static readonly int W = int.TryParse(Environment.GetEnvironmentVariable("CAPTURE_W"), out int w) ? w : 1920;
    static readonly int H = int.TryParse(Environment.GetEnvironmentVariable("CAPTURE_H"), out int h) ? h : 1080;
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    static Type T(string n) => Type.GetType(n + ", Assembly-CSharp");
    static object Get(object o, string f) => o.GetType().GetField(f, Any)?.GetValue(o);
    static void Set(object o, string f, object v) => o.GetType().GetField(f, Any).SetValue(o, v);
    // 같은 이름이 여럿이면 인자 수가 맞는 것 (선택 인자는 Type.Missing 으로 채움)
    static object Call(object o, string m, params object[] a)
    {
        foreach (MethodInfo mi in o.GetType().GetMethods(Any))
        {
            if (mi.Name != m) continue;
            ParameterInfo[] ps = mi.GetParameters();
            if (ps.Length < a.Length) continue;
            bool ok = true;
            for (int i = a.Length; i < ps.Length; i++) if (!ps[i].IsOptional) { ok = false; break; }
            if (!ok) continue;
            object[] full = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++) full[i] = i < a.Length ? a[i] : Type.Missing;
            return mi.Invoke(o, BindingFlags.OptionalParamBinding | Any, null, full, null);
        }
        throw new MissingMethodException(o.GetType().Name, m);
    }
    static void In(string f, object v) => T("GameInput").GetField(f).SetValue(null, v);
    static UnityEngine.Object Find(string type) => UnityEngine.Object.FindFirstObjectByType(T(type), FindObjectsInactive.Include);

    static RenderTexture rt;
    static Texture2D frame;

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    // 화면 캔버스를 카메라 캔버스로 바꿔 렌더 텍스처에 UI까지 함께 그림 (배치 모드에는 화면이 없음)
    static IEnumerator Shot(string name)
    {
        Camera cam = Camera.main;
        if (cam == null) { Debug.Log("[UICAP] 카메라 없음 " + name); yield break; }
        cam.targetTexture = rt;
        cam.cullingMask |= 1 << 5;
        SortingLayer[] layers = SortingLayer.layers;
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 5f;
                c.overrideSorting = true;
                c.sortingLayerName = layers[layers.Length - 1].name;
                c.sortingOrder = Mathf.Min(32000, 30000 + c.sortingOrder);
            }
            else if (!c.isRootCanvas && c.overrideSorting && c.sortingLayerName != layers[layers.Length - 1].name)
            {
                c.sortingLayerName = layers[layers.Length - 1].name;
                c.sortingOrder = Mathf.Min(32400, 31000 + c.sortingOrder);
            }
        yield return Frames(2);
        // 실제 화면은 LateUpdate 뒤에 그려짐 → 찍기 전에 선 두께 보정(PixelStrokeMin.LateUpdate)을 한 번 돌려 같은 모습으로
        Type psm = T("PixelStrokeMin");
        if (psm != null)
            foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(psm, FindObjectsSortMode.None))
                psm.GetMethod("LateUpdate", Any)?.Invoke(o, null);
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = rt;
        frame.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        frame.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(Out, name + ".png"), frame.EncodeToPNG());
        Debug.Log("[UICAP] " + name);
    }

    static IEnumerator Load(string hero, int stage)
    {
        Type cd = T("CharacterData"), cid = T("CharacterId");
        cd.GetField("Override", Any).SetValue(null, Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(cid), Enum.Parse(cid, hero)));
        SceneManager.LoadScene("GameScene");
        yield return Frames(3);
        // 시작 카드 창 닫기
        for (int i = 0; i < 6; i++)
        {
            yield return null;
            object shop = Find("LevelShop");
            if (shop != null && (bool)shop.GetType().GetProperty("IsOpen").GetValue(shop)) { Set(shop, "pendingSlot", -1); Call(shop, "onSelect", Get(shop, "first")); }
        }
        Time.timeScale = 1f;
        if (stage > 0) GoStage(stage);
        Tough();
        yield return Frames(20);
    }

    // 찍는 동안 쓰러지지 않게
    static void Tough()
    {
        object p = Find("PlayerController");
        p.GetType().GetField("PlayerMaxHealth").SetValue(p, 100000f);
        p.GetType().GetField("PlayerHealth").SetValue(p, 100000f);
    }

    static void GoStage(int stage)
    {
        object sm = Find("StageManager");
        foreach (Renderer r in (Renderer[])Get(sm, "caveRenderers")) if (r != null) r.enabled = stage == 0;
        ((GameObject)Get(sm, "hellMap"))?.SetActive(stage == 1);
        ((GameObject)Get(sm, "meadowMap"))?.SetActive(stage == 2);
        Camera.main.backgroundColor = (Color)Get(sm, stage == 1 ? "hellBackground" : "meadowBackground");
        sm.GetType().GetProperty("CurrentStage", Any).SetValue(sm, stage);
        object sp = Get(sm, "spawner");
        sp.GetType().GetMethod("StartStage").Invoke(sp, new object[] { stage });
        object bar = Find("bossbar");
        bar.GetType().GetField("bossSpawn").SetValue(bar, false);
        ((Transform)Get(sm, "player")).position = (Vector2)Get(sm, "stage2PlayerStart");
    }

    static void Hint(string key, string ko)
    {
        PlayerPrefs.DeleteKey("hint." + key);
        In("Auto", false);
        T("Hints").GetMethod("Show").Invoke(null, new object[] { key, ko });
        In("Auto", true);
    }

    // 1.4.8~ 전투 표시만 (CAPTURE_ONLY=fx): 검사 필살기 조준(우클릭 누르는 동안 고리) · 지옥의 군주 스킬(운석 비 예고 · 화염 파동)
    // 카드 창(입문 갈래 · 레벨업)이 늦게 열려도 닫힐 때까지 계속 첫 카드를 골라 닫음
    static IEnumerator CloseCards(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;
            object shop = Find("LevelShop");
            if (shop == null || !(bool)shop.GetType().GetProperty("IsOpen").GetValue(shop)) continue;
            Set(shop, "selectReady", true);
            Set(shop, "pendingSlot", -1);
            Call(shop, "onSelect", Get(shop, "first"));
        }
        Time.timeScale = 1f;
    }

    static IEnumerator CaptureFx()
    {
        yield return Load("Swordsman", 0);
        yield return CloseCards(90);
        Time.timeScale = 1f;
        object gauge = Find("SkillGauge");
        if (gauge != null) Call(gauge, "AddSkillPoint", 100000f);
        Component pc = (Component)UnityEngine.Object.FindFirstObjectByType(T("PlayerController"));
        Vector3 me = pc != null ? pc.transform.position : Vector3.zero;
        In("AutoAim", me + new Vector3(5f, 2f, 0f));
        In("AutoUlt", true);
        In("UltDownFrame", Time.frameCount + 1);
        Time.captureFramerate = 60;                 // 시간을 실제 게임처럼 1/60초씩 (배치 실행은 프레임이 너무 빨라 모으기가 거의 안 흐름)
        object kit = Find("CharacterKit");
        foreach (int f in new[] { 30, 30, 50 })     // 0.5초 · 1초 · 다 참(1.8초)
        {
            yield return Frames(f);
            Debug.Log("[UICAP-CHARGE] charging=" + Get(kit, "charging") + " charge=" + Get(kit, "charge") + " dt=" + Time.deltaTime);
            yield return Shot("20_swordsman_ult_aim_" + Time.frameCount);
        }
        Time.captureFramerate = 0;
        In("AutoUlt", false);
        In("UltUpFrame", Time.frameCount + 1);
        yield return Frames(20);

        // 지옥의 군주: 2장으로 옮긴 뒤 보스를 불러 운석 비 · 화염 파동을 바로 씀
        GoStage(1);
        yield return CloseCards(30);
        object sm = Find("StageManager");
        object sp = Get(sm, "spawner");
        Array stages = (Array)Get(sp, "stages");
        object st = stages.GetValue(1);
        GameObject prefab = (GameObject)st.GetType().GetField("bossPrefab").GetValue(st);
        pc = (Component)UnityEngine.Object.FindFirstObjectByType(T("PlayerController"));
        me = pc != null ? pc.transform.position : Vector3.zero;
        GameObject boss = UnityEngine.Object.Instantiate(prefab, me + new Vector3(0f, 6f, 0f), Quaternion.identity);
        yield return CloseCards(30);
        MonoBehaviour skills = (MonoBehaviour)boss.GetComponent(T("BossSkills"));
        if (skills != null)
        {
            MethodInfo meteor = skills.GetType().GetMethod("MeteorRain", Any);
            if (meteor != null) skills.StartCoroutine((IEnumerator)meteor.Invoke(skills, new object[] { pc }));
            yield return Frames(25);
            yield return Shot("21_hell_lord_meteor");
            MethodInfo wave = skills.GetType().GetMethod("FireWave", Any);
            if (wave != null) skills.StartCoroutine((IEnumerator)wave.Invoke(skills, null));
            yield return Frames(40);
            yield return Shot("22_hell_lord_firewave");
        }
    }

    [UnityTest]
    public IEnumerator Capture()
    {
        LogAssert.ignoreFailingMessages = true;
        Directory.CreateDirectory(Out);
        rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        frame = new Texture2D(W, H, TextureFormat.RGBA32, false);
        In("Auto", true);
        T("GameSettings").GetProperty("Language").SetValue(null, Enum.ToObject(T("Loc+Lang"), 0));
        if (Environment.GetEnvironmentVariable("CAPTURE_ONLY") == "fx")
        {
            yield return CaptureFx();
            In("Auto", false);
            Debug.Log("[UICAP] done");
            yield break;
        }

        // ---------------- 거너: HUD · 레벨업 대기 배지 · 도움말
        yield return Load("Gunner", 0);
        object shop = Find("LevelShop");
        Call(shop, "AddPending", 2);
        Hint("levelup", "레벨이 오르면 능력 포인트가 쌓입니다. [{INTERACT}]를 눌러 원할 때 능력을 고르세요.");
        yield return Frames(5);
        yield return Shot("01_hud_gunner");

        // ---------------- 레벨업 카드 (다시 뽑기 2회 · 가운데 카드 고름)
        object sp = T("SpecialAbilities").GetProperty("SharedInstance").GetValue(null);
        if (sp != null) Set(sp, "rerolls", 2);
        Call(shop, "openLevelShop");
        Set(shop, "selectReady", true);
        CanvasGroup cg = (CanvasGroup)Get(shop, "cardGroup");
        if (cg != null) { cg.alpha = 1f; cg.interactable = true; }
        yield return Frames(3);
        yield return Shot("02_levelup_cards");
        Call(shop, "onSecondButton");
        yield return Frames(3);
        yield return Shot("03_levelup_cards_picked");
        Set(shop, "pendingSlot", -1);
        Call(shop, "onSelect", Get(shop, "second"));
        Time.timeScale = 1f;
        yield return Frames(5);

        // ---------------- 위 가운데: 알림판 · 포털 카운트다운 · 레벨업 표시가 한꺼번에
        object sm = Find("StageManager");
        Call(sm, "EnsureCountdownUI");
        TMP_Text cd = (TMP_Text)Get(sm, "countdownText");
        cd.text = "포털이 열렸습니다  ·  5초 뒤 다음 세계로";
        cd.gameObject.SetActive(true);
        Call(sm, "ShowBanner", "보스 처치!  영혼 조각 +120", 10f);
        Set(shop, "showLv", true);
        yield return Frames(30);
        yield return Shot("04_top_stack");
        cd.gameObject.SetActive(false);
        Set(shop, "showLv", false);

        // ---------------- ESC 메뉴
        object esc = Find("ESCmenu");
        Call(esc, "ToggleEsc");
        yield return Frames(3);
        yield return Shot("05_esc_menu");
        Call(esc, "ToggleEsc");
        Time.timeScale = 1f;
        yield return Frames(3);

        // ---------------- 상점
        object shopUi = Find("Shop");
        if (shopUi != null && (bool)Call(shopUi, "OpenFromStall"))
        {
            yield return Frames(3);
            yield return Shot("06_shop");
            Call(shopUi, "ToggleShop");
            Time.timeScale = 1f;
            yield return Frames(3);
        }

        // ---------------- 영혼 트리
        T("SoulShards").GetMethod("Add").Invoke(null, new object[] { 4000, Vector3.zero, false });
        Time.timeScale = 1f;
        Call(sm, "OpenSoulTree");
        yield return Frames(5);
        yield return Shot("07_soul_tree");
        T("SoulTreeUI").GetMethod("Close").Invoke(null, null);
        yield return Frames(3);

        // ---------------- 검사: 특수 강화 버튼 + 레벨업 대기 배지 + 특수 트리
        yield return Load("Swordsman", 1);
        sm = Find("StageManager");
        Set(sm, "specialPoints", 2);
        shop = Find("LevelShop");
        Call(shop, "AddPending", 1);
        Hint("levelup", "레벨이 오르면 능력 포인트가 쌓입니다. [{INTERACT}]를 눌러 원할 때 능력을 고르세요.");
        yield return Frames(5);
        yield return Shot("08_hud_swordsman");
        Call(sm, "OpenUpgrade");
        yield return Frames(5);
        yield return Shot("09_special_tree");

        // ---------------- 메인 메뉴
        T("CharacterData").GetField("Override", Any).SetValue(null, null);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
        yield return Frames(20);
        yield return Shot("10_main_menu");

        // ---------------- 설정창 (메인 메뉴 위 · 첫 탭)
        Canvas menuCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (menuCanvas != null)
        {
            T("SettingsUI").GetMethod("Open", new[] { typeof(Transform) }).Invoke(null, new object[] { menuCanvas.transform });
            yield return Frames(5);
            yield return Shot("10b_settings");
            T("SettingsUI").GetMethod("Close").Invoke(null, null);
            yield return Frames(3);
            T("CodexUI").GetMethod("Open", new[] { typeof(Transform) }).Invoke(null, new object[] { menuCanvas.transform.root });
            yield return Frames(5);
            yield return Shot("10c_codex");
            T("CodexUI").GetMethod("Close", Any)?.Invoke(null, null);
            yield return Frames(3);
            T("CharacterUI").GetMethod("Open", new[] { typeof(Transform) }).Invoke(null, new object[] { menuCanvas.transform.root });
            yield return Frames(5);
            yield return Shot("10d_character");
        }

        // ---------------- 게임 오버 (무한 모드 기록 · 획득 포인트 포함)
        T("EndlessMode").GetField("LastSeconds").SetValue(null, 754f);
        T("EndlessMode").GetField("LastBosses").SetValue(null, 3);
        T("CharacterData").GetProperty("RunPoints").GetSetMethod(true).Invoke(null, new object[] { 340 });
        SceneManager.LoadScene("GameOver");
        yield return Frames(20);
        yield return Shot("11_game_over");

        In("Auto", false);
        Debug.Log("[UICAP] done");
    }
}
