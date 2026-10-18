using UnityEngine;

// 캐릭터 전용 레벨업 카드: 무기와 관련된 카드(관통 · 노려보는 눈빛 · 멀티 샷 · 밀어내기)만
// 캐릭터의 평타 · 우클릭에 맞는 특수 능력 카드로 바뀜 (기본 스펙을 올리지 않음). 나머지 카드는 모든 캐릭터가 같이 씀
// 거너도 총알이 하는 일을 바꾸는 카드 6장 (권총 · 진화한 무기 모두에 붙음, SpecialAbilities.GunCards)
public partial class LevelShop
{
    // 바뀌는 카드 번호
    const int PierceId = 0, GlareId = 3, MultiId = 6, KnockId = 7;
    // 도적만 쓰는 다섯 번째 칸: 모두에게서 빠진 11번(피의 굶주림) 자리를 빌림
    public const int HungerId = 11;
    // 캐릭터 카드 두 장 더 · 고를 카드가 모자랄 때만 나오는 비상 보급
    public const int ExtraAId = 12, ExtraBId = 13, SupplyId = 14;
    public static readonly int[] KitIds = { PierceId, MultiId, KnockId, GlareId, HungerId, ExtraAId, ExtraBId };

    class KitCard
    {
        public string name, desc;       // 카드 이름 · 한 문장 설명 (한국어 키)
        public string stat;             // 레벨마다 바뀌는 수치의 이름 (한국어 키)
        public string[] values;         // 레벨 1 ~ 최대의 수치 ("앞말|숫자|단위", 앞말 · 단위는 번역 키)
        public int icon;                // Resources/Icons/ability_<icon>
        public string zero;             // 1.0.4~: 배우기 전 기본 수치 (줄어드는 수치는 「없음 → 10」이 거꾸로 읽혀서, 있으면 「12 → 10」)
        public string iconPath;         // 2026-10-19~ 갈래 새 카드 그림 (Resources/Icons/Paths/<이름>, 없으면 icon 번호 그림)
        public int max => values.Length;    // 최대 선택 횟수
    }

    static KitCard Card(string name, string desc, int icon, string stat, params string[] values)
        => new KitCard { name = name, desc = desc, icon = icon, stat = stat, values = values };

    // 1.0.4~: 배우기 전 기본 수치를 붙임 (Card(...) 를 감쌈)
    static KitCard Zero(string zero, KitCard c) { c.zero = zero; return c; }

    // "공격력 |40%|" → "공격력 40%" (앞말 · 단위만 번역)
    static string Val(string v)
    {
        string[] p = v.Split('|');
        if (p.Length != 3) return Loc.T(v);
        string unit = p[2].Length > 0 ? Loc.T(p[2]) : "";
        // 영어 「번째마다」(th): 「3th」가 되지 않게 서수로 — 「every 3rd」
        if (Loc.Current == Loc.Lang.English && unit.StartsWith("th") && int.TryParse(p[1], out int n))
            return "every " + n + Ordinal(n) + unit.Substring(2);
        return (p[0].Length > 0 ? Loc.T(p[0]) : "") + p[1] + unit;
    }

    static string Ordinal(int n)
    {
        int h = n % 100;
        if (h >= 11 && h <= 13) return "th";
        return (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
    }

    // 모든 레벨업 카드 설명의 모양: 한 문장 + "수치  지금 ▶ 다음(금빛 굵게)"
    // 1.1.2~ 단계(「Lv 2 / 3」)는 글자 대신 카드 맨 위 단계 칸(LevelPips, LevelShop.Pips)으로 — 숫자 나열이 읽기 어렵다는 의견
    // HudView: 왼쪽 위 레벨업 스킬 칸 설명 (1.0.1) — 「지금 → 다음」 대신 지금 수치만, 그리고 최대 단계일 때의 수치(maxVal)
    public static bool HudView;
    public static string CardText(string sentence, string stat, string now, string next, int level, int max, string maxVal = null)
    {
        if (HudView)
        {
            // 단계는 HUD 칸의 단계 칸 · 툴팁 제목(Lv)에 있음
            string cap = max > 0 && level >= max ? "  <color=#FFD34D>MAX</color>"
                       : maxVal != null ? "\n<color=#A89C86>" + Loc.T("최대") + "  " + maxVal + "</color>" : "";
            return sentence + "\n<color=#F5D478>" + stat + "  " + now + "</color>" + cap;
        }
        return sentence + "\n" + StatLine(stat, now, next);
    }

    // 「공격력  20% ▶ 30%」: 이름은 금색 · 지금 값은 밝은 양피지색 · ▶ 는 흐리게 · 다음 값은 금빛 굵게 (1.1.2~)
    public static string StatLine(string stat, string now, string next)
        => "<color=#F5D478>" + stat + "</color>  <color=#E6DCC4>" + now + "</color> <color=#A89C86>▶</color> <b><color=#FFD34D>" + next + "</color></b>";

    static string KitText(KitCard c, int level)
    {
        string now = level > 0 ? Val(c.values[Mathf.Min(level, c.max) - 1]) : c.zero != null ? Val(c.zero) : Loc.T("없음");     // 1.0.4~ 기본 수치
        string next = level < c.max ? Val(c.values[level]) : Loc.T("최대");
        return CardText(Loc.T(c.desc), Loc.T(c.stat), now, next, level, c.max, c.max > 0 ? Val(c.values[c.max - 1]) : null);
    }

    static KitCard KitCardOf(int id) => KitCardOf(CharacterData.Selected, id);

    // 도감용 단계별 수치: 「튕긴 탄 피해  40% ▶ 55% ▶ 70%」 (1.1.2~ 예전엔 「40% / 55% / 70%」라 나눗셈처럼 읽힘) · 최대 단계 값은 금빛 굵게
    static string StepValues(KitCard c)
    {
        string[] v = new string[c.max];
        for (int i = 0; i < c.max; i++)
            v[i] = i == c.max - 1 && c.max > 1 ? "<b><color=#FFD34D>" + Val(c.values[i]) + "</color></b>" : Val(c.values[i]);
        return "<color=#F5D478>" + Loc.T(c.stat) + "</color>  <color=#E6DCC4>" + string.Join(" <color=#A89C86>▶</color> ", v) + "</color>";
    }

    // 도감용: 이 캐릭터의 전용 레벨업 카드 (이름 · 설명(레벨별 수치 포함) · 아이콘 번호)
    public static System.Collections.Generic.List<(string name, string desc, int icon)> KitCardsFor(CharacterId who)
    {
        var list = new System.Collections.Generic.List<(string, string, int)>();
        var ids = new System.Collections.Generic.List<int>(KitIds);
        for (int i = 0; i < SigCount; i++) ids.Add(SigFirstId + i);
        foreach (int id in ids)
        {
            KitCard c = KitCardOf(who, id);
            if (c == null) continue;
            list.Add((c.name, Loc.T(c.desc) + "\n" + StepValues(c), c.icon));
        }
        return list;
    }

    static KitCard KitCardOf(CharacterId who, int id)
    {
        if (IsSig(id)) return SigCardOf(who, id);       // 새 고유 스킬 10종 (LevelShop.Signature)
        if (id == Paths.ExtraId) return PathExtraCard(who);     // 2026-10-19~ 갈래 새 카드 31번 (LevelShop.Paths · 연금술사 「서리 장막」)
        if (Paths.IsPathId(id)) return null;                    // 입문 · 외길은 카드 모양이 달라 LevelShop.Paths 가 맡음
        switch (who)
        {
            case CharacterId.Gunner:
                return id switch
                {
                    PierceId => Card("도탄 사격", "총알이 적을 맞히면 가까운 다른 적에게 작은 탄이 튕겨 나갑니다.", 86, "튕긴 탄 피해", "|40%|", "|55%|", "|70%|"),
                    MultiId => Card("폭발 탄두", "몇 발마다 한 발은 맞은 자리에서 폭발합니다.", 87, "폭발하는 탄", "|5|번째마다", "|4|번째마다", "|3|번째마다"),
                    KnockId => Card("소각탄", "총알에 맞은 적이 2초 동안 불탑니다.", 88, "화상 (초당)", "공격력 |15%|", "공격력 |25%|", "공격력 |35%|"),
                    GlareId => Card("유도 탄두", "총알이 조준한 대로 날아가다 적 옆을 스치면 그 적 쪽으로 꺾여 맞습니다.", 89, "보정 범위", "|0.9|칸", "|1.3|칸", "|1.7|칸"),
                    ExtraAId => Card("전기탄", "총알이 적을 맞히면 확률로 번개가 주변 적 둘에게 튑니다.", 90, "번개 확률", "|15%|", "|25%|", "|35%|"),
                    ExtraBId => Card("장전 충격파", "장전을 시작하면 몸 주변에 충격파가 터져 적을 밀어냅니다.", 91, "충격파 피해", "공격력 |100%|", "공격력 |160%|", "공격력 |220%|"),
                    _ => null,
                };
            case CharacterId.Swordsman:
                return id switch
                {
                    PierceId => Card("날아가는 검기", "평타를 휘두르면 적을 꿰뚫는 검기가 날아갑니다.", 60, "검기 피해", "공격력 |30%|", "공격력 |45%|", "공격력 |60%|"),
                    MultiId => Card("흡혈 베기", "평타로 벤 적 하나당 체력을 회복합니다. (한 번 휘두를 때 최대 3마리까지)", 61, "적 하나당 회복", "|0.45|", "|0.8|", "|1.15|"),
                    KnockId => Card("쳐내기", "평타를 휘두르면 베기 범위 안으로 날아오는 적 투사체를 모두 베어 없앱니다.", 62, "투사체 베기", "켜짐||"),
                    ExtraAId => Card("굳건한 자세", "필살기를 모으는 동안 받는 피해가 줄어듭니다.", 77, "받는 피해", "|-30%|", "|-45%|", "|-60%|"),
                    ExtraBId => Card("연속 베기", "몇 번 휘두를 때마다 한 번은 사거리 1.4배 · 피해 1.6배의 강한 일격이 됩니다. (3단계: 강한 일격에 주변 충격파)", 78, "강한 일격", "|4|번째마다", "|3|번째마다", "|3|번째마다 + 충격파"),
                    GlareId => Card("칼바람", "필살기 뒤 칼바람이 몸을 감싸고 돌며 주변을 벱니다. (0.3초마다 공격력 35%)", 63, "지속 시간", "|2|초", "|3|초", "|4|초"),
                    _ => null,
                };
            case CharacterId.Rogue:
                return id switch
                {
                    PierceId => Card("도탄 표창", "표창이 적에 맞으면 가까운 다른 적에게 튕겨 날아갑니다.", 64, "튕기는 횟수", "|1|회", "|2|회", "|3|회"),
                    MultiId => Card("갈고리 표창", "표창에 맞은 적이 3초 동안 출혈을 입습니다.", 65, "출혈 (초당)", "공격력 |20%|", "공격력 |40%|", "공격력 |60%|"),
                    KnockId => Card("그림자 분신", "필살기를 시작한 자리에 분신이 남아 표창을 던집니다.", 66, "분신 지속", "|2|초", "|3|초", "|4|초"),
                    ExtraAId => Card("급소 노리기", "출혈 중인 적에게 주는 피해가 늘어납니다.", 79, "추가 피해", "|+30%|", "|+60%|", "|+90%|"),
                    ExtraBId => Card("표창 회수", "탄창이 비면 확률로 재장전 없이 절반을 되찾습니다.", 80, "회수 확률", "|25%|", "|50%|", "|75%|"),
                    HungerId => Card("사냥의 기세", "적을 처치할 때마다 필살기 게이지가 찹니다. 공격력이 오를수록 더 많이 찹니다. (최대 4배)", 76, "처치당 게이지", "|1%|", "|2%|", "|3%|", "|4%|", "|5%|"),
                    GlareId => Card("표창 폭풍", "필살기가 끝나는 자리에서 표창이 사방으로 퍼집니다.", 67, "표창 수", "|8|개", "|12|개", "|16|개"),
                    _ => null,
                };
            case CharacterId.Archer:
                return id switch
                {
                    PierceId => Card("분열 화살", "가득 당긴 화살이 처음 맞힌 적에게서 여러 갈래로 갈라집니다. (갈래마다 피해 40%)", 68, "갈래 수", "|2|갈래", "|3|갈래", "|4|갈래"),
                    MultiId => Card("메아리 화살", "화살을 쏘면 잠시 뒤 유령 화살이 같은 방향으로 한 발 더 날아갑니다.", 69, "유령 화살 피해", "|50%|"),
                    KnockId => Card("강풍 화살", "가득 당긴 화살에 맞은 적이 크게 밀려나고 잠깐 기절합니다.", 70, "기절 시간", "|0.4|초", "|0.6|초", "|0.8|초"),
                    ExtraAId => Card("정조준", "시위를 더 빨리 가득 당깁니다.", 81, "당기는 속도", "|+25%|", "|+50%|", "|+75%|"),
                    ExtraBId => Card("사냥감 표식", "가득 당긴 화살에 맞은 적은 4초 동안 피해를 더 받습니다.", 82, "추가 피해", "|+15%|", "|+25%|", "|+35%|"),
                    GlareId => Card("가시 덤불", "필살기가 떨어진 자리에 가시 덤불이 남아 적을 느리게 하고 찌릅니다.", 71, "지속 시간", "|3|초", "|4|초", "|5|초"),
                    _ => null,
                };
            case CharacterId.Alchemist:
                return id switch
                {
                    PierceId => Card("연쇄 반응", "플라스크 폭발로 쓰러진 적이 그 자리에서 한 번 더 터집니다.", 72, "연쇄 폭발 피해", "|40%|", "|60%|", "|80%|"),
                    MultiId => Card("급속 냉동", "빙결 시약이 적을 느리게 하는 대신 꽁꽁 얼립니다.", 73, "얼리는 시간", "|0.8|초", "|1.1|초", "|1.4|초"),
                    KnockId => Card("호문쿨루스", "작은 조수가 머리 위를 맴돌며 적에게 플라스크를 던집니다.", 74, "던지는 간격", "|2.7|초", "|2|초", "|1.3|초"),
                    ExtraAId => Card("원소 융합", "불타는 적에게 빙결 시약이 닿으면 증기 폭발이 일어납니다.", 83, "증기 폭발", "공격력 |150%|", "공격력 |220%|", "공격력 |290%|"),
                    ExtraBId => Card("끈적한 산성", "산성 웅덩이가 적을 40% 느리게 하고 더 오래 남습니다.", 84, "웅덩이 지속", "|3.5|초", "|4.5|초", "|5.5|초"),
                    GlareId => Card("파편 플라스크", "필살기가 터지며 작은 플라스크들이 흩어져 다시 터집니다.", 75, "파편 수", "|4|개", "|6|개", "|8|개"),
                    _ => null,
                };
            default:
                return NewHeroKitCard(who, id);         // 1.0.2 새 캐릭터 (LevelShop.NewHeroes)
        }
    }

    public static Sprite KitIcon(int id)
    {
        if (id == SupplyId) return Resources.Load<Sprite>("Icons/ability_85");
        if (Paths.IsSeed(id) || id == Paths.SingleId) return PathCardIcon(CharacterData.Selected, id);     // 입문 · 외길 (그림이 없으면 기존 아이콘)
        KitCard c = KitCardOf(id);
        return c != null ? KitCardIcon(c) : null;
    }

    // 카드 그림: 갈래 새 카드는 Icons/Paths 의 그림이 있으면 그것 (없으면 기존 아이콘 번호)
    static Sprite KitCardIcon(KitCard c)
    {
        Sprite s = c.iconPath != null ? Paths.Icon(c.iconPath) : null;
        return s != null ? s : Resources.Load<Sprite>("Icons/ability_" + c.icon);
    }

    void KitSetAbilitys()
    {
        foreach (int id in KitIds)
        {
            KitCard c = KitCardOf(id);
            if (c == null || id >= ability_name.Length) continue;
            ability_name[id] = Loc.T(c.name);
            ability_content[id] = KitText(Extend(c, id), ability_level[id]);        // 외길이면 늘어난 단계 수치까지 (LevelShop.Paths)
        }
    }

    bool KitTooltip(int id, out string tip)
    {
        tip = null;
        KitCard c = KitCardOf(id);
        if (c == null) return false;
        tip = KitText(Extend(c, id), ability_level[id]);
        SkillEvo evo = EvoUsing(id);
        if (evo != null) tip += "\n<color=#C9A0FF>" + Loc.T("진화") + " ◆ " + Loc.T(evo.name) + "</color>";
        if (CharacterData.IsGunner && ability_level[id] > 0 && !KitFits(id))
            tip += "\n<color=#9fd8ff>" + Loc.T("지금 무기에는 맞지 않아 대신 단계마다 무기 피해 +6%") + "</color>";
        return true;
    }

    // 거너 카드가 지금 무기에 맞는지 (안 맞으면 뽑히지 않음). 다른 카드는 늘 true
    public static bool KitFits(int id)
    {
        if (IsSig(id) || !CharacterData.IsGunner || SpecialAbilities.SharedInstance == null || KitCardOf(id) == null) return true;
        int g = id == PierceId ? SpecialAbilities.GunRicochet : id == MultiId ? SpecialAbilities.GunExplosive : id == KnockId ? SpecialAbilities.GunIncendiary
              : id == GlareId ? SpecialAbilities.GunHoming : id == ExtraAId ? SpecialAbilities.GunShock : SpecialAbilities.GunReloadWave;
        return SpecialAbilities.SharedInstance.CardFits(g);
    }

    // 캐릭터 카드면 효과를 적용하고 true
    bool KitApply(int id)
    {
        KitCard c = KitCardOf(id);
        if (c != null && CharacterData.IsGunner)
        {
            SpecialAbilities sp = SpecialAbilities.SharedInstance;
            if (sp == null) return false;
            int g = id == PierceId ? SpecialAbilities.GunRicochet : id == MultiId ? SpecialAbilities.GunExplosive : id == KnockId ? SpecialAbilities.GunIncendiary
                  : id == GlareId ? SpecialAbilities.GunHoming : id == ExtraAId ? SpecialAbilities.GunShock : SpecialAbilities.GunReloadWave;
            sp.gunCard[g]++;
            if (ability_level[id] >= CapOf(id)) ability_selected[id] = true;       // 외길이면 한 단계 더 (LevelShop.Paths)
            return true;
        }
        CharacterKit kit = CharacterKit.Instance;
        if (c == null || kit == null) return false;

        // 카드 칸 → CharacterKit.card 번호 (0 관통 · 1 멀티 샷 · 2 밀어내기 · 3 노려보는 눈빛 자리)
        int slot = id == PierceId ? 0 : id == MultiId ? 1 : id == KnockId ? 2 : id == HungerId ? 4 : id == ExtraAId ? 5 : id == ExtraBId ? 6 : 3;
        kit.card[slot]++;
        kit.CardPicked(slot);
        if (ability_level[id] >= CapOf(id)) ability_selected[id] = true;
        return true;
    }
}
