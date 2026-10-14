using TMPro;
using UnityEngine;

public class Point : MonoBehaviour
{
    public int points = 0;

    public TextMeshProUGUI TextInput;

    void Start()
    {
        UpdatePointText();
    }

    public void AddPoint(int point)
    {
        points += point;
        // 캐릭터 구매에 쓰는 포인트로도 쌓임 (게임을 꺼도 유지)
        CharacterData.AddPoints(point);

        UpdatePointText();

    }

    void UpdatePointText()
    {
        if (TextInput != null) TextInput.text = Loc.T("포인트") + " : " + points;      // 1.0.4~ 번역 없이 「POINT : 」였음 (판 안의 코인과 다른, 영웅 · 스킨을 사는 재화)
    }
}