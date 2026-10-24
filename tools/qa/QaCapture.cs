using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// QA 화면 확인 (tools/qa/qa.ps1 로 프로젝트 복사본에서 배치 실행 · 게임 코드와 컴파일 의존 없음 — 모두 리플렉션)
// 한 번의 Unity 실행에 여러 장면을 묶어 스크린샷(PNG) · 로그(예외 · 경고 · 프레임 시간 · 오브젝트 수)를 남김
// 장면 고르기: 환경 변수 QA_ONLY (쉼표 · 비우면 모두) · 결과: QA_OUT (qa_log.txt · *.png)
public class QaCapture
{
    static readonly string Out = Environment.GetEnvironmentVariable("QA_OUT") ?? "C:/Temp/SoulSaverQa";
    static readonly string[] Only = (Environment.GetEnvironmentVariable("QA_ONLY") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    // ================================================================= 리플렉션 도우미
    static Type T(string n) => Type.GetType(n + ", Assembly-CSharp");
    static object Get(object o, string f)
    {
        if (o == null) return null;
        Type t = o is Type st ? st : o.GetType();
        object inst = o is Type ? null : o;
        FieldInfo fi = t.GetField(f, Any);
        if (fi != null) return fi.GetValue(inst);
        PropertyInfo pi = t.GetProperty(f, Any);
        return pi != null ? pi.GetValue(inst) : null;
    }
    static void Set(object o, string f, object v)
    {
        Type t = o is Type st ? st : o.GetType();
        object inst = o is Type ? null : o;
        FieldInfo fi = t.GetField(f, Any);
        if (fi != null) { fi.SetValue(inst, v); return; }
        PropertyInfo pi = t.GetProperty(f, Any);
        if (pi != null) { (pi.GetSetMethod(true) ?? throw new Exception("읽기 전용 " + f)).Invoke(inst, new[] { v }); return; }
        throw new Exception("칸 없음 " + t.Name + "." + f);
    }
    static object Call(object o, string m, params object[] a)
    {
        Type t = o is Type st ? st : o.GetType();
        object inst = o is Type ? null : o;
        MethodInfo mi = t.GetMethods(Any).FirstOrDefault(x => x.Name == m && x.GetParameters().Length == a.Length);
        if (mi == null) throw new Exception("함수 없음 " + t.Name + "." + m + "/" + a.Length);
        return mi.Invoke(inst, a);
    }
    static object SGet(string type, string f) => Get(T(type), f);
    static void SSet(string type, string f, object v) => Set(T(type), f, v);
    static object SCall(string type, string m, params object[] a) => Call(T(type), m, a);
    static void In(string f, object v) => T("GameInput").GetField(f).SetValue(null, v);
    static UnityEngine.Object Find(string type) => T(type) == null ? null : UnityEngine.Object.FindFirstObjectByType(T(type), FindObjectsInactive.Exclude);
    static object Enm(string type, string name) => Enum.Parse(T(type), name);
    static object Nullable(string type, string name) => Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(T(type)), Enm(type, name));
    static float F(object o) => o == null ? 0f : Convert.ToSingle(o);

    // ================================================================= 기록
    static StreamWriter logFile;
    static string scene = "-";
    static readonly Dictionary<string, int> errCount = new Dictionary<string, int>(), warnCount = new Dictionary<string, int>();
    static readonly HashSet<string> errSeen = new HashSet<string>();

    static void Log(string s)
    {
        string line = "[" + scene + "] " + s;
        Debug.Log("[QA] " + line);
        logFile?.WriteLine(line);
        logFile?.Flush();
    }

    static void OnLog(string msg, string stack, LogType type)
    {
        if (msg.StartsWith("[QA]")) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            errCount[scene] = errCount.TryGetValue(scene, out int n) ? n + 1 : 1;
            string key = scene + "|" + msg.Split('\n')[0];
            if (errSeen.Add(key)) logFile?.WriteLine("[" + scene + "] !! " + type + ": " + msg.Split('\n')[0] + " @ " + (stack ?? "").Split('\n').FirstOrDefault(l => l.Contains(".cs")));
        }
        else if (type == LogType.Warning)
        {
            warnCount[scene] = warnCount.TryGetValue(scene, out int n) ? n + 1 : 1;
            string key = scene + "|W|" + msg.Split('\n')[0];
            if (errSeen.Add(key) && errSeen.Count < 400) logFile?.WriteLine("[" + scene + "] ! Warning: " + msg.Split('\n')[0]);
        }
    }

    // ================================================================= 스크린샷 (화면 캔버스를 카메라 캔버스로 · 원래 정렬 순서를 그대로 유지)
    static RenderTexture rt;
    static Texture2D frame;
    static readonly Dictionary<int, int> origOrder = new Dictionary<int, int>();
    static int shotN;

    static void PrepCanvases(Camera cam)
    {
        SortingLayer[] layers = SortingLayer.layers;
        string top = layers[layers.Length - 1].name;
        var list = new List<(Canvas c, int order)>();
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            int id = c.GetInstanceID();
            if (c.isRootCanvas)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) { origOrder[id] = c.sortingOrder; c.renderMode = RenderMode.ScreenSpaceCamera; }
                if (!origOrder.ContainsKey(id)) continue;           // 원래 카메라 · 월드 캔버스는 그대로
                c.worldCamera = cam;
                c.planeDistance = 5f;
                // 코드로 만든 캔버스는 Default 층인 것이 많음 → UI 전용 카메라만 그리게 UI 층으로 (본 카메라는 UI 층을 빼고 그림)
                //   (나중에 붙은 자식도 있어 촬영마다 다시)
                foreach (Transform t in c.GetComponentsInChildren<Transform>(true)) if (t.gameObject.layer != UiLayer) t.gameObject.layer = UiLayer;
                list.Add((c, origOrder[id]));
            }
            else if (c.overrideSorting)
            {
                Canvas root = c.rootCanvas;
                if (root == null || !(origOrder.ContainsKey(root.GetInstanceID()) || root.renderMode == RenderMode.ScreenSpaceOverlay)) continue;
                if (!origOrder.ContainsKey(id)) origOrder[id] = c.sortingOrder;
                list.Add((c, origOrder[id]));
            }
        }
        list.Sort((a, b) => a.order.CompareTo(b.order));
        for (int i = 0; i < list.Count; i++)
        {
            list[i].c.overrideSorting = list[i].c.isRootCanvas ? true : list[i].c.overrideSorting;
            list[i].c.sortingLayerName = top;
            list[i].c.sortingOrder = 30000 + i * 2;
        }
    }

    // 2026-10-24 가독성 1-K: 예전엔 화면 캔버스를 본 카메라에 붙여 한 번에 그려서, 정렬 32000 · 그리기 순서 4000 인 범위표시가 UI 를 덮음 (03 「글자 잘림」)
    //   → 본 카메라는 UI 층(5)을 빼고 그린 뒤, UI 전용 카메라(같은 자리 · 직교 크기 · 같은 RT · 깊이만 지움 · UI 층만)로 화면 캔버스를 그 위에 = 실제 게임(Overlay)과 같은 순서
    static Camera uiCam;
    const int UiLayer = 5;
    static Camera UiCam(Camera main)
    {
        if (uiCam == null)
        {
            GameObject go = new GameObject("QaUiCamera");
            UnityEngine.Object.DontDestroyOnLoad(go);
            uiCam = go.AddComponent<Camera>();
        }
        uiCam.CopyFrom(main);                      // 자리 · 직교 크기 · 깊이 범위까지 같게 (그리기 전에 다시 맞춤)
        uiCam.clearFlags = CameraClearFlags.Depth; // 색은 지우지 않음 (본 카메라 그림 위에) · 깊이만 지워 UI 가 월드 깊이에 가리지 않게
        uiCam.cullingMask = 1 << UiLayer;
        uiCam.depth = main.depth + 50f;
        uiCam.targetTexture = main.targetTexture;
        uiCam.enabled = true;                      // 꺼진 카메라에 붙은 화면 캔버스는 Overlay 로 되돌아갈 수 있어 켜 둠 (같은 RT 에만 그림)
        return uiCam;
    }

    static float shotZoom = 1f;
    static IEnumerator Shot(string name, int w = 1920, int h = 1080, bool settle = true)
    {
        Camera cam = Camera.main;
        if (cam == null) { Log("카메라 없음 " + name); yield break; }
        if (rt == null || rt.width != w || rt.height != h)
        {
            if (rt != null) { cam.targetTexture = null; if (uiCam != null) uiCam.targetTexture = null; rt.Release(); UnityEngine.Object.Destroy(rt); }
            rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            if (frame != null) UnityEngine.Object.Destroy(frame);
            frame = new Texture2D(w, h, TextureFormat.RGBA32, false);
            settle = true;
        }
        cam.targetTexture = rt;
        Camera ui = UiCam(cam);
        PrepCanvases(ui);
        if (settle) { yield return null; yield return null; }
        cam = Camera.main;
        if (cam == null) { Log("카메라 없음 " + name); yield break; }
        cam.targetTexture = rt;
        ui = UiCam(cam);
        PrepCanvases(ui);
        Canvas.ForceUpdateCanvases();
        float size0 = cam.orthographicSize;
        int mask0 = cam.cullingMask;
        if (shotZoom != 1f) cam.orthographicSize = size0 * shotZoom;
        cam.cullingMask = mask0 & ~(1 << UiLayer);
        ui.orthographicSize = cam.orthographicSize;
        cam.Render();
        ui.Render();
        cam.cullingMask = mask0;
        cam.orthographicSize = size0;
        RenderTexture.active = rt;
        frame.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        frame.Apply();
        RenderTexture.active = null;
        string file = (++shotN).ToString("000") + "_" + name + (w != 1920 || h != 1080 ? "_" + w + "x" + h : "") + ".png";
        File.WriteAllBytes(Path.Combine(Out, file), frame.EncodeToPNG());
        Log("찰칵 " + file);
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
    static IEnumerator Secs(float s)
    {
        float end = Time.time + s, realStart = Time.realtimeSinceStartup;
        bool warned = false;
        while (Time.time < end)
        {
            if (Time.realtimeSinceStartup - realStart > s * 3f + 8f)
            {
                if (!warned) { warned = true; Log("?? 시간이 흐르지 않음 timeScale " + Time.timeScale + " · 레벨업 창 " + (Find("LevelShop") is object ls && (bool)Get(ls, "IsOpen")) + " · 고르기 " + SGet("ChoiceUI", "Open") + " · 무기 진화 " + SGet("WeaponEvolutionUI", "Open") + " · 스킬 진화 " + SGet("SkillEvolutionUI", "Open") + " · 컷신 " + SGet("BossUltimate", "CutscenePlaying") + " · 이야기 " + SGet("StoryDirector", "Playing") + " · ESC " + SGet("ESCmenu", "IsOpen")); }
                if (Time.realtimeSinceStartup - realStart > s * 3f + 30f) { Log("?? 기다림 포기"); yield break; }
            }
            yield return null;
        }
    }
    static IEnumerator Real(float s) { float end = Time.realtimeSinceStartup + s; while (Time.realtimeSinceStartup < end) yield return null; }

    static void Lang(string l)
    {
        int i = l == "ko" ? 0 : l == "ja" ? 2 : l == "zh" ? 3 : 1;
        T("GameSettings").GetProperty("Language").SetValue(null, Enum.ToObject(T("Loc+Lang"), i));
    }
    static string L(string ko) => (string)SCall("Loc", "T", ko);

    // 화면에 그 글자를 담은 글이 보이는지 (토스트 · 알림 · 떠오르는 숫자)
    static List<string> TextsWith(string sub)
    {
        var r = new List<string>();
        foreach (TMP_Text t in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            if (t.isActiveAndEnabled && t.alpha > 0.05f && !string.IsNullOrEmpty(t.text) && t.text.Contains(sub)) r.Add(t.text.Length > 90 ? t.text.Substring(0, 90) : t.text);
        return r;
    }

    // ================================================================= 판 불러오기
    static IEnumerator LoadGame(string hero, string mode = "Endless", bool tough = true)
    {
        Time.timeScale = 1f;
        SSet("CharacterData", "Override", Nullable("CharacterId", hero));
        SSet("GameMode", "Override", Nullable("Difficulty", mode));
        SceneManager.LoadScene("GameScene");
        yield return Frames(4);
        for (int i = 0; i < 10; i++) { yield return null; CloseLevelShop(); }
        Time.timeScale = 1f;
        if (mode == "Endless") for (int i = 0; i < 30 && SGet("EndlessMode", "Instance") == null; i++) yield return null;
        pilot.Reset(tough);
        yield return Frames(10);
    }

    static object Player() => Find("PlayerController");
    static Vector3 PPos() => ((Component)Player()).transform.position;
    static object EM() => SGet("EndlessMode", "Instance");

    static void CloseLevelShop()
    {
        object shop = Find("LevelShop");
        if (shop != null && (bool)Get(shop, "IsOpen"))
        {
            string f = new[] { "first", "second", "third" }[UnityEngine.Random.Range(0, 3)];
            Set(shop, "pendingSlot", -1);
            Set(shop, "selectReady", true);
            Call(shop, "onSelect", Get(shop, f));
        }
    }

    static List<Component> Bosses()
    {
        var r = new List<Component>();
        foreach (object o in (IEnumerable)SGet("bosss", "Live")) if (o is Component c && c != null && !(bool)Get(c, "IsDead")) r.Add(c);
        return r;
    }

    // 무한 모드 차례대로 보스 (0 리치 왕 · 1 지옥의 군주 · 2 킹 슬라임 · 3 거울의 군주) — 내 옆에 세움
    static Component EndlessBoss(int turn, Vector3 offset)
    {
        object em = EM();
        var before = Bosses();
        Set(em, "bossTurn", turn);
        Call(em, "SpawnBoss");
        Component b = Bosses().FirstOrDefault(x => !before.Contains(x));
        if (b == null)
        {
            // Live 등록은 Start 에서 → 가장 최근 bosss
            foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(T("bosss"), FindObjectsSortMode.None))
                if (!before.Contains((Component)o)) b = (Component)o;
        }
        if (b != null) b.transform.position = PPos() + offset;
        Log("보스 등장 turn " + turn + " → " + (b != null ? b.name + " kind " + Get(b, "bossKind") : "없음"));
        return b;
    }

    static Component FinalBoss(bool mom, Vector3 offset)
    {
        object em = EM();
        Component b = (Component)Call(em, "SpawnFinalBoss", mom);
        if (b != null)
        {
            b.transform.position = PPos() + offset;
            Set(em, mom ? "mom" : "developer", b);
            Set(em, mom ? "momSummoned" : "devSummoned", true);
            Set(em, "finalStage", Math.Max(2, Convert.ToInt32(Get(em, "finalStage"))));
            Set(em, "finalMusic", mom ? 2 : 1);
        }
        return b;
    }

    static void HpFrac(Component b, float frac) => Set(b, "EnemyHealth", F(Get(b, "setEnemyHP")) * frac);
    static float HpOf(Component b) => F(Get(b, "EnemyHealth")) / Mathf.Max(1f, F(Get(b, "setEnemyHP")));

    static void Kill(Component b)
    {
        if (b == null) return;
        float hp = Mathf.Max(1f, F(Get(b, "EnemyHealth")));
        Set(b, "invulnerable", false);
        Call(b, "TakeDamage", hp * 20f + 100f, 0f, Vector3.zero);
        if (!(bool)Get(b, "IsDead")) { Set(b, "EnemyHealth", 0.01f); Call(b, "TakeDamage", hp * 20f + 100f, 0f, Vector3.zero); }
    }

    // 킹 슬라임처럼 갈라지는 보스까지 그 종류가 다 쓰러질 때까지
    static IEnumerator KillKind(int kind)
    {
        for (int i = 0; i < 40; i++)
        {
            var list = Bosses().Where(x => Convert.ToInt32(Get(x, "bossKind")) == kind && !(bool)Get(x, "developer")).ToList();
            if (list.Count == 0) yield break;
            foreach (var b in list) Kill(b);
            yield return Secs(0.4f);
        }
    }

    static void EndlessTime(float sec) => SCall("GameMode", "StartEndlessClock", sec);

    static void MusicLog(string tag)
    {
        var parts = new List<string>();
        foreach (AudioSource a in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (a.isPlaying && a.clip != null && a.clip.length > 15f) parts.Add(a.clip.name + "@" + a.volume.ToString("0.00"));
        Log("곡 " + tag + ": " + string.Join(", ", parts));
    }

    static float[] UltGauge() => (float[])SGet("BossUltimate", "gauge");

    // ================================================================= 자동 조종 · 피해 기록 · 창 자동 닫기
    static Pilot pilot;

    public enum Move { Stand, Ranged, Melee, Flank, Eye }

    public class Pilot : MonoBehaviour
    {
        public bool on, fire = true, autoCards = true, autoEvo = true, autoChoice = true, refill = true, ult;
        public Move move = Move.Ranged;
        public Component target;              // 비우면 가까운 보스 → 적
        public float nominalMax = 100f;       // 피해 비율 기준 (게임의 최대 체력)
        public readonly List<(float t, float dmg)> hits = new List<(float, float)>();
        float lastHp = -1f, nextTap, nextUlt;
        bool held, ultHeld;
        public float lastFrameDt;

        public void Reset(bool tough)
        {
            on = false; fire = true; move = Move.Ranged; target = null; ult = false;
            autoCards = autoEvo = autoChoice = refill = true;
            hits.Clear();
            lastHp = -1f;
            object p = Player();
            if (p == null) return;
            nominalMax = F(Get(p, "PlayerMaxHealth"));
            if (tough)
            {
                p.GetType().GetField("PlayerMaxHealth").SetValue(p, 100000f);
                p.GetType().GetField("PlayerHealth").SetValue(p, 100000f);
            }
        }

        Vector3? Target(Vector3 me, out bool boss)
        {
            boss = true;
            if (target != null && target.gameObject.activeInHierarchy) return Body(target);
            var bs = Bosses();
            if (bs.Count > 0) return Body(bs.OrderBy(b => (b.transform.position - me).sqrMagnitude).First());
            boss = false;
            Type et = T("EnermyController");
            float best = 1e9f; Vector3? at = null;
            foreach (object o in (IEnumerable)Get(et, "Live"))
                if (o is Component c && c != null) { float d = (c.transform.position - me).sqrMagnitude; if (d < best) { best = d; at = c.transform.position; } }
            return at;
        }

        // 몸 가운데 (히트박스 · 그림 가운데 — 피벗이 발밑인 큰 보스도 맞게)
        static Vector3 Body(Component c)
        {
            Collider2D col = c.GetComponent<Collider2D>();
            if (col != null && col.enabled) { Vector3 v = col.bounds.center; v.z = 0f; return v; }
            SpriteRenderer sr = c.GetComponent<SpriteRenderer>();
            if (sr != null) { Vector3 v = sr.bounds.center; v.z = 0f; return v; }
            return c.transform.position;
        }

        void Update()
        {
            if (!on) return;
            Component p = (Component)Player();
            if (p == null) return;
            Vector3 me = p.transform.position;
            Vector3? tg = Target(me, out bool boss);
            Vector2 mv = Vector2.zero;
            if (tg.HasValue)
            {
                Vector2 to = tg.Value - me;
                float d = to.magnitude;
                Vector2 side = new Vector2(-to.y, to.x).normalized;
                switch (move)
                {
                    case Move.Ranged:
                        mv = side * 0.75f + (d < (boss ? 8f : 5f) ? -to.normalized * 1.2f : d > (boss ? 11f : 8f) ? to.normalized * 0.8f : Vector2.zero);
                        break;
                    case Move.Melee:
                        mv = side * 0.5f + (d > 2.2f ? to.normalized : d < 1.2f ? -to.normalized * 0.5f : Vector2.zero);
                        break;
                    case Move.Flank:
                        {
                            Vector3 want = tg.Value;
                            Component b = target ?? Bosses().FirstOrDefault();
                            object mem = b != null ? Get(b, "memory") : null;
                            if (mem != null && (bool)Get(mem, "shield"))
                            {
                                Vector2 sd = (Vector2)Get(mem, "shieldDir");
                                Vector2 back = Quaternion.Euler(0, 0, 150f) * sd;
                                want = tg.Value + (Vector3)(back * 2.2f);
                            }
                            else want = tg.Value + (Vector3)(-to.normalized * 2f);
                            Vector2 go = want - me;
                            mv = go.magnitude > 0.3f ? go.normalized : Vector2.zero;
                        }
                        break;
                    case Move.Eye:
                        if (SGet("DomainStrikes", "EyeOn") is bool eo && eo)
                        {
                            Vector2 go = (Vector3)SGet("DomainStrikes", "EyePos") - me;
                            mv = go.magnitude > 0.4f ? go.normalized : Vector2.zero;
                        }
                        else mv = side * 0.75f;
                        break;
                }
            }
            In("AutoMove", mv);
            In("AutoAim", tg ?? me + Vector3.right * 5f);
            if (fire && Time.time >= nextTap)
            {
                held = !held;
                if (held) { In("FireDownFrame", Time.frameCount + 1); In("AutoFire", true); nextTap = Time.time + 0.42f; }
                else { In("FireUpFrame", Time.frameCount + 1); In("AutoFire", false); nextTap = Time.time + 0.06f; }
            }
            if (ult && Time.time >= nextUlt)
            {
                ultHeld = !ultHeld;
                if (ultHeld) { In("AutoUlt", true); In("UltDownFrame", Time.frameCount + 1); nextUlt = Time.time + 0.4f; }
                else { In("AutoUlt", false); In("UltUpFrame", Time.frameCount + 1); nextUlt = Time.time + 3f; }
            }
        }

        void LateUpdate()
        {
            lastFrameDt = Time.unscaledDeltaTime;
            object p = Player();
            if (p != null)
            {
                float hp = F(Get(p, "PlayerHealth")), max = F(Get(p, "PlayerMaxHealth"));
                if (lastHp >= 0f && hp < lastHp - 0.01f) hits.Add((Time.time, lastHp - hp));
                if (refill && hp < max) { p.GetType().GetField("PlayerHealth").SetValue(p, max); hp = max; }
                lastHp = hp;
            }
            if (autoCards)
            {
                object shop = Find("LevelShop");
                if (shop != null)
                {
                    if ((bool)Get(shop, "IsOpen")) CloseLevelShop();
                    else if ((bool)Get(shop, "CanOpenPending") && !(bool)SGet("ESCmenu", "IsOpen")) { try { Call(shop, "OpenPending"); } catch { } }
                }
            }
            if (autoEvo)
            {
                object we = Find("WeaponEvolutionUI");
                if (we != null) { try { Call(we, "Select", 0); Set(we, "confirmed", true); } catch { } }
                object se = Find("SkillEvolutionUI");
                if (se != null) { try { Set(se, "closeRequested", true); } catch { } }
            }
            if (autoChoice)
            {
                object ch = Find("ChoiceUI");
                if (ch != null && Get(ch, "ready") is bool r && r) { try { if (Convert.ToInt32(Get(ch, "confirmed")) < 0) Set(ch, "confirmed", 0); } catch { } }
            }
        }
    }

    // ================================================================= 장면 실행 (장면마다 예외를 삼키고 다음으로)
    static IEnumerator Safe(string name, Func<IEnumerator> make, float maxReal)
    {
        if (Only.Length > 0 && !Only.Contains(name)) yield break;
        scene = name;
        float start = Time.realtimeSinceStartup;
        Log("==== 시작");
        var stack = new Stack<IEnumerator>();
        try { stack.Push(make()); } catch (Exception e) { Log("XX 시작 실패 " + e); yield break; }
        while (stack.Count > 0)
        {
            if (Time.realtimeSinceStartup - start > maxReal) { Log("XX 시간 초과 " + maxReal + "초"); break; }
            object cur;
            bool moved;
            try { moved = stack.Peek().MoveNext(); cur = moved ? stack.Peek().Current : null; }
            catch (Exception e) { Log("XX 예외: " + (e.InnerException ?? e).GetType().Name + " " + (e.InnerException ?? e).Message + "\n" + (e.InnerException ?? e).StackTrace); break; }
            if (!moved) { stack.Pop(); continue; }
            if (cur is IEnumerator sub) { stack.Push(sub); continue; }
            yield return cur;
        }
        Time.timeScale = 1f;
        try { SSet("EnermyController", "GlobalSpeedMultiplier", 1f); SSet("DamageSource", "Current", null); } catch { }      // readability 가 멈춰 둔 적 · 피해 출처 (예외로 끊겨도)
        if (pilot != null) pilot.on = false;
        In("AutoMove", Vector2.zero); In("AutoFire", false);
        Log("==== 끝 (" + (Time.realtimeSinceStartup - start).ToString("0") + "초 · 오류 " + (errCount.TryGetValue(name, out int ec) ? ec : 0) + " · 경고 " + (warnCount.TryGetValue(name, out int wc) ? wc : 0) + ")");
    }

    [UnityTest, Timeout(int.MaxValue)]
    public IEnumerator Run()
    {
        LogAssert.ignoreFailingMessages = true;
        Directory.CreateDirectory(Out);
        logFile = new StreamWriter(Path.Combine(Out, "qa_log.txt"), false, new UTF8Encoding(false));
        Application.logMessageReceived += OnLog;
        AudioListener.volume = 0f;
        In("Auto", true);
        GameObject host = new GameObject("QaPilot");
        UnityEngine.Object.DontDestroyOnLoad(host);
        pilot = host.AddComponent<Pilot>();
        Lang("ko");
        Log("QA 시작 " + DateTime.Now);
        // 무한 모드 시계(GameMode.endlessStart = Time.time - 경과)는 0 아래를 받지 않음 → 15분 장면을 위해 Time.time 을 1000초 넘게 미리 흘려 둠
        if (Only.Length == 0 || Only.Any(o => o == "final_dodge" || o == "system1" || o == "system2"))
        {
            Time.timeScale = 100f;
            float until = Time.realtimeSinceStartup + 60f;
            while (Time.time < 1000f && Time.realtimeSinceStartup < until) yield return null;
            Time.timeScale = 1f;
            Log("Time.time 미리 흘림 → " + Time.time.ToString("0"));
        }

        yield return Safe("map_overview", MapOverview, 300);
        yield return Safe("readability", Readability, 240);
        yield return Safe("memory_bar", MemoryBar, 240);
        yield return Safe("terrain", TerrainAndErosion, 420);
        yield return Safe("erosion_pending", ErosionPending, 180);
        yield return Safe("final_dodge", FinalDodge, 300);
        yield return Safe("system1", System1, 240);
        yield return Safe("mom_domain", MomDomain, 300);
        yield return Safe("dev_domain", DevDomain, 200);
        yield return Safe("own_attacks", OwnAttacks, 300);
        yield return Safe("mem_shield", MemShield, 300);
        yield return Safe("ttk", Ttk, 600);
        yield return Safe("reward_slot", RewardSlot, 240);
        yield return Safe("save_resume", SaveResume, 420);
        yield return Safe("user_resume", UserResume, 300);
        yield return Safe("ddol_canvas", DdolCanvas, 240);
        yield return Safe("loading", LoadingScreen, 120);
        yield return Safe("fire_ground", FireGroundShot, 180);
        yield return Safe("skin_fx", SkinFxShot, 180);
        yield return Safe("system2", System2, 240);
        yield return Safe("soak", Soak, 600);
        yield return Safe("ui_menu", UiMenu, 900);

        scene = "-";
        Log("QA 끝 · 오류 합계 " + errCount.Values.Sum() + " · 경고 합계 " + warnCount.Values.Sum());
        Application.logMessageReceived -= OnLog;
        In("Auto", false);
        logFile.Close();
        logFile = null;
        yield return null;
    }

    // ================================================================= 0) 가독성 (2026-10-24 그래픽 가독성 개편 1-K · tools/qa/luma_ab.py 가 qa_log 의 「명도 · 밀도 · 내이펙트」 줄을 읽어 6-3 기준 판정)
    // 거너 · 시드 고정 · 무한 사막 / 초원 영토 / 이야기 묘역 / 심연 — 적 24마리를 플레이어 오른쪽 3칸에 멈춰 세움
    //   read_<곳>_clean: 내 이펙트 없이 · read_<곳>: 화염 지대(무리를 계속 때림) · 적 원형 예고 3 · 결계 땅 타격 1 · 큰 알림 1 + (0.85초에) 내 폭발 2 → 1.0초 뒤 멈춰 찍음
    //   read_<곳>_nofx: 바로 그 순간 내 이펙트만 끈 판 (5번 기준 「이펙트가 적을 덮는지」 비교용 · 명도 줄 없음)
    const int ReadSeed = 20261024;
    const float ReadFreeze = 1e-4f;
    static readonly Dictionary<string, float> readClean = new Dictionary<string, float>();
    static readonly Dictionary<string, float> readNoFx = new Dictionary<string, float>();

    // 내 이펙트 렌더러: 화염 지대 + 바닥 · 효과 층의 fx_ 그림 (적 경고는 Warning 층이라 빠짐 · 피해 숫자 · 적 · 플레이어는 그대로)
    static List<Renderer> MyFxRenderers(Component zone)
    {
        var list = new List<Renderer>();
        if (zone != null) list.AddRange(zone.GetComponentsInChildren<Renderer>());
        foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || list.Contains(r) || !r.name.StartsWith("fx_")) continue;
            string layer = r.sortingLayerName;
            if (layer == "GroundFx" || layer == "Effect") list.Add(r);
        }
        return list.Where(r => r.enabled).ToList();
    }

    // 적 몸 흑백 Y: 렌더러 화면 상자 가운데 50% 평균 — all = 전체 평균 · hit = (zoneR > 0) 화염 지대 안 평균 · n · nHit
    static (float all, float hit, int n, int nHit) EnemyBodyY(Camera cam, Texture2D tex, Vector3 zoneC, float zoneR)
    {
        if (cam == null || tex == null) return (-1f, -1f, 0, 0);
        int w = tex.width, h = tex.height;
        Color32[] px = tex.GetPixels32();
        var body = new List<float>(); var bodyHit = new List<float>();
        foreach (object o in (IEnumerable)SGet("EnermyController", "Live"))
        {
            if (!(o is Component ec) || ec == null || !ec.gameObject.activeInHierarchy) continue;
            SpriteRenderer sr = ec.GetComponent<SpriteRenderer>();
            if (sr == null || !sr.enabled) continue;
            Bounds bb = sr.bounds;
            Vector2 a = cam.WorldToScreenPoint(bb.min), b = cam.WorldToScreenPoint(bb.max);
            float cx = (a.x + b.x) * 0.5f, cy = (a.y + b.y) * 0.5f, hw = Mathf.Abs(b.x - a.x) * 0.25f, hh = Mathf.Abs(b.y - a.y) * 0.25f;
            int x0 = Mathf.Max(0, Mathf.RoundToInt(cx - hw)), x1 = Mathf.Min(w - 1, Mathf.RoundToInt(cx + hw)), y0 = Mathf.Max(0, Mathf.RoundToInt(cy - hh)), y1 = Mathf.Min(h - 1, Mathf.RoundToInt(cy + hh));
            if (x1 < x0 || y1 < y0) continue;
            float s = 0f; int n = 0;
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) { s += Y601(px[y * w + x]); n++; }
            if (n == 0) continue;
            body.Add(s / n);
            if (zoneR > 0f && Vector2.Distance(ec.transform.position, zoneC) <= zoneR + 0.3f) bodyHit.Add(s / n);
        }
        return (body.Count > 0 ? body.Average() : -1f, bodyHit.Count > 0 ? bodyHit.Average() : -1f, body.Count, bodyHit.Count);
    }

    static IEnumerator Readability()
    {
        foreach (string place in new[] { "desert", "meadow", "cave", "abyss" })
        {
            readClean.Remove(place); readNoFx.Remove(place);
            UnityEngine.Random.InitState(ReadSeed);
            yield return LoadGame("Gunner", place == "desert" || place == "meadow" ? "Endless" : "Normal");
            UnityEngine.Random.InitState(ReadSeed);
            pilot.on = false;
            In("AutoMove", Vector2.zero); In("AutoFire", false);
            object sp = Find("EnemySpawner");
            if (sp != null) Set(sp, "spawningEnabled", false);
            if (place == "abyss")
            {
                Call(SGet("StageManager", "Instance"), "JumpToStage", 3);
                yield return Frames(6);
            }
            else if (place == "meadow")
            {
                // 킹 슬라임(종류 2) 처치 = 초원 영토 (보스를 실제로 부르지 않고 지형 쪽에 처치만 알림)
                object et = SGet("EndlessTerrain", "Instance");
                if (et == null) { Log("XX EndlessTerrain 없음 · 초원 건너뜀"); continue; }
                SCall("EndlessTerrain", "NoteBossDown", 2, PPos());
                Call(et, "OnBossDefeated", 1, 0);
                yield return Frames(3);
                for (int i = 0; i < 80 && ((bool)Get(et, "busy") || ((ICollection)Get(et, "pendings")).Count > 0); i++) yield return Secs(0.25f);
                yield return Secs(1f);
                Vector3 bc = (Vector3)Get(et, "blobC");
                ((Component)Player()).transform.position = bc + new Vector3(-1.5f, 0f, 0f);
                Log("초원 영토 중심 " + bc + " · 내 자리 영토 " + Call(et, "RealmAt", PPos()) + " · 무리 자리 영토 " + Call(et, "RealmAt", PPos() + new Vector3(3f, 0f, 0f)) + " (3 = 초원)");
            }
            foreach (Component e in UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None).Cast<Component>()) e.gameObject.SetActive(false);
            yield return Frames(2);

            // ---- 적 24마리 (지금 장의 적 목록에서 차례대로) · 멈춤 · 접촉 피해 0 · 죽지 않게
            Vector3 me = PPos();
            Vector3 pack = me + new Vector3(3f, 0f, 0f);
            GameObject[] pool = sp != null && Get(Get(sp, "Stage"), "enemies") is GameObject[] st ? st.Where(g => g != null).ToArray() : new GameObject[0];
            if (pool.Length == 0) pool = ((GameObject[])Get(Resources.Load("CodexData"), "enemies")).Where(g => g != null).Take(8).ToArray();
            SSet("EnermyController", "GlobalSpeedMultiplier", 0f);
            var pack24 = new List<Component>();
            for (int i = 0; i < 24; i++)
            {
                float a = i * 2.39996f, r = 0.35f * Mathf.Sqrt(i + 0.5f);        // 해바라기 배치 (반지름 약 1.7 · 겹침 적게)
                GameObject go = UnityEngine.Object.Instantiate(pool[i % pool.Length], pack + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f), Quaternion.identity);
                Component ec = go.GetComponent(T("EnermyController"));
                if (ec == null) continue;
                Set(ec, "dashInterval", 0f);
                Set(ec, "separationSpeed", 0f);
                Set(ec, "contactDamage", 0f);
                pack24.Add(ec);
            }
            yield return Frames(3);
            foreach (Component ec in pack24)
            {
                if (ec == null) continue;
                Set(ec, "EnemyHealth", 1e7f);
                if (T("EnemySkill") != null && ec.GetComponent(T("EnemySkill")) is Behaviour skb) skb.enabled = false;
            }
            Log("무리 " + pack24.Count + "마리 (" + string.Join(",", pool.Select(g => g.name).Distinct().Take(8)) + ") · 플레이어 " + me);
            yield return Secs(1.5f);         // 등장 고리가 사라질 때까지

            // ---- 내 이펙트 없이
            Time.timeScale = 0f;
            yield return Shot("read_" + place + "_clean");
            ReadMeasure("read_" + place + "_clean", place, null, Vector3.zero, 0f, null);
            Time.timeScale = 1f;

            // ---- 이펙트 · 경고 · 알림
            Color danger = new Color(1f, 0.25f, 0.2f, 0.9f);
            Vector3[] warnAt = { me + new Vector3(-4f, 2.5f, 0f), me + new Vector3(-3.5f, -3f, 0f), me + new Vector3(0.5f, 4.5f, 0f) };
            float[] warnR = { 1.6f, 2.0f, 1.4f };
            for (int i = 0; i < 3; i++) Call(T("Hostile"), "Circle", warnAt[i], warnR[i], 2.6f, danger);
            Vector3 strikeAt = me + new Vector3(-1f, -5.5f, 0f);
            object ctx = Activator.CreateInstance(T("DomainCtx"));
            Set(ctx, "center", me);
            Set(ctx, "radius", 35f);
            Set(ctx, "color", new Color(1f, 0.45f, 0.15f));
            Call(ctx, "Strike", strikeAt, 2.6f, 2.6f, new Color(1f, 0.45f, 0.15f), 0f, "fx_meteor", false);
            SCall("Notice", "Big", L("분노!"), Enm("NoticeIcon", "Boss"), 2.5f, Enm("NoticeLevel", "Urgent"), null, null);
            object zoneSrc = SGet("DamageSource", "Current");
            SSet("DamageSource", "Current", (string)SGet("DamageSource", "Special"));      // 내 것 (Fx.Mine · 화염 지대의 피해 출처)
            object sa = T("SpecialAbilities").GetProperty("SharedInstance").GetValue(null);
            Component zone = sa != null ? Call(sa, "SpawnZone", pack, 2.2f, 8f, 1f, new Color(1f, 0.45f, 0.1f, 0.8f)) as Component : null;
            SSet("DamageSource", "Current", zoneSrc);
            yield return Secs(0.85f);
            SSet("DamageSource", "Current", (string)SGet("DamageSource", "Special"));
            Vector3[] boomAt = { pack + new Vector3(-0.9f, 0.7f, 0f), pack + new Vector3(1.1f, -0.6f, 0f) };
            foreach (Vector3 b in boomAt)
            {
                SCall("Fx", "Spawn", "fx_explosion", b, 3.2f, (Color?)new Color(1f, 0.75f, 0.35f), 18f, 0f, 12, false, -1f, "Effect");
                SCall("CharacterKit", "DamageCircle", b, 1.6f, 1f, 0f, 0f);
            }
            SSet("DamageSource", "Current", zoneSrc);
            yield return Secs(0.15f);
            // 2026-10-25~ 0 이 아닌 아주 작은 배속으로 멈춤 — timeScale 0 이면 Notice.Held 가 큰 알림 띠를 숨겨(알파 0) 「분노!」 띠가 사진에 없었음
            Time.timeScale = ReadFreeze;
            // 같은 순간에 내 이펙트(화염 지대 · fx_ 그림)만 끈 판 → 적 몸 평균 (5번 기준의 비교 대상)
            //   예전엔 맞기 전의 깨끗한 판과 비교해 피격 꼬리 어둡게(HitDim 0.78 · 의도된 연출)가 「이펙트가 적을 덮음」으로 섞였음 (18~25%)
            var mine = MyFxRenderers(zone);
            foreach (Renderer r in mine) r.enabled = false;
            yield return Shot("read_" + place + "_nofx", 1920, 1080, false);
            readNoFx[place] = EnemyBodyY(Camera.main, frame, Vector3.zero, 0f).all;
            foreach (Renderer r in mine) if (r != null) r.enabled = true;
            yield return Shot("read_" + place);
            ReadMeasure("read_" + place, place, zone, pack, 2.2f, warnAt.Select((p, i) => (p, warnR[i])).Append((strikeAt, 2.6f)).ToArray());
            Time.timeScale = 1f;
            SSet("EnermyController", "GlobalSpeedMultiplier", 1f);
        }
    }

    static float Y601(Color32 c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
    static string F1(float v) => v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

    // 마지막 촬영(frame)에서 명도 숫자 → qa_log 「명도 <촬영> 키=값 …」 (luma_ab.py 가 읽음 · 값 목록은 쉼표, 없으면 -)
    static void ReadMeasure(string shot, string place, Component zone, Vector3 zoneC, float zoneR, (Vector3 p, float r)[] warns)
    {
        Camera cam = Camera.main;
        if (cam == null || frame == null) return;
        int w = frame.width, h = frame.height;
        Color32[] px = frame.GetPixels32();
        float Yat(int x, int y) => Y601(px[Mathf.Clamp(y, 0, h - 1) * w + Mathf.Clamp(x, 0, w - 1)]);
        Vector2 Scr(Vector3 wp) => cam.WorldToScreenPoint(wp);
        float pxPerUnit = h / (2f * cam.orthographicSize);
        var sb = new StringBuilder("명도 " + shot);

        // 화면에 보이는 렌더러 (타일맵 · 화면을 거의 덮는 큰 바탕은 빼고) · 화면 UI 그림
        var rends = new List<Renderer>();
        foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r is UnityEngine.Tilemaps.TilemapRenderer) continue;
            if (r.gameObject.layer == UiLayer) continue;
            Vector3 s = r.bounds.size;
            if (s.x > 30f || s.y > 30f) continue;
            if (r is SpriteRenderer sr0 && (sr0.sprite == null || sr0.color.a < 0.02f)) continue;
            rends.Add(r);
        }
        var uiRects = new List<Rect>();
        foreach (UnityEngine.UI.Graphic g in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
        {
            if (!g.isActiveAndEnabled || g.color.a < 0.02f || g.canvas == null || g.canvas.renderMode == RenderMode.WorldSpace) continue;
            Vector3[] c4 = new Vector3[4];
            g.rectTransform.GetWorldCorners(c4);
            Camera gc = g.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : g.canvas.worldCamera;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(gc, c4[0]), b = RectTransformUtility.WorldToScreenPoint(gc, c4[2]);
            Rect rc = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            if (rc.width * rc.height > w * h * 0.5f) continue;          // 화면을 덮는 투명 판 · 비네트
            uiRects.Add(rc);
        }

        // 1) 바닥: 렌더러 · UI 가 없는 점 64곳 (화면 가운데 70% · 8×8 상자 평균 · 표준편차)
        var floorMeans = new List<float>(); var floorStd = new List<float>();
        for (int gy = 0; gy < 12 && floorMeans.Count < 64; gy++)
            for (int gx = 0; gx < 16 && floorMeans.Count < 64; gx++)
            {
                int sx = Mathf.RoundToInt(w * (0.15f + 0.7f * (gx + 0.5f) / 16f)), sy = Mathf.RoundToInt(h * (0.15f + 0.7f * ((gy * 7 + gx) % 12 + 0.5f) / 12f));
                Vector3 wp = cam.ScreenToWorldPoint(new Vector3(sx, sy, -cam.transform.position.z));
                bool hit = false;
                foreach (Renderer r in rends) { Bounds bb = r.bounds; bb.Expand(0.5f); wp.z = bb.center.z; if (bb.Contains(wp)) { hit = true; break; } }
                if (!hit) foreach (Rect rc in uiRects) if (rc.Overlaps(new Rect(sx - 6, sy - 6, 12, 12))) { hit = true; break; }
                if (hit) continue;
                float sum = 0f, sq = 0f;
                for (int dy = -4; dy < 4; dy++) for (int dx = -4; dx < 4; dx++) { float y = Yat(sx + dx, sy + dy); sum += y; sq += y * y; }
                float m = sum / 64f;
                floorMeans.Add(m); floorStd.Add(Mathf.Sqrt(Mathf.Max(0f, sq / 64f - m * m)));
            }
        float floor = floorMeans.Count > 0 ? floorMeans.Average() : -1f;
        sb.Append(" 바닥=" + F1(floor) + " 바닥편차=" + F1(floorStd.Count > 0 ? floorStd.Average() : -1f) + " 바닥점=" + floorMeans.Count);

        // 2) 적 몸: 렌더러 화면 상자 가운데 50% 평균 (화염 지대 안 = 맞는 중)
        var bodyY = EnemyBodyY(cam, frame, zoneC, zoneR);
        float bodyMean = bodyY.all;
        sb.Append(" 적=" + F1(bodyMean) + " 적수=" + bodyY.n + " 맞는적=" + (bodyY.nHit > 0 ? F1(bodyY.hit) : "-") + " 맞는수=" + bodyY.nHit);

        // 3) 플레이어: 그림 상자 + 4화소 안의 밝은 쪽 10% (= 밝은 외곽 띠) · 화면 전체 상위 1% · 2%
        var all = new float[px.Length];
        for (int i = 0; i < px.Length; i++) all[i] = Y601(px[i]);
        float[] sorted = (float[])all.Clone();
        Array.Sort(sorted);
        float p99 = sorted[(int)(sorted.Length * 0.99f)], p98 = sorted[(int)(sorted.Length * 0.98f)];
        Component pc = (Component)Player();
        SpriteRenderer psr = pc != null ? pc.GetComponent<SpriteRenderer>() : null;
        float band = -1f;
        if (psr != null)
        {
            Vector2 a = Scr(psr.bounds.min), b = Scr(psr.bounds.max);
            var ys = new List<float>();
            for (int y = Mathf.Max(0, (int)Mathf.Min(a.y, b.y) - 4); y <= Mathf.Min(h - 1, (int)Mathf.Max(a.y, b.y) + 4); y++)
                for (int x = Mathf.Max(0, (int)Mathf.Min(a.x, b.x) - 4); x <= Mathf.Min(w - 1, (int)Mathf.Max(a.x, b.x) + 4); x++) ys.Add(all[y * w + x]);
            ys.Sort();
            if (ys.Count > 10) band = ys.Skip((int)(ys.Count * 0.9f)).Average();
        }
        sb.Append(" 띠=" + F1(band) + " 화면p99=" + F1(p99) + " 화면p98=" + F1(p98));

        // 4) 적 경고: 테(반지름 0.9 ~ 1.1 중 채도가 가장 높은 둘레의 점 24곳 평균색) 색상 · Y, 닫히는 흰 고리(둘레 Y 중앙값의 최댓값)
        //   2026-10-25~ 닫히는 고리는 반지름 1.8 → 1.0 으로 「바깥에서」 좁혀 옴 (Telegraph.Update) — 예전엔 0.85 안쪽만 훑어 고리 대신 채움 원판을 쟀음(Y 100~147)
        //   → 1.12 ~ 1.85 를 0.005 간격 · 둘레 48곳 중앙값으로 (고리 끝 6° 틈 · 겹친 다른 경고 · 피해 숫자 몇 점에 흔들리지 않게)
        if (warns != null && warns.Length > 0)
        {
            var hues = new List<string>(); var rimY = new List<string>(); var ringY = new List<string>();
            var ys48 = new float[48];
            foreach (var (wpos, wr) in warns)
            {
                float bestS = -1f, bestH = 0f, bestY = 0f, ring = 0f;
                for (int k = 0; k <= 40; k++)
                {
                    float rr = wr * (0.05f + k * 0.0275f);         // 0.05 ~ 1.15
                    float sr = 0f, sg = 0f, sbb = 0f, sy = 0f;
                    for (int j = 0; j < 24; j++)
                    {
                        float an = j * Mathf.PI * 2f / 24f;
                        Vector2 s = Scr(wpos + new Vector3(Mathf.Cos(an), Mathf.Sin(an), 0f) * rr);
                        Color32 c = px[Mathf.Clamp((int)s.y, 0, h - 1) * w + Mathf.Clamp((int)s.x, 0, w - 1)];
                        sr += c.r; sg += c.g; sbb += c.b; sy += Y601(c);
                    }
                    Color mc = new Color(sr / 24f / 255f, sg / 24f / 255f, sbb / 24f / 255f);
                    Color.RGBToHSV(mc, out float hh, out float ss, out float vv);
                    if (rr >= wr * 0.88f && rr <= wr * 1.12f && ss * vv > bestS) { bestS = ss * vv; bestH = hh * 360f; bestY = sy / 24f; }
                }
                for (float f = 1.12f; f <= 1.85f; f += 0.005f)
                {
                    for (int j = 0; j < 48; j++)
                    {
                        float an = (j + 0.5f) * Mathf.PI * 2f / 48f;
                        Vector2 s = Scr(wpos + new Vector3(Mathf.Cos(an), Mathf.Sin(an), 0f) * (wr * f));
                        ys48[j] = Yat((int)s.x, (int)s.y);
                    }
                    Array.Sort(ys48);
                    ring = Mathf.Max(ring, (ys48[23] + ys48[24]) * 0.5f);
                }
                hues.Add(F1(bestH)); rimY.Add(F1(bestY)); ringY.Add(F1(ring));
            }
            sb.Append(" 테색상=" + string.Join(",", hues) + " 테Y=" + string.Join(",", rimY) + " 흰고리Y=" + string.Join(",", ringY));
        }
        else sb.Append(" 테색상=- 테Y=- 흰고리Y=-");

        // 7) 룬 · 큰 충격파 (결계 · 침식 거대 룬 제거 확인)
        int rune = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Count(r => r.enabled && r.gameObject.activeInHierarchy && r.gameObject.layer != UiLayer && r.name.IndexOf("rune", StringComparison.OrdinalIgnoreCase) >= 0);
        int bigShock = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r => r.enabled && r.gameObject.activeInHierarchy && r.name.StartsWith("fx_shock") && r.bounds.size.x * pxPerUnit > h * 0.33f);
        sb.Append(" 룬=" + rune + " 큰충격파=" + bigShock);
        Log(sb.ToString());

        // 5) 내 이펙트: 깨끗한 판과 적 몸 평균 차이 · 적 위(Effect 층) fx_ 렌더러 알파 상위 · 화염 지대 층
        if (shot.EndsWith("_clean")) readClean[place] = bodyMean;
        else
        {
            var sb5 = new StringBuilder("내이펙트 " + shot);
            // 적차이 = 같은 순간 내 이펙트만 끈 판(_nofx) 대비 (피격 꼬리 어둡게는 양쪽에 같이 있음) · 깨끗한판차이 = 맞기 전 판 대비 (참고 · 피격 연출 포함)
            float n0 = readNoFx.TryGetValue(place, out float nv) ? nv : -1f;
            float c0 = readClean.TryGetValue(place, out float cv) ? cv : -1f;
            sb5.Append(" 적차이=" + (n0 > 0f && bodyMean >= 0f ? F1(Mathf.Abs(bodyMean - n0) / n0 * 100f) : "-"));
            sb5.Append(" 깨끗한판차이=" + (c0 > 0f && bodyMean >= 0f ? F1(Mathf.Abs(bodyMean - c0) / c0 * 100f) : "-"));
            var fxTop = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && r.sortingLayerName == "Effect" && r.name.StartsWith("fx_") && r.name != "fx_warn" && r.name != "fx_shock")
                .OrderByDescending(r => r.color.a).Take(4).ToList();
            sb5.Append(" 효과층알파=" + (fxTop.Count > 0 ? string.Join(",", fxTop.Select(r => r.name + ":" + r.color.a.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))) : "-"));
            SpriteRenderer zsr = zone != null ? zone.GetComponent<SpriteRenderer>() : null;
            SpriteRenderer esr = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).FirstOrDefault(r => r.enabled && r.GetComponent(T("EnermyController")) != null);
            sb5.Append(" 화염층=" + (zsr != null ? zsr.sortingLayerName + "/" + zsr.sortingOrder + "#" + SortingLayer.GetLayerValueFromID(zsr.sortingLayerID) : "-"));
            sb5.Append(" 적층=" + (esr != null ? esr.sortingLayerName + "/" + esr.sortingOrder + "#" + SortingLayer.GetLayerValueFromID(esr.sortingLayerID) : "-"));
            var booms = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && r.name == "fx_explosion").ToList();
            sb5.Append(" 폭발층=" + (booms.Count > 0 ? string.Join(",", booms.Select(r => r.sortingLayerName + "/" + r.sortingOrder + "#" + SortingLayer.GetLayerValueFromID(r.sortingLayerID)).Distinct()) : "-"));
            Log(sb5.ToString());
        }

        // 6) 화소 밀도: 그림 1칸이 화면 몇 화소인지 (|배율| ÷ PPU × 화면 화소/월드) — 12 넘는 것만
        var dense = new Dictionary<string, float>();
        foreach (Renderer r in rends)
        {
            if (!(r is SpriteRenderer sr) || sr.sprite == null || !r.isVisible) continue;
            Vector2 a = Scr(r.bounds.min), b = Scr(r.bounds.max);
            if (b.x < 0 || a.x > w || b.y < 0 || a.y > h) continue;
            float d = Mathf.Abs(sr.transform.lossyScale.y) / Mathf.Max(0.0001f, sr.sprite.pixelsPerUnit) * pxPerUnit;
            if (d <= 12f) continue;
            string key = r.gameObject.name.Replace("(Clone)", "").Trim() + "[" + sr.sprite.texture.name + "]";
            dense[key] = Mathf.Max(dense.TryGetValue(key, out float old) ? old : 0f, d);
        }
        Log("밀도 " + shot + " 개수=" + dense.Count + " " + (dense.Count > 0 ? string.Join(" ", dense.OrderByDescending(x => x.Value).Select(x => x.Key.Replace(" ", "_") + "=" + F1(x.Value))) : "-"));
    }

    // ================================================================= 1) graphics-1: 「기억: … n초」 줄 · 결계 게이지 겹침 (무한 · 이야기 · 영어)
    static IEnumerator MemoryBar()
    {
        foreach (string mode in new[] { "Endless", "Normal" })
        {
            yield return LoadGame("Gunner", mode);
            Component b;
            if (mode == "Endless") b = EndlessBoss(0, new Vector3(8f, 2f, 0f));
            else
            {
                object data = Resources.Load("CodexData");
                GameObject prefab = ((GameObject[])Get(data, "bosses"))[0];
                GameObject go = UnityEngine.Object.Instantiate(prefab, PPos() + new Vector3(8f, 2f, 0f), Quaternion.identity);
                object bar = Find("bossbar");
                Set(bar, "bossSpawn", true);
                b = go.GetComponent(T("bosss"));
            }
            yield return Frames(3);
            pilot.on = true; pilot.target = b;
            yield return Secs(2.5f);            // 평타를 쌓음 → 「가장 많이 맞은 공격」
            HpFrac(b, 0.755f);
            yield return Secs(0.3f);
            Call(b, "TakeDamage", F(Get(b, "setEnemyHP")) * 0.012f, 0f, Vector3.zero);
            yield return Secs(1.6f);
            object mem = Get(b, "memory");
            Log(mode + " 기억 발동=" + (mem != null ? Get(mem, "Active") : "기억 없음") + " 줄='" + (mem != null ? Get(mem, "BarText") : "") + "' 체력 " + HpOf(b).ToString("0.000"));
            pilot.fire = false;
            yield return Shot("g1_" + mode + "_mem_line");
            yield return Shot("g1_" + mode + "_mem_line", 1440, 1080);
            float[] g = UltGauge();
            int k = Convert.ToInt32(Get(b, "bossKind"));
            g[k] = 0.9f;
            yield return Frames(2);
            yield return Shot("g1_" + mode + "_gauge90_a", 1920, 1080, false);
            yield return Frames(7);
            yield return Shot("g1_" + mode + "_gauge90_b", 1920, 1080, false);
            if (mode == "Endless")
            {
                Lang("en");
                yield return Frames(5);
                yield return Shot("g1_en_mem_line");
                Lang("ko");
            }
        }
    }

    // ================================================================= 2) graphics-4 · 지형 변화 · 곡 교차 · graphics-5 · balance-3 · 컷신 ESC
    // 맵 확장 (docs/art/맵-확장-기획서.md): 묘역 · 지옥 · 초원 · 심연 · 무한 사막 맵 전체를 한 장씩 + 경계 · 벽 상자 · 포털 로그
    static IEnumerator MapOverview()
    {
        yield return LoadGame("Gunner", "Easy");
        object sm = SGet("StageManager", "Instance");
        Log("경기장 배율 " + SGet("ArenaRules", "Mul") + " · 바닥 " + SGet("ArenaRules", "FloorMin") + "~" + SGet("ArenaRules", "FloorMax") + " · 자리 " + ((System.Collections.IList)SGet("ArenaLayout", "PropSockets")).Count);
        object sp = Find("EnemySpawner");
        if (sp != null) Log("spawnArea " + Get(sp, "spawnAreaMin") + "~" + Get(sp, "spawnAreaMax") + " · 보스 " + Get(sp, "bossSpawnPoint"));
        foreach (BoxCollider2D b in UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Log("  상자 " + b.name + " offset " + b.offset + " size " + b.size + (b.isTrigger ? " (트리거)" : ""));
        string[] names = { "cave", "hell", "meadow", "abyss" };
        for (int st = 0; st < 4; st++)
        {
            Call(sm, "JumpToStage", st);
            yield return Frames(6);
            ((Component)Player()).transform.position = new Vector3(0.5f, -3.5f, 0f);
            foreach (Component e in UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None).Cast<Component>()) e.gameObject.SetActive(false);
            Time.timeScale = 0f;
            yield return Frames(3);
            shotZoom = 1.6f;
            yield return Shot("map_" + names[st]);
            shotZoom = 1f;
            Time.timeScale = 1f;
        }
        yield return LoadGame("Gunner");
        ((Component)Player()).transform.position = new Vector3(0.5f, -3.5f, 0f);
        foreach (Component e in UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None).Cast<Component>()) e.gameObject.SetActive(false);
        Time.timeScale = 0f;
        yield return Frames(3);
        shotZoom = 1.6f;
        yield return Shot("map_desert");
        shotZoom = 1f;
        Time.timeScale = 1f;
    }

    static IEnumerator TerrainAndErosion()
    {
        yield return LoadGame("Gunner");
        pilot.on = true;
        MusicLog("시작");
        {
            object et0 = SGet("EndlessTerrain", "Instance");
            if (et0 == null) Log("EndlessTerrain 없음");
            else
            {
                var layers = (UnityEngine.Tilemaps.Tilemap[])Get(et0, "layers");
                var slot = (byte[][])Get(et0, "slot");
                var offs = (Vector3Int[])Get(et0, "off");
                for (int l = 0; l < 3; l++)
                {
                    var tm = layers[l];
                    int filled = slot[l] == null ? -1 : slot[l].Count(b => b != 0xFF);
                    var names = new HashSet<string>();
                    if (tm != null) foreach (Vector3Int pos in tm.cellBounds.allPositionsWithin) { Sprite sp = tm.GetSprite(pos); if (sp != null && names.Count < 6) names.Add(sp.name); }
                    Log("  지형 층 " + l + ": " + (tm != null ? tm.name + " 칸 " + tm.cellBounds.size + " 순서 " + (tm.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>()?.sortingOrder) : "없음") + " · 칠할 수 있는 칸 " + filled + "/" + (slot[l]?.Length ?? 0) + " · off " + offs[l] + " · 그림 " + string.Join(",", names));
                }
            }
        }
        // 지옥의 군주 → 용암 · 킹 슬라임 → 늪
        foreach (int turn in new[] { 1, 2 })
        {
            Component b = EndlessBoss(turn, new Vector3(6f, 0f, 0f));
            yield return Secs(2.5f);
            MusicLog("보스 " + turn + " 등장 뒤");
            yield return KillKind(turn);
            for (int i = 0; i < 6; i++) { yield return Secs(0.3f); if (i == 1 || i == 5) MusicLog("처치 뒤 " + (i * 0.3f + 0.3f).ToString("0.0") + "초"); }
            yield return Secs(3f);
            object et = SGet("EndlessTerrain", "Instance");
            for (int i = 0; i < 60 && et != null && ((bool)Get(et, "busy") || ((ICollection)Get(et, "pendings")).Count > 0); i++) yield return Secs(0.25f);
            yield return Secs(2f);
            Vector3 bc = et != null ? (Vector3)Get(et, "blobC") : Vector3.zero;
            Log("영토 번짐 끝 · 조각 " + (et != null ? ((ICollection)Get(et, "pieces")).Count : -1) + " · TerrainPiece " + UnityEngine.Object.FindObjectsByType(T("TerrainPiece"), FindObjectsSortMode.None).Length + " · 얼룩 중심 " + bc + " 반지름 " + (et != null ? Get(et, "blobR") : "") + " · 나 " + PPos() + " · 내 자리 영토 " + (et != null ? Call(et, "RealmAt", PPos()) : ""));
            if (et != null)
            {
                var sb = new StringBuilder("  지형 칸: ");
                foreach (FieldInfo fi in et.GetType().GetFields(Any))
                    if (fi.FieldType == typeof(bool) || fi.FieldType == typeof(int) || fi.FieldType == typeof(float)) sb.Append(fi.Name + "=" + fi.GetValue(fi.IsStatic ? null : et) + " ");
                Log(sb.ToString());
            }
            ((Component)Player()).transform.position = bc + new Vector3(0f, -2f, 0f);
            yield return Secs(1f);
            yield return Shot("terrain_after_kind" + turn);
            shotZoom = 3f;
            yield return Shot("terrain_after_kind" + turn + "_far", 1920, 1080, false);
            shotZoom = 1f;
        }
        object em = EM();
        Log("보스 처치 " + Get(em, "BossesDefeated"));
        var zones = UnityEngine.Object.FindObjectsByType(T("TerrainZone"), FindObjectsSortMode.None).Cast<Component>().ToList();
        Log("지형 웅덩이 " + zones.Count + " (용암 " + zones.Count(z => (bool)Get(z, "lava")) + ")");
        yield return Shot("g4_terrain_overview");
        foreach (bool lava in new[] { true, false })
        {
            Component z = zones.FirstOrDefault(x => (bool)Get(x, "lava") == lava);
            if (z == null) { Log((lava ? "용암" : "늪") + " 웅덩이 없음"); continue; }
            ((Component)Player()).transform.position = z.transform.position + new Vector3(-6f, -2f, 0f);
            // 웅덩이 위에 적을 깔아 둠 (멈춰 두고 체력바가 보이게 한 대씩)
            object data = Resources.Load("CodexData");
            var enemies = (GameObject[])Get(data, "enemies");
            for (int i = 0; i < 10; i++)
            {
                GameObject e = UnityEngine.Object.Instantiate(enemies[i % Math.Min(8, enemies.Length)], z.transform.position + (Vector3)(UnityEngine.Random.insideUnitCircle * 2.5f), Quaternion.identity);
                Component ec = e.GetComponent(T("EnermyController"));
                if (ec != null) Call(ec, "TakeDamage", 1f, 0f, Vector3.zero);      // 인자 셋 (SendMessage 는 하나만 넘겨 오류)
            }
            pilot.fire = false;
            yield return Secs(0.6f);
            yield return Shot("g4_" + (lava ? "lava" : "bog") + "_enemies");
            pilot.fire = true;
            yield return Secs(0.5f);
            yield return Shot("g4_" + (lava ? "lava" : "bog") + "_enemies_hit");
        }

        // ---- 보스 둘 → 공간 침식 (용암이 깔린 채)
        Component lavaZone = zones.FirstOrDefault(x => (bool)Get(x, "lava"));
        if (lavaZone != null) ((Component)Player()).transform.position = lavaZone.transform.position + new Vector3(4f, 0f, 0f);
        Component a = EndlessBoss(0, new Vector3(7f, 3f, 0f));
        Component c = EndlessBoss(3, new Vector3(-7f, 3f, 0f));
        yield return Secs(4.5f);
        Log("통합 게이지 " + SGet("BossUltimate", "Shared") + " 참가 " + SGet("BossUltimate", "SharedCount"));
        pilot.fire = false;                  // 침식 동안 보스가 쓰러지지 않게 (곡 · 겹친 보스 처치 시험을 뒤에서)
        HpFrac(a, 0.38f);
        // 컷신 기다림 · 컷신 중 ESC 열고 닫기 → 멈춤 유지
        bool escTested = false;
        for (int i = 0; i < 600; i++)
        {
            yield return null;
            if ((bool)SGet("BossUltimate", "CutscenePlaying"))
            {
                if (!escTested)
                {
                    escTested = true;
                    yield return Frames(10);
                    yield return Shot("g5_erosion_cutscene");
                    object esc = Find("ESCmenu");
                    Call(esc, "ToggleEsc");
                    yield return Frames(3);
                    Call(esc, "ToggleEsc");
                    yield return Frames(3);
                    Log("컷신 중 ESC 열고 닫음 → timeScale " + Time.timeScale + " (컷신 " + SGet("BossUltimate", "CutscenePlaying") + ")");
                }
            }
            if ((bool)SGet("BossUltimate", "ErosionActive") && !(bool)SGet("BossUltimate", "CutscenePlaying") && escTested) break;
        }
        Log("침식 활성 " + SGet("BossUltimate", "ErosionActive"));
        pilot.move = Move.Eye;
        yield return Secs(2f);
        yield return Shot("g5_erosion_spread");
        yield return Shot("g5_erosion_spread", 1440, 1080);
        yield return Secs(2f);
        Vector3 eye = (Vector3)SGet("DomainStrikes", "EyePos");
        Log("고요의 눈 켜짐 " + SGet("DomainStrikes", "EyeOn") + " 자리 " + eye + " · 용암 웅덩이까지 거리 " + string.Join(", ", zones.Where(z => z != null && (bool)Get(z, "lava")).Select(z => Vector2.Distance(z.transform.position, eye).ToString("0.0"))) + " (용암 반지름 " + SGet("EndlessTerrain", "LavaRadius") + ")");
        yield return Shot("g5_erosion_eye");
        // balance-3: 고요의 눈 자리에 용암 웅덩이를 옮겨 그 안에 서 봄 → 「용암 웅덩이」 피해가 들어오는지
        if (lavaZone != null)
        {
            pilot.fire = false;
            pilot.hits.Clear();
            int lavaSeen = 0;
            for (int i = 0; i < 150; i++)
            {
                Vector3 e2 = (Vector3)SGet("DomainStrikes", "EyePos");
                lavaZone.transform.position = e2;
                ((Component)Player()).transform.position = e2;
                In("AutoMove", Vector2.zero);
                pilot.move = Move.Stand;
                yield return null;
                if (i % 10 == 0) lavaSeen += TextsWith(L("용암 웅덩이")).Count;
                if (i == 60) yield return Shot("b3_eye_on_lava");
            }
            Log("눈 + 용암 2.5초: 받은 피해 " + pilot.hits.Count + "번 합 " + pilot.hits.Sum(h => h.dmg).ToString("0") + " (" + string.Join(",", pilot.hits.Select(h => h.dmg.ToString("0"))) + ") · 화면의 「용암 웅덩이」 글 (10프레임마다 센 합) " + lavaSeen);
        }
        pilot.move = Move.Eye;
        for (int i = 0; i < 30 && (bool)SGet("BossUltimate", "Active"); i++) yield return Secs(0.5f);
        Log("침식 끝 · 남은 보스 " + Bosses().Count);
        MusicLog("침식 뒤");
        // 겹쳐 나온 보스 하나만 쓰러짐 → 보스 곡 유지
        if (c != null) { Kill(c); yield return Secs(1.6f); MusicLog("겹친 보스 하나 처치 뒤"); }
        if (a != null) { Kill(a); yield return Secs(1.6f); MusicLog("마지막 보스 처치 뒤"); }
    }

    // ================================================================= 3) 침식 대기 중 보스 하나를 쓰러뜨림 → 남은 보스가 게이지 1.0 → 개별 결계
    static IEnumerator ErosionPending()
    {
        yield return LoadGame("Gunner");
        pilot.on = true;
        Component a = EndlessBoss(0, new Vector3(7f, 3f, 0f));
        Component b = EndlessBoss(1, new Vector3(-7f, 3f, 0f));
        yield return Secs(4.5f);
        HpFrac(a, 0.38f);
        bool pend = false;
        for (int i = 0; i < 120 && !pend; i++) { yield return null; pend = (bool)SGet("BossUltimate", "ErosionPending"); }
        Log("침식 대기 " + pend);
        Kill(b);
        yield return Frames(2);
        Log("한 보스 처치 직후: 대기 " + SGet("BossUltimate", "ErosionPending") + " · 통합 " + SGet("BossUltimate", "Shared") + " · 리치 게이지 " + UltGauge()[0].ToString("0.00"));
        float tk = Time.time;
        for (int i = 0; i < 100000; i++)
        {
            yield return null;
            if ((bool)SGet("BossUltimate", "Active")) break;
            if (Time.time - tk > 25f) break;
            if (i % 600 == 0) Log("  기다림 " + (Time.time - tk).ToString("0.0") + "초: timeScale " + Time.timeScale + " · 무기 진화 " + SGet("WeaponEvolutionUI", "Open") + " · 연출 " + SGet("PlayerController", "Cinematic") + " · 리치 casting " + (a != null ? Get(a, "casting") : "-") + " · 쉬는 시간까지 " + (F(SGet("BossUltimate", "restUntil")) - Time.time).ToString("0.0") + " · 게이지 " + UltGauge()[0].ToString("0.00") + " · 통합 " + SGet("BossUltimate", "Shared"));
        }
        Log((Time.time - tk).ToString("0.0") + "초 뒤 결계 " + SGet("BossUltimate", "Active") + " · 침식 " + SGet("BossUltimate", "ErosionActive") + " · 컷신 " + SGet("BossUltimate", "CutscenePlaying"));
        yield return Secs(1.5f);
        yield return Shot("pending_solo_domain");
    }

    // ================================================================= 4) 15분 최종 국면 보스 넷 · 공간 침식 15초 피하기 (받은 한 대 크기)
    static IEnumerator FinalDodge()
    {
        yield return LoadGame("Gunner", "Endless", false);
        object em = EM();
        Set(em, "BossesDefeated", 1);
        EndlessTime(899f);
        yield return Secs(2f);
        Log("최종 국면 " + Get(em, "finalStage") + " 보스 " + Bosses().Count);
        pilot.on = true; pilot.move = Move.Eye; pilot.refill = true;
        foreach (var bb in Bosses()) bb.transform.position = PPos() + (Vector3)(UnityEngine.Random.insideUnitCircle.normalized * 9f);
        yield return Secs(4.5f);
        var bs = Bosses();
        if (bs.Count > 0) HpFrac(bs[0], 0.38f);
        for (int i = 0; i < 900; i++) { yield return null; if ((bool)SGet("BossUltimate", "ErosionActive") && !(bool)SGet("BossUltimate", "CutscenePlaying")) break; }
        Log("침식 " + SGet("BossUltimate", "ErosionActive") + " 참가 " + SGet("BossUltimate", "SharedCount"));
        pilot.hits.Clear();
        float max = F(Get(Player(), "PlayerMaxHealth"));
        float t0 = Time.time;
        for (int s = 0; s < 15; s++)
        {
            yield return Secs(1f);
            if (s == 3 || s == 8 || s == 13) yield return Shot("final_erosion_" + s + "s");
            if (!(bool)SGet("BossUltimate", "ErosionActive")) break;
        }
        var hs = pilot.hits;
        Log("침식 15초 (눈 따라가기): 피격 " + hs.Count + "번 · 합 " + hs.Sum(h => h.dmg).ToString("0") + " · 최대 한 대 " + (hs.Count > 0 ? hs.Max(h => h.dmg) : 0f).ToString("0.0") + " / 최대 체력 " + max.ToString("0") + " (" + (hs.Count > 0 ? hs.Max(h => h.dmg) / max * 100f : 0f).ToString("0") + "%)");
        Log("피격 시각 · 크기: " + string.Join(" ", hs.Take(40).Select(h => (h.t - t0).ToString("0.0") + ":" + h.dmg.ToString("0"))));
    }

    // ================================================================= 5) system-1: 침식이 펼쳐진 채로 15:00 → 새 보스 · 인트로
    static IEnumerator System1()
    {
        yield return LoadGame("Gunner");
        object em = EM();
        Set(em, "BossesDefeated", 1);
        pilot.on = true;
        EndlessTime(14f * 60f + 40f);
        Component a = EndlessBoss(0, new Vector3(7f, 3f, 0f));
        Component b = EndlessBoss(1, new Vector3(-7f, 3f, 0f));
        yield return Secs(4.5f);
        HpFrac(a, 0.38f);
        for (int i = 0; i < 900; i++) { yield return null; if ((bool)SGet("BossUltimate", "ErosionActive") && !(bool)SGet("BossUltimate", "CutscenePlaying")) break; }
        Log("침식 " + SGet("BossUltimate", "ErosionActive") + " · 시계 " + F(SGet("GameMode", "EndlessSeconds")).ToString("0"));
        // 처음 보는 보스 인트로가 보이게 (배치에서도 · 자동 입력 끔)
        SSet("StoryDirector", "ForceInBatch", true);
        pilot.on = false;
        In("Auto", false);
        EndlessTime(899.3f);
        Vector3 center = (Vector3)Get(Find("SpaceErosion"), "center");
        bool shot = false;
        for (int i = 0; i < 360; i++)
        {
            yield return null;
            int fs = Convert.ToInt32(Get(em, "finalStage"));
            if (fs > 0 && !shot)
            {
                Log("15:00 최종 국면 시작 · 침식 중=" + SGet("BossUltimate", "ErosionActive") + " · 보스 " + Bosses().Count + " · 인트로 " + SGet("StoryDirector", "Playing"));
                shot = true;
            }
            if (shot && (i % 40 == 0)) yield return Shot("s1_final_during_erosion_" + i, 1920, 1080, false);
        }
        foreach (var x in Bosses()) Log("  보스 kind " + Get(x, "bossKind") + " 침식 중심까지 " + Vector2.Distance(x.transform.position, center).ToString("0.0") + " (반지름 48)");
        SSet("StoryDirector", "ForceInBatch", false);
        In("Auto", true);
        Time.timeScale = 1f;
    }

    // ================================================================= 6) graphics-2 · 엄마의 대청소 (분노) · 잔소리 · ESC · 설정 · 근접
    static IEnumerator MomDomain()
    {
        yield return LoadGame("Swordsman", "Endless", false);
        pilot.on = true; pilot.move = Move.Melee;
        Component mom = FinalBoss(true, new Vector3(4f, 0f, 0f));
        yield return Secs(4.5f);
        HpFrac(mom, 0.45f);
        yield return Secs(0.5f);
        HpFrac(mom, 0.38f);
        for (int i = 0; i < 900; i++) { yield return null; if ((bool)SGet("BossUltimate", "Active") && !(bool)SGet("BossUltimate", "CutscenePlaying")) break; }
        Log("엄마 결계 " + SGet("BossUltimate", "Active") + " · 분노 " + Get(mom, "Enraged"));
        pilot.hits.Clear();
        float max = F(Get(Player(), "PlayerMaxHealth"));
        float t0 = Time.time;
        // 잔소리 글자가 뜨는 순간을 찾아 찍음
        int nags = 0;
        string lastNag = "";
        int[][] sizes = { new[] { 1920, 1080 }, new[] { 1440, 1080 }, new[] { 1920, 1200 } };
        for (int f = 0; f < 60 * 16 && (bool)SGet("BossUltimate", "Active"); f++)
        {
            yield return null;
            GameObject nagCanvas = GameObject.Find("MomNagCanvas");
            TMP_Text nag = nagCanvas != null ? nagCanvas.GetComponentInChildren<TMP_Text>() : null;
            if (nag != null && nag.alpha > 0.95f && nag.text != lastNag)
            {
                lastNag = nag.text;
                nags++;
                if (nags <= 3)
                {
                    int[] sz = sizes[nags - 1];
                    yield return Shot("g2_nag_" + nags, sz[0], sz[1]);
                    if (nags == 1 || nags == 2)
                    {
                        object esc = Find("ESCmenu");
                        Call(esc, "ToggleEsc");
                        yield return Real(0.3f);
                        yield return Shot("g2_nag_esc_" + nags, sz[0], sz[1]);
                        object hud = SCall("UIKit", "HudCanvas");
                        SCall("SettingsUI", "Open", ((Component)hud).transform);
                        yield return Real(0.2f);
                        yield return Shot("g2_nag_settings_" + nags, sz[0], sz[1]);
                        SCall("SettingsUI", "Close");
                        yield return Frames(2);
                        Call(esc, "ToggleEsc");
                        yield return Frames(2);
                        Log("ESC 닫은 뒤 timeScale " + Time.timeScale);
                    }
                }
            }
            if (f == 60 * 5) yield return Shot("mom_patterns_5s");
            if (f == 60 * 9) yield return Shot("mom_patterns_9s");
        }
        var hs = pilot.hits;
        Log("엄마 결계 (검사 · 붙어 싸움): 잔소리 " + nags + "번 · 피격 " + hs.Count + "번 · 합 " + hs.Sum(h => h.dmg).ToString("0") + " / 최대 체력 " + max.ToString("0") + " · 최대 한 대 " + (hs.Count > 0 ? hs.Max(h => h.dmg) : 0).ToString("0"));
        Log("피격: " + string.Join(" ", hs.Take(50).Select(h => (h.t - t0).ToString("0.0") + ":" + h.dmg.ToString("0"))));
        // 결계 밖 엄마 기본 패턴 (분노) 10초
        pilot.hits.Clear();
        yield return Secs(10f);
        Log("결계 뒤 엄마 분노 10초 (붙어 싸움): 피격 " + pilot.hits.Count + "번 · 합 " + pilot.hits.Sum(h => h.dmg).ToString("0"));
    }

    // ================================================================= 7) 개발자 「디버그 모드」
    static IEnumerator DevDomain()
    {
        yield return LoadGame("Gunner", "Endless", false);
        pilot.on = true;
        Component dev = FinalBoss(false, new Vector3(8f, 2f, 0f));
        yield return Secs(4.5f);
        HpFrac(dev, 0.38f);
        for (int i = 0; i < 900; i++) { yield return null; if ((bool)SGet("BossUltimate", "Active") && !(bool)SGet("BossUltimate", "CutscenePlaying")) break; }
        Log("개발자 결계 " + SGet("BossUltimate", "Active"));
        pilot.hits.Clear();
        float max = F(Get(Player(), "PlayerMaxHealth"));
        for (int s = 0; s < 15 && (bool)SGet("BossUltimate", "Active"); s++)
        {
            yield return Secs(1f);
            if (s == 1 || s == 5 || s == 9 || s == 13) yield return Shot("dev_domain_" + s + "s");
        }
        var hs = pilot.hits;
        Log("개발자 결계 (거너 · 거리 두기): 피격 " + hs.Count + "번 · 합 " + hs.Sum(h => h.dmg).ToString("0") + " / " + max.ToString("0") + " · 최대 한 대 " + (hs.Count > 0 ? hs.Max(h => h.dmg) : 0).ToString("0"));
    }

    // ================================================================= 7-2) 개발자 · 엄마 고유 기본 공격 12종 (공격마다 한 장 · 결계 · 영웅 흉내는 끔)
    static IEnumerator OwnAttacks()
    {
        string[][] names = {
            new[] { "LiveCoding", "SelectDelete", "BuildFail", "ErrorPopup", "Loading", "StackOverflow" },
            new[] { "Slipper", "Scratcher", "TimeSale", "Broom", "Medal", "LightsOut" } };
        float[][] when = { new[] { 1.7f, 0.75f, 1.0f, 0.9f, 0.9f, 1.25f }, new[] { 1.0f, 0.55f, 0.45f, 1.25f, 0.15f, 1.25f } };
        Type atk = T("BossSkills").GetNestedType("OwnAtk", BindingFlags.NonPublic);
        for (int m = 0; m < 2; m++)
        {
            bool mom = m == 1;
            yield return LoadGame("Gunner", "Endless", false);
            pilot.on = false;
            In("AutoMove", Vector2.zero); In("AutoFire", false);
            Component b = FinalBoss(mom, new Vector3(9f, 3f, 0f));
            if (b == null) { Log("XX 보스 없음"); yield break; }
            if (b.GetComponent(T("BossUltimate")) is Behaviour ult) ult.enabled = false;
            yield return Secs(1f);
            Component sk = b.GetComponent(T("BossSkills"));       // bosss.Start 에서 붙음
            if (sk == null) { Log("XX BossSkills 없음"); yield break; }
            for (int i = 0; i < names[m].Length; i++)
            {
                Call(sk, "CancelOwn");
                Set(sk, "next", Time.time + 999f);
                Set(sk, "ownNext", Time.time + 999f);
                Set(sk, "ownPhase", mom ? 3 : 2);
                Call(Player(), "GrantInvincibility", 30f);
                b.transform.position = PPos() + new Vector3(9f, 3f, 0f);
                yield return Secs(0.4f);
                bool ok = false;
                for (int t = 0; t < 20 && !ok; t++)
                {
                    ok = (bool)Call(sk, "StartOwn", Enum.Parse(atk, names[m][i]), Player());
                    if (!ok) yield return null;
                }
                Log("고유 " + names[m][i] + " 시작 " + ok);
                if (!ok) continue;
                yield return Secs(when[m][i]);
                yield return Shot("own_" + (mom ? "mom_" : "dev_") + (i + 1) + "_" + names[m][i]);
                foreach (object md in (System.Collections.IEnumerable)Get(sk, "medals"))
                {
                    Component part = Get(md, "part") as Component;
                    SpriteRenderer msr = part != null ? part.GetComponent<SpriteRenderer>() : null;
                    Log("메달 " + (part != null ? part.transform.position.ToString() + " 크기 " + part.transform.lossyScale.x.ToString("0.00") : "없음") + " 그림 " + (msr != null && msr.sprite != null ? msr.sprite.name + " 색 " + msr.color + " 층 " + msr.sortingLayerName + "/" + msr.sortingOrder + " 켬 " + msr.enabled + " 경계 " + msr.bounds.size : "없음") + " · 플레이어 " + PPos());
                }
                for (int f = 0; f < 600 && (bool)Get(sk, "ownRunning"); f++) yield return null;
                Log("고유 " + names[m][i] + " 끝 · 돌고 있음 " + Get(sk, "ownRunning") + " · 경고 " + Get(sk, "teleCount") + " · 탄 " + ((System.Collections.ICollection)SGet("HostileProjectile", "Live")).Count);
            }
            Call(sk, "CancelOwn");
            yield return Secs(0.5f);
            Log((mom ? "엄마" : "개발자") + " 정리 뒤 돌고 있음 " + Get(sk, "ownRunning") + " · 메달 " + ((System.Collections.ICollection)Get(sk, "medals")).Count + " · 시전 " + Get(b, "casting"));
        }
    }

    // ================================================================= 8) 기억의 방패 · 근접 (검사 · 유령 vs 지옥의 군주)
    static IEnumerator MemShield()
    {
        foreach (string hero in new[] { "Swordsman", "Ghost" })
        {
            yield return LoadGame(hero);
            Component b = EndlessBoss(1, new Vector3(3f, 0f, 0f));
            yield return Frames(3);
            pilot.on = true; pilot.move = Move.Melee; pilot.target = b;
            // 날개 단계(66%) 아래 · 기억(50%) 위
            HpFrac(b, 0.6f);
            yield return Secs(4f);
            HpFrac(b, 0.505f);
            yield return Secs(0.2f);
            Call(b, "TakeDamage", F(Get(b, "setEnemyHP")) * 0.012f, 0f, Vector3.zero);
            object mem = null;
            for (int i = 0; i < 120; i++) { yield return null; mem = Get(b, "memory"); if (mem != null && (bool)Get(mem, "Active")) break; }
            bool shield = mem != null && (bool)Get(mem, "shield");
            Log(hero + " 기억 " + (mem != null ? Get(mem, "Active") : "없음") + " 방패=" + shield + " 줄 '" + (mem != null ? Get(mem, "BarText") : "") + "' 다시 돌기 " + (mem != null ? Get(mem, "retargetEvery") : "") + "초 · 깨기 필요 " + (mem != null ? Get(mem, "breakNeed") : ""));
            pilot.move = Move.Flank;
            float t0 = Time.time;
            int flankFrames = 0, frames = 0;
            bool broke = false;
            float brokeAt = -1f;
            for (int f = 0; Time.time - t0 < 12f; f++)
            {
                yield return null;
                frames++;
                if (mem != null && (bool)Get(mem, "shield") && (bool)Get(mem, "Active"))
                {
                    Vector2 sd = (Vector2)Get(mem, "shieldDir");
                    Vector2 me = PPos() - b.transform.position;
                    if (Vector2.Angle(me, sd) > 60f) flankFrames++;
                }
                if (!broke && F(Get(b, "memoryTakenMul")) > 1.01f) { broke = true; brokeAt = Time.time - t0; }
                if (f == 90) yield return Shot("mem_shield_" + hero);
            }
            Log(hero + " 12초 옆 · 뒤 돌기: 옆 · 뒤에 있던 비율 " + (flankFrames * 100f / Mathf.Max(1, frames)).ToString("0") + "% · 깨짐 " + broke + (broke ? " (" + brokeAt.ToString("0.0") + "초)" : "") + " · 누적 " + (mem != null ? F(Get(mem, "breakAccum")) / F(Get(b, "setEnemyHP")) : 0f).ToString("0.000"));
        }
    }

    // ================================================================= 9) balance-4: 개발자 · 엄마 처치 시간 (보통 빌드 근사: 레벨 45 · 무기 진화 2번)
    static IEnumerator Ttk()
    {
        foreach (string hero in new[] { "Gunner", "Postman", "Alchemist" })
        {
            yield return LoadGame(hero);
            object p = Player();
            object shop = Find("LevelShop");
            Call(shop, "GrantLevels", p, 35);
            pilot.autoCards = true;
            for (int i = 0; i < 400 && Convert.ToInt32(Get(shop, "PendingLevels")) > 0; i++) { yield return null; }
            object sp = Get(Find("StageManager"), "specials");
            for (int e = 0; e < 2 && sp != null; e++)
            {
                int[] opts = (int[])Call(sp, "NextEvolutionOptions");
                if (opts != null && opts.Length > 0) Call(sp, "EvolveWeapon", opts[0]);
            }
            Log(hero + " 레벨 " + Get(p, "level") + " · 공격력 " + Get(p, "attackDamage"));
            foreach (bool mom in new[] { false, true })
            {
                Component boss = FinalBoss(mom, new Vector3(9f, 2f, 0f));
                yield return Frames(3);
                float max = F(Get(boss, "setEnemyHP"));
                pilot.on = true; pilot.move = Move.Ranged; pilot.target = boss; pilot.ult = true;
                yield return Secs(2f);
                Time.timeScale = 2f;
                float hp0 = F(Get(boss, "EnemyHealth")), t0 = Time.time;
                int hc0 = Convert.ToInt32(Get(boss, "hitCount"));
                for (int k = 0; k < 8; k++)
                {
                    yield return Secs(5f);
                    Log("  " + (Time.time - t0).ToString("0") + "초: 거리 " + Vector2.Distance(PPos(), boss.transform.position).ToString("0.0") + " · 무적 " + Get(boss, "invulnerable") + " · casting " + Get(boss, "casting") + " · 받는 배율 " + Get(boss, "damageTakenMul") + " · 기억 " + (Get(boss, "memory") is object mm && mm != null ? Get(mm, "Active") : "-") + " · 체력 " + F(Get(boss, "EnemyHealth")).ToString("0") + " · 결계 " + SGet("BossUltimate", "Active") + " · 연출 " + SGet("PlayerController", "Cinematic") + " · timeScale " + Time.timeScale);
                    if (k == 2) { Time.timeScale = 1f; yield return Shot("ttk_" + hero + (mom ? "_mom" : "_dev")); Time.timeScale = 2f; }
                }
                Time.timeScale = 1f;
                float lost = hp0 - F(Get(boss, "EnemyHealth"));
                float dt = Time.time - t0;
                float est = lost > 0 ? max / (lost / dt) : -1f;
                Log(hero + (mom ? " 엄마 " : " 개발자 ") + "체력 " + max + " · " + dt.ToString("0") + "초에 " + lost.ToString("0") + " (" + (lost / max * 100f).ToString("0.0") + "%) · DPS " + (lost / dt).ToString("0") + " → 단순 처치 예상 " + (est / 60f).ToString("0.0") + "분 (결계 · 기억 방패 · 무적 시간 제외)");
                pilot.target = null;
                UnityEngine.Object.Destroy(boss.gameObject);
                yield return Secs(1f);
            }
        }
    }

    // ================================================================= 10) balance-1: 보스 보상 가운데 칸 (체력 30% 아래 · 400번 뽑기) + 실제 창
    static IEnumerator RewardSlot()
    {
        yield return LoadGame("Gunner", "Normal", false);
        object p = Player();
        float max = F(Get(p, "PlayerMaxHealth"));
        foreach (float frac in new[] { 0.25f, 1f })
        {
            var count = new Dictionary<string, int>();
            for (int i = 0; i < 400; i++)
            {
                p.GetType().GetField("PlayerHealth").SetValue(p, max * frac);
                object slot = SCall("ChestRewards", "MakeBossSlot", p, PPos());
                string n = slot == null ? "(없음)" : (string)Get(Get(slot, "option"), "name");
                count[n] = count.TryGetValue(n, out int c) ? c + 1 : 1;
            }
            Log("체력 " + (frac * 100) + "%: " + string.Join(" · ", count.OrderByDescending(x => x.Value).Select(x => L(x.Key) + " " + x.Value)));
        }
        p.GetType().GetField("PlayerHealth").SetValue(p, max);
        // 실제 창 (QaFlags.AllowReward 로 배치 모드 생략을 풂)
        if (T("QaFlags") != null)
        {
            SSet("QaFlags", "AllowReward", true);
            In("Auto", false);
            pilot.autoChoice = false;
            p.GetType().GetField("PlayerHealth").SetValue(p, max * 0.25f);
            object sm = Find("StageManager");
            ((MonoBehaviour)sm).StartCoroutine((IEnumerator)Call(sm, "BossReward"));
            for (int i = 0; i < 200 && Find("ChoiceUI") == null; i++) yield return null;
            yield return Real(0.8f);
            yield return Shot("b1_boss_reward_lowhp");
            object ch = Find("ChoiceUI");
            if (ch != null) Set(ch, "confirmed", 2);
            yield return Frames(5);
            In("Auto", true);
            SSet("QaFlags", "AllowReward", false);
        }
    }

    // ================================================================= 11) 무한 판 저장 → 이어하기 · system-3 풍화 토스트 · 엄마 남은 수
    // 1.2.8 제보 재현: 사용자 실제 저장(qa.ps1 -UserSave 가 QA 데이터 폴더에 복사)으로 이어하기 → 화면 · 글자 · 메인 메뉴로 나간 뒤
    static void LogTexts(string tag)
    {
        var tmps = UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None);
        Log(tag + " 글자 " + tmps.Length + " · 캔버스 " + UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length + " · 장면 " + SceneManager.GetActiveScene().name);
        foreach (var grp in tmps.Where(t => t.isActiveAndEnabled).GroupBy(t => t.gameObject.name + " @" + (t.transform.root != null ? t.transform.root.name : "-") + (t.gameObject.scene.name == "DontDestroyOnLoad" ? " [DDOL]" : "")).OrderByDescending(g => g.Count()).Take(12))
            Log("  " + grp.Key + " × " + grp.Count() + " (예: " + grp.First().text.Replace("\n", " ").Substring(0, Math.Min(50, grp.First().text.Length)) + ")");
    }

    // 1.3.3 이펙트 스킨 처치 연출 크기 (네 가지를 나란히)
    static IEnumerator SkinFxShot()
    {
        yield return LoadGame("Gunner");
        yield return Secs(2f);
        Transform p = ((Component)Find("PlayerController")).transform;
        Type sk = T("SkinFx"); Type sd = T("SkinData");
        object fxInst = sk.GetProperty("Instance").GetValue(null);
        if (fxInst == null) { Log("SkinFx 없음"); yield break; }
        string[] ids = { "skin_bluefire", "skin_sakura", "skin_thunder", "skin_starlight" };
        var all = (System.Collections.IEnumerable)sd.GetField("All", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null);
        var kill = sk.GetMethod("OnKill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        for (int round = 0; round < 3; round++)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                object def = null;
                foreach (object d in all) if ((string)d.GetType().GetField("id").GetValue(d) == ids[i]) def = d;
                sk.GetField("EffectSkin").SetValue(null, def);
                sk.GetField("killFxAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(fxInst, 0f);
                kill.Invoke(fxInst, new object[] { p.position + new Vector3(-9f + i * 6f, 2f, 0f) });
            }
            yield return Secs(0.12f);
        }
        yield return Shot("skin_fx_kill", 1920, 1080, false);
    }

    // 1.3.3 불 장판 (화염 지대 · 지옥의 군주 불 웅덩이 · FireGround)
    static IEnumerator FireGroundShot()
    {
        yield return LoadGame("Gunner");
        yield return Secs(2f);
        Transform p = ((Component)Find("PlayerController")).transform;
        Type sa = T("SpecialAbilities");
        object inst = sa.GetProperty("SharedInstance").GetValue(null);
        object zone = sa.GetMethod("SpawnZone").Invoke(inst, new object[] { p.position + new Vector3(-5f, 1f, 0f), 3f, 8f, 0f, new Color(1f, 0.4f, 0.1f, 0.8f) });
        zone.GetType().GetField("lava").SetValue(zone, true);
        T("HazardZone").GetMethod("Spawn").Invoke(null, new object[] { p.position + new Vector3(5f, 1f, 0f), 2.8f, 8f, 0f, new Color(1f, 0.5f, 0.1f, 0.9f), "fire", 0f, "Qa", "Qa" });
        yield return Secs(1.2f);
        yield return Shot("fire_ground_a", 1920, 1080, false);
        yield return Secs(0.35f);
        yield return Shot("fire_ground_b", 1920, 1080, false);
    }

    // 1.3.3 로딩 화면 (배치 모드는 SceneFlow 가 바로 넘어가므로 막 · 로딩을 직접 띄워 찍음)
    static IEnumerator LoadingScreen()
    {
        In("Auto", false);
        SceneManager.LoadScene("MainMenu");
        yield return Frames(20);
        Type sf = T("SceneFlow");
        const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
        object inst = sf.GetMethod("Ensure", BF).Invoke(null, null);
        ((Canvas)sf.GetField("canvas", BF).GetValue(inst)).enabled = true;
        ((UnityEngine.UI.Image)sf.GetField("curtain", BF).GetValue(inst)).color = Color.black;
        sf.GetMethod("ShowLoading", BF).Invoke(inst, new object[] { "GameScene" });
        foreach (float target in new[] { 0.35f, 0.8f })
        {
            sf.GetField("targetProgress", BF).SetValue(inst, target);
            float end = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < end) { sf.GetMethod("Dots", BF).Invoke(inst, null); sf.GetMethod("Tick", BF).Invoke(inst, null); yield return null; }
            yield return Shot("loading_" + Mathf.RoundToInt(target * 100), 1920, 1080, false);
        }
        ((RectTransform)sf.GetField("loadingBox", BF).GetValue(inst)).gameObject.SetActive(false);
        ((UnityEngine.UI.Image)sf.GetField("curtain", BF).GetValue(inst)).color = Color.clear;
        ((Canvas)sf.GetField("canvas", BF).GetValue(inst)).enabled = false;
        In("Auto", true);
    }

    // 1.2.9 제보: FPS 표시(장면이 바뀌어도 남는 정렬 32000 캔버스)를 켜면 결과 화면 UI 가 거기 붙어 메인 메뉴까지 남음
    static IEnumerator DdolCanvas()
    {
        var made = new List<Canvas>();
        foreach (string n in new[] { "QaDdolA", "QaDdolB" })
        {
            GameObject go = new GameObject(n);
            UnityEngine.Object.DontDestroyOnLoad(go);
            Canvas c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 32000;
            made.Add(c);
        }
        yield return LoadGame("Rogue");
        yield return Secs(2f);
        SSet("RunStats", "GaveUp", true);
        SceneManager.LoadScene("GameOver");
        yield return Real(3f);
        LogTexts("결과 화면");
        Log("결과 화면: 남는 캔버스 자식 " + string.Join(", ", made.Select(c => c.name + " " + c.transform.childCount)));
        yield return Shot("ddol_gameover", 1920, 1080, false);
        SceneManager.LoadScene("MainMenu");
        yield return Real(3f);
        LogTexts("메인 메뉴");
        int ddolTexts = UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Count(t => t.gameObject.scene.name == "DontDestroyOnLoad");
        Log("메인 메뉴: 남는 캔버스 자식 " + string.Join(", ", made.Select(c => c.name + " " + c.transform.childCount)) + " · 장면 밖 글자 " + ddolTexts + (ddolTexts == 0 && made.All(c => c.transform.childCount == 0) ? " → 통과" : " → 실패"));
        yield return Shot("ddol_menu", 1920, 1080, false);
        foreach (var c in made) UnityEngine.Object.Destroy(c.gameObject);
    }

    static IEnumerator UserResume()
    {
        In("Auto", false);
        SceneManager.LoadScene("MainMenu");
        yield return Frames(30);
        Log("이어하기 저장 있음 " + SGet("RunSave", "Has"));
        SCall("RunSave", "Continue");
        for (int i = 0; i < 900; i++)
        {
            yield return null;
            object sm = Find("StageManager");
            if (sm != null && (bool)Get(sm, "Started") && EM() != null) break;
        }
        yield return Real(2f);
        LogTexts("이어한 뒤 2초");
        yield return Shot("user_resume_2s", 1920, 1080, false);
        yield return Real(8f);
        LogTexts("이어한 뒤 10초");
        yield return Shot("user_resume_10s", 1920, 1080, false);
        SceneManager.LoadScene("MainMenu");
        yield return Real(3f);
        LogTexts("메인 메뉴로 나간 뒤");
        yield return Shot("user_resume_menu", 1920, 1080, false);
        In("Auto", true);
    }

    static IEnumerator SaveResume()
    {
        if (T("QaFlags") == null) { Log("QaFlags 없음 — 저장 시험 건너뜀"); yield break; }
        yield return LoadGame("Gunner");
        pilot.on = true;
        object em = EM();
        foreach (int turn in new[] { 0, 1, 2 })
        {
            EndlessBoss(turn, new Vector3(6f, 0f, 0f));
            yield return Secs(1.5f);
            yield return KillKind(turn);
            yield return Secs(4f);
        }
        Component live = EndlessBoss(3, new Vector3(8f, 2f, 0f));
        yield return Secs(1f);
        Set(live, "spawnedAt", Time.time - 200f);
        HpFrac(live, 0.7f);
        yield return Secs(1.5f);
        string erodeKey = L("{0} — 모래 폭풍에 풍화되어 받는 피해가 1분마다 {1}% 늘어납니다.").Split('—')[0].Length > 0 ? "%" : "%";
        var toast1 = TextsWith(L("모래 폭풍에 풍화되어").Trim());
        Log("풍화 토스트 (저장 전) " + toast1.Count + " " + string.Join(" | ", toast1));
        yield return Shot("save_before");
        object terrain = SGet("EndlessTerrain", "Instance");
        object[] args = { 0, "" };
        terrain.GetType().GetMethod("Save").Invoke(terrain, args);
        string terr0 = (string)args[1];
        float hp0 = F(Get(live, "EnemyHealth")), sec0 = F(SGet("GameMode", "EndlessSeconds")), next0 = F(Get(em, "nextBoss")) - Time.time;
        int bossesDef = Convert.ToInt32(Get(em, "BossesDefeated"));
        Log("저장: 시계 " + sec0.ToString("0") + " · 처치 " + bossesDef + " · 살아 있는 보스 체력 " + hp0.ToString("0") + " · 다음 보스까지 " + next0.ToString("0") + "초 · 지형 " + terr0.Length + "자");
        SSet("QaFlags", "AllowSave", true);
        In("Auto", false);
        SCall("RunSave", "Save");
        Log("저장 있음 " + SGet("RunSave", "Has"));
        SceneManager.LoadScene("MainMenu");
        yield return Frames(20);
        SCall("RunSave", "Continue");
        In("Auto", true);
        for (int i = 0; i < 600; i++)
        {
            yield return null;
            object sm = Find("StageManager");
            if (sm != null && (bool)Get(sm, "Started") && EM() != null && Bosses().Count > 0) break;
        }
        pilot.Reset(true);
        yield return Secs(0.5f);
        var toast2 = TextsWith(L("모래 폭풍에 풍화되어").Trim());
        em = EM();
        terrain = SGet("EndlessTerrain", "Instance");
        args = new object[] { 0, "" };
        terrain.GetType().GetMethod("Save").Invoke(terrain, args);
        var b2 = Bosses();
        Log("이어하기: 시계 " + F(SGet("GameMode", "EndlessSeconds")).ToString("0") + " · 처치 " + Get(em, "BossesDefeated") + " · 보스 " + b2.Count + " 체력 " + string.Join(",", b2.Select(x => F(Get(x, "EnemyHealth")).ToString("0"))) + " · 다음 보스까지 " + (F(Get(em, "nextBoss")) - Time.time).ToString("0") + "초 · 지형 같음 " + (terr0 == (string)args[1]) + " (" + ((string)args[1]).Length + "자)");
        yield return Secs(1.5f);
        toast2.AddRange(TextsWith(L("모래 폭풍에 풍화되어").Trim()));
        Log("풍화 토스트 (이어한 뒤) " + toast2.Count + " " + string.Join(" | ", toast2.Distinct()));
        yield return Shot("save_after_resume");
        // 1.2.8 제보: 이어하기 뒤 UI 도배 — 자동 조종을 끄고(실제 플레이처럼 알림이 켜짐) 화면 · 글자 수를 봄
        Log("이어한 뒤 글자 수 (자동 켬) " + UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Length);
        In("Auto", false);
        yield return Secs(3f);
        Log("이어한 뒤 3초 글자 수 (자동 끔) " + UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Length + " · 캔버스 " + UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length);
        yield return Shot("resume_noauto_3s", 1920, 1080, false);
        yield return Secs(5f);
        var tmps = UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None);
        Log("이어한 뒤 8초 글자 수 (자동 끔) " + tmps.Length + " · 캔버스 " + UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length);
        foreach (var grp in tmps.GroupBy(t => t.gameObject.name).OrderByDescending(g => g.Count()).Take(8)) Log("  글자 오브젝트 " + grp.Key + " × " + grp.Count() + " (예: " + grp.First().text.Replace("\n", " ").Substring(0, Math.Min(40, grp.First().text.Length)) + ")");
        yield return Shot("resume_noauto_8s", 1920, 1080, false);
        In("Auto", true);

        // 개발자 처치 → 영혼 모으는 중 저장 · 이어하기 → 남은 수
        foreach (var x in Bosses()) UnityEngine.Object.Destroy(x.gameObject);
        yield return Frames(3);
        em = EM();
        Set(em, "finalStage", 2);
        Set(em, "devSummoned", true);
        Set(em, "devDown", true);
        SSet("RunStats", "Kills", 500);
        Set(em, "momKillBase", 200);
        Log("엄마까지 남은 수 (저장 전) " + Get(em, "MomSoulsLeft"));
        In("Auto", false);
        SCall("RunSave", "Save");
        SceneManager.LoadScene("MainMenu");
        yield return Frames(20);
        SCall("RunSave", "Continue");
        In("Auto", true);
        for (int i = 0; i < 600; i++) { yield return null; object sm = Find("StageManager"); if (sm != null && (bool)Get(sm, "Started") && EM() != null) break; }
        pilot.Reset(true);
        yield return Secs(1f);
        Log("엄마까지 남은 수 (이어한 뒤) " + Get(EM(), "MomSoulsLeft") + " · finalStage " + Get(EM(), "finalStage"));
        yield return Shot("save_mom_souls_resume");
        SSet("QaFlags", "AllowSave", false);
        SCall("RunSave", "Delete");
    }

    // ================================================================= 12) system-2: 일일 도전 15:00 「완주!」 창 + 최후의 시간 인트로
    static IEnumerator System2()
    {
        Type dc = T("DailyChallenge");
        Set(dc, "runDay", DateTime.UtcNow.Date);
        Set(dc, "runRule", Call(dc, "RuleOf", DateTime.UtcNow.Date));
        Set(dc, "Active", true);
        object hero = Call(dc, "CharacterOf", DateTime.UtcNow.Date);
        yield return LoadGame(hero.ToString());
        object em = EM();
        Log("일일 도전 " + Get(em, "daily") + " 영웅 " + hero);
        pilot.on = true;
        EndlessBoss(0, new Vector3(6f, 0f, 0f));
        yield return Secs(1.5f);
        yield return KillKind(0);
        yield return Secs(4f);
        SSet("StoryDirector", "ForceInBatch", true);
        pilot.on = false;
        In("Auto", false);
        EndlessTime(899.4f);
        bool any = false;
        bool lastIntro = false; int lastFin = 0;
        for (int i = 0; i < 3000; i++)
        {
            yield return null;
            bool intro = (bool)SGet("StoryDirector", "Playing");
            string fk = L("daily.reward.15"); var fin = TextsWith(fk.Length > 6 ? fk.Substring(0, 6) : fk);
            bool change = intro != lastIntro || (fin.Count > 0) != (lastFin > 0);
            lastIntro = intro; lastFin = fin.Count;
            if ((intro || fin.Count > 0) && (change || i % 300 == 0))
            {
                any = true;
                Log("프레임 " + i + ": 인트로 " + intro + " · 완주 글 " + fin.Count + " · 최종 국면 " + Get(em, "finalStage") + " · timeScale " + Time.timeScale);
                yield return Shot("s2_daily_finish_" + i, 1920, 1080, false);
            }
        }
        if (!any) Log("완주 창 · 인트로 둘 다 보이지 않음 · 최종 국면 " + Get(em, "finalStage"));
        SSet("StoryDirector", "ForceInBatch", false);
        In("Auto", true);
        Time.timeScale = 1f;
        Set(dc, "Active", false);
    }

    // ================================================================= 13) 5분 자동 플레이: 오류 · 오브젝트 수 · 프레임 시간 · 체력바 피격 · 그래픽 낮음
    static IEnumerator Soak()
    {
        yield return LoadGame("Gunner");
        pilot.on = true; pilot.ult = true;
        Time.timeScale = 1.5f;
        float t0 = Time.time;
        int lastFrame = Time.frameCount;
        float lastReal = Time.realtimeSinceStartup;
        float worst = 0f;
        int next = 0;
        while (Time.time - t0 < 300f)
        {
            yield return null;
            worst = Mathf.Max(worst, pilot.lastFrameDt);
            float el = Time.time - t0;
            if (el >= next)
            {
                float real = Time.realtimeSinceStartup - lastReal;
                int fr = Time.frameCount - lastFrame;
                int go = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
                int en = ((ICollection)SGet("EnermyController", "Live")).Count;
                int pr = ((ICollection)SGet("HostileProjectile", "Live")).Count;
                int fx = UnityEngine.Object.FindObjectsByType(T("FxAnim"), FindObjectsSortMode.None).Length;
                int ar = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length;
                Log("t=" + el.ToString("000") + " 시계 " + F(SGet("GameMode", "EndlessSeconds")).ToString("0") + " · 평균 프레임 " + (fr > 0 ? real / fr * 1000f : 0f).ToString("0.0") + "ms · 최악 " + (worst * 1000f).ToString("0") + "ms · 오브젝트 " + go + " · 적 " + en + " · 적 탄 " + pr + " · Fx " + fx + " · 소리 " + ar + " · 보스 " + Bosses().Count + " · 레벨 " + Get(Player(), "level"));
                lastReal = Time.realtimeSinceStartup; lastFrame = Time.frameCount; worst = 0f;
                next += 20;
            }
            if (Mathf.Abs(el - 100f) < 0.02f || (el > 100f && el < 100.1f)) { }
        }
        Time.timeScale = 1f;
        // 체력바 피격 연출: 보스를 두고 큰 한 방 · 작은 여러 방
        Component b = EndlessBoss(2, new Vector3(6f, 2f, 0f));
        yield return Secs(1.5f);
        Call(b, "TakeDamage", F(Get(b, "setEnemyHP")) * 0.12f, 0f, Vector3.zero);
        yield return Frames(4);
        yield return Shot("hpbar_bighit", 1920, 1080, false);
        yield return Secs(0.4f);
        yield return Shot("hpbar_bighit_04s", 1920, 1080, false);
        // 그래픽 품질 낮음
        SSet("GraphicsQuality", "Index", 1);
        In("Auto", false);
        yield return Secs(1f);
        yield return Shot("graphics_low");
        SSet("GraphicsQuality", "Index", 0);
        In("Auto", true);
    }

    // ================================================================= 14) 메인 메뉴 새 UI · 4개 언어 · 16:9 / 4:3 · 소리 경로
    static IEnumerator UiMenu()
    {
        // 게임 안 소리 사전 확인
        yield return LoadGame("Gunner");
        object fxIn = SGet("SpecialAbilities", "SharedFx");
        var clips = fxIn != null ? (IDictionary)Get(fxIn, "clips") : null;
        Log("게임 안 SharedFx " + (fxIn != null) + " · 합성음 coin=" + (clips != null && clips.Contains("coin")) + " chime=" + (clips != null && clips.Contains("chime")) + " pop=" + (clips != null && clips.Contains("pop")) + " pew=" + (clips != null && clips.Contains("pew")) + " weakhit=" + (clips != null && clips.Contains("weakhit")));
        // 게임 안 ESC 스킬 목록 · 도감 기억 탭 (4개 언어)
        Call(Find("LevelShop"), "GrantLevels", Player(), 12);
        for (int i = 0; i < 200 && Convert.ToInt32(Get(Find("LevelShop"), "PendingLevels")) > 0; i++) yield return null;
        pilot.autoCards = false;
        object esc = Find("ESCmenu");
        foreach (string l in new[] { "ko", "en", "ja", "zh" })
        {
            Lang(l);
            Call(esc, "ToggleEsc");
            yield return Real(0.3f);
            yield return Shot("ui_esc_skills_" + l);
            if (l == "ko" || l == "en") yield return Shot("ui_esc_skills_" + l, 1440, 1080);
            Call(esc, "ToggleEsc");
            yield return Frames(2);
        }
        Lang("zh");
        yield return Frames(10);
        yield return Shot("ui_game_zh");
        Lang("ko");

        SceneManager.LoadScene("MainMenu");
        yield return Frames(30);
        object fxMenu = SGet("SpecialAbilities", "SharedFx");
        bool fxAlive = fxMenu is UnityEngine.Object uo ? uo != null : fxMenu != null;
        Log("메인 메뉴 SharedFx 살아 있음 " + fxAlive + " · SpecialFeedback 수 " + (T("SpecialFeedback") != null ? UnityEngine.Object.FindObjectsByType(T("SpecialFeedback"), FindObjectsSortMode.None).Length : -1) + " → Hostile.Play 는 " + (fxAlive ? "소리 남" : "아무 소리도 안 남"));

        // 보상 · 기억 조각 · 비밀 상태 만들기
        SSet("CodexSeen", "ShowAll", false);
        SCall("CodexSeen", "Mark", "sec.dev", "Developer");
        SCall("CodexSeen", "Mark", "sec.mom", "Mom");
        object gunner = Enm("CharacterId", "Gunner");
        SCall("Prefs", "SetInt", "memory." + SCall("Contracts", "HeroKey", gunner) + ".1", 1);
        SCall("Prefs", "SetInt", "memory." + SCall("Contracts", "HeroKey", gunner) + ".2", 1);
        try { SCall("EndlessMilestones", "Reach", 0); SCall("EndlessMilestones", "Reach", 1); } catch (Exception e) { Log("이정표 실패 " + e.Message); }
        Transform root = ((Component)SCall("UIKit", "HudCanvas")).transform;

        foreach (string l in new[] { "ko", "en", "ja", "zh" })
        {
            Lang(l);
            yield return Frames(3);
            foreach (int[] sz in new[] { new[] { 1920, 1080 }, new[] { 1440, 1080 } })
            {
                int w = sz[0], h = sz[1];
                string tag = l;
                yield return Shot("ui_menu_" + tag, w, h);
                // 계약 보드
                try { SCall("ContractBoard", "Open", root); } catch (Exception e) { Log("계약 보드 실패 " + e.InnerException?.Message); }
                yield return Frames(3);
                yield return Shot("ui_contracts_" + tag, w, h);
                try { SCall("ContractBoard", "Close"); } catch { }
                yield return Frames(2);
                // 받기 창
                SCall("ClaimRewards", "Open", root);
                yield return Frames(3);
                object win = SGet("ClaimRewards", "window");
                if (win is GameObject wg && wg != null) { yield return Shot("ui_claim_" + tag, w, h); UnityEngine.Object.Destroy(wg); }
                else Log("받기 창이 열리지 않음 (받을 것 없음)");
                yield return Frames(2);
                // 설정 6탭
                SCall("SettingsUI", "Open", root);
                for (int t = 0; t < 6; t++)
                {
                    SCall("SettingsUI", "ShowTab", t);
                    yield return Frames(2);
                    yield return Shot("ui_settings_" + t + "_" + tag, w, h);
                }
                SCall("SettingsUI", "Close");
                yield return Frames(2);
                // 도감: 기억 · 비밀 · 보스 · 검색
                SCall("CodexUI", "Open", root);
                foreach (int t in new[] { 13, 9, 1 })
                {
                    SCall("CodexUI", "Show", t);
                    yield return Frames(3);
                    yield return Shot("ui_codex_" + t + "_" + tag, w, h);
                }
                if (w == 1920)
                {
                    object search = SGet("CodexUI", "search");
                    if (search is TMP_InputField f) f.SetTextWithoutNotify("Developer");
                    SCall("CodexUI", "RunSearch", "Developer");
                    yield return Frames(3);
                    yield return Shot("ui_codex_search_dev_" + tag, w, h);
                    if (search is TMP_InputField f2) f2.SetTextWithoutNotify(l == "ko" ? "개발자" : l == "ja" ? "開発者" : l == "zh" ? "开发者" : "Developer");
                    SCall("CodexUI", "RunSearch", l == "ko" ? "개발자" : l == "ja" ? "開発者" : l == "zh" ? "开发者" : "Developer");
                    yield return Frames(3);
                    yield return Shot("ui_codex_search_local_" + tag, w, h);
                    try { SCall("CodexUI", "ClearSearch"); } catch { }
                }
                SCall("CodexUI", "Close");
                yield return Frames(2);
                // 기억 조각 읽기
                try
                {
                    Type rd = T("MemoryShards+Reader");
                    rd.GetMethod("Open", Any).Invoke(null, new object[] { root, gunner, 1 });
                }
                catch (Exception e) { Log("기억 조각 실패 " + (e.InnerException ?? e).Message); }
                yield return Real(1.5f);
                yield return Shot("ui_memory_read_" + tag, w, h);
                SCall("MemoryShards", "CloseReader");
                yield return Frames(2);
                // 기록 창
                try { SCall("RunHistoryUI", "Open", root); } catch (Exception e) { Log("기록 실패 " + (e.InnerException ?? e).Message); }
                yield return Frames(3);
                yield return Shot("ui_history_" + tag, w, h);
                try { SCall("RunHistoryUI", "Close"); } catch { }
                yield return Frames(2);
            }
        }
        Lang("ko");
    }
}
