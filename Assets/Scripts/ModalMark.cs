using UnityEngine;

// 2.0.1~ 열려 있는 모달 창 수 (UIKit.Modal 이 붙임 · UiV2.Modal 을 직접 부르는 ESC 확인 창은 ESC 열림으로 덮임) — ResumeCountdown 이 「목록에 없는 창」도 창으로 봄
//   창이 사라지면(Destroy · 장면 전환) 저절로 줄어듦
public class ModalMark : MonoBehaviour
{
    public static int Count { get; private set; }
    void OnEnable() => Count++;
    void OnDisable() => Count = Mathf.Max(0, Count - 1);
}
