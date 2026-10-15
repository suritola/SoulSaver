using System.Collections.Generic;
using UnityEngine;

// 콘텐츠 항목 표 (1.1~): 장 중반 이벤트 · 보물 상자 보상 · 저주를 상수 대신 항목으로 등록
//   항목마다 since(처음 나온 버전) · saveCode(이어하기 기록에 쓰는 짧은 이름, 바꾸지 말 것)
//   여기에 한 줄 더하면 이벤트 풀 · 상자 보상 풀에 바로 들어가고, 이어하기(RunSave)도 saveCode 로 저절로 따라옴
//   이름 · 설명은 LocTable.Content 의 event. · chest. · curse. 열쇠 (4개 언어)
public enum RewardRarity { Common, Rare, Legendary }

// 장 중반 이벤트 하나 (ChapterEvents 가 장마다 무작위로 뽑음)
public sealed class EventDef
{
    public string id, saveCode, since;
    public string nameKey, descKey;
    public int minSlot;                 // 몇 번째 장부터 (0 = 1장)
    public int maxPerRun = 2;           // 한 판에 최대 몇 번
    public float weight = 1f;
    public string Name => Loc.T(nameKey);
    public string Desc => Loc.T(descKey);
}

// 보물 상자 보상 하나 (ChestRewards 가 3택 · 전설 룰렛에 뽑음)
public sealed class RewardDef
{
    public string id, saveCode, since;
    public string nameKey;
    public RewardRarity rarity;
    public float weight = 1f;
    public bool heal;                   // 회복 보상: 체력 30% 아래면 가중치 ×3
    public string icon;                 // Icons/choice_<icon>
    public Color color;
    public System.Func<ChestRewards.Ctx, bool> available;      // 지금 줄 수 있는지 (없으면 늘)
    public System.Func<ChestRewards.Ctx, string> describe;     // 카드 설명 (번역된 글)
    public System.Action<ChestRewards.Ctx> apply;
    public string Name => Loc.T(nameKey);
}

// 저주 하나 (지금은 저주 제단 하나뿐 · Curse.cs 의 값과 같게, 2-1 영혼의 무게에서 늘릴 틀)
public sealed class CurseDef
{
    public string id, saveCode, since;
    public string nameKey, descKey;
    public float hpMul, rewardMul;
    public int minSlot;
}

// 1.2~ 유물 하나 (봉인된 석관을 부수면 2택 · [Q] 유물 칸에 장착): 지금 얻을 길이 없는 「잠든 스킬」을 쿨타임 1.5배로 되살림
//   abilityId = SpecialAbilities 능력 번호 (스킬만) · 고른 영웅의 능력(CharacterData.pool)과 이미 가진 것은 후보에서 뺌
public sealed class RelicDef
{
    public string id, saveCode, since;
    public int abilityId;
    public float weight = 1f;
}

// 1.2~ 부서지는 맵 물체 하나 (MapProps): 장마다 6~8개 · 닿으면 부서져 DropDef 하나를 떨어뜨림
//   stage = 스테이지 번호(Chapters.StageNames 순서, -1 = 어느 장이든) · sarcophagus = 봉인된 석관 (2장부터 60%, 부수면 유물 2택)
//   그림은 Icons/<icon> (tools/pixelart/make_map_props.py)
public sealed class PropDef
{
    public string id, saveCode, since;
    public string nameKey, icon;
    public int stage = -1;
    public bool sarcophagus;
    public float size = 2f;
    public Color color = Color.white;               // 부서질 때 파편 · 빛 색
    public string Name => Loc.T(nameKey);
}

// 1.2~ 맵 물체가 떨어뜨리는 것 하나 (MapProps)
//   icon 이 있으면 바닥에 놓이고 닿으면 apply, 없으면(코인 · 금화) 부서진 자리에서 바로 apply (코인을 흩뿌림)
public sealed class DropDef
{
    public string id, saveCode, since;
    public string nameKey, icon;
    public float weight = 1f;
    public bool heal;                               // 회복: 체력 40% 아래면 가중치 ×2
    public Color color = Color.white;
    public System.Action<MapProps.DropCtx> apply;
    public string Name => Loc.T(nameKey);
}

public static partial class ContentRegistry
{
    // ================================================================= 장 중반 이벤트 (ChapterEvents)
    public const string Raid = "raid", Elite = "elite", Goblin = "goblin", CursedChest = "cursedchest", Altar = "altar", Pack = "pack";

    public static readonly List<EventDef> Events = new List<EventDef>
    {
        new EventDef { id = Raid,        saveCode = "R", since = "1.1", nameKey = "event.raid.name",   descKey = "event.raid.desc",   weight = 1.2f },
        new EventDef { id = Elite,       saveCode = "E", since = "1.1", nameKey = "event.elite.name",  descKey = "event.elite.desc",  weight = 1f },
        new EventDef { id = Goblin,      saveCode = "G", since = "1.1", nameKey = "event.goblin.name", descKey = "event.goblin.desc", weight = 0.9f },
        new EventDef { id = CursedChest, saveCode = "C", since = "1.1", nameKey = "event.cursed.name", descKey = "event.cursed.desc", weight = 0.9f, minSlot = 1 },
        new EventDef { id = Altar,       saveCode = "A", since = "1.1", nameKey = "event.altar.name",  descKey = "event.altar.desc",  weight = 1f },
        new EventDef { id = Pack,        saveCode = "P", since = "1.1", nameKey = "event.pack.name",   descKey = "event.pack.desc",   weight = 1f },
    };

    public static EventDef EventByCode(string code)
    {
        foreach (EventDef e in Events) if (e.saveCode == code) return e;
        return null;
    }

    public static EventDef EventById(string id)
    {
        foreach (EventDef e in Events) if (e.id == id) return e;
        return null;
    }

    // ================================================================= 보물 상자 보상 (ChestRewards)
    // 일반 6 · 희귀 5 · 전설 1 (전설 = 룰렛 대박, 3택에는 나오지 않음)
    // 1.1~ 그림은 보상마다 하나씩 (치유의 샘 · 깨달음 · 숙련 · 영혼 트리의 축복 · 운명의 주사위는 영혼 트리 그림 tree_*, ChoiceUI.IconPath)
    public static readonly List<RewardDef> Rewards = new List<RewardDef>
    {
        // ---------------- 일반
        new RewardDef { id = "coins", saveCode = "co", since = "1.1", nameKey = "chest.coins.name", rarity = RewardRarity.Common, icon = "gold", color = new Color(1f, 0.82f, 0.3f),
                        describe = c => Loc.T("chest.coins.desc").Replace("{0}", c.Coins.ToString()), apply = ChestRewards.GiveCoins },
        new RewardDef { id = "shards", saveCode = "sh", since = "1.1", nameKey = "chest.shards.name", rarity = RewardRarity.Common, icon = "soul", color = new Color(0.55f, 0.9f, 1f),
                        describe = c => Loc.T("chest.shards.desc").Replace("{0}", c.Shards.ToString()), apply = ChestRewards.GiveShards },
        new RewardDef { id = "life", saveCode = "li", since = "1.1", nameKey = "chest.life.name", rarity = RewardRarity.Common, heal = true, icon = "life", color = new Color(1f, 0.45f, 0.45f),
                        describe = c => Loc.T("chest.life.desc"), apply = c => ChestRewards.GiveMaxHp(c, 10f) },
        new RewardDef { id = "heal", saveCode = "he", since = "1.1", nameKey = "chest.heal.name", rarity = RewardRarity.Common, heal = true, icon = "tree_s_regen1", color = new Color(1f, 0.6f, 0.6f),
                        available = c => c.player != null && c.player.PlayerHealth < c.player.PlayerMaxHealth,
                        describe = c => Loc.T("chest.heal.desc"), apply = ChestRewards.GiveHeal },
        new RewardDef { id = "level", saveCode = "lv", since = "1.1", nameKey = "chest.level.name", rarity = RewardRarity.Common, weight = 0.8f, icon = "tree_g_exp1", color = new Color(0.6f, 1f, 0.6f),
                        available = c => c.shop != null && c.shop.ChestCanLevel, describe = c => Loc.T("chest.level.desc"), apply = ChestRewards.GiveLevel },
        new RewardDef { id = "might", saveCode = "mi", since = "1.1", nameKey = "chest.might.name", rarity = RewardRarity.Common, weight = 0.8f, icon = "might", color = new Color(1f, 0.6f, 0.3f),
                        describe = c => Loc.T("chest.might.desc"), apply = ChestRewards.GiveMight },
        // ---------------- 희귀
        new RewardDef { id = "card", saveCode = "ca", since = "1.1", nameKey = "chest.card.name", rarity = RewardRarity.Rare, icon = "tree_mark_evolve", color = new Color(1f, 0.85f, 0.45f),
                        available = c => c.cardId >= 0, describe = c => Loc.T("chest.card.desc").Replace("{0}", c.CardName), apply = ChestRewards.GiveCard },
        new RewardDef { id = "node", saveCode = "no", since = "1.1", nameKey = "chest.node.name", rarity = RewardRarity.Rare, icon = "tree_o_root", color = new Color(0.75f, 0.6f, 1f),
                        available = c => c.node != null, describe = c => Loc.T("chest.node.desc").Replace("{0}", c.node.name), apply = ChestRewards.GiveNode },
        new RewardDef { id = "reroll", saveCode = "rr", since = "1.1", nameKey = "chest.reroll.name", rarity = RewardRarity.Rare, weight = 0.8f, icon = "tree_g_fate", color = new Color(0.6f, 0.85f, 1f),
                        available = c => c.specials != null && !SoulWeight.NoReroll, describe = c => Loc.T("chest.reroll.desc"), apply = ChestRewards.GiveReroll },
        new RewardDef { id = "dormant", saveCode = "do", since = "1.1", nameKey = "chest.dormant.name", rarity = RewardRarity.Rare, weight = 0.9f, icon = "curse", color = new Color(0.85f, 0.5f, 1f),
                        available = c => c.dormantId >= 0, describe = c => Loc.T("chest.dormant.desc").Replace("{0}", c.DormantName) + "\n" + c.specials.DormantSlotNote(), apply = ChestRewards.GiveDormant },
        new RewardDef { id = "essence", saveCode = "es", since = "1.1", nameKey = "chest.essence.name", rarity = RewardRarity.Rare, heal = true, icon = "essence", color = new Color(1f, 0.4f, 0.5f),
                        describe = c => Loc.T("chest.essence.desc"), apply = c => ChestRewards.GiveMaxHp(c, 20f) },
        // ---------------- 전설 (룰렛)
        new RewardDef { id = "jackpot", saveCode = "jp", since = "1.1", nameKey = "chest.jackpot.name", rarity = RewardRarity.Legendary, icon = "chest", color = new Color(1f, 0.75f, 0.2f),
                        describe = c => Loc.T("chest.jackpot.desc") },
    };

    // ================================================================= 유물 (1.2~, MapProps 봉인된 석관 · SpecialAbilities.Relics)
    // 첫 출시 8종: 영웅마다 고유 장치(활시위 · 플라스크 · 탄창 · 영혼 개수)에 묶이지 않고 누르면 바로 나가는 스킬만
    //   (조준해서 떼는 그림자 대시 · 화염 장판 · 갈고리, 활 전용 사냥꾼의 집중, 플라스크 전용 현자의 돌, 시계공 되감기와 겹치는 시간 역행 물약,
    //    영혼을 모아야 하는 영혼 폭발, 체력을 깎는 희생의 계약, 분신, 맵 물체 「시간 정지」와 같은 값을 건드리는 시간 왜곡 · 소매치기 · 대지 가르기는 다음 업데이트로)
    public static readonly List<RelicDef> Relics = new List<RelicDef>
    {
        new RelicDef { id = "warcry",    saveCode = "wc", since = "1.2", abilityId = SpecialAbilities.KitWarCry },
        new RelicDef { id = "judgment",  saveCode = "jd", since = "1.2", abilityId = SpecialAbilities.KitJudgment },
        new RelicDef { id = "deathmark", saveCode = "dm", since = "1.2", abilityId = SpecialAbilities.KitDeathMark },
        new RelicDef { id = "caltrops",  saveCode = "ct", since = "1.2", abilityId = SpecialAbilities.KitCaltrops },
        new RelicDef { id = "falcon",    saveCode = "fa", since = "1.2", abilityId = SpecialAbilities.KitFalcon },
        new RelicDef { id = "gale",      saveCode = "ga", since = "1.2", abilityId = SpecialAbilities.KitGale },
        new RelicDef { id = "giant",     saveCode = "gi", since = "1.2", abilityId = SpecialAbilities.KitGiant },
        new RelicDef { id = "skeletons", saveCode = "sk", since = "1.2", abilityId = SpecialAbilities.SkeletonsId },
    };

    public static RelicDef RelicByCode(string code)
    {
        foreach (RelicDef r in Relics) if (r.saveCode == code) return r;
        return null;
    }

    public static RelicDef RelicByAbility(int abilityId)
    {
        foreach (RelicDef r in Relics) if (r.abilityId == abilityId) return r;
        return null;
    }

    // ================================================================= 부서지는 맵 물체 (1.2~, MapProps)
    // 스테이지 번호: 0 지하 묘역 · 1 불타는 지옥 · 2 초원 · 3 영혼의 심연
    public const string Sarcophagus = "sarcophagus";
    // 1.1.2~ size = 몸통 크기 · 더 잘 보이게 키움 (항아리 · 나무통 1.9 → 2.2 · 화로 · 수정 2 → 2.3 · 석관 3.2 → 3.6, 그림은 둘레 테두리까지 40칸)
    public static readonly List<PropDef> Props = new List<PropDef>
    {
        new PropDef { id = "urn",     saveCode = "u", since = "1.2", nameKey = "prop.urn.name",     icon = "prop_urn",     stage = 0, size = 2.2f, color = new Color(0.85f, 0.62f, 0.45f) },
        new PropDef { id = "brazier", saveCode = "z", since = "1.2", nameKey = "prop.brazier.name", icon = "prop_brazier", stage = 1, size = 2.3f, color = new Color(1f, 0.55f, 0.2f) },
        new PropDef { id = "barrel",  saveCode = "b", since = "1.2", nameKey = "prop.barrel.name",  icon = "prop_barrel",  stage = 2, size = 2.2f, color = new Color(0.8f, 0.55f, 0.3f) },
        new PropDef { id = "crystal", saveCode = "y", since = "1.2", nameKey = "prop.crystal.name", icon = "prop_crystal", stage = 3, size = 2.3f, color = new Color(0.6f, 0.85f, 1f) },
        new PropDef { id = Sarcophagus, saveCode = "S", since = "1.2", nameKey = "prop.sarco.name", icon = "prop_sarcophagus", sarcophagus = true, size = 3.6f, color = new Color(0.75f, 0.5f, 1f) },
    };

    public static PropDef PropById(string id)
    {
        foreach (PropDef p in Props) if (p.id == id) return p;
        return null;
    }

    // ================================================================= 맵 물체가 떨어뜨리는 것 (1.2~, MapProps)
    // 1.1.2~ color = 바닥 빛 · 고리 색 (고기는 자석 빨강과 헷갈려서 회복 초록으로)
    public static readonly List<DropDef> Drops = new List<DropDef>
    {
        new DropDef { id = "coin",     saveCode = "co", since = "1.2", nameKey = "drop.coin.name",     weight = 34f, color = new Color(1f, 0.82f, 0.3f),  apply = MapProps.DropCoins },
        new DropDef { id = "meat",     saveCode = "me", since = "1.2", nameKey = "drop.meat.name",     weight = 22f, heal = true, icon = "drop_meat", color = new Color(0.5f, 1f, 0.55f), apply = MapProps.DropMeat },
        new DropDef { id = "gold",     saveCode = "go", since = "1.2", nameKey = "drop.gold.name",     weight = 14f, color = new Color(1f, 0.9f, 0.5f),   apply = MapProps.DropGold },
        new DropDef { id = "magnet",   saveCode = "mg", since = "1.2", nameKey = "drop.magnet.name",   weight = 12f, icon = "drop_magnet", color = new Color(1f, 0.45f, 0.45f), apply = MapProps.DropMagnet },
        new DropDef { id = "timestop", saveCode = "ts", since = "1.2", nameKey = "drop.timestop.name", weight = 9f,  icon = "drop_hourglass", color = new Color(0.6f, 0.85f, 1f), apply = MapProps.DropTimeStop },
        new DropDef { id = "purify",   saveCode = "pu", since = "1.2", nameKey = "drop.purify.name",   weight = 9f,  icon = "drop_purify", color = new Color(1f, 0.97f, 0.8f), apply = MapProps.DropPurify },
    };

    // ================================================================= 저주
    public static readonly List<CurseDef> Curses = new List<CurseDef>
    {
        new CurseDef { id = "altar", saveCode = "k", since = "1.0.1", nameKey = "curse.altar.name", descKey = "curse.altar.desc", hpMul = 1.3f, rewardMul = 1.5f, minSlot = 1 },
    };
}

// 이어하기 (RunSave) 에 판 콘텐츠 상태를 적고 되살림: 이벤트 시드 · 이번 장 계획 · 쓴 이벤트 · 상자 전설 · 촉매 횟수 (1.1~)
// 1.2~ 장착한 유물(saveCode) · 맵 물체 시드 · 이번 장에 부순 물체 수 · 석관을 연 장 (MapProps)
public static class RunContent
{
    public static void Save(RunSave.Data d)
    {
        ChapterEvents.SaveRun(d);
        ChestRewards.SaveRun(d);
        MapProps.SaveRun(d);
        SoulBrand.SaveRun(d);           // 1.2~ 영혼 각인
    }

    public static void Restore(RunSave.Data d)
    {
        ChapterEvents.RestoreRun(d);
        ChestRewards.RestoreRun(d);
        MapProps.RestoreRun(d);
        SoulBrand.RestoreRun(d);        // 1.2~ 영혼 각인 (옛 저장은 각인 없는 판)
    }
}

// ================================================================= 영혼 각인 (1.2~, SoulBrand)
// 이야기 모드 판 규칙 카드: 판을 시작할 때 3택 1, 2장 · 3장 보스를 쓰러뜨릴 때 한 장씩 더 (한 판 최대 3장)
//   장점과 단점이 함께 있음. 수치형은 일일 도전과 같은 DailyStat 배율 (GameMode 가 곱함)
//   + 공격력 · 최대 체력 % (고를 때 한 번 · 판 중간 저장 'a' · 'h' 기록으로 이어할 때 다시 적용)
//   saveCode 는 이어하기(RunSave.Data.soulBrands)에 적는 짧은 이름 — 바꾸지 말 것
//   이름 · 설명 · 해금 조건은 LocTable.SoulBrand 의 brand.<id>. 열쇠 (4개 언어), 그림은 Icons/choice_brand_<id> (tools/pixelart/make_brand_icons.py)
public sealed class SoulBrandDef
{
    public string id, saveCode, since;
    public Color color;
    public (DailyStat stat, float mul)[] muls = new (DailyStat stat, float mul)[0];
    public int attack;                      // 공격력 % (+40 = +40%, 고를 때 한 번)
    public int maxHp;                       // 최대 체력 % (-30 = 그때 최대 체력의 -30%)
    public float pointMul = 1f;             // 포인트 배율 (어려운 각인일수록 큼, Point.AddPoint 에서 곱함)
    public System.Func<bool> unlocked;      // null = 처음부터 열림
    public System.Func<string> progress;    // 해금까지 진행 (짧은 숫자 글, 없으면 null)
    public string Icon => "brand_" + id;
    public string NameKey => "brand." + id + ".name";
    public string Name => Loc.T(NameKey);
    public string Desc => Loc.T("brand." + id + ".desc");
    public string Hint => Loc.T("brand." + id + ".hint");
    public bool Unlocked => unlocked == null || unlocked();
}

public static partial class ContentRegistry
{
    // 처음 열린 4장 + 지금 있는 기록(누적 처치 · 스킬 진화 · 난이도 클리어 · 영웅 클리어 · 무한 모드)으로 여는 8장
    public static readonly List<SoulBrandDef> SoulBrands = new List<SoulBrandDef>
    {
        // ---------------- 처음부터
        new SoulBrandDef { id = "glass", saveCode = "gc", since = "1.2", color = new Color(0.65f, 0.88f, 1f), attack = 40, maxHp = -30, pointMul = 1.15f },
        new SoulBrandDef { id = "tide", saveCode = "td", since = "1.2", color = new Color(0.45f, 0.7f, 1f), pointMul = 1.1f,
                           muls = new[] { (DailyStat.Spawn, 0.75f), (DailyStat.Reward, 1.25f) } },
        new SoulBrandDef { id = "giant", saveCode = "gh", since = "1.2", color = new Color(1f, 0.45f, 0.4f), pointMul = 1.1f,
                           muls = new[] { (DailyStat.BossHp, 1.4f), (DailyStat.Gauge, 1.5f) } },
        new SoulBrandDef { id = "iron", saveCode = "io", since = "1.2", color = new Color(0.78f, 0.8f, 0.86f), attack = -15, pointMul = 1f,
                           muls = new[] { (DailyStat.EnemyDamage, 0.75f) } },
        // ---------------- 기록으로 열림
        new SoulBrandDef { id = "greed", saveCode = "gr", since = "1.2", color = new Color(1f, 0.8f, 0.35f), pointMul = 1.15f,
                           muls = new[] { (DailyStat.Reward, 1.6f), (DailyStat.EnemyHp, 1.3f) },
                           unlocked = () => SoulBrand.TotalKills >= SoulBrand.GreedKills,
                           progress = () => Mathf.Min(SoulBrand.TotalKills, SoulBrand.GreedKills).ToString("N0") + " / " + SoulBrand.GreedKills.ToString("N0") },
        new SoulBrandDef { id = "frenzy", saveCode = "fz", since = "1.2", color = new Color(1f, 0.4f, 0.35f), attack = 25, pointMul = 1.2f,
                           muls = new[] { (DailyStat.EnemySpeed, 1.2f), (DailyStat.EnemySkill, 0.77f) },
                           unlocked = () => GameMode.HasCleared(Difficulty.Normal) },
        new SoulBrandDef { id = "blood", saveCode = "bp", since = "1.2", color = new Color(0.95f, 0.3f, 0.35f), maxHp = -40, pointMul = 1.25f,
                           muls = new[] { (DailyStat.Reward, 1.4f), (DailyStat.Gauge, 1.4f) },
                           unlocked = () => SoulBrand.SkillEvoCount >= 5,
                           progress = () => Mathf.Min(SoulBrand.SkillEvoCount, 5) + " / 5" },
        new SoulBrandDef { id = "calm", saveCode = "st", since = "1.2", color = new Color(0.7f, 0.9f, 1f), pointMul = 1f,
                           muls = new[] { (DailyStat.EnemySpeed, 0.85f), (DailyStat.Gauge, 0.65f) },
                           unlocked = () => CharacterData.ConditionMet(CharUnlock.EasyFast) },
        new SoulBrandDef { id = "tyrant", saveCode = "tc", since = "1.2", color = new Color(0.8f, 0.5f, 0.9f), attack = 20, pointMul = 1.3f,
                           muls = new[] { (DailyStat.BossHp, 1.6f), (DailyStat.EnemyDamage, 1.2f) },
                           unlocked = () => GameMode.HasCleared(Difficulty.Hard) },
        new SoulBrandDef { id = "surge", saveCode = "ms", since = "1.2", color = new Color(0.8f, 0.55f, 1f), pointMul = 1.15f,
                           muls = new[] { (DailyStat.Gauge, 1.8f), (DailyStat.EnemySkill, 0.71f) },
                           unlocked = () => SoulBrand.SkillEvoCount >= 15,
                           progress = () => Mathf.Min(SoulBrand.SkillEvoCount, 15) + " / 15" },
        new SoulBrandDef { id = "bulwark", saveCode = "bw", since = "1.2", color = new Color(0.75f, 0.75f, 0.8f), attack = -20, maxHp = 40, pointMul = 1f,
                           unlocked = () => Mastery.Level(CharacterId.Swordsman) >= 1 },
        new SoulBrandDef { id = "dance", saveCode = "dd", since = "1.2", color = new Color(1f, 0.55f, 0.3f), attack = 30, pointMul = 1.35f,
                           muls = new[] { (DailyStat.EnemyDamage, 1.5f), (DailyStat.Reward, 1.3f) },
                           unlocked = () => EndlessMode.BestSeconds >= 600f,
                           progress = () => EndlessMode.Clock(Mathf.Min(EndlessMode.BestSeconds, 600f)) + " / 10:00" },
    };

    public static SoulBrandDef SoulBrandByCode(string code)
    {
        foreach (SoulBrandDef b in SoulBrands) if (b.saveCode == code) return b;
        return null;
    }
}
