using UnityEngine;

// 기억하는 보스 (BossMemory · 차별점 기획 2-6) 번역 — 키 접두어 bossmem. (memory.* 는 영웅 기억 조각이 씀)
// bossmem.codex.* 6개는 도감 · 보스바 툴팁이 그대로 읽는 완성 문장 (보스마다 체력 값 · 초를 채움)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddBossMemoryLinesBoot() { if (!SceneFlow.DeferStartup(5, AddBossMemoryLines)) AddBossMemoryLines(); }      // 1.6.1~ 켤 때는 로딩 화면이 뜬 뒤로 (큰 함수는 불릴 때 통째로 JIT 되므로 작은 함수에서 미룸 · SceneFlow)

    static void AddBossMemoryLines()
    {
        void A(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // 기억한 공격의 이름말 (평타는 영웅마다)
        A("bossmem.noun.gunner", "탄환", "bullets", "弾丸", "子弹");
        A("bossmem.noun.swordsman", "칼날", "blade", "刃", "剑刃");
        A("bossmem.noun.rogue", "표창", "shuriken", "手裏剣", "飞镖");
        A("bossmem.noun.archer", "화살", "arrows", "矢", "箭矢");
        A("bossmem.noun.alchemist", "플라스크", "flasks", "フラスコ", "烧瓶");
        A("bossmem.noun.cartographer", "잉크", "ink", "インク", "墨迹");
        A("bossmem.noun.clockmaker", "톱니", "gears", "歯車", "齿轮");
        A("bossmem.noun.postman", "편지", "letters", "手紙", "信件");
        A("bossmem.noun.ghost", "손길", "cold touch", "冷たい手", "冰冷之手");
        A("bossmem.noun.ult", "일격", "ultimate", "一撃", "必杀一击");
        A("bossmem.noun.other", "힘", "power", "力", "力量");

        // 보스 대사 ({0} = 이름말)
        A("bossmem.line.2", "…그 {0}, 이제 알아.", "...I know your {0} now.", "……その{0}、もう知ってる。", "……你的{0}，我已经懂了。");
        A("bossmem.line.0", "…그 {0}, 기억했다.", "...I remember your {0}.", "……その{0}、覚えたぞ。", "……你的{0}，我记住了。");
        A("bossmem.line.1", "그 {0}… 두 번은 통하지 않는다!", "Your {0} won't work twice!", "その{0}……二度は通じぬ！", "你的{0}……休想再得逞！");
        A("bossmem.line.3", "그 {0}, 이미 비춰 두었다.", "I have already reflected your {0}.", "その{0}、すでに映しておいた。", "你的{0}，早已映在镜中。");
        A("bossmem.line.dev", "그 {0}, 너무 세더라. 방금 고쳤어.", "Your {0}? Too strong. Just patched it.", "その{0}、強すぎたから今直した。", "你的{0}太强了，刚刚削弱了。");
        A("bossmem.line.mom", "엄마는 다 기억한다. 그 {0}도.", "Mom remembers everything. Even your {0}.", "ママは全部覚えてるの。その{0}もね。", "妈妈什么都记得。你的{0}也是。");
        A("bossmem.dev.title", "패치 노트: 「{0}」 하향", "Patch Notes: \"{0}\" nerfed", "パッチノート：「{0}」弱体化", "更新日志：「{0}」削弱");

        // 규칙 · 상태 ({0} = 공격 이름, {1} = 초)
        A("bossmem.sub.shield", "{1}초 동안 정면으로 들어오는 「{0}」 공격은 막힙니다. 옆이나 뒤로 돌아가 맞히세요.", "For {1}s, {0} attacks from the front are blocked. Move to its side or back to land hits.", "{1}秒間、正面からの「{0}」は防がれます。横か後ろに回り込んで当ててください。", "{1}秒内，从正面打来的「{0}」会被挡下。请绕到侧面或背后攻击。");
        A("bossmem.sub.ward", "{1}초 동안 「{0}」의 피해가 크게 줄어듭니다. 다른 공격으로 기억을 깨세요.", "For {1}s, {0} deals much less damage. Break the memory with other attacks.", "{1}秒間、「{0}」のダメージが大きく下がります。ほかの攻撃で記憶を砕いてください。", "{1}秒内，「{0}」造成的伤害大幅降低。请用其他攻击打破记忆。");
        A("bossmem.break.title", "기억이 깨졌다!", "The memory shatters!", "記憶が砕けた！", "记忆破碎了！");
        A("bossmem.break.sub", "잠시 휘청이는 동안 받는 피해가 25% 늘어납니다.", "While it staggers, it takes 25% more damage.", "よろめいている間、受けるダメージが25%増えます。", "踉跄期间受到的伤害提高25%。");
        A("bossmem.fade", "기억이 흐려졌다.", "The memory fades.", "記憶が薄れた。", "记忆淡去了。");
        A("bossmem.bar", "기억: {0} {1}초", "Remembers: {0} {1}s", "記憶：{0} {1}秒", "记忆：{0} {1}秒");
        A("bossmem.blocked", "막힘", "Blocked", "防がれた", "挡下");
        A("bossmem.shield.name", "기억의 방패", "Memory Shield", "記憶の盾", "记忆之盾");
        A("bossmem.ward.name", "기억의 결", "Memory Veil", "記憶の帳", "记忆之幕");
        // 받은 피해 출처 (거울의 군주가 되쏜 조각 · 결과 화면 「쓰러진 곳」 · balance_log last_hit)
        A("기억의 거울 조각", "기억의 거울 조각", "Mirror of Memory", "記憶の鏡片", "记忆镜片");

        // 도감 · 보스바 툴팁 (보스마다 값을 채운 완성 문장 · 0 리치 왕 · 1 지옥의 군주 · 2 킹 슬라임 · 3 거울의 군주)
        A("bossmem.codex.2",
            "기억: 체력 60%에서 이번 보스전에 가장 많이 맞은 공격을 기억해 10초 동안 조금 막습니다. 다른 공격으로 기억을 깰 수 있습니다.",
            "Memory: At 60% health, it remembers the attack that hurt it most this fight and partly blocks it for 10s. Other attacks can break the memory.",
            "記憶：体力60%で、この戦いで最も多く受けた攻撃を覚え、10秒間少し防ぎます。ほかの攻撃で記憶を砕けます。",
            "记忆：生命降到60%时，会记住本场首领战中受伤最多的攻击，并在10秒内稍微防御。用其他攻击可以打破记忆。");
        A("bossmem.codex.0",
            "기억: 체력 75% · 30%에서 이번 보스전에 가장 많이 맞은 공격을 기억해 12초(쉬움 10초) 동안 막습니다. 다른 공격으로 기억을 깰 수 있습니다.",
            "Memory: At 75% and 30% health, it remembers the attack that hurt it most this fight and blocks it for 12s (10s on Easy). Other attacks can break the memory.",
            "記憶：体力75%・30%で、この戦いで最も多く受けた攻撃を覚え、12秒間（イージーは10秒）防ぎます。ほかの攻撃で記憶を砕けます。",
            "记忆：生命降到75% · 30%时，会记住本场首领战中受伤最多的攻击，并在12秒内（简单难度为10秒）防御。用其他攻击可以打破记忆。");
        A("bossmem.codex.1",
            "기억: 체력 50% · 20%에서 이번 보스전에 가장 많이 맞은 공격을 기억해 12초(쉬움 10초) 동안 막습니다. 방패가 빠르게 다시 돌아섭니다. 다른 공격으로 기억을 깰 수 있습니다.",
            "Memory: At 50% and 20% health, it remembers the attack that hurt it most this fight and blocks it for 12s (10s on Easy). Its shield turns back quickly. Other attacks can break the memory.",
            "記憶：体力50%・20%で、この戦いで最も多く受けた攻撃を覚え、12秒間（イージーは10秒）防ぎます。盾がすばやく向き直ります。ほかの攻撃で記憶を砕けます。",
            "记忆：生命降到50% · 20%时，会记住本场首领战中受伤最多的攻击，并在12秒内（简单难度为10秒）防御。盾会很快重新转向。用其他攻击可以打破记忆。");
        A("bossmem.codex.3",
            "기억: 체력 75% · 30%에서 이번 보스전에 가장 많이 맞은 공격을 기억해 12초(쉬움 10초) 동안 막습니다. 정면에서 막힌 평타는 느린 거울 조각이 되어 돌아옵니다. 다른 공격으로 기억을 깰 수 있습니다.",
            "Memory: At 75% and 30% health, it remembers the attack that hurt it most this fight and blocks it for 12s (10s on Easy). Basic attacks blocked from the front return as slow mirror shards. Other attacks can break the memory.",
            "記憶：体力75%・30%で、この戦いで最も多く受けた攻撃を覚え、12秒間（イージーは10秒）防ぎます。正面で防がれた通常攻撃は、遅い鏡の欠片になって返ってきます。ほかの攻撃で記憶を砕けます。",
            "记忆：生命降到75% · 30%时，会记住本场首领战中受伤最多的攻击，并在12秒内（简单难度为10秒）防御。从正面被挡下的普通攻击会化作缓慢的镜片反射回来。用其他攻击可以打破记忆。");
        A("bossmem.codex.dev",
            "기억: 체력 66% · 33%에서 이번 보스전에 가장 많이 맞은 공격을 기억해 12초(쉬움 10초) 동안 막습니다. 다른 공격으로 기억을 깰 수 있습니다.",
            "Memory: At 66% and 33% health, it remembers the attack that hurt it most this fight and blocks it for 12s (10s on Easy). Other attacks can break the memory.",
            "記憶：体力66%・33%で、この戦いで最も多く受けた攻撃を覚え、12秒間（イージーは10秒）防ぎます。ほかの攻撃で記憶を砕けます。",
            "记忆：生命降到66% · 33%时，会记住本场首领战中受伤最多的攻击，并在12秒内（简单难度为10秒）防御。用其他攻击可以打破记忆。");
        A("bossmem.codex.mom",
            "기억: 체력 75% · 50% · 25%에서 이번 보스전에 가장 많이 맞은 공격을 기억해 10초 동안 막습니다. 다른 공격으로 기억을 깰 수 있습니다.",
            "Memory: At 75%, 50%, and 25% health, it remembers the attack that hurt it most this fight and blocks it for 10s. Other attacks can break the memory.",
            "記憶：体力75%・50%・25%で、この戦いで最も多く受けた攻撃を覚え、10秒間防ぎます。ほかの攻撃で記憶を砕けます。",
            "记忆：生命降到75% · 50% · 25%时，会记住本场首领战中受伤最多的攻击，并在10秒内防御。用其他攻击可以打破记忆。");
    }
}
