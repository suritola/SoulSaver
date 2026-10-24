using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 특수 스킬 (키 1 · 2 · 3): 쿨타임 · 조준 미리보기 · 사용
// SpecialAbilities.cs 에서 나눔 (2.1.1)
public partial class SpecialAbilities
{
    // ================================================================= skills
    // 스킬 키: 쿨타임 중이면 경고, 조준형은 누르고 있는 동안 미리보기 후 떼면 사용
    void HandleSkillKey(int id, GameAction key)
    {
        // 상점 제단 앞에서는 상호작용 키가 상점 열기 (스킬 키를 같은 키로 바꿨을 때)
        if (KeyBindings.Get(key) == KeyBindings.Get(GameAction.Interact) && ShopStall.PlayerNear) return;
        if (KeyBindings.Down(key))
        {
            KeyTried(id);           // 1.1.2~ 처음 눌러 봤으면 키 배지 맥동을 멈춤
            float left = CooldownUntil(id) - Time.time;
            if (left > 0f)
            {
                fx.Play("buzz", 0.6f);
                fx.FloatText(player.transform.position, Loc.T(abilities[id].name) + " " + left.ToString("0.0") + Loc.T("초"), new Color(0.7f, 0.66f, 0.72f), 4f, 0.4f);
                return;
            }
            if (player.IsSkillUsing) return;
            if (IsAimedSkill(id)) aimingSkill = id;
            else UseSkill(id);
        }

        if (aimingSkill == id && KeyBindings.Up(key))
        {
            aimingSkill = -1;
            if (Time.time >= CooldownUntil(id) && !player.IsSkillUsing) UseSkill(id);
        }
    }

    void ShowSkillPreview(int id)
    {
        Vector3 from = player.transform.position;
        Vector3 mouse = MouseWorld();
        Vector2 dir = ((Vector2)(mouse - from)).normalized;
        switch (id)
        {
            case DashId:
                {
                    Vector3 end = ClampToArena(from + (Vector3)(dir * DashDistance));
                    PreviewLine(aimLine, from, end, new Color(0.5f, 0.95f, 1f, 0.7f), 0.1f);
                    PreviewRing(previewRing, end, 0.9f, new Color(0.5f, 0.95f, 1f, 0.8f), 0.1f);
                }
                break;
            case FireZoneId:
                PreviewRing(previewRing, mouse, FireZoneRadius, new Color(1f, 0.45f, 0.1f, 0.8f), 0.12f);
                break;
            case HookId:
                {
                    HookTarget(from, dir, out Collider2D enemy, out float dist);
                    Vector3 end = from + (Vector3)(dir * dist);
                    PreviewLine(aimLine, from, end, new Color(0.85f, 0.15f, 0.2f, 0.8f), 0.1f);
                    if (enemy != null) PreviewRing(targetRing, enemy.transform.position, 1.6f, new Color(1f, 0.2f, 0.25f, 0.9f), 0.14f);
                    else PreviewRing(previewRing, end, 0.8f, new Color(0.85f, 0.15f, 0.2f, 0.6f), 0.08f);
                }
                break;
        }
    }

    float FireZoneRadius => IsEvolved(FireZoneId) ? 4.5f : 3f;
    int SoulsNeeded => IsEvolved(SoulBurstId) ? 3 : 5;

    void UseSkill(int id)
    {
        using var source = DamageSource.As(SourceOf(id));
        bool evo = IsEvolved(id);
        switch (id)
        {
            case DashId: StartCoroutine(Dash()); StartCooldown(id, evo ? 1.8f : 3f); fx.Play("whoosh", 0.9f, 1.2f); break;
            case FireZoneId:
                DamageZone zone = SpawnZone(MouseWorld(), FireZoneRadius, evo ? 6f : 4f, Damage * 0.75f, new Color(1f, 0.4f, 0.1f, 0.8f));
                zone.lava = true;
                ShockRing.Spawn(zone.transform.position, 0.3f, FireZoneRadius * 1.2f, 0.35f, new Color(1f, 0.55f, 0.2f, 0.9f), 0.3f);
                // 1.3.3~ 도는 마법진(fx_rune)은 뺌 — 불 바닥(FireGround)이 장판을 그림
                Fx.Spawn("fx_explosion", zone.transform.position, FireZoneRadius * 1.6f, Color.white, 16f);
                StartCooldown(id, evo ? 9f : 12f);
                fx.Play("boom", 0.5f, 0.8f);
                fx.Play("crackle", 0.8f);
                break;
            case TimeWarpId:
                StartCoroutine(TimeWarp());
                StartCooldown(id, 20f);
                fx.Play("shimmer", 0.9f, 0.6f);
                fx.FloatText(player.transform.position, Loc.T("시간 왜곡!"), new Color(0.55f, 0.85f, 1f), 5f, 0f);
                break;
            case PactId:
                StartCoroutine(Pact());
                StartCooldown(id, 25f);
                fx.Play("pulse", 1f, 0.8f);
                fx.FloatText(player.transform.position, Loc.T("희생의 계약! 공격력 2배"), new Color(1f, 0.3f, 0.3f), 5f, 0f);
                break;
            case SoulBurstId:
                if (souls < SoulsNeeded)
                {
                    fx.Play("buzz", 0.6f);
                    fx.FloatText(player.transform.position, Loc.T("영혼 부족 ") + souls + "/" + SoulsNeeded, new Color(0.7f, 0.66f, 0.72f), 4.5f, 0.4f);
                    return;
                }
                Explode(player.transform.position, evo ? 10f : 7f, Damage * (1.5f + souls * 0.25f), 3f, new Color(0.6f, 0.95f, 1f, 0.9f));
                souls = 0;
                StartCooldown(id, 5f);
                break;
            case SkeletonsId:
                SummonSkeletons(evo ? 5 : 3);
                StartCooldown(id, evo ? 10f : 15f);
                fx.Play("shimmer", 0.8f, 1.2f);
                fx.FloatText(player.transform.position, Loc.T("해골 소환!"), new Color(0.6f, 0.95f, 1f), 4.5f, 0f);
                break;
            case MirrorId:
                StartCoroutine(Mirror());
                StartCooldown(id, 12f);
                fx.Play("shimmer", 0.8f, 1.6f);
                break;
            case HookId: Hook(); StartCooldown(id, evo ? 2f : 4f); fx.Play("clank", 0.8f); break;
            default: KitUseSkill(id); return;     // 캐릭터 스킬은 KitUseSkill 안에서 연금 순환 (두 번 터지지 않게)
        }
        CycleOnSkill(id, player.transform.position);     // 1.5.9~ 거너 계열 스킬도 연금 순환 (영혼 부족으로 빠진 영혼 폭발은 위에서 return)
    }

    float CooldownUntil(int id) => cooldownUntil.TryGetValue(id, out float t) ? t : 0f;

    void StartCooldown(int id, float seconds)
    {
        seconds *= TreeCooldownMul;
        if (relics.Contains(id)) seconds *= RelicCooldownMul;     // 1.2~ 유물로 되살린 스킬은 쿨타임 1.5배
        cooldownLength[id] = seconds;
        cooldownUntil[id] = Time.time + seconds;
        wasReady[id] = false;
    }

    // 쿨타임이 끝나면 알림음과 HUD 반짝임 (1.2~ 유물 칸 스킬도) — 목록을 바로 돌아 매 프레임 이터레이터를 만들지 않음
    void UpdateReadyChimes()
    {
        for (int i = 0; i < skills.Count; i++) ReadyChime(skills[i]);
        for (int i = 0; i < relics.Count; i++) ReadyChime(relics[i]);
    }

    void ReadyChime(int id)
    {
        bool ready = Time.time >= CooldownUntil(id);
        if (ready && wasReady.TryGetValue(id, out bool before) && !before)
        {
            fx.Play("chime", 0.45f);
            rowFlashUntil[id] = Time.time + 0.5f;
        }
        wasReady[id] = ready;
    }

    // 탄창 저주: 마지막 한 발이 장전되면 알림과 붉은 빛
    void UpdateCurseNotice()
    {
        if (!Has(CurseId)) return;
        // 1.4.1~ 과열 사격(갈래)은 탄이 줄지 않으므로 Shoot 처럼 「과열까지 남은 발 수」로 봄 (과열 중에는 저주탄 없음 → 0)
        GunnerPaths gpCurse = GunnerPaths.Live;
        int now = WeaponActive && UsesAmmo(CurrentWeapon) ? Ammo(CurrentWeapon).ammo
            : gpCurse != null && gpCurse.NoAmmo ? (player.IsReloading ? 0 : gpCurse.HeatShotsLeft) : player.NowBullet;
        if (IsLastBulletCursed(now) && !IsLastBulletCursed(lastBullets))
        {
            fx.Play("pulse", 0.6f, 1.6f);
            fx.FloatText(player.transform.position, Loc.T("저주탄 장전!"), new Color(1f, 0.3f, 0.3f), 4.5f, 0f);
        }
        lastBullets = now;

        bool show = IsLastBulletCursed(now) && (!WeaponActive || UsesAmmo(CurrentWeapon));
        if (show && curseGlow == null) curseGlow = MakeSprite("CurseGlow", glowSprite, player.MuzzlePosition, 0.18f, new Color(1f, 0.2f, 0.2f, 0.8f), "Effect", 6);
        if (!show && curseGlow != null) Destroy(curseGlow);
        if (curseGlow != null)
        {
            curseGlow.transform.position = player.MuzzlePosition;
            curseGlow.transform.localScale = Vector3.one * (0.16f + Mathf.Sin(Time.time * 10f) * 0.04f);
        }
    }

    // 지속형 스킬 표시 (시간 왜곡 범위, 희생의 계약 기운)
    void UpdateAuras()
    {
        if (Time.time < timeWarpUntil)
            fx.SetRing(auraRing, player.transform.position, 6f + Mathf.Sin(Time.time * 4f) * 0.4f, new Color(0.5f, 0.8f, 1f, 0.45f), 0.12f);
        else
            SpecialFeedback.Hide(auraRing);

        if (pactAura != null)
        {
            pactAura.transform.position = player.transform.position + new Vector3(0f, -0.8f, 0f);
            pactAura.transform.localScale = Vector3.one * (0.75f + Mathf.Sin(Time.time * 6f) * 0.08f);
        }
    }

    // 조준 미리보기는 그린 프레임 뒤에만 숨김 (예전엔 매 프레임 선 4개 · 점 전부를 끄는 쓰기를 했음)
    bool previewsShown = true;

    void HidePreviews()
    {
        if (!previewsShown) return;
        previewsShown = false;
        SpecialFeedback.Hide(aimLine);
        SpecialFeedback.Hide(previewRing);
        SpecialFeedback.Hide(previewCone);
        SpecialFeedback.Hide(targetRing);
    }

    void PreviewLine(LineRenderer lr, Vector3 a, Vector3 b, Color color, float width) { previewsShown = true; fx.SetLine(lr, a, b, color, width); }
    void PreviewRing(LineRenderer lr, Vector3 center, float radius, Color color, float width) { previewsShown = true; fx.SetRing(lr, center, radius, color, width); }
    void PreviewCone(LineRenderer lr, Vector3 origin, Vector2 dir, float halfAngle, float range, Color color, float width) { previewsShown = true; fx.SetCone(lr, origin, dir, halfAngle, range, color, width); }

    IEnumerator Dash()
    {
        Vector3 start = player.transform.position;
        Vector3 dir = (MouseWorld() - start).normalized;
        Vector3 end = ClampToArena(start + dir * DashDistance);
        player.GrantInvincibility(0.35f);
        ShockRing.Spawn(start, 0.3f, 2.2f, 0.3f, new Color(0.5f, 0.95f, 1f, 0.9f), 0.25f);
        for (int i = 0; i < 3; i++) Fx.Spawn("fx_smoke", start + (Vector3)(Random.insideUnitCircle * 0.6f), 1.8f, new Color(0.6f, 0.9f, 1f), 14f);
        SpriteRenderer body = player.GetComponent<SpriteRenderer>();
        int ghosts = 0;
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            player.transform.position = Vector3.Lerp(start, end, t / 0.15f);
            // 잔상
            if (body != null && t >= ghosts * 0.035f)
            {
                ghosts++;
                GameObject g = MakeSprite("DashGhost", body.sprite, player.transform.position, 1f, new Color(0.5f, 0.95f, 1f, 0.5f), "Character", -1);
                g.transform.localScale = player.transform.lossyScale;
                g.GetComponent<SpriteRenderer>().flipX = body.flipX;
                g.AddComponent<FadeOut>().duration = 0.25f;
            }
            yield return null;
        }
        player.transform.position = end;
        ShockRing.Spawn(end, 0.3f, 1.8f, 0.25f, new Color(0.5f, 0.95f, 1f, 0.9f), 0.2f);
        Fx.Spawn("fx_shock", end, 4f, new Color(0.55f, 0.95f, 1f), 22f);
        for (int i = 0; i < 6; i++) SoulWisp.Spawn(end, end + (Vector3)(Random.insideUnitCircle.normalized * 2.5f), new Color(0.5f, 0.95f, 1f), true);
        // 진화: 도착 지점 충격파
        if (IsEvolved(DashId)) Explode(end, 2.5f, Damage * 1.5f, 2f, new Color(0.5f, 0.95f, 1f, 0.85f));
    }

    IEnumerator TimeWarp()
    {
        bool evo = IsEvolved(TimeWarpId);
        float duration = evo ? 7f : 5f;
        float warpMul = evo ? 0.25f : 0.5f;
        EnermyController.GlobalSpeedMultiplier = warpMul;
        timeWarpUntil = Time.time + duration;
        Flash(player.transform.position, 12f, new Color(0.5f, 0.8f, 1f, 0.5f), 0.5f);
        ShockRing.Spawn(player.transform.position, 1f, 14f, 0.7f, new Color(0.55f, 0.85f, 1f, 0.9f), 0.5f);
        FxAnim clock = Fx.Play("fx_rune", player.transform.position, 12f, new Color(0.55f, 0.85f, 1f, 0.45f), 1f, 0f, 1, true, duration);
        if (clock != null) { clock.follow = player.transform; clock.spin = -25f; }
        Fx.Spawn("fx_shock", player.transform.position, 14f, new Color(0.6f, 0.9f, 1f), 12f);
        ShockRing.Spawn(player.transform.position, 0.5f, 9f, 0.5f, Color.white, 0.2f);
        yield return new WaitForSeconds(duration);
        // 1.5.9~ 자기 값일 때만 되돌림 — 그 사이 시간의 틈 · 시간 정지가 켜졌으면 그쪽이 끝날 때 스스로 되돌림 (판 시작 · 끝에도 1 로 리셋)
        if (Mathf.Approximately(EnermyController.GlobalSpeedMultiplier, warpMul)) EnermyController.GlobalSpeedMultiplier = 1f;
    }

    IEnumerator Pact()
    {
        bool evo = IsEvolved(PactId);
        if (!evo) player.PlayerHealth = Mathf.Max(1f, player.PlayerHealth - player.PlayerMaxHealth * 0.2f);
        // 1.5.9~ 값을 덮어쓰지 않고 곱했다 나눔 (예전엔 = 2 · = 1 이라 신속 물약 · 전투 열기가 끝나며 나눌 때 1 아래로 굳었음)
        if (!pactOn) { player.damageMultiplier *= 2f; player.fireRateMultiplier *= 1.5f; pactOn = true; }
        pactUntil = Mathf.Max(pactUntil, Time.time + (evo ? 12f : 8f));     // 다시 쓰면 시간만 늘림 (먼저 쓴 쪽이 끝나며 끊지 않게)
        if (pactAura != null) Destroy(pactAura);
        pactAura = MakeSprite("PactAura", glowSprite, player.transform.position, 0.75f, new Color(1f, 0.15f, 0.2f, 0.45f), "Background", 8);
        Flash(player.transform.position, 5f, new Color(1f, 0.15f, 0.2f, 0.7f), 0.4f);
        for (int i = 0; i < 14; i++)
            SoulWisp.Spawn(player.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 5f), player.transform.position, new Color(1f, 0.2f, 0.25f));
        ShockRing.Spawn(player.transform.position, 4f, 0.5f, 0.4f, new Color(1f, 0.2f, 0.25f, 0.9f), 0.3f);
        Fx.Spawn("fx_shock", player.transform.position, 7f, new Color(1f, 0.25f, 0.3f), 16f);
        Fx.Spawn("fx_spark", player.transform.position, 3f, new Color(1f, 0.3f, 0.3f), 14f);
        while (pactOn && Time.time < pactUntil) yield return null;     // pactUntil 은 정해진 시각이라 저절로 끝남
        if (!pactOn) yield break;       // 겹쳐 쓴 다른 코루틴이 이미 되돌림
        pactOn = false;
        player.damageMultiplier /= 2f;
        player.fireRateMultiplier /= 1.5f;
        if (pactAura != null) Destroy(pactAura);
        fx.FloatText(player.transform.position, Loc.T("계약 종료"), new Color(0.7f, 0.66f, 0.72f), 4f, 0f);
    }

    IEnumerator Mirror()
    {
        SpriteRenderer body = player.GetComponent<SpriteRenderer>();
        GameObject decoy = MakeSprite("MirrorDecoy", body.sprite, player.transform.position, 1f, new Color(0.55f, 0.9f, 1f, 0.75f), "Character", 0);
        decoy.transform.localScale = player.transform.lossyScale;
        decoy.GetComponent<SpriteRenderer>().flipX = body.flipX;
        EnermyController.Decoy = decoy.transform;
        ShockRing.Spawn(decoy.transform.position, 0.3f, 3f, 0.35f, new Color(0.55f, 0.9f, 1f, 0.9f), 0.25f);
        Fx.Spawn("fx_soulburst", decoy.transform.position, 3.5f, Color.white, 18f);
        for (int i = 0; i < 8; i++) SoulWisp.Spawn(decoy.transform.position, decoy.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 3f), new Color(0.55f, 0.9f, 1f), true);
        player.bodyAlpha = 0.4f;
        bool evo = IsEvolved(MirrorId);
        yield return new WaitForSeconds(evo ? 5f : 3f);
        EnermyController.Decoy = null;
        player.bodyAlpha = 1f;
        // 진화: 분신이 사라지며 폭발
        if (evo && decoy != null) Explode(decoy.transform.position, 4f, Damage * 3f, 2.5f, new Color(0.55f, 0.9f, 1f, 0.9f));
        Destroy(decoy);
    }

    void Hook()
    {
        Vector3 from = player.transform.position;
        Vector2 dir = ((Vector2)(MouseWorld() - from)).normalized;
        float wallDist = HookTarget(from, dir, out Collider2D enemy, out _);

        if (enemy != null)
        {
            DrawBolt(from, enemy.transform.position, new Color(0.9f, 0.15f, 0.2f), 0.25f);
            Flash(enemy.transform.position, 2f, new Color(1f, 0.2f, 0.25f, 0.8f), 0.15f);
            Specials.Damage(enemy.gameObject, Damage * (IsEvolved(HookId) ? 3f : 1.5f), Vector3.zero, 0f);
            if (enemy.CompareTag("enermy")) StartCoroutine(Pull(enemy.transform, from + (Vector3)dir * 2.5f));
        }
        else
        {
            Vector3 end = ClampToArena(from + (Vector3)dir * Mathf.Max(0f, wallDist - 1.5f));
            DrawLine(from, from + (Vector3)dir * wallDist, new Color(0.8f, 0.1f, 0.15f), 0.2f);
            StartCoroutine(PullPlayer(end));
        }
    }

    const float HookRange = 14f;
    static readonly List<RaycastHit2D> hookHits = new List<RaycastHit2D>(64);
    static ContactFilter2D hookFilter;

    // 갈고리가 걸릴 대상: 벽보다 가까운 첫 적 (없으면 null). 반환값 = 벽까지 거리
    float HookTarget(Vector3 from, Vector2 dir, out Collider2D enemy, out float reach)
    {
        enemy = null;
        float enemyDist = HookRange;
        float wallDist = HookRange;
        // CircleCastAll 과 같은 조건(기본 레이캐스트 층 · 트리거 설정 그대로) · 결과는 재사용 목록에 (가장 가까운 것만 고르므로 순서 무관)
        hookFilter.useTriggers = Physics2D.queriesHitTriggers;
        hookFilter.useLayerMask = true;
        hookFilter.layerMask = Physics2D.DefaultRaycastLayers;
        Physics2D.CircleCast(from, 0.6f, dir, hookFilter, hookHits, HookRange);
        foreach (RaycastHit2D hit in hookHits)
        {
            if (hit.collider.CompareTag("enermy") || hit.collider.CompareTag("boss"))
            {
                if (hit.distance < enemyDist) { enemyDist = hit.distance; enemy = hit.collider; }
            }
            else if (hit.collider.CompareTag("Wall") && hit.distance < wallDist && hit.distance > 0.1f)
            {
                wallDist = hit.distance;
            }
        }
        if (enemy != null && enemyDist >= wallDist) enemy = null;
        reach = enemy != null ? enemyDist : wallDist;
        return wallDist;
    }

    IEnumerator Pull(Transform target, Vector3 to)
    {
        Vector3 start = target.position;
        for (float t = 0f; t < 0.15f && target != null; t += Time.deltaTime)
        {
            target.position = Vector3.Lerp(start, to, t / 0.15f);
            yield return null;
        }
    }

    IEnumerator PullPlayer(Vector3 to)
    {
        player.GrantInvincibility(0.3f);
        Vector3 start = player.transform.position;
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            player.transform.position = Vector3.Lerp(start, to, t / 0.2f);
            yield return null;
        }
        player.transform.position = to;
    }

    void SummonSkeletons(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = player.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 2f);
            GameObject s = MakeSprite("AllySkeleton", allySprite, pos, 1f, new Color(0.6f, 0.95f, 1f), "Character", 1);
            s.transform.localScale = Vector3.one * 1.1f;
            Flash(pos, 2.2f, new Color(0.6f, 0.95f, 1f, 0.8f), 0.25f);
            Fx.Spawn("fx_soulburst", pos, 3f, Color.white, 18f);
            ShockRing.Spawn(pos, 0.2f, 1.6f, 0.3f, new Color(0.6f, 0.95f, 1f, 0.9f), 0.15f);
            AllySkeleton a = s.AddComponent<AllySkeleton>();
            a.owner = this;
            a.damage = Damage * 3f;
        }
    }
}
