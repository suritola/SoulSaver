using UnityEngine;

// 번역표 (2.0.0~): 특별 모드 창 · 영혼 릴레이 (주자 고르기 · 교대 · 유품 14종 · 결과 줄 · 순위 탭 · 계약 4종)
// 열쇠는 relay. 접두어 (한국어 원문은 0번 칸) · 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
// 다른 번역표 파일을 고치지 않게 게임이 켜질 때 스스로 더함 (LocTable.Leaderboard 와 같은 방식) · 역류 글은 역류 작업자가 이 파일 아래에 이어서
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddRelay()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ---------------- 메인 메뉴 · 특별 모드 창
        C("특별 모드", "특별 모드", "Special Modes", "スペシャルモード", "特别模式");
        C("영혼 릴레이", "영혼 릴레이", "Soul Relay", "魂のリレー", "灵魂接力");
        C("역류", "역류", "Reflux", "逆流", "逆流");
        C("relay.hub.pick", "고르기", "Select", "選ぶ", "选择");
        C("relay.hub.ranks", "순위 보기", "View Ranks", "ランキング", "查看排名");
        C("relay.hub.best", "내 최고 기록", "My Best", "自己ベスト", "个人最佳");
        C("relay.hub.locked.relay", "무한 모드를 열고 영웅을 셋 이상 가지면 열립니다", "Unlocks once Endless is open and you own 3 heroes", "エンドレスを解放し、英雄を3人以上持つと解放", "开启无尽模式并拥有3名英雄后解锁");
        C("relay.hub.locked.reflux", "개발자를 쓰러뜨리면 열립니다", "Unlocks after defeating the Developer", "開発者を倒すと解放", "击败开发者后解锁");
        C("relay.hub.soon", "준비 중입니다", "Coming soon", "準備中です", "敬请期待");
        C("relay.card.relay",
          "영웅 셋을 골라 차례로 무한 사막에 들어갑니다.\n쓰러지면 그 자리에서 다음 영웅이 일어나 이어받습니다.\n앞 영웅의 빌드는 사라지고 「유품」 하나가 남습니다.",
          "Pick three heroes and run the endless desert in turn.\nWhen one falls, the next rises on the spot and takes over.\nThe fallen hero's build is lost, but one Heirloom remains.",
          "英雄を3人選び、順番に無限の砂漠へ。\n倒れるとその場で次の英雄が立ち上がり引き継ぎます。\n前の英雄のビルドは消え、「形見」が1つ残ります。",
          "选择三名英雄依次进入无尽沙漠。\n倒下后，下一名英雄会在原地起身接棒。\n前一名英雄的构筑会消失，只留下一件“遗物”。");
        C("relay.card.reflux",
          "15분째 완성된 몸으로 시작해 시간이 거꾸로 흐릅니다.\n1분마다 힘을 하나씩 내려놓고 적도 약해집니다.\n맨몸으로 「처음의 슬라임」을 쓰러뜨리면 클리어.",
          "Start fully built at 15:00 as time flows backward.\nEvery minute you give up a power, and enemies weaken too.\nDefeat the First Slime bare-handed to clear.",
          "15分時点の完成した体で始まり、時間が逆に流れます。\n1分ごとに力を1つ手放し、敵も弱くなります。\n素手で「最初のスライム」を倒せばクリア。",
          "以15分钟时的完整状态开始，时间倒流。\n每分钟放弃一项力量，敌人也随之变弱。\n赤手空拳击败“最初的史莱姆”即可通关。");

        // ---------------- 주자 고르기 창
        C("relay.pick.title", "주자 고르기", "Pick the Runners", "走者を選ぶ", "选择接力顺序");
        C("relay.pick.slot", "{0}번 주자", "Runner {0}", "{0}番走者", "第{0}棒");
        C("relay.pick.heir", "넘겨줄 유품: {0}", "Heirloom passed on: {0}", "引き継ぐ形見：{0}", "传下的遗物：{0}");
        C("relay.pick.last", "마지막 주자", "Last runner", "最終走者", "最后一棒");
        C("relay.pick.empty", "영웅을 골라 주세요", "Choose a hero", "英雄を選んでください", "请选择英雄");
        C("relay.pick.go", "출발", "Go", "出発", "出发");
        C("relay.pick.hint", "영웅을 눌러 넣고 빼세요. 셋이 차면 출발할 수 있습니다. 같은 영웅은 한 번만.", "Click a hero to add or remove. Start when all three slots are filled. Each hero only once.", "英雄を押して出し入れします。3人揃うと出発できます。同じ英雄は1回だけ。", "点击英雄加入或移除。三个位置填满后即可出发。每名英雄只能选一次。");
        C("relay.pick.best", "이 조합 최고 기록", "Best for this trio", "この組み合わせの最高記録", "此组合最佳记录");
        C("relay.pick.top", "조합 순위 상위 5", "Top 5 for this trio", "この組み合わせ上位5", "此组合前5名");

        // ---------------- 판 안
        C("relay.hud.runner", "주자 {0}/3", "Runner {0}/3", "走者 {0}/3", "第{0}/3棒");
        // 2.0.1~ 「{1}」 뒤 조사 없이: 유품 이름 받침에 따라 을/를이 갈려 「를」 고정이 반쯤 틀렸음 · 「{0} 의」 띄어쓰기도 붙임 (영 · 일 · 중은 조사 문제 없음)
        C("relay.toast.handoff", "주자 교대 — {0}의 유품 「{1}」 이어받음", "Runner change — inherited {0}'s heirloom \"{1}\"", "走者交代 — {0}の形見「{1}」を引き継いだ", "接力交棒 — 继承了{0}的遗物“{1}”");
        C("relay.heir.label", "유품", "Heirlooms", "形見", "遗物");
        C("relay.heir.none", "유품 없음 (첫 주자)", "No heirlooms (first runner)", "形見なし（最初の走者）", "无遗物（第一棒）");
        C("영혼 이어받기", "영혼 이어받기", "Soul Takeover", "魂の継承", "灵魂接棒");
        C("유품", "유품", "Heirloom", "形見", "遗物");

        // ---------------- 결과 화면 · 순위
        C("relay.result.best", "이 조합 최고 기록", "Best for this trio", "この組み合わせの最高記録", "此组合最佳记录");
        C("relay.result.newrecord", "새 기록!", "New record!", "新記録！", "新纪录！");
        C("relay.lb.rank", "조합 순위", "Trio Rank", "組み合わせ順位", "组合排名");
        C("relay.lb.all", "모든 조합", "All trios", "すべての組み合わせ", "所有组合");
        C("relay.lb.reflux", "짧을수록 좋음", "Faster is better", "短いほど良い", "越短越好");

        // ---------------- 새 모드 알림
        C("relay.unlock.title", "새 모드 「영혼 릴레이」가 열렸다", "New mode unlocked: Soul Relay", "新モード「魂のリレー」が解放された", "新模式“灵魂接力”已开启");
        C("relay.unlock.sub", "「특별 모드」에서 영웅 셋을 이어 달려 보세요", "Run with three heroes in Special Modes", "「スペシャルモード」で英雄3人をつないで走ろう", "在“特别模式”中让三名英雄接力吧");

        // ---------------- 계약
        C("relay.contract.kind", "릴레이 · 600 P", "Relay · 600 P", "リレー・600 P", "接力 · 600 P");
        C("contract.r.first5.name", "첫 번째 주자", "The First Runner", "最初の走者", "第一棒");
        C("contract.r.first5.cond", "영혼 릴레이에서 첫 주자로 5분 버티기", "Survive 5 minutes as the first runner in Soul Relay", "魂のリレーで最初の走者として5分生き残る", "在灵魂接力中以第一棒坚持5分钟");
        C("contract.r.handoff30.name", "이어받은 분노", "Inherited Fury", "受け継いだ怒り", "继承的怒火");
        C("contract.r.handoff30.cond", "영혼 릴레이에서 이어받고 10초 안에 30마리 처치", "Defeat 30 enemies within 10 seconds of taking over in Soul Relay", "魂のリレーで引き継いでから10秒以内に30体倒す", "在灵魂接力中接棒后10秒内击败30只敌人");
        C("contract.r.allboss.name", "한 사람 한 보스", "One Boss Each", "一人一ボス", "每人一个首领");
        C("contract.r.allboss.cond", "영혼 릴레이에서 세 주자 모두 보스를 하나씩 쓰러뜨리기", "Have all three runners defeat a boss each in Soul Relay", "魂のリレーで3人の走者がそれぞれボスを1体倒す", "在灵魂接力中让三名走者各击败一个首领");
        C("contract.r.burstboss.name", "혼을 건 일격", "Soul-Backed Strike", "魂を懸けた一撃", "魂之一击");
        C("contract.r.burstboss.cond", "영혼 릴레이 이어받기 파동으로 보스에게 피해 주기", "Hit a boss with the takeover shockwave in Soul Relay", "魂のリレーの継承の波動でボスにダメージを与える", "在灵魂接力中用接棒冲击波对首领造成伤害");

        // ---------------- 유품 14종 (modes.md 1-6)
        C("relay.heir.0.name", "마지막 탄창", "Last Magazine", "最後の弾倉", "最后的弹匣");
        C("relay.heir.0.desc", "필살기를 쓸 때마다 화면 안 적 최대 6마리에 탄 한 발씩 (공격력 120%)", "Each time you use your ult, fire one round at up to 6 enemies on screen (120% attack)", "必殺技のたびに画面内の敵最大6体へ1発ずつ（攻撃力120%）", "每次使用必杀技，向屏幕内最多6个敌人各射一发（攻击力120%）");
        C("relay.heir.1.name", "녹슨 장검", "Rusty Longsword", "錆びた長剣", "生锈的长剑");
        C("relay.heir.1.desc", "4초마다 둘레 2.5칸에 적이 셋 이상이면 회전 베기 한 바퀴 (공격력 80%)", "Every 4s, if 3+ enemies are within 2.5 tiles, spin-slash once (80% attack)", "4秒ごとに周囲2.5マスに敵が3体以上いれば回転斬り（攻撃力80%）", "每4秒，若周围2.5格内有3个以上敌人，则回旋斩一圈（攻击力80%）");
        C("relay.heir.2.name", "그림자 발자국", "Shadow Footprints", "影の足跡", "暗影足迹");
        C("relay.heir.2.desc", "맞을 때마다(쿨 6초) 가장 가까운 적 쪽으로 출혈 선 4칸 (3초, 초당 공격력 40%)", "When hit (6s cooldown), lay a 4-tile bleed trail toward the nearest enemy (3s, 40% attack/s)", "被弾時（クール6秒）、最も近い敵へ4マスの出血ライン（3秒、毎秒攻撃力40%）", "受击时（冷却6秒）向最近的敌人铺一条4格的流血线（3秒，每秒攻击力40%）");
        C("relay.heir.3.name", "꺾인 활", "Broken Bow", "折れた弓", "折断的弓");
        C("relay.heir.3.desc", "10초마다 커서 쪽으로 가득 당긴 화살 한 발 자동 (모두 관통, 공격력 250%)", "Every 10s, auto-fire a full-draw piercing arrow toward the cursor (250% attack)", "10秒ごとにカーソル方向へ全力の貫通矢を自動発射（攻撃力250%）", "每10秒自动向光标方向射出一支满弦贯穿箭（攻击力250%）");
        C("relay.heir.4.name", "깨진 플라스크", "Cracked Flask", "割れたフラスコ", "破碎的烧瓶");
        C("relay.heir.4.desc", "적 12마리를 처치할 때마다 마지막 처치 자리에서 플라스크 폭발 (반경 2.5, 공격력 150%)", "Every 12 kills, a flask explodes where the last enemy fell (radius 2.5, 150% attack)", "敵を12体倒すごとに最後の撃破地点でフラスコ爆発（半径2.5、攻撃力150%）", "每击败12个敌人，在最后击败处发生烧瓶爆炸（半径2.5，攻击力150%）");
        C("relay.heir.5.name", "접힌 지도", "Folded Map", "折れた地図", "折叠的地图");
        C("relay.heir.5.desc", "걸은 자리에 잉크 꼬리 3초 (닿은 적 초당 공격력 30%)", "Leave an ink trail for 3s where you walk (30% attack/s to enemies touching it)", "歩いた跡に3秒間インクの尾（触れた敵に毎秒攻撃力30%）", "行走之处留下持续3秒的墨迹（接触的敌人每秒受到攻击力30%）");
        C("relay.heir.6.name", "멈춘 회중시계", "Stopped Pocket Watch", "止まった懐中時計", "停摆的怀表");
        C("relay.heir.6.desc", "15초마다 「째깍」 둘레 4칸 적 0.6초 정지 (보스 제외 · 시간 자체는 그대로)", "Every 15s, a \"tick\" freezes enemies within 4 tiles for 0.6s (not bosses; time itself is unchanged)", "15秒ごとに「カチッ」と周囲4マスの敵が0.6秒停止（ボス除く・時間そのものは不変）", "每15秒“滴答”一声，使4格内敌人静止0.6秒（首领除外，时间本身不变）");
        C("relay.heir.7.name", "마지막 우편", "Last Letter", "最後の郵便", "最后的邮件");
        C("relay.heir.7.desc", "10번째 처치마다 화면 안 가장 먼 적에게 소포 (착탄 반경 2, 공격력 200%)", "Every 10th kill drops a parcel on the farthest on-screen enemy (radius 2, 200% attack)", "10体撃破ごとに画面内で最も遠い敵へ小包（着弾半径2、攻撃力200%）", "每击败10个敌人，向屏幕内最远的敌人投下包裹（半径2，攻击力200%）");
        C("relay.heir.8.name", "차가운 손", "Cold Hand", "冷たい手", "冰冷的手");
        C("relay.heir.8.desc", "지금 영웅 평타에 맞은 적에게 냉기 1겹 (0.5초에 한 번 · 다섯 겹이면 얼림)", "Basic attacks add 1 chill to the enemy hit (once per 0.5s; five stacks freeze)", "通常攻撃が当たった敵に冷気を1層（0.5秒に1回・5層で凍結）", "普通攻击命中的敌人叠加1层寒气（每0.5秒一次，5层冻结）");
        C("relay.heir.9.name", "샅바", "Wrestler's Belt", "締め込み", "缠腰带");
        C("relay.heir.9.desc", "8초마다 몸에 닿은 일반 적 하나를 들어 반대편 4칸으로 메침 (공격력 200% · 떨어진 자리 반경 1.5)", "Every 8s, lift a nearby normal enemy and slam it 4 tiles to the opposite side (200% attack, radius 1.5)", "8秒ごとに体に触れた通常の敵を持ち上げ、反対側4マスへ投げ飛ばす（攻撃力200%・着地点半径1.5）", "每8秒举起身边一个普通敌人，摔向对侧4格处（攻击力200%，落点半径1.5）");
        C("relay.heir.10.name", "공사 중 표지판", "Under Construction Sign", "工事中の看板", "施工中标志牌");
        C("relay.heir.10.desc", "필살기를 쓸 때마다 영웅 뒤쪽 2칸에 「공사 중」 바리케이드 2개 6초 (적을 크게 늦추고 닿으면 공격력 50%)", "Each ult drops 2 \"under construction\" barricades 2 tiles behind you for 6s (heavily slow enemies, 50% attack on touch)", "必殺技のたびに背後2マスに「工事中」バリケードを2つ6秒（敵を大きく減速、接触で攻撃力50%）", "每次使用必杀技，在身后2格放置2个“施工中”路障6秒（大幅减速敌人，接触造成攻击力50%）");
        C("relay.heir.11.name", "남은 줄", "Leftover Strings", "残った糸", "残留的线");
        C("relay.heir.11.desc", "작은 더미 하나가 2칸 뒤를 따라다니며 3초마다 둘레 1.5칸 휘두름 (공격력 70%)", "A small puppet follows 2 tiles behind and swings every 3s (radius 1.5, 70% attack)", "小さな人形が2マス後ろをついて回り、3秒ごとに周囲1.5マスを薙ぐ（攻撃力70%）", "一个小人偶跟在身后2格，每3秒挥击周围1.5格（攻击力70%）");
        C("relay.heir.12.name", "삽 한 자루", "A Shovel", "シャベル一本", "一把铁锹");
        C("relay.heir.12.desc", "적 처치 때 20% 확률로 그 자리 구멍 2초 (지나던 적이 빠져 1초 못 나옴)", "20% chance on a kill to leave a hole for 2s (enemies falling in are stuck for 1s)", "敵撃破時20%の確率でその場に穴を2秒（落ちた敵は1秒動けない）", "击败敌人时有20%概率留下持续2秒的坑（掉入的敌人被困1秒）");
        C("relay.heir.13.name", "마지막 필름", "Last Film", "最後のフィルム", "最后的胶卷");
        C("relay.heir.13.desc", "20초마다 커서 자리 3×3칸 셔터 한 번 — 틀 안 적이 2초 느려지고 피해 (공격력 60%)", "Every 20s, a shutter fires at the cursor (3x3 tiles) — enemies in frame are slowed for 2s and hit (60% attack)", "20秒ごとにカーソル位置の3×3マスでシャッター — 枠内の敵は2秒減速しダメージ（攻撃力60%）", "每20秒在光标处3×3格按一次快门——画框内敌人减速2秒并受到伤害（攻击力60%）");
    }
}
