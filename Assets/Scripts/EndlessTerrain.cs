using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

// 무한 모드 지형 변화 「왕들의 영토」 (1.2.3~): 보스를 쓰러뜨리면 그 왕의 영토(묘역 · 지옥 · 초원 · 심연 타일)가
//   쓰러진 자리에서 파동처럼 퍼지고, 영토마다 지형 조각 2개(묘비 · 용암 웅덩이 · 점액 늪 · 영혼 수정)가 솟음
//   EndlessMode.BuildDesertMap 이 붙임 · bosss.Die 가 자리를 알림 · EndlessMode.OnBossDefeated / StartFinal / ClearEndless 가 부름
//   저장 · 이어하기: EndlessMode.SaveRun · RestoreRun 이 Save · Restore 로 RunSave.Data.terrainSeed · terrain 에 적고 되살림
// 성능: 타일은 층마다 프레임당 SetTiles 몇 번(고정 크기 배열 묶음) · Tile 은 스프라이트마다 하나(정적 캐시) · 새 재질 · 텍스처 없음 · 조각 · 장식은 상한
public class EndlessTerrain : MonoBehaviour
{
    public static EndlessTerrain Instance { get; private set; }

    public const int Desert = 0, Cave = 1, Hell = 2, Meadow = 3, Abyss = 4;
    const float RadiusEarly = 18f, RadiusLate = 21f, RadiusBeyond = 24f, WaveSpeed = 30f;
    const int MaxObstacles = 6, MaxZones = 4, MaxDecor = 300, DecorPerRealm = 8, PiecesPerRealm = 2, SpotTries = 40, PerFrame = 400;
    const float ClearPlayer = 9f, ClearPiece = 7f, ClearEdge = 5f, ClearGate = 12f, ClearEnemy = 2.5f, CenterInset = 6f;
    public const float LavaRadius = 2.6f, BogRadius = 3.0f;
    const float LavaCore = 2.2f, LavaRim = 3.0f, BogCore = 2.6f;
    public const float PieceScale = 2.4f;
    const byte Empty = 0xFF;
    const int MaxSlot = 16;

    // 영토마다: 알림 · 고리 색, 모래 바람 색, 추가 장식 칸 (objects_*_k), 알림 부제
    static readonly Color[] AccentColor =
    {
        new Color(0.95f, 0.78f, 0.5f), new Color(0.75f, 0.6f, 1f), new Color(1f, 0.45f, 0.15f), new Color(0.55f, 1f, 0.35f), new Color(0.55f, 0.9f, 1f),
    };
    static readonly Color[] GustColor =
    {
        new Color(0.95f, 0.78f, 0.5f, 0.35f), new Color(0.6f, 0.65f, 0.8f, 0.3f), new Color(0.9f, 0.4f, 0.2f, 0.35f), new Color(0.7f, 0.9f, 0.5f, 0.3f), new Color(0.45f, 0.4f, 0.75f, 0.25f),
    };
    static readonly int[][] DecorKinds = { new int[0], new[] { 5, 6, 3, 7 }, new[] { 4, 6, 7, 8 }, new[] { 3, 4, 5, 7 }, new[] { 4, 5, 6, 7 } };
    static readonly string[] Subtitle =
    {
        "",
        "묘비가 솟아 길과 탄을 막습니다. 보스가 지나가면 부서집니다.",
        "용암 웅덩이가 끓어오릅니다. 밟으면 아프고, 적은 느려집니다.",
        "점액 늪이 번집니다. 안에서는 모두가 느려집니다.",
        "영혼 수정이 솟아 길과 탄을 막습니다. 보스가 지나가면 부서집니다.",
    };
    static readonly Color MeadowTint = new Color(0.58f, 0.58f, 0.58f, 1f);     // StageManager.Dim(Color.white) — 초원 맵은 타일맵 색으로 어둡게 함
    static readonly Color BogTint = new Color(0.6f, 1f, 0.5f, 1f);

    public static Color Accent(int realm) => Hostile.Accessible(AccentColor[Mathf.Clamp(realm, 0, 4)]);
    public static Color Gust(int realm) => GustColor[Mathf.Clamp(realm, 0, 4)];
    public static bool Embers(int realm) => realm == Desert || realm == Hell;       // 불티는 사막 · 지옥 위에서만

    // ---------------------------------------------------------------- 맵
    readonly Tilemap[] layers = new Tilemap[3];         // 0 바닥 · 1 벽 · 2 장식 (TilemapRenderer.sortingOrder 순)
    readonly Vector3Int[] off = new Vector3Int[3];      // 바닥 칸 → 층 칸
    readonly byte[][] slot = new byte[3][];              // 칸 번호 k (tileset_*_32_k · objects_*_k), Empty = 빈 칸
    int ux, uy, uw, uh;                                  // 세 층을 덮는 범위 (바닥 칸 좌표)
    byte[] realmOf;                                      // 칸의 영토
    bool[] decorFlag;                                    // 지형이 더한 장식 칸
    readonly List<int> decorAdded = new List<int>();
    Transform props, pieceRoot;
    int sorting, order;
    internal int SortLayer => sorting;
    internal int SortOrder => order;
    Vector2 areaMin, areaMax;
    Vector2 gate = new Vector2(0.5f, 32f);

    // ---------------------------------------------------------------- 판 상태
    int seed, stage;                 // 지형 단계 = 적용한 영토 수 (기록의 항목 수)
    int entries;                     // 기록에 그대로 적힌 영토 항목 수 (많아지면 Save 가 한 장으로 접음)
    const int CompactAt = 24;
    bool ready, final, beyond, busy;
    readonly List<TerrainPiece> pieces = new List<TerrainPiece>();
    readonly StringBuilder log = new StringBuilder();
    readonly List<Pending> pendings = new List<Pending>();
    class Pending { public int realm; public Vector3 c; public float r; }

    // ---------------------------------------------------------------- 그림 · 타일
    static readonly Dictionary<Sprite, Tile> tileCache = new Dictionary<Sprite, Tile>();
    static readonly Sprite[][][] art = new Sprite[5][][];      // [영토][0 타일셋 · 1 장식][칸] (에셋이라 판끼리 함께 씀 · 사막은 Init 마다)

    // ---------------------------------------------------------------- 얼룩 · 칠하기 버퍼 (Init 에서 한 번)
    const int BlobMax = 3200;
    readonly int[] blobCells = new int[BlobMax];
    readonly float[] blobDist = new float[BlobMax];
    int blobCount, blobDone, blobRealm;
    readonly float[] blobRad = new float[16];
    Vector3 blobC; float blobR;
    readonly int[] newDecorCell = new int[DecorPerRealm];
    readonly byte[] newDecorKind = new byte[DecorPerRealm];
    int newDecorCount;
    readonly TileChangeData[][] staging = { new TileChangeData[512], new TileChangeData[512], new TileChangeData[512] };
    readonly int[] stagingCount = new int[3];
    static readonly int[] BucketSize = { 8, 16, 32, 64, 128, 256, 512 };
    static TileChangeData[][] buckets;

    // ---------------------------------------------------------------- 그 밖
    static int lastKind = -1;
    static Vector3 lastAt;
    readonly System.Random fxRng = new System.Random(5113);       // 연출용 (UnityEngine.Random 전역 상태를 건드리지 않게)
    struct Sinking { public Transform t; public float at, y0; public bool started; }
    readonly List<Sinking> sinking = new List<Sinking>();
    Vector3 stuckPos; float stuckTime;
    Collider2D playerBody; PlayerController bodyOf;
    static readonly Collider2D[] nearBuf = new Collider2D[48];
    static ContactFilter2D allFilter = new ContactFilter2D { useTriggers = true };

    // ================================================================= 시작
    public void Init(GameObject map, Vector2 min, Vector2 max, Dictionary<string, Sprite> desert, int sortingLayer, int topOrder)
    {
        Instance = this;
        areaMin = min; areaMax = max;
        sorting = sortingLayer; order = topOrder;
        lastKind = -1;
        EnemySpawner sp = FindFirstObjectByType<EnemySpawner>();
        if (sp != null) gate = sp.bossSpawnPoint;
        if (buckets == null)
        {
            buckets = new TileChangeData[BucketSize.Length][];
            for (int i = 0; i < BucketSize.Length; i++) buckets[i] = new TileChangeData[BucketSize[i]];
        }

        // 세 층: 그리는 순서로 바닥 · 벽 · 장식
        Tilemap[] all = map.GetComponentsInChildren<Tilemap>(true);
        System.Array.Sort(all, (a, b) => OrderOf(a).CompareTo(OrderOf(b)));
        if (all.Length == 0) { enabled = false; return; }
        layers[0] = all[0];
        layers[2] = all.Length >= 2 ? all[all.Length - 1] : null;
        layers[1] = all.Length >= 3 ? all[1] : null;
        Vector3 zero = layers[0].GetCellCenterWorld(Vector3Int.zero);
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
        for (int l = 0; l < 3; l++)
        {
            if (layers[l] == null) continue;
            off[l] = layers[l].WorldToCell(zero);
            BoundsInt b = layers[l].cellBounds;
            off[l].z = TileZ(layers[l]);             // 씬 타일이 z 100 에 깔려 있음 (cellBounds 는 z 0 ~ 100 이라 zMin 0 · z 0 으로 읽으면 전부 빈칸)
            x0 = Mathf.Min(x0, b.xMin - off[l].x); y0 = Mathf.Min(y0, b.yMin - off[l].y);
            x1 = Mathf.Max(x1, b.xMax - off[l].x); y1 = Mathf.Max(y1, b.yMax - off[l].y);
        }
        ux = x0; uy = y0; uw = Mathf.Max(1, x1 - x0); uh = Mathf.Max(1, y1 - y0);
        int n = uw * uh;
        realmOf = new byte[n];
        decorFlag = new bool[n];
        Dictionary<Sprite, byte> known = new Dictionary<Sprite, byte>();
        for (int l = 0; l < 3; l++)
        {
            byte[] s = slot[l] = new byte[n];
            Tilemap tm = layers[l];
            for (int i = 0; i < n; i++)
            {
                s[i] = Empty;
                if (tm == null) continue;
                Sprite sprite = tm.GetSprite(CellOf(i, l));
                if (sprite == null) continue;
                if (!known.TryGetValue(sprite, out byte k)) known[sprite] = k = SlotOf(sprite.name, l == 2 ? "objects_" : "tileset_");
                s[i] = k;
            }
        }

        Transform p = map.transform.Find("DesertProps");
        props = p;
        GameObject root = new GameObject("TerrainPieces");
        root.transform.SetParent(map.transform, false);
        pieceRoot = root.transform;
        art[Desert] = ArtFrom(desert != null ? desert.Values : null, null);
    }

    // 타일이 실제로 깔린 z (위에서부터 찾음 · 씬은 z 100) — cellBounds 의 zMin 은 원점 0 이라 쓸 수 없음 · CompressBounds 는 다른 곳이 쓰는 범위를 바꿀 수 있어 안 씀
    static int TileZ(Tilemap tm)
    {
        if (tm == null) return 0;
        BoundsInt b = tm.cellBounds;
        for (int z = b.zMax - 1; z >= b.zMin; z--)
            for (int y = b.yMin; y < b.yMax; y++)
                for (int x = b.xMin; x < b.xMax; x++)
                    if (tm.HasTile(new Vector3Int(x, y, z))) return z;
        return 0;
    }

    static int OrderOf(Tilemap tm) => tm.TryGetComponent(out TilemapRenderer r) ? r.sortingOrder : 0;

    static byte SlotOf(string name, string prefix)
    {
        if (!name.StartsWith(prefix)) return Empty;
        int i = name.LastIndexOf('_');
        return i >= 0 && int.TryParse(name.Substring(i + 1), out int k) && k >= 0 && k < MaxSlot ? (byte)k : Empty;
    }

    // 이번 판의 시드 (EndlessMode.Init · 이어하기는 Restore)
    public void Begin(int runSeed)
    {
        ClearPieces();
        seed = runSeed != 0 ? runSeed : 1;
        stage = entries = 0;
        final = beyond = false;
        log.Length = 0;
        pendings.Clear();
        ready = true;
    }

    public static int NewSeed() => new System.Random().Next(1, int.MaxValue);

    // bosss.Death: 쓰러진 보스 종류(-1 = 개발자 · 엄마) · 자리 — 바로 뒤 같은 호출 안에서 OnBossDefeated 가 읽고 비움
    public static void NoteBossDown(int kind, Vector3 at)
    {
        lastKind = kind;
        lastAt = at;
    }

    // ================================================================= 판 흐름
    public void OnBossDefeated(int bossesDefeated, int finalStage)
    {
        int kind = lastKind;
        lastKind = -1;               // 1.2.3~ 한 번 쓰면 비움 (다음 처치가 앞 보스 기록을 다시 쓰지 않게)
        if (!ready || (finalStage >= 1 && finalStage <= 3) || (final && !beyond)) return;      // 최종 국면 · 개발자 · 엄마 · 클리어 연출: 지형은 그대로
        if (kind < 0 || kind > 3) return;
        Pending p = new Pending { realm = kind + 1, c = ClampCenter(lastAt), r = finalStage == 4 ? RadiusBeyond : stage + pendings.Count + 1 <= 4 ? RadiusEarly : RadiusLate };
        pendings.Add(p);
        StartCoroutine(Live(p));
    }

    // 15분 최종 국면 (EndlessMode.StartFinal): 칠한 영토는 그대로, 조각만 모두 꺼짐
    public void OnFinalBegin()
    {
        if (final) return;
        final = true;
        if (!ready) return;          // 이어하기 중 (Restore 가 EndlessMode 상태에 맞춤)
        pendings.Clear();            // 15분 직전에 쓰러진 왕의 영토는 번지지 않음 (Live 가 보고 그만둠 · 저장에도 안 적힘)
        Append("f");
        int alive = 0;
        foreach (TerrainPiece piece in pieces) if (piece != null && piece.Alive) alive++;
        int i = 0;
        foreach (TerrainPiece piece in pieces)
        {
            if (piece == null || !piece.Alive) continue;
            piece.Sink(alive <= 1 ? 0f : 1.5f * i / (alive - 1), false);
            i++;
        }
        if (alive > 0) Hostile.Play("whoosh", 0.8f, 0.7f);
        Notice.Toast(NoticeIcon.Warn, Loc.T("모래 폭풍이 지형을 집어삼켰다"), 3f);
    }

    // 클리어 뒤 한계 돌파 (EndlessMode.ClearEndless): 처치마다 다시 영토가 번짐
    public void OnBeyond()
    {
        if (!ready || beyond) return;
        beyond = true;
        Append("B");
    }

    IEnumerator Live(Pending p)
    {
        // 그림은 처치 연출 동안 미리 모음 (파동 첫 프레임에 몰리지 않게 · 한 프레임에 하나)
        Art(p.realm);
        yield return null;
        // 처치 연출(영혼 소용돌이) · 진화 창 · 다른 보스의 결계가 끝난 뒤에
        while (PlayerController.Cinematic || Time.timeScale == 0f || busy || BossUltimate.Active) yield return null;
        if (!pendings.Remove(p)) yield break;        // 기다리는 동안 최종 국면이 시작됨 (OnFinalBegin 이 비움) · 판이 다시 시작됨
        if (final && !beyond) yield break;
        Vector3[] spots = Prepare(p.realm, p.c, p.r, null, null, false);
        PlayerController player = Hostile.Player;
        bool alive = player != null && !player.IsDying;
        busy = true;
        Color color = Accent(p.realm);
        float reach = blobR * 1.15f;                 // 파동이 칠하는 끝 (얼룩의 가장 먼 칸)
        Hostile.Play("boom", 0.6f, 0.55f);
        Hostile.Play("shimmer", 0.7f, 0.8f);
        Hostile.Shake(0.3f);
        ShockRing.Spawn(p.c, 1f, reach, reach / WaveSpeed, color, 0.6f);
        if (GameSettings.Flashes && Hostile.Glow != null)
            FadeSprite.Spawn("TerrainFlash", Hostile.Glow, p.c, p.r * 2f / 8f, new Color(color.r, color.g, color.b, 0.35f), "Effect", 2, 0.4f);
        float risen = RaisePieces(p.realm, spots, false);
        float w = 0f;
        while (blobDone < blobCount)
        {
            w += WaveSpeed * Time.deltaTime;
            PaintUpTo(w, PerFrame);
            if (Hostile.Glow != null && Time.deltaTime > 0f)
                for (int i = 0; i < 4; i++)
                {
                    float a = (float)(fxRng.NextDouble() * Mathf.PI * 2.0);
                    float rr = Mathf.Min(w, blobR * (0.85f + 0.3f * Noise(a)));
                    Vector3 at = blobC + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * rr;
                    FlameParticle.Spawn(Hostile.Glow, at, new Vector2((float)(fxRng.NextDouble() - 0.5), 1f + (float)fxRng.NextDouble()), 0.5f, 0.04f, 0.10f, true);
                }
            yield return null;
        }
        UpdateBackground();
        while (Time.time < risen) yield return null;        // 알림은 마지막 조각이 다 솟은 뒤
        busy = false;
        if (!alive || player == null || player.IsDying) yield break;
        if (final && !beyond) yield break;                 // 파동 도중에 최종 국면이 시작됨: 「15분을 버텼다」 알림과 겹치지 않게
        string title = Loc.T("{0}의 영토가 사막을 침식한다!").Replace("{0}", bossbar.BossName(p.realm - 1));
        if (spots.Length > 0 && PiecesAllowed) Notice.Big(title, Loc.T(Subtitle[p.realm]), NoticeIcon.Boss, 3.5f, NoticeLevel.Important, color);
        else Notice.Big(title, NoticeIcon.Boss, 3.5f, NoticeLevel.Important, color);
        Hints.Show("terrain", "보스를 쓰러뜨리면 그 왕의 영토가 쓰러진 자리에서 퍼집니다. 묘비 · 수정은 길과 탄을 막고, 용암은 밟으면 아프며, 점액은 모두를 느리게 합니다.");
    }

    bool PiecesAllowed => !final || beyond;

    Vector3 ClampCenter(Vector3 at) => new Vector3(Mathf.Clamp(at.x, areaMin.x + CenterInset, areaMax.x - CenterInset), Mathf.Clamp(at.y, areaMin.y + CenterInset, areaMax.y - CenterInset), 0f);

    // 한 단계: 얼룩 · 조각 자리 · 추가 장식 · 기록 · 덮인 웅덩이 · 묻힐 소품 (칠하기는 Live 의 파동 · 즉시 적용이 함)
    // spots: 기록대로 놓을 조각 자리 (null = 새로 뽑음) · decor: 기록대로 놓을 추가 장식 "칸.종류,…" (null = 새로 뽑음)
    Vector3[] Prepare(int realm, Vector3 c, float r, Vector3[] spots, string decor, bool instant)
    {
        Purge();
        stage++;
        entries++;
        int key = seed ^ (stage * 7919);
        Art(realm);                      // 처음 쓰는 영토면 원본 타일맵 · Resources 에서 그림을 모음 (영토당 한 번 · 판끼리 함께 씀)
        BuildBlob(realm, c, r, new System.Random(key));
        PlayerController player = Hostile.Player;
        bool alive = instant || (player != null && !player.IsDying);
        if (spots == null) spots = PiecesAllowed && alive ? PickSpots(new System.Random(key * 31 + 17), realm) : new Vector3[0];
        if (decor == null || !ReadDecor(decor, realm)) AddDecor(realm, new System.Random(key * 13 + 5), spots, player);

        // 기록: "k@x,y,R|px,py|px,py|d칸.종류,칸.종류"
        StringBuilder e = new StringBuilder();
        e.Append(realm - 1).Append('@').Append(F(c.x)).Append(',').Append(F(c.y)).Append(',').Append(F(r));
        foreach (Vector3 s in spots) e.Append('|').Append(F(s.x)).Append(',').Append(F(s.y));
        e.Append("|d");
        for (int d = 0; d < newDecorCount; d++) { if (d > 0) e.Append(','); e.Append(newDecorCell[d]).Append('.').Append(newDecorKind[d]); }
        Append(e.ToString());

        // 새 얼룩에 덮인 웅덩이 · 늪은 마름 (원의 어느 곳이든 걸치면 · 장애물은 상한 · 보스 · 15분으로만)
        for (int pi = 0; pi < pieces.Count; pi++)
        {
            TerrainPiece piece = pieces[pi];
            if (piece != null && piece.Alive && piece.zone && piece is TerrainZone z && CircleInBlob(piece.transform.position, z.lava ? LavaRim : BogCore))
                piece.Sink(instant ? 0f : Mathf.Max(0f, Vector2.Distance(piece.transform.position, c) - (z.lava ? LavaRim : BogCore)) / WaveSpeed, instant);
        }
        BuryProps(instant);
        return spots;
    }

    // 기록에 적힌 추가 장식을 그대로 (맵이 같으니 칸 번호도 같음)
    bool ReadDecor(string decor, int realm)
    {
        newDecorCount = 0;
        if (DecorKinds[realm].Length == 0 || layers[2] == null) return true;
        if (decor.Length == 0) return true;
        foreach (string pair in decor.Split(','))
        {
            if (newDecorCount >= DecorPerRealm) break;
            int dot = pair.IndexOf('.');
            if (dot <= 0 || !int.TryParse(pair.Substring(0, dot), out int i) || !byte.TryParse(pair.Substring(dot + 1), out byte k)) continue;
            if (i < 0 || i >= realmOf.Length || k >= MaxSlot || slot[0][i] == Empty) continue;
            newDecorCell[newDecorCount] = i;
            newDecorKind[newDecorCount] = k;
            newDecorCount++;
        }
        return true;
    }

    // 웅덩이 원(반지름 rad)이 얼룩에 조금이라도 걸치는지 (중심 + 둘레 두 겹을 봄)
    bool CircleInBlob(Vector3 c, float rad)
    {
        if (Vector2.Distance(c, blobC) > blobR * 1.15f + rad) return false;
        if (InBlob(c)) return true;
        for (int ring = 1; ring <= 2; ring++)
        {
            float rr = rad * ring * 0.5f;
            for (int i = 0; i < 16; i++)
            {
                float a = i * (Mathf.PI / 8f);
                if (InBlob(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * rr)) return true;
            }
        }
        return false;
    }

    static string F(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    void Append(string entry)
    {
        if (log.Length > 0) log.Append(';');
        log.Append(entry);
    }

    // ================================================================= 얼룩
    float Noise(float angle)
    {
        float t = Mathf.Repeat(angle / (Mathf.PI * 2f), 1f) * 16f;
        int i = Mathf.FloorToInt(t) & 15;
        return Mathf.Lerp(blobRad[i], blobRad[(i + 1) & 15], t - Mathf.Floor(t));
    }

    bool InBlob(Vector3 p)
    {
        Vector2 d = p - blobC;
        float dist = d.magnitude;
        if (dist > blobR * 1.15f) return false;
        return dist < blobR * (0.85f + 0.3f * Noise(Mathf.Atan2(d.y, d.x)));
    }

    void BuildBlob(int realm, Vector3 c, float r, System.Random rng)
    {
        blobC = c; blobR = r; blobRealm = realm;
        for (int i = 0; i < 16; i++) blobRad[i] = (float)rng.NextDouble();
        blobCount = blobDone = 0;
        float ext = r * 1.15f + 1f;
        Vector3Int cc = layers[0].WorldToCell(c);
        int xa = Mathf.Max(ux, cc.x - Mathf.CeilToInt(ext)), xb = Mathf.Min(ux + uw - 1, cc.x + Mathf.CeilToInt(ext));
        int ya = Mathf.Max(uy, cc.y - Mathf.CeilToInt(ext)), yb = Mathf.Min(uy + uh - 1, cc.y + Mathf.CeilToInt(ext));
        for (int y = ya; y <= yb; y++)
            for (int x = xa; x <= xb && blobCount < BlobMax; x++)
            {
                Vector3 p = layers[0].GetCellCenterWorld(new Vector3Int(x, y, 0));
                if (!InBlob(p) || Gated(p)) continue;
                int i = (x - ux) + (y - uy) * uw;
                if (slot[0][i] == Empty && slot[1][i] == Empty && slot[2][i] == Empty) continue;
                blobCells[blobCount] = i;
                blobDist[blobCount] = Vector2.Distance(p, c);
                blobCount++;
            }
        System.Array.Sort(blobDist, blobCells, 0, blobCount);
    }

    // 보스가 나오는 성문과 그 둘레는 어느 층도 칠하지 않음
    bool Gated(Vector3 p) => p.y >= gate.y - 1f && Mathf.Abs(p.x - gate.x) <= 4f;

    Vector3Int CellOf(int i, int layer) => new Vector3Int(ux + i % uw + off[layer].x, uy + i / uw + off[layer].y, off[layer].z);
    Vector3 WorldOf(int i) => layers[0].GetCellCenterWorld(new Vector3Int(ux + i % uw, uy + i / uw, 0));
    int IndexOf(Vector3 world)
    {
        Vector3Int c = layers[0].WorldToCell(world);
        int x = c.x - ux, y = c.y - uy;
        return x < 0 || y < 0 || x >= uw || y >= uh ? -1 : x + y * uw;
    }

    // 파동 반지름 w 안의 얼룩 칸을 층마다 모아 SetTiles 한 번씩 (limit: 이번 프레임 칸 수 상한)
    void PaintUpTo(float w, int limit)
    {
        int done = 0;
        Sprite[][] a = art[blobRealm];
        Color tint = blobRealm == Meadow ? MeadowTint : Color.white;
        while (blobDone < blobCount && blobDist[blobDone] <= w)
        {
            if (done >= limit) break;
            int i = blobCells[blobDone++];
            done++;
            realmOf[i] = (byte)blobRealm;            // 벽 · 장식만 있는 칸도 (접은 기록이 그대로 되살리게)
            if (slot[0][i] != Empty)
            {
                Sprite s = Pick(a[0], slot[0][i], true);
                if (s != null) Stage(0, i, s, tint);
            }
            if (slot[1][i] != Empty)
            {
                Sprite s = Pick(a[0], slot[1][i], false);
                if (s != null) Stage(1, i, s, tint);
            }
            byte dk = slot[2][i];
            for (int d = 0; d < newDecorCount; d++)
                if (newDecorCell[d] == i) { dk = newDecorKind[d]; AddedDecor(i, dk); break; }
            if (dk != Empty && dk != 0)
            {
                Sprite s = Pick(a[1], dk, false);
                if (s != null) { Stage(2, i, s, tint); slot[2][i] = dk; }
                else if (decorFlag[i]) { Stage(2, i, null, Color.white); slot[2][i] = Empty; RemoveAdded(i); }
            }
            if (stagingCount[0] >= 500 || stagingCount[1] >= 500 || stagingCount[2] >= 500) Flush();
        }
        Flush();
    }

    // 칸 k 그림, 없으면 (바닥만) 0번 칸 · 그것도 없으면 그대로 둠
    static Sprite Pick(Sprite[] from, byte k, bool fallback)
    {
        if (from == null) return null;
        Sprite s = k < from.Length ? from[k] : null;
        return s != null || !fallback ? s : from[0];
    }

    void Stage(int l, int i, Sprite s, Color color)
    {
        Tilemap tm = layers[l];
        if (tm == null || stagingCount[l] >= staging[l].Length) return;
        Vector3Int cell = CellOf(i, l);
        staging[l][stagingCount[l]++] = new TileChangeData(cell, s != null ? TileFor(s) : null, color, tm.GetTransformMatrix(cell));
    }

    // SetTiles 는 배열 전체를 쓰므로 미리 잡은 고정 크기 배열(8~512)로 나눠 보냄: 남은 칸 수 이하의 가장 큰 배열부터
    //   채움 칸(마지막 값 되풀이 · 같은 칸 같은 값)은 마지막 8칸 묶음에서만 생김
    void Flush()
    {
        for (int l = 0; l < 3; l++)
        {
            int n = stagingCount[l];
            if (n == 0) continue;
            stagingCount[l] = 0;
            int from = 0;
            while (from < n)
            {
                int left = n - from;
                int b = BucketSize.Length - 1;
                while (b > 0 && BucketSize[b] > left) b--;
                TileChangeData[] arr = buckets[b];
                int m = Mathf.Min(left, arr.Length);
                System.Array.Copy(staging[l], from, arr, 0, m);
                for (int k = m; k < arr.Length; k++) arr[k] = arr[m - 1];
                layers[l].SetTiles(arr, true);
                from += m;
            }
        }
    }

    // 최적화: EndlessMode.ReskinHellMap 도 이 캐시를 같이 씀 (판마다 같은 그림의 Tile 을 새로 만들지 않게)
    internal static Tile TileFor(Sprite s)
    {
        if (tileCache.TryGetValue(s, out Tile t) && t != null) return t;
        t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = s;
        t.flags = TileFlags.None;
        t.colliderType = Tile.ColliderType.None;
        t.hideFlags = HideFlags.DontUnloadUnusedAsset;
        tileCache[s] = t;
        return t;
    }

    // ================================================================= 영토 그림 (처음 쓸 때 원본 타일맵 · Resources 에서 · 정적이라 판마다 다시 모으지 않음)
    Sprite[][] Art(int realm)
    {
        if (art[realm] != null) return art[realm];
        StageManager sm = StageManager.Instance;
        List<Tilemap> src = new List<Tilemap>();
        switch (realm)
        {
            case Desert: return art[realm] = ArtFrom(Resources.LoadAll<Sprite>("Desert"), null);
            case Abyss: return art[realm] = ArtFrom(Resources.LoadAll<Sprite>("Abyss"), null);
            case Hell:
                if (sm != null && sm.hellMap != null) src.AddRange(sm.hellMap.GetComponentsInChildren<Tilemap>(true));
                break;
            case Meadow:
                if (sm != null && sm.meadowMap != null) src.AddRange(sm.meadowMap.GetComponentsInChildren<Tilemap>(true));
                break;
            case Cave:
                if (sm != null && sm.caveRenderers != null)
                    foreach (Renderer r in sm.caveRenderers)
                        if (r != null && r.TryGetComponent(out Tilemap tm) && !src.Contains(tm)) src.Add(tm);
                if (src.Count < 3 && sm != null && sm.hellMap != null && sm.hellMap.transform.parent != null)
                    foreach (string n in new[] { "Floor", "Wall", "Object" })
                    {
                        Transform t = sm.hellMap.transform.parent.Find(n);
                        if (t != null && t.TryGetComponent(out Tilemap tm) && !src.Contains(tm)) src.Add(tm);
                    }
                break;
        }
        Sprite[][] found = ArtFrom(null, src);
        if (src.Count > 0) art[realm] = found;       // 원본 맵을 못 찾았으면 다음에 다시 찾음 (빈 그림을 판끼리 남기지 않게)
        return found;
    }

    static Sprite[][] ArtFrom(IEnumerable<Sprite> sprites, List<Tilemap> maps)
    {
        Sprite[][] a = { new Sprite[MaxSlot], new Sprite[MaxSlot] };
        void Add(Sprite s)
        {
            byte k = SlotOf(s.name, "tileset_");
            if (k != Empty) { if (a[0][k] == null) a[0][k] = s; return; }
            k = SlotOf(s.name, "objects_");
            if (k != Empty && a[1][k] == null) a[1][k] = s;
        }
        if (sprites != null) foreach (Sprite s in sprites) if (s != null) Add(s);
        if (maps != null)
        {
            HashSet<Sprite> seen = new HashSet<Sprite>();
            foreach (Tilemap tm in maps)
            {
                BoundsInt b = tm.cellBounds;
                int z = TileZ(tm);
                for (int y = b.yMin; y < b.yMax; y++)
                    for (int x = b.xMin; x < b.xMax; x++)
                    {
                        Sprite s = tm.GetSprite(new Vector3Int(x, y, z));
                        if (s != null && seen.Add(s)) Add(s);
                    }
            }
        }
        return a;
    }

    // ================================================================= 추가 장식 · 소품
    void AddDecor(int realm, System.Random rng, Vector3[] spots, PlayerController player)
    {
        newDecorCount = 0;
        int[] kinds = DecorKinds[realm];
        if (kinds.Length == 0 || blobCount == 0 || layers[2] == null) return;
        for (int tries = 0; tries < DecorPerRealm * 12 && newDecorCount < DecorPerRealm; tries++)
        {
            int i = blobCells[rng.Next(blobCount)];
            byte k = (byte)kinds[rng.Next(kinds.Length)];
            if (slot[2][i] != Empty || slot[0][i] == Empty) continue;
            Vector3 p = WorldOf(i);
            if (player != null && Vector2.Distance(p, player.transform.position) < 3f) continue;
            bool near = false;
            foreach (Vector3 s in spots) if (Vector2.Distance(p, s) < 2f) { near = true; break; }
            for (int d = 0; d < newDecorCount && !near; d++) if (Vector2.Distance(p, WorldOf(newDecorCell[d])) < 3f) near = true;
            if (near || Pick(art[realm][1], k, false) == null) continue;
            newDecorCell[newDecorCount] = i;
            newDecorKind[newDecorCount] = k;
            newDecorCount++;
        }
    }

    void AddedDecor(int i, byte k)
    {
        if (decorFlag[i]) return;
        decorFlag[i] = true;
        decorAdded.Add(i);
        // 상한: 오래된 것부터 지움
        while (decorAdded.Count > MaxDecor)
        {
            int old = decorAdded[0];
            decorAdded.RemoveAt(0);
            decorFlag[old] = false;
            slot[2][old] = Empty;
            Stage(2, old, null, Color.white);
        }
    }

    void RemoveAdded(int i)
    {
        decorFlag[i] = false;
        decorAdded.Remove(i);
    }

    // 얼룩 안의 사막 소품은 땅에 꺼짐 (되살리지 않음 · 영토 장식이 대신 채움)
    void BuryProps(bool instant)
    {
        if (props == null) return;
        for (int i = 0; i < props.childCount; i++)
        {
            Transform t = props.GetChild(i);
            if (!t.gameObject.activeSelf || !InBlob(t.position)) continue;
            bool queued = false;
            foreach (Sinking s in sinking) if (s.t == t) { queued = true; break; }
            if (queued) continue;
            if (t.TryGetComponent(out FireFlicker ff)) ff.enabled = false;
            if (instant) { t.gameObject.SetActive(false); continue; }
            sinking.Add(new Sinking { t = t, at = Time.time + Vector2.Distance(t.position, blobC) / WaveSpeed, y0 = t.localScale.y });
        }
    }

    void Update()
    {
        // 소품 꺼짐 (0.4초)
        for (int i = sinking.Count - 1; i >= 0; i--)
        {
            Sinking s = sinking[i];
            if (s.t == null) { sinking.RemoveAt(i); continue; }
            if (Time.time < s.at) continue;
            if (!s.started)
            {
                s.started = true;
                s.y0 = s.t.localScale.y;
                sinking[i] = s;
                Fx.Spawn("fx_smoke", s.t.position, 1.6f, Gust(blobRealm), 18f);
            }
            float k = Mathf.Clamp01((Time.time - s.at) / 0.4f);
            Vector3 sc = s.t.localScale;
            s.t.localScale = new Vector3(sc.x, s.y0 * (1f - k), sc.z);
            if (k >= 1f) { s.t.gameObject.SetActive(false); sinking.RemoveAt(i); }
        }
        CheckStuck();
    }

    // 끼임 방지: 움직이려는데 1초 동안 0.05칸도 못 움직였고 몸이 장애물에 닿아 있으면, 닿은 장애물과 2.8칸 안의 장애물을 부숨
    //   필살기 조준 중(제자리)은 세지 않음 · 닿아 있지 않으면 부수지 않음 (몸을 대고 버티는 것만으로는 안 부서지게 · 둘 이상에 끼였을 때만)
    void CheckStuck()
    {
        PlayerController p = Hostile.Player;
        if (p == null || pieces.Count == 0 || Time.timeScale == 0f) return;
        Purge();
        Vector3 pos = p.transform.position;
        if (p.IsDying || p.IsAiming || PlayerController.Cinematic || p.MoveInput.sqrMagnitude < 0.01f || (pos - stuckPos).sqrMagnitude > 0.0025f)
        {
            stuckPos = pos;
            stuckTime = 0f;
            return;
        }
        stuckTime += Time.deltaTime;
        if (stuckTime < 1f) return;
        stuckTime = 0f;
        Collider2D body = Body(p);
        if (body == null) return;
        int touching = 0;
        foreach (TerrainPiece piece in pieces)
            if (piece != null && piece.Alive && piece is TerrainObstacle o && o.Touches(body)) touching++;
        // 장애물 하나에만 닿았으면 다른 쪽(벽 · 맵 끝)도 막혔는지 봄
        if (touching == 0 || (touching == 1 && !OtherWallNear(pos, 1.2f))) return;
        foreach (TerrainPiece piece in pieces)
            if (piece != null && piece.Alive && !piece.zone && Vector2.Distance(piece.transform.position, pos) < 2.8f) piece.Break();
    }

    // 지형 장애물이 아닌 벽 (맵 구조물 · 테두리)
    static bool OtherWallNear(Vector3 pos, float radius)
    {
        int n = Physics2D.OverlapCircle(pos, radius, allFilter, nearBuf);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = nearBuf[i];
            if (c != null && !c.isTrigger && c.CompareTag("Wall") && !c.TryGetComponent(out TerrainObstacle _)) return true;
        }
        return false;
    }

    Collider2D Body(PlayerController p)
    {
        if (bodyOf == p && playerBody != null) return playerBody;
        bodyOf = p;
        playerBody = null;
        foreach (Collider2D c in p.GetComponents<Collider2D>()) if (c.enabled && !c.isTrigger) { playerBody = c; break; }
        return playerBody;
    }

    // ================================================================= 조각
    Vector3[] PickSpots(System.Random rng, int realm)
    {
        PlayerController player = Hostile.Player;
        HPpack[] packs = HPpack.Live.ToArray();      // 1.4.1~ 씬 전체 검색 대신 목록
        ShopStall[] stalls = FindObjectsByType<ShopStall>(FindObjectsSortMode.None);
        List<Vector3> found = new List<Vector3>(PiecesPerRealm);
        for (int j = 0; j < PiecesPerRealm; j++)
            for (int tries = 0; tries < SpotTries; tries++)
            {
                float th = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rr = Mathf.Lerp(4f, Mathf.Max(4f, blobR - 3f), (float)rng.NextDouble());
                Vector3 at = blobC + new Vector3(Mathf.Cos(th), Mathf.Sin(th)) * rr;
                if (SpotOk(at, player, packs, stalls, found)) { found.Add(at); break; }
            }
        return found.ToArray();
    }

    bool SpotOk(Vector3 at, PlayerController player, HPpack[] packs, ShopStall[] stalls, List<Vector3> found)
    {
        if (!Hostile.InArena(at) || at.x < areaMin.x + ClearEdge || at.x > areaMax.x - ClearEdge || at.y < areaMin.y + ClearEdge || at.y > areaMax.y - ClearEdge) return false;
        if (player != null && Vector2.Distance(at, player.transform.position) < ClearPlayer) return false;
        foreach (TerrainPiece piece in pieces) if (piece != null && piece.Alive && Vector2.Distance(at, piece.transform.position) < ClearPiece) return false;
        foreach (Vector3 f in found) if (Vector2.Distance(at, f) < ClearPiece) return false;
        if (Vector2.Distance(at, gate) < ClearGate || (at.x >= gate.x - 7.5f && at.x <= gate.x + 7.5f && at.y >= 20f)) return false;
        if (Hostile.WallNear(at, 2.5f)) return false;
        int n = Physics2D.OverlapCircle(at, ClearEnemy, allFilter, nearBuf);
        for (int i = 0; i < n; i++) if (nearBuf[i] != null && nearBuf[i].CompareTag("enermy")) return false;
        foreach (HPpack h in packs) if (h != null && Vector2.Distance(at, h.transform.position) < 2.5f) return false;
        foreach (ShopStall s in stalls) if (s != null && Vector2.Distance(at, s.transform.position) < 3f) return false;
        if (!InBlob(at)) return false;
        int idx = IndexOf(at);
        return idx >= 0 && slot[0][idx] != Empty;
    }

    // 조각을 솟게 함 · 돌려주는 값: 마지막 조각이 다 솟는 시각 (Live 가 알림을 그 뒤로 미룸)
    float RaisePieces(int realm, Vector3[] spots, bool instant)
    {
        float end = Time.time;
        if (!PiecesAllowed) return end;
        bool zone = realm == Hell || realm == Meadow;
        for (int j = 0; j < spots.Length; j++)
        {
            float delay = instant ? 0f : Vector2.Distance(spots[j], blobC) / WaveSpeed + 0.2f + 0.15f * j;
            Trim(zone, instant ? 0f : Mathf.Max(0f, delay - 0.2f), instant);
            if (SpawnPiece(realm, spots[j], stage, j, delay, instant)) end = Mathf.Max(end, Time.time + delay + 0.3f);
        }
        return end;
    }

    bool SpawnPiece(int realm, Vector3 at, int entry, int index, float delay, bool instant)
    {
        bool zone = realm == Hell || realm == Meadow;
        Sprite look = zone ? null : Pick(Art(realm)[1], (byte)(realm == Cave ? 5 : 4), false);
        if (!zone && look == null) return false;          // 그림이 없으면 보이지 않는 벽이 되므로 만들지 않음
        GameObject go = new GameObject(zone ? (realm == Hell ? "Terrain_Lava" : "Terrain_Bog") : (realm == Cave ? "Terrain_Tomb" : "Terrain_Crystal"));
        go.transform.SetParent(pieceRoot, false);
        go.transform.position = at;
        TerrainPiece piece;
        if (zone)
        {
            TerrainZone z = go.AddComponent<TerrainZone>();
            z.lava = realm == Hell;
            piece = z;
        }
        else
        {
            TerrainObstacle o = go.AddComponent<TerrainObstacle>();
            o.Build(look, sorting, order + 2);
            piece = o;
        }
        piece.owner = this;
        piece.realm = realm;
        piece.entry = entry;
        piece.index = index;
        piece.zone = zone;
        pieces.Add(piece);
        piece.Rise(delay, instant);
        return true;
    }

    // 상한: 장애물은 장애물끼리, 웅덩이 · 늪은 둘을 합쳐 가장 오래된 것부터
    void Trim(bool zone, float delay, bool instant)
    {
        int max = zone ? MaxZones : MaxObstacles;
        int count = 0;
        foreach (TerrainPiece p in pieces) if (p != null && p.Alive && p.zone == zone) count++;
        for (int i = 0; i < pieces.Count && count >= max; i++)
        {
            TerrainPiece p = pieces[i];
            if (p == null || !p.Alive || p.zone != zone) continue;
            p.Sink(delay, instant);
            count--;
        }
    }

    // 조각이 부서짐 (보스 접촉 · 끼임 방지): 이어하면 다시 솟지 않게 기록
    public void NoteBroken(TerrainPiece piece)
    {
        if (ready) Append("x@" + piece.entry + "." + piece.index);
    }

    // 없어진 조각을 목록에서 뺌 (목록을 도는 중에는 빼지 않고 여기서만)
    void Purge() => pieces.RemoveAll(p => p == null || p.Gone);

    // 살아 있는 웅덩이 · 늪과 가까운지 (pad: 웅덩이 반지름 밖으로 더 띄울 거리) · 공간 침식 고요의 눈 자리 고를 때
    public static bool NearZone(Vector3 at, float pad)
    {
        EndlessTerrain t = Instance;
        if (t == null || !t.isActiveAndEnabled) return false;
        foreach (TerrainPiece piece in t.pieces)
        {
            if (piece == null || !piece.Alive || !piece.zone || !(piece is TerrainZone z)) continue;
            float r = (z.lava ? LavaRadius : BogRadius) + pad;
            if (((Vector2)(at - piece.transform.position)).sqrMagnitude < r * r) return true;
        }
        return false;
    }

    void ClearPieces()
    {
        for (int i = 0; i < pieces.Count; i++) if (pieces[i] != null && !pieces[i].Gone) pieces[i].Remove(true);
        pieces.Clear();
    }

    // 웅덩이 · 늪 바닥: 용암(중심 2.2칸) · 균열(3.0칸까지) / 초원 0번 + 초록 (2.6칸) · 마르면 칸의 영토 그림으로
    public void PaintZone(TerrainZone z, bool on)
    {
        Vector3 c = z.transform.position;
        float rim = z.lava ? LavaRim : BogCore;
        if (on && busy) PaintUpTo(Vector2.Distance(c, blobC) + rim + 1f, int.MaxValue);     // 파동이 아직 못 칠한 칸이 웅덩이를 덮지 않게
        Vector3Int cc = layers[0].WorldToCell(c);
        int ext = Mathf.CeilToInt(rim);
        Sprite lava = null, crack = null, bog = null;
        if (on && z.lava) { lava = Pick(Art(Hell)[0], 3, false); crack = Pick(Art(Hell)[0], 4, false); }
        else if (on) bog = Pick(Art(Meadow)[0], 0, false);
        for (int y = cc.y - ext; y <= cc.y + ext; y++)
            for (int x = cc.x - ext; x <= cc.x + ext; x++)
            {
                int i = (x - ux) + (y - uy) * uw;
                if (x < ux || y < uy || x >= ux + uw || y >= uy + uh || slot[0][i] == Empty) continue;
                Vector3 p = WorldOf(i);
                float d = Vector2.Distance(p, c);
                if (d > rim || Gated(p)) continue;
                if (on)
                {
                    Sprite s = z.lava ? (d < LavaCore ? lava : crack) : bog;
                    if (s != null) Stage(0, i, s, z.lava ? Color.white : BogTint);
                }
                else
                {
                    int realm = realmOf[i];
                    Sprite s = Pick(Art(realm)[0], slot[0][i], true);
                    if (s != null) Stage(0, i, s, realm == Meadow ? MeadowTint : Color.white);
                }
            }
        Flush();
    }

    // ================================================================= 배경 · 조회
    void UpdateBackground()
    {
        Camera cam = Camera.main;
        StageManager sm = StageManager.Instance;
        if (cam == null || sm == null || realmOf == null) return;
        int[] count = new int[5];
        int total = 0;
        for (int i = 0; i < realmOf.Length; i++)
            if (slot[0][i] != Empty) { count[Mathf.Min(4, (int)realmOf[i])]++; total++; }
        if (total == 0) return;
        Color[] bg = { sm.desertBackground, sm.CaveBackground, sm.hellBackground, sm.meadowBackground, AbyssStage.Background };
        Color mix = Color.black;
        for (int r = 0; r < 5; r++) mix += bg[r] * (count[r] / (float)total);
        mix.a = 1f;
        Color tone = VisualRules.FloorTone(0, true);       // 1단계-7~ 바닥 톤다운 (StageManager.PrepareEndless 와 같은 값)
        cam.backgroundColor = new Color(mix.r * tone.r, mix.g * tone.g, mix.b * tone.b, 1f);
    }

    public int RealmAt(Vector3 world)
    {
        if (realmOf == null) return Desert;
        int i = IndexOf(world);
        return i < 0 ? Desert : realmOf[i];
    }

    // ================================================================= 저장 · 이어하기 (RunSave.Data.terrainSeed · terrain)
    //   "k@x,y,R|px,py|px,py|d칸.종류,…"  k = 쓰러진 왕 0~3 · x,y = 얼룩 중심 · R = 반지름 · '|' 뒤는 솟은 조각 자리 ("|*" = 이어할 때 새로 뽑음)
    //     · "|d" 뒤는 더한 장식 (없으면 옛 기록: 이어할 때 새로 뽑음)
    //   "x@n.j"  n번째 항목의 j번째 조각이 부서짐 · "f" 15분 최종 국면 (조각 모두 꺼짐) · "B" 한계 돌파
    //   "S단계|칸 영토(RLE)|더한 장식|살아 있는 조각"  영토 항목이 CompactAt 개를 넘으면 기록을 한 장으로 접음 (한계 돌파에서 끝없이 늘지 않게)
    public void Save(out int runSeed, out string terrain)
    {
        runSeed = ready ? seed : 0;
        if (ready && !busy && entries > CompactAt) Compact();
        if (pendings.Count == 0) { terrain = log.ToString(); return; }
        // 처치 연출 · 진화 창 중에 저장: 아직 번지지 않은 영토도 적음 (조각 자리는 이어할 때 그때 상태로 다시 뽑음)
        StringBuilder sb = new StringBuilder(log.ToString());
        foreach (Pending p in pendings)
        {
            if (sb.Length > 0) sb.Append(';');
            sb.Append(p.realm - 1).Append('@').Append(F(p.c.x)).Append(',').Append(F(p.c.y)).Append(',').Append(F(p.r)).Append("|*");
        }
        terrain = sb.ToString();
    }

    // 지금 지형을 한 장으로: 영토 칸 · 더한 장식 · 살아 있는 조각 (부서진 기록 · 지나간 항목은 버림)
    void Compact()
    {
        Purge();
        StringBuilder sb = new StringBuilder();
        if (final) sb.Append("f;");
        if (beyond) sb.Append("B;");
        sb.Append('S').Append(stage).Append('|');
        int run = 0;
        byte cur = realmOf.Length > 0 ? realmOf[0] : (byte)0;
        for (int i = 0; i <= realmOf.Length; i++)
        {
            if (i < realmOf.Length && realmOf[i] == cur) { run++; continue; }
            sb.Append((char)('a' + Mathf.Min(4, (int)cur))).Append(run);
            if (i < realmOf.Length) { cur = realmOf[i]; run = 1; }
        }
        sb.Append('|');
        for (int d = 0; d < decorAdded.Count; d++)
        {
            int i = decorAdded[d];
            if (d > 0) sb.Append(',');
            sb.Append(i).Append('.').Append(slot[2][i]);
        }
        sb.Append('|');
        bool first = true;
        foreach (TerrainPiece piece in pieces)
        {
            if (piece == null || !piece.Alive) continue;
            if (!first) sb.Append(',');
            first = false;
            Vector3 at = piece.transform.position;
            sb.Append(piece.realm).Append('/').Append(piece.entry).Append('/').Append(piece.index).Append('/').Append(F(at.x)).Append('/').Append(F(at.y));
        }
        log.Length = 0;
        log.Append(sb);
        entries = 0;
    }

    // 맵 · EndlessMode 상태(경과 시간 · 최종 국면) · 플레이어 자리가 되살아난 뒤, 적이 나오기 전 (EndlessMode.RestoreRun 끝)
    public void Restore(int runSeed, string terrain)
    {
        bool wasFinal = final;
        if (runSeed == 0) { Begin(NewSeed()); Sync(wasFinal); return; }     // 옛 저장: 사막 그대로 시작
        Begin(runSeed);
        if (!string.IsNullOrEmpty(terrain))
            foreach (string raw in terrain.Split(';'))
            {
                string e = raw.Trim();
                if (e.Length == 0) continue;
                if (e == "f") { final = true; ClearPieces(); Append("f"); continue; }
                if (e == "B") { beyond = true; Append("B"); continue; }
                if (e[0] == 'S') { ApplySnapshot(e); continue; }
                if (e.StartsWith("x@"))
                {
                    string[] nj = e.Substring(2).Split('.');
                    if (nj.Length == 2 && int.TryParse(nj[0], out int n) && int.TryParse(nj[1], out int j))
                        for (int k = 0; k < pieces.Count; k++)
                        {
                            TerrainPiece p = pieces[k];
                            if (p != null && p.Alive && p.entry == n && p.index == j) { p.Remove(true); Append(e); break; }
                        }
                    continue;
                }
                ApplyEntry(e);
            }
        Sync(wasFinal);
        // 되살린 자리 바로 옆 장애물 · 용암은 조용히 뺌 (이어하자마자 막히거나 데지 않게)
        PlayerController player = Hostile.Player;
        if (player != null)
            for (int i = pieces.Count - 1; i >= 0; i--)
            {
                TerrainPiece p = pieces[i];
                if (p == null || !p.Alive) continue;
                float d = Vector2.Distance(p.transform.position, player.transform.position);
                bool lava = p is TerrainZone z && z.lava;
                if ((!p.zone && d < 2.5f) || (lava && d < LavaRadius + 1f))
                {
                    NoteBroken(p);
                    p.Remove(true);
                }
            }
        UpdateBackground();
    }

    void ApplyEntry(string e)
    {
        string[] parts = e.Split('|');
        int at = parts[0].IndexOf('@');
        if (at <= 0 || !int.TryParse(parts[0].Substring(0, at), out int kind) || kind < 0 || kind > 3) return;
        string[] xyr = parts[0].Substring(at + 1).Split(',');
        if (xyr.Length < 2 || !TryF(xyr[0], out float x) || !TryF(xyr[1], out float y)) return;
        float r = xyr.Length >= 3 && TryF(xyr[2], out float rr) ? rr : RadiusEarly;
        Vector3[] spots = null;
        string decor = null;
        bool pick = parts.Length > 1 && parts[1] == "*";
        List<Vector3> list = pick ? null : new List<Vector3>();
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0 && parts[i][0] == 'd') { decor = parts[i].Substring(1); continue; }
            if (list == null) continue;
            string[] p = parts[i].Split(',');
            if (p.Length == 2 && TryF(p[0], out float px) && TryF(p[1], out float py)) list.Add(new Vector3(px, py, 0f));
        }
        if (list != null) spots = list.ToArray();
        int realm = kind + 1;
        spots = Prepare(realm, new Vector3(x, y, 0f), r, spots, decor, true);
        PaintUpTo(float.MaxValue, int.MaxValue);
        RaisePieces(realm, spots, true);
    }

    // 접은 기록: 칸마다 영토를 그대로 칠하고, 더한 장식 · 조각을 되살림
    void ApplySnapshot(string e)
    {
        string[] parts = e.Substring(1).Split('|');
        if (parts.Length < 4 || !int.TryParse(parts[0], out int st)) return;
        // 칸 영토 (RLE: 영토 글자 a~e + 칸 수)
        byte[] rs = new byte[realmOf.Length];
        int n = 0;
        string rle = parts[1];
        for (int c = 0; c < rle.Length;)
        {
            int realm = rle[c] - 'a';
            int c2 = c + 1, run = 0;
            while (c2 < rle.Length && rle[c2] >= '0' && rle[c2] <= '9') { run = run * 10 + (rle[c2] - '0'); c2++; }
            if (realm < 0 || realm > 4 || run <= 0 || n + run > rs.Length) return;      // 맵이 달라졌으면 기록을 버리고 사막 그대로
            for (int k = 0; k < run; k++) rs[n++] = (byte)realm;
            c = c2;
        }
        if (n != rs.Length) return;
        stage = st;
        // 더한 장식 (오래된 순)
        if (parts[2].Length > 0)
            foreach (string pair in parts[2].Split(','))
            {
                int dot = pair.IndexOf('.');
                if (dot <= 0 || !int.TryParse(pair.Substring(0, dot), out int i) || !byte.TryParse(pair.Substring(dot + 1), out byte k)) continue;
                if (i < 0 || i >= rs.Length || k >= MaxSlot || decorFlag[i] || slot[0][i] == Empty || slot[2][i] != Empty) continue;
                slot[2][i] = k;
                decorFlag[i] = true;
                decorAdded.Add(i);
            }
        // 칠하기
        for (int i = 0; i < rs.Length; i++)
        {
            realmOf[i] = rs[i];
            if (rs[i] == Desert) continue;
            Sprite[][] a = Art(rs[i]);
            Color tint = rs[i] == Meadow ? MeadowTint : Color.white;
            if (slot[0][i] != Empty) { Sprite s = Pick(a[0], slot[0][i], true); if (s != null) Stage(0, i, s, tint); }
            if (slot[1][i] != Empty) { Sprite s = Pick(a[0], slot[1][i], false); if (s != null) Stage(1, i, s, tint); }
            byte dk = slot[2][i];
            if (dk != Empty && dk != 0)
            {
                Sprite s = Pick(a[1], dk, false);
                if (s != null) Stage(2, i, s, tint);
                else if (decorFlag[i]) { Stage(2, i, null, Color.white); slot[2][i] = Empty; RemoveAdded(i); }
            }
            if (stagingCount[0] >= 500 || stagingCount[1] >= 500 || stagingCount[2] >= 500) Flush();
        }
        Flush();
        // 영토에 묻힌 사막 소품
        if (props != null)
            for (int i = 0; i < props.childCount; i++)
            {
                Transform t = props.GetChild(i);
                if (!t.gameObject.activeSelf || RealmAt(t.position) == Desert) continue;
                if (t.TryGetComponent(out FireFlicker ff)) ff.enabled = false;
                t.gameObject.SetActive(false);
            }
        // 조각
        if (parts[3].Length > 0 && PiecesAllowed)
            foreach (string one in parts[3].Split(','))
            {
                string[] f = one.Split('/');
                if (f.Length != 5 || !int.TryParse(f[0], out int realm) || realm < Cave || realm > Abyss || !int.TryParse(f[1], out int entry) || !int.TryParse(f[2], out int index)
                    || !TryF(f[3], out float px) || !TryF(f[4], out float py)) continue;
                SpawnPiece(realm, new Vector3(px, py, 0f), entry, index, 0f, true);
            }
        if (log.Length > 0) log.Append(';');
        log.Append(e);
        entries = 0;
    }

    static bool TryF(string s, out float v) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    // EndlessMode 의 국면과 맞춤 (기록이 어긋나도 깨지지 않게)
    void Sync(bool wasFinal)
    {
        EndlessMode em = EndlessMode.Instance;
        bool fin = wasFinal || (em != null && em.FinalPhase);
        if (fin && !final) { final = true; ClearPieces(); Append("f"); }
        if (em != null && em.Beyond && !beyond) { beyond = true; Append("B"); }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

// ===================================================================== 지형 조각 공통: 솟기 · 꺼지기
public class TerrainPiece : MonoBehaviour
{
    [HideInInspector] public EndlessTerrain owner;
    [HideInInspector] public int realm, entry, index;
    [HideInInspector] public bool zone;
    protected SpriteRenderer sr;
    protected bool up;              // 다 솟음 (판정 켜짐)
    float riseAt = -1f, sinkAt = -1f, t;
    bool rising, sinking, quiet, smoked;

    // 아직 꺼지기로 하지 않았음
    public bool Alive => sinkAt < 0f && !Gone;
    public bool Gone { get; private set; }      // 지워짐 (EndlessTerrain.Purge 가 목록에서 뺌)

    public void Rise(float delay, bool instant)
    {
        if (instant) { SetHeight(1f); up = true; OnUp(true); return; }
        SetHeight(0f);
        riseAt = Time.time + delay;
        rising = true;
    }

    public void Sink(float delay, bool instant)
    {
        if (!Alive) return;
        if (instant) { Remove(true); return; }
        if (rising && !smoked) { Remove(true); return; }      // 아직 솟지도 않았으면 바로 없앰
        sinkAt = Time.time + delay;
    }

    // 보스 접촉 · 끼임 방지
    public void Break()
    {
        if (!Alive) return;
        if (owner != null) owner.NoteBroken(this);
        sinkAt = Time.time;
    }

    public void Remove(bool silent)
    {
        if (Gone) return;
        quiet = silent;
        if (up) OnDown();
        up = false;
        Gone = true;
        Destroy(gameObject);
    }

    protected virtual void SetHeight(float k) { }
    protected virtual void OnUp(bool instant) { }
    protected virtual void OnDown() { }
    protected virtual void Tick() { }

    protected void Smoke(float vol, float pitch)
    {
        Fx.Spawn("fx_smoke", transform.position, 2f, EndlessTerrain.Gust(realm), 18f);
        Hostile.Play("crack", vol, pitch);
    }

    protected virtual void Update()
    {
        if (rising)
        {
            if (Time.time < riseAt) return;
            if (!smoked) { smoked = true; Smoke(0.5f, 0.9f); }
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.3f);
            SetHeight(k);
            if (k >= 1f) { rising = false; up = true; t = 0f; OnUp(false); }
            if (sinkAt < 0f) return;
        }
        if (sinkAt >= 0f && Time.time >= sinkAt)
        {
            if (!sinking)
            {
                sinking = true;
                t = 0f;
                if (up) { OnDown(); up = false; }
                if (!quiet) Smoke(0.4f, 0.6f);
            }
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.5f);
            SetHeight(1f - k);
            if (k >= 1f) { Gone = true; Destroy(gameObject); }
            return;
        }
        if (up) Tick();
    }
}

// 묘비 · 영혼 수정: 길과 탄을 막는 엄폐물 (태그 Wall 상자 · 보스가 지나가면 부서짐)
public class TerrainObstacle : TerrainPiece
{
    BoxCollider2D box;
    float nextCheck;

    public void Build(Sprite sprite, int sortingLayer, int sortingOrder)
    {
        gameObject.tag = "Wall";
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = sortingLayer;
        sr.sortingOrder = sortingOrder;
        box = gameObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.6f);          // 2.4배 → 월드 약 1.9 × 1.4 (그림 윗부분은 막지 않음)
        box.offset = new Vector2(0f, -0.12f);
        box.enabled = false;
    }

    protected override void SetHeight(float k) => transform.localScale = new Vector3(EndlessTerrain.PieceScale, EndlessTerrain.PieceScale * Mathf.Max(0.001f, k), 1f);
    protected override void OnUp(bool instant) { if (box != null) box.enabled = true; }
    public bool Touches(Collider2D body) => box != null && box.enabled && body != null && Physics2D.Distance(body, box).distance < 0.05f;
    protected override void OnDown() { if (box != null) box.enabled = false; }

    protected override void Tick()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 0.2f;
        foreach (bosss b in bosss.Live)
            if (b != null && !b.IsDead && Vector2.Distance(b.transform.position, transform.position) < 2.6f) { Break(); return; }
    }
}

// 용암 웅덩이 (밟으면 0.6초마다 피해 · 적은 느려짐) · 점액 늪 (안에서는 모두 느려짐)
public class TerrainZone : TerrainPiece
{
    public bool lava;
    FxAnim visual;
    float tick, nextEnemy, nextFlame;
    static readonly Collider2D[] buf = new Collider2D[48];
    static ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
    const float LavaDamage = 6f;
    static readonly System.Random fxRng = new System.Random(7177);      // 연출용 (UnityEngine.Random 전역 상태를 건드리지 않게)

    float Radius => lava ? EndlessTerrain.LavaRadius : EndlessTerrain.BogRadius;

    protected override void OnUp(bool instant)
    {
        if (owner != null) owner.PaintZone(this, true);
        Color c = Hostile.Accessible(lava ? new Color(1f, 0.45f, 0.15f, 0.8f) : new Color(0.55f, 1f, 0.35f, 0.6f));
        visual = Fx.Play("fx_puddle", transform.position, Radius * 1.1f, c, 8f, 0f, 1, true, -1f);
        // 바닥 그림이라 지형 타일 바로 위(배경 층)에 깔음 (Effect 층이면 지나가는 적 위를 덮음)
        if (visual != null && visual.sr != null && owner != null)
        {
            visual.sr.sortingLayerID = owner.SortLayer;
            visual.sr.sortingOrder = owner.SortOrder + 1;
        }
    }

    protected override void OnDown()
    {
        if (visual != null) Destroy(visual.gameObject);
        visual = null;
        if (owner != null) owner.PaintZone(this, false);
    }

    protected override void Tick()
    {
        if (BossUltimate.Active) { tick = 0f; return; }      // 결계 · 침식 중에는 바닥 피해 · 느려짐 없음 (그림은 그대로)
        PlayerController p = Hostile.Player;
        if (p != null && !p.IsDying && Vector2.Distance(p.HurtCenter, transform.position) < Radius)      // 1.4.4~ 몸 아래쪽 판정
        {
            if (!lava) p.Slow(0.6f, 0.2f);
            else
            {
                tick += Time.deltaTime;
                if (tick >= 0.6f)
                {
                    tick = 0f;
                    AttackLabel.Pending = "용암 웅덩이";
                    p.TryHit(LavaDamage);            // 난이도 배율은 TryHit 이 곱함
                    AttackLabel.Pending = null;
                }
            }
        }
        else tick = 0f;                  // 나갔다 다시 들어오면 처음부터 0.6초 셈
        if (Time.time >= nextEnemy)
        {
            nextEnemy = Time.time + 0.2f;
            int n = Physics2D.OverlapCircle(transform.position, Radius, filter, buf);
            for (int i = 0; i < n; i++)
                if (buf[i] != null && buf[i].CompareTag("enermy") && buf[i].TryGetComponent(out EnermyController e)) e.Slow(lava ? 0.7f : 0.6f, 0.3f);
        }
        if (lava && Time.time >= nextFlame && visual != null && visual.sr != null && visual.sr.isVisible && Hostile.Glow != null)
        {
            nextFlame = Time.time + 0.3f;
            float a = (float)(fxRng.NextDouble() * Mathf.PI * 2.0), d = (float)fxRng.NextDouble() * Radius * 0.7f;
            Vector3 o = new Vector3(Mathf.Cos(a) * d, Mathf.Sin(a) * d, 0f);
            FlameParticle.Spawn(Hostile.Glow, transform.position + o, new Vector2(0f, 0.8f + 0.8f * (float)fxRng.NextDouble()), 0.4f + 0.3f * (float)fxRng.NextDouble(), 0.05f, 0.02f, false);
        }
    }

    void OnDestroy()
    {
        if (visual != null) Destroy(visual.gameObject);
    }
}
