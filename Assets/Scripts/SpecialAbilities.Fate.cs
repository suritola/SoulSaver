using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 영혼 트리 '운명' 가지 (1.8.2~, 예전 액티브 스킬 자리)
// 다른 가지 · 레벨업 카드 · 거너 카드에 없던 새 효과만:
//   운명의 실(레벨업 카드 다시 뽑기) · 시간의 틈(위기에 세상이 느려짐) · 각성의 파동(레벨업 충격파)
//   연쇄 광풍(몰아서 잡으면 폭풍) · 중력 우물(적이 몰린 곳을 빨아들임) · 저주 전이(상태 이상이 옮겨 감) · 영혼 메아리(처치 자리의 유령)
public partial class SpecialAbilities
{
    // 도감 · 트리가 같이 쓰는 목록 (키, 부모, 이름, 설명, 가격, 아이콘 번호)
    public static readonly (string key, string parent, string name, string desc, int cost, int icon)[] FateInfo =
    {
        ("f.thread", null, "운명의 실", "레벨업 카드를 한 번 다시 뽑을 수 있습니다 ([R]). 레벨이 오를 때마다 1회 채워짐, 최대 1회 보관", 8, 105),
        ("f.thread2", "f.thread", "운명의 실 II", "다시 뽑기를 최대 2회까지 보관합니다", 24, 105),
        ("f.thread3", "f.thread2", "운명의 실 III", "다시 뽑기를 최대 3회까지 보관합니다", 44, 105),
        ("f.rift1", "f.thread", "시간의 틈", "체력이 30% 아래로 떨어지면 3초 동안 적이 65% 느려집니다 (60초마다)", 22, 106),
        ("f.rift2", "f.rift1", "시간의 틈 II", "4초 동안 · 40초마다", 44, 106),
        ("f.nova1", "f.thread", "각성의 파동", "레벨이 오를 때 몸 주위에 충격파가 터집니다 (반경 6칸, 공격력 300%)", 16, 107),
        ("f.nova2", "f.nova1", "각성의 파동 II", "반경 9칸, 공격력 500%", 36, 107),
        ("f.storm1", "f.nova1", "연쇄 광풍", "4초 안에 적 12마리를 쓰러뜨릴 때마다 몸 주위에 폭풍이 몰아칩니다 (공격력 250%)", 26, 108),
        ("f.storm2", "f.storm1", "연쇄 광풍 II", "8마리마다, 공격력 350%", 48, 108),
        ("f.well1", "f.rift1", "중력 우물", "25초마다 적이 가장 많이 몰린 곳에 3초 동안 적을 빨아들이는 우물이 열립니다", 34, 109),
        ("f.well2", "f.well1", "중력 우물 II", "15초마다, 더 넓게 빨아들이고 마지막에 터집니다", 60, 109),
        ("f.curse1", "f.thread", "저주 전이", "적이 쓰러질 때 걸려 있던 화상 · 독 · 출혈이 가까운 적 둘에게 옮겨 갑니다. 상태 이상을 걸지 못하는 캐릭터는 공격이 25% 확률로 3초 출혈을 입힙니다", 20, 110),
        ("f.curse2", "f.curse1", "저주 전이 II", "가까운 적 넷에게 옮겨 갑니다", 42, 110),
        ("f.echo1", "f.curse1", "영혼 메아리", "적을 처치하면 6% 확률로 그 자리에 3초 동안 주변을 치는 유령이 남습니다", 28, 111),
        ("f.echo2", "f.echo1", "영혼 메아리 II", "확률 12%, 유령이 더 세게 칩니다", 52, 111),
    };

    // 다시 뽑기는 트리 없이도 한 판에 1회는 갖고 시작 (운명의 실을 배우면 레벨업마다 채워짐)
    int rerollMax, rerolls = 1;
    // 행운 (1.1.0): 높을수록 지금 키우는 레벨업 카드(이미 가진 카드 · 진화 재료)가 더 자주 나옴 (LevelShop.DrawWeight) · 영혼 트리 재물 가지
    public int Luck { get; private set; }
    public int Rerolls => rerolls;
    public void SetRerolls(int n) => rerolls = Mathf.Max(0, n);     // 판 중간 저장 (RunSave)
    float riftSeconds, riftEvery, riftReadyAt;
    float novaRadius, novaMul;
    int stormNeed; float stormMul;
    readonly Queue<float> stormKills = new Queue<float>();
    float wellEvery, wellRadius, wellNextAt;
    bool wellBurst;
    int curseTargets;
    float echoChance, echoMul;
    readonly List<Collider2D> fateHits = new List<Collider2D>();

    void FateNodes(List<SoulNode> t)
    {
        foreach (var f in FateInfo)
            t.Add(new SoulNode
            {
                key = f.key, parent = f.parent, branch = 2, name = Loc.T(f.name), desc = Loc.T(f.desc), cost = f.cost,
                icon = Resources.Load<Sprite>("Icons/ability_" + f.icon), apply = () => FateApply(f.key),
            });
    }

    void FateApply(string key)
    {
        switch (key)
        {
            case "f.thread": rerollMax = 1; rerolls = Mathf.Max(rerolls, 1); break;
            case "f.thread2": rerollMax = 2; break;
            case "f.thread3": rerollMax = 3; break;
            case "f.rift1": riftSeconds = 3f; riftEvery = 60f; break;
            case "f.rift2": riftSeconds = 4f; riftEvery = 40f; break;
            case "f.nova1": novaRadius = 6f; novaMul = 3f; break;
            case "f.nova2": novaRadius = 9f; novaMul = 5f; break;
            case "f.storm1": stormNeed = 12; stormMul = 2.5f; break;
            case "f.storm2": stormNeed = 8; stormMul = 3.5f; break;
            case "f.well1": wellEvery = 25f; wellRadius = 5f; wellNextAt = Time.time + 5f; break;
            case "f.well2": wellEvery = 15f; wellRadius = 7f; wellBurst = true; break;
            case "f.curse1": curseTargets = 2; break;
            case "f.curse2": curseTargets = 4; break;
            case "f.echo1": echoChance = 0.06f; echoMul = 0.5f; break;
            case "f.echo2": echoChance = 0.12f; echoMul = 0.8f; break;
        }
    }

    // ---------------- 운명의 실: 레벨업 창에서 다시 뽑기 (LevelShop)
    public static event System.Action Rerolled;     // 업적

    public bool TryReroll()
    {
        if (rerolls <= 0) return false;
        rerolls--;
        Rerolled?.Invoke();
        if (fx != null) fx.Play("shimmer", 0.6f, 1.4f);
        return true;
    }

    // 레벨이 오를 때: 다시 뽑기 채우기 · 각성의 파동
    void FateOnLevelUp(int levels)
    {
        using var source = DamageSource.As(DamageSource.Tree);
        if (rerollMax > 0) rerolls = Mathf.Min(rerollMax, rerolls + levels);
        if (novaRadius > 0f && player != null)
        {
            Explode(player.transform.position, novaRadius, Damage * novaMul, 3f, new Color(0.75f, 0.9f, 1f, 0.85f));
            fx?.FloatText(player.transform.position, Loc.T("각성의 파동!"), new Color(0.75f, 0.9f, 1f), 5f, 0.2f);
        }
    }

    // 처치할 때: 연쇄 광풍 · 영혼 메아리
    void FateOnKill(Vector3 pos)
    {
        using var source = DamageSource.As(DamageSource.Tree);
        if (stormNeed > 0 && player != null)
        {
            stormKills.Enqueue(Time.time);
            while (stormKills.Count > 0 && Time.time - stormKills.Peek() > 4f) stormKills.Dequeue();
            if (stormKills.Count >= stormNeed)
            {
                stormKills.Clear();
                Explode(player.transform.position, 5f, Damage * stormMul, 2.5f, new Color(0.6f, 1f, 0.85f, 0.85f));
                fx?.FloatText(player.transform.position, Loc.T("연쇄 광풍!"), new Color(0.6f, 1f, 0.85f), 5f, 0.2f);
            }
        }
        if (echoChance > 0f && Random.value < echoChance) StartCoroutine(SoulEcho(pos));
    }

    IEnumerator SoulEcho(Vector3 pos)
    {
        GameObject ghost = MakeSprite("SoulEcho", glowSprite, pos, 0.3f, new Color(0.7f, 0.85f, 1f, 0.7f), "Effect", 4);
        for (float t = 0f, tick = 0f; t < 3f && ghost != null; t += Time.deltaTime)
        {
            ghost.transform.localScale = Vector3.one * (0.3f + 0.05f * Mathf.Sin(t * 12f));
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = 0.5f;
                // 피해로 적이 죽으면 다른 운명 효과가 같은 목록을 쓰므로, 복사본을 돌며 침
                foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(pos, 2.5f, fateHits)))
                {
                    if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                    Specials.Damage(c.gameObject, Damage * echoMul, (c.transform.position - pos).normalized, 0.5f);
                }
                Fx.Spawn("fx_shock", pos, 5f, new Color(0.7f, 0.85f, 1f, 0.5f), 22f);
            }
            yield return null;
        }
        if (ghost != null) Destroy(ghost);
    }

    // 적이 쓰러질 때 (EnermyController.Die): 저주 전이
    public void FateOnEnemyDie(EnermyController e)
    {
        if (curseTargets <= 0 || e == null) return;
        Burn burn = e.GetComponent<Burn>();
        Poison poison = e.GetComponent<Poison>();
        Bleed bleed = e.GetComponent<Bleed>();
        if (burn == null && poison == null && bleed == null) return;
        Vector3 at = e.transform.position;
        List<Transform> near = new List<Transform>();
        foreach (Collider2D c in Specials.Overlap(at, 6f, fateHits))
        {
            if (!c.CompareTag("enermy") || c.transform == e.transform) continue;
            if (c.TryGetComponent(out EnermyController o) && o.IsDead) continue;
            near.Add(c.transform);
        }
        near.Sort((a, b) => (a.position - at).sqrMagnitude.CompareTo((b.position - at).sqrMagnitude));
        for (int i = 0; i < near.Count && i < curseTargets; i++)
        {
            GameObject g = near[i].gameObject;
            if (burn != null) Burn.Apply(g, burn.dps, 2.5f);
            if (poison != null) Poison.Apply(g, poison.perStack, Mathf.Max(1, poison.stacks));
            if (bleed != null) Bleed.Apply(g, bleed.dps, 3f);
            for (int k = 1; k <= 3; k++) Fx.Spawn("fx_spark", Vector3.Lerp(at, g.transform.position, k / 4f), 0.8f, new Color(0.7f, 1f, 0.5f), 26f);
        }
    }

    // 매 프레임: 시간의 틈 · 중력 우물
    void FateTick()
    {
        if (Time.timeScale == 0f || player == null) return;
        if (riftSeconds > 0f && Time.time >= riftReadyAt && player.PlayerHealth > 0f && player.PlayerHealth <= player.PlayerMaxHealth * 0.3f)
        {
            riftReadyAt = Time.time + riftEvery;
            StartCoroutine(TimeRift());
        }
        if (wellEvery > 0f && Time.time >= wellNextAt)
        {
            wellNextAt = Time.time + wellEvery;
            Vector3? at = DensestEnemies();
            if (at.HasValue) using (DamageSource.As(DamageSource.Tree)) StartCoroutine(GravityWell(at.Value));
            else wellNextAt = Time.time + 3f;       // 몰린 곳이 없으면 곧 다시
        }
    }

    Image riftTint;

    IEnumerator TimeRift()
    {
        EnermyController.GlobalSpeedMultiplier = 0.35f;
        fx?.Play("shimmer", 0.8f, 0.6f);
        fx?.FloatText(player.transform.position, Loc.T("시간의 틈!"), new Color(0.6f, 0.8f, 1f), 6f, 0f);
        Fx.Spawn("fx_shock", player.transform.position, 14f, new Color(0.6f, 0.8f, 1f, 0.7f), 16f);
        // 화면 가장자리가 옅은 푸른빛 (HUD 뒤)
        if (riftTint == null)
        {
            Canvas c = UIKit.HudCanvas();
            if (c != null)
            {
                RectTransform r = UIKit.Rect("TimeRiftTint", c.transform, Vector2.zero, Vector2.zero);
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                r.SetSiblingIndex(0);
                riftTint = r.gameObject.AddComponent<Image>();
                riftTint.raycastTarget = false;
            }
        }
        for (float t = 0f; t < riftSeconds; t += Time.deltaTime)
        {
            if (riftTint != null) riftTint.color = new Color(0.4f, 0.6f, 1f, 0.14f * Mathf.Min(1f, Mathf.Min(t, riftSeconds - t) * 4f));
            yield return null;
        }
        if (riftTint != null) riftTint.color = Color.clear;
        if (Mathf.Approximately(EnermyController.GlobalSpeedMultiplier, 0.35f)) EnermyController.GlobalSpeedMultiplier = 1f;
    }

    // 플레이어 주변에서 적이 가장 많이 몰린 자리 (3마리 미만이면 없음)
    Vector3? DensestEnemies()
    {
        List<Collider2D> around = new List<Collider2D>(Specials.Overlap(player.transform.position, 14f, fateHits));
        Vector3 best = Vector3.zero;
        int bestCount = 2;
        int checkedN = 0;
        foreach (Collider2D c in around)
        {
            if (!c.CompareTag("enermy") || ++checkedN > 30) continue;
            int n = 0;
            foreach (Collider2D o in around)
                if (o.CompareTag("enermy") && (o.transform.position - c.transform.position).sqrMagnitude < 12.25f) n++;
            if (n > bestCount) { bestCount = n; best = c.transform.position; }
        }
        return bestCount > 2 ? best : (Vector3?)null;
    }

    IEnumerator GravityWell(Vector3 at)
    {
        GameObject core = MakeSprite("GravityWell", glowSprite, at, 0.6f, new Color(0.35f, 0.2f, 0.55f, 0.85f), "Effect", 2);
        fx?.Play("pulse", 0.7f, 0.6f);
        for (float t = 0f, tick = 0f; t < 3f; t += Time.deltaTime)
        {
            if (core != null) { core.transform.Rotate(0f, 0f, -360f * Time.deltaTime); core.transform.localScale = Vector3.one * (0.6f + 0.1f * Mathf.Sin(t * 10f)); }
            foreach (Collider2D c in Specials.Overlap(at, wellRadius, fateHits))
            {
                if (c == null || !c.CompareTag("enermy")) continue;
                c.transform.position = Vector3.MoveTowards(c.transform.position, at, 5f * Time.deltaTime);
            }
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = 0.5f;
                foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(at, wellRadius, fateHits)))
                    if (c != null && (c.CompareTag("enermy") || c.CompareTag("boss"))) Specials.Damage(c.gameObject, Damage * 0.3f, Vector3.zero, 0f);
                Fx.Spawn("fx_shock", at, wellRadius * 2f, new Color(0.45f, 0.3f, 0.7f, 0.5f), 18f);
            }
            yield return null;
        }
        if (core != null) Destroy(core);
        if (wellBurst) Explode(at, wellRadius * 0.7f, Damage * 2f, 2f, new Color(0.55f, 0.35f, 0.85f, 0.85f));
    }
}
