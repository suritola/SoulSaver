using System.Collections.Generic;
using UnityEngine;

// 보물 상자 보상 (1.1~, ChestRewards): 카드 하나 +1단계 · 쌓인 레벨업 +1 · 진화 촉매
// 카드는 시작 카드처럼 레벨이 오르지 않는 'f' 기록으로 남겨 이어하기에서 그대로 다시 적용 (ReplayPick)
public partial class LevelShop
{
    // 레벨업 +1 을 줄 수 있는지 (최대 레벨이면 안 줌)
    public bool ChestCanLevel
    {
        get
        {
            if (bul == null) bul = FindFirstObjectByType<PlayerController>();
            return bul != null && bul.level + PendingLevels < LevelCap;
        }
    }

    // 이미 배웠고 아직 최대가 아닌 카드 중 하나 (없으면 -1)
    public int ChestCardCandidate()
    {
        List<int> open = new List<int>();
        int n = Mathf.Min(Total_abilitys, Mathf.Min(ability_level.Length, ability_selected.Length));
        for (int i = 0; i < n; i++)
        {
            if (i == SupplyId || IsGrowth(i) || ability_level[i] <= 0 || ability_selected[i]) continue;
            if (!KitFits(i) || SigLocked(i) || !PathAllows(i)) continue;        // 갈래 규칙: 고른 갈래 · 공용 카드만 (LevelShop.Paths)
            open.Add(i);
        }
        return open.Count > 0 ? open[Random.Range(0, open.Count)] : -1;
    }

    public string ChestCardName(int id)
    {
        if (id < 0 || id >= ability_name.Length) return "";
        if (string.IsNullOrEmpty(ability_name[id])) setAbilitys();
        return ability_name[id];
    }

    // 카드 하나 +1단계 (레벨은 오르지 않음 · 재료가 모두 최대면 그 자리에서 스킬 진화)
    public void ChestUpgradeCard(int id)
    {
        if (id < 0 || id >= ability_level.Length) return;
        RunSave.Record("f" + id);
        ReplayPick(id, true);
        Hostile.Play("chime", 0.7f, 1.4f);
    }

    // 진화 촉매: 재료를 모두 배운(1단계 이상) 진화 중 하나 (없으면 null)
    public SkillEvo ChestCatalystCandidate()
    {
        List<SkillEvo> ready = new List<SkillEvo>();
        foreach (SkillEvo e in MyEvos)
        {
            if (evolved.Contains(e.key)) continue;
            bool ok = true;
            foreach (int p in e.parts)
                if (LevelOf(p) <= 0 || MaxLevelOf(CharacterData.Selected, p) <= 0 || !KitFits(p)) { ok = false; break; }
            if (ok) ready.Add(e);
        }
        return ready.Count > 0 ? ready[Random.Range(0, ready.Count)] : null;
    }

    // 재료 카드를 모두 최대로 올려 진화시킴 (고를 때와 같은 길 · 기록 'f')
    public bool ChestCatalyst(SkillEvo e)
    {
        if (e == null || evolved.Contains(e.key)) return false;
        foreach (int p in e.parts)
        {
            for (int guard = 0; guard < 12 && !Maxed(p); guard++)
            {
                RunSave.Record("f" + p);
                ReplayPick(p, true);
            }
        }
        return evolved.Contains(e.key);
    }
}
