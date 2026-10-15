using System.Collections.Generic;
using UnityEngine;

// 보물 상자 보상 (1.1~, ChestRewards): 영혼 트리 칸 무료 · 잠든 특수 능력(스킬) 하나 장착
// 트리 칸은 BuyNode 와 같이 't' 기록 (조각은 쓰지 않음) · 잠든 능력은 StageManager.ReplayPicks → Equip 으로 's' 기록
public partial class SpecialAbilities
{
    // 지금 배울 수 있는 칸 중 하나 (막힌 칸 · 숨은 칸 제외, 없으면 null)
    // 패시브 칸("o.<번호>", 배우면 Equip)인데 그 패시브를 이미 들고 있으면 배워도 헛것이라 뺌
    public SoulNode ChestNodeCandidate()
    {
        List<SoulNode> open = new List<SoulNode>();
        foreach (SoulNode n in BuildSoulTree())
            if (IsOpenNode(n) && BlockReason(n) == null && !IsOwnedPassiveNode(n)) open.Add(n);
        return open.Count > 0 ? open[Random.Range(0, open.Count)] : null;
    }

    public bool ChestFreeNode(SoulNode n)
    {
        if (n == null || !IsOpenNode(n)) return false;
        ownedNodes.Add(n.key);
        RunSave.Record("t" + n.key);
        n.apply?.Invoke();
        if (fx != null) { fx.Play("chime", 0.6f, 1.3f); fx.Play("pulse", 0.4f, 1.6f); }
        return true;
    }

    bool IsOwnedPassiveNode(SoulNode n)
    {
        if (n.key == null || !n.key.StartsWith("o.") || n.key.EndsWith("+")) return false;
        return int.TryParse(n.key.Substring(2), out int id) && equipped.Contains(id);
    }

    // 잠든 특수 능력: 고른 캐릭터의 능력(CharacterData.pool) 중 스킬만, 칸이 남을 때만
    // 무기는 진화 방식과 겹치고, 패시브는 모두 영혼 트리 칸(o.<번호>)이라 빼고 트리 칸 보상으로 받음 (트리 밖에서 장착하면 칸이 열린 채 남음)
    public int DormantCandidate()
    {
        if (abilities == null || skills.Count >= MaxSkills) return -1;
        List<int> open = new List<int>();
        foreach (int id in CharacterData.Current.pool)
        {
            if (id < 0 || id >= abilities.Length || abilities[id] == null || equipped.Contains(id)) continue;
            if (abilities[id].kind != SpecialKind.Skill) continue;
            open.Add(id);
        }
        return open.Count > 0 ? open[Random.Range(0, open.Count)] : -1;
    }

    public string DormantName(int id) => abilities != null && id >= 0 && id < abilities.Length && abilities[id] != null ? Loc.T(abilities[id].name) : "";
}
