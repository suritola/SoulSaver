using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Level : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject EXPgauge;
    public float EXPmaxWidth;
    public float playerEXP;
    public float playerMaxEXP;
    public int playerLevel;
    public float bonusEXP = 1.0f;

    public TextMeshProUGUI levelText;
    RectTransform gauge;
    int shownLevel = -1;
    void Start()
    {
        EXPmaxWidth = GetComponent<RectTransform>().sizeDelta.x;
        playerEXP = FindFirstObjectByType<PlayerController>().nowEXP;
        playerMaxEXP = FindFirstObjectByType<PlayerController>().needEXP;
        playerLevel = FindFirstObjectByType<PlayerController>().level;
    }

    // Update is called once per frame
    void Update()
    {

        PlayerController player = Cache<PlayerController>.Get;
        playerLevel = player.level;
        // 레벨업 카드를 모두 배우면 만렙 (LevelShop.NothingToLearn)
        LevelShop shop = Cache<LevelShop>.Get;
        // 1.0.4~: 최대 레벨(LevelShop.LevelCap, 50 · 무한 모드 100)에 닿아도 MAX (경험치 바가 꽉 찬 채 멈춰 버그로 보였음)
        bool max = shop != null && (shop.NothingToLearn || playerLevel >= LevelShop.LevelCap);
        int show = max ? -2 - playerLevel : playerLevel;
        if (show != shownLevel)
        {
            if (max && shownLevel >= 0 && playerLevel >= LevelShop.LevelCap)
                Hints.Show("levelmax", "최대 레벨에 닿았습니다! 더는 레벨이 오르지 않습니다.");       // 1.0.4~ 한 번만
            shownLevel = show;
            levelText.text = "Lv. " + playerLevel + (max ? " MAX" : "");
        }
        playerEXP = player.nowEXP;
        playerMaxEXP = player.needEXP;
        if (gauge == null) gauge = EXPgauge.GetComponent<RectTransform>();
        gauge.sizeDelta = new Vector2(playerEXP > 0 ? playerEXP / playerMaxEXP * EXPmaxWidth : 0, gauge.sizeDelta.y);
    }
}
