using UnityEngine;

// 2.0.0~ 특별 모드 공통 뼈대 (docs/plans/2026-11-02-big-update/modes.md 0절)
// 영혼 릴레이 · 역류는 일일 도전처럼 GameMode.Override = Endless · CharacterData.Override 를 걸고 무한 모드 판 위에서 돎
// 기존 코드가 DailyChallenge.Active 를 따로 묻던 곳 중 「무한 모드 기록과 섞지 않음」 뜻인 곳은 여기 Special 하나로 묻는다
public enum ModeKind { None, Daily, Relay, Reflux }

public static class ModeRun
{
    public static ModeKind Kind => DailyChallenge.Active ? ModeKind.Daily : RelayRun.Active ? ModeKind.Relay : RefluxRun.Active ? ModeKind.Reflux : ModeKind.None;
    public static bool Special => Kind != ModeKind.None;          // 무한 모드 기록 · 이정표 · 무한 순위표에 섞지 않음
    public static bool Relay => RelayRun.Active;
    public static bool Reflux => RefluxRun.Active;

    // 메인 메뉴 진입 (MenuExtras.InstallMainMenu): 어느 모드든 풀어 GameMode · CharacterData Override 를 원래대로
    // 2.0.0~ 검토: GameScene 을 (다시) 불러오기 직전 — 「다시 시작」 경로가 모두 SceneFlow.Go 를 지나므로 여기 하나에서 모드 판 상태를 새로
    public static void BeforeGameScene()
    {
        RelayRun.BeforeGameScene();
        RefluxRun.BeforeGameScene();
    }

    public static void EndAll()
    {
        DailyChallenge.End();
        RelayRun.End();
        RefluxRun.End();
    }
}
