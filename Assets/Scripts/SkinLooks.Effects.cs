// 스킨 시청각 레시피 — 2~4단계: 이펙트 벚꽃 · 뇌전 · 별의 축복 / 커서 불씨 · 영혼 · 황금 (기획 4-15 ②③④ · 4-16 ②③④)
//   예전 처치 고리 · 원판(fx_shock · fx_explosion — D4)은 모두 세로로 솟는 모양으로 · 뇌전 번개는 풀 입자(D7)
public static partial class SkinLooks
{
    static partial void AddEffectsMore()
    {
        // ---- 벚꽃 (희귀 7칸): 명중 꽃잎 · 처치 꽃잎 회오리 · 처치음 3변주 · 코인 방울 · 연쇄 벚꽃 가지 · 상자 꽃잎 분수 · 이정표 꽃비
        L("skin_sakura", "#FF99BF", "#FFE0EC",
          "hit: burst petal 3 0.25 0.35 r=0.8; burst cherry 1 0.35 0.35 r=0.3 c=a",
          "kill: column petal 10 0.3 0.9 r=0.9 h=3; rise cherry 1 0.6 0.6 h=1.2 r=0 c=a",
          "kill~ chime 1760 0.15; chime 2349 0.12 @0.06",
          "coin: rise petal 1 0.22 0.5 h=0.6 r=0.1",
          "coin~ chime 2800 0.12",
          "combo: float sakura_branch 1 2.4 0.9 y=2; fall petal 16 0.3 1.2 h=3.5 r=2",
          "combo~ pluck 523,659,784,1046 0.08",
          "chest: fountain petal 20 0.3 1.2 r=1 h=3",
          "chest~ chime 2400 0.2; pluck 784,1047 0.1 @0.1",
          "mile: fall petal 24 0.3 1.2 h=4 r=3",
          "mile~ pluck 523,659,784,1046,1319 0.09");

        // ---- 뇌전 (영웅 9칸): 명중 지그재그 + 3번에 한 번 짧은 번개 · 처치 하늘 번개(기존) + 불티 · 연쇄 번개 다섯 줄기(세로 — 적끼리 잇지 않음 L4)
        L("skin_thunder", "#FFE659", "#FFF8C8",
          "opt: bolt3 skybolt",
          "hit: burst bolt 2 0.35 0.18 r=0.6",
          "kill: burst spark 6 0.22 0.4 r=1.5 c=a; flash glow 1 1.2 0.2 al=0.5",
          "kill~ beep 1800>2400 0.06 x0.6; noise f5000 0.12",
          "coin: burst bolt 1 0.25 0.2 r=0.3",
          "coin~ chime 2600 0.1; beep 2000 0.04 @0.02 x0.4",
          "combo: beam lightning 5 0.8 0.35 h=5 r=0.8 y=-0.5",
          "combo~ noise f300 0.5; roar 60 0.5 x0.6",
          "chest: beam lightning 1 0.9 0.4 h=4 y=-0.3; burst spark 8 0.2 0.5 r=1.5",
          "chest~ noise f300 0.6; roar 55 0.5 x0.6",
          "big: beam lightning 1 1.6 1.2 h=6 y=-0.5; burst spark 16 0.3 0.8 r=3 c=a; flash glow 1 3 0.3 al=0.5",
          "big~ noise f200 1.0; roar 50 0.9",
          "edge: bolt 16");

        // ---- 별의 축복 (전설 10칸): 무지개 별 · 처치마다 큰 별 + 별 여덟 · 연속 처치가 음계 · 출처별 처치(평타 작은 별 · 스킬 혜성 · 필살기 초신성)
        L("skin_starlight", "#FFFFFF", "#FFF0B0",
          "opt: scale",
          "hit: burst star4 1 0.45 0.25 r=0.2 c=rb; burst spark 2 0.2 0.25 r=0.6 c=rb",
          "kill: rise star5 1 0.9 0.6 h=2 r=0 c=w; burst star4 8 0.3 0.6 r=2 c=rb",
          "kill.basic: burst star4 3 0.3 0.4 r=1 c=rb; rise star5 1 0.5 0.5 h=1 r=0 c=w",
          "kill.skill: spray comet 1 0.9 0.5 r=1.5 c=w; burst star4 4 0.25 0.4 r=1 c=rb",
          "kill.ult: beam glow 1 1.4 0.7 h=6 c=w al=0.8 y=-0.5; burst star5 12 0.35 0.9 r=3 c=rb",
          "kill~ chime 2093 0.25",
          "coin: rise star4 1 0.2 0.5 h=0.6 r=0.1 c=rb",
          "coin~ chime 3000 0.12",
          "combo: rise star5 6 0.5 1 h=2.5 r=1.5 c=rb",
          "combo~ musicbox 1568,2093,2637 0.08",
          "chest: fountain star4 20 0.3 1.2 r=1 h=3 c=rb",
          "chest~ musicbox 1047,1319,1568,2093 0.1",
          "mile: fall comet 6 0.8 1.2 h=5 r=3 c=w",
          "mile~ musicbox 1047,1319,1568,2093,2637 0.1",
          "big: fall comet 6 0.9 0.8 h=5 r=2.5 c=w; rise star5 1 1.8 1.2 h=2 r=0 c=w",
          "big~ bell 1047 0.6; musicbox 2093,2637 0.1 @0.2",
          "edge: star4 16");
    }

    static partial void AddCursorsMore()
    {
        // ---- 불씨 커서 (희귀 5칸): 클릭 불티 4 · 「틱」 · 호버 「치」 · 메뉴 꼬리 불티 · 전투 조준점이 일렁
        L("cursor_ember", "#EB6E2E", "#FFC060",
          "click: ember 4",
          "click~ fire 0.06; wood 2600 0.015",
          "hover~ noise f6000 0.04 x0.5",
          "tail: ember",
          "aimhit: flicker");

        // ---- 영혼 커서 (영웅 6칸): 클릭 영혼 연기 · 「휘잉」 · 호버 속삭임 · 연기 꼬리 · 조준점 둘레 푸른 빛 · 필살기 조준 중 영혼 눈 + 「우웅」
        L("cursor_soul", "#975EEB", "#8CD8FF",
          "click: smoke 2",
          "click~ wind 0.15 f2500",
          "hover~ breath 0.08",
          "tail: smoke",
          "aimhit: glow",
          "ultaim: souleye",
          "ultaim~ sine 220 0.45 x0.8; sine 223 0.45; sine 110 0.3 x0.4");      // 조준 시작 낮은 「우웅」 (맥놀이)

        // ---- 황금 커서 (전설 7칸): 클릭 금화 반짝 · 「팅」 · 호버 「짤랑」 · 금가루 꼬리 · 조준점 금빛 「팅」 · 왕관 조준점 · 메뉴 금가루가 커서로 모임
        L("cursor_gold", "#F5C74F", "#FFF2B0",
          "click: coin 3",
          "click~ chime 3000 0.1",
          "hover~ glass 0.06 n2",
          "tail: spark",
          "aimhit: ting",
          "ultaim: crown",
          "ultaim~ bell 2600 0.35; chime 3200 0.2 @0.05 x0.4",      // 조준 시작 금 「띵」
          "dust: spark");
    }
}
