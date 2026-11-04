// 스킨 시청각 레시피 — 무기 28종: 희귀 5칸(발사 · 발사음 · 명중음) · 영웅 9칸(+ 장전 또는 무기 대기 · 하이라이트 · 진화) (기획 4절 각 ⑤ · ⑥)
//   투사체 형태 · 꼬리는 그림 시트(W2 · W5) 몫이라 여기 없음 · 발사음(fire~)은 원래 공격 소리를 통째로 대신하니 0.05~0.25초
public static partial class SkinLooks
{
    static partial void AddWeapons()
    {
        // ======================================================== 거너 (리볼버)
        // ---- 희귀: 은장 리볼버 「탕—팅」
        L("pistol__silver", "#CCE6FF", "#FFFFFF",
          "fire: flash star4 1 0.9 0.15 at=hand c=w; spray spark 3 0.12 0.2 r=0.7 at=hand",
          "fire~ noise f5000 0.04; thud 220>90 0.06 x0.8; metal 2700 0.2 @0.02 x0.5; metal 4100 0.18 @0.02 x0.35",
          "whit~ metal 3300 0.05");

        // ---- 영웅: 용의 숨결 「탕—화륵」
        // 근사: 총구의 작은 용 머리 불꽃 = dragon 글리프 + 불꽃 뿜기
        L("pistol__dragon", "#FF8C33", "#FFD45A",
          "fire: spray dragon 1 0.7 0.18 r=0.5 at=hand c=a; spray flame 3 0.25 0.22 r=0.9 at=hand",
          "fire~ noise f2500 0.05; thud 140>50 0.08; fire 0.2 @0.03 x0.7",
          "whit~ fire 0.06; noise f6000 0.03 x0.5",
          "reload: burst ember 6 0.16 0.4 r=0.8 g=4 at=hand; flash glow 1 1.2 0.4 at=hand c=#FF8030 al=0.6",
          "reload~ metal 1500 0.04; metal 1300 0.04 @0.12; fire 0.25 @0.18",
          "high: rise dragon 1 1.4 0.5 h=2.5 r=0 c=a; column flame 10 0.3 0.5 r=0.4 h=3",
          "high~ roar 90 0.3; fire 0.3 @0.05 x0.6",
          // 근사: 불꽃 용이 손 무기를 감고 올라감 = 손 둘레 불꽃 나선 기둥 + 위로 오르는 용
          "evo: column flame 18 0.3 1.0 r=0.5 h=3 at=hand; rise dragon 1 1.6 1.0 h=2.5 r=0 at=hand c=a",
          "evo~ roar 110 0.5; fire 0.8 @0.1");

        // ======================================================== 검사 (장검)
        // ---- 희귀: 푸른 서리검 「쉬잉—」
        L("sword__frost", "#99E6FF", "#FFFFFF",
          "fire: spray flake 3 0.18 0.3 r=0.9 at=hand c=w; puff fx_smoke 1 0.6 0.3 r=0.3 at=hand c=a al=0.5",
          "fire~ wind 0.12 f3000; noise f8000 0.06 x0.4; ice 4000 0.2 @0.03 x0.45",
          "whit~ ice 3800 0.05");

        // ---- 영웅: 흑요석 마검 「쉭—지직」
        L("sword__obsidian", "#A659FF", "#2A1840",
          "fire: spray bolt 2 0.3 0.15 r=0.8 at=hand; spray shard 2 0.18 0.25 r=0.8 at=hand c=a",
          "fire~ wind 0.1 f2200; beep 1800 0.1 @0.03 x0.35; noise f6000 0.05 @0.04 x0.4",
          "whit~ glass 0.06 n3",
          "widle: flash lightning 1 0.6 0.4 at=hand; burst spark 2 0.1 0.3 r=0.4 at=hand",
          "widle~ beep 1800 0.08; beep 2400 0.04 @0.08 x0.5",
          // 근사: 공중에 남은 칼자국이 균열처럼 깨짐 = 기운 빛 띠 + 흑요석 조각 튐 (부채꼴 아님)
          "high: beam glow 1 0.25 0.5 h=2.6 rot=40 al=0.8; burst shard 10 0.22 0.5 r=1.2 g=4 c=a",
          "high~ glass 0.3 n7; beep 1800 0.12 @0.05 x0.5",
          "evo: float crystal 1 1.6 0.6 at=hand c=a; burst shard 20 0.25 1.0 r=2 g=5; flash glow 1 1.8 0.5 at=hand al=0.6",
          "evo~ glass 0.5 n9; sine 90 0.8 x0.7");

        // ======================================================== 도적 (표창)
        // ---- 희귀: 황금 표창 「쉬잉—팅」
        L("shuriken__gold", "#FFD959", "#FFF2B0",
          "fire: flash star4 1 0.7 0.15 at=hand c=a; spray star4 2 0.12 0.2 r=0.6 at=hand",
          "fire~ wind 0.1 f2500; chime 2600 0.12 @0.05 x0.6",
          "whit~ chime 2800 0.05");

        // ---- 영웅: 혈월 표창 「슈욱」
        L("shuriken__bloodmoon", "#FF404D", "#5A0A14",
          "fire: spray moon 1 0.5 0.2 r=0.6 at=hand al=0.7; spray spark 2 0.1 0.18 r=0.6 at=hand",
          "fire~ wind 0.12 f900; scratch 0.1 f80 @0.02 x0.6",
          "whit~ noise f7000 0.04; sine 3000 0.04 x0.5",
          // 손가락 사이 표창 여럿 겹쳐 반짝 (몸 둘레를 도는 모양 아님 — 손끝 한 줄)
          "reload: float shuriken 3 0.35 0.4 r=0.18 at=hand; flash spark 1 0.5 0.3 at=hand c=w",
          "reload~ pulse 0.3 n3 f2600; metal 2800 0.05 @0.28 x0.5",
          "high: rise moon 1 1.4 0.5 h=2 r=0 at=head y=0.5; rise spark 6 0.14 0.5 r=0.4 h=2.5 c=a",
          "high~ metal 3200 0.15; drum 90 0.3 @0.03",
          "evo: rise moon 1 1.6 0.5 h=2 r=0 at=hand y=0.5; absorb glow 8 0.25 0.5 r=1.5 at=hand; flash glow 1 1.5 0.4 at=hand al=0.5",
          "evo~ gong 110 0.9; sine 220 0.6 @0.3 x0.4");

        // ======================================================== 궁수 (활)
        // ---- 희귀: 은빛 활 「티잉」
        L("bow__silver", "#D9EBFF", "#FFFFFF",
          "fire: beam glow 1 0.15 0.15 h=0.9 at=hand c=w; burst spark 2 0.1 0.2 r=0.4 at=hand",
          "fire~ pluck 330 0.12; metal 3000 0.15 @0.02 x0.4",
          "whit~ metal 3500 0.04");

        // ---- 영웅: 세계수 활 「바람과 방울」
        L("bow__worldtree", "#80FF8C", "#3A8A3A",
          "fire: spray leaf 2 0.25 0.3 r=0.8 at=hand c=a; flash glow 1 0.7 0.15 at=hand al=0.6",
          "fire~ pluck 300 0.12; wind 0.12 @0.02 x0.5; chime 2200 0.1 @0.05 x0.4",
          "whit~ wood 900 0.04",
          // 근사: 활에서 새싹이 돋았다 짐 = 손끝에서 잎이 솟았다 떨어짐
          "widle: pop leaf 2 0.3 0.8 h=0.6 r=0.3 at=hand",
          "widle~ chime 1800 0.15; chime 2400 0.15 @0.12",
          // 근사: 꽃이 위로 핌 = 벚꽃 가지가 솟고 꽃잎이 따라 오름
          "high: rise sakura_branch 1 1.2 0.5 h=1.8 r=0 c=#FFB0D0; rise petal 6 0.2 0.5 r=0.4 h=2 c=#FFB0D0",
          "high~ pluck 523,659,784 0.08; chime 2600 0.15 @0.25",
          "evo: column leaf 14 0.25 1.0 r=0.5 h=2.5 at=hand c=a; fountain petal 10 0.2 1.0 r=0.6 h=2 at=hand c=#FFB0D0",
          "evo~ pluck 392,523,659,784 0.12; wind 0.8 x0.4");

        // ======================================================== 연금술사 (플라스크)
        // ---- 희귀: 수정 플라스크 「찡」
        L("flask__crystal", "#B3E6FF", "#FFFFFF",
          "fire: flash crystal 1 0.6 0.15 at=hand; spray star4 2 0.12 0.2 r=0.5 at=hand c=w",
          "fire~ glass 0.08 n3; sine 2400 0.18 x0.4; sine 2412 0.18 x0.4",
          "whit~ glass 0.1 n5");

        // ---- 영웅: 용암 플라스크 「보글—퉁」
        L("flask__lava", "#FF8026", "#5A2010",
          "fire: rise fx_smoke 1 0.6 0.4 r=0.2 h=0.6 at=hand c=#A08070 al=0.6; spray ember 3 0.14 0.3 r=0.7 g=3 at=hand",
          "fire~ bubble 0.1 f25 x0.6; thud 110>50 0.1 @0.06",
          "whit~ thud 90>40 0.1; noise f3000 0.06 x0.6",
          "widle: rise bubble 2 0.25 0.6 r=0.2 h=0.7 at=hand",
          "widle~ bubble 0.35 f14",
          "high: fountain ember 16 0.22 0.5 r=0.5 h=3; column flame 8 0.3 0.5 r=0.3 h=3",
          "high~ thud 70>35 0.3; noise f1500 0.3 x0.6; fire 0.4 @0.05 x0.6",
          "evo: flash glow 1 1.8 1.0 at=hand c=#FFD45A al=0.7; rise bubble 10 0.25 1.0 r=0.4 h=2 at=hand c=#FFD45A",
          "evo~ bubble 0.6 f18; sine 80 0.8 x0.7");

        // ======================================================== 지도 제작자 (잉크 획)
        // ---- 희귀: 붓 「먹빛으로 번짐」
        L("quill__brush", "#4D59A6", "#14141E",
          "fire: puff ink 2 0.4 0.4 r=0.4 at=hand c=a al=0.7; spray drop 2 0.14 0.3 r=0.6 at=hand c=a",
          "fire~ noise f900 0.15; chime 1500 0.15 @0.04 x0.35",
          "whit~ drop 500>250 0.05");

        // ---- 영웅: 분필 「가루처럼 흩날림」
        L("quill__chalk", "#F2F2FF", "#B8C0D8",
          "fire: spray dust 4 0.14 0.4 r=0.9 at=hand c=w al=0.8; puff fx_dust 1 0.5 0.4 r=0.2 at=hand c=w al=0.6",
          "fire~ scratch 0.1 f3500 x0.6; sand 5000 0.05 @0.06",
          "whit~ wood 2600 0.03",
          // 근사: 공중에 작은 별 낙서 = 분필색 별이 손 위에 떠 있음
          "widle: float star5 1 0.6 1.0 at=hand y=0.6 c=w al=0.85; burst dust 3 0.08 0.5 r=0.4 at=hand y=0.6 c=w",
          "widle~ sand 5200 0.08; sand 4800 0.08 @0.12",
          "high: burst dust 14 0.16 0.5 r=1.3 g=2 c=w; puff eraser 1 0.6 0.5 r=0.2 c=#F0A0A0",
          "high~ wood 2600 0.03; wood 2600 0.03 @0.08; wood 2600 0.03 @0.16; noise f1200 0.08 @0.26",
          "evo: column chalk 12 0.3 1.0 r=0.5 h=2.5 at=hand c=rb; burst dust 12 0.12 1.0 r=1.2 at=hand c=rb",
          "evo~ sand 4000 0.12; sand 4800 0.12 @0.15; sand 5600 0.12 @0.3; chime 2000,2500,3000 0.1 @0.4");

        // ======================================================== 시계공 (톱니)
        // ---- 희귀: 금화 「팅~」
        L("gear__coin", "#FFD14D", "#FFF2B0",
          "fire: spin coin 1 0.5 0.15 sp=900 at=hand; spray star4 2 0.1 0.2 r=0.5 at=hand c=a",
          "fire~ chime 3000 0.2; cloth 30 0.15 x0.25",
          "whit~ chime 2600 0.05; chime 3400 0.06 @0.04");

        // ---- 영웅: 주판알 「딱」
        L("gear__abacus", "#BF7340", "#6A3A1A",
          "fire: flash pixel 1 0.4 0.12 at=hand c=w; spray clod 2 0.12 0.2 r=0.5 at=hand c=a",
          "fire~ wood 1400 0.05",
          "whit~ wood 1600 0.03; wood 1600 0.03 @0.045",
          // 근사: 줄 위를 오가는 주판알 = 손 위 가로 한 줄의 알
          "widle: float pixel 4 0.25 0.5 r=0.2 at=hand y=0.3",
          "widle~ wood 1500 0.03; wood 1700 0.03 @0.1; wood 1500 0.03 @0.2",
          // 근사: 큰 주판 알 한 줄 = 공중 가로 한 줄 알 + 금 반짝
          "high: float pixel 5 0.4 0.5 r=0.35 at=head y=0.5; burst star4 4 0.15 0.5 r=0.6 at=head y=0.5 c=#FFD45A",
          "high~ wood 1500 0.03; wood 1500 0.03 @0.06; wood 1500 0.03 @0.12; wood 1500 0.03 @0.18; wood 1100 0.06 @0.3",
          "evo: column pixel 14 0.25 1.0 r=0.5 h=2.5 at=hand c=#FFD45A; flash glow 1 1.4 0.6 at=hand c=#FFD45A al=0.6",
          "evo~ wood 1000,1200,1400,1700 0.1");

        // ======================================================== 우체부 (편지)
        // ---- 희귀: 종이비행기 「슈웅」
        L("letter__plane", "#D9E6FF", "#FFFFFF",
          "fire: spray paper 2 0.2 0.25 r=0.8 at=hand c=w; spray fx_trail_dot 2 0.3 0.2 r=0.6 at=hand c=a al=0.6",
          "fire~ whistle 1800>2600 0.12 x0.4; wind 0.15 f3000",
          "whit~ paper 0.05");

        // ---- 영웅: 청구서 「휙 · 쿵」
        L("letter__bill", "#E6334D", "#F2E6C8",
          "fire: flash stamp 1 0.5 0.15 at=hand al=0.8; spray paper 2 0.18 0.25 r=0.7 at=hand c=a",
          "fire~ wind 0.08 f2000; thud 160>90 0.05 @0.07",
          "whit~ metal 1200 0.04; bell 2600 0.12 @0.04 x0.6",
          // 근사: 영수증이 길게 출력되어 나풀 = 손에서 종이 띠가 내려와 흔들림
          "widle: fall ribbon 1 0.5 1.0 h=0.6 r=0.1 at=hand c=a",
          "widle~ saw 140 0.35 x0.5; noise f2000 0.35 x0.3",
          "high: rise check_mark 1 2 0.5 h=1.5 r=0; fountain coin 6 0.2 0.5 r=0.6 h=2 c=#FFD45A",
          "high~ metal 1200 0.05; bell 2600 0.3 @0.06",
          "evo: rise paper 8 0.3 1.0 r=0.5 h=2.5 at=hand c=#FFD45A; flash glow 1 1.4 0.6 at=hand c=#FFD45A al=0.6",
          "evo~ bell 2600 0.7");

        // ======================================================== 유령 (차가운 손길)
        // ---- 희귀: 촛대 「화륵」
        L("lantern__candle", "#FFCC66", "#FF8030",
          "fire: flash candle 1 0.5 0.18 at=hand; rise ember 2 0.1 0.3 r=0.2 h=0.6 at=hand c=a",
          "fire~ fire 0.15; sine 160 0.12 x0.5",
          "whit~ drop 700>350 0.05");

        // ---- 영웅: 쇠사슬 등불 「철커덩」
        // 근사: 사슬 고리 = metal 글리프 (사슬 그림 없음)
        L("lantern__chain", "#FF9933", "#7A7A88",
          "fire: flash lantern 1 0.5 0.18 at=hand; spray metal 2 0.14 0.2 r=0.5 at=hand c=a",
          "fire~ pulse 0.12 n3 f1200; sine 110 0.12 x0.6",
          "whit~ metal 2400 0.05",
          "widle: float lantern 1 0.6 0.8 at=hand sp=30; burst metal 2 0.1 0.4 r=0.3 at=hand c=a",
          "widle~ metal 2600 0.04; metal 3100 0.04 @0.1; metal 2800 0.04 @0.2",
          "high: flash glow 1 2.2 0.5 c=#FF9933 al=0.7; rise lantern 1 1 0.5 h=1.5 r=0",
          "high~ metal 1400 0.06; metal 1700 0.06 @0.06; ice 3800 0.25 @0.12",
          "evo: column metal 12 0.25 1.0 r=0.5 h=2.5 at=hand c=#FFD45A; flash glow 1 1.4 0.6 at=hand c=#FFD45A al=0.6",
          "evo~ metal 600 0.8");

        // ======================================================== 씨름꾼 (샅바)
        // ---- 희귀: 밧줄 「누런빛 · 툭—쉭」
        L("satba__rope", "#D9B366", "#8A6A30",
          "fire: flash glow 1 0.7 0.15 at=hand c=m al=0.7; spray grain 4 0.12 0.25 r=0.7 at=hand",
          "fire~ thud 140>70 0.05; cloth 40 0.15 @0.04 x0.6",
          "whit~ thud 100>50 0.08");

        // ---- 영웅: 비단 띠 「띠가 길게 나부낌」
        L("satba__silk", "#F273A6", "#FFD45A",
          "fire: spray ribbon 2 0.5 0.35 r=1.2 at=hand; spray star4 2 0.12 0.25 r=0.8 at=hand c=a",
          "fire~ cloth 25 0.15; chime 2400 0.1 @0.05 x0.4",
          "whit~ thud 120>60 0.06; chime 2400 0.06 @0.03 x0.4",
          "widle: float ribbon 1 0.6 1.0 at=hand sp=20",
          "widle~ cloth 15 0.35 x0.6",
          "high: fountain ribbon 4 0.5 0.5 r=0.4 h=2.5; fountain star4 10 0.14 0.5 r=0.5 h=3 c=a",
          "high~ metal 2200 0.2; crowd 0.5 @0.08 x0.5",
          // 근사: 비단 띠가 용 모양으로 휘감김 = 손 둘레 리본 나선 + 위로 오르는 용
          "evo: column ribbon 10 0.4 1.0 r=0.5 h=2.5 at=hand; rise dragon 1 1.4 1.0 h=2 r=0 at=hand c=a",
          "evo~ cloth 20 0.4; gong 160 0.7 @0.2");

        // ======================================================== 공사 반장 (삽날)
        // ---- 희귀: 빗자루 「쓱싹」 (빗자루 그림 · fx_broom 은 엄마 보스 공격이라 안 씀)
        L("plow__broom", "#E6BF66", "#A0803A",
          "fire: spray grain 3 0.16 0.3 r=0.9 at=hand",
          "fire~ noise f6000 0.08 x0.7; noise f6000 0.08 @0.1 x0.7",
          "whit~ noise f2500 0.05; thud 140>80 0.04 x0.5");

        // ---- 영웅: 쇠스랑 「끼긱」 (바닥 평행선 · 이빨 세 줄 선은 L12 로 안 씀)
        L("plow__pitchfork", "#CCD9F2", "#FF9A40",
          "fire: spray spark 3 0.12 0.2 r=0.7 at=hand c=a",
          "fire~ scratch 0.15 f60",
          "whit~ metal 1800 0.05",
          "widle: burst spark 3 0.1 0.35 r=0.4 g=4 at=feet c=a",
          "widle~ scratch 0.3 f45 x0.6",
          // 근사: 공중 이빨 자국 세 줄 = 나란히 선 짧은 빛기둥 셋 (바닥 아님 · 공중 세로)
          "high: beam glow 3 0.15 0.5 h=2 r=0.4 y=0.3 al=0.8; burst spark 10 0.12 0.5 r=1.2 g=4 c=a",
          "high~ wood 600 0.05; metal 1600 0.25 @0.06",
          "evo: column spark 14 0.18 1.0 r=0.5 h=2.5 at=hand c=#FFD45A; flash glow 1 1.4 0.6 at=hand c=#FFD45A al=0.6",
          "evo~ metal 500 0.8");

        // ======================================================== 인형사 (더미 · dummy: 색은 그림이 이미 바뀌어 안 씀)
        // ---- 희귀: 곰 인형 「휘두를 때 솜」
        L("dummy__bear", "#B3804D", "#F4EEE4",
          "fire: spray cloud 2 0.25 0.35 r=0.8 at=hand c=a; spray feather 2 0.18 0.35 r=0.7 at=hand c=a",
          "fire~ thud 100>60 0.06 x0.7; wind 0.12 @0.03 x0.6; sine 1200>1600 0.08 @0.08 x0.4",
          "whit~ thud 90>50 0.06; noise f1500 0.05 x0.5");

        // ---- 영웅: 목각 인형 「딱딱」
        L("dummy__wood", "#D9A666", "#7A4A24",
          "fire: flash pixel 1 0.4 0.12 at=hand c=w; spray clod 2 0.12 0.2 r=0.6 at=hand c=a",
          "fire~ wind 0.08 f1500 x0.5; wood 1200 0.03 @0.04; wood 1200 0.03 @0.1",
          "whit~ wood 1000 0.04",
          "widle: pop clod 2 0.12 0.4 h=0.3 r=0.3 at=hand c=a",
          "widle~ wood 1300 0.03",
          // 근사: 목각 인형이 인사 = 석상 모양이 솟아 고개 숙이듯 기움
          "high: rise statue 1 2 0.5 h=1 r=0 sp=-60 c=m",
          "high~ wood 1200 0.03; wood 1200 0.03 @0.07; wood 1200 0.03 @0.14; clap 0.35 n6 @0.2",
          "evo: column star4 14 0.16 1.0 r=0.5 h=2.5 at=hand c=#FFD45A; flash glow 1 1.4 0.6 at=hand c=#FFD45A al=0.6",
          "evo~ wood 900,1100,1300,1600 0.1");

        // ======================================================== 장의사 (삽)
        // ---- 희귀: 곡괭이 「흙 튐이 돌 조각으로」
        L("shovel__pickaxe", "#B3BFD9", "#8A8478",
          "fire: spray shard 4 0.18 0.3 r=0.8 g=4 at=hand c=a; burst spark 2 0.1 0.15 r=0.4 at=hand",
          "fire~ metal 1400 0.12; noise f2000 0.06 x0.5",
          "whit~ wood 400 0.03; wood 350 0.03 @0.04; noise f1200 0.08 @0.06 x0.6");

        // ---- 영웅: 은수저 「구멍이 반짝」
        L("shovel__spoon", "#E6F2FF", "#FFFFFF",
          "hole: #E8F4FF",
          "fire: flash star4 1 0.5 0.15 at=hand c=w; spray star4 3 0.12 0.25 r=0.6 at=hand",
          "fire~ sand 6000 0.05; metal 4000 0.08 @0.03 x0.5",
          "whit~ chime 3600 0.05",
          "widle: flash star4 1 0.4 0.4 at=hand c=w; burst star4 2 0.1 0.4 r=0.3 at=hand",
          "widle~ scratch 0.12 f80 x0.4",
          "high: beam glow 1 0.4 0.5 h=3 al=0.6; rise star4 10 0.15 0.5 r=0.4 h=3",
          "high~ chime 3600 0.06; chime 3600 0.06 @0.08; chime 3600 0.06 @0.16; sand 1500 0.15 @0.2",
          // 은수저가 금수저로
          "evo: flash star4 1 1 0.8 at=hand c=#FFD45A; column star4 14 0.15 1.0 r=0.5 h=2.5 at=hand c=#FFD45A",
          "evo~ chime 4400 0.6");

        // ======================================================== 사진사 (셔터)
        // ---- 희귀: 일회용 카메라 「지익 딸깍」
        L("camera__disposable", "#F2C773", "#FF8A30",
          "fire: spin gear 1 0.4 0.15 sp=720 at=hand c=a; flash glow 1 0.6 0.12 at=hand c=w al=0.6",
          "fire~ scratch 0.08 f40; shutter 0.12 @0.07",
          "whit~ shutter 0.06; sine 1800 0.08 @0.02 x0.3");

        // ---- 영웅: 필름 카메라 「플래시 가루 펑」
        L("camera__film", "#F2CC66", "#3A2A1A",
          "flash: #FFF4D0",
          "fire: flash glow 1 1.2 0.12 at=hand c=w; puff fx_smoke 2 0.9 0.5 r=0.4 at=hand c=#C8C0B0 al=0.7",
          "fire~ noise f800 0.1; shutter 0.12 @0.06",
          "whit~ noise f1500 0.08",
          // 근사: 손잡이 감기 + 새 필름 상자 = 손끝 톱니 회전 + 떠오르는 상자
          "reload: spin gear 1 0.4 0.4 sp=900 at=hand c=a; rise parcel 1 0.5 0.4 h=0.6 r=0 at=hand",
          "reload~ pulse 0.3 n8 f1500; metal 1500 0.05 @0.3",
          "high: rise photo 1 3 0.5 h=0.8 r=0; flash glow 1 2 0.2 c=w",
          "high~ noise f800 0.15; clap 0.45 n8 @0.12",
          "evo: flash camera 1 1 0.8 at=hand c=#FFD45A; column star4 12 0.15 1.0 r=0.5 h=2.5 at=hand c=#FFD45A",
          "evo~ shutter 0.25; bell 1800 0.6 @0.2");
    }
}
