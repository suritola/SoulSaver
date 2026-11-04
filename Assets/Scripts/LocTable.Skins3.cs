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
        S("흰 러닝셔츠에 운동회 머리띠, 마을 대회 단골 우승자. 잡을 때 손바닥이 「짝」, 레벨업하면 만국기가 펼쳐지고 이기면 황소 트로피와 함성이 터집니다.",
          "A white tank top and a sports-day headband, the regular champion of the village tournament. Palms slap as you grab, flags of all nations unfurl on level-up, and victory brings a bull trophy and cheers.",
          "白いランニングシャツに運動会のハチマキ、村の大会の常連優勝者。つかむと手のひらが「パン」、レベルアップで万国旗が広がり、勝利すると牛のトロフィーと歓声が上がります。",
          "白背心配运动会头带，村里比赛的常胜冠军。抓取时手掌「啪」地一响，升级时万国旗展开，胜利时黄牛奖杯与欢呼齐发。");
        S("청동 샅바", "Bronze Wrestling Belt", "青銅のまわし", "青铜缠腰带");
        S("샅바와 팔 토시가 청동이라 메칠 때마다 쇳소리가 납니다. 걸으면 청동이 번쩍이고, 필살기엔 징이 울리며, 이기면 청동 트로피가 솟습니다.",
          "Bronze belt and arm guards ring like metal with every throw. The bronze glints as you walk, a gong sounds for your ultimate, and a bronze trophy rises when you win.",
          "まわしと腕当てが青銅なので、投げるたびに金属音が響きます。歩くと青銅がきらめき、必殺技ではドラが鳴り、勝つと青銅のトロフィーがそびえます。",
          "腰带与护臂都是青铜，每次摔投都铿锵作响。行走时青铜闪光，必杀技时铜锣轰鸣，胜利时青铜奖杯升起。");
        S("묘역의 장사", "Graveyard Strongman", "墓地の怪力", "墓地大力士");
        S("해골 샅바를 맨 묘역의 장사. 걸음마다 뼈가 달그락, 레벨업하면 해골 관중이 떠오르며 함성이 터지고, 쓰러지면 뼈가 와르르 무너집니다.",
          "A graveyard strongman in a skull belt. Bones rattle with each step, a skeleton crowd rises and roars on level-up, and you collapse into a heap of bones when you fall.",
          "どくろのまわしを締めた墓地の力士。歩くたびに骨がカタカタ、レベルアップでどくろの観客が浮かび歓声が上がり、倒れると骨がガラガラ崩れます。",
          "系着骷髅腰带的墓园力士。每一步骨头咔嗒作响，升级时骷髅观众浮现、欢呼四起，倒下时骨头哗啦散落。");
        S("심판", "Referee", "審判", "裁判");
        S("줄무늬 심판복에 호루라기 「삑」. 발밑에 모래가 튀고, 출발하면 깃발을 들며, 필살기에 호루라기와 함성이 울리고, 되살아날 땐 호루라기가 세 번 울립니다. (업적형)",
          "A striped referee shirt and a whistle. Sand kicks up at your feet, you raise a flag as you start moving, your ultimate brings whistles and cheers, and the whistle blows three times when you revive. (Achievement)",
          "縞模様の審判服にホイッスル「ピッ」。足元に砂が跳ね、動き出すと旗を上げ、必殺技でホイッスルと歓声が響き、復活するときはホイッスルが3回鳴ります。(実績型)",
          "条纹裁判服配哨子「哔」。脚下沙粒飞溅，起步时举起旗子，必杀技时哨声与欢呼齐响，复活时哨子连吹三声。(成就型)");
        S("밧줄", "Rope", "ロープ", "绳索");
        S("누런 밧줄 샅바. 던질 때 손끝이 누렇게 빛나며 「툭—쉭」, 맞히면 묵직하게 「퍽」 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "A yellowed rope belt. Throws glow tawny with a thud-swish, and hits land with a heavy thump. Changes the look of the basic attack before evolution.",
          "黄ばんだ縄のまわし。投げると手元が黄色く光り「トン—シュッ」、命中すると重く「ドスッ」と鳴ります。進化前の通常攻撃の見た目が変わります。",
          "泛黄的绳索腰带。投出时手边泛起黄光「咚—嗖」，命中时沉重「砰」一声。会改变进化前基础普攻的外观。");
        S("비단 띠", "Silk Sash", "絹の帯", "丝绸腰带");
        S("금실 수놓은 비단 띠, 던질 때 띠가 길게 나부낍니다. 쉴 땐 손에서 띠가 하늘거리고, 큰 한 방엔 함성이 터지며, 진화하면 띠가 용처럼 휘감깁니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "A silk sash embroidered with gold thread that streams out long when thrown. It ripples in your hand when idle, big hits draw cheers, and it coils like a dragon when it evolves. Changes the look of the basic attack before evolution.",
          "金糸を刺繍した絹の帯。投げると帯が長くなびきます。休むと手元で帯が揺らめき、大きな一撃では歓声が上がり、進化すると帯が竜のように巻きつきます。進化前の通常攻撃の見た目が変わります。",
          "金线刺绣的丝带，投出时长长飘扬。静止时在手中轻摆，重击时欢呼迸发，进化时丝带如龙盘绕。会改变进化前基础普攻的外观。");

        // ---------------- 공사 반장
        S("야간 작업", "Night Shift", "夜間作業", "夜间施工");
        S("야광 조끼에 이마 전등, 밤에도 공사는 계속된다. 발 앞을 전등 빛이 비추고, 레벨업하면 서치라이트가 엇갈리며 사이렌, 필살기에 후진 경보가 울립니다.",
          "A reflective vest and a headlamp: the work goes on through the night. Your lamp lights the ground ahead, searchlights cross with a siren on level-up, and your ultimate sounds a reversing alarm.",
          "蛍光ベストに額のライト、夜でも工事は続く。足元をライトが照らし、レベルアップでサーチライトが交差してサイレンが鳴り、必殺技でバック警報が響きます。",
          "反光背心配头灯，夜里工程照常进行。头灯照亮脚前的地面，升级时探照灯交错、警笛鸣响，必杀技响起倒车警报。");
        S("제설차", "Snow Plow", "除雪車", "扫雪车");
        S("눈 쌓인 안전모에 빨간 목도리. 걸음마다 눈이 뽀득, 퍼낼 때 눈이 흩날리고, 레벨업하면 눈사람이 솟으며 쓰러지면 눈덩이처럼 굳어 버립니다.",
          "A snow-capped hard hat and a red scarf. Snow crunches with each step, dumps throw up snow, a snowman pops up on level-up, and you freeze solid like a snowball when you fall.",
          "雪の積もったヘルメットに赤いマフラー。歩くたびに雪がキュッ、すくい出すと雪が舞い、レベルアップで雪だるまが現れ、倒れると雪玉のように固まります。",
          "积雪的安全帽配红围巾。每一步踩雪咯吱响，倾倒时雪花飞扬，升级时冒出雪人，倒下时像雪团一样冻住。");
        S("지옥 공사", "Hell Works", "地獄工事", "地狱施工");
        S("뿔 돋은 그을린 안전모에 용암빛 조끼. 걸음마다 불티가 오르고, 레벨업하면 모루가 솟아 「땅 땅」 울리며, 쓰러지면 잿빛으로 굳고 안전모만 남습니다.",
          "A scorched, horned hard hat and a lava-colored vest. Sparks rise with each step, an anvil rises and rings clang-clang on level-up, and you turn ash-gray, leaving only the hard hat, when you fall.",
          "角の生えた焦げたヘルメットに溶岩色のベスト。歩くたびに火の粉が昇り、レベルアップで金床がせり上がって「カーン、カーン」と鳴り、倒れると灰色に固まりヘルメットだけが残ります。",
          "长角的焦黑安全帽配熔岩色背心。每一步火星升腾，升级时铁砧升起「当、当」作响，倒下时化作灰色，只留下安全帽。");
        S("베타 테스터", "Beta Tester", "ベータテスター", "测试员");
        S("BETA 창틀 안전모, 처치하면 초록 체크 표시. 걸음마다 창틀 자국이 찍히고, 레벨업하면 진행 막대가 차오르며, 필살기에 딸깍, 쓰러지면 오류음과 함께 꺼집니다. (업적형)",
          "A BETA window-frame hard hat; kills pop a green check mark. Steps print window frames, a progress bar fills on level-up, your ultimate clicks, and you shut down with an error tone when you fall. (Achievement)",
          "BETAの窓枠ヘルメット。倒すと緑のチェックマークが跳ねます。歩くたびに窓枠の跡が押され、レベルアップで進捗バーが満ち、必殺技でカチッ、倒れるとエラー音とともに消えます。(実績型)",
          "BETA窗框安全帽，击杀时弹出绿色对勾。每一步留下窗框印迹，升级时进度条填满，必杀技时咔嗒一声，倒下时伴着错误提示音熄灭。(成就型)");
        S("빗자루", "Broom", "ほうき", "扫帚");
        S("커다란 마당 빗자루로 쓸어 담습니다. 쓸 때 「쓱싹」 흙알이 날리고, 맞히면 툭 털리는 소리가 납니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "Sweeps things up with a big yard broom. Each sweep swishes and kicks up grit, and hits land with a dusty thump. Changes the look of the basic attack before evolution.",
          "大きな庭ぼうきで掃き集めます。掃くと「サッサッ」と土粒が舞い、命中するとパタッとはたく音がします。進化前の通常攻撃の見た目が変わります。",
          "用大院子扫帚扫起敌人。扫动时「刷刷」扬起沙粒，命中时传来拍打声。会改变进化前基础普攻的外观。");
        S("쇠스랑", "Pitchfork", "熊手", "铁耙");
        S("갈퀴 이빨로 긁어모으는 쇠스랑. 휘두르면 「끼긱」 불티가 튀고, 쉴 땐 바닥을 긁으며, 큰 한 방엔 공중에 이빨 자국 세 줄이 남습니다. 진화 전 기본 평타의 그림이 바뀝니다.",
          "A pitchfork that rakes foes in with its tines. Swings screech and throw sparks, it scrapes the ground when idle, and big hits leave three tine marks hanging in the air. Changes the look of the basic attack before evolution.",
          "熊手の歯でかき集める鋤。振るうと「ギギッ」と火花が散り、休むと地面をひっかき、大きな一撃では空中に歯の跡が3本残ります。進化前の通常攻撃の見た目が変わります。",
          "用齿耙搂起敌人的铁叉。挥动时「嘎吱」火星四溅，静止时刮擦地面，重击时空中留下三道齿痕。会改变进化前基础普攻的外观。");
    }
}
