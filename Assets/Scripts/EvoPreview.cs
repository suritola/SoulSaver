using UnityEngine;
using UnityEngine.UI;

// 무기 진화 카드의 미리보기: 그 무기(형태 · 강화)가 어떻게 나가는지 작은 그림으로 계속 되풀이
// 왼쪽 점 = 나, 오른쪽 점 셋 = 적. 게임이 멈춰 있으니 실제 시간으로 움직임
public class EvoPreview : MonoBehaviour
{
    enum Kind { Spray, Beam, Cone, Around, Circle, Chain, Homing, Boomerang, Lunge, Pull, Lob }

    Kind kind;
    Color color;
    int count = 1;              // 발 수 (연사 · 부채꼴)
    float size = 1f;            // 폭발 크기 · 광선 굵기
    bool twice;                 // 한 번 더 (잔상 · 이중 폭발)
    bool sparks;                // 폭죽 불꽃

    static readonly Vector2 Me = new Vector2(-140f, -4f);
    static readonly Vector2[] Foes = { new Vector2(60f, 26f), new Vector2(112f, -22f), new Vector2(150f, 20f) };
    const float Cycle = 1.5f;

    Image meDot, ring, ring2;
    readonly Image[] foe = new Image[3], dot = new Image[8], bar = new Image[3];

    public static void Attach(RectTransform panel, int id, Sprite glow)
    {
        // 1.1~ 각성(무한 모드 3번째 진화)은 수치만 오르는 강화라 공격 그림을 붙이지 않음 (예전엔 셋 다 같은 보라 폭탄)
        if (SpecialAbilities.IsAwaken(id)) return;
        EvoPreview p = panel.gameObject.AddComponent<EvoPreview>();
        p.Setup(id, glow);
    }

    void Setup(int id, Sprite glow)
    {
        Configure(id);
        RectTransform r = (RectTransform)transform;
        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        bg.raycastTarget = false;
        gameObject.AddComponent<RectMask2D>();
        ring = Make(r, glow, 10f);
        ring2 = Make(r, glow, 10f);
        for (int i = 0; i < bar.Length; i++) bar[i] = Make(r, null, 6f);
        for (int i = 0; i < foe.Length; i++) { foe[i] = Make(r, glow, 26f); foe[i].color = new Color(1f, 0.35f, 0.3f); }
        for (int i = 0; i < dot.Length; i++) dot[i] = Make(r, glow, 16f);
        meDot = Make(r, glow, 28f);
        meDot.color = new Color(0.6f, 0.95f, 1f);
    }

    static Image Make(RectTransform parent, Sprite s, float px)
    {
        RectTransform r = UIKit.Rect("P", parent, Vector2.zero, Vector2.one * px);
        Image i = r.gameObject.AddComponent<Image>();
        i.sprite = s;
        i.raycastTarget = false;
        i.enabled = false;
        return i;
    }

    // 무기마다 그림 종류 · 색
    void Configure(int id)
    {
        Color orange = new Color(1f, 0.6f, 0.25f), cyan = new Color(0.55f, 0.95f, 1f), gold = new Color(1f, 0.85f, 0.45f),
              purple = new Color(0.75f, 0.5f, 1f), green = new Color(0.6f, 1f, 0.45f), white = new Color(0.9f, 0.95f, 1f), red = new Color(1f, 0.4f, 0.35f),
              ink = new Color(0.45f, 0.6f, 1f), brass = new Color(1f, 0.72f, 0.4f), sand = new Color(0.95f, 0.85f, 0.55f), paper = new Color(0.95f, 0.88f, 0.72f),
              ice = new Color(0.7f, 0.92f, 1f), shade = new Color(0.62f, 0.5f, 0.9f);
        switch (id)
        {
            case SpecialAbilities.ShotgunId: Set(Kind.Cone, orange, 5, 1f); break;
            case SpecialAbilities.SniperId: Set(Kind.Beam, cyan, 1, 1.4f); break;
            case SpecialAbilities.DualId: Set(Kind.Spray, gold, 4, 1f); break;
            case SpecialAbilities.FlameId: Set(Kind.Cone, orange, 8, 0.95f); break;     // 1.1~ 0.7 → 0.95 (불길이 첫 적까지 닿게)
            case SpecialAbilities.SeekerId: Set(Kind.Homing, purple, 2, 1f); break;
            case SpecialAbilities.ChainId: Set(Kind.Chain, cyan, 1, 1f); break;
            case SpecialAbilities.ScytheId: Set(Kind.Boomerang, purple, 1, 1.4f); break;
            case SpecialAbilities.GrenadeId: Set(Kind.Lob, orange, 1, 1.2f); break;
            case SpecialAbilities.KitHammer: Set(Kind.Around, white, 1, 1.2f); break;
            case SpecialAbilities.KitWhip: Set(Kind.Beam, white, 1, 0.5f); break;
            case SpecialAbilities.KitLance: Set(Kind.Lunge, white, 1, 1f); break;
            case SpecialAbilities.KitBlowgun: Set(Kind.Spray, green, 6, 0.6f); break;
            case SpecialAbilities.KitCards: Set(Kind.Cone, new Color(1f, 0.55f, 0.6f), 3, 1f); break;
            case SpecialAbilities.KitWire: Set(Kind.Chain, white, 1, 1f); break;
            case SpecialAbilities.KitNetBow: Set(Kind.Lob, green, 1, 1f); break;
            case SpecialAbilities.KitJavelin: Set(Kind.Beam, new Color(0.9f, 0.8f, 0.6f), 1, 1.6f); break;
            case SpecialAbilities.KitBurstBow: Set(Kind.Spray, white, 3, 0.8f); break;
            case SpecialAbilities.KitQuicksilver: Set(Kind.Chain, new Color(0.8f, 0.85f, 0.9f), 1, 1f); break;
            case SpecialAbilities.KitMagnet: Set(Kind.Pull, new Color(0.6f, 0.7f, 1f), 1, 1.2f); break;
            case SpecialAbilities.KitFirework: Set(Kind.Lob, gold, 1, 1f); sparks = true; break;
            case CharacterKit.AugStorm: Set(Kind.Around, cyan, 1, 1.5f); break;
            case CharacterKit.AugExecute: Set(Kind.Beam, red, 1, 1.2f); break;
            case CharacterKit.AugThunder: Set(Kind.Chain, gold, 1, 1f); break;
            case CharacterKit.AugEcho: Set(Kind.Spray, purple, 2, 1f); twice = true; break;
            case CharacterKit.AugVenom: Set(Kind.Spray, green, 3, 1f); break;
            case CharacterKit.AugVolley: Set(Kind.Cone, white, 4, 1f); break;
            case CharacterKit.AugBlast: Set(Kind.Lob, orange, 1, 1f); break;
            case CharacterKit.AugSeek: Set(Kind.Homing, green, 2, 1f); break;
            case CharacterKit.AugGale: Set(Kind.Beam, cyan, 1, 0.8f); break;
            case CharacterKit.AugDouble: Set(Kind.Lob, orange, 1, 1f); twice = true; break;
            case CharacterKit.AugGiant: Set(Kind.Lob, green, 1, 1.7f); break;
            case CharacterKit.AugOverload: Set(Kind.Lob, purple, 1, 1.5f); break;      // 연금 폭주
            // 1.1~ 새 캐릭터(지도 제작자 · 시계공 · 우체부 · 유령) 형태 · 강화 — 예전엔 모두 기본값(보라 투척 폭발)이라 세 카드가 같은 그림이었음
            case SpecialAbilities.KitSurveyChain: Set(Kind.Chain, gold, 1, 1f); break;
            case SpecialAbilities.KitInkpot: Set(Kind.Lob, ink, 1, 0.8f); break;
            case SpecialAbilities.KitGlobe: Set(Kind.Pull, new Color(0.5f, 0.9f, 0.8f), 1, 1.5f); break;
            case SpecialAbilities.KitCuckoo: Set(Kind.Lob, brass, 1, 1f); sparks = true; break;
            case SpecialAbilities.KitHourglass: Set(Kind.Boomerang, sand, 1, 1f); break;
            case SpecialAbilities.KitGrandfather: Set(Kind.Around, brass, 1, 1.4f); break;
            case SpecialAbilities.KitPigeon: Set(Kind.Homing, white, 2, 1f); break;
            case SpecialAbilities.KitParcel: Set(Kind.Lob, paper, 1, 1f); break;
            case SpecialAbilities.KitPostbox: Set(Kind.Spray, red, 3, 1f); break;
            case SpecialAbilities.KitSoulChain: Set(Kind.Chain, cyan, 1, 1f); break;
            case SpecialAbilities.KitIceGhost: Set(Kind.Beam, ice, 1, 1.2f); break;
            case SpecialAbilities.KitShadowGhost: Set(Kind.Spray, shade, 1, 1f); twice = true; break;
            case CharacterKit.AugSurvey: Set(Kind.Chain, ink, 1, 1f); break;
            case CharacterKit.AugReprint: Set(Kind.Spray, ink, 2, 1f); twice = true; break;
            case CharacterKit.AugEngrave: Set(Kind.Lob, ink, 1, 1.5f); break;
            case CharacterKit.AugPrecise: Set(Kind.Boomerang, gold, 1, 1f); break;
            case CharacterKit.AugTwinGear: Set(Kind.Spray, brass, 2, 1f); break;
            case CharacterKit.AugRift: Set(Kind.Pull, purple, 1, 1.2f); break;
            case CharacterKit.AugThickEnvelope: Set(Kind.Homing, paper, 1, 1f); break;
            case CharacterKit.AugBundle: Set(Kind.Homing, paper, 3, 1f); break;
            case CharacterKit.AugRegistered: Set(Kind.Chain, red, 1, 1f); break;
            case CharacterKit.AugDeepChill: Set(Kind.Around, ice, 1, 1.3f); break;
            case CharacterKit.AugTwoHands: Set(Kind.Cone, shade, 3, 1f); break;
            case CharacterKit.AugSoulBurst: Set(Kind.Around, new Color(0.8f, 0.7f, 1f), 1, 1.5f); break;
            default: Set(Kind.Lob, purple, 1, 1.5f); break;
        }
    }

    void Set(Kind k, Color c, int n, float s) { kind = k; color = c; count = n; size = s; }

    void Update()
    {
        float t = Time.unscaledTime % Cycle;
        foreach (Image d in dot) d.enabled = false;
        foreach (Image b in bar) b.enabled = false;
        ring.enabled = ring2.enabled = false;
        meDot.enabled = true;
        Vector2 me = Me;
        Vector2[] foes = { Foes[0], Foes[1], Foes[2] };
        bool[] hit = new bool[3];

        switch (kind)
        {
            case Kind.Spray:
                for (int i = 0; i < count && i < dot.Length; i++)
                {
                    float s = t - i * 0.12f;
                    if (s < 0f || s > 0.45f) continue;
                    Vector2 to = foes[i % 3];
                    Dot(i, Vector2.Lerp(me, to, s / 0.45f), 1f);
                    if (s > 0.4f) hit[i % 3] = true;
                    if (twice && i < dot.Length - count)
                    {
                        float s2 = s - 0.25f;
                        if (s2 > 0f) Dot(count + i, Vector2.Lerp(me, to, s2 / 0.45f), 0.6f);
                    }
                }
                break;
            case Kind.Beam:
                {
                    float grow = Mathf.Clamp01(t / 0.15f), fade = 1f - Mathf.Clamp01((t - 0.35f) / 0.4f);
                    if (fade > 0f) { Bar(0, me, me + Vector2.right * 330f * grow, 6f * size, fade); for (int i = 0; i < 3; i++) hit[i] = grow >= 1f && fade > 0.3f; }
                    break;
                }
            case Kind.Cone:
                for (int i = 0; i < count && i < dot.Length; i++)
                {
                    if (t > 0.5f) break;
                    float a = Mathf.Lerp(-22f, 22f, count > 1 ? i / (float)(count - 1) : 0.5f) * Mathf.Deg2Rad;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Dot(i, me + d * 230f * size * (t / 0.5f), 1f - t / 0.5f);
                }
                // 1.1~ 탄이 실제로 닿은 적만 번쩍임 (예전엔 시간으로 정해 탄이 닿기 전 · 닿지 않는 적도 번쩍였음)
                if (t < 0.65f)
                {
                    float reach = 230f * size * Mathf.Clamp01(t / 0.5f);
                    for (int i = 0; i < 3; i++) hit[i] = reach >= Vector2.Distance(me, foes[i]) - 13f;
                }
                break;
            case Kind.Around:
                if (t < 0.5f) { Ring(ring, me, 30f + 150f * size * t / 0.5f, 1f - t / 0.5f); if (t > 0.3f) hit[0] = true; }
                break;
            case Kind.Circle:
            case Kind.Lob:
                {
                    Vector2 at = (foes[0] + foes[1] + foes[2]) / 3f;
                    if (t < 0.45f) Dot(0, Vector2.Lerp(me, at, t / 0.45f) + Vector2.up * Mathf.Sin(t / 0.45f * Mathf.PI) * 40f, 1f);
                    else if (t < 0.95f)
                    {
                        float k = (t - 0.45f) / 0.5f;
                        Ring(ring, at, 20f + 120f * size * k, 1f - k);
                        for (int i = 0; i < 3; i++) hit[i] = k < 0.6f;
                        if (sparks) for (int i = 0; i < 5; i++) { float a = i * 72f * Mathf.Deg2Rad; Dot(1 + i, at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 70f * k, 1f - k); }
                        if (twice && k > 0.5f) Ring(ring2, at, 20f + 90f * (k - 0.5f) * 2f, 1f - (k - 0.5f) * 2f);
                    }
                    break;
                }
            case Kind.Chain:
                {
                    if (t < 0.3f) Dot(0, Vector2.Lerp(me, foes[0], t / 0.3f), 1f);
                    else
                    {
                        float fade = 1f - Mathf.Clamp01((t - 0.9f) / 0.4f);
                        hit[0] = true;
                        if (t > 0.4f) { Bar(0, foes[0], foes[1], 4f, fade); hit[1] = true; }
                        if (t > 0.55f) { Bar(1, foes[1], foes[2], 4f, fade); hit[2] = true; }
                    }
                    break;
                }
            case Kind.Homing:
                for (int i = 0; i < count; i++)
                {
                    if (t > 0.6f) break;
                    float k = t / 0.6f;
                    Vector2 to = foes[i % 3];
                    Vector2 mid = (me + to) / 2f + Vector2.up * (i == 0 ? 70f : -70f);
                    Dot(i, Vector2.Lerp(Vector2.Lerp(me, mid, k), Vector2.Lerp(mid, to, k), k), 1f);
                    if (k > 0.9f) hit[i % 3] = true;
                }
                break;
            case Kind.Boomerang:
                {
                    float k = t < 0.5f ? t / 0.5f : 1f - (t - 0.5f) / 0.5f;
                    if (t < 1f) { Dot(0, me + Vector2.right * 310f * Mathf.Clamp01(k), 1f); dot[0].rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 900f); }
                    for (int i = 0; i < 3; i++) hit[i] = t > 0.2f && t < 0.8f;
                    break;
                }
            case Kind.Lunge:
                {
                    float k = t < 0.2f ? t / 0.2f : 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
                    me = Me + Vector2.right * 70f * k;
                    if (t > 0.1f && t < 0.6f) { Bar(0, me, me + Vector2.right * 160f, 7f, 1f); hit[0] = true; }
                    break;
                }
            case Kind.Pull:
                {
                    Vector2 at = new Vector2(100f, 0f);
                    float k = Mathf.Clamp01((t - 0.35f) / 0.3f);
                    for (int i = 0; i < 3; i++) foes[i] = Vector2.Lerp(Foes[i], at + (Foes[i] - at) * 0.2f, k);
                    if (t < 0.35f) Dot(0, Vector2.Lerp(me, at, t / 0.35f), 1f);
                    else if (t < 0.65f) Ring(ring, at, 160f * (1f - k) + 20f, 0.6f);
                    else if (t < 1.1f) { float b = (t - 0.65f) / 0.45f; Ring(ring2, at, 30f + 110f * b, 1f - b); for (int i = 0; i < 3; i++) hit[i] = b < 0.6f; }
                    break;
                }
        }

        meDot.rectTransform.anchoredPosition = me;
        for (int i = 0; i < 3; i++)
        {
            foe[i].enabled = true;
            foe[i].rectTransform.anchoredPosition = foes[i];
            foe[i].color = hit[i] ? Color.white : new Color(1f, 0.35f, 0.3f);
            foe[i].rectTransform.localScale = Vector3.one * (hit[i] ? 1.25f : 1f);
        }
    }

    void Dot(int i, Vector2 at, float a)
    {
        if (i < 0 || i >= dot.Length) return;
        dot[i].enabled = true;
        dot[i].rectTransform.anchoredPosition = at;
        dot[i].color = new Color(color.r, color.g, color.b, a);
    }

    void Bar(int i, Vector2 a, Vector2 b, float w, float alpha)
    {
        Vector2 d = b - a;
        bar[i].enabled = true;
        RectTransform r = bar[i].rectTransform;
        r.anchoredPosition = (a + b) / 2f;
        r.sizeDelta = new Vector2(d.magnitude, w);
        r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        bar[i].color = new Color(color.r, color.g, color.b, alpha);
    }

    void Ring(Image r, Vector2 at, float radius, float alpha)
    {
        r.enabled = true;
        r.rectTransform.anchoredPosition = at;
        r.rectTransform.sizeDelta = Vector2.one * radius * 2f;
        r.color = new Color(color.r, color.g, color.b, 0.55f * alpha);
    }
}
