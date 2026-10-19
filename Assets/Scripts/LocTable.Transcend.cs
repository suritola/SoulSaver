// 번역표 (1.1~): 판 후반 보상 — 3장 각성 · 초월 카드 (LevelShop.Transcend) · 도감 · 튜토리얼 문구
// LocTable.Skills 의 정적 생성자가 부름 (이미 있는 키는 건드리지 않음)
public static partial class LocTable
{
    static void AddTranscend()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }
        // 키(코드 속 한국어)는 그대로 두고 화면에 보일 한국어만 바꿀 때 (2026-10-16 문구 다듬기)
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // 초월 카드 (1.2~ 「새 규칙」 열한 장, TranscendRules)
        A("초월 카드", "Transcendence Cards", "超越カード", "超越卡");
        A("초월: 새 규칙", "Transcendence: New Rule", "超越：新ルール", "超越：新规则");
        K("판의 규칙 하나를 바꿉니다 · 같은 카드를 다시 고르면 다음 단계",
          "판의 규칙 하나를 바꿉니다. 같은 카드를 다시 고르면 다음 단계로 오릅니다.",
          "Change one rule of this run. Pick the same card again to advance it.",
          "このプレイのルールを1つ変えます。同じカードをもう一度選ぶと次の段階に進みます。",
          "改变本局的一条规则。再次选择同一张卡会进入下一阶段。");
        A("새 규칙까지", "Next rule", "次のルールまで", "距离新规则");
        A("새 규칙!", "New rule!", "新ルール！", "新规则！");
        A("2단계째", "Stage 2", "2段階目", "第2阶段");
        A("3단계째", "Stage 3", "3段階目", "第3阶段");
        A("최대 단계!", "Max stage!", "最大段階！", "最大阶段！");

        A("초월: 메아리", "Transcend: Echo", "超越：残響", "超越：回响");
        A("초월: 영혼 수확", "Transcend: Reaping", "超越：魂の収穫", "超越：灵魂收割");
        A("초월: 시간의 틈", "Transcend: Time Rift", "超越：時の裂け目", "超越：时间裂隙");
        A("초월: 연금 손", "Transcend: Alchemist's Touch", "超越：錬金の手", "超越：炼金之手");
        A("초월: 그림자 분신", "Transcend: Shadow Clones", "超越：影分身", "超越：影分身");
        A("초월: 중력장", "Transcend: Gravity Well", "超越：重力場", "超越：重力场");
        A("초월: 역전", "Transcend: Reversal", "超越：逆転", "超越：逆转");
        A("초월: 맵 지배", "Transcend: Dominion", "超越：地の支配", "超越：地图支配");
        A("초월: 연쇄 붕괴", "Transcend: Cascade", "超越：連鎖崩壊", "超越：连锁崩坏");
        A("초월: 영혼 보호막", "Transcend: Soul Ward", "超越：魂の障壁", "超越：灵魂护盾");
        A("초월: 영혼불 발자국", "Transcend: Soulfire Steps", "超越：魂火の足跡", "超越：魂火足迹");

        // 메아리
        K("필살기를 쓰고 나면 잠시 뒤 그 자리에 영혼 메아리가 울려 둘레 적을 휩쓸고 적 탄을 지웁니다",
          "필살기를 쓰고 나면 잠시 뒤 그 자리에 영혼 메아리가 울려 둘레의 적을 휩쓸고 적 탄을 지웁니다.",
          "Shortly after your Ultimate, a soul echo rings out where you stand, sweeping nearby enemies and erasing enemy shots.",
          "必殺技を使った少し後、その場に魂の残響が響き、周りの敵をなぎ払って敵の弾を消します。",
          "使用必杀技后不久，原地会响起灵魂回响，横扫周围敌人并消除敌方子弹。");
        K("메아리에 휩쓸린 적이 1.5초 동안 얼어붙습니다", "메아리에 휩쓸린 적이 1.5초 동안 얼어붙습니다.", "Enemies caught in the echo freeze for 1.5s.", "残響に巻き込まれた敵が1.5秒間凍りつきます。", "被回响卷入的敌人会冻结1.5秒。");
        K("메아리가 한 번 더, 더 넓게 울립니다", "메아리가 한 번 더, 더 넓게 울립니다.", "The echo rings once more, even wider.", "残響がもう一度、さらに広く響きます。", "回响会再响一次，范围更大。");
        // 영혼 수확
        K("적 150마리를 쓰러뜨릴 때마다 영혼 파동이 둘레 잡몹을 쓸어 버립니다",
          "적 150마리를 쓰러뜨릴 때마다 영혼 파동이 둘레의 일반 적을 쓸어 버립니다.",
          "Every 150 kills, a soul wave sweeps away nearby regular enemies.",
          "敵を150体倒すたびに、魂の波動が周りの通常の敵を一掃します。",
          "每击败150个敌人，灵魂波动就会扫除周围的普通敌人。");
        K("이제 110마리마다 영혼 파동이 일어납니다", "이제 110마리마다 영혼 파동이 일어납니다.", "A soul wave now triggers every 110 kills.", "これからは110体ごとに魂の波動が起こります。", "现在每击败110个敌人触发一次灵魂波动。");
        K("이제 75마리마다 영혼 파동이 일어납니다", "이제 75마리마다 영혼 파동이 일어납니다.", "A soul wave now triggers every 75 kills.", "これからは75体ごとに魂の波動が起こります。", "现在每击败75个敌人触发一次灵魂波动。");
        // 시간의 틈
        K("체력이 40% 아래일 때 맞으면 3초 동안 세상이 거의 멈춥니다 (60초마다)",
          "체력이 40% 아래일 때 맞으면 3초 동안 세상이 거의 멈춥니다. (60초마다)",
          "Get hit below 40% health and the world nearly stops for 3s. (Every 60s)",
          "体力40%未満で攻撃を受けると、3秒間世界がほぼ止まります。（60秒ごと）",
          "生命低于40%时受到攻击，世界几乎静止3秒。（每60秒一次）");
        K("시간의 틈이 4초로 길어지고 40초마다 열립니다", "시간의 틈이 4초로 길어지고 40초마다 열립니다.", "The rift lasts 4s and opens every 40s.", "時の裂け目が4秒に伸び、40秒ごとに開きます。", "时间裂隙延长至4秒，每40秒开启一次。");
        K("시간의 틈 동안 피해를 받지 않습니다", "시간의 틈 동안 피해를 받지 않습니다.", "You take no damage during the rift.", "時の裂け目の間はダメージを受けません。", "时间裂隙期间不会受到伤害。");
        // 연금 손
        K("코인을 주울 때 10% 확률로 경험치로 연성됩니다 (다음 초월 카드가 빨라짐)",
          "코인을 주울 때 10% 확률로 경험치로 연성됩니다. (다음 초월 카드가 빨라짐)",
          "Coins you pick up have a 10% chance to transmute into EXP. (Your next Transcendence card comes sooner)",
          "コインを拾うと10%の確率で経験値に錬成されます。（次の超越カードが早まる）",
          "拾取金币时有10%概率炼成经验。（下一张超越卡来得更快）");
        K("연성 확률이 18%로 오릅니다", "연성 확률이 18%로 오릅니다.", "Transmute chance rises to 18%.", "錬成の確率が18%に上がります。", "炼成概率提升至18%。");
        K("연성 확률이 25%로 오르고, 연성할 때 영혼 조각도 하나 생깁니다", "연성 확률이 25%로 오르고, 연성할 때 영혼 조각도 하나 생깁니다.", "Transmute chance rises to 25%, and each transmutation also gives a Soul Shard.",
          "錬成の確率が25%に上がり、錬成するたびに魂のかけらも1つ手に入ります。", "炼成概率提升至25%，每次炼成还会获得一枚灵魂碎片。");
        // 그림자 분신
        K("그림자 분신 하나가 발자취를 따라오며 가까운 적에게 그림자 화살을 쏩니다",
          "그림자 분신 하나가 발자취를 따라오며 가까운 적에게 그림자 화살을 쏩니다.",
          "A shadow clone follows in your footsteps and fires shadow bolts at nearby enemies.",
          "影分身が1体、足跡をたどってついて来て、近くの敵に影の矢を放ちます。",
          "一个影分身沿着你的足迹跟随，向附近敌人射出暗影箭。");
        K("그림자 분신이 둘이 됩니다", "그림자 분신이 둘이 됩니다.", "You now have two shadow clones.", "影分身が2体になります。", "影分身增加到两个。");
        K("그림자 분신이 셋이 됩니다", "그림자 분신이 셋이 됩니다.", "You now have three shadow clones.", "影分身が3体になります。", "影分身增加到三个。");
        // 중력장
        K("1초 동안 가만히 서 있으면 중력장이 생겨 둘레 잡몹과 코인을 끌어당깁니다",
          "1초 동안 가만히 서 있으면 중력장이 생겨 둘레의 일반 적과 코인을 끌어당깁니다.",
          "Stand still for 1s to form a gravity well that pulls in nearby regular enemies and coins.",
          "1秒間じっと立っていると重力場が生まれ、周りの通常の敵とコインを引き寄せます。",
          "静止1秒后形成重力场，吸引周围的普通敌人和金币。");
        K("중력장이 더 넓어집니다", "중력장이 더 넓어집니다.", "The gravity well grows wider.", "重力場がさらに広がります。", "重力场范围扩大。");
        K("중력장이 더 넓어지고, 끌려온 적이 느려집니다", "중력장이 더 넓어지고, 끌려온 적이 느려집니다.", "The gravity well grows wider and slows the enemies it pulls.", "重力場がさらに広がり、引き寄せた敵が遅くなります。", "重力场范围扩大，被吸引的敌人会减速。");
        // 역전
        K("최후의 보루가 발동하면 그 자리에서 대폭발이 일고 무적이 4초로 늘어납니다",
          "최후의 보루가 발동하면 그 자리에서 대폭발이 일고 무적이 4초로 늘어납니다.",
          "When Last Stand triggers, a huge explosion erupts on the spot and your invincibility lasts 4s.",
          "最後の砦が発動すると、その場で大爆発が起き、無敵が4秒に伸びます。",
          "最后的堡垒触发时，原地引发大爆炸，无敌时间延长至4秒。");
        K("최후의 보루가 장마다 한 번 더 발동합니다", "최후의 보루가 장마다 한 번 더 발동합니다.", "Last Stand triggers one more time per chapter.", "最後の砦が章ごとにもう1回発動します。", "最后的堡垒每章多触发一次。");
        K("역전할 때 체력의 50%를 회복합니다", "역전할 때 체력의 50%를 회복합니다.", "Reversal also restores 50% of your health.", "逆転するとき体力の50%を回復します。", "逆转时恢复50%生命。");
        // 맵 지배
        K("부순 맵 물체가 18초 뒤 다른 자리에 다시 생기고, 부술 때 폭발합니다",
          "부순 맵 물체가 18초 뒤 다른 자리에 다시 생기고, 부술 때 폭발합니다.",
          "Broken map objects reappear elsewhere after 18s, and explode when you break them.",
          "壊したマップの物体が18秒後に別の場所に再び現れ、壊すと爆発します。",
          "被打碎的地图物体会在18秒后于别处重新出现，打碎时还会爆炸。");
        K("맵 물체가 10초 만에 다시 생기고 폭발이 커집니다", "맵 물체가 10초 만에 다시 생기고 폭발이 커집니다.", "Map objects reappear in 10s with bigger explosions.", "マップの物体が10秒で再び現れ、爆発が大きくなります。", "地图物体10秒后重新出现，爆炸范围更大。");
        // 연쇄 붕괴
        K("쓰러진 적이 12% 확률로 터져 둘레 적에게 번집니다 (터진 적도 다시 터짐)",
          "쓰러진 적이 12% 확률로 터져 둘레의 적에게 번집니다. (터진 적도 다시 터짐)",
          "Slain enemies have a 12% chance to burst and spread to those nearby. (Bursts can chain)",
          "倒した敵が12%の確率で破裂し、周りの敵に広がります。（破裂で倒れた敵も再び破裂）",
          "被击败的敌人有12%概率爆裂并波及周围敌人。（爆裂击败的敌人也会再次爆裂）");
        K("연쇄 확률이 18%로 오릅니다", "연쇄 확률이 18%로 오릅니다.", "Chain chance rises to 18%.", "連鎖の確率が18%に上がります。", "连锁概率提升至18%。");
        K("연쇄 확률이 25%로 오르고 폭발이 커집니다", "연쇄 확률이 25%로 오르고 폭발이 커집니다.", "Chain chance rises to 25% and bursts grow bigger.", "連鎖の確率が25%に上がり、爆発が大きくなります。", "连锁概率提升至25%，爆炸范围更大。");
        // 영혼 보호막
        K("8초 동안 맞지 않으면 공격 한 번을 막는 영혼 보호막이 생깁니다",
          "8초 동안 맞지 않으면 공격 한 번을 막는 영혼 보호막이 생깁니다.",
          "Go 8s without being hit to gain a soul ward that blocks one attack.",
          "8秒間攻撃を受けなければ、攻撃を1回防ぐ魂の障壁が生まれます。",
          "8秒内未受攻击，就会生成可抵挡一次攻击的灵魂护盾。");
        K("보호막이 5초 만에 다시 생깁니다", "보호막이 5초 만에 다시 생깁니다.", "The ward returns in 5s.", "障壁が5秒で再び生まれます。", "护盾5秒即可重新生成。");
        K("보호막이 깨질 때 둘레 적을 밀쳐 내고 얼립니다", "보호막이 깨질 때 둘레의 적을 밀어내고 얼립니다.", "When the ward breaks, it pushes back and freezes nearby enemies.", "障壁が割れるとき、周りの敵を押しのけて凍らせます。", "护盾破碎时击退并冻结周围敌人。");
        // 영혼불 발자국
        K("움직이면 지나간 자리에 영혼불이 남아 밟은 적을 태웁니다",
          "움직이면 지나간 자리에 영혼불이 남아 밟은 적을 태웁니다.",
          "As you move, soulfire lingers in your trail and burns enemies who step in it.",
          "移動すると通った跡に魂火が残り、踏んだ敵を燃やします。",
          "移动时走过的地方会留下魂火，灼烧踏入的敌人。");
        K("영혼불이 더 오래 남습니다", "영혼불이 더 오래 남습니다.", "Soulfire lingers longer.", "魂火がより長く残ります。", "魂火持续更久。");
        K("영혼불이 더 넓고 오래 남습니다", "영혼불이 더 넓고 오래 남습니다.", "Soulfire spreads wider and lingers longer.", "魂火がより広く、長く残ります。", "魂火范围更大、持续更久。");

        // 게임 안 짧은 글
        A("메아리!", "Echo!", "残響！", "回响！");
        A("영혼 수확!", "Reaping!", "魂の収穫！", "灵魂收割！");
        A("시간의 틈!", "Time Rift!", "時の裂け目！", "时间裂隙！");
        A("연성!", "Transmute!", "錬成！", "炼成！");
        A("역전!", "Reversal!", "逆転！", "逆转！");
        A("막아 냄!", "Blocked!", "防いだ！", "挡下了！");

        A("최대 레벨에 닿은 뒤에는 경험치 막대를 세 번 채울 때마다 초월 카드로 판의 규칙 하나를 바꿉니다. (레벨은 오르지 않고, 같은 카드를 다시 고르면 다음 단계가 됩니다)",
          "After reaching max level, every third time your EXP bar fills, a Transcendence card changes one rule of the run. (Your level doesn't rise; picking the same card again advances it to the next stage)",
          "最大レベルに達した後は、経験値バーを3回満タンにするたびに超越カードでこのプレイのルールを1つ変えます。（レベルは上がらず、同じカードをもう一度選ぶと次の段階になります）",
          "达到等级上限后，每填满经验条三次，就用超越卡改变本局的一条规则。（等级不会提升，再次选择同一张卡会进入下一阶段）");

        // 도감 · 튜토리얼
        K("레벨이 오를 때 세 장 중 하나를 고르거나 건너뜁니다 · 최대 레벨 50 (그 뒤로는 경험치 막대를 세 번 채울 때마다 초월 카드) · 재료를 모두 올리면 스킬 진화",
          "레벨이 오를 때 세 장 중 하나를 고르거나 건너뜁니다. 최대 레벨은 50이고, 그 뒤로는 경험치 막대를 세 번 채울 때마다 초월 카드를 고릅니다. 재료를 모두 올리면 스킬이 진화합니다.",
          "Pick one of three cards or skip on each level-up. Max level is 50; after that, you pick a Transcendence card every third time the EXP bar fills. Max all ingredients to evolve a skill.",
          "レベルアップごとに3枚から1枚を選ぶか、スキップします。最大レベルは50で、その後は経験値バーを3回満タンにするたびに超越カードを選びます。素材をすべて上げるとスキルが進化します。",
          "每次升级可从三张卡中选一张或跳过。等级上限为50，之后每填满经验条三次可选择一张超越卡。素材全部升满即可进化技能。");
        // 도감 초월 카드 설명 (1.2~ 새 규칙 열한 장 — 키는 CodexUI 의 문장과 글자까지 같아야 함)
        K("최대 레벨(50)에 닿은 뒤 경험치 막대를 세 번 채울 때마다 (레벨은 오르지 않음) 금테 초월 카드 세 장 중 하나를 골라 판의 규칙을 바꿉니다. 같은 카드를 다시 고르면 다음 단계가 됩니다 (최대 3단계 · 맵 지배는 2단계). 메아리: 필살기 뒤 영혼 메아리 · 영혼 수확: 처치 수마다 영혼 파동 · 시간의 틈: 위기에 세상이 멈춤 · 연금 손: 코인이 경험치로 연성 · 그림자 분신: 따라오며 쏘는 분신 · 중력장: 멈춰 서면 끌어당김 · 역전: 최후의 보루에 대폭발 · 맵 지배: 맵 물체가 다시 생김 (이야기 모드) · 연쇄 붕괴: 쓰러진 적이 터져 번짐 · 영혼 보호막: 맞지 않으면 한 번 막음 · 영혼불 발자국: 지나간 자리가 불탐",
          "최대 레벨(50)에 닿은 뒤 경험치 막대를 세 번 채울 때마다 (레벨은 오르지 않음) 금테 초월 카드 세 장 중 하나를 골라 판의 규칙을 바꿉니다. 같은 카드를 다시 고르면 다음 단계가 됩니다. (최대 3단계 · 맵 지배는 2단계) 메아리: 필살기 뒤 영혼 메아리 · 영혼 수확: 처치 수마다 영혼 파동 · 시간의 틈: 위기에 세상이 멈춤 · 연금 손: 코인이 경험치로 연성 · 그림자 분신: 따라오며 쏘는 분신 · 중력장: 멈춰 서면 끌어당김 · 역전: 최후의 보루에 대폭발 · 맵 지배: 맵 물체가 다시 생김 (이야기 모드) · 연쇄 붕괴: 쓰러진 적이 터져 번짐 · 영혼 보호막: 맞지 않으면 한 번 막음 · 영혼불 발자국: 지나간 자리가 불탐",
          "After reaching max level (50), every third time your EXP bar fills (your level doesn't rise) you pick one of three gold-framed Transcendence cards to change a rule of the run. Picking the same card again advances it to the next stage (up to stage 3 · Dominion up to stage 2). Echo: a soul echo after your Ultimate · Reaping: a soul wave every so many kills · Time Rift: the world stops when you're in danger · Alchemist's Touch: coins transmute into EXP · Shadow Clones: clones that follow you and shoot · Gravity Well: stand still to pull things in · Reversal: a huge explosion on Last Stand · Dominion: map objects reappear (Story mode) · Cascade: slain enemies burst and spread · Soul Ward: blocks one hit if you avoid damage · Soulfire Steps: your trail burns",
          "最大レベル（50）に達した後、経験値バーを3回満タンにするたびに（レベルは上がりません）金枠の超越カード3枚から1枚を選んでこのプレイのルールを変えます。同じカードをもう一度選ぶと次の段階になります（最大3段階・地の支配は2段階）。残響：必殺技の後に魂の残響・魂の収穫：撃破数ごとに魂の波動・時の裂け目：危機に世界が止まる・錬金の手：コインが経験値に錬成・影分身：ついて来て撃つ分身・重力場：立ち止まると引き寄せる・逆転：最後の砦で大爆発・地の支配：マップの物体が再び現れる（ストーリーモード）・連鎖崩壊：倒れた敵が破裂して広がる・魂の障壁：被弾しなければ1回防ぐ・魂火の足跡：通った跡が燃える",
          "达到等级上限（50）后，每填满经验条三次（等级不会提升），就从三张金框超越卡中选择一张，改变本局的规则。再次选择同一张卡会进入下一阶段（最多3阶段 · 地图支配最多2阶段）。回响：必杀技后的灵魂回响 · 灵魂收割：每击败一定数量敌人触发灵魂波动 · 时间裂隙：危急时世界静止 · 炼金之手：金币炼成经验 · 影分身：跟随并射击的分身 · 重力场：静止时吸引周围 · 逆转：最后的堡垒触发大爆炸 · 地图支配：地图物体重新出现（故事模式） · 连锁崩坏：被击败的敌人爆裂扩散 · 灵魂护盾：未受攻击时抵挡一次 · 魂火足迹：走过的地方燃烧");
        K("모든 캐릭터. 이야기 모드에서 3장 보스를, 무한 모드에서 세 번째 보스를 쓰러뜨리면 2차까지 진화한 무기가 각성합니다. 파괴(모든 공격 피해 +35%) · 질풍(공격 · 발사 간격 -25%) · 영혼(치명타 확률 +15%, 치명타 피해 +60%) 가운데 하나를 고릅니다.", "3장 보스(무한 모드는 세 번째 보스)를 쓰러뜨리면 2차까지 진화한 무기가 각성합니다. 파괴(모든 공격 피해 +35%) · 질풍(공격 · 발사 간격 -25%) · 영혼(치명타 확률 +15% · 치명타 피해 +60%) 중 하나를 고릅니다.", "Defeat the Chapter 3 boss (the third boss in Endless) to awaken your twice-evolved weapon. Choose Ruin (all attack damage +35%) · Gale (attack · fire interval -25%) · Soul (crit chance +15% · crit damage +60%).", "3章のボス（エンドレスは3体目のボス）を倒すと、第二進化まで終えた武器が覚醒します。破壊（すべての攻撃ダメージ +35%）・疾風（攻撃・発射間隔 -25%）・魂（クリティカル率 +15%・クリティカルダメージ +60%）から1つを選びます。", "击败第3章首领（无尽模式为第三个首领）后，完成二次进化的武器觉醒。从毁灭（所有攻击伤害 +35%）· 疾风（攻击 · 射击间隔 -25%）· 灵魂（暴击率 +15% · 暴击伤害 +60%）中选择一个。");
        A("보스를 쓰러뜨리면 무기가 진화합니다. 세 갈래 중 하나를 고르세요. (보통 판에서는 두 번 · 3장 보스는 각성, 되돌릴 수 없음)",
          "Defeating a boss evolves your weapon. Choose one of three paths. (Twice in a normal run · the Chapter 3 boss awakens it, cannot be undone)",
          "ボスを倒すと武器が進化します。3つの道から1つを選びましょう。（通常のプレイでは2回・3章のボスで覚醒、元に戻せません）",
          "击败首领后武器会进化。请从三条路线中选择一条。（普通对局中两次 · 第3章首领后觉醒，无法撤销）");
    }
}
