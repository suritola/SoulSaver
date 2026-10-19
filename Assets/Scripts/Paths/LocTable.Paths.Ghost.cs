// 번역표: 유령 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   (Paths/Paths.Ghost.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
public static partial class LocTable
{
    static void AddPathsGhost()
    {
        PA("혹한", "Deep Frost", "酷寒", "酷寒");
        PA("냉기 두 배 · 무리 제어", "Double chill · Crowd control", "冷気二倍 · 群れの制御", "双倍寒气 · 控制敌群");
        PA("서리 손", "Frost Hand", "霜の手", "霜之手");
        PA("냉기가 두 배로 쌓이고 손길이 1칸 길어집니다.", "Chill builds up twice as fast, and your touch reaches 1 tile farther.", "冷気が二倍たまり、手が1マス長く伸びます。", "寒气叠加翻倍，冰冷之手延长1格。");
        PA("차가운 손길 피해가 25% 줄어듭니다.", "Cold Touch deals 25% less damage.", "冷たい手のダメージが25%減ります。", "冰冷之手的伤害降低25%。");
        PA("무리 제어", "Crowd control", "群れの制御", "控制敌群");
        PA("보스 (얼지 않음)", "Bosses (won't stay frozen)", "ボス（凍りにくい）", "首领（难以冻结）");
        PA("얼어붙은 적 둘레 2칸에 냉기 오라가 생겨 0.6초마다 냉기 1을 쌓습니다.", "Frozen enemies give off a chill aura that adds 1 chill every 0.6 seconds within 2 tiles.", "凍った敵の周り2マスに冷気のオーラが生まれ、0.6秒ごとに冷気を1ためます。", "被冻结的敌人周围2格出现寒气光环，每0.6秒叠加1层寒气。");
        PA("얼음이 풀릴 때 냉기 파동이 퍼져 둘레 3칸의 적에게 피해(공격력 40%)와 냉기 2를 줍니다.", "When the ice thaws, a cold wave deals damage (40% Attack) and 2 chill to enemies within 3 tiles.", "氷が解けるとき冷気の波が広がり、周り3マスの敵にダメージ（攻撃力40%）と冷気2を与えます。", "冰冻解除时扩散寒气波，对周围3格的敌人造成伤害（攻击力40%）并叠加2层寒气。");
        PA("눈보라의 냉기 안개가 2배 오래 남습니다.", "Blizzard's frost mist lasts twice as long.", "吹雪の冷気の霧が2倍長く残ります。", "暴风雪的寒雾持续时间翻倍。");
        PA("산산조각", "Shatter", "粉砕", "粉碎");
        PA("얼린 적 부수기 · 보스 균열", "Shatter the frozen · Crack bosses", "凍った敵を砕く · ボスに亀裂", "粉碎冻结之敌 · 首领裂痕");
        PA("얼어붙은 적을 손길로 치면 부서집니다. (잡몹 즉사 · 큰 적은 최대 체력 6%) 보스는 냉기 5중첩마다 균열이 터집니다. (공격력 80% · 같은 보스에 2초에 한 번)", "Striking a frozen enemy with your touch shatters it. (Small foes die instantly · large foes lose 6% max HP) Every 5 chill on a boss bursts a crack. (80% Attack · once per 2 seconds per boss)", "凍った敵を手で打つと砕けます。（雑魚は即死 · 大きな敵は最大体力の6%）ボスには冷気5重ごとに亀裂が走ります。（攻撃力80% · 同じボスに2秒に1回）", "用冰冷之手击中冻结的敌人会将其粉碎。（小怪即死 · 大型敌人损失最大生命6%）首领每叠加5层寒气就会爆发裂痕。（攻击力80% · 同一首领每2秒一次）");
        PA("얼어붙는 시간이 50% 줄어듭니다.", "Freezes last 50% shorter.", "凍りつく時間が50%短くなります。", "冻结时间缩短50%。");
        PA("보스 · 처형", "Bosses · Executions", "ボス · 処刑", "首领 · 处决");
        PA("냉기 준비 시간", "Time to build chill", "冷気をためる時間", "积累寒气的时间");
        PA("부서질 때 얼음 파편 4개가 기본으로 튑니다.", "Shattering always sends out 4 ice shards.", "砕けるとき氷の破片が4つ必ず飛び散ります。", "粉碎时必定飞出4块冰之碎片。");
        PA("보스 균열 세 번째마다 큰 균열이 터집니다. (공격력 200% · 둘레 3칸의 적에게도 80%)", "Every third boss crack is a great crack. (200% Attack · 80% to enemies within 3 tiles)", "ボスの亀裂が3回目ごとに大亀裂になります。（攻撃力200% · 周り3マスの敵にも80%）", "每第三次首领裂痕都会成为大裂痕。（攻击力200% · 周围3格的敌人也受80%）");
        PA("냉기 3중첩이면 균열이 터집니다.", "Cracks burst at 3 chill.", "冷気3重で亀裂が走ります。", "叠加3层寒气即爆发裂痕。");
        PA("영혼 군단", "Soul Legion", "魂の軍団", "灵魂军团");
        PA("영혼이 돌진 공격 · 자동 화력", "Souls charge foes · Automatic damage", "魂が突進攻撃 · 自動火力", "灵魂冲锋攻击 · 自动火力");
        PA("영혼 거느림", "Soul Retinue", "魂の従え", "统御灵魂");
        PA("영혼이 1.2초마다 하나씩 가까운 적에게 돌진했다 돌아오고 (공격력 30%), 영혼이 2배로 잘 생깁니다.", "Every 1.2 seconds, one soul charges the nearest enemy and returns (30% Attack), and souls appear twice as often.", "1.2秒ごとに魂が1つずつ近くの敵へ突進して戻り（攻撃力30%）、魂が2倍生まれやすくなります。", "每1.2秒有一个灵魂冲向附近的敌人后返回（攻击力30%），灵魂的出现几率翻倍。");
        PA("영혼이 피해를 막을 확률이 50%가 됩니다.", "Souls only have a 50% chance to block damage.", "魂がダメージを防ぐ確率が50%になります。", "灵魂抵挡伤害的几率变为50%。");
        PA("자동 화력 · 무리", "Automatic damage · Crowds", "自動火力 · 群れ", "自动火力 · 敌群");
        PA("방패 약화", "Weaker shield", "盾が弱まる", "护盾减弱");
        PA("거느릴 수 있는 영혼이 2 늘어납니다. (도깨비불의 피해 증가는 영혼 6까지)", "You can hold 2 more souls. (Will-o'-Wisp's damage bonus counts up to 6 souls)", "従えられる魂が2つ増えます。（鬼火のダメージ増加は魂6つまで）", "可统御的灵魂增加2个。（鬼火的伤害加成最多计算6个灵魂）");
        PA("영혼이 5 이상이면 모든 영혼이 서로 다른 적에게 한꺼번에 돌진합니다.", "With 5 or more souls, all of them charge different enemies at once.", "魂が5つ以上なら、すべての魂がそれぞれ別の敵へ一斉に突進します。", "灵魂达到5个以上时，所有灵魂会同时冲向不同的敌人。");
        PA("영혼 상한이 10이 됩니다.", "Your soul limit becomes 10.", "魂の上限が10になります。", "灵魂上限变为10。");
        PA("사령술", "Necromancy", "死霊術", "死灵术");
        PA("긴 빙의 · 무적 근접 폭딜", "Long possession · Untouchable close-range burst", "長い憑依 · 無敵の接近爆発", "长时间附身 · 无敌近身爆发");
        PK("빙의가 3초에서 4.5초로 늘고, 빙의한 몸이 부딪친 적에게 냉기와 피해(공격력 30%)를 주며, 빙의 게이지가 25% 덜 듭니다.", "빙의가 3초에서 4.5초로 늘고 게이지가 25% 덜 듭니다. 빙의한 몸에 부딪친 적은 냉기와 피해(공격력 30%)를 받습니다.", "Possession lasts 4.5s instead of 3s and costs 25% less gauge. Enemies the possessed body bumps take chill and damage (30% Attack).", "憑依が3秒から4.5秒に延び、ゲージが25%少なく済みます。憑依した体にぶつかった敵は冷気とダメージ（攻撃力30%）を受けます。", "附身从3秒延长至4.5秒，消耗能量减少25%。附身的身体撞到的敌人受到寒气与伤害（攻击力30%）。");
        PA("평타 피해가 20% 줄고, 빙의 중 무적은 처음 2초뿐입니다.", "Basic attacks deal 20% less damage, and you are only invincible for the first 2 seconds of Possession.", "通常攻撃のダメージが20%減り、憑依中の無敵は最初の2秒だけです。", "普通攻击伤害降低20%，附身期间仅前2秒无敌。");
        PA("무적 시간 · 보스 근접 폭딜", "Invincibility · Close-range burst on bosses", "無敵時間 · ボスへの接近爆発", "无敌时间 · 对首领近身爆发");
        PA("빙의 밖 화력", "Damage outside Possession", "憑依していない時の火力", "附身以外的火力");
        PA("빙의 폭발 범위가 1.5배가 되고, 폭발에 휘말린 적(보스 제외)에게 저주가 걸립니다.", "Possession's blast radius becomes 1.5x, and enemies caught in it (except bosses) are cursed.", "憑依の爆発範囲が1.5倍になり、爆発に巻き込まれた敵（ボスを除く）に呪いがかかります。", "附身爆炸范围变为1.5倍，被卷入爆炸的敌人（首领除外）会受到诅咒。");
        PA("터진 몸에서 원혼 셋이 나와 가까운 적을 노립니다. (공격력 50%)", "Three vengeful spirits burst from the body and strike nearby enemies. (50% Attack)", "破裂した体から怨霊が3体出て、近くの敵を狙います。（攻撃力50%）", "爆裂的身体中飞出三个冤魂，袭击附近的敌人。（攻击力50%）");
        PA("터진 뒤 가까운 적에게 한 번 바로 다시 빙의합니다. (3초 · 무적 1.5초 · 폭발 피해 50%)", "After the blast, you immediately possess a nearby enemy once more. (3 seconds · 1.5 seconds invincible · 50% blast damage)", "破裂した後、近くの敵にすぐもう一度憑依します。（3秒 · 無敵1.5秒 · 爆発ダメージ50%）", "爆裂后立即再附身附近的敌人一次。（3秒 · 无敌1.5秒 · 爆炸伤害50%）");
        PA("유체 이탈", "Astral Projection", "幽体離脱", "灵魂出窍");
        PA("움직이면 접촉 무시 · 기동 생존", "Ignore contact while moving · Mobile survival", "動けば接触無視 · 機動生存", "移动时无视接触 · 机动生存");
        PA("움직이는 동안 적의 몸을 통과하고, 몸에 닿는 피해를 받지 않습니다.", "While moving, you pass through enemies and take no contact damage.", "動いている間は敵の体をすり抜け、体に触れるダメージを受けません。", "移动期间可穿过敌人的身体，不受接触伤害。");
        PA("최대 체력 −30%, 몸에 닿는 것이 아닌 피해(투사체 · 패턴)를 30% 더 받습니다.", "Max HP −30%, and you take 30% more damage that isn't from contact (projectiles · patterns).", "最大体力−30%、体に触れる以外のダメージ（投射物 · パターン）を30%多く受けます。", "最大生命−30%，受到的非接触伤害（投射物 · 招式）增加30%。");
        PA("기동 · 접촉 생존", "Mobility · Contact survival", "機動 · 接触に強い", "机动 · 抵御接触");
        PA("탄막 보스", "Bullet-hell bosses", "弾幕ボス", "弹幕首领");
        PA("통과한 적에게 냉기 1을 쌓습니다.", "Enemies you pass through gain 1 chill.", "すり抜けた敵に冷気を1ためます。", "对穿过的敌人叠加1层寒气。");
        PA("폴터가이스트가 내가 통과한 적을 먼저 노립니다.", "Poltergeist targets enemies you passed through first.", "ポルターガイストがすり抜けた敵を先に狙います。", "骚灵会优先攻击你穿过的敌人。");
        PA("멈춰도 0.5초 동안 유지됩니다.", "It lasts 0.5 seconds even after you stop.", "止まっても0.5秒間続きます。", "停下后仍持续0.5秒。");
        PA("서리 군단", "Frost Legion", "霜の軍団", "霜之军团");
        PA("영혼의 돌진이 냉기 2를 쌓습니다.", "Soul charges add 2 chill.", "魂の突進が冷気を2ためます。", "灵魂冲锋叠加2层寒气。");
        PA("안에서 얼리기", "Freeze from Within", "内から凍らせる", "由内冻结");
        PA("빙의 폭발이 둘레의 적을 얼립니다.", "Possession's blast freezes nearby enemies.", "憑依の爆発が周りの敵を凍らせます。", "附身爆炸会冻结周围的敌人。");
        // 판 기록 dealt_by 출처 이름 (Paths.Deal 의 source)
        PA("냉기 파동", "Cold Wave", "冷気の波", "寒气波");
        PA("균열", "Crack", "亀裂", "裂痕");
        PA("영혼 돌진", "Soul Charge", "魂の突進", "灵魂冲锋");
        PA("빙의 충돌", "Possession Bump", "憑依の衝突", "附身冲撞");
    }
}
