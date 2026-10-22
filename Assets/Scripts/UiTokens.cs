// UI 전면 개편 색 · 글자 · 간격 토큰 (docs/art/UI-개편-기획서.md 3절 + 8절 수정) — 2026-10-22~
//   UI 코드는 숫자 대신 이 값만 씀 · tools/pixelart/make_ui_v2.py 가 아래 16진 값을 정규식으로 읽어 그림 팔레트와 대조함 (`Hex("#RRGGBB")` 형식 유지)
//   대비는 판(Panel #14131A) 기준 — Text3 · Danger 는 비판 검토 M7 에 따라 4.5:1 이상으로 올림 (6C675F 3.3 → 8C867C · D2533F 4.4 → E0644E)
using UnityEngine;

public static class UiTokens
{
    // 바탕 · 판
    public static readonly Color Void = Hex("#0A090E");            // 로딩 · 영혼 트리 같은 전면 화면 바탕
    public static readonly Color Panel = Hex("#14131A");           // 창 · 판 (알파 1 — 반투명 판은 쓰지 않음: 사용자 「메뉴 뒤 반투명 판은 별로」)
    public static readonly Color Raised = Hex("#1C1B24");          // 카드 · 버튼 · 목록 줄 (불투명: 사용자 「투명하면 누를 수 있는지 모름」)
    public static readonly Color Sunken = Hex("#0E0D12");          // 홈 · 게이지 바탕 · 입력칸 · 누름
    public static readonly Color Outline = Hex("#050407");         // 모든 도트 그림 바깥 1도트
    public static Color Scrim(bool full) => new Color(Void.r, Void.g, Void.b, full ? 0.92f : 0.78f);     // 창 뒤 막 (모달 0.78 · 전면 0.92)

    // 선
    public static readonly Color LineDim = Hex("#2B2935");         // 구분선 · 비활성
    public static readonly Color Line = Hex("#45414F");            // 기본 테
    public static readonly Color LineHi = Hex("#6E687C");          // 마우스 올림 · 툴팁 테

    // 글자 (뼈색)
    public static readonly Color Text1 = Hex("#EAE3D2");           // 제목 · 값 · 포커스 (14.4:1)
    public static readonly Color Text2 = Hex("#A59E90");           // 본문 · 라벨 (6.9:1)
    public static readonly Color Text3 = Hex("#8C867C");           // 힌트 · 키 안내 · 비활성 (판 5.1 · Raised 4.7)

    // 강조 — 청록 하나 (월드 푸른 화로 · 포털 색), 위험, 희소한 보상색 바랜 청동
    public static readonly Color Accent = Hex("#7FD3DC");          // 포커스 · 주 버튼 테 · 선택 밑줄 · 완료 (10.8:1)
    public static readonly Color AccentDim = Hex("#2E5E66");
    public static readonly Color AccentDeep = Hex("#15292D");
    public static readonly Color Danger = Hex("#E0644E");          // 파괴적 버튼 · 대가 · 받은 피해 (5.4:1) — 색만으로 구분하지 않음 (구분선 · 아이콘 · 확인 창을 함께)
    public static readonly Color DangerDim = Hex("#5A2620");
    public static readonly Color DangerDeep = Hex("#1F1012");
    public static readonly Color Bronze = Hex("#B89A5E");          // 8절: 금을 0 으로 하지 않고 새 기록 · 전설 · 진화 완료 · 동전에만 (6.9:1)
    public static readonly Color Good = Hex("#9CC98A");            // 글자 전용: 수치 상승 · 강점

    // 범주색 — 띠 · 틀 · 선에만 (글자 · 바탕 칠 금지)
    public static readonly Color CatWeapon = Hex("#C48A5A"), CatUltimate = Hex("#6FAFC4"), CatSkill = Hex("#7E8ED0"),
                                 CatSurvival = Hex("#84AE78"), CatSoul = Hex("#9C86CF"), CatWealth = Hex("#C6AF6A");

    // 게이지
    public static readonly Color GaugeHp = Hex("#B8473B");
    public static readonly Color GaugeSoul = Hex("#9C86CF");       // 경험치 · 결계 (기존 안내 「보라색 게이지」와 같게)

    // 글자 4단계 (1080p · UI 100%) 와 자동 크기 하한
    public const float Title = 48f, Heading = 34f, Body = 26f, Small = 22f;
    public static float MinSize(float size) => size >= Title ? 34f : size >= Heading ? 26f : 22f;

    // 간격 (6 의 배수 = 2도트)
    public const float S1 = 6f, S2 = 12f, S3 = 18f, S4 = 24f, S6 = 36f, S8 = 48f, S12 = 72f, S16 = 96f;
    public const float Edge = 48f;              // 화면 가장자리 여백 (HUD 포함)
    public const float PanelPad = 24f, WindowPad = 36f;
    public const float ButtonH = 60f, ButtonHInWindow = 48f, ButtonMinW = 192f, ListRowH = 54f;

    // 움직임 (초) — ease-out 만 · 넘침 · 회전 · 확대 · 사인 반복 금지
    public const float Fast = 0.08f, Base = 0.14f, Slow = 0.25f, Ceremony = 0.4f;

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out Color c);
        return c;
    }
}
