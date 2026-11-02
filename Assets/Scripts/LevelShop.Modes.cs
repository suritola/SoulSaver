using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 2.0.0~ 특별 모드가 레벨업 카드를 만지는 곳 (modes.md 1-8 3번 · 1-5 환급)
//   지금은 영혼 릴레이 (영웅 카드 버리기 · 환급 무료 카드) — 역류가 쓸 되돌림 · 자동 채우기도 이 파일에 이어 붙임
public partial class LevelShop
{
    // 판을 시작할 때 닫혀 있는 카드 칸 (11 · 12 · 13 은 그 영웅에게 카드가 없으면 · 비상 보급 · 재활용 · 갈래 입문/외길/새 카드 중 못 쓰는 칸)
    //   Start 의 첫 계산과 영혼 릴레이 교대 뒤 다시 계산이 같은 식을 씀
    bool ClosedAtStart(int i) => ((i == 11 || i == 12 || i == 13) && KitIcon(i) == null) || i == SupplyId || i == RecycleId || PathIdClosed(i);

    // 영웅마다 다른 카드 칸: 영웅 카드 · 고유 스킬 · 갈래 (공용 카드 1 · 4 · 5 · 8 · 9 · 10 · 성장 · 초월은 영웅과 무관)
    static IEnumerable<int> HeroCardIds()
    {
        foreach (int id in KitIds) yield return id;
        for (int id = SigFirstId; id < SigFirstId + SigCount; id++) yield return id;
        for (int id = Paths.SeedFirstId; id < Paths.IdEnd; id++) yield return id;
    }

    // 영혼 릴레이 교대: 앞 영웅의 카드를 모두 0 으로 (버린 단계 수 → 환급 계산) · 스킬 진화도 함께 지움
    //   Override 가 아직 앞 영웅일 때 부름 (KitIcon 등이 영웅을 보므로)
    public void RelayDropHero(out int dropped)
    {
        dropped = 0;
        List<int> gone = new List<int>();
        foreach (int id in HeroCardIds())
        {
            if (id < 0 || id >= ability_level.Length) continue;
            if (ability_level[id] > 0) { dropped += ability_level[id]; ability_level[id] = 0; }
            gone.Add(id);
        }
        // 거너 총탄 카드는 영웅 카드 칸과 같은 횟수라 이미 셌음 (두 번 세지 않음) · 효과만 0 으로
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        if (sp != null) System.Array.Clear(sp.gunCard, 0, sp.gunCard.Length);
        evolved.Clear();
        myEvos = null;
        if (abilityHUD != null) abilityHUD.RemoveAbilities(gone);
    }

    // 새 영웅이 정해진 뒤(Override 가 바뀐 뒤): 닫힌 칸을 새 영웅 기준으로 · 갈래 규칙 · 고유 스킬 효과를 새로
    public void RelayAfterHeroChange()
    {
        foreach (int id in HeroCardIds())
            if (id >= 0 && id < ability_selected.Length) ability_selected[id] = ClosedAtStart(id);
        SignatureSkills.Rebind(null);               // who · 구독 · 소품 정리 (옛 효과의 OnDestroy 가 Paths.End 를 부르므로 새 갈래를 열기 전에)
        PathsBegin();                               // Paths.BeginRun(this, 새 영웅) · myEvos · 창 상태 비움
        SignatureSkills.Ensure()?.Refresh(this);    // 새 영웅의 고유 스킬 단계 읽기
        setAbilitys();
    }

    // 환급 무료 카드 n장 (레벨이 오르지 않는 고르기): 첫 장은 새 영웅 갈래 입문 창 (아직 갈래가 없으므로 자동으로 그 창)
    //   RelayRun.FreeLeft 가 남은 장수 (저장 · 이어하기가 이 값을 씀) — 한 장 고를 때마다 하나 줄고 남았으면 다음 창
    public void RelayGrantFree(int n)
    {
        if (n <= 0) return;
        RelayRun.FreeLeft = n;
        StartCoroutine(RelayOpenFree());
    }

    bool relayFreeWaiting;
    IEnumerator RelayOpenFree()
    {
        if (relayFreeWaiting) yield break;
        relayFreeWaiting = true;
        // 열려 있는 창 · 시간 멈춤 · 연출이 끝난 뒤에 (진화 창 뒤)
        while (IsOpen || Time.timeScale != 1f || ShopOpen || ChoiceUI.Open || ESCmenu.IsOpen || PlayerController.Cinematic || BossUltimate.Active
               || StoryDirector.Playing || (StageManager.Instance != null && StageManager.Instance.IsMenuOpen))
            yield return null;
        relayFreeWaiting = false;
        if (RelayRun.FreeLeft <= 0) yield break;
        freePick = true;
        openLevelShop();
    }

    // onSelect: 무료 카드를 하나 골랐음
    void RelayFreePicked()
    {
        if (RelayRun.FreeLeft <= 0) return;
        RelayRun.FreeLeft--;
        RunSave.MarkDirty();
        if (RelayRun.FreeLeft > 0) StartCoroutine(RelayOpenFree());
    }
}

// 2.0.0~ 역류 (modes.md 2-5 · 2-6): 완성된 몸을 고정 시드로 채우고(AutoFill), 「무엇을 잃을까」로 카드 · 갈래를 하나씩 되돌림(UndoCard · RefluxLosePath)
//   되돌림은 ApplyPick / KitApply 의 반대 연산 — 같은 순서(갈래 → 고유 스킬 → 영웅 칸 → 공용)로 가름
public partial class LevelShop
{
    int refluxSkillBase;            // 자동 채우기 직전의 필살기 게이지 최대치 (카드 2 되돌림 기준)

    public struct RefluxCard
    {
        public int id, level, max;
        public bool evoPart;        // 지금 쓰는 스킬 진화의 재료 (잃으면 진화도 함께 사라짐)
        public string name;
        public Sprite icon;
    }

    // 역류에서 쓸모 없는 칸 (코인 · 경험치 · 자석) · 재활용 · 비상 보급은 처음부터 닫혀 있어 배우지 않음
    static bool RefluxUseless(int id) => id == 1 || id == 4 || id == 5;

    // 지금 쓰는 스킬 진화의 재료 카드 번호
    HashSet<int> RefluxEvoParts()
    {
        HashSet<int> parts = new HashSet<int>();
        foreach (SkillEvo e in MyEvos)
            if (evolved.Contains(e.key)) foreach (int p in e.parts) parts.Add(p);
        return parts;
    }

    // 잃을 수 있는 카드 (갈래 입문 · 외길 · 31번은 RefluxLosePath 가 따로)
    public List<RefluxCard> RefluxCards()
    {
        List<RefluxCard> list = new List<RefluxCard>();
        setAbilitys();
        HashSet<int> parts = RefluxEvoParts();
        CharacterId who = CharacterData.Selected;
        for (int id = 0; id < ability_level.Length; id++)
        {
            if (id == SupplyId || Paths.IsPathId(id) || IsGrowth(id) || RefluxUseless(id) || ability_level[id] <= 0) continue;
            list.Add(new RefluxCard
            {
                id = id, level = ability_level[id], max = Mathf.Max(ability_level[id], MaxLevelOf(who, id)), evoPart = parts.Contains(id),
                name = ability_name[id],
                icon = KitIcon(id) ?? (abilityHUD != null ? abilityHUD.GetIcon(id) : null),
            });
        }
        return list;
    }

    // 갈래 한 묶음 (입문 · 외길 · 31번 새 카드의 단계 합 · 이름 · 그림) — 없으면 false
    public bool RefluxPathInfo(out string name, out Sprite icon, out int steps)
    {
        name = ""; icon = null; steps = 0;
        if (!Paths.Active || Paths.A < 0) return false;
        CharacterId who = CharacterData.Selected;
        PathDef d = Paths.Def(Paths.A);
        name = d != null ? Loc.T(d.name) : "";
        if (Paths.B >= 0 && Paths.Def(Paths.B) != null) name += " + " + Loc.T(Paths.Def(Paths.B).name);
        for (int id = Paths.SeedFirstId; id < Paths.IdEnd; id++) steps += ability_level[id];
        icon = PathCardIcon(who, Paths.SeedFirstId + Paths.A);
        return true;
    }

    // ================================================================= 자동 채우기 (고정 시드 · 같은 선택이면 누구나 같은 몸)
    //   갈래 입문 → 39번 레벨업(레벨 40) 우선순위로 → 진화 3단계 → 역류의 축복
    public void AutoFill(CharacterId who, int path, int form, int seed)
    {
        bul = FindFirstObjectByType<PlayerController>();
        skill = FindFirstObjectByType<SkillGauge>();
        if (lv == null) lv = FindFirstObjectByType<Level>();
        if (bul == null) return;
        refluxSkillBase = skill != null ? skill.MaxSkillPoint : 0;
        Random.State saved = Random.state;
        Random.InitState(seed);
        RunSave.SetAutoFill(true);
        try
        {
            path = Mathf.Clamp(path, 0, Paths.Count - 1);
            if (Paths.Active) ReplayPick(Paths.SeedFirstId + path, true);       // 입문은 레벨이 오르지 않는 공짜 카드
            for (int step = 0; step < 39; step++)
            {
                PathWindow w = PathWindowDue();
                if (w == PathWindow.Crossroads) { ReplayPick(Paths.SingleId, false); continue; }     // 갈림길: 외길 (고른 갈래를 깊게)
                if (w == PathWindow.Seed) { ReplayPick(Paths.SeedFirstId + path, false); continue; }
                int pick = RefluxAutoPick(who, path);
                if (pick < 0) { if (bul.level < LevelCap) bul.level++; continue; }
                ReplayPick(pick, false);
            }
            // 무기 진화: 1차(고른 형태) → 2차(첫째 강화) → 각성(파괴 — 갈래 정의에 추천 각성이 없으므로)
            SpecialAbilities sp = SpecialAbilities.SharedInstance;
            if (sp != null)
            {
                int[] opts = sp.NextEvolutionOptions();
                int[] forms = sp.RefluxFormIds();
                int formId = forms != null && forms.Length > 0 ? forms[Mathf.Clamp(form, 0, forms.Length - 1)] : -1;
                if (opts != null && opts.Length > 0) sp.EvolveWeapon(System.Array.IndexOf(opts, formId) >= 0 ? formId : opts[0]);
                opts = sp.NextEvolutionOptions();
                if (opts != null && opts.Length > 0) sp.EvolveWeapon(opts[0]);
                opts = sp.NextEvolutionOptions();
                if (opts != null && opts.Length > 0) sp.EvolveWeapon(System.Array.IndexOf(opts, SpecialAbilities.AwakenFirst) >= 0 ? SpecialAbilities.AwakenFirst : opts[0]);
                sp.RefluxFillBlessing();
            }
            if (bul != null) bul.PlayerHealth = bul.PlayerMaxHealth;
        }
        finally
        {
            RunSave.SetAutoFill(false);
            Random.state = saved;
        }
        freePick = false;
        setAbilitys();
        RefluxRefreshHud();
    }

    // 세 장 중 하나: ① 고른 갈래 카드 · 그 갈래 31번 ② 아직 덜 찬 스킬 진화 재료 ③ 이미 가진 카드 ④ 공용 생존(8 · 9 · 10) ⑤ 나머지 — 성장 카드는 고르지 않음
    int RefluxAutoPick(CharacterId who, int path)
    {
        List<int> picks = new List<int>(3);
        if (Paths.Active) DrawPathCards(picks, null);
        else RefluxLegacyDraw(picks);
        int best = -1;
        float bestScore = -1f;
        foreach (int id in picks)
        {
            if (id < 0 || IsGrowth(id) || id >= ability_level.Length) continue;
            float s = 1f;
            if (RefluxUseless(id)) s = 0.1f;
            else if (Paths.Active && Paths.PathOfCard(who, id) == path) s = 5f;
            else if (RefluxIsEvoMaterial(id)) s = 4f;
            else if (ability_level[id] > 0) s = 3f;
            else if (id == 8 || id == 9 || id == 10) s = 2f;
            s += Random.value * 0.01f;        // 같은 점수면 시드로 정해진 순서
            if (s > bestScore) { bestScore = s; best = id; }
        }
        return best;
    }

    // 아직 완성되지 않은 스킬 진화의 재료인가
    bool RefluxIsEvoMaterial(int id)
    {
        foreach (SkillEvo e in MyEvos)
            if (!evolved.Contains(e.key) && System.Array.IndexOf(e.parts, id) >= 0) return true;
        return false;
    }

    // 갈래 규칙이 아닌 영웅(지금은 없음) · 방어용: 배울 수 있는 카드 중 DrawWeight 로 세 장
    void RefluxLegacyDraw(List<int> picks)
    {
        List<int> open = new List<int>();
        for (int i = 0; i < Total_abilitys; i++) if (Learnable(i)) open.Add(i);
        while (picks.Count < 3 && open.Count > 0)
        {
            float total = 0f;
            foreach (int c in open) total += DrawWeight(c);
            float roll = Random.Range(0f, total);
            int k = open.Count - 1;
            for (int i = 0; i < open.Count; i++) { roll -= DrawWeight(open[i]); if (roll < 0f) { k = i; break; } }
            picks.Add(open[k]);
            open.RemoveAt(k);
        }
        while (picks.Count < 3) picks.Add(GrowthFirstId);
    }

    // ================================================================= 되돌림 (한 장 = 그 카드의 모든 단계)
    public void UndoCard(int id)
    {
        if (id < 0 || id >= ability_level.Length || (Paths.IsPathId(id) && id != Paths.ExtraId)) return;
        int n = ability_level[id];
        if (n <= 0) return;
        bul = FindFirstObjectByType<PlayerController>();
        skill = FindFirstObjectByType<SkillGauge>();
        if (lv == null) lv = FindFirstObjectByType<Level>();
        if (bul == null) return;
        CharacterId who = CharacterData.Selected;
        if (id == Paths.ExtraId || IsSig(id)) { }                      // 단계만 (효과는 고유 스킬 · 영웅 효과가 단계를 읽음)
        else if (KitCardOf(who, id) != null)
        {
            if (CharacterData.IsGunner)
            {
                SpecialAbilities sp = SpecialAbilities.SharedInstance;
                int g = id == PierceId ? SpecialAbilities.GunRicochet : id == MultiId ? SpecialAbilities.GunExplosive : id == KnockId ? SpecialAbilities.GunIncendiary
                      : id == GlareId ? SpecialAbilities.GunHoming : id == ExtraAId ? SpecialAbilities.GunShock : SpecialAbilities.GunReloadWave;
                if (sp != null) sp.gunCard[g] = 0;
            }
            else
            {
                CharacterKit kit = CharacterKit.Instance;
                int slot = id == PierceId ? 0 : id == MultiId ? 1 : id == KnockId ? 2 : id == HungerId ? 4 : id == ExtraAId ? 5 : id == ExtraBId ? 6 : 3;
                if (kit != null) { kit.card[slot] = 0; kit.CardLost(slot); }
            }
        }
        else
        {
            switch (id)     // ApplyPick 의 공용 카드 반대 연산
            {
                case 0: bul.pene = Mathf.Max(1, bul.pene - n); break;
                case 1: bul.bonusCoin = Mathf.Max(0, bul.bonusCoin - n); break;
                case 2: if (skill != null && refluxSkillBase > 0) skill.SetMax(refluxSkillBase); break;
                case 3: bul.Skill_setTime /= Mathf.Pow(0.8f, n); break;
                case 4: if (lv != null) lv.bonusEXP = Mathf.Max(1f, lv.bonusEXP - 0.1f * n); break;
                case 5: bul.coinMagnetRange = Mathf.Max(0f, bul.coinMagnetRange - MagnetStep * n); break;
                case 6: bul.multiShot = Mathf.Max(1, bul.multiShot - n); break;
                case 7: bul.knockBack /= Mathf.Pow(KnockBackGrowth, n); break;
                case 8:
                    bul.PlayerMaxHealth = Mathf.Max(1f, Mathf.Round(bul.PlayerMaxHealth / Mathf.Pow(1.12f, n)));
                    bul.PlayerHealth = Mathf.Min(bul.PlayerHealth, bul.PlayerMaxHealth);
                    break;
                case 9: bul.def = Mathf.Max(0f, bul.def - DefStep * n); break;
                case 10: bul.regenPerSecond = Mathf.Max(0f, bul.regenPerSecond - RegenStep * n); break;
                case 11: bul.healOnKill = Mathf.Max(0f, bul.healOnKill - HealOnKillStep * n); break;
            }
        }
        ability_level[id] = 0;
        ability_selected[id] = true;         // 역류에는 레벨업이 없어 다시 배울 일이 없음 (닫아 둠)
        RefluxDropBrokenEvos();
        SignatureSkills.Ensure()?.Refresh(this);
        Paths.Recompute(this, id, true);
        RefluxRefreshHud();
    }

    // 재료가 하나라도 최대에서 내려간 스킬 진화를 지움
    void RefluxDropBrokenEvos()
    {
        List<SkillEvo> gone = new List<SkillEvo>();
        foreach (SkillEvo e in MyEvos)
        {
            if (!evolved.Contains(e.key)) continue;
            foreach (int p in e.parts) if (!Maxed(p)) { gone.Add(e); break; }
        }
        foreach (SkillEvo e in gone)
        {
            evolved.Remove(e.key);
            string key = e.key;
            EvoLog.RemoveAll(kv => kv.Key.key == key);
        }
    }

    // 갈래 전체를 잃음: 입문 · 외길 · 31번 단계 0 · 효과를 풀고 전투 훅을 멈춤 (Paths.Lose)
    public void RefluxLosePath()
    {
        if (!Paths.Active) return;
        List<int> ids = new List<int>();
        for (int id = Paths.SeedFirstId; id < Paths.IdEnd; id++)
        {
            ability_level[id] = 0;
            ability_selected[id] = true;
            ids.Add(id);
        }
        Paths.Lose();
        myEvos = null;
        RefluxDropBrokenEvos();
        SignatureSkills.Ensure()?.Refresh(this);
        if (abilityHUD != null) abilityHUD.RemoveAbilities(ids);
        RefluxRefreshHud();
    }

    // 모든 것을 잃음(맨몸): 카드 전부 · 갈래 (처치 직후 처음의 슬라임 전 — 진화 · 축복은 SpecialAbilities 쪽)
    public void RefluxStripCards()
    {
        foreach (RefluxCard c in RefluxCards()) UndoCard(c.id);
        RefluxLosePath();
    }

    // HUD 레벨업 스킬 칸을 지금 단계에 맞게 다시 (잃은 칸 지움 · 남은 진화는 다시 합침)
    void RefluxRefreshHud()
    {
        if (abilityHUD == null) return;
        List<int> zero = new List<int>();
        for (int id = 0; id < ability_level.Length; id++) if (ability_level[id] <= 0 && !IsGrowth(id)) zero.Add(id);
        abilityHUD.RemoveAbilities(zero);            // 진화 칸(id >= 1000)도 함께 지우고 합침 표시를 비움
        for (int id = 0; id < ability_level.Length; id++)
            if (ability_level[id] > 0 && !IsGrowth(id) && id != SupplyId) abilityHUD.SetAbility(id, ability_level[id]);
        foreach (SkillEvo e in MyEvos)
            if (evolved.Contains(e.key)) abilityHUD.Merge(e.parts, e.Id);
    }
}
