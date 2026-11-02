// 번역표: 인형사 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   + 갈래 효과의 피해 출처 이름(결과 화면 · 판 기록) (인형사는 31번 새 카드가 없음)
//   (Paths/Paths.Puppeteer.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
// 용어: 더미 dummy · ダミー · 假人 / 줄 string · 糸 · 丝线 / 휘두름 swing · 振り回し · 挥动 / 꼭두각시 puppet · 操り人形 · 傀儡 / 실 thread · 糸 · 丝 / 커튼콜 Curtain Call
public static partial class LocTable
{
    static void AddPathsPuppeteer()
    {
        // ---------------- 팽팽한 줄
        PA("팽팽한 줄", "Taut String", "ピンと張った糸", "绷紧的线");
        PA("줄이 칼날 · 무리 쓸기", "The string is the blade · Sweeping crowds", "糸が刃 · 群れを薙ぎ払う", "线即刀刃 · 横扫群敌");
        PA("줄 베기 피해가 60% 늘고 판정이 1.5배 굵어지며, 줄이 늘 팽팽하게 유지됩니다. (팽팽함 하한 50%)",
           "String-cut damage rises by 60%, the hitbox becomes 1.5x thicker, and the string always stays taut. (Tension never below 50%)",
           "糸斬りのダメージが60%増え、判定が1.5倍太くなり、糸が常にピンと張られます。（張り具合の下限50%）",
           "割线伤害提高60%，判定变粗1.5倍，线始终保持绷紧。（绷紧度下限50%）");
        PA("더미 휘두름 피해가 30% 줄어듭니다.", "Dummy swing damage is 30% lower.", "ダミーの振り回しダメージが30%減ります。", "假人挥动伤害降低30%。");
        PA("무리 쓸기 · 일렬", "Sweeping crowds · Lines of foes", "群れの薙ぎ払い · 一列の敵", "横扫群敌 · 成排敌人");
        PA("근접 포위 · 짧은 줄", "Being surrounded · Short strings", "近接包囲 · 短い糸", "近身包围 · 短线");
        PA("줄 끝 3분의 1(더미 쪽)으로 벤 피해가 30% 늘어납니다.",
           "Damage dealt by the last third of the string (the dummy's end) rises by 30%.",
           "糸の先端3分の1（ダミー側）で斬ったダメージが30%増えます。",
           "用线末端三分之一（假人一侧）斩出的伤害提高30%。");
        PA("줄이 보스 몸에 걸쳐 있는 동안 0.25초마다 톱질합니다. (공격력 25%)",
           "While the string rests across a boss's body, it saws every 0.25s. (25% Attack)",
           "糸がボスの体にかかっている間、0.25秒ごとに鋸引きします。（攻撃力25%）",
           "线搭在首领身上期间，每0.25秒拉锯一次。（攻击力25%）");
        PA("줄이 한 가닥 더 갈라져 10° 옆으로 함께 벱니다. (피해 70%)",
           "The string splits into one more strand that cuts 10° to the side. (70% damage)",
           "糸がもう一本分かれ、10°横で一緒に斬ります。（ダメージ70%）",
           "线再分出一股，在10°的侧方一同斩击。（伤害70%）");

        // ---------------- 호위 인형
        PA("호위 인형", "Guard Doll", "護衛人形", "护卫人偶");
        PA("곁에 두는 더미 · 생존", "A dummy kept close · Survival", "傍に置くダミー · 生存", "守在身旁的假人 · 生存");
        PA("더미가 영웅 4칸 안에 있으면 「곁에 둔 더미」가 되어 휘두름 피해가 40% 늘고, 더미 몸에 닿은 적 투사체가 사라집니다.",
           "A dummy within 4 tiles of the hero becomes a \"Guard Dummy\": its swing damage rises by 40%, and enemy projectiles touching its body vanish.",
           "ダミーが主人公から4マス以内にいると「傍らのダミー」になり、振り回しダメージが40%増え、ダミーの体に触れた敵の飛び道具が消えます。",
           "假人在英雄4格以内时会成为“身旁的假人”：挥动伤害提高40%，碰到假人身体的敌方投射物会消失。");
        PA("최대 줄 길이가 9칸에서 6칸으로 줄고, 줄 베기 피해가 30% 줄어듭니다.",
           "The maximum string length drops from 9 to 6 tiles, and string-cut damage is 30% lower.",
           "最大の糸の長さが9マスから6マスに縮み、糸斬りのダメージが30%減ります。",
           "最大线长从9格缩短到6格，割线伤害降低30%。");
        PA("생존 · 보스 근접", "Survival · Close-range bosses", "生存 · ボスへの近接", "生存 · 近身打首领");
        PA("넓은 무리 · 원거리 적", "Wide crowds · Ranged foes", "広い群れ · 遠距離の敵", "大范围群敌 · 远程敌人");
        PA("곁에 둔 더미가 있으면 받는 피해가 12% 줄어듭니다.",
           "While a Guard Dummy is beside you, you take 12% less damage.",
           "傍らのダミーがいると、受けるダメージが12%減ります。",
           "身旁有假人时，受到的伤害降低12%。");
        PA("같은 적을 1.2초 안에 이어 휘두르면 「연타」로 휘두를 때마다 피해가 8% 늘어납니다. (최대 5번)",
           "Swinging at the same foe again within 1.2s builds a \"Combo\": damage rises by 8% per swing. (Up to 5 times)",
           "同じ敵を1.2秒以内に続けて振り回すと「連打」になり、振り回すたびにダメージが8%増えます。（最大5回）",
           "1.2秒内连续挥向同一敌人会形成“连击”：每次挥动伤害提高8%。（最多5次）");
        PA("휘두름이 0.2초 뒤 한 번 더 이어집니다. (피해 60%)",
           "Each swing follows through once more 0.2s later. (60% damage)",
           "振り回しが0.2秒後にもう一度続きます。（ダメージ60%）",
           "挥动会在0.2秒后再追加一次。（伤害60%）");

        // ---------------- 바꿔치기
        PA("바꿔치기", "Quick Swap", "入れ替え", "偷换");
        PA("싼 자리 바꾸기 · 기동", "Cheap swaps · Mobility", "安い入れ替え · 機動", "廉价换位 · 机动");
        PA("자리 바꾸기에 드는 필살기 게이지가 20%에서 12%로, 최소 간격이 1.5초에서 0.8초로 줄고 가르기 피해가 50% 늘어납니다.",
           "Swapping costs 12% of the Ultimate gauge instead of 20%, the minimum gap shrinks from 1.5s to 0.8s, and the cutting damage rises by 50%.",
           "入れ替えに使う必殺技ゲージが20%から12%に、最短間隔が1.5秒から0.8秒に減り、切り裂きダメージが50%増えます。",
           "换位消耗的必杀槽从20%降到12%，最短间隔从1.5秒缩短到0.8秒，斩断伤害提高50%。");
        PA("공격 속도가 20% 느려집니다.", "Attack speed is 20% slower.", "攻撃速度が20%遅くなります。", "攻击速度降低20%。");
        PA("기동 · 생존 · 보스", "Mobility · Survival · Bosses", "機動 · 生存 · ボス", "机动 · 生存 · 首领");
        PA("서서 쌓는 화력 · 늦은 커튼콜", "Stand-still firepower · A late Curtain Call", "立ち止まって積む火力 · 遅いカーテンコール", "站桩输出 · 谢幕太晚");
        PA("자리를 바꾼 직후 0.8초 안의 첫 휘두름이 60% 더 아픕니다. (등장)",
           "The first swing within 0.8s after a swap hits 60% harder. (Entrance)",
           "入れ替えた直後0.8秒以内の最初の振り回しが60%強くなります。（登場）",
           "换位后0.8秒内的第一次挥动伤害提高60%。（登场）");
        PA("줄이 보스를 가로지르게 바꾸면 보스에 「꼬인 실」이 1 쌓이고, 3이 되면 조여 공격력 350%를 줍니다. (보스당 6초에 한 번)",
           "Swapping so the string crosses a boss adds 1 \"Tangled Thread\" to it; at 3 it tightens for 350% Attack. (Once per 6s per boss)",
           "糸がボスを横切るように入れ替えると、ボスに「もつれ糸」が1つ付き、3つになると締め上げて攻撃力350%を与えます。（ボスごとに6秒に1回）",
           "换位时让线穿过首领，会给首领叠加1层“缠结之线”，达到3层时收紧造成攻击力350%的伤害。（每个首领每6秒一次）");
        PA("가르기로 적을 쓰러뜨리면 필살기 게이지 4%를 돌려받습니다. (바꾸기 한 번에 최대 8%)",
           "Defeating a foe with the cut refunds 4% of the Ultimate gauge. (Up to 8% per swap)",
           "切り裂きで敵を倒すと、必殺技ゲージ4%が戻ります。（入れ替え1回につき最大8%）",
           "用斩断击败敌人会返还4%的必杀槽。（每次换位最多8%）");

        // ---------------- 거울 인형
        PA("거울 인형", "Mirror Doll", "鏡の人形", "镜中人偶");
        PA("좌우 대칭 두 더미 · 포위", "Two mirrored dummies · Encirclement", "左右対称の二体のダミー · 包囲", "左右对称的两个假人 · 包围");
        PA("영웅을 기준으로 커서의 좌우를 뒤집은 자리에 거울 더미가 하나 더 떠서, 같은 줄로 같이 휘두릅니다.",
           "A mirror dummy floats where the cursor would be flipped left-to-right around the hero, swinging along on the same string.",
           "主人公を基準にカーソルの左右を反転した位置に鏡のダミーがもう一体浮かび、同じ糸で一緒に振り回します。",
           "以英雄为基准，在光标左右翻转的位置多出一个镜像假人，用同一根线一起挥动。");
        PA("두 더미의 휘두름 피해가 각각 65%가 됩니다.", "Each of the two dummies swings for 65% damage.", "二体のダミーの振り回しダメージがそれぞれ65%になります。", "两个假人的挥动伤害各为65%。");
        PA("양옆 포위 · 보스 겹치기", "Flank encirclement · Overlapping on bosses", "両脇の包囲 · ボスに重ねる", "两侧包围 · 叠击首领");
        PA("조작 난도 · 기본 단일 화력", "Control difficulty · Base single-target power", "操作の難しさ · 基本の単体火力", "操作难度 · 基础单体火力");
        PA("두 더미가 1.2칸 안으로 겹쳐 같은 적을 치면 그 적이 3초 동안 받는 피해가 15% 늘어납니다.",
           "When the two dummies overlap within 1.2 tiles and hit the same foe, it takes 15% more damage for 3s.",
           "二体のダミーが1.2マス以内に重なって同じ敵を叩くと、その敵が3秒間受けるダメージが15%増えます。",
           "两个假人重叠到1.2格以内并击中同一敌人时，该敌人3秒内受到的伤害提高15%。");
        PA("더미가 겹친 채 휘두르면 「합주」로 각 피해가 65%에서 95%가 됩니다. (커서를 영웅 바로 위나 아래에 두면 겹칩니다)",
           "Swinging while the dummies overlap makes an \"Ensemble\": each deals 95% instead of 65%. (Put the cursor right above or below the hero to overlap them)",
           "ダミーが重なったまま振り回すと「合奏」になり、それぞれ65%から95%のダメージになります。（カーソルを主人公の真上か真下に置くと重なります）",
           "假人重叠时挥动会触发“合奏”：各自伤害从65%提高到95%。（把光标放在英雄正上方或正下方即可重叠）");
        PA("커서의 위아래를 뒤집은 자리에 세 번째 더미가 나옵니다. (피해 50%)",
           "A third dummy appears where the cursor would be flipped top-to-bottom. (50% damage)",
           "カーソルの上下を反転した位置に3体目のダミーが現れます。（ダメージ50%）",
           "在光标上下翻转的位置出现第三个假人。（伤害50%）");

        // ---------------- 꼭두각시
        PA("꼭두각시", "Marionette", "操り人形", "傀儡");
        PA("적을 실에 매달기 · 제어", "Hang foes on threads · Control", "敵を糸に吊るす · 制御", "把敌人吊在线上 · 控制");
        PA("꼭두각시 실", "Puppet Threads", "操り糸", "傀儡丝");
        PA("줄에 베일 때마다 적에게 실이 1 감깁니다. (최대 4) 실이 4가 된 적은 4초 동안 꼭두각시가 되어 더미 쪽으로 끌려오고, 더미가 휘두를 때 제자리에서 둘레를 벱니다. (공격력 40%) 풀릴 때 한 번 더 터집니다. (80% · 동시 4)",
           "Each string cut winds 1 thread around a foe. (Up to 4) A foe with 4 threads becomes a puppet for 4s: it is dragged toward the dummy and slashes around itself when the dummy swings. (40% Attack) It bursts once more when released. (80% · up to 4 at once)",
           "糸で斬られるたびに敵に糸が1巻き付きます。（最大4）糸が4になった敵は4秒間操り人形になってダミーの方へ引かれ、ダミーが振り回すたびにその場で周囲を斬ります。（攻撃力40%）解けるときにもう一度弾けます。（80% · 同時に4体）",
           "每次被线割到，敌人身上就会缠上1根丝。（最多4）丝达到4的敌人会变成傀儡4秒，被拉向假人，并在假人挥动时就地斩击周围。（攻击力40%）解除时再爆发一次。（80% · 同时4个）");
        PA("줄 베기 피해가 40% 줄어듭니다.", "String-cut damage is 40% lower.", "糸斬りのダメージが40%減ります。", "割线伤害降低40%。");
        PA("무리 제어 · 자동 화력", "Crowd control · Automatic firepower", "群れの制御 · 自動火力", "群体控制 · 自动输出");
        PA("보스 · 순간 폭딜", "Bosses · Burst damage", "ボス · 瞬間火力", "首领 · 瞬间爆发");
        PA("끌려가는 꼭두각시에 부딪힌 적이 피해를 입습니다. (공격력 30%)",
           "Foes bumped by a dragged puppet take damage. (30% Attack)",
           "引かれていく操り人形にぶつかった敵がダメージを受けます。（攻撃力30%）",
           "被拖拽的傀儡撞到的敌人会受到伤害。（攻击力30%）");
        PA("보스와 중간 보스에 실이 4가 되면 「얽힘」 2초: 이동이 35% 느려지고 받는 피해가 15% 늘어납니다. (12초에 한 번 · 보스를 옮기지는 않습니다)",
           "When a boss or mid-boss reaches 4 threads, it is \"Entangled\" for 2s: movement is 35% slower and it takes 15% more damage. (Once per 12s · bosses are never moved)",
           "ボスや中ボスに糸が4つ付くと「絡まり」が2秒間起き、移動が35%遅くなり、受けるダメージが15%増えます。（12秒に1回 · ボスを動かすことはありません）",
           "首领或中型首领的丝达到4时会“缠绕”2秒：移动速度降低35%，受到的伤害提高15%。（每12秒一次 · 不会移动首领）");
        PA("꼭두각시가 동시에 6마리까지, 6초 동안 이어집니다.",
           "Up to 6 puppets can be out at once, each lasting 6s.",
           "操り人形が同時に6体まで、6秒間続きます。",
           "傀儡最多同时6个，持续6秒。");

        // ---------------- 새 교차 둘
        PA("이중주", "Duet", "二重奏", "二重奏");
        PA("거울 더미 쪽 줄도 팽팽함이 적용되고, 두 줄이 0.4초 안에 같은 적을 베면 그 한 번이 두 배가 됩니다.",
           "The mirror dummy's string also gets the taut bonuses, and when both strings cut the same foe within 0.4s, that hit deals double.",
           "鏡のダミー側の糸にもピンと張った効果が適用され、二本の糸が0.4秒以内に同じ敵を斬ると、その一撃が2倍になります。",
           "镜像假人一侧的线也适用绷紧效果，两根线在0.4秒内斩中同一敌人时，那一击伤害翻倍。");
        PA("대역", "Understudy", "代役", "替身");
        PA("자리를 바꿀 때 남은 더미 대신 가장 가까운 꼭두각시가 그 자리로 끌려와 2초 동안 적을 끕니다. 더미는 바로 커서로 돌아가 무기가 묶이는 대가가 사라집니다.",
           "When you swap, the nearest puppet is dragged to that spot instead of the left-behind dummy and draws foes for 2s. The dummy returns to the cursor at once, so the weapon is no longer tied down.",
           "入れ替えるとき、残るダミーの代わりに最も近い操り人形がその場所へ引かれ、2秒間敵を引きつけます。ダミーはすぐカーソルに戻り、武器が縛られる代償がなくなります。",
           "换位时，由最近的傀儡代替留下的假人被拖到那个位置，吸引敌人2秒。假人立刻回到光标处，武器被束缚的代价随之消失。");

        // ---------------- 피해 출처 이름 (결과 화면 · 판 기록)
        PA("줄 베기", "String Cut", "糸斬り", "割线");
        PA("가르기", "Slash Through", "切り裂き", "斩断");
        PA("꼭두각시", "Marionette", "操り人形", "傀儡");
        PA("톱질", "Sawing", "鋸引き", "拉锯");
        PA("꼬인 실", "Tangled Thread", "もつれ糸", "缠结之线");
    }
}
