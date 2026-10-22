// 그래픽 가독성 개편 (docs/art/그래픽-개편-기획서.md) 의 공통 값 · 되돌리기 스위치 — 2026-10-22~
//   단계마다 값을 여기로 모으고, Legacy = true 면 개편 전 값으로 돌아감 (A/B 비교 캡처 · 긴급 복구용 · 설정 화면 · 저장 없음)
public static class VisualRules
{
    public static bool Legacy;

    // 장식 이펙트 배율 (판정 범위인 이펙트 Fx.AreaFx 는 늘 그대로) · 스킬 출처면 한 번 더
    //   지금은 개편 전과 같은 값 — 1단계 8번 커밋(이펙트 과장 제거)에서 1.0 으로
    const float LegacyDecor = 1.45f, LegacySkillDecor = 1.4f;
    const float NewDecor = 1.45f, NewSkillDecor = 1.4f;
    public static float DecorScale => Legacy ? LegacyDecor : NewDecor;
    public static float SkillDecor => Legacy ? LegacySkillDecor : NewSkillDecor;

    // 보이는 그림 크기로 닿는 거리를 재는 곳(우체부 편지 · 비둘기 · 카드)이 배율이 바뀌어도 판정을 예전 그대로 두려고 곱하는 값
    //   = 예전 배율 ÷ 지금 배율 (지금은 1)
    public static float DecorKeep(bool skill) => LegacyDecor / DecorScale * (skill ? LegacySkillDecor / SkillDecor : 1f);
}
