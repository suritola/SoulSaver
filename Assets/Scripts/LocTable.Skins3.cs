using UnityEngine;

// 번역표 (2.1.0~): 새 영웅 둘(씨름꾼 · 공사 반장)의 스킨 이름 6 · 설명 6 · 무기 스킨 4 · 4 (SkinData) + 상점 문구
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddSkins3()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        S("업적으로 해금", "Unlocked by achievement", "実績で解放", "成就解锁");

        // ---------------- 씨름꾼
        S("동네 장사", "Neighborhood Strongman", "町内の怪力", "街坊大力士");
        S("흰 러닝셔츠에 운동회 머리띠, 마을 대회 단골 우승자. 피격 소리가 가벼워집니다.",
          "A white tank top and a sports-day headband, the regular champion of the village contest. Hit sounds become lighter.",
          "白いランニングシャツに運動会のはちまき、村の大会の常連優勝者。被弾音が軽くなります。",
          "白背心配运动会头带，村里比赛的常胜冠军。受击声变得轻快。");
        S("청동 샅바", "Bronze Satba", "青銅のまわし", "青铜缠腰带");
        S("샅바와 팔 토시가 청동이라 메칠 때마다 쇳소리가 납니다.",
          "The belt and arm guards are bronze, so every throw rings with metal.",
          "まわしと腕当てが青銅製で、投げるたびに金属音が響きます。",
          "缠腰带与护臂都是青铜制成，每次摔投都会发出金属声。");
        S("묘역의 장사", "Graveyard Strongman", "墓地の怪力", "墓地大力士");
        S("해골 샅바를 맨 묘역의 장사. 움직이면 뼛빛 잔상이 남고, 피격하면 서늘한 바람 소리가 납니다.",
          "A graveyard strongman in a skull belt. Moving leaves bone-pale afterimages; getting hit sounds like a cold wind.",
          "ドクロのまわしを締めた墓地の怪力。動くと骨色の残像が残り、被弾すると冷たい風の音が鳴ります。",
          "系着骷髅缠腰带的墓地大力士。移动时会留下骨白残影，受击时发出阴冷的风声。");
        S("심판", "Referee", "審判", "裁判");
        S("줄무늬 심판복에 호루라기 「삑」. 발밑 모래판 선 오라가 감돌고, 처치하면 모래로 흩어지며 필살기에 호루라기와 함성이 울립니다. (업적형)",
          "A striped referee shirt and a whistle. A sand-ring line aura circles your feet; kills scatter into sand, and the ultimate rings with a whistle and a cheering crowd. (Achievement)",
          "縞模様の審判服にホイッスル「ピッ」。足元に土俵の線のオーラが漂い、倒すと砂になって散り、必殺技にはホイッスルと歓声が響きます。(実績型)",
          "条纹裁判服与哨子「哔」。脚下环绕沙场线条光环，击杀时化为沙粒散开，必杀技会响起哨声与欢呼。(成就型)");
        S("밧줄", "Rope", "ロープ", "绳索");
        S("누런 밧줄 샅바. 누런빛으로 날고 묵직한 소리가 더해집니다.",
          "A yellow rope belt. It flies in a straw-yellow glow with a heavy thud added.",
          "黄色いロープのまわし。黄色い光で飛び、重い音が加わります。",
          "黄色的绳索缠腰带。以黄色光芒飞出，并带有沉重的声响。");
        S("비단 띠", "Silk Sash", "絹の帯", "丝绸腰带");
        S("금실 수놓은 비단 띠, 던질 때 띠가 길게 나부낍니다. 반짝이는 소리가 섞입니다.",
          "A silk sash embroidered with gold thread; it streams out long when thrown. A shimmering sound is mixed in.",
          "金糸の刺繍が入った絹の帯。投げるときに長くなびきます。きらめく音が混ざります。",
          "绣有金线的丝绸腰带，投出时长长飘扬。带有闪亮的声响。");

        // ---------------- 공사 반장
        S("야간 작업", "Night Shift", "夜間作業", "夜间施工");
        S("야광 조끼에 이마 전등, 밤에도 공사는 계속된다. 피격하면 삑 소리가 납니다.",
          "A glow vest and a forehead lamp: construction goes on through the night. Getting hit beeps.",
          "蛍光ベストにヘッドランプ、夜も工事は続く。被弾するとピッと鳴ります。",
          "荧光背心配额头灯，夜里工程照旧。受击时会发出哔声。");
        S("제설차", "Snow Plow", "除雪車", "扫雪车");
        S("눈 쌓인 안전모에 빨간 목도리. 피격하면 얼음 부서지는 소리가 납니다.",
          "A snow-capped hard hat and a red scarf. Getting hit sounds like shattering ice.",
          "雪をかぶったヘルメットに赤いマフラー。被弾すると氷が砕ける音が鳴ります。",
          "戴着积雪安全帽与红围巾。受击时发出冰裂声。");
        S("지옥 공사", "Hell Works", "地獄工事", "地狱施工");
        S("뿔 돋은 그을린 안전모에 용암빛 조끼. 움직이면 불씨 잔상이 남고, 피격하면 포효 소리가 납니다.",
          "A charred hard hat with horns and a lava-glow vest. Moving leaves ember afterimages; getting hit roars.",
          "角の生えた焦げたヘルメットに溶岩色のベスト。動くと火の粉の残像が残り、被弾すると咆哮が鳴ります。",
          "长角的烧焦安全帽与熔岩色背心。移动时会留下火星残影，受击时发出咆哮。");
        S("베타 테스터", "Beta Tester", "ベータテスター", "测试员");
        S("BETA 창틀 안전모, 처치하면 초록 체크 표시. 회색 창틀 오라가 감돌고 필살기에 딸깍 소리가 울립니다. (업적형)",
          "A BETA window-frame hard hat; kills pop a green check mark. A gray window-frame aura circles you, and the ultimate rings with a click. (Achievement)",
          "BETAの窓枠ヘルメット。倒すと緑のチェックマークが跳ねます。灰色の窓枠のオーラが漂い、必殺技にはカチッという音が響きます。(実績型)",
          "BETA窗框安全帽，击杀时弹出绿色对勾。灰色窗框光环环绕，必杀技会响起点击声。(成就型)");
        S("빗자루", "Broom", "ほうき", "扫帚");
        S("커다란 마당 빗자루로 쓸어 담습니다. 베는 소리가 더해집니다.",
          "Sweeps everything up with a big yard broom. A slashing sound is added.",
          "大きな庭ぼうきで掃き集めます。斬る音が加わります。",
          "用巨大的院子扫帚扫拢一切。带有劈砍的声响。");
        S("쇠스랑", "Pitchfork", "熊手", "铁耙");
        S("갈퀴 이빨로 긁어모으는 쇠스랑. 철컹이는 소리가 섞입니다.",
          "A pitchfork that rakes things in with its tines. A clanking sound is mixed in.",
          "爪で掻き集める熊手。ガチャンという音が混ざります。",
          "用耙齿搂拢一切的铁耙。带有叮当的金属声。");
    }
}
