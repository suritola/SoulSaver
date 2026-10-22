using System.Collections.Generic;
using UnityEngine;

// 경기장 치수 한 곳 (docs/art/맵-확장-기획서.md 맵-1 · 2026-10-22~)
//   다섯 맵(묘역 · 지옥 · 초원 · 심연 · 사막)이 같은 틀: 걷는 바닥 109×69칸 · 벽 띠 두께 왼 8 · 오른 8 · 아래 7 · 위 10 · 벽 판정은 묘역 Wall 의 BoxCollider2D 4개
//   원본 치수 × 배율(Mul)로 바닥 · 벽 상자 · 적 생성 범위 · 보스 등장 · 포털을 계산하고 StageManager.Awake 에서 한 번에 적용
//   배율은 Awake 마다 다시 정함 (정적 「적용함」 플래그 없음 — 장면을 다시 읽으면 벽 상자 · 포털은 장면값, spawner 는 코드 기본값으로 돌아오므로)
//   하나라도 맞지 않으면(벽 상자를 못 찾음 · 원본 값이 예상과 다름) 아무것도 바꾸지 않고 LogError — 맵마다 따로 바꾸면 벽 그림과 경계가 어긋남
public static class ArenaRules
{
    // 원본 (배율 1) — 장면 실측: Floor 타일맵 원점 (−54,−38) · 크기 109×69, Wall 타일맵 (−62,−45) 125×86
    static readonly Vector2 BaseMin = new Vector2(-54f, -38f), BaseMax = new Vector2(55f, 31f);
    const float WallLeft = 8f, WallRight = 8f, WallBottom = 7f, WallTop = 10f;
    const float SpawnInset = 2f;            // 적 생성 범위 = 바닥 안쪽 2칸 (BossUltimate.KeepInside 의 +3 여유와 맞물림)
    const float BossAbove = 1f, PortalAbove = 2.8f;     // 위 벽선 위로 (보스 등장 (0.5,32) · 포털 (0.5,33.8))
    const float GateClear = 12f;            // 무한 지형: 성문(보스 등장) 아래 이만큼은 비움 (EndlessTerrain.SpotOk)

    // 이번 장면에서 쓰는 배율 (Apply 가 정함 · 그 전엔 1)
    public static float Mul { get; private set; } = 1f;
    public static Vector2 FloorMin { get; private set; } = BaseMin;
    public static Vector2 FloorMax { get; private set; } = BaseMax;
    public static Vector2 Center => (FloorMin + FloorMax) * 0.5f;
    public static Vector2 SpawnMin => FloorMin + Vector2.one * SpawnInset;
    public static Vector2 SpawnMax => FloorMax - Vector2.one * SpawnInset;
    public static Vector2 BossSpawn => new Vector2(Center.x, FloorMax.y + BossAbove);
    public static Vector2 PortalAt => new Vector2(Center.x, FloorMax.y + PortalAbove);
    public static float GateForbidY(float gateY) => gateY - GateClear;

    // 벽 상자 4개 (월드 = Grid(0,0) 자식이라 offset 그대로)
    public static void WallBox(int side, out Vector2 offset, out Vector2 size)
    {
        Vector2 mn = FloorMin, mx = FloorMax;
        float outW = (mx.x - mn.x) + WallLeft + WallRight, outH = (mx.y - mn.y) + WallBottom + WallTop;
        float cx = (mn.x - WallLeft + mx.x + WallRight) * 0.5f, cy = (mn.y - WallBottom + mx.y + WallTop) * 0.5f;
        switch (side)
        {
            case Top: offset = new Vector2(cx, mx.y + WallTop * 0.5f); size = new Vector2(outW, WallTop); break;
            case Bottom: offset = new Vector2(cx, mn.y - WallBottom * 0.5f); size = new Vector2(outW, WallBottom); break;
            case Left: offset = new Vector2(mn.x - WallLeft * 0.5f, cy); size = new Vector2(WallLeft, outH); break;
            default: offset = new Vector2(mx.x + WallRight * 0.5f, cy); size = new Vector2(WallRight, outH); break;
        }
    }
    public const int Top = 0, Bottom = 1, Left = 2, Right = 3;

    // 배율에 맞춘 바닥: 중심 고정 · 가로 · 세로 칸 수는 홀수로 반올림 (가운데 칸 기준 좌우 대칭이 되게)
    static void Compute(float mul)
    {
        Vector2 c = (BaseMin + BaseMax) * 0.5f;
        float w = Odd((BaseMax.x - BaseMin.x) * mul), h = Odd((BaseMax.y - BaseMin.y) * mul);
        FloorMin = new Vector2(c.x - w * 0.5f, c.y - h * 0.5f);
        FloorMax = new Vector2(c.x + w * 0.5f, c.y + h * 0.5f);
        Mul = mul;
    }
    static float Odd(float v)
    {
        int n = Mathf.RoundToInt(v);
        if ((n & 1) == 0) n += v >= n ? 1 : -1;
        return n;
    }

    // StageManager.Awake: 벽 상자 · 생성 범위 · 보스 등장 · 포털을 배율에 맞춤
    public static void Apply(StageManager sm, float mul)
    {
        Compute(1f);
        if (sm == null) return;
        EnemySpawner sp = sm.spawner != null ? sm.spawner : Object.FindFirstObjectByType<EnemySpawner>();
        BoxCollider2D[] walls = FindWalls(sm.caveRenderers);
        // 원본 점검 (배율 1 계산값과 지금 값이 같아야 함 — 다르면 장면 · 코드가 바뀐 것이라 손대지 않음)
        List<string> bad = new List<string>();
        if (sp == null) bad.Add("EnemySpawner 없음");
        else
        {
            if (!Near(sp.spawnAreaMin, SpawnMin) || !Near(sp.spawnAreaMax, SpawnMax)) bad.Add("spawnArea " + sp.spawnAreaMin + "~" + sp.spawnAreaMax);
            if (!Near(sp.bossSpawnPoint, BossSpawn)) bad.Add("bossSpawnPoint " + sp.bossSpawnPoint);
        }
        if (walls == null) bad.Add("벽 상자 4개를 못 찾음");
        else
            for (int s = 0; s < 4; s++)
            {
                WallBox(s, out Vector2 o, out Vector2 z);
                if (!Near(walls[s].offset, o) || !Near(walls[s].size, z)) bad.Add("벽 상자 " + s + " " + walls[s].offset + "/" + walls[s].size);
            }
        if (sm.portal != null && !Near(sm.portal.transform.position, PortalAt)) bad.Add("portal " + sm.portal.transform.position);
        if (bad.Count > 0)
        {
            Debug.LogError("[ArenaRules] 원본 치수가 예상과 달라 경기장을 바꾸지 않음: " + string.Join(" · ", bad));
            return;
        }
        if (Mathf.Approximately(mul, 1f)) return;

        Compute(mul);
        sp.spawnAreaMin = SpawnMin;
        sp.spawnAreaMax = SpawnMax;
        sp.bossSpawnPoint = BossSpawn;
        for (int s = 0; s < 4; s++)
        {
            WallBox(s, out Vector2 o, out Vector2 z);
            walls[s].offset = o;
            walls[s].size = z;
        }
        if (sm.portal != null)
        {
            Vector3 p = sm.portal.transform.position;
            sm.portal.transform.position = new Vector3(PortalAt.x, PortalAt.y, p.z);
        }
    }

    // 묘역 Wall 렌더러(caveRenderers 중 BoxCollider2D 를 정확히 4개 가진 것)에서 offset 방향으로 위 · 아래 · 왼 · 오른 판별
    //   태그로 찾지 않음 — 장면에 Wall 태그가 둘 (콜라이더 없는 장식 층에도)
    static BoxCollider2D[] FindWalls(Renderer[] rs)
    {
        if (rs == null) return null;
        foreach (Renderer r in rs)
        {
            if (r == null) continue;
            BoxCollider2D[] bs = r.GetComponents<BoxCollider2D>();
            if (bs.Length != 4) continue;
            BoxCollider2D[] by = new BoxCollider2D[4];
            foreach (BoxCollider2D b in bs)
            {
                bool wide = b.size.x > b.size.y;
                int side = wide ? (b.offset.y > 0f ? Top : Bottom) : (b.offset.x > 0f ? Right : Left);
                if (by[side] != null) return null;
                by[side] = b;
            }
            return by;
        }
        return null;
    }

    static bool Near(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 0.01f;
}
