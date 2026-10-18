using System.Collections.Generic;
using UnityEngine;

// 거너 「포탑 설치」 갈래의 포탑 (GunnerPaths.SpawnTurret 이 new GameObject + 이 컴포넌트로 만듦 · 자기 복제 없음 · 최대 4)
//   0.4초마다 가까운 적에게 한 발 (공격력 20%) · 12초 뒤 사라짐 (극의: 사라지지 않음)
//   공명 II: 0.8초마다 적 투사체 하나를 쏴서 지움 · 사라질 때 폭발 / 극의: 5초마다 탄피 지뢰
//   쓰러진 뒤에는 아무것도 하지 않음 · 다른 판이 시작되면(효과 인스턴스가 바뀌면) 스스로 사라짐
//   그림: Resources/Icons/Paths/gunner_turret · gunner_turret_fire (tools/pixelart/make_paths_gunner.py) — 없으면 둥근 빛으로
public class GunTurret : MonoBehaviour
{
    GunnerPaths owner;
    SpriteRenderer sr;
    Sprite idle, fire;
    float bornAt, nextShot, nextZap, nextMine, fireUntil;
    bool expired;

    const float Size = 1.7f;

    public void Init(GunnerPaths o)
    {
        owner = o;
        bornAt = Time.time;
        nextShot = Time.time + 0.2f;
        nextZap = Time.time + GunnerPaths.TurretZapEvery;
        nextMine = Time.time + 1f;
        idle = Paths.Icon("gunner_turret");
        fire = Paths.Icon("gunner_turret_fire");
        sr = gameObject.AddComponent<SpriteRenderer>();
        Sprite s = idle != null ? idle : SpecialAbilities.GlowSprite;
        sr.sprite = s;
        sr.color = idle != null ? Color.white : GunnerPaths.TurretColor;
        sr.sortingLayerName = "Character";
        sr.sortingOrder = -2;
        float h = s != null ? s.bounds.size.y : 1f;
        transform.localScale = Vector3.one * (Size / Mathf.Max(0.01f, h));
        if (fire == null) fire = idle;
    }

    Vector3 Muzzle => transform.position + new Vector3(sr != null && sr.flipX ? -0.6f : 0.6f, 0.3f, 0f);

    void Update()
    {
        if (Time.timeScale == 0f) return;
        // 이번 판 효과가 아니면 (판이 바뀜 · 갈래 규칙이 꺼짐) 스스로 사라짐
        if (owner == null || !ReferenceEquals(Paths.Hero, owner) || !Paths.Active || !Paths.Has(GunnerPaths.Turret)) { Destroy(gameObject); return; }
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;         // 쓰러진 뒤 동작 금지
        float now = Time.time;
        if (!Paths.Mastered(GunnerPaths.Turret) && now - bornAt >= GunnerPaths.TurretLife)
        {
            Expire(Paths.Res(GunnerPaths.Turret) >= 2);
            return;
        }
        if (sr != null) sr.sprite = now < fireUntil ? fire : idle != null ? idle : sr.sprite;
        if (now >= nextShot)
        {
            nextShot = now + GunnerPaths.TurretEvery;
            Transform t = Specials.NearestEnemy(transform.position, GunnerPaths.TurretRange);
            if (t != null) ShootAt(t.position, Paths.Atk * GunnerPaths.TurretRate);
        }
        if (Paths.Res(GunnerPaths.Turret) >= 2 && now >= nextZap)
        {
            nextZap = now + GunnerPaths.TurretZapEvery;
            Zap();
        }
        if (Paths.Mastered(GunnerPaths.Turret) && now >= nextMine)
        {
            nextMine = now + GunnerPaths.TurretMineEvery;
            SignatureSkills ss = SignatureSkills.Instance;
            if (ss != null)
            {
                Vector3 at = Hostile.ClampArena(transform.position + (Vector3)(Random.insideUnitCircle * 1.8f));
                if (!Hostile.IsWall(at)) ss.PathMine(at);
            }
        }
    }

    void ShootAt(Vector3 target, float dmg)
    {
        if (owner == null) return;
        if (sr != null) sr.flipX = target.x < transform.position.x;
        Vector3 m = Muzzle;
        Vector2 dir = (Vector2)(target - m);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        owner.PathShot(m, dir.normalized, dmg * Paths.Power(GunnerPaths.Turret), 1, 26f, GunnerPaths.TurretRange + 2f, null, 0f, GunnerPaths.TurretColor, "포탑");
        fireUntil = Time.time + 0.08f;
        Fx.Spawn("fx_spark", m, 0.8f, GunnerPaths.TurretColor, 30f);
        if (owner.TurretSound()) Hostile.Play("pew", 0.22f, 1.5f);
    }

    // 공명 II: 가장 가까운 적 투사체 하나를 쏴서 지움
    void Zap()
    {
        Vector3 me = transform.position;
        HostileProjectile best = null;
        float bestD = GunnerPaths.TurretZapRange * GunnerPaths.TurretZapRange;
        List<HostileProjectile> live = HostileProjectile.Live;
        for (int i = live.Count - 1; i >= 0; i--)
        {
            HostileProjectile h = live[i];
            if (h == null || !h.isActiveAndEnabled) continue;
            float d = ((Vector2)(h.transform.position - me)).sqrMagnitude;
            if (d < bestD) { bestD = d; best = h; }
        }
        if (best == null) return;
        Vector3 at = best.transform.position;
        Fx.Bolt(Muzzle, at, 0.35f, GunnerPaths.TurretColor, 0.12f);
        Fx.Spawn("fx_spark", at, 1.2f, Color.white, 26f);
        fireUntil = Time.time + 0.08f;
        Destroy(best.gameObject);
    }

    // 총잡이의 춤 (완성): 가까운 적 여섯에게 한 발씩
    public void Volley(float dmg)
    {
        if (expired) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        foreach (Transform t in Paths.Nearest(transform.position, 14f, 6))
            if (t != null) ShootAt(t.position, dmg);
    }

    // 사라짐 (공명 II 면 폭발) — 시간이 다 됨 · 개수가 넘쳐 가장 오래된 것
    public void Expire(bool explode)
    {
        if (expired) return;
        expired = true;
        PlayerController p = Hostile.Player;
        if (explode && p != null && !p.IsDying)
        {
            Vector3 at = transform.position;
            Paths.Circle(at, 3f, Paths.Atk * GunnerPaths.TurretBoom * Paths.Power(GunnerPaths.Turret), 2f, "포탑");
            Fx.Spawn("fx_explosion", at, 6f, new Color(0.7f, 1f, 0.6f), 20f);
            ShockRing.Spawn(at, 0.3f, 3.4f, 0.3f, new Color(0.55f, 1f, 0.55f, 0.85f), 0.24f);
            Hostile.Play("boom", 0.45f, 1.3f);
        }
        else Fx.Spawn("fx_sparkle", transform.position, 1.4f, GunnerPaths.TurretColor, 22f);
        Destroy(gameObject);
    }
}

// 저격탄 · 화상탄의 hitOnce 비교기: Bullet 이 맞히기 직전 hitOnce.Add(대상 번호)를 부를 때 그 번호를 GunnerPaths 에 알려 줌
//   (Bullet 을 고치지 않고 「우리 탄의 이 한 대」를 Outgoing · Hit 훅에서 알아보는 길 · 처음 맞히는 대상만 · 같은 대상 두 번은 원래처럼 막힘)
sealed class GunHitTap : IEqualityComparer<int>
{
    readonly GunnerPaths owner;
    public readonly bool snipe, burn;
    public HashSet<int> set;
    bool busy;

    public GunHitTap(GunnerPaths owner, bool snipe, bool burn)
    {
        this.owner = owner;
        this.snipe = snipe;
        this.burn = burn;
    }

    public bool Equals(int a, int b) => a == b;

    public int GetHashCode(int v)
    {
        if (!busy && set != null && owner != null)
        {
            busy = true;
            try { if (!set.Contains(v)) owner.Tapped(this, v); }
            finally { busy = false; }
        }
        return v;
    }
}
