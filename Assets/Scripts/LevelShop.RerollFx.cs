using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 카드 다시 뽑기 연출 (1.0.1): 카드가 반짝이며 부풀고 → 불꽃이 사방으로 터지며 → 새 카드가 좌우로 뒤집히듯 나타남
// 세 장이 조금씩 엇갈려 차례로 바뀜. 연출 동안(약 0.45초)은 카드를 고르지 못하게 (바뀌는 도중 잘못 고르지 않게)
// 레벨업 창은 멈춘 시간(timeScale 0)이라 실제 시간으로 움직임
public partial class LevelShop
{
    Coroutine rerollFx;

    void PlayRerollFx()
    {
        if (rerollFx != null) StopCoroutine(rerollFx);
        rerollFx = StartCoroutine(RerollFx());
    }

    IEnumerator RerollFx()
    {
        selectReady = false;
        // 1.5.9~ 잠금은 시각으로 — selectReady 만 끄면 다음 프레임 UpdateSelectLock 이 바로 풀어 [R] 연타 · 배지 두 번에 다시 뽑기가 2회 빠졌음
        //   (풀기는 UpdateSelectLock 에 맡김 · 연출이 끊겨도 시각이 지나면 저절로 풀림 · 다음 창은 openLevelShop 이 introEnd 를 새로 씀)
        introEnd = Mathf.Max(introEnd, Time.unscaledTime + 0.45f);
        Hostile.Play("shimmer", 0.8f, 1.4f);
        Hostile.Play("pop", 0.7f, 1.1f);
        for (int slot = 0; slot < 3; slot++)
        {
            RectTransform card = CardOf(slot) as RectTransform;
            if (card != null) StartCoroutine(CardBurst(card, slot * 0.06f));
        }
        yield return new WaitForSecondsRealtime(0.45f);
        rerollFx = null;
    }

    IEnumerator CardBurst(RectTransform card, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        if (card == null) yield break;
        Vector3 baseScale = Vector3.one;
        // 1.1.9~ 「움직임 줄이기」: 부풂 · 뒤집기 · 불꽃 없이 0.15초 흰빛 페이드만 (잠금 0.45초는 그대로)
        if (GameSettings.Motion <= 0f)
        {
            GameObject qf = new GameObject("RerollFlash", typeof(RectTransform), typeof(Image));
            RectTransform qr = qf.GetComponent<RectTransform>();
            qr.SetParent(card, false);
            qr.anchorMin = Vector2.zero; qr.anchorMax = Vector2.one;
            qr.offsetMin = qr.offsetMax = Vector2.zero;
            qr.SetAsLastSibling();
            Image qi = qf.GetComponent<Image>();
            qi.raycastTarget = false;
            for (float t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
            {
                if (card == null) { Destroy(qf); yield break; }
                qi.color = new Color(1f, 0.97f, 0.85f, 0.6f * (1f - t / 0.15f));
                yield return null;
            }
            Destroy(qf);
            yield break;
        }

        // 카드 위 흰 번쩍임 (가장 위에, 누르는 데 방해되지 않게)
        GameObject fl = new GameObject("RerollFlash", typeof(RectTransform), typeof(Image));
        RectTransform fr = fl.GetComponent<RectTransform>();
        fr.SetParent(card, false);
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        fr.SetAsLastSibling();
        Image flash = fl.GetComponent<Image>();
        flash.raycastTarget = false;
        flash.color = new Color(1f, 0.97f, 0.85f, 0f);

        // 1) 부풀며 하얘짐 (0.12초)
        for (float t = 0f; t < 0.12f; t += Time.unscaledDeltaTime)
        {
            if (card == null) { Destroy(fl); yield break; }
            float k = t / 0.12f;
            card.localScale = baseScale * (1f + 0.12f * k);
            flash.color = new Color(1f, 0.97f, 0.85f, 0.9f * k);
            yield return null;
        }
        // 2) 사방으로 불꽃이 터짐
        Sparks(card, 14);
        Hostile.Play("pop", 0.5f, 1.5f + Random.Range(-0.1f, 0.1f));
        // 3) 새 카드가 좌우로 뒤집히듯 펼쳐지며 살짝 튐 (0.3초)
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            if (card == null) { Destroy(fl); yield break; }
            float k = t / 0.3f;
            float flip = Mathf.Abs(Mathf.Cos(k * Mathf.PI));          // 1 → 0 → 1 : 뒤집힘
            float bounce = 1f + 0.08f * Mathf.Sin(k * Mathf.PI);
            card.localScale = new Vector3(baseScale.x * Mathf.Max(0.05f, flip) * bounce, baseScale.y * bounce, 1f);
            flash.color = new Color(1f, 0.97f, 0.85f, 0.9f * (1f - k));
            yield return null;
        }
        if (card != null) card.localScale = baseScale;
        Destroy(fl);
    }

    // 카드 가운데에서 금빛 · 흰빛 작은 별이 사방으로 날아가며 사라짐
    void Sparks(RectTransform card, int count)
    {
        Sprite glow = SpecialAbilities.GlowSprite;
        Transform parent = card.parent != null ? card.parent : card;
        for (int i = 0; i < count; i++)
        {
            GameObject g = new GameObject("RerollSpark", typeof(RectTransform), typeof(Image));
            RectTransform r = g.GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.position = card.position;
            r.sizeDelta = Vector2.one * Random.Range(18f, 34f);
            Image img = g.GetComponent<Image>();
            img.sprite = glow;
            img.raycastTarget = false;
            img.color = Random.value < 0.5f ? new Color(1f, 0.85f, 0.4f, 1f) : new Color(1f, 1f, 1f, 1f);
            float a = (i + Random.Range(-0.3f, 0.3f)) / count * Mathf.PI * 2f;
            Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(380f, 620f);
            StartCoroutine(SparkFly(r, img, v));
        }
    }

    static IEnumerator SparkFly(RectTransform r, Image img, Vector2 v)
    {
        const float life = 0.45f;
        Color c0 = img.color;
        Vector2 size0 = r.sizeDelta;
        for (float t = 0f; t < life; t += Time.unscaledDeltaTime)
        {
            if (r == null) yield break;
            float k = t / life;
            r.anchoredPosition += v * Time.unscaledDeltaTime * (1f - k);
            r.sizeDelta = size0 * (1f - 0.6f * k);
            img.color = new Color(c0.r, c0.g, c0.b, 1f - k);
            yield return null;
        }
        if (r != null) Destroy(r.gameObject);
    }
}
