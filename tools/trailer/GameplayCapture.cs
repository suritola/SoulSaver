using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 실제 게임 플레이 트레일러 + 스팀 스크린샷 (1.8.6~, tools/trailer/record.ps1 로 프로젝트 복사본에서 배치 실행)
// 게임 씬을 자동 조종으로 플레이하며 매 프레임 카메라를 MP4로 인코딩하고, 중요한 순간은 PNG 스크린샷으로 저장
// 흐름: 거너 전투 · 레벨업 카드 → 무기 진화 → 영혼 트리 → 스킬 진화 (2.1~) → 전설 스킨 영웅 3명 · 보스 3종
//       → 4장 심연 · 거울의 군주가 필살기를 흉내 (2.1.4~) → 보스 필살기 결계 (2.1~) → 스킨 상점 → 로고
public class GameplayCapture
{
    static readonly string Out = Environment.GetEnvironmentVariable("CAPTURE_OUT") ?? "C:/Temp/SoulSaverCapture";
    static readonly int Fps = int.TryParse(Environment.GetEnvironmentVariable("CAPTURE_FPS"), out int f) ? f : 30;
    static readonly string Quality = Environment.GetEnvironmentVariable("CAPTURE_QUALITY") ?? "Medium";
    static readonly string Lang = Environment.GetEnvironmentVariable("CAPTURE_LANG") ?? "en";   // ko · en · ja · zh

    // 트레일러 자막 (언어별)
    static readonly Dictionary<string, string[]> Captions = new Dictionary<string, string[]>
    {
        ["en"] = new[] { "THE DEAD HAVE RISEN.", "BUILD YOUR RUN.", "DEFEAT A BOSS.  EVOLVE YOUR WEAPON.", "GROW A SOUL TREE OF 100+ NODES.", "FIVE HEROES.  FOUR WORLDS.  FOUR KINGS.", "20 CHARACTER SKINS.  10 WEAPON SKINS.", "WISHLIST NOW ON STEAM",
                         "MAX OUT SKILLS.  FUSE 37 EVOLUTIONS.", "SURVIVE THE BOSS'S BARRIER.", "THE FINAL ABYSS.  A MIRROR THAT COPIES YOUR EVERY SKILL." },
        ["ko"] = new[] { "망자들이 깨어났다.", "나만의 빌드를 만들어라.", "보스를 쓰러뜨리고, 무기를 진화시켜라.", "100칸이 넘는 영혼 트리.", "다섯 영웅.  네 개의 세계.  네 명의 왕.", "캐릭터 스킨 20종 · 무기 스킨 10종", "지금 스팀에서 찜하세요",
                         "스킬을 모아 합쳐라.  37가지 스킬 진화.", "보스의 결계를 버텨라.", "마지막 심연.  너의 모든 기술을 흉내 내는 거울." },
        ["ja"] = new[] { "死者が目覚めた。", "自分だけのビルドを。", "ボスを倒し、武器を進化させろ。", "100マスを超える魂のツリー。", "五人の英雄。四つの世界。四人の王。", "キャラクタースキン20種・武器スキン10種", "Steamでウィッシュリストに追加",
                         "スキルを極め、合わせろ。37のスキル進化。", "ボスの結界を耐え抜け。", "最後の深淵。お前の技をすべて真似る鏡。" },
        ["zh"] = new[] { "亡者已苏醒。", "打造你的流派。", "击败首领，进化武器。", "超过100个节点的灵魂树。", "五位英雄。四个世界。四位王者。", "20款角色皮肤 · 10款武器皮肤", "立即在Steam上添加愿望单",
                         "练满技能，融合进化。37种技能进化。", "撑过首领的结界。", "最后的深渊。模仿你每一招的镜子。" },
    };
    static string C(int i) => (Captions.TryGetValue(Lang, out string[] c) ? c : Captions["en"])[i];

    // 게임 언어 바꾸기 (GameSettings.Language: 0 한국어 · 1 English · 2 日本語 · 3 中文)
    static void SetGameLanguage(string lang)
    {
        int i = lang == "ko" ? 0 : lang == "ja" ? 2 : lang == "zh" ? 3 : 1;
        Type l = T("Loc+Lang");
        T("GameSettings").GetProperty("Language").SetValue(null, Enum.ToObject(l, i));
    }
    const int W = 1920, H = 1080;
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);

    static Type T(string n) => Type.GetType(n + ", Assembly-CSharp");
    static object Get(object o, string f) => o.GetType().GetField(f, Any)?.GetValue(o);
    static void Set(object o, string f, object v) => o.GetType().GetField(f, Any).SetValue(o, v);
    static object Call(object o, string m, params object[] a) => o.GetType().GetMethod(m, Any).Invoke(o, a);
    static void In(string f, object v) => T("GameInput").GetField(f).SetValue(null, v);
    static UnityEngine.Object Find(string type) => UnityEngine.Object.FindFirstObjectByType(T(type));

    // ================================================================= 녹화기 (씬이 바뀌어도 남음)
    class Rec : MonoBehaviour
    {
        public bool recording, autopilot, levelUpAllowed;
        public float musicVolume = 0.9f;
        public string musicPath = "Music/bgm_boss";
        public int frames;
        // 하이라이트 영상 연출 (2.2.1~): 펀치 줌 · 화면 흔들림 · 흰 번쩍임 (찍을 때만 카메라에 얹고 바로 되돌림)
        public float baseZoom = 1f, punch, shake, flashAmt;
        // 자동 조종이 적과 두는 거리 (잡몹 · 보스, 가까우면 물러나고 멀면 다가감)
        public float nearMob = 3.5f, farMob = 7f, nearBoss = 6.5f, farBoss = 9f;
        public Image flash;
        public Canvas captionCanvas;
        public TextMeshProUGUI caption, logo, sub;
        public Image black;

        MediaEncoder encoder;
        NativeArray<float> audio;
        RenderTexture rt;
        Texture2D frame;
        float[] music;
        int musicCh, musicRate, musicFrames, cursor, perFrame;
        const int Rate = 48000;
        float nextTap;
        bool held;

        public void Begin(string path)
        {
            rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            frame = new Texture2D(W, H, TextureFormat.RGBA32, false);
            AudioClip clip = Resources.Load<AudioClip>(musicPath);
            music = new float[clip.samples * clip.channels];
            clip.GetData(music, 0);
            musicCh = clip.channels; musicRate = clip.frequency; musicFrames = clip.samples;
            perFrame = Rate / Fps;
            var mode = (UnityEditor.VideoBitrateMode)Enum.Parse(typeof(UnityEditor.VideoBitrateMode), Quality);
            var v = new VideoTrackAttributes { frameRate = new MediaRational(Fps), width = W, height = H, includeAlpha = false, bitRateMode = mode };
            var a = new AudioTrackAttributes { sampleRate = new MediaRational(Rate), channelCount = (ushort)musicCh, language = "en" };
            encoder = new MediaEncoder(path, v, a);
            audio = new NativeArray<float>(perFrame * musicCh, Allocator.Persistent);
        }

        public void End()
        {
            recording = false;
            encoder?.Dispose();
            encoder = null;
            if (audio.IsCreated) audio.Dispose();
        }

        // 매 프레임 게임 처리가 끝난 뒤 카메라를 그려 영상에 붙임 (UI 캔버스도 카메라로 옮겨서 함께 찍힘)
        // 배치 모드에서는 WaitForEndOfFrame 이 오지 않을 수 있어 LateUpdate 에서 찍음
        void Capture()
        {
            {
                if (!recording || encoder == null) return;
                Texture2D t = Grab(true);
                encoder.AddFrame(t);
                for (int i = 0; i < perFrame; i++)
                {
                    double pos = (cursor + i) * (double)musicRate / Rate;
                    int i0 = (int)pos % musicFrames, i1 = (i0 + 1) % musicFrames;
                    float k = (float)(pos - Math.Floor(pos));
                    for (int c = 0; c < musicCh; c++)
                        audio[i * musicCh + c] = Mathf.Lerp(music[i0 * musicCh + c], music[i1 * musicCh + c], k) * musicVolume;
                }
                cursor += perFrame;
                encoder.AddSamples(audio);
                frames++;
            }
        }

        public Texture2D Grab(bool withCaption)
        {
            Camera cam = Camera.main;
            if (cam == null) return frame;
            cam.cullingMask |= 1 << 5;
            CanvasesToCamera(cam);
            captionCanvas.worldCamera = cam;
            captionCanvas.enabled = withCaption;
            RenderTexture old = cam.targetTexture;
            float size = cam.orthographicSize;
            Vector3 pos = cam.transform.position;
            cam.orthographicSize = size * baseZoom * (1f - 0.2f * Mathf.Clamp01(punch));
            cam.transform.position = pos + (Vector3)(UnityEngine.Random.insideUnitCircle * shake * 0.6f);
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            cam.targetTexture = old;
            cam.orthographicSize = size;
            cam.transform.position = pos;
            captionCanvas.enabled = true;
            RenderTexture.active = rt;
            frame.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            frame.Apply();
            RenderTexture.active = null;
            return frame;
        }

        // ---------------- 자동 조종: 가까운 적(보스 먼저)을 겨누고 옆으로 돌며 싸움
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
                // 보스는 덩치가 커서 멀리서 싸움 (영웅이 보스 그림에 가려지지 않게)
                float near = boss ? nearBoss : nearMob, far = boss ? farBoss : farMob;
                In("AutoAim", target.Value);
                In("AutoMove", side * 0.75f + (d < near ? -to.normalized * 1.2f : d > far ? to.normalized * 0.7f : Vector2.zero));
            }
            else
            {
                In("AutoMove", Vector2.zero);
                In("AutoAim", me + Vector3.right * 5f);
            }
            // 눌렀다 떼기를 반복 (총 · 베기 · 활 시위 · 플라스크 모두 쏘게)
            if (Time.time >= nextTap)
            {
                held = !held;
                if (held) { In("FireDownFrame", Time.frameCount + 1); In("AutoFire", true); nextTap = Time.time + 0.42f; }
                else { In("FireUpFrame", Time.frameCount + 1); In("AutoFire", false); nextTap = Time.time + 0.06f; }
            }
        }

        // 죽지 않고, 보여 주려는 때가 아니면 레벨업 창이 뜨지 않게
        void LateUpdate()
        {
            punch *= 0.86f;
            shake *= 0.85f;
            flashAmt *= 0.8f;
            if (flash != null) flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(flashAmt));
            object p = Find("PlayerController");
            if (p != null)
            {
                Set(p, "PlayerHealth", Get(p, "PlayerMaxHealth"));
                if (!levelUpAllowed) Set(p, "nowEXP", 0f);
            }
            // 1.1~ 판 시작 카드 · 쌓인 레벨업 이어 고르기 창이 녹화 중에 화면을 덮지 않게 바로 확정
            if (!levelUpAllowed && Find("LevelShop") is object shop && (bool)shop.GetType().GetProperty("IsOpen").GetValue(shop))
                ConfirmCard(shop, 0);
            Capture();
        }

        static Vector3? Nearest(Vector3 from, out bool boss)
        {
            Vector3? best = null; float bd = 16f * 16f;
            boss = false;
            foreach (string type in new[] { "bosss", "EnermyController" })
            {
                boss = type == "bosss";
                foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(T(type), FindObjectsSortMode.None))
                {
                    if (o.GetType().GetProperty("IsDead")?.GetValue(o) is bool dead && dead) continue;
                    Vector3 p = ((Component)o).transform.position;
                    float d = (p - from).sqrMagnitude;
                    if (d < bd) { bd = d; best = p; }
                }
                if (best.HasValue) return best;
            }
            return best;
        }
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
            // 진화 카드 · 영혼 트리처럼 자체 정렬을 쓰는 하위 캔버스도 가장 위 레이어로 (안 그러면 맵 그림 아래로 깔림)
            else if (!c.isRootCanvas && c.overrideSorting && c.sortingLayerName != layers[layers.Length - 1].name)
            {
                c.sortingLayerName = layers[layers.Length - 1].name;
                c.sortingOrder = Mathf.Min(32400, 31000 + c.sortingOrder);
            }
    }

    // ================================================================= 연출 도구
    static Rec rec;

    // 녹화 중에는 게임 시간이 1/Fps 씩 흐르므로 프레임 수로 기다림
    static IEnumerator F(float seconds)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Fps));
        for (int i = 0; i < n; i++) yield return null;
    }

    static void Shot(string name)
    {
        Texture2D t = rec.Grab(false);
        string path = Path.Combine(Out, "screenshots", name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, t.EncodeToPNG());
        Debug.Log("[CAP] " + name);
    }

    static IEnumerator Fade(float from, float to, float time)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(time * Fps));
        for (int i = 0; i <= n; i++) { rec.black.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, i / (float)n)); yield return null; }
    }

    static IEnumerator Caption(string text, float time)
    {
        rec.caption.text = text;
        int n = Mathf.RoundToInt(time * Fps);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Fps;
            rec.caption.alpha = Mathf.Min(Mathf.Clamp01(t / 0.3f), Mathf.Clamp01((time - t) / 0.35f));
            yield return null;
        }
        rec.caption.alpha = 0f;
    }

    static void Say(string text, float time) => rec.StartCoroutine(Caption(text, time));

    static IEnumerator Load(string hero, int stage)
    {
        Type cd = T("CharacterData"), cid = T("CharacterId");
        cd.GetField("Override", Any).SetValue(null, Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(cid), Enum.Parse(cid, hero)));
        SceneManager.LoadScene("GameScene");
        yield return null; yield return null; yield return null;
        yield return CloseLevelUps();
        if (stage > 0) GoStage(stage);
        Swarm(stage, 12);
        yield return null;
    }

    // 레벨업 카드 확정 (게임에서는 카드를 누른 뒤 [Space])
    static void ConfirmCard(object shop, int slot)
    {
        string f = slot == 0 ? "first" : slot == 1 ? "second" : "third";
        Set(shop, "pendingSlot", -1);
        Call(shop, "onSelect", Get(shop, f));
    }

    // 판을 시작하면 뜨는 첫 카드 창과 쌓인 레벨업을 정리
    static IEnumerator CloseLevelUps()
    {
        for (int i = 0; i < 6; i++)
        {
            yield return null;
            object shop = Find("LevelShop");
            if (shop != null && (bool)shop.GetType().GetProperty("IsOpen").GetValue(shop)) ConfirmCard(shop, 0);
        }
        Time.timeScale = 1f;
    }

    static void GoStage(int stage)
    {
        object sm = Find("StageManager");
        Call(sm, "ShowStageMap", stage);            // 0 묘역 · 1 지옥 · 2 초원 · 3 심연 (맵 켜기 · 배경색)
        sm.GetType().GetProperty("CurrentStage", Any).SetValue(sm, stage);
        object sp = Get(sm, "spawner");
        sp.GetType().GetMethod("StartStage").Invoke(sp, new object[] { stage });
        object bar = Find("bossbar");
        bar.GetType().GetField("bossSpawn").SetValue(bar, false);
        ((Transform)Get(sm, "player")).position = (Vector2)Get(sm, "stage2PlayerStart");
    }

    // 주인공 둘레에 그 스테이지 적을 깔아 둠 (화면이 비지 않게)
    static void Swarm(int stage, int count)
    {
        object data = Resources.Load("CodexData");
        var enemies = (GameObject[])Get(data, "enemies");
        var stages = (int[])Get(data, "enemyStage");
        var pool = new List<GameObject>();
        for (int i = 0; i < enemies.Length; i++) if (stages[i] == stage) pool.Add(enemies[i]);
        if (pool.Count == 0 && StageConfig(stage) is object cfg) pool.AddRange((GameObject[])Get(cfg, "enemies"));     // 4장 심연: 도감 대신 스폰러의 견본
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            float a = i / (float)count * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
            UnityEngine.Object.Instantiate(pool[i % pool.Count], me + new Vector3(Mathf.Cos(a) * 1.4f, Mathf.Sin(a)) * UnityEngine.Random.Range(5.5f, 8.5f), Quaternion.identity);
        }
    }

    // 스폰러의 그 장 설정 (StageConfig)
    static object StageConfig(int stage)
    {
        object sp = Get(Find("StageManager"), "spawner");
        Array stages = sp != null ? (Array)Get(sp, "stages") : null;
        return stages != null && stage < stages.Length ? stages.GetValue(stage) : null;
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
        yield return null; yield return null;
    }

    static object SpawnBoss(int stage, float hp)
    {
        object data = Resources.Load("CodexData");
        GameObject[] bosses = (GameObject[])Get(data, "bosses");
        GameObject prefab = stage < bosses.Length ? bosses[stage] : (GameObject)Get(StageConfig(stage), "bossPrefab");     // 4장 거울의 군주는 스폰러에만 있음
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        GameObject go = UnityEngine.Object.Instantiate(prefab, me + new Vector3(8.5f, 3.5f, 0f), Quaternion.identity);
        object bar = Find("bossbar");
        bar.GetType().GetField("bossSpawn").SetValue(bar, true);
        object sp = Get(Find("StageManager"), "spawner");
        sp.GetType().GetField("bossSpawned", Any)?.SetValue(sp, true);
        object b = go.GetComponent(T("bosss"));
        if (b != null) Set(b, "EnemyHealth", Convert.ToSingle(Get(b, "setEnemyHP")) * hp);
        // 보스는 제자리에서 기술만 씀 (쫓아와서 영웅을 덮으면 화면에서 영웅이 안 보임)
        if (b != null && b.GetType().GetField("speed", Any) is FieldInfo speed) speed.SetValue(b, Convert.ChangeType(0, speed.FieldType));
        return b;
    }

    // 전설 스킨 장착 (복사본 에디터 PlayerPrefs 에만 저장됨 · 끝나면 지움)
    static readonly List<string> skinKeys = new List<string>();
    static void WearLegend(int who)
    {
        Type sd = T("SkinData");
        foreach (object s in (Array)sd.GetField("All").GetValue(null))
        {
            if (Convert.ToInt32(Get(s, "tier")) != 3 || Convert.ToInt32(Get(s, "who")) != who || Get(s, "kind").ToString() == "Effect") continue;
            string id = (string)Get(s, "id");
            PlayerPrefs.SetInt("skin.own." + id, 1);
            skinKeys.Add("skin.own." + id);
            skinKeys.Add("skin.eq." + Convert.ToInt32(Get(s, "kind")) + "." + who);
            sd.GetMethod("Equip").Invoke(null, new[] { s });
        }
    }

    static void BuildOverlay()
    {
        GameObject go = new GameObject("CaptureOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.layer = 5;
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.planeDistance = 1f;
        c.overrideSorting = true;
        SortingLayer[] layers = SortingLayer.layers;
        c.sortingLayerName = layers[layers.Length - 1].name;
        c.sortingOrder = 32500;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(W, H);
        cs.matchWidthOrHeight = 1f;
        rec.captionCanvas = c;

        rec.black = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        rec.black.transform.SetParent(go.transform, false);
        RectTransform br = rec.black.rectTransform;
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
        rec.black.color = Color.black;

        rec.flash = new GameObject("Flash", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        rec.flash.transform.SetParent(go.transform, false);
        RectTransform fr = rec.flash.rectTransform;
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
        rec.flash.color = new Color(1f, 1f, 1f, 0f);
        rec.flash.raycastTarget = false;

        TMP_FontAsset font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x.name.StartsWith("Cafe24"));
        Material outline = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name.StartsWith("Cafe24") && m.name.Contains("Outline"));
        rec.caption = Text(go.transform, font, outline, 66f, new Vector2(0f, -400f));
        rec.logo = Text(go.transform, font, outline, 230f, new Vector2(0f, 60f));
        rec.logo.text = "Soul Saver";
        rec.sub = Text(go.transform, font, null, 54f, new Vector2(0f, -150f));
        rec.sub.text = "WISHLIST NOW ON STEAM";
        rec.sub.color = new Color(0.92f, 0.88f, 0.8f);
        rec.caption.alpha = rec.logo.alpha = rec.sub.alpha = 0f;
    }

    static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, Material mat, float size, Vector2 pos)
    {
        TextMeshProUGUI t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.gameObject.layer = 5;
        t.transform.SetParent(parent, false);
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = new Vector2(1800f, size * 1.3f);
        if (font != null) t.font = font;
        if (mat != null) t.fontSharedMaterial = mat;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Gold;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    // ================================================================= 장면
    // 한 영웅 장면: 불러오기(녹화 멈춤) → 싸움 → 필살기
    static IEnumerator Hero(string hero, int who, int stage, int boss, float fight, float ultHold, string shot)
    {
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        object b = boss >= 0 ? SpawnBoss(boss, 0.75f) : null;
        rec.autopilot = true;
        yield return F(1.2f);                       // 적이 다가올 시간 (녹화 안 함)
        rec.recording = true;
        yield return Fade(1f, 0f, 0.2f);
        yield return F(fight * 0.6f);
        if (shot != null) Shot(shot);
        yield return F(fight * 0.4f);
        yield return Ult(ultHold);
        yield return F(1.1f);
        yield return Fade(0f, 1f, 0.2f);
    }

    // ---------------- 스킬 진화: 진화에 한 단계 남은 카드를 가운데에 놓고 골라서 진화 연출까지
    // 거너 「천둥 장전」 = 장전 충격파(13, 3단계) + 탄피 지뢰(15, 4단계) + 섬광 장전(19, 4단계)
    static void ForceCards(object shop, int a, int b, int c)
    {
        string[] names = (string[])Get(shop, "ability_name");
        string[] contents = (string[])Get(shop, "ability_content");
        Set(shop, "first", a); Set(shop, "second", b); Set(shop, "third", c);
        foreach (var (title, body, icon, id) in new[] { ("FirstTitle", "FirstAbility", "FirstIcon", a), ("SecondTitle", "SecondAbility", "SecondIcon", b), ("ThirdTitle", "ThirdAbility", "ThirdIcon", c) })
        {
            ((TMP_Text)Get(shop, title)).text = names[id];
            ((TMP_Text)Get(shop, body)).text = contents[id];
            Call(shop, "SetCardIcon", Get(shop, icon), id);
        }
        Call(shop, "ShowEvoHints");
    }

    static IEnumerator SkillEvolution()
    {
        object shop = Find("LevelShop");
        int[] lv = (int[])Get(shop, "ability_level");
        bool[] sel = (bool[])Get(shop, "ability_selected");
        // 진화 재료 단계를 미리 채워 둠 (거너 카드: 장전 충격파 3/3 · 탄피 지뢰 4/4 · 섬광 장전 3/4, 그 밖에 진행 중인 조합 몇 개)
        void Lv(int id, int n) { lv[id] = n; if (n >= (int)T("LevelShop").GetMethod("MaxLevelOf").Invoke(null, new object[] { Enum.Parse(T("CharacterId"), "Gunner"), id })) sel[id] = true; }
        Lv(13, 3); Lv(15, 4); Lv(19, 3); Lv(0, 2); Lv(16, 2); Lv(17, 1); Lv(6, 2);
        rec.levelUpAllowed = true;
        Call(shop, "openLevelShop");
        Call(shop, "setAbilitys");
        ForceCards(shop, 16, 19, 17);
        Say(C(7), 4.6f);
        yield return F(0.9f);
        Call(shop, "onSecondButton");
        yield return F(0.5f);
        Shot("09_skill_evolution_cards");
        yield return F(0.6f);
        ConfirmCard(shop, 1);                       // 마지막 재료 → 진화 연출
        rec.levelUpAllowed = false;
        yield return F(2.9f);
        Shot("10_skill_evolution");
        yield return F(0.8f);
        object ui = Find("SkillEvolutionUI");
        if (ui != null) Set(ui, "closeRequested", true);
        yield return F(0.6f);
    }

    // ---------------- 보스 필살기 결계: 외침 화면 → 어두워진 맵 · 둥근 결계 안의 공격
    static IEnumerator Barrier(string hero, int who, int stage)
    {
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        object b = SpawnBoss(stage, 0.8f);
        rec.autopilot = true;
        yield return F(1.2f);
        rec.recording = true;
        yield return Fade(1f, 0f, 0.2f);
        yield return F(1.4f);
        Component bc = (Component)b;
        Component ult = bc.GetComponent(T("BossUltimate"));
        if (ult == null) ult = bc.gameObject.AddComponent(T("BossUltimate"));
        yield return null;
        ((MonoBehaviour)ult).StartCoroutine((IEnumerator)Call(ult, "Run"));
        Say(C(8), 4f);
        yield return F(1.7f);
        Shot("11_boss_barrier_title");
        yield return F(4.4f);                      // 외침(약 3.3초) → 결계가 펼쳐지고 공격이 쏟아지는 중
        Shot("12_boss_barrier");
        yield return F(1.6f);
        yield return Fade(0f, 1f, 0.2f);
    }

    // ---------------- 4장 영혼의 심연 (2.1.1~): 거울의 군주가 지금 영웅의 필살기를 흉내 냄 (2.1.4~)
    static IEnumerator Mirror(string hero, int who)
    {
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, 3);
        Component b = (Component)SpawnBoss(3, 0.8f);
        yield return null;
        Component skills = b != null ? b.GetComponent(T("BossSkills")) : null;
        if (skills != null) Set(skills, "next", Time.time + 999f);     // 흉내를 쓰기 전에 다른 기술을 꺼내지 않게
        rec.autopilot = true;
        yield return F(1.2f);
        rec.recording = true;
        yield return Fade(1f, 0f, 0.2f);
        Say(C(9), 4.6f);
        yield return F(1.6f);
        Shot("13_abyss_mirror_sovereign");
        if (skills != null) ((MonoBehaviour)skills).StartCoroutine((IEnumerator)Call(skills, "Run", Call(skills, "MirrorUlt", true, true)));
        yield return F(2.4f);
        Shot("14_mirror_mimics_ult");
        yield return F(2.2f);
        yield return Ult(0.6f);
        yield return F(0.9f);
        yield return Fade(0f, 1f, 0.2f);
    }

    [UnityTest]
    public IEnumerator Capture()
    {
        LogAssert.ignoreFailingMessages = true;
        Directory.CreateDirectory(Out);
        Time.captureFramerate = Fps;
        In("Auto", true);                          // 업적 · 누적 기록이 풀리지 않게
        AudioListener.volume = 0f;                 // 촬영하는 동안 스피커로 소리를 내지 않음 (영상 음악은 아래에서 파일로 직접 넣음)
        SetGameLanguage(Lang);
        GameObject host = new GameObject("CaptureRec");
        UnityEngine.Object.DontDestroyOnLoad(host);
        rec = host.AddComponent<Rec>();
        BuildOverlay();
        rec.sub.text = C(6);

        // ---------------- 1) 거너 · 지하 묘역 전투 → 레벨업 카드
        yield return Load("Gunner", 0);
        rec.autopilot = true;
        yield return F(1.2f);
        rec.Begin(Path.Combine(Out, "SoulSaver_Trailer_" + Lang + ".mp4"));
        rec.recording = true;
        yield return Fade(1f, 0f, 0.6f);
        Say(C(0), 2.4f);
        yield return F(2.6f);
        yield return Ult(1.2f);
        yield return F(0.8f);
        Shot("01_gunner_catacombs");
        yield return F(0.6f);

        object shop = Find("LevelShop");
        rec.levelUpAllowed = true;
        Call(shop, "openLevelShop");
        Say(C(1), 2.2f);
        yield return F(0.9f);
        Call(shop, "onSecondButton");            // 가운데 카드를 고름 (금빛)
        yield return F(0.5f);
        Shot("02_level_up_cards");
        yield return F(0.6f);
        ConfirmCard(shop, 1);
        rec.levelUpAllowed = false;
        yield return F(0.6f);

        // ---------------- 2) 무기 진화 (보스를 쓰러뜨렸을 때 나오는 화면)
        object sm = Find("StageManager");
        rec.autopilot = false;
        In("AutoMove", Vector2.zero);
        In("AutoFire", false);
        ((MonoBehaviour)sm).StartCoroutine((IEnumerator)Call(sm, "Evolution", false));
        Say(C(2), 4.2f);
        yield return F(2.6f);
        object evo = Find("WeaponEvolutionUI");
        if (evo != null) Call(evo, "Select", 0);
        yield return F(0.8f);
        Shot("03_weapon_evolution");
        yield return F(0.4f);
        if (evo != null) Set(evo, "confirmed", true);
        yield return F(0.5f);
        rec.autopilot = true;
        yield return F(2.2f);
        yield return Ult(0.8f);
        yield return F(1f);

        // ---------------- 3) 영혼 트리
        rec.autopilot = false;
        In("AutoMove", Vector2.zero);
        In("AutoFire", false);
        Type shards = T("SoulShards");
        shards.GetMethod("Add").Invoke(null, new object[] { 4000, Vector3.zero, false });
        Call(sm, "OpenSoulTree");
        Say(C(3), 4.6f);
        yield return F(0.8f);
        object specials = Get(sm, "specials");
        // 트리 화면의 칸 누르기와 같은 처리 (새 칸이 드러나고 반짝임) · 여섯 가지를 돌아가며 배움
        object tree = T("SoulTreeUI").GetProperty("Instance").GetValue(null);
        for (int i = 0; i < 14 && tree != null; i++)
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
            yield return F(0.26f);
            if (i == 11) Shot("04_soul_tree");
        }
        yield return F(0.6f);
        T("SoulTreeUI").GetMethod("Close").Invoke(null, null);
        yield return F(0.2f);

        // ---------------- 3-2) 스킬 진화 (2.1~)
        yield return SkillEvolution();
        yield return Fade(0f, 1f, 0.25f);

        // ---------------- 4) 전설 스킨을 입은 영웅들 · 네 세계의 왕
        rec.caption.alpha = 0f;
        rec.StartCoroutine(Delayed(0.4f, () => Say(C(4), 4.5f)));
        yield return Hero("Swordsman", 1, 1, 1, 3.2f, 1.0f, "05_swordsman_demon_lord");
        yield return Hero("Archer", 3, 0, 0, 3.0f, 0.9f, "06_archer_lich_king");
        yield return Hero("Rogue", 2, 2, 2, 3.0f, 0.05f, "07_rogue_king_slime");
        // 4장 심연 · 거울의 군주 (2.1.1~): 검사의 회전 베기를 되비춤
        yield return Mirror("Swordsman", 1);
        // 보스 필살기 결계 (2.1~): 연금술사가 지옥의 군주의 「연옥 낙화」에 갇힘
        yield return Barrier("Alchemist", 4, 1);

        // ---------------- 5) 스킨 상점
        rec.recording = false;
        rec.autopilot = false;
        T("CharacterData").GetField("Override", Any).SetValue(null, null);
        In("Auto", true);
        SceneManager.LoadScene("MainMenu");
        yield return F(1.5f);
        GameObject skinButton = GameObject.Find("SkinButton");
        skinButton?.GetComponent<Button>()?.onClick.Invoke();
        yield return F(0.8f);
        rec.recording = true;
        yield return Fade(1f, 0f, 0.25f);
        Say(C(5), 3f);
        yield return F(1.6f);
        Shot("08_skin_shop");
        yield return F(1.4f);

        // ---------------- 6) 로고
        yield return Fade(0f, 1f, 0.5f);
        for (int i = 0; i <= Fps; i++)
        {
            float k = Mathf.SmoothStep(0f, 1f, i / (float)Fps);
            rec.logo.alpha = k;
            rec.sub.alpha = Mathf.Clamp01(k * 1.5f - 0.5f);
            rec.logo.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, k);
            yield return null;
        }
        int tail = Fps * 3;
        for (int i = 0; i < tail; i++) { rec.musicVolume = 0.9f * (1f - i / (float)tail); yield return null; }

        rec.End();
        foreach (string k in skinKeys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
        Time.captureFramerate = 0;
        In("Auto", false);
        Debug.Log("[CAP] done frames=" + rec.frames + " (" + (rec.frames / (float)Fps).ToString("0.0") + "s)");
        yield return null;
    }

    // ================================================================= 하이라이트 영상 (2.2.1~, record.ps1 -Mode highlight)
    // 자막 없이 긴박한 음악 + 펀치 줌 · 흔들림 · 번쩍임 · 슬로 모션, 가장 화려하게 터지는 장면만 빠르게 이어 붙임
    static void Hit(float p, float s, float f) { rec.punch = Mathf.Max(rec.punch, p); rec.shake = Mathf.Max(rec.shake, s); rec.flashAmt = Mathf.Max(rec.flashAmt, f); }

    // 슬로 모션 (게임 시간만 느려지고 음악은 그대로)
    static IEnumerator SlowMo(float scale, float seconds)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Fps));
        for (int i = 0; i < n; i++) { Time.timeScale = Mathf.Lerp(scale, 1f, Mathf.Pow(i / (float)n, 3f)); yield return null; }
        Time.timeScale = 1f;
    }

    static void Evolve(int id)
    {
        if (id < 0) return;
        object sp = Get(Find("StageManager"), "specials");
        if (sp != null) Call(sp, "EvolveWeapon", id);
    }

    // 그 영웅의 n번째 전용 무기 (무기 진화 형태)
    static int KitWeapon(string hero, int nth)
    {
        Type sa = T("SpecialAbilities");
        object def = T("CharacterData").GetMethod("Def").Invoke(null, new[] { Enum.Parse(T("CharacterId"), hero) });
        int k = 0;
        foreach (int id in (int[])Get(def, "pool"))
            if ((bool)sa.GetMethod("KitIsWeapon").Invoke(null, new object[] { id }) && k++ == nth) return id;
        return -1;
    }

    // 빽빽한 적 무리 한가운데서 진화 무기 필살기 → 한꺼번에 쓸려 나감
    static IEnumerator Blast(string hero, int who, int stage, int evolve, int swarm, float lead, float hold, float zoom)
    {
        Debug.Log("[CAP] blast " + hero + " stage " + stage + " evolve " + evolve + " frames " + (rec != null ? rec.frames : 0));
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        Evolve(evolve);
        Swarm(stage, swarm);
        Swarm(stage, swarm / 2);
        rec.autopilot = true;
        yield return F(1.1f);                       // 적이 몰려올 시간 (녹화 안 함)
        rec.baseZoom = zoom;
        rec.recording = true;
        Hit(1f, 0.6f, 0.9f);                       // 장면이 바뀔 때마다 번쩍 · 쿵
        yield return F(lead);
        yield return Ult(hold);
        Hit(1.2f, 1.4f, 0.5f);
        rec.StartCoroutine(SlowMo(0.3f, 1.1f));
        yield return F(1.5f);
        Hit(0.6f, 0.8f, 0f);
        yield return F(0.7f);
    }

    // 보스 결계가 펼쳐지고 → 보스를 쓰러뜨려 결계가 산산조각
    static IEnumerator BarrierBreak(string hero, int who, int stage, int evolve)
    {
        Debug.Log("[CAP] barrier " + hero + " stage " + stage + " frames " + (rec != null ? rec.frames : 0));
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        Evolve(evolve);
        object b = SpawnBoss(stage, 0.8f);
        rec.autopilot = true;
        yield return F(0.8f);
        Component bc = (Component)b;
        Component ult = bc.GetComponent(T("BossUltimate"));
        if (ult == null) ult = bc.gameObject.AddComponent(T("BossUltimate"));
        yield return null;
        ((MonoBehaviour)ult).StartCoroutine((IEnumerator)Call(ult, "Run"));
        yield return F(3.6f);                       // 외침 화면(글자)은 찍지 않음
        rec.baseZoom = 0.95f;
        rec.recording = true;
        Hit(1f, 1f, 1f);
        yield return F(2.6f);                       // 결계 안에서 쏟아지는 공격
        yield return Ult(0.3f);
        yield return F(0.5f);
        Call(b, "TakeDamage", 9999999f, 0f, Vector3.zero);     // 결계가 깨지는 순간
        Hit(1.5f, 2f, 1f);
        rec.StartCoroutine(SlowMo(0.25f, 1.6f));
        yield return F(2.2f);
    }

    [UnityTest]
    public IEnumerator HighlightReel()
    {
        Debug.Log("[CAP] highlight start");
        LogAssert.ignoreFailingMessages = true;
        Directory.CreateDirectory(Out);
        Time.captureFramerate = Fps;
        In("Auto", true);
        AudioListener.volume = 0f;
        SetGameLanguage(Lang);
        GameObject host = new GameObject("CaptureRec");
        UnityEngine.Object.DontDestroyOnLoad(host);
        rec = host.AddComponent<Rec>();
        rec.musicPath = "TrailerMusic";             // 직접 합성한 웅장한 트레일러 음악 (make_trailer_music.py, 장면 전환 · 필살기 · 결계 파괴에 충격음을 맞춤)
        rec.musicVolume = 1f;
        BuildOverlay();
        rec.sub.text = "";
        rec.black.color = new Color(0f, 0f, 0f, 0f);

        yield return Load("Gunner", 1);
        Debug.Log("[CAP] loaded");
        rec.Begin(Path.Combine(Out, "SoulSaver_Highlight.mp4"));

        int dual = 2, shotgun = 0, chain = 5, grenade = 7;      // SpecialAbilities: DualId · ShotgunId · ChainId · GrenadeId
        yield return Blast("Gunner", 0, 1, dual, 34, 0.5f, 0.05f, 0.9f);            // 총알 폭풍
        yield return Blast("Swordsman", 1, 2, KitWeapon("Swordsman", 0), 30, 0.6f, 0.6f, 0.85f);
        yield return Blast("Alchemist", 4, 0, KitWeapon("Alchemist", 1), 30, 0.6f, 0.5f, 0.9f);
        yield return BarrierBreak("Archer", 3, 1, KitWeapon("Archer", 0));          // 지옥의 군주 결계 파괴
        yield return Blast("Gunner", 0, 3, chain, 30, 0.5f, 1.0f, 0.9f);            // 뇌운
        yield return Blast("Rogue", 2, 1, KitWeapon("Rogue", 0), 30, 0.5f, 0.5f, 0.85f);
        yield return Blast("Archer", 3, 2, KitWeapon("Archer", 1), 30, 0.5f, 0.9f, 0.9f);
        yield return BarrierBreak("Swordsman", 1, 0, KitWeapon("Swordsman", 1));    // 리치 왕 결계 파괴
        yield return Blast("Gunner", 0, 1, grenade, 40, 0.4f, 0.8f, 0.85f);         // 용암 융단폭격으로 마무리
        yield return Blast("Gunner", 0, 2, shotgun, 40, 0.4f, 0.8f, 0.85f);

        // 로고만 (자막 없음)
        rec.autopilot = false;
        yield return Fade(0f, 1f, 0.3f);
        Hit(0f, 0f, 1f);
        for (int i = 0; i <= Fps; i++)
        {
            float k = Mathf.SmoothStep(0f, 1f, i / (float)Fps);
            rec.logo.alpha = k;
            rec.logo.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, k);
            yield return null;
        }
        int tail = Fps * 2;
        for (int i = 0; i < tail; i++) { rec.musicVolume = 1f - i / (float)tail; yield return null; }

        rec.End();
        foreach (string k in skinKeys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
        Time.captureFramerate = 0;
        Time.timeScale = 1f;
        In("Auto", false);
        Debug.Log("[CAP] done frames=" + rec.frames + " (" + (rec.frames / (float)Fps).ToString("0.0") + "s)");
        yield return null;
    }

    // ================================================================= 레딧 홍보 영상 15초 (record.ps1 -Mode reddit)
    // 트레일러처럼 적에게 딱 붙지 않고 실제 플레이처럼 거리를 두고 돌며 싸움 · 영웅 필살기 · 보스 궁극기(결계) · 거울의 흉내만 빠르게
    static IEnumerator Clip(string hero, int who, int stage, int evolve, int swarm, float lead, float hold, float after, float zoom)
    {
        Debug.Log("[CAP] clip " + hero + " stage " + stage + " frames " + (rec != null ? rec.frames : 0));
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        Evolve(evolve);
        Swarm(stage, swarm);
        Swarm(stage, swarm / 2);
        rec.autopilot = true;
        yield return F(1.3f);                       // 적이 몰려오고 영웅이 거리를 벌릴 시간 (녹화 안 함)
        rec.baseZoom = zoom;
        rec.recording = true;
        Hit(0.8f, 0.4f, 0.6f);
        yield return F(lead);                       // 거리를 두고 돌며 쏘는 평소 전투
        yield return Ult(hold);
        Hit(1.1f, 1.2f, 0.4f);
        rec.StartCoroutine(SlowMo(0.35f, 0.9f));
        yield return F(after);
    }

    // 보스 궁극기: 결계 안에 쏟아지는 공격을 거리를 두고 피하며 버티다 필살기 → 보스가 쓰러지며 결계가 깨짐
    static IEnumerator ClipBarrier(string hero, int who, int stage, int evolve)
    {
        Debug.Log("[CAP] clip barrier " + hero + " stage " + stage + " frames " + (rec != null ? rec.frames : 0));
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        Evolve(evolve);
        object b = SpawnBoss(stage, 0.8f);
        rec.autopilot = true;
        yield return F(0.8f);
        Component bc = (Component)b;
        Component ult = bc.GetComponent(T("BossUltimate"));
        if (ult == null) ult = bc.gameObject.AddComponent(T("BossUltimate"));
        yield return null;
        ((MonoBehaviour)ult).StartCoroutine((IEnumerator)Call(ult, "Run"));
        yield return F(3.6f);                       // 외침 화면(글자)은 찍지 않음
        rec.baseZoom = 1.05f;
        rec.recording = true;
        Hit(1f, 1f, 1f);
        yield return F(2.0f);                       // 결계 안에서 쏟아지는 보스 공격
        yield return Ult(0.3f);
        yield return F(0.4f);
        Call(b, "TakeDamage", 9999999f, 0f, Vector3.zero);
        Hit(1.5f, 2f, 1f);
        rec.StartCoroutine(SlowMo(0.25f, 1.3f));
        yield return F(1.5f);
    }

    // 거울의 군주가 영웅의 필살기를 그대로 흉내 냄 → 맞받아 필살기
    static IEnumerator ClipMirror(string hero, int who)
    {
        Debug.Log("[CAP] clip mirror " + hero + " frames " + (rec != null ? rec.frames : 0));
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, 3);
        Component b = (Component)SpawnBoss(3, 0.8f);
        yield return null;
        Component skills = b != null ? b.GetComponent(T("BossSkills")) : null;
        if (skills != null) Set(skills, "next", Time.time + 999f);
        rec.autopilot = true;
        yield return F(1.0f);
        rec.baseZoom = 1.05f;
        rec.recording = true;
        Hit(0.8f, 0.5f, 0.7f);
        if (skills != null) ((MonoBehaviour)skills).StartCoroutine((IEnumerator)Call(skills, "Run", Call(skills, "MirrorUlt", true, true)));
        yield return F(1.6f);                       // 보스가 내 필살기를 흉내
        yield return Ult(0.4f);
        Hit(1f, 1f, 0.3f);
        yield return F(0.6f);
    }

    [UnityTest]
    public IEnumerator RedditClip()
    {
        Debug.Log("[CAP] reddit start");
        LogAssert.ignoreFailingMessages = true;
        Directory.CreateDirectory(Out);
        Time.captureFramerate = Fps;
        In("Auto", true);
        AudioListener.volume = 0f;
        SetGameLanguage(Lang);
        GameObject host = new GameObject("CaptureRec");
        UnityEngine.Object.DontDestroyOnLoad(host);
        rec = host.AddComponent<Rec>();
        rec.musicPath = "TrailerMusic";
        rec.musicVolume = 1f;
        // 실제 플레이처럼 거리를 둠 (잡몹 5.5 ~ 8.5칸, 보스 7.5 ~ 10칸)
        rec.nearMob = 5.5f; rec.farMob = 8.5f; rec.nearBoss = 7.5f; rec.farBoss = 10f;
        BuildOverlay();
        rec.sub.text = "";
        rec.black.color = new Color(0f, 0f, 0f, 0f);

        yield return Load("Gunner", 1);
        rec.Begin(Path.Combine(Out, "SoulSaver_Reddit15.mp4"));

        int dual = 2, grenade = 7;                  // SpecialAbilities: DualId · GrenadeId
        yield return Clip("Gunner", 0, 1, dual, 34, 0.7f, 0.05f, 1.4f, 1.05f);                       // 약 2.2초: 쌍권총 총알 폭풍
        yield return ClipBarrier("Archer", 3, 1, KitWeapon("Archer", 0));                           // 약 4초: 지옥의 군주 결계 → 파괴
        yield return Clip("Swordsman", 1, 2, KitWeapon("Swordsman", 0), 30, 0.6f, 0.5f, 1.1f, 1.0f); // 약 2.2초
        yield return ClipMirror("Rogue", 2);                                                       // 약 2.6초: 거울의 군주가 필살기 흉내
        yield return Clip("Gunner", 0, 2, grenade, 40, 0.5f, 0.7f, 1.1f, 1.0f);                     // 약 2.3초: 융단폭격 마무리

        // 로고 (약 1.3초)
        rec.autopilot = false;
        yield return Fade(0f, 1f, 0.2f);
        Hit(0f, 0f, 1f);
        int n = Fps * 2 / 3;
        for (int i = 0; i <= n; i++)
        {
            float k = Mathf.SmoothStep(0f, 1f, i / (float)n);
            rec.logo.alpha = k;
            rec.logo.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, k);
            yield return null;
        }
        for (int i = 0; i < n; i++) { rec.musicVolume = 1f - i / (float)n; yield return null; }

        rec.End();
        foreach (string k in skinKeys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
        Time.captureFramerate = 0;
        Time.timeScale = 1f;
        In("Auto", false);
        Debug.Log("[CAP] done frames=" + rec.frames + " (" + (rec.frames / (float)Fps).ToString("0.0") + "s)");
        yield return null;
    }

    static IEnumerator Delayed(float s, Action a) { yield return F(s); a(); }
}
