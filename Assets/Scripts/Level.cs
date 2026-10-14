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
        // 레벨업 카드를 모두 배웠거나 최대 레벨이면 만렙 (LevelShop.GrowthOnly) · 1.0.4~: 그 뒤 고른 성장 카드 수를 「+n」으로
        LevelShop shop = Cache<LevelShop>.Get;
        bool max = shop != null && shop.GrowthOnly;
        int grown = shop != null ? shop.GrowthPicks : 0;
        int show = playerLevel * 8192 + grown * 2 + (max ? 1 : 0);
        if (show != shownLevel)
        {
            shownLevel = show;
            levelText.text = "Lv. " + playerLevel + (max ? " MAX" : "") + (grown > 0 ? " <color=#9FE89F>+" + grown + "</color>" : "");
        }
        playerEXP = player.nowEXP;
        playerMaxEXP = player.needEXP;
        if (gauge == null) gauge = EXPgauge.GetComponent<RectTransform>();
        gauge.sizeDelta = new Vector2(playerEXP > 0 ? playerEXP / playerMaxEXP * EXPmaxWidth : 0, gauge.sizeDelta.y);
    }
}
