using UnityEngine;

// 설정 「화면」 탭 「그래픽 품질」 (높음 = 기본 · 지금 모습 그대로 / 낮음 = 꾸밈 효과만 줄임)
// 낮음이 줄이는 것 (판정 · 피해 · 적 탄 · 경고 · 피해 숫자 · 체력바 · 적 · 플레이어 윤곽선은 그대로):
//   L1 잔상 · 꼬리 2개 중 1개만 (FadeSprite.Spawn) · L2 스킬 이펙트 밑 빛 번짐 생략 (Fx.SkillGlow)
//   L3 이펙트 윤곽선 셰이더 대신 기본 스프라이트 재질 (Fx.Style) · L4 불꽃 · 반짝임 · 핏방울 · 연기 0.05초에 12개까지 (Fx.Spawn)
//   L5 불꽃 입자 동시 150개까지 (FlameParticle) · L6 처치 폭발 4 · 충격파 1 (Juice.EnemyDied) · L7 코인 · 상자 후광 맥동 멈춤 (PickupGlow)
// 트레일러 · 자동 테스트(GameInput.Auto)는 늘 높음 (번쩍임 효과와 같은 규칙)
public static class GraphicsQuality
{
    const string Key = "settings.graphicsQuality";         // 0 높음(기본) · 1 낮음
    public static readonly string[] Names = { "높음", "낮음" };
    static int index = -1;

    public static int Index
    {
        get { if (index < 0) index = Mathf.Clamp(Prefs.GetInt(Key, 0), 0, 1); return index; }
        set { index = Mathf.Clamp(value, 0, 1); Prefs.SetInt(Key, index); }
    }

    public static bool Low => Index == 1 && !GameInput.Auto;
}
