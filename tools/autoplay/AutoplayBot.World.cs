using System.Collections.Generic;
using UnityEngine;

// 자동 플레이 봇 — 줍기 · 지형 위험 · 튕기는 구 · 고요의 눈 (복사본 전용)
public partial class AutoplayBot
{
    float pickWeight;
    TerrainZone[] terrain = new TerrainZone[0];
    HPpack[] hpPacks = new HPpack[0];

    void ScanTerrain(Vector3 me)
    {
        terrain = EndlessTerrain.Instance != null ? FindObjectsByType<TerrainZone>(FindObjectsSortMode.None) : new TerrainZone[0];
        hpPacks = FindObjectsByType<HPpack>(FindObjectsSortMode.None);
        if (stall == null || !stall) stall = null;
    }

    void AddTerrainZones()
    {
        foreach (TerrainZone z in terrain)
        {
            if (z == null || !z.isActiveAndEnabled) continue;
            zones.Add(new Zone { c = z.transform.position, r = z.lava ? EndlessTerrain.LavaRadius : EndlessTerrain.BogRadius, w = z.lava ? 9f : 2.5f });
        }
        // 결계 · 침식의 튕기는 덩어리는 움직이는 탄처럼
        foreach (DomainBall b in DomainBall.Live)
        {
            if (b == null || !b.isActiveAndEnabled) continue;
            if (!Noticed(((long)b.GetInstanceID() << 20) | 7)) continue;
            projs.Add(new Proj { p = b.transform.position, v = b.velocity, r = b.hitRadius });
        }
    }

    // 가고 싶은 쪽 (단위 벡터 · 없으면 0): 상점 제단 → 체력이 낮으면 회복 → 공간 침식의 고요의 눈 → 가까운 코인 무리
    Vector2 PickupGoal(Vector2 me, PlayerController p)
    {
        pickWeight = 0f;
        float hpFrac = p.PlayerHealth / Mathf.Max(1f, p.PlayerMaxHealth);
        Vector2 goal = Vector2.zero;
        // 공간 침식 중 고요의 눈 (땅 타격을 막아 줌)
        if (BossUltimate.Active && BossUltimate.ErosionActive && DomainStrikes.EyeOn)
        {
            Vector2 d = (Vector2)DomainStrikes.EyePos - me;
            if (d.magnitude > DomainStrikes.EyeRadius * 0.6f) { pickWeight = 1.2f; return d.normalized; }
        }
        Vector2 st = StallGoal(me);
        if (st != Vector2.zero && lastDanger < 8f)
        {
            TryOpenStall(me);
            Vector2 d = st - me;
            if (d.magnitude > 1f) { pickWeight = 0.9f; return d.normalized; }
        }
        if (hpFrac < 0.75f)
        {
            float bd = hpFrac < 0.4f ? 25f : 12f;
            foreach (HPpack h in hpPacks)
            {
                if (h == null || !h.isActiveAndEnabled) continue;
                float d = ((Vector2)h.transform.position - me).magnitude;
                if (d < bd) { bd = d; goal = ((Vector2)h.transform.position - me).normalized; pickWeight = hpFrac < 0.4f ? 1.4f : 0.7f; }
            }
            if (goal != Vector2.zero) return goal;
        }
        // 코인 (상점 재화): 가까운 것 몇 개 쪽으로 가볍게
        if (CoinTag.All.Count > 0 && lastDanger < 3f)
        {
            Vector2 sum = Vector2.zero; int n = 0;
            foreach (Transform c in CoinTag.All)
            {
                if (c == null) continue;
                Vector2 d = (Vector2)c.position - me;
                float m = d.magnitude;
                if (m < 10f && m > 0.5f) { sum += d / m * (1f - m / 10f); n++; }
            }
            if (n > 0) { pickWeight = 0.35f; return sum.normalized; }
        }
        return Vector2.zero;
    }
}

// 숙련자 계정 (복사본 저장 파일 · 무한 모드 화력에 실제로 걸리는 영구 강화는 없음 — 영혼 트리는 판 안 성장이라 봇이 직접 삼)
public static class AutoplayMeta
{
    public static void SetupVeteranAccount()
    {
        for (int i = 0; i <= 8; i++) { Prefs.SetInt("char.unlocked." + i, 1); Prefs.SetInt("mastery." + i, 3); Prefs.SetInt("hero.guide." + i, 1); }
        for (int i = 1; i <= 4; i++) Prefs.SetInt("char.cond." + i, 1);
        Prefs.SetInt("mode.unlocked", 2);
        for (int i = 0; i <= 2; i++) Prefs.SetInt("mode.cleared." + i, 1);
        Prefs.SetInt("endless.cleared", 1);
        Prefs.SetInt("intro.lvopens", 3);
        Prefs.SetInt("juice.runs", 10);
        Prefs.Save();
    }

    // 판마다 같은 조건: 지난 판 흔적(최근 고른 갈래 가중치 등)을 지움
    public static void BeforeRun(CharacterId id)
    {
        for (int i = 0; i <= 8; i++) Prefs.DeleteKey("path.recent." + i);
        Prefs.Save();
    }
}

// 게임 코드 복사본에 끼운 갈고리 (run.ps1 이 고친 줄에서 부름)
public static class AutoplayHooks
{
    static AutoplayRunner.RunRecord rec;
    public static void Begin(AutoplayRunner.RunRecord r) => rec = r;
    public static void End() => rec = null;

    // PlayerController.TryHit: 체력이 0 이 됐을 때 — 불사 측정이면 체력을 채우고 사망 횟수만 셈 (true = 쓰러지지 않음)
    public static bool KeepAlive(PlayerController p)
    {
        if (rec == null) return false;
        rec.deaths++;
        AutoplayRunner.Log("쓰러짐 " + rec.deaths + "번째 · " + EndlessMode.Clock(GameMode.EndlessSeconds) + " · 마지막 피해 " + RunStats.LastHurtBy + (AutoplayRunner.NoDeath ? " → 불사 측정: 체력 회복" : ""));
        if (!AutoplayRunner.NoDeath) return false;
        p.PlayerHealth = p.PlayerMaxHealth;
        p.GrantInvincibility(2f);
        return true;
    }
}

// 복사본에서 Auto 로 꺼지던 실제 플레이 규칙(초월 카드 · 궁수 숨 고르기)을 되살리는 스위치
public static class AutoplayFlags
{
    public static bool Real => GameInput.Auto && AutoplayRunner.Out != null;
}
