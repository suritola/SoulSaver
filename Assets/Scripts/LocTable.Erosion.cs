using UnityEngine;

// 번역표 (1.2.5~): 통합 결계 게이지 · 「공간 침식」 (BossUltimate.Erosion · SpaceErosion · 보스바 · 도감)
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddErosion()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void K(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        // ================================================================= 공간 침식 컷신 · 보스바 · 받은 피해 이름
        K("공간 침식", "Spatial Erosion", "空間侵食", "空间侵蚀");
        K("공간 침식 ×{0}", "Spatial Erosion ×{0}", "空間侵食 ×{0}", "空间侵蚀 ×{0}");
        K("결계 붕괴", "Barrier Collapse", "結界崩壊", "结界崩塌");
        K("모든 왕의 영역이, 하나로 무너져 내린다.", "Every king's domain collapses into one.", "すべての王の領域が、ひとつに崩れ落ちる。", "所有王的领域，崩塌为一。");
        K("결계 게이지가 하나로 합쳐졌다!", "The barrier gauges have merged into one!", "結界ゲージがひとつに合わさった！", "结界槽合而为一了！");
        K("고요의 눈", "Eye of Calm", "静寂の目", "寂静之眼");
        K("…엄마, 지금 결계 쓰는 중이라니까요.", "...Mom, I told you, I'm in the middle of a barrier.", "…母さん、今結界を張ってるところだってば。", "……妈，我都说了我正在放结界。");

        // ================================================================= 도움말
        K("보스가 둘 이상이면 결계 게이지가 하나로 합쳐집니다. 어느 보스를 때려도 같은 게이지가 차고, 가득 차면 모든 보스의 결계가 하나로 무너지는 「공간 침식」이 펼쳐집니다. 보스가 하나만 남으면 남은 게이지를 그 보스가 그대로 이어받습니다.",
          "When two or more bosses are on the field, their barrier gauges merge into one. Hitting any boss fills the same gauge, and when it is full every boss's barrier collapses into a single 「Spatial Erosion」. When only one boss remains, it inherits the remaining gauge as is.",
          "ボスが2体以上いると、結界ゲージがひとつに合わさります。どのボスを攻撃しても同じゲージがたまり、満タンになるとすべてのボスの結界がひとつに崩れ落ちる「空間侵食」が展開されます。ボスが1体だけ残ると、残ったゲージをそのボスがそのまま引き継ぎます。",
          "当场上有两名以上首领时，结界槽会合而为一。攻击任何一名首领都会充能同一个槽，充满后所有首领的结界将崩塌为一，展开「空间侵蚀」。当只剩一名首领时，该首领会原样继承剩余的槽。");

        // ================================================================= 도감 카드
        K("여러 보스의 필살기", "Multi-boss ultimate", "複数ボスの必殺技", "多名首领的必杀技");
        K("보스가 둘 이상 함께 있으면 결계 게이지가 하나로 합쳐집니다. 어느 보스를 때려도 같은 게이지가 차고, 가득 차면 모든 보스의 결계가 하나로 무너지는 「공간 침식」이 펼쳐집니다. 15초 동안 맵 거의 전체가 결계가 되고, 모든 보스의 결계 공격이 더 짧은 간격으로 함께 쏟아집니다. 경고 시간은 그대로이고, 천천히 떠도는 「고요의 눈」 안에는 땅을 치는 공격이 떨어지지 않습니다. 보스가 하나만 남으면 남은 게이지를 그 보스가 그대로 이어받습니다.",
          "When two or more bosses are on the field together, their barrier gauges merge into one. Hitting any boss fills the same gauge, and when it is full every boss's barrier collapses into a single 「Spatial Erosion」. For 15 seconds almost the entire map becomes a barrier, and all of the bosses' barrier attacks pour down together at shorter intervals. Warning times stay the same, and ground strikes never land inside the slowly drifting 「Eye of Calm」. When only one boss remains, it inherits the remaining gauge as is.",
          "ボスが2体以上同時にいると、結界ゲージがひとつに合わさります。どのボスを攻撃しても同じゲージがたまり、満タンになるとすべてのボスの結界がひとつに崩れ落ちる「空間侵食」が展開されます。15秒間、マップのほぼ全体が結界となり、すべてのボスの結界攻撃がより短い間隔で一斉に降り注ぎます。警告時間はそのままで、ゆっくり漂う「静寂の目」の中には地面を打つ攻撃が落ちてきません。ボスが1体だけ残ると、残ったゲージをそのボスがそのまま引き継ぎます。",
          "当场上同时有两名以上首领时，结界槽会合而为一。攻击任何一名首领都会充能同一个槽，充满后所有首领的结界将崩塌为一，展开「空间侵蚀」。15秒内，几乎整张地图都会化为结界，所有首领的结界攻击以更短的间隔一同倾泻而下。警告时间保持不变，缓缓漂移的「寂静之眼」内不会落下砸向地面的攻击。当只剩一名首领时，该首领会原样继承剩余的槽。");
    }
}
