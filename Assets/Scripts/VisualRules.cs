// 그래픽 가독성 개편 (docs/art/그래픽-개편-기획서.md) 의 공통 값 · 되돌리기 스위치 — 2026-10-22~
//   단계마다 값을 여기로 모으고, Legacy = true 면 개편 전 값으로 돌아감 (A/B 비교 캡처 · 긴급 복구용 · 설정 화면 · 저장 없음)
using UnityEngine;

public static class VisualRules
{
    // 정렬 층 (뒤 → 앞): Background · GroundFx(새: 내 범위 채움 · 아군 장판 · 픽업) · Character · Effect · Warning(새: 적 경고 · 「!」) · HitScan · TargetMark · HPpack · Player
    //   층이 없으면 SortingLayer.NameToID 가 0(Default = 맨 아래)을 돌려줘 경고가 바닥 밑으로 사라짐 → 시작 때 한 번 점검
    //   Player 가 마지막이어야 함 (PixelStrokeMin.TopLayer 가 마지막 층을 범위표시 층으로 씀)
    public const string GroundFx = "GroundFx", Warning = "Warning";
    // 적 경고(Telegraph · 「!」 · 돌진 예고선 · 스킬 이름표 · 적 장판 테)를 그릴 층 — 1단계-6~ 내 이펙트(Effect) · 피해 숫자보다 위
    public static string WarnLayer => Legacy ? "Effect" : Warning;

    // 1단계-12~ 맵 물체(깨지는 통 · 석관) · 보물 상자 몸통: 적(Character) 위 Effect 층이라 뒤에 선 적을 가렸음 → 적과 같은 층에서 Y 정렬
    //   빛 기둥은 바닥 층 · 분위기 입자(사막 불티 · 모래 바람 · 심연 영혼 불빛 · 안개)도 적 아래 바닥 층으로 · 개수 절반
    public static string PropLayer => Legacy ? "Effect" : "Character";
    public static int PropOrder(int legacy) => Legacy ? legacy : legacy - 9;       // 몸통 9 → 0 · 테 8 → −1 · 빛 7 → −2
    public static string AmbientLayer => Legacy ? "Effect" : "Background";
    public static float AmbientRate => Legacy ? 1f : 2f;         // 나오는 간격 배율 (2 = 절반)

    // 1단계-7~ 바닥 톤다운: 바닥이 화면에서 가장 크고 시끄러워 적 · 경고를 묻었음 (사막 명도 170 · 지옥 채도 .66)
    //   바닥 타일맵 · 지도 그림 · 배경색에 곱함 (위에 막을 덮으면 Background 층의 내 장판 · 룬까지 어두워져서 타일 색으로)
    //   지옥은 붉은 기를 더 뺌 · 무한 모드는 한 타일맵에 여러 영토를 칠해서 하나의 값
    public static Color FloorTone(int stage, bool endless)
    {
        if (Legacy) return Color.white;
        if (endless) return new Color(0.55f, 0.56f, 0.6f);
        switch (stage)
        {
            case 0: return new Color(0.85f, 0.85f, 0.9f);       // 묘역 (이미 어두움)
            case 1: return new Color(0.62f, 0.66f, 0.74f);      // 지옥
            case 2: return new Color(0.85f, 0.85f, 0.9f);       // 초원 (StageManager.DimMeadow 0.58 위에)
            default: return new Color(0.8f, 0.8f, 0.85f);       // 심연
        }
    }

    // 경기장 배율 (ArenaRules · docs/art/맵-확장-기획서.md) — 맵-6 에서 1.4 로 (그때까지 1 = 예전 그대로)
    public static float ArenaMul => Legacy ? 1f : 1.4f;     // 맵 확장 8 (2026-10-22): 카메라 28 에 맞춰 109×69 → 153×97
    // 정돈된 경기장 배치 (ArenaLayout · 맵 확장 5) — 묘역 · 지옥 · 초원 타일맵을 시작 때 다시 깖
    public static bool TidyArena => !Legacy;

    // 카메라를 더 멀리 (직교 크기 20 → 28 · 조준 줌 최대도 같은 배율) — 2026-10-22 사용자 「카메라 좀 더 멀리」 · 「28 정도 해봐」
    //   적 생성은 화면 범위를 그때그때 읽어(EnemySpawn 뷰포트) 화면 밖에서 나오는 건 그대로
    public static float CameraMul => Legacy ? 1f : 1.4f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]

    static void CheckLayers()
    {
        // 1단계-11~ 같은 층 · 같은 순서끼리는 화면 아래(앞)에 있는 것을 앞에 그림 — 적끼리 겹칠 때 누가 앞인지 (예전엔 생긴 순서대로 뒤죽박죽)
        if (!Legacy)
        {
            UnityEngine.Rendering.GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
            UnityEngine.Rendering.GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);
        }
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
    //   1단계-8b~ 내 것 1.45 → 1.2 · 스킬 추가 1.4 → 1.0 (합쳐 약 2배 → 1.2배)
    //   적 장식은 1.45 그대로 — 적 가시 등 그림을 판정에 맞춰 둔 곳이 있어서 (BossUltimate.Domain.cs · BossSkills.Reborn.cs 의 fx_spike)
    const float LegacyDecor = 1.45f, LegacySkillDecor = 1.4f;
    const float NewDecorMine = 1.2f, NewDecor = 1.45f, NewSkillDecor = 1f;
    public static float DecorScaleFor(bool mine) => Legacy ? LegacyDecor : mine ? NewDecorMine : NewDecor;
    public static float SkillDecor => Legacy ? LegacySkillDecor : NewSkillDecor;

    // 보이는 그림 크기로 닿는 거리를 재는 곳(우체부 편지 · 비둘기 · 카드)이 배율이 바뀌어도 판정을 예전 그대로 두려고 곱하는 값
    //   = 예전 배율 ÷ 지금 배율 (지금은 1)
    // 1단계-8~ 내 스킬 이펙트에 붙던 과장 — 그리는 순서 +40(적 경고 · 피해 숫자 · 투사체 위로) · 흰 쪽 35% 밝게 + 알파 0.97 이상 · 밑에 흰빛 원판(적 무리를 안개처럼 덮음)
    public static int SkillOrder => Legacy ? 40 : 0;
    public static bool SkillBrighten => Legacy;
    public static bool SkillGlow => Legacy;
    // 내 장식 이펙트(불꽃 튐 · 반짝임 · 핏방울 · 연기) 0.05초 상한 — 예전엔 그래픽 「낮음」에서만 12개
    public static int MyDecorCap => Legacy ? int.MaxValue : 6;

    public static float DecorKeep(bool mine, bool skill) => LegacyDecor / DecorScaleFor(mine) * (skill ? LegacySkillDecor / SkillDecor : 1f);
}
