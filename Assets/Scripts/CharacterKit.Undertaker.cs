using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 장의사 (2.0.0): 석관을 묻은 자 — docs/plans/2026-11-02-big-update/hero-undertaker.md
//   평타 · 파기: 빈 땅을 누르면 커서 자리에 구멍이 고정되어 열리고(0.7칸), 누르는 동안 넓어져(최대 2.0칸) 떼면 그 크기로 완성. 구멍에 걸어 들어온 적은 빠져 잠깐 갇힘 (EnemyHold)
//   평타 · 매장: 판 구멍 위를 누르면 모든 구멍을 한꺼번에 덮음 — 갇힌 적 220% · 쓰러진 자리에 묘비(Wall 상자 · 장의사와 자기 탄은 통과)
//   필살기 · 장례: CharacterKit.Undertaker.Ult
//   묘비 상한 · 수명: 영구 8개(수명 20초) · 장례 임시 12개(4초) — 넘으면 가장 오래된 것부터 무너짐 / 구멍 3개(수명 6초) — 넘으면 가장 오래된 구멍이 메워짐
//   상태는 「끝나는 시각」 값만 (코루틴 켜고 끄는 깃발 없음) · 구멍 · 묘비는 저장하지 않음 (이어하면 빈 판)
// 레벨업 카드 (card): 0 깊은 구덩이 · 1 여러 삽 · 2 무너지는 흙 · 3 종지기 · 5 비문 · 6 되파기 / 형태: 묘지 까마귀 · 상여 종 · 석관 / 강화: 넓은 삽날 · 두 번 파기 · 무거운 흙
// 간소화: 갈래 5개(생매장 · 묘역 설계 · 땅굴 · 장례식 · 도굴꾼) · 사막 비밀은 범위 밖 — 판정 · 손맛만 먼저 (구멍 위 「×3」 글자 · 삽 파는 손 각도 · 성문 앞 12칸 금지는 생략)

// 묘비 · 흙담 · 적의 묘비 (거울의 군주 · 개발자)가 모두 쓰는 표식 — Bullet 이 「플레이어 탄이 이 표식의 Wall 을 지나감」을 읽음
public class UndertakerGrave : MonoBehaviour
{
    public bool hostile;                    // 적(거울의 군주 · 개발자)의 묘비 — 플레이어도 막힘
    public bool big, temp, stone, isWall;   // stone = 석관(내구 두 배)
    public int hp;
    public float until, chipAt;
    public BoxCollider2D box;
    public SpriteRenderer sr;
    Vector2 pos0;

    public Vector2 Pos => pos0;

    // 그림 한 장 (없으면 null — 예외 없이)
    public static Sprite SpriteOf(string name)
    {
        Sprite[] f = Fx.Frames(name);
        return f != null && f.Length > 0 ? f[0] : null;
    }

    // 묘비 하나 만들기 (Wall 태그 상자 · 트리거 아님 · Rigidbody 없음 — 무한 지형 묘비 TerrainObstacle 과 같은 방식)
    public static UndertakerGrave Make(Vector2 pos, bool big, bool temp, bool hostile, float life, int hp, SpriteRenderer layerFrom = null)
    {
        GameObject go = new GameObject(temp ? "UtGraveTemp" : "UtGrave");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.tag = "Wall";
        UndertakerGrave g = go.AddComponent<UndertakerGrave>();
        g.pos0 = pos; g.hostile = hostile; g.big = big; g.temp = temp; g.hp = hp;
        g.until = Time.time + life;
        Vector2 size = big ? new Vector2(1.5f, 0.9f) : new Vector2(0.9f, 0.6f);
        g.box = go.AddComponent<BoxCollider2D>();
        g.box.size = size;
        g.box.offset = new Vector2(0f, size.y * 0.5f + 0.1f);
        // 그림은 자식에 (상자가 그림 배율을 따라 줄어들지 않게)
        GameObject sg = new GameObject("Sprite");
        sg.transform.SetParent(go.transform, false);
        g.sr = sg.AddComponent<SpriteRenderer>();
        Sprite s = SpriteOf(temp ? "prop_grave_temp" : big ? "prop_grave_big" : "prop_grave");
        if (s != null)
        {
            g.sr.sprite = s;
            float h = big ? 2.1f : 1.5f;
            sg.transform.localScale = Vector3.one * (s.bounds.size.y > 0.01f ? h / s.bounds.size.y : 1f);
        }
        else if (Hostile.Glow != null)
        {
            g.sr.sprite = Hostile.Glow;
            g.sr.color = new Color(0.6f, 0.65f, 0.72f, 0.8f);
            sg.transform.localScale = Vector3.one * (big ? 1.2f : 0.8f);
        }
        if (layerFrom != null) { g.sr.sortingLayerID = layerFrom.sortingLayerID; g.sr.sortingOrder = layerFrom.sortingOrder - 1; }
        return g;
    }

    // 두 묘비 사이 흙담 (상자 하나를 회전 · 늘린 타일 그림)
    public static UndertakerGrave MakeWall(Vector2 a, Vector2 b, SpriteRenderer layerFrom)
    {
        Vector2 d = b - a;
        float len = d.magnitude;
        GameObject go = new GameObject("UtEarthWall");
        go.transform.position = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        go.tag = "Wall";
        UndertakerGrave g = go.AddComponent<UndertakerGrave>();
        g.isWall = true;
        g.pos0 = (a + b) * 0.5f;
        g.until = float.MaxValue;
        g.box = go.AddComponent<BoxCollider2D>();
        g.box.size = new Vector2(Mathf.Max(0.2f, len), 0.3f);
        GameObject sg = new GameObject("Sprite");
        sg.transform.SetParent(go.transform, false);
        g.sr = sg.AddComponent<SpriteRenderer>();
        Sprite s = SpriteOf("fx_earthwall");
        if (s != null)
        {
            g.sr.sprite = s;
            sg.transform.localScale = new Vector3(Mathf.Max(0.2f, len) / Mathf.Max(0.1f, s.bounds.size.x), 0.7f / Mathf.Max(0.1f, s.bounds.size.y), 1f);
            g.sr.sortingLayerID = layerFrom != null ? layerFrom.sortingLayerID : 0;
            g.sr.sortingOrder = layerFrom != null ? layerFrom.sortingOrder - 1 : 0;
        }
        return g;
    }

    // 적(거울의 군주 · 개발자)의 묘비는 끝나면 스스로 무너짐
    void Update()
    {
        if (hostile && Time.time >= until) Break();
    }

    public void Break()
    {
        Vector3 p = transform.position;
        Fx.Spawn("fx_grave_break", p, 2f, Color.white, 14f);
        Hostile.Play("shatter", 0.3f, 1.3f);
        Destroy(gameObject);
    }
}

// 손잡이 한 곳 (hero-undertaker.md 9-2 수치 표 · 카드 · 번역 숫자와 같게)
public static class UndertakerTuning
{
    public const float HoleMin = 0.7f, HoleMax = 2f, HoleLife = 6f, TrapSec = 1.4f, RefallBlock = 0.8f;
    public const float DigSplash = 0.4f, FallHit = 0.6f, BuryTrapped = 2.2f, BuryLoose = 1.1f, BossBuryMul = 1.1f, BossSizeMin = 0.6f;
    public const float BuryHitR = 0.9f, BuryDelay = 0.15f, GraveLife = 20f, TempGraveLife = 4f, GraveMinFromPlayer = 1f, BossBreakRadius = 2.6f;
    public const int HoleMaxCount = 3, TrapTotalCap = 40, GraveHp = 6, BigGraveHp = 10, GraveCap = 8, BigGraveAt = 3, TempGraveCap = 12;
    public const float FuneralHold = 0.5f, FuneralFall = 1f, FuneralBury = 2.2f, FuneralBoss = 3.2f, FuneralDelay = 1f;
    public const int FuneralCap = 60;
}

public partial class CharacterKit
{
    static readonly Color UtSilver = new Color(0.725f, 0.796f, 0.894f);        // 창백한 청회색 은 #B9CBE4 (영웅색)
    static readonly Color UtSoil = new Color(0.72f, 0.55f, 0.36f);

    // ---------------- 갇힌 적 · 구멍 · 묘비
    sealed class UtTrap { public EnermyController e; public float until, ang, dist, peckAt, crushAt; }

    sealed class UtHole
    {
        public Vector2 pos;
        public float r, rMax, until, checkAt, bornAt;
        public bool growing, burying, pitfall;
        public int pitCap = 1;
        public Vector2 trenchEnd;               // 도랑 끝 (고유 스킬 도랑 파기 · 없으면 pos)
        public float trenchLen;
        public GameObject go;
        public SpriteRenderer floor, rim, trench, skinLight;      // skinLight: 2026-11-04~ 스킨 구멍 빛 (설명 약속)
        public readonly List<UtTrap> traps = new List<UtTrap>(8);
        public readonly Dictionary<int, float> block = new Dictionary<int, float>();
    }

    readonly List<UtHole> utHoles = new List<UtHole>(8);
    readonly List<UtHole> utBurying = new List<UtHole>(8);      // 2.0.0~ 검토: 매장 중(utHoles 에서는 빠짐)인 구멍 — 매장 중 교대하면 그림이 남지 않게 정리 대상에 둠
    readonly List<UndertakerGrave> utGraves = new List<UndertakerGrave>(24);
    readonly List<UndertakerGrave> utWalls = new List<UndertakerGrave>(8);
    UtHole utCur;
    bool utDigging, utLatch, utGraveDirty;
    float utRestUntil, utGraveTickAt, utWallAt, utDoubleAt;
    Vector2 utCursor;
    LineRenderer utRing, utRingMax;
    readonly List<LineRenderer> utHoleRings = new List<LineRenderer>(8);
    static readonly List<EnermyController> utTmp = new List<EnermyController>(32);
    static Collider2D[] utPointBuf = new Collider2D[8];
    static ContactFilter2D utFilter;

    // ---------------- 밖에서 읽는 값
    public bool Digs => Id == CharacterId.Undertaker;
    public bool DigActive => utDigging;
    public int UtHoleCount { get { int n = 0; foreach (UtHole h in utHoles) if (!h.pitfall) n++; return n; } }
    public int UtHoleMax => UndertakerTuning.HoleMaxCount + card[1] + UtPathHoles;
    public int UtGraveCount { get { int n = 0; foreach (UndertakerGrave g in utGraves) if (g != null && !g.temp) n++; return n; } }
    public int UtGraveMax => UndertakerTuning.GraveCap + UtPathGraveCap;
    public int UtTrappedCount { get { int n = 0; foreach (UtHole h in utHoles) n += h.traps.Count; return n; } }
    public bool UtDigging => utDigging;
    public Vector2 UtCursorPos => utCursor;
    public IReadOnlyList<UndertakerGrave> UtGraveList => utGraves;
    float UtTrapSec => UndertakerTuning.TrapSec + 0.4f * card[0] + UtPathTrap;
    float UtHoleMaxR => UndertakerTuning.HoleMax * (augment == AugWideBlade ? 1.3f : 1f);
    float UtGrowMul => (Special != null && Special.Has(SpecialAbilities.KitVigil) && player != null && player.PlayerHealth < player.PlayerMaxHealth * 0.3f
                           ? (Special.IsEvolved(SpecialAbilities.KitVigil) ? 1.8f : 1.5f) : 1f) * UtPathGrow;
    float UtBuryMul => (augment == AugHeavySoil ? 1.25f : 1f) * formPower * UtPathBuryMul;
    // 갈래 자리 값 (UtPathHoles · UtPathGraveCap · UtPathTrap · UtPathGrow · UtPathLife)은 CharacterKit.Undertaker.Paths

    static bool UtEvo(string key) => SignatureSkills.UtEvo(key);

    // ================================================================= 입력 (PlayerController 가 매 프레임 부름)
    //   interval: 평타 한 번의 쉼 (ShootSpeed / 공속) — 가득 넓히는 데는 두 배 (궁수 시위와 같은 눈금)
    public void UpdateDig(bool canAct, float interval)
    {
        if (player == null) return;
        if (player.IsDying) { if (utDigging) UtFinishDig(interval, false); return; }
        if (Time.timeScale == 0f) return;       // 창이 열림: 놓은 것으로 보지 않고 그대로 (창이 닫힌 뒤 버튼이 떨어져 있으면 그때 완성)
        bool held = GameInput.FireHeld && canAct;
        if (!held || GameInput.AttackLockNow) utLatch = false;          // 매장 뒤에는 한 번 떼야 다음 파기 (공격 고정은 쉼이 끝날 때마다 새로 누른 것으로 봄)
        if (utDigging)
        {
            UtHole h = utCur;
            if (h == null || h.burying || !utHoles.Contains(h)) { utDigging = false; utCur = null; return; }      // 가장 오래된 구멍이라 메워졌음
            if (!held) { UtFinishDig(interval, false); return; }
            float full = Mathf.Max(0.3f, interval * 2f) / UtGrowMul;
            h.r = Mathf.Min(h.rMax, h.r + (h.rMax - UndertakerTuning.HoleMin) * Time.deltaTime / full);
            player.FaceTowards(new Vector3(h.pos.x, h.pos.y, 0f));
            if (h.r >= h.rMax - 0.001f) UtFinishDig(interval, true);
            return;
        }
        if (!held || utLatch || Time.time < utRestUntil) return;
        Vector2 c = UtCursor(transform.position);
        player.FaceTowards(new Vector3(c.x, c.y, 0f));
        PlayerController.RaiseAttackFired(transform.position, (c - (Vector2)transform.position).normalized);
        PlayerLook.Fired(-1);
        if (UtHoleAt(c) != null) { UtBury(interval); return; }
        UtStartDig(c);
    }

    // 커서 (사거리 10 으로 자름 · 패드는 막대를 놓고 있으면 바라보는 쪽 3칸)
    Vector2 UtCursor(Vector3 me)
    {
        Vector3 m = ClampRange(me, Mouse, def.range);
        if (GamePad.Active && !GameInput.Auto && ((Vector2)(m - me)).sqrMagnitude < 2.25f)
            m = me + Vector3.right * (body != null && body.flipX ? -3f : 3f);
        return new Vector2(m.x, m.y);
    }

    // 커서가 판 구멍 안인가 (작은 구멍은 0.9칸으로 봄 — 반쯤 판 구멍도 다시 찍기 쉽게)
    UtHole UtHoleAt(Vector2 c)
    {
        UtHole best = null;
        float bd = float.MaxValue;
        foreach (UtHole h in utHoles)
        {
            if (h.burying || h.pitfall) continue;
            float d = Vector2.Distance(c, h.pos);
            if (d <= Mathf.Max(h.r, UndertakerTuning.BuryHitR) && d < bd) { bd = d; best = h; }
        }
        return best;
    }

    // ================================================================= 파기
    void UtStartDig(Vector2 c)
    {
        c = UtFreeSpot(Hostile.ClampArena(new Vector3(c.x, c.y, 0f)));
        // 되파기 (카드): 묘비 위에 구멍을 파면 묘비가 무너지며 둘레 2칸 적에게 피해
        bool robbed = false;
        if (card[6] > 0 || UtpOn(UndertakerPaths.Robber))
        {
            UndertakerGrave g = UtGraveAt(c, 1f);
            if (g != null && !g.isWall) { UtRedig(g); robbed = true; }
        }
        // 겹치지 않게 다른 구멍 곁으로 밀어 냄
        foreach (UtHole o in utHoles)
        {
            if (o.pitfall) continue;
            Vector2 d = c - o.pos;
            float need = o.r + UndertakerTuning.HoleMin;
            if (d.sqrMagnitude < need * need) c = o.pos + (d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right) * need;
        }
        c = (Vector2)Hostile.ClampArena(new Vector3(c.x, c.y, 0f));
        UtHole h = UtNewHole(c, UndertakerTuning.HoleMin, UtHoleMaxR);
        h.growing = true;
        if (robbed && UtpOn(UndertakerPaths.Robber)) h.r = h.rMax;       // 도굴꾼: 묘비 위에 판 구멍은 곧바로 가장 크게
        UtEnforceHoleCap(h);
        utCur = h;
        utDigging = true;
        // 흙 튀김: 그 자리 적(보스 포함)에게 공격력 40% (밀침 없음)
        UtDamageCircle(c, UndertakerTuning.HoleMin + 0.2f, Damage * UndertakerTuning.DigSplash * formPower, 0f, DamageSource.Basic, false);
        Fx.Spawn("fx_dirt", new Vector3(c.x, c.y, 0f), 1.8f, Color.white, 16f, 0f, 14);
        Play("thump", 0.35f, Random.Range(1.0f, 1.2f));
        Play("whoosh", 0.2f, 0.6f);
        SignatureSkills.UtDugStart(c, h.rMax);
        RunCounters.Both("dig");
    }

    void UtFinishDig(float interval, bool full)
    {
        UtHole h = utCur;
        utDigging = false;
        utCur = null;
        float rest = interval;
        if (SignatureSkills.UtNoRest()) rest = 0f;       // 장례식 공명 I: 장례 뒤 6초 쉼 없음
        utRestUntil = Time.time + rest;
        if (h == null) return;
        h.growing = false;
        SignatureSkills.UtDugDone(h.pos, h.r, h.rMax);
        UtSetTrench(h);
        // 두 번 파기 (강화): 완성 0.4초 뒤 커서 쪽 3칸 앞에 절반 크기 구멍이 하나 더
        if (augment == AugDoubleDig) { utDoubleAt = Time.time + 0.4f; StartCoroutine(UtDoubleDig(h)); }
    }

    IEnumerator UtDoubleDig(UtHole from)
    {
        yield return new WaitForSeconds(0.4f);
        if (player == null || player.IsDying || from == null || !utHoles.Contains(from)) yield break;
        Vector2 dir = (from.pos - (Vector2)transform.position);
        dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.right;
        Vector2 c = (Vector2)Hostile.ClampArena(new Vector3(from.pos.x + dir.x * 3f, from.pos.y + dir.y * 3f, 0f));
        c = UtFreeSpot(c);
        float r = Mathf.Max(UndertakerTuning.HoleMin, from.r * 0.5f);
        UtHole h = UtNewHole(c, r, r);
        UtEnforceHoleCap(h);
        Fx.Spawn("fx_dirt", new Vector3(c.x, c.y, 0f), 1.5f, Color.white, 16f, 0f, 14);
        Play("thump", 0.25f, 1.3f);
    }

    // 도랑 (고유 스킬 도랑 파기): 구멍에서 커서 쪽으로 이어짐 — 구멍과 함께 메워짐
    void UtSetTrench(UtHole h)
    {
        float len = SignatureSkills.UtTrenchLen();
        if (len <= 0.01f || h == null) return;
        Vector2 dir = (utCursor - h.pos);
        if (dir.sqrMagnitude < 0.01f) dir = h.pos - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        dir.Normalize();
        h.trenchLen = len;
        h.trenchEnd = h.pos + dir * (h.r + len);
        GameObject t = new GameObject("UtTrench");
        t.transform.SetParent(h.go.transform, false);
        h.trench = t.AddComponent<SpriteRenderer>();
        Sprite s = UndertakerGrave.SpriteOf("fx_trench");
        if (s != null)
        {
            h.trench.sprite = s;
            Vector2 a = h.pos, b = h.trenchEnd;
            t.transform.position = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
            t.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            t.transform.localScale = new Vector3(Vector2.Distance(a, b) / Mathf.Max(0.1f, s.bounds.size.x), 0.8f / Mathf.Max(0.1f, s.bounds.size.y), 1f);
            h.trench.sortingLayerID = SortingLayer.NameToID("Background");
            h.trench.sortingOrder = 8;
        }
        SignatureSkills.UtTrenchMade(h.pos, h.trenchEnd);
    }

    // ================================================================= 구멍 만들기 · 치우기
    UtHole UtNewHole(Vector2 pos, float r, float rMax, bool pitfall = false)
    {
        UtHole h = new UtHole { pos = pos, r = r, rMax = rMax, bornAt = Time.time, pitfall = pitfall, trenchEnd = pos };
        h.until = Time.time + (pitfall ? 10f : UndertakerTuning.HoleLife + UtPathLife);
        h.checkAt = Time.time + 0.05f;
        h.go = new GameObject(pitfall ? "UtPit" : "UtHole");
        h.go.transform.position = new Vector3(pos.x, pos.y, 0f);
        h.floor = h.go.AddComponent<SpriteRenderer>();
        Sprite fs = UndertakerGrave.SpriteOf(pitfall ? "fx_pitfall" : "fx_hole");
        if (fs != null) h.floor.sprite = fs;
        else if (Hostile.Glow != null) { h.floor.sprite = Hostile.Glow; h.floor.color = new Color(0.12f, 0.08f, 0.05f, 0.9f); }
        h.floor.sortingLayerID = SortingLayer.NameToID("Background");
        h.floor.sortingOrder = 9;
        GameObject rg = new GameObject("Rim");
        rg.transform.SetParent(h.go.transform, false);
        h.rim = rg.AddComponent<SpriteRenderer>();
        h.rim.sprite = UndertakerGrave.SpriteOf("fx_hole_rim");
        h.rim.enabled = false;
        // 2026-11-04~ 캐릭터 · 무기 스킨 설명 약속 (묘지기 「구멍 가장자리에 랜턴 불빛」): 구멍 테 안쪽에 빛 — 테 · 크기 · 판정은 그대로
        string holeLight = SkinFx.ObjectLook("hole");
        if (holeLight != null && !pitfall && Hostile.Glow != null && ColorUtility.TryParseHtmlString(holeLight, out Color lc))
        {
            GameObject lg = new GameObject("SkinLight");
            lg.transform.SetParent(h.go.transform, false);
            SpriteRenderer lsr = lg.AddComponent<SpriteRenderer>();
            lsr.sprite = Hostile.Glow;
            lsr.color = new Color(lc.r, lc.g, lc.b, 0.55f * GameSettings.MyFxAlpha);
            lsr.sortingLayerID = h.floor.sortingLayerID;
            lsr.sortingOrder = h.floor.sortingOrder + 1;
            float fw = h.floor.sprite != null ? h.floor.sprite.bounds.size.x : 1f;
            lg.transform.localScale = Vector3.one * (fw * 0.7f / Mathf.Max(0.01f, Hostile.Glow.bounds.size.x));
            h.skinLight = lsr;
        }
        utHoles.Add(h);
        UtDrawHole(h);
        return h;
    }

    void UtEnforceHoleCap(UtHole keep)
    {
        while (UtHoleCount > UtHoleMax)
        {
            UtHole oldest = null;
            foreach (UtHole h in utHoles) if (!h.pitfall && h != keep && (oldest == null || h.bornAt < oldest.bornAt)) oldest = h;
            if (oldest == null) break;
            UtRemoveHole(oldest);
        }
    }

    void UtRemoveHole(UtHole h)
    {
        if (h == null) return;
        for (int i = h.traps.Count - 1; i >= 0; i--) UtRelease(h, h.traps[i], true);
        utHoles.Remove(h);
        if (h == utCur) { utCur = null; utDigging = false; }
        if (h.go != null) Destroy(h.go);
    }

    // 벽 · 경계가 아닌 자리 (우리 묘비는 벽으로 치지 않음)
    bool UtRealWall(Vector2 p)
    {
        if (utFilter.useTriggers) utFilter.useTriggers = false;
        int n = Physics2D.OverlapPoint(p, utFilter, utPointBuf);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = utPointBuf[i];
            if (c != null && !c.isTrigger && c.CompareTag("Wall") && !c.TryGetComponent(out UndertakerGrave _)) return true;
        }
        return false;
    }

    Vector2 UtFreeSpot(Vector2 p)
    {
        if (!UtRealWall(p)) return p;
        for (float r = 0.5f; r <= 4f; r += 0.5f)
            for (int k = 0; k < 12; k++)
            {
                float a = k * 30f * Mathf.Deg2Rad;
                Vector2 q = (Vector2)Hostile.ClampArena(new Vector3(p.x + Mathf.Cos(a) * r, p.y + Mathf.Sin(a) * r, 0f));
                if (!UtRealWall(q)) return q;
            }
        return p;
    }

    void UtDrawHole(UtHole h)
    {
        if (h.go == null) return;
        Sprite s = h.floor.sprite;
        float w = s != null ? s.bounds.size.x : 1f;
        float scale = w > 0.01f ? (h.pitfall ? 1.8f : 2f * (h.r + UtPathHoleR(h)) * 1.06f) / w : 1f;
        h.floor.transform.localScale = new Vector3(scale, scale, 1f);
        if (h.rim != null) h.rim.transform.localScale = new Vector3(scale, scale, 1f);
        float left = h.until - Time.time;
        float a = h.growing || left > 1f ? 1f : Mathf.Clamp01(left);
        Color c = h.floor.color;
        c.a = a;
        h.floor.color = c;
        if (h.skinLight != null) { Color l = h.skinLight.color; l.a = 0.55f * GameSettings.MyFxAlpha * a; h.skinLight.color = l; }      // 바닥과 같이 흐려짐
    }

    // ================================================================= 매 프레임 (LateUpdate)
    void UtLate()
    {
        if (player == null) return;
        if (player.IsDying) { UtHideAim(); return; }
        bool paused = Time.timeScale == 0f;
        float now = Time.time;
        if (!paused)
        {
            for (int i = utHoles.Count - 1; i >= 0; i--)
            {
                UtHole h = utHoles[i];
                if (h == null || h.go == null) { utHoles.RemoveAt(i); continue; }
                UtDrawHole(h);
                UtFollowTraps(h, now);
                if (now >= h.checkAt && !h.burying) { h.checkAt = now + 0.1f; UtScan(h, now); }
                if (!h.growing && !h.burying && now >= h.until) { UtExpireHole(h); }
            }
            if (now >= utGraveTickAt) { utGraveTickAt = now + 0.25f; UtGraveTick(now); }
            if (utGraveDirty && now >= utWallAt) { utWallAt = now + 0.5f; utGraveDirty = false; UtRelink(); }
            UtUltTick();
            UtPathTick(now);
        }
        UtAimTick();
    }

    void UtExpireHole(UtHole h)
    {
        Fx.Spawn("fx_dirt", new Vector3(h.pos.x, h.pos.y, 0f), h.r * 1.4f, new Color(1f, 1f, 1f, 0.6f), 14f, 0f, 13);
        if (h.pitfall) SignatureSkills.UtPitEnded(h.pos);
        UtRemoveHole(h);
    }

    // 갇힌 적을 구멍 안 제 자리에 (매 프레임 — 지도 접기 MapLoopPull 과 같은 방식) · 구멍 앞 가장자리는 적보다 한 층 위에
    void UtFollowTraps(UtHole h, float now)
    {
        SpriteRenderer top = null;
        for (int i = h.traps.Count - 1; i >= 0; i--)
        {
            UtTrap t = h.traps[i];
            if (t.e == null || t.e.IsDead || t.e.holder != (object)this) { h.traps.RemoveAt(i); continue; }
            if (now >= t.until) { UtRelease(h, t, true); continue; }
            float br = UtBodyR(t.e);
            float rr = Mathf.Max(0f, h.r - br) * 0.6f * t.dist;
            Vector2 p = h.pos + new Vector2(Mathf.Cos(t.ang), Mathf.Sin(t.ang)) * rr;
            EnemyHold.Place(t.e, new Vector3(p.x, p.y, 0f));
            if (top == null) top = t.e.GetComponent<SpriteRenderer>();
            if (form == SpecialAbilities.KitCrow && now >= t.peckAt)
            {
                t.peckAt = now + 0.5f;
                using (DamageSource.As(DamageSource.Basic)) Specials.Damage(t.e.gameObject, Damage * 0.3f * formPower, Vector3.zero, 0f);
                Fx.Spawn("fx_crow", t.e.transform.position + Vector3.up * 0.6f, 1.4f, Color.white, 16f, 0f, 16);
            }
            if (UtEvo("ye.mass") && now >= t.crushAt)
            {
                t.crushAt = now + 0.5f;
                using (DamageSource.As(DamageSource.Basic)) Specials.Damage(t.e.gameObject, Damage * 0.25f * formPower, Vector3.zero, 0f);
            }
        }
        if (h.rim != null)
        {
            bool on = top != null && h.rim.sprite != null;
            if (h.rim.enabled != on) h.rim.enabled = on;
            if (on) { h.rim.sortingLayerID = top.sortingLayerID; h.rim.sortingOrder = top.sortingOrder + 1; }
        }
    }

    void UtRelease(UtHole h, UtTrap t, bool crawl)
    {
        h.traps.Remove(t);
        if (t.e == null) return;
        EnemyHold.Release(t.e, this);
        if (!crawl || t.e.IsDead) return;
        float br = UtBodyR(t.e);
        Vector2 dir = (Vector2)t.e.transform.position - h.pos;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Random.insideUnitCircle.normalized;
        Vector2 p = h.pos + dir * (h.r + br * 0.6f + 0.2f);
        EnemyHold.Place(t.e, EnemyHold.FreeSpot(new Vector3(p.x, p.y, 0f)));
        h.block[t.e.GetInstanceID()] = Time.time + UndertakerTuning.RefallBlock;
        if (h.block.Count > 40) h.block.Clear();
    }

    static float UtBodyR(Component c)
    {
        if (c == null) return 0.6f;
        EnemyHitbox hb = c.GetComponent<EnemyHitbox>();
        if (hb != null && hb.BodyWorldRadius > 0.05f) return hb.BodyWorldRadius;
        return c.TryGetComponent(out Collider2D col) ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.y) : 0.6f;
    }

    // 0.1초마다: 구멍 둘레의 적이 빠지는지 · 무너지는 흙(카드)으로 끌려오는지
    void UtScan(UtHole h, float now)
    {
        int cap = h.pitfall ? h.pitCap : 2 + Mathf.FloorToInt(3f * h.r);
        float pull = h.pitfall ? (UtEvo("ye.antlion") ? 2f : 0f) : (card[2] > 0 ? 0.5f + 0.5f * card[2] : 0f);
        using var list = PhysHits.Circle(new Vector3(h.pos.x, h.pos.y, 0f), h.r + 0.4f + pull, true);
        foreach (Collider2D c in list)
        {
            if (c == null || !c.CompareTag("enermy")) continue;
            if (!c.TryGetComponent(out EnermyController e) || e.IsDead || e.holder != null || e.swarm != null) continue;
            float br = UtBodyR(e);
            Vector2 ep = e.transform.position;
            float d = Vector2.Distance(ep, h.pos);
            if (d > h.r + 0.4f)
            {
                // 무너지는 흙: 구멍 가장자리 밖 (1 ~ 2칸)의 적을 구멍 쪽으로 (초당 2칸 · 보스 · 중간 보스 제외)
                if (pull > 0f && !e.IsMidBoss && d <= h.r + pull + 0.4f)
                {
                    Vector3 np = Vector3.MoveTowards(e.transform.position, new Vector3(h.pos.x, h.pos.y, 0f), 0.2f);
                    if (!Hostile.IsWall(np)) e.transform.position = np;
                }
                continue;
            }
            if (d > h.r - br * 0.5f) continue;
            int id = e.GetInstanceID();
            if (h.block.TryGetValue(id, out float until) && now < until) continue;
            if (e.IsMidBoss || br > h.r * 0.8f)
            {
                e.Slow(0.5f, 1f);           // 발이 빠짐
                h.block[id] = now + 1f;
                continue;
            }
            if (h.traps.Count >= cap || UtTrappedCount >= UndertakerTuning.TrapTotalCap) break;
            if (!EnemyHold.TryTake(e, this)) continue;
            h.traps.Add(new UtTrap { e = e, until = now + UtTrapSec + SignatureSkills.UtTrapExtra(), ang = Random.value * 6.283f, dist = Mathf.Sqrt(Random.value), peckAt = now + 0.3f, crushAt = now + 0.5f });
            using (DamageSource.As(DamageSource.Basic)) Specials.Damage(e.gameObject, Damage * UndertakerTuning.FallHit * formPower, Vector3.zero, 0f);
            Fx.Spawn("fx_dirt", e.transform.position, 1.2f, new Color(1f, 1f, 1f, 0.7f), 16f, 0f, 14);
            RunCounters.Both("fall");
            if (h.pitfall) { h.pitfall = false; h.pitCap = 1; h.until = now + UtTrapSec + 0.5f; SignatureSkills.UtPitSprung(h.pos); UtPitToHole(h); }
        }
        // 도랑 위의 적은 40% 느려짐 (빠지지는 않음)
        if (h.trenchLen > 0.01f)
        {
            using var tl = PhysHits.Circle(new Vector3((h.pos.x + h.trenchEnd.x) * 0.5f, (h.pos.y + h.trenchEnd.y) * 0.5f, 0f), Vector2.Distance(h.pos, h.trenchEnd) * 0.5f + 1f, true);
            foreach (Collider2D c in tl)
            {
                if (c == null || !c.CompareTag("enermy") || !c.TryGetComponent(out EnermyController e) || e.IsDead || e.holder != null) continue;
                if (Hostile.DistanceToSegment(c.transform.position, new Vector3(h.pos.x, h.pos.y, 0f), new Vector3(h.trenchEnd.x, h.trenchEnd.y, 0f)) > 0.8f) continue;
                e.Slow(0.6f, 0.3f);
            }
        }
    }

    // 빈 무덤 함정이 처음 밟혔다 → 보통 구멍 그림으로
    void UtPitToHole(UtHole h)
    {
        Sprite fs = UndertakerGrave.SpriteOf("fx_hole");
        if (fs != null) h.floor.sprite = fs;
        h.r = Mathf.Max(h.r, 0.9f);
        h.rMax = h.r;
    }

    // ================================================================= 매장 (판 구멍 위 평타)
    void UtBury(float interval)
    {
        List<UtHole> list = new List<UtHole>(utHoles.Count);
        foreach (UtHole h in utHoles) if (!h.pitfall && !h.burying) list.Add(h);
        if (list.Count == 0) return;
        foreach (UtHole h in list) { h.burying = true; h.growing = false; utHoles.Remove(h); }
        if (utCur != null && list.Contains(utCur)) { utCur = null; utDigging = false; }
        utLatch = true;
        utRestUntil = Time.time + (SignatureSkills.UtNoRest() ? 0f : interval);
        StartCoroutine(UtBuryRoutine(list, false));
    }

    // fromUlt: 장례의 일제 매장 (피해 × UltMul · 임시 묘비 · 부고 · 명부는 세지 않음)
    IEnumerator UtBuryRoutine(List<UtHole> list, bool fromUlt)
    {
        Play("thump", 0.5f, 0.7f);
        Vector2 aim = utCursor;
        utBurying.AddRange(list);
        foreach (UtHole h in list)
        {
            if (h.go == null) continue;
            Fx.Spawn("fx_bury", new Vector3(h.pos.x, h.pos.y, 0f), h.r * 2.4f, Color.white, 12f, 0f, 16);
            if (h.floor != null) h.floor.enabled = true;
        }
        SignatureSkills.UtBuryStart(list.Count, aim);
        yield return new WaitForSeconds(UndertakerTuning.BuryDelay);
        float trappedMul = fromUlt ? UndertakerTuning.FuneralBury * UltMul : UndertakerTuning.BuryTrapped;
        float looseMul = fromUlt ? UndertakerTuning.FuneralBury * 0.5f * UltMul : UndertakerTuning.BuryLoose;
        int killedAll = 0, trappedKilledAll = 0;
        using (DamageSource.As(fromUlt ? DamageSource.Ult : DamageSource.Basic))
        {
            foreach (UtHole h in list)
            {
                if (h.go == null) continue;
                int killedHere = UtBuryOne(h, trappedMul, looseMul, fromUlt);
                trappedKilledAll += killedHere;
                killedAll += killedHere;
            }
        }
        // 손맛: 한 번의 매장으로 갇힌 적 4마리 이상이 쓰러짐
        if (!fromUlt && trappedKilledAll >= 4)
        {
            Vector2 at = list[0].pos;
            Impact.Hit(ImpactTier.Attack, new Vector3(at.x, at.y, 0f), Vector2.down);
        }
        SignatureSkills.UtBuryEnd(list.Count, trappedKilledAll, fromUlt);
        if (!fromUlt) RunCounters.Both("bury.times");
        foreach (UtHole h in list) { if (h.go != null) Destroy(h.go); utBurying.Remove(h); }
    }

    // 구멍 하나 매장: 갇힌 적 → 구멍 위 적 → 보스 (구멍당 한 번) · 쓰러진 갇힌 적이 있으면 그 자리에 묘비 하나
    int UtBuryOne(UtHole h, float trappedMul, float looseMul, bool fromUlt)
    {
        float baseDmg = Damage * UtBuryMul;
        Vector3 hp3 = new Vector3(h.pos.x, h.pos.y, 0f);
        int killedHere = 0, trappedCount = h.traps.Count;
        utTmp.Clear();
        foreach (UtTrap t in h.traps) if (t.e != null) utTmp.Add(t.e);
        for (int i = h.traps.Count - 1; i >= 0; i--) UtRelease(h, h.traps[i], false);
        float tm = trappedMul * SignatureSkills.UtBuryMul(h.pos, trappedCount, false) * UtPathTrappedMul(fromUlt);
        float extraR = UtPathHoleR(h);        // 생매장 공명 I (갇힌 적을 놓기 전에 셈)
        foreach (EnermyController e in utTmp)
        {
            if (e == null || e.IsDead) continue;
            Specials.Damage(e.gameObject, baseDmg * tm, (e.transform.position - hp3).normalized, 0f);
            if (e == null || e.IsDead) { killedHere++; continue; }
            e.Slow(0f, 1f);         // 반쯤 묻혀 1초 기절
        }
        // 구멍 위의 갇히지 않은 적 · 보스
        float ratio = Mathf.Clamp(h.r / Mathf.Max(0.1f, UndertakerTuning.HoleMax), UndertakerTuning.BossSizeMin, 1f);
        using (var list = PhysHits.Circle(hp3, h.r + extraR, true))
        {
            foreach (Collider2D c in list)
            {
                if (c == null) continue;
                bool boss = c.CompareTag("boss");
                if (!boss && !c.CompareTag("enermy")) continue;
                if (!boss && c.TryGetComponent(out EnermyController ee) && (ee.IsDead || utTmp.Contains(ee))) continue;
                float m = boss ? UndertakerTuning.BossBuryMul * ratio * SignatureSkills.UtBuryMul(h.pos, trappedCount, true) : looseMul;
                if (fromUlt && boss) m = UndertakerTuning.BossBuryMul * ratio;
                else if (boss) m *= UtPathBossMul(trappedCount);
                Specials.Damage(c.gameObject, baseDmg * m, (c.transform.position - hp3).normalized, 0f);
            }
        }
        // 도랑 위의 적도 매장 피해 (진화 지하 묘도)
        if (UtEvo("ye.catacomb") && h.trenchLen > 0.01f)
        {
            Vector3 tb = new Vector3(h.trenchEnd.x, h.trenchEnd.y, 0f);
            using var tl = PhysHits.Circle((hp3 + tb) * 0.5f, Vector3.Distance(hp3, tb) * 0.5f + 1f, true);
            foreach (Collider2D c in tl)
            {
                if (c == null || !(c.CompareTag("enermy") || c.CompareTag("boss"))) continue;
                if (Hostile.DistanceToSegment(c.transform.position, hp3, tb) > 0.8f) continue;
                Specials.Damage(c.gameObject, baseDmg * 1.1f, Vector3.zero, 0f);
            }
        }
        // 형태 · 상여 종: 매장한 구멍 둘레 3칸 적이 0.4초 멈춤 (보스 제외)
        if (form == SpecialAbilities.KitBell)
        {
            using var bl = PhysHits.Circle(hp3, 3f, true);
            foreach (Collider2D c in bl) if (c != null && c.CompareTag("enermy") && c.TryGetComponent(out EnermyController be) && !be.IsMidBoss && be.holder == null) be.Slow(0f, 0.4f);
            Fx.Spawn("fx_bell_ring", hp3, 6f, UtSilver, 14f, 0f, 16);
            Play("chime", 0.3f, 0.7f);
        }
        if (killedHere > 0)
        {
            RunCounters.Both("bury", killedHere);
            RunCounters.Max("bury.hole", killedHere);
            UtSpawnGrave(h.pos, killedHere >= UndertakerTuning.BigGraveAt, fromUlt);
            if (Special != null && Special.Has(SpecialAbilities.KitCondolence))
            {
                float chance = Special.IsEvolved(SpecialAbilities.KitCondolence) ? 0.1f : 0.05f;
                for (int i = 0; i < killedHere; i++)
                    if (Random.value < chance) { Coin coin = Cache<Coin>.Get; if (coin != null) coin.AddCoin(1); }
            }
        }
        else if (trappedCount == 0)
        {
            // 아무도 빠지지 않은 빈 구멍: 그냥 메워짐 (묘비 없음 · 고유 스킬 빈 무덤은 함정을 남김)
            SignatureSkills.UtEmptyBuried(h.pos);
            UtPathEmptyGrave(h.pos);
        }
        SignatureSkills.UtBuriedHole(h.pos, h.r, trappedCount, killedHere, fromUlt);
        return killedHere;
    }

    // 빈 무덤 (고유 스킬): 흙을 덮은 함정 — 처음 밟은 적이 빠짐 (동시 maxPits 개 · 오래된 것부터 치움)
    public void UtAddPit(Vector2 pos, int maxPits, int trapCap)
    {
        if (player == null || player.IsDying) return;
        while (true)
        {
            int n = 0;
            UtHole oldest = null;
            foreach (UtHole h in utHoles) if (h.pitfall) { n++; if (oldest == null || h.bornAt < oldest.bornAt) oldest = h; }
            if (n < maxPits || oldest == null) break;
            UtRemoveHole(oldest);
        }
        UtHole p = UtNewHole(pos, 0.9f, 0.9f, true);
        p.pitCap = Mathf.Max(1, trapCap);
        Fx.Spawn("fx_dirt", new Vector3(pos.x, pos.y, 0f), 1.4f, new Color(1f, 1f, 1f, 0.6f), 16f, 0f, 14);
    }

    // 되파기 (카드): 묘비가 무너지며 둘레 2칸 적에게 공격력 120 / 160 / 200%
    void UtRedig(UndertakerGrave g)
    {
        Vector2 p = g.Pos;
        float dmg = card[6] > 0 ? Damage * (0.8f + 0.4f * card[6]) * formPower : 0f;       // 카드 없이 도굴꾼 입문만 있으면 갈래 피해만
        UtBreakGrave(g);
        if (dmg > 0f) using (DamageSource.As(DamageSource.Basic)) UtDamageCircle(p, 2f, dmg, 2f, DamageSource.Basic, true);
        RunCounters.Both("grave.break");
        SignatureSkills.UtGraveRedug(p);
        UtPathRedug(p);
    }

    // 반지름 안 적 · 보스에게 (tag: 쓸 출처)
    void UtDamageCircle(Vector2 p, float r, float dmg, float knock, string source, bool bosses = true)
    {
        Vector3 p3 = new Vector3(p.x, p.y, 0f);
        using (DamageSource.As(source))
        using (var list = PhysHits.Circle(p3, r, true))
        {
            foreach (Collider2D c in list)
            {
                if (c == null) continue;
                bool boss = c.CompareTag("boss");
                if (!c.CompareTag("enermy") && !boss) continue;
                Specials.Damage(c.gameObject, dmg, (c.transform.position - p3).normalized, knock);
            }
        }
    }

    // ================================================================= 묘비
    //   매장으로 쓰러진 자리에만 (구멍당 하나) · 영구 8개 / 장례 임시 12개 · 장의사는 자기 묘비를 지나다님
    public UndertakerGrave UtSpawnGrave(Vector2 pos, bool big, bool temp, float lifeOverride = -1f, int hpOverride = -1)
    {
        if (player == null || player.IsDying) return null;
        if (Vector2.Distance(pos, transform.position) < UndertakerTuning.GraveMinFromPlayer) return null;
        if (!UtGraveSpotOk(pos)) return null;
        Fx.Spawn("fx_grave_rise", new Vector3(pos.x, pos.y, 0f), big ? 2.6f : 2f, Color.white, 10f, 0f, 14);       // 2.0.1~ 검사 뒤로 (묘비가 안 생겨도 솟는 연출이 나왔음)
        bool stone = form == SpecialAbilities.KitSarcophagus && !temp;
        int hp = (big ? UndertakerTuning.BigGraveHp : UndertakerTuning.GraveHp) * (stone ? 2 : 1) + SignatureSkills.UtGraveHpExtra();
        float life = temp ? UndertakerTuning.TempGraveLife : UndertakerTuning.GraveLife + SignatureSkills.UtGraveLifeExtra();
        if (lifeOverride > 0f) life = lifeOverride;
        if (hpOverride > 0) hp = hpOverride;
        hp = UtPathGraveHp(hp);
        UndertakerGrave g = UndertakerGrave.Make(pos, big, temp, false, life, hp, body);
        g.stone = stone;
        if (stone && g.sr != null) g.sr.color = new Color(0.82f, 0.85f, 0.95f);
        foreach (Collider2D pc in player.GetComponentsInChildren<Collider2D>()) if (pc != null && g.box != null) Physics2D.IgnoreCollision(pc, g.box);
        utGraves.Add(g);
        UtTrimGraves(temp);
        utGraveDirty = true;
        RunCounters.Both("grave");
        // 부장품(패시브): 묘비가 솟을 때 확률로 경험치
        if (Special != null && Special.Has(SpecialAbilities.KitGraveGoods) && Random.value < (Special.IsEvolved(SpecialAbilities.KitGraveGoods) ? 0.2f : 0.1f))
        {
            LevelShop ls = Cache<LevelShop>.Get;
            if (ls != null) ls.GainExp(player, 3f);
        }
        SignatureSkills.UtGraveRose(pos, big, temp);
        return g;
    }

    // 경기장 바닥 안쪽 2칸 밖 (벽 띠 가까이는 세우지 않음 — 흙더미 연출만)
    bool UtGraveSpotOk(Vector2 p)
    {
        Vector3 q = new Vector3(p.x, p.y, 0f);
        if (!Hostile.InArena(q)) return false;
        for (int k = 0; k < 4; k++)
        {
            float a = k * 90f * Mathf.Deg2Rad;
            if (!Hostile.InArena(q + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 2f)) return false;
        }
        foreach (UndertakerGrave g in utGraves) if (g != null && !g.isWall && Vector2.Distance(g.Pos, p) < 0.8f) return false;
        return true;
    }

    // 넘으면 가장 오래된 것부터 무너짐 (Paths.Trim 방식)
    void UtTrimGraves(bool temp)
    {
        int cap = temp ? UndertakerTuning.TempGraveCap : UtGraveMax;
        while (true)
        {
            int n = 0;
            UndertakerGrave oldest = null;
            foreach (UndertakerGrave g in utGraves)
            {
                if (g == null || g.isWall || g.temp != temp) continue;
                n++;
                if (oldest == null || g.until - (g.temp ? UndertakerTuning.TempGraveLife : UndertakerTuning.GraveLife) < oldest.until - (oldest.temp ? UndertakerTuning.TempGraveLife : UndertakerTuning.GraveLife)) oldest = g;
            }
            if (n <= cap || oldest == null) break;
            UtBreakGrave(oldest);
        }
    }

    public UndertakerGrave UtOldestGrave()
    {
        UndertakerGrave o = null;
        foreach (UndertakerGrave g in utGraves) if (g != null && !g.isWall && !g.temp && (o == null || g.until < o.until)) o = g;
        return o;
    }

    UndertakerGrave UtGraveAt(Vector2 p, float r)
    {
        UndertakerGrave best = null;
        float bd = r;
        foreach (UndertakerGrave g in utGraves)
        {
            if (g == null || g.isWall) continue;
            float d = Vector2.Distance(g.Pos, p);
            if (d <= bd) { bd = d; best = g; }
        }
        return best;
    }

    // 묘비를 무너뜨림 (피해 없음 — 피해는 부른 쪽이)
    public void UtBreakGrave(UndertakerGrave g)
    {
        if (g == null) return;
        utGraves.Remove(g);
        utGraveDirty = true;
        SignatureSkills.UtGraveBroke(g.Pos, g.temp);
        if (g.temp) UtPathTempBroke(g.Pos);
        g.Break();
    }

    // 묘비 조용히 치우기 (운구가 관으로 가져감)
    public void UtTakeGrave(UndertakerGrave g)
    {
        if (g == null) return;
        utGraves.Remove(g);
        utGraveDirty = true;
        Destroy(g.gameObject);
    }

    // 0.25초마다: 수명 · 보스 근접 무너짐 · 닿은 적에 의한 내구 (0.5초마다 하나당 −1, 큰 적 −2) · 석관은 닿은 적을 침
    void UtGraveTick(float now)
    {
        for (int i = utGraves.Count - 1; i >= 0; i--)
        {
            UndertakerGrave g = utGraves[i];
            if (g == null) { utGraves.RemoveAt(i); utGraveDirty = true; continue; }
            if (now >= g.until) { UtBreakGrave(g); SignatureSkills.UtGraveExpired(g.Pos); continue; }
            bosss nearBoss = null;
            foreach (bosss b in bosss.Live)
                if (b != null && !b.IsDead && Vector2.Distance(b.transform.position, g.Pos) < UndertakerTuning.BossBreakRadius) { nearBoss = b; break; }
            if (nearBoss != null) { SignatureSkills.UtBossBroke(g.Pos); UtPathBossBroke(nearBoss, g.Pos); UtBreakGrave(g); continue; }
            if (now < g.chipAt) continue;
            g.chipAt = now + 0.5f;
            int chip = 0;
            using (var list = PhysHits.Circle(new Vector3(g.Pos.x, g.Pos.y + 0.3f, 0f), 0.9f, true))
            {
                foreach (Collider2D c in list)
                {
                    if (c == null || !c.CompareTag("enermy") || !c.TryGetComponent(out EnermyController e) || e.IsDead || e.swarm != null || e.holder != null) continue;
                    chip += UtBodyR(e) > 0.9f || e.IsMidBoss ? 2 : 1;
                    if (g.stone) { using (DamageSource.As(DamageSource.Basic)) Specials.Damage(e.gameObject, Damage * 0.25f * formPower, Vector3.zero, 0f); }
                }
            }
            if (chip > 0)
            {
                g.hp -= chip;
                if (g.hp <= 0) { UtBreakGrave(g); }
            }
        }
    }

    // 흙담 (고유 스킬): 3칸 안에 이웃한 묘비끼리 이어짐 — 묘비가 바뀐 뒤 0.5초 안에 다시 이음
    void UtRelink()
    {
        foreach (UndertakerGrave w in utWalls) if (w != null) Destroy(w.gameObject);
        utWalls.Clear();
        int max = SignatureSkills.UtWallLinks();
        if (max <= 0 || utGraves.Count < 2) return;
        List<(float d, UndertakerGrave a, UndertakerGrave b)> pairs = new List<(float, UndertakerGrave, UndertakerGrave)>();
        for (int i = 0; i < utGraves.Count; i++)
            for (int j = i + 1; j < utGraves.Count; j++)
            {
                UndertakerGrave a = utGraves[i], b = utGraves[j];
                if (a == null || b == null || a.isWall || b.isWall || a.temp || b.temp) continue;
                float d = Vector2.Distance(a.Pos, b.Pos);
                if (d <= 3f && d >= 0.9f) pairs.Add((d, a, b));
            }
        pairs.Sort((x, y) => x.d.CompareTo(y.d));
        for (int k = 0; k < pairs.Count && utWalls.Count < max; k++)
        {
            UndertakerGrave w = UndertakerGrave.MakeWall(pairs[k].a.Pos + Vector2.up * 0.3f, pairs[k].b.Pos + Vector2.up * 0.3f, body);
            foreach (Collider2D pc in player.GetComponentsInChildren<Collider2D>()) if (pc != null && w.box != null) Physics2D.IgnoreCollision(pc, w.box);
            utWalls.Add(w);
        }
    }

    public int UtWallCount => utWalls.Count;

    // 자동 조작(GameInput.Auto · AutoplayBot): 갇힌 적이 4마리 이상이거나 가장 오래된 구멍이 4초를 넘으면 가장 많이 갇힌 구멍 가운데를 돌려줌
    public bool UtAutoBuryAt(out Vector3 at)
    {
        at = default;
        UtHole best = null;
        foreach (UtHole h in utHoles)
        {
            if (h.pitfall || h.burying || h.growing) continue;
            if (best == null || h.traps.Count > best.traps.Count) best = h;
        }
        if (best == null) return false;
        bool old = Time.time - best.bornAt > 4f;
        foreach (UtHole h in utHoles) if (!h.pitfall && !h.burying && !h.growing && Time.time - h.bornAt > 4f) old = true;
        if (UtTrappedCount < 4 && !old) return false;
        at = new Vector3(best.pos.x, best.pos.y, 0f);
        return true;
    }

    // ================================================================= 조준 표시 (HeroAim)
    //   커서가 판 구멍 위면 「덮을 것」 — 모든 구멍 테가 흙빛으로 밝아짐 · 빈 땅이면 「여기 팔 것」 작은 고리 + 최대 크기 점선 고리 · 파는 동안 실제 반지름이 차오름
    void UtAimTick()
    {
        bool hide = PlayerController.Cinematic || player.IsDying || Time.timeScale == 0f || (player.special != null && player.special.WeaponActive) || Busy;
        if (hide) { UtHideAim(); return; }
        Vector2 c = utDigging && utCur != null ? utCur.pos : UtCursor(transform.position);
        if (!utDigging) utCursor = c;
        UtHole over = utDigging ? null : UtHoleAt(c);
        Color hero = HeroAim.ColorOf(HeroAimKind.Undertaker);
        if (utRing == null) { utRing = Hostile.NewLine("UtRing", hero, 0.1f, 24); utRing.loop = true; }
        if (utRingMax == null) { utRingMax = Hostile.NewLine("UtRingMax", hero, 0.06f, 23); utRingMax.loop = true; }
        float pulse = 0.55f + 0.2f * Mathf.Sin(Time.time * 8f);
        // 구멍 테: 덮을 것이면 밝게
        for (int i = 0; i < utHoles.Count; i++)
        {
            while (utHoleRings.Count <= i)
            {
                LineRenderer lr = Hostile.NewLine("UtHoleRing", hero, 0.08f, 22);
                lr.loop = true;
                utHoleRings.Add(lr);
            }
            UtHole h = utHoles[i];
            LineRenderer ring = utHoleRings[i];
            bool on = !h.pitfall;
            ring.enabled = on;
            if (!on) continue;
            Hostile.SetArc(ring, new Vector3(h.pos.x, h.pos.y, 0f), h.r, 0f, 360f);
            Color hc = over != null ? Color.Lerp(UtSoil, Color.white, 0.35f) : hero;
            hc.a = over != null ? 0.95f : 0.35f;
            ring.startColor = ring.endColor = hc;
            ring.startWidth = ring.endWidth = over != null ? 0.14f : 0.06f;
            HeroAim.Mark(ring, HeroAimKind.Undertaker, HeroAimFx.None);
        }
        for (int i = utHoles.Count; i < utHoleRings.Count; i++) if (utHoleRings[i] != null) utHoleRings[i].enabled = false;
        if (over != null)
        {
            // 뒤집힌 삽: 작은 고리를 흙빛으로 (이 자리를 누르면 모든 구멍이 덮임)
            utRing.enabled = true;
            Hostile.SetArc(utRing, new Vector3(c.x, c.y, 0f), 0.35f, 0f, 360f);
            utRing.startColor = utRing.endColor = new Color(UtSoil.r, UtSoil.g, UtSoil.b, 0.9f);
            utRing.startWidth = utRing.endWidth = 0.12f;
            HeroAim.Mark(utRing, HeroAimKind.Undertaker, HeroAimFx.None);
            utRingMax.enabled = false;
            return;
        }
        float r = utDigging && utCur != null ? utCur.r : UndertakerTuning.HoleMin;
        utRing.enabled = true;
        Hostile.SetArc(utRing, new Vector3(c.x, c.y, 0f), r, 0f, 360f);
        utRing.startColor = utRing.endColor = new Color(hero.r, hero.g, hero.b, pulse);
        utRing.startWidth = utRing.endWidth = utDigging ? 0.14f : 0.1f;
        HeroAim.Mark(utRing, HeroAimKind.Undertaker, HeroAimFx.Full);
        utRingMax.enabled = true;
        float rm = utDigging && utCur != null ? utCur.rMax : UtHoleMaxR;
        Hostile.SetArc(utRingMax, new Vector3(c.x, c.y, 0f), rm, 0f, 360f);
        utRingMax.startColor = utRingMax.endColor = new Color(hero.r, hero.g, hero.b, 0.3f);
        utRingMax.startWidth = utRingMax.endWidth = 0.05f;
        HeroAim.Mark(utRingMax, HeroAimKind.Undertaker, HeroAimFx.None);
    }

    void UtHideAim()
    {
        if (utRing != null) utRing.enabled = false;
        if (utRingMax != null) utRingMax.enabled = false;
        foreach (LineRenderer lr in utHoleRings) if (lr != null) lr.enabled = false;
        UtHideUltAim();
    }

    // 머리 위 막대: 구멍 수 / 최대 (구멍이 없으면 묘비 수 / 최대)
    void UtRuleBar()
    {
        int holes = UtHoleCount, graves = UtGraveCount;
        if (holes > 0 || graves == 0)
        {
            int max = UtHoleMax;
            DrawRuleBar(Mathf.Clamp01(holes / (float)Mathf.Max(1, max)), holes >= max ? Color.Lerp(UtSoil, Color.white, 0.4f + 0.4f * Mathf.Sin(Time.time * 12f)) : UtSilver, true, "구멍", holes, max);
        }
        else DrawRuleBar(Mathf.Clamp01(graves / (float)Mathf.Max(1, UtGraveMax)), UtSilver, true, "묘비", graves, UtGraveMax);
    }

    // ================================================================= 정리
    void OnDestroyUndertaker()
    {
        if (Id != CharacterId.Undertaker) return;
        EnemyHold.ReleaseAll(this);
        foreach (UtHole h in utHoles) if (h != null && h.go != null) Destroy(h.go);
        utHoles.Clear();
        foreach (UtHole h in utBurying) if (h != null && h.go != null) Destroy(h.go);
        utBurying.Clear();
        foreach (UndertakerGrave g in utGraves) if (g != null) Destroy(g.gameObject);
        utGraves.Clear();
        foreach (UndertakerGrave w in utWalls) if (w != null) Destroy(w.gameObject);
        utWalls.Clear();
        foreach (LineRenderer lr in utHoleRings) if (lr != null) Destroy(lr.gameObject);
        if (utRing != null) Destroy(utRing.gameObject);
        if (utRingMax != null) Destroy(utRingMax.gameObject);
        UtUltCleanup();
    }
}
