using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 사막 비밀 (2.0.0, docs/plans/2026-11-02-big-update/desert-secrets.md): 새 영웅을 얻는 장치
//   무한 모드 사막에서만 켜짐 (일일 도전도 — 사막 맵이므로). 튜토리얼 · 자동 조작 · 체험판 · 배치 모드에서는 꺼짐
//   자리는 판마다 같음 (시작점 기준 방향 · 거리 · 경기장 배율). 둘레 장식 소품은 비움 · 지형 변화 조각도 비켜 감
//   할 일은 말하지 않음 (화살표 · 안내 글 없음) — 흔적(발자국 줄 · 고깔 줄 · 긴 그림자 · 시든 꽃)만 시작점 쪽으로 뻗음
//   비밀마다 작은 클래스 (Secret 상속: Spawn · Tick · OnSolved)
//   풀면: 처음이면 영웅 해금(CharacterData.UnlockSecret · 포인트 없음) + 스팀 업적 + 결과 화면 축하 한 줄,
//         이미 해금했으면 판당 한 번 영혼 조각 소량 (그 영웅으로 하면 업적형 전설 스킨), 일일 도전은 해금 없이 순위 보너스 점수
//   발견 연출은 짧게 (시간 정지 · 컷인 없음): 영웅의 첫 대사 말풍선 1.5초 · Notice 작게
public class DesertSecrets : MonoBehaviour
{
    public const int DailyBonus = 500;                  // 일일 도전: 비밀 하나를 풀 때마다 더해지는 순위 점수 (다섯 비밀 같은 값)
    const int ShardReward = 80;                         // 이미 해금했을 때 판당 한 번 영혼 조각

    // 비밀 목록 (키 · 영웅 · 스팀 업적). 도감 · 해금 판정이 같이 씀
    public static readonly (string key, CharacterId hero, string achievement)[] Defs =
    {
        ("wrestler", CharacterId.Wrestler, SteamAchievements.UnlockWrestler),
        ("foreman", CharacterId.Foreman, SteamAchievements.UnlockForeman),
        ("puppeteer", CharacterId.Puppeteer, SteamAchievements.UnlockPuppeteer),
        ("undertaker", CharacterId.Undertaker, SteamAchievements.UnlockUndertaker),
        ("photographer", CharacterId.Photographer, SteamAchievements.UnlockPhotographer),
    };

    public static DesertSecrets Instance { get; private set; }
    public static int DailyBonusScore { get; private set; }        // 이번 판 일일 도전 보너스 (DailyChallenge.Submit 이 순위 점수에 더함)
    public static string ResultLine { get; private set; } = "";    // 결과 화면 축하 한 줄 (MenuExtras.InstallGameOver)
    static readonly HashSet<string> rewarded = new HashSet<string>();

    // 무한 모드 사막 · 진짜 플레이만 (모드 플래그는 아직 릴레이 · 역류가 없어 무한 모드 조건으로)
    public static bool On => GameMode.IsEndless && !TutorialRun.Active && !GameInput.Auto && !GameInput.TrailerRunning && !Demo.On && !Application.isBatchMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) => { if (s.name == "GameScene") Begin(); };
        if (SceneManager.GetActiveScene().name == "GameScene" && Instance == null) Begin();
    }

    static void Begin()
    {
        DailyBonusScore = 0;
        ResultLine = "";
        rewarded.Clear();
        zones.Clear();
        if (Instance != null) Destroy(Instance.gameObject);
        if (!On) return;
        new GameObject("DesertSecrets").AddComponent<DesertSecrets>();
    }

    // ================================================================= 비켜 갈 자리 (지형 변화 조각 · EndlessTerrain.SpotOk)
    static readonly List<Vector3> zones = new List<Vector3>();       // x, y, 반지름
    public static bool NearAny(Vector3 at, float extra = 0f)
    {
        foreach (Vector3 z in zones) if (Vector2.Distance(at, z) < z.z + extra) return true;
        return false;
    }

    // 이미 얻었는지 (도감 수집 · 해금)
    public static bool IsSolvedEver(string key)
    {
        foreach (var d in Defs) if (d.key == key) return CharacterData.IsUnlocked(d.hero);
        return false;
    }
    public static bool IsSeen(string key) => Prefs.GetInt("secret.seen." + key, 0) == 1;

    // ================================================================= 판 안
    readonly List<Secret> secrets = new List<Secret>();
    bool built;
    float findAt;
    PlayerController player;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        foreach (Secret s in secrets) s.Dispose();
        secrets.Clear();
    }

    void Update()
    {
        if (!On) return;
        StageManager sm = StageManager.Instance;
        player = Hostile.Player;
        if (sm == null || player == null || !sm.Started || RunSave.Pending != null) return;
        if (!built)
        {
            if (Time.unscaledTime < findAt) return;
            findAt = Time.unscaledTime + 0.5f;
            GameObject map = GameObject.Find("DesertMap");
            if (map == null) return;
            Build(map.transform, sm.stage2PlayerStart);
            built = true;
        }
        float dt = Time.deltaTime;
        if (dt <= 0f) dt = 0f;
        foreach (Secret s in secrets) s.Frame(player, dt);
    }

    void Build(Transform map, Vector2 start)
    {
        // 경기장 배율 (자리표는 경기장 197×125 기준 · 다른 배율이면 같은 비율로)
        float k = Mathf.Clamp(ArenaRules.Mul / 1.8f, 0.5f, 1.2f);
        Transform decor = FindDeep(map, "DesertProps");
        secrets.Add(new BullSecret { key = "wrestler", hero = CharacterId.Wrestler, achievement = SteamAchievements.UnlockWrestler, offset = new Vector2(44f, 30f) });
        secrets.Add(new SignSecret { key = "foreman", hero = CharacterId.Foreman, achievement = SteamAchievements.UnlockForeman, offset = new Vector2(50f, -34f) });
        secrets.Add(new TPoseSecret { key = "puppeteer", hero = CharacterId.Puppeteer, achievement = SteamAchievements.UnlockPuppeteer, offset = new Vector2(-46f, 12f) });
        secrets.Add(new GraveSecret { key = "undertaker", hero = CharacterId.Undertaker, achievement = SteamAchievements.UnlockUndertaker, offset = new Vector2(-30f, -38f) });
        secrets.Add(new CrowdSecret { key = "photographer", hero = CharacterId.Photographer, achievement = SteamAchievements.UnlockPhotographer, offset = new Vector2(15f, 42f) });
        foreach (Secret s in secrets)
        {
            s.host = this;
            s.start = start;
            Vector3 at = ClearSpot(Hostile.ClampArena(new Vector3(start.x + s.offset.x * k, start.y + s.offset.y * k, 0f)), s.ClearRadius);
            s.pos = at;
            s.root = new GameObject("Secret_" + s.key).transform;
            s.root.SetParent(transform, false);
            zones.Add(new Vector3(at.x, at.y, s.ClearRadius));
            ClearDecor(decor, at, s.ClearRadius);
            s.Spawn();
        }
    }

    // 2.0.0 검토: 비밀 자리가 벽 · 구조물 · 용암에 겹치면 가까운 빈 자리로 (원 안 전부가 아니라 한가운데 둘레 2.5칸만 봄 — 소품은 ClearDecor 가 치움)
    static Vector3 ClearSpot(Vector3 at, float clearRadius)
    {
        float r0 = Mathf.Min(clearRadius * 0.4f, 2.5f);
        if (!Hostile.WallNear(at, r0)) return at;
        for (float r = 2f; r <= 16f; r += 2f)
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Vector3 q = Hostile.ClampArena(at + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r);
                if (!Hostile.WallNear(q, r0)) return q;
            }
        return at;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t == null) return null;
        foreach (Transform c in t)
        {
            if (c.name == name) return c;
            Transform r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    // 둘레 장식 소품 비우기
    static void ClearDecor(Transform decor, Vector2 at, float radius)
    {
        if (decor == null) return;
        for (int i = decor.childCount - 1; i >= 0; i--)
        {
            Transform c = decor.GetChild(i);
            if (Vector2.Distance(c.position, at) < radius) c.gameObject.SetActive(false);
        }
    }

    // ================================================================= 풀었을 때
    internal void Solve(Secret s)
    {
        if (s.solved) return;
        s.solved = true;
        RunCounters.Add("secret." + s.key + ".solved");
        Vector3 at = s.pos;
        string heroName = Loc.T(CharacterData.Def(s.hero).name);
        if (DailyChallenge.Active)
        {
            // 일일 도전: 해금 없이 순위 보너스 점수만 (D12)
            DailyBonusScore += DailyBonus;
            ResultLine = Loc.T("secret.result.daily").Replace("{0}", DailyBonus.ToString());
            Notice.Toast(NoticeIcon.Star, Loc.T("secret.toast.daily").Replace("{0}", DailyBonus.ToString()), 3.5f);
        }
        else if (!CharacterData.IsUnlocked(s.hero))
        {
            if (CharacterData.UnlockSecret(s.hero))       // 조건 달성 = 바로 해금 (포인트 없음 · D3) · 알림은 그쪽
            {
                SteamAchievements.Unlock(s.achievement);
                s.unlockedNow = true;
                ResultLine = Loc.T("secret.result.new").Replace("{0}", heroName);
            }
        }
        else
        {
            // 이미 해금: 같은 장치가 작은 보상 (판당 한 번)
            if (rewarded.Add(s.key))
            {
                if (ModeRun.Reflux) Cache<Point>.Get?.AddPoint(100);        // 2.0.0~ 역류는 영혼 조각이 없어 포인트로 (쌓는 길을 끔)
                else
                {
                    SoulShards.Add(ShardReward, at, true);
                    Notice.Toast(NoticeIcon.Shards, Loc.T("secret.toast.again").Replace("{0}", ShardReward.ToString()), 3.5f);
                    ResultLine = Loc.T("secret.result.again").Replace("{0}", heroName);
                }
            }
            // 그 영웅으로 다시 풀면 업적형 전설 스킨 (D11)
            if (CharacterData.Selected == s.hero) UnlockLegend(s.hero);
        }
        s.OnSolved();
    }

    static void UnlockLegend(CharacterId hero)
    {
        foreach (SkinDef sk in SkinData.All)
        {
            if (!sk.achievement || sk.who != (int)hero || SkinData.Owns(sk.id)) continue;
            SkinData.UnlockByAchievement(sk.id);
            Notice.Toast(NoticeIcon.Unlock, Loc.T("secret.toast.skin").Replace("{0}", Loc.T(sk.name)), 4f);
            ResultLine = Loc.T("secret.result.skin").Replace("{0}", Loc.T(sk.name));
            Hostile.Play("chime", 0.9f, 1.3f);
            return;
        }
    }

    // 전령 줄 · 로딩 팁이 쓰는 값: 아직 못 푼 비밀 영웅 (판 시작 영웅이 그 영웅이면 없음)
    public static void AddHeraldExtras(HashSet<string> extra)
    {
        if (!On) return;
        foreach (var d in Defs)
        {
            if (CharacterData.IsUnlocked(d.hero) || CharacterData.Selected == d.hero) continue;
            if (d.key == "undertaker" && Random.value > 0.35f) continue;      // 묘비 줄은 낮은 확률
            extra.Add("sec." + d.key);
        }
    }

    // 결과 화면 축하 한 줄 (MenuExtras.InstallGameOver · ResultScreen.StackCenter 가 자리를 잡음)
    public static void BuildResultLine(Transform canvas)
    {
        if (canvas == null || string.IsNullOrEmpty(ResultLine)) return;
        UIKit.EnsureStyle();
        TMP_Text t = UIKit.Text(canvas, "", UiRules.Legacy ? 26f : UiTokens.Body, new Color(1f, 0.84f, 0.35f), Vector2.zero, new Vector2(1200f, 40f));
        t.fontSizeMin = 20f;
        t.name = "SecretLine";
        t.text = ResultLine;
    }

    // ================================================================= 공용 도구
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    internal static Sprite Spr(string name)
    {
        if (!sprites.TryGetValue(name, out Sprite s) || s == null)
        {
            s = Resources.Load<Sprite>("Props/" + name);
            sprites[name] = s;
        }
        return s;
    }

    // 바닥 흔적(발자국 · 돌 · 그림자 · 꽃)은 바닥 층, 서 있는 것은 캐릭터 층에서 Y 순서
    internal static SpriteRenderer MakeSprite(Transform parent, string spriteName, Vector2 at, float scale, bool ground, string nameTag = null)
    {
        GameObject go = new GameObject(nameTag ?? spriteName);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(at.x, at.y, 0f);
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr(spriteName);
        if (ground) { sr.sortingLayerID = SortingLayer.NameToID("GroundFx"); sr.sortingOrder = -20; }
        else { sr.sortingLayerID = SortingLayer.NameToID("Character"); sr.sortingOrder = -Mathf.RoundToInt(at.y * 10f); }
        return sr;
    }

    internal static TextMeshPro MakeText(Transform parent, Vector2 at, string text, float size, Color color, Vector2 box)
    {
        UIKit.EnsureStyle();
        GameObject go = new GameObject("SecretText");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(at.x, at.y, 0f);
        TextMeshPro t = go.AddComponent<TextMeshPro>();
        if (UIKit.Font != null) t.font = UIKit.Font;
        if (UIKit.FontMaterial != null) t.fontSharedMaterial = UIKit.FontMaterial;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        t.color = color;
        t.rectTransform.sizeDelta = box;
        t.text = text;
        Loc.EnsureCjkFallback(t);
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sortingLayerID = SortingLayer.NameToID("Character");
        mr.sortingOrder = -Mathf.RoundToInt(at.y * 10f) + 2;
        return t;
    }

    // 영웅의 말풍선 한 줄 (1.5초)
    internal static void Say(Vector3 at, string key)
    {
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.FloatText(at, Loc.T(key), Color.white, 5f, 0f, 1.5f);
    }

    // 영웅 몸 시트의 첫 칸으로 서 있는 NPC (life 초 뒤 사라짐)
    internal SpriteRenderer Npc(string sheet, Vector2 at, float life, float scale = 0.8f)
    {
        Sprite pick = null;
        foreach (Sprite s in Resources.LoadAll<Sprite>("Characters/" + sheet))
            if (pick == null || s.name == sheet + "_0") pick = s;
        if (pick == null) return null;
        GameObject go = new GameObject("SecretNpc_" + sheet);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(at.x, at.y, 0f);
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pick;
        sr.sortingLayerID = SortingLayer.NameToID("Character");
        sr.sortingOrder = -Mathf.RoundToInt(at.y * 10f) + 1;
        Destroy(go, life);
        return sr;
    }

    internal static Vector2 Perp(Vector2 v) => new Vector2(-v.y, v.x);

    // ================================================================= 비밀 틀
    internal abstract class Secret
    {
        public string key, achievement;
        public CharacterId hero;
        public Vector2 offset, pos, start;           // offset: 시작점 기준 칸 · pos: 월드 자리
        public DesertSecrets host;
        public Transform root;
        public bool solved, unlockedNow;
        float nearT;

        public abstract float ClearRadius { get; }
        public virtual float SeenRadius => 16f;
        public virtual float SeenSeconds => 1f;
        public abstract void Spawn();
        public abstract void Tick(PlayerController p, float dt);
        public virtual void OnSolved() { }
        public virtual void Dispose() { }

        // 판이 멈추지 않은 프레임마다 (죽는 슬로 동안도 부름 — 묘비)
        public void Frame(PlayerController p, float dt)
        {
            if (p == null) return;
            if (!solved && p.isActiveAndEnabled && !p.IsDying)
            {
                // 발견: 가까이 와서 잠깐 머묾 → 도감 · 계약 (이미 본 것은 그대로)
                if (Vector2.Distance(p.transform.position, pos) < SeenRadius) nearT += dt; else nearT = 0f;
                if (nearT >= SeenSeconds) MarkSeen();
            }
            Tick(p, dt);
        }

        void MarkSeen()
        {
            nearT = -9999f;
            if (IsSeen(key)) return;
            Prefs.SetInt("secret.seen." + key, 1);
            Prefs.Save();
            RunCounters.Add("secret." + key + ".found");
        }
    }

    // ================================================================= 1. 씨름꾼: 황소 (원 밖으로 밀어내기)
    internal sealed class BullSecret : Secret
    {
        const float RingR = 4.5f, BodyR = 2.2f, PushShare = 0.35f, Scale = 1.6f;
        Vector2 bull, prevCc, lastDir = Vector2.right;
        bool prevOk, tele;
        float cycle, teleT, lastPush = -9f, pushBack, lurch, anim;
        Vector2 backDir;
        int face = 1;
        SpriteRenderer sr;
        string[] graze = { "secret_bull_graze0", "secret_bull_graze1" };

        public override float ClearRadius => 7f;
        public override float SeenRadius => 18f;

        public override void Spawn()
        {
            bull = pos;
            Vector2 dir = ((Vector2)pos - start).normalized;
            float len = Vector2.Distance(pos, start);
            // 돌 원: 둥근 돌 16개
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f;
                MakeSprite(root, "secret_stonering", pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * RingR, 1.5f, true);
            }
            // 흔적: 시작점에서 10칸부터 돌 원까지 발굽 자국 한 줄 (1.6칸 간격 · 약 22쌍 · 살짝 굽은 길)
            float from = 10f, to = Mathf.Max(from + 8f, len - RingR - 1.5f);
            int n = Mathf.Max(8, Mathf.RoundToInt((to - from) / 1.6f));
            Vector2 perp = Perp(dir);
            float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            for (int i = 0; i < n; i++)
            {
                float t = n <= 1 ? 0f : (float)i / (n - 1);
                Vector2 at = start + dir * Mathf.Lerp(from, to, t) + perp * (Mathf.Sin(t * Mathf.PI * 2f) * 1.6f + (i % 2 == 0 ? 0.4f : -0.4f));
                SpriteRenderer h = MakeSprite(root, "secret_hoof", at, 1.3f, true);
                h.transform.rotation = Quaternion.Euler(0f, 0f, rot);
            }
            sr = MakeSprite(root, graze[0], pos, Scale, false, "Bull");
            sr.transform.position = new Vector3(pos.x, pos.y, 0f);
        }

        public override void Tick(PlayerController p, float dt)
        {
            if (sr == null || dt <= 0f) return;
            anim += dt;
            Vector2 cc = p.ContactCenter;
            bool ok = !p.IsDying && Time.timeScale > 0f;
            if (solved || !ok)
            {
                if (solved || !tele) sr.sprite = Spr(graze[((int)(anim * 2f)) % 2]);
                prevOk = false;
                Place();
                return;
            }
            float period = CharacterData.Selected == CharacterId.Wrestler ? 3f : 2f;       // 씨름꾼으로 풀면 덜 버팀
            Vector2 r = cc - bull;
            float d = r.magnitude;
            Vector2 n = d > 0.01f ? r / d : Vector2.right;
            bool contact = d < BodyR + 0.4f;
            // 몸으로 밀기: 닿은 채 그쪽으로 움직이면 플레이어 속도의 35% 로 밀림 (황소는 단단해서 플레이어는 못 통과)
            bool pushing = false;
            if (d < BodyR)
            {
                bool closing = prevOk && Vector2.Dot(cc - prevCc, -n) > 0.0005f;
                float pen = Mathf.Min(BodyR - d, 0.8f);
                if (closing)
                {
                    bull += -n * pen * PushShare;
                    lastDir = -n;
                    lastPush = Time.time;
                    pushing = true;
                }
                Vector2 want = bull + n * BodyR;
                p.transform.position += (Vector3)(want - cc);
                cc = want;
            }
            if (Mathf.Abs(lastDir.x) > 0.2f) face = lastDir.x > 0f ? 1 : -1;

            // 2초마다 되밂: 0.45초 예고 → 계속 밀고 있으면 2칸 뒤로 / 멈추거나 비키면 황소가 헛힘으로 1.2칸 쏠림
            cycle += dt;
            if (!tele)
            {
                if (cycle >= period)
                {
                    if (Time.time - lastPush < 1.2f && contact) { tele = true; teleT = 0.45f; Fx.Spawn("fx_sandpuff", (Vector3)(bull + Vector2.down * 0.2f), 1.8f, new Color(0.9f, 0.75f, 0.5f), 18f); Hostile.Play("thump", 0.5f, 0.6f); }
                    else cycle = period - 0.4f;
                }
            }
            else
            {
                teleT -= dt;
                if (teleT <= 0f)
                {
                    tele = false;
                    cycle = 0f;
                    if (Time.time - lastPush < 0.15f && d < BodyR + 0.5f) { pushBack = 2f; backDir = n; Hostile.Play("bigboom", 0.5f, 1.2f); }
                    else { lurch = 1.2f; Hostile.Play("thump", 0.6f, 0.8f); }
                }
            }
            if (pushBack > 0f)
            {
                float step = Mathf.Min(pushBack, 14f * dt);
                pushBack -= step;
                Vector3 np = Hostile.ClampArena(p.transform.position + (Vector3)(backDir * step));
                p.transform.position = np;
            }
            if (lurch > 0f)
            {
                float step = Mathf.Min(lurch, 8f * dt);
                lurch -= step;
                bull += lastDir * step;
            }

            // 원 밖으로 나가면 성공
            if (Vector2.Distance(bull, pos) > RingR) { Place(); host.Solve(this); return; }

            sr.sprite = Spr(tele ? "secret_bull_brace" : pushing ? "secret_bull_brace" : graze[((int)(anim * 2f)) % 2]);
            prevCc = cc;
            prevOk = true;
            Place(tele ? Mathf.Sin(anim * 60f) * 0.08f : 0f);
        }

        void Place(float shake = 0f)
        {
            bull = Hostile.ClampArena(new Vector3(bull.x, bull.y, 0f));
            sr.transform.position = new Vector3(bull.x + shake, bull.y, 0f);
            sr.transform.localScale = new Vector3(Scale * face, Scale, 1f);
            sr.sortingOrder = -Mathf.RoundToInt(bull.y * 10f);
        }

        public override void OnSolved()
        {
            // 「음머어—」 + 돌 원에 모래가 솟음 → 씨름꾼이 모래를 털며 일어나 한마디. 황소는 그 자리에서 다시 풀을 뜯음
            Hostile.Play("thump", 1f, 0.45f);
            Hostile.Play("bigboom", 0.5f, 0.7f);
            if (sr != null) sr.sprite = Spr("secret_bull_moo");
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Fx.Spawn("fx_sandpuff", pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * RingR, 2.2f, new Color(0.9f, 0.75f, 0.5f), 18f);
            }
            Vector2 at = pos + (start - pos).normalized * (RingR + 0.5f);
            host.Npc("wrestler", at, 5f);
            Say(at + Vector2.up * 0.8f, "secret.say.wrestler");
            RunCounters.Add("secret.wrestler");
        }
    }

    // ================================================================= 2. 공사 반장: 대답하는 표지판 (8대 때리기)
    internal sealed class SignSecret : Secret
    {
        const int Hits = 8;
        const float Scale = 1.6f;
        readonly List<SpriteRenderer> cones = new List<SpriteRenderer>();
        SpriteRenderer board;
        TextMeshPro label;
        int hits;
        float lastHit = -9f, shake;
        bool beta;

        public override float ClearRadius => 8f;
        public override float SeenRadius => 14f;

        public override void Spawn()
        {
            // 고깔 9개: 시작점에서 약 15칸부터 표지판 앞까지 한 줄 (첫 고깔이 첫 화면 가장자리에 보임)
            Vector2 dir = ((Vector2)pos - start).normalized;
            float len = Vector2.Distance(pos, start);
            float from = 15f, to = Mathf.Max(from + 10f, len - 5f);
            Vector2 perp = Perp(dir);
            for (int i = 0; i < 9; i++)
            {
                float t = (float)i / 8f;
                Vector2 at = start + dir * Mathf.Lerp(from, to, t) + perp * (i % 2 == 0 ? 0.25f : -0.25f);
                SpriteRenderer c = MakeSprite(root, "secret_cone", at, 1.5f, false, "Cone");
                cones.Add(c);
            }
            board = MakeSprite(root, "secret_sign_0", pos, Scale, false, "Sign");
            label = MakeText(root, pos + Vector2.up * 2.25f, Loc.T("secret.sign.0"), 2.2f, new Color(0.1f, 0.08f, 0.06f), new Vector2(3.4f, 1.4f));
            label.enableAutoSizing = true;
            label.fontSizeMin = 1f;
            label.fontSizeMax = 2.4f;
            PlayerController.AttackFired += OnAttack;
            PlayerController.UltUsed += OnUlt;
        }

        public override void Dispose()
        {
            PlayerController.AttackFired -= OnAttack;
            PlayerController.UltUsed -= OnUlt;
        }

        Vector2 Center => pos + Vector2.up * 1.6f;

        // 표지판 2.5칸 안에서 표지판 쪽(조준 ±60°)으로 평타 → 한 대 (0.4초에 한 번만)
        void OnAttack(Vector3 origin, Vector2 dir)
        {
            if (solved || !On || Time.timeScale <= 0f) return;
            Vector2 to = Center - (Vector2)origin;
            if (to.magnitude > 2.5f + 1.6f || Vector2.Angle(dir, to) > 60f) return;
            Hit();
        }

        // 필살기가 3칸 안에 떨어지면 한 대
        void OnUlt()
        {
            if (solved || !On || Time.timeScale <= 0f) return;
            PlayerController p = Hostile.Player;
            if (p != null && Vector2.Distance(p.transform.position, Center) <= 3f + 1.6f) Hit();
        }

        void Hit()
        {
            if (Time.time - lastHit < 0.4f) return;
            lastHit = Time.time;
            hits++;
            if (hits == 1)
            {
                RunCounters.Add("secret.foreman.found");
                PlayerController p = Hostile.Player;
                if (p != null) Say(p.transform.position + Vector3.up * 0.6f, "secret.say.foreman.first");      // 첫 대에 머리 위 「언제요?」
            }
            Hostile.Play("clank", 0.7f, 0.9f + hits * 0.04f);
            Fx.Spawn("fx_spark", (Vector3)Center, 1.4f, new Color(1f, 0.85f, 0.4f), 18f);
            shake = 0.2f;
            if (hits >= Hits) { host.Solve(this); return; }
            if (hits >= 4) board.sprite = Spr("secret_sign_1");
            string key = "secret.sign." + Mathf.Min(hits, 6);
            label.text = Loc.T(key);
            RunCounters.Add("secret.foreman");
        }

        public override void Tick(PlayerController p, float dt)
        {
            if (board == null) return;
            if (shake > 0f)
            {
                shake -= dt;
                board.transform.position = new Vector3(pos.x + Random.Range(-0.1f, 0.1f), pos.y, 0f);
            }
            else board.transform.position = new Vector3(pos.x, pos.y, 0f);
            if (label != null && !solved) label.transform.position = new Vector3(board.transform.position.x, pos.y + 2.25f, 0f);
        }

        public override void OnSolved()
        {
            bool again = CharacterData.IsUnlocked(hero) && !DailyChallenge.Active;
            beta = CharacterData.Selected == CharacterId.Foreman;      // 반장으로 다시: 마지막 글이 「버그 제보 감사합니다」 (고깔 더미 덤프는 간소화 — 8대로 대신)
            host.StartCoroutine(Fall(again));
            RunCounters.Add("secret.foreman");
            if (beta && !DailyChallenge.Active) RunCounters.Add("secret.foreman.beta");
        }

        IEnumerator Fall(bool again)
        {
            // 앞으로 「쿵」 넘어짐
            board.sprite = Spr("secret_sign_2");
            if (label != null) label.text = "";
            Hostile.Play("bigboom", 0.7f, 0.8f);
            Fx.Spawn("fx_sandpuff", (Vector3)pos, 2.4f, new Color(0.9f, 0.75f, 0.5f), 18f);
            if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.Shake(0.25f, 0.2f);
            // 고깔 9개가 차례로 「딱딱딱」 쓰러짐 (0.08초 간격)
            host.StartCoroutine(TopplePath());
            // 반장이 삽날을 들고 일어남
            Vector2 at = pos + new Vector2(-0.8f, 1.2f);
            host.Npc("foreman", at, 4.5f);
            Say(at + Vector2.up * 1.2f, "secret.say.foreman");
            yield return new WaitForSeconds(1.6f);
            // 표지판을 다시 세움 (뒷면 「공사 완료?」)
            if (board != null)
            {
                board.sprite = Spr("secret_sign_0");
                if (label != null)
                {
                    label.text = Loc.T(beta ? "secret.sign.beta" : again ? "secret.sign.again" : "secret.sign.done");
                    label.transform.position = new Vector3(pos.x, pos.y + 2.25f, 0f);
                }
            }
        }

        IEnumerator TopplePath()
        {
            foreach (SpriteRenderer c in cones)
            {
                if (c == null) continue;
                c.transform.rotation = Quaternion.Euler(0f, 0f, c.transform.position.x < pos.x ? -78f : 78f);
                Hostile.Play("clank", 0.35f, 1.4f);
                yield return new WaitForSeconds(0.08f);
            }
        }
    }

    // ================================================================= 3. 인형사: 돌아보지 않을 때만 걷는 더미
    internal sealed class TPoseSecret : Secret
    {
        const float WakeR = 22f, StepSpeed = 2.4f, LookDeg = 60f, BackDeg = 100f, TouchR = 1.5f, Scale = 1.6f;
        Vector2 dummy;
        SpriteRenderer body, shadow;
        float anim, creakAt = -9f;
        bool wasMoving;

        public override float ClearRadius => 8f;
        public override float SeenRadius => 20f;

        public override void Spawn()
        {
            dummy = pos;
            shadow = MakeSprite(root, "secret_shadow", pos, 4.5f, true, "Shadow");
            body = MakeSprite(root, "secret_tpose_0", pos, Scale, false, "Dummy");
            AimShadow();
        }

        // 긴 그림자: 발밑에서 시작점 쪽으로 18칸 (지는 해 방향) — 더미가 움직이면 같이 옮겨짐
        void AimShadow()
        {
            Vector2 dir = (start - dummy).normalized;
            shadow.transform.position = new Vector3(dummy.x, dummy.y, 0f);
            shadow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        public override void Tick(PlayerController p, float dt)
        {
            if (body == null || solved || dt <= 0f || p.IsDying || Time.timeScale <= 0f) return;
            Vector2 hero = p.transform.position;
            Vector2 to = dummy - hero;
            float dist = to.magnitude;
            bool moving = false;
            if (dist <= WakeR)
            {
                Camera cam = Camera.main;
                Vector2 aimDir = Vector2.right;
                if (cam != null)
                {
                    Vector3 w = cam.ScreenToWorldPoint(GameInput.MousePosition);
                    aimDir = (Vector2)w - hero;
                }
                if (aimDir.sqrMagnitude < 0.0001f) aimDir = Vector2.right;
                float ang = Vector2.Angle(aimDir, to);
                if (ang > BackDeg)
                {
                    // 등지고 있는 동안 영웅 쪽으로 걸어옴 (초당 2.4칸)
                    moving = true;
                    dummy -= to.normalized * StepSpeed * dt;
                    anim += dt;
                }
                else if (ang < LookDeg && wasMoving && Time.time - creakAt > 1f)
                {
                    // 걷던 중에 들킴: 「삐걱」 한 번
                    creakAt = Time.time;
                    Hostile.Play("clank", 0.5f, 0.55f);
                }
                if (Vector2.Distance(dummy, hero) < TouchR && ang >= LookDeg + 20f) { Refresh(false); host.Solve(this); return; }
            }
            wasMoving = moving;
            Refresh(moving);
        }

        void Refresh(bool moving)
        {
            body.sprite = Spr(moving && ((int)(anim * 2f)) % 2 == 1 ? "secret_tpose_1" : "secret_tpose_0");
            body.transform.position = new Vector3(dummy.x, dummy.y, 0f);
            body.sortingOrder = -Mathf.RoundToInt(dummy.y * 10f);
            AimShadow();
        }

        public override void OnSolved()
        {
            host.StartCoroutine(Pulled());
            RunCounters.Add("secret.puppeteer");
        }

        // 위에서 줄이 「팅」 내려와 머리에 걸림 → 꾸벅 인사 → 인형사 목소리 1.5초 → 줄에 끌려 위로 사라짐
        IEnumerator Pulled()
        {
            Vector3 top = new Vector3(dummy.x, dummy.y + 3.2f, 0f);
            SpriteRenderer str = MakeSprite(root, "secret_string", new Vector2(top.x, top.y), 1f, false, "String");
            str.sortingOrder = 40;
            str.transform.localScale = new Vector3(1f, 0.1f, 1f);
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                str.transform.localScale = new Vector3(1f, Mathf.Lerp(0.1f, 2.4f, t / 0.2f), 1f);
                yield return null;
            }
            Hostile.Play("chime", 0.9f, 2f);
            Say(top + Vector3.up * 7f, "secret.say.puppeteer");
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)         // 꾸벅
            {
                body.transform.localScale = new Vector3(Scale, Scale * (1f - 0.12f * Mathf.Sin(t / 0.5f * Mathf.PI)), 1f);
                yield return null;
            }
            body.transform.localScale = Vector3.one * Scale;
            yield return new WaitForSeconds(0.5f);
            for (float t = 0f; t < 1f; t += Time.deltaTime)           // 줄에 끌려 위로
            {
                body.transform.position += Vector3.up * (9f * Time.deltaTime);
                str.transform.localScale = new Vector3(1f, 2.4f + t * 4f, 1f);
                Color c = body.color; c.a = 1f - t; body.color = c;
                if (shadow != null) { Color sc = shadow.color; sc.a = 1f - t; shadow.color = sc; }
                yield return null;
            }
            if (body != null) body.enabled = false;
            if (shadow != null) shadow.enabled = false;
            if (str != null) str.enabled = false;
        }
    }

    // ================================================================= 5. 사진사: 북적이는 장면 (조건형 · 화면 안 적을 살려 둔 채 12초)
    //   고정 흔적: 빈 삼각대 (렌즈 반사광이 3초마다 반짝) — 판정 장소가 아님
    //   조건: 3:00 이후, 카메라 화면 안(1칸 안쪽) 살아 있는 일반 적 수 >= N (N = 한 화면 최대 수 - 4, 12~18) 인 동안 노출 시간이 쌓여 12초
    //         기준 아래면 0.5배속으로 줄어듦 · 개발자 · 엄마전(잡몹 없음)은 판정 멈춤
    //   달성: 화면 가장자리 밖 가까운 곳에서 플래시 「찰칵!」 -> 그쪽에 삼각대와 사진사 -> 가까이 가면 해금 (30초 안에 못 가면 처음부터)
    internal sealed class CrowdSecret : Secret
    {
        const float Need = 12f, CheckEvery = 0.25f, MinSeconds = 180f, MeetR = 3.5f, WaitSeconds = 30f;
        SpriteRenderer tripod, lens;
        float exposure, checkAt, flickAt, waitUntil;
        int state;                      // 0 지켜봄 · 1 사진사가 기다림 · 2 끝
        Vector2 spot;
        SpriteRenderer spotTripod;
        SpriteRenderer npc;
        UnityEngine.UI.Image[] corner;
        GameObject canvasGo;

        public override float ClearRadius => 6f;
        public override float SeenRadius => 22f;
        public override float SeenSeconds => 1f;

        public override void Spawn()
        {
            tripod = MakeSprite(root, "secret_tripod_0", pos, 1.6f, false, "Tripod");
            flickAt = Time.time + 1f;
        }

        // 기준 수: 지금 한 화면 상한 - 6 (10 ~ 16)
        //   2.0.0 검토: 3~15분이 너무 빡빡 (상한의 75~82%를 화면 안에 12초) — 자동 조작 실측으로 다시 볼 것
        const int ThresholdBelow = 6, ThresholdMin = 10, ThresholdMax = 16;
        static int Threshold() => Mathf.Clamp(EnemySpawner.MaxAliveNow - ThresholdBelow, ThresholdMin, ThresholdMax);

        public override void Tick(PlayerController p, float dt)
        {
            // 렌즈 반사광: 3초마다 반짝 (멀리서도 보이는 유일한 반짝임)
            if (tripod != null && state != 2 && Time.time >= flickAt)
            {
                flickAt = Time.time + 3f;
                host.StartCoroutine(Glint());
            }
            if (solved || state == 2 || p.IsDying || Time.timeScale <= 0f) return;
            if (state == 1) { WaitTick(p); return; }
            if (GameMode.EndlessSeconds < MinSeconds) return;
            if (Time.time < checkAt) { Fade(); return; }
            checkAt = Time.time + CheckEvery;
            EndlessMode em = EndlessMode.Instance;
            if (em != null && em.NoMobs) return;                 // 개발자 · 엄마전: 판정 멈춤 (노출 시간 유지)
            int n = CountInView();
            if (n >= Threshold()) exposure = Mathf.Min(Need, exposure + CheckEvery);
            else exposure = Mathf.Max(0f, exposure - CheckEvery * 0.5f);
            Fade();
            if (exposure >= Need) host.StartCoroutine(Appear(p));
        }

        // 카메라 화면 안(뷰포트에서 1칸 안쪽) 살아 있는 일반 적 수 (부스러기 · 보스 제외 · 중간 보스 · 종이 포함)
        static int CountInView()
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return 0;
            float h = cam.orthographicSize - 1f, w = cam.orthographicSize * cam.aspect - 1f;
            int n = 0;
            using (var ovl = PhysHits.Box(cam.transform.position, new Vector2(w * 2f, h * 2f), 0f))
                foreach (Collider2D c in ovl)
                {
                    if (c == null || !c.CompareTag("enermy")) continue;
                    EnermyController e = EnermyController.FromCollider(c);
                    if (e != null && !e.IsDead && e.swarm == null) n++;
                }
            return n;
        }

        // 화면 네 모서리 초점 꺾쇠: 노출 비율에 맞춰 알파 0 -> 0.6 · 기준 이상인 동안은 0.5초 주기로 깜빡
        void Fade()
        {
            float k = Mathf.Clamp01(exposure / Need);
            if (k <= 0.01f) { if (canvasGo != null) canvasGo.SetActive(false); return; }
            if (canvasGo == null) BuildCorners();
            canvasGo.SetActive(true);
            float a = 0.6f * k;
            if (exposure > 0f && Time.time % 0.5f < 0.25f && k > 0.5f) a *= 0.6f;
            foreach (var im in corner) if (im != null) im.color = new Color(1f, 1f, 1f, a);
        }

        void BuildCorners()
        {
            canvasGo = new GameObject("PhotoCorners");
            canvasGo.transform.SetParent(host.transform, false);
            Canvas cv = canvasGo.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 30;
            corner = new UnityEngine.UI.Image[8];
            for (int i = 0; i < 4; i++)
            {
                float sx = (i & 1) == 0 ? 0f : 1f, sy = i < 2 ? 0f : 1f;
                for (int k = 0; k < 2; k++)
                {
                    GameObject g = new GameObject("c" + i + k);
                    g.transform.SetParent(canvasGo.transform, false);
                    UnityEngine.UI.Image im = g.AddComponent<UnityEngine.UI.Image>();
                    im.raycastTarget = false;
                    RectTransform rt = im.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(sx, sy);
                    rt.pivot = new Vector2(sx, sy);
                    float off = 40f;
                    rt.anchoredPosition = new Vector2(sx == 0f ? off : -off, sy == 0f ? off : -off);
                    rt.sizeDelta = k == 0 ? new Vector2(70f, 5f) : new Vector2(5f, 70f);
                    corner[i * 2 + k] = im;
                }
            }
        }

        IEnumerator Glint()
        {
            if (tripod == null) yield break;
            tripod.sprite = Spr("secret_tripod_1") ?? tripod.sprite;
            yield return new WaitForSeconds(0.25f);
            if (tripod != null) tripod.sprite = Spr("secret_tripod_0") ?? tripod.sprite;
        }

        // 달성: 화면 가장자리만 흰빛 0.1초 + 「찰칵!」 -> 그쪽 화면 밖 가까운 곳에 삼각대와 사진사
        IEnumerator Appear(PlayerController p)
        {
            state = 1;
            exposure = 0f;
            if (canvasGo != null) canvasGo.SetActive(false);
            Camera cam = Camera.main;
            Vector2 me = p.transform.position;
            Vector2 toward = ((Vector2)pos - me);
            if (toward.sqrMagnitude < 1f) toward = Vector2.up;
            toward.Normalize();
            float hh = cam != null ? cam.orthographicSize : 15f, hw = cam != null ? hh * cam.aspect : 26f;
            Vector2 cc = cam != null ? (Vector2)cam.transform.position : me;
            // 삼각대 쪽과 가장 가까운 변 밖 3칸
            bool horizontal = Mathf.Abs(toward.x) * hh > Mathf.Abs(toward.y) * hw;
            Vector2 edge = horizontal ? new Vector2(cc.x + Mathf.Sign(toward.x) * (hw + 3f), Mathf.Clamp(cc.y + toward.y * hh, cc.y - hh * 0.6f, cc.y + hh * 0.6f))
                                      : new Vector2(Mathf.Clamp(cc.x + toward.x * hw, cc.x - hw * 0.6f, cc.x + hw * 0.6f), cc.y + Mathf.Sign(toward.y) * (hh + 3f));
            spot = (Vector2)Hostile.ClampArena(new Vector3(edge.x, edge.y, 0f));
            // 2.0.0 검토: 지형 조각 · 용암 위면 화면 안쪽으로 2칸씩 당겨 다시 찾음
            for (int pull = 0; pull < 6 && Hostile.WallNear(spot, 3.5f); pull++)
                spot = (Vector2)Hostile.ClampArena(new Vector3(spot.x + (cc.x - spot.x) * 0.15f, spot.y + (cc.y - spot.y) * 0.15f, 0f));
            Hostile.Play("imp_shutter", 1f, 1f);
            RunCounters.Add("secret.photographer");
            host.StartCoroutine(EdgeFlash(horizontal, toward));
            spotTripod = MakeSprite(root, "secret_tripod_0", spot, 1.6f, false, "SpotTripod");
            npc = host.Npc("photographer", spot + new Vector2(horizontal ? -Mathf.Sign(toward.x) * 1.4f : 1.4f, 0f), WaitSeconds + 8f, 1f);
            waitUntil = Time.time + WaitSeconds;
            yield return null;
        }

        IEnumerator EdgeFlash(bool horizontal, Vector2 toward)
        {
            GameObject g = new GameObject("PhotoEdge");
            g.transform.SetParent(host.transform, false);
            Canvas cv = g.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 31;
            // 빛이 오는 쪽 변에 얇은 흰 띠
            GameObject b = new GameObject("band");
            b.transform.SetParent(g.transform, false);
            UnityEngine.UI.Image im = b.AddComponent<UnityEngine.UI.Image>();
            im.raycastTarget = false;
            RectTransform rt = im.rectTransform;
            if (horizontal)
            {
                float sx = toward.x > 0f ? 1f : 0f;
                rt.anchorMin = new Vector2(sx, 0f); rt.anchorMax = new Vector2(sx, 1f); rt.pivot = new Vector2(sx, 0.5f); rt.sizeDelta = new Vector2(90f, 0f);
            }
            else
            {
                float sy = toward.y > 0f ? 1f : 0f;
                rt.anchorMin = new Vector2(0f, sy); rt.anchorMax = new Vector2(1f, sy); rt.pivot = new Vector2(0.5f, sy); rt.sizeDelta = new Vector2(0f, 90f);
            }
            im.color = new Color(1f, 1f, 1f, 0.9f);
            yield return new WaitForSecondsRealtime(0.1f);
            Destroy(g);
        }

        // 가까이 가면 해금 · 시간이 지나면 모두 거두고 처음부터
        void WaitTick(PlayerController p)
        {
            if (Vector2.Distance(p.transform.position, spot) < MeetR)
            {
                state = 2;
                host.Solve(this);
                return;
            }
            if (Time.time >= waitUntil)
            {
                state = 0;
                exposure = 0f;
                if (spotTripod != null) Destroy(spotTripod.gameObject);
                if (npc != null) Destroy(npc.gameObject);
            }
        }

        public override void OnSolved()
        {
            if (unlockedNow) Herald.Forced = "herald.secret.photo.met";
            host.StartCoroutine(Reveal());
        }

        // 말풍선 1.5초 -> 「현상하러 갈게요」 하며 원래 변으로 걸어 나감
        IEnumerator Reveal()
        {
            string line = !CharacterData.IsUnlocked(hero) || DailyChallenge.Active ? "secret.say.photographer"
                        : CharacterData.Selected == CharacterId.Photographer ? "secret.say.photographer.self" : "secret.say.photographer.again";
            Say(spot + new Vector2(0f, 2.2f), line);
            yield return new WaitForSeconds(1.5f);
            Say(spot + new Vector2(0f, 2.2f), "secret.say.photographer.bye");
            Vector2 away = (spot - (Vector2)pos).normalized;
            for (float t = 0f; t < 2f && npc != null; t += Time.deltaTime)
            {
                npc.transform.position += (Vector3)(away * 3f * Time.deltaTime);
                yield return null;
            }
            if (npc != null) Destroy(npc.gameObject);
            if (spotTripod != null) Destroy(spotTripod.gameObject);
        }

        public override void Dispose()
        {
            if (canvasGo != null) Destroy(canvasGo);
        }
    }

    // ================================================================= 4. 장의사: 내 이름 묘비 (4칸 안에서 쓰러지면)
    internal sealed class GraveSecret : Secret
    {
        const float DeathR = 4f, Scale = 1.6f;
        TextMeshPro inscription;
        bool dyingDone;
        int deaths;

        public override float ClearRadius => 8f;
        public override float SeenRadius => 18f;
        public override float SeenSeconds => 2f;       // 묘비를 봄: 화면 안에 2초

        public override void Spawn()
        {
            // 흔적: 시작점에서 묘비 쪽 직선 위에 시든 꽃 세 송이 (12 · 24 · 36칸)
            Vector2 dir = ((Vector2)pos - start).normalized;
            float len = Vector2.Distance(pos, start);
            foreach (float d in new[] { 12f, 24f, 36f })
                if (d < len - 4f) MakeSprite(root, "secret_flower", start + dir * d + Perp(dir) * Random.Range(-0.8f, 0.8f), 1.4f, false, "Flower");
            MakeSprite(root, "secret_grave", pos, Scale, false, "Grave");
            deaths = 0;
            for (int i = 0; i < CharacterData.All.Length; i++) deaths += Prefs.GetInt("death." + i, 0);
            inscription = MakeText(root, pos + new Vector2(0f, 1.5f), InscriptionText(deaths), 1.7f, new Color(0.12f, 0.12f, 0.16f), new Vector2(2.4f, 1.8f));
            inscription.fontStyle = FontStyles.Normal;
            inscription.enableAutoSizing = true;
            inscription.fontSizeMin = 0.8f;
            inscription.fontSizeMax = 1.8f;
            inscription.lineSpacing = -5f;
        }

        // 이름 = 스팀 이름 (8글자 넘으면 …) · 없으면 「여행자」 · 일일 도전은 「오늘의 도전자」
        static string NameText()
        {
            if (DailyChallenge.Active) return Loc.T("secret.grave.daily");
            string n = SteamManager.MyName();
            if (string.IsNullOrWhiteSpace(n)) return Loc.T("여행자");
            n = n.Trim();
            StringInfo si = new StringInfo(n);
            if (si.LengthInTextElements > 8) n = si.SubstringByTextElements(0, 8) + "…";
            return "<noparse>" + n + "</noparse>";
        }

        static string InscriptionText(int n)
        {
            string name = NameText();
            if (DailyChallenge.Active) return Loc.T("secret.grave.today").Replace("{0}", name);
            if (n <= 0) return Loc.T("secret.grave.empty").Replace("{0}", name);
            return Loc.T("secret.grave.text").Replace("{0}", name).Replace("{1}", n.ToString());
        }

        public override void Tick(PlayerController p, float dt)
        {
            if (solved || dyingDone || !p.IsDying) return;
            if (CharacterData.Selected == hero) return;         // 기획: 「장의사가 아닌 영웅」 (장의사 본인은 해금 대상이 아님)
            dyingDone = true;                                   // 쓰러지는 순간 위치를 한 번만 봄
            if (Vector2.Distance(p.transform.position, pos) > DeathR) return;
            RunCounters.Add("secret.undertaker");
            host.Solve(this);
        }

        public override void OnSolved()
        {
            if (unlockedNow) Herald.Forced = "herald.secret.grave";        // 그다음 전령 줄은 「네 묘비 앞에서 쓰러졌다…」
            host.StartCoroutine(Reveal());
        }

        // 쓰러지는 슬로 1.1초 안: N → N+1 깜빡 · 묘비 뒤에서 장의사가 삽을 짚고 걸어 나옴 · 말풍선 1.5초
        IEnumerator Reveal()
        {
            if (inscription != null)
            {
                for (int i = 0; i < 3; i++) { inscription.enabled = false; yield return new WaitForSecondsRealtime(0.06f); inscription.enabled = true; yield return new WaitForSecondsRealtime(0.06f); }
                inscription.text = InscriptionText(deaths + 1);
            }
            SpriteRenderer npc = host.Npc("undertaker", pos + new Vector2(-0.4f, 0.6f), 6f);
            string line = !CharacterData.IsUnlocked(hero) || DailyChallenge.Active ? "secret.say.undertaker"
                        : CharacterData.Selected == CharacterId.Undertaker ? "secret.say.undertaker.self" : "secret.say.undertaker.again";
            Say(pos + new Vector2(1.2f, 1.6f), line);
            if (npc != null)
                for (float t = 0f; t < 0.9f && npc != null; t += Time.unscaledDeltaTime)
                {
                    npc.transform.position += new Vector3(1.4f * Time.unscaledDeltaTime, -0.15f * Time.unscaledDeltaTime, 0f);
                    yield return null;
                }
        }
    }
}
