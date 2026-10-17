using UnityEngine;

// 번역표 (1.2.3~): 무한 모드 보스 타이밍 「보스는 기다리지 않는다」 — 겹쳐 등장 · 모래 폭풍 풍화 · 15분 판정 · 개발자 · 엄마전 약한 잡몹
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddEndlessBoss()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void K(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        // ================================================================= 겹쳐 등장
        K("다음 왕이 기다리지 않고 나타났다!", "The next king would not wait — it has arrived!", "次の王は待たずに現れた！", "下一位王不再等待，现身了！");
        K("보스는 기다려 주지 않습니다. 한 번에 둘까지 나옵니다.", "Bosses do not wait for you. Up to two can be out at once.",
          "ボスは待ってくれません。一度に2体まで現れます。", "首领不会等你。最多可同时出现两名。");
        K("보스는 기다려 주지 않습니다. 보스가 3분 넘게 살아 있으면 다음 보스가 함께 나오고(한 번에 둘까지), 오래 버틴 보스는 모래 폭풍에 풍화되어 받는 피해가 늘어납니다. 15분까지 보스를 하나라도 쓰러뜨려야 「최후의 시간」이 열립니다.",
          "Bosses do not wait for you. If a boss survives more than 3 minutes, the next one joins it (up to two at once), and a lingering boss is weathered by the sandstorm and takes more damage. Defeat at least one boss before the 15-minute mark to open the Final Hour.",
          "ボスは待ってくれません。ボスが3分以上生き残ると次のボスが加わり（一度に2体まで）、長く居座るボスは砂嵐に風化して受けるダメージが増えます。15分までにボスを1体でも倒さないと「最後の時」は開かれません。",
          "首领不会等你。若首领存活超过3分钟，下一名首领会一同出现（最多两名），久留的首领会被沙暴风化，受到的伤害增加。必须在15分钟前至少击败一名首领，才能开启「最后时刻」。");

        // ================================================================= 모래 폭풍 풍화
        K("{0} — 모래 폭풍에 풍화되어 받는 피해가 1분마다 {1}% 늘어납니다.", "{0} — weathered by the sandstorm: damage taken rises {1}% every minute.",
          "{0} — 砂嵐に風化し、受けるダメージが1分ごとに{1}%増えます。", "{0} — 被沙暴风化，受到的伤害每分钟增加{1}%。");
        K("풍화", "Weathered", "風化", "风化");

        // ================================================================= 15분: 보스 처치 0 → 모래 폭풍
        K("15분을 버텼다… 모래 폭풍이 거세진다!", "You survived 15 minutes... the sandstorm rages!", "15分を耐え抜いた…砂嵐が激しくなる！", "坚持了15分钟……沙暴愈发猛烈！");
        K("최후의 시간은 보스를 한 번이라도 쓰러뜨린 자에게만 열립니다. 적은 끝없이 몰려옵니다 — 끝까지 버티세요.",
          "The Final Hour opens only to those who have felled at least one boss. Enemies will swarm without end — hold out as long as you can.",
          "最後の時は、ボスを一度でも倒した者にだけ開かれます。敵は果てしなく押し寄せます——最後まで耐え抜いてください。",
          "最后时刻只为至少击败过一名首领的人开启。敌人将源源不断地涌来——坚持到最后。");

        // ================================================================= 도감 「개발자」 설명 (CodexUI · 옛 문장 키는 LocTable.cs 에 그대로)
        K("15분까지 보스를 하나라도 쓰러뜨렸으면 15분에 모든 보스가 깨어나고, 그 보스들을 모두 쓰러뜨리면 강림합니다. 영웅들의 최종 진화 필살기를 씁니다. 처음엔 10초에 한 번 쓰고, 체력이 닳을수록 점점 빨라져 1초에 한 번까지 씁니다. 싸우는 동안에는 약한 적만 1초에 4마리씩, 최대 30마리까지 나옵니다.",
          "If you have defeated at least one boss by the 15-minute mark, every boss awakens at 15 minutes; defeat them all and the Developer descends. It uses the heroes' final-evolution ultimates, once every 10 seconds at first and faster as its health drops, down to once every second. During the fight only weak enemies appear, 4 every second, up to 30 at a time.",
          "15分までにボスを1体でも倒していれば、15分に全てのボスが目覚め、それらを全て倒すと降臨します。英雄たちの最終進化必殺技を使います。最初は10秒に1回、体力が減るほど速くなり、最後は1秒に1回使います。戦っている間は弱い敵だけが1秒に4体ずつ、最大30体まで現れます。",
          "若在15分钟前至少击败过一名首领，15分钟时所有首领将苏醒；全部击败后开发者降临。它会使用英雄们的最终进化必杀技，起初每10秒一次，生命越低越快，最终每1秒一次。战斗期间只会出现弱小的敌人，每秒4名，最多同时30名。");

        // ================================================================= 도감 「개발자 엄마」 설명 (완결 문장 · 규칙 E)
        C("개발자를 쓰러뜨린 뒤 영혼 3000개를 거두면 강림합니다. 개발자보다 체력이 2배이고, 피해와 이동 속도도 2배입니다. 필살기는 처음엔 5초에 한 번 쓰고, 체력이 닳을수록 점점 빨라져 1초에 한 번까지 씁니다. 싸우는 동안에는 약한 적만 1초에 4마리씩, 최대 30마리까지 나옵니다. 개발자와 엄마를 모두 쓰러뜨리면 무한 모드를 클리어합니다.",
          "개발자를 쓰러뜨린 뒤 영혼 3,000개를 거두면 강림합니다. 개발자보다 체력이 2배이고, 피해와 이동 속도도 2배입니다. 필살기는 처음엔 5초에 한 번 쓰고, 체력이 닳을수록 점점 빨라져 1초에 한 번까지 씁니다. 싸우는 동안에는 약한 적만 1초에 4마리씩, 최대 30마리까지 나옵니다. 개발자와 엄마를 모두 쓰러뜨리면 무한 모드를 클리어합니다.",
          "Descends once you gather 3,000 souls after defeating the Developer. It has twice the Developer's health, and its damage and movement speed are doubled as well. It uses its ultimate once every 5 seconds at first and faster as its health drops, down to once every second. During the fight only weak enemies appear, 4 every second, up to 30 at a time. Defeat both the Developer and the Developer's Mom to clear Endless Mode.",
          "開発者を倒した後、魂を3,000個集めると降臨します。開発者の2倍の体力を持ち、ダメージと移動速度も2倍です。必殺技は最初は5秒に1回、体力が減るほど速くなり、最後は1秒に1回使います。戦っている間は弱い敵だけが1秒に4体ずつ、最大30体まで現れます。開発者とママを両方倒すと、エンドレスをクリアします。",
          "击败开发者后收集3,000个灵魂即会降临。它的生命是开发者的2倍，伤害和移动速度也是2倍。必杀技起初每5秒一次，生命越低越快，最终每1秒一次。战斗期间只会出现弱小的敌人，每秒4名，最多同时30名。击败开发者和妈妈即可通关无尽模式。");

        // ================================================================= 패치노트 (게임 안 공지 · 결과 화면 여유분)
        K("보스는 기다리지 않는다", "Bosses Do Not Wait", "ボスは待たない", "首领不会等待");
        K("보스가 3분 넘게 살아 있으면 다음 보스가 함께 나오고, 오래 버틴 보스는 모래 폭풍에 풍화됩니다. 15분의 최후의 시간은 보스를 하나라도 쓰러뜨린 판에만 열립니다.",
          "If a boss survives more than 3 minutes the next one joins it, and a lingering boss is weathered by the sandstorm. The Final Hour at 15 minutes opens only in runs where you have defeated at least one boss.",
          "ボスが3分以上生き残ると次のボスが加わり、長く居座るボスは砂嵐に風化します。15分の最後の時は、ボスを1体でも倒したプレイでのみ開かれます。",
          "若首领存活超过3分钟，下一名首领会一同出现，久留的首领会被沙暴风化。15分钟的最后时刻只在至少击败过一名首领的对局中开启。");
    }
}
