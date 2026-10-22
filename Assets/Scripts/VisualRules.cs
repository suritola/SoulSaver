// 그래픽 가독성 개편 (docs/art/그래픽-개편-기획서.md) 의 공통 값 · 되돌리기 스위치 — 2026-10-22~
//   단계마다 값을 여기로 모으고, Legacy = true 면 개편 전 값으로 돌아감 (A/B 비교 캡처 · 긴급 복구용 · 설정 화면 · 저장 없음)
using UnityEngine;

public static class VisualRules
{
    // 정렬 층 (뒤 → 앞): Background · GroundFx(새: 내 범위 채움 · 아군 장판 · 픽업) · Character · Effect · Warning(새: 적 경고 · 「!」) · HitScan · TargetMark · HPpack · Player
    //   층이 없으면 SortingLayer.NameToID 가 0(Default = 맨 아래)을 돌려줘 경고가 바닥 밑으로 사라짐 → 시작 때 한 번 점검
    //   Player 가 마지막이어야 함 (PixelStrokeMin.TopLayer 가 마지막 층을 범위표시 층으로 씀)
    public const string GroundFx = "GroundFx", Warning = "Warning";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CheckLayers()
    {
        SortingLayer[] all = SortingLayer.layers;
        if (all.Length == 0 || all[all.Length - 1].name != "Player")
            Debug.LogError("[VisualRules] 마지막 정렬 층이 Player 가 아님 — 범위표시가 묻힘 (TagManager 확인)");
        if (SortingLayer.NameToID(GroundFx) == 0 || SortingLayer.NameToID(Warning) == 0)
            Debug.LogError("[VisualRules] GroundFx · Warning 정렬 층이 없음 (TagManager 확인)");
        else if (SortingLayer.GetLayerValueFromName(GroundFx) >= SortingLayer.GetLayerValueFromName("Character")
              || SortingLayer.GetLayerValueFromName(Warning) <= SortingLayer.GetLayerValueFromName("Effect"))
            Debug.LogError("[VisualRules] GroundFx · Warning 정렬 층 순서가 틀림 (TagManager 확인)");
    }

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
