using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 메뉴의 도감: 적 · 보스 · 무기 · 스킬 · 패시브 · 진화 · 레벨업 · 상점 정보를 탭별 카드로 한눈에 보여줌
// 자료는 Resources/CodexData (특수 능력, 아이콘, 적 프리팹)와 아래 설명 표에서 가져옴
public static class CodexUI
{
    static GameObject open;
    static int tab;
    static RectTransform grid;
    static ScrollRect scroll;
    static TMP_Text hint;
    static readonly List<Button> tabButtons = new List<Button>();

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.72f, 0.68f, 0.76f);
    static readonly Color Accent = new Color(1f, 0.72f, 0.55f);

    static readonly string[] Tabs = { "적", "보스", "캐릭터", "무기", "운명", "패시브", "진화", "레벨업", "상점" };
    static readonly string[] Hints =
    {
        "스테이지마다 나오는 적과 스킬 · 붉은 경고가 보이면 피하세요",
        "각 스테이지의 마지막 적 · 체력이 절반 아래면 특수 스킬, 필살기 게이지가 차면 결계를 펼칩니다",
        "메인 메뉴의 캐릭터에서 고릅니다 · 잠긴 캐릭터는 포인트로 해금하면 정보가 보입니다",
        "무기 진화로 얻는 무기 · 우클릭 필살기는 무기마다 다릅니다",
        "영혼 트리의 운명 가지 · 모든 캐릭터 공통 · 다른 어디에도 없는 효과",
        "영혼 트리의 영혼 가지에서 배우면 항상 적용되는 능력",
        "영혼 트리({UPGRADE})에서 배운 능력의 다음 칸을 배우면 진화합니다",
        "레벨이 오를 때 세 장 중 하나를 고르거나 건너뜁니다 · 최대 레벨 50 · 재료를 모두 올리면 스킬 진화",
        "상점 제단(적을 처치하다 보면 나타남)에서 코인으로 강화합니다",
    };

    static readonly string[] StageNames = { "지하 묘역", "불타는 지옥", "초원", "영혼의 심연" };

    // 적 이름 · 설명 (프리팹 이름으로 찾음)
    // 프리팹 이름으로 적 이름 (번역됨, 모르면 빈 문자열)
    public static string EnemyName(string prefab) => EnemyText.TryGetValue(prefab, out var t) ? Loc.T(t.name) : "";

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

    // 필살기 설명 (무기 ID 순서) · 즉발 여부
    static readonly string[] UltDesc =
    {
        "마우스 쪽으로 거대한 부채꼴 폭발을 3번 연달아 일으킵니다.",
        "우클릭을 누른 채 방향을 정하면 충전 뒤 화면을 가로지르는 굵은 광선을 쏩니다. 광선 위의 적이 많을수록 강합니다.",
        "두 줄기 나선으로 사방에 총알을 난사합니다. 움직이며 쓸 수 있습니다.",
        "우클릭을 누른 채 자리를 정하면 불기둥이 생겨 적을 빨아들이고 태웁니다.",
        "영혼 구슬들이 주위를 돌다 조준한 적에게 번갈아 달려듭니다.",
        "먹구름이 따라다니며 조준한 적들에게 번개를 연달아 떨어뜨립니다.",
        "무적 상태로 조준한 적들 사이를 순간이동하며 벤 뒤 제자리로 돌아옵니다.",
        "플레이어에서 마우스 쪽으로 줄지어 운석이 떨어집니다.",
    };
    static readonly bool[] UltInstant = { true, false, true, false, false, false, false, true };

    static readonly (string name, string desc)[] LevelUps =
    {
        ("관통하는 총알", "총알이 적을 1마리 더 관통합니다."),
        ("코인충", "적이 코인을 1개 더 떨어뜨립니다."),
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
        ("fx_prompt", "능력치 상점", "공격력 · 공격 속도 · 재장전 속도 · 탄창 · 이동 속도를 올립니다. 살수록 값이 오릅니다."),
        ("fx_muzzle", "무기 진화", "모든 캐릭터. 스테이지 보스를 쓰러뜨릴 때마다 무기가 세 갈래 중 하나로 진화합니다. 거너는 무기가 바뀌고, 다른 캐릭터는 평타 자체가 바뀝니다. 되돌릴 수 없습니다."),
        ("fx_reticle", "영혼 트리", "모든 캐릭터. 영혼 조각으로 무기 · 필살기 · 스킬 · 생존 · 영혼 · 재물 여섯 가지의 칸을 배웁니다. 캐릭터마다 칸이 다르고, 배운 칸 다음 칸만 보이며, 무기 가지는 진화해도 그대로 이어집니다."),
    };

    // ================================================================= window
    public static void Open(Transform root)
    {
        if (open != null) return;
        RectTransform win = UIKit.Modal(root, "CodexPanel", new Vector2(1640f, 960f), out open);
        UIKit.Text(win, "도감", 50f, Gold, new Vector2(0f, 425f), new Vector2(600f, 70f));
        UIKit.MakeButton(win, "닫기", new Vector2(720f, 425f), new Vector2(150f, 58f), Close, 22f);

        tabButtons.Clear();
        float w = 164f;
        for (int i = 0; i < Tabs.Length; i++)
        {
            int t = i;
            tabButtons.Add(UIKit.MakeButton(win, Tabs[i], new Vector2((i - (Tabs.Length - 1) * 0.5f) * (w + 12f), 345f), new Vector2(w, 64f), () => Show(t), 26f));
        }
        hint = UIKit.Text(win, "", 22f, Dim, new Vector2(0f, 287f), new Vector2(1500f, 36f));

        BuildScroll(win);
        Show(0);
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
        g.cellSize = new Vector2(756f, 240f);
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

        CodexData data = CodexData.Load();
        switch (tab)
        {
            case 0: Enemies(data); break;
            case 1: Bosses(data); break;
            case 2: Characters(); break;
            case 3: Specials(data, SpecialKind.Weapon); break;
            case 4: FateCards(); break;
            case 5: Specials(data, SpecialKind.Passive); break;
            case 6: Evolutions(data); break;
            case 7: LevelUpCards(data); break;
            case 8:
                foreach (var s in Shops) Card(FxIcon(s.icon), Gold, Loc.T(s.name), "", Loc.T(s.desc));
                KitShops();
                break;
        }
        scroll.verticalNormalizedPosition = 1f;
    }

    // ================================================================= tabs
    static void Enemies(CodexData data)
    {
        Card(FxIcon("fx_warn"), Color.white, Loc.T("중간 보스"), Loc.T("2장부터"),
             Loc.T("단계가 오를 때마다 크고 단단한 적이 나타납니다. 체력 12배 · 피해 1.5배. 쓰러뜨리면 영혼 조각을 많이 줍니다."));
        if (data == null || data.enemies == null) return;
        for (int i = 0; i < data.enemies.Length; i++)
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
        // 4장 적은 프리팹이 없어 도트와 AbyssStage 수치로
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
        if (data == null || data.bosses == null) return;
        for (int i = 0; i < data.bosses.Length; i++)
        {
            GameObject p = data.bosses[i];
            if (p == null) continue;
            bosss b = p.GetComponent<bosss>();
            int kind = b != null ? b.bossKind : i;
            string tag = Loc.T(StageNames[Mathf.Clamp(i, 0, 2)]);
            if (b != null) tag += "  ·  " + Stats(b.setEnemyHP, b.speed, b.contactDamage);
            Card(SpriteOf(p), Color.white, bossbar.BossName(kind), tag, Loc.T(BossDesc[Mathf.Clamp(kind, 0, 2)]) + "\n" + Accent.Tag(Loc.T(BarrierDesc[Mathf.Clamp(kind, 0, 2)])));
        }
        int mk = AbyssStage.MirrorKind;
        Card(AbyssStage.Frames("mirrorlord")[0], Color.white, bossbar.BossName(mk), Loc.T(StageNames[3]) + "  ·  " + Stats(AbyssStage.MirrorHp, 7.5f, 48f),
             Loc.T(BossDesc[mk]) + "\n" + Accent.Tag(Loc.T(BarrierDesc[mk])));
        // 1.0.4~ 무한 모드 최종 보스 「개발자」 · 「개발자 엄마」 (무한 모드가 열린 뒤에만 · 거울의 군주 몸 기준 속도 7.5 · 접촉 피해 48)
        if (!GameMode.IsUnlocked(Difficulty.Endless)) return;
        Sprite dev = AbyssStage.Frames("developer")[0];
        Card(dev, Color.white, bossbar.BossName(bossbar.DeveloperKind), Loc.T("무한 모드 15분") + "  ·  " + Stats(EndlessMode.DeveloperHp, 7.5f, 48f),
             Loc.T("15분에 깨어난 보스를 모두 쓰러뜨리면 강림합니다. 모든 영웅의 최종 진화 필살기를 쉬지 않고 겹쳐 씁니다 (예고는 그대로). 나온 뒤로는 잡몹이 나오지 않습니다."));
        Card(dev, EndlessMode.MomTint, bossbar.BossName(bossbar.DeveloperMomKind), Loc.T("무한 모드 30분") + "  ·  " + Stats(EndlessMode.MomHp, 15f, 96f),
             Loc.T("30분에 나타납니다. 개발자의 모든 능력치가 2배 — 체력 · 피해 · 이동 속도 2배, 필살기를 2배 더 자주 씁니다. 개발자와 엄마를 모두 쓰러뜨리면 무한 모드 클리어."));
    }

    static void Specials(CodexData data, SpecialKind kind)
    {
        if (kind == SpecialKind.Weapon)
            Card(FxIcon("fx_muzzle"), Gold, Loc.T("기본 권총"), Loc.T("처음부터 가진 무기"),
                 Loc.T("좌클릭으로 사격, R로 재장전.") + "\n" + Accent.Tag(Loc.T("필살기") + " · " + SpecialAbilities.UltName(SpecialAbilities.PistolUlt))
                 + "  " + Loc.T("우클릭을 누른 채 적을 조준하고, 떼면 조준한 적들에게 한꺼번에 쏩니다."));
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
                body += "\n" + Accent.Tag(Loc.T("필살기") + " · " + SpecialAbilities.UltName(id) + " (" + Loc.T("꾹 눌러 조준") + ")")
                        + "  " + Loc.T(UltDesc[id]);
            }
            tag += "  ·  " + Loc.T("거너");
            Card(d.icon, Color.white, Loc.T(d.name), tag, body);
        }
        KitSpecials(kind);
    }

    static void Evolutions(CodexData data)
    {
        WeaponEvolutions(data);
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

    // 캐릭터별 무기 진화 (거너 + 해금한 캐릭터): 1차 세 갈래 · 2차 세 갈래를 판 시작 전에 계획할 수 있게
    static void WeaponEvolutions(CodexData data)
    {
        Color tint = new Color(1f, 0.85f, 0.55f);
        // 거너: 권총 그대로 + 1차 능력 → 2차 (같은 계열 극대화 또는 능력 하나 더)
        string gunner = Loc.T(CharacterData.Def(CharacterId.Gunner).name);
        foreach (int w in SpecialAbilities.Tier1Options)
        {
            SpecialDef d = data != null && data.specials != null && w < data.specials.Length ? data.specials[w] : null;
            if (d == null) continue;
            // 권총 그대로 + 능력, 필살기는 고른 무기의 것으로 (2차: 같은 계열 극대화 또는 능력 하나 더)
            string paths = "";
            foreach (int n in SpecialAbilities.Tier2Options(w))
                paths += (paths.Length > 0 ? " / " : "") + Loc.T(SpecialAbilities.GunAugName(n, n == w));
            Card(d.icon, tint, Loc.T(SpecialAbilities.GunAugName(w, false)), gunner + " \u00B7 " + Loc.T("1차 진화"),
                 Loc.T(SpecialAbilities.GunAugDesc(w, false)) + "  " + Accent.Tag(Loc.T(w == SpecialAbilities.DualId ? "필살기" : "필살기에 덧붙음") + " \u00B7 " + SpecialAbilities.UltName(w))
                 + "  <color=#9fd8ff>" + Loc.T("2차") + "</color> " + paths);
        }
        // 다른 캐릭터: 1차 형태 셋 · 2차 강화 셋
        foreach (CharacterId c in UnlockedKits())
        {
            string who = Loc.T(CharacterData.Def(c).name);
            foreach (int id in CharacterData.Def(c).pool)
            {
                if (!SpecialAbilities.IsKit(id) || !SpecialAbilities.KitIsWeapon(id)) continue;
                Card(Resources.Load<Sprite>("Icons/ability_" + id), tint, Loc.T(CharacterKit.FormAugName(id)), who + " \u00B7 " + Loc.T("1차 진화"),
                     Loc.T(CharacterKit.FormDesc(id)) + "  " + Accent.Tag(Loc.T("필살기에 덧붙음") + " \u00B7 " + Loc.T(SpecialAbilities.KitUltName(id))));
            }
            foreach (int a in CharacterKit.AugmentsFor(c))
                Card(Resources.Load<Sprite>(CharacterKit.AugIcon(a)), tint, Loc.T(CharacterKit.AugmentName(a)), who + " \u00B7 " + Loc.T("2차 진화"), Loc.T(CharacterKit.AugmentDesc(a)));
        }
    }

    // 운명 가지 (예전 액티브 스킬 자리): 트리와 같은 목록
    static void FateCards()
    {
        Color tint = new Color(0.8f, 0.7f, 1f);
        foreach (var f in SpecialAbilities.FateInfo)
            Card(Resources.Load<Sprite>("Icons/ability_" + f.icon), tint, Loc.T(f.name), Loc.T("운명") + " \u00B7 " + f.cost, Loc.T(f.desc));
    }

    static void LevelUpCards(CodexData data)
    {
        Card(Resources.Load<Sprite>("Icons/ability_205"), Color.white, Loc.T("스킬 진화"), Loc.T("모든 캐릭터") + " · 37" + Loc.T("종"),
             Loc.T("정해진 두세 장의 레벨업 카드를 모두 최대 단계로 올리면 하나로 합쳐져 새 능력이 생깁니다. 최대 레벨은 50 (무한 모드 100) 이라 모두 올릴 수 없으니 노릴 진화를 고르세요. 원하지 않는 카드는 건너뛸 수 있고, 레벨은 카드를 골라야 오릅니다. 조합은 메인 메뉴의 「스킬 진화」에서 볼 수 있습니다."));
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
                + "\n" + Accent.Tag(Loc.T("우클릭") + " · " + Loc.T(d.skill)) + "  " + Loc.T(d.skillDesc);
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
            string body = Loc.T(SpecialAbilities.KitDesc(id)).Replace("\n", " ");
            if (kind == SpecialKind.Weapon)
                body += "\n" + Accent.Tag(Loc.T("필살기에 덧붙음") + " · " + Loc.T(SpecialAbilities.KitUltName(id))) + "  " + Loc.T(SpecialAbilities.KitUltDesc(id));
            Card(Resources.Load<Sprite>("Icons/ability_" + id), Color.white, Loc.T(SpecialAbilities.KitName(id)), tag, body);
        }
    }

    // 해금한 캐릭터의 상점 업그레이드
    static void KitShops()
    {
        foreach (CharacterId c in UnlockedKits())
        {
            string ups = c switch
            {
                CharacterId.Swordsman => "장검 공격력 · 베기 사거리 · 베기 각도 · 필살기 위력 · 이동 속도",
                CharacterId.Rogue => "표창 공격력 · 투척 속도 · 표창 회수 속도 · 표창 주머니 · 이동 속도",
                CharacterId.Archer => "화살 공격력 · 시위 당기는 속도 · 관통력 · 필살기 위력 · 이동 속도",
                CharacterId.Alchemist => "플라스크 공격력 · 투척 속도 · 폭발 범위 · 필살기 위력 · 이동 속도",
                CharacterId.Cartographer => "깃펜 공격력 · 이동 속도",
                CharacterId.Clockmaker => "톱니 공격력 · 이동 속도",
                CharacterId.Postman => "편지 공격력 · 이동 속도",
                CharacterId.Ghost => "손길 공격력 · 이동 속도",
                _ => "",
            };
            if (ups == "") continue;
            string[] parts = ups.Split(new[] { " · " }, System.StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++) parts[i] = Loc.T(parts[i]);
            Card(CharacterUI.Portrait(c), Color.white, Loc.T("능력치 상점") + " · " + Loc.T(CharacterData.Def(c).name), Loc.T(CharacterData.Def(c).name) + " " + Loc.T("전용"),
                 string.Join(" · ", parts) + "\n" + Loc.T("공격 속도 · 재장전 · 탄창 대신 무기에 맞는 강화가 나옵니다."));
        }
    }

    // ================================================================= card
    static void Card(Sprite icon, Color tint, string title, string tag, string body)
    {
        RectTransform card = UIKit.Rect("Card", grid, Vector2.zero, new Vector2(756f, 240f));
        Image bg = card.gameObject.AddComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.24f, 0.2f, 0.3f, 1f);
        RectTransform inner = UIKit.Rect("Inner", card, Vector2.zero, new Vector2(740f, 224f));
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

        TMP_Text t = UIKit.Text(inner, "", 32f, Gold, new Vector2(90f, 80f), new Vector2(540f, 44f), TextAlignmentOptions.Left);
        t.text = title;
        TMP_Text g = UIKit.Text(inner, "", 20f, Dim, new Vector2(90f, 46f), new Vector2(540f, 28f), TextAlignmentOptions.Left);
        g.text = tag;
        TMP_Text b = UIKit.Text(inner, "", 22f, Parch, new Vector2(90f, -38f), new Vector2(540f, 136f), TextAlignmentOptions.TopLeft);
        b.fontSizeMin = 14f;
        b.text = body;
    }

    static string Stats(float hp, float speed, float damage) =>
        Loc.T("체력") + " " + hp.ToString("0") + "  ·  " + Loc.T("속도") + " " + speed.ToString("0.#") + "  ·  " + Loc.T("접촉 피해") + " " + damage.ToString("0");

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
        if (open != null) Object.Destroy(open);
        open = null;
    }
}
