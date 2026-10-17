using UnityEngine;

// 캐릭터: 거너(기본) · 검사 · 도적은 처음부터, 궁수 · 연금술사는 포인트로 구매
// 1.0.2~ 지도 제작자 · 시계공 · 우체부 · 유령: 해제 조건을 달성한 뒤 포인트로 구매 (CharacterKit.<캐릭터>.cs), 마지막 한 칸은 준비 중
// 스탯은 모두 거너를 1(100%)로 한 배율. 고른 캐릭터 · 잠금 해제 · 보유 포인트는 PlayerPrefs에 저장
// 캐릭터 해제 조건 (1.0.2): 조건을 달성해야 포인트로 살 수 있음 (달성 기록은 저장)
public enum CharUnlock { None = 0, Endless10 = 1, EndlessBosses5 = 2, EasyFast = 3, EndlessClear = 4 }

public enum CharacterId { Gunner = 0, Swordsman = 1, Rogue = 2, Archer = 3, Alchemist = 4, Cartographer = 5, Clockmaker = 6, Postman = 7, Ghost = 8, Slot10 = 9 }

public class CharacterDef
{
    public string name, title, description;
    public string body;             // Resources/Characters/<body> (null = 거너 원래 그림)
    public string held;             // 손에 드는 기본 무기 Resources/Weapons/weapon_<held>
    public string weapon, attack;   // 기본 무기 이름 · 평타 설명
    public string skill, skillDesc; // 우클릭 스킬
    public float hp = 1f, damage = 1f, attackSpeed = 1f, gauge = 1f, move = 1f;
    public float range;             // 평타 사거리 (칸), 0 = 무한
    public int mag;                 // 기본 탄창 수, 0 = 무한 (거너는 인스펙터 값)
    public int price;               // 0 = 처음부터, -1 = 준비 중
    public CharUnlock condition;     // 살 수 있게 되는 조건 (None = 바로 살 수 있음)
    public int[] pool;              // 지옥의 문에서 고를 수 있는 특수 능력 (무기 · 스킬 · 패시브)
    public Color color = Color.white;
}

public static class CharacterData
{
    public static readonly CharacterDef[] All =
    {
        new CharacterDef {
            name = "거너", title = "마지막 총잡이", body = null, held = "pistol",
            description = "리볼버와 여러 특수 총기를 다루는 총잡이. 다루기 쉬운 기본 캐릭터.",
            weapon = "리볼버", attack = "마우스 방향으로 총알 발사",
            skill = "일제 사격", skillDesc = "누르고 있으면 시간이 느려지며 적을 조준, 떼면 조준한 적 모두에게 사격",
            damage = 1.15f, range = 0f, mag = 8, price = 0, pool = Range(0, 20), color = new Color(0.4f, 0.85f, 0.9f) },
        new CharacterDef {
            name = "검사", title = "떠돌이 기사", body = "swordsman", held = "sword",
            description = "무겁고 느리지만 한 번에 여럿을 베는 장검의 달인.",
            weapon = "장검", attack = "검을 크게 휘둘러 앞쪽 부채꼴의 적을 직접 벱니다. 근접 특성: 휘두르는 동안 받는 피해 절반 · 벤 적을 크게 밀쳐냄 · 벤 만큼 조금 회복",
            skill = "회전 베기", skillDesc = "검을 사방으로 휘둘러 주변을 벱니다. 오래 누를수록 피해가 커집니다",
            hp = 1.15f, damage = 1.75f, attackSpeed = 0.55f,     // 1.1.3 빌드 뒤 2차: 「검사 데미지 전체 30% 하향」 2.5 → 1.75 (평타 · 검기 · 회전 베기)     // 1.1.3 빌드 뒤: 「검사 확 너프, 너무 세」 체력 1.3 → 1.15 · 공격력 3.1 → 2.5 (처치 속도 원거리 중앙값 +31% → 약 +5%)
                 // 1.0.1: 체력 200 → 130 (혈갑과 함께 절대 안 죽던 것), 공격력 4 → 3.4 (딜 상승폭 하향)
                                                                 // 1.1.3: 3.4 → 3.1 — 근접이라 다른 영웅보다 +30~50% 빠른 처치는 정상으로 보되, 보스 처치가 원거리 중앙값의 2배 · 사망률은 가장 낮아 그 넘는 몫만 (SigPower 2 → 1.7 · 회전 베기 · 휘두름 보호 · 회복과 함께)
            gauge = 0.85f, range = 5.5f, price = 0, pool = Pool(20, 52, 53),
            color = new Color(0.45f, 0.6f, 1f) },
        new CharacterDef {
            name = "도적", title = "그림자 칼날", body = "rogue", held = "shuriken",
            description = "약하지만 빠른 표창 세례와 그림자 돌진으로 싸우는 암살자.",
            weapon = "표창", attack = "끝없이 날아가는 표창",
            skill = "출혈 돌진", skillDesc = "무적 상태로 마우스 방향으로 돌진해, 지나간 적에게 출혈 피해를 입힙니다 (누르고 있으면 경로가 보이고, 떼면 돌진). 영혼 트리에서 그림자 숙련을 배우면 돌진에 이동 속도 · 둔화 · 연속 돌진 등이 붙습니다",
            hp = 0.85f, damage = 1f, attackSpeed = 1.4f, gauge = 1.2f, range = 0f, mag = 6, price = 0, pool = Pool(28, 54, 55),
            color = new Color(0.7f, 0.45f, 0.9f) },
        new CharacterDef {
            name = "궁수", title = "숲의 사냥꾼", body = "archer", held = "bow",
            description = "적을 꿰뚫는 화살과 하늘을 덮는 화살비의 명사수.",
            weapon = "사냥 활", attack = "누르고 있으면 시위를 당기고, 떼면 발사. 가득 찬 순간(딩!) 놓으면 「완벽!」 화살, 너무 오래 쥐면 약해집니다. 당기는 동안은 느려집니다. 숨 고르기: 당기는 동안 숨이 닳고, 바닥나면 활이 떨려 절반 위력 · 「완벽!」은 숨을 되돌려 줍니다",
            skill = "화살비", skillDesc = "누르고 있는 동안 화살이 쏟아질 위치를 조정하고, 떼면 쏟아붓습니다",
            hp = 0.95f, damage = 1.25f, attackSpeed = 1f, gauge = 1.15f, move = 1.05f, range = 0f, price = 1500, pool = Pool(36, 56, 57),
            color = new Color(0.45f, 0.8f, 0.4f) },
        new CharacterDef {
            name = "연금술사", title = "미친 학자", body = "alchemist", held = "flask",
            description = "터지는 플라스크로 적 무리를 한꺼번에 녹이는 괴짜 학자.",
            weapon = "플라스크", attack = "화염 · 빙결 · 산성 시약을 번갈아 채운 플라스크. 가끔 불안정한 플라스크가 크게 폭발. 불안정도: 던질수록 올라 폭발이 커지지만, 가득 차면 발밑에서 자폭합니다 (대폭발 플라스크가 모두 쏟아 냄)",
            skill = "대폭발 플라스크", skillDesc = "누를수록 커지는 플라스크를 던져 크게 폭발하고 산성 웅덩이를 남깁니다",
            hp = 1f, damage = 0.8f, attackSpeed = 0.8f, gauge = 1.2f, range = 10f, price = 2500, pool = Pool(44, 58, 59),     // 1.1.3: 공격 속도 0.75 → 0.8 (클리어가 9영웅 중 가장 느림 · 화력 중앙값 −7~12%)
            color = new Color(0.65f, 0.4f, 0.95f) },
        // ---------------- 1.0.2 새 캐릭터: 해제 조건 달성 뒤 포인트로
        new CharacterDef {
            name = "지도 제작자", title = "길 잃은 측량사", body = "cartographer", held = "quill",
            description = "깃펜으로 세상을 그리는 측량사. 그은 선으로 적을 베고, 세 획마다 저절로 그려지는 도형으로 안쪽을 통째로 짓이깁니다.",
            weapon = "깃펜", attack = "마우스 쪽으로 잉크 획을 그어 닿은 적을 벱니다. 세 번째 획마다 획 끝을 감싸는 삼각형이 저절로 닫혀 안쪽의 적이 크게 다칩니다",
            skill = "지도 접기", skillDesc = "누르고 있으면 접히는 선이 보이고, 떼면 선 양쪽의 적을 선 위로 끌어당겨 짓이깁니다. 오래 누를수록 넓게 접힙니다",
            hp = 0.95f, damage = 1.1f, attackSpeed = 0.85f, gauge = 1.1f, range = 9f, price = 5000, condition = CharUnlock.Endless10,
            pool = new[] { 60, 61, 62, 63, 64, 65, 66 }, color = new Color(0.35f, 0.55f, 0.95f) },
        new CharacterDef {
            name = "시계공", title = "멈춘 시간의 장인", body = "clockmaker", held = "gear",
            description = "톱니바퀴와 태엽으로 시간을 다루는 장인. 맞힐수록 시계 눈금이 차 정각의 큰 톱니를 던지고, 위기에는 3초 전으로 되돌아갑니다.",
            weapon = "톱니바퀴", attack = "회전하는 톱니를 던집니다. 맞힐 때마다 발밑 시계 눈금이 차고, 다 차면 다음 톱니가 모든 적을 꿰뚫고 터지는 「정각」 톱니가 됩니다",
            skill = "되감기", skillDesc = "누르고 있으면 3초 전의 내 모습이 보이고, 떼면 그 자리로 돌아갑니다. 지나온 길의 적을 베고, 그동안 잃은 체력의 절반을 되찾으며, 잃은 체력의 세 배를 둘레 적에게 되갚습니다",
            hp = 1f, damage = 1f, attackSpeed = 0.85f, gauge = 1f, range = 16f, price = 8000, condition = CharUnlock.EndlessBosses5,
            pool = new[] { 67, 68, 69, 70, 71, 72, 73 }, color = new Color(1f, 0.78f, 0.35f) },
        new CharacterDef {
            name = "우체부", title = "저승 배달부", body = "postman", held = "letter",
            description = "살아 있는 자에게도 죽은 자에게도 편지를 전하는 배달부. 편지는 가장 먼 적부터 찾아가고, 속달 한 번이면 둘레의 적 여럿에게 편지가 한꺼번에 터집니다.",
            weapon = "편지", attack = "마우스 쪽에서 가장 먼 적을 받는 이로 정해 편지를 날립니다. 편지는 다른 적을 지나쳐 받는 이를 쫓아가고, 직접 보낸 편지를 열 번 배달할 때마다 코인과 필살기 게이지를 받습니다",
            skill = "속달", skillDesc = "누르고 있는 동안 둘레의 적에게 편지 표식이 차례로 붙고 (오래 누를수록 많이), 떼면 1초 뒤 표식이 모두 한꺼번에 터집니다",
            hp = 0.9f, damage = 0.8f, attackSpeed = 1f, gauge = 1.15f, move = 1.08f, range = 12f, price = 12000, condition = CharUnlock.EasyFast,       // 1.0.6~ 하향: 공격력 0.9 → 0.8, 게이지 1.05 → 1.15 (필살기 충전 95% → 87%), 사거리 14 → 12
            pool = new[] { 74, 75, 76, 77, 78, 79, 80 }, color = new Color(0.95f, 0.45f, 0.4f) },
        new CharacterDef {
            name = "유령", title = "죽지 못한 영혼", body = "ghost", held = "lantern",
            description = "세상을 떠나지 못한 영혼. 차가운 손길로 적을 얼리고, 쓰러뜨린 적의 영혼을 몸 둘레에 거느려 피해를 대신 받게 하며, 적의 몸에 빙의해 안에서 터뜨립니다.",
            weapon = "차가운 손길", attack = "앞으로 짧게 뻗는 손이 한 줄의 적을 모두 꿰뚫고 냉기를 쌓습니다 (다섯 번 쌓이면 얼어붙음). 적을 쓰러뜨리면 확률로 영혼이 생겨 몸 둘레를 돌다가, 맞을 때 체력 대신 영혼이 사라지며 피해를 막습니다",
            skill = "빙의", skillDesc = "누르고 있으면 마우스 가까운 적에 표시가 붙고, 떼면 그 적의 몸에 들어가 3초 동안 그 적의 이동 속도로 몸을 끌고 다니다가 안에서 터뜨립니다. 들어간 동안은 무적이고, 둘레 적에게도 피해와 냉기를 줍니다",     // 1.0.4~: 1초 → 3초 · 조종
            hp = 0.9f, damage = 1f, attackSpeed = 1.05f, gauge = 1.05f, move = 1.05f, range = 4.5f, price = 15000, condition = CharUnlock.EndlessClear,     // 1.1.3: 체력 0.8 → 0.9 (사거리 4.5 근접인데 화력은 중앙값 · 받는 피해의 70~77% 가 접촉 · 사망률 가장 높음)
            pool = new[] { 81, 82, 83, 84, 85, 86, 87 }, color = new Color(0.65f, 0.92f, 1f) },
        Coming(),
    };

    static CharacterDef Coming(CharUnlock condition = CharUnlock.None) => new CharacterDef { name = "???", title = "준비 중", body = "mystery", price = -1, pool = new int[0], condition = condition,
                                                        description = "아직 준비 중인 캐릭터입니다." };

    // 캐릭터 능력 8개 (from ~ from+7) + 나중에 더한 패시브
    static int[] Pool(int from, params int[] more)
    {
        int[] a = new int[8 + more.Length];
        for (int i = 0; i < 8; i++) a[i] = from + i;
        for (int i = 0; i < more.Length; i++) a[8 + i] = more[i];
        return a;
    }

    static int[] Range(int from, int count)
    {
        int[] a = new int[count];
        for (int i = 0; i < count; i++) a[i] = from + i;
        return a;
    }

    const string SelectedKey = "char.selected";
    const string UnlockKey = "char.unlocked.";
    const string PointsKey = "char.points";

    // 자동 테스트가 캐릭터를 정할 때 (저장값을 건드리지 않음)
    public static CharacterId? Override;

    public static CharacterDef Def(CharacterId id) => All[(int)id];

    public static bool IsDeveloped(CharacterId id) => Def(id).price >= 0;

    public static bool IsUnlocked(CharacterId id)
    {
        CharacterDef d = Def(id);
        if (d.price < 0) return false;
        if (Demo.On) return Demo.CharacterAllowed(id);      // 체험판: 거너 · 검사만 (구매 없이)
        return d.price == 0 || Prefs.GetInt(UnlockKeyOf(id), 0) == 1;
    }

    // 저장 키를 영웅마다 한 번만 만들어 둠 (예전엔 부를 때마다 문자열을 이어 붙였음 — 피해마다 · 프레임마다 불림)
    static string[] unlockKeys;
    static string UnlockKeyOf(CharacterId id)
    {
        if (unlockKeys == null)
        {
            unlockKeys = new string[All.Length];
            for (int i = 0; i < unlockKeys.Length; i++) unlockKeys[i] = UnlockKey + i;
        }
        int k = (int)id;
        return k >= 0 && k < unlockKeys.Length ? unlockKeys[k] : UnlockKey + k;
    }

    // 고른 캐릭터를 프레임마다 한 번만 저장값에서 읽음 (같은 프레임 안에서 바꾸면 setter · Buy 가 바로 지움)
    static int selectedFrame = -1;
    static CharacterId selectedCache;

    // 게임에서 고른 캐릭터 (트레일러 · 배치 모드 테스트는 거너)
    public static CharacterId Selected
    {
        get
        {
            if (Override.HasValue) return Override.Value;
            if (GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) return CharacterId.Gunner;
            int frame = Application.isPlaying ? Time.frameCount : -2;      // 에디터(재생 전)에서는 캐시하지 않음
            if (frame >= 0 && frame == selectedFrame) return selectedCache;
            CharacterId id = (CharacterId)Mathf.Clamp(Prefs.GetInt(SelectedKey, 0), 0, All.Length - 1);
            selectedCache = IsUnlocked(id) ? id : CharacterId.Gunner;
            selectedFrame = frame;
            return selectedCache;
        }
        set
        {
            if (!IsUnlocked(value)) return;
            Prefs.SetInt(SelectedKey, (int)value);
            Prefs.Save();
            selectedFrame = -1;
        }
    }

    public static CharacterDef Current => Def(Selected);
    public static bool IsGunner => Selected == CharacterId.Gunner;
    // 1.0.2 새 캐릭터 (지도 제작자 · 시계공 · 우체부 · 유령)
    public static bool IsNewHero(CharacterId id) => id >= CharacterId.Cartographer && id <= CharacterId.Ghost;

    // ================================================================= 포인트 (적 처치 등으로 쌓이고 캐릭터 구매에 씀)
    public static int Points => Prefs.GetInt(PointsKey, 0);

    // 이번 판에 얻은 포인트 (게임 오버 화면에 보여 줌)
    public static int RunPoints { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name == "GameScene") RunPoints = 0;
            else Save();
        };
    }

    public static void AddPoints(int n)
    {
        if (n <= 0 || GameInput.Auto || TutorialRun.Active || (Application.isBatchMode && !Override.HasValue)) return;     // 튜토리얼은 포인트를 주지 않음
        RunPoints += n;
        Prefs.SetInt(PointsKey, Points + n);
    }

    // 포인트 쓰기 (스킨 상점). 모자라면 false
    public static bool SpendPoints(int n)
    {
        if (n <= 0 || Points < n) return false;
        Prefs.SetInt(PointsKey, Points - n);
        Prefs.Save();
        return true;
    }

    // 사면 true
    public static bool Buy(CharacterId id)
    {
        CharacterDef d = Def(id);
        if (d.price <= 0 || IsUnlocked(id) || Points < d.price || Demo.On || !ConditionMet(id)) return false;
        Prefs.SetInt(PointsKey, Points - d.price);
        Prefs.SetInt(UnlockKeyOf(id), 1);
        Prefs.Save();
        selectedFrame = -1;
        return true;
    }

    public static void Save() => Prefs.Save();

    // ================================================================= 해제 조건 (1.0.2)
    const string ConditionKey = "char.cond.";
    public static bool ConditionMet(CharUnlock c) => c == CharUnlock.None || Prefs.GetInt(ConditionKey + (int)c, 0) == 1;
    public static bool ConditionMet(CharacterId id) => ConditionMet(Def(id).condition);

    public static string ConditionText(CharUnlock c) => ConditionText(c, true);

    // 1.1~ prerequisite = false: 선행 조건(무한 모드 열기)은 빼고 조건만 (캐릭터 카드처럼 좁은 칸)
    public static string ConditionText(CharUnlock c, bool prerequisite) => (prerequisite ? Prerequisite(c) : "") + c switch
    {
        CharUnlock.Endless10 => Loc.T("무한 모드에서 10분 버티기"),
        CharUnlock.EndlessBosses5 => Loc.T("무한 모드 한 판에서 보스 5마리 처치"),
        CharUnlock.EasyFast => Loc.T("「쉬움」 난이도를 20분 안에 클리어"),
        CharUnlock.EndlessClear => Loc.T("무한 모드 클리어 (개발자 엄마 처치)"),     // 1.0.4~ 클리어 = 개발자와 엄마를 모두 처치
        _ => "",
    };

    // 1.0.4~: 무한 모드 조건인데 무한 모드가 아직 잠겨 있으면 그 선행 조건을 앞에 (예전엔 무한 모드가 잠긴 줄 모르고 조건만 보였음)
    static string Prerequisite(CharUnlock c)
    {
        bool endless = c == CharUnlock.Endless10 || c == CharUnlock.EndlessBosses5 || c == CharUnlock.EndlessClear;
        if (!endless || Demo.On || GameMode.IsUnlocked(Difficulty.Endless)) return "";
        return Loc.T("먼저 「어려움」을 클리어해 무한 모드 열기") + " → ";
    }

    // 게임 중 조건을 처음 달성했을 때 (한 번만 알림)
    public static void Achieve(CharUnlock c)
    {
        if (c == CharUnlock.None || ConditionMet(c) || GameInput.Auto || TutorialRun.Active || Demo.On || Application.isBatchMode) return;
        Prefs.SetInt(ConditionKey + (int)c, 1);
        Prefs.Save();
        if (StageManager.Instance != null)
            Notice.Big(Loc.T("새 캐릭터 해제 조건 달성!") + "\n" + ConditionText(c), NoticeIcon.Unlock, 4.5f, NoticeLevel.Important);     // 1.0.4~ 보스 처치 등 다른 알림에 덮이지 않게
        Hostile.Play("chime", 0.9f, 1.1f);
    }

    // 이 캐릭터가 지옥의 문에서 고를 수 있는 특수 능력인지
    public static bool InPool(int abilityId)
    {
        foreach (int id in Current.pool) if (id == abilityId) return true;
        return false;
    }
}
