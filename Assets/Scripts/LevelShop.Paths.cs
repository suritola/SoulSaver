using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 빌드 갈래의 카드 창 규칙 (2026-10-19 기획서 2장 · 6-1 의 0 · 1단계, 갈래 데이터 · 판 상태는 Paths)
//   입문 창: 갈래 A 가 없으면 레벨업 창이 입문 카드 3장(5개 중 · 최근 고른 갈래는 덜 · 일일 도전은 그날 시드) — 판 시작 무료 카드 자리
//   갈림길 창: 갈래 A 가 공명 I 에 닿았거나 레벨 8 에 오르는 창 → 남은 갈래 2장 + 외길(세 번째 칸) · 건너뛰기 없음 · 다시 뽑기는 그대로
//            창 내용은 판 상태에서 매번 다시 정하므로, 창이 못 뜨거나 이어하기로 끊겨도 다음 레벨업 창이 다시 갈림길 (레벨 12 강제 재표시를 겸함)
//   레벨업 세 장: 갈래 카드 최대 2장 + 공용 최소 1장 (공용이 없으면 갈래 카드, 둘 다 없으면 성장 카드) · 다른 갈래 카드는 나오지 않음
//   가중치: 다음 공명 · 완성 문턱에 1 ~ 2단계 남은 갈래 카드 ×1.5 · 행운 그대로 / 추천 반짝임 · 배지도 공명 · 교차 기준
//   외길: 갈래 A 카드의 최대 단계 +1 (최대 2단계 이상인 카드만 · 수치는 마지막 증가폭을 한 번 더) — 완성은 원래 최대에서, 늘어난 단계까지 모두면 극의
// 구판 저장을 이어하면 Paths.Active 가 꺼져 이 파일의 규칙은 모두 빠지고 예전 뽑기 그대로
public partial class LevelShop
{
    enum PathWindow { None, Seed, Crossroads }
    PathWindow pathWindow;
    int pathRerolls;            // 일일 도전 시드에 섞는 다시 뽑기 횟수 (같은 날 같은 순서로)

    // 입문 · 갈림길 창이 떠 있음 (건너뛰기 버튼을 숨김)
    bool PathWindowShown => IsOpen && pathWindow != PathWindow.None;

    // 새 판 (Start): 지난 판 상태를 버리고 이 영웅의 갈래 규칙으로
    void PathsBegin()
    {
        Paths.BeginRun(this, CharacterData.Selected);      // 영웅 데이터 예외는 안에서 잡아 구판 규칙으로
        if (TutorialRun.Active) Paths.Disable();            // 튜토리얼(거너 고정): 입문 창이 안내 문구와 겹치지 않게 구판 규칙 · 최근 갈래 기록도 남지 않음
        myEvos = null;
        pathWindow = PathWindow.None;
        pathRerolls = 0;
#if UNITY_EDITOR
        PathsValidate(CharacterData.Selected);
#endif
    }

#if UNITY_EDITOR
    // 데이터 점검 (에디터에서 판을 시작할 때 · 경고만): 영웅 카드가 정확히 한 갈래에 · 갈래 카드가 이 영웅에게 있는 카드 ·
    // 완성 진화의 재료가 그 갈래 안 · 교차 진화의 재료가 두 갈래 안 (Paths.<영웅>.cs 데이터를 고친 뒤 확인용)
    static void PathsValidate(CharacterId who)
    {
        HeroPaths h = Paths.Data(who);
        if (h == null) return;
        List<int> ids = new List<int>(KitIds);
        for (int i = 0; i < SigCount; i++) ids.Add(SigFirstId + i);
        ids.Add(Paths.ExtraId);
        foreach (int id in ids)
        {
            if (KitCardOf(who, id) == null) continue;
            int n = 0;
            foreach (PathDef d in h.Defs) if (d.cards != null && System.Array.IndexOf(d.cards, id) >= 0) n++;
            if (n != 1) Debug.LogWarning("[갈래] " + who + " 카드 " + id + "(" + CardName(who, id) + ") 가 갈래 " + n + "곳에 들어 있음 — 정확히 한 곳이어야 함");
        }
        List<SkillEvo> evos = EvosFor(who, true);
        SkillEvo Find(string key) => evos.Find(e => e.key == key);
        for (int p = 0; p < h.Defs.Length; p++)
        {
            PathDef d = h.Defs[p];
            if (d.cards == null) continue;
            foreach (int c in d.cards) if (KitCardOf(who, c) == null) Debug.LogWarning("[갈래] " + who + " 갈래 " + d.key + " 의 카드 " + c + " 가 이 영웅에게 없음");
            if (string.IsNullOrEmpty(d.evo)) continue;
            SkillEvo e = Find(d.evo);
            if (e == null) { Debug.LogWarning("[갈래] " + who + " 갈래 " + d.key + " 의 완성 진화 " + d.evo + " 가 없음"); continue; }
            foreach (int part in e.parts)
                if (System.Array.IndexOf(d.cards, part) < 0) Debug.LogWarning("[갈래] " + who + " 완성 진화 " + d.evo + " 의 재료 " + part + " 가 갈래 " + d.key + " 밖에 있음");
        }
        foreach (CrossDef x in h.Crosses)
        {
            if (x.evo == null) continue;
            SkillEvo e = Find(x.evo);
            if (e == null) { Debug.LogWarning("[갈래] " + who + " 교차 진화 " + x.evo + " 가 없음"); continue; }
            foreach (int part in e.parts)
            {
                int at = Paths.PathOfCard(who, part);
                if (at != x.a && at != x.b) Debug.LogWarning("[갈래] " + who + " 교차 진화 " + x.evo + " 의 재료 " + part + " 가 두 갈래 밖에 있음");
            }
        }
    }
#endif

    // 이어하기 (RunSave.Restore): 저장한 판의 규칙 — 옛 저장 · 구판 규칙 판이면 false (끝까지 예전 뽑기 · 진화 재료)
    public void SetPathRules(bool on)
    {
        Paths.SetRules(on);
        myEvos = null;                                  // 진화 재료 6종이 규칙에 따라 다름
        SignatureSkills.Ensure()?.Refresh(this);
    }

    // ================================================================= 카드 단계 상한 · 뽑을 수 있는 카드
    // 이번 판의 최대 단계 (외길이면 갈래 A 카드 +1). 진화(완성)는 원래 최대(MaxLevelOf · Maxed)로 그대로
    public int CapOf(int id)
    {
        CharacterId who = CharacterData.Selected;
        int max = MaxLevelOf(who, id);
        if (max >= 2 && Paths.Active && Paths.Single && Paths.A >= 0 && Paths.PathOfCard(who, id) == Paths.A) max += 1;
        return max;
    }

    // 갈래 규칙에서 이 카드가 레벨업에 나올 수 있나 (구판 규칙: 25 ~ 31 만 막음)
    bool PathAllows(int id)
    {
        if (!Paths.Active) return !Paths.IsPathId(id);
        if (Paths.IsSeed(id) || id == Paths.SingleId) return false;        // 입문 · 외길은 입문 창 · 갈림길 창에서만
        int path = Paths.PathOfCard(CharacterData.Selected, id);
        if (path < 0) return id != Paths.ExtraId;                          // 공용 카드 (31번은 갈래에 들어 있을 때만)
        return path == Paths.A || path == Paths.B;
    }

    // 지금 레벨업에서 고를 수 있는 카드
    bool Learnable(int id) => id >= 0 && id < ability_selected.Length && !ability_selected[id] && KitFits(id) && !SigLocked(id) && PathAllows(id);

    // 판 시작 때 닫아 두는 칸: 입문 · 외길(창에서만 나옴) · 이 영웅에게 없는 31번 새 카드
    static bool PathIdClosed(int id) => id >= Paths.SeedFirstId && (id <= Paths.SingleId || KitCardOf(id) == null);

    // 이 진화를 이번 판에 이룰 수 있나 (갈래 규칙: 재료가 모두 고른 갈래 · 공용이거나 이미 배움) — 진화 띠 · 추천에서 못 이룰 진화를 빼려고
    bool EvoReachable(SkillEvo e)
    {
        if (!Paths.Active) return true;
        foreach (int p in e.parts) if (LevelOf(p) <= 0 && !PathAllows(p)) return false;
        return true;
    }

    // 지금 열 창이 입문 · 갈림길인가 (판 상태에서 매번 정함 · 최대 레벨이면 아님)
    PathWindow PathWindowDue()
    {
        if (!Paths.Active || Paths.Hero == null) return PathWindow.None;
        if (bul == null) bul = FindFirstObjectByType<PlayerController>();
        if (bul != null && bul.level >= LevelCap) return PathWindow.None;
        if (Paths.A < 0) return PathWindow.Seed;
        if (Paths.B < 0 && !Paths.Single && (Paths.Rank(Paths.A) >= Paths.ResI || (bul != null && bul.level + 1 >= Paths.CrossroadsLevel)))
            return PathWindow.Crossroads;
        return PathWindow.None;
    }

    // ================================================================= 세 장 뽑기 (UpdateLvShopContent 가 부름)
    void PathsDraw(bool reroll)
    {
        int[] shown = { first, second, third };
        pathWindow = PathWindowDue();
        if (reroll && pathWindow != PathWindow.None) pathRerolls++;
        ResetGrowthDeck();
        List<int> picks = new List<int>(3);
        if (pathWindow == PathWindow.Seed)
        {
            picks.AddRange(OfferSeeds(3, reroll ? shown : null));
            while (picks.Count < 3) picks.Add(DrawGrowth());
            Hints.Show("paths.seed", "이번 판의 갈래를 고르세요. 입문 카드는 평타 · 움직임을 바꾸고 대가가 붙습니다. 레벨업에는 고른 갈래의 카드만 나옵니다.");
        }
        else if (pathWindow == PathWindow.Crossroads)
        {
            picks.AddRange(OfferSeeds(2, reroll ? shown : null));
            while (picks.Count < 2) picks.Add(DrawGrowth());
            picks.Add(Paths.SingleId);                      // 세 번째 칸은 늘 외길 (금빛)
            Hints.Show("paths.crossroads", "갈림길: 두 번째 갈래를 고르거나 「외길」로 첫 갈래만 더 깊게 팝니다. 두 갈래가 정해지면 다른 갈래 카드는 나오지 않습니다.");
        }
        else DrawPathCards(picks, reroll ? shown : null);
        first = picks[0];
        second = picks[1];
        third = picks[2];
        ShowPathHeader();
    }

    // 갈래 카드 최대 2장 + 공용 최소 1장 (공용이 없으면 갈래 카드로, 둘 다 없으면 성장 카드) · 자리는 섞음
    void DrawPathCards(List<int> picks, int[] shown)
    {
        List<int> hero = new List<int>(), heroSpare = new List<int>(), common = new List<int>(), commonSpare = new List<int>();
        if (!GrowthOnly)
            for (int i = 0; i < Total_abilitys; i++)
            {
                if (!Learnable(i)) continue;
                bool isHero = Paths.PathOfCard(CharacterData.Selected, i) >= 0;
                bool seen = shown != null && System.Array.IndexOf(shown, i) >= 0;      // 다시 뽑기: 방금 본 카드는 다른 카드가 모자랄 때만
                (isHero ? (seen ? heroSpare : hero) : (seen ? commonSpare : common)).Add(i);
            }
        if (hero.Count + heroSpare.Count + common.Count + commonSpare.Count < 3) GrowthHint();
        for (int k = 0; k < 2 && hero.Count + heroSpare.Count > 0; k++) picks.Add(TakeWeighted(hero, heroSpare));
        if (common.Count + commonSpare.Count > 0) picks.Add(TakeWeighted(common, commonSpare));
        while (picks.Count < 3 && hero.Count + heroSpare.Count > 0) picks.Add(TakeWeighted(hero, heroSpare));
        while (picks.Count < 3 && common.Count + commonSpare.Count > 0) picks.Add(TakeWeighted(common, commonSpare));
        while (picks.Count < 3) picks.Add(DrawGrowth());
        for (int i = picks.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (picks[i], picks[j]) = (picks[j], picks[i]);
        }
    }

    int TakeWeighted(List<int> pool, List<int> spare)
    {
        if (pool.Count == 0) { pool.AddRange(spare); spare.Clear(); }
        float total = 0f;
        foreach (int c in pool) total += PathDrawWeight(c);
        float roll = Random.Range(0f, total);
        int k = pool.Count - 1;
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= PathDrawWeight(pool[i]);
            if (roll < 0f) { k = i; break; }
        }
        int id = pool[k];
        pool.RemoveAt(k);
        return id;
    }

    // 다음 공명 · 완성 문턱에 1 ~ 2단계 남은 갈래 카드 ×1.5 (기획서 2-3 · 예전 「가까운 진화 ×1.8 · ×1.4」 대신) · 행운 1마다 +40% 그대로
    float PathDrawWeight(int id)
    {
        float w = 1f;
        int path = Paths.PathOfCard(CharacterData.Selected, id);
        if (path >= 0)
        {
            int left = PathStepsToNext(path);
            if (left >= 1 && left <= 2) w *= PathNearBoost;
        }
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        int luck = sp != null ? sp.Luck : 0;
        if (luck > 0 && (path >= 0 || LevelOf(id) > 0)) w *= 1f + LuckDrawBoost * luck;
        return w;
    }

    const float PathNearBoost = 1.5f;

    // 이 갈래가 다음 문턱(공명 I · 공명 II · 완성 · 외길이면 극의)까지 남은 단계 (0 = 남은 문턱 없음)
    int PathStepsToNext(int path)
    {
        if (!Paths.Has(path)) return 0;
        int r = Paths.Rank(path);
        if (r < Paths.ResI) return Paths.ResI - r;
        if (r < Paths.ResII) return Paths.ResII - r;
        PathDef d = Paths.Def(path);
        if (d == null) return 0;
        int left = 0, leftCap = 0;
        foreach (int c in d.cards)
        {
            left += Mathf.Max(0, MaxLevelOf(CharacterData.Selected, c) - LevelOf(c));
            leftCap += Mathf.Max(0, CapOf(c) - LevelOf(c));
        }
        return left > 0 ? left : leftCap;
    }

    // 추천 반짝임: 고르면 공명 · 완성 문턱에 닿는 카드 = 1 (강하게) · 가까울수록 큼
    float PathProgressAfter(int id)
    {
        if (!Paths.Active) return 0f;
        int path = Paths.PathOfCard(CharacterData.Selected, id);
        if (path < 0 || !Paths.Has(path)) return 0f;
        int left = PathStepsToNext(path);
        if (left <= 0) return 0f;
        return left == 1 ? 1f : 0.5f / left;
    }

    // 입문 카드 n 장: 이미 들어간 갈래는 빼고, 최근 3판에 고른 갈래는 덜 (일일 도전은 그날 시드 · 가중치 없음)
    List<int> OfferSeeds(int n, int[] shown)
    {
        CharacterId who = CharacterData.Selected;
        List<int> fresh = new List<int>(), seen = new List<int>();
        for (int p = 0; p < Paths.Count; p++)
        {
            if (p == Paths.A || p == Paths.B || Paths.DefOf(who, p) == null) continue;
            int id = Paths.SeedFirstId + p;
            (shown != null && System.Array.IndexOf(shown, id) >= 0 ? seen : fresh).Add(id);
        }
        bool daily = DailyChallenge.Active;
        System.Random rng = daily ? new System.Random(unchecked(DailyChallenge.RunSeed * 31 + (int)who * 7919 + (Paths.A < 0 ? 0 : 1009) + pathRerolls * 104729)) : null;
        float W(int id) => daily ? 1f : Paths.RecentWeight(who, id - Paths.SeedFirstId);
        List<int> res = new List<int>(n);
        while (res.Count < n && fresh.Count + seen.Count > 0)
        {
            List<int> from = fresh.Count > 0 ? fresh : seen;
            float total = 0f;
            foreach (int id in from) total += W(id);
            float roll = (float)(rng != null ? rng.NextDouble() : Random.value) * total;
            int k = from.Count - 1;
            for (int i = 0; i < from.Count; i++)
            {
                roll -= W(from[i]);
                if (roll < 0f) { k = i; break; }
            }
            res.Add(from[k]);
            from.RemoveAt(k);
        }
        return res;
    }

    // ================================================================= 고름 (ApplyPick 이 맨 먼저 부름)
    bool PathApply(int id)
    {
        if (!Paths.IsPathId(id)) return false;
        bool replay = RunSave.Replaying;
        CharacterId who = CharacterData.Selected;
        if (Paths.IsSeed(id))
        {
            ability_selected[id] = true;
            int path = id - Paths.SeedFirstId;
            Paths.PickSeed(path, replay);
            if (!replay) SeedMoment(path);
        }
        else if (id == Paths.SingleId)
        {
            ability_selected[id] = true;
            Paths.PickSingle(replay);
            // 갈래 A 카드: 원래 최대에서 닫혔던 카드를 늘어난 단계까지 다시 엶 (HUD 단계 칸도 새 상한으로)
            PathDef d = Paths.Def(Paths.A);
            if (d != null)
                foreach (int c in d.cards)
                {
                    if (c < 0 || c >= ability_selected.Length) continue;
                    if (ability_level[c] < CapOf(c) && KitCardOf(c) != null) ability_selected[c] = false;
                    if (ability_level[c] > 0 && abilityHUD != null) abilityHUD.SetAbility(c, ability_level[c]);
                }
            if (!replay && d != null)
            {
                Paths.Ring(new Color(1f, 0.84f, 0.35f), 1.2f);
                SpecialAbilities.SharedFx?.FloatText(PlayerPos + Vector3.up * 2f, Loc.T("외길") + " · " + Loc.T(d.name), new Color(1f, 0.85f, 0.45f), 6f, 0f);
                Hostile.Play("chime", 0.8f, 1.1f);
            }
        }
        else if (KitCardOf(who, id) != null)                // 31번 새 카드: 단계만 (효과는 영웅 효과 코드가 Paths.Level 로 읽음)
        {
            if (ability_level[id] >= CapOf(id)) ability_selected[id] = true;
        }
        SignatureSkills.Ensure()?.Refresh(this);
        return true;
    }

    Vector3 PlayerPos => bul != null ? bul.transform.position : Hostile.Player != null ? Hostile.Player.transform.position : Vector3.zero;

    // 입문 카드를 확정한 순간: 영웅 둘레 갈래 색 고리 + 짧은 문구
    void SeedMoment(int path)
    {
        PathDef d = Paths.Def(path);
        if (d == null) return;
        Paths.Ring(d.color, 1.2f);
        SpecialAbilities.SharedFx?.FloatText(PlayerPos + Vector3.up * 2f, Loc.T("입문") + " · " + Loc.T(d.name), d.color, 6f, 0f);
        Hostile.Play("chime", 0.8f, 1.25f);
    }

    // 고른 뒤 (ApplyPick · CheckEvolutions 다음): 갈래 단계를 다시 세고 새로 닿은 공명 · 완성 · 극의 · 교차를 알림
    void PathsAfterPick(int what) => Paths.Recompute(this, what, RunSave.Replaying);

    // ================================================================= 카드 글 · 이름 · 아이콘
    void PathSetAbilitys()
    {
        CharacterId who = CharacterData.Selected;
        for (int p = 0; p < Paths.Count; p++)
        {
            int id = Paths.SeedFirstId + p;
            PathDef d = Paths.DefOf(who, p);
            if (d == null || id >= ability_name.Length) continue;
            ability_name[id] = Loc.T(d.seed);
            ability_content[id] = SeedText(who, p, HudView);
        }
        if (Paths.SingleId < ability_name.Length)
        {
            ability_name[Paths.SingleId] = Loc.T("외길");
            ability_content[Paths.SingleId] = SingleText();
        }
        KitCard x = KitCardOf(who, Paths.ExtraId);
        if (x != null && Paths.ExtraId < ability_name.Length)
        {
            ability_name[Paths.ExtraId] = Loc.T(x.name);
            ability_content[Paths.ExtraId] = KitText(Extend(x, Paths.ExtraId), ability_level[Paths.ExtraId]);
        }
    }

    static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    // 입문 카드: 갈래 이름 · 무엇이 바뀌나 · 대가 · 강점 / 약점 (status: HUD 칸 설명이면 지금 갈래 진행까지)
    string SeedText(CharacterId who, int path, bool status)
    {
        PathDef d = Paths.DefOf(who, path);
        if (d == null) return "";
        string s = "<color=" + Hex(d.color) + ">" + Loc.T("갈래") + " · " + Loc.T(d.name) + "</color>"
                 + (string.IsNullOrEmpty(d.tagline) ? "" : "  <size=85%><color=#A89C86>" + Loc.T(d.tagline) + "</color></size>")
                 + "\n" + Loc.T(d.seedDesc)
                 + "\n<color=#FF9C86>" + Loc.T("대가") + "</color>  " + Loc.T(d.cost)
                 + "\n<size=90%><color=#9FE89F>" + Loc.T("강점") + "</color> " + Loc.T(d.strong) + "   <color=#FF9F9F>" + Loc.T("약점") + "</color> " + Loc.T(d.weak) + "</size>";
        if (status && Paths.Has(path)) s += "\n" + PathStatus(path);
        else if (!status && Paths.A >= 0) s += CrossLine(path);
        return s;
    }

    // 갈림길에서: 이 갈래를 고르면 갈래 A 와 생기는 교차
    string CrossLine(int path)
    {
        HeroPaths h = Paths.Hero;
        if (h == null || Paths.A < 0 || path == Paths.A) return "";
        List<string> names = new List<string>();
        foreach (CrossDef x in h.Crosses)
            if ((x.a == Paths.A && x.b == path) || (x.b == Paths.A && x.a == path))
                names.Add(x.evo != null ? EvoNameOf(x.evo) : Loc.T(x.name));
        return names.Count > 0 ? "\n<size=90%><color=#C9A0FF>" + Loc.T("교차") + "</color> " + string.Join(" · ", names) + "</size>" : "";
    }

    // 진화 키 → 이름 (카드 글은 창이 열린 동안 매 프레임 만들므로 목록을 새로 만들지 않고 표에서 바로)
    static string EvoNameOf(string key)
    {
        int w = (int)CharacterData.Selected;
        if (w >= 0 && w < CharEvos.Length) foreach (SkillEvo e in CharEvos[w]) if (e.key == key) return Loc.T(e.name);
        foreach (SkillEvo e in CommonEvos) if (e.key == key) return Loc.T(e.name);
        return key;
    }

    // HUD 입문 칸 설명: 갈래 단계 · 공명 · 갈래 카드 단계 · 완성 · 극의
    string PathStatus(int path)
    {
        PathDef d = Paths.Def(path);
        if (d == null) return "";
        CharacterId who = CharacterData.Selected;
        int r = Paths.Rank(path), t = Paths.Res(path);
        string next = r < Paths.ResI ? "  <color=#A89C86>" + Loc.T("공명 I까지 {0}").Replace("{0}", (Paths.ResI - r).ToString()) + "</color>"
                    : r < Paths.ResII ? "  <color=#A89C86>" + Loc.T("공명 II까지 {0}").Replace("{0}", (Paths.ResII - r).ToString()) + "</color>" : "";
        string s = "\n<color=#F5D478>" + Loc.T("갈래 단계") + " " + r + "</color>" + next;
        s += "\n" + (t >= 1 ? "<color=#9CFF9C>✓</color> " : "<color=#6E665A>·</color> ") + Loc.T("공명 I") + "  <size=88%>" + Loc.T(d.res1) + "</size>";
        s += "\n" + (t >= 2 ? "<color=#9CFF9C>✓</color> " : "<color=#6E665A>·</color> ") + Loc.T("공명 II") + "  <size=88%>" + Loc.T(d.res2) + "</size>";
        List<string> cards = new List<string>();
        foreach (int c in d.cards) cards.Add(CardName(who, c) + " " + LevelOf(c) + "/" + CapOf(c));
        s += "\n<size=88%><color=#C9BCA4>" + Loc.T("갈래 카드") + "</color>  " + string.Join(" · ", cards) + "</size>";
        if (!string.IsNullOrEmpty(d.evo)) s += "\n" + (Paths.Complete(path) ? "<color=#9CFF9C>✓</color> " : "<color=#6E665A>·</color> ") + Loc.T("완성") + "  <color=#C9A0FF>" + EvoNameOf(d.evo) + "</color>";
        if (Paths.Single && path == Paths.A)
            s += "\n" + (Paths.Mastered(path) ? "<color=#9CFF9C>✓</color> " : "<color=#6E665A>·</color> ") + "<color=#FFD34D>" + Loc.T("극의") + "</color>  <size=88%>" + Loc.T(d.mastery) + "</size>";
        HeroPaths h = Paths.Hero;
        if (h != null)
            for (int k = 0; k < h.Crosses.Length; k++)
            {
                CrossDef x = h.Crosses[k];
                if (x.evo != null || (x.a != path && x.b != path) || !Paths.CrossOn(k)) continue;
                s += "\n<color=#9CFF9C>✓</color> " + Loc.T("교차") + "  <color=#C9A0FF>" + Loc.T(x.name) + "</color>  <size=88%>" + Loc.T(x.desc) + "</size>";
            }
        return s;
    }

    // 외길 카드: 갈래 A 만 깊게 · 단계 +1 · 극의
    string SingleText()
    {
        PathDef d = Paths.Def(Paths.A);
        string s = d != null ? Loc.T("「{0}」 하나만 더 깊게 팝니다. 두 번째 갈래는 고르지 않습니다.").Replace("{0}", Loc.T(d.name))
                             : Loc.T("첫 갈래 하나만 더 깊게 팝니다. 두 번째 갈래는 고르지 않습니다.");
        s += "\n<color=#F5D478>" + Loc.T("갈래 카드 최대 단계 +1") + "</color>  ·  " + Loc.T("늘어난 단계까지 모두 채우면 극의");
        if (d != null && !string.IsNullOrEmpty(d.mastery)) s += "\n<color=#FFD34D>" + Loc.T("극의") + "</color>  " + Loc.T(d.mastery);
        return s;
    }

    // 툴팁 (GetAbilityTooltip): 입문 · 외길
    bool PathTooltip(int id, out string tip)
    {
        tip = null;
        if (!Paths.IsSeed(id) && id != Paths.SingleId) return false;
        tip = Paths.IsSeed(id) ? SeedText(CharacterData.Selected, id - Paths.SeedFirstId, HudView) : SingleText();
        return true;
    }

    static string PathCardName(CharacterId who, int id)
    {
        if (Paths.IsSeed(id)) { PathDef d = Paths.DefOf(who, id - Paths.SeedFirstId); return d != null ? Loc.T(d.seed) : ""; }
        if (id == Paths.SingleId) return Loc.T("외길");
        return "";
    }

    static Sprite PathCardIcon(CharacterId who, int id)
    {
        if (Paths.IsSeed(id)) return Paths.SeedIcon(who, id - Paths.SeedFirstId);
        if (id == Paths.SingleId) return Paths.SingleIcon(who);
        return null;
    }

    // 31번 새 카드 (영웅 데이터 HeroPaths.Extra · 없으면 null)
    static KitCard PathExtraCard(CharacterId who)
    {
        HeroPaths h = Paths.Data(who);
        ExtraCardDef x = h != null ? h.Extra : null;
        if (x == null || x.values == null || x.values.Length == 0) return null;
        KitCard c = Card(x.name, x.desc, x.fallbackIcon, x.stat, x.values);
        c.iconPath = h.IconKey + "_card31";
        return c;
    }

    // 외길로 늘어난 단계의 수치: 마지막 두 단계의 증가폭을 한 번 더 (「|+30%|」 · 「공격력 |80%|」 · 「|0.6|초」 · 「|4|번째마다」)
    KitCard Extend(KitCard c, int id)
    {
        if (c == null || c.max < 2) return c;
        int cap = CapOf(id);
        if (cap <= c.max) return c;
        string[] v = new string[cap];
        for (int i = 0; i < c.max; i++) v[i] = c.values[i];
        for (int i = c.max; i < cap; i++) v[i] = NextStep(v[i - 2], v[i - 1]);
        c.values = v;
        return c;
    }

    static string NextStep(string a, string b)
    {
        string[] pa = a.Split('|'), pb = b.Split('|');
        if (pa.Length != 3 || pb.Length != 3) return b;
        if (!ParseStep(pa[1], out float na, out _, out _, out int da) || !ParseStep(pb[1], out float nb, out bool plus, out bool pct, out int db)) return b;
        float n = nb + (nb - na);
        string num = n.ToString("F" + Mathf.Max(da, db), System.Globalization.CultureInfo.InvariantCulture);
        return pb[0] + "|" + (plus && n >= 0f ? "+" : "") + num + (pct ? "%" : "") + "|" + pb[2];
    }

    static bool ParseStep(string s, out float n, out bool plus, out bool pct, out int decimals)
    {
        n = 0f;
        plus = s.StartsWith("+");
        pct = s.EndsWith("%");
        string t = s.Trim('+', '%');
        int dot = t.IndexOf('.');
        decimals = dot >= 0 ? t.Length - dot - 1 : 0;
        return float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out n);
    }

    // ================================================================= 배지 (시너지 배지 자리): 「공명 I까지 N」 · 갈림길 「교차: ○○」
    string PathBadge(int id)
    {
        if (Paths.IsSeed(id))
        {
            string cross = CrossLine(id - Paths.SeedFirstId);
            return cross.Length > 0 ? StripTags(cross) : null;
        }
        int path = Paths.PathOfCard(CharacterData.Selected, id);
        if (path < 0 || !Paths.Has(path)) return null;
        int r = Paths.Rank(path);
        if (r < Paths.ResI) return Loc.T("공명 I까지 {0}").Replace("{0}", (Paths.ResI - r).ToString());
        if (r < Paths.ResII) return Loc.T("공명 II까지 {0}").Replace("{0}", (Paths.ResII - r).ToString());
        return null;
    }

    static string StripTags(string s) => System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "").Trim();

    // ================================================================= 창 머리말 (입문 · 갈림길) · 외길 카드 금빛
    TextMeshProUGUI pathHeader;
    bool PathHeaderShown => pathHeader != null && pathHeader.gameObject.activeSelf;

    void ShowPathHeader()
    {
        bool show = pathWindow != PathWindow.None;
        if (pathHeader == null)
        {
            if (!show || LvshopPanel == null) return;
            UIKit.EnsureStyle();
            // 진화 첫 안내 줄(448)과 같은 자리 — 입문 · 갈림길 창에는 진화 띠가 없어 안내가 뜨지 않음 · 이어 고르기 줄은 위로 비킴 (LevelShop.Chain)
            pathHeader = Label((RectTransform)LvshopPanel.transform, new Vector2(0f, 448f), new Vector2(1500f, 48f), 32f);
            pathHeader.name = "PathHeader";
            pathHeader.enableAutoSizing = true;
            pathHeader.fontSizeMin = 18f;
            pathHeader.fontSizeMax = 32f;
            pathHeader.fontStyle = FontStyles.Bold;
        }
        pathHeader.gameObject.SetActive(show);
        if (!show) return;
        if (pathWindow == PathWindow.Seed)
        {
            pathHeader.color = new Color(1f, 0.86f, 0.4f);
            pathHeader.text = Loc.T("입문") + "  <size=80%><color=#E6DCC4>" + Loc.T("이번 판의 갈래를 고르세요") + "</color></size>";
        }
        else
        {
            PathDef a = Paths.Def(Paths.A);
            pathHeader.color = new Color(0.79f, 0.63f, 1f);
            pathHeader.text = Loc.T("갈림길") + "  <size=80%><color=#E6DCC4>" + Loc.T("두 번째 갈래 또는 외길") + (a != null ? "  ·  " + Loc.T("지금 갈래") + " " + Loc.T(a.name) : "") + "</color></size>";
        }
    }

    void HidePathWindow()
    {
        pathWindow = PathWindow.None;
        if (pathHeader != null) pathHeader.gameObject.SetActive(false);
    }

    // 외길 카드 자리 (갈림길 창의 세 번째 칸 · 고르지 않아도 옅은 금빛)
    bool IsSingleSlot(int slot) => pathWindow == PathWindow.Crossroads && (slot == 0 ? first : slot == 1 ? second : third) == Paths.SingleId;
}
