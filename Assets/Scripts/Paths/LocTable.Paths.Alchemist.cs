// 번역표: 연금술사 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣은 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   + 31번 새 카드 「서리 장막」의 이름 · 설명 (수치 이름 「받는 피해」는 공용 표에 있음) + 갈래 효과의 피해 출처 이름(결과 화면 · 판 기록)
//   (Paths/Paths.Alchemist.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
public static partial class LocTable
{
    static void AddPathsAlchemist()
    {
        // 1.4.8~ 입문 카드 · 공명 문구 줄이기 (docs/review/2026-10-21-UI-정리-기획.md 3절): 키는 그대로 · PK 로 화면 문구만
        // ================================================================= 늪
        PA("늪", "Swamp", "沼", "沼泽");
        PK("모든 플라스크가 웅덩이 · 지역", "웅덩이 · 지역", "Puddles · Area", "水たまり・範囲", "水洼 · 区域");
        PA("늪지기", "Swampkeeper", "沼守り", "沼泽看守");
        PA("모든 플라스크가 작은 산성 웅덩이를 남깁니다. (2초)", "Every flask leaves a small acid puddle. (2s)", "すべてのフラスコが小さな酸の水たまりを残します。（2秒）", "所有烧瓶都会留下小小的酸液水洼。（2秒）");
        PA("폭발 피해가 30% 줄어듭니다.", "Blast damage is reduced by 30%.", "爆発ダメージが30%減ります。", "爆炸伤害降低30%。");
        PA("지역 · 생존", "Area · Survival", "範囲・生存", "区域 · 生存");
        PA("이동 보스", "Moving bosses", "動き回るボス", "移动的首领");
        PA("웅덩이 위의 적은 받는 피해가 15% 늘어납니다.", "Enemies standing in puddles take 15% more damage.", "水たまりの上の敵は受けるダメージが15%増えます。", "水洼上的敌人受到的伤害提升15%。");
        PA("웅덩이끼리 겹치면 합쳐져 커집니다.", "Overlapping puddles merge into a bigger one.", "重なった水たまりは合わさって大きくなります。", "重叠的水洼会合并变大。");
        PA("웅덩이가 2배 오래 남습니다.", "Puddles last twice as long.", "水たまりが2倍長く残ります。", "水洼持续时间翻倍。");

        // ================================================================= 빙결
        PA("빙결", "Frost", "氷結", "冰冻");
        PA("얼리고 산산조각 · 제어", "Freeze and shatter · Control", "凍らせて粉砕・制御", "冰冻并粉碎 · 控制");
        PA("빙결 전문", "Frost Specialist", "氷結の専門家", "冰冻专家");
        PK("빙결 시약이 두 번에 한 번 나와 적을 얼리고, 얼어붙은 적을 플라스크로 맞히면 산산조각 납니다. (체력 8% · 보스는 체력 2%까지)",
           "두 번에 한 번 빙결 시약이 나옵니다. 언 적을 플라스크로 맞히면 산산조각 납니다. (체력 8% · 보스는 2%까지)",
           "Every other reagent is Frost. Flasks shatter frozen enemies. (8% max HP · bosses up to 2%)",
           "2回に1回は氷結の試薬です。凍った敵にフラスコを当てると粉砕します。（体力の8% · ボスは2%まで）",
           "每两次有一次是冰冻试剂。用烧瓶击中冻结的敌人会将其粉碎。（生命值8% · 首领至多2%）");
        PA("화염 피해가 40% 줄어듭니다.", "Fire damage is reduced by 40%.", "火炎ダメージが40%減ります。", "火焰伤害降低40%。");
        PA("제어 · 생존 · 보스 약화", "Control · Survival · Weakening bosses", "制御・生存・ボス弱体化", "控制 · 生存 · 削弱首领");
        PA("직접 화력", "Raw damage", "直接火力", "直接火力");
        PA("얼린 적 둘레의 적이 느려집니다.", "Enemies around frozen ones are slowed.", "凍らせた敵の周りの敵が遅くなります。", "被冻结敌人周围的敌人会减速。");
        PA("산산조각이 얼음 파편 3개를 흩뿌리고, 얼음 파편이 10% 확률로 다시 얼립니다.",
           "Shattering scatters 3 Ice Shards, and Ice Shards have a 10% chance to freeze again.",
           "粉砕すると氷の破片を3つまき散らし、氷の破片は10%の確率で再び凍らせます。",
           "粉碎时会溅出3枚冰之碎片，冰之碎片有10%概率再次冻结敌人。");
        PA("산산조각 피해가 체력 12%가 됩니다. (보스 3%)", "Shatter damage becomes 12% of max HP. (3% for bosses)", "粉砕のダメージが体力の12%になります。（ボスは3%）", "粉碎伤害变为生命值的12%。（首领为3%）");

        // ================================================================= 연쇄 폭발
        PA("연쇄 폭발", "Chain Blast", "連鎖爆発", "连锁爆炸");
        PA("자폭까지 무기로 · 무리", "Even self-detonation is a weapon · Crowds", "自爆まで武器に・群れ", "连自爆都是武器 · 群敌");
        PA("불안정 연구", "Instability Research", "不安定研究", "不稳定研究");
        PA("불안정도가 2배로 오르고, 불안정 폭발이 1.5배 넓어집니다.", "Instability rises twice as fast, and unstable blasts are 1.5x wider.", "不安定度が2倍の速さで上がり、不安定な爆発が1.5倍広くなります。", "不稳定度上升速度翻倍，不稳定爆炸范围扩大至1.5倍。");
        PA("자폭 피해가 2배가 됩니다.", "Self-detonation damage is doubled.", "自爆のダメージが2倍になります。", "自爆伤害翻倍。");
        PA("무리", "Crowds", "群れ", "群敌");
        PA("자해", "Self-harm", "自傷", "自伤");
        PA("불안정 플라스크로 처치할 때마다 불안정도가 10% 내려갑니다. (한 번에 최대 30%)",
           "Each kill with an unstable flask lowers Instability by 10%. (Up to 30% at once)",
           "不安定なフラスコで倒すたびに不安定度が10%下がります。（一度に最大30%）",
           "每用不稳定烧瓶击败一名敌人，不稳定度降低10%。（一次最多30%）");
        PA("자폭하는 순간 0.5초 무적이 되고 거대 폭발이 일어납니다.", "When you self-detonate, you become invincible for 0.5s and set off a huge blast.", "自爆した瞬間に0.5秒無敵になり、巨大な爆発が起きます。", "自爆瞬间获得0.5秒无敌，并引发巨大爆炸。");
        PA("자폭 피해가 없어집니다.", "Self-detonation no longer hurts you.", "自爆のダメージがなくなります。", "自爆不再对自己造成伤害。");

        // ================================================================= 조수
        PA("조수", "Assistants", "助手", "助手");
        PK("처음부터 호문쿨루스 · 자동 전투", "호문쿨루스 · 자동 전투", "Homunculus · Auto combat", "ホムンクルス・自動戦闘", "人造小人 · 自动战斗");
        PA("조수 공방", "Assistant Workshop", "助手工房", "助手工坊");
        PA("처음부터 호문쿨루스가 함께하고, 조수가 내 시약 순서를 따라 던집니다.", "A Homunculus joins you from the start, and assistants throw the same reagent you last threw.", "最初からホムンクルスが一緒にいて、助手が自分の試薬の順番に合わせて投げます。", "开局就有人造小人相伴，助手会按照你的试剂顺序投掷。");
        PA("내 투척 속도가 30% 느려집니다.", "Your own throwing speed is 30% slower.", "自分の投てき速度が30%遅くなります。", "自身投掷速度降低30%。");
        PA("자동 전투 · 기동", "Auto combat · Mobility", "自動戦闘・機動", "自动战斗 · 机动");
        PA("보스 · 화력 분산", "Bosses · Split damage", "ボス・火力分散", "首领 · 火力分散");
        PA("조수가 하나 더 생깁니다. (완성하면 쌍둥이 조수로 바뀜)", "You gain one more assistant. (Replaced by the Twin Assistants on completion)", "助手がもう一体増えます。（完成すると双子の助手に替わります）", "多一名助手。（完成后换成双子助手）");
        PA("조수가 내 착탄점으로 날아가 한 번 더 던집니다.", "An assistant flies to where your flask landed and throws one more.", "助手が自分の着弾点へ飛んでいき、もう一度投げます。", "助手会飞到你的落点再投掷一次。");
        PA("조수가 넷이 됩니다.", "You have four assistants.", "助手が4体になります。", "助手变为四名。");

        // ================================================================= 원소 촉매
        PA("원소 촉매", "Elemental Catalyst", "元素触媒", "元素催化");
        PA("순서 맞춰 붕괴 · 보스", "Collapse in order · Bosses", "順番どおりに崩壊・ボス", "按顺序崩坏 · 首领");
        PA("정밀 조제", "Precise Formula", "精密調合", "精密调配");
        PA("한 적이 화염 → 빙결 → 산성 순서대로 맞으면 원소 붕괴가 일어납니다. (공격력 150%)",
           "When an enemy is hit by Fire → Frost → Acid in that order, it suffers an elemental collapse. (150% attack)",
           "一体の敵が火炎 → 氷結 → 酸の順に当たると元素崩壊が起きます。（攻撃力150%）",
           "敌人依次被火焰 → 冰冻 → 酸性击中时，会引发元素崩坏。（攻击力150%）");
        PA("불안정한 플라스크가 나오지 않습니다.", "Unstable flasks no longer appear.", "不安定なフラスコが出なくなります。", "不再出现不稳定烧瓶。");
        PA("보스", "Bosses", "ボス", "首领");
        PA("무리 · 정밀", "Crowds · Precision", "群れ・精密さ", "群敌 · 精准");
        PA("붕괴한 적은 4초 동안 받는 피해가 15% 늘어납니다.", "Collapsed enemies take 15% more damage for 4s.", "崩壊した敵は4秒間、受けるダメージが15%増えます。", "崩坏的敌人4秒内受到的伤害提升15%。");
        PA("붕괴가 가까운 적 둘에게 원소를 하나씩 묻힙니다.", "A collapse coats the two nearest enemies with one element each.", "崩壊が近くの敵2体に元素を1つずつ付けます。", "崩坏会给附近两名敌人各沾上一种元素。");
        PA("원소 둘만 맞아도 붕괴합니다.", "Just two elements are enough to collapse.", "元素2つだけで崩壊します。", "只需两种元素即可崩坏。");

        // ================================================================= 새 교차
        PA("얼음 늪", "Frozen Bog", "氷の沼", "冰沼");
        PA("웅덩이 위의 얼린 적이 받는 피해가 30% 늘어납니다.", "Frozen enemies standing in puddles take 30% more damage.", "水たまりの上の凍った敵は受けるダメージが30%増えます。", "水洼上被冻结的敌人受到的伤害提升30%。");
        PA("폭탄 조수", "Bomb Assistants", "爆弾助手", "炸弹助手");
        PA("조수의 플라스크가 세 번에 한 번 불안정해져 크게 터집니다.", "Every third assistant flask turns unstable and blows up big.", "助手のフラスコが3回に1回不安定になり、大きく爆発します。", "助手的烧瓶每三次就有一次变得不稳定并猛烈爆炸。");

        // ================================================================= 31번 새 카드
        PA("서리 장막", "Frost Veil", "霜の帳", "霜之帷幕");
        PA("얼어붙은 적 3칸 안에 있으면 받는 피해가 줄어듭니다.", "You take less damage while within 3 tiles of a frozen enemy.", "凍った敵から3マス以内にいると受けるダメージが減ります。", "处于被冻结敌人3格范围内时，受到的伤害降低。");

        // ================================================================= 피해 출처 이름 (결과 화면 · 판 기록)
        PA("늪 웅덩이", "Swamp Puddle", "沼の水たまり", "沼泽水洼");
        PA("산산조각", "Shatter", "粉砕", "粉碎");
    }
}
