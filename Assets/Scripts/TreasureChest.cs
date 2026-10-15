using System.Collections;
using UnityEngine;

// 보물 상자 (1.0.1): 습격을 이겨 내거나 중간 보스 · 엘리트를 쓰러뜨리면 나옴 (ChapterEvents)
// 다가가면 열리고 셋 중 하나를 고름: 코인 · 영혼 조각 · 체력 모두 회복 + 최대 체력 +10
public class TreasureChest : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
    // 1.0.1: 더 잘 보이게 — 크기 1.6 → 2.6, 크고 숨 쉬는 후광, 하늘로 솟는 빛기둥, 화면 밖이면 가장자리에 방향 표시
    const float OpenRange = 2.2f, Size = 2.6f;

    GameObject label, halo, pillar, pointer;
    SpriteRenderer sr;
    Vector3 home;
    bool opening;

    public static TreasureChest Spawn(Vector3 at)
    {
        at = Hostile.ClampArena(at);
        // 상자 그림 (tools/pixelart/make_choice_icons.py 의 chest) · 뒤에 금빛
        Sprite art = Resources.Load<Sprite>("Icons/choice_chest");
        GameObject go = SpecialAbilities.MakeSprite("TreasureChest", art != null ? art : Hostile.Glow, at, Size, art != null ? Color.white : Gold, "Effect", 8);
        TreasureChest c = go.AddComponent<TreasureChest>();
        c.sr = go.GetComponent<SpriteRenderer>();
        c.home = at;
        c.halo = SpecialAbilities.MakeSprite("ChestGlow", Hostile.Glow, at, 0.6f, new Color(1f, 0.82f, 0.3f, 0.6f), "Effect", 7);
        c.pillar = SpecialAbilities.MakeSprite("ChestPillar", Hostile.Glow, at + Vector3.up * 3f, 1f, new Color(1f, 0.85f, 0.4f, 0.4f), "Effect", 6);
        c.pillar.transform.localScale = new Vector3(0.22f, 2.6f, 1f);
        c.pointer = SpecialAbilities.MakeSprite("ChestPointer", art != null ? art : Hostile.Glow, at, 0.9f, Color.white, "Effect", 40);
        GameObject pointerGlow = SpecialAbilities.MakeSprite("ChestPointerGlow", Hostile.Glow, at, 0.25f, new Color(1f, 0.82f, 0.3f, 0.7f), "Effect", 39);
        pointerGlow.transform.SetParent(c.pointer.transform, true);
        c.pointer.SetActive(false);
        c.label = SkillTag.Show(go.transform, Loc.T("보물 상자"), 2.6f);
        Fx.Spawn("fx_soulburst", at, 3.5f, Gold, 18f);
        ShockRing.Spawn(at, 0.4f, 3f, 0.4f, Gold, 0.3f);
        Hostile.Play("chime", 0.8f, 1.3f);
        if (StageManager.Instance != null) StageManager.Instance.ShowBanner(Loc.T("보물 상자가 나타났다!"), 2f);
        return c;
    }

    void Update()
    {
        if (opening) return;
        float t = Time.time;
        transform.position = home + Vector3.up * Mathf.Sin(t * 3f) * 0.2f;
        transform.localScale = Vector3.one * Size * (1f + 0.07f * Mathf.Sin(t * 6f));
        if (halo != null)
        {
            halo.transform.localScale = Vector3.one * (0.6f + 0.1f * Mathf.Sin(t * 4f));
            halo.GetComponent<SpriteRenderer>().color = new Color(1f, 0.82f, 0.3f, 0.5f + 0.2f * Mathf.Sin(t * 4f));
        }
        if (pillar != null) pillar.GetComponent<SpriteRenderer>().color = new Color(1f, 0.85f, 0.4f, 0.3f + 0.15f * Mathf.Sin(t * 2.5f));
        if (Random.value < Time.deltaTime * 9f) Fx.Spawn("fx_sparkle", home + (Vector3)(Random.insideUnitCircle * 1.4f), 1.2f, Gold, 20f);
        UpdatePointer(t);
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying || Time.timeScale == 0f) return;
        if (Vector2.Distance(p.transform.position, home) <= OpenRange) StartCoroutine(OpenChest(p));
    }

    IEnumerator OpenChest(PlayerController p)
    {
        opening = true;
        Fx.Spawn("fx_shock", home, 5f, Gold, 18f);
        Hostile.Play("shimmer", 0.8f, 1.2f);
        int slot = StageManager.Instance != null ? Chapters.SlotOf(StageManager.Instance.CurrentStage) : 0;
        int coins = 20 + 10 * slot, shards = 20 + 10 * slot;
        ChoiceUI.Option[] options =
        {
            new ChoiceUI.Option("황금", Loc.T("코인 +{0}").Replace("{0}", coins.ToString()), new Color(1f, 0.82f, 0.3f), "gold"),
            new ChoiceUI.Option("영혼", Loc.T("영혼 조각 +{0}").Replace("{0}", shards.ToString()), new Color(0.55f, 0.9f, 1f), "soul"),
            new ChoiceUI.Option("생명", "체력을 모두 회복하고 최대 체력 +10", new Color(1f, 0.45f, 0.45f), "life"),
        };
        int pick = 0;
        yield return ChoiceUI.Run("보물 상자", "하나를 고르세요", options, i => pick = i);
        if (pick == 0)
        {
            Coin coin = FindFirstObjectByType<Coin>();
            if (coin != null) coin.AddCoin(coins);
        }
        else if (pick == 1) SoulShards.Add(shards, home, false);
        else if (p != null)
        {
            p.PlayerMaxHealth += 10f;
            p.PlayerHealth = p.PlayerMaxHealth;
            RunSave.Record("h10");                  // 판 중간 저장: 이어할 때 최대 체력도 다시 (RunSave 'h')
        }
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
