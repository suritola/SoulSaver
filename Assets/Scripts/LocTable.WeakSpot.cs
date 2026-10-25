using UnityEngine;

// 2026-10-18~ 빈틈 (WeakSpot · WeakHit) 문구 — 기획서 docs/review/2026-10-18-차별점-구현.md 1-8
//   부분 파일의 A() 는 같은 키가 이미 있으면 조용히 건너뜀 → 새 키는 지금 없는 접두어 weak. 로만 (계약 문구 contract.e.weak30.* 도 여기)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddWeakSpotLinesBoot() { if (!SceneFlow.DeferStartup(5, AddWeakSpotLines)) AddWeakSpotLines(); }      // 1.6.1~ 켤 때는 로딩 화면이 뜬 뒤로 (큰 함수는 불릴 때 통째로 JIT 되므로 작은 함수에서 미룸 · SceneFlow)

    static void AddWeakSpotLines()
    {
        void A(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        A("weak.name", "빈틈", "Weak Point", "隙", "破绽");
        A("weak.popup", "빈틈!", "WEAK POINT!", "隙！", "破绽！");
        A("weak.popup.perfect", "완벽한 빈틈!", "PERFECT WEAK POINT!", "完璧な隙！", "完美破绽！");
        A("weak.intro.title", "빈틈이 열렸다!", "A weak point is open!", "隙が開いた！", "破绽出现了！");
        A("weak.intro.sub", "빛나는 점을 직접 조준해 맞히면 피해가 크게 늘어납니다.", "Aim at the glowing spot and hit it to deal much more damage.", "光る点を直接狙って当てると、ダメージが大きく上がります。", "亲手瞄准发光点并击中，伤害会大幅提高。");
        A("weak.rule", "직접 조준한 공격만 빈틈을 맞힙니다. 평타와, 조준한 쪽(커서 · 오른쪽 스틱)으로 곧게 쏜 탄이 여기에 듭니다. 탄은 피해가 75%, 근접 공격과 샷건은 40%, 던진 공격의 직격점은 60% 늘어납니다. 궁수의 「완벽!」 화살과 시계공의 「정각」 톱니는 더 크게 늘어나며, 이때는 「완벽한 빈틈!」이 뜹니다.",
          "Only aimed attacks hit a weak point: basic attacks and shots fired straight where you aim (cursor or right stick). Shots deal 75% more damage, melee attacks and the shotgun deal 40% more, and a thrown attack's direct hit deals 60% more. The Archer's Perfect! arrows and the Clockmaker's O'Clock gears deal even more, and then PERFECT WEAK POINT! appears.",
          "直接狙った攻撃だけが隙に当たります。通常攻撃と、狙った方向（カーソル・右スティック）へまっすぐ撃った弾がこれに入ります。弾はダメージが75%、近接攻撃とショットガンは40%、投げた攻撃の直撃点は60%上がります。弓使いの「パーフェクト！」の矢と時計職人の「正時」の歯車はさらに大きく上がり、そのときは「完璧な隙！」と表示されます。",
          "只有亲手瞄准的攻击才能击中破绽，包括普通攻击，以及朝瞄准方向（光标或右摇杆）笔直射出的弹。弹的伤害提高75%，近战攻击和霰弹枪提高40%，投掷攻击的直击点提高60%。弓箭手的「完美！」之箭和钟表匠的「整点」齿轮提高得更多，此时会显示「完美破绽！」。");
        A("weak.boss.2", "빈틈: 왕관 아래의 핵입니다. 늘 열려 있지만, 뛰어오른 동안에는 닫힙니다.",
          "Weak point: the core under its crown. It is always open, except while the slime is in the air.",
          "隙：王冠の下の核です。いつも開いていますが、跳び上がっている間は閉じます。",
          "破绽：王冠下的核心。平时一直敞开，跃起时会合上。");
        A("weak.boss.0", "빈틈: 지팡이 끝의 영혼 구슬입니다. 주문을 외는 동안에만 열립니다.",
          "Weak point: the soul orb atop its staff. It opens only while the Lich King is casting.",
          "隙：杖の先の魂の宝珠です。呪文を唱えている間だけ開きます。",
          "破绽：法杖顶端的灵魂宝珠。只在念咒时打开。");
        A("weak.boss.1", "빈틈: 가슴의 갈라진 비늘입니다. 기술을 마친 직후 잠깐만 열리며, 날개를 편 뒤에는 날개 뿌리로 옮겨 갑니다.",
          "Weak point: the cracked scales on its chest. They open only for a moment right after an attack, and move to the base of its wings once it spreads them.",
          "隙：胸の割れた鱗です。技を終えた直後に一瞬だけ開き、翼を広げた後は翼の付け根に移ります。",
          "破绽：胸口裂开的鳞片。只在招式结束后短暂露出，展开双翼后会移到翼根。");
        A("weak.boss.3", "빈틈: 몸의 금 간 거울 조각입니다. 6초마다 자리를 옮기며, 분신 가운데 진짜에게만 있습니다.",
          "Weak point: a cracked mirror shard on its body. It moves every 6 seconds, and only the real one among the clones has it.",
          "隙：体のひび割れた鏡のかけらです。6秒ごとに場所を移し、分身の中では本物にだけあります。",
          "破绽：身上裂开的镜片。每6秒换一次位置，分身中只有真身才有。");
        A("weak.boss.dev", "빈틈: 몸 둘레를 도는 버그입니다. 개발자 엄마에게는 버그가 둘 있습니다.",
          "Weak point: the bug circling its body. The Developer's Mom has two of them.",
          "隙：体のまわりを回るバグです。開発者のママにはバグが2匹います。",
          "破绽：绕着身体打转的漏洞虫。开发者的妈妈身上有两只。");
        A("weak.midboss", "중간 보스의 빈틈은 머리입니다. 스킬을 준비하는 동안 크게 열립니다.",
          "A mid-boss's weak point is its head. It opens wide while the mid-boss prepares a skill.",
          "中ボスの隙は頭です。スキルを準備している間は大きく開きます。",
          "中头目的破绽在头部。准备技能时会大幅敞开。");
        A("weak.tip", "보스와 중간 보스에게는 빈틈이 있습니다. 직접 조준한 공격만 빈틈을 맞힙니다.",
          "Bosses and mid-bosses have weak points. Only aimed attacks can hit them.",
          "ボスと中ボスには隙があります。直接狙った攻撃だけが隙に当たります。",
          "首领和中头目都有破绽。只有亲手瞄准的攻击才能击中。");
        A("weak.codex.tag", "보스 · 중간 보스", "Bosses · Mid-bosses", "ボス・中ボス", "首领 · 中头目");
        A("contract.e.weak30.name", "서른 번의 빈틈", "Thirty Openings", "三十の隙", "三十处破绽");
        A("contract.e.weak30.cond", "빈틈 30번 맞히기", "Hit weak points 30 times", "隙に30回当てる", "击中破绽30次");
    }
}
