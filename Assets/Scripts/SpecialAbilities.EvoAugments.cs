using System.Collections.Generic;
using UnityEngine;

// 무기 진화 개편 (1.9.4~): 진화해도 무기를 바꾸지 않음
//   고른 무기의 느낌이 ① 기본 공격에 붙는 능력 ② 우클릭 필살기 두 가지로 들어옴
//   거너: 권총 그대로 + 화염 총구(총알이 불태움) · 영혼 탄두 · 쌍발 총구 …, 필살기는 원래 조준 사격 뒤에 그 무기의 궁극기가 절반 세기로 덧붙음 (화염 회오리 · 관통 레일건 …, 2.2.0~) · 쌍권총만 총알 폭풍으로 바뀜
//   2차 진화는 1차 능력 위에 하나 더 (같은 계열을 고르면 1차 능력이 극대화), 필살기는 2차 무기 것으로
//   다른 캐릭터: 평타 모양은 그대로 + 형태 능력 (CharacterKit.Forms), 필살기는 그 무기의 궁극기 (SpecialAbilities.KitUlts)
public partial class SpecialAbilities
{
    int gunEvo1 = -1, gunEvo2 = -1;
    public int GunEvo1 => gunEvo1;

    // 우클릭 필살기를 정하는 무기 (-1 = 캐릭터 기본 필살기)
    public int UltId => WeaponActive ? CurrentWeapon : Gunner ? (gunEvo2 >= 0 ? gunEvo2 : gunEvo1) : (Kit != null ? Kit.form : -1);
    public bool UltActive => UltId >= 0;

    // 필살기에 덧붙는 궁극기들 (1.0.2): 거너는 1차 · 2차 진화가 다르면 둘 다 (예전엔 2차 것만 남아 쌍권총 → 낫이면 총알 폭풍이 사라졌음)
    public List<int> AddedUltIds()
    {
        List<int> ids = new List<int>();
        if (WeaponActive || !Gunner) { if (UltActive) ids.Add(UltId); return ids; }
        if (gunEvo1 >= 0) ids.Add(gunEvo1);
        if (gunEvo2 >= 0 && gunEvo2 != gunEvo1) ids.Add(gunEvo2);
        return ids;
    }

    bool HasAug(int id) => gunEvo1 == id || gunEvo2 == id;
    bool AugMaxed(int id) => gunEvo1 == id && gunEvo2 == id;      // 2차에서 같은 계열 = 극대화
    // 영혼 트리 숙련 특성 (예전 단계마다 +25%, 쌍발 총구만 +15% — 1.1.2 거너 하향: 거의 늘 쌍발 총구를 골라 숙련까지 다 찍으면 덤 총알이 원래 총알만큼 셌음)
    // 1.1.2~ 평타 고점 하향: +25% → +15%, 쌍발 총구 +15% → +10% (숙련 세 단계가 진화 능력 위에 곱으로 얹혀 고점을 키웠음)
    static float AugStep(int id) => id == DualId ? 0.05f : 0.15f;     // 쌍발은 총알 판정 두께 0.022 → 0.3 (1.1.2 가시성)으로 무리에 더 맞으므로 +10% → +5%
    float AugPower(int id) => 1f + AugStep(id) * Trait(id);

    // ---------------- 이름 · 설명 (한국어 원문, 쓸 때 번역)
    public static string GunAugName(int id, bool maxed) => id switch
    {
        FlameId => maxed ? "지옥불 총구" : "화염 총구",
        SniperId => maxed ? "레일 탄두" : "영혼 탄두",
        DualId => maxed ? "심판의 삼연발" : "쌍발 총구",
        GrenadeId => "점착탄",
        ShotgunId => "산탄 총구",
        SeekerId => "영혼 사냥",
        ChainId => "번개 그물",
        ScytheId => "사신의 탄환",
        _ => "",
    };

    public static string GunAugDesc(int id, bool maxed) => id switch
    {
        // 레벨업 카드(소각탄 · 폭발 탄두 · 유도 탄두 · 전기탄)와 영혼 트리(철갑탄 · 고속탄 · 급소 사격)와 겹치지 않게
        FlameId => maxed ? "불길이 더 길고 넓어지며 (사거리 6.5칸, 피해 65%) 맞은 적이 불탑니다." : "쏠 때마다 총구에서 짧은 불길이 뿜어져 앞쪽 적을 태웁니다. (사거리 5칸, 피해 45%)",
        SniperId => maxed ? "충전이 더 빨리 되고, 충전탄 피해 450%." : "사격을 잠깐 멈췄다 쏘면 첫 발이 충전탄이 되어 모든 적을 꿰뚫고 피해 300%.",
        DualId => maxed ? "쏠 때마다 총알 두 발이 더 나갑니다. (피해 40%)" : "쏠 때마다 총알 한 발이 더 나갑니다. (피해 40%)",
        GrenadeId => "총알에 맞은 적에게 폭탄이 붙어 1.5초 뒤 터집니다. (범위 2.5칸, 피해 110%)",
        ShotgunId => "쏠 때마다 짧게 날아가는 산탄 네 발이 함께 퍼집니다. (발당 피해 55%)",
        SeekerId => "적을 처치하면 영혼탄 세 발이 튀어나와 가까운 적을 쫓아갑니다. (피해 60%)",
        ChainId => "총알이 맞힌 적과 바로 전에 맞힌 적 사이에 2초 동안 번개 줄이 이어져, 줄에 닿는 적이 계속 감전됩니다. (0.3초마다 공격력 30%, 줄 최대 4개)",
        ScytheId => "적을 처치하면 35% 확률로 영혼 낫이 휘돌아 주변을 벱니다. (공격력 180%)",
        _ => "",
    };

    // 필살기 한 줄 설명 (진화 카드)
    public static string VolleyDesc(int id) => id switch
    {
        ShotgunId => "마우스 쪽으로 거대한 부채꼴 폭발 3연발",
        SniperId => "화면을 가로지르는 굵은 광선 (광선 위의 적이 많을수록 강함)",
        DualId => "1.6초 동안 주위 18칸까지 사방으로 총알을 난사",
        FlameId => "마우스 위치에 적을 빨아들이며 태우는 불기둥",
        SeekerId => "영혼 구슬 다섯이 조준한 적에게 번갈아 달려듦",
        ChainId => "조준한 적들에게 하늘에서 번개가 연달아 떨어짐",
        ScytheId => "무적 상태로 조준한 적들 사이를 순간이동하며 벰",
        GrenadeId => "마우스 쪽으로 줄지어 운석이 떨어짐",
        _ => "",
    };

    // 영혼 트리 숙련 가지의 특성 칸 (예전 무기 특성 대신 능력을 강화)
    static string AugTraitName(int id) => Loc.T(GunAugName(id, false)) + " " + Loc.T("강화");
    static string AugTraitStep(int id) => Loc.T(id == DualId ? "능력 효과 +5%" : "능력 효과 +15%");

    // ---------------- 총알에 붙는 능력 (ApplyGunCards 에서, 권총 총알마다). main = 쏠 때 나가는 첫 발 (쌍발 · 산탄 덤 총알이 아님)
    float lastMainShot = -99f;
    // 번개 그물: 바로 전에 맞힌 적 (그 적과 지금 맞힌 적 사이에 번개 줄)
    Transform netLast;
    int netLines;
    float netSoundAt;
    const int NetMaxLines = 4;
    const float NetRange = 9f, NetLife = 2f, NetTick = 0.3f, NetDamage = 0.3f;
    readonly HashSet<int> stuckBombs = new HashSet<int>();

    void ApplyEvoAugments(Bullet b, bool light, bool main)
    {
        if (gunEvo1 < 0 || light || b == null) return;
        // 영혼 탄두: 잠깐 쉬었다 쏘면 충전탄 (모두 관통 · 큰 피해)
        if (HasAug(SniperId) && main)
        {
            bool rail = AugMaxed(SniperId);
            // 평소 연사 간격의 두 배(극대화 1.4배) 넘게 쉬었다 쏜 첫 발 (공격 속도가 느려도 매 발이 충전되지 않게)
            float interval = player.ShootSpeed / Mathf.Max(0.1f, player.fireRateMultiplier);
            if (Time.time - lastMainShot >= Mathf.Max(rail ? 0.4f : 0.6f, interval * (rail ? 1.4f : 2f)))
            {
                b.damage *= (rail ? 4.5f : 3f) * AugPower(SniperId);
                b.pene = 9999;
                b.hitOnce = new HashSet<int>();
                b.speed *= 1.6f;
                b.transform.localScale *= 1.5f;
                if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(0.5f, 0.95f, 1f);
                if (fx != null) fx.Play("railgun", 0.35f, 1.6f);
            }
        }
        if (main) lastMainShot = Time.time;

        bool sticky = HasAug(GrenadeId), thunder = HasAug(ChainId);
        if (!sticky && !thunder) return;
        b.onHitEnemy += (bullet, col) =>
        {
            if (col == null) return;
            if (sticky && stuckBombs.Add(col.GetInstanceID())) StartCoroutine(StickyBomb(col.transform, col.GetInstanceID()));
            if (thunder) NetHit(col.transform);
        };
    }

    // 번개 그물 (1.0.1, 예전 「낙뢰탄」: 여섯 번마다 낙뢰 — 잘 보이지 않고 전기탄과 겹쳤음)
    // 맞힌 적과 바로 전에 맞힌 적 사이에 번개 줄 → 무리 양 끝을 번갈아 쏘면 그 사이가 그물로 덮임
    void NetHit(Transform hit)
    {
        Transform prev = netLast;
        netLast = hit;
        if (prev == null || prev == hit || netLines >= NetMaxLines) return;
        if ((prev.position - hit.position).sqrMagnitude > NetRange * NetRange) return;
        StartCoroutine(NetLine(prev, hit));
    }

    System.Collections.IEnumerator NetLine(Transform a, Transform b)
    {
        netLines++;
        Color c = new Color(0.55f, 0.9f, 1f);
        LineRenderer core = Hostile.NewLine("NetBolt", Color.white, 0.09f, 31);
        LineRenderer glow = Hostile.NewLine("NetGlow", new Color(c.r, c.g, c.b, 0.55f), 0.32f, 30);
        core.positionCount = glow.positionCount = 9;
        if (fx != null && Time.time >= netSoundAt) { netSoundAt = Time.time + 0.25f; fx.Play("thunder", 0.22f, Random.Range(1.4f, 1.7f)); }
        Vector3 pa = a.position, pb = b.position;
        float tick = 0f;
        for (float t = 0f; t < NetLife; t += Time.deltaTime)
        {
            if (a != null) pa = a.position;            // 적이 쓰러지면 마지막 자리에 남음
            if (b != null) pb = b.position;
            Vector3 d = pb - pa;
            Vector3 side = new Vector3(-d.y, d.x, 0f).normalized;
            float fade = t > NetLife - 0.3f ? (NetLife - t) / 0.3f : 1f;
            for (int i = 0; i < 9; i++)
            {
                float k = i / 8f;
                float jag = i == 0 || i == 8 ? 0f : Random.Range(-0.35f, 0.35f);
                Vector3 p = pa + d * k + side * jag;
                core.SetPosition(i, p);
                glow.SetPosition(i, p);
            }
            core.startColor = core.endColor = new Color(1f, 1f, 1f, fade);
            glow.startColor = glow.endColor = new Color(c.r, c.g, c.b, 0.55f * fade);
            if ((tick -= Time.deltaTime) <= 0f)
            {
                tick = NetTick;
                Vector3 mid = (pa + pb) * 0.5f;
                foreach (Collider2D col in Physics2D.OverlapCircleAll(mid, d.magnitude * 0.5f + 1f))
                {
                    if (col == null || (!col.CompareTag("enermy") && !col.CompareTag("boss"))) continue;
                    if (Hostile.DistanceToSegment(col.transform.position, pa, pb) > 0.7f) continue;
                    Specials.Damage(col.gameObject, Damage * NetDamage * AugPower(ChainId), side, 0f);
                    Fx.Spawn("fx_spark", col.transform.position, 1f, c, 24f);
                }
            }
            yield return null;
        }
        Destroy(core.gameObject);
        Destroy(glow.gameObject);
        netLines--;
    }

    // 점착탄: 맞은 적에 붙은 폭탄이 1.5초 뒤 터짐 (적마다 하나씩)
    System.Collections.IEnumerator StickyBomb(Transform target, int key)
    {
        Fx.Spawn("fx_spark", target.position, 1f, new Color(1f, 0.55f, 0.2f), 20f);
        Vector3 at = target.position;
        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            if (target != null) at = target.position;
            yield return null;
        }
        stuckBombs.Remove(key);
        Explode(at, 2.5f + 0.3f * Trait(GrenadeId), Damage * 1.1f * AugPower(GrenadeId), 1.2f, new Color(1f, 0.55f, 0.2f, 0.85f));
    }
    // 치명타 (금색 · 크게) — 영혼 탄두 · 영혼 트리 치명타가 같이 씀
    void MakeCrit(Bullet b)
    {
        if (b.isCrit) return;
        b.isCrit = true;
        b.damage *= TreeCritDamage;
        b.transform.localScale *= 1.25f;
        if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(1f, 0.85f, 0.3f);
    }

    // ---------------- 쏠 때 더 나가는 총알 (PlayerController.Shoot)
    const float DualShotRate = 0.4f;          // 1.1.2~ 평타 고점 하향 (판정 확대 반영): 덤 총알 50% → 40%
    public void EvoExtraShots(Vector3 start, Vector2 dir, float dmg)
    {
        if (!Gunner || gunEvo1 < 0) return;
        // 화염 총구: 총구 앞 짧은 불길 (부채꼴)
        if (HasAug(FlameId))
        {
            bool hell = AugMaxed(FlameId);
            float range = hell ? 6.5f : 5f, half = hell ? 30f : 24f;     // 1.0.1: 쌍발 총구만 고르던 것 → 다른 진화 강화
            float hit = Damage * (hell ? 0.65f : 0.45f) * AugPower(FlameId);
            foreach (Collider2D c in Specials.Overlap(start, range, gunHits))
            {
                if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                Vector2 to = c.transform.position - start;
                if (Vector2.Angle(dir, to) > half) continue;
                Specials.Damage(c.gameObject, hit, to.normalized, 0.3f);
                if (hell) Burn.Apply(c.gameObject, Damage * 0.3f, 2f);
            }
            for (int i = 1; i <= 3; i++)
                Fx.Spawn("fx_explosion", start + (Vector3)(dir * range * i / 3.5f), 0.8f + i * (hell ? 0.6f : 0.4f), new Color(1f, 0.55f, 0.15f, 0.8f), 30f);
        }
        if (HasAug(DualId))
        {
            // 덤 총알 피해: 60% → 50% (1.1.2 거너 하향) → 40% (평타 고점 하향)
            int extra = AugMaxed(DualId) ? 2 : 1;
            for (int i = 0; i < extra; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, i == 0 ? 4f : -4f) * dir;
                Bullet b = player.CreateBullet(start + (Vector3)(new Vector2(-d.y, d.x) * (i == 0 ? 0.25f : -0.25f)), d, dmg * DualShotRate * AugPower(DualId), player.pene, 0, false, 0.5f);
                ApplyGunCards(b, false, false);
            }
        }
        if (HasAug(ShotgunId))
        {
            float[] angles = { -24f, -12f, 12f, 24f };
            foreach (float a in angles)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, a + Random.Range(-3f, 3f)) * dir;
                Bullet b = player.CreateBullet(start, d, dmg * 0.55f * AugPower(ShotgunId), 1, 0, false, 0.4f);
                if (b == null) continue;
                b.lifetime = 0.3f;
                b.transform.localScale *= 0.8f;
                if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(1f, 0.7f, 0.35f);
                ApplyGunCards(b, false, false);
            }
        }
    }

    // ---------------- 무한 모드: 진화를 다 마친 뒤 보스를 잡을 때마다 쌓이는 전리품 (돌아가며)
    public string BossTrophy(int n)
    {
        if (player == null) return "";
        string got;
        switch (n % 3)
        {
            case 1: AddAttack(0.08f); got = Loc.T("모든 공격 피해 +8%"); break;       // 1.1.2~ 평타 고점 하향: +12% → +8%
            case 2:
                GrowHp(player, 1.15f);
                player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + player.PlayerMaxHealth * 0.3f);
                got = Loc.T("최대 체력 +15% · 체력 30% 회복");
                break;
            default: AddRate(0.95f); got = Loc.T("모든 무기 발사 간격 -5%"); break;     // -8% → -5%
        }
        if (fx != null) { fx.Play("levelup", 0.7f, 1f); fx.FloatText(player.transform.position, got, new Color(1f, 0.85f, 0.4f), 6f, 0f); }
        Flash(player.transform.position, 6f, new Color(1f, 0.85f, 0.4f, 0.8f), 0.5f);
        return got;
    }

    // ---------------- 처치할 때: 사신의 탄환
    void EvoOnKill(Vector3 pos)
    {
        if (!Gunner || player == null) return;
        // 영혼 사냥: 처치한 자리에서 가까운 적을 쫓는 영혼탄 두 발
        if (HasAug(SeekerId))
            for (int i = 0; i < 3; i++)
            {
                Bullet b = player.CreateBullet(pos, Random.insideUnitCircle.normalized, Damage * 0.6f * AugPower(SeekerId), 1, 0, false, 0.3f);
                if (b == null) continue;
                b.gameObject.AddComponent<Homing>().turnSpeed = 420f;
                b.transform.localScale *= 0.8f;
                if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(0.75f, 0.5f, 1f);
            }
        if (!HasAug(ScytheId) || Random.value >= 0.35f) return;
        float r = 3f;
        CharacterKit.DamageCircle(pos, r, Damage * 1.8f * AugPower(ScytheId), 1.5f);
        Fx.Spawn("fx_slash", pos, r * 2.2f, new Color(0.8f, 0.55f, 1f), 30f, Random.Range(0f, 360f), 16);
        if (fx != null) fx.Play("slash", 0.5f, 1.3f);
    }
}
