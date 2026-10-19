using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// 1.4.4~ 플레이 데이터 보내기 (동의한 이용자만)
//   판이 끝날 때 balance_log.csv 한 줄과 같은 칸 · 값을 JSON 한 줄로 만들어 큐(persistentDataPath/telemetry_queue.jsonl)에 쌓고
//   Cloudflare Worker(server/telemetry) 로 하나씩 보냄. 실패하면 다음 판 끝 · 다음 실행에 다시
//   자유 텍스트 · 기기 이름 · 경로 · SteamID · 이메일은 넣지 않음 · 날짜는 일 단위(UTC)
//   에디터 · 자동 플레이 · 트레일러 · 배치 모드에서는 보내지 않음
public static class Telemetry
{
    // TODO: 서버(server/telemetry) 배포 후 Worker 주소 (예: https://soulsaver-telemetry.<계정>.workers.dev) — 비어 있으면 아무것도 안 보내고 동의창 · 설정 줄도 숨김
    public const string Endpoint = "https://soulsaver-telemetry.soulsaver.workers.dev";
    // 공개 키 (Worker 의 INGEST_KEY 와 같게 · 비밀이 아님, 아무나 막 보내는 것만 줄임) — 비워 두면 헤더를 안 붙임
    public const string IngestKey = "0b288103af20d0fc319533b112deddd0";
    // 동의문이 바뀌면 +1 → 저장된 동의가 이보다 낮으면 다시 물음
    public const int ConsentVersion = 1;
    // TODO: 문의 메일 (동의문 「연락처」에 들어감) — 비어 있으면 「스팀 상점 페이지의 개발자 문의」로 대체
    public const string ContactEmail = "mingeunb0@gmail.com";

    const string ConsentKey = "telemetry.consent";          // 0 미정 · 1 동의 · 2 거부
    const string ConsentVerKey = "telemetry.consent_ver";
    const string AnonKey = "telemetry.anon_id";
    const int MaxLines = 200;
    const int MaxBytes = 1024 * 1024;
    const int MaxLineBytes = 15000;                          // 서버 상한 16KB 보다 조금 작게
    const int MaxFlushPerCall = 50;                          // 한 번에 보내는 줄 수 상한 (대기 상한)

    public static bool Configured => !string.IsNullOrEmpty(Endpoint);
    // 보내지 않는 환경 (WriteBalanceLog 의 금지 조건 + 에디터)
    static bool Blocked => Application.isEditor || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode;
    static string QueuePath => System.IO.Path.Combine(Application.persistentDataPath, "telemetry_queue.jsonl");
    static string Base => Endpoint.TrimEnd('/');

    // 0 미정 · 1 동의 · 2 거부 (저장된 동의문 버전이 낮으면 미정)
    public static int Consent
    {
        get
        {
            int c = Prefs.GetInt(ConsentKey, 0);
            if (c != 1 && c != 2) return 0;
            return Prefs.GetInt(ConsentVerKey, 0) < ConsentVersion ? 0 : c;
        }
    }

    public static string AnonId => Prefs.GetString(AnonKey, "");
    public static string ShortId { get { string id = AnonId; return id.Length >= 8 ? id.Substring(0, 8) : "-"; } }

    public static void SetConsent(bool on)
    {
        Prefs.SetInt(ConsentKey, on ? 1 : 2);
        Prefs.SetInt(ConsentVerKey, ConsentVersion);
        if (on && AnonId.Length == 0) NewAnonId();
        Prefs.Save();
        if (on) Flush();
        else ClearQueue();          // 철회: 아직 안 보낸 줄은 버림 (익명 ID 는 삭제 요청에 필요하므로 남김)
    }

    static void NewAnonId() => Prefs.SetString(AnonKey, System.Guid.NewGuid().ToString("D"));

    static string LangCode
    {
        get
        {
            switch (Loc.Current)
            {
                case Loc.Lang.English: return "en";
                case Loc.Lang.Japanese: return "ja";
                case Loc.Lang.Chinese: return "zh";
                default: return "ko";
            }
        }
    }

    // 게임 시작 시 남은 큐 보내기
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnLaunch()
    {
        if (Configured && !Blocked && Consent == 1) Flush();
    }

    // ================================================================= 판 끝 (RunStats.WriteBalanceLog 와 같은 시점 · 같은 줄)
    // header · row: balance_log.csv 의 머리줄 · 이번 판 줄 (쉼표로 나뉨)
    public static void RecordRun(string header, string row)
    {
        if (!Configured || Blocked || Consent != 1) return;
        try
        {
            if (AnonId.Length == 0) { NewAnonId(); Prefs.Save(); }
            string[] keys = header.Split(',');
            string[] vals = row.Split(',');
            StringBuilder j = new StringBuilder(2048);
            j.Append('{');
            j.Append("\"schema\":1");
            Str(j, "build", Demo.On ? "demo" : "full");
            Str(j, "anon_id", AnonId);
            j.Append(",\"consent_ver\":").Append(ConsentVersion);
            Str(j, "platform", Application.platform.ToString());
            Str(j, "lang", LangCode);
            HashSet<string> seen = new HashSet<string> { "schema", "build", "anon_id", "consent_ver", "platform", "lang" };
            int n = Mathf.Min(keys.Length, vals.Length);
            for (int i = 0; i < n; i++)
            {
                string k = keys[i].Trim();
                if (k.Length == 0 || !seen.Add(k)) continue;
                string v = vals[i].Trim();
                if (k == "date") v = System.DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                if (k != "date" && k != "version" && IsNumber(v)) j.Append(",\"").Append(Escape(k)).Append("\":").Append(v);
                else Str(j, k, v.Length > 2000 ? v.Substring(0, 2000) : v);
            }
            j.Append('}');
            string line = j.ToString();
            if (Encoding.UTF8.GetByteCount(line) > MaxLineBytes) return;        // 너무 긴 판은 보내지 않음 (서버가 어차피 거절)
            Append(line);
            Flush();
        }
        catch (System.Exception ex) { Debug.LogWarning("telemetry record: " + ex.Message); }
    }

    static void Str(StringBuilder j, string k, string v) => j.Append(",\"").Append(Escape(k)).Append("\":\"").Append(Escape(v)).Append('"');

    static bool IsNumber(string v)
    {
        if (string.IsNullOrEmpty(v) || v.Length > 15) return false;
        int i = v[0] == '-' ? 1 : 0;
        if (i >= v.Length) return false;
        if (v[i] == '0' && i + 1 < v.Length && v[i + 1] != '.') return false;     // JSON 은 앞자리 0 금지
        bool dot = false, digit = false;
        for (; i < v.Length; i++)
        {
            char c = v[i];
            if (c >= '0' && c <= '9') { digit = true; continue; }
            if (c == '.' && !dot && digit && i + 1 < v.Length) { dot = true; continue; }
            return false;
        }
        return digit;
    }

    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        StringBuilder b = new StringBuilder(s.Length + 8);
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': b.Append("\\\""); break;
                case '\\': b.Append("\\\\"); break;
                case '\n': b.Append("\\n"); break;
                case '\r': b.Append("\\r"); break;
                case '\t': b.Append("\\t"); break;
                default:
                    if (c < 0x20) b.Append("\\u").Append(((int)c).ToString("x4"));
                    else b.Append(c);
                    break;
            }
        }
        return b.ToString();
    }

    // ================================================================= 큐 파일 (한 줄 = 한 판)
    static List<string> ReadQueue()
    {
        List<string> lines = new List<string>();
        try
        {
            if (!System.IO.File.Exists(QueuePath)) return lines;
            foreach (string l in System.IO.File.ReadAllLines(QueuePath, Encoding.UTF8))
                if (!string.IsNullOrWhiteSpace(l)) lines.Add(l);
        }
        catch (System.Exception ex) { Debug.LogWarning("telemetry queue read: " + ex.Message); }
        return lines;
    }

    static void WriteQueue(List<string> lines)
    {
        try
        {
            if (lines.Count == 0) { if (System.IO.File.Exists(QueuePath)) System.IO.File.Delete(QueuePath); return; }
            string tmp = QueuePath + ".tmp";
            System.IO.File.WriteAllLines(tmp, lines, new UTF8Encoding(false));
            if (System.IO.File.Exists(QueuePath)) System.IO.File.Delete(QueuePath);
            System.IO.File.Move(tmp, QueuePath);
        }
        catch (System.Exception ex) { Debug.LogWarning("telemetry queue write: " + ex.Message); }
    }

    // 끝에 한 줄 · 200줄 · 1MB 를 넘으면 오래된 것부터 버림
    static void Append(string line)
    {
        List<string> lines = ReadQueue();
        lines.Add(line);
        int bytes = 0;
        foreach (string l in lines) bytes += Encoding.UTF8.GetByteCount(l) + 1;
        while (lines.Count > 1 && (lines.Count > MaxLines || bytes > MaxBytes))
        {
            bytes -= Encoding.UTF8.GetByteCount(lines[0]) + 1;
            lines.RemoveAt(0);
        }
        WriteQueue(lines);
    }

    // 보낸(또는 버린) 줄을 지움 — 그사이 맨 앞이 바뀌었으면 같은 줄을 찾아 지움
    // 한 번 보내기 동안 모아 둔 줄들을 한 번에 지움 (파일 읽기 · 쓰기 한 번 · 그사이 덧붙은 줄은 남음)
    static void RemoveMany(List<string> done)
    {
        if (done.Count == 0) return;
        List<string> lines = ReadQueue();
        bool changed = false;
        foreach (string line in done)
        {
            int at = lines.IndexOf(line);
            if (at < 0) continue;
            lines.RemoveAt(at);
            changed = true;
        }
        if (changed) WriteQueue(lines);
    }

    static void ClearQueue() => WriteQueue(new List<string>());

    // ================================================================= 보내기 (동시에 하나 · 메인 스레드는 기다리지 않음)
    static Runner runner;
    static bool sending, again;

    static Runner Host()
    {
        if (runner != null) return runner;
        GameObject go = new GameObject("TelemetryRunner");
        go.hideFlags = HideFlags.HideInHierarchy;
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
        return runner;
    }

    class Runner : MonoBehaviour { }

    static void Flush()
    {
        if (!Configured || Blocked || Consent != 1 || deleting) return;
        if (sending) { again = true; return; }
        try { Host().StartCoroutine(FlushLoop()); }
        catch (System.Exception ex) { sending = false; Debug.LogWarning("telemetry flush: " + ex.Message); }
    }

    static IEnumerator FlushLoop()
    {
        sending = true;
        do
        {
            again = false;
            List<string> q = ReadQueue();          // 큐는 한 번만 읽음 (줄마다 파일 전체를 읽고 쓰지 않게)
            List<string> sent = new List<string>();
            for (int n = 0; n < MaxFlushPerCall && n < q.Count; n++)
            {
                if (!Configured || Blocked || Consent != 1 || deleting) break;
                string line = q[n];
                long code = 0;
                bool ok = false;
                UnityWebRequest req = null;
                try { req = Post(Base + "/v1/run", line); }
                catch (System.Exception ex) { Debug.LogWarning("telemetry send: " + ex.Message); }
                if (req == null) break;
                yield return req.SendWebRequest();
                try
                {
                    code = req.responseCode;
                    ok = req.result == UnityWebRequest.Result.Success && code >= 200 && code < 300;
                }
                catch { ok = false; }
                finally { req.Dispose(); }
                if (ok) { sent.Add(line); continue; }
                // 줄 자체가 잘못된 400 · 413 · 422 만 버림 (403 키 · 404 주소 · 415 등 설정 오류 · 408 · 429 는 다음 기회에 다시)
                if (code == 400 || code == 413 || code == 422) { sent.Add(line); continue; }
                break;          // 네트워크 · 서버 오류: 다음 판 끝 · 다음 실행에
            }
            if (!deleting) RemoveMany(sent);        // 삭제 요청 중이면 큐는 이미 비움
        } while (again);
        sending = false;
    }

    static UnityWebRequest Post(string url, string json)
    {
        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(IngestKey)) req.SetRequestHeader("X-Ingest-Key", IngestKey);
        req.timeout = 10;
        return req;
    }

    // ================================================================= 삭제 요청 (설정 > 일반)
    // done(true) = 서버가 지움 → 익명 ID 를 새로 만듦
    public static void RequestDelete(System.Action<bool> done)
    {
        string id = AnonId;
        if (id.Length == 0) { ClearQueue(); done?.Invoke(true); return; }     // 한 번도 동의하지 않음 = 보낸 기록 없음 (지울 것 없음)
        if (!Configured || deleting) { done?.Invoke(false); return; }
        ClearQueue();           // 지울 ID 의 남은 줄이 다시 올라가지 않게
        deleting = true;
        try { Host().StartCoroutine(DeleteLoop(id, done)); }
        catch (System.Exception ex) { deleting = false; Debug.LogWarning("telemetry delete: " + ex.Message); done?.Invoke(false); }
    }

    static bool deleting;

    static IEnumerator DeleteLoop(string id, System.Action<bool> done)
    {
        // 옛 ID 로 나가 있는 전송이 서버에서 삭제 뒤에 들어가지 않게, 진행 중인 전송이 끝나길 기다림 (상한 15초)
        float until = Time.unscaledTime + 15f;
        while (sending && Time.unscaledTime < until) yield return null;
        UnityWebRequest req = null;
        try { req = Post(Base + "/v1/delete", "{\"anon_id\":\"" + Escape(id) + "\"}"); }
        catch (System.Exception ex) { Debug.LogWarning("telemetry delete: " + ex.Message); }
        if (req == null) { deleting = false; done?.Invoke(false); yield break; }
        yield return req.SendWebRequest();
        deleting = false;
        bool ok = false;
        try { ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300; }
        catch { ok = false; }
        finally { req.Dispose(); }
        if (ok && AnonId == id)
        {
            NewAnonId();
            Prefs.Save();
            ClearQueue();
        }
        try { done?.Invoke(ok); }
        catch (System.Exception ex) { Debug.LogWarning("telemetry delete callback: " + ex.Message); }
    }

    // ================================================================= 동의문 (LocTable.Telemetry)
    public static string PolicyText()
    {
        string contact = string.IsNullOrEmpty(ContactEmail) ? Loc.T("telemetry.contact.fallback") : ContactEmail;
        return Loc.T("telemetry.policy").Replace("\r", "").Replace("{contact}", contact);
    }
}

// ===================================================================== 동의창
// 처음 메인 메뉴가 뜰 때 동의가 미정이면 (MenuExtras.InstallMainMenu → TelemetryConsentUI.InstallMenu)
// readOnly: 설정 「전문 보기」 — 닫기 버튼 하나
// 「동의」 「동의하지 않음」은 같은 크기 · 같은 색 · 처음엔 어느 쪽도 선택하지 않음 (←→ · Tab 을 누르면 그쪽 버튼부터 선택)
// 창이 떠 있는 동안 뒤 메뉴로 선택이 새지 않게 막음 · timeScale 은 건드리지 않음
public static class TelemetryConsentUI
{
    static GameObject open;
    public static bool IsOpen => open != null;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.78f, 0.74f, 0.66f);

    // 메뉴의 다른 창들 (이 창들이 다 닫힌 뒤에 띄움 · 소급 보상 창(ClaimRewards)도 먼저)
    static readonly string[] Blockers = { "SaveNotice", "LanguagePicker", "PatchNotes", "PatchNotesHistory", "ClaimRewards", "ClaimRewardsRunner",
                                          "CharacterPanel", "SettingsPanel", "SkinShop", "CodexPanel", "SkillEvoCodex", "DailyChallenge", "Leaderboard",
                                          "RunHistory", "HeroGuide", "BuildView", "MemoryReader", "SoulBrand", "SoulWeightWindow",
                                          "ContractBoard", "BoardClaim", "CodexRewards", "DailyFinish", "DemoEnd" };

    public static void InstallMenu(Transform root)
    {
        if (!Telemetry.Configured || Telemetry.Consent != 0 || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode || root == null) return;
        new GameObject("TelemetryConsentRunner").AddComponent<Waiter>().root = root;
    }

    class Waiter : MonoBehaviour
    {
        public Transform root;

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1f);            // 소급 보상 러너(0.4초)가 먼저 창을 띄우게
            float giveUp = Time.unscaledTime + 600f;                // 대기 상한 10분 (다음에 메뉴로 오면 다시 물음)
            while (Time.unscaledTime < giveUp)
            {
                if (root == null || Telemetry.Consent != 0) { Destroy(gameObject); yield break; }
                if (!Busy()) break;
                yield return new WaitForSecondsRealtime(0.25f);
            }
            if (root != null && Telemetry.Consent == 0 && !Busy()) Open(root, false);
            Destroy(gameObject);
        }

        static bool Busy()
        {
            if (open != null || StoryDirector.Playing || TutorialRun.Active || SettingsUI.IsOpen) return true;
            foreach (string b in Blockers) if (GameObject.Find(b) != null) return true;
            return false;
        }
    }

    public static void Open(Transform root, bool readOnly)
    {
        if (open != null || root == null) return;
        RectTransform win = UIKit.Modal(root, "TelemetryConsent", new Vector2(1240f, 900f), out open);
        GameObject self = open;

        UIKit.Text(win, "telemetry.title", 44f, Gold, new Vector2(0f, 395f), new Vector2(1000f, 60f));
        TMP_Text summary = UIKit.Text(win, "telemetry.summary", 24f, Parch, new Vector2(0f, 312f), new Vector2(1120f, 100f));
        summary.fontSizeMin = 16f;
        summary.enableWordWrapping = true;

        // 스크롤되는 전문 (패치노트 창과 같은 방식)
        RectTransform view = UIKit.Rect("Viewport", win, new Vector2(0f, -10f), new Vector2(1160f, 520f));
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        view.gameObject.AddComponent<RectMask2D>();
        RectTransform content = UIKit.Rect("Content", view, Vector2.zero, new Vector2(1110f, 0f));
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        TMP_Text body = UIKit.Text(content, "", 22f, Dim, Vector2.zero, new Vector2(1110f, 0f), TextAlignmentOptions.TopLeft);
        body.enableAutoSizing = false;
        body.fontSize = 22f;
        body.enableWordWrapping = true;
        body.lineSpacing = 6f;
        body.text = Telemetry.PolicyText();
        RectTransform br = body.rectTransform;
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 1f);
        br.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup v = content.gameObject.AddComponent<VerticalLayoutGroup>();
        v.childControlHeight = true;
        v.padding = new RectOffset(0, 0, 16, 16);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;

        Keys keys = self.AddComponent<Keys>();
        keys.scroll = scroll;
        if (readOnly)
        {
            Button close = UIKit.MakeButton(win, "닫기", new Vector2(0f, -360f), new Vector2(360f, 72f), Close, 28f);
            close.navigation = new Navigation { mode = Navigation.Mode.None };
            keys.left = keys.right = close;
            keys.escClose = true;
        }
        else
        {
            // 같은 크기 · 같은 색 (어느 쪽도 더 눈에 띄지 않게)
            Button yes = UIKit.MakeButton(win, "telemetry.agree", new Vector2(-210f, -360f), new Vector2(380f, 72f), () => Choose(true), 28f);
            Button no = UIKit.MakeButton(win, "telemetry.disagree", new Vector2(210f, -360f), new Vector2(380f, 72f), () => Choose(false), 28f);
            yes.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = no, selectOnLeft = no };
            no.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = yes, selectOnRight = yes };
            keys.left = yes;
            keys.right = no;
        }
        // 뒤 메뉴에서 선택돼 있던 버튼이 Enter 를 받지 않게
        if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    static void Choose(bool agree)
    {
        Telemetry.SetConsent(agree);
        Close();
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
        if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    // 키보드: ←→ · Tab 으로 버튼 선택(Enter · Space 로 누름), ↑↓ · PageUp/Down 으로 전문 스크롤 · 읽기 전용이면 ESC = 닫기
    // 동의창(읽기 전용 아님)에서 ESC 는 아무 일도 하지 않음 (먹기만 함 · 뒤 메뉴로 새지 않게)
    class Keys : MonoBehaviour
    {
        public ScrollRect scroll;
        public Button left, right;
        public bool escClose;

        void Update()
        {
            if (GameInput.EscDown && SettingsUI.EscHandledFrame != Time.frameCount)
            {
                SettingsUI.EscHandledFrame = Time.frameCount;
                if (escClose) { Close(); return; }
            }
            UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null)
            {
                GameObject sel = es.currentSelectedGameObject;
                bool mine = sel != null && ((left != null && sel == left.gameObject) || (right != null && sel == right.gameObject));
                if (sel != null && !mine) { es.SetSelectedGameObject(null); sel = null; }       // 뒤 메뉴로 선택이 새면 거둠
                if (sel == null && !GamePad.Active)
                {
                    if (Input.GetKeyDown(KeyCode.LeftArrow) && left != null) es.SetSelectedGameObject(left.gameObject);
                    else if ((Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Tab)) && right != null) es.SetSelectedGameObject(right.gameObject);
                }
            }
            if (scroll == null || scroll.content == null || scroll.viewport == null) return;
            float h = scroll.content.rect.height - scroll.viewport.rect.height;
            if (h <= 1f) return;
            float step = 0f;
            if (Input.GetKey(KeyCode.DownArrow)) step -= 900f * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.UpArrow)) step += 900f * Time.unscaledDeltaTime;
            if (Input.GetKeyDown(KeyCode.PageDown)) step -= scroll.viewport.rect.height * 0.9f;
            if (Input.GetKeyDown(KeyCode.PageUp)) step += scroll.viewport.rect.height * 0.9f;
            if (step != 0f) scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + step / h);
        }
    }
}
