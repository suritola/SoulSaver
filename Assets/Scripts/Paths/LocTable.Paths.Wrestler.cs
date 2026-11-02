// 번역표: 씨름꾼 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   + 31번 새 카드 「모래판 끌기」 이름 · 설명 · 수치 이름 + 갈래 효과의 피해 출처 이름(결과 화면 · 판 기록)
//   (Paths/Paths.Wrestler.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
// 용어: 모래판 Sand Ring · 土俵 · 沙圈 / 링아웃 ring-out · 場外 · 出界 / 핀 pin · ピン · 球瓶 / 배지기 belly throw · 腹投げ · 挺腹摔 / 볼 ball · ボール · 球
public static partial class LocTable
{
    static void AddPathsWrestler()
    {
        // ---------------- 볼링
        PA("볼링", "Bowling", "ボウリング", "保龄球");
        PA("던진 적이 핀을 휩씀 · 일렬 무리", "Thrown foes sweep the pins · Lined-up crowds", "投げた敵がピンを薙ぎ倒す · 一列の群れ", "被扔的敌人横扫球瓶 · 成列群敌");
        PA("볼링 메치기", "Bowling Throw", "ボウリング投げ", "保龄摔");
        PA("던진 적이 쓰러뜨릴 수 있는 핀이 두 배로 늘고, 던짐 거리가 25% 늘며, 핀이 옆으로 3.5칸 튕깁니다.",
           "A thrown enemy can knock down twice as many pins, the throw reaches 25% farther, and pins are knocked 3.5 tiles sideways.",
           "投げた敵が倒せるピンが2倍に増え、投げる距離が25%伸び、ピンが横へ3.5マス弾かれます。",
           "被扔出的敌人可撞倒的球瓶翻倍，投掷距离增加25%，球瓶向侧面弹开3.5格。");
        PA("던진 적 자신의 메침 피해가 50% 줄어듭니다.", "The thrown enemy's own slam damage is 50% lower.", "投げた敵自身の叩きつけダメージが50%減ります。", "被扔出的敌人自身的摔击伤害降低50%。");
        PA("일렬 무리 · 경험치", "Lined-up crowds · XP", "一列の群れ · 経験値", "成列群敌 · 经验");
        PA("보스 (볼이 멈춤)", "Bosses (the ball stops)", "ボス（ボールが止まる）", "首领（球会停下）");
        PA("볼이 벽 · 경기장 끝 · 모래판 경계에 닿으면 한 번 되튕겨 계속 굴러갑니다.",
           "When the ball touches a wall, the arena edge or the sand ring border, it bounces back once and keeps rolling.",
           "ボールが壁・闘技場の端・土俵の縁に触れると一度跳ね返り、そのまま転がり続けます。",
           "球碰到墙壁、场地边缘或沙圈边界时会弹回一次，继续滚动。");
        PA("핀을 6마리 이상 쓰러뜨리면 볼이 터집니다. (반경 2.5칸 · 공격력 200%) 보스를 맞힌 볼은 쓰러뜨린 핀 하나마다 보스 피해가 10% 늘어납니다. (최대 80%)",
           "Knocking down 6 or more pins makes the ball burst. (2.5-tile radius · 200% Attack) A ball that hits a boss deals 10% more boss damage per pin knocked down. (Up to 80%)",
           "ピンを6体以上倒すとボールが炸裂します。（半径2.5マス・攻撃力200%）ボスに当たったボールは、倒したピン1体ごとにボスへのダメージが10%増えます。（最大80%）",
           "撞倒6个以上球瓶时球会爆裂。（半径2.5格 · 攻击力200%）击中首领的球，每撞倒一个球瓶，对首领的伤害提高10%。（最多80%）");
        PA("볼이 끝에서 되돌아옵니다. (왕복 · 핀 상한이 새로 참)", "The ball returns from the far end. (Round trip · the pin cap refills)", "ボールが端で折り返してきます。（往復・ピン上限が新たに満ちる）", "球会从终点折返。（往返 · 球瓶上限重新计算）");

        // ---------------- 모래판
        PA("모래판", "Sand Ring", "土俵", "沙圈");
        PA("넓고 오래 가는 모래판 · 지역 장악", "A wide, lasting ring · Area control", "広く長持ちする土俵 · 地域制圧", "又宽又久的沙圈 · 控场");
        PA("도효 장인", "Ring Master", "土俵の匠", "沙圈匠人");
        PA("모래판이 반지름 7칸 · 지속 14초로 커지고, 무거운 적 링아웃이 최대 체력 35%로 늘며, 링아웃마다 필살기 게이지가 0.5% 찹니다. (0.5초에 한 번)",
           "The sand ring grows to a 7-tile radius and lasts 14s, heavy-enemy ring-outs take 35% of max HP, and each ring-out fills 0.5% of the Ultimate gauge. (Once per 0.5s)",
           "土俵が半径7マス・持続14秒に広がり、重い敵の場外は最大体力の35%になり、場外のたびに必殺技ゲージが0.5%たまります。（0.5秒に1回）",
           "沙圈扩大为半径7格、持续14秒，重型敌人出界造成最大生命35%的伤害，每次出界充能0.5%的必杀槽。（每0.5秒一次）");
        PA("모래판 밖에 있는 동안 주는 피해가 30% 줄어듭니다.", "While outside the sand ring, you deal 30% less damage.", "土俵の外にいる間、与えるダメージが30%減ります。", "身在沙圈外时，造成的伤害降低30%。");
        PA("지역 장악 · 생존", "Area control · Survival", "地域制圧 · 生存", "控场 · 生存");
        PA("이동 강제 (침식 · 결계 · 돌진 보스)", "Forced movement (erosion · barriers · charging bosses)", "強制移動（侵食・結界・突進ボス）", "被迫移动（侵蚀 · 结界 · 冲锋首领）");
        PA("경계에 걸친 잡몹이 0.5초마다 바깥으로 1칸 미끄러집니다. (밖으로 나가면 링아웃)",
           "Small foes straddling the border slide 1 tile outward every 0.5s. (Slide out and they're ringed out)",
           "縁にかかった雑魚が0.5秒ごとに外側へ1マス滑ります。（外に出れば場外）",
           "跨在边界上的小怪每0.5秒向外滑出1格。（滑出去就算出界）");
        PA("링아웃 8번마다 모래판 지속이 가득 차고, 모래판 안의 보스는 받는 피해가 15% 늘어납니다.",
           "Every 8 ring-outs refill the sand ring's duration, and bosses inside the ring take 15% more damage.",
           "場外8回ごとに土俵の持続が満タンになり、土俵の中のボスは受けるダメージが15%増えます。",
           "每出界8次，沙圈持续时间回满；沙圈内的首领受到的伤害提高15%。");
        PA("모래판이 둘 깔립니다. (새 천하장사가 옛 모래판을 지우지 않음 · 가장 오래된 것부터 최대 2개)",
           "Two sand rings can stand at once. (A new Grand Champion no longer erases the old ring · up to 2, oldest first)",
           "土俵が2つ敷かれます。（新しい天下壮士が古い土俵を消さない・最大2つ、古い方から消える）",
           "可同时存在两个沙圈。（新的天下壮士不会抹掉旧沙圈 · 最多2个，先消失最旧的）");

        // ---------------- 인간 방패
        PA("인간 방패", "Human Shield", "人間の盾", "人肉盾牌");
        PA("든 적으로 탄을 막음 · 탄막 보스", "Block shots with a held foe · Bullet-hell bosses", "掴んだ敵で弾を防ぐ · 弾幕ボス", "用举起的敌人挡弹 · 弹幕首领");
        PA("들고 있는 시간이 10초로 늘고, 들고 있는 동안 받는 피해가 30% 줄며, 막을 때 방패가 닳는 피해가 40%로 늘어납니다.",
           "You can hold for 10s, take 30% less damage while holding, and the shield wears down by 40% damage per block.",
           "持ち上げていられる時間が10秒に伸び、持っている間の被ダメージが30%減り、防ぐときに盾が削れるダメージが40%になります。",
           "举起时间延长到10秒，举着时受到的伤害降低30%，挡弹时盾牌受到的磨损伤害增至40%。");
        PA("던짐 · 핀 피해가 25% 줄어듭니다.", "Throw and pin damage is 25% lower.", "投げとピンのダメージが25%減ります。", "投掷与球瓶伤害降低25%。");
        PA("탄막 보스 · 생존", "Bullet-hell bosses · Survival", "弾幕ボス · 生存", "弹幕首领 · 生存");
        PA("무리 처리 속도", "Crowd-clearing speed", "群れの処理速度", "清理群敌的速度");
        PA("막은 투사체가 커서 쪽으로 되튕깁니다. (공격력 50%)", "Blocked projectiles bounce back toward the cursor. (50% Attack)", "防いだ飛び道具がカーソルの方へ跳ね返ります。（攻撃力50%）", "被挡下的投射物会弹向光标方向。（攻击力50%）");
        PA("방패가 닳아 쓰러지면 터지고(반경 2.5칸 · 공격력 150%) 다음 들기가 즉시 됩니다. 보스 탄을 막을 때마다 필살기 게이지가 1% 찹니다. (한 번 들 때 최대 10%)",
           "When the shield wears out and falls, it bursts (2.5-tile radius · 150% Attack) and your next lift is instant. Each boss shot you block fills 1% of the Ultimate gauge. (Up to 10% per lift)",
           "盾が削れて倒れると爆発し（半径2.5マス・攻撃力150%）、次の持ち上げが即座に行えます。ボスの弾を防ぐたびに必殺技ゲージが1%たまります。（1回の持ち上げで最大10%）",
           "盾牌被磨损倒下时会爆炸（半径2.5格 · 攻击力150%），下一次举起立即完成。每挡下一发首领的弹幕，必杀槽充能1%。（每次举起最多10%）");
        PA("막아도 방패가 닳지 않습니다.", "Blocking no longer wears the shield down.", "防いでも盾が削れません。", "挡弹时盾牌不再磨损。");

        // ---------------- 엎어치기
        PA("엎어치기", "Overhead Slam", "叩きつけ", "过肩摔");
        PA("바닥에 내리꽂음 · 포위 무리", "Slam it to the ground · Surrounding crowds", "地面に叩きつける · 包囲した群れ", "砸向地面 · 围攻群敌");
        PA("던지는 대신 앞 1.5칸 바닥에 내리꽂습니다. (반경 2칸 + 무게 단계마다 0.4칸 · 공격력 160% · 바깥으로 2칸 밀침)",
           "Instead of throwing, you slam the enemy onto the ground 1.5 tiles ahead. (2-tile radius +0.4 per weight class · 160% Attack · knocks foes 2 tiles outward)",
           "投げる代わりに前方1.5マスの地面へ叩きつけます。（半径2マス＋重さ段階ごとに0.4マス・攻撃力160%・外側へ2マス押し出す）",
           "不再投掷，而是将敌人砸向前方1.5格的地面。（半径2格，每升一个重量等级+0.4格 · 攻击力160% · 向外推开2格）");
        PA("던질 수 없어 먼 적이나 일렬 무리를 맞힐 수 없고, 핀이 없습니다.", "You can't throw, so distant enemies and lined-up crowds can't be hit, and there are no pins.", "投げられないため遠い敵や一列の群れには当てられず、ピンもありません。", "无法投掷，打不到远处的敌人或成列群敌，也没有球瓶。");
        PA("포위 무리 · 근접", "Surrounding crowds · Close range", "包囲した群れ · 近接", "围攻群敌 · 近身");
        PA("원거리 적 · 탄막", "Ranged foes · Bullet-hell", "遠距離の敵 · 弾幕", "远程敌人 · 弹幕");
        PA("메친 자리에 2초 동안 모래 웅덩이가 남아 안의 적이 40% 느려집니다.", "A sand puddle remains for 2s where you slam, slowing enemies inside by 40%.", "叩きつけた場所に2秒間砂だまりが残り、中の敵が40%遅くなります。", "砸中的地方会留下持续2秒的沙坑，坑内敌人减速40%。");
        PA("2초 안에 세 번 메치면 세 번째가 뒤집기가 됩니다. (반경 1.6배 · 피해 2배) 보스 2칸 안에서 메치면 보스도 충격을 1.5배로 받습니다.",
           "Slamming three times within 2s makes the third one a flip. (1.6x radius · double damage) Slamming within 2 tiles of a boss makes the boss take 1.5x the shock.",
           "2秒以内に3回叩きつけると3回目がひっくり返しになります。（半径1.6倍・ダメージ2倍）ボスの2マス以内で叩きつけるとボスも衝撃を1.5倍受けます。",
           "2秒内连续砸三次，第三次会变成掀翻。（半径1.6倍 · 伤害翻倍）在首领2格内砸地时，首领也会受到1.5倍冲击。");
        PA("메친 적이 튀어 올라 한 번 더 메쳐집니다.", "The slammed enemy bounces up and is slammed once more.", "叩きつけた敵が跳ね上がり、もう一度叩きつけられます。", "被砸的敌人弹起后再被砸一次。");

        // ---------------- 장사
        PA("장사", "Strongman", "力士", "大力士");
        PA("거구를 들어 던짐 · 보스 · 큰 적", "Lift giants · Bosses and big foes", "巨体を持ち上げて投げる · ボス · 大きな敵", "举起巨躯投掷 · 首领与大型敌人");
        PA("거구 사냥", "Giant Hunting", "巨体狩り", "猎杀巨躯");
        PA("중간 보스를 들 수 있고, 무거운 적과 중간 보스의 무게 배율이 0.5 늘며, 보스 배지기 피해가 40% 늘어납니다.",
           "You can lift mid-bosses, the weight multiplier of heavy enemies and mid-bosses rises by 0.5, and belly throws on bosses deal 40% more damage.",
           "中ボスを持ち上げられるようになり、重い敵と中ボスの重さ倍率が0.5増え、ボスへの腹投げのダメージが40%増えます。",
           "可以举起中型首领，重型敌人与中型首领的重量倍率提高0.5，对首领的挺腹摔伤害提高40%。");
        PA("가벼운 적의 던짐 피해가 40% 줄고, 들기 시간이 1.5배가 됩니다.", "Light enemies deal 40% less damage when thrown, and lifting takes 1.5x as long.", "軽い敵を投げたときのダメージが40%減り、持ち上げる時間が1.5倍になります。", "投掷轻型敌人的伤害降低40%，举起时间变为1.5倍。");
        PA("보스 · 큰 적", "Bosses · Big foes", "ボス · 大きな敵", "首领 · 大型敌人");
        PA("잔챙이 무리 (15분 전 경험치)", "Small fry crowds (XP before 15 min)", "雑魚の群れ（15分前の経験値）", "杂兵群敌（15分钟前的经验）");
        PA("무거운 적이나 중간 보스를 던지면 길 둘레 1.5칸의 적이 볼 쪽으로 끌려와 핀이 됩니다.",
           "Throwing a heavy enemy or mid-boss pulls enemies within 1.5 tiles of its path toward the ball, turning them into pins.",
           "重い敵や中ボスを投げると、道の周囲1.5マスの敵がボールの方へ引き寄せられてピンになります。",
           "扔出重型敌人或中型首领时，路径周围1.5格内的敌人会被吸向球，变成球瓶。");
        PA("보스 배지기를 세 번 할 때마다 보스가 들썩여 3초 동안 받는 피해가 15% 늘어납니다.",
           "Every third belly throw on a boss makes it stagger, taking 15% more damage for 3s.",
           "ボスへの腹投げを3回するたびにボスがぐらつき、3秒間受けるダメージが15%増えます。",
           "每对首领使出三次挺腹摔，首领就会晃动，3秒内受到的伤害提高15%。");
        PA("중간 보스를 던져 벽꿍 · 링아웃시키면 피해가 두 배가 됩니다.", "Throwing a mid-boss into a wall crash or ring-out doubles the damage.", "中ボスを投げて壁にぶつける・場外にするとダメージが2倍になります。", "把中型首领扔到撞墙或出界时，伤害翻倍。");

        // ---------------- 새 교차 둘
        PA("도효 다지기", "Ring Tamping", "土俵固め押し", "夯实沙圈");
        PA("모래판 안에서 메친 충격이 경계까지 번져, 경계 1칸 안의 적을 바깥으로 밀어냅니다. (잡몹이 넘으면 링아웃)",
           "A slam inside the sand ring sends its shock out to the border, pushing enemies within 1 tile of the edge outward. (Small foes pushed over are ringed out)",
           "土俵の中で叩きつけた衝撃が縁まで広がり、縁から1マス以内の敵を外へ押し出します。（雑魚が越えれば場外）",
           "在沙圈内摔出的冲击会扩散到边界，把边界1格内的敌人推出圈外。（小怪被推出就算出界）");
        PA("거구 볼링", "Giant Bowling", "巨体ボウリング", "巨球保龄");
        PA("무거운 적 · 중간 보스를 던지면 핀 상한이 없어지고, 멈출 때 반드시 스트라이크가 터집니다.",
           "Throwing a heavy enemy or mid-boss removes the pin cap, and it always ends in a strike when it stops.",
           "重い敵や中ボスを投げるとピン上限がなくなり、止まるとき必ずストライクが炸裂します。",
           "扔出重型敌人或中型首领时不再有球瓶上限，停下时必定触发全中。");

        // ---------------- 31번 새 카드
        PA("모래판 끌기", "Drag the Ring", "土俵引き", "拖动沙圈");
        PA("내가 모래판 밖으로 나가면 모래판이 내 쪽으로 미끄러져 따라옵니다. (경계가 나에게 닿을 때까지 0.6초 · 남은 지속 그대로)",
           "When you step outside the sand ring, it slides after you. (0.6s until its border reaches you · remaining duration unchanged)",
           "自分が土俵の外に出ると、土俵が自分の方へ滑って付いてきます。（縁が自分に届くまで0.6秒・残り時間はそのまま）",
           "你走出沙圈时，沙圈会滑向你的方向。（0.6秒内边界贴到你身上 · 剩余时间不变）");
        PA("따라오는 횟수", "Follows", "付いてくる回数", "跟随次数");

        // ---------------- 피해 출처 이름 (결과 화면 · 판 기록)
        PA("방패 폭발", "Shield Burst", "盾の爆発", "盾牌爆炸");
        PA("볼 폭발", "Ball Burst", "ボール炸裂", "球体爆裂");
        PA("되튕김", "Rebound", "跳ね返し", "反弹");
        PA("링아웃", "Ring-out", "場外", "出界");
    }
}
