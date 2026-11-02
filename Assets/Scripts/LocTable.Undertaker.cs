using UnityEngine;

// 번역표 (2.0.0): 새 영웅 장의사 — 이름 · 설명 · 카드 · 고유 스킬 · 진화 · 무기 · 강화 · 패시브 · 이야기 · 전령 · 기억 · 계약 · 보드
// 키 앞글자 y. · ye. (hero-undertaker.md) — 이미 있는 키는 건드리지 않음 (영웅 키 under)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddUndertakerLines()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 정체 · 글
        A("장의사",
          "Undertaker",
          "葬儀屋",
          "殡葬师");
        A("석관을 묻은 자",
          "The One Who Buried the Sarcophagus",
          "石棺を埋めた者",
          "掩埋石棺之人");
        A("이야기 모드 묘역의 석관을 묻은 장의사. 커서 자리에 구멍을 파 지나가는 적을 빠뜨리고, 한꺼번에 덮어 묻으면 쓰러진 자리마다 묘비가 솟아 적의 길과 탄을 막습니다.",
          "The undertaker who buried the sarcophagus in the Story-mode graveyard. He digs holes at the cursor to trip up passing enemies, and when he fills them all in at once, a gravestone rises wherever enemies fall, blocking enemy paths and shots.",
          "ストーリーモードの墓地で石棺を埋めた葬儀屋。カーソルの位置に穴を掘って通りかかる敵を落とし、まとめて埋めると倒れた場所に墓石が立ち、敵の進路と弾をふさぎます。",
          "故事模式墓地中掩埋石棺的殡葬师。他在光标处挖坑，让路过的敌人掉进去；一并填埋后，敌人倒下的地方会立起墓碑，挡住敌人的去路和弹幕。");
        A("삽",
          "Shovel",
          "シャベル",
          "铁锹");
        A("빈 땅을 누르고 있으면 마우스 자리에 구멍을 파 넓히고, 떼면 그 크기로 완성됩니다. 구멍을 지나가던 적은 빠져 잠깐 갇힙니다. 판 구멍 위를 누르면 매장: 모든 구멍을 한꺼번에 덮어 안의 적을 묻고, 묻혀 쓰러진 자리에는 묘비가 솟아 적의 길과 탄을 막습니다. (구멍은 3개까지 · 장의사는 자기 묘비를 지나다님)",
          "Hold on bare ground to dig a hole at the mouse and widen it; release to finish it at that size. Enemies walking over the hole fall in and are trapped for a moment. Press on a dug hole to Bury: all holes are filled in at once, burying the enemies inside, and a gravestone rises where buried enemies fall, blocking enemy paths and shots. (up to 3 holes · the Undertaker can walk through his own gravestones)",
          "何もない地面を押し続けるとマウスの位置に穴を掘って広げ、離すとその大きさで完成します。穴を通りかかった敵は落ちてしばらく閉じ込められます。掘った穴の上を押すと埋葬：すべての穴を一度に埋めて中の敵を埋め、埋められて倒れた場所には墓石が立って敵の進路と弾をふさぎます。（穴は3つまで・葬儀屋は自分の墓石を通り抜けられます）",
          "在空地上按住会在鼠标处挖坑并逐渐扩大，松开则按当前大小完成。经过坑的敌人会掉进去，被短暂困住。按下已挖好的坑则执行掩埋：一次填埋所有坑，埋住里面的敌人，被埋而倒下的地方会立起墓碑，挡住敌人的去路和弹幕。（最多3个坑 · 殡葬师可以穿过自己的墓碑）");
        A("장례",
          "Funeral",
          "葬儀",
          "葬礼");
        A("누르고 있으면 종이 흔들리고, 떼면 종이 울려 화면 안 모든 적의 발밑이 열려 떨어집니다. 1초 뒤 한꺼번에 묻혀 그 자리에 잠깐 묘비가 솟습니다",
          "Hold to make the bell sway; release and it tolls, opening the ground under every enemy on screen so they fall in. One second later they are all buried at once, and gravestones briefly rise on those spots.",
          "押し続けると鐘が揺れ、離すと鐘が鳴って画面内のすべての敵の足元が開いて落ちます。1秒後にまとめて埋められ、その場所にしばらく墓石が立ちます。",
          "按住时钟会摇晃，松开则钟声响起，屏幕内所有敌人脚下的地面裂开，把它们摔进坑里。1秒后一并掩埋，原地会短暂立起墓碑。");
        A("삽질 공격력",
          "Digging Damage",
          "シャベル攻撃力",
          "挖掘攻击力");
        A("구멍 — 지금 판 구멍 수 / 최대입니다. 구멍이 없으면 묘비 — 서 있는 묘비 수 / 최대를 보여 줍니다. 판 구멍을 다시 누르면 덮습니다.",
          "Holes — the number of holes dug / the maximum. When there are no holes, it shows Gravestones — the number standing / the maximum. Press a dug hole again to fill it in.",
          "穴 — 今掘ってある穴の数／最大数です。穴がないときは墓石 — 立っている墓石の数／最大数を表示します。掘った穴をもう一度押すと埋めます。",
          "坑——当前已挖的坑数 / 上限。没有坑时显示墓碑——立着的墓碑数 / 上限。再次按下已挖的坑即可填埋。");
        A("구멍",
          "Holes",
          "穴",
          "坑");
        A("묘비",
          "Gravestones",
          "墓石",
          "墓碑");
        A("번",
          "×",
          "回",
          "次");
        K("sec.hint.12", "사막에 네 이름이 새겨진 돌이 있다. 거기서 끝나면, 시작된다.",
          "There is a stone in the desert with your name carved on it. If it ends there, it begins.",
          "砂漠に、お前の名が刻まれた石がある。そこで終われば、始まる。",
          "沙漠里有一块刻着你名字的石头。若在那里结束，便会开始。");

        // ================================================================= 전용 레벨업 카드 6
        A("깊은 구덩이",
          "Deep Pit",
          "深い穴",
          "深坑");
        A("빠진 적이 더 오래 갇힙니다.",
          "Enemies that fall in stay trapped longer.",
          "落ちた敵がより長く閉じ込められます。",
          "掉进坑里的敌人被困的时间更长。");
        A("갇힘 시간",
          "Trap time",
          "閉じ込め時間",
          "困住时间");
        A("여러 삽",
          "Many Shovels",
          "たくさんのシャベル",
          "众多铁锹");
        A("동시에 둘 수 있는 구멍이 늘어납니다.",
          "You can keep more holes open at once.",
          "同時に置ける穴が増えます。",
          "可同时存在的坑增加。");
        A("최대 구멍",
          "Max holes",
          "最大の穴の数",
          "最大坑数");
        A("무너지는 흙",
          "Crumbling Soil",
          "崩れる土",
          "塌陷的土");
        A("구멍 가장자리가 무너져 둘레의 적을 구멍 쪽으로 끌어당깁니다. (보스 제외)",
          "The hole's edge crumbles and pulls surrounding enemies toward it. (bosses excluded)",
          "穴のふちが崩れて、周囲の敵を穴の方へ引き寄せます。（ボス除く）",
          "坑沿塌陷，把周围的敌人拉向坑里。（首领除外）");
        A("끌어당기는 범위",
          "Pull range",
          "引き寄せる範囲",
          "拉拽范围");
        A("종지기",
          "Bellringer",
          "鐘守",
          "敲钟人");
        A("장례로 솟은 묘비마다 종이 한 번 더 울려 둘레 2.5칸 적에게 피해를 줍니다. (공격력 60% · 0.6초 간격)",
          "Each gravestone raised by the Funeral tolls a bell once more, damaging enemies within 2.5 tiles. (60% attack · every 0.6s)",
          "葬儀で立った墓石ごとに鐘がもう一度鳴り、周囲2.5マスの敵にダメージを与えます。（攻撃力60%・0.6秒間隔）",
          "葬礼立起的每块墓碑都会再敲一次钟，对周围2.5格内的敌人造成伤害。（攻击力60% · 间隔0.6秒）");
        A("울리는 횟수",
          "Number of tolls",
          "鳴る回数",
          "敲钟次数");
        A("비문",
          "Epitaph",
          "碑文",
          "碑文");
        A("묘비 둘레 2칸 안의 적이 받는 피해가 늘어납니다.",
          "Enemies within 2 tiles of a gravestone take more damage.",
          "墓石の周囲2マス以内の敵が受けるダメージが増えます。",
          "墓碑周围2格内的敌人受到的伤害增加。");
        A("받는 피해",
          "Damage taken",
          "受けるダメージ",
          "受到的伤害");
        A("되파기",
          "Re-dig",
          "掘り返し",
          "回挖");
        A("묘비 위에 구멍을 파면 묘비가 무너지며 둘레 2칸 적에게 피해를 줍니다.",
          "Digging a hole on a gravestone collapses it and damages enemies within 2 tiles.",
          "墓石の上に穴を掘ると墓石が崩れ、周囲2マスの敵にダメージを与えます。",
          "在墓碑上挖坑会使墓碑崩塌，对周围2格内的敌人造成伤害。");
        A("무너짐 피해",
          "Collapse damage",
          "崩壊ダメージ",
          "崩塌伤害");

        // ================================================================= 고유 스킬 10
        A("관 뚜껑",
          "Coffin Lid",
          "棺のふた",
          "棺盖");
        A("한 구멍에 세 마리 넘게 묻으면 관 뚜껑이 닫히는 충격이 둘레로 퍼집니다. (반지름 = 구멍 + 1.5칸)",
          "Burying more than three enemies in one hole sends out the shock of a coffin lid slamming shut. (radius = hole + 1.5 tiles)",
          "1つの穴に3体を超えて埋めると、棺のふたが閉まる衝撃が周囲に広がります。（半径＝穴＋1.5マス）",
          "一个坑里埋下三个以上敌人时，会向四周扩散棺盖合上的冲击。（半径 = 坑 + 1.5格）");
        A("충격 피해",
          "Shock damage",
          "衝撃ダメージ",
          "冲击伤害");
        A("도랑 파기",
          "Trench Digging",
          "溝掘り",
          "挖沟");
        A("구멍을 누르고 있는 동안 구멍에서 커서 쪽으로 도랑이 이어집니다. 도랑 위의 적은 40% 느려집니다. (빠지지는 않음 · 구멍과 함께 메워짐)",
          "While you hold on a hole, a trench extends from it toward the cursor. Enemies on the trench are slowed by 40%. (they do not fall in · it fills in along with the hole)",
          "穴を押し続けている間、穴からカーソルの方へ溝が伸びます。溝の上の敵は40%遅くなります。（落ちはしません・穴と一緒に埋まります）",
          "按住坑期间，会从坑向光标方向延伸出一条沟。沟上的敌人减速40%。（不会掉落 · 随坑一同填平）");
        A("도랑 길이",
          "Trench length",
          "溝の長さ",
          "沟的长度");
        A("유령 손",
          "Ghost Hand",
          "幽霊の手",
          "幽灵之手");
        A("묘비에서 1초마다 손이 뻗어 3칸 안 가장 가까운 적의 발목을 잡습니다. (보스 제외)",
          "Every second a hand reaches out from a gravestone and grabs the ankle of the nearest enemy within 3 tiles. (bosses excluded)",
          "墓石から1秒ごとに手が伸び、3マス以内で最も近い敵の足首をつかみます。（ボス除く）",
          "每隔1秒，墓碑中会伸出一只手，抓住3格内最近敌人的脚踝。（首领除外）");
        A("묶는 시간",
          "Bind time",
          "拘束時間",
          "束缚时间");
        A("흙담",
          "Earthen Wall",
          "土塀",
          "土墙");
        A("3칸 안에 이웃한 묘비끼리 흙담으로 이어져 그 사이도 막습니다.",
          "Gravestones within 3 tiles of each other are joined by earthen walls, blocking the gap between them too.",
          "3マス以内に隣り合う墓石同士が土塀でつながり、その間もふさぎます。",
          "相邻3格内的墓碑之间会连起土墙，把中间也挡住。");
        A("이어지는 담 수",
          "Walls linked",
          "つながる塀の数",
          "连接的墙数");
        A("운구",
          "Pallbearing",
          "運び棺",
          "抬棺");
        A("매장할 때 가장 오래된 묘비 하나가 관이 되어 커서 쪽으로 8칸 미끄러지며 부딪힌 적을 모두 칩니다.",
          "When you Bury, the oldest gravestone turns into a coffin and slides 8 tiles toward the cursor, hitting every enemy it bumps into.",
          "埋葬するとき、最も古い墓石が棺になってカーソルの方へ8マス滑り、ぶつかった敵をすべて打ちます。",
          "掩埋时，最旧的一块墓碑会化作棺材，向光标方向滑行8格，撞击沿途所有敌人。");
        A("관 피해",
          "Coffin damage",
          "棺のダメージ",
          "棺材伤害");
        A("추모",
          "Mourning",
          "追悼",
          "追悼");
        A("묘비 1.5칸 안에 서 있으면 받는 피해가 줄어듭니다.",
          "Standing within 1.5 tiles of a gravestone reduces damage taken.",
          "墓石の1.5マス以内に立っていると、受けるダメージが減ります。",
          "站在墓碑1.5格内时，受到的伤害降低。");
        A("빈 무덤",
          "Empty Grave",
          "空の墓",
          "空坟");
        A("아무도 빠지지 않은 구멍을 매장하면 흙을 덮은 함정이 남아, 처음 밟은 적이 빠집니다. (함정 10초)",
          "Burying a hole nobody fell into leaves a soil-covered trap, and the first enemy to step on it falls in. (trap lasts 10s)",
          "誰も落ちていない穴を埋めると、土をかぶせた罠が残り、最初に踏んだ敵が落ちます。（罠は10秒）",
          "掩埋没有任何敌人掉入的坑时，会留下一个覆土陷阱，第一个踩上去的敌人会掉进去。（陷阱持续10秒）");
        A("동시 함정 수",
          "Traps at once",
          "同時に置ける罠の数",
          "同时陷阱数");
        A("부고",
          "Obituary",
          "訃報",
          "讣告");
        A("매장으로 묻은 적 하나마다 필살기 게이지가 찹니다. (한 번에 최대 8%)",
          "Each enemy you bury charges the ultimate gauge. (up to 8% at a time)",
          "埋葬で埋めた敵1体ごとに必殺技ゲージが溜まります。（1回で最大8%）",
          "每埋下一个敌人，必杀技槽就会充能。（单次最多8%）");
        A("적 하나당",
          "Per enemy",
          "敵1体あたり",
          "每个敌人");
        A("명부",
          "Register",
          "名簿",
          "名册");
        A("매장으로 쓰러뜨린 적 수만큼 다음 장례의 피해가 늘어납니다. (최대 25명 · 장례 때 비움)",
          "The next Funeral's damage grows with the number of enemies you defeated by burying. (max 25 · reset at each Funeral)",
          "埋葬で倒した敵の数だけ、次の葬儀のダメージが増えます。（最大25体・葬儀のときにリセット）",
          "下次葬礼的伤害会随掩埋击败的敌人数量增加。（最多25个 · 葬礼时清空）");
        A("하나당",
          "Per one",
          "1体あたり",
          "每个");
        A("산 자의 무덤",
          "Tomb of the Living",
          "生者の墓",
          "生者之墓");
        A("체력이 30% 아래로 떨어지는 순간 발밑에 스스로 들어가 잠깐 무적이 되고, 나오며 둘레 2칸 적을 밀칩니다. (60초에 한 번)",
          "The moment your health drops below 30%, you climb into the ground beneath you and become briefly invincible, then shove back enemies within 2 tiles as you emerge. (once per 60s)",
          "体力が30%を下回った瞬間、足元に自ら入り込んでしばらく無敵になり、出てくるときに周囲2マスの敵を押し返します。（60秒に1回）",
          "生命值降到30%以下的瞬间，你会自己钻进脚下的地里短暂无敌，钻出时将周围2格内的敌人推开。（每60秒一次）");
        A("무적 시간",
          "Invincibility time",
          "無敵時間",
          "无敌时间");

        // ================================================================= 스킬 진화 7
        A("집단 매장",
          "Mass Burial",
          "集団埋葬",
          "集体掩埋");
        A("갇힌 적끼리 짓눌려 0.5초마다 공격력 25%를 받고, 관 뚜껑 충격이 0.4초 뒤 한 번 더 퍼집니다.",
          "Trapped enemies crush one another, taking 25% attack every 0.5s, and the Coffin Lid shock spreads once more 0.4s later.",
          "閉じ込められた敵同士が押しつぶし合い、0.5秒ごとに攻撃力25%を受け、棺のふたの衝撃が0.4秒後にもう一度広がります。",
          "被困的敌人互相挤压，每0.5秒受到攻击力25%的伤害，棺盖冲击会在0.4秒后再扩散一次。");
        A("묘지기의 담",
          "Gravekeeper's Wall",
          "墓守の塀",
          "守墓人之墙");
        A("흙담이 5칸까지 이어지고, 흙담에 닿은 적은 0.5초 묶이며 비문 효과를 받습니다.",
          "Earthen walls link up to 5 segments, and enemies touching a wall are bound for 0.5s and receive the Epitaph effect.",
          "土塀が5つまでつながり、土塀に触れた敵は0.5秒拘束されて碑文の効果を受けます。",
          "土墙最多可连接5段，碰到土墙的敌人会被束缚0.5秒并受到碑文效果。");
        A("지하 묘도",
          "Underground Passage",
          "地下墓道",
          "地下墓道");
        A("도랑 끝에 작은 구멍(반지름 1칸)이 저절로 열리고, 도랑 위의 적도 매장 피해(110%)를 받습니다.",
          "A small hole (radius 1 tile) opens by itself at the end of the trench, and enemies on the trench also take burial damage (110%).",
          "溝の端に小さな穴（半径1マス）が自然に開き、溝の上の敵も埋葬ダメージ（110%）を受けます。",
          "沟的尽头会自行裂开一个小坑（半径1格），沟上的敌人也会受到掩埋伤害（110%）。");
        A("국장",
          "State Funeral",
          "国葬",
          "国葬");
        A("장례 때 종이 세 번 울리고(두 번째 · 세 번째는 둘레 적에게 피해 40%), 장례로 쓰러진 적마다 게이지 1%를 돌려받습니다. (최대 25%)",
          "The Funeral bell tolls three times (the second and third deal 40% damage to surrounding enemies), and each enemy felled by the Funeral refunds 1% gauge. (max 25%)",
          "葬儀のとき鐘が3回鳴り（2回目・3回目は周囲の敵に40%のダメージ）、葬儀で倒した敵1体ごとにゲージ1%が戻ります。（最大25%）",
          "葬礼时钟会敲三次（第二、三次对周围敌人造成40%伤害），每个被葬礼击败的敌人返还1%的槽。（最多25%）");
        A("무덤 털이",
          "Grave Robbing",
          "墓荒らし",
          "盗墓");
        A("되파기로 무너진 묘비가 코인 1개와 관 하나(운구 피해)를 내보내고, 유령 손이 둘씩 뻗습니다.",
          "A gravestone collapsed by Re-dig yields 1 coin and sends out a coffin (Pallbearing damage), and Ghost Hands reach out two at a time.",
          "掘り返しで崩れた墓石がコイン1枚と棺1つ（運び棺のダメージ）を出し、幽霊の手が2本ずつ伸びます。",
          "被回挖崩塌的墓碑会掉出1枚硬币并送出一口棺材（抬棺伤害），幽灵之手每次伸出两只。");
        A("안식처",
          "Haven",
          "安息所",
          "安息之所");
        A("산 자의 무덤에서 나온 자리에 큰 묘비가 솟고, 추모 효과가 두 배가 되며, 쉬는 시간이 60초 → 40초가 됩니다.",
          "A large gravestone rises where you emerge from the Tomb of the Living, the Mourning effect doubles, and the cooldown drops from 60s to 40s.",
          "生者の墓から出てきた場所に大きな墓石が立ち、追悼の効果が2倍になり、待機時間が60秒→40秒になります。",
          "从生者之墓钻出的地方会立起一块大墓碑，追悼效果翻倍，冷却时间从60秒变为40秒。");
        A("개미지옥",
          "Antlion Pit",
          "アリジゴク",
          "蚁狮陷阱");
        A("빈 무덤 함정이 둘레 2칸 적을 끌어당기고, 한 번에 세 마리까지 빠뜨립니다.",
          "Empty Grave traps pull in enemies within 2 tiles and can drop up to three at once.",
          "空の墓の罠が周囲2マスの敵を引き寄せ、一度に3体まで落とします。",
          "空坟陷阱会拉拽周围2格内的敌人，一次最多让三个掉进去。");

        // ================================================================= 특수 능력 (형태 · 패시브) · 궁극기 · 강화
        K("구멍에 갇힌 적을 까마귀가 0.5초마다 쪼습니다. (공격력 30%)\n궁극기: 까마귀 떼", "구멍에 갇힌 적을 까마귀가 0.5초마다 쪼습니다. (공격력 30%)\n궁극기: 까마귀 떼",
          "A crow pecks enemies trapped in holes every 0.5s. (30% attack)\nUltimate: Murder of Crows",
          "穴に閉じ込められた敵を、カラスが0.5秒ごとにつつきます。（攻撃力30%）\n究極技：カラスの群れ",
          "乌鸦每0.5秒啄一次被困在坑里的敌人。（攻击力30%）\n终极技：鸦群");
        A("구멍에 갇힌 적을 까마귀가 0.5초마다 쪼습니다. (공격력 30%)",
          "A crow pecks enemies trapped in holes every 0.5s. (30% attack)",
          "穴に閉じ込められた敵を、カラスが0.5秒ごとにつつきます。（攻撃力30%）",
          "乌鸦每0.5秒啄一次被困在坑里的敌人。（攻击力30%）");
        K("매장할 때마다 작은 종이 울려 매장한 구멍 둘레 3칸 적이 0.4초 멈춥니다. (보스 제외)\n궁극기: 저녁 종", "매장할 때마다 작은 종이 울려 매장한 구멍 둘레 3칸 적이 0.4초 멈춥니다. (보스 제외)\n궁극기: 저녁 종",
          "Each Burial rings a small bell, freezing enemies within 3 tiles of the buried hole for 0.4s. (bosses excluded)\nUltimate: Evening Bell",
          "埋葬するたびに小さな鐘が鳴り、埋めた穴の周囲3マスの敵が0.4秒止まります。（ボス除く）\n究極技：夕べの鐘",
          "每次掩埋都会响起小钟，被掩埋的坑周围3格内的敌人停住0.4秒。（首领除外）\n终极技：暮钟");
        A("매장할 때마다 작은 종이 울려 매장한 구멍 둘레 3칸 적이 0.4초 멈춥니다. (보스 제외)",
          "Each Burial rings a small bell, freezing enemies within 3 tiles of the buried hole for 0.4s. (bosses excluded)",
          "埋葬するたびに小さな鐘が鳴り、埋めた穴の周囲3マスの敵が0.4秒止まります。（ボス除く）",
          "每次掩埋都会响起小钟，被掩埋的坑周围3格内的敌人停住0.4秒。（首领除外）");
        K("묘비 대신 석관이 솟습니다. 석관은 내구가 두 배이고 닿아 있는 적에게 0.5초마다 공격력 25%를 줍니다.\n궁극기: 석관 봉인", "묘비 대신 석관이 솟습니다. 석관은 내구가 두 배이고 닿아 있는 적에게 0.5초마다 공격력 25%를 줍니다.\n궁극기: 석관 봉인",
          "Sarcophagi rise instead of gravestones. A sarcophagus has double durability and deals 25% attack every 0.5s to enemies touching it.\nUltimate: Sarcophagus Seal",
          "墓石の代わりに石棺が立ちます。石棺は耐久が2倍で、触れている敵に0.5秒ごとに攻撃力25%のダメージを与えます。\n究極技：石棺封印",
          "升起的不是墓碑而是石棺。石棺耐久翻倍，并每0.5秒对碰到它的敌人造成攻击力25%的伤害。\n终极技：石棺封印");
        A("묘비 대신 석관이 솟습니다. 석관은 내구가 두 배이고 닿아 있는 적에게 0.5초마다 공격력 25%를 줍니다.",
          "Sarcophagi rise instead of gravestones. A sarcophagus has double durability and deals 25% attack every 0.5s to enemies touching it.",
          "墓石の代わりに石棺が立ちます。石棺は耐久が2倍で、触れている敵に0.5秒ごとに攻撃力25%のダメージを与えます。",
          "升起的不是墓碑而是石棺。石棺耐久翻倍，并每0.5秒对碰到它的敌人造成攻击力25%的伤害。");
        K("이동 속도가 8% 빨라집니다.\n진화: 14%", "이동 속도가 8% 빨라집니다.\n진화: 14%",
          "Movement speed increases by 8%.\nEvolved: 14%",
          "移動速度が8%速くなります。\n進化：14%",
          "移动速度提升8%。\n进化：14%");
        K("매장으로 쓰러뜨린 적이 5% 확률로 코인 1개를 떨어뜨립니다.\n진화: 10%", "매장으로 쓰러뜨린 적이 5% 확률로 코인 1개를 떨어뜨립니다.\n진화: 10%",
          "Enemies defeated by burying have a 5% chance to drop 1 coin.\nEvolved: 10%",
          "埋葬で倒した敵が5%の確率でコインを1枚落とします。\n進化：10%",
          "被掩埋击败的敌人有5%几率掉落1枚硬币。\n进化：10%");
        K("체력이 30% 아래면 구멍이 50% 빨리 넓어집니다.\n진화: 80%", "체력이 30% 아래면 구멍이 50% 빨리 넓어집니다.\n진화: 80%",
          "Below 30% health, holes widen 50% faster.\nEvolved: 80%",
          "体力が30%未満だと、穴が50%速く広がります。\n進化：80%",
          "生命值低于30%时，坑扩大速度提升50%。\n进化：80%");
        K("묘비가 솟을 때 10% 확률로 경험치 구슬이 하나 나옵니다.\n진화: 20%", "묘비가 솟을 때 10% 확률로 경험치 구슬이 하나 나옵니다.\n진화: 20%",
          "When a gravestone rises, there is a 10% chance an experience orb appears.\nEvolved: 20%",
          "墓石が立つとき、10%の確率で経験値の玉が1つ出ます。\n進化：20%",
          "墓碑升起时，有10%几率出现一颗经验球。\n进化：20%");
        A("묘지 까마귀",
          "Graveyard Crow",
          "墓地のカラス",
          "墓地乌鸦");
        A("상여 종",
          "Funeral Bell",
          "葬送の鐘",
          "丧钟");
        A("석관",
          "Sarcophagus",
          "石棺",
          "石棺");
        A("묘지기의 장화",
          "Gravekeeper's Boots",
          "墓守のブーツ",
          "守墓人之靴");
        A("조의금",
          "Condolence Money",
          "香典",
          "奠仪");
        A("밤샘",
          "Night Vigil",
          "通夜",
          "守夜");
        A("부장품",
          "Grave Goods",
          "副葬品",
          "陪葬品");
        A("까마귀 떼",
          "Murder of Crows",
          "カラスの群れ",
          "鸦群");
        A("화면 안 적 열두 마리에게 까마귀가 차례로 내리꽂힙니다. (공격력 120%씩 · 보스는 네 번)",
          "Crows dive one after another at twelve enemies on screen. (120% attack each · four dives on bosses)",
          "画面内の敵12体に、カラスが次々と急降下します。（攻撃力120%ずつ・ボスには4回）",
          "乌鸦依次俯冲攻击屏幕内的十二个敌人。（每次攻击力120% · 对首领四次）");
        A("저녁 종",
          "Evening Bell",
          "夕べの鐘",
          "暮钟");
        A("0.5초 간격으로 종이 세 번 울려 둘레 12칸 적에게 피해를 주고 0.5초 멈춥니다. (공격력 100%씩)",
          "The bell tolls three times at 0.5s intervals, damaging enemies within 12 tiles and freezing them for 0.5s. (100% attack each)",
          "0.5秒間隔で鐘が3回鳴り、周囲12マスの敵にダメージを与えて0.5秒止めます。（攻撃力100%ずつ）",
          "钟以0.5秒的间隔敲三次，对周围12格内的敌人造成伤害并使其停住0.5秒。（每次攻击力100%）");
        A("석관 봉인",
          "Sarcophagus Seal",
          "石棺封印",
          "石棺封印");
        A("커서 자리에 큰 석관이 떨어져 반지름 3칸을 덮치고, 10초 동안 큰 석관(내구 20)으로 남습니다. (공격력 350%)",
          "A large sarcophagus drops at the cursor, crushing a 3-tile radius, and remains for 10s as a large sarcophagus (durability 20). (350% attack)",
          "カーソルの位置に大きな石棺が落ちて半径3マスを押しつぶし、10秒間大きな石棺（耐久20）として残ります。（攻撃力350%）",
          "光标处落下一口巨大的石棺，砸向半径3格的范围，并作为大石棺（耐久20）留存10秒。（攻击力350%）");
        A("넓은 삽날",
          "Wide Blade",
          "広いシャベル刃",
          "宽铲刃");
        A("구멍이 최대 30% 더 크게 넓어집니다. (2.0칸 → 2.6칸 · 한 구멍에 갇히는 마릿수 상한도 함께 늘어남)",
          "Holes can widen up to 30% larger. (2.0 → 2.6 tiles · the cap on enemies trapped per hole also rises)",
          "穴が最大30%大きく広がります。（2.0マス→2.6マス・1つの穴に閉じ込められる数の上限も一緒に増えます）",
          "坑最大可扩大30%。（2.0格 → 2.6格 · 单个坑可困住的敌人数上限也随之增加）");
        A("두 번 파기",
          "Double Dig",
          "二度掘り",
          "二次挖掘");
        A("구멍을 완성하면 0.4초 뒤 커서 쪽 3칸 앞에 절반 크기 구멍이 하나 더 열립니다. (구멍 수에 셈)",
          "When you finish a hole, a half-size hole opens 3 tiles ahead toward the cursor 0.4s later. (counts toward the hole limit)",
          "穴を完成させると、0.4秒後にカーソル方向3マス先に半分の大きさの穴がもう1つ開きます。（穴の数に数えます）",
          "完成一个坑后0.4秒，光标方向3格外会再裂开一个半大小的坑。（计入坑数）");
        A("무거운 흙",
          "Heavy Soil",
          "重い土",
          "沉重的土");
        A("매장 피해가 25% 늘어납니다.",
          "Burial damage increases by 25%.",
          "埋葬のダメージが25%増えます。",
          "掩埋伤害提升25%。");

        // ================================================================= 이야기
        A("초원 끝 낡은 묘지에서, 삽을 든 검은 상복의 사내가 허리를 폈다.",
          "In an old graveyard at the edge of the meadow, a man in black mourning clothes with a shovel straightened his back.",
          "草原の端の古い墓地で、シャベルを持った黒い喪服の男が腰を伸ばした。",
          "草原尽头的旧墓地里，一个身穿黑色丧服、手持铁锹的男人直起了腰。");
        A("“구하는 게 아니라 제대로 묻어 주는 거지.”",
          "\"I'm not here to save them. I'm here to bury them properly.\"",
          "「救うんじゃない。きちんと埋めてやるだけさ。」",
          "“我不是来拯救的，我是来好好安葬的。”");
        A("장의사는 삽을 어깨에 걸치고 슬라임이 지나간 자리로 걸어갔다. 파야 할 구멍이 많았다.",
          "The undertaker slung his shovel over his shoulder and walked toward where the slime had passed. There were many holes to dig.",
          "葬儀屋はシャベルを肩にかけ、スライムが通った跡へ歩いていった。掘るべき穴はたくさんあった。",
          "殡葬师把铁锹扛在肩上，朝史莱姆经过的地方走去。要挖的坑太多了。");
        A("묘역 깊은 곳의 석관은 장의사가 직접 묻은 것이었다. 뚜껑이 열려 있었다.",
          "The sarcophagus deep in the graveyard was one the undertaker had buried himself. Its lid was open.",
          "墓地の奥の石棺は、葬儀屋が自ら埋めたものだった。ふたが開いていた。",
          "墓地深处的石棺是殡葬师亲手埋下的。棺盖已经开了。");
        A("장의사는 삽을 어깨에 걸치고 신전의 구멍으로 내려갔다. 다시 묻을 것이 많았다.",
          "The undertaker slung his shovel over his shoulder and descended through the hole in the temple. There was much to bury again.",
          "葬儀屋はシャベルを肩にかけ、神殿の穴へ下りていった。また埋めるものがたくさんあった。",
          "殡葬师把铁锹扛在肩上，顺着神殿的洞口走了下去。又有许多东西要埋了。");
        A("“당신 자리는 아직 비워 두었소. 이번엔 뚜껑을 못 박겠소.”",
          "\"I've kept your place empty. This time I'll nail the lid shut.\"",
          "「あなたの場所はまだ空けてあります。今度こそ、ふたに釘を打ちましょう。」",
          "“您的位子我还给您留着。这回，我要把棺盖钉死。”");
        A("묘비마다 이름을 새기는 데 사흘이 걸렸다. 이름 모를 자리에는 꽃을 두었다.",
          "Carving a name on every gravestone took three days. Where the name was unknown, he left flowers.",
          "墓石ごとに名前を刻むのに三日かかった。名の分からない場所には花を供えた。",
          "给每块墓碑刻上名字用了三天。不知名字的地方，就放上了花。");
        A("장의사는 마지막 흙을 다지고 삽을 땅에 꽂았다. 묘역은 다시 조용해졌다.",
          "The undertaker packed down the last of the soil and drove his shovel into the ground. The graveyard fell quiet again.",
          "葬儀屋は最後の土を固めてシャベルを地面に突き立てた。墓地は再び静かになった。",
          "殡葬师夯实最后一捧土，把铁锹插进地里。墓地再次安静下来。");
        A("“이번엔 오래 쉬시오.”",
          "\"Rest for a long time, this time.\"",
          "「今度は、ゆっくりお休みなさい。」",
          "“这回，请好好长眠。”");
        A("하지만 붉은 사막 한가운데, 아직 아무도 묻히지 않은 회색 비석 하나가 서 있었다...",
          "But in the middle of the red desert stood a single gray gravestone, one where no one had yet been buried...",
          "しかし赤い砂漠の真ん中に、まだ誰も埋められていない灰色の石碑が一つ立っていた...",
          "可是在红色沙漠的正中央，还立着一块尚无人埋葬的灰色石碑……");
        A("장례 일정이 잡혔습니다.",
          "A funeral has been scheduled.",
          "葬儀の日程が決まりました。",
          "葬礼日程已经排定。");

        // ================================================================= 전령 대사 6줄
        K("herald.hero.under.1", "묘지기가 쓰러졌다.\n“묻는 사람이 묻히면 누가 덮어 주나… 아니, 내가 일어나지.”",
          "The gravekeeper has fallen.\n\"If the one who buries gets buried, who covers him? ...No, I'll get up myself.\"",
          "墓守が倒れた。\n「埋める者が埋められたら、誰が土をかけてくれる？……いや、自分で起き上がるさ。」",
          "守墓人倒下了。\n“埋人的人要是被埋了，谁来给他盖土呢……不，我自己会爬起来。”");
        K("herald.hero.under.2", "삽이 손에서 미끄러졌다.\n“구덩이는 늘 한 뼘씩 모자라더군.”",
          "The shovel slipped from his hand.\n\"A grave always comes up a hand's breadth short.\"",
          "シャベルが手から滑り落ちた。\n「穴はいつも、ひと握り足りないんだよな。」",
          "铁锹从手里滑落了。\n“坑总是差那么一拃深。”");
        K("herald.hero.under.3", "구멍을 너무 늦게 팠다.\n“발밑을 봐야 했는데, 나는 늘 남의 발밑만 보지.”",
          "He dug the hole too late.\n\"I should have watched my own feet, but I only ever watch other people's.\"",
          "穴を掘るのが遅すぎた。\n「足元を見るべきだったのに、私はいつも他人の足元ばかり見ている。」",
          "坑挖得太晚了。\n“我本该看看自己脚下，可我总是只盯着别人的脚下。”");
        K("herald.hero.under.4", "묘비가 길을 막았다. 제 길까지.\n“장의사는 제 무덤 자리를 고르지 않는 법이다.”",
          "The gravestones blocked the way — even his own.\n\"An undertaker never picks out his own grave.\"",
          "墓石が道をふさいだ。自分の道まで。\n「葬儀屋は、自分の墓の場所を選ばないものだ。」",
          "墓碑挡住了路，连他自己的路也挡住了。\n“殡葬师从不给自己挑墓地。”");
        K("herald.hero.under.5", "리치 왕은 석관으로 돌아가지 않았다.\n“뚜껑을 다시 덮으려면 한 번 더 가야겠군.”",
          "The Lich King did not return to his sarcophagus.\n\"Looks like I'll have to go once more to put the lid back on.\"",
          "リッチキングは石棺に戻らなかった。\n「ふたをもう一度閉めるには、もう一度行かねばならんようだ。」",
          "巫妖王没有回到石棺里去。\n“要想重新盖上棺盖，看来还得再去一趟。”");
        K("herald.hero.under.6", "사막의 모래는 묻어도 묻어도 흘러내린다.\n“여긴 무덤 파기엔 형편없는 땅이야.”",
          "The desert sand keeps flowing back no matter how much you bury.\n\"This is terrible ground for digging graves.\"",
          "砂漠の砂は、埋めても埋めても流れ落ちる。\n「ここは墓を掘るにはひどい土地だな。」",
          "沙漠的沙子怎么埋都会流回去。\n“这儿的地，挖坟可真够糟的。”");
        K("bossmem.noun.undertaker", "삽",
          "shovel",
          "シャベル",
          "铁锹");

        // ================================================================= 기억 조각 6
        K("memory.under.1.title", "첫 삽",
          "The First Shovelful",
          "最初のひと掘り",
          "第一锹");
        K("memory.under.1.text", "처음 판 구덩이는 고양이 무덤이었다.\n너무 깊게 파서 아버지가 웃었다.\n“그날부터 얕게 파는 법을 몰랐어요.”",
          "The first hole he ever dug was a cat's grave.\nHe dug it so deep that his father laughed.\n\"Since that day, I never learned how to dig shallow.\"",
          "初めて掘った穴は、猫の墓だった。\nあまりに深く掘ったので、父が笑った。\n「あの日から、浅く掘る方法を知らないんです。」",
          "他挖的第一个坑，是一只猫的坟。\n挖得太深，父亲笑了。\n“从那天起，我就不会挖浅坑了。”");
        K("memory.under.2.title", "다섯 번째 장례",
          "The Fifth Funeral",
          "五度目の葬儀",
          "第五场葬礼");
        K("memory.under.2.text", "다섯 번 쓰러지고 나서야 알았다.\n묻히는 쪽도 순서를 기다린다.\n“줄 서 있는 걸 몰랐네요.”",
          "Only after falling five times did he realize it.\nThose being buried wait their turn too.\n\"I didn't know there was a line.\"",
          "五度倒れて、ようやく分かった。\n埋められる側も、順番を待っているのだと。\n「列に並んでいるなんて、知りませんでした。」",
          "倒下五次之后，他才明白。\n被埋葬的一方，也在等着排队。\n“我竟不知道还要排队。”");
        K("memory.under.3.title", "석관",
          "The Sarcophagus",
          "石棺",
          "石棺");
        K("memory.under.3.text", "리치 왕의 석관을 묻은 날, 뚜껑에 못을 박지 않았다.\n「그래도 왕인데」라고 생각했다.\n“그게 제일 큰 실수였죠.”",
          "On the day he buried the Lich King's sarcophagus, he did not nail the lid shut.\nHe thought, \"He's still a king, after all.\"\n\"That was my biggest mistake.\"",
          "リッチキングの石棺を埋めた日、ふたに釘を打たなかった。\n「それでも王なのだから」と思ったのだ。\n「あれが一番の失敗でしたね。」",
          "埋下巫妖王石棺的那天，他没有在棺盖上钉钉子。\n他想：“毕竟是位君王。”\n“那是我最大的失误。”");
        K("memory.under.4.title", "무거운 흙",
          "Heavy Soil",
          "重い土",
          "沉重的土");
        K("memory.under.4.text", "어려운 날일수록 흙이 무거웠다.\n그래도 덮었다. 덮어야 끝나니까.\n“끝나야 쉴 수 있으니까요.”",
          "The harder the day, the heavier the soil.\nStill, he covered it up. It only ends once it's covered.\n\"Because you can only rest once it's over.\"",
          "つらい日ほど、土は重かった。\nそれでも覆った。覆わなければ終わらないから。\n「終わって初めて、休めますから。」",
          "越是艰难的日子，土就越重。\n可他还是盖上了。盖上了，才算完。\n“完了，才能歇一歇啊。”");
        K("memory.under.5.title", "사막의 비석",
          "The Desert Gravestone",
          "砂漠の石碑",
          "沙漠的石碑");
        K("memory.under.5.text", "사막에 이름 없는 비석이 하나 있다.\n누군가 매번 거기 와서 쓰러진다.\n“누가 오는지는 묻지 않기로 했어요.”",
          "In the desert stands a single nameless gravestone.\nSomeone comes there and falls, again and again.\n\"I decided not to ask who it is.\"",
          "砂漠に名もない石碑が一つある。\n誰かが毎回そこへ来て、倒れる。\n「誰が来るのかは、訊かないことにしました。」",
          "沙漠里有一块无名的石碑。\n总有人每次都来到这里倒下。\n“我决定不去问是谁来了。”");
        K("memory.under.6.title", "장의사의 자리",
          "The Undertaker's Place",
          "葬儀屋の場所",
          "殡葬师的位置");
        K("memory.under.6.text", "묘역 끝 빈자리 하나. 자기 것이다.\n아직은 꽃만 두었다.\n“서두를 일은 아니니까요.”",
          "One empty plot at the far end of the graveyard. It is his own.\nFor now, he has left only flowers.\n\"There's no need to hurry.\"",
          "墓地の端に、空いた区画が一つ。自分の場所だ。\nまだ、花だけを供えてある。\n「急ぐことではありませんから。」",
          "墓地尽头有一块空位，是他自己的。\n眼下只放了些花。\n“这事不用着急。”");

        // ================================================================= 영혼 계약 · 보드
        K("contract.h.under.1.name", "백오십 번의 매장",
          "A Hundred and Fifty Burials",
          "百五十回の埋葬",
          "一百五十次掩埋");
        K("contract.h.under.1.cond", "매장으로 적 150마리 묻기",
          "Bury 150 enemies",
          "埋葬で敵を150体埋める",
          "掩埋150个敌人");
        K("contract.h.under.2.name", "종지기",
          "Bellringer",
          "鐘守",
          "敲钟人");
        K("contract.h.under.2.cond", "필살기 10번 쓰기",
          "Use the ultimate 10 times",
          "必殺技を10回使う",
          "使用必杀技10次");
        K("contract.h.under.3.name", "한 구덩이에 여섯",
          "Six in One Hole",
          "ひとつの穴に六体",
          "一坑六人");
        K("contract.h.under.3.cond", "한 구멍에서 한 번에 6마리 묻기",
          "Bury 6 enemies from a single hole at once",
          "1つの穴から一度に6体埋める",
          "在同一个坑里一次掩埋6个敌人");
        K("contract.h.under.4.name", "묘역 조성",
          "Building the Graveyard",
          "墓地の造成",
          "营造墓园");
        K("contract.h.under.4.cond", "묘비 60개 세우기",
          "Raise 60 gravestones",
          "墓石を60基立てる",
          "立起60块墓碑");
        K("contract.h.under.6.name", "사백 번의 매장",
          "Four Hundred Burials",
          "四百回の埋葬",
          "四百次掩埋");
        K("contract.h.under.6.cond", "매장으로 적 400마리 묻기 (누적)",
          "Bury 400 enemies (total)",
          "埋葬で敵を400体埋める（累計）",
          "掩埋400个敌人（累计）");
        K("board.under.1.name", "천오백 번의 매장",
          "Fifteen Hundred Burials",
          "千五百回の埋葬",
          "一千五百次掩埋");
        K("board.under.1.desc", "파고, 덮고, 또 팠습니다. 삽 자루는 어느새 손 모양으로 닳았습니다.",
          "Dug, covered, and dug again. The shovel handle wore into the shape of a hand.",
          "掘って、覆って、また掘りました。シャベルの柄はいつしか手の形にすり減っていました。",
          "挖了，埋了，又挖。铁锹的柄不知不觉被磨成了手的形状。");
        K("board.under.1.cond", "매장으로 적 1500마리 묻기",
          "Bury 1500 enemies",
          "埋葬で敵を1500体埋める",
          "掩埋1500个敌人");
        K("board.under.2.name", "네 왕을 묻은 삽",
          "The Shovel That Buried Four Kings",
          "四王を埋めたシャベル",
          "埋葬四王的铁锹");
        K("board.under.2.desc", "왕들도 결국 흙 아래로 갔습니다. 뚜껑은 장의사가 덮었습니다.",
          "Even kings went under the soil in the end. The undertaker closed the lid.",
          "王たちも結局は土の下へ行きました。ふたを閉めたのは葬儀屋でした。",
          "君王们终究也都去了土下。盖上棺盖的是殡葬师。");
        K("board.under.2.cond", "장의사로 장 보스 4종 모두 쓰러뜨리기",
          "Defeat all four chapter bosses as the Undertaker",
          "葬儀屋で章のボス4種をすべて倒す",
          "以殡葬师击败全部四种章节首领");
        K("board.under.3.name", "이백 개의 묘비",
          "Two Hundred Gravestones",
          "二百基の墓石",
          "两百块墓碑");
        K("board.under.3.desc", "세운 묘비가 줄지어 서면 하나의 길이 됩니다. 이름은 새기지 못했습니다.",
          "When the gravestones you raised stand in a row, they become a road. There was no time to carve the names.",
          "立てた墓石が並べば、ひとつの道になります。名前を刻む時間はありませんでした。",
          "立起的墓碑排成一列，便成了一条路。名字来不及刻上。");
        K("board.under.3.cond", "묘비 200개 세우기",
          "Raise 200 gravestones",
          "墓石を200基立てる",
          "立起200块墓碑");
    }
}
