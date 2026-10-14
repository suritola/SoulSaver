using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerHP : MonoBehaviour
{

    public GameObject HPgauge;
    // 체력바 위에 표시되는 "현재 / 최대" 텍스트
    public TextMeshProUGUI hpText;
    public float HPmaxWidth;
    public float playerHP;
    public float playerMaxHP;
    RectTransform gauge;
    int shownHp = -1, shownMax = -1, shownGuard = -1;
    bool guardHintTried;

    // Start is called before the first frame update
    void Start()
    {
        HPmaxWidth = GetComponent<RectTransform>().sizeDelta.x;
        playerHP = FindFirstObjectByType<PlayerController>().PlayerHealth;
        playerMaxHP = FindFirstObjectByType<PlayerController>().PlayerMaxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerController player = Cache<PlayerController>.Get;
        playerHP = player.PlayerHealth;
        playerMaxHP = player.PlayerMaxHealth;
        if (gauge == null) gauge = HPgauge.GetComponent<RectTransform>();
        gauge.sizeDelta = new Vector2(playerHP > 0 ? playerHP / playerMaxHP * HPmaxWidth : 0, gauge.sizeDelta.y);

        // 숫자가 바뀔 때만 글자를 새로 (매 프레임 문자열을 만들지 않게)
        int hp = Mathf.CeilToInt(Mathf.Max(playerHP, 0)), max = Mathf.CeilToInt(playerMaxHP);
        int guard = LastStand.Left;
        if (hpText != null && (hp != shownHp || max != shownMax || guard != shownGuard))
        {
            shownHp = hp;
            shownMax = max;
            shownGuard = guard;
            // 1.0.4~: 이번 장에 「최후의 보루」가 남았으면 숫자 옆에 금빛 ◆ (둘 이상이면 x2)
            hpText.text = hp + " / " + max + (guard > 0 ? "  <color=#FFD95A>◆" + (guard > 1 ? "x" + guard : "") + "</color>" : "");
        }
        // 처음 맞았을 때 한 번: ◆ 의 뜻 (튜토리얼 · 힌트 끄기면 Hints 가 알아서 건너뜀)
        if (!guardHintTried && guard > 0 && PlayerController.HurtCount > 0)
        {
            guardHintTried = true;
            Hints.Show("laststand.hud", "체력 숫자 옆 금빛 ◆ 는 이번 장에 남은 「최후의 보루」입니다. 체력이 30% 아래로 떨어지거나 한 방에 쓰러질 피해를 받으면 발동해, 체력 1 이상으로 버티고 2초 동안 무적이 됩니다.");
        }
    }
}
