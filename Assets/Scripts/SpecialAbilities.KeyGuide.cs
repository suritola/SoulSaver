using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 키 안내 (1.1.2~): 특수 스킬(잠든 힘, 스킬 1~3 = E · F · C / 패드 RB · LB · L3)과 유물([Q] / 패드 Y)을 얻었을 때 어느 키로 쓰는지 한눈에
//   · HUD 칸 아래 바깥의 키캡 배지 (SpecialAbilities.Hud) — 그림은 여기서 코드로 그림 (KeyCapSprite · ArrowSprite)
//   · 얻는 순간 화면 가운데 위에 큰 키캡 「[E] 전투 함성 / E 키를 눌러 사용」(Notice.BigKey) + 그 칸이 몇 초 반짝이고 화살표가 가리킴
//   · 처음 그 키를 눌러 볼 때까지 칸의 키 배지가 은은히 맥동
//   · 상자 「잠든 힘」 · 석관 유물 고르기 설명에 들어갈 칸 · 키를 미리 (DormantSlotNote · RelicSlotNote)
//   키 이름은 늘 KeyBindings.Name — 패드를 쓰는 중이면 패드 버튼 이름, 키를 바꿨으면 바뀐 키
public partial class SpecialAbilities
{
    const float KeyRow = 30f;               // 스킬 · 유물 창에서 칸 아래 키 배지 자리
    const float KeyCapH = 26f, KeyCapMinW = 34f;
    const float NewTileSeconds = 6f;
    static readonly Color KeyCapInk = new Color(0.1f, 0.08f, 0.12f);
    static readonly Color KeyGold = new Color(1f, 0.84f, 0.35f);

    readonly Dictionary<int, float> newTileUntil = new Dictionary<int, float>();   // 반짝임 · 화살표가 끝나는 때 (unscaled)
    readonly HashSet<int> keyPending = new HashSet<int>();                         // 아직 한 번도 키를 눌러 보지 않은 칸

    // ================================================================= 칸 · 키
    // 이 능력을 쓰는 동작 (스킬 1~3 · 유물), 없으면 null
    GameAction? SlotAction(int id)
    {
        if (relics.Contains(id)) return GameAction.Relic;
        int slot = skills.IndexOf(id);
        return slot >= 0 && slot < SkillKeys.Length ? SkillKeys[slot] : (GameAction?)null;
    }

    string SlotKeyName(int id)
    {
        GameAction? a = SlotAction(id);
        return a.HasValue ? KeyBindings.Name(a.Value) : "";
    }

    // 카드 설명 속 키 (금빛 굵은 글자)
    public static string KeyRich(string key) => "<b><color=#FFE07A>[" + key + "]</color></b>";

    // 상자 「잠든 힘」 설명: 몇 번째 스킬 칸 · 어느 키로 들어갈지 (마지막 칸이면 다 찬 뒤 어떻게 되는지)
    public string DormantSlotNote()
    {
        int slot = skills.Count;
        if (slot >= MaxSkills || slot >= SkillKeys.Length) return "";
        string s = Loc.T("keyguide.slot").Replace("{0}", KeyRich(KeyBindings.Name(SkillKeys[slot])))
                   .Replace("{1}", (slot + 1).ToString()).Replace("{2}", MaxSkills.ToString());
        if (slot + 1 >= MaxSkills) s += "\n<size=85%>" + Loc.T("keyguide.slot.last") + "</size>";
        return s;
    }

    // 석관 유물 고르기 설명: [Q] 유물 칸 몇 번째인지 · 이미 하나 있으면 같은 키를 나눠 씀 · 마지막 칸이면 그 뒤 석관은 코인
    public string RelicSlotNote()
    {
        int n = relics.Count;
        string s = Loc.T("keyguide.relic.slot").Replace("{0}", KeyRich(KeyBindings.Name(GameAction.Relic)))
                   .Replace("{1}", (n + 1).ToString()).Replace("{2}", MaxRelics.ToString());
        if (n > 0) s += "\n<size=85%>" + Loc.T("keyguide.relic.share").Replace("{0}", Loc.T(abilities[relics[0]].name)) + "</size>";
        if (n + 1 >= MaxRelics) s += "\n<size=85%>" + Loc.T("keyguide.relic.last") + "</size>";
        return s;
    }

    // 키를 처음 눌렀으면 맥동을 멈춤 (HandleSkillKey · HandleRelicKey)
    void KeyTried(int id) => keyPending.Remove(id);

    // ================================================================= 얻는 순간
    // 이어하기로 되살릴 때(RunSave.Replaying)는 조용히
    void AnnounceKey(int id, bool relic)
    {
        if (RunSave.Replaying || Application.isBatchMode || GameInput.TrailerRunning) return;
        if (SlotAction(id) == null) return;
        newTileUntil[id] = Time.unscaledTime + NewTileSeconds;
        keyPending.Add(id);
        if (!relic && fx != null) { fx.Play("chime", 0.6f, 1.35f); fx.Play("shimmer", 0.4f, 1.4f); }
        KeyAnnounceNow(id, relic);
    }

    // 1.1.2~ 안내 메시지 개편: 큰 알림(Notice.BigKey)으로 — 다른 큰 알림과 줄을 서고, 고르기 창 · 일시정지 중에는 기다렸다가 닫히면 뜸
    void KeyAnnounceNow(int id, bool relic)
    {
        string key = SlotKeyName(id);
        bool none = key.Length == 0 || key == "-";
        GameAction act = SlotAction(id) ?? GameAction.Relic;
        KeyCode kc = KeyBindings.Get(act);
        bool button = GamePad.Active || (kc >= KeyCode.Mouse0 && kc <= KeyCode.Mouse6);
        string press = none ? Loc.T("keyguide.press.none")
                     : Loc.T(button ? "keyguide.press.pad" : "keyguide.press.key").Replace("{0}", "<color=#FFE07A>" + key + "</color>");
        if (relic && relics.Count > 1) press += "\n" + Loc.T("keyguide.relic.shared");
        Color accent = relic ? new Color(0.8f, 0.6f, 1f) : KeyGold;
        Notice.BigKey(none ? "?" : key, Loc.T(relic ? "keyguide.new.relic" : "keyguide.new.skill"), Loc.T(abilities[id].name), press, accent);
    }

    // ================================================================= HUD 칸 (매 프레임, UpdateHud)
    void AnimateKeyGuide(HudTile t)
    {
        float now = Time.unscaledTime;
        RectTransform tile = (RectTransform)t.border.transform;
        bool fresh = newTileUntil.TryGetValue(t.id, out float until) && now < until;
        if (fresh)
        {
            float k = 0.5f + 0.5f * Mathf.Sin(now * 6f);
            if (t.glow == null && GlowSprite != null)
            {
                // 창의 모든 칸 뒤에 (옆 칸을 덮지 않고 이 칸 둘레로만 금빛이 번짐)
                GameObject g = new GameObject("NewGlow", typeof(RectTransform), typeof(Image));
                RectTransform gr = g.GetComponent<RectTransform>();
                gr.SetParent(tile.parent, false);
                gr.SetAsFirstSibling();
                gr.anchorMin = gr.anchorMax = Vector2.zero;
                gr.pivot = new Vector2(0.5f, 0.5f);
                gr.sizeDelta = new Vector2(TileSize * 2.2f, TileSize * 2.2f);
                t.glow = g.GetComponent<Image>();
                t.glow.sprite = GlowSprite;
                t.glow.raycastTarget = false;
            }
            if (t.glow != null) t.glow.rectTransform.anchoredPosition = tile.anchoredPosition + tile.sizeDelta * 0.5f;
            if (t.glow != null) t.glow.color = new Color(KeyGold.r, KeyGold.g, KeyGold.b, 0.3f + 0.45f * k);
            if (t.arrow == null)
            {
                GameObject a = new GameObject("NewArrow", typeof(RectTransform), typeof(Image));
                RectTransform ar = a.GetComponent<RectTransform>();
                ar.SetParent(tile, false);
                ar.anchorMin = ar.anchorMax = new Vector2(1f, 0.5f);
                ar.pivot = new Vector2(0f, 0.5f);
                ar.sizeDelta = new Vector2(36f, 32f);
                t.arrow = a.GetComponent<Image>();
                t.arrow.sprite = ArrowSprite;
                t.arrow.raycastTarget = false;
            }
            // 칸 오른쪽에서 칸 쪽으로 콕콕 (새 칸은 늘 그 창의 맨 오른쪽이라 오른쪽은 비어 있음)
            t.arrow.rectTransform.anchoredPosition = new Vector2(6f + 8f * (1f - Mathf.Abs(Mathf.Sin(now * 5f))), 0f);
            t.arrow.color = new Color(1f, 1f, 1f, Mathf.Clamp01((until - now) / 0.5f));
            t.border.color = Color.Lerp(KeyGold, Color.white, k);
        }
        else
        {
            if (t.glow != null) { Destroy(t.glow.gameObject); t.glow = null; }
            if (t.arrow != null) { Destroy(t.arrow.gameObject); t.arrow = null; }
        }

        // 처음 눌러 볼 때까지 키 배지가 은은히 커졌다 작아지고 금빛이 돎
        bool pending = keyPending.Contains(t.id) && t.keyCap.gameObject.activeSelf;
        float s = 1f;
        if (pending)
        {
            float p = 0.5f + 0.5f * Mathf.Sin(now * 4f);
            s = 1f + 0.12f * p;
            t.keyCap.color = Color.Lerp(t.capBase, new Color(1f, 0.86f, 0.45f), 0.55f * p);
        }
        if (t.keyCap.rectTransform.localScale.x != s) t.keyCap.rectTransform.localScale = new Vector3(s, s, 1f);
    }

    // ================================================================= 코드로 그린 그림 (도트, Point 필터)
    static Sprite keyCapSprite, arrowSprite;

    // 키보드 키캡: 진한 테두리 · 밝은 윗면 · 아래쪽 옆면(그림자) · 윗줄 하이라이트 · 둥근 모서리 — 9칸 늘림(Sliced)
    internal static Sprite KeyCapSprite
    {
        get
        {
            if (keyCapSprite != null) return keyCapSprite;
            const int W = 20, H = 20;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "KeyCap" };
            Color clear = new Color(0f, 0f, 0f, 0f), ink = new Color(0.1f, 0.08f, 0.12f, 1f);
            Color face = new Color(0.97f, 0.95f, 0.89f), faceLow = new Color(0.86f, 0.83f, 0.77f), side = new Color(0.62f, 0.58f, 0.53f), shine = Color.white;
            Color[] px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    bool edgeX = x == 0 || x == W - 1, edgeY = y == 0 || y == H - 1;
                    bool nearX = x == 1 || x == W - 2, nearY = y == 1 || y == H - 2;
                    Color c;
                    if (edgeX && edgeY) c = clear;
                    else if (edgeX || edgeY || (nearX && nearY)) c = ink;
                    else if (y <= 4) c = side;
                    else if (y == 5) c = faceLow;
                    else if (y == H - 2) c = shine;
                    else c = face;
                    px[y * W + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(false, true);
            keyCapSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(3f, 6f, 3f, 3f));
            keyCapSprite.name = "KeyCap";
            return keyCapSprite;
        }
    }

    // 왼쪽을 가리키는 금빛 화살표 (진한 테두리)
    static Sprite ArrowSprite
    {
        get
        {
            if (arrowSprite != null) return arrowSprite;
            const int W = 18, H = 15;
            bool In(int x, int y)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return false;
                float dy = Mathf.Abs(y + 0.5f - H * 0.5f);
                if (x < 10) return dy <= (x + 1) * 0.75f;      // 머리 (삼각형)
                return dy <= 2.6f;                              // 자루
            }
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "KeyArrow" };
            Color ink = new Color(0.12f, 0.07f, 0.02f, 1f), fill = new Color(1f, 0.84f, 0.35f), hi = new Color(1f, 0.95f, 0.7f);
            Color[] px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (!In(x, y)) { px[y * W + x] = new Color(0f, 0f, 0f, 0f); continue; }
                    bool rim = !In(x - 1, y) || !In(x + 1, y) || !In(x, y - 1) || !In(x, y + 1);
                    px[y * W + x] = rim ? ink : y > H / 2 ? hi : fill;
                }
            tex.SetPixels(px);
            tex.Apply(false, true);
            arrowSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
            arrowSprite.name = "KeyArrow";
            return arrowSprite;
        }
    }
}
