// 스킨 시청각 레시피 — 1단계: 일반 캐릭터 14 · 이펙트 「푸른 불꽃」 · 커서 「뼈 커서」 (기획 4절 각 ① · 4-15 ① · 4-16 ①)
//   형식은 SkinLooks.cs 머리 주석 · 소리 음량 · 간격은 2-6절 표 (SkinFx 가 칸마다 정함)
public static partial class SkinLooks
{
    static void AddCommon()
    {
        // ---- 거너: 사막 방랑자 「먼지 낀 떠돌이」
        L("gunner_nogun__desert", "#D9B873", "#8A6A3A",
          "step: mark boot 1 0.55 1.0; burst grain 3 0.12 0.6 r=0.5 g=6 at=feet",
          "step~ sand 2800 0.07",
          "acc: spray fx_sandpuff 1 0.8 0.35 dir=-1 r=0.8 at=feet; spray grain 4 0.12 0.4 dir=-1 r=1 at=feet",
          "acc~ wood 1600 0.02; sand 2000 0.05 @0.12",
          "hurt: burst grain 8 0.16 0.45 r=2 g=3; puff fx_sandpuff 2 1.2 0.5 r=0.6",
          "hurt~ thud 120>60 0.12",
          "lvl: column grain 18 0.16 0.8 r=0.7 h=5; column dust 8 0.3 0.8 r=0.9 h=5",
          "lvl~ saw 523,659,784,1046 0.14; breath 0.6 x0.15",
          "idle: roll tumbleweed 1 0.8 1.2 r=2 at=feet y=0.35 c=#B08850",
          "idle~ wind 0.8",
          "ultc~ metal 1800 0.03; metal 1800 0.03 @0.08; whistle 1600>1100 0.25 @0.16",
          "death: freeze body 1 1 1.0 al=0.85; burst grain 24 0.16 1.1 r=3 g=-1; pop hat 1 0.8 1.1 h=1 at=head c=a",
          "death~ sand 1200 0.8; thud 300>200 0.05 @0.7",
          "win: pop hat 1 1 1.4 h=2.5 at=head c=a; column grain 24 0.16 2 r=1 h=6",
          "win~ whistle 1046,1318,1568,2093 0.28");

        // ---- 검사: 청동 기사 「녹슨 옛 기사」
        L("swordsman__bronze", "#D99959", "#5E8C6A",
          "step: mark squareboot 1 0.55 1.0; burst rust 2 0.12 0.5 r=0.4 g=6 at=feet c=a",
          "step~ metal 1100 0.04; noise 0.03 x0.4",
          "acc: burst rust 5 0.14 0.5 r=1.2 c=a",
          "acc~ metal 900 0.05",
          "hurt: burst metal 6 0.2 0.5 r=2 g=4",
          "hurt~ metal 600 0.15",
          "lvl: rise shield 1 1.4 0.9 h=2.5 r=0 y=0.5; column rust 14 0.15 0.8 r=0.8 h=5 c=a; beam glow 1 1.6 0.8 h=5 al=0.45 at=feet",
          "lvl~ brass 523,659,784,1046 0.15",
          "idle: rise fx_smoke 1 0.7 1.2 r=0.2 h=1.2 c=#B0B0B0 at=head",
          "idle~ scratch 0.3",
          "ultc~ gong 196 0.5; scratch 0.3 @0.1",
          "death: burst metal 20 0.22 1.0 r=2.5 g=10; roll helmet 1 0.7 1.1 r=1.5 at=feet y=0.3; shrink body 1 1 0.9",
          "death~ metal 900 0.08; metal 1300 0.08 @0.1; metal 700 0.08 @0.22; metal 1100 0.08 @0.35; metal 800 0.1 @0.5; metal 600 0.15 @0.62",
          "win: rise sword 1 2 2 h=2 r=0 y=1.5 at=head c=#FFD45A; fountain rust 16 0.14 1.6 r=1 h=3 c=a",
          "win~ brass 523,659,784,1046 0.18");

        // ---- 도적: 모래 도적 「조용한 사막 손」
        L("rogue__sand", "#D9B373", "#9C7A52",
          "opt: quiet",
          "step: mark smallfoot 1 0.45 0.9; burst grain 1 0.1 0.4 r=0.3 at=feet",
          "step~ sand 3600 0.05",
          "acc: spray fx_sandpuff 1 0.6 0.3 r=0.6 at=hand; spray grain 4 0.1 0.35 r=1.2 at=hand",
          "acc~ sand 4000 0.04",
          "hurt: burst cloth 4 0.25 0.5 r=1.8 c=a; burst grain 6 0.12 0.45 r=2",
          "hurt~ cloth 20 0.15",
          "lvl: column cloth 10 0.3 0.9 r=0.6 h=5 c=a; column grain 14 0.12 0.9 r=0.9 h=5",
          "lvl~ wind 0.5; pipe 523,659,784,1046 0.13 @0.1",
          "idle: spin shuriken 1 0.5 1.2 sp=540 y=0.4 x=0.6 c=#C8CCD8",
          "idle~ metal 2400 0.06 x0.5",
          "ultc~ scratch 0.2 f60; wind 0.3 f2500 @0.15",
          "death: fall dagger 1 0.8 0.3 h=1.5 r=0 at=feet c=#C8CCD8; shrink body 1 1 1 sp=360; column grain 24 0.12 1.0 r=1.2 h=1 at=feet",
          "death~ wood 400 0.05; wind 0.8 f1500 @0.05",
          "win: column grain 20 0.14 1.6 r=0.9 h=5; fountain shuriken 3 0.5 1.4 r=0.5 h=3 c=#E0E4F0",
          "win~ pipe 784,988,1175 0.12; metal 2600 0.05 @0.4; metal 2600 0.05 @0.5; metal 2600 0.05 @0.6");

        // ---- 궁수: 가을 사냥꾼 「낙엽 밟는 숲」
        L("archer__autumn", "#F28C33", "#B5462A",
          "step: mark maple 1 0.45 1.0 rot=30; fall maple 1 0.25 0.6 h=0.4 r=0.3 at=feet c=a",
          "step~ paper 0.07; noise f4000 0.03 x0.3",
          "acc: fall maple 2 0.25 0.7 h=0.6 r=0.4 at=hand",
          "acc~ paper 0.05",
          "hurt: burst maple 8 0.25 0.6 r=2 g=2",
          "hurt~ paper 0.12; sine 600>400 0.08 x0.6",
          "lvl: column maple 18 0.28 0.9 r=0.9 h=5",
          "lvl~ pipe 523,659,784,1046 0.15; breath 0.6 x0.2",
          "idle: fall leaf 1 0.35 1.6 h=2.4 r=0.2 c=a",
          "idle~ whistle 3000>3400 0.06; whistle 3000>3400 0.06 @0.12",
          "ultc~ brass 220>330 0.6",
          "death: fall maple 22 0.3 1.1 h=3 r=1.2; shrink body 1 1 1.1",
          "death~ wind 0.9; paper 0.5 @0.2",
          "win: fountain maple 24 0.3 1.8 r=1.2 h=4",
          "win~ brass 330 0.3; brass 440 0.4 @0.3; whistle 3000>3400 0.06 @0.8; whistle 3200>3600 0.06 @0.92");

        // ---- 연금술사: 초록 약사 「약초 주머니」
        L("alchemist__herb", "#73CC73", "#C8F07A",
          "step: mark shoe 1 0.5 1.0; burst herb 1 0.2 0.6 r=0.3 at=feet; burst dust 2 0.08 0.5 r=0.3 at=feet c=a",
          "step~ glass 0.05 n3 x0.6; paper 0.04 x0.5",
          "acc: spray dust 3 0.1 0.4 r=1 at=hand c=a; rise bubble 1 0.25 0.6 r=0.2 h=0.8 at=hand",
          "acc~ drop 400>900 0.04",
          "hurt: burst bubble 6 0.3 0.6 r=1.8; burst herb 2 0.25 0.6 r=1.5",
          "hurt~ bubble 0.18 f18",
          "lvl: float mortar 1 1.2 1.0 y=1.3 at=head c=#C8B89A; column bubble 16 0.3 0.9 r=0.8 h=5",
          "lvl~ bubble 0.5; sine 523,659,784,1046 0.12 @0.1",
          "idle: rise bubble 2 0.22 1.0 r=0.3 h=1 at=hand",
          "idle~ drop 500>700 0.05; drop 600>800 0.05 @0.15",
          "ultc~ drop 400>900 0.05; bubble 0.5 @0.08",
          "death: puff fx_smoke 4 1 1 r=1; burst bubble 14 0.3 1.0 r=2; shrink body 1 1 1",
          "death~ bubble 0.7 f20; noise f3000 0.5 x0.6 @0.3",
          "win: float flask 1 1.2 2 y=1.5 at=head rot=-30 c=#D8F0FF; fountain bubble 24 0.3 2 r=0.6 h=4 y=1.5 at=head",
          "win~ drop 400>900 0.05; bubble 0.6 @0.1; sine 784,988,1175 0.12 @0.6");

        // ---- 지도 제작자: 제도사 「연필과 도면통」
        L("cartographer__drafter", "#C7B37A", "#F2EAD0",
          "step: mark heel 1 0.5 1.0; float ink 1 0.3 1.0 at=feet x=0.3 c=k al=0.45",
          "step~ sand 4500 0.06",
          "acc: burst dust 3 0.08 0.4 r=0.8 at=hand c=k; burst eraser 2 0.1 0.4 r=0.8 at=hand c=#F0A0A0",
          "acc~ sand 3500 0.05",
          "hurt: burst paper 6 0.3 0.6 r=2 c=a",
          "hurt~ paper 0.2",
          "lvl: rise scroll 1 1.6 1.0 h=2.5 r=0 c=a; column paper 10 0.25 0.9 r=0.8 h=5 c=a",
          "lvl~ paper 0.25; wood 523,659,784,1046 0.11 @0.15",
          "idle: spin compass 1 0.8 1.2 sp=300 x=0.7 y=0.3 c=#C8CCD8",
          "idle~ scratch 0.25 x0.4",
          "ultc~ paper 0.4; cloth 30 0.3 @0.05",
          "death: burst paper 18 0.3 1.1 r=3 g=-1 c=a; shrink body 1 1 1",
          "death~ paper 0.6; wind 0.8 @0.2",
          "win: pop flag 1 1.4 2 h=2.6 g=10 at=head c=#D84A3A; fountain confetti 24 0.12 1.8 r=1.2 h=4 c=a",
          "win~ wood 784,988,1175,1568 0.12; wood 400 0.05 @0.55");

        // ---- 시계공: 견습 시계공 「째깍 걸음」 (걸음이 째 · 깍 번갈아 + 세 번째 똑)
        L("clockmaker__apprentice", "#99A6BF", "#E8C060",
          "step: mark gearprint 1 0.5 1.0",
          "step~ wood 2000 0.02 | wood 1600 0.02 | wood 1200 0.03",
          "acc: spray screw 2 0.2 0.45 r=1.2 g=8 at=hand c=#B8BCC8",
          "acc~ pulse 0.06 n3 f3000",
          "hurt: burst screw 3 0.2 0.5 r=2 g=6; burst gear 3 0.3 0.5 r=2 g=6",
          "hurt~ metal 1500 0.1; wood 2400 0.02 @0.05",
          "lvl: float cog3 1 1.6 1.0 y=1.4 at=head sp=60; column gear 10 0.3 0.9 r=0.8 h=5 sp=360",
          "lvl~ pulse 0.3 n8 f2500; musicbox 1047,1319,1568,2093 0.12 @0.25",
          "idle: float pocketwatch 1 0.6 1.2 x=0.6 y=0.2 c=a",
          "idle~ wood 3000 0.02; wood 2000 0.02 @0.25; wood 1600 0.02 @0.5; wood 2000 0.02 @0.75",
          "ultc~ pulse 0.4 n12 f3200",
          "death: burst gear 10 0.3 1 r=2.5 g=10; burst screw 10 0.2 1 r=2.5 g=10; freeze body 1 1 1 c=#9098A8 al=0.7",
          "death~ wood 2000 0.02; wood 1600 0.02 @0.2; wood 2000 0.02 @0.45; wood 1600 0.02 @0.75; metal 1200 0.3 @0.3 x0.6",
          "win: fountain gear 20 0.3 1.8 r=1 h=4 sp=400",
          "win~ musicbox 1047,1319,1568,1319,2093 0.14");

        // ---- 우체부: 신문 배달부 「아침 배달」 (16걸음마다 자전거 벨)
        L("postman__paperboy", "#598CE6", "#EDE6D2",
          "step: mark shoe 1 0.5 1.0",
          "step~ wood 250 0.02; noise 0.02 x0.3",
          "stepn: 16",
          "step2~ chime 2600 0.3",
          "acc: burst news 1 0.3 0.5 r=0.8 at=hand c=a",
          "acc~ paper 0.06",
          "hurt: burst news 6 0.35 0.6 r=2 c=a",
          "hurt~ paper 0.2",
          "lvl: spin newspaper 1 1.6 1.0 sp=720 y=1.4 at=head c=a; column confetti 12 0.12 0.9 r=0.8 h=5 c=w",
          "lvl~ pulse 0.3 n6 f600; brass 784,1047 0.12 @0.35",
          "idle: float news 1 0.5 1.0 x=0.6 rot=10 c=a",
          "idle~ paper 0.15",
          "ultc~ chime 2600 0.4; chime 2600 0.3 @0.05",
          "death: burst news 14 0.35 1.1 r=3 c=a; pop hat 1 0.6 1 h=0.8 at=head; shrink body 1 1 1",
          "death~ paper 0.5; chime 2600 0.08 @0.3",
          "win: spin newspaper 1 2 2 sp=720 y=1.6 at=head c=a; fountain confetti 24 0.14 1.8 r=1.2 h=4 c=w",
          "win~ chime 2600 0.25; chime 2600 0.25 @0.3; brass 784,1047,1319 0.14 @0.6");

        // ---- 유령: 이불 유령 「잠꾸러기」 (발이 없어 이불 자락 끌린 줄)
        L("ghost__sheet", "#B3D1F2", "#E8A0B0",
          "step: mark dragmark 1 0.5 1.0 rot=90 al=0.7",
          "step~ wind 0.07 f400",
          "acc: burst check 1 0.3 0.5 r=0.8 at=hand c=a",
          "acc~ cloth 18 0.08",
          "hurt: burst feather 6 0.3 0.7 r=1.8 g=-0.5 c=w",
          "hurt~ thud 200>120 0.08; noise f1500 0.08 x0.5",
          "lvl: fall mobile 1 1.6 1.0 h=1.2 r=0 y=0.8 at=head c=#FFE98A; column star4 12 0.25 0.9 r=0.8 h=5 c=#FFE98A",
          "lvl~ musicbox 523,659,784,1046 0.18",
          "idle: rise z 3 0.35 1.6 h=1.6 r=0.2 at=head c=w",
          "idle~ breath 0.6",
          "ultc~ thud 150>90 0.1; breath 0.3 @0.08; sine 300>500 0.3 @0.1",
          "death: freeze body 1 1 0.6 al=0.6; float check 1 1.2 1.1 at=feet y=0.2 c=a; rise z 1 0.4 1 at=head h=1 c=w",
          "death~ thud 120>70 0.15; sine 400>250 0.6 @0.15",
          "win: fountain feather 24 0.3 2 r=1.2 h=4 c=w; pop dot 1 0.3 1.2 h=2 at=head c=a",
          "win~ musicbox 523,659,784,1046 0.14; thud 200>120 0.08 @0.6");

        // ---- 씨름꾼: 동네 장사 「마을 대회 단골」
        L("wrestler__local", "#D94050", "#D8C08A",
          "step: mark barefoot 1 0.55 1.0; burst grain 2 0.1 0.5 r=0.4 g=6 at=feet c=a",
          "step~ thud 140>90 0.05",
          "acc: spray dust 3 0.25 0.4 r=0.8 at=hand c=a",
          "acc~ clap 0.05 n1",
          "hurt: burst sweat 6 0.22 0.5 r=1.8 g=6 c=#BFE6FF",
          "hurt~ breath 0.15",
          "lvl: float bunting 1 2.2 1.0 y=1.2 at=head; fall confetti 16 0.12 1 h=3 r=1.2 c=rb",
          "lvl~ brass 523,784,1047 0.12; clap 0.5 n8 @0.2",
          "idle: float ribbon 1 0.6 1.2 x=0.4 y=0.1 at=head",
          "idle~ crowd 0.5 x0.6",
          "ultc~ drum 110 0.3; crowd 0.4 @0.15",
          "death: puff dust 6 0.5 1 r=1 c=a; pop ribbon 1 0.6 1 h=1.5 at=head; shrink body 1 1 1",
          "death~ thud 120>60 0.15; crowd 0.7 @0.15",
          "win: rise trophy 1 1.6 2 h=1.5 r=0 y=0.8 at=head c=#F2C94C; fall confetti 24 0.14 2 h=4 r=1.5 c=rb",
          "win~ metal 2400 0.05; metal 2400 0.05 @0.1; metal 2400 0.05 @0.2; metal 2400 0.05 @0.3; crowd 0.8 @0.35");

        // ---- 공사 반장: 야간 작업 「밤샘 공사」 (평타 몸짓은 덤프 때만 그림 — 밀기 틱은 소리만)
        L("foreman__night", "#B8F021", "#FFF6C0",
          "step: mark workboot 1 0.55 1.0; flash glow 1 0.35 0.3 at=feet y=0.7 c=a al=0.6",
          "step~ wood 220 0.03; metal 2600 0.015 x0.4",
          "acc: burst metal 4 0.2 0.4 r=1; puff dust 2 0.4 0.5 r=0.6 c=#A09080",
          "acc~ scratch 0.15",
          "hurt: burst metal 6 0.22 0.5 r=1.8; puff dust 2 0.4 0.5 c=#A09080",
          "hurt~ beep 1000 0.1",
          "lvl: beam searchbeam 2 0.6 1.0 h=6 rot=14 sp=-20 c=a al=0.7 at=feet; column dust 10 0.2 0.9 r=0.8 h=5",
          "lvl~ siren 600>1100 0.5; bell 1568 0.4 @0.5",
          "idle: flash glow 1 0.4 0.5 at=head c=a; rise fx_smoke 1 0.5 1.2 x=0.5 h=1 r=0.1 c=#C8C8C8",
          "idle~ wood 3000 0.015; breath 0.5 @0.2",
          "ultc~ beep 1000 0.18; beep 1000 0.18 @0.3",
          "death: pop helmet 1 0.7 1 h=0.6 at=head c=#F2C230; shrink body 1 1 1",
          "death~ wood 3000 0.015; thud 200>120 0.12 @0.2",
          "win: pop flag 1 1.4 2 h=2.5 g=10 at=head; fountain spark 20 0.2 1.6 r=1.2 h=5 c=rb",
          "win~ siren 600>1100 0.6; clap 0.6 n10 @0.4");

        // ---- 인형사: 그림자극 「벽에 비친 무대」 (설명 약속: 더미도 그림자 인형 — dummy 색 · 줄 = 검은 막대 실 — string 색만)
        L("puppeteer__shadow", "#665999", "#FFC870",
          "dummy: #1E1A28",
          "string: plain #1E1A28",
          "step: mark paperfoot 1 0.5 1.0 c=k al=0.8",
          "step~ wood 3500 0.02; paper 0.03 x0.4",
          "acc: ghost body 1 1 0.3 c=k al=0.5 y=-0.3",
          "acc~ cloth 20 0.08",
          "hurt: burst paper 6 0.3 0.5 r=2 c=k",
          "hurt~ paper 0.12; gong 660 0.15 @0.03 x0.5",
          "lvl: rise birdshadow 1 1.6 1.0 h=3 r=0 c=k; column paper 10 0.2 0.9 r=0.8 h=5 c=k; flash glow 1 2.2 0.8 c=a al=0.35",
          "lvl~ gong 523,587,659,784,880 0.12",
          "idle: rise handbird 1 0.8 1.4 h=2.5 r=0.3 x=0.5 c=k",
          "idle~ gong 880 0.3 x0.5",
          "ultc~ breath 0.25; gong 440 0.5 @0.2",
          "death: shrink body 1 1 1 c=k; flash glow 1 0.8 0.4 at=head c=a al=0.6",
          "death~ breath 0.3; gong 220 0.8 @0.25",
          "win: float birdshadow 2 1.2 2 r=1.5 y=1.5 at=head c=k; float sun 1 1 2 y=2.2 at=head c=k; flash glow 1 3 2 c=a al=0.3 y=1.5 at=head",
          "win~ gong 523,659,784,1046,784 0.16");

        // ---- 장의사: 묘지기 「밤의 묘지기」 (설명 약속: 구멍 테 안쪽 랜턴 불빛 — hole 색 · 8걸음마다 랜턴 삐걱)
        L("undertaker__keeper", "#668C4D", "#FFC860",
          "hole: #FFC860",
          "step: mark mudfoot 1 0.55 1.0 c=#4A3A28",
          "step~ sand 1400 0.08",
          "stepn: 8",
          "step2~ scratch 0.2 f15 x0.5",
          "acc: burst clod 2 0.2 0.5 r=1 g=8 at=feet c=#5A4630; flash glow 1 0.4 0.3 at=hand c=a al=0.6",
          "acc~ scratch 0.12 f20",
          "hurt: burst clod 6 0.22 0.5 r=1.8 g=6 c=#5A4630; burst ember 3 0.15 0.5 r=1.2 c=a",
          "hurt~ sand 1000 0.12; metal 1800 0.04 @0.04 x0.4",
          "lvl: flash glow 1 2 0.8 c=a al=0.6; column firefly 18 0.2 1 r=1 h=5 c=#D8FF70",
          "lvl~ sine 400 0.2; sine 330 0.3 @0.25; bell 880 0.5 @0.5",
          "idle: float lantern 1 0.6 1.2 x=0.6 y=0.6 c=a; rise firefly 2 0.18 1.2 r=1 h=1 c=#D8FF70",
          "idle~ pulse 0.6 n12 f4500 x0.6",
          "ultc~ wind 0.2 f2000; bell 330 0.6 @0.15",
          "death: fall lantern 1 0.6 0.5 h=1.2 r=0 c=a; shrink body 1 1 1",
          "death~ glass 0.2 n4; noise f800 0.3 @0.2 x0.5",
          "win: fountain firefly 24 0.2 2 r=1.5 h=4 c=#D8FF70; float lantern 1 0.8 2 y=1.4 at=head c=a",
          "win~ sine 400 0.2; sine 330 0.3 @0.25; bell 880 0.6 @0.5");

        // ---- 사진사: 신문 기자 「특종 사냥」 (설명 약속: 플래시가 조금 노랗다 — flash 색)
        L("photographer__press", "#E0C780", "#FFF0A0",
          "flash: #FFE07A",
          "step: mark heel 1 0.5 1.0",
          "step~ wood 1200 0.02",
          "acc: puff fx_smoke 1 0.5 0.5 r=0.3 at=hand c=a",
          "acc~ thud 300>200 0.04",
          "hurt: burst memo 3 0.3 0.6 r=2 c=w; burst photo 3 0.35 0.6 r=2 c=w",
          "hurt~ paper 0.15; shutter 0.1 x0.4",
          "lvl: rise photo 1 1.4 1.0 h=2 r=0 y=0.5 c=w; column memo 10 0.25 0.9 r=0.8 h=5 c=w",
          "lvl~ pulse 0.35 n6 f900; chime 2600 0.3 @0.4",
          "idle: float memo 1 0.5 1.2 x=0.6 c=w; float pencil 1 0.5 1.2 x=0.8 rot=-30 c=#E8C060",
          "idle~ pulse 0.4 n4 f800 x0.7",
          "ultc~ chime 2400 0.25; noise f600 0.3 @0.1",
          "death: burst photo 12 0.35 1.1 r=2.5 c=w; pop hat 1 0.6 1 h=0.8 at=head; shrink body 1 1 1",
          "death~ paper 0.4; shutter 0.25 @0.4",
          "win: flash glow 3 2 0.3 r=1 c=a; fountain photo 18 0.35 2 r=1.2 h=4 c=w",
          "win~ pulse 0.5 n8 f900; chime 2600 0.3 @0.55");
    }

    static void AddEffects()
    {
        // ---- 푸른 불꽃: 명중 = 불꽃 혀 세 갈래 · 처치 = 세로로 「확」 솟는 불꽃 (원판 · 고리 교체 D4) · 처치음 새로 · 코인 소리 교체
        L("skin_bluefire", "#73BFFF", "#D0ECFF",
          "hit: burst flame 3 0.45 0.22 r=0.5",
          "kill: rise flame 3 1.1 0.45 h=2.2 r=0.25; rise flame 1 1.6 0.45 h=2.4 r=0 c=a al=0.8; burst ember 4 0.16 0.5 r=1 g=-2 c=a",
          "kill~ fire 0.15; sine 300>150 0.18",
          "coin: rise ember 1 0.22 0.4 h=0.6 r=0.1",
          "coin~ chime 2400 0.12; noise f1200 0.08 x0.4");
        AddEffectsMore();
    }
    static partial void AddEffectsMore();

    static void AddCursors()
    {
        // ---- 뼈 커서: 메뉴 클릭 = 커서 끝에서 뼈 조각 3 · 「딱」 / 호버 「달그락」 / 전투 평타 = 조준점 오므라짐 (CursorSkin)
        L("cursor_bone", "#D6D1BF", "#F2EEDF",
          "click: bone 3",
          "click~ bone 1500 0.06",
          "hover~ bone 1900 0.03 x0.6");
        AddCursorsMore();
    }
    static partial void AddCursorsMore();
}
