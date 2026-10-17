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

    static void Load()
    {
        if (data != null) return;
        data = new Dictionary<string, (char, string)>();
        if (!hooked) { hooked = true; Application.quitting += Save; }
        // 1.0.4~: 본 파일 → 쓰다가 꺼졌을 때 남는 임시 파일(.tmp) → 지난 저장 백업(.bak) 차례로 시도 (예전엔 본 파일이 깨지면 빈 저장으로 덮어씀)
        string main = FilePath;
        bool mainExists = File.Exists(main);
        if (mainExists && TryRead(main)) return;
        // (.bak 은 본 파일이 있는데 깨졌을 때만 · 일부러 save.json 을 지워 처음부터 하려는 경우는 되살리지 않음)
        foreach (string backup in mainExists ? new[] { main + ".tmp", main + ".bak" } : new[] { main + ".tmp" })
        {
            if (!File.Exists(backup) || !TryRead(backup)) continue;
            if (mainExists)
            {
                KeepBroken(main);
                Recovered = true;
                LoadProblem = "저장 파일이 손상되어 백업에서 되살렸습니다. 최근 진행 일부가 빠졌을 수 있습니다.";
            }
            dirty = true;                                                   // 되살린 내용을 본 파일에 다시 씀
            Debug.LogWarning("[Prefs] 저장 파일을 백업에서 되살림: " + backup);
            return;
        }
        if (!mainExists) return;                                            // 처음 켬 (저장 없음)
        // 다 실패: 깨진 파일은 지우지 않고 옆에 남겨 두고 빈 저장으로 (다음 저장이 .bak 을 깨진 내용으로 덮지 않게 본 파일은 옮김)
        KeepBroken(main);
        LoadProblem = "저장 파일을 읽지 못해 새 저장으로 시작합니다. 깨진 파일은 지우지 않고 저장 폴더에 남겨 두었습니다.";
    }

    // 파일 하나를 읽어 data 에 채움 (내용이 깨졌으면 false, data 는 비운 채로)
    static bool TryRead(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json) || json.TrimStart()[0] != '{') throw new System.FormatException("JSON 이 아님");
            Store s = JsonUtility.FromJson<Store>(json);
            if (s == null || s.keys == null || s.types == null || s.values == null) throw new System.FormatException("내용 없음");
            for (int i = 0; i < s.keys.Count && i < s.types.Count && i < s.values.Count; i++)
                if (!string.IsNullOrEmpty(s.keys[i]) && !string.IsNullOrEmpty(s.types[i])) data[s.keys[i]] = (s.types[i][0], s.values[i]);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Prefs] 저장 파일을 읽지 못함 (" + Path.GetFileName(path) + "): " + e.Message);
            data.Clear();
            return false;
        }
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

    // ---------------- 읽기 (파일에 없으면 옛 레지스트리 값을 옮겨 옴)
    public static int GetInt(string key, int def = 0)
    {
        Load();
        if (data.TryGetValue(key, out var e)) return int.TryParse(e.value, NumberStyles.Integer, Inv, out int v) ? v : def;
        if (!PlayerPrefs.HasKey(key)) return def;
        int old = PlayerPrefs.GetInt(key, def);
        data[key] = ('i', old.ToString(Inv));
        dirty = true;
        return old;
    }

    public static float GetFloat(string key, float def = 0f)
    {
        Load();
        if (data.TryGetValue(key, out var e)) return float.TryParse(e.value, NumberStyles.Float, Inv, out float v) ? v : def;
        if (!PlayerPrefs.HasKey(key)) return def;
        float old = PlayerPrefs.GetFloat(key, def);
        data[key] = ('f', old.ToString("R", Inv));
        dirty = true;
        return old;
    }

    public static string GetString(string key, string def = "")
    {
        Load();
        if (data.TryGetValue(key, out var e)) return e.value ?? def;
        if (!PlayerPrefs.HasKey(key)) return def;
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
            }
            catch (System.Exception e) { dirty = true; Debug.LogWarning("[Prefs] 저장 파일을 쓰지 못함: " + e.Message); }
        }
    }
}
