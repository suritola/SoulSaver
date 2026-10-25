using UnityEngine;

// 체험판 초원판 (차별점 구현 3-4): 체험판이 1장 초원 · 킹 슬라임에서 끝나고 영웅이 거너 · 검사 · 유령 셋이 되며 바뀐 끝 화면 문구 (Demo.ShowEnd)
// 옛 키(「리치 왕을 쓰러뜨렸습니다! 고마워요.」 · 「정식판에서는 초원 …」)는 LocTable.cs 에 그대로 남겨 둠 (문구 지침 1-5)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddDemoMeadowLinesBoot() { if (!SceneFlow.DeferStartup(5, AddDemoMeadowLines)) AddDemoMeadowLines(); }      // 1.6.1~ 켤 때는 로딩 화면이 뜬 뒤로 (큰 함수는 불릴 때 통째로 JIT 되므로 작은 함수에서 미룸 · SceneFlow)

    static void AddDemoMeadowLines()
    {
        void A(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        A("demo.end.thanks",
            "킹 슬라임을 쓰러뜨렸습니다. 고맙습니다!",
            "You defeated the King Slime. Thank you for playing!",
            "キングスライムを倒しました。遊んでくれてありがとう！",
            "你击败了史莱姆王。感谢游玩！");
        A("demo.end.full",
            "정식판에서는 지하 묘역 · 불타는 지옥 · 영혼의 심연과 리치 왕 · 지옥의 군주 · 거울의 군주,\n스킬 진화 · 보통 / 어려움 난이도 · 영혼 각인 · 영혼의 무게 · 봉인된 석관과 유물 · 무한 모드 · 일일 도전이 기다립니다.",
            "The full game adds the Catacombs, the Burning Hell and the Abyss of Souls, the Lich King, the Demon Lord and the Mirror Sovereign,\nskill evolutions · Normal / Hard difficulty · Soul Brands · Soul Weight · Sealed Sarcophagi & relics · Endless mode · Daily Challenge.",
            "製品版では、地下墓所・燃える地獄・魂の深淵、リッチ王・地獄の君主・鏡の君主、\nスキル進化・ノーマル / ハード難易度・魂の刻印・魂の重み・封印された石棺と遺物・エンドレスモード・デイリーチャレンジが待っています。",
            "正式版中还有地下墓穴 · 燃烧地狱 · 灵魂深渊与巫妖王 · 地狱领主 · 镜之君主，\n技能进化 · 普通 / 困难难度 · 灵魂烙印 · 灵魂之重 · 封印的石棺与遗物 · 无尽模式 · 每日挑战。");
        A("demo.end.heroes",
            "영웅 9명, 9개의 손. 체험판의 세 명 말고도 여섯 명이 더 있습니다.",
            "Nine heroes, nine hands. Six more are waiting beyond the three in this demo.",
            "9人の英雄、9つの手触り。体験版の3人のほかに、あと6人います。",
            "9位英雄，9种手感。除了试玩版的三位，还有六位在等你。");
    }
}
