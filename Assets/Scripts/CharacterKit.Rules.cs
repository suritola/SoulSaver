using System.Collections;
using UnityEngine;

// 캐릭터 고유 규칙 (1.0.2): 다른 캐릭터에게 없는 몸의 규칙 하나씩 (유령의 영혼처럼)
//   궁수 · 숨 고르기: 시위를 당기는 동안 숨이 닳고 쉬면 참. 바닥나면 활이 떨려 약해지고 「완벽!」이 나오지 않음.
//                    「완벽!」 사격은 숨을 크게 되돌려 줌
//   연금술사 · 불안정도: 던질 때마다 오르고 높을수록 폭발이 크고 아픔. 가득 차면 발밑에서 자폭(내 체력 8% · 주변 대폭발).
//                       던지지 않으면 내려가고, 대폭발 플라스크(우클릭)는 불안정도를 모두 쏟아 그만큼 강해짐
// 두 게이지 모두 머리 위 작은 막대로 보임
public partial class CharacterKit
{
    // ================================================================= 궁수: 숨
    public const float BreathMax = 100f;
    public float Breath { get; private set; } = BreathMax;
    float breathRestAt;
    public bool Breathless { get; private set; }
    const float BreathDrain = 20f, BreathRegen = 45f, PerfectBreath = 35f;     // 1.0.2 조정: 30 · 28 → 20 · 45 (쏘는 사이 0.45초에도 조금 참)

    // 당기는 동안 (UpdateBow)
    void ConsumeBreath()
    {
        Breath = Mathf.Max(0f, Breath - BreathDrain * Time.deltaTime);
        breathRestAt = Time.time + 0.2f;
        if (Breath <= 0f && !Breathless)
        {
            Breathless = true;
            SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 1.4f, Loc.T("숨이 차다!"), new Color(1f, 0.55f, 0.4f), 4f, 0.5f);
            Play("hiss", 0.4f, 1.6f);
        }
    }

    void RestoreBreath(float n)
    {
        Breath = Mathf.Min(BreathMax, Breath + n);
        if (Breath >= 30f) Breathless = false;
    }

    // ================================================================= 연금술사: 불안정도
    public const float InstabilityMax = 100f;
    public float Instability { get; private set; }
    float instabilityRestAt;
    bool selfBlastQueued;
    // 지금 불안정도에 따른 폭발 배율 (반지름 · 피해)
    float InstRadiusMul => 1f + 0.25f * Instability / InstabilityMax;
    float InstDamageMul => 1f + 0.6f * Instability / InstabilityMax;

    void AddInstability(float n)
    {
        Instability = Mathf.Min(InstabilityMax, Instability + n);
        instabilityRestAt = Time.time + 0.8f;
        if (Instability >= InstabilityMax && !selfBlastQueued) StartCoroutine(SelfBlast());
    }

    // 대폭발 플라스크: 불안정도를 모두 쏟아 그만큼 강해짐 (가득이면 두 배)
    float ConsumeInstability()
    {
        float mul = 1f + Instability / InstabilityMax;
        Instability = 0f;
        return mul;
    }

    // 가득 차면 발밑에서 자폭: 주변 대폭발 + 내 체력 8% (죽지는 않음)
    IEnumerator SelfBlast()
    {
        selfBlastQueued = true;
        SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 1.6f, Loc.T("폭주!"), new Color(0.9f, 0.45f, 1f), 5f, 0f);
        Play("bubble", 0.8f, 1.6f);
        for (int i = 0; i < 3; i++)
        {
            Fx.Spawn("fx_sparkle", transform.position + (Vector3)(Random.insideUnitCircle * 0.8f), 1.2f, new Color(0.9f, 0.5f, 1f), 24f);
            yield return new WaitForSeconds(0.15f);
        }
        Vector3 p = transform.position;
        DamageCircle(p, 6f, Damage * 5f, 4f);
        Fx.Spawn("fx_alchemyblast", p, 14f, SkinFx.Tint(new Color(0.9f, 0.55f, 1f)), 14f);
        Fx.Spawn("fx_shock", p, 13f, new Color(0.85f, 0.5f, 1f, 0.85f), 18f);
        ShockRing.Spawn(p, 0.5f, 6f, 0.45f, new Color(0.9f, 0.55f, 1f, 0.9f), 0.3f);
        Hostile.Shake(0.35f);
        Play("bigboom", 0.9f, 1f);
        if (!player.IsInvincible)
        {
            player.PlayerHealth = Mathf.Max(1f, player.PlayerHealth - player.PlayerMaxHealth * 0.08f);
            DamageFlash.Show(0.5f);
        }
        Instability = 0f;
        selfBlastQueued = false;
    }

    // ================================================================= 매 프레임: 회복 · 감소 · 머리 위 막대
    LineRenderer ruleBarBack, ruleBarFill;

    void UpdateKitRules()
    {
        if (Id == CharacterId.Archer)
        {
            if (!drawing && Time.time >= breathRestAt) RestoreBreath(BreathRegen * Time.deltaTime);
            DrawRuleBar(Breath / BreathMax, Breathless ? new Color(1f, 0.45f, 0.35f) : new Color(0.6f, 0.95f, 1f), Breath < BreathMax - 0.5f);
        }
        else if (Id == CharacterId.Alchemist)
        {
            if (Time.time >= instabilityRestAt && Instability > 0f) Instability = Mathf.Max(0f, Instability - 20f * Time.deltaTime);
            float k = Instability / InstabilityMax;
            Color c = Color.Lerp(new Color(0.55f, 1f, 0.45f), new Color(0.9f, 0.4f, 1f), k);
            if (k > 0.8f) c = Color.Lerp(c, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.time * 18f));
            DrawRuleBar(k, c, Instability > 0.5f);
        }
        // 1.0.2: 새 캐릭터 넷도 고유 수치를 머리 위 막대로 (예전엔 화면에 거의 안 보였음)
        else if (Id == CharacterId.Clockmaker)
        {
            // 시계 눈금: 다 차면 금빛으로 깜빡 (다음 톱니가 정각)
            float k = clockTicks / (float)ClockNeed;
            Color c = ClockReady ? Color.Lerp(new Color(1f, 0.85f, 0.3f), Color.white, 0.4f + 0.4f * Mathf.Sin(Time.time * 14f)) : new Color(0.95f, 0.75f, 0.4f);
            DrawRuleBar(k, c, true);
            ClockLate();        // 발밑 시계 · 멈춤 · 연출 · 사망 중 되감기 미리 보기 정리 (CharacterKit.Clockmaker)
        }
        else if (Id == CharacterId.Cartographer)
        {
            // 도형까지: 세 번째 획에 닫힘 → 다음 획에 닫히면 금빛
            int step = strokeCount % ShapeEvery;
            bool next = step == ShapeEvery - 1;
            Color c = next ? Color.Lerp(new Color(1f, 0.82f, 0.35f), Color.white, 0.4f + 0.4f * Mathf.Sin(Time.time * 14f)) : new Color(0.45f, 0.6f, 1f);
            DrawRuleBar((step + 1f) / ShapeEvery, c, true);
        }
        else if (Id == CharacterId.Postman)
        {
            // 속달을 누르는 동안: 붙은 표식 수 / 최대 (붉게 두근거림, 다 차면 금빛)
            if (Expressing)
            {
                float f = ExpressFill;
                Color ec = f >= 0.999f ? Color.Lerp(new Color(1f, 0.85f, 0.4f), Color.white, 0.4f + 0.4f * Mathf.Sin(Time.time * 14f)) : Color.Lerp(new Color(1f, 0.3f, 0.25f), new Color(1f, 0.6f, 0.5f), 0.5f + 0.5f * Mathf.Sin(Time.time * 12f));
                DrawRuleBar(f, ec, true);
                return;
            }
            // 우편 보너스까지 (배달 10번마다)
            int d = Deliveries % 10;
            Color c = d == 9 ? Color.Lerp(new Color(1f, 0.4f, 0.35f), Color.white, 0.4f + 0.4f * Mathf.Sin(Time.time * 14f)) : new Color(1f, 0.45f, 0.4f);
            DrawRuleBar(d / 10f, c, true);
        }
        else if (Id == CharacterId.Ghost)
        {
            // 영혼: 몸 둘레를 도는 영혼 수 / 최대
            DrawRuleBar(Wisps / (float)Mathf.Max(1, WispMax), new Color(0.65f, 0.9f, 1f), true);
        }
    }

    void DrawRuleBar(float k, Color c, bool show)
    {
        if (ruleBarBack == null)
        {
            // 1.0.2: 스킬 이펙트(+40)에 덮이지 않게 그 위 순서로, 조금 더 굵고 진하게
            ruleBarBack = Hostile.NewLine("RuleBarBack", new Color(0.04f, 0.02f, 0.08f, 0.85f), 0.26f, 62);
            ruleBarFill = Hostile.NewLine("RuleBarFill", c, 0.16f, 63);
            ruleBarBack.loop = ruleBarFill.loop = false;
            ruleBarBack.sortingLayerID = ruleBarFill.sortingLayerID = PlayerLook.TopLayer;     // 1.0.4~ 플레이어와 같이 모든 이펙트 위
        }
        show &= !PlayerController.Cinematic;
        ruleBarBack.enabled = ruleBarFill.enabled = show;
        if (!show) return;
        // 연금술사는 다음 시약 표시 위에, 궁수는 머리 위에
        float top = body.bounds.max.y + (Id == CharacterId.Alchemist ? 2f : 0.6f);
        float w = 1.6f;
        Vector3 left = new Vector3(transform.position.x - w * 0.5f, top, 0f);
        ruleBarBack.positionCount = 2;
        ruleBarBack.SetPosition(0, left - Vector3.right * 0.06f);
        ruleBarBack.SetPosition(1, left + Vector3.right * (w + 0.06f));
        ruleBarFill.positionCount = 2;
        ruleBarFill.SetPosition(0, left);
        ruleBarFill.SetPosition(1, left + Vector3.right * Mathf.Max(0.02f, w * Mathf.Clamp01(k)));
        ruleBarFill.startColor = ruleBarFill.endColor = c;
    }

    void OnDestroyRules()
    {
        if (ruleBarBack != null) Destroy(ruleBarBack.gameObject);
        if (ruleBarFill != null) Destroy(ruleBarFill.gameObject);
    }
}
