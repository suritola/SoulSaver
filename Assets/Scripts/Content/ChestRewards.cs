using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 보물 상자 보상 (1.1~): ContentRegistry.Rewards 에서 뽑음 (TreasureChest.OpenChest 가 부름)
//   3택: 칸마다 일반 · 희귀를 굴림 (상자 전체로 일반 70% · 희귀 25% · 전설 5%, 중간 보스 · 정예 상자는 전설 15%)
//   전설: 고르지 않고 룰렛이 돈 뒤 보상 3~5개가 한꺼번에 · 진화할 수 있는 스킬이 있으면 그 자리에서 진화 (진화 촉매)
//   한 판에 전설 최대 2번 · 진화 촉매 최대 2번 (RunSave 에 기록) · 체력 30% 아래면 회복 보상 가중치 ×3
public static class ChestRewards
{
    const float LegendChance = 0.05f, EliteLegendChance = 0.15f;
    const float RareShare = 0.25f / 0.95f;          // 전설이 아닐 때 희귀 비율 (전체로 25%)
    const float LegendRareShare = 0.5f;             // 룰렛은 희귀가 더 잘 나옴
    public const int MaxLegends = 2, MaxCatalysts = 2;
    const float LowHp = 0.3f, LowHpHealMul = 3f;

    // 이번 판 전설 · 진화 촉매 횟수
    public static int Legends { get; private set; }
    public static int Catalysts { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name != "GameScene") return;
            Legends = Catalysts = 0;
        };
    }

    public static void SaveRun(RunSave.Data d)
    {
        d.chestLegends = Legends;
        d.chestCatalysts = Catalysts;
    }

    public static void RestoreRun(RunSave.Data d)
    {
        Legends = Mathf.Clamp(d.chestLegends, 0, MaxLegends);
        Catalysts = Mathf.Clamp(d.chestCatalysts, 0, MaxCatalysts);
    }

    // 상자 하나를 여는 동안의 정보 (보상 설명과 적용이 같은 대상을 가리키게, 카드 · 트리 칸 · 잠든 스킬은 처음에 한 번만 고름)
    public class Ctx
    {
        public PlayerController player;
        public LevelShop shop;
        public SpecialAbilities specials;
        public StageManager sm;
        public Vector3 at;
        public int slot;
        public int cardId = -1, dormantId = -1;
        public SpecialAbilities.SoulNode node;

        public int Coins => 20 + 10 * slot;
        public int Shards => 20 + 10 * slot;
        public string CardName => shop != null ? shop.ChestCardName(cardId) : "";
        public string DormantName => specials != null ? specials.DormantName(dormantId) : "";
    }

    static Ctx MakeCtx(PlayerController p, Vector3 at)
    {
        Ctx c = new Ctx
        {
            player = p,
            shop = Object.FindFirstObjectByType<LevelShop>(),
            specials = SpecialAbilities.SharedInstance,
            sm = StageManager.Instance,
            at = at,
        };
        c.slot = c.sm != null ? Chapters.SlotOf(c.sm.CurrentStage) : 0;
        if (c.shop != null) c.cardId = c.shop.ChestCardCandidate();
        if (c.specials != null)
        {
            c.node = c.specials.ChestNodeCandidate();
            c.dormantId = c.specials.DormantCandidate();
        }
        return c;
    }

    static bool Usable(RewardDef r, Ctx c) => r.apply != null && (r.available == null || r.available(c));

    static float WeightOf(RewardDef r, Ctx c)
    {
        float w = r.weight;
        if (r.heal && c.player != null && c.player.PlayerHealth < c.player.PlayerMaxHealth * LowHp) w *= LowHpHealMul;
        return w;
    }

    // 겹치지 않게 하나 (rarity 가 비면 다른 등급에서)
    static RewardDef Draw(Ctx c, RewardRarity rarity, List<RewardDef> taken)
    {
        RewardDef pick = DrawOf(c, rarity, taken);
        if (pick == null) pick = DrawOf(c, rarity == RewardRarity.Rare ? RewardRarity.Common : RewardRarity.Rare, taken);
        return pick;
    }

    static RewardDef DrawOf(Ctx c, RewardRarity rarity, List<RewardDef> taken)
    {
        float total = 0f;
        foreach (RewardDef r in ContentRegistry.Rewards)
            if (r.rarity == rarity && !taken.Contains(r) && Usable(r, c)) total += WeightOf(r, c);
        if (total <= 0f) return null;
        float roll = Random.Range(0f, total);
        RewardDef last = null;
        foreach (RewardDef r in ContentRegistry.Rewards)
        {
            if (r.rarity != rarity || taken.Contains(r) || !Usable(r, c)) continue;
            last = r;
            roll -= WeightOf(r, c);
            if (roll < 0f) return r;
        }
        return last;
    }

    // ================================================================= 상자 열기
    public static IEnumerator Open(PlayerController p, Vector3 at, bool elite)
    {
        Ctx c = MakeCtx(p, at);
        float legend = Legends >= MaxLegends ? 0f : elite ? EliteLegendChance : LegendChance;
        if (Random.value < legend)
        {
            yield return Legend(c);
            yield break;
        }

        List<RewardDef> picks = new List<RewardDef>();
        for (int i = 0; i < 3; i++)
        {
            RewardDef r = Draw(c, Random.value < RareShare ? RewardRarity.Rare : RewardRarity.Common, picks);
            if (r != null) picks.Add(r);
        }
        if (picks.Count == 0) { GiveCoins(c); yield break; }
        ChoiceUI.Option[] options = new ChoiceUI.Option[picks.Count];
        for (int i = 0; i < picks.Count; i++)
        {
            RewardDef r = picks[i];
            string desc = r.describe != null ? r.describe(c) : "";
            if (r.rarity == RewardRarity.Rare) desc = "<color=#7FD3FF>" + Loc.T("chest.rare") + "</color>\n" + desc;
            options[i] = new ChoiceUI.Option(r.nameKey, desc, r.color, r.icon);
        }
        int pick = 0;
        yield return ChoiceUI.Run(elite ? "chest.elite.title" : "chest.title", "chest.pick", options, i => pick = i);
        picks[Mathf.Clamp(pick, 0, picks.Count - 1)].apply(c);
    }

    // ================================================================= 전설: 룰렛 → 보상 3~5개 (+ 진화 촉매)
    static IEnumerator Legend(Ctx c)
    {
        Legends++;
        int count = Random.Range(3, 6);
        List<RewardDef> picks = new List<RewardDef>();
        for (int i = 0; i < count; i++)
        {
            RewardDef r = Draw(c, Random.value < LegendRareShare ? RewardRarity.Rare : RewardRarity.Common, picks);
            if (r != null) picks.Add(r);
        }
        // 진화 촉매: 재료를 모두 가진 진화가 있으면 그 재료를 최대로 (체험판도 스킬 진화는 그대로라 같은 기준 · 「숙련」 카드와 맞춤)
        LevelShop.SkillEvo evo = null;
        if (Catalysts < MaxCatalysts && c.shop != null) evo = c.shop.ChestCatalystCandidate();

        List<string> lines = new List<string>();
        if (evo != null) lines.Add("<color=#FFC94D>" + Loc.T("chest.catalyst.line").Replace("{0}", Loc.T(evo.name)) + "</color>");
        foreach (RewardDef r in picks) lines.Add("· " + (r.describe != null ? r.describe(c) : r.Name));

        yield return Roulette(picks);
        CodexSeen.Mark("sec.jackpot", Loc.T("chest.jackpot.name"));      // 1.2~ 발견형 도감 비밀

        ChoiceUI.Option[] options =
        {
            new ChoiceUI.Option("chest.jackpot.take", string.Join("\n", lines), new Color(1f, 0.78f, 0.25f), "chest"),
        };
        string sub = Loc.T("chest.jackpot.desc").Replace("{0}", (picks.Count + (evo != null ? 1 : 0)).ToString());
        yield return ChoiceUI.Run("chest.legend.title", sub, options, null);

        foreach (RewardDef r in picks) r.apply(c);
        if (evo != null && c.shop.ChestCatalyst(evo)) Catalysts++;
        Fx.Spawn("fx_soulburst", c.at, 6f, new Color(1f, 0.8f, 0.3f), 18f);
        ShockRing.Spawn(c.at, 0.5f, 7f, 0.6f, new Color(1f, 0.85f, 0.4f), 0.4f);
    }

    // 룰렛 연출 (게임을 멈추고 약 2초): 그림이 빠르게 돌다 느려지며 멈춤 · 틱 소리가 점점 높아지고 마지막에 상승 화음
    static IEnumerator Roulette(List<RewardDef> picks)
    {
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) yield break;
        while (ChoiceUI.Open) yield return null;
        UIKit.EnsureStyle();
        float before = Time.timeScale;
        Time.timeScale = 0f;
        ChoiceUI.Open = true;                       // ESC 메뉴 · 필살기 등이 끼어들지 않게 (고르기 창과 같게)
        TooltipUI.Hide();

        GameObject go = new GameObject("ChestRoulette", typeof(RectTransform));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.rootCanvas.transform, false);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.SetAsLastSibling();
        SoulTreeUI.OnTop(go, 620);
        RectTransform back = UIKit.Rect("Back", r, Vector2.zero, Vector2.zero);
        back.anchorMin = Vector2.zero; back.anchorMax = Vector2.one;
        back.offsetMin = back.offsetMax = Vector2.zero;
        back.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.02f, 0.01f, 0.85f);
        UIKit.Text(r, "chest.legend.title", 64f, new Color(1f, 0.82f, 0.3f), new Vector2(0f, 300f), new Vector2(1400f, 90f));
        UIKit.Text(r, "chest.legend.sub", 28f, new Color(0.93f, 0.9f, 0.84f), new Vector2(0f, 235f), new Vector2(1400f, 50f));

        RectTransform glow = UIKit.Rect("Glow", r, Vector2.zero, new Vector2(420f, 420f));
        Image gi = glow.gameObject.AddComponent<Image>();
        gi.sprite = SpecialAbilities.GlowSprite;
        gi.color = new Color(1f, 0.8f, 0.3f, 0.45f);
        gi.raycastTarget = false;
        RectTransform frame = UIKit.Rect("Frame", r, Vector2.zero, new Vector2(260f, 260f));
        Image fi = frame.gameObject.AddComponent<Image>();
        fi.sprite = UIKit.ButtonSprite;
        fi.type = Image.Type.Sliced;
        fi.color = new Color(0.25f, 0.18f, 0.1f);
        fi.raycastTarget = false;
        RectTransform ir = UIKit.Rect("Icon", frame, Vector2.zero, new Vector2(190f, 190f));
        Image icon = ir.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        // 돌릴 그림: 뽑힌 보상 그림 + 상자 보상 그림 전부
        List<Sprite> sprites = new List<Sprite>();
        foreach (RewardDef d in ContentRegistry.Rewards)
        {
            Sprite s = Resources.Load<Sprite>(ChoiceUI.IconPath(d.icon));
            if (s != null && !sprites.Contains(s)) sprites.Add(s);
        }
        if (sprites.Count == 0) icon.enabled = false;

        // 1.1~ 소리는 따로 둔 오브젝트에서: 룰렛 창(go)과 함께 지우면 상승 화음(1.1초)이 0.8초에 뚝 끊겼음 → 화음이 끝난 뒤 지움
        GameObject sfx = new GameObject("ChestRouletteSfx");
        AudioSource src = sfx.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.ignoreListenerPause = true;
        float vol = 0.35f * GameSettings.SfxVolume;

        float step = 0.05f, pitch = 0.8f;
        int k = Random.Range(0, Mathf.Max(1, sprites.Count));
        while (step < 0.3f)
        {
            if (sprites.Count > 0) icon.sprite = sprites[k++ % sprites.Count];
            src.pitch = pitch;
            src.PlayOneShot(Tick, vol);
            pitch = Mathf.Min(2f, pitch + 0.04f);
            frame.localScale = Vector3.one * 1.08f;
            float until = Time.unscaledTime + step;
            while (Time.unscaledTime < until)
            {
                frame.localScale = Vector3.Lerp(frame.localScale, Vector3.one, 0.3f);
                glow.localEulerAngles = new Vector3(0f, 0f, Time.unscaledTime * 90f);
                yield return null;
            }
            step *= 1.13f;
        }
        // 멈춤: 금빛 번쩍임 + 상승 화음
        if (picks.Count > 0)
        {
            Sprite last = Resources.Load<Sprite>("Icons/choice_chest");
            if (last != null) icon.sprite = last;
        }
        src.pitch = 1f;
        src.PlayOneShot(Chord, vol);
        fi.color = new Color(1f, 0.82f, 0.3f);
        float end = Time.unscaledTime + 0.8f;
        while (Time.unscaledTime < end)
        {
            float t = 1f - (end - Time.unscaledTime) / 0.8f;
            frame.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t * Mathf.PI));
            glow.localScale = Vector3.one * (1f + t);
            gi.color = new Color(1f, 0.8f, 0.3f, 0.45f * (1f - t) + 0.2f);
            yield return null;
        }

        Object.Destroy(go);
        Object.Destroy(sfx, 0.5f);
        ChoiceUI.Open = false;
        Time.timeScale = before > 0f ? before : 1f;
    }

    // 코드에서 만드는 소리 (UISound 방식): 짧은 틱 · 도-미-솔-도 상승 화음
    const int Rate = 44100;
    static AudioClip tick, chord;
    static readonly float[] ChordNotes = { 523.25f, 659.25f, 783.99f, 1046.5f };     // 도 · 미 · 솔 · 도
    static AudioClip Tick => tick != null ? tick : tick = Make("chest_tick", 0.045f, t => Mathf.Sin(2f * Mathf.PI * 1500f * t) * Mathf.Pow(1f - t / 0.045f, 3f) * 0.5f);
    static AudioClip Chord => chord != null ? chord : chord = Make("chest_chord", 1.1f, t =>
    {
        float v = 0f;
        for (int i = 0; i < ChordNotes.Length; i++)
        {
            float s = t - i * 0.08f;
            if (s < 0f) continue;
            float env = Mathf.Min(1f, s / 0.01f) * Mathf.Exp(-s * 3.2f);
            v += (Mathf.Sin(2f * Mathf.PI * ChordNotes[i] * s) + 0.3f * Mathf.Sin(4f * Mathf.PI * ChordNotes[i] * s)) * env;
        }
        return v * 0.22f;
    });

    static AudioClip Make(string name, float dur, System.Func<float, float> wave)
    {
        int n = Mathf.CeilToInt(dur * Rate);
        float[] data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
        AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ================================================================= 보상 적용 (판을 바꾸는 것은 RunSave 기록으로 이어하기에서 다시)
    public static void GiveCoins(Ctx c)
    {
        Coin coin = Object.FindFirstObjectByType<Coin>();
        if (coin != null) coin.AddCoin(c.Coins);
    }

    public static void GiveShards(Ctx c) => SoulShards.Add(c.Shards, c.at, false);

    public static void GiveMaxHp(Ctx c, float amount)
    {
        if (c.player == null) return;
        c.player.PlayerMaxHealth += amount;
        c.player.PlayerHealth = c.player.PlayerMaxHealth;
        RunSave.Record("h" + Mathf.RoundToInt(amount));         // 이어할 때 최대 체력도 다시 (RunSave 'h')
    }

    public static void GiveHeal(Ctx c)
    {
        if (c.player == null) return;
        c.player.PlayerHealth = Mathf.Min(c.player.PlayerMaxHealth, c.player.PlayerHealth + c.player.PlayerMaxHealth * 0.5f);
    }

    public static void GiveLevel(Ctx c)
    {
        if (c.shop != null) c.shop.AddPending(1);              // 쌓인 레벨업 +1 (경험치 요구량은 그대로 · 저장할 때 값 그대로)
    }

    public static void GiveMight(Ctx c)
    {
        if (c.player == null) return;
        c.player.damage *= 1.05f;
        RunSave.Record("a5");                                   // RunSave 'a' (공격력 %)
    }

    public static void GiveCard(Ctx c)
    {
        if (c.shop != null) c.shop.ChestUpgradeCard(c.cardId);
    }

    public static void GiveNode(Ctx c)
    {
        if (c.specials != null) c.specials.ChestFreeNode(c.node);
    }

    public static void GiveReroll(Ctx c)
    {
        if (c.specials != null) c.specials.SetRerolls(c.specials.Rerolls + 1);     // 저장할 때 값 그대로 (RunSave rerolls)
    }

    public static void GiveDormant(Ctx c)
    {
        if (c.sm != null && c.dormantId >= 0) c.sm.ReplayPicks(new[] { c.dormantId });     // 특수 능력 고르기와 같은 길 (RunSave 's' 기록 · SpecialAbilities.Equip)
    }
}
