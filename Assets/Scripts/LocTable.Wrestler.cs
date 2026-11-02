using UnityEngine;

// 번역표 (2.0.0): 새 영웅 씨름꾼 — 이름 · 설명 · 카드 · 고유 스킬 · 진화 · 무기 · 강화 · 패시브 · 이야기 · 전령 · 기억 · 계약 · 보드
// 키 앞글자 wr. · wre. (hero-wrestler.md 결정 W8) — 이미 있는 키는 건드리지 않음
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddWrestlerLines()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 정체 · 글
        A("씨름꾼", "Wrestler", "相撲取り", "摔跤手");
        A("천하장사", "Grand Champion", "天下壮士", "天下第一力士");
        K("명절 TV 씨름 중계에서 넘어온 장사. 적을 샅바째 붙잡아 머리 위로 들어 올리고 무리 한가운데로 메다꽂습니다. 모래판 밖으로 던져 낸 적은 그대로 장외입니다.",
          "명절 TV 씨름 중계에서 넘어온 장사. 적을 샅바째 붙잡아 머리 위로 들어 올리고 무리 한가운데로 메다꽂습니다. 모래판 밖으로 던져 낸 적은 그대로 장외입니다.",
          "A strongman who stepped out of a holiday wrestling broadcast. He grabs enemies by the belt, lifts them overhead and slams them into the middle of the crowd. Anyone thrown out of the sand ring is out for good.",
          "祝日の相撲中継から飛び出してきた力士。敵をまわしごと掴んで頭上に持ち上げ、群れの真ん中に叩きつけます。土俵の外へ投げ出された敵はそのまま場外です。",
          "从节日电视摔跤转播里走出来的力士。揪住敌人的腰带举过头顶，砸进敌群中央。被扔出沙圈的敌人直接出界。");
        A("샅바", "Belt", "まわし", "腰带");
        K("마우스 가까운 적을 붙잡아 머리 위로 들어 올리고(들린 적이 날아오는 공격을 막음), 다시 누르면 마우스 쪽으로 던집니다. 던진 적은 지나가는 적을 볼링처럼 쓰러뜨리고, 큰 적일수록 느리게 들리지만 세게 날아갑니다. 누르고 있으면 잡고 던지기를 저절로 이어 갑니다",
          "마우스 가까운 적을 붙잡아 머리 위로 들어 올리고(들린 적이 날아오는 공격을 막음), 다시 누르면 마우스 쪽으로 던집니다. 던진 적은 지나가는 적을 볼링처럼 쓰러뜨리고, 큰 적일수록 느리게 들리지만 세게 날아갑니다. 누르고 있으면 잡고 던지기를 저절로 이어 갑니다.",
          "Grab the enemy nearest the cursor and lift it overhead (the lifted enemy blocks incoming attacks); press again to throw it toward the cursor. A thrown enemy bowls over whatever it passes, and bigger enemies lift slowly but hit harder. Hold to keep grabbing and throwing automatically.",
          "カーソルに近い敵を掴んで頭上に持ち上げ（持ち上げた敵が飛んでくる攻撃を防ぐ）、もう一度押すとカーソル方向に投げます。投げた敵はボウリングのように他の敵を倒し、大きい敵ほどゆっくり持ち上がりますが強く飛びます。押し続けると掴みと投げを自動で繰り返します。",
          "抓住离光标最近的敌人举过头顶（被举起的敌人会挡住飞来的攻击），再按一次朝光标方向扔出。被扔的敌人像保龄球一样撞倒路上的敌人，越大的敌人举得越慢但砸得越重。按住不放会自动连续抓取和投掷。");
        K("누르고 있으면 가장 큰 적에 표시가 붙고, 떼면 그 적에게 뛰어들어 들어 메칩니다. 모래 충격파가 둘레 적을 밀어내고, 메친 자리에 10초 동안 모래판이 깔립니다. 모래판 안에서는 받는 피해가 줄고 밀리지 않으며, 모래판 밖으로 던져 낸 적은 장외로 쓰러집니다",
          "누르고 있으면 가장 큰 적에 표시가 붙고, 떼면 그 적에게 뛰어들어 들어 메칩니다. 모래 충격파가 둘레 적을 밀어내고, 메친 자리에 10초 동안 모래판이 깔립니다. 모래판 안에서는 받는 피해가 줄고 밀리지 않으며, 모래판 밖으로 던져 낸 적은 장외로 쓰러집니다.",
          "Hold to mark the biggest enemy; release to leap at it, lift it and slam it down. A sand shockwave pushes enemies away, and a sand ring covers the spot for 10s. Inside the ring you take less damage and cannot be pushed; enemies thrown out of the ring fall as ring-outs.",
          "押し続けると一番大きい敵に印が付き、離すとその敵に飛び込んで持ち上げ、叩きつけます。砂の衝撃波が周囲の敵を押し出し、叩きつけた場所に10秒間土俵が敷かれます。土俵の中では受けるダメージが減って押されず、土俵の外へ投げ出された敵は場外で倒れます。",
          "按住会给最大的敌人加上标记，松开后扑向它，举起并砸下。沙浪冲击波把周围敌人推开，砸下的地方铺出持续10秒的沙圈。沙圈内受到的伤害减少且不会被推开，被扔出沙圈的敌人会因出界而倒下。");
        A("장외!", "Ring Out!", "場外！", "出界！");
        A("들기", "Lift", "持ち上げ", "举起");
        A("모래판", "Sand Ring", "土俵", "沙圈");
        A("새 영웅: {0}", "New hero: {0}", "新しい英雄：{0}", "新英雄：{0}");
        K("sec.hint.9", "붉은 사막 어딘가, 소 발자국이 이어진다.", "Somewhere in the red desert, a trail of hoofprints leads on.", "赤い砂漠のどこかに、牛の足跡が続いている。", "红色沙漠的某处，牛的蹄印一路延伸。");
        A("메치기 공격력", "Throw Damage", "投げ攻撃力", "摔投攻击力");
        A("메치기 공격력 · 이동 속도", "Throw Damage · Move Speed", "投げ攻撃力・移動速度", "摔投攻击力 · 移动速度");
        A("들기 — 들고 있는 동안 줄고, 바닥나면 들린 적이 발밑에 메쳐집니다. 모래판 — 남은 시간이 테두리로 줄어듭니다.",
          "Lift — drains while you carry an enemy; when empty the enemy is slammed at your feet. Sand Ring — the remaining time shrinks along its rim.",
          "持ち上げ — 持っている間減り、尽きると持ち上げた敵が足元に叩きつけられます。土俵 — 残り時間が縁で減っていきます。",
          "举起——举着敌人时减少，耗尽后被举的敌人会摔在脚下。沙圈——剩余时间沿边缘缩短。");

        // ================================================================= 전용 레벨업 카드 6
        A("도미노", "Domino", "ドミノ", "多米诺");
        A("던진 적에 처음 맞은 핀이 함께 날아가 다른 적을 쓰러뜨립니다. (핀 피해 60%)", "The first pin hit by the thrown enemy flies off too and knocks down others. (60% pin damage)", "投げた敵に最初に当たったピンも一緒に飛んで他の敵を倒します。（ピンのダメージ60%）", "被扔出的敌人最先撞到的“球瓶”也会飞出去撞倒其他敌人。（球瓶伤害60%）");
        A("함께 날아가는 핀", "Pins flying along", "一緒に飛ぶピン", "同飞的球瓶");
        A("한 아름", "Armful", "ひと抱え", "一抱");
        A("잡을 때 곁(1.2칸)의 가벼운 적을 함께 들어 올려 나란히 던집니다.", "When you grab, nearby (1.2 tiles) light enemies are lifted too and thrown side by side.", "掴むとき、そばの（1.2マス）軽い敵も一緒に持ち上げて並べて投げます。", "抓取时会连同身边（1.2格）的轻型敌人一起举起并并排扔出。");
        A("함께 드는 적", "Extra enemies lifted", "一緒に持つ敵", "同举的敌人");
        A("되치기", "Counter Throw", "切り返し", "反摔");
        A("맞는 순간 빈손이면 때린 적(3칸 안)을 곧바로 붙잡아 들어 올리고, 그 피해를 절반으로 줄입니다.", "If you are hit with empty hands, you instantly grab the attacker (within 3 tiles) and halve that damage.", "空手のときに攻撃を受けると、その敵（3マス以内）をすぐ掴んで持ち上げ、そのダメージを半分にします。", "空手时被打中会立刻抓住攻击者（3格内）并举起，该次伤害减半。");
        A("재사용 대기", "Cooldown", "クールダウン", "冷却时间");
        A("모래 둑", "Sand Levee", "砂の堤", "沙堤");
        A("모래판 경계를 넘어 들어오는 적이 모래에 발이 빠져 잠깐 크게 느려집니다. (70%)", "Enemies crossing into the sand ring sink into the sand and are greatly slowed for a moment. (70%)", "土俵の縁を越えて入ってくる敵が砂に足を取られ、しばらく大きく遅くなります。（70%）", "越过沙圈边界进来的敌人会陷进沙里，短暂大幅减速。（70%）");
        A("빠지는 시간", "Sink time", "足を取られる時間", "下陷时间");
        A("번쩍 들기", "Heave-Ho", "ひょいと持ち上げ", "一把举起");
        A("무거운 적 · 중간 보스를 들 때 걸리는 시간과 들고 걸을 때 느려짐이 줄어듭니다.", "Lifting heavy enemies and mid-bosses takes less time, and carrying them slows you less.", "重い敵・中ボスを持ち上げる時間と、持って歩くときの鈍化が減ります。", "举起重型敌人和中型首领所需的时间缩短，扛着走时的减速也减少。");
        A("줄어듦", "Reduction", "軽減", "减少");
        A("발 걸기", "Trip", "足払い", "绊腿");
        A("던진 적이 떨어진 자리 둘레(1.5칸)의 적이 넘어져 잠깐 움직이지 못합니다. (보스 제외)", "Enemies around (1.5 tiles) where the thrown enemy lands fall over and cannot move for a moment. (Bosses excluded)", "投げた敵が落ちた場所の周囲（1.5マス）の敵が転び、しばらく動けなくなります。（ボス除く）", "被扔出的敌人落地处周围（1.5格）的敌人会摔倒，短暂无法移动。（首领除外）");
        A("넘어지는 시간", "Fall time", "転ぶ時間", "摔倒时间");
        A("최대 체력", "Max HP", "最大体力", "最大生命");

        // ================================================================= 고유 스킬 10
        A("샅바 낚기", "Belt Hook", "まわし釣り", "钩腰带");
        A("손 닿는 거리에 잡을 적이 없으면 샅바를 던져 6칸 안의 적을 끌어와 잡습니다. (보스 제외)", "With nothing to grab in reach, you throw your belt to pull in and grab an enemy within 6 tiles. (Bosses excluded)", "手の届く範囲に掴む敵がいないとき、まわしを投げて6マス以内の敵を引き寄せて掴みます。（ボス除く）", "够得着的范围内没有可抓的敌人时，会甩出腰带把6格内的敌人拉过来抓住。（首领除外）");
        A("장외 패대기", "Ring-Out Slam", "場外叩きつけ", "出界重摔");
        A("링아웃된 적이 떨어진 자리에서 모래가 터져 둘레 2칸의 적이 다칩니다.", "Sand bursts where a ringed-out enemy lands, hurting enemies within 2 tiles.", "場外になった敵が落ちた場所で砂が弾け、周囲2マスの敵がダメージを受けます。", "出界的敌人落地处沙子炸开，伤害周围2格内的敌人。");
        A("모래 피해", "Sand damage", "砂のダメージ", "沙子伤害");
        A("배지기", "Belly Throw", "腹投げ", "挺腹摔");
        A("적을 다 들어 올리는 순간 배로 받쳐 쿵, 둘레 1.5칸 적을 밀치고 다치게 합니다.", "The instant you finish lifting an enemy, a belly thump pushes back and hurts enemies within 1.5 tiles.", "敵を持ち上げきった瞬間、腹で支えてドスン。周囲1.5マスの敵を押しのけてダメージを与えます。", "完全举起敌人的瞬间用肚子一顶，轰的一声推开并伤害周围1.5格内的敌人。");
        A("쿵 피해", "Thump damage", "ドスンのダメージ", "撞击伤害");
        A("황소 돌진", "Bull Rush", "雄牛の突進", "公牛冲撞");
        A("적을 든 채 0.6초 넘게 한 방향으로 달리면 들린 적으로 앞을 들이받습니다. (0.4초마다 · 밀침)", "Run in one direction for over 0.6s while carrying an enemy to ram forward with it. (Every 0.4s, knockback)", "敵を持ったまま0.6秒以上同じ方向に走ると、持ち上げた敵で前方に体当たりします。（0.4秒ごと・ノックバック）", "扛着敌人朝同一方向跑超过0.6秒，会用被举的敌人向前冲撞。（每0.4秒一次，带击退）");
        A("들이받는 피해", "Ram damage", "体当たりのダメージ", "冲撞伤害");
        A("모래판 다지기", "Pack the Sand", "土俵固め", "夯实沙圈");
        A("모래판 안에서 적을 쓰러뜨릴 때마다 모래판이 조금 더 오래 남습니다. (원래 지속의 두 배까지)", "Each enemy defeated inside the sand ring makes it last a bit longer. (Up to double the original duration)", "土俵の中で敵を倒すたびに、土俵が少し長く残ります。（元の持続の2倍まで）", "每在沙圈内击倒一个敌人，沙圈就多持续一会儿。（最多为原持续时间的两倍）");
        A("처치당", "Per defeat", "撃破ごと", "每击倒一个");
        A("끈기", "Grit", "粘り", "韧劲");
        A("적을 들고 있는 동안 맞지 않고 2초가 지날 때마다 보호막이 한 겹 생깁니다. (최대 2겹)", "While carrying an enemy, every 2s without being hit adds a layer of shield. (Max 2 layers)", "敵を持っている間、攻撃を受けずに2秒経つごとにバリアが1枚増えます。（最大2枚）", "扛着敌人时，每2秒不被打中就多一层护盾。（最多2层）");
        A("한 겹", "Per layer", "1枚あたり", "每层");
        A("기술 연결", "Combo Chain", "技の連携", "招式连携");
        A("던진 적이 땅에 닿기 전에 다음 적을 잡으면 바로 들리고, 그 적을 던질 때 피해가 늘어납니다. (최대 3연속)", "If you grab the next enemy before the thrown one lands, it lifts instantly and its throw deals more damage. (Up to 3 in a row)", "投げた敵が地面に着く前に次の敵を掴むとすぐ持ち上がり、その敵を投げるときのダメージが増えます。（最大3連続）", "在被扔的敌人落地前抓住下一个敌人会立刻举起，扔出时伤害提高。（最多连续3次）");
        A("연결당", "Per chain", "連携ごと", "每次连携");
        A("거인 메치기", "Giant Slam", "巨人投げ", "巨人摔");
        A("보스 · 중간 보스에게 배지기를 세 번 할 때마다 크게 뒤집어 둘레까지 충격이 퍼집니다.", "Every third belly throw on a boss or mid-boss flips it hard, and the shock spreads around it.", "ボス・中ボスに腹投げを3回するたびに大きくひっくり返し、周囲まで衝撃が広がります。", "每对首领或中型首领使出三次挺腹摔，就会大幅掀翻，冲击波扩散到周围。");
        A("뒤집기 피해", "Flip damage", "ひっくり返しのダメージ", "掀翻伤害");
        A("눈덩이", "Snowball", "雪だるま", "雪球");
        A("던진 적이 핀을 쓰러뜨릴 때마다 커지고 무거워집니다. (핀 하나당 · 최대 8)", "A thrown enemy grows bigger and heavier with every pin it knocks down. (Per pin, up to 8)", "投げた敵はピンを倒すたびに大きく重くなります。（ピン1つにつき・最大8）", "被扔的敌人每撞倒一个球瓶就变得更大更重。（每个球瓶一次，最多8个）");
        A("핀당 피해 · 크기", "Damage and size per pin", "ピンごとのダメージ・大きさ", "每个球瓶的伤害与体型");
        A("꽃가마", "Flower Palanquin", "花かご", "花轿");
        A("천하장사를 쓴 뒤 5초 동안 적을 잡자마자 들리고, 던진 적의 피해가 늘어납니다.", "For 5s after Grand Champion, grabbed enemies lift instantly and thrown enemies deal more damage.", "天下壮士を使った後5秒間、敵を掴むとすぐ持ち上がり、投げた敵のダメージが増えます。", "使用天下力士后的5秒内，抓住敌人立刻举起，扔出的敌人伤害提高。");
        A("던짐 피해", "Throw damage", "投げのダメージ", "投掷伤害");

        // ================================================================= 스킬 진화 7
        A("스트라이크 장사", "Strike Champion", "ストライク壮士", "全中力士");
        A("한 번에 핀을 다섯 넘게 쓰러뜨리면 볼이 터지며 핀들이 사방으로 흩어져 각자 한 번씩 더 굴러갑니다. (2세대까지)", "Knock down more than five pins at once and the ball bursts, scattering the pins to roll once more each. (Up to the 2nd generation)", "一度にピンを5つ以上倒すとボールが弾け、ピンが四方に散ってそれぞれもう一度転がります。（2世代まで）", "一次撞倒超过五个球瓶时，球会炸开，球瓶向四面八方散开各自再滚一次。（最多两代）");
        A("천하 도효", "Grand Dohyo", "天下土俵", "天下沙圈");
        A("모래판이 1.4배 넓어지고, 링아웃마다 모래판이 1초 더 남으며 장외 패대기가 두 배로 넓게 터집니다.", "The sand ring becomes 1.4x wider, lasts 1s longer per ring-out, and Ring-Out Slam bursts twice as wide.", "土俵が1.4倍に広がり、場外のたびに土俵が1秒長く残り、場外叩きつけが2倍の広さで炸裂します。", "沙圈扩大1.4倍，每次出界沙圈多持续1秒，出界重摔的范围翻倍。");
        A("철벽 샅바", "Iron Belt", "鉄壁まわし", "铁壁腰带");
        A("들고 있는 동안 밀리지 않고, 황소 돌진이 들이받은 적을 핀처럼 날려 보내며, 돌진하는 동안 뒤에서 오는 투사체도 막습니다.", "You cannot be pushed while carrying, Bull Rush sends rammed enemies flying like pins, and projectiles from behind are blocked during the rush.", "持っている間は押されず、雄牛の突進で当たった敵をピンのように吹き飛ばし、突進中は背後から来る飛び道具も防ぎます。", "举着敌人时不会被推开，公牛冲撞会把撞到的敌人像球瓶一样撞飞，冲刺时还会挡住身后飞来的投射物。");
        A("천지 메치기", "Heaven-and-Earth Slam", "天地投げ", "天地摔");
        A("들어 올릴 때와 떨어질 때 모두 둘레 2칸에 쿵이 울리고, 둘을 들어 던지면 둘이 X자로 엇갈려 날아갑니다.", "A thump rings out within 2 tiles both when lifting and when landing, and two lifted enemies fly in a crossing X when thrown.", "持ち上げるときも落ちるときも周囲2マスにドスンと響き、2体を持って投げると×字に交差して飛びます。", "举起和落地时都会在周围2格内震出一记重响，举着两个敌人扔出时会呈X形交错飞出。");
        A("황금 황소", "Golden Bull", "黄金の雄牛", "黄金公牛");
        A("보스 배지기 두 번째마다 뒤집기가 되고, 뒤집을 때 황금 황소가 달려와 보스를 들이받습니다. (공격력 300%)", "Every second belly throw on a boss becomes a flip, and a golden bull charges in to ram the boss. (300% attack)", "ボスへの腹投げ2回ごとにひっくり返しになり、そのとき黄金の雄牛が駆けてきてボスに体当たりします。（攻撃力300%）", "对首领每第二次挺腹摔都会变成掀翻，掀翻时黄金公牛会冲来撞击首领。（攻击力300%）");
        A("장외 스트라이크", "Ring-Out Strike", "場外ストライク", "出界全中");
        A("볼에 튕긴 보통 적까지 경계를 넘으면 링아웃되고, 링아웃마다 모래판이 0.5초 더 남습니다.", "Even normal-sized enemies knocked by the ball are ringed out if they cross the border, and each ring-out keeps the sand ring 0.5s longer.", "ボールに弾かれた普通の敵も境界を越えれば場外になり、場外のたびに土俵が0.5秒長く残ります。", "被球撞开的普通敌人越过边界也会出界，每次出界沙圈多持续0.5秒。");
        A("황소 장사", "Bull Champion", "雄牛壮士", "公牛力士");
        A("천하장사로 든 적이 보스가 아니면 메친 뒤에도 4초 동안 머리 위 방패로 남고(끈기 보호막 두 배), 그 적을 던지면 천하장사 충격파가 한 번 더 퍼집니다. (공격력 150%)", "A non-boss enemy lifted by Grand Champion stays overhead as a shield for 4s after the slam (Grit shields doubled), and throwing it sends out another Grand Champion shockwave. (150% attack)", "天下壮士で持ち上げた敵がボスでなければ、叩きつけた後も4秒間頭上の盾として残り（粘りのバリア2倍）、その敵を投げると天下壮士の衝撃波がもう一度広がります。（攻撃力150%）", "被天下力士举起的非首领敌人在摔下后仍会作为头顶盾牌保留4秒（韧劲护盾翻倍），把它扔出时天下力士冲击波会再扩散一次。（攻击力150%）");

        // ================================================================= 특수 능력 (형태 · 패시브) · 궁극기 · 강화
        K("던진 적이 한 단계 무겁게 날아갑니다(피해 · 핀 상한 ↑ · 거리 ↓). 잡을 적이 없으면 발밑의 들돌을 들어 던집니다. (공격력 150% · 핀 4)\n궁극기: 들돌 비",
          "던진 적이 한 단계 무겁게 날아갑니다(피해 · 핀 상한 ↑ · 거리 ↓). 잡을 적이 없으면 발밑의 들돌을 들어 던집니다. (공격력 150% · 핀 4)\n궁극기: 들돌 비",
          "Thrown enemies fly one weight class heavier (more damage and pins, shorter distance). With nothing to grab, you lift and throw the lift stone at your feet. (150% attack, 4 pins)\nUltimate: Lift Stone Rain",
          "投げた敵が一段階重く飛びます（ダメージ・ピン上限↑、距離↓）。掴む敵がいないときは足元の力石を持ち上げて投げます。（攻撃力150%・ピン4）\n究極技：力石の雨",
          "被扔出的敌人会重一个等级飞出（伤害与球瓶上限提高，距离缩短）。没有可抓的敌人时，会举起脚边的石锁扔出。（攻击力150%，球瓶4个）\n终极技：石锁雨");
        K("적을 들고 있는 동안 워낭이 울려 0.5초마다 둘레 3칸의 적에게 소리 파동이 퍼집니다. (공격력 30%)\n궁극기: 황소 떼",
          "적을 들고 있는 동안 워낭이 울려 0.5초마다 둘레 3칸의 적에게 소리 파동이 퍼집니다. (공격력 30%)\n궁극기: 황소 떼",
          "While carrying an enemy the cowbell rings, sending a sound wave every 0.5s to enemies within 3 tiles. (30% attack)\nUltimate: Bull Stampede",
          "敵を持っている間カウベルが鳴り、0.5秒ごとに周囲3マスの敵へ音の波が広がります。（攻撃力30%）\n究極技：雄牛の群れ",
          "扛着敌人时牛铃会响，每0.5秒向周围3格内的敌人扩散一道声波。（攻击力30%）\n终极技：公牛群");
        K("잡는 거리가 1.5칸 늘고, 던진 적이 밧줄에 매여 끝에서 되돌아오며 한 번 더 쓰러뜨립니다. (피해 50%)\n궁극기: 줄다리기",
          "잡는 거리가 1.5칸 늘고, 던진 적이 밧줄에 매여 끝에서 되돌아오며 한 번 더 쓰러뜨립니다. (피해 50%)\n궁극기: 줄다리기",
          "Grab range grows by 1.5 tiles, and thrown enemies are tied to a rope and return from the end, knocking things down once more. (50% damage)\nUltimate: Tug of War",
          "掴む距離が1.5マス伸び、投げた敵が縄に繋がれて端から戻ってきて、もう一度倒します。（ダメージ50%）\n究極技：綱引き",
          "抓取距离增加1.5格，被扔出的敌人拴着绳子从尽头折返，再撞倒一次。（伤害50%）\n终极技：拔河");
        K("밀쳐지지 않고, 적과 부딪혀 받는 피해가 10% 줄어듭니다.\n진화: 18%", "밀쳐지지 않고, 적과 부딪혀 받는 피해가 10% 줄어듭니다.\n진화: 18%",
          "You cannot be shoved, and damage taken from colliding with enemies is reduced by 10%.\nEvolved: 18%", "押されなくなり、敵とぶつかって受けるダメージが10%減ります。\n進化：18%", "不会被推开，与敌人碰撞受到的伤害减少10%。\n进化：18%");
        K("체력이 30% 아래면 들기 · 던지기가 25% 빨라집니다.\n진화: 40%", "체력이 30% 아래면 들기 · 던지기가 25% 빨라집니다.\n진화: 40%",
          "Below 30% health, lifting and throwing are 25% faster.\nEvolved: 40%", "体力が30%未満のとき、持ち上げ・投げが25%速くなります。\n進化：40%", "生命低于30%时，举起和投掷加快25%。\n进化：40%");
        K("무거운 적이나 중간 보스를 쓰러뜨리면 체력을 3 회복합니다.\n진화: 6", "무거운 적이나 중간 보스를 쓰러뜨리면 체력을 3 회복합니다.\n진화: 6",
          "Defeating a heavy enemy or mid-boss restores 3 health.\nEvolved: 6", "重い敵や中ボスを倒すと体力を3回復します。\n進化：6", "击倒重型敌人或中型首领时恢复3点生命。\n进化：6");
        K("링아웃시킬 때마다 필살기 게이지가 1.5% 찹니다. (0.5초에 한 번까지)\n진화: 2.5%", "링아웃시킬 때마다 필살기 게이지가 1.5% 찹니다. (0.5초에 한 번까지)\n진화: 2.5%",
          "Each ring-out fills 1.5% of the ultimate gauge. (At most once per 0.5s)\nEvolved: 2.5%", "場外にさせるたびに必殺技ゲージが1.5%溜まります。（0.5秒に1回まで）\n進化：2.5%", "每让敌人出界一次，必杀技槽增加1.5%。（每0.5秒最多一次）\n进化：2.5%");
        A("들돌", "Lift Stone", "力石", "石锁");
        A("워낭", "Cowbell", "カウベル", "牛铃");
        A("밧줄 샅바", "Rope Belt", "縄のまわし", "绳索腰带");
        A("통뼈", "Sturdy Bones", "頑丈な骨", "铁骨");
        A("모래 근성", "Sand Grit", "砂の根性", "沙地韧性");
        A("장사 밥상", "Champion's Feast", "力士の膳", "力士饭桌");
        A("관중의 환호", "Crowd Cheer", "観客の歓声", "观众欢呼");
        A("던진 적이 한 단계 무겁게 날아갑니다(피해 · 핀 상한 ↑ · 거리 ↓). 잡을 적이 없으면 발밑의 들돌을 들어 던집니다. (공격력 150% · 핀 4)", "Thrown enemies fly one weight class heavier (more damage and pins, shorter distance). With nothing to grab, you lift and throw the lift stone at your feet. (150% attack, 4 pins)", "投げた敵が一段階重く飛びます（ダメージ・ピン上限↑、距離↓）。掴む敵がいないときは足元の力石を持ち上げて投げます。（攻撃力150%・ピン4）", "被扔出的敌人会重一个等级飞出（伤害与球瓶上限提高，距离缩短）。没有可抓的敌人时，会举起脚边的石锁扔出。（攻击力150%，球瓶4个）");
        A("적을 들고 있는 동안 워낭이 울려 0.5초마다 둘레 3칸의 적에게 소리 파동이 퍼집니다. (공격력 30%)", "While carrying an enemy the cowbell rings, sending a sound wave every 0.5s to enemies within 3 tiles. (30% attack)", "敵を持っている間カウベルが鳴り、0.5秒ごとに周囲3マスの敵へ音の波が広がります。（攻撃力30%）", "扛着敌人时牛铃会响，每0.5秒向周围3格内的敌人扩散一道声波。（攻击力30%）");
        A("잡는 거리가 1.5칸 늘고, 던진 적이 밧줄에 매여 끝에서 되돌아오며 한 번 더 쓰러뜨립니다. (피해 50%)", "Grab range grows by 1.5 tiles, and thrown enemies are tied to a rope and return from the end, knocking things down once more. (50% damage)", "掴む距離が1.5マス伸び、投げた敵が縄に繋がれて端から戻ってきて、もう一度倒します。（ダメージ50%）", "抓取距离增加1.5格，被扔出的敌人拴着绳子从尽头折返，再撞倒一次。（伤害50%）");
        A("들돌 비", "Lift Stone Rain", "力石の雨", "石锁雨");
        A("마우스 둘레에 들돌 여덟 개가 차례로 떨어져 터집니다. (공격력 128%씩)", "Eight lift stones fall around the cursor one after another and burst. (128% attack each)", "カーソルの周りに力石が8個、順番に落ちて炸裂します。（攻撃力128%ずつ）", "光标周围依次落下八个石锁并爆开。（每个攻击力128%）");
        A("황소 떼", "Bull Stampede", "雄牛の群れ", "公牛群");
        A("황소 다섯 마리가 마우스 쪽으로 달려가며 닿는 적을 날려 버립니다. (공격력 192%씩)", "Five bulls charge toward the cursor, blasting away any enemy they touch. (192% attack each)", "雄牛5頭がカーソルの方へ駆け、触れた敵を吹き飛ばします。（攻撃力192%ずつ）", "五头公牛朝光标方向冲去，撞飞碰到的敌人。（每头攻击力192%）");
        A("줄다리기", "Tug of War", "綱引き", "拔河");
        A("마우스 쪽으로 밧줄을 던져 한 줄의 적을 꿰어 당긴 뒤 내 등 뒤로 한꺼번에 넘겨 메칩니다. (공격력 192%)", "Throw a rope toward the cursor to string up a line of enemies, haul them in and slam them all behind you at once. (192% attack)", "カーソル方向へ縄を投げて一列の敵を絡め取り、引き寄せて背後へ一斉に投げ飛ばして叩きつけます。（攻撃力192%）", "朝光标方向甩出绳索串起一排敌人，拉过来后一并甩到背后砸下。（攻击力192%）");
        A("안다리", "Inner Leg Trip", "内掛け", "里勾腿");
        A("적을 잡는 순간 둘레 1.5칸의 적이 넘어져 0.5초 동안 움직이지 못합니다. (보스 제외)", "The moment you grab an enemy, enemies within 1.5 tiles fall over and cannot move for 0.5s. (Bosses excluded)", "敵を掴んだ瞬間、周囲1.5マスの敵が転んで0.5秒間動けなくなります。（ボス除く）", "抓住敌人的瞬间，周围1.5格内的敌人会摔倒，0.5秒内无法移动。（首领除外）");
        A("뒤집기", "Flip", "ひっくり返し", "翻转");
        A("던진 적이 떨어진 뒤 한 번 더 튀어 올라 메쳐집니다. (피해 50%)", "After landing, the thrown enemy bounces up and is slammed once more. (50% damage)", "投げた敵は落ちた後もう一度跳ね上がって叩きつけられます。（ダメージ50%）", "被扔出的敌人落地后会再弹起并再摔一次。（伤害50%）");
        A("모래 바람", "Sand Gust", "砂風", "沙风");
        A("던지면 0.15초 뒤 같은 길로 모래 바람이 한 번 더 쓸고 지나갑니다. (핀 피해 50%)", "0.15s after a throw, a gust of sand sweeps along the same path once more. (50% pin damage)", "投げると0.15秒後に同じ道を砂風がもう一度吹き抜けます。（ピンのダメージ50%）", "扔出后0.15秒，一阵沙风会沿同一条路再扫过一次。（球瓶伤害50%）");

        // ================================================================= 이야기
        A("킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 것들까지 꿈틀대기 시작했다.", "When the King Slime swallowed the meadow, even the things sleeping underground began to stir.", "キングスライムが草原を飲み込むと、地面の下で眠るものたちまでうごめき始めた。", "史莱姆王吞没草原后，连沉睡在地下的东西也开始蠕动。");
        A("명절 씨름 중계가 한창이던 밤, 화면 속 장사가 모래판째 초원으로 굴러 나왔다.", "One night, in the middle of a holiday wrestling broadcast, the strongman on screen rolled out into the meadow, sand ring and all.", "祝日の相撲中継が盛り上がっていた夜、画面の中の力士が土俵ごと草原へ転がり出てきた。", "节日摔跤转播正酣的夜里，屏幕里的力士连同沙圈一起滚到了草原上。");
        A("상품으로 걸린 황소가 보이지 않았다. 발굽 자국은 슬라임이 지나간 쪽으로 이어져 있었다.", "The prize bull was nowhere to be seen. Hoofprints led off where the slime had passed.", "賞品の雄牛が見当たらなかった。蹄の跡はスライムが通った方へ続いていた。", "作为奖品的公牛不见了。蹄印一路延伸向史莱姆经过的方向。");
        A("“황소 돌려받기 전엔 경기 안 끝난 겁니다.” 장사는 샅바를 고쳐 맸다.", "\"The match isn't over until I get my bull back.\" The strongman retied his belt.", "「雄牛を取り返すまで試合は終わりません。」力士はまわしを締め直した。", "“没把公牛要回来，比赛就没结束。”力士重新系紧了腰带。");
        A("리치 왕이 지하 묘역을 깨우자, 죽은 자들이 땅 위로 기어 나왔다.", "When the Lich King awakened the crypt, the dead crawled up to the surface.", "リッチキングが地下墓地を目覚めさせると、死者たちが地上へはい出てきた。", "巫妖王唤醒地下墓穴后，死者们爬到了地面上。");
        A("TV 앞에서 졸던 개발자의 화면이 지직거리더니, 씨름 중계 속 장사가 모래와 함께 쏟아져 나왔다.", "The screen of a developer dozing in front of the TV crackled, and the strongman from the wrestling broadcast poured out with a spill of sand.", "テレビの前でうたた寝していた開発者の画面がジジッと乱れ、相撲中継の力士が砂とともにこぼれ出てきた。", "在电视前打盹的开发者的屏幕滋滋作响，摔跤转播里的力士连同沙子一起涌了出来。");
        A("신전 입구 모래 위에 커다란 발굽 자국이 찍혀 있었다. 상품 황소의 것이었다.", "Huge hoofprints were pressed into the sand at the temple entrance. They belonged to the prize bull.", "神殿の入口の砂の上に大きな蹄の跡が刻まれていた。賞品の雄牛のものだった。", "神殿入口的沙地上印着巨大的蹄印，那是奖品公牛留下的。");
        A("장사는 구멍 앞에서 허리를 한 번 숙였다. 씨름은 인사로 시작하는 법이다.", "The strongman bowed once before the hole. Wrestling always begins with a bow.", "力士は穴の前で一度腰を折って礼をした。相撲は挨拶から始まるものだ。", "力士在洞口前躬身行了一礼。摔跤总是从行礼开始的。");
        A("장사는 모래를 털고, 보이지 않는 관중석을 향해 두 팔을 들었다.", "The strongman brushed off the sand and raised both arms toward the unseen stands.", "力士は砂を払い、見えない観客席に向かって両腕を上げた。", "力士拍掉身上的沙子，朝着看不见的观众席高举双臂。");
        A("어디선가 함성이 들렸다. 명절 TV의 그 함성이었다.", "A roar rose from somewhere. It was the very cheer from the holiday broadcast.", "どこからか歓声が聞こえた。祝日のテレビのあの歓声だった。", "不知从哪里传来了欢呼声，正是节日电视里的那种欢呼。");
        A("“천하장사, 확정입니다.”", "\"The Grand Champion is decided.\"", "「天下壮士、確定です。」", "“天下力士，确定无疑。”");
        A("하지만 황소는 아직 돌아오지 않았다. 붉은 사막 쪽에서 「음머어」 소리가 바람에 실려 왔다...", "But the bull had not come back. From the red desert, a \"moo\" drifted in on the wind...", "しかし雄牛はまだ戻っていなかった。赤い砂漠のほうから「モォー」という声が風に乗って届いた……", "但公牛还没有回来。红色沙漠的方向，“哞——”的叫声随风飘来……");

        // ================================================================= 전령 대사 6줄
        K("herald.hero.wrestler.1", "장사가 모래 위에 등을 댔다.\n“한 판 졌소. 씨름은 삼판양승이오.”", "The strongman lay on his back in the sand.\n\"One bout lost. Wrestling is best of three.\"", "力士が砂の上に背をついた。\n「一番負けたな。相撲は三番勝負だ。」", "力士背部着了沙。\n“输了一局。摔跤是三局两胜。”");
        K("herald.hero.wrestler.2", "너무 많은 것이 한꺼번에 샅바를 잡았다.\n“샅바는 하나씩 잡는 거요. 저쪽이 반칙이지.”", "Too many things grabbed his belt at once.\n\"You grab a belt one at a time. That's a foul on their part.\"", "あまりに多くのものが一度にまわしを掴んだ。\n「まわしは一人ずつ取るものだ。あっちの反則だな。」", "太多东西同时抓住了他的腰带。\n“腰带是一个一个抓的，那边犯规了。”");
        K("herald.hero.wrestler.3", "멀리서 던지는 것들은 잡히지 않았다.\n“던지는 놈은 안 잡히니, 잡힌 놈으로 막는 거요.”", "He couldn't grab the things thrown from afar.\n\"Throwers can't be grabbed, so I block with whatever I've grabbed.\"", "遠くから投げられるものは掴めなかった。\n「投げてくる奴は掴めないから、掴んだ奴で防ぐのだ。」", "远处扔来的东西抓不住。\n“扔东西的抓不到，就拿抓到的去挡。”");
        K("herald.hero.wrestler.4", "리치 왕은 들리지 않았다.\n“무게가 있는 건 다 들려요. 저건 무게가 없어. 마음이 비었거든.”", "The Lich King wouldn't lift.\n\"Anything with weight can be lifted. That one has none. Its heart is empty.\"", "リッチキングは持ち上がらなかった。\n「重さのあるものは全部持ち上がる。あれには重さがない。心が空っぽだからな。」", "巫妖王举不起来。\n“有分量的都举得起来，那个没有分量，因为心是空的。”");
        K("herald.hero.wrestler.5", "거울 속의 장사가 너를 먼저 들었다.\n“거울하고는 샅바 잡기가 어렵네. 손이 반대라.”", "The strongman in the mirror lifted you first.\n\"Hard to get a belt grip on a mirror. The hands are reversed.\"", "鏡の中の力士が先にお前を持ち上げた。\n「鏡相手はまわしが取りにくいな。手が逆だ。」", "镜中的力士先把你举了起来。\n“和镜子抓腰带真难，手是反的。”");
        K("herald.hero.wrestler.6", "모래판이 끝났다. 너는 이겼다.\n“아직 꽃가마를 안 탔는데요.”", "The sand ring is over. You won.\n\"But I haven't ridden the flower palanquin yet.\"", "土俵は終わった。お前は勝った。\n「まだ花かごに乗っていないんですが。」", "沙圈结束了，你赢了。\n“我还没坐上花轿呢。”");
        K("bossmem.noun.wrestler", "메치기", "throws", "投げ", "摔投");

        // ================================================================= 기억 조각 6
        K("memory.wrestler.1.title", "TV 밖으로", "Out of the TV", "テレビの外へ", "走出电视");
        K("memory.wrestler.1.text", "명절 밤 결승이었다. 개발자가 TV 앞에서 졸던 순간 화면이 지직거렸고, 장사는 들배지기를 하다 화면 밖 모래로 떨어졌다.\n처음 쓰러진 날 전령이 말했다. 「두 번째 죽음이다.」\n“아니, 두 번째 판이오.”",
          "It was the holiday-night final. The moment the developer dozed off in front of the TV, the screen crackled, and the strongman, mid belly lift, fell onto sand outside the screen.\nThe day he first fell, the herald said, \"This is your second death.\"\n\"No. The second bout.\"",
          "祝日の夜の決勝だった。開発者がテレビの前でうたた寝した瞬間、画面がジジッと乱れ、力士は吊り出しの最中に画面の外の砂へ落ちた。\n初めて倒れた日、伝令は言った。「二度目の死だ。」\n「いや、二番目の取組だ。」",
          "那是节日夜晚的决赛。开发者在电视前打盹的瞬间，屏幕滋滋作响，力士在使出挺腹举的途中掉到了屏幕外的沙地上。\n他第一次倒下的那天，传令者说：“这是第二次死亡。”\n“不，是第二局。”");
        K("memory.wrestler.2.title", "삼판양승", "Best of Three", "三番勝負", "三局两胜");
        K("memory.wrestler.2.text", "다섯 번 쓰러져도 그는 「세 판 중 두 판」을 센다.\n진 판을 세지 않는 것이 아니다. 다음 판이 늘 있다는 것을 믿는 것이다.",
          "Even after falling five times, he counts \"two out of three.\"\nIt isn't that he ignores the lost bouts. He simply believes there is always a next bout.",
          "五回倒れても、彼は「三番のうち二番」を数える。\n負けた取組を数えないわけではない。次の取組がいつでもあると信じているのだ。",
          "即使倒下五次，他数的仍是“三局两胜”。\n并不是不把输掉的局算进去，而是相信下一局永远都在。");
        K("memory.wrestler.3.title", "상품 황소", "The Prize Bull", "賞品の雄牛", "奖品公牛");
        K("memory.wrestler.3.text", "이번 황소는 그의 열 번째 상품이다. 앞의 아홉은 모두 고향 마을에 보냈다.\n열 번째만은 데려가고 싶었다.\n「저 녀석은 저를 처음 이긴 소라서.」",
          "This bull is his tenth prize. He sent the previous nine to his home village.\nThe tenth one, he wanted to take home himself.\n\"That one was the first bull to ever beat me.\"",
          "今回の雄牛は彼の十個目の賞品だ。前の九頭はすべて故郷の村に送った。\n十頭目だけは連れて帰りたかった。\n「あいつは、私に初めて勝った牛だからな。」",
          "这头公牛是他的第十件奖品。前九件都送回了老家的村子。\n唯独第十件，他想亲自带走。\n“那家伙是第一头赢过我的牛。”");
        K("memory.wrestler.4.title", "들배지기", "The Belly Lift", "吊り出し", "挺腹举");
        K("memory.wrestler.4.text", "힘으로 드는 게 아니라 허리로 든다.\n상대가 밀어 오는 무게를 빌려 들어 올리는 것.\n무거운 적일수록 잘 들린다는 말은 그래서 거짓이 아니다.",
          "You don't lift with strength, you lift with your waist.\nYou borrow the weight your opponent pushes at you and heave it up.\nThat is why \"the heavier the foe, the easier it lifts\" is no lie.",
          "力で持ち上げるのではなく、腰で持ち上げる。\n相手が押してくる重さを借りて持ち上げるのだ。\n重い敵ほどよく持ち上がるという言葉は、だから嘘ではない。",
          "不是靠力气举，而是靠腰举。\n借用对手推来的重量把他举起。\n所以“越重的敌人越好举”这话并不假。");
        K("memory.wrestler.5.title", "모래판", "The Sand Ring", "土俵", "沙圈");
        K("memory.wrestler.5.text", "원 하나를 긋고 나가면 진다. 세상에서 가장 공정한 규칙.\n그는 사막에 원을 그을 때마다 「여긴 내 판」이라고 중얼거린다.",
          "Draw one circle, and whoever steps out loses. The fairest rule in the world.\nEvery time he draws a circle in the desert, he mutters, \"This is my ring.\"",
          "円をひとつ描き、出たら負け。世界で最も公平なルール。\n彼は砂漠に円を描くたびに「ここは俺の土俵だ」とつぶやく。",
          "画一个圈，出圈就输。世上最公平的规则。\n每次在沙漠里画圈，他都会嘟囔一句：“这是我的场子。”");
        K("memory.wrestler.6.title", "천하장사", "Grand Champion", "天下壮士", "天下力士");
        K("memory.wrestler.6.text", "칭호는 한 해에 한 사람. 그런데 이 세계엔 모래판이 끝없다.\n마지막 상대가 누구인지 그는 안다 — TV를 켜 둔 채 잠든 사람.\n「깨워서 인사는 하고 붙어야지.」",
          "The title goes to one person a year. But in this world, the sand rings never end.\nHe knows who the last opponent is: the person who fell asleep with the TV on.\n\"I'll wake him up and greet him properly before we grapple.\"",
          "称号は一年にひとり。だがこの世界には土俵が果てしなく続いている。\n最後の相手が誰か、彼は知っている――テレビをつけたまま眠った人。\n「起こして挨拶してから組まないとな。」",
          "称号一年只有一人。可这个世界的沙圈没有尽头。\n他知道最后的对手是谁——开着电视睡着的那个人。\n“得先把他叫醒，打过招呼再交手。”");

        // ================================================================= 영혼 계약 · 보드
        K("contract.h.wrestler.1.name", "백 번 메치기", "A Hundred Throws", "百回投げ", "一百次摔投");
        K("contract.h.wrestler.1.cond", "적 100번 던지기", "Throw enemies 100 times", "敵を100回投げる", "投掷敌人100次");
        K("contract.h.wrestler.2.name", "열 번의 천하장사", "Ten Grand Champions", "十度の天下壮士", "十次天下力士");
        K("contract.h.wrestler.2.cond", "필살기 10번 쓰기", "Use the ultimate 10 times", "必殺技を10回使う", "使用必杀技10次");
        K("contract.h.wrestler.3.name", "장외!", "Ring Out!", "場外！", "出界！");
        K("contract.h.wrestler.3.cond", "한 판에 링아웃 40번", "Ring out enemies 40 times in one run", "1プレイで場外を40回", "一局中让敌人出界40次");
        K("contract.h.wrestler.4.name", "스트라이크", "Strike", "ストライク", "全中");
        K("contract.h.wrestler.4.cond", "한 번 던져 적 6마리 쓰러뜨리기 8번", "Knock down 6 enemies with a single throw, 8 times", "1回の投げで敵6体を倒すを8回", "一次投掷击倒6个敌人，共8次");
        K("contract.h.wrestler.6.name", "삼백 번 메치기", "Three Hundred Throws", "三百回投げ", "三百次摔投");
        K("contract.h.wrestler.6.cond", "적 300번 던지기", "Throw enemies 300 times", "敵を300回投げる", "投掷敌人300次");
        K("board.wrestler.1.name", "천 번의 메치기", "A Thousand Throws", "千回の投げ", "一千次摔投");
        K("board.wrestler.1.desc", "던지고 또 던졌습니다. 샅바는 닳지 않았습니다.", "Thrown and thrown again. The belt never wore out.", "投げて、また投げました。まわしは擦り切れませんでした。", "扔了又扔，腰带却没有磨损。");
        K("board.wrestler.1.cond", "적 1,000번 던지기", "Throw enemies 1,000 times", "敵を1,000回投げる", "投掷敌人1,000次");
        K("board.wrestler.2.name", "네 왕을 들어 올린 손", "The Hands That Lifted Four Kings", "四王を持ち上げた手", "举起四王的手");
        K("board.wrestler.2.desc", "왕들은 무거웠습니다. 그래도 들렸습니다.", "The kings were heavy. They lifted all the same.", "王たちは重かった。それでも持ち上がりました。", "国王们很重，但还是被举起来了。");
        K("board.wrestler.2.cond", "씨름꾼으로 장 보스 4종 모두 쓰러뜨리기", "Defeat all four chapter bosses as the Wrestler", "相撲取りで章のボス4種をすべて倒す", "以摔跤手击败全部四种章节首领");
        K("board.wrestler.3.name", "장외승", "Win by Ring-Out", "場外勝ち", "出界胜");
        K("board.wrestler.3.desc", "경계 밖으로 던진 이만 이백. 판정승입니다.", "Two hundred thrown past the rope. A win on points.", "縁の外へ投げ出したのは二百。判定勝ちです。", "扔出绳圈的足有两百个，判定获胜。");
        K("board.wrestler.3.cond", "링아웃 200번", "Ring out enemies 200 times", "場外を200回", "让敌人出界200次");
    }
}
