using UnityEngine;

// 스킨 연출 — 개편 전 모습 그대로 (VisualRules.Legacy 가 켜졌을 때만 · 6-6절: 겉모습 비교 · 긴급 복구용)
//   스킨 시청각 개편(2026-11-04) 이전 SkinFx.cs 의 처치 · 명중 · 오라 덧연출 · 필살기 코드를 옮겨 둔 것 (고리 · 원판 · 남의 그림 재사용 D4 · D5 포함)
public partial class SkinFx
{
    static float legacyHitAt;
    static int legacyHitCount;

    // 새 전설 넷의 오라 덧연출 (구슬 말고 몸 둘레에 가끔): 백지도 = 종이 조각 · 멈춘 시계 = 시계 룬 · 반송 = 소인 봉투 · 삭제된 장면 = 깨진 픽셀
    void LegacyAuraAmbient(Vector3 p)
    {
        Vector3 at = p + (Vector3)(Random.insideUnitCircle * 1.4f);
        switch (CharSkin.id)
        {
            case "cartographer__blank":
                auraFxAt = Time.time + 0.5f;
                Fx.Spawn("fx_card", at, 1f, new Color(0.96f, 0.95f, 0.88f, 0.85f), 8f);
                break;
            case "clockmaker__stopped":
                auraFxAt = Time.time + 1.3f;
                Fx.Spawn("fx_target_rune", p + new Vector3(0f, -0.9f, 0f), 2.6f, new Color(0.8f, 0.85f, 0.9f, 0.55f), 10f);
                break;
            case "postman__return":
                auraFxAt = Time.time + 0.7f;
                Fx.Spawn("fx_card", at, 0.9f, new Color(0.95f, 0.85f, 0.7f, 0.8f), 8f);
                Fx.Spawn("fx_sparkle", at, 0.8f, new Color(0.9f, 0.15f, 0.2f), 14f);
                break;
            case "ghost__deleted":
                auraFxAt = Time.time + 0.25f;
                Fx.Spawn("fx_codeblock", at, 0.9f, Random.value < 0.5f ? new Color(1f, 0.2f, 0.85f, 0.9f) : new Color(0.2f, 0.95f, 0.95f, 0.9f), 12f);
                break;
            case "wrestler__referee":      // 발밑 모래판 선 (모래빛 고리가 번져 나감)
                auraFxAt = Time.time + 1.1f;
                Fx.Spawn("fx_shock", p + new Vector3(0f, -0.9f, 0f), 2.4f, new Color(0.91f, 0.76f, 0.48f, 0.5f), 10f);
                break;
            case "foreman__beta":          // 회색 창 테두리 사각 틀 + 작은 「응답 없음」 창 조각
                auraFxAt = Time.time + 0.6f;
                Fx.Spawn("fx_codeblock", at, 0.9f, new Color(0.72f, 0.75f, 0.82f, 0.85f), 10f);
                break;
            case "puppeteer__devhand":     // 발밑 작업 표시줄 도트 띠
                auraFxAt = Time.time + 0.9f;
                Fx.Spawn("fx_codeblock", p + new Vector3(Random.Range(-1.2f, 1.2f), -0.9f, 0f), 1.1f, new Color(0.6f, 0.7f, 0.9f, 0.7f), 8f);
                break;
            case "undertaker__todo":       // 발밑 반투명 「저장 중」 모래시계 점
                auraFxAt = Time.time + 0.8f;
                Fx.Spawn("fx_sparkle", p + new Vector3(0f, -0.9f, 0f) + (Vector3)(Random.insideUnitCircle * 0.5f), 1.1f, new Color(0.95f, 0.85f, 0.4f, 0.7f), 10f);
                break;
            case "photographer__f12":      // 발밑 픽셀 격자 깜빡
                auraFxAt = Time.time + 0.5f;
                Fx.Spawn("fx_codeblock", p + new Vector3(Random.Range(-1.3f, 1.3f), Random.Range(-1.1f, -0.5f), 0f), 0.8f, new Color(0.3f, 0.95f, 0.95f, 0.8f), 8f);
                break;
            default:
                auraFxAt = Time.time + 99f;
                break;
        }
    }

    // 처치: 이펙트 스킨 · 전설 캐릭터 스킨의 연출 (초당 최대 약 16번)
    // 1.9.1: 기본 처치 효과(영혼 폭발)에 묻혀 거의 보이지 않던 것을 키움. 기본 효과 색도 스킨 색으로 (Juice · KillColor)
    void LegacyKill(Vector3 pos)
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
            LegacyLegendKill(pos);
            Fx.Spawn("fx_sparkle", pos, 2.6f, CharSkin.color, 18f);
            Fx.Spawn("fx_shock", pos, 3f, new Color(CharSkin.color.r, CharSkin.color.g, CharSkin.color.b, 0.7f), 22f);
        }
    }

    // 새 전설 넷의 처치 연출 (2.0.0~): 종이 조각 · 톱니 분해 · 소인 봉투 · 픽셀 흩어짐
    void LegacyLegendKill(Vector3 pos)
    {
        switch (CharSkin.id)
        {
            case "cartographer__blank":
                for (int i = 0; i < 6; i++) Fx.Spawn("fx_card", pos + (Vector3)(Random.insideUnitCircle * 1.8f), 1.3f, new Color(0.97f, 0.96f, 0.9f), 10f);
                break;
            case "clockmaker__stopped":
                for (int i = 0; i < 4; i++) Fx.Spawn("atk_gear", pos + (Vector3)(Random.insideUnitCircle * 1.6f), 1.1f, new Color(0.75f, 0.8f, 0.85f), 12f);
                break;
            case "postman__return":
                for (int i = 0; i < 3; i++) Fx.Spawn("atk_letter", pos + (Vector3)(Random.insideUnitCircle * 1.6f), 1.2f, new Color(0.98f, 0.92f, 0.8f), 12f);
                Fx.Spawn("fx_sparkle", pos, 2.2f, new Color(0.9f, 0.15f, 0.2f), 18f);
                break;
            case "ghost__deleted":
                for (int i = 0; i < 8; i++) Fx.Spawn("fx_codeblock", pos + (Vector3)(Random.insideUnitCircle * 2f), 0.8f, i % 2 == 0 ? new Color(1f, 0.2f, 0.85f) : new Color(0.2f, 0.95f, 0.95f), 14f);
                break;
            case "wrestler__referee":      // 모래로 흩어짐
                for (int i = 0; i < 7; i++) Fx.Spawn("fx_sparkle", pos + (Vector3)(Random.insideUnitCircle * 1.8f), 1.4f, new Color(0.91f, 0.76f, 0.48f), 12f);
                break;
            case "puppeteer__devhand":     // 작은 창이 틱 닫힘
                Fx.Spawn("fx_codeblock", pos, 1.1f, new Color(0.7f, 0.78f, 0.95f), 14f);
                Fx.Spawn("fx_sparkle", pos, 1.2f, new Color(0.9f, 0.3f, 0.3f), 12f);
                break;
            case "undertaker__todo":       // 작은 저장 아이콘으로 줄어듦
                Fx.Spawn("fx_codeblock", pos, 0.8f, new Color(0.35f, 0.5f, 0.85f), 14f);
                Fx.Spawn("fx_codeblock", pos + new Vector3(0f, 0.25f, 0f), 0.5f, new Color(0.9f, 0.9f, 0.95f), 14f);
                break;
            case "photographer__f12":      // 4x3 칸 썸네일로 줄어듦 (청록 네모 조각)
                for (int i = 0; i < 6; i++) Fx.Spawn("fx_codeblock", pos + new Vector3((i % 3 - 1) * 0.35f, (i / 3 - 0.5f) * 0.35f, 0f), 0.6f, i % 2 == 0 ? new Color(0.3f, 0.95f, 0.95f) : new Color(0.9f, 0.97f, 1f), 14f);
                break;
            case "foreman__beta":          // 초록 체크 표시가 튀어오름 (초록 불꽃 줄기)
                for (int i = 0; i < 4; i++) Fx.Spawn("fx_sparkle", pos + new Vector3(-0.5f + i * 0.35f, 0.3f + (i == 1 ? -0.3f : i == 2 ? 0.2f : i == 3 ? 0.7f : 0f), 0f), 1.6f, new Color(0.35f, 0.95f, 0.45f), 14f);
                break;
        }
    }

    static void LegacyEnemyHit(Vector3 pos)
    {
        if (EffectSkin == null || Time.time < legacyHitAt) return;
        legacyHitAt = Time.time + 0.05f;
        Vector3 at = pos + (Vector3)(Random.insideUnitCircle * 0.3f);
        Fx.Spawn("fx_spark", at, 2.4f, HitColor(Color.white), 26f);
        // 뇌전: 세 번에 한 번 전기가 튐
        if (EffectSkin.id == "skin_thunder" && ++legacyHitCount % 3 == 0)
            Fx.Bolt(at + (Vector3)(Random.insideUnitCircle.normalized * 1.8f), at, 0.8f, new Color(1f, 0.95f, 0.5f), 0.12f);
    }

    void LegacyUlt()
    {
        if (CharSkin == null || CharSkin.tier < 3) return;
        SpecialAbilities.SharedFx?.Play(SkinAudio.UltSound(CharSkin.id), 0.6f);
        Fx.Spawn("fx_shock", transform.position, 11f, new Color(CharSkin.color.r, CharSkin.color.g, CharSkin.color.b, 0.85f), 18f);
        Fx.Spawn("fx_sparkle", transform.position + Vector3.up * 0.6f, 4f, CharSkin.color, 16f);
    }
}
