using UnityEngine;

// 번역표 (2.0.0~): 역류 (준비 창 · 잃기 3택 · 맨몸 · 처음의 슬라임 · 결과 줄 · 계약 4종)
// 열쇠는 reflux. 접두어 (한국어 원문은 0번 칸) · 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
// 다른 번역표 파일을 고치지 않게 게임이 켜질 때 스스로 더함 (LocTable.Relay 와 같은 방식)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddReflux()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ---------------- 열림 · 준비 창
        C("reflux.unlock.title", "새 모드 「역류」가 열렸다", "New mode \"Reflux\" unlocked", "新モード「逆流」が解放された", "新模式“逆流”已解锁");
        C("reflux.unlock.sub", "메인 메뉴 「특별 모드」에서 만날 수 있습니다", "Find it under \"Special Modes\" in the main menu", "メインメニューの「スペシャルモード」で挑戦できます", "可在主菜单“特别模式”中游玩");
        C("reflux.prep.path", "갈래", "Path", "系統", "流派");
        C("reflux.prep.form", "1차 무기", "First Weapon", "第一武器", "一阶武器");
        C("reflux.prep.strong", "강점", "Strong", "強み", "优势");
        C("reflux.prep.weak", "약점", "Weak", "弱み", "劣势");
        C("reflux.prep.body", "완성된 몸으로 시작: 레벨 40 · 무기 진화 3단계(각성) · 고른 갈래 카드 위주 · 역류의 축복\n영웅 · 갈래 · 1차 무기가 같으면 누구나 같은 몸 — 순위는 클리어 시간(짧을수록 위)",
          "Start fully built: Level 40, all 3 weapon evolutions (Awakening), cards focused on your Path, and the Blessing of Reflux.\nSame hero, Path and weapon means the same body for everyone — ranked by clear time (shorter is higher).",
          "完成した体で開始：Lv40・武器進化3段階（覚醒）・選んだ系統のカード中心・逆流の祝福\n英雄・系統・第一武器が同じなら誰でも同じ体 — 順位はクリア時間（短いほど上）",
          "以完整状态开始：40级、武器进化3阶段（觉醒）、以所选流派卡牌为主、逆流祝福\n英雄、流派、一阶武器相同则人人状态相同 — 按通关时间排名（越短越高）");
        C("reflux.prep.go", "역류 시작", "Begin Reflux", "逆流開始", "开始逆流");
        C("reflux.prep.nosave", "역류는 중간 저장되지 않습니다", "Reflux runs are not saved mid-run", "逆流は途中セーブされません", "逆流不会中途存档");
        C("reflux.prep.thishero", "이 영웅", "this hero", "この英雄", "该英雄");
        // 2.0.1~ 남은 영혼 설명 · 막힌 1차 무기 · 해금 조건 · 감독 준비 실패
        C("reflux.prep.spirit", "남은 영혼 = 클리어할 때 아직 쓰지 않은 잃기 수 (1분마다 하나씩 줄어 빨리 깰수록 많음)",
          "Spirits left = losses you haven't used when you clear (one is spent every minute, so faster clears keep more)",
          "残りの魂 = クリア時にまだ使っていない喪失の数（1分ごとに1つ減るので、早くクリアするほど多い）",
          "剩余灵魂 = 通关时尚未用掉的放弃次数（每分钟用掉一个，通关越快剩得越多）");
        C("reflux.prep.formblocked", "「일필」은 도형을 닫지 않아 「지구본」을 1차 무기로 고를 수 없습니다",
          "\"Single Stroke\" never closes shapes, so \"Globe\" can't be your first weapon",
          "「一筆」は図形を閉じないため、「地球儀」を第一武器に選べません",
          "“一笔”不会闭合图形，因此无法选择“地球仪”作为一阶武器");
        C("reflux.hub.locked", "무한 모드에서 개발자를 쓰러뜨리면 열립니다", "Unlocks after defeating the Developer in Endless Mode", "エンドレスモードで開発者を倒すと解放", "在无尽模式中击败开发者后解锁");
        C("reflux.fail.title", "역류를 준비하지 못했습니다", "Couldn't prepare Reflux", "逆流の準備に失敗しました", "逆流准备失败");
        C("reflux.fail.sub", "메인 메뉴로 돌아갑니다. 다시 시작해 주세요", "Returning to the main menu. Please start again.", "メインメニューに戻ります。もう一度開始してください", "将返回主菜单，请重新开始");

        // ---------------- 판 안 HUD · 알림
        C("reflux.banner", "역류 — 시간이 거꾸로 흐른다", "Reflux — time flows backward", "逆流 — 時間が逆に流れる", "逆流 — 时间倒流");
        C("reflux.banner.sub", "1분마다 힘을 하나씩 내려놓고, 적도 함께 약해진다", "Give up a power every minute — the enemies weaken too", "1分ごとに力を手放す。敵も弱くなる", "每分钟放弃一项力量，敌人也随之变弱");
        C("reflux.hud.time", "기록", "Time", "記録", "记录");
        C("reflux.hud.lost", "잃기 {0}/{1}", "Lost {0}/{1}", "喪失 {0}/{1}", "放弃 {0}/{1}");
        C("reflux.hud.spirit", "남은 영혼 {0}", "Spirits {0}", "残りの魂 {0}", "剩余灵魂 {0}");
        C("reflux.hud.boss", "보스 {0}/{1}", "Boss {0}/{1}", "ボス {0}/{1}", "首领 {0}/{1}");
        C("reflux.boss.arrive", "{0} 강림!", "{0} descends!", "{0}が降臨！", "{0}降临！");

        // ---------------- 「무엇을 잃을까」
        C("reflux.lose.title", "무엇을 잃을까", "What will you lose?", "何を失う？", "要放弃什么？");
        C("reflux.lose.sub", "역류 {0}/{1} · 남은 시간 {2} → 적도 약해집니다", "Reflux {0}/{1} · {2} left → the enemies weaken too", "逆流 {0}/{1}・残り {2} → 敵も弱くなります", "逆流 {0}/{1} · 剩余 {2} → 敌人也会变弱");
        C("reflux.lose.name", "{0} 을(를) 잃는다", "Lose {0}", "{0}を失う", "失去{0}");
        C("reflux.lose.evo", "스킬 진화도 사라짐", "its Skill Evolution is lost too", "スキル進化も消える", "技能进化也会消失");
        C("reflux.lose.gone", "사라짐", "gone", "消える", "消失");
        C("reflux.lose.all", "전체", "all of it", "すべて", "全部");
        C("reflux.lose.path", "갈래 「{0}」", "Path \"{0}\"", "系統「{0}」", "流派“{0}”");
        C("reflux.tier1", "1차 무기", "1st Weapon", "第一武器", "一阶武器");
        C("reflux.tier2", "2차 강화", "2nd Upgrade", "第二強化", "二阶强化");
        C("reflux.bless.0", "역류의 축복 · 공격력", "Blessing of Reflux · Attack", "逆流の祝福・攻撃力", "逆流祝福 · 攻击力");
        C("reflux.bless.1", "역류의 축복 · 공격 속도", "Blessing of Reflux · Attack Speed", "逆流の祝福・攻撃速度", "逆流祝福 · 攻击速度");
        C("reflux.bless.2", "역류의 축복 · 치명타", "Blessing of Reflux · Critical", "逆流の祝福・クリティカル", "逆流祝福 · 暴击");
        C("reflux.bless.3", "역류의 축복 · 최대 체력", "Blessing of Reflux · Max HP", "逆流の祝福・最大体力", "逆流祝福 · 最大生命");
        C("reflux.cat.0", "공격을 잃는다", "An offensive loss", "攻撃の喪失", "失去攻击");
        C("reflux.cat.1", "생존을 잃는다", "A survival loss", "生存の喪失", "失去生存");
        C("reflux.cat.2", "영웅다움을 잃는다", "A hero-identity loss", "英雄らしさの喪失", "失去英雄特色");

        // ---------------- 맨몸 · 처음의 슬라임 · 클리어
        C("reflux.end.title", "역류의 끝", "The End of Reflux", "逆流の終わり", "逆流的尽头");
        C("reflux.end.sub", "남은 모든 힘을 잃는다…", "You lose all that remains…", "残った力をすべて失う…", "剩下的一切力量都将消失…");
        C("reflux.slime.arrive", "맨몸의 앞에 「처음의 슬라임」이 나타났다!", "The First Slime appears before the bare-handed you!", "素手のあなたの前に「最初のスライム」が現れた！", "“最初的史莱姆”出现在赤手空拳的你面前！");
        C("reflux.slime.name", "처음의 슬라임", "The First Slime", "最初のスライム", "最初的史莱姆");
        C("reflux.clear.title", "역류 끝 — 처음으로 돌아왔다", "Reflux complete — back to the beginning", "逆流終了 — 始まりに戻ってきた", "逆流结束 — 回到了最初");

        // ---------------- 결과 줄 · 순위
        C("reflux.result.clear", "클리어", "Cleared", "クリア", "通关");
        C("reflux.result.spirit", "남은 영혼", "Spirit left", "残りの魂", "剩余灵魂");
        C("reflux.result.kept", "끝까지 쥔 것:", "Held to the end:", "最後まで握っていたもの：", "坚持到最后的：");
        C("reflux.result.best", "최고 기록", "Best", "自己ベスト", "最佳记录");
        C("reflux.result.fail", "보스 {0}/{1} · {2} 에 쓰러짐", "Boss {0}/{1} · fell at {2}", "ボス {0}/{1}・{2}で倒れた", "首领 {0}/{1} · 于{2}倒下");
        C("reflux.lb.rank", "역류 순위", "Reflux Rank", "逆流ランク", "逆流排名");

        // ---------------- 계약 4종
        C("contract.rf.dev2.name", "번개 같은 개발자 사냥", "Lightning Dev Hunt", "稲妻の開発者狩り", "闪电猎发者");
        C("contract.rf.dev2.cond", "역류에서 개발자를 2분 안에 쓰러뜨리기", "Defeat the Developer within 2 minutes in Reflux", "逆流で開発者を2分以内に倒す", "在逆流中2分钟内击败开发者");
        C("contract.rf.keepEvo.name", "끝까지 지킨 진화", "Evolution Kept", "守り抜いた進化", "守住的进化");
        C("contract.rf.keepEvo.cond", "역류에서 무기 진화를 리치 왕까지 하나도 잃지 않기", "Lose no weapon evolution until the Lich King in Reflux", "逆流でリッチキングまで武器進化を一つも失わない", "在逆流中直到巫妖王都不失去任何武器进化");
        C("contract.rf.nakedNoHit.name", "맨몸의 춤", "Dance of the Bare", "素手の舞", "赤手之舞");
        C("contract.rf.nakedNoHit.cond", "역류에서 처음의 슬라임이 나온 뒤 한 대도 맞지 않고 쓰러뜨리기", "In Reflux, defeat the First Slime without taking any hit after it appears", "逆流で最初のスライムが現れてから一度も被弾せずに倒す", "在逆流中，最初的史莱姆出现后不受任何伤害将其击败");      // 2.0.1~ 판정(슬라임이 나온 뒤 어떤 피해든)에 맞춤
        C("contract.rf.spirit3.name", "아껴 둔 영혼", "Spirits Saved", "取っておいた魂", "留存的灵魂");
        C("contract.rf.spirit3.cond", "역류를 남은 영혼 3 이상으로 클리어하기", "Clear Reflux with 3 or more Spirits left", "残りの魂3以上で逆流をクリアする", "以3个以上剩余灵魂通关逆流");
    }
}
