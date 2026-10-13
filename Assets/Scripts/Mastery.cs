using UnityEngine;

// 영웅 숙련도 (1.0.1): 그 영웅으로 마지막 보스를 쓰러뜨릴 때마다 숙련 +1 (최대 3)
// 숙련 1단계마다 판을 시작할 때 레벨업 카드 한 장을 더 고름 (경험치 요구량은 그대로) · 영웅 선택 화면에 「숙련 n/3」
// 판 기록에서 두 영웅만 플레이돼서, 다른 영웅도 키워 보고 싶게
public static class Mastery
{
    public const int Max = 3;
    static string Key(CharacterId id) => "mastery." + (int)id;

    public static int Level(CharacterId id) => Mathf.Clamp(Prefs.GetInt(Key(id), 0), 0, Max);

    // 엔딩 (StageManager: 마지막 보스 처치) · 무한 모드 · 튜토리얼 · 자동 플레이는 세지 않음
    public static bool OnCleared(CharacterId id)
    {
        if (GameMode.IsEndless || TutorialRun.Active || GameInput.Auto || Application.isBatchMode) return false;
        int lv = Level(id);
        if (lv >= Max) return false;
        Prefs.SetInt(Key(id), lv + 1);
        Prefs.Save();
        return true;
    }

    // 「숙련 2/3」 (특수 문자는 글꼴에 없을 수 있어 글자로)
    public static string Label(CharacterId id) => Loc.T("숙련 {0}/{1}").Replace("{0}", Level(id).ToString()).Replace("{1}", Max.ToString());
}
