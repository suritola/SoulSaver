// 번역표 (1.1~): 판 후반 보상 — 3장 각성 · 초월 카드 (LevelShop.Transcend) · 도감 · 튜토리얼 문구
// LocTable.Skills 의 정적 생성자가 부름 (이미 있는 키는 건드리지 않음)
public static partial class LocTable
{
    static void AddTranscend()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }

        // 초월 카드 (1.2~ 「새 규칙」 열한 장, TranscendRules)
        A("초월 카드", "Transcendence Cards", "超越カード", "超越卡");
        A("초월: 새 규칙", "Transcendence: New Rule", "超越：新ルール", "超越：新规则");
        A("판의 규칙 하나를 바꿉니다 · 같은 카드를 다시 고르면 다음 단계",
          "Change one rule of this run · Pick the same card again to advance it",
          "このプレイのルールを1つ変えます・同じカードをもう一度選ぶと次の段階へ",
          "改变本局的一条规则 · 再次选择同一张卡会进入下一阶段");
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
        A("필살기를 쓰고 나면 잠시 뒤 그 자리에 영혼 메아리가 울려 둘레 적을 휩쓸고 적 탄을 지웁니다",
          "Shortly after your ultimate, a soul echo rings out where you stand, sweeping nearby enemies and erasing enemy shots",
          "必殺技を使った少し後、その場に魂の残響が響き、周りの敵をなぎ払い敵の弾を消します",
          "使用必杀技后不久，原地会响起灵魂回响，横扫周围敌人并消除敌方子弹");
        A("메아리에 휩쓸린 적이 1.5초 동안 얼어붙습니다", "Enemies caught in the echo freeze for 1.5s", "残響に巻き込まれた敵が1.5秒間凍りつきます", "被回响卷入的敌人冻结1.5秒");
        A("메아리가 한 번 더, 더 넓게 울립니다", "The echo rings once more, even wider", "残響がもう一度、さらに広く響きます", "回响会再响一次，范围更大");
        // 영혼 수확
        A("적 150마리를 쓰러뜨릴 때마다 영혼 파동이 둘레 잡몹을 쓸어 버립니다",
          "Every 150 kills, a soul wave sweeps away nearby minions",
          "敵を150体倒すたびに、魂の波動が周りの雑魚を一掃します",
          "每击败150个敌人，灵魂波动就会扫除周围的小怪");
        A("이제 110마리마다 영혼 파동이 일어납니다", "Now a soul wave every 110 kills", "これからは110体ごとに魂の波動", "现在每110个敌人触发一次灵魂波动");
        A("이제 75마리마다 영혼 파동이 일어납니다", "Now a soul wave every 75 kills", "これからは75体ごとに魂の波動", "现在每75个敌人触发一次灵魂波动");
        // 시간의 틈
        A("체력이 40% 아래일 때 맞으면 3초 동안 세상이 거의 멈춥니다 (60초마다)",
          "Get hit below 40% health and the world nearly stops for 3s (every 60s)",
          "体力40%未満で攻撃を受けると、3秒間世界がほぼ止まります（60秒ごと）",
          "生命低于40%时受到攻击，世界几乎静止3秒（每60秒一次）");
        A("시간의 틈이 4초로 길어지고 40초마다 열립니다", "The rift lasts 4s and opens every 40s", "時の裂け目が4秒に伸び、40秒ごとに開きます", "时间裂隙延长至4秒，每40秒开启一次");
        A("시간의 틈 동안 피해를 받지 않습니다", "You take no damage during the rift", "時の裂け目の間はダメージを受けません", "时间裂隙期间不会受到伤害");
        // 연금 손
        A("코인을 주울 때 10% 확률로 경험치로 연성됩니다 (다음 초월 카드가 빨라짐)",
          "Coins you pick up have a 10% chance to transmute into XP (your next Transcendence card comes sooner)",
          "コインを拾うと10%の確率で経験値に錬成されます（次の超越カードが早まる）",
          "拾取金币时有10%概率炼成经验（下一张超越卡来得更快）");
        A("연성 확률이 18%로 오릅니다", "Transmute chance rises to 18%", "錬成の確率が18%に上がります", "炼成概率提升至18%");
        A("연성 확률이 25%로 오르고, 연성할 때 영혼 조각도 하나 생깁니다", "Transmute chance rises to 25%, and each transmutation also gives a Soul Shard",
          "錬成の確率が25%に上がり、錬成するたびに魂のかけらも1つ手に入ります", "炼成概率提升至25%，每次炼成还会获得一枚灵魂碎片");
        // 그림자 분신
        A("그림자 분신 하나가 발자취를 따라오며 가까운 적에게 그림자 화살을 쏩니다",
          "A shadow clone follows in your footsteps and fires shadow bolts at nearby enemies",
          "影分身が1体足跡をたどってついて来て、近くの敵に影の矢を放ちます",
          "一个影分身沿着你的足迹跟随，向附近敌人射出暗影箭");
        A("그림자 분신이 둘이 됩니다", "You now have two shadow clones", "影分身が2体になります", "影分身增加到两个");
        A("그림자 분신이 셋이 됩니다", "You now have three shadow clones", "影分身が3体になります", "影分身增加到三个");
        // 중력장
        A("1초 동안 가만히 서 있으면 중력장이 생겨 둘레 잡몹과 코인을 끌어당깁니다",
          "Stand still for 1s to form a gravity well that pulls in nearby minions and coins",
          "1秒間じっと立っていると重力場が生まれ、周りの雑魚とコインを引き寄せます",
          "静止1秒后形成重力场，吸引周围的小怪和金币");
        A("중력장이 더 넓어집니다", "The gravity well grows wider", "重力場がさらに広がります", "重力场范围扩大");
        A("중력장이 더 넓어지고, 끌려온 적이 느려집니다", "The gravity well grows wider and slows the enemies it pulls", "重力場がさらに広がり、引き寄せた敵が遅くなります", "重力场范围扩大，被吸引的敌人会减速");
        // 역전
        A("최후의 보루가 발동하면 그 자리에서 대폭발이 일고 무적이 4초로 늘어납니다",
          "When Last Stand triggers, a huge explosion erupts on the spot and your invincibility lasts 4s",
          "最後の砦が発動すると、その場で大爆発が起き、無敵が4秒に伸びます",
          "最后的堡垒触发时，原地引发大爆炸，无敌时间延长至4秒");
        A("최후의 보루가 장마다 한 번 더 발동합니다", "Last Stand triggers one more time per chapter", "最後の砦が章ごとにもう1回発動します", "最后的堡垒每章多触发一次");
        A("역전할 때 체력의 50%를 회복합니다", "Reversal also restores 50% of your health", "逆転するとき体力の50%を回復します", "逆转时恢复50%生命");
        // 맵 지배
        A("부순 맵 물체가 18초 뒤 다른 자리에 다시 생기고, 부술 때 폭발합니다",
          "Broken map objects reappear elsewhere after 18s, and explode when you break them",
          "壊したマップの物体が18秒後に別の場所に再び現れ、壊すと爆発します",
          "被打碎的地图物体会在18秒后于别处重新出现，打碎时还会爆炸");
        A("맵 물체가 10초 만에 다시 생기고 폭발이 커집니다", "Map objects reappear in 10s and explode bigger", "マップの物体が10秒で再び現れ、爆発が大きくなります", "地图物体10秒后重新出现，爆炸范围更大");
        // 연쇄 붕괴
        A("쓰러진 적이 12% 확률로 터져 둘레 적에게 번집니다 (터진 적도 다시 터짐)",
          "Slain enemies have a 12% chance to burst and spread to those nearby (bursts can chain)",
          "倒した敵が12%の確率で破裂し、周りの敵に広がります（破裂で倒れた敵も再び破裂）",
          "被击败的敌人有12%概率爆裂并波及周围敌人（爆裂击败的敌人也会再次爆裂）");
        A("연쇄 확률이 18%로 오릅니다", "Chain chance rises to 18%", "連鎖の確率が18%に上がります", "连锁概率提升至18%");
        A("연쇄 확률이 25%로 오르고 폭발이 커집니다", "Chain chance rises to 25% and bursts grow bigger", "連鎖の確率が25%に上がり、爆発が大きくなります", "连锁概率提升至25%，爆炸范围更大");
        // 영혼 보호막
        A("8초 동안 맞지 않으면 공격 한 번을 막는 영혼 보호막이 생깁니다",
          "Go 8s without being hit to gain a soul ward that blocks one attack",
          "8秒間攻撃を受けなければ、攻撃を1回防ぐ魂の障壁が生まれます",
          "8秒内未受攻击，就会生成可抵挡一次攻击的灵魂护盾");
        A("보호막이 5초 만에 다시 생깁니다", "The ward returns in 5s", "障壁が5秒で再び生まれます", "护盾5秒即可重新生成");
        A("보호막이 깨질 때 둘레 적을 밀쳐 내고 얼립니다", "When the ward breaks, it pushes back and freezes nearby enemies", "障壁が割れるとき、周りの敵を押しのけて凍らせます", "护盾破碎时击退并冻结周围敌人");
        // 영혼불 발자국
        A("움직이면 지나간 자리에 영혼불이 남아 밟은 적을 태웁니다",
          "As you move, soulfire lingers in your trail and burns enemies who step in it",
          "移動すると通った跡に魂火が残り、踏んだ敵を燃やします",
          "移动时走过的地方会留下魂火，灼烧踏入的敌人");
        A("영혼불이 더 오래 남습니다", "Soulfire lingers longer", "魂火がより長く残ります", "魂火持续更久");
        A("영혼불이 더 넓고 오래 남습니다", "Soulfire spreads wider and lingers longer", "魂火がより広く、長く残ります", "魂火范围更大、持续更久");

        // 게임 안 짧은 글
        A("메아리!", "Echo!", "残響！", "回响！");
        A("영혼 수확!", "Reaping!", "魂の収穫！", "灵魂收割！");
        A("시간의 틈!", "Time Rift!", "時の裂け目！", "时间裂隙！");
        A("연성!", "Transmute!", "錬成！", "炼成！");
        A("역전!", "Reversal!", "逆転！", "逆转！");
        A("막아 냄!", "Blocked!", "防いだ！", "挡下了！");

        A("최대 레벨에 닿은 뒤에는 경험치 막대를 세 번 채울 때마다 초월 카드로 판의 규칙 하나를 바꿉니다. (레벨은 오르지 않고, 같은 카드를 다시 고르면 다음 단계가 됩니다)",
          "After reaching max level, every third time your XP bar fills, a Transcendence card changes one rule of the run. (Your level doesn't rise; picking the same card again advances it to the next stage)",
          "最大レベルに達した後は、経験値バーを3回満タンにするたびに超越カードでこのプレイのルールを1つ変えます。（レベルは上がらず、同じカードをもう一度選ぶと次の段階になります）",
          "达到等级上限后，每填满经验条三次，就用超越卡改变本局的一条规则。（等级不会提升，再次选择同一张卡会进入下一阶段）");

        // 도감 · 튜토리얼
        A("레벨이 오를 때 세 장 중 하나를 고르거나 건너뜁니다 · 최대 레벨 50 · 무한 모드 80 (그 뒤로는 경험치 막대를 세 번 채울 때마다 초월 카드) · 재료를 모두 올리면 스킬 진화",
          "Pick one of three cards or skip on each level-up · Max level 50 · Endless 80 (after that, a Transcendence card every third time the XP bar fills) · Max all ingredients to evolve a skill",
          "レベルアップごとに3枚から1枚を選ぶかスキップ・最大レベル50・エンドレス80（その後は経験値バーを3回満タンにするたびに超越カード）・素材をすべて上げるとスキル進化",
          "每次升级从三张中选一张或跳过 · 等级上限50 · 无尽80（之后每填满经验条三次获得超越卡）· 素材全部升满即可技能进化");
        A("최대 레벨(50 · 무한 모드 80)에 닿은 뒤 경험치 막대를 세 번 채울 때마다 (레벨은 오르지 않음) 금테 초월 카드 세 장 중 하나를 고릅니다. 파괴: 모든 무기 피해 +10% (최대 5번) · 충전: 필살기 게이지 차는 속도 +25% (최대 3번) · 급소: 치명타 확률 +5%, 치명타 피해 +20% (최대 4번) · 생명: 최대 체력 +25, 체력 모두 회복 (최대 4번) · 질풍: 공격 속도 +6% (최대 5번)",
          "After reaching max level (50 · Endless 80), every third time your XP bar fills (your level doesn't rise) you pick one of three gold-framed Transcendence cards. Ruin: all weapon damage +10% (up to 5) · Charge: ultimate gauge charge speed +25% (up to 3) · Vitals: crit chance +5%, crit damage +20% (up to 4) · Life: max health +25, fully heal (up to 4) · Gale: fire rate +6% (up to 5)",
          "最大レベル（50・エンドレス80）に達した後、経験値バーを3回満タンにするたびに（レベルは上がりません）金枠の超越カード3枚から1枚を選びます。破壊：すべての武器ダメージ +10%（最大5回）・充填：必殺技ゲージの溜まる速さ +25%（最大3回）・急所：会心率 +5%、会心ダメージ +20%（最大4回）・生命：最大体力 +25、体力全回復（最大4回）・疾風：攻撃速度 +6%（最大5回）",
          "达到等级上限（50 · 无尽80）后，每填满经验条三次（等级不会提升），就从三张金框超越卡中选择一张。毁灭：所有武器伤害 +10%（最多5次）· 充能：必杀技槽充能速度 +25%（最多3次）· 要害：暴击率 +5%，暴击伤害 +20%（最多4次）· 生命：最大生命 +25，生命全部恢复（最多4次）· 疾风：攻击速度 +6%（最多5次）");
        A("모든 캐릭터. 이야기 모드에서 3장 보스를, 무한 모드에서 세 번째 보스를 쓰러뜨리면 2차까지 진화한 무기가 각성합니다. 파괴(모든 공격 피해 +35%) · 질풍(공격 · 발사 간격 -25%) · 영혼(치명타 확률 +15%, 치명타 피해 +60%) 가운데 하나를 고릅니다.",
          "All characters. Defeat the Chapter 3 boss in Story mode, or the third boss in Endless mode, and your twice-evolved weapon awakens. Choose one: Ruin (all attack damage +35%) · Gale (attack / fire interval -25%) · Soul (crit chance +15%, crit damage +60%).",
          "全キャラクター。ストーリーモードで3章のボスを、エンドレスモードで3体目のボスを倒すと、2次まで進化した武器が覚醒します。破壊（すべての攻撃ダメージ +35%）・疾風（攻撃・発射間隔 -25%）・魂（会心率 +15%、会心ダメージ +60%）から1つを選びます。",
          "所有角色。在故事模式击败第3章首领，或在无尽模式击败第三个首领后，完成二次进化的武器将会觉醒。从毁灭（所有攻击伤害 +35%）· 疾风（攻击·射击间隔 -25%）· 灵魂（暴击率 +15%，暴击伤害 +60%）中选择一个。");
        A("보스를 쓰러뜨리면 무기가 진화합니다. 세 갈래 중 하나를 고르세요. (보통 판에서는 두 번 · 3장 보스는 각성, 되돌릴 수 없음)",
          "Defeating a boss evolves your weapon. Choose one of three paths. (Twice in a normal run · the Chapter 3 boss awakens it, cannot be undone)",
          "ボスを倒すと武器が進化します。3つの道から1つを選びましょう。(通常のプレイでは2回・3章のボスで覚醒、元に戻せません)",
          "击败首领后武器会进化。从三条路线中选择一条。(普通对局中两次 · 第3章首领后觉醒，无法撤销)");
    }
}
