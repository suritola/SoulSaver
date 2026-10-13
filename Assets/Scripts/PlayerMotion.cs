using System.Collections;
using UnityEngine;

// 스킬 · 필살기 몸동작 (1.0.1): 내려찍는 기술이면 실제로 뛰어올라 내려찍고, 회전 베기면 몸이 돌고, 돌진이면 앞으로 늘어나는 등
// 플레이어 그림은 몸통 판정과 같은 오브젝트에 붙어 있어 그림만 움직일 수 없음
// → 동작하는 동안 원래 그림을 숨기고, 매 프레임 그대로 베낀 「인형」 그림을 띄워 그것만 움직임 (판정 · 위치는 그대로)
public class PlayerMotion : MonoBehaviour
{
    SpriteRenderer body, puppet;
    Transform held;                 // 들고 있는 특수 무기 그림 (PlayerLook) — 동작 중에는 숨김
    bool active, heldWasOn;
    Coroutine running;

    // 지금 동작이 그리는 값 (인형의 위치 · 기울기 · 크기)
    Vector2 offset;
    float angle;
    Vector2 scale = Vector2.one;

    public static PlayerMotion Of(Component c)
    {
        if (c == null) return null;
        PlayerMotion m = c.GetComponent<PlayerMotion>();
        return m != null ? m : c.gameObject.AddComponent<PlayerMotion>();
    }

    void Awake()
    {
        body = GetComponent<SpriteRenderer>();
        GameObject go = new GameObject("MotionPuppet");
        go.transform.SetParent(transform, false);
        puppet = go.AddComponent<SpriteRenderer>();
        puppet.enabled = false;
    }

    static float Face(Vector2 dir) => dir.x < 0f ? -1f : 1f;

    // ---------------------------------------------------------------- 동작
    // 뛰어올라 내려찍기: 위로 솟구쳤다 빠르게 떨어지고 땅에서 납작해짐
    public void Leap(float seconds, float height, Vector2 dir) => Play(LeapRoutine(seconds, height, Face(dir)));
    // 땅 두드리기: 작게 들썩였다 쿵 (연달아 내려찍을 때)
    public void Pound(Vector2 dir) => Play(LeapRoutine(0.14f, 0.45f, Face(dir)));
    // 제자리 회전 (회전 베기 · 난사 · 휘몰아치기)
    public void Spin(float seconds, float turns) => Play(SpinRoutine(seconds, turns));
    // 앞으로 몸을 늘이며 찌르듯 (돌진 · 돌격)
    public void Lunge(float seconds, Vector2 dir) => Play(LungeRoutine(seconds, dir));
    // 반동: 쏜 반대쪽으로 튕겼다 돌아옴
    public void Recoil(Vector2 dir, float strength = 1f) => Play(RecoilRoutine(dir, strength));
    // 던지기: 뒤로 젖혔다 앞으로 휙
    public void Throw(float seconds, Vector2 dir) => Play(ThrowRoutine(seconds, dir));
    // 웅크렸다(충전) 큰 반동 (레일건)
    public void Brace(float seconds, Vector2 dir) => Play(BraceRoutine(seconds, dir));
    // 떠올라 힘을 모음 (소환 · 주문)
    public void Cast(float seconds) => Play(CastRoutine(seconds));

    void Play(IEnumerator routine)
    {
        if (!isActiveAndEnabled || body == null) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Wrap(routine));
    }

    IEnumerator Wrap(IEnumerator routine)
    {
        Begin();
        yield return routine;
        End();
        running = null;
    }

    void Begin()
    {
        if (active) return;
        active = true;
        if (held == null) held = transform.Find("HeldWeapon");
        heldWasOn = held != null && held.gameObject.activeSelf;
        if (heldWasOn) held.gameObject.SetActive(false);
        puppet.enabled = true;
    }

    void End()
    {
        offset = Vector2.zero;
        angle = 0f;
        scale = Vector2.one;
        active = false;
        puppet.enabled = false;
        if (body != null) body.enabled = true;
        if (heldWasOn && held != null) held.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        if (active) End();
    }

    // 원래 그림(애니메이션 · 좌우 · 색 · 외곽선 재질)을 그대로 베끼고 동작만 얹음
    void LateUpdate()
    {
        if (!active || body == null) return;
        body.enabled = false;
        puppet.sprite = body.sprite;
        puppet.flipX = body.flipX;
        puppet.flipY = body.flipY;
        puppet.color = body.color;
        puppet.sharedMaterial = body.sharedMaterial;
        puppet.sortingLayerID = body.sortingLayerID;
        puppet.sortingOrder = body.sortingOrder;
        Transform t = puppet.transform;
        Vector3 ls = transform.lossyScale;
        t.localPosition = new Vector3(offset.x / Mathf.Max(0.001f, Mathf.Abs(ls.x)), offset.y / Mathf.Max(0.001f, Mathf.Abs(ls.y)), 0f);
        t.localRotation = Quaternion.Euler(0f, 0f, angle);
        t.localScale = new Vector3(scale.x, scale.y, 1f);
    }

    // ---------------------------------------------------------------- 동작 곡선
    IEnumerator LeapRoutine(float seconds, float height, float face)
    {
        float up = seconds * 0.62f;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (t < up)
            {
                float k = t / up;
                offset = new Vector2(0f, height * Mathf.Sin(k * Mathf.PI * 0.5f));
                angle = 12f * face * k;                                    // 몸을 뒤로 젖히며 솟구침
                scale = new Vector2(1f - 0.08f * k, 1f + 0.12f * k);
            }
            else
            {
                float k = (t - up) / (seconds - up);
                offset = new Vector2(0f, height * (1f - k * k));
                angle = Mathf.Lerp(12f * face, -18f * face, k);            // 앞으로 내려찍음
                scale = new Vector2(1f, 1f + 0.12f * (1f - k));
            }
            yield return null;
        }
        // 착지: 납작해졌다 돌아옴
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            float k = t / 0.12f;
            offset = Vector2.zero;
            angle = Mathf.Lerp(-18f * face, 0f, k);
            scale = new Vector2(Mathf.Lerp(1.3f, 1f, k), Mathf.Lerp(0.72f, 1f, k));
            yield return null;
        }
    }

    IEnumerator SpinRoutine(float seconds, float turns)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = t / seconds;
            angle = -360f * turns * (1f - (1f - k) * (1f - k));
            offset = new Vector2(0f, 0.25f * Mathf.Sin(k * Mathf.PI));
            scale = Vector2.one * (1f + 0.08f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
    }

    IEnumerator LungeRoutine(float seconds, Vector2 dir)
    {
        float face = Face(dir);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = Mathf.Sin(t / seconds * Mathf.PI);
            offset = dir.normalized * 0.35f * k;
            angle = -16f * face * k;                                        // 앞으로 숙임
            scale = new Vector2(1f + 0.3f * k, 1f - 0.18f * k);              // 앞으로 늘어남
            yield return null;
        }
    }

    IEnumerator RecoilRoutine(Vector2 dir, float strength)
    {
        float face = Face(dir);
        const float time = 0.14f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = 1f - t / time;
            offset = -dir.normalized * 0.3f * strength * k;
            angle = 14f * face * strength * k;                               // 뒤로 젖혀짐
            scale = new Vector2(1f - 0.06f * strength * k, 1f + 0.04f * strength * k);
            yield return null;
        }
    }

    IEnumerator ThrowRoutine(float seconds, Vector2 dir)
    {
        float face = Face(dir);
        float back = seconds * 0.55f;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (t < back)
            {
                float k = t / back;
                angle = 22f * face * k;                                      // 뒤로 젖힘
                offset = -dir.normalized * 0.15f * k;
                scale = new Vector2(1f, 1f - 0.08f * k);
            }
            else
            {
                float k = (t - back) / (seconds - back);
                angle = Mathf.Lerp(22f * face, -24f * face, Mathf.Min(1f, k * 2f)) * (1f - Mathf.Max(0f, k - 0.5f) * 2f);
                offset = dir.normalized * 0.3f * Mathf.Sin(k * Mathf.PI);   // 앞으로 휙
                scale = new Vector2(1f + 0.12f * Mathf.Sin(k * Mathf.PI), 1f);
            }
            yield return null;
        }
    }

    IEnumerator BraceRoutine(float seconds, Vector2 dir)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = Mathf.Min(1f, t / 0.15f);
            scale = new Vector2(1f + 0.14f * k, 1f - 0.2f * k);               // 웅크림
            offset = new Vector2(0f, -0.12f * k);
            angle = 0f;
            yield return null;
        }
        yield return RecoilRoutine(dir, 2f);
    }

    IEnumerator CastRoutine(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t / 0.25f)) * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, (seconds - t) / 0.25f));
            offset = new Vector2(0f, 0.7f * rise + 0.08f * Mathf.Sin(t * 10f) * rise);
            angle = 0f;
            scale = Vector2.one * (1f + 0.1f * rise);
            yield return null;
        }
    }
}
