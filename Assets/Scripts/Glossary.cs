using System.Collections.Generic;
using System.Text;

// 1.4.8~ 용어 사전 (docs/review/2026-10-21-UI-정리-기획.md 4절)
// 용어마다 언어별 화면 표기(변형 포함) · 한 줄 정의(네 언어, 「~합니다.」 40자 안팎)
//   Link(글)      : 글 속 용어를 <link="term:id"><u>…</u></link> 로 감쌈 (TermLinks 가 부름 · 마우스를 올리면 정의)
//   WithNotes(…)  : 툴팁 본문 아래에 「· 용어: 정의」를 작게 덧붙임 (TooltipUI · 커서를 따라다니는 창이라 다시 올릴 수 없어서)
// 표기 규칙 (언어별 '|' 로 나눔): '=' 앞붙임 = 영어 대소문자 그대로만 (없으면 첫 글자 대 · 소문자 둘 다)
//                               '^' 앞붙임 = 앞뒤가 다른 낱말에 붙어 있으면 안 됨 (한국어는 뒤에 조사만 허용) — 「숨」「대가」처럼 짧은 말
//   첫 표기가 툴팁 제목 · 덧붙임 이름 · 가장 긴 표기부터 찾음 · 막음 목록(Stop)은 다른 뜻의 이름(「연쇄 광풍」 등)을 건너뛰게 함
//   영어 「Mastery」 는 극의 · 숙련이 같은 말 → 극의만 대문자 그대로(=) · 숙련 쪽 이름(Shadow Mastery 등)은 막음
// 정의 속 {ULT} 같은 키 표시는 KeyBindings.Apply 로 지금 키 이름 · 수치가 자주 바뀌는 것은 개념만 (바뀌지 않는 문턱만 숫자)
public static class Glossary
{
    sealed class Term
    {
        public string id;
        public readonly string[][] names = new string[4][];
        public string[] def;
    }

    struct Entry
    {
        public string s;
        public int term;            // -1 = 막음 (다른 뜻)
        public bool strict;
    }

    static readonly List<Term> terms = new List<Term>(64);
    static readonly List<string>[] stops = { new List<string>(), new List<string>(), new List<string>(), new List<string>() };
    static readonly Dictionary<char, Entry[]>[] index = new Dictionary<char, Entry[]>[4];

    public static int Count { get { Init(); return terms.Count; } }

    // ================================================================= 사전
    static bool built;

    static void Init()
    {
        if (built) return;
        built = true;

        // ---- 전투 기본
        Add("ult", "필살기", "Ultimate|Ultimates", "必殺技", "必杀技",
            "필살기 게이지가 가득 차면 [{ULT}]로 쓰는 영웅의 큰 기술입니다.",
            "Your hero's big move, used with [{ULT}] once the Ultimate gauge is full.",
            "必殺技ゲージが満タンのとき[{ULT}]で使う英雄の大技です。",
            "必杀技槽满后按[{ULT}]使用的英雄大招。");
        Add("ultgauge", "필살기 게이지", "Ultimate gauge|Ultimate Gauge", "必殺技ゲージ", "必杀技槽|必杀技能量",
            "적을 맞히거나 쓰러뜨리면 차오르고, 가득 차면 필살기를 쓸 수 있습니다.",
            "Fills as you hit and defeat enemies. When full, you can use your Ultimate.",
            "敵に当てたり倒したりすると溜まり、満タンで必殺技が使えます。",
            "命中或击败敌人时积累，满后即可使用必杀技。");
        Add("basic", "평타", "basic attack|basic attacks", "通常攻撃", "普通攻击|普攻",
            "[{ATTACK}]로 하는 영웅의 기본 공격입니다.",
            "Your hero's basic attack, used with [{ATTACK}].",
            "[{ATTACK}]で行う英雄の基本攻撃です。",
            "按[{ATTACK}]进行的英雄基本攻击。");
        Add("cooldown", "쿨타임", "cooldown|cooldowns", "クールタイム", "冷却时间",
            "한 번 쓴 뒤 다시 쓸 수 있을 때까지 기다리는 시간입니다.",
            "The wait before you can use it again.",
            "一度使ってから再び使えるまでの待ち時間です。",
            "使用后到可以再次使用的等待时间。");
        Add("invuln", "무적", "invincible|invincibility", "無敵", "无敌",
            "그동안 어떤 피해도 받지 않습니다.",
            "You take no damage while it lasts.",
            "その間、一切ダメージを受けません。",
            "持续期间不受任何伤害。");
        Add("stack", "중첩", "stack|stacks", "スタック", "叠层|叠加",
            "같은 효과가 겹쳐 쌓인 수입니다. 쌓일수록 효과가 커집니다.",
            "How many times an effect has piled up. More stacks, stronger effect.",
            "同じ効果が重なった数です。重なるほど効果が大きくなります。",
            "同一效果叠加的层数。层数越多效果越强。");
        Add("crit", "치명타", "crit|crits|critical hit|critical hits", "クリティカル", "暴击",
            "확률로 터지는 강한 공격입니다. 피해가 치명타 배율만큼 커집니다.",
            "A lucky hit that deals extra damage based on your crit multiplier.",
            "確率で出る強力な攻撃です。ダメージがクリティカル倍率の分だけ増えます。",
            "按几率触发的强力攻击，伤害按暴击倍率提高。");
        Add("pierce", "관통|꿰뚫", "pierce|pierces|piercing", "貫通|貫く|貫き", "穿透",
            "총알 · 화살 등이 적을 꿰뚫고 지나가 뒤의 적도 맞힙니다.",
            "Shots pass through enemies and hit the ones behind.",
            "弾や矢が敵を貫き、後ろの敵にも当たります。",
            "子弹或箭矢穿过敌人，命中后方的敌人。");
        Add("chain", "연쇄", "=Chain|chains", "連鎖", "连锁",
            "맞힌 적에서 가까운 적으로 옮겨 가며 이어서 맞힙니다. 숫자는 옮겨 가는 횟수입니다.",
            "Hits jump from the struck enemy to nearby ones. The number is how many jumps.",
            "当たった敵から近くの敵へ移りながら続けて当たります。数字は移る回数です。",
            "从命中的敌人跳向附近的敌人继续命中。数字为跳跃次数。");
        Add("shield", "보호막", "shield|shields", "シールド", "护盾",
            "받는 피해를 대신 막아 줍니다. 다 닳거나 깨지면 사라집니다.",
            "Absorbs incoming damage for you. Disappears when depleted or broken.",
            "受けるダメージを代わりに防ぎます。削り切られるか壊れると消えます。",
            "代为抵挡受到的伤害。耗尽或破碎后消失。");

        // ---- 상태 이상
        Add("bleed", "출혈", "bleed|bleeds|bleeding", "出血", "出血",
            "시간이 지나며 계속 피해를 받는 상태 이상입니다.",
            "A status that deals damage over time.",
            "時間とともにダメージを受け続ける状態異常です。",
            "随时间持续受到伤害的异常状态。");
        Add("burn", "화상", "burn|burns|burning", "火傷|燃焼", "灼烧|燃烧",
            "불이 붙어 시간이 지나며 계속 피해를 받는 상태 이상입니다.",
            "Set on fire, the enemy keeps taking damage over time.",
            "火が付き、時間とともにダメージを受け続ける状態異常です。",
            "被点燃后持续受到伤害的异常状态。");
        Add("frost", "빙결", "=Frost|frost", "氷結", "冰结|冰冻",
            "냉기로 적을 느리게 하거나 얼려 움직임을 막습니다.",
            "Cold that slows enemies or freezes them in place.",
            "冷気で敵を遅くしたり凍らせたりして動きを止めます。",
            "以寒气使敌人减速或将其冻结。");
        Add("stun", "기절", "stun|stuns|stunned", "気絶", "眩晕",
            "잠깐 움직이거나 공격하지 못합니다.",
            "Briefly unable to move or attack.",
            "少しの間、動いたり攻撃したりできません。",
            "短时间内无法移动或攻击。");

        // ---- 성장 (판 안)
        Add("exp", "경험치", "=EXP|=XP", "経験値", "经验值|经验",
            "적을 쓰러뜨리면 얻습니다. 막대가 가득 차면 레벨이 올라 레벨업 카드를 고릅니다.",
            "Gained by defeating enemies. Fill the bar to level up and pick a level-up card.",
            "敵を倒すと得られます。バーが満タンになるとレベルが上がり、レベルアップカードを選びます。",
            "击败敌人获得。经验条满后升级并选择升级卡。");
        Add("lvcard", "레벨업 카드", "level-up card|level-up cards|Level-Up Card|Level-Up Cards", "レベルアップカード", "升级卡牌|升级卡",
            "레벨이 오를 때마다 세 장 중 하나를 골라 능력을 얻거나 올립니다.",
            "Each level-up, pick one of three to gain or upgrade an ability.",
            "レベルが上がるたびに3枚から1枚を選び、能力を得るか強化します。",
            "每次升级从三张中选一张，获得或强化能力。");
        Add("reroll", "다시 뽑기", "reroll|rerolls|redraw", "引き直し", "重抽",
            "[{REROLL}]로 지금 나온 카드를 새로 뽑습니다. 쓸 수 있는 횟수가 정해져 있습니다.",
            "Press [{REROLL}] to draw new cards. Uses are limited.",
            "[{REROLL}]で今のカードを引き直します。使える回数は限られています。",
            "按[{REROLL}]重新抽取当前卡牌。次数有限。");
        Add("evo", "진화", "evolution|evolutions|evolve|evolves|evolved|evolving", "進化", "进化",
            "능력이 더 강한 모습으로 바뀝니다. 무기 진화 · 스킬 진화 · 특수 능력 진화가 있습니다.",
            "An ability turns into a stronger form. Weapons, skills and special abilities can evolve.",
            "能力がより強い姿に変わります。武器進化・スキル進化・特殊能力の進化があります。",
            "能力变为更强的形态。分为武器进化、技能进化与特殊能力进化。");
        Add("skillevo", "스킬 진화", "skill evolution|skill evolutions|Skill Evolution|Skill Evolutions", "スキル進化", "技能进化",
            "재료 레벨업 카드를 모두 최대 단계로 올리면 하나로 합쳐져 새 능력이 됩니다.",
            "Max out all of its ingredient level-up cards and they merge into a new ability.",
            "素材のレベルアップカードをすべて最大段階にすると、一つに合わさり新しい能力になります。",
            "将所有素材升级卡升到最高等级后，合并为新能力。");
        Add("ingredient", "재료|재료 카드", "ingredient|ingredients", "素材", "素材|材料",
            "스킬 진화에 필요한 레벨업 카드입니다. 모두 최대 단계로 올리면 진화합니다.",
            "Level-up cards needed for a skill evolution. Max them all to evolve.",
            "スキル進化に必要なレベルアップカードです。すべて最大段階にすると進化します。",
            "技能进化所需的升级卡。全部升到最高等级即可进化。");
        Add("weaponevo", "무기 진화", "weapon evolution|weapon evolutions|Weapon Evolution|Weapon Evolutions", "武器進化", "武器进化",
            "보스를 쓰러뜨린 뒤 몇 가지 중 하나를 골라 무기를 더 강하게 바꿉니다.",
            "After defeating a boss, pick one of several forms to power up your weapon.",
            "ボスを倒した後、いくつかの中から一つを選んで武器を強化します。",
            "击败首领后，从数个选项中选择一个强化武器。");
        Add("awaken", "각성", "awakening|awakenings", "覚醒", "觉醒",
            "무기를 2차까지 진화한 뒤 3장 보스(무한 모드는 세 번째 보스)를 쓰러뜨리면 고르는 마지막 진화입니다.",
            "The final evolution, chosen after a second evolution once you beat the Chapter 3 boss (third boss in Endless).",
            "武器を第二進化させた後、3章のボス（エンドレスは3体目のボス）を倒すと選べる最後の進化です。",
            "武器完成第二次进化后，击败第3章首领（无尽模式为第三个首领）时选择的最终进化。");
        Add("special", "특수 능력", "special ability|special abilities|Special Ability|Special Abilities", "特殊能力", "特殊能力",
            "보스 · 중간 보스가 주는 포인트로 배우는 무기 · 스킬 · 패시브입니다. 한 번 더 고르면 진화합니다.",
            "Weapons, skills and passives learned with points from bosses and mid-bosses. Pick one again to evolve it.",
            "ボス・中ボスがくれるポイントで習得する武器・スキル・パッシブです。もう一度選ぶと進化します。",
            "用首领与中头目给予的点数学习的武器、技能与被动。再次选择即可进化。");
        Add("shard", "영혼 조각", "Soul Shards|Soul Shard|soul shards|soul shard", "魂のかけら", "灵魂碎片",
            "보스 · 중간 보스 · 제단 등에서 얻습니다. 영혼 트리 칸을 배우는 데 씁니다.",
            "Earned from bosses, mid-bosses, altars and more. Spent to learn Soul Tree nodes.",
            "ボス・中ボス・祭壇などで手に入ります。魂のツリーのマスを習得するのに使います。",
            "从首领、中头目、祭坛等处获得。用于学习灵魂树节点。");
        Add("soultree", "영혼 트리", "Soul Tree", "魂のツリー", "灵魂树",
            "판 안에서 영혼 조각으로 칸을 배워 무기 · 필살기 · 생존 · 영혼을 키우는 나무입니다.",
            "An in-run tree where Soul Shards buy nodes for weapon, Ultimate, survival and soul.",
            "プレイ中に魂のかけらでマスを習得し、武器・必殺技・生存・魂を育てるツリーです。",
            "局内用灵魂碎片学习节点，强化武器、必杀技、生存与灵魂的树。");
        Add("transcend", "초월 카드|초월", "Transcendence card|Transcendence cards|Transcendence Card|Transcendence Cards|Transcendence", "超越のカード|超越カード|超越", "超越卡",
            "최대 레벨에 닿은 뒤 경험치 막대를 채우면 고르는 카드입니다. 판의 규칙 하나를 바꿉니다.",
            "Chosen by filling the EXP bar after max level. Each one changes a rule of the run.",
            "最大レベル到達後、経験値バーを満たすと選べるカードです。プレイのルールを一つ変えます。",
            "达到最高等级后填满经验条即可选择的卡。会改变本局的一条规则。");
        Add("laststand", "최후의 보루", "Last Stand", "最後の砦", "最后的堡垒",
            "체력이 30% 아래로 떨어지면 장마다 한 번 저절로 발동합니다. 잠깐 무적이 되고 둘레의 적을 밀어냅니다.",
            "Triggers once per chapter when HP drops below 30%. Brief invincibility and a push on nearby enemies.",
            "体力が30%を下回ると章ごとに一度自動で発動します。少しの間無敵になり、周りの敵を押し出します。",
            "生命低于30%时每章自动触发一次。短暂无敌并击退周围的敌人。");

        // ---- 갈래
        Add("path", "갈래", "=Path|=Paths", "流派", "流派",
            "이번 판의 성장 방향입니다. 입문 카드로 고르고, 맞는 카드를 모으면 공명이 열립니다.",
            "This run's build direction. Pick it with a Path Seed; collect matching cards to unlock Resonance.",
            "このプレイの成長の方向です。入門カードで選び、合うカードを集めると共鳴が開きます。",
            "本局的成长方向。用入门卡选择，收集相符的卡即可开启共鸣。");
        Add("seed", "입문|입문 카드", "Path Seed|Path Seeds", "入門|入門カード", "入门|入门卡",
            "갈래를 정하는 첫 카드입니다. 평타나 움직임을 바꾸고 대가가 붙습니다.",
            "The first card of a Path. Changes your basic attack or movement, with a cost.",
            "流派を決める最初のカードです。通常攻撃や動きを変え、代償が付きます。",
            "决定流派的第一张卡。改变普通攻击或移动方式，并附带代价。");
        Add("resonance", "공명|공명 I|공명 II", "Resonance|Resonance I|Resonance II", "共鳴|共鳴 I|共鳴 II|共鳴I|共鳴II", "共鸣|共鸣 I|共鸣 II|共鸣I|共鸣II",
            "갈래 단계(입문 1 + 갈래 카드 단계)가 4 · 8에 닿으면 열리는 갈래 효과입니다.",
            "Path effects unlocked when Path rank (Seed 1 + path card ranks) reaches 4 and 8.",
            "流派段階（入門1＋流派カードの段階）が4・8に達すると開く流派の効果です。",
            "流派阶段（入门1＋流派卡等级）达到4和8时开启的流派效果。");
        Add("mastery", "극의", "=Mastery", "極意", "极意",
            "「외길」을 고르고 늘어난 단계까지 갈래 카드를 모두 채우면 열리는 마지막 효과입니다.",
            "The final effect, unlocked by choosing Single Path and filling your path cards' extra ranks.",
            "「一筋」を選び、増えた段階まで流派カードをすべて満たすと開く最後の効果です。",
            "选择「专精」并将流派卡填满增加的等级后开启的最终效果。");
        Add("cost", "^대가", "=Cost", "代償", "代价",
            "입문 카드에 붙는 약점입니다. 갈래의 힘을 얻는 대신 감수합니다.",
            "The drawback attached to a Path Seed: the price of the path's power.",
            "入門カードに付く弱みです。流派の力と引き換えに受け入れます。",
            "入门卡附带的劣势，是获得流派力量的代价。");

        // ---- 영웅 고유
        Add("perfect", "「완벽!」|완벽!", "Perfect!|=Perfect arrow|=Perfect arrows|=Perfect shot|=Perfect shots", "「パーフェクト！」|パーフェクト", "「完美！」|完美！",
            "궁수가 시위가 가득 찬 순간(딩!)에 놓아 쏘는 강한 화살입니다. 숨을 되돌려 줍니다.",
            "The Archer's strong shot, released the instant the draw is full (ding!). Restores Breath.",
            "弦が引き絞られた瞬間（ディン！）に放つ弓使いの強い矢です。息を回復します。",
            "弓箭手在拉满的瞬间（叮！）松手射出的强力箭矢。会恢复气息。");
        Add("breath", "^숨", "=Breath|breath", "^息", "气息",
            "궁수가 시위를 당기는 동안 닳습니다. 바닥나면 활이 떨려 위력이 절반이 됩니다.",
            "Drains while the Archer draws. When empty, the bow shakes and shots deal half power.",
            "弓使いが弓を引いている間に減ります。尽きると弓が震え、威力が半分になります。",
            "弓箭手拉弓时消耗。耗尽后弓会颤抖，威力减半。");
        Add("windlane", "바람길", "wind lane|wind lanes", "風の道", "风道",
            "가득 당긴 화살이 지나간 자리에 남는 길입니다. 그 위에 서 있으면 빨라집니다.",
            "A trail left by fully drawn arrows. Standing on it makes you faster.",
            "引き絞った矢が通った跡に残る道です。その上に立つと速くなります。",
            "满弓之箭飞过后留下的路径。站在其上移动更快。");
        Add("ambush", "급습|급습 시간", "=Ambush", "急襲", "突袭",
            "도적이 필살기를 마친 뒤 2초 동안입니다. 급습 효과가 이때 켜집니다.",
            "The 2 seconds after the Rogue's Ultimate ends. Ambush effects are active then.",
            "盗賊の必殺技が終わってから2秒間です。急襲の効果がこの間に発動します。",
            "盗贼必杀技结束后的2秒。突袭效果在此期间生效。");
        Add("knot", "그림자 매듭|그림자 줄로 묶여", "Shadow Knot|knotted", "影の結び目", "暗影之结",       // 「묶인 적」은 궁수 덫(묶임)과 겹쳐 쓰지 않음
            "도적의 그림자 매듭에 묶인 적들은 하나가 받은 피해의 일부를 함께 받습니다.",
            "Enemies bound by the Rogue's Shadow Knot share part of the damage any one of them takes.",
            "盗賊の影の結び目に結ばれた敵は、一体が受けたダメージの一部を共に受けます。",
            "被盗贼暗影之结绑定的敌人，会共同承受其中一个所受伤害的一部分。");
        Add("possess", "빙의", "Possession|possess|possessed|possesses", "憑依", "附身",
            "유령이 적의 몸에 들어가 잠시 끌고 다니다가 안에서 터뜨립니다. 들어간 동안 무적입니다.",
            "The Ghost enters an enemy, drags it around for a while, then bursts it from inside. Invincible meanwhile.",
            "幽霊が敵の体に入り、しばらく引き回してから内側から破裂させます。入っている間は無敵です。",
            "幽灵进入敌人体内，操纵一阵后从内部引爆。附身期间无敌。");
        Add("rewind", "되감기", "=Rewind", "巻き戻し", "倒带",
            "시계공이 몇 초 전 자리로 돌아가 잃은 체력 일부를 되찾고 둘레의 적에게 되갚습니다.",
            "The Clockmaker returns to where they were seconds ago, recovering lost health and paying it back to nearby enemies.",
            "時計職人が数秒前の位置に戻り、失った体力の一部を取り戻して周りの敵にやり返します。",
            "钟表匠回到数秒前的位置，夺回部分失去的生命并反击周围的敌人。");

        // ---- 적 · 보스 · 판의 사건
        Add("barrier", "결계|결계 게이지", "barrier|barriers|barrier gauge", "結界|結界ゲージ", "结界|结界槽",
            "보스를 때리면 차는 결계 게이지가 가득 차면, 보스가 둘레를 가두고 잠시 공격을 퍼붓습니다.",
            "Hitting a boss fills its barrier gauge. When full, it traps you nearby and unleashes attacks for a while.",
            "ボスを攻撃すると溜まる結界ゲージが満タンになると、ボスが周りを閉じ込めてしばらく攻撃を浴びせます。",
            "攻击首领会积累结界槽。满后首领会封锁周围，猛攻一段时间。");
        Add("erosion", "공간 침식", "Spatial Erosion", "空間侵食", "空间侵蚀",
            "보스가 둘 이상일 때 결계가 하나로 합쳐져 펼쳐지는 거대한 결계입니다.",
            "With two or more bosses, their barriers merge into one giant barrier.",
            "ボスが二体以上いるとき、結界が一つに合わさって展開される巨大な結界です。",
            "存在两个以上首领时，各自的结界合而为一展开的巨大结界。");
        Add("weak", "빈틈", "Weak Point|Weak Points|weak point|weak points|WEAK POINT", "隙", "破绽",
            "보스 · 중간 보스 몸의 빛나는 점입니다. 직접 조준한 공격으로 맞히면 피해가 늘어납니다.",
            "A glowing spot on bosses and mid-bosses. Aimed attacks that hit it deal extra damage.",
            "ボス・中ボスの体に光る点です。直接狙った攻撃で当てるとダメージが増えます。",
            "首领与中头目身上的发光点。用直接瞄准的攻击命中可提高伤害。");
        Add("midboss", "중간 보스", "mid-boss|mid-bosses|mini-boss|mini-bosses", "中ボス", "中头目|精英首领",
            "장 중간에 나오는 크고 단단한 적입니다. 쓰러뜨리면 특수 능력 포인트나 영혼 조각과 정예 보물 상자를 줍니다.",
            "A big, tough foe that appears mid-chapter. Drops Special Ability Points or Soul Shards, plus an elite chest.",
            "章の途中に出る大きくて硬い敵です。倒すと特殊能力ポイントか魂のかけらと、精鋭の宝箱を落とします。",
            "章节中途出现的高大坚韧的敌人。击败后掉落特殊能力点或灵魂碎片，以及精英宝箱。");
        Add("elite", "정예|엘리트", "elite|elites", "精鋭|エリート", "精英",
            "더 크고 단단하며 보상이 많은 적입니다. 정예 보물 상자는 좋은 보상이 더 잘 나옵니다.",
            "Bigger, tougher enemies with better rewards. Elite chests roll rarer rewards more often.",
            "より大きく硬く、報酬の多い敵です。精鋭の宝箱は良い報酬が出やすくなります。",
            "更大、更坚韧且奖励更多的敌人。精英宝箱更容易出好奖励。");
        Add("shop", "떠돌이 상점", "Wandering Shop|wandering shop", "さすらいのショップ", "流浪商店",
            "판 중간에 잠깐 나타나는 상점입니다. 사라지기 전에 다가가 코인으로 물건을 삽니다.",
            "A shop that appears briefly mid-run. Reach it before it leaves to spend your coins.",
            "プレイ中に少しの間だけ現れるショップです。消える前に近づいてコインで買い物をします。",
            "局中短暂出现的商店。在消失前靠近，用金币购买物品。");
        Add("relic", "유물", "relic|relics", "遺物", "遗物",
            "[{RELIC}]로 쓰는 특별한 도구입니다. 봉인된 석관에서 얻고 한 판에 두 개까지 가집니다.",
            "A special tool used with [{RELIC}]. Found in Sealed Sarcophagi; carry up to two per run.",
            "[{RELIC}]で使う特別な道具です。封印された石棺で手に入り、1プレイに2つまで持てます。",
            "按[{RELIC}]使用的特殊道具。从封印的石棺获得，每局最多持有两个。");
        Add("sarco", "봉인된 석관|석관", "Sealed Sarcophagus|Sealed Sarcophagi|sarcophagus|sarcophagi", "封印された石棺|石棺", "封印的石棺|封印石棺|石棺",
            "2장부터 나타납니다. 곁에 머물러 봉인을 부수면 유물을 고릅니다.",
            "Appears from Chapter 2. Stay beside it to break the seal and choose a relic.",
            "2章から現れます。そばにとどまって封印を壊すと遺物を選べます。",
            "第2章起出现。停留在旁打破封印即可选择遗物。");

        // ---- 판 밖 (모드 · 규칙)
        Add("brand", "영혼 각인|각인", "Soul Brand|Soul Brands|=Brand|=Brands", "魂の刻印","灵魂烙印|烙印",
            "이야기 모드 판을 시작할 때 고르는 규칙입니다. 강점과 약점을 함께 줍니다.",
            "A rule chosen at the start of a Story run. It grants a strength and a weakness together.",
            "ストーリーモードの開始時に選ぶルールです。強みと弱みを同時に与えます。",
            "故事模式开局时选择的规则。同时带来优势与劣势。");
        Add("weight", "영혼의 무게", "Soul Weight", "魂の重み", "灵魂之重",
            "어려움을 클리어하면 열리는 추가 도전입니다. 무게가 오를수록 어려워지고 포인트가 늘어납니다.",
            "An extra challenge unlocked by clearing Hard. Higher weight means harder runs and more points.",
            "ハードをクリアすると開く追加の挑戦です。重みが増すほど難しくなり、ポイントも増えます。",
            "通关困难难度后开启的额外挑战。重量越高越难，点数也越多。");
        Add("contract", "영혼 계약", "Soul Contract|Soul Contracts", "魂の契約", "灵魂契约",
            "판마다 주어지는 목표 세 가지입니다. 이루면 그 자리에서 포인트를 받습니다.",
            "Three goals offered each run. Complete one to earn points on the spot.",
            "プレイごとに提示される3つの目標です。達成するとその場でポイントがもらえます。",
            "每局提供的三个目标。达成后当场获得点数。");
        Add("beyond", "한계 돌파", "Beyond the Limit|beyond the limit", "限界突破", "突破极限",
            "무한 모드를 클리어한 뒤 이어지는 구간입니다. 최대 레벨이 100으로 늘어납니다.",
            "The stretch that continues after clearing Endless. Max level rises to 100.",
            "エンドレスをクリアした後に続く区間です。最大レベルが100に上がります。",
            "通关无尽模式后继续的阶段。最高等级提升至100。");

        // ---- 막음: 같은 글자지만 다른 뜻 (이름 · 일반 낱말)
        Stop(0, "연쇄 광풍|연쇄 처치|연쇄 반응|연쇄 붕괴|세 갈래|네 갈래|다섯 갈래|여러 갈래|여덟 갈래|이 갈래를 고르면|한 갈래를|두 갈래 더|갈래마다|갈래 수|매의 급습|숨은|숨을 쉬|숨 쉬|숨도 쉬|숨이 막");
        Stop(1, "Chain Reaction|Shadow Mastery|Draw Mastery|Mastery bonus|Killing Spree|Falcon Dive");
        Stop(2, "連鎖反応|鷹の急襲|隙間|休息|一息|息子");
        Stop(3, "连锁反应|猎鹰突袭|连杀");
    }

    static void Add(string id, string ko, string en, string ja, string zh, string dKo, string dEn, string dJa, string dZh)
    {
        Term t = new Term { id = id, def = new[] { dKo, dEn, dJa, dZh } };
        t.names[0] = ko.Split('|');
        t.names[1] = en.Split('|');
        t.names[2] = ja.Split('|');
        t.names[3] = zh.Split('|');
        terms.Add(t);
    }

    static void Stop(int lang, string list) => stops[lang].AddRange(list.Split('|'));

    // 언어별 찾기표: 첫 글자 → 그 글자로 시작하는 표기 (긴 것부터)
    static Dictionary<char, Entry[]> Index(int lang)
    {
        Init();
        if (index[lang] != null) return index[lang];
        List<Entry> all = new List<Entry>();
        for (int i = 0; i < terms.Count; i++)
            foreach (string raw in terms[i].names[lang])
            {
                if (string.IsNullOrEmpty(raw)) continue;
                bool exact = raw[0] == '=', strict = raw[0] == '^';
                string s = exact || strict ? raw.Substring(1) : raw;
                if (s.Length == 0) continue;
                all.Add(new Entry { s = s, term = i, strict = strict });
                // 영어: 첫 글자 대 · 소문자 둘 다 (문장 첫머리 · 이름표 Title Case)
                if (lang == 1 && !exact && char.IsLetter(s[0]))
                {
                    char f = char.IsUpper(s[0]) ? char.ToLowerInvariant(s[0]) : char.ToUpperInvariant(s[0]);
                    all.Add(new Entry { s = f + s.Substring(1), term = i, strict = strict });
                }
            }
        foreach (string s in stops[lang])
            if (!string.IsNullOrEmpty(s)) all.Add(new Entry { s = s, term = -1 });
        all.Sort((a, b) => b.s.Length.CompareTo(a.s.Length));
        Dictionary<char, List<Entry>> by = new Dictionary<char, List<Entry>>();
        foreach (Entry e in all)
        {
            if (!by.TryGetValue(e.s[0], out List<Entry> l)) by[e.s[0]] = l = new List<Entry>();
            l.Add(e);
        }
        Dictionary<char, Entry[]> map = new Dictionary<char, Entry[]>(by.Count);
        foreach (KeyValuePair<char, List<Entry>> kv in by) map[kv.Key] = kv.Value.ToArray();
        return index[lang] = map;
    }

    // ================================================================= 찾기 · 감싸기
    // 덧칠 색: 본문(Parch)보다 살짝 금빛 — 강조 금빛(Gold)과 겹치지 않게 은은히 (이미 <color> 안이면 그 색 그대로 밑줄만)
    const string TintOpen = "<color=#EBD9A8>";
    const string LinkPrefix = "term:";
    static readonly StringBuilder sb = new StringBuilder(512);
    static readonly StringBuilder notes = new StringBuilder(256);
    static readonly List<int> hits = new List<int>(8);
    static bool[] used;

    static int Lang => (int)Loc.Current;

    // 글 속 용어를 링크로 감싼 글 (용어가 없으면 원래 글 그대로 = 할당 없음) · 한 글에서 같은 용어는 처음 한 번만
    public static string Link(string text) => Scan(text, Lang, true, null);

    static string Scan(string text, int lang, bool wrap, List<int> found)
    {
        if (string.IsNullOrEmpty(text)) return text;
        Dictionary<char, Entry[]> idx = Index(lang);
        if (used == null || used.Length != terms.Count) used = new bool[terms.Count];
        else System.Array.Clear(used, 0, used.Length);
        if (wrap) sb.Clear();
        int linkDepth = 0, colorDepth = 0, n = text.Length;
        bool any = false;
        for (int i = 0; i < n;)
        {
            char c = text[i];
            // 서식 태그 · 자리표시자 · [키] 는 그대로 옮김
            int skip = Skip(text, i, ref linkDepth, ref colorDepth);
            if (skip > 0)
            {
                if (wrap) sb.Append(text, i, skip);
                i += skip;
                continue;
            }
            if (linkDepth == 0 && idx.TryGetValue(c, out Entry[] list))
            {
                int m = Match(text, i, list, lang);
                if (m >= 0)
                {
                    Entry e = list[m];
                    int len = e.s.Length;
                    if (e.term >= 0 && !used[e.term])
                    {
                        used[e.term] = true;
                        any = true;
                        found?.Add(e.term);
                        if (wrap)
                        {
                            sb.Append("<link=\"").Append(LinkPrefix).Append(terms[e.term].id).Append("\">");
                            if (colorDepth == 0) sb.Append(TintOpen);
                            sb.Append("<u>").Append(text, i, len).Append("</u>");
                            if (colorDepth == 0) sb.Append("</color>");
                            sb.Append("</link>");
                        }
                    }
                    else if (wrap) sb.Append(text, i, len);
                    i += len;
                    continue;
                }
            }
            if (wrap) sb.Append(c);
            i++;
        }
        return wrap && any ? sb.ToString() : text;
    }

    // 건너뛸 길이 (0 = 보통 글자): <태그> · {자리표시자} · [키 이름]
    static int Skip(string t, int i, ref int linkDepth, ref int colorDepth)
    {
        char c = t[i];
        if (c == '<' && i + 1 < t.Length && (char.IsLetter(t[i + 1]) || t[i + 1] == '/' || t[i + 1] == '#'))
        {
            int e = t.IndexOf('>', i + 1);
            if (e < 0 || e - i > 80) return 0;
            if (Starts(t, i, "<link")) linkDepth++;
            else if (Starts(t, i, "</link")) linkDepth = linkDepth > 0 ? linkDepth - 1 : 0;
            else if (Starts(t, i, "<color") || Starts(t, i, "<#")) colorDepth++;
            else if (Starts(t, i, "</color")) colorDepth = colorDepth > 0 ? colorDepth - 1 : 0;
            return e - i + 1;
        }
        if (c == '{' || c == '[')
        {
            int e = t.IndexOf(c == '{' ? '}' : ']', i + 1);
            if (e > i && e - i <= 24) return e - i + 1;
        }
        return 0;
    }

    static bool Starts(string t, int i, string p) => i + p.Length <= t.Length && string.CompareOrdinal(t, i, p, 0, p.Length) == 0;

    static int Match(string t, int i, Entry[] list, int lang)
    {
        for (int k = 0; k < list.Length; k++)
        {
            string s = list[k].s;
            if (i + s.Length > t.Length || string.CompareOrdinal(t, i, s, 0, s.Length) != 0) continue;
            if (list[k].term >= 0 && !Bounded(t, i, s.Length, lang, list[k].strict)) continue;
            return k;
        }
        return -1;
    }

    // 낱말 경계: 영어는 늘 (앞뒤가 글자면 안 됨) · 그 밖은 '^' 표기만 (한국어는 뒤에 조사만 허용)
    const string Particles = "이가은는을를의에도만과와로으";

    static bool Bounded(string t, int i, int len, int lang, bool strict)
    {
        int end = i + len;
        if (lang == 1)
            return (i == 0 || !char.IsLetter(t[i - 1])) && (end >= t.Length || !char.IsLetter(t[end]));
        if (!strict) return true;
        if (i > 0 && Word(t[i - 1])) return false;
        if (end < t.Length && Word(t[end]))
        {
            if (lang == 0 && Particles.IndexOf(t[end]) >= 0) return true;
            if (lang == 2 && t[end] >= '぀' && t[end] <= 'ゟ') return true;      // 일본어: 뒤 히라가나(조사 · 어미)는 허용
            return false;
        }
        return true;
    }

    // 한글 음절 · 한자 · 가나 (낱말에 붙은 글자)
    static bool Word(char c) => (c >= '가' && c <= '힣') || (c >= '一' && c <= '鿿') || (c >= '぀' && c <= 'ヿ');

    // ================================================================= 정의
    // 링크 id(「term:ult」) → 용어 번호 (-1 = 용어 링크 아님) · 부분 문자열을 만들지 않음
    public static int FromLinkId(string linkId)
    {
        Init();
        if (linkId == null || !linkId.StartsWith(LinkPrefix, System.StringComparison.Ordinal)) return -1;
        int len = linkId.Length - LinkPrefix.Length;
        for (int i = 0; i < terms.Count; i++)
            if (terms[i].id.Length == len && string.CompareOrdinal(linkId, LinkPrefix.Length, terms[i].id, 0, len) == 0) return i;
        return -1;
    }

    // 지금 언어의 이름 (첫 표기)
    public static string Name(int term)
    {
        string s = terms[term].names[Lang][0];
        return s.Length > 0 && (s[0] == '=' || s[0] == '^') ? s.Substring(1) : s;
    }

    public static string Def(int term) => KeyBindings.Apply(terms[term].def[Lang]);

    // ================================================================= 툴팁 덧붙임
    // 본문에 나온 용어(나온 차례 · 최대 3개)의 정의를 아래에 작게 · 같은 본문이면 지난 결과를 그대로 (멈춘 화면 · 매 프레임 Show 도 할당 없음)
    const int MaxNotes = 3;
    static string noteIn, noteTitle, noteOut;
    static int noteLang = -1;

    public static string WithNotes(string title, string body)
    {
        if (string.IsNullOrEmpty(body)) return body;
        int lang = Lang;
        if (lang == noteLang && body == noteIn && title == noteTitle) return noteOut;
        hits.Clear();
        Scan(body, lang, false, hits);
        notes.Clear();
        int shown = 0;
        for (int k = 0; k < hits.Count && shown < MaxNotes; k++)
        {
            string name = Name(hits[k]);
            if (title == name) continue;            // 제목이 그 용어면 본문이 곧 설명
            notes.Append(shown == 0 ? "\n<size=80%><color=#A89C86>" : "\n").Append("· ").Append(name).Append(": ").Append(Def(hits[k]));
            shown++;
        }
        if (shown > 0) notes.Append("</color></size>");
        noteLang = lang;
        noteIn = body;
        noteTitle = title;
        noteOut = shown > 0 ? body + notes.ToString() : body;
        return noteOut;
    }
}
