using System.Collections.Generic;
using UnityEngine;

// 1.4.1~ 고르기 창 카드 등장 애니메이션 (사용자 「시작할 때 선택해야 하는 창에는 선택 전 카드 등장 애니메이션」)
//   계약 · 각인 → 갈래 입문 창처럼 고르기 창이 연달아 뜰 때, 앞 창에서 누르던 클릭이 다음 창 카드를 잘못 고르던 문제
//   카드가 한 장씩 튀어나오며 커짐 (크기 · 투명도 · 기울기만 바꿔 배치 · 레이아웃은 건드리지 않음) — 끝나는 시각을 돌려주면 창이 그때까지 입력을 막음
//   멈춘 시간(timeScale 0)에도 돌도록 unscaledTime · 꺼지거나 끊기면 원래 모습으로 되돌림 (상태를 남기지 않음)
public sealed class CardIntro : MonoBehaviour
{
    const float Duration = 0.36f;
    public const float Stagger = 0.13f;

    float startAt;
    Vector3 baseScale;
    Quaternion baseRot;
    CanvasGroup group;
    float baseAlpha;
    bool restored;
    bool still;             // 1.4.1~ 설정 「움직임 줄이기」: 크기 · 기울기는 그대로, 투명도만 (다시 뽑기 연출 LevelShop.RerollFx 와 같음)

    // 카드들을 차례로 등장시키고, 마지막 카드가 다 나오는 시각(unscaledTime)을 돌려줌
    public static float Play(IList<Transform> cards, float stagger = Stagger, float firstDelay = 0.08f)
    {
        float now = Time.unscaledTime;
        if (cards == null || cards.Count == 0) return now;
        int n = 0;
        bool still = GameSettings.Motion <= 0f;
        for (int i = 0; i < cards.Count; i++)
        {
            Transform c = cards[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            CardIntro old = c.GetComponent<CardIntro>();
            if (old != null) { old.Restore(); Destroy(old); }
            CardIntro a = c.gameObject.AddComponent<CardIntro>();
            a.still = still;
            a.Begin(now + firstDelay + n * stagger);
            n++;
        }
        return now + firstDelay + Mathf.Max(0, n - 1) * stagger + Duration;
    }

    void Begin(float at)
    {
        startAt = at;
        baseScale = transform.localScale;
        baseRot = transform.localRotation;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        baseAlpha = group.alpha;
        Apply(0f);
    }

    void Update()
    {
        float t = (Time.unscaledTime - startAt) / Duration;
        if (t >= 1f) { Restore(); Destroy(this); return; }
        Apply(Mathf.Max(0f, t));
    }

    void Apply(float t)
    {
        if (group != null) group.alpha = baseAlpha * Mathf.Clamp01(t * 2.5f);
        if (still) { transform.localScale = baseScale; transform.localRotation = baseRot; return; }
        // 살짝 넘쳤다가 제자리 (ease-out-back)
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float u = t - 1f;
        float s = t <= 0f ? 0.001f : 1f + c3 * u * u * u + c1 * u * u;
        transform.localScale = baseScale * Mathf.Max(0.001f, s);
        transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, (1f - t) * -9f);
    }

    void Restore()
    {
        if (restored) return;
        restored = true;
        transform.localScale = baseScale;
        transform.localRotation = baseRot;
        if (group != null) group.alpha = baseAlpha;
    }

    void OnDisable() { Restore(); Destroy(this); }
}
