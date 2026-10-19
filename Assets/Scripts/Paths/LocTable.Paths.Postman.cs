// 번역표: 우체부 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   (Paths/Paths.Postman.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
// 용어: 받는 이 addressee · 宛先 · 收件人 / 속달 Express Delivery · 速達 · 特快专递 / 우표 stamp · 切手 · 邮票 / 소인 postmark · 消印 · 邮戳
public static partial class LocTable
{
    static void AddPathsPostman()
    {
        // ---------------- 순회 배달
        PA("순회 배달", "Delivery Round", "巡回配達", "巡回投递");
        PA("편지가 네 명을 돎 · 무리", "Letters visit four foes · Crowds", "手紙が4体を巡る · 群れ", "信件巡访四人 · 群敌");
        PA("편지가 닿은 뒤 다음으로 가장 먼 적에게 계속 갑니다. (최대 4명 · 15%씩 약해짐)",
           "After a delivery, the letter moves on to the next farthest enemy. (Up to 4 addressees · 15% weaker each time)",
           "手紙が届いた後、次に最も遠い敵へ向かい続けます。（最大4体 · 15%ずつ弱まる）",
           "信件送达后继续飞向下一个最远的敌人。（最多4人 · 每次减弱15%）");
        PA("편지 속도가 15% 느려집니다.", "Letters fly 15% slower.", "手紙の速度が15%遅くなります。", "信件速度降低15%。");
        PA("무리", "Crowds", "群れ", "群敌");
        PA("보스 (약해짐)", "Bosses (decay)", "ボス（弱まる）", "首领（衰减）");
        PA("마지막 받는 이가 쓰러지면 처음 적에게 돌아갑니다.", "If the last addressee falls, the letter returns to the first one.", "最後の宛先が倒れると、最初の敵へ戻ります。", "最后一位收件人倒下时，信件返回第一个敌人。");
        PA("끝까지 다 돌면 마지막 받는 이에 소인이 찍혀 4초 동안 받는 피해가 20% 늘어납니다.",
           "Completing the full round postmarks the last addressee: it takes 20% more damage for 4s.",
           "最後まで巡ると最後の宛先に消印が押され、4秒間受けるダメージが20%増えます。",
           "巡回完整一圈后，最后一位收件人被盖上邮戳，4秒内受到的伤害提高20%。");
        PA("최대 8명까지 돕니다.", "The round reaches up to 8 addressees.", "最大8体まで巡ります。", "最多巡访8人。");

        // ---------------- 무거운 소포
        PA("무거운 소포", "Heavy Parcel", "重い小包", "沉重包裹");
        PA("느린 폭발 소포 · 밀집 무리", "Slow exploding parcels · Packed crowds", "遅い爆発小包 · 密集した群れ", "缓慢爆炸包裹 · 密集群敌");
        PA("편지 대신 느린 소포를 보냅니다. (피해 ×1.8 · 반경 2칸 폭발 · 「소포」 카드가 폭발을 키움)",
           "Send slow parcels instead of letters. (×1.8 damage · 2-tile blast · the \"Parcel\" card enlarges the blast)",
           "手紙の代わりに遅い小包を送ります。（ダメージ×1.8 · 半径2マスの爆発 · 「小包」カードで爆発が大きくなる）",
           "改为寄出缓慢的包裹。（伤害×1.8 · 半径2格爆炸 · 「包裹」卡牌会扩大爆炸）");
        PA("공격 속도가 35% 느려집니다.", "Attack speed is 35% slower.", "攻撃速度が35%遅くなります。", "攻击速度降低35%。");
        PA("밀집 무리", "Packed crowds", "密集した群れ", "密集群敌");
        PA("빠른 적 · 근접", "Fast foes · Close range", "速い敵 · 近接", "快速敌人 · 近身");
        PA("터진 자리 둘레에서 우표 조각 두 개가 작게 터집니다. (소포 피해 30%)",
           "Two stamp shards burst around the blast. (30% of parcel damage)",
           "爆発した場所の周りで切手の破片が2つ小さく弾けます。（小包ダメージの30%）",
           "爆炸处周围有两片邮票碎片小幅爆开。（包裹伤害的30%）");
        PA("세 번째 소포마다 대형 소포가 됩니다. (피해 +25% · 반경 1.4배)",
           "Every third parcel is a big one. (+25% damage · 1.4× radius)",
           "3つ目の小包ごとに大型小包になります。（ダメージ+25% · 半径1.4倍）",
           "每第三个包裹变为大型包裹。（伤害+25% · 半径1.4倍）");
        PA("폭발 반경이 1.5배가 됩니다.", "Blast radius becomes 1.5×.", "爆発半径が1.5倍になります。", "爆炸半径变为1.5倍。");

        // ---------------- 속달
        PA("속달", "Express Delivery", "速達", "特快专递");
        PA("표식 폭발 중심 · 무리 폭딜", "Mark bursts · Crowd burst damage", "印の爆発が中心 · 群れへの瞬間火力", "以标记爆炸为主 · 群体爆发");
        PA("속달 전문", "Express Specialist", "速達専門", "特快专员");
        PA("표식이 2배 빨리 붙고, 속달 게이지가 25% 덜 듭니다.", "Marks attach twice as fast, and Express Delivery costs 25% less gauge.", "印が2倍速く付き、速達のゲージ消費が25%減ります。", "标记附着速度翻倍，特快专递消耗的能量减少25%。");
        PA("평타 편지의 피해가 25% 줄어듭니다.", "Basic-attack letters deal 25% less damage.", "通常攻撃の手紙のダメージが25%減ります。", "普攻信件的伤害降低25%。");
        PA("무리 폭딜", "Crowd bursts", "群れへの瞬間火力", "群体爆发");
        PA("평타 공백", "Weak basic attacks", "通常攻撃の空白", "普攻空档");
        PA("표식이 붙은 적은 받는 피해가 15% 늘어납니다. (터진 뒤 4초까지)",
           "Marked enemies take 15% more damage. (Until 4s after the burst)",
           "印が付いた敵は受けるダメージが15%増えます。（爆発後4秒まで）",
           "被标记的敌人受到的伤害提高15%。（持续到爆炸后4秒）");
        PA("속달이 터질 때마다 다음 속달 게이지가 10% 찹니다.", "Each Express Delivery burst fills 10% of the next one's gauge.", "速達が弾けるたびに次の速達ゲージが10%たまります。", "特快专递每次爆炸都会为下一次充能10%。");
        PA("속달이 0.5초 뒤 같은 표식에서 한 번 더 터집니다. (피해 60%)",
           "Express Delivery bursts again on the same marks 0.5s later. (60% damage)",
           "速達が0.5秒後に同じ印でもう一度弾けます。（ダメージ60%）",
           "特快专递在0.5秒后于同一标记处再爆炸一次。（伤害60%）");

        // ---------------- 자전거 배달
        PA("자전거 배달", "Bike Courier", "自転車配達", "自行车投递");
        PA("달리면 편지가 저절로 · 기동", "Letters fly as you ride · Mobility", "走れば手紙が勝手に · 機動", "骑行时自动寄信 · 机动");
        PA("이동 속도 +20%, 움직이는 동안 0.5초마다 가장 가까운 적에게 편지가 저절로 날아갑니다. (편지 피해 18%)",
           "+20% move speed. While moving, a letter flies to the nearest enemy every 0.5s. (18% letter damage)",
           "移動速度+20%。移動中は0.5秒ごとに最も近い敵へ手紙が自動で飛びます。（手紙ダメージ18%）",
           "移动速度+20%。移动时每0.5秒自动向最近的敌人飞出一封信。（信件伤害18%）");
        PA("멈추면 편지 피해가 40% 줄어듭니다.", "Letters deal 40% less damage while you stand still.", "止まっていると手紙のダメージが40%減ります。", "停下时信件伤害降低40%。");
        PA("기동 · 생존", "Mobility · Survival", "機動 · 生存", "机动 · 生存");
        PA("서서 싸우는 보스 · 결계", "Stand-and-fight bosses · Barriers", "立ち止まるボス戦 · 結界", "需要站桩的首领 · 结界");
        PA("10칸 달릴 때마다 우표가 1 쌓입니다. (우표 수집이 없어도 중첩당 피해 +3%)",
           "Gain 1 stamp for every 10 tiles you ride. (+3% damage per stack even without Stamp Collecting)",
           "10マス走るごとに切手が1枚たまります。（切手集めがなくても1枚につきダメージ+3%）",
           "每骑行10格积累1张邮票。（即使没有集邮，每层伤害也+3%）");
        PA("1초 넘게 달리면 부딪힌 적을 밀쳐 냅니다. (공격력 25% · 보스 제외)",
           "After riding for over 1s, you shove enemies you bump into. (25% ATK · not bosses)",
           "1秒以上走り続けると、ぶつかった敵を押しのけます。（攻撃力25% · ボスを除く）",
           "持续骑行超过1秒后，会撞开碰到的敌人。（攻击力25% · 首领除外）");
        PA("자동 편지가 두 통씩 날아갑니다.", "Automatic letters fly two at a time.", "自動の手紙が2通ずつ飛びます。", "自动信件每次飞出两封。");

        // ---------------- 장거리 배송
        PA("장거리 배송", "Long-Haul Delivery", "長距離配送", "长途配送");
        PA("사거리 20 · 보스 · 원거리", "Range 20 · Bosses · Long range", "射程20 · ボス · 遠距離", "射程20 · 首领 · 远程");
        PA("사거리가 12칸에서 20칸으로 늘고, 피해가 1.5배가 됩니다.", "Range grows from 12 to 20 tiles, and damage becomes 1.5×.", "射程が12マスから20マスに伸び、ダメージが1.5倍になります。", "射程从12格增至20格，伤害变为1.5倍。");
        PA("공격 속도 −25%, 5칸 안의 적에게는 피해 −40%.", "−25% attack speed, −40% damage to enemies within 5 tiles.", "攻撃速度−25%、5マス以内の敵へのダメージ−40%。", "攻击速度−25%，对5格内敌人的伤害−40%。");
        PA("보스 · 원거리", "Bosses · Long range", "ボス · 遠距離", "首领 · 远程");
        PA("근접 포위", "Being surrounded", "近接での包囲", "近身包围");
        PA("20칸 넘게 날아간 편지는 치명타가 됩니다. (피해 ×1.5)", "Letters that fly over 20 tiles become critical hits. (×1.5 damage)", "20マス以上飛んだ手紙は会心になります。（ダメージ×1.5）", "飞行超过20格的信件成为暴击。（伤害×1.5）");
        PA("보스에게 배달하면 우표가 2중첩 쌓입니다.", "Deliveries to bosses add 2 stamp stacks.", "ボスに配達すると切手が2枚たまります。", "向首领投递时累积2层邮票。");
        PA("사거리가 40칸이 됩니다.", "Range becomes 40 tiles.", "射程が40マスになります。", "射程变为40格。");

        // ---------------- 새 교차
        PA("달리는 소포", "Rolling Parcel", "走る小包", "奔跑的包裹");
        PA("달리는 동안 1초마다 발밑에 소포를 떨어뜨립니다. (0.7초 뒤 반경 2칸 폭발 · 공격력 90%)",
           "While moving, drop a parcel at your feet every 1s. (Explodes after 0.7s · 2-tile radius · 90% ATK)",
           "走っている間、1秒ごとに足元へ小包を落とします。（0.7秒後に半径2マスの爆発 · 攻撃力90%）",
           "移动时每1秒在脚下丢下一个包裹。（0.7秒后半径2格爆炸 · 攻击力90%）");
        PA("전국 배송", "Nationwide Delivery", "全国配送", "全国配送");
        PA("순회 배달로 이어 갈 적이 멀수록 덜 약해집니다. (15칸 넘게 떨어지면 3%씩만)",
           "Delivery Round weakens less the farther the next addressee is. (Only 3% per hop beyond 15 tiles)",
           "巡回配達で次の宛先が遠いほど弱まりにくくなります。（15マス以上離れていれば3%ずつだけ）",
           "巡回投递的下一位收件人越远，衰减越少。（相距超过15格时每次只减3%）");

        // ---------------- 판 기록 · 피해 출처 (dealt_by · 판 결과 「준 피해」)
        PA("우표 조각", "Stamp Shards", "切手の破片", "邮票碎片");
        PA("자동 편지", "Auto Letters", "自動の手紙", "自动信件");
        PA("자전거 충돌", "Bike Bump", "自転車の体当たり", "自行车冲撞");
        PA("두 번째 속달", "Second Express", "二度目の速達", "第二次特快");
    }
}
