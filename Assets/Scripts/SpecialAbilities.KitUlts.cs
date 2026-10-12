using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 캐릭터 전용 특수 무기의 우클릭 궁극기 (거너 무기처럼 무기마다 하나씩, 스킬 게이지를 씀)
// 그리고 전용 무기의 발사 소리 (총소리 · 총구 섬광 대신 무기에 맞는 소리)
public partial class SpecialAbilities
{
    static readonly (int id, string name, string desc)[] KitUlts =
    {
        (KitHammer, "대지 진동", "망치로 땅을 연달아 내리쳐 앞으로 나아가는 충격파 여섯 번을 일으킵니다. (공격력 240%씩, 기절)"),
        (KitWhip, "뱀의 춤", "채찍검을 사방 열두 방향으로 휘몰아칩니다. (공격력 160%씩)"),
        (KitLance, "기사 돌격", "무적 상태로 멀리 돌격해 지나간 적을 모두 날려 버립니다. (공격력 560%)"),
        (KitBlowgun, "독안개", "마우스 위치에 5초 동안 독안개를 퍼뜨려 안의 적에게 중독을 계속 쌓습니다."),
        (KitCards, "잭팟", "카드 52장을 소용돌이치듯 사방으로 뿌립니다. 조커도 잔뜩 섞여 있습니다."),
        (KitWire, "거미줄 감옥", "주변을 와이어로 둘러싼 뒤 조여 들어가며 닿는 적을 벱니다."),
        (KitNetBow, "대형 그물", "마우스 위치에 거대한 그물을 던져 3초 동안 모두 묶고 피해를 줍니다. (공격력 240%)"),
        (KitJavelin, "창의 벽", "투창 아홉 자루를 한 줄로 늘어세워 한꺼번에 던집니다. (공격력 320%씩)"),
        (KitBurstBow, "천 개의 화살", "3초 동안 가까운 적들을 자동으로 겨눠 화살을 퍼붓습니다."),
        (KitQuicksilver, "수은 폭풍", "수은 구슬 열여섯 개를 사방으로 튕겨 보냅니다."),
        (KitMagnet, "블랙홀 플라스크", "마우스 위치에 3초 동안 모든 것을 빨아들이는 소용돌이를 만든 뒤 크게 폭발합니다. (공격력 640%)"),
        (KitFirework, "불꽃 축제", "3초 동안 적들 머리 위로 폭죽 스무 발을 터뜨립니다."),
    };

    public static bool IsKitWeapon(int id) => IsKit(id) && KitDefs[id - KitFirstId].kind == SpecialKind.Weapon;
    public static string KitName(int id) => IsKit(id) ? KitDefs[id - KitFirstId].name : "";
    public static string KitDesc(int id) => IsKit(id) ? KitDefs[id - KitFirstId].desc : "";
    public static SpecialKind KitKind(int id) => KitDefs[id - KitFirstId].kind;
    public static int KitCount => KitDefs.Length;

    public static string KitUltName(int id)
    {
        foreach (var u in KitUlts) if (u.id == id) return u.name;
        return "";
    }

    public static string KitUltDesc(int id)
    {
        foreach (var u in KitUlts) if (u.id == id) return u.desc;
        return "";
    }

    // 전용 무기를 들고 있거나 그 무기로 진화했으면 원래 필살기를 쓸 때 그 무기의 궁극기가 덧붙음 (CharacterKit.KitUltAdd)
    public bool KitWeaponUltActive => UltActive && IsKitWeapon(UltId);

    // 우클릭을 누르고 있는 동안 보여 줄 궁극기 범위 (떼면 발동). CharacterKit 이 그림
    public enum UltAimShape { Point, Line, Around }

    // Point: at 에 반지름 size 원 · Line: 플레이어에서 마우스 쪽으로 길이 size · Around: 플레이어 둘레 반지름 size
    public UltAimShape KitUltAim(out float size, out Vector3 at)
    {
        int id = UltId;
        bool evo = IsEvolved(id);
        at = KitClamp(MouseWorld());
        switch (id)
        {
            case KitBlowgun: size = evo ? 6f : 5f; return UltAimShape.Point;
            case KitNetBow: size = evo ? 8.5f : 7f; return UltAimShape.Point;
            case KitMagnet: size = evo ? 11f : 9f; return UltAimShape.Point;
            case KitHammer: size = 6 * 2.2f; return UltAimShape.Line;
            case KitLance: size = evo ? 20f : 16f; return UltAimShape.Line;
            case KitJavelin: size = 24f; return UltAimShape.Line;
            case KitWhip: size = evo ? 13f : 11f; return UltAimShape.Around;
            case KitWire: size = evo ? 11f : 9f; return UltAimShape.Around;
            case KitBurstBow: size = 16f; return UltAimShape.Around;
            case KitFirework: size = 12f; return UltAimShape.Around;
            default: size = 8f; return UltAimShape.Around;       // 카드 · 수은 구슬: 사방으로
        }
    }

    // mul: 궁극기 세기 배율 (2.1.8~ 원래 필살기에 덧붙는 효과라 절반) · dash: 도적은 출혈 돌진으로 파고든 뒤 (덧붙일 때는 이미 돌진했으므로 끔)
    // aimAt: 조준한 자리 (1.1.0 — 도적은 돌진 뒤 0.25초에 터지는데, 그때 마우스를 다시 읽으면 카메라가 돌진을 따라가 조준 범위와 다른 곳에 퍼졌음)
    public void KitWeaponUlt(float mul = 1f, bool dash = true, Vector3? aimAt = null)
    {
        int id = UltId;
        fx.Play("levelup", 0.5f, 1.3f);
        Hostile.Shake(0.15f);
        // 캐릭터 우클릭 강화(영혼 트리 · 상점)도 진화한 궁극기에 그대로
        float D = KitD * UltPower(id) * WeaponDamageMul(id) * (1f + 0.1f * UltTrait(id)) * (Kit != null ? Kit.ultMul : 1f) * mul;
        // 도적: 출혈 돌진으로 파고든 뒤 궁극기 (그림자 숙련이 계속 쓸모 있게)
        if (dash && CharacterData.Selected == CharacterId.Rogue && Kit != null) StartCoroutine(RogueDashUlt(id, D));
        else StartCoroutine(KitUltRoutine(id, D, aimAt));
    }

    IEnumerator RogueDashUlt(int id, float D)
    {
        Vector2 dir = ((Vector2)(MouseWorld() - player.transform.position)).normalized;
        yield return Kit.StartCoroutine(Kit.BleedDash(dir));
        yield return StartCoroutine(KitUltRoutine(id, D));
    }

    // 전용 무기 발사 소리 (케이스 안에서 이미 소리를 내는 무기는 여기서 안 냄)
    void KitShotSound(int id)
    {
        switch (id)
        {
            case KitBlowgun: fx.Play("pop", 0.35f, 2.2f); fx.Play("whoosh", 0.2f, 2.2f); break;
            case KitCards: fx.Play("whoosh", 0.45f, 1.9f); break;
            case KitNetBow: fx.Play("bowtwang", 0.6f, 0.9f); fx.Play("arrowfly", 0.3f, 0.8f); break;
            case KitJavelin: fx.Play("thump", 0.4f, 1.4f); break;
            case KitQuicksilver: fx.Play("glassclink", 0.5f, 1.6f); fx.Play("pop", 0.3f, 0.9f); break;
            case KitMagnet: case KitFirework: fx.Play("glassclink", 0.6f, Random.Range(0.9f, 1.15f)); fx.Play("whoosh", 0.25f, 1.4f); break;
        }
    }

    IEnumerator KitUltRoutine(int id, float D, Vector3? aimAt = null)
    {
        Vector3 me = player.transform.position;
        Vector3 mouse = aimAt ?? MouseWorld();
        Vector2 dir = ((Vector2)(mouse - me)).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        bool evo = IsEvolved(id);

        switch (id)
        {
            // ---------------- 검사
            case KitHammer:
                // 몸동작 (1.0.5): 크게 뛰어올라 첫 내려찍기, 이어서 작게 들썩이며 땅을 두드림
                PlayerMotion.Of(player).Leap(0.45f, 2.6f, dir);
                yield return new WaitForSeconds(0.45f);
                for (int i = 1; i <= 6; i++)
                {
                    Vector3 p = me + (Vector3)(dir * (i * 2.2f));
                    if (Hostile.IsWall(p)) break;
                    if (i > 1) PlayerMotion.Of(player).Pound(dir);
                    Smash(p, evo ? 3f : 2.5f, D * 3f, 0.8f);
                    yield return new WaitForSeconds(0.12f);
                }
                break;
            case KitWhip:
                PlayerMotion.Of(player).Spin(1f, 3f);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 d = Quaternion.Euler(0f, 0f, i * 30f) * dir;
                    Whip(player.MuzzlePosition, d, evo ? 13f : 11f, D * 2f, 1.5f);
                    yield return new WaitForSeconds(0.05f);
                }
                break;
            case KitLance:
                PlayerMotion.Of(player).Lunge(0.5f, dir);
                yield return StartCoroutine(LanceCharge(dir, evo ? 20f : 16f, 0.35f, D * 7f, 6f, true));
                Hostile.Shake(0.3f);
                fx.Play("bigboom", 0.6f, 1.1f);
                break;

            // ---------------- 도적
            case KitBlowgun:
                PlayerMotion.Of(player).Recoil(dir, 1.2f);
                {
                    Vector3 at = KitClamp(mouse);
                    float r = evo ? 6f : 5f;
                    Fx.Spawn("fx_cloud", at, r * 2.2f, new Color(0.5f, 1f, 0.4f, 0.6f), 6f, 0f, 3, true, 5f);
                    fx.Play("hiss", 0.9f, 0.7f);
                    for (float t = 0f; t < 5f; t += 0.5f)
                    {
                        foreach (Collider2D c in Physics2D.OverlapCircleAll(at, r))
                        {
                            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                            Poison.Apply(c.gameObject, D * 0.3f, 12);
                        }
                        Fx.Spawn("fx_cloud", at + (Vector3)(Random.insideUnitCircle * r), 2.5f, new Color(0.5f, 1f, 0.4f, 0.5f), 10f);
                        yield return new WaitForSeconds(0.5f);
                    }
                    break;
                }
            case KitCards:
                PlayerMotion.Of(player).Spin(1.2f, 4f);
                for (int i = 0; i < 52; i++)
                {
                    Vector2 d = Quaternion.Euler(0f, 0f, i * 27.7f) * dir;
                    ThrowCard(player.transform.position, d, D, evo ? 0.35f : 0.25f);
                    if (i % 6 == 0) fx.Play("whoosh", 0.4f, 2f);
                    yield return new WaitForSeconds(0.025f);
                }
                break;
            case KitWire:
                PlayerMotion.Of(player).Cast(1.2f);
                {
                    Vector3 c0 = player.transform.position;
                    LineRenderer web = fx.NewLine("WebPrison", false, 18);
                    web.loop = true;
                    web.enabled = true;
                    web.startWidth = web.endWidth = 0.1f;
                    web.startColor = web.endColor = new Color(0.9f, 0.85f, 1f, 0.9f);
                    float from = evo ? 11f : 9f, tick = 0f;
                    fx.Play("clank", 0.8f, 1.4f);
                    for (float t = 0f; t < 2f; t += Time.deltaTime)
                    {
                        float r = Mathf.Lerp(from, 1f, t / 2f);
                        Hostile.SetArc(web, c0, r, 0f, 360f);
                        tick -= Time.deltaTime;
                        if (tick <= 0f)
                        {
                            tick = 0.12f;
                            foreach (Collider2D c in Physics2D.OverlapCircleAll(c0, r + 1f))
                            {
                                if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                                if (Mathf.Abs(Vector2.Distance(c.transform.position, c0) - r) > 1f) continue;
                                Specials.Damage(c.gameObject, D * 0.8f, (c0 - c.transform.position).normalized, 0.8f);
                                Fx.Spawn("fx_spark", c.transform.position, 0.9f, new Color(1f, 0.5f, 0.6f), 24f);
                            }
                            fx.Play("crackle", 0.3f, 1.8f);
                        }
                        yield return null;
                    }
                    Destroy(web.gameObject);
                    break;
                }

            // ---------------- 궁수
            case KitNetBow:
                PlayerMotion.Of(player).Throw(0.4f, dir);
                {
                    Vector3 at = KitClamp(mouse);
                    float r = evo ? 8.5f : 7f;
                    NetAt(at, r, 3f, D * 3f);
                    Fx.Spawn("fx_net", at, r * 2.4f, new Color(1f, 1f, 1f, 0.9f), 1f, 0f, 11, true, 3f);
                    Hostile.Shake(0.2f);
                    break;
                }
            case KitJavelin:
                PlayerMotion.Of(player).Throw(0.45f, dir);
                {
                    Vector2 side = new Vector2(-dir.y, dir.x);
                    for (int i = -4; i <= 4; i++)
                    {
                        Bullet b = KitProjectile(player.transform.position + (Vector3)(side * i * 1.1f), dir, D * 4f, 9999, 36f, "fx_arrow", 1.4f, new Color(1f, 0.85f, 0.55f), false);
                        if (b != null) b.hitOnce = new HashSet<int>();
                    }
                    fx.Play("whoosh", 1f, 0.7f);
                    fx.Play("thump", 0.8f, 1.2f);
                    break;
                }
            case KitBurstBow:
                PlayerMotion.Of(player).Recoil(dir, 1.2f);
                for (float t = 0f; t < 3f; t += 1f / 15f)
                {
                    List<Transform> near = NearestEnemies(player.transform.position, 16f, 3);
                    Vector3 target = near.Count > 0 ? near[Random.Range(0, near.Count)].position : MouseWorld();
                    Vector2 d = ((Vector2)(target - player.MuzzlePosition)).normalized;
                    KitProjectile(player.MuzzlePosition, d, D * (evo ? 1f : 0.8f), player.pene, 70f, "fx_arrow", 0.4f, new Color(1f, 1f, 0.8f), false);
                    fx.Play("bowtwang", 0.25f, 1.7f + Random.Range(-0.1f, 0.1f));
                    yield return new WaitForSeconds(1f / 15f);
                }
                break;

            // ---------------- 연금술사
            case KitQuicksilver:
                PlayerMotion.Of(player).Spin(0.8f, 2f);
                for (int i = 0; i < 16; i++)
                {
                    Vector2 d = Quaternion.Euler(0f, 0f, i * 22.5f) * dir;
                    Bullet b = KitProjectile(player.transform.position, d, D, 9999, 24f, "fx_orb", 0.7f, new Color(0.85f, 0.9f, 1f), false);
                    if (b != null) Quicksilver(b, evo ? 6 : 4, D);
                }
                fx.Play("glassclink", 0.8f, 0.8f);
                break;
            case KitMagnet:
                PlayerMotion.Of(player).Cast(1f);
                {
                    Vector3 at = KitClamp(mouse);
                    yield return StartCoroutine(MagnetPull(at, evo ? 11f : 9f, 3f, D * 8f, 1, 5f));
                    Hostile.Shake(0.4f);
                    fx.Play("bigboom", 0.9f, 0.7f);
                    break;
                }
            case KitFirework:
                PlayerMotion.Of(player).Cast(1.5f);
                for (int i = 0; i < 20; i++)
                {
                    // 1.1.0: 조준 원(반지름 12) 밖 최대 24칸까지 터지던 것을 원 안의 적으로
                    List<Transform> near = NearestEnemies(player.transform.position, 12f, 6);
                    Vector3 at = near.Count > 0 ? near[Random.Range(0, near.Count)].position : KitClamp(player.transform.position + (Vector3)(Random.insideUnitCircle * 8f));
                    FireworkBurst(at, evo ? 8 : 6, D * 1.2f);
                    yield return new WaitForSeconds(0.15f);
                }
                break;
        }
    }
}
