using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 스팀 상점 스크린샷 1920×1080 (tools/steam/store-art/shoot.ps1 로 프로젝트 복사본에서 배치 실행)
// 영상 없이 사진만. 판마다 「한창 진행 중인 판」처럼 꾸밈: 레벨 · 배운 카드 · 진화 무기 · 경과 시간 · 코인 · 깎인 체력 · 몰려든 적.
// 언어(SHOTS_LANGS = en,ko,ja,zh)마다 같은 장면을 다시 찍어 <언어>/<이름>.png 로 저장.
public class StoreShots
{
    static readonly string Out = Environment.GetEnvironmentVariable("SHOTS_OUT") ?? "C:/Temp/SoulSaverShots";
    static readonly string[] Langs = (Environment.GetEnvironmentVariable("SHOTS_LANGS") ?? "en,ko,ja,zh").Split(',');
    static readonly string Only = Environment.GetEnvironmentVariable("SHOTS_ONLY") ?? "";     // 쉼표로 고른 장면만 (비우면 전부)
    const int W = 1920, H = 1080, Fps = 30;
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    static Type T(string n) => Type.GetType(n + ", Assembly-CSharp");
    static object Get(object o, string f) => o.GetType().GetField(f, Any)?.GetValue(o);
    static void Set(object o, string f, object v) { FieldInfo fi = o.GetType().GetField(f, Any); if (fi != null) fi.SetValue(o, Convert.ChangeType(v, fi.FieldType)); }
    static object Call(object o, string m, params object[] a)
    {
        foreach (MethodInfo mi in o.GetType().GetMethods(Any))
            if (mi.Name == m && mi.GetParameters().Length == a.Length) return mi.Invoke(o, a);
        throw new MissingMethodException(o.GetType().Name, m);
    }
    static object SCall(string type, string m, params object[] a) => T(type).GetMethod(m, Any).Invoke(null, a);
    static void SSet(string type, string f, object v) => T(type).GetField(f, Any).SetValue(null, v);
    static void In(string f, object v) => T("GameInput").GetField(f).SetValue(null, v);
    static UnityEngine.Object Find(string type) => UnityEngine.Object.FindFirstObjectByType(T(type));
    static string lang = "en";

    // ================================================================= 매 프레임 지킴이: 자동 조종 · 체력 · 경험치 · 떠다니는 글자 정리
    class Keeper : MonoBehaviour
    {
        public bool autopilot, keepLevel = true, hideBanner = true;
        public float hpFrac = 0.82f, expFrac = 0.68f;
        float nextTap;
        bool held;

        void Update()
        {
            if (!autopilot) return;
            Component player = (Component)Find("PlayerController");
            if (player == null) return;
            Vector3 me = player.transform.position;
            Vector3? target = Nearest(me, out bool boss);
            if (target.HasValue)
            {
                Vector2 to = target.Value - me;
                float d = to.magnitude;
                Vector2 side = new Vector2(-to.y, to.x).normalized;
                float near = boss ? 7f : 3.5f, far = boss ? 10f : 7f;
                In("AutoAim", target.Value);
                In("AutoMove", side * 0.6f + (d < near ? -to.normalized * 1.2f : d > far ? to.normalized * 0.7f : Vector2.zero));
            }
            else { In("AutoMove", Vector2.zero); In("AutoAim", me + Vector3.right * 5f); }
            if (Time.time >= nextTap)
            {
                held = !held;
                if (held) { In("FireDownFrame", Time.frameCount + 1); In("AutoFire", true); nextTap = Time.time + 0.42f; }
                else { In("FireUpFrame", Time.frameCount + 1); In("AutoFire", false); nextTap = Time.time + 0.06f; }
            }
        }

        void LateUpdate()
        {
            object p = Find("PlayerController");
            if (p != null)
            {
                float max = Convert.ToSingle(Get(p, "PlayerMaxHealth"));
                Set(p, "PlayerHealth", Mathf.Round(max * hpFrac));
                if (keepLevel)
                {
                    int lv = (int)Get(p, "level");
                    float need = (float)T("PlayerController").GetMethod("NeedExp").Invoke(null, new object[] { lv });
                    Set(p, "nowEXP", need * expFrac);
                }
            }
            // 적 기술 이름 · 중간 보스 이름표처럼 장면을 가리는 떠다니는 글자
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t != null && (t.name == "SkillTag" || t.name == "MidBossTag" || t.name == "FloatText")) Destroy(t.gameObject);
            // 맞을 때 화면 가장자리가 붉게 번쩍이는 효과 · 머리 위 「영혼 트리 [T]」 알림 (촬영 중 체력을 고정해 두므로 의미 없음)
            GameObject flash = GameObject.Find("DamageFlash");
            if (flash != null && flash.activeSelf) flash.SetActive(false);
            if (Find("GrowthPrompts") is Behaviour gp && gp.enabled)
            {
                gp.enabled = false;
                if (Get(gp, "tag") is Component tg) tg.gameObject.SetActive(false);
            }
            if (hideBanner)
            {
                object sm = Find("StageManager");
                if (sm != null && Get(sm, "banner") is GameObject b && b.activeSelf) b.SetActive(false);
            }
        }

        static Vector3? Nearest(Vector3 from, out bool boss)
        {
            Vector3? best = null; float bd = 18f * 18f;
            boss = false;
            foreach (string type in new[] { "bosss", "EnermyController" })
            {
                boss = type == "bosss";
                foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(T(type), FindObjectsSortMode.None))
                {
                    if (o.GetType().GetProperty("IsDead")?.GetValue(o) is bool dead && dead) continue;
                    Vector3 q = ((Component)o).transform.position;
                    float d = (q - from).sqrMagnitude;
                    if (d < bd) { bd = d; best = q; }
                }
                if (best.HasValue) return best;
            }
            return best;
        }
    }

    static Keeper keep;

    // ================================================================= 찍기
    static IEnumerator F(float seconds)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Fps));
        for (int i = 0; i < n; i++) yield return null;
    }

    // 연출이 실제 시간(WaitForSecondsRealtime · unscaledDeltaTime)으로 흐르는 곳은 실제 시간으로 기다림
    static IEnumerator Real(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end) yield return null;
    }

    static IEnumerator Until(Func<bool> done, float maxSeconds)
    {
        float end = Time.realtimeSinceStartup + maxSeconds;
        while (!done() && Time.realtimeSinceStartup < end) yield return null;
    }

    static void CanvasesToCamera(Camera cam)
    {
        SortingLayer[] layers = SortingLayer.layers;
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 5f;
                c.overrideSorting = true;
                c.sortingLayerName = layers[layers.Length - 1].name;
                c.sortingOrder = Mathf.Min(32000, 30000 + c.sortingOrder);
            }
            else if (!c.isRootCanvas && c.overrideSorting && c.sortingLayerName != layers[layers.Length - 1].name)
            {
                c.sortingLayerName = layers[layers.Length - 1].name;
                c.sortingOrder = Mathf.Min(32400, 31000 + c.sortingOrder);
            }
    }

    static RenderTexture rt;
    static Texture2D frame;

    static void Shot(string name)
    {
        if (!Wanted(name)) return;
        Camera cam = Camera.main;
        if (cam == null) { Debug.Log("[SHOT] no camera " + name); return; }
        rt = rt ?? new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        frame = frame ?? new Texture2D(W, H, TextureFormat.RGB24, false);
        cam.cullingMask |= 1 << 5;
        CanvasesToCamera(cam);
        RenderTexture old = cam.targetTexture;
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        cam.targetTexture = old;
        RenderTexture.active = rt;
        frame.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        frame.Apply();
        RenderTexture.active = null;
        string path = Path.Combine(Out, lang, name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, frame.EncodeToPNG());
        Debug.Log("[SHOT] " + lang + "/" + name);
    }

    static bool Wanted(string name)
    {
        if (string.IsNullOrEmpty(Only)) return true;
        foreach (string o in Only.Split(',')) if (o.Length > 0 && name.StartsWith(o)) return true;
        return false;
    }

    // ================================================================= 판 꾸미기
    static void SetGameLanguage(string l)
    {
        int i = l == "ko" ? 0 : l == "ja" ? 2 : l == "zh" ? 3 : 1;
        T("GameSettings").GetProperty("Language").SetValue(null, Enum.ToObject(T("Loc+Lang"), i));
    }

    static object Nullable(string type, string value)
    {
        Type t = T(type);
        return Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(t), Enum.Parse(t, value));
    }

    // 영웅 · 난이도를 정하고 게임 씬을 불러와 장으로 바로 이동
    static IEnumerator Load(string hero, int stage, string difficulty = "Normal")
    {
        T("CharacterData").GetField("Override", Any).SetValue(null, Nullable("CharacterId", hero));
        T("GameMode").GetField("Override", Any).SetValue(null, Nullable("Difficulty", difficulty));
        SceneManager.LoadScene("GameScene");
        yield return null; yield return null; yield return null;
        object shop = Find("LevelShop");
        for (int i = 0; i < 8 && shop != null; i++)
        {
            if ((bool)shop.GetType().GetProperty("IsOpen").GetValue(shop)) Call(shop, "CancelOpening");
            yield return null;
        }
        Time.timeScale = 1f;
        object sm = Find("StageManager");
        if (stage > 0) Call(sm, "JumpToStage", stage);
        if (shop != null) Call(shop, "RestoreLevels", 0, 0, 0f, 0f);
        yield return null;
    }

    // 카드를 여러 장 배운 상태 (실제 효과 · HUD 아이콘까지) — 스킬 진화 연출은 건너뜀
    static void Build(string hero, int level, int[] picks, int avoidA = -1, int avoidB = -1)
    {
        object shop = Find("LevelShop");
        object p = Find("PlayerController");
        if (shop == null || p == null) return;
        PropertyInfo replaying = T("RunSave").GetProperty("Replaying", Any);
        replaying.SetValue(null, true);
        int[] lv = (int[])Get(shop, "ability_level");
        object who = Enum.Parse(T("CharacterId"), hero);
        MethodInfo max = T("LevelShop").GetMethod("MaxLevelOf", Any);
        foreach (int id in picks)
        {
            if (id < 0 || id >= lv.Length || id == avoidA || id == avoidB) continue;
            if (lv[id] >= (int)max.Invoke(null, new object[] { who, id })) continue;
            try { Call(shop, "ReplayPick", id, false); } catch (Exception e) { Debug.Log("[SHOT] pick " + id + " " + e.InnerException?.Message); }
        }
        replaying.SetValue(null, false);
        Set(p, "level", level);
    }

    static void Dress(float minutes, int coins, int points)
    {
        object clock = Find("RunClock");
        if (clock != null) Set(clock, "start", Time.time - minutes * 60f);
        T("RunStats").GetField("Seconds", Any)?.SetValue(null, minutes * 60f);
        object coin = Find("Coin");
        if (coin != null) Call(coin, "AddCoin", coins - (int)Get(coin, "coins"));
        object pt = Find("Point");
        if (pt != null) { Set(pt, "points", points); Call(pt, "AddPoint", 0); }
    }

    static void Evolve(params int[] ids)
    {
        object sp = Get(Find("StageManager"), "specials");
        foreach (int id in ids) if (sp != null && id >= 0) Call(sp, "EvolveWeapon", id);
    }

    static int[] NextEvo()
    {
        object sp = Get(Find("StageManager"), "specials");
        return sp != null ? (int[])Call(sp, "NextEvolutionOptions") : null;
    }

    // 1차 · 2차 진화 (영웅마다 첫 선택지)
    static void EvolveTwice(int first = 0, int second = 0)
    {
        int[] o = NextEvo();
        if (o != null && o.Length > 0) Evolve(o[Mathf.Min(first, o.Length - 1)]);
        o = NextEvo();
        if (o != null && o.Length > 0) Evolve(o[Mathf.Min(second, o.Length - 1)]);
    }

    static object StageConfig(int stage)
    {
        object sp = Get(Find("StageManager"), "spawner");
        Array stages = sp != null ? (Array)Get(sp, "stages") : null;
        return stages != null && stage < stages.Length ? stages.GetValue(stage) : null;
    }

    // 주인공 둘레에 그 장 적을 깔아 둠 (멀리서 몰려오게)
    static void Swarm(int stage, int count, float rmin = 7f, float rmax = 13f, float arc = 360f, float facing = 0f, float tough = 6f)
    {
        object data = Resources.Load("CodexData");
        var enemies = (GameObject[])Get(data, "enemies");
        var stages = (int[])Get(data, "enemyStage");
        var pool = new List<GameObject>();
        for (int i = 0; i < enemies.Length; i++) if (stages[i] == stage) pool.Add(enemies[i]);
        if (pool.Count == 0 && StageConfig(stage) is object cfg) pool.AddRange((GameObject[])Get(cfg, "enemies"));
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            float a = (facing + (i / (float)count - 0.5f) * arc) * Mathf.Deg2Rad + UnityEngine.Random.Range(-0.15f, 0.15f);
            Vector3 at = me + new Vector3(Mathf.Cos(a) * 1.4f, Mathf.Sin(a)) * UnityEngine.Random.Range(rmin, rmax);
            GameObject go = UnityEngine.Object.Instantiate(pool[i % pool.Count], at, Quaternion.identity);
            // 사진이 찍히기 전에 다 쓰러지지 않게 체력을 늘림 (Start 전에 넣어야 배율이 그대로 곱해짐)
            Component ec = go.GetComponent(T("EnermyController"));
            if (ec != null) Set(ec, "setEnemyHP", Mathf.RoundToInt(Convert.ToSingle(Get(ec, "setEnemyHP")) * tough));
        }
    }

    static object SpawnBoss(int stage, float hp, Vector3 offset)
    {
        object data = Resources.Load("CodexData");
        GameObject[] bosses = (GameObject[])Get(data, "bosses");
        GameObject prefab = stage < bosses.Length ? bosses[stage] : (GameObject)Get(StageConfig(stage), "bossPrefab");
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        GameObject go = UnityEngine.Object.Instantiate(prefab, me + offset, Quaternion.identity);
        object bar = Find("bossbar");
        bar.GetType().GetField("bossSpawn").SetValue(bar, true);
        object sp = Get(Find("StageManager"), "spawner");
        sp.GetType().GetField("bossSpawned", Any)?.SetValue(sp, true);
        object b = go.GetComponent(T("bosss"));
        if (b != null) Set(b, "EnemyHealth", Convert.ToSingle(Get(b, "setEnemyHP")) * hp);
        if (b != null && b.GetType().GetField("speed", Any) is FieldInfo speed) speed.SetValue(b, Convert.ChangeType(0, speed.FieldType));
        return b;
    }

    static IEnumerator Ult(float hold)
    {
        object g = Find("SkillGauge");
        if (g != null) g.GetType().GetField("SkillPoint").SetValue(g, 99999f);
        yield return null;
        In("AutoUlt", true);
        In("UltDownFrame", Time.frameCount + 1);
        yield return F(Mathf.Max(hold, 0.05f));
        In("AutoUlt", false);
        In("UltUpFrame", Time.frameCount + 1);
        yield return null;
    }

    static void Stop()
    {
        keep.autopilot = false;
        In("AutoMove", Vector2.zero);
        In("AutoFire", false);
    }

    // 카드 묶음 (캐릭터 전용 칸 0 3 6 7 12 13 · 공용 4 5 8 9 10 · 고유 15~24) — 최대 단계는 Build 가 지킴
    static readonly int[] Common = { 4, 4, 5, 5, 8, 8, 9, 9, 10 };
    static int[] Picks(params int[] ids)
    {
        var l = new List<int>();
        foreach (int id in ids) { l.Add(id); l.Add(id); l.Add(id); }
        l.AddRange(Common);
        return l.ToArray();
    }

    // ================================================================= 장면
    // 1~5) 거너 · 지하 묘역: 무기 진화 화면 → 전투 · 필살기 → 레벨업 카드 → 스킬 진화 → 영혼 트리
    static IEnumerator GunnerRun()
    {
        yield return Load("Gunner", 0);
        Build("Gunner", 21, Picks(0, 6, 7, 3, 12, 16, 17, 18), 13, 15);
        Dress(11.4f, 2847, 1290);
        object sm = Find("StageManager");
        object shop = Find("LevelShop");

        // 2) 레벨업 카드 (가운데 카드를 고른 순간)
        if (Wanted("02"))
        {
            Swarm(0, 18, 9f, 15f);
            yield return F(0.6f);
            Stop();
            keep.keepLevel = false;
            Call(shop, "openLevelShop");
            yield return F(1.0f);
            Call(shop, "onSecondButton");
            yield return F(0.6f);
            Shot("02_level_up_cards");
            Call(shop, "CancelOpening");
            keep.keepLevel = true;
            yield return F(0.6f);
        }

        // 3) 무기 진화 화면 (리치 왕을 쓰러뜨리면 나오는 3택)
        if (Wanted("03"))
        {
            Stop();
            ((MonoBehaviour)sm).StartCoroutine((IEnumerator)Call(sm, "Evolution", false));
            yield return Until(() => Find("WeaponEvolutionUI") is object ui && Get(ui, "confirmButton") is Component cb && cb.gameObject.activeSelf, 12f);
            object evo = Find("WeaponEvolutionUI");
            if (evo != null) Call(evo, "Select", 2);       // 쌍권총
            yield return Real(0.7f);
            Shot("03_weapon_evolution");
            if (evo != null) Set(evo, "confirmed", true);
            yield return Until(() => !(bool)Get(sm, "evolving"), 12f);     // 「무기가 진화했다」 알림이 끝날 때까지
            yield return F(0.5f);
        }
        else Evolve(2);
        Evolve(5);                                        // 2차: 연쇄 번개
        Time.timeScale = 1f;

        // 1) 오른쪽에서 벽처럼 몰려오는 적에게 사격 · 필살기
        if (Wanted("01"))
        {
            Swarm(0, 44, 9f, 16f, 150f, 0f, 12f);
            Swarm(0, 18, 10f, 15f, 120f, 180f, 12f);
            keep.autopilot = true;
            yield return F(1.6f);
            yield return Ult(0.4f);
            yield return F(0.25f);
            Shot("01_gunner_catacombs_horde");
            yield return F(1.0f);
        }

        // 4) 스킬 진화: 「천둥 장전」 = 장전 충격파(13) + 탄피 지뢰(15) + 섬광 장전(19), 마지막 재료를 고르는 순간 진화 연출
        if (Wanted("04"))
        {
            Stop();
            int[] lv = (int[])Get(shop, "ability_level");
            bool[] sel = (bool[])Get(shop, "ability_selected");
            object who = Enum.Parse(T("CharacterId"), "Gunner");
            MethodInfo max = T("LevelShop").GetMethod("MaxLevelOf", Any);
            int Mx(int id) => (int)max.Invoke(null, new object[] { who, id });
            lv[13] = Mx(13); sel[13] = true;
            lv[15] = Mx(15); sel[15] = true;
            lv[19] = Mx(19) - 1;
            keep.keepLevel = false;
            Call(shop, "openLevelShop");
            yield return F(0.8f);
            Set(shop, "selectReady", true);
            Set(shop, "pendingSlot", -1);
            Call(shop, "onSelect", 19);
            yield return Real(3.6f);
            Shot("04_skill_evolution");
            object ui = Find("SkillEvolutionUI");
            if (ui != null) Set(ui, "closeRequested", true);
            keep.keepLevel = true;
            yield return Until(() => Find("SkillEvolutionUI") == null, 8f);
            if ((bool)shop.GetType().GetProperty("IsOpen").GetValue(shop)) Call(shop, "CancelOpening");
            Time.timeScale = 1f;
            yield return F(0.5f);
        }

        // 5) 영혼 트리
        if (Wanted("05"))
        {
            Stop();
            T("SoulShards").GetMethod("Add").Invoke(null, new object[] { 4000, Vector3.zero, false });
            Call(sm, "OpenSoulTree");
            yield return F(0.8f);
            object specials = Get(sm, "specials");
            object tree = T("SoulTreeUI").GetProperty("Instance").GetValue(null);
            for (int i = 0; i < 16 && tree != null; i++)
            {
                object pick = null;
                foreach (object v in (IList)Get(tree, "views"))
                {
                    object n = Get(v, "node");
                    if (!(bool)Call(specials, "CanBuy", n)) continue;
                    if (pick == null) pick = v;
                    if (Convert.ToInt32(Get(n, "branch")) == i % 6) { pick = v; break; }
                }
                if (pick != null) Call(tree, "Buy", pick);
                yield return F(0.25f);
            }
            yield return F(0.6f);
            Shot("05_soul_tree");
            T("SoulTreeUI").GetMethod("Close").Invoke(null, null);
            yield return F(0.3f);
        }
    }

    // 체험판: 진화 전 거너 (체험판에서는 리치 왕을 쓰러뜨린 뒤에야 무기 진화) · 레벨업 카드
    static IEnumerator DemoRun()
    {
        if (!Wanted("demo_01") && !Wanted("demo_02")) yield break;
        yield return Load("Gunner", 0, "Easy");
        Build("Gunner", 14, new[] { 0, 0, 0, 6, 6, 7, 7, 3, 16, 16, 17, 4, 4, 5, 8, 9 }, 13, 15);
        Dress(7.6f, 1384, 520);
        Swarm(0, 40, 9f, 16f, 150f, 0f, 10f);
        Swarm(0, 16, 10f, 15f, 120f, 180f, 10f);
        keep.autopilot = true;
        yield return F(1.6f);
        yield return Ult(0.4f);
        yield return F(0.25f);
        Shot("demo_01_gunner_catacombs");
        yield return F(1.0f);
        if (Wanted("demo_02"))
        {
            Stop();
            object shop = Find("LevelShop");
            keep.keepLevel = false;
            Call(shop, "openLevelShop");
            yield return F(0.9f);
            Call(shop, "onSecondButton");
            yield return F(0.5f);
            Shot("demo_02_level_up_cards");
            Call(shop, "CancelOpening");
            keep.keepLevel = true;
        }
        Stop();
    }

    // 보스전 한 장면: 영웅 · 장 · 보스, 진화 · 카드로 꾸미고 싸우다 필살기 → 찍기
    static IEnumerator BossFight(string hero, int stage, int boss, string shot, float minutes, int[] picks, int level, float ultHold, Vector3 bossAt, int swarm = 26, bool evolve = true)
    {
        if (!Wanted(shot)) yield break;
        yield return Load(hero, stage);
        Build(hero, level, picks);
        if (evolve) EvolveTwice();
        Dress(minutes, 1500 + level * 90, 600 + level * 40);
        SpawnBoss(boss, 0.62f, bossAt);
        Swarm(stage, swarm, 8f, 14f, 200f, 180f);
        keep.autopilot = true;
        yield return F(2.2f);
        if (ultHold > 0f) yield return Ult(ultHold);
        yield return F(0.25f);
        Shot(shot);
        yield return F(0.3f);
        Stop();
    }

    // 보스 필살기 결계 (어두워진 맵 · 둥근 결계 안의 공격)
    static IEnumerator Barrier(string hero, int stage, string shot, float minutes, int[] picks, int level, bool evolve = true)
    {
        if (!Wanted(shot)) yield break;
        yield return Load(hero, stage);
        Build(hero, level, picks);
        if (evolve) EvolveTwice();
        Dress(minutes, 2100, 900);
        object b = SpawnBoss(stage, 0.7f, new Vector3(8f, 3.5f, 0f));
        keep.autopilot = true;
        yield return F(1.4f);
        Component bc = (Component)b;
        Component ult = bc.GetComponent(T("BossUltimate"));
        if (ult == null) ult = bc.gameObject.AddComponent(T("BossUltimate"));
        yield return null;
        ((MonoBehaviour)ult).StartCoroutine((IEnumerator)Call(ult, "Run"));
        yield return Until(() => Time.timeScale == 0f, 3f);
        yield return Until(() => Time.timeScale > 0f, 8f);   // 외침 화면(실제 시간) → 결계가 펼쳐지고
        yield return F(2.2f);                                // 공격이 쏟아지는 중
        Shot(shot);
        yield return F(0.3f);
        Stop();
    }

    // 4장 심연 · 거울의 군주가 지금 영웅의 필살기를 흉내
    static IEnumerator Mirror(string hero, string shot, int[] picks)
    {
        if (!Wanted(shot)) yield break;
        yield return Load(hero, 3);
        Build(hero, 27, picks);
        EvolveTwice();
        Dress(19.2f, 4210, 2380);
        Component b = (Component)SpawnBoss(3, 0.55f, new Vector3(8f, 3.5f, 0f));
        yield return null;
        Component skills = b != null ? b.GetComponent(T("BossSkills")) : null;
        if (skills != null) Set(skills, "next", Time.time + 999f);
        Swarm(3, 22, 8f, 14f, 200f, 180f);
        keep.autopilot = true;
        yield return F(1.6f);
        if (skills != null) ((MonoBehaviour)skills).StartCoroutine((IEnumerator)Call(skills, "Run", Call(skills, "MirrorUlt", true, true)));
        yield return F(1.1f);
        yield return Ult(0.5f);
        yield return F(0.6f);
        Shot(shot);
        yield return F(0.3f);
        Stop();
    }

    // 무한 모드: 25분을 버틴 판 · 최종 보스 「개발자」 강림
    static IEnumerator Endless(string hero, string shot, int[] picks)
    {
        if (!Wanted(shot)) yield break;
        yield return Load(hero, 0, "Endless");
        object shop = Find("LevelShop");
        yield return F(0.3f);
        for (int i = 0; i < 6; i++)
        {
            if (shop != null && (bool)shop.GetType().GetProperty("IsOpen").GetValue(shop)) Call(shop, "CancelOpening");
            yield return null;
        }
        if (shop != null) Call(shop, "RestoreLevels", 0, 0, 0f, 0f);
        Build(hero, 34, picks);
        EvolveTwice();
        T("GameMode").GetField("endlessStart", Any).SetValue(null, Time.time - (25f * 60f + 41f));
        Dress(25.7f, 6630, 3120);
        object e = T("EndlessMode").GetProperty("Instance").GetValue(null);
        keep.autopilot = true;
        Swarm(0, 30, 8f, 14f, 200f, 180f);
        if (e != null) ((MonoBehaviour)e).StartCoroutine((IEnumerator)Call(e, "SummonDeveloper"));
        yield return F(4.4f);
        // 개발자를 화면 안으로 (보스 출현 지점은 맵 위쪽 끝)
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(T("bosss"), FindObjectsSortMode.None))
            if ((bool)Get(o, "developer")) ((Component)o).transform.position = me + new Vector3(9f, 4f, 0f);
        yield return F(2.2f);
        yield return Ult(0.6f);
        yield return F(0.5f);
        Shot(shot);
        yield return F(0.3f);
        Stop();
        T("GameMode").GetField("Override", Any).SetValue(null, null);
    }

    // 메인 메뉴 화면: 영웅 아홉 명 · 스킨 상점
    static IEnumerator Menu(string button, string shot)
    {
        if (!Wanted(shot)) yield break;
        T("CharacterData").GetField("Override", Any).SetValue(null, null);
        T("GameMode").GetField("Override", Any).SetValue(null, null);
        SceneManager.LoadScene("MainMenu");
        yield return F(1.5f);
        GameObject b = GameObject.Find(button);
        b?.GetComponent<Button>()?.onClick.Invoke();
        yield return F(1.4f);
        Shot(shot);
        yield return F(0.2f);
    }

    static void Unlock()
    {
        // 촬영 복사본 전용 저장 (shoot.ps1 이 제품 이름을 바꿔 둠): 영웅 아홉 명 · 무한 모드 해금
        for (int i = 3; i <= 8; i++) SCall("Prefs", "SetInt", "char.unlocked." + i, 1);
        SCall("Prefs", "SetInt", "mode.unlocked", 2);
        SCall("Prefs", "SetInt", "AbilityHUDOpen", 1);
        SCall("Prefs", "Save");
    }

    [UnityTest, Timeout(3600000)]       // 기본 3분이면 4개 언어를 다 못 찍음
    public IEnumerator Shoot()
    {
        LogAssert.ignoreFailingMessages = true;
        Time.captureFramerate = Fps;
        In("Auto", true);
        AudioListener.volume = 0f;
        Unlock();
        T("GameSettings").GetProperty("DamageNumbers", Any)?.GetValue(null);
        T("GameSettings").GetField("damageNumbers", Any)?.SetValue(null, 2);   // 피해 숫자 끄기 (겹쳐 쌓이면 화면이 지저분)
        GameObject host = new GameObject("ShotKeeper");
        UnityEngine.Object.DontDestroyOnLoad(host);
        keep = host.AddComponent<Keeper>();

        foreach (string l in Langs)
        {
            lang = l.Trim();
            SetGameLanguage(lang);
            Debug.Log("[SHOT] lang " + lang);
            UnityEngine.Random.InitState(1234);

            yield return GunnerRun();
            yield return BossFight("Swordsman", 0, 0, "06_swordsman_lich_king", 13.8f, Picks(0, 6, 7, 3, 12, 15, 16, 17), 23, 0f, new Vector3(10f, 3f, 0f));
            yield return Barrier("Archer", 1, "07_archer_demon_lord_barrier", 16.5f, Picks(0, 6, 7, 3, 12, 15, 16, 18), 24);
            yield return BossFight("Cartographer", 2, 2, "08_cartographer_king_slime", 17.9f, Picks(0, 6, 7, 3, 12, 15, 16, 17), 25, 0f, new Vector3(10f, 3f, 0f));
            yield return Mirror("Ghost", "09_ghost_mirror_sovereign", Picks(0, 6, 7, 3, 12, 15, 16, 17));
            yield return Endless("Postman", "10_endless_developer", Picks(0, 6, 7, 3, 12, 13, 15, 16, 17, 18));
            yield return Menu("CharacterButton", "11_nine_heroes");
            yield return Menu("SkinButton", "12_skin_shop");
            // 체험판 전용 (1장 지하 묘역 · 거너 · 검사 · 진화 전)
            yield return DemoRun();
            yield return BossFight("Swordsman", 0, 0, "demo_06_swordsman_lich_king", 9.1f, new[] { 0, 0, 6, 6, 7, 3, 15, 16, 16, 17, 4, 5, 8, 9 }, 15, 0f, new Vector3(10f, 3f, 0f), 26, false);
            yield return Barrier("Gunner", 0, "demo_07_gunner_lich_barrier", 9.6f, new[] { 0, 0, 0, 6, 6, 7, 7, 3, 16, 16, 17, 4, 4, 5, 8, 9 }, 16, false);
        }

        Time.captureFramerate = 0;
        Time.timeScale = 1f;
        In("Auto", false);
        Debug.Log("[SHOT] done");
        yield return null;
    }
}
