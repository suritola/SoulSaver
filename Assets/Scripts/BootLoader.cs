using UnityEngine;

// 2026-11-02~ 게임을 켜면 곧장 메인 메뉴를 동기로 여는 대신, 빈 Boot 씬(빌드 첫 씬)에서 로딩 화면부터 띄우고 메인 메뉴를 비동기로 불러옴
//   (사용자 「게임 키면 바로 불러오는 게 아니라 로딩창부터 띄우고 불러오는 것」) — 로딩 화면 · 진행 막대 · 팁은 SceneFlow 의 씬 전환 화면을 그대로 씀
//   테스트 · 배치 · 트레일러는 SceneFlow.Go 가 알아서 즉시 불러옴
public class BootLoader : MonoBehaviour
{
    void Start()
    {
        SceneFlow.Go("MainMenu");
    }
}
