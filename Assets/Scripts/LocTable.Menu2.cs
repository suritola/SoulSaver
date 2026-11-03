using UnityEngine;

// 번역표 (2.0.1~): 메인 메뉴 개편 — 도전 모드(일일 도전 · 영혼 릴레이 · 역류 한 창) · 영웅 · 스킨 · 튜토리얼 다시 하기
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddMenu2()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        S("도전 모드", "Challenges", "チャレンジ", "挑战模式");
        S("영웅 · 스킨", "Heroes · Skins", "ヒーロー・スキン", "英雄 · 皮肤");
        S("튜토리얼 다시 하기", "Replay Tutorial", "チュートリアルをもう一度", "重玩教程");
        C("mode.card.daily", "매일 바뀌는 영웅과 규칙으로 하루 한 판. 모두 같은 조건에서 순위를 겨룹니다.",
          "A new hero and rule every day. Everyone plays the same setup and competes on one leaderboard.",
          "毎日変わるヒーローとルールで1日1プレイ。全員が同じ条件で順位を競います。",
          "每天更换英雄与规则，一天一局。所有人在相同条件下争夺排名。");
    }
}
