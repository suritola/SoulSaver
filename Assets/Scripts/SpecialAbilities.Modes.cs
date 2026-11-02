using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// 2.0.0~ 특별 모드가 무기 진화 · 영혼 트리 · 특수 능력을 만지는 곳 (modes.md 1-8 4 · 5번)
//   지금은 영혼 릴레이 교대 (영웅 칸 정리 · 진화 되돌림 · 기준 공격력 비율) — 역류가 쓸 UndoEvolution · 축복도 이 파일에 이어 붙임
public partial class SpecialAbilities
{
    // 기준 공격력(baseAttack)을 영웅 배율 바뀐 만큼 같은 비율로 (AddAttack 이 damage += BaseAttack × pct 덧셈이라 둘을 같은 비율로 바꾸면 성립)
    public void RescaleBase(float mul)
    {
        if (baseAttack > 0f && mul > 0f) baseAttack *= mul;
    }

    // 이어하기 재생: 저장한 기준 공격력은 셋째 주자 배율이 곱해진 값 → 첫 주자 시점으로 되돌려 둠 (재생 중 교대가 다시 곱해 저장값과 같아짐)
    public void RelayRebaseForReplay(float fromMul, float toMul)
    {
        if (baseAttack > 0f && fromMul > 0f) baseAttack *= toMul / fromMul;
    }

    static readonly Regex HeroNodeKey = new Regex(@"^(w\.(reach|arc|blast)\d*|u\.kit\d*|u\.shadow\d*|u\.t-?\d+\.\d|m\d+\.t\d|o\.\d+\+?)$");

    // 영혼 트리 영웅 칸 정리: 영웅 전용 칸(베기 사거리 · 각도 · 폭발 · 필살기 강화 · 그림자 숙련 · 형태 단련 · 필살기 특성 줄 · 패시브)만 지우고
    //   쓴 영혼 조각의 70% 를 돌려줌 · 효과는 키트 필드에 있어(reachMul 등) 키트와 함께 사라짐 · 공용 칸(무기 화력 · 생존 · 재물 …)은 그대로
    //   Override 가 아직 앞 영웅이고 진화도 되돌리기 전에 부름 (트리 칸 비용을 지금 영웅 기준 트리에서 읽음)
    public int RelayDropHeroNodes()
    {
        Dictionary<string, int> cost = new Dictionary<string, int>();
        try { foreach (SoulNode n in BuildSoulTree()) cost[n.key] = n.cost; }
        catch (System.Exception e) { Debug.LogException(e); }
        List<string> drop = new List<string>();
        foreach (string k in ownedNodes) if (HeroNodeKey.IsMatch(k)) drop.Add(k);
        int paid = 0;
        foreach (string k in drop)
        {
            ownedNodes.Remove(k);
            if (cost.TryGetValue(k, out int c)) paid += c;
        }
        int refund = Mathf.RoundToInt(paid * 0.7f);
        if (refund > 0) SoulShards.Restore(SoulShards.Amount + refund, SoulShards.Total, Mathf.Max(0, SoulShards.Spent - refund));
        affordTree = null;
        return refund;
    }

    // 특수 능력(무기 · 스킬 · 패시브)을 모두 내려놓음 — 유물은 영웅과 무관해 그대로. 내려놓은 수만큼 특수 능력 포인트로 돌려줌
    public int RelayUnequipAll()
    {
        List<int> gone = new List<int>();
        foreach (int id in equipped) if (!relics.Contains(id)) gone.Add(id);
        foreach (int id in gone)
        {
            if (id == KitPlate && player != null)
            {
                if (evolved.Contains(id)) { player.def -= 0.08f; player.speed *= 0.95f; }      // KitOnEquip 의 반대 (진화분 먼저)
                player.def -= 0.12f;
                player.speed /= 0.95f;
            }
            equipped.Remove(id);
            weapons.Remove(id);
            skills.Remove(id);
            evolved.Remove(id);
        }
        if (gone.Contains(OrbsId))
        {
            foreach (Transform o in orbs) if (o != null) Destroy(o.gameObject);
            orbs.Clear();
        }
        weaponIndex = -1;
        cooldownUntil.Clear();
        cooldownLength.Clear();
        pactUntil = 0f;
        affordTree = null;
        RebuildHudRows();
        return gone.Count;
    }

    // 무기 진화 1차 · 2차를 버리고 같은 단계만큼 새 영웅이 다시 고르게 (owed) · 각성(3번째)은 공통 능력치라 이어짐
    public void RelayResetEvolution(out int owed)
    {
        owed = Mathf.Min(EvolutionTier, 2);
        EvolutionTier = 0;
        gunEvo1 = gunEvo2 = -1;
        weaponHistory.Clear();
        weaponHistory.Add(-1);
        historyTier.Clear();
        historyTier[-1] = 0;
        affordTree = null;
        RebuildHudRows();
    }
}

// 2.0.0~ 역류 (modes.md 2-5 · 2-6): 「역류의 축복」(영혼 트리 대신 되돌릴 수 있는 덩어리) · 무기 진화 되돌림
//   축복 = 공격력 +15% × 4 · 공격 간격 −6% × 3 · 치명타 +5% × 3 · 최대 체력 +10% × 3 — 모두 정확한 반대 연산이 있음 (AddAttack · AddRate · TreeCrit · 체력은 더한 몫을 쌓아 둠)
public partial class SpecialAbilities
{
    public const int BlessAttack = 0, BlessRate = 1, BlessCrit = 2, BlessHp = 3;
    public static readonly int[] BlessMax = { 4, 3, 3, 3 };
    const float BlessAttackPct = 0.15f, BlessRateMul = 0.94f, BlessCritAdd = 0.05f, BlessHpMul = 1.1f;
    public readonly int[] blessing = new int[4];                 // 지금 걸린 조각 수
    readonly System.Collections.Generic.Stack<float> blessHpAdds = new System.Collections.Generic.Stack<float>();     // 더한 최대 체력 몫 (그대로 되돌리려고)

    // 판 시작: 조각을 모두 채움
    public void RefluxFillBlessing()
    {
        for (int k = 0; k < BlessMax.Length; k++)
            while (blessing[k] < BlessMax[k]) RefluxBless(k, 1);
    }

    // 조각 하나 더하기(+1) · 빼기(-1)
    public void RefluxBless(int kind, int sign)
    {
        if (player == null || kind < 0 || kind >= blessing.Length) return;
        if (sign > 0 && blessing[kind] >= BlessMax[kind]) return;
        if (sign < 0 && blessing[kind] <= 0) return;
        switch (kind)
        {
            case BlessAttack: AddAttack(BlessAttackPct * sign); break;
            case BlessRate: AddRate(sign > 0 ? BlessRateMul : 1f / BlessRateMul); break;
            case BlessCrit: TreeCrit += BlessCritAdd * sign; break;
            default:
                if (sign > 0)
                {
                    float add = player.PlayerMaxHealth * (BlessHpMul - 1f);
                    player.PlayerMaxHealth += add;
                    player.PlayerHealth += add;
                    blessHpAdds.Push(add);
                }
                else if (blessHpAdds.Count > 0)
                {
                    player.PlayerMaxHealth = Mathf.Max(1f, player.PlayerMaxHealth - blessHpAdds.Pop());
                    player.PlayerHealth = Mathf.Min(player.PlayerHealth, player.PlayerMaxHealth);
                }
                break;
        }
        blessing[kind] += sign;
        if (blessing[kind] == 0 && kind == BlessCrit) TreeCrit = Mathf.Max(0f, TreeCrit);
    }

    // 1차 무기 형태 후보 (메뉴의 「1차 무기」 세 칸과 같은 순서 — NextEvolutionOptions 의 걸러진 목록이 아니라 원래 목록)
    public int[] RefluxFormIds() => Gunner ? Tier1Options : KitFormOptions();

    public int RefluxBlessLeft { get { int n = 0; foreach (int c in blessing) n += c; return n; } }

    // 지금 가장 위 진화 단계의 이름 · 그림 · 무게 (잃기 목록): 3 각성 → 2 2차 → 1 1차
    public int RefluxTopTier => EvolutionTier;

    public string RefluxTopEvolutionName()
    {
        int tier = EvolutionTier;
        if (tier <= 0) return "";
        CharacterKit k = Kit;
        int id = tier >= 3 ? awakenId : Gunner ? (tier == 2 ? gunEvo2 : gunEvo1) : k == null ? -1 : (tier == 2 ? k.augment : k.form);
        return id == -1 && tier < 3 ? "" : EvolutionName(id, tier);
    }

    public Sprite RefluxTopEvolutionIcon()
    {
        int tier = EvolutionTier;
        if (tier <= 0) return null;
        CharacterKit k = Kit;
        int id = tier >= 3 ? awakenId : Gunner ? (tier == 2 ? gunEvo2 : gunEvo1) : k == null ? -1 : (tier == 2 ? k.augment : k.form);
        return id == -1 && tier < 3 ? null : EvolutionIcon(id);
    }

    // 무기 진화를 한 단계 되돌림 (위에서부터만: 각성 → 2차 → 1차)
    public void UndoEvolution()
    {
        int tier = EvolutionTier;
        if (tier <= 0) return;
        CharacterKit k = Kit;
        if (tier >= 3)
        {
            switch (awakenId - AwakenFirst)       // ApplyAwaken 의 반대
            {
                case 0: AddAttack(-AwakenAttack); break;
                case 1: AddRate(1f / AwakenRate); break;
                default: TreeCrit = Mathf.Max(0f, TreeCrit - AwakenCrit); TreeCritDamage -= AwakenCritDamage; break;
            }
            awakenId = -1;
        }
        else if (tier == 2)
        {
            int id;
            if (Gunner) { id = gunEvo2; gunEvo2 = -1; }
            else { id = k != null ? k.augment : -1; if (k != null) k.SetAugment(-1); }
            if (id >= 0)
            {
                if ((Gunner && id == gunEvo1) || (!Gunner && k != null && id == k.form)) historyTier[id] = 1;        // 2차에서 같은 계열을 골랐으면 1차 기록으로
                else historyTier.Remove(id);
            }
        }
        else
        {
            int id;
            if (Gunner) { id = gunEvo1; gunEvo1 = -1; }
            else { id = k != null ? k.form : -1; if (k != null) k.SetForm(-1); }
            if (id >= 0) { weaponHistory.Remove(id); historyTier.Remove(id); }
        }
        EvolutionTier = tier - 1;
        if (EvolutionLog.Count > 0) EvolutionLog.RemoveAt(EvolutionLog.Count - 1);
        affordTree = null;
        RebuildHudRows();
    }
}
