using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// 발견형 도감 (1.2~, 로드맵 2-2): 진화 · 유물 · 각인 · 비밀 탭은 한 번 얻은(만난) 것만 자세히 보이고, 나머지는 실루엣 + 힌트 한 줄
//   적 · 보스 · 캐릭터 · 무기 · 운명 · 패시브 · 레벨업 · 상점 탭은 예전처럼 모두 보임
//   기록 (Prefs, 이 PC):
//     codex.seen     얻은 항목 열쇠 ('|') — evo.g1.<무기> · evo.g2.<무기> · evo.g2m.<무기> · evo.f.<형태> · evo.a.<강화> · evo.aw.<각성>
//                                          relic.<saveCode> · brand.<saveCode> · sec.hero.<영웅> · sec.dev · sec.mom · sec.jackpot
//     (스킬 진화는 따로 적지 않고 LevelShop 이 쓰는 stats.skillEvos 를 그대로 읽음 = 기존 플레이어 기록이 바로 들어감)
//     codex.viewed   도감에서 이미 본 발견 (NEW 표시를 끄는 데만 씀)
//     codex.tabs     열어 본 탭과 그때의 최신 콘텐츠 버전 ("relic@1.2|brand@1.2")
//     codex.reward   받은 수집 보상 (25 · 50 · 75 · 100)
//     codex.showAll  설정 「도감 모두 보기」(스포일러 허용, 수집률에는 들어가지 않음)
//     codex.migrated 기존 기록(클리어 · 해금 조건)으로 채운 것을 NEW 없이 넣었는지
//   판 안에서 처음 얻으면 알림판(StageManager.ShowBanner)에 짧게 · 수집률이 25 · 50 · 75 · 100% 를 넘으면 한 번 더
//   체험판: 체험판에 없는 항목(2장 이후 · 체험판 밖 영웅 · 각인 · 유물 · 무한 모드)은 「정식판에서 만날 수 있습니다」이고 수집률에서 빠짐
public static class CodexSeen
{
    public enum Cat { Evo = 0, Relic = 1, Brand = 2, Secret = 3 }
    public static readonly string[] TabIds = { "evo", "relic", "brand", "secret" };

    const string SeenKey = "codex.seen", ViewedKey = "codex.viewed", TabsKey = "codex.tabs", RewardKey = "codex.reward",
                 ShowAllKey = "codex.showAll", MigratedKey = "codex.migrated";
    public const string SkillEvoKey = "stats.skillEvos";

    // ================================================================= 수집 보상 (수집률 = 잠기는 탭 전체 기준)
    //   포인트: 판 하나(이야기 모드 클리어 보너스 200 × (1+2+3+4) + 최종 1,500 = 3,500 P) 기준으로 25% ≈ 판 반 개 … 100% ≈ 판 네 개
    //   100%: 전설 이펙트 스킨 「별의 축복」(상점 100,000 P) — 이미 가졌으면 RewardSkinOwnedPoints 를 더 줌
    public static readonly (int pct, int points)[] Rewards = { (25, 2000), (50, 4000), (75, 8000), (100, 15000) };
    public const string RewardSkin = "skin_starlight";
    public const int RewardSkinOwnedPoints = 15000;

    // ================================================================= 항목
    public sealed class Entry
    {
        public string key;
        public Cat cat;
        public string since;            // 처음 들어온 버전 (유물 · 각인), 없으면 null
        public bool inDemo;             // 체험판에서도 얻을 수 있는지
    }

    static List<Entry> entries;
    public static List<Entry> Entries => entries ??= Build();

    static List<Entry> Build()
    {
        List<Entry> l = new List<Entry>();
        void Add(string key, Cat cat, bool demo, string since = null)
        {
            foreach (Entry e in l) if (e.key == key) return;
            l.Add(new Entry { key = key, cat = cat, inDemo = demo, since = since });
        }
        // ---------------- 진화: 거너 1차 · 2차 (체험판은 리치 왕 뒤 1차까지)
        foreach (int w in SpecialAbilities.Tier1Options) Add(GunT1(w), Cat.Evo, true);
        foreach (int w in SpecialAbilities.Tier1Options)
            foreach (int n in SpecialAbilities.Tier2Options(w)) Add(GunT2(w, n), Cat.Evo, false);
        // 다른 영웅: 1차 형태 셋 · 2차 강화 셋 (체험판은 검사 1차까지)
        for (int i = 1; i < CharacterData.All.Length; i++)
        {
            CharacterId c = (CharacterId)i;
            if (!CharacterData.IsDeveloped(c)) continue;
            bool demo = Demo.CharacterAllowed(c) && c != CharacterId.Gunner;
            foreach (int id in CharacterData.Def(c).pool)
                if (SpecialAbilities.IsKit(id) && SpecialAbilities.KitIsWeapon(id)) Add("evo.f." + id, Cat.Evo, demo);
            foreach (int a in CharacterKit.AugmentsFor(c)) Add("evo.a." + a, Cat.Evo, false);
        }
        foreach (int id in SpecialAbilities.AwakenOptions) Add("evo.aw." + id, Cat.Evo, false);
        // 스킬 진화: 영웅마다 전용 + 모든 영웅 공통(한 번만)
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterId c = (CharacterId)i;
            if (!CharacterData.IsDeveloped(c)) continue;
            foreach (LevelShop.SkillEvo e in LevelShop.EvosFor(c))
                Add(SkillEvoEntry(c, e.key), Cat.Evo, Demo.CharacterAllowed(c));
        }
        // ---------------- 유물 · 각인 (체험판에는 석관 · 각인이 없음)
        foreach (RelicDef r in ContentRegistry.Relics) Add("relic." + r.saveCode, Cat.Relic, false, r.since);
        foreach (SoulBrandDef b in ContentRegistry.SoulBrands) Add("brand." + b.saveCode, Cat.Brand, false, b.since);
        // ---------------- 비밀: 조건으로 만나는 영웅 · 무한 모드 최종 보스 둘 · 전설 상자 대박
        foreach (CharacterId c in SecretHeroes()) Add("sec.hero." + (int)c, Cat.Secret, false);
        Add("sec.dev", Cat.Secret, false);
        Add("sec.mom", Cat.Secret, false);
        Add("sec.jackpot", Cat.Secret, true);
        return l;
    }

    public static IEnumerable<CharacterId> SecretHeroes()
    {
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterId c = (CharacterId)i;
            if (CharacterData.IsDeveloped(c) && CharacterData.Def(c).condition != CharUnlock.None) yield return c;
        }
    }

    public static string GunT1(int w) => "evo.g1." + w;
    public static string GunT2(int tier1, int n) => n == tier1 ? "evo.g2m." + n : "evo.g2." + n;
    public static string SkillEvoEntry(CharacterId c, string key) => key.StartsWith("ce.") ? "se." + key : "se." + c + ":" + key;

    public static bool Available(Entry e) => !Demo.On || e.inDemo;
    public static bool Available(string key)
    {
        foreach (Entry e in Entries) if (e.key == key) return Available(e);
        return false;
    }

    // ================================================================= 저장
    static HashSet<string> seen, viewed;
    static Dictionary<string, string> tabs;
    static string skillRaw;
    static HashSet<string> skillSet = new HashSet<string>();

    static HashSet<string> Split(string s) => new HashSet<string>((s ?? "").Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries));

    static void Load()
    {
        seen ??= Split(Prefs.GetString(SeenKey, ""));
        viewed ??= Split(Prefs.GetString(ViewedKey, ""));
        if (tabs == null)
        {
            tabs = new Dictionary<string, string>();
            foreach (string t in Split(Prefs.GetString(TabsKey, "")))
            {
                int at = t.IndexOf('@');
                if (at > 0) tabs[t.Substring(0, at)] = t.Substring(at + 1);
            }
        }
        string raw = Prefs.GetString(SkillEvoKey, "");
        if (raw != skillRaw) { skillRaw = raw; skillSet = Split(raw); }
    }

    static void SaveSets()
    {
        Prefs.SetString(SeenKey, string.Join("|", seen));
        Prefs.SetString(ViewedKey, string.Join("|", viewed));
        List<string> t = new List<string>();
        foreach (KeyValuePair<string, string> kv in tabs) t.Add(kv.Key + "@" + kv.Value);
        Prefs.SetString(TabsKey, string.Join("|", t));
        Prefs.Save();
    }

    public static bool ShowAll
    {
        get => Prefs.GetInt(ShowAllKey, 0) == 1;
        set { Prefs.SetInt(ShowAllKey, value ? 1 : 0); Prefs.Save(); }
    }

    // 정말 얻었는지 (수집률 · 보상)
    public static bool Found(string key)
    {
        Load();
        if (key.StartsWith("se."))
        {
            string k = key.Substring(3);
            if (k.StartsWith("ce."))
            {
                foreach (string s in skillSet) if (s.EndsWith(":" + k)) return true;      // 공통 진화는 누가 만들었든
                return false;
            }
            return skillSet.Contains(k);
        }
        return seen.Contains(key) || Live(key);
    }

    // 도감에 자세히 보여 줄지 (설정 「모두 보기」면 모두)
    public static bool Shown(string key) => ShowAll || Found(key);

    // 이미 있는 기록으로 알 수 있는 것 (기존 플레이어도 그대로 열림)
    static bool Live(string key)
    {
        if (key.StartsWith("sec.hero."))
        {
            if (!int.TryParse(key.Substring(9), out int c) || c < 0 || c >= CharacterData.All.Length) return false;
            return CharacterData.ConditionMet((CharacterId)c);       // 해금 조건을 이룬 순간 만난 것 (사기 전이라도)
        }
        if (key == "sec.dev" || key == "sec.mom")
            return EndlessMode.EverCleared || CharacterData.ConditionMet(CharUnlock.EndlessClear);       // 무한 모드 클리어 = 둘 다 쓰러뜨림
        return false;
    }

    // ================================================================= 수집률
    public static void Count(out int found, out int total) => Count(null, out found, out total);

    public static void Count(Cat? cat, out int found, out int total)
    {
        found = total = 0;
        foreach (Entry e in Entries)
        {
            if (cat.HasValue && e.cat != cat.Value) continue;
            if (!Available(e)) continue;
            total++;
            if (Found(e.key)) found++;
        }
    }

    public static int Percent()
    {
        Count(out int f, out int t);
        return Pct(f, t);
    }

    static int Pct(int f, int t) => t <= 0 ? 0 : f >= t ? 100 : Mathf.Min(99, f * 100 / t);

    // ================================================================= 판 안에서 얻었을 때
    static bool Off => GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode;

    // 얻은 것을 적고, 처음이면 알림 (name = 번역된 이름)
    public static void Mark(string key, string name)
    {
        if (string.IsNullOrEmpty(key) || Off) return;
        Load();
        if (Found(key)) return;
        seen.Add(key);
        SaveSets();
        Announce(key, name);
    }

    // 스킬 진화: LevelShop 이 stats.skillEvos 에 막 적었을 때 (처음 만든 진화일 때만 부름)
    public static void OnSkillEvo(CharacterId who, LevelShop.SkillEvo e)
    {
        if (e == null || Off) return;
        string key = SkillEvoEntry(who, e.key);
        // 공통 진화를 다른 영웅으로 또 만든 것은 도감에는 새것이 아님
        if (e.key.StartsWith("ce."))
        {
            int count = 0;
            foreach (string s in Split(Prefs.GetString(SkillEvoKey, ""))) if (s.EndsWith(":" + e.key)) count++;
            if (count > 1) return;
        }
        Announce(key, Loc.T(e.name));
    }

    // 무기 진화 · 각성 (StageManager.Evolution: EvolveWeapon 바로 뒤)
    public static void OnWeaponEvolved(SpecialAbilities sa, int id)
    {
        if (sa == null) return;
        string key;
        if (SpecialAbilities.IsAwaken(id)) key = "evo.aw." + id;
        else if (id >= CharacterKit.AugFirst) key = "evo.a." + id;
        else if (!CharacterData.IsGunner) key = "evo.f." + id;
        else if (sa.EvolutionTier <= 1) key = GunT1(id);
        else key = GunT2(sa.GunEvo1, id);
        Mark(key, sa.EvolutionName(id, Mathf.Clamp(sa.EvolutionTier, 1, 2)));
    }

    static void Announce(string key, string name)
    {
        if (!Available(key)) return;
        Count(out int f, out int t);
        int after = Pct(f, t), before = Pct(f - 1, t);
        StageManager sm = StageManager.Instance;
        if (sm == null) return;
        sm.ShowBanner(Loc.T("codex.toast").Replace("{0}", name) + "\n<size=70%>" + Loc.T("codex.toast.rate").Replace("{0}", after.ToString()) + "</size>", 2.2f);
        foreach (var r in Rewards)
            if (before < r.pct && after >= r.pct)
            {
                sm.ShowBanner(Loc.T("codex.milestone").Replace("{0}", r.pct.ToString()) + "\n<size=70%>" + Loc.T("codex.milestone.sub") + "</size>", 3.5f, true);
                Hostile.Play("chime", 0.8f, 1.2f);
            }
    }

    // ================================================================= NEW 표시
    // 업데이트로 새로 들어온 콘텐츠 버전 = ContentRegistry 의 since 가운데 가장 높은 것 (지금 "1.2")
    static string latest;
    public static string Latest
    {
        get
        {
            if (latest != null) return latest;
            string best = "0";
            void Take(string v) { if (!string.IsNullOrEmpty(v) && CompareVersion(v, best) > 0) best = v; }
            foreach (EventDef e in ContentRegistry.Events) Take(e.since);
            foreach (RewardDef e in ContentRegistry.Rewards) Take(e.since);
            foreach (CurseDef e in ContentRegistry.Curses) Take(e.since);
            foreach (RelicDef e in ContentRegistry.Relics) Take(e.since);
            foreach (PropDef e in ContentRegistry.Props) Take(e.since);
            foreach (DropDef e in ContentRegistry.Drops) Take(e.since);
            foreach (SoulBrandDef e in ContentRegistry.SoulBrands) Take(e.since);
            return latest = best;
        }
    }

    public static int CompareVersion(string a, string b)
    {
        string[] x = (a ?? "0").Split('.'), y = (b ?? "0").Split('.');
        for (int i = 0; i < Mathf.Max(x.Length, y.Length); i++)
        {
            int p = i < x.Length && int.TryParse(x[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int px) ? px : 0;
            int q = i < y.Length && int.TryParse(y[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int qy) ? qy : 0;
            if (p != q) return p.CompareTo(q);
        }
        return 0;
    }

    public static bool IsLatest(string since) => !string.IsNullOrEmpty(since) && CompareVersion(since, Latest) == 0;

    // 이 탭을 이번 최신 콘텐츠 버전에서 열어 본 적이 있는지
    public static bool TabOpenedThisVersion(Cat cat)
    {
        Load();
        return tabs.TryGetValue(TabIds[(int)cat], out string v) && CompareVersion(v, Latest) >= 0;
    }

    // 발견했는데 아직 도감에서 못 본 것
    public static bool Unviewed(string key)
    {
        Load();
        return !key.StartsWith("se.") && seen.Contains(key) && !viewed.Contains(key);
    }

    // 탭 NEW: 한 번도 안 열어 봤거나 · 이번 업데이트 항목이 있는데 이번 버전에서 안 열어 봤거나 · 못 본 발견이 있음
    public static bool TabNew(Cat cat)
    {
        Load();
        if (!tabs.ContainsKey(TabIds[(int)cat])) return true;
        bool opened = TabOpenedThisVersion(cat);
        foreach (Entry e in Entries)
        {
            if (e.cat != cat || !Available(e)) continue;
            if (!opened && IsLatest(e.since)) return true;
            if (Unviewed(e.key)) return true;
        }
        return false;
    }

    // 탭을 열었을 때 (그린 뒤에): 이번 버전에서 열어 봤다고 · 이 탭의 발견을 모두 봤다고
    public static void TabViewed(Cat cat)
    {
        Load();
        tabs[TabIds[(int)cat]] = Latest;
        foreach (Entry e in Entries) if (e.cat == cat && seen.Contains(e.key)) viewed.Add(e.key);
        SaveSets();
    }

    // 메인 메뉴 도감 버튼 반짝임: NEW 탭이 있거나 받을 보상이 있음
    public static bool HasNew
    {
        get
        {
            Migrate();
            if (CanClaimAny) return true;
            for (int i = 0; i < TabIds.Length; i++) if (TabNew((Cat)i)) return true;
            return false;
        }
    }

    // 기존 기록(해금 조건 · 무한 모드 클리어)으로 아는 것을 codex.seen 에 옮김 — 처음 한 번은 NEW 없이
    //   그 뒤로는 메뉴에서 새로 알게 된 것(판 밖에서 이룬 조건)도 NEW 로 보임
    public static void Migrate()
    {
        Load();
        bool first = Prefs.GetInt(MigratedKey, 0) == 0;
        bool changed = first;
        foreach (Entry e in Entries)
        {
            if (e.key.StartsWith("se.") || seen.Contains(e.key) || !Live(e.key)) continue;
            seen.Add(e.key);
            if (first) viewed.Add(e.key);
            changed = true;
        }
        if (!changed) return;
        Prefs.SetInt(MigratedKey, 1);
        SaveSets();
    }

    // ================================================================= 보상
    static HashSet<string> Claimed() => Split(Prefs.GetString(RewardKey, ""));
    public static bool IsClaimed(int pct) => Claimed().Contains(pct.ToString(CultureInfo.InvariantCulture));
    public static bool CanClaim(int pct) => !IsClaimed(pct) && Percent() >= pct;

    public static bool CanClaimAny
    {
        get
        {
            int p = Percent();
            HashSet<string> c = Claimed();
            foreach (var r in Rewards) if (p >= r.pct && !c.Contains(r.pct.ToString(CultureInfo.InvariantCulture))) return true;
            return false;
        }
    }

    // 받으면 보상 설명 (번역됨), 못 받으면 null
    public static string Claim(int pct)
    {
        if (!CanClaim(pct)) return null;
        int points = 0;
        foreach (var r in Rewards) if (r.pct == pct) points = r.points;
        string skinText = null;
        if (pct >= 100)
        {
            SkinDef s = SkinData.Get(RewardSkin);
            if (s != null && !SkinData.Owns(s.id))
            {
                Prefs.SetInt("skin.own." + s.id, 1);        // SkinData.Owns 와 같은 열쇠 (상점에서 바로 장착할 수 있음)
                skinText = Loc.T(s.name);
            }
            else points += RewardSkinOwnedPoints;
        }
        HashSet<string> c = Claimed();
        c.Add(pct.ToString(CultureInfo.InvariantCulture));
        Prefs.SetString(RewardKey, string.Join("|", c));
        CharacterData.AddPoints(points);
        Prefs.Save();
        string text = points.ToString("N0") + " P";
        if (skinText != null) text += " + " + Loc.T("codex.reward.skin").Replace("{0}", skinText);
        return text;
    }
}
