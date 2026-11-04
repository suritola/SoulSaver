using System.Collections;
using System.Globalization;
using UnityEngine;

// 캐릭터 스킨 칸 (3-1절): 걸음 · 출발/멈춤 · 잔상 점 · 평타 몸짓 · 피격 · 레벨업 · 대기 · 필살기 시전 · 버팀/부활/교대 · 처치 흡수 · 위기 · 오라 · 사망 · 승리
//   걸음 · 대기 · 위기는 게임 시간 기준(일시정지 · 레벨업 창이면 같이 멈춤) · 이야기 연출 · 보스 컷신 · 영화 모드 · 쓰러지는 중엔 끔 (6-3절)
public partial class SkinFx
{
    float stepAcc, stillFor, moveFor, idleAt, trailAt, lastAttackAt = -9f;
    int stepCount, idleTurn, attackFrame = -1;
    bool crisisOn;

    bool Hushed => StoryDirector.Playing || BossUltimate.CutscenePlaying || PlayerController.Cinematic || (player != null && player.IsDying);

    void CharUpdate(Vector3 p, Vector3 delta, bool moving)
    {
        float dt = Time.deltaTime;
        bool dashing = CharacterKit.Instance != null && CharacterKit.Instance.Dashing;
        // 도적 돌진 · 한 프레임 3칸 넘는 이동(자리 바꾸기 · 밀어내기)은 걸음 · 출발에서 뺌 (2-4절)
        bool walk = moving && !dashing && delta.magnitude < 3f && !Hushed;
        if (walk) moveDir = Vector2.Lerp(moveDir, ((Vector2)delta).normalized, 0.5f).normalized;
        if (moveDir.sqrMagnitude < 0.01f) moveDir = Vector2.up;

        SkinLook look = CharLook;
        StringTick();
        if (walk)
        {
            if (stillFor >= 0.35f && look != null && SlotReady("start", 0.6f)) Slot(CharSkin, look, "start", p, -moveDir, true, 30, Bud.Rep, 0.25f, 4f, SkinAudio.Prio.Step);
            moveFor += dt;
            stillFor = 0f;
            stepAcc += dt;
            if (stepAcc >= 0.2f) { stepAcc = Mathf.Min(stepAcc - 0.2f, 0.2f); Step(p); }
            if (look != null && look.Fx("trail") != null && Time.time >= trailAt && !GraphicsQuality.Low)
            {
                trailAt = Time.time + 0.06f;
                Emit(look.Fx("trail"), look, p, -moveDir, true, 29, Bud.Rep);
            }
        }
        else
        {
            if (moveFor >= 0.3f && look != null && !Hushed && SlotReady("stop", 0.6f)) Slot(CharSkin, look, "stop", p, moveDir, true, 30, Bud.Rep, 0.25f, 4f, SkinAudio.Prio.Step);
            moveFor = 0f;
            stillFor += dt;
            stepAcc = 0.15f;        // 다시 걸으면 곧 첫 발자국
        }

        // 대기: 멈춤 3초 + 3초 동안 공격 없음 → 4초마다 (캐릭터 · 무기 번갈아 — 6-1절)
        if (!walk && !Hushed && stillFor >= 3f && Time.time - lastAttackAt >= 3f && Time.time >= idleAt && !GraphicsQuality.Low)
        {
            idleAt = Time.time + 4f;
            idleTurn++;
            bool weaponTurn = WeaponLook != null && WeaponLook.Has("widle") && (CharLook == null || (idleTurn & 1) == 0);
            if (weaponTurn) Slot(WeaponSkin, WeaponLook, "widle", p, Vector2.up, true, 30, Bud.Rare, 0.15f, 3f, SkinAudio.Prio.Idle);
            else if (look != null) Slot(CharSkin, look, "idle", p, Vector2.up, true, 30, Bud.Rare, 0.15f, 3f, SkinAudio.Prio.Idle);
        }

        if (look == null) return;
        // 위기: 체력 30% 아래 동안 몸 안 맥동 (전설 — 회복 신호가 없어 매 프레임 비교)
        string crisis = look.Fx("crisis");
        if (crisis != null && player != null && skinBody != null)
        {
            bool low = player.PlayerHealth > 0f && player.PlayerHealth < player.PlayerMaxHealth * 0.3f && !player.IsDying;
            if (low != crisisOn)
            {
                crisisOn = low;
                ColorUtility.TryParseHtmlString(crisis, out Color cc);
                skinBody.Crisis(low, cc);
            }
        }
        // 오라 덧연출 (전설): 표의 aura 레시피를 auragap 초마다
        string aura = look.Fx("aura");
        if (aura != null && Time.time >= auraFxAt && !GraphicsQuality.Low && !Hushed)
        {
            float gap = 0.6f;
            string g = look.Fx("auragap");
            if (g != null) float.TryParse(g, NumberStyles.Float, CultureInfo.InvariantCulture, out gap);
            auraFxAt = Time.time + Mathf.Max(0.25f, gap);
            Emit(aura, look, p, Vector2.up, true, 3, Bud.Rep);
        }
    }

    void Step(Vector3 p)
    {
        SkinLook look = CharLook;
        if (look == null) return;
        stepSide = !stepSide;
        stepCount++;
        string fx = look.Fx("step");
        if (fx != null) Emit(fx, look, p, moveDir, true, 6, Bud.Rep);
        // N 걸음마다 덧그림 (「stepn: 8」 + 「step2: 그림」 — 4걸음 깃털 · 8걸음 사진 한 장 …)
        string every = look.Fx("stepn");
        int n = 0;
        bool nth = every != null && int.TryParse(every, out n) && n > 0 && stepCount % n == 0;
        string fx2 = nth ? look.Fx("step2") : null;
        if (fx2 != null) Emit(fx2, look, p, moveDir, true, 6, Bud.Rep);
        if (RunSave.Replaying) return;
        // 2026-11-04~ 사용자 「모든 스킨들의 발소리가 시끄럽고 거슬려」: 0.2 / 0.12 → 0.06 / 0.035 · 덧소리 0.18 → 0.06 · 전설 2겹은 없앰
        float vol = Time.time - lastAttackAt < 0.5f ? 0.035f : 0.06f;
        if (look.opt.Contains("quiet")) vol *= 0.8f;
        // N 걸음마다 덧소리 (자전거 벨 · 랜턴 삐걱 …) — 「stepn: 16」 + 「step2~ …」
        if (nth && SkinAudio.Play(CharSkin, "step2", 0.06f, 3f, SkinAudio.Prio.Step)) return;
        SkinAudio.Play(CharSkin, "step", vol, 4f, SkinAudio.Prio.Step);
    }

    // ================================================================= 평타 (AttackFired 구독 + 신호가 없는 곳의 직접 호출 — D9)
    void OnAttackFired(Vector3 origin, Vector2 dir)
    {
        // 공사 반장의 신호는 밀기 틱마다 옴 → 소리만 (그림은 덤프 때 FmDump 가 직접 부름)
        // 인형사는 영웅 손이 아니라 더미가 휘두름 → 몸짓 · 무기 발사 그림(솜 · 나무 조각 · 뼈)을 더미 자리에서 (2026-11-04 마무리)
        if (CharacterData.Selected == CharacterId.Puppeteer && CharacterKit.Instance != null && CharacterKit.Instance.PupDummyAt(out Vector3 dummy)) origin = dummy;
        OnAttack(origin, dir, CharacterData.Selected == CharacterId.Foreman ? 1 : 0);
    }

    // kind: 0 보통 · 1 틱(소리만) · 2 덤프 — 같은 프레임 두 번째는 무시 (사진사는 한 프레임에 두 번 옴)
    //   2026-11-04~ PlayerController.AttackFired 를 늘리지 않으려고 만든 길: 그 이벤트는 사막 베타 표지판(업적형 전설 해금 비밀)이 구독함
    public static void OnAttack(Vector3 origin, Vector2 dir, int kind = 0)
    {
        try { OnAttackCore(origin, dir, kind); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnAttackCore(Vector3 origin, Vector2 dir, int kind = 0)
    {
        if (Instance == null || Off) return;
        Instance.Attack(origin, dir, kind);
    }

    void Attack(Vector3 origin, Vector2 dir, int kind)
    {
        if (Time.frameCount == attackFrame) return;
        attackFrame = Time.frameCount;
        lastAttackAt = Time.time;
        if (kind != 1) CursorSkin.OnAttack();
        // 무기 스킨을 끼면 평타 몸짓 칸은 꺼지고 무기 발사가 그 자리 (6-1절)
        if (WeaponSkin != null) { WeaponFire(origin, dir, kind); return; }
        if (CharLook == null) return;
        if (kind == 1)
        {
            if (SlotReady("acc.tick", 0.12f) && !RunSave.Replaying) SkinAudio.Play(CharSkin, "acc", 0.22f, 5f, SkinAudio.Prio.Acc);
            return;
        }
        if (!SlotReady("acc", 0.12f)) return;
        Slot(CharSkin, CharLook, "acc", origin, dir, true, 30, Bud.Rep, 0.25f, 5f, SkinAudio.Prio.Acc);
    }

    // ================================================================= 피격 · 레벨업 · 필살기 · 버팀 · 사망 · 승리
    // 피격 그림 (소리는 SkinAudio.PlayHurt — 예전 소리와 새 변주 번갈아)
    public static void OnHurt()
    {
        try { OnHurtCore(); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnHurtCore()
    {
        SkinFx f = Instance;
        if (f == null || Off || CharLook == null || !f.SlotReady("hurt", 0.2f)) return;
        string fx = CharLook.Fx("hurt");
        if (fx != null) f.Emit(fx, CharLook, f.transform.position, Vector2.up, true, 30, Bud.Rare);
    }

    // 레벨업 (Juice.LevelUp): 그림 + 레벨업 소리 자리 교체 → 바꿨으면 true (원래 levelup 소리를 안 냄)
    public static bool OnLevelUp(Vector3 pos)
    {
        try { return OnLevelUpCore(pos); }
        catch (System.Exception e) { GuardFail(e); return false; }
    }

    static bool OnLevelUpCore(Vector3 pos)
    {
        SkinFx f = Instance;
        if (f == null || Off || CharLook == null) return false;
        // 1초 안의 연달은 레벨업: 스킨 소리가 있으면 기본 소리로 섞이지 않게 소리만 생략
        if (!f.SlotReady("lvl", 1f)) return SkinAudio.Has(CharSkin, "lvl");
        string fx = CharLook.Fx("lvl");
        if (fx != null) f.Emit(fx, CharLook, pos, Vector2.up, true, 30, Bud.Rare);
        return !RunSave.Replaying && SkinAudio.Play(CharSkin, "lvl", 0.8f, 0f, SkinAudio.Prio.Lvl);
    }

    public void OnUlt()
    {
        try { OnUltCore(); }
        catch (System.Exception e) { GuardFail(e); }
    }

    void OnUltCore()
    {
        if (Off) { LegacyUlt(); return; }
        if (CharSkin == null) return;
        // 전설 전용 필살기 소리는 그대로 (기존 구매자) · 고리(fx_shock) 대신 세로 시전 연출 (D4)
        if (CharSkin.tier >= 3) SpecialAbilities.SharedFx?.Play(SkinAudio.UltSound(CharSkin.id), 0.6f);
        if (CharLook == null || !SlotReady("ultc", 1f)) return;
        CornerOnUlt();      // F12: 파노라마 「저장됨」 썸네일 (설명 약속)
        Slot(CharSkin, CharLook, "ultc", transform.position, Vector2.up, true, 31, Bud.Rare, 0.55f, 0f, SkinAudio.Prio.Ult);
    }

    // 버팀 · 부활 · 교대 등장 (불사의 맹세 · 최후의 보루 · 영혼 릴레이) — 희귀부터
    public static void OnRevive()
    {
        try { OnReviveCore(); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnReviveCore()
    {
        SkinFx f = Instance;
        if (f == null || Off || CharLook == null || RunSave.Replaying || !f.SlotReady("revive", 1f)) return;
        f.Slot(CharSkin, CharLook, "revive", f.transform.position, Vector2.up, true, 31, Bud.Rare, 0.65f, 0f, SkinAudio.Prio.Revive);
    }

    void OnLastStand(PlayerController p, Vector3 at) => OnRevive();

    // 사망 (PlayerController.DeathSequence): 기존 붉은 터짐 · bigboom 자리 — 바꿨으면 true
    //   그 1.1초(실제) 동안 시간 배율이 0.25 → 0.05 라 그림은 실제 시간으로 (4-0절)
    public static bool OnDeath()
    {
        try { return OnDeathCore(); }
        catch (System.Exception e) { GuardFail(e); return false; }
    }

    static bool OnDeathCore()
    {
        SkinFx f = Instance;
        if (f == null || Off || CharLook == null || CharLook.Fx("death") == null) return false;
        if (f.skinBody != null) f.skinBody.Clear();
        f.Emit(CharLook.Fx("death"), CharLook, f.transform.position, Vector2.up, true, 31, Bud.Rare, true);
        if (!RunSave.Replaying) SkinAudio.Play(CharSkin, "death", 0.7f, 0f, SkinAudio.Prio.Death);
        return true;
    }

    // 승리: 장 보스 처치(Juice.BossDown — 슬로 0.35초 뒤) · 최종 클리어(StageManager.Cleared)
    public static void OnVictory()
    {
        try { OnVictoryCore(); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnVictoryCore()
    {
        SkinFx f = Instance;
        if (f == null || Off || CharLook == null || RunSave.Replaying || f.player == null || f.player.IsDying) return;
        if (!f.SlotReady("win", 2f)) return;
        f.StartCoroutine(f.VictoryLater());
    }

    IEnumerator VictoryLater()
    {
        yield return new WaitForSecondsRealtime(0.35f);
        if (CharLook == null || player == null || player.IsDying) yield break;
        Slot(CharSkin, CharLook, "win", transform.position, Vector2.up, true, 31, Bud.Rare, 0.6f, 0f, SkinAudio.Prio.Win);
    }

    void OnCleared(Difficulty d) => OnVictory();

    // 처치 흡수 (영웅 = 연쇄 때 · 전설 = 매번) — 쓰러진 자리 → 몸 (6-1절: 처치 자리는 이펙트 스킨 몫, 캐릭터는 몸으로)
    float absorbAt;
    void CharAbsorb(Vector3 pos, bool combo)
    {
        if (CharLook == null) return;
        string fx = CharLook.Fx("absorb");
        if (fx == null) return;
        bool every = CharSkin.tier >= 3;
        if (!combo && !every) return;
        if (!combo && Time.time < absorbAt) return;
        absorbAt = Time.time + 0.06f;
        Emit(fx, CharLook, pos, Vector2.up, false, 27, Bud.Rep);
        if (combo && !RunSave.Replaying) SkinAudio.Play(CharSkin, "absorb", 0.55f, 0f, SkinAudio.Prio.Absorb);
    }

    // ================================================================= 상점 미리보기 무대 (SkinPreview) — 게임과 같은 레시피를 그 순간만 다시
    public bool PreviewSlot(string slot, Vector3 at)
    {
        if (Off) return false;
        switch (slot)
        {
            case "lvl": slotAt.Remove("lvl"); bool lv = OnLevelUp(transform.position); return lv || (CharLook != null && CharLook.Fx("lvl") != null);
            case "death": return OnDeath();
            case "hurt": slotAt.Remove("hurt"); OnHurt(); return CharLook != null && CharLook.Fx("hurt") != null;
        }
        bool effect = slot == "combo" || slot == "big" || slot == "chest" || slot == "coin";
        bool weapon = slot == "reload" || slot == "widle" || slot == "high" || slot == "evo";
        SkinDef s = effect ? EffectSkin : weapon ? WeaponSkin : CharSkin;
        SkinLook l = effect ? EffectLook : weapon ? WeaponLook : CharLook;
        if (l == null || !l.Has(slot)) return false;
        float vol = slot == "idle" || slot == "widle" ? 0.3f : 0.6f;
        return Slot(s, l, slot, effect || slot == "high" ? at : transform.position, Vector2.right, !(effect || slot == "high"), 30, Bud.Rare, vol, 0f, SkinAudio.Prio.Big);
    }
}
