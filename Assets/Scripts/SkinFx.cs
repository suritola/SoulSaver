using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 장착한 스킨을 게임에 입힘 (SkinData). 판정 · 크기는 그대로, 겉모습과 소리만
//   캐릭터 스킨: 영웅부터 이동 잔상, 전설은 오라 · 처치 연출 · 필살기 소리
//   무기 스킨:   총알 · 투사체 색 (SkinTint), 공격 소리 음높이 · 덧소리 (SkinAudio)
//   이펙트 스킨: 명중 불꽃 색, 처치 효과 · 소리, 코인 반짝임
// 1.3.3~ 사용자 「연출 스킨들 훨씬 더 크고 잘 보이게」: 처치 · 명중 · 오라 · 잔상 · 필살기 연출을 2배 가까이 크고 진하게 (예전엔 적 경고보다 눈에 띄지 않게 옅고 작았음)
//   판정 · 크기 · 소리 크기는 그대로, 겉모습만 · 처치 연출은 초당 최대 약 16번 그대로
public class SkinFx : MonoBehaviour
{
    public static SkinDef CharSkin, WeaponSkin, EffectSkin;
    public static SkinFx Instance { get; private set; }

    PlayerController player;
    SpriteRenderer body;
    Vector3 lastPos;
    float ghostAt, killFxAt, killSoundAt;
    readonly List<Transform> orbs = new List<Transform>();
    GameObject groundGlow;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Refresh();
        SceneManager.sceneLoaded += (s, m) =>
        {
            Refresh();
            if (s.name != "GameScene") return;
            PlayerController p = Object.FindFirstObjectByType<PlayerController>();
            if (p != null && p.GetComponent<SkinFx>() == null) p.gameObject.AddComponent<SkinFx>();
        };
    }

    // 장착한 스킨 다시 읽기 (게임 시작 · 상점에서 바꾼 뒤)
    public static void Refresh()
    {
        CharSkin = SkinData.Equipped(SkinKind.Character);
        WeaponSkin = SkinData.Equipped(SkinKind.Weapon);
        EffectSkin = SkinData.Equipped(SkinKind.Effect);
    }

    void Start()
    {
        Instance = this;
        player = GetComponent<PlayerController>();
        body = GetComponent<SpriteRenderer>();
        lastPos = transform.position;
        if (SpecialAbilities.SharedFx != null) SkinAudio.Register(SpecialAbilities.SharedFx);
        EnermyController.Killed += OnKill;
        PlayerController.UltUsed += OnUlt;
        if (CharSkin != null && CharSkin.tier >= 3) BuildAura();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        EnermyController.Killed -= OnKill;
        PlayerController.UltUsed -= OnUlt;
    }

    void Update()
    {
        if (Time.timeScale == 0f || body == null) return;
        Vector3 p = transform.position;
        bool moving = (p - lastPos).sqrMagnitude > 0.0004f;
        lastPos = p;
        // 영웅 이상: 움직일 때 옅은 잔상 (0.09초마다)
        if (CharSkin != null && CharSkin.tier >= 2 && moving && Time.time >= ghostAt && body.enabled && body.sprite != null)
        {
            ghostAt = Time.time + 0.09f;
            // 최적화 16: 새로 만들고 지우던 잔상(MakeSprite + FadeOut)을 다시 쓰는 FadeSprite 로 — 알파 1→0 · 크기 0.8→1.2 곡선이 같음
            //   좌우 뒤집기는 가로 크기 부호로 (FadeSprite 는 flipX 를 되돌리지 않으므로)
            FadeSprite f = FadeSprite.Spawn("SkinGhost", body.sprite, p, 1f, new Color(CharSkin.color.r, CharSkin.color.g, CharSkin.color.b, 0.6f), "Character", -1, 0.45f);
            Vector3 ls = transform.lossyScale;
            if (body.flipX) ls.x = -ls.x;
            f.Shape(Quaternion.identity, ls);
        }
        // 전설: 오라 (작은 빛 셋이 천천히 돎)
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null) continue;
            float a = Time.time * 1.6f + i * Mathf.PI * 2f / orbs.Count;
            orbs[i].position = p + new Vector3(Mathf.Cos(a) * 1.5f, Mathf.Sin(a) * 0.75f + 0.6f, 0f);
            float pulse = 1f + 0.25f * Mathf.Sin(Time.time * 5f + i);
            orbs[i].localScale = Vector3.one * 0.13f * pulse;
        }
        if (groundGlow != null)
        {
            groundGlow.transform.position = p + new Vector3(0f, -0.9f, 0f);
            groundGlow.transform.localScale = Vector3.one * 0.5f * (1f + 0.08f * Mathf.Sin(Time.time * 3f));
        }
    }

    void BuildAura()
    {
        Sprite glow = SpecialAbilities.GlowSprite;
        if (glow == null) return;
        Color c = CharSkin.color;
        // 1.6.0~ 가독성 개편 1-D: 전설 오라 구슬 알파 1 → 0.7 (적 위 Effect 층이라 붙은 적을 가렸음) · 스킨 색은 그대로 (구매한 꾸미기)
        float orbAlpha = VisualRules.Legacy ? 1f : 0.7f;
        for (int i = 0; i < 3; i++)
        {
            GameObject o = SpecialAbilities.MakeSprite("SkinAura", glow, transform.position, 0.13f, new Color(c.r, c.g, c.b, orbAlpha), "Effect", 2);
            orbs.Add(o.transform);
        }
        groundGlow = SpecialAbilities.MakeSprite("SkinGround", glow, transform.position, 0.5f, new Color(c.r, c.g, c.b, 0.42f), "Background", 50);
    }

    // 처치: 이펙트 스킨 · 전설 캐릭터 스킨의 연출 (초당 최대 약 16번)
    // 1.9.1: 기본 처치 효과(영혼 폭발)에 묻혀 거의 보이지 않던 것을 키움. 기본 효과 색도 스킨 색으로 (Juice · KillColor)
    void OnKill(Vector3 pos)
    {
        bool effect = EffectSkin != null;
        bool legend = CharSkin != null && CharSkin.tier >= 3;
        if ((!effect && !legend) || Time.time < killFxAt) return;
        killFxAt = Time.time + 0.06f;
        if (effect)
        {
            switch (EffectSkin.id)
            {
                case "skin_bluefire":
                    // 푸른 불꽃이 확 피어오름 (폭발 + 충격파 + 불꽃 넷)
                    Fx.Spawn("fx_explosion", pos, 4.6f, EffectSkin.color, 20f);
                    Fx.Spawn("fx_shock", pos, 5.6f, new Color(EffectSkin.color.r, EffectSkin.color.g, EffectSkin.color.b, 0.9f), 22f);
                    for (int i = 0; i < 4; i++) Fx.Spawn("fx_sparkle", pos + (Vector3)(Random.insideUnitCircle * 1.8f), 2.4f, EffectSkin.color, 16f);
                    break;
                case "skin_sakura":
                    // 꽃잎이 넓게 흩날림 + 분홍 충격파
                    Fx.Spawn("fx_shock", pos, 5.2f, new Color(1f, 0.65f, 0.8f, 0.85f), 20f);
                    for (int i = 0; i < 10; i++) Fx.Spawn("fx_sparkle", pos + (Vector3)(Random.insideUnitCircle * 2.6f), 2.4f, new Color(1f, 0.65f, 0.8f), 12f);
                    break;
                case "skin_thunder":
                    // 하늘에서 굵은 번개가 내리꽂히고 큰 노란 충격파
                    Vector3 sky = pos + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(5f, 6.5f), 0f);
                    Fx.Bolt(sky, pos, 1.7f, new Color(1f, 0.95f, 0.45f), 0.2f);
                    Fx.Spawn("fx_shock", pos, 6.6f, new Color(1f, 0.9f, 0.35f, 0.95f), 22f);
                    Fx.Spawn("fx_sparkle", pos, 3.4f, new Color(1f, 0.97f, 0.7f), 18f);
                    break;
                default:
                    // 별의 축복: 무지갯빛 별 여덟 + 가운데 큰 별 + 흰 충격파
                    Fx.Spawn("fx_sparkle", pos, 4f, Color.white, 16f);
                    for (int i = 0; i < 8; i++) Fx.Spawn("fx_sparkle", pos + (Vector3)(Random.insideUnitCircle * 2.4f), 2.4f, Color.HSVToRGB(Random.value, 0.5f, 1f), 16f);
                    Fx.Spawn("fx_shock", pos, 6.2f, new Color(1f, 1f, 1f, 0.85f), 20f);
                    break;
            }
            if (EffectSkin.killSound != null && Time.time >= killSoundAt)
            {
                killSoundAt = Time.time + 0.12f;
                SpecialAbilities.SharedFx?.Play(EffectSkin.killSound, 0.55f, Random.Range(0.95f, 1.08f));
            }
        }
        if (legend)
        {
            Fx.Spawn("fx_sparkle", pos, 2.6f, CharSkin.color, 18f);
            Fx.Spawn("fx_shock", pos, 3f, new Color(CharSkin.color.r, CharSkin.color.g, CharSkin.color.b, 0.7f), 22f);
        }
    }

    // 적이 맞았을 때 (모든 캐릭터 · 모든 공격): 이펙트 스킨 색 명중 불꽃 (초당 최대 20번)
    // 예전에는 거너 총알에만 명중 색이 입혀져 다른 캐릭터는 이펙트 스킨이 거의 보이지 않았음
    static float hitFxAt;
    static int hitCount;

    public static void OnEnemyHit(Vector3 pos)
    {
        if (EffectSkin == null || Time.time < hitFxAt) return;
        hitFxAt = Time.time + 0.05f;
        Vector3 at = pos + (Vector3)(Random.insideUnitCircle * 0.3f);
        Fx.Spawn("fx_spark", at, 2.4f, HitColor(Color.white), 26f);
        // 뇌전: 세 번에 한 번 전기가 튐
        if (EffectSkin.id == "skin_thunder" && ++hitCount % 3 == 0)
            Fx.Bolt(at + (Vector3)(Random.insideUnitCircle.normalized * 1.8f), at, 0.8f, new Color(1f, 0.95f, 0.5f), 0.12f);
    }

    // 기본 처치 효과(영혼 폭발) 색: 이펙트 스킨이 있으면 스킨 색 (Juice)
    public static Color KillColor(Color stageColor)
    {
        if (EffectSkin == null) return stageColor;
        return EffectSkin.id == "skin_starlight" ? Color.HSVToRGB(Random.value, 0.4f, 1f) : EffectSkin.color;
    }

    void OnUlt()
    {
        if (CharSkin == null || CharSkin.tier < 3) return;
        SpecialAbilities.SharedFx?.Play(SkinAudio.UltSound(CharSkin.id), 0.6f);
        Fx.Spawn("fx_shock", transform.position, 11f, new Color(CharSkin.color.r, CharSkin.color.g, CharSkin.color.b, 0.85f), 18f);
        Fx.Spawn("fx_sparkle", transform.position + Vector3.up * 0.6f, 4f, CharSkin.color, 16f);
    }

    // ================================================================= 게임 코드가 부르는 도우미
    // 명중 불꽃 색 (Bullet)
    public static Color HitColor(Color baseColor)
    {
        if (EffectSkin == null) return baseColor;
        return EffectSkin.id == "skin_starlight" ? Color.HSVToRGB((Time.time * 0.8f) % 1f, 0.45f, 1f) : EffectSkin.color;
    }

    // 코인 반짝임 색 (Juice)
    public static Color CoinColor(Color baseColor) => EffectSkin == null ? baseColor
        : EffectSkin.id == "skin_starlight" ? Color.HSVToRGB(Random.value, 0.4f, 1f) : EffectSkin.color;

    // 평타 이펙트 색 (검 베기 · 플라스크 폭발): 무기 스킨 색으로 물들임
    public static Color Tint(Color c) => WeaponSkin == null ? c : Color.Lerp(c, new Color(WeaponSkin.color.r, WeaponSkin.color.g, WeaponSkin.color.b, c.a), 0.6f);
}

// 총알 · 투사체 색: 만들어진 뒤(색이 다 정해진 다음) 무기 스킨 색으로 물들임
public class SkinTint : MonoBehaviour
{
    bool done;

    void Start() => ApplyNow();

    // 다시 쓰는 총알(풀링)은 Start 보다 BulletGlow 꾸미기가 먼저 올 수 있어 거기서도 부름 (한 번만)
    public void ApplyNow()
    {
        if (done) return;
        done = true;
        if (SkinFx.WeaponSkin == null) { Destroy(this); return; }
        if (TryGetComponent(out SpriteRenderer sr) && sr.enabled) sr.color = SkinFx.Tint(sr.color);
        Destroy(this);
    }
}

// 스킨 소리: 합성으로 만든 소리를 등록하고, 공격 · 코인 소리를 스킨에 맞게 바꿈 (SpecialFeedback.Play 가 부름)
public static class SkinAudio
{
    static SpecialFeedback registered;
    static float layerAt;

    public static void Register(SpecialFeedback fx)
    {
        if (fx == null || registered == fx) return;
        registered = fx;
        float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
        float Dec(float t, float d, float p) => Mathf.Pow(Mathf.Clamp01(1f - t / d), p);
        float N(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
        // ---- 피격
        fx.MakeClip("sk_hurt_low", 0.22f, (t, d, r) => (Sin(Mathf.Lerp(140f, 70f, t / d), t) * 0.7f + N(r) * 0.2f) * Dec(t, d, 2f));
        fx.MakeClip("sk_hurt_light", 0.14f, (t, d, r) => (Sin(Mathf.Lerp(700f, 400f, t / d), t) * 0.5f + N(r) * 0.15f) * Dec(t, d, 2f));
        fx.MakeClip("sk_hurt_whistle", 0.25f, (t, d, r) => Sin(Mathf.Lerp(1800f, 1200f, t / d), t) * 0.4f * Dec(t, d, 1.5f));
        fx.MakeClip("sk_hurt_echo", 0.5f, (t, d, r) => (Sin(520f, t) + Sin(523f, t) * 0.8f) * 0.3f * (Dec(t, 0.15f, 2f) + 0.5f * Dec(Mathf.Max(0f, t - 0.18f), 0.3f, 2f)));
        fx.MakeClip("sk_hurt_bell", 0.6f, (t, d, r) => (Sin(880f, t) + Sin(1320f, t) * 0.5f + Sin(2640f, t) * 0.2f) * 0.35f * Dec(t, d, 2.5f));
        fx.MakeClip("sk_hurt_metal", 0.3f, (t, d, r) => (Sin(1900f, t) + Sin(2750f, t) * 0.7f + N(r) * 0.3f * Dec(t, 0.05f, 2f)) * 0.3f * Dec(t, d, 3f));
        fx.MakeClip("sk_hurt_chime", 0.4f, (t, d, r) => (Sin(1568f, t) + Sin(2093f, t) * 0.6f) * 0.3f * Dec(t, d, 2f));
        fx.MakeClip("sk_hurt_roar", 0.45f, (t, d, r) => (Sin(90f + 20f * Sin(9f, t), t) * 0.6f + N(r) * 0.35f) * Mathf.Sin(Mathf.PI * t / d) * 0.6f);
        fx.MakeClip("sk_hurt_flap", 0.25f, (t, d, r) => N(r) * 0.45f * Mathf.Abs(Sin(18f, t)) * Dec(t, d, 1.5f));
        fx.MakeClip("sk_hurt_smoke", 0.35f, (t, d, r) => N(r) * 0.35f * Mathf.Sin(Mathf.PI * t / d) * Dec(t, d, 1f));
        fx.MakeClip("sk_hurt_void", 0.5f, (t, d, r) => (Sin(Mathf.Lerp(220f, 110f, t / d), t) * 0.5f + Sin(Mathf.Lerp(330f, 165f, t / d), t) * 0.35f) * Mathf.Sin(Mathf.PI * t / d) * 0.5f);
        fx.MakeClip("sk_hurt_ice", 0.3f, (t, d, r) => (N(r) * 0.3f * Dec(t, 0.08f, 2f) + Sin(3136f, t) * 0.25f + Sin(4186f, t) * 0.15f) * Dec(t, d, 2.5f));
        fx.MakeClip("sk_hurt_wind", 0.4f, (t, d, r) => N(r) * 0.3f * Mathf.Sin(Mathf.PI * t / d) * (0.6f + 0.4f * Sin(7f, t)));
        fx.MakeClip("sk_hurt_bubble", 0.3f, (t, d, r) => Sin(300f + 400f * ((t * 12f) % 1f), t) * 0.35f * Dec(t, d, 1.5f));
        // ---- 처치 (이펙트 스킨)
        fx.MakeClip("sk_kill_petal", 0.3f, (t, d, r) => (Sin(1760f, t) * 0.4f + Sin(2349f, t) * 0.25f) * Dec(t, d, 2.5f));
        fx.MakeClip("sk_kill_zap", 0.18f, (t, d, r) => (N(r) * 0.3f + Mathf.Sign(Sin(1800f + 900f * Sin(55f, t), t)) * 0.2f) * Dec(t, d, 2f));
        fx.MakeClip("sk_kill_star", 0.5f, (t, d, r) => (Sin(2093f, t) + Sin(2637f, t) * 0.6f + Sin(3136f, t) * 0.4f) * 0.25f * Dec(t, d, 2.5f));
        // ---- 필살기 (전설 캐릭터 스킨)
        fx.MakeClip("sk_ult_bell", 1.2f, (t, d, r) => (Sin(523f, t) + Sin(784f, t) * 0.6f + Sin(1046f, t) * 0.4f + Sin(1568f, t) * 0.2f) * 0.35f * Dec(t, d, 2f));
        fx.MakeClip("sk_ult_roar", 1f, (t, d, r) => (Sin(70f + 25f * Sin(6f, t), t) * 0.6f + N(r) * 0.4f) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / d * 1.2f)) * 0.7f);
        fx.MakeClip("sk_ult_whisper", 1f, (t, d, r) => N(r) * 0.3f * (0.5f + 0.5f * Sin(4f, t)) * Mathf.Sin(Mathf.PI * t / d) + Sin(Mathf.Lerp(200f, 120f, t / d), t) * 0.2f * Dec(t, d, 1f));
        fx.MakeClip("sk_ult_choir", 1.3f, (t, d, r) => (Sin(440f, t) + Sin(554f, t) * 0.8f + Sin(659f, t) * 0.7f + Sin(880f, t) * 0.3f) * 0.2f * (0.7f + 0.3f * Sin(5f, t)) * Mathf.Sin(Mathf.PI * t / d));
        fx.MakeClip("sk_ult_resonance", 1.1f, (t, d, r) => (Sin(330f, t) * Sin(3f, t) + Sin(495f, t) * 0.5f + Sin(990f, t) * 0.25f * Sin(11f, t)) * 0.35f * Dec(t, d, 1.5f));
    }

    public static string UltSound(string skinId) => skinId switch
    {
        "gunner_nogun__gold" => "sk_ult_bell",
        "swordsman__dragon" => "sk_ult_roar",
        "rogue__void" => "sk_ult_whisper",
        "archer__sun" => "sk_ult_choir",
        _ => "sk_ult_resonance",
    };

    // 캐릭터마다 '공격 소리'로 치는 이름
    static bool IsAttack(string name)
    {
        switch (CharacterData.Selected)
        {
            case CharacterId.Gunner: return name == "gunshot" || name == "crack" || name == "shotgun";
            case CharacterId.Swordsman: return name == "slash";
            case CharacterId.Rogue: return name == "whoosh";
            case CharacterId.Archer: return name == "bowtwang" || name == "arrowfly";
            case CharacterId.Alchemist: return name == "glassclink" || name == "shatter";
        }
        return false;
    }

    // SpecialFeedback.Play 가 소리를 틀기 직전에: 공격 소리 · 코인 소리를 스킨에 맞게 (덧소리는 따로 틈)
    public static void Adjust(SpecialFeedback fx, ref string name, ref float vol, ref float pitch)
    {
        SkinDef w = SkinFx.WeaponSkin;
        if (w != null && IsAttack(name))
        {
            pitch *= w.pitch;
            if (w.attackSound != null) name = w.attackSound;
            if (w.layer != null && Time.unscaledTime >= layerAt)
            {
                layerAt = Time.unscaledTime + 0.08f;
                fx.PlayRaw(w.layer, vol * 0.35f, 1f);
            }
            return;
        }
        SkinDef e = SkinFx.EffectSkin;
        if (e != null && name == "sparkle")
        {
            if (e.id == "skin_sakura") pitch *= 1.3f;
            else if (e.id == "skin_thunder" && Time.unscaledTime >= layerAt) { layerAt = Time.unscaledTime + 0.1f; fx.PlayRaw("sk_kill_zap", vol * 0.4f, 1.4f); }
            else if (e.id == "skin_starlight") pitch *= 1.5f;
        }
    }

    // 피격 소리 (PlayerController): 캐릭터 스킨이 있으면 스킨 소리, 없으면 false (원래 소리)
    public static bool PlayHurt()
    {
        SkinDef c = SkinFx.CharSkin;
        if (c == null || c.hurtSound == null || SpecialAbilities.SharedFx == null) return false;
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
}
