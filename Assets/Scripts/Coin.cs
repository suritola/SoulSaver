using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public int coins = 0;
    


public TextMeshProUGUI TextInput;




    void Start()
    {
        UpdateCoinText();
    }


    public void AddCoin(int coin)
    {
        coins += coin;
        if (coin > 0) RunCounters.Add("coins", coin);       // 1.2~ 영혼 계약 (모은 코인)
        UpdateCoinText();
    }


    void UpdateCoinText()
    {
        if (TextInput != null) TextInput.text = coins.ToString();

    }

    public void SubCoin(int coin)
    {
        coins -= coin;
        UpdateCoinText();
    }

}
