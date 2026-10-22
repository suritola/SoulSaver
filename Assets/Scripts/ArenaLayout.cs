using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 정돈된 경기장 배치 (docs/art/맵-확장-기획서.md 3장 · 맵 확장 5 · 2026-10-22~)
//   예전: 바닥 14~27% 무작위 점무늬 · 피해 없는 용암 · 웅덩이 타일 · 해골 · 가시 · 통 · 상자처럼 적 · 깨지는 물체와 닮은 장식이 반쯤 대칭으로 흩어져 있었음
//   지금: 바탕 타일 하나 + 8칸 이음매 · 십자 길(폭 5 · 양쪽 연석) · 가운데 광장 · 성문 앞뜰, 벽 정면 창은 좌우 대칭,
//         키 큰 장식은 벽선 위 · 성문 양옆 · 광장 표지에만, 구획 무늬와 깨지는 물체 자리(PropSockets)는 바둑판으로 번갈아
//   묘역 · 지옥 · 초원의 장면 타일맵 세 층(바닥 · 벽 · 장식)을 지우고 다시 깖 — 사막 · 심연은 지옥을 복제하므로 그대로 물려받음
//   모두 정적 계산 · 그림이 하나라도 없으면 아무것도 안 바꿈 (Prepare 가 null) · 스크립트를 붙이지 않음 (복제되는 hellMap 에 컴포넌트 없음)
public static class ArenaLayout
{
    const int Z = 100;      // 장면 타일은 모두 z 100 (EndlessTerrain.TileZ)

    // 장마다 재료 (타일셋 칸 · 장식 칸 번호는 tileset_<장>_32_N · objects_<장>_N)
    class Theme
    {
        public int realm;
        public int pathSlot; public float pathMul;      // 길 바닥
        public int rimSlot; public float rimMul;        // 광장 테
        public bool square;                             // 광장을 정사각으로 (묘역)
        public int topPillar;                           // 위 벽선
        public int[] rimAlt;                            // 아래 · 옆 벽선 (번갈아)
        public int gateSide, marker;                    // 성문 양옆 · 광장 표지
        public (int slot, int dx)[] motif;              // 구획 무늬 (같은 바닥선 위)
    }
    static readonly Theme[] Themes =
    {
        new Theme { realm = EndlessTerrain.Cave, pathSlot = 0, pathMul = 1.08f, rimSlot = 0, rimMul = 0.85f, square = true,
                    topPillar = 2, rimAlt = new[] { 2, 1 }, gateSide = 7, marker = 2, motif = new[] { (5, -4), (5, 0), (5, 4) } },
        new Theme { realm = EndlessTerrain.Hell, pathSlot = 1, pathMul = 1f, rimSlot = 2, rimMul = 1f,
                    topPillar = 2, rimAlt = new[] { 2, 3 }, gateSide = 7, marker = 2, motif = new[] { (3, -6), (1, 0), (3, 6) } },
        new Theme { realm = EndlessTerrain.Meadow, pathSlot = 3, pathMul = 1f, rimSlot = 9, rimMul = 1f,
                    topPillar = 2, rimAlt = new[] { 2, 4 }, gateSide = 7, marker = 7, motif = new[] { (5, -4), (5, 0), (5, 4) } },
    };

    const float SeamMul = 0.92f, CurbMul = 0.88f;
    // 바닥 밖 벽 채움 폭 — 카메라 반 높이 28 · 16:9 반 너비 약 50 (+ 여유) · 그보다 넓은 화면 · 조준 줌은 배경색이 보임
    const int OuterX = 54, OuterY = 32;
    const int SeamEvery = 8, PathHalf = 2, PlazaR = 11, MarkerDx = 12, MarkerDy = 12, GateClearX = 10, TopStep = 13, RimStep = 12;

    // 깨지는 물체 자리 (월드 좌표 · 칸 가운데) — MapProps 가 씀 (맵 확장 6)
    public static readonly List<Vector3> PropSockets = new List<Vector3>();
    // 구획 무늬 자리 (가운데 칸 · 왼쪽이면 true) — 사막 · 심연이 지옥 무늬를 비우고 자기 소품을 둠 (맵 확장 6)
    public static readonly List<(Vector3Int cell, bool left)> MotifCenters = new List<(Vector3Int, bool)>();

    public class Plan
    {
        public readonly List<(GameObject root, Tilemap tm, TileChangeData[] tiles)> maps = new List<(GameObject, Tilemap, TileChangeData[])>();
    }

    // 세 장(묘역 · 지옥 · 초원) 계산 — 하나라도 못 하면 null (그림 · 타일맵이 빠짐)
    public static Plan Prepare(StageManager sm, out string why)
    {
        why = null;
        Plan plan = new Plan();
        Transform grid = sm.hellMap != null ? sm.hellMap.transform.parent : null;
        GameObject[] roots = { null, sm.hellMap, sm.meadowMap };
        for (int t = 0; t < Themes.Length; t++)
        {
            Theme th = Themes[t];
            Tilemap floor, wall, obj;
            if (t == 0) { floor = Child(grid, "Floor"); wall = Child(grid, "Wall"); obj = Child(grid, "Object"); }
            else { floor = Find(roots[t], "Floor"); wall = Find(roots[t], "Wall"); obj = Find(roots[t], "Object"); }
            if (floor == null || wall == null || obj == null) { why = "타일맵 없음 (" + th.realm + ")"; return null; }
            if (!Build(th, out TileChangeData[] f, out TileChangeData[] w, out TileChangeData[] o, out why)) return null;
            plan.maps.Add((roots[t], floor, f));
            plan.maps.Add((roots[t], wall, w));
            plan.maps.Add((roots[t], obj, o));
        }
        return plan;
    }

    // 한 번에 적용 (비활성 맵은 잠깐 켜고 편집 뒤 원래대로)
    public static void Apply(Plan plan)
    {
        foreach (var m in plan.maps)
        {
            bool was = m.root == null || m.root.activeSelf;
            if (m.root != null && !was) m.root.SetActive(true);
            m.tm.ClearAllTiles();
            m.tm.SetTiles(m.tiles, true);
            m.tm.CompressBounds();      // 경계를 실제 타일로 (z 도 100 한 층) — 맵 확장 3 의 지형 기록 칸 틀이 이 범위를 씀
            if (m.root != null && !was) m.root.SetActive(false);
        }
    }

    static Tilemap Child(Transform grid, string name)
    {
        Transform c = grid != null ? grid.Find(name) : null;
        return c != null ? c.GetComponent<Tilemap>() : null;
    }
    static Tilemap Find(GameObject root, string part)
    {
        if (root == null) return null;
        foreach (Tilemap tm in root.GetComponentsInChildren<Tilemap>(true)) if (tm.name.EndsWith(part)) return tm;
        return null;
    }

    // ---------------------------------------------------------------- 한 장 계산 (칸 좌표 · 가운데 열 x 0 · 가운데 줄 cy)
    static bool Build(Theme th, out TileChangeData[] floorTiles, out TileChangeData[] wallTiles, out TileChangeData[] objTiles, out string why)
    {
        floorTiles = wallTiles = objTiles = null;
        why = null;
        Sprite Ts(int k) => EndlessTerrain.PaletteSprite(th.realm, 0, k);
        Sprite Os(int k) => EndlessTerrain.PaletteSprite(th.realm, 1, k);
        int[] needT = { 0, th.pathSlot, th.rimSlot, 5, 6, 7, 8 };
        foreach (int nt in needT) if (Ts(nt) == null) { why = "타일셋 " + th.realm + "_" + nt + " 없음"; return false; }
        List<int> needO = new List<int> { 0, th.topPillar, th.gateSide, th.marker };
        needO.AddRange(th.rimAlt);
        foreach (var m in th.motif) needO.Add(m.slot);
        foreach (int no in needO) if (Os(no) == null) { why = "장식 " + th.realm + "_" + no + " 없음"; return false; }

        int x0 = Mathf.RoundToInt(ArenaRules.FloorMin.x), x1 = Mathf.RoundToInt(ArenaRules.FloorMax.x) - 1;      // 바닥 칸 (양 끝 포함)
        int y0 = Mathf.RoundToInt(ArenaRules.FloorMin.y), y1 = Mathf.RoundToInt(ArenaRules.FloorMax.y) - 1;
        int cy = Mathf.RoundToInt(ArenaRules.Center.y - 0.5f);         // 가운데 줄 (−3.5 → −4)
        int top = y1 + 1;                                               // 벽 정면 첫 줄

        // 바닥
        List<TileChangeData> fl = new List<TileChangeData>();
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int dy = y - cy, ax = Mathf.Abs(x), ay = Mathf.Abs(dy);
                Sprite s = Ts(0);
                float mul = 1f;
                bool yard = ax <= 7 && y >= y1 - 8;                     // 성문 앞뜰
                bool plaza = th.square ? Mathf.Max(ax, ay) <= PlazaR : ax * ax + dy * dy <= PlazaR * PlazaR;
                bool plazaRim = plaza && (th.square ? Mathf.Max(ax, ay) == PlazaR : ax * ax + dy * dy > (PlazaR - 1) * (PlazaR - 1));
                bool path = ax <= PathHalf || ay <= PathHalf;
                bool curb = !path && (ax == PathHalf + 1 || ay == PathHalf + 1);
                if (y == y1) s = Ts(5);                                  // 벽 바로 아래 그늘
                else if (plazaRim) { s = Ts(th.rimSlot); mul = th.rimMul; }
                else if (plaza || path || yard) { s = Ts(th.pathSlot); mul = th.pathMul; }
                else
                {
                    if (curb) mul = CurbMul;
                    else if (x % SeamEvery == 0 || (dy % SeamEvery + SeamEvery) % SeamEvery == 0) mul = SeamMul;      // 8칸 이음매
                }
                fl.Add(Cell(x, y, s, mul, false));
            }
        floorTiles = fl.ToArray();

        // 벽 띠 (채움 · 정면 4줄 · 창은 x ≡ 3 (mod 6) 좌우 대칭 · 성문 자리는 비움)
        //   2026-10-22~ 맵 바깥이 보이지 않게 채움을 화면 반 너비 · 반 높이 너머까지 (사용자 「맵 바깥쪽이 안 보였으면」) — 판정은 벽 상자 4개 그대로
        List<TileChangeData> wl = new List<TileChangeData>();
        int wx0 = x0 - OuterX, wx1 = x1 + OuterX, wy0 = y0 - OuterY, wy1 = y1 + OuterY;
        for (int y = wy0; y <= wy1; y++)
            for (int x = wx0; x <= wx1; x++)
            {
                bool inside = x >= x0 && x <= x1 && y >= y0 && y <= y1;
                if (inside) continue;
                bool front = x >= x0 && x <= x1 && y >= top && y <= top + 3;
                Sprite s = Ts(6);
                if (front) s = (y == top + 1 && Mathf.Abs(x) >= 8 && ((x % 6) + 6) % 6 == 3) ? Ts(8) : Ts(7);
                wl.Add(Cell(x, y, s, 1f, false));
            }
        wallTiles = wl.ToArray();

        // 장식 (좌우 대칭 · 왼쪽은 좌우 반전)
        List<TileChangeData> ob = new List<TileChangeData>();
        void Put(int x, int y, int slot) => ob.Add(Cell(x, y, Os(slot), 1f, x < 0));
        void Pair(int x, int y, int slot) { Put(x, y, slot); if (x != 0) Put(-x, y, slot); }
        Put(0, top + 5, 0);                                             // 성문
        Pair(8, top, th.gateSide);                                      // 성문 양옆
        for (int x = TopStep; x <= x1 - 4; x += TopStep) if (x >= GateClearX) Pair(x, top + 3, th.topPillar);     // 위 벽선
        int k = 0;
        for (int x = 6; x <= x1 - 4; x += RimStep, k++) Pair(x, y0, th.rimAlt[k % th.rimAlt.Length]);           // 아래 벽선
        k = 0;
        for (int d = RimStep; cy + d <= y1 - 6 || cy - d >= y0 + 4; d += RimStep, k++)                          // 옆 벽선 (길 자리는 비움)
        {
            int slot = th.rimAlt[k % th.rimAlt.Length];
            if (cy + d <= y1 - 6) { Put(x0, cy + d, slot); Put(x1, cy + d, slot); }
            if (cy - d >= y0 + 4) { Put(x0, cy - d, slot); Put(x1, cy - d, slot); }
        }
        foreach (int dyM in new[] { MarkerDy, -MarkerDy }) Pair(MarkerDx, cy + dyM, th.marker);                // 광장 표지

        // 구획: x ±20 · ±40 … × y 가운데 ±12 · ±28 (바닥 안쪽) — 바둑판으로 무늬 / 깨지는 물체 자리 (109×69 에서도 자리 8곳 · 물체 6~8개)
        if (th.realm == EndlessTerrain.Hell) { PropSockets.Clear(); MotifCenters.Clear(); }
        int ix = 0;
        for (int gx = 20; gx <= x1 - 12; gx += 20, ix++)
        {
            int iy = 0;
            foreach (int gdy in new[] { 28, 12, -12, -28 })
            {
                int gy = cy + gdy;
                iy++;
                if (gy > y1 - 6 || gy < y0 + 4) continue;
                bool motif = ((ix + iy) & 1) == 0;
                foreach (int sx in new[] { gx, -gx })
                {
                    if (motif)
                    {
                        foreach (var m in th.motif) Put(sx + (sx < 0 ? -m.dx : m.dx), gy, m.slot);
                        if (th.realm == EndlessTerrain.Hell) MotifCenters.Add((new Vector3Int(sx, gy, Z), sx < 0));
                    }
                    else if (th.realm == EndlessTerrain.Hell) PropSockets.Add(new Vector3(sx + 0.5f, gy + 0.5f, 0f));     // 자리는 한 번만 (세 장 공통 틀)
                }
            }
        }
        objTiles = ob.ToArray();
        return true;
    }

    // 지옥 복제(사막 · 심연)의 구획 무늬 타일을 비우고 그 자리에 자기 소품 (스크립트 없는 SpriteRenderer · 무작위 없음 · 왼쪽은 좌우 반전)
    //   kinds: 지옥 무늬(Themes[1].motif)의 dx 순서대로 · 이름이 없으면 그 칸은 비움
    public static Transform PlaceMotifProps(GameObject map, Dictionary<string, Sprite> art, string rootName, (string name, float scale)[] kinds, int sorting, int order)
    {
        if (map == null || MotifCenters.Count == 0) return null;
        Tilemap obj = Find(map, "Object");
        var motif = Themes[1].motif;
        GameObject root = new GameObject(rootName);
        root.transform.SetParent(map.transform, false);
        foreach (var mc in MotifCenters)
            for (int i = 0; i < motif.Length; i++)
            {
                int x = mc.cell.x + (mc.left ? -motif[i].dx : motif[i].dx);
                if (obj != null) obj.SetTile(new Vector3Int(x, mc.cell.y, Z), null);
                if (i >= kinds.Length || !art.TryGetValue(kinds[i].name, out Sprite sprite)) continue;
                GameObject go = new GameObject(kinds[i].name);
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(x + 0.5f, mc.cell.y + 0.5f, 0f);
                go.transform.localScale = Vector3.one * kinds[i].scale;
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerID = sorting;
                sr.sortingOrder = order;
                sr.flipX = mc.left;
            }
        return root.transform;
    }

    static TileChangeData Cell(int x, int y, Sprite s, float mul, bool flip)
    {
        Color c = new Color(mul, mul, mul, 1f);
        Matrix4x4 m = flip ? Matrix4x4.Scale(new Vector3(-1f, 1f, 1f)) : Matrix4x4.identity;
        return new TileChangeData(new Vector3Int(x, y, Z), EndlessTerrain.TileFor(s), c, m);
    }
}
