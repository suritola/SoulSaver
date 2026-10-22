using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 적 · 보스가 피해를 입으면 머리 위로 떠올랐다 사라지는 피해 숫자
// 같은 적에게 짧은 사이에 연달아 들어온 피해(도트 · 연사)는 숫자 하나로 합침
// 합산 창은 체력바 타격감(HealthBarFeel)과 같은 값 — 숫자에 보이는 수 = 등급을 매긴 수 · 플레이어가 받은 피해도 여기서 (ShowHurt)
public class DamagePopup : MonoBehaviour
{
    const float Life = 0.75f, Rise = 1.3f;
    // 2.2.0: 보스 피해가 거의 안 보였음 (큰 몸 꼭대기 위 · 화면 밖에 작게 떴다 금방 사라짐)
    // → 몸 위쪽 안에서, 화면 안으로 맞춰, 1.8배 크게 · 주황 금색 · 두꺼운 테두리 · 더 오래, 연타는 더 길게 모아 큰 숫자로
    const float BossLife = 1.15f, BossRise = 1.8f, BossSize = 1.45f;     // 1.2.2~ 1.8 → 1.45 (숫자 크기 줄임)
    // 플레이어가 받은 피해: 빨강 "-N" · 머리 위 조금 낮게 (체력바 조각과 눈이 같이 가게) · 적 숫자 위에
    const float HurtLife = 1.0f, HurtSize = 1.1f, HurtHeight = 1.6f;     // 1.2.2~ 1.3 → 1.1
    const int HurtOrder = 50;
    const int MaxActive = 80;
    // 그래픽 개편 1단계-10 (2026-10-22~): 적이 받은 피해 숫자는 동시에 32개까지 (80개가 적 무리 위를 덮었음 · 설정 「피해 숫자」와 별개) — 내가 받은 피해 숫자는 그대로 80
    static int EnemyCap => VisualRules.Legacy ? MaxActive : 32;

    static TMP_FontAsset font;
    static Material material, bossMaterial;
    static readonly Dictionary<int, DamagePopup> byTarget = new Dictionary<int, DamagePopup>();
    // 빈틈 숫자도 대상마다 하나로 합침 (일반 숫자와는 따로 · 열린 빈틈을 연사로 맞히면 한 대마다 팝업이 생겨 MaxActive 를 채웠음)
    static readonly Dictionary<int, DamagePopup> byWeak = new Dictionary<int, DamagePopup>();
    static int active;

    // 다음 피해가 치명타인지 (총알 · 피해 훅이 피해를 주기 직전에 켜 둠, Show 가 읽고 끔)
    public static bool NextCrit;
    // 다음 피해가 빈틈 명중인 대상 (WeakHit.Apply 가 켜고 Show 가 그 대상일 때만 읽고 끔 · bool 이면 TakeDamage 안의 다른 적 숫자가 가로챔)
    public static Transform NextWeakTarget;
    public static bool NextWeakPerfect;     // 「완벽한 빈틈!」 (궁수 완벽 · 시계공 정각)

    TextMeshPro text;
    int key;
    bool crit, boss, hurt, weak, weakPerfect;
    int tierMax;                 // 합친 피해 중 가장 높은 등급 (올라만 감)
    float amount, t, lastAdd, firstAdd, size = 1f;     // firstAdd: 첫 합산 시각 (HealthBarFeel.WindowMax 가 지나면 새 숫자)
    // 1.1~: 합칠 때마다 t 를 0 으로 되돌려 떠오르던 숫자가 처음 자리로 툭 떨어지며 덜덜 떨렸음
    //   → 떠오르기(riseT)는 이어서, 수명(t)만 늘림 · 합칠 때 튀는 크기는 작게
    float riseT, popT, popAmp;
    Vector3 start;
    // 1.5.5~: 숫자가 맞은 자리에 멈춰 있어 움직이는 적과 떨어졌음 → 맞은 대상을 따라감 (대상이 죽어 꺼지면 그 자리에서 마저 떠오름)
    Transform follow;
    Vector3 followOffset;

    public static void Show(Transform target, float damage, SpriteRenderer body, bool fromBoss = false, HealthBarFeel.Tier tier = HealthBarFeel.Tier.Small)
    {
        bool isCrit = NextCrit;
        NextCrit = false;
        bool isWeak = target != null && NextWeakTarget == target, weakPerf = isWeak && NextWeakPerfect;
        if (isWeak) { NextWeakTarget = null; NextWeakPerfect = false; }
        if (target == null || damage <= 0f || GameSettings.DamageNumbers == 2) return;
        int k = target.GetInstanceID();
        bool isBoss = fromBoss;      // 보스 본체만 (같은 "boss" 태그인 등불 · 묘비는 일반 숫자)
        // 치명타 · 빈틈은 다른 숫자와 합치지 않고 따로 크게
        float window = isBoss ? HealthBarFeel.WindowBoss : HealthBarFeel.WindowEnemy;
        if (!isCrit && !isWeak && byTarget.TryGetValue(k, out DamagePopup p) && p != null && !p.crit && !p.weak && !p.hurt && Time.time - p.lastAdd < window && Time.time - p.firstAdd < HealthBarFeel.WindowMax)
        {
            p.Add(damage, tier);
            return;
        }
        // 빈틈 (치명타 아님): 같은 대상의 열린 빈틈 숫자에 합침 · 「완벽한 빈틈!」이 끼면 머리 글자만 올림
        if (!isCrit && isWeak && byWeak.TryGetValue(k, out p) && p != null && p.live && p.weak && !p.crit && Time.time - p.lastAdd < window && Time.time - p.firstAdd < HealthBarFeel.WindowMax)
        {
            if (weakPerf) p.weakPerfect = true;
            p.Add(damage, tier);
            return;
        }
        if (active >= EnemyCap || !EnsureFont()) return;

        float top = body != null ? body.bounds.max.y : target.position.y + 1f;
        Vector3 at = new Vector3(target.position.x + Random.Range(-0.3f, 0.3f), top + 0.35f, 0f);
        if (isBoss)
        {
            // 몸 위쪽 안에서 (머리 위 멀리 · 화면 밖으로 나가지 않게)
            float y = body != null ? Mathf.Lerp(body.bounds.center.y, body.bounds.max.y, 0.55f) : target.position.y + 0.8f;
            float halfW = body != null ? body.bounds.extents.x * 0.5f : 0.6f;
            at = new Vector3(target.position.x + Random.Range(-halfW, halfW), y, 0f);
            Camera cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                Vector3 c = cam.transform.position;
                float hh = cam.orthographicSize, hw = hh * cam.aspect;
                at.x = Mathf.Clamp(at.x, c.x - hw + 1.2f, c.x + hw - 1.2f);
                at.y = Mathf.Clamp(at.y, c.y - hh + 0.8f, c.y + hh - BossRise - 1f);
            }
        }

        // 다 떠오른 숫자를 버리지 않고 다시 씀 (맞을 때마다 글자 오브젝트를 새로 만들고 지우지 않게)
        if (isCrit)
        {
            at += new Vector3(Random.Range(-0.25f, 0.25f), 0.3f, 0f);
            Fx.Spawn("fx_sparkle", at, 1.4f, new Color(1f, 0.85f, 0.3f), 24f);
        }
        p = Take(k, target, at, isBoss ? 45 : 40, isBoss && bossMaterial != null ? bossMaterial : material);
        p.crit = isCrit;
        p.boss = isBoss;
        p.weak = isWeak;
        p.weakPerfect = weakPerf;
        if (!isCrit && !isWeak) byTarget[k] = p;
        else if (!isCrit) byWeak[k] = p;
        p.Add(damage, tier);
    }

    // 플레이어가 받은 피해: 짧은 사이 피해를 한 숫자로 합침 (WindowPlayer) · 빨강 · "-N" · 등급별 크기
    public static void ShowHurt(Transform player, float taken, HealthBarFeel.Tier tier)
    {
        if (player == null || taken <= 0f || GameSettings.DamageNumbers == 2) return;
        int k = player.GetInstanceID();
        if (byTarget.TryGetValue(k, out DamagePopup p) && p != null && p.hurt && Time.time - p.lastAdd < HealthBarFeel.WindowPlayer && Time.time - p.firstAdd < HealthBarFeel.WindowMax)
        {
            p.Add(taken, tier);
            return;
        }
        if (active >= MaxActive || !EnsureFont()) return;
        Vector3 at = new Vector3(player.position.x, player.position.y + HurtHeight, 0f);
        p = Take(k, player, at, HurtOrder, material);
        p.hurt = true;
        byTarget[k] = p;
        p.Add(taken, tier);
    }

    // 다 떠오른 숫자를 꺼내 처음 상태로 (풀이 비면 새로 만듦)
    static DamagePopup Take(int k, Transform target, Vector3 at, int order, Material mat)
    {
        DamagePopup p = null;
        while (pool.Count > 0 && p == null) p = pool.Pop();
        if (p == null)
        {
            GameObject go = new GameObject("DamagePopup");
            p = go.AddComponent<DamagePopup>();
            p.text = go.AddComponent<TextMeshPro>();
            p.text.font = font;
            if (material != null) p.text.fontSharedMaterial = material;
            p.text.alignment = TextAlignmentOptions.Center;
            p.text.enableWordWrapping = false;
            p.text.rectTransform.sizeDelta = new Vector2(6f, 2f);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sortingLayerName = "Effect";
            mr.sortingOrder = 40;
        }
        p.GetComponent<MeshRenderer>().sortingOrder = order;
        p.text.fontSharedMaterial = mat;
        p.transform.position = at;
        p.transform.localScale = Vector3.one;
        p.text.fontSize = GameSettings.DamageNumbers == 1 ? 15f : 11f;
        p.key = k;
        p.start = at;
        p.follow = target;
        p.followOffset = target != null ? at - target.position : Vector3.zero;
        p.amount = 0f;
        p.t = 0f;
        p.size = 1f;
        p.tierMax = 0;
        p.crit = false;
        p.boss = false;
        p.hurt = false;
        p.weak = p.weakPerfect = false;
        p.live = true;
        p.gameObject.SetActive(true);
        active++;
        return p;
    }

    // 다 쓴 숫자 (다시 쓸 때까지 꺼 둠)
    static readonly Stack<DamagePopup> pool = new Stack<DamagePopup>();
    bool live;

    void Release()
    {
        if (!live) return;
        live = false;
        active--;
        follow = null;
        if (byTarget.TryGetValue(key, out DamagePopup p) && p == this) byTarget.Remove(key);
        if (byWeak.TryGetValue(key, out p) && p == this) byWeak.Remove(key);
        gameObject.SetActive(false);
        pool.Push(this);
    }

    static bool EnsureFont()
    {
        if (font != null) return true;
        font = UIKit.Font;
        if (font == null)
        {
            TMP_Text any = FindFirstObjectByType<TMP_Text>();
            if (any != null) font = any.font;
        }
        if (font == null) font = TMP_Settings.defaultFontAsset;
        if (font == null) return false;
        // 어느 바닥에서도 읽히게 검은 테두리 (모든 숫자가 한 재질을 같이 씀)
        material = new Material(font.material);
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.3f);
        material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.08f, 0.05f, 0.08f, 1f));
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
        // 보스 숫자: 테두리를 더 두껍게 (보스 몸 위에서도 또렷이)
        bossMaterial = new Material(material);
        bossMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.42f);
        bossMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.25f, 0.02f, 0.02f, 1f));
        bossMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.25f);
        return true;
    }

    void Add(float damage, HealthBarFeel.Tier tier)
    {
        bool first = amount <= 0f;
        SetMeshAlpha(1f);      // 사라지던 숫자에 합치거나 다시 쓸 때 정점 알파를 되돌림
        amount += damage;
        if ((int)tier > tierMax) tierMax = (int)tier;
        lastAdd = Time.time;
        if (first) firstAdd = lastAdd;
        t = 0f;
        popT = 0f;
        if (first) { riseT = 0f; popAmp = crit ? 0.3f : 0.35f; }       // 1.1~: 치명타 처음 튐 1.8배 → 1.3배
        else popAmp = 0.08f + 0.06f * tierMax;                          // 큰 누적일수록 합칠 때 더 튐 (Deadly 0.26)
        float scale = HealthBarFeel.PopupScale[tierMax];
        // 플레이어가 받은 피해: 빨강 (치명 등급은 더 진하게) · 등급별 크기
        if (hurt)
        {
            text.text = "-" + Mathf.CeilToInt(amount);
            text.color = tierMax >= (int)HealthBarFeel.Tier.Deadly ? new Color(1f, 0.15f, 0.15f) : new Color(1f, 0.3f, 0.28f);
            size = HurtSize * scale;
            return;
        }
        text.text = (weak ? WeakHead(weakPerfect) : "") + (amount < 10f ? amount.ToString("0.#") : Mathf.RoundToInt(amount).ToString()) + (crit ? "!" : "");
        // 큰 피해일수록 크고 붉은 금색
        PlayerController pl = Hostile.Player;
        float baseDmg = pl != null ? Mathf.Max(0.1f, pl.damage) : 1f;
        float k = Mathf.Clamp01((amount / baseDmg - 1f) / 4f);
        // 1.0.4~: 가장 큰 쪽을 주황(1, 0.55, 0.2) → 금빛 노랑(1, 0.78, 0.3)으로 — 받은 피해(빨강)와 헷갈리지 않게
        text.color = Color.Lerp(new Color(1f, 1f, 0.95f), new Color(1f, 0.78f, 0.3f), k);
        size = 1f + 0.5f * k;
        // 치명타: 금색으로 훨씬 크게
        if (crit)
        {
            text.color = new Color(1f, 0.82f, 0.15f);
            size = 1.6f + 0.3f * k;     // 1.2.2~ 1.9~2.3 → 1.6~1.9
        }
        // 보스: 늘 주황 금색으로 크게 (치명타는 더 크게)
        if (boss)
        {
            if (!crit) text.color = Color.Lerp(new Color(1f, 0.8f, 0.3f), new Color(1f, 0.6f, 0.15f), k);     // 1.0.4~: 끝 색 (1, 0.4, 0.15) → (1, 0.6, 0.15), 붉은빛을 덜어 받은 피해와 구분
            // 1.1~: 치명타 1.9~2.3 × 1.8 = 최대 4.1배라 보스 패턴 시작점 · 적 탄을 덮었음 → 보스 치명타는 1.3배까지 · 최대 2.8
            size = crit ? Mathf.Min(size * 1.3f * scale, 2.3f) : size * BossSize * scale;
        }
        else size *= scale;      // 누적 등급별 크기 (1 / 1.2 / 1.55 / 2)
        // 치명 등급 누적 (치명타 아님): 더 진한 금빛
        if (!crit && tierMax >= (int)HealthBarFeel.Tier.Deadly) text.color = new Color(1f, 0.7f, 0.2f);
        // 빈틈: 청록 흰색 · 1.15배 (치명타와 겹치면 금빛 그대로)
        if (weak)
        {
            if (!crit) text.color = new Color(0.62f, 1f, 0.95f);
            size *= 1.15f;
        }
    }

    // 「빈틈!」 머리 글자: 언어가 바뀔 때만 다시 만듦
    static string weakHead, weakHeadPerfect;
    static int weakHeadLang = -1;
    static string WeakHead(bool perfect)
    {
        if (weakHead == null || weakHeadLang != (int)Loc.Current)
        {
            weakHeadLang = (int)Loc.Current;
            weakHead = "<size=55%>" + Loc.T("weak.popup") + "</size>\n";
            weakHeadPerfect = "<size=55%>" + Loc.T("weak.popup.perfect") + "</size>\n";
        }
        return perfect ? weakHeadPerfect : weakHead;
    }

    void Update()
    {
        t += Time.deltaTime;
        riseT += Time.deltaTime;
        popT += Time.deltaTime;
        float life = hurt ? HurtLife : boss ? BossLife : Life;
        float k = t / life;
        if (k >= 1f) { Release(); return; }
        float kr = Mathf.Clamp01(riseT / life);
        float up = 1f - (1f - kr) * (1f - kr);
        float pop = popT < 0.08f ? 1f + popAmp * (1f - popT / 0.08f) : 1f;
        if (follow != null)
        {
            // 꺼진 대상(죽어 풀로 돌아감)은 놓아줌 — 다시 쓰인 적을 따라가지 않게
            if (follow.gameObject.activeInHierarchy) start = follow.position + followOffset;
            else follow = null;
        }
        transform.position = start + new Vector3(0f, (boss ? BossRise : Rise) * up, 0f);
        transform.localScale = Vector3.one * (size * pop);
        // 1.1.9: 사라지는 알파를 text.color 로 바꾸면 TMP 가 매 프레임 글자 메시를 통째로 다시 만들었음 (최대 80개)
        //   → 이미 만든 메시의 정점 알파만 바꿈 (같은 모습 · 메시 재생성 없음)
        SetMeshAlpha(k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
    }

    byte meshAlpha = 255;
    void SetMeshAlpha(float a)
    {
        byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f);
        if (b == meshAlpha) return;
        meshAlpha = b;
        TMP_TextInfo info = text.textInfo;
        if (info == null || info.meshInfo == null) return;
        for (int i = 0; i < info.meshInfo.Length; i++)
        {
            Color32[] cols = info.meshInfo[i].colors32;
            if (cols == null) continue;
            int n = Mathf.Min(info.meshInfo[i].vertexCount, cols.Length);
            for (int j = 0; j < n; j++) cols[j].a = b;
        }
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    // 장면이 바뀌어 지워질 때 (살아 있던 것만 셈에서 뺌)
    void OnDestroy()
    {
        if (!live) return;
        live = false;
        active--;
        if (byTarget.TryGetValue(key, out DamagePopup p) && p == this) byTarget.Remove(key);
        if (byWeak.TryGetValue(key, out p) && p == this) byWeak.Remove(key);
    }
}
