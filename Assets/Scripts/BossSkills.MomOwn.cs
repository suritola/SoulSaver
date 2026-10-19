using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 개발자 엄마 고유 기본 공격 6종 (기획서 D절) — 차례 · 정리 · 풀은 BossSkills.OwnKit.cs
//   슬리퍼 투척 · 효자손 · 타임 세일! (구간 0~) · 빗자루질 (75%~) · 엄친아 (50%~) · 불 끄고 자! (25%~)
//   보스를 옮기는 공격(효자손 다가서기 · 타임 세일 돌진)은 transform.position 만 바꾸고 다른 상태는 건드리지 않음 (끊기면 그 자리에 선 채 끝)
public partial class BossSkills
{
    // ================================================================= D1. 슬리퍼 투척
    const int MaxSlippers = 3;
    readonly int[] slStage = new int[MaxSlippers];
    readonly float[] slAt = new float[MaxSlippers], slSpeed = new float[MaxSlippers];
    readonly bool[] slBounce = new bool[MaxSlippers], slHit = new bool[MaxSlippers];
    readonly Vector3[] slFrom = new Vector3[MaxSlippers], slTo = new Vector3[MaxSlippers], slPos = new Vector3[MaxSlippers];
    readonly SpriteRenderer[] slSprite = new SpriteRenderer[MaxSlippers];

    // 끊김 정리 (CancelOwn): 슬리퍼 그림은 풀 아이콘이라 HideOwnPools 가 숨김 — 여기서는 참조만 비움
    void KillSlippers()
    {
        for (int i = 0; i < MaxSlippers; i++) { slSprite[i] = null; slStage[i] = 9; }
    }

    IEnumerator SlipperToss(int e)
    {
        int ph = Mathf.Clamp(ownPhase, 0, 3);
        int count = ph == 0 ? 1 : ph == 3 ? 3 : 2;
        float interval = ph == 3 ? 0.25f : 0.3f;
        float t0 = Time.time + 0.4f;
        Hostile.Play("whoosh", 0.4f, 0.6f);
        for (int i = 0; i < MaxSlippers; i++)
        {
            slStage[i] = i < count ? 0 : 9;
            slAt[i] = t0 + i * interval;
            slBounce[i] = (ph == 2 && i == 1) || (ph == 3 && i >= 1);
            slHit[i] = false;
            slSprite[i] = null;
        }
        float hardEnd = t0 + 6f;
        Color pink = MomPink;
        while (Time.time < hardEnd)
        {
            if (!OwnOk(e)) yield break;
            TickAura();
            float now = Time.time, dt = Time.deltaTime;
            bool any = false;
            PlayerController pl = Hostile.Player;
            for (int i = 0; i < count; i++)
            {
                if (slStage[i] == 9) continue;
                any = true;
                switch (slStage[i])
                {
                    case 0:     // 경고 선: 엄마 → 그때 플레이어 + (엄마→플레이어 방향으로) 6칸
                        if (now < slAt[i]) break;
                        if (OwnWinding || pl == null) { slStage[i] = 9; break; }
                        {
                            Vector3 from = transform.position, p = pl.transform.position;
                            Vector2 d = ((Vector2)(p - from)).normalized;
                            if (d.sqrMagnitude < 0.01f) d = Vector2.right;
                            Vector3 to = Hostile.ClampArena(p + (Vector3)(d * 6f));
                            if (Hostile.IsWall(to)) to = p;
                            slFrom[i] = from; slTo[i] = to;
                            float warn = OwnTele(0.5f);
                            OwnLineWarn(from, to, 1.6f, warn, pink);
                            slAt[i] = now + warn;
                            slStage[i] = 1;
                            if (i == 1) OwnFloat(transform.position, Loc.T("한 짝 더!"), pink, 5f);
                        }
                        break;
                    case 1:     // 던짐
                        if (now < slAt[i]) break;
                        Hostile.Play("whoosh", 0.8f, 1.3f);
                        slPos[i] = slFrom[i];
                        slSpeed[i] = 26f;
                        slAt[i] = now + Vector2.Distance(slFrom[i], slTo[i]) / 26f + 0.5f;      // 비행 시간 상한
                        slSprite[i] = OwnIcon(FxFrame("fx_slipper", 0), slFrom[i], 1.8f, Color.white, 28);
                        slStage[i] = 2;
                        break;
                    case 2:
                    case 4:     // 날기 (매 프레임 지난 자리 → 지금 자리 선분 판정)
                        {
                            Vector3 prev = slPos[i];
                            float step = slSpeed[i] * EnermyController.GlobalSpeedMultiplier * dt;
                            Vector3 cur = Vector3.MoveTowards(prev, slTo[i], step);
                            slPos[i] = cur;
                            if (slSprite[i] != null)
                            {
                                slSprite[i].transform.position = cur;
                                slSprite[i].transform.Rotate(0f, 0f, 1080f * dt);
                            }
                            if (!slHit[i] && pl != null && Hostile.HurtToSegment(pl, prev, cur) < 0.8f + 0.2f
                                && OwnHit(true, 14f, 0.12f, OwnAtk.Slipper))
                            {
                                slHit[i] = true;
                                Hostile.Play("mom_slap", 0.9f, 1f);
                                Hostile.Shake(0.12f);
                            }
                            bool arrived = (cur - slTo[i]).sqrMagnitude < 0.0001f || now >= slAt[i];
                            if (!arrived) break;
                            if (slStage[i] == 2 && slBounce[i] && !OwnWinding && pl != null)
                            {
                                // 튕김: 끝점 → 그때 플레이어 + 5칸 (경고 0.42초) → 속도 24로 한 번 더
                                Fx.Spawn("fx_shock", cur, 2.4f, pink, 22f);
                                Hostile.Play("stamp", 0.7f, 1.1f);
                                Vector3 p = pl.transform.position;
                                Vector2 d = ((Vector2)(p - cur)).normalized;
                                if (d.sqrMagnitude < 0.01f) d = Vector2.right;
                                Vector3 to = Hostile.ClampArena(p + (Vector3)(d * 5f));
                                if (Hostile.IsWall(to)) to = p;
                                slFrom[i] = cur; slTo[i] = to;
                                float warn = OwnTele(0.42f);
                                OwnLineWarn(cur, to, 1.6f, warn, pink);
                                slAt[i] = now + warn;
                                slStage[i] = 3;
                            }
                            else
                            {
                                slAt[i] = now + 0.4f;       // 바닥에 0.4초 놓였다가 흐려짐
                                slStage[i] = 5;
                            }
                        }
                        break;
                    case 3:
                        if (now < slAt[i]) break;
                        Hostile.Play("whoosh", 0.7f, 1.4f);
                        slSpeed[i] = 24f;
                        slHit[i] = false;
                        slAt[i] = now + Vector2.Distance(slFrom[i], slTo[i]) / 24f + 0.5f;
                        slStage[i] = 4;
                        break;
                    case 5:
                        if (slSprite[i] != null) slSprite[i].color = new Color(1f, 1f, 1f, Mathf.Clamp01((slAt[i] - now) / 0.4f));
                        if (now >= slAt[i]) { HideObj(slSprite[i]); slSprite[i] = null; slStage[i] = 9; }
                        break;
                }
            }
            if (!any) yield break;
            yield return null;
        }
    }

    // ================================================================= D2. 효자손
    static void SetConePts(LineRenderer l, Vector3 apex, Vector2 dir, float half, float range)
    {
        if (l == null) return;
        const int seg = 16;
        int n = seg + 3;
        Vector3[] buf = Hostile.PointBuffer(n);
        buf[0] = apex;
        for (int i = 0; i <= seg; i++)
        {
            float a = Mathf.Lerp(-half, half, i / (float)seg);
            buf[i + 1] = apex + (Vector3)((Vector2)(Quaternion.Euler(0, 0, a) * dir) * range);
        }
        buf[seg + 2] = apex;
        if (l.positionCount != n) l.positionCount = n;
        l.SetPositions(buf);
    }

    static void SetConeArc(LineRenderer l, Vector3 apex, Vector2 dir, float half, float r)
    {
        if (l == null) return;
        float baseAng = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Hostile.SetArc(l, apex, Mathf.Max(0.05f, r), baseAng - half, baseAng + half);
    }

    IEnumerator BackScratcher(int e)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        int ph = Mathf.Clamp(ownPhase, 0, 3);
        int swings = ph <= 1 ? 1 : ph == 2 ? 2 : 3;
        float R = ph <= 1 ? 8f : 9f;
        Vector3 s0 = transform.position;
        Vector2 d = DirTo(pl);
        if (d.sqrMagnitude < 0.01f) d = Vector2.right;
        // 다가서기: 보스를 옮기는 영웅 흉내가 돌고 있으면 제자리 (보스 위치를 두 코루틴이 같이 쓰지 않게)
        float approach = heroMovers > 0 ? 0f : Mathf.Clamp(Vector2.Distance(s0, pl.transform.position) - 4.5f, 0f, 4f);
        Vector3 apex = s0 + (Vector3)(d * approach);
        if (approach > 0f && (!Hostile.InArena(apex) || Hostile.IsWall(apex))) { approach = 0f; apex = s0; }
        float approachT = approach > 0f ? 0.45f : 0f;
        float warn0 = OwnTele(0.65f);
        heroHoldUntil = Mathf.Max(heroHoldUntil, Time.time + approachT + warn0 + 0.2f + 0.3f);     // 다가서기 ~ 첫 휘두름 (끝 시각으로만)
        Color pink = MomPink;
        float dir = 1f;
        for (int k = 0; k < swings; k++)
        {
            if (!OwnOk(e)) yield break;
            if (k > 0)
            {
                if (OwnWinding) yield break;
                yield return OwnWait(Time.time + 0.2f, e);
                if (!OwnOk(e)) yield break;
                pl = Hostile.Player;
                if (pl == null) yield break;
                Vector2 nd = ((Vector2)(pl.transform.position - apex)).normalized;
                if (nd.sqrMagnitude > 0.01f) d = nd;
                dir = -dir;      // 반대로 휘두름
            }
            float warn = k == 0 ? warn0 : OwnTele(ph == 2 ? 0.45f : 0.42f);
            float lead = k == 0 ? approachT : 0f;
            // 부채꼴 경고: 시작 때부터 다가설 자리를 꼭짓점으로 (채움 + 검정 밑선 + 분홍 테두리 + 꼭짓점에서 커지는 진행 호)
            OwnCone(apex, d, R, new Color(pink.r, pink.g, pink.b, 0.22f));
            LineRenderer under = OwnSolid(new Color(0f, 0f, 0f, 0.8f), 0.42f, 17);
            LineRenderer edge = OwnSolid(pink, 0.22f, 18);
            LineRenderer prog = OwnSolid(Color.white, 0.16f, 19);
            SetConePts(under, apex, d, ConeHalf, R);
            SetConePts(edge, apex, d, ConeHalf, R);
            float s = Time.time, until = s + lead + warn;
            while (Time.time < until)
            {
                if (!OwnOk(e)) yield break;
                TickAura();
                float now = Time.time;
                if (lead > 0f)
                {
                    float q = Mathf.Clamp01((now - s) / lead);
                    q = 1f - (1f - q) * (1f - q);
                    transform.position = Vector3.Lerp(s0, apex, q);
                }
                float pr = Mathf.Clamp01((now - s) / (lead + warn));
                SetConeArc(prog, apex, d, ConeHalf, R * pr);
                ConeColor(new Color(pink.r, pink.g, pink.b, 0.15f + 0.2f * pr));
                if (edge != null) edge.startColor = edge.endColor = Hostile.Accessible(new Color(pink.r, pink.g, pink.b, 0.6f + 0.4f * Mathf.PingPong(now * 8f, 1f)));
                yield return null;
            }
            if (lead > 0f) transform.position = apex;
            // 휘두름: 효자손이 꼭짓점을 축으로 −65° → +65° (0.14초) · 판정은 고정 꼭짓점 기준
            PlayerController p = Hostile.Player;
            bool inside = false;
            if (p != null)
            {
                Vector2 to = p.HurtCenter - (Vector2)apex;      // 1.4.4~ 몸 아래쪽 판정
                inside = to.magnitude + PlayerController.HurtShrink < R + 0.3f && Vector2.Angle(d, to) <= ConeHalf + 6f;
            }
            float baseAng = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Fx.Spawn("fx_slash", apex + (Vector3)(d * R * 0.5f), R * 1.4f, pink, 26f, baseAng, 14);
            Hostile.Play("mom_slap", 0.8f, 1f);
            Hostile.Play("whoosh", 0.6f, 0.9f);
            if (OwnHit(inside, 16f, 0.16f, OwnAtk.Scratcher)) Hostile.Shake(0.18f);
            if (oCone != null) oCone.gameObject.SetActive(false);
            HideObj(under); HideObj(edge); HideObj(prog);
            SpriteRenderer stick = OwnIcon(FxFrame("fx_scratcher", 0), apex, Mathf.Min(6f, R * 0.65f), Color.white, 29);
            float len = Mathf.Min(6f, R * 0.65f);
            float ss = Time.time, su = ss + 0.14f;
            while (Time.time < su + 0.1f)
            {
                if (!OwnOk(e)) yield break;
                float q = Mathf.Clamp01((Time.time - ss) / 0.14f);
                float a = baseAng + dir * Mathf.Lerp(-ConeHalf, ConeHalf, q);
                Vector3 ax = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                if (stick != null)
                {
                    stick.transform.position = apex + ax * (len * 0.5f + 0.6f);
                    stick.transform.rotation = Quaternion.Euler(0f, 0f, a - 90f);
                }
                yield return null;
            }
            HideObj(stick);
        }
        yield return OwnWait(Time.time + 0.2f, e);
    }

    // ================================================================= D3. 타임 세일!
    readonly Vector3[] salePts = new Vector3[5];
    readonly TextMeshPro[] saleNums = new TextMeshPro[4];
    int saleGhosts;

    // 꺾은선: 1구간 = 플레이어 옆 5칸 지점까지(거리의 절반) · 2구간 = 플레이어 자리를 지나 5칸 더 · 3구간 = ±70° 꺾어 7칸 · 4구간 = 그때 플레이어 쪽으로 7칸
    //   끝점마다 맵 안으로 · 벽이면 0.7 · 0.4배로 줄임 · 그래도 벽이면 그 앞 구간에서 길을 끝냄
    int BuildSalePath(Vector3 start, Vector3 p, int legs)
    {
        salePts[0] = start;
        Vector2 v = p - start;
        if (v.sqrMagnitude < 0.01f) v = Vector2.right;
        Vector2 perp = new Vector2(-v.y, v.x).normalized * (Random.value < 0.5f ? -1f : 1f);
        Vector3 side = p + (Vector3)(perp * 5f);
        int n = 0;
        for (int j = 0; j < legs; j++)
        {
            Vector3 prev = salePts[n];
            Vector3 cand;
            if (j == 0) cand = start + (side - start) * 0.5f;
            else if (j == 1) cand = p + (p - prev).normalized * 5f;
            else if (j == 2)
            {
                Vector2 dd = (prev - salePts[n - 1]).normalized;
                cand = prev + (Vector3)(Rotate(dd, Random.value < 0.5f ? 70f : -70f) * 7f);
            }
            else
            {
                PlayerController pl = Hostile.Player;
                Vector3 pp = pl != null ? pl.transform.position : p;
                cand = prev + (pp - prev).normalized * 7f;
            }
            Vector3 vec = cand - prev;
            bool ok = false;
            Vector3 q = prev;
            float[] fs = SaleFit;
            for (int f = 0; f < fs.Length; f++)
            {
                q = Hostile.ClampArena(prev + vec * fs[f]);
                if (!Hostile.IsWall(q)) { ok = true; break; }
            }
            if (!ok || (q - prev).sqrMagnitude < 1f) break;
            salePts[++n] = q;
        }
        return n;
    }

    static readonly float[] SaleFit = { 1f, 0.7f, 0.4f };

    IEnumerator TimeSale(int e)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        int ph = Mathf.Clamp(ownPhase, 0, 3);
        int legs = ph <= 1 ? 3 : 4;
        int runs = ph == 3 ? 2 : 1;
        heroHoldUntil = Mathf.Max(heroHoldUntil, Time.time + (runs == 2 ? 6f : 3.5f) + 0.3f);     // 돌진 동안 새 영웅 필살기 없음 (끝 시각으로만)
        OwnFloat(transform.position, Loc.T("반값!"), new Color(1f, 0.85f, 0.2f), 6f);
        Hostile.Play("chime", 0.5f, 1.6f);
        saleGhosts = 0;
        for (int run = 0; run < runs; run++)
        {
            if (!OwnOk(e)) yield break;
            if (run > 0 && (OwnWinding || heroMovers > 0)) yield break;
            pl = Hostile.Player;
            if (pl == null) yield break;
            int n = BuildSalePath(transform.position, pl.transform.position, legs);
            if (n == 0) yield break;
            float warn = OwnTele(run > 0 ? 0.6f : ph <= 1 ? 0.8f : 0.7f);
            for (int j = 0; j < n; j++)
            {
                OwnLineWarn(salePts[j], salePts[j + 1], 3.0f, warn, MomPink);
                saleNums[j] = OwnText(salePts[j + 1] + Vector3.up * 0.6f, (j + 1).ToString(), MomWhite, 4.5f, 33);
            }
            yield return OwnWait(Time.time + warn, e);
            if (!OwnOk(e)) yield break;
            // 돌진: 속도 32 · 잔상 · 먼지 · 구간마다 whoosh (매 프레임 지난 자리 → 지금 자리 선분 판정)
            float nextGhost = Time.time, nextDust = Time.time;
            for (int j = 0; j < n; j++)
            {
                Vector3 to = salePts[j + 1];
                Hostile.Play("whoosh", 0.7f, 1.2f);
                float cap = Time.time + Vector2.Distance(transform.position, to) / 32f + 0.1f;
                while (true)
                {
                    if (!OwnOk(e)) yield break;
                    float now = Time.time;
                    Vector3 prev = transform.position;
                    Vector3 cur = Vector3.MoveTowards(prev, to, 32f * EnermyController.GlobalSpeedMultiplier * Time.deltaTime);
                    if (now >= cap) cur = to;
                    transform.position = cur;
                    TickAura();
                    PlayerController p = Hostile.Player;
                    if (p != null && Hostile.HurtToSegment(p, prev, cur) < 1.8f && OwnHit(true, 16f, 0.16f, OwnAtk.TimeSale))
                    {
                        Hostile.Play("mom_slap", 0.9f, 0.8f);
                        Hostile.Shake(0.18f);
                    }
                    if (now >= nextGhost && saleGhosts < 30 && sr != null && sr.sprite != null)
                    {
                        nextGhost = now + 0.05f;
                        saleGhosts++;
                        FadeSprite.Spawn("SaleGhost", sr.sprite, cur, transform.lossyScale.x, new Color(MomPink.r, MomPink.g, MomPink.b, 0.5f), "Character", -1, 0.3f);
                    }
                    if (now >= nextDust) { nextDust = now + 0.07f; Fx.Spawn("fx_smoke", cur, 2f, new Color(1f, 0.9f, 0.85f, 0.8f), 14f); }
                    if ((cur - to).sqrMagnitude < 0.0001f) break;
                    yield return null;
                }
                HideObj(saleNums[j]);
                saleNums[j] = null;
            }
            OwnFloat(transform.position, Loc.T("득템!"), new Color(1f, 0.85f, 0.2f), 6f);
            Hostile.Play("stamp", 0.8f, 1f);
            if (ph >= 2 && HostileProjectile.Live.Count + 10 <= OwnBulletCap)
                for (int i = 0; i < 10; i++)
                {
                    float a = (i * 36f + Random.Range(0f, 18f)) * Mathf.Deg2Rad;
                    OwnShot(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 7f, 10f, 0.08f, 0.4f, MomWhite, OwnAtk.TimeSale, '%');
                }
            for (int j = 0; j < saleNums.Length; j++) { HideObj(saleNums[j]); saleNums[j] = null; }
            if (run + 1 < runs) yield return OwnWait(Time.time + 0.2f, e);
        }
    }

    // ================================================================= D4. 빗자루질
    class Sweep
    {
        public Vector3 o;               // 시작 때 플레이어 자리 (띠 가운데)
        public Vector2 d, l;            // 쓰는 방향 · 띠 방향
        public float u, s, sFrom, sTo, at;
        public int stage;               // 0 경고 · 1 쓸기 · 2 멈춤(왕복) · 3 돌아오는 경고 · 9 끝
        public bool hit, roundTrip, back;
        public float nextDust, nextSfx;
        public readonly SpriteRenderer[] heads = new SpriteRenderer[7];
        public LineRenderer startLine;
    }
    readonly Sweep[] sweeps = { new Sweep(), new Sweep() };
    static readonly float[] HeadLat = { -11.5f, -7.7f, -3.8f, 0f, 3.8f, 7.7f, 11.5f };
    const float BroomHalf = 13f, BroomGap = 1.6f, BroomSpeed = 13f;

    bool SetupSweep(Sweep w, Vector3 p, int preferDir, bool roundTrip)
    {
        int start = preferDir >= 0 ? preferDir : Random.Range(0, 8);
        int pick = -1;
        float u = Random.Range(-9f, 9f);
        for (int i = 0; i < 8; i++)
        {
            int k = (start + i) % 8;
            Vector2 d = Dirs8[k];
            Vector2 l = new Vector2(-d.y, d.x);
            if (!Hostile.InArena(p - (Vector3)(d * 9f)) || !Hostile.InArena(p + (Vector3)(d * 9f))) continue;
            if (!PathClear(p, p + (Vector3)(l * u))) { u = -u; if (!PathClear(p, p + (Vector3)(l * u))) continue; }
            pick = k;
            break;
        }
        if (pick < 0) return false;
        w.o = p;
        w.d = Dirs8[pick];
        w.l = new Vector2(-w.d.y, w.d.x);
        w.u = u;
        w.sFrom = -9f; w.sTo = 9f; w.s = -9f;
        w.hit = false;
        w.back = false;
        w.roundTrip = roundTrip;
        w.stage = 0;
        w.startLine = null;
        for (int i = 0; i < w.heads.Length; i++) w.heads[i] = null;
        return true;
    }

    void WarnSweep(Sweep w, float warn)
    {
        // 쓸 영역을 빈틈 띠만 비워 두 줄 (시작선 → 끝선으로 차오름)
        float gl = w.u - BroomGap, gr = w.u + BroomGap;
        Vector3 a0 = w.o + (Vector3)(w.d * w.sFrom), b0 = w.o + (Vector3)(w.d * w.sTo);
        // 경고 영역은 시작선 · 끝선 밖으로 1.4 더 (판정이 띠 가운데 ± 1.4 라 시작선 바로 뒤 · 끝선 바로 앞도 맞음 — 경고 없는 피해 방지)
        float ext = 1.4f * Mathf.Sign(w.sTo - w.sFrom);
        Vector3 wa = w.o + (Vector3)(w.d * (w.sFrom - ext)), wb = w.o + (Vector3)(w.d * (w.sTo + ext));
        if (gl > -BroomHalf + 0.3f)
        {
            float mid = (-BroomHalf + gl) * 0.5f, width = gl + BroomHalf;
            OwnLineWarn(wa + (Vector3)(w.l * mid), wb + (Vector3)(w.l * mid), width, warn, MomPink);
        }
        if (gr < BroomHalf - 0.3f)
        {
            float mid = (gr + BroomHalf) * 0.5f, width = BroomHalf - gr;
            OwnLineWarn(wa + (Vector3)(w.l * mid), wb + (Vector3)(w.l * mid), width, warn, MomPink);
        }
        if (w.startLine == null) w.startLine = OwnSolid(new Color(1f, 0.4f, 0.65f, 0.95f), 0.5f, 20);      // 쓸기 시작 때 놓아 줌 (풀에서 다른 띠가 다시 쓸 수 있게)
        SetSeg(w.startLine, a0 - (Vector3)(w.l * BroomHalf), a0 + (Vector3)(w.l * BroomHalf));
        w.at = Time.time + warn;
    }

    void PlaceHeads(Sweep w, bool alt)
    {
        Vector3 band = w.o + (Vector3)(w.d * w.s);
        float rot = Mathf.Atan2(w.d.y, w.d.x) * Mathf.Rad2Deg - 90f;
        for (int i = 0; i < HeadLat.Length; i++)
        {
            float lat = HeadLat[i];
            bool inGap = Mathf.Abs(lat - w.u) < BroomGap + 1.4f;
            // 숨긴 머리는 참조를 놓음 (풀에서 다른 띠가 다시 쓸 수 있어 남의 것을 움직이지 않게)
            if (inGap) { HideObj(w.heads[i]); w.heads[i] = null; continue; }
            if (w.heads[i] == null) w.heads[i] = OwnIcon(FxFrame("fx_broom", 0), band, 2.4f, Color.white, 27);
            SpriteRenderer h = w.heads[i];
            if (h == null) continue;
            h.sprite = FxFrame("fx_broom", alt ? 1 : 0);
            h.transform.position = band + (Vector3)(w.l * lat);
            h.transform.rotation = Quaternion.Euler(0f, 0f, rot + (w.back ? 180f : 0f));
        }
    }

    IEnumerator BroomSweep(int e)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        int ph = Mathf.Clamp(ownPhase, 1, 3);
        Sweep a = sweeps[0], b = sweeps[1];
        if (!SetupSweep(a, pl.transform.position, -1, ph == 3)) yield break;
        b.stage = 9;
        bool wantSecond = ph == 2;
        float warn = OwnTele(0.9f);
        WarnSweep(a, warn);
        float hardEnd = Time.time + 8f;
        while (Time.time < hardEnd)
        {
            if (!OwnOk(e)) yield break;
            TickAura();
            float now = Time.time;
            bool any = false;
            for (int k = 0; k < 2; k++)
            {
                Sweep w = sweeps[k];
                if (w.stage == 9) continue;
                any = true;
                if (w.stage == 0 || w.stage == 3)
                {
                    if (now < w.at) continue;
                    w.stage = 1;
                    w.hit = false;
                    w.nextDust = w.nextSfx = now;
                    if (k == 0 && wantSecond && !OwnWinding)
                    {
                        // 두 번째: 90° 돌린 방향 · 첫째가 움직이기 시작할 때 경고 0.9초
                        wantSecond = false;
                        PlayerController p2 = Hostile.Player;
                        int dirIdx = System.Array.IndexOf(Dirs8, a.d);
                        if (p2 != null && SetupSweep(b, p2.transform.position, dirIdx >= 0 ? (dirIdx + 2) % 8 : -1, false)) WarnSweep(b, warn);
                        else b.stage = 9;
                    }
                }
                if (w.stage == 1)
                {
                    float prevS = w.s;
                    float dir = w.back ? -1f : 1f;
                    w.s += dir * BroomSpeed * EnermyController.GlobalSpeedMultiplier * Time.deltaTime;
                    bool done = w.back ? w.s <= w.sFrom : w.s >= w.sTo;
                    if (done) w.s = w.back ? w.sFrom : w.sTo;
                    PlaceHeads(w, Mathf.Repeat(now * 6f, 1f) < 0.5f);
                    if (w.startLine != null) { HideObj(w.startLine); w.startLine = null; }
                    // 판정: 이번 프레임에 띠가 지나간 구간 (큰 끊김에도 건너뛰지 않게) · 빈틈 밖 · 띠 길이 안
                    PlayerController p = Hostile.Player;
                    if (!w.hit && p != null)
                    {
                        Vector2 rel = p.HurtCenter - (Vector2)w.o;      // 1.4.4~ 몸 아래쪽 판정
                        float sp = Vector2.Dot(rel, w.d), lp = Vector2.Dot(rel, w.l);
                        float lo = Mathf.Min(prevS, w.s) - 1.4f, hi = Mathf.Max(prevS, w.s) + 1.4f;
                        if (sp >= lo && sp <= hi && Mathf.Abs(lp) <= BroomHalf && Mathf.Abs(lp - w.u) >= BroomGap
                            && OwnHit(true, 14f, 0.12f, OwnAtk.Broom))
                        {
                            w.hit = true;
                            Hostile.Shake(0.12f);
                        }
                    }
                    if (now >= w.nextDust)
                    {
                        w.nextDust = now + 0.08f;
                        Vector3 band = w.o + (Vector3)(w.d * (w.s - dir * 1.2f));
                        for (int i = 0; i < 3; i++)
                        {
                            float lat = Random.Range(-BroomHalf, BroomHalf);
                            if (Mathf.Abs(lat - w.u) < BroomGap) continue;
                            Fx.Spawn("fx_smoke", band + (Vector3)(w.l * lat), 1.6f, new Color(0.95f, 0.9f, 0.8f, 0.7f), 14f);
                        }
                    }
                    if (now >= w.nextSfx) { w.nextSfx = now + 0.5f; Hostile.Play("mom_sweep", 0.6f, 1f); }
                    if (done)
                    {
                        if (w.roundTrip && !w.back && !OwnWinding)
                        {
                            // 왕복: 0.5초 깜빡이며 멈춤 + 돌아오는 경고 0.5초 (빈틈 ±5 옮김)
                            w.stage = 2;
                            w.at = now + 0.5f;
                        }
                        else
                        {
                            w.stage = 9;
                            for (int i = 0; i < w.heads.Length; i++) { HideObj(w.heads[i]); w.heads[i] = null; }
                        }
                    }
                }
                else if (w.stage == 2)
                {
                    bool show = Mathf.Repeat(now * 8f, 1f) < 0.5f;
                    for (int i = 0; i < w.heads.Length; i++) if (w.heads[i] != null) w.heads[i].enabled = show;
                    if (now >= w.at)
                    {
                        for (int i = 0; i < w.heads.Length; i++) if (w.heads[i] != null) w.heads[i].enabled = true;
                        w.back = true;
                        w.u = Mathf.Clamp(w.u + (Random.value < 0.5f ? -5f : 5f), -9f, 9f);
                        float t = w.sFrom; w.sFrom = w.sTo; w.sTo = t;     // 경고는 끝선 → 시작선으로 차오름
                        WarnSweep(w, OwnTele(0.5f));
                        w.sTo = w.sFrom; w.sFrom = t;                        // 움직임은 s 를 줄이는 쪽 (back)
                        w.stage = 3;
                        PlaceHeads(w, false);
                    }
                }
            }
            if (!any) yield break;
            yield return null;
        }
    }

    // ================================================================= D5. 엄친아 (메달은 따로 산다 — OwnGuard → TickMedals)
    class OwnMedal
    {
        public BossPart part;
        public SpriteRenderer sr;
        public Vector3 lastPos;
        public float chaseEnd, hardEnd, blastAt, frozenAdd;
        public bool exploding;
        public Telegraph tel;
        public int telId;
    }
    readonly List<OwnMedal> medals = new List<OwnMedal>(3);
    int medalHits;
    const float MedalChase = 6f, MedalSpeed = 6f, MedalBlastR = 2.8f;

    IEnumerator FriendsSon(int e)
    {
        Hints.Show("mom.medal", "「엄친아」 메달은 부술 수 있습니다. 쫓아오기 전에 쏴서 깨뜨리세요. 오래 두면 그 자리에서 터집니다.");
        int ph = Mathf.Clamp(ownPhase, 2, 3);
        int n = Mathf.Min(ph == 3 ? 3 : 2, 3 - medals.Count);
        medalHits = 0;          // 한 번 내놓은 묶음 전체에서 최대 2대
        float hp = Mathf.Max(300f, (boss != null ? boss.setEnemyHP : 0f) * 0.006f);
        Sprite sp = FxFrame("fx_medal", 0);
        float off = Random.Range(0f, 360f);
        EnsureOwnRoot();
        for (int k = 0; k < n && sp != null; k++)
        {
            float a = (off + k * 360f / n) * Mathf.Deg2Rad;
            Vector3 at = Hostile.ClampArena(transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 2.5f);
            float scale = 2.2f / Mathf.Max(0.01f, sp.bounds.size.y);
            BossPart part = BossPart.Make("DevOwnMedal", sp, at, scale, Color.white, hp, 1.0f);
            part.transform.SetParent(ownRoot.transform, true);      // 보스 몸의 자식이 아님 (보스 크기 · 위치를 따라가지 않게 · 일시정지 설명이 보스로 착각하지 않게)
            // 말풍선 (흰 글자 · 분홍 외곽) — 메달 자식이라 메달과 함께 지워짐
            GameObject bub = new GameObject("MedalBubble");
            bub.transform.SetParent(part.transform, false);
            bub.transform.localPosition = new Vector3(0f, 1.9f / scale, 0f);
            bub.transform.localScale = Vector3.one / scale;
            TextMeshPro t = bub.AddComponent<TextMeshPro>();
            UIKit.EnsureStyle();
            if (UIKit.Font != null) t.font = UIKit.Font;
            Material m = Notice.WorldMaterial(t.font);
            if (m != null) t.fontSharedMaterial = m;
            t.text = Loc.T("devown.medal." + ((medalLine++) % 4));
            t.fontSize = 3.2f * OwnTextScale;       // 다른 월드 글자와 같은 배율 (BossSkills.OwnKit)
            t.color = Color.white;
            t.outlineColor = new Color32(240, 120, 170, 255);
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = false;
            t.sortingLayerID = SortingLayer.NameToID("Effect");
            t.sortingOrder = 45;
            float now = Time.time;
            medals.Add(new OwnMedal
            {
                part = part, sr = part.GetComponent<SpriteRenderer>(), lastPos = at,
                chaseEnd = now + MedalChase, hardEnd = now + MedalChase + 0.75f + 1f,
            });
            Fx.Spawn("fx_sparkle", at, 2.4f, new Color(1f, 0.85f, 0.3f), 16f);
        }
        Hostile.Play("sparkle", 0.6f, 1.1f);
        Hostile.Play("chime", 0.5f, 1.2f);
        yield return OwnWait(Time.time + 0.6f, e);
    }

    int medalLine;

    bool MedalHit(bool inside)
    {
        PlayerController pl = Hostile.Player;
        if (!inside || pl == null || pl.IsDying || medalHits >= 2) return false;
        AttackLabel.Pending = OwnLabel(OwnAtk.Medal);
        bool hit = pl.TryHit(OwnDmg(14f, 0.12f));
        AttackLabel.Pending = null;
        if (hit) medalHits++;
        return hit;
    }

    void KillMedal(OwnMedal m, bool spark)
    {
        if (m.tel != null) m.tel.EndEarly(m.telId);
        m.tel = null;
        if (m.part != null)
        {
            if (spark) Fx.Spawn("fx_spark", m.part.transform.position, 1.6f, new Color(1f, 0.85f, 0.3f), 22f);
            Destroy(m.part.gameObject);
        }
        m.part = null;
    }

    // 메달을 모두 지움 (CancelOwn · 피해 없이 작은 불꽃)
    void ClearMedals()
    {
        for (int i = 0; i < medals.Count; i++) KillMedal(medals[i], true);
        medals.Clear();
    }

    // 매 프레임 (OwnGuard 안 · try/catch): 쫓기 · 시간 · 폭발 · 깨짐 · 「불 끄고 자!」 동안 멈춤 (멈춤은 끝 시각 · 메달마다 최대 +6초)
    void TickMedals()
    {
        float now = Time.time, dt = Time.deltaTime;
        bool frozen = now < medalFreezeUntil;
        PlayerController pl = Hostile.Player;
        for (int i = medals.Count - 1; i >= 0; i--)
        {
            OwnMedal m = medals[i];
            if (m.part == null || m.part.Broken)
            {
                // 플레이어가 깨뜨림 (BossPart.Break 가 지움)
                OwnFloat(m.lastPos, Loc.T("비교 거부!"), new Color(1f, 0.85f, 0.3f), 5f);
                if (m.tel != null) m.tel.EndEarly(m.telId);
                medals.RemoveAt(i);
                continue;
            }
            Transform t = m.part.transform;
            m.lastPos = t.position;
            if (m.sr != null) m.sr.sprite = FxFrame("fx_medal", Mathf.Repeat(now * 3f, 1f) < 0.5f ? 0 : 1);
            if (frozen)
            {
                float add = Mathf.Min(dt, 6f - m.frozenAdd);
                if (add > 0f)
                {
                    m.frozenAdd += add;
                    m.chaseEnd += add;
                    m.hardEnd += add;
                    if (m.exploding) m.blastAt += add;
                }
                continue;
            }
            if (now >= m.hardEnd) { KillMedal(m, true); medals.RemoveAt(i); continue; }
            if (!m.exploding)
            {
                if (now < m.chaseEnd)
                {
                    if (pl == null) continue;
                    Vector3 to = pl.transform.position - t.position;
                    t.position = Vector3.MoveTowards(t.position, pl.transform.position, MedalSpeed * EnermyController.GlobalSpeedMultiplier * dt);
                    if (to.magnitude < 1.0f + 0.2f)
                    {
                        // 닿으면 한 대 + 깨짐 (글자 없음)
                        if (MedalHit(true)) Hostile.Play("mom_slap", 0.7f, 1.2f);
                        KillMedal(m, true);
                        medals.RemoveAt(i);
                    }
                    continue;
                }
                m.exploding = true;
                float warn = OwnTele(0.75f);
                m.blastAt = now + warn;
                m.tel = Hostile.Circle(t.position, MedalBlastR, warn, new Color(1f, 0.85f, 0.3f, 0.95f));
                m.telId = m.tel != null ? m.tel.UseId : 0;
                continue;
            }
            // 멈춤이 풀린 뒤 경고가 먼저 끝났으면 남은 시간만큼 다시
            if ((m.tel == null || !m.tel.gameObject.activeSelf || m.tel.UseId != m.telId) && m.blastAt - now > 0.1f)
            {
                m.tel = Hostile.Circle(t.position, MedalBlastR, m.blastAt - now, new Color(1f, 0.85f, 0.3f, 0.95f));
                m.telId = m.tel != null ? m.tel.UseId : 0;
            }
            if (now >= m.blastAt)
            {
                Fx.Spawn("fx_explosion", t.position, MedalBlastR * 2.2f, new Color(1f, 0.85f, 0.3f), 18f);
                Hostile.Play("boom", 0.5f, 1.3f);
                MedalHit(PlayerIn(t.position, MedalBlastR));
                m.tel = null;
                KillMedal(m, false);
                medals.RemoveAt(i);
            }
        }
    }

    // ================================================================= D6. 불 끄고 자!
    IEnumerator LightsOut(int e)
    {
        bool first = !ownLightsSeen;
        ownLightsSeen = true;
        float maxLen = 7f;
        heroHoldUntil = Mathf.Max(heroHoldUntil, Time.time + maxLen + 0.3f);       // 끝까지 새 영웅 필살기 없음 (끝 시각으로만)
        medalFreezeUntil = Mathf.Max(medalFreezeUntil, Time.time + maxLen + 0.3f); // 메달도 멈춤 (끝 시각으로만)
        TextMeshPro shout = OwnText(transform.position + Vector3.up * (OwnHeadY + 2.4f), Loc.T("불 끄고 자!"), MomPink, 6f, 48);
        float shoutUntil = Time.time + 1.2f;
        Hostile.Play("mom_door", 0.8f, 1f);
        Hostile.Play("bell", 0.3f, 0.5f);
        bool caught = false;
        for (int round = 0; round < 2 && !caught; round++)
        {
            if (!OwnOk(e)) yield break;
            if (round == 1)
            {
                if (OwnWinding) break;
                yield return OwnWait(Time.time + 0.5f, e);
                if (!OwnOk(e)) yield break;
                OwnFloat(transform.position + Vector3.up * 1.5f, Loc.T("…진짜 자?"), MomWhite, 5f);
                Hostile.Play("mom_door", 0.6f, 1.1f);
            }
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 a = transform.position;
            Vector2 d = ((Vector2)(pl.transform.position - a)).normalized;
            if (d.sqrMagnitude < 0.01f) d = Vector2.right;
            Vector3 b = pl.transform.position + (Vector3)(d * 14f);
            float warn = OwnTele(round == 0 ? (first ? 1.4f : 1.0f) : 0.8f);
            float check = round == 0 ? 0.9f : 0.8f;
            OwnLineWarn(a, b, 9f, warn, HomeLight);
            TextMeshPro hush = OwnText(pl.transform.position + Vector3.up * 3.2f, Loc.T("쉿… 자는 척!"), MomWhite, 4f, 47);
            TextMeshPro zzz = OwnText(pl.transform.position + Vector3.up * 4.4f, "Z z z", new Color(0.75f, 0.85f, 1f), 3.6f, 47);
            float until = Time.time + warn;
            while (Time.time < until)
            {
                if (!OwnOk(e)) yield break;
                TickAura();
                if (shout != null && Time.time >= shoutUntil) { HideObj(shout); shout = null; }
                PlayerController p = Hostile.Player;
                if (p != null)
                {
                    if (hush != null) hush.transform.position = p.transform.position + Vector3.up * 3.2f;
                    if (zzz != null) zzz.transform.position = p.transform.position + Vector3.up * (4.4f + 0.2f * Mathf.Sin(Time.time * 4f));
                }
                yield return null;
            }
            HideObj(hush); HideObj(zzz);
            // 확인: 빛 띠가 밝아짐 · 엄마 눈 · 플레이어 머리 위 「…」
            LineRenderer band = OwnSolid(new Color(1f, 0.95f, 0.8f, 0.28f), 9f, 6);
            SetSeg(band, a, b);
            SpriteRenderer eye = OwnIcon(FxFrame("fx_momeye", 0), transform.position + Vector3.up * (OwnHeadY + 1.2f), 1.6f, Color.white, 34);
            TextMeshPro dots = OwnText(pl.transform.position + Vector3.up * 3.2f, "…", Color.white, 5f, 47);
            PlayerController pl0 = Hostile.Player;
            Vector3 lastPos = pl0 != null ? pl0.transform.position : Vector3.zero;     // 확인 시작 프레임에 지금 자리로 (첫 프레임 오판 방지)
            int fastFrames = 0;
            float start = Time.time, end = Time.time + check;
            while (Time.time < end)
            {
                if (!OwnOk(e)) yield break;
                TickAura();
                if (shout != null && Time.time >= shoutUntil) { HideObj(shout); shout = null; }
                PlayerController p = Hostile.Player;
                if (p == null || p.IsDying) break;
                Vector3 now = p.transform.position;
                if (dots != null) dots.transform.position = now + Vector3.up * 3.2f;
                if (band != null) band.startColor = band.endColor = Hostile.Accessible(new Color(1f, 0.95f, 0.8f, 0.2f + 0.12f * Mathf.Sin(Time.time * 9f)));
                if (eye != null) eye.sprite = FxFrame("fx_momeye", Mathf.Repeat(Time.time * 1.5f, 1f) < 0.8f ? 0 : 1);
                if (Time.deltaTime > 0f)
                {
                    // 움직임 = 이동 입력(의도) 또는 위치 속도 6칸/초가 2프레임 연속 (잡몹에 밀림 · 끼임 풀기 · 프레임 튐은 봐줌)
                    float spd = (now - lastPos).magnitude / Time.deltaTime;
                    fastFrames = spd > 6f ? fastFrames + 1 : 0;
                    lastPos = now;
                    bool moving = p.MoveInput.magnitude > 0.25f || fastFrames >= 2;
                    if (Time.time - start >= 0.12f && moving && Hostile.DistanceToSegment(now, a, b) < 4.5f)
                    {
                        OwnHit(true, 18f, 0.18f, OwnAtk.LightsOut);
                        OwnFloat(now, Loc.T("안 자고 뭐 해!"), MomPink, 6f);
                        Hostile.Play("mom_slap", 1f, 0.9f);
                        Hostile.Shake(0.2f);
                        caught = true;          // 들켰으면 다시 확인은 없음 (엄마가 문을 닫고 끝)
                        break;
                    }
                }
                yield return null;
            }
            HideObj(band); HideObj(eye); HideObj(dots);
            if (!caught && OwnOk(e))
            {
                PlayerController p = Hostile.Player;
                if (p != null) OwnFloat(p.transform.position, Loc.T("…자는구나."), new Color(0.95f, 0.95f, 1f), 5f);
                Hostile.Play("chime", 0.25f, 0.8f);
            }
        }
        HideObj(shout);
        yield return OwnWait(Time.time + 0.3f, e);
    }
}
