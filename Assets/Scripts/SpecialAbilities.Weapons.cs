using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 특수 무기 (거너 무기 8종 · 캐릭터 전용 무기의 좌클릭): 조준 표시 · 발사 · 장전
// SpecialAbilities.cs 에서 나눔 (2.1.1)
public partial class SpecialAbilities
{
    // ================================================================= weapons
    void UpdateWeapon()
    {
        bool down = GameInput.FireDown && !OverUI;
        bool held = GameInput.FireHeld && !OverUI;
        bool up = GameInput.FireUp;

        switch (CurrentWeapon)
        {
            case ShotgunId:
                // 공격 범위(부채꼴) 표시
                PreviewCone(previewCone, player.MuzzlePosition, AimDir(), ShotgunHalfAngle, ShotgunRange, new Color(1f, 0.55f, 0.2f, 0.45f), 0.1f);
                if (held && Ready()) FireShotgun();
                break;
            case SniperId:
                UpdateSniper(down, held, up);
                break;
            case DualId:
                if (held && Ready()) FireDual();
                break;
            case FlameId:
                UpdateFlame(held);
                break;
            case SeekerId:
                {
                    // 유도탄이 쫓아갈 적 표시
                    Transform target = Specials.NearestEnemy(MouseWorld(), 20f) ?? Specials.NearestEnemy(player.transform.position, 20f);
                    if (target != null) PreviewRing(targetRing, target.position, 1.6f + Mathf.Sin(Time.time * 8f) * 0.15f, new Color(0.75f, 0.5f, 1f, 0.85f), 0.12f);
                    if (held && Ready()) FireSeeker();
                }
                break;
            case ChainId:
                if (held && Ready()) FireChain();
                break;
            case ScytheId:
                if (activeScythe == null)
                    PreviewLine(aimLine, player.MuzzlePosition, player.MuzzlePosition + (Vector3)(AimDir() * 12f), new Color(0.75f, 0.45f, 1f, 0.5f), 0.1f);
                if (held && activeScythe == null && !player.IsSkillUsing && Time.time >= nextScytheAt) FireScythe();
                player.ammoTextOverride = activeScythe == null ? Loc.T("낫 준비") : Loc.T("낫 회수 중");
                break;
            case GrenadeId:
                {
                    // 떨어질 지점과 폭발 범위
                    Vector3 land = GrenadeLanding();
                    PreviewLine(aimLine, player.MuzzlePosition, land, new Color(1f, 0.5f, 0.15f, 0.4f), 0.08f);
                    PreviewRing(previewRing, land, GrenadeRadius, new Color(1f, 0.45f, 0.1f, Ready() ? 0.75f : 0.3f), 0.1f);
                    if (held && Ready()) FireGrenade();
                }
                break;
            default:
                // 모든 무기: 꾹 누르고 있으면 공격 속도에 맞춰 계속 공격
                if (IsKit(CurrentWeapon)) KitUpdateWeapon(CurrentWeapon, held, held, up);
                break;
        }
    }

    bool Ready() => !player.IsSkillUsing && WeaponHasAmmo(CurrentWeapon) && Time.time >= nextFire;

    Vector2 AimDir() => ((Vector2)(MouseWorld() - player.MuzzlePosition)).normalized;

    float GrenadeRadius => 3.5f + 0.6f * Trait(GrenadeId);

    Vector3 GrenadeLanding()
    {
        Vector3 start = player.MuzzlePosition;
        Vector3 target = MouseWorld();
        if (Vector2.Distance(start, target) > 14f) target = start + (Vector3)(AimDir() * 14f);
        return target;
    }

    // 저격총: 화면 끝까지 닿는 점선 조준선은 항상, 누르고 있으면 충전 (빛, 소리, 완충 알림)
    // 계속 누르고 있으면 완충되는 순간 발사하고 다시 충전 (꾹 눌러 연사), 일찍 떼면 그만큼만 충전해 발사
    void UpdateSniper(bool down, bool held, bool up)
    {
        Vector3 muzzle = player.MuzzlePosition;
        Vector3 mouse = MouseWorld();
        // 1.2~ 공격 고정 설정: 누름 · 뗌은 고정을 걸고 푸는 데 쓰이므로 무시 (걸 때 · 풀 때 덜 찬 약한 한 발이 나가지 않게)
        //   고정 중에는 완충 자동 발사만 · 풀려서 누르고 있지 않으면 충전을 거둠
        if (GameSettings.AttackLock && !GamePad.Active && !TutorialRun.Active && !GameInput.Auto)
        {
            down = up = false;
            if (!held && sniperCharge >= 0f) CancelSniperCharge();
        }

        if ((down || (held && sniperCharge < 0f)) && Ready())
        {
            sniperCharge = 0f;
            chargeFullPlayed = false;
            chargeGlow = MakeSprite("SniperCharge", glowSprite, muzzle, 0.05f, new Color(0.5f, 0.95f, 1f, 0.8f), "Effect", 6);
        }

        float k = sniperCharge >= 0f ? sniperCharge / SniperChargeTime : 0f;
        bool full = k >= 1f;
        Color lineColor = sniperCharge < 0f ? new Color(0.5f, 0.95f, 1f, 0.35f)
            : full ? Color.Lerp(new Color(1f, 0.85f, 0.3f, 0.7f), new Color(1f, 1f, 0.8f, 1f), Mathf.PingPong(Time.time * 6f, 1f))
            : new Color(0.5f, 0.95f, 1f, 0.4f + 0.5f * k);
        // 화면 끝까지 닿는 점선 조준선 (충전할수록 밝고 굵어짐)
        PreviewLine(aimLine, muzzle, muzzle + (Vector3)(AimDir() * ScreenEdgeDistance(muzzle, AimDir())), lineColor, 0.08f + 0.1f * k);
        if (sniperCharge < 0f) return;

        sniperCharge = Mathf.Min(SniperChargeTime, sniperCharge + Time.deltaTime);
        player.ammoTextOverride = full ? Loc.T("완충!") : Loc.T("충전 ") + Mathf.RoundToInt(k * 100f) + "%";
        fx.StartLoop("hum", 0.5f, 0.7f + 0.9f * k);

        if (chargeGlow != null)
        {
            chargeGlow.transform.position = muzzle;
            float pulse = full ? 1f + Mathf.Sin(Time.time * 18f) * 0.15f : 1f;
            chargeGlow.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 0.28f, k) * pulse;
            chargeGlow.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.5f, 0.95f, 1f, 0.8f), new Color(1f, 0.85f, 0.3f, 0.95f), k);
        }

        if (full && !chargeFullPlayed)
        {
            chargeFullPlayed = true;
            fx.Play("ding", 0.8f);
        }

        // 떼면 지금까지 충전한 만큼, 누르고 있으면 완충되는 순간 발사
        if (up || (full && held))
        {
            FireSniper(k);
            CancelSniperCharge();
        }
    }

    const float SniperLineLength = 40f;

    // 화면 가장자리까지의 거리 (조준선을 화면 끝까지 그릴 때)
    public float ScreenEdgeDistance(Vector3 from, Vector2 dir)
    {
        Camera cam = player.MainCamera;
        if (cam == null) return SniperLineLength;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector3 c = cam.transform.position;
        float t = 500f;
        if (dir.x > 0.0001f) t = Mathf.Min(t, (c.x + w - from.x) / dir.x);
        else if (dir.x < -0.0001f) t = Mathf.Min(t, (c.x - w - from.x) / dir.x);
        if (dir.y > 0.0001f) t = Mathf.Min(t, (c.y + h - from.y) / dir.y);
        else if (dir.y < -0.0001f) t = Mathf.Min(t, (c.y - h - from.y) / dir.y);
        return Mathf.Max(1f, t) + 2f;          // 화면 밖까지 조금 더 (가장자리에서 끊겨 보이지 않게)
    }
    float SniperChargeTime => (IsEvolved(SniperId) ? 0.8f : 1.2f) * (1f - 0.15f * Trait(SniperId));

    void CancelSniperCharge()
    {
        sniperCharge = -1f;
        if (chargeGlow != null) Destroy(chargeGlow);
        if (fx != null && CurrentWeapon != FlameId) fx.StopLoop();
    }

    // 공통: 발사 준비 (방향, 소리, 탄약, 탄창 저주)
    Vector2 BeginShot(int ammoCost, out Vector3 start, out bool cursed)
    {
        Vector3 target = MouseWorld();
        player.FaceTowards(target);
        start = player.MuzzlePosition;
        nextFire = Time.time + BaseInterval(CurrentWeapon) / player.fireRateMultiplier * WeaponRateMul(CurrentWeapon) * TreeRateMul;
        // 무기마다 자기 탄창을 씀 (권총 탄창과 별개)
        bool kitWeapon = IsKit(CurrentWeapon);
        Vector2 aimDir = ((Vector2)(target - start)).normalized;
        if (!kitWeapon)
        {
            // 총: 총구 섬광 (검 · 표창 · 활 · 플라스크에는 없음)
            Flash(start, 1.3f, new Color(WeaponColor(CurrentWeapon).r, WeaponColor(CurrentWeapon).g, WeaponColor(CurrentWeapon).b, 0.85f), 0.08f);
            Fx.Spawn("fx_muzzle", start + (Vector3)(aimDir * 0.4f), 1.4f, WeaponColor(CurrentWeapon), 24f, Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg, 15);
        }
        PlayerLook.Fired(CurrentWeapon);
        WeaponAmmo mag = Ammo(CurrentWeapon);
        cursed = IsLastBulletCursed(mag.ammo);
        mag.ammo = Mathf.Max(0, mag.ammo - ammoCost);
        if (mag.ammo <= 0) StartWeaponReload(CurrentWeapon);
        if (!kitWeapon && Gunner) SignatureSkills.Attacked(start, aimDir);     // 2.1.8: 총열 과열 · 속사 장전도 특수 총기로 (예전엔 권총만)
        if (kitWeapon) KitShotSound(CurrentWeapon);
        else if (!SkinAudio.PlayPistol() && player.shotSound != null && player.TryGetComponent(out AudioSource a)) a.PlayOneShot(player.shotSound, GameSettings.SfxVolume);
        return ((Vector2)(target - start)).normalized;
    }

    Bullet Shot(Vector3 start, Vector2 dir, float dmg, int pene, float knockRate, bool cursed, Color tint, float speedMul = 1f, float scale = 1f, float skillCharge = 1f)
    {
        if (pene < 50 && CurrentWeapon != DualId) pene += TreePene;
        Bullet b = player.CreateBullet(start, dir, dmg * (cursed ? 3f : 1f), pene, 0, false, knockRate);
        if (b == null) return null;
        // 스킬 게이지: 화염 방사기가 기준, 나머지 무기는 천천히
        b.skillCharge = skillCharge * (CurrentWeapon == FlameId ? 1f : PlayerController.GaugeRate);
        b.speed *= speedMul;
        b.transform.localScale *= scale;
        if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = tint;
        CurseBullet(b, cursed);
        ApplyGunCards(b, CurrentWeapon == FlameId);
        return b;
    }

    // 총알 없이 앞쪽 부채꼴 안의 적을 즉시 타격하는 근거리 폭발
    float ShotgunRange => 6f + Trait(ShotgunId) + (IsEvolved(ShotgunId) ? 2f : 0f);
    float ShotgunHalfAngle => IsEvolved(ShotgunId) ? 40f : 30f;

    void FireShotgun()
    {
        Vector2 dir = BeginShot(1, out Vector3 start, out bool cursed);
        bool evo = IsEvolved(ShotgunId);
        float dmg = WDamage * (evo ? 3.2f : 2.5f) * (cursed ? 3f : 1f);
        bool hitAny = false;
        bool boom = GunBoomShot();

        using (var ovl = PhysHits.Circle(start, ShotgunRange)) foreach (Collider2D c in ovl)
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Vector2 to = (Vector2)(c.transform.position - start);
            if (Vector2.Angle(dir, to) > ShotgunHalfAngle) continue;

            Specials.Damage(c.gameObject, dmg, to.normalized, 2.5f);
            if (evo) Burn.Apply(c.gameObject, WDamage * 0.5f, 2f);
            GunCardHit(c, dmg, to.normalized, false, boom && !hitAny);
            hitAny = true;
            if (cursed) Explode(c.transform.position, 2.5f, Damage * 1.5f, 1.5f, new Color(1f, 0.3f, 0.2f, 0.85f));
        }

        fx.Play("shotgun", 0.9f, 1.05f);
        fx.Shake(0.3f, 0.14f);

        // 부채꼴을 따라 불꽃이 퍼지는 연출
        for (int i = -2; i <= 2; i++)
        {
            Vector2 d = Quaternion.Euler(0, 0, i * ShotgunHalfAngle / 2f) * dir;
            for (int k = 1; k <= 3; k++)
                Flash(start + (Vector3)(d * ShotgunRange * k / 3.5f), 1.2f + k * 0.6f, new Color(1f, 0.55f, 0.2f, 0.8f), 0.18f + k * 0.04f);
        }

        // 스킬 게이지는 맞힌 경우에만 조금 참
        if (hitAny)
        {
            SkillGauge gauge = FindFirstObjectByType<SkillGauge>();
            // 스킬 게이지는 시간으로 참
        }
    }

    void FireSniper(float charge)
    {
        if (player.IsSkillUsing || !WeaponHasAmmo(SniperId)) return;
        Vector2 dir = BeginShot(1, out Vector3 start, out bool cursed);
        // 바로 떼면 약하고 완충할수록 강함 (연타가 완충 사격보다 초당 피해가 높지 않게)
        Bullet b = Shot(start, dir, WDamage * Mathf.Lerp(0.6f, 3.2f, charge), 999, 1.5f, cursed, new Color(0.5f, 0.95f, 1f), 1.6f, 1.4f, 1.5f);
        // 진화: 완충 사격이 맞은 곳마다 폭발
        if (b != null && charge >= 1f && IsEvolved(SniperId))
        {
            float boom = WDamage * 1.5f;
            b.onHitEnemy += (bullet, col) => Explode(col.transform.position, 3f, boom, 1.5f, new Color(0.5f, 0.95f, 1f, 0.85f));
        }
        fx.Play("crack", 0.7f + 0.3f * charge, 1.2f - 0.3f * charge);
        fx.Shake(0.15f + 0.25f * charge, 0.12f);
    }

    void FireDual()
    {
        dualToggle = !dualToggle;
        Vector2 dir = BeginShot(dualToggle ? 1 : 0, out Vector3 start, out bool cursed);
        Vector3 side = new Vector3(-dir.y, dir.x) * 0.35f;
        fx.Play("gunshot", 0.45f, dualToggle ? 1.05f : 1.2f);
        int pene = player.pene + Trait(DualId);
        // 진화: 양쪽 총구에서 동시에
        foreach (float s in IsEvolved(DualId) ? new[] { 1f, -1f } : new[] { dualToggle ? 1f : -1f })
        {
            Flash(start + side * s, 0.9f, new Color(1f, 0.85f, 0.5f, 0.8f), 0.06f);
            Shot(start + side * s, dir, WDamage * 0.55f, pene, 0.6f, cursed, new Color(1f, 0.85f, 0.5f), 1f, 1f, 0.25f);
        }
    }

    float nextFlameLook;

    void UpdateFlame(bool held)
    {
        if (overheated && heat <= 0.3f)
        {
            overheated = false;
            fx.Play("chime", 0.4f, 1.2f);
            fx.FloatText(player.transform.position, Loc.T("냉각 완료"), new Color(0.5f, 0.95f, 1f));
        }
        bool firing = held && !overheated && !player.IsSkillUsing;

        // 켤 때 점화음, 뿜는 동안 불길 소리 (열이 오를수록 조금 높아짐)
        if (firing && !flameWasFiring) fx.Play("ignite", 0.9f);
        if (firing) fx.StartLoop("flame", 0.95f, 0.9f + heat * 0.25f);
        if (firing && Time.time >= nextFlameLook) { nextFlameLook = Time.time + 0.07f; PlayerLook.Fired(FlameId); }
        if (!firing) fx.StopLoop();     // 1.1~: else 가 위 if 에 붙어 쏘는 중에도 매 프레임 끊기던 문제
        flameWasFiring = firing;

        // 총구 불빛
        if (firing && flameMuzzle == null)
            flameMuzzle = MakeSprite("FlameMuzzle", glowSprite, player.MuzzlePosition, 0.12f, new Color(1f, 0.6f, 0.2f, 0.9f), "Effect", 7);
        if (!firing && flameMuzzle != null) Destroy(flameMuzzle);
        if (flameMuzzle != null)
        {
            flameMuzzle.transform.position = player.MuzzlePosition;
            flameMuzzle.transform.localScale = Vector3.one * Random.Range(0.1f, 0.16f);
        }

        // 과열 중에는 총구에서 연기
        if (overheated && Random.value < Time.deltaTime * 10f)
            FlameParticle.Spawn(glowSprite, player.MuzzlePosition, Vector2.up * Random.Range(1.5f, 3f) + Random.insideUnitCircle, 0.9f, 0.08f, 0.35f, true);

        if (firing && Time.time >= nextFire)
        {
            Vector3 target = MouseWorld();
            player.FaceTowards(target);
            Vector3 start = player.MuzzlePosition;
            Vector2 aim = ((Vector2)(target - start)).normalized;
            Vector2 dir = Quaternion.Euler(0, 0, Random.Range(-8f, 8f)) * aim;
            nextFire = Time.time + 0.07f / player.fireRateMultiplier * WeaponRateMul(FlameId) * TreeRateMul;

            // 실제 피해는 보이지 않는 판정용 탄이 담당
            Bullet b = Shot(start, dir, WDamage * 0.25f, 99, 0.2f, false, new Color(1f, 0.55f, 0.15f, 0.9f), 0.2f, 1.6f, 0.05f);
            if (b != null)
            {
                b.lifetime = 0.35f;
                if (b.TryGetComponent(out SpriteRenderer hitboxSr)) hitboxSr.enabled = false;
                float burnDps = WDamage * (IsEvolved(FlameId) ? 0.5f : 0.3f);
                b.onHitEnemy += (bullet, col) => Burn.Apply(col.gameObject, burnDps, 2f);
            }

            // 퍼져 나가는 불꽃과 불티
            for (int i = 0; i < 3; i++)
            {
                Vector2 v = (Vector2)(Quaternion.Euler(0, 0, Random.Range(-14f, 14f)) * aim) * Random.Range(22f, 32f);
                FlameParticle.Spawn(glowSprite, start + (Vector3)(aim * 0.3f), v, Random.Range(0.35f, 0.5f), 0.05f, Random.Range(0.45f, 0.7f), false);
            }
            if (Random.value < 0.6f)
            {
                Vector2 v = (Vector2)(Quaternion.Euler(0, 0, Random.Range(-25f, 25f)) * aim) * Random.Range(12f, 24f);
                FlameParticle.SpawnEmber(glowSprite, start, v);
            }

            heat += 0.035f * (1f - 0.2f * Trait(FlameId)) * (IsEvolved(FlameId) ? 0.6f : 1f);
            if (heat >= 1f)
            {
                heat = 1f;
                overheated = true;
                fx.StopLoop();
                fx.Play("hiss", 0.9f);
                fx.FloatText(player.transform.position, Loc.T("과열!"), new Color(1f, 0.35f, 0.25f), 6f);
            }
        }
        else if (!firing)
        {
            // 쏘지 않을 때만 식음
            heat = Mathf.Max(0f, heat - Time.deltaTime * 0.35f);
        }
        player.ammoTextOverride = overheated ? Loc.T("과열!") : Loc.T("열기 ") + Mathf.RoundToInt(heat * 100f) + "%";
    }

    void FireSeeker()
    {
        Vector2 dir = BeginShot(1, out Vector3 start, out bool cursed);
        fx.Play("whoosh", 0.4f, 1.6f);
        // 진화: 양옆으로 두 발
        int count = IsEvolved(SeekerId) ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            Vector2 d = count == 1 ? dir : (Vector2)(Quaternion.Euler(0, 0, i == 0 ? -14f : 14f) * dir);
            Bullet b = Shot(start, d, WDamage * 0.7f, 1 + Trait(SeekerId), 0.5f, cursed, new Color(0.7f, 0.5f, 1f), 0.3f, 1.2f, 0.8f);
            if (b != null) b.gameObject.AddComponent<Homing>().turnSpeed = 360f;
        }
    }

    void FireChain()
    {
        Vector2 dir = BeginShot(1, out Vector3 start, out bool cursed);
        Bullet b = Shot(start, dir, WDamage, 1, 1f, cursed, new Color(0.6f, 0.9f, 1f));
        fx.Play("zap", 0.5f, 0.9f);
        fx.Play("gunshot", 0.3f, 1.4f);
        if (b != null)
        {
            float dmg = WDamage;
            bool evo = IsEvolved(ChainId);
            int jumps = (evo ? 7 : 4) + Trait(ChainId);
            b.onHitEnemy += (bullet, col) => ChainLightning(col.transform.position, col, dmg * 0.8f, jumps, evo ? 0.9f : 0.8f);
        }
    }

    float nextScytheAt;

    void FireScythe()
    {
        nextScytheAt = Time.time + 0.35f;
        Vector3 target = MouseWorld();
        player.FaceTowards(target);
        bool evo = IsEvolved(ScytheId);
        float grow = 1f + 0.15f * Trait(ScytheId);
        Bullet b = Shot(player.MuzzlePosition, ((Vector2)(target - player.MuzzlePosition)).normalized, WDamage * (evo ? 1.75f : 1.2f), 9999, 1.2f, false,
                        new Color(0.75f, 0.45f, 1f), 0f, (evo ? 3.5f : 2.5f) * grow, 0.3f);
        if (b == null) return;
        b.lifetime = 5f;
        b.hitOnce = new HashSet<int>();         // 적마다 갈 때 한 번, 돌아올 때 한 번 (Scythe가 비움)
        Scythe s = b.gameObject.AddComponent<Scythe>();
        s.distance = (evo ? 16f : 12f) * grow;
        // 총알 대신 코드로 그린 낫을 보여줌 (판정은 그대로)
        AttachScytheVisual(b, (evo ? 1.7f : 1.3f) * grow);
        s.outTime = 0.45f * WeaponRateMul(ScytheId);
        s.owner = player.transform;
        s.direction = ((Vector2)(target - player.MuzzlePosition)).normalized;
        PlayerLook.Fired(ScytheId);
        activeScythe = b.gameObject;
        fx.Play("whoosh", 0.7f, 0.9f);
    }

    void FireGrenade()
    {
        Vector3 target = GrenadeLanding();
        BeginShot(2, out Vector3 start, out bool cursed);
        fx.Play("thump", 0.8f);
        GameObject g = MakeSprite("LavaGrenade", glowSprite, start, 1.2f, new Color(1f, 0.45f, 0.1f), "Effect", 5);
        Grenade gr = g.AddComponent<Grenade>();
        gr.owner = this;
        gr.target = target;
        gr.damage = WDamage * 2f * (cursed ? 3f : 1f);
        gr.radius = GrenadeRadius;
        gr.cluster = IsEvolved(GrenadeId);
    }
}
