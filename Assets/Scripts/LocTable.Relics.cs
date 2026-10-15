using UnityEngine;

// 번역표 (1.2~): 봉인된 석관 · 유물 · 부서지는 맵 물체 · 떨어지는 것 (MapProps · SpecialAbilities.Relics · ContentRegistry)
// 열쇠는 relic. · prop. · drop. 접두어 (한국어 원문은 0번 칸) · 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddRelics()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 유물 칸 · 설정
        C("유물", "유물", "Relic", "遺物", "遗物");
        C("relic.title", "봉인된 석관 · 유물 고르기", "Sealed Sarcophagus · Choose a Relic", "封印された石棺・遺物を選ぶ", "封印的石棺 · 选择遗物");
        C("relic.pick", "[{RELIC}] 유물 칸에 장착합니다 · 쿨타임 1.5배 · 한 판에 최대 2개",
          "Equips to the [{RELIC}] relic slot · 1.5× cooldown · Up to 2 per run",
          "[{RELIC}] 遺物スロットに装備・クールタイム1.5倍・1回のプレイで最大2個",
          "装备到 [{RELIC}] 遗物栏 · 冷却时间1.5倍 · 每局最多2个");
        C("relic.tag", "유물 [{RELIC}] · 쿨타임 1.5배", "Relic [{RELIC}] · 1.5× cooldown", "遺物 [{RELIC}]・クールタイム1.5倍", "遗物 [{RELIC}] · 冷却时间1.5倍");
        C("relic.equip", "유물 장착: {0}", "Relic equipped: {0}", "遺物を装備：{0}", "已装备遗物：{0}");
        C("유물은 [{RELIC}]로 씁니다. 두 개를 가지면 준비된 것부터 나갑니다. 쿨타임은 1.5배입니다.",
          "유물은 [{RELIC}]로 씁니다. 두 개를 가지면 준비된 것부터 나갑니다. 쿨타임은 1.5배입니다.",
          "Use relics with [{RELIC}]. With two, whichever is ready fires first. Relic cooldowns are 1.5×.",
          "遺物は [{RELIC}] で使います。2つ持つと準備できた方から発動します。クールタイムは1.5倍です。",
          "按 [{RELIC}] 使用遗物。拥有两个时，先就绪的先发动。冷却时间为1.5倍。");

        // ================================================================= 맵 물체 · 석관
        C("prop.urn.name", "낡은 항아리", "Old Urn", "古びた壺", "破旧陶罐");
        C("prop.brazier.name", "꺼진 화로", "Cold Brazier", "消えた火鉢", "熄灭的火盆");
        C("prop.barrel.name", "나무통", "Wooden Barrel", "木の樽", "木桶");
        C("prop.crystal.name", "영혼 수정", "Soul Crystal", "魂の水晶", "灵魂水晶");
        C("prop.sarco.name", "봉인된 석관", "Sealed Sarcophagus", "封印された石棺", "封印的石棺");
        C("prop.sarco.appear", "봉인된 석관이 나타났다!\n화살표를 따라가 곁에 머물러 봉인을 부수면 유물을 얻습니다",
          "A Sealed Sarcophagus appeared!\nFollow the arrow and stay beside it to break the seal for a relic",
          "封印された石棺が現れた！\n矢印をたどり、そばにとどまって封印を破ると遺物が手に入ります",
          "封印的石棺出现了！\n跟随箭头，停留在旁边破除封印即可获得遗物");
        C("prop.sarco.empty", "석관은 비어 있었다...\n코인 +{0}", "The sarcophagus was empty...\nCoins +{0}", "石棺は空っぽだった……\nコイン +{0}", "石棺里空空如也……\n金币 +{0}");
        C("맵 곳곳의 물체에 부딪치면 부서지며 코인 · 고기 · 자석 같은 것이 나옵니다.",
          "맵 곳곳의 물체에 부딪치면 부서지며 코인 · 고기 · 자석 같은 것이 나옵니다.",
          "Bump into objects around the map to break them and find coins, meat, magnets and more.",
          "マップのあちこちにある物にぶつかると壊れて、コイン・肉・磁石などが出てきます。",
          "撞上地图各处的物体即可将其打碎，掉出金币 · 肉 · 磁铁等物品。");

        // ================================================================= 떨어지는 것
        C("drop.coin.name", "코인", "Coins", "コイン", "金币");
        C("drop.meat.name", "고기", "Meat", "肉", "肉");
        C("drop.gold.name", "금화", "Gold Coin", "金貨", "大金币");
        C("drop.magnet.name", "자석", "Magnet", "磁石", "磁铁");
        C("drop.timestop.name", "시간 정지", "Time Stop", "時間停止", "时间停止");
        C("drop.purify.name", "정화", "Purify", "浄化", "净化");
        C("drop.meat.text", "체력 +{0}", "HP +{0}", "体力 +{0}", "生命 +{0}");
        C("drop.magnet.text", "자석! 코인이 모두 날아온다", "Magnet! All coins fly to you", "磁石！コインが全部飛んでくる", "磁铁！金币全部飞过来");
        C("drop.timestop.text", "시간 정지!", "Time Stop!", "時間停止！", "时间停止！");
        C("drop.purify.text", "정화! 적 {0}마리", "Purify! {0} foes", "浄化！敵 {0}体", "净化！敌人 {0} 名");

        // ================================================================= 설정 → 조작 게임패드 안내 (Y 무기 교체 → Y 유물)
        C("왼쪽 스틱 이동 · 오른쪽 스틱 조준(기울이면 자동 공격) · RT 공격 · LT 필살기 · A 상호작용 · 확정 · B 취소 · X 재장전 · Y 유물\nRB · LB · L3 스킬 1 · 2 · 3 · View 영혼 트리 · Menu 일시정지 · 메뉴에서는 왼쪽 스틱 · 십자키로 커서, A 클릭, 오른쪽 스틱 스크롤",
          "",
          "Left stick Move · Right stick Aim (tilt to auto-attack) · RT Attack · LT Ultimate · A Interact · Confirm · B Cancel · X Reload · Y Relic\nRB · LB · L3 Skill 1 · 2 · 3 · View Soul Tree · Menu Pause · In menus: left stick · D-pad move the cursor, A click, right stick scroll",
          "左スティック 移動・右スティック 照準（倒すと自動攻撃）・RT 攻撃・LT 必殺技・A 調べる・決定・B キャンセル・X リロード・Y 遺物\nRB・LB・L3 スキル 1・2・3・View 魂のツリー・Menu 一時停止・メニューでは左スティック・十字キーでカーソル、A クリック、右スティック スクロール",
          "左摇杆 移动 · 右摇杆 瞄准（推动即自动攻击） · RT 攻击 · LT 必杀技 · A 互动 · 确认 · B 取消 · X 换弹 · Y 遗物\nRB · LB · L3 技能 1 · 2 · 3 · View 灵魂树 · Menu 暂停 · 菜单中用左摇杆 · 十字键移动光标，A 点击，右摇杆滚动");
    }
}
