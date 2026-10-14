using System.Collections;
using UnityEngine;

// 저주 제단 (1.0.1): 2장부터 장에 들어갈 때 저주를 받을지 고름 — 이번 장 적 · 보스 체력 +30% 대신 경험치 · 코인 보상 +50%
// 잘하는 사람은 더 어렵게 · 더 크게 벌 수 있게 (반복 플레이 동기). 판 중간 저장 'k<장>' (RunSave)
public static class Curse
{
    public static bool Active;
    // 제단을 띄웠지만 아직 고르지 않은 장 (판 중간 저장: 고르기 전에 나가면 이어할 때 다시, 1.0.4~)
    public static int PendingStage = -1;
    public static float HpMul => Active ? 1.3f : 1f;
    public static float RewardMul => Active ? 1.5f : 1f;

    static bool Off => GameMode.IsEndless || TutorialRun.Active || Demo.On || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode;

    // 이 장에서 제단이 나올 예정 (EnterStage 가 맵을 바꾼 직후, 페이드 중 저장용)
    public static void MarkPending(int stage) => PendingStage = Off || Chapters.SlotOf(stage) < 1 ? -1 : stage;

    // StageManager.EnterStage 가 장에 들어간 뒤 부름
    public static IEnumerator Altar(int stage)
    {
        Active = false;
        PendingStage = -1;
        if (Off || Chapters.SlotOf(stage) < 1) yield break;
        PendingStage = stage;
        yield return new WaitForSeconds(1.2f);          // 장 이름 배너를 먼저 보여 줌
        int pick = 1;
        ChoiceUI.Option[] options =
        {
            new ChoiceUI.Option("저주를 받는다", "이번 장 적 · 보스 체력 +30%\n경험치 · 코인 보상 +50%", new Color(0.8f, 0.4f, 1f), "curse"),
            new ChoiceUI.Option("거절한다", "평소대로 싸웁니다", new Color(0.85f, 0.8f, 0.7f), "refuse"),
        };
        yield return ChoiceUI.Run("저주 제단", "위험을 감수하면 더 큰 보상을 얻습니다", options, i => pick = i);
        PendingStage = -1;
        if (pick != 0) yield break;
        Active = true;
        RunSave.Record("k" + stage);
        Hostile.Play("roar", 0.5f, 1.3f);
        Fx.Spawn("fx_soulburst", Hostile.Player != null ? Hostile.Player.transform.position : Vector3.zero, 6f, new Color(0.75f, 0.4f, 1f), 16f);
        if (StageManager.Instance != null) StageManager.Instance.ShowBanner(Loc.T("저주를 받았다!\n적 체력 +30% · 보상 +50%"), 2.5f);
    }
}
