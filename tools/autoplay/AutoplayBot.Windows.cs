using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// 자동 플레이 봇 — 판 중 창 처리 (레벨업 · 갈래 입문 · 갈림길 · 무기 진화 · 스킬 진화 · 고르기(초월) · 영혼 트리 · 떠돌이 상점)
// 사람처럼 창이 열리고 잠깐 본 뒤 고름 (프레임 수로 기다림 · 멈춘 화면이라 게임 시간은 흐르지 않음)
// 모르는 창이 멈춘 화면으로 오래 남아 있으면 안전장치: 보이는 창의 첫 선택지를 고르고 기록
public partial class AutoplayBot
{
    LevelShop shop;
    Shop stallShop;
    int windowFrames;          // 지금 창이 열려 있은 프레임
    string windowName = "";
    int pausedFrames;          // 알 수 없는 이유로 멈춘 프레임
    float nextTreeBuy, nextPendingOpen;
    int pathForced1 = -1, pathForced2 = -2;     // -2 = 정하지 않음(무작위) · -1 = 외길(두 번째)
    public static readonly List<string> WindowLog = new List<string>();

    void WindowsBegin()
    {
        pathForced1 = ParsePath(want1);
        pathForced2 = want2 == "single" ? -1 : ParsePath(want2);
        WindowLog.Clear();
        // 다시 뽑기 안내가 이미 지난 계정 (Auto 는 이 값을 올리지 않음)
        if (Prefs.GetInt("intro.lvopens", 0) < 3) Prefs.SetInt("intro.lvopens", 3);
    }

    // "0"~"4" · 갈래 키(g.snipe) · 상수 이름(Snipe) · 갈래 이름(관통 저격) → 번호, 그 밖(random 등) → -2
    int ParsePath(string s)
    {
        if (string.IsNullOrEmpty(s) || s == "random") return -2;
        if (int.TryParse(s, out int n) && n >= 0 && n < Paths.Count) return n;
        HeroPaths h = Paths.Data(hero);
        if (h == null) return -2;
        for (int i = 0; i < h.Defs.Length; i++)
            if (string.Equals(h.Defs[i].key, s, StringComparison.OrdinalIgnoreCase) || h.Defs[i].name == s) return i;
        foreach (var f in h.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            if (f.IsLiteral && f.FieldType == typeof(int) && string.Equals(f.Name, s, StringComparison.OrdinalIgnoreCase)) return (int)f.GetRawConstantValue();
        AutoplayRunner.Log("?? 모르는 갈래 '" + s + "' → 무작위");
        return -2;
    }

    bool AnyWindowOpen()
    {
        if (shop == null) shop = FindFirstObjectByType<LevelShop>();
        if (stallShop == null) stallShop = FindFirstObjectByType<Shop>();
        return (shop != null && shop.IsOpen) || ChoiceUI.Open || WeaponEvolutionUI.Open || SkillEvolutionUI.Open || SoulTreeUI.IsOpen
            || (stallShop != null && stallShop.isShopOpen) || ESCmenu.IsOpen;
    }

    string OpenWindowName()
    {
        if (shop != null && shop.IsOpen) return "레벨업";
        if (ChoiceUI.Open) return "고르기";
        if (WeaponEvolutionUI.Open) return "무기진화";
        if (SkillEvolutionUI.Open) return "스킬진화";
        if (SoulTreeUI.IsOpen) return "영혼트리";
        if (stallShop != null && stallShop.isShopOpen) return "상점";
        if (ESCmenu.IsOpen) return "ESC";
        if (BossUltimate.CutscenePlaying) return "결계컷신";
        if (StoryDirector.Playing) return "이야기";
        return Time.timeScale <= 0f ? "?멈춤" : "-";
    }

    int ThinkFrames => 4 + rnd.Next(6);

    void HandleWindows()
    {
        string name = OpenWindowName();
        if (name != windowName) { windowName = name; windowFrames = 0; thinkAt = ThinkFrames; }
        windowFrames++;
        try
        {
            switch (name)
            {
                case "레벨업": LevelUpWindow(); break;
                case "고르기": ChoiceWindow(); break;
                case "무기진화": WeaponEvoWindow(); break;
                case "스킬진화": SkillEvoWindow(); break;
                case "영혼트리": SoulTreeUI.Close(); break;
                case "상점": StallShopWindow(); break;
                case "ESC": Call(FindFirstObjectByType<ESCmenu>(), "onPressResume"); break;
                case "-": OpenPendingLevels(); BuySoulTree(); break;
            }
        }
        catch (Exception e) { AutoplayRunner.Log("창 처리 예외 (" + name + ") " + (e.InnerException ?? e).Message); }

        // 안전장치: 알 수 없게 멈췄거나 아는 창이 너무 오래 열려 있음
        bool cinematic = name == "결계컷신" || name == "이야기";
        if (Time.timeScale <= 0f && !cinematic) pausedFrames++; else pausedFrames = 0;
        if (pausedFrames == 900 || (windowFrames == 900 && name != "-" && !cinematic))
        {
            rec.unknownWindows++;
            string line = "멈춤 안전장치: 창 " + name + " · " + DescribeUi();
            AutoplayRunner.Log("?? " + line);
            if (WindowLog.Count < 20) WindowLog.Add(line);
            ForceFirstChoice();
            windowFrames = 0; pausedFrames = 1;
        }
    }

    int thinkAt;

    // ================================================================= 레벨업 · 입문 · 갈림길
    void LevelUpWindow()
    {
        if (!Get(shop, "selectReady", false)) return;
        if (windowFrames < thinkAt) return;
        int[] ids = { Get(shop, "first", 0), Get(shop, "second", 0), Get(shop, "third", 0) };
        int slot;
        if (ids.Any(Paths.IsSeed) || ids.Contains(Paths.SingleId))
        {
            bool cross = ids[2] == Paths.SingleId;
            int want = cross ? pathForced2 : pathForced1;
            slot = -1;
            if (cross && want == -1) slot = 2;
            else if (want >= 0)
            {
                slot = Array.IndexOf(ids, Paths.SeedFirstId + want);
                if (slot < 0 && want != Paths.A)
                {
                    // 원하는 갈래가 이번 세 장에 없음 → 첫 칸을 그 입문으로 바꿔 고름 (측정용 · 실제 플레이라면 다시 뽑기를 썼을 상황)
                    Set(shop, "first", Paths.SeedFirstId + want);
                    slot = 0;
                    AutoplayRunner.Log("갈래 지정: 입문 칸에 없어서 " + (want) + " 로 바꿔 고름");
                }
            }
            if (slot < 0) slot = rnd.Next(3);
            AutoplayRunner.Log((cross ? "갈림길 " : "입문 ") + string.Join(",", ids) + " → " + ids[Mathf.Clamp(slot, 0, 2)] + " (Lv " + (Hostile.Player != null ? Hostile.Player.level : 0) + ")");
        }
        else slot = BestCard(ids);
        Call(shop, "ConfirmSlot", slot);
    }

    // 레벨업 카드 점수: 고른 갈래 카드 · 화력 위주 · 체력이 낮으면 생존 · 진화가 가까운 카드 · 실력이 낮을수록 잡음
    int BestCard(int[] ids)
    {
        PlayerController p = Hostile.Player;
        float hpFrac = p != null ? p.PlayerHealth / Mathf.Max(1f, p.PlayerMaxHealth) : 1f;
        int best = 0; float bs = -1e9f;
        for (int k = 0; k < 3; k++)
        {
            int id = ids[k];
            float s = CardValue(id, hpFrac);
            try { s *= Mathf.Clamp(Convert.ToSingle(Call(shop, "PathDrawWeight", id)), 0.8f, 2f); } catch { }
            s += Gauss() * Mathf.Lerp(1.6f, 0.2f, skill);
            if (s > bs) { bs = s; best = k; }
        }
        return best;
    }

    float CardValue(int id, float hpFrac)
    {
        if (LevelShop.IsGrowth(id)) return (id - LevelShop.GrowthFirstId) switch { 0 => 2.2f, 1 => hpFrac < 0.5f ? 2.4f : 1.4f, 2 => 2f, _ => 0.8f };
        if (id == LevelShop.SupplyId) return hpFrac < 0.4f ? 2.5f : 0.3f;
        int path = Paths.PathOfCard(hero, id);
        if (path >= 0)
        {
            float v = 3f;
            int r = Paths.Rank(path);
            if (r == Paths.ResI - 1 || r == Paths.ResII - 1) v += 0.8f;       // 공명 문턱 바로 앞
            return v;
        }
        if (id >= LevelShop.SigFirstId && id < LevelShop.SigFirstId + 10) return 2.3f;
        // 공용 · 영웅 전용 카드 (0 관통 · 3 눈빛 · 6 멀티 샷 · 7 밀어내기 · 11 은 영웅 전용으로 바뀌기도 함 · 12 · 13 영웅 전용)
        return id switch
        {
            0 => 2.2f, 3 => 2f, 6 => 2.6f, 7 => 0.9f, 11 => 1.3f, 12 => 2.2f, 13 => 2.2f,
            1 => 0.4f, 4 => 1.4f, 5 => 0.9f,
            8 => hpFrac < 0.6f ? 2.4f : 1.5f,
            9 => 1.7f,
            10 => hpFrac < 0.6f ? 2f : 1.1f,
            _ => 1f,
        };
    }

    // 쌓인 레벨업: 시간이 흐르는 화면에서 바로 열어 고름 (사람은 위험할 때 잠깐 미룸)
    void OpenPendingLevels()
    {
        if (shop == null || shop.PendingLevels <= 0 || shop.IsOpen || Time.timeScale != 1f) return;
        if (stallShop != null && stallShop.isShopOpen) return;
        if (Time.time < nextPendingOpen) return;
        nextPendingOpen = Time.time + Mathf.Lerp(1.2f, 0.3f, skill) * (lastDanger > 5f ? 2f : 1f);
        Call(shop, "TryOpenPending");
    }

    // ================================================================= 무기 진화 (보스마다) · 스킬 진화 알림
    void WeaponEvoWindow()
    {
        if (windowFrames < thinkAt + 10) return;
        WeaponEvolutionUI ui = FindFirstObjectByType<WeaponEvolutionUI>();
        if (ui == null) return;
        Set(ui, "skipIntro", true);
        RectTransform[] cards = Get<RectTransform[]>(ui, "cards");
        int n = cards != null && cards.Length > 0 ? cards.Length : 1;
        if (Get(ui, "selected", -1) < 0) { int pick = rnd.Next(n); Set(ui, "selected", pick); AutoplayRunner.Log("무기 진화 " + (pick + 1) + "/" + n); }
        try { Call(ui, "Confirm"); } catch { Set(ui, "confirmed", true); }
    }

    void SkillEvoWindow()
    {
        SkillEvolutionUI ui = FindFirstObjectByType<SkillEvolutionUI>();
        if (ui != null && windowFrames > 3) Set(ui, "closeRequested", true);
    }

    // ================================================================= 고르기 창 (무한 모드는 초월 카드 · 그 밖은 안전하게 처리)
    void ChoiceWindow()
    {
        ChoiceUI ui = FindFirstObjectByType<ChoiceUI>();
        if (ui == null || !Get(ui, "ready", false) || windowFrames < thinkAt) return;
        if (Get(ui, "confirmed", -1) >= 0) return;
        RectTransform[] cards = Get<RectTransform[]>(ui, "cards");
        int n = cards?.Length ?? 0;
        if (n == 0) { Set(ui, "confirmed", 0); return; }
        int best = 0; float bs = -1e9f;
        var texts = new List<string>();
        for (int i = 0; i < n; i++)
        {
            string t = cards[i] == null ? "" : string.Join(" ", cards[i].GetComponentsInChildren<TMP_Text>(true).Select(x => x.text));
            texts.Add(t.Length > 40 ? t.Substring(0, 40) : t);
            float s = 1f;
            foreach (string w in new[] { "공격", "피해", "화력", "속도", "치명" }) if (t.Contains(w)) s += 1f;
            foreach (string w in new[] { "체력", "회복", "보호" }) if (t.Contains(w)) s += 0.6f;
            s += Gauss() * Mathf.Lerp(1.5f, 0.3f, skill);
            if (s > bs) { bs = s; best = i; }
        }
        AutoplayRunner.Log("고르기 창 → " + (best + 1) + "/" + n + " " + texts[best].Replace("\n", " "));
        Set(ui, "confirmed", best);
    }

    // ================================================================= 영혼 트리 (판 중 영혼 조각): 창을 열지 않고 추천 칸 → 가장 싼 칸 · 다 배우면 정련
    void BuySoulTree()
    {
        if (Time.time < nextTreeBuy) return;
        nextTreeBuy = Time.time + Mathf.Lerp(8f, 2.5f, skill) * (0.7f + R01() * 0.6f);
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        if (sp == null || StageManager.Instance == null || StageManager.Instance.IsMenuOpen) return;
        for (int i = 0; i < 4; i++)
        {
            SpecialAbilities.SoulNode n = sp.QuickBuy();
            if (n == null) break;
            AutoplayRunner.Log("영혼 트리 " + n.name + " (조각 " + SoulShards.Amount + " 남음)");
        }
        if (sp.CanRefine() && sp.Refine()) AutoplayRunner.Log("영혼 정련 " + sp.Refines);
    }

    // ================================================================= 떠돌이 상점: 제단에 다가가 열고 공격력 위주로 삼
    ShopStall stall;
    float nextStallFind;
    Vector2 StallGoal(Vector2 me)
    {
        if (stall == null && Time.time >= nextStallFind)
        {
            nextStallFind = Time.time + 0.5f;
            ShopStall[] all = FindObjectsByType<ShopStall>(FindObjectsSortMode.None);
            float bd = 30f;
            foreach (ShopStall s in all) { float d = ((Vector2)s.transform.position - me).magnitude; if (d < bd) { bd = d; stall = s; } }
        }
        return stall != null ? (Vector2)stall.transform.position : Vector2.zero;
    }

    void TryOpenStall(Vector2 me)
    {
        if (stall == null || stallShop == null || Time.timeScale != 1f || stallShop.isShopOpen) return;
        if (((Vector2)stall.transform.position - me).magnitude > stall.openDistance * 0.9f) return;
        if (stallShop.OpenFromStall())
        {
            ShopStall.PlayerNear = false;
            Destroy(stall.gameObject);
            stall = null;
            shopBuys = 0;
        }
    }

    int shopBuys;
    void StallShopWindow()
    {
        if (windowFrames < thinkAt + 6) return;
        if (windowFrames % 3 != 0) return;
        Coin c = stallShop.coind;
        int before = c != null ? c.coins : 0;
        PlayerController p = Hostile.Player;
        float hpFrac = p != null ? p.PlayerHealth / Mathf.Max(1f, p.PlayerMaxHealth) : 1f;
        if (hpFrac < 0.6f && shopBuys < 30) { try { Call(stallShop, "BuyPotion", 0); } catch { } }
        if (c != null && c.coins == before) stallShop.OnPressB1();
        if (c != null && c.coins == before && shopBuys % 4 == 3) stallShop.OnPressB5();
        if (c != null && c.coins == before) { try { Call(stallShop, "BuyPotion", 2); } catch { } }
        if (c != null && c.coins == before || shopBuys > 40)
        {
            AutoplayRunner.Log("상점 닫음 · 산 횟수 " + shopBuys + " · 코인 " + (c != null ? c.coins : 0));
            Call(stallShop, "ToggleShop");
            return;
        }
        shopBuys++;
    }

    // ================================================================= 안전장치
    string DescribeUi()
    {
        var names = new List<string>();
        foreach (Canvas cv in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (cv.isRootCanvas && cv.isActiveAndEnabled && names.Count < 12) names.Add(cv.name);
        return "timeScale " + Time.timeScale + " · 캔버스 " + string.Join(",", names) + " · 컷신 " + PlayerController.Cinematic + " · 진화 중 " + (StageManager.Instance != null && StageManager.Instance.IsEvolving);
    }

    void ForceFirstChoice()
    {
        try
        {
            if (shop != null && shop.IsOpen) { Set(shop, "selectReady", true); Call(shop, "ConfirmSlot", 0); return; }
            ChoiceUI ui = FindFirstObjectByType<ChoiceUI>();
            if (ui != null) { Set(ui, "ready", true); Set(ui, "confirmed", 0); return; }
            WeaponEvolutionUI we = FindFirstObjectByType<WeaponEvolutionUI>();
            if (we != null) { Set(we, "selected", 0); Set(we, "confirmed", true); return; }
            SkillEvolutionUI se = FindFirstObjectByType<SkillEvolutionUI>();
            if (se != null) { Set(se, "closeRequested", true); return; }
            if (SoulTreeUI.IsOpen) { SoulTreeUI.Close(); return; }
            if (stallShop != null && stallShop.isShopOpen) { Call(stallShop, "ToggleShop"); return; }
            // 정말 모르는 멈춤: 화면에 보이는 첫 버튼을 누름
            foreach (UnityEngine.UI.Button b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (b.isActiveAndEnabled && b.interactable) { AutoplayRunner.Log("?? 첫 버튼 누름: " + b.name); b.onClick.Invoke(); return; }
        }
        catch (Exception e) { AutoplayRunner.Log("안전장치 예외 " + (e.InnerException ?? e).Message); }
    }
}
