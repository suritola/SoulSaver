// 번역표 (1.2.3~): 무한 모드 지형 변화 「왕들의 영토」 (EndlessTerrain)
// LocTable.Skills 의 정적 생성자가 부름 (이미 있는 키는 건드리지 않음)
public static partial class LocTable
{
    static void AddTerrain()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }

        A("{0}의 영토가 사막을 침식한다!", "The {0}'s domain seeps into the desert!", "{0}の領土が砂漠を侵食する！", "{0}的领地正在侵蚀沙漠！");
        A("묘비가 솟아 길과 탄을 막습니다. 보스가 지나가면 부서집니다.",
          "Tombstones rise to block paths and shots. A boss walking through shatters them.",
          "墓石がそびえ、道と弾を遮ります。ボスが通ると砕けます。",
          "墓碑耸立，阻挡道路与子弹。首领经过时会将其击碎。");
        A("용암 웅덩이가 끓어오릅니다. 밟으면 아프고, 적은 느려집니다.",
          "Lava pools bubble up. They burn you, and enemies wading through slow down.",
          "溶岩の水たまりが煮えたぎります。踏むと痛く、敵は遅くなります。",
          "熔岩池沸腾翻滚。踩上会受伤，敌人穿过时会变慢。");
        A("점액 늪이 번집니다. 안에서는 모두가 느려집니다.",
          "Slime bogs spread. Everything inside moves slowly.",
          "粘液の沼が広がります。中では誰もが遅くなります。",
          "黏液沼泽蔓延。身处其中的一切都会变慢。");
        A("영혼 수정이 솟아 길과 탄을 막습니다. 보스가 지나가면 부서집니다.",
          "Soul crystals rise to block paths and shots. A boss walking through shatters them.",
          "魂の水晶がそびえ、道と弾を遮ります。ボスが通ると砕けます。",
          "灵魂水晶耸立，阻挡道路与子弹。首领经过时会将其击碎。");
        A("보스를 쓰러뜨리면 그 왕의 영토가 쓰러진 자리에서 퍼집니다. 묘비 · 수정은 길과 탄을 막고, 용암은 밟으면 아프며, 점액은 모두를 느리게 합니다.",
          "When a boss falls, its domain spreads from where it died. Tombstones and crystals block paths and shots, lava burns you, and slime slows everyone.",
          "ボスを倒すと、その王の領土が倒れた場所から広がります。墓石・水晶は道と弾を遮り、溶岩は踏むと痛く、粘液は皆を遅くします。",
          "击败首领后，其领地会从倒下之处蔓延。墓碑与水晶阻挡道路和子弹，熔岩会灼伤你，黏液使所有人变慢。");
        A("모래 폭풍이 지형을 집어삼켰다", "A sandstorm has swallowed the terrain", "砂嵐が地形を飲み込んだ", "沙暴吞没了地形");
        A("용암 웅덩이", "Lava pool", "溶岩の水たまり", "熔岩池");
        A("지형 변화", "Terrain shifts", "地形変化", "地形变化");
        A("왕들의 영토", "Domains of the Kings", "王たちの領土", "诸王之领");
        A("쓰러진 왕의 영토가 사막을 침식합니다. 보스를 쓰러뜨릴 때마다 지형이 바뀝니다.",
          "The fallen king's domain seeps into the desert. The terrain shifts with every boss you defeat.",
          "倒れた王の領土が砂漠を侵食します。ボスを倒すたびに地形が変わります。",
          "倒下之王的领地侵蚀着沙漠。每击败一名首领，地形都会改变。");
    }
}
