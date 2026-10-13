using UnityEngine;

// 캐릭터: 거너(기본) · 검사 · 도적은 처음부터, 궁수 · 연금술사는 포인트로 구매
// 1.1.2~ 지도 제작자 · 시계공 · 우체부 · 유령: 해제 조건을 달성한 뒤 포인트로 구매 (CharacterKit.<캐릭터>.cs), 마지막 한 칸은 준비 중
// 스탯은 모두 거너를 1(100%)로 한 배율. 고른 캐릭터 · 잠금 해제 · 보유 포인트는 PlayerPrefs에 저장
// 캐릭터 해제 조건 (1.1.2): 조건을 달성해야 포인트로 살 수 있음 (달성 기록은 저장)
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
            damage = 1.05f, range = 0f, mag = 8, price = 0, pool = Range(0, 20), color = new Color(0.4f, 0.85f, 0.9f) },
        new CharacterDef {
            name = "검사", title = "떠돌이 기사", body = "swordsman", held = "sword",
            description = "무겁고 느리지만 한 번에 여럿을 베는 장검의 달인.",
            weapon = "장검", attack = "검을 크게 휘둘러 앞쪽 부채꼴의 적을 직접 벱니다. 근접 특성: 휘두르는 동안 받는 피해 절반 · 벤 적을 크게 밀쳐냄 · 벤 만큼 조금 회복",
            skill = "회전 베기", skillDesc = "검을 사방으로 휘둘러 주변을 벱니다. 오래 누를수록 피해가 커집니다",
            hp = 1.3f, damage = 3.4f, attackSpeed = 0.55f,     // 1.0.5: 체력 200 → 130 (혈갑과 함께 절대 안 죽던 것), 공격력 4 → 3.4 (딜 상승폭 하향)
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
            weapon = "사냥 활", attack = "누르고 있으면 시위를 당기고, 떼면 발사. 가득 찬 순간(딩!) 놓으면 「완벽!」 화살, 너무 오래 쥐면 약해집니다. 당기는 동안은 느려집니다",
            skill = "화살비", skillDesc = "누르고 있는 동안 화살이 쏟아질 위치를 조정하고, 떼면 쏟아붓습니다",
            hp = 0.95f, damage = 1.25f, attackSpeed = 1f, gauge = 1.15f, move = 1.05f, range = 0f, price = 1500, pool = Pool(36, 56, 57),
            color = new Color(0.45f, 0.8f, 0.4f) },
        new CharacterDef {
            name = "연금술사", title = "미친 학자", body = "alchemist", held = "flask",
            description = "터지는 플라스크로 적 무리를 한꺼번에 녹이는 괴짜 학자.",
            weapon = "플라스크", attack = "화염 · 빙결 · 산성 시약을 번갈아 채운 플라스크. 가끔 불안정한 플라스크가 크게 폭발",
            skill = "대폭발 플라스크", skillDesc = "누를수록 커지는 플라스크를 던져 크게 폭발하고 산성 웅덩이를 남깁니다",
            hp = 1f, damage = 0.8f, attackSpeed = 0.75f, gauge = 1.2f, range = 10f, price = 2500, pool = Pool(44, 58, 59),
            color = new Color(0.65f, 0.4f, 0.95f) },
        // ---------------- 1.1.2 새 캐릭터: 해제 조건 달성 뒤 포인트로
        new CharacterDef {
            name = "지도 제작자", title = "길 잃은 측량사", body = "cartographer", held = "quill",
            description = "깃펜으로 세상을 그리는 측량사. 그은 선으로 적을 베고, 선을 이어 도형을 닫으면 안쪽을 통째로 짓이깁니다.",
            weapon = "깃펜", attack = "마우스 쪽으로 잉크 획을 그어 닿은 적을 벱니다. 획은 앞 획의 끝에서 이어지고, 이어진 획으로 도형을 닫으면 안쪽의 적이 크게 다칩니다",
            skill = "지도 접기", skillDesc = "누르고 있으면 접히는 선이 보이고, 떼면 선 양쪽의 적을 선 위로 끌어당겨 짓이깁니다. 오래 누를수록 넓게 접힙니다",
            hp = 0.95f, damage = 1.1f, attackSpeed = 0.85f, gauge = 1.1f, range = 9f, price = 5000, condition = CharUnlock.Endless10,
            pool = new[] { 60, 61, 62, 63, 64, 65, 66 }, color = new Color(0.35f, 0.55f, 0.95f) },
        Coming(CharUnlock.EndlessBosses5), Coming(CharUnlock.EasyFast), Coming(CharUnlock.EndlessClear), Coming(),
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
        return d.price == 0 || Prefs.GetInt(UnlockKey + (int)id, 0) == 1;
    }

    // 게임에서 고른 캐릭터 (트레일러 · 배치 모드 테스트는 거너)
    public static CharacterId Selected
    {
        get
        {
            if (Override.HasValue) return Override.Value;
            if (GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) return CharacterId.Gunner;
            CharacterId id = (CharacterId)Mathf.Clamp(Prefs.GetInt(SelectedKey, 0), 0, All.Length - 1);
            return IsUnlocked(id) ? id : CharacterId.Gunner;
        }
        set
        {
            if (!IsUnlocked(value)) return;
            Prefs.SetInt(SelectedKey, (int)value);
            Prefs.Save();
        }
    }

    public static CharacterDef Current => Def(Selected);
    public static bool IsGunner => Selected == CharacterId.Gunner;
    // 1.1.2 새 캐릭터 (지도 제작자 · 시계공 · 우체부 · 유령)
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
        Prefs.SetInt(UnlockKey + (int)id, 1);
        Prefs.Save();
        return true;
    }

    public static void Save() => Prefs.Save();

    // ================================================================= 해제 조건 (1.1.2)
    const string ConditionKey = "char.cond.";
    public static bool ConditionMet(CharUnlock c) => c == CharUnlock.None || Prefs.GetInt(ConditionKey + (int)c, 0) == 1;
    public static bool ConditionMet(CharacterId id) => ConditionMet(Def(id).condition);

    public static string ConditionText(CharUnlock c) => c switch
    {
        CharUnlock.Endless10 => Loc.T("무한 모드에서 10분 버티기"),
        CharUnlock.EndlessBosses5 => Loc.T("무한 모드 한 판에서 보스 5마리 처치"),
        CharUnlock.EasyFast => Loc.T("「쉬움」 난이도를 15분 안에 클리어"),
        CharUnlock.EndlessClear => Loc.T("무한 모드 클리어 (개발자 처치)"),
        _ => "",
    };

    // 게임 중 조건을 처음 달성했을 때 (한 번만 알림)
    public static void Achieve(CharUnlock c)
    {
        if (c == CharUnlock.None || ConditionMet(c) || GameInput.Auto || TutorialRun.Active || Demo.On || Application.isBatchMode) return;
        Prefs.SetInt(ConditionKey + (int)c, 1);
        Prefs.Save();
        if (StageManager.Instance != null)
            StageManager.Instance.ShowBanner(Loc.T("새 캐릭터 해제 조건 달성!") + "\n" + ConditionText(c), 4f);
        Hostile.Play("chime", 0.9f, 1.1f);
    }

    // 이 캐릭터가 지옥의 문에서 고를 수 있는 특수 능력인지
    public static bool InPool(int abilityId)
    {
        foreach (int id in Current.pool) if (id == abilityId) return true;
        return false;
    }
}
