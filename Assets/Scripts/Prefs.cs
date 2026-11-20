using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// 저장 (2.1.9~): PlayerPrefs(윈도우 레지스트리) 대신 파일 하나 — 스팀 클라우드(자동 클라우드)가 다른 PC로 옮길 수 있게
//   %LOCALAPPDATA%\<제품 이름>\save.json (회사 이름이 한글이라 Unity 기본 저장 폴더 대신 영문 경로 · 에디터는 _Editor 를 붙여 따로)
//   함수는 PlayerPrefs 와 같음 (GetInt · SetInt · GetFloat · SetFloat · GetString · SetString · HasKey · DeleteKey · Save)
// 옛 저장 옮기기: 파일에 없는 키를 읽으면 레지스트리(PlayerPrefs)에 있던 값을 가져와 파일에 넣음 (모든 키를 한 번에 알 수 없어 읽을 때마다)
// Save() 는 바뀐 게 있을 때만 파일을 씀 (임시 파일에 쓰고 바꿔치기 · 게임을 끌 때도 씀)
// 1.0.4~: 바꿔치기 때 지난 저장을 save.json.bak 으로 남기고, 본 파일이 깨졌으면 .tmp → .bak 순서로 되살리고 메인 메뉴에 알림
public static class Prefs
{
    [System.Serializable]
    class Store
    {
        public int version = 1;
        public List<string> keys = new List<string>();
        public List<string> types = new List<string>();      // i 정수 · f 실수 · s 글자
        public List<string> values = new List<string>();
    }

    static Dictionary<string, (char type, string value)> data;
    static bool dirty, hooked;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string FolderPath
    {
        get
        {
            string root = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string name = Application.productName + (Application.isEditor ? "_Editor" : "");
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return Path.Combine(root, name);
        }
    }
    public static string FilePath => Path.Combine(FolderPath, "save.json");

    // 1.0.4~: 읽기에 실패했을 때 메인 메뉴가 띄울 알림 (null = 문제 없음) · Recovered = 백업에서 되살림
    public static string LoadProblem { get; private set; }
    public static bool Recovered { get; private set; }
    public static string BrokenPath { get; private set; }
    public static void ClearLoadProblem() { LoadProblem = null; }

    // 2.0.5~ 저장을 되살린 일 기록 ("yyyyMMdd:종류|…", 판 기록 · 텔레메트리 save_events 칸) — 저장이 왜 · 얼마나 자주 날아가는지 보려고
    public const string EventsKey = "save.events";
    public static void NoteEvent(string tag)
    {
        string s = GetString(EventsKey, "");
        s = (s.Length > 0 ? s + "|" : "") + System.DateTime.UtcNow.ToString("yyyyMMdd", Inv) + ":" + tag;
        if (s.Length > 300) s = s.Substring(s.Length - 300);
        SetString(EventsKey, s);
    }

    static void Load()
    {
        if (data != null) return;
        Store keep = null;                  // LoadFiles 가 되살리며 바로 쓰기 전에 keepProgress 를 알아야 .keep 을 덮지 않음
        try { if (File.Exists(KeepPath)) keep = ReadStore(KeepPath); }
        catch (System.Exception e) { Debug.LogWarning("[Prefs] .keep 을 읽지 못함: " + e.Message); }
        if (keep != null) keepProgress = Progress(keep.keys, keep.values);
        LoadFiles();
        RestoreFromKeep(keep);
    }

    // 2.0.5~ save.json.keep: 해금 · 클리어 수(Progress)가 지금까지 가장 많은 마지막 저장 — 진행이 줄어든 저장으로는 덮지 않음
    //   (.bak 은 저장할 때마다 바로 전 내용이라, 저장이 날아간 뒤 두 번만 저장해도 빈 내용으로 덮였음)
    //   게임 안에 저장 초기화가 없고 해금 · 클리어는 줄지 않으므로, 본 저장보다 .keep 이 많으면 저장이 날아간 것 → .keep 으로 되살림
    //   처음부터 다시 하려면 저장 폴더를 통째로 지워야 함 (스팀 클라우드도 save.json* 을 함께 옮김)
    static int keepProgress = -1;         // -1 = .keep 없음 (Write 가 잠금 안에서 읽고 씀)
    static string KeepPath => FilePath + ".keep";

    static int Progress(List<string> keys, List<string> values)
    {
        int n = 0;
        for (int i = 0; i < keys.Count && i < values.Count; i++)
        {
            string k = keys[i];
            if (values[i] == "1" && (k.StartsWith("char.unlocked.") || k.StartsWith("mode.cleared.") || k == "endless.cleared" || k == "dev.defeated")) n++;
        }
        return n;
    }

    static void RestoreFromKeep(Store k)
    {
        if (k == null) return;
        var keys = new List<string>(data.Keys);
        var values = new List<string>();
        foreach (string key in keys) values.Add(data[key].value);
        int now = Progress(keys, values);
        if (keepProgress <= now) return;
        data.Clear();
        Fill(k);
        Recovered = true;
        LoadProblem = "저장 진행이 사라진 것을 발견해 백업에서 되살렸습니다.";
        NoteEvent("keep" + now + ">" + keepProgress);
        dirty = true;
        Debug.LogWarning("[Prefs] 진행이 줄어든 저장 → .keep 에서 되살림 (" + now + " → " + keepProgress + ")");
        Save();                                                             // 지금 본 파일은 File.Replace 로 .bak 에 남음
    }

    static void LoadFiles()
    {
        data = new Dictionary<string, (char, string)>();
        if (!hooked) { hooked = true; Application.quitting += Save; }
        // 1.0.4~: 본 파일 → 쓰다가 꺼졌을 때 남는 임시 파일(.tmp) → 지난 저장 백업(.bak) 차례로 시도 (예전엔 본 파일이 깨지면 빈 저장으로 덮어씀)
        string main = FilePath;
        bool mainExists = File.Exists(main);
        if (mainExists && TryRead(main)) return;
        // 2.0.5~ 본 파일이 없어도 .bak 을 시도 (게임 안에 저장 초기화가 없어, 본 파일만 없는 건 사고로 봄)
        foreach (string backup in new[] { main + ".tmp", main + ".bak" })
        {
            if (!File.Exists(backup) || !TryRead(backup)) continue;
            if (mainExists)
            {
                KeepBroken(main);
                Recovered = true;
                LoadProblem = "저장 파일이 손상되어 백업에서 되살렸습니다. 최근 진행 일부가 빠졌을 수 있습니다.";
            }
            else if (backup.EndsWith(".bak"))
            {
                Recovered = true;
                LoadProblem = "저장 파일이 없어 백업에서 되살렸습니다. 최근 진행 일부가 빠졌을 수 있습니다.";
            }
            NoteEvent((mainExists ? "broken-" : "missing-") + backup.Substring(backup.Length - 3));
            dirty = true;                                                   // 되살린 내용을 본 파일에 다시 씀
            Debug.LogWarning("[Prefs] 저장 파일을 백업에서 되살림: " + backup);
            // 1.5.9~ 다음 Save() 까지 미루지 않고 바로 씀 — 그 전에 튕기면 다음 실행에서 본 파일이 없어 「처음 켬」으로 빈 저장이 시작되고,
            //   두 번째 저장이 .bak 마저 빈 내용으로 덮었음. 본 파일이 옮겨진 뒤에만 (File.Move 를 타서 .bak 을 건드리지 않음 ·
            //   옮기기에 실패해 깨진 본 파일이 남아 있으면 File.Replace 가 깨진 내용을 .bak 으로 보내므로 쓰지 않음 — 예전처럼 다음 Save 때)
            if (!File.Exists(main))
            {
                Store s = Snapshot(out string folder, out string file, out int seq);
                if (s != null) Write(s, folder, file, seq);
            }
            return;
        }
        if (!mainExists) return;                                            // 처음 켬 (저장 없음)
        // 다 실패: 깨진 파일은 지우지 않고 옆에 남겨 두고 빈 저장으로 (다음 저장이 .bak 을 깨진 내용으로 덮지 않게 본 파일은 옮김)
        KeepBroken(main);
        NoteEvent("broken-none");
        LoadProblem = "저장 파일을 읽지 못해 새 저장으로 시작합니다. 깨진 파일은 지우지 않고 저장 폴더에 남겨 두었습니다.";
    }

    // 파일 하나를 읽어 data 에 채움 (내용이 깨졌으면 false, data 는 비운 채로)
    static bool TryRead(string path)
    {
        try
        {
            Fill(ReadStore(path));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Prefs] 저장 파일을 읽지 못함 (" + Path.GetFileName(path) + "): " + e.Message);
            data.Clear();
            return false;
        }
    }

    static Store ReadStore(string path)
    {
        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json) || json.TrimStart()[0] != '{') throw new System.FormatException("JSON 이 아님");
        Store s = JsonUtility.FromJson<Store>(json);
        if (s == null || s.keys == null || s.types == null || s.values == null) throw new System.FormatException("내용 없음");
        return s;
    }

    static void Fill(Store s)
    {
        for (int i = 0; i < s.keys.Count && i < s.types.Count && i < s.values.Count; i++)
            if (!string.IsNullOrEmpty(s.keys[i]) && !string.IsNullOrEmpty(s.types[i])) data[s.keys[i]] = (s.types[i][0], s.values[i]);
    }

    // 깨진 본 파일을 save.json.broken-<시각> 으로 옮겨 둠 (나중에 손으로 되살릴 수 있게)
    static void KeepBroken(string main)
    {
        try
        {
            string to = main + ".broken-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss", Inv);
            File.Move(main, to);
            BrokenPath = to;
        }
        catch (System.Exception e) { Debug.LogWarning("[Prefs] 깨진 저장 파일을 옮기지 못함: " + e.Message); }
    }

    // 1.2~ 옛 레지스트리에 없다고 한 번 확인한 열쇠 (없는 열쇠를 자주 읽는 곳 — 보드 칸 · 계약 — 이 매번 레지스트리를 조회하지 않게)
    //   레지스트리는 옛 저장을 옮겨 올 때만 읽으므로 새로 생기지 않음 · 파일 쪽 값은 data 가 먼저 봄
    static readonly HashSet<string> notInRegistry = new HashSet<string>();
    static bool NotInRegistry(string key)
    {
        if (notInRegistry.Contains(key)) return true;
        if (PlayerPrefs.HasKey(key)) return false;
        notInRegistry.Add(key);
        return true;
    }

    // ---------------- 읽기 (파일에 없으면 옛 레지스트리 값을 옮겨 옴)
    public static int GetInt(string key, int def = 0)
    {
        Load();
        if (data.TryGetValue(key, out var e)) return int.TryParse(e.value, NumberStyles.Integer, Inv, out int v) ? v : def;
        if (NotInRegistry(key)) return def;
        int old = PlayerPrefs.GetInt(key, def);
        data[key] = ('i', old.ToString(Inv));
        dirty = true;
        return old;
    }

    public static float GetFloat(string key, float def = 0f)
    {
        Load();
        if (data.TryGetValue(key, out var e)) return float.TryParse(e.value, NumberStyles.Float, Inv, out float v) ? v : def;
        if (NotInRegistry(key)) return def;
        float old = PlayerPrefs.GetFloat(key, def);
        data[key] = ('f', old.ToString("R", Inv));
        dirty = true;
        return old;
    }

    public static string GetString(string key, string def = "")
    {
        Load();
        if (data.TryGetValue(key, out var e)) return e.value ?? def;
        if (NotInRegistry(key)) return def;
        string old = PlayerPrefs.GetString(key, def);
        data[key] = ('s', old);
        dirty = true;
        return old;
    }

    public static bool HasKey(string key)
    {
        Load();
        return data.ContainsKey(key) || PlayerPrefs.HasKey(key);
    }

    // ---------------- 쓰기
    public static void SetInt(string key, int value) => Set(key, 'i', value.ToString(Inv));
    public static void SetFloat(string key, float value) => Set(key, 'f', value.ToString("R", Inv));
    public static void SetString(string key, string value) => Set(key, 's', value ?? "");

    static void Set(string key, char type, string value)
    {
        Load();
        if (data.TryGetValue(key, out var e) && e.type == type && e.value == value) return;
        data[key] = (type, value);
        dirty = true;
    }

    // 옛 레지스트리 값도 지움 (지운 키가 다음에 읽을 때 되살아나지 않게)
    public static void DeleteKey(string key)
    {
        Load();
        if (data.Remove(key)) dirty = true;
        if (PlayerPrefs.HasKey(key)) PlayerPrefs.DeleteKey(key);
    }

    // 1.1.9~ 마지막 저장이 실패했으면 그 까닭 (성공하면 null) — 판 저장이 「저장하지 못했습니다」 알림을 띄움 (RunSave)
    public static string LastError { get; private set; }

    public static void Save()
    {
        Store s = Snapshot(out string folder, out string file, out int seq);
        if (s != null) Write(s, folder, file, seq);
    }

    // 최적화: 판 중 자동 저장(1분)용 — 값 목록만 지금 복사하고 JSON 만들기 · 파일 쓰기는 백그라운드에서 (주기적 끊김 방지)
    //   끌 때 · 씬 전환 등 다른 Save() 는 그대로 바로 씀. 쓰기는 잠금으로 한 번에 하나, 더 새 저장이 이미 쓰였으면 오래된 것은 버림
    public static void SaveInBackground()
    {
        Store s = Snapshot(out string folder, out string file, out int seq);
        if (s != null) System.Threading.Tasks.Task.Run(() => Write(s, folder, file, seq));
    }

    static readonly object writeLock = new object();
    static int snapSeq, writtenSeq;

    // 메인 스레드: 지금 값의 복사본 (Application.productName 등 경로도 여기서)
    static Store Snapshot(out string folder, out string file, out int seq)
    {
        folder = file = null;
        seq = 0;
        if (data == null || !dirty) return null;
        Store s = new Store();
        foreach (KeyValuePair<string, (char type, string value)> kv in data)
        {
            s.keys.Add(kv.Key);
            s.types.Add(kv.Value.type.ToString());
            s.values.Add(kv.Value.value);
        }
        folder = FolderPath;
        file = FilePath;
        seq = ++snapSeq;
        dirty = false;                  // 쓰다 실패하면 Write 가 다시 true
        return s;
    }

    static void Write(Store s, string folder, string file, int seq)
    {
        lock (writeLock)
        {
            if (seq < writtenSeq) return;           // 더 새 저장이 이미 쓰임
            try
            {
                Directory.CreateDirectory(folder);
                string tmp = file + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(s));
                // 한 번에 바꿔치기 (중간에 꺼져도 둘 중 하나는 남음) · 1.0.4~: 바로 전 저장은 save.json.bak 으로 남김 (본 파일이 깨지면 이걸로 되살림)
                if (File.Exists(file)) File.Replace(tmp, file, file + ".bak");
                else File.Move(tmp, file);
                writtenSeq = seq;
                int p = Progress(s.keys, s.values);
                if (p >= keepProgress) { File.Copy(file, file + ".keep", true); keepProgress = p; }
                LastError = null;
            }
            catch (System.Exception e) { dirty = true; LastError = e.Message; Debug.LogWarning("[Prefs] 저장 파일을 쓰지 못함: " + e.Message); }
        }
    }
}
