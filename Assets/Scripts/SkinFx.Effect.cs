using System.Collections.Generic;
using UnityEngine;

// 이펙트 스킨 칸 (3-3절): 명중 · 처치 · 처치음 · 코인 · 연쇄 · 상자/이정표 · 큰 적 · 가장자리 연쇄 · 출처별 처치
//   처치 연출은 적 Die 안에서 불려 그 순간의 피해 출처를 이어받던 것(D6) → 출처를 읽어 두기만 하고, 그림은 Fx 가 아니라 SkinMote(출처와 무관)로
//   처치 자리는 이펙트 스킨 몫, 전설 캐릭터는 몸으로 흡수 (D8 · 6-1절)
public partial class SkinFx
{
    readonly Queue<float> recentKills = new Queue<float>();
    float comboAt, killStreakAt;
    int killTurn, killStreak, comboRun;

    // 처치: 상점 미리보기(SkinPreview)도 부름
    public void OnKill(Vector3 pos)
    {
        try { OnKillCore(pos); }
        catch (System.Exception e) { GuardFail(e); }
    }

    void OnKillCore(Vector3 pos)
    {
        if (Off) { LegacyKill(pos); return; }
        string src = DamageSource.Current;
        // 연쇄: 0.6초 안 5처치 (쉬는 1.5초)
        float now = Time.time;
        recentKills.Enqueue(now);
        while (recentKills.Count > 0 && now - recentKills.Peek() > 0.6f) recentKills.Dequeue();
        bool combo = recentKills.Count >= 5;
        comboRun = combo ? comboRun + 1 : 0;
        if (combo && now >= comboAt)
        {
            comboAt = now + 1.5f;
            if (EffectLook != null)
            {
                string fx = EffectLook.Fx("combo");
                if (fx != null) Emit(fx, EffectLook, pos, Vector2.up, false, 28, Bud.Big);
                if (!RunSave.Replaying) SkinAudio.Play(EffectSkin, "combo", 0.55f, 0f, SkinAudio.Prio.Combo);
            }
            CharAbsorb(pos, true);
        }
        else CharAbsorb(pos, false);
        // 가장자리 연쇄 (연쇄 10 이상 이어지는 동안)
        if (recentKills.Count >= 10 && EffectLook != null && EffectLook.Fx("edge") != null) SkinEdge.Show(EffectLook.Fx("edge"), EffectLook.main, false);

        if (EffectLook == null || now < killFxAt) return;
        killFxAt = now + 0.06f;
        // 출처별 처치 (전설): 평타 · 스킬 · 필살기
        string slot = "kill";
        string by = src == DamageSource.Basic ? "kill.basic" : src == DamageSource.Ult ? "kill.ult" : src != null && src != DamageSource.Other ? "kill.skill" : null;
        if (by != null && EffectLook.Fx(by) != null) slot = by;
        Emit(EffectLook.Fx(slot), EffectLook, pos, Vector2.up, false, 26, Bud.Kill);
        // 뇌전: 하늘에서 내리꽂는 번개 (풀 입자 — D7)
        if (EffectLook.opt.Contains("skybolt")) Bolt(pos + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(5f, 6.5f), 0f), pos, 1.7f, EffectLook.main, 0.2f);
        if (now < killSoundAt || RunSave.Replaying) return;
        killSoundAt = now + 0.12f;
        // 연속 처치가 음계 (별의 축복): 1초 안에 이어진 처치마다 한 칸 위 · 1초 쉬면 처음부터
        if (EffectLook.opt.Contains("scale"))
        {
            killStreak = now - killStreakAt < 1f ? Mathf.Min(killStreak + 1, 5) : 0;
            killStreakAt = now;
            int[] semis = { 0, 2, 4, 7, 9, 12 };
            AudioClip c = SkinAudio.Clip(EffectSkin, "kill");
            if (c != null) { SkinAudio.PlayClip(c, 0.5f, 0f, SkinAudio.Prio.Kill, null, Mathf.Pow(2f, semis[killStreak] / 12f)); return; }
        }
        // 예전 처치음이 있으면 새 변주와 번갈아 (구매자가 익숙한 소리를 잃지 않게)
        if (EffectSkin.killSound != null && (killTurn++ % 3 == 0 || !SkinAudio.Has(EffectSkin, "kill")))
            SpecialAbilities.SharedFx?.Play(EffectSkin.killSound, 0.55f, Random.Range(0.95f, 1.08f));
        else SkinAudio.Play(EffectSkin, "kill", 0.5f, 6f, SkinAudio.Prio.Kill);
    }

    // 엘리트 · 중간 보스 · 보스 처치 (EnermyController · boss)
    public static void OnBigKill(Vector3 pos)
    {
        try { OnBigKillCore(pos); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnBigKillCore(Vector3 pos)
    {
        SkinFx f = Instance;
        if (f == null || Off || EffectLook == null || !f.SlotReady("big", 0.5f)) return;
        f.Slot(EffectSkin, EffectLook, "big", pos, Vector2.up, false, 28, Bud.Big, 0.6f, 0f, SkinAudio.Prio.Big);
    }

    // 보물 상자 열기 (TreasureChest) · 처치 수 이정표 (Juice)
    public static void OnChest(Vector3 pos)
    {
        try { OnChestCore(pos); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnChestCore(Vector3 pos)
    {
        SkinFx f = Instance;
        if (f == null || Off || EffectLook == null || !f.SlotReady("chest", 1f)) return;
        f.Slot(EffectSkin, EffectLook, "chest", pos, Vector2.up, false, 28, Bud.Rare, 0.55f, 0f, SkinAudio.Prio.Combo);
    }

    public static void OnMilestone()
    {
        try { OnMilestoneCore(); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnMilestoneCore()
    {
        SkinFx f = Instance;
        if (f == null || Off || EffectLook == null || !f.SlotReady("mile", 1f)) return;
        string fx = EffectLook.Fx("mile") ?? EffectLook.Fx("chest");
        if (fx != null) f.Emit(fx, EffectLook, f.transform.position + Vector3.up * 2f, Vector2.up, false, 28, Bud.Rare);
        if (!SkinAudio.Play(EffectSkin, "mile", 0.55f, 0f, SkinAudio.Prio.Combo)) SkinAudio.Play(EffectSkin, "chest", 0.55f, 0f, SkinAudio.Prio.Combo);
    }

    // 코인 반짝 (Juice.CoinPicked) — 소리는 PlayerController 의 반음 사다리 (SkinAudio.CoinClip)
    public static void OnCoin(Vector3 pos)
    {
        try { OnCoinCore(pos); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnCoinCore(Vector3 pos)
    {
        SkinFx f = Instance;
        if (f == null || Off || EffectLook == null) return;
        string fx = EffectLook.Fx("coin");
        if (fx != null) f.Emit(fx, EffectLook, pos, Vector2.up, false, 20, Bud.Rep);
    }

    // 적이 맞았을 때 (모든 캐릭터 · 모든 공격): 이펙트 스킨 명중 그림 (초당 최대 20번) · 무기 스킨 근접 명중음
    static float hitFxAt, whitAt;
    static int hitCount;

    public static void OnEnemyHit(Vector3 pos)
    {
        try { OnEnemyHitCore(pos); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnEnemyHitCore(Vector3 pos)
    {
        if (Off) { LegacyEnemyHit(pos); return; }
        SkinFx f = Instance;
        if (f == null) return;
        // 무기 명중음: 평타 출처일 때만 (출혈 · 독 틱 · 스킬엔 안 남)
        if (WeaponSkin != null && DamageSource.Current == DamageSource.Basic && Time.unscaledTime >= whitAt && !RunSave.Replaying)
        {
            whitAt = Time.unscaledTime + 0.03f;
            SkinAudio.Play(WeaponSkin, "whit", 0.3f, 6f, SkinAudio.Prio.Hit);
        }
        if (EffectLook == null || Time.time < hitFxAt) return;
        hitFxAt = Time.time + 0.05f;
        Vector3 at = pos + (Vector3)(Random.insideUnitCircle * 0.3f);
        string fx = EffectLook.Fx("hit");
        if (fx != null) f.Emit(fx, EffectLook, at, Vector2.up, false, 24, Bud.Rep);
        // 뇌전: 세 번에 한 번 짧은 번개 (풀 입자 — D7: 예전엔 매번 새 오브젝트)
        if (EffectLook.opt.Contains("bolt3") && ++hitCount % 3 == 0)
            Bolt(at + (Vector3)(Random.insideUnitCircle.normalized * 1.8f), at, 0.8f, EffectLook.main, 0.14f);
    }

    // 풀 판 번개 (D7): fx_bolt 프레임을 세로로 늘려 두 점 사이에 — 번개는 범위 표에 없는 장식이 아니라 그대로 크기
    public static void Bolt(Vector3 from, Vector3 to, float width, Color color, float life, bool unscaled = false)
    {
        Sprite[] f = Fx.Frames("fx_bolt");
        if (f == null || f.Length == 0) return;
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 0.05f) return;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
        color.a *= GameSettings.MyFxAlpha;
        SkinMote m = SkinMote.Make(f[0], (from + to) * 0.5f, len, color, "Effect", 25, life, unscaled);
        if (m == null) return;
        float w = f[0].bounds.size.x, h = f[0].bounds.size.y;
        float baseScale = len / Mathf.Max(0.001f, h);
        m.Spin(0f, angle).Width(width / Mathf.Max(0.001f, w) / baseScale).Hold(0.6f).Anim(f, f.Length / Mathf.Max(0.05f, life));
    }
}
