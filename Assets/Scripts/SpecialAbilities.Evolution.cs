using System.Collections.Generic;
using UnityEngine;

// 거너의 새 성장 방식 (1.7.5~)
//  · 무기 진화: 스테이지 보스를 쓰러뜨릴 때마다 지금 무기에서 3갈래 중 하나로 진화 (되돌릴 수 없음)
//      권총 → 1차 (화염 방사기 / 영혼 저격총 / 저주받은 쌍권총) → 2차 (계열마다 강화판 3갈래)
//  · 영혼 트리 (1.7.7~): 가운데 = 지금 무기, 여섯 가지(무기 · 필살기 · 스킬 · 생존 · 영혼 · 재물)
//      앞 칸을 배워야 이어진 다음 칸이 열림 (그 너머는 화면에 보이지 않음)
//      무기 가지는 모든 무기에 공통이라 진화해도 그대로 이어짐. 들었던 무기마다 숙련 가지가 덧붙음
// 다른 캐릭터(1.7.8~)도 같은 방식: 진화하면 평타 자체가 바뀜 (CharacterKit.Forms), 트리는 캐릭터마다 다름
public partial class SpecialAbilities
{
    // 모든 캐릭터가 무기 진화 + 영혼 트리 (예전 지옥의 문 특수 능력 고르기 · 무기 교체는 없음)
    public static bool UsesEvolution => true;
    static bool Gunner => CharacterData.IsGunner;
    static CharacterKit Kit => CharacterKit.Instance;

    // ================================================================= 무기 진화
    public int EvolutionTier { get; private set; }         // 0 = 권총, 1 = 1차, 2 = 2차

    public static readonly int[] Tier1Options = { FlameId, SniperId, DualId };

    // 무한 모드 3번째 진화 (1.8.7~): 각성 — 2차까지 마친 무기에 덧붙는 공통 강화 (모든 캐릭터)
    public const int AwakenFirst = 9000;
    public static readonly int[] AwakenOptions = { AwakenFirst, AwakenFirst + 1, AwakenFirst + 2 };
    public static readonly string[] AwakenNames = { "파괴의 각성", "질풍의 각성", "영혼의 각성" };
    public static readonly string[] AwakenDescs =
    {
        "모든 공격 피해 +35% (기본 공격력 기준)",           // 1.1.2~ 평타 고점 하향: +50% → +35%
        "모든 공격 · 발사 간격 -25%",                       // -30% → -25%
        "치명타 확률 +15%, 치명타 피해 +60%",               // +20% · +80% → +15% · +60%
    };
    public static bool IsAwaken(int id) => id >= AwakenFirst && id < AwakenFirst + AwakenOptions.Length;
    // 1.1~: 이야기 모드도 3장 보스를 쓰러뜨리면 각성 (3장부터 · 이어하기로 4장에서 고르는 경우 포함, 4장 보스는 마지막이라 진화 없음)
    public static bool AwakenOpen => GameMode.IsEndless || Chapters.SlotOf(Chapters.CurrentStage) >= 2;

    // 1.1.2~ 평타 고점 하향 (각성은 판 끝까지 곱으로 쌓여 고점을 크게 키웠음): 파괴 +50% → +35% · 질풍 간격 ×0.7 → ×0.75 · 영혼 치명타 +20% / +80% → +15% / +60%
    const float AwakenAttack = 0.35f, AwakenRate = 0.75f, AwakenCrit = 0.15f, AwakenCritDamage = 0.6f;

    void ApplyAwaken(int id)
    {
        switch (id - AwakenFirst)
        {
            case 0: AddAttack(AwakenAttack); break;
            case 1: AddRate(AwakenRate); break;
            default: TreeCrit += AwakenCrit; TreeCritDamage += AwakenCritDamage; break;
        }
    }

    public static int[] Tier2Options(int tier1) => tier1 switch
    {
        FlameId => new[] { FlameId, GrenadeId, ShotgunId },
        SniperId => new[] { SniperId, SeekerId, ChainId },
        _ => new[] { DualId, ScytheId, ChainId },
    };

    // 다음 진화 선택지 (더 없으면 null)
    public int[] NextEvolutionOptions()
    {
        if (!UsesEvolution) return null;
        if (EvolutionTier == 2 && AwakenOpen) return AwakenOptions;
        if (!Gunner)
        {
            // 1차: 캐릭터 전용 무기 셋의 모양으로 평타가 바뀜 · 2차: 형태 위에 덧붙는 강화
            if (EvolutionTier == 0) return DropDeadEvolutions(KitFormOptions());
            if (EvolutionTier == 1) return DropDeadEvolutions(CharacterKit.AugmentsFor(CharacterData.Selected));
            return null;
        }
        if (EvolutionTier == 0) return Tier1Options;
        if (EvolutionTier == 1 && gunEvo1 >= 0) return Tier2Options(gunEvo1);
        return null;
    }

    // 1.4.1~ 지금 판에서 효과가 하나도 없는 진화는 빼고 보여 줌 (다 빠지면 원래대로)
    //   지도 제작자: 도형이 닫히지 않는 판(일필 · 대형 도면 · 행군 없음)이면 지구본(도형 1.5배 · 끌어모음 · +30%) · 판화(도형을 닫으면 잉크 폭발)는 효과 0
    static int[] DropDeadEvolutions(int[] options)
    {
        if (options == null || CharacterData.Selected != CharacterId.Cartographer || !Paths.Safe(() => CharacterKit.MapNoClosures, false)) return options;
        List<int> keep = new List<int>();
        foreach (int id in options) if (id != KitGlobe && id != CharacterKit.AugEngrave) keep.Add(id);
        return keep.Count > 0 ? keep.ToArray() : options;
    }

    // 진화 단계에 맞는 무기 이름 (2차에서 같은 무기를 고르면 새 이름)
    // 캐릭터 전용 무기 (지옥의 문에서 고르던 무기 셋) = 1차 진화 형태
    int[] KitFormOptions()
    {
        List<int> l = new List<int>();
        foreach (int id in CharacterData.Current.pool)
            if (IsKit(id) && id < abilities.Length && abilities[id].kind == SpecialKind.Weapon) l.Add(id);
        return l.ToArray();
    }

    // 2차에서 같은 무기를 다시 고른 강화판 이름 (도감)
    public static string MaxedName(int id) => Loc.T(GunAugName(id, true));

    public string EvolutionName(int id, int tier)
    {
        if (IsAwaken(id)) return Loc.T(AwakenNames[id - AwakenFirst]);
        if (id >= CharacterKit.AugFirst) return Loc.T(CharacterKit.AugmentName(id));
        if (id < 0) return Gunner ? Loc.T("기본 권총") : Loc.T(CharacterData.Current.weapon);
        // 무기가 아니라 기본 공격에 붙는 능력 이름 (2차에서 같은 계열이면 극대화 이름)
        if (IsKit(id)) return Loc.T(CharacterKit.FormAugName(id));
        if (GunAugName(id, false).Length > 0) return Loc.T(GunAugName(id, tier >= 2 && (id == FlameId || id == SniperId || id == DualId)));
        return Loc.T(abilities[id].name);
    }

    public string MainWeaponName
    {
        get
        {
            if (Gunner)
            {
                if (gunEvo1 < 0) return Loc.T("기본 권총");
                if (gunEvo2 < 0) return EvolutionName(gunEvo1, 1);
                return gunEvo2 == gunEvo1 ? EvolutionName(gunEvo2, 2) : EvolutionName(gunEvo1, 1) + " · " + EvolutionName(gunEvo2, 2);
            }
            CharacterKit k = Kit;
            if (k == null || k.form < 0) return EvolutionName(-1, 0);
            string n = EvolutionName(k.form, 1);
            return k.augment >= 0 ? n + " \u00B7 " + EvolutionName(k.augment, 2) : n;
        }
    }

    // 진화 카드 · 트리 가운데 그림
    public Sprite EvolutionIcon(int id)
    {
        if (IsAwaken(id)) return id == AwakenFirst ? StatIcon(0) : id == AwakenFirst + 1 ? StatIcon(2) : LvIcon(3);
        if (id >= CharacterKit.AugFirst) return Resources.Load<Sprite>(CharacterKit.AugIcon(id));
        if (id < 0) return Resources.Load<Sprite>("Weapons/weapon_" + (CharacterData.Current.held ?? "pistol"));
        Sprite s = Resources.Load<Sprite>("Weapons/weapon_" + id);
        return s != null ? s : abilities[id].icon;
    }

    // 지금 무기 그림 (트리 가운데 · 진화 연출)
    public Sprite MainWeaponIcon
    {
        get
        {
            if (Gunner) return EvolutionIcon(UltActive ? UltId : -1);
            CharacterKit k = Kit;
            return EvolutionIcon(k != null && k.form >= 0 ? k.form : -1);
        }
    }

    // 진화 카드 설명: 1차는 무기 설명, 2차는 진화한 무기 설명 + 진화 효과
    public string EvolutionDesc(int id, int tier)
    {
        if (IsAwaken(id)) return Loc.T(AwakenDescs[id - AwakenFirst]);
        if (id >= CharacterKit.AugFirst) return Loc.T(CharacterKit.AugmentDesc(id));
        // 기본 공격에 붙는 능력 + 바뀌는 필살기
        // 2.2.0~ 거너도 쌍권총만 필살기가 바뀌고, 나머지는 원래 조준 사격 뒤에 덧붙음
        string ult = "\n<color=#9fd8ff>" + Loc.T("필살기에 덧붙음") + " · " + UltName(id) + "</color>  ";
        // 2.1.8~ 거너가 아니면 우클릭은 원래 필살기 그대로, 진화 무기의 궁극기는 거기에 덧붙음 (세기 50%)
        if (!Gunner) return Loc.T(CharacterKit.FormDesc(id)) + "\n<color=#9fd8ff>" + Loc.T("필살기에 덧붙음") + " · " + UltName(id) + "</color>  " + Loc.T(KitUltDesc(id));
        bool maxed = tier >= 2 && id == gunEvo1;
        string keep = tier >= 2 && !maxed && gunEvo1 >= 0 ? "\n<color=#A89C86>" + Loc.T(GunAugName(gunEvo1, false)) + Loc.T(" 능력도 그대로") + "</color>" : "";
        return Loc.T(GunAugDesc(id, maxed)) + keep + ult + Loc.T(VolleyDesc(id));
    }

    // 들었던 무기 (권총 = -1부터 진화 순서대로): 무기마다 숙련 가지가 트리에 남음
    readonly List<int> weaponHistory = new List<int> { -1 };
    readonly Dictionary<int, int> historyTier = new Dictionary<int, int> { { -1, 0 } };

    // 진화: 지금 무기를 새 무기로 바꿈. 무기 가지(공통 강화)와 필살기 특성은 그대로 이어짐
    public void EvolveWeapon(int id)
    {
        EvolutionLog.Add(new KeyValuePair<string, float>(EvolutionName(id, EvolutionTier + 1), RunStats.Seconds));
        if (IsAwaken(id)) { ApplyAwaken(id); EvolutionTier = 3; return; }
        int tier = EvolutionTier + 1;
        // 1.4.1~ 필살기 특성 단계를 새 무기로 복사하지 않음 (예전엔 이전 무기 단계가 새 무기에 공짜로 붙고 이전 무기 단계도 그대로 남았음)
        //   원래 우클릭 · 이전 궁극기는 진화 뒤에도 나가므로 각자의 특성 줄(u.t<무기>.N)에 그대로 남음 — 옮길 것도 사라질 것도 없음
        if (!Gunner) { KitEvolve(id); return; }
        // 무기는 권총 그대로, 능력만 덧붙음 (SpecialAbilities.EvoAugments)
        if (tier == 1) gunEvo1 = id; else gunEvo2 = id;
        EvolutionTier = tier;
        if (!weaponHistory.Contains(id)) weaponHistory.Add(id);
        historyTier[id] = tier;
        if (fx != null)
        {
            fx.Play("pulse", 0.9f, 0.9f);
            fx.Play("chime", 0.8f, 1.1f);
            fx.Play("evolve", 0.8f, 1f);
            if (player != null) fx.FloatText(player.transform.position, EvolutionName(id, tier) + "!", new Color(1f, 0.85f, 0.4f), 6f, 0f);
        }
        RebuildHudRows();
    }
    // 다른 캐릭터: 평타의 형태(1차) · 강화(2차)를 바꿈
    void KitEvolve(int id)
    {
        CharacterKit k = Kit;
        if (k == null) return;
        int tier = EvolutionTier + 1;
        if (id >= CharacterKit.AugFirst) k.SetAugment(id);
        else
        {
            k.SetForm(id);
            if (!weaponHistory.Contains(id)) weaponHistory.Add(id);
        }
        historyTier[id] = tier;
        EvolutionTier = tier;
        if (fx != null) { fx.Play("pulse", 0.9f, 0.9f); fx.Play("chime", 0.8f, 1.1f); fx.Play("evolve", 0.8f, 1f); }
        RebuildHudRows();
    }

    // ================================================================= 영혼 트리
    public class SoulNode
    {
        public string key, parent;          // parent == null → 가운데 무기에서 바로 이어짐
        public int branch;                  // 0 무기 · 1 필살기 · 2 스킬 · 3 생존 · 4 영혼 · 5 재물
        public string name, desc;           // 번역된 글
        public int cost;
        public Sprite icon;
        public Sprite mark;                 // 아이콘 오른쪽 아래에 겹치는 작은 표식 (무기 숙련 · 패시브 진화 칸, 없으면 null)
        public System.Action apply;
        public bool hidden;                 // 지금은 해당 없음 (예: 탄창이 없는 무기의 탄창 칸)
        public System.Func<string> blocked; // 배울 수 없는 이유 (없으면 null) — 예: 스킬 칸이 가득
        public bool recommended;            // 지금 무기(형태)에 잘 맞는 칸 (트리에 금빛 표시)
    }

    public const int BranchCount = 6;
    public static readonly string[] BranchNames = { "무기", "필살기", "운명", "생존", "영혼", "재물" };
    readonly HashSet<string> ownedNodes = new HashSet<string>();
    public bool OwnsNode(string key) => ownedNodes.Contains(key);

    // ---------------- 트리가 올리는 값 (무기가 바뀌어도 그대로)
    public float TreeUltPower { get; private set; }         // 필살기 위력 +
    public float TreeGaugeMul { get; private set; } = 1f;   // 필살기 게이지 차는 속도
    public float TreeShardMul { get; private set; } = 1f;   // 영혼 조각 획득량
    public float TreeUltRefund { get; private set; }        // 필살기를 쓴 뒤 게이지가 이만큼 남음
    public float TreeBigShardMul { get; private set; } = 1f;    // 보스 · 중간 보스 영혼 조각 (영혼 끝 칸 「영혼 수확」, 1.0.1)
    public float TreeStormRange { get; private set; }       // 총알 폭풍 사거리 + (칸, 2.2.0)
    public float TreeRateMul { get; private set; } = 1f;    // 모든 무기 발사 간격 배율
    public float TreeMagMul { get; private set; } = 1f;     // 모든 무기 탄창 배율
    public float TreeReloadMul { get; private set; } = 1f;  // 모든 무기 장전 시간 배율
    public float TreeCrit { get; private set; }             // 치명타 확률 (피해 2배)
    public float TreeCritDamage { get; private set; } = 2f;
    public int TreePene { get; private set; }               // 관통 +
    public float TreeBulletSpeed { get; private set; } = 1f;
    public float TreeExecute { get; private set; }          // 체력 20% 아래 적에게 추가 피해 비율
    public float TreeCooldownMul { get; private set; } = 1f;
    float barrierEvery, barrierReadyAt;                     // 보호막: 이 시간마다 공격 한 번을 막음
    public float TreeLowHpDef { get; private set; }         // 체력 절반 아래일 때 받는 피해 감소
    public float TreeMend { get; private set; }             // 안 맞고 있으면 초당 최대 체력 비율 회복
    public float TreeDamageCap { get; private set; }        // 한 번에 잃는 체력 상한 (최대 체력 비율, 0 = 없음)
    public float TreeGoldChance { get; private set; }       // 처치 시 코인 3개 확률
    public float TreeShopMul { get; private set; } = 1f;    // 떠돌이 상점 가격 배율
    public int TreeInsight { get; private set; }            // 레벨업마다 영혼 조각
    float lastHurtAt = -99f;
    float interestRate, interestAt;                         // 이자: 30초마다 가진 코인의 일부

    float baseAttack = -1f;
    int basePistolMag;
    float BaseAttack => baseAttack > 0f ? baseAttack : player.damage;

    // 트리 전체 (숨김 칸 포함). 칸 위치는 이 목록으로 정해져서, 새 칸이 열려도 배치가 흔들리지 않음
    public List<SoulNode> BuildSoulTree()
    {
        if (baseAttack < 0f && player != null) { baseAttack = player.damage; basePistolMag = player.MaxBullet; }
        List<SoulNode> t = new List<SoulNode>();
        PlayerController p = player;
        Sprite atk = StatIcon(0), def = StatIcon(1), spd = StatIcon(2), rel = StatIcon(3), mov = StatIcon(4);

        // ---------------- 0 무기: 모든 무기에 공통 (진화해도 이어짐)
        t.Add(Node("w.dmg1", null, 0, "무기 화력", "모든 무기 피해 +10%", 6, atk, () => AddAttack(0.1f)));
        // 1.1.2~ 평타 고점 하향: 초반 두 칸(I · II)은 +10% 그대로, III ~ V 는 +10% → +6%
        Chain(t, "w.dmg", 2, 2, "w.dmg1", 0, "무기 화력", "모든 무기 피해 +10%", new[] { 14 }, atk, () => AddAttack(0.1f));
        Chain(t, "w.dmg", 3, 5, "w.dmg2", 0, "무기 화력", "모든 무기 피해 +6%", new[] { 26, 42, 64 }, atk, () => AddAttack(0.06f));
        t.Add(Node("w.rate1", "w.dmg1", 0, "연사 강화", "모든 무기 발사 간격 -8%", 10, spd, () => AddRate(0.92f)));
        // 1.1.2~ 평타 고점 하향: 첫 칸은 -8% 그대로, II ~ IV 는 -8% → -5% (발사 간격은 곱으로 쌓여 고점이 컸음)
        Chain(t, "w.rate", 2, 4, "w.rate1", 0, "연사 강화", "모든 무기 발사 간격 -5%", new[] { 22, 38, 60 }, spd, () => AddRate(0.95f));
        bool ammo = Gunner || (Kit != null && Kit.UsesAmmo);
        t.Add(Node("w.mag1", "w.rate1", 0, "큰 탄창", "모든 무기 탄창 +20%", 12, rel, () => AddMag(0.2f)));
        Chain(t, "w.mag", 2, 3, "w.mag1", 0, "큰 탄창", "모든 무기 탄창 +20%", new[] { 26, 44 }, rel, () => AddMag(0.2f));
        t.Add(Node("w.reload1", "w.mag1", 0, "재빠른 장전", "모든 무기 장전 시간 -12%", 14, rel, () => AddReload(0.88f)));
        Chain(t, "w.reload", 2, 3, "w.reload1", 0, "재빠른 장전", "모든 무기 장전 시간 -12%", new[] { 28, 46 }, rel, () => AddReload(0.88f));
        t.Add(Node("w.crit1", "w.dmg1", 0, "급소 사격", "치명타 확률 +8% (치명타는 피해 2배)", 16, LvIcon(3), () => TreeCrit += 0.08f));
        Chain(t, "w.crit", 2, 3, "w.crit1", 0, "급소 사격", "치명타 확률 +8%", new[] { 30, 50 }, LvIcon(3), () => TreeCrit += 0.08f);
        t.Add(Node("w.critdmg", "w.crit3", 0, "처참한 일격", "치명타 피해 2배 → 2.4배", 60, LvIcon(3), () => TreeCritDamage += 0.4f));     // 1.1.2~ 평타 고점 하향: 2.6배 → 2.4배     // 1.1~ 더하기로 (각성 · 초월 카드의 치명타 피해가 덮이지 않게)
        bool pierces = Gunner || CharacterData.Selected == CharacterId.Rogue || CharacterData.Selected == CharacterId.Archer;
        t.Add(Node("w.pene1", "w.crit1", 0, "철갑탄", "모든 총알 관통 +1", 24, LvIcon(0), () => AddPene()));
        t.Add(Node("w.pene2", "w.pene1", 0, "철갑탄 II", "모든 총알 관통 +1", 48, LvIcon(0), () => AddPene()));
        t.Add(Node("w.speed", "w.rate1", 0, "고속탄", "총알이 30% 더 빠르게 날아갑니다", 14, spd, () => TreeBulletSpeed *= 1.3f));
        foreach (SoulNode n in t)
        {
            if (!ammo && (n.key.StartsWith("w.mag") || n.key.StartsWith("w.reload"))) n.hidden = true;     // 탄창이 없는 캐릭터
            if (!pierces && n.key.StartsWith("w.pene")) n.hidden = true;
            if (!Gunner && n.key == "w.speed") n.hidden = true;
        }
        if (!Gunner) KitWeaponNodes(t);
        t.Add(Node("w.exec1", "w.dmg3", 0, "처형", "체력 20% 아래인 적에게 피해 +50%", 34, LvIcon(7), () => TreeExecute += 0.5f));
        t.Add(Node("w.exec2", "w.exec1", 0, "처형 II", "체력 20% 아래인 적에게 피해 +50%", 58, LvIcon(7), () => TreeExecute += 0.5f));

        // 들었던 무기마다 숙련 가지 (무기 화력에서 뻗음). 숙련 보너스는 다음 무기에도 이어짐
        foreach (int w in weaponHistory) AddMastery(t, w);

        // ---------------- 1 필살기
        Sprite ult = UltActive && UltId < abilities.Length ? abilities[UltId].icon : LvIcon(3);
        if (!Gunner)
        {
            // 캐릭터 우클릭 (회전 베기 · 출혈 돌진 · 화살비 · 대폭발) 전용 위력 (예전 상점 네 번째 줄)
            string un = UltName(PistolUlt);
            t.Add(RawNode("u.kit1", "u.power1", 1, un + " " + Loc.T("강화"), un + " " + Loc.T("위력 +10%"), 14, ult, () => { if (Kit != null) Kit.ultMul += 0.1f; }));
            t.Add(RawNode("u.kit2", "u.kit1", 1, un + " " + Loc.T("강화") + " II", un + " " + Loc.T("위력 +10%"), 30, ult, () => { if (Kit != null) Kit.ultMul += 0.1f; }));
            t.Add(RawNode("u.kit3", "u.kit2", 1, un + " " + Loc.T("강화") + " III", un + " " + Loc.T("위력 +10%"), 50, ult, () => { if (Kit != null) Kit.ultMul += 0.1f; }));
        }
        t.Add(Node("u.power1", null, 1, "필살 위력", "필살기 피해 +20%", 8, ult, () => TreeUltPower += 0.2f));
        Chain(t, "u.power", 2, 4, "u.power1", 1, "필살 위력", "필살기 피해 +20%", new[] { 20, 36, 56 }, ult, () => TreeUltPower += 0.2f);
        t.Add(Node("u.gauge1", "u.power1", 1, "빠른 충전", "필살기 게이지 차는 속도 +12%", 12, LvIcon(2), () => TreeGaugeMul += 0.12f));
        Chain(t, "u.gauge", 2, 3, "u.gauge1", 1, "빠른 충전", "필살기 게이지 차는 속도 +12%", new[] { 26, 44 }, LvIcon(2), () => TreeGaugeMul += 0.12f);
        // 필살기 특성: 무기마다 다름 (권총 타겟 수 · 화염 회오리 지속 · 레일건 굵기 …)
        // 1.4.1~ 무기마다 따로 된 줄 (u.t<무기>.1~3, 권총 · 원래 우클릭 = u.t-1.1~3). 예전엔 칸 하나(u.trait1~3)가 지금 무기를 따라 바뀌어,
        //   진화 전에 산 「타겟 수」가 진화 뒤 새 무기 특성으로 「배움」 처리되고(새 무기 특성은 공짜 · 다시 살 수 없음) 권총 쪽 값도 그대로 남았음
        //   원래 우클릭(일제 사격 · 캐릭터 필살기)은 진화 뒤에도 늘 나가므로 권총 줄은 늘 두고, 덧붙는 궁극기마다 줄을 하나씩
        AddUltTraitLine(t, PistolUlt, ult);
        foreach (int aw in AddedUltIds()) AddUltTraitLine(t, aw, ult);
        if (Gunner)
        {
            // 1.0.1: 쌍권총도 일제 사격으로 조준하므로 「노려보는 눈빛」은 늘, 덧붙는 총알 폭풍이 있으면 사거리 칸도
            if (UltActive && HasAug(DualId))     // 1.0.2: 2차를 다른 무기로 해도 총알 폭풍이 남으므로
            {
                // 1.4.1~ 쌍권총 특성 줄 첫 칸에서 (예전엔 u.trait1 이라 권총 시절에 산 「타겟 수」만으로 바로 열렸음)
                t.Add(Node("u.storm1", UltTraitKey(DualId, 1), 1, "폭풍 사거리", "총알 폭풍 사거리 +3칸", 18, ult, () => TreeStormRange += 3f));
                t.Add(Node("u.storm2", "u.storm1", 1, "폭풍 사거리 II", "총알 폭풍 사거리 +3칸", 34, ult, () => TreeStormRange += 3f));
            }
            t.Add(Node("u.slow1", "u.gauge1", 1, "노려보는 눈빛", "필살기를 조준하는 동안 적이 20% 더 느려집니다", 18, LvIcon(3), () => p.Skill_setTime *= 0.8f));
            t.Add(Node("u.slow2", "u.slow1", 1, "노려보는 눈빛 II", "필살기를 조준하는 동안 적이 20% 더 느려집니다", 34, LvIcon(3), () => p.Skill_setTime *= 0.8f));
        }
        t.Add(Node("u.refund1", "u.gauge2", 1, "잔불", "필살기를 쓴 뒤 게이지가 15% 남습니다", 30, LvIcon(2), () => TreeUltRefund += 0.15f));
        t.Add(Node("u.refund2", "u.refund1", 1, "잔불 II", "필살기를 쓴 뒤 게이지가 15% 더 남습니다", 54, LvIcon(2), () => TreeUltRefund += 0.15f));
        if (CharacterData.Selected == CharacterId.Rogue)
        {
            // 도적 그림자 숙련: 출혈 돌진에 유틸을 한 단계씩 (이동 속도 · 착지 둔화 · 거리 · 연속 돌진 · 게이지 반환)
            string[] roman = { "I", "II", "III", "IV", "V" };
            int[] costs = { 12, 24, 38, 54, 72 };
            for (int i = 0; i < 5; i++)
            {
                int lv = i + 1;
                t.Add(RawNode("u.shadow" + lv, i == 0 ? "u.power1" : "u.shadow" + i, 1, Loc.T("그림자 숙련") + " " + roman[i],
                              Loc.T(CharacterKit.MasteryNames[i]), costs[i], ult, () => { if (Kit != null) Kit.shadowLevel = Mathf.Max(Kit.shadowLevel, lv); }));
            }
        }

        // ---------------- 2 운명 (1.8.2~, 예전 액티브 스킬 자리): 다른 가지 · 레벨업 카드에 없는 새 효과만
        List<int> myPassives = new List<int>();
        if (Gunner) myPassives.AddRange(new[] { OrbsId, ThornsId, CurseId, UndyingId });
        else
            foreach (int id in CharacterData.Current.pool)
                if (id < abilities.Length && abilities[id].kind == SpecialKind.Passive) myPassives.Add(id);
        Sprite soulIcon = myPassives.Count > 0 ? abilities[myPassives[0]].icon : def;
        FateNodes(t);

        // ---------------- 3 생존
        // 레벨업 카드(강철같은 심장 · 단단한 신체 · 생명의 샘)와 겹치지 않는, 싸우는 방식에 따라 달라지는 생존 칸
        t.Add(Node("s.hp1", null, 3, "강인함", "체력이 절반 아래일 때 받는 피해 -12%", 6, LvIcon(8), () => TreeLowHpDef += 0.12f));
        Chain(t, "s.hp", 2, 3, "s.hp1", 3, "강인함", "체력이 절반 아래일 때 받는 피해 -12%", new[] { 18, 34 }, LvIcon(8), () => TreeLowHpDef += 0.12f);
        t.Add(Node("s.regen1", "s.hp1", 3, "전투 치유", "3초 동안 맞지 않으면 초당 최대 체력의 1%를 회복", 12, LvIcon(10), () => TreeMend += 0.01f));
        Chain(t, "s.regen", 2, 3, "s.regen1", 3, "전투 치유", "3초 동안 맞지 않으면 회복량 +0.75%", new[] { 26, 44 }, LvIcon(10), () => TreeMend += 0.0075f);
        t.Add(Node("s.def1", "s.hp1", 3, "완충", "한 번에 잃는 체력이 최대 체력의 25%를 넘지 않습니다", 16, def, () => TreeDamageCap = TreeDamageCap > 0f ? Mathf.Min(TreeDamageCap, 0.25f) : 0.25f));
        t.Add(Node("s.def2", "s.def1", 3, "완충 II", "한 번에 잃는 체력의 상한 25% → 18%", 36, def, () => TreeDamageCap = 0.18f));
        t.Add(Node("s.guard1", "s.def1", 3, "재정비", "맞은 뒤 무적 시간 +0.15초", 22, def, () => p.hurtInvincibleTime += 0.15f));
        t.Add(Node("s.guard2", "s.guard1", 3, "재정비 II", "맞은 뒤 무적 시간 +0.15초", 42, def, () => p.hurtInvincibleTime += 0.15f));
        t.Add(Node("s.barrier1", "s.def2", 3, "영혼 보호막", "20초마다 공격 한 번을 막아 주는 보호막이 생깁니다", 40, def, () => { barrierEvery = 20f; barrierReadyAt = Time.time; }));
        t.Add(Node("s.barrier2", "s.barrier1", 3, "영혼 보호막 II", "보호막이 12초마다 다시 생깁니다", 66, def, () => barrierEvery = 12f));
        // 1.0.2: 너무 셌음 — 칸마다 +1 → +0.5, 처치 회복은 모두 합쳐 초당 최대 6 (PlayerController.HealOnKill)
        t.Add(Node("s.leech1", "s.regen1", 3, "피의 굶주림", "적을 처치할 때마다 체력 +0.5 회복 (처치 회복은 초당 최대 6)", 20, LvIcon(11), () => p.healOnKill += 0.5f));
        t.Add(Node("s.leech2", "s.leech1", 3, "피의 굶주림 II", "적을 처치할 때마다 체력 +0.5 회복 (처치 회복은 초당 최대 6)", 40, LvIcon(11), () => p.healOnKill += 0.5f));
        // 1.0.1: 가지 끝에 판을 바꾸는 큰 보상 (판 기록에서 생존 · 영혼 · 재물 가지가 거의 안 찍혔음)
        t.Add(Node("s.laststand", "s.guard2", 3, "꺾이지 않는 의지", "「최후의 보루」가 장마다 한 번 더 발동합니다", 44, def, () => LastStand.Extra += 1));
        t.Add(Node("s.revive", "s.barrier2", 3, "되살아난 영혼", "판마다 한 번, 쓰러질 피해를 받으면 체력 40%로 되살아납니다", 90, def, () => TreeRevives += 1));

        // ---------------- 4 영혼 (예전 거너 패시브가 여기로)
        t.Add(Node("o.root", null, 4, "영혼 각성", "영혼 조각 획득량 +15%", 8, soulIcon, () => TreeShardMul += 0.15f));
        t.Add(Node("o.root2", "o.root", 4, "영혼 각성 II", "영혼 조각 획득량 +15%", 30, soulIcon, () => TreeShardMul += 0.15f));
        t.Add(Node("o.root3", "o.root2", 4, "영혼 각성 III", "영혼 조각 획득량 +20%", 60, soulIcon, () => TreeShardMul += 0.2f));
        t.Add(Node("o.harvest", "o.root2", 4, "영혼 수확", "보스 · 중간 보스가 주는 영혼 조각 2배", 48, soulIcon, () => TreeBigShardMul = 2f));
        for (int i = 0; i < myPassives.Count; i++) AddPassive(t, myPassives[i], 22 + 6 * i, 48 + 8 * i);

        // ---------------- 5 재물
        // 레벨업 카드(코인충 · 코인 자석 · 더 많은 경험치)와 겹치지 않는 재물 칸
        t.Add(Node("g.coin1", null, 5, "황금 손길", "적을 처치하면 6% 확률로 코인 3개를 더 받습니다", 8, LvIcon(1), () => TreeGoldChance += 0.06f));
        Chain(t, "g.coin", 2, 3, "g.coin1", 5, "황금 손길", "처치 시 코인 확률 +6%", new[] { 22, 40 }, LvIcon(1), () => TreeGoldChance += 0.06f);
        t.Add(Node("g.magnet1", "g.coin1", 5, "흥정", "떠돌이 상점 가격 -10%", 12, LvIcon(1), () => TreeShopMul *= 0.9f));
        t.Add(Node("g.magnet2", "g.magnet1", 5, "흥정 II", "떠돌이 상점 가격 -10%", 28, LvIcon(1), () => TreeShopMul *= 0.9f));
        t.Add(Node("g.fate", "g.magnet2", 5, "운명 바꾸기", "레벨업 카드 다시 뽑기 +3", 34, LvIcon(4), () => rerolls += 3));
        t.Add(Node("g.luck1", "g.coin1", 5, "행운", "행운 +1 — 이미 가진 카드 · 진화 재료 카드가 레벨업에 더 자주 나옵니다", 14, LvIcon(4), () => Luck += 1));
        Chain(t, "g.luck", 2, 4, "g.luck1", 5, "행운", "행운 +1", new[] { 30, 48, 70 }, LvIcon(4), () => Luck += 1);
        t.Add(Node("g.exp1", "g.coin1", 5, "깨달음", "레벨이 오를 때마다 영혼 조각 +5", 12, LvIcon(4), () => TreeInsight += 5));
        Chain(t, "g.exp", 2, 3, "g.exp1", 5, "깨달음", "레벨이 오를 때마다 영혼 조각 +5", new[] { 28, 46 }, LvIcon(4), () => TreeInsight += 5);
        t.Add(Node("g.interest1", "g.coin2", 5, "이자", "30초마다 가진 코인의 5%를 더 받습니다 (최대 20개)", 30, LvIcon(1), () => { interestRate += 0.05f; if (interestAt <= 0f) interestAt = Time.time + 30f; }));
        t.Add(Node("g.interest2", "g.interest1", 5, "이자 II", "이자 +5%", 56, LvIcon(1), () => interestRate += 0.05f));
        DecorateTreeIcons(t);                           // 칸마다 전용 그림 · 표식 (1.0.6~)
        ApplyFit(t);                                    // 지금 무기에 쓸모없는 칸은 숨김 (SpecialAbilities.Fit)
        string[] rec = RecommendedPrefixes();
        foreach (SoulNode n in t)
            foreach (string r in rec)
                if (n.key.StartsWith(r)) { n.recommended = true; break; }
        return t;
    }

    // ---------------- 칸 그림 (1.0.6~): 예전엔 능력치 · 레벨업 카드 그림을 여러 칸이 돌려 써서 한 그림을 많게는 10칸이 같이 썼음
    //   Icons/tree_<칸 키> (키의 '.' → '_', 예: w.dmg3 → tree_w_dmg3) — tools/pixelart/make_tree_icons.py 가 그림
    //   여러 단계 칸은 같은 그림에 아래쪽 단계 구슬 수로 다름. 전용 그림이 없는 칸(무기 숙련 · 패시브)은
    //   무기 · 능력 그림을 그대로 두고 작은 표식(tree_mark_*)을 겹쳐 칸마다 다르게
    static readonly Dictionary<string, Sprite> treeIcons = new Dictionary<string, Sprite>();

    public static Sprite TreeIcon(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (!treeIcons.TryGetValue(key, out Sprite s))
        {
            s = Resources.Load<Sprite>("Icons/tree_" + key.Replace('.', '_'));
            treeIcons[key] = s;                         // 없는 그림도 기억 (트리를 만들 때마다 다시 찾지 않게)
        }
        return s;
    }

    void DecorateTreeIcons(List<SoulNode> t)
    {
        foreach (SoulNode n in t)
        {
            Sprite own = TreeIcon(n.key);
            if (own != null) { n.icon = own; continue; }
            n.mark = TreeIcon(TreeMarkName(n.key));
        }
    }

    // 표식 이름: 무기 숙련 갈래 (m<무기>.a · .b · .c · .t1~3 · .x) · 패시브 진화 (o.<id>+)
    string TreeMarkName(string key)
    {
        if (key.StartsWith("o.") && key.EndsWith("+")) return "mark.evolve";
        // 1.4.1~ 진화 무기 필살기 특성 줄: 무기 그림 + 단계 표식 (권총 줄은 전용 그림 tree_u_trait1~3)
        if (ParseUltTraitKey(key, out int uw, out int us)) return uw >= 0 && us >= 1 && us <= 3 ? "mark." + us : null;
        if (!key.StartsWith("m")) return null;
        int dot = key.IndexOf('.');
        if (dot < 0) return null;                       // 숙련 첫 칸은 무기 그림 그대로
        switch (key.Substring(dot + 1))
        {
            case "a": return "mark.rate";               // 빠른 손
            case "b": return "mark.crit";               // 명사수 · 날카로운 감각
            case "c": return Gunner ? "mark.reload" : "mark.power";     // 총잡이의 감 · 기본기
            case "t1": return "mark.1";                 // 단련 · 능력 강화 I~III
            case "t2": return "mark.2";
            case "t3": return "mark.3";
            case "x": return "mark.memory";             // 기억
        }
        return null;
    }

    // 지금 무기(형태)에 잘 맞는 칸의 앞글자 (영혼 트리 추천 표시)
    string[] RecommendedPrefixes()
    {
        int w = Gunner ? (gunEvo2 >= 0 ? gunEvo2 : gunEvo1) : (Kit != null ? Kit.form : -1);
        switch (w)
        {
            case SniperId: case SeekerId: return new[] { "w.crit", "w.critdmg", "m" + w };
            case FlameId: case GrenadeId: case ShotgunId: return new[] { "w.dmg", "m" + w };
            case DualId: case ScytheId: return new[] { "w.rate", "w.mag", "m" + w };
            case ChainId: return new[] { "w.pene", "w.dmg", "m" + w };
            case KitHammer: return new[] { "w.dmg", "u.kit", "m" + w };
            case KitWhip: return new[] { "w.reach", "w.crit", "m" + w };
            case KitLance: return new[] { "w.crit", "w.exec", "m" + w };
            case KitBlowgun: return new[] { "w.rate", "w.mag", "m" + w };
            case KitCards: return new[] { "w.crit", "w.critdmg", "m" + w };
            case KitWire: return new[] { "w.dmg", "w.pene", "m" + w };
            case KitNetBow: return new[] { "w.rate", "w.dmg", "m" + w };
            case KitJavelin: return new[] { "w.pene", "w.dmg", "m" + w };
            case KitBurstBow: return new[] { "w.rate", "w.crit", "m" + w };
            case KitQuicksilver: case KitFirework: return new[] { "w.blast", "w.dmg", "m" + w };
            case KitMagnet: return new[] { "w.dmg", "w.blast", "m" + w };
            default: return new[] { "w.dmg", "s.hp" };          // 진화 전: 기본기
        }
    }

    // [Shift]+[T] 빠른 배우기: 트리를 열지 않고 살 수 있는 칸 하나 (추천 칸 → 가장 싼 칸)
    public SoulNode QuickBuy()
    {
        SoulNode best = null;
        foreach (SoulNode n in BuildSoulTree())
        {
            if (!CanBuy(n)) continue;
            if (best == null || (n.recommended && !best.recommended) || (n.recommended == best.recommended && n.cost < best.cost)) best = n;
        }
        return best != null && BuyNode(best) ? best : null;
    }

    // 가지마다 배운 칸 수 (결과 화면 · 밸런스 기록). 칸 이름 앞글자로 가지를 알 수 있음
    public int[] OwnedPerBranch()
    {
        int[] c = new int[BranchCount];
        foreach (string k in ownedNodes)
        {
            int b = k.StartsWith("w.") || k.StartsWith("m") ? 0 : k.StartsWith("u.") ? 1 : k.StartsWith("f.") ? 2
                  : k.StartsWith("s.") ? 3 : k.StartsWith("o.") ? 4 : 5;
            c[b]++;
        }
        return c;
    }

    // 진화 기록: (이름, 판 시작 뒤 몇 초) — 결과 화면 · 밸런스 기록
    public readonly List<KeyValuePair<string, float>> EvolutionLog = new List<KeyValuePair<string, float>>();

    // 다른 캐릭터 전용 무기 칸: 예전 상점의 캐릭터 줄(베기 사거리 · 베기 각도 · 폭발 범위)이 여기로
    void KitWeaponNodes(List<SoulNode> t)
    {
        Sprite atk = StatIcon(0), spd = StatIcon(2);
        switch (CharacterData.Selected)
        {
            case CharacterId.Swordsman:
                t.Add(Node("w.reach1", "w.dmg1", 0, "긴 칼날", "베기 사거리 +8%", 12, atk, () => { if (Kit != null) Kit.reachMul += 0.08f; }));
                Chain(t, "w.reach", 2, 3, "w.reach1", 0, "긴 칼날", "베기 사거리 +8%", new[] { 26, 44 }, atk, () => { if (Kit != null) Kit.reachMul += 0.08f; });
                t.Add(Node("w.arc1", "w.reach1", 0, "넓은 베기", "베기 각도 +20°", 14, spd, () => { if (Kit != null) Kit.arcBonus += 10f; }));
                Chain(t, "w.arc", 2, 3, "w.arc1", 0, "넓은 베기", "베기 각도 +20°", new[] { 28, 46 }, spd, () => { if (Kit != null) Kit.arcBonus += 10f; });
                break;
            case CharacterId.Alchemist:
                t.Add(Node("w.blast1", "w.dmg1", 0, "넓은 폭발", "플라스크 폭발 범위 +8%", 12, atk, () => { if (Kit != null) Kit.blastMul += 0.08f; }));
                Chain(t, "w.blast", 2, 4, "w.blast1", 0, "넓은 폭발", "플라스크 폭발 범위 +8%", new[] { 26, 42, 60 }, atk, () => { if (Kit != null) Kit.blastMul += 0.08f; });
                break;
        }
    }

    // 무기 숙련: 무기 하나를 들었던 기록마다 가지 하나 (숙련 · 특성 세 단계 · 무기마다 다른 이어지는 보너스)
    void AddMastery(List<SoulNode> t, int w)
    {
        string k = "m" + w;
        string wname = EvolutionName(w, historyTier.TryGetValue(w, out int tr) ? tr : 1);
        Sprite icon = EvolutionIcon(w);
        if (w < 0 && Gunner) icon = Resources.Load<Sprite>("Weapons/weapon_pistol");
        t.Add(RawNode(k, "w.dmg1", 0, Loc.T("숙련: ") + wname, Loc.T("모든 무기 피해 +6% (진화해도 이어짐)"), 12, icon, () => AddAttack(0.06f)));
        if (w < 0)
        {
            // 권총: 기본기를 다지는 숙련 (모두 진화해도 이어짐)
            t.Add(Node(k + ".a", k, 0, "빠른 손", "모든 무기 발사 간격 -6%", 16, icon, () => AddRate(0.94f)));
            t.Add(Node(k + ".b", k + ".a", 0, Gunner ? "명사수" : "날카로운 감각", "치명타 확률 +6%", 26, icon, () => TreeCrit += 0.06f));
            if (Gunner) t.Add(Node(k + ".c", k + ".b", 0, "총잡이의 감", "모든 무기 장전 시간 -10%", 36, icon, () => AddReload(0.9f)));
            else t.Add(Node(k + ".c", k + ".b", 0, "기본기", "모든 무기 피해 +6%", 36, icon, () => AddAttack(0.06f)));
            return;
        }
        if (!Gunner)
        {
            // 다른 캐릭터의 형태: 그 형태로 치는 평타 피해 세 단계 + 이어지는 보너스 (모든 무기 피해)
            System.Action power = () => { if (Kit != null) Kit.formPower += 0.05f; };       // 1.1.2~ 평타 고점 하향: 단계마다 +12% → +5% (투사체 판정 ×1.3 반영)
            t.Add(RawNode(k + ".t1", k, 0, wname + " " + Loc.T("단련"), Loc.T("평타 피해 +5%"), 18, icon, power));
            t.Add(RawNode(k + ".t2", k + ".t1", 0, wname + " " + Loc.T("단련") + " II", Loc.T("평타 피해 +5%"), 34, icon, power));
            t.Add(RawNode(k + ".t3", k + ".t2", 0, wname + " " + Loc.T("단련") + " III", Loc.T("평타 피해 +5%"), 56, icon, power));
            t.Add(RawNode(k + ".x", k, 0, Loc.T("기억: ") + wname, Loc.T("모든 무기 피해 +8%"), 30, icon, () => AddAttack(0.08f)));
            return;
        }
        // 진화 능력: 그 능력을 강하게 하는 세 단계 + 이어지는 보너스
        string trait = AugTraitName(w), step = AugTraitStep(w);
        t.Add(RawNode(k + ".t1", k, 0, trait, step, 18, icon, () => AddTrait(w)));
        t.Add(RawNode(k + ".t2", k + ".t1", 0, trait + " II", step, 34, icon, () => AddTrait(w)));
        t.Add(RawNode(k + ".t3", k + ".t2", 0, trait + " III", step, 56, icon, () => AddTrait(w)));
        t.Add(RawNode(k + ".x", k, 0, Loc.T("기억: ") + wname, MasteryBonusText(w), 30, icon, () => MasteryBonus(w)));
    }

    static string MasteryBonusText(int w) => w switch
    {
        FlameId or GrenadeId or ShotgunId => Loc.T("모든 무기 피해 +8%"),
        SniperId or SeekerId => Loc.T("치명타 확률 +8%"),
        DualId or ScytheId => Loc.T("모든 무기 발사 간격 -5%"),
        _ => Loc.T("모든 총알 관통 +1"),
    };

    void MasteryBonus(int w)
    {
        switch (w)
        {
            case FlameId: case GrenadeId: case ShotgunId: AddAttack(0.08f); break;
            case SniperId: case SeekerId: TreeCrit += 0.08f; break;
            case DualId: case ScytheId: AddRate(0.95f); break;      // 1.1.2~ 평타 고점 하향: -8% → -5%
            default: AddPene(); break;
        }
    }

    // 특성 강화: 지금 들고 있지 않은 무기에도 기록해 둠 (다시 쓸 일은 없지만 산 조각이 날아가지 않게)
    void AddTrait(int id)
    {
        if (!weaponLevels.ContainsKey(id)) weaponLevels[id] = new int[4];
        weaponLevels[id][StatTrait] = Mathf.Min(WeaponStatMax[StatTrait], weaponLevels[id][StatTrait] + 1);
    }

    void AddPassive(List<SoulNode> t, int id, int cost, int evoCost)
    {
        SpecialDef d = abilities[id];
        t.Add(new SoulNode { key = "o." + id, parent = "o.root", branch = 4, name = Loc.T(d.name), desc = BodyText(id, d.description), cost = cost, icon = d.icon,
                             apply = () => Equip(new[] { id }) });
        t.Add(new SoulNode { key = "o." + id + "+", parent = "o." + id, branch = 4, name = Loc.T(d.name) + Loc.T(" 진화"), desc = EvolveText(id), cost = evoCost, icon = d.icon,
                             apply = () => Evolve(id) });
    }

    // 1.4.1~ 필살기 특성 줄 (무기마다 따로): u.t<무기>.<단계> — 권총 · 캐릭터 원래 우클릭은 u.t-1.N
    public static string UltTraitKey(int weapon, int step) => "u.t" + weapon + "." + step;

    void AddUltTraitLine(List<SoulNode> t, int w, Sprite ult)
    {
        string ut = UltTraitName(w);
        bool pistol = w == PistolUlt;
        // 진화 전에는 예전 글 그대로 (「필살기 타겟 수 +2마리」), 진화 뒤에는 어느 필살기 특성인지 이름을 붙임
        string head = pistol ? (UltActive ? UltName(PistolUlt) : Loc.T("필살기")) : UltName(w);
        string name = pistol ? ut : UltName(w) + " " + ut;
        string desc = head + " " + ut + " " + UltTraitStep(w);
        int[] costs = { 16, 32, 52 };
        string[] roman = { "", " II", " III" };
        for (int i = 1; i <= 3; i++)
        {
            Sprite own = pistol ? TreeIcon("u.trait" + i) : null;
            Sprite icon = pistol ? (own != null ? own : ult) : EvolutionIcon(w);
            t.Add(RawNode(UltTraitKey(w, i), i == 1 ? "u.power1" : UltTraitKey(w, i - 1), 1, name + roman[i - 1], desc, costs[i - 1], icon,
                          () => UpgradeUlt(w, UltTraitStat)));
        }
    }

    // 줄 키에서 무기 · 단계 (u.t<무기>.<단계>가 아니면 false)
    static bool ParseUltTraitKey(string key, out int weapon, out int step)
    {
        weapon = 0; step = 0;
        if (key == null || !key.StartsWith("u.t") || key.StartsWith("u.trait")) return false;
        int dot = key.LastIndexOf('.');
        return dot > 3 && int.TryParse(key.Substring(3, dot - 3), out weapon) && int.TryParse(key.Substring(dot + 1), out step);
    }

    // 같은 칸을 여러 단계로 (예: w.dmg2 ~ w.dmg5): 앞 단계에서 이어짐
    static void Chain(List<SoulNode> t, string prefix, int from, int to, string first, int branch, string ko, string descKo, int[] costs, Sprite icon, System.Action apply)
    {
        string prev = first;
        string[] roman = { "", "", "II", "III", "IV", "V", "VI" };
        for (int i = from; i <= to; i++)
        {
            string key = prefix + i;
            t.Add(Node(key, prev, branch, ko, descKo, costs[i - from], icon, apply));
            t[t.Count - 1].name = Loc.T(ko) + " " + roman[Mathf.Min(i, roman.Length - 1)];
            prev = key;
        }
    }

    static SoulNode Node(string key, string parent, int branch, string ko, string descKo, int cost, Sprite icon, System.Action apply)
        => new SoulNode { key = key, parent = parent, branch = branch, name = Loc.T(ko), desc = Loc.T(descKo), cost = cost, icon = icon, apply = apply };

    static SoulNode RawNode(string key, string parent, int branch, string name, string desc, int cost, Sprite icon, System.Action apply)
        => new SoulNode { key = key, parent = parent, branch = branch, name = name, desc = desc, cost = cost, icon = icon, apply = apply };

    // ---------------- 트리 효과를 실제 수치에 반영
    // 공격력: 처음 공격력 기준으로 더함 (상점 강화 · 희생의 계약과 겹쳐도 안전)
    void AddAttack(float pct) => player.damage += BaseAttack * pct;

    void AddRate(float mul)
    {
        TreeRateMul *= mul;
        player.ShootSpeed *= mul;               // 권총
    }

    void AddMag(float pct)
    {
        TreeMagMul += pct;
        int add = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, basePistolMag) * pct));
        player.MaxBullet += add;                // 권총
        player.NowBullet += add;
        foreach (int w in weapons) if (UsesAmmo(w)) Ammo(w).ammo = Mathf.Min(MagSize(w), Ammo(w).ammo + Mathf.RoundToInt(BaseMag(w) * pct));
    }

    void AddReload(float mul)
    {
        TreeReloadMul *= mul;
        player.reloadTime *= mul;               // 권총
    }

    void AddPene()
    {
        TreePene++;
        player.pene++;                          // 권총
    }

    void AddExp(float pct)
    {
        Level lv = Cache<Level>.Get;
        if (lv != null) lv.bonusEXP += pct;
    }

    static void GrowHp(PlayerController p, float mul)
    {
        float add = p.PlayerMaxHealth * (mul - 1f);
        p.PlayerMaxHealth += add;
        p.PlayerHealth += add;
    }

    // 받는 피해 조정 (PlayerController.TryHit): 강인함 · 완충. 맞은 시각도 적어 둠 (전투 치유)
    public float AdjustTaken(float taken)
    {
        lastHurtAt = Time.time;
        if (TreeLowHpDef > 0f && player.PlayerHealth <= player.PlayerMaxHealth * 0.5f) taken *= 1f - Mathf.Min(0.5f, TreeLowHpDef);
        if (TreeDamageCap > 0f) taken = Mathf.Min(taken, player.PlayerMaxHealth * TreeDamageCap);
        return taken;
    }

    // 황금 손길 (처치할 때)
    void TreeOnKill(Vector3 pos)
    {
        FateOnKill(pos);
        EvoOnKill(pos);
        if (TreeGoldChance <= 0f || Random.value >= TreeGoldChance) return;
        Coin c = Cache<Coin>.Get;
        if (c == null) return;
        c.AddCoin(3);
        Fx.Spawn("fx_sparkle", pos, 1.2f, new Color(1f, 0.85f, 0.35f), 18f);
        if (fx != null) fx.FloatText(pos, "+3", new Color(1f, 0.85f, 0.35f), 4f, 0.2f);
    }

    // 깨달음 (레벨이 오를 때, LevelShop.AddPending)
    public void TreeOnLevelUp(int levels)
    {
        FateOnLevelUp(levels);
        if (TreeInsight > 0 && player != null) SoulShards.Add(TreeInsight * levels, player.transform.position, false);
    }

    // 보호막이 있으면 공격 한 번을 막음 (PlayerController.TryHit)
    public bool ConsumeBarrier()
    {
        if (barrierEvery <= 0f || Time.time < barrierReadyAt) return false;
        barrierReadyAt = Time.time + barrierEvery;
        if (fx != null)
        {
            fx.Play("clank", 0.7f, 1.4f);
            fx.FloatText(player.transform.position, Loc.T("보호막!"), new Color(0.6f, 0.85f, 1f), 5f, 0.5f);
        }
        Flash(player.transform.position, 3f, new Color(0.6f, 0.85f, 1f, 0.8f), 0.25f);
        return true;
    }

    // 매 프레임 (장착한 능력이 없어도): 이자
    void TreeTick()
    {
        FateTick();
        if (TreeMend > 0f && Time.timeScale > 0f && Time.time - lastHurtAt > 3f && player.PlayerHealth < player.PlayerMaxHealth && player.PlayerHealth > 0f)
            player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + player.PlayerMaxHealth * TreeMend * Time.deltaTime);
        if (interestRate <= 0f || Time.timeScale == 0f || Time.time < interestAt) return;
        interestAt = Time.time + 30f;
        Coin c = Cache<Coin>.Get;
        if (c == null) return;
        int n = Mathf.Min(20, Mathf.FloorToInt(c.coins * interestRate));
        if (n <= 0) return;
        c.AddCoin(n);
        if (fx != null) fx.FloatText(player.transform.position, Loc.T("이자 +") + n, new Color(1f, 0.85f, 0.35f), 5f, 0f);
    }

    // 능력치 아이콘 (StatsHUD: 공격력 · 방어력 · 공격 속도 · 재장전 · 이동 속도)
    // 씬 검색은 한 번만 (트리를 만들 때마다 찾지 않게)
    static StatsHUD statsHud;
    public static Sprite StatIcon(int i)        // 1.0.4~: 레벨업 「성장」 카드 아이콘도 (LevelShop.Growth)
    {
        if (statsHud == null) statsHud = FindFirstObjectByType<StatsHUD>(FindObjectsInactive.Include);
        return statsHud != null && statsHud.icons != null && i < statsHud.icons.Length ? statsHud.icons[i] : null;
    }

    // 레벨업 카드 아이콘 (0 관통 · 1 코인 · 2 재활용 · 3 눈빛 · 4 경험치 · 5 자석 · 7 밀어내기 · 8 심장 · 10 샘 · 11 굶주림)
    static AbilityHUD abilityHud;
    public static Sprite LvIcon(int id)             // 1.1~ public: 초월 카드 「급소」 아이콘도 (LevelShop.Transcend)
    {
        if (abilityHud == null) abilityHud = FindFirstObjectByType<AbilityHUD>(FindObjectsInactive.Include);
        return abilityHud != null ? abilityHud.GetIcon(id) : null;
    }

    public bool IsOpenNode(SoulNode n) => !ownedNodes.Contains(n.key) && !n.hidden && (n.parent == null || ownedNodes.Contains(n.parent));
    public string BlockReason(SoulNode n) => n.blocked?.Invoke();
    public bool CanBuy(SoulNode n) => IsOpenNode(n) && SoulShards.Amount >= n.cost && BlockReason(n) == null;
    // 화면에 보이는 칸: 배운 칸과 지금 배울 수 있는 칸 (그 너머는 숨김)
    public bool IsVisibleNode(SoulNode n) => !n.hidden && (ownedNodes.Contains(n.key) || n.parent == null || ownedNodes.Contains(n.parent));

    public bool BuyNode(SoulNode n)
    {
        if (!CanBuy(n) || !SoulShards.Spend(n.cost)) return false;
        ownedNodes.Add(n.key);
        RunSave.Record("t" + n.key);           // 판 중간 저장 (2.1.9)
        n.apply?.Invoke();
        if (fx != null) { fx.Play("chime", 0.6f, 1.3f); fx.Play("pulse", 0.4f, 1.6f); }
        return true;
    }

    // 판 중간 저장 (RunSave): 기록한 칸을 조각 없이 다시 배움 (배운 순서대로 부르므로 그때와 같은 트리)
    public void ReplayNode(string key)
    {
        // 1.4.1~ 옛 저장의 필살기 특성 칸(u.trait1~3)은 그때 무기의 줄로 (기록은 배운 순서대로 다시 적용하므로 지금 무기 = 그때 무기)
        if (key != null && key.StartsWith("u.trait") && int.TryParse(key.Substring(7), out int oldStep))
            key = UltTraitKey(UltActive ? UltId : PistolUlt, oldStep);
        if (string.IsNullOrEmpty(key) || ownedNodes.Contains(key)) return;
        foreach (SoulNode n in BuildSoulTree())
        {
            if (n.key != key) continue;
            ownedNodes.Add(n.key);
            n.apply?.Invoke();
            return;
        }
    }

    // 예전 방식 호환 (무기 가지가 공통이 되어 이제 지울 칸이 없음)
    public void OnEvolvedTier1() { }

    // 살 수 있는 칸이 있는지 (버튼 반짝임)
    // 1.2.3~ 최적화: 2초마다 트리 전체(칸 ~100개 · 클로저 · 번역 글)를 새로 만들던 것을, 트리 모양을 바꿀 수 있는 상태가 같으면 지난 트리로 검사
    //   (배운 칸 · 진화 · 무기/형태 · 능력 수 · 정련 · 언어 · 영웅) — 그래도 20초가 지나면 새로 만듦. 판정(CanBuy)은 지금 값 그대로
    List<SoulNode> affordTree;
    int affordKey;
    float affordBuiltAt;

    int AffordKey()
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + ownedNodes.Count;
            h = h * 31 + gunEvo1;
            h = h * 31 + gunEvo2;
            h = h * 31 + UltId;
            h = h * 31 + equipped.Count;
            h = h * 31 + evolved.Count;
            h = h * 31 + weapons.Count;
            h = h * 31 + skills.Count;
            h = h * 31 + relics.Count;
            h = h * 31 + weaponHistory.Count;
            h = h * 31 + historyTier.Count;
            h = h * 31 + Refines;
            h = h * 31 + (int)Loc.Current;
            h = h * 31 + (int)CharacterData.Selected;
            h = h * 31 + (Kit != null ? Kit.form : -9);
            return h;
        }
    }

    public bool AnyAffordable()
    {
        int key = AffordKey();
        if (affordTree == null || key != affordKey || Time.time - affordBuiltAt > 20f || Time.time < affordBuiltAt)
        {
            affordTree = BuildSoulTree();
            affordKey = key;
            affordBuiltAt = Time.time;
        }
        for (int i = 0; i < affordTree.Count; i++) if (CanBuy(affordTree[i])) return true;
        return false;
    }

    // ================================================================= 영혼 정련 (1.0.4~)
    // 영혼 트리를 모두 배운 뒤 남는 조각을 쓰는 곳: 할 때마다 모든 무기 피해 +2% (기본 공격력 기준) · 최대 체력 +4, 끝없이
    // 비용은 40에서 시작해 할 때마다 ×5 (5 단위로 반올림). 이어하기 기록은 r (RunSave)
    // 1.1.2~: 예전엔 +10 씩만 올라 조각이 쌓이면 끝없이 눌러 공격력이 지나치게 커졌음 → ×1.4 → 다시 상승률 10배(+40% → +400%) ×5: 40 · 200 · 1000 · 5000 …
    // 1.2~ (밸런스 결정 묶음) 40 × 5ⁿ → 60 × 3ⁿ: 60 · 180 · 540 · 1,620 · 4,860 — 한 판 안에서 남는 조각이 끝까지 쓸모 있게 (이야기 모드 정련 2 → 3~4번)
    //   무한 모드 · 일일 도전은 옛 40 × 5ⁿ 그대로 (초월 막대처럼 순위표 기록이 1.2 앞뒤로 갈리지 않게)
    public const int RefineBaseCost = 60;
    const float RefineCostGrowth = 3f;
    const int EndlessRefineBaseCost = 40;
    const float EndlessRefineCostGrowth = 5f;
    static bool RefineOldCurve => GameMode.IsEndless || DailyChallenge.Active;
    const float RefineAttack = 0.02f, RefineHp = 4f;
    public int Refines { get; private set; }
    public int RefineCost
    {
        get
        {
            int baseCost = RefineOldCurve ? EndlessRefineBaseCost : RefineBaseCost;
            double c = baseCost * System.Math.Pow(RefineOldCurve ? EndlessRefineCostGrowth : RefineCostGrowth, Refines);
            if (c >= int.MaxValue / 2) return int.MaxValue / 2;
            return Mathf.Max(baseCost, (int)System.Math.Round(c / 5.0) * 5);
        }
    }

    // 지금 배울 수 있는 칸이 하나도 없음 (배울 수 없는 이유가 있는 칸 · 숨은 칸은 빼고)
    public bool TreeComplete(List<SoulNode> tree = null)
    {
        foreach (SoulNode n in tree ?? BuildSoulTree())
            if (IsOpenNode(n) && BlockReason(n) == null) return false;
        return true;
    }

    public bool CanRefine(List<SoulNode> tree = null) => SoulShards.Amount >= RefineCost && TreeComplete(tree);

    public bool Refine(List<SoulNode> tree = null)
    {
        if (!CanRefine(tree) || !SoulShards.Spend(RefineCost)) return false;
        ApplyRefine();
        RunSave.Record("r");
        if (fx != null) { fx.Play("chime", 0.6f, 1.1f); fx.Play("pulse", 0.4f, 1.4f); }
        return true;
    }

    // 판 중간 저장 (RunSave): 조각 없이 다시 적용 (조각은 저장할 때의 값으로 되돌림)
    public void ReplayRefine() => ApplyRefine();

    void ApplyRefine()
    {
        Refines++;
        GrowAttack(RefineAttack);
        if (player == null) return;
        player.PlayerMaxHealth += RefineHp;
        player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + RefineHp);
    }

    // 끝없는 성장 (1.0.4~, LevelShop.Growth · 영혼 정련): 트리와 같은 기준으로 공격력 · 발사 간격
    public void GrowAttack(float pct)
    {
        if (player == null) return;
        if (baseAttack < 0f) { baseAttack = player.damage; basePistolMag = player.MaxBullet; }     // 트리를 아직 안 열었어도 처음 공격력 기준
        AddAttack(pct);
    }

    // 이어하기 (RunSave): 기준 공격력 · 기준 탄창을 저장해 두었다가 기록을 다시 적용하기 전에 되돌림
    public void GetBase(out float attack, out int mag)
    {
        attack = baseAttack > 0f ? baseAttack : 0f;
        mag = baseAttack > 0f ? basePistolMag : 0;
    }

    // 저장값이 없으면(옛 저장 · 아직 정해지지 않음) 지금 = 시작 공격력으로 정함 — 실제 판에서 시작 직후 정해지는 값과 같음
    public void RestoreBase(float attack, int mag)
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (attack > 0f) { baseAttack = attack; basePistolMag = mag; }
        else if (baseAttack < 0f && player != null) { baseAttack = player.damage; basePistolMag = player.MaxBullet; }
    }

    public void GrowRate(float mul)
    {
        if (player != null) AddRate(mul);
    }
}

// 영혼 조각: 적을 쓰러뜨리면 모이는 재화 (골드와 별개). 강한 적일수록 많이 줌. 한 판마다 새로
public static class SoulShards
{
    public static int Amount { get; private set; }
    public static int Total { get; private set; }            // 이번 판에 모은 총량
    public static int Spent { get; private set; }            // 이번 판에 트리에 쓴 양
    public static event System.Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name != "GameScene") return;
            Amount = 0;
            Total = 0;
            Spent = 0;
            Changed?.Invoke();
        };
    }

    public static void Add(int n, Vector3 at, bool announce)
    {
        if (n <= 0) return;
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        if (sp != null) n = Mathf.Max(1, Mathf.RoundToInt(n * sp.TreeShardMul * (announce ? sp.TreeBigShardMul : 1f)));
        Amount += n;
        Total += n;
        Changed?.Invoke();
        // 적마다 반짝이면 화면이 지저분해서, 많이 주는 중간 보스 · 보스만 보여 줌
        if (announce && SpecialAbilities.UsesEvolution)
        {
            Fx.Spawn("fx_sparkle", at + Vector3.up * 0.4f, 1.6f, new Color(0.7f, 0.55f, 1f), 18f);
            if (SpecialAbilities.SharedFx != null)
                SpecialAbilities.SharedFx.FloatText(at, Loc.T("영혼 조각 +") + n, new Color(0.75f, 0.6f, 1f), 5f, 0f);
        }
    }

    // 판 중간 저장 (RunSave): 저장할 때의 조각 수
    public static void Restore(int amount, int total, int spent)
    {
        Amount = Mathf.Max(0, amount);
        Total = Mathf.Max(0, total);
        Spent = Mathf.Max(0, spent);
        Changed?.Invoke();
    }

    public static bool Spend(int n)
    {
        if (Amount < n) return false;
        Amount -= n;
        Spent += n;
        Changed?.Invoke();
        return true;
    }

    // 적 수준별 양: 일반 1~2 · 강한 적 3~7 · 중간 보스 약 20~30 · 스테이지 보스 약 50
    public static int ForEnemy(EnermyController e)
    {
        int before = Mathf.Max(1, Mathf.RoundToInt(e.expReward * (e.countsTowardBoss ? GameMode.KillStretch : 1f) / 30f));
        // 중간 보스: 경험치 기준 그대로
        if (e.IsMidBoss) return before;
        // 일반 적 (1.8.9~): 판이 길어져도 클리어 전에 영혼 트리가 다 차지 않게 줄이고,
        // 보스까지 세는 적은 늘어난 처치 수만큼 더 나눔. 소수는 확률로 (여러 마리 모으면 평균이 맞음)
        return GameMode.RoundRandom(before * GameMode.KillShardMul(e.countsTowardBoss));
    }
    public static int ForBoss(int expReward) => 30 + Mathf.RoundToInt(expReward / 30f);
}
