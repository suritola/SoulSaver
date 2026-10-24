using UnityEngine;

// 번역표 (1.1.9~): 편의성 3차 (흥행작 참고) — 일시정지 도감 · 적 설명 · 전투 통계 · 획득 범위 · 움직임 줄이기 · 빠른 연출 · 화면 밖 표시 · 빌드 보기 · 결과 복사
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddUx3()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        // ================================================================= 1. 일시정지 도감 · 적 설명
        S("지금 장: {0}", "Current chapter: {0}", "現在の章：{0}", "当前章节：{0}");
        S("멈춘 동안 적 위에 마우스를 올리면 설명이 보입니다.", "While paused, hover over an enemy to see its description.", "停止中に敵へカーソルを合わせると説明が表示されます。", "暂停时将鼠标移到敌人上即可查看说明。");

        // ================================================================= 2. 전투 통계
        S("전투 통계", "Combat Stats", "戦闘統計", "战斗统计");
        S("화면 위에 최근 10초의 초당 피해와 최근 1분의 분당 처치 수를 보여 줍니다. 멈춘 동안은 흐리게 보입니다.", "Shows damage per second over the last 10 seconds and kills per minute over the last minute at the top of the screen. It is dimmed while paused.", "画面上部に直近10秒の秒間ダメージと直近1分の毎分撃破数を表示します。停止中は薄く表示されます。", "在屏幕上方显示最近10秒的每秒伤害和最近1分钟的每分钟击杀数。暂停时会变淡。");
        S("작게", "Small", "小", "小");
        S("/분", "/min", "/分", "/分");

        // ================================================================= 3. 획득 범위
        S("획득 범위 표시", "Show Pickup Range", "取得範囲を表示", "显示拾取范围");
        S("코인을 끌어오는 범위를 캐릭터 둘레에 연한 고리로 늘 보여 줍니다. 꺼도 코인 자석 카드를 고른 직후에는 잠깐 보입니다.", "Always shows the coin pickup range as a faint ring around your character. Even when off, the ring appears briefly right after you pick a Coin Magnet card.", "コインを引き寄せる範囲をキャラクターの周りに薄い輪で常に表示します。オフでもコインマグネットカードを取った直後は少しの間表示されます。", "以淡色圆环始终显示吸取金币的范围。关闭时，选取金币磁铁卡牌后仍会短暂显示。");

        // ================================================================= 6. 움직임 줄이기
        S("움직임 줄이기", "Reduce Motion", "動きを減らす", "减少动态效果");
        S("버튼 · 배지의 맥동, 카드 뒤집기, 체력바 흔들림, 알림이 들어오는 움직임을 없애고 제자리에서 바로 보여 줍니다. 켜면 화면 흔들림도 함께 꺼집니다. 조준 줌과 진화 연출은 그대로입니다.", "Removes pulsing buttons and badges, card flips, health-bar shake and sliding notices, showing things in place instead. Turning it on also turns off screen shake. Aim zoom and evolution sequences are unchanged.", "ボタン・バッジの脈動、カードの反転、体力バーの揺れ、通知のスライドをなくし、その場で表示します。オンにすると画面の揺れも一緒にオフになります。照準ズームと進化演出はそのままです。", "去除按钮与徽章的脉动、卡牌翻转、生命条抖动和通知滑入，改为原地直接显示。开启时屏幕震动也会一并关闭。瞄准缩放与进化演出保持不变。");

        // ================================================================= 7. 빠른 연출
        S("빠른 연출", "Fast Cutscenes", "演出を速く", "快速演出");
        S("보스 처치, 진화, 상자 · 보상, 장 전환, 엔딩 같은 조작하지 않는 연출을 절반 시간으로 보여 줍니다. 상점 · 포털 카운트다운, 보스 결계 · 등장 연출, 카드 실수 방지 시간은 그대로입니다.", "Plays non-interactive sequences such as boss defeats, evolutions, chests and rewards, chapter transitions and the ending at half length. Shop and portal countdowns, boss barriers and entrances, and card misclick guards are unchanged.", "ボス撃破、進化、宝箱・報酬、章の移行、エンディングなどの操作しない演出を半分の時間で表示します。ショップ・ポータルのカウントダウン、ボスの結界・登場演出、カードの誤操作防止時間はそのままです。", "将首领击败、进化、宝箱与奖励、章节切换、结局等非操作演出缩短为一半。商店与传送门倒计时、首领结界与登场演出、卡牌防误触时间保持不变。");

        // ================================================================= 8. 화면 밖 표시
        S("화면 밖 표시", "Off-Screen Markers", "画面外マーカー", "屏幕外标记");
        S("보물 상자 · 떠돌이 상점 · 황금 고블린 · 중간 보스가 화면 밖에 있으면 화면 가장자리에 방향과 남은 시간을 보여 줍니다. 석관 · 영혼 제단 화살표와 포털 안내선은 늘 보입니다.", "When a treasure chest, wandering shop, golden goblin or mid-boss is off screen, shows its direction and remaining time at the screen edge. Sarcophagus and soul altar arrows and the portal guide line are always shown.", "宝箱・さすらいの商店・黄金ゴブリン・中ボスが画面外にあるとき、画面端に方向と残り時間を表示します。石棺・魂の祭壇の矢印とポータルの案内線は常に表示されます。", "当宝箱、流浪商店、黄金哥布林、中头目在屏幕外时，在屏幕边缘显示方向与剩余时间。石棺与灵魂祭坛的箭头和传送门指引线始终显示。");

        // ================================================================= 1.6.0~ 적 체력바 (그래픽 가독성 개편 1-F · HUD 탭, 용어는 위 화면 밖 표시와 같게)
        S("적 체력바", "Enemy HP Bars", "敵の体力バー", "敌人血条");
        S("정예만", "Elites Only", "精鋭のみ", "仅精英");
        S("맞은 적 모두", "All Damaged", "被弾した敵すべて", "所有受伤敌人");
        S("정예 · 중간 보스 · 황금 고블린만 머리 위 체력바를 보여 줍니다. 「맞은 적 모두」면 한 번이라도 맞은 적에게 모두 보입니다. 보스 체력은 화면 위 막대로 늘 보입니다.", "Shows overhead HP bars only on elites, mid-bosses and the golden goblin. Set to All Damaged to show a bar on every enemy you have hit. Boss HP is always shown at the top of the screen.", "精鋭・中ボス・黄金ゴブリンだけ頭上に体力バーを表示します。「被弾した敵すべて」にすると、一度でも攻撃した敵すべてに表示されます。ボスの体力は画面上部のバーに常に表示されます。", "仅在精英、中头目和黄金哥布林头顶显示血条。选择「所有受伤敌人」时，被击中过的敌人都会显示血条。头目血量始终显示在屏幕上方。");

        // ================================================================= 9. 빌드 보기 — 피해 · 진화 진행
        S("진화 진행", "Evolution Progress", "進化の進み", "进化进度");
        S("분당 처치", "Kills / min", "毎分撃破", "每分钟击杀");

        // ================================================================= 10. 결과 복사
        S("결과 복사", "Copy Result", "結果をコピー", "复制结果");
        S("복사", "Copy", "コピー", "复制");
        S("복사했습니다", "Copied", "コピーしました", "已复制");
    }
}
