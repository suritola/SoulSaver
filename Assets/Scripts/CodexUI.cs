using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 메뉴의 도감: 적 · 보스 · 무기 · 스킬 · 패시브 · 진화 · 레벨업 · 상점 정보를 탭별 카드로 한눈에 보여줌
// 자료는 Resources/CodexData (특수 능력, 아이콘, 적 프리팹)와 아래 설명 표에서 가져옴
public static class CodexUI
{
    static GameObject open;
    public static bool IsOpen => open != null;          // 1.1.9~ 게임 씬 ESC 메뉴에서도 엶 (ESCmenu 가 같은 ESC 로 일시정지를 풀지 않게)
    static int tab;
    static RectTransform grid;
    static ScrollRect scroll;
    static TMP_Text hint;
    static readonly List<Button> tabButtons = new List<Button>();
    // 1.1~ 카드 높이: 캐릭터 탭은 본문(설명 + 기본 무기 + 필살기 + 전용 무기)이 길어 더 큼 (영어 11줄도 14px 로 들어가게)
    static GridLayoutGroup gridLayout;
    const float BaseCardH = 240f, HeroCardH = 300f, BossCardH = 500f;     // 보스: 설명 + 빈틈 · 기억 · 결계 줄 (4~5문단) · 개발자 · 엄마 고유 기본 공격 설명으로 400 → 500
    static float CardH = BaseCardH;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.72f, 0.68f, 0.76f);
    static readonly Color Accent = new Color(1f, 0.72f, 0.55f);

    // 1.2~ 발견형 도감: 유물 · 각인 · 비밀 탭을 더함 · 진화 · 유물 · 각인 · 비밀은 얻은 것만 자세히 (CodexSeen)
    // 1.1.9~ 13번째 「이야기」 탭 (편의성 2차 6 — 본 이야기를 글로 다시 읽기 · 수집률 · 보상에는 넣지 않음)
    // 1.2~ 14번째 「기억」: 영웅마다 기억 조각 6장 (MemoryShards · 도감 수집률 밖)
    static readonly string[] Tabs = { "적", "보스", "캐릭터", "무기", "운명", "패시브", "진화", "유물", "각인", "비밀", "레벨업", "상점", "이야기", "memory.section" };
    const int TabEvo = 6, TabRelic = 7, TabBrand = 8, TabSecret = 9, TabLevelUp = 10, TabShop = 11, TabStory = 12, TabMemory = 13;
    static CodexSeen.Cat? CatOf(int t) => t == TabEvo ? CodexSeen.Cat.Evo : t == TabRelic ? CodexSeen.Cat.Relic : t == TabBrand ? CodexSeen.Cat.Brand : t == TabSecret ? CodexSeen.Cat.Secret : (CodexSeen.Cat?)null;
    static readonly List<GameObject> tabBadges = new List<GameObject>();
    static Transform openRoot;
    static TMP_Text rateText;
    static RectTransform rateFill;
    static Button rewardButton;
    static bool tabWasOpened;       // 지금 그리는 탭을 이번 버전에서 열어 본 적이 있었는지 (이번 업데이트 항목 NEW)
    static readonly string[] Hints =
    {
        "스테이지마다 나오는 적과 스킬 · 붉은 경고가 보이면 피하세요",
        "각 스테이지의 마지막 적 · 체력이 절반 아래면 특수 스킬, 필살기 게이지가 차면 결계를 펼칩니다",
        "메인 메뉴의 캐릭터에서 고릅니다 · 잠긴 캐릭터는 포인트로 해금하면 정보가 보입니다",
        "무기 진화로 얻는 무기 · [{ULT}] 필살기는 무기마다 다릅니다",      // 1.1~ 바꾼 필살기 키 이름을 따름
        "영혼 트리의 운명 가지 · 모든 캐릭터 공통 · 다른 어디에도 없는 효과",
        "영혼 트리의 영혼 가지에서 배우면 항상 적용되는 능력",
        "codex.tab.evo",        // 1.2~ 발견형: 무기 진화 · 각성 · 스킬 진화 · 능력 진화
        "codex.tab.relic",
        "codex.tab.brand",
        "codex.tab.secret",
        "transcend.codex.levelhint",     // 1.0.4~ 성장 카드 · 1.1~ 초월 카드 · 1.2~ 초월 간격 이야기 모드 2번 · 무한 모드 3번
        "상점 제단(적을 처치하다 보면 나타남)에서 코인으로 강화합니다",
        "codex.tab.story",
        "memory.hint",
    };

    // 1.1.9~ 로딩 화면 팁 (SceneFlow): 탭 안내 가운데 따로 읽어도 뜻이 통하는 것만 (캐릭터 · 운명 · 패시브 탭 안내는 화면 이름을 가리켜 빼 둠)
    internal static IEnumerable<string> LoadingTips()
    {
        int[] use = { 0, 1, 3, TabEvo, TabRelic, TabBrand, TabSecret, TabLevelUp, TabShop };
        foreach (int i in use) if (i < Hints.Length) yield return Hints[i];
        yield return "weak.tip";            // 빈틈 (WeakSpot)
    }

    static readonly string[] StageNames = { "지하 묘역", "불타는 지옥", "초원", "영혼의 심연" };
    // 1.0.4~: 도감은 플레이 순서로 (예전엔 옛 순서 = 지하 묘역 먼저). 체험판도 정식판과 같은 초원 먼저 (2026-10-18~)
    static readonly int[] PlayOrder = { 2, 0, 1, 3 };
    static int PlaySlot(int stage) => System.Array.IndexOf(PlayOrder, Mathf.Clamp(stage, 0, 3));

    // 적 이름 · 설명 (프리팹 이름으로 찾음)
    // 프리팹 이름으로 적 이름 (번역됨, 모르면 빈 문자열)
    public static string EnemyName(string prefab) => EnemyText.TryGetValue(prefab, out var t) ? Loc.T(t.name) : "";

    // 1.1.9~ 멈춘 동안 적 위에 마우스 (PauseInspect · 편의성 3차 1): 프리팹 이름으로 설명 (번역됨, 모르면 빈 문자열)
    internal static string EnemyDesc(string prefab) => EnemyText.TryGetValue(prefab, out var t) ? Loc.T(t.desc) : "";
    // 보스 설명 + 빈틈 줄 + 기억 줄 (기억하는 보스 문구가 있을 때만) — 도감 카드 · 멈춘 화면 · 보스바 툴팁이 같이 씀 (보여 줄 때 한 번)
    internal static string BossDescOf(int kind)
    {
        if (kind < 0 || kind >= BossDesc.Length) return "";
        string s = Loc.T(BossDesc[kind]) + "\n" + Loc.T("weak.boss." + kind);
        string mem = "bossmem.codex." + kind;
        if (Loc.Has(mem)) s += "\n" + Loc.T(mem);
        return s;
    }

    // 개발자 · 엄마 설명 (일시정지 중 보스 위 마우스 · 보스바 툴팁): 영웅 흉내 + 고유 기본 공격 목록 + 빈틈 · 기억 줄
    internal static string DevDescOf(bool mom) => Loc.T(mom ? "codex.mom.tip" : "codex.dev.tip") + DevExtra(mom ? "bossmem.codex.mom" : "bossmem.codex.dev");

    // 개발자 · 엄마 카드 끝 줄 (BossDescOf 는 이 둘에 빈 글자)
    static string DevExtra(string memKey)
    {
        string s = "\n" + Loc.T("weak.boss.dev");
        if (Loc.Has(memKey)) s += "\n" + Loc.T(memKey);
        return s;
    }

    // 1.1.9~ 유물 이름 (메인 메뉴 기록 창 · 판 밖에서도)
    internal static string RelicName(RelicDef r)
    {
        if (r == null) return "";
        int id = r.abilityId;
        if (SpecialAbilities.IsKit(id)) return Loc.T(SpecialAbilities.KitName(id));
        CodexData data = CodexData.Load();
        SpecialDef d = data != null && data.specials != null && id >= 0 && id < data.specials.Length ? data.specials[id] : null;
        return d != null ? Loc.T(d.name) : r.id;
    }

    static readonly Dictionary<string, (string name, string desc)> EnemyText = new Dictionary<string, (string, string)>
    {
        { "Enermy", ("해골", "지하 묘역의 기본 적. 스킬 없이 곧장 달려듭니다.") },
        { "Enemy2", ("구울", "떨어질 곳에 원을 띄운 뒤 높이 뛰어 내려찍습니다.") },
        { "Enemy3", ("망령", "사라졌다가 표시한 곳에 나타나며 영혼 폭발을 일으킵니다.") },
        { "HellImp", ("임프", "지그재그로 움직이며 플레이어 자리에 X자 레이저를 쏩니다.") },
        { "HellFlameSkull", ("불꽃 해골", "가까이 오면 부풀어 오르다 폭발합니다.") },
        { "HellHound", ("지옥견", "경로를 보여준 뒤 일직선으로 돌진합니다.") },
        { "HellGolem", ("용암 골렘", "점점 넓어지는 세 번의 지면 파동을 일으킵니다.") },
        { "HellKnight", ("악마 기사", "칼날을 휘감고 회전하며 플레이어 쪽으로 미끄러집니다.") },
        { "MeadowSlime", ("초원 슬라임", "밟으면 아픈 산성 웅덩이를 남깁니다.") },
        { "GiantBee", ("거대 벌", "플레이어 주변에 꽃가루 구름을 뿌립니다. 안에 있으면 느려지고 조금씩 피해를 입습니다.") },
        { "MushroomBrute", ("버섯 거인", "주변에 버섯 지뢰를 심고 잠시 뒤 차례로 터뜨립니다.") },
        { "DireWolf", ("다이어울프", "울부짖어 주변 적을 잠시 빠르게 만듭니다.") },
        { "Treant", ("트렌트", "플레이어 쪽으로 뿌리 가시를 차례로 솟게 합니다.") },
        // 2.1.1 4장 영혼의 심연 (AbyssStage 가 코드로 만듦)
        { "AbyssWisp", ("떠도는 혼", "흔들리며 다가오다 가끔 빠르게 돌진합니다.") },
        { "ChainWraith", ("사슬 망령", "경로를 보여 준 뒤 사슬을 던져, 맞으면 앞까지 끌어당깁니다.") },
        { "VoidEye", ("공허 눈알", "부채꼴 양 끝을 보여 준 뒤 그 사이를 광선으로 훑습니다.") },
        { "SoulReaper", ("영혼 수확자", "둘레를 두 번 베어 냅니다. 두 번째는 바깥 고리라 안쪽이 안전합니다.") },
        { "AbyssColossus", ("심연 거상", "세 갈래로 땅을 가르며 영혼빛 가시를 솟게 합니다.") },
    };

    static readonly string[] BossDesc =
    {
        "추적 영혼구(쏴서 터뜨릴 수 있음) · 뼈 감옥(틈으로 빠져나가세요) · 저주 묘비(부수기 전까지 터짐) · 순간이동을 씁니다.\n특수: 망자의 의식 - 무적이 되어 영혼 등불 셋을 띄움. 등불을 모두 부수면 무적이 풀리고 휘청입니다 (받는 피해 1.5배)",
        "3단계로 싸웁니다. 돌진 연타 · 운석 낙하 · 화염 파동(틈으로 피하세요)\n체력 66%: 날개를 펴고 공중 탄막 · 내려찍기 / 33%: 가장자리가 용암으로 좁아지고 모든 공격이 빨라짐",
        "대점프 · 점액 방울 · 구르기 돌진을 씁니다. 맞을수록 작아지고 빨라집니다.\n특수: 체력이 절반이 되면 작은 슬라임 넷을 뱉어 냄",
        "플레이어의 평타를 흉내 내고, 분신 셋 중 진짜만 피해를 받는 분신술 · 순간이동 십자 낙인을 씁니다.\n특수: 되비친 기억 - 이번 판에 가장 많이 쓴 공격을 거꾸로 돌려줌",
    };

    // 보스 필살기 「결계」 (2.1~)
    static readonly string[] BarrierDesc =
    {
        "필살기 「망자의 묘역」 — 15초 동안 회전하는 영혼 광선 넷 · 틈이 있는 저주 고리 · 발밑에서 솟는 뼈 가시",
        "필살기 「연옥 낙화」 — 15초 동안 곳곳의 운석 비 · 내 자리의 거대 운석 · 틈이 있는 운석 행진을 번갈아",
        "필살기 「산성 범람」 — 9초 동안 결계 벽에 튕기는 산성 덩어리 · 쏟아지는 산성 비",
        "필살기 「거울의 방」 — 15초 동안 둘레의 거울들이 차례로 플레이어를 노려 광선을 되쏘고, 깨진 거울 조각이 쏟아집니다",
    };
    // 1.2.5~ 개발자 · 엄마 결계 (BossUltimate.Developer.cs) — 카드 설명 뒤에 붙음
    const string DevBarrierDesc = "필살기 「디버그 모드」 — 12초 동안 결계 안이 디버그 화면으로 바뀝니다. 예상 이동 경로에 찍히는 브레이크포인트, 위에서부터 한 줄씩 실행되는 코드 줄(주석 처리된 한 줄만 안전합니다), 결계 벽에 튕기는 버그, 회전하는 무한 루프 탄막이 이어지고, 결계 끝 무렵에는 플레이어를 가로지르는 거대한 X자 「강제 종료」를 씁니다. 체력이 절반 아래면 「강제 종료」를 한 번 더 씁니다.";
    const string MomBarrierDesc = "필살기 「엄마의 대청소」 — 14초 동안 결계 안을 대청소합니다. 내 자리를 내려치는 등짝 스매싱, 몸을 끌어당기며 먼지를 빨아들이는 청소기, 줄지어 떨어지는 빨래, 잔소리와 함께 쏟아지는 탄막이 이어지고, 통금 시간이 되면 결계가 절반 가까이 좁아집니다.";

    // 필살기 설명 (무기 ID 순서) · 즉발 여부
    static readonly string[] UltDesc =
    {
        "마우스 쪽으로 거대한 부채꼴 폭발을 3번 연달아 일으킵니다.",
        "[{ULT}]을 누른 채 방향을 정하면 충전 뒤 화면을 가로지르는 굵은 광선을 쏩니다. 광선 위의 적이 많을수록 강합니다.",
        "두 줄기 나선으로 사방에 총알을 난사합니다. 움직이며 쓸 수 있습니다.",
        "[{ULT}]을 누른 채 자리를 정하면 불기둥이 생겨 적을 빨아들이고 태웁니다.",
        "영혼 구슬들이 주위를 돌다 조준한 적에게 번갈아 달려듭니다.",
        "먹구름이 따라다니며 조준한 적들에게 번개를 연달아 떨어뜨립니다.",
        "무적 상태로 조준한 적들 사이를 순간이동하며 벤 뒤 제자리로 돌아옵니다.",
        "플레이어에서 마우스 쪽으로 줄지어 운석이 떨어집니다.",
    };
    static readonly bool[] UltInstant = { true, false, true, false, false, false, false, true };

    static readonly (string name, string desc)[] LevelUps =
    {
        ("관통하는 총알", "총알이 적을 1마리 더 관통합니다."),
        ("코인충", "코인을 주울 때 더 많이 얻습니다."),      // 1.1~ 실제 카드 · 스킬 진화 툴팁과 같게 (코인 1개당 획득량)
        ("재활용 에너지", "필살기 게이지가 20% 줄어 필살기를 더 자주 씁니다."),
        ("노려보는 눈빛", "필살기를 조준하는 동안 타겟이 된 적이 20% 더 느려집니다."),
        ("더 많은 경험치", "처치 경험치가 10% 늘어납니다."),
        ("코인 자석", "주변의 코인을 끌어옵니다. 범위 +4 (최대 4번)"),
        ("멀티 샷", "한 번에 쏘는 총알이 1발 늘지만 한 발당 피해는 줄어듭니다."),
        ("밀어내기", "총알이 적을 밀어내는 힘 +25% (최대 3번)"),
        ("강철같은 심장", "체력을 모두 회복하고 최대 체력이 12% 늘어납니다."),
        ("단단한 신체", "받는 피해 -12% (최대 3번)"),
        ("생명의 샘", "시간이 지나면 체력이 회복됩니다. 초당 +0.5 (최대 4번)"),
        ("피의 굶주림", "적을 처치할 때마다 체력 +1 회복 (최대 3번)"),
    };

    static readonly (string icon, string name, string desc)[] Shops =
    {
        // 1.0.4~: 지금 상점에 없는 공격 속도 · 재장전 · 탄창 · 캐릭터 전용 강화를 소개하던 것을 실제 창에 맞춤
        ("stall", "떠돌이 상점", "적을 처치하다 보면 나타나는 떠돌이 상점에서 코인으로 공격력 · 이동 속도를 올리고, 물약(회복 · 축소 · 신속 · 질풍)을 삽니다. 살수록 값이 오르고, 효과는 이번 판 동안만 남습니다."),
        ("fx_muzzle", "무기 진화", "모든 캐릭터. 스테이지 보스를 쓰러뜨릴 때마다 무기가 세 갈래 중 하나로 진화합니다. 거너는 무기가 바뀌고, 다른 캐릭터는 평타 자체가 바뀝니다. 되돌릴 수 없습니다."),
        // 1.1~ 이야기 모드도 3장 보스를 잡으면 각성 (SpecialAbilities.AwakenOpen)
        ("fx_muzzle", "무기 각성", "모든 캐릭터. 이야기 모드에서 3장 보스를, 무한 모드에서 세 번째 보스를 쓰러뜨리면 2차까지 진화한 무기가 각성합니다. 파괴(모든 공격 피해 +35%) · 질풍(공격 · 발사 간격 -25%) · 영혼(치명타 확률 +15%, 치명타 피해 +60%) 가운데 하나를 고릅니다."),
        ("fx_reticle", "영혼 트리", "모든 캐릭터. 영혼 조각으로 무기 · 필살기 · 운명 · 생존 · 영혼 · 재물 여섯 가지의 칸을 배웁니다. 캐릭터마다 칸이 다르고, 배운 칸 다음 칸만 보이며, 무기 가지는 진화해도 그대로 이어집니다."),
    };

    // ================================================================= window
    public static void Open(Transform root)
    {
        if (open != null) return;
        openRoot = root;
        CodexSeen.Migrate();            // 기존 기록(해금 조건 · 무한 모드 클리어)을 먼저 옮김
        RectTransform win = UIKit.Modal(root, "CodexPanel", new Vector2(1640f, 960f), out open);
        UIKit.Text(win, "도감", 50f, Gold, new Vector2(0f, 425f), new Vector2(600f, 70f));
        UIKit.MakeButton(win, "닫기", new Vector2(720f, 425f), new Vector2(150f, 58f), Close, 22f);

        // 1.2~ 왼쪽 위 수집률 · 오른쪽 수집 보상
        rateText = UIKit.Text(win, "", 24f, Gold, new Vector2(-560f, 442f), new Vector2(460f, 34f), TextAlignmentOptions.Left);
        RectTransform bar = UIKit.Rect("RateBar", win, new Vector2(-560f, 412f), new Vector2(460f, 14f));
        bar.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.17f, 0.26f, 1f);
        rateFill = UIKit.Rect("Fill", bar, Vector2.zero, Vector2.zero);
        rateFill.anchorMin = Vector2.zero;
        rateFill.anchorMax = new Vector2(0f, 1f);
        rateFill.pivot = new Vector2(0f, 0.5f);
        rateFill.offsetMin = rateFill.offsetMax = Vector2.zero;
        rateFill.gameObject.AddComponent<Image>().color = Gold;
        for (int k = 1; k < 4; k++)     // 25 · 50 · 75% 눈금
        {
            RectTransform tick = UIKit.Rect("Tick", bar, new Vector2(-230f + 460f * k / 4f, 0f), new Vector2(3f, 20f));
            tick.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.1f, 1f);
        }
        rewardButton = UIKit.MakeButton(win, "codex.rewards", new Vector2(520f, 425f), new Vector2(200f, 58f), OpenRewards, 22f);
        // 1.1.9~ 게임 씬(일시정지 메뉴)에서 열면 「스킬 진화」 버튼 (메인 메뉴에는 타일이 따로 있음 · 편의성 3차 1)
        bool inGame = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene";
        if (inGame)
        {
            Transform evoRoot = root;
            UIKit.MakeButton(win, "스킬 진화", new Vector2(330f, 425f), new Vector2(160f, 58f), () => SkillEvoCodexUI.Open(evoRoot), 22f);
        }
        // 1.1.9~ 검색 칸 (편의성 2차 14): 게임 씬에서는 「스킬 진화」 버튼을 피해 좁게
        //   메인 메뉴 (290, 425) 240×50 + 왼쪽 「검색」 라벨 · 게임 씬 (158, 425) 176×50 (라벨은 제목 글자와 닿아 뺌, 자리 글이 대신)
        Vector2 sPos = inGame ? new Vector2(158f, 425f) : new Vector2(290f, 425f), sSize = inGame ? new Vector2(176f, 50f) : new Vector2(240f, 50f);
        if (!inGame)
        {
            TMP_Text sLabel = UIKit.Text(win, "검색", 20f, Dim, sPos - new Vector2(sSize.x * 0.5f + 34f, 0f), new Vector2(64f, 40f));
            sLabel.fontSizeMin = 12f;
        }
        search = UIKit.MakeInput(win, GamePad.Active && !SteamManager.Initialized ? "키보드로 입력" : "이름이나 설명으로 찾기…", sPos, sSize, 20f);
        search.onValueChanged.AddListener(_ => searchAt = Time.unscaledTime + 0.15f);
        search.onSelect.AddListener(_ => SteamManager.ShowTextInput(Loc.T("검색"), search.text, s => { if (search != null) search.text = s; }));
        searchAt = -1f;
        searching = false;
        open.AddComponent<CodexSearchTick>();

        tabButtons.Clear();
        tabBadges.Clear();
        float w = 99f;          // 1.1.9~ 13탭 108 · 1.2~ 14탭(이야기 · 기억): 99 × 14 + 간격 8 × 13 = 1490 (양 끝 ±745, 창 1660 안)
        for (int i = 0; i < Tabs.Length; i++)
        {
            int t = i;
            Button b = UIKit.MakeButton(win, Tabs[i], new Vector2((i - (Tabs.Length - 1) * 0.5f) * (w + 8f), 345f), new Vector2(w, 64f), () => { ClearSearch(); Show(t); }, 26f);
            TMP_Text bt = b.GetComponentInChildren<TMP_Text>();
            if (bt != null) { bt.enableAutoSizing = true; bt.fontSizeMin = 14f; bt.fontSizeMax = 26f; }
            tabButtons.Add(b);
            tabBadges.Add(CatOf(i).HasValue || i == TabMemory ? Badge(b.transform, new Vector2(-24f, -6f)) : null);      // 1.2~ 기억 탭은 안 읽은 조각
        }
        RefreshHeader();
        hint = UIKit.Text(win, "", 22f, Dim, new Vector2(0f, 278f), new Vector2(1500f, 56f));      // 2026-10-16: 두 줄까지 (탭 아래끝 313 · 목록 위끝 238 사이) — 문장으로 다듬어 길어진 안내가 작아지지 않게
        hint.fontSizeMin = 16f;

        BuildScroll(win);
        Show(0);
        // 1.1~ ESC · 패드 B 로도 닫힘 · 1.1.9~ 검색 글이 있으면 ESC 는 검색만 비움 (두 번째 ESC 가 닫기)
        //   위에 스킬 진화 도감이 열려 있으면 그것부터 (두 창의 ESC 처리 순서가 정해져 있지 않아도 위 창이 먼저 닫히게)
        UIKit.EscCancel(open, () =>
        {
            if (SkillEvoCodexUI.IsOpen) { SkillEvoCodexUI.Close(); return; }
            if (MemoryShards.ReaderOpen) { MemoryShards.CloseReader(); return; }      // 기억 읽기 창도 위 창이 먼저
            if (search != null && search.text.Length > 0) { ClearSearch(); Show(tab); }
            else Close();
        });
        WindowFit.Attach(win, new Vector2(1660f, 980f));      // 1.1~ UI 크기 120% · 좁은 화면에서 창이 화면 밖으로 나가지 않게
    }

    static void BuildScroll(RectTransform win)
    {
        RectTransform view = UIKit.Rect("Viewport", win, new Vector2(0f, -112f), new Vector2(1560f, 700f));
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        view.gameObject.AddComponent<RectMask2D>();

        grid = UIKit.Rect("Grid", view, Vector2.zero, new Vector2(1540f, 0f));
        grid.anchorMin = new Vector2(0.5f, 1f);
        grid.anchorMax = new Vector2(0.5f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.anchoredPosition = Vector2.zero;
        GridLayoutGroup g = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout = g;
        g.cellSize = new Vector2(756f, CardH);
        g.spacing = new Vector2(16f, 14f);
        g.padding = new RectOffset(6, 6, 10, 10);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 2;
        ContentSizeFitter fit = grid.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = grid;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
    }

    static void Show(int t)
    {
        tab = t;
        for (int i = grid.childCount - 1; i >= 0; i--) Object.Destroy(grid.GetChild(i).gameObject);
        for (int i = 0; i < tabButtons.Count; i++)
        {
            bool on = i == tab;
            tabButtons[i].GetComponent<Image>().color = on ? Gold : new Color(0.6f, 0.58f, 0.65f);
            tabButtons[i].transform.localScale = Vector3.one * (on ? 1.05f : 1f);
        }
        hint.text = Loc.T(Hints[tab]);
        if (tab == 0 && CurrentStage >= 0) hint.text = Loc.T("지금 장: {0}").Replace("{0}", Chapters.Title(CurrentStage)) + "  ·  " + hint.text;     // 1.1.9~ 판 안에서 열면 지금 장의 적이 맨 앞
        if (tab == TabMemory) hint.text = "<color=#F5D478>" + MemoryShards.HeaderLine + "</color>  " + hint.text;
        CodexSeen.Cat? cat = CatOf(tab);
        tabWasOpened = cat.HasValue && CodexSeen.TabOpenedThisVersion(cat.Value);
        if (cat.HasValue && CodexSeen.ShowAll) hint.text += "  <color=#F5D478>(" + Loc.T("codex.showall.on") + ")</color>";
        CardH = tab == 1 ? BossCardH : tab == 2 || tab == TabStory ? HeroCardH : BaseCardH;
        if (gridLayout != null) gridLayout.cellSize = new Vector2(756f, CardH);

        Build(tab, CodexData.Load());
        scroll.verticalNormalizedPosition = 1f;
        if (cat.HasValue) CodexSeen.TabViewed(cat.Value);       // 그린 뒤에 (이번에 보이는 NEW 는 그대로 두고 다음부터 끔)
        RefreshHeader();
    }

    // 탭 하나의 카드를 그리드에 그림 (Show · 검색이 같이 씀)
    static void Build(int t, CodexData data)
    {
        switch (t)
        {
            case 0: Enemies(data); break;
            case 1: Bosses(data); break;
            case 2: Characters(); break;
            case 3: Specials(data, SpecialKind.Weapon); break;
            case 4: FateCards(); break;
            case 5: Specials(data, SpecialKind.Passive); break;
            case TabEvo: Evolutions(data); break;
            case TabRelic: Relics(data); break;
            case TabBrand: Brands(); break;
            case TabSecret: Secrets(); break;
            case TabLevelUp: LevelUpCards(data); break;
            case TabMemory: MemoryShards.FillCodex(grid, CardH, openRoot); break;
            case TabShop:
                // 1.1~ 떠돌이 상점은 게임 속 상인 제단 그림 그대로 (예전 fx_prompt 는 어두운 말풍선 막대라 빈칸처럼 보였음)
                foreach (var s in Shops)
                {
                    bool stall = s.icon == "stall";
                    Card(stall ? ShopStall.StallSprite : FxIcon(s.icon), stall ? Color.white : Gold, Loc.T(s.name), "", Loc.T(s.desc));
                }
                break;
            case TabStory: Stories(data); break;
        }
    }

    // 판 안에서 열었으면 지금 장 번호 (아니면 -1)
    static int CurrentStage => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene" && StageManager.Instance != null ? StageManager.Instance.CurrentStage : -1;

    // ================================================================= 1.1.9~ 이야기 탭: 본 것만 글로 (안 본 것은 조건 한 줄 · 「도감 모두 보기」면 모두)
    static void Stories(CodexData data)
    {
        bool all = CodexSeen.ShowAll;
        bool classic = Chapters.IsClassic;
        string sec = Loc.T("시작 이야기");
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterId c = (CharacterId)i;
            if (!CharacterData.IsDeveloped(c) || !Demo.CharacterAllowed(c)) continue;
            CharacterDef d = CharacterData.Def(c);
            bool seen = all || Prefs.GetInt(StoryDirector.IntroSeenKey(false, classic, c), 0) == 1;
            StoryCard(CharacterUI.Portrait(c), Loc.T(d.name), sec + "  ·  " + Loc.T(d.title), seen, StoryDirector.IntroLinesFor(c, classic), "이 영웅으로 판을 시작하면 볼 수 있습니다.");
        }
        if (!Demo.On)
        {
            bool seen = all;
            for (int i = 0; i < CharacterData.All.Length && !seen; i++) seen = Prefs.GetInt(StoryDirector.IntroSeenKey(true, false, (CharacterId)i), 0) == 1;
            StoryCard(FxIcon("fx_warn"), Loc.T("무한 모드"), sec, seen, StoryDirector.EndlessIntroLines, "이 영웅으로 판을 시작하면 볼 수 있습니다.");
        }
        sec = Loc.T("보스 등장");
        for (int kind = 0; kind < StoryDirector.BossCount; kind++)
        {
            if (Demo.On && kind != 2) continue;      // 체험판은 1장 보스(킹 슬라임)만
            var b = StoryDirector.BossLines(kind);
            Sprite icon = null;
            if (kind == AbyssStage.MirrorKind) icon = AbyssStage.Frames("mirrorlord")[0];
            else if (data != null && data.bosses != null)
                foreach (GameObject p in data.bosses)
                {
                    bosss bb = p != null ? p.GetComponent<bosss>() : null;
                    if (bb != null && bb.bossKind == kind) { icon = SpriteOf(p); break; }
                }
            bool seen = all || Prefs.GetInt(StoryDirector.BossSeenKey(kind), 0) == 1;
            StoryCard(icon, Loc.T(b.name), sec + "  ·  " + Loc.T(b.title), seen, b.lines,
                      Loc.T("{0}에서 만나면 볼 수 있습니다.").Replace("{0}", Loc.T(StageNames[Mathf.Clamp(kind, 0, StageNames.Length - 1)])));
        }
        if (Demo.On) return;                         // 체험판은 1장에서 끝나 엔딩이 없음
        sec = Loc.T("엔딩");
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterId c = (CharacterId)i;
            if (!CharacterData.IsDeveloped(c)) continue;
            CharacterDef d = CharacterData.Def(c);
            bool seen = all || Prefs.GetInt("ending.seen." + i, 0) == 1;
            StoryCard(CharacterUI.Portrait(c), Loc.T(d.name), sec + "  ·  " + Loc.T(d.title), seen, StoryDirector.EndingLinesFor(c), "이 영웅으로 끝까지 클리어하면 볼 수 있습니다.");
        }
    }

    static void StoryCard(Sprite icon, string title, string tag, bool seen, string[] lines, string howKo)
    {
        if (!seen)
        {
            Card(icon, new Color(0.25f, 0.24f, 0.3f, 1f), "<color=#9E968A>" + title + "</color>", tag,
                 "<color=#A89C86>" + Loc.T("아직 보지 못한 이야기") + "\n" + Loc.T(howKo) + "</color>");
            return;
        }
        List<string> said = new List<string>();
        if (lines != null) foreach (string l in lines) said.Add(Loc.T(l));
        bool en = Loc.Current == Loc.Lang.English;
        string open = en ? "\u201C" : "\u300C", close = en ? "\u201D" : "\u300D";
        Card(icon, Color.white, title, tag, open + string.Join("\n", said) + close);
    }

    // ================================================================= 1.1.9~ 도감 검색 (편의성 2차 14)
    // 탭 빌더를 고치지 않고 「모든 탭을 그린 뒤 글자로 걸러냄」 — 「???」 카드는 빼고(스포일러) · 최대 60장 · NEW 처리(TabViewed)는 부르지 않음
    static TMP_InputField search;
    static float searchAt = -1f;
    static bool searching;
    const int MaxResults = 60;

    static void ClearSearch()
    {
        searching = false;
        searchAt = -1f;
        if (search != null && search.text.Length > 0) search.SetTextWithoutNotify("");
    }

    internal static void SearchTick()
    {
        if (searchAt < 0f || Time.unscaledTime < searchAt || search == null) return;
        searchAt = -1f;
        string q = search.text.Trim();
        if (q.Length < 2) { if (searching) { searching = false; Show(tab); } return; }
        RunSearch(q);
    }

    static void RunSearch(string q)
    {
        searching = true;
        for (int i = grid.childCount - 1; i >= 0; i--) { GameObject g = grid.GetChild(i).gameObject; g.SetActive(false); Object.Destroy(g); }
        foreach (Button b in tabButtons) { b.GetComponent<Image>().color = new Color(0.6f, 0.58f, 0.65f); b.transform.localScale = Vector3.one; }
        tabWasOpened = true;                     // 이번 업데이트 NEW 배지를 띄우지 않음
        CardH = BaseCardH;
        if (gridLayout != null) gridLayout.cellSize = new Vector2(756f, CardH);
        CodexData data = CodexData.Load();
        int found = 0;
        bool more = false;
        for (int t = 0; t < Tabs.Length; t++)
        {
            int before = grid.childCount;
            try { Build(t, data); }
            catch (System.Exception e) { Debug.LogWarning("[Codex] 검색 중 탭 " + t + ": " + e.Message); }
            for (int i = before; i < grid.childCount; i++)
            {
                GameObject card = grid.GetChild(i).gameObject;
                if (!card.activeSelf) continue;
                bool keep = !more && Matches(card, q, out bool hidden) && !hidden;
                if (keep && found >= MaxResults) { more = true; keep = false; }
                if (!keep) { card.SetActive(false); Object.Destroy(card); continue; }
                found++;
                // 왼쪽 위 작은 탭 이름 · 긴 본문은 줄임표
                TMP_Text tabTag = UIKit.Text(card.transform, Tabs[t], 18f, Dim, new Vector2(-300f, CardH * 0.5f - 22f), new Vector2(130f, 24f), TextAlignmentOptions.Left);
                tabTag.fontSizeMin = 12f;
                foreach (TMP_Text tx in card.GetComponentsInChildren<TMP_Text>(true)) tx.overflowMode = TextOverflowModes.Ellipsis;
            }
        }
        hint.text = found == 0 ? Loc.T("일치하는 항목이 없습니다.") : Loc.T("검색 결과 {0}개").Replace("{0}", found + (more ? "+" : ""));
        scroll.verticalNormalizedPosition = 1f;
    }

    static bool Matches(GameObject card, string q, out bool hidden)
    {
        hidden = false;
        System.Text.StringBuilder all = new System.Text.StringBuilder();
        foreach (TMP_Text t in card.GetComponentsInChildren<TMP_Text>(true))
        {
            string s = RunRecords.Plain(t.text);
            if (s == "???") hidden = true;
            all.Append(s).Append(' ');
        }
        return all.ToString().IndexOf(q, System.StringComparison.CurrentCultureIgnoreCase) >= 0;
    }

    // ================================================================= tabs
    static void Enemies(CodexData data)
    {
        Card(FxIcon("fx_warn"), Color.white, Loc.T("중간 보스"), Loc.T("2장부터"),
             Loc.T("단계가 오를 때마다 크고 단단한 적이 나타납니다. 체력 12배 · 피해 1.5배. 쓰러뜨리면 영혼 조각을 많이 줍니다."));
        if (data == null || data.enemies == null) return;
        List<int> order = new List<int>();
        for (int i = 0; i < data.enemies.Length; i++) order.Add(i);
        int StageOf(int i) => data.enemyStage != null && i < data.enemyStage.Length ? data.enemyStage[i] : 0;
        int cur = CurrentStage;
        int SlotOf(int i) => StageOf(i) == cur ? -1 : PlaySlot(StageOf(i));        // 1.1.9~ 판 안에서 열면 지금 장의 적부터
        order.Sort((x, y) => { int cmp = SlotOf(x).CompareTo(SlotOf(y)); return cmp != 0 ? cmp : x.CompareTo(y); });
        if (cur == 3) AbyssEnemies();
        foreach (int i in order)
        {
            GameObject p = data.enemies[i];
            if (p == null) continue;
            EnermyController e = p.GetComponent<EnermyController>();
            EnemyText.TryGetValue(p.name, out var txt);
            int stage = data.enemyStage != null && i < data.enemyStage.Length ? data.enemyStage[i] : 0;
            string tag = Loc.T(StageNames[Mathf.Clamp(stage, 0, 2)]);
            if (e != null) tag += "  ·  " + Stats(e.setEnemyHP, e.speed, e.contactDamage);
            Card(SpriteOf(p), Color.white, Loc.T(txt.name ?? p.name), tag, Loc.T(txt.desc ?? ""));
        }
        if (cur != 3) AbyssEnemies();
    }

    // 4장 적은 프리팹이 없어 도트와 AbyssStage 수치로
    static void AbyssEnemies()
    {
        foreach (var a in AbyssStage.CodexEnemies())
        {
            EnemyText.TryGetValue(a.name, out var txt);
            Card(AbyssStage.Frames(a.art)[0], Color.white, Loc.T(txt.name ?? a.name), Loc.T(StageNames[3]) + "  ·  " + Stats(a.hp, a.speed, a.contact), Loc.T(txt.desc ?? ""));
        }
    }

    static void Bosses(CodexData data)
    {
        Card(FxIcon("fx_rune"), new Color(0.79f, 0.63f, 1f), Loc.T("결계 침식"), Loc.T("모든 보스의 필살기"),
             Loc.T("보스를 때릴 때마다 보스 체력바 아래 보라색 게이지가 찹니다. 약한 공격 여러 번보다 센 한 방이 훨씬 많이 채우고, 한동안 때리지 않으면 아주 서서히 줄어듭니다. 가득 차면 맵 전체가 어두워지며 보스 둘레에 거대한 결계가 펼쳐지고, 15초 동안 보스마다 다른 공격이 쏟아집니다. 공격 자리마다 느낌표가 먼저 뜹니다.")
             + " " + Loc.T("보스전마다 적어도 한 번: 결계를 펼치기 전에 보스 체력이 40% 아래로 내려가면 게이지가 곧바로 가득 찹니다."));     // 1.0.4~: 40% 규칙도 적음
        // 빈틈 (WeakSpot): 규칙 · 중간 보스
        Card(FxIcon("fx_target_rune"), new Color(1f, 0.78f, 0.35f), Loc.T("weak.name"), Loc.T("weak.codex.tag"),
             Loc.T("weak.rule") + " " + Loc.T("weak.midboss"));
        // 1.2.5~ 보스가 둘 이상일 때의 통합 게이지 · 공간 침식 (무한 모드에서만 겹쳐 나와서 체험판에는 없음)
        if (!Demo.On)
            Card(FxIcon("fx_rune"), new Color(0.54f, 0.17f, 0.89f), Loc.T("공간 침식"), Loc.T("여러 보스의 필살기"),
                 Loc.T("보스가 둘 이상 함께 있으면 결계 게이지가 하나로 합쳐집니다. 어느 보스를 때려도 같은 게이지가 차고, 가득 차면 모든 보스의 결계가 하나로 무너지는 「공간 침식」이 펼쳐집니다. 15초 동안 맵 거의 전체가 결계가 되고, 모든 보스의 결계 공격이 더 짧은 간격으로 함께 쏟아집니다. 경고 시간은 그대로이고, 천천히 떠도는 「고요의 눈」 안에는 땅을 치는 공격이 떨어지지 않습니다. 보스가 하나만 남으면 남은 게이지를 그 보스가 그대로 이어받습니다."));
        if (data == null || data.bosses == null) return;
        List<int> order = new List<int>();
        for (int i = 0; i < data.bosses.Length; i++) order.Add(i);
        order.Sort((x, y) => { int cmp = PlaySlot(x).CompareTo(PlaySlot(y)); return cmp != 0 ? cmp : x.CompareTo(y); });
        foreach (int i in order)
        {
            GameObject p = data.bosses[i];
            if (p == null) continue;
            bosss b = p.GetComponent<bosss>();
            int kind = b != null ? b.bossKind : i;
            string tag = Loc.T(StageNames[Mathf.Clamp(i, 0, 2)]);
            if (b != null) tag += "  ·  " + Stats(b.setEnemyHP, b.speed, b.contactDamage);
            Card(SpriteOf(p), Color.white, bossbar.BossName(kind), tag, BossDescOf(Mathf.Clamp(kind, 0, 2)) + "\n" + Accent.Tag(Loc.T(BarrierDesc[Mathf.Clamp(kind, 0, 2)])));
        }
        int mk = AbyssStage.MirrorKind;
        Card(AbyssStage.Frames("mirrorlord")[0], Color.white, bossbar.BossName(mk), Loc.T(StageNames[3]) + "  ·  " + Stats(AbyssStage.MirrorHp, 7.5f, 48f),
             BossDescOf(mk) + "\n" + Accent.Tag(Loc.T(BarrierDesc[mk])));
        // 1.0.4~ 무한 모드 최종 보스 「개발자」 · 「개발자 엄마」 (무한 모드가 열린 뒤에만 · 거울의 군주 몸 기준 속도 7.5 · 접촉 피해 48)
        if (!GameMode.IsUnlocked(Difficulty.Endless)) return;
        Sprite dev = AbyssStage.Frames("developer")[0];
        Card(dev, Color.white, bossbar.BossName(bossbar.DeveloperKind), Loc.T("무한 모드 15분") + "  ·  " + Stats(EndlessMode.DeveloperHp, 7.5f, 48f),
             Loc.T("codex.dev.desc2") + DevExtra("bossmem.codex.dev") + "\n" + Accent.Tag(Loc.T(DevBarrierDesc)));
        Card(dev, EndlessMode.MomTint, bossbar.BossName(bossbar.DeveloperMomKind), Loc.T("개발자 처치 후 영혼 1000") + "  ·  " + Stats(EndlessMode.MomHp, 15f, 96f),
             Loc.T("codex.mom.desc2") + DevExtra("bossmem.codex.mom") + "\n" + Accent.Tag(Loc.T(MomBarrierDesc)));
    }

    static void Specials(CodexData data, SpecialKind kind)
    {
        if (kind == SpecialKind.Weapon)
            Card(FxIcon("fx_muzzle"), Gold, Loc.T("기본 권총"), Loc.T("처음부터 가진 무기"),
                 Loc.T("[{ATTACK}] 사격 · [{RELOAD}] 재장전.") + "\n" + Accent.Tag(Loc.T("필살기") + " · " + SpecialAbilities.UltName(SpecialAbilities.PistolUlt))     // 1.0.4~ 바꾼 키를 따름
                 + "  " + Loc.T("[{ULT}]을 누른 채 적을 조준하고, 떼면 조준한 적들에게 한꺼번에 쏩니다."));
        if (data == null || data.specials == null) return;
        for (int id = 0; id < data.specials.Length; id++)
        {
            SpecialDef d = data.specials[id];
            if (d == null || d.kind != kind) continue;
            string body = Loc.T(d.description).Replace("\n", " ");
            string tag = kind == SpecialKind.Weapon ? Loc.T("특수 무기") : kind == SpecialKind.Skill ? Loc.T("스킬") : Loc.T("패시브");
            if (kind == SpecialKind.Weapon && id < UltDesc.Length)
            {
                tag += "  ·  " + Loc.T("강화 특성") + ": " + SpecialAbilities.TraitName(id) + " (" + SpecialAbilities.TraitStep(id) + ")";
                // 1.1~ 쌍권총만 필살기가 바뀌고 나머지는 원래 조준 사격 뒤에 덧붙음 (진화 탭 · SpecialAbilities.Evolution 과 같게)
                string ultLabel = id == SpecialAbilities.DualId ? Loc.T("필살기") + " · " + SpecialAbilities.UltName(id) + " (" + Loc.T("꾹 눌러 조준") + ")"
                                                                : Loc.T("필살기에 덧붙음") + " · " + SpecialAbilities.UltName(id);
                body += "\n" + Accent.Tag(ultLabel) + "  " + Loc.T(UltDesc[id]);
            }
            tag += "  ·  " + Loc.T("거너");
            Card(d.icon, Color.white, Loc.T(d.name), tag, body);
        }
        KitSpecials(kind);
    }

    static void Evolutions(CodexData data)
    {
        // 1.2~ 스킬 진화 발견 수 (자세한 조합은 메인 메뉴 「스킬 진화」 도감 · 여기 수집률에도 들어감)
        int sf = 0, st = 0;
        foreach (CodexSeen.Entry e in CodexSeen.Entries)
        {
            if (e.cat != CodexSeen.Cat.Evo || !e.key.StartsWith("se.") || !CodexSeen.Available(e)) continue;
            st++;
            if (CodexSeen.Found(e.key)) sf++;
        }
        Card(Resources.Load<Sprite>("Icons/ability_205"), Purple, Loc.T("스킬 진화"), Loc.T("발견") + " " + sf + " / " + st,
             Loc.T("codex.skillevo.body"));
        WeaponEvolutions(data);
        Awakenings();
        // 능력 진화 (영혼 트리에 늘 보이는 칸이라 잠그지 않음 · 수집률에도 넣지 않음)
        if (data == null || data.specials == null) return;
        for (int id = 0; id < data.specials.Length && id < SpecialAbilities.EvolveTexts.Length; id++)
        {
            SpecialDef d = data.specials[id];
            if (d == null) continue;
            string kindName = d.kind == SpecialKind.Weapon ? Loc.T("특수 무기") : d.kind == SpecialKind.Skill ? Loc.T("스킬") : Loc.T("패시브");
            Card(d.icon, new Color(1f, 0.92f, 0.7f), Loc.T(d.name) + " · " + Loc.T("진화"), kindName,
                 Loc.T(SpecialAbilities.EvolveTexts[id]));
        }
    }

    static readonly Color Purple = new Color(0.79f, 0.63f, 1f);

    // 1.2~ 잠긴 항목 카드 (캐릭터 탭의 잠긴 캐릭터와 같은 방식: 그림은 검은 실루엣 · 이름 ??? · 힌트 한 줄)
    //   체험판에 없는 항목은 「정식판에서 만날 수 있습니다」
    static void Hidden(string key, Sprite icon, string tag, string hint, string since = null)
    {
        if (!CodexSeen.Available(key)) hint = Loc.T("정식판에서 만날 수 있습니다");
        Card(icon, Color.black, "???", tag, "<color=#A89C86>" + hint + "</color>", Fresh(key, since));
    }

    // NEW: 얻었는데 도감에서 아직 못 봤거나 · 이번 업데이트 항목인데 이번 버전에서 이 탭을 처음 엶
    static bool Fresh(string key, string since = null) => CodexSeen.Unviewed(key) || (!tabWasOpened && CodexSeen.IsLatest(since) && CodexSeen.Available(key));

    // 캐릭터별 무기 진화: 1차 세 갈래 · 2차 세 갈래 (1.2~ 얻은 것만 자세히, 잠긴 캐릭터도 실루엣으로)
    static void WeaponEvolutions(CodexData data)
    {
        Color tint = new Color(1f, 0.85f, 0.55f);
        string gunner = Loc.T(CharacterData.Def(CharacterId.Gunner).name);
        SpecialDef Def(int w) => data != null && data.specials != null && w >= 0 && w < data.specials.Length ? data.specials[w] : null;
        // 거너 1차: 권총 그대로 + 능력, 필살기는 고른 무기의 것으로
        foreach (int w in SpecialAbilities.Tier1Options)
        {
            SpecialDef d = Def(w);
            string key = CodexSeen.GunT1(w);
            string tag = gunner + " · " + Loc.T("1차 진화");
            if (!CodexSeen.Shown(key)) { Hidden(key, d != null ? d.icon : null, tag, Loc.T("codex.hint.evo1")); continue; }
            string paths = "";
            foreach (int n in SpecialAbilities.Tier2Options(w))
                paths += (paths.Length > 0 ? " / " : "") + (CodexSeen.Shown(CodexSeen.GunT2(w, n)) ? Loc.T(SpecialAbilities.GunAugName(n, n == w)) : "???");
            Card(d != null ? d.icon : null, tint, Loc.T(SpecialAbilities.GunAugName(w, false)), tag,
                 Loc.T(SpecialAbilities.GunAugDesc(w, false)) + "  " + Accent.Tag(Loc.T(w == SpecialAbilities.DualId ? "필살기" : "필살기에 덧붙음") + " · " + SpecialAbilities.UltName(w))
                 + "  <color=#9fd8ff>" + Loc.T("2차") + "</color> " + paths, Fresh(key));
        }
        // 거너 2차: 같은 계열 극대화 셋 + 다른 능력 다섯 (번개 그물은 두 갈래에서 나옴)
        HashSet<string> done = new HashSet<string>();
        foreach (int w in SpecialAbilities.Tier1Options)
            foreach (int n in SpecialAbilities.Tier2Options(w))
            {
                string key = CodexSeen.GunT2(w, n);
                if (!done.Add(key)) continue;
                bool maxed = n == w;
                List<string> from = new List<string>();
                foreach (int f in SpecialAbilities.Tier1Options)
                    if (System.Array.IndexOf(SpecialAbilities.Tier2Options(f), n) >= 0 && (maxed ? f == n : f != n))
                        from.Add(CodexSeen.Shown(CodexSeen.GunT1(f)) ? Loc.T(SpecialAbilities.GunAugName(f, false)) : "???");
                SpecialDef d = Def(n);
                string tag = gunner + " · " + Loc.T("2차 진화");
                if (!CodexSeen.Shown(key)) { Hidden(key, d != null ? d.icon : null, tag, Loc.T("codex.hint.evo2").Replace("{0}", string.Join(" · ", from))); continue; }
                string ult = maxed ? "" : "  " + Accent.Tag(Loc.T("필살기에 덧붙음") + " · " + SpecialAbilities.UltName(n));
                Card(d != null ? d.icon : null, tint, Loc.T(SpecialAbilities.GunAugName(n, maxed)), tag + " · " + string.Join(" · ", from),
                     Loc.T(SpecialAbilities.GunAugDesc(n, maxed)) + ult, Fresh(key));
            }
        // 다른 캐릭터: 1차 형태 셋 · 2차 강화 셋 (잠긴 캐릭터는 이름도 숨김)
        for (int i = 1; i < CharacterData.All.Length; i++)
        {
            CharacterId c = (CharacterId)i;
            if (!CharacterData.IsDeveloped(c)) continue;
            bool known = CharacterData.IsUnlocked(c) || CodexSeen.ShowAll;
            string who = known ? Loc.T(CharacterData.Def(c).name) : Loc.T("잠긴 캐릭터");
            foreach (int id in CharacterData.Def(c).pool)
            {
                if (!SpecialAbilities.IsKit(id) || !SpecialAbilities.KitIsWeapon(id)) continue;
                string key = "evo.f." + id;
                Sprite icon = Resources.Load<Sprite>("Icons/ability_" + id);
                string tag = who + " · " + Loc.T("1차 진화");
                if (!known || !CodexSeen.Shown(key)) { Hidden(key, icon, tag, Loc.T(known ? "codex.hint.evo1" : "codex.hint.lockedhero")); continue; }
                Card(icon, tint, Loc.T(CharacterKit.FormAugName(id)), tag,
                     Loc.T(CharacterKit.FormDesc(id)) + "  " + Accent.Tag(Loc.T("필살기에 덧붙음") + " · " + Loc.T(SpecialAbilities.KitUltName(id))), Fresh(key));
            }
            foreach (int a in CharacterKit.AugmentsFor(c))
            {
                string key = "evo.a." + a;
                Sprite icon = Resources.Load<Sprite>(CharacterKit.AugIcon(a));
                string tag = who + " · " + Loc.T("2차 진화");
                if (!known || !CodexSeen.Shown(key)) { Hidden(key, icon, tag, Loc.T(known ? "codex.hint.evo2k" : "codex.hint.lockedhero")); continue; }
                Card(icon, tint, Loc.T(CharacterKit.AugmentName(a)), tag, Loc.T(CharacterKit.AugmentDesc(a)), Fresh(key));
            }
        }
    }

    // 1.2~ 무기 각성 셋 (2차 진화 뒤 3장 보스 · 무한 모드 세 번째 보스)
    static void Awakenings()
    {
        Color tint = new Color(1f, 0.75f, 0.4f);
        for (int i = 0; i < SpecialAbilities.AwakenOptions.Length; i++)
        {
            int id = SpecialAbilities.AwakenOptions[i];
            string key = "evo.aw." + id;
            Sprite icon = i == 0 ? SpecialAbilities.StatIcon(0) : i == 1 ? SpecialAbilities.StatIcon(2) : SpecialAbilities.LvIcon(3);
            if (!CodexSeen.Shown(key)) { Hidden(key, icon, Loc.T("무기 각성"), Loc.T("codex.hint.awaken")); continue; }
            Card(icon, tint, Loc.T(SpecialAbilities.AwakenNames[i]), Loc.T("무기 각성") + " · " + Loc.T("모든 캐릭터"), Loc.T(SpecialAbilities.AwakenDescs[i]), Fresh(key));
        }
    }

    // ================================================================= 1.2~ 유물 (봉인된 석관)
    static void Relics(CodexData data)
    {
        foreach (RelicDef r in ContentRegistry.Relics)
        {
            int id = r.abilityId;
            string key = "relic." + r.saveCode;
            SpecialDef d = data != null && data.specials != null && id >= 0 && id < data.specials.Length ? data.specials[id] : null;
            Sprite icon = Resources.Load<Sprite>("Icons/ability_" + id);
            if (icon == null && d != null) icon = d.icon;
            string name = SpecialAbilities.IsKit(id) ? SpecialAbilities.KitName(id) : d != null ? d.name : "";
            string desc = SpecialAbilities.IsKit(id) ? SpecialAbilities.KitDesc(id) : d != null ? d.description : "";
            string tag = Loc.T("relic.tag");
            if (!CodexSeen.Shown(key)) { Hidden(key, icon, tag, Loc.T("codex.hint.relic"), r.since); continue; }
            Card(icon, Color.white, Loc.T(name), tag, SpecialAbilities.BodyText(id, desc).Replace("\n", " "), Fresh(key, r.since));
        }
    }

    // ================================================================= 1.2~ 영혼 각인
    static void Brands()
    {
        foreach (SoulBrandDef b in ContentRegistry.SoulBrands)
        {
            string key = "brand." + b.saveCode;
            Sprite icon = Resources.Load<Sprite>(ChoiceUI.IconPath(b.Icon));
            string tag = Loc.T("brand.title") + (b.pointMul > 1.001f ? "  ·  " + Loc.T("brand.points").Replace("{0}", b.pointMul.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture)) : "");
            if (!CodexSeen.Shown(key))
            {
                string hint;
                if (!SoulBrand.FeatureUnlocked) hint = Loc.T("brand.menu.locked");
                else if (!b.Unlocked) hint = Loc.T("brand.menu.unlock") + ": " + b.Hint + (b.progress != null ? "  (" + b.progress() + ")" : "");
                else hint = Loc.T("codex.hint.brand");
                Hidden(key, icon, Loc.T("brand.title"), hint, b.since);
                continue;
            }
            Card(icon, Color.white, "<color=#" + ColorUtility.ToHtmlStringRGB(b.color) + ">" + b.Name + "</color>", tag, b.Desc.Replace("\n", " · "), Fresh(key, b.since));
        }
    }

    // ================================================================= 1.2~ 비밀: 조건으로 만나는 영웅 · 무한 모드 최종 보스 둘 · 전설 상자 대박
    static void Secrets()
    {
        foreach (CharacterId c in CodexSeen.SecretHeroes())
        {
            string key = "sec.hero." + (int)c;
            CharacterDef d = CharacterData.Def(c);
            Sprite portrait = CharacterUI.Portrait(c);
            string cond = CharacterData.ConditionText(d.condition);
            if (!CodexSeen.Shown(key)) { Hidden(key, portrait, Loc.T("codex.sec.hero"), cond); continue; }
            string body = Loc.T(d.description) + "\n" + Accent.Tag(Loc.T("brand.menu.unlock")) + "  " + cond
                        + (CharacterData.IsUnlocked(c) ? "" : "\n" + Loc.T("codex.sec.hero.buy").Replace("{0}", d.price.ToString("N0")));
            Card(portrait, Color.white, Loc.T(d.name) + "  <size=70%>" + Loc.T(d.title) + "</size>", Loc.T("codex.sec.hero"), body, Fresh(key));
        }
        Sprite dev = AbyssStage.Frames("developer")[0];
        if (!CodexSeen.Shown("sec.dev")) Hidden("sec.dev", dev, Loc.T("codex.sec.boss"), Loc.T("codex.hint.dev"));
        else Card(dev, Color.white, bossbar.BossName(bossbar.DeveloperKind), Loc.T("codex.sec.boss") + "  ·  " + Loc.T("무한 모드 15분"),
                  Loc.T("codex.dev.secret"), Fresh("sec.dev"));      // 1.2.6: 결계 설명은 보스 탭 카드(높이 400)에만 (비밀 탭 카드 240 에서는 「…」로 잘렸음)
        if (!CodexSeen.Shown("sec.mom")) Hidden("sec.mom", dev, Loc.T("codex.sec.boss"), Loc.T("codex.hint.mom"));
        else Card(dev, EndlessMode.MomTint, bossbar.BossName(bossbar.DeveloperMomKind), Loc.T("codex.sec.boss") + "  ·  " + Loc.T("개발자 처치 후 영혼 1000"),
                  Loc.T("codex.mom.secret"), Fresh("sec.mom"));
        Sprite chest = Resources.Load<Sprite>(ChoiceUI.IconPath("chest"));
        if (!CodexSeen.Shown("sec.jackpot")) Hidden("sec.jackpot", chest, Loc.T("codex.sec.luck"), Loc.T("codex.hint.jackpot"));
        else Card(chest, Color.white, Loc.T("chest.jackpot.name"), Loc.T("codex.sec.luck") + "  ·  " + Loc.T("chest.legend.title"), Loc.T("codex.sec.jackpot.desc"), Fresh("sec.jackpot"));
    }

    // ================================================================= 1.2~ 수집률 · 보상 · NEW 배지
    static void RefreshHeader()
    {
        CodexSeen.Count(out int f, out int t);
        int pct = CodexSeen.Percent();
        if (rateText != null) rateText.text = Loc.T("codex.rate").Replace("{0}", pct.ToString()).Replace("{1}", f.ToString()).Replace("{2}", t.ToString());
        if (rateFill != null) rateFill.anchorMax = new Vector2(t > 0 ? Mathf.Clamp01((float)f / t) : 0f, 1f);
        for (int i = 0; i < tabBadges.Count; i++)
        {
            CodexSeen.Cat? c = CatOf(i);
            if (tabBadges[i] != null && c.HasValue) tabBadges[i].SetActive(i != tab && CodexSeen.TabNew(c.Value));
            else if (tabBadges[i] != null && i == TabMemory) tabBadges[i].SetActive(i != tab && MemoryShards.HasNew);
        }
        if (rewardButton != null)
        {
            NewGlow g = rewardButton.GetComponent<NewGlow>();
            bool can = CodexSeen.CanClaimAny;
            if (can && g == null) rewardButton.gameObject.AddComponent<NewGlow>();
            else if (!can && g != null) g.Remove();
        }
    }

    // 작은 금빛 「NEW」 알약 (NewGlow 배지와 같은 색)
    static GameObject Badge(Transform parent, Vector2 offset)
    {
        RectTransform r = UIKit.Rect("NewBadge", parent, Vector2.zero, new Vector2(54f, 24f));
        r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
        r.anchoredPosition = offset;
        Image pill = r.gameObject.AddComponent<Image>();
        pill.color = new Color(1f, 0.84f, 0.35f);
        pill.raycastTarget = false;
        TMP_Text t = UIKit.Text(r, "", 18f, new Color(0.15f, 0.08f, 0f), Vector2.zero, new Vector2(54f, 24f));
        t.text = "NEW";
        return r.gameObject;
    }

    static GameObject rewardsOpen;

    static void OpenRewards()
    {
        if (rewardsOpen != null || openRoot == null) return;
        RectTransform win = UIKit.Modal(openRoot, "CodexRewards", new Vector2(980f, 660f), out rewardsOpen);
        UIKit.EscCancel(rewardsOpen, CloseRewards);
        UIKit.Text(win, "codex.rewards.title", 42f, Gold, new Vector2(0f, 270f), new Vector2(800f, 60f));
        TMP_Text how = UIKit.Text(win, "codex.rewards.how", 20f, Dim, new Vector2(0f, 212f), new Vector2(880f, 56f));
        how.enableWordWrapping = true;
        int pct = CodexSeen.Percent();
        for (int i = 0; i < CodexSeen.Rewards.Length; i++)
        {
            var r = CodexSeen.Rewards[i];
            float y = 120f - i * 96f;
            RectTransform row = UIKit.Rect("Row", win, new Vector2(0f, y), new Vector2(880f, 84f));
            row.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.13f, 0.2f, 1f);
            TMP_Text p = UIKit.Text(row, "", 34f, pct >= r.pct ? Gold : Dim, new Vector2(-370f, 0f), new Vector2(120f, 60f));
            p.text = r.pct + "%";
            string what = r.points.ToString("N0") + " P";
            if (r.pct >= 100)
            {
                SkinDef s = SkinData.Get(CodexSeen.RewardSkin);
                if (s != null) what += " + " + Loc.T("codex.reward.skin").Replace("{0}", Loc.T(s.name))
                                     + "\n<size=75%><color=#A89C86>" + Loc.T("codex.reward.owned").Replace("{0}", CodexSeen.RewardSkinOwnedPoints.ToString("N0")) + "</color></size>";
            }
            TMP_Text wt = UIKit.Text(row, "", 24f, Parch, new Vector2(-20f, 0f), new Vector2(560f, 76f), TextAlignmentOptions.Left);
            wt.text = what;
            int at = r.pct;
            bool claimed = CodexSeen.IsClaimed(at), can = CodexSeen.CanClaim(at);
            Button b = UIKit.MakeButton(row, claimed ? "codex.claimed" : can ? "codex.claim" : "", new Vector2(350f, 0f), new Vector2(170f, 62f), () =>
            {
                string got = CodexSeen.Claim(at);
                if (got == null) return;
                Hostile.Play("chime", 0.9f, 1.1f);
                CloseRewards();
                OpenRewards();
                RefreshHeader();
                if (rewardsOpen != null)
                {
                    TMP_Text msg = UIKit.Text(rewardsOpen.transform.GetChild(0).GetChild(0), "", 24f, new Color(0.6f, 1f, 0.6f), new Vector2(0f, -298f), new Vector2(900f, 34f));
                    msg.text = Loc.T("codex.got").Replace("{0}", got);
                }
            }, 24f);
            if (!can)
            {
                b.interactable = false;
                b.GetComponent<Image>().color = new Color(0.45f, 0.43f, 0.5f);
                if (!claimed) { TMP_Text bt = b.GetComponentInChildren<TMP_Text>(); if (bt != null) bt.text = Loc.T("잠김"); }
            }
            else b.GetComponent<Image>().color = Gold;
        }
        UIKit.MakeButton(win, "닫기", new Vector2(0f, -246f), new Vector2(220f, 58f), CloseRewards, 24f);
    }

    static void CloseRewards()
    {
        if (rewardsOpen != null) Object.Destroy(rewardsOpen);
        rewardsOpen = null;
    }

    // 운명 가지 (예전 액티브 스킬 자리): 트리와 같은 목록
    static void FateCards()
    {
        Color tint = new Color(0.8f, 0.7f, 1f);
        foreach (var f in SpecialAbilities.FateInfo)        // 1.0.6~: 그림은 트리 칸과 같은 것 (단계마다 다름)
            Card(SpecialAbilities.TreeIcon(f.key) ?? Resources.Load<Sprite>("Icons/ability_" + f.icon), tint, Loc.T(f.name), Loc.T("운명") + " \u00B7 " + f.cost, Loc.T(f.desc));
    }

    static void LevelUpCards(CodexData data)
    {
        // 1.1~ 종 수는 실제 진화 정의에서 셈 (예전 「37」 고정은 캐릭터 5명 시절 숫자) · 최대 레벨은 LevelShop.LevelCap 과 같은 80
        Card(Resources.Load<Sprite>("Icons/ability_205"), Color.white, Loc.T("스킬 진화"), Loc.T("모든 캐릭터") + " · " + SkillEvoCount() + Loc.T("종"),
             Loc.T("정해진 두세 장의 레벨업 카드를 모두 최대 단계로 올리면 하나로 합쳐져 새 능력이 생깁니다. 최대 레벨은 50 (무한 모드 80) 이라 모두 올릴 수 없으니 노릴 진화를 고르세요. 원하지 않는 카드는 건너뛸 수 있고, 레벨은 카드를 골라야 오릅니다. 조합은 메인 메뉴의 「스킬 진화」에서 볼 수 있습니다."));
        // 끝없는 성장 (1.0.4~, LevelShop.Growth)
        Card(Resources.Load<Sprite>("Icons/ability_85"), Color.white, Loc.T("성장 카드"), Loc.T("모든 캐릭터") + " · 4" + Loc.T("종"),
             Loc.T("최대 레벨(50 · 무한 모드 80) 전에 고를 레벨업 카드를 모두 배우면, 그 뒤 레벨업마다 상한 없이 쌓이는 성장 카드 중 하나를 고릅니다. 힘: 모든 무기 피해 +2% · 생명: 최대 체력 +5 · 손놀림: 공격 속도 +1% · 발걸음: 이동 속도 +1%"));
        // 초월 카드 (1.1~, LevelShop.Transcend)
        Card(Resources.Load<Sprite>("Icons/choice_tr_echo"), Color.white, Loc.T("초월 카드"), Loc.T("모든 캐릭터") + " · " + LevelShop.TranscendCount + Loc.T("종"),
             Loc.T("transcend.codex.rule") + " " + Loc.T("transcend.codex.list"));       // 1.2~ 초월 간격 이야기 모드 2번 · 무한 모드 3번
        for (int i = 0; i < LevelUps.Length; i++)
        {
            // 무기 · 스킬 카드(관통 · 재활용 에너지 · 노려보는 눈빛 · 멀티 샷 · 밀어내기)는 더 이상 나오지 않음
            if (i == 0 || i == 2 || i == 3 || i == 6 || i == 7) continue;
            Sprite icon = data != null && data.abilityIcons != null && i < data.abilityIcons.Length ? data.abilityIcons[i] : null;
            if (i == 5) icon = Resources.Load<Sprite>("Icons/ability_104");      // 코인 자석
            Card(icon, Color.white, Loc.T(LevelUps[i].name), Loc.T("모든 캐릭터"), Loc.T(LevelUps[i].desc));
        }
        // 캐릭터 전용 카드 (무기 관련 카드 대신 나옴): 거너 + 해금한 캐릭터
        foreach (CharacterId c in System.Linq.Enumerable.Prepend(UnlockedKits(), CharacterId.Gunner))
            foreach (var k in LevelShop.KitCardsFor(c))
                Card(Resources.Load<Sprite>("Icons/ability_" + k.icon), Color.white, Loc.T(k.name), Loc.T(CharacterData.Def(c).name) + " " + Loc.T("전용"), Loc.T(k.desc).Replace("\n", " "));
    }

    // 1.1~ 스킬 진화 종 수: 만들어진 캐릭터마다 전용 진화 + 모든 캐릭터 공통 진화 (공통은 한 번만)
    static int SkillEvoCount()
    {
        int total = 0, common = 0;
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            if (!CharacterData.IsDeveloped((CharacterId)i)) continue;
            int c = 0;
            foreach (LevelShop.SkillEvo e in LevelShop.EvosFor((CharacterId)i))
                if (e.key.StartsWith("ce.")) c++; else total++;
            common = Mathf.Max(common, c);
        }
        return total + common;
    }

    // ================================================================= 캐릭터
    static System.Collections.Generic.IEnumerable<CharacterId> UnlockedKits()
    {
        for (int i = 1; i < CharacterData.All.Length; i++)
        {
            CharacterId c = (CharacterId)i;
            if (CharacterData.IsDeveloped(c) && CharacterData.IsUnlocked(c)) yield return c;
        }
    }

    static CharacterId? OwnerOf(int abilityId)
    {
        for (int i = 1; i < CharacterData.All.Length; i++)
            if (System.Array.IndexOf(CharacterData.All[i].pool, abilityId) >= 0) return (CharacterId)i;
        return null;
    }

    static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";

    static void Characters()
    {
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterId c = (CharacterId)i;
            if (!CharacterData.IsDeveloped(c)) continue;
            CharacterDef d = CharacterData.Def(c);
            Sprite portrait = CharacterUI.Portrait(c);
            if (!CharacterData.IsUnlocked(c))
            {
                // 잠긴 캐릭터: 모습 · 능력 모두 숨김
                Card(portrait, Color.black, "???", Loc.T("잠긴 캐릭터") + "  ·  " + d.price.ToString("N0") + " P",
                     Loc.T("포인트로 잠금을 풀면 이 캐릭터의 모습과 능력을 볼 수 있습니다."));
                continue;
            }
            string tag = Loc.T("체력") + " " + Pct(d.hp) + " · " + Loc.T("공격력") + " " + Pct(d.damage) + " · " + Loc.T("공격 속도") + " " + Pct(d.attackSpeed)
                         + " · " + Loc.T("필살기 충전") + " " + Pct(1f / Mathf.Max(0.1f, d.gauge)) + " · " + Loc.T("탄창") + " " + (d.mag > 0 ? d.mag + Loc.T("발") : Loc.T("무한"));
            string body = Loc.T(d.description)
                + "\n" + Accent.Tag(Loc.T("기본 무기") + " · " + Loc.T(d.weapon)) + "  " + Loc.T(d.attack)
                + "  (" + Loc.T("사거리") + " " + (d.range > 0f ? Loc.T("약 ") + d.range.ToString("0") + Loc.T("칸") : Loc.T("무한")) + ")"
                + "\n" + Accent.Tag(Loc.T("{ULT}") + " · " + Loc.T(d.skill)) + "  " + Loc.T(d.skillDesc);     // 1.0.4~ 바꾼 필살기 키 이름 (기본 우클릭)
            if (c != CharacterId.Gunner)
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (int id in d.pool) if (SpecialAbilities.IsKitWeapon(id)) names.Add(Loc.T(SpecialAbilities.KitName(id)));
                body += "\n" + Accent.Tag(Loc.T("전용 무기")) + "  " + string.Join(" · ", names);
            }
            Card(portrait, Color.white, Loc.T(d.name) + "  <size=70%>" + Loc.T(d.title) + "</size>", tag, body);
        }
    }

    // 해금한 캐릭터의 전용 무기 · 스킬 · 패시브
    static void KitSpecials(SpecialKind kind)
    {
        for (int id = SpecialAbilities.KitFirstId; id < SpecialAbilities.KitFirstId + SpecialAbilities.KitCount; id++)
        {
            if (SpecialAbilities.KitKind(id) != kind) continue;
            CharacterId? owner = OwnerOf(id);
            if (owner == null || !CharacterData.IsUnlocked(owner.Value)) continue;
            string tag = (kind == SpecialKind.Weapon ? Loc.T("특수 무기") : kind == SpecialKind.Skill ? Loc.T("스킬") : Loc.T("패시브"))
                         + "  ·  " + Loc.T(CharacterData.Def(owner.Value).name) + " " + Loc.T("전용");
            string desc = Loc.T(SpecialAbilities.KitDesc(id));
            // 새 영웅 진화 무기는 설명 마지막 줄이 「필살기에 덧붙음: X」 — 아래 태그 줄과 겹치므로 뺌
            int last = desc.LastIndexOf('\n');
            if (kind == SpecialKind.Weapon && last >= 0 && desc.Substring(last + 1).StartsWith(Loc.T("필살기에 덧붙음")))
                desc = desc.Substring(0, last);
            string body = desc.Replace("\n", " ");
            if (kind == SpecialKind.Weapon)
                body += "\n" + Accent.Tag(Loc.T("필살기에 덧붙음") + " · " + Loc.T(SpecialAbilities.KitUltName(id))) + "  " + Loc.T(SpecialAbilities.KitUltDesc(id));
            Card(Resources.Load<Sprite>("Icons/ability_" + id), Color.white, Loc.T(SpecialAbilities.KitName(id)), tag, body);
        }
    }

    // 1.0.4~: 캐릭터 전용 상점 강화 카드(KitShops)는 뺌 — 지금 상점 창에는 공격력 · 이동 속도 두 장만 나옴

    // ================================================================= card
    static void Card(Sprite icon, Color tint, string title, string tag, string body, bool fresh = false)
    {
        float extra = CardH - BaseCardH;        // 1.1~ 늘어난 높이: 제목 · 태그는 위쪽에 붙이고 본문 칸을 그만큼 키움
        RectTransform card = UIKit.Rect("Card", grid, Vector2.zero, new Vector2(756f, CardH));
        Image bg = card.gameObject.AddComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.24f, 0.2f, 0.3f, 1f);
        RectTransform inner = UIKit.Rect("Inner", card, Vector2.zero, new Vector2(740f, CardH - 16f));
        inner.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.16f, 0.97f);

        // 아이콘 칸
        RectTransform frame = UIKit.Rect("IconFrame", inner, new Vector2(-282f, 0f), new Vector2(160f, 160f));
        frame.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.17f, 0.26f, 1f);
        if (icon != null)
        {
            RectTransform ir = UIKit.Rect("Icon", frame, Vector2.zero, new Vector2(140f, 140f));
            Image img = ir.gameObject.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.color = tint;
            img.raycastTarget = false;
        }

        TMP_Text t = UIKit.Text(inner, "", 32f, Gold, new Vector2(90f, 80f + extra * 0.5f), new Vector2(540f, 44f), TextAlignmentOptions.Left);
        t.text = title;
        TMP_Text g = UIKit.Text(inner, "", 20f, Dim, new Vector2(90f, 46f + extra * 0.5f), new Vector2(540f, 28f), TextAlignmentOptions.Left);
        g.text = tag;
        TMP_Text b = UIKit.Text(inner, "", 22f, Parch, new Vector2(90f, -38f), new Vector2(540f, 136f + extra), TextAlignmentOptions.TopLeft);
        b.fontSizeMin = 14f;
        b.overflowMode = TextOverflowModes.Ellipsis;     // 그래도 넘치면 다음 카드에 겹치지 않고 줄임표 (기본 높이 카드 · 검색 결과도)
        b.text = body;
        if (fresh) Badge(card, new Vector2(-40f, -18f));        // 1.2~ 새로 얻었거나 이번 업데이트 항목
    }

    // 1.3.3~ 적 체력 공통 ×0.6 (GameMode.GlobalEnemyHpMul) 을 도감 숫자에도
    static string Stats(float hp, float speed, float damage) =>
        Loc.T("체력") + " " + Mathf.RoundToInt(hp * GameMode.GlobalEnemyHpMul).ToString("0") + "  ·  " + Loc.T("속도") + " " + speed.ToString("0.#") + "  ·  " + Loc.T("접촉 피해") + " " + damage.ToString("0");

    static string Tag(this Color c, string s) => "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + s + "</color>";

    static Sprite SpriteOf(GameObject prefab)
    {
        SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
        return sr != null ? sr.sprite : null;
    }

    static Sprite FxIcon(string name)
    {
        Sprite[] f = Fx.Frames(name);
        return f.Length > 0 ? f[0] : null;
    }

    public static void Close()
    {
        CloseRewards();
        MemoryShards.CloseReader();         // 도감 위에 뜬 기억 읽기 창도 함께
        if (open != null) Object.Destroy(open);
        open = null;
        search = null;
        searching = false;
        // 1.2~ 메인 메뉴 도감 버튼: 더 볼 NEW · 받을 보상이 없으면 반짝임을 걷음
        GameObject button = GameObject.Find("CodexButton");
        if (button == null) return;
        NewGlow glow = button.GetComponent<NewGlow>();
        bool want = CodexSeen.HasNew || MemoryShards.HasNew;      // 메뉴 도감 버튼과 같은 기준 (MenuExtras)
        if (glow != null && !want) glow.Remove();
        else if (glow == null && want) button.AddComponent<NewGlow>();
    }
}

// 1.1.9~ 도감 검색 디바운스 (멈춘 화면에서도 돎)
public class CodexSearchTick : MonoBehaviour
{
    void Update() => CodexUI.SearchTick();
}

// 1.1~ 도감 · 스킬 진화 도감처럼 큰 창: 캔버스(UI 크기로 바뀜)보다 크면 창을 통째로 줄임
//   inner 의 부모(창 테두리)를 줄임 · 화면 크기 · UI 크기가 바뀌어도 맞게 매 프레임 확인
public class WindowFit : MonoBehaviour
{
    RectTransform window, area;
    Vector2 need;

    public static void Attach(RectTransform inner, Vector2 need)
    {
        if (inner == null || inner.parent == null || inner.parent.parent == null) return;
        WindowFit f = inner.gameObject.AddComponent<WindowFit>();
        f.window = (RectTransform)inner.parent;
        f.area = (RectTransform)inner.parent.parent;        // 화면 전체를 덮는 판 (UIKit.Modal)
        f.need = need;
        f.LateUpdate();
    }

    void LateUpdate()
    {
        if (window == null || area == null) return;
        Vector2 have = area.rect.size;
        if (have.x <= 0f || have.y <= 0f) return;
        float s = Mathf.Min(1f, have.x / need.x, have.y / need.y);
        if (!Mathf.Approximately(window.localScale.x, s)) window.localScale = new Vector3(s, s, 1f);
    }
}
