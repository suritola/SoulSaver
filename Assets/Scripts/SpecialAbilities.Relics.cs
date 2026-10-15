using System.Collections.Generic;
using UnityEngine;

// 유물 칸 (1.2~, 로드맵 2-3): 봉인된 석관(MapProps)에서 고른 「잠든 스킬」을 [Q](GameAction.Relic) 하나로 씀 · 한 판 최대 2개
//   스킬 1~3(E · F · C) 칸과 따로 · 쿨타임 1.5배 (StartCooldown) · 두 개면 누를 때 준비된 것부터 (둘 다 쿨타임이면 남은 시간이 짧은 쪽을 알림)
//   장착하면 equipped 에도 들어가 Has() · 처치 효과 · HUD · 결과 화면 능력 목록이 그대로 따라옴 (skills 목록에는 넣지 않음)
//   이어하기: RunSave.Data.relics (RelicDef.saveCode, MapProps.SaveRun · RestoreRun)
public partial class SpecialAbilities
{
    public const int MaxRelics = 2;
    public const float RelicCooldownMul = 1.5f;

    readonly List<int> relics = new List<int>();
    public IReadOnlyList<int> Relics => relics;
    public bool IsRelic(int id) => relics.Contains(id);

    // silent: 이어하기로 되살릴 때 (알림 · 소리 없음)
    public bool EquipRelic(int id, bool silent)
    {
        if (abilities == null || id < 0 || id >= abilities.Length || abilities[id] == null) return false;
        if (relics.Count >= MaxRelics || equipped.Contains(id)) return false;
        equipped.Add(id);
        relics.Add(id);
        KitOnEquip(id, false);
        RebuildHudRows();
        if (silent) return true;
        AnnounceKey(id, true);          // 1.1.2~ 「[Q] 이름 / Q 키를 눌러 사용」 + 칸 반짝임 (SpecialAbilities.KeyGuide)
        RelicDef codexRelic = ContentRegistry.RelicByAbility(id);
        if (codexRelic != null) CodexSeen.Mark("relic." + codexRelic.saveCode, Loc.T(abilities[id].name));      // 1.2~ 발견형 도감
        Hints.Show("relic", "유물은 [{RELIC}]로 씁니다. 두 개를 가지면 준비된 것부터 나갑니다. 쿨타임은 1.5배입니다.");
        if (fx != null)
        {
            fx.Play("shimmer", 0.8f, 0.9f);
            fx.Play("chime", 0.7f, 1.2f);
            if (player != null) fx.FloatText(player.transform.position + Vector3.up * 1.4f, Loc.T("relic.equip").Replace("{0}", Loc.T(abilities[id].name)), new Color(0.8f, 0.6f, 1f), 6f, 0f);
        }
        if (player != null) Fx.Spawn("fx_soulburst", player.transform.position, 4f, new Color(0.75f, 0.5f, 1f), 18f);
        return true;
    }

    // 유물 키 (UpdateAbilities 가 매 프레임)
    void HandleRelicKey()
    {
        if (relics.Count == 0) return;
        // 상점 제단 앞에서는 상호작용 키가 상점 열기 (유물 키를 같은 키로 바꿨을 때)
        if (KeyBindings.Get(GameAction.Relic) == KeyBindings.Get(GameAction.Interact) && ShopStall.PlayerNear) return;
        if (!KeyBindings.Down(GameAction.Relic)) return;
        foreach (int r in relics) KeyTried(r);      // 두 유물이 같은 키
        if (player.IsSkillUsing) return;
        int wait = -1;
        float soonest = float.MaxValue;
        foreach (int id in relics)
        {
            float left = CooldownUntil(id) - Time.time;
            if (left <= 0f) { UseSkill(id); return; }
            if (left < soonest) { soonest = left; wait = id; }
        }
        if (wait < 0) return;
        fx.Play("buzz", 0.6f);
        fx.FloatText(player.transform.position, Loc.T(abilities[wait].name) + " " + soonest.ToString("0.0") + Loc.T("초"), new Color(0.7f, 0.66f, 0.72f), 4f, 0.4f);
    }
}
