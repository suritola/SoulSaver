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
        (KitSurveyChain, "삼각 측량", "내 둘레에 커다란 삼각형 세 개를 차례로 그려 안쪽의 적을 벱니다. (공격력 220%씩)"),
        (KitInkpot, "먹물 해일", "마우스 쪽으로 거대한 먹물 파도를 일으켜 휩쓸린 적을 밀어내고 느리게 합니다. (공격력 300%)"),
        (KitGlobe, "자전", "지구본 네 개가 3초 동안 내 둘레를 돌며 닿는 적을 벱니다. (0.25초마다 공격력 60%)"),
        (KitCuckoo, "뻐꾸기 떼", "뻐꾸기 열두 마리가 가까운 적들을 차례로 덮칩니다. (공격력 150%씩)"),
        (KitHourglass, "모래 폭풍", "마우스 자리에 4초 동안 모래 회오리가 생겨 적을 끌어당기고 0.3초마다 벱니다. (공격력 60%)"),
        (KitGrandfather, "열두 번의 종", "내 둘레로 종소리 고리가 열두 번 퍼집니다. (공격력 80%씩)"),
        (KitPigeon, "비둘기 떼", "비둘기 스무 마리가 마우스 쪽으로 날아가며 닿는 적을 쪼습니다. (공격력 90%씩)"),
        (KitParcel, "택배 폭격", "마우스 둘레에 상자 열 개가 차례로 떨어져 터집니다. (공격력 180%씩)"),
        (KitPostbox, "우체통 행렬", "내 둘레에 우체통 다섯 개가 서서 4초 동안 가까운 적에게 편지를 던집니다."),
        (KitSoulChain, "영혼 사슬", "둘레의 적을 모두 영혼 사슬로 묶어 3초 동안 0.5초마다 벱니다. (공격력 70%)"),
        (KitIceGhost, "절대 영도", "둘레의 모든 적을 2초 동안 얼리고 피해를 줍니다. (공격력 300%)"),
        (KitShadowGhost, "그림자 군단", "그림자 넷이 3초 동안 둘레의 적에게 손길을 뻗습니다. (공격력 80%씩)"),
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
    public enum UltAimShape { Point, Line, Around, Burst }      // Burst: 사방으로 날아가는 투사체 (원 대신 방사형 화살, 1.0.2)

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
            case KitCards: case KitQuicksilver: size = 8f; return UltAimShape.Burst;
            case KitSurveyChain: size = evo ? 9f : 8f; return UltAimShape.Around;
            case KitInkpot: size = evo ? 20f : 16f; return UltAimShape.Line;
            case KitGlobe: size = evo ? 5f : 4f; return UltAimShape.Around;
            case KitCuckoo: size = 12f; return UltAimShape.Around;
            case KitHourglass: size = evo ? 7f : 6f; return UltAimShape.Point;
            case KitGrandfather: size = evo ? 8f : 7f; return UltAimShape.Around;
            case KitPigeon: size = evo ? 20f : 16f; return UltAimShape.Line;
            case KitParcel: size = evo ? 7f : 6f; return UltAimShape.Point;
            case KitPostbox: size = 10f; return UltAimShape.Around;
            case KitSoulChain: size = evo ? 10f : 8f; return UltAimShape.Around;
            case KitIceGhost: size = evo ? 11f : 9f; return UltAimShape.Around;
            case KitShadowGhost: size = 9f; return UltAimShape.Around;       // 카드 · 수은 구슬: 사방으로 날아감 (원으로 보여 범위 공격처럼 오해했음)
            default: size = 8f; return UltAimShape.Around;
        }
    }

    // mul: 궁극기 세기 배율 (2.1.8~ 원래 필살기에 덧붙는 효과라 절반) · dash: 도적은 출혈 돌진으로 파고든 뒤 (덧붙일 때는 이미 돌진했으므로 끔)
    // aimAt: 조준한 자리 (1.0.2 — 도적은 돌진 뒤 0.25초에 터지는데, 그때 마우스를 다시 읽으면 카메라가 돌진을 따라가 조준 범위와 다른 곳에 퍼졌음)
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
                // 몸동작 (1.0.1): 크게 뛰어올라 첫 내려찍기, 이어서 작게 들썩이며 땅을 두드림
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
                    Fx.Spawn("fx_poisonfog", at, r * 2.3f, Color.white, 6f, 0f, 3, true, 5f);       // 가장자리 독 연기 띠 = 범위 · 속은 비움 (예전 화면을 덮던 반투명 구름 fx_cloud)
                    fx.Play("hiss", 0.9f, 0.7f);
                    for (float t = 0f; t < 5f; t += 0.5f)
                    {
                        using (var ovl = PhysHits.Circle(at, r)) foreach (Collider2D c in ovl)
                        {
                            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                            Poison.Apply(c.gameObject, D * 0.3f, 12);
                        }
                        Fx.Spawn("fx_poisonbubble", at + (Vector3)(Random.insideUnitCircle * r), 2f, Color.white, 10f);     // 터지는 독 거품
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
                    float from = evo ? 11f : 9f, tick = 0f;
                    // 마디로 끊긴 거미줄 테 (그림의 테 반지름 29/64 → 지름 from × 2.2 가 테 = r) · 예전 한 줄 LineRenderer 원
                    FxAnim web = Fx.Play("fx_webring", c0, from * 2.2f, Color.white, 6f, 0f, 18, true, 2.1f);
                    Vector3 webScale = web != null ? web.transform.localScale : Vector3.one;
                    fx.Play("clank", 0.8f, 1.4f);
                    for (float t = 0f; t < 2f; t += Time.deltaTime)
                    {
                        float r = Mathf.Lerp(from, 1f, t / 2f);
                        if (web != null)
                        {
                            web.transform.localScale = webScale * (r / from);
                            web.transform.rotation = Quaternion.Euler(0f, 0f, t * 40f);
                        }
                        tick -= Time.deltaTime;
                        if (tick <= 0f)
                        {
                            tick = 0.12f;
                            using (var ovl = PhysHits.Circle(c0, r + 1f)) foreach (Collider2D c in ovl)
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
                    if (web != null) Destroy(web.gameObject);
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
                    using var aim = WeakHit.AimScope(1.75f);      // 빈틈: 커서 쪽으로 나란히 곧게 나가는 투창 (이 case 안에 yield 없음)
                    for (int i = -4; i <= 4; i++)
                    {
                        Bullet b = KitProjectile(player.transform.position + (Vector3)(side * i * 1.1f), dir, D * 4f, 9999, 36f, "fx_javelin", 1.4f, Color.white, false);     // 투창 (예전 화살 fx_arrow · 같은 5칸 높이)
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
                    Bullet b = KitProjectile(player.transform.position, d, D, 9999, 24f, "fx_mercury", 0.7f, Color.white, false);     // 출렁이는 수은 방울 (예전 흰 구슬 fx_orb · 같은 16칸)
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
                    // 1.0.2: 조준 원(반지름 12) 밖 최대 24칸까지 터지던 것을 원 안의 적으로
                    List<Transform> near = NearestEnemies(player.transform.position, 12f, 6);
                    Vector3 at = near.Count > 0 ? near[Random.Range(0, near.Count)].position : KitClamp(player.transform.position + (Vector3)(Random.insideUnitCircle * 8f));
                    FireworkBurst(at, evo ? 8 : 6, D * 1.2f);
                    yield return new WaitForSeconds(0.15f);
                }
                break;
            default:
                yield return StartCoroutine(HeroKitUlt(id, D, me, mouse, dir, evo));      // 1.0.2 새 캐릭터 (SpecialAbilities.NewHeroes)
                break;
        }
    }
}
