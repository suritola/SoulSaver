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

public static class ContentRegistry
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
    public static readonly List<RewardDef> Rewards = new List<RewardDef>
    {
        // ---------------- 일반
        new RewardDef { id = "coins", saveCode = "co", since = "1.1", nameKey = "chest.coins.name", rarity = RewardRarity.Common, icon = "gold", color = new Color(1f, 0.82f, 0.3f),
                        describe = c => Loc.T("chest.coins.desc").Replace("{0}", c.Coins.ToString()), apply = ChestRewards.GiveCoins },
        new RewardDef { id = "shards", saveCode = "sh", since = "1.1", nameKey = "chest.shards.name", rarity = RewardRarity.Common, icon = "soul", color = new Color(0.55f, 0.9f, 1f),
                        describe = c => Loc.T("chest.shards.desc").Replace("{0}", c.Shards.ToString()), apply = ChestRewards.GiveShards },
        new RewardDef { id = "life", saveCode = "li", since = "1.1", nameKey = "chest.life.name", rarity = RewardRarity.Common, heal = true, icon = "life", color = new Color(1f, 0.45f, 0.45f),
                        describe = c => Loc.T("chest.life.desc"), apply = c => ChestRewards.GiveMaxHp(c, 10f) },
        new RewardDef { id = "heal", saveCode = "he", since = "1.1", nameKey = "chest.heal.name", rarity = RewardRarity.Common, heal = true, icon = "life", color = new Color(1f, 0.6f, 0.6f),
                        available = c => c.player != null && c.player.PlayerHealth < c.player.PlayerMaxHealth,
                        describe = c => Loc.T("chest.heal.desc"), apply = ChestRewards.GiveHeal },
        new RewardDef { id = "level", saveCode = "lv", since = "1.1", nameKey = "chest.level.name", rarity = RewardRarity.Common, weight = 0.8f, icon = "essence", color = new Color(0.6f, 1f, 0.6f),
                        available = c => c.shop != null && c.shop.ChestCanLevel, describe = c => Loc.T("chest.level.desc"), apply = ChestRewards.GiveLevel },
        new RewardDef { id = "might", saveCode = "mi", since = "1.1", nameKey = "chest.might.name", rarity = RewardRarity.Common, weight = 0.8f, icon = "might", color = new Color(1f, 0.6f, 0.3f),
                        describe = c => Loc.T("chest.might.desc"), apply = ChestRewards.GiveMight },
        // ---------------- 희귀
        new RewardDef { id = "card", saveCode = "ca", since = "1.1", nameKey = "chest.card.name", rarity = RewardRarity.Rare, icon = "might", color = new Color(1f, 0.85f, 0.45f),
                        available = c => c.cardId >= 0, describe = c => Loc.T("chest.card.desc").Replace("{0}", c.CardName), apply = ChestRewards.GiveCard },
        new RewardDef { id = "node", saveCode = "no", since = "1.1", nameKey = "chest.node.name", rarity = RewardRarity.Rare, icon = "soul", color = new Color(0.75f, 0.6f, 1f),
                        available = c => c.node != null, describe = c => Loc.T("chest.node.desc").Replace("{0}", c.node.name), apply = ChestRewards.GiveNode },
        new RewardDef { id = "reroll", saveCode = "rr", since = "1.1", nameKey = "chest.reroll.name", rarity = RewardRarity.Rare, weight = 0.8f, icon = "chest", color = new Color(0.6f, 0.85f, 1f),
                        available = c => c.specials != null, describe = c => Loc.T("chest.reroll.desc"), apply = ChestRewards.GiveReroll },
        new RewardDef { id = "dormant", saveCode = "do", since = "1.1", nameKey = "chest.dormant.name", rarity = RewardRarity.Rare, weight = 0.9f, icon = "curse", color = new Color(0.85f, 0.5f, 1f),
                        available = c => c.dormantId >= 0, describe = c => Loc.T("chest.dormant.desc").Replace("{0}", c.DormantName), apply = ChestRewards.GiveDormant },
        new RewardDef { id = "essence", saveCode = "es", since = "1.1", nameKey = "chest.essence.name", rarity = RewardRarity.Rare, heal = true, icon = "essence", color = new Color(1f, 0.4f, 0.5f),
                        describe = c => Loc.T("chest.essence.desc"), apply = c => ChestRewards.GiveMaxHp(c, 20f) },
        // ---------------- 전설 (룰렛)
        new RewardDef { id = "jackpot", saveCode = "jp", since = "1.1", nameKey = "chest.jackpot.name", rarity = RewardRarity.Legendary, icon = "chest", color = new Color(1f, 0.75f, 0.2f),
                        describe = c => Loc.T("chest.jackpot.desc") },
    };

    // ================================================================= 저주
    public static readonly List<CurseDef> Curses = new List<CurseDef>
    {
        new CurseDef { id = "altar", saveCode = "k", since = "1.0.1", nameKey = "curse.altar.name", descKey = "curse.altar.desc", hpMul = 1.3f, rewardMul = 1.5f, minSlot = 1 },
    };
}

// 이어하기 (RunSave) 에 판 콘텐츠 상태를 적고 되살림: 이벤트 시드 · 이번 장 계획 · 쓴 이벤트 · 상자 전설 · 촉매 횟수 (1.1~)
public static class RunContent
{
    public static void Save(RunSave.Data d)
    {
        ChapterEvents.SaveRun(d);
        ChestRewards.SaveRun(d);
    }

    public static void Restore(RunSave.Data d)
    {
        ChapterEvents.RestoreRun(d);
        ChestRewards.RestoreRun(d);
    }
}
