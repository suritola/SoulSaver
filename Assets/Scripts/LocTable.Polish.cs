using UnityEngine;

// 번역표 (문구 다듬기 2026-10-16): 코드에는 있지만 표에 빠져 있던 문구
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddPolish()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // 보스를 쓰러뜨려 다음 장 문이 열릴 때 큰 알림 둘째 줄 (StageManager.Gate) — 같은 배열의 다른 세 줄(LocTable.cs)과 같은 모양
        C("성문 너머로 초원이 보인다", "성문 너머로 초원이 보인다.",
          "A meadow lies beyond the gate.", "城門の向こうに草原が見える。", "城门外是一片草原。");
    }
}
