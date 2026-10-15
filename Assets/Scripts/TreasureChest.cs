using System.Collections;
using UnityEngine;

// 보물 상자 (1.0.1): 습격을 이겨 내거나 중간 보스 · 엘리트를 쓰러뜨리면 나옴 (ChapterEvents)
// 다가가면 열리고 셋 중 하나를 고름: 코인 · 영혼 조각 · 체력 모두 회복 + 최대 체력 +10
// 1.1~: 보상은 ContentRegistry 보상 풀에서 3택 (ChestRewards) · 중간 보스 · 정예 상자(elite)는 전설 확률이 높음
//       저주받은 상자(장 중반 이벤트)는 보라색이고, 열면 보상 대신 onOpen (저주받은 적이 깨어남)
public class TreasureChest : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
    // 1.0.1: 더 잘 보이게 — 크기 1.6 → 2.6, 크고 숨 쉬는 후광, 하늘로 솟는 빛기둥, 화면 밖이면 가장자리에 방향 표시
    const float OpenRange = 2.2f, Size = 2.6f;

    GameObject label, halo, pillar, pointer;
    SpriteRenderer sr;
    Vector3 home;
    bool opening;
    bool elite;                         // 1.1~ 정예 상자 (전설 15%)
    System.Action onOpen;               // 1.1~ 저주받은 상자: 열면 보상 대신 이것

    public static TreasureChest Spawn(Vector3 at) => Spawn(at, false);

    // 1.1~ 저주받은 상자 (ChapterEvents): 보라색 · 열면 onOpen
    public static TreasureChest SpawnCursed(Vector3 at, System.Action onOpen)
    {
        TreasureChest c = Make(at, CursedTint, Loc.T("event.cursed.name"));
        c.onOpen = onOpen;
        return c;
    }

    static readonly Color CursedTint = new Color(0.75f, 0.45f, 1f);
    static readonly Color EliteTint = new Color(1f, 0.6f, 0.35f);

    public static TreasureChest Spawn(Vector3 at, bool elite)
    {
        TreasureChest c = Make(at, elite ? EliteTint : Gold, Loc.T(elite ? "chest.elite.title" : "보물 상자"));
        c.elite = elite;
        if (StageManager.Instance != null) StageManager.Instance.ShowBanner(Loc.T(elite ? "chest.elite.appear" : "보물 상자가 나타났다!"), 2f);
        return c;
    }

    static TreasureChest Make(Vector3 at, Color tint, string label)
    {
        at = Hostile.ClampArena(at);
        // 상자 그림 (tools/pixelart/make_choice_icons.py 의 chest) · 뒤에 금빛
        Sprite art = Resources.Load<Sprite>("Icons/choice_chest");
        Color body = tint == Gold ? Color.white : Color.Lerp(Color.white, tint, 0.55f);
        GameObject go = SpecialAbilities.MakeSprite("TreasureChest", art != null ? art : Hostile.Glow, at, Size, art != null ? body : tint, "Effect", 8);
        TreasureChest c = go.AddComponent<TreasureChest>();
        c.sr = go.GetComponent<SpriteRenderer>();
        c.home = at;
        c.tint = tint;
        c.halo = SpecialAbilities.MakeSprite("ChestGlow", Hostile.Glow, at, 0.6f, new Color(tint.r, tint.g, tint.b, 0.6f), "Effect", 7);
        c.pillar = SpecialAbilities.MakeSprite("ChestPillar", Hostile.Glow, at + Vector3.up * 3f, 1f, new Color(tint.r, tint.g, tint.b, 0.4f), "Effect", 6);
        c.pillar.transform.localScale = new Vector3(0.22f, 2.6f, 1f);
        c.pointer = SpecialAbilities.MakeSprite("ChestPointer", art != null ? art : Hostile.Glow, at, 0.9f, Color.white, "Effect", 40);
        GameObject pointerGlow = SpecialAbilities.MakeSprite("ChestPointerGlow", Hostile.Glow, at, 0.25f, new Color(tint.r, tint.g, tint.b, 0.7f), "Effect", 39);
        pointerGlow.transform.SetParent(c.pointer.transform, true);
        c.pointer.SetActive(false);
        c.label = SkillTag.Show(go.transform, label, 2.6f);
        Fx.Spawn("fx_soulburst", at, 3.5f, tint, 18f);
        ShockRing.Spawn(at, 0.4f, 3f, 0.4f, tint, 0.3f);
        Hostile.Play("chime", 0.8f, 1.3f);
        return c;
    }

    Color tint = Gold;

    void Update()
    {
        if (opening) return;
        float t = Time.time;
        transform.position = home + Vector3.up * Mathf.Sin(t * 3f) * 0.2f;
        transform.localScale = Vector3.one * Size * (1f + 0.07f * Mathf.Sin(t * 6f));
        if (halo != null)
        {
            halo.transform.localScale = Vector3.one * (0.6f + 0.1f * Mathf.Sin(t * 4f));
            halo.GetComponent<SpriteRenderer>().color = new Color(tint.r, tint.g, tint.b, 0.5f + 0.2f * Mathf.Sin(t * 4f));
        }
        if (pillar != null) pillar.GetComponent<SpriteRenderer>().color = new Color(tint.r, tint.g, tint.b, 0.3f + 0.15f * Mathf.Sin(t * 2.5f));
        if (Random.value < Time.deltaTime * 9f) Fx.Spawn("fx_sparkle", home + (Vector3)(Random.insideUnitCircle * 1.4f), 1.2f, tint, 20f);
        UpdatePointer(t);
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying || Time.timeScale == 0f) return;
        if (Vector2.Distance(p.transform.position, home) <= OpenRange) StartCoroutine(OpenChest(p));
    }

    IEnumerator OpenChest(PlayerController p)
    {
        opening = true;
        Fx.Spawn("fx_shock", home, 5f, tint, 18f);
        if (onOpen != null)
        {
            // 저주받은 상자: 보상 대신 저주가 깨어남 (ChapterEvents)
            Hostile.Play("roar", 0.6f, 1.2f);
            System.Action a = onOpen;
            onOpen = null;
            if (label != null) Destroy(label);
            Destroy(gameObject);
            a();
            yield break;
        }
        Hostile.Play("shimmer", 0.8f, 1.2f);
        // 1.1~ 보상 풀 3택 · 전설 룰렛 (ChestRewards, 예전엔 코인 · 영혼 조각 · 생명 고정)
        yield return ChestRewards.Open(p, home, elite);
        Fx.Spawn("fx_sparkle", home, 3f, Color.white, 20f);
        if (label != null) Destroy(label);
        Destroy(gameObject);
    }

    // 화면 밖이면 화면 가장자리(안쪽으로 조금)에 상자 그림이 상자 쪽을 가리킴
    void UpdatePointer(float t)
    {
        Camera cam = Camera.main;
        if (pointer == null || cam == null || !cam.orthographic) return;
        Vector3 c = cam.transform.position;
        float hh = cam.orthographicSize, hw = hh * cam.aspect;
        Vector2 d = home - c;
        bool inside = Mathf.Abs(d.x) < hw - 0.5f && Mathf.Abs(d.y) < hh - 0.5f;
        pointer.SetActive(!inside);
        if (inside) return;
        // 1.1~ 가장자리 여백 1.2 → 화면 비율 (위 16% · 아래 14% · 옆 5%): 체력 패널 · 경험치 바 같은 HUD 밑에 숨지 않게
        float mx = Mathf.Max(1.2f, hw * 2f * 0.05f), my = Mathf.Max(1.2f, hh * 2f * (d.y > 0f ? 0.16f : 0.14f));
        float sx = (hw - mx) / Mathf.Max(0.001f, Mathf.Abs(d.x)), sy = (hh - my) / Mathf.Max(0.001f, Mathf.Abs(d.y));
        Vector2 edge = d * Mathf.Min(sx, sy);
        pointer.transform.position = new Vector3(c.x + edge.x, c.y + edge.y, 0f);
        pointer.transform.localScale = Vector3.one * (0.9f + 0.12f * Mathf.Sin(t * 8f));
    }

    void OnDestroy()
    {
        if (label != null) Destroy(label);
        if (halo != null) Destroy(halo);
        if (pillar != null) Destroy(pillar);
        if (pointer != null) Destroy(pointer);
    }
}
