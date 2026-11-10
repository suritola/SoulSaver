using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 릴스 · 쇼츠용 세로 컷 촬영 (2026-11-11~, tools/trailer/reels-shoot.ps1)
//   옛 트레일러와 달리 여기서는 영상 · 소리를 섞지 않는다. 컷(테이크)마다
//     <컷>_s<씨앗>.mkv        프레임 (ffmpeg 표준 입력 · 모든 프레임이 열쇠 프레임 · 60fps 고정)
//     <컷>_s<씨앗>.cues.jsonl 몇 번째 프레임에 무슨 소리가 났는지 (게임이 낸 소리를 SpecialFeedback.Tapped 로 엿들음 + 총 · 장전)
//     <컷>_s<씨앗>.trace.csv  조준선 · 표적 · 이동 · 발사 기록 (tools/trailer/post/takes.py 가 「사람 같은가」 지표와 궤적 그림을 만듦)
//   만 남기고, 편집 · 음악 · 글자 · 음량은 tools/trailer/post/build.py 가 한다 (시계는 프레임 번호 하나).
//   조종은 「사람 손 모델」(Rec.HumanUpdate): 반응 지연 → 최소 저크 도약(피츠 법칙 · 휜 경로 · 끝점 오차) → 고쳐 잡기 → 뒤처진 따라가기 · 느린 손떨림,
//   발사는 조준이 붙은 뒤 2~5발 묶음, 이동은 8방향 · 초당 3~5번 결정.
public partial class GameplayCapture
{
    partial class Rec
    {
        // ---------------- 컷 출력
        public bool cuts;                 // 릴스 컷 모드 (프레임은 ffmpeg 로 · 소리는 기록만)
        public bool hud;                  // 게임 HUD 를 찍을지 (세로 컷은 기본 숨김 · 가로로 찍어 자르는 UI 컷만 켬)
        public float ortho;               // 찍을 때 카메라 크기 (0 이면 게임 값 15)
        public Vector2 viewCenter;        // 이번 프레임에 찍는 화면 가운데 (월드)
        public Vector2 viewBias;          // 화면 가운데를 영웅에서 이만큼 옮김 (쇼츠 UI 가 가리는 아래쪽을 피해 영웅을 조금 위로 등)
        public float lookAmount = 0.28f;  // 조준 방향으로 화면을 미리 보여 주는 정도
        Vector2 look, lookVel;
        System.Diagnostics.Process ff;
        Stream ffIn;
        StreamWriter cueW, traceW;
        byte[] raw;
        string soundDir;
        readonly HashSet<string> dumped = new HashSet<string>();
        public int hits;                  // 이 테이크에서 맞은 횟수 (체력이 줄어든 프레임)

        public void PipeBegin(string basePath)
        {
            string exe = Environment.GetEnvironmentVariable("CAPTURE_FFMPEG") ?? "ffmpeg";
            rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            frame = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var psi = new System.Diagnostics.ProcessStartInfo(exe,
                "-hide_banner -loglevel error -y -f rawvideo -pix_fmt rgba -s " + W + "x" + H + " -r " + Fps + " -i - " +
                "-vf vflip,scale=out_color_matrix=bt709:out_range=tv:flags=neighbor,format=yuv444p -c:v libx264 -preset ultrafast -crf 12 -g 1 " +
                "-colorspace bt709 -color_primaries bt709 -color_trc bt709 -color_range tv -threads 2 \"" + basePath + ".mkv\"")
            { UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true };
            ff = System.Diagnostics.Process.Start(psi);
            ffIn = ff.StandardInput.BaseStream;
            cueW = new StreamWriter(basePath + ".cues.jsonl", false, new UTF8Encoding(false));
            traceW = new StreamWriter(basePath + ".trace.csv", false, new UTF8Encoding(false));
            traceW.WriteLine("f,cx,cy,gx,gy,st,held,hx,hy,mv,hit,bullets,reload,tgt,alive");
            soundDir = Path.Combine(Path.GetDirectoryName(basePath), "sounds");
            Directory.CreateDirectory(soundDir);
            frames = 0;
            hits = 0;
        }

        public void PipeEnd()
        {
            recording = false;
            try { ffIn?.Flush(); ffIn?.Close(); ff?.WaitForExit(120000); } catch (Exception e) { Debug.Log("[CAP] ffmpeg 닫기 실패 " + e.Message); }
            ff = null; ffIn = null;
            cueW?.Dispose(); cueW = null;
            traceW?.Dispose(); traceW = null;
            if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
            if (frame != null) { Destroy(frame); frame = null; }
        }

        void PipeFrame()
        {
            if (!recording || ffIn == null) return;
            Texture2D t = Grab(true);
            var data = t.GetRawTextureData<byte>();
            if (raw == null || raw.Length != data.Length) raw = new byte[data.Length];
            data.CopyTo(raw);
            ffIn.Write(raw, 0, raw.Length);      // 쓰기가 끝날 때까지 기다림 — 프레임을 되풀이하거나 건너뛰지 않음
            Trace();
            frames++;
        }

        static string Inv(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        // 소리 기록: 클립은 처음 볼 때 wav 로 꺼내 둠 (게임 합성음은 파일이 없으므로)
        void CueLog(AudioClip clip, float vol, float pitch)
        {
            if (!recording || cueW == null || clip == null) return;
            string name = new string(clip.name.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_').ToArray());
            if (dumped.Add(name))
            {
                string path = Path.Combine(soundDir, name + ".wav");
                if (!File.Exists(path)) DumpWav(clip, path);
            }
            cueW.WriteLine("{\"f\":" + frames + ",\"bus\":\"sfx\",\"clip\":\"" + name + "\",\"vol\":" + Inv(vol) + ",\"pitch\":" + Inv(Mathf.Clamp(pitch, 0.25f, 4f)) + "}");
        }

        void DumpWav(AudioClip clip, string path)
        {
            float[] m = Mono(clip);
            if (m == null || m.Length == 0) { Debug.Log("[CAP] 소리를 꺼내지 못함: " + clip.name); return; }
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = m.Length * 2;
                w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes); w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(clip.frequency); w.Write(clip.frequency * 2); w.Write((short)2); w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (float s in m) w.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1f, 1f) * 32767f));
            }
        }

        // 세로 컷은 HUD 없이 (가로용 배치라 세로에서 겹침) — 화면에 붙는 캔버스만 끄고 월드 캔버스(체력 줄 · 피해 숫자)는 둠
        void HideHud()
        {
            if (uiOnly) { HideHudKeepWindows(); return; }
            foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.isRootCanvas && c != captionCanvas && c.renderMode != RenderMode.WorldSpace) Off(c);
        }

        // UI 컷(카드 고르기 · 무기 진화)을 세로로 직접 찍음: 창은 가로 1920 기준이라 캔버스를 「너비 맞춤」으로 바꿔 창 전체가 세로 화면 안에 들어오게 하고,
        //   HUD(체력 · 탄 · 타이머 · 알림)는 그림 · 글자만 꺼서 창(레벨업 카드 창 · 무기 진화 창)만 남김 — 찍는 순간에만, 게임 코드는 그대로
        public bool uiOnly;
        readonly Dictionary<CanvasScaler, (float match, Vector2 size)> scalerWas = new Dictionary<CanvasScaler, (float, Vector2)>();
        const float UiFitWidth = 1440f;      // 창(카드 세 장 1344)이 양옆 여백 조금만 두고 꽉 차게 — 1920 그대로면 세로 화면에서 너무 작음
        void HideHudKeepWindows()
        {
            var keep = new List<Transform>();
            if (Find("LevelShop") is object shop && Get(shop, "LvshopPanel") is GameObject panel && panel != null) keep.Add(panel.transform);
            if (Find("WeaponEvolutionUI") is Component evo && evo != null) keep.Add(evo.transform);
            foreach (CanvasScaler cs in UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
            {
                Canvas c = cs.GetComponent<Canvas>();
                if (c == null || c == captionCanvas || !c.isRootCanvas || c.renderMode == RenderMode.WorldSpace || cs.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
                if (!scalerWas.ContainsKey(cs)) scalerWas[cs] = (cs.matchWidthOrHeight, cs.referenceResolution);
                cs.matchWidthOrHeight = 0f;
                cs.referenceResolution = new Vector2(UiFitWidth, scalerWas[cs].size.y);
            }
            foreach (Graphic g in UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None))
            {
                if (!g.enabled || g.canvas == null) continue;
                Canvas root = g.canvas.rootCanvas;
                if (root == captionCanvas || root.renderMode == RenderMode.WorldSpace) continue;
                bool kept = false;
                foreach (Transform k in keep) if (g.transform.IsChildOf(k)) { kept = true; break; }
                if (!kept) Off(g);
            }
        }
        public void UiOnlyEnd()
        {
            uiOnly = false;
            foreach (var kv in scalerWas) if (kv.Key != null) { kv.Key.matchWidthOrHeight = kv.Value.match; kv.Key.referenceResolution = kv.Value.size; }
            scalerWas.Clear();
        }

        float HalfH => (ortho > 0f ? ortho : 15f) * baseZoom;
        float HalfW => HalfH * W / H;
        float PxPerUnit => H / (2f * HalfH);

        // 찍는 화면의 가운데: 게임 카메라(흔들림 포함) + 조준 쪽 미리 보기 — 카메라맨처럼 임계 감쇠로 따라가고 정수 픽셀에 맞춤
        void ViewTick()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector2 basePos = cam.transform.position;
            Component p = Find("PlayerController") as Component;
            Vector2 want = Vector2.zero;
            if (p != null && humanOn && autopilot)
            {
                Vector2 d = (Vector2)(aimNow - p.transform.position) * lookAmount;
                want = new Vector2(Mathf.Clamp(d.x, -HalfW * 0.3f, HalfW * 0.3f), Mathf.Clamp(d.y, -HalfH * 0.3f, HalfH * 0.3f));
            }
            look = Vector2.SmoothDamp(look, want, ref lookVel, 0.55f, 99f, 1f / Fps);
            Vector2 c = basePos + look + viewBias;
            float px = PxPerUnit;
            viewCenter = new Vector2(Mathf.Round(c.x * px) / px, Mathf.Round(c.y * px) / px);
        }

        Vector3 ViewPoint(Vector3 world) => new Vector3((world.x - viewCenter.x) / HalfW * 0.5f + 0.5f, (world.y - viewCenter.y) / HalfH * 0.5f + 0.5f, 1f);
        Vector2 ViewPx(Vector2 world) { Vector3 v = ViewPoint(world); return new Vector2(v.x * W, (1f - v.y) * H); }

        // ================================================================= 사람 손 모델
        public bool humanOn;
        public int aimStyle;                     // 0 보통 · 1 보스 위주(잡몹보다 보스 · 빈틈)
        System.Random hr = new System.Random(1);
        enum Ast { Idle, React, Saccade, Settle, Track }
        Ast ast;
        int astLeft, dwellLeft, lagFrames, idleFrames;
        Vector2 cur, curVel, ou;                  // 조준선: 화면 가운데 기준 (월드 단위) — 손이 쉬면 화면에서 멈춰 있고 월드에서는 카메라와 같이 감
        Vector2 sacFrom, sacTo, sacPerp;
        float sacT, sacDur, sacBow, hand = 1f;
        bool needCorrect, premature, curInit;
        Transform tgt;
        readonly List<Vector2> goalHist = new List<Vector2>();
        Vector2 prevView; bool prevViewOk;
        Vector2 goalNow, wander; bool hasGoal; int wanderLeft;
        // 발사
        int okFrames, fireDelay, gapLeft, holdLeft, shotsLeft, lastB = -1;
        bool fHeld;
        // 이동 (8방향 · 결정은 0.2~0.33초마다)
        Vector2 moveQ; int moveHold;
        int lastHp = -1;

        float U(float a, float b) => a + (float)hr.NextDouble() * (b - a);
        float N() => Mathf.Sqrt(-2f * Mathf.Log(Mathf.Max(1e-6f, (float)hr.NextDouble()))) * Mathf.Cos(2f * Mathf.PI * (float)hr.NextDouble());
        int Fr(float sec) => Mathf.Max(1, Mathf.RoundToInt(sec * Fps));

        public void SeedHuman(int seed)
        {
            hr = new System.Random(seed * 7919 + 13);
            hand = hr.NextDouble() < 0.5 ? 1f : -1f;
            ast = Ast.Idle; astLeft = dwellLeft = idleFrames = 0; tgt = null; curInit = false; curVel = ou = Vector2.zero;
            goalHist.Clear(); okFrames = gapLeft = holdLeft = shotsLeft = 0; lastB = -1; fHeld = false;
            prevViewOk = false;
            moveQ = Vector2.zero; moveHold = 0; look = lookVel = Vector2.zero; lastHp = -1;
            movePlayer = null; sceneNo = seed;      // HumanMove 의 씨앗도 테이크마다 다르게
            fireDelay = Fr(U(0.04f, 0.09f));
        }

        static readonly Vector2[] Dir8 = Enumerable.Range(0, 8).Select(i => new Vector2(Mathf.Round(Mathf.Cos(i * Mathf.PI / 4f)), Mathf.Round(Mathf.Sin(i * Mathf.PI / 4f)))).ToArray();
        static Vector2 Snap8(Vector2 v) => Dir8[((Mathf.RoundToInt(Mathf.Atan2(v.y, v.x) / (Mathf.PI / 4f)) % 8) + 8) % 8];

        // 이동: 키보드처럼 8방향만, 한 방향을 최소 0.2~0.33초, 반대로 돌 때는 옆을 거침
        void PutMove(Vector2 v)
        {
            if (!humanOn) { In("AutoMove", v); return; }
            if (--moveHold <= 0)
            {
                Vector2 q = v.sqrMagnitude < 0.09f ? Vector2.zero : Snap8(v);
                if (q != moveQ && moveQ != Vector2.zero && q != Vector2.zero && Vector2.Dot(q.normalized, moveQ.normalized) < -0.5f)
                {
                    Vector2 side = new Vector2(-moveQ.y, moveQ.x);
                    q = Snap8(Vector2.Dot(side, q) >= 0f ? side : -side);
                }
                moveQ = q;
                moveHold = Fr(U(0.2f, 0.33f));
            }
            In("AutoMove", moveQ);
        }

        static bool Alive(Transform t) => t != null && t.gameObject.activeInHierarchy && !(t.GetComponent(T("EnermyController")) is Component e && e.GetType().GetProperty("IsDead")?.GetValue(e) is bool d && d)
                                                    && !(t.GetComponent(T("bosss")) is Component b && b.GetType().GetProperty("IsDead")?.GetValue(b) is bool bd && bd);
        bool OnScreen(Vector2 world, float margin = 0.6f) => Mathf.Abs(world.x - viewCenter.x) < HalfW - margin && Mathf.Abs(world.y - viewCenter.y) < HalfH - margin;

        float Score(Transform t, Vector2 me, bool isBoss)
        {
            Vector2 p = t.position;
            float dHero = (p - me).magnitude, dCur = (p - viewCenter - cur).magnitude;
            float s = 6f / (1f + dHero) + 3f / (1f + dCur);      // 다가온 놈 먼저 · 지금 조준선 근처에 가산점 (크게 휘두르기를 피함)
            if (isBoss) s = aimStyle == 1 ? 9f : 1.3f;
            return s;
        }

        Transform Choose(Vector2 me, Transform bossT, out float best)
        {
            Transform pick = null; best = -1f;
            foreach (Transform m in mobs)
            {
                if (m == null || !OnScreen(m.position)) continue;
                float s = Score(m, me, false) + U(0f, 0.25f);
                if (s > best) { best = s; pick = m; }
            }
            if (bossT != null && OnScreen(bossT.position, -4f))
            {
                float s = Score(bossT, me, true);
                if (s > best) { best = s; pick = bossT; }
            }
            return pick;
        }

        void StartSaccade(Vector2 to, bool corrective)
        {
            Vector2 d = to - cur;
            float dist = d.magnitude;
            Vector2 along = dist > 0.001f ? d / dist : Vector2.right, perp = new Vector2(-along.y, along.x);
            const float targetW = 1.0f;
            if (!corrective)
            {
                float err = dist * U(0.04f, 0.06f);          // 끝점 오차: 멀리 휘두를수록 더 빗나감 (넘치기도 모자라기도)
                to += along * N() * err + perp * N() * err * 0.6f;
                sacDur = Mathf.Clamp(0.09f + 0.11f * Mathf.Log(dist / targetW + 1f, 2f), 0.12f, 0.45f);      // 피츠 법칙
                sacBow = dist * U(0.03f, 0.08f) * (hr.NextDouble() < 0.85 ? hand : -hand);                    // 살짝 휜 경로 (손목 버릇)
            }
            else
            {
                to += new Vector2(N(), N()) * 0.06f;
                sacDur = Mathf.Clamp(0.06f + 0.08f * Mathf.Log(dist / 0.5f + 1f, 2f), 0.07f, 0.16f);
                sacBow = dist * U(0f, 0.04f) * hand;
            }
            sacFrom = cur; sacTo = to; sacPerp = perp; sacT = 0f;
            needCorrect = !corrective;
            ast = Ast.Saccade;
        }

        void HumanUpdate()
        {
            Component player = (Component)Find("PlayerController");
            if (player == null) return;
            float dt = 1f / Fps;
            Vector3 me3 = player.transform.position;
            Vector2 me = me3;
            Vector3? target = Nearest(me3, out bool boss);
            humanMove = true;
            // 보스가 16칸(Nearest 의 범위)보다 멀어져도 화면에 있으면 계속 보스를 상대로 움직임 (아니면 표적을 잃고 그 자리에 서서 쏘기만 함)
            if (!target.HasValue && Find("bosss") is Component far && Alive(far.transform)) { target = far.transform.position; boss = true; }
            HumanMove(player, me3, target, boss);            // 이동 (PutMove 가 8방향으로) · mobs 채움
            Vector2 viewVel = prevViewOk ? (viewCenter - prevView) / dt : Vector2.zero;      // 화면(카메라)이 움직이는 속도 — 도약의 끝점을 정할 때 뺌
            prevView = viewCenter; prevViewOk = true;
            if (viewVel.magnitude > 30f) viewVel = Vector2.zero;
            if (!curInit) { cur = me - viewCenter + new Vector2(U(-3f, 3f), U(1f, 4f)); curInit = true; }

            Transform bossT = (Find("bosss") as Component)?.transform;
            if (bossT != null && !Alive(bossT)) bossT = null;

            // ---- 표적 고르기: 한 표적에 0.35~0.9초는 머묾 (죽으면 예외) · 더 나은 표적이 뚜렷할 때만 옮김
            if (dwellLeft > 0) dwellLeft--;
            bool alive = aimFixed || (tgt != null && Alive(tgt) && OnScreen(tgt.position, tgt == bossT ? -4f : 0.2f));
            bool reacting = ast == Ast.React;
            if (!aimFixed && (!reacting || !alive) && (!alive || dwellLeft <= 0))
            {
                Transform pick = Choose(me, bossT, out float best);
                bool sw = pick != tgt && (!alive || best > Score(tgt, me, tgt == bossT) * 1.35f);
                if (sw)
                {
                    tgt = pick;
                    goalHist.Clear();
                    if (pick == null) ast = Ast.Idle;
                    else
                    {
                        bool nearCur = ((Vector2)pick.position - viewCenter - cur).magnitude < 1.5f;
                        ast = Ast.React;
                        // 반응 지연 (반응하던 중에 표적이 죽어 바꾸는 것이면 처음부터 다시 세지 않고 조금만 더)
                        astLeft = reacting ? astLeft + Fr(0.05f) : Fr(nearCur ? U(0.08f, 0.12f) : U(0.18f, 0.28f));
                        dwellLeft = astLeft + Fr(U(0.35f, 0.9f));
                    }
                }
                else if (alive) dwellLeft = Fr(U(0.2f, 0.4f));
            }
            if (aimFixed && ast == Ast.Idle) { ast = Ast.React; astLeft = Fr(U(0.18f, 0.28f)); goalHist.Clear(); }

            // ---- 목표 점 (화면 가운데 기준)
            hasGoal = aimFixed || tgt != null;
            Vector2 goalVel = Vector2.zero;
            if (hasGoal)
            {
                // 큰 보스는 정중앙만 겨누지 않음: 몸 안에서 겨누는 자리를 0.6~1.4초마다 조금 옮김
                if (!aimFixed && tgt == bossT && --wanderLeft <= 0) { wander = new Vector2(N(), N()) * 0.45f; wanderLeft = Fr(U(0.6f, 1.4f)); }
                Vector2 world = aimFixed ? (Vector2)aimPoint : (Vector2)tgt.position + (tgt == bossT ? wander : Vector2.zero);
                goalNow = world - viewCenter;
                goalHist.Add(world);      // 월드 자리로 기억: 뒤처짐은 표적의 움직임에만 (내가 걸어서 생기는 화면 움직임은 손이 이미 앎)
                if (goalHist.Count > 20) goalHist.RemoveAt(0);
                if (goalHist.Count > 6) goalVel = (world - goalHist[goalHist.Count - 7]) / (6f * dt);
            }

            switch (ast)
            {
                case Ast.Idle:
                {
                    // 쉴 때: 화면 가운데 얼어 있지 않고 가는 방향 앞쪽으로 느리게 흘러감
                    Vector2 rest = me - viewCenter + (moveQ != Vector2.zero ? moveQ.normalized * 4f : new Vector2(hand * 1.5f, 2.5f));
                    cur = Vector2.SmoothDamp(cur, rest, ref curVel, 0.7f, 99f, dt);
                    idleFrames++;
                    break;
                }
                case Ast.React:
                    if (--astLeft <= 0)
                    {
                        if (!hasGoal) { ast = Ast.Idle; break; }
                        float guess = Mathf.Clamp(0.09f + 0.11f * Mathf.Log((goalNow - cur).magnitude + 1f, 2f), 0.12f, 0.45f);
                        StartSaccade(goalNow + (goalVel - viewVel) * guess * U(0.6f, 0.9f), false);      // 움직이는 표적은 앞을 겨누되 필요한 만큼의 60~90%
                        lagFrames = Fr(U(0.08f, 0.13f));
                    }
                    break;
                case Ast.Saccade:
                {
                    sacT = Mathf.Min(1f, sacT + dt / sacDur);
                    float s = sacT * sacT * sacT * (10f - 15f * sacT + 6f * sacT * sacT);      // 최소 저크 (속도가 종 모양)
                    cur = Vector2.LerpUnclamped(sacFrom, sacTo, s) + sacPerp * sacBow * Mathf.Sin(Mathf.PI * s);
                    curVel = Vector2.zero;
                    if (sacT >= 1f)
                    {
                        if (needCorrect) { ast = Ast.Settle; astLeft = Fr(U(0.09f, 0.14f)); premature = hr.NextDouble() < 0.10; }
                        else ast = Ast.Track;
                    }
                    break;
                }
                case Ast.Settle:
                    if (--astLeft <= 0)
                    {
                        if (!hasGoal) { ast = Ast.Idle; break; }
                        StartSaccade(goalNow + (goalVel - viewVel) * 0.1f * U(0.6f, 0.9f), true);            // 작은 교정 한 번
                    }
                    break;
                case Ast.Track:
                {
                    if (!hasGoal) { ast = Ast.Idle; break; }
                    // 80~130ms 전 자리를 임계 감쇠 스프링으로 따라감 (약간 뒤처짐)
                    Vector2 g = goalHist[Mathf.Max(0, goalHist.Count - 1 - lagFrames)] + goalVel * (lagFrames * dt) * 0.75f - viewCenter;
                    // 표적과 화면의 움직임은 손이 대부분(85%) 미리 따라가고, 남은 어긋남만 스프링으로 줄임
                    cur += (goalVel - viewVel) * 0.85f * dt;
                    const float w = 16f;
                    curVel += (w * w * (g - cur) - 2f * w * curVel) * dt;
                    cur += curVel * dt;
                    if ((goalNow - cur).magnitude > 3f) { ast = Ast.React; astLeft = Fr(U(0.1f, 0.18f)); }      // 표적이 순간이동 · 돌진
                    break;
                }
            }
            if (ast != Ast.Idle) idleFrames = 0;

            // 느린 손떨림 (OU 잡음 · 표준편차 약 4.5px · 시상수 0.25초) — 프레임마다 흰 잡음이 아님
            float sigma = 4.5f / PxPerUnit * (H / 1920f);
            ou += -ou / 0.25f * dt + new Vector2(N(), N()) * sigma * Mathf.Sqrt(2f * dt / 0.25f);
            // 조준선은 화면 안에만
            cur = new Vector2(Mathf.Clamp(cur.x, -HalfW + 0.4f, HalfW - 0.4f), Mathf.Clamp(cur.y, -HalfH + 0.4f, HalfH - 0.4f));
            aimNow = viewCenter + cur + ou;
            In("AutoAim", aimNow);

            // ---- 발사: 조준이 붙은 뒤 40~90ms · 큰 도약 중에는 쏘지 않음 · 가끔 교정 전에 쏴서 빗나감 · 2~5발 묶음
            int bullets = Convert.ToInt32(Get(player, "NowBullet"));
            bool reloading = player.GetType().GetProperty("IsReloading")?.GetValue(player) is bool r && r;
            if (lastB >= 0 && bullets < lastB && fHeld) shotsLeft -= lastB - bullets;
            lastB = bullets;
            if (!manualFire)
            {
                float tol = tgt != null && tgt == bossT && !aimFixed ? 2.2f : IsMelee(Hero()) ? 3.5f : 1.1f;
                bool onTarget = hasGoal && (goalNow - cur).magnitude < tol;
                bool may = ((ast == Ast.Track || ast == Ast.Settle || (ast == Ast.Saccade && !needCorrect)) && onTarget) || (ast == Ast.Settle && premature);
                okFrames = may ? okFrames + 1 : 0;
                if (!fHeld)
                {
                    if (gapLeft > 0) gapLeft--;
                    else if (okFrames >= fireDelay && !reloading)
                    {
                        fHeld = true;
                        In("FireDownFrame", Time.frameCount + 1); In("AutoFire", true);
                        shotsLeft = hr.Next(2, 6);
                        holdLeft = Fr(U(0.45f, 1.3f));
                        fireDelay = Fr(U(0.04f, 0.09f));
                    }
                }
                else
                {
                    holdLeft--;
                    bool farJump = ast == Ast.Saccade && needCorrect && sacDur > 0.2f;
                    if (holdLeft <= 0 || shotsLeft <= 0 || farJump || ast == Ast.Idle || reloading)
                    {
                        fHeld = false;
                        In("FireUpFrame", Time.frameCount + 1); In("AutoFire", false);
                        gapLeft = Fr(U(0.09f, 0.4f));
                    }
                }
                // 장전은 싸움의 틈에: 쉬는 중이고 탄이 40% 아래일 때 (탄이 떨어지면 게임이 바로 장전함)
                int max = Convert.ToInt32(Get(player, "MaxBullet"));
                if (!reloading && idleFrames == Fr(0.3f) && bullets > 0 && bullets < max * 0.4f) Call(player, "ForceReload");
            }
        }

        // 조준선 · 표적 · 이동 · 발사 기록 (화면 픽셀)
        void Trace()
        {
            if (traceW == null) return;
            Component p = Find("PlayerController") as Component;
            Vector2 c = ViewPx(aimNow), h = p != null ? ViewPx(p.transform.position) : Vector2.zero;
            Vector2 g = hasGoal ? ViewPx(viewCenter + goalNow) : new Vector2(-1f, -1f);
            bool hit = hitFlag;
            hitFlag = false;
            int mv = moveQ == Vector2.zero ? -1 : Array.IndexOf(Dir8, moveQ);
            traceW.WriteLine(string.Join(",", frames, Inv(c.x), Inv(c.y), Inv(g.x), Inv(g.y), (int)ast, fHeld || GameInputFire() ? 1 : 0, Inv(h.x), Inv(h.y), mv, hit ? 1 : 0,
                lastB, p != null && p.GetType().GetProperty("IsReloading")?.GetValue(p) is bool r && r ? 1 : 0, tgt != null ? tgt.GetInstanceID() : 0, mobs.Count));
        }

        static bool GameInputFire() => T("GameInput").GetField("AutoFire").GetValue(null) is bool b && b;

        // 촬영 중에는 LateUpdate 가 체력을 다시 채우므로(죽으면 테이크가 버려짐) 채우기 직전에 맞았는지 봄
        bool hitFlag;
        void HitCheck(object p)
        {
            if (!recording) return;
            if (Convert.ToSingle(Get(p, "PlayerHealth")) < Convert.ToSingle(Get(p, "PlayerMaxHealth")) - 0.5f) { hitFlag = true; hits++; }
        }
    }

    // ================================================================= 컷 촬영
    static int Seed;
    static string TakeName(string cut) => cut + "_s" + Seed;

    static IEnumerator Prep(string hero, int who, int stage, bool portrait = true, float ortho = 20f, float dmg = 3f)
    {
        trickleGen++;
        rec.recording = false;
        rec.autopilot = false;
        rec.UiOnlyEnd();
        rec.manualFire = false; rec.aimFixed = false; rec.aimOverride = null; rec.gunSfx = false; rec.aimStyle = 0;
        rec.baseZoom = 1f; rec.viewBias = Vector2.zero; rec.hud = !portrait; rec.showCursor = true; rec.lookAmount = 0.28f;
        float scale = float.TryParse(Environment.GetEnvironmentVariable("CAPTURE_SCALE"), NumberStyles.Float, CultureInfo.InvariantCulture, out float sc) ? sc : 1f;
        W = Mathf.RoundToInt((portrait ? 1080 : 3414) * scale) / 2 * 2; H = Mathf.RoundToInt(1920 * scale) / 2 * 2;
        rec.ortho = portrait ? ortho : 15f;      // 세로 16: 한 칸 60px (가로 1080p 의 36px 보다 크게 — 휴대폰에서 영웅이 보이게) · 가로 18칸 세로 32칸
        rec.captionCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(portrait ? 1080 : 3414, 1920);
        if (rec.cursorImage != null) rec.cursorImage.rectTransform.sizeDelta = new Vector2(96f, 96f);      // 48px 도트 커서 × 2 (정수 배)
        rec.viewBias = Vector2.zero;
        UnityEngine.Random.InitState(Seed * 1000 + 17);
        rec.SeedHuman(Seed);
        Time.timeScale = 1f;
        if (noSkinsReels) ClearSkins(); else WearLegend(who);
        yield return Load(hero, stage);
        rec.gunSfx = hero == "Gunner";
        rec.rangedMobNear = 8f; rec.rangedMobFar = 12f; rec.rangedBossNear = 8f; rec.rangedBossFar = 12f;
        // 촬영용 빌드: 판 중반쯤 키운 공격력 (맨몸 1레벨로는 2장 적이 안 죽어 무리에 파묻힘)
        if (Find("PlayerController") is Component pc)
        {
            Set(pc, "damageMultiplier", dmg);
            Set(pc, "fireRateMultiplier", 2f);
            Set(pc, "reloadTime", 1.3f);
            Set(pc, "speed", Convert.ToSingle(Get(pc, "speed")) * 1.15f);
            if (hero == "Gunner") { Set(pc, "MaxBullet", 8); Set(pc, "NowBullet", 8); }
            // 맵 가운데에서 시작 (시작 자리는 맵 가장자리라 물러나다 벽에 몰림)
            if (Find("EnemySpawner") is object sp && Get(sp, "spawnAreaMin") is Vector2 lo && Get(sp, "spawnAreaMax") is Vector2 hi)
            {
                Vector3 at = new Vector3((lo.x + hi.x) * 0.5f, (lo.y + hi.y) * 0.5f, pc.transform.position.z);
                for (int i = 1; i < 10 && Rec.Wall(at); i++) at += new Vector3(1.5f, (i % 2 == 0 ? 1.5f : -1.5f) * i, 0f);
                if (!Rec.Wall(at)) pc.transform.position = at;
            }
        }
        yield return null;
    }
    static bool noSkinsReels = true;      // 릴스는 기본 모습 (게임을 처음 켰을 때 보는 그대로)

    static void Roll(string cut)
    {
        rec.PipeBegin(Path.Combine(Out, TakeName(cut)));
        rec.recording = true;
        Debug.Log("[CAP] take " + TakeName(cut) + " " + W + "x" + H);
    }

    static void Cut(string cut)
    {
        int n = rec.frames, hits = rec.hits;
        rec.PipeEnd();
        trickleGen++;
        rec.autopilot = false;
        StopInput();
        File.WriteAllText(Path.Combine(Out, TakeName(cut) + ".meta.json"), "{\"frames\":" + n + ",\"w\":" + W + ",\"h\":" + H + ",\"fps\":" + Fps + ",\"hits\":" + hits + "}");
        Debug.Log("[CAP] cut " + TakeName(cut) + " frames=" + n);
    }

    // 그 장의 적을 방향 a(라디안) 둘레 spread 안, 거리 r0~r1 에 n 마리 (한 덩어리로 뭉치지 않게 흩어 놓음)
    static void Wave(int stage, int n, float a, float spread, float r0, float r1)
    {
        object data = Resources.Load("CodexData");
        var enemies = (GameObject[])Get(data, "enemies");
        var stages = (int[])Get(data, "enemyStage");
        var pool = new List<GameObject>();
        for (int i = 0; i < enemies.Length; i++) if (stages[i] == stage) pool.Add(enemies[i]);
        if (pool.Count == 0 && StageConfig(stage) is object cfg) pool.AddRange((GameObject[])Get(cfg, "enemies"));
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        for (int i = 0; i < n && pool.Count > 0; i++)
        {
            float t = a + UnityEngine.Random.Range(-spread, spread), r = UnityEngine.Random.Range(r0, r1);
            Vector3 at = me + new Vector3(Mathf.Cos(t), Mathf.Sin(t)) * r;
            if (Rec.Wall(at)) continue;
            // 앞쪽(약한 적)이 더 많이 — 뒤쪽 큰 적은 드물게
            int k = Mathf.Min(pool.Count - 1, (int)(Mathf.Pow(UnityEngine.Random.value, 1.6f) * pool.Count));
            UnityEngine.Object.Instantiate(pool[k], at, Quaternion.identity);
        }
    }

    // 싸우는 동안 적이 고르지 않은 간격으로 계속 흘러 들어옴 (주로 위 · 옆에서 — 세로 화면)
    static int trickleGen;      // 다음 컷을 준비하기 시작하면 앞 컷의 흘러 들어오기는 멈춤
    static IEnumerator Trickle(int stage, float seconds, int perWave, float every)
    {
        float t = 0f, next = 0f;
        int gen = trickleGen;
        while (t < seconds && gen == trickleGen && Find("PlayerController") != null)
        {
            if (t >= next)
            {
                float a = UnityEngine.Random.value < 0.55f ? UnityEngine.Random.Range(1.0f, 2.1f) : UnityEngine.Random.Range(3.9f, 5.5f);
                Wave(stage, perWave + UnityEngine.Random.Range(-1, 2), a, 0.3f, 15f, 19f);
                next = t + every * UnityEngine.Random.Range(0.6f, 1.5f);
            }
            t += 1f / Fps;
            yield return null;
        }
    }

    // ---- aim: 거너가 조준선으로 하나씩 겨눠 쏨 → 탄창이 비면 장전 (HUD 없이도 조준선 · 총구 · 탄피로 읽힘)
    static IEnumerator CutAim(int stage, float seconds, int startBullets = -1)
    {
        yield return Prep("Gunner", 0, stage);
        Wave(stage, 5, 1.6f, 0.7f, 10f, 14f);
        Wave(stage, 3, 4.7f, 0.6f, 12f, 15f);
        rec.autopilot = true;
        rec.StartCoroutine(Trickle(stage, seconds + 2f, 3, 1.0f));
        yield return F(1.2f);
        if (startBullets >= 0) Set(Find("PlayerController"), "NowBullet", startBullets);
        Roll(startBullets >= 0 ? "reload" + stage : "aim" + stage);
        yield return F(seconds);
        Cut(startBullets >= 0 ? "reload" + stage : "aim" + stage);
    }

    // ---- horde: 진화 무기를 든 영웅이 큰 무리와 싸우다 우클릭 필살기
    static IEnumerator CutHorde(string name, string hero, int who, int stage, int evolve, float seconds, float ultAt, float ultHold)
    {
        yield return Prep(hero, who, stage);
        Evolve(evolve);
        Wave(stage, 14, 1.6f, 0.9f, 9f, 15f);
        Wave(stage, 9, 4.6f, 0.9f, 11f, 16f);
        rec.autopilot = true;
        rec.StartCoroutine(Trickle(stage, seconds + 2f, 5, 0.6f));
        yield return F(1.3f);
        Roll(name);
        yield return F(ultAt);
        if (ultHold > 0f) yield return Ult(ultHold);
        yield return F(Mathf.Max(0.1f, seconds - ultAt - ultHold));
        Cut(name);
    }

    // ---- barrier: 보스가 결계를 펼친 직후 — 어두워진 맵 · 쏟아지는 공격 · 피하며 쏨. kill 이면 끝에 쓰러뜨려 결계가 깨짐
    static IEnumerator CutBarrier(string name, string hero, int who, int stage, float seconds, bool kill)
    {
        yield return Prep(hero, who, stage, true, 22f, 2f);
        object b = SpawnBoss(stage, 0.8f);
        ((Component)b).transform.position = ((Component)Find("PlayerController")).transform.position + new Vector3(1.5f, 11f, 0f);      // 세로 화면: 보스는 위쪽
        rec.aimStyle = 1;
        rec.autopilot = true;
        rec.rangedBossNear = 9f; rec.rangedBossFar = 13f; rec.lookAmount = 0.4f;
        yield return F(0.8f);
        yield return RaiseBarrier(b);
        Roll(name);
        SxRes2("Sounds/domain_open", 0.5f);
        yield return F(kill ? seconds - 2.6f : seconds);
        if (kill)
        {
            KillBoss(b);
            SxRes2("Sounds/domain_break", 0.7f);
            rec.StartCoroutine(SlowMo(0.3f, 0.6f));
            yield return F(2.6f);
        }
        Cut(name);
    }
    static void SxRes2(string path, float vol) { AudioClip c = Resources.Load<AudioClip>(path); if (c != null) { c.LoadAudioData(); rec.Sfx(c, vol); } }

    // ---- weak: 리치 왕이 주문을 외는 동안 지팡이 끝 빈틈을 직접 겨눠 쏨
    static IEnumerator CutWeak(float seconds)
    {
        Type ws = T("WeakSpot");
        if (ws == null) yield break;
        yield return Prep("Gunner", 0, 0, true, 20f, 2f);
        object b = SpawnBoss(0, 0.8f);
        ((Component)b).transform.position = ((Component)Find("PlayerController")).transform.position + new Vector3(1.5f, 10f, 0f);
        rec.aimStyle = 1;
        rec.autopilot = true;
        rec.rangedBossNear = 8f; rec.rangedBossFar = 11f; rec.lookAmount = 0.4f;
        FieldInfo casting = b.GetType().GetField("casting", Any);
        yield return F(0.6f);
        Roll("weak");
        Component spot = null;
        for (int f = 0; f < Fps * seconds; f++)
        {
            if (spot == null) spot = ((Component)b).GetComponent(ws);
            bool open = casting != null && (bool)casting.GetValue(b) && spot != null && WeakCenter(spot, out Vector3 at) && at != Vector3.zero;
            rec.aimFixed = open;
            if (open && WeakCenter(spot, out Vector3 now)) rec.aimPoint = now;
            yield return null;
        }
        rec.aimFixed = false;
        Cut("weak");
    }

    // ---- hero: 영웅 하나의 평타와 우클릭 스킬
    static IEnumerator CutHero(string hero, int stage, float seconds)
    {
        int who = (int)Enum.Parse(T("CharacterId"), hero);
        yield return Prep(hero, who, stage);
        Wave(stage, 12, 1.6f, 1.0f, 7f, 13f);
        Wave(stage, 8, 4.6f, 1.0f, 8f, 13f);
        rec.autopilot = true;
        rec.StartCoroutine(Trickle(stage, seconds + 2f, 4, 0.6f));
        yield return F(1.0f);
        Roll("hero_" + hero);
        yield return F(seconds * 0.35f);
        yield return Ult(0.45f);
        yield return F(seconds * 0.65f - 0.45f);
        Cut("hero_" + hero);
    }

    // ---- dev: 무한 모드의 마지막 보스 「개발자」
    static IEnumerator CutDev(float seconds)
    {
        Type dt = T("Difficulty");
        FieldInfo ov = T("GameMode")?.GetField("Override", Any);
        if (dt == null || ov == null) yield break;
        ov.SetValue(null, Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(dt), Enum.Parse(dt, "Endless")));
        yield return Prep("Gunner", 0, 0, true, 22f, 2f);
        object em = null;
        for (int i = 0; i < 10 && em == null; i++) { em = StaticGet("EndlessMode", "Instance"); if (em == null) yield return null; }
        object dev = null;
        try { dev = em != null ? Call(em, "SpawnFinalBoss", false, 1f) : null; } catch (Exception e) { Debug.Log("[CAP] 개발자 소환 실패 " + e.Message); }
        if (dev == null) { ov.SetValue(null, null); yield break; }
        ((Component)dev).transform.position = ((Component)Find("PlayerController")).transform.position + new Vector3(1f, 11f, 0f);
        rec.aimStyle = 1;
        rec.autopilot = true;
        rec.rangedBossNear = 9f; rec.rangedBossFar = 13f; rec.lookAmount = 0.4f;
        yield return F(1.0f);
        Roll("dev");
        yield return F(seconds * 0.4f);
        yield return Ult(0.7f);
        yield return F(seconds * 0.6f - 0.7f);
        Cut("dev");
        ov.SetValue(null, null);
    }

    // ---- endless: 무한 모드의 불타는 사막에서 세 장의 적과
    static IEnumerator CutEndless(float seconds)
    {
        Type dt = T("Difficulty");
        FieldInfo ov = T("GameMode")?.GetField("Override", Any);
        if (dt == null || ov == null) yield break;
        ov.SetValue(null, Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(dt), Enum.Parse(dt, "Endless")));
        yield return Prep("Gunner", 0, 0);
        Evolve(2);
        for (int st = 0; st < 3; st++) Wave(st, 9, 1.6f + st * 2.1f, 1.0f, 9f, 15f);
        rec.autopilot = true;
        rec.StartCoroutine(Trickle(1, seconds + 2f, 5, 0.5f));
        yield return F(1.2f);
        Roll("endless");
        yield return F(seconds * 0.5f);
        yield return Ult(0.5f);
        yield return F(seconds * 0.5f - 0.5f);
        Cut("endless");
        ov.SetValue(null, null);
    }

    // ---- cards (가로로 찍어 세로로 자름): 레벨업 카드 세 장 — 화살표 커서가 카드로 가서 읽고, 옆 카드를 봤다가 돌아와 누르고 확정
    static IEnumerator CutCards(float fight)
    {
        yield return Prep("Gunner", 0, 2);
        rec.uiOnly = true;
        Wave(2, 8, 1.6f, 0.8f, 9f, 14f);
        rec.autopilot = true;
        yield return F(1.0f);
        Roll("cards");
        yield return F(fight);
        rec.autopilot = false;
        StopInput();
        object shop = Find("LevelShop");
        rec.levelUpAllowed = true;
        Call(shop, "openLevelShop");
        Call(shop, "setAbilitys");
        Texture2D arrow = Resources.Load<Texture2D>("Cursors/cursor_gold_arrow_0"), aim = rec.cursorImage.texture as Texture2D;
        if (arrow != null) rec.cursorImage.texture = arrow;
        // 카드 자리는 화면 비율 (가로 화면 기준: 카드 세 장이 0.31 · 0.5 · 0.69 에 놓임)
        rec.manualCursor = true;
        rec.cursorImage.rectTransform.sizeDelta = new Vector2(72f, 72f);      // 창이 0.75배로 줄어든 만큼 커서도
        Vector2 a = new Vector2(0.58f, 0.36f), b1 = new Vector2(0.73f, 0.47f), b2 = new Vector2(0.51f, 0.47f);
        yield return F(0.35f);
        yield return rec.Glide(a, b1, 0.5f);         // 오른쪽 카드를 먼저 봄
        yield return F(0.45f);
        yield return rec.Glide(b1, b2, 0.38f);       // 가운데로 돌아옴
        yield return F(0.4f);
        Call(shop, "onSecondButton");
        yield return F(0.55f);
        ConfirmCard(shop, 1);
        rec.levelUpAllowed = false;
        rec.manualCursor = false;
        rec.cursorImage.texture = aim;
        rec.autopilot = true;
        rec.cursorImage.rectTransform.sizeDelta = new Vector2(96f, 96f);
        Wave(2, 6, 1.6f, 1.2f, 8f, 12f);
        yield return F(1.6f);
        Cut("cards");
        rec.UiOnlyEnd();
    }

    // ---- evo (가로로 찍어 세로로 자름): 결계 안에서 보스를 쓰러뜨리면 무기가 부서지고 진화 카드 3장 → 고름
    static IEnumerator CutEvo()
    {
        yield return Prep("Gunner", 0, 2, true, 22f, 2f);
        rec.uiOnly = true;
        object b = SpawnBoss(2, 0.8f);
        ((Component)b).transform.position = ((Component)Find("PlayerController")).transform.position + new Vector3(1.5f, 11f, 0f);
        rec.aimStyle = 1;
        rec.autopilot = true;
        rec.rangedBossNear = 9f; rec.rangedBossFar = 13f; rec.lookAmount = 0.4f;
        yield return F(0.8f);
        yield return RaiseBarrier(b);
        Roll("evo");
        SxRes2("Sounds/domain_open", 0.5f);
        yield return F(1.4f);
        KillBoss(b);
        SxRes2("Sounds/domain_break", 0.7f);
        rec.StartCoroutine(SlowMo(0.3f, 0.5f));
        object evo = null;
        for (int i = 0; i < Fps * 2 && evo == null; i++) { evo = Find("WeaponEvolutionUI"); if (evo == null) yield return null; }
        rec.autopilot = false;
        StopInput();
        if (evo == null)
        {
            object sm = Find("StageManager");
            if (sm != null) ((MonoBehaviour)sm).StartCoroutine((IEnumerator)Call(sm, "Evolution", false));
            yield return F(0.5f);
            evo = Find("WeaponEvolutionUI");
        }
        yield return F(1.6f);
        if (evo != null) Call(evo, "Select", 1);
        yield return F(0.7f);
        if (evo != null) Set(evo, "confirmed", true);
        yield return F(2.2f);
        Cut("evo");
        rec.UiOnlyEnd();
    }

    partial class Rec
    {
        // UI 컷의 화살표 커서: 화면 비율 자리 사이를 최소 저크로 (살짝 휨)
        public bool manualCursor;
        public IEnumerator Glide(Vector2 from, Vector2 to, float seconds)
        {
            int n = Fr(seconds);
            Vector2 d = to - from, perp = new Vector2(-d.y, d.x) * 0.06f * hand;
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n, s = t * t * t * (10f - 15f * t + 6f * t * t);
                SetCursor(Vector2.LerpUnclamped(from, to, s) + perp * Mathf.Sin(Mathf.PI * s));
                yield return null;
            }
        }
        void SetCursor(Vector2 v)
        {
            cursorImage.enabled = true;
            RectTransform r = cursorImage.rectTransform;
            r.anchorMin = r.anchorMax = v;
            r.anchoredPosition = Vector2.zero;
        }
    }

    // CAPTURE_CUTS = "aim2:1,aim2:2,horde_meadow:1,…" (컷:씨앗) — 하나가 실패해도 다음 컷으로
    [UnityTest, Timeout(14400000)]
    public IEnumerator ReelsShoot()
    {
        LogAssert.ignoreFailingMessages = true;
        Directory.CreateDirectory(Out);
        Time.captureFramerate = Fps;
        T("SkinFx").GetField("InCapture").SetValue(null, true);
        In("Auto", true);
        AudioListener.volume = 0f;
        SetGameLanguage(Lang);
        GameObject host = new GameObject("CaptureRec");
        UnityEngine.Object.DontDestroyOnLoad(host);
        rec = host.AddComponent<Rec>();
        BuildOverlay();
        rec.cuts = true; rec.humanOn = true; rec.humanMove = true; rec.showCursor = true; rec.rich = false;
        rec.sub.text = ""; rec.logo.text = "";
        rec.black.color = new Color(0f, 0f, 0f, 0f);
        // 근거리 영웅 · 원거리 영웅의 싸움 거리: 세로 화면(가로 13.5칸 · 세로 24칸)에 맞게 가까이
        rec.rangedMobNear = 5f; rec.rangedMobFar = 8.5f; rec.rangedBossNear = 7f; rec.rangedBossFar = 10f;
        FieldInfo tap = T("SpecialFeedback").GetField("Tapped", Any);
        tap.SetValue(null, (Action<AudioClip, float, float>)((c, v, p) => { if (c != null && rec.recording) rec.Sfx(c, v, p); }));

        string[] list = (Environment.GetEnvironmentVariable("CAPTURE_CUTS") ?? "aim2:1").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string item in list)
        {
            string[] kv = item.Trim().Split(':');
            string cut = kv[0];
            Seed = kv.Length > 1 ? int.Parse(kv[1]) : 1;
            IEnumerator body = Body(cut);
            if (body == null) { Debug.Log("[CAP] 모르는 컷 " + cut); continue; }
            // 예외가 나도 다음 컷으로 (열린 파이프는 닫음)
            while (true)
            {
                bool more;
                try { more = body.MoveNext(); }
                catch (Exception e) { Debug.Log("[CAP] 컷 실패 " + cut + ": " + e); rec.PipeEnd(); break; }
                if (!more) break;
                yield return body.Current;
            }
            Time.timeScale = 1f;
        }
        tap.SetValue(null, null);
        T("CharacterData").GetField("Override", Any).SetValue(null, null);
        T("GameMode")?.GetField("Override", Any)?.SetValue(null, null);
        ClearSkins();
        Time.captureFramerate = 0;
        In("Auto", false);
        Debug.Log("[CAP] done reels");
        yield return null;
    }

    static IEnumerator Body(string cut)
    {
        switch (cut)
        {
            case "aim2": return CutAim(2, 9f);
            case "aim1": return CutAim(1, 9f);
            case "aim0": return CutAim(0, 9f);
            case "reload2": return CutAim(2, 6f, 2);
            case "reload1": return CutAim(1, 6f, 2);
            case "horde_meadow": return CutHorde(cut, "Gunner", 0, 2, 2, 7f, 3.2f, 0.4f);                              // 초원 · 쌍권총
            case "horde_catacomb": return CutHorde(cut, "Swordsman", 1, 0, KitWeaponSafe("Swordsman", 0), 7f, 3.2f, 0.4f);
            case "horde_hell": return CutHorde(cut, "Archer", 3, 1, KitWeaponSafe("Archer", 1), 7f, 3.2f, 0.4f);
            case "horde_abyss": return CutHorde(cut, "Gunner", 0, 3, 5, 7f, 3.2f, 0.4f);                               // 심연 · 번개 사슬총
            case "ult_gunner": return CutHorde(cut, "Gunner", 0, 1, -1, 6f, 1.6f, 0.9f);                               // 지옥 · 일제 사격
            case "barrier0": return CutBarrier(cut, "Gunner", 0, 0, 10f, false);
            case "barrier1": return CutBarrier(cut, "Gunner", 0, 1, 10f, false);
            case "barrier2": return CutBarrier(cut, "Gunner", 0, 2, 10f, false);
            case "break1": return CutBarrier(cut, "Gunner", 0, 1, 7f, true);
            case "break2": return CutBarrier(cut, "Gunner", 0, 2, 7f, true);
            case "weak": return CutWeak(7f);
            case "dev": return CutDev(8f);
            case "endless": return CutEndless(7f);
            case "cards": return CutCards(1.6f);
            case "evo": return CutEvo();
        }
        if (cut.StartsWith("hero_"))
        {
            string[] p = cut.Substring(5).Split('@');      // hero_Archer@1 = 궁수 · 지옥
            return CutHero(p[0], p.Length > 1 ? int.Parse(p[1]) : 2, 5f);
        }
        return null;
    }

    static int KitWeaponSafe(string hero, int nth) { try { return KitWeapon(hero, nth); } catch (Exception) { return -1; } }
}
