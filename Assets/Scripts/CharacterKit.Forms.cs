using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 거너가 아닌 캐릭터의 무기 진화 (1.9.4~): 평타 모양은 그대로, 고른 무기의 느낌이 평타 능력 + 우클릭 궁극기로 (SpecialAbilities.KitUlts)
//   1차 진화(형태 능력): 캐릭터 전용 무기 셋 중 하나
//     검사     대지의 일격(세 번째마다 내리찍어 기절) · 채찍 칼날(사거리 +30%, 끝이 아픔) · 창날 찌르기(벨 때마다 창날이 앞으로)
//     도적     독 표창(독 · 공격 속도 +20%) · 도박 표창(피해 제각각 · 조커 폭발) · 와이어 표창(맞은 적에서 줄이 튐)
//     궁수     그물 화살(맞은 자리 묶기) · 투창 화살(가득 당기면 모두 관통 · 멀리 갈수록 강함) · 연사 화살(한 발 더)
//     연금술사 수은 플라스크(한 번 더 튕겨 터짐) · 자석 플라스크(끌어모아 터짐) · 폭죽 플라스크(불꽃 세 갈래)
//   2차 진화(강화): 그 위에 덧붙는 효과 (캐릭터마다 셋)
public partial class CharacterKit
{
    // 2차 진화 강화 번호 (특수 능력 번호와 겹치지 않게 100부터)
    public const int AugStorm = 100, AugExecute = 101, AugThunder = 102;       // 검사
    public const int AugEcho = 103, AugVenom = 104, AugVolley = 105;           // 도적
    public const int AugBlast = 106, AugSeek = 107, AugGale = 108;             // 궁수
    public const int AugDouble = 109, AugGiant = 110, AugOverload = 111;       // 연금술사
    public const int AugSurvey = 112, AugReprint = 113, AugEngrave = 114;      // 지도 제작자 (1.1.2)
    public const int AugPrecise = 115, AugTwinGear = 116, AugRift = 117;       // 시계공
    public const int AugThickEnvelope = 118, AugBundle = 119, AugRegistered = 120;       // 우체부
    public const int AugDeepChill = 121, AugTwoHands = 122, AugSoulBurst = 123;       // 유령
    public const int AugFirst = 100, AugLast = 123;
    // 강화 아이콘: 1.1.2 새 캐릭터는 600번대 (예전 92 ~ 103 다음 번호는 다른 아이콘이 쓰고 있음)
    public static string AugIcon(int id) => "Icons/ability_" + (id >= 112 ? 600 + (id - 112) : id - AugFirst + 92);

    [HideInInspector] public int form = -1;         // 1차 진화 형태 (캐릭터 전용 무기 번호), -1 = 기본
    [HideInInspector] public int augment = -1;      // 2차 진화 강화
    [HideInInspector] public float formPower = 1f;  // 영혼 트리 형태 숙련: 평타 피해 배율
    int swingCount, throwCount, hammerCount;

    public static int[] AugmentsFor(CharacterId who) => who switch
    {
        CharacterId.Swordsman => new[] { AugStorm, AugExecute, AugThunder },
        CharacterId.Rogue => new[] { AugEcho, AugVenom, AugVolley },
        CharacterId.Archer => new[] { AugBlast, AugSeek, AugGale },
        CharacterId.Cartographer => new[] { AugSurvey, AugReprint, AugEngrave },
        CharacterId.Clockmaker => new[] { AugPrecise, AugTwinGear, AugRift },
        CharacterId.Postman => new[] { AugThickEnvelope, AugBundle, AugRegistered },
        CharacterId.Ghost => new[] { AugDeepChill, AugTwoHands, AugSoulBurst },
        _ => new[] { AugDouble, AugGiant, AugOverload },
    };

    public static string AugmentName(int id) => id switch
    {
        AugStorm => "폭풍", AugExecute => "처단", AugThunder => "뇌광",
        AugEcho => "잔상", AugVenom => "맹독", AugVolley => "연발",
        AugBlast => "폭발 화살", AugSeek => "추적 화살", AugGale => "꿰뚫는 바람",
        AugDouble => "이중 폭발", AugGiant => "거대 플라스크",
        AugSurvey => "정밀 측량", AugReprint => "이중 인쇄", AugEngrave => "판화",
        AugPrecise => "정밀 톱니", AugTwinGear => "이중 톱니", AugRift => "시간 균열",
        AugThickEnvelope => "두꺼운 봉투", AugBundle => "다발 배송", AugRegistered => "등기 표식",
        AugDeepChill => "깊은 냉기", AugTwoHands => "두 손", AugSoulBurst => "영혼 폭발",
        _ => "연금 폭주",
    };

    public static string AugmentDesc(int id) => id switch
    {
        AugStorm => "세 번 벨 때마다 한 번은 몸 주위를 한 바퀴 크게 벱니다.",
        AugExecute => "체력 25% 아래인 적은 세 배로 벱니다.",
        AugThunder => "벨 때마다 처음 맞은 적에게서 번개가 적 셋에게 튑니다.",
        AugEcho => "던질 때마다 잠시 뒤 그림자가 같은 방향으로 한 번 더 던집니다. (피해 60%)",
        AugVenom => "모든 평타가 독을 쌓습니다. (최대 6중첩)",
        AugVolley => "한 번에 하나 더 던집니다.",
        AugBlast => "화살이 맞은 자리에서 폭발합니다. (피해 60%)",
        AugSeek => "화살이 가까운 적을 쫓아갑니다.",
        AugGale => "화살이 적 셋을 더 꿰뚫고 30% 더 빠르게 날아갑니다.",
        AugDouble => "플라스크가 터진 자리에서 한 번 더 터집니다. (피해 60%)",
        AugGiant => "폭발 범위 +40%, 피해 +20%",
        AugSurvey => "획이 적 하나를 지날 때마다 그다음 적에게 주는 피해가 10%씩 오릅니다. (최대 +50%)",
        AugReprint => "획을 그을 때마다 0.3초 뒤 같은 자리에 한 번 더 그어집니다. (피해 50%)",
        AugEngrave => "도형을 닫으면 안쪽에 잉크 폭발이 일어납니다. (공격력 150%)",
        AugPrecise => "정각까지 필요한 눈금이 2 줄어듭니다.",
        AugTwinGear => "톱니를 하나 더 던집니다.",
        AugRift => "정각 톱니가 터진 자리에 2초 동안 시간 균열이 생겨 안의 적을 느리게 하고 벱니다.",
        AugThickEnvelope => "편지 피해가 25% 늘어납니다.",
        AugBundle => "편지를 두 통 더 보냅니다. (피해 50%)",
        AugRegistered => "속달 표식이 50% 더 많이 붙습니다.",
        AugDeepChill => "얼어붙는 시간이 50% 늘어납니다.",
        AugTwoHands => "손길이 양옆으로 두 갈래 더 뻗습니다. (피해 70%)",
        AugSoulBurst => "영혼이 피해를 막을 때 터져 둘레 적에게 피해를 줍니다. (공격력 150%)",
        _ => "네 번째 플라스크마다 반드시 불안정해져 크게 터집니다.",
    };

    // 형태 능력 이름 (진화 카드 · 트리 · 도감)
    public static string FormAugName(int id) => id switch
    {
        SpecialAbilities.KitHammer => "대지의 일격",
        SpecialAbilities.KitWhip => "채찍 칼날",
        SpecialAbilities.KitLance => "기사의 기세",
        SpecialAbilities.KitBlowgun => "독 표창",
        SpecialAbilities.KitCards => "도박 표창",
        SpecialAbilities.KitWire => "와이어 덫",
        SpecialAbilities.KitNetBow => "그물 화살",
        SpecialAbilities.KitJavelin => "투창 화살",
        SpecialAbilities.KitBurstBow => "속사 화살",
        SpecialAbilities.KitQuicksilver => "수은 플라스크",
        SpecialAbilities.KitMagnet => "자석 플라스크",
        SpecialAbilities.KitFirework => "폭죽 플라스크",
        SpecialAbilities.KitSurveyChain => "측량 사슬",
        SpecialAbilities.KitInkpot => "잉크병",
        SpecialAbilities.KitGlobe => "지구본",
        SpecialAbilities.KitCuckoo => "뻐꾸기 시계",
        SpecialAbilities.KitHourglass => "모래시계",
        SpecialAbilities.KitGrandfather => "괘종시계",
        SpecialAbilities.KitPigeon => "비둘기 우편",
        SpecialAbilities.KitParcel => "소포 상자",
        SpecialAbilities.KitPostbox => "우체통",
        SpecialAbilities.KitSoulChain => "사슬 망령",
        SpecialAbilities.KitIceGhost => "얼음 유령",
        SpecialAbilities.KitShadowGhost => "그림자 유령",
        _ => "",
    };

    // 형태 능력 설명 (진화 카드 · 트리 · 도감)
    public static string FormDesc(int id) => id switch
    {
        SpecialAbilities.KitHammer => "세 번 벨 때마다 땅을 내리찍어 주변 적에게 피해(150%)를 주고 잠깐 기절시킵니다.",
        SpecialAbilities.KitWhip => "베기 사거리 +30%, 칼끝에 맞은 적은 피해 1.5배.",
        SpecialAbilities.KitLance => "움직이면서 베면 피해 +40%.",
        SpecialAbilities.KitBlowgun => "표창이 맞은 적에게 독을 쌓고 (최대 5중첩), 공격 속도 +20%.",
        SpecialAbilities.KitCards => "표창 피해가 제각각 (50~170%), 가끔 조커가 폭발합니다.",
        SpecialAbilities.KitWire => "표창에 맞은 적과 가까운 적 하나가 줄로 묶여 1초 동안 70% 느려집니다.",
        SpecialAbilities.KitNetBow => "반 이상 당긴 화살이 맞은 자리에 그물을 펼쳐 주변 적을 잠깐 묶습니다.",
        SpecialAbilities.KitJavelin => "가득 당긴 화살이 모든 적을 꿰뚫고 멀리 날아갈수록 피해가 증가합니다. (최대 200%)",
        SpecialAbilities.KitBurstBow => "시위를 25% 더 빨리 당기고, 반 이상 당긴 화살은 적을 하나 더 꿰뚫습니다.",
        SpecialAbilities.KitQuicksilver => "플라스크가 터진 뒤 가까운 적에게 한 번 더 튕겨 터집니다. (피해 70%)",
        SpecialAbilities.KitMagnet => "플라스크가 터지기 전에 주변 적을 끌어모읍니다.",
        SpecialAbilities.KitFirework => "플라스크가 터지면 불꽃 세 갈래가 퍼져 한 번씩 더 터집니다. (피해 45%)",
        SpecialAbilities.KitSurveyChain => "획 끝에서 가장 가까운 적에게 측량 사슬이 튑니다. (피해 50%)",
        SpecialAbilities.KitInkpot => "획 끝에서 잉크병이 깨져 작게 터집니다. (피해 60%)",
        SpecialAbilities.KitGlobe => "도형을 닫기 쉬워지고(닫는 거리 1.5배), 닫으면 안쪽 적이 가운데로 모이며 도형 피해 +30%.",
        SpecialAbilities.KitCuckoo => "정각 톱니가 터질 때마다 뻐꾸기 셋이 튀어나와 가까운 적을 쪼습니다. (공격력 100%씩)",
        SpecialAbilities.KitHourglass => "톱니에 맞은 적이 모래에 묻혀 1초 동안 절반 속도로 느려집니다.",
        SpecialAbilities.KitGrandfather => "톱니가 크고 느려지지만 피해가 40% 늘어납니다.",
        SpecialAbilities.KitPigeon => "첫 편지는 비둘기가 물고 가 받는 이를 두 번 더 쪼습니다. (피해 50%씩)",
        SpecialAbilities.KitParcel => "편지가 소포 상자가 되어 맞은 자리에서 터집니다. (피해 60%)",
        SpecialAbilities.KitPostbox => "3초마다 발밑에 우체통이 서서 4초 동안 가까운 적에게 편지를 던집니다.",
        SpecialAbilities.KitSoulChain => "손길에 처음 맞은 적에게서 영혼 사슬이 가장 가까운 적에게 튑니다. (피해 40%)",
        SpecialAbilities.KitIceGhost => "냉기가 두 배로 쌓입니다.",
        SpecialAbilities.KitShadowGhost => "손길이 0.15초 뒤 한 번 더 뻗습니다. (피해 50%)",
        _ => "",
    };

    // 형태별 공격 속도 배율 (평타 모양이 그대로라 독 표창만 빨라짐)
    float FormRate => form == SpecialAbilities.KitBlowgun ? 1.2f : form == SpecialAbilities.KitBurstBow ? 1.25f : 1f;

    // ================================================================= 검사
    float SwingReachMul => form == SpecialAbilities.KitWhip ? 1.3f : 1f;

    float SwingHalf(float half) => half;

    // 적 하나를 벨 때 피해 배율 (형태 · 강화)
    float SwingHitMul(Collider2D c, float dist, float reach)
    {
        float m = formPower;
        if (form == SpecialAbilities.KitWhip && dist > reach * 0.6f) m *= 1.5f;
        if (form == SpecialAbilities.KitLance && stillTime <= 0.05f) m *= 1.4f;      // 기사의 기세: 움직이면서 벨 때
        if (augment == AugExecute && c.TryGetComponent(out EnermyController e) && e.EnemyHealth <= e.setEnemyHP * 0.25f) m *= 3f;
        return m;
    }

    void OnSwingHit(Collider2D c, bool first)
    {
        if (first && augment == AugThunder) ChainBolt(c, Damage * 0.6f * formPower, 3);
    }

    void SwingFormBefore(Vector2 dir) { }

    // (예전 기창 돌진 자리) 평타를 가로채지 않음
    bool LanceIntercept(Vector2 dir, int shots) => false;

    void SwingFormAfter(Vector3 origin, float reach)
    {
        if (form == SpecialAbilities.KitHammer && ++hammerCount % 3 == 0)
        {
            // 대지의 일격: 세 번째 베기마다 내리찍어 주변 적 피해 + 잠깐 기절
            float r = reach * 0.8f;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(origin, r))
                if (c.TryGetComponent(out EnermyController e) && !e.IsDead) e.Slow(0f, 0.6f);
            DamageCircle(origin, r, Damage * 1.5f * formPower, 2f);
            Fx.Spawn("fx_shock", origin, r * 2.4f, new Color(0.8f, 0.85f, 1f, 0.8f), 20f);
            Hostile.Shake(0.12f);
            Play("thump", 0.8f, 0.8f);
        }
        if (augment == AugStorm && ++swingCount % 3 == 0)
        {
            DamageCircle(origin, reach * 1.2f, Damage * 1.2f * formPower, 2.5f);
            Fx.Spawn("fx_shock", origin, reach * 2.6f, new Color(0.7f, 0.9f, 1f, 0.8f), 18f);
            Play("whoosh", 0.7f, 0.7f);
        }
    }

    // 번개: 처음 맞은 적에서 가까운 적 n 명에게 차례로
    void ChainBolt(Collider2D first, float dmg, int jumps)
    {
        Transform cur = first.transform;
        HashSet<Transform> hit = new HashSet<Transform> { cur };
        for (int i = 0; i < jumps; i++)
        {
            Transform next = null;
            float best = 6f;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(cur.position, 6f))
            {
                if (hit.Contains(c.transform) || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                float d = Vector2.Distance(cur.position, c.transform.position);
                if (d < best) { best = d; next = c.transform; }
            }
            if (next == null) break;
            hit.Add(next);
            SparkLine(cur.position, next.position, new Color(0.6f, 0.9f, 1f));
            Specials.Damage(next.gameObject, dmg, Vector3.zero, 0f);
            cur = next;
        }
        Play("zap", 0.4f, 1.1f);
    }

    static void SparkLine(Vector3 a, Vector3 b, Color c)
    {
        for (int i = 1; i <= 4; i++) Fx.Spawn("fx_spark", Vector3.Lerp(a, b, i / 4f), 1f, c, 26f);
    }

    // ================================================================= 도적
    void RogueThrow(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        if (augment == AugVolley) shots++;
        float step = form == SpecialAbilities.KitCards ? 9f : 8f;
        foreach (Vector2 d in Spread(dir, shots, step)) ThrowOne(start, d, dmg, 1f);
        Play("whoosh", 0.35f, form == SpecialAbilities.KitBlowgun ? 2.3f : 1.8f);
        if (augment == AugEcho) StartCoroutine(EchoThrow(dir, dmg, shots, step));
    }

    IEnumerator EchoThrow(Vector2 dir, float dmg, int shots, float step)
    {
        yield return new WaitForSeconds(0.15f);
        Vector3 start = player.MuzzlePosition;
        foreach (Vector2 d in Spread(dir, shots, step))
        {
            Bullet b = ThrowOne(start, d, dmg * 0.6f, 0.6f);
            if (b != null && b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(0.55f, 0.4f, 0.8f, 0.8f);
        }
    }

    Bullet ThrowOne(Vector3 start, Vector2 d, float dmg, float alpha)
    {
        float mul = formPower;
        if (form == SpecialAbilities.KitCards) mul *= Random.Range(0.5f, 1.7f);
        Bullet b = Shuriken(start, d, dmg * mul, card[0]);
        if (b == null) return null;
        SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
        switch (form)
        {
            case SpecialAbilities.KitBlowgun:
                if (sr != null) sr.color = new Color(0.6f, 1f, 0.45f, alpha);
                break;
            case SpecialAbilities.KitCards:
                bool joker = Random.value < 0.12f;
                if (sr != null) sr.color = joker ? new Color(1f, 0.85f, 0.3f, alpha) : new Color(1f, 0.55f, 0.6f, alpha);
                if (joker)
                    b.onHitEnemy += (s, c) =>
                    {
                        DamageCircle(c.transform.position, 2.2f, Damage * 1.8f * formPower, 2f);
                        Fx.Spawn("fx_explosion", c.transform.position, 4.4f, Color.white, 16f);
                        Play("boom", 0.5f, 1.3f);
                    };
                break;
            case SpecialAbilities.KitWire:
                if (sr != null) sr.color = new Color(0.8f, 0.85f, 1f, alpha);
                b.onHitEnemy += (s, c) => WireSnare(c);
                break;
        }
        if (form == SpecialAbilities.KitBlowgun || augment == AugVenom)
        {
            float per = Damage * 0.12f;
            int max = augment == AugVenom ? 6 : 5;
            b.onHitEnemy += (s, c) => Poison.Apply(c.gameObject, per, max);
        }
        return b;
    }

    // ================================================================= 궁수
    // 화살 하나에 형태 · 강화 (UpdateBow 가 쏠 때마다)
    void ArrowForm(Bullet b, float draw, Vector3 start)
    {
        b.damage *= formPower;
        switch (form)
        {
            case SpecialAbilities.KitNetBow:
                if (draw >= 0.5f)
                    b.onHitEnemy += (s, c) =>
                    {
                        foreach (Collider2D o in Physics2D.OverlapCircleAll(c.transform.position, 2.6f))
                            if (o.TryGetComponent(out EnermyController e)) e.Slow(0f, 0.8f + 0.6f * draw);
                        Fx.Spawn("fx_shock", c.transform.position, 5.2f, new Color(0.7f, 1f, 0.6f, 0.7f), 18f);
                    };
                break;
            case SpecialAbilities.KitBurstBow:
                if (draw >= 0.5f) b.pene += 1;
                break;
            case SpecialAbilities.KitJavelin:
                if (draw < 1f) break;
                b.pene = 9999;
                b.hitOnce = new HashSet<int>();
                b.transform.localScale *= 1.3f;
                float baseDmg = b.damage;
                b.onHitEnemy += (s, c) =>
                {
                    // 멀리 날아갈수록 강해짐 (최대 2배): 기본 피해는 이미 들어갔으니 더할 몫만
                    float k = Mathf.Min(2f, 1f + Vector2.Distance(start, c.transform.position) / 10f);
                    if (k > 1f) Specials.Damage(c.gameObject, baseDmg * (k - 1f), Vector3.zero, 0f);
                };
                break;
        }
        switch (augment)
        {
            case AugBlast:
                float boom = b.damage * 0.6f;
                b.onHitEnemy += (s, c) =>
                {
                    DamageCircle(c.transform.position, 2f, boom, 1.5f);
                    Fx.Spawn("fx_explosion", c.transform.position, 4f, Color.white, 16f);
                };
                break;
            case AugSeek:
                b.gameObject.AddComponent<Homing>().turnSpeed = 240f;
                break;
            case AugGale:
                b.pene += 3;
                b.speed *= 1.3f;
                break;
        }
    }

    // 와이어 덫: 맞은 적과 가까운 적 하나를 줄로 묶어 1초 동안 느리게
    void WireSnare(Collider2D c)
    {
        if (c == null) return;
        if (c.TryGetComponent(out EnermyController a)) a.Slow(0.3f, 1f);
        Transform other = NearestOther(c.transform.position, 6f, c.transform, Vector2.right, 360f);
        if (other == null) return;
        if (other.TryGetComponent(out EnermyController b)) b.Slow(0.3f, 1f);
        SparkLine(c.transform.position, other.position, new Color(0.85f, 0.85f, 1f));
    }
    // ================================================================= 연금술사
    float FlaskRadiusMul => augment == AugGiant ? 1.4f : 1f;
    float FlaskDamageMul => formPower * (augment == AugGiant ? 1.2f : 1f);

    bool ForceUnstable() => augment == AugOverload && ++throwCount % 4 == 0;
    // 다음 투척이 과부하로 반드시 불안정해지는지 (머리 위 시약 표시용, 횟수는 올리지 않음)
    bool NextForcedUnstable => augment == AugOverload && (throwCount + 1) % 4 == 0;

    // 터지는 순간 (피해를 주기 전): 끌어모으기 · 튕기기 · 불꽃 · 두 번째 폭발
    void FlaskExtras(Vector3 p, float r, float hit, int depth)
    {
        if (form == SpecialAbilities.KitMagnet)
        {
            foreach (Collider2D c in Physics2D.OverlapCircleAll(p, r * 2.2f))
                if (c.CompareTag("enermy")) c.transform.position = Vector3.Lerp(c.transform.position, p, 0.7f);
            Fx.Spawn("fx_shock", p, r * 4.4f, new Color(0.6f, 0.7f, 1f, 0.6f), 22f);
        }
        if (form == SpecialAbilities.KitQuicksilver && depth < 1)
        {
            Transform next = NearestOther(p, 7f, null, Vector2.right, 360f);
            Vector3 land = next != null ? next.position : p + (Vector3)(Random.insideUnitCircle.normalized * 3f);
            ThrowReagent(p, land, hit / 1.6f * 0.7f, depth + 1);
        }
        if (form == SpecialAbilities.KitFirework)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad + Random.Range(-0.3f, 0.3f);
                Vector3 q = p + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r * 1.5f;
                DamageCircle(q, r * 0.5f, hit * 0.45f, 0.8f);
                Fx.Spawn("fx_sparkle", q, r * 1.2f, Color.HSVToRGB(i / 5f, 0.6f, 1f), 20f);
            }
        }
        if (augment == AugDouble) StartCoroutine(SecondBlast(p, r * 0.8f, hit * 0.6f));
    }

    IEnumerator SecondBlast(Vector3 p, float r, float hit)
    {
        yield return new WaitForSeconds(0.35f);
        DamageCircle(p, r, hit, 1.2f);
        Fx.Spawn("fx_alchemyblast", p, r * 2.2f, new Color(1f, 0.8f, 0.5f), 18f);
        Play("boom", 0.4f, 1.3f);
    }

    // ================================================================= 영혼 트리 · 진화 연결
    public void SetForm(int id)
    {
        form = id;
        Fx.Spawn("fx_shock", transform.position, 8f, new Color(1f, 0.85f, 0.4f, 0.8f), 18f);
    }

    public void SetAugment(int id) => augment = id;
}
