using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// 스킨 소리 (스킨 시청각 개편 1단계에서 SkinFx.cs 에서 떼어냄)
//   ① 예전 소리(피격 18 · 처치 3 · 필살기 14): SpecialFeedback 합성기에 등록 — 이제 끼운 스킨 것만 (Register)
//   ② 새 칸 소리: SkinLooks 의 소리 레시피를 음색 사전(4-0절)으로 합성한 AudioClip — 끼운 스킨 것만 (EnsureAll), 바꾸면 안 쓰는 것은 버림
//   ③ 재생 규칙(2-6절): 음높이는 5단계로만 · 같은 0.1초 안에는 우선순위 높은 것 하나 · 동시 4개 넘으면 새 소리 생략
public static class SkinAudio
{
    const int Rate = 44100;

    // ================================================================= ① 예전 소리 등록
    // 2026-11-04~ 판 시작 · 상점마다 35개를 전부 합성하던 것 → 끼운 스킨이 쓰는 이름만 (다시 불러도 이미 있는 것은 건너뜀)
    public static void Register(SpecialFeedback fx)
    {
        if (fx == null) return;
        HashSet<string> need = new HashSet<string>();
        void Want(SkinDef d)
        {
            if (d == null) return;
            if (d.hurtSound != null) need.Add(d.hurtSound);
            if (d.killSound != null) need.Add(d.killSound);
            if (d.kind == SkinKind.Character && d.tier >= 3) need.Add(UltSound(d.id));
        }
        Want(SkinFx.CharSkin); Want(SkinFx.WeaponSkin); Want(SkinFx.EffectSkin);
        if (SkinFx.EffectSkin != null && SkinFx.EffectSkin.id == "skin_thunder") need.Add("sk_kill_zap");
        float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
        float Dec(float t, float d, float p) => Mathf.Pow(Mathf.Clamp01(1f - t / d), p);
        float N(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
        void Mk(string name, float dur, System.Func<float, float, System.Random, float> w) { if (need.Contains(name)) fx.MakeClip(name, dur, w); }
        // ---- 피격
        Mk("sk_hurt_low", 0.22f, (t, d, r) => (Sin(Mathf.Lerp(140f, 70f, t / d), t) * 0.7f + N(r) * 0.2f) * Dec(t, d, 2f));
        Mk("sk_hurt_light", 0.14f, (t, d, r) => (Sin(Mathf.Lerp(700f, 400f, t / d), t) * 0.5f + N(r) * 0.15f) * Dec(t, d, 2f));
        Mk("sk_hurt_whistle", 0.25f, (t, d, r) => Sin(Mathf.Lerp(1800f, 1200f, t / d), t) * 0.4f * Dec(t, d, 1.5f));
        Mk("sk_hurt_echo", 0.5f, (t, d, r) => (Sin(520f, t) + Sin(523f, t) * 0.8f) * 0.3f * (Dec(t, 0.15f, 2f) + 0.5f * Dec(Mathf.Max(0f, t - 0.18f), 0.3f, 2f)));
        Mk("sk_hurt_bell", 0.6f, (t, d, r) => (Sin(880f, t) + Sin(1320f, t) * 0.5f + Sin(2640f, t) * 0.2f) * 0.35f * Dec(t, d, 2.5f));
        Mk("sk_hurt_metal", 0.3f, (t, d, r) => (Sin(1900f, t) + Sin(2750f, t) * 0.7f + N(r) * 0.3f * Dec(t, 0.05f, 2f)) * 0.3f * Dec(t, d, 3f));
        Mk("sk_hurt_chime", 0.4f, (t, d, r) => (Sin(1568f, t) + Sin(2093f, t) * 0.6f) * 0.3f * Dec(t, d, 2f));
        Mk("sk_hurt_roar", 0.45f, (t, d, r) => (Sin(90f + 20f * Sin(9f, t), t) * 0.6f + N(r) * 0.35f) * Mathf.Sin(Mathf.PI * t / d) * 0.6f);
        Mk("sk_hurt_flap", 0.25f, (t, d, r) => N(r) * 0.45f * Mathf.Abs(Sin(18f, t)) * Dec(t, d, 1.5f));
        Mk("sk_hurt_smoke", 0.35f, (t, d, r) => N(r) * 0.35f * Mathf.Sin(Mathf.PI * t / d) * Dec(t, d, 1f));
        Mk("sk_hurt_void", 0.5f, (t, d, r) => (Sin(Mathf.Lerp(220f, 110f, t / d), t) * 0.5f + Sin(Mathf.Lerp(330f, 165f, t / d), t) * 0.35f) * Mathf.Sin(Mathf.PI * t / d) * 0.5f);
        Mk("sk_hurt_ice", 0.3f, (t, d, r) => (N(r) * 0.3f * Dec(t, 0.08f, 2f) + Sin(3136f, t) * 0.25f + Sin(4186f, t) * 0.15f) * Dec(t, d, 2.5f));
        Mk("sk_hurt_wind", 0.4f, (t, d, r) => N(r) * 0.3f * Mathf.Sin(Mathf.PI * t / d) * (0.6f + 0.4f * Sin(7f, t)));
        Mk("sk_hurt_bubble", 0.3f, (t, d, r) => Sin(300f + 400f * ((t * 12f) % 1f), t) * 0.35f * Dec(t, d, 1.5f));
        Mk("sk_hurt_paper", 0.22f, (t, d, r) => (N(r) * 0.4f * Dec(t, 0.05f, 2f) + N(r) * 0.18f * Mathf.Abs(Sin(60f, t))) * Dec(t, d, 1.5f));
        Mk("sk_hurt_cuckoo", 0.34f, (t, d, r) => (t < 0.14f ? Sin(1175f, t) : t > 0.17f ? Sin(880f, t) : 0f) * 0.35f * Dec(t % 0.17f, 0.15f, 1f));
        Mk("sk_hurt_beep", 0.22f, (t, d, r) => (t < 0.08f || (t > 0.12f && t < 0.2f) ? Mathf.Sign(Sin(2400f, t)) * 0.22f : 0f));
        Mk("sk_hurt_pixel", 0.25f, (t, d, r) => Mathf.Sign(Sin(Mathf.Floor(Mathf.Lerp(900f, 200f, t / d) / 100f) * 100f, t)) * 0.2f * Dec(t, d, 1.2f));
        // ---- 처치 (이펙트 스킨)
        Mk("sk_kill_petal", 0.3f, (t, d, r) => (Sin(1760f, t) * 0.4f + Sin(2349f, t) * 0.25f) * Dec(t, d, 2.5f));
        Mk("sk_kill_zap", 0.18f, (t, d, r) => (N(r) * 0.3f + Mathf.Sign(Sin(1800f + 900f * Sin(55f, t), t)) * 0.2f) * Dec(t, d, 2f));
        Mk("sk_kill_star", 0.5f, (t, d, r) => (Sin(2093f, t) + Sin(2637f, t) * 0.6f + Sin(3136f, t) * 0.4f) * 0.25f * Dec(t, d, 2.5f));
        // ---- 필살기 (전설 캐릭터 스킨)
        Mk("sk_ult_bell", 1.2f, (t, d, r) => (Sin(523f, t) + Sin(784f, t) * 0.6f + Sin(1046f, t) * 0.4f + Sin(1568f, t) * 0.2f) * 0.35f * Dec(t, d, 2f));
        Mk("sk_ult_roar", 1f, (t, d, r) => (Sin(70f + 25f * Sin(6f, t), t) * 0.6f + N(r) * 0.4f) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / d * 1.2f)) * 0.7f);
        Mk("sk_ult_whisper", 1f, (t, d, r) => N(r) * 0.3f * (0.5f + 0.5f * Sin(4f, t)) * Mathf.Sin(Mathf.PI * t / d) + Sin(Mathf.Lerp(200f, 120f, t / d), t) * 0.2f * Dec(t, d, 1f));
        Mk("sk_ult_choir", 1.3f, (t, d, r) => (Sin(440f, t) + Sin(554f, t) * 0.8f + Sin(659f, t) * 0.7f + Sin(880f, t) * 0.3f) * 0.2f * (0.7f + 0.3f * Sin(5f, t)) * Mathf.Sin(Mathf.PI * t / d));
        Mk("sk_ult_resonance", 1.1f, (t, d, r) => (Sin(330f, t) * Sin(3f, t) + Sin(495f, t) * 0.5f + Sin(990f, t) * 0.25f * Sin(11f, t)) * 0.35f * Dec(t, d, 1.5f));
        Mk("sk_ult_whistle", 1.4f, (t, d, r) => t < 0.35f
            ? Sin(2600f + 300f * Sin(28f, t), t) * 0.3f * Mathf.Min(1f, t / 0.02f)
            : t < 0.85f ? (Sin(Mathf.Lerp(110f, 45f, (t - 0.35f) / 0.5f), t) * 0.8f + N(r) * 0.15f * Dec(t - 0.35f, 0.05f, 2f)) * Dec(t - 0.35f, 0.5f, 1.6f)
            : N(r) * 0.4f * Mathf.Sin(Mathf.PI * (t - 0.85f) / 0.55f));
        Mk("sk_ult_click", 1.0f, (t, d, r) => t < 0.12f
            ? (N(r) * 0.5f + Sin(1800f, t) * 0.3f) * Dec(t, 0.12f, 3f)
            : t > 0.3f ? (Sin(Mathf.Lerp(150f, 55f, (t - 0.3f) / 0.5f), t) * 0.8f + N(r) * 0.2f * Dec(t - 0.3f, 0.04f, 2f)) * Dec(t - 0.3f, 0.7f, 1.8f) : 0f);
        Mk("sk_ult_clicks", 1.1f, (t, d, r) =>
        {
            float v = 0f;
            for (int k = 0; k < 12; k++)
            {
                float tt = t - k * 0.05f;
                if (tt >= 0f && tt < 0.03f) v += (N(r) * 0.4f + Sin(2200f, tt)) * 0.3f * Dec(tt, 0.03f, 2f);
            }
            float te = t - 0.75f;
            if (te >= 0f) v += Sin(Mathf.Lerp(900f, 400f, te / 0.3f), t) * 0.5f * Dec(te, 0.3f, 2f);
            return v;
        });
        Mk("sk_ult_dingding", 1.3f, (t, d, r) => t < 0.7f
            ? (Sin(196f, t) + Sin(294f, t) * 0.5f + Sin(98f, t) * 0.6f) * 0.35f * Dec(t, 0.7f, 2f)
            : (Sin(1319f, t) + Sin(1976f, t) * 0.5f) * 0.3f * Dec(t - 0.7f, 0.6f, 2.5f));
        Mk("sk_ult_snap", 1.0f, (t, d, r) => t < 0.08f
            ? (N(r) * 0.6f + Sin(2800f, t) * 0.3f) * Dec(t, 0.08f, 2f)
            : t < 0.18f ? 0f
            : t < 0.3f ? (N(r) * 0.5f + Sin(1800f, t) * 0.25f) * Dec(t - 0.18f, 0.12f, 2f)
            : Sin(Mathf.Lerp(1500f, 700f, (t - 0.3f) / 0.7f), t) * 0.2f * Dec(t - 0.3f, 0.7f, 2f) + N(r) * 0.05f * Dec(t - 0.3f, 0.5f, 2f));
        Mk("sk_ult_paper", 1.1f, (t, d, r) => t < 0.5f
            ? (Sin(Mathf.Lerp(380f, 900f, t / 0.5f) + 18f * Sin(30f, t), t) * 0.25f + N(r) * 0.12f) * Mathf.Sin(Mathf.PI * t / 0.5f)
            : N(r) * 0.55f * Dec(t - 0.5f, 0.6f, 2.2f) * (0.6f + 0.4f * Mathf.Abs(Sin(37f, t))));
        Mk("sk_ult_gong", 1.9f, (t, d, r) => (Sin(196f, t) + Sin(392f, t) * 0.6f + Sin(541f, t) * 0.45f + Sin(1058f, t) * 0.25f + Sin(98f, t) * 0.5f) * 0.3f * Dec(t, d, 2.2f));
        Mk("sk_ult_stamp", 1.1f, (t, d, r) =>
        {
            float v = 0f;
            for (int k = 0; k < 3; k++)
            {
                float tt = t - k * 0.22f;
                if (tt >= 0f && tt < 0.2f) v += (Sin(Mathf.Lerp(120f, 50f, tt / 0.2f), tt) * 0.8f + N(r) * 0.3f * Dec(tt, 0.03f, 2f)) * Dec(tt, 0.2f, 2f);
            }
            float td = t - 0.7f;
            if (td >= 0f) v += (Sin(1760f, t) + Sin(2637f, t) * 0.5f) * 0.3f * Dec(td, 0.4f, 2.5f);
            return v;
        });
        Mk("sk_ult_modem", 1.3f, (t, d, r) => t < 0.9f
            ? Sin(((int)(t * 14f) % 2 == 0) ? 1300f : 2100f, t) * 0.28f * Mathf.Min(1f, t / 0.04f) + N(r) * 0.06f
            : (t > 0.95f ? Sin(Mathf.Lerp(140f, 70f, (t - 0.95f) / 0.35f), t) * 0.6f * Dec(t - 0.95f, 0.35f, 2f) : 0f));
    }

    public static string UltSound(string skinId) => skinId switch
    {
        "gunner_nogun__gold" => "sk_ult_bell",
        "swordsman__dragon" => "sk_ult_roar",
        "rogue__void" => "sk_ult_whisper",
        "archer__sun" => "sk_ult_choir",
        "cartographer__blank" => "sk_ult_paper",
        "clockmaker__stopped" => "sk_ult_gong",
        "postman__return" => "sk_ult_stamp",
        "ghost__deleted" => "sk_ult_modem",
        "wrestler__referee" => "sk_ult_whistle",
        "foreman__beta" => "sk_ult_click",
        "puppeteer__devhand" => "sk_ult_clicks",
        "undertaker__todo" => "sk_ult_dingding",
        "photographer__f12" => "sk_ult_snap",
        _ => "sk_ult_resonance",
    };

    // 캐릭터마다 '공격 소리'로 치는 이름
    //   2.0.1~ 같은 이름을 적 · 보스도 쓰는 공용 소리(쿵 · 휙 · 딸깍 · 똑딱)는 영웅 쪽 파일이 부른 것만 — 무기 스킨이 적 소리까지 바꾸던 문제
    static bool IsAttack(string name, string from)
    {
        if ((name == "thump" || name == "whoosh" || name == "click" || name == "tick") && !HeroSource(from)) return false;
        switch (CharacterData.Selected)
        {
            case CharacterId.Gunner: return name == "gunshot" || name == "crack" || name == "shotgun";
            case CharacterId.Swordsman: return name == "slash";
            case CharacterId.Rogue: return name == "whoosh";
            case CharacterId.Archer: return name == "bowtwang" || name == "arrowfly";
            case CharacterId.Alchemist: return name == "glassclink" || name == "shatter";
            case CharacterId.Cartographer: return name == "ink";
            case CharacterId.Clockmaker: return name == "ratchet";
            case CharacterId.Postman: return name == "flick";
            case CharacterId.Ghost: return name == "freeze";
            case CharacterId.Wrestler: return name == "thump";
            case CharacterId.Foreman: return name == "thump";
            case CharacterId.Puppeteer: return name == "whoosh";
            case CharacterId.Undertaker: return name == "dig" || name == "bury" || name == "thump";
            case CharacterId.Photographer: return name == "shutter" || name == "click" || name == "tick";     // 2026-11-04~ 셔터는 새 shutter 클립 (D2)
        }
        return false;
    }

    // 발사음 통째 교체(fire~)는 한 공격의 「발사」 소리 하나만 — 궁수 화살 날음 · 연금 깨짐 · 장의사 매장 · 사진사 딸깍은 예전처럼 음높이만
    static bool IsFire(string name)
    {
        switch (CharacterData.Selected)
        {
            case CharacterId.Archer: return name == "bowtwang";
            case CharacterId.Alchemist: return name == "glassclink";
            case CharacterId.Undertaker: return name == "thump";
            case CharacterId.Photographer: return name == "shutter";
        }
        return true;
    }

    static bool HeroSource(string from) => !string.IsNullOrEmpty(from)
        && (from.Contains("CharacterKit") || from.Contains("SpecialAbilities") || from.Contains("SignatureSkills") || from.Contains("Paths") || from.Contains("PlayerController"));

    // SpecialFeedback.Play 가 소리를 틀기 직전에: 공격 소리를 무기 스킨에 맞게 (덧소리는 따로 틈)
    //   무기 스킨에 발사음 레시피(fire~)가 있으면 그 소리로 통째 교체 (원래 이름은 빈 이름 → 무음)
    static float layerAt;
    public static void Adjust(SpecialFeedback fx, ref string name, ref float vol, ref float pitch, string from = "")
    {
        SkinDef w = SkinFx.WeaponSkin;
        if (w != null && IsAttack(name, from))
        {
            if (!VisualRules.Legacy && IsFire(name) && Play(w, "fire", vol, 5f, Prio.Fire, fx)) { name = ""; return; }
            pitch *= w.pitch;
            if (w.attackSound != null) name = w.attackSound;
            if (w.layer != null && Time.unscaledTime >= layerAt)
            {
                layerAt = Time.unscaledTime + 0.08f;
                fx.PlayRaw(w.layer, vol * 0.35f, 1f);
            }
            return;
        }
        // 코인 반짝 소리: 새 코인 소리(SkinAudio.CoinClip)가 없을 때만 예전처럼 음높이 · 덧소리 (두 겹 방지)
        SkinDef e = SkinFx.EffectSkin;
        if (e != null && name == "sparkle" && (VisualRules.Legacy || CoinClip == null))
        {
            if (e.id == "skin_sakura") pitch *= 1.3f;
            else if (e.id == "skin_thunder" && Time.unscaledTime >= layerAt) { layerAt = Time.unscaledTime + 0.1f; fx.PlayRaw("sk_kill_zap", vol * 0.4f, 1.4f); }
            else if (e.id == "skin_starlight") pitch *= 1.5f;
        }
    }

    // 피격 소리 (PlayerController): 캐릭터 스킨이 있으면 스킨 소리 — 예전 소리와 새 변주를 번갈아 (구매자가 익숙한 소리를 잃지 않게)
    static int hurtTurn;
    public static bool PlayHurt()
    {
        SkinDef c = SkinFx.CharSkin;
        if (c == null || SpecialAbilities.SharedFx == null) return false;
        bool fresh = !VisualRules.Legacy && Has(c, "hurt") && (c.hurtSound == null || (++hurtTurn & 1) == 0);
        if (fresh && Play(c, "hurt", 0.8f, 5f, Prio.Hurt)) return true;
        if (c.hurtSound == null) return false;
        SpecialAbilities.SharedFx.PlayRaw(c.hurtSound, 0.8f, Random.Range(0.95f, 1.05f));
        return true;
    }

    // 거너 권총 소리 (PlayerController · SpecialAbilities): 무기 스킨이 있으면 합성 총소리를 스킨 음색으로
    public static bool PlayPistol()
    {
        if (SkinFx.WeaponSkin == null || SpecialAbilities.SharedFx == null) return false;
        SpecialAbilities.SharedFx.Play("gunshot", 0.6f, Random.Range(0.97f, 1.03f));
        return true;
    }

    // ================================================================= ② 새 칸 소리 (레시피 합성)
    static readonly Dictionary<string, AudioClip> made = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, int> variants = new Dictionary<string, int>();      // "스킨:칸" → 변주 수
    static readonly HashSet<string> keep = new HashSet<string>();
    static readonly Dictionary<string, int> turn = new Dictionary<string, int>();

    // 변주 3개를 돌려 쓰는 칸 (2-6절)
    static bool ThreeWay(string slot) => slot == "step" || slot == "acc" || slot == "kill" || slot == "whit" || slot == "fire" || slot == "click";

    static string Key(SkinDef s, string slot) => s.id + ":" + slot;
    public static bool Has(SkinDef s, string slot) => s != null && variants.ContainsKey(Key(s, slot));

    // 끼운 스킨(캐릭터 · 무기 · 이펙트 · 커서) 것만 만들고 나머지는 버림 — 판 시작 · 상점에서 고를 때 · 릴레이 교대 · 스킨을 바꾼 뒤
    public static void EnsureAll(params SkinDef[] also)
    {
        if (Application.isBatchMode) return;
        keep.Clear();
        Ensure(SkinFx.CharSkin); Ensure(SkinFx.WeaponSkin); Ensure(SkinFx.EffectSkin); Ensure(SkinData.Equipped(SkinKind.Cursor));
        if (also != null) foreach (SkinDef d in also) Ensure(d);
        List<string> drop = null;
        foreach (var kv in made) if (!keep.Contains(kv.Key)) (drop ??= new List<string>()).Add(kv.Key);
        if (drop != null)
            foreach (string k in drop)
            {
                if (made[k] != null) Object.Destroy(made[k]);
                made.Remove(k);
            }
        List<string> dropV = null;
        foreach (var kv in variants) if (!keep.Contains(kv.Key + ":0")) (dropV ??= new List<string>()).Add(kv.Key);
        if (dropV != null) foreach (string k in dropV) variants.Remove(k);
        if (SpecialAbilities.SharedFx != null) Register(SpecialAbilities.SharedFx);
    }

    static void Ensure(SkinDef s)
    {
        SkinLook look = SkinLooks.Get(s);
        if (look == null) return;
        foreach (var kv in look.sfx)
        {
            string[] parts = kv.Value.Split('|');
            int n = parts.Length > 1 ? parts.Length : ThreeWay(kv.Key) && !(kv.Key == "kill" && look.opt.Contains("scale")) ? 3 : 1;
            string key = Key(s, kv.Key);
            variants[key] = n;
            for (int v = 0; v < n; v++)
            {
                string name = key + ":" + v;
                keep.Add(name);
                if (made.ContainsKey(name)) continue;
                if (GameSettings.SfxVolume <= 0f) continue;      // 효과음 0 이면 만들지 않음 (나중에 켜면 그때 — 6-5절)
                made[name] = Synth(name, parts.Length > 1 ? parts[v] : parts[0], parts.Length > 1 || n == 1 ? 1 : v);      // 변주 하나면 음높이 그대로
            }
        }
    }

    public static AudioClip Clip(SkinDef s, string slot)
    {
        if (s == null) return null;
        if (!variants.ContainsKey(Key(s, slot))) Ensure(s);      // 메뉴(커서 소리) · 상점처럼 EnsureAll 전이면 그 스킨만 만듦
        if (!variants.TryGetValue(Key(s, slot), out int n) || n <= 0) return null;
        string key = Key(s, slot);
        turn.TryGetValue(key, out int i);
        // 같은 것 연속 금지: 차례로 돌림
        turn[key] = (i + 1) % n;
        if (!made.TryGetValue(key + ":" + i, out AudioClip c) || c == null)
        {
            if (GameSettings.SfxVolume <= 0f) return null;
            SkinLook look = SkinLooks.Get(s);
            string recipe = look?.Sfx(slot);
            if (recipe == null) return null;
            string[] parts = recipe.Split('|');
            made[key + ":" + i] = c = Synth(key + ":" + i, parts.Length > 1 ? parts[Mathf.Min(i, parts.Length - 1)] : parts[0], parts.Length > 1 || n == 1 ? 1 : i);
        }
        return c;
    }

    // 코인 줍는 소리 (PlayerController 의 반음 사다리 소스로 틈 — 이펙트 스킨)
    public static AudioClip CoinClip => VisualRules.Legacy ? null : Clip(SkinFx.EffectSkin, "coin");

    // ================================================================= ③ 재생 규칙
    public enum Prio { Cursor = 0, Idle = 1, Step = 2, Hit = 3, Acc = 4, Kill = 5, Lvl = 6, Absorb = 7, Combo = 8, Big = 9, Hurt = 10, Revive = 11, Win = 12, Death = 13, Ult = 14, Fire = 99 }
    static float prioAt = -9f;
    static Prio prioLast;
    static readonly float[] busy = { -9f, -9f, -9f, -9f };

    // 칸 소리를 냄 (pitchPct: 흔들기 범위 % — 5단계로만) · 냈으면 true
    public static bool Play(SkinDef s, string slot, float vol, float pitchPct, Prio prio, SpecialFeedback via = null)
    {
        AudioClip c = Clip(s, slot);
        if (c == null) return false;
        return PlayClip(c, vol, pitchPct, prio, via);
    }

    public static bool PlayClip(AudioClip c, float vol, float pitchPct, Prio prio, SpecialFeedback via = null, float fixedPitch = 0f)
    {
        SpecialFeedback fx = via ?? SpecialAbilities.SharedFx;
        if (fx == null || c == null) return false;
        float now = Time.unscaledTime;
        if (prio != Prio.Fire)
        {
            // 같은 순간(0.1초) 우선순위: 앞선 소리가 더 높으면 생략
            if (now - prioAt < 0.1f && prio < prioLast) return false;
            // 동시 4개: 가장 오래된 것이 아직 울리면 새 소리 생략 (Juice.BulletHit 방식)
            int oldest = 0;
            for (int i = 1; i < busy.Length; i++) if (busy[i] < busy[oldest]) oldest = i;
            if (busy[oldest] > now && prio < Prio.Hurt) return false;
            busy[oldest] = now + c.length;
            prioAt = now;
            prioLast = prio;
        }
        float pitch = fixedPitch > 0f ? fixedPitch : 1f + Random.Range(-2, 3) * pitchPct * 0.005f;
        fx.PlayClip(c, Mathf.Min(0.8f, vol), pitch);
        return true;
    }

    // ================================================================= 합성 (음색 사전 4-0절)
    struct Seg { public string tb; public float f0, f1, dur, at, gain; public float[] notes; public int n; }

    static readonly HashSet<string> NoFreq = new HashSet<string> { "wind", "paper", "fire", "shutter", "modem", "breath", "clap", "pulse", "glass", "crowd", "noise", "bubble", "scratch" };
    static readonly HashSet<string> Chord = new HashSet<string> { "choir", "organ" };

    static List<Seg> ParseSound(string recipe)
    {
        List<Seg> list = new List<Seg>();
        foreach (string part in recipe.Split(';'))
        {
            string[] t = part.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (t.Length == 0) continue;
            Seg s = new Seg { tb = t[0], gain = 1f, dur = 0.2f, n = 1 };
            bool freqDone = NoFreq.Contains(s.tb);
            bool durDone = false;
            for (int i = 1; i < t.Length; i++)
            {
                string w = t[i];
                char p = w[0];
                if (p == '@') { s.at = F(w.Substring(1)); continue; }
                if (p == 'x') { s.gain = F(w.Substring(1)); continue; }
                if (p == 'n') { s.n = Mathf.Max(1, Mathf.RoundToInt(F(w.Substring(1)))); continue; }
                if (p == 'f') { ParseFreq(w.Substring(1), ref s); freqDone = true; continue; }
                if (!freqDone) { ParseFreq(w, ref s); freqDone = true; continue; }
                if (!durDone) { s.dur = Mathf.Clamp(F(w), 0.005f, 3f); durDone = true; }
            }
            list.Add(s);
        }
        return list;
    }

    static void ParseFreq(string w, ref Seg s)
    {
        if (w.Contains(","))
        {
            string[] ps = w.Split(',');
            s.notes = new float[ps.Length];
            for (int i = 0; i < ps.Length; i++) s.notes[i] = F(ps[i]);
            s.f0 = s.f1 = s.notes[0];
        }
        else if (w.Contains(">"))
        {
            string[] ps = w.Split('>');
            s.f0 = F(ps[0]); s.f1 = F(ps[1]);
        }
        else s.f0 = s.f1 = F(w);
    }

    static float F(string w) => float.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;

    static AudioClip Synth(string name, string recipe, int variant)
    {
        List<Seg> segs = ParseSound(recipe);
        float shift = 1f + (variant - 1) * 0.035f;       // 변주: 씨앗 + 음높이 ±3.5%
        float total = 0.02f;
        foreach (Seg s in segs)
        {
            float len = s.notes != null && s.notes.Length > 1 && !Chord.Contains(s.tb) ? s.dur * (s.notes.Length + 1.2f) : s.dur;
            total = Mathf.Max(total, s.at + len + 0.01f);
        }
        total = Mathf.Min(total, 3f);
        float[] buf = new float[Mathf.CeilToInt(total * Rate)];
        System.Random rng = new System.Random(name.GetHashCode());
        foreach (Seg s in segs)
        {
            if (s.notes != null && s.notes.Length > 1 && !Chord.Contains(s.tb))
            {
                for (int i = 0; i < s.notes.Length; i++)
                {
                    Seg one = s;
                    one.notes = null;
                    one.f0 = one.f1 = s.notes[i] * shift;
                    one.at = s.at + i * s.dur;
                    one.dur = s.dur * (i == s.notes.Length - 1 ? 2.2f : 1.3f);
                    Render(buf, one, rng, 1f);
                }
            }
            else Render(buf, s, rng, shift);
        }
        // 크기 맞춤: 가장 큰 칸 0.75 (음색마다 세기가 달라 칸 음량 표가 그대로 먹게)
        float peak = 0f;
        foreach (float v in buf) peak = Mathf.Max(peak, Mathf.Abs(v));
        if (peak > 0.0001f) { float k = 0.75f / peak; for (int i = 0; i < buf.Length; i++) buf[i] *= k; }
        // 끝 4ms 페이드 (딸깍 방지)
        int fade = Mathf.Min(buf.Length, Rate / 250);
        for (int i = 0; i < fade; i++) buf[buf.Length - 1 - i] *= i / (float)fade;
        AudioClip clip = AudioClip.Create(name, buf.Length, 1, Rate, false);
        clip.SetData(buf, 0);
        return clip;
    }

    static float Dec(float k, float p) => Mathf.Pow(Mathf.Clamp01(1f - k), p);
    static float A(float cutoff) => Mathf.Clamp01(2f * Mathf.PI * cutoff / Rate);       // 한 극 저역 필터 계수

    static void Render(float[] buf, Seg s, System.Random r, float shift)
    {
        int start = Mathf.Max(0, (int)(s.at * Rate));
        int n = Mathf.Min(buf.Length - start, Mathf.CeilToInt(s.dur * Rate));
        if (n <= 0) return;
        float f0 = s.f0 * shift, f1 = s.f1 * shift, d = s.dur;
        float ph = 0f, ph2 = 0f, l1 = 0f, l2 = 0f, l3 = 0f;
        // 무작위 시각이 필요한 음색 (유리 조각 · 박수)
        float[] hits = null;
        if (s.tb == "glass" || s.tb == "clap")
        {
            int cnt = s.tb == "glass" ? Mathf.Max(3, s.n == 1 ? 7 : s.n) : s.n;
            hits = new float[cnt];
            for (int i = 0; i < cnt; i++) hits[i] = (float)r.NextDouble() * d * 0.75f;
        }
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate, k = t / d;
            float f = Mathf.Lerp(f0, f1, k);
            ph += 2f * Mathf.PI * f / Rate;
            float nz = (float)(r.NextDouble() * 2.0 - 1.0);
            float v = 0f;
            switch (s.tb)
            {
                case "sine": v = Mathf.Sin(ph) * Dec(k, 2f); break;
                case "noise": l1 += (nz - l1) * (f > 0f ? A(f) : 1f); v = l1 * (f > 0f ? 2f : 1f) * Dec(k, 2f); break;
                case "sand":
                    l1 += (nz - l1) * A(f * 1.6f); l2 += (l1 - l2) * A(f * 0.6f);
                    v = (l1 - l2) * 3f * Dec(k, 2f); break;
                case "metal": v = (Mathf.Sin(ph) + 0.7f * Mathf.Sin(ph * 1.45f) + 0.4f * Mathf.Sin(ph * 2.1f)) * 0.45f * Dec(k, 3f) + nz * Dec(t / 0.01f, 2f) * 0.4f; break;
                case "bell": v = (Mathf.Sin(ph) + 0.6f * Mathf.Sin(ph * 2.76f) + 0.4f * Mathf.Sin(ph * 5.4f)) * 0.4f * Dec(k, 2.5f); break;
                case "gong": v = (Mathf.Sin(ph) + 0.6f * Mathf.Sin(ph * 2f) + 0.45f * Mathf.Sin(ph * 2.76f) + 0.25f * Mathf.Sin(ph * 5.4f) + 0.5f * Mathf.Sin(ph * 0.5f)) * 0.3f * Dec(k, 2.2f); break;
                case "chime": v = (Mathf.Sin(ph) + 0.6f * Mathf.Sin(ph * 1.33f)) * 0.5f * Dec(k, 2f); break;
                case "wood": v = Mathf.Sin(ph) * Dec(k, 4f) * 0.8f + nz * Dec(t / 0.008f, 2f) * 0.5f; break;
                case "bone":
                    {
                        float t2 = t - 0.035f;
                        v = Mathf.Sin(ph) * Dec(t / 0.03f, 4f) * 0.8f + nz * Dec(t / 0.008f, 2f) * 0.5f;
                        if (t2 >= 0f) v += Mathf.Sin(ph * 1.12f) * Dec(t2 / 0.03f, 4f) * 0.7f + nz * Dec(t2 / 0.008f, 2f) * 0.4f;
                        break;
                    }
                case "wind": l1 += (nz - l1) * A(f0 > 0f ? f0 : 900f); v = l1 * 2.2f * Mathf.Sin(Mathf.PI * k) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 7f * t)); break;
                case "drop": v = Mathf.Sin(ph) * Dec(k, 1.5f); break;
                case "paper":
                    l1 += (nz - l1) * A(1500f);
                    { float hp = nz - l1; v = hp * Dec(t / 0.05f, 2f) * 0.8f + 0.45f * hp * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 60f * t)) * Dec(k, 1.5f); }
                    break;
                case "beep": v = (Mathf.Sin(ph) >= 0f ? 1f : -1f) * 0.35f * (k < 0.85f ? 1f : 0f); break;
                case "chip": v = (Mathf.Sin(ph) >= 0f ? 1f : -1f) * 0.3f * Dec(k, 1f); break;
                case "fire":
                    l1 += (nz - l1) * A(500f);
                    v = (r.NextDouble() < 0.025 ? nz : 0f) * 0.9f + l1 * 0.9f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 2f)) * Dec(k, 1f) + 0.25f * Mathf.Sin(2f * Mathf.PI * 80f * t) * Dec(k, 1f);
                    break;
                case "ice": v = (Mathf.Sin(ph) + 0.6f * Mathf.Sin(ph * 1.33f)) * 0.35f * Dec(k, 2.5f) + nz * Dec(t / 0.05f, 2f) * 0.3f; break;
                case "cloth": l1 += (nz - l1) * A(2200f); v = l1 * 2f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * (f0 > 0f ? f0 : 18f) * t)) * Dec(k, 1.5f); break;
                case "glass":
                    foreach (float h in hits)
                    {
                        float th = t - h;
                        if (th >= 0f && th < 0.04f) v += Mathf.Sin(2f * Mathf.PI * (3000f + 1500f * h / d) * th) * Dec(th / 0.04f, 2f) * 0.4f;
                    }
                    break;
                case "thud": v = Mathf.Clamp(Mathf.Sin(ph) * 2f * Dec(k, 2f) + nz * Dec(t / 0.05f, 2f) * 0.5f, -1f, 1f) * 0.6f; break;
                case "drum":
                    ph2 += 2f * Mathf.PI * Mathf.Lerp(f0, f0 * 0.5f, Mathf.Min(1f, t / 0.12f)) / Rate;
                    v = Mathf.Sin(ph2) * Dec(k, 2f) * 0.8f + nz * Dec(t / 0.03f, 2f) * 0.4f; break;
                case "roar": l1 += (nz - l1) * A(700f); v = (Mathf.Sin(ph + 2f * Mathf.Sin(2f * Mathf.PI * 9f * t)) * 0.6f + l1 * 1.2f) * Mathf.Sin(Mathf.PI * k); break;
                case "choir":
                case "organ":
                    {
                        float[] ns = s.notes ?? new[] { f0 / shift, f0 / shift * 1.26f, f0 / shift * 1.5f };
                        float vib = 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 5f * t);
                        foreach (float fn in ns)
                        {
                            float w = 2f * Mathf.PI * fn * shift * vib * t;
                            v += Mathf.Sin(w);
                            if (s.tb == "organ") v += 0.5f * Mathf.Sin(w * 2f) + 0.25f * Mathf.Sin(w * 4f);
                        }
                        v *= 0.6f / ns.Length * Mathf.Sqrt(Mathf.Sin(Mathf.PI * k));
                        break;
                    }
                case "pulse":
                    {
                        float step = d / Mathf.Max(1, s.n), tt = t % step;
                        v = (nz * 0.5f + Mathf.Sin(2f * Mathf.PI * (f0 > 0f ? f0 : 2200f) * tt)) * Dec(tt / 0.02f, 2f) * 0.5f;
                        break;
                    }
                case "modem": v = Mathf.Sin(2f * Mathf.PI * ((int)(t * 14f) % 2 == 0 ? 1300f : 2100f) * t) * 0.3f; break;
                case "whistle": v = (Mathf.Sin(ph) * 0.9f + nz * 0.05f) * Mathf.Min(1f, t / 0.02f) * Dec(k, 1.2f); break;
                case "bubble":
                    ph2 += 2f * Mathf.PI * (300f + 400f * Mathf.Repeat((f0 > 0f ? f0 : 12f) * t, 1f)) / Rate;
                    v = Mathf.Sin(ph2) * 0.5f * Dec(k, 1.5f); break;
                case "scratch":
                    l1 += (nz - l1) * A(2500f);
                    v = (nz - l1) * (1f + Mathf.Sin(2f * Mathf.PI * (f0 > 0f ? f0 : 30f) * t)) * 0.6f * Mathf.Sin(Mathf.PI * k); break;
                case "breath": l1 += (nz - l1) * A(600f); v = l1 * 3f * Mathf.Sin(Mathf.PI * k) * (0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 3f * t)); break;
                case "crowd": l1 += (nz - l1) * A(1100f); l2 += (l1 - l2) * A(250f); v = (l1 - l2) * 3f * Mathf.Sin(Mathf.PI * k) * (0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 4f * t)); break;
                case "clap":
                    l1 += (nz - l1) * A(1200f);
                    foreach (float h in hits) { float th = t - h; if (th >= 0f && th < 0.03f) v += (nz - l1) * Dec(th / 0.03f, 2f) * 0.8f; }
                    break;
                case "shutter":
                    v = nz * Dec(t / 0.008f, 1f) * 0.9f;
                    { float t2 = t - Mathf.Min(0.15f, d * 0.6f); if (t2 >= 0f) v += nz * Dec(t2 / 0.012f, 1f) * 0.8f + Mathf.Sin(2f * Mathf.PI * 1800f * t2) * Dec(t2 / 0.03f, 2f) * 0.3f; }
                    break;
                case "musicbox": v = (Mathf.Sin(ph) + 0.3f * Mathf.Sin(ph * 2f)) * Dec(t / Mathf.Max(d, 0.4f), 2f) * 0.5f; break;
                case "pluck": v = (Mathf.Sin(ph) + 0.5f * Mathf.Sin(ph * 2f) * Dec(k, 2f) + 0.3f * Mathf.Sin(ph * 3f) * Dec(k, 3f)) * Dec(k, 2.5f) * 0.5f; break;
                case "brass": v = (Mathf.Sin(ph) + 0.35f * Mathf.Sin(ph * 3f) + 0.2f * Mathf.Sin(ph * 5f)) * 0.45f * Mathf.Min(1f, t / 0.02f) * (k < 0.8f ? 1f : (1f - k) / 0.2f); break;
                case "pipe":
                    ph2 += 2f * Mathf.PI * f * (1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5f * t)) / Rate;
                    v = (Mathf.Sin(ph2) + nz * 0.1f) * 0.5f * Mathf.Min(1f, t / 0.03f) * (k < 0.75f ? 1f : (1f - k) / 0.25f); break;
                case "saw": v = (Mathf.Sin(ph) + 0.5f * Mathf.Sin(ph * 2f) + 0.33f * Mathf.Sin(ph * 3f) + nz * 0.12f) * 0.35f * Mathf.Min(1f, t / 0.02f) * (k < 0.75f ? 1f : (1f - k) / 0.25f); break;
                case "siren":
                    ph2 += 2f * Mathf.PI * Mathf.Lerp(f0, f1, 1f - Mathf.Abs(2f * k - 1f)) / Rate;
                    v = Mathf.Sin(ph2) * 0.5f * Mathf.Sin(Mathf.PI * k); break;
                default: v = Mathf.Sin(ph) * Dec(k, 2f); break;
            }
            buf[start + i] += v * s.gain;
        }
    }
}
