using System.Collections.Generic;
using UnityEngine;

// 빌드 갈래 (2026-10-19 · docs/review/2026-10-19-빌드-시너지-기획.md 0 · 1단계): 모든 영웅에 공통인 틀
//   영웅마다 갈래 5개. 판 시작 카드 = 입문 카드 3장(5개 중) → 갈래 A, 갈림길 창(남은 갈래 2장 + 외길) → 갈래 B 또는 외길
//   두 갈래가 정해지면 다른 갈래 카드는 레벨업에 나오지 않음 (플레이어가 지우는 버튼이 아니라 선택의 대가, 기획서 2-7)
//   갈래 단계(입문 1 + 갈래 카드 단계 합) 4 → 공명 I · 8 → 공명 II · 갈래 카드가 모두 최대 → 완성(기존 스킬 진화) · 외길이면 극의
// 이 파일: 갈래 데이터 모양(PathDef · CrossDef · ExtraCardDef) · 영웅별 효과 틀(HeroPaths) · 판 상태와 묻기 · 효과 도우미(Paths)
// 영웅별 데이터 · 효과는 Paths/Paths.<영웅>.cs, 문구는 Paths/LocTable.Paths.<영웅>.cs, 카드 창 규칙은 LevelShop.Paths
// 영웅 효과 구현 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md

// 갈래 하나
public class PathDef
{
    public string key;              // 저장 · 판 기록 · 최근 고른 갈래(Prefs) 열쇠 — 한 번 정하면 바꾸지 않음 (예 "g.snipe")
    public string name;             // 갈래 이름 (한국어 키 · 번역은 LocTable.Paths.<영웅>)
    public string tagline;          // 한 줄 성격 (입문 카드 · HUD 설명)
    public string seed;             // 입문 카드 이름
    public string seedDesc;         // 입문 카드: 무엇이 바뀌나 (완결 문장)
    public string cost;             // 대가 (완결 문장)
    public string strong, weak;     // 강점 · 약점 (짧게, 「보스 · 일렬」처럼)
    public int[] cards;             // 갈래 카드 (레벨업 카드 번호: 기존 전용 카드 · 고유 스킬 15 ~ 24 · 새 카드 31)
    public string evo;              // 완성 = 기존 스킬 진화 키 (LevelShop.CharEvos, 재료가 모두 이 갈래 카드)
    public string res1, res2;       // 공명 I (단계 4) · 공명 II (단계 8) 설명
    public string mastery;          // 극의 (외길 + 늘어난 단계까지 모두) 설명
    public float power = 1f;        // 갈래 효과 피해 배율 — 밸런스 손잡이 하나 (효과 코드가 Paths.Power(갈래) 를 곱함)
    public Color color = Color.white;   // 갈래 색 (입문 고리 · 알림 띠 · HUD)
    public string form;             // 형태 공명 짝 (보스 무기 진화 1차 · 2차 형태 이름, 4단계 연결용 · 지금은 표시만 안 함)
}

// 두 갈래를 섞으면 열리는 교차 (영웅마다 4종: 기존 진화 2 + 새 효과 2)
public class CrossDef
{
    public int a, b;                // 두 갈래 번호 (0 ~ 4)
    public string evo;              // 기존 진화로 생기는 교차 (재료가 두 갈래에 걸침) — 효과는 진화 그대로. null 이면 새 효과
    public string key, name, desc;  // 새 효과 (evo 가 null): 두 갈래 모두 공명 I 이면 켜짐 (HeroPaths.OnCross)
}

// 영웅이 새로 얻는 레벨업 카드 (31번 칸 · 지금은 연금술사 「서리 장막」 하나)
public class ExtraCardDef
{
    public string name, desc, stat;     // 이름 · 한 문장 설명 · 단계마다 바뀌는 수치 이름 (한국어 키)
    public string[] values;             // 단계별 수치 ("앞말|숫자|단위", LevelShop.KitCard 와 같은 모양)
    public int fallbackIcon;            // 그림이 아직 없을 때 쓸 기존 아이콘 (Resources/Icons/ability_<번호>)
}

// ================================================================= 영웅별 갈래 (Paths.<영웅>.cs 가 물려받음)
// 데이터(BuildPaths · BuildCrosses · Extra)와 효과 훅. 훅은 모두 비어 있어도 게임은 정상 진행 (입문 카드를 고르면 그 갈래 카드만 나옴까지)
// 판마다 새 인스턴스 (Paths.BeginRun) → 필드에 둔 상태는 판이 바뀌면 처음부터. 상태는 「끝나는 시각」(until) 값으로 (안내서 참고)
public abstract class HeroPaths
{
    public abstract CharacterId Who { get; }
    // 아이콘 파일 이름 앞말 (Resources/Icons/Paths/<IconKey>_<갈래 번호>.png …, 안내서 「아이콘」)
    public abstract string IconKey { get; }

    PathDef[] defs;
    CrossDef[] crosses;
    public PathDef[] Defs => defs ??= BuildPaths() ?? new PathDef[0];
    public CrossDef[] Crosses => crosses ??= BuildCrosses() ?? new CrossDef[0];
    protected abstract PathDef[] BuildPaths();
    protected abstract CrossDef[] BuildCrosses();
    // 31번 새 카드 (없으면 null)
    public virtual ExtraCardDef Extra => null;

    // ---------------- 데이터를 쓸 때 쓰는 카드 번호 (LevelShop 의 카드 칸과 같음)
    //   P 0 (관통 자리) · G 3 (노려보는 눈빛 자리) · M 6 (멀티 샷 자리) · K 7 (밀어내기 자리) · H 11 (도적 사냥의 기세) · A 12 · B 13 · S(k) = 고유 스킬 15 + k · X 31 새 카드
    protected const int P = 0, G = 3, M = 6, K = 7, H = 11, A = 12, B = 13, X = Paths.ExtraId;
    protected static int S(int k) => LevelShop.SigFirstId + k;
    protected static CrossDef Cross(int a, int b, string evo) => new CrossDef { a = a, b = b, evo = evo };
    protected static CrossDef Cross(int a, int b, string key, string name, string desc) => new CrossDef { a = a, b = b, key = key, name = name, desc = desc };

    // ================================================================= 판 흐름 훅 (LevelShop 이 부름)
    // replay: 이어하기로 기록을 다시 적용하는 중 (연출 · 소리 · 알림 금지, 상태만 맞춤) — 같은 판에서 같은 일로 두 번 불리지 않음
    public virtual void OnSeed(int path, bool replay) { }                 // 입문 카드를 고름 (갈래 A 또는 B 가 생김)
    public virtual void OnSingle(int path, bool replay) { }               // 외길을 고름 (path = 갈래 A)
    public virtual void OnResonance(int path, int tier, bool replay) { }  // 공명 I (tier 1) · II (tier 2)에 닿음
    public virtual void OnComplete(int path, bool replay) { }             // 갈래 카드가 모두 원래 최대 (완성 · 진화는 LevelShop 이 따로 일으킴)
    public virtual void OnMastery(int path, bool replay) { }              // 극의 (외길 + 늘어난 단계까지 모두)
    public virtual void OnCross(int cross, bool replay) { }               // 새 교차 효과가 켜짐 (Crosses[cross], evo == null 인 것만)
    public virtual void OnPick(int id, bool replay) { }                   // 레벨업 카드를 하나 고를 때마다 (위 일들이 끝난 뒤 · 성장 카드 · 비상 보급은 빼고)
    public virtual void End() { }                                         // 판이 끝남 (플레이어가 사라질 때 · 바꿔 둔 플레이어 값을 되돌림)

    // ================================================================= 전투 훅 (SignatureSkills · CharacterKit 이 부름, 이번 판이 갈래 규칙일 때만)
    public virtual float Outgoing(GameObject target, float dmg) => 1f;     // 주는 피해 배율 (곱함)
    public virtual void Hit(GameObject target, float dmg, bool killed, bool proc) { }    // 명중 (proc: 평타 · 스킬 명중 — 이 효과가 준 피해 · 지속 피해가 아님)
    public virtual float Taken(float dmg) => dmg;                         // 받는 피해 (바꾼 값을 돌려줌 · 이미 무적 시간은 시작됨)
    public virtual void Hurt(float taken) { }                             // 실제로 피해를 받은 뒤
    public virtual void Attacked(Vector3 start, Vector2 dir) { }          // 평타를 쏨 (거너 사격 · 도적 표창처럼 SignatureSkills.Attacked 를 부르는 곳만)
    public virtual void Killed(Vector3 pos) { }                           // 적이 쓰러짐 (모든 처치)
    public virtual void Ult() { }                                         // 필살기를 씀 (PlayerController.UltUsed)
    public virtual float MoveMul() => 1f;                                 // 이동 속도 배율
    public virtual float RateMul() => 1f;                                 // 공격 속도 배율 (거너가 아닌 영웅 — CharacterKit.AttackSpeedMul · 거너는 PlayerController 사격 간격을 직접)
    public virtual void Tick() { }                                        // 매 프레임 (멈춘 동안 · 쓰러지는 중은 안 불림)
    // 거너가 아닌 영웅의 평타 (CharacterKit.Attack): true 를 돌려주면 원래 평타를 쏘지 않음 (평타 모양을 바꾸는 입문)
    public virtual bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots) => false;
    // HUD 입문 칸 오른쪽 위 숫자 (열 게이지 · 중첩 등, 없으면 null)
    public virtual string Badge(int path) => null;
}

// ================================================================= 판 상태 · 묻기 · 도우미
public static class Paths
{
    // 전체 스위치: false 면 모든 판이 예전 규칙 (영웅별로는 HeroPaths 가 없거나 갈래가 5개가 아니면 꺼짐 — Slot10)
    public static bool RulesOn = true;

    // 카드 번호 (기존 0 ~ 24 · 성장 100 ~ 103 은 그대로): 입문 25 ~ 29 (갈래 번호 + 25) · 외길 30 · 새 카드 31
    public const int SeedFirstId = 25, SingleId = 30, ExtraId = 31, IdEnd = 32, Count = 5;
    public const int ResI = 4, ResII = 8;          // 공명 문턱 (갈래 단계)
    public const int CrossroadsLevel = 8;          // 갈래 A 가 공명 I 에 닿기 전이라도 이 레벨에 오르는 창이 갈림길
    public const float InvulnCap = 1.5f;           // 갈래 효과가 한 번에 더하는 무적 상한 (기획서 6-3)

    public static bool IsSeed(int id) => id >= SeedFirstId && id < SeedFirstId + Count;
    public static bool IsPathId(int id) => id >= SeedFirstId && id < IdEnd;

    // ---------------- 영웅별 데이터 (게임 밖 도감 · 카드 이름에도 씀 · 훅은 부르지 않음)
    // 영웅 번호로 바로 (카드 뽑기 · HUD 가 자주 물어서 사전 대신 배열)
    static readonly HeroPaths[] catalog = new HeroPaths[System.Enum.GetValues(typeof(CharacterId)).Length];
    static readonly bool[] made = new bool[catalog.Length];

    public static HeroPaths Create(CharacterId who) => who switch
    {
        CharacterId.Gunner => new GunnerPaths(),
        CharacterId.Swordsman => new SwordsmanPaths(),
        CharacterId.Rogue => new RoguePaths(),
        CharacterId.Archer => new ArcherPaths(),
        CharacterId.Alchemist => new AlchemistPaths(),
        CharacterId.Cartographer => new CartographerPaths(),
        CharacterId.Clockmaker => new ClockmakerPaths(),
        CharacterId.Postman => new PostmanPaths(),
        CharacterId.Ghost => new GhostPaths(),
        _ => null,
    };

    public static HeroPaths Data(CharacterId who)
    {
        int i = (int)who;
        if (i < 0 || i >= catalog.Length) return null;
        if (!made[i]) { catalog[i] = Create(who); made[i] = true; }
        return catalog[i];
    }

    // 갈래 5개가 모두 카드를 갖고 있어야 켬 (입문 카드가 없는 영웅은 예전 규칙 — 진행 막힘 방지)
    public static bool Supported(CharacterId who)
    {
        HeroPaths h = Data(who);
        if (h == null || h.Defs.Length != Count) return false;
        foreach (PathDef d in h.Defs) if (d == null || d.cards == null || d.cards.Length == 0) return false;
        return true;
    }

    public static bool RulesFor(CharacterId who) => RulesOn && Supported(who);

    public static PathDef DefOf(CharacterId who, int path)
    {
        HeroPaths h = Data(who);
        return h != null && path >= 0 && path < h.Defs.Length ? h.Defs[path] : null;
    }

    // 이 카드가 들어 있는 갈래 (-1 = 공용 · 갈래 밖)
    public static int PathOfCard(CharacterId who, int id)
    {
        HeroPaths h = Data(who);
        if (h == null) return -1;
        for (int i = 0; i < h.Defs.Length; i++)
            if (h.Defs[i].cards != null && System.Array.IndexOf(h.Defs[i].cards, id) >= 0) return i;
        return -1;
    }

    // ================================================================= 이번 판 상태
    public static bool Active { get; private set; }         // 이번 판이 갈래 규칙 (구판 저장을 이어하면 false · 끝까지 예전 규칙)
    public static HeroPaths Hero { get; private set; }      // 이번 판 효과 (판마다 새로)
    public static int A { get; private set; } = -1;         // 첫 갈래 (-1 = 아직)
    public static int B { get; private set; } = -1;         // 두 번째 갈래 (-1 = 아직 · 외길이면 계속 -1)
    public static bool Single { get; private set; }         // 외길 (갈래 A 만 깊게)
    static readonly int[] rank = new int[Count];
    static readonly int[] tier = new int[Count];            // 0 · 1 공명 I · 2 공명 II
    static readonly bool[] complete = new bool[Count], mastered = new bool[Count];
    static readonly List<bool> crossOn = new List<bool>();
    static LevelShop shop;

    // 전투 훅을 부를지 (규칙이 켜져 있고 효과가 있음)
    static bool Live => Active && Hero != null;

    // LevelShop.Start: 새 판 (지난 판 상태를 모두 버림)
    internal static void BeginRun(LevelShop s, CharacterId who)
    {
        shop = s;
        Hero = Create(who);
        Active = RulesFor(who);
        A = B = -1;
        Single = false;
        System.Array.Clear(rank, 0, Count);
        System.Array.Clear(tier, 0, Count);
        System.Array.Clear(complete, 0, Count);
        System.Array.Clear(mastered, 0, Count);
        crossOn.Clear();
        if (Hero != null) for (int i = 0; i < Hero.Crosses.Length; i++) crossOn.Add(false);
        tierLog.Clear();
        crossLog.Clear();
        cardSeq.Clear();
    }

    // 이어하기: 저장한 판의 규칙으로 (구판 저장은 false)
    internal static void SetRules(bool on) => Active = on && Hero != null && RulesFor(Hero.Who);

    // ---------------- 묻기 (영웅 효과 코드가 씀)
    public static PathDef Def(int path) => Hero != null && path >= 0 && path < Hero.Defs.Length ? Hero.Defs[path] : null;
    public static bool Has(int path) => Active && path >= 0 && (path == A || path == B);     // 이 갈래에 들어왔나 (입문 효과가 켜져 있나)
    public static int Rank(int path) => path >= 0 && path < Count ? rank[path] : 0;           // 갈래 단계 (입문 1 + 갈래 카드 단계 합)
    public static int Res(int path) => Active && path >= 0 && path < Count ? tier[path] : 0;  // 0 · 1 공명 I · 2 공명 II
    public static bool Complete(int path) => Active && path >= 0 && path < Count && complete[path];
    public static bool Mastered(int path) => Active && path >= 0 && path < Count && mastered[path];
    public static bool CrossOn(int cross) => Active && cross >= 0 && cross < crossOn.Count && crossOn[cross];
    public static float Power(int path) => Def(path) != null ? Def(path).power : 1f;
    public static int Level(int id) => shop != null ? shop.LevelOf(id) : 0;                   // 카드 단계 (31번 새 카드 등)
    public static int OtherOf(int path) => path == A ? B : path == B ? A : -1;

    // ================================================================= 카드를 고름 (LevelShop.Paths 가 부름)
    internal static void PickSeed(int path, bool replay)
    {
        if (!Active || path < 0 || path >= Count || path == A || path == B) return;
        if (A < 0) A = path;
        else if (B < 0 && !Single) B = path;
        else return;
        if (!replay) Remember(path);
        Safe(() => Hero?.OnSeed(path, replay));
    }

    internal static void PickSingle(bool replay)
    {
        if (!Active || A < 0 || B >= 0 || Single) return;
        Single = true;
        Safe(() => Hero?.OnSingle(A, replay));
    }

    // 고를 때마다 · 이어하기로 다시 적용할 때마다 단계를 다시 세고 새로 닿은 것을 알림 (매 프레임 세지 않음)
    internal static void Recompute(LevelShop s, int picked, bool replay)
    {
        if (s != null) shop = s;
        if (Active && Hero != null && shop != null)
        {
            CharacterId who = Hero.Who;
            for (int p = 0; p < Count && p < Hero.Defs.Length; p++)
            {
                bool inPath = p == A || p == B;
                int r = 0;
                bool all = inPath, allCap = inPath;
                if (inPath)
                {
                    r = 1;
                    foreach (int id in Hero.Defs[p].cards)
                    {
                        int cap = shop.CapOf(id), lv = shop.LevelOf(id);
                        r += Mathf.Min(lv, cap);
                        if (lv < LevelShop.MaxLevelOf(who, id)) all = false;
                        if (lv < cap) allCap = false;
                    }
                }
                rank[p] = r;
                int t = r >= ResII ? 2 : r >= ResI ? 1 : 0;
                for (int k = tier[p] + 1; k <= t; k++)
                {
                    tier[p] = k;
                    int path = p, tk = k;
                    Log("p" + (p + 1) + "r" + k);
                    if (!replay) Announce(p, k == 1 ? "공명 I" : "공명 II", k == 1 ? Hero.Defs[p].res1 : Hero.Defs[p].res2, false);
                    Safe(() => Hero.OnResonance(path, tk, replay));
                }
                if (all && !complete[p])
                {
                    complete[p] = true;
                    int path = p;
                    Log("p" + (p + 1) + "c");
                    Safe(() => Hero.OnComplete(path, replay));
                }
                if (Single && p == A && allCap && !mastered[p])
                {
                    mastered[p] = true;
                    int path = p;
                    Log("p" + (p + 1) + "m");
                    if (!replay) Announce(p, "극의", Hero.Defs[p].mastery, true);
                    Safe(() => Hero.OnMastery(path, replay));
                }
            }
            for (int c = 0; c < Hero.Crosses.Length && c < crossOn.Count; c++)
            {
                CrossDef x = Hero.Crosses[c];
                if (crossOn[c] || x.evo != null || !Has(x.a) || !Has(x.b) || tier[x.a] < 1 || tier[x.b] < 1) continue;
                crossOn[c] = true;
                int cross = c;
                crossLog.Add(x.key);
                Log("x" + (c + 1));
                if (!replay) AnnounceCross(x);
                Safe(() => Hero.OnCross(cross, replay));
            }
        }
        if (Hero != null && Active) Safe(() => Hero.OnPick(picked, replay));
    }

    // ---------------- 알림 (화면 위 띠 · 영웅 둘레 갈래 색 고리 · 소리)
    static void Announce(int path, string what, string desc, bool gold)
    {
        PathDef d = Def(path);
        if (d == null) return;
        Color c = gold ? new Color(1f, 0.84f, 0.35f) : d.color;
        Notice.Big(Loc.T(what) + "  ·  " + Loc.T(d.name), string.IsNullOrEmpty(desc) ? "" : Loc.T(desc), NoticeIcon.Star, 3.2f, NoticeLevel.Normal, c, TierIcon(path, gold ? "m" : what == "공명 I" ? "r1" : "r2"));
        Ring(c, gold ? 1.4f : 1f);
        Hostile.Play(gold ? "evolve" : "chime", 0.75f, gold ? 1.2f : 1.35f);
    }

    static void AnnounceCross(CrossDef x)
    {
        PathDef a = Def(x.a), b = Def(x.b);
        Color c = a != null && b != null ? Color.Lerp(a.color, b.color, 0.5f) : Color.white;
        Notice.Big(Loc.T("교차") + "  ·  " + Loc.T(x.name), Loc.T(x.desc), NoticeIcon.Star, 3.2f, NoticeLevel.Normal, c, CrossIcon(x));
        Ring(c, 1.2f);
        Hostile.Play("chime", 0.8f, 1.15f);
    }

    // 입문 · 공명 순간: 영웅 둘레에 갈래 색 고리 두 겹
    internal static void Ring(Color c, float scale)
    {
        PlayerController p = Hostile.Player;
        if (p == null) return;
        Vector3 at = p.transform.position;
        ShockRing.Spawn(at, 0.4f * scale, 4.5f * scale, 0.45f, new Color(c.r, c.g, c.b, 0.9f), 0.28f);
        ShockRing.Spawn(at, 0.2f * scale, 3f * scale, 0.35f, new Color(1f, 1f, 1f, 0.7f), 0.18f);
    }

    // ================================================================= 최근 3판에 고른 갈래 (입문 창 가중치 0.5 · 0.7 · 0.85, 기획서 2-5)
    static string RecentKey(CharacterId who) => "path.recent." + (int)who;

    public static float RecentWeight(CharacterId who, int path)
    {
        PathDef d = DefOf(who, path);
        if (d == null) return 1f;
        string[] keys = Prefs.GetString(RecentKey(who), "").Split(',');
        for (int i = 0; i < keys.Length && i < 3; i++)
            if (keys[i] == d.key) return i == 0 ? 0.5f : i == 1 ? 0.7f : 0.85f;
        return 1f;
    }

    static void Remember(int path)
    {
        if (GameInput.Auto || Hero == null) return;
        PathDef d = Def(path);
        if (d == null) return;
        List<string> keys = new List<string>(Prefs.GetString(RecentKey(Hero.Who), "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
        keys.Remove(d.key);
        keys.Insert(0, d.key);
        while (keys.Count > 3) keys.RemoveAt(keys.Count - 1);
        Prefs.SetString(RecentKey(Hero.Who), string.Join(",", keys));
    }

    // ================================================================= 판 기록 (balance_log 끝 다섯 칸, RunLog)
    //   path1 · path2 (외길이면 single) · path_tiers "p1r1@초|p1r2@초|p2r1@초|p1c@초|p1m@초|x3@초" · cross (새 교차 키) · cards_seq (고른 카드 번호 순서, 구판 판도)
    //   다음 판의 LevelShop.Start 가 지우므로 판이 끝나 씬이 바뀔 때 쓰는 기록에는 지난 판 값이 남아 있음
    public const string LogColumns = "path1,path2,path_tiers,cross,cards_seq";
    static readonly List<string> tierLog = new List<string>(), crossLog = new List<string>();
    static readonly List<int> cardSeq = new List<int>();

    static void Log(string what) => tierLog.Add(what + "@" + RunStats.Seconds.ToString("0"));
    internal static void NoteCard(int id) { if (cardSeq.Count < 400) cardSeq.Add(id); }

    public static string LogRow()
    {
        string p1 = Active && Def(A) != null ? Def(A).key : "";
        string p2 = !Active ? "" : Single ? "single" : Def(B) != null ? Def(B).key : "";
        return p1 + "," + p2 + "," + string.Join("|", tierLog) + "," + string.Join("|", crossLog) + "," + string.Join("|", cardSeq);
    }

    // ================================================================= 아이콘 (안내서 「아이콘」)
    // Resources/Icons/Paths/<IconKey>_<갈래 0~4>        입문 카드 · HUD 입문 칸 (= 갈래 문장)
    //                      <IconKey>_<갈래>_r1 · _r2 · _m  공명 I · II · 극의 알림 (없으면 입문 그림)
    //                      <IconKey>_x<교차 번호>          새 교차 알림 (없으면 앞 갈래의 입문 그림)
    //                      <IconKey>_card31               31번 새 카드 (없으면 ExtraCardDef.fallbackIcon)
    //                      single                         외길 카드 (없으면 갈래 A 의 입문 그림)
    // 파일이 없으면 기존 아이콘으로 대신하므로 그림이 늦어도 게임은 그대로
    static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

    public static Sprite Icon(string file)
    {
        if (string.IsNullOrEmpty(file)) return null;
        if (!icons.TryGetValue(file, out Sprite s)) icons[file] = s = Resources.Load<Sprite>("Icons/Paths/" + file);
        return s;
    }

    public static Sprite SeedIcon(CharacterId who, int path)
    {
        HeroPaths h = Data(who);
        PathDef d = DefOf(who, path);
        if (h == null || d == null) return null;
        Sprite s = Icon(h.IconKey + "_" + path);
        if (s == null && d.cards != null && d.cards.Length > 0) s = LevelShop.CardIcon(who, d.cards[0]);
        return s;
    }

    public static Sprite TierIcon(int path, string suffix)
    {
        if (Hero == null) return null;
        return Icon(Hero.IconKey + "_" + path + "_" + suffix) ?? SeedIcon(Hero.Who, path);
    }

    public static Sprite CrossIcon(CrossDef x)
    {
        if (Hero == null || x == null) return null;
        int k = System.Array.IndexOf(Hero.Crosses, x);
        return Icon(Hero.IconKey + "_x" + k) ?? SeedIcon(Hero.Who, x.a);
    }

    public static Sprite SingleIcon(CharacterId who) => Icon("single") ?? (Active && A >= 0 ? SeedIcon(who, A) : null);

    public static Sprite ExtraIcon(CharacterId who)
    {
        HeroPaths h = Data(who);
        if (h == null || h.Extra == null) return null;
        return Icon(h.IconKey + "_card31") ?? Resources.Load<Sprite>("Icons/ability_" + h.Extra.fallbackIcon);
    }

    // ================================================================= 훅 전달 (SignatureSkills · CharacterKit 이 부름)
    // 영웅 효과 코드에 버그가 있어도 카드 고르기 · 이어하기 · 피격 흐름이 끊기지 않게 예외는 잡아서 로그만 (1초에 한 번)
    static float errAt = -9f;

    static void Report(System.Exception e)
    {
        if (Time.unscaledTime - errAt < 1f) return;
        errAt = Time.unscaledTime;
        Debug.LogException(e);
    }

    static void Safe(System.Action a)
    {
        try { a(); }
        catch (System.Exception e) { Report(e); }
    }

    public static float Outgoing(GameObject target, float dmg)
    {
        if (!Live) return 1f;
        try { return Hero.Outgoing(target, dmg); }
        catch (System.Exception e) { Report(e); return 1f; }
    }

    public static void Hit(GameObject target, float dmg, bool killed, bool proc)
    {
        if (!Live) return;
        try { Hero.Hit(target, dmg, killed, proc); }
        catch (System.Exception e) { Report(e); }
    }

    public static float Taken(float dmg)
    {
        if (!Live) return dmg;
        try { return Mathf.Max(0f, Hero.Taken(dmg)); }
        catch (System.Exception e) { Report(e); return dmg; }
    }

    public static void Hurt(float taken) { if (Live) Safe(() => Hero.Hurt(taken)); }
    public static void Attacked(Vector3 start, Vector2 dir) { if (Live) Safe(() => Hero.Attacked(start, dir)); }
    public static void Killed(Vector3 pos) { if (Live) Safe(() => Hero.Killed(pos)); }
    public static void Ult() { if (Live) Safe(() => Hero.Ult()); }
    public static void Tick() { if (Live) Safe(() => Hero.Tick()); }
    public static void End() { if (Hero != null) Safe(() => Hero.End()); }

    public static float MoveMul()
    {
        if (!Live) return 1f;
        try { return Mathf.Max(0.05f, Hero.MoveMul()); }
        catch (System.Exception e) { Report(e); return 1f; }
    }

    public static float RateMul()
    {
        if (!Live) return 1f;
        try { return Mathf.Max(0.05f, Hero.RateMul()); }
        catch (System.Exception e) { Report(e); return 1f; }
    }

    public static bool KitAttack(CharacterKit kit, Vector3 start, Vector2 dir, float dmg, int shots)
    {
        if (!Live || A < 0) return false;
        try { return Hero.KitAttack(kit, start, dir, dmg, shots); }
        catch (System.Exception e) { Report(e); return false; }
    }

    public static string Badge(int path)
    {
        if (!Live) return null;
        try { return Hero.Badge(path); }
        catch (System.Exception e) { Report(e); return null; }
    }

    // HUD 입문 칸 오른쪽 위: 갈래 효과의 숫자가 있으면 그것, 없으면 극의 · II · I (AbilityHUD)
    public static string HudBadge(int path)
    {
        if (!Has(path)) return null;
        string b = Badge(path);
        if (!string.IsNullOrEmpty(b)) return b;
        return Mastered(path) ? Loc.T("극의") : Res(path) >= 2 ? "II" : Res(path) >= 1 ? "I" : null;
    }

    // ================================================================= 효과 도우미 (영웅 효과 코드가 씀 · 안내서 「도우미」)
    // 고유 스킬과 같은 기준 공격력 (지금 공격력 ÷ 영웅 공격력 배율 × 고유 스킬 배율, SignatureSkills.Atk) — 갈래 효과 피해 = Atk × 비율 × Power(갈래)
    public static float Atk => SignatureSkills.Instance != null ? SignatureSkills.Instance.BaseAtk : 1f;

    // 갈래 효과의 피해 한 번 (명중 효과가 다시 명중 효과를 부르지 않게 · source: 판 기록 dealt_by 에 남는 이름, 예 "저격탄")
    public static void Deal(GameObject target, float dmg, Vector3 dir, float knock, string source = null)
    {
        if (target == null || dmg <= 0f) return;
        bool was = SignatureSkills.Dealing;
        SignatureSkills.Dealing = true;
        using var scope = DamageSource.As(string.IsNullOrEmpty(source) ? DamageSource.Signature : source);
        try { Specials.Damage(target, dmg, dir, knock); }
        finally { SignatureSkills.Dealing = was; }
    }

    // 반지름 안의 적 · 보스 모두에게 (맞힌 수를 돌려줌 · dmg 0 이면 each 만)
    public static int Circle(Vector3 pos, float r, float dmg, float knock, string source = null, System.Action<Collider2D> each = null)
    {
        int n = 0;
        using var list = PhysHits.Circle(pos, r, true);
        foreach (Collider2D c in list)
        {
            if (c == null || !IsFoe(c)) continue;
            if (dmg > 0f) Deal(c.gameObject, dmg, (c.transform.position - pos).normalized, knock, source);
            if (c != null) each?.Invoke(c);
            n++;
        }
        return n;
    }

    // 지속 장판 (SignatureSkills.Zone 그대로 · 끝나면 스스로 사라짐 · 겹친 장판은 합치거나 상한을 둘 것)
    public static SigZone Zone(Vector3 pos, float r, float life, float tick, System.Action<SigZone> onTick, string fx = null, Color? tint = null, float fxSize = 0f, bool ground = false)
        => SignatureSkills.Ensure() != null ? SignatureSkills.Instance.Zone(pos, r, life, tick, onTick, fx, tint, fxSize, ground) : null;

    // 플레이어 무적: 지금 무적 끝 시각에 Mathf.Max 로만 합침 · 한 번에 최대 1.5초 (직접 켜고 끄지 말 것)
    public static void Invuln(float seconds)
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying || seconds <= 0f) return;
        p.GrantInvincibility(Mathf.Min(seconds, InvulnCap));
    }

    // 개수 상한: 목록에서 사라진 것을 치우고, max 를 넘으면 가장 오래된 것부터 Destroy (새것을 넣기 전에 부름)
    public static void Trim(List<GameObject> list, int max)
    {
        list.RemoveAll(g => g == null);
        while (list.Count >= max && list.Count > 0)
        {
            if (list[0] != null) Object.Destroy(list[0]);
            list.RemoveAt(0);
        }
    }

    public static bool IsFoe(Collider2D c) => c != null && (c.CompareTag("enermy") || c.CompareTag("boss"));
    // 보스 · 중간 보스 (황금 고블린 제외 — SignatureSkills.IsBoss 와 같은 기준)
    public static bool IsBoss(GameObject g) => g != null && (g.CompareTag("boss") || (g.GetComponent<MidBossMark>() != null && g.GetComponent<GoldenGoblin>() == null));
    public static List<Transform> Nearest(Vector3 pos, float r, int count, GameObject except = null) => SignatureSkills.Nearest(pos, r, count, except);
}
