using System.Collections.Generic;
using UnityEngine;

// 떨어진 코인 목록: 코인 자석이 매 프레임 씬 전체를 태그로 뒤지지 않도록 코인이 스스로 등록 · 해제
public class CoinTag : MonoBehaviour
{
    public static readonly List<Transform> All = new List<Transform>();

    public static void Register(GameObject coin)
    {
        if (coin != null && coin.GetComponent<CoinTag>() == null) coin.AddComponent<CoinTag>();
        PickupGlow.Add(coin, new Color(1f, 0.82f, 0.3f), 1.1f);       // 1.0.2: 스킬 이펙트에 묻히지 않게
    }

    void OnEnable() => All.Add(transform);
    void OnDisable() => All.Remove(transform);
}
