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
    public const float MouseSensMin = 0.25f, MouseSensMax = 3f;

    static bool loaded;
    static float mouseSens = 1f;
    static Loc.Lang language = Loc.Lang.Korean;
    static float volume = 0.5f;
    static float music = 1f, sfx = 1f;
    static bool shake = true, flash = true, vsync = true;
    static bool hints = true, colorBlind = false, keyIcons = true, gameCursor = true, autoLevelUp, muteUnfocused = true;
    static int uiScale = 2;
    static int damageNumbers;
    static int myFxAlpha;
    static int noticeSize = 1;

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
        volume = Prefs.GetFloat(VolumeKey, 0.5f);
        music = Prefs.GetFloat(MusicKey, 1f);
        sfx = Prefs.GetFloat(SfxKey, 1f);
        shake = Prefs.GetInt(ShakeKey, 1) == 1;
        flash = Prefs.GetInt(FlashKey, 1) == 1;
        vsync = Prefs.GetInt(VSyncKey, 1) == 1;
        hints = Prefs.GetInt(HintsKey, 1) == 1;
        damageNumbers = Mathf.Clamp(Prefs.GetInt(DamageNumKey, 0), 0, 2);
        myFxAlpha = Mathf.Clamp(Prefs.GetInt(FxAlphaKey, 0), 0, MyFxAlphas.Length - 1);
        colorBlind = Prefs.GetInt(ColorBlindKey, 0) == 1;
        keyIcons = Prefs.GetInt(KeyIconKey, 1) == 1;
        gameCursor = Prefs.GetInt(CursorKey, 1) == 1;
        autoLevelUp = Prefs.GetInt(AutoLevelKey, 0) == 1;
        muteUnfocused = Prefs.GetInt(MuteKey, 1) == 1;
        uiScale = Mathf.Clamp(Prefs.GetInt(UiScaleKey, 2), 0, UiScales.Length - 1);
        mouseSens = Mathf.Clamp(Prefs.GetFloat(MouseSensKey, 1f), MouseSensMin, MouseSensMax);
        noticeSize = Mathf.Clamp(Prefs.GetInt(NoticeSizeKey, 1), 0, 2);
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
        set { Load(); vsync = value; Prefs.SetInt(VSyncKey, value ? 1 : 0); ApplyVSync(); }
    }

    static void ApplyVSync()
    {
        QualitySettings.vSyncCount = vsync ? 1 : 0;
        Application.targetFrameRate = -1;
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
        if (!Application.isBatchMode) ApplyVSync();
        Loc.ApplyFonts();
        SceneLocalizer.RefreshAll();
        SceneManager.sceneLoaded += (s, m) =>
        {
            AudioListener.volume = Volume;
            Loc.ApplyFonts();
            SceneLocalizer.RefreshAll();
            MenuExtras.Install();
        };
        MenuExtras.Install();
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
