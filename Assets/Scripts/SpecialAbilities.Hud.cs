using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD: 무기 · 스킬 · 패시브를 아이콘 칸으로 (왼쪽 아래)
// 칸마다 쿨타임(어두운 원), 탄창 · 남은 시간 · 중첩 같은 짧은 상태, 마우스를 올리면 자세한 설명과 지금 상태
// 창 이름표를 누르면 접었다 폈다 (접힘 상태는 저장)
public partial class SpecialAbilities
{
    class HudTile
    {
        public int id;
        public Image border, icon, cooldown;
        public TextMeshProUGUI label, key;
        public Image keyCap;
        public Color capBase = Color.white;
        public Image glow, arrow;           // 1.1.2~ 새로 얻은 칸 반짝임 · 가리키는 화살표 (SpecialAbilities.KeyGuide)
    }

    class HudGroup
    {
        public string title, prefKey;
        public RectTransform root, tiles;
        public TextMeshProUGUI header;
        public bool collapsed;
        public readonly List<HudTile> list = new List<HudTile>();
    }

    HudGroup weaponGroup, skillGroup, passiveGroup, relicGroup;     // 1.2~ 유물 칸 (스킬 칸 위)
    const int PistolRow = -1;
    const int EvoRow = -2;      // 1.0.1: 진화한 무기 칸 (마우스를 올리면 진화 능력 · 덧붙은 궁극기 설명)
    const float TileSize = 58f, TileGap = 6f, HeaderHeight = 26f, GroupGap = 8f;
    static readonly Color TileBorder = new Color(0.3f, 0.26f, 0.34f, 0.95f);
    static readonly Color TileActive = new Color(0.96f, 0.75f, 0.3f, 1f);
    static readonly Color TileText = new Color(0.95f, 0.92f, 0.85f);

    void BuildHud()
    {
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        weaponGroup = NewGroup(canvas, "WeaponSlot", UsesEvolution ? Loc.T("무기") : Loc.T("무기") + " [" + KeyBindings.Name(GameAction.Swap) + "]", "hud.fold.weapon");
        skillGroup = NewGroup(canvas, "SkillSlot", Loc.T("스킬"), "hud.fold.skill");
        passiveGroup = NewGroup(canvas, "PassiveSlot", Loc.T("패시브"), "hud.fold.passive");
        relicGroup = NewGroup(canvas, "RelicSlot", Loc.T("유물"), "hud.fold.relic");
    }

    HudGroup NewGroup(Canvas canvas, string name, string title, string prefKey)
    {
        HudGroup g = new HudGroup { title = title, prefKey = prefKey };
        GameObject go = new GameObject(name, typeof(RectTransform));
        g.root = go.GetComponent<RectTransform>();
        g.root.SetParent(canvas.transform, false);
        Transform ammo = canvas.transform.Find("AmmoPanel");
        if (ammo != null) g.root.SetSiblingIndex(ammo.GetSiblingIndex() + 1);
        g.root.anchorMin = g.root.anchorMax = g.root.pivot = Vector2.zero;

        // 이름표 (누르면 접기 · 펴기)
        GameObject head = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform hr = head.GetComponent<RectTransform>();
        hr.SetParent(g.root, false);
        hr.anchorMin = hr.anchorMax = hr.pivot = Vector2.zero;
        hr.sizeDelta = new Vector2(150f, HeaderHeight);
        Image himg = head.GetComponent<Image>();
        himg.color = new Color(0.08f, 0.06f, 0.1f, 0.75f);
        Button hb = head.GetComponent<Button>();
        hb.navigation = new Navigation { mode = Navigation.Mode.None };
        hb.onClick.AddListener(() =>
        {
            g.collapsed = !g.collapsed;
            Prefs.SetInt(g.prefKey, g.collapsed ? 1 : 0);
            if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            LayoutGroups();
            fx.Play("clank", 0.25f, 1.8f);
        });
        g.header = HudText(hr, 15f, new Color(1f, 0.78f, 0.5f), TextAlignmentOptions.Left);
        g.header.margin = new Vector4(8f, 0f, 6f, 0f);

        GameObject tiles = new GameObject("Tiles", typeof(RectTransform));
        g.tiles = tiles.GetComponent<RectTransform>();
        g.tiles.SetParent(g.root, false);
        g.tiles.anchorMin = g.tiles.anchorMax = g.tiles.pivot = Vector2.zero;
        UiScaler.Isolate(tiles);                        // 1.1.9: 쿨타임 원 · 키 배지 맥동이 HUD 전체를 다시 묶지 않게

        g.collapsed = Prefs.GetInt(prefKey, 0) == 1;
        go.SetActive(false);
        return g;
    }

    TextMeshProUGUI HudText(RectTransform parent, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        if (fontMaterial != null) t.fontSharedMaterial = fontMaterial;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    Image HudImage(RectTransform parent, string name, Sprite sprite, Color color, float inset)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // 고른 능력을 종류별 창에 한 칸씩 넣음
    void RebuildHudRows()
    {
        if (weaponGroup == null) return;
        List<int> w = new List<int>(), s = new List<int>(), p = new List<int>(), r = new List<int>();
        if (weapons.Count > 0 && !UsesEvolution) w.Add(PistolRow);    // 진화 방식은 권총으로 돌아가지 않음
        if (UsesEvolution && EvolutionTier > 0) w.Insert(0, EvoRow);  // 지금 진화한 무기가 어떤 능력인지 볼 수 있게 (1.0.1)
        foreach (int id in equipped)
        {
            if (relics.Contains(id)) r.Add(id);
            else if (abilities[id].kind == SpecialKind.Weapon) w.Add(id);
            else if (abilities[id].kind == SpecialKind.Skill) s.Add(id);
            else p.Add(id);
        }
        FillGroup(weaponGroup, w);
        FillGroup(skillGroup, s);
        FillGroup(passiveGroup, p);
        if (relicGroup != null) FillGroup(relicGroup, r);
        LayoutGroups();
    }

    void FillGroup(HudGroup g, List<int> ids)
    {
        foreach (HudTile t in g.list)
        {
            Destroy(t.border.gameObject);
            if (t.glow != null) Destroy(t.glow.gameObject);     // 1.1.2~ 새 칸 빛무리는 칸 뒤(창 안)에 따로 있음
        }
        g.list.Clear();
        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            GameObject go = new GameObject("Tile", typeof(RectTransform), typeof(Image));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(g.tiles, false);
            r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
            r.sizeDelta = new Vector2(TileSize, TileSize);
            r.anchoredPosition = new Vector2(i * (TileSize + TileGap), 0f);

            HudTile t = new HudTile { id = id };
            t.border = go.GetComponent<Image>();
            t.border.color = TileBorder;
            t.border.raycastTarget = true;                  // 마우스를 올리면 설명
            HudImage(r, "Back", null, new Color(0.07f, 0.06f, 0.09f, 0.92f), 3f);
            t.icon = HudImage(r, "Icon", TileIcon(id), Color.white, 6f);
            t.icon.preserveAspect = true;
            t.cooldown = HudImage(r, "Cooldown", barFillSprite, new Color(0f, 0f, 0f, 0.62f), 3f);
            t.cooldown.type = Image.Type.Filled;
            t.cooldown.fillMethod = Image.FillMethod.Radial360;
            t.cooldown.fillOrigin = (int)Image.Origin360.Top;
            t.cooldown.fillClockwise = false;
            t.cooldown.fillAmount = 0f;

            RectTransform lr = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            lr.SetParent(r, false);
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = new Vector2(1f, 0f);
            lr.pivot = new Vector2(0.5f, 0f);
            lr.sizeDelta = new Vector2(0f, 18f);
            lr.anchoredPosition = new Vector2(0f, 2f);
            t.label = HudText(lr, 14f, TileText, TextAlignmentOptions.Center);
            // 1.0.4~: 「부활 1」 · 「진화 II」처럼 낱말이 붙어 길어지면 칸 폭에 맞게 줄임
            t.label.enableAutoSizing = true;
            t.label.fontSizeMin = 9f;
            t.label.fontSizeMax = 14f;
            t.label.overflowMode = TextOverflowModes.Truncate;
            t.label.outlineWidth = 0.25f;
            t.label.outlineColor = new Color32(0, 0, 0, 255);

            // 키 배지 (1.1.2~): 키캡 모양(밝은 면 · 진한 글자 · 테두리)을 칸 아래 바깥에 붙임 → 아이콘 · 쿨타임 원과 겹치지 않음
            //   예전엔 칸 안 왼쪽 위 26×24 에 「SPACE」가 그려진 fx_keycap 그림을 늘려 깔아 「E」와 섞여 읽히지 않았음
            //   스킬 · 유물 창은 LayoutGroups 가 칸 아래에 KeyRow 만큼 자리를 비워 둠 · 키가 없는 칸(무기 · 패시브)은 숨김
            GameObject cap = new GameObject("KeyCap", typeof(RectTransform), typeof(Image));
            RectTransform cr = cap.GetComponent<RectTransform>();
            cr.SetParent(r, false);
            cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0f);
            cr.pivot = new Vector2(0.5f, 1f);
            cr.anchoredPosition = new Vector2(0f, -3f);
            cr.sizeDelta = new Vector2(KeyCapMinW, KeyCapH);
            t.keyCap = cap.GetComponent<Image>();
            t.keyCap.sprite = KeyCapSprite;
            t.keyCap.type = Image.Type.Sliced;
            t.keyCap.pixelsPerUnitMultiplier = 0.7f;      // 테두리 · 옆면을 조금 굵게
            t.keyCap.color = Color.white;
            t.keyCap.raycastTarget = false;
            cap.SetActive(false);
            t.key = HudText(cr, 20f, KeyCapInk, TextAlignmentOptions.Center);
            t.key.rectTransform.offsetMin = new Vector2(3f, 4f);       // 아래쪽 옆면(그림자)만큼 글자를 올림
            t.key.rectTransform.offsetMax = new Vector2(-3f, -1f);
            t.key.fontStyle = FontStyles.Bold;
            // 긴 키 이름(마우스 4 · 휠 클릭 · Num5)은 배지 안에서 글자를 줄임
            t.key.enableAutoSizing = true;
            t.key.fontSizeMax = 20f;
            t.key.fontSizeMin = 10f;
            t.key.overflowMode = TextOverflowModes.Truncate;

            TooltipTrigger tip = go.AddComponent<TooltipTrigger>();
            tip.titleProvider = () => TileTitle(id);
            tip.bodyProvider = () => TileBody(id);
            tip.hud = true;         // 1.0.4~: 이 칸 위를 겨눠도 평타가 멈추지 않음 · 전투 중엔 잠깐 머물러야 설명
            g.list.Add(t);
        }
    }

    Sprite TileIcon(int id)
    {
        if (id == EvoRow) return MainWeaponIcon;
        if (id == PistolRow) return Resources.Load<Sprite>("Weapons/weapon_" + (CharacterData.Current.held ?? "pistol"));
        return abilities[id].icon;
    }

    // 창 크기 · 위치: 무기 → 스킬 → 패시브 순서로 아래에서 위로 쌓음
    void LayoutGroups()
    {
        float y = 100f;
        foreach (HudGroup g in new[] { weaponGroup, skillGroup, relicGroup, passiveGroup })
        {
            if (g == null) continue;
            bool any = g.list.Count > 0;
            g.root.gameObject.SetActive(any);
            if (!any) continue;
            float tilesH = g.collapsed ? 0f : TileSize;
            // 1.1.2~ 스킬 · 유물 창은 칸 아래에 키 배지 자리 (KeyRow)
            float keyRow = !g.collapsed && (g == skillGroup || g == relicGroup) ? KeyRow : 0f;
            g.tiles.gameObject.SetActive(!g.collapsed);
            g.tiles.anchoredPosition = new Vector2(0f, keyRow);
            Transform head = g.header.transform.parent;
            ((RectTransform)head).anchoredPosition = new Vector2(0f, keyRow + tilesH + (g.collapsed ? 0f : 4f));
            g.header.text = (g.collapsed ? "+ " : "- ") + g.title + (g.collapsed ? "  (" + g.list.Count + ")" : "");
            g.root.anchoredPosition = new Vector2(24f, y);
            y += keyRow + tilesH + (g.collapsed ? 0f : 4f) + HeaderHeight + GroupGap;
        }
    }

    // ================================================================= 매 프레임 상태
    HudGroup[] hudGroups;
    float nextHudText;

    // 1.1.9: 쿨타임 원은 1/120 단위로만 고침 (매 프레임 조금씩 바뀌면 HUD 캔버스를 매 프레임 다시 빌드함 · 눈에는 같음)
    static float CoolStep(float v) => v <= 0f ? 0f : v >= 1f ? 1f : Mathf.Ceil(v * 120f) / 120f;

    void UpdateHud()
    {
        if (weaponGroup == null) return;
        if (hudGroups == null) hudGroups = relicGroup != null ? new[] { weaponGroup, skillGroup, relicGroup, passiveGroup } : new[] { weaponGroup, skillGroup, passiveGroup };
        bool full = Time.unscaledTime >= nextHudText;
        if (full) nextHudText = Time.unscaledTime + 0.1f;
        foreach (HudGroup g in hudGroups)
        {
            if (g.collapsed) continue;
            foreach (HudTile t in g.list)
            {
                if (full) UpdateTile(t);
                else t.cooldown.fillAmount = CoolStep(TileCool(t.id));
                if (g == skillGroup || g == relicGroup) AnimateKeyGuide(t);     // 1.1.2~ 새로 얻은 칸 반짝임 · 화살표 · 키 배지 맥동
            }
        }
    }

    // 쿨타임 원만 (글자를 만들지 않음)
    float TileCool(int id)
    {
        if (id == EvoRow) return 0f;
        if (id == PistolRow)
        {
            CharacterKit kit = CharacterKit.Instance;
            if (kit != null && !kit.UsesAmmo) return 0f;
            return player.reload > 0f ? 1f - Mathf.Clamp01(player.reload / Mathf.Max(0.01f, player.reloadTime)) : 0f;
        }
        SpecialKind kind = abilities[id].kind;
        if (kind == SpecialKind.Weapon)
        {
            if (id == FlameId) return heat;
            if (id == ScytheId) return activeScythe == null ? 0f : 1f;
            if (MagSize(id) <= 0) return 0f;
            WeaponAmmo a = Ammo(id);
            return a.Reloading ? Mathf.Clamp01((a.reloadEnd - Time.time) / BaseReload(id)) : 0f;
        }
        if (kind == SpecialKind.Skill)
        {
            float left = CooldownUntil(id) - Time.time;
            float length = cooldownLength.TryGetValue(id, out float l) ? l : 1f;
            return left > 0f ? Mathf.Clamp01(left / length) : 0f;
        }
        return 0f;
    }

    Image evoGlow;
    void EvoGlow(HudTile t, float k)
    {
        if (evoGlow == null && SpecialAbilities.GlowSprite != null)
        {
            RectTransform tile = (RectTransform)t.border.transform;
            GameObject g = new GameObject("EvoGlow", typeof(RectTransform), typeof(Image));
            RectTransform r = g.GetComponent<RectTransform>();
            r.SetParent(tile.parent, false);
            r.SetSiblingIndex(tile.GetSiblingIndex());          // 칸 뒤에
            r.anchorMin = tile.anchorMin; r.anchorMax = tile.anchorMax; r.pivot = tile.pivot;
            r.sizeDelta = tile.sizeDelta * 2f;
            evoGlow = g.GetComponent<Image>();
            evoGlow.sprite = SpecialAbilities.GlowSprite;
            evoGlow.raycastTarget = false;
        }
        if (evoGlow == null) return;
        RectTransform tr = (RectTransform)t.border.transform, gr = (RectTransform)evoGlow.transform;
        if (gr.parent != tr.parent) { Destroy(evoGlow.gameObject); evoGlow = null; return; }     // 칸을 다시 만들었으면 다음 프레임에 새로
        gr.anchoredPosition = tr.anchoredPosition - tr.sizeDelta * 0.5f;      // 칸 기준점이 왼쪽 아래라 한가운데에 맞춤
        evoGlow.color = new Color(1f, 0.82f, 0.35f, 0.3f + 0.35f * k);
    }

    void UpdateTile(HudTile t)
    {
        bool active = false;
        float cool = 0f;
        string label = "", key = "";
        int id = t.id;

        if (id == EvoRow)
        {
            // 진화 단계 표시 (I · II), 금빛 테두리
            // 1.0.1: 다른 무기 칸과 똑같아 눈에 안 띄었음 → 뒤에 숨 쉬는 금빛 빛무리 + 금색 ↔ 흰색 반짝임
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            t.border.color = Color.Lerp(TileActive, Color.white, 0.55f * k);
            EvoGlow(t, k);
            t.cooldown.fillAmount = 0f;
            // 1.0.4~: 「I · II」만으로는 뜻을 몰랐음 → 「진화 I」처럼 낱말을 붙임
            string tierText = Loc.T("진화") + (EvolutionTier >= 2 ? " II" : " I");
            if (t.label.text != tierText) t.label.text = tierText;
            if (t.keyCap.gameObject.activeSelf) t.keyCap.gameObject.SetActive(false);
            if (t.key.text != "") t.key.text = "";
            return;
        }
        if (id == PistolRow || abilities[id].kind == SpecialKind.Weapon)
        {
            active = id == PistolRow ? !WeaponActive : CurrentWeapon == id;
            WeaponStatus(id, out label, out cool);
        }
        else if (abilities[id].kind == SpecialKind.Skill)
        {
            key = SlotKeyName(id);
            float left = CooldownUntil(id) - Time.time;
            float length = cooldownLength.TryGetValue(id, out float l) ? l : 1f;
            cool = left > 0f ? Mathf.Clamp01(left / length) : 0f;
            label = left > 0f ? left.ToString(left < 10f ? "0.0" : "0") : "";
            if (id == SoulBurstId) label = souls + "/" + SoulsNeeded;
            active = left <= 0f;
        }
        else
        {
            label = PassiveState(id, false);
        }

        bool flash = rowFlashUntil.TryGetValue(id, out float until) && Time.time < until;
        t.border.color = flash ? Color.white : active ? TileActive : TileBorder;
        t.cooldown.fillAmount = CoolStep(cool);
        if (t.label.text != label) t.label.text = label;
        // 키 배지 (1.1.2~ 칸 아래 바깥의 키캡): 설정 「키 아이콘」이면 밝은 키캡 + 진한 글자, 끄면 어두운 키캡 + 금빛 글자
        //   쿨타임 중에도 배지는 가리지 않고 면만 조금 어둡게 (글자는 그대로 진함)
        bool cap = key.Length > 0;
        bool icons = GameSettings.KeyIcons;
        if (t.keyCap.gameObject.activeSelf != cap) t.keyCap.gameObject.SetActive(cap);
        string keyText = key + (id >= 0 && IsEvolved(id) ? "+" : "");
        if (cap)
        {
            // 배지 폭은 실제로 보이는 글자에 맞춤 (한글 · 가나는 글자당 18px) · 옆 칸 배지와 닿지 않게 칸 폭 + 간격까지
            float est = 0f;
            foreach (char ch in keyText) est += ch >= 0x2E80 ? 18f : 12f;
            float capW = Mathf.Clamp(16f + est, KeyCapMinW, TileSize + TileGap - 2f);
            t.keyCap.rectTransform.sizeDelta = new Vector2(capW, KeyCapH);
            Color face = icons ? Color.white : new Color(0.32f, 0.28f, 0.36f);
            if (cool > 0f) face *= new Color(0.82f, 0.82f, 0.82f, 1f);
            t.capBase = face;
            t.keyCap.color = face;
        }
        t.key.color = icons ? KeyCapInk : new Color(1f, 0.86f, 0.4f);
        if (t.key.text != keyText) t.key.text = keyText;
    }

    // 무기 칸: 짧은 상태 (탄창 · 장전 · 열기)와 가려질 비율
    void WeaponStatus(int id, out string label, out float cool)
    {
        cool = 0f;
        if (id == PistolRow)
        {
            CharacterKit kit = CharacterKit.Instance;
            if (kit != null && !kit.UsesAmmo) { label = ""; return; }
            if (player.reload > 0f)
            {
                cool = 1f - Mathf.Clamp01(player.reload / Mathf.Max(0.01f, player.reloadTime));
                label = Loc.T("장전");
                return;
            }
            label = player.NowBullet + "/" + player.MaxBullet;
            return;
        }
        if (id == FlameId) { label = overheated ? Loc.T("과열") : Mathf.RoundToInt(heat * 100f) + "%"; cool = heat; return; }
        if (id == ScytheId) { label = activeScythe == null ? "" : Loc.T("회수"); cool = activeScythe == null ? 0f : 1f; return; }
        WeaponAmmo a = Ammo(id);
        if (MagSize(id) <= 0) { label = ""; return; }
        if (a.Reloading)
        {
            cool = Mathf.Clamp01((a.reloadEnd - Time.time) / BaseReload(id));
            label = Loc.T("장전");
            return;
        }
        label = a.ammo + "/" + MagSize(id);
    }

    // 패시브 칸의 상태 (거너 능력 + 캐릭터 능력)
    string PassiveState(int id, bool detail)
    {
        if (id == UndyingId)
        {
            int left = UndyingMaxUses - undyingUses;
            // 1.0.4~: 칸 글자 「x1」 → 「부활 1」 · 「영혼 3」처럼 낱말을 붙임 (툴팁 없이도 뜻이 보이게)
            return detail ? (left > 0 ? Loc.T("부활 ") + left + Loc.T("번 남음") : Loc.T("이번 판에 다 씀")) : Loc.T("부활 ") + left;
        }
        if (id == OrbsId) return detail ? Loc.T("수호 영혼 ") + orbs.Count : Loc.T("영혼") + " " + orbs.Count;
        return IsKit(id) ? KitPassiveState(id, detail) : "";
    }

    // 1.1.9~ 빌드 자세히 보기(BuildView): HUD 칸과 같은 순서 · 같은 글 (무기 진화 칸 → 기본 무기 → 고른 순서)
    public struct TileInfo
    {
        public Sprite icon;
        public string title, body;
        public bool relic, evoRow;
        public SpecialKind kind;        // 1.2.5~ 일시정지 「지금 가진 스킬」 묶음 (기본 무기 · 무기 진화 칸 = Weapon)
        public bool evolved;            // 진화한 능력 · 무기 진화 칸
    }

    public List<TileInfo> BuildTiles()
    {
        List<TileInfo> list = new List<TileInfo>();
        if (abilities == null) return list;
        List<int> ids = new List<int>();
        if (UsesEvolution && EvolutionTier > 0) ids.Add(EvoRow);
        if (weapons.Count > 0 && !UsesEvolution) ids.Add(PistolRow);
        foreach (int id in equipped) if (id >= 0 && id < abilities.Length && abilities[id] != null) ids.Add(id);
        foreach (int id in ids)
            list.Add(new TileInfo
            {
                icon = TileIcon(id), title = TileTitle(id), body = TileBody(id), relic = id >= 0 && relics.Contains(id), evoRow = id == EvoRow,
                kind = id >= 0 ? abilities[id].kind : SpecialKind.Weapon, evolved = id == EvoRow || (id >= 0 && IsEvolved(id)),
            });
        return list;
    }

    // ================================================================= 툴팁
    string TileTitle(int id)
    {
        if (id == EvoRow) return Loc.T("무기 진화") + "  · " + EvolutionName(EvoStep(EvolutionTier), EvolutionTier);
        if (id == PistolRow) return Loc.T(CharacterData.IsGunner ? "기본 권총" : CharacterData.Current.weapon) + "  · " + Loc.T("기본 무기");
        SpecialDef def = abilities[id];
        string kind = def.kind == SpecialKind.Weapon ? Loc.T("무기") : def.kind == SpecialKind.Skill ? Loc.T("스킬") : Loc.T("패시브");
        return Loc.T(def.name) + (IsEvolved(id) ? Loc.T(" (진화)") : "") + "  · " + kind;
    }

    string TileBody(int id)
    {
        if (id == EvoRow)
        {
            // 1차 · 2차 진화마다 능력과 (덧붙은) 궁극기
            string all = "";
            for (int tier = 1; tier <= Mathf.Min(2, EvolutionTier); tier++)
            {
                int step = EvoStep(tier);
                if (step < 0) continue;
                all += (all.Length > 0 ? "\n\n" : "") + "<color=#F5D478>" + EvolutionName(step, tier) + "</color>\n" + EvolutionDesc(step, tier);
            }
            return all;
        }
        string state;
        string body;
        if (id == PistolRow)
        {
            body = Loc.T(CharacterData.Current.attack);
            WeaponStatus(id, out string l, out float c);
            state = (!WeaponActive ? Loc.T("사용 중") : Loc.T("[{SWAP}]로 꺼내기")) + (l.Length > 0 ? "  · " + l : "");
        }
        else
        {
            SpecialDef def = abilities[id];
            body = AbilityText(id, def.description);
            if (def.kind == SpecialKind.Weapon)
            {
                WeaponStatus(id, out string l, out float c);
                state = (CurrentWeapon == id ? Loc.T("사용 중") : Loc.T("[{SWAP}]로 꺼내기")) + (l.Length > 0 ? "  · " + l : "");
            }
            else if (def.kind == SpecialKind.Skill)
            {
                float left = CooldownUntil(id) - Time.time;
                state = left > 0f ? Loc.T("쿨타임 ") + left.ToString("0.0") + Loc.T("초") : Loc.T("사용 가능");
                if (relics.Contains(id)) state = Loc.T("relic.tag") + " · " + state;     // 1.2~ 유물 칸 [Q] · 쿨타임 1.5배
                else if (SlotKeyName(id).Length > 0) state = Loc.T("keyguide.use").Replace("{0}", SlotKeyName(id)) + " · " + state;     // 1.1.2~ 어느 키인지 설명에도
                if (id == SoulBurstId) state += Loc.T(" · 영혼 ") + souls;
            }
            else state = PassiveState(id, true);
        }
        return body + (state.Length > 0 ? "\n\n<color=#F5D478>" + state + "</color>" : "");
    }
}

// (SpecialAbilities) 1 · 2차 진화로 고른 것: 거너는 무기 번호, 다른 영웅은 형태 · 강화
public partial class SpecialAbilities
{
    int EvoStep(int tier)
    {
        if (Gunner) return tier >= 2 ? (gunEvo2 >= 0 ? gunEvo2 : gunEvo1) : gunEvo1;
        CharacterKit k = Kit;
        if (k == null) return -1;
        return tier >= 2 ? (k.augment >= 0 ? k.augment : k.form) : k.form;
    }
}

// 무한 모드가 아닐 때 화면 위 가운데에 플레이 시간 (00:00, 멈춘 시간은 빼고)
public class RunClock : MonoBehaviour
{
    TextMeshProUGUI text;
    float start;
    int shown = -1;
    static RunClock instance;

    // 지금 화면에 보이는 시간 (판 중간 저장이 적어 둠)
    public static float Elapsed => instance != null ? Time.time - instance.start : 0f;

    // already: 이어하기면 저장할 때까지 흐른 시간부터 (1.0.4~, 예전엔 00:00 부터 다시 셈)
    public static void Create(float already = 0f)
    {
        // 탄약 패널이 있는 화면 캔버스 (HUD 와 같은 곳)
        Canvas canvas = null;
        foreach (Canvas cv in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (cv.isRootCanvas && cv.renderMode != RenderMode.WorldSpace && cv.transform.Find("AmmoPanel") != null) { canvas = cv; break; }
        if (canvas == null) canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        // 어두운 칸 위에 시간
        GameObject box = new GameObject("RunClockBox", typeof(RectTransform), typeof(Image));
        RectTransform br = box.GetComponent<RectTransform>();
        br.SetParent(canvas.transform, false);
        // 1.1~: 맨 뒤(SetAsLastSibling)에 두면 페이드 · 일시정지 · 보상 창 위에도 떠 있었음 → HUD 순서 안(탄약 패널 바로 뒤)
        Transform ammoPanel = canvas.transform.Find("AmmoPanel");
        if (ammoPanel != null) br.SetSiblingIndex(ammoPanel.GetSiblingIndex() + 1);
        else br.SetAsLastSibling();
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
        br.pivot = new Vector2(0.5f, 1f);
        br.sizeDelta = new Vector2(150f, 44f);
        br.anchoredPosition = new Vector2(0f, -10f);
        Image bg = box.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.04f, 0.07f, 0.7f);
        bg.raycastTarget = false;
        GameObject go = new GameObject("RunClock", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(br, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) t.font = UIKit.Font;
        if (UIKit.FontMaterial != null) t.fontSharedMaterial = UIKit.FontMaterial;
        t.fontSize = 30f;
        t.color = new Color(1f, 0.92f, 0.75f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        RunClock c = go.AddComponent<RunClock>();
        c.text = t;

        // 1.0.4~: 저주 제단에서 저주를 받았으면 장이 끝날 때까지 시계 오른쪽에 보라색 저주 표시 (예전엔 배너 한 번뿐이라 잊어버렸음)
        GameObject cg = new GameObject("CurseTag", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform cr = cg.GetComponent<RectTransform>();
        cr.SetParent(br, false);
        cr.anchorMin = cr.anchorMax = new Vector2(1f, 0.5f);
        cr.pivot = new Vector2(0f, 0.5f);
        cr.sizeDelta = new Vector2(420f, 40f);
        cr.anchoredPosition = new Vector2(10f, 0f);
        TextMeshProUGUI ct = cg.GetComponent<TextMeshProUGUI>();
        if (UIKit.Font != null) ct.font = UIKit.Font;
        if (UIKit.FontMaterial != null) ct.fontSharedMaterial = UIKit.FontMaterial;
        ct.fontSize = 20f;
        ct.color = new Color(0.85f, 0.6f, 1f);
        ct.alignment = TextAlignmentOptions.MidlineLeft;
        ct.enableWordWrapping = false;
        ct.outlineWidth = 0.2f;
        ct.outlineColor = new Color32(0, 0, 0, 255);
        ct.raycastTarget = false;
        ct.text = "";
        c.curseText = ct;
        c.start = Time.time - Mathf.Max(0f, already);
        instance = c;
        // 1초마다 글자가 바뀌므로 하위 캔버스로 (HUD 루트를 다시 묶지 않게 · 1.2.4 HUD 최적화와 같은 규칙, 클릭 없음)
        UiScaler.Isolate(box, false);
    }

    void Update()
    {
        int s = Mathf.FloorToInt(Time.time - start);
        if (text != null && s != shown) { shown = s; text.text = EndlessMode.Clock(s); }
        if (curseText != null && (Curse.Active != curseShown || Loc.Current != curseLang))
        {
            curseShown = Curse.Active;
            curseLang = Loc.Current;
            curseText.text = curseShown ? Loc.T("저주") + "  <size=85%>" + Loc.T("적 체력 +30% · 보상 +50%") + "</size>" : "";
        }
    }

    TextMeshProUGUI curseText;
    bool curseShown;
    Loc.Lang curseLang;
}
