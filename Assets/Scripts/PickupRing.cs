using UnityEngine;
using UnityEngine.SceneManagement;

// 1.1.9~ 코인 획득 범위 고리 (편의성 3차 3): 실제로 끌어오는 범위(coinMagnetRange + 기본 2.5칸)를 캐릭터 둘레에 연한 원으로
// 설정 「획득 범위 표시」면 늘 아주 연하게(알파 0.18) · 꺼져 있어도 코인 자석 카드를 고른 직후엔 옛 반지름 → 새 반지름으로 0.4초 커지며 2.5초 보임
// 레벨업 창은 시간이 멈춰 있으므로 Time.time 기준 — 창을 닫은 뒤에 시작 · 내 이펙트 불투명도를 곱함 · 연출 중엔 숨김
public class PickupRing : MonoBehaviour
{
    static PickupRing instance;
    PlayerController player;
    LineRenderer ring;
    float flashAt = -99f, fromRadius, toRadius;
    float shownRadius = -1f, shownAlpha = -1f;
    const float FlashLen = 2.5f, GrowLen = 0.4f;
    static readonly Color Tint = new Color(1f, 0.85f, 0.4f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) => { if (s.name == "GameScene") Ensure(); };
        if (SceneManager.GetActiveScene().name == "GameScene") Ensure();
    }

    static PickupRing Ensure()
    {
        if (instance != null) return instance;
        PlayerController p = Object.FindFirstObjectByType<PlayerController>();
        if (p == null) return null;
        instance = p.gameObject.AddComponent<PickupRing>();
        instance.player = p;
        return instance;
    }

    // 코인 자석 카드를 고른 직후 (LevelShop) — oldRange: 고르기 전 coinMagnetRange
    public static void Flash(float oldRange)
    {
        PickupRing r = Ensure();
        if (r == null || r.player == null) return;
        r.fromRadius = oldRange + PlayerController.BaseCoinPull;
        r.toRadius = r.player.coinMagnetRange + PlayerController.BaseCoinPull;
        r.flashAt = Time.time;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (ring != null) Destroy(ring.gameObject);
    }

    void LateUpdate()
    {
        if (player == null) return;
        float radius = player.coinMagnetRange + PlayerController.BaseCoinPull;
        float a = GameSettings.ShowPickupRange ? 0.18f : 0f;
        float since = Time.time - flashAt;
        if (since >= 0f && since < FlashLen)
        {
            float grow = Mathf.Clamp01(since / GrowLen);
            radius = Mathf.Lerp(fromRadius, toRadius, 1f - (1f - grow) * (1f - grow));
            float fade = since < FlashLen - 0.6f ? 1f : (FlashLen - since) / 0.6f;
            a = Mathf.Max(a, 0.55f * fade);
        }
        bool hide = PlayerController.Cinematic || player.IsDying || StoryDirector.Playing || GameInput.Auto;
        a *= GameSettings.MyFxAlpha;                    // 내 이펙트 취급
        if (hide || a <= 0.001f)
        {
            if (ring != null && ring.enabled) ring.enabled = false;
            return;
        }
        if (ring == null)
        {
            ring = Hostile.NewLine("PickupRing", Tint, 0.06f, 5);      // 「Effect」 순서 5 = 바닥 빛 위 · 적 아래
            ring.loop = true;
            ring.useWorldSpace = false;     // 점은 고리 원점 기준 · 위치만 플레이어를 따라감 (줄 메시를 프레임마다 다시 만들지 않게)
            shownRadius = shownAlpha = -1f;
        }
        ring.enabled = true;
        ring.transform.position = player.transform.position;
        if (!Mathf.Approximately(a, shownAlpha)) { shownAlpha = a; ring.startColor = ring.endColor = new Color(Tint.r, Tint.g, Tint.b, a); }
        if (!Mathf.Approximately(radius, shownRadius)) { shownRadius = radius; Hostile.SetArc(ring, Vector3.zero, radius, 0f, 360f); }
    }
}
