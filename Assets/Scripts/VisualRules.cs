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
    public static float ArenaMul => Legacy ? 1f : 1.8f;     // 맵 확장 8 (2026-10-22): 109×69 → 1.4배 153×97 → 1.6배 175×111 → 사용자 「좀만 더 키우자」 1.8배 197×125
    // 정돈된 경기장 배치 (ArenaLayout · 맵 확장 5) — 묘역 · 지옥 · 초원 타일맵을 시작 때 다시 깖
    public static bool TidyArena => !Legacy;
    // 끝없어 보이는 맵 (2026-10-22 사용자) — 벽 띠 대신 바닥을 화면 밖까지 잇고 경계는 어두워지는 안개로 (ArenaLayout)
    public static bool EndlessLook => !Legacy;

    // 카메라를 더 멀리 (직교 크기 20 → 28 → 25 → 22 · 조준 줌 최대도 같은 배율) — 2026-10-22 사용자 「카메라 좀 더 멀리」 · 「28 정도 해봐」 · 「조금만 더 가깝게」
    //   적 생성은 화면 범위를 그때그때 읽어(EnemySpawn 뷰포트) 화면 밖에서 나오는 건 그대로
    public static float CameraMul => Legacy ? 1f : 1.1f;      // 2026-10-22 28 → 25 → 사용자 「좀 더 가까이」 22

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

    // ───── 1.6.0~ 가독성 개편 1단계 (외부 평가 「가독성이 현저히 낮다」 · Gemini 개선안 검증판) — 값만 여기, 그리는 쪽은 각 파일 ─────

    // C1 바닥 재질 (Resources/Shaders/FloorFlat.shader · StageManager): 곱(FloorTone)은 대비 「비율」을 못 줄여서 바닥만 따로 누름
    //   밝은 쪽 무릎 = 흑백 Y 41(0.16, 화면 감마 값) 을 넘는 몫 × 0.2 (색상 유지) · 국소 대비 = 타일 1장 평균(밉 5)과 lerp 0.65
    //   밉 5 = 32칸 타일 1장 상자 평균 — 밉 3(8×8)은 상자마다 평균이 달라 경계에 3~4 Y 계단이 생겼음. 타일셋 5장 모두 160×64 라 이웃 타일로 안 번짐
    //   2026-10-25~ 무릎 0.18 → 0.16 (Y 41): 셰이더를 화면(감마) 기준으로 고친 뒤 사막 바닥 56.1 — 기준 28~55 를 넘어서 (기획 예상도 「약 55」 경계)
    //     → 사막 약 52 · 다른 바닥(묘역 46 · 심연 42 · 초원 36)은 무릎 위 밝은 점만 조금 더 눌림
    public static bool FloorFlat => !Legacy;
    public const float FloorKnee = 0.16f, FloorKneeRatio = 0.2f, FloorContrast = 0.65f, FloorMeanMip = 5f;

    // C6 적대 문법: 적 공격의 「테」(경고 원 테 · 직선 예고 가장자리)는 적대색 하나 — 채움 · 바닥 원판은 보스 테마색 그대로(정체성)
    //   #FF4033 = 흑백 Y 120. 색각 모드 주황은 그리는 쪽에서 Hostile.Accessible 로 (여기서 바꾸지 않음)
    public static readonly Color HostileRim = new Color(1f, 0.25f, 0.2f, 1f);

    // 적 탄 고리 색을 적대 대역으로: 색상이 35°~250°(아군 영웅색 대역)이면 142.5° 기준으로 주홍 15° 또는 보라 285°
    //   채도 0.15 미만(흰 · 회색 · 강철)은 그대로 — 흰 = 적 문법의 닫히는 고리 · 탄 흰 심 색. 채도 · 밝기 · 알파 유지
    public static Color HostileColor(Color c)
    {
        if (Legacy) return c;
        Color.RGBToHSV(c, out float h, out float s, out float v);
        if (s < 0.15f || h < 0.0972f || h > 0.6944f) return c;
        Color o = Color.HSVToRGB(h < 0.3958f ? 0.0417f : 0.7917f, s, v);
        o.a = c.a;
        return o;
    }

    // 적 예고 · 결계 · 공간 침식의 도는 바닥 룬 — 아군 마법진과 같은 모양이라 「적 예고 = 내 마법진」으로 오인됐음(스크린샷 03) → 끔
    public static bool HostileRune => Legacy;

    // 색각 모드에선 적 테가 주황 41°(Hostile.Accessible)가 되어 거너 금 · 시계공 황동 · 우체부 금(37°~42°)과 같은 색상 →
    //   그때만 아군 영웅 전투색 20°~65° 를 하늘 195° 로 (채도 · 밝기 · 알파 유지). 평소 · Legacy 는 그대로
    public static Color AllyCb(Color c)
    {
        if (Legacy || !GameSettings.ColorBlind) return c;
        Color.RGBToHSV(c, out float h, out float s, out float v);
        if (s < 0.15f || h < 20f / 360f || h > 65f / 360f) return c;
        Color o = Color.HSVToRGB(195f / 360f, s, v);
        o.a = c.a;
        return o;
    }

    // 2단계 묶음 8 키트 색 (2026-10-25~): 내 이펙트 색 중 적 경고와 같은 「적 신호 색」(색상 345°~25° · 265°~320°, 채도 ≥ 0.5 · 밝기 ≥ 0.6)만
    //   그 영웅 전투색(CharacterData.Ally) ±15° 나 원소색(불 28° · 비전/어둠 248° · 물리 45°)으로. 채도 · 밝기 · 알파는 그대로
    //   바꾼 자리 · 예전 색 · 이유는 tools/art/kit_colors.csv · 구매 꾸미기 색(SkinData · SkinFx 스킨 색)은 손대지 않음
    //   부를 때마다 고름 (static readonly 에 담으면 Legacy 를 켜도 안 돌아가서 속성으로)
    public static Color Kit(Color legacy, Color now) => Legacy ? legacy : now;

    // C2 내 이펙트 분류 (Fx) — 표에 없는 이름은 지금과 같음 (층 Effect · 알파 = 설정만 · 수명 그대로)
    //   표 A 바닥: 적 몸 아래 GroundFx 로 — 적 위치가 내 폭발 · 장판에 묻혔음(스크린샷 02). 알파 상한 없음(이미 적을 안 덮음 · 상한은 「내 스킬이 안 보임」만 악화)
    //     적을 묶는 그림(fx_net · fx_webring · fx_soulshackle · fx_silk · fx_tendril)은 적 위에 보여야 해서 뺌. fx_rune 은 내 것일 때만(Fx 가 Mine 을 봄)
    static readonly System.Collections.Generic.HashSet<string> GroundSet = new System.Collections.Generic.HashSet<string>
    {
        "fx_explosion", "fx_shock", "fx_puddle", "fx_vortex", "fx_alchemyblast", "fx_fissure", "fx_geyser", "fx_quake",
        "fx_poisonfog", "fx_jokerburst", "fx_blackhole", "fx_magfield", "fx_fwstar", "fx_inkwave", "fx_inkshatter", "fx_inkspatter",
        "fx_chimering", "fx_parcelburst", "fx_icenova", "fx_soulburst", "fx_cloud", "fx_firefloor", "fx_poisonbubble", "fx_rune",
    };
    //   표 B 몸(베기 · 빔 · 번개 · 구체 · 낙하물 · 소환체 · 사슬): 적 위 Effect 그대로 · 알파 상한 0.8 (하한 0.5)
    static readonly System.Collections.Generic.HashSet<string> BodySet = new System.Collections.Generic.HashSet<string>
    {
        "fx_slash", "fx_swordswing", "fx_spinslash", "fx_bigsword", "fx_swordwave", "fx_whipblade", "fx_beam", "fx_bolt",
        "fx_orb", "fx_meteor", "fx_firepillar", "fx_tornado", "fx_arrowrain", "fx_globe", "fx_sandstorm", "fx_lanceghost",
        "fx_shadowdash", "fx_scythe_ghost", "fx_cuckoo", "fx_pigeon", "fx_falcon", "fx_card", "fx_playcard", "fx_flametongue",
        "fx_soulchain", "fx_soullink",
    };
    //   표 C 장식: 적 위 Effect 그대로 · 알파 상한 0.7 (하한 0.35). fx_soulstep(사용자 승인 영혼불 발자국) · fx_trail_dot(잔상)은 넣지 않음
    static readonly System.Collections.Generic.HashSet<string> DecorSet = new System.Collections.Generic.HashSet<string>
    {
        "fx_spark", "fx_sparkle", "fx_bleed", "fx_smoke", "fx_muzzle",
    };

    public static bool IsGround(string name) => !Legacy && name != null && GroundSet.Contains(name);
    public static float MyAlphaCap(string name)
    {
        if (Legacy || name == null) return 1f;
        if (BodySet.Contains(name)) return 0.8f;
        if (DecorSet.Contains(name)) return 0.7f;
        return 1f;
    }
    // 「내 이펙트 불투명도」 설정을 곱한 뒤의 하한 (50% 설정에서도 몸 · 장식이 사라지지 않게)
    public static float MyAlphaFloor(string name)
    {
        if (Legacy || name == null) return 0f;
        if (BodySet.Contains(name)) return 0.5f;
        if (DecorSet.Contains(name)) return 0.35f;
        return 0f;
    }
    // 장식 수명 상한 (풀 Spawn 경로 · loop 아님 · 수명 지정 없음일 때만 Fx 가 씀) — 불꽃 튐 · 반짝임 · 핏방울 · 연기만 0.25초
    //   fx_muzzle 은 3프레임이라 이미 짧아 뺌. 터짐 0.4초 상한은 안 함 (표 C 밖 6프레임 시트는 16fps 에서 이미 0.375초)
    public static float DecorLifeMax(string name)
    {
        if (Legacy || name == null) return 0f;
        switch (name)
        {
            case "fx_spark": case "fx_sparkle": case "fx_bleed": case "fx_smoke": return 0.25f;
            default: return 0f;
        }
    }

    // C4 플레이어 외곽 · 발밑 (SpriteOutline.Light · FootShadow.Player)
    //   외곽 흰색 불투명 — 예전 (1, 0.95, 0.8, 0.85). 원판은 Player 층이면 발밑으로 들어오는 적 탄을 가려서 GroundFx 층 순서 100(내 바닥 이펙트 12 · 코인 70 위)
    public static Color PlayerOutline => Legacy ? new Color(1f, 0.95f, 0.8f, 0.85f) : Color.white;
    public static float PlayerDiscAlpha => Legacy ? 0.3f : 0.5f;
    public const int PlayerDiscOrder = 100;
    public static float PlayerRingAlpha => Legacy ? 0.5f : 0.75f;

    // C7 적 외곽 (SpriteOutline.shader _SkipDark): 붙은 불투명 그림 칸 중 가장 밝은 흑백 값이 이보다 작으면 그 자리 셰이더 외곽 생략
    //   그림에 이미 #1A112C 외곽이 있는 가장자리(41~98%)는 이중 외곽(보스는 화면 30~40화소 검은 띠)이었음 → 어디서나 정확히 1칸. 0 = 예전 그대로
    public static float EnemySkipDark => Legacy ? 0f : 41f / 255f;     // 1.6.0~ 0.16(40.8) 이면 hell_imp 손 외곽(Y 40.9)에 셰이더 외곽이 한 겹 더 붙음 → 41/255

    // 너무 어두운 잡몹 2종(Enemy3 Y 63 · 사신 66)만 ×1.15 — 밝은 적은 어둡게 하지 않음 (문제는 「적 위치가 안 보임」)
    //   심연 그림은 프레임마다 텍스처라 이름 앞부분으로 가름
    public static float EnemyBrightness(string textureName)
    {
        if (Legacy || string.IsNullOrEmpty(textureName)) return 1f;
        if (textureName.StartsWith("eneymy3", System.StringComparison.OrdinalIgnoreCase)
         || textureName.StartsWith("reaper_", System.StringComparison.OrdinalIgnoreCase)) return 1.15f;
        return 1f;
    }

    // C10 피격: 잡몹 꼬리 「어둡게」(×0.55 · 0.4 · 0.42 = 밝기 약 45%)가 초원 잡몹 Y 125 → 56 으로 바닥(58 · 눌린 뒤 48)에 묻혔음 →
    //   밝기만 ×0.78 (rgb 같은 배율 · 알파 유지). 흰 번쩍임 0.05초 · 간격 0.25초는 그대로. Legacy 꼬리(Color.red)는 그리는 쪽(EnermyController.HitTint)이 가름
    public const float HitDim = 0.78f;
    //   보스는 맞을 때마다 0.1초 순빨강 → 밝기 ×0.85 (흰빛은 기억 「휘청」 신호 · 큰 몸이 초당 3번 넘게 번쩍이면 광과민 기준 초과라 안 씀)
    public const float BossHitDim = 0.85f;

    // 줍는 것(코인만 · 회복 물약은 그대로): 적 위 Effect 층 70 이라 무한 모드에서 쌓인 코인이 적 무리를 덮었음 → 적 아래 · 내 바닥 이펙트 위
    public static string PickupLayer => Legacy ? "Effect" : GroundFx;
}
