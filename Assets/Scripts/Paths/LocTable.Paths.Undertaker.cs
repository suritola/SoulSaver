// 번역표: 장의사 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   + 갈래 효과의 피해 출처 이름(결과 화면 · 판 기록) (장의사는 31번 새 카드가 없음)
//   (Paths/Paths.Undertaker.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
// 용어: 구멍 hole · 穴 · 坑 / 매장 burial · 埋葬 · 掩埋 / 묘비 gravestone · 墓石 · 墓碑 / 장례 Funeral · 葬儀 · 葬礼 / 땅굴 tunnel · トンネル · 地道 / 갇힘 trapped · 閉じ込め · 困住
public static partial class LocTable
{
    static void AddPathsUndertaker()
    {
        // ---------------- 생매장
        PA("생매장", "Buried Alive", "生き埋め", "活埋");
        PA("오래 가두고 한꺼번에 · 무리 매장", "Hold them long, bury them all at once · Mass burial", "長く閉じ込めて一度に · 群れの埋葬", "久困一并掩埋 · 成群活埋");
        PA("깊이 파기", "Dig Deep", "深掘り", "深挖");
        PA("갇힘 시간이 1.4초에서 2.2초로 늘고, 갇힌 적에게 주는 매장 피해가 35% 늘어납니다.",
           "Trap time rises from 1.4s to 2.2s, and burial damage against trapped foes rises by 35%.",
           "閉じ込め時間が1.4秒から2.2秒に増え、閉じ込めた敵への埋葬ダメージが35%増えます。",
           "困住时间从1.4秒延长到2.2秒，对被困敌人的掩埋伤害提高35%。");
        PA("구멍이 넓어지는 속도가 30% 느려집니다.", "Holes widen 30% slower.", "穴が広がる速さが30%遅くなります。", "坑扩大的速度降低30%。");
        PA("밀집 무리", "Dense crowds", "密集した群れ", "密集的敌群");
        PA("흩어진 적 · 보스 단독", "Scattered foes · A lone boss", "散らばった敵 · ボス単独", "分散的敌人 · 单独首领");
        PA("갇힌 적이 2마리 이상인 구멍은 반지름이 0.5칸 늘어납니다.",
           "A hole with 2 or more trapped foes grows 0.5 tiles wider in radius.",
           "閉じ込めた敵が2体以上いる穴は、半径が0.5マス広がります。",
           "被困敌人达到2只以上的坑，半径增加0.5格。");
        PA("보스가 구멍 위에 있으면 매장할 때 그 구멍에 갇힌 적 하나당 보스 피해가 15% 늘어납니다. (최대 60%)",
           "If a boss is over the hole when you bury it, boss damage rises by 15% for each trapped foe in that hole. (Up to 60%)",
           "ボスが穴の上にいる状態で埋葬すると、その穴に閉じ込めた敵1体につきボスへのダメージが15%増えます。（最大60%）",
           "首领在坑上方时掩埋，该坑里每有一个被困敌人，对首领的伤害提高15%。（最多60%）");
        PA("갇힘 시간이 3초가 되고, 구멍을 한 개 더 둘 수 있습니다.",
           "Trap time becomes 3s, and you can keep one more hole.",
           "閉じ込め時間が3秒になり、穴をもう1つ置けます。",
           "困住时间变为3秒，并且可以多留一个坑。");

        // ---------------- 묘역 설계
        PA("묘역 설계", "Graveyard Design", "墓地設計", "墓园设计");
        PA("묘비로 짓는 담 · 길 막기", "Walls built of gravestones · Blocking paths", "墓石で築く塀 · 道を塞ぐ", "用墓碑筑墙 · 封路");
        PA("묘지기", "Gravekeeper", "墓守", "守墓人");
        PA("빈 구멍을 매장해도 빈 묘비(내구 4)가 솟고, 묘비 상한이 8개에서 14개로, 수명이 20초에서 35초로 늘어납니다.",
           "Burying an empty hole still raises an empty gravestone (durability 4), and the gravestone cap rises from 8 to 14 with a lifespan of 35s instead of 20s.",
           "空の穴を埋葬しても空の墓石（耐久4）が立ち、墓石の上限が8個から14個に、寿命が20秒から35秒に増えます。",
           "掩埋空坑也会立起一块空墓碑（耐久4），墓碑上限从8块提高到14块，寿命从20秒延长到35秒。");
        PA("매장 피해가 30% 줄어듭니다.", "Burial damage is 30% lower.", "埋葬ダメージが30%減ります。", "掩埋伤害降低30%。");
        PA("생존 · 길 막기 · 엄폐", "Survival · Blocking paths · Cover", "生存 · 道を塞ぐ · 遮蔽", "生存 · 封路 · 掩护");
        PA("화력 · 빠른 무리", "Firepower · Fast crowds", "火力 · 素早い群れ", "输出 · 快速敌群");
        PA("묘비 둘레 2칸 안의 적이 받는 피해가 기본으로 10% 늘어납니다. (비문 카드와 더해집니다)",
           "Foes within 2 tiles of a gravestone take 10% more damage by default. (Stacks with the Epitaph card)",
           "墓石の周囲2マス以内の敵が受けるダメージが、基本で10%増えます。（碑文カードと加算されます）",
           "墓碑周围2格内的敌人默认多受到10%的伤害。（与碑文卡片叠加）");
        PA("보스가 묘비를 부수면 무너진 흙이 보스에게 공격력 250%를 줍니다. (1초에 한 번)",
           "When a boss smashes a gravestone, the collapsing earth deals 250% Attack to it. (Once per second)",
           "ボスが墓石を壊すと、崩れた土がボスに攻撃力250%を与えます。（1秒に1回）",
           "首领撞毁墓碑时，崩落的泥土对其造成攻击力250%的伤害。（每秒一次）");
        PA("묘비 내구가 두 배가 되고 묘비 상한이 20개가 됩니다.",
           "Gravestone durability doubles and the gravestone cap becomes 20.",
           "墓石の耐久が2倍になり、墓石の上限が20個になります。",
           "墓碑耐久翻倍，墓碑上限变为20块。");

        // ---------------- 땅굴
        PA("땅굴", "Tunnel", "トンネル", "地道");
        PA("구멍 사이를 오감 · 기동", "Travel between holes · Mobility", "穴の間を行き来 · 機動", "在坑之间穿行 · 机动");
        PA("땅굴꾼", "Tunneler", "坑道掘り", "掘地道的人");
        PA("내 구멍 위로 걸어 들어가면 땅속으로 들어가(0.25초 무적) 가장 최근에 판 다른 구멍에서 튀어나와, 둘레 2칸 적에게 공격력 100%를 주고 밀칩니다. (2초에 한 번)",
           "Step into your hole to pop out of the latest other one (0.25s invulnerable), hitting foes within 2 tiles for 100% Attack and knocking them back. (Once per 2s)",
           "自分の穴に歩いて入ると地中に潜り（0.25秒無敵）、最後に掘った別の穴から飛び出して、周囲2マスの敵に攻撃力100%を与えて押しのけます。（2秒に1回）",
           "走进自己的坑会钻入地下（0.25秒无敌），从最近挖的另一个坑里冒出，对周围2格内的敌人造成攻击力100%的伤害并将其推开。（每2秒一次）");
        PA("최대 구멍이 1개 줄고, 받는 피해가 10% 늘어납니다.", "One fewer hole at most, and you take 10% more damage.", "穴の最大数が1つ減り、受けるダメージが10%増えます。", "最大坑数减少1个，受到的伤害提高10%。");
        PA("기동 · 탈출 · 생존", "Mobility · Escape · Survival", "機動 · 脱出 · 生存", "机动 · 脱身 · 生存");
        PA("무리 처리", "Clearing crowds", "群れの処理", "清理敌群");
        PA("튀어나온 구멍이 곧바로 가장 크게 넓어집니다.", "The hole you pop out of instantly widens to its largest size.", "飛び出した穴がすぐに最大まで広がります。", "冒出的那个坑会立刻扩到最大。");
        PA("들어간 구멍에서 나온 구멍까지 직선 위의 적이 흙에 걸려 1초 동안 50% 느려지고 공격력 60%를 받습니다. (보스는 피해만)",
           "Foes on the straight line from the hole you entered to the hole you left catch in the soil: they are 50% slower for 1s and take 60% Attack. (Bosses take only the damage)",
           "入った穴から出た穴までの直線上の敵が土に引っかかり、1秒間50%遅くなり攻撃力60%を受けます。（ボスはダメージのみ）",
           "从进入的坑到冒出的坑之间直线上的敌人会陷进泥土，1秒内减速50%并受到攻击力60%的伤害。（首领只受伤害）");
        PA("땅굴 쉬는 시간이 2초에서 0.8초로 줄어듭니다.", "The tunnel cooldown drops from 2s to 0.8s.", "トンネルの待ち時間が2秒から0.8秒に減ります。", "地道冷却时间从2秒缩短到0.8秒。");

        // ---------------- 장례식
        PA("장례식", "Funeral Rites", "葬式", "葬礼仪式");
        PA("필살기가 주인공 · 섬멸", "The ultimate takes center stage · Annihilation", "必殺技が主役 · 殲滅", "必杀技是主角 · 歼灭");
        PA("상주", "Chief Mourner", "喪主", "丧主");
        PA("필살기 게이지 충전이 40% 빨라지고, 장례 범위가 화면 + 4칸으로 넓어지며, 매장으로 묻은 적 하나당 게이지가 0.5% 찹니다.",
           "Ultimate gauge charges 40% faster, the Funeral reaches the screen plus 4 tiles, and each foe you bury adds 0.5% to the gauge.",
           "必殺技ゲージの充填が40%速くなり、葬儀の範囲が画面＋4マスに広がり、埋葬した敵1体につきゲージが0.5%溜まります。",
           "必杀槽充能速度提高40%，葬礼范围扩大到屏幕外4格，每掩埋一个敌人，必杀槽增加0.5%。");
        PA("구멍 수명이 6초에서 3초로 줄어듭니다.", "Hole lifespan drops from 6s to 3s.", "穴の寿命が6秒から3秒に減ります。", "坑的寿命从6秒缩短到3秒。");
        PA("필살기 섬멸 · 보스 순간", "Ultimate annihilation · Boss burst", "必殺技の殲滅 · ボスへの瞬間火力", "必杀歼灭 · 首领瞬间爆发");
        PA("게이지 사이 공백", "Gaps between gauges", "ゲージの合間の空白", "两次必杀之间的空档");
        PA("장례 뒤 6초 동안 구멍이 두 배 빨리 넓어지고 매장 뒤 쉼이 없어집니다.",
           "For 6s after a Funeral, holes widen twice as fast and there is no rest after burying.",
           "葬儀の後6秒間、穴が2倍の速さで広がり、埋葬後の休止がなくなります。",
           "葬礼后6秒内，坑扩大速度翻倍，掩埋后没有停顿。");
        PA("장례 임시 묘비가 무너질 때 둘레 2칸 적(보스 포함)에게 공격력 80%를 줍니다.",
           "When a temporary Funeral gravestone collapses, it deals 80% Attack to foes within 2 tiles (bosses included).",
           "葬儀の仮の墓石が崩れるとき、周囲2マスの敵（ボス含む）に攻撃力80%を与えます。",
           "葬礼的临时墓碑崩塌时，对周围2格内的敌人（含首领）造成攻击力80%的伤害。");
        PA("장례가 1.5초 뒤 한 번 더 일어납니다. (피해 50% · 묘비 없음)",
           "The Funeral happens once more 1.5s later. (50% damage · no gravestones)",
           "葬儀が1.5秒後にもう一度起こります。（ダメージ50% · 墓石なし）",
           "葬礼会在1.5秒后再来一次。（伤害50% · 不留墓碑）");

        // ---------------- 도굴꾼
        PA("도굴꾼", "Grave Robber", "墓荒らし", "盗墓贼");
        PA("묘비를 터뜨림 · 보스 순간 화력", "Blow up gravestones · Boss burst damage", "墓石を爆破 · ボスへの瞬間火力", "炸毁墓碑 · 首领瞬间输出");
        PA("도굴", "Grave Robbing", "墓荒らし", "盗墓");
        PA("묘비 위에 구멍을 파면 묘비가 무너지며 둘레 2칸 적에게 공격력 150%를 주고(되파기 카드와 더해집니다), 그 구멍은 곧바로 가장 크게 열립니다.",
           "Digging a hole on a gravestone collapses it, dealing 150% Attack to foes within 2 tiles (stacks with the Redig card), and that hole opens at its largest size at once.",
           "墓石の上に穴を掘ると墓石が崩れ、周囲2マスの敵に攻撃力150%を与え（掘り返しカードと加算）、その穴はすぐに最大の大きさで開きます。",
           "在墓碑上挖坑会使墓碑崩塌，对周围2格内的敌人造成攻击力150%的伤害（与回挖卡片叠加），那个坑会立刻开到最大。");
        PA("묘비 수명이 20초에서 8초로 줄어듭니다.", "Gravestone lifespan drops from 20s to 8s.", "墓石の寿命が20秒から8秒に減ります。", "墓碑寿命从20秒缩短到8秒。");
        PA("보스 순간 화력 · 코인", "Boss burst damage · Coins", "ボスへの瞬間火力 · コイン", "首领瞬间输出 · 金币");
        PA("생존 (벽이 오래 못 감)", "Survival (walls do not last)", "生存（壁が長持ちしない）", "生存（墙撑不久）");
        PA("무너진 묘비 파편 셋이 가까운 적에게 날아갑니다. (공격력 40%씩)",
           "Three shards of the collapsed gravestone fly at the nearest foes. (40% Attack each)",
           "崩れた墓石の破片3つが近くの敵に飛んでいきます。（攻撃力40%ずつ）",
           "崩塌墓碑的三块碎片飞向最近的敌人。（每块攻击力40%）");
        PA("보스 4칸 안의 묘비가 무너지면 그 피해가 50% 늘어납니다.",
           "When a gravestone within 4 tiles of a boss collapses, its damage rises by 50%.",
           "ボスから4マス以内の墓石が崩れると、そのダメージが50%増えます。",
           "首领4格内的墓碑崩塌时，其伤害提高50%。");
        PA("무너짐이 2칸 안 다른 묘비로 이어집니다. (최대 4개)",
           "The collapse chains to other gravestones within 2 tiles. (Up to 4)",
           "崩落が2マス以内の別の墓石へ連鎖します。（最大4個）",
           "崩塌会连锁到2格内的其他墓碑。（最多4块）");

        // ---------------- 새 교차 둘
        PA("합동 장례", "Joint Funeral", "合同葬", "合葬");
        PA("장례로 떨어진 적의 일제 매장이 1초가 아닌 2초 뒤로 늦춰지는 대신 매장 피해가 25% 늘고, 그사이 지나가던 다른 적도 같은 구멍에 빠집니다.",
           "The mass burial of foes dropped by the Funeral comes 2s later instead of 1s, but burial damage rises by 25%, and other foes passing by in the meantime fall into the same holes.",
           "葬儀で落ちた敵の一斉埋葬が1秒後ではなく2秒後に遅れる代わりに、埋葬ダメージが25%増え、その間に通りかかった他の敵も同じ穴に落ちます。",
           "葬礼落下敌人的集体掩埋从1秒后推迟到2秒后，但掩埋伤害提高25%，这期间路过的其他敌人也会掉进同一个坑。");
        PA("저승길", "Road to the Underworld", "冥途", "黄泉路");
        PA("장례 뒤 4초 동안 땅굴이 화면 안 아무 묘비나 구멍에서(커서에 가장 가까운 곳) 튀어나오고 쉬는 시간이 없어집니다.",
           "For 4s after a Funeral, the tunnel can pop out of any gravestone or hole on screen (the one closest to the cursor) and has no cooldown.",
           "葬儀の後4秒間、トンネルが画面内のどの墓石や穴からでも（カーソルに最も近い所）飛び出し、待ち時間がなくなります。",
           "葬礼后4秒内，地道可以从屏幕内任意墓碑或坑（离光标最近的一个）冒出，且没有冷却时间。");

        // ---------------- 피해 출처 이름 (결과 화면 · 판 기록)
        PA("묘비 무너짐", "Gravestone Collapse", "墓石の崩落", "墓碑崩塌");
        PA("땅굴", "Tunnel", "トンネル", "地道");
    }
}
