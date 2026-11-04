using UnityEngine;

// 번역표 (2026-11-04~ 스킨 시청각 개편): 상점 「바뀌는 칸」 칩 이름 · 칸 수
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddSkinSlots()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        C("skin.slots", "{0}칸", "{0} slots", "{0}枠", "{0}项");
        S("몸", "Body", "体", "身体");
        S("걸음", "Steps", "足音", "脚步");
        S("평타 몸짓", "Attack flourish", "攻撃の所作", "攻击动作");
        S("피격", "Hit", "被弾", "受击");
        S("레벨업", "Level up", "レベルアップ", "升级");
        S("대기", "Idle", "待機", "待机");
        S("필살기 시전", "Ultimate cast", "必殺技発動", "必杀技施放");
        S("사망", "Death", "死亡", "阵亡");
        S("승리", "Victory", "勝利", "胜利");
        S("출발", "Dash-off", "走り出し", "起步");
        S("잔상", "Trail", "残像", "残影");
        S("버팀", "Last stand", "踏ん張り", "坚守");
        S("필살기 가장자리", "Ultimate frame", "必殺技の縁取り", "必杀技边框");
        S("처치 흡수", "Kill absorb", "撃破吸収", "击杀吸收");
        S("오라", "Aura", "オーラ", "光环");
        S("위기", "Crisis", "ピンチ", "危机");
        S("손 무기", "Held weapon", "手の武器", "手持武器");
        S("투사체", "Projectile", "投射物", "投射物");
        S("발사", "Fire", "発射", "发射");
        S("명중음", "Hit sound", "命中音", "命中音");
        S("꼬리", "Trail", "尾", "拖尾");
        S("장전", "Reload", "装填", "装填");
        S("무기 대기", "Weapon idle", "武器待機", "武器待机");
        S("하이라이트", "Highlight", "ハイライト", "高光");
        S("진화", "Evolution", "進化", "进化");
        S("명중", "Hit", "命中", "命中");
        S("처치", "Kill", "撃破", "击杀");
        S("코인", "Coin", "コイン", "金币");
        S("연쇄", "Chain", "連鎖", "连锁");
        S("상자 · 이정표", "Chest · milestone", "宝箱 · 節目", "宝箱 · 里程碑");
        S("큰 적", "Big foe", "大物", "强敌");
        S("가장자리 연쇄", "Chain frame", "連鎖の縁取り", "连锁边框");
        S("출처별 처치", "Kill by source", "撃破元別", "按来源击杀");
        S("화살표 · 조준점", "Arrow · crosshair", "矢印 · 照準", "箭头 · 准星");
        S("클릭", "Click", "クリック", "点击");
        S("호버음", "Hover sound", "ホバー音", "悬停音");
        S("필살기 조준점", "Ultimate crosshair", "必殺技の照準", "必杀技准星");
        S("메뉴 금가루", "Menu gold dust", "メニューの金粉", "菜单金粉");
    }
}
