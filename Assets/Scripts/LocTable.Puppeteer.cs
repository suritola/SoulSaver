using UnityEngine;

// 번역표 (2.0.0): 새 영웅 인형사 — 이름 · 설명 · 카드 · 고유 스킬 · 진화 · 무기 · 강화 · 패시브 · 이야기 · 전령 · 기억 · 계약 · 보드
// 키 앞글자 m. · me. (hero-puppeteer.md) — 이미 있는 키는 건드리지 않음 (영웅 키 pup)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddPuppeteerLines()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 정체 · 글
        A("인형사",
          "Puppeteer",
          "人形使い",
          "傀儡师");
        A("줄 끝의 연출가",
          "The Director at the String's End",
          "糸の先の演出家",
          "线端的导演");
        A("개발자가 「나중에 바꿀게」 하고 넣어 둔 임시 더미를 줄에 매달아 무대에 세운 사람. 더미는 커서를 따라 떠다니며 휘두르고, 영웅과 더미 사이의 줄은 칼날이 되어 가로지르는 적을 벱니다. 「대사는 제가 할게요. 얘는 아직 없거든요.」",
          "A director who hung the placeholder dummy a developer left behind with an \"I'll change it later\" note on strings and stood it on the stage. The dummy floats after the cursor and swings, and the string between hero and dummy becomes a blade that slices any enemy crossing it. \"I'll do the lines. This one doesn't have any yet.\"",
          "開発者が「後で変えるよ」と置いていった仮のダミーを糸に吊るして舞台に立たせた人。ダミーはカーソルに付いて漂いながら振り回され、主人公とダミーの間の糸は刃となって横切る敵を斬ります。「台詞は私が言います。この子はまだ無いので。」",
          "把开发者留下的“回头再改”的临时假人吊在线上、推上舞台的人。假人跟着光标飘浮挥动，英雄与假人之间的线会变成刀刃，斩断横穿而过的敌人。“台词我来说。这家伙还没有呢。”");
        A("임시 더미",
          "Placeholder Dummy",
          "仮ダミー",
          "临时假人");
        A("더미가 커서 자리에 떠 있다가, 누르면 그 자리에서 팔을 돌려 둘레의 적을 벱니다. 영웅과 더미 사이의 줄은 가로지르는 적을 베고, 더미가 멀수록 줄이 팽팽해져 더 아픕니다",
          "The dummy hovers at the cursor; press to spin its arms on the spot and slash nearby enemies. The string between hero and dummy cuts enemies that cross it, and the farther the dummy, the tauter and more painful the string.",
          "ダミーはカーソルの位置に浮かび、押すとその場で腕を回して周りの敵を斬ります。主人公とダミーの間の糸は横切る敵を斬り、ダミーが遠いほど糸が張って痛くなります。",
          "假人悬浮在光标处，按下后原地挥动手臂，斩击周围的敌人。英雄与假人之间的线会斩断横穿的敌人，假人越远，线绷得越紧、伤害越高。");
        A("자리 바꾸기 · 커튼콜",
          "Swap · Curtain Call",
          "入れ替え・カーテンコール",
          "换位 · 谢幕");
        A("짧게 누르면 필살기 게이지 20%를 써서 영웅과 더미의 자리가 바뀌며 사이의 적을 줄로 가르고, 남은 더미가 잠깐 적을 끕니다. 게이지가 가득할 때 누르고 있으면 커튼콜: 줄에 매달린 더미 넷이 내려와 커서 둘레에서 함께 휘두르고, 박수와 함께 올라가며 크게 벱니다",
          "A short press spends 20% of the ultimate gauge to swap places between hero and dummy, slicing enemies in between with the string while the left-behind dummy briefly draws enemies in. Hold with a full gauge for the Curtain Call: four dummies descend on strings, swing together around the cursor, and rise with applause for a big slash.",
          "短く押すと必殺技ゲージ20%を使って主人公とダミーが入れ替わり、間の敵を糸で斬り裂き、残ったダミーがしばらく敵を引きつけます。ゲージが満タンのときに押し続けるとカーテンコール：糸に吊られたダミー4体が降りてきてカーソルの周りで一緒に振り回され、拍手とともに上がりながら大きく斬ります。",
          "短按消耗20%必杀技槽，英雄与假人互换位置，并用线斩断两者之间的敌人，留下的假人会暂时吸引敌人。槽满时按住则发动谢幕：四个吊线假人落下，在光标周围一同挥动，伴着掌声升起并造成重创。");
        A("더미 공격력",
          "Dummy Damage",
          "ダミー攻撃力",
          "假人攻击力");
        A("줄 — 영웅과 더미를 잇는 칼날입니다. 더미가 멀수록 팽팽해져 더 아프고, 필살기 게이지 눈금 하나(20%)가 자리 바꾸기 한 번입니다. 게이지가 가득 차면 꾹 눌러 커튼콜.",
          "String — the blade linking hero and dummy. The farther the dummy, the tauter and more painful it gets, and one gauge tick (20%) is one swap. When the gauge is full, hold to start the Curtain Call.",
          "糸 — 主人公とダミーをつなぐ刃です。ダミーが遠いほど張って痛くなり、必殺技ゲージの目盛り1つ（20%）が入れ替え1回です。ゲージが満タンになったら押し続けてカーテンコール。",
          "线——连接英雄与假人的刀刃。假人越远线越紧、伤害越高，必杀技槽的一格（20%）即换位一次。槽满后按住发动谢幕。");
        A("바꾸기",
          "Swaps",
          "入れ替え",
          "换位");
        K("sec.hint.11", "보지 않을 때만 다가오는 것이 있다.",
          "There is something that only comes closer when you are not looking.",
          "見ていない時だけ近づいてくるものがある。",
          "有一样东西，只在你不看它的时候靠近。");

        // ================================================================= 전용 레벨업 카드 6
        A("실 자국",
          "Thread Trace",
          "糸の跡",
          "线痕");
        A("줄이 지나간 자리에 실 자국이 잠시 남아, 닿는 적을 한 번 더 벱니다. (줄 베기 피해의 60%)",
          "Where the string passed, a thread trace lingers for a moment and cuts touching enemies once more. (60% of string damage)",
          "糸が通った場所にしばらく糸の跡が残り、触れた敵をもう一度斬ります。（糸の斬撃ダメージの60%）",
          "线划过的地方会短暂留下线痕，再次斩击碰到的敌人。（线斩伤害的60%）");
        A("자국 지속",
          "Trace duration",
          "跡の持続",
          "线痕持续时间");
        A("팔 벌린 인형",
          "Open-Armed Puppet",
          "腕を広げた人形",
          "张臂人偶");
        A("더미가 팔을 크게 벌려 휘두르는 범위가 넓어집니다.",
          "The dummy spreads its arms wide and its swing range grows.",
          "ダミーが腕を大きく広げ、振り回す範囲が広がります。",
          "假人张开双臂，挥动范围变大。");
        A("휘두름 반지름",
          "Swing radius",
          "振り回しの半径",
          "挥动半径");
        A("끊어진 실",
          "Severed Thread",
          "切れた糸",
          "断线");
        A("줄로 쓰러뜨린 적에게서 끊긴 실이 튀어 가까운 적을 벱니다.",
          "A severed thread springs from an enemy felled by the string and cuts a nearby enemy.",
          "糸で倒した敵から切れた糸が跳ね、近くの敵を斬ります。",
          "被线斩倒的敌人身上会崩出断线，斩击附近的敌人。");
        A("튀는 실 피해",
          "Thread damage",
          "跳ねる糸のダメージ",
          "崩线伤害");
        A("빈자리의 인형",
          "Empty-Seat Puppet",
          "空席の人形",
          "空位人偶");
        A("자리 바꾸기로 남은 더미가 더 오래 적을 끌고, 그동안 닿는 적을 0.4초마다 벱니다. (공격력 30%)",
          "The dummy left behind by a swap draws enemies for longer and cuts touching enemies every 0.4s. (30% attack)",
          "入れ替えで残ったダミーがより長く敵を引きつけ、その間に触れた敵を0.4秒ごとに斬ります。（攻撃力30%）",
          "换位后留下的假人吸引敌人的时间更长，期间每0.4秒斩击碰到的敌人。（攻击力30%）");
        A("끄는 시간",
          "Lure time",
          "引きつける時間",
          "吸引时间");
        A("짧은 막간",
          "Short Interlude",
          "短い幕間",
          "短暂幕间");
        A("자리 바꾸기에 드는 필살기 게이지가 줄어듭니다.",
          "Swapping costs less ultimate gauge.",
          "入れ替えに使う必殺技ゲージが減ります。",
          "换位消耗的必杀技槽减少。");
        A("게이지 비용",
          "Gauge cost",
          "ゲージ消費",
          "槽消耗");
        A("관객의 박수",
          "Audience Applause",
          "観客の拍手",
          "观众掌声");
        A("한 번 휘둘러 적 셋 이상을 맞히면 필살기 게이지가 찹니다.",
          "Hitting three or more enemies with one swing charges the ultimate gauge.",
          "一振りで敵3体以上に当てると必殺技ゲージが溜まります。",
          "一次挥动命中三个以上敌人时，必杀技槽会充能。");
        A("게이지",
          "Gauge",
          "ゲージ",
          "必杀技槽");

        // ================================================================= 고유 스킬 10
        A("무대 조명",
          "Stage Spotlight",
          "舞台照明",
          "舞台灯光");
        A("더미가 휘두른 자리에 2초 동안 조명이 남아, 안의 적이 받는 피해가 늘어납니다.",
          "A spotlight stays for 2s where the dummy swung, and enemies inside take more damage.",
          "ダミーが振り回した場所に2秒間照明が残り、中の敵が受けるダメージが増えます。",
          "假人挥动过的地方会留下2秒聚光灯，灯光内的敌人受到的伤害增加。");
        A("받는 피해",
          "Damage taken",
          "受けるダメージ",
          "受到的伤害");
        A("현 울림",
          "String Twang",
          "弦の鳴り",
          "弦鸣");
        A("줄이 0.4초 안에 적 다섯 이상을 베면 줄을 따라 파동이 한 번 퍼집니다. (1초에 한 번)",
          "If the string cuts five or more enemies within 0.4s, a shockwave spreads along it once. (once per second)",
          "糸が0.4秒以内に敵5体以上を斬ると、糸に沿って波動が一度広がります。（1秒に1回）",
          "线在0.4秒内斩中五个以上敌人时，会沿线扩散一次冲击波。（每秒一次）");
        A("파동 피해",
          "Wave damage",
          "波動ダメージ",
          "冲击波伤害");
        A("복화술",
          "Ventriloquism",
          "腹話術",
          "腹语术");
        A("4초마다 더미가 「여기야!」 외쳐 둘레 4칸의 적을 더미 쪽으로 끌어당깁니다. (보스 제외)",
          "Every 4s the dummy shouts \"Over here!\" and pulls enemies within 4 tiles toward it. (bosses excluded)",
          "4秒ごとにダミーが「こっちだよ！」と叫び、周囲4マスの敵をダミーの方へ引き寄せます。（ボス除く）",
          "每4秒假人喊一声“在这儿！”，把周围4格内的敌人拉向自己。（首领除外）");
        A("끄는 거리",
          "Pull distance",
          "引き寄せる距離",
          "拉拽距离");
        A("솜 채우기",
          "Stuffing",
          "綿詰め",
          "填充棉花");
        A("더미의 휘두름으로 적을 쓰러뜨리면 체력을 회복합니다. (0.3초에 한 번)",
          "Defeating an enemy with the dummy's swing restores health. (once per 0.3s)",
          "ダミーの振り回しで敵を倒すと体力を回復します。（0.3秒に1回）",
          "用假人挥动击倒敌人时恢复生命。（每0.3秒一次）");
        A("회복",
          "Recovery",
          "回復",
          "恢复");
        A("줄타기",
          "Tightrope",
          "綱渡り",
          "走钢丝");
        A("자리를 바꾼 뒤 1.5초 동안 이동 속도가 오르고 적에게 부딪혀도 피해가 줄어듭니다. (−30%)",
          "For 1.5s after swapping, move speed rises and damage from enemy contact is reduced. (−30%)",
          "入れ替えた後1.5秒間、移動速度が上がり、敵に当たってもダメージが減ります。（−30%）",
          "换位后1.5秒内移动速度提升，被敌人碰到时受到的伤害降低。（−30%）");
        A("이동 속도",
          "Move speed",
          "移動速度",
          "移动速度");
        A("막 내림",
          "Curtain Drop",
          "幕下ろし",
          "落幕");
        A("자리를 바꿀 때 떠난 자리에 작은 막이 떨어져 둘레의 적 투사체를 지웁니다.",
          "When you swap, a small curtain drops where you left and wipes out enemy projectiles around it.",
          "入れ替えるとき、離れた場所に小さな幕が落ちて周りの敵の飛び道具を消します。",
          "换位时，离开的地方会落下一块小幕，清除周围的敌方投射物。");
        A("지우는 범위",
          "Erase range",
          "消す範囲",
          "清除范围");
        A("앙코르",
          "Encore",
          "アンコール",
          "安可");
        A("커튼콜이 끝나도 더미 하나가 남아 함께 휘두릅니다. (피해 50%)",
          "After the Curtain Call ends, one dummy stays and swings along with you. (50% damage)",
          "カーテンコールが終わってもダミー1体が残って一緒に振り回します。（ダメージ50%）",
          "谢幕结束后仍有一个假人留下，与你一同挥动。（伤害50%）");
        A("남는 시간",
          "Remaining time",
          "残る時間",
          "留存时间");
        A("얽힌 실",
          "Tangled Thread",
          "絡まった糸",
          "缠线");
        A("줄에 베인 적이 1.5초 동안 느려집니다.",
          "Enemies cut by the string are slowed for 1.5s.",
          "糸に斬られた敵が1.5秒間遅くなります。",
          "被线斩中的敌人会减速1.5秒。");
        A("느려짐",
          "Slow",
          "鈍化",
          "减速");
        A("T 자세",
          "T-Pose",
          "Tポーズ",
          "T 字姿势");
        A("더미가 1.5초 동안 휘두르지 않고 쉬면, 다음 휘두름이 크게 돌아 범위가 1.5배가 되고 더 아픕니다.",
          "If the dummy rests for 1.5s without swinging, the next swing spins wide: 1.5x the range and more damage.",
          "ダミーが1.5秒間振り回さずに休むと、次の振り回しが大きく回り、範囲が1.5倍になり痛くなります。",
          "假人停手休息1.5秒后，下一次挥动会转得更大：范围变为1.5倍，伤害更高。");
        A("큰 휘두름 피해",
          "Big swing damage",
          "大振りのダメージ",
          "大挥伤害");
        A("실 퉁기기",
          "String Pluck",
          "糸弾き",
          "拨线");
        A("커서를 줄 길이보다 멀리 끌면 줄이 「팅」 튕기며 줄 전체를 벱니다. (1.5초에 한 번)",
          "Drag the cursor beyond the string's length and the string twangs, cutting along its whole length. (once per 1.5s)",
          "カーソルを糸の長さより遠くへ引くと、糸が「ピン」と弾けて糸全体で斬ります。（1.5秒に1回）",
          "把光标拖到超过线长的位置时，线会“铮”地弹开，沿整条线斩击。（每1.5秒一次）");
        A("퉁기기 피해",
          "Pluck damage",
          "弾きダメージ",
          "拨线伤害");
        A("여기야!",
          "Over here!",
          "こっちだよ！",
          "在这儿！");

        // ================================================================= 스킬 진화 7
        A("하프 현",
          "Harp String",
          "ハープの弦",
          "竖琴弦");
        A("실 자국도 줄과 같은 피해로 베고, 현 울림 파동이 0.3초 뒤 한 번 더 퍼집니다.",
          "Thread traces cut for the same damage as the string, and String Twang's wave spreads once more 0.3s later.",
          "糸の跡も糸と同じダメージで斬り、弦の鳴りの波動が0.3秒後にもう一度広がります。",
          "线痕的斩击伤害与线相同，弦鸣的冲击波会在0.3秒后再扩散一次。");
        A("갑옷 인형",
          "Armored Puppet",
          "鎧の人形",
          "铠甲人偶");
        A("T 자세 쉬는 시간이 1.5초에서 0.8초가 되고, 큰 휘두름이 더미 둘레의 적 투사체를 쳐냅니다.",
          "T-Pose rest time drops from 1.5s to 0.8s, and the big swing bats away enemy projectiles around the dummy.",
          "Tポーズの休む時間が1.5秒から0.8秒になり、大振りがダミーの周りの敵の飛び道具を弾き飛ばします。",
          "T 字姿势的休息时间由1.5秒缩短为0.8秒，大挥会击落假人周围的敌方投射物。");
        A("퀵 체인지",
          "Quick Change",
          "早替え",
          "快速换装");
        A("남은 더미가 끄는 시간이 끝날 때 터지고 (공격력 200%), 막 내림 범위가 두 배가 됩니다.",
          "The left-behind dummy bursts when its lure time ends (200% attack), and Curtain Drop's range doubles.",
          "残ったダミーが引きつける時間の終わりに爆発し（攻撃力200%）、幕下ろしの範囲が2倍になります。",
          "留下的假人在吸引时间结束时爆炸（攻击力200%），落幕的范围翻倍。");
        A("극단",
          "Troupe",
          "劇団",
          "剧团");
        A("커튼콜의 더미가 넷에서 여섯이 되고, 앙코르로 남는 더미가 둘이 됩니다.",
          "The Curtain Call dummies grow from four to six, and two dummies remain from Encore.",
          "カーテンコールのダミーが4体から6体になり、アンコールで残るダミーが2体になります。",
          "谢幕的假人由四个增加到六个，安可留下的假人变为两个。");
        A("천 가닥의 실",
          "Thousand Threads",
          "千本の糸",
          "千缕丝线");
        A("꼭두각시가 풀릴 때 실이 가까운 적 둘에게 옮겨 가 실 2를 쌓고, 끊어진 실이 두 번 튑니다.",
          "When a puppet is released, its threads jump to the two nearest enemies and add 2 threads each, and Severed Thread springs twice.",
          "操り人形が解けるとき、糸が近くの敵2体に移って糸を2ずつ積み、切れた糸が2回跳ねます。",
          "傀儡解除时，丝线会转移到最近的两名敌人身上各叠加2根，断线会崩出两次。");
        A("가르는 퇴장",
          "Cutting Exit",
          "斬り裂く退場",
          "斩断退场");
        A("자리를 바꾸는 순간 실 퉁기기가 저절로 터지고, 바꿀 때 가르는 피해가 두 배가 됩니다.",
          "String Pluck fires by itself the moment you swap, and the swap's cutting damage is doubled.",
          "入れ替える瞬間に糸弾きが自動で発動し、入れ替えで斬り裂くダメージが2倍になります。",
          "换位的瞬间会自动触发拨线，换位斩断的伤害翻倍。");
        A("인형 호위대",
          "Puppet Guard",
          "人形の護衛隊",
          "人偶护卫队");
        A("복화술로 끌려온 적이 1초 동안 꼭두각시처럼 영웅을 치지 않고, 그 적을 쓰러뜨리면 솜 채우기 회복이 두 배가 됩니다.",
          "Enemies pulled by Ventriloquism cannot hit you for 1s, like puppets, and defeating them doubles Stuffing's recovery.",
          "腹話術で引き寄せられた敵は操り人形のように1秒間主人公を攻撃せず、その敵を倒すと綿詰めの回復が2倍になります。",
          "被腹语术拉来的敌人像傀儡一样1秒内无法攻击英雄，击败它们后填充棉花的恢复量翻倍。");

        // ================================================================= 특수 능력 (형태 · 패시브) · 궁극기 · 강화
        K("더미의 팔이 가위가 되어, 휘두를 때 더미 바로 아래(0.8칸 안)의 적 하나에게 두 배 피해를 줍니다.\n궁극기: 싹둑", "더미의 팔이 가위가 되어, 휘두를 때 더미 바로 아래(0.8칸 안)의 적 하나에게 두 배 피해를 줍니다.\n궁극기: 싹둑",
          "The dummy's arms become scissors; when swinging, one enemy right below it (within 0.8 tiles) takes double damage.\nUltimate: Snip-Snip",
          "ダミーの腕がハサミになり、振り回すときにダミーの真下（0.8マス以内）の敵1体に2倍のダメージを与えます。\n究極技：チョキン",
          "假人的手臂变成剪刀，挥动时对假人正下方（0.8格内）的一个敌人造成双倍伤害。\n终极技：咔嚓");
        A("더미의 팔이 가위가 되어, 휘두를 때 더미 바로 아래(0.8칸 안)의 적 하나에게 두 배 피해를 줍니다.",
          "The dummy's arms become scissors; when swinging, one enemy right below it (within 0.8 tiles) takes double damage.",
          "ダミーの腕がハサミになり、振り回すときにダミーの真下（0.8マス以内）の敵1体に2倍のダメージを与えます。",
          "假人的手臂变成剪刀，挥动时对假人正下方（0.8格内）的一个敌人造成双倍伤害。");
        K("더미가 실감개가 되어, 줄에 처음 베인 적을 0.6초 동안 묶습니다. (보스 제외 · 같은 적 3초에 한 번)\n궁극기: 실타래", "더미가 실감개가 되어, 줄에 처음 베인 적을 0.6초 동안 묶습니다. (보스 제외 · 같은 적 3초에 한 번)\n궁극기: 실타래",
          "The dummy becomes a spool and binds an enemy first cut by the string for 0.6s. (bosses excluded · once per 3s per enemy)\nUltimate: Skein",
          "ダミーが糸巻きになり、糸に最初に斬られた敵を0.6秒間縛ります。（ボス除く・同じ敵に3秒に1回）\n究極技：糸玉",
          "假人变成线轴，把第一次被线斩中的敌人束缚0.6秒。（首领除外 · 同一敌人每3秒一次）\n终极技：线团");
        A("더미가 실감개가 되어, 줄에 처음 베인 적을 0.6초 동안 묶습니다. (보스 제외 · 같은 적 3초에 한 번)",
          "The dummy becomes a spool and binds an enemy first cut by the string for 0.6s. (bosses excluded · once per 3s per enemy)",
          "ダミーが糸巻きになり、糸に最初に斬られた敵を0.6秒間縛ります。（ボス除く・同じ敵に3秒に1回）",
          "假人变成线轴，把第一次被线斩中的敌人束缚0.6秒。（首领除外 · 同一敌人每3秒一次）");
        K("좌클릭을 떼고 있으면 더미가 0.6초마다 저절로 휘두릅니다. (피해 60%)\n궁극기: 행진", "좌클릭을 떼고 있으면 더미가 0.6초마다 저절로 휘두릅니다. (피해 60%)\n궁극기: 행진",
          "While the attack button is released, the dummy swings by itself every 0.6s. (60% damage)\nUltimate: March",
          "左クリックを離している間、ダミーが0.6秒ごとに自動で振り回します。（ダメージ60%）\n究極技：行進",
          "松开左键时，假人每0.6秒自动挥动一次。（伤害60%）\n终极技：行军");
        A("좌클릭을 떼고 있으면 더미가 0.6초마다 저절로 휘두릅니다. (피해 60%)",
          "While the attack button is released, the dummy swings by itself every 0.6s. (60% damage)",
          "左クリックを離している間、ダミーが0.6秒ごとに自動で振り回します。（ダメージ60%）",
          "松开左键时，假人每0.6秒自动挥动一次。（伤害60%）");
        K("자리 바꾸기로 적을 가를 때마다 필살기 게이지가 1% 찹니다. (한 번에 최대 3%)\n진화: 1.5% (최대 5%)", "자리 바꾸기로 적을 가를 때마다 필살기 게이지가 1% 찹니다. (한 번에 최대 3%)\n진화: 1.5% (최대 5%)",
          "Each enemy cut by a swap charges 1% of the ultimate gauge. (max 3% per swap)\nEvolved: 1.5% (max 5%)",
          "入れ替えで敵を斬り裂くたびに必殺技ゲージが1%溜まります。（1回の入れ替えで最大3%）\n進化：1.5%（最大5%）",
          "换位每斩断一个敌人，必杀技槽充能1%。（单次换位最多3%）\n进化：1.5%（最多5%）");
        K("최대 줄 길이가 1칸 늘어납니다.\n진화: 2칸", "최대 줄 길이가 1칸 늘어납니다.\n진화: 2칸",
          "Maximum string length increases by 1 tile.\nEvolved: 2 tiles",
          "糸の最大の長さが1マス伸びます。\n進化：2マス",
          "最大线长增加1格。\n进化：2格");
        K("둘레 6칸에 적이 열 넘게 있으면 주는 피해가 10% 늘어납니다.\n진화: +18%", "둘레 6칸에 적이 열 넘게 있으면 주는 피해가 10% 늘어납니다.\n진화: +18%",
          "With more than ten enemies within 6 tiles, damage dealt rises by 10%.\nEvolved: +18%",
          "周囲6マスに敵が10体を超えると、与えるダメージが10%増えます。\n進化：+18%",
          "周围6格内敌人超过十个时，造成的伤害提升10%。\n进化：+18%");
        K("더미가 6칸보다 멀리 있는 동안 받는 피해가 10% 줄어듭니다.\n진화: 18%", "더미가 6칸보다 멀리 있는 동안 받는 피해가 10% 줄어듭니다.\n진화: 18%",
          "While the dummy is more than 6 tiles away, damage taken is reduced by 10%.\nEvolved: 18%",
          "ダミーが6マスより遠くにいる間、受けるダメージが10%減ります。\n進化：18%",
          "假人距离超过6格时，受到的伤害降低10%。\n进化：18%");
        A("가위 손 인형",
          "Scissor-Hand Puppet",
          "ハサミの手の人形",
          "剪刀手人偶");
        A("실감개 인형",
          "Spool Puppet",
          "糸巻きの人形",
          "线轴人偶");
        A("태엽 병정",
          "Windup Soldier",
          "ゼンマイ兵",
          "发条士兵");
        A("무대 체질",
          "Born for the Stage",
          "舞台向きの体質",
          "舞台体质");
        A("긴 팔",
          "Long Arms",
          "長い腕",
          "长臂");
        A("만원 관객",
          "Packed House",
          "満員の観客",
          "满座观众");
        A("텅 빈 몸",
          "Hollow Body",
          "空っぽの体",
          "空心之躯");
        A("싹둑",
          "Snip-Snip",
          "チョキン",
          "咔嚓");
        A("커튼콜이 끝날 때 커서 자리에 거대한 가위가 닫힙니다. (공격력 400%)",
          "When the Curtain Call ends, a giant pair of scissors snaps shut at the cursor. (400% attack)",
          "カーテンコールが終わるとき、カーソルの位置に巨大なハサミが閉じます。（攻撃力400%）",
          "谢幕结束时，光标处会合上一把巨大的剪刀。（攻击力400%）");
        A("실타래",
          "Skein",
          "糸玉",
          "线团");
        A("커튼콜 더미끼리 실이 이어져 그물이 되고, 그물 안 적이 느려지고 0.45초마다 베입니다. (공격력 56%씩)",
          "The Curtain Call dummies link up with thread into a net; enemies inside are slowed and cut every 0.45s. (56% attack each)",
          "カーテンコールのダミー同士が糸でつながって網になり、網の中の敵は遅くなり0.45秒ごとに斬られます。（攻撃力56%ずつ）",
          "谢幕的假人之间连成线网，网内敌人减速，并每0.45秒被斩击一次。（每次攻击力56%）");
        A("행진",
          "March",
          "行進",
          "行军");
        A("커튼콜 더미가 커서를 따라 한 줄로 행진하며, 지나간 길을 벱니다. (공격력 56%씩)",
          "The Curtain Call dummies march in a single line toward the cursor, cutting the path they pass. (56% attack each)",
          "カーテンコールのダミーがカーソルに沿って一列に行進し、通った道を斬ります。（攻撃力56%ずつ）",
          "谢幕的假人排成一列跟随光标行军，斩击经过的路径。（每次攻击力56%）");
        A("대역 배우",
          "Understudy",
          "代役俳優",
          "替身演员");
        A("자리를 바꾸면 남은 더미가 0.5초 뒤 터집니다. (공격력 150%)",
          "After a swap, the left-behind dummy bursts 0.5s later. (150% attack)",
          "入れ替えると、残ったダミーが0.5秒後に爆発します。（攻撃力150%）",
          "换位后，留下的假人会在0.5秒后爆炸。（攻击力150%）");
        A("두 번째 실",
          "Second String",
          "二本目の糸",
          "第二根线");
        A("줄이 한 가닥 더 생깁니다. (영웅 쪽에서 0.6칸 옆으로 갈라짐 · 피해 60%)",
          "A second string appears. (splits 0.6 tiles to the side from the hero · 60% damage)",
          "糸がもう1本増えます。（主人公側から0.6マス横に分かれる・ダメージ60%）",
          "多出一根线。（从英雄一侧向旁边分出0.6格 · 伤害60%）");
        A("무거운 머리",
          "Heavy Head",
          "重い頭",
          "沉重的脑袋");
        A("휘두름 범위 +30% · 피해 +20%, 대신 더미가 따라오는 속도가 20% 느려집니다.",
          "Swing range +30% and damage +20%, but the dummy follows 20% slower.",
          "振り回しの範囲+30%・ダメージ+20%、代わりにダミーが付いてくる速度が20%遅くなります。",
          "挥动范围 +30%、伤害 +20%，但假人的跟随速度降低20%。");

        // ================================================================= 이야기
        A("무대 뒤 창고에서, 「나중에 바꿀게」라는 쪽지가 붙은 더미 하나가 일어섰다.",
          "In a backstage storeroom, a dummy with a note reading \"I'll change it later\" stood up.",
          "舞台裏の倉庫で、「後で変えるよ」というメモが貼られたダミーが一体立ち上がった。",
          "后台仓库里，一个贴着“回头再改”便条的假人站了起来。");
        A("아니, 일어선 건 줄이었다. 줄 끝에는 키 큰 연출가가 서 있었다.",
          "No — it was the string that stood. At its end stood a tall director.",
          "いや、立ち上がったのは糸だった。糸の先には背の高い演出家が立っていた。",
          "不，站起来的是线。线的另一端站着一位高个子的导演。");
        A("“대사는 제가 할게요. 얘는 아직 없거든요.” 연출가는 더미를 앞세워 초원으로 걸어 나갔다.",
          "\"I'll do the lines. This one doesn't have any yet.\" The director walked out onto the meadow with the dummy leading the way.",
          "「台詞は私が言います。この子はまだ無いので。」演出家はダミーを先に立てて草原へ歩き出した。",
          "“台词我来说。这家伙还没有呢。”导演让假人走在前面，朝草原走去。");
        A("관객이 모두 해골인 무대였다. 연출가는 줄을 고쳐 잡고 객석을 향해 허리를 숙였다.",
          "It was a stage where every spectator was a skeleton. The director tightened his grip on the strings and bowed to the audience.",
          "観客がすべて骸骨の舞台だった。演出家は糸を握り直し、客席に向かって腰を折った。",
          "这是一座观众全是骷髅的舞台。导演重新握紧丝线，向观众席鞠了一躬。");
        A("“(더미가) 저 큰 분이 주인공인가요? — 아니, 오늘 주인공은 너야.”",
          "\"(The dummy) Is that big one the lead? — No, tonight the lead is you.\"",
          "「（ダミーが）あの大きな方が主役ですか？ — いや、今夜の主役は君だ。」",
          "“（假人）那位大个子是主角吗？——不，今晚的主角是你。”");
        A("연출가는 더미의 팔을 벌려 세우고 묘역 한가운데로 걸어 들어갔다.",
          "The director spread the dummy's arms wide and walked into the middle of the graveyard.",
          "演出家はダミーの腕を広げて立たせ、墓地の真ん中へ歩いて入っていった。",
          "导演把假人的手臂张开立好，走进了墓地正中。");
        A("연출가는 더미의 줄을 하나씩 풀었다. 더미는 쓰러지지 않고 서 있었다.",
          "The director untied the dummy's strings one by one. The dummy did not fall; it kept standing.",
          "演出家はダミーの糸を一本ずつほどいた。ダミーは倒れずに立っていた。",
          "导演把假人的线一根根解开。假人没有倒下，依旧站着。");
        A("“나중에 바꿀 필요는 없겠네요.”",
          "\"Looks like there's no need to change it later.\"",
          "「後で変える必要はなさそうですね。」",
          "“看来不用回头再改了。”");
        A("하지만 객석 맨 뒷줄, 붉은 사막 쪽에서 누군가 아직 박수를 치고 있었다...",
          "But in the very back row, toward the red desert, someone was still clapping...",
          "しかし客席の最後列、赤い砂漠の方で、まだ誰かが拍手をしていた...",
          "可是在观众席最后一排、红色沙漠那边，仍有人在鼓掌……");

        // ================================================================= 전령 대사 6줄
        K("herald.hero.pup.1", "줄을 놓지 마라. 네가 놓으면 그것은 다시 아무것도 아니다.",
          "Don't let go of the string. If you let go, it becomes nothing again.",
          "糸を離すな。お前が離せば、それはまた何者でもなくなる。",
          "别放开线。你一放手，它就又变回什么都不是了。");
        K("herald.hero.pup.2", "무기를 멀리 보냈으니 몸이 비었지. 연출가가 무대 한가운데 서면 안 된다.",
          "You sent the weapon far away, so your body was left open. A director must never stand center stage.",
          "武器を遠くへやったから体が空いたのだ。演出家が舞台の真ん中に立ってはならん。",
          "把武器送得那么远，身子就空了。导演可不该站在舞台正中央。");
        K("herald.hero.pup.3", "더미에게 이름을 지어 주지 않았나. 이름이 없는 것은 오래 버티지 못한다.",
          "You never gave the dummy a name, did you? Things without names do not last long.",
          "ダミーに名前をつけてやらなかったのか。名のないものは長くもたん。",
          "你没给假人起名字吧。没有名字的东西撑不了多久。");
        K("herald.hero.pup.4", "리치 왕도 줄을 쓴다. 다만 그의 줄 끝에는 산 것이 매달린 적이 없다.",
          "The Lich King uses strings too. But nothing living has ever hung at the end of his.",
          "リッチキングも糸を使う。ただ、その糸の先に生きた者が吊られたことはない。",
          "巫妖王也用线。只不过他的线端从来没吊过活物。");
        K("herald.hero.pup.5", "등 뒤를 조심하라고 했는데.",
          "I told you to watch your back.",
          "背後に気をつけろと言ったはずだが。",
          "我不是让你当心背后吗。");
        K("herald.hero.pup.6", "막이 내리면 관객은 집에 간다. 너는 어디로 가지?",
          "When the curtain falls, the audience goes home. Where will you go?",
          "幕が下りれば観客は家に帰る。お前はどこへ行く？",
          "幕一落，观众就回家了。你要去哪儿？");
        K("bossmem.noun.puppeteer", "줄",
          "strings",
          "糸",
          "丝线");

        // ================================================================= 기억 조각 6
        K("memory.pup.1.title", "임시 더미",
          "Placeholder Dummy",
          "仮ダミー",
          "临时假人");
        K("memory.pup.1.text", "개발자가 「나중에 바꿀게」 하고 세워 둔 더미가 있었다.\n쪽지는 바래고, 더미는 창고에서 T 자세로 몇 해를 섰다. 줄을 건 사람이 누구인지는 더미도 모른다.\n처음 쓰러진 날 전령이 물었다. 줄이 끊긴 것이냐, 네가 놓은 것이냐.\n“놓은 적은 없어요.”",
          "A dummy stood there, put up by a developer who said \"I'll change it later.\"\nThe note faded, and the dummy stood in a T-pose in the storeroom for years. Even the dummy does not know who hung the strings.\nThe day he first fell, the herald asked: was the string cut, or did you let go?\n\"I never let go.\"",
          "開発者が「後で変えるよ」と立てておいたダミーがあった。\nメモは色あせ、ダミーは倉庫でTポーズのまま何年も立っていた。糸をかけた人が誰なのかは、ダミーも知らない。\n初めて倒れた日、伝令が尋ねた。糸が切れたのか、お前が離したのか。\n「離したことはありません。」",
          "曾有一个假人，是开发者说着“回头再改”立在那儿的。\n便条褪了色，假人在仓库里以 T 字姿势站了好几年。是谁把线挂上去的，连假人自己也不知道。\n他第一次倒下的那天，传令官问：是线断了，还是你松的手？\n“我从没松过手。”");
        K("memory.pup.2.title", "다섯 번의 막",
          "Five Curtains",
          "五度の幕",
          "五次落幕");
        K("memory.pup.2.text", "다섯 번 쓰러지는 동안 더미는 한 번도 넘어지지 않았다.\n연출가는 그것이 더미가 강해서가 아니라 자기가 먼저 넘어져서임을 안다.\n“막이 내려도 얘는 서 있더라고요.”",
          "Through five falls, the dummy never once tumbled.\nThe director knows it was not because the dummy was strong, but because he always fell first.\n\"Even after the curtain fell, it was still standing.\"",
          "五度倒れる間、ダミーは一度も転ばなかった。\n演出家は知っている。それはダミーが強いからではなく、自分が先に倒れるからだと。\n「幕が下りても、この子は立ってるんです。」",
          "倒下五次的这段时间里，假人一次也没摔倒。\n导演明白，那并不是因为假人强，而是因为自己总是先倒下。\n“就算幕落了，它也还站着。”");
        K("memory.pup.3.title", "첫 공연",
          "First Show",
          "初公演",
          "首演");
        K("memory.pup.3.text", "첫 클리어. 객석에는 아무도 없었는데 박수 소리가 났다.\n연출가는 돌아보지 않고 허리를 숙였다.\n“돌아보면 사라질 것 같아서요.”",
          "The first clear. Nobody sat in the audience, yet applause rang out.\nThe director bowed without turning around.\n\"I was afraid it would vanish if I looked back.\"",
          "初めてのクリア。客席には誰もいなかったのに、拍手の音がした。\n演出家は振り返らずに頭を下げた。\n「振り返ったら消えてしまいそうで。」",
          "首次通关。观众席上明明空无一人，却响起了掌声。\n导演没有回头，只是深深鞠了一躬。\n“我怕一回头它就消失了。”");
        K("memory.pup.4.title", "대본 없음",
          "No Script",
          "台本なし",
          "没有剧本");
        K("memory.pup.4.text", "어려움을 이긴 날, 대본이 없다는 걸 들켰다.\n연출가는 즉흥으로 끝까지 했다. 더미는 한 번도 틀린 줄을 말하지 않았다.\n“애초에 대사가 없으니까요.”",
          "The day he beat a harder difficulty, they found out there was no script.\nThe director improvised to the very end. The dummy never spoke a wrong line.\n\"It never had any lines to begin with.\"",
          "難しい難易度に勝った日、台本がないことがばれた。\n演出家は最後まで即興でやり通した。ダミーは一度も間違った台詞を言わなかった。\n「もともと台詞がないですからね。」",
          "战胜高难度的那天，没有剧本的事被识破了。\n导演即兴演到了最后。假人一句错台词也没说。\n“反正它本来就没有台词。”");
        K("memory.pup.5.title", "긴 그림자",
          "Long Shadow",
          "長い影",
          "长长的影子");
        K("memory.pup.5.text", "사막의 더미는 자기를 기다리던 이야기가 아니었다.\n사실은 연출가가 더미를 찾은 게 아니라, 더미가 연출가를 찾아왔다.\n등을 돌릴 때만 한 걸음씩, 아주 조금씩.\n“들키면 멈추는 게 예의잖아요.”",
          "The dummy in the desert was not a story that had been waiting for him.\nThe truth is, the director did not find the dummy; the dummy came looking for the director.\nOne step at a time, only when his back was turned.\n\"It's only polite to stop when you're caught.\"",
          "砂漠のダミーは、彼を待っていた物語ではなかった。\n本当は、演出家がダミーを見つけたのではなく、ダミーが演出家を探しに来たのだ。\n背を向けたときだけ、一歩ずつ、ほんの少しずつ。\n「見つかったら止まるのが礼儀でしょう。」",
          "沙漠里的那个假人，并不是一直在等他的故事。\n其实不是导演找到了假人，而是假人来找导演。\n只在他背过身时，一步一步，一点一点地靠近。\n“被发现了就该停下，这是礼貌。”");
        K("memory.pup.6.title", "줄을 푸는 법",
          "How to Undo a String",
          "糸のほどき方",
          "解线之法");
        K("memory.pup.6.text", "숙련의 끝에서 연출가는 줄을 푸는 법을 배웠다.\n줄을 풀어도 더미는 서 있었다. 더 이상 매달려 있지 않았다.\n“대사는 이제 얘가 할 거예요.”",
          "At the end of mastery, the director learned how to undo the strings.\nEven with the strings undone, the dummy kept standing. It no longer hung from anything.\n\"From now on, it will do the talking.\"",
          "熟練の果てに、演出家は糸のほどき方を学んだ。\n糸をほどいても、ダミーは立っていた。もう何にも吊られていなかった。\n「台詞はこれからこの子が言います。」",
          "在精通的尽头，导演学会了解线。\n线解开后，假人依旧站着，不再吊在任何东西上。\n“从现在起，台词由它来说。”");

        // ================================================================= 영혼 계약 · 보드
        K("contract.h.pup.1.name", "사백 번의 줄",
          "Four Hundred Cuts",
          "四百回の糸",
          "四百次线斩");
        K("contract.h.pup.1.cond", "줄로 400번 베기",
          "Cut 400 times with the string",
          "糸で400回斬る",
          "用线斩击400次");
        K("contract.h.pup.2.name", "열 번의 커튼콜",
          "Ten Curtain Calls",
          "十度のカーテンコール",
          "十次谢幕");
        K("contract.h.pup.2.cond", "필살기 10번 쓰기",
          "Use the ultimate 10 times",
          "必殺技を10回使う",
          "使用必杀技10次");
        K("contract.h.pup.3.name", "한꺼번에 가르기",
          "Cut Them All at Once",
          "まとめて斬り裂き",
          "一并斩断");
        K("contract.h.pup.3.cond", "자리 바꾸기로 적 셋 이상 한꺼번에 가르기 30번",
          "Cut 3 or more enemies at once by swapping, 30 times",
          "入れ替えで敵3体以上を一度に斬り裂く30回",
          "换位一次斩断三个以上敌人，共30次");
        K("contract.h.pup.4.name", "쓸어내기",
          "Sweep",
          "薙ぎ払い",
          "横扫");
        K("contract.h.pup.4.cond", "한 번 쓸기로 적 다섯 이상 베기 8번",
          "Cut 5 or more enemies in one sweep, 8 times",
          "一度の薙ぎで敵5体以上を斬る8回",
          "一次横扫斩中五个以上敌人，共8次");
        K("contract.h.pup.6.name", "육백 번의 줄",
          "Six Hundred Cuts",
          "六百回の糸",
          "六百次线斩");
        K("contract.h.pup.6.cond", "줄로 600번 베기 (누적)",
          "Cut 600 times with the string (total)",
          "糸で600回斬る（累計）",
          "用线斩击600次（累计）");
        K("board.pup.1.name", "천오백 번의 줄",
          "Fifteen Hundred Cuts",
          "千五百回の糸",
          "一千五百次线斩");
        K("board.pup.1.desc", "휘젓고 또 휘저었습니다. 무대는 늘 실밥투성이였습니다.",
          "Swept and swept again. The stage was always full of loose threads.",
          "振って、また振りました。舞台はいつも糸くずだらけでした。",
          "挥了又挥。舞台上总是满地线头。");
        K("board.pup.1.cond", "줄로 1500번 베기",
          "Cut 1500 times with the string",
          "糸で1500回斬る",
          "用线斩击1500次");
        K("board.pup.2.name", "네 왕을 가른 줄",
          "The String That Cut Four Kings",
          "四王を斬った糸",
          "斩断四王的线");
        K("board.pup.2.desc", "왕들은 객석에 앉아 있었습니다. 막은 연출가가 내렸습니다.",
          "The kings sat in the audience. The director brought down the curtain.",
          "王たちは客席に座っていました。幕を下ろしたのは演出家でした。",
          "诸王坐在观众席上。落幕的是导演。");
        K("board.pup.2.cond", "인형사로 장 보스 4종 모두 쓰러뜨리기",
          "Defeat all four chapter bosses as the Puppeteer",
          "人形使いで章のボス4種をすべて倒す",
          "以傀儡师击败全部四种章节首领");
        K("board.pup.3.name", "백오십 번의 퇴장",
          "A Hundred and Fifty Exits",
          "百五十回の退場",
          "一百五十次退场");
        K("board.pup.3.desc", "자리를 바꿀 때마다 객석이 술렁였습니다. 박수는 늘 한 박자 늦었습니다.",
          "Every swap stirred the audience. The applause was always a beat late.",
          "入れ替えるたびに客席がざわめきました。拍手はいつも一拍遅れでした。",
          "每次换位，观众席都会一阵骚动。掌声总是慢一拍。");
        K("board.pup.3.cond", "자리 바꾸기로 적 셋 이상 가르기 150번",
          "Cut 3 or more enemies by swapping, 150 times",
          "入れ替えで敵3体以上を斬り裂く150回",
          "换位斩断三个以上敌人，共150次");

        // ================================================================= 보스 말풍선
        A("나중에 바꿀게!",
          "I'll change it later!",
          "後で変えるよ！",
          "回头再改！");
    }
}
