using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 개발자 고유 기본 공격 6종 (기획서 C절) — 차례 · 정리 · 풀은 BossSkills.OwnKit.cs
//   라이브 코딩 · 범위 선택 · 빌드 실패 (구간 0~) · 오류 창 · 로딩 중… (66%~) · 스택 오버플로 (33%~)
//   모든 기다림은 시각 끝이 있고(OwnOk(e) 로 끊김 확인), 표시는 ownRoot 풀 · 경고는 OwnCircle/OwnLineWarn (끊기면 EndEarly)
public partial class BossSkills
{
    // ================================================================= C1. 라이브 코딩
    // 코드 줄 (번역하지 않음 · 대문자 글꼴로 그림 · 터미널 글자는 원문 그대로)
    static readonly string[] CodeLines =
    {
        "if (hero.alive) hero.hp = 0;", "while (true) Spawn(boss);", "hero.speed *= 0.5f;", "var boss = new God();",
        "for (;;) hero.Nerf();", "throw new Exception(\"GG\");", "git push --force origin main", "player.lives = -1;",
        "if (bug) return feature;", "deadline = now - 1;", "return new Bug();", "delete hero.save; hero = null;",
    };
    static readonly string[] CommentLines = { "// TODO: nerf hero", "// FIXME: works on my PC", "// do not touch", "// magic number" };
    static readonly string[] CodeKeywords = { "if", "while", "true", "var", "new", "for", "throw", "return", "delete", "null", "git", "push" };
    static readonly int[] codeOrder = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
    static Color[][] codeColors;
    static readonly Color SynKey = new Color(0.45f, 0.7f, 1f), SynNum = new Color(0.6f, 1f, 0.6f), SynStr = new Color(1f, 0.72f, 0.4f),
                          SynSym = new Color(1f, 0.85f, 0.35f), SynName = new Color(0.92f, 0.94f, 1f);
    static readonly Color GhostCode = new Color(0.45f, 0.8f, 0.55f, 0.5f);

    // 줄마다 글자 색 (문법 색 · 한 번만 계산)
    static Color[] CodeColorsOf(int line)
    {
        if (codeColors == null) codeColors = new Color[CodeLines.Length][];
        if (codeColors[line] != null) return codeColors[line];
        string s = CodeLines[line];
        Color[] c = new Color[s.Length];
        bool str = false;
        for (int i = 0; i < s.Length;)
        {
            char ch = s[i];
            if (ch == '"') { c[i] = SynStr; str = !str; i++; continue; }
            if (str) { c[i] = SynStr; i++; continue; }
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                int j = i;
                while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] == '_')) j++;
                string w = s.Substring(i, j - i);
                Color col = char.IsDigit(ch) ? SynNum : System.Array.IndexOf(CodeKeywords, w) >= 0 ? SynKey : SynName;
                for (int k = i; k < j; k++) c[k] = col;
                i = j;
                continue;
            }
            c[i] = SynSym;
            i++;
        }
        codeColors[line] = c;
        return c;
    }

    // 줄의 칸 수 (글자 1칸 · 띄어쓰기 4칸)
    static int CellsOf(string s)
    {
        int n = 0;
        foreach (char ch in s) n += ch == ' ' ? 4 : 1;
        return n;
    }

    static int CharsOf(string s)
    {
        int n = 0;
        foreach (char ch in s) if (ch != ' ') n++;
        return n;
    }

    const float CodeCell = 0.75f;
    readonly SpriteRenderer[] ghostSr = new SpriteRenderer[MaxOwnGhost];
    readonly Vector3[] ghostFrom = new Vector3[MaxOwnGhost];
    int ghostCount;
    Vector2 ghostDir;
    float ghostSpeed, ghostAt;

    IEnumerator LiveCoding(int e)
    {
        int ph = Mathf.Clamp(ownPhase, 0, 2);
        int lines = ph == 0 ? 4 : ph == 1 ? 5 : 6;
        float gap = ph == 0 ? 0.42f : ph == 1 ? 0.38f : 0.34f;
        float speed = ph == 0 ? 7f : ph == 1 ? 7.5f : 8f;
        int ctrlz = ph;
        int commentAt = ph >= 1 && Random.value < 0.35f ? Random.Range(1, lines) : -1;
        if (!ownCodeSeen)
        {
            ownCodeSeen = true;
            Hints.Show("dev.code", "개발자의 「라이브 코딩」은 글자 하나하나가 탄입니다. 띄어쓰기 자리로 빠져나가세요. 초록 「//」 주석 줄은 실행되지 않아 맞아도 아프지 않습니다.");
        }
        // 한 번 쓸 때 같은 줄을 두 번 쓰지 않음 (정적 배열을 섞음)
        for (int i = codeOrder.Length - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int t = codeOrder[i]; codeOrder[i] = codeOrder[j]; codeOrder[j] = t; }

        float start = Time.time;
        float typeEnd = start + 0.5f;
        TextMeshPro term = OwnText(transform.position + Vector3.up * (OwnHeadY + 1.4f), "", Terminal, 4f, 47);
        int fired = 0;
        float nextKey = start;
        float end = typeEnd + (lines - 1) * gap + 0.3f;
        // Ctrl+Z 줄 (마지막 ctrlz 줄)
        int rwGroups = 0;
        int[] grpFrom = rwGroupFrom, grpTo = rwGroupTo;
        float[] grpAt = rwGroupAt;
        int[] grpState = rwGroupState;
        ghostCount = 0;
        rwCount = 0;
        int typing = -1;
        float typeFrom = start, typeDur = 0.5f;
        string lineText = null;

        while (Time.time < end)
        {
            if (!OwnOk(e)) yield break;
            float now = Time.time;
            TickAura();
            // 타자: 다음 줄을 터미널에 한 글자씩
            if (fired < lines && typing != fired)
            {
                typing = fired;
                bool comment = fired == commentAt;
                lineText = comment ? CommentLines[Random.Range(0, CommentLines.Length)] : CodeLines[codeOrder[fired % codeOrder.Length]];
                typeFrom = now;
                typeDur = fired == 0 ? Mathf.Max(0.05f, typeEnd - now) : gap * 0.9f;
                if (term != null)
                {
                    term.text = "> " + lineText;
                    term.color = Hostile.Accessible(comment ? GhostCode : Terminal);
                    term.maxVisibleCharacters = 2;
                }
            }
            if (term != null && typing == fired && lineText != null)
            {
                float k = Mathf.Clamp01((now - typeFrom) / Mathf.Max(0.05f, typeDur));
                term.maxVisibleCharacters = 2 + Mathf.CeilToInt(lineText.Length * k);
                term.transform.position = transform.position + Vector3.up * (OwnHeadY + 1.4f);
                if (k < 1f && now >= nextKey) { nextKey = now + 0.06f; Hostile.Play("dev_key", 0.35f, Random.Range(0.9f, 1.3f)); }
            }
            // 줄 발사
            float lineAt = typeEnd + fired * gap;
            if (fired < lines && now >= lineAt)
            {
                if (OwnWinding)
                {
                    // 결계 · 침식 대기: 새 줄은 더 쏘지 않음 (지금까지 쏜 것까지만)
                    lines = fired;
                    end = Mathf.Min(end, now + 0.2f);
                    for (int g = 0; g < rwGroups; g++) end = Mathf.Max(end, grpAt[g] + 1.95f);
                }
                else
                {
                    bool comment = fired == commentAt;
                    bool rewind = !comment && fired >= lines - ctrlz;
                    int from = rwCount;
                    FireCodeLine(lineText, comment ? -1 : codeOrder[fired % codeOrder.Length], speed, comment, rewind);
                    if (rewind && rwCount > from && rwGroups < grpFrom.Length)
                    {
                        grpFrom[rwGroups] = from; grpTo[rwGroups] = rwCount; grpAt[rwGroups] = now; grpState[rwGroups] = 0;
                        rwGroups++;
                        end = Mathf.Max(end, now + 1.95f);
                    }
                    if (comment) end = Mathf.Max(end, now + 1.2f);
                    Hostile.Play("dev_key", 0.6f, 0.7f);
                    fired++;
                }
            }
            // Ctrl+Z: 키캡 (쏜 뒤 1.25초 · 0.6초 보임) → 0.25초 뒤 멈춰 깜빡 (0.35초) → 같은 길을 거꾸로
            for (int g = 0; g < rwGroups; g++)
            {
                float t = now - grpAt[g];
                if (grpState[g] == 0 && t >= 1.25f)
                {
                    grpState[g] = 1;
                    Vector3 head = transform.position + Vector3.up * (OwnHeadY + 2.8f + g * 1.9f);
                    OwnKeycapOut(head + Vector3.left * 1.1f, "Ctrl", 1.8f, out rwCapA[g], out rwCapTa[g]);
                    OwnKeycapOut(head + Vector3.right * 1.3f, "Z", 1f, out rwCapB[g], out rwCapTb[g]);
                    Hostile.Play("dev_key", 0.6f, 1.1f);
                }
                if (grpState[g] == 1 && t >= 1.5f)
                {
                    grpState[g] = 2;
                    for (int i = grpFrom[g]; i < grpTo[g]; i++)
                    {
                        if (!RwValid(i)) continue;
                        rwSpeed[i] = rwShot[i].speed;
                        rwShot[i].speed = 0f;
                        rwFrozen[i] = true;
                    }
                    Hostile.Play("ratchet", 0.4f, 1f);
                }
                if (grpState[g] == 2)
                {
                    bool white = Mathf.Repeat(t * 16f, 1f) < 0.5f;
                    for (int i = grpFrom[g]; i < grpTo[g]; i++) if (rwFrozen[i] && RwValid(i)) rwShot[i].Flash(white ? Color.white : (Color?)null);
                    if (t >= 1.85f)
                    {
                        grpState[g] = 3;
                        HideObj(rwCapA[g]); HideObj(rwCapB[g]); HideObj(rwCapTa[g]); HideObj(rwCapTb[g]);
                        rwCapA[g] = rwCapB[g] = null; rwCapTa[g] = rwCapTb[g] = null;
                        float back = 8.5f * GameMode.ProjectileSpeedMul;
                        for (int i = grpFrom[g]; i < grpTo[g]; i++)
                        {
                            if (!rwFrozen[i]) continue;
                            rwFrozen[i] = false;
                            if (!RwValid(i)) continue;
                            HostileProjectile h = rwShot[i];
                            h.dir = -h.dir;
                            h.speed = back;
                            // life 는 「남은 수명」: 쏜 자리까지 돌아갈 시간 + 0.3초
                            h.life = Vector2.Distance(h.transform.position, rwFrom[i]) / Mathf.Max(0.1f, back) + 0.3f;
                            h.Flash(null);
                        }
                        Hostile.Play("rewind", 0.5f, 1.4f);
                    }
                }
            }
            TickGhosts();
            yield return null;
        }
    }

    readonly int[] rwGroupFrom = new int[2], rwGroupTo = new int[2], rwGroupState = new int[2];
    readonly SpriteRenderer[] rwCapA = new SpriteRenderer[2], rwCapB = new SpriteRenderer[2];
    readonly TextMeshPro[] rwCapTa = new TextMeshPro[2], rwCapTb = new TextMeshPro[2];
    readonly float[] rwGroupAt = new float[2];

    static void HideObj(Component c)
    {
        if (c != null) c.gameObject.SetActive(false);
    }

    void OwnKeycapOut(Vector3 at, string label, float width, out SpriteRenderer cap, out TextMeshPro text)
    {
        cap = OwnIcon(FxFrame("fx_keycap", 0), at, 1.6f, Color.white, 30);
        if (cap != null) cap.transform.localScale = new Vector3(cap.transform.localScale.x * width, cap.transform.localScale.y, 1f);
        text = OwnText(at + new Vector3(0f, 0.05f, 0f), label, new Color(0.1f, 0.08f, 0.15f), 4.2f, 31);
    }

    // 한 줄 발사: 보스 앞 1.2칸에서 조준 방향에 수직으로 늘어선 글자 탄 (띄어쓰기 = 빈칸 4칸 · 줄 전체를 옆으로 ±3 밀어 빈틈 자리가 매번 다름)
    void FireCodeLine(string text, int line, float speed, bool comment, bool rewind)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null || text == null) return;
        Vector2 d = DirTo(pl);
        if (d.sqrMagnitude < 0.01f) d = Vector2.right;
        Vector2 perp = new Vector2(-d.y, d.x);
        Vector3 basePos = transform.position + (Vector3)(d * 1.2f) + (Vector3)(perp * Random.Range(-3f, 3f));
        int cells = CellsOf(text);
        float width = cells * CodeCell;
        if (comment)
        {
            // 주석 줄: 회녹색 반투명 유령 글자 (탄 아님 · 피해 없음 · Ctrl+Z 대상 아님)
            ghostCount = 0;
            ghostDir = d;
            ghostSpeed = speed * GameMode.ProjectileSpeedMul;
            ghostAt = Time.time;
            int cell = 0;
            foreach (char ch in text)
            {
                if (ch == ' ') { cell += 4; continue; }
                Vector3 at = basePos + (Vector3)(perp * ((cell + 0.5f) * CodeCell - width * 0.5f));
                cell++;
                Sprite g = GlyphOf(ch);
                if (g == null || ghostCount >= MaxOwnGhost) continue;
                SpriteRenderer s = OwnGhost(g, at, 0.6f, Hostile.Accessible(GhostCode));
                if (s == null) continue;
                ghostSr[ghostCount] = s;
                ghostFrom[ghostCount] = at;
                ghostCount++;
            }
            return;
        }
        int chars = CharsOf(text);
        if (HostileProjectile.Live.Count + chars > OwnBulletCap) return;       // 반쯤 잘린 벽이 생기지 않게 줄째로 건너뜀
        Color[] cols = line >= 0 ? CodeColorsOf(line) : null;
        int c2 = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == ' ') { c2 += 4; continue; }
            Vector3 at = basePos + (Vector3)(perp * ((c2 + 0.5f) * CodeCell - width * 0.5f));
            c2++;
            Color col = cols != null && i < cols.Length ? cols[i] : SynName;
            HostileProjectile h = OwnShot(at, d, speed, 10f, 0.08f, 0.35f, col, OwnAtk.LiveCoding, ch);
            if (h == null || !rewind || rwCount >= MaxRewind) continue;
            rwShot[rwCount] = h;
            rwSerial[rwCount] = h.Serial;
            rwFrom[rwCount] = at;
            rwFrozen[rwCount] = false;
            rwCount++;
        }
    }

    void TickGhosts()
    {
        if (ghostCount == 0) return;
        float t = Time.time - ghostAt;
        Vector3 off = (Vector3)(ghostDir * ghostSpeed * EnermyController.GlobalSpeedMultiplier * t);
        for (int i = 0; i < ghostCount; i++)
            if (ghostSr[i] != null && ghostSr[i].gameObject.activeSelf) ghostSr[i].transform.position = ghostFrom[i] + off;
    }

    // ================================================================= C2. 범위 선택
    struct SelRect { public Vector3 c; public float w, h; }
    readonly SelRect[] selRects = new SelRect[2];
    readonly LineRenderer[] selLines = new LineRenderer[8];

    static bool InRect(Vector3 p, SelRect r) => Mathf.Abs(p.x - r.c.x) <= r.w * 0.5f + 0.2f && Mathf.Abs(p.y - r.c.y) <= r.h * 0.5f + 0.2f;

    // 플레이어 → 각 변 바깥 1.2칸 네 방향 중 열린 길 수
    static int RectExits(Vector3 p, Vector3 c, float w, float h)
    {
        int n = 0;
        if (PathClear(p, new Vector3(c.x + w * 0.5f + 1.2f, p.y))) n++;
        if (PathClear(p, new Vector3(c.x - w * 0.5f - 1.2f, p.y))) n++;
        if (PathClear(p, new Vector3(p.x, c.y + h * 0.5f + 1.2f))) n++;
        if (PathClear(p, new Vector3(p.x, c.y - h * 0.5f - 1.2f))) n++;
        return n;
    }

    // 플레이어가 안쪽 inner(비율) 안 무작위 자리에 오도록 중심 (출구 둘 이상 · 최대 6번)
    // 1.3.3~ 6번 모두 출구가 모자라면 false — 부르는 쪽이 그 공격을 그만둠 (예전엔 플레이어 자리를 그대로 써서 벽 · 용암 구석에선 피할 수 없었음)
    static bool RectAround(Vector3 p, float w, float h, float inner, int needExits, out Vector3 center)
    {
        for (int t = 0; t < 6; t++)
        {
            Vector3 c = p - new Vector3(Random.Range(-0.5f, 0.5f) * inner * w, Random.Range(-0.5f, 0.5f) * inner * h, 0f);
            if (RectExits(p, c, w, h) >= needExits) { center = c; return true; }
        }
        center = p;
        return false;
    }

    void DrawSel(int k, Vector3 a, Vector3 b, Color c)
    {
        LineRenderer l0 = selLines[k * 4], l1 = selLines[k * 4 + 1], l2 = selLines[k * 4 + 2], l3 = selLines[k * 4 + 3];
        Vector3 ab = new Vector3(b.x, a.y), ba = new Vector3(a.x, b.y);
        DashSeg(l0, a, ab, c, 0.18f);
        DashSeg(l1, ab, b, c, 0.18f);
        DashSeg(l2, b, ba, c, 0.18f);
        DashSeg(l3, ba, a, c, 0.18f);
    }

    void DrawSelRect(int k, SelRect r, Color c) =>
        DrawSel(k, r.c - new Vector3(r.w * 0.5f, r.h * 0.5f), r.c + new Vector3(r.w * 0.5f, r.h * 0.5f), c);

    IEnumerator SelectDelete(int e)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        int ph = Mathf.Clamp(ownPhase, 0, 2);
        int count = ph == 0 ? 1 : 2;
        float w = ph == 2 ? 18f : 16f, h = ph == 2 ? 11f : 10f;
        Vector3 p = pl.transform.position;
        if (!RectAround(p, w, h, 0.6f, 2, out Vector3 c00)) yield break;      // 피할 길이 없는 구석이면 쓰지 않음 (아직 띄운 것 없음 · 끝은 FinishOwn 이 정리)
        selRects[0] = new SelRect { c = c00, w = w, h = h };
        if (count == 2)
        {
            // 두 번째는 첫 사각형 한 변에 겹쳐 붙음 — 붙는 쪽은 플레이어가 움직이던 쪽
            Vector2 v = ownPlayerVel;
            Vector3 off;
            if (v.sqrMagnitude < 1f) v = Random.insideUnitCircle;
            if (Mathf.Abs(v.x) >= Mathf.Abs(v.y)) off = new Vector3(Mathf.Sign(v.x) * 0.75f * w, 0f);
            else off = new Vector3(0f, Mathf.Sign(v.y) * 0.75f * h);
            selRects[1] = new SelRect { c = selRects[0].c + off, w = w, h = h };
        }
        for (int i = 0; i < count * 4; i++) selLines[i] = OwnDash();
        Color gold = Hostile.Accessible(Dev);
        float drag = 0.5f, del = OwnTele(0.45f);
        float t0 = Time.time;
        float deleteAt = t0 + drag * count + del;
        // 최종 사각형은 처음부터 옅게 (끄는 방향으로 차오름)
        int[] sx = selSx, sy = selSy;
        for (int k = 0; k < count; k++)
        {
            sx[k] = Random.value < 0.5f ? -1 : 1;
            sy[k] = Random.value < 0.5f ? -1 : 1;
            SelRect r = selRects[k];
            Vector3 a = new Vector3(r.c.x - sx[k] * r.w * 0.5f, r.c.y), b = new Vector3(r.c.x + sx[k] * r.w * 0.5f, r.c.y);
            OwnLineWarn(a, b, r.h, deleteAt - Time.time, ErrRed);
        }
        SpriteRenderer cursor = OwnIcon(FxFrame("fx_devcursor", 0), selRects[0].c, 1.6f, Color.white, 32);
        Hostile.Play("tick", 0.4f, 1.6f);
        // 끌기: 커서가 한 꼭짓점에서 반대 꼭짓점까지 (ease-out) · 점선 사각형이 따라 커짐
        for (int k = 0; k < count; k++)
        {
            SelRect r = selRects[k];
            Vector3 from = r.c + new Vector3(-sx[k] * r.w * 0.5f, -sy[k] * r.h * 0.5f), to = r.c + new Vector3(sx[k] * r.w * 0.5f, sy[k] * r.h * 0.5f);
            float s = Time.time, until = s + drag;
            while (Time.time < until)
            {
                if (!OwnOk(e)) yield break;
                TickAura();
                float q = Mathf.Clamp01((Time.time - s) / drag);
                q = 1f - (1f - q) * (1f - q);
                Vector3 cur = Vector3.Lerp(from, to, q);
                DrawSel(k, from, cur, gold);
                if (cursor != null)
                {
                    cursor.transform.position = cur + new Vector3(0.5f, -0.6f);
                    cursor.sprite = FxFrame("fx_devcursor", q < 0.15f || q > 0.95f ? 1 : 0);
                }
                yield return null;
            }
            DrawSel(k, from, to, gold);
            if (OwnWinding) break;
        }
        // Del 키캡 → 삭제
        Vector3 head = transform.position + Vector3.up * (OwnHeadY + 2.8f);
        OwnKeycapOut(head, "Del", 1.3f, out SpriteRenderer cap, out TextMeshPro capT);
        Hostile.Play("dev_key", 0.7f, 1f);
        yield return OwnWait(deleteAt, e);
        if (!OwnOk(e)) yield break;
        HideObj(cap); HideObj(capT); HideObj(cursor);
        yield return SelDelete(e, count);
        if (!OwnOk(e) || ph < 2 || OwnWinding) yield break;

        // Ctrl+V: 0.3초 뒤 지운 사각형의 점선 유령이 플레이어 쪽으로 미끄러짐 (90° 돌려 가로 · 세로를 바꿈) → 0.8초 뒤 다시 삭제
        yield return OwnWait(Time.time + 0.3f, e);
        if (!OwnOk(e)) yield break;
        pl = Hostile.Player;
        if (pl == null) yield break;
        p = pl.transform.position;
        SelRect old0 = selRects[0], old1 = selRects[1];
        float nw = old0.h, nh = old0.w;
        Vector3 rel = old1.c - old0.c;
        if (!RectAround(p, nw, nh, 0.6f, 2, out Vector3 c0)) yield break;     // 붙여넣기 자리에 피할 길이 없으면 여기서 끝 (키캡 · 경고를 띄우기 전)
        selRects[0] = new SelRect { c = c0, w = nw, h = nh };
        selRects[1] = new SelRect { c = c0 + new Vector3(-rel.y, rel.x), w = nw, h = nh };
        head = transform.position + Vector3.up * (OwnHeadY + 2.8f);
        OwnKeycapOut(head + Vector3.left * 1.1f, "Ctrl", 1.8f, out SpriteRenderer capC, out TextMeshPro capCt);
        OwnKeycapOut(head + Vector3.right * 1.3f, "V", 1f, out SpriteRenderer capV, out TextMeshPro capVt);
        Hostile.Play("pop", 0.5f, 1.6f);
        float paste = OwnTele(0.8f);
        float pasteAt = Time.time + paste;
        for (int k = 0; k < 2; k++)
        {
            SelRect r = selRects[k];
            OwnLineWarn(new Vector3(r.c.x - r.w * 0.5f, r.c.y), new Vector3(r.c.x + r.w * 0.5f, r.c.y), r.h, paste, ErrRed);
        }
        float s2 = Time.time;
        while (Time.time < pasteAt)
        {
            if (!OwnOk(e)) yield break;
            TickAura();
            float q = Mathf.Clamp01((Time.time - s2) / 0.3f);
            q = 1f - (1f - q) * (1f - q);
            DrawSelRect(0, Lerp(old0, selRects[0], q), gold);
            DrawSelRect(1, Lerp(old1, selRects[1], q), gold);
            yield return null;
        }
        HideObj(capC); HideObj(capCt); HideObj(capV); HideObj(capVt);
        yield return SelDelete(e, 2);
    }

    readonly int[] selSx = new int[2], selSy = new int[2];

    static SelRect Lerp(SelRect a, SelRect b, float q) =>
        new SelRect { c = Vector3.Lerp(a.c, b.c, q), w = Mathf.Lerp(a.w, b.w, q), h = Mathf.Lerp(a.h, b.h, q) };

    // 삭제: 흰 번쩍임 + 흩어지는 픽셀 + 판정 (삭제 한 번에 한 대)
    IEnumerator SelDelete(int e, int count)
    {
        PlayerController pl = Hostile.Player;
        bool inside = false;
        float alpha = GameSettings.Flashes ? 0.5f : 0.2f;
        SpriteRenderer f0 = null, f1 = null;
        for (int k = 0; k < count; k++)
        {
            SelRect r = selRects[k];
            SpriteRenderer f = OwnRect(r.c, r.w, r.h, new Color(1f, 1f, 1f, alpha), 20);
            if (k == 0) f0 = f; else f1 = f;
            for (int i = 0; i < 8; i++)
                Fx.Spawn("fx_spark", r.c + new Vector3(Random.Range(-0.5f, 0.5f) * r.w, Random.Range(-0.5f, 0.5f) * r.h), 1.6f, Random.value < 0.5f ? Color.white : (Color)Terminal, 22f);
            if (pl != null && InRect(pl.HurtCenter, r)) inside = true;      // 1.4.4~ 몸 아래쪽 판정
            HideObj(selLines[k * 4]); HideObj(selLines[k * 4 + 1]); HideObj(selLines[k * 4 + 2]); HideObj(selLines[k * 4 + 3]);
        }
        if (OwnHit(inside, 14f, 0.12f, OwnAtk.SelectDelete)) Hostile.Shake(0.12f);
        Hostile.Play("crack", 0.5f, 1.3f);
        float s = Time.time, until = s + 0.25f;
        while (Time.time < until)
        {
            if (!OwnOk(e)) yield break;
            float a = alpha * (1f - (Time.time - s) / 0.25f);
            if (f0 != null) f0.color = new Color(1f, 1f, 1f, a);
            if (f1 != null) f1.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        HideObj(f0); HideObj(f1);
        // 다음 Ctrl+V 가 같은 점선을 다시 씀
        for (int k = 0; k < count * 4; k++) if (selLines[k] != null) selLines[k].gameObject.SetActive(true);
    }

    // ================================================================= C3. 빌드 실패
    IEnumerator BuildFail(int e)
    {
        int ph = Mathf.Clamp(ownPhase, 0, 2);
        float build = ph == 0 ? 1.5f : ph == 1 ? 1.35f : 1.2f;
        float r = ph == 2 ? 7.5f : 7f;
        int shards = ph == 0 ? 12 : 16;
        Vector3 anchor = transform.position;        // 기준점은 시작 때 보스 자리 (보스가 옮겨도 여기서 터짐)
        yield return BuildOnce(e, anchor, build, r, shards, ph == 2, Loc.T("빌드 중…"));
        if (!OwnOk(e) || ph < 2 || OwnWinding) yield break;
        yield return OwnWait(Time.time + 0.5f, e);
        if (!OwnOk(e)) yield break;
        yield return BuildOnce(e, anchor, 0.9f, 6f, 12, false, Loc.T("다시 빌드…"));
    }

    IEnumerator BuildOnce(int e, Vector3 anchor, float build, float r, int shards, bool second, string label)
    {
        build = OwnTele(build);
        float headY = sr != null ? EnemyHitbox.FrameOf(sr).extents.y + 1.6f : 4f;
        Vector3 barC = anchor + Vector3.up * headY;
        const float len = 5.5f;
        Vector3 L = barC + Vector3.left * len * 0.5f;
        LineRenderer bg = OwnSolid(new Color(0f, 0f, 0f, 0.85f), 0.6f, 40);
        SetSeg(bg, L - Vector3.right * 0.1f, L + Vector3.right * (len + 0.1f));
        LineRenderer fill = OwnSolid(Dev, 0.42f, 41);
        TextMeshPro txt = OwnText(barC + Vector3.up * 1.1f, "", Color.white, 3.6f, 42);
        string fmt = label + " {0:0}%";
        OwnCircle(anchor, r, build, ErrRed);
        float s = Time.time, until = s + build;
        int lastTick = -1;
        while (Time.time < until)
        {
            if (!OwnOk(e)) yield break;
            TickAura();
            float k = Mathf.Clamp01((Time.time - s) / build);
            Color c = k < 0.75f ? Dev : k < 0.9f ? new Color(1f, 0.6f, 0.2f) : ErrRed;
            if (fill != null)
            {
                fill.startColor = fill.endColor = Hostile.Accessible(c);
                SetSeg(fill, L, L + Vector3.right * Mathf.Max(0.01f, len * k));
            }
            if (txt != null) txt.SetText(fmt, Mathf.Floor(k * 100f));
            int tick = Mathf.FloorToInt(k * 10f);
            if (tick != lastTick) { lastTick = tick; Hostile.Play("dev_key", 0.5f, Mathf.Lerp(0.9f, 1.4f, k)); }
            yield return null;
        }
        // 100%: 언제나 실패
        if (fill != null) { fill.startColor = fill.endColor = Hostile.Accessible(ErrRed); SetSeg(fill, L, L + Vector3.right * len); }
        if (txt != null) txt.gameObject.SetActive(false);
        OwnFloat(barC - Vector3.up * 2.2f, Loc.T("빌드 실패!"), ErrRed, 6f);
        Hostile.Play("dev_error", 0.8f, 1f);
        Hostile.Play("boom", 0.8f, 0.85f);
        Hostile.Shake(0.25f);
        Fx.Spawn("fx_explosion", anchor, r * 2.2f, new Color(1f, 0.45f, 0.25f), 18f);
        Fx.Spawn("fx_shock", anchor, r * 2.4f, ErrRed, 20f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            Fx.Spawn("fx_spark", anchor + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r * Random.Range(0.6f, 1f), 1.8f, i % 2 == 0 ? (Color)Terminal : Color.white, 22f);
        }
        if (OwnHit(PlayerIn(anchor, r), 18f, 0.2f, OwnAtk.BuildFail)) Hostile.Shake(0.15f);
        // 오류 조각: 고리 탄 (구간 2 첫 빌드는 0.25초 뒤 반 칸 엇갈린 고리 한 번 더)
        ShardRing(anchor, shards, 0f);
        float hideAt = Time.time + 0.25f;
        bool secondDone = !second;
        while (Time.time < hideAt)
        {
            if (!OwnOk(e)) yield break;
            if (fill != null) fill.enabled = Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
            yield return null;
        }
        if (!secondDone && OwnOk(e)) ShardRing(anchor, shards, 180f / shards);
        HideObj(bg); HideObj(fill);
        if (fill != null) fill.enabled = true;
        yield return OwnWait(Time.time + 0.3f, e);
    }

    void ShardRing(Vector3 at, int n, float offsetDeg)
    {
        if (HostileProjectile.Live.Count + n > OwnBulletCap) return;
        for (int i = 0; i < n; i++)
        {
            float a = (offsetDeg + i * 360f / n) * Mathf.Deg2Rad;
            OwnShot(at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 7f, 10f, 0.08f, 0.4f, ErrRed, OwnAtk.BuildFail, '!');
        }
    }

    // ================================================================= C4. 오류 창
    static readonly string[] ExceptionNames = { "NullReferenceException", "StackOverflowException", "IndexOutOfRangeException", "OutOfMemoryException", "HeroTooStrongException" };
    Vector3 popupC, popupBtn;
    readonly List<Component> popupParts = new List<Component>(12);

    Vector2 PopupSize(int idx) => ownPhase >= 2 ? new Vector2(22f, 13f) : new Vector2(20f, 12f);

    // 창 자리 (플레이어가 안쪽 50%) · 버튼 (창 아래쪽 줄 · 플레이어와 3~8칸 · 안전 자리 · 가는 길이 열림) — 없으면 false (이번엔 오류 창을 쓰지 않음)
    bool PlacePopup(Vector3 p, Vector2 size, out Vector3 c, out Vector3 btn)
    {
        float W = size.x, H = size.y;
        for (int t = 0; t < 6; t++)
        {
            c = p - new Vector3(Random.Range(-0.25f, 0.25f) * W, Random.Range(-0.25f, 0.25f) * H, 0f);
            if (RectExits(p, c, W, H) < 1) continue;
            float y = c.y - H * 0.5f + 2.2f;
            float minX = c.x - W * 0.5f + 1.6f, maxX = c.x + W * 0.5f - 1.6f;
            for (float dx = 3f; dx <= 8f; dx += 0.5f)
            {
                for (int side = 0; side < 2; side++)
                {
                    float x = p.x + (side == 0 ? dx : -dx);
                    if (x < minX || x > maxX) continue;
                    Vector3 b = new Vector3(x, y);
                    if (SafeSpot(b) && PathClear(p, b) && CanOutrun(Vector2.Distance(p, b), 0.95f)) { btn = b; return true; }
                }
            }
        }
        c = btn = p;
        return false;
    }

    IEnumerator ErrorPopup(int e)
    {
        int ph = Mathf.Clamp(ownPhase, 1, 2);
        int windows = ph == 2 ? 2 : 1;
        for (int w = 0; w < windows; w++)
        {
            if (!OwnOk(e)) yield break;
            Vector2 size = PopupSize(w);
            Vector3 c = popupC, btn = popupBtn;
            if (w > 0)
            {
                if (OwnWinding) yield break;
                yield return OwnWait(Time.time + 0.45f, e);
                if (!OwnOk(e)) yield break;
                PlayerController pl = Hostile.Player;
                if (pl == null || !PlacePopup(pl.transform.position, size, out c, out btn)) yield break;
            }
            float warn = ph == 1 ? 1.2f : w == 0 ? 1.05f : 0.95f;
            if (!ownPopupSeen) { ownPopupSeen = true; warn = Mathf.Max(warn, 1.5f); }      // 처음 쓸 때만 소개 박자
            warn = OwnTele(warn);
            heroHoldUntil = Mathf.Max(heroHoldUntil, Time.time + warn + 0.5f);       // 경고 동안 새 영웅 필살기 없음 (끝 시각으로만)
            yield return PopupOnce(e, c, btn, size, warn);
        }
    }

    IEnumerator PopupOnce(int e, Vector3 c, Vector3 btn, Vector2 size, float warn)
    {
        float W = size.x, H = size.y;
        popupParts.Clear();
        Color gold = Hostile.Accessible(Dev);
        AddPart(OwnRect(c + new Vector3(0.4f, -0.4f), W, H, new Color(0f, 0f, 0f, 0.35f), 4));
        OwnLineWarn(new Vector3(c.x, c.y + H * 0.5f), new Vector3(c.x, c.y - H * 0.5f), W, warn, ErrRed);
        LineRenderer border = OwnSolid(gold, 0.2f, 23);
        Vector3 tl = c + new Vector3(-W * 0.5f, H * 0.5f), tr = c + new Vector3(W * 0.5f, H * 0.5f), br = c + new Vector3(W * 0.5f, -H * 0.5f), bl = c + new Vector3(-W * 0.5f, -H * 0.5f);
        SetBox(border, tl, tr, br, bl);
        AddPart(border);
        AddPart(OwnRect(c + new Vector3(0f, H * 0.5f - 0.8f), W, 1.6f, new Color(gold.r, gold.g, gold.b, 0.95f), 25));
        AddPart(OwnText(c + new Vector3(-W * 0.5f + 3f, H * 0.5f - 0.8f), "! " + Loc.T("devown.popup.title"), new Color(0.12f, 0.08f, 0.1f), 4.5f, 26));
        AddPart(OwnText(c + new Vector3(0f, 1.6f), ExceptionNames[Random.Range(0, ExceptionNames.Length)], Color.white, 4.2f, 26));
        AddPart(OwnText(c + new Vector3(0f, -0.4f), Loc.T("devown.popup.msg" + Random.Range(0, 3)), new Color(1f, 1f, 1f, 0.9f), 3.4f, 26));
        LineRenderer ring = OwnSolid(Terminal, 0.22f, 27);
        if (ring != null) { ring.loop = true; Hostile.SetArc(ring, btn, 1.5f, 0f, 354f); }
        AddPart(ring);
        SpriteRenderer glow = OwnIcon(Hostile.Glow, btn, 4.2f, new Color(Terminal.r, Terminal.g, Terminal.b, 0.35f), 5);
        AddPart(glow);
        AddPart(OwnText(btn, Loc.T("확인"), Terminal, 4f, 28));
        Hostile.Play("dev_error", 0.5f, 1.2f);
        float s = Time.time, until = s + warn;
        while (Time.time < until)
        {
            if (!OwnOk(e)) yield break;
            TickAura();
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
            if (glow != null) glow.color = new Color(Terminal.r, Terminal.g, Terminal.b, 0.25f + 0.25f * pulse);
            if (ring != null) ring.startWidth = ring.endWidth = 0.18f + 0.1f * pulse;
            yield return null;
        }
        // 쾅: 창 안은 지워지고 「확인」 버튼 위만 안전 (창 밖도 안전)
        PlayerController pl = Hostile.Player;
        bool inWin = pl != null && Mathf.Abs(pl.transform.position.x - c.x) <= W * 0.5f + 0.2f && Mathf.Abs(pl.transform.position.y - c.y) <= H * 0.5f + 0.2f;
        bool onBtn = pl != null && Vector2.Distance(pl.transform.position, btn) <= 1.5f + 0.15f;
        SpriteRenderer flash = OwnRect(c, W, H, new Color(1f, 1f, 1f, GameSettings.Flashes ? 0.5f : 0.2f), 21);
        AddPart(flash);
        Fx.Spawn("fx_shock", c, Mathf.Max(W, H), ErrRed, 20f);
        Hostile.Play("thump", 0.9f, 1f);
        Hostile.Play("dev_error", 0.6f, 0.9f);
        if (inWin && !onBtn) { if (OwnHit(true, 16f, 0.16f, OwnAtk.ErrorPopup)) Hostile.Shake(0.15f); }
        else if (onBtn && pl != null) { OwnFloat(pl.transform.position, Loc.T("devown.popup.ok"), Terminal, 5f); Hostile.Play("ding", 0.5f, 1.6f); }
        // 버튼 쪽으로 0.2초 동안 줄어들며 닫힘
        float cs = Time.time, cu = cs + 0.2f;
        while (Time.time < cu)
        {
            if (!OwnOk(e)) yield break;
            float q = Mathf.Clamp01((Time.time - cs) / 0.2f);
            SetBox(border, Vector3.Lerp(tl, btn, q), Vector3.Lerp(tr, btn, q), Vector3.Lerp(br, btn, q), Vector3.Lerp(bl, btn, q));
            if (flash != null) flash.color = new Color(1f, 1f, 1f, (GameSettings.Flashes ? 0.5f : 0.2f) * (1f - q));
            yield return null;
        }
        foreach (Component part in popupParts) HideObj(part);
        popupParts.Clear();
    }

    void AddPart(Component c)
    {
        if (c != null) popupParts.Add(c);
    }

    static void SetBox(LineRenderer l, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        if (l == null) return;
        l.loop = true;
        if (l.positionCount != 4) l.positionCount = 4;
        l.SetPosition(0, a); l.SetPosition(1, b); l.SetPosition(2, c); l.SetPosition(3, d);
    }

    // ================================================================= C5. 로딩 중…
    static readonly Vector2[] Dirs8 =
    {
        new Vector2(1, 0), new Vector2(0.7071f, 0.7071f), new Vector2(0, 1), new Vector2(-0.7071f, 0.7071f),
        new Vector2(-1, 0), new Vector2(-0.7071f, -0.7071f), new Vector2(0, -1), new Vector2(0.7071f, -0.7071f),
    };
    readonly SpriteRenderer[] ringDot = new SpriteRenderer[44];
    readonly SpriteRenderer[] ringOrb = new SpriteRenderer[44];
    readonly int[] ringOf = new int[44];
    readonly float[] ringSlot = new float[44];

    IEnumerator LoadingRing(int e)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        int ph = Mathf.Clamp(ownPhase, 1, 2);
        Vector3 center = pl.transform.position;
        int rings = ph == 2 ? 2 : 1;
        float rOuter = ph == 2 ? 7.2f : 6f;
        // 처음 빈자리 방향: 「가운데 → 빈자리 바깥 r + 1.5」 길이 열린 8방향 중 무작위
        int start = Random.Range(0, 8), gapDir = start;
        for (int i = 0; i < 8; i++)
        {
            int k = (start + i) % 8;
            if (PathClear(center, center + (Vector3)(Dirs8[k] * (rOuter + 1.5f)))) { gapDir = k; break; }
        }
        float gapAng = Mathf.Atan2(Dirs8[gapDir].y, Dirs8[gapDir].x) * Mathf.Rad2Deg;
        float[] radius = ringRadius, speed = ringSpeed, ang = ringAng;
        int[] dots = ringDots;
        if (rings == 1) { radius[0] = 6f; dots[0] = 24; speed[0] = 75f * (Random.value < 0.5f ? -1f : 1f); }
        else
        {
            radius[0] = 3.6f; dots[0] = 16; speed[0] = 85f;
            radius[1] = 7.2f; dots[1] = 28; speed[1] = -65f;
        }
        int n = 0;
        Color gold = Dev;
        for (int r = 0; r < rings; r++)
        {
            ang[r] = gapAng;
            for (int i = 0; i < dots[r]; i++)
            {
                // 빈자리: 0번 · 마지막 칸 둘이 빠짐 (빈자리 가운데 = gapAng)
                if (i == 0 || i == dots[r] - 1) continue;
                if (n >= ringDot.Length) break;
                SpriteRenderer d = OwnDot(center, gold, 1.0f, out SpriteRenderer orb);
                if (d == null) continue;
                ringDot[n] = d; ringOrb[n] = orb; ringOf[n] = r; ringSlot[n] = i + 0.5f;
                n++;
            }
        }
        float total = ph == 2 ? 1.8f : 1.4f;
        TextMeshPro txt = OwnText(center + Vector3.up * 0.2f, Loc.T("로딩 중…") + " 99%", gold, 3.6f, 30);
        OwnCircle(center, rOuter, total + 0.35f, Dev);
        Hostile.Play("chime", 0.4f, 1.3f);
        float s = Time.time, until = s + total;
        float nextTick = s;
        float gatherUntil = until + 0.35f;
        while (Time.time < gatherUntil)
        {
            if (!OwnOk(e)) yield break;
            TickAura();
            float now = Time.time, dt = Time.deltaTime * EnermyController.GlobalSpeedMultiplier;
            float shrink = now < until ? 1f : 1f - Mathf.Clamp01((now - until) / 0.35f);
            for (int r = 0; r < rings; r++) ang[r] += speed[r] * dt;
            pl = Hostile.Player;
            bool touch = false;
            for (int i = 0; i < n; i++)
            {
                if (ringDot[i] == null) continue;
                int r = ringOf[i];
                float a = (ang[r] + (ringSlot[i] - 0.5f) * 360f / dots[r] + 360f / dots[r] * 0.5f) * Mathf.Deg2Rad;
                Vector3 at = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius[r] * shrink;
                ringDot[i].transform.position = at;
                if (ringOrb[i] != null)
                {
                    Color oc = ringOrb[i].color;
                    oc.a = Mathf.Lerp(0.45f, 1f, ringSlot[i] / dots[r]);      // 스피너처럼 앞에서 뒤로 옅어짐
                    ringOrb[i].color = oc;
                }
                if (pl != null && now < until && Hostile.HurtDistance(pl, at) < 0.75f) touch = true;
            }
            if (touch) OwnHit(true, 10f, 0.08f, OwnAtk.Loading);
            if (now >= nextTick && now < until) { nextTick = now + 0.25f; Hostile.Play("tick", 0.25f, 1.8f); }
            yield return null;
        }
        // 모임: 끝나는 순간 고리 안이면 한 대
        Hostile.Play("crack", 0.5f, 1.2f);
        Hostile.Play("pop", 0.5f, 1.3f);
        Fx.Spawn("fx_shock", center, rOuter * 1.2f, Dev, 22f);
        OwnHit(PlayerIn(center, rOuter - 0.2f), 14f, 0.12f, OwnAtk.Loading);
        HideObj(txt);
        for (int i = 0; i < n; i++) HideObj(ringDot[i]);
        yield return OwnWait(Time.time + 0.2f, e);
    }

    readonly float[] ringRadius = new float[2], ringSpeed = new float[2], ringAng = new float[2];
    readonly int[] ringDots = new int[2];

    // ================================================================= C6. 스택 오버플로
    readonly SpriteRenderer[] stackBlock = new SpriteRenderer[24];

    IEnumerator StackOverflow(int e)
    {
        int towers = ownStackUses >= 1 ? 2 : 1;      // 두 번째 쓸 때부터 두 탑
        ownStackUses++;
        int side0 = Random.value < 0.5f ? -1 : 1;
        Vector3[] baseAt = stackBase;
        float[] t0 = stackT0;
        int[] placed = stackPlaced, stage = stackStage;
        Vector2[] dir = stackDir;
        TextMeshPro[] label = stackLabel;
        float now0 = Time.time;
        for (int k = 0; k < towers; k++)
        {
            baseAt[k] = transform.position + Vector3.right * (k == 0 ? side0 : -side0) * 2.5f + Vector3.down * (sr != null ? EnemyHitbox.FrameOf(sr).extents.y * 0.6f : 1f);     // 1.6.0~ 고정 틀 (탑 자리 = 판정)
            t0[k] = now0 + k * 0.45f;
            placed[k] = 0;
            stage[k] = 0;
            dir[k] = Vector2.right;
            label[k] = null;
        }
        string fmt = Loc.T("devown.stack") + " {0:0}";
        float end = now0 + (towers - 1) * 0.45f + 1.2f + 0.15f + 0.5f;
        while (Time.time < end)
        {
            if (!OwnOk(e)) yield break;
            TickAura();
            float now = Time.time;
            for (int k = 0; k < towers; k++)
            {
                float t = now - t0[k];
                if (t < 0f) continue;
                if (stage[k] == 0 && k > 0 && OwnWinding) { stage[k] = 9; continue; }      // 결계 대기: 새 탑은 쌓지 않음
                // 쌓기: 0.08초마다 하나씩 12개
                int want = Mathf.Min(12, Mathf.FloorToInt(t / 0.08f) + 1);
                while (stage[k] < 2 && placed[k] < want)
                {
                    int i = placed[k];
                    stackBlock[k * 12 + i] = OwnBlock(baseAt[k] + Vector3.up * (0.6f + i * 1.0f), 1.1f);
                    placed[k]++;
                    Hostile.Play("dev_key", 0.4f, Mathf.Lerp(0.8f, 1.6f, i / 11f));
                    if (label[k] == null) label[k] = OwnText(baseAt[k] + Vector3.up * (1.8f + i), "", Dev, 3.4f, 30);
                    if (label[k] != null)
                    {
                        label[k].transform.position = baseAt[k] + Vector3.up * (1.6f + i * 1.0f);
                        label[k].SetText(fmt, placed[k]);
                    }
                }
                // 6개째(0.45초): 쓰러질 방향을 그때 플레이어 쪽으로 고정 → 띠 경고 0.75초
                if (stage[k] == 0 && t >= 0.45f)
                {
                    stage[k] = 1;
                    PlayerController pl = Hostile.Player;
                    Vector2 d = pl != null ? ((Vector2)(pl.transform.position - baseAt[k])).normalized : Vector2.right;
                    if (d.sqrMagnitude < 0.01f) d = Vector2.right;
                    dir[k] = d;
                    OwnLineWarn(baseAt[k], baseAt[k] + (Vector3)(d * 13f), 2.6f, OwnTele(0.75f), ErrRed);
                }
                // 1.2초: 쓰러짐 (0.15초 동안 세로 줄에서 띠 위로 눕힘)
                if (stage[k] == 1 && t >= 1.2f)
                {
                    float q = Mathf.Clamp01((t - 1.2f) / 0.15f);
                    float from = 90f, to = Mathf.Atan2(dir[k].y, dir[k].x) * Mathf.Rad2Deg;
                    float a = Mathf.LerpAngle(from, to, q);
                    Vector3 axis = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                    for (int i = 0; i < placed[k]; i++)
                    {
                        SpriteRenderer b = stackBlock[k * 12 + i];
                        if (b == null) continue;
                        b.transform.position = baseAt[k] + axis * (0.6f + i * 1.0f);
                        b.transform.rotation = Quaternion.Euler(0f, 0f, a - 90f);
                    }
                    if (label[k] != null) { HideObj(label[k]); label[k] = null; }
                    if (q >= 1f)
                    {
                        stage[k] = 2;
                        Vector3 end0 = baseAt[k] + (Vector3)(dir[k] * 13f);
                        OwnFloat(Vector3.Lerp(baseAt[k], end0, 0.5f), Loc.T("스택 오버플로!"), ErrRed, 6f);
                        Hostile.Play("crack", 0.7f, 0.9f);
                        Hostile.Play("thump", 0.8f, 1f);
                        Hostile.Shake(0.2f);
                        PlayerController pl = Hostile.Player;
                        bool hit = pl != null && Hostile.HurtToSegment(pl, baseAt[k], end0) < 1.3f + 0.2f;
                        OwnHit(hit, 16f, 0.16f, OwnAtk.StackOverflow);
                        // 부딪힌 블록마다 작은 탄이 띠 양옆으로 번갈아
                        Vector2 perp = new Vector2(-dir[k].y, dir[k].x);
                        if (HostileProjectile.Live.Count + placed[k] <= OwnBulletCap)
                            for (int i = 0; i < placed[k]; i++)
                            {
                                Vector3 at = baseAt[k] + (Vector3)(dir[k] * (0.6f + i * 1.0f));
                                float sgn = i % 2 == 0 ? 1f : -1f;
                                OwnShot(at, perp * sgn, 6f, 10f, 0.08f, 0.35f, Dev, OwnAtk.StackOverflow, sgn > 0f ? '{' : '}');
                            }
                        for (int i = 0; i < placed[k]; i++) Fx.Spawn("fx_spark", baseAt[k] + (Vector3)(dir[k] * (0.6f + i * 1.0f)), 1.4f, Dev, 22f);
                    }
                }
                // 쓰러진 블록은 0.4초 뒤 사라짐
                if (stage[k] == 2 && t >= 1.2f + 0.15f + 0.4f)
                {
                    stage[k] = 3;
                    for (int i = 0; i < placed[k]; i++) HideObj(stackBlock[k * 12 + i]);
                }
            }
            yield return null;
        }
    }

    readonly Vector3[] stackBase = new Vector3[2];
    readonly float[] stackT0 = new float[2];
    readonly int[] stackPlaced = new int[2], stackStage = new int[2];
    readonly Vector2[] stackDir = new Vector2[2];
    readonly TextMeshPro[] stackLabel = new TextMeshPro[2];
}
