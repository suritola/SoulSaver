using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// 설정값 (언어 · 볼륨 · 화면): PlayerPrefs에 저장하고 게임이 켜질 때 적용
// 화면 모드와 해상도는 Unity가 스스로 기억함
public static class GameSettings
{
    const string LangKey = "settings.language";
    const string VolumeKey = "settings.volume";
    const string MusicKey = "settings.music";
    const string SfxKey = "settings.sfx";
    const string ShakeKey = "settings.shake";
    const string FlashKey = "settings.flash";
    const string VSyncKey = "settings.vsync";
    const string HintsKey = "settings.hints";
    const string DamageNumKey = "settings.damageNumbers";
    const string FxAlphaKey = "settings.myFxAlpha";
    const string ColorBlindKey = "settings.colorBlind";
    const string KeyIconKey = "settings.keyIcons";
    const string CursorKey = "settings.gameCursor";
    const string AutoLevelKey = "settings.autoLevelUp";
    const string MuteKey = "settings.muteUnfocused";
    const string UiScaleKey = "settings.uiScale";
    const string MouseSensKey = "settings.mouseSensitivity";
    const string NoticeSizeKey = "settings.noticeSize";
    const string ConfineKey = "settings.confineCursor";
    const string OneClickKey = "settings.oneClickPick";
    const string HeraldKey = "settings.herald";            // 1.2~ 망자의 전령 (쓰러진 뒤 한 줄)
    const string MaxFpsKey = "settings.maxFps";
    const string ShowFpsKey = "settings.showFps";
    // 1.1.9~ 편의성 2 · 3차: 접근성 · 표시 · 화면
    const string BossIntroKey = "settings.bossIntroAlways", AttackLockKey = "settings.attackLock", UltToggleKey = "settings.ultToggle", DialogueKey = "settings.dialogueSpeed";
    const string ReduceMotionKey = "settings.reduceMotion", FastFxKey = "settings.fastFx";
    const string CombatStatsKey = "settings.combatStats", PickupRangeKey = "settings.showPickupRange", EdgeMarkersKey = "settings.edgeMarkers";
    const string DisplayKey = "settings.display";
    public const float MouseSensMin = 0.25f, MouseSensMax = 3f;

    // 1.1.9~ 기본값 (처음 불러올 때 · 설정 「이 탭 기본값으로」가 같이 써서 어긋나지 않게)
    const float DefVolume = 0.5f, DefMusic = 1f, DefSfx = 1f, DefMouseSens = 1f;
    const bool DefMute = true, DefVSync = false, DefShowFps = false, DefShake = true, DefFlash = true;
    const int DefMaxFps = 1, DefUiScale = 2, DefNoticeSize = 1, DefDamageNumbers = 0, DefMyFxAlpha = 0;
    const bool DefHints = true, DefColorBlind = false, DefGameCursor = true, DefAutoLevelUp = false, DefConfine = true, DefOneClick = false, DefHerald = true;
    const bool DefBossIntroAlways = false, DefAttackLock = false, DefUltToggle = false, DefReduceMotion = false, DefFastFx = false;
    const int DefDialogueSpeed = 0, DefCombatStats = 0, DefDisplay = 0;
    const bool DefShowPickupRange = false, DefEdgeMarkers = true;

    static bool loaded;
    static float mouseSens = 1f;
    static Loc.Lang language = Loc.Lang.Korean;
    static float volume = 0.5f;
    static float music = 1f, sfx = 1f;
    static bool shake = true, flash = true, vsync;
    static bool hints = true, colorBlind = false, keyIcons = true, gameCursor = true, autoLevelUp, muteUnfocused = true;
    static int uiScale = 2;
    static int damageNumbers;
    static int myFxAlpha;
    static int noticeSize = 1;
    static bool confineCursor = DefConfine, oneClickPick = DefOneClick, herald = DefHerald;
    static int maxFps = DefMaxFps;
    static bool showFps;
    static bool bossIntroAlways, attackLock, ultToggle, reduceMotion, fastFx, showPickupRange, edgeMarkers = DefEdgeMarkers;
    static int dialogueSpeed, combatStats, displayIndex;

    public static Loc.Lang Language
    {
        get { Load(); return language; }
        set
        {
            Load();
            Prefs.SetInt(LangKey, (int)value);          // 1.0.4~: 감지한 언어와 같은 언어를 골라도 「골랐음」을 남김
            Prefs.Save();
            if (language == value) return;
            language = value;
            Loc.ApplyFonts();
            SceneLocalizer.RefreshAll();
            Loc.RaiseChanged();
        }
    }

    // 1.0.4~: 언어를 직접 고른 적이 있는지 (없으면 메인 메뉴가 처음 한 번 언어 고르기 창을 띄움)
    public static bool LanguageChosen => Prefs.HasKey(LangKey);

    // 스팀 게임 언어 → OS 언어 순서로 (지원하지 않는 언어는 영어) · 자동 테스트는 늘 한국어
    static Loc.Lang DetectLanguage(bool useSteam)
    {
        if (Application.isBatchMode || GameInput.Auto) return Loc.Lang.Korean;
        string steam = useSteam ? SteamManager.GameLanguage : "";
        if (!string.IsNullOrEmpty(steam))
        {
            if (steam == "koreana" || steam == "korean") return Loc.Lang.Korean;
            if (steam == "japanese") return Loc.Lang.Japanese;
            if (steam == "schinese" || steam == "tchinese") return Loc.Lang.Chinese;
            return Loc.Lang.English;
        }
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Korean: return Loc.Lang.Korean;
            case SystemLanguage.Japanese: return Loc.Lang.Japanese;
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional: return Loc.Lang.Chinese;
            default: return Loc.Lang.English;
        }
    }

    // 0 ~ 1 (기본 0.5)
    public static float Volume
    {
        get { Load(); return volume; }
        set
        {
            Load();
            volume = Mathf.Clamp01(value);
            AudioListener.volume = volume;
            Prefs.SetFloat(VolumeKey, volume);
        }
    }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        // 1.0.4~: 아직 언어를 고른 적이 없으면 OS 언어로 (스팀 언어는 스팀이 켜진 뒤 Boot 에서 다시 봄 · 예전엔 무조건 한국어)
        language = LanguageChosen ? (Loc.Lang)Mathf.Clamp(Prefs.GetInt(LangKey, 0), 0, 3) : DetectLanguage(false);
        volume = Prefs.GetFloat(VolumeKey, DefVolume);
        music = Prefs.GetFloat(MusicKey, DefMusic);
        sfx = Prefs.GetFloat(SfxKey, DefSfx);
        shake = Prefs.GetInt(ShakeKey, B(DefShake)) == 1;
        flash = Prefs.GetInt(FlashKey, B(DefFlash)) == 1;
        vsync = Prefs.GetInt(VSyncKey, B(DefVSync)) == 1;            // 1.1.9~ 기본 끔 (최대 프레임으로 제한)
        maxFps = Mathf.Clamp(Prefs.GetInt(MaxFpsKey, DefMaxFps), 0, MaxFpsValues.Length - 1);
        showFps = Prefs.GetInt(ShowFpsKey, B(DefShowFps)) == 1;
        hints = Prefs.GetInt(HintsKey, B(DefHints)) == 1;
        damageNumbers = Mathf.Clamp(Prefs.GetInt(DamageNumKey, DefDamageNumbers), 0, 2);
        myFxAlpha = Mathf.Clamp(Prefs.GetInt(FxAlphaKey, DefMyFxAlpha), 0, MyFxAlphas.Length - 1);
        colorBlind = Prefs.GetInt(ColorBlindKey, B(DefColorBlind)) == 1;
        keyIcons = Prefs.GetInt(KeyIconKey, 1) == 1;
        gameCursor = Prefs.GetInt(CursorKey, B(DefGameCursor)) == 1;
        autoLevelUp = Prefs.GetInt(AutoLevelKey, B(DefAutoLevelUp)) == 1;
        muteUnfocused = Prefs.GetInt(MuteKey, B(DefMute)) == 1;
        uiScale = Mathf.Clamp(Prefs.GetInt(UiScaleKey, DefUiScale), 0, UiScales.Length - 1);
        mouseSens = Mathf.Clamp(Prefs.GetFloat(MouseSensKey, DefMouseSens), MouseSensMin, MouseSensMax);
        noticeSize = Mathf.Clamp(Prefs.GetInt(NoticeSizeKey, DefNoticeSize), 0, 2);
        confineCursor = Prefs.GetInt(ConfineKey, B(DefConfine)) == 1;
        oneClickPick = Prefs.GetInt(OneClickKey, B(DefOneClick)) == 1;
        herald = Prefs.GetInt(HeraldKey, B(DefHerald)) == 1;
        bossIntroAlways = Prefs.GetInt(BossIntroKey, B(DefBossIntroAlways)) == 1;
        attackLock = Prefs.GetInt(AttackLockKey, B(DefAttackLock)) == 1;
        ultToggle = Prefs.GetInt(UltToggleKey, B(DefUltToggle)) == 1;
        dialogueSpeed = Mathf.Clamp(Prefs.GetInt(DialogueKey, DefDialogueSpeed), 0, 2);
        reduceMotion = Prefs.GetInt(ReduceMotionKey, B(DefReduceMotion)) == 1;
        fastFx = Prefs.GetInt(FastFxKey, B(DefFastFx)) == 1;
        combatStats = Mathf.Clamp(Prefs.GetInt(CombatStatsKey, DefCombatStats), 0, 2);
        showPickupRange = Prefs.GetInt(PickupRangeKey, B(DefShowPickupRange)) == 1;
        edgeMarkers = Prefs.GetInt(EdgeMarkersKey, B(DefEdgeMarkers)) == 1;
        displayIndex = Mathf.Max(0, Prefs.GetInt(DisplayKey, DefDisplay));
    }

    // ================================================================= 1.1.9~ 접근성 탭
    // 보스 등장 대사: 「처음만」(false) · 「항상」(true) — 보스마다 첫 번째는 늘 전체 (StoryDirector.RunBossIntro)
    public static readonly string[] BossIntroNames = { "처음만", "항상" };
    public static bool BossIntroAlways
    {
        get { Load(); return bossIntroAlways; }
        set { Load(); bossIntroAlways = value; Prefs.SetInt(BossIntroKey, B(value)); }
    }

    // 공격 고정: 공격 키를 한 번 누르면 계속 쏘고 다시 누르면 멈춤 (GameInput.AttackLockNow · 패드 제외)
    public static bool AttackLock
    {
        get { Load(); return attackLock; }
        set { Load(); attackLock = value; Prefs.SetInt(AttackLockKey, B(value)); if (!value) GameInput.ResetAttackLock(); }
    }

    // 필살기 누르면 토글: 한 번 누르면 조준 시작, 다시 누르면 발동 (GameInput.UltDown/Held/Up)
    public static bool UltToggle
    {
        get { Load(); return ultToggle; }
        set { Load(); ultToggle = value; Prefs.SetInt(UltToggleKey, B(value)); GameInput.ResetUltLatch(); }
    }

    // 대사 속도: 0 보통 · 1 빠름 · 2 즉시 (StoryDirector.CharTime)
    public static readonly string[] DialogueSpeedNames = { "보통", "빠름", "즉시" };
    public static int DialogueSpeed
    {
        get { Load(); return dialogueSpeed; }
        set { Load(); dialogueSpeed = Mathf.Clamp(value, 0, 2); Prefs.SetInt(DialogueKey, dialogueSpeed); }
    }

    // 3차 6 움직임 줄이기: UI 맥동 · 카드 연출 · 체력바 흔들림 · 알림 움직임의 진폭에 곱함 (켤 때 화면 흔들림도 끔)
    public static bool ReduceMotion
    {
        get { Load(); return reduceMotion; }
        set { Load(); reduceMotion = value; Prefs.SetInt(ReduceMotionKey, B(value)); if (value) ScreenShake = false; }
    }
    public static float Motion => ReduceMotion && !GameInput.Auto ? 0f : 1f;

    // 3차 7 빠른 연출: 조작하지 않는 연출의 대기 시간만 절반 (튜토리얼 · 자동 조종은 그대로)
    public static bool FastFx
    {
        get { Load(); return fastFx; }
        set { Load(); fastFx = value; Prefs.SetInt(FastFxKey, B(value)); }
    }
    public static float FxTime => FastFx && !GameInput.Auto && !TutorialRun.Active ? 0.5f : 1f;

    public static void ResetAccess()
    {
        ColorBlind = DefColorBlind; AttackLock = DefAttackLock; UltToggle = DefUltToggle; DialogueSpeed = DefDialogueSpeed;
        BossIntroAlways = DefBossIntroAlways; ReduceMotion = DefReduceMotion; FastFx = DefFastFx;
        Save();
    }

    // ================================================================= 1.1.9~ 표시 탭 (전장 · HUD 에 그려지는 것)
    // 3차 2 전투 통계: 0 꺼짐 · 1 작게 · 2 크게
    public static readonly string[] CombatStatsNames = { "꺼짐", "작게", "크게" };
    public static int CombatStats
    {
        get { Load(); return combatStats; }
        set { Load(); combatStats = Mathf.Clamp(value, 0, 2); Prefs.SetInt(CombatStatsKey, combatStats); CombatStatsHUD.Refresh(); }
    }

    // 3차 3 획득 범위 표시 (꺼도 코인 자석 카드를 고른 직후엔 잠깐)
    public static bool ShowPickupRange
    {
        get { Load(); return showPickupRange; }
        set { Load(); showPickupRange = value; Prefs.SetInt(PickupRangeKey, B(value)); }
    }

    // 3차 8 화면 밖 표시 (보물 상자 · 떠돌이 상점 · 황금 고블린 · 중간 보스, 기본 켜짐)
    public static bool EdgeMarkers
    {
        get { Load(); return edgeMarkers; }
        set { Load(); edgeMarkers = value; Prefs.SetInt(EdgeMarkersKey, B(value)); }
    }

    public static void ResetHud()
    {
        DamageNumbers = DefDamageNumbers; MyFxAlphaIndex = DefMyFxAlpha; CombatStats = DefCombatStats; ShowPickupRange = DefShowPickupRange; EdgeMarkers = DefEdgeMarkers;
        Save();
    }

    // ================================================================= 1.1.9~ 화면: 모니터 (최대 프레임은 아래 MaxFpsIndex — 1.1.9 핫픽스 저장 키 settings.maxFps)
    // 고른 모니터 (0 = 주 모니터) — 표시는 저장값이 아니라 창이 실제로 있는 모니터 (CurrentDisplayIndex)
    public static int DisplayIndex
    {
        get { Load(); return displayIndex; }
        set { Load(); displayIndex = Mathf.Max(0, value); Prefs.SetInt(DisplayKey, displayIndex); ApplyDisplay(); }
    }

    public static System.Collections.Generic.List<DisplayInfo> Displays()
    {
        System.Collections.Generic.List<DisplayInfo> list = new System.Collections.Generic.List<DisplayInfo>();
        try { Screen.GetDisplayLayout(list); } catch { }
        return list;
    }

    public static int CurrentDisplayIndex(System.Collections.Generic.List<DisplayInfo> list)
    {
        DisplayInfo now = Screen.mainWindowDisplayInfo;
        return list.FindIndex(d => d.Equals(now));
    }

    // 창이 있는 모니터의 해상도 (보조 모니터에서도 맞게 — 예전엔 Display.main = 주 모니터)
    public static Vector2Int MonitorSize
    {
        get
        {
            DisplayInfo d = Screen.mainWindowDisplayInfo;
            return d.width > 0 && d.height > 0 ? new Vector2Int(d.width, d.height) : new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);
        }
    }

    static void ApplyDisplay()
    {
        System.Collections.Generic.List<DisplayInfo> list = Displays();
        if (displayIndex >= list.Count) return;                // 모니터가 빠졌으면 그대로 (설정값은 남겨 다음에 꽂으면 다시)
        DisplayInfo d = list[displayIndex];
        if (d.Equals(Screen.mainWindowDisplayInfo)) return;
        bool full = Screen.fullScreenMode != FullScreenMode.Windowed;
        int w = Screen.width, h = Screen.height;
        // 전체 화면은 모니터 사이 이동이 들쭉날쭉 → 창 모드로 잠깐 바꾸고 옮긴 뒤 그 모니터 해상도로 다시 전체 화면
        if (full)
        {
            w = Mathf.Min(w, d.width - 80);
            h = Mathf.Min(h, d.height - 80);
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
        }
        Vector2Int at = new Vector2Int(Mathf.Max(0, (d.width - w) / 2), Mathf.Max(0, (d.height - h) / 2));
        AsyncOperation op;
        try { op = Screen.MoveMainWindowTo(d, at); } catch { op = null; }
        if (full && op != null) op.completed += _ => Screen.SetResolution(d.width, d.height, FullScreenMode.FullScreenWindow);
    }

    static int B(bool v) => v ? 1 : 0;

    // 1.1.9~ 전투 중 마우스 커서를 창 안에 가둠 (멈춘 화면 · 메뉴 · 패드 중엔 풀림, CursorSkin 이 적용)
    public static bool ConfineCursor
    {
        get { Load(); return confineCursor; }
        set { Load(); confineCursor = value; Prefs.SetInt(ConfineKey, B(value)); }
    }

    // 1.1.9~ 레벨업 · 보물 상자 · 보스 보상 카드를 한 번 클릭으로 확정 (무기 진화는 늘 두 번 · 튜토리얼은 두 단계 그대로)
    public static bool OneClickPick
    {
        get { Load(); return oneClickPick; }
        set { Load(); oneClickPick = value; Prefs.SetInt(OneClickKey, B(value)); }
    }

    // 1.2~ 망자의 전령: 쓰러진 뒤 결과 화면 전에 한 줄 (끄면 바로 결과 화면, Herald)
    public static bool Herald
    {
        get { Load(); return herald; }
        set { Load(); herald = value; Prefs.SetInt(HeraldKey, B(value)); }
    }

    // 1.1.9~ 설정 「이 탭 기본값으로」: 속성을 거쳐 되돌려 부수 효과(볼륨 · UI 크기 · 커서 · 수직 동기화)가 그대로 따라옴
    public static void ResetGeneral()           // 언어는 그대로
    {
        Volume = DefVolume; MusicVolume = DefMusic; SfxVolume = DefSfx; MouseSensitivity = DefMouseSens; MuteUnfocused = DefMute;
        Save();
    }

    public static void ResetDisplay()           // 화면 모드 · 해상도 · 모니터는 그대로
    {
        UiScaleIndex = DefUiScale; NoticeSize = DefNoticeSize; VSync = DefVSync; MaxFpsIndex = DefMaxFps; ShowFps = DefShowFps; ScreenShake = DefShake; Flashes = DefFlash;
        Save();
    }

    public static void ResetGameplay()
    {
        Hints = DefHints; GameCursor = DefGameCursor;       // 1.1.9~ 색각 이상 모드는 접근성, 피해 숫자 · 내 이펙트 불투명도는 표시 탭으로
        AutoLevelUp = DefAutoLevelUp; ConfineCursor = DefConfine; OneClickPick = DefOneClick; Herald = DefHerald; CodexSeen.ShowAll = false;
        Save();
    }

    // 1.1.2~ 안내 메시지 크기 (게임 중 큰 알림 · 토스트 · 도움말 · 진행 줄): 0 보통 · 1 크게(기본) · 2 아주 크게 — Notice.Scale
    public static int NoticeSize
    {
        get { Load(); return noticeSize; }
        set { Load(); noticeSize = Mathf.Clamp(value, 0, 2); Prefs.SetInt(NoticeSizeKey, noticeSize); }
    }

    // 전투 중 마우스 감도 (1 = 윈도우 커서 그대로, 0.25 ~ 3) · 1.8.9~ (MouseSensitivity)
    public static float MouseSensitivity
    {
        get { Load(); return mouseSens; }
        set { Load(); mouseSens = Mathf.Clamp(value, MouseSensMin, MouseSensMax); Prefs.SetFloat(MouseSensKey, mouseSens); }
    }

    // 음악 · 효과음 볼륨 (0 ~ 1, 전체 볼륨에 곱해짐)
    public static float MusicVolume
    {
        get { Load(); return music; }
        set { Load(); music = Mathf.Clamp01(value); Prefs.SetFloat(MusicKey, music); }
    }

    public static float SfxVolume
    {
        get { Load(); return sfx; }
        set { Load(); sfx = Mathf.Clamp01(value); Prefs.SetFloat(SfxKey, sfx); }
    }

    // 화면 흔들림 (트레일러 촬영 중에는 항상 켬)
    public static bool ScreenShake
    {
        get { Load(); return shake || GameInput.Auto; }
        set { Load(); shake = value; Prefs.SetInt(ShakeKey, value ? 1 : 0); }
    }

    // 피격 시 붉은 번쩍임 · 저체력 깜빡임 · 폭발 섬광 (끄면 약하게, 깜빡이지 않게)
    public static bool Flashes
    {
        get { Load(); return flash || GameInput.Auto; }
        set { Load(); flash = value; Prefs.SetInt(FlashKey, value ? 1 : 0); }
    }

    // 처음 한 번씩 뜨는 도움말
    public static bool Hints
    {
        get { Load(); return hints; }
        set { Load(); hints = value; Prefs.SetInt(HintsKey, value ? 1 : 0); }
    }

    // 피해 숫자: 0 보통 · 1 크게 · 2 꺼짐 (1.0.4~ 다른 설정처럼 「꺼짐」)
    public static readonly string[] DamageNumberNames = { "보통", "크게", "꺼짐" };
    public static int DamageNumbers
    {
        get { Load(); return damageNumbers; }
        set { Load(); damageNumbers = Mathf.Clamp(value, 0, 2); Prefs.SetInt(DamageNumKey, damageNumbers); }
    }

    // 1.0.2: 내 이펙트 투명도 (평타 · 스킬 · 필살기 · 진화 이펙트, 적 공격은 그대로) — 화면이 가득 찰 때 내 캐릭터가 묻히지 않게
    public static readonly float[] MyFxAlphas = { 1f, 0.75f, 0.5f };
    public static readonly string[] MyFxAlphaNames = { "100%", "75%", "50%" };
    public static float MyFxAlpha => MyFxAlphas[MyFxAlphaIndex];
    public static int MyFxAlphaIndex
    {
        get { Load(); return GameInput.Auto ? 0 : myFxAlpha; }
        set { Load(); myFxAlpha = Mathf.Clamp(value, 0, MyFxAlphas.Length - 1); Prefs.SetInt(FxAlphaKey, myFxAlpha); }
    }

    // 색각 이상 모드: 경고 표시를 빨강 · 초록 대신 주황 · 파랑 · 자홍으로
    public static bool ColorBlind
    {
        get { Load(); return colorBlind; }
        set { Load(); colorBlind = value; Prefs.SetInt(ColorBlindKey, value ? 1 : 0); }
    }

    // HUD 스킬 칸의 키를 키보드 모양 아이콘으로
    public static bool KeyIcons
    {
        get { Load(); return keyIcons; }
        set { Load(); keyIcons = value; Prefs.SetInt(KeyIconKey, value ? 1 : 0); }
    }

    // 게임 전용 마우스 커서 (끄면 윈도우 기본 포인터) · 1.8.7~
    public static bool GameCursor
    {
        get { Load(); return gameCursor; }
        set { Load(); gameCursor = value; Prefs.SetInt(CursorKey, value ? 1 : 0); CursorSkin.Refresh(); }
    }

    // 레벨이 오르면 카드 창을 바로 엶 (끄면 [상호작용] 키로 하나씩 · 2.1.8~)
    public static bool AutoLevelUp
    {
        get { Load(); return autoLevelUp; }
        set { Load(); autoLevelUp = value; Prefs.SetInt(AutoLevelKey, value ? 1 : 0); }
    }

    // 창이 비활성이면 (Alt+Tab · 다른 창 클릭) 소리를 멈춤 · 2.1.9~ (UiScaler 가 적용)
    public static bool MuteUnfocused
    {
        get { Load(); return muteUnfocused; }
        set { Load(); muteUnfocused = value; Prefs.SetInt(MuteKey, value ? 1 : 0); }
    }

    // UI 크기: 모든 화면의 글자 · 버튼 · HUD (CanvasScaler 기준 해상도를 나눔) · 2.1.9~
    public static readonly float[] UiScales = { 0.8f, 0.9f, 1f, 1.1f, 1.2f };
    public static readonly string[] UiScaleNames = { "80%", "90%", "100%", "110%", "120%" };
    public static float UiScale => UiScales[UiScaleIndex];
    public static int UiScaleIndex
    {
        get { Load(); return uiScale; }
        set { Load(); uiScale = Mathf.Clamp(value, 0, UiScales.Length - 1); Prefs.SetInt(UiScaleKey, uiScale); UiScaler.ApplyAll(); }
    }

    public static bool VSync
    {
        get { Load(); return vsync; }
        set { Load(); vsync = value; Prefs.SetInt(VSyncKey, value ? 1 : 0); ApplyFrameRate(); }
    }

    // 1.1.9~ 최대 프레임: 30 · 60(기본) · 120 · 144 · 제한 없음 (수직 동기화를 켜면 모니터 주사율이 우선)
    public static readonly int[] MaxFpsValues = { 30, 60, 120, 144, -1 };
    public static readonly string[] MaxFpsNames = { "30", "60", "120", "144", "제한 없음" };
    public static int MaxFpsIndex
    {
        get { Load(); return maxFps; }
        set { Load(); maxFps = Mathf.Clamp(value, 0, MaxFpsValues.Length - 1); Prefs.SetInt(MaxFpsKey, maxFps); ApplyFrameRate(); }
    }

    // 1.1.9~ 화면 구석 FPS 숫자 (FpsCounter)
    public static bool ShowFps
    {
        get { Load(); return showFps; }
        set { Load(); showFps = value; Prefs.SetInt(ShowFpsKey, value ? 1 : 0); FpsCounter.Refresh(); }
    }

    // 프레임 설정은 여기 한 곳에서만 (targetFrameRate · vSyncCount)
    static void ApplyFrameRate()
    {
        if (Application.isBatchMode) return;
        QualitySettings.vSyncCount = vsync ? 1 : 0;
        Application.targetFrameRate = vsync ? -1 : MaxFpsValues[maxFps];
    }

    // 설정 창을 닫을 때 디스크에 바로 기록 (비정상 종료에도 남도록)
    public static void Save() => Prefs.Save();

    // 게임이 켜질 때 볼륨 · 폰트 적용, 씬마다 글자 번역
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Load();
        // 1.0.4~: 이제 스팀이 켜져 있으니 스팀 언어로 다시 봄 (저장은 고르기 창에서 고를 때)
        if (!LanguageChosen)
        {
            Loc.Lang detected = DetectLanguage(true);
            if (detected != language) { language = detected; Loc.RaiseChanged(); }
        }
        AudioListener.volume = volume;
        ApplyFrameRate();
        FpsCounter.Refresh();
        Loc.ApplyFonts();
        SceneLocalizer.RefreshAll();
        SceneManager.sceneLoaded += (s, m) =>
        {
            AudioListener.volume = Volume;
            Loc.ApplyFonts();
            SceneLocalizer.RefreshAll();
            MenuExtras.Install();
        };
        SteamDeckFirstRun();            // 메인 메뉴를 그리기 전에 (알림이 메뉴에 뜨게)
        MenuExtras.Install();
        // 1.1.9~ 고른 모니터가 지금 모니터와 다르면 첫 프레임 뒤 한 번 옮김 (MoveMainWindowTo 는 첫 프레임 전엔 실패)
        if (displayIndex > 0 && !Application.isBatchMode && !GameInput.Auto) MenuToast.RunLater(ApplyDisplay);
    }

    // 1.1.9~ 스팀 덱에서 처음 켰을 때 한 번: UI 110% · 안내 메시지 크게 · 바로 게임패드 안내 · 전체 화면 (사용자가 바꾼 값은 그 뒤로 그대로)
    public static bool DeckApplied { get; private set; }
    public static void DeckNoticed() => DeckApplied = false;        // 알림은 한 번만 (언어 창 또는 메뉴 아래)
    static void SteamDeckFirstRun()
    {
        if (!SteamManager.OnDeck || Prefs.GetInt("deck.applied", 0) != 0 || GameInput.Auto || Application.isBatchMode) return;
        UiScaleIndex = 3;
        NoticeSize = 2;
        GamePad.SetActive(true);
        Vector2Int m = MonitorSize;
        Screen.SetResolution(m.x, m.y, FullScreenMode.FullScreenWindow);
        Prefs.SetInt("deck.applied", 1);
        Prefs.Save();
        DeckApplied = true;          // 메인 메뉴가 한 번 알림 (MenuExtras)
    }
}

// 씬에 적혀 있는 한국어 글자(버튼, 제목 등)를 현재 언어로 바꿔 줌
public static class SceneLocalizer
{
    static readonly System.Collections.Generic.Dictionary<int, string> original = new System.Collections.Generic.Dictionary<int, string>();

    public static void RefreshAll()
    {
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            int id = t.GetInstanceID();
            if (!original.TryGetValue(id, out string ko))
            {
                if (!Loc.Has(t.text)) continue;
                ko = t.text;
                original[id] = ko;
            }
            t.text = Loc.T(ko);
        }
    }
}
