using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 거너가 아닌 캐릭터의 특수 능력 (ID 20 ~ 51): 캐릭터마다 무기 3 · 스킬 3 · 패시브 2
// 지옥의 문 · 특수 강화에서는 고른 캐릭터의 능력만 나옴 (CharacterData.pool)
// SpecialAbilities 의 나머지 부분(탄창 · 쿨타임 · 진화 · 강화 상점 · HUD)을 그대로 씀
// 거너의 능력 · 캐릭터 전용 레벨업 카드 · 다른 캐릭터의 능력과 겹치지 않게 캐릭터마다 따로 만듦
public partial class SpecialAbilities
{
    public const int KitFirstId = 20;
    // 검사: 묵직한 기사
    public const int KitHammer = 20, KitWhip = 21, KitLance = 22, KitWarCry = 23, KitJudgment = 24, KitEarthsplit = 25, KitUnyielding = 26, KitFervor = 27;
    // 도적: 속임수와 암살
    public const int KitBlowgun = 28, KitCards = 29, KitWire = 30, KitDeathMark = 31, KitCaltrops = 32, KitPickpocket = 33, KitEvasion = 34, KitSpree = 35;
    // 궁수: 사냥꾼
    public const int KitNetBow = 36, KitJavelin = 37, KitBurstBow = 38, KitFalcon = 39, KitGale = 40, KitFocus = 41, KitTrophy = 42, KitKeepDistance = 43;
    // 연금술사: 괴짜 발명
    public const int KitQuicksilver = 44, KitMagnet = 45, KitFirework = 46, KitStone = 47, KitRewind = 48, KitGiant = 49, KitCycle = 50, KitVolatile = 51;
    // 캐릭터마다 패시브 두 개씩 더 (52 ~ 59)
    public const int KitOath = 52, KitPlate = 53, KitVeil = 54, KitFugitive = 55, KitWeakspot = 56, KitInstinct = 57, KitEmergency = 58, KitGoldConvert = 59;
    // 1.1.2 새 캐릭터: 캐릭터마다 무기(진화 형태) 3 · 패시브 4 (스킬은 진화 캐릭터에서 쓰지 않아 없음)
    // 지도 제작자
    public const int KitSurveyChain = 60, KitInkpot = 61, KitGlobe = 62, KitExplorer = 63, KitLocal = 64, KitUncharted = 65, KitDraft = 66;
    // 아이콘: 60번부터는 Icons/ability_<400 + 번호> (60 ~ 91 은 레벨업 카드 아이콘이 쓰고 있음)
    public static string KitIconPath(int id) => "Icons/ability_" + (id >= 60 ? 400 + id : id);

    static readonly (string name, SpecialKind kind, string desc)[] KitDefs =
    {
        ("전쟁 망치", SpecialKind.Weapon, "가까운 땅을 내리찍어 주변 적에게 피해를 주고 잠깐 기절시킵니다. (공격력 200%)\n진화: 범위 증가, 기절 두 배"),
        ("채찍검", SpecialKind.Weapon, "길게 휘어지는 칼날 채찍. 끝부분에 맞은 적은 두 배 가까운 피해를 받습니다.\n진화: 더 길고, 끝 피해 증가"),
        ("기창", SpecialKind.Weapon, "창을 앞세워 짧게 돌격하며 지나간 적을 찌르고 밀쳐냅니다. (공격력 140%)\n진화: 더 멀리, 돌격 중 무적"),
        ("전투 함성", SpecialKind.Skill, "크게 외쳐 주변 적을 밀쳐내고 6초 동안 공격력이 30% 오릅니다.\n진화: 공격력 +45%"),
        ("심판의 대검", SpecialKind.Skill, "마우스 위치에 하늘에서 거대한 검이 떨어져 크게 베고 기절시킵니다. (공격력 640%)\n진화: 대검 세 자루"),
        ("대지 가르기", SpecialKind.Skill, "마우스 쪽으로 땅을 길게 갈라 한 줄의 적에게 피해를 주고 느리게 만듭니다. (공격력 240%)\n진화: 세 갈래로 갈라짐"),
        ("불굴", SpecialKind.Passive, "잃은 체력이 많을수록 공격력이 오릅니다. (최대 +40%)\n진화: 최대 +60%"),
        ("전투 열기", SpecialKind.Passive, "적을 처치할 때마다 4초 동안 공격 속도 +5% (최대 5중첩).\n진화: +6%, 최대 8중첩"),

        ("독침 대롱", SpecialKind.Weapon, "누르고 있으면 독침을 빠르게 붑니다. 맞은 적은 중독이 쌓입니다. (최대 5중첩)\n진화: 최대 10중첩"),
        ("도박 카드", SpecialKind.Weapon, "카드 세 장을 던집니다. 카드마다 피해가 제각각이고, 가끔 조커가 터집니다.\n진화: 다섯 장, 조커 확률 두 배"),
        ("살상 와이어", SpecialKind.Weapon, "클릭한 곳에 와이어 고리를 박습니다. 고리를 잇는 줄에 닿은 적은 계속 베입니다. (고리 3개)\n진화: 고리 5개"),
        ("죽음의 표식", SpecialKind.Skill, "마우스 근처의 적에게 표식. 3초 뒤 큰 피해로 터집니다. 그 전에 쓰러지면 쿨타임 초기화. (공격력 800%)\n진화: 표식 세 개"),
        ("마름쇠", SpecialKind.Skill, "주변에 마름쇠를 흩뿌립니다. 밟은 적은 피해를 입고 느려집니다.\n진화: 마름쇠 두 배"),
        ("소매치기", SpecialKind.Skill, "순식간에 주변 적 6명 사이를 누비며 벱니다. 털린 적은 쓰러질 때 코인을 1개 더 떨어뜨립니다.\n진화: 8명"),
        ("회피 본능", SpecialKind.Passive, "받는 공격을 15% 확률로 피합니다.\n진화: 25%"),
        ("연쇄 처치", SpecialKind.Passive, "적을 처치하면 2초 동안 이동 속도 +15%, 모든 스킬 쿨타임 0.3초 감소.\n진화: 쿨타임 0.6초 감소"),

        ("그물 활", SpecialKind.Weapon, "맞은 자리에 그물이 펼쳐져 주변 적을 묶습니다.\n진화: 그물이 더 넓고 오래"),
        ("투창", SpecialKind.Weapon, "모든 적을 꿰뚫는 무거운 창. 멀리 날아갈수록 피해가 커집니다. (최대 공격력 240%)\n진화: 최대 400%"),
        ("속사 활", SpecialKind.Weapon, "한 번 누르면 화살 세 발을 눈 깜짝할 새 연달아 쏩니다. (발당 공격력 60%)\n진화: 다섯 발"),
        ("매의 급습", SpecialKind.Skill, "사냥 매를 날려 마우스 근처의 적을 차례로 덮칩니다. (적마다 공격력 200%)\n진화: 더 많은 적"),
        ("돌풍 화살", SpecialKind.Skill, "거센 바람을 두른 거대한 화살. 한 줄을 꿰뚫고 적을 멀리 날려 버립니다. (공격력 240%)\n진화: 화살이 두 배 커짐"),
        ("사냥꾼의 집중", SpecialKind.Skill, "5초 동안 활시위를 순식간에 가득 당깁니다.\n진화: 8초"),
        ("전리품 사냥", SpecialKind.Passive, "적을 처치할 때마다 최대 체력 +0.4 (최대 +30).\n진화: +0.8 (최대 +60)"),
        ("거리 유지", SpecialKind.Passive, "5칸 안에 적이 없으면 공격력이 25% 오릅니다.\n진화: +40%"),

        ("수은 구슬", SpecialKind.Weapon, "벽과 적에게 튕겨 다니는 수은 구슬. 네 번 튕기면 터집니다.\n진화: 일곱 번 튕김"),
        ("자석 폭탄", SpecialKind.Weapon, "떨어진 자리로 주변 적을 끌어모은 뒤 폭발합니다. (공격력 180%)\n진화: 더 넓게 끌어당기고 두 번 폭발"),
        ("폭죽 발사기", SpecialKind.Weapon, "마우스 위치로 폭죽을 쏘아 올려 색색의 불꽃으로 터뜨립니다.\n진화: 불꽃 다섯 → 여덟 갈래"),
        ("현자의 돌", SpecialKind.Skill, "6초 동안 던지는 플라스크가 모두 불안정해져 크게 폭발합니다.\n진화: 9초"),
        ("시간 역행 물약", SpecialKind.Skill, "3초 전의 자리로 되돌아가고, 그때 체력이 더 많았다면 되찾습니다.\n진화: 쿨타임 감소"),
        ("거대화 물약", SpecialKind.Skill, "5초 동안 몸이 커져 받는 피해가 절반, 닿는 적을 밀쳐내며 피해를 줍니다.\n진화: 8초"),
        ("연금 순환", SpecialKind.Passive, "스킬을 쓸 때마다 체력 4% 회복, 다른 스킬 쿨타임 1초 감소.\n진화: 체력 6%, 2초 감소"),
        ("불안정 연구", SpecialKind.Passive, "불안정한 플라스크가 나올 확률이 15% → 35%, 불안정 폭발이 불을 붙입니다.\n진화: 50%"),

        ("기사의 맹세", SpecialKind.Passive, "체력이 가득 차 있는 동안 공격력이 15% 오릅니다.\n진화: +25%"),
        ("강철 갑옷", SpecialKind.Passive, "받는 피해가 12% 줄지만 이동 속도가 5% 느려집니다.\n진화: 피해 -20%, 느려지지 않음"),
        ("그림자 은신", SpecialKind.Passive, "3초 동안 맞지 않으면 다음 평타의 피해가 1.7배로 증가합니다.\n진화: 2초, 2.1배"),
        ("도망자의 발걸음", SpecialKind.Passive, "맞으면 1.5초 동안 이동 속도가 30% 빨라집니다.\n진화: +45%"),
        ("약점 간파", SpecialKind.Passive, "체력이 가득한 적에게 주는 피해가 25% 늘어납니다.\n진화: +50%"),
        ("사냥 본능", SpecialKind.Passive, "적을 20마리 처치할 때마다 다음 화살 3발이 저절로 가득 당겨집니다.\n진화: 12마리마다"),
        ("비상 물약", SpecialKind.Passive, "체력이 25% 아래로 떨어지면 체력 25%를 곧바로 회복합니다. (쿨타임 45초)\n진화: 쿨타임 30초"),
        ("금속 변환", SpecialKind.Passive, "적을 처치하면 8% 확률로 적이 금으로 변해 코인 3개를 줍니다.\n진화: 15%"),

        // ---------------- 지도 제작자 (60 ~ 66)
        ("측량 사슬", SpecialKind.Weapon, "획 끝에서 가장 가까운 적에게 측량 사슬이 튑니다. (피해 50%)\n궁극기: 삼각 측량"),
        ("잉크병", SpecialKind.Weapon, "획 끝에서 잉크병이 깨져 작게 터집니다. (피해 60%)\n궁극기: 먹물 해일"),
        ("지구본", SpecialKind.Weapon, "도형을 닫기 쉬워지고, 닫으면 안쪽 적이 가운데로 모이며 도형 피해 +30%.\n궁극기: 자전"),
        ("탐험가의 체력", SpecialKind.Passive, "도형을 닫을 때마다 체력을 2 회복합니다.\n진화: 4"),
        ("현지 안내", SpecialKind.Passive, "움직이는 동안 받는 피해가 10% 줄어듭니다.\n진화: 18%"),
        ("미지의 영역", SpecialKind.Passive, "6칸보다 멀리 있는 적에게 주는 피해가 20% 늘어납니다.\n진화: +35%"),
        ("정밀 제도", SpecialKind.Passive, "획 끝 가까이의 적에게 주는 피해가 30% 늘어납니다.\n진화: +50%"),
    };

    public static bool IsKit(int id) => id >= KitFirstId && id < KitFirstId + KitDefs.Length;
    public static bool KitIsWeapon(int id) => IsKit(id) && KitDefs[id - KitFirstId].kind == SpecialKind.Weapon;

    // 인스펙터의 능력 목록(거너) 뒤에 새 캐릭터 능력을 붙임 (ID = 순서)
    void KitExtendAbilities()
    {
        if (abilities == null || abilities.Length >= KitFirstId + KitDefs.Length) return;
        SpecialDef[] all = new SpecialDef[KitFirstId + KitDefs.Length];
        for (int i = 0; i < abilities.Length && i < KitFirstId; i++) all[i] = abilities[i];
        for (int i = 0; i < KitDefs.Length; i++)
        {
            int id = KitFirstId + i;
            all[id] = new SpecialDef
            {
                name = KitDefs[i].name,
                kind = KitDefs[i].kind,
                description = KitDefs[i].desc,
                icon = Resources.Load<Sprite>(KitIconPath(id)),
            };
        }
        abilities = all;
    }

    // ================================================================= 탄창 · 간격 · 장전 (0 = 탄약 없음)
    static int KitMag(int id) => id switch
    {
        KitBlowgun => 12, KitCards => 9, KitNetBow => 5, KitBurstBow => 6,
        KitQuicksilver => 6, KitMagnet => 4, KitFirework => 5, _ => 0,
    };
    static float KitInterval(int id) => id switch
    {
        KitHammer => 1f, KitWhip => 0.45f, KitLance => 0.8f,
        KitBlowgun => 0.3f, KitCards => 0.45f, KitWire => 0.25f,
        KitNetBow => 0.8f, KitJavelin => 1f, KitBurstBow => 0.6f,
        KitQuicksilver => 0.5f, KitMagnet => 1.1f, KitFirework => 0.7f, _ => 0.45f,
    };
    static float KitReload(int id) => id switch { KitBlowgun => 1.6f, KitMagnet => 2.2f, _ => 1.8f };
    static Color KitColor(int id) => id switch
    {
        >= 20 and < 28 => new Color(0.6f, 0.8f, 1f),
        >= 28 and < 36 => new Color(0.8f, 0.55f, 1f),
        >= 36 and < 44 => new Color(0.6f, 1f, 0.5f),
        >= 44 and < 52 => new Color(0.7f, 1f, 0.5f),
        >= 60 and < 67 => new Color(0.45f, 0.6f, 1f),
        _ => new Color(1f, 0.9f, 0.6f),
    };

    // ================================================================= 범위 피해 도우미
    int DamageArc(Vector3 origin, Vector2 dir, float range, float halfAngle, float damage, float knock)
    {
        int n = 0;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(origin, range))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Vector2 to = c.transform.position - origin;
            if (to.sqrMagnitude > 0.25f && Vector2.Angle(dir, to) > halfAngle) continue;
            Specials.Damage(c.gameObject, damage, to.normalized, knock);
            n++;
        }
        return n;
    }

    int DamageLine(Vector3 a, Vector3 b, float width, float damage, float knock)
    {
        int n = 0;
        foreach (Collider2D c in Physics2D.OverlapCircleAll((a + b) * 0.5f, Vector3.Distance(a, b) * 0.5f + width))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            if (Hostile.DistanceToSegment(c.transform.position, a, b) > width) continue;
            Specials.Damage(c.gameObject, damage, (b - a).normalized, knock);
            n++;
        }
        return n;
    }

    void DamageCircle(Vector3 pos, float r, float dmg, float knock)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, r))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Specials.Damage(c.gameObject, dmg, (c.transform.position - pos).normalized, knock);
        }
    }

    // 반지름 안의 살아 있는 일반 적 (보스 제외)
    static IEnumerable<EnermyController> EnemiesIn(Vector3 pos, float r)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, r))
        {
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && !e.IsDead) yield return e;
        }
    }

    // 마우스에 가까운 적부터 n 마리 (range 안)
    static List<Transform> NearestEnemies(Vector3 from, float range, int n)
    {
        List<Transform> list = new List<Transform>();
        foreach (Collider2D c in Physics2D.OverlapCircleAll(from, range))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && e.IsDead) continue;
            list.Add(c.transform);
        }
        list.Sort((a, b) => (a.position - from).sqrMagnitude.CompareTo((b.position - from).sqrMagnitude));
        if (list.Count > n) list.RemoveRange(n, list.Count - n);
        return list;
    }

    Bullet KitProjectile(Vector3 start, Vector2 dir, float dmg, int pene, float speed, string fxName, float size, Color tint, bool spin)
    {
        Bullet b = Shot(start, dir, dmg, pene, 1f, false, tint, 1f, 1f, 1f);
        if (b == null) return null;
        b.speed = speed;
        b.lifetime = 6f;
        SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
        Sprite[] f = Fx.Frames(fxName);
        if (sr != null && f.Length > 0)
        {
            sr.sprite = f[0];
            sr.color = tint;
            b.transform.localScale = Vector3.one * (size / f[0].bounds.size.y);
            FrameLoop loop = b.gameObject.AddComponent<FrameLoop>();
            loop.frames = f;
            loop.spin = spin ? -900f : 0f;
        }
        return b;
    }

    Vector3 KitClamp(Vector3 p)
    {
        Vector3 c = Hostile.ClampArena(p);
        return Hostile.IsWall(c) ? player.transform.position : c;
    }

    // from 에서 dir 로 dist 만큼, 벽 · 경기장 끝 앞에서 멈춘 자리
    Vector3 KitReach(Vector3 from, Vector2 dir, float dist)
    {
        float len = dist;
        for (float d = 0.5f; d <= dist; d += 0.5f)
        {
            Vector3 p = from + (Vector3)(dir * d);
            if (Hostile.IsWall(p) || (Hostile.ClampArena(p) - p).sqrMagnitude > 0.01f) { len = d - 0.5f; break; }
        }
        return from + (Vector3)(dir * Mathf.Max(0f, len));
    }

    // ================================================================= 무기 (Q로 교체, 좌클릭)
    bool whipAlt, lancing;

    void KitUpdateWeapon(int id, bool down, bool held, bool up)
    {
        Vector3 m = player.MuzzlePosition;
        Vector2 dir = AimDir();
        bool evo = IsEvolved(id);
        Color col = KitColor(id);
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        switch (id)
        {
            // ---------------- 검사
            case KitHammer:
                {
                    Vector3 me = player.transform.position;
                    Vector3 at = me + Vector3.ClampMagnitude(MouseWorld() - me, 4f);
                    float r = evo ? 3.2f : 2.5f;
                    fx.SetRing(previewRing, at, r, new Color(col.r, col.g, col.b, Ready() ? 0.6f : 0.25f), 0.1f);
                    if (!down || !Ready()) break;
                    BeginShot(0, out _, out _);
                    Smash(at, r, KitWD * 2.5f, evo ? 1f : 0.5f);
                    break;
                }
            case KitWhip:
                {
                    float len = evo ? 12f : 10f;
                    fx.SetLine(aimLine, m, m + (Vector3)(dir * len), new Color(col.r, col.g, col.b, 0.3f), 0.06f);
                    if (!held || !Ready()) break;
                    BeginShot(0, out _, out _);
                    Whip(m, dir, len, KitWD * 0.9f, evo ? 2.5f : 1.8f);
                    break;
                }
            case KitLance:
                {
                    float dist = evo ? 4.5f : 3f;
                    fx.SetLine(aimLine, m, m + (Vector3)(dir * (dist + 1.5f)), new Color(col.r, col.g, col.b, 0.4f), 0.1f);
                    if (!down || lancing || !Ready()) break;
                    BeginShot(0, out _, out _);
                    StartCoroutine(LanceCharge(dir, dist, 0.12f, KitWD * 1.8f, 2.5f, evo));
                    break;
                }

            // ---------------- 도적
            case KitBlowgun:
                if (!held || !Ready()) break;
                {
                    BeginShot(1, out Vector3 s, out _);
                    Bullet b = KitProjectile(s, dir, KitWD * 0.4f, player.pene, 55f, "fx_arrow", 0.3f, new Color(0.55f, 1f, 0.4f), false);
                    int max = evo ? 10 : 5;
                    float per = KitWD * 0.25f;
                    if (b != null) b.onHitEnemy += (bb, c) => Poison.Apply(c.gameObject, per, max);
                }
                break;
            case KitCards:
                if (!down || !Ready()) break;
                {
                    BeginShot(1, out Vector3 s, out _);
                    int n = evo ? 5 : 3;
                    for (int i = 0; i < n; i++)
                        ThrowCard(s, Quaternion.Euler(0f, 0f, (i - (n - 1) * 0.5f) * 12f) * dir, WDamage, evo ? 0.2f : 0.1f);
                }
                break;
            case KitWire:
                {
                    int max = evo ? 5 : 3;
                    Vector3 at = MouseWorld();
                    fx.SetRing(previewRing, at, 0.45f, new Color(col.r, col.g, col.b, 0.8f), 0.08f);
                    if (wires.Count > 0) fx.SetLine(aimLine, wires[wires.Count - 1].pos, at, new Color(0.85f, 0.8f, 1f, 0.35f), 0.05f);
                    player.ammoTextOverride = Loc.T("와이어") + " " + wires.Count + " / " + max;
                    if (!down || !Ready()) break;
                    BeginShot(0, out _, out _);
                    wires.Add((KitClamp(at), Time.time + 10f));
                    while (wires.Count > max) wires.RemoveAt(0);
                    Fx.Spawn("fx_spark", at, 1f, new Color(0.9f, 0.85f, 1f), 24f);
                    fx.Play("clank", 0.45f, 1.9f);
                    break;
                }

            // ---------------- 궁수
            case KitNetBow:
                if (!down || !Ready()) break;
                {
                    BeginShot(1, out Vector3 s, out _);
                    Bullet b = KitProjectile(s, dir, WDamage, 1, 45f, "fx_arrow", 0.55f, new Color(0.95f, 0.9f, 0.65f), false);
                    float r = evo ? 3f : 2f, hold = evo ? 2f : 1.2f;
                    if (b != null) b.onHitEnemy += (bb, c) => NetAt(c.transform.position, r, hold, 0f);
                }
                break;
            case KitJavelin:
                if (!down || !Ready()) break;
                {
                    BeginShot(0, out Vector3 s, out _);
                    Bullet b = KitProjectile(s, dir, WDamage, 9999, 32f, "fx_arrow", 1.3f, new Color(0.95f, 0.8f, 0.55f), false);
                    if (b != null)
                    {
                        b.hitOnce = new HashSet<int>();
                        JavelinGrow g = b.gameObject.AddComponent<JavelinGrow>();
                        g.baseDamage = WDamage;
                        g.maxMul = evo ? 4f : 3f;
                    }
                    fx.Play("whoosh", 0.7f, 0.8f);
                }
                break;
            case KitBurstBow:
                if (!down || !Ready()) break;
                BeginShot(1, out _, out _);
                StartCoroutine(BurstRoutine(evo ? 5 : 3, KitWD * 0.7f));
                break;

            // ---------------- 연금술사
            case KitQuicksilver:
                if (!down || !Ready()) break;
                {
                    BeginShot(1, out Vector3 s, out _);
                    Bullet b = KitProjectile(s, dir, KitWD * 0.9f, 9999, 26f, "fx_orb", 0.6f, new Color(0.85f, 0.9f, 1f), false);
                    if (b != null) Quicksilver(b, evo ? 7 : 4, WDamage);
                }
                break;
            case KitMagnet:
                {
                    Vector3 land = GrenadeLanding();
                    float pull = evo ? 6f : 4.5f;
                    fx.SetLine(aimLine, m, land, new Color(col.r, col.g, col.b, 0.4f), 0.08f);
                    fx.SetRing(previewRing, land, pull, new Color(0.7f, 0.6f, 1f, Ready() ? 0.6f : 0.25f), 0.1f);
                    if (!down || !Ready()) break;
                    BeginShot(1, out Vector3 s, out _);
                    float dmg = KitWD * 2.2f;
                    int blasts = evo ? 2 : 1;
                    FlaskLob.Throw(s, land, 0.4f, 0.8f, new Color(0.7f, 0.6f, 1f), p => StartCoroutine(MagnetPull(p, pull, 1.2f, dmg, blasts, 2.6f)));
                    break;
                }
            case KitFirework:
                {
                    Vector3 land = GrenadeLanding();
                    fx.SetLine(aimLine, m, land, new Color(1f, 0.8f, 0.4f, 0.35f), 0.06f);
                    fx.SetRing(previewRing, land, 2.2f, new Color(1f, 0.8f, 0.4f, Ready() ? 0.6f : 0.25f), 0.1f);
                    if (!down || !Ready()) break;
                    BeginShot(1, out Vector3 s, out _);
                    StartCoroutine(FireworkRoutine(s, land, evo ? 8 : 5, KitWD * 0.7f));
                    break;
                }
        }
    }

    // 검사 전쟁 망치 · 심판의 대검: 원 안의 적에게 피해 + 기절
    void Smash(Vector3 at, float r, float dmg, float stun)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(at, r))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Specials.Damage(c.gameObject, dmg, (c.transform.position - at).normalized, 1.5f);
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && stun > 0f) e.Slow(0f, stun);
        }
        Fx.Spawn("fx_shock", at, r * 2.2f, new Color(1f, 0.85f, 0.55f, 0.9f), 20f);
        Fx.Spawn("fx_fissure", at, r * 1.6f, Color.white, 16f);
        Hostile.Shake(0.12f);
        fx.Play("boom", 0.5f, 0.7f);
        fx.Play("thump", 0.7f, 0.8f);
    }

    // 검사 채찍검: 한 줄을 휘감고 끝부분은 더 아프게
    void Whip(Vector3 a, Vector2 dir, float len, float dmg, float tipMul)
    {
        whipAlt = !whipAlt;
        Vector2 side = new Vector2(-dir.y, dir.x) * (whipAlt ? 1f : -1f);
        Vector3 b = a + (Vector3)(dir * len);
        foreach (Collider2D c in Physics2D.OverlapCircleAll((a + b) * 0.5f, len * 0.5f + 1f))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            if (Hostile.DistanceToSegment(c.transform.position, a, b) > 0.9f) continue;
            bool tip = Vector2.Dot(c.transform.position - a, dir) >= len * 0.66f;
            Specials.Damage(c.gameObject, dmg * (tip ? tipMul : 1f), dir, tip ? 1.2f : 0.4f);
            if (tip) Fx.Spawn("fx_spark", c.transform.position, 1.3f, new Color(1f, 0.9f, 0.5f), 24f);
        }
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        for (float t = 0.6f; t <= len; t += 0.6f)
        {
            Vector3 p = a + (Vector3)(dir * t + side * Mathf.Sin(t / len * Mathf.PI) * 0.9f);
            Fx.Spawn("fx_trail_dot", p, t > len * 0.66f ? 0.9f : 0.6f, new Color(0.75f, 0.85f, 1f), 1f, rot, 16, false, 0.08f + t * 0.012f);
        }
        Fx.Spawn("fx_spark", b, 1.4f, new Color(1f, 0.9f, 0.5f), 24f);
        fx.Play("whoosh", 0.55f, 1.5f);
        fx.Play("crack", 0.45f, 1.7f);
    }

    // 검사 기창: 창을 앞세운 짧은 돌격
    IEnumerator LanceCharge(Vector2 dir, float dist, float time, float dmg, float knock, bool invincible)
    {
        lancing = true;
        Vector3 from = player.transform.position;
        Vector3 to = KitReach(from, dir, dist);
        if (invincible) player.GrantInvincibility(time + 0.15f);
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        fx.Play("whoosh", 0.8f, 1.1f);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            player.transform.position = Vector3.Lerp(from, to, t / time);
            yield return null;
        }
        player.transform.position = to;
        Vector3 tip = to + (Vector3)(dir * 1.8f);
        DamageLine(from, tip, 1.1f, dmg, knock);
        for (float k = 0f; k <= 1f; k += 0.2f) Fx.Spawn("fx_trail_dot", Vector3.Lerp(from, tip, k), 0.9f, new Color(0.8f, 0.9f, 1f), 1f, rot, 16, false, 0.15f);
        Fx.Spawn("fx_spark", tip, 1.4f, Color.white, 24f);
        fx.Play("crack", 0.6f, 1.2f);
        lancing = false;
    }

    // 도적 도박 카드: 피해 30% ~ 250% 무작위, 조커는 맞으면 터짐
    void ThrowCard(Vector3 s, Vector2 d, float baseDmg, float jokerChance)
    {
        bool joker = Random.value < jokerChance;
        float mul = joker ? 1f : Random.Range(0.3f, 2.5f);
        Color tint = joker ? new Color(1f, 0.35f, 0.4f) : mul > 1.8f ? new Color(1f, 0.85f, 0.35f) : Color.white;
        Bullet b = KitProjectile(s, d, baseDmg * mul, player.pene, 42f, "fx_card", 0.8f, tint, true);
        if (b == null || !joker) return;
        float boom = baseDmg * 2f;
        b.onHitEnemy += (bb, c) => Explode(c.transform.position, 2.2f, boom, 1.5f, new Color(1f, 0.3f, 0.4f, 0.85f));
    }

    // 도적 살상 와이어: 고리(끝나는 시각)들을 잇는 줄에 닿은 적을 계속 벰
    readonly List<(Vector3 pos, float until)> wires = new List<(Vector3, float)>();
    LineRenderer wireLine;
    float wireTick;

    void UpdateWires()
    {
        wires.RemoveAll(w => Time.time > w.until);
        if (wires.Count < 2)
        {
            if (wireLine != null) wireLine.enabled = false;
            return;
        }
        if (wireLine == null) wireLine = fx.NewLine("Wire", false, 17);
        bool loop = wires.Count >= 3;
        wireLine.enabled = true;
        wireLine.positionCount = wires.Count + (loop ? 1 : 0);
        for (int i = 0; i < wires.Count; i++) wireLine.SetPosition(i, wires[i].pos);
        if (loop) wireLine.SetPosition(wires.Count, wires[0].pos);
        float shine = 0.55f + 0.25f * Mathf.Sin(Time.time * 12f);
        wireLine.startColor = wireLine.endColor = new Color(0.85f, 0.8f, 1f, shine);
        wireLine.startWidth = wireLine.endWidth = 0.07f;

        wireTick -= Time.deltaTime;
        if (wireTick > 0f) return;
        wireTick = 0.2f;
        float dmg = KitD * WeaponDamageMul(KitWire) * 0.5f;
        for (int i = 0; i < wires.Count - (loop ? 0 : 1); i++)
        {
            Vector3 a = wires[i].pos, b = wires[(i + 1) % wires.Count].pos;
            if (DamageLine(a, b, 0.5f, dmg, 0f) > 0) Fx.Spawn("fx_spark", Vector3.Lerp(a, b, Random.value), 0.8f, new Color(1f, 0.5f, 0.6f), 24f);
        }
    }

    // 궁수 그물: 원 안의 적을 묶음
    void NetAt(Vector3 p, float r, float hold, float dmg)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(p, r))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            if (dmg > 0f) Specials.Damage(c.gameObject, dmg, Vector3.zero, 0f);
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null) e.Slow(0f, hold);
        }
        Fx.Spawn("fx_net", p, r * 2.2f, Color.white, 10f, Random.Range(0f, 90f), 11);
        fx.Play("clank", 0.35f, 1.4f);
        fx.Play("whoosh", 0.3f, 0.8f);
    }

    // 궁수 속사 활: 조준 방향으로 빠르게 연달아
    IEnumerator BurstRoutine(int n, float dmg)
    {
        for (int i = 0; i < n; i++)
        {
            Vector2 d = Quaternion.Euler(0f, 0f, Random.Range(-2f, 2f)) * AimDir();
            KitProjectile(player.MuzzlePosition, d, dmg, player.pene, 65f, "fx_arrow", 0.4f, Color.white, false);
            fx.Play("bowtwang", 0.35f, 1.6f + 0.08f * i);
            if (i == 0) fx.Play("arrowfly", 0.3f, 1.3f);
            yield return new WaitForSeconds(0.07f);
        }
    }

    // 연금술사 수은 구슬: 벽 · 적에 튕길 때마다 한 번 세고, 다 튕기면 터짐
    void Quicksilver(Bullet b, int bounces, float boom)
    {
        int left = bounces;
        b.lifetime = 8f;
        System.Action bounce = () =>
        {
            left--;
            fx.Play("clank", 0.2f, 2.1f);
            if (left > 0 || b == null) return;
            Explode(b.transform.position, 1.8f, boom * 1.2f, 1f, new Color(0.85f, 0.9f, 1f, 0.85f));
            Destroy(b.gameObject);
        };
        b.onHitWall = () =>
        {
            if (b == null) return;
            b.Dir = Quaternion.Euler(0f, 0f, 180f + Random.Range(-35f, 35f)) * b.Direction;
            b.transform.position += (Vector3)(b.Direction * 0.3f);
            bounce();
        };
        b.onHitEnemy += (bb, c) =>
        {
            Vector2 away = bb.transform.position - c.transform.position;
            if (away.sqrMagnitude < 0.01f) away = -bb.Direction;
            bb.Dir = Quaternion.Euler(0f, 0f, Random.Range(-40f, 40f)) * away.normalized;
            bounce();
        };
    }

    // 연금술사 자석: 한 점으로 적을 끌어당긴 뒤 터짐
    IEnumerator MagnetPull(Vector3 p, float r, float time, float dmg, int blasts, float blastR)
    {
        Fx.Spawn("fx_vortex", p, r * 2f, new Color(0.7f, 0.6f, 1f, 0.8f), 16f, 0f, 12, true, time);
        fx.Play("hum", 0.5f, 1.4f);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            Pull(p, r, 7f);
            yield return null;
        }
        for (int i = 0; i < blasts; i++)
        {
            Explode(p, blastR, dmg, 2f, new Color(0.7f, 0.55f, 1f, 0.9f));
            if (i + 1 < blasts) yield return new WaitForSeconds(0.25f);
        }
    }

    static readonly List<Collider2D> pullHits = new List<Collider2D>(64);

    // 매 프레임 부르므로 재사용 목록으로 (적을 옮기기만 해서 목록이 바뀌지 않음)
    static void Pull(Vector3 p, float r, float speed)
    {
        foreach (Collider2D c in Specials.Overlap(p, r, pullHits))
        {
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && !e.IsDead) e.transform.position = Vector3.MoveTowards(e.transform.position, p, speed * Time.deltaTime);
        }
    }

    // 연금술사 폭죽: 솟아올라 색색으로 터짐
    static readonly Color[] FireworkColors = { new Color(1f, 0.4f, 0.4f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 1f, 0.5f), new Color(0.4f, 0.7f, 1f), new Color(0.9f, 0.5f, 1f) };

    IEnumerator FireworkRoutine(Vector3 from, Vector3 to, int sparks, float dmg)
    {
        const float rise = 0.35f;
        for (float t = 0f; t < rise; t += Time.deltaTime)
        {
            Vector3 p = Vector3.Lerp(from, to, t / rise) + Vector3.up * Mathf.Sin(t / rise * Mathf.PI) * 1.5f;
            if (Random.value < 0.6f) Fx.Spawn("fx_spark", p, 0.6f, new Color(1f, 0.9f, 0.6f), 24f);
            yield return null;
        }
        FireworkBurst(to, sparks, dmg);
    }

    void FireworkBurst(Vector3 at, int sparks, float dmg)
    {
        float spin = Random.Range(0f, 360f);
        for (int i = 0; i < sparks; i++)
        {
            float a = (spin + 360f / sparks * i) * Mathf.Deg2Rad;
            Vector3 q = at + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 1.8f;
            Color c = FireworkColors[i % FireworkColors.Length];
            DamageCircle(q, 1.4f, dmg, 0.8f);
            Fx.Spawn("fx_explosion", q, 2.2f, c, 20f);
        }
        Fx.Spawn("fx_sparkle", at, 3f, Color.white, 16f);
        fx.Play("boom", 0.4f, 1.5f);
        fx.Play("crackle", 0.4f, 1.3f);
    }

    // ================================================================= 스킬 (E · F · C)
    bool KitUseSkill(int id)
    {
        if (!IsKit(id)) return false;
        bool evo = IsEvolved(id);
        Vector3 pos = player.transform.position;
        Vector3 mouse = MouseWorld();
        Vector2 dir = ((Vector2)(mouse - pos)).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        switch (id)
        {
            case KitWarCry:
                warCryUntil = Time.time + 6f;
                foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, 5f))
                    if (c.CompareTag("enermy") || c.CompareTag("boss")) Specials.Damage(c.gameObject, KitD * 0.5f, (c.transform.position - pos).normalized, 4f);
                Fx.Spawn("fx_shock", pos, 11f, new Color(1f, 0.75f, 0.4f, 0.85f), 16f);
                fx.FloatText(pos, Loc.T("전투 함성!"), new Color(1f, 0.75f, 0.4f), 5f, 0f);
                fx.Play("roar", 0.9f, 1.1f);
                Hostile.Shake(0.2f);
                StartCooldown(id, 15f);
                break;
            case KitJudgment:
                StartCoroutine(JudgmentRoutine(KitClamp(mouse), evo));
                StartCooldown(id, 10f);
                break;
            case KitEarthsplit:
                for (int k = 0; k < (evo ? 3 : 1); k++)
                    StartCoroutine(EarthsplitRoutine(pos, Quaternion.Euler(0f, 0f, (evo ? k - 1 : 0) * 20f) * dir));
                StartCooldown(id, 8f);
                break;
            case KitDeathMark:
                {
                    List<Transform> targets = NearestEnemies(mouse, 12f, evo ? 3 : 1);
                    if (targets.Count == 0) { NoTarget(pos); return true; }
                    markResetUsed = false;
                    foreach (Transform t in targets) StartCoroutine(DeathMarkRoutine(t, id));
                    fx.Play("crack", 0.5f, 0.6f);
                    StartCooldown(id, 10f);
                    break;
                }
            case KitCaltrops:
                for (int i = 0; i < (evo ? 20 : 12); i++)
                {
                    Vector3 at = KitClamp(pos + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(1.2f, 5f)));
                    Caltrop.Place(at, KitD * 0.8f);
                }
                fx.Play("clank", 0.6f, 1.6f);
                fx.Play("crackle", 0.4f, 1.8f);
                StartCooldown(id, 10f);
                break;
            case KitPickpocket:
                {
                    List<Transform> targets = NearestEnemies(pos, 7f, evo ? 8 : 6);
                    if (targets.Count == 0) { NoTarget(pos); return true; }
                    StartCoroutine(PickpocketRoutine(targets, 1));
                    StartCooldown(id, 12f);
                    break;
                }
            case KitFalcon:
                {
                    List<Transform> targets = NearestEnemies(mouse, 14f, evo ? 8 : 5);
                    if (targets.Count == 0) { NoTarget(pos); return true; }
                    StartCoroutine(FalconRoutine(targets));
                    StartCooldown(id, 9f);
                    break;
                }
            case KitGale:
                {
                    Bullet b = KitProjectile(player.MuzzlePosition, dir, KitD * 3f, 9999, 38f, "fx_arrow", evo ? 3f : 1.6f, new Color(0.7f, 1f, 0.95f), false);
                    if (b != null)
                    {
                        b.hitOnce = new HashSet<int>();
                        b.knockBack = 6f;
                        b.gameObject.AddComponent<WindTrail>();
                    }
                    fx.Play("whoosh", 1f, 0.6f);
                    fx.Play("bowtwang", 0.9f, 0.7f);
                    StartCooldown(id, 7f);
                    break;
                }
            case KitFocus:
                focusUntil = Time.time + (evo ? 8f : 5f);
                Fx.Spawn("fx_levelup", pos + Vector3.up, 3.5f, new Color(0.7f, 1f, 0.6f), 14f, 0f, 30);
                fx.FloatText(pos, Loc.T("집중!"), new Color(0.7f, 1f, 0.6f), 5f, 0f);
                fx.Play("chime", 0.7f, 1.4f);
                StartCooldown(id, 16f);
                break;
            case KitStone:
                stoneUntil = Time.time + (evo ? 9f : 6f);
                Fx.Spawn("fx_rune", pos, 4f, new Color(0.9f, 0.5f, 1f), 14f, 0f, 3);
                fx.FloatText(pos, Loc.T("현자의 돌!"), new Color(0.9f, 0.55f, 1f), 5f, 0f);
                fx.Play("shimmer", 0.8f, 0.9f);
                StartCooldown(id, 18f);
                break;
            case KitRewind:
                {
                    if (rewindLog.Count == 0) { NoTarget(pos); return true; }
                    var past = rewindLog.Peek();
                    Fx.Spawn("fx_vortex", pos, 3f, new Color(0.6f, 0.9f, 1f, 0.8f), 18f);
                    player.transform.position = past.pos;
                    if (past.hp > player.PlayerHealth) player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, past.hp);
                    player.GrantInvincibility(0.5f);
                    Fx.Spawn("fx_vortex", past.pos, 3f, new Color(0.6f, 0.9f, 1f, 0.8f), 18f);
                    fx.FloatText(past.pos, Loc.T("시간 역행!"), new Color(0.6f, 0.9f, 1f), 5f, 0f);
                    fx.Play("shimmer", 0.9f, 0.6f);
                    rewindLog.Clear();
                    StartCooldown(id, evo ? 12f : 20f);
                    break;
                }
            case KitGiant:
                StartGiant(evo ? 8f : 5f);
                StartCooldown(id, 18f);
                break;
            default:
                return false;
        }
        // 연금 순환: 스킬을 쓸 때마다 회복 · 다른 스킬 쿨타임 감소
        if (Has(KitCycle))
        {
            bool e = IsEvolved(KitCycle);
            player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + player.PlayerMaxHealth * (e ? 0.06f : 0.04f));
            ReduceCooldowns(e ? 2f : 1f, id);
            Fx.Spawn("fx_sparkle", pos, 1.6f, new Color(0.6f, 1f, 0.6f), 18f);
        }
        return true;
    }

    void NoTarget(Vector3 pos)
    {
        fx.Play("buzz", 0.6f);
        fx.FloatText(pos, Loc.T("대상 없음"), new Color(0.7f, 0.66f, 0.72f), 4f, 0.4f);
    }

    // 쿨타임 줄이기 (except 는 빼고)
    void ReduceCooldowns(float seconds, int except = -1)
    {
        foreach (int k in new List<int>(cooldownUntil.Keys))
            if (k != except) cooldownUntil[k] -= seconds;
    }

    IEnumerator JudgmentRoutine(Vector3 at, bool evo)
    {
        List<Vector3> spots = new List<Vector3> { at };
        if (evo)
            for (int i = 0; i < 2; i++) spots.Add(KitClamp(at + (Vector3)(Random.insideUnitCircle.normalized * 4f)));
        foreach (Vector3 s in spots) Hostile.Circle(s, 3.5f, 0.6f, new Color(1f, 0.85f, 0.4f, 0.7f));
        fx.Play("railcharge", 0.5f, 1.2f);
        yield return new WaitForSeconds(0.45f);
        foreach (Vector3 s in spots)
        {
            FxAnim sword = Fx.Play("fx_bigsword", s + Vector3.up * 8f, 5f, Color.white, 1f, 0f, 18, true, 0.35f);
            for (float t = 0f; t < 0.12f && sword != null; t += Time.deltaTime)
            {
                sword.transform.position = Vector3.Lerp(s + Vector3.up * 8f, s + Vector3.up * 1.5f, t / 0.12f);
                yield return null;
            }
            if (sword != null) sword.transform.position = s + Vector3.up * 1.5f;
            Smash(s, 3.5f, KitD * 8f, 1f);
            fx.Play("bigboom", 0.7f, 0.9f);
            Hostile.Shake(0.3f);
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator EarthsplitRoutine(Vector3 from, Vector2 dir)
    {
        HashSet<Collider2D> hit = new HashSet<Collider2D>();
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        fx.Play("thump", 0.8f, 0.6f);
        for (float d = 1.2f; d <= 14f; d += 1.2f)
        {
            Vector3 p = from + (Vector3)(dir * d);
            if (Hostile.IsWall(p)) break;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(p, 1.3f))
            {
                if ((!c.CompareTag("enermy") && !c.CompareTag("boss")) || !hit.Add(c)) continue;
                Specials.Damage(c.gameObject, KitD * 3f, Vector3.up, 1f);
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null) e.Slow(0.4f, 2f);
            }
            Fx.Spawn("fx_fissure", p, 2.2f, Color.white, 18f, rot, 11);
            if (d % 3.6f < 1.2f) fx.Play("crack", 0.35f, 0.7f);
            yield return new WaitForSeconds(0.03f);
        }
    }

    bool markResetUsed;

    IEnumerator DeathMarkRoutine(Transform t, int id)
    {
        FxAnim mark = Fx.Play("fx_markskull", t.position + Vector3.up * 1.4f, 1.4f, new Color(1f, 0.35f, 0.4f), 8f, 0f, 18, true, 3f);
        EnermyController e = t.GetComponent<EnermyController>();
        for (float k = 0f; k < 3f; k += Time.deltaTime)
        {
            if (t == null || (e != null && e.IsDead))
            {
                // 표식이 터지기 전에 쓰러짐: 쿨타임 초기화 (한 번 쓸 때 한 번만)
                if (!markResetUsed)
                {
                    markResetUsed = true;
                    cooldownUntil[id] = Time.time;
                    fx.FloatText(player.transform.position, Loc.T("표식 초기화!"), new Color(1f, 0.45f, 0.5f), 4.5f, 0f);
                    fx.Play("ding", 0.6f, 1.6f);
                }
                if (mark != null) Destroy(mark.gameObject);
                yield break;
            }
            if (mark != null) mark.transform.position = t.position + Vector3.up * 1.4f;
            yield return null;
        }
        if (mark != null) Destroy(mark.gameObject);
        Specials.Damage(t.gameObject, KitD * 10f, Vector3.zero, 1f);
        Fx.Spawn("fx_deathburst", t.position, 3f, new Color(1f, 0.35f, 0.4f), 18f);
        fx.Play("crack", 0.9f, 0.8f);
        Hostile.Shake(0.15f);
    }

    static bool IsDeadEnemy(Transform t) => t.TryGetComponent(out EnermyController e) && e.IsDead;

    IEnumerator PickpocketRoutine(List<Transform> targets, int coins)
    {
        Vector3 home = player.transform.position;
        player.GrantInvincibility(targets.Count * 0.09f + 0.4f);
        foreach (Transform t in targets)
        {
            if (t == null || IsDeadEnemy(t)) continue;      // 1.1.0: 앞선 공격에 이미 쓰러진 적은 건너뜀
            Fx.Spawn("fx_stealth", player.transform.position, 2f, new Color(0.85f, 0.7f, 1f), 20f);
            player.transform.position = KitClamp(t.position + (Vector3)(Random.insideUnitCircle.normalized * 1f));
            Specials.Damage(t.gameObject, KitD * 1.5f, Vector3.zero, 0.5f);
            EnermyController e = t.GetComponent<EnermyController>();
            if (e != null) e.coinDrop += coins;
            Fx.Spawn("fx_sparkle", t.position, 1.2f, new Color(1f, 0.85f, 0.3f), 20f);
            fx.Play("pop", 0.4f, 1.6f);
            yield return new WaitForSeconds(0.08f);
        }
        Fx.Spawn("fx_stealth", player.transform.position, 2f, new Color(0.85f, 0.7f, 1f), 20f);
        player.transform.position = home;
        fx.Play("whoosh", 0.7f, 1.6f);
    }

    IEnumerator FalconRoutine(List<Transform> targets)
    {
        Sprite[] f = Fx.Frames("fx_falcon");
        if (f.Length == 0) yield break;
        GameObject bird = MakeSprite("Falcon", f[0], player.transform.position + Vector3.up * 1.5f, 1.4f / f[0].bounds.size.y, Color.white, "Effect", 20);
        FrameLoop loop = bird.AddComponent<FrameLoop>();
        loop.frames = f;
        loop.fps = 10f;
        SpriteRenderer sr = bird.GetComponent<SpriteRenderer>();
        fx.Play("whoosh", 0.8f, 1.8f);
        foreach (Transform picked in targets)
        {
            Transform t = picked;
            // 1.1.0: 이미 쓰러진 적이면 그 근처의 살아 있는 적에게 (시체를 헛치지 않게)
            if (t != null && IsDeadEnemy(t)) t = Specials.NearestEnemy(t.position, 6f);
            if (t == null) continue;
            Vector3 from = bird.transform.position;
            for (float k = 0f; k < 0.13f && t != null; k += Time.deltaTime)
            {
                bird.transform.position = Vector3.Lerp(from, t.position, k / 0.13f);
                sr.flipX = t.position.x < from.x;
                yield return null;
            }
            if (t == null) continue;
            Specials.Damage(t.gameObject, KitD * 2.5f, (t.position - from).normalized, 1f);
            Fx.Spawn("fx_slash", t.position, 1.8f, new Color(1f, 0.9f, 0.7f), 24f, Random.Range(0f, 360f), 16);
            fx.Play("slash", 0.5f, 1.8f);
            yield return new WaitForSeconds(0.04f);
        }
        bird.AddComponent<FadeOut>().duration = 0.4f;
    }

    // ================================================================= 캐릭터 상태 (스킬 · 패시브)
    float warCryUntil, focusUntil, stoneUntil, spreeUntil, fervorUntil, giantUntil;
    int fervor;
    float trophyHp;
    // 3초 전 기록 (시간 역행 물약)
    readonly Queue<(float t, Vector3 pos, float hp)> rewindLog = new Queue<(float, Vector3, float)>();
    float rewindRecord;
    // 매 프레임 곱해 둔 값 (다음 프레임에 되돌리고 다시 곱함)
    float appliedDmg = 1f, appliedRate = 1f, appliedSpeed = 1f;
    float giantTick;

    // 1.0.6: 영웅 특수 능력 전체 하향 — 피해를 주는 무기 · 스킬 · 궁극기는 0.8배 (설명의 공격력 %도 0.8배)
    const float KitPower = 0.8f;
    float KitD => Damage * KitPower;
    float KitWD => WDamage * KitPower;

    public bool KitFocusActive => Time.time < focusUntil;
    public bool KitStoneActive => Time.time < stoneUntil;
    public float KitUnstableChance => Has(KitVolatile) ? (IsEvolved(KitVolatile) ? 0.5f : 0.35f) : 0.15f;
    public bool KitUnstableBurns => Has(KitVolatile);

    void KitTick()
    {
        if (player == null) return;
        float dmg = 1f, rate = 1f, speed = 1f;
        if (Has(KitUnyielding))
        {
            bool e = IsEvolved(KitUnyielding);
            float lost = 1f - Mathf.Clamp01(player.PlayerHealth / Mathf.Max(1f, player.PlayerMaxHealth));
            dmg *= 1f + Mathf.Min(e ? 0.6f : 0.4f, lost * (e ? 0.8f : 0.5f));
        }
        if (Time.time > fervorUntil) fervor = 0;
        rate *= 1f + fervor * (IsEvolved(KitFervor) ? 0.06f : 0.05f);
        if (Time.time < warCryUntil) dmg *= IsEvolved(KitWarCry) ? 1.45f : 1.3f;
        if (Time.time < spreeUntil) speed *= 1.15f;
        if (Has(KitOath) && player.PlayerHealth >= player.PlayerMaxHealth * 0.99f) dmg *= IsEvolved(KitOath) ? 1.25f : 1.15f;
        if (Time.time < fugitiveUntil) speed *= IsEvolved(KitFugitive) ? 1.45f : 1.3f;
        if (Has(KitKeepDistance) && Specials.NearestEnemy(player.transform.position, 5f) == null)
            dmg *= IsEvolved(KitKeepDistance) ? 1.4f : 1.25f;
        ApplyDynamic(dmg, rate, speed);

        UpdateWires();
        if (Has(KitRewind)) RecordRewind();
        UpdateGiant();
    }

    void ApplyDynamic(float dmg, float rate, float speed)
    {
        player.damage *= dmg / appliedDmg; appliedDmg = dmg;
        player.fireRateMultiplier *= rate / appliedRate; appliedRate = rate;
        player.speed *= speed / appliedSpeed; appliedSpeed = speed;
    }

    void RecordRewind()
    {
        rewindRecord -= Time.deltaTime;
        if (rewindRecord > 0f) return;
        rewindRecord = 0.1f;
        rewindLog.Enqueue((Time.time, player.transform.position, player.PlayerHealth));
        while (rewindLog.Count > 0 && Time.time - rewindLog.Peek().t > 3f) rewindLog.Dequeue();
    }

    void StartGiant(float seconds)
    {
        if (Time.time >= giantUntil)
        {
            // 곱했다가 나눔 (축소 물약과 겹쳐도 원래 크기로 돌아오게)
            player.transform.localScale *= 1.5f;
            player.def += 0.5f;
        }
        giantUntil = Time.time + seconds;
        Fx.Spawn("fx_alchemyblast", player.transform.position, 4f, new Color(0.7f, 1f, 0.5f), 16f);
        fx.Play("bubble", 0.8f, 0.6f);
        fx.Play("roar", 0.5f, 0.7f);
    }

    void UpdateGiant()
    {
        if (giantUntil <= 0f) return;
        if (Time.time >= giantUntil)
        {
            giantUntil = 0f;
            player.transform.localScale /= 1.5f;
            player.def -= 0.5f;
            fx.Play("pop", 0.6f, 0.8f);
            return;
        }
        giantTick -= Time.deltaTime;
        if (giantTick > 0f) return;
        giantTick = 0.3f;
        DamageCircle(player.transform.position, 2f, Damage, 3f);
    }

    void KitOnKill(Vector3 pos)
    {
        if (player == null) return;
        if (Has(KitFervor))
        {
            fervor = Mathf.Min(IsEvolved(KitFervor) ? 8 : 5, fervor + 1);
            fervorUntil = Time.time + 4f;
        }
        if (Has(KitSpree))
        {
            spreeUntil = Time.time + 2f;
            ReduceCooldowns(IsEvolved(KitSpree) ? 0.6f : 0.3f);
        }
        if (Has(KitInstinct))
        {
            instinctKills++;
            if (instinctKills >= (IsEvolved(KitInstinct) ? 12 : 20))
            {
                instinctKills = 0;
                KitFreeDraws = 3;
                fx.FloatText(player.transform.position, Loc.T("사냥 본능!"), new Color(0.7f, 1f, 0.6f), 4.5f, 0.3f);
                fx.Play("chime", 0.5f, 1.5f);
            }
        }
        if (Has(KitGoldConvert) && Random.value < (IsEvolved(KitGoldConvert) ? 0.15f : 0.08f))
        {
            Coin c = FindFirstObjectByType<Coin>();
            if (c != null) c.AddCoin(3);
            Fx.Spawn("fx_sparkle", pos, 1.4f, new Color(1f, 0.85f, 0.3f), 18f);
            fx.FloatText(pos, "+3", new Color(1f, 0.85f, 0.3f), 4f, 0.2f);
        }
        if (Has(KitTrophy))
        {
            bool e = IsEvolved(KitTrophy);
            float add = e ? 0.8f : 0.4f;
            if (trophyHp < (e ? 60f : 30f))
            {
                trophyHp += add;
                player.PlayerMaxHealth += add;
                player.PlayerHealth += add;
            }
        }
    }

    // 도적 회피 본능 (PlayerController.TryHit)
    public bool KitDodge()
    {
        if (!Has(KitEvasion) || Random.value >= (IsEvolved(KitEvasion) ? 0.25f : 0.15f)) return false;
        fx.FloatText(player.transform.position, Loc.T("회피!"), new Color(0.8f, 0.7f, 1f), 4.5f, 0.2f);
        Fx.Spawn("fx_stealth", player.transform.position, 2f, new Color(0.8f, 0.7f, 1f, 0.7f), 20f);
        fx.Play("whoosh", 0.5f, 2f);
        player.GrantInvincibility(0.3f);
        return true;
    }

    // 새 패시브 상태
    float fugitiveUntil, lastHurt = -999f, emergencyReady;
    int instinctKills;
    // 사냥 본능: 남은 "저절로 가득 당겨지는 화살" 수 (CharacterKit 이 씀)
    [System.NonSerialized] public int KitFreeDraws;

    // 그림자 은신: 오래 안 맞았으면 평타 피해 배율을 주고 다시 모으기 시작
    public float KitConsumeVeil()
    {
        if (!Has(KitVeil)) return 1f;
        bool evo = IsEvolved(KitVeil);
        if (Time.time - lastHurt < (evo ? 2f : 3f)) return 1f;
        lastHurt = Time.time;                   // 한 번 쓰면 다시 기다림
        Fx.Spawn("fx_stealth", player.transform.position, 1.6f, new Color(0.7f, 0.5f, 1f, 0.8f), 20f);
        return evo ? 2.1f : 1.7f;
    }

    // 플레이어가 맞았을 때 (OnPlayerHurt)
    void KitOnHurt()
    {
        lastHurt = Time.time;
        if (Has(KitFugitive)) fugitiveUntil = Time.time + 1.5f;
        if (Has(KitEmergency) && Time.time >= emergencyReady && player.PlayerHealth > 0f && player.PlayerHealth < player.PlayerMaxHealth * 0.25f)
        {
            emergencyReady = Time.time + (IsEvolved(KitEmergency) ? 30f : 45f);
            player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + player.PlayerMaxHealth * 0.25f);
            Fx.Spawn("fx_levelup", player.transform.position + Vector3.up, 3.5f, new Color(0.6f, 1f, 0.6f), 14f, 0f, 30);
            fx.FloatText(player.transform.position, Loc.T("비상 물약!"), new Color(0.5f, 1f, 0.5f), 5f, 0f);
            fx.Play("bubble", 0.8f, 1.2f);
        }
    }

    // 적이 받는 피해 (EnermyController.DamageHook): 약점 간파 · 도적 급소 노리기 · 궁수 사냥감 표식
    // 검사 · 궁수 · 소각탄이 없는 거너
    bool NoNativeDot => CharacterData.Selected == CharacterId.Swordsman || CharacterData.Selected == CharacterId.Archer
                        || (CharacterData.IsGunner && gunCard[GunIncendiary] == 0);

    // 지금 들어가는 피해가 이미 치명타를 굴린 총알인지 (Bullet 이 피해를 주는 동안만 켜짐)
    public static bool CritRolled;

    // 치명타 판정: 영혼 트리 치명타 확률 · 피해 (피해 숫자도 치명타로)
    public float RollCrit(float damage)
    {
        if (TreeCrit <= 0f || Random.value >= TreeCrit) return damage;
        DamagePopup.NextCrit = true;
        return damage * TreeCritDamage;
    }

    float KitDamageHook(EnermyController e, float damage)
    {
        // 치명타 · 처형: 거너 평타 총알은 쏠 때 이미 굴림 (GunCards), 그 밖의 모든 공격 · 스킬 · 필살기는 여기서
        if (!CritRolled)
        {
            damage = RollCrit(damage);
            if (TreeExecute > 0f && e.EnemyHealth <= e.setEnemyHP * 0.2f) damage *= 1f + TreeExecute;
        }
        if (Has(KitWeakspot) && e.EnemyHealth >= e.setEnemyHP * 0.999f) damage *= IsEvolved(KitWeakspot) ? 1.5f : 1.25f;
        CharacterKit kit = CharacterKit.Instance;
        if (kit != null) damage *= kit.TargetDamageMul(e);
        // 저주 전이: 화상 · 독 · 출혈을 스스로 못 거는 캐릭터는 공격이 25% 확률로 3초 출혈 (옮길 상태 이상이 생기게)
        if (curseTargets > 0 && NoNativeDot && e.GetComponent<Bleed>() == null && Random.value < 0.25f)
            Bleed.Apply(e.gameObject, Damage * 0.3f, 3f);
        return damage;
    }

    // 패시브 장착 · 진화 때 한 번
    void KitOnEquip(int id, bool evolving)
    {
        if (player == null || id != KitPlate) return;
        if (!evolving) { player.def += 0.12f; player.speed *= 0.95f; }
        else { player.def += 0.08f; player.speed /= 0.95f; }
    }

    // HUD 아이콘 아래 짧은 상태 · 툴팁의 자세한 상태 (캐릭터 능력)
    string KitPassiveState(int id, bool detail)
    {
        switch (id)
        {
            case KitUnyielding:
                {
                    bool e = IsEvolved(id);
                    float lost = 1f - Mathf.Clamp01(player.PlayerHealth / Mathf.Max(1f, player.PlayerMaxHealth));
                    int pct = Mathf.RoundToInt(Mathf.Min(e ? 0.8f : 0.5f, lost * (e ? 1f : 0.6f)) * 100f);
                    return detail ? Loc.T("지금 공격력 +") + pct + "%" : "+" + pct + "%";
                }
            case KitFervor: return detail ? Loc.T("중첩 ") + fervor + Loc.T(" · 공격 속도 +") + Mathf.RoundToInt(fervor * (IsEvolved(id) ? 8f : 6f)) + "%" : "x" + fervor;
            case KitEvasion: return (IsEvolved(id) ? "30" : "20") + "%";
            case KitSpree: return Time.time < spreeUntil ? (detail ? Loc.T("질주 중") : "ON") : "";
            case KitTrophy: return detail ? Loc.T("최대 체력 +") + trophyHp.ToString("0.#") : "+" + trophyHp.ToString("0");
            case KitKeepDistance:
                {
                    bool on = Specials.NearestEnemy(player.transform.position, 5f) == null;
                    return detail ? (on ? Loc.T("발동 중 (주변에 적 없음)") : Loc.T("꺼짐 (5칸 안에 적)")) : on ? "ON" : "OFF";
                }
            case KitCycle: return "";
            case KitVolatile: return Mathf.RoundToInt(KitUnstableChance * 100f) + "%";
            case KitOath:
                {
                    bool on = player.PlayerHealth >= player.PlayerMaxHealth * 0.99f;
                    return detail ? (on ? Loc.T("발동 중 (체력 가득)") : Loc.T("꺼짐 (체력이 가득해야 함)")) : on ? "ON" : "OFF";
                }
            case KitPlate: return "-" + (IsEvolved(id) ? "25" : "15") + "%";
            case KitVeil:
                {
                    float wait = (IsEvolved(id) ? 2f : 3f) - (Time.time - lastHurt);
                    return wait <= 0f ? (detail ? Loc.T("준비됨 · 다음 평타 강화") : Loc.T("준비")) : wait.ToString("0.0");
                }
            case KitFugitive: return Time.time < fugitiveUntil ? (detail ? Loc.T("빨라짐") : "ON") : "";
            case KitWeakspot: return "+" + (IsEvolved(id) ? "70" : "35") + "%";
            case KitInstinct:
                {
                    int need = IsEvolved(id) ? 12 : 20;
                    return KitFreeDraws > 0 ? (detail ? Loc.T("자동 만궁 ") + KitFreeDraws + Loc.T("발 남음") : "x" + KitFreeDraws) : instinctKills + "/" + need;
                }
            case KitEmergency:
                {
                    float left = emergencyReady - Time.time;
                    return left > 0f ? left.ToString("0") + (detail ? Loc.T("초 뒤 준비") : "") : (detail ? Loc.T("준비됨") : Loc.T("준비"));
                }
            case KitGoldConvert: return (IsEvolved(id) ? "20" : "10") + "%";
        }
        return "";
    }
}

// 중독: 쌓일수록 강해지는 초록 도트 피해 (4초 동안 새로 안 맞으면 사라짐)
public class Poison : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    public float perStack;
    public int stacks;
    float until, tick, drip;

    public static void Apply(GameObject target, float perStack, int maxStacks)
    {
        if (target == null) return;
        Poison p = target.GetComponent<Poison>();
        if (p == null) p = target.AddComponent<Poison>();
        p.perStack = Mathf.Max(p.perStack, perStack);
        p.stacks = Mathf.Min(maxStacks, p.stacks + 1);
        p.until = Time.time + 4f;
    }

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        if (Time.time > until) { Destroy(this); return; }
        drip += Time.deltaTime;
        if (drip >= 0.35f)
        {
            drip = 0f;
            Fx.Spawn("fx_bleed", transform.position + (Vector3)(Random.insideUnitCircle * 0.4f), 1.2f, new Color(0.5f, 1f, 0.35f), 14f);
        }
        tick += Time.deltaTime;
        if (tick >= 0.5f)
        {
            tick = 0f;
            SignatureSkills.DotTick = true;        // 지속 피해 (맞힐 때 효과가 붙지 않게)
            Specials.Damage(gameObject, perStack * stacks * 0.5f, Vector3.zero, 0f);
            SignatureSkills.DotTick = false;
        }
    }
}

// 마름쇠: 밟은 적에게 피해 + 느리게, 세 번 밟히거나 8초가 지나면 사라짐
public class Caltrop : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    static readonly List<Collider2D> hits = new List<Collider2D>(8);
    float damage, life = 8f, cooldown;
    int uses = 3;

    public static void Place(Vector3 at, float damage)
    {
        FxAnim a = Fx.Play("fx_caltrop", at, 0.7f, Color.white, 1f, Random.Range(0f, 360f), 3, true, 8f);
        if (a == null) return;
        a.gameObject.AddComponent<Caltrop>().damage = damage;
    }

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        life -= Time.deltaTime;
        cooldown -= Time.deltaTime;
        if (life <= 0f || uses <= 0) { Destroy(gameObject); return; }
        if (cooldown > 0f) return;
        // 마름쇠마다 매 프레임이라 재사용 목록 (첫 적에 맞히면 바로 멈추므로 목록이 도중에 바뀌지 않음)
        foreach (Collider2D c in Specials.Overlap(transform.position, 0.6f, hits))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Specials.Damage(c.gameObject, damage, Vector3.zero, 0f);
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null) e.Slow(0.4f, 1.5f);
            uses--;
            cooldown = 0.4f;
            Fx.Spawn("fx_spark", transform.position, 0.8f, new Color(1f, 0.6f, 0.6f), 24f);
            break;
        }
    }
}

// 투창: 날아간 거리만큼 피해가 커짐 (15칸에서 최대)
public class JavelinGrow : MonoBehaviour
{
    public float baseDamage, maxMul = 3f;
    Bullet b;
    Vector3 start;

    void Start()
    {
        b = GetComponent<Bullet>();
        start = transform.position;
    }

    void Update()
    {
        if (b == null) return;
        b.damage = baseDamage * Mathf.Lerp(1f, maxMul, Vector2.Distance(start, transform.position) / 15f);
    }
}

// 돌풍 화살: 날아가며 바람 자국을 남김
public class WindTrail : MonoBehaviour
{
    float t;

    void Update()
    {
        t += Time.deltaTime;
        if (t < 0.05f) return;
        t = 0f;
        Fx.Spawn("fx_smoke", transform.position, 1.6f, new Color(0.75f, 1f, 0.95f, 0.5f), 18f);
    }
}
