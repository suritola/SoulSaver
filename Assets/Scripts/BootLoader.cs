using UnityEngine;
using UnityEngine.SceneManagement;

// 2026-11-02~ 게임을 켜면 곧장 메인 메뉴를 동기로 여는 대신, 빈 Boot 씬(빌드 첫 씬)에서 로딩 화면부터 띄우고 메인 메뉴를 비동기로 불러옴
//   (사용자 「게임 키면 바로 불러오는 게 아니라 로딩창부터 띄우고 불러오는 것」) — 로딩 화면 · 진행 막대 · 팁은 SceneFlow 의 씬 전환 화면을 그대로 씀
//   테스트 · 배치 · 트레일러는 SceneFlow.Go 가 알아서 즉시 불러옴
public class BootLoader : MonoBehaviour
{
    // 2.0.0~ 사용자 「로딩할 때 한글이 깨짐」: UIKit.Font 는 장면에 놓인 글자에서 한글 폰트를 찾는데 Boot 씬엔 글자가 없어
    //   SceneFlow 로딩 화면(세션 내내 재사용)이 기본 영문 폰트(LiberationSans)로 만들어졌음 → Boot 씬이 한글 폰트를 직접 들고 먼저 넣어 줌
    public TMPro.TMP_FontAsset font;

    void Awake()
    {
        if (UIKit.Font == null && font != null) { UIKit.Font = font; UIKit.FontMaterial = font.material; }
    }

    void Start()
    {
        SceneFlow.Go("MainMenu");
        StartCoroutine(Watch());
    }

    // 2.0.0~ 검토: SceneFlow 가 어디서든 실패하면(Ensure · Build · 코루틴 예외 · 씬 없음) 입력 없는 빈 Boot 화면에 갇혔음 — 몇 초 뒤에도 Boot 면 메인 메뉴를 직접 불러옴
    System.Collections.IEnumerator Watch()
    {
        // 느린 PC 에서 정상 불러오기가 8초를 넘을 수 있음 → SceneFlow 가 불러오는 중이면 기다리고, 멈췄을 때만 끼어듦 (두 번 불러오지 않게)
        yield return new WaitForSecondsRealtime(8f);
        while (SceneFlow.Loading && SceneManager.GetActiveScene().name == "Boot") yield return new WaitForSecondsRealtime(1f);
        if (SceneManager.GetActiveScene().name == "Boot")
        {
            Debug.LogWarning("[BootLoader] SceneFlow 가 메인 메뉴로 넘기지 못해 직접 불러옴");
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
